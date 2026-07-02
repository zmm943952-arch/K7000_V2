param(
    [Parameter(Mandatory = $true)]
    [string]$ConfigPath,

    [Parameter(Mandatory = $true)]
    [string]$ProjectPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

trap {
    Write-Host "[FAIL] $($_.Exception.Message)"
    exit 1
}

function Resolve-ConfiguredPath {
    param(
        [string]$BaseDir,
        [string]$PathText
    )

    if ([string]::IsNullOrWhiteSpace($PathText)) {
        throw "Configured path is empty."
    }

    if ([System.IO.Path]::IsPathRooted($PathText)) {
        return [System.IO.Path]::GetFullPath($PathText)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BaseDir $PathText))
}

function Get-RequiredProperty {
    param(
        [object]$Object,
        [string]$Name,
        [string]$Context
    )

    if ($null -eq $Object.PSObject.Properties[$Name]) {
        throw "Missing '$Name' in $Context."
    }

    return $Object.$Name
}

function Copy-FirmwareFile {
    param(
        [string]$MesDir,
        [string]$LocalDir,
        [string]$MesFileName,
        [string]$LocalFileName
    )

    if ([string]::IsNullOrWhiteSpace($MesFileName)) {
        throw "MesFileName is empty."
    }

    if ([string]::IsNullOrWhiteSpace($LocalFileName)) {
        $LocalFileName = Split-Path -Leaf $MesFileName
    }

    $sourcePath = Join-Path $MesDir $MesFileName
    try {
        $sourceExists = Test-Path -LiteralPath $sourcePath -PathType Leaf -ErrorAction Stop
    }
    catch {
        throw "MES firmware file not found or inaccessible: $sourcePath. $($_.Exception.Message)"
    }

    if (-not $sourceExists) {
        throw "MES firmware file not found: $sourcePath"
    }

    $sourceInfo = Get-Item -LiteralPath $sourcePath
    if ($sourceInfo.Length -le 0) {
        throw "MES firmware file is empty: $sourcePath"
    }

    if (-not (Test-Path -LiteralPath $LocalDir)) {
        New-Item -ItemType Directory -Path $LocalDir | Out-Null
    }

    $targetPath = Join-Path $LocalDir $LocalFileName
    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force

    $targetInfo = Get-Item -LiteralPath $targetPath
    if ($targetInfo.Length -le 0) {
        throw "Copied firmware file is empty: $targetPath"
    }

    return [System.IO.Path]::GetFullPath($targetPath)
}

function Get-RfpFirmwareFiles {
    param(
        [string]$MesDir
    )

    $allowedExtensions = @(".mot", ".srec", ".hex", ".bin")
    $files = @(Get-ChildItem -LiteralPath $MesDir -File |
        Where-Object { $allowedExtensions -contains $_.Extension.ToLowerInvariant() } |
        Sort-Object Name)

    if ($files.Count -eq 0) {
        throw "No RFP firmware files found in MES directory: $MesDir"
    }

    $orderedRanks = @($files |
        ForEach-Object { Get-RfpFirmwareSortRank -FileName $_.Name } |
        Where-Object { $_ -lt 3 } |
        Sort-Object -Unique)

    if ($orderedRanks.Count -gt 1) {
        $files = @($files | Sort-Object @{ Expression = { Get-RfpFirmwareSortRank -FileName $_.Name }; Ascending = $true }, Name)
    }

    return $files
}

