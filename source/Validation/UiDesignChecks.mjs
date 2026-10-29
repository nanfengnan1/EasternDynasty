// Verify production UI pixels are traceable to a real design-tool export.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const root=path.resolve(import.meta.dirname,'..');
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const read=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const check=(ok,message)=>{if(!ok)throw new Error(message);};
const resolve=p=>{const full=path.resolve(root,p);check(full.startsWith(root+path.sep),'Design path leaves project');return full;};
const manifest=read(resolve('Assets/EasternDynasty/Resources/asset-manifest.json'));
{
 const directory='Docs/DesignUI/s1-20261029-010252300';
 for(const target of ['desktop','mobile','states','components']){
  const receipt=read(resolve(`${directory}/${target}/exports/export-receipt.json`));
  check(receipt.toolVersion.startsWith('Inkscape '),'Skill sheet requires an actual Inkscape export: '+target);
  check(resolve(receipt.relativeSource)===resolve(`${directory}/${target}/skill-sheet.svg`),'Skill sheet receipt references another source: '+target);
  const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
  check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Skill sheet source changed after export: '+target);
  check(receipt.images.length>0,'Skill sheet has no actual exported preview: '+target);
  for(const output of receipt.images){
   const bytes=fs.readFileSync(resolve(output.relativePath));
   check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Skill sheet export bytes differ from receipt: '+target);
   check(bytes.length>=24&&bytes.subarray(0,8).equals(Buffer.from([137,80,78,71,13,10,26,10])),'Skill sheet output is not a PNG: '+target);
   check(bytes.readUInt32BE(16)===output.width&&bytes.readUInt32BE(20)===output.height,'Skill sheet PNG dimensions differ from receipt: '+target);
  }
  const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
  check(new Set(ids).size===ids.length&&!/<script\b/i.test(source),'Skill sheet layers must retain unique IDs and inert content');
  if(target==='components')check(!/<(?:image|text)\b/i.test(source),'Skill sheet formal surfaces must not contain images or baked text');
  else {
   const layers=[...source.matchAll(/<g\b[^>]*\binkscape:groupmode="layer"[^>]*>/g)].map(x=>x[0].match(/\bid="([^"]+)"/)?.[1]);
   const parts=['surfaces','images','live-text','state','hit-regions'];
   if(target==='desktop'||target==='mobile')
    for(const id of ['reference-original','baseline-context',...parts])check(layers.includes(id),'Skill sheet lost independent editable layer: '+target+'/'+id);
   else for(const prefix of ['preview-','owned-','missing-'])
    for(const part of parts)check(layers.includes(prefix+part),'Skill sheet state lost independent editable layer: '+prefix+part);
  }
 }
 const componentReceiptPath=`${directory}/components/exports/export-receipt.json`;
 const componentReceipt=read(resolve(componentReceiptPath));
 for(const [name,objectId,w,h] of [['skill_sheet','skill-sheet',1240,480],['skill_learning','skill-learning',1240,168]]){
  const entry=manifest.entries.find(e=>e.id==='ui.youzhou.'+name);
  check(entry&&entry.kind==='ui-sprite'&&entry.status==='ready'&&entry.width===w&&entry.height===h,'Skill sheet formal component missing or has incorrect dimensions: '+name);
  check(resolve(entry.source.reference)===resolve(componentReceiptPath),'Skill sheet production asset lost its actual export receipt: '+name);
  const exportedPath=resolve(`${directory}/components/exports/skill-sheet-${objectId}.png`);
  const output=componentReceipt.images.find(i=>resolve(i.relativePath)===exportedPath);
  check(output&&output.width===w&&output.height===h&&output.sha256.toLowerCase()===entry.sha256.toLowerCase(),'Skill sheet production asset differs from its named Inkscape object: '+name);
  const productionPath=resolve('Assets/EasternDynasty/Resources/'+entry.resourcePath+'.png');
  check(sha(productionPath)===entry.sha256.toLowerCase()&&sha(productionPath)===sha(exportedPath),'Skill sheet imported PNG bytes differ from the actual Inkscape export: '+name);
 }
 console.log('PASS: skill sheet desktop/mobile/three-state/component sources, independent editable layers and real Inkscape SHA/dimension chains match both formal PNG assets. Runtime acceptance remains publisher work.');
}
{
 const directory='Docs/DesignUI/s1-20261028-235752015';
 for(const target of ['desktop','mobile','states','components']){
  const receipt=read(resolve(`${directory}/${target}/exports/export-receipt.json`));
  check(receipt.toolVersion.startsWith('Inkscape '),'Task sleeve needs actual Inkscape export');
  const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
  check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Task sleeve source changed after export');
  for(const output of receipt.images)check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Task sleeve export bytes changed');
  const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
  check(new Set(ids).size===ids.length&&!source.includes('<script'),'Task sleeve must retain unique inert layers');
  if(target==='components')check(!source.includes('<image')&&!source.includes('<text'),'Task sleeve production surfaces must not bake screenshot pixels or text');
  else for(const prefix of target==='states'?['active-','ready-','complete-','long-','hover-','disabled-']:[''])
   for(const part of ['quest-surfaces','quest-images','quest-live-labels','quest-state','quest-hit-regions'])check(source.includes(`id="${prefix+part}"`),'Task sleeve lost editable layer');
 }
 const spec=read(resolve(`${directory}/design-spec.json`));
 check(spec.layout.panel[3]===118&&spec.layout.book.join(',')==='20,90,80,68'&&spec.layout.body.join(',')==='110,90,264,68','Task sleeve composition contract changed');
 check(spec.baselines[0].path!==spec.baselines[1].path,'Task sleeve requires separate mobile baseline');
 for(const reused of spec.reusedAssets)check(sha(resolve(reused.path))===reused.sha256,'Task sleeve reused asset changed');
 for(const id of spec.components){
  const entry=manifest.entries.find(e=>e.id===id);
  check(entry&&entry.width===528&&entry.height===136,'Task sleeve needs actual 528x136 exported textures');
  check(resolve(entry.source.reference)===resolve(`${directory}/components/exports/export-receipt.json`),'Task sleeve lost export provenance');
  check(read(resolve(entry.source.reference)).images.some(i=>i.sha256.toLowerCase()===entry.sha256),'Task sleeve production pixels differ from Inkscape export');
 }
 console.log('PASS: detached task book, desktop/mobile/six-state editable designs and both formal Inkscape source/PNG SHA chains. Runtime acceptance pending.');
}
{
 const directory='Docs/DesignUI/s1-20261028-225251901';
 for(const target of ['desktop','mobile','states','components']){
  const receipt=read(resolve(`${directory}/${target}/exports/export-receipt.json`));
  check(receipt.toolVersion.startsWith('Inkscape '),'Recruitment pedestal needs a real Inkscape export');
  check(resolve(receipt.relativeSource)===resolve(`${directory}/${target}/recruit.svg`),'Recruitment receipt references another design');
  const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
  check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Recruitment design changed after export');
  for(const output of receipt.images){
   const bytes=fs.readFileSync(resolve(output.relativePath));
   check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Recruitment export bytes changed');
   check(bytes.readUInt32BE(16)===output.width&&bytes.readUInt32BE(20)===output.height,'Recruitment receipt dimensions differ');
  }
  const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
  check(new Set(ids).size===ids.length&&!source.includes('<script'),'Recruitment layers must be unique and inert');
  if(target==='components')check(!source.includes('<image')&&!source.includes('<text'),'Formal recruitment frames must not contain screenshots, portraits or live text');
  else for(const prefix of target==='states'?['state-0-','state-1-','state-2-']:[''])
   for(const part of ['frame','portrait','live-title','hit-region'])check(ids.includes(prefix+part),'Recruitment layer missing: '+prefix+part);
  if(target==='states')check(ids.includes('selected-land-compact'),'Recruitment design must document unobscured compact mode while land commands are open');
 }
 const spec=read(resolve(`${directory}/design-spec.json`));
 check(spec.baselines.length===2&&spec.baselines[0].path!==spec.baselines[1].path,'Recruitment requires independent desktop/mobile baselines');
 check(spec.layout.entry.join(',')==='width-180,height-144,160,140'&&spec.layout.art.join(',')==='50,10,60,88'&&spec.layout.caption.join(',')==='10,114,140,24','Recruitment geometry differs from inspected source');
 check(sha(resolve(spec.portrait.path))===spec.portrait.sha256,'Recruitment illustration identity or source changed');
 for(const id of spec.components){
  const entry=manifest.entries.find(e=>e.id===id);
  check(entry&&entry.kind==='ui-sprite'&&entry.status==='ready'&&entry.width===320&&entry.height===280,'Recruitment formal export missing');
  check(resolve(entry.source.reference)===resolve(`${directory}/components/exports/export-receipt.json`),'Recruitment asset lost actual source receipt');
  check(read(resolve(entry.source.reference)).images.some(i=>i.sha256.toLowerCase()===entry.sha256.toLowerCase()&&i.width===320&&i.height===280),'Recruitment production pixels differ from actual export');
 }
 console.log('PASS: recruitment desktop/mobile/four-state design, four actual Inkscape receipts and two formal PNG SHA chains; runtime remains publisher acceptance.');
}
{
 const directory='Docs/DesignUI/s1-20261028-214751426';
 for(const target of ['desktop','mobile','states','components']){
  const receipt=read(resolve(`${directory}/${target}/exports/export-receipt.json`));
  check(receipt.toolVersion.startsWith('Inkscape '),'Card chrome requires an actual Inkscape export');
  check(resolve(receipt.relativeSource)===resolve(`${directory}/${target}/chrome.svg`),'Card chrome receipt points to another design');
  const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
  check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Card chrome source changed after export: '+target);
  for(const output of receipt.images){
   const bytes=fs.readFileSync(resolve(output.relativePath));
   check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Card chrome export bytes changed');
   check(bytes.readUInt32BE(16)===output.width&&bytes.readUInt32BE(20)===output.height,'Card chrome export dimensions differ');
  }
  const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
  check(new Set(ids).size===ids.length&&!source.includes('<script'),'Card chrome layers must be unique and inert');
  if(target==='components')check(!source.includes('<image')&&!source.includes('<text'),'Card chrome production skin cannot bake reference pixels or text');
  else for(const prefix of target==='states'?['preview-','owned-','disabled-']:[''])
   for(const part of ['chrome-surfaces','chrome-images','chrome-live-labels','chrome-state','chrome-hit-regions'])check(ids.includes(prefix+part),'Missing independently editable card chrome layer '+prefix+part);
 }
 const spec=read(resolve(`${directory}/design-spec.json`));
 check(spec.baselines.length===2&&spec.baselines[0].path!==spec.baselines[1].path,'Card chrome requires separate desktop/mobile baselines');
 for(const baseline of spec.baselines)check(sha(resolve(baseline.path))===baseline.sha256,'Card chrome frozen baseline changed');
 for(const [name,w,h] of [['card_tabs',600,54],['card_tab_active',200,54],['card_identity',600,40],['card_close',54,54],['card_action',240,54],['card_action_hover',240,54]]){
  const entry=manifest.entries.find(e=>e.id==='ui.youzhou.'+name);
  check(entry&&entry.status==='ready'&&entry.kind==='ui-sprite'&&entry.width===w&&entry.height===h,'Missing card chrome exported component '+name);
  check(resolve(entry.source.reference)===resolve(`${directory}/components/exports/export-receipt.json`),'Card chrome lost its source receipt');
  const receipt=read(resolve(entry.source.reference));
  check(receipt.images.some(i=>i.sha256.toLowerCase()===entry.sha256.toLowerCase()&&i.width===w&&i.height===h),'Card chrome production pixels differ from Inkscape export');
 }
 console.log('PASS: card chrome four editable designs, frozen desktop/mobile context, real export receipts and six production PNG SHA chains. Runtime visual review remains required.');
}
{
 const directory='Docs/DesignUI/s1-20261028-204251299';
 for(const target of ['desktop','mobile','states','components']){
  const receipt=read(resolve(`${directory}/${target}/exports/export-receipt.json`));
  check(receipt.toolVersion.startsWith('Inkscape '),'City ledger requires actual Inkscape export');
  check(resolve(receipt.relativeSource)===resolve(`${directory}/${target}/city-header.svg`),'City ledger receipt points to another source');
  const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
  check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'City ledger source changed after export: '+target);
  for(const output of receipt.images){
   const bytes=fs.readFileSync(resolve(output.relativePath));
   check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'City ledger export bytes changed');
   check(bytes.readUInt32BE(16)===output.width&&bytes.readUInt32BE(20)===output.height,'City ledger export dimensions differ from receipt');
  }
  const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
  check(ids.length===new Set(ids).size&&!source.includes('<script'),'City ledger layers must be unique and non-executable');
  if(target==='components')check(!source.includes('<image')&&!source.includes('<text'),'City backing must not bake source images or live text');
  else for(const prefix of target==='states'?['normal-','hover-','modal-']:[''])
   for(const layer of ['header-backdrop','header-surfaces','header-images','header-live-labels','header-state','header-hit-regions'])check(ids.includes(prefix+layer),'Missing independent city ledger layer '+prefix+layer);
 }
 const spec=read(resolve(`${directory}/design-spec.json`));
 check(spec.baselines.length===2&&spec.baselines[0].path!==spec.baselines[1].path,'City ledger requires distinct desktop/mobile baselines');
 for(const asset of spec.reusedAssets)check(sha(resolve(asset.path))===asset.sha256.toLowerCase(),'Shared city ledger component changed after inspection');
 const entry=manifest.entries.find(e=>e.id==='ui.youzhou.city_header_backdrop');
 check(entry&&entry.kind==='ui-sprite'&&entry.status==='ready'&&entry.width===1440&&entry.height===70&&!entry.hasTransparency,'City band needs the full opaque formal backdrop');
 check(resolve(entry.source.reference)===resolve(`${directory}/components/exports/export-receipt.json`),'City backing lost its design receipt');
 check(read(resolve(entry.source.reference)).images.some(i=>i.sha256.toLowerCase()===entry.sha256.toLowerCase()&&i.width===1440&&i.height===70),'City backing pixels differ from actual Inkscape output');
 console.log('PASS: city ledger desktop/mobile/states, complete opaque backing and reused component SHA chains; actual cross-page pixels remain publisher acceptance.');
}
{
 const directory='Docs/DesignUI/s1-20261028-193752561';
 for(const target of ['desktop','mobile','states','components']){
  const receipt=read(resolve(`${directory}/${target}/exports/export-receipt.json`));
  check(receipt.toolVersion.startsWith('Inkscape '),'Header ledger requires actual Inkscape export');
  check(resolve(receipt.relativeSource)===resolve(`${directory}/${target}/header.svg`),'Header receipt points to another source');
  const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
  check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Header source changed after export: '+target);
  for(const output of receipt.images){
   const bytes=fs.readFileSync(resolve(output.relativePath));
   check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Header export bytes changed');
   check(bytes.readUInt32BE(16)===output.width&&bytes.readUInt32BE(20)===output.height,'Header export dimensions differ from receipt');
  }
  const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
  check(ids.length===new Set(ids).size&&!source.includes('<script'),'Header source has duplicate IDs or executable content');
  if(target==='components')check(!source.includes('<image')&&!source.includes('<text'),'Formal header skin must not bake screenshots or text');
  else for(const prefix of target==='states'?['normal-','hover-','modal-']:[''])
   for(const layer of ['header-surfaces','header-images','header-live-labels','header-state','header-hit-regions'])check(ids.includes(prefix+layer),'Missing editable header layer '+prefix+layer);
 }
 const spec=read(resolve(`${directory}/design-spec.json`));
 check(spec.baselines.length===2&&spec.baselines[0].path!==spec.baselines[1].path,'Header requires separate real desktop and mobile baselines');
 for(const asset of spec.reusedAssets)check(sha(resolve(asset.path))===asset.sha256.toLowerCase(),'Reused header asset changed after inspection');
 for(const [name,w,h] of [['header_identity',492,124],['header_ledger',1344,116]]){
  const entry=manifest.entries.find(e=>e.id==='ui.youzhou.'+name);
  check(entry&&entry.kind==='ui-sprite'&&entry.status==='ready'&&entry.width===w&&entry.height===h,'Missing exact header export '+name);
  check(resolve(entry.source.reference)===resolve(`${directory}/components/exports/export-receipt.json`),'Header asset lost its actual design receipt');
  check(read(resolve(entry.source.reference)).images.some(i=>i.sha256.toLowerCase()===entry.sha256.toLowerCase()&&i.width===w&&i.height===h),'Header formal pixels differ from Inkscape output');
 }
 console.log('PASS: header identity/contiguous ledger editable desktop/mobile/states, four Inkscape receipts and two formal PNG SHA chains; runtime acceptance pending.');
}
{
 const directory='Docs/DesignUI/s1-20261028-183252245';
 for(const target of ['desktop','mobile','states','components']){
  const receipt=read(resolve(`${directory}/${target}/exports/export-receipt.json`));
  check(receipt.toolVersion.startsWith('Inkscape '),'Atlas sidebar requires an actual Inkscape export');
  check(resolve(receipt.relativeSource)===resolve(`${directory}/${target}/atlas.svg`),'Atlas sidebar receipt refers to another editable source');
  const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
  check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Atlas sidebar source changed after export: '+target);
  check(receipt.images.length>0,'Atlas sidebar has no exported preview');
  for(const output of receipt.images){
   const file=resolve(output.relativePath),bytes=fs.readFileSync(file);
   check(sha(file)===output.sha256.toLowerCase(),'Atlas sidebar export bytes changed: '+target);
   check(bytes.readUInt32BE(16)===output.width&&bytes.readUInt32BE(20)===output.height,'Atlas sidebar PNG dimensions differ from receipt');
  }
  const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
  check(new Set(ids).size===ids.length&&!source.includes('<script'),'Atlas sidebar has duplicate IDs or executable content');
  if(target==='components'){
   check(!source.includes('<image')&&!source.includes('<text'),'Atlas sidebar components must not bake screenshots or live text');
   for(const id of ['atlas-locate-dark','atlas-locate-dark-hover','atlas-pin-light'])check(ids.includes(id),'Missing independently editable atlas component '+id);
  } else for(const prefix of target==='states'?['normal-','hover-','disabled-']:[''])
   for(const part of ['atlas-surfaces','atlas-images','atlas-live-labels','atlas-state','atlas-hit-regions','atlas-terrain-context'])
    check(ids.includes(prefix+part),'Atlas sidebar layer is not independently editable: '+prefix+part);
 }
 const spec=read(resolve(`${directory}/design-spec.json`));
 for(const [name,expected] of Object.entries({panel:'width-302,90,282,204',terrain:'0,0,204,204',title:'210,0,72,54',locate:'216,64,60,60',pin:'236,70,20,28',caption:'216,101,60,20',legend:'210,134,72,70'}))
  check(spec.layout[name].join(',')===expected,'Atlas sidebar differs from the inspected design contract: '+name);
 check(spec.layout.terrainOpacity===.88,'Atlas live terrain opacity changed from the inspected design');
 check(spec.baselines.length===2&&spec.baselines[0].path!==spec.baselines[1].path,'Atlas sidebar needs separate actual desktop and mobile baselines');
 for(const reused of spec.reusedAssets)check(sha(resolve(reused.path))===reused.sha256.toLowerCase(),'Atlas reused component changed after design');
 const components=[['atlas_locate_dark',120,120],['atlas_locate_dark_hover',120,120],['atlas_pin_light',40,56]];
 check(spec.components.slice().sort().join(',')===components.map(x=>'ui.youzhou.'+x[0]).sort().join(','),'Atlas sidebar production component identities changed');
 for(const [name,w,h] of components){
  const entry=manifest.entries.find(e=>e.id==='ui.youzhou.'+name);
  check(entry&&entry.kind==='ui-sprite'&&entry.status==='ready'&&entry.width===w&&entry.height===h,'Missing exact exported atlas sidebar component: '+name);
  check(resolve(entry.source.reference)===resolve(`${directory}/components/exports/export-receipt.json`),'Atlas sidebar component lost its actual export receipt');
  const receipt=read(resolve(entry.source.reference));
  check(receipt.images.some(i=>i.sha256.toLowerCase()===entry.sha256.toLowerCase()&&i.width===w&&i.height===h),'Atlas sidebar production pixels differ from actual Inkscape export');
 }
 console.log('PASS: atlas sidebar desktop/mobile/three-state/component sources, exact full-terrain geometry, four real Inkscape receipts and three formal PNG SHA chains. Runtime input, fonts and visual review remain publisher checks.');
}
{
 const directory='Docs/DesignUI/s1-20261028-172751912';
 for(const target of ['desktop','mobile','city-desktop','city-mobile','states','components']){
  const receipt=read(resolve(`${directory}/${target}/exports/export-receipt.json`));
  check(receipt.toolVersion.startsWith('Inkscape '),'Navigation captions require a real design-tool export');
  const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
  check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Navigation source changed after export: '+target);
  for(const output of receipt.images)check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Navigation export bytes changed');
  const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
  check(new Set(ids).size===ids.length,'Duplicate navigation design IDs');
  if(target==='components')check(!source.includes('<image')&&!source.includes('<text'),'Navigation skin must not bake reference pixels or dynamic text');
  else for(const prefix of target==='states'?['map-','city-','modal-']:[''])
   for(const part of ['navigation-surfaces','navigation-images','navigation-live-labels','navigation-state','navigation-hit-regions'])
    check(source.includes(`id="${prefix+part}"`),'Navigation layers are not independently editable');
 }
 const spec=read(resolve(`${directory}/design-spec.json`));
 check(spec.layout.art.join(',')==='4,3,100,42'&&spec.layout.caption.join(',')==='4,46,100,22','Navigation image/caption design contract changed');
 check(spec.baselines[0].path!==spec.baselines[1].path&&spec.cityBaselines.length===2,'Navigation needs separate map/city mobile baselines');
 for(const reused of spec.reusedAssets)check(sha(resolve(reused.path))===reused.sha256,'Navigation illustration changed after design');
 for(const id of spec.components){
  const entry=manifest.entries.find(e=>e.id===id);
  check(entry&&entry.width===216&&entry.height===140,'Navigation panel requires exact exported size');
  check(resolve(entry.source.reference)===resolve(`${directory}/components/exports/export-receipt.json`),'Navigation formal skin lost actual receipt');
  check(read(resolve(entry.source.reference)).images.some(i=>i.sha256.toLowerCase()===entry.sha256),'Navigation production pixels differ from actual Inkscape output');
 }
 console.log('PASS: map/city navigation caption designs, six actual Inkscape exports and two formal source/PNG SHA chains. Runtime acceptance pending.');
}
{
 const directory='Docs/DesignUI/s1-20261028-162252252';
 for(const target of ['desktop','mobile','states','components']){
  const receipt=read(resolve(`${directory}/${target}/exports/export-receipt.json`));
  check(receipt.toolVersion.startsWith('Inkscape '),'Task docket requires actual Inkscape export');
  const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
  check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Task docket source changed after export');
  for(const output of receipt.images)check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Task docket preview changed after export');
  const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
  check(new Set(ids).size===ids.length,'Duplicate task design layer IDs');
  if(target==='components')check(!source.includes('<image')&&!source.includes('<text'),'Task skin must not bake reference pixels or dynamic text');
  else for(const prefix of target==='states'?['active-','ready-','complete-','long-']:[''])
   for(const part of ['quest-surfaces','quest-images','quest-live-labels','quest-state','quest-hit-regions'])check(source.includes(`id="${prefix+part}"`),'Task design lost independent editable layer');
 }
 const spec=read(resolve(`${directory}/design-spec.json`));
 check(spec.baselines[0].path!==spec.baselines[1].path,'Mobile task design needs its own real baseline');
 for(const reused of spec.reusedAssets)check(sha(resolve(reused.path))===reused.sha256,'Task reused illustration changed after design');
 const entry=manifest.entries.find(e=>e.id==='ui.youzhou.quest_docket');
 check(entry&&entry.width===708&&entry.height===164,'Task docket formal skin missing or changed size');
 check(resolve(entry.source.reference)===resolve(`${directory}/components/exports/export-receipt.json`),'Task skin lost real export receipt');
 check(read(resolve(entry.source.reference)).images.some(i=>i.sha256.toLowerCase()===entry.sha256),'Task production pixels differ from actual Inkscape output');
 console.log('PASS: task docket editable desktop/mobile/four-state designs and formal source/export SHA chain. Runtime review pending.');
}
const armyRows='Docs/DesignUI/s1-20261028-151752217';
for(const target of ['desktop','mobile','states','components']){
 const receipt=read(resolve(`${armyRows}/${target}/exports/export-receipt.json`));
 check(receipt.toolVersion.startsWith('Inkscape '),'Army rows require an actual Inkscape export');
 const file=resolve(receipt.relativeSource),source=fs.readFileSync(file,'utf8');
 check(sha(file)===receipt.sourceSha256.toLowerCase(),'Army row design changed after export: '+target);
 for(const output of receipt.images){
  const bytes=fs.readFileSync(resolve(output.relativePath));
  check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Army row export pixels changed');
  check(bytes.readUInt32BE(16)===output.width&&bytes.readUInt32BE(20)===output.height,'Army row PNG dimensions mismatch');
 }
 const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
 check(new Set(ids).size===ids.length&&!source.includes('<script'),'Invalid editable army row source');
 if(target==='components')check(!source.includes('<image')&&!source.includes('<text'),'Army skin must not bake reference pixels or live text');
 else for(const prefix of target==='states'?['idle-','long-','empty-','training-','returning-','hover-']:[''])
  for(const id of ['army-surfaces','army-images','army-live-labels','army-state','army-hit-regions'])
   check(source.includes(`id="${prefix+id}"`),'Army row lacks editable layer: '+prefix+id);
}
const armyRowSpec=read(resolve(`${armyRows}/design-spec.json`));
check(armyRowSpec.baselines[0].path!==armyRowSpec.baselines[1].path,'Army mobile design requires its own baseline');
for(const [key,expected] of Object.entries({area:'20,height-396,354,302',header:'0,0,354,28',card:'0,32,354,64',art:'4,1,44,62',name:'62,12,164,40',identity:'242,12,102,40',status:'0,236,354,20'}))
 check(armyRowSpec.layout[key].join(',')===expected,'Inspected army row geometry changed: '+key);
