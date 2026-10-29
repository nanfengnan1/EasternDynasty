import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261027-222251138',design=`Docs/DesignUI/${version}`,validation=`Validation/CardDesign/${version}`;
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const read=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const write=(p,v)=>fs.writeFileSync(p,JSON.stringify(v,null,2)+'\n');
const bind=p=>({path:p,sha256:sha(p),bytes:fs.statSync(p).size});
const logText=p=>{const b=fs.readFileSync(p);return b.toString(b[0]===255&&b[1]===254?'utf16le':'utf8').replace(/^\uFEFF/,'');};
const previews=['desktop','mobile','states','components'].map(target=>{
 const p=`${design}/${target}/exports/export-receipt.json`,r=read(p);
 if(sha(r.relativeSource)!==r.sourceSha256.toLowerCase())throw Error('Source no longer matches export');
 for(const image of r.images)if(sha(image.relativePath)!==image.sha256.toLowerCase())throw Error('Export no longer matches receipt');
 return {target,receipt:bind(p),source:bind(r.relativeSource),toolVersion:r.toolVersion,preview:r.images.find(x=>x.relativePath.endsWith('portrait-preview.png'))};
});
const communityRoot='F:/工作项目/率土之滨/rag/imports/learning/learn-question-20261025-134754707-Q07-f3a85437d17b6b7d992d';
const community=read(communityRoot+'/batch.json'),communitySource=community.sources.find(x=>x.id==='community-233');
const communityImage='F:/工作项目/率土之滨/rag/assets/38/381febccce9065421a0eff6731ad21abd368f0e4d8b0a82cd3c38c46969d9568.webp';
const communityHtml='C:/Users/fenglei/Downloads/率土之滨_素材集_2026-10-25/S1_持续学习/批次/question-20261025-134754707-Q07/'+communitySource.local_path;
if(sha(communityHtml)!==communitySource.sha256)throw Error('Community provenance SHA mismatch');
const spec=read(`${design}/design-spec.json`);
spec.status='exported-inspected-runtime-integrated-awaiting-publisher';
spec.supplementaryReference={id:'Q07-community-card',...bind(communityImage),size:[2772,1240],sourcePublishedAt:communitySource.published_at,screenshotAt:null,currentS1:'unknown',use:'Later reference cross-check: same vertical name and rarity composition; not primary official provenance or production asset.'};
spec.actualMobileSafeArea={x:56,y:12,width:1504,height:684,scale:684/820};
write(`${design}/design-spec.json`,spec);
write(`${design}/inspection.json`,{version,atUtc:new Date().toISOString(),status:'design-reviewed-runtime-pending',previews,
 referenceReview:['STZB-0467 official 2022 full details opened before design; primary layout source.','STZB-0505 early complete frame opened before design; historical comparison only.','104752269 desktop and mobile actual card screenshots opened before design.','Q07-community-card later publication inspected as supplementary cross-check; screenshot age/client/S1 unknown.'],
 supplementarySource:{...communitySource,actualFile:bind(communityHtml),image:bind(communityImage)},
 baseline:['desktop','mobile'].map(t=>bind(`${design}/baseline/${t}.png`)),
 inspectedFindings:['Removed continuous opaque frame/edge bands; complete stable-ID portrait and independent identity column remain visually separate.','Desktop and mobile previews show portrait, name, faction, rarity and unchanged factual base without overlap.','Actual long-name four-star Shu and low-rarity three-star Jin identities are shown as historical collection; owned Sun Jian is explicitly a design-state sample.','Final source uses unique editable object IDs and transparent slice bounds at 2x dimensions.','Final desktop/mobile/state preview hashes remained the same after slice-bound/object-ID corrections; final components also viewed.'],
 limitations:['Not a current game screenshot or visual approval.','Existing 240x348 portrait detail is unchanged; full-body background and faction-specific ornamentation remain unavailable.','Inkscape user-profile/recent-file warnings retained; exports and receipts succeeded.']});
