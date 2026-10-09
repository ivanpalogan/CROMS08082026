# Shared helpers for the demo-environment scripts. Dot-source: . "$PSScriptRoot\_Common.ps1"
# No password lives in any script. The administrator login is read at run time from the app's own
# configuration (CROMS\App.config), kept in memory, and handed to the MySQL tools through a
# temporary --defaults-extra-file that is deleted afterwards.

$ErrorActionPreference = 'Stop'
$script:Repo   = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$script:MySqlBin = 'C:\Program Files\MySQL\MySQL Server 9.3\bin'
$script:LiveSchema = 'croms'
$script:DemoRoot   = Join-Path $env:APPDATA 'CROMS'

function Get-AdminConnection {
    # server/port/user/password of the account that may create schemas (from App.config, in memory only)
    $cfg = Join-Path $script:Repo 'CROMS\App.config'
    [xml]$x = Get-Content -LiteralPath $cfg -Raw
    $cs = ($x.configuration.connectionStrings.add | Where-Object { $_.name -eq 'Croms' }).connectionString
    $h = @{}
    foreach ($p in $cs -split ';') { if ($p -match '^\s*([^=]+)=(.*)$') { $h[$matches[1].Trim().ToLower()] = $matches[2] } }
    $server = $h['server']; $port = $h['port']
    $sc = Join-Path $script:DemoRoot 'server.cfg'     # the host chosen on this PC wins
    if (Test-Path $sc) {
        foreach ($l in Get-Content $sc) {
            if ($l -match '^\s*host=(.+)$') { $server = $matches[1].Trim() }
            if ($l -match '^\s*port=(\d+)') { $port = $matches[1] }
        }
    }
    [pscustomobject]@{ Server = $server; Port = $port; User = $h['user id']; Password = $h['password'] }
}