check(armyRowSpec.layout.pitch===68,'Army row gaps lost');
for(const name of ['army_row','army_row_hover']){
 const entry=manifest.entries.find(e=>e.id==='ui.youzhou.'+name);
 check(entry&&entry.status==='ready'&&entry.width===708&&entry.height===128,'Missing exact army row formal skin');
 check(resolve(entry.source.reference)===resolve(`${armyRows}/components/exports/export-receipt.json`),'Army row skin lacks export provenance');
 const receipt=read(resolve(entry.source.reference));
 check(receipt.images.some(i=>i.sha256.toLowerCase()===entry.sha256),'Army row production pixels differ from actual export');
}
console.log('PASS: army row desktop/mobile/six-state editable sources, two exact formal slices and real Inkscape SHA chain. Runtime review pending.');
const cityMarker='Docs/DesignUI/s1-20261028-130751956';
for(const target of ['desktop','mobile','states','components']){
 const receipt=read(resolve(`${cityMarker}/${target}/exports/export-receipt.json`));
 check(receipt.toolVersion.startsWith('Inkscape '),'City marker lacks actual design-tool export');
 const sourceFile=resolve(receipt.relativeSource),source=fs.readFileSync(sourceFile,'utf8');
 check(sha(sourceFile)===receipt.sourceSha256.toLowerCase(),'City marker source changed after export');
 for(const img of receipt.images)check(sha(resolve(img.relativePath))===img.sha256.toLowerCase(),'City marker preview/source mismatch');
 const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
 check(new Set(ids).size===ids.length&&!source.includes('<script'),'Invalid city marker layers');
 if(target==='components')check(!source.includes('<image')&&!source.includes('<text'),'City marker production skin contains screenshot/text');
 else for(const id of ['home-surfaces','home-city-image','home-live-labels','home-hit-regions'])check(source.includes(`id="${id}"`),'City marker lacks independent editable layer');
}
for(const [name,w] of [['crest',104],['selected',104],['ribbon',280]]){
 const entry=manifest.entries.find(e=>e.id==='ui.youzhou.city_marker_'+name);
 check(entry&&entry.width===w&&entry.height===104&&entry.status==='ready','Missing city marker formal skin');
 check(resolve(entry.source.reference)===resolve(`${cityMarker}/components/exports/export-receipt.json`),'City marker lacks export provenance');
 const receipt=read(resolve(entry.source.reference));check(receipt.images.some(i=>i.sha256.toLowerCase()===entry.sha256),'City marker pixels differ from actual export');
}
console.log('PASS: city marker editable desktop/mobile/states and actual Inkscape source/PNG provenance. Runtime review pending.');
const compactFacts='Docs/DesignUI/s1-20261028-120252024';
for(const target of ['desktop','mobile','states','components']){
 const receipt=read(resolve(`${compactFacts}/${target}/exports/export-receipt.json`));
 check(receipt.toolVersion.startsWith('Inkscape '),'Compact facts require a real design-tool export');
 const file=resolve(receipt.relativeSource),source=fs.readFileSync(file,'utf8');
 check(sha(file)===receipt.sourceSha256.toLowerCase(),'Compact fact source changed after export: '+target);
 for(const output of receipt.images){
  const bytes=fs.readFileSync(resolve(output.relativePath));
  check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Compact fact export changed: '+target);
  check(bytes.readUInt32BE(16)===output.width&&bytes.readUInt32BE(20)===output.height,'Compact fact PNG dimensions mismatch');
 }
 const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
 check(new Set(ids).size===ids.length&&!source.includes('<script'),'Invalid editable compact fact source');
 if(target==='components'){
  check(source.includes('id="land-facts"')&&!source.includes('<image')&&!source.includes('<text'),'Formal fact skin must not bake screenshot pixels or dynamic text');
 }else{
  for(const id of ['reference-original','baseline-context'])check(source.includes(`id="${id}"`),'Missing compact fact context: '+id);
  for(const prefix of target==='states'?['neutral-','city-','blocked-']:[''])
   for(const id of ['fact-surface','terrain-image','fact-symbols','live-facts','close-control','hit-regions'])
    check(source.includes(`id="${prefix+id}"`),'Missing editable compact fact layer: '+prefix+id);
 }
}
const compactSpec=read(resolve(`${compactFacts}/design-spec.json`));
for(const [key,value] of Object.entries({facts:'280,228',header:'0,0,280,148',art:'12,64,256,84',image:'12,68,156,78',identity:'178,72,90,70',summary:'16,156,248,60',defense:'16,156,248,28',yield:'16,188,248,28',label:'0,0,112,28',icon:'116,3,22,22',value:'144,0,104,28',close:'224,12,44,44'}))
 check(compactSpec.layout[key].join(',')===value,'Inspected compact fact contract changed: '+key);
