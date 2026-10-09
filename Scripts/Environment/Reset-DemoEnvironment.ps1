<#
.SYNOPSIS
  Empty every TRANSACTIONAL table of a croms_demo* / croms_test* schema and reload the REFERENCE data.
.DESCRIPTION
  Transactional = every base table that is not a reference table and not users / _env_migrations
  (see _Common.ps1 and Docs\Environment_Design.md). The two demo logins and the migration ledger are kept.
  Safe to run twice. Refuses croms and any name that does not start with croms_demo / croms_test.
.PARAMETER Schema   Target schema (default croms_demo).
.PARAMETER Confirm  Required.
#>
param(
    [string]$Schema = 'croms_demo',
    [switch]$Confirm
)
. "$PSScriptRoot\_Common.ps1"

Confirm-Destructive $Schema "EMPTY all transactional tables and reload reference data" -Confirm:$Confirm
if (-not (Test-SchemaExists $Schema)) { throw "Schema '$Schema' does not exist." }

$all = Get-BaseTables $Schema
$txn = @($all | Where-Object { ($script:ReferenceTables -notcontains $_) -and ($script:KeepTables -notcontains $_) })
$sb = New-Object Text.StringBuilder
[void]$sb.AppendLine('SET FOREIGN_KEY_CHECKS=0;')
foreach ($t in $txn) { [void]$sb.AppendLine("TRUNCATE TABLE ``$Schema``.``$t``;") }
[void]$sb.AppendLine('SET FOREIGN_KEY_CHECKS=1;')
Invoke-MySql -Sql $sb.ToString() | Out-Null
Write-Host ("Emptied {0} transactional tables." -f $txn.Count)

Copy-ReferenceData $Schema
Write-Host ("Reloaded {0} reference tables from the live schema (read-only on croms)." -f $script:ReferenceTables.Count)
