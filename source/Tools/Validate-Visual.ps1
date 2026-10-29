param([ValidateRange(640,7680)][int]$PixelWidth=1440,[ValidateRange(480,4320)][int]$PixelHeight=900,[ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]*$')][string]$Tag='youzhou-40',[switch]$Legacy,[switch]$MobileReview,[ValidateRange(45,180)][int]$TimeoutSeconds=120)
$ErrorActionPreference='Stop'
function Wait-EdVisualSmokeExit($Process,[int]$TimeoutMilliseconds) {
    if($TimeoutMilliseconds -le 0){throw 'Visual smoke wait must have a positive timeout.'}
    if($Process.WaitForExit($TimeoutMilliseconds)){return $true}
    # The player can finish while WaitForExit reaches its boundary. Recheck the
    # same owned process; never use its completion marker as a substitute for exit.
    $Process.Refresh()
    return [bool]$Process.HasExited
}
. (Join-Path $PSScriptRoot 'Common.ps1')
$edRoot=Split-Path -Parent $PSScriptRoot
if(@(Get-CimInstance Win32_Process -Filter "Name='EasternDynasty.exe'").Count -gt 0){throw 'PLAYER_IN_USE: Keep the existing player intact and defer visual checks.'}
$edArtifacts=Join-Path $edRoot ('Validation\Visual\'+$Tag+'-'+$PixelWidth+'x'+$PixelHeight)
New-Item -ItemType Directory -Path $edArtifacts -Force | Out-Null
$edLog=Join-Path $edArtifacts 'player.log'
$edStart=Get-Date
$edArgs=@('-edSmoke','-screen-fullscreen','0','-screen-width',"$PixelWidth",'-screen-height',"$PixelHeight",'-logFile',$edLog)
if($Legacy){$edArgs+='-edLegacy'}
if($MobileReview){$edArgs+='-edMobileReview'}
$edPlayer=Start-Process -FilePath (Join-Path $edRoot 'Builds\Windows\EasternDynasty.exe') -ArgumentList (ConvertTo-EdProcessArguments $edArgs) -WindowStyle Normal -PassThru
# The S1 suite now captures more than 190 states and rebuilds isolated worlds.
# A 45-second run already reached SMOKE_OK but raced shutdown in publication
# s1-20261028-162252252. Keep a bounded budget without dropping any evidence gate.
if(-not (Wait-EdVisualSmokeExit $edPlayer ($TimeoutSeconds*1000))) {
    $edTimeoutLog=if(Test-Path -LiteralPath $edLog){Get-Content -LiteralPath $edLog -Raw -Encoding UTF8}else{''}
    $edTimeout=[ordered]@{status='timed_out';timeoutSeconds=$TimeoutSeconds;elapsedSeconds=((Get-Date)-$edStart).TotalSeconds;processId=$edPlayer.Id;playerLog=$edLog;completionMarkerObserved=$edTimeoutLog.Contains('EASTERN_DYNASTY_RUNTIME_SMOKE_OK');lastLogLines=@($edTimeoutLog -split '\r?\n' | Select-Object -Last 12)}
    [IO.File]::WriteAllText((Join-Path $edArtifacts 'timeout.json'),($edTimeout | ConvertTo-Json -Depth 4),[Text.UTF8Encoding]::new($false))
    # This is solely the read-only validation process started above. If it exits
    # during evidence writing, do not turn that race into a Stop-Process error.
    $edPlayer.Refresh()
    if(-not $edPlayer.HasExited){Stop-Process -Id $edPlayer.Id -Force -ErrorAction SilentlyContinue}
    throw ('Visual smoke timed out after '+$TimeoutSeconds+' seconds. Check player.log and timeout.json.')
}
$edPlayer.Refresh()
if($edPlayer.ExitCode -ne 0){throw ('Player failed: '+$edPlayer.ExitCode)}
$edPersist=Join-Path $env:USERPROFILE 'AppData\LocalLow\EasternDynasty\EasternDynasty'
$edCaptures=Get-ChildItem -LiteralPath $edPersist -Filter 'smoke*.png' | Where-Object {$_.LastWriteTime -ge $edStart.AddSeconds(-1)}
$edExpected=@('smoke-welcome.png','smoke-overview.png','smoke-land.png','smoke-city.png','smoke-march.png','smoke.png','smoke-roster.png','smoke-recruitment.png','smoke-report.png','smoke-detail.png','smoke-result.png','smoke-result-placeholder.png')
if(-not $Legacy){$edExpected+=@('smoke-map-far.png','smoke-river.png','smoke-resources.png')}
if(-not $Legacy){$edExpected+=@('smoke-hud-long-values.png','smoke-hud-empty-formation.png')}
if(-not $Legacy){$edExpected+=@('smoke-hud-command-cards.png')}
if(-not $Legacy){$edExpected+=@('smoke-map-quest-active.png','smoke-map-quest-ready.png','smoke-map-quest-army.png','smoke-map-quest-complete.png')}
if(-not $Legacy){$edExpected+=@('smoke-land-owned.png','smoke-land-busy.png','smoke-land-blocked.png','smoke-land-design.png')}
if(-not $Legacy){$edExpected+=@('smoke-land-flanking.png','smoke-land-edge-dock.png')}
if(-not $Legacy){$edExpected+=@('smoke-land-orders.png','smoke-land-city-orders.png')}
if(-not $Legacy){$edExpected+=@('smoke-land-facts.png')}
if(-not $Legacy){$edExpected+=@('smoke-land-grain.png','smoke-land-pasture.png','smoke-land-stone.png','smoke-land-iron.png')}
if(-not $Legacy){$edExpected+=@('smoke-s1-garrison-march.png','smoke-s1-garrison.png','smoke-s1-return.png','smoke-s1-farm-march.png','smoke-s1-farming.png','smoke-s1-archive.png','smoke-s1-wei.png','smoke-s1-draw.png')}
if(-not $Legacy){$edExpected+=@('smoke-s1-archive-scrolled.png','smoke-s1-archive-recruitable.png','smoke-s1-archive-empty.png')}
if(-not $Legacy){$edExpected+=@('smoke-s1-archive-design.png','smoke-s1-archive-long-name.png','smoke-s1-archive-long-skill.png')}
if(-not $Legacy){$edExpected+=@('smoke-owned-roster-scrolled.png','smoke-owned-roster-empty-formation.png')}
if(-not $Legacy){$edExpected+=@('smoke-minimap-atlas.png','smoke-minimap-edge.png')}
if(-not $Legacy){$edExpected+=@('smoke-terrain-close.png','smoke-terrain-river.png','smoke-terrain-wide.png')}
if(-not $Legacy){$edExpected+=@('smoke-river-crossing-close.png','smoke-river-bank-bend.png')}
if(-not $Legacy){$edExpected+=@('smoke-card-full.png','smoke-card-skill.png','smoke-city-overview.png','smoke-city-developed.png','smoke-city-district.png','smoke-city-construction.png','smoke-city-quests.png','smoke-city-quests-completed.png','smoke-city-map.png')}
if(-not $Legacy){$edExpected+=@('smoke-city-overview-long-name.png','smoke-city-overview-empty-formation.png','smoke-city-panorama-toolbar.png')}
if(-not $Legacy){$edExpected+=@('smoke-city-affairs-working.png')}
if(-not $Legacy){$edExpected+=@('smoke-city-affairs-design.png')}
if(-not $Legacy){$edExpected+=@('smoke-city-dock-design.png')}
if(-not $Legacy){$edExpected+=@('smoke-city-header-design.png')}
if(-not $Legacy){$edExpected+=@('smoke-city-header-ledger.png','smoke-city-header-ordinary.png','smoke-city-header-ordinary-return.png')}
if(-not $Legacy){$edExpected+=@('smoke-city-footer-design.png')}
if(-not $Legacy){$edExpected+=@('smoke-city-footer-captions.png','smoke-hud-navigation-captions.png')}
if(-not $Legacy){$edExpected+=@('smoke-hud-recruit-seal.png')}
if(-not $Legacy){$edExpected+=@('smoke-city-army-away.png')}
if(-not $Legacy){$edExpected+=@('smoke-city-army-design.png')}
if(-not $Legacy){$edExpected+=@('smoke-city-neighbourhoods.png')}
if(-not $Legacy){$edExpected+=@('smoke-city-garden-shore.png','smoke-city-river-grove.png')}
if(-not $Legacy){$edExpected+=@('smoke-city-gate-masonry.png')}
if(-not $Legacy){$edExpected+=@('smoke-card-owned.png','smoke-card-archive-source.png','smoke-card-archive-scrolled.png','smoke-card-long-name.png')}
if(-not $Legacy){$edExpected+=@('smoke-card-skill-long.png','smoke-card-skill-scrolled.png')}
if(-not $Legacy){$edExpected+=@('smoke-card-skill-sheet.png','smoke-card-skill-sheet-owned.png')}
if(-not $Legacy){$edExpected+=@('smoke-card-low-rarity.png')}
if(-not $Legacy){$edExpected+=@('smoke-card-design.png')}
if(-not $Legacy){$edExpected+=@('smoke-card-dossier-design.png')}
if(-not $Legacy){$edExpected+=@('smoke-card-dossier-long-skill.png')}
if(-not $Legacy){$edExpected+=@('smoke-card-chrome.png','smoke-card-chrome-owned.png')}
if(-not $Legacy){$edExpected+=@('smoke-training-no-land.png','smoke-training-ready.png','smoke-training-march.png','smoke-training-working.png','smoke-training-completed.png','smoke-training-cancelled.png')}
if(-not $Legacy){$edExpected+=@('smoke-training-targets.png','smoke-training-targets-gain.png','smoke-training-targets-capped.png')}
if(-not $Legacy){$edExpected+=@('smoke-training-history-empty.png','smoke-training-history-completed.png','smoke-training-history-cancelled.png','smoke-training-history-archive.png','smoke-training-history-updated.png')}
if(-not $Legacy){$edExpected+=@('smoke-battle-replay-start.png','smoke-battle-replay-event.png','smoke-battle-replay-result.png','smoke-battle-legacy-report.png')}
if(-not $Legacy){$edExpected+=@('smoke-battle-replay-loss.png','smoke-battle-replay-details.png','smoke-battle-replay-recovery.png','smoke-battle-replay-defeat.png')}
if(-not $Legacy){$edExpected+=@('smoke-battle-waiting.png','smoke-battle-current.png')}
if(-not $Legacy){$edExpected+=@('smoke-battle-outcome.png','smoke-battle-outcome-recovery.png','smoke-battle-outcome-defeat.png','smoke-battle-outcome-capped.png','smoke-battle-outcome-current.png')}
if(-not $Legacy){$edExpected+=@('smoke-battle-formation.png','smoke-battle-formation-learned.png','smoke-battle-formation-outcome.png','smoke-battle-formation-current.png')}
if(-not $Legacy){$edExpected+=@('smoke-report-archive-all.png','smoke-report-archive-captured.png','smoke-report-archive-not-captured.png','smoke-report-archive-legacy.png','smoke-report-archive-empty.png')}
if(-not $Legacy){$edExpected+=@('smoke-facility-locked.png','smoke-facility-shortage.png','smoke-facility-existing.png','smoke-facility-upgrade.png','smoke-facility-maximum.png')}
if(-not $Legacy){$edExpected+=@('smoke-facility-dossier.png','smoke-facility-dossier-working.png')}
if(-not $Legacy){$edExpected+=@('smoke-facility-sheet-design.png')}
if(-not $Legacy){$edExpected+=@('smoke-facility-gallery-design.png')}
if(-not $Legacy){$edExpected+=@('smoke-facility-refund.png','smoke-quest-construction.png','smoke-quest-current.png','smoke-quest-land.png')}
if(-not $Legacy){$edExpected+=@('smoke-quest-reward.png','smoke-quest-chapter-complete.png','smoke-quest-all-complete.png')}
if(-not $Legacy){$edExpected+=@('smoke-quest-objective-active.png','smoke-quest-objective-locked.png','smoke-quest-objective-working.png','smoke-quest-objective-ready.png','smoke-quest-objective-claimed.png','smoke-quest-objective-land.png')}
if(-not $Legacy){$edExpected+=@('smoke-reinforcement-ready.png','smoke-reinforcement-confirm.png','smoke-reinforcement-shortage.png','smoke-reinforcement-completed.png','smoke-reinforcement-quest.png','smoke-reinforcement-full.png','smoke-reinforcement-busy.png')}
if(-not $Legacy){$edExpected+=@('smoke-facility-catalog.png','smoke-facility-catalog-military.png','smoke-facility-catalog-economy.png','smoke-facility-catalog-working.png','smoke-facility-catalog-built.png')}
if(-not $Legacy){$edExpected+=@('smoke-facility-catalog-developed.png','smoke-hud-design-navigation.png')}
if(-not $Legacy){$edExpected+=@('smoke-facility-catalog-design.png','smoke-facility-catalog-maximum.png')}
if(-not $Legacy){$edExpected+=@('smoke-hud-design-header.png','smoke-hud-design-quest.png','smoke-hud-quest-docket.png','smoke-hud-quest-sleeve.png')}
if(-not $Legacy){$edExpected+=@('smoke-hud-header-ledger.png')}
if(-not $Legacy){$edExpected+=@('smoke-map-city-markers.png','smoke-map-city-markers-long.png')}
if(-not $Legacy){$edExpected+=@('smoke-hud-design-army.png','smoke-hud-design-army-rows.png')}
if(-not $Legacy){$edExpected+=@('smoke-hud-design-minimap.png','smoke-minimap-sidebar.png')}
if(-not $Legacy){$edExpected+=@('smoke-city-planning.png','smoke-city-planning-detail.png','smoke-city-planning-working.png','smoke-city-planning-built.png')}
if(-not $Legacy){$edExpected+=@('smoke-city-expanded.png','smoke-city-zoomed.png','smoke-city-zoomed-planning.png','smoke-city-expanded-detail.png','smoke-city-expanded-developed.png')}
if(-not $Legacy){$edExpected+=@('smoke-construction-idle.png','smoke-construction-working.png','smoke-construction-completed.png','smoke-construction-upgrading.png','smoke-construction-offline.png')}
if($edCaptures.Count -ne $edExpected.Count){throw ('Expected '+$edExpected.Count+' new captures, received '+$edCaptures.Count)}
foreach($edName in $edExpected){if($edName -notin $edCaptures.Name){throw ('Missing visual state: '+$edName)}}
Add-Type -AssemblyName System.Drawing
foreach($edCapture in $edCaptures){
    $edBitmap=[Drawing.Image]::FromFile($edCapture.FullName)
    try {if($edBitmap.Width -ne $PixelWidth -or $edBitmap.Height -ne $PixelHeight){throw ('Capture size mismatch: '+$edCapture.Name+' '+$edBitmap.Width+'x'+$edBitmap.Height+' expected '+$PixelWidth+'x'+$PixelHeight)}} finally {$edBitmap.Dispose()}
}
$edCaptures | Copy-Item -Destination $edArtifacts
Copy-Item -LiteralPath (Join-Path $edPersist 'visual-metrics.txt') -Destination $edArtifacts
$edLogText=Get-Content -LiteralPath $edLog -Raw -Encoding UTF8
if(-not $edLogText.Contains('EASTERN_DYNASTY_RUNTIME_SMOKE_OK')){throw 'Runtime smoke completion marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('TRAINING_RUNTIME_OK')){throw 'Independent training runtime completion marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CARD_VISUAL_RUNTIME_OK')){throw 'Card identity, source scrolling and unchanged campaign runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CARD_CHROME_RUNTIME_OK')){throw 'Card compact Inkscape navigation, real font fit, disjoint visible actions and expanded content runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CARD_DOSSIER_RUNTIME_OK')){throw 'Card dossier visible skill slots, safe area and long skill scrolling runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CARD_SKILL_SHEET_RUNTIME_OK')){throw 'Card skill Inkscape sheet, imported image, actual font and separated complete description runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CARD_DOSSIER_DESIGN_RUNTIME_OK')){throw 'Card dossier controlled Inkscape symbols, actual Chinese font fit, inline values/growth and horizontal image/name skill geometry runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CARD_PORTRAIT_RUNTIME_OK')){throw 'Card portrait aspect, name pennant, rarity crown, owned base and safe-area runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CARD_PORTRAIT_DESIGN_RUNTIME_OK')){throw 'Inkscape portrait column geometry, actual name/star font fit and complete-source runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('ARCHIVE_VISUAL_RUNTIME_OK')){throw 'Archive scope/search, fully visible card targets, nonzero scroll return and unchanged campaign runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('ARCHIVE_DESIGN_RUNTIME_OK')){throw 'Archive compact Inkscape cards, actual fonts, two complete rows, longest name and modal isolation runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('ARCHIVE_TEXT_EXTREMES_RUNTIME_OK')){throw 'Archive actual longest catalog name and skill font/visible-identity runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('OWNED_ROSTER_VISUAL_RUNTIME_OK')){throw 'Owned roster identity, safe-area targets, modal suppression, nonzero scroll return and unchanged campaign runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MAP_HUD_VISUAL_RUNTIME_OK')){throw 'Map HUD safe-area, visible targets and isolated stress states runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MAP_COMMAND_VISUAL_RUNTIME_OK')){throw 'Map illustrated navigation, full portrait regions and separate touch commands runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MAP_HUD_DESIGN_RUNTIME_OK')){throw 'Reference-led exported navigation skins and visible title/image targets runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('NAVIGATION_CAPTIONS_RUNTIME_OK')){throw 'Shared image-above-horizontal-caption navigation, exact geometry and actual Chinese font fit runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MAP_RECRUIT_SEAL_RUNTIME_OK')){throw 'Map-only raised recruitment seal, exact exported textures and portrait/title geometry, actual Chinese font fit and complete raised-region input blocking runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MAP_HEADER_DESIGN_RUNTIME_OK')){throw 'Exported header, actual-font fit and safe-area camera coverage marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MAP_HEADER_LEDGER_RUNTIME_OK')){throw 'Map header ledger exported artwork, independent identity, stock/income geometry and actual Chinese font fit runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MAP_QUEST_DESIGN_RUNTIME_OK')){throw 'Exported compact task banner, exact design dimensions, actual-font fit and safe-area marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MAP_QUEST_DOCKET_RUNTIME_OK')){throw 'Exported task docket, book entrance, full-width objective and independent action runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MAP_QUEST_SLEEVE_RUNTIME_OK')){throw 'Detached task book, actual exported sleeve textures, live-text fit and compact safe-area runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MAP_CITY_MARKER_DESIGN_RUNTIME_OK')){throw 'City marker design, actual font fit and ground-pick visibility marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MAP_ARMY_DESIGN_RUNTIME_OK')){throw 'Exported horizontal army rows, exact design dimensions, actual-font fit and isolated empty/long-name fixture marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MAP_ARMY_ROWS_RUNTIME_OK')){throw '64-high horizontal army rows, complete portrait containers, separated identities and protected gaps marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MAP_QUEST_CHARTER_RUNTIME_OK')){throw 'Map task charter actual progress, illustration, touch regions and completion runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MINIMAP_ATLAS_RUNTIME_OK')){throw 'Minimap terrain atlas, actual camera footprint and edge clipping runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MINIMAP_DESIGN_RUNTIME_OK')){throw 'Exported floating minimap design, actual-font fit, exact visible geometry and isolated camera navigation marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MINIMAP_SIDEBAR_RUNTIME_OK')){throw 'Full minimap beside exported side navigation, actual assets, font fit and 282x204 visible layout marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_ARMY_DESIGN_RUNTIME_OK')){throw 'City army Inkscape assets, compact full portraits, actual fonts and design geometry marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('MAP_TERRAIN_RUNTIME_OK')){throw 'Resource terrain geometry, source UVs and near/river/wide runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('RIVER_BANKS_RUNTIME_OK')){throw 'River bank distance texture, actual crossing structures and water/bridge picking runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('LAND_VISUAL_RUNTIME_OK')){throw 'Land panel geometry, touch targets, ownership/order states, modal suppression and unchanged campaign runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('LAND_WARRANT_RUNTIME_OK')){throw 'Land warrant image/identity separation, factual rows, command icons and four real resource screenshots runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('LAND_FLOATING_DESIGN_RUNTIME_OK')){throw 'Land floating design compact inline commands, actual font fit, label geometry and design screenshot runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('LAND_FLANKING_DESIGN_RUNTIME_OK')){throw 'Land flanking surfaces, readable font, open map gap and edge docking runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('LAND_DETACHED_ORDERS_RUNTIME_OK')){throw 'Detached land orders, actual exported skins, full labels and state-specific geometry runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('LAND_COMPACT_FACTS_RUNTIME_OK')){throw 'Compact land facts exported skin, complete enlarged terrain image, exact full-width rows and actual fact label/value fonts runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('TRAINING_HISTORY_RUNTIME_OK')){throw 'Training history read-only navigation marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('TRAINING_TARGETS_RUNTIME_OK')){throw 'Training comparison and exact capped forecast runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('TRAINING_EXECUTION_RUNTIME_OK')){throw 'Training execution phase and credit/home countdown runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_PAGES_RUNTIME_OK')){throw 'Independent city and quest pages runtime completion marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_DEVELOPED_RUNTIME_OK')){throw 'Developed city visual fixture completion marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_FACILITIES_RUNTIME_OK')){throw 'Facility detail runtime completion marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_FACILITY_DOSSIER_RUNTIME_OK')){throw 'Facility portrait, compact district, image selectors and first-screen resource cost runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_FACILITY_SHEET_DESIGN_RUNTIME_OK')){throw 'Facility Inkscape sheet, actual fonts and large inventory runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_FACILITY_GALLERY_RUNTIME_OK')){throw 'Facility rectangular gallery, complete illustration, actual font fit and visible image/name target runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_CATALOG_RUNTIME_OK')){throw 'Facility catalog navigation runtime completion marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_CATALOG_ATLAS_RUNTIME_OK')){throw 'Facility atlas image, prerequisite, touch action, scrolled built nodes and modal isolation runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_CATALOG_DESIGN_RUNTIME_OK')){throw 'Facility atlas inspected Inkscape geometry, exported assets, actual Chinese fonts and complete horizontal image/name/action runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_PLANNING_RUNTIME_OK')){throw 'Panorama construction site navigation runtime completion marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_PANORAMA_RUNTIME_OK')){throw 'Expanded city panorama, aspect and navigation runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_PANORAMA_TOOLBAR_RUNTIME_OK')){throw 'Inkscape panorama toolbar runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_STREETSCAPE_RUNTIME_OK')){throw 'City streetscape, real facility targets and unchanged campaign runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_LANDSCAPE_RUNTIME_OK')){throw 'City landscape viewport coverage, town origin alignment and unchanged campaign runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_SCENERY_DETAIL_RUNTIME_OK')){throw 'City garden/river/grove captures, cached artwork and unchanged World runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_GATE_MASONRY_RUNTIME_OK')){throw 'City gate masonry capture, visible source-art bounds, cached artwork and unchanged World runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_OVERVIEW_VISUAL_RUNTIME_OK')){throw 'Full-height city panorama, side-rail geometry, vertical portraits, empty formation and unchanged campaign runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_AFFAIRS_COMPOSITION_RUNTIME_OK')){throw 'City facility medallion, live queue/level, separate portrait identity, chapter scroll and illustrated navigation touch bounds runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_AFFAIRS_DESIGN_RUNTIME_OK')){throw 'City affairs Inkscape assets, separate building/caption and chapter regions, actual fonts and exact design geometry runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_ARMY_COMPOSITION_RUNTIME_OK')){throw 'City army standards, strength and exact supply costs, illustrated action geometry, idle/empty/real away states and unchanged World runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_SHELL_RUNTIME_OK')){throw 'City crest, resource ledger, illustrated navigation and full landscape canvas runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_HEADER_DESIGN_RUNTIME_OK')){throw 'City header Inkscape assets, complete wrapped identity, actual fonts, independent stock/income regions and isolated large-value fixture runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_FOOTER_DESIGN_RUNTIME_OK')){throw 'City footer six imported Inkscape components, exact shared ticket geometry, all eight non-overlapping controls, actual Chinese fonts and complete shelf input blocking runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_EDGE_DOCK_RUNTIME_OK')){throw 'City edge navigation dock, raised panorama, visible art/captions and return geometry runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_DOCK_DESIGN_RUNTIME_OK')){throw 'City dock Inkscape assets, exact design geometry, actual Chinese font fit and separate navigation/return image and caption runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_FACILITY_PLAQUES_RUNTIME_OK')){throw 'City facility name ribbons, touch targets, non-overlap and actual facility identity runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_CONSTRUCTION_RUNTIME_OK')){throw 'Construction progress and completion runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_QUEST_NAVIGATION_RUNTIME_OK')){throw 'Actionable quest navigation runtime completion marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_QUEST_REWARD_RUNTIME_OK')){throw 'Actual quest reward receipt runtime completion marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('CITY_QUEST_OBJECTIVE_RUNTIME_OK')){throw 'Quest objective detail and real action runtime completion marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('REINFORCEMENT_RUNTIME_OK')){throw 'Independent reinforcement runtime completion marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('REINFORCEMENT_JSON_RUNTIME_OK')){throw 'Fractional reinforcement JSON roundtrip marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('BATTLE_REPLAY_RUNTIME_OK')){throw 'Frozen battle replay runtime completion marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('BATTLE_PRESENTATION_RUNTIME_OK')){throw 'Recorded battlefield and event detail runtime completion marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('BATTLE_WATCH_RUNTIME_OK')){throw 'Arrival battle watching runtime completion marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('BATTLE_OUTCOME_RUNTIME_OK')){throw 'Frozen battle outcome and layered return runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('BATTLE_FORMATION_RUNTIME_OK')){throw 'Frozen battle formation and layered return runtime marker missing.'}
if(-not $Legacy -and -not $edLogText.Contains('BATTLE_REPORT_ARCHIVE_RUNTIME_OK')){throw 'Battle report filtering and return runtime marker missing.'}
if($edLogText -match '(?im)^ERROR:|^Shader error|Exception:|^Assertion failed'){throw 'Player log contains a runtime or shader error.'}
Write-Output ('PASS: '+$edExpected.Count+' visual states at '+$PixelWidth+'x'+$PixelHeight+'; '+$edArtifacts)
Get-Content -LiteralPath (Join-Path $edArtifacts 'visual-metrics.txt') -Encoding UTF8
