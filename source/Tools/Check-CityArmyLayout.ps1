param([string]$UnityData)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$edRoot=Get-EdProjectRoot
$edMono=Join-Path $UnityData 'MonoBleedingEdge\bin\mono.exe'
$edCompiler=Join-Path $UnityData 'MonoBleedingEdge\lib\mono\4.5\csc.exe'
$edBin=Join-Path $edRoot 'Validation\bin'
New-Item -ItemType Directory -Path $edBin -Force|Out-Null
$edOutput=Join-Path $edBin 'CityArmyLayoutChecks.exe'
$edSources=@('CityPanoramaCamera.cs','CityOverviewLayout.cs','CityArmyLayout.cs','MobileScreenLayout.cs') | ForEach-Object {Join-Path $edRoot ('Assets/EasternDynasty/Scripts/Runtime/'+$_)}
& $edMono $edCompiler /nologo /target:exe /r:System.Runtime.Serialization.dll /out:$edOutput $edSources (Join-Path $edRoot 'Validation/CityArmyLayoutChecks.cs')
if($LASTEXITCODE -ne 0){throw 'City army layout checks failed to compile.'}
& $edMono $edOutput $edRoot
if($LASTEXITCODE -ne 0){throw 'City army layout checks failed.'}
