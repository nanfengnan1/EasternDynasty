param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$edPanoramaRoot=Split-Path -Parent $PSScriptRoot
$edPanoramaScript=Join-Path $edPanoramaRoot 'Tools/Check-Experience.ps1'
$edPanoramaTokens=$null;$edPanoramaErrors=$null
$edPanoramaAst=[Management.Automation.Language.Parser]::ParseFile($edPanoramaScript,[ref]$edPanoramaTokens,[ref]$edPanoramaErrors)
if($edPanoramaErrors.Count -gt 0){throw ($edPanoramaErrors | Out-String)}

# Load only pure assertions, never the desktop/process code in Check-Experience.
# This is a regression of recorded geometry, not a new Unity or pointer run.
$edPanoramaFunctions=@('Assert-Experience','Get-ExperienceTarget','Assert-ExperienceTargetInClient','Assert-ExperienceMapStill','Assert-ExperienceCityPage','Assert-ExperienceCityOverview','Assert-ExperienceCityPanoramaExpanded','Assert-ExperienceCityPanoramaRestored','Assert-ExperienceCityFooter','Test-ExperienceLandContains','Test-ExperienceLandOverlap')
foreach($edPanoramaName in $edPanoramaFunctions) {
    $edPanoramaFunction=@($edPanoramaAst.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst]},$true) | Where-Object {$_.Name -eq $edPanoramaName})
    if($edPanoramaFunction.Count -ne 1){throw ('Expected one pure assertion: '+$edPanoramaName)}
    . ([scriptblock]::Create($edPanoramaFunction[0].Extent.Text))
}

