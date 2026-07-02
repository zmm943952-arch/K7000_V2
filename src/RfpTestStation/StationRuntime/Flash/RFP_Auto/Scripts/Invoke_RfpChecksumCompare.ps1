param(
    [Parameter(Mandatory = $true)]
    [string]$RfpCliPath,

    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputCsvPath,

    [string]$Device = "RL78",
    [string]$Tool = "e2l",
    [string]$ToolSerial = "",
    [string]$Interface = "uart1",
    [string]$Speed = "500000",
    [string]$ChecksumType = "add16"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-ProjectFilePath {
    param(
        [string]$BaseDir,
        [string]$PathText
    )

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
        [hashtable]$ByteMap
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
        throw "Invalid S-record length: $Line"
    }

    $payload = $Line.Substring($payloadStart, $payloadLength)
    $address = [Convert]::ToUInt32($payload.Substring(0, $addressHexLength), 16)
    $dataHexLength = ($count * 2) - $addressHexLength - 2
    if ($dataHexLength -lt 0) {
        throw "Invalid S-record data section: $Line"
    }

    for ($offset = 0; $offset -lt $dataHexLength; $offset += 2) {
        $currentAddress = [uint32]($address + ($offset / 2))
        $ByteMap[[string]$currentAddress] = $true
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
$addressMap = @{}

foreach ($item in $programItems) {
    $firmwarePath = Resolve-ProjectFilePath -BaseDir $repoRoot -PathText $item.'#text'
    $firmwareFiles.Add($firmwarePath)

    foreach ($line in Get-Content -LiteralPath $firmwarePath) {
        $trimmed = $line.Trim()
        if ($trimmed -ne "") {
            Add-SrecRecordBytes -Line $trimmed -ByteMap $addressMap
        }
    }
}

$addresses = $addressMap.Keys | ForEach-Object { [uint32]$_ } | Sort-Object
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

$results = New-Object System.Collections.Generic.List[object]
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

    $fileResult = Invoke-RfpCli -Arguments ($commonArgs + $fileArgs + @("-checksum-file", "-noprogress"))
    $deviceResult = Invoke-RfpCli -Arguments ($commonArgs + @("-checksum", "-noprogress"))

    $expectedChecksum = Get-ChecksumText -Text $fileResult.Text
    $deviceChecksum = Get-ChecksumText -Text $deviceResult.Text
    $match = $false
    if ($fileResult.ExitCode -eq 0 -and $deviceResult.ExitCode -eq 0 -and $expectedChecksum -ne "" -and $expectedChecksum -eq $deviceChecksum) {
        $match = $true
    }

    $results.Add([PSCustomObject]@{
        StartAddress       = ('0x{0:X8}' -f $range.Start)
        EndAddress         = ('0x{0:X8}' -f $range.End)
        ByteCount          = $range.Count
        ExpectedChecksum   = $expectedChecksum
        DeviceChecksum     = $deviceChecksum
        Match              = $match
        FileCommandExit    = $fileResult.ExitCode
        DeviceCommandExit  = $deviceResult.ExitCode
        FileCommandOutput   = ($fileResult.Text -replace '\r?\n', ' | ')
        DeviceCommandOutput = ($deviceResult.Text -replace '\r?\n', ' | ')
    })
}

$outputDir = Split-Path -Parent $OutputCsvPath
if ($outputDir -and -not (Test-Path -LiteralPath $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

$results | Export-Csv -LiteralPath $OutputCsvPath -NoTypeInformation -Encoding UTF8

if ($results.Count -eq 0) {
    throw "No checksum ranges were generated from project firmware files."
}

$failedResults = @($results | Where-Object { -not $_.Match })
if ($failedResults.Count -gt 0) {
    $failedRanges = $failedResults |
        ForEach-Object { '{0}-{1}' -f $_.StartAddress, $_.EndAddress }
    throw ("Checksum compare failed for {0} range(s): {1}" -f $failedResults.Count, ($failedRanges -join ", "))
}
