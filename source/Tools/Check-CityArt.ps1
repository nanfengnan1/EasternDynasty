param([string]$UnityData,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$cityArtRoot=Split-Path -Parent $PSScriptRoot
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
if(-not $OutputDirectory){$OutputDirectory=Join-Path $cityArtRoot 'Validation/CityArt'}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$cityArtBin=Join-Path $cityArtRoot 'Validation/bin'
New-Item -ItemType Directory -Path $cityArtBin -Force | Out-Null
$cityArtMono=Join-Path $UnityData 'MonoBleedingEdge/bin/mono.exe'
$cityArtCompiler=Join-Path $UnityData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$cityArtEngine=Join-Path $UnityData 'Managed/UnityEngine'
$cityArtCore=Join-Path $cityArtEngine 'UnityEngine.CoreModule.dll'
$cityArtFacade=Join-Path $UnityData 'UnityReferenceAssemblies/unity-4.8-api/Facades/netstandard.dll'
$cityArtExe=Join-Path $cityArtBin 'CityArtChecks.exe'
$cityArtSource=Join-Path $cityArtRoot 'Assets/EasternDynasty/Scripts/Runtime/CitySceneRaster.cs'
$cityArtModels=@(Get-ChildItem -LiteralPath (Join-Path $cityArtRoot 'Assets/EasternDynasty/Scripts/Core') -Filter '*.cs' | ForEach-Object {$_.FullName})
& $cityArtMono $cityArtCompiler /nologo /target:exe /r:System.Drawing.dll /r:System.Runtime.Serialization.dll /r:$cityArtCore /r:$cityArtFacade /out:$cityArtExe $cityArtModels (Join-Path $cityArtRoot 'Validation/CityArtChecks.cs') $cityArtSource
if($LASTEXITCODE -ne 0){throw 'City facility art checks failed to compile.'}
$cityArtPriorMonoPath=$env:MONO_PATH
try {
    $env:MONO_PATH=$cityArtEngine
    & $cityArtMono $cityArtExe $OutputDirectory $cityArtSource
    if($LASTEXITCODE -ne 0){throw 'City art checks failed: inspect facility bounds, production overview state transitions and deterministic read-only rendering.'}
} finally {$env:MONO_PATH=$cityArtPriorMonoPath}
Write-Output 'CITY_ART_CHECKS_OK: 9 production facility rasters fit the actual detail tile; four production overview states and scaled previews have SHA256 evidence; generated previews are not Unity or phone screenshots.'
