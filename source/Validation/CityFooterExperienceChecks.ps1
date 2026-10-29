param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$edFooterRoot=Split-Path -Parent $PSScriptRoot
$edFooterTokens=$null;$edFooterErrors=$null
$edFooterAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $edFooterRoot 'Tools/Check-Experience.ps1'),[ref]$edFooterTokens,[ref]$edFooterErrors)
if($edFooterErrors.Count -gt 0){throw ($edFooterErrors | Out-String)}
# Import only pure assertions. No client, desktop, Unity or new capture is run.
foreach($edFooterName in @('Assert-Experience','Get-ExperienceTarget','Assert-ExperienceTargetInClient','Assert-ExperienceCityFooter','Test-ExperienceLandContains','Test-ExperienceLandOverlap')) {
    $edFooterFunction=@($edFooterAst.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst]},$true) | Where-Object {$_.Name -eq $edFooterName})
    if($edFooterFunction.Count -ne 1){throw ('Expected one pure assertion: '+$edFooterName)}
    . ([scriptblock]::Create($edFooterFunction[0].Extent.Text))
}
# Exact relevant fields from the publisher capture which triggered the 110 failure.
# Frozen here so a clean snapshot can run without generated Validation JSON.
$edFooterFixture=@'
{"source":{"path":"F:/EasternDynastyBuild/Releases/s1-20261028-110251322/Project/Validation/Visual/experience-interactive/20261028-122503-037439ca/04-home-click.json","sha256":"ab704462564a94bc99701e79ec1d51c6d4d53406aa5f1b0391f450b48d300f21"},"state":{"tab":"主城","cityFooterDesignStyle":"youzhou-reference-tickets-v1","cityFooterDesignTextFits":true,"mapHudTouchMinimum":44,"mapHudSafeX":0,"mapHudSafeY":0,"mapHudSafeWidth":1440,"mapHudSafeHeight":900,"targets":[{"id":"hud-save","x":661,"y":863.5,"w":82,"h":49},{"id":"city-open","x":74,"y":861,"w":108,"h":70},{"id":"city-open-art","x":89,"y":862,"w":70,"h":56},{"id":"city-open-caption","x":37.5,"y":855,"w":25,"h":50},{"id":"map-tab","x":190,"y":861,"w":108,"h":70},{"id":"map-tab-art","x":205,"y":862,"w":70,"h":56},{"id":"map-tab-caption","x":153.5,"y":855,"w":25,"h":50},{"id":"roster-tab","x":306,"y":861,"w":108,"h":70},{"id":"roster-tab-art","x":321,"y":862,"w":70,"h":56},{"id":"roster-tab-caption","x":269.5,"y":855,"w":25,"h":50},{"id":"reports-tab","x":422,"y":861,"w":108,"h":70},{"id":"reports-tab-art","x":437,"y":862,"w":70,"h":56},{"id":"reports-tab-caption","x":385.5,"y":855,"w":25,"h":50},{"id":"hud-help","x":538,"y":861,"w":108,"h":70},{"id":"hud-help-art","x":553,"y":862,"w":70,"h":56},{"id":"hud-help-caption","x":501.5,"y":855,"w":25,"h":50},{"id":"recruit-tab","x":1354,"y":861,"w":132,"h":70},{"id":"recruit-tab-art","x":1354,"y":852,"w":40,"h":38},{"id":"recruit-tab-caption","x":1354,"y":884,"w":116,"h":22},{"id":"speed-cycle","x":800,"y":863.5,"w":180,"h":49}]}}
'@ | ConvertFrom-Json
$cw=1440.0;$ch=900.0;$PixelWidth=1440;$PixelHeight=900
$edExperienceChecks=[Collections.Generic.List[string]]::new()
$edFooterCases=[Collections.Generic.List[object]]::new()
function Copy-FooterState($State){$State | ConvertTo-Json -Depth 8 -Compress | ConvertFrom-Json}
function New-CaptionFooterState {
    # The historical capture remains untouched provenance. These new rectangles
    # are a synthetic design fixture, not a claim that a new client ran.
    $state=Copy-FooterState $edFooterFixture.state
    $state.cityFooterDesignStyle='youzhou-reference-image-captions-v2'
    foreach($id in @('city-open','map-tab','roster-tab','reports-tab','hud-help')) {
        $parent=Get-ExperienceTarget $state $id
        $image=Get-ExperienceTarget $state ($id+'-art');$caption=Get-ExperienceTarget $state ($id+'-caption')
        $left=$parent.x-$parent.w/2;$top=$parent.y-$parent.h/2
        $image.x=$left+54;$image.y=$top+24;$image.w=100;$image.h=42
        $caption.x=$left+54;$caption.y=$top+57;$caption.w=100;$caption.h=22
    }
    return $state
}
function Invoke-FooterCase([string]$Name,$State,[bool]$ExpectedPass,[string]$ExpectedMessage='') {
    $failure=$null
    try{Assert-ExperienceCityFooter $State}catch{$failure=[string]$_}
    if(($null -eq $failure) -ne $ExpectedPass -or ($ExpectedMessage -and $failure -notlike ('*'+$ExpectedMessage+'*'))){throw ('CITY_FOOTER_REGRESSION: '+$Name+' expected pass='+$ExpectedPass+'; '+$failure)}
    $edFooterCases.Add([ordered]@{name=$Name;expected=if($ExpectedPass){'accept'}else{'reject'};passed=$true;rejection=$failure})
}
$edFooterCurrent=New-CaptionFooterState
Invoke-FooterCase 'reject prior publisher vertical-ticket style' $edFooterFixture.state $false 'image-above-caption design'
$edMutation=Copy-FooterState $edFooterFixture.state
$edMutation.cityFooterDesignStyle='youzhou-reference-image-captions-v2'
Invoke-FooterCase 'reject old vertical geometry despite new style text' $edMutation $false 'image-above-horizontal-caption hierarchy'
Invoke-FooterCase 'accept explicit horizontal-caption design fixture with eight-unit utility gap' $edFooterCurrent $true
$edMutation=Copy-FooterState $edFooterCurrent
(Get-ExperienceTarget $edMutation 'city-open-caption').w=25
Invoke-FooterCase 'reject narrow obsolete caption even when below image' $edMutation $false 'city-open-caption exactly matches'
$edMutation=Copy-FooterState $edFooterCurrent
(Get-ExperienceTarget $edMutation 'map-tab-art').w=70
Invoke-FooterCase 'reject old narrow image inside the current image row' $edMutation $false 'map-tab-art exactly matches'
$edMutation=Copy-FooterState $edFooterCurrent
(Get-ExperienceTarget $edMutation 'reports-tab-caption').y=850
Invoke-FooterCase 'reject caption overlapping illustration' $edMutation $false
$edMutation=Copy-FooterState $edFooterCurrent
$edMutation.cityFooterDesignTextFits=$false
Invoke-FooterCase 'reject actual Chinese caption font overflow' $edMutation $false 'horizontal Chinese labels'
$edMutation=Copy-FooterState $edFooterCurrent
(Get-ExperienceTarget $edMutation 'speed-cycle').x-=8
Invoke-FooterCase 'reject obsolete contiguous save/time layout' $edMutation $false 'speed-cycle exactly matches'
$edMutation=Copy-FooterState $edFooterCurrent
(Get-ExperienceTarget $edMutation 'speed-cycle').x+=1
Invoke-FooterCase 'reject one pixel time-control drift without weakening tolerance' $edMutation $false 'speed-cycle exactly matches'
$edMutation=Copy-FooterState $edFooterCurrent
(Get-ExperienceTarget $edMutation 'hud-save').w=81
Invoke-FooterCase 'reject incorrect actual save width' $edMutation $false 'hud-save exactly matches'
$edMutation=Copy-FooterState $edFooterCurrent
$edMutation.targets=@($edMutation.targets | Where-Object {$_.id -ne 'speed-cycle'})
Invoke-FooterCase 'reject missing visible time control' $edMutation $false 'Missing/ambiguous current visible target speed-cycle'
$edMutation=Copy-FooterState $edFooterCurrent
$edMutation.targets+=Copy-FooterState (Get-ExperienceTarget $edMutation 'speed-cycle')
Invoke-FooterCase 'reject duplicate visible time control' $edMutation $false 'Missing/ambiguous current visible target speed-cycle'
$edMutation=Copy-FooterState $edFooterCurrent
(Get-ExperienceTarget $edMutation 'speed-cycle').x=1450
Invoke-FooterCase 'reject time control outside visible client' $edMutation $false
$edFooterResult=[ordered]@{status='PASSED';checkedUtc=[DateTime]::UtcNow.ToString('o');caseCount=$edFooterCases.Count;scope='Pure rejection of prior publisher vertical-ticket capture plus explicit synthetic horizontal-caption design fixture and mutations; no new runtime, screenshot or click evidence.';fixtureSource=$edFooterFixture.source;cases=@($edFooterCases.ToArray())}
if($OutputDirectory){New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null;[IO.File]::WriteAllText((Join-Path $OutputDirectory 'publication-city-footer-regression.json'),($edFooterResult | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))}
Write-Output ('PASS: '+$edFooterCases.Count+' city footer assertion regressions; prior publisher rejection, synthetic design geometry and explicit rejection variants; no desktop interaction.')
