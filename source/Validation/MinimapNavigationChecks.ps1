param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$edMinimapRoot=Split-Path -Parent $PSScriptRoot
$edMinimapScript=Join-Path $edMinimapRoot 'Tools/Check-Experience.ps1'
$edMinimapTokens=$null;$edMinimapErrors=$null
$edMinimapAst=[Management.Automation.Language.Parser]::ParseFile($edMinimapScript,[ref]$edMinimapTokens,[ref]$edMinimapErrors)
if($edMinimapErrors.Count -gt 0){throw ($edMinimapErrors | Out-String)}
# Import pure assertions only. Never execute process, P/Invoke or desktop code.
foreach($edMinimapName in @('Assert-Experience','Get-ExperienceTarget','Assert-ExperienceTargetInClient','Assert-ExperienceMinimap','Assert-ExperienceMinimapDesign','Assert-ExperienceMinimapFocus','Assert-ExperienceMinimapNavigation')) {
    $edMinimapFunction=@($edMinimapAst.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst]},$true) | Where-Object Name -eq $edMinimapName)
    if($edMinimapFunction.Count -ne 1){throw ('Expected one pure minimap function '+$edMinimapName)}
    . ([scriptblock]::Create($edMinimapFunction[0].Extent.Text))
}
$cw=1440.0;$ch=900.0;$PixelWidth=1440;$PixelHeight=900
$edExperienceChecks=[Collections.Generic.List[string]]::new()
$edMinimapCases=[Collections.Generic.List[object]]::new()
function Copy-MinimapFixture($Value) {$Value | ConvertTo-Json -Depth 12 -Compress | ConvertFrom-Json}
function Invoke-MinimapCase([string]$Name,[scriptblock]$Action,[string]$ExpectedRejection='') {
    $failure=$null
    try {$null=& $Action} catch {$failure=[string]$_}
    $edCaseMatches=if($ExpectedRejection){$failure -and $failure.Contains($ExpectedRejection)}else{$null -eq $failure}
    if(-not $edCaseMatches){throw ('MINIMAP_REGRESSION: '+$Name+'; expected '+$ExpectedRejection+'; actual '+$failure)}
    $edMinimapCases.Add([ordered]@{name=$Name;expected=if($ExpectedRejection){'reject'}else{'accept'};passed=$true;rejection=$failure})
}

