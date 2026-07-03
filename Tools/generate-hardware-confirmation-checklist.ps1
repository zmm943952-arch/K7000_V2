param(
    [string]$TestPlanPath = "Runtime\TestPlans\Rfp7000V2.testplan.json",
    [string]$OutputPath = "docs\validation\testplan-hardware-confirmation-checklist.md"
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
$items = To-Array $plan.items
$flashItems = @($items | Where-Object { [string](Get-PropValue $_ "kind") -eq "Flash" })
$settleEvents = @()
$i2cEvents = @()
$continueOnFailureEntries = @()

foreach ($item in $items) {
    $id = [string](Get-PropValue $item "id")
    $parameters = Get-PropValue $item "parameters"
    $address = [string](Get-PropValue $parameters "address")
    $readLength = Get-PropValue $parameters "readLength"
    $readRegister = Get-PropValue $parameters "readRegister"
    $stopOnFailure = Get-PropValue $item "stopOnFailure"

    if ($stopOnFailure -ne $true) {
        $continueOnFailureEntries += $id
    }

    $settleMs = Get-PropValue $parameters "settleMs"
    if ($null -ne $settleMs -and [int]$settleMs -gt 0) {
        $settleEvents += [pscustomobject]@{ Scope = $id; SettleMs = [int]$settleMs }
    }

    if (-not [string]::IsNullOrWhiteSpace($address)) {
        $i2cEvents += [pscustomobject]@{
            Scope = $id
            Signature = "$address|readLength=$readLength|readRegister=$readRegister"
        }
    }

    foreach ($child in (To-Array (Get-PropValue $parameters "items"))) {
        $childId = [string](Get-PropValue $child "id")
        $childScope = if ([string]::IsNullOrWhiteSpace($childId)) { $id + "/child" } else { $id + "/" + $childId }
        foreach ($check in (To-Array (Get-PropValue $child "checks"))) {
            $checkName = [string](Get-PropValue $check "name")
            $checkScope = if ([string]::IsNullOrWhiteSpace($checkName)) { $childScope + "/check" } else { $childScope + "/" + $checkName }
            $checkSettleMs = Get-PropValue $check "settleMs"
            if ($null -ne $checkSettleMs -and [int]$checkSettleMs -gt 0) {
                $settleEvents += [pscustomobject]@{ Scope = $checkScope; SettleMs = [int]$checkSettleMs }
            }

            $writeData = [string](Get-PropValue $check "writeData")
            if (-not [string]::IsNullOrWhiteSpace($address) -or -not [string]::IsNullOrWhiteSpace($writeData)) {
                $i2cEvents += [pscustomobject]@{
                    Scope = $checkScope
                    Signature = "$address|readLength=$readLength|readRegister=$readRegister|writeData=$writeData"
                }
            }
        }
    }
}

$i2cGroups = @($i2cEvents | Group-Object Signature | Where-Object { $_.Count -gt 1 } | Sort-Object Count -Descending)
$settleTotal = ($settleEvents | Measure-Object -Property SettleMs -Sum).Sum
if ($null -eq $settleTotal) {
    $settleTotal = 0
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Testplan Hardware Confirmation Checklist")
$lines.Add("")
$lines.Add("Generated from ``$(Format-RelativePath $TestPlanPath)``.")
$lines.Add("")
$lines.Add("## Summary")
$lines.Add("")
$lines.Add("- No-Hardware Completed: Mock validation, report generation, static testplan sync, group power guard, and audit report generation.")
$lines.Add("- Hardware Required: Flash timeout tuning, settleMs reduction, I2C read merge confirmation, and final station cycle-time measurement.")
$lines.Add("- Flash steps: $($flashItems.Count)")
$lines.Add("- Explicit settleMs checks: $($settleEvents.Count); total $settleTotal ms")
$lines.Add("- Repeated I2C signatures: $($i2cGroups.Count)")
$lines.Add("- Continue-on-failure entries: $($continueOnFailureEntries.Count)")
$lines.Add("")
$lines.Add("## Checklist")
$lines.Add("")
$lines.Add("| Area | Status | Evidence | Hardware Action |")
$lines.Add("| --- | --- | --- | --- |")
$lines.Add("| Flash Timeout | Hardware Required | See ``docs/validation/flash-timeout-review.md``. | Measure actual duration for all flash scripts and set suggested timeout with margin. |")
$lines.Add("| Settle Time | Hardware Required | See ``docs/validation/settle-time-review.md``. | Measure signal/device settle time before reducing any explicit wait. |")
$lines.Add("| I2C Reuse | Hardware Required | See ``docs/validation/i2c-reuse-review.md``. | Confirm repeated signatures read the same physical state before executor-level merge. |")
$lines.Add("| Stop Policy | No-Hardware Completed | See ``docs/validation/stop-policy-review.md``. | Reconfirm only result output and cleanup should continue after failure. |")
$lines.Add("| Shared Power | No-Hardware Completed | Group-level 3.3 V and 12.2 V reuse is guarded by static tests. | Spot-check relay/power rail stability on real fixture. |")
$lines.Add("| Reporting | No-Hardware Completed | Mock validation checks CSV expected value, comparison type, sent, reply, and reason fields. | Run one real failed DUT and confirm production log traceability. |")
$lines.Add("")
$lines.Add("## Hardware Run Order")
$lines.Add("")
$lines.Add("1. Run the current testplan once without changing timing values and record total cycle time.")
$lines.Add("2. Fill actual/max/suggested values in Flash and settleMs review tables.")
$lines.Add("3. Validate I2C merge candidates one group at a time; do not merge reads that cross a state change.")
$lines.Add("4. Re-run ``.\Tools\run-testplan-optimization-audit.ps1`` and full ``dotnet test`` after each accepted timing or executor change.")

Set-Content -Path $OutputPath -Value $lines -Encoding UTF8

Write-Host "HARDWARE CONFIRMATION CHECKLIST"
Write-Host "Plan: $($plan.name) v$($plan.version)"
Write-Host "Flash steps: $($flashItems.Count)"
Write-Host "Explicit settleMs checks: $($settleEvents.Count)"
Write-Host "Repeated I2C signatures: $($i2cGroups.Count)"
Write-Host "Continue-on-failure entries: $($continueOnFailureEntries.Count)"
Write-Host "Markdown report: $OutputPath"
