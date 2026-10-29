param([string]$UnityData)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
# Uses the bundled standalone C# compiler/runtime only; never starts the Editor.
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$edRoot=Get-EdProjectRoot
$edMono=Join-Path $UnityData 'MonoBleedingEdge\bin\mono.exe'
$edCompiler=Join-Path $UnityData 'MonoBleedingEdge\lib\mono\4.5\csc.exe'
$edBin=Join-Path $edRoot 'Validation\bin'
New-Item -ItemType Directory -Path $edBin -Force | Out-Null
$edOutput=Join-Path $edBin 'CityFacilitySheetChecks.exe'
& $edMono $edCompiler /nologo /target:exe /r:System.Drawing.dll /out:$edOutput (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/CityFacilitySheetLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/MobileScreenLayout.cs') (Join-Path $edRoot 'Validation/CityFacilitySheetChecks.cs')
if($LASTEXITCODE -ne 0){throw 'City facility sheet checks failed to compile.'}
& $edMono $edOutput $edRoot
if($LASTEXITCODE -ne 0){throw 'City facility sheet checks failed.'}
