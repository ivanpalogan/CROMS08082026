$App = 'CROMS'
. "$PSScriptRoot\_Init.ps1"   # loads the built exe, points config at it, defines $asm / Sql / Widths
$results = New-Object System.Collections.ArrayList
function Check($label,$ctl,$expect){
  $v = $ctl.MaxLength
  $ok = ($v -eq $expect)
  [void]$results.Add([pscustomobject]@{Control=$label; MaxLength=$v; Expected=$expect; Pass=$ok})
}

$df = [Activator]::CreateInstance($asm.GetType('CROMS.Forms.DeathRegistrationForm'))
$dt = $df.GetType(); $fl=[Reflection.BindingFlags]'Instance,NonPublic,Public,Static'
# column widths of deaths
$w=@{}; Sql "SELECT column_name, character_maximum_length FROM information_schema.columns WHERE table_schema='croms' AND table_name='deaths' AND character_maximum_length IS NOT NULL" 2>$null | ForEach-Object { $p=$_ -split "`t"; $w[$p[0]]=[int][Math]::Min([long]$p[1],100000) }
# every text control, recursively
function Walk($c){ foreach($k in $c.Controls){ $k; Walk $k } }
$all = @(Walk $df)
$filled=0; $skipped=0
foreach($c in $all){
  if($c -is [System.Windows.Forms.TextBox] -and -not $c.ReadOnly -and -not $c.UseSystemPasswordChar){
    $n = if($c.MaxLength -gt 0 -and $c.MaxLength -lt 32767){$c.MaxLength}else{400}
    $c.Text = ('a' * $n); $filled++
  } elseif($c -is [System.Windows.Forms.ComboBox]){
    if($c.DropDownStyle -eq 'DropDown'){ $n = if($c.MaxLength -gt 0 -and $c.MaxLength -lt 32767){$c.MaxLength}else{400}; $c.Text=('a'*$n); $filled++ } else { $skipped++ }
  }
}
"filled $filled text boxes/editable combos, skipped $skipped pick-only lists"
$ps = @(); $ps += $dt.GetMethod('FieldParams',$fl).Invoke($df,@())
$ex = $dt.GetField('_extras',$fl).GetValue($df); $exT=$ex.GetType()
$ps += $exT.GetMethod('Params',$fl).Invoke($ex,@())
$cols = ($dt.GetField('Columns',$fl).GetValue($null)) + ', ' + $exT.GetProperty('Columns',$fl).GetValue($null)
$phs  = ($dt.GetField('ValuePlaceholders',$fl).GetValue($null)) + ', ' + $exT.GetProperty('Placeholders',$fl).GetValue($null)
$cl = $cols -split '\s*,\s*' | ? {$_}; $pl = $phs -split '\s*,\s*' | ? {$_}
"columns=$($cl.Count) placeholders=$($pl.Count) params=$($ps.Count)"
$map=@{}; for($i=0;$i -lt [Math]::Min($cl.Count,$pl.Count);$i++){ $map[$pl[$i]]=$cl[$i] }
# extras: params named @x_<col>
foreach($c in $cl){ $map["@x_$c"]=$c }
$rows=@()
foreach($p in $ps){
  $col=$map[$p.ParameterName]; if(-not $col){continue}
  if(-not $w.ContainsKey($col)){continue}
  $len = if($p.Value -is [string]){$p.Value.Length}else{0}
  if($len -gt 0){ $rows += [pscustomobject]@{Column=$col; Width=$w[$col]; MaxValue=$len; Overflow=($len -gt $w[$col])} }
}
$rows | Sort-Object Overflow -Descending | Format-Table -AutoSize | Out-String -Width 160
"OVERFLOW: " + @($rows | ? Overflow).Count + " of " + $rows.Count
