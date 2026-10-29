import fs from 'node:fs';
import crypto from 'node:crypto';
const version='s1-20261028-002751316',design='Docs/DesignUI/'+version,out='Validation/UIRound/'+version;
const read=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const bind=p=>({path:p,sha256:sha(p),bytes:fs.statSync(p).size});
const write=(p,v)=>fs.writeFileSync(p,JSON.stringify(v,null,2)+'\n');
const log=p=>{const b=fs.readFileSync(p);return b.toString(b[0]===255?'utf16le':'utf8').replace(/^\uFEFF/,'');};
const exports=['desktop','mobile','states','components'].map(target=>{
 const p=`${design}/${target}/exports/export-receipt.json`,r=read(p);
 if(sha(r.relativeSource)!==r.sourceSha256.toLowerCase())throw Error('Source/export mismatch '+target);
 for(const i of r.images)if(sha(i.relativePath)!==i.sha256.toLowerCase())throw Error('Output/export mismatch '+target);
 return {target,tool:r.toolVersion,receipt:bind(p),source:bind(r.relativeSource),images:r.images};
});
const logic=log(out+'/logic.log'),assets=log(out+'/assets.log');
if(!logic.includes('PASS: runtime and Editor scripts compile')||!logic.includes('CITY_FACILITY_SHEET_CHECKS_OK: 3776'))throw Error('Required logic/reference checks absent');
if(!assets.includes('ready=311; missing=16; pending Unity meta=53'))throw Error('Asset result missing');
const spec=read(design+'/design-spec.json');spec.status='exported-inspected-runtime-integrated-awaiting-publisher';
spec.actualMobileSafeArea={x:56,y:12,width:1504,height:684,scale:684/820};write(design+'/design-spec.json',spec);
const manifest=read('Assets/EasternDynasty/Resources/asset-manifest.json');
const production=spec.components.map(name=>{const entry=manifest.entries.find(e=>e.id==='ui.youzhou.'+name);if(!entry)throw Error('Missing component '+name);return {id:entry.id,width:entry.width,height:entry.height,source:entry.source,file:bind('Assets/EasternDynasty/Resources/'+entry.resourcePath+'.png')};});
write(design+'/inspection.json',{version,atUtc:new Date().toISOString(),status:'design-inspected-runtime-pending',exports,
 baselines:['desktop','mobile'].map(t=>bind(design+'/baseline/'+t+'.png')),
 inspected:['Opened Q09-CU-A001 official 2024 facility page, S1-RES-029 historical residence detail and 104752269 desktop/mobile facility baselines before design.','Description now leads a compact horizontal current/next effect row; five project resources share one horizontal strip.','The fixed 360x44 command keeps time next to build/upgrade and keeps prerequisite/cancel/maximum states.','First two previews exposed nested SVG atlas overflow; corrected with explicit clipPath, then re-exported and opened desktop/mobile/states before Runtime implementation.','State preview corrected to level-zero construction, paid receipt-only cost, correct next-level costs and visibly disabled shortage command.','Five vector components inspected; no original screenshot pixels or live labels in the production exports.'],
 limitations:['Design previews are composites, not runtime screenshots.','Existing building illustrations and eight selectors retained; current S1 high-resolution facility artwork still missing.','Inkscape profile/recent-file warning logs retained. No Figma, Unity or desktop activity.']});
const files=['Assets/EasternDynasty/Scripts/Runtime/CityFacilitySheetLayout.cs','Assets/EasternDynasty/Scripts/Runtime/CityFacilitySheetPresentation.cs','Assets/EasternDynasty/Scripts/Runtime/CityFacilityPresentation.cs','Assets/EasternDynasty/Scripts/Runtime/Diagnostics/CityFacilityDossierDiagnostics.cs','Assets/EasternDynasty/Scripts/Runtime/Diagnostics/ExperienceDiagnostics.cs','Validation/CityFacilitySheetChecks.cs','Validation/UiDesignChecks.mjs','Tools/Check-CityFacilitySheet.ps1','Tools/Validate.ps1','Tools/Validate-Visual.ps1','Tools/Check-Experience.ps1'];
write(out+'/result.json',{version,atUtc:new Date().toISOString(),status:'changed',runtimeAccepted:false,design:bind(design+'/inspection.json'),sources:files.map(bind),production,
 validation:{logic:{exitCode:0,file:bind(out+'/logic.log')},assets:{exitCode:0,file:bind(out+'/assets.log')},design:bind(out+'/design.log'),layout:bind(out+'/layout.log'),inputSyntax:bind(out+'/input-syntax.log')},
 research:bind(out+'/research.json'),failureReview:bind(out+'/failure-review.json'),
 ragApplied:'Q09-CU-A001 historical facility layout partially applied to presentation only; Q12 observed, not game rules. No roster, Core, Application or save changes.',
 publisherRequired:['New double-desktop/mobile facility screenshots including smoke-facility-sheet-design.png and new runtime marker','Actual GUI fonts, description/effect/cost order, five first-screen costs and disabled action contrast','Real left-side build/right-side cancel clicks plus existing prerequisite, scrolling, return, refunds and stale cancellation tests','Windows/APK delivery only after publication gates; Android real-device validation remains outstanding']});
console.log('Recorded actual design exports, inspected previews, source hashes and passing pure validations; runtime pending.');
