param(
    [string]$TestPlanPath = "Runtime\TestPlans\Rfp7000V2.testplan.json",
    [string]$OutputPath = "docs\validation\stop-policy-review.md"
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

$allowedContinueReasons = @{
    "result.output" = "Result output must run so a failed unit still produces the production CSV/JSON/LOG record."
    "cleanup.fixture" = "Fixture cleanup must run after failures to leave relays and fixture outputs in a known state."
}

$plan = Get-Content -Path $TestPlanPath -Raw | ConvertFrom-Json
$items = To-Array $plan.items
$rows = @()
$violations = @()

foreach ($item in $items) {
    $id = [string](Get-PropValue $item "id")
    $name = [string](Get-PropValue $item "name")
    $kind = [string](Get-PropValue $item "kind")
    $stopOnFailure = Get-PropValue $item "stopOnFailure"
    $allowed = $allowedContinueReasons.ContainsKey($id)
    $reason = if ($allowed) { $allowedContinueReasons[$id] } else { "Critical test step; later results are not meaningful if this step fails." }
    $policy = if ($stopOnFailure -eq $true) { "Stop On Failure" } else { "Continue On Failure" }

    if ($stopOnFailure -ne $true -and -not $allowed) {
        $violations += $id
    }

    $rows += [pscustomobject]@{
        Id = $id
        Name = $name
        Kind = $kind
        Policy = $policy
        AllowedToContinue = if ($allowed) { "Yes" } else { "No" }
        Reason = $reason
    }
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Stop Policy Review")
$lines.Add("")
$lines.Add("Generated from ``$(Format-RelativePath $TestPlanPath)``.")
$lines.Add("")
$lines.Add("## Summary")
$lines.Add("")
$lines.Add("- Top-level item count: $($rows.Count)")
$lines.Add("- Continue-on-failure count: $(($rows | Where-Object { $_.Policy -eq "Continue On Failure" }).Count)")
$lines.Add("- Unexpected continue-on-failure count: $($violations.Count)")
$lines.Add("- Rule: only result output and cleanup may continue after a previous failure.")
$lines.Add("")
$lines.Add("## Review Table")
$lines.Add("")
$lines.Add("| ID | Kind | Policy | Allowed To Continue | Reason |")
$lines.Add("| --- | --- | --- | --- | --- |")
foreach ($row in $rows) {
    $lines.Add("| $($row.Id) | $($row.Kind) | $($row.Policy) | $($row.AllowedToContinue) | $($row.Reason) |")
}

if ($violations.Count -gt 0) {
    $lines.Add("")
    $lines.Add("## Violations")
    $lines.Add("")
    foreach ($violation in $violations) {
        $lines.Add("- $violation")
    }
}

Set-Content -Path $OutputPath -Value $lines -Encoding UTF8

Write-Host "STOP POLICY REVIEW"
Write-Host "Plan: $($plan.name) v$($plan.version)"
Write-Host "Top-level item count: $($rows.Count)"
Write-Host "Continue-on-failure count: $(($rows | Where-Object { $_.Policy -eq "Continue On Failure" }).Count)"
Write-Host "Unexpected continue-on-failure count: $($violations.Count)"
Write-Host "Markdown report: $OutputPath"
