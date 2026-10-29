param([string]$UnityData)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$edRoot=Get-EdProjectRoot
$edMono=Join-Path $UnityData 'MonoBleedingEdge\bin\mono.exe'
$edCompiler=Join-Path $UnityData 'MonoBleedingEdge\lib\mono\4.5\csc.exe'
$edBin=Join-Path $edRoot 'Validation\bin'
New-Item -ItemType Directory -Path $edBin -Force | Out-Null
$edOutput=Join-Path $edBin 'CardChromeChecks.exe'
& $edMono $edCompiler /nologo /target:exe /out:$edOutput (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/CardPortraitDesignLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/CardChromeLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/CardDossierDesignLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/MobileScreenLayout.cs') (Join-Path $edRoot 'Validation/CardChromeChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Card chrome checks failed to compile.'}
& $edMono $edOutput
if($LASTEXITCODE -ne 0){throw 'Card chrome checks failed.'}
