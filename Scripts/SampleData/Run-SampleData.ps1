<#
.SYNOPSIS
  Run the sample-data seeder (CROMS.SampleData.exe) against the DEMO schema or the LIVE registry.
.DESCRIPTION
  -Target Demo  uses the croms_demo account stored by Scripts\Environment (DPAPI).
  -Target Live  uses the application's own database login (CROMS\App.config), takes a mysqldump backup
                first (to C:\Users\<you>\CROMS_Backups\<date>_pre-sample-data), and needs -ConfirmLive.
  The connection string is written next to a COPY of the executables in %LOCALAPPDATA% (never into the
  repo or OneDrive) and deleted when the run ends.
  Actions: Seed (default), Cleanup (removes only rows tagged "SAMPLE DATA 2026"), Counts.
.PARAMETER Only   Optional comma list: births,deaths,marriages,petitions,certs,breqs,queue.
#>
param(
    [ValidateSet('Demo','Live')][string]$Target = 'Demo',
    [ValidateSet('Seed','Cleanup','Counts')][string]$Action = 'Seed',
    [string]$Only = '',
    [string]$DemoSchema = 'croms_demo',
    [switch]$ConfirmLive
)
$envDir = Join-Path (Split-Path -Parent $PSScriptRoot) 'Environment'
. "$envDir\_Common.ps1"

if ($Target -eq 'Live' -and $Action -ne 'Counts' -and -not $ConfirmLive) { throw "REFUSED: the LIVE registry needs -ConfirmLive." }
if ($Target -eq 'Demo') { Assert-DisposableSchema $DemoSchema }

$bin = Join-Path $script:Repo 'CROMS\bin\Debug'
if (-not (Test-Path (Join-Path $bin 'CROMS.SampleData.exe'))) { throw "Build CROMS.SampleData first (msbuild CROMS.SampleData\CROMS.SampleData.csproj)." }

# connection
if ($Target -eq 'Demo') {
    $sec = Get-DemoSecret; $db = $sec.Schema; $user = $sec.User; $pw = $sec.Password
} else {
    $a = Get-AdminConnection; $db = $script:LiveSchema; $user = $a.User; $pw = $a.Password
    if ($Action -eq 'Seed') {
        $bk = "C:\Users\$env:USERNAME\CROMS_Backups\$(Get-Date -Format 'yyyy-MM-dd')_pre-sample-data\croms.sql"
        if (Test-Path $bk) { $bk = $bk -replace 'croms\.sql$', ('croms_' + (Get-Date -Format 'HHmmss') + '.sql') }   # never overwrite an earlier backup
        Write-Host "Backing up the live schema first -> $bk"
        Invoke-MySqlDump -Schema $script:LiveSchema -OutFile $bk -Extra @('--routines','--triggers')
    }
}

$run = Join-Path $env:LOCALAPPDATA ('CROMS\SampleRun_' + $Target)
New-Item -ItemType Directory -Force -Path $run | Out-Null
robocopy $bin $run /E /XO /XF *.config *.pdb /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed ($LASTEXITCODE)" }
$cfg = Join-Path $run 'CROMS.SampleData.exe.config'
try {
    [xml]$x = Get-Content -LiteralPath (Join-Path $bin 'CROMS.SampleData.exe.config') -Raw
    ($x.configuration.connectionStrings.add | Where-Object { $_.name -eq 'Croms' }).connectionString =
        "server=localhost;port=3306;database=$db;user id=$user;password=$pw;SslMode=Required;AllowPublicKeyRetrieval=True;Convert Zero Datetime=True;"
    $x.Save($cfg); & icacls $cfg /inheritance:r /grant:r "$($env:USERNAME):(R,W)" | Out-Null

    $argv = @('--database', $db)
    switch ($Action) { 'Seed' { $argv += '--seed'; if ($Only) { $argv += @('--only', $Only) } } 'Cleanup' { $argv += @('--cleanup','--confirm') } 'Counts' { $argv += '--counts' } }
    Push-Location $run
    & (Join-Path $run 'CROMS.SampleData.exe') @argv
    $rc = $LASTEXITCODE
    Pop-Location
    if ($rc -ne 0) { exit $rc }
} finally {
    Remove-Item -LiteralPath $cfg -Force -ErrorAction SilentlyContinue
}
