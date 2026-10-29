param([string]$UnityData)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$edRoot=Get-EdProjectRoot
$edMono=Join-Path $UnityData 'MonoBleedingEdge/bin/mono.exe'
$edCompiler=Join-Path $UnityData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$edExe=Join-Path $edRoot 'Validation/bin/MapHudNavigationChecks.exe'
& $edMono $edCompiler /nologo /target:exe /out:$edExe (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/MobileScreenLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/MapPointerGesture.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/MapHudNavigationLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/MapHudHeaderLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/MapHudQuestLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/MapHudArmyLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/MapHudAtlasLayout.cs') (Join-Path $edRoot 'Validation/MapHudNavigationChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Map navigation checks failed to compile.'}
& $edMono $edExe
if($LASTEXITCODE -ne 0){throw 'Map navigation checks failed.'}