const log=logText(`${validation}/logic.log`);
const assets=logText(`${validation}/assets.log`);
const imported=logText(`${validation}/import.log`);
if(!log.includes('PASS: runtime and Editor scripts compile')||!log.includes('CARD_PORTRAIT_DESIGN_CHECKS_OK: 84811')||!assets.includes('ready=300; missing=16; pending Unity meta=42'))throw Error('Required actual validation result missing');
for(const p of ['ribbon','seal','base'])if(!imported.includes('UNCHANGED ui.youzhou.portrait_'+p))throw Error('Idempotent controlled-import verification missing');
const sourceFiles=['Assets/EasternDynasty/Scripts/Runtime/CardPortraitDesignLayout.cs','Assets/EasternDynasty/Scripts/Runtime/CardDetailPresentation.cs','Assets/EasternDynasty/Scripts/Runtime/Diagnostics/CardVisualDiagnostics.cs','Tools/Check-Experience.ps1','Tools/Validate-Visual.ps1','Tools/Check-CardPortraitText.ps1','Validation/CardPortraitDesignChecks.cs','Validation/CardPortraitTextChecks.cs','Validation/UiDesignChecks.mjs','Assets/EasternDynasty/Resources/asset-manifest.json'];
const manifest=read(sourceFiles[sourceFiles.length-1]);
const production=manifest.entries.filter(x=>x.id.startsWith('ui.youzhou.portrait_')).map(x=>({id:x.id,width:x.width,height:x.height,sha256:x.sha256,file:bind('Assets/EasternDynasty/Resources/'+x.resourcePath+'.png'),receipt:x.source.reference}));
write(`${validation}/result.json`,{version,atUtc:new Date().toISOString(),status:'changed',uiStatus:'design-and-runtime-source-complete-awaiting-publisher',
 changes:['Portrait frame and twelve bands removed; name/faction/vertical rarity form a separate column; full portrait is top-aligned with 33 more units of available height.','Three real Inkscape surfaces controlled-imported; actual name/star font fitting, complete-image geometry and independent factual base integrated.','New screenshot/marker and three actual-bounds read-only click checks added; existing controls, scrolling, return and World isolation retained.'],
 validation:{logic:{exitCode:0,log:bind(`${validation}/logic.log`),includesReferenceCompilation:true},assets:{exitCode:0,log:bind(`${validation}/assets.log`),ready:300,existingMissing:16,pendingUnityMeta:42},controlledImport:{exitCode:0,log:bind(`${validation}/import.log`),idempotent:true},portraitGeometry:{assertions:84811,cases:2940,sourcePngCount:245,screenLayouts:12},portraitFonts:{assertions:23241},uiExportChain:{components:42}},
 production,sourceFiles:sourceFiles.map(bind),designInspection:bind(`${design}/inspection.json`),designSpec:bind(`${design}/design-spec.json`),uiRecord:bind(`Docs/UIIterations/${version}.md`),research:bind(`${validation}/research.json`),failureReview:bind(`${validation}/failure-review.json`),
 toolingNote:'Initial import wrapper returned nonzero after all three successful commits because redirection followed a foreach statement. A corrected script-block wrapper re-ran the importer and verified all three UNCHANGED; independent resource and SHA checks passed.',
 ragApplied:[{version:'base-d728496fa74ee1ee61ed',configuration:'UI功能清单.json / UI-03',status:'partially-applied-presentation-only'}],ragObservedNotApplied:['learn-question-20261027-211755045-Q10-3c527259556e300f4c5f'],
 runtimeExecuted:false,unityLaunched:false,windowsOrApkDelivered:false,saveSchemaChanged:false,
 remaining:['Publisher: new dual-desktop/mobile card screenshots, actual Chinese fonts, independent column and full-image visual comparison; run actual three-bounds clicks and original tabs/scroll/return/modal checks.','Current S1 full-body scene/card and faction-specific assets remain unverified; continue professional design of right dossier/skill area or city facilities.','Independent training, chapter quests, frozen replay/current-battle viewing and Android device acceptance remain open.']});
console.log('Recorded inspected design, source hashes and completed pure checks; runtime acceptance remains pending.');
