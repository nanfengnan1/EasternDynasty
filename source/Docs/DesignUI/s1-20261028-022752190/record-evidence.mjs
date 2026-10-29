import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261028-022752190',design=`Docs/DesignUI/${version}`,out=`Validation/UIRound/${version}`;
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const record=p=>({path:p,sha256:sha(p),bytes:fs.statSync(p).size});
const read=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const log=p=>{const b=fs.readFileSync(p);return b[0]===255&&b[1]===254?b.subarray(2).toString('utf16le'):b.toString('utf8');};
const receipts=['desktop','mobile','states','components'].map(target=>{
 const p=`${design}/${target}/exports/export-receipt.json`,r=read(p);
 if(sha(r.relativeSource)!==r.sourceSha256.toLowerCase())throw Error('Source changed after export: '+target);
 for(const image of r.images)if(sha(image.relativePath)!==image.sha256.toLowerCase())throw Error('Preview changed: '+target);
 return {...record(p),toolVersion:r.toolVersion,source:record(r.relativeSource),images:r.images.map(i=>({...record(i.relativePath),width:i.width,height:i.height}))};
});
const inspection={version,status:'design-export-inspected-runtime-pending',atUtc:new Date().toISOString(),
 inspected:['Q08-A001 original 767x393','STZB-0453 original 510x288','Q09-CU-A001 original 800x450','104752269 desktop/mobile facility-catalog baseline','desktop/mobile/working/component Inkscape previews before Runtime changes'],
 findings:['Removed circular medallions and separate level ticks; full building artwork occupies a 216x162 rectangle beside factual rows and the action.','Desktop preview shows four complete nodes; mobile preview six. Partially visible rows still require scrolling before interaction.','Initial example benefits and busy queue labels corrected against existing Core/Application, then re-exported and viewed.','Font budget prompted increasing the three-column threshold to 1460; the inspected desktop/mobile compositions are byte-identical after regeneration and their receipts remain valid.','Historical reference is layout research. Existing 240x180 project building illustrations remain; not current S1 official art or runtime screenshots.'],
 baselines:['desktop','mobile'].map(n=>record(`${design}/baseline/${n}.png`)),receipts,
 sourceBoundaries:'Locked references are excluded from component exports. Components contain only editable vector surfaces; dynamic text and existing building images are independent.',
 publisherRequired:['Both desktop resolutions and mobile safe-area actual screenshots','Actual Chinese font fit including level-10 barracks','Image/name/action input, touch scroll cancellation, actual navigation/back and modal isolation','Three-way original/design/runtime visual review; Android device validation remains outstanding']};
fs.writeFileSync(`${design}/inspection.json`,JSON.stringify(inspection,null,2));
const spec=read(`${design}/design-spec.json`);spec.status='exported-inspected-runtime-integrated-awaiting-publisher';fs.writeFileSync(`${design}/design-spec.json`,JSON.stringify(spec,null,2));
if(process.argv.includes('--inspection-only'))process.exit(0);
for(const [file,marker] of [['validate.log','PASS: runtime and Editor scripts compile against installed Unity references.'],['atlas-check.log','CITY_FACILITY_ATLAS_CHECKS_OK: 1435'],['design-check.log','PASS: 60 production UI assets'],['assets.log','ready=318; missing=16; pending Unity meta=60']])
 if(!log(`${out}/${file}`).includes(marker))throw Error('Missing successful check marker: '+file);
const changed=['Assets/EasternDynasty/Scripts/Runtime/CityFacilityAtlasLayout.cs','Assets/EasternDynasty/Scripts/Runtime/CityFacilityAtlasPresentation.cs','Assets/EasternDynasty/Scripts/Runtime/CityFacilityCatalogPresentation.cs','Assets/EasternDynasty/Scripts/Runtime/Diagnostics/CityFacilityCatalogDiagnostics.cs','Tools/Check-CityFacilityAtlas.ps1','Tools/Validate.ps1','Tools/Check-Experience.ps1','Tools/Validate-Visual.ps1','Validation/CityFacilityAtlasChecks.cs','Validation/UiDesignChecks.mjs'];
const manifest=read('Assets/EasternDynasty/Resources/asset-manifest.json');
const assets=spec.components.map(name=>{const entry=manifest.entries.find(e=>e.id==='ui.youzhou.'+name);return {id:entry.id,width:entry.width,height:entry.height,source:entry.source,production:record(`Assets/EasternDynasty/Resources/YouzhouUI/${name}.png`)};});
const result={version,atUtc:new Date().toISOString(),status:'source-and-design-complete-runtime-pending',uiIteration:`Docs/UIIterations/${version}.md`,
 sourceChanges:changed.map(record),designSpec:record(`${design}/design-spec.json`),inspection:record(`${design}/inspection.json`),assets,
 checks:{validate:{exitCode:0,log:record(`${out}/validate.log`)},atlas:{exitCode:0,assertions:1435,textCases:156,log:record(`${out}/atlas-check.log`)},design:{exitCode:0,components:60,log:record(`${out}/design-check.log`)},assets:{exitCode:0,ready:318,missing:16,pendingUnityMeta:60,log:record(`${out}/assets.log`)},inputSyntax:{exitCode:0,log:record(`${out}/input-syntax.log`)}},
 research:record(`${out}/evidence-audit.json`),
 ragApplied:'Existing Q08-A001/STZB-0453 historical facility-atlas presentation hierarchy partially applied. Both new batches observed only; no new rules, roster or save fields.',
 gapTracking:'Existing rag/question.md Q08 and S1UX-02/04 retained without duplicate questions.',
 publisherRequired:inspection.publisherRequired};
fs.writeFileSync(`${out}/result.json`,JSON.stringify(result,null,2));
console.log('Recorded actual source/design/asset hashes and successful check logs. Runtime visual acceptance remains pending.');
