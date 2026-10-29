param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$edCityHeaderRoot=Split-Path -Parent $PSScriptRoot
$edCityHeaderTokens=$null;$edCityHeaderErrors=$null
$edCityHeaderAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $edCityHeaderRoot 'Tools/Check-Experience.ps1'),[ref]$edCityHeaderTokens,[ref]$edCityHeaderErrors)
if($edCityHeaderErrors.Count){throw ($edCityHeaderErrors | Out-String)}
# Import only pure assertions and file-based pixel comparison. Never execute
# Check-Experience's client, desktop or native-input initialization.
foreach($name in @('Assert-Experience','Get-ExperienceTarget','Assert-ExperienceTargetInClient','Assert-ExperienceCityHeader','Test-ExperienceLandContains','Test-ExperienceLandOverlap','Compare-ExperienceCityHeaderPixels')) {
    $fn=@($edCityHeaderAst.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst]},$true) | Where-Object Name -eq $name)
    if($fn.Count -ne 1){throw ('Expected one pure city header function: '+$name)}
    . ([scriptblock]::Create($fn[0].Extent.Text))
}
$cw=4096;$ch=2160;$PixelWidth=4096;$PixelHeight=2160
$edExperienceChecks=[Collections.Generic.List[string]]::new()
$edCityHeaderCases=[Collections.Generic.List[object]]::new()
function Copy-CityHeaderFixture($State){$State | ConvertTo-Json -Depth 8 -Compress | ConvertFrom-Json}
function New-CityHeaderFixture([double]$Unit,[double]$Width,[bool]$Wrapped) {
    # Frozen inspected design coordinates. These are synthetic fixture records,
    # deliberately independent of Runtime layout and never labelled a capture.
    $rows=@(@('city-header-backdrop',0,0,$Width,70),@('city-hud-home',16,4,246,62),@('city-hud-home-crest',16,4,62,62),@('city-hud-home-art',22,13,50,44),@('city-hud-home-name',84,$(if($Wrapped){5}else{7}),174,$(if($Wrapped){38}else{30})),@('city-hud-home-detail',84,44,174,20),@('city-header-production',280,5,672,58))
    foreach($i in 0..5) {
        $x=if($i -lt 4){280+168*$i}else{968+150*($i-4)}
        $rows+=@(@(('hud-resource-'+$i),$x,5,$(if($i -lt 4){162}else{144}),58),@(('hud-resource-icon-'+$i),($x+$(if($i -lt 4){6}else{7})),19,$(if($i -lt 4){32}else{26}),$(if($i -lt 4){26}else{28})),@(('hud-resource-stock-'+$i),($x+$(if($i -lt 4){44}else{40})),30,$(if($i -lt 4){114}else{100}),26),@(('hud-resource-detail-'+$i),($x+$(if($i -lt 4){44}else{40})),6,$(if($i -lt 4){114}else{100}),23))
    }
    $targets=@(foreach($r in $rows){[pscustomobject]@{id=$r[0];x=56+($r[1]+$r[3]*.5)*$Unit;y=12+($r[2]+$r[4]*.5)*$Unit;w=$r[3]*$Unit;h=$r[4]*$Unit}})
    [pscustomobject]@{tab='主城';cityShellStyle='city-crest-landscape-navigation-v1';cityHeaderDesignStyle='inkscape-city-ledger-v2';cityHeaderDesignTextFits=$true;cityHeaderBackdropDrawn=$true;cityHeaderNameWrapped=$Wrapped;cityHudFullName='幽州主城';cityHudShortName='幽州主城';cityHudLevel=3;mapHudTouchMinimum=44*$Unit;mapHudSafeX=56;mapHudSafeY=12;mapHudSafeWidth=$Width*$Unit;targets=$targets}
}
function Check-CityHeaderCase([string]$Name,$State,[bool]$ExpectedPass,[string]$ExpectedMessage='') {
    $failure=$null
    try{Assert-ExperienceCityHeader $State}catch{$failure=[string]$_}
    if(($null -eq $failure) -ne $ExpectedPass -or ($ExpectedMessage -and $failure -notlike ('*'+$ExpectedMessage+'*'))){throw ('CITY_HEADER_REGRESSION: '+$Name+' expected pass='+$ExpectedPass+'; '+$failure)}
    $edCityHeaderCases.Add([ordered]@{name=$Name;expected=if($ExpectedPass){'accept'}else{'reject'};passed=$true;rejection=$failure})
}
foreach($unit in @(1.0,0.833333,0.72)) {
    foreach($width in @(1280,1440,1804.8,2560)){foreach($wrapped in @($false,$true)){Check-CityHeaderCase "valid-$unit-$width-$wrapped" (New-CityHeaderFixture $unit $width $wrapped) $true}}
    $s=New-CityHeaderFixture $unit 1804.8 $true;$s.cityHeaderDesignStyle='inkscape-city-header-v1';Check-CityHeaderCase "old-style-$unit" $s $false 'inspected ledger design'
    $s=New-CityHeaderFixture $unit 1804.8 $true;$s.cityHeaderBackdropDrawn=$false;Check-CityHeaderCase "backdrop-not-drawn-$unit" $s $false 'actual opaque backdrop draw'
    $s=New-CityHeaderFixture $unit 1804.8 $true;$s.targets=@($s.targets | Where-Object id -ne 'city-header-backdrop');Check-CityHeaderCase "missing-backdrop-target-$unit" $s $false 'Missing/ambiguous current visible target city-header-backdrop'
    $s=New-CityHeaderFixture $unit 1804.8 $true;(Get-ExperienceTarget $s 'city-header-backdrop').w-=32*$unit;Check-CityHeaderCase "partial-backdrop-$unit" $s $false
    $s=New-CityHeaderFixture $unit 1804.8 $true;$s.cityHeaderDesignTextFits=$false;Check-CityHeaderCase "font-overflow-$unit" $s $false 'inspected ledger design'
    $s=New-CityHeaderFixture $unit 1804.8 $true;$s.cityHudShortName='幽州…';Check-CityHeaderCase "truncated-home-name-$unit" $s $false 'inspected ledger design'
    $s=New-CityHeaderFixture $unit 1804.8 $true;$s.tab='武将';Check-CityHeaderCase "stale-header-on-ordinary-page-$unit" $s $false 'inspected ledger design'
    $s=New-CityHeaderFixture $unit 1804.8 $true;(Get-ExperienceTarget $s 'city-hud-home').w=252*$unit;Check-CityHeaderCase "old-identity-width-$unit" $s $false 'city-hud-home matches'
    $s=New-CityHeaderFixture $unit 1804.8 $true;(Get-ExperienceTarget $s 'city-hud-home-art').w=72*$unit;(Get-ExperienceTarget $s 'city-hud-home-art').h=60*$unit;Check-CityHeaderCase "old-oversized-art-$unit" $s $false
    $s=New-CityHeaderFixture $unit 1804.8 $true;(Get-ExperienceTarget $s 'city-hud-home-crest').w=20*$unit;Check-CityHeaderCase "narrow-seal-$unit" $s $false 'city-hud-home-crest matches'
    $s=New-CityHeaderFixture $unit 1804.8 $true;(Get-ExperienceTarget $s 'hud-resource-stock-4').y-=23*$unit;Check-CityHeaderCase "obsolete-currency-baseline-$unit" $s $false
    $s=New-CityHeaderFixture $unit 1804.8 $true;(Get-ExperienceTarget $s 'hud-resource-detail-5').y+=27*$unit;Check-CityHeaderCase "currency-label-below-stock-$unit" $s $false
    $s=New-CityHeaderFixture $unit 1804.8 $true;foreach($r in @($s.targets | Where-Object id -match '^hud-resource(-icon|-stock|-detail)?-[45]$')){$r.x+=320*$unit};Check-CityHeaderCase "detached-wide-screen-currencies-$unit" $s $false 'hud-resource-4 matches'
    $s=New-CityHeaderFixture $unit 1804.8 $true;(Get-ExperienceTarget $s 'hud-resource-icon-0').w=26*$unit;Check-CityHeaderCase "obsolete-production-icon-width-$unit" $s $false 'hud-resource-icon-0 matches'
    $s=New-CityHeaderFixture $unit 1804.8 $true;$s.targets=@($s.targets | Where-Object id -ne 'hud-resource-detail-0');Check-CityHeaderCase "missing-income-$unit" $s $false 'Missing/ambiguous current visible target hud-resource-detail-0'
    $s=New-CityHeaderFixture $unit 1804.8 $true;$s.targets+=Copy-CityHeaderFixture (Get-ExperienceTarget $s 'hud-resource-stock-5');Check-CityHeaderCase "duplicate-stock-$unit" $s $false 'Missing/ambiguous current visible target hud-resource-stock-5'
    $s=New-CityHeaderFixture $unit 1804.8 $true;(Get-ExperienceTarget $s 'city-header-production').w-=168*$unit;Check-CityHeaderCase "missing-fourth-ledger-column-$unit" $s $false 'same visible ledger'
}
$edPixelCases=[Collections.Generic.List[object]]::new()
if($OutputDirectory) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    Add-Type -AssemblyName System.Drawing
    $edImageA=Join-Path $OutputDirectory 'city-header-synthetic-before.png'
    $edImageB=Join-Path $OutputDirectory 'city-header-synthetic-after.png'
    $edImage=[Drawing.Bitmap]::new(160,90)
    try {
        $edBrush=[Drawing.Graphics]::FromImage($edImage)
        try{$edBrush.Clear([Drawing.Color]::FromArgb(255,30,40,50))}finally{$edBrush.Dispose()}
        $edImage.Save($edImageA,[Drawing.Imaging.ImageFormat]::Png)
        $edImage.Save($edImageB,[Drawing.Imaging.ImageFormat]::Png)
        $edPixelState=[pscustomobject]@{mapHudTouchMinimum=4.4;mapHudSafeX=4;mapHudSafeY=2;mapHudSafeWidth=144}
        $equal=Compare-ExperienceCityHeaderPixels $edImageA $edImageB $edPixelState
        if($equal.differentPixels -ne 0 -or $equal.comparedPixels -ne 1008){throw 'Pixel comparator did not accept equal synthetic header rectangles.'}
        $edPixelCases.Add([ordered]@{name='equal synthetic header';passed=$true;result=$equal})
        $edImage.SetPixel(130,6,[Drawing.Color]::White)
        $edImage.Save($edImageB,[Drawing.Imaging.ImageFormat]::Png)
        $changed=Compare-ExperienceCityHeaderPixels $edImageA $edImageB $edPixelState
        if($changed.differentPixels -ne 1 -or $changed.firstDifferentPixel[0] -ne 130 -or $changed.firstDifferentPixel[1] -ne 6){throw 'Pixel comparator failed to find one changed synthetic header pixel.'}
        $edPixelCases.Add([ordered]@{name='detect one stale header pixel';passed=$true;result=$changed})
        $edImage.SetPixel(130,6,[Drawing.Color]::FromArgb(255,30,40,50));$edImage.SetPixel(130,50,[Drawing.Color]::White)
        $edImage.Save($edImageB,[Drawing.Imaging.ImageFormat]::Png)
        $outside=Compare-ExperienceCityHeaderPixels $edImageA $edImageB $edPixelState
        if($outside.differentPixels -ne 0){throw 'Pixel comparator incorrectly included pixels outside the city header.'}
        $edPixelCases.Add([ordered]@{name='ignore unrelated body pixels';passed=$true;result=$outside})
    } finally {$edImage.Dispose()}
    $edCityHeaderResult=[ordered]@{status='PASSED';checkedUtc=[DateTime]::UtcNow.ToString('o');caseCount=$edCityHeaderCases.Count+$edPixelCases.Count;scope='Pure assertions against synthetic inspected-layout records and synthetic PNGs; no client, Unity, runtime capture or visual-quality acceptance.';cases=@($edCityHeaderCases.ToArray());pixelCases=@($edPixelCases.ToArray())}
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'city-header-experience.json'),($edCityHeaderResult | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
}
Write-Output ('PASS: '+$edCityHeaderCases.Count+' city header geometry/visibility rejection cases and '+$edPixelCases.Count+' synthetic pixel cases; no desktop interaction or runtime visual acceptance.')
