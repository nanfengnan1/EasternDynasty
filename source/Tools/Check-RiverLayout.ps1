param([string]$UnityData)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$riverRoot=Split-Path -Parent $PSScriptRoot
$riverBin=Join-Path $riverRoot 'Validation/bin'
New-Item -ItemType Directory -Path $riverBin -Force | Out-Null
$riverMono=Join-Path $UnityData 'MonoBleedingEdge/bin/mono.exe'
$riverCompiler=Join-Path $UnityData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$riverCore=@(Get-ChildItem -LiteralPath (Join-Path $riverRoot 'Assets/EasternDynasty/Scripts/Core') -Filter '*.cs' | ForEach-Object {$_.FullName})
$riverExe=Join-Path $riverBin 'RiverLayoutChecks.exe'
& $riverMono $riverCompiler /nologo /target:exe /r:System.Runtime.Serialization.dll /out:$riverExe $riverCore (Join-Path $riverRoot 'Assets/EasternDynasty/Scripts/Runtime/YouzhouRiverLayout.cs') (Join-Path $riverRoot 'Validation/RiverLayoutChecks.cs')
if($LASTEXITCODE -ne 0){throw 'River layout checks failed to compile.'}
& $riverMono $riverExe
if($LASTEXITCODE -ne 0){throw 'River layout checks failed.'}