# Exact relevant fields from the failed publisher's paired captures. Kept in
# test source so the clean publication snapshot need not copy generated JSON.
$edPanoramaFixture=@'
{"source":{"version":"s1-20261026-141251368","directory":"F:\\EasternDynastyBuild\\Releases\\s1-20261026-141251368\\Project\\Validation\\Visual\\experience-interactive\\20261026-151348-ed9c56a6","compactSha256":"FDCC162F6FF82FC06415DE50AD7A6DA95D757BAE7953EB71A505992648F904F2","expandedSha256":"D34312BCBF6C4388D802002CD090F1CD90CE26F98FA61C5ACC64DCA3D0AEBFD9"},"compact":{"cityOpen":true,"tab":"主城","cityPage":"overview","cityOverviewVisible":true,"cityOverviewStyle":"panorama-side-army-v1","cityExpanded":false,"cityPlanning":false,"cityOverviewScene":[24.0,142.0,1038.0,660.0],"cityOverviewAffairs":[1074.0,142.0,342.0,218.0],"cityOverviewArmy":[1074.0,372.0,342.0,430.0],"cityOverviewArmyUids":[1,2,3],"cityPanoramaViewportWidth":1036.0,"cityPanoramaViewportHeight":570.0,"battleWorldHash":"8937F3622CA35B8CF326DEC600758DF4BFD256DC9168751542352EAFC9CD94B5","focusX":9.0,"focusZ":16.0,"speed":0.0,"targets":[{"id":"city-open","x":194.0,"y":861.0,"w":112.0,"h":40.0},{"id":"city-catalog","x":867.0,"y":112.0,"w":110.0,"h":44.0},{"id":"city-overview","x":747.0,"y":112.0,"w":110.0,"h":44.0},{"id":"city-facilities","x":987.0,"y":112.0,"w":110.0,"h":44.0},{"id":"city-quests","x":1107.0,"y":112.0,"w":110.0,"h":44.0},{"id":"city-training","x":1227.0,"y":112.0,"w":110.0,"h":44.0},{"id":"city-back","x":1354.0,"y":112.0,"w":124.0,"h":44.0},{"id":"city-panorama-expand","x":674.0,"y":172.0,"w":108.0,"h":44.0},{"id":"city-panorama-minus","x":758.0,"y":172.0,"w":44.0,"h":44.0},{"id":"city-panorama-plus","x":808.0,"y":172.0,"w":44.0,"h":44.0},{"id":"city-panorama-reset","x":880.0,"y":172.0,"w":84.0,"h":44.0},{"id":"city-planning","x":991.0,"y":172.0,"w":122.0,"h":44.0},{"id":"city-panorama-viewport","x":543.0,"y":487.0,"w":1036.0,"h":570.0},{"id":"city-building-4","x":543.0,"y":521.4400024414063,"w":154.0,"h":30.0},{"id":"city-overview-facilities","x":1164.5,"y":268.0,"w":153.0,"h":44.0},{"id":"city-construction-page","x":1325.5,"y":268.0,"w":153.0,"h":44.0},{"id":"city-overview-quests","x":1245.0,"y":325.0,"w":314.0,"h":50.0},{"id":"city-general-0","x":1137.6666259765625,"y":560.0,"w":99.33333587646485,"h":200.0},{"id":"city-general-1","x":1245.0,"y":560.0,"w":99.33333587646485,"h":200.0},{"id":"city-general-2","x":1352.333251953125,"y":560.0,"w":99.33333587646485,"h":200.0},{"id":"city-formation","x":1138.3333740234375,"y":774.0,"w":100.66666412353516,"h":44.0},{"id":"city-recruit","x":1245.0,"y":774.0,"w":100.66666412353516,"h":44.0},{"id":"city-army-training","x":1351.666748046875,"y":774.0,"w":100.66666412353516,"h":44.0}]},"expanded":{"cityOpen":true,"tab":"主城","cityPage":"overview","cityOverviewVisible":false,"cityOverviewStyle":"","cityExpanded":true,"cityPlanning":false,"cityOverviewScene":[24.0,142.0,1038.0,660.0],"cityOverviewAffairs":[1074.0,142.0,342.0,218.0],"cityOverviewArmy":[1074.0,372.0,342.0,430.0],"cityOverviewArmyUids":[1,2,3],"cityPanoramaViewportWidth":1390.0,"cityPanoramaViewportHeight":570.0,"battleWorldHash":"8937F3622CA35B8CF326DEC600758DF4BFD256DC9168751542352EAFC9CD94B5","focusX":9.0,"focusZ":16.0,"speed":0.0,"targets":[{"id":"city-open","x":194.0,"y":861.0,"w":112.0,"h":40.0},{"id":"city-catalog","x":867.0,"y":112.0,"w":110.0,"h":44.0},{"id":"city-overview","x":747.0,"y":112.0,"w":110.0,"h":44.0},{"id":"city-facilities","x":987.0,"y":112.0,"w":110.0,"h":44.0},{"id":"city-quests","x":1107.0,"y":112.0,"w":110.0,"h":44.0},{"id":"city-training","x":1227.0,"y":112.0,"w":110.0,"h":44.0},{"id":"city-back","x":1354.0,"y":112.0,"w":124.0,"h":44.0},{"id":"city-panorama-expand","x":1028.0,"y":172.0,"w":108.0,"h":44.0},{"id":"city-panorama-minus","x":1112.0,"y":172.0,"w":44.0,"h":44.0},{"id":"city-panorama-plus","x":1162.0,"y":172.0,"w":44.0,"h":44.0},{"id":"city-panorama-reset","x":1234.0,"y":172.0,"w":84.0,"h":44.0},{"id":"city-planning","x":1345.0,"y":172.0,"w":122.0,"h":44.0},{"id":"city-panorama-viewport","x":720.0,"y":487.0,"w":1390.0,"h":570.0},{"id":"city-building-4","x":720.0,"y":531.8181762695313,"w":154.0,"h":30.0}]}}
'@ | ConvertFrom-Json
$cw=1440.0;$ch=900.0;$PixelWidth=1440;$PixelHeight=900
$edExperienceChecks=[Collections.Generic.List[string]]::new()
$edPanoramaCases=[Collections.Generic.List[object]]::new()
function Copy-PanoramaState($State) { $State | ConvertTo-Json -Depth 12 -Compress | ConvertFrom-Json }
function Invoke-PanoramaCase([string]$Name,$Before,$After,[bool]$ExpectedPass,[bool]$Restored=$false) {
    $failure=$null
    try {
        if($Restored){Assert-ExperienceCityPanoramaRestored $Before $After}
        else {Assert-ExperienceCityPanoramaExpanded $Before $After}
    } catch {$failure=[string]$_}
    if(($null -eq $failure) -ne $ExpectedPass){throw ('PANORAMA_ASSERTION_REGRESSION: '+$Name+' expected pass='+$ExpectedPass+'; '+$failure)}
    $edPanoramaCases.Add([ordered]@{name=$Name;expected=if($ExpectedPass){'accept'}else{'reject'};passed=$true;rejection=$failure})
}
$edBefore=$edPanoramaFixture.compact;$edAfter=$edPanoramaFixture.expanded
Invoke-PanoramaCase 'recorded side-rail expansion grows width with unchanged height' $edBefore $edAfter $true

