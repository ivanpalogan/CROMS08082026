# Shared setup for the field-limit checks. Dot-sourced by every script in this folder:
#   $App = 'CROMS'   (or 'CROMS.Kiosk')
#   . "$PSScriptRoot\_Init.ps1"
# Gives the caller: $bin, $asm (the built exe, loaded), Sql "<query>" (rows as tab-separated
# lines, like mysql -N), and the app pointed at its OWN config file (so it talks to the same
# database the app does). No database password lives in these scripts: the connection comes from
# the app's own configuration, exactly as the app itself would use it.
#
# The exe must already be built (Debug) - these scripts test what is on disk, they do not build.
#   CROMS_BIN   optional env var: use another build folder instead of <repo>\<App>\bin\Debug

$ErrorActionPreference = 'Continue'
if (-not $App) { throw "Set `$App ('CROMS' or 'CROMS.Kiosk') before dot-sourcing _Init.ps1" }
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$bin  = if ($env:CROMS_BIN) { $env:CROMS_BIN } else { Join-Path $repo "$App\bin\Debug" }
if (-not (Test-Path "$bin\$App.exe")) { throw "Build $App first - $bin\$App.exe was not found." }
Set-Location $bin
Add-Type -AssemblyName System.Windows.Forms, System.Drawing

# Point ConfigurationManager at the app's own config. Setting APP_CONFIG_FILE alone is not
# enough: ConfigurationManager caches its state on first use, so clear that cache too.
[AppDomain]::CurrentDomain.SetData('APP_CONFIG_FILE', "$bin\$App.exe.config")
Add-Type -AssemblyName System.Configuration
$cm = [System.Configuration.ConfigurationManager].Assembly
$t  = $cm.GetType('System.Configuration.ConfigurationManager')
$bf = [Reflection.BindingFlags]'NonPublic,Static'
foreach ($n in 's_initState', 's_configSystem') {
    $f = $t.GetField($n, $bf)
    if ($f) { if ($n -eq 's_initState') { $f.SetValue($null, 0) } else { $f.SetValue($null, $null) } }
}
$cp = $cm.GetType('System.Configuration.ClientConfigPaths')
$cf = $cp.GetField('s_current', $bf); if ($cf) { $cf.SetValue($null, $null) }

Add-Type -Path "$bin\MySql.Data.dll"
$asm = [Reflection.Assembly]::LoadFrom("$bin\$App.exe")

function ConnectionStringForTests {
    # Prefer the app's own ServerConfig (honours the server chosen on this PC), else App.config.
    foreach ($ty in $asm.GetTypes()) {
        if ($ty.Name -eq 'ServerConfig') {
            $p = $ty.GetProperty('EffectiveConnectionString', [Reflection.BindingFlags]'Static,Public,NonPublic')
            if ($p) { $v = $p.GetValue($null); if ($v) { return $v } }
        }
    }
    return [System.Configuration.ConfigurationManager]::ConnectionStrings['Croms'].ConnectionString
}

function Sql([string]$query) {
    $conn = New-Object MySql.Data.MySqlClient.MySqlConnection (ConnectionStringForTests)
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand(); $cmd.CommandText = $query
        $r = $cmd.ExecuteReader()
        while ($r.Read()) {
            $v = @(); for ($i = 0; $i -lt $r.FieldCount; $i++) { $v += [string]$r.GetValue($i) }
            $v -join "`t"
        }
        $r.Close()
    } finally { $conn.Close() }
}
