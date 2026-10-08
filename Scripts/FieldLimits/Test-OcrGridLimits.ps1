$App = 'CROMS'
. "$PSScriptRoot\_Init.ps1"
# Intelligent Document Processing writes OCR values straight into births / deaths / marriages.
# Checks (1) every field the three INSERTs write has a known column width in the grid
# (OcrDigitizationForm.LimitFor) and (2) ValuesFitColumns refuses a value one character over the
# limit and accepts one exactly at the limit. The refusal dialog is closed automatically.
Add-Type -TypeDefinition 'using System;using System.Runtime.InteropServices;public class WOcr{[DllImport("user32.dll",CharSet=CharSet.Unicode)]public static extern IntPtr FindWindow(string c,string t);[DllImport("user32.dll")]public static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);}'
$tm = New-Object System.Windows.Forms.Timer; $tm.Interval = 150
$tm.Add_Tick({ $h = [WOcr]::FindWindow($null, 'Value too long'); if ($h -ne [IntPtr]::Zero) { [WOcr]::PostMessage($h, 0x10, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null } })
$tm.Start()

$inst = [Reflection.BindingFlags]'NonPublic,Instance'
$ft   = $asm.GetType('CROMS.Forms.OcrDigitizationForm')
$form = [Activator]::CreateInstance($ft)
$kindT = $asm.GetType('CROMS.Data.DocKind'); $fc = $asm.GetType('CROMS.Data.FormCatalog')
$dfT = $asm.GetType('CROMS.Data.DocField'); $drT = $asm.GetType('CROMS.Data.DocAiResult')
$lim = $ft.GetMethod('LimitFor', $inst)
$results = New-Object System.Collections.ArrayList
function Add-Result($name, $pass, $detail) { [void]$results.Add([pscustomobject]@{ Check = $name; Pass = [bool]$pass; Detail = $detail }) }
function Use-Kind($name) {
    $kind = [Enum]::Parse($kindT, $name)
    $ft.GetField('_kind', $inst).SetValue($form, $kind)
    $ft.GetField('_formDef', $inst).SetValue($form, $fc.GetMethod('Current').Invoke($null, @($kind)))
}
function Fits($kindName, $vals) {
    Use-Kind $kindName
    $r = [Activator]::CreateInstance($drT)
    foreach ($k in $vals.Keys) { $r.Fields.Add([Activator]::CreateInstance($dfT, @($k, $k, [string]$vals[$k], $false))) }
    $ft.GetField('_result', $inst).SetValue($form, $r)
    return $ft.GetMethod('ValuesFitColumns', $inst).Invoke($form, @())
}

# every key the INSERTs write must have a limit
$keys = @{
 'Birth'    = 'RegistryNo','BookVolume','BookPage','ChildFirst','ChildMiddle','ChildLast','TimeOfBirth','PlaceOfBirth','TypeOfBirth','MotherFirst','MotherMiddle','MotherLast','MotherOccupation','Religion','Nationality','FatherFirst','FatherMiddle','FatherLast','FatherOccupation','Attendant','AttendantName','AttendantTitle','AttendantAddress','Informant','InformantRelationship','InformantAddress','PreparedByName','PreparedByTitle','ReceivedByName','ReceivedByTitle','RegisteredByName','RegisteredByTitle'
 'Death'    = 'RegistryNo','BookVolume','BookPage','FullName','DeceasedFirst','DeceasedMiddle','DeceasedLast','CivilStatus','Citizenship','PlaceOfDeath','Religion','CauseOfDeath','CorpseDisposal','PlaceOfDisposal','Informant','InformantRelationship','InformantAddress','PreparedByName','PreparedByTitle','ReceivedByName','ReceivedByTitle','RegisteredByName','RegisteredByTitle'
 'Marriage' = 'RegistryNo','BookVolume','BookPage','HusbandFirst','HusbandMiddle','HusbandLast','WifeFirst','WifeMiddle','WifeLast','PlaceOfMarriage','Solemnizer'
}
foreach ($k in $keys.Keys) {
    Use-Kind $k
    $zero = @($keys[$k] | Where-Object { [int]$lim.Invoke($form, @($_)) -le 0 })
    Add-Result "$k keys all have a column width" ($zero.Count -eq 0) $(if ($zero.Count) { 'no limit for: ' + ($zero -join ', ') } else { "$($keys[$k].Count) keys" })
}

# the guard itself
Add-Result 'Birth: 50 chars in a 50-wide column fits'      (Fits 'Birth' @{ ChildFirst = ('A' * 50) })      ''
Add-Result 'Birth: 51 chars in a 50-wide column refused'   (-not (Fits 'Birth' @{ ChildFirst = ('A' * 51) })) ''
Add-Result 'Birth: religion 51 refused (narrower of mother/father)' (-not (Fits 'Birth' @{ Religion = ('R' * 51) })) ''
Add-Result 'Death: middle name 41 refused (joined into full_name)'  (-not (Fits 'Death' @{ DeceasedMiddle = ('M' * 41) })) ''
Add-Result 'Death: middle name 40 fits'                    (Fits 'Death' @{ DeceasedMiddle = ('M' * 40) }) ''
Add-Result 'Marriage: place of marriage 201 refused'       (-not (Fits 'Marriage' @{ PlaceOfMarriage = ('p' * 201) })) ''
Add-Result 'Marriage: book page 21 refused'                (-not (Fits 'Marriage' @{ BookPage = ('9' * 21) })) ''
$tm.Stop()

$results | Format-Table -AutoSize | Out-String -Width 200
"FAILED: " + @($results | Where-Object { -not $_.Pass }).Count + " of " + $results.Count
