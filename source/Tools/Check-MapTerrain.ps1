param([string]$UnityData)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$terrainRoot=Split-Path -Parent $PSScriptRoot
$terrainBin=Join-Path $terrainRoot 'Validation/bin'
New-Item -ItemType Directory -Path $terrainBin -Force | Out-Null
$terrainMono=Join-Path $UnityData 'MonoBleedingEdge/bin/mono.exe'
$terrainCompiler=Join-Path $UnityData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$terrainCore=@(Get-ChildItem -LiteralPath (Join-Path $terrainRoot 'Assets/EasternDynasty/Scripts/Core') -Filter '*.cs' | ForEach-Object {$_.FullName})
$terrainExe=Join-Path $terrainBin 'MapTerrainChecks.exe'
& $terrainMono $terrainCompiler /nologo /target:exe /r:System.Runtime.Serialization.dll /out:$terrainExe $terrainCore (Join-Path $terrainRoot 'Assets/EasternDynasty/Scripts/Runtime/YouzhouTerrainLayout.cs') (Join-Path $terrainRoot 'Validation/MapTerrainChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Map terrain checks failed to compile.'}
& $terrainMono $terrainExe
if($LASTEXITCODE -ne 0){throw 'Map terrain checks failed.'}
