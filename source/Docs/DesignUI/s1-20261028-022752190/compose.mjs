import fs from 'node:fs';
import path from 'node:path';
const version='s1-20261028-022752190',root=path.resolve('Docs/DesignUI/'+version);
const save=(p,s)=>{let f=path.join(root,p);fs.mkdirSync(path.dirname(f),{recursive:true});fs.writeFileSync(f,s);};
const data=p=>'data:image/'+(p.endsWith('.jpg')?'jpeg':'png')+';base64,'+fs.readFileSync(p).toString('base64');
const layer=(id,label,s,attrs='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${label}" ${attrs}>${s}</g>`;
const svg=(w,h,s)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${s}</svg>`;
const text=(x,y,s,size=16,color='#e3e2d9',attrs='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}" ${attrs}>${s}</text>`;
const defs='<defs><linearGradient id="sheet" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#3a4041"/><stop offset=".55" stop-color="#292f33"/><stop offset="1" stop-color="#21262c"/></linearGradient><linearGradient id="rule"><stop stop-color="#9aa6aa" stop-opacity=".05"/><stop offset=".2" stop-color="#9aa6aa" stop-opacity=".38"/><stop offset=".8" stop-color="#9aa6aa" stop-opacity=".38"/><stop offset="1" stop-color="#9aa6aa" stop-opacity=".05"/></linearGradient></defs>';
const sheet=(w,h)=>`<rect width="${w}" height="${h}" fill="url(#sheet)"/>`;
const rule=(w,h)=>`<rect width="${w}" height="${h}" fill="url(#rule)"/>`;
const command=data('Assets/EasternDynasty/Resources/YouzhouUI/facility_command.png');
const kinds=['TownHall','Mill','Lumberyard','Quarry','Ironworks','Residence','Barracks','DrillGround','Workshop'];
const names=['城主府','磨坊','伐木场','采石场','炼铁场','民居','兵营','校场','工坊'];
const effects=['3 → 4 级 · 设施等级上限','0 → 160 粮食 / 小时','0 → 140 木材 / 小时','0 → 120 石料 / 小时','0 → 80 铁矿 / 小时','0 → 100 金币 / 小时','0 → 250 带兵上限','0 → 100 带兵上限','0 → 5% 工期缩短'];
const reference='F:/工作项目/率土之滨/rag/assets/85/857a6e5afaa30ad8669c8b9197ba5cfe59ddf1362d30bf6158940d2be993555e.png';
save('components/catalog.svg',svg(1024,760,defs+layer('catalog-surfaces','共享图谱底板、分隔与分类态；不含文字或截图',`<g id="catalog-sheet">${sheet(1024,640)}</g><g id="catalog-rule" transform="translate(0 650)">${rule(1024,2)}</g><g id="catalog-category" transform="translate(0 666)"><rect width="368" height="88" fill="#262d32"/></g><g id="catalog-category-active" transform="translate(400 666)"><rect width="368" height="88" fill="#5a5747"/></g>`)));
function scene(lw,lh,state='empty'){
 const ax=24,ay=142,aw=lw-48,ah=lh-240,px=248,pw=aw-224,vx=266,vy=222,vw=pw-36,vh=ah-94,cols=vw>=1460?3:2,cw=(vw-24-16*(cols-1))/cols;
 let surfaces=`<g transform="translate(${ax} ${ay})">${sheet(aw,ah)}</g>`,images='',labels='',hits='';
 labels+=text(42,178,'设施一览',24,'#d8c68f');
 images+=`<image x="36" y="198" width="184" height="138" href="${data('Validation/CityArt/TownHall.png')}"/>`;
 labels+=text(128,365,'城主府  Lv. 1',18,'#e3e2d9','text-anchor="middle"')+text(128,392,'已建 1 / 9 处',14,'#b8c3c7','text-anchor="middle"');
 for(let i=0;i<4;i++){const yy=418+i*50;surfaces+=`<rect x="36" y="${yy}" width="184" height="44" fill="${i===0?'#5a5747':'#262d32'}"/>`;labels+=text(128,yy+28,['全部设施','城务发展','资源生产','军备建设'][i],17,'#e3e2d9','text-anchor="middle"');hits+=`<rect x="36" y="${yy}" width="184" height="44"/>`;}
 labels+=text(42,ay+ah-46,'项目设施配置 · 建筑示意',12,'#b8c3c7')+text(42,ay+ah-27,'一支部队 · 单施工队列',12,'#b8c3c7')+text(42,ay+ah-8,'图像前往真实位置',12,'#b8c3c7');
 labels+=text(px+22,175,'全部设施 / 建筑图谱',20)+text(px+22,200,state==='working'?'施工中 磨坊 → Lv. 1 · 20秒':'工匠待命 · 选择建筑，核对费用后建设',14,'#b8c3c7');
 if(state==='working'){surfaces+=`<image x="${px+pw-184}" y="156" width="162" height="44" preserveAspectRatio="none" href="${command}"/>`;labels+=text(px+pw-103,184,'查看当前施工',16,'#202526','text-anchor="middle"');}
 surfaces+=`<g transform="translate(${px+22} 211)">${rule(pw-44,1)}</g>`;
 let ns='',ni='',nt='',nh='';
 for(let i=0;i<9;i++){
  const x=vx+i%cols*(cw+16),y=vy+Math.floor(i/cols)*222,tx=x+238,tw=cw-250;
  ns+=`<g transform="translate(${x} ${y+204})">${rule(cw,1)}</g><image x="${tx}" y="${y+150}" width="${tw}" height="44" preserveAspectRatio="none" href="${command}"/>`;
  ni+=`<image x="${x+10}" y="${y+14}" width="216" height="162" preserveAspectRatio="xMidYMid meet" href="${data('Validation/CityArt/'+kinds[i]+'.png')}"/>`;
  nt+=text(tx,y+33,names[i],22,'#e3e2d9')+text(tx,y+58,i===1&&state==='working'?'施工至 Lv. 1':i===0?'Lv. 1 / 8':'尚未建成',14,'#d8c68f')+text(tx,y+90,effects[i],14,'#cbd4d5')+text(tx,y+120,state==='working'?(i===1?'完工剩余 20秒':'等待 磨坊完工'):i===0?'主城发展核心':i>=6?'需城主府 Lv. '+(i===8?3:2):'城主府 Lv. 1 · 已满足',14,'#b8c3c7')+text(tx+tw/2,y+178,state==='working'?'前往磨坊工地':i===0?'查看城主府':i>=6?'前往城主府':'查看建造位置',16,'#202526','text-anchor="middle"');
  nh+=`<rect x="${x+10}" y="${y+14}" width="216" height="162"/><rect x="${tx}" y="${y+150}" width="${tw}" height="44"/>`;
 }
 const clip=`<defs><clipPath id="catalog-viewport"><rect x="${vx}" y="${vy}" width="${vw}" height="${vh}"/></clipPath></defs>`;
 return clip+layer('catalog-surfaces','共享图谱；两列桌面 / 三列宽屏；横向节点',surfaces+`<g clip-path="url(#catalog-viewport)">${ns}</g>`)+layer('catalog-images','完整现有建筑示意；去除圆章',images+`<g clip-path="url(#catalog-viewport)">${ni}</g>`)+layer('catalog-live-labels','可编辑真实名称 / 等级 / 收益 / 前置 / 行动',labels+`<g clip-path="url(#catalog-viewport)">${nt}</g>`)+layer('catalog-state','在建 / 未建 / 锁定状态',`<desc>${state}; project values; historical layout only</desc>`)+layer('catalog-hit-regions','仅完整可见节点可点；沿用44高命令',hits+nh,'style="display:none" fill="none" stroke="#ff00ff"');
}
for(const [target,pw,ph,s,ox,oy,lw,lh] of [['desktop',1440,900,1,0,0,1440,900],['mobile',1600,720,684/820,56,12,1504/(684/820),820],['states',1440,900,1,0,0,1440,900]]){
 let baseline=`F:/EasternDynastyBuild/Releases/s1-20261027-104752269/Project/Validation/Visual/s1-20261027-104752269-${target==='mobile'?'mobile-1600x720':'youzhou-1440x900'}/smoke-facility-catalog.png`;
 if(target!=='states'){fs.mkdirSync(path.join(root,'baseline'),{recursive:true});fs.copyFileSync(baseline,path.join(root,'baseline',target+'.png'));}
 save(target+'/catalog.svg',svg(pw,ph,defs+layer('reference-original','2021教程图：只研究设施节点层级；禁止游戏导入',`<image width="767" height="393" href="${data(reference)}"/>`,'style="display:none" sodipodi:insensitive="true"')+layer('baseline-context','104752269旧运行图；并非本轮运行结果',`<image width="${pw}" height="${ph}" href="${data(baseline)}"/>`,'sodipodi:insensitive="true"')+`<g transform="translate(${ox} ${oy}) scale(${s})">${scene(lw,lh,target==='states'?'working':'empty')}</g>`));
}
save('design-spec.json',JSON.stringify({version,status:'draft-awaiting-export',reference:{id:'Q08-A001',path:reference,resolution:[767,393],publishedAt:'2021 (tutorial; source date fields conflict)',s1Status:'unknown',use:'Historical facility atlas hierarchy only; no tree or cost rules imported'},secondaryReference:'STZB-0453',baselineVersion:'s1-20261027-104752269',layout:{cardHeight:206,gap:16,threeColumnThreshold:1460,art:[10,14,216,162],name:[238,8,'width-250',32],state:[238,42,'width-250',22],effect:[238,72,'width-250',24],condition:[238,102,'width-250',28],action:[238,150,'width-250',44],ruleY:204,hall:[12,56,184,138]},components:['catalog_sheet','catalog_rule','catalog_category','catalog_category_active'],reused:['facility_command','facility_command_hover'],notes:['Complete existing project building art; no new raster algorithm.','Horizontal node is project adaptation of compact unframed reference atlas.','Only fully visible nodes enable actions; existing routes/filters/back retained.','Baseline surrounding chrome is old runtime, not current screenshot.']},null,2));
const spec=JSON.parse(fs.readFileSync(path.join(root,'design-spec.json'),'utf8'));
Object.assign(spec.reference,{s1Status:'historical',excludedFromS1:true,currentS1Applicability:'unknown',versionId:'learn-question-20261025-154754815-Q08-867e7eab10fd38fe0b67',sha256:'857a6e5afaa30ad8669c8b9197ba5cfe59ddf1362d30bf6158940d2be993555e'});
spec.secondaryReference={id:'STZB-0453',versionId:'base-d728496fa74ee1ee61ed',resolution:[510,288],s1Status:'unknown',excludedFromS1:false,publishedAt:'unknown',sha256:'a4f629bfbae411dfcf180358c23e12b300c93b078e8929da803371fb9bc04f4f'};
spec.fonts={family:'Microsoft YaHei; Runtime uses existing Chinese body font',name:22,state:14,effect:14,condition:14,action:16,category:17,heading:24,minimum:13,sourceNote:12,fit:'Fit using actual GUI width and height; reduce to minimum, then fail diagnostic if still clipped.'};
spec.colors={heading:'#D8C68F',body:'#E3E2D9',state:'#D8C68F',effect:'#CBD4D5',condition:'#B8C3C7',action:'#202526',category:'#262D32',categoryActive:'#5A5747',sheet:['#3A4041','#292F33','#21262C']};
spec.states=['unbuilt','built','hall-required','queue-blocked','working','maximum-level','unavailable','scrolled-partial-input-disabled','modal-covered-input-disabled'];
spec.runtime=['CityFacilityAtlasLayout.cs','CityFacilityAtlasPresentation.cs','CityFacilityCatalogPresentation.cs','Diagnostics/CityFacilityCatalogDiagnostics.cs'];
save('design-spec.json',JSON.stringify(spec,null,2));
console.log('Editable catalog sources composed. Export and inspect before Runtime edits.');
