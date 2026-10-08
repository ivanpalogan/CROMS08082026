$App = 'CROMS'
. "$PSScriptRoot\_Init.ps1"   # loads the built exe, points config at it, defines $asm / Sql / Widths
$results = New-Object System.Collections.ArrayList
function Check($label,$ctl,$expect){
  $v = $ctl.MaxLength
  $ok = ($v -eq $expect)
  [void]$results.Add([pscustomobject]@{Control=$label; MaxLength=$v; Expected=$expect; Pass=$ok})
}

$df = [Activator]::CreateInstance($asm.GetType('CROMS.Forms.DeathRegistrationForm'))
$dt = $df.GetType(); $fl=[Reflection.BindingFlags]'Instance,NonPublic,Public'
$pod = $dt.GetField('_pod',$fl).GetValue($df)
Check 'Death place: facility' $pod[0] 100
Check 'Death place of disposal' ($dt.GetField('txtDispPlace',$fl).GetValue($df)) 200
Check 'Death informant address' ($dt.GetField('txtCInfAddr',$fl).GetValue($df)) 200
Check 'Death place: province' $pod[1] 60
Check 'Death place: municipality' $pod[2] 80
# type 200 chars into each via WM_CHAR
Add-Type -Namespace W -Name N -MemberDefinition '[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);'
$h = New-Object System.Windows.Forms.Form
foreach($c in $pod){ $c.Parent = $null; $h.Controls.Add($c) }
$null=$h.Handle
foreach($i in 0..2){ $null=$pod[$i].Handle; $pod[$i].DropDownStyle='DropDown'; for($k=0;$k -lt 200;$k++){ [void][W.N]::SendMessage($pod[$i].Handle,0x0102,[IntPtr][int][char]'a',[IntPtr]1) } }
"typed 200 chars: facility=" + $pod[0].Text.Length + " province=" + $pod[1].Text.Length + " municipality=" + $pod[2].Text.Length
$bfm=[Activator]::CreateInstance($asm.GetType('CROMS.Forms.BirthRegistrationForm')); Check 'Birth facility (_pob[0])' ($bfm.GetType().GetField('_pob',$fl).GetValue($bfm))[0] 100
$results | Format-Table -AutoSize | Out-String -Width 160
"FAILED: " + @($results | Where-Object { -not $_.Pass }).Count + " of " + $results.Count
