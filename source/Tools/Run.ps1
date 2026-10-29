param([switch]$Editor,[switch]$Review,[switch]$Legacy,[ValidateRange(640,7680)][int]$PixelWidth=1440,[ValidateRange(480,4320)][int]$PixelHeight=900,[string]$UnityEditor)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$edRoot=Get-EdProjectRoot
if($Editor) {
    if($Review -or $Legacy){throw 'Review and Legacy are Player flags. Omit -Editor to use them.'}
    $edEditor=Resolve-EdUnityEditor $UnityEditor
    Assert-EdEditorAvailable $edEditor
    $edArgs=ConvertTo-EdProcessArguments @('-projectPath',$edRoot)
    $edProcess=Start-Process -FilePath $edEditor -ArgumentList $edArgs -WindowStyle Normal -PassThru
} else {
    $edPlayer=Join-Path $edRoot 'Builds\Windows\EasternDynasty.exe'
    if(-not (Test-Path -LiteralPath $edPlayer)){throw 'Player is not built. Run Tools/Build-Windows.ps1 first.'}
    New-Item -ItemType Directory -Path (Join-Path $edRoot 'Logs') -Force | Out-Null
    $edFlags=@('-screen-fullscreen','0','-screen-width',[string]$PixelWidth,'-screen-height',[string]$PixelHeight,'-logFile',(Join-Path $edRoot 'Logs\player.log'))
    if($Review){$edFlags+='-edReview'}
    if($Legacy){$edFlags+='-edLegacy'}
    $edArgs=ConvertTo-EdProcessArguments $edFlags
    $edProcess=Start-Process -FilePath $edPlayer -WorkingDirectory (Split-Path -Parent $edPlayer) -ArgumentList $edArgs -WindowStyle Normal -PassThru
}
Write-Output ('Started PID '+$edProcess.Id)
