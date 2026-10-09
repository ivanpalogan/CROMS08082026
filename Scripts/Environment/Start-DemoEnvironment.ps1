<#
.SYNOPSIS
  Start CROMS (and optionally the kiosk / display) against the DEMO schema, WITHOUT touching the live config.
.DESCRIPTION
  Choice made: a separate output folder. The built executables are copied from CROMS\bin\Debug,
  CROMS.Kiosk\bin\Debug and CROMS.Display\bin\Debug into %LOCALAPPDATA%\CROMS\Demo\App (outside the repo
  and outside OneDrive), and a DEMO .exe.config is written beside each exe. Why not APP_CONFIG_FILE: an
  environment variable is not honoured by the Windows shortcut / launcher path and the apps start each
  other (the launcher starts the kiosk and display), so a per-folder .config is the only form that every
  child process inherits automatically. The live bin\Debug configs are never edited.
  The demo database password is read from the DPAPI file, written into the generated configs only
  (ACL: current user), and never echoed.
  The apps themselves show a DEMO ENVIRONMENT badge whenever the connected schema is not "croms".
.PARAMETER Schema   Demo schema (default croms_demo).
.PARAMETER Launch   Which exe to open: Main (default, the launcher), Kiosk, Display, All.
.PARAMETER NoCopy   Reuse the folder as it is (skip re-copying binaries).
#>
param(
    [string]$Schema = 'croms_demo',
    [ValidateSet('Main','Kiosk','Display','All','None')][string]$Launch = 'Main',
    [switch]$NoCopy
)
. "$PSScriptRoot\_Common.ps1"
Assert-DisposableSchema $Schema

$sec = Get-DemoSecret
if ($sec.Schema -ne $Schema) { throw "The registered demo environment is '$($sec.Schema)', not '$Schema'." }
if (-not (Test-SchemaExists $Schema)) { throw "Schema '$Schema' does not exist. Run New-DemoEnvironment.ps1." }

# The apps read the DB host from %APPDATA%\CROMS\server.cfg (shared with the live install). The demo
# account only exists on localhost, so a remote host saved there would silently fail or re-point.
$sc = Join-Path $script:DemoRoot 'server.cfg'
if (Test-Path $sc) {
    $h = (Get-Content $sc | Where-Object { $_ -match '^\s*host=' } | Select-Object -First 1) -replace '^\s*host=',''
    if ($h -and $h.Trim() -notin @('','localhost','127.0.0.1','::1','.')) {
        throw "server.cfg points at '$($h.Trim())'. The demo database lives on THIS PC (localhost). Run the demo on the server PC, or set host=localhost."
    }
}

# Prove the account really reaches the demo schema and only that one.
$probe = Invoke-MySql -Schema $Schema -User $sec.User -Password $sec.Password -Batch -Sql "SELECT DATABASE();" | Select-Object -First 1
if ($probe -ne $Schema) { throw "Demo login did not land on '$Schema' (got '$probe')." }

$appDir = Join-Path $env:LOCALAPPDATA 'CROMS\Demo\App'
New-Item -ItemType Directory -Force -Path $appDir | Out-Null
$srcs = @(
    @{ Name='CROMS';         Dir=(Join-Path $script:Repo 'CROMS\bin\Debug') },
    @{ Name='CROMS.Kiosk';   Dir=(Join-Path $script:Repo 'CROMS.Kiosk\bin\Debug') },
    @{ Name='CROMS.Display'; Dir=(Join-Path $script:Repo 'CROMS.Display\bin\Debug') }
)
foreach ($s in $srcs) { if (-not (Test-Path (Join-Path $s.Dir ($s.Name + '.exe')))) { throw "Build $($s.Name) first - $($s.Dir)\$($s.Name).exe not found." } }

if (-not $NoCopy) {
    Write-Host "Copying the built applications to $appDir ..."
    foreach ($s in $srcs) {
        robocopy $s.Dir $appDir /E /XO /XF *.config *.pdb /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "robocopy failed for $($s.Name) ($LASTEXITCODE)" }
    }
}

$cs = "server=localhost;port=3306;database=$Schema;user id=$($sec.User);password=$($sec.Password);SslMode=Required;AllowPublicKeyRetrieval=True;Convert Zero Datetime=True;"
foreach ($s in $srcs) {
    $tpl = Join-Path $s.Dir ($s.Name + '.exe.config')
    if (-not (Test-Path $tpl)) { throw "$tpl not found." }
    [xml]$x = Get-Content -LiteralPath $tpl -Raw
    $node = $x.configuration.connectionStrings.add | Where-Object { $_.name -eq 'Croms' }
    if (-not $node) { throw "$($s.Name).exe.config has no 'Croms' connection string." }
    $node.connectionString = $cs
    $apps = $x.configuration.appSettings
    if ($apps) {
        foreach ($kv in @{ 'IonicAutoStart'='false'; 'LanDbUser'=''; 'LanDbPassword'='' }.GetEnumerator()) {
            $e = $apps.add | Where-Object { $_.key -eq $kv.Key }
            if ($e) { $e.value = $kv.Value }
        }
    }
    $out = Join-Path $appDir ($s.Name + '.exe.config')
    $x.Save($out)
    & icacls $out /inheritance:r /grant:r "$($env:USERNAME):(R,W)" | Out-Null
}
Write-Host "Demo configuration written (database=$Schema, user=$($sec.User))."

$map = @{ Main='CROMS.exe'; Kiosk='CROMS.Kiosk.exe'; Display='CROMS.Display.exe' }
$todo = switch ($Launch) { 'All' { @('Main','Kiosk','Display') } 'None' { @() } default { @($Launch) } }
foreach ($k in $todo) { Start-Process -FilePath (Join-Path $appDir $map[$k]) -WorkingDirectory $appDir }
if ($todo.Count) { Write-Host ("Started: " + ($todo -join ', ') + "  - look for the orange DEMO ENVIRONMENT badge.") }
Write-Host "Demo folder: $appDir"
