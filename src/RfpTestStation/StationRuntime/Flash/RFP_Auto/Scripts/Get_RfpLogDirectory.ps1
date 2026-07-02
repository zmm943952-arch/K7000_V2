param(
    [Parameter(Mandatory = $true)]
    [string]$DefaultLogDirectory,

    [string]$ProjectPath = "",

    [string]$AutobenchDataDirectory = $(if ($env:AUTOBENCH_DATA_DIR) { $env:AUTOBENCH_DATA_DIR } else { "D:\Autobench\Data" })
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-IniValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Section,

        [Parameter(Mandatory = $true)]
        [string]$Key
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }

    $currentSection = ""
    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith(";") -or $trimmed.StartsWith("#")) {
            continue
        }

        if ($trimmed -match '^\[(.+)\]$') {
            $currentSection = $matches[1].Trim()
            continue
        }

        if ($currentSection -ieq $Section -and $trimmed -match '^([^=]+?)\s*=\s*(.*)$') {
            $name = $matches[1].Trim()
            if ($name -ieq $Key) {
                $value = $matches[2].Trim()
                if (($value.StartsWith('"') -and $value.EndsWith('"')) -or
                    ($value.StartsWith("'") -and $value.EndsWith("'"))) {
                    $value = $value.Substring(1, $value.Length - 2)
                }
                return $value.Trim()
            }
        }
    }

    return $null
}

$sequencePath = Join-Path $AutobenchDataDirectory "Sequence.ini"
$csvReportPath = Get-IniValue -Path $sequencePath -Section "ReportConfigure" -Key "CSVReportPath"

$logDirectory = $DefaultLogDirectory
if (-not [string]::IsNullOrWhiteSpace($csvReportPath)) {
    $logDirectory = $csvReportPath
    if (-not [System.IO.Path]::IsPathRooted($logDirectory)) {
        $logDirectory = Join-Path $AutobenchDataDirectory $logDirectory
    }
}

$projectName = [System.IO.Path]::GetFileName($ProjectPath)
if ($projectName -ieq "k7000_1.rpj") {
    $logDirectory = Join-Path $logDirectory ("MCU " + [char]0x7B80 + [char]0x6613)
}
elseif ($projectName -ieq "k7000_2.rpj") {
    $logDirectory = Join-Path $logDirectory ("MCU " + [char]0x51FA + [char]0x8D27)
}

[System.IO.Path]::GetFullPath($logDirectory)
