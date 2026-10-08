$App = 'CROMS'
. "$PSScriptRoot\_Init.ps1"   # loads the built exe, points config at it, defines $asm / Sql / Widths
$results = New-Object System.Collections.ArrayList
function Check($label,$ctl,$expect){
  $v = $ctl.MaxLength
  $ok = ($v -eq $expect)
  [void]$results.Add([pscustomobject]@{Control=$label; MaxLength=$v; Expected=$expect; Pass=$ok})
}

# 1) AddrBox (parents / consent person) + typing past the limit through the real window message
$ab = [Activator]::CreateInstance($asm.GetType('CROMS.Forms.AddrBox'))
Check 'AddrBox.Prov'  $ab.Prov 60
Check 'AddrBox.Muni'  $ab.Muni 80
Check 'AddrBox.Brgy'  $ab.Brgy 80
Check 'AddrBox.House' $ab.House 100
$host1 = New-Object System.Windows.Forms.Form
$host1.Controls.Add($ab.House); $null = $host1.Handle; $null = $ab.House.Handle
Add-Type -Namespace W -Name N -MemberDefinition '[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);'
for($i=0;$i -lt 150;$i++){ [void][W.N]::SendMessage($ab.House.Handle,0x0102,[IntPtr][int][char]'a',[IntPtr]1) }   # WM_CHAR
[void]$results.Add([pscustomobject]@{Control='House typed 150 chars -> kept'; MaxLength=$ab.House.Text.Length; Expected=100; Pass=($ab.House.Text.Length -eq 100)})

# 2) Form 90 and Form 97: walk every Party-like holder
function ScanForm($formTypeName,$tag){
  $ft = $asm.GetType($formTypeName)
  try { $frm = New-Object -TypeName $formTypeName -ErrorAction Stop } catch { $frm = $null }
  if(-not $frm){ $frm = [Activator]::CreateInstance($ft,@($null)) }
  $fl = [Reflection.BindingFlags]'Instance,NonPublic,Public'
  $seen = 0
  foreach($f in $ft.GetFields($fl)){
    $val = $f.GetValue($frm); if(-not $val){continue}
    $items = @(); if($val -is [System.Collections.IEnumerable] -and $val -isnot [string]){ $items=@($val) } else { $items=@($val) }
    foreach($it in $items){
      if(-not $it){continue}
      foreach($pn in 'ResProv','ResMuni','ResBrgy','ResHouse','Prov','Muni','Brgy','House'){
        $fi = $it.GetType().GetField($pn,$fl)
        if($fi){ $c=$fi.GetValue($it); if($c -and ($c -is [System.Windows.Forms.TextBox] -or $c -is [System.Windows.Forms.ComboBox])){
            $exp = switch -Regex ($pn){ 'Prov$' {60} 'Muni$' {80} 'Brgy$' {80} 'House$' {100} }
            Check "$tag $($f.Name).$pn" $c $exp; $seen++ } }
      }
    }
  }
  "$tag : checked $seen address controls"
  $frm.Dispose()
}
ScanForm 'CROMS.Forms.MarriageLicenseForm' 'Form90'
ScanForm 'CROMS.Forms.MarriageEntryForm' 'Form97'

# 3) Birth Registration: residence cells, hospital address
$bf2 = $null
if(-not $bf2){ $bf2 = [Activator]::CreateInstance($asm.GetType('CROMS.Forms.BirthRegistrationForm')) }
$bt = $bf2.GetType(); $fl=[Reflection.BindingFlags]'Instance,NonPublic,Public'
foreach($nm in '_mres','_fres'){
  $arr = $bt.GetField($nm,$fl).GetValue($bf2)
  Check "Birth $nm[0] house/street" $arr[0] 100
  Check "Birth $nm[1] province"     $arr[1] 60
  Check "Birth $nm[2] municipality" $arr[2] 80
  Check "Birth $nm[3] barangay"     $arr[3] 80
}
Check 'Birth _hospHouse'    ($bt.GetField('_hospHouse',$fl).GetValue($bf2)) 100
Check 'Birth _hospBarangay' ($bt.GetField('_hospBarangay',$fl).GetValue($bf2)) 80

$results | Format-Table -AutoSize | Out-String -Width 200
"FAILED: " + @($results | Where-Object { -not $_.Pass }).Count + " of " + $results.Count