function New-MyCnf([string]$User, [string]$Password, [string]$Server, [string]$Port) {
    $f = Join-Path $env:TEMP ('croms_env_' + [guid]::NewGuid().ToString('N') + '.cnf')
    $body = "[client]`nuser=$User`npassword=`"$($Password.Replace('\','\\').Replace('"','\"'))`"`nhost=$Server`nport=$Port`ndefault-character-set=utf8mb4`n"
    [IO.File]::WriteAllText($f, $body, (New-Object Text.UTF8Encoding($false)))
    $f
}

function Invoke-MySql {
    # Run SQL text (stdin) or a file against a schema as the admin account. Returns stdout lines.
    param([string]$Schema = '', [string]$Sql, [string]$File, [switch]$Batch, [string]$User, [string]$Password)
    $a = Get-AdminConnection
    if (-not $User) { $User = $a.User; $Password = $a.Password }
    $cnf = New-MyCnf $User $Password $a.Server $a.Port
    try {
        $args = @("--defaults-extra-file=$cnf", '--default-character-set=utf8mb4')
        if ($Batch) { $args += @('-N', '-B') }
        if ($Schema) { $args += $Schema }
        if ($File) {
            $out = Get-Content -LiteralPath $File -Raw -Encoding UTF8 | & "$script:MySqlBin\mysql.exe" @args 2>&1
        } else {
            $out = $Sql | & "$script:MySqlBin\mysql.exe" @args 2>&1
        }
        if ($LASTEXITCODE -ne 0) { throw ("mysql failed: " + (($out | Out-String).Trim() -replace 'password=[^\s]+','password=***')) }
        $out
    } finally { Remove-Item -LiteralPath $cnf -Force -ErrorAction SilentlyContinue }
}

function Invoke-MySqlDump {
    param([string]$Schema, [string]$OutFile, [string[]]$Extra = @())
    $a = Get-AdminConnection
    $cnf = New-MyCnf $a.User $a.Password $a.Server $a.Port
    try {
        $dir = Split-Path -Parent $OutFile
        if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
        $args = @("--defaults-extra-file=$cnf", '--default-character-set=utf8mb4', "--result-file=$OutFile",
                  '--single-transaction', '--skip-lock-tables', '--set-gtid-purged=OFF', '--column-statistics=0') + $Extra + @($Schema)
        $out = & "$script:MySqlBin\mysqldump.exe" @args 2>&1
        if ($LASTEXITCODE -ne 0) {
            # older/newer clients reject --column-statistics; retry without
            $args = $args | Where-Object { $_ -ne '--column-statistics=0' }
            $out = & "$script:MySqlBin\mysqldump.exe" @args 2>&1
            if ($LASTEXITCODE -ne 0) { throw ("mysqldump failed: " + ($out | Out-String).Trim()) }
        }
    } finally { Remove-Item -LiteralPath $cnf -Force -ErrorAction SilentlyContinue }
}

function Assert-DisposableSchema([string]$Schema) {
    # Rule 2: every destructive script goes through this. Nothing may ever default to the live schema.
    if ([string]::IsNullOrWhiteSpace($Schema)) { throw "REFUSED: no -Schema given. Nothing defaults to '$script:LiveSchema'." }
    if ($Schema -eq $script:LiveSchema) { throw "REFUSED: '$Schema' is the LIVE registry. This script only works on croms_demo* or croms_test* schemas." }
    if ($Schema -notmatch '^(croms_demo|croms_test)[A-Za-z0-9_]*$') {
        throw "REFUSED: '$Schema' does not start with croms_demo or croms_test."
    }
}

function Confirm-Destructive([string]$Schema, [string]$What, [switch]$Confirm) {
    Assert-DisposableSchema $Schema
    $a = Get-AdminConnection
    Write-Host ("About to $What on schema '$Schema' at " + $a.Server + ':' + $a.Port) -ForegroundColor Yellow
    if (-not $Confirm) { throw "REFUSED: add -Confirm to proceed ($What on '$Schema')." }
}

function Get-SchemaFingerprint([string]$Schema) {
    # Row counts of every table + table/column counts, as sorted text lines (exact COUNT(*), not estimates).
    $tables = Invoke-MySql -Batch -Sql "SELECT table_name FROM information_schema.tables WHERE table_schema='$Schema' AND table_type='BASE TABLE' ORDER BY table_name;"
    $sql = New-Object Text.StringBuilder
    foreach ($t in $tables) { [void]$sql.AppendLine("SELECT '$t', COUNT(*) FROM ``$Schema``.``$t``;") }
    $counts = Invoke-MySql -Batch -Sql $sql.ToString()
    $meta = Invoke-MySql -Batch -Sql "SELECT 'TABLES', COUNT(*) FROM information_schema.tables WHERE table_schema='$Schema' UNION ALL SELECT 'COLUMNS', COUNT(*) FROM information_schema.columns WHERE table_schema='$Schema' UNION ALL SELECT 'VIEWS', COUNT(*) FROM information_schema.views WHERE table_schema='$Schema' UNION ALL SELECT 'ROUTINES', COUNT(*) FROM information_schema.routines WHERE routine_schema='$Schema' UNION ALL SELECT 'TRIGGERS', COUNT(*) FROM information_schema.triggers WHERE trigger_schema='$Schema';"
    @($meta) + @($counts)
}

# ----------------------------------------------------------------------------------------------
# Reference vs transactional tables (derived from the schema + code; see Docs\Environment_Design.md).
# REFERENCE = what the app needs in order to start and to offer its pick-lists. No personal data.
# EVERYTHING ELSE in the schema (except _env_migrations and users) is TRANSACTIONAL and is emptied
# by Reset-DemoEnvironment.ps1.
$script:ReferenceTables = @(
    'app_settings','barangays','birth_orders','causes_of_death','certificate_template_images',
    'certificate_templates','churches','civil_statuses','countries','document_requirement_types',
    'fees','hospitals','municipalities','nationalities','occupations','office_assets','office_profile',
    'provinces','relationships','religions','residences','type_of_births','window_service_assignments',
    'windows'
)
# Kept as they are by Reset (accounts of the demo environment itself, and the migration ledger).
$script:KeepTables = @('users','_env_migrations')
$script:DemoAccountUser = 'croms_demo_app'

function New-RandomPassword([int]$Length = 24) {
    $chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789'
    $rng = New-Object Security.Cryptography.RNGCryptoServiceProvider
    $bytes = New-Object byte[] $Length
    $rng.GetBytes($bytes)
    -join ($bytes | ForEach-Object { $chars[$_ % $chars.Length] })
}

function New-PasswordHash([string]$Password) {
    # Same format as CROMS\Data\PasswordHasher.cs: "100000.<salt b64>.<hash b64>" (PBKDF2, HMAC-SHA256).
    $salt = New-Object byte[] 16
    (New-Object Security.Cryptography.RNGCryptoServiceProvider).GetBytes($salt)
    $kdf = New-Object Security.Cryptography.Rfc2898DeriveBytes($Password, $salt, 100000, [Security.Cryptography.HashAlgorithmName]::SHA256)
    $key = $kdf.GetBytes(32)
    '100000.' + [Convert]::ToBase64String($salt) + '.' + [Convert]::ToBase64String($key)
}

function Get-SecretPath { Join-Path $script:DemoRoot 'demo-env.cfg' }

function Save-DemoSecret([string]$Schema, [string]$User, [string]$Password) {
    # DPAPI (current Windows user only), the same protection Data\MobileCaptureConfig.cs uses.
    Add-Type -AssemblyName System.Security
    if (-not (Test-Path $script:DemoRoot)) { New-Item -ItemType Directory -Force -Path $script:DemoRoot | Out-Null }
    $prot = [Security.Cryptography.ProtectedData]::Protect([Text.Encoding]::UTF8.GetBytes($Password), $null, 'CurrentUser')
    $lines = @("schema=$Schema", "user=$User", ("password=" + [Convert]::ToBase64String($prot)))
    [IO.File]::WriteAllLines((Get-SecretPath), $lines)
}

function Get-DemoSecret {
    $p = Get-SecretPath
    if (-not (Test-Path $p)) { throw "No demo environment is registered on this PC. Run New-DemoEnvironment.ps1 first." }
    Add-Type -AssemblyName System.Security
    $h = @{}
    foreach ($l in [IO.File]::ReadAllLines($p)) { if ($l -match '^([^=]+)=(.*)$') { $h[$matches[1]] = $matches[2] } }
    $pw = [Text.Encoding]::UTF8.GetString([Security.Cryptography.ProtectedData]::Unprotect([Convert]::FromBase64String($h['password']), $null, 'CurrentUser'))
    [pscustomobject]@{ Schema = $h['schema']; User = $h['user']; Password = $pw }
}

function Remove-DemoSecret { Remove-Item -LiteralPath (Get-SecretPath) -Force -ErrorAction SilentlyContinue }

function Test-SchemaExists([string]$Schema) {
    $r = Invoke-MySql -Batch -Sql "SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name='$Schema';"
    return ([int]($r | Select-Object -First 1)) -gt 0
}

function Get-BaseTables([string]$Schema) {
    @(Invoke-MySql -Batch -Sql "SELECT table_name FROM information_schema.tables WHERE table_schema='$Schema' AND table_type='BASE TABLE' ORDER BY table_name;")
}

function Copy-ReferenceData([string]$Schema) {
    Assert-DisposableSchema $Schema      # the only writer of reference tables: never the live schema
    # live -> demo only. The live schema is only ever READ (INSERT ... SELECT FROM croms.x).
    $sb = New-Object Text.StringBuilder
    [void]$sb.AppendLine('SET FOREIGN_KEY_CHECKS=0; SET UNIQUE_CHECKS=0; SET sql_mode=REPLACE(@@sql_mode,"NO_AUTO_VALUE_ON_ZERO","");')
    foreach ($t in $script:ReferenceTables) {
        [void]$sb.AppendLine("TRUNCATE TABLE ``$Schema``.``$t``;")
        [void]$sb.AppendLine("INSERT INTO ``$Schema``.``$t`` SELECT * FROM ``$script:LiveSchema``.``$t``;")
    }
    # A copied window must not look signed-in: presence belongs to whoever runs the demo.
    [void]$sb.AppendLine("UPDATE ``$Schema``.windows SET current_operator=NULL, operator_name=NULL, last_heartbeat=NULL;")
    [void]$sb.AppendLine('SET UNIQUE_CHECKS=1; SET FOREIGN_KEY_CHECKS=1;')
    Invoke-MySql -Sql $sb.ToString() | Out-Null
    [void](Clear-JunkLookups $Schema)
}

. "$PSScriptRoot\_Junk.ps1"