check(compactSpec.baselines[0].path!==compactSpec.baselines[1].path,'Compact mobile design needs its own baseline');
const compactEntry=manifest.entries.find(e=>e.id==='ui.youzhou.land_facts');
check(compactEntry&&compactEntry.status==='ready'&&compactEntry.width===560&&compactEntry.height===456,'Missing formal 560x456 fact skin');
check(resolve(compactEntry.source.reference)===resolve(`${compactFacts}/components/exports/export-receipt.json`),'Compact fact skin has unrelated provenance');
const compactReceipt=read(resolve(compactEntry.source.reference));
check(compactReceipt.images.some(i=>i.sha256.toLowerCase()===compactEntry.sha256),'Formal fact pixels differ from actual export');
console.log('PASS: compact land facts desktop/mobile/state layers, actual Inkscape export hashes, formal component and inspected geometry. Runtime fonts/clicks remain publisher checks.');
const detachedOrders='Docs/DesignUI/s1-20261028-110251322';
for(const target of ['desktop','mobile','busy','city','neutral','blocked','components']){
 const receipt=read(resolve(`${detachedOrders}/${target}/exports/export-receipt.json`));
 check(receipt.toolVersion.startsWith('Inkscape '),'Detached orders lack an actual design-tool receipt');
 const file=resolve(receipt.relativeSource),source=fs.readFileSync(file,'utf8');
 check(sha(file)===receipt.sourceSha256.toLowerCase(),'Detached order source changed after export: '+target);
 for(const output of receipt.images){
  const bytes=fs.readFileSync(resolve(output.relativePath));
  check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Detached order export changed');
  check(bytes.readUInt32BE(16)===output.width&&bytes.readUInt32BE(20)===output.height,'Detached order PNG dimensions mismatch');
 }
 const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
 check(new Set(ids).size===ids.length&&!source.includes('<script'),'Invalid detached order design');
 for(const id of target==='components'?['land-order-components','land-order','land-order-active']:['reference-original','baseline-context','command-information-surface','command-state-text','command-resource-icons','command-controls','command-hit-regions'])
  check(source.includes(`id="${id}"`),'Missing editable detached order layer: '+id);
 if(target==='components')check(!source.includes('<image')&&!source.includes('<text'),'Production order skin must not bake screenshot pixels or dynamic text');
}
const detachedSpec=read(resolve(`${detachedOrders}/design-spec.json`));
for(const [state,height,info] of [['owned',292,116],['busy',216,96],['city',144,76],['neutral',268,200],['blocked',268,200]]){
 const c=detachedSpec.contracts[state];check(c.height===height&&c.info===info,'Inspected order geometry changed: '+state);
 check(c.ys[0]>=c.info+12&&c.ys.at(-1)+44<=c.height-12,'Order controls obscure information or leave their bounds');
}
check(detachedSpec.command.icon.join(',')==='5,5,34,34'&&detachedSpec.command.label.join(',')==='48,4,156,36','Detached command image/label budget changed');
for(const name of detachedSpec.components){
 const entry=manifest.entries.find(e=>e.id==='ui.youzhou.'+name);
 check(entry&&entry.status==='ready'&&entry.width===416&&entry.height===88,'Missing actual circular command skin');
 check(resolve(entry.source.reference)===resolve(`${detachedOrders}/components/exports/export-receipt.json`),'Command skin has unrelated provenance');
 const receipt=read(resolve(entry.source.reference));check(receipt.images.some(i=>i.sha256.toLowerCase()===entry.sha256),'Formal command pixels differ from actual export');
}
console.log('PASS: detached land desktop/mobile/five-state editable sources, two actual Inkscape exports and state-specific layout; runtime review remains pending.');
const flankingDesign='Docs/DesignUI/s1-20261028-095751175';
for(const target of ['desktop','mobile','neutral','busy','city','blocked','edge']){
 const receipt=read(resolve(`${flankingDesign}/${target}/exports/export-receipt.json`));
 check(receipt.toolVersion.startsWith('Inkscape '),'Target-relative land design lacks actual Inkscape export');
 const file=resolve(receipt.relativeSource),source=fs.readFileSync(file,'utf8');
 check(sha(file)===receipt.sourceSha256.toLowerCase(),'Target-relative land design changed after export: '+target);
 check(receipt.images.length>0,'Target-relative land design lacks a preview');
 for(const output of receipt.images){
  const bytes=fs.readFileSync(resolve(output.relativePath));
  check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Target-relative land preview changed');
  check(bytes.readUInt32BE(16)===output.width&&bytes.readUInt32BE(20)===output.height,'Target-relative PNG dimensions mismatch');
 }
 for(const id of ['reference-original','baseline-context','fact-surfaces','terrain-image','dynamic-facts','fact-hit-regions','command-surface','command-state-text','command-resource-icons','command-controls','command-hit-regions'])
  check(source.includes(`id="${id}"`),'Target-relative land design lacks editable layer: '+id);
 const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
 check(new Set(ids).size===ids.length&&!source.includes('<script'),'Invalid target-relative editable source');
}
const flankingSpec=read(resolve(`${flankingDesign}/design-spec.json`));
check(flankingSpec.layout.facts.join(',')==='280,266'&&flankingSpec.layout.commandSurface.join(',')==='240,292','Inspected independent land surfaces changed');
check(flankingSpec.layout.actions.join(',')==='16,12,208,264'&&flankingSpec.layout.commandYs.join(',')==='116,168,220','Inspected vertical action stack changed');
check(flankingSpec.layout.minimumFontSize===14&&flankingSpec.layout.preferredFontSize===18,'Land command readable font range changed');
check(flankingSpec.baselines[0].path!==flankingSpec.baselines[1].path,'Mobile land preview must use its own runtime baseline');
for(const id of flankingSpec.reusedAssets)check(manifest.entries.some(e=>e.id===id&&e.status==='ready'),'Target-relative land design uses unregistered artwork: '+id);
console.log('PASS: target-relative land desktop/mobile/five-state/edge drafts, actual Inkscape SHA and independent editable layers; existing registered skins retained. Runtime screenshots remain publisher checks.');
const landDesign='Docs/DesignUI/s1-20261028-085252514';
for(const target of ['desktop','mobile','states','busy','city','blocked','components']){
 const receipt=read(resolve(`${landDesign}/${target}/exports/export-receipt.json`));
 check(receipt.toolVersion.startsWith('Inkscape '),'Land selection lacks an actual Inkscape export');
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Land selection source changed after export: '+target);
 check(receipt.images.length>0,'Land selection lacks an exported preview: '+target);
 for(const output of receipt.images){
  const file=resolve(output.relativePath),bytes=fs.readFileSync(file);
  check(sha(file)===output.sha256.toLowerCase(),'Land selection exported image changed: '+target);
  check(bytes.subarray(0,8).equals(Buffer.from([137,80,78,71,13,10,26,10])),'Land selection output is not PNG');
  check(bytes.readUInt32BE(16)===output.width&&bytes.readUInt32BE(20)===output.height,'Land selection PNG dimensions differ from receipt');
 }
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
 check(new Set(ids).size===ids.length,'Land selection contains duplicate editable object IDs: '+target);
 for(const id of target==='components'?['land-components']:['reference-original','baseline-context','land-surfaces','land-images','land-live-labels','land-state','land-hit-regions'])
  check(source.includes(`id="${id}"`),'Missing independent land selection layer: '+id);
 if(target==='components')check(!source.includes('<image')&&!source.includes('<text'),'Formal land surfaces must not contain screenshot pixels or baked live text');
 check(!source.includes('<script'),'Executable land selection design content');
}
const landSpec=read(resolve(`${landDesign}/design-spec.json`));
for(const [name,value] of Object.entries({panel:'360,418',header:'0,0,360,168',art:'12,68,336,100',image:'16,70,192,96',identity:'220,70,124,96',summary:'16,180,328,86',actions:'16,278,328,128',surfaceActions:'0,278,360,140'}))
 check(landSpec.layout[name].join(',')===value,'Inspected land selection contract changed: '+name);
