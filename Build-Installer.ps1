param([Parameter(Mandatory=$true)][string]$WixDirectory, [string]$BuildDirectory, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$wix = (Resolve-Path $WixDirectory).Path
if(-not $BuildDirectory){$BuildDirectory=Join-Path $PSScriptRoot 'build\installer'}
if(-not $OutputDirectory){$OutputDirectory=Join-Path $PSScriptRoot 'dist'}
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$build = [IO.Path]::GetFullPath($BuildDirectory)
$payload = Join-Path $build 'payload'
New-Item -ItemType Directory -Force $payload | Out-Null
& (Join-Path $PSScriptRoot 'Build.ps1') -ExecutableName ClassAlerts-Updated.exe
Copy-Item (Join-Path $PSScriptRoot 'App\ClassAlerts-Updated.exe') (Join-Path $payload 'ClassAlerts.exe') -Force
Copy-Item (Join-Path $PSScriptRoot 'App\ClassAlerts-Updated.exe.config') (Join-Path $payload 'ClassAlerts.exe.config') -Force
Copy-Item (Join-Path $PSScriptRoot 'Source\config.json'),(Join-Path $PSScriptRoot 'LICENSE.txt'),(Join-Path $PSScriptRoot 'App\ClassAlerts-schedule-template.csv'),(Join-Path $PSScriptRoot 'Assets\ClassAlerts.ico') $payload -Force
Set-Content (Join-Path $payload 'installed.flag') 'Installed per-user. Settings: %LOCALAPPDATA%\ClassAlerts\UserData.' -Encoding ascii
$license = Get-Content (Join-Path $PSScriptRoot 'LICENSE.txt') -Raw
$escaped = $license.Replace('\','\\').Replace('{','\{').Replace('}','\}').Replace("`r",'').Replace("`n",'\par ')
$rtf = Join-Path $build 'License.rtf'
Set-Content $rtf ('{\rtf1\ansi\deff0{\fonttbl{\f0 Segoe UI;}}\f0\fs18 Class Alerts\par Francisco Javier Vertedor Postigo\par\par '+$escaped+'}') -Encoding ascii
$object = Join-Path $build 'Product.wixobj'
& (Join-Path $wix 'candle.exe') -nologo -arch x86 -ext (Join-Path $wix 'WixNetFxExtension.dll') ('-dPayload='+$payload) ('-dLicenseRtf='+$rtf) -out $object (Join-Path $PSScriptRoot 'Installer\Product.wxs')
if($LASTEXITCODE -ne 0){throw 'Installer compilation failed'}
$msi = Join-Path $OutputDirectory 'ClassAlerts-1.1.0-User.msi'
& (Join-Path $wix 'light.exe') -nologo -spdb -ext (Join-Path $wix 'WixUIExtension.dll') -ext (Join-Path $wix 'WixNetFxExtension.dll') -out $msi $object
if($LASTEXITCODE -ne 0){throw 'Installer validation failed'}
Write-Host "Built $msi"