# Variants below are explicit mutations of that recorded input. They verify
# failure detection and must not be presented as new captures or click evidence.
$edMutation=Copy-PanoramaState $edAfter
$edMutation.cityExpanded=$false
Invoke-PanoramaCase 'reject expansion flag absent' $edBefore $edMutation $false
$edMutation=Copy-PanoramaState $edAfter
$edMutation.cityPage='facilities'
Invoke-PanoramaCase 'reject navigation into another city page' $edBefore $edMutation $false
$edMutation=Copy-PanoramaState $edAfter
$edMutation.battleWorldHash='changed-by-charge'
Invoke-PanoramaCase 'reject resource or other campaign mutation' $edBefore $edMutation $false
$edMutation=Copy-PanoramaState $edAfter
$edMutation.speed=1
Invoke-PanoramaCase 'reject an unpaused campaign comparison' $edBefore $edMutation $false
$edMutation=Copy-PanoramaState $edAfter
$edMutation.cityPanoramaViewportWidth=$edBefore.cityPanoramaViewportWidth
$edMutation.cityPanoramaViewportHeight=$edBefore.cityPanoramaViewportHeight
$edView=Get-ExperienceTarget $edMutation 'city-panorama-viewport'
$edOriginal=Get-ExperienceTarget $edBefore 'city-panorama-viewport'
$edView.x=$edOriginal.x;$edView.y=$edOriginal.y;$edView.w=$edOriginal.w;$edView.h=$edOriginal.h
Invoke-PanoramaCase 'reject unchanged viewport behind expanded flag' $edBefore $edMutation $false
$edMutation=Copy-PanoramaState $edAfter
$edView=Get-ExperienceTarget $edMutation 'city-panorama-viewport'
$edView.x=$edOriginal.x;$edView.y=$edOriginal.y;$edView.w=$edOriginal.w;$edView.h=$edOriginal.h
Invoke-PanoramaCase 'reject enlarged diagnostics with unchanged visible target' $edBefore $edMutation $false
$edMutation=Copy-PanoramaState $edAfter
$edMutation.cityPanoramaViewportWidth+=200
Invoke-PanoramaCase 'reject inconsistent logical and visible growth ratios' $edBefore $edMutation $false
$edMutation=Copy-PanoramaState $edAfter
$edView=Get-ExperienceTarget $edMutation 'city-panorama-viewport'
$edView.w=1400;$edView.h=500;$edView.x=725
$edMutation.cityPanoramaViewportWidth=1400;$edMutation.cityPanoramaViewportHeight=500
Invoke-PanoramaCase 'reject larger area that clips prior visible height' $edBefore $edMutation $false
$edMutation=Copy-PanoramaState $edAfter
$edView=Get-ExperienceTarget $edMutation 'city-panorama-viewport'
$edView.x+=50
Invoke-PanoramaCase 'reject viewport shifted off the prior left edge' $edBefore $edMutation $false
$edMutation=Copy-PanoramaState $edAfter
$edMutation.targets+=Copy-PanoramaState (Get-ExperienceTarget $edBefore 'city-army-training')
Invoke-PanoramaCase 'reject invisible side-rail training target' $edBefore $edMutation $false
$edMutation=Copy-PanoramaState $edAfter
$edMutation.targets=@($edMutation.targets | Where-Object {$_.id -ne 'city-back'})
Invoke-PanoramaCase 'reject missing return target' $edBefore $edMutation $false
$edMutation=Copy-PanoramaState $edAfter
$edView=Get-ExperienceTarget $edMutation 'city-panorama-plus';$edView.y=1
Invoke-PanoramaCase 'reject clipped zoom control' $edBefore $edMutation $false
$edMutation=Copy-PanoramaState $edAfter
$edMutation.focusX+=1
Invoke-PanoramaCase 'reject underlying map motion during expansion' $edBefore $edMutation $false