check(landSpec.layout.surfaceGap===12,'Land selection lost the two visible map gaps');
check(landSpec.components.join(',')==='land_surface,land_command,land_command_active','Land selection must use its reviewed surface and inline command components');
const landSurface=manifest.entries.find(x=>x.id==='ui.youzhou.land_surface');
check(landSurface&&landSurface.status==='ready'&&landSurface.width===720&&landSurface.height===336,'Missing formal land surface or wrong dimensions');
check(resolve(landSurface.source.reference)===resolve(`${landDesign}/components/exports/export-receipt.json`),'Land surface source points to a different design receipt');
for(const name of ['land_command','land_command_active']){
 const entry=manifest.entries.find(x=>x.id==='ui.youzhou.'+name);
 check(entry&&entry.status==='ready'&&entry.width===656&&entry.height===88,'Land command skin is missing or has wrong reviewed dimensions: '+name);
 check(resolve(entry.source.reference)===resolve(`${landDesign}/components/exports/export-receipt.json`),'Land command skin uses an unrelated receipt: '+name);
}
check(landSpec.baselines.desktop!==landSpec.baselines.mobile&&landSpec.mobile.width===1600&&landSpec.mobile.height===720,'Mobile land design must use its own baseline and physical canvas');
console.log('PASS: land selection desktop/mobile/five-state sources, editable layers and three formal components match actual Inkscape exports. Real clicks and runtime appearance remain publisher checks.');
const archiveDesign='Docs/DesignUI/s1-20261028-043752328';
for(const target of ['desktop','mobile','states','components']){
 const receipt=read(resolve(`${archiveDesign}/${target}/exports/export-receipt.json`));
 check(receipt.toolVersion.startsWith('Inkscape '),'Archive lacks an actual Inkscape export');
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Archive source changed after export: '+target);
 for(const output of receipt.images){
  const file=resolve(output.relativePath),bytes=fs.readFileSync(file);
  check(sha(file)===output.sha256.toLowerCase(),'Archive exported PNG changed: '+target);
  check(bytes.readUInt32BE(16)===output.width&&bytes.readUInt32BE(20)===output.height,'Archive export dimensions differ from receipt');
 }
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
 check(new Set(ids).size===ids.length,'Archive has duplicate editable object IDs');
 for(const name of target==='components'?['archive-components']:['reference-original','baseline-context','archive-surfaces','archive-images','archive-live-labels','archive-state','archive-hit-regions'])
  check(source.includes(`id="${name}"`),'Missing independent archive design layer: '+name);
 if(target==='components')check(!source.includes('<image')&&!source.includes('<text'),'Archive skins contain screenshot pixels or live labels');
 check(!source.includes('<script'),'Executable archive design');
}
const archiveSpec=read(resolve(`${archiveDesign}/design-spec.json`));
check(archiveSpec.layout.art.join(',')==='36,26,width-40,height-70'&&archiveSpec.layout.footer.join(',')==='2,height-44,width-4,42','Archive portrait/footer differ from inspected design');
for(const [name,w,h] of [['archive_frame',384,504],['archive_selected',384,504],['archive_ribbon',56,140],['archive_footer',384,96]]){
 const entry=manifest.entries.find(x=>x.id==='ui.youzhou.'+name);
 check(entry&&entry.status==='ready'&&entry.width===w&&entry.height===h,'Missing formal archive component: '+name);
 check(resolve(entry.source.reference)===resolve(`${archiveDesign}/components/exports/export-receipt.json`),'Archive asset receipt differs: '+name);
}
console.log('PASS: archive desktop/mobile/extreme-state sources, four vector components and actual Inkscape exports match; runtime fonts, pictures and clicks await publisher.');
const catalogDesign='Docs/DesignUI/s1-20261028-022752190';
for(const target of ['desktop','mobile','states','components']){
 const receipt=read(resolve(`${catalogDesign}/${target}/exports/export-receipt.json`));
 check(receipt.toolVersion.startsWith('Inkscape '),'Facility atlas lacks actual Inkscape tool receipt');
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Facility atlas changed after export: '+target);
 for(const output of receipt.images){
  const file=resolve(output.relativePath),bytes=fs.readFileSync(file);
  check(sha(file)===output.sha256.toLowerCase(),'Facility atlas preview changed: '+target);
  check(bytes.readUInt32BE(16)===output.width&&bytes.readUInt32BE(20)===output.height,'Facility atlas PNG dimensions differ from receipt');
 }
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
 check(new Set(ids).size===ids.length,'Facility atlas has duplicate editable IDs');
 for(const name of target==='components'?['catalog-surfaces']:['reference-original','baseline-context','catalog-surfaces','catalog-images','catalog-live-labels','catalog-state','catalog-hit-regions'])
  check(source.includes(`id="${name}"`),'Missing independent atlas design layer: '+name);
 if(target==='components')check(!source.includes('<image')&&!source.includes('<text'),'Facility atlas skins must not contain screenshots or live values');
 check(!source.includes('<script'),'Executable facility atlas content');
}
const catalogSpec=read(resolve(`${catalogDesign}/design-spec.json`));
check(catalogSpec.layout.cardHeight===206&&catalogSpec.layout.gap===16&&catalogSpec.layout.threeColumnThreshold===1460,'Inspected atlas node/grid contract changed');
check(catalogSpec.layout.art.join(',')==='10,14,216,162'&&catalogSpec.layout.action.join(',')==='238,150,width-250,44','Inspected atlas complete art/action contract changed');
for(const [name,w,h] of [['catalog_sheet',1024,640],['catalog_rule',1024,2],['catalog_category',368,88],['catalog_category_active',368,88]]){
 const entry=manifest.entries.find(x=>x.id==='ui.youzhou.'+name);
 check(catalogSpec.components.includes(name)&&entry&&entry.status==='ready'&&entry.width===w&&entry.height===h,'Missing formal atlas component: '+name);
 check(resolve(entry.source.reference)===resolve(`${catalogDesign}/components/exports/export-receipt.json`),'Atlas asset points to another receipt: '+name);
}
console.log('PASS: facility atlas desktop/mobile/state sources and four vector components match real Inkscape SHA/dimensions; editable layers preserve dynamic values. Runtime visual and input checks remain separate.');
const galleryDesign='Docs/DesignUI/s1-20261028-012751756';
for(const target of ['desktop','mobile','states','components']){
 const receipt=read(resolve(`${galleryDesign}/${target}/exports/export-receipt.json`));
 check(receipt.toolVersion.startsWith('Inkscape '),'Gallery requires an actual Inkscape export: '+target);
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Gallery editable source changed after export: '+target);
 check(receipt.images.length>0,'Missing actual gallery preview: '+target);
 for(const output of receipt.images){
  const filename=resolve(output.relativePath),bytes=fs.readFileSync(filename);
  check(sha(filename)===output.sha256.toLowerCase(),'Gallery exported PNG changed: '+target);
  check(bytes.readUInt32BE(16)===output.width&&bytes.readUInt32BE(20)===output.height,'Gallery receipt dimensions differ from its actual PNG: '+target);
 }
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
 check(new Set(ids).size===ids.length,'Duplicate gallery editable object identity: '+target);
 const layers=[...source.matchAll(/<g\b[^>]*\bid="([^"]+)"[^>]*inkscape:groupmode="layer"[^>]*>/g)].map(x=>x[1]);
 for(const name of target==='components'?['gallery-surfaces']:['gallery-surfaces','gallery-images','gallery-live-labels','gallery-state','gallery-hit-regions'])
  check(layers.some(id=>id===name||id.startsWith(name+'-')),'Missing independently editable gallery layer: '+name);
 if(target==='components')check(!source.includes('<image')&&!source.includes('<text'),'Gallery skins contain screenshot pixels or baked live labels');
 check(!source.includes('<script'),'Executable gallery design content');
}
const gallerySpec=read(resolve(`${galleryDesign}/design-spec.json`));
check(gallerySpec.layout.gridHeight===156&&gallerySpec.layout.optionHeight===96,'Gallery district or selector footprint changed');
check(gallerySpec.layout.optionNameY+gallerySpec.layout.optionNameHeight<=gallerySpec.layout.optionStateY,
 'Gallery thumbnail name and state rows overlap');
