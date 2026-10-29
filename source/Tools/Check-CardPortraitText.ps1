param([string]$UnityData)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$edRoot=Get-EdProjectRoot
$edMono=Join-Path $UnityData 'MonoBleedingEdge\bin\mono.exe'
$edCompiler=Join-Path $UnityData 'MonoBleedingEdge\lib\mono\4.5\csc.exe'
$edBin=Join-Path $edRoot 'Validation\bin'
New-Item -ItemType Directory -Path $edBin -Force|Out-Null
$edOutput=Join-Path $edBin 'CardPortraitTextChecks.exe'
$edModel=@(Get-ChildItem -LiteralPath (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Core') -Filter '*.cs' | ForEach-Object { $_.FullName })
& $edMono $edCompiler /nologo /target:exe /r:System.Drawing.dll /r:System.Runtime.Serialization.dll /out:$edOutput $edModel (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/CardPortraitTextLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/CardPortraitDesignLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/MobileScreenLayout.cs') (Join-Path $edRoot 'Validation/CardPortraitTextChecks.cs') (Join-Path $edRoot 'Validation/CardPortraitDesignChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Card portrait text checks failed to compile.'}
& $edMono $edOutput $edRoot
if($LASTEXITCODE -ne 0){throw 'Card portrait text checks failed.'}
