<#
.SYNOPSIS
  OPTIONAL one-way refresh live -> demo. Reloads the REFERENCE tables only.
.DESCRIPTION
  Personal data is NOT copied. The registry tables (births, deaths, marriages, licences, queue tickets,
  payments, scans, history, audit...) hold names, addresses, ID numbers, contact numbers, photographs
  and signatures spread over hundreds of free-text and binary columns. A column-by-column mask for
  them cannot be made trustworthy (a missed column = a real citizen in a demo), so those tables are
  SKIPPED on purpose and the demo's people come from the sample-data seeder instead
  (Scripts\SampleData). What this does copy is the non-personal reference set (provinces,
  municipalities, barangays, fees, settings, lookups, templates, windows...), read-only on croms.
  Refuses croms and any schema not named croms_demo* / croms_test*.
.PARAMETER Schema   Target schema (default croms_demo).
.PARAMETER Confirm  Required.
#>
param(
    [string]$Schema = 'croms_demo',
    [switch]$Confirm
)
. "$PSScriptRoot\_Common.ps1"

Confirm-Destructive $Schema "RELOAD the reference tables from the live schema" -Confirm:$Confirm
if (-not (Test-SchemaExists $Schema)) { throw "Schema '$Schema' does not exist." }
Copy-ReferenceData $Schema
Write-Host ("Reloaded {0} reference tables." -f $script:ReferenceTables.Count)
Write-Host "Skipped on purpose (personal data, masking not trustworthy):"
$skipped = (Get-BaseTables $script:LiveSchema) | Where-Object { ($script:ReferenceTables -notcontains $_) -and ($script:KeepTables -notcontains $_) }
Write-Host ("  " + ($skipped -join ', '))
