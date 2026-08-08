@echo off
REM ============================================================
REM  CROMS - Import / Restore Database
REM  Drops the existing "croms" database, recreates it, and
REM  imports the dump file (newonw.sql) into it.
REM
REM  USE THIS to set up CROMS on a fresh machine, or to reset
REM  the database back to the dump for a demo run.
REM ============================================================

setlocal

REM ---- Settings (edit these if your setup differs) -----------
set "MYSQL_BIN=C:\Program Files\MySQL\MySQL Server 9.3\bin"
set "DB_HOST=localhost"
set "DB_PORT=3306"
set "DB_USER=root"
set "DB_PASS=@ivan123"
set "DB_NAME=croms"

REM Dump file: looked for next to this script first, then Downloads
set "DUMP=%~dp0newonw.sql"
if not exist "%DUMP%" set "DUMP=%USERPROFILE%\Downloads\newonw.sql"
REM ------------------------------------------------------------

set "MYSQL=%MYSQL_BIN%\mysql.exe"

echo(
echo ==========================================================
echo   CROMS - Import / Restore Database
echo ==========================================================
echo   Server : %DB_HOST%:%DB_PORT%
echo   Target : %DB_NAME%   (will be DROPPED and recreated)
echo   Dump   : %DUMP%
echo ==========================================================
echo(

if not exist "%MYSQL%" (
    echo [ERROR] mysql.exe not found at:
    echo         %MYSQL%
    echo         Edit MYSQL_BIN at the top of this script.
    goto :fail
)
if not exist "%DUMP%" (
    echo [ERROR] Dump file not found:
    echo         %DUMP%
    echo         Put newonw.sql next to this script or in Downloads.
    goto :fail
)

echo This will PERMANENTLY DELETE the current "%DB_NAME%" database
echo and replace it with the contents of the dump file.
echo(
set /p "OK=Type YES to continue: "
if /I not "%OK%"=="YES" (
    echo Cancelled. Nothing changed.
    goto :end
)

echo(
echo [1/2] Recreating database "%DB_NAME%" ...
"%MYSQL%" -h %DB_HOST% -P %DB_PORT% -u %DB_USER% -p%DB_PASS% -e "DROP DATABASE IF EXISTS `%DB_NAME%`; CREATE DATABASE `%DB_NAME%` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;"
if errorlevel 1 goto :fail

echo [2/2] Importing dump (this may take a moment) ...
"%MYSQL%" -h %DB_HOST% -P %DB_PORT% -u %DB_USER% -p%DB_PASS% %DB_NAME% < "%DUMP%"
if errorlevel 1 goto :fail

echo(
echo ==========================================================
echo   SUCCESS - "%DB_NAME%" imported and ready.
echo ==========================================================
goto :end

:fail
echo(
echo ==========================================================
echo   FAILED - see the message above.
echo ==========================================================

:end
echo(
pause
endlocal
