param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$edQuestRoot=Split-Path -Parent $PSScriptRoot
$edQuestTokens=$null;$edQuestErrors=$null
$edQuestAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $edQuestRoot 'Tools/Check-Experience.ps1'),[ref]$edQuestTokens,[ref]$edQuestErrors)
if($edQuestErrors.Count){throw ($edQuestErrors|Out-String)}
# Only import assertions, never desktop/input/client execution.
foreach($edQuestName in @('Assert-Experience','Get-ExperienceTarget','Test-ExperienceLandContains','Test-ExperienceLandOverlap','Assert-ExperienceMapQuest')) {
    $fn=@($edQuestAst.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst]},$true)|Where-Object Name -eq $edQuestName)
    if($fn.Count -ne 1){throw ('Missing assertion '+$edQuestName)}
    . ([scriptblock]::Create($fn[0].Extent.Text))
}
$edExperienceChecks=[Collections.Generic.List[string]]::new()
$edQuestCases=[Collections.Generic.List[object]]::new()
function New-QuestState([double]$Unit,[string]$Mode){
    $action=if($Mode -eq 'complete'){'hud-quest-history'}elseif($Mode -eq 'ready'){'quest-claim'}else{'quest-go'}
    $id=if($Mode -eq 'complete'){0}else{1}
    $targets=@(foreach($r in @(@('hud-quest-panel',20,90,354,118),@('hud-quest-art',20,90,80,68),@('quest-open',20,90,354,68),@('hud-quest-body',110,90,264,68),@('hud-quest-chapter',120,90,244,18),@('hud-quest-title',120,108,244,24),@('hud-quest-objective',120,132,244,26),@('hud-quest-progress',20,164,212,44),@($action,242,164,132,44),@('hud-army-panel',20,424,354,302),@('quest-land-return',20,278,268,40))){
        [pscustomobject]@{id=$r[0];x=56+($r[1]+$r[3]*.5)*$Unit;y=12+($r[2]+$r[4]*.5)*$Unit;w=$r[3]*$Unit;h=$r[4]*$Unit}
    })
    [pscustomobject]@{mapQuestStyle='youzhou-quest-sleeve-v4';mapQuestArtKind='task-book';mapQuestTextFits=$true;mapQuestId=$id;questCurrentId=$id;mapQuestReady=($Mode -eq 'ready');mapQuestTarget=10;mapQuestProgress=$(if($Mode -eq 'active'){0}else{10});mapHudTouchMinimum=44*$Unit;mapHudSafeX=56;mapHudSafeY=12;mapHudSafeWidth=1804*$Unit;mapHudSafeHeight=820*$Unit;targets=$targets}
}
function Check-QuestCase([string]$Name,$State,[bool]$Pass){
    $failed=$false
    try{Assert-ExperienceMapQuest $State}catch{$failed=$true}
    if($failed -eq $Pass){throw ('Unexpected quest assertion result: '+$Name)}
    $edQuestCases.Add([pscustomobject]@{name=$Name;expectedPass=$Pass;passed=$true})
}
foreach($unit in @(1.0,0.833333,0.72)){
    foreach($mode in @('active','ready','complete')){Check-QuestCase ('valid-'+$unit+'-'+$mode) (New-QuestState $unit $mode) $true}
    foreach($fault in @('old-style','old-banner','old-docket','sleeve-over-book','chapter-clipped','objective-clipped','art-over-title','missing-book','duplicate-action','wrong-task','font-overflow','progress-overflow')){
        $s=New-QuestState $unit 'active'
        switch($fault){
            'old-style'{$s.mapQuestStyle='youzhou-quest-design-v2'}
            'old-banner'{($s.targets|Where-Object id -eq 'hud-quest-art').h=120*$unit}
            'old-docket'{($s.targets|Where-Object id -eq 'hud-quest-panel').h=132*$unit}
            'sleeve-over-book'{($s.targets|Where-Object id -eq 'hud-quest-body').x=($s.targets|Where-Object id -eq 'hud-quest-art').x}
            'chapter-clipped'{($s.targets|Where-Object id -eq 'hud-quest-chapter').w=280*$unit}
            'objective-clipped'{($s.targets|Where-Object id -eq 'hud-quest-objective').w=355*$unit}
            'art-over-title'{($s.targets|Where-Object id -eq 'hud-quest-art').x=($s.targets|Where-Object id -eq 'hud-quest-title').x}
            'missing-book'{$s.targets=@($s.targets|Where-Object id -ne 'hud-quest-art')}
            'duplicate-action'{$s.targets+=([pscustomobject]@{id='quest-claim';x=200;y=200;w=132*$unit;h=44*$unit})}
            'wrong-task'{$s.questCurrentId=2}
            'font-overflow'{$s.mapQuestTextFits=$false}
            'progress-overflow'{$s.mapQuestProgress=11}
        }
        Check-QuestCase ($fault+'-'+$unit) $s $false
    }
}
if($OutputDirectory){
    [IO.Directory]::CreateDirectory($OutputDirectory)|Out-Null
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'map-quest-experience.json'),([pscustomobject]@{status='passed';cases=$edQuestCases;runtimeVerified=$false}|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
}
Write-Output ('PASS: '+$edQuestCases.Count+' task docket external acceptance cases; actual clicks remain publisher work.')
