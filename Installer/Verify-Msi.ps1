param([Parameter(Mandatory=$true)][string]$Path)
$ErrorActionPreference='Stop'
$msi=(Resolve-Path $Path).Path
$installer=New-Object -ComObject WindowsInstaller.Installer
function Invoke-Com($Object,[string]$Name,[object[]]$Arguments){$Object.GetType().InvokeMember($Name,[Reflection.BindingFlags]::InvokeMethod,$null,$Object,$Arguments)}
function Get-Com($Object,[string]$Name,[object[]]$Arguments){$Object.GetType().InvokeMember($Name,[Reflection.BindingFlags]::GetProperty,$null,$Object,$Arguments)}
$db=Invoke-Com $installer 'OpenDatabase' @($msi,0)
function Rows([string]$Sql,[int]$Columns){
 $view=Invoke-Com $db 'OpenView' @($Sql)
 Invoke-Com $view 'Execute' @() | Out-Null
 $result=[Collections.Generic.List[object]]::new()
 while($record=Invoke-Com $view 'Fetch' @()){
  $values=@();for($i=1;$i -le $Columns;$i++){$values+=Get-Com $record 'StringData' @($i)}
  $result.Add($values)
 }
 Invoke-Com $view 'Close' @() | Out-Null
 return ,$result
}
function Assert($Condition,[string]$Message){if(-not $Condition){throw $Message}}
$properties=Rows 'SELECT `Property`,`Value` FROM `Property`' 2
$propertyMap=@{};foreach($row in $properties){$propertyMap[$row[0]]=$row[1]}
Assert (-not $propertyMap.ContainsKey('ALLUSERS') -or $propertyMap['ALLUSERS'] -eq '') 'MSI must be per-user'
Assert ($propertyMap['Manufacturer'] -eq 'Francisco Javier Vertedor Postigo') 'Manufacturer mismatch'
$summary=Get-Com $db 'SummaryInformation' @(0)
$flags=[int](Get-Com $summary 'Property' @(15))
Assert (($flags -band 8) -eq 8) 'Package requests elevated privileges'
$directories=Rows 'SELECT `Directory`,`Directory_Parent`,`DefaultDir` FROM `Directory`' 3
$directoryMap=@{};foreach($row in $directories){$directoryMap[$row[0]]=$row}
Assert ($directoryMap['INSTALLFOLDER'][1] -eq 'UserPrograms' -and $directoryMap['UserPrograms'][1] -eq 'LocalAppDataFolder') 'Wrong installation root'
$shortcuts=Rows 'SELECT `Shortcut`,`Directory_`,`Target` FROM `Shortcut`' 3
Assert ($shortcuts.Count -eq 2) 'Expected desktop and Start menu shortcuts'
Assert (@($shortcuts | Where-Object {$_[1] -eq 'DesktopFolder'}).Count -eq 1) 'Desktop shortcut missing'
Assert (@($shortcuts | Where-Object {$_[1] -eq 'AppStartMenu'}).Count -eq 1) 'Start menu shortcut missing'
$registry=Rows 'SELECT `Root`,`Key`,`Name` FROM `Registry`' 3
foreach($row in $registry){Assert ($row[0] -eq '1') 'Installer writes outside HKCU'}
$files=Rows 'SELECT `File`,`FileName` FROM `File`' 2
Assert ($files.Count -eq 7) 'Unexpected payload; check for private data'
Assert ($propertyMap['WIXUI_EXITDIALOGOPTIONALCHECKBOXTEXT'] -like '*sign in*') 'Startup option missing'
$actions=Rows 'SELECT `Action`,`Source`,`Target` FROM `CustomAction`' 3
Assert (@($actions | Where-Object {$_[0] -eq 'EnableStartup' -and $_[2] -eq '--enable-startup'}).Count -eq 1) 'Startup action missing'
Assert (@($actions | Where-Object {$_[0] -eq 'DisableStartup' -and $_[2] -eq '--disable-startup'}).Count -eq 1) 'Startup uninstall cleanup missing'
Write-Output 'PASS: per-user MSI; no elevation flag; LocalAppData installation; two user shortcuts; HKCU-only writes; seven approved payload files; opt-in startup and uninstall cleanup.'