# Exact relevant fields from publisher captures; no new event observations are
# fabricated into these historical states. Keep fixtures in source so the next
# snapshot does not depend on the old release directory being available.
$edRecordedBefore=@'
{"tab":"领地","mapHudVisible":true,"minimapStyle":"youzhou-atlas-v1","speed":0.0,"selected":-1,"battleWorldHash":"246ED5FB6717820CE80AE5E4F98FF25F9DE14419058551C9CD169A89206109E0","focusX":9.0,"focusZ":16.0,"minimapViewportPoints":[0.0,0.5406525135040283,0.0,0.6469858884811401,0.23118047416210175,0.8781663775444031,0.528165340423584,0.581181526184082,0.24381819367408753,0.29683437943458559],"targets":[{"id":"map-home","x":1373.0,"y":120.0,"w":70.0,"h":44.0},{"id":"minimap-area","x":1304.0,"y":254.0,"w":204.0,"h":204.0}]}
'@ | ConvertFrom-Json
$edRecordedAfter=@'
{"tab":"领地","mapHudVisible":true,"minimapStyle":"youzhou-atlas-v1","speed":0.0,"selected":-1,"battleWorldHash":"246ED5FB6717820CE80AE5E4F98FF25F9DE14419058551C9CD169A89206109E0","focusX":8.323529243469239,"focusZ":26.558822631835939,"minimapViewportPoints":[0.0,0.25977030396461489,0.0,0.3999270498752594,0.2142687737941742,0.6141958236694336,0.5112536549568176,0.31721097230911257,0.22690649330615998,0.0328638069331646],"targets":[{"id":"map-home","x":1373.0,"y":120.0,"w":70.0,"h":44.0},{"id":"minimap-area","x":1304.0,"y":254.0,"w":204.0,"h":204.0}]}
'@ | ConvertFrom-Json
$edRecordedSource=[ordered]@{
    version='s1-20261026-193751710'
    directory='F:\EasternDynastyBuild\Releases\s1-20261026-193751710\Project\Validation\Visual\experience-interactive\20261026-200949-638b5ba0'
    before='02r-map-reinforcement-origin-3.json';beforeSha256='D490EAF32AE2A99E6EA1DDCB285390182BDE459F71DAD1607BE4F15632061383'
    after='02m-atlas-22.json';afterSha256='9750A3351174AEEB2E4D0BB6CFD5FC88A48A0BAC0FBD31EAA90D4DF620AED2F5'
    playerLogSha256='A5A03422BBD907FB467D8C7CD5381DC34508EF2B1C1C1AE873CD9450A1750A4B'
    pointerRequest=@(1247,217);loggedTopLeftPointer=@(1247,218);loggedBottomLeftPointer=@(1247,682);loggedFrame=1152
    interpretation='Historical MAP_MOUSE input log and focus agree at Unity top-left Y=218. Old captures did not record consumed IMGUI events; the new observer still requires a new publisher run.'
}
Invoke-MinimapCase 'historical Win32 Y assumption reproduces exact old failure' {Assert-ExperienceMinimapFocus $edRecordedBefore $edRecordedAfter 1247 217} 'minimap pointer position resolves'
Invoke-MinimapCase 'historical logged Unity pointer matches exact recorded focus' {Assert-ExperienceMinimapFocus $edRecordedBefore $edRecordedAfter 1247 218}

