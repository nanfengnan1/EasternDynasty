param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$edCaptureRoot=Split-Path -Parent $PSScriptRoot
$edCaptureScript=Join-Path $edCaptureRoot 'Tools/Check-Experience.ps1'
$edCaptureTokens=$null;$edCaptureErrors=$null
$edCaptureAst=[Management.Automation.Language.Parser]::ParseFile($edCaptureScript,[ref]$edCaptureTokens,[ref]$edCaptureErrors)
if($edCaptureErrors.Count -gt 0){throw ($edCaptureErrors | Out-String)}
# Import only pure data assertions. Never execute the desktop/process script.
foreach($edCaptureName in @('Assert-Experience','Get-ExperienceTarget','Assert-ExperienceTargetInClient','ConvertFrom-ExperienceCapture','Assert-ExperienceMinimap','Assert-ExperienceMinimapFocus','Assert-ExperienceMinimapNavigation')) {
    $edCaptureFunction=@($edCaptureAst.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst]},$true) | Where-Object Name -eq $edCaptureName)
    if($edCaptureFunction.Count -ne 1){throw ('Expected one pure function '+$edCaptureName)}
    . ([scriptblock]::Create($edCaptureFunction[0].Extent.Text))
}
$edExperienceChecks=[Collections.Generic.List[string]]::new()
$edCaptureCases=[Collections.Generic.List[object]]::new()
function Invoke-CaptureCase([string]$Name,[scriptblock]$Action,[bool]$ExpectedPass) {
    $failure=$null
    try {$null=& $Action} catch {$failure=[string]$_}
    if(($null -eq $failure) -ne $ExpectedPass){throw ('CAPTURE_REGRESSION: '+$Name+' expected pass='+$ExpectedPass+'; '+$failure)}
    $edCaptureCases.Add([ordered]@{name=$Name;expected=if($ExpectedPass){'accept'}else{'reject'};passed=$true;rejection=$failure})
}
# Synthetic byte envelopes exercise the completeness gate only. They are NOT
# encoded screenshots and must never be reported as visual or interaction proof.
$edCandidate=[byte[]]::new(1000);$edCandidate[0]=137
[Text.Encoding]::ASCII.GetBytes('IEND').CopyTo($edCandidate,992)
$edJson='{"targets":[{"id":"archive-search","x":20,"y":20,"w":10,"h":10}],"archiveSearch":"99999999","archiveCount":0}'
Invoke-CaptureCase 'accept complete envelope and visible empty-search controls' {ConvertFrom-ExperienceCapture $edCandidate $edJson} $true
Invoke-CaptureCase 'reject missing PNG' {ConvertFrom-ExperienceCapture $null $edJson} $false
Invoke-CaptureCase 'reject truncated PNG' {ConvertFrom-ExperienceCapture $edCandidate[0..998] $edJson} $false
$edBadHeader=$edCandidate.Clone();$edBadHeader[0]=0
Invoke-CaptureCase 'reject wrong PNG signature' {ConvertFrom-ExperienceCapture $edBadHeader $edJson} $false
$edBadEnd=$edCandidate.Clone();$edBadEnd[992]=0
Invoke-CaptureCase 'reject incomplete PNG IEND marker' {ConvertFrom-ExperienceCapture $edBadEnd $edJson} $false
Invoke-CaptureCase 'reject partially written JSON' {ConvertFrom-ExperienceCapture $edCandidate '{"targets":['} $false
Invoke-CaptureCase 'reject missing visible target field' {ConvertFrom-ExperienceCapture $edCandidate '{}'} $false
Invoke-CaptureCase 'reject null visible targets' {ConvertFrom-ExperienceCapture $edCandidate '{"targets":null}'} $false
Invoke-CaptureCase 'reject empty visible targets' {ConvertFrom-ExperienceCapture $edCandidate '{"targets":[]}'} $false

