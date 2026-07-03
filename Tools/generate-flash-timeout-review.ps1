param(
    [string]$TestPlanPath = "Runtime\TestPlans\Rfp7000V2.testplan.json",
    [string]$OutputPath = "docs\validation\flash-timeout-review.md"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

if (-not [System.IO.Path]::IsPathRooted($TestPlanPath)) {
    $TestPlanPath = Join-Path $repoRoot $TestPlanPath
}

if (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $repoRoot $OutputPath
}

if (-not (Test-Path $TestPlanPath)) {
    throw "Cannot find testplan: $TestPlanPath"
}

function Get-PropValue {
    param(
        [object]$Object,
        [string]$Name
    )

    if ($null -eq $Object) {
        return $null
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Resolve-RepoPath {
    param([string]$PathText)

    if ([string]::IsNullOrWhiteSpace($PathText)) {
        return ""
    }

    if ([System.IO.Path]::IsPathRooted($PathText)) {
        return $PathText
    }

    return Join-Path $repoRoot $PathText
}

function Format-RelativePath {
    param([string]$PathText)

    if ([string]::IsNullOrWhiteSpace($PathText)) {
        return ""
    }

    if ($PathText.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $PathText.Substring($repoRoot.Length).TrimStart('\', '/').Replace('\', '/')
    }

    return $PathText.Replace('\', '/')
}

$plan = Get-Content -Path $TestPlanPath -Raw | ConvertFrom-Json
$flashItems = @($plan.items | Where-Object { $_.kind -eq "Flash" })
$timeoutTotal = ($flashItems | Measure-Object -Property timeoutSeconds -Sum).Sum
if ($null -eq $timeoutTotal) {
    $timeoutTotal = 0
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Flash Timeout Review")
$lines.Add("")
$lines.Add("Generated from ``$(Format-RelativePath $TestPlanPath)``.")
$lines.Add("")
$lines.Add("## Summary")
$lines.Add("")
$lines.Add("- Flash step count: $($flashItems.Count)")
$lines.Add("- Current flash timeout total: $timeoutTotal seconds")
$lines.Add("- Rule: do not reduce timeout until real hardware duration data is recorded.")
$lines.Add("")
$lines.Add("## Review Table")
$lines.Add("")
$lines.Add("| Step ID | Name | Flash Kind | Script | Script Exists | Current Timeout Seconds | Actual Duration Seconds | Max Observed Seconds | Suggested Timeout Seconds | Hardware Confirmed | Notes |")
$lines.Add("| --- | --- | --- | --- | --- | ---: | --- | --- | --- | --- | --- |")

foreach ($item in $flashItems) {
    $parameters = Get-PropValue $item "parameters"
    $script = [string](Get-PropValue $parameters "script")
    $scriptPath = Resolve-RepoPath $script
    $scriptExists = if (-not [string]::IsNullOrWhiteSpace($scriptPath) -and (Test-Path $scriptPath)) { "Yes" } else { "No" }
    $flashKind = [string](Get-PropValue $parameters "flashKind")

    $lines.Add("| $($item.id) | $($item.name) | $flashKind | ``$(Format-RelativePath $script)`` | $scriptExists | $($item.timeoutSeconds) | TBD | TBD | TBD | No | Need hardware timing data. |")
}

$lines.Add("")
$lines.Add("## Hardware Timing Instructions")
$lines.Add("")
$lines.Add("1. Run each flash step at least 10 times on representative hardware.")
$lines.Add("2. Record actual duration and max observed duration.")
$lines.Add("3. Suggested timeout should include margin for slow station PC, retry behavior, and firmware size variance.")
$lines.Add("4. Update testplan timeout only after hardware confirmation is recorded in this file.")

Set-Content -Path $OutputPath -Value $lines -Encoding UTF8

Write-Host "FLASH TIMEOUT REVIEW"
Write-Host "Plan: $($plan.name) v$($plan.version)"
Write-Host "Flash steps: $($flashItems.Count)"
Write-Host "Current flash timeout total: $timeoutTotal seconds"
Write-Host "Markdown report: $OutputPath"
