param(
    [string]$Version = (Get-Date -Format "yyyyMMdd_HHmmss"),
    [string]$Configuration = "Release",
    [string]$Platform = "x86"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$projectPath = Join-Path $repoRoot "src\RfpTestStation\RfpTestStation.App\RfpTestStation.App.csproj"
$buildOutput = Join-Path $repoRoot "src\RfpTestStation\RfpTestStation.App\bin\$Platform\$Configuration\net48"
$deployRoot = Join-Path $repoRoot "Deploy"
$packageRoot = Join-Path $deployRoot ("RfpTestStation_" + $Version)
$runtimeSource = Join-Path $repoRoot "Runtime"
$runtimeTarget = Join-Path $packageRoot "Runtime"
$reportsTarget = Join-Path $packageRoot "Reports"

if (-not (Test-Path $projectPath)) {
    throw "Cannot find app project: $projectPath"
}

if (-not (Test-Path $runtimeSource)) {
    throw "Cannot find Runtime folder: $runtimeSource"
}

dotnet build $projectPath -c $Configuration -p:Platform=$Platform

if (-not (Test-Path (Join-Path $buildOutput "RfpTestStation.App.exe"))) {
    throw "Build output does not contain RfpTestStation.App.exe: $buildOutput"
}

New-Item -ItemType Directory -Force -Path $deployRoot | Out-Null
if (Test-Path $packageRoot) {
    Remove-Item -LiteralPath $packageRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $packageRoot, $reportsTarget | Out-Null
Copy-Item -Path (Join-Path $buildOutput "*") -Destination $packageRoot -Recurse -Force
Copy-Item -Path $runtimeSource -Destination $runtimeTarget -Recurse -Force

$readme = @"
RFP Test Station deployment package

How to deploy:
1. Copy this whole RfpTestStation folder to the target PC.
2. Run RfpTestStation.App.exe.
3. Edit Runtime\Config\Config.json for station-specific COM ports, IP, scripts, and firmware.
4. Test logs and reports are written to Reports.

Do not copy files from the legacy Project folder for normal deployment.
"@

Set-Content -Path (Join-Path $packageRoot "README.txt") -Value $readme -Encoding UTF8

Write-Host "Package created: $packageRoot"
Write-Host "Entry point: $(Join-Path $packageRoot 'RfpTestStation.App.exe')"