$edMutation=Copy-PanoramaState $edAfter
$edView=Get-ExperienceTarget $edMutation 'city-panorama-viewport'
$edView.x=$edOriginal.x;$edView.y=$edOriginal.y+35;$edView.w=$edOriginal.w;$edView.h=$edOriginal.h+70
$edMutation.cityPanoramaViewportWidth=$edBefore.cityPanoramaViewportWidth
$edMutation.cityPanoramaViewportHeight=$edBefore.cityPanoramaViewportHeight+70
Invoke-PanoramaCase 'accept a contained vertical expansion without assuming a growth axis' $edBefore $edMutation $true

$edScaledBefore=Copy-PanoramaState $edBefore;$edScaledAfter=Copy-PanoramaState $edAfter
foreach($edState in @($edScaledBefore,$edScaledAfter)) {
    foreach($edTarget in $edState.targets){$edTarget.x*=.75;$edTarget.y*=.75;$edTarget.w*=.75;$edTarget.h*=.75}
}
$cw=1080.0;$ch=675.0;$PixelWidth=1080;$PixelHeight=675
Invoke-PanoramaCase 'accept equivalent client-pixel scaling without confusing logical height' $edScaledBefore $edScaledAfter $true
$cw=1440.0;$ch=900.0;$PixelWidth=1440;$PixelHeight=900

Invoke-PanoramaCase 'synthetic return restores the recorded compact geometry' $edBefore (Copy-PanoramaState $edBefore) $true $true
$edMutation=Copy-PanoramaState $edBefore
$edView=Get-ExperienceTarget $edMutation 'city-panorama-viewport';$edView.w-=10
Invoke-PanoramaCase 'reject collapsed viewport with changed bounds' $edBefore $edMutation $false $true
$edMutation=Copy-PanoramaState $edBefore
$edMutation.targets=@($edMutation.targets | Where-Object {$_.id -ne 'city-army-training'})
Invoke-PanoramaCase 'reject collapsed city without its real training control' $edBefore $edMutation $false $true
$edMutation=Copy-PanoramaState $edBefore
$edMutation.battleWorldHash='changed-by-return'
Invoke-PanoramaCase 'reject campaign change on return' $edBefore $edMutation $false $true

