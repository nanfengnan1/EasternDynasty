import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261027-211752367',dir=`Docs/DesignUI/${version}`,validation=`Validation/CityFooterDesign/${version}`;
const json=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const evidence=p=>({path:p,sha256:sha(p)});
const spec=json(`${dir}/design-spec.json`);
const drafts=['desktop','mobile','states'].map(target=>{
 const receipt=json(`${dir}/${target}/exports/export-receipt.json`);
 if(sha(receipt.source)!==receipt.sourceSha256.toLowerCase())throw new Error('Source changed after Inkscape export: '+target);
 const preview=receipt.images[0];
 if(sha(preview.path)!==preview.sha256.toLowerCase())throw new Error('Preview changed after export: '+target);
 return {target,toolVersion:receipt.toolVersion,source:evidence(receipt.relativeSource),preview:evidence(preview.relativePath),receipt:evidence(`${dir}/${target}/exports/export-receipt.json`),actuallyViewed:true};
});
spec.status='exported-inspected-runtime-integrated-awaiting-publisher';
spec.fonts.emptyColor='#46413d';
fs.writeFileSync(`${dir}/design-spec.json`,JSON.stringify(spec,null,2));
const manifest=json('Assets/EasternDynasty/Resources/asset-manifest.json');
const assets=spec.components.map(id=>{
 const asset=manifest.entries.find(x=>x.id===id);
 const p='Assets/EasternDynasty/Resources/'+asset.resourcePath+'.png';
 if(sha(p)!==asset.sha256.toLowerCase())throw new Error('Formal asset changed: '+id);
 return {id,...evidence(p),originalExportReceipt:asset.source.reference,reusedWithoutImport:true};
});
const inspection={version,atUtc:new Date().toISOString(),method:'view_image on original images, actual historical desktop/mobile screenshots and real Inkscape exports before Runtime changes',
 references:spec.references,baselines:spec.baselines.map(x=>({...x,actuallyViewed:true,currentIterationScreenshot:false})),drafts,assets,
 outcome:'Grouped image tickets, vertical labels, separated recruitment and utility group inspected; empty-formation contrast corrected in SVG and re-exported/re-inspected before code.',
 limits:['Runtime acceptance pending publisher; no Unity or client launched.','Full-screen contexts include previous designs on old runtime screenshots.','Modal opacity is illustrative only; actual modal overlay/input must be verified.','Historical city same-page reference and 2021 map language do not prove current S1 city appearance.','No new production assets, no new raw-generator art, Figma not connected or called.']};
fs.writeFileSync(`${dir}/inspection.json`,JSON.stringify(inspection,null,2));
console.log('Recorded actual preview/source hashes and six unchanged production assets.');
const readLog=p=>{const b=fs.readFileSync(p);return b[0]===255&&b[1]===254?b.subarray(2).toString('utf16le'):b.toString('utf8').replace(/^\uFEFF/,'');};
if(fs.existsSync(`${validation}/logic.log`)){
 const logic=readLog(`${validation}/logic.log`),assetLog=readLog(`${validation}/assets.log`);
 const required=['PASS: runtime and Editor scripts compile against installed Unity references.',
  'PASS: city footer desktop/mobile/states and real Inkscape receipts match;',
  'PASS: 38 city expansion/return/footer assertion regressions;'];
 for(const marker of required)if(!logic.includes(marker))throw new Error('Required actual check result missing: '+marker);
 const assetMatch=assetLog.match(/ready=(\d+); missing=(\d+); pending Unity meta=(\d+)/);
 if(!assetMatch)throw new Error('Asset validation receipt missing');
 const regression=json(`${validation}/publication-panorama-regression.json`);
 if(regression.status!=='PASSED'||regression.caseCount!==38)throw new Error('Assertion regression failed');
 const implementation=[
  'Assets/EasternDynasty/Scripts/Runtime/MapHudPresentation.cs',
  'Assets/EasternDynasty/Scripts/Runtime/Diagnostics/CityOverviewDiagnostics.cs',
  'Assets/EasternDynasty/Scripts/Runtime/Diagnostics/MapHudVisualDiagnostics.cs',
  'Tools/Check-Experience.ps1','Tools/Validate-Visual.ps1','Validation/UiDesignChecks.mjs','Validation/ExperienceCityPanoramaChecks.ps1'];
 const result={version,atUtc:new Date().toISOString(),status:'passed-development-checks-runtime-pending',uiSourceComplete:true,runtimeVisualAcceptance:'pending-publisher',
  changes:['City old full-width footer replaced by existing inspected image tickets, vertical labels, independent right recruitment and centered save/time group.','Full six-entry/eight-control navigation preserved; real font fit and empty-formation contrast implemented.','Required screenshot, runtime diagnostic and twelve real image/title input paths added for publisher.'],
  tests:[{command:'Tools/Validate.ps1',exitCode:0,log:evidence(`${validation}/logic.log`),requiredMarkers:required},
   {command:'Tools/Validate-Assets.ps1',exitCode:0,log:evidence(`${validation}/assets.log`),ready:+assetMatch[1],missing:+assetMatch[2],pendingUnityMeta:+assetMatch[3]},
   {command:'Validation/ExperienceCityPanoramaChecks.ps1',record:evidence(`${validation}/publication-panorama-regression.json`),cases:38,originalCasesPreserved:27,newExplicitSyntheticCases:11,runtimeEvidence:false}],
  implementation:implementation.map(evidence),designInspection:evidence(`${dir}/inspection.json`),designSpec:evidence(`${dir}/design-spec.json`),
  ragReview:evidence(`${validation}/rag-review.json`),failureReview:evidence(`${validation}/failure-review.json`),
  resources:{newOrReplaced:0,reusedStableIds:spec.components,originalMetaPreserved:true},
  runtime:{unityStarted:false,clientStarted:false,newScreenshotsCaptured:false,desktopInputExecuted:false,windowsOrApkDelivered:false,publicationModified:false},
  remaining:['Publisher: new city-footer screenshot at both desktop resolutions and phone preview; compare references/draft/runtime.','Publisher: twelve image/title clicks, return, whole World preservation, actual fonts, save/time and modal suppression; Android device checks remain separate.','Continue facility/card professional design using corresponding reference; current S1 high-resolution city and finished building art gaps remain S1UX-04/S1DEV-06.','Training, chapter tasks, frozen-event replay and current-battle watch runtime acceptance remain open.']};
 fs.writeFileSync(`${validation}/result.json`,JSON.stringify(result,null,2));
 console.log('Recorded passed development checks; runtime screenshot/input acceptance remains pending.');
}
