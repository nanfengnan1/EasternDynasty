param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$edArmyRoot=Split-Path -Parent $PSScriptRoot
$edArmyTokens=$null;$edArmyErrors=$null
$edArmyAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $edArmyRoot 'Tools/Check-Experience.ps1'),[ref]$edArmyTokens,[ref]$edArmyErrors)
if($edArmyErrors.Count -gt 0){throw ($edArmyErrors | Out-String)}
# Load data assertions only. No native pointer code, client or desktop code runs.
foreach($edArmyName in @('Assert-Experience','Get-ExperienceTarget','Assert-ExperienceMapCommand','Test-ExperienceLandContains','Test-ExperienceLandOverlap','Get-ExperienceMapArmyGap','Assert-ExperienceMapArmyGap','Assert-ExperienceMapStill')) {
    $edArmyFunction=@($edArmyAst.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst]},$true) | Where-Object {$_.Name -eq $edArmyName})
    if($edArmyFunction.Count -ne 1){throw ('Expected one army assertion: '+$edArmyName)}
    . ([scriptblock]::Create($edArmyFunction[0].Extent.Text))
}
$edExperienceChecks=[Collections.Generic.List[string]]::new()
$edArmyCases=[Collections.Generic.List[object]]::new()
function Copy-ArmyState($State){$State | ConvertTo-Json -Depth 8 -Compress | ConvertFrom-Json}
function New-SyntheticArmyState([double]$Unit=1,[int]$Count=3,[bool]$Training=$false,[bool]$Watch=$false) {
    $records=[Collections.Generic.List[object]]::new();$height=820.0;$armyY=424.0
    function Add-ArmyTarget([string]$Id,[double]$X,[double]$Y,[double]$W,[double]$H) {
        $records.Add([pscustomobject]@{id=$Id;x=56+($X+$W*.5)*$Unit;y=12+($Y+$H*.5)*$Unit;w=$W*$Unit;h=$H*$Unit})
    }
    # Explicit synthetic shared-footer design; these are not publisher captures.
    $ids=@('city-open','map-tab','roster-tab','reports-tab','hud-help','recruit-tab')
    for($i=0;$i -lt $ids.Count;$i++) {
        $x=20+$i*116;$y=$height-74
        if($ids[$i] -eq 'recruit-tab') {
            $x=1500-152
            Add-ArmyTarget $ids[$i] $x $y 132 70
            Add-ArmyTarget ($ids[$i]+'-art') ($x+46) ($y+7) 40 38
            Add-ArmyTarget ($ids[$i]+'-caption') ($x+8) ($y+47) 116 22
        } else {
            Add-ArmyTarget $ids[$i] $x $y 108 70
            Add-ArmyTarget ($ids[$i]+'-art') ($x+4) ($y+3) 100 42
            Add-ArmyTarget ($ids[$i]+'-caption') ($x+4) ($y+46) 100 22
        }
    }
    Add-ArmyTarget 'hud-army-panel' 20 $armyY 354 302
    Add-ArmyTarget 'hud-army-header' 20 $armyY 354 28
    Add-ArmyTarget 'hud-army-status' 20 660 354 20
    Add-ArmyTarget 'army-reinforcement' 20 682 171 44
    Add-ArmyTarget $(if($Training){'hud-army-recall'}else{'hud-army-formation'}) 203 682 171 44
    Add-ArmyTarget 'hud-quest-panel' 20 90 354 120
    Add-ArmyTarget 'quest-land-return' 20 278 268 40
    for($i=0;$i -lt $Count;$i++) {
        $y=456+$i*68
        Add-ArmyTarget ('hud-army-card-'+$i) 20 $y 354 64
        Add-ArmyTarget ('hud-army-art-'+$i) 24 ($y+1) 44 62
        Add-ArmyTarget ('hud-army-name-'+$i) 82 ($y+12) 164 40
        Add-ArmyTarget ('hud-army-identity-'+$i) 262 ($y+12) 102 40
    }
    if($Training){Add-ArmyTarget 'army-training-progress' 20 373 354 44}
    if($Watch){Add-ArmyTarget 'army-battle-watch' 20 $(if($Training){322}else{373}) 354 44}
    $uids=@();for($i=0;$i -lt $Count;$i++){$uids+=100+$i}
    return [pscustomobject]@{mapHudTouchMinimum=44*$Unit;mapHudSafeX=56;mapHudSafeY=12;mapHudSafeWidth=1500*$Unit;mapHudSafeHeight=$height*$Unit;mapHudArmyUids=$uids;orderActive=$Training;trainingActive=$Training;targets=@($records.ToArray())}
}
function Invoke-ArmyCase([string]$Name,$State,[bool]$ExpectedPass) {
    $failure=$null;try{Assert-ExperienceMapCommand $State}catch{$failure=[string]$_}
    if(($null -eq $failure) -ne $ExpectedPass){throw ('ARMY_ASSERTION_REGRESSION: '+$Name+' expected pass='+$ExpectedPass+'; '+$failure)}
    $edArmyCases.Add([ordered]@{name=$Name;expected=if($ExpectedPass){'accept'}else{'reject'};passed=$true;rejection=$failure})
}
foreach($edArmyScale in @(1,.8,(684.0/820))) {
    foreach($edArmyCount in @(0,1,3)) {
        Invoke-ArmyCase ('accept synthetic '+$edArmyCount+' generals at '+$edArmyScale) (New-SyntheticArmyState $edArmyScale $edArmyCount) $true
    }
    Invoke-ArmyCase ('accept training and previous battle together at '+$edArmyScale) (New-SyntheticArmyState $edArmyScale 3 $true $true) $true
    Invoke-ArmyCase ('accept battle without training at '+$edArmyScale) (New-SyntheticArmyState $edArmyScale 3 $false $true) $true
}
$edArmyBase=New-SyntheticArmyState
$edMutation=Copy-ArmyState $edArmyBase
$edOldButton=Get-ExperienceTarget $edMutation 'city-open'
$edOldArt=Get-ExperienceTarget $edMutation 'city-open-art';$edOldCaption=Get-ExperienceTarget $edMutation 'city-open-caption'
$edOldLeft=$edOldButton.x-$edOldButton.w/2;$edOldTop=$edOldButton.y-$edOldButton.h/2
$edOldArt.x=$edOldLeft+69;$edOldArt.y=$edOldTop+36;$edOldArt.w=70;$edOldArt.h=56
$edOldCaption.x=$edOldLeft+17.5;$edOldCaption.y=$edOldTop+29;$edOldCaption.w=25;$edOldCaption.h=50
Invoke-ArmyCase 'reject obsolete vertical footer even when army rows are valid' $edMutation $false
$edMutation=Copy-ArmyState $edArmyBase;(Get-ExperienceTarget $edMutation 'hud-army-panel').h=242
Invoke-ArmyCase 'reject previous 242-high panel' $edMutation $false
$edMutation=Copy-ArmyState $edArmyBase;(Get-ExperienceTarget $edMutation 'hud-army-card-0').h=44
Invoke-ArmyCase 'reject miniature 44-high row' $edMutation $false
$edMutation=Copy-ArmyState $edArmyBase;(Get-ExperienceTarget $edMutation 'hud-army-art-0').w=30;(Get-ExperienceTarget $edMutation 'hud-army-art-0').h=42
Invoke-ArmyCase 'reject rejected-draft miniature portrait' $edMutation $false
$edMutation=Copy-ArmyState $edArmyBase;(Get-ExperienceTarget $edMutation 'hud-army-name-0').x=(Get-ExperienceTarget $edMutation 'hud-army-art-0').x
Invoke-ArmyCase 'reject name over portrait' $edMutation $false
$edMutation=Copy-ArmyState $edArmyBase;(Get-ExperienceTarget $edMutation 'hud-army-card-1').y-=4
Invoke-ArmyCase 'reject collapsed row gap' $edMutation $false
$edMutation=Copy-ArmyState $edArmyBase;$edMutation.targets=@($edMutation.targets | Where-Object {$_.id -ne 'hud-army-name-1'})
Invoke-ArmyCase 'reject missing drawn name' $edMutation $false
$edMutation=Copy-ArmyState $edArmyBase;$edMutation.mapHudArmyUids=@()
Invoke-ArmyCase 'reject invented targets for empty formation' $edMutation $false
$edMutation=New-SyntheticArmyState 1 3 $true $true;(Get-ExperienceTarget $edMutation 'army-battle-watch').y-=5
Invoke-ArmyCase 'reject watch overlapping actual objective return' $edMutation $false
$edMutation=New-SyntheticArmyState 1 3 $true $true;(Get-ExperienceTarget $edMutation 'army-battle-watch').y=(Get-ExperienceTarget $edMutation 'army-training-progress').y
Invoke-ArmyCase 'reject stacked training and battle entries' $edMutation $false
$edMutation=New-SyntheticArmyState 1 3 $true $true;(Get-ExperienceTarget $edMutation 'army-training-progress').h=30
Invoke-ArmyCase 'reject undersized training touch area' $edMutation $false
$edMutation=New-SyntheticArmyState 1 3 $true $true;$edMutation.mapHudSafeX+=100
Invoke-ArmyCase 'reject dynamic entry outside safe area' $edMutation $false
# Exercise the actual pure post-click guard. These are injected observations,
# not real input: every unsafe outcome must still be rejected by publisher code.
$edGapOrigin=[pscustomobject]@{mapHudVisible=$true;mouseBlocked=$true;cardDetail='';selected=-1;battleWorldHash='synthetic-paused-world';focusX=10.0;focusZ=20.0;zoom=7.0}
foreach($edGapField in @('none','mouseBlocked','cardDetail','selected','battleWorldHash','focusX','zoom')) {
    $edGapAfter=Copy-ArmyState $edGapOrigin
    switch($edGapField){'mouseBlocked'{$edGapAfter.mouseBlocked=$false};'cardDetail'{$edGapAfter.cardDetail='s1_100039'};'selected'{$edGapAfter.selected=57};'battleWorldHash'{$edGapAfter.battleWorldHash='mutated'};'focusX'{$edGapAfter.focusX+=1};'zoom'{$edGapAfter.zoom+=1}}
    $edGapFailure=$null;try{Assert-ExperienceMapArmyGap $edGapOrigin $edGapAfter}catch{$edGapFailure=[string]$_}
    if(($null -eq $edGapFailure) -ne ($edGapField -eq 'none')){throw ('Incorrect gap post-click guard for '+$edGapField)}
    $edArmyCases.Add([ordered]@{name='gap observed outcome '+$edGapField;expected=if($edGapField -eq 'none'){'accept'}else{'reject'};passed=$true;rejection=$edGapFailure})
}
$edGap=Get-ExperienceMapArmyGap $edArmyBase
if([Math]::Abs($edGap.x-253) -gt .001 -or [Math]::Abs($edGap.y-534) -gt .001){throw 'Gap pointer was not derived from real row boundaries.'}
$edArmyResult=[ordered]@{status='PASSED';checkedUtc=[DateTime]::UtcNow.ToString('o');caseCount=$edArmyCases.Count;scope='Pure PowerShell assertion regression with explicitly synthetic data; no screenshot, client, desktop or device verification.';cases=@($edArmyCases.ToArray())}
if($OutputDirectory){New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null;[IO.File]::WriteAllText((Join-Path $OutputDirectory 'map-army-experience-regression.json'),($edArmyResult | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))}
Write-Output ('PASS: '+$edArmyCases.Count+' synthetic army row, empty formation, dynamic entry and rejection cases; no desktop interaction.')
