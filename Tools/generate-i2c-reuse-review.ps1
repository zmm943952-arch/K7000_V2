param(
    [string]$TestPlanPath = "Runtime\TestPlans\Rfp7000V2.testplan.json",
    [string]$OutputPath = "docs\validation\i2c-reuse-review.md"
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
    $address = [string](Get-PropValue $parameters "address")
    $readLength = Get-PropValue $parameters "readLength"
    $readRegister = Get-PropValue $parameters "readRegister"

    if (-not [string]::IsNullOrWhiteSpace($address)) {
        $events += [pscustomobject]@{
            Scope = $itemId
            Signature = "$address|readLength=$readLength|readRegister=$readRegister"
        }
    }

    foreach ($child in (To-Array (Get-PropValue $parameters "items"))) {
        $childId = [string](Get-PropValue $child "id")
        $childScope = if ([string]::IsNullOrWhiteSpace($childId)) { $itemId + "/child" } else { $itemId + "/" + $childId }

        foreach ($check in (To-Array (Get-PropValue $child "checks"))) {
            $checkName = [string](Get-PropValue $check "name")
            $checkScope = if ([string]::IsNullOrWhiteSpace($checkName)) { $childScope + "/check" } else { $childScope + "/" + $checkName }
            $writeData = [string](Get-PropValue $check "writeData")
            if (-not [string]::IsNullOrWhiteSpace($address) -or -not [string]::IsNullOrWhiteSpace($writeData)) {
                $events += [pscustomobject]@{
                    Scope = $checkScope
                    Signature = "$address|readLength=$readLength|readRegister=$readRegister|writeData=$writeData"
                }
            }
        }
    }
}

$groups = @($events | Group-Object Signature | Where-Object { $_.Count -gt 1 } | Sort-Object Count -Descending)

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# I2C Reuse Review")
$lines.Add("")
$lines.Add("Generated from ``$(Format-RelativePath $TestPlanPath)``.")
$lines.Add("")
$lines.Add("## Summary")
$lines.Add("")
$lines.Add("- I2C event count: $($events.Count)")
$lines.Add("- Repeated signature count: $($groups.Count)")
$lines.Add("- Rule: do not merge I2C reads until hardware confirms product state is unchanged between candidate reads.")
$lines.Add("")
$lines.Add("## Review Table")
$lines.Add("")
$lines.Add("| Signature | Occurrences | Merge Candidate | Hardware Confirmed | Notes |")
$lines.Add("| --- | ---: | --- | --- | --- |")

foreach ($group in $groups) {
    $candidate = if ($group.Count -ge 4) { "Yes" } else { "Maybe" }
    $examples = ($group.Group | Select-Object -First 4 | ForEach-Object { $_.Scope }) -join "<br>"
    $lines.Add("| ``$($group.Name)`` | $($group.Count) | $candidate | No | Examples:<br>$examples |")
}

$lines.Add("")
$lines.Add("## Hardware Confirmation Instructions")
$lines.Add("")
$lines.Add("1. Confirm each candidate read happens after the DUT state has settled.")
$lines.Add("2. Confirm no relay, power rail, or writeData changes make a later read intentionally different.")
$lines.Add("3. Only merge reads inside the executor after the repeated signature maps to the same physical state.")
$lines.Add("4. Re-run Mock validation and hardware spot checks after any I2C merge.")

Set-Content -Path $OutputPath -Value $lines -Encoding UTF8

Write-Host "I2C REUSE REVIEW"
Write-Host "Plan: $($plan.name) v$($plan.version)"
Write-Host "I2C event count: $($events.Count)"
Write-Host "Repeated signature count: $($groups.Count)"
Write-Host "Markdown report: $OutputPath"