# The new city-army design intentionally ends above the scene bottom. Preserve
# every recorded fixture above; this is a labelled synthetic layout adaptation.
function Invoke-CompactArmyCase([string]$Name,$State,[bool]$ExpectedPass) {
    $failure=$null
    try {Assert-ExperienceCityOverview $State} catch {$failure=[string]$_}
    if(($null -eq $failure) -ne $ExpectedPass){throw ('CITY_ARMY_ASSERTION_REGRESSION: '+$Name+' expected pass='+$ExpectedPass+'; '+$failure)}
    $edPanoramaCases.Add([ordered]@{name=$Name;expected=if($ExpectedPass){'accept'}else{'reject'};passed=$true;rejection=$failure})
}
$edCompactArmy=Copy-PanoramaState $edBefore
$edCompactArmy | Add-Member -NotePropertyName cityArmyDesignStyle -NotePropertyValue 'youzhou-city-army-tickets-v1'
$edCompactArmy | Add-Member -NotePropertyName mapHudTouchMinimum -NotePropertyValue 44
$edCompactArmy | Add-Member -NotePropertyName cityContentCanvas -NotePropertyValue @($edBefore.cityOverviewScene[0],$edBefore.cityOverviewScene[1],($edBefore.cityOverviewAffairs[0]+$edBefore.cityOverviewAffairs[2]-$edBefore.cityOverviewScene[0]),$edBefore.cityOverviewScene[3])
$edArmyWidth=[double]$edBefore.cityOverviewArmy[2]
$edArmyAvailable=[double]$edBefore.cityOverviewArmy[3]
$edArmyCardWidth=($edArmyWidth-32)/3
$edArmyCardHeight=[Math]::Min($edArmyAvailable-190,($edArmyCardWidth-8)*1.45+48)
$edCompactArmy.cityOverviewArmy[3]=$edArmyCardHeight+190
for($edArmyIndex=0;$edArmyIndex -lt 3;$edArmyIndex++) {
    $edCard=Get-ExperienceTarget $edCompactArmy ('city-general-'+$edArmyIndex)
    $edCard.w=$edArmyCardWidth;$edCard.h=$edArmyCardHeight
    $edCard.x=$edCompactArmy.cityOverviewArmy[0]+8+$edArmyIndex*($edArmyCardWidth+8)+$edArmyCardWidth/2
    $edCard.y=$edCompactArmy.cityOverviewArmy[1]+52+$edArmyCardHeight/2
    $edAction=Get-ExperienceTarget $edCompactArmy @('city-formation','city-recruit','city-army-training')[$edArmyIndex]
    $edAction.w=($edArmyWidth-28)/3;$edAction.h=44
    $edAction.x=$edCompactArmy.cityOverviewArmy[0]+8+$edArmyIndex*($edAction.w+6)+$edAction.w/2
    $edAction.y=$edCompactArmy.cityOverviewArmy[1]+$edArmyCardHeight+140+22
}
Invoke-CompactArmyCase 'synthetic compact design keeps all three full cards and commands above the scene bottom' $edCompactArmy $true
$edArmyMutation=Copy-PanoramaState $edCompactArmy
$edArmyMutation.cityOverviewArmy[3]=$edArmyAvailable
Invoke-CompactArmyCase 'reject new design retaining the old full-height army mat' $edArmyMutation $false
$edArmyMutation=Copy-PanoramaState $edCompactArmy
$edArmyMutation.cityOverviewArmy[3]-=60
Invoke-CompactArmyCase 'reject compact rail shortened through its card and command content' $edArmyMutation $false
$edArmyMutation=Copy-PanoramaState $edCompactArmy
$edCard=Get-ExperienceTarget $edArmyMutation 'city-general-0'
$edCard.h=40
Invoke-CompactArmyCase 'reject squeezing a complete vertical portrait into a short banner' $edArmyMutation $false
$edArmyMutation=Copy-PanoramaState $edCompactArmy
$edAction=Get-ExperienceTarget $edArmyMutation 'city-army-training'
$edAction.y+=80
Invoke-CompactArmyCase 'reject command extending below the compact army area' $edArmyMutation $false
$edArmyMutation=Copy-PanoramaState $edCompactArmy
$edArmyMutation.cityOverviewArmy[1]+=90
Invoke-CompactArmyCase 'reject compact army extending below the city content' $edArmyMutation $false
$edArmyMutation=Copy-PanoramaState $edCompactArmy
$edArmyMutation.cityOverviewScene[3]-=20
Invoke-CompactArmyCase 'reject shortening the panorama while compacting only the army' $edArmyMutation $false

