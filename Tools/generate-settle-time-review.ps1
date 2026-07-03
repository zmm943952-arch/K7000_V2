param(
    [string]$TestPlanPath = "Runtime\TestPlans\Rfp7000V2.testplan.json",
    [string]$OutputPath = "docs\validation\settle-time-review.md"
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

function To-Array {
    param([object]$Value)

    if ($null -eq $Value) {
        return @()
    }

    if ($Value -is [System.Array]) {
        return $Value
    }

    return @($Value)
}

function Format-RelativePath {
    param([string]$PathText)

    if ($PathText.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $PathText.Substring($repoRoot.Length).TrimStart('\', '/').Replace('\', '/')
    }

    return $PathText.Replace('\', '/')
}

$plan = Get-Content -Path $TestPlanPath -Raw | ConvertFrom-Json
$events = @()

foreach ($item in (To-Array $plan.items)) {
    $itemId = [string](Get-PropValue $item "id")
    $parameters = Get-PropValue $item "parameters"
    $itemSettle = Get-PropValue $parameters "settleMs"
    if ($null -ne $itemSettle -and [int]$itemSettle -gt 0) {
        $events += [pscustomobject]@{
            Scope = $itemId
            Level = "Item"
            SettleMs = [int]$itemSettle
            Review = "Confirm this cannot be moved to a narrower check or removed."
        }
    }

    foreach ($child in (To-Array (Get-PropValue $parameters "items"))) {
        $childId = [string](Get-PropValue $child "id")
        $childScope = if ([string]::IsNullOrWhiteSpace($childId)) { $itemId + "/child" } else { $itemId + "/" + $childId }
        $childSettle = Get-PropValue $child "settleMs"
        if ($null -ne $childSettle -and [int]$childSettle -gt 0) {
            $events += [pscustomobject]@{
                Scope = $childScope
                Level = "Child"
                SettleMs = [int]$childSettle
                Review = "Confirm child-level wait is needed for every check."
            }
        }

        foreach ($check in (To-Array (Get-PropValue $child "checks"))) {
            $checkName = [string](Get-PropValue $check "name")
            $checkScope = if ([string]::IsNullOrWhiteSpace($checkName)) { $childScope + "/check" } else { $childScope + "/" + $checkName }
            $checkSettle = Get-PropValue $check "settleMs"
            if ($null -ne $checkSettle -and [int]$checkSettle -gt 0) {
                $events += [pscustomobject]@{
                    Scope = $checkScope
                    Level = "Check"
                    SettleMs = [int]$checkSettle
                    Review = "Needs hardware confirmation before reducing."
                }
            }
        }
    }
}

$total = ($events | Measure-Object -Property SettleMs -Sum).Sum
if ($null -eq $total) {
    $total = 0
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Settle Time Review")
$lines.Add("")
$lines.Add("Generated from ``$(Format-RelativePath $TestPlanPath)``.")
$lines.Add("")
$lines.Add("## Summary")
$lines.Add("")
$lines.Add("- Explicit settleMs count: $($events.Count)")
$lines.Add("- Explicit settleMs total: $total ms ($([string]::Format('{0:n1}', $total / 1000.0)) seconds)")
$lines.Add("- Rule: do not reduce settleMs until signal/device response data is recorded.")
$lines.Add("")
$lines.Add("## Review Table")
$lines.Add("")
$lines.Add("| Scope | Level | Current settleMs | Hardware Confirmed | Suggested settleMs | Notes |")
$lines.Add("| --- | --- | ---: | --- | --- | --- |")

foreach ($event in ($events | Sort-Object @{Expression = "SettleMs"; Descending = $true}, @{Expression = "Scope"; Descending = $false})) {
    $lines.Add("| $($event.Scope) | $($event.Level) | $($event.SettleMs) | No | TBD | $($event.Review) |")
}

$lines.Add("")
$lines.Add("## Hardware Timing Instructions")
$lines.Add("")
$lines.Add("1. Scope or log the signal/device response for every 5000 ms wait first.")
$lines.Add("2. Record stable response time and margin before reducing a wait.")
$lines.Add("3. If multiple checks share the same physical settling condition, consider moving the wait to group level after hardware confirmation.")
$lines.Add("4. Re-run `Tools/analyze-testplan.ps1 -MarkdownPath docs\\validation\\testplan-optimization-report.md` after any change.")

Set-Content -Path $OutputPath -Value $lines -Encoding UTF8

Write-Host "SETTLE TIME REVIEW"
Write-Host "Plan: $($plan.name) v$($plan.version)"
Write-Host "Explicit settleMs count: $($events.Count)"
Write-Host "Explicit settleMs total: $total ms"
Write-Host "Markdown report: $OutputPath"
