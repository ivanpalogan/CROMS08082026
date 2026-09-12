<#
  Build-Bundle.ps1 — makes ONE deployment folder that contains all three CROMS apps
  (Admin + Display + Kiosk) side by side, so the launcher's "same folder" lookup works
  on a client PC. Without this, a machine that only got CROMS.exe shows
  "Could not find CROMS.Display.exe".

  Usage:  powershell -ExecutionPolicy Bypass -File Scripts\Build-Bundle.ps1 [-Config Debug] [-NoZip]
#>
param(
    [string]$Config = "Debug",
    [switch]$NoZip
)

$ErrorActionPreference = "Stop"
$root   = Split-Path -Parent $PSScriptRoot
$out    = Join-Path $root "CROMS_Bundle"
$zip    = Join-Path $root "CROMS_AllApps_Bundle.zip"
$apps   = @("CROMS", "CROMS.Display", "CROMS.Kiosk")

# --- locate build output for each project (chosen config first, then the other) ---
$sources = @{}
foreach ($app in $apps) {
    $found = $null
    foreach ($cfg in @($Config, "Debug", "Release")) {
        $p = Join-Path $root "$app\bin\$cfg"
        if (Test-Path (Join-Path $p "$app.exe")) { $found = $p; break }
    }
    if (-not $found) { throw "$app.exe not built. Build $app in Visual Studio first." }
    $sources[$app] = $found
    Write-Host "  found $app  <-  $found"
}

# --- merge: Admin first (it has the most dependencies), then the other two on top ---
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Path $out | Out-Null

foreach ($app in $apps) {
    Copy-Item (Join-Path $sources[$app] "*") $out -Recurse -Force
}

# --- config files the LAN deployment needs (server IP etc.) live next to each exe ---
foreach ($app in $apps) {
    $cfg = Join-Path $sources[$app] "$app.exe.config"
    if (Test-Path $cfg) { Copy-Item $cfg $out -Force }
}

Write-Host ""
Write-Host "Bundle: $out"
Get-ChildItem $out -Filter "CROMS*.exe" | ForEach-Object { Write-Host "  $($_.Name)" }

if (-not $NoZip) {
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($out, $zip)
    Write-Host ""
    Write-Host "Zip:    $zip"
}
