param([string]$UnityEditor,[switch]$SkipImport)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$edRoot=Get-EdProjectRoot
$edEditor=Resolve-EdUnityEditor $UnityEditor
$edVersion=Get-EdUnityVersion
$edData=Join-Path (Split-Path -Parent $edEditor) 'Data'
foreach($edRequired in @('MonoBleedingEdge\bin\mono.exe','MonoBleedingEdge\lib\mono\4.5\csc.exe','PlaybackEngines\windowsstandalonesupport')) {
    if(-not (Test-Path -LiteralPath (Join-Path $edData $edRequired))){throw ('Unity component missing: '+$edRequired+'. Add Windows build support in Unity Hub.')}
}
$edFileVersion=(Get-Item -LiteralPath $edEditor).VersionInfo.ProductVersion
if($edFileVersion -and -not $edFileVersion.StartsWith($edVersion,[StringComparison]::OrdinalIgnoreCase)){throw ('Editor version differs: expected '+$edVersion+', found '+$edFileVersion)}
New-Item -ItemType Directory -Path (Join-Path $edRoot '.local') -Force | Out-Null
$edConfig=@{unityEditor=$edEditor;unityVersion=$edVersion} | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $edRoot '.local\unity.json'),$edConfig,[Text.UTF8Encoding]::new($false))
if(-not $SkipImport){Invoke-EdUnity $edEditor 'EasternDynasty.Editor.ProjectTools.Prepare' (Join-Path $edRoot 'setup.log')}
Write-Output ('READY: Unity '+$edVersion+'; '+$edRoot)
Write-Output 'Next: Tools/Validate.ps1, Tools/Build-Windows.ps1, then Tools/Run.ps1.'
