param([string]$UnityData)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
# Standalone C# compiler only; never launches Unity or a player.
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$edRoot=Get-EdProjectRoot
$edMono=Join-Path $UnityData 'MonoBleedingEdge\bin\mono.exe'
$edCompiler=Join-Path $UnityData 'MonoBleedingEdge\lib\mono\4.5\csc.exe'
$edBin=Join-Path $edRoot 'Validation\bin'
New-Item -ItemType Directory -Path $edBin -Force | Out-Null
$edOutput=Join-Path $edBin 'ArchiveCardLayoutChecks.exe'
$edCore=@(Get-ChildItem -LiteralPath (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Core') -Filter '*.cs' | ForEach-Object {$_.FullName})
& $edMono $edCompiler /nologo /target:exe /out:$edOutput $edCore (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/ArchiveCardLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/MobileScreenLayout.cs') (Join-Path $edRoot 'Validation/ArchiveCardLayoutChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Archive card layout checks failed to compile.'}
& $edMono $edOutput
if($LASTEXITCODE -ne 0){throw 'Archive card layout checks failed.'}
