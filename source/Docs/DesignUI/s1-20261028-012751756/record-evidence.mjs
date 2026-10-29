import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261028-012751756',dir='Validation/UIRound/'+version,design='Docs/DesignUI/'+version;
const bytes=p=>fs.readFileSync(p),sha=p=>crypto.createHash('sha256').update(bytes(p)).digest('hex');
const read=p=>{const b=bytes(p);return b[0]===255&&b[1]===254?b.toString('utf16le').replace(/^\uFEFF/,''):b.toString('utf8').replace(/^\uFEFF/,'');};
const json=p=>JSON.parse(read(p));
const save=(p,v)=>fs.writeFileSync(p,JSON.stringify(v,null,2)+'\n');
const file=p=>({path:p.replaceAll('\\','/'),sha256:sha(p),bytes:bytes(p).length});
const validate=read(dir+'/validate.log'),assets=read(dir+'/assets.log');
for(const marker of ['CITY_FACILITY_GALLERY_CHECKS_OK: 3514','PASS: runtime and Editor scripts compile against installed Unity references.','PASS: gallery desktop/mobile/state/components match actual Inkscape'])
 if(!validate.includes(marker))throw Error('Missing completed check: '+marker);
if(!assets.includes('ready=314; missing=16; pending Unity meta=56'))throw Error('Asset check did not complete as recorded');
const sources=['CityFacilityGalleryLayout.cs','CityFacilityGalleryPresentation.cs','CityFacilityDossierPresentation.cs','CityFacilityPresentation.cs','CityPresentation.cs','Diagnostics/CityFacilityDossierDiagnostics.cs','Diagnostics/ExperienceDiagnostics.cs'].map(p=>'Assets/EasternDynasty/Scripts/Runtime/'+p);
const tools=['Tools/Check-CityFacilityGallery.ps1','Tools/Validate.ps1','Tools/Validate-Visual.ps1','Tools/Check-Experience.ps1','Validation/CityFacilityGalleryChecks.cs','Validation/UiDesignChecks.mjs'];
const manifest=json('Assets/EasternDynasty/Resources/asset-manifest.json');
const imported=manifest.entries.filter(x=>['ui.youzhou.facility_stage','ui.youzhou.facility_stage_footer','ui.youzhou.facility_stage_selected'].includes(x.id));
if(imported.length!==3)throw Error('Missing controlled gallery asset');
const spec=json(design+'/design-spec.json');spec.status='exported-inspected-runtime-integrated-awaiting-publisher';save(design+'/design-spec.json',spec);
const inspected=['desktop','mobile','states','components'].map(target=>{
 const receiptPath=design+'/'+target+'/exports/export-receipt.json',receipt=json(receiptPath);
 if(sha(receipt.relativeSource)!==receipt.sourceSha256.toLowerCase())throw Error('Source changed after tool export: '+target);
 for(const img of receipt.images)if(sha(img.relativePath)!==img.sha256.toLowerCase())throw Error('Output changed after tool export');
 return {target,toolVersion:receipt.toolVersion,receipt:file(receiptPath),source:file(receipt.relativeSource),preview:file(design+'/'+target+'/exports/gallery-preview.png'),viewed:true};
});
save(design+'/inspection.json',{version,atUtc:new Date().toISOString(),status:'design-inspected-runtime-pending',inspected,corrections:['Moved selected emphasis from the bottom to the top to clear the status label, re-exported and viewed desktop/mobile.','Corrected construction-state district 0 to the actual mill/working labels; re-exported and viewed.'],limitations:['Right ledger inherits the prior inspected design; surrounding UI uses old 104752269 screenshots. These composites are not current runtime evidence.','Existing 240x180 project building silhouettes are illustrations, not new official high-resolution art.','Inkscape configuration/recent-file write warnings retained; exported files and hashes verified.']});
const handoff=`## ${version} / 主城设施矩形建筑展台与完整缩略图

**专业分层设计、三个正式组件与 UI 源码已接入；运行观感及真实点击待发布者。** 实看2024官方Q09-CU-A001设施页和104752269桌面/手机基线，将大圆章改为矩形建筑展台、34高独立名称/等级底栏；八设施取消小圆章、增大完整缩略图并分离名称/解锁状态，九格48高定位控件保留。[三个差距、源稿、实际预览与收据](UIIterations/${version}.md)。现有建筑示意复用，不称原作正式高清素材或城内全景已还原。

Inkscape 1.4.4 实际导出并查看桌面/手机/施工/组件稿，修正选中线压字后接入。三张 ui.youzhou.facility_stage / facility_stage_footer / facility_stage_selected 经 Import-Asset 登记。统一纯逻辑与 Runtime/Editor 引用编译通过；3514 项新布局/字体预算覆盖12屏，56个UI组件来源链及314 ready/16既有missing资源校验通过，56项meta待发布者正常导入。[开发结果](../${dir}/result.json)。新增必需gallery截图/实际字体与几何门禁，以及六次可见图像/名称选择；原建造、退款、滚动和前置检查保留。无规则、卡池或存档变化。

RAG002759588→012800571新增民居批次，64版本/130配置/982身份/723图、零新图；batch、12来源、5派生及文档SHA匹配，仅observed。2022排列无图、2025名望/税收缺参数与当前同服确认；S1UX-02/04已去重补记。此前Q09仅部分采用展示布局。旧094发布失败已有修复；上一轮002纯检查完成后网络重连并超时，无worker-result，不能称已交付。

下一轮先读发布反馈，核对三尺寸新gallery/锁定/施工/升级/最高级图、完整建筑和中文底栏、九格时间及六次图文选择。继续设施图谱或主城全景有原图依据的设计；当前S1正式建筑、民居规则仍缺，独立练兵/任务/冻结回放/本场观看与Android真机待验继续开放。本轮未启动Unity/客户端、未交付Windows/APK、未改快照/发布状态/调度/锁。

`;
for(const p of ['Docs/Iteration-Review.md','Docs/Hourly-UI-Gameplay-Backlog.md']){
 let s=read(p);if(s.includes('## '+version+' /'))continue;
 const offset=p.endsWith('Iteration-Review.md')?s.indexOf('\n')+1:s.indexOf('## s1-20261028-002751316');
 s=s.slice(0,offset)+'\n'+handoff+s.slice(offset);fs.writeFileSync(p,s);
}
const navigation='agent.md';let nav=read(navigation);
if(!nav.includes('| 设施矩形建筑展台与完整缩略图 |')){
 const old='| 主城设施专业横向账页 |';const position=nav.indexOf(old);
 if(position<0)throw Error('Navigation insertion anchor missing');
 nav=nav.slice(0,position)+'| 设施矩形建筑展台与完整缩略图 | Runtime/CityFacilityGalleryLayout.cs、CityFacilityGalleryPresentation.cs、CityFacilityPresentation.cs；Diagnostics/CityFacilityDossierDiagnostics.cs；Docs/DesignUI/'+version+' | 三个Inkscape正式组件，大图/34高身份等级底栏与58高完整缩略图；九格48高保留，3514布局/字体预算及引用编译通过，新图/六次图文点击待发布；无规则/存档变化 |\n'+nav.slice(position);
 fs.writeFileSync(navigation,nav);
}
const result={version,atUtc:new Date().toISOString(),status:'source-and-design-complete-runtime-pending',uiIteration:'Docs/UIIterations/'+version+'.md',sourceChanges:sources.map(file),validationEntryChanges:tools.map(file),designInspection:file(design+'/inspection.json'),designSpec:file(design+'/design-spec.json'),assets:imported.map(x=>({id:x.id,sha256:x.sha256,width:x.width,height:x.height,source:x.source,production:file('Assets/EasternDynasty/Resources/'+x.resourcePath+'.png')})),checks:{validate:{exitCode:0,log:file(dir+'/validate.log')},gallery:{exitCode:0,assertions:3514,log:file(dir+'/gallery-check.log')},assets:{exitCode:0,ready:314,missing:16,pendingUnityMeta:56,log:file(dir+'/assets.log')},inputSyntax:{exitCode:0,log:file(dir+'/input-syntax.log')}},research:file(dir+'/research.json'),failureReview:file(dir+'/failure-review.json'),ragApplied:'Q09-CU-A001 historical rectangular facility gallery hierarchy partially applied to presentation only. New residence batch observed, not applied to rules; no roster/Core/Application/save changes.',questionNote:'../rag/question.md: S1UX-02/04 residence follow-up',publisherRequired:['New smoke-facility-gallery-design.png and existing empty/locked/working/upgrade/maximum views at both desktop sizes and mobile safe area.','Actual font fit, full building illustration, 34-high footer, thumbnail names/states and nine 48-high district controls.','Six visible image/title clicks preserve the whole World; original build/refund/prerequisite/scroll/back tests remain.','Three-way visual review before local Windows/APK delivery; Android device testing remains outstanding.']};
save(dir+'/result.json',result);
console.log('RECORDED: final source/design/check evidence and review/backlog/navigation; runtime acceptance remains pending.');
