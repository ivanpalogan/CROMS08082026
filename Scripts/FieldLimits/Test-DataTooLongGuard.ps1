$App = 'CROMS'
. "$PSScriptRoot\_Init.ps1"
# Proves the server-side guard for MySQL error 1406 ("Data too long"): a deliberately over-long
# write is rejected by MySQL (nothing is stored) and ErrorLog turns the exception into a sentence
# that names the column and its limit. Uses `windows.window_name` (VARCHAR(50)); the INSERT fails,
# so no row is ever written.
$results = New-Object System.Collections.ArrayList
function Add-Result($name, $pass, $detail) { [void]$results.Add([pscustomobject]@{ Check = $name; Pass = [bool]$pass; Detail = $detail }) }

$db  = $asm.GetType('CROMS.Data.Db')
$log = $asm.GetType('CROMS.Data.ErrorLog')
$pub = [Reflection.BindingFlags]'Static,Public,NonPublic'
$before = [int](Sql "SELECT COUNT(*) FROM windows")

# build the over-long INSERT through the app's own Db.Push(sql, params)
$sql = "INSERT INTO windows (window_name, status, display_order) VALUES (@n, 'Inactive', 9999)"
$p   = New-Object MySql.Data.MySqlClient.MySqlParameter('@n', ('x' * 60))
$ex  = $null
try {
    $m = $db.GetMethod('Push', [Type[]]@([string], [MySql.Data.MySqlClient.MySqlParameter[]]))
    [void]$m.Invoke($null, @($sql, [MySql.Data.MySqlClient.MySqlParameter[]]@($p)))
} catch { $ex = $_.Exception.InnerException; if (-not $ex) { $ex = $_.Exception } }

Add-Result 'over-long insert is refused by MySQL (error 1406)' ($ex -is [MySql.Data.MySqlClient.MySqlException] -and $ex.Number -eq 1406) $(if ($ex) { "$($ex.GetType().Name) #$($ex.Number)" } else { 'no exception' })
Add-Result 'nothing was written' ([int](Sql "SELECT COUNT(*) FROM windows") -eq $before) "windows rows: $before"
Add-Result 'Db tagged the table on the exception' ($ex -and $ex.Data['table'] -eq 'windows') $(if ($ex) { [string]$ex.Data['table'] } else { '' })
Add-Result 'ErrorLog.TooLongColumn finds the column' ($log.GetMethod('TooLongColumn', $pub).Invoke($null, @($ex)) -eq 'window_name') ''
$friendly = [string]$log.GetMethod('Friendly', $pub).Invoke($null, @($ex, 'save'))
Add-Result 'Friendly names the column and the limit' ($friendly -match 'window name' -and $friendly -match '50 characters') $friendly
$text = [string]$log.GetMethod('Text', $pub).Invoke($null, @($ex))
Add-Result 'Text() gives the same sentence' ($text -eq $friendly) ''
$other = [Exception](New-Object System.InvalidOperationException "a business rule message")
Add-Result 'Text() leaves other exceptions alone' ([string]$log.GetMethod('Text', $pub).Invoke($null, [object[]]@($other)) -eq 'a business rule message') ''

$results | Format-Table -AutoSize | Out-String -Width 200
"FAILED: " + @($results | Where-Object { -not $_.Pass }).Count + " of " + $results.Count