check(gallerySpec.layout.optionStateY+gallerySpec.layout.optionStateHeight<=gallerySpec.layout.optionHeight,
 'Gallery thumbnail live state leaves its touch target');
for(const [name,w,h] of [['facility_stage',960,640],['facility_stage_footer',960,68],['facility_stage_selected',192,192]]){
 const entry=manifest.entries.find(x=>x.id==='ui.youzhou.'+name);
 check(gallerySpec.components.includes(name)&&entry&&entry.status==='ready'&&entry.width===w&&entry.height===h,
  'Missing or incorrectly sized formal gallery component: '+name);
 check(resolve(entry.source.reference)===resolve(`${galleryDesign}/components/exports/export-receipt.json`),
  'Gallery formal component points to a different design export: '+name);
}
console.log('PASS: gallery desktop/mobile/state/components match actual Inkscape source/output SHA and PNG dimensions; three formal vector skins retain independent editable layers. Runtime screenshots and input remain publisher checks.');
const facilityDesign='Docs/DesignUI/s1-20261028-002751316';
for(const target of ['desktop','mobile','states','components']){
 const receipt=read(resolve(`${facilityDesign}/${target}/exports/export-receipt.json`));
 check(receipt.toolVersion.startsWith('Inkscape '),'Facility design requires a real Inkscape export');
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Facility design changed after export: '+target);
 for(const output of receipt.images)check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Facility output changed after export');
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
 check(new Set(ids).size===ids.length,'Duplicate facility design identity');
 if(target==='components')check(!source.includes('<image')&&!source.includes('<text'),'Facility skins contain reference pixels or baked text');
 else for(const id of ['facility-surfaces','facility-images','facility-live-labels','facility-state','facility-hit-regions'])check(source.includes(id),'Missing editable facility layer: '+id);
}
const facilitySpec=read(resolve(`${facilityDesign}/design-spec.json`));
check(facilitySpec.layout.contentHeight===374&&facilitySpec.layout.action.width===360&&facilitySpec.layout.action.height===44,'Facility design footprint changed');
for(const name of facilitySpec.components)check(manifest.entries.some(x=>x.id==='ui.youzhou.'+name&&x.status==='ready'),'Missing formal facility component: '+name);
console.log('PASS: facility desktop/mobile/state layers and five vector skins match actual Inkscape exports. Runtime appearance remains a publisher check.');
const dossierDesign='Docs/DesignUI/s1-20261027-232251526';
for(const target of ['desktop','mobile','states','components']){
 const receipt=read(resolve(`${dossierDesign}/${target}/exports/export-receipt.json`));
 check(receipt.toolVersion.startsWith('Inkscape '),'Missing actual dossier design export');
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Dossier source changed after export: '+target);
 for(const output of receipt.images)check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Dossier output changed: '+target);
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
 check(new Set(ids).size===ids.length,'Duplicate editable dossier object identity: '+target);
 if(target==='components')check(!source.includes('<image')&&!source.includes('<text'),'Dossier component contains bitmap or baked live text');
 else for(const id of ['dossier-surfaces','dossier-images','dossier-live-labels','dossier-state','dossier-hit-regions'])
  check(source.includes(`id="${id}"`),'Missing editable dossier layer '+id);
 check(!source.includes('<script'),'Executable dossier source');
}
const dossierSpec=read(resolve(`${dossierDesign}/design-spec.json`));
check(dossierSpec.layout.skillY===332&&dossierSpec.layout.skillHeight===100&&dossierSpec.layout.contentHeight===440,'Reviewed dossier skill row dimensions changed');
for(const name of dossierSpec.components){
 const item=manifest.entries.find(x=>x.id==='ui.youzhou.'+name);
 const size=name==='dossier_skill_ring'?144:64;
 check(item&&item.status==='ready'&&item.width===size&&item.height===size,'Missing or invalid dossier component '+name);
}
for(const name of dossierSpec.reusedComponents)check(manifest.entries.some(x=>x.id==='ui.youzhou.'+name&&x.status==='ready'),'Missing reused dossier component '+name);
console.log('PASS: dossier desktop/mobile/state layers and six vector components match actual Inkscape exports; layout and field semantics await runtime review.');
const portraitDesign='Docs/DesignUI/s1-20261027-222251138';
for(const target of ['desktop','mobile','states','components']){
 const receipt=read(resolve(`${portraitDesign}/${target}/exports/export-receipt.json`));
 check(receipt.toolVersion.startsWith('Inkscape '),'Missing actual portrait design export');
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Portrait source changed after export: '+target);
 for(const output of receipt.images)check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Portrait design export changed: '+target);
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 const ids=[...source.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
 check(new Set(ids).size===ids.length,'Duplicate editable portrait object identity: '+target);
 if(target!=='components')for(const id of ['portrait-surfaces','portrait-images','portrait-live-labels','portrait-hit-regions'])
  check(source.includes(`id="${id}"`),'Missing editable portrait layer '+id);
 if(target==='components')check(!source.includes('<image'),'Portrait components must not contain reference bitmap pixels');
 check(!source.includes('<script'),'Executable portrait design content');
}
for(const [name,w,h] of [['portrait_ribbon',96,480],['portrait_seal',96,96],['portrait_base',1040,136]]){
 const item=manifest.entries.find(x=>x.id==='ui.youzhou.'+name);
 check(item&&item.status==='ready'&&item.width===w&&item.height===h,'Missing or incorrectly sized exported portrait component: '+name);
}
console.log('PASS: portrait desktop/mobile/states/components match actual Inkscape source and output SHA; three formal surfaces are ready. Runtime appearance and input remain separate.');
let count=0;
for(const item of manifest.entries.filter(x=>x.kind==='ui-sprite')){
 const receiptPath=resolve(item.source.reference), receipt=read(receiptPath);
 check(receipt.toolVersion.startsWith('Inkscape '),'Missing real tool version');
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Editable source changed after export');
 const output=receipt.images.find(x=>x.sha256.toLowerCase()===item.sha256.toLowerCase());
 check(!!output,'Production UI asset absent from export receipt: '+item.id);
 check(sha(resolve(output.relativePath))===item.sha256.toLowerCase(),'Export PNG changed after receipt');
 check(sha(resolve('Assets/EasternDynasty/Resources/'+item.resourcePath+'.png'))===item.sha256.toLowerCase(),'Production image differs from reviewed export');
 count++;
}
check(count>=6,'Missing production navigation component');
const design='Docs/DesignUI/s1-20261027-114202570';
for(const target of ['desktop','mobile']){
 const receipt=read(resolve(`${design}/${target}/exports/export-receipt.json`));
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Preview source changed: '+target);
 for(const output of receipt.images)check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Preview image changed: '+target);
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 for(const id of ['reference-original','baseline-context','button-frames','formal-images','live-labels','hit-regions'])
  check(source.includes(`id="${id}"`),'Missing editable layer '+id);
}
console.log(`PASS: ${count} production UI assets match Inkscape export/source SHA; desktop/mobile editable layers and preview receipts match. Runtime review remains separate.`);
const headerDesign='Docs/DesignUI/s1-20261027-124251578';
for(const target of ['desktop','mobile','stress']){
 const receipt=read(resolve(`${headerDesign}/${target}/exports/export-receipt.json`));
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Header source changed after export: '+target);
 for(const output of receipt.images)check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Header preview changed: '+target);
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 for(const id of ['reference-2023','header-surfaces','header-images','header-live-labels','header-hit-regions'])
  check(source.includes(`id="${id}"`),'Missing editable header layer '+id);
}
for(const id of ['header_veil','header_divider','header_currency'])check(manifest.entries.some(x=>x.id==='ui.youzhou.'+id&&x.status==='ready'),'Missing formal header component '+id);
console.log('PASS: header desktop/mobile/stress sources and tool receipts match; three stable production IDs present. Runtime font, transparency and clicks remain publisher checks.');
const questDesign='Docs/DesignUI/s1-20261027-134751635';
for(const [target,file] of [['desktop','map-hud'],['mobile','map-hud'],['states','quest-states']]){
 const receipt=read(resolve(`${questDesign}/${target}/exports/export-receipt.json`));
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Quest source changed after actual export: '+target);
 for(const output of receipt.images)check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Quest preview changed: '+target);
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 for(const id of ['quest-surfaces','quest-live-labels','quest-state','quest-hit-regions'])
  check(source.includes(id),'Missing independent quest design layer '+id);
 check(!source.includes('<script'),'Executable design content');
}
const questSpec=read(resolve(`${questDesign}/design-spec.json`));
check(questSpec.area.join(',')==='20,90,354,120'&&questSpec.action[3]===44,'Quest design touch/footprint contract changed');
for(const id of questSpec.components)check(manifest.entries.some(x=>x.id===id&&x.status==='ready'),'Missing exported quest component '+id);
console.log('PASS: quest desktop/mobile/three-state design receipts and four production components match. Runtime typography and actual input remain publisher checks.');

const armyDesign='Docs/DesignUI/s1-20261027-145251750';
for(const target of ['desktop','mobile','states','controls']){
 const receipt=read(resolve(`${armyDesign}/${target}/exports/export-receipt.json`));
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Army source changed after export: '+target);
 for(const output of receipt.images)check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Army preview changed: '+target);
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 for(const id of target==='controls'?['army-control-states']:['army-surfaces','army-images','army-live-labels','army-state','army-hit-regions'])
  check(source.includes(id),'Missing editable army layer '+id);
}
const armySpec=read(resolve(`${armyDesign}/design-spec.json`));
check(armySpec.area.width===354&&armySpec.area.height===242,'Army should preserve the full existing content footprint');
check(armySpec.art.join(',')==='26,4,80,116'&&armySpec.actions.every(x=>x[3]>=44),'Inspected complete portrait/touch bounds changed');
for(const id of armySpec.components)check(manifest.entries.some(x=>x.id===id&&x.status==='ready'),'Missing exported army component '+id);
console.log('PASS: army desktop/mobile/state/control sources and receipts match, complete portraits and 44-unit commands retained. Runtime input, actual font and visual review remain publisher checks.');

const atlasDesign='Docs/DesignUI/s1-20261027-155751815';
for(const target of ['desktop','mobile','states']){
 const receipt=read(resolve(`${atlasDesign}/${target}/exports/export-receipt.json`));
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'Atlas source changed after actual export: '+target);
 for(const output of receipt.images)check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'Atlas preview changed: '+target);
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 for(const id of ['atlas-surfaces','atlas-terrain-context','atlas-images','atlas-live-labels','atlas-state','atlas-hit-regions'])
  check(source.includes(id),'Missing independent atlas design layer '+id);
}
const atlasSpec=read(resolve(`${atlasDesign}/design-spec.json`));
check(atlasSpec.terrain.join(',')==='14,54,204,204'&&atlasSpec.locate.join(',')==='178,0,44,44','Reviewed atlas resolution/touch contract changed');
for(const id of atlasSpec.components)check(manifest.entries.some(x=>x.id===id&&x.status==='ready'),'Missing exported atlas component '+id);
console.log('PASS: atlas desktop/mobile/state design receipts and four production components match; current-world terrain and actual input remain publisher checks.');

