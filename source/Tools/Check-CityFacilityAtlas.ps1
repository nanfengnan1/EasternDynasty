param([string]$UnityData)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
# Standalone Mono/compiler only. Does not launch Unity, a player, or desktop interaction.
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$edRoot=Get-EdProjectRoot
$edMono=Join-Path $UnityData 'MonoBleedingEdge\bin\mono.exe'
$edCompiler=Join-Path $UnityData 'MonoBleedingEdge\lib\mono\4.5\csc.exe'
$edBin=Join-Path $edRoot 'Validation\bin'
New-Item -ItemType Directory -Path $edBin -Force | Out-Null
$edOutput=Join-Path $edBin 'CityFacilityAtlasChecks.exe'
$edCore=@(Get-ChildItem -LiteralPath (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Core') -Filter '*.cs' | ForEach-Object {$_.FullName})
& $edMono $edCompiler /nologo /target:exe /r:System.Drawing.dll /out:$edOutput $edCore (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Application/CityFacilityNavigation.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/CityFacilityAtlasLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/MobileScreenLayout.cs') (Join-Path $edRoot 'Validation/CityFacilityAtlasChecks.cs')
if($LASTEXITCODE -ne 0){throw 'City facility atlas checks failed to compile.'}
& $edMono $edOutput $edRoot
if($LASTEXITCODE -ne 0){throw 'City facility atlas checks failed.'}
