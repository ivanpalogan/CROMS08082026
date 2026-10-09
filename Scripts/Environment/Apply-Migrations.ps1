<#
.SYNOPSIS
  Apply CROMS\Database\NN_*.sql to a croms_demo* / croms_test* schema, in numeric order, once each.
.DESCRIPTION
  The demo schema keeps a ledger (table _env_migrations). A file already in the ledger is skipped, so
  running this twice changes nothing the second time. This matters because an old migration re-run after
  migration 85 (the table renames) would re-create the OLD table names as empty tables.
  Refuses the live schema. A pending file that names the live schema (USE croms, `croms`.x) is refused
  rather than rewritten.
.PARAMETER Schema   Target schema (default croms_demo).
.PARAMETER Confirm  Required.
.PARAMETER Only     Optional single file name to apply (still recorded in the ledger).
#>
param(
    [string]$Schema = 'croms_demo',
    [switch]$Confirm,
    [string]$Only = ''
)
. "$PSScriptRoot\_Common.ps1"

Confirm-Destructive $Schema "APPLY migrations" -Confirm:$Confirm
if (-not (Test-SchemaExists $Schema)) { throw "Schema '$Schema' does not exist." }

Invoke-MySql -Schema $Schema -Sql @"
CREATE TABLE IF NOT EXISTS _env_migrations (
  file_name  VARCHAR(120) NOT NULL PRIMARY KEY,
  applied_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  note       VARCHAR(40) NULL
) ENGINE=InnoDB;
"@ | Out-Null

$done = @{}
foreach ($r in (Invoke-MySql -Schema $Schema -Batch -Sql "SELECT file_name FROM _env_migrations;")) { $done[[string]$r] = $true }

$dbDir = Join-Path $script:Repo 'CROMS\Database'
$files = Get-ChildItem $dbDir -Filter '*.sql' | Where-Object { $_.Name -match '^(\d+)_' } |
         Sort-Object { [int]($_.Name -replace '^(\d+)_.*','$1') }, Name
if ($Only) { $files = $files | Where-Object { $_.Name -eq $Only } }

$ran = @(); $skipped = 0
foreach ($f in $files) {
    if ($done.ContainsKey($f.Name)) { $skipped++; continue }
    $body = [IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8)
    if ($body -match '(?i)\buse\s+`?croms`?\s*;' -or $body -match '(?i)`croms`\.') {
        throw "Migration $($f.Name) names the live schema (USE croms / `croms`.x). Not applied; fix the file or apply it by hand."
    }
    Write-Host ("  applying {0}" -f $f.Name)
    Invoke-MySql -Schema $Schema -File $f.FullName | Out-Null
    Invoke-MySql -Schema $Schema -Sql ("INSERT IGNORE INTO _env_migrations (file_name, note) VALUES ('" + $f.Name.Replace("'","''") + "', 'applied');") | Out-Null
    $ran += $f.Name
}
Write-Host ("Migrations on '{0}': {1} applied now, {2} already in the ledger." -f $Schema, $ran.Count, $skipped)
if ($ran.Count -eq 0) { Write-Host "  (nothing to do)" }