# Independent synthetic records for the newly inspected footer. These are
# design-contract inputs, never relabelled historical captures or runtime proof.
function New-SyntheticCityFooter([double]$Width,[double]$Height,[double]$Unit,[double]$SafeX=0,[double]$SafeY=0) {
    $targets=[Collections.Generic.List[object]]::new()
    function Add-FooterRegion([string]$Id,[double]$X,[double]$Y,[double]$W,[double]$H) {
        $targets.Add([pscustomobject]@{id=$Id;x=$SafeX+($X+$W/2)*$Unit;y=$SafeY+($Y+$H/2)*$Unit;w=$W*$Unit;h=$H*$Unit})
    }
    $ids=@('city-open','map-tab','roster-tab','reports-tab','hud-help','recruit-tab')
    for($i=0;$i -lt 6;$i++) {
        $x=20+116*$i;$y=$Height-74
        if($i -lt 5) {
            Add-FooterRegion $ids[$i] $x $y 108 70
            Add-FooterRegion ($ids[$i]+'-art') ($x+4) ($y+3) 100 42
            Add-FooterRegion ($ids[$i]+'-caption') ($x+4) ($y+46) 100 22
        } else {
            $x=$Width-152
            Add-FooterRegion $ids[$i] $x $y 132 70
            Add-FooterRegion ($ids[$i]+'-art') ($x+46) ($y+7) 40 38
            Add-FooterRegion ($ids[$i]+'-caption') ($x+8) ($y+47) 116 22
        }
    }
    $utilityX=[Math]::Max(620,($Width-270)/2)
    Add-FooterRegion 'hud-save' $utilityX ($Height-61) 82 49
    Add-FooterRegion 'speed-cycle' ($utilityX+90) ($Height-61) 180 49
    return [pscustomobject]@{tab='主城';cityFooterDesignStyle='youzhou-reference-image-captions-v2';cityFooterDesignTextFits=$true;mapHudTouchMinimum=44*$Unit;mapHudSafeX=$SafeX;mapHudSafeY=$SafeY;mapHudSafeWidth=$Width*$Unit;mapHudSafeHeight=$Height*$Unit;targets=@($targets.ToArray())}
}
function Invoke-CityFooterCase([string]$Name,$State,[bool]$ExpectedPass) {
    $failure=$null
    try {Assert-ExperienceCityFooter $State} catch {$failure=[string]$_}
    if(($null -eq $failure) -ne $ExpectedPass){throw ('CITY_FOOTER_ASSERTION_REGRESSION: '+$Name+' expected pass='+$ExpectedPass+'; '+$failure)}
    $edPanoramaCases.Add([ordered]@{name=$Name;expected=if($ExpectedPass){'accept'}else{'reject'};passed=$true;rejection=$failure})
}
$edFooter=New-SyntheticCityFooter 1440 900 1
Invoke-CityFooterCase 'synthetic city footer has all eight independent designed controls and image/title regions' $edFooter $true
$cw=1600;$ch=720;$PixelWidth=1600;$PixelHeight=720
$edMobileFooter=New-SyntheticCityFooter 1940 860 .8 24 16
Invoke-CityFooterCase 'synthetic mobile footer applies its positive scale and inset safe area' $edMobileFooter $true
$cw=1440;$ch=900;$PixelWidth=1440;$PixelHeight=900
$edFooterMutation=Copy-PanoramaState $edFooter
(Get-ExperienceTarget $edFooterMutation 'hud-save').x=(Get-ExperienceTarget $edFooterMutation 'city-open').x
Invoke-CityFooterCase 'reject utility overlapping a non-adjacent navigation ticket' $edFooterMutation $false
$edFooterMutation=Copy-PanoramaState $edFooter
(Get-ExperienceTarget $edFooterMutation 'speed-cycle').x=(Get-ExperienceTarget $edFooterMutation 'hud-save').x
Invoke-CityFooterCase 'reject two overlapping footer utilities' $edFooterMutation $false
$edFooterMutation=Copy-PanoramaState $edFooter
(Get-ExperienceTarget $edFooterMutation 'reports-tab').x+=1
Invoke-CityFooterCase 'reject plausible but incorrect navigation position against exported draft' $edFooterMutation $false
$edFooterMutation=Copy-PanoramaState $edFooter
(Get-ExperienceTarget $edFooterMutation 'city-open-caption').y=(Get-ExperienceTarget $edFooterMutation 'city-open-art').y
Invoke-CityFooterCase 'reject readable title overlapping its actual image' $edFooterMutation $false
$edFooterMutation=Copy-PanoramaState $edFooter
$edFooterMutation.cityFooterDesignStyle='youzhou-reference-tickets-v1'
Invoke-CityFooterCase 'reject obsolete vertical-ticket style identifier' $edFooterMutation $false
$edFooterMutation=Copy-PanoramaState $edFooter
$edOldCaption=Get-ExperienceTarget $edFooterMutation 'city-open-caption'
$edOldCaption.x=37.5;$edOldCaption.y=855;$edOldCaption.w=25;$edOldCaption.h=50
Invoke-CityFooterCase 'reject old vertical title geometry under the new style identifier' $edFooterMutation $false
$edFooterMutation=Copy-PanoramaState $edFooter
(Get-ExperienceTarget $edFooterMutation 'recruit-tab-art').y+=18
Invoke-CityFooterCase 'reject recruitment portrait overlapping its horizontal caption' $edFooterMutation $false
$edFooterMutation=Copy-PanoramaState $edFooter
(Get-ExperienceTarget $edFooterMutation 'roster-tab-art').w=84
Invoke-CityFooterCase 'reject historical 84-unit image width as current city ticket geometry' $edFooterMutation $false
$edFooterMutation=Copy-PanoramaState $edFooter
$edFooterMutation.targets=@($edFooterMutation.targets | Where-Object {$_.id -ne 'hud-help-caption'})
Invoke-CityFooterCase 'reject absent visible help caption target' $edFooterMutation $false
$edFooterMutation=Copy-PanoramaState $edFooter
$edFooterMutation.cityFooterDesignTextFits=$false
Invoke-CityFooterCase 'reject actual Chinese text that cannot fit the designed region' $edFooterMutation $false
$edFooterMutation=Copy-PanoramaState $edFooter
$edFooterMutation.tab='领地'
Invoke-CityFooterCase 'reject a stale city footer diagnostic on another page' $edFooterMutation $false

