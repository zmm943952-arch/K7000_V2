param(
    [string]$OutputDirectory = "docs\validation",
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

if (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot $OutputDirectory
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    Write-Host ""
    Write-Host "== $Name =="
    & $Action
}

Write-Host "TESTPLAN OPTIMIZATION AUDIT"
Write-Host "Output directory: $OutputDirectory"

Push-Location $repoRoot
try {
    Invoke-Step "Testplan analysis" {
        & (Join-Path $scriptRoot "analyze-testplan.ps1") `
            -MarkdownPath (Join-Path $OutputDirectory "testplan-optimization-report.md")
    }

    Invoke-Step "Flash timeout review" {
        & (Join-Path $scriptRoot "generate-flash-timeout-review.ps1") `
            -OutputPath (Join-Path $OutputDirectory "flash-timeout-review.md")
    }

    Invoke-Step "Settle time review" {
        & (Join-Path $scriptRoot "generate-settle-time-review.ps1") `
            -OutputPath (Join-Path $OutputDirectory "settle-time-review.md")
    }

    Invoke-Step "I2C reuse review" {
        & (Join-Path $scriptRoot "generate-i2c-reuse-review.ps1") `
            -OutputPath (Join-Path $OutputDirectory "i2c-reuse-review.md")
    }

    Invoke-Step "Stop policy review" {
        & (Join-Path $scriptRoot "generate-stop-policy-review.ps1") `
            -OutputPath (Join-Path $OutputDirectory "stop-policy-review.md")
    }

    Invoke-Step "Mock validation" {
        & (Join-Path $scriptRoot "run-mock-validation.ps1") `
            -Configuration $Configuration
    }
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "Optimization audit completed."
