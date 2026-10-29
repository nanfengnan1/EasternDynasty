param([string]$UnityData)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$edRoot=Get-EdProjectRoot
$edMono=Join-Path $UnityData 'MonoBleedingEdge\bin\mono.exe'
$edCompiler=Join-Path $UnityData 'MonoBleedingEdge\lib\mono\4.5\csc.exe'
$edBin=Join-Path $edRoot 'Validation\bin'
New-Item -ItemType Directory -Path $edBin -Force|Out-Null
$edOutput=Join-Path $edBin 'CardDossierSkillLayoutChecks.exe'
$edModel=@(Get-ChildItem -LiteralPath (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Core') -Filter '*.cs' | ForEach-Object { $_.FullName })
& $edMono $edCompiler /nologo /target:exe /r:System.Drawing.dll /out:$edOutput $edModel (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/CardDossierSkillLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/MobileScreenLayout.cs') (Join-Path $edRoot 'Validation/CardDossierSkillLayoutChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Card dossier skill checks failed to compile.'}
& $edMono $edOutput
if($LASTEXITCODE -ne 0){throw 'Card dossier skill checks failed.'}
