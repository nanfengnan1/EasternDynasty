param([string]$UnityData)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$edRoot=Get-EdProjectRoot
$edMono=Join-Path $UnityData 'MonoBleedingEdge\bin\mono.exe'
$edCompiler=Join-Path $UnityData 'MonoBleedingEdge\lib\mono\4.5\csc.exe'
$edBin=Join-Path $edRoot 'Validation\bin'
New-Item -ItemType Directory -Path $edBin -Force|Out-Null
$edOutput=Join-Path $edBin 'UiLineRasterChecks.exe'
& $edMono $edCompiler /nologo /target:exe /out:$edOutput (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/UiLineRaster.cs') (Join-Path $edRoot 'Validation/UiLineRasterChecks.cs')
if($LASTEXITCODE -ne 0){throw 'UI line raster checks failed to compile.'}
& $edMono $edOutput
if($LASTEXITCODE -ne 0){throw 'UI line raster checks failed.'}