const cityArmyDesign='Docs/DesignUI/s1-20261027-170251762';
for(const target of ['desktop','mobile','states']){
 const receipt=read(resolve(`${cityArmyDesign}/${target}/exports/export-receipt.json`));
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'City army source changed after export: '+target);
 for(const output of receipt.images)check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'City army preview changed: '+target);
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 for(const id of ['army-surfaces','army-images','army-live-labels','army-state','army-hit-regions'])
  check(source.includes(id),'Missing independently editable city-army layer '+id);
 check(!source.includes('<script'),'Executable city-army design content');
}
const cityArmySpec=read(resolve(`${cityArmyDesign}/design-spec.json`));
check(cityArmySpec.layout.identityHeight===42&&cityArmySpec.layout.touchHeight===44,'City army name/touch geometry contract changed');
for(const id of cityArmySpec.components)check(manifest.entries.some(x=>x.id==='ui.youzhou.'+id&&x.status==='ready'),'Missing city army component '+id);
console.log('PASS: city-army desktop/mobile/state sources and Inkscape receipts match; five formal components, full portrait layout and touch contract preserved. Runtime acceptance remains separate.');

const cityAffairsDesign='Docs/DesignUI/s1-20261027-180252216';
for(const target of ['desktop','mobile','states']){
 const receipt=read(resolve(`${cityAffairsDesign}/${target}/exports/export-receipt.json`));
 check(receipt.toolVersion.startsWith('Inkscape '),'Missing actual city-affairs export tool version');
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'City affairs source changed after actual export: '+target);
 for(const output of receipt.images)check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'City affairs preview changed: '+target);
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 for(const id of ['affairs-surfaces','affairs-images','affairs-live-labels','affairs-state','affairs-hit-regions'])
  check(source.includes(id),'Missing independently editable city-affairs layer '+id);
 check(!source.includes('<script'),'Executable city-affairs design content');
}
const cityAffairsSpec=read(resolve(`${cityAffairsDesign}/design-spec.json`));
check(cityAffairsSpec.layout.height===218&&cityAffairsSpec.layout.touchHeight===44,'City affairs original footprint or usable touch contract changed');
check(cityAffairsSpec.layout.seal.join(',')==='8,0,112,112'&&cityAffairsSpec.layout.building.join(',')==='12,4,104,78','City affairs reviewed seal/building dimensions changed');
for(const id of cityAffairsSpec.components)check(manifest.entries.some(x=>x.id==='ui.youzhou.'+id&&x.status==='ready'),'Missing city affairs formal component '+id);
console.log('PASS: city-affairs desktop/mobile/state sources and real Inkscape receipts match; five production components and separate building/caption geometry preserved. Runtime font, input and screenshots remain publisher checks.');

