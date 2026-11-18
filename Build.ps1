param([string]$ExecutableName = 'ClassAlerts.exe')
$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$source = Join-Path $PSScriptRoot 'Source'
$destination = Join-Path $PSScriptRoot 'App'
New-Item -ItemType Directory -Force $destination | Out-Null
$references = @('System.dll','System.Core.dll','System.Xaml.dll','System.Web.Extensions.dll','System.Windows.Forms.dll','System.Drawing.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$sources = Get-ChildItem $source -Filter '*.cs' | Select-Object -ExpandProperty FullName
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:anycpu /optimize+ ('/win32icon:' + (Join-Path $PSScriptRoot 'Assets\ClassAlerts.ico')) ('/out:' + (Join-Path $destination $ExecutableName)) $references $sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Copy-Item (Join-Path $source 'config.json') $destination -Force
Copy-Item (Join-Path $source 'LICENSE-upstream.txt') $destination -Force
Copy-Item (Join-Path $PSScriptRoot 'LICENSE.txt') $destination -Force
Copy-Item (Join-Path $source 'App.config') (Join-Path $destination ($ExecutableName + '.config')) -Force
Write-Host "Built App\$ExecutableName"


Copy-Item (Join-Path $PSScriptRoot 'Templates\schedule-template.csv') (Join-Path $destination 'ClassAlerts-schedule-template.csv') -Force

