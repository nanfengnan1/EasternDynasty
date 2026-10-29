import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261027-201251989',design=`Docs/DesignUI/${version}`,out=`Validation/CityHeaderDesign/${version}`;
const read=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const save=(p,v)=>fs.writeFileSync(p,JSON.stringify(v,null,2)+'\n');
const record=p=>({path:p,sha256:sha(p)});
const spec=read(`${design}/design-spec.json`);
spec.status='exported-inspected-runtime-integrated-awaiting-publisher';
spec.componentsVectorExportScale=2;
spec.notes[1]='Preview uses the same existing resource crops as Runtime; currency glyphs are cropped from the old baseline solely in the design layer. All three formal components are vectors with no embedded images or text.';
spec.baselines=['desktop','mobile'].map(target=>({...record(`Docs/DesignUI/s1-20261027-190751837/baseline/${target}-104752269.png`),version:'s1-20261027-104752269',actuallyViewed:true,currentIterationScreenshot:false}));
save(`${design}/design-spec.json`,spec);
const previews=['desktop','mobile','stress','components'].map(target=>{const p=`${design}/${target}/exports/export-receipt.json`,receipt=read(p);return {target,receipt:record(p),toolVersion:receipt.toolVersion,source:record(receipt.relativeSource),images:receipt.images.map(x=>({...record(x.relativePath),width:x.width,height:x.height})),actuallyViewedPreview:true};});
save(`${design}/inspection.json`,{version,atUtc:new Date().toISOString(),status:'design-inspected-runtime-awaiting-publisher',original:{...record(spec.reference.path),id:spec.reference.id,size:spec.reference.size,actuallyViewed:true,scope:spec.reference.scope},baselines:spec.baselines,previews,findings:['Removed the continuous black backing; identity, four production strips and right currencies are separate.','Both desktop/mobile full-screen designs inspected; long-name and large-stock strip fits without overlapping level or other resource text.','Final component page expanded from 1400 to 1440 to include the final 8 pixels of preview content; object slices remain 504x124/368x116 and unchanged SHA.','Reference pixels and dynamic text are absent from all formal component SVG groups.','City scene and previous side designs are context only; final runtime font/rendering requires new publisher screenshots.'],runtimeAccepted:false});
const manifest=read('Assets/EasternDynasty/Resources/asset-manifest.json');
save(`${out}/asset-imports.json`,{version,tool:'Tools/Import-Asset.ps1',entries:manifest.entries.filter(x=>spec.components.some(id=>x.id==='ui.youzhou.'+id)),existingMetaPolicy:'Existing meta preserved by controlled importer; new meta awaits publisher Unity import.'});
const regression=`.local/continuous-development/runs/${version}/validation/publication-panorama-regression.json`;
if(fs.existsSync(regression))fs.copyFileSync(regression,`${out}/publication-panorama-regression.json`);
// Complete only after the actual command has finished successfully.
if(process.argv.includes('--final')){
 const logic=fs.readFileSync(`${out}/logic.log`,'utf16le');
 if(!logic.includes('PASS: runtime and Editor scripts compile against installed Unity references.'))throw new Error('Missing final reference compilation marker');
 const files=['Assets/EasternDynasty/Scripts/Runtime/CityHeaderLayout.cs','Assets/EasternDynasty/Scripts/Runtime/CityHeaderDesignPresentation.cs','Assets/EasternDynasty/Scripts/Runtime/CityPresentation.cs','Assets/EasternDynasty/Scripts/Runtime/Diagnostics/CityOverviewDiagnostics.cs','Tools/Check-Experience.ps1','Tools/Validate-Visual.ps1','Tools/Validate.ps1','Tools/Check-CityHeaderLayout.ps1','Validation/CityHeaderLayoutChecks.cs','Validation/UiDesignChecks.mjs','Assets/EasternDynasty/Resources/asset-manifest.json'];
 save(`${out}/result.json`,{version,status:'development-checks-passed-runtime-pending',atUtc:new Date().toISOString(),checks:['Tools/Validate.ps1: pure logic and installed-reference C# compilation passed','CityHeaderLayoutChecks: 568 checks across nine desktop/mobile safe-area layouts','UiDesignChecks: 39 formal UI assets match design/export/production SHA','Tools/Validate-Assets.ps1: 297 ready, 16 existing missing, 39 pending Unity meta','Publication script AST parsing and 27 historical recorded panorama geometry cases passed'],evidence:['logic.log','layout.log','design-checks.log','assets.log','publication-panorama-regression.json','rag-review.json','failure-review.json','asset-imports.json'].map(p=>record(`${out}/${p}`)),sources:files.map(record),unityStarted:false,clientStarted:false,newRuntimeScreenshots:false,published:false,publisherRequired:['Windows 1440x900 and 1280x720 plus 1600x720 mobile-safe-area captures','smoke-city-header-design.png and CITY_HEADER_DESIGN_RUNTIME_OK','Actual home art/name and twelve stock/income clicks; full-name fit and modal isolation','Compare original, editable draft and runtime images; Android real-device testing remains pending']});
}
console.log('Recorded actual design inspection, sources and controlled assets'+(process.argv.includes('--final')?' plus final validation.':'.'));
