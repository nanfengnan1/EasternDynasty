param([string]$UnityData)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
# Standalone compiler/runtime only; never starts Unity Editor or a client.
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$edRoot=Get-EdProjectRoot
$edMono=Join-Path $UnityData 'MonoBleedingEdge\bin\mono.exe'
$edCompiler=Join-Path $UnityData 'MonoBleedingEdge\lib\mono\4.5\csc.exe'
$edBin=Join-Path $edRoot 'Validation\bin'
New-Item -ItemType Directory -Path $edBin -Force | Out-Null
$edOutput=Join-Path $edBin 'CityFacilityGalleryChecks.exe'
& $edMono $edCompiler /nologo /target:exe /r:System.Drawing.dll /out:$edOutput (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/CityFacilityGalleryLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/CityFacilitySheetLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/MobileScreenLayout.cs') (Join-Path $edRoot 'Validation/CityFacilityGalleryChecks.cs')
if($LASTEXITCODE -ne 0){throw 'City facility gallery checks failed to compile.'}
& $edMono $edOutput $edRoot
if($LASTEXITCODE -ne 0){throw 'City facility gallery checks failed.'}
