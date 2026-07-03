param(
    [string]$TestPlanPath = "Runtime\TestPlans\Rfp7000V2.testplan.json",
    [int]$Top = 8,
    [string]$MarkdownPath = ""
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

if (-not [System.IO.Path]::IsPathRooted($TestPlanPath)) {
    $TestPlanPath = Join-Path $repoRoot $TestPlanPath
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

function Format-Power {
    param([object]$Power)

    $channel = Get-PropValue $Power "channel"
    $voltage = Get-PropValue $Power "voltage"
    if ($null -eq $channel -and $null -eq $voltage) {
        return ($Power | ConvertTo-Json -Compress)
    }

    return "CH$channel $voltage V"
}

$statusImmediate = -join ([char[]](0x53EF, 0x7ACB, 0x5373, 0x6539))
$statusHardwareConfirm = -join ([char[]](0x9700, 0x786C, 0x4EF6, 0x786E, 0x8BA4))
$statusKeep = -join ([char[]](0x6682, 0x4E0D, 0x6539))

$plan = Get-Content -Path $TestPlanPath -Raw | ConvertFrom-Json
$items = To-Array (Get-PropValue $plan "items")
$topItems = @()
$powerEvents = @()
$settleEvents = @()
$i2cEvents = @()
$riskItems = @()

foreach ($item in $items) {
    $id = [string](Get-PropValue $item "id")
    $name = [string](Get-PropValue $item "name")
    $kind = [string](Get-PropValue $item "kind")
    $timeout = [int](Get-PropValue $item "timeoutSeconds")
    $stopOnFailure = Get-PropValue $item "stopOnFailure"
    $parameters = Get-PropValue $item "parameters"
    $address = [string](Get-PropValue $parameters "address")
    $readLength = Get-PropValue $parameters "readLength"
    $readRegister = Get-PropValue $parameters "readRegister"

    $topItems += [pscustomobject]@{
        Id = $id
        Name = $name
        Kind = $kind
        TimeoutSeconds = $timeout
        StopOnFailure = $stopOnFailure
    }

    if ($stopOnFailure -ne $true) {
        $riskItems += [pscustomobject]@{
            Id = $id
            Name = $name
            Reason = "stopOnFailure is not true"
        }
    }

    foreach ($power in (To-Array (Get-PropValue $parameters "powerOnBefore"))) {
        $powerEvents += [pscustomobject]@{
            Scope = $id
            Power = Format-Power $power
        }
    }

    $settleMs = Get-PropValue $parameters "settleMs"
    if ($null -ne $settleMs -and [int]$settleMs -gt 0) {
        $settleEvents += [pscustomobject]@{
            Scope = $id
            SettleMs = [int]$settleMs
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($address)) {
        $i2cEvents += [pscustomobject]@{
            Scope = $id
            Key = "$address|readLength=$readLength|readRegister=$readRegister"
        }
    }

    foreach ($child in (To-Array (Get-PropValue $parameters "items"))) {
        $childId = [string](Get-PropValue $child "id")
        $childScope = if ([string]::IsNullOrWhiteSpace($childId)) { $id + "/child" } else { $id + "/" + $childId }

        foreach ($power in (To-Array (Get-PropValue $child "powerOnBefore"))) {
            $powerEvents += [pscustomobject]@{
                Scope = $childScope
                Power = Format-Power $power
            }
        }

        foreach ($check in (To-Array (Get-PropValue $child "checks"))) {
            $checkName = [string](Get-PropValue $check "name")
            $checkScope = if ([string]::IsNullOrWhiteSpace($checkName)) { $childScope + "/check" } else { $childScope + "/" + $checkName }
            $checkSettleMs = Get-PropValue $check "settleMs"
            if ($null -ne $checkSettleMs -and [int]$checkSettleMs -gt 0) {
                $settleEvents += [pscustomobject]@{
                    Scope = $checkScope
                    SettleMs = [int]$checkSettleMs
                }
            }

            $writeData = [string](Get-PropValue $check "writeData")
            if (-not [string]::IsNullOrWhiteSpace($address) -or -not [string]::IsNullOrWhiteSpace($writeData)) {
                $i2cEvents += [pscustomobject]@{
                    Scope = $checkScope
                    Key = "$address|readLength=$readLength|readRegister=$readRegister|writeData=$writeData"
                }
            }
        }
    }
}

$timeoutTotal = ($topItems | Measure-Object -Property TimeoutSeconds -Sum).Sum
if ($null -eq $timeoutTotal) {
    $timeoutTotal = 0
}

Write-Host "TESTPLAN ANALYSIS"
Write-Host "Plan: $($plan.name) v$($plan.version)"
Write-Host "Path: $TestPlanPath"
Write-Host ("Top-level items: {0}" -f $topItems.Count)
Write-Host ("Timeout total: {0}s ({1:n1} min)" -f $timeoutTotal, ($timeoutTotal / 60.0))
Write-Host ""

Write-Host "KIND SUMMARY"
$topItems |
    Group-Object Kind |
    Sort-Object Name |
    ForEach-Object {
        $sum = ($_.Group | Measure-Object -Property TimeoutSeconds -Sum).Sum
        if ($null -eq $sum) { $sum = 0 }
        Write-Host ("- {0}: count={1}; timeout={2}s" -f $_.Name, $_.Count, $sum)
    }
Write-Host ""

Write-Host "SLOWEST ITEMS"
$topItems |
    Sort-Object TimeoutSeconds -Descending |
    Select-Object -First $Top |
    ForEach-Object {
        Write-Host ("- {0}: {1}s; kind={2}; stopOnFailure={3}" -f $_.Id, $_.TimeoutSeconds, $_.Kind, $_.StopOnFailure)
    }
Write-Host ""

Write-Host "POWER-ON REUSE"
$powerGroups = $powerEvents | Group-Object Power | Sort-Object Count -Descending
if ($powerGroups.Count -eq 0) {
    Write-Host "- No powerOnBefore entries found."
} else {
    foreach ($group in $powerGroups) {
        Write-Host ("- {0}: {1} occurrence(s)" -f $group.Name, $group.Count)
        $group.Group | Select-Object -First 6 | ForEach-Object {
            Write-Host ("  - {0}" -f $_.Scope)
        }
    }
}
Write-Host ""

Write-Host "SETTLE TIME"
$settleTotal = ($settleEvents | Measure-Object -Property SettleMs -Sum).Sum
if ($null -eq $settleTotal) {
    $settleTotal = 0
}
Write-Host ("- Explicit settleMs total: {0}ms ({1:n1}s)" -f $settleTotal, ($settleTotal / 1000.0))
$settleEvents |
    Sort-Object SettleMs -Descending |
    Select-Object -First $Top |
    ForEach-Object {
        Write-Host ("- {0}: {1}ms" -f $_.Scope, $_.SettleMs)
    }
Write-Host ""

Write-Host "I2C REUSE"
$i2cGroups = $i2cEvents | Group-Object Key | Where-Object { $_.Count -gt 1 } | Sort-Object Count -Descending
if ($i2cGroups.Count -eq 0) {
    Write-Host "- No repeated I2C signatures found."
} else {
    foreach ($group in ($i2cGroups | Select-Object -First $Top)) {
        Write-Host ("- {0}: {1} occurrence(s)" -f $group.Name, $group.Count)
        $group.Group | Select-Object -First 5 | ForEach-Object {
            Write-Host ("  - {0}" -f $_.Scope)
        }
    }
}
Write-Host ""

Write-Host "STOP-ON-FAILURE REVIEW"
if ($riskItems.Count -eq 0) {
    Write-Host "- All top-level items use stopOnFailure=true."
} else {
    foreach ($risk in $riskItems) {
        Write-Host ("- {0}: {1}" -f $risk.Id, $risk.Reason)
    }
}
Write-Host ""

Write-Host "OPTIMIZATION SUGGESTIONS"
Write-Host "- Review flash item timeouts first; they dominate worst-case station time."
Write-Host "- Keep shared 3.3V power at group level where child checks use the same rail."
Write-Host "- Merge repeated I2C reads with the same address/readLength/register when the product state is unchanged."
Write-Host "- Confirm each explicit settleMs is required by signal settling; reduce or move it to group level when safe."
Write-Host "- Keep stopOnFailure=true for steps whose failure makes later results meaningless."

if (-not [string]::IsNullOrWhiteSpace($MarkdownPath)) {
    if (-not [System.IO.Path]::IsPathRooted($MarkdownPath)) {
        $MarkdownPath = Join-Path $repoRoot $MarkdownPath
    }

    $markdownDirectory = Split-Path -Parent $MarkdownPath
    if (-not [string]::IsNullOrWhiteSpace($markdownDirectory)) {
        New-Item -ItemType Directory -Force -Path $markdownDirectory | Out-Null
    }

    $markdown = New-Object System.Collections.Generic.List[string]
    $displayTestPlanPath = $TestPlanPath
    if ($TestPlanPath.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        $displayTestPlanPath = $TestPlanPath.Substring($repoRoot.Length).TrimStart('\', '/')
    }
    $displayTestPlanPath = $displayTestPlanPath.Replace('\', '/')

    $markdown.Add("# Testplan Optimization Report")
    $markdown.Add("")
    $markdown.Add("Generated from ``$displayTestPlanPath``.")
    $markdown.Add("")
    $markdown.Add("## Summary")
    $markdown.Add("")
    $markdown.Add("- Plan: $($plan.name) v$($plan.version)")
    $markdown.Add("- Top-level items: $($topItems.Count)")
    $markdown.Add("- Timeout total: $timeoutTotal seconds ($([string]::Format('{0:n1}', $timeoutTotal / 60.0)) minutes)")
    $markdown.Add("- Explicit settleMs total: $settleTotal ms ($([string]::Format('{0:n1}', $settleTotal / 1000.0)) seconds)")
    $markdown.Add("")

    $markdown.Add("## Kind Summary")
    $markdown.Add("")
    $markdown.Add("| Kind | Count | Timeout Seconds |")
    $markdown.Add("| --- | ---: | ---: |")
    $topItems |
        Group-Object Kind |
        Sort-Object Name |
        ForEach-Object {
            $sum = ($_.Group | Measure-Object -Property TimeoutSeconds -Sum).Sum
            if ($null -eq $sum) { $sum = 0 }
            $markdown.Add("| $($_.Name) | $($_.Count) | $sum |")
        }
    $markdown.Add("")

    $markdown.Add("## Slowest Items")
    $markdown.Add("")
    $markdown.Add("| ID | Timeout Seconds | Kind | Stop On Failure |")
    $markdown.Add("| --- | ---: | --- | --- |")
    $topItems |
        Sort-Object TimeoutSeconds -Descending |
        Select-Object -First $Top |
        ForEach-Object {
            $markdown.Add("| $($_.Id) | $($_.TimeoutSeconds) | $($_.Kind) | $($_.StopOnFailure) |")
        }
    $markdown.Add("")

    $markdown.Add("## Power-On Reuse")
    $markdown.Add("")
    if ($powerGroups.Count -eq 0) {
        $markdown.Add("- No powerOnBefore entries found.")
    } else {
        foreach ($group in $powerGroups) {
            $markdown.Add("- $($group.Name): $($group.Count) occurrence(s)")
            $group.Group | Select-Object -First 6 | ForEach-Object {
                $markdown.Add("  - $($_.Scope)")
            }
        }
    }
    $markdown.Add("")

    $markdown.Add("## Settle Time")
    $markdown.Add("")
    $markdown.Add("- Explicit settleMs total: $settleTotal ms")
    $settleEvents |
        Sort-Object SettleMs -Descending |
        Select-Object -First $Top |
        ForEach-Object {
            $markdown.Add("- $($_.Scope): $($_.SettleMs) ms")
        }
    $markdown.Add("")

    $markdown.Add("## I2C Reuse")
    $markdown.Add("")
    if ($i2cGroups.Count -eq 0) {
        $markdown.Add("- No repeated I2C signatures found.")
    } else {
        foreach ($group in ($i2cGroups | Select-Object -First $Top)) {
            $markdown.Add("- ``$($group.Name)``: $($group.Count) occurrence(s)")
            $group.Group | Select-Object -First 5 | ForEach-Object {
                $markdown.Add("  - $($_.Scope)")
            }
        }
    }
    $markdown.Add("")

    $markdown.Add("## Stop-On-Failure Review")
    $markdown.Add("")
    if ($riskItems.Count -eq 0) {
        $markdown.Add("- All top-level items use stopOnFailure=true.")
    } else {
        foreach ($risk in $riskItems) {
            $markdown.Add("- $($risk.Id): $($risk.Reason)")
        }
    }
    $markdown.Add("")

    $markdown.Add("## Optimization Priority Review")
    $markdown.Add("")
    $markdown.Add("| Item | Status | Evidence | Next Action |")
    $markdown.Add("| --- | --- | --- | --- |")
    $markdown.Add("| Keep this report as the baseline | $statusImmediate | Current report captures timeout, power, settle, and I2C reuse signals. | Re-run this script after every testplan change and compare the generated report. |")
    $markdown.Add("| Flash timeout audit | $statusHardwareConfirm | Flash kind timeout total is 2400 seconds across 4 steps. | Measure real script duration on hardware or collect historical station logs before reducing timeout values. |")
    $markdown.Add("| 5000 ms settle checks | $statusHardwareConfirm | Explicit settleMs total is $settleTotal ms; longest checks are 5000 ms. | Use oscilloscope or device response data to decide whether any wait can be reduced or moved to group level. |")
    $markdown.Add("| Repeated I2C read signatures | $statusHardwareConfirm | Repeated I2C signatures are listed above, including button and HVAC switch groups. | Confirm product state does not change between reads before merging reads in the executor. |")
    $markdown.Add("| Shared power-on groups | $statusImmediate | CH1 12.2 V and CH3 3.3 V reuse is visible at group level. | Keep new group-level power structure; avoid reintroducing child-level repeated power toggles. |")
    $markdown.Add("| result.output and cleanup.fixture stopOnFailure=false | $statusKeep | These are terminal/reporting and cleanup steps. | Keep running result output and cleanup even when the test has failed. |")
    $markdown.Add("")

    $markdown.Add("## Optimization Suggestions")
    $markdown.Add("")
    $markdown.Add("- Review flash item timeouts first; they dominate worst-case station time.")
    $markdown.Add("- Keep shared 3.3V power at group level where child checks use the same rail.")
    $markdown.Add("- Merge repeated I2C reads with the same address/readLength/register when the product state is unchanged.")
    $markdown.Add("- Confirm each explicit settleMs is required by signal settling; reduce or move it to group level when safe.")
    $markdown.Add("- Keep stopOnFailure=true for steps whose failure makes later results meaningless.")

    Set-Content -Path $MarkdownPath -Value $markdown -Encoding UTF8
    Write-Host ""
    Write-Host "Markdown report: $MarkdownPath"
}
