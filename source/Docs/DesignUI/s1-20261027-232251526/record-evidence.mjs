import fs from 'node:fs';
import crypto from 'node:crypto';
const version='s1-20261027-232251526',design=`Docs/DesignUI/${version}`,validation=`Validation/CardInfoDesign/${version}`;
const read=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const bind=p=>({path:p,sha256:sha(p),bytes:fs.statSync(p).size});
const write=(p,v)=>fs.writeFileSync(p,JSON.stringify(v,null,2)+'\n');
const log=p=>{const b=fs.readFileSync(p);return b.toString(b[0]===255&&b[1]===254?'utf16le':'utf8').replace(/^\uFEFF/,'');};
const previews=['desktop','mobile','states','components'].map(target=>{
 const p=`${design}/${target}/exports/export-receipt.json`,r=read(p);
 if(sha(r.relativeSource)!==r.sourceSha256.toLowerCase())throw Error('Design differs from actual export: '+target);
 for(const image of r.images)if(sha(image.relativePath)!==image.sha256.toLowerCase())throw Error('Output differs from receipt: '+target);
 return {target,receipt:bind(p),source:bind(r.relativeSource),toolVersion:r.toolVersion,preview:r.images.find(x=>x.relativePath.endsWith('dossier-preview.png'))};
});
const spec=read(`${design}/design-spec.json`);
spec.status='exported-inspected-runtime-integrated-awaiting-publisher';
spec.actualMobileSafeArea={x:56,y:12,width:1504,height:684,scale:684/820};
spec.supplementaryReference={id:'Q07-community-card',path:'F:/工作项目/率土之滨/rag/assets/38/381febccce9065421a0eff6731ad21abd368f0e4d8b0a82cd3c38c46969d9568.webp',sourcePublishedAt:'2026-06-25',screenshotAt:null,currentS1:'unknown',excludedFromS1:true,use:'Layout cross-check only; no rules, identity or production pixels imported.'};
write(`${design}/design-spec.json`,spec);
write(`${design}/inspection.json`,{version,atUtc:new Date().toISOString(),status:'design-inspected-runtime-pending',previews,
 referenceReview:['STZB-0467 2022 official card details opened before design; primary composition and silhouette reference.','Q07-community-card opened as a later-published cross-check; original screenshot age/S1 unknown and excludedFromS1 preserved.','104752269 desktop/mobile actual card screenshots opened before design; predecessor portrait editable layers reused in this composition.'],
 baseline:['desktop','mobile'].map(t=>bind(`${design}/baseline/${t}.png`)),
 inspectedFindings:['The vertical skill rail was removed; all five real attributes share the full information width in two columns.','Names, base values and growth remain on one line; the independent level/preview band stays above them.','Two lower horizontal skill groups show their own image, type and name with separate editable text and hit regions.','First preview exposed incorrect aspect handling of reused surface images; preserveAspectRatio=none was applied and final desktop/mobile/state PNGs re-exported and opened before Runtime integration.','Five reference-derived vector symbols and a neutral round container are exported without historical screenshot pixels or baked text. Existing book is a project record icon, not an official skill image or grade.','Final desktop/mobile/state/component previews have been opened. State samples are illustrative, not player-save evidence.'],
 limitations:['No Unity, client, screenshot capture or desktop interaction performed. New runtime appearance is awaiting publisher.','Full-body scene art, current S1 card assets and official skill images remain unavailable; the detailed skill/source tabs were not redesigned.','Inkscape profile/recent-file write warnings are retained; export, geometry and receipts succeeded.']});
const logic=log(`${validation}/logic.log`),assets=log(`${validation}/assets.log`),imports=log(`${validation}/import.log`);
if(!logic.includes('PASS: runtime and Editor scripts compile')||!logic.includes('CARD_DOSSIER_DESIGN_CHECKS_OK:'))throw Error('Required actual pure/static validation missing');
if(!assets.includes('ready=306; missing=16; pending Unity meta=48'))throw Error('Actual resource result missing');
for(const name of spec.components)if(!imports.includes('IMPORTED ui.youzhou.'+name))throw Error('Actual controlled import missing: '+name);
const manifest=read('Assets/EasternDynasty/Resources/asset-manifest.json');
const production=manifest.entries.filter(x=>x.id.startsWith('ui.youzhou.dossier_')).map(x=>({id:x.id,width:x.width,height:x.height,sha256:x.sha256,file:bind('Assets/EasternDynasty/Resources/'+x.resourcePath+'.png'),receipt:x.source.reference}));
const files=['Assets/EasternDynasty/Scripts/Runtime/CardDossierDesignLayout.cs','Assets/EasternDynasty/Scripts/Runtime/CardDossierPresentation.cs','Assets/EasternDynasty/Scripts/Runtime/Diagnostics/CardVisualDiagnostics.cs','Tools/Check-CardDossierDesign.ps1','Tools/Check-Experience.ps1','Tools/Validate.ps1','Tools/Validate-Visual.ps1','Validation/CardDossierDesignChecks.cs','Validation/UiDesignChecks.mjs','Assets/EasternDynasty/Resources/asset-manifest.json'];
write(`${validation}/result.json`,{version,atUtc:new Date().toISOString(),status:'changed',uiStatus:'design-and-runtime-source-complete-awaiting-publisher',
 changes:['Full-width two-column attributes use inline base/growth and six controlled Inkscape components.','Two skills move below attributes into a horizontal image/type/name row, with a separate factual level/preview band.','Pure geometry/provenance and actual runtime font/asset/input gates added without removing original assertions.'],
 validation:{logic:{exitCode:0,log:bind(`${validation}/logic.log`),includesReferenceCompilation:true},assets:{exitCode:0,log:bind(`${validation}/assets.log`),ready:306,existingMissing:16,pendingUnityMeta:48},controlledImport:{exitCode:0,log:bind(`${validation}/import.log`),count:6},dossierGeometry:{assertions:Number(logic.match(/CARD_DOSSIER_DESIGN_CHECKS_OK: (\d+)/)?.[1]),screenLayouts:12,verifiedPngs:8},uiExportChain:{components:48}},
 production,sourceFiles:files.map(bind),designInspection:bind(`${design}/inspection.json`),designSpec:bind(`${design}/design-spec.json`),uiRecord:bind(`Docs/UIIterations/${version}.md`),research:bind(`${validation}/research.json`),failureReview:bind(`${validation}/failure-review.json`),
 ragApplied:[{version:'base-d728496fa74ee1ee61ed',configuration:'UI功能清单.json / UI-03',status:'partially-applied-presentation-only'}],
 ragObservedNotApplied:[{version:'learn-question-20261027-223254840-Q11-ccffd3218d1c8335bc1b',reason:'Asset-object relation audit; not applicable to game rules. Original image scopes and exclusions retained.'}],
 runtimeExecuted:false,unityLaunched:false,windowsOrApkDelivered:false,saveSchemaChanged:false,
 remaining:['Publisher: dual-desktop/mobile new dossier and longest-skill-name screenshots, actual fonts, four image/name clicks plus original slot center/tab/scroll/return and World isolation.','Current S1 full-body/card/skill artwork still missing; professional redesign of detailed skill/source tabs or city facilities is next.','Independent training, chapter quests, frozen replay/current-battle viewing and Android real-device acceptance stay open.']});
console.log('Recorded inspected design and completed pure/static checks with source SHA; runtime acceptance remains pending.');
