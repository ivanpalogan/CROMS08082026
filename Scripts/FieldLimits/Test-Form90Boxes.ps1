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

$f90=[Activator]::CreateInstance($asm.GetType('CROMS.Forms.MarriageLicenseForm'),@($null))
FillAll $f90
$cur=$f90.GetType().GetMethod('Current',$fl).Invoke($f90,@())
$m=@{First='_first_name';Middle='_middle_name';Last='_last_name';PlaceOfBirth='_place_of_birth';Sex='_sex';BirthCountry='_birth_country';Citizenship='_citizenship';CivilStatus='_civil_status';Religion='_religion';Residence='_residence';ResProvince='_res_province';ResMunicipality='_res_municipality';ResBarangay='_res_barangay';ResHouse='_res_house';
 FatherFirst='_father_first_name';FatherMiddle='_father_middle_name';FatherLast='_father_last_name';FatherCitizenship='_father_citizenship';FatherResidence='_father_residence';
 MotherFirst='_mother_first_name';MotherMiddle='_mother_middle_name';MotherLast='_mother_last_name';MotherCitizenship='_mother_citizenship';MotherResidence='_mother_residence';
 ConsentFirst='_consent_first_name';ConsentMiddle='_consent_middle_name';ConsentLast='_consent_last_name';ConsentRelationship='_consent_relationship';ConsentCitizenship='_consent_citizenship';ConsentResidence='_consent_residence';
 PrevDissolution='_prev_dissolution';PrevDissolvedMunicipality='_prev_dissolved_municipality';PrevDissolvedProvince='_prev_dissolved_province'}
$pairs=@()
foreach($side in 'Husband','Wife'){
  $party=$cur.GetType().GetField($side,$fl).GetValue($cur); $pre=$side.ToLower()
  foreach($k in $m.Keys){ $fi=$party.GetType().GetField($k,$fl); if($fi){ $v=$fi.GetValue($party); $pairs += [pscustomobject]@{Col=$pre+$m[$k];Val=[string]$v} } }
  # nested residence cells of father/mother/consent
  foreach($who in 'Father','Mother','Consent'){
    $a=$party.GetType().GetField($who+'Addr',$fl); if($a){ $ad=$a.GetValue($party); if($ad){
      foreach($pp in @(@('Province','_res_province'),@('Municipality','_res_municipality'),@('Barangay','_res_barangay'),@('House','_res_house'))){
        $x=$ad.GetType().GetField($pp[0],$fl); if($x){ $pairs += [pscustomobject]@{Col=$pre+'_'+$who.ToLower()+$pp[1];Val=[string]$x.GetValue($ad)} } } } }
  }
}
$rem=$cur.GetType().GetField('Remarks',$fl); if($rem){ $pairs += [pscustomobject]@{Col='remarks';Val=[string]$rem.GetValue($cur)} }
Report 'Form 90 (marriage_licenses)' $pairs (Widths 'marriage_licenses')
'done'
