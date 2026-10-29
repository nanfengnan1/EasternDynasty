param([string]$UnityData)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$edRoot=Get-EdProjectRoot
$edMono=Join-Path $UnityData 'MonoBleedingEdge\bin\mono.exe'
$edCompiler=Join-Path $UnityData 'MonoBleedingEdge\lib\mono\4.5\csc.exe'
$edBin=Join-Path $edRoot 'Validation\bin'
New-Item -ItemType Directory -Path $edBin -Force|Out-Null
$edSources=@('MobileMapGesture','MobileScreenLayout','MobileSuspendClock','MobileNavigation')|ForEach-Object{Join-Path $edRoot ('Assets\EasternDynasty\Scripts\Runtime\'+$_+'.cs')}
$edOutput=Join-Path $edBin 'MobileInputChecks.exe'
& $edMono $edCompiler /nologo /target:exe /out:$edOutput $edSources (Join-Path $edRoot 'Validation\MobileInputChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Mobile input checks failed to compile.'}
& $edMono $edOutput
if($LASTEXITCODE -ne 0){throw 'Mobile input checks failed.'}
