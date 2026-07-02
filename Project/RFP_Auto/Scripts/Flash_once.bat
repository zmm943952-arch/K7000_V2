@echo off
setlocal enabledelayedexpansion

REM User configurable section
set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "RFP_AUTO_DIR=%%~fI"
for %%I in ("%RFP_AUTO_DIR%\..") do set "ROOT_DIR=%%~fI"

set "RFP_DIR=C:\Program Files (x86)\Renesas Electronics\Programming Tools\Renesas Flash Programmer V3.16"
set "RFP_EXE=%RFP_DIR%\RFPV3.exe"
set "RFP_CONSOLE=%RFP_DIR%\RFPV3.Console.exe"
set "POWERSHELL_EXE=powershell"

set "DEFAULT_PROJECT=%ROOT_DIR%\RFP_Auto\Project\k7000_1.rpj"
set "PROJECT=%DEFAULT_PROJECT%"
set "PROJECT_DIR=%ROOT_DIR%\RFP_Auto\Project"
set "FIRMWARE="
set "LOG_DIR=%ROOT_DIR%\RFP_Auto\Logs"
set "LOG_DIR_SCRIPT=%SCRIPT_DIR%Get_RfpLogDirectory.ps1"
set "CONFIG_PATH=%ROOT_DIR%\Config.json"
set "PREPARE_SCRIPT=%SCRIPT_DIR%Prepare_RfpFirmware.ps1"
set "PREPARED_FIRMWARE_PATHS=%RFP_AUTO_DIR%\prepared_firmware_paths.txt"
set "PREPARED_PROJECT_FIRMWARE_PATHS="
set "RETRY=2"
set "TOOL_SN="

set "ARG1=%~1"
set "ARG1_NAME=%~nx1"
set "ARG2=%~2"
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
    set "SN=7000ZVMT26500010036"
) else if /I "%~x2"==".rpj" (
    if exist "%PROJECT_DIR%\%ARG2_NAME%" (
        set "PROJECT=%PROJECT_DIR%\%ARG2_NAME%"
    ) else (
        set "PROJECT=%~f2"
    )
    set "SN=7000ZVMT26500010036"
) else (
    set "SN=7000ZVMT26500010036"
)

if "%SN%"=="" set "SN=7000ZVMT26500010036"
for %%I in ("%PROJECT%") do set "PROJECT_BASE=%%~nI"
set "PREPARED_PROJECT_FIRMWARE_PATHS=%RFP_AUTO_DIR%\prepared_firmware_paths_%PROJECT_BASE%.txt"

if exist "%LOG_DIR_SCRIPT%" (
    for /f "delims=" %%i in ('%POWERSHELL_EXE% -NoProfile -ExecutionPolicy Bypass -File "%LOG_DIR_SCRIPT%" -DefaultLogDirectory "%LOG_DIR%" -ProjectPath "%PROJECT%"') do set "LOG_DIR=%%i"
)
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

echo [SN=%SN%] [INFO] Preparing RFP firmware...
"%POWERSHELL_EXE%" -NoProfile -ExecutionPolicy Bypass -File "%PREPARE_SCRIPT%" -ConfigPath "%CONFIG_PATH%" -ProjectPath "%PROJECT%"
set "PREPARE_RC=%ERRORLEVEL%"
if not "%PREPARE_RC%"=="0" (
    echo [SN=%SN%] [FAIL] Prepare RFP firmware failed. ExitCode=%PREPARE_RC%
    echo FAIL
    exit /b 12
)
echo [SN=%SN%] [INFO] PreparedFirmwarePaths=%PREPARED_FIRMWARE_PATHS%
echo [SN=%SN%] [INFO] PreparedProjectFirmwarePaths=%PREPARED_PROJECT_FIRMWARE_PATHS%

for /f %%i in ('%POWERSHELL_EXE% -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmmss"') do set "TS=%%i"

set "LOG_FILE=%LOG_DIR%\%SN%_%TS%.log"

echo [SN=%SN%] [INFO] Project=%PROJECT%
if not "%FIRMWARE%"=="" echo [SN=%SN%] [INFO] Firmware=%FIRMWARE%
echo [SN=%SN%] [INFO] Log=%LOG_FILE%
echo.

set COMMON_ARGS=/silent "%PROJECT%" /command epv /log "%LOG_FILE%"
if not "%FIRMWARE%"=="" set COMMON_ARGS=%COMMON_ARGS% /file "%FIRMWARE%"
if not "%TOOL_SN%"=="" set "COMMON_ARGS=%COMMON_ARGS% /tool %TOOL_SN%"

set /a COUNT=0

:RETRY_LOOP
set /a COUNT+=1
echo [SN=%SN%] [INFO] Attempt !COUNT! ...

"%RFP_EXE%" %COMMON_ARGS%
set "RC=%ERRORLEVEL%"

echo [SN=%SN%] [INFO] ExitCode=%RC%

if "%RC%"=="0" goto PASS

if !COUNT! LEQ %RETRY% (
    echo [SN=%SN%] [WARN] Programming failed, retrying...
    timeout /t 2 >nul
    goto RETRY_LOOP
)

goto FAIL

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
set "FINAL_LOG=%LOG_DIR%\%SN%_%TS%_%RESULT_TAG%.log"

if /I not "%LOG_FILE%"=="%FINAL_LOG%" (
    if exist "%LOG_FILE%" (
        ren "%LOG_FILE%" "%SN%_%TS%_%RESULT_TAG%.log"
    )
    set "LOG_FILE=%FINAL_LOG%"
)
exit /b 0

















































































