param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$edLandRoot=Split-Path -Parent $PSScriptRoot
$edLandScript=Join-Path $edLandRoot 'Tools/Check-Experience.ps1'
$edLandTokens=$null;$edLandErrors=$null
$edLandAst=[Management.Automation.Language.Parser]::ParseInput([IO.File]::ReadAllText($edLandScript,[Text.Encoding]::UTF8),[ref]$edLandTokens,[ref]$edLandErrors)
if($edLandErrors.Count -gt 0){throw ($edLandErrors | Out-String)}
# Import only data assertions and the label-routing helper. The click endpoint
# below is a local recorder; no client, native declarations or desktop are run.
foreach($edLandName in @('Assert-Experience','Get-ExperienceTarget','Test-ExperienceLandContains','Test-ExperienceLandOverlap','Assert-ExperienceLand','Assert-ExperienceLandModal','Click-ExperienceLandCaption')) {
    $edLandFunction=@($edLandAst.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst]},$true) | Where-Object Name -eq $edLandName)
    if($edLandFunction.Count -ne 1){throw ('Expected one land assertion/helper: '+$edLandName)}
    . ([scriptblock]::Create($edLandFunction[0].Extent.Text))
}
$edExperienceChecks=[Collections.Generic.List[string]]::new()
$edLandCases=[Collections.Generic.List[object]]::new()
$script:edLandRecordedClick=''
function Click-ExperienceTarget($State,[string]$Id) { $null=Get-ExperienceTarget $State $Id; $script:edLandRecordedClick=$Id }
function Invoke-LandCase([string]$Name,[scriptblock]$Action,[bool]$ExpectedPass) {
    $failure=$null
    try {$null=& $Action} catch {$failure=[string]$_}
    if(($null -eq $failure) -ne $ExpectedPass){throw ('LAND_ASSERTION_REGRESSION: '+$Name+' expected pass='+$ExpectedPass+'; '+$failure)}
    $edLandCases.Add([ordered]@{name=$Name;expected=if($ExpectedPass){'accept'}else{'reject'};passed=$true;rejection=$failure})
}
function New-LandFixture([string]$State,[double]$Scale) {
    # Explicit synthetic target records for the inspected independent surfaces.
    # They verify acceptance/rejection logic, never runtime visual appearance.
    $records=[Collections.Generic.List[object]]::new()
    function Add-LandTarget([string]$Id,[double]$X,[double]$Y,[double]$W,[double]$H) {
        $records.Add([pscustomobject]@{id=$Id;x=56+(880+$X+$W*.5)*$Scale;y=12+(113+$Y+$H*.5)*$Scale;w=$W*$Scale;h=$H*$Scale})
    }
    Add-LandTarget 'land-panel' 0 0 280 228
    $menuHeight=switch($State){'owned'{292};'busy'{216};'city'{144};default{268}}
    $infoHeight=switch($State){'owned'{116};'busy'{96};'city'{76};default{200}}
    $menuY=(228-$menuHeight)*.5
    Add-LandTarget 'land-command-surface' 300 $menuY 240 $menuHeight
    Add-LandTarget 'land-order-information' 300 $menuY 240 $infoHeight
    Add-LandTarget 'land-art' 12 64 256 84
    Add-LandTarget 'land-art-image' 12 68 156 78
    Add-LandTarget 'land-art-identity' 178 72 90 70
    Add-LandTarget 'land-summary' 16 156 248 60
    Add-LandTarget 'land-fact-defense' 16 156 248 28
    Add-LandTarget 'land-fact-yield' 16 188 248 28
    Add-LandTarget 'land-actions' 316 ($menuY+12) 208 ($menuHeight-24)
    Add-LandTarget 'land-close' 224 12 44 44
    Add-LandTarget 'map-close' 224 12 44 44
    $commands=@()
    switch($State) {
        'owned' {$commands=@(@('army-garrison',316,($menuY+128),208,5),@('army-farm',316,($menuY+180),208,5),@('land-training',316,($menuY+232),208,5))}
        'busy' {$commands=@(@('army-recall',316,($menuY+108),208,5),@('land-training',316,($menuY+160),208,5))}
        'city' {$commands=,@('land-enter-city',316,($menuY+88),208,5)}
        default {$commands=,@('army-attack',316,($menuY+212),208,5)}
    }
    foreach($command in $commands) {
        Add-LandTarget $command[0] $command[1] $command[2] $command[3] 44
        Add-LandTarget ($command[0]+'-icon') ($command[1]+$command[4]) ($command[2]+5) 34 34
        Add-LandTarget ($command[0]+'-label') ($command[1]+48) ($command[2]+4) 156 36
    }
    return [pscustomobject]@{landPanelVisible=$true;landCommandTextFits=$true;landPanelStyle='land-compact-facts-v5';landPanelLandId=13;selected=13;landPanelKind='Grain';tab='领地';confirmationOpen=$false;cardDetail='';landPanelState=$State;landTouchMinimum=44*$Scale;mapHudSafeX=56;mapHudSafeY=12;mapHudSafeWidth=1440*$Scale;mapHudSafeHeight=820*$Scale;targets=$records.ToArray()}
}
function Copy-LandFixture($State) { $State | ConvertTo-Json -Depth 8 -Compress | ConvertFrom-Json }
foreach($edLandScale in @(1,.8,(684.0/820))) {
    foreach($edLandState in @('neutral','owned','busy','city','blocked')) {
        $edLandFixture=New-LandFixture $edLandState $edLandScale
        Invoke-LandCase ('accept '+$edLandState+' scaled '+$edLandScale) {Assert-ExperienceLand $edLandFixture} $true
    }
}
$edLandOwned=New-LandFixture 'owned' 1
$edMutation=Copy-LandFixture $edLandOwned;(Get-ExperienceTarget $edMutation 'land-panel').h=266
Invoke-LandCase 'reject obsolete 266-high facts surface' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;(Get-ExperienceTarget $edMutation 'land-summary').h=102
Invoke-LandCase 'reject obsolete 102-high facts summary' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;(Get-ExperienceTarget $edMutation 'land-art-image').w=140;(Get-ExperienceTarget $edMutation 'land-art-image').h=70
Invoke-LandCase 'reject reduced complete terrain image region' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;(Get-ExperienceTarget $edMutation 'land-art-identity').x+=8
Invoke-LandCase 'reject identity shifted from inspected design' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;$edMutation.targets=@($edMutation.targets | Where-Object id -ne 'land-fact-defense')
Invoke-LandCase 'reject missing actual defense row' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;(Get-ExperienceTarget $edMutation 'land-fact-defense').w=118;(Get-ExperienceTarget $edMutation 'land-fact-yield').w=118
Invoke-LandCase 'reject obsolete narrow factual columns' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;(Get-ExperienceTarget $edMutation 'land-fact-yield').y=(Get-ExperienceTarget $edMutation 'land-fact-defense').y
Invoke-LandCase 'reject overlapping factual rows' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;(Get-ExperienceTarget $edMutation 'land-fact-defense').h=30
Invoke-LandCase 'reject factual row taller than the inspected design' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;(Get-ExperienceTarget $edMutation 'land-fact-yield').x+=6
Invoke-LandCase 'reject misaligned yield row' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;$edMutation.landPanelStyle='land-detached-orders-v4'
Invoke-LandCase 'reject prior detached-orders-only composition identity' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;$edMutation.targets=@($edMutation.targets | Where-Object id -ne 'land-command-surface')
Invoke-LandCase 'reject missing visible command surface' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;(Get-ExperienceTarget $edMutation 'land-command-surface').x=(Get-ExperienceTarget $edMutation 'land-panel').x
Invoke-LandCase 'reject command surface over facts' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;(Get-ExperienceTarget $edMutation 'army-farm').y=(Get-ExperienceTarget $edMutation 'army-garrison').y
Invoke-LandCase 'reject overlapping vertical actions' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;$edMutation.landCommandTextFits=$false
Invoke-LandCase 'reject measured command text overflow' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;$edMutation.targets=@($edMutation.targets | Where-Object id -ne 'land-training-label')
Invoke-LandCase 'reject missing actual label target' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;$edMutation.targets+=Get-ExperienceTarget $edMutation 'land-training-label'
Invoke-LandCase 'reject duplicate label target' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;(Get-ExperienceTarget $edMutation 'land-training').h=72
Invoke-LandCase 'reject inherited oversized 72-high command' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;(Get-ExperienceTarget $edMutation 'land-training-label').x=(Get-ExperienceTarget $edMutation 'land-training-icon').x
Invoke-LandCase 'reject label over actual command icon' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;(Get-ExperienceTarget $edMutation 'land-training-label').w=10
Invoke-LandCase 'reject label recorded too narrowly' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;(Get-ExperienceTarget $edMutation 'land-training-icon').y+=8
Invoke-LandCase 'reject icon outside vertical row center' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;(Get-ExperienceTarget $edMutation 'land-actions').h=136
Invoke-LandCase 'reject obsolete actions region dimensions' {Assert-ExperienceLand $edMutation} $false
$edMutation=Copy-LandFixture $edLandOwned;$edMutation.landPanelStyle='land-floating-design-v1'
Invoke-LandCase 'reject obsolete composition identity' {Assert-ExperienceLand $edMutation} $false
$edModal=[pscustomobject]@{landPanelVisible=$false;landCommandTextFits=$false;tab='领地';targets=@()}
Invoke-LandCase 'accept covered panel with no targets' {Assert-ExperienceLandModal $edModal} $true
foreach($edLeak in @('land-command-surface','army-attack-label','army-garrison-label','army-farm-label','army-recall-label','land-training-label','land-enter-city-label')) {
    $edMutation=Copy-LandFixture $edModal;$edMutation.targets=@([pscustomobject]@{id=$edLeak;x=10;y=10;w=44;h=36})
    Invoke-LandCase ('reject covered label leak '+$edLeak) {Assert-ExperienceLandModal $edMutation} $false
}
$edMutation=Copy-LandFixture $edModal;$edMutation.landCommandTextFits=$true
Invoke-LandCase 'reject stale hidden-panel text-fit diagnostic' {Assert-ExperienceLandModal $edMutation} $false
$edRail=Copy-LandFixture $edModal;$edRail.targets=@([pscustomobject]@{id='army-recall';x=200;y=600;w=171;h=44},[pscustomobject]@{id='hud-army-recall';x=200;y=600;w=171;h=44})
Invoke-LandCase 'accept independent army-rail recall alias under modal' {Assert-ExperienceLandModal $edRail} $true
$edMutation=Copy-LandFixture $edRail;(Get-ExperienceTarget $edMutation 'army-recall').x+=100
Invoke-LandCase 'reject land recall confused with independent rail' {Assert-ExperienceLandModal $edMutation} $false
Invoke-LandCase 'label helper routes through actual rendered target' {
    Click-ExperienceLandCaption $edLandOwned 'land-training'
    if($script:edLandRecordedClick -ne 'land-training-label'){throw 'Helper did not use the rendered label ID.'}
} $true
$edMutation=Copy-LandFixture $edLandOwned;$edMutation.targets=@($edMutation.targets | Where-Object id -ne 'land-training-label')
Invoke-LandCase 'label helper rejects missing drawing instead of guessing a coordinate' {Click-ExperienceLandCaption $edMutation 'land-training'} $false
$edResult=[ordered]@{status='PASSED';scope='Pure assertion and label-routing checks with explicit synthetic records. No Unity, client, screenshot or pointer input.';checks=$edLandCases.Count;cases=$edLandCases.ToArray();scriptSha256=(Get-FileHash -LiteralPath $edLandScript -Algorithm SHA256).Hash}
if($OutputDirectory){
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'land-experience-checks.json'),($edResult | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
}
Write-Output ('LAND_EXPERIENCE_CHECKS_OK: '+$edLandCases.Count+' pure acceptance/rejection cases; no runtime visual or input claim.')
