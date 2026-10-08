$App = 'CROMS.Kiosk'
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

$ci=[Reflection.BindingFlags]'Instance,NonPublic,Public'
$st=$asm.GetTypes() | Where-Object { $_.Name -eq 'KioskSession' } | Select-Object -First 1
$sess=[Activator]::CreateInstance($st)
$res=New-Object System.Collections.ArrayList
function Chk($form,$name,$expect){ $f=$form.GetType().GetField($name,$ci); $c=$f.GetValue($form); [void]$res.Add([pscustomobject]@{Control="$($form.GetType().Name).$name";MaxLength=$c.MaxLength;Expected=$expect;Pass=($c.MaxLength -eq $expect)}) }
$d=[Activator]::CreateInstance($asm.GetType('CROMS.Kiosk.DetailsPhotoForm'),$ci,$null,[object[]]@($sess),$null)
foreach($n in '_txtFirst','_txtLast','_txtFirst2','_txtLast2'){ Chk $d $n 40 }; foreach($n in '_txtMiddle','_txtMiddle2'){ Chk $d $n 30 }; Chk $d '_txtIdNo' 60; Chk $d '_txtClaimTicket' 30; Chk $d '_txtSubOrg' 150; Chk $d '_txtContact' 20
$c=[Activator]::CreateInstance($asm.GetType('CROMS.Kiosk.CtcDetailsForm'),$ci,$null,[object[]]@($sess),$null)
foreach($n in '_ownerFirst','_ownerMiddle','_ownerLast','_spouseFirst','_spouseMiddle','_spouseLast'){ Chk $c $n 60 }; foreach($n in '_ownerSuffix','_spouseSuffix'){ Chk $c $n 20 }; Chk $c '_registryNo' 40; Chk $c '_remarks' 255
$b=[Activator]::CreateInstance($asm.GetType('CROMS.Kiosk.BreqsDetailsForm'),$ci,$null,[object[]]@($sess),$null)
foreach($n in '_txtOwnerFirst','_txtOwnerMiddle','_txtOwnerLast','_txtSpouseFirst','_txtSpouseMiddle','_txtSpouseLast','_txtIdNo'){ Chk $b $n 60 }; Chk $b '_txtFather' 150; Chk $b '_txtMother' 150; Chk $b '_txtCity' 80; Chk $b '_txtProvince' 60
# worst-case joined name against queue_tickets.full_name (120)
$sess.GetType().GetField('First').SetValue($sess,('a'*40)); $sess.GetType().GetField('Middle').SetValue($sess,('a'*30)); $sess.GetType().GetField('Last').SetValue($sess,('a'*40))
$core=$asm.GetType('CROMS.Kiosk.KioskCore'); $fn=$core.GetMethod('FullName',[Reflection.BindingFlags]'Static,Public,NonPublic').Invoke($null,@($sess))
"worst-case joined full_name length = $($fn.Length) (column 120)"
$res | Format-Table -AutoSize | Out-String -Width 160
"FAILED: " + @($res | Where-Object { -not $_.Pass }).Count + " of " + $res.Count
