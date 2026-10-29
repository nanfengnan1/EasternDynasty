param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$edChromeRoot=Split-Path -Parent $PSScriptRoot
$edChromeTokens=$null;$edChromeErrors=$null
$edChromeAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $edChromeRoot 'Tools/Check-Experience.ps1'),[ref]$edChromeTokens,[ref]$edChromeErrors)
if($edChromeErrors.Count){throw ($edChromeErrors | Out-String)}
# Import only pure assertions. This does not initialize native input, launch a
# client, take a capture or access the desktop. Fixtures are synthetic records.
foreach($name in @('Assert-Experience','Get-ExperienceTarget','Assert-ExperienceTargetInClient','Assert-ExperienceCardChrome','Convert-ExperienceCityBounds','Test-ExperienceLandContains','Test-ExperienceLandOverlap')) {
    $fn=@($edChromeAst.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst]},$true) | Where-Object Name -eq $name)
    if($fn.Count -ne 1){throw ('Expected one pure card chrome function: '+$name)}
    . ([scriptblock]::Create($fn[0].Extent.Text))
}
$cw=4096;$ch=2160;$PixelWidth=4096;$PixelHeight=2160
$edExperienceChecks=[Collections.Generic.List[string]]::new()
$edChromeCases=[Collections.Generic.List[object]]::new()
function New-CardChromeFixture([double]$Unit,[double]$Height,[bool]$Owned) {
    # Frozen inspected coordinates for the 1280-wide existing modal. Independent
    # of the Runtime calculator, with the full portrait deliberately retained.
    $rightX=678.4;$rightY=40;$width=657.6;$tabWidth=197.2
    $rows=@(@('card-attributes',0,24,$tabWidth,54),@('card-skill',$tabWidth,24,$tabWidth,54),@('card-source',394.4,24,$tabWidth,54),@('card-close',603.6,24,54,54),@('card-scroll',0,142,$width,($Height-226)),@('card-return',477.6,($Height-62),180,54))
    if($Owned){$rows+=,@('owned-card-manage',0,($Height-62),240,54)}
    $targets=@(foreach($r in $rows){[pscustomobject]@{id=$r[0];x=20+($rightX+$r[1]+$r[3]/2)*$Unit;y=12+($rightY+$r[2]+$r[4]/2)*$Unit;w=$r[3]*$Unit;h=$r[4]*$Unit}})
    [pscustomobject]@{
        cardDetail='synthetic-card';cardDetailOwnedUid=$(if($Owned){7}else{0});cardSection=0
        cardChromeStyle='inkscape-card-chrome-v1';cardChromeAssetsReady=$true;cardChromeTextFits=$true
        cardChromeRight=@((20+$rightX*$Unit),(12+$rightY*$Unit),($width*$Unit),($Height*$Unit))
        cardPortraitFrame=@((20+100*$Unit),(12+54*$Unit),(550.4*$Unit),(($Height-28)*$Unit))
        cardDossierIdentity=@((20+$rightX*$Unit),(12+130*$Unit),($width*$Unit),(40*$Unit))
        mapHudTouchMinimum=(44*$Unit);mapHudSafeX=20;mapHudSafeY=12;mapHudSafeWidth=(1440*$Unit);mapHudSafeHeight=(1000*$Unit)
        targets=$targets
    }
}
function Check-CardChromeCase([string]$Name,$State,[bool]$ExpectedPass,[string]$ExpectedMessage='') {
    $failure=$null
    try{Assert-ExperienceCardChrome $State}catch{$failure=[string]$_}
    if(($null -eq $failure) -ne $ExpectedPass -or ($ExpectedMessage -and $failure -notlike ('*'+$ExpectedMessage+'*'))){throw ('CARD_CHROME_REGRESSION: '+$Name+' expected pass='+$ExpectedPass+'; '+$failure)}
    $edChromeCases.Add([ordered]@{name=$Name;expected=if($ExpectedPass){'accept'}else{'reject'};passed=$true;rejection=$failure})
}
foreach($unit in @(1.0,0.833333,0.72)) {
    foreach($height in @(792,820)){foreach($owned in @($false,$true)){Check-CardChromeCase "valid-$unit-$height-$owned" (New-CardChromeFixture $unit $height $owned) $true}}
    $s=New-CardChromeFixture $unit 820 $false;$s.cardChromeStyle='old-tall-header';Check-CardChromeCase "old-style-$unit" $s $false 'inspected Inkscape surfaces'
    $s=New-CardChromeFixture $unit 820 $false;$s.cardChromeAssetsReady=$false;Check-CardChromeCase "missing-export-$unit" $s $false 'inspected Inkscape surfaces'
    $s=New-CardChromeFixture $unit 820 $false;$s.cardChromeTextFits=$false;Check-CardChromeCase "font-overflow-$unit" $s $false 'fits actual Chinese fonts'
    $s=New-CardChromeFixture $unit 820 $false;$s.cardDetail='';Check-CardChromeCase "closed-modal-stale-chrome-$unit" $s $false
    $s=New-CardChromeFixture $unit 820 $false;$s.cardPortraitFrame[2]-=20*$unit;Check-CardChromeCase "portrait-shrunk-$unit" $s $false 'complete portrait allocation'
    $s=New-CardChromeFixture $unit 820 $false;$r=Get-ExperienceTarget $s 'card-scroll';$r.y+=28*$unit;$r.h-=56*$unit;Check-CardChromeCase "old-tall-header-viewport-$unit" $s $false 'card-scroll matches'
    $s=New-CardChromeFixture $unit 820 $false;$r=Get-ExperienceTarget $s 'card-skill';$r.y+=116*$unit;$r.h-=2*$unit;Check-CardChromeCase "old-second-row-tabs-$unit" $s $false 'card-skill matches'
    $s=New-CardChromeFixture $unit 820 $false;(Get-ExperienceTarget $s 'card-close').x-=24*$unit;Check-CardChromeCase "close-overlaps-tab-$unit" $s $false 'card-close matches'
    $s=New-CardChromeFixture $unit 820 $false;(Get-ExperienceTarget $s 'card-return').h=30*$unit;Check-CardChromeCase "small-return-touch-$unit" $s $false
    $s=New-CardChromeFixture $unit 820 $true;$s.targets=@($s.targets | Where-Object id -ne 'owned-card-manage');Check-CardChromeCase "missing-owned-management-$unit" $s $false 'owned-card-manage'
    $s=New-CardChromeFixture $unit 820 $true;$s.cardDetailOwnedUid=0;Check-CardChromeCase "preview-stale-management-$unit" $s $false 'stale owned management'
    $s=New-CardChromeFixture $unit 820 $true;(Get-ExperienceTarget $s 'owned-card-manage').x+=470*$unit;Check-CardChromeCase "manage-overlaps-return-$unit" $s $false
    $s=New-CardChromeFixture $unit 820 $false;$s.cardDossierIdentity[1]+=16*$unit;Check-CardChromeCase "identity-overlaps-content-$unit" $s $false 'identity band'
    $s=New-CardChromeFixture $unit 820 $false;$s.mapHudSafeHeight=700*$unit;Check-CardChromeCase "footer-outside-mobile-safe-area-$unit" $s $false 'safe area'
    $s=New-CardChromeFixture $unit 820 $false;$s.targets+=Get-ExperienceTarget $s 'card-source';Check-CardChromeCase "duplicate-source-target-$unit" $s $false 'Missing/ambiguous'
}
if($OutputDirectory) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    $edChromeResult=[ordered]@{status='PASSED';checkedUtc=[DateTime]::UtcNow.ToString('o');caseCount=$edChromeCases.Count;scope='Pure assertions against synthetic inspected-layout records; no Unity, client, runtime capture or visual-quality acceptance.';cases=@($edChromeCases.ToArray())}
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'card-chrome-experience.json'),($edChromeResult | ConvertTo-Json -Depth 7),[Text.UTF8Encoding]::new($false))
}
Write-Output ('PASS: '+$edChromeCases.Count+' pure card chrome acceptance/rejection cases; synthetic data, no runtime or desktop execution.')
