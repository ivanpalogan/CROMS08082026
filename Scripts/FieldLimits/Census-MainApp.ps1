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

$forms = $asm.GetTypes() | Where-Object { $_.IsSubclassOf([System.Windows.Forms.Form]) -and -not $_.IsAbstract -and (($_.Namespace -eq 'CROMS.Forms') -or ($_.Namespace -eq 'CROMS.Modules')) } | Sort-Object Name
$out=@()
foreach($t in $forms){
  $f=$null; $how=''
  foreach($ci in ($t.GetConstructors([Reflection.BindingFlags]'Instance,Public,NonPublic') | Sort-Object { $_.GetParameters().Count })){
    $args=@(); foreach($pi in $ci.GetParameters()){ $pt=$pi.ParameterType
      if($pt -eq [string]){$args+=''} elseif($pt -eq [int]){$args+=0} elseif($pt -eq [bool]){$args+=$false} elseif($pt.IsValueType -and -not [Nullable]::GetUnderlyingType($pt)){ $args+=[Activator]::CreateInstance($pt) } else {$args+=$null} }
    try{ $f=$ci.Invoke([object[]]$args); $how=$args.Count; break }catch{ $f=$null }
  }  if(-not $f){ $out += [pscustomobject]@{Form=$t.Name;Built='no';Unlimited=''}; continue }
  $un=@(); $fn=@{}; foreach($fi in $t.GetFields([Reflection.BindingFlags]"Instance,NonPublic,Public")){ try{ $v=$fi.GetValue($f); if($v -is [System.Windows.Forms.Control]){ $fn[$v]=$fi.Name } }catch{} } 
  foreach($c in @(Walk $f)){
    $isT = ($c -is [System.Windows.Forms.TextBox] -and -not $c.ReadOnly -and -not $c.UseSystemPasswordChar -and $c.Visible -ne $null)
    $isC = ($c -is [System.Windows.Forms.ComboBox] -and $c.DropDownStyle -eq 'DropDown')
    if(($isT -or $isC) -and ($c.MaxLength -le 0 -or $c.MaxLength -ge 32767)){ $un += $(if($c.Name){$c.Name}elseif($fn.ContainsKey($c)){$fn[$c]}else{"(unnamed:"+$c.Parent.GetType().Name+")"}) }
  }
  $out += [pscustomobject]@{Form=$t.Name;Built='yes';Unlimited=$un.Count; Names=(($un|Select-Object -First 12) -join ',')}
  try{ $f.Dispose() }catch{}
}
$out | Format-Table -AutoSize | Out-String -Width 220
