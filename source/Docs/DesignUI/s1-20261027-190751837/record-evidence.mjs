import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261027-190751837',dir=path.resolve('Docs/DesignUI',version);
const read=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const write=(name,value)=>fs.writeFileSync(path.join(dir,name),JSON.stringify(value,null,2)+'\n');
const spec=read(path.join(dir,'design-spec.json'));
const baselineRoot='F:/EasternDynastyBuild/Releases/s1-20261027-104752269/Project/Validation/Visual/';
spec.status='exported-inspected-runtime-integrated-awaiting-publisher';
spec.componentsVectorExportScale=2;
spec.baselines=['youzhou-1440x900','mobile-1600x720'].map((target,i)=>{
 const sourcePath=baselineRoot+'s1-20261027-104752269-'+target+'/smoke-city-developed.png';
 const p=path.join(dir,'baseline',(i===0?'desktop':'mobile')+'-104752269.png');
 return {path:path.relative(process.cwd(),p),sourcePath,sha256:sha(p),actuallyViewed:true,currentIterationScreenshot:false};
});
write('design-spec.json',spec);
const previews=['desktop','mobile','states','components'].map(target=>{
 const receiptPath=path.join(dir,target,'exports/export-receipt.json'),receipt=read(receiptPath);
 if(sha(receipt.source)!==receipt.sourceSha256.toLowerCase())throw new Error('Source mismatch '+target);
 const preview=receipt.images.find(x=>x.relativePath.endsWith('-preview.png'));
 if(sha(preview.path)!==preview.sha256.toLowerCase())throw new Error('Preview mismatch '+target);
 return {target,source:receipt.relativeSource,sourceSha256:receipt.sourceSha256,receipt:path.relative(process.cwd(),receiptPath),preview:preview.relativePath,previewSha256:preview.sha256,toolVersion:receipt.toolVersion,actuallyViewed:true};
});
const manifest=read('Assets/EasternDynasty/Resources/asset-manifest.json');
const assets=spec.components.map(name=>{
 const asset=manifest.entries.find(e=>e.id==='ui.youzhou.'+name);
 const p='Assets/EasternDynasty/Resources/'+asset.resourcePath+'.png';
 if(sha(p)!==asset.sha256)throw new Error('Production mismatch '+name);
 return {id:asset.id,path:p,sha256:asset.sha256,width:asset.width,height:asset.height,sourceReference:asset.source.reference,metaPresent:fs.existsSync(p+'.meta')};
});
write('inspection.json',{version,recordedAtUtc:new Date().toISOString(),status:'design-inspected-and-integrated-runtime-review-pending',originalsActuallyViewed:['STZB-0428','STZB-0429'],baselines:spec.baselines,previews,assets,
 differencesBefore:['Full-height black left rail forms a hard divider instead of separate round entrances.','60x48 icon bounds keep the image smaller than its large rectangular parent.','Selected page and back action use flat rectangular fills and an underline; the reference separates circular artwork from the lower caption.'],
 inspection:['Viewed desktop/mobile compositions and four-state minimum-height sheet. Five image/caption pairs and bottom return are separate without overlap.','Viewed final component export: independent transparent silhouettes with normal/active/hover states. 2x outputs come directly from editable vector paths.','No source screenshot pixels in the five production textures. All city/building artwork and district/document symbols are reused.','Preview context uses old runtime screenshots plus prior accepted design exports. It is not a screenshot of this iteration.','Read-only review found a 1-pixel-wide vector stroke overflow. Rounded joins were re-exported and viewed; final seals are exactly 168x172 and return components 168x128.','The publisher refreshed the old 104752269 mobile screenshot during development. Both baseline copies were viewed again and preserved inside this design directory; preview context remains the explicitly identified prior-iteration design export.'],
 limitations:['The reference is 2015 historical city UI; current S1 applicability unknown. The five-entry left dock is a project adaptation.','Actual Unity font fit, texture import, screenshots and desktop/mobile input await the publisher. Android hardware remains untested.','Inkscape exits successfully but stderr reports denied profile/recent-file writes outside workspace; export files, geometry and hashes are valid and logs retained.']});
console.log('Recorded actual design receipts, viewed previews, baseline hashes and five production asset identities. Runtime review remains pending.');
