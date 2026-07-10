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
    $targetInfo.LastWriteTimeUtc = $sourceInfo.LastWriteTimeUtc
    $targetInfo = Get-Item -LiteralPath $targetPath
    if ($targetInfo.Length -le 0) {
        throw "Copied firmware file is empty: $targetPath"
    }

    return [System.IO.Path]::GetFullPath($targetPath)
}

function Test-FileCurrent {
    param(
        [System.IO.FileInfo]$SourceInfo,
        [string]$TargetPath
    )

    if (-not (Test-Path -LiteralPath $TargetPath -PathType Leaf)) {
        return $false
    }

    $targetInfo = Get-Item -LiteralPath $TargetPath
    if ($targetInfo.Length -ne $SourceInfo.Length) {
        return $false
    }

    $timeDeltaSeconds = [Math]::Abs(($targetInfo.LastWriteTimeUtc - $SourceInfo.LastWriteTimeUtc).TotalSeconds)
    return $timeDeltaSeconds -lt 2
}

function Get-ConfiguredFirmwareFileName {
    param(
        [object]$FirmwareFile
    )

    if ($null -eq $FirmwareFile) {
        return ""
    }

    if ($FirmwareFile -is [string]) {
        return [string]$FirmwareFile
    }

    if ($null -ne $FirmwareFile.PSObject.Properties["MesFileName"]) {
        return [string]$FirmwareFile.MesFileName
    }

    if ($null -ne $FirmwareFile.PSObject.Properties["LocalFileName"]) {
        return [string]$FirmwareFile.LocalFileName
    }

    return [string]$FirmwareFile
}

function Get-RfpFirmwareFiles {
    param(
        [string]$MesDir,
        [object[]]$ConfiguredFirmwareFiles = @()
    )

    if ($ConfiguredFirmwareFiles.Count -gt 0) {
        $configuredFiles = New-Object System.Collections.Generic.List[System.IO.FileInfo]
        foreach ($firmwareFile in $ConfiguredFirmwareFiles) {
            $fileName = Get-ConfiguredFirmwareFileName -FirmwareFile $firmwareFile
            if ([string]::IsNullOrWhiteSpace($fileName)) {
                continue
            }

            $path = Join-Path $MesDir $fileName.Trim()
            if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
                throw "Configured RFP firmware file not found: $path"
            }

            $configuredFiles.Add((Get-Item -LiteralPath $path))
        }

        if ($configuredFiles.Count -eq 0) {
            throw "Configured RFP FirmwareFiles is empty after trimming."
        }

        return @($configuredFiles.ToArray())
    }

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