# All new event fields below are explicitly synthetic. They exercise the gates
# that the publisher will check against actual consumed events on the next run.
$edBefore=Copy-MinimapFixture $edRecordedBefore
$edBefore | Add-Member NoteProperty minimapNavigationSequence 3
$edBefore | Add-Member NoteProperty minimapNavigationPointerX 0.
$edBefore | Add-Member NoteProperty minimapNavigationPointerY 0.
$edAfter=Copy-MinimapFixture $edRecordedAfter
$edAfter | Add-Member NoteProperty minimapNavigationSequence 4
$edAfter | Add-Member NoteProperty minimapNavigationPointerX 1247.
$edAfter | Add-Member NoteProperty minimapNavigationPointerY 218.
Invoke-MinimapCase 'synthetic single consumed event preserves recorded map projection' {Assert-ExperienceMinimapNavigation $edBefore $edAfter 1247 217}
$edChanged=Copy-MinimapFixture $edAfter;$edChanged.minimapNavigationSequence=3
Invoke-MinimapCase 'reject stale event reused by a new click' {Assert-ExperienceMinimapNavigation $edBefore $edChanged 1247 217} 'exactly one new visible pointer event'
$edChanged=Copy-MinimapFixture $edAfter;$edChanged.minimapNavigationSequence=5
Invoke-MinimapCase 'reject two events consumed for one click' {Assert-ExperienceMinimapNavigation $edBefore $edChanged 1247 217} 'exactly one new visible pointer event'
$edChanged=Copy-MinimapFixture $edAfter;$edChanged.minimapNavigationPointerX+=1
Invoke-MinimapCase 'reject a received pointer from another client column' {Assert-ExperienceMinimapNavigation $edBefore $edChanged 1247 217} 'requested Windows client pixel'
$edChanged=Copy-MinimapFixture $edAfter;$edChanged.minimapNavigationPointerY=217
Invoke-MinimapCase 'reject conflating Windows client and Unity event rows' {Assert-ExperienceMinimapNavigation $edBefore $edChanged 1247 217} 'requested Windows client pixel'
$edChanged=Copy-MinimapFixture $edAfter;$edChanged.focusZ=26.754901960784313
Invoke-MinimapCase 'reject one screen pixel of real focus error without raising tolerance' {Assert-ExperienceMinimapNavigation $edBefore $edChanged 1247 217} 'minimap pointer position resolves'
$edChanged=Copy-MinimapFixture $edAfter;$edChanged.focusZ=39-$edChanged.focusZ
Invoke-MinimapCase 'reject vertically reversed map projection' {Assert-ExperienceMinimapNavigation $edBefore $edChanged 1247 217} 'minimap pointer position resolves'
$edChanged=Copy-MinimapFixture $edAfter;$edChanged.focusX=9;$edChanged.focusZ=16
Invoke-MinimapCase 'reject event receipt without moving camera' {Assert-ExperienceMinimapNavigation $edBefore $edChanged 1247 217} 'minimap pointer position resolves'
$edChanged=Copy-MinimapFixture $edAfter;$edChanged.battleWorldHash='changed'
Invoke-MinimapCase 'reject campaign mutation' {Assert-ExperienceMinimapNavigation $edBefore $edChanged 1247 217} 'preserves the paused campaign'
$edChanged=Copy-MinimapFixture $edAfter;$edChanged.selected=10
Invoke-MinimapCase 'reject terrain selection through atlas' {Assert-ExperienceMinimapNavigation $edBefore $edChanged 1247 217} 'preserves the paused campaign'
$edChanged=Copy-MinimapFixture $edAfter;$edChanged.minimapViewportPoints=$edBefore.minimapViewportPoints
Invoke-MinimapCase 'reject stale actual camera footprint' {Assert-ExperienceMinimapNavigation $edBefore $edChanged 1247 217} 'footprint moves with actual map navigation'
$edChanged=Copy-MinimapFixture $edAfter;$edChanged.minimapViewportPoints[0]=-.000001
Invoke-MinimapCase 'reject unclipped footprint even with a correct event' {Assert-ExperienceMinimapNavigation $edBefore $edChanged 1247 217} 'footprint remains within normalized map bounds'
$edChanged=Copy-MinimapFixture $edAfter;$edChanged.minimapNavigationPointerX=1190
Invoke-MinimapCase 'reject a received point outside visible atlas' {Assert-ExperienceMinimapNavigation $edBefore $edChanged 1190 217} 'clicks inside the actual visible atlas'