function Get-RfpFirmwareSortRank {
    param(
        [string]$FileName
    )

    if ($FileName.IndexOf("FIDM", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        return 0
    }

    if ($FileName.IndexOf("boot", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        return 1
    }

    if ($FileName.IndexOf("data", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        return 2
    }

    return 3
}

function Replace-RfpProgramFiles {
    param(
        [xml]$ProjectXml,
        [string[]]$LocalPaths
    )

    $programFilesNode = $ProjectXml.SelectSingleNode("/RfpProject/OperationTab/ProgramFiles")
    if ($null -eq $programFilesNode) {
        throw "No ProgramFiles node found in RFP project."
    }

    $oldItems = @($programFilesNode.SelectNodes("Item"))
    $templateItem = $null
    if ($oldItems.Count -gt 0) {
        $templateItem = $oldItems[$oldItems.Count - 1]
    }

    foreach ($oldItem in $oldItems) {
        [void]$programFilesNode.RemoveChild($oldItem)
    }

    for ($i = 0; $i -lt $LocalPaths.Count; $i++) {
        if ($i -lt $oldItems.Count) {
            $newItem = $oldItems[$i].Clone()
        }
        elseif ($null -ne $templateItem) {
            $newItem = $templateItem.Clone()
        }
        else {
            $newItem = $ProjectXml.CreateElement("Item")
            $newItem.SetAttribute("Address", "00000000")
            $newItem.SetAttribute("Type", "SREC")
        }

        $newItem.InnerText = $LocalPaths[$i]
        [void]$programFilesNode.AppendChild($newItem)
    }
}

function Clear-LocalFirmwareDirectory {
    param(
        [string]$LocalDir,
        [string]$MesDir,
        [string]$ConfigDir,
        [string]$Context
    )

    if ([string]::IsNullOrWhiteSpace($LocalDir)) {
        throw "Local firmware directory is empty for $Context."
    }

    $fullLocalDir = [System.IO.Path]::GetFullPath($LocalDir)
    $fullMesDir = [System.IO.Path]::GetFullPath($MesDir)
    $fullConfigDir = [System.IO.Path]::GetFullPath($ConfigDir)
    $localRoot = [System.IO.Path]::GetPathRoot($fullLocalDir)
    $normalizedLocal = $fullLocalDir.TrimEnd('\', '/')

    if ([string]::Equals($normalizedLocal, $localRoot.TrimEnd('\', '/'), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clear drive root for ${Context}: $fullLocalDir"
    }

    if ([string]::Equals($normalizedLocal, $fullConfigDir.TrimEnd('\', '/'), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clear config root for ${Context}: $fullLocalDir"
    }

    if ([string]::Equals($normalizedLocal, $fullMesDir.TrimEnd('\', '/'), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clear MES source directory for ${Context}: $fullLocalDir"
    }

    if (-not (Test-Path -LiteralPath $fullLocalDir)) {
        New-Item -ItemType Directory -Path $fullLocalDir | Out-Null
        return
    }

    Get-ChildItem -LiteralPath $fullLocalDir -Force | Remove-Item -Recurse -Force
}

$configFullPath = [System.IO.Path]::GetFullPath($ConfigPath)
if (-not (Test-Path -LiteralPath $configFullPath -PathType Leaf)) {
    throw "Config file not found: $configFullPath"
}

$projectFullPath = [System.IO.Path]::GetFullPath($ProjectPath)
if (-not (Test-Path -LiteralPath $projectFullPath -PathType Leaf)) {
    throw "RFP project file not found: $projectFullPath"
}

$configDir = Split-Path -Parent $configFullPath
$projectName = Split-Path -Leaf $projectFullPath
$config = Get-Content -LiteralPath $configFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
$params = Get-RequiredProperty -Object (Get-RequiredProperty -Object $config -Name "Burn1" -Context "Config.json") -Name "Params" -Context "Burn1"

[xml]$projectXml = Get-Content -LiteralPath $projectFullPath
$projectBaseName = [System.IO.Path]::GetFileNameWithoutExtension($projectName)
$rfpAutoDir = Split-Path -Parent (Split-Path -Parent $projectFullPath)
$preparedFirmwarePathsFile = Join-Path $rfpAutoDir "prepared_firmware_paths.txt"
$preparedProjectFirmwarePathsFile = Join-Path $rfpAutoDir ("prepared_firmware_paths_{0}.txt" -f $projectBaseName)

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

if ($null -eq $params.PSObject.Properties["RfpProjects"]) {
    $currentItems = @($projectXml.SelectNodes("/RfpProject/OperationTab/ProgramFiles/Item"))
    $currentPaths = @($currentItems | ForEach-Object { [string]$_.InnerText } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($currentPaths.Count -eq 0) {
        throw "Missing 'RfpProjects' in Burn1.Params and no firmware paths were found in project: $projectName"
    }

    [System.IO.File]::WriteAllLines($preparedFirmwarePathsFile, $currentPaths, $utf8NoBom)
    [System.IO.File]::WriteAllLines($preparedProjectFirmwarePathsFile, $currentPaths, $utf8NoBom)

    Write-Host "[INFO] Prepared firmware paths for $projectName from existing RFP project."
    Write-Host "[INFO] PreparedFirmwarePaths=$preparedFirmwarePathsFile"
    Write-Host "[INFO] PreparedProjectFirmwarePaths=$preparedProjectFirmwarePathsFile"
    return
}

$projectConfigs = @($params.RfpProjects)
$projectConfig = $projectConfigs |
    Where-Object { [string]$_.ProjectName -ieq $projectName } |
    Select-Object -First 1

if ($null -eq $projectConfig) {
    throw "No RfpProjects entry found for project: $projectName"
}

$mesDir = Resolve-ConfiguredPath -BaseDir $configDir -PathText ([string](Get-RequiredProperty -Object $projectConfig -Name "MesFirmwarePath" -Context $projectName))
$localDir = Resolve-ConfiguredPath -BaseDir $configDir -PathText ([string](Get-RequiredProperty -Object $projectConfig -Name "LocalFirmwarePath" -Context $projectName))

try {
    $mesDirExists = Test-Path -LiteralPath $mesDir -PathType Container -ErrorAction Stop
}
catch {
    throw "MES firmware directory not found or inaccessible: $mesDir. $($_.Exception.Message)"
}

if (-not $mesDirExists) {
    throw "MES firmware directory not found: $mesDir"
}

Clear-LocalFirmwareDirectory -LocalDir $localDir -MesDir $mesDir -ConfigDir $configDir -Context $projectName

$mesFirmwareFiles = @(Get-RfpFirmwareFiles -MesDir $mesDir)
$localPaths = New-Object System.Collections.Generic.List[string]
foreach ($firmwareFile in $mesFirmwareFiles) {
    $localPath = Copy-FirmwareFile -MesDir $mesDir -LocalDir $localDir -MesFileName $firmwareFile.Name -LocalFileName $firmwareFile.Name
    $localPaths.Add($localPath)
}

$backupDir = Join-Path (Split-Path -Parent $projectFullPath) "Backup"
if (-not (Test-Path -LiteralPath $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss_fff"
$backupPath = Join-Path $backupDir ("{0}_{1}.rpj" -f $projectBaseName, $timestamp)
Copy-Item -LiteralPath $projectFullPath -Destination $backupPath -Force

Replace-RfpProgramFiles -ProjectXml $projectXml -LocalPaths $localPaths.ToArray()

$writerSettings = [System.Xml.XmlWriterSettings]::new()
$writerSettings.Encoding = $utf8NoBom
$writerSettings.Indent = $true
$writerSettings.NewLineChars = "`r`n"

$writer = [System.Xml.XmlWriter]::Create($projectFullPath, $writerSettings)
try {
    $projectXml.Save($writer)
}
finally {
    $writer.Dispose()
}

[System.IO.File]::WriteAllLines($preparedFirmwarePathsFile, $localPaths.ToArray(), $utf8NoBom)
[System.IO.File]::WriteAllLines($preparedProjectFirmwarePathsFile, $localPaths.ToArray(), $utf8NoBom)

Write-Host "[INFO] Prepared firmware for $projectName"
Write-Host "[INFO] MES=$mesDir"
Write-Host "[INFO] Local=$localDir"
Write-Host "[INFO] Backup=$backupPath"
