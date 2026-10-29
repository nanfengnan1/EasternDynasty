param([string]$UnityData)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$edRoot = Split-Path -Parent $PSScriptRoot
$edBin = Join-Path $edRoot 'Validation\bin'
New-Item -ItemType Directory -Path $edBin -Force | Out-Null
$edCompiler = Join-Path $UnityData 'MonoBleedingEdge\lib\mono\4.5\csc.exe'
$edMono = Join-Path $UnityData 'MonoBleedingEdge\bin\mono.exe'
$edModel = @(Get-ChildItem -LiteralPath (Join-Path $edRoot 'Assets\EasternDynasty\Scripts\Core') -Filter '*.cs' | ForEach-Object { $_.FullName })
$edChecks = Join-Path $edRoot 'Validation\CoreChecks.cs'
$edCheckExe = Join-Path $edBin 'CoreChecks.exe'
& $edMono $edCompiler /nologo /target:exe /out:$edCheckExe $edModel $edChecks
if ($LASTEXITCODE -ne 0) { throw 'Core checks failed to compile.' }
& $edMono $edCheckExe
if ($LASTEXITCODE -ne 0) { throw 'Core checks failed.' }
$edYouzhouExe = Join-Path $edBin 'YouzhouChecks.exe'
$edYouzhouChecks = Join-Path $edRoot 'Validation\YouzhouChecks.cs'
$edLegacyFixtures = Join-Path $edRoot 'Validation\LegacySerializationFixtures.cs'
# Frozen Unity v1 JSON lives in test source so clean publication snapshots can run
# before an Editor has generated Validation/serialization-*.json.
& $edMono $edCompiler /nologo /target:exe /r:System.Runtime.Serialization.dll /out:$edYouzhouExe $edModel $edYouzhouChecks $edLegacyFixtures
if ($LASTEXITCODE -ne 0) { throw 'Youzhou checks failed to compile.' }
& $edMono $edYouzhouExe $edRoot
if ($LASTEXITCODE -ne 0) { throw 'Youzhou checks failed.' }
$edApplication=@(Get-ChildItem -LiteralPath (Join-Path $edRoot 'Assets\EasternDynasty\Scripts\Application') -Filter '*.cs' -Recurse | ForEach-Object {$_.FullName})
$edArchitectureExe=Join-Path $edBin 'ArchitectureChecks.exe'
& $edMono $edCompiler /nologo /target:exe /out:$edArchitectureExe $edModel $edApplication (Join-Path $edRoot 'Validation\ArchitectureChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Architecture checks failed to compile.'}
& $edMono $edArchitectureExe
if($LASTEXITCODE -ne 0){throw 'Architecture checks failed.'}

foreach($edSuite in @('S1OrderChecks','S1RosterChecks','MapInteractionChecks','CityProgressionChecks','CityFacilityNavigationChecks','CityConstructionCompletionChecks','CityQuestNavigationChecks','CityQuestRewardChecks','CityQuestObjectivePlanChecks','ReinforcementChecks','TrainingChecks','TrainingPlanPreviewChecks','TrainingExecutionChecks','BattleReplayChecks','BattleReplayFrameChecks','BattleOutcomeSummaryChecks','BattleFormationSummaryChecks','BattleReportArchiveChecks')) {
    $edSuiteSource=Join-Path $edRoot ('Validation/'+$edSuite+'.cs')
    if(-not (Test-Path -LiteralPath $edSuiteSource)){throw ('Missing S1 checks: '+$edSuiteSource)}
    $edSuiteExe=Join-Path $edBin ($edSuite+'.exe')
    $edExtras=@()
    if($edSuite -eq 'MapInteractionChecks'){$edExtras+=Join-Path $edRoot 'Assets\EasternDynasty\Scripts\Runtime\MapPointerGesture.cs'}
    if($edSuite -in @('TrainingChecks','TrainingPlanPreviewChecks','TrainingExecutionChecks','CityFacilityNavigationChecks','CityConstructionCompletionChecks','CityQuestNavigationChecks','CityQuestRewardChecks','CityQuestObjectivePlanChecks','ReinforcementChecks','BattleReplayFrameChecks','BattleOutcomeSummaryChecks','BattleFormationSummaryChecks','BattleReportArchiveChecks')){$edExtras+=$edApplication}
    & $edMono $edCompiler /nologo /target:exe /r:System.Runtime.Serialization.dll /out:$edSuiteExe $edModel $edSuiteSource $edExtras
    if($LASTEXITCODE -ne 0){throw ($edSuite+' compile failed.')}
    & $edMono $edSuiteExe
    if($LASTEXITCODE -ne 0){throw ($edSuite+' failed.')}
}

