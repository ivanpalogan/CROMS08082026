<#
.SYNOPSIS
  Drop a croms_demo* / croms_test* schema, its MySQL account and the local demo files.
.PARAMETER Schema   Target schema (default croms_demo).
.PARAMETER Confirm  Required.
#>
param(
    [string]$Schema = 'croms_demo',
    [switch]$Confirm
)
. "$PSScriptRoot\_Common.ps1"

Confirm-Destructive $Schema "DROP the schema, its account and the local demo files" -Confirm:$Confirm
if (Test-SchemaExists $Schema) {
    Invoke-MySql -Sql "DROP DATABASE ``$Schema``;" | Out-Null
    Write-Host "Dropped schema '$Schema'."
} else { Write-Host "Schema '$Schema' was not present." }

$u = $script:DemoAccountUser
Invoke-MySql -Sql "DROP USER IF EXISTS '$u'@'localhost';" | Out-Null
Write-Host "Dropped account '$u'@'localhost'."
Remove-DemoSecret
$demoDir = Join-Path $env:LOCALAPPDATA 'CROMS\Demo'
if (Test-Path $demoDir) { Remove-Item -LiteralPath $demoDir -Recurse -Force; Write-Host "Removed $demoDir" }
