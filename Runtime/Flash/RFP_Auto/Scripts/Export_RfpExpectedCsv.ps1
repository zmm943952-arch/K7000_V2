param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputCsvPath,

    [string]$RangeCsvPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-ProjectFilePath {
    param(
        [string]$BaseDir,
        [string]$PathText
    )

    if ([string]::IsNullOrWhiteSpace($PathText)) {
        throw "Empty firmware path in project file."
    }

    if ([System.IO.Path]::IsPathRooted($PathText)) {
        if (Test-Path -LiteralPath $PathText) {
            return (Resolve-Path -LiteralPath $PathText).Path
        }

        $parts = $PathText -split "[:\\\/]+" | Where-Object { $_ -ne "" }
        if ($parts.Count -ge 2) {
            $anchorIndex = [Array]::IndexOf($parts, "RFP_Auto")
            if ($anchorIndex -ge 0) {
                $relativeTail = $parts[$anchorIndex]
                for ($i = $anchorIndex + 1; $i -lt $parts.Count; $i++) {
                    $relativeTail = Join-Path $relativeTail $parts[$i]
                }

                $candidate = Join-Path $BaseDir $relativeTail
                if (Test-Path -LiteralPath $candidate) {
                    return (Resolve-Path -LiteralPath $candidate).Path
                }
            }
        }

        throw "Firmware file not found: $PathText"
    }

    $resolved = Join-Path $BaseDir $PathText
    if (-not (Test-Path -LiteralPath $resolved)) {
        throw "Firmware file not found: $resolved"
    }

    return (Resolve-Path -LiteralPath $resolved).Path
}

function Add-SrecRecordBytes {
    param(
        [string]$Line,
        [hashtable]$ByteMap,
        [string]$SourceName
    )

    if ($Line.Length -lt 4 -or $Line[0] -ne 'S') {
        return
    }

    $recordType = $Line[1]
    $addressHexLength = switch ($recordType) {
        '1' { 4 }
        '2' { 6 }
        '3' { 8 }
        default { return }
    }

    $count = [Convert]::ToInt32($Line.Substring(2, 2), 16)
    $payloadLength = $count * 2
    $payloadStart = 4
    if ($Line.Length -lt ($payloadStart + $payloadLength)) {
        throw "Invalid S-record length in ${SourceName}: $Line"
    }

    $payload = $Line.Substring($payloadStart, $payloadLength)
    $address = [Convert]::ToUInt32($payload.Substring(0, $addressHexLength), 16)
    $dataHexLength = ($count * 2) - $addressHexLength - 2
    if ($dataHexLength -lt 0) {
        throw "Invalid S-record data section in ${SourceName}: $Line"
    }

    for ($offset = 0; $offset -lt $dataHexLength; $offset += 2) {
        $byteHex = $payload.Substring($addressHexLength + $offset, 2).ToUpperInvariant()
        $currentAddress = [uint32]($address + ($offset / 2))
        $ByteMap[[string]$currentAddress] = [PSCustomObject]@{
            Address = $currentAddress
            Data    = $byteHex
            Source  = $SourceName
        }
    }
}

$projectFullPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$projectDir = Split-Path -Parent $projectFullPath
$repoRoot = Split-Path -Parent (Split-Path -Parent $projectDir)
[xml]$projectXml = Get-Content -LiteralPath $projectFullPath

$programItems = @($projectXml.RfpProject.OperationTab.ProgramFiles.Item)
if ($programItems.Count -eq 0) {
    throw "No ProgramFiles items found in $projectFullPath"
}

$byteMap = @{}

foreach ($item in $programItems) {
    $firmwarePath = Resolve-ProjectFilePath -BaseDir $repoRoot -PathText $item.'#text'
    $sourceName = Split-Path -Leaf $firmwarePath
    foreach ($line in Get-Content -LiteralPath $firmwarePath) {
        $trimmed = $line.Trim()
        if ($trimmed -ne "") {
            Add-SrecRecordBytes -Line $trimmed -ByteMap $byteMap -SourceName $sourceName
        }
    }
}

$sortedBytes = $byteMap.Values | Sort-Object Address
if ($sortedBytes.Count -eq 0) {
    throw "No data records found in project firmware files."
}

$outputDir = Split-Path -Parent $OutputCsvPath
if ($outputDir -and -not (Test-Path -LiteralPath $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

$sortedBytes |
    Select-Object @{Name = "Address"; Expression = { ('0x{0:X8}' -f $_.Address) } },
                  @{Name = "ExpectedData"; Expression = { $_.Data } },
                  @{Name = "SourceFile"; Expression = { $_.Source } } |
    Export-Csv -LiteralPath $OutputCsvPath -NoTypeInformation -Encoding UTF8

if (-not [string]::IsNullOrWhiteSpace($RangeCsvPath)) {
    $ranges = New-Object System.Collections.Generic.List[object]
    $rangeStart = $null
    $rangeEnd = $null
    $rangeCount = 0

    foreach ($item in $sortedBytes) {
        if ($null -eq $rangeStart) {
            $rangeStart = $item.Address
            $rangeEnd = $item.Address
            $rangeCount = 1
            continue
        }

        if ($item.Address -eq ($rangeEnd + 1)) {
            $rangeEnd = $item.Address
            $rangeCount++
            continue
        }

        $ranges.Add([PSCustomObject]@{
            StartAddress = ('0x{0:X8}' -f $rangeStart)
            EndAddress   = ('0x{0:X8}' -f $rangeEnd)
            ByteCount    = $rangeCount
        })
        $rangeStart = $item.Address
        $rangeEnd = $item.Address
        $rangeCount = 1
    }

    $ranges.Add([PSCustomObject]@{
        StartAddress = ('0x{0:X8}' -f $rangeStart)
        EndAddress   = ('0x{0:X8}' -f $rangeEnd)
        ByteCount    = $rangeCount
    })

    $rangeDir = Split-Path -Parent $RangeCsvPath
    if ($rangeDir -and -not (Test-Path -LiteralPath $rangeDir)) {
        New-Item -ItemType Directory -Path $rangeDir | Out-Null
    }

    $ranges | Export-Csv -LiteralPath $RangeCsvPath -NoTypeInformation -Encoding UTF8
}
