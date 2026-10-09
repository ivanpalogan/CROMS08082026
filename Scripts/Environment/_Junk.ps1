# Dot-sourced by _Common.ps1. ASCII only (non-ASCII is written as \u escapes inside regexes).

function Test-JunkLookupName([string]$n) {
    # Entries typed while the live master files were being tested: SQL fragments, markup, keyboard mashing,
    # an emoji, an 'aaaa...' church, doubled spaces. True = leave it out of the DEMO copy.
    if ([string]::IsNullOrWhiteSpace($n)) { return $true }
    if ($n.Trim().Length -lt 3 -or $n.Length -gt 60) { return $true }
    if ($n -match '[;<>%"_\[\]\\]') { return $true }
    if ($n -match '--') { return $true }
    if ($n -match '\s{2,}') { return $true }
    if ($n -match '^(.)\1+$') { return $true }                 # aaaa
    if ($n -match '[Àîõ]') { return $true }     # accented keyboard-test strings
    if ($n -match '[^\u0000-ÿ]') { return $true }         # emoji, other scripts
    if (@('dss','gusman','PATCHER','1e5','Pinagsasaksak','DIOPULMONARY ARREST') -contains $n) { return $true }
    return $false
}

function Clear-JunkLookups([string]$Schema) {
    Assert-DisposableSchema $Schema      # demo copy only: the live master files are never edited
    $tables = 'churches','hospitals','occupations','causes_of_death','relationships','religions','nationalities','residences'
    $removed = 0
    foreach ($t in $tables) {
        $rows = @(Invoke-MySql -Batch -Sql "SELECT id, name FROM ``$Schema``.``$t``;")
        $bad = New-Object System.Collections.Generic.List[int]
        foreach ($r in $rows) {
            if (([string]$r) -notmatch '^\d+(	|$)') { continue }      # continuation line of a multi-line name
            $p = ([string]$r) -split "`t", 2
            $name = if ($p.Count -ge 2) { $p[1] } else { '' }
            if (Test-JunkLookupName $name) { $bad.Add([int]$p[0]) }
        }
        if ($bad.Count -gt 0) {
            Invoke-MySql -Sql ("DELETE FROM ``$Schema``.``$t`` WHERE id IN (" + ($bad -join ',') + ");") | Out-Null
            $removed += $bad.Count
        }
    }
    return $removed
}