$edPanoramaResult=[ordered]@{
    status='PASSED';checkedUtc=[DateTime]::UtcNow.ToString('o');caseCount=$edPanoramaCases.Count
    scope='Pure PowerShell assertion regression. One recorded expansion pair; other cases are labelled synthetic mutations. No new client, screenshot or pointer execution.'
    fixtureSource=$edPanoramaFixture.source
    oldHeightAssertionPass=($edAfter.cityPanoramaViewportHeight -gt $edBefore.cityPanoramaViewportHeight)
    compactViewport=@($edBefore.cityPanoramaViewportWidth,$edBefore.cityPanoramaViewportHeight)
    expandedViewport=@($edAfter.cityPanoramaViewportWidth,$edAfter.cityPanoramaViewportHeight)
    viewportAreaRatio=([double]$edAfter.cityPanoramaViewportWidth*$edAfter.cityPanoramaViewportHeight/([double]$edBefore.cityPanoramaViewportWidth*$edBefore.cityPanoramaViewportHeight))
    worldHashUnchanged=($edBefore.battleWorldHash -eq $edAfter.battleWorldHash)
    cases=@($edPanoramaCases.ToArray())
}
if($OutputDirectory) {
    $edPanoramaOutput=[IO.Path]::GetFullPath($OutputDirectory)
    New-Item -ItemType Directory -Path $edPanoramaOutput -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $edPanoramaOutput 'publication-panorama-regression.json'),($edPanoramaResult | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
}
Write-Output ('PASS: '+$edPanoramaCases.Count+' city expansion/return/footer assertion regressions; recorded viewport 1036x570 -> 1390x570 and explicitly synthetic current-footer cases; no desktop interaction.')
