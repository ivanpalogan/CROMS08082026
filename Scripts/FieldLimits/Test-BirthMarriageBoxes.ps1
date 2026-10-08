$App = 'CROMS'
. "$PSScriptRoot\_Init.ps1"   # loads the built exe, points config at it, defines $asm / Sql / Widths
$results = New-Object System.Collections.ArrayList
function Check($label,$ctl,$expect){
  $v = $ctl.MaxLength
  $ok = ($v -eq $expect)
  [void]$results.Add([pscustomobject]@{Control=$label; MaxLength=$v; Expected=$expect; Pass=$ok})
}

$fl=[Reflection.BindingFlags]'Instance,NonPublic,Public,Static'
function Widths($table){ $w=@{}; Sql "SELECT column_name, character_maximum_length FROM information_schema.columns WHERE table_schema='croms' AND table_name='$table' AND character_maximum_length IS NOT NULL" 2>$null | ForEach-Object { $p=$_ -split "`t"; $w[$p[0]]=[int][Math]::Min([long]$p[1],100000) }; $w }
function Walk($c){ foreach($k in $c.Controls){ $k; Walk $k } }
function FillAll($form){
  $n1=0;$sk=0
  foreach($c in @(Walk $form)){
    $max = if($c -is [System.Windows.Forms.TextBox] -or $c -is [System.Windows.Forms.ComboBox]){ $c.MaxLength } else { 0 }
    $len = if($max -gt 0 -and $max -lt 32767){$max}else{400}
    if($c -is [System.Windows.Forms.TextBox] -and -not $c.ReadOnly -and -not $c.UseSystemPasswordChar){
      $c.Text = ('a'*$len); $n1++ }
    elseif($c -is [System.Windows.Forms.ComboBox]){ if($c.DropDownStyle -eq 'DropDown'){ $c.Text=('a'*$len); $n1++ } else { $sk++ } }
  }
  "filled $n1 editable boxes, skipped $sk pick-only lists"
}
function Report($label,$pairs,$w){
  $rows=@()
  foreach($p in $pairs){ if($w.ContainsKey($p.Col) -and $p.Val -is [string] -and $p.Val.Length -gt 0){ $rows += [pscustomobject]@{Column=$p.Col;Width=$w[$p.Col];MaxValue=$p.Val.Length;Overflow=($p.Val.Length -gt $w[$p.Col])} } }
  "== $label : checked $($rows.Count) text columns, OVERFLOW " + @($rows|Where-Object Overflow).Count
  $rows | Where-Object Overflow | Sort-Object Column | Format-Table -AutoSize | Out-String -Width 160
}

# ---------- BIRTH ----------
$bf=[Activator]::CreateInstance($asm.GetType('CROMS.Forms.BirthRegistrationForm')); $bt=$bf.GetType()
FillAll $bf
$ps=$bt.GetMethod('FieldParams',$fl).Invoke($bf,@('Draft'))
$cl=($bt.GetField('Columns',$fl).GetValue($null)) -split '\s*,\s*'; $pl=($bt.GetField('ValuePlaceholders',$fl).GetValue($null)) -split '\s*,\s*'
$map=@{}; for($i=0;$i -lt $cl.Count;$i++){ $map[$pl[$i]]=$cl[$i] }
$pairs=@(); foreach($p in $ps){ if($map.ContainsKey($p.ParameterName)){ $pairs += [pscustomobject]@{Col=$map[$p.ParameterName];Val=$p.Value} } }
Report 'Birth (births)' $pairs (Widths 'births')

# ---------- MARRIAGE FORM 97 ----------
$mf=[Activator]::CreateInstance($asm.GetType('CROMS.Forms.MarriageEntryForm'),@($null))
FillAll $mf
$vals=$mf.GetType().GetMethod('Values',$fl).Invoke($mf,@('Draft'))
$pairs=@(); foreach($k in $vals.Keys){ $pairs += [pscustomobject]@{Col=$k;Val=$vals[$k]} }
Report 'Marriage Form 97 (marriages)' $pairs (Widths 'marriages')
'done'
