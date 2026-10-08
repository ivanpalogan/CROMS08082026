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

$results.Clear()
$ci=[Reflection.BindingFlags]'Instance,NonPublic,Public'
function Make($typeName,$ctorArgs){ $t=$asm.GetType($typeName); $a=New-Object object[] ($ctorArgs.Count); for($i=0;$i -lt $ctorArgs.Count;$i++){$a[$i]=$ctorArgs[$i]}; [Activator]::CreateInstance($t,$ci,$null,$a,$null) }
function Ctl($obj,$name){ $f=$obj.GetType().GetField($name,$ci); if($f){$f.GetValue($obj)}else{$null} }
function Chk($label,$obj,$name,$table,$col){
  $c=Ctl $obj $name; if(-not $c){ [void]$results.Add([pscustomobject]@{Control="$label.$name";MaxLength='(not found)';Column="$table.$col";Width=0;Pass=$false}); return }
  $w=(Widths $table)[$col]; [void]$results.Add([pscustomobject]@{Control="$label.$name";MaxLength=$c.MaxLength;Column="$table.$col";Width=$w;Pass=($c.MaxLength -eq $w)}) }
function ChkMax($label,$obj,$name,$expect){ $c=Ctl $obj $name; [void]$results.Add([pscustomobject]@{Control="$label.$name";MaxLength=$(if($c){$c.MaxLength}else{'?'});Column='(fixed)';Width=$expect;Pass=($c -and $c.MaxLength -eq $expect)}) }