foreach($edScale in @(1.0,.8)) {
    foreach($edPoint in @(@(.22,.32),@(.78,.68))) {
        $edScaledBefore=Copy-MinimapFixture $edBefore;$edScaledAfter=Copy-MinimapFixture $edAfter
        foreach($edState in @($edScaledBefore,$edScaledAfter)) {
            foreach($edTarget in $edState.targets){$edTarget.x=$edTarget.x*$edScale+16;$edTarget.y=$edTarget.y*$edScale+12;$edTarget.w*=$edScale;$edTarget.h*=$edScale}
        }
        $edArea=Get-ExperienceTarget $edScaledBefore 'minimap-area'
        $edClickX=[Math]::Round($edArea.x+($edPoint[0]-.5)*$edArea.w)
        $edClickY=[Math]::Round($edArea.y+($edPoint[1]-.5)*$edArea.h)
        $edScaledAfter.minimapNavigationPointerX=$edClickX;$edScaledAfter.minimapNavigationPointerY=$edClickY+1
        $edScaledAfter.focusX=($edClickX-($edArea.x-$edArea.w*.5))/$edArea.w*40-.5
        $edScaledAfter.focusZ=39.5-(($edClickY+1)-($edArea.y-$edArea.h*.5))/$edArea.h*40
        Invoke-MinimapCase ('synthetic scaled/offset safe area '+$edScale+' at '+$edPoint[0]) {Assert-ExperienceMinimapNavigation $edScaledBefore $edScaledAfter $edClickX $edClickY}
    }
}
$edSidebarContract='youzhou-atlas-sidebar-v3'
# Synthetic design-contract fixtures: these are computed expectations, never new
# client captures or evidence of an actual font/resource/input/visual acceptance.
function New-SyntheticSidebarFixture([double]$Width,[double]$Height,[bool]$Mobile) {
    $safeX=if($Mobile){$Width*.035}else{0.0}
    $safeY=if($Mobile){12.0}else{0.0}
    $safeWidth=if($Mobile){$Width*.94}else{$Width}
    $safeHeight=if($Mobile){$Height-36}else{$Height}
    $scale=[Math]::Max($(if($Mobile){.1}else{.35}),[Math]::Min($safeWidth/1440.0,$safeHeight/$(if($Mobile){820.0}else{900.0})))
    $panelX=$safeWidth/$scale-302
    $state=Copy-MinimapFixture $edBefore
    $state | Add-Member NoteProperty minimapDesignStyle $edSidebarContract
    $state | Add-Member NoteProperty minimapTextFits $true
    $state | Add-Member NoteProperty mapHudSafeX $safeX
    $state | Add-Member NoteProperty mapHudSafeY $safeY
    $state | Add-Member NoteProperty mapHudSafeWidth $safeWidth
    $state | Add-Member NoteProperty mapHudTouchMinimum (44*$scale)
    $state.targets=@(foreach($rect in @(@('minimap-panel',0,0,282,204),@('minimap-title',210,0,72,54),@('map-home',216,64,60,60),@('map-home-art',236,70,20,28),@('map-home-caption',216,101,60,20),@('minimap-area',0,0,204,204),@('minimap-legend',210,134,72,70))) {
        [pscustomobject]@{id=$rect[0];x=$safeX+($panelX+$rect[1]+$rect[3]*.5)*$scale;y=$safeY+(90+$rect[2]+$rect[4]*.5)*$scale;w=$rect[3]*$scale;h=$rect[4]*$scale}
    })
    return $state
}
foreach($edSize in @(@(1440,900),@(1280,720),@(1600,720),@(1920,1080),@(1024,768),@(2340,1080))) {
    foreach($edMobile in @($false,$true)) {
        $cw=[double]$edSize[0];$ch=[double]$edSize[1];$PixelWidth=$edSize[0];$PixelHeight=$edSize[1]
        $edSidebar=New-SyntheticSidebarFixture $cw $ch $edMobile
        $edCaseLabel='synthetic sidebar '+$PixelWidth+'x'+$PixelHeight+' mobile='+$edMobile
        Invoke-MinimapCase ($edCaseLabel+' exact full terrain and separate sidebar') {Assert-ExperienceMinimapDesign $edSidebar}
        foreach($edPoint in @(@(.22,.32),@(.78,.68))) {
            $edSidebarAfter=Copy-MinimapFixture $edSidebar
            $edSidebarAfter.minimapViewportPoints=$edAfter.minimapViewportPoints
            $edSidebarAfter.minimapNavigationSequence=$edSidebar.minimapNavigationSequence+1
            $edArea=Get-ExperienceTarget $edSidebar 'minimap-area'
            $edClickX=[Math]::Round($edArea.x+($edPoint[0]-.5)*$edArea.w)
            $edClickY=[Math]::Round($edArea.y+($edPoint[1]-.5)*$edArea.h)
            $edSidebarAfter.minimapNavigationPointerX=$edClickX;$edSidebarAfter.minimapNavigationPointerY=$edClickY+1
            $edSidebarAfter.focusX=($edClickX-($edArea.x-$edArea.w*.5))/$edArea.w*40-.5
            $edSidebarAfter.focusZ=39.5-(($edClickY+1)-($edArea.y-$edArea.h*.5))/$edArea.h*40
            Invoke-MinimapCase ($edCaseLabel+' unchanged 40x40 pointer projection at '+$edPoint[0]) {Assert-ExperienceMinimapNavigation $edSidebar $edSidebarAfter $edClickX $edClickY;Assert-ExperienceMinimapDesign $edSidebarAfter}
        }
        $edChanged=Copy-MinimapFixture $edSidebar;$edChanged.minimapDesignStyle='youzhou-atlas-floating-v2'
        Invoke-MinimapCase ($edCaseLabel+' rejects old layout identity') {Assert-ExperienceMinimapDesign $edChanged} 'exported sidebar design'
        $edChanged=Copy-MinimapFixture $edSidebar;$edChanged.minimapTextFits=$false
        Invoke-MinimapCase ($edCaseLabel+' rejects clipped live title or missing resource readiness') {Assert-ExperienceMinimapDesign $edChanged} 'actual font fit'
        $edChanged=Copy-MinimapFixture $edSidebar
        (Get-ExperienceTarget $edChanged 'minimap-panel').h=286*($edChanged.mapHudTouchMinimum/44.0)
        Invoke-MinimapCase ($edCaseLabel+' rejects former 286-high panel despite new identity') {Assert-ExperienceMinimapDesign $edChanged} 'minimap-panel matches'
        $edChanged=Copy-MinimapFixture $edSidebar
        (Get-ExperienceTarget $edChanged 'minimap-title').h-=8*($edChanged.mapHudTouchMinimum/44.0)
        Invoke-MinimapCase ($edCaseLabel+' rejects truncated title region') {Assert-ExperienceMinimapDesign $edChanged} 'minimap-title matches'
        $edChanged=Copy-MinimapFixture $edSidebar;$edChanged.targets=@($edChanged.targets | Where-Object id -ne 'minimap-legend')
        Invoke-MinimapCase ($edCaseLabel+' rejects missing legend') {Assert-ExperienceMinimapDesign $edChanged} 'Missing/ambiguous current visible target minimap-legend'
        $edChanged=Copy-MinimapFixture $edSidebar
        (Get-ExperienceTarget $edChanged 'map-home-art').y=(Get-ExperienceTarget $edChanged 'map-home-caption').y
        Invoke-MinimapCase ($edCaseLabel+' rejects locator image over live caption') {Assert-ExperienceMinimapDesign $edChanged} 'map-home-art matches'
        $edChanged=Copy-MinimapFixture $edSidebar
        (Get-ExperienceTarget $edChanged 'minimap-legend').x=(Get-ExperienceTarget $edChanged 'minimap-area').x
        Invoke-MinimapCase ($edCaseLabel+' rejects legend over live terrain') {Assert-ExperienceMinimapDesign $edChanged} 'minimap-legend matches'
    }
}
$edMinimapResult=[ordered]@{
    status='PASSED';checkedUtc=[DateTime]::UtcNow.ToString('o');checks=$edMinimapCases.Count
    scope='Pure assertion regression with historical capture/log fields and labelled synthetic event/design fixtures. No Unity, client, screenshot, pointer or Android run; synthetic font/resource flags are not runtime evidence.'
    syntheticDesignContract=$edSidebarContract
    recordedSource=$edRecordedSource
    oldExpectedFocus=@(8.323529411764706,26.754901960784313)
    recordedFocus=@($edRecordedAfter.focusX,$edRecordedAfter.focusZ)
    logPointerExpectedFocus=@(8.323529411764706,26.558823529411764)
    cases=$edMinimapCases.ToArray();scriptSha256=(Get-FileHash -LiteralPath $edMinimapScript -Algorithm SHA256).Hash
}
if($OutputDirectory) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'navigation-checks.json'),($edMinimapResult | ConvertTo-Json -Depth 10),[Text.UTF8Encoding]::new($false))
}
Write-Output ('MINIMAP_NAVIGATION_CHECKS_OK: '+$edMinimapCases.Count+' pure regression cases; recorded input diagnosis, synthetic event/sidebar gates; actual new input and visuals remain for publisher.')
