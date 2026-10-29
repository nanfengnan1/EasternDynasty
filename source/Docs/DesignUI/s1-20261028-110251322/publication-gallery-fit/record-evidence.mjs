import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261028-110251322';
const root=path.resolve('Docs/DesignUI/'+version+'/publication-gallery-fit');
const evidence=path.resolve('Validation/UIRound/'+version);
const failedRoot='F:/EasternDynastyBuild/Releases/s1-20261028-095751175';
const log=failedRoot+'/Project/Validation/Visual/s1-20261028-095751175-mobile-1600x720/player.log';
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const save=(p,value)=>fs.writeFileSync(p,JSON.stringify(value,null,2));
fs.mkdirSync(evidence,{recursive:true});
fs.copyFileSync(log,path.join(evidence,'publication-failed-mobile-player.log'));
const spec=JSON.parse(fs.readFileSync(path.join(root,'design-spec.json'),'utf8'));
const inspected=['desktop','mobile'].map(target=>{
  const receipt=JSON.parse(fs.readFileSync(path.join(root,target,'exports/export-receipt.json'),'utf8').replace(/^\uFEFF/,''));
  if(hash(receipt.source).toLowerCase()!==receipt.sourceSha256.toLowerCase())throw new Error('Design source changed after export');
  return {target,source:receipt.relativeSource,sourceSha256:receipt.sourceSha256,preview:receipt.images[0].relativePath,previewSha256:receipt.images[0].sha256,tool:receipt.toolVersion,receipt:path.relative(process.cwd(),path.join(root,target,'exports/export-receipt.json'))};
});
const inspection={status:'exported-inspected-and-implemented-awaiting-runtime',atUtc:new Date().toISOString(),inspected,notes:['Both tool-produced PNG previews were opened with view_image before Runtime edits.','Eight full 58-high building illustrations and 20-high names retain their existing design; the status text now uses a separate 20-high row inside a 40-high footer.','The tile ends at y=334 on desktop, before the unchanged ledger y=342; mobile uses the same logical 8-unit separation.','Inherited surrounding layout is design context, not a new runtime capture or a claim that all current facilities match the original game.','No new artwork, assets, rules, storage or player changes. Actual Unity mobile font measurement and click/screenshot acceptance remain pending.']};
save(path.join(root,'inspection.json'),inspection);
spec.status=inspection.status;save(path.join(root,'design-spec.json'),spec);
const sources=['Assets/EasternDynasty/Scripts/Runtime/CityFacilityGalleryPresentation.cs','Assets/EasternDynasty/Scripts/Runtime/CityFacilityGalleryLayout.cs','Assets/EasternDynasty/Scripts/Runtime/CityFacilitySheetLayout.cs','Assets/EasternDynasty/Scripts/Runtime/CityFacilityPresentation.cs','Assets/EasternDynasty/Scripts/Runtime/Diagnostics/CityFacilityDossierDiagnostics.cs','Validation/CityFacilityGalleryChecks.cs'];
save(path.join(evidence,'publication-fix.json'),{
  status:'source-fixed-pure-checks-passed-runtime-pending',version,atUtc:new Date().toISOString(),
  previousFailure:{version:'s1-20261028-095751175',stage:'validating',sourceLog:log,sourceSha256:hash(log),copiedLog:'Validation/UIRound/'+version+'/publication-failed-mobile-player.log',exception:'Facility gallery requires three actual exported skins and fitting GUI fonts.',context:'Both desktop resolutions passed 181 states. Mobile 1600x720 passed all land gates, then failed in AssertCityFacilityDossierGeometry during CityFacilitySmoke.',limitation:'The old exception combined asset dimensions and aggregate text fit; it did not report the specific offending label. The bundled-font metadata independently reproduces the 16-high status defect, consistent with the mobile-only failure; next publisher run must verify the actual GUIStyle.'},
  font:spec.font,designInspection:path.relative(process.cwd(),path.join(root,'inspection.json')),
  changes:['Extend existing facility option tiles 96 to 100 inside unchanged 108 row spacing; use shared height for drawing, bounds and checks.','Extend footer 36 to 40 and status 16 to 20 without shrinking the complete 58-high image, the name row, the 12px readable minimum, or moving the ledger.','Retain the asset dimension and text-fit gates separately; include actual lineHeight in label fitting and report specific label, font, size, bounds, CalcSize, lineHeight and selected facility on failure.','Read head/hhea from the actual bundled CJK.otf in the existing unified gallery checker; reproduce old 16-high failure and verify new geometry.'],
  sources:sources.map(source=>({source,sha256:hash(source)})),
  checks:[{command:'Tools/Check-CityFacilityGallery.ps1',assertions:3521,log:'Validation/UIRound/'+version+'/publication-gallery-checks.log',status:'passed'},{command:'Tools/Check-CityFacilitySheet.ps1',assertions:3776,log:'Validation/UIRound/'+version+'/publication-sheet-checks.log',status:'passed'}],
  remaining:['Publisher must rerun mobile1600x720 actual GUIStyle and every facility state, then review screenshots and real image/name clicks.','Full Runtime reference compilation is performed by the parent unified validation after integration.','Do not count this font repair alone as the required substantial visual redesign.'],
  boundaries:['No Unity Editor/client/desktop interaction started.','No release snapshot, publication state, locks, saves or delivery directories modified.']
});
console.log(JSON.stringify({evidence:path.join(evidence,'publication-fix.json'),inspection:path.join(root,'inspection.json')}));