$f=Make 'CROMS.Forms.PetitionsForm' @(); Chk 'Petitions' $f 'txtRemarks' 'petitions' 'remarks'; Chk 'Petitions' $f 'txtRequester' 'petitions' 'requester_name'; Chk 'Petitions' $f 'txtRelationship' 'petitions' 'requester_relationship'
$f=Make 'CROMS.Forms.CertificateRequestForm' @(); Chk 'CertRequest' $f 'txtPurpose' 'certificate_requests' 'purpose'; ChkMax 'CertRequest' $f 'txtFirst' 44; ChkMax 'CertRequest' $f 'txtMiddle' 30; ChkMax 'CertRequest' $f 'txtLast' 44
$f=Make 'CROMS.Forms.ReleaseClaimForm' @(); Chk 'Release' $f 'txtClaimant' 'releases' 'claimant_name'; Chk 'Release' $f 'txtIdType' 'releases' 'representative_id_type'; Chk 'Release' $f 'txtIdNum' 'releases' 'representative_id_number'
$f=Make 'CROMS.Forms.FeesPaymentsForm' @(); Chk 'Fees' $f '_txtOr' 'payments' 'or_number'; Chk 'Fees' $f '_txtRef' 'payments' 'reference_no'; Chk 'Fees' $f '_txtRemarks' 'payments' 'remarks'; Chk 'Fees' $f '_wPayer' 'payments' 'payer_name'; Chk 'Fees' $f '_wPurpose' 'payments' 'purpose'; Chk 'Fees' $f '_wOr' 'payments' 'or_number'
$f=Make 'CROMS.Forms.UsersAuditForm' @(); Chk 'Users' $f 'txtUsername' 'users' 'username'; Chk 'Users' $f 'txtFullName' 'users' 'full_name'; Chk 'Users' $f 'txtBioAddress' 'staff_biodata' 'address'; Chk 'Users' $f 'txtBioPosition' 'staff_biodata' 'position'; Chk 'Users' $f 'txtBioEmergencyName' 'staff_biodata' 'emergency_contact_name'
$f=Make 'CROMS.Forms.StaffBiodataForm' @(); Chk 'Biodata' $f 'txtAddress' 'staff_biodata' 'address'; Chk 'Biodata' $f 'txtEmployeeNo' 'staff_biodata' 'employee_no'
$f=Make 'CROMS.Forms.OfficeAssetsForm' @(); Chk 'OfficeAssets' $f '_txtOffice' 'office_profile' 'office_name'; Chk 'OfficeAssets' $f '_txtRegistrar' 'office_profile' 'registrar_name'; Chk 'OfficeAssets' $f '_txtProvince' 'office_profile' 'province'
$f=Make 'CROMS.Forms.OldBirthRecordsForm' @(); Chk 'OldBirth' $f 'txtReg' 'births' 'registry_no'; Chk 'OldBirth' $f 'txtFirst' 'births' 'first_name'; Chk 'OldBirth' $f 'txtPlace' 'births' 'place_of_birth'; Chk 'OldBirth' $f 'txtRemarks' 'births' 'remarks'
$f=Make 'CROMS.Forms.OldDeathRecordsForm' @(); Chk 'OldDeath' $f 'txtLast' 'deaths' 'last_name'; Chk 'OldDeath' $f 'txtPlace' 'deaths' 'place_of_death'; Chk 'OldDeath' $f 'txtInfAddr' 'deaths' 'informant_address'
$f=Make 'CROMS.Forms.OldMarriageRecordsForm' @(); Chk 'OldMarriage' $f 'txtHLast' 'marriages' 'husband_last_name'; Chk 'OldMarriage' $f 'txtPlaceOfMarriage' 'marriages' 'place_of_marriage'; Chk 'OldMarriage' $f 'txtSolemnizer' 'marriages' 'solemnizer'
$bid=(Sql "SELECT id FROM births LIMIT 1" 2>$null); $f=Make 'CROMS.Forms.DelayedBirthCaseForm' @([int]$bid); ChkMax 'DelayedBirth' $f '_txtEvaluation' 500
$f=Make 'CROMS.Forms.LoginForm' @(); Chk 'Login' $f 'txtUser' 'users' 'username'
# dialogs that need real data
$mid=(Sql "SELECT id FROM marriages LIMIT 1" 2>$null)
$f=Make 'CROMS.Forms.MarriageCaseForm' @([int]$mid); Chk 'MarriageCase' $f '_basisNotes' 'marriages' 'exemption_notes'; Chk 'MarriageCase' $f '_delayReason' 'marriages' 'delay_reason'; Chk 'MarriageCase' $f '_reviewNotes' 'marriages' 'registrar_review_notes'
$bt=$asm.GetType('CROMS.Data.BreqsRequest'); $bs=$asm.GetType('CROMS.Data.BreqsService').GetProperty('Settings',[Reflection.BindingFlags]'Static,Public,NonPublic'); 
try{ $req=[Activator]::CreateInstance($bt); $set=$bs.GetValue($null); $f=Make 'CROMS.Forms.BreqsRequestDialog' @($req,$set)
  Chk 'BreqsDialog' $f '_rFirst' 'breqs_requests' 'requester_first'; Chk 'BreqsDialog' $f '_rLast' 'breqs_requests' 'requester_last'; Chk 'BreqsDialog' $f '_father' 'breqs_requests' 'father_name'; Chk 'BreqsDialog' $f '_city' 'breqs_requests' 'event_city'; Chk 'BreqsDialog' $f '_purpose' 'breqs_requests' 'purpose'; Chk 'BreqsDialog' $f '_contact' 'breqs_requests' 'contact_no' }catch{ "BreqsDialog build failed: " + $_.Exception.InnerException.Message }
$wd=Make 'CROMS.Forms.WindowEditDialog' @('', '', 'Active', $false); Chk 'WindowDialog' $wd '_name' 'windows' 'window_name'; Chk 'WindowDialog' $wd '_desc' 'windows' 'description'
$pf=Make 'CROMS.Forms.PetitionFeeDialog' @('', 0, '', ''); Chk 'PetitionFee' $pf '_txtOr' 'payments' 'or_number'
# Master Files follows the category
$mf=Make 'CROMS.Forms.MasterFilesForm' @(); $cat=Ctl $mf 'cboCategory'; $ok=@(); foreach($i in 0..($cat.Items.Count-1)){ $cat.SelectedIndex=$i; $nb=Ctl $mf 'txtName'; $ok += "$($cat.Items[$i])=$($nb.MaxLength)" }
"MasterFiles name widths by category: " + ($ok -join ', ')
$results | Format-Table -AutoSize | Out-String -Width 200
"FAILED: " + @($results | Where-Object { -not $_.Pass }).Count + " of " + $results.Count
