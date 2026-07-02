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
    if ($targetInfo.Length -le 0) {
        throw "Copied RedCase bin is empty: $targetPath"
    }

    return [System.IO.Path]::GetFullPath($targetPath)
}

function Get-RedCaseBinFile {
    param(
        [string]$MesDir
    )

    $files = @(Get-ChildItem -LiteralPath $MesDir -File -Filter "*.bin" | Sort-Object Name)
    if ($files.Count -eq 0) {
        throw "No RedCase bin file found in MES directory: $MesDir"
    }

    if ($files.Count -gt 1) {
        throw "More than one RedCase bin file found in MES directory: $MesDir"
    }

    return $files[0]
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

Clear-LocalFirmwareDirectory -LocalDir $localDir -MesDir $mesDir -ConfigDir $configDir -Context "RedCase"

$binFile = Get-RedCaseBinFile -MesDir $mesDir
$mesFileName = $binFile.Name
$localFileName = $binFile.Name

$localBinPath = Copy-RedCaseBin -MesDir $mesDir -LocalDir $localDir -MesFileName $mesFileName -LocalFileName $localFileName

$params.BinFilePath = $localBinPath
$configJson = $config | ConvertTo-Json -Depth 20
[System.IO.File]::WriteAllText($configFullPath, $configJson, [System.Text.UTF8Encoding]::new($true))

$outputDir = Split-Path -Parent $OutputBinPathFile
if ($outputDir -and -not (Test-Path -LiteralPath $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}
Set-Content -LiteralPath $OutputBinPathFile -Value $localBinPath -Encoding ASCII

Write-Host "[INFO] Prepared RedCase bin"
Write-Host "[INFO] MES=$mesDir"
Write-Host "[INFO] LocalBin=$localBinPath"
