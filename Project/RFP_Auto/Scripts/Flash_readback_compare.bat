@echo off
setlocal enabledelayedexpansion

REM RL78 + RFP CLI limitation:
REM rfp-cli does not support raw byte readback (-read / -read-bin / -read-view) on RL78.
REM This script therefore generates one summary report after programming:
REM 1. Address rows list the programmed byte values from the MOT files.
REM 2. Match status is determined by checksum compare for each programmed contiguous address segment.

set SCRIPT_DIR=%~dp0
for %%I in ("%SCRIPT_DIR%..") do set RFP_AUTO_DIR=%%~fI
for %%I in ("%RFP_AUTO_DIR%\..") do set ROOT_DIR=%%~fI

set RFP_DIR=C:\Program Files (x86)\Renesas Electronics\Programming Tools\Renesas Flash Programmer V3.16
set RFP_EXE=%RFP_DIR%\RFPV3.exe
set RFP_CLI=%RFP_DIR%\rfp-cli.exe
set POWERSHELL_EXE=powershell

set "DEFAULT_PROJECT=%ROOT_DIR%\RFP_Auto\Project\k7000_1.rpj"
set "PROJECT=%DEFAULT_PROJECT%"
set "PROJECT_DIR=%ROOT_DIR%\RFP_Auto\Project"
set "LOG_DIR=%ROOT_DIR%\RFP_Auto\Logs"
set "LOG_DIR_SCRIPT=%SCRIPT_DIR%Get_RfpLogDirectory.ps1"
set "COMPARE_DIR=%ROOT_DIR%\RFP_Auto\Compare"

set "DEVICE=RL78"
set "TOOL=e2l"
set "INTERFACE=uart1"
set "SPEED=500000"
set "CHECKSUM_TYPE=add16"
set "TOOL_SN="
set "RETRY=2"

if not exist "%COMPARE_DIR%" mkdir "%COMPARE_DIR%"

set "ARG1=%~1"
set "ARG2=%~2"
set "ARG1_NAME=%~nx1"
set "ARG2_NAME=%~nx2"

if /I "%~x1"==".prj" (
    echo [FAIL] Invalid project extension: %~1 . Use .rpj instead.
    exit /b 10
)
if /I "%~x2"==".prj" (
    echo [FAIL] Invalid project extension: %~2 . Use .rpj instead.
    exit /b 10
)

if /I "%~x1"==".rpj" (
    if exist "%PROJECT_DIR%\%ARG1_NAME%" (
        set "PROJECT=%PROJECT_DIR%\%ARG1_NAME%"
    ) else (
        set "PROJECT=%~f1"
    )
    set "SN=111"
) else if /I "%~x2"==".rpj" (
    if exist "%PROJECT_DIR%\%ARG2_NAME%" (
        set "PROJECT=%PROJECT_DIR%\%ARG2_NAME%"
    ) else (
        set "PROJECT=%~f2"
    )
    set "SN=111"
) else (
    set "SN=111"
)

if "%SN%"=="" set "SN=111"

if exist "%LOG_DIR_SCRIPT%" (
    for /f "delims=" %%i in ('%POWERSHELL_EXE% -NoProfile -ExecutionPolicy Bypass -File "%LOG_DIR_SCRIPT%" -DefaultLogDirectory "%LOG_DIR%" -ProjectPath "%PROJECT%"') do set "LOG_DIR=%%i"
)
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

for /f %%i in ('%POWERSHELL_EXE% -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmmss"') do set TS=%%i

set "LOG_FILE=%LOG_DIR%\%SN%_%TS%_flash.log"
set "SUMMARY_CSV=%COMPARE_DIR%\%SN%_%TS%_summary_report.csv"

echo [SN=%SN%] [INFO] Project=%PROJECT%
echo [SN=%SN%] [INFO] Log=%LOG_FILE%
echo [SN=%SN%] [INFO] SummaryCsv=%SUMMARY_CSV%
echo.

