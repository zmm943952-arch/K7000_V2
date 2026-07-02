@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul

rem === User config ===
set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..\..\..\..\..") do set "ROOT_DIR=%%~fI"
for %%I in ("%ROOT_DIR%\..") do set "STATION_RUNTIME_DIR=%%~fI"
set "CONFIG_PATH=%STATION_RUNTIME_DIR%\Config\Config.json"
set "PREPARE_SCRIPT=%SCRIPT_DIR%Prepare_TddiLua.ps1"
set "PREPARED_LUA_PATH_FILE=%SCRIPT_DIR%prepared_lua_path.txt"
set "LOG_DIR_SCRIPT=%SCRIPT_DIR%Get_TddiLogDirectory.ps1"
set "LUA="
set TIMEOUT=180
set CONFIG=Debug
set PLATFORM=AnyCPU
set "SN=7000ZVMT26500010036"
if not defined SN set "SN=7000ZVMT26500010036"
set "COM=COM14"
if not defined COM if defined FLASH_COM set "COM=COM14"
if not defined COM set "COM=COM14"

echo [INFO] Preparing TDDI lua...
powershell -NoProfile -ExecutionPolicy Bypass -File "%PREPARE_SCRIPT%" -ConfigPath "%CONFIG_PATH%" -OutputLuaPathFile "%PREPARED_LUA_PATH_FILE%"
set "PREPARE_RC=%ERRORLEVEL%"
if not "%PREPARE_RC%"=="0" (
  echo [ERROR] Prepare TDDI lua failed. ExitCode=%PREPARE_RC%
  echo FAIL
  exit /b 12
)

if exist "%PREPARED_LUA_PATH_FILE%" (
  set /p LUA=<"%PREPARED_LUA_PATH_FILE%"
)

if "%LUA%"=="" (
  echo [ERROR] Lua path is empty.
  echo Prepared lua path file: "%PREPARED_LUA_PATH_FILE%"
  exit /b 3
)

set ROOT=%~dp0
rem Locate repo root that contains Test\Test.csproj by walking up (pure batch)
set CUR=%ROOT%
set PREV=
:find_root
if exist "%CUR%Test\Test.csproj" (
  set ROOT=%CUR%
  goto :root_found
)
for %%i in ("%CUR%..") do set CUR=%%~fi\
if /i "%CUR%"=="%PREV%" goto :root_found
set PREV=%CUR%
goto :find_root
:root_found

set PROJ=%ROOT%Test\Test.csproj
set OUTDIR=%ROOT%Test\bin\%CONFIG%
set EXE=%OUTDIR%\Test.exe

