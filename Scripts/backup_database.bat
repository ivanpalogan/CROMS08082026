@echo off
REM ============================================================
REM  CROMS - Backup Database
REM  Dumps the live "croms" database to a timestamped .sql file
REM  in the Backups folder (mysqldump). Run before a demo, or
REM  on any day you want a restore point.
REM ============================================================

setlocal

REM ---- Settings (edit these if your setup differs) -----------
set "MYSQL_BIN=C:\Program Files\MySQL\MySQL Server 9.3\bin"
set "DB_HOST=localhost"
set "DB_PORT=3306"
set "DB_USER=root"
set "DB_PASS=@ivan123"
set "DB_NAME=croms"
set "OUT_DIR=%~dp0Backups"
REM ------------------------------------------------------------

set "MYSQLDUMP=%MYSQL_BIN%\mysqldump.exe"

REM Build a safe timestamp: YYYY-MM-DD_HHMMSS (locale-independent)
for /f %%a in ('powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd_HHmmss"') do set "STAMP=%%a"
set "OUT_FILE=%OUT_DIR%\croms_%STAMP%.sql"

echo(
echo ==========================================================
echo   CROMS - Backup Database
echo ==========================================================
echo   Source : %DB_NAME% on %DB_HOST%:%DB_PORT%
echo   Output : %OUT_FILE%
echo ==========================================================
echo(

if not exist "%MYSQLDUMP%" (
    echo [ERROR] mysqldump.exe not found at:
    echo         %MYSQLDUMP%
    echo         Edit MYSQL_BIN at the top of this script.
    goto :fail
)

if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"

echo Backing up ...
"%MYSQLDUMP%" -h %DB_HOST% -P %DB_PORT% -u %DB_USER% -p%DB_PASS% --routines --events --single-transaction %DB_NAME% > "%OUT_FILE%"
if errorlevel 1 goto :fail

echo(
echo ==========================================================
echo   SUCCESS - backup saved:
echo   %OUT_FILE%
echo ==========================================================
goto :end

:fail
echo(
echo ==========================================================
echo   FAILED - see the message above.
echo ==========================================================
REM Remove a half-written file so it can't be mistaken for good
if exist "%OUT_FILE%" del "%OUT_FILE%"

:end
echo(
pause
endlocal
