param([string]$UnityData)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$edRoot=Get-EdProjectRoot
$edMono=Join-Path $UnityData 'MonoBleedingEdge\bin\mono.exe'
$edCompiler=Join-Path $UnityData 'MonoBleedingEdge\lib\mono\4.5\csc.exe'
$edBin=Join-Path $edRoot 'Validation\bin'
New-Item -ItemType Directory -Path $edBin -Force|Out-Null
$edSource=Join-Path $edRoot 'Assets\EasternDynasty\Scripts\Runtime\CityPanoramaCamera.cs'
$edOutput=Join-Path $edBin 'CityPanoramaCameraChecks.exe'
& $edMono $edCompiler /nologo /target:exe /out:$edOutput $edSource (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/CityPanoramaToolbarLayout.cs') (Join-Path $edRoot 'Assets\EasternDynasty\Scripts\Runtime\CityOverviewLayout.cs') (Join-Path $edRoot 'Assets\EasternDynasty\Scripts\Runtime\MobileScreenLayout.cs') (Join-Path $edRoot 'Validation\CityPanoramaCameraChecks.cs')
if($LASTEXITCODE -ne 0){throw 'City panorama camera checks failed to compile.'}
& $edMono $edOutput
if($LASTEXITCODE -ne 0){throw 'City panorama camera checks failed.'}