for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "[DateTime]::Now.ToString('yyyyMMdd_HHmmss')"`) do set "TS=%%i"
if not defined TS set "TS=00000000_000000"
set LOGDIR=%~dp0logs
if exist "%LOG_DIR_SCRIPT%" (
  for /f "delims=" %%i in ('powershell -NoProfile -ExecutionPolicy Bypass -File "%LOG_DIR_SCRIPT%" -DefaultLogDirectory "%LOGDIR%"') do set "LOGDIR=%%i"
)
if not exist "%LOGDIR%" mkdir "%LOGDIR%"
set "LOG_TMP=%LOGDIR%\%SN%_%TS%_Running.log"
set "LOG="

echo [INFO] SN: %SN%
echo [INFO] COM: %COM%
echo [INFO] Temp log file: %LOG_TMP%
powershell -NoProfile -Command "Add-Content -LiteralPath '%LOG_TMP%' -Value '[INFO] SN: %SN%' -Encoding utf8"
powershell -NoProfile -Command "Add-Content -LiteralPath '%LOG_TMP%' -Value '[INFO] COM: %COM%' -Encoding utf8"
powershell -NoProfile -Command "Add-Content -LiteralPath '%LOG_TMP%' -Value '[INFO] Temp log file: %LOG_TMP%' -Encoding utf8"

if not exist "%EXE%" (
  echo [INFO] Building %CONFIG%...
  powershell -NoProfile -Command "Add-Content -LiteralPath '%LOG_TMP%' -Value '[INFO] Building %CONFIG%...' -Encoding utf8"
  powershell -NoProfile -Command ^
    "$msb = (Get-Command msbuild -ErrorAction SilentlyContinue); " ^
    "if ($msb) { & $msb.Source '%PROJ%' /p:Configuration=%CONFIG%;Platform=%PLATFORM% /m 2>&1 | Tee-Object -FilePath '%LOG_TMP%' -Append; exit $LASTEXITCODE } " ^
    "else { & dotnet build '%PROJ%' -c %CONFIG% 2>&1 | Tee-Object -FilePath '%LOG_TMP%' -Append; exit $LASTEXITCODE }"
  if errorlevel 1 goto :fail
)

if not exist "%EXE%" (
  rem Try to find framework-specific output, e.g. bin\Debug\net6.0\Test.exe
  for /f "delims=" %%p in ('powershell -NoProfile -Command "Get-ChildItem -Path ''%OUTDIR%'' -Recurse -Filter Test.exe -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName"') do set EXE=%%p
)

if not exist "%EXE%" (
  rem Fall back to Debug if user left CONFIG as Release but only Debug exists
  for /f "delims=" %%p in ('powershell -NoProfile -Command "Get-ChildItem -Path ''%ROOT%Test\\bin\\Debug'' -Recurse -Filter Test.exe -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName"') do set EXE=%%p
)

if not exist "%EXE%" goto :fail_noexe

echo [INFO] Run: "%EXE%" %COM% "%LUA%" %TIMEOUT%
powershell -NoProfile -Command "Add-Content -LiteralPath '%LOG_TMP%' -Value '[INFO] Run: \"%EXE%\" %COM% \"%LUA%\" %TIMEOUT%' -Encoding utf8"
powershell -NoProfile -Command "$out = & '%EXE%' %COM% '%LUA%' %TIMEOUT% 2>&1 | ForEach-Object { $_ -replace '\[SUCCESS\]', '[PASS]' }; $out | Out-File -FilePath '%LOG_TMP%' -Append -Encoding utf8; $out; exit $LASTEXITCODE"
set "RC=%ERRORLEVEL%"
set "RESULT=Fail"
if !RC!==0 set "RESULT=Pass"
set "LOG=%LOGDIR%\%SN%_%TS%_%RESULT%.log"
move /Y "%LOG_TMP%" "%LOG%" >nul
echo [INFO] ExitCode=!RC!
powershell -NoProfile -Command "Add-Content -LiteralPath '%LOG%' -Value '[INFO] ExitCode=!RC!' -Encoding utf8"
echo [INFO] Log saved to: %LOG%
echo !RESULT!
powershell -NoProfile -Command "Add-Content -LiteralPath '%LOG%' -Value '!RESULT!' -Encoding utf8"
exit /b %RC%

:fail
  set "RESULT=Fail"
  set "LOG=%LOGDIR%\%SN%_%TS%_%RESULT%.log"
  powershell -NoProfile -Command "Add-Content -LiteralPath '%LOG_TMP%' -Value '[ERROR] Build failed.' -Encoding utf8"
  move /Y "%LOG_TMP%" "%LOG%" >nul
  echo [ERROR] Build failed. See log: %LOG%
  powershell -NoProfile -Command "Add-Content -LiteralPath '%LOG%' -Value '[ERROR] Build failed. See log: %LOG%' -Encoding utf8"
  echo [INFO] Log saved to: %LOG%
  echo FAIL
  powershell -NoProfile -Command "Add-Content -LiteralPath '%LOG%' -Value 'FAIL' -Encoding utf8"
  exit /b 1

:fail_noexe
  set "RESULT=Fail"
  set "LOG=%LOGDIR%\%SN%_%TS%_%RESULT%.log"
  powershell -NoProfile -Command "Add-Content -LiteralPath '%LOG_TMP%' -Value '[ERROR] EXE not found: %EXE%' -Encoding utf8"
  move /Y "%LOG_TMP%" "%LOG%" >nul
  echo [ERROR] EXE not found: %EXE%
  powershell -NoProfile -Command "Add-Content -LiteralPath '%LOG%' -Value '[ERROR] EXE not found: %EXE%' -Encoding utf8"
  echo [INFO] Log saved to: %LOG%
  echo FAIL
  powershell -NoProfile -Command "Add-Content -LiteralPath '%LOG%' -Value 'FAIL' -Encoding utf8"
  exit /b 1

