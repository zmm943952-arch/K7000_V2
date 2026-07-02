@echo off
setlocal enabledelayedexpansion

rem === Configurable paths ===
set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "REDCASE_DIR=%%~fI"
for %%I in ("%REDCASE_DIR%\..") do set "ROOT_DIR=%%~fI"
for %%I in ("%ROOT_DIR%\..") do set "STATION_RUNTIME_DIR=%%~fI"
set "EXE=%SCRIPT_DIR%FlashUpdate.exe"
set "POWERSHELL_EXE=powershell"
set "CONFIG_PATH=%STATION_RUNTIME_DIR%\Config\Config.json"
set "PREPARE_SCRIPT=%SCRIPT_DIR%Prepare_RedCaseFirmware.ps1"
set "PREPARED_BIN_PATH_FILE=%SCRIPT_DIR%prepared_bin_path.txt"
set "LOG_DIR=%SCRIPT_DIR%logs"
set "LOG_DIR_SCRIPT=%SCRIPT_DIR%Get_RedCaseLogDirectory.ps1"

rem SN priority: arg1 > default
set "SN=7000ZVMT26500010036"
if "%SN%"=="" set "SN=7000ZVMT26500010036"

if not exist "%EXE%" (
  echo [ERROR] FlashUpdate.exe not found: "%EXE%"
  exit /b 2
)

echo [INFO] Preparing RedCase firmware...
"%POWERSHELL_EXE%" -NoProfile -ExecutionPolicy Bypass -File "%PREPARE_SCRIPT%" -ConfigPath "%CONFIG_PATH%" -OutputBinPathFile "%PREPARED_BIN_PATH_FILE%"
set "PREPARE_RC=%ERRORLEVEL%"
if not "%PREPARE_RC%"=="0" (
  echo [ERROR] Prepare RedCase firmware failed. ExitCode=%PREPARE_RC%
  echo FAIL
  exit /b 12
)

set "BIN=%SCRIPT_DIR%Firmware_Local\HX6330B03_C_Sharp_GM_Mobis_2992x1299_16.3_Falcon_20251223_S1C7A404_Test_Ver4-0.bin"
if exist "%PREPARED_BIN_PATH_FILE%" (
  set /p BIN=<"%PREPARED_BIN_PATH_FILE%"
)

if "%BIN%"=="" (
  echo [ERROR] Bin path is empty.
  echo Prepared bin path file: "%PREPARED_BIN_PATH_FILE%"
  exit /b 3
)

if not exist "%BIN%" (
  echo [ERROR] Bin file not found: "%BIN%"
  exit /b 4
)

if exist "%LOG_DIR_SCRIPT%" (
  for /f "delims=" %%i in ('%POWERSHELL_EXE% -NoProfile -ExecutionPolicy Bypass -File "%LOG_DIR_SCRIPT%" -DefaultLogDirectory "%LOG_DIR%"') do set "LOG_DIR=%%i"
)
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

rem Use PowerShell to get timestamp (wmic may be missing)
for /f %%i in ('%POWERSHELL_EXE% -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmmss"') do set "TS=%%i"
set "LOG_TMP=%LOG_DIR%\%SN%_%TS%_Running.log"

echo [INFO] EXE: "%EXE%"
echo [INFO] BIN: "%BIN%"
echo [INFO] SN: "%SN%"
echo [INFO] TEMP LOG: "%LOG_TMP%"

rem Initialize log (per run) and capture all output
for /f "delims=" %%i in ('%POWERSHELL_EXE% -NoProfile -Command "Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff'"') do set "START_TS=%%i"
> "%LOG_TMP%" echo ========================================
>> "%LOG_TMP%" echo [INFO] Start: !START_TS!
>> "%LOG_TMP%" echo [INFO] CMD: "%EXE%" "%BIN%"
>> "%LOG_TMP%" echo [INFO] SN: "%SN%"
"%EXE%" "%BIN%" 1>> "%LOG_TMP%" 2>>&1
for /f "delims=" %%i in ('%POWERSHELL_EXE% -NoProfile -Command "Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff'"') do set "END_TS=%%i"
>> "%LOG_TMP%" echo [INFO] End: !END_TS!
set "EXITCODE=%ERRORLEVEL%"

echo [INFO] ExitCode: !EXITCODE!
findstr /C:"CRC Check Pass" "%LOG_TMP%" >nul
if !ERRORLEVEL! EQU 0 (
  set "RESULT=Pass"
  set "RET=0"
  >> "%LOG_TMP%" echo [RESULT] SUCCESS
) else (
  set "RESULT=Fail"
  set "RET=1"
  >> "%LOG_TMP%" echo [RESULT] FAIL
)

set "LOG=%LOG_DIR%\%SN%_%TS%_%RESULT%.log"
move /Y "%LOG_TMP%" "%LOG%" >nul

echo [INFO] Log saved to: "%LOG%"
if /I "%RESULT%"=="Pass" (
  echo PASS
) else (
  echo FAIL
)
exit /b %RET%


