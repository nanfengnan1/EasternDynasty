param([string]$UnityEditor)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$edRoot = Get-EdProjectRoot
$edLog = Join-Path $edRoot 'build.log'
Invoke-EdUnity $UnityEditor 'EasternDynasty.Editor.ProjectTools.BuildWindows' $edLog
Write-Output (Join-Path $edRoot 'Builds\Windows\EasternDynasty.exe')
