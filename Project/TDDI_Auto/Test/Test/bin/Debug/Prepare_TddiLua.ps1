param(
    [Parameter(Mandatory = $true)]
    [string]$ConfigPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputLuaPathFile
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

$configFullPath = [System.IO.Path]::GetFullPath($ConfigPath)
if (-not (Test-Path -LiteralPath $configFullPath -PathType Leaf)) {
    throw "Config file not found: $configFullPath"
}

$configDir = Split-Path -Parent $configFullPath
$config = Get-Content -LiteralPath $configFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
$params = Get-RequiredProperty -Object (Get-RequiredProperty -Object $config -Name "Burn3" -Context "Config.json") -Name "Params" -Context "Burn3"
$mesDir = Resolve-ConfiguredPath -BaseDir $configDir -PathText ([string](Get-RequiredProperty -Object $params -Name "MesFirmwarePath" -Context "Burn3.Params"))

try {
    $mesDirExists = Test-Path -LiteralPath $mesDir -PathType Container -ErrorAction Stop
}
catch {
    throw "MES TDDI directory not found or inaccessible: $mesDir. $($_.Exception.Message)"
}

if (-not $mesDirExists) {
    throw "MES TDDI directory not found: $mesDir"
}

$binFiles = @(Get-ChildItem -LiteralPath $mesDir -File -Filter "*.bin" | Sort-Object Name)
if ($binFiles.Count -eq 0) {
    throw "No TDDI bin file found in MES directory: $mesDir"
}

if ($binFiles.Count -gt 1) {
    throw "More than one TDDI bin file found in MES directory: $mesDir"
}

$luaPathForDevice = "\Demo/" + [System.IO.Path]::GetFileNameWithoutExtension($binFiles[0].Name) + ".lua"
$outputDir = Split-Path -Parent $OutputLuaPathFile
if ($outputDir -and -not (Test-Path -LiteralPath $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

Set-Content -LiteralPath $OutputLuaPathFile -Value $luaPathForDevice -Encoding ASCII

Write-Host "[INFO] Prepared TDDI lua"
Write-Host "[INFO] MES=$mesDir"
Write-Host "[INFO] Bin=$($binFiles[0].FullName)"
Write-Host "[INFO] Lua=$luaPathForDevice"
