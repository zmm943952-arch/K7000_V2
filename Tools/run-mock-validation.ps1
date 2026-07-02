param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$testProject = Join-Path $repoRoot "src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj"

if (-not (Test-Path $testProject)) {
    throw "Cannot find test project: $testProject"
}

Write-Host "Running mock validation..."
Write-Host "Project: $testProject"

dotnet test $testProject `
    --configuration $Configuration `
    --filter "FullyQualifiedName~MockValidationTests" `
    --logger "console;verbosity=normal"

Write-Host "Mock validation completed."
