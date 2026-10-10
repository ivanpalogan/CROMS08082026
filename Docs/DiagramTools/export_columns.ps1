# Writes columns.tsv (table, ordinal, column, type, key) from information_schema of the DEMO schema.
# Login comes from the app's own configuration through Scripts\Environment\_Common.ps1 - nothing is stored here.
param([string]$Schema = 'croms_demo', [string]$Out = "$PSScriptRoot\columns.tsv")
. "$PSScriptRoot\..\..\Scripts\Environment\_Common.ps1"
Assert-DisposableSchema $Schema
$o = Invoke-MySql -Batch -Sql "SELECT table_name, ordinal_position, column_name, column_type, column_key FROM information_schema.columns WHERE table_schema='$Schema' ORDER BY table_name, ordinal_position;"
[IO.File]::WriteAllLines($Out, [string[]]$o)
"wrote $($o.Count) lines to $Out"