set "COMMON_ARGS=/silent "%PROJECT%" /command epv /log "%LOG_FILE%""
if not "%TOOL_SN%"=="" set "COMMON_ARGS=%COMMON_ARGS% /tool %TOOL_SN%"

set /a COUNT=0

:RETRY_LOOP
set /a COUNT+=1
echo [SN=%SN%] [INFO] Flash attempt !COUNT! ...
"%RFP_EXE%" %COMMON_ARGS%
set RC=%ERRORLEVEL%
echo [SN=%SN%] [INFO] ExitCode=%RC%

if "%RC%"=="0" goto FLASH_OK

if !COUNT! LEQ %RETRY% (
    echo [SN=%SN%] [WARN] Flash failed, retrying...
    timeout /t 2 >nul
    goto RETRY_LOOP
)

echo [SN=%SN%] [FAIL] Flash operation failed.
goto FAIL

:FLASH_OK
echo [SN=%SN%] [INFO] Generating summary report...
"%POWERSHELL_EXE%" -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Export_RfpSummaryReport.ps1" ^
    -RfpCliPath "%RFP_CLI%" ^
    -ProjectPath "%PROJECT%" ^
    -OutputCsvPath "%SUMMARY_CSV%" ^
    -SerialNumber "%SN%" ^
    -Device "%DEVICE%" ^
    -Tool "%TOOL%" ^
    -ToolSerial "%TOOL_SN%" ^
    -Interface "%INTERFACE%" ^
    -Speed "%SPEED%" ^
    -ChecksumType "%CHECKSUM_TYPE%" ^
    -FlashLogPath "%LOG_FILE%"
if errorlevel 1 (
    echo [SN=%SN%] [FAIL] Failed to generate summary report.
    set "RC=2"
    goto FAIL
)
echo [SN=%SN%] [INFO] Summary report CSV: %SUMMARY_CSV%

:PASS
call :FINALIZE_LOG pass
for /f %%i in ('%POWERSHELL_EXE% -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmmss"') do set "END_TS=%%i"
echo [SN=%SN%] [INFO] EndTime=%END_TS%
echo [SN=%SN%] [INFO] Log saved to: %LOG_FILE%
echo PASS
exit /b 0

:FAIL
call :FINALIZE_LOG fail
for /f %%i in ('%POWERSHELL_EXE% -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmmss"') do set "END_TS=%%i"
echo [SN=%SN%] [INFO] EndTime=%END_TS%
echo [SN=%SN%] [INFO] Log saved to: %LOG_FILE%
echo FAIL
exit /b 1

:FINALIZE_LOG
set "RESULT_TAG=%~1"
if /I "%RESULT_TAG%"=="pass" (
    set "FINAL_LOG=%LOG_DIR%\%SN%_%TS%_pass.log"
) else (
    set "FINAL_LOG=%LOG_DIR%\%SN%_%TS%_fail.log"
)

if /I not "%LOG_FILE%"=="%FINAL_LOG%" (
    if exist "%LOG_FILE%" (
        ren "%LOG_FILE%" "%SN%_%TS%_%RESULT_TAG%.log"
    )
    if exist "%SUMMARY_CSV%" (
        "%POWERSHELL_EXE%" -NoProfile -Command ^
            "$csvPath = [System.IO.Path]::GetFullPath('%SUMMARY_CSV%');" ^
            "$oldLog = [System.IO.Path]::GetFullPath('%LOG_FILE%');" ^
            "$newLog = [System.IO.Path]::GetFullPath('%FINAL_LOG%');" ^
            "$content = [System.IO.File]::ReadAllText($csvPath);" ^
            "$updated = $content.Replace($oldLog, $newLog);" ^
            "[System.IO.File]::WriteAllText($csvPath, $updated, [System.Text.UTF8Encoding]::new($false))"
    )
    set "LOG_FILE=%FINAL_LOG%"
)
exit /b 0







































































































































































































