const cityDockDesign='Docs/DesignUI/s1-20261027-190751837';
for(const target of ['desktop','mobile','states']){
 const receipt=read(resolve(`${cityDockDesign}/${target}/exports/export-receipt.json`));
 check(receipt.toolVersion.startsWith('Inkscape '),'Missing actual city dock export tool');
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'City dock source changed after export: '+target);
 for(const output of receipt.images)check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'City dock preview changed: '+target);
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 for(const id of ['dock-surfaces','dock-images','dock-live-labels','dock-state','dock-hit-regions'])
  check(source.includes(id),'Missing independent city dock design layer '+id);
 check(!source.includes('<script'),'Executable city dock source');
}
const cityDockSpec=read(resolve(`${cityDockDesign}/design-spec.json`));
check(cityDockSpec.layout.art.join(',')==='6,2,72,54'&&cityDockSpec.layout.caption.join(',')==='2,60,80,24','Inspected city dock image/caption layout changed');
for(const id of cityDockSpec.components)check(manifest.entries.some(x=>x.id==='ui.youzhou.'+id&&x.status==='ready'),'Missing city dock formal component '+id);
console.log('PASS: city dock desktop/mobile/states and actual Inkscape receipts match; five formal components, independent image/caption sources retained. Actual fonts, input and screenshots await publisher.');

