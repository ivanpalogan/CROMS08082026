# ============================================================
#  Setup-UpdateShare.ps1
#  Run this ONCE on the SERVER PC (A), as Administrator.
#  Right-click Start -> Terminal (Admin), then run:
#     powershell -ExecutionPolicy Bypass -File "<path>\Setup-UpdateShare.ps1"
#
#  Creates C:\CROMSRelease with Main / Kiosk / Display sub-folders that
#  point (via junctions) at the three build outputs, and shares it as
#  "CROMSRelease" (Everyone = Read). Clients' one-click "Update" button
#  pulls the latest build from this share over the LAN.
#
#  Junctions mean you DON'T re-run this after each rebuild — rebuilding in
#  Visual Studio updates the shared files automatically.
# ============================================================
$ErrorActionPreference = 'Stop'

# Repo root = the parent of this Scripts folder.
$root = Split-Path -Parent $PSScriptRoot
Write-Host "Repo root: $root"

$rel = "C:\CROMSRelease"
$map = @{
    Main    = Join-Path $root "CROMS\bin\Debug"
    Kiosk   = Join-Path $root "CROMS.Kiosk\bin\Debug"
    Display = Join-Path $root "CROMS.Display\bin\Debug"
}

# Verify the build outputs exist.
foreach ($k in $map.Keys) {
    if (-not (Test-Path (Join-Path $map[$k] ("CROMS" + $(if($k -eq 'Main'){''}else{".$k"}) + ".exe")))) {
        Write-Warning "Build not found for $k at $($map[$k]) — build the solution in Visual Studio first."
    }
}

New-Item -ItemType Directory -Force -Path $rel | Out-Null

foreach ($k in $map.Keys) {
    $link = Join-Path $rel $k
    if (Test-Path $link) { cmd /c rmdir "$link" 2>$null }
    cmd /c mklink /J "$link" "$($map[$k])" | Out-Null
    Write-Host "Linked $link  ->  $($map[$k])"
}

# (Re)create the share, Everyone = Read.
cmd /c net share CROMSRelease 2>$null | Out-Null
if ($LASTEXITCODE -eq 0) { cmd /c net share CROMSRelease /DELETE /Y 2>$null | Out-Null }
cmd /c net share CROMSRelease=$rel /GRANT:Everyone,READ

Write-Host ""
Write-Host "Done. Share ready at  \\<this-PC-IP>\CROMSRelease" -ForegroundColor Green
Write-Host "Clients click the Update button to pull the latest build."
Write-Host ""
Write-Host "If clients get 'access denied', turn OFF password-protected sharing:" -ForegroundColor Yellow
Write-Host "  Control Panel -> Network and Sharing Center -> Advanced sharing settings"
Write-Host "  -> All Networks -> Turn off password protected sharing -> Save."