function Get-ConfiguredFirmwareFiles {
    param(
        [object]$Object
    )

    if ($null -eq $Object -or $null -eq $Object.PSObject.Properties["FirmwareFiles"]) {
        return @()
    }

    return @($Object.FirmwareFiles |
        Where-Object { -not [string]::IsNullOrWhiteSpace((Get-ConfiguredFirmwareFileName -FirmwareFile $_)) } |
        ForEach-Object { $_ } |
        Where-Object { $null -ne $_ })
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

function Test-RfpProgramFilesCurrent {
    param(
        [xml]$ProjectXml,
        [string[]]$LocalPaths
    )

    $items = @($ProjectXml.SelectNodes("/RfpProject/OperationTab/ProgramFiles/Item"))
    $currentPaths = @($items |
        ForEach-Object { [string]$_.InnerText } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { [System.IO.Path]::GetFullPath($_) })

    if ($currentPaths.Count -ne $LocalPaths.Count) {
        return $false
    }

    for ($i = 0; $i -lt $LocalPaths.Count; $i++) {
        if (-not [string]::Equals($currentPaths[$i], [System.IO.Path]::GetFullPath($LocalPaths[$i]), [StringComparison]::OrdinalIgnoreCase)) {
            return $false
        }
    }

    return $true
}

function Test-RfpPreparationCurrent {
    param(
        [xml]$ProjectXml,
        [System.IO.FileInfo[]]$MesFiles,
        [string[]]$LocalPaths
    )

    if (-not (Test-RfpProgramFilesCurrent -ProjectXml $ProjectXml -LocalPaths $LocalPaths)) {
        return $false
    }

    for ($i = 0; $i -lt $MesFiles.Count; $i++) {
        if (-not (Test-FileCurrent -SourceInfo $MesFiles[$i] -TargetPath $LocalPaths[$i])) {
            return $false
        }
    }

    return $true
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

    Get-ChildItem -LiteralPath $fullLocalDir -Force |
        Where-Object { $_.Name -ne ".rfp-firmware-cache" } |
        Remove-Item -Recurse -Force
}

function Assert-NoReparsePoints {
    param([string]$AllowedRoot, [string]$TargetPath)

    foreach ($path in @($AllowedRoot, $TargetPath)) {
        if ((Test-Path -LiteralPath $path) -and ((Get-Item -LiteralPath $path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Unsafe firmware cache reparse point: $path"
        }
    }

    $cursor = $TargetPath
    while (-not [string]::Equals($cursor, $AllowedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        if ((Test-Path -LiteralPath $cursor) -and ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Unsafe firmware cache reparse point: $cursor"
        }
        $parent = [IO.Directory]::GetParent($cursor)
        if ($null -eq $parent) { break }
        $cursor = $parent.FullName
    }

    if (Test-Path -LiteralPath $TargetPath -PathType Container) {
        $linkedEntry = Get-ChildItem -LiteralPath $TargetPath -Force -Recurse |
            Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint } |
            Select-Object -First 1
        if ($null -ne $linkedEntry) {
            throw "Unsafe firmware cache reparse point: $($linkedEntry.FullName)"
        }
    }
}

function Initialize-OwnedRfpCache {
    param([string]$LocalDir, [string]$MesDir, [string]$ConfigDir)

    $fullLocalDir = [IO.Path]::GetFullPath($LocalDir).TrimEnd('\', '/')
    $allowedRoot = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $PSScriptRoot) "Firmware")).TrimEnd('\', '/')
    $prefix = $allowedRoot + [IO.Path]::DirectorySeparatorChar
    if (-not $fullLocalDir.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe RFP LocalFirmwarePath outside allowed Firmware cache root: $fullLocalDir"
    }
    if ([string]::Equals($fullLocalDir, [IO.Path]::GetFullPath($ConfigDir).TrimEnd('\', '/'), [StringComparison]::OrdinalIgnoreCase) -or
        [string]::Equals($fullLocalDir, [IO.Path]::GetFullPath($MesDir).TrimEnd('\', '/'), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe RFP LocalFirmwarePath overlaps config or MES directory: $fullLocalDir"
    }

    Assert-NoReparsePoints -AllowedRoot $allowedRoot -TargetPath $fullLocalDir
    if (-not (Test-Path -LiteralPath $fullLocalDir)) {
        New-Item -ItemType Directory -Path $fullLocalDir | Out-Null
    }

    $markerPath = Join-Path $fullLocalDir ".rfp-firmware-cache"
    $entries = @(Get-ChildItem -LiteralPath $fullLocalDir -Force)
    if ($entries.Count -gt 0 -and -not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        throw "RFP firmware cache ownership marker is missing: $markerPath"
    }
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        [IO.File]::WriteAllText($markerPath, "RfpTestStation firmware cache`r`n", [Text.UTF8Encoding]::new($false))
    }
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

$configuredFirmwareFiles = @(Get-ConfiguredFirmwareFiles -Object $projectConfig)
$mesFirmwareFiles = @(Get-RfpFirmwareFiles -MesDir $mesDir -ConfiguredFirmwareFiles $configuredFirmwareFiles)
$expectedLocalPaths = @($mesFirmwareFiles | ForEach-Object { [System.IO.Path]::GetFullPath((Join-Path $localDir $_.Name)) })

Initialize-OwnedRfpCache -LocalDir $localDir -MesDir $mesDir -ConfigDir $configDir

if (Test-RfpPreparationCurrent -ProjectXml $projectXml -MesFiles $mesFirmwareFiles -LocalPaths $expectedLocalPaths) {
    [System.IO.File]::WriteAllLines($preparedFirmwarePathsFile, $expectedLocalPaths, $utf8NoBom)
    [System.IO.File]::WriteAllLines($preparedProjectFirmwarePathsFile, $expectedLocalPaths, $utf8NoBom)

    Write-Host "[INFO] Prepared firmware for $projectName already current."
    Write-Host "[INFO] MES=$mesDir"
    Write-Host "[INFO] Local=$localDir"
    return
}

Clear-LocalFirmwareDirectory -LocalDir $localDir -MesDir $mesDir -ConfigDir $configDir -Context $projectName

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
