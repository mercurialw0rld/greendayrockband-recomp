param([Parameter(Mandatory)][string]$BeatlesWorkspace,[switch]$RebuildGame,[string]$OutputDirectory,[string]$GameBuildDirectory,[string]$SdkDirectory)
$ErrorActionPreference='Stop'
$projectRoot=$PSScriptRoot
$workspace=(Resolve-Path -LiteralPath $BeatlesWorkspace).Path
if(-not $GameBuildDirectory){$GameBuildDirectory=Join-Path $projectRoot 'build-graphics'}
if(-not $OutputDirectory){$OutputDirectory=Join-Path $projectRoot 'portable'}
if($RebuildGame){& (Join-Path $projectRoot 'Build.ps1') -SdkDirectory $SdkDirectory -BuildDirectory $GameBuildDirectory -BeatlesWorkspace $workspace;if($LASTEXITCODE -ne 0){throw 'Could not build the port'}}
$runtime=Join-Path $OutputDirectory 'runtime'
New-Item -ItemType Directory -Path $runtime -Force | Out-Null
$src=Join-Path $projectRoot 'launcher-src'
$csc=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$compilerArgs=@('/nologo','/target:winexe','/platform:x64','/optimize+','/codepage:65001',('/out:'+(Join-Path $OutputDirectory 'GreenDayRockBandLauncher.exe')),('/win32manifest:'+(Join-Path $src 'launcher.manifest')),('/win32icon:'+(Join-Path $src 'launcher.ico')),'/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:System.Runtime.Serialization.dll',(Join-Path $src 'LauncherCore.cs'),(Join-Path $src 'LauncherApp.cs'),(Join-Path $src 'Controls.cs'),(Join-Path $src 'ControlsForm.cs'))
$compilerArgs += @((Join-Path $src 'AeroTheme.cs'),('/resource:'+(Join-Path $src 'assets/greenday-xbox360-cover.jpg')+',GreenDayLauncher.Cover.jpg'))
$compilerArgs += (Join-Path $src 'ControllerDevices.cs')
$compilerArgs += (Join-Path $src 'Responsive.cs')
$compilerArgs += @((Join-Path $src 'DlcInstaller.cs'),(Join-Path $src 'DlcForm.cs'))
& $csc @compilerArgs
if($LASTEXITCODE -ne 0){throw 'Could not compile the launcher'}
Copy-Item -LiteralPath (Join-Path $src 'GreenDayRockBandLauncher.exe.config') -Destination $OutputDirectory -Force
foreach($name in @('greendayrockband.exe','rexruntime.dll','rexgpu-xenos.dll','beatles-input.dll','greenday-content.exe')){Copy-Item -LiteralPath (Join-Path $GameBuildDirectory $name) -Destination $runtime -Force}
$crt=Join-Path $workspace 'work/tools/vsbuild/VC/Redist/MSVC/14.44.35112/x64/Microsoft.VC143.CRT'
foreach($file in (Get-ChildItem -LiteralPath $crt -Filter '*.dll' -File)){Copy-Item -LiteralPath $file.FullName -Destination $runtime -Force}
Copy-Item -LiteralPath (Join-Path $src 'game-files.json') -Destination $OutputDirectory -Force
$lines=@('# Portable defaults. Data paths are supplied by the launcher.') + @(Get-Content -LiteralPath (Join-Path $projectRoot 'greendayrockband.toml') -Encoding UTF8 | Where-Object {$_ -notmatch '^\s*(game_data_root|user_data_root|cache_root|log_file)\s*=' -and $_ -notmatch '^# Local test defaults'})
[IO.File]::WriteAllText((Join-Path $runtime 'greendayrockband.toml'),(($lines -join [Environment]::NewLine)+[Environment]::NewLine),[Text.UTF8Encoding]::new($false))
$licenses=Join-Path $OutputDirectory 'licenses'
New-Item -ItemType Directory -Path $licenses -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $workspace 'work/references/rexglue-sdk/LICENSE') -Destination (Join-Path $licenses 'ReXGlue-LICENSE.txt') -Force
Copy-Item -LiteralPath (Join-Path $workspace 'work/tools/rexglue-sdk/win-amd64/licenses/SDL3/LICENSE.txt') -Destination (Join-Path $licenses 'SDL3-LICENSE.txt') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $OutputDirectory -Force
Write-Output ('Portable launcher: '+$OutputDirectory)




