param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$edCityLabelRoot=Split-Path -Parent $PSScriptRoot
$edCityLabelTokens=$null;$edCityLabelErrors=$null
$edCityLabelAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $edCityLabelRoot 'Tools/Check-Experience.ps1'),[ref]$edCityLabelTokens,[ref]$edCityLabelErrors)
if($edCityLabelErrors.Count -gt 0){throw ($edCityLabelErrors | Out-String)}
# Import only pure validators, never the desktop harness. All data below is
# explicitly synthetic; this cannot establish runtime rendering or click success.
foreach($edCityLabelName in @('Assert-Experience','Get-ExperienceTarget','Assert-ExperienceTargetInClient','Assert-ExperienceMapCityLabels','Assert-ExperienceMapCommandModal','Test-ExperienceLandContains','Test-ExperienceLandOverlap')) {
    $edCityLabelFunction=@($edCityLabelAst.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst]},$true) | Where-Object {$_.Name -eq $edCityLabelName})
    if($edCityLabelFunction.Count -ne 1){throw ('Expected one pure assertion: '+$edCityLabelName)}
    . ([scriptblock]::Create($edCityLabelFunction[0].Extent.Text))
}
$cw=1440.0;$ch=900.0;$PixelWidth=1440;$PixelHeight=900
$edExperienceChecks=[Collections.Generic.List[string]]::new()
$edCityLabelCases=[Collections.Generic.List[object]]::new()
function Copy-CityLabelState($State){$State | ConvertTo-Json -Depth 8 -Compress | ConvertFrom-Json}
function New-SyntheticCityLabelState([double]$Width=184,[double]$Height=52,[double]$Unit=1) {
    $sx=24.0;$sy=16.0;$x=520.0;$y=340.0
    $targets=[Collections.Generic.List[object]]::new()
    function Add-LabelTarget([string]$Id,[double]$X,[double]$Y,[double]$W,[double]$H){$targets.Add([pscustomobject]@{id=$Id;x=$sx+($X+$W*.5)*$Unit;y=$sy+($Y+$H*.5)*$Unit;w=$W*$Unit;h=$H*$Unit})}
    Add-LabelTarget 'city-label-731' $x $y $Width $Height
    Add-LabelTarget 'capital-label-731' $x $y $Width $Height
    # Full 3:2 image fitted to the 40x28 container, centered in the 52px seal.
    Add-LabelTarget 'city-label-art-731' ($x+6) ($y+$Height*.5-40.0/3) 40 (80.0/3)
    Add-LabelTarget 'city-label-name-731' ($x+60) ($y+2) ($Width-70) ($Height-28)
    Add-LabelTarget 'city-label-detail-731' ($x+60) ($y+$Height-24) ($Width-70) 22
    return [pscustomobject]@{tab='领地';modal=$false;confirmationOpen=$false;cardDetail='';mapCityLabelStyle='youzhou-city-marker-v1';mapCityLabelDesignReady=$true;mapCityLabelTextFits=$true;mapHudTouchMinimum=44*$Unit;mapHudSafeX=$sx;mapHudSafeY=$sy;mapHudSafeWidth=1200*$Unit;mapHudSafeHeight=800*$Unit;targets=@($targets.ToArray())}
}
function Invoke-CityLabelCase([string]$Name,$State,[bool]$ExpectedPass,[bool]$Modal=$false) {
    $failure=$null
    try{if($Modal){Assert-ExperienceMapCommandModal $State}else{Assert-ExperienceMapCityLabels $State}}catch{$failure=[string]$_}
    if(($null -eq $failure) -ne $ExpectedPass){throw ('CITY_LABEL_REGRESSION: '+$Name+' expected pass='+$ExpectedPass+'; '+$failure)}
    $edCityLabelCases.Add([ordered]@{name=$Name;expected=if($ExpectedPass){'accept'}else{'reject'};passed=$true;rejection=$failure})
}
foreach($edScale in @(1.0,.8)) {
    Invoke-CityLabelCase ('accept synthetic short city with inset safe area at '+$edScale) (New-SyntheticCityLabelState 184 52 $edScale) $true
    Invoke-CityLabelCase ('accept synthetic long city with two-line name at '+$edScale) (New-SyntheticCityLabelState 304 72 $edScale) $true
}
$edCityLabelState=New-SyntheticCityLabelState
$edMutation=Copy-CityLabelState $edCityLabelState;$edMutation.targets=@()
Invoke-CityLabelCase 'accept fully culled view without inventing an always-visible city ID' $edMutation $true
$edMutation=Copy-CityLabelState $edCityLabelState;$edMutation.mapCityLabelDesignReady=$false
Invoke-CityLabelCase 'reject absent formal exported components' $edMutation $false
$edMutation=Copy-CityLabelState $edCityLabelState;$edMutation.mapCityLabelTextFits=$false
Invoke-CityLabelCase 'reject measured Chinese name or ownership overflow' $edMutation $false
$edMutation=Copy-CityLabelState $edCityLabelState;(Get-ExperienceTarget $edMutation 'city-label-731').h=29
Invoke-CityLabelCase 'reject old undersized city strip below touch minimum' $edMutation $false
$edMutation=Copy-CityLabelState $edCityLabelState;(Get-ExperienceTarget $edMutation 'city-label-name-731').y+=1
Invoke-CityLabelCase 'reject one pixel name geometry drift' $edMutation $false
$edMutation=Copy-CityLabelState $edCityLabelState;(Get-ExperienceTarget $edMutation 'city-label-detail-731').y=(Get-ExperienceTarget $edMutation 'city-label-name-731').y
Invoke-CityLabelCase 'reject ownership overlapping full name' $edMutation $false
$edMutation=Copy-CityLabelState $edCityLabelState;(Get-ExperienceTarget $edMutation 'city-label-art-731').w=18
Invoke-CityLabelCase 'reject fake small image target instead of actual ScaleToFit image' $edMutation $false
$edMutation=Copy-CityLabelState $edCityLabelState;(Get-ExperienceTarget $edMutation 'city-label-art-731').x+=3
Invoke-CityLabelCase 'reject image off its inspected seal center' $edMutation $false
$edMutation=Copy-CityLabelState $edCityLabelState;(Get-ExperienceTarget $edMutation 'capital-label-731').x+=1
Invoke-CityLabelCase 'reject capital alias differing from real command' $edMutation $false
$edMutation=Copy-CityLabelState $edCityLabelState;$edMutation.targets=@($edMutation.targets | Where-Object {$_.id -ne 'city-label-name-731'})
Invoke-CityLabelCase 'reject missing actual name drawing' $edMutation $false
$edMutation=Copy-CityLabelState $edCityLabelState;$edMutation.targets+=Copy-CityLabelState (Get-ExperienceTarget $edMutation 'city-label-name-731')
Invoke-CityLabelCase 'reject duplicate actual name target' $edMutation $false
$edMutation=Copy-CityLabelState $edCityLabelState;$edMutation.targets=@($edMutation.targets | Where-Object {$_.id -ne 'city-label-731'})
Invoke-CityLabelCase 'reject orphan child regions after parent culling' $edMutation $false
$edMutation=Copy-CityLabelState $edCityLabelState;$edMutation.modal=$true
Invoke-CityLabelCase 'reject unobstructed-map claim beneath a modal' $edMutation $false
$edModal=[pscustomobject]@{mapHudVisible=$false;minimapDesignStyle='';targets=@()}
Invoke-CityLabelCase 'accept modal with no background label targets' $edModal $true $true
foreach($edHiddenId in @('city-label-731','capital-label-731','city-label-art-731','city-label-name-731','city-label-detail-731')) {
    $edMutation=Copy-CityLabelState $edModal;$edMutation.targets=@([pscustomobject]@{id=$edHiddenId;x=600;y=500;w=100;h=52})
    Invoke-CityLabelCase ('reject covered label target leak '+$edHiddenId) $edMutation $false $true
}
$edCityLabelResult=[ordered]@{status='PASSED';checkedUtc=[DateTime]::UtcNow.ToString('o');caseCount=$edCityLabelCases.Count;scope='Pure PowerShell assertion regression on explicitly synthetic geometry; no new Unity/client, screenshot or pointer evidence.';cases=@($edCityLabelCases.ToArray())}
if($OutputDirectory){New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null;[IO.File]::WriteAllText((Join-Path $OutputDirectory 'map-city-label-experience-regression.json'),($edCityLabelResult | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))}
Write-Output ('PASS: '+$edCityLabelCases.Count+' synthetic city-label geometry and modal assertion regressions; no desktop interaction.')
