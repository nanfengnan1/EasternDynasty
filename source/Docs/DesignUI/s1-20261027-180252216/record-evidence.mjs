// Record completed reads, exports and pure validation; never a runtime acceptance.
import fs from 'node:fs';
import crypto from 'node:crypto';
const version='s1-20261027-180252216', design=`Docs/DesignUI/${version}`, validation=`Validation/CityAffairsDesign/${version}`;
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const read=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const write=(p,o)=>fs.writeFileSync(p,JSON.stringify(o,null,2)+'\n');
const manifest=read('Assets/EasternDynasty/Resources/asset-manifest.json');
const sourceFiles=['Assets/EasternDynasty/Scripts/Runtime/CityAffairsLayout.cs','Assets/EasternDynasty/Scripts/Runtime/CityAffairsDesignPresentation.cs','Assets/EasternDynasty/Scripts/Runtime/CityOverviewPresentation.cs','Assets/EasternDynasty/Scripts/Runtime/Diagnostics/CityOverviewDiagnostics.cs','Tools/Check-Experience.ps1','Tools/Validate-Visual.ps1','Tools/Check-CityAffairsLayout.ps1','Tools/Validate.ps1','Validation/CityAffairsLayoutChecks.cs','Validation/UiDesignChecks.mjs'];
const outputs=['desktop','mobile','states'].map(target=>{
 const r=read(`${design}/${target}/exports/export-receipt.json`);
 return {target,source:r.relativeSource,sourceSha256:r.sourceSha256,preview:r.images[0].relativePath,previewSha256:r.images[0].sha256,width:r.images[0].width,height:r.images[0].height,toolVersion:r.toolVersion,actuallyViewed:true};
});
const baselines=['youzhou-1440x900','mobile-1600x720'].map(target=>{
 const p=`F:/EasternDynastyBuild/Releases/s1-20261027-104752269/Project/Validation/Visual/s1-20261027-104752269-${target}/smoke-city-developed.png`;
 return {version:'s1-20261027-104752269',path:p,sha256:sha(p),actuallyViewed:true,currentIterationScreenshot:false};
});
const spec=read(`${design}/design-spec.json`);
spec.status='exported-inspected-runtime-integrated-awaiting-publisher';
spec.colors={text:'#eee6cb',muted:'#b5bbaa',accent:'#e1cc8e',progress:'#d4ba78',track:'#46534a'};
spec.baselines=baselines;
spec.notes=spec.notes.map(x=>x.replace('Working-state building image remains an explicitly labelled context sample; runtime must draw the actual job kind.','Existing TownHall/Mill pure-raster artifacts illustrate the unchanged runtime images; source geometry is not modified.'));
write(`${design}/design-spec.json`,spec);
write(`${design}/inspection.json`,{
 version,recordedAtUtc:new Date().toISOString(),status:'design-export-inspected-runtime-pending',reference:spec.reference,baselines,exports:outputs,
 inspectedCorrections:['First export: baseline building crop left square corners inside the circular seal; replaced only design context with the existing transparent TownHall/Mill outputs.','Removed the scalable decorative divider that could intersect the chapter document icon.','Matched the reused command component gradient and working-command emphasis to the existing formal export/runtime.','Re-exported and opened final desktop, mobile and idle/working/reward/hover previews; also opened the formal component sheet.'],
 conclusion:'Independent round facility identity, separated summary and chapter surfaces are visible in both design previews. Dynamic text, actual facility image and source entry remain runtime-owned. No new game screenshot or visual acceptance claimed.',
 sourceBoundaries:{historicalPixelsInProductionComponents:false,figmaConnected:false,figmaCalled:false,newProceduralArt:false,existingArtReused:true,unityStarted:false,desktopInteracted:false},
 reusedArt:['Validation/CityArt/TownHall.png','Validation/CityArt/Mill.png'].map(path=>({path,sha256:sha(path),purpose:'Embedded design context only; unchanged existing facility rendering, not a newly imported asset.'})),
 requiredPublisherReview:['Windows 1440x900 and second resolution; mobile 1600x720 safe-area previews.','New/long-name/developed city, active mill upgrade and chapter state typography.','Real building/caption/chapter-caption clicks, same-facility navigation and return, modal isolation and immutable campaign.','Circle and thin border sampling, unchanged city panorama and all existing facilities still reachable.']
});
const logic=fs.readFileSync(`${validation}/logic.log`,'utf8').replace(/\0/g,'');
const assets=fs.readFileSync(`${validation}/assets.log`,'utf8');
if(!logic.includes('PASS: runtime and Editor scripts compile')||!logic.includes('City affairs layout checks passed: 4299')||!assets.includes('ready=289')) throw Error('Expected completed validation evidence missing');
write(`${validation}/result.json`,{
 version,status:'passed-pure-validation-runtime-pending',recordedAtUtc:new Date().toISOString(),
 designInspection:`${design}/inspection.json`,ragAudit:`${design}/rag-audit.json`,uiIteration:`Docs/UIIterations/${version}.md`,
 checks:{unifiedPureCSharpAndReferenceCompile:'passed',cityAffairsLayout:4299,historicalGeometryCases:27,productionUiSourceAndExportChain:31,assetReady:289,existingAssetMissing:16,pendingUnityUiMeta:31},
 logEvidence:['logic.log','assets.log','design-checks.log','import.log'].map(file=>({path:`${validation}/${file}`,sha256:sha(`${validation}/${file}`)})),
 applied:{ragReference:'base-d728496fa74ee1ee61ed / STZB-0428 historical city composition, partial presentation use only',newRagBatch:'Q04 observed, not applied: current S1 identity/display is unverified',rulesChanged:false,saveFieldsChanged:false,assets:manifest.entries.filter(x=>x.id.startsWith('ui.youzhou.city_affairs_')).map(x=>({id:x.id,path:`Assets/EasternDynasty/Resources/${x.resourcePath}.png`,sha256:x.sha256,receipt:x.source.reference}))},
 sourceFiles:sourceFiles.map(path=>({path,sha256:sha(path)})),
 runtime:{executed:false,newCapture:'smoke-city-affairs-design.png',requiredMarker:'CITY_AFFAIRS_DESIGN_RUNTIME_OK',existingWorkingCapture:'smoke-city-affairs-working.png',actualClicksExecuted:false,visualAcceptance:'pending publisher',androidDeviceTested:false,windowsApkDelivered:false},
 publication:{operated:false,oldFailure:'094252047 planning target capture fix already present and verified in 104752269 log'},
 remaining:['Publisher must execute new screenshots/actual clicks and compare original/design/runtime.','Current S1 high-resolution city reference, finished facility art and state assets remain missing (S1UX-04/S1DEV-06).','Keep independent training, chapter tasks, frozen replay/current battle and Android device acceptance pending.']
});
console.log('Recorded actual design inspection and completed pure validation; runtime acceptance explicitly pending.');
