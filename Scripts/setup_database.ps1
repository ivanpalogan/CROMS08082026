<#
============================================================
 CROMS - Database Setup (PowerShell)
 One script for both jobs:
   Import  -> drop + recreate "croms" and load newonw.sql
   Backup  -> dump live "croms" to a timestamped .sql

 Run (no menu):
   powershell -ExecutionPolicy Bypass -File setup_database.ps1 -Action import
   powershell -ExecutionPolicy Bypass -File setup_database.ps1 -Action backup
 Run (menu): just double-click, or:
   powershell -ExecutionPolicy Bypass -File setup_database.ps1
============================================================
#>

param(
    [ValidateSet('import','backup','menu')]
    [string]$Action = 'menu'
)

# ---- Settings (edit these if your setup differs) -----------
$MysqlBin = 'C:\Program Files\MySQL\MySQL Server 9.3\bin'
$DbHost   = 'localhost'
$DbPort   = '3306'
$DbUser   = 'root'
$DbPass   = '@ivan123'
$DbName   = 'croms'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BackupDir = Join-Path $ScriptDir 'Backups'
# Dump file: next to this script first, then Downloads
$Dump = Join-Path $ScriptDir 'newonw.sql'
if (-not (Test-Path $Dump)) { $Dump = Join-Path $env:USERPROFILE 'Downloads\newonw.sql' }
# ------------------------------------------------------------

$Mysql     = Join-Path $MysqlBin 'mysql.exe'
$MysqlDump = Join-Path $MysqlBin 'mysqldump.exe'

function Fail($msg) {
    Write-Host ''
    Write-Host '==========================================================' -ForegroundColor Red
    Write-Host "  FAILED - $msg" -ForegroundColor Red
    Write-Host '==========================================================' -ForegroundColor Red
    Write-Host ''
    Read-Host 'Press Enter to close'
    exit 1
}

function Ok($msg) {
    Write-Host ''
    Write-Host '==========================================================' -ForegroundColor Green
    Write-Host "  SUCCESS - $msg" -ForegroundColor Green
    Write-Host '==========================================================' -ForegroundColor Green
}

function Do-Import {
    Write-Host ''
    Write-Host '=========================================================='
    Write-Host '  CROMS - Import / Restore Database'
    Write-Host '=========================================================='
    Write-Host "  Server : $DbHost`:$DbPort"
    Write-Host "  Target : $DbName   (will be DROPPED and recreated)"
    Write-Host "  Dump   : $Dump"
    Write-Host '=========================================================='
    Write-Host ''

    if (-not (Test-Path $Mysql)) { Fail "mysql.exe not found at $Mysql (edit `$MysqlBin)" }
    if (-not (Test-Path $Dump))  { Fail "dump not found at $Dump (put newonw.sql beside this script or in Downloads)" }

    Write-Host 'This will PERMANENTLY DELETE the current "' -NoNewline
    Write-Host $DbName -NoNewline -ForegroundColor Yellow
    Write-Host '" database and replace it with the dump.'
    $ok = Read-Host 'Type YES to continue'
    if ($ok -ne 'YES') { Write-Host 'Cancelled. Nothing changed.'; return }

    Write-Host ''
    Write-Host "[1/2] Recreating database `"$DbName`" ..."
    $create = "DROP DATABASE IF EXISTS ``$DbName``; CREATE DATABASE ``$DbName`` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;"
    & $Mysql -h $DbHost -P $DbPort -u $DbUser "-p$DbPass" -e $create
    if ($LASTEXITCODE -ne 0) { Fail 'could not recreate the database (check server + password)' }

    Write-Host '[2/2] Importing dump (this may take a moment) ...'
    # cmd.exe handles the "<" input redirection reliably for large dumps
    cmd /c "`"$Mysql`" -h $DbHost -P $DbPort -u $DbUser -p$DbPass $DbName < `"$Dump`""
    if ($LASTEXITCODE -ne 0) { Fail 'import failed while loading the dump' }

    Ok "`"$DbName`" imported and ready."
}

function Do-Backup {
    $stamp   = Get-Date -Format 'yyyy-MM-dd_HHmmss'
    $outFile = Join-Path $BackupDir "croms_$stamp.sql"

    Write-Host ''
    Write-Host '=========================================================='
    Write-Host '  CROMS - Backup Database'
    Write-Host '=========================================================='
    Write-Host "  Source : $DbName on $DbHost`:$DbPort"
    Write-Host "  Output : $outFile"
    Write-Host '=========================================================='
    Write-Host ''

    if (-not (Test-Path $MysqlDump)) { Fail "mysqldump.exe not found at $MysqlDump (edit `$MysqlBin)" }
    if (-not (Test-Path $BackupDir)) { New-Item -ItemType Directory -Path $BackupDir | Out-Null }

    Write-Host 'Backing up ...'
    cmd /c "`"$MysqlDump`" -h $DbHost -P $DbPort -u $DbUser -p$DbPass --routines --events --single-transaction $DbName > `"$outFile`""
    if ($LASTEXITCODE -ne 0) {
        if (Test-Path $outFile) { Remove-Item $outFile -Force }
        Fail 'mysqldump failed'
    }

    Ok "backup saved: $outFile"
}

switch ($Action) {
    'import' { Do-Import }
    'backup' { Do-Backup }
    default  {
        Write-Host ''
        Write-Host '  CROMS Database Setup'
        Write-Host '  --------------------'
        Write-Host '  [1] Import / Restore  (load newonw.sql into croms)'
        Write-Host '  [2] Backup            (dump croms to a .sql file)'
        Write-Host '  [Q] Quit'
        Write-Host ''
        $c = Read-Host 'Choose'
        switch ($c) {
            '1' { Do-Import }
            '2' { Do-Backup }
            default { Write-Host 'Bye.'; exit 0 }
        }
    }
}

Write-Host ''
Read-Host 'Press Enter to close'
