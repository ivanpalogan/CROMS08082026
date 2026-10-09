<#
.SYNOPSIS
  Run the CROMS.MarriageTest suites against the DEMO schema and prove the live schema did not change.
.DESCRIPTION
  1. fingerprints croms (every table's exact row count + table / column / view counts);
  2. makes sure the demo folder is current (Start-DemoEnvironment.ps1 -Launch None);
  3. writes CROMS.MarriageTest.exe.config pointing at the demo schema and runs the chosen modes;
  4. fingerprints croms again and fails if anything differs.
.PARAMETER Modes   Test modes to run; '' is the default suite (expects PASSED 77). Default: '', --birthtest, --recycle, --envbadge.
.PARAMETER Schema  Demo schema (default croms_demo).
#>
param(
    [string]$Schema = 'croms_demo',
    [string[]]$Modes = @('', '--birthtest', '--recycle', '--envbadge')
)
. "$PSScriptRoot\_Common.ps1"
Assert-DisposableSchema $Schema
$sec = Get-DemoSecret

$before = Get-SchemaFingerprint $script:LiveSchema
& "$PSScriptRoot\Start-DemoEnvironment.ps1" -Schema $Schema -Launch None | Out-Null

$appDir = Join-Path $env:LOCALAPPDATA 'CROMS\Demo\App'
$tpl = Join-Path $script:Repo 'CROMS\bin\Debug\CROMS.MarriageTest.exe.config'
[xml]$x = Get-Content -LiteralPath $tpl -Raw
($x.configuration.connectionStrings.add | Where-Object { $_.name -eq 'Croms' }).connectionString =
    "server=localhost;port=3306;database=$Schema;user id=$($sec.User);password=$($sec.Password);SslMode=Required;AllowPublicKeyRetrieval=True;Convert Zero Datetime=True;"
$cfg = Join-Path $appDir 'CROMS.MarriageTest.exe.config'
$x.Save($cfg); & icacls $cfg /inheritance:r /grant:r "$($env:USERNAME):(R,W)" | Out-Null

$exe = Join-Path $appDir 'CROMS.MarriageTest.exe'
$allOk = $true
foreach ($m in $Modes) {
    $label = if ($m) { $m } else { '(default suite)' }
    Write-Host ">>> $label against $Schema" -ForegroundColor Cyan
    $out = if ($m) { & $exe $m 2>&1 } else { & $exe 2>&1 }
    $summary = @($out | Where-Object { $_ -match 'PASSED\s+\d+' }) | Select-Object -Last 1
    $fails = @($out | Where-Object { $_ -match '^\s*FAIL' })
    Write-Host ("    " + $summary)
    if ($fails.Count) { $allOk = $false; $fails | Select-Object -First 8 | ForEach-Object { Write-Host ("    " + $_) -ForegroundColor Red } }
    if (-not $summary) { $allOk = $false; Write-Host "    (no PASSED line - last output:)"; $out | Select-Object -Last 5 | ForEach-Object { Write-Host "    $_" } }
}

$after = Get-SchemaFingerprint $script:LiveSchema
$diff = Compare-Object $before $after
if ($diff) { $allOk = $false; Write-Host "LIVE SCHEMA CHANGED:" -ForegroundColor Red; $diff | Out-String | Write-Host }
else { Write-Host ("croms unchanged by this run: {0} fingerprint lines (table/column/view counts + every table's row count) identical." -f $after.Count) -ForegroundColor Green }
if (-not $allOk) { exit 1 }