# Mobile gesture and safe-area logic remains independent of Unity and devices.
& (Join-Path $PSScriptRoot 'Check-MobileInput.ps1')
& (Join-Path $PSScriptRoot 'Check-MapHudNavigation.ps1') -UnityData $UnityData
& (Join-Path $edRoot 'Validation/MapHeaderExperienceChecks.ps1')
& (Join-Path $edRoot 'Validation/MapRecruitExperienceChecks.ps1') -OutputDirectory (Join-Path $edRoot 'Validation/MapRecruitExperience')
& node (Join-Path $edRoot 'Validation/VisualSmokeWaitChecks.mjs')
if($LASTEXITCODE -ne 0){throw 'Visual smoke wait checks failed.'}
& (Join-Path $edRoot 'Validation/MapArmyExperienceChecks.ps1')
& (Join-Path $edRoot 'Validation/MapQuestExperienceChecks.ps1')
& (Join-Path $PSScriptRoot 'Check-MapCityLabel.ps1') -UnityData $UnityData
& (Join-Path $PSScriptRoot 'Check-MapCityLabelDiagnostics.ps1') -UnityData $UnityData
& (Join-Path $edRoot 'Validation/MapCityLabelExperienceChecks.ps1')
& (Join-Path $PSScriptRoot 'Check-LandSelectionLayout.ps1') -UnityData $UnityData
& (Join-Path $PSScriptRoot 'Check-LandVisualDiagnostics.ps1') -UnityData $UnityData
& (Join-Path $PSScriptRoot 'Check-LandCommandText.ps1') -UnityData $UnityData
& (Join-Path $edRoot 'Validation/LandExperienceChecks.ps1')
& node (Join-Path $edRoot 'Validation/UiDesignChecks.mjs')
if($LASTEXITCODE -ne 0){throw 'UI design provenance checks failed.'}
& (Join-Path $PSScriptRoot 'Check-CityPanorama.ps1') -UnityData $UnityData
& (Join-Path $PSScriptRoot 'Check-CityArmyLayout.ps1') -UnityData $UnityData
& (Join-Path $PSScriptRoot 'Check-CityAffairsLayout.ps1') -UnityData $UnityData
& (Join-Path $PSScriptRoot 'Check-CityDockLayout.ps1') -UnityData $UnityData
& (Join-Path $PSScriptRoot 'Check-CityHeaderLayout.ps1') -UnityData $UnityData
& (Join-Path $edRoot 'Validation/CityHeaderExperienceChecks.ps1') -OutputDirectory (Join-Path $edRoot 'Validation/CityHeaderExperience')
& (Join-Path $PSScriptRoot 'Check-UiLineRaster.ps1') -UnityData $UnityData
& (Join-Path $PSScriptRoot 'Check-CardPortraitText.ps1') -UnityData $UnityData
& (Join-Path $PSScriptRoot 'Check-CardDossierSkill.ps1') -UnityData $UnityData
& (Join-Path $PSScriptRoot 'Check-CardDossierDesign.ps1') -UnityData $UnityData
& (Join-Path $PSScriptRoot 'Check-CardChrome.ps1') -UnityData $UnityData
& (Join-Path $edRoot 'Validation/CardChromeExperienceChecks.ps1') -OutputDirectory (Join-Path $edRoot 'Validation/CardChromeExperience')
& (Join-Path $PSScriptRoot 'Check-CityFacilitySheet.ps1') -UnityData $UnityData
& (Join-Path $PSScriptRoot 'Check-CityFacilityGallery.ps1') -UnityData $UnityData
& (Join-Path $PSScriptRoot 'Check-CityFacilityAtlas.ps1') -UnityData $UnityData
& (Join-Path $PSScriptRoot 'Check-ArchiveCardLayout.ps1') -UnityData $UnityData
# Pure replay of recorded geometry; imports assertion functions only, never desktop code.
& (Join-Path $edRoot 'Validation/ExperienceCityPanoramaChecks.ps1')
& (Join-Path $edRoot 'Validation/CityFooterExperienceChecks.ps1')
& (Join-Path $edRoot 'Validation/ExperienceCaptureChecks.ps1')
& (Join-Path $edRoot 'Validation/ReviewCaptureProtocolChecks.ps1') -OutputDirectory (Join-Path $edRoot 'Validation/ReviewCaptureProtocol')
& (Join-Path $PSScriptRoot 'Check-MinimapAtlas.ps1') -UnityData $UnityData
& (Join-Path $PSScriptRoot 'Check-MapTerrain.ps1') -UnityData $UnityData
& (Join-Path $PSScriptRoot 'Check-RiverLayout.ps1') -UnityData $UnityData

# Render the production CPU facility artwork without a Unity process, and reject clipped tiles.
& (Join-Path $PSScriptRoot 'Check-CityArt.ps1') -UnityData $UnityData

# Static compile against this exact Editor's reference assemblies. This is not an Editor import, shader test or player run.
$edReferences = @()
$edFramework = Join-Path $UnityData 'UnityReferenceAssemblies\unity-4.8-api'
foreach ($edName in @('mscorlib.dll','System.dll','System.Core.dll','Facades\netstandard.dll')) {
    $edReferences += '/reference:"' + (Join-Path $edFramework $edName) + '"'
}
Get-ChildItem -LiteralPath (Join-Path $UnityData 'Managed\UnityEngine') -Filter '*.dll' | ForEach-Object {
    $edReferences += '/reference:"' + $_.FullName + '"'
}
$edRuntimeFiles = Get-ChildItem -LiteralPath (Join-Path $edRoot 'Assets\EasternDynasty\Scripts') -Filter '*.cs' -Recurse
$edEditorFiles = Get-ChildItem -LiteralPath (Join-Path $edRoot 'Assets\EasternDynasty\Editor') -Filter '*.cs' -Recurse
$edResponse = @('/nologo','/target:library','/nostdlib+',('/out:"' + (Join-Path $edBin 'EasternDynasty.StaticCheck.dll') + '"')) + $edReferences
$edResponse += @($edRuntimeFiles + $edEditorFiles | ForEach-Object { '"' + $_.FullName + '"' })
$edResponsePath = Join-Path $edRoot 'Validation\compile.rsp'
[System.IO.File]::WriteAllLines($edResponsePath, $edResponse, [System.Text.UTF8Encoding]::new($false))
& $edMono $edCompiler ('@' + $edResponsePath)
if ($LASTEXITCODE -ne 0) { throw 'Unity reference compilation failed.' }
Write-Output 'PASS: runtime and Editor scripts compile against installed Unity references. Use the Unity build and runtime smoke checks for actual execution validation.'