$cw=1440.0;$ch=900.0;$PixelWidth=1440;$PixelHeight=900
$edMapBefore=[pscustomobject]@{tab='领地';mapHudVisible=$true;minimapStyle='youzhou-atlas-v1';speed=0;selected=321;battleWorldHash='frozen-campaign';focusX=9;focusZ=16;minimapNavigationSequence=0;minimapNavigationPointerX=0.;minimapNavigationPointerY=0.;minimapViewportPoints=@(.15,.2,.4,.2,.4,.6,.15,.6);targets=@([pscustomobject]@{id='minimap-area';x=1300.;y=600.;w=204.;h=204.},[pscustomobject]@{id='map-home';x=1300.;y=750.;w=70.;h=44.})}
function Copy-CaptureFixture($Value) {$Value | ConvertTo-Json -Depth 10 -Compress | ConvertFrom-Json}
$edMapAfter=Copy-CaptureFixture $edMapBefore
$edMapAfter.focusX=8.323529411764707;$edMapAfter.focusZ=26.558823529411764
$edMapAfter.minimapNavigationSequence=1;$edMapAfter.minimapNavigationPointerX=1243.;$edMapAfter.minimapNavigationPointerY=564.
$edMapAfter.minimapViewportPoints=@(.03,.1,.3,.1,.3,.4,.03,.4)
# Synthetic Windows click 1243,563 is received at IMGUI pixel 1243,564.
Invoke-CaptureCase 'accept client pixel conversion and observed minimap event mapping' {Assert-ExperienceMinimapNavigation $edMapBefore $edMapAfter 1243 563} $true
$edChanged=Copy-CaptureFixture $edMapAfter;$edChanged.focusZ=12.245098039215687
Invoke-CaptureCase 'reject vertically inverted map navigation' {Assert-ExperienceMinimapNavigation $edMapBefore $edChanged 1243 563} $false
$edChanged=Copy-CaptureFixture $edMapAfter;$edChanged.focusX=9
Invoke-CaptureCase 'reject click target with unchanged camera focus' {Assert-ExperienceMinimapNavigation $edMapBefore $edChanged 1243 563} $false
$edChanged=Copy-CaptureFixture $edMapAfter;$edChanged.selected=322
Invoke-CaptureCase 'reject accidental land selection through minimap' {Assert-ExperienceMinimapNavigation $edMapBefore $edChanged 1243 563} $false
$edChanged=Copy-CaptureFixture $edMapAfter;$edChanged.battleWorldHash='mutated'
Invoke-CaptureCase 'reject campaign mutation during minimap click' {Assert-ExperienceMinimapNavigation $edMapBefore $edChanged 1243 563} $false
$edChanged=Copy-CaptureFixture $edMapAfter;$edChanged.speed=1
Invoke-CaptureCase 'reject nonpaused world comparison' {Assert-ExperienceMinimapNavigation $edMapBefore $edChanged 1243 563} $false
$edChanged=Copy-CaptureFixture $edMapAfter;$edChanged.minimapViewportPoints=$edMapBefore.minimapViewportPoints
Invoke-CaptureCase 'reject stale camera footprint' {Assert-ExperienceMinimapNavigation $edMapBefore $edChanged 1243 563} $false
$edChanged=Copy-CaptureFixture $edMapAfter;$edChanged.minimapViewportPoints[0]=-.1
Invoke-CaptureCase 'reject unclipped camera footprint' {Assert-ExperienceMinimapNavigation $edMapBefore $edChanged 1243 563} $false
$edChanged=Copy-CaptureFixture $edMapAfter;$edChanged.minimapViewportPoints=@(0,0,1,1)
Invoke-CaptureCase 'reject line instead of viewport polygon' {Assert-ExperienceMinimapNavigation $edMapBefore $edChanged 1243 563} $false
$edChanged=Copy-CaptureFixture $edMapAfter;$edChanged.targets=@($edChanged.targets | Where-Object id -ne 'map-home')
Invoke-CaptureCase 'reject missing visible home return' {Assert-ExperienceMinimapNavigation $edMapBefore $edChanged 1243 563} $false

# Compile native declarations without calling any P/Invoke or touching desktop.
$edNativeSource=@($edCaptureAst.FindAll({param($node) $node -is [Management.Automation.Language.StringConstantExpressionAst] -and $node.Value -like '*public static class EDExperiencePointer*'},$true))
if($edNativeSource.Count -ne 1){throw 'Expected one native declaration source.'}
if(-not ('EDExperiencePointer' -as [type])){Add-Type $edNativeSource[0].Value}
$edResult=[ordered]@{status='PASSED';scope='Pure data assertions, synthetic gate fixtures, and native declaration compilation; no Unity, client, screenshots or desktop interaction.';checks=$edCaptureCases.Count;cases=$edCaptureCases.ToArray();scriptSha256=(Get-FileHash -LiteralPath $edCaptureScript -Algorithm SHA256).Hash}
if($OutputDirectory){
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'capture-checks.json'),($edResult | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
}
Write-Output ('EXPERIENCE_CAPTURE_CHECKS_OK: '+$edCaptureCases.Count+' pure cases; no runtime capture or pointer input.')
