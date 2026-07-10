param(
    [Parameter(Mandatory = $true)]
    [string]$ConfigPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputBinPathFile
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

function Write-PreparedBinPathFile {
    param(
        [string]$OutputBinPathFile,
        [string]$LocalBinPath
    )

    $outputDir = Split-Path -Parent $OutputBinPathFile
    if ($outputDir -and -not (Test-Path -LiteralPath $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir | Out-Null
    }

    Set-Content -LiteralPath $OutputBinPathFile -Value $LocalBinPath -Encoding ASCII
}

function Convert-ToRelativePath {
    param(
        [string]$BaseDir,
        [string]$PathText
    )

    $baseUri = [System.Uri]::new(([System.IO.Path]::GetFullPath($BaseDir).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar))
    $pathUri = [System.Uri]::new([System.IO.Path]::GetFullPath($PathText))

    if (-not [string]::Equals($baseUri.Scheme, $pathUri.Scheme, [StringComparison]::OrdinalIgnoreCase)) {
        return $PathText
    }

    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($pathUri).ToString()).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
}

function Copy-RedCaseBin {
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
        throw "MES RedCase bin not found or inaccessible: $sourcePath. $($_.Exception.Message)"
    }

    if (-not $sourceExists) {
        throw "MES RedCase bin not found: $sourcePath"
    }

    $sourceInfo = Get-Item -LiteralPath $sourcePath
    if ($sourceInfo.Length -le 0) {
        throw "MES RedCase bin is empty: $sourcePath"
    }

    if (-not (Test-Path -LiteralPath $LocalDir)) {
        New-Item -ItemType Directory -Path $LocalDir | Out-Null
    }

    $targetPath = Join-Path $LocalDir $LocalFileName
    if (Test-Path -LiteralPath $targetPath -PathType Leaf) {
        $backupDir = Join-Path $LocalDir "Backup"
        if (-not (Test-Path -LiteralPath $backupDir)) {
            New-Item -ItemType Directory -Path $backupDir | Out-Null
        }

        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss_fff"
        $backupName = "{0}_{1}{2}" -f [System.IO.Path]::GetFileNameWithoutExtension($LocalFileName), $timestamp, [System.IO.Path]::GetExtension($LocalFileName)
        Copy-Item -LiteralPath $targetPath -Destination (Join-Path $backupDir $backupName) -Force
    }

    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force

    $targetInfo = Get-Item -LiteralPath $targetPath
    $targetInfo.LastWriteTimeUtc = $sourceInfo.LastWriteTimeUtc
    $targetInfo = Get-Item -LiteralPath $targetPath
    if ($targetInfo.Length -le 0) {
        throw "Copied RedCase bin is empty: $targetPath"
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

function Get-RedCaseBinFile {
    param(
        [string]$MesDir,
        [string[]]$ConfiguredFirmwareFiles = @()
    )

    if ($ConfiguredFirmwareFiles.Count -gt 0) {
        if ($ConfiguredFirmwareFiles.Count -gt 1) {
            throw "RedCase FirmwareFiles must contain exactly one bin file."
        }

        $path = Join-Path $MesDir $ConfiguredFirmwareFiles[0]
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Configured RedCase bin file not found: $path"
        }

        return Get-Item -LiteralPath $path
    }

    $files = @(Get-ChildItem -LiteralPath $MesDir -File -Filter "*.bin" | Sort-Object Name)
    if ($files.Count -eq 0) {
        throw "No RedCase bin file found in MES directory: $MesDir"
    }

    if ($files.Count -gt 1) {
        throw "More than one RedCase bin file found in MES directory: $MesDir"
    }

    return $files[0]
}

function Get-ConfiguredFirmwareFiles {
    param(
        [object]$Object
    )

    if ($null -eq $Object -or $null -eq $Object.PSObject.Properties["FirmwareFiles"]) {
        return @()
    }

    return @($Object.FirmwareFiles |
        ForEach-Object { [string]$_ } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
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
    param([string]$TargetPath)

    if ((Test-Path -LiteralPath $TargetPath) -and ((Get-Item -LiteralPath $TargetPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Unsafe RedCase firmware cache reparse point: $TargetPath"
    }
    if (Test-Path -LiteralPath $TargetPath -PathType Container) {
        $linkedEntry = Get-ChildItem -LiteralPath $TargetPath -Force -Recurse |
            Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint } |
            Select-Object -First 1
        if ($null -ne $linkedEntry) {
            throw "Unsafe RedCase firmware cache reparse point: $($linkedEntry.FullName)"
        }
    }
}

function Initialize-OwnedRedCaseCache {
    param([string]$LocalDir, [string]$MesDir, [string]$ConfigDir)

    $fullLocalDir = [IO.Path]::GetFullPath($LocalDir).TrimEnd('\', '/')
    $allowedPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "Firmware_Local")).TrimEnd('\', '/')
    if (-not [string]::Equals($fullLocalDir, $allowedPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe RedCase LocalFirmwarePath; expected fixed Firmware_Local cache: $allowedPath"
    }
    if ([string]::Equals($fullLocalDir, [IO.Path]::GetFullPath($ConfigDir).TrimEnd('\', '/'), [StringComparison]::OrdinalIgnoreCase) -or
        [string]::Equals($fullLocalDir, [IO.Path]::GetFullPath($MesDir).TrimEnd('\', '/'), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe RedCase LocalFirmwarePath overlaps config or MES directory: $fullLocalDir"
    }

    Assert-NoReparsePoints -TargetPath $fullLocalDir
    if (-not (Test-Path -LiteralPath $fullLocalDir)) {
        New-Item -ItemType Directory -Path $fullLocalDir | Out-Null
    }
    $markerPath = Join-Path $fullLocalDir ".rfp-firmware-cache"
    $entries = @(Get-ChildItem -LiteralPath $fullLocalDir -Force)
    if ($entries.Count -gt 0 -and -not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        throw "RedCase firmware cache ownership marker is missing: $markerPath"
    }
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        [IO.File]::WriteAllText($markerPath, "RfpTestStation firmware cache`r`n", [Text.UTF8Encoding]::new($false))
    }
}

$configFullPath = [System.IO.Path]::GetFullPath($ConfigPath)
if (-not (Test-Path -LiteralPath $configFullPath -PathType Leaf)) {
    throw "Config file not found: $configFullPath"
}

$configDir = Split-Path -Parent $configFullPath
$config = Get-Content -LiteralPath $configFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
$params = Get-RequiredProperty -Object (Get-RequiredProperty -Object $config -Name "Burn2" -Context "Config.json") -Name "Params" -Context "Burn2"

$mesDir = Resolve-ConfiguredPath -BaseDir $configDir -PathText ([string](Get-RequiredProperty -Object $params -Name "MesFirmwarePath" -Context "Burn2.Params"))
$localDir = Resolve-ConfiguredPath -BaseDir $configDir -PathText ([string](Get-RequiredProperty -Object $params -Name "LocalFirmwarePath" -Context "Burn2.Params"))

try {
    $mesDirExists = Test-Path -LiteralPath $mesDir -PathType Container -ErrorAction Stop
}
catch {
    throw "MES RedCase directory not found or inaccessible: $mesDir. $($_.Exception.Message)"
}

if (-not $mesDirExists) {
    throw "MES RedCase directory not found: $mesDir"
}

$configuredFirmwareFiles = @(Get-ConfiguredFirmwareFiles -Object $params)
$binFile = Get-RedCaseBinFile -MesDir $mesDir -ConfiguredFirmwareFiles $configuredFirmwareFiles
$mesFileName = $binFile.Name
$localFileName = $binFile.Name
$localBinPath = [System.IO.Path]::GetFullPath((Join-Path $localDir $localFileName))
$configuredBinPath = Convert-ToRelativePath -BaseDir $configDir -PathText $localBinPath
$currentBinPath = ""
if ($null -ne $params.PSObject.Properties["BinFilePath"]) {
    $currentBinPath = [string]$params.BinFilePath
}

Initialize-OwnedRedCaseCache -LocalDir $localDir -MesDir $mesDir -ConfigDir $configDir

if ((Test-FileCurrent -SourceInfo $binFile -TargetPath $localBinPath) -and
    [string]::Equals($currentBinPath, $configuredBinPath, [StringComparison]::OrdinalIgnoreCase)) {
    Write-PreparedBinPathFile -OutputBinPathFile $OutputBinPathFile -LocalBinPath $localBinPath

    Write-Host "[INFO] Prepared RedCase bin already current."
    Write-Host "[INFO] MES=$mesDir"
    Write-Host "[INFO] LocalBin=$localBinPath"
    return
}

Clear-LocalFirmwareDirectory -LocalDir $localDir -MesDir $mesDir -ConfigDir $configDir -Context "RedCase"

$localBinPath = Copy-RedCaseBin -MesDir $mesDir -LocalDir $localDir -MesFileName $mesFileName -LocalFileName $localFileName

$params.BinFilePath = Convert-ToRelativePath -BaseDir $configDir -PathText $localBinPath
$configJson = $config | ConvertTo-Json -Depth 20
[System.IO.File]::WriteAllText($configFullPath, $configJson, [System.Text.UTF8Encoding]::new($true))

Write-PreparedBinPathFile -OutputBinPathFile $OutputBinPathFile -LocalBinPath $localBinPath

Write-Host "[INFO] Prepared RedCase bin"
Write-Host "[INFO] MES=$mesDir"
Write-Host "[INFO] LocalBin=$localBinPath"
