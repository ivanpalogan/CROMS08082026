# Runs every field-limit check against the BUILT exes and the live database, and prints one
# summary per script. Each Test-* script opens real forms (STA) and fills every text box to its
# limit, so run this after any schema change, any new input box, or any new migration:
#
#   powershell.exe -STA -NoProfile -ExecutionPolicy Bypass -File Scripts\FieldLimits\Run-All.ps1
#
# Build first (these test what is on disk). Close CROMS.exe / the kiosk before rebuilding - they
# lock bin\Debug. The scripts only READ the database; the one write they attempt
# (Test-DataTooLongGuard) is an over-long INSERT that MySQL refuses, so nothing is stored.
#
# Test-*   pass/fail: exit code 1 if any reports an overflow, a failed check or a script error.
# Census-* informational: lists boxes with no limit. Many are legitimate (search boxes, boxes
#          that are replaced by a pick list, numeric pickers) - read the list, it is a worklist.

$here = $PSScriptRoot
$bad = 0
foreach ($s in Get-ChildItem "$here\*.ps1" | Where-Object { $_.Name -notlike '_*' -and $_.Name -ne 'Run-All.ps1' } | Sort-Object Name) {
    $isCensus = $s.Name -like 'Census-*'
    Write-Host "=== $($s.Name)"
    $out = & powershell.exe -STA -NoProfile -ExecutionPolicy Bypass -File $s.FullName 2>&1 | Out-String
    $lines = $out -split "`r?`n"
    if ($isCensus) {
        $n = ($lines | Where-Object { $_ -match '^\S+\s+yes\s+([1-9]\d*)\s' }).Count
        Write-Host "    $n form(s) have boxes with no limit (informational - run the script alone to see which)"
        continue
    }
    $lines | Where-Object { $_ -match '^FAILED:|OVERFLOW' } | Select-Object -First 6 | ForEach-Object { Write-Host "    $_" }
    $failed = $lines | Where-Object { $_ -match '^FAILED: (\d+)' -and [int]$Matches[1] -gt 0 }
    $over   = $lines | Where-Object { $_ -match 'OVERFLOW:? (\d+)' -and [int]$Matches[1] -gt 0 }
    $broken = $out -match 'At .*\.ps1:\d+ char:' -or $out -match 'FullyQualifiedErrorId'
    $noverdict = -not ($lines | Where-Object { $_ -match '^FAILED:|OVERFLOW' })
    if ($failed -or $over -or $broken -or $noverdict) { $bad++; Write-Host "    -> PROBLEM in $($s.Name)" -ForegroundColor Red }
}
if ($bad -eq 0) { Write-Host "All field-limit checks clean." -ForegroundColor Green; exit 0 }
Write-Host "$bad script(s) reported a problem." -ForegroundColor Red; exit 1
