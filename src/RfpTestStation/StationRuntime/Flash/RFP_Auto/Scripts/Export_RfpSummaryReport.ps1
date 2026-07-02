param(
    [Parameter(Mandatory = $true)]
    [string]$RfpCliPath,

    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputCsvPath,

    [string]$SerialNumber = "NO_SN",
    [string]$Device = "RL78",
    [string]$Tool = "e2l",
    [string]$ToolSerial = "",
    [string]$Interface = "uart1",
    [string]$Speed = "500000",
    [string]$ChecksumType = "add16",
    [string]$FlashLogPath = ""
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

function Get-ContiguousRanges {
    param(
        [uint32[]]$Addresses
    )

    $ranges = New-Object System.Collections.Generic.List[object]
    if ($Addresses.Count -eq 0) {
        return $ranges
    }

    $start = $Addresses[0]
    $end = $Addresses[0]
    for ($i = 1; $i -lt $Addresses.Count; $i++) {
        if ($Addresses[$i] -eq ($end + 1)) {
            $end = $Addresses[$i]
            continue
        }

        $ranges.Add([PSCustomObject]@{
            Start = $start
            End   = $end
            Count = [int]($end - $start + 1)
        })
        $start = $Addresses[$i]
        $end = $Addresses[$i]
    }

    $ranges.Add([PSCustomObject]@{
        Start = $start
        End   = $end
        Count = [int]($end - $start + 1)
    })

    return $ranges
}

function Invoke-RfpCli {
    param(
        [string]$RfpCliPath,
        [string[]]$Arguments
    )

    $output = & $RfpCliPath @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    return [PSCustomObject]@{
        ExitCode = $exitCode
        Text     = ($output | Out-String).Trim()
    }
}

function Get-ChecksumText {
    param(
        [string]$Text
    )

    $patterns = @(
        '(?im)^\s*\[[^\]]+\]\s+[0-9A-F]{8}\s+-\s+[0-9A-F]{8}:\s*(0x[0-9A-F]+|[0-9A-F]{4,16})\s*$',
        '(?im)^.*checksum.*?(0x[0-9A-F]+).*$',
        '(?im)^.*checksum.*?([0-9A-F]{4,16}).*$',
        '(?im)^.*result.*?(0x[0-9A-F]+|[0-9A-F]{4,16}).*$'
    )

    foreach ($pattern in $patterns) {
        $match = [regex]::Match($Text, $pattern)
        if ($match.Success) {
            return $match.Groups[1].Value.ToUpperInvariant()
        }
    }

    return ""
}

$projectFullPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$projectDir = Split-Path -Parent $projectFullPath
$repoRoot = Split-Path -Parent (Split-Path -Parent $projectDir)
[xml]$projectXml = Get-Content -LiteralPath $projectFullPath
$programItems = @($projectXml.RfpProject.OperationTab.ProgramFiles.Item)
$connectOptionTab = $projectXml.RfpProject.ConnectOptionTab

if ($programItems.Count -eq 0) {
    throw "No ProgramFiles items found in $projectFullPath"
}

$firmwareFiles = New-Object System.Collections.Generic.List[string]
$byteMap = @{}
foreach ($item in $programItems) {
    $firmwarePath = Resolve-ProjectFilePath -BaseDir $repoRoot -PathText $item.'#text'
    $firmwareFiles.Add($firmwarePath)
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

$addresses = $sortedBytes | ForEach-Object { [uint32]$_.Address }
$ranges = Get-ContiguousRanges -Addresses $addresses

$toolOption = $Tool
if (-not [string]::IsNullOrWhiteSpace($ToolSerial)) {
    $toolOption = "$Tool`:$ToolSerial"
}

$authArgs = @()
if ($null -ne $connectOptionTab) {
    $idCodeAuth = [string]$connectOptionTab.IdCodeAuthen
    $codeFlashPwdAuth = [string]$connectOptionTab.CodeFlashPWDAuthen
    $dataFlashPwdAuth = [string]$connectOptionTab.DataFlashPWDAuthen

    if (-not [string]::IsNullOrWhiteSpace($idCodeAuth)) {
        $authArgs += @("-auth", "id", $idCodeAuth.Trim())
    }
    if (-not [string]::IsNullOrWhiteSpace($codeFlashPwdAuth)) {
        $authArgs += @("-auth", "cfpw", $codeFlashPwdAuth.Trim())
    }
    if (-not [string]::IsNullOrWhiteSpace($dataFlashPwdAuth)) {
        $authArgs += @("-auth", "dfpw", $dataFlashPwdAuth.Trim())
    }
}

$rangeResults = New-Object System.Collections.Generic.List[object]
foreach ($range in $ranges) {
    $commonArgs = @(
        "-device", $Device,
        "-tool", $toolOption,
        "-if", $Interface,
        "-speed", $Speed,
        "-checksum-type", $ChecksumType,
        "-range", ('0x{0:X8},0x{1:X8}' -f $range.Start, $range.End)
    ) + $authArgs

    $fileArgs = @()
    foreach ($firmwareFile in $firmwareFiles) {
        $fileArgs += $firmwareFile
    }

    $fileResult = Invoke-RfpCli -RfpCliPath $RfpCliPath -Arguments ($commonArgs + $fileArgs + @("-checksum-file", "-noprogress"))
    $deviceResult = Invoke-RfpCli -RfpCliPath $RfpCliPath -Arguments ($commonArgs + @("-checksum", "-noprogress"))
    $expectedChecksum = Get-ChecksumText -Text $fileResult.Text
    $deviceChecksum = Get-ChecksumText -Text $deviceResult.Text
    $match = $false
    if ($fileResult.ExitCode -eq 0 -and $deviceResult.ExitCode -eq 0 -and $expectedChecksum -ne "" -and $expectedChecksum -eq $deviceChecksum) {
        $match = $true
    }

    $rangeResults.Add([PSCustomObject]@{
        RangeStart          = ('0x{0:X8}' -f $range.Start)
        RangeEnd            = ('0x{0:X8}' -f $range.End)
        ByteCount           = $range.Count
        ExpectedChecksum    = $expectedChecksum
        DeviceChecksum      = $deviceChecksum
        Match               = $match
        FileCommandExit     = $fileResult.ExitCode
        DeviceCommandExit   = $deviceResult.ExitCode
        FileCommandOutput   = ($fileResult.Text -replace '\r?\n', ' | ')
        DeviceCommandOutput = ($deviceResult.Text -replace '\r?\n', ' | ')
    })
}

$overallPass = ($rangeResults.Count -gt 0) -and (@($rangeResults | Where-Object { -not $_.Match }).Count -eq 0)
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$compareMethod = "RL78 raw byte readback unsupported by rfp-cli; address rows show programmed bytes, match is based on containing range checksum."
$deviceDataText = "N/A - RL78 raw readback unsupported"

$rangeIndex = 0
function Get-RangeForAddress {
    param(
        [uint32]$Address,
        [ref]$CurrentRangeIndex,
        [object[]]$RangeResults
    )

    while ($CurrentRangeIndex.Value -lt $RangeResults.Count) {
        $currentRange = $RangeResults[$CurrentRangeIndex.Value]
        $rangeStartValue = [Convert]::ToUInt32($currentRange.RangeStart.Substring(2), 16)
        $rangeEndValue = [Convert]::ToUInt32($currentRange.RangeEnd.Substring(2), 16)
        if ($Address -lt $rangeStartValue) {
            break
        }
        if ($Address -le $rangeEndValue) {
            return $currentRange
        }
        $CurrentRangeIndex.Value++
    }

    throw ("Address 0x{0:X8} is outside all generated checksum ranges." -f $Address)
}

$blockSize = 16
$currentBlock = New-Object System.Collections.Generic.List[object]
$currentBlockRange = $null
$dataBlocks = New-Object System.Collections.Generic.List[object]
foreach ($byte in $sortedBytes) {
    $matchedRange = Get-RangeForAddress -Address ([uint32]$byte.Address) -CurrentRangeIndex ([ref]$rangeIndex) -RangeResults $rangeResults

    $shouldFlush = $false
    if ($currentBlock.Count -gt 0) {
        $lastByte = $currentBlock[$currentBlock.Count - 1]
        $expectedNextAddress = [uint32]($lastByte.Address + 1)
        if ($byte.Address -ne $expectedNextAddress) {
            $shouldFlush = $true
        }
        elseif ($currentBlock.Count -ge $blockSize) {
            $shouldFlush = $true
        }
        elseif ($lastByte.Source -ne $byte.Source) {
            $shouldFlush = $true
        }
        elseif ($currentBlockRange.RangeStart -ne $matchedRange.RangeStart -or $currentBlockRange.RangeEnd -ne $matchedRange.RangeEnd) {
            $shouldFlush = $true
        }
    }

    if ($shouldFlush) {
        $dataBlocks.Add([PSCustomObject]@{
            RangeStart       = $currentBlockRange.RangeStart
            RangeEnd         = $currentBlockRange.RangeEnd
            ByteCount        = $currentBlockRange.ByteCount
            Address          = ('0x{0:X8}' -f $currentBlock[0].Address)
            BlockEndAddress  = ('0x{0:X8}' -f $currentBlock[$currentBlock.Count - 1].Address)
            ExpectedData     = (($currentBlock | ForEach-Object { $_.Data }) -join " ")
            DeviceData       = $deviceDataText
            Match            = $currentBlockRange.Match
            ExpectedChecksum = $currentBlockRange.ExpectedChecksum
            DeviceChecksum   = $currentBlockRange.DeviceChecksum
        })
        $currentBlock.Clear()
    }

    if ($currentBlock.Count -eq 0) {
        $currentBlockRange = $matchedRange
    }

    $currentBlock.Add($byte)
}

if ($currentBlock.Count -gt 0) {
    $dataBlocks.Add([PSCustomObject]@{
        RangeStart       = $currentBlockRange.RangeStart
        RangeEnd         = $currentBlockRange.RangeEnd
        ByteCount        = $currentBlockRange.ByteCount
        Address          = ('0x{0:X8}' -f $currentBlock[0].Address)
        BlockEndAddress  = ('0x{0:X8}' -f $currentBlock[$currentBlock.Count - 1].Address)
        ExpectedData     = (($currentBlock | ForEach-Object { $_.Data }) -join " ")
        DeviceData       = $deviceDataText
        Match            = $currentBlockRange.Match
        ExpectedChecksum = $currentBlockRange.ExpectedChecksum
        DeviceChecksum   = $currentBlockRange.DeviceChecksum
    })
}

$outputDir = Split-Path -Parent $OutputCsvPath
if ($outputDir -and -not (Test-Path -LiteralPath $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

function ConvertTo-CsvLine {
    param(
        [object[]]$Values
    )

    return (($Values | ForEach-Object {
        $text = [string]$_
        '"' + ($text -replace '"', '""') + '"'
    }) -join ",")
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add((ConvertTo-CsvLine @("Metadata")))
$lines.Add((ConvertTo-CsvLine @("Field", "Value")))
$lines.Add((ConvertTo-CsvLine @("SerialNumber", $SerialNumber)))
$lines.Add((ConvertTo-CsvLine @("Timestamp", $timestamp)))
$lines.Add((ConvertTo-CsvLine @("OverallResult", $(if ($overallPass) { "PASS" } else { "FAIL" }))))
$lines.Add((ConvertTo-CsvLine @("Project", $projectFullPath)))
$lines.Add((ConvertTo-CsvLine @("Device", $Device)))
$lines.Add((ConvertTo-CsvLine @("Tool", $toolOption)))
$lines.Add((ConvertTo-CsvLine @("Interface", $Interface)))
$lines.Add((ConvertTo-CsvLine @("Speed", $Speed)))
$lines.Add((ConvertTo-CsvLine @("ChecksumType", $ChecksumType)))
$lines.Add((ConvertTo-CsvLine @("CompareMethod", $compareMethod)))
$lines.Add((ConvertTo-CsvLine @("FlashLog", $FlashLogPath)))
$lines.Add("")

$lines.Add((ConvertTo-CsvLine @("Range Compare")))
$lines.Add((ConvertTo-CsvLine @("RangeStart", "RangeEnd", "ByteCount", "Match", "ExpectedChecksum", "DeviceChecksum", "FileCommandExit", "DeviceCommandExit")))
foreach ($rangeResult in $rangeResults) {
    $lines.Add((ConvertTo-CsvLine @(
        $rangeResult.RangeStart,
        $rangeResult.RangeEnd,
        $rangeResult.ByteCount,
        $rangeResult.Match,
        $rangeResult.ExpectedChecksum,
        $rangeResult.DeviceChecksum,
        $rangeResult.FileCommandExit,
        $rangeResult.DeviceCommandExit
    )))
}
$lines.Add("")

$lines.Add((ConvertTo-CsvLine @("Data Blocks")))
$lines.Add((ConvertTo-CsvLine @("RangeStart", "RangeEnd", "ByteCount", "Address", "BlockEndAddress", "ExpectedData", "DeviceData", "Match", "ExpectedChecksum", "DeviceChecksum")))
foreach ($block in $dataBlocks) {
    $lines.Add((ConvertTo-CsvLine @(
        $block.RangeStart,
        $block.RangeEnd,
        $block.ByteCount,
        $block.Address,
        $block.BlockEndAddress,
        $block.ExpectedData,
        $block.DeviceData,
        $block.Match,
        $block.ExpectedChecksum,
        $block.DeviceChecksum
    )))
}

Set-Content -LiteralPath $OutputCsvPath -Value $lines -Encoding UTF8

if (-not $overallPass) {
    $failedRanges = @($rangeResults | Where-Object { -not $_.Match } | ForEach-Object { '{0}-{1}' -f $_.RangeStart, $_.RangeEnd })
    throw ("Summary report generated, but checksum compare failed for range(s): {0}" -f ($failedRanges -join ", "))
}
