param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$edRecruitRoot=Split-Path -Parent $PSScriptRoot
$edTokens=$null;$edErrors=$null
$edAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $edRecruitRoot 'Tools/Check-Experience.ps1'),[ref]$edTokens,[ref]$edErrors)
if($edErrors.Count){throw ($edErrors|Out-String)}
# Pure assertion import. Synthetic geometry is not a client screenshot or input run.
foreach($name in @('Assert-Experience','Get-ExperienceTarget','Assert-ExperienceMapRecruit','Get-ExperienceMapRecruitRaisedPoint','Assert-ExperienceMapRecruitDrag','Assert-ExperienceMapStill','Assert-ExperienceMapCommandModal','Test-ExperienceLandContains','Test-ExperienceLandOverlap')) {
    $fn=@($edAst.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst]},$true)|Where-Object Name -eq $name)
    if($fn.Count -ne 1){throw ('Missing recruitment assertion: '+$name)}
    . ([scriptblock]::Create($fn[0].Extent.Text))
}
$edExperienceChecks=[Collections.Generic.List[string]]::new()
$edCases=[Collections.Generic.List[object]]::new()
function Copy-RecruitFixture($State){$State|ConvertTo-Json -Depth 8 -Compress|ConvertFrom-Json}
function New-RecruitFixture([double]$Unit=1,[double]$Width=1440,[bool]$Selected=$false) {
    $height=820.0;$rows=@()
    if($Selected) {
        $rows+=@(@('recruit-tab',($Width-152),($height-74),132,70),@('recruit-tab-art',($Width-106),($height-67),40,38),@('recruit-tab-caption',($Width-144),($height-27),116,22))
    } else {
        $rows+=@(@('recruit-tab',($Width-180),($height-144),160,140),@('recruit-tab-art',($Width-130),($height-134),60,88),@('recruit-tab-caption',($Width-170),($height-30),140,24))
    }
    $ids=@('city-open','map-tab','roster-tab','reports-tab','hud-help')
    for($i=0;$i -lt 5;$i++){$rows+=,@($ids[$i],(20+116*$i),($height-74),108,70)}
    $utilityX=[Math]::Max(620,($Width-270)/2)
    $rows+=@(@('hud-save',$utilityX,($height-61),82,49),@('speed-cycle',($utilityX+90),($height-61),180,49))
    $targets=@($rows|ForEach-Object {[pscustomobject]@{id=$_[0];x=56+($_[1]+$_[3]*.5)*$Unit;y=12+($_[2]+$_[4]*.5)*$Unit;w=$_[3]*$Unit;h=$_[4]*$Unit}})
    return [pscustomobject]@{tab='领地';mapHudVisible=$true;mapRecruitStyle=$(if($Selected){''}else{'youzhou-recruit-pedestal-v3'});mapRecruitTextFits=(-not $Selected);selected=$(if($Selected){57}else{-1});mapHudTouchMinimum=44*$Unit;mapHudSafeX=56.0;mapHudSafeY=12.0;mapHudSafeWidth=$Width*$Unit;mapHudSafeHeight=$height*$Unit;targets=$targets;mouseBlocked=$true;cardDetail='';battleWorldHash='synthetic-paused-campaign';focusX=15.0;focusZ=12.0;zoom=7.0}
}
function Invoke-RecruitCase([string]$Name,$State,[bool]$ExpectedPass) {
    $failure=$null;try{Assert-ExperienceMapRecruit $State}catch{$failure=[string]$_}
    if(($null -eq $failure) -ne $ExpectedPass){throw ('RECRUIT_ASSERTION_REGRESSION: '+$Name+' expected pass='+$ExpectedPass+'; '+$failure)}
    $edCases.Add([ordered]@{name=$Name;expected=if($ExpectedPass){'accept'}else{'reject'};passed=$true;rejection=$failure})
}
foreach($unit in @(1,.8,(684.0/820))) {foreach($width in @(1180,1440,1920)) {foreach($selected in @($false,$true)) {
    $fixture=New-RecruitFixture $unit $width $selected
    Invoke-RecruitCase ('safe-area scale '+$unit+' width '+$width+' selected '+$selected) $fixture $true
    if(-not $selected) {
        $point=Get-ExperienceMapRecruitRaisedPoint $fixture
        if([Math]::Abs($point.x-(56+($width-100)*$unit)) -gt .001 -or [Math]::Abs($point.y-(12+712*$unit)) -gt .001){throw 'Upper portrait pointer was not derived from the actual visible image.'}
    }
}}}
$baseline=New-RecruitFixture
foreach($mutation in @('old-compact','portrait-too-small','title-over-image','missing-portrait','font-overflow','stale-style','safe-area-clip','utility-overlap')) {
    $state=Copy-RecruitFixture $baseline
    switch($mutation) {
        'old-compact' {$old=New-RecruitFixture 1 1440 $true;$state.targets=$old.targets}
        'portrait-too-small' {(Get-ExperienceTarget $state 'recruit-tab-art').h=38}
        'title-over-image' {(Get-ExperienceTarget $state 'recruit-tab-caption').y=(Get-ExperienceTarget $state 'recruit-tab-art').y}
        'missing-portrait' {$state.targets=@($state.targets|Where-Object id -ne 'recruit-tab-art')}
        'font-overflow' {$state.mapRecruitTextFits=$false}
        'stale-style' {$state.mapRecruitStyle='youzhou-reference-image-captions-v2'}
        'safe-area-clip' {$state.mapHudSafeWidth-=100}
        'utility-overlap' {(Get-ExperienceTarget $state 'speed-cycle').x=(Get-ExperienceTarget $state 'recruit-tab').x}
    }
    Invoke-RecruitCase $mutation $state $false
}
$selected=New-RecruitFixture 1 1440 $true
$selected.mapRecruitStyle='youzhou-recruit-pedestal-v3'
Invoke-RecruitCase 'reject raised seal still active behind selected-land commands' $selected $false
$selected=New-RecruitFixture 1 1440 $true
$selected.targets=$baseline.targets
Invoke-RecruitCase 'reject raised geometry behind selected-land commands even if style cleared' $selected $false
foreach($field in @('none','tab','mapHudVisible','mouseBlocked','cardDetail','selected','battleWorldHash','focusX','zoom')) {
    $after=Copy-RecruitFixture $baseline
    switch($field) {
        'tab' {$after.tab='招募'}
        'mapHudVisible' {$after.mapHudVisible=$false}
        'mouseBlocked' {$after.mouseBlocked=$false}
        'cardDetail' {$after.cardDetail='s1_100039'}
        'selected' {$after.selected=57}
        'battleWorldHash' {$after.battleWorldHash='mutated'}
        'focusX' {$after.focusX+=1}
        'zoom' {$after.zoom+=1}
    }
    $failure=$null;try{Assert-ExperienceMapRecruitDrag $baseline $after}catch{$failure=[string]$_}
    if(($null -eq $failure) -ne ($field -eq 'none')){throw ('Incorrect raised recruitment drag outcome guard: '+$field)}
    $edCases.Add([ordered]@{name='observed drag outcome '+$field;expected=if($field -eq 'none'){'accept'}else{'reject'};passed=$true;rejection=$failure})
}
foreach($field in @('none','style','font','hidden-target')) {
    $modal=[pscustomobject]@{mapHudVisible=$false;mapRecruitStyle='';mapRecruitTextFits=$false;minimapDesignStyle='';targets=@()}
    switch($field) {
        'style' {$modal.mapRecruitStyle='youzhou-recruit-pedestal-v3'}
        'font' {$modal.mapRecruitTextFits=$true}
        'hidden-target' {$modal.targets=@([pscustomobject]@{id='recruit-tab-art';x=1300;y=720;w=60;h=88})}
    }
    $failure=$null;try{Assert-ExperienceMapCommandModal $modal}catch{$failure=[string]$_}
    if(($null -eq $failure) -ne ($field -eq 'none')){throw ('Incorrect modal recruitment isolation guard: '+$field)}
    $edCases.Add([ordered]@{name='modal recruitment isolation '+$field;expected=if($field -eq 'none'){'accept'}else{'reject'};passed=$true;rejection=$failure})
}
$result=[ordered]@{status='PASSED';checkedUtc=[DateTime]::UtcNow.ToString('o');caseCount=$edCases.Count;scope='Pure PowerShell assertions using explicit synthetic geometry and outcomes; no Unity, client, desktop, screenshot or device verification.';cases=@($edCases.ToArray())}
if($OutputDirectory){New-Item -ItemType Directory -Path $OutputDirectory -Force|Out-Null;[IO.File]::WriteAllText((Join-Path $OutputDirectory 'map-recruit-experience-regression.json'),($result|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))}
Write-Output ('PASS: '+$edCases.Count+' map-only recruitment geometry, selected-land compatibility and drag outcome guards; actual input remains publisher work.')
