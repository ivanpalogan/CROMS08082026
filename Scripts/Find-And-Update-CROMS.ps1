# ================================================================
#  Find-And-Update-CROMS.ps1
#  Auto-finds the CROMS server on the CURRENT Wi-Fi (no IP to type),
#  copies the latest build from its \\<ip>\CROMSRelease share, and
#  launches CROMS. Remembers the IP for next time. Re-scans whenever
#  the saved IP no longer answers (e.g. a new Wi-Fi / new server IP).
#  Run it via the .bat (double-click).
# ================================================================
$ErrorActionPreference = 'SilentlyContinue'

$dest   = Join-Path $env:USERPROFILE 'CROMS'
$cfg    = Join-Path $dest 'server.txt'

function ShareOk($ip)  { $ip -and (Test-Path ("\\$ip\CROMSRelease\Main\CROMS.exe")) }
function PortOpen($ip) {
    try {
        $c = New-Object Net.Sockets.TcpClient
        $a = $c.BeginConnect($ip, 445, $null, $null)
        $ok = $a.AsyncWaitHandle.WaitOne(150)   # 150ms per host
        if ($ok) { $c.EndConnect($a) }
        $c.Close(); return $ok
    } catch { return $false }
}

# Build an ordered candidate list, most-reliable first. The IP we were LAUNCHED
# from is the server by definition, so it works across subnets / new Wi-Fi / new
# hotspot IP without any luck. CROMS_SRC is set by the .bat to its own path
# (%~dp0), e.g. \\192.168.100.111\CROMSRelease\ -> 192.168.100.111.
$candidates = New-Object System.Collections.Generic.List[string]
$launchIp = $null
if ($env:CROMS_SRC -match '\\\\(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\\') { $launchIp = $Matches[1] }
if ($launchIp) { $candidates.Add($launchIp) }
if (Test-Path $cfg) { $s = (Get-Content $cfg -First 1).Trim(); if ($s) { $candidates.Add($s) } }
$candidates.Add('192.168.1.81')               # last-resort hardcoded guess

$server = $null
foreach ($c in $candidates) { if (ShareOk $c) { $server = $c; break } }

# Only if no known IP answers, sweep every /24 this PC is on.
if (-not $server) {
    Write-Host 'Looking for the CROMS server on this Wi-Fi...'
    $found = $null
    foreach ($e in (Get-NetIPAddress -AddressFamily IPv4 |
            Where-Object { $_.IPAddress -notlike '169.254*' -and $_.IPAddress -ne '127.0.0.1' })) {
        $base = $e.IPAddress -replace '\.\d+$', ''
        foreach ($i in 1..254) {
            $ip = "$base.$i"
            if (PortOpen $ip) { if (ShareOk $ip) { $found = $ip; break } }
        }
        if ($found) { break }
    }
    if ($found) { $server = $found }
    else {
        Write-Host ''
        Write-Host 'Could not find the CROMS server.' -ForegroundColor Red
        Write-Host 'Make sure THIS laptop is on the SAME Wi-Fi as the server,'
        Write-Host 'and the server has Published with password-protected sharing OFF.'
        Read-Host 'Press Enter to close'
        exit 1
    }
}

Write-Host "Server found: $server" -ForegroundColor Green
New-Item -ItemType Directory -Force -Path $dest | Out-Null
Set-Content -Path $cfg -Value $server          # remember for next time

# Copy all three apps into sibling folders (Main / Kiosk / Display). The in-app
# Update button uses the same layout, so both stay in sync. Each app's own
# *.config is kept on later updates (its connection settings stay on this PC).
$copied = 0
foreach ($app in @('Main', 'Kiosk', 'Display')) {
    $src = "\\$server\CROMSRelease\$app"
    if (-not (Test-Path $src)) { continue }     # that app wasn't published
    $dst = Join-Path $dest $app
    New-Item -ItemType Directory -Force -Path $dst | Out-Null
    $hasCfg = (Get-ChildItem $dst -Filter *.exe.config -ErrorAction SilentlyContinue | Measure-Object).Count -gt 0
    if ($hasCfg) {
        robocopy $src $dst /E /XF *.exe.config /R:2 /W:2 /NFL /NDL /NJH /NJS | Out-Null
    } else {
        robocopy $src $dst /E /R:2 /W:2 /NFL /NDL /NJH /NJS | Out-Null
    }
    if ($LASTEXITCODE -lt 8) { $copied++; Write-Host "  updated $app" -ForegroundColor Green }
}

if ($copied -eq 0) {
    Write-Host 'Copy failed (share unreachable, or nothing published).' -ForegroundColor Red
    Read-Host 'Press Enter to close'; exit 1
}

$main = Join-Path $dest 'Main\CROMS.exe'
if (Test-Path $main) {
    Write-Host 'Done. Launching CROMS...' -ForegroundColor Green
    Start-Process $main
} else {
    Write-Host 'Updated. (Main app not found to launch.)' -ForegroundColor Yellow
    Read-Host 'Press Enter to close'
}