const cityHeaderDesign='Docs/DesignUI/s1-20261027-201251989';
for(const target of ['desktop','mobile','stress']){
 const receipt=read(resolve(`${cityHeaderDesign}/${target}/exports/export-receipt.json`));
 check(receipt.toolVersion.startsWith('Inkscape '),'Missing actual city header export tool');
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'City header source changed after export: '+target);
 for(const output of receipt.images)check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'City header preview changed: '+target);
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 for(const id of ['header-surfaces','header-images','header-live-labels','header-state','header-hit-regions'])
  check(source.includes(id),'Missing independent city header layer '+id);
 check(!source.includes('<script'),'Executable city header design');
}
const cityHeaderSpec=read(resolve(`${cityHeaderDesign}/design-spec.json`));
for(const id of cityHeaderSpec.components)check(manifest.entries.some(x=>x.id==='ui.youzhou.'+id&&x.status==='ready'),'Missing city header production component '+id);
const cityHeaderComponents=fs.readFileSync(resolve(`${cityHeaderDesign}/components/city-header.svg`),'utf8');
check(!cityHeaderComponents.includes('<image')&&!cityHeaderComponents.includes('<text'),'City header components must contain no screenshot pixels or baked live text');
console.log('PASS: city header desktop/mobile/long-name sources and real Inkscape receipts match; three formal vector components contain no screenshots or live text. Runtime font, input and visual acceptance remain separate.');

const cityFooterDesign='Docs/DesignUI/s1-20261027-211752367';
for(const target of ['desktop','mobile','states']){
 const receipt=read(resolve(`${cityFooterDesign}/${target}/exports/export-receipt.json`));
 check(receipt.toolVersion.startsWith('Inkscape '),'Missing actual city footer export tool');
 check(sha(resolve(receipt.relativeSource))===receipt.sourceSha256.toLowerCase(),'City footer source changed after actual export: '+target);
 check(receipt.images.length>0,'Missing exported city footer preview: '+target);
 for(const output of receipt.images)check(sha(resolve(output.relativePath))===output.sha256.toLowerCase(),'City footer preview changed: '+target);
 const source=fs.readFileSync(resolve(receipt.relativeSource),'utf8');
 for(const id of ['reference-original','baseline-context','footer-surfaces','footer-images','footer-live-labels','footer-state','footer-hit-regions'])
  check(source.includes(`id="${id}"`),'Missing independently editable city footer layer '+id);
 check(!source.includes('<script'),'Executable city footer design content');
}
const cityFooterSpec=read(resolve(`${cityFooterDesign}/design-spec.json`));
const footerLayout=cityFooterSpec.layout;
check(footerLayout.footerHeight===78&&footerLayout.pitch===116,'City footer footprint or ticket rhythm changed');
for(const [name,expected] of Object.entries({entry:'20,-74,108,70',recruit:'-152,-74,132,70',art:'34,8,70,56',caption:'5,4,25,50',recruitArt:'46,7,40,38',recruitCaption:'8,47,116,22'}))
 check(footerLayout[name].join(',')===expected,'Inspected city footer geometry changed: '+name);
check(cityFooterSpec.components.length===6,'City footer must reuse all six formal navigation components');
for(const id of ['nav_ticket','nav_ticket_active','nav_recruit','nav_letter','nav_book','nav_shelf'])
 check(cityFooterSpec.components.includes('ui.youzhou.'+id)&&manifest.entries.some(x=>x.id==='ui.youzhou.'+id&&x.status==='ready'),'Missing existing formal city footer component '+id);
check(sha(resolve(cityFooterSpec.reusedEditableComponents.path))===cityFooterSpec.reusedEditableComponents.sha256.toLowerCase(),'Reused editable navigation source differs from its inspected identity');
for(const baseline of cityFooterSpec.baselines)check(sha(resolve(baseline.path))===baseline.sha256.toLowerCase(),'City footer comparison baseline changed');
console.log('PASS: city footer desktop/mobile/states and real Inkscape receipts match; six existing formal components, independent source layers and inspected image/title geometry retained. Actual fonts, all eight controls and new screenshots await publisher.');
