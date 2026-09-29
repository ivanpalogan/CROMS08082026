<#
  Build-MobileRuntime.ps1 - assembles the self-contained Mobile Capture runtime so an office PC
  needs NO Node.js / npm / Ionic install to use Mobile Capture:

      <bundle>\MobileApp\
          node\node.exe          portable Node runtime (copied from this PC)
          server\                the save-API (index.js, db.js, signaling.js, public\)
          ssl\get-cert.js        Let's Encrypt (DNS-01) certificate tool
          node_modules\          production dependencies only (express, mysql2, ws, acme-client, ...)

  CROMS finds this folder beside CROMS.exe on its own (Data\MobileRuntime.cs) - no path in
  App.config to edit. What is deliberately NOT copied: server\.env (it holds the developer's database
  password; CROMS hands the service its database settings at start), *.log, and any private keys or
  certificates (each office PC issues its own).

  NOT included: the Angular phone-scanner / claimapp dev servers. Mobile Capture (the QR page hosted by
  the save-API on https) does not use them.

  Needs, on THIS (developer) PC only: node + npm on PATH, Internet (npm install), and the
  ORCMobile_Application folder.

  Usage:  powershell -ExecutionPolicy Bypass -File Scripts\Build-MobileRuntime.ps1 [-Out <folder>] [-Source <ORCMobile_Application>]
          (default -Out is CROMS_Bundle\MobileApp; Build-Bundle.ps1 -WithMobileRuntime calls this for you)
#>
param(
    [string]$Source = "",
    [string]$Out    = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not $Out) { $Out = Join-Path $root "CROMS_Bundle\MobileApp" }
if (-not $Source) {
    foreach ($c in @((Join-Path $env:USERPROFILE "ORCMobile_Application"), (Join-Path (Split-Path -Parent $root) "ORCMobile_Application"))) {
        if (Test-Path (Join-Path $c "server\index.js")) { $Source = $c; break }
    }
}
if (-not $Source -or -not (Test-Path (Join-Path $Source "server\index.js"))) {
    throw "ORCMobile_Application not found. Pass -Source <folder that contains server\index.js>."
}

$nodeCmd = Get-Command node -ErrorAction SilentlyContinue
if (-not $nodeCmd) { throw "node.exe not found on PATH - install Node.js on this PC once to build the runtime." }
$npmCmd = Get-Command npm -ErrorAction SilentlyContinue
if (-not $npmCmd) { throw "npm not found on PATH." }

Write-Host "Source : $Source"
Write-Host "Output : $Out"

if (Test-Path $Out) { Remove-Item $Out -Recurse -Force }
New-Item -ItemType Directory -Path $Out | Out-Null

# --- server (no secrets, no logs, no node_modules) ---
$srvOut = Join-Path $Out "server"
New-Item -ItemType Directory -Path $srvOut | Out-Null
Get-ChildItem (Join-Path $Source "server") -Force | Where-Object {
    $_.Name -notin @("node_modules", ".env", "package-lock.json") -and $_.Name -notlike "*.log" -and $_.Name -notlike ".env.*"
} | ForEach-Object { Copy-Item $_.FullName $srvOut -Recurse -Force }

# --- certificate tool (script only - never keys, certs or the ACME account key) ---
$sslOut = Join-Path $Out "ssl"
New-Item -ItemType Directory -Path $sslOut | Out-Null
Copy-Item (Join-Path $Source "ssl\get-cert.js") $sslOut -Force

# --- production dependencies: server's own + acme-client (for get-cert.js), installed at the
#     MobileApp root so both server\index.js and ssl\get-cert.js resolve them ---
$srvPkg  = Get-Content (Join-Path $Source "server\package.json") -Raw | ConvertFrom-Json
$rootPkg = Get-Content (Join-Path $Source "package.json") -Raw | ConvertFrom-Json
$deps = [ordered]@{}
foreach ($p in $srvPkg.dependencies.PSObject.Properties) { $deps[$p.Name] = $p.Value }
if ($rootPkg.dependencies.'acme-client') { $deps['acme-client'] = $rootPkg.dependencies.'acme-client' }
else { throw "acme-client is not in $Source\package.json dependencies." }

$pkg = [ordered]@{ name = "croms-mobile-runtime"; version = "1.0.0"; private = $true; dependencies = $deps }
($pkg | ConvertTo-Json -Depth 5) | Set-Content (Join-Path $Out "package.json") -Encoding ASCII

Push-Location $Out
try {
    Write-Host "npm install --omit=dev ..."
    & npm install --omit=dev --no-audit --no-fund --loglevel=error
    if ($LASTEXITCODE -ne 0) { throw "npm install failed (exit $LASTEXITCODE)." }
} finally { Pop-Location }

# package.json / lock are only needed for the install; without package.json CROMS also knows
# this folder is not the Angular app (it never tries to `ng serve` it).
Remove-Item (Join-Path $Out "package.json") -Force
Remove-Item (Join-Path $Out "package-lock.json") -Force -ErrorAction SilentlyContinue

# --- portable node.exe ---
$nodeOut = Join-Path $Out "node"
New-Item -ItemType Directory -Path $nodeOut | Out-Null
Copy-Item $nodeCmd.Source (Join-Path $nodeOut "node.exe") -Force

# --- prove it works: load every module the service needs with THE BUNDLED node.exe ---
Push-Location $Out
try {
    $check = "require('express');require('mysql2');require('ws');require('dotenv');require('cors');require('acme-client');console.log('modules-ok')"
    $res = & (Join-Path $nodeOut "node.exe") -e $check 2>&1
    if ("$res" -notmatch "modules-ok") { throw "Runtime self-check failed: $res" }
    & (Join-Path $nodeOut "node.exe") --check (Join-Path $srvOut "index.js")
    if ($LASTEXITCODE -ne 0) { throw "server\index.js failed a syntax check." }
} finally { Pop-Location }

$size = [math]::Round(((Get-ChildItem $Out -Recurse -File | Measure-Object Length -Sum).Sum) / 1MB, 1)
Write-Host ""
Write-Host "Mobile Capture runtime ready: $Out  ($size MB)"
Write-Host "Self-check passed (bundled node loaded every required module)."
