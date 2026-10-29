param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$edHeaderRoot=Split-Path -Parent $PSScriptRoot
$edTokens=$null;$edErrors=$null
$edAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $edHeaderRoot 'Tools/Check-Experience.ps1'),[ref]$edTokens,[ref]$edErrors)
if($edErrors.Count){throw ($edErrors|Out-String)}
# Import only data assertions. No client, native input or desktop code is executed.
foreach($name in @('Assert-Experience','Get-ExperienceTarget','Assert-ExperienceMapHeader','Get-ExperienceHeaderCrestPoint')) {
    $fn=@($edAst.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst]},$true)|Where-Object Name -eq $name)
    if($fn.Count -ne 1){throw ('Missing header assertion: '+$name)}
    . ([scriptblock]::Create($fn[0].Extent.Text))
}
$edExperienceChecks=[Collections.Generic.List[string]]::new()
$edCases=[Collections.Generic.List[object]]::new()
function New-HeaderFixture([double]$Unit,[double]$Width,[bool]$Wrapped) {
    $rows=@(@('hud-ruler-city',16,4,246,62),@('hud-ruler-crest',16,4,62,62),@('hud-ruler-art',22,13,50,44),@('hud-ruler-detail',84,44,174,20),@('hud-ruler-name',84,$(if($Wrapped){5}else{7}),174,$(if($Wrapped){38}else{30})))
    # Frozen inspected 168-unit ledger at all supported logical widths.
    foreach($i in 0..5) {
        $x=if($i -lt 4){280+168*$i}else{968+150*($i-4)}
        $rows+=@(@(('hud-resource-'+$i),$x,5,$(if($i -lt 4){162}else{144}),58),@(('hud-resource-icon-'+$i),($x+$(if($i -lt 4){6}else{7})),19,$(if($i -lt 4){32}else{26}),$(if($i -lt 4){26}else{28})),@(('hud-resource-stock-'+$i),($x+$(if($i -lt 4){44}else{40})),30,$(if($i -lt 4){114}else{100}),26),@(('hud-resource-detail-'+$i),($x+$(if($i -lt 4){44}else{40})),6,$(if($i -lt 4){114}else{100}),23))
    }
    $targets=@(foreach($r in $rows){[pscustomobject]@{id=$r[0];x=56+($r[1]+$r[3]*.5)*$Unit;y=12+($r[2]+$r[4]*.5)*$Unit;w=$r[3]*$Unit;h=$r[4]*$Unit}})
    [pscustomobject]@{mapHudHeaderStyle='youzhou-reference-ledger-v2';mapHudHeaderTextFits=$true;mapHudTouchMinimum=44*$Unit;mapHudSafeX=56;mapHudSafeY=12;mapHudSafeWidth=$Width*$Unit;targets=$targets}
}
function Check-HeaderCase([string]$Name,$State,[bool]$Pass) {
    $failed=$false;try{Assert-ExperienceMapHeader $State}catch{$failed=$true}
    if($failed -eq $Pass){throw ('Unexpected header assertion result: '+$Name)}
    if($Pass) {
        $point=Get-ExperienceHeaderCrestPoint $State
        $crest=Get-ExperienceTarget $State 'hud-ruler-crest';$art=Get-ExperienceTarget $State 'hud-ruler-art'
        $px=[Math]::Round($point.x);$py=[Math]::Round($point.y)
        if($px -le $crest.x-$crest.w*.5 -or $px -ge $crest.x+$crest.w*.5 -or $py -le $crest.y-$crest.h*.5 -or $py -ge $art.y-$art.h*.5){throw 'Crest rim click must stay visible and separate from the city-image center after physical pixel rounding.'}
    }
    $edCases.Add([pscustomobject]@{name=$Name;expectedPass=$Pass;passed=$true})
}
foreach($unit in @(1.0,0.833333,0.72)) {
    foreach($width in @(1440,1804.8,2560)) {
        foreach($wrapped in @($false,$true)){Check-HeaderCase "valid-$unit-$width-$wrapped" (New-HeaderFixture $unit $width $wrapped) $true}
    }
    $s=New-HeaderFixture $unit 1804.8 $true;$s.mapHudHeaderTextFits=$false;Check-HeaderCase "font-overflow-$unit" $s $false
    $s=New-HeaderFixture $unit 1804.8 $true;$s.mapHudHeaderStyle='youzhou-reference-ledger-v1';Check-HeaderCase "old-style-$unit" $s $false
    $s=New-HeaderFixture $unit 1804.8 $true;($s.targets|Where-Object id -eq 'hud-resource-stock-4').y-=23*$unit;Check-HeaderCase "currency-old-baseline-$unit" $s $false
    $s=New-HeaderFixture $unit 1804.8 $true;foreach($r in @($s.targets|Where-Object id -match '^hud-resource-.*4$')){$r.x+=320*$unit};Check-HeaderCase "detached-currency-$unit" $s $false
    $s=New-HeaderFixture $unit 1804.8 $true;($s.targets|Where-Object id -eq 'hud-ruler-crest').w=20*$unit;Check-HeaderCase "clipped-crest-$unit" $s $false
    $s=New-HeaderFixture $unit 1804.8 $true;$s.targets=@($s.targets|Where-Object id -ne 'hud-resource-detail-0');Check-HeaderCase "missing-visible-rate-$unit" $s $false
}
if($OutputDirectory){New-Item -ItemType Directory -Path $OutputDirectory -Force|Out-Null;$edCases|ConvertTo-Json -Depth 4|Set-Content -Encoding UTF8 (Join-Path $OutputDirectory 'header-experience.json')}
Write-Output ('PASS: '+$edCases.Count+' pure header acceptance cases; synthetic data, no runtime or desktop execution.')
