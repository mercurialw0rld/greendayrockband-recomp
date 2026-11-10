param([Parameter(Mandatory)][string]$BeatlesWorkspace,[int]$Jobs = 3,[string]$SdkDirectory,[string]$BuildDirectory,[string]$Target)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$workspaceRoot = (Resolve-Path -LiteralPath $BeatlesWorkspace).Path
$toolsRoot = Join-Path $workspaceRoot 'work/tools'
$vsRoot = Join-Path $toolsRoot 'vsbuild'
$devcmd = Join-Path $vsRoot 'Common7/Tools/VsDevCmd.bat'
$cmake = Join-Path $vsRoot 'Common7/IDE/CommonExtensions/Microsoft/CMake/CMake/bin/cmake.exe'
$clang = Join-Path $vsRoot 'VC/Tools/Llvm/bin/clang-cl.exe'
$ninja = Join-Path $toolsRoot 'ninja/ninja.exe'
$sdk = if($SdkDirectory){[IO.Path]::GetFullPath($SdkDirectory)}else{Join-Path $workspaceRoot 'work/graphics-sdk/out/install/beatles'}
$build = if($BuildDirectory){[IO.Path]::GetFullPath($BuildDirectory)}else{Join-Path $projectRoot 'build-graphics'}
if(-not (Test-Path -LiteralPath (Join-Path $sdk 'lib/cmake/rexglue/rexglueConfig.cmake'))){throw 'Build the patched SDK in the Beatles workspace, or pass -SdkDirectory explicitly.'}
foreach ($file in @($devcmd,$cmake,$clang,$ninja)) {
  if (-not (Test-Path -LiteralPath $file)) { throw "Missing build tool: $file" }
}
if (-not (Test-Path -LiteralPath (Join-Path $projectRoot 'generated/default/sources.cmake'))) {
  Push-Location -LiteralPath $projectRoot
  try {
    & (Join-Path $sdk 'bin/rexglue.exe') codegen (Join-Path $projectRoot 'greendayrockband_manifest.toml')
    if ($LASTEXITCODE -ne 0) { throw 'Could not generate code from the XEX' }
  } finally {
    Pop-Location
  }
}
$envLines = & $env:ComSpec /d /s /c ('""{0}" -no_logo -arch=x64 -host_arch=x64 >nul && set"' -f $devcmd)
if ($LASTEXITCODE -ne 0) { throw 'Could not initialize the C++ toolchain' }
foreach ($line in $envLines) {
  if ($line -match '^([^=]+)=(.*)$') { [Environment]::SetEnvironmentVariable($matches[1], $matches[2], 'Process') }
}
$env:Path = (Split-Path $clang) + ';' + (Split-Path $ninja) + ';' + $env:Path
& $cmake -S $projectRoot -B $build -G Ninja "-DCMAKE_MAKE_PROGRAM=$ninja" "-DCMAKE_CXX_COMPILER=$clang" -DCMAKE_CXX_COMPILER_TARGET=x86_64-pc-windows-msvc -DCMAKE_BUILD_TYPE=Release "-Drexglue_DIR=$sdk/lib/cmake/rexglue"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$targetArgs = if ($Target) { @('--target', $Target) } else { @() }
& $cmake --build $build --parallel $Jobs @targetArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item -LiteralPath "$projectRoot/greendayrockband.toml" -Destination (Join-Path $build 'greendayrockband.toml') -Force



