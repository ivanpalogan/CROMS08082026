<#
.SYNOPSIS
  Create the isolated DEMO / TEST copy of CROMS (schema croms_demo + its own restricted MySQL account).
.DESCRIPTION
  1. structure-only dump of the live schema (mysqldump --no-data --routines --triggers), with every
     schema-qualified name rewritten so the demo views point at the DEMO tables, never at croms;
  2. create the schema (same charset / collation as live), load the structure;
  3. create the restricted account 'croms_demo_app'@'localhost' (rights on the demo schema ONLY);
  4. copy REFERENCE data live -> demo (read-only on croms);
  5. create the demo logins (demoadmin must change its password at first sign-in);
  6. record the migration baseline and apply anything newer (Apply-Migrations.ps1).
  The generated passwords are printed ONCE and stored only DPAPI-protected under %APPDATA%\CROMS.
  The live schema is never written. Refuses any schema name that does not start with croms_demo / croms_test.
.PARAMETER Schema   Target schema (default croms_demo).
.PARAMETER Recreate Drop and rebuild if it already exists.
.PARAMETER Confirm  Required. Nothing runs without it.
#>
param(
    [string]$Schema = 'croms_demo',
    [switch]$Recreate,
    [switch]$Confirm
)
. "$PSScriptRoot\_Common.ps1"

Confirm-Destructive $Schema "CREATE the demo environment" -Confirm:$Confirm

if (Test-SchemaExists $Schema) {
    if (-not $Recreate) { throw "Schema '$Schema' already exists. Use Reset-DemoEnvironment.ps1 to empty it, or add -Recreate to rebuild it." }
    Write-Host "Dropping existing '$Schema' (-Recreate)..." -ForegroundColor Yellow
    Invoke-MySql -Sql "DROP DATABASE ``$Schema``;" | Out-Null
}

# 1. Structure-only dump of the live schema --------------------------------------------------------
$tmp = Join-Path $env:TEMP ("croms_structure_" + [guid]::NewGuid().ToString('N') + '.sql')
try {
    Write-Host "Reading the live structure (no data)..."
    Invoke-MySqlDump -Schema $script:LiveSchema -OutFile $tmp -Extra @('--no-data','--routines','--triggers','--skip-add-drop-table')
    $text = [IO.File]::ReadAllText($tmp, [Text.Encoding]::UTF8)
    # Views are dumped with schema-qualified names (`croms`.`births`); left alone they would read the LIVE tables.
    $text = $text -replace '`croms`\.', ('`' + $Schema + '`.')
    if ($text -match '(?i)`croms`') { throw "The structure dump still names the live schema; refusing to load it." }
    [IO.File]::WriteAllText($tmp, $text, (New-Object Text.UTF8Encoding($false)))

    # 2. Schema + structure ----------------------------------------------------------------------
    $cs = Invoke-MySql -Batch -Sql "SELECT default_character_set_name, default_collation_name FROM information_schema.schemata WHERE schema_name='$($script:LiveSchema)';" | Select-Object -First 1
    $charset, $collation = $cs -split "`t"
    Invoke-MySql -Sql "CREATE DATABASE ``$Schema`` CHARACTER SET $charset COLLATE $collation;" | Out-Null
    Write-Host "Loading the structure into '$Schema'..."
    Invoke-MySql -Schema $Schema -File $tmp | Out-Null
} finally { Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue }

$tables = Get-BaseTables $Schema
Write-Host ("  {0} tables created." -f $tables.Count)

# 3. Restricted account ----------------------------------------------------------------------------
$appPw = New-RandomPassword 24
$u = $script:DemoAccountUser
Invoke-MySql -Sql @"
CREATE USER IF NOT EXISTS '$u'@'localhost' IDENTIFIED BY '$appPw';
ALTER USER '$u'@'localhost' IDENTIFIED BY '$appPw';
REVOKE ALL PRIVILEGES, GRANT OPTION FROM '$u'@'localhost';
GRANT SELECT, INSERT, UPDATE, DELETE, CREATE TEMPORARY TABLES, LOCK TABLES, SHOW VIEW, EXECUTE ON ``$Schema``.* TO '$u'@'localhost';
FLUSH PRIVILEGES;
"@ | Out-Null
Save-DemoSecret $Schema $u $appPw
Write-Host "  account '$u'@'localhost' created (rights on '$Schema' only)."

# 4. Reference data live -> demo -------------------------------------------------------------------
Write-Host "Copying reference data from the live schema (read-only on croms)..."
Copy-ReferenceData $Schema

# 5. Demo logins -----------------------------------------------------------------------------------
$adminPw = New-RandomPassword 16
$staffPw = New-RandomPassword 16
$ah = New-PasswordHash $adminPw
$sh = New-PasswordHash $staffPw
Invoke-MySql -Schema $Schema -Sql @"
DELETE FROM users;
INSERT INTO users (username, password_hash, full_name, role, is_active, must_change_password)
VALUES ('demoadmin', '$ah', 'Demo Administrator', 'Admin', 1, 1),
       ('demostaff', '$sh', 'Demo Staff', 'Staff', 1, 0);
"@ | Out-Null

# 6. Migration ledger: the live structure is at the repo head (migration 86) ----------------------
Invoke-MySql -Schema $Schema -Sql @"
CREATE TABLE IF NOT EXISTS _env_migrations (
  file_name  VARCHAR(120) NOT NULL PRIMARY KEY,
  applied_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  note       VARCHAR(40) NULL
) ENGINE=InnoDB;
"@ | Out-Null
$dbDir = Join-Path $script:Repo 'CROMS\Database'
$baseline = Get-ChildItem $dbDir -Filter '*.sql' | Where-Object { $_.Name -match '^(\d+)_' -and [int]$matches[1] -le 86 }
$ins = New-Object Text.StringBuilder
foreach ($f in $baseline) { [void]$ins.AppendLine("INSERT IGNORE INTO _env_migrations (file_name, note) VALUES ('" + $f.Name.Replace("'","''") + "', 'baseline');") }
Invoke-MySql -Schema $Schema -Sql $ins.ToString() | Out-Null
Write-Host ("  migration baseline recorded ({0} files up to 86)." -f @($baseline).Count)

& "$PSScriptRoot\Apply-Migrations.ps1" -Schema $Schema -Confirm:$true

Write-Host ""
Write-Host "==================== DEMO ENVIRONMENT READY ====================" -ForegroundColor Green
Write-Host " Schema      : $Schema"
Write-Host " DB account  : $u@localhost   (password stored DPAPI-protected, not shown)"
Write-Host " App login 1 : demoadmin   password: $adminPw    (Admin - must change it at first sign-in)"
Write-Host " App login 2 : demostaff   password: $staffPw    (Staff)"
Write-Host " THESE PASSWORDS ARE SHOWN ONLY NOW. Write them down; they are not stored anywhere readable."
Write-Host " Start it with: Scripts\Environment\Start-DemoEnvironment.ps1"
Write-Host "================================================================"
