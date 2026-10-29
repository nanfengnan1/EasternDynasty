// Editable layout only; all production raster pixels come from Inkscape.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261028-204251299',dir=`Docs/DesignUI/${version}`;
const save=(p,s)=>{fs.mkdirSync(path.dirname(`${dir}/${p}`),{recursive:true});fs.writeFileSync(`${dir}/${p}`,s);};
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const data=p=>`data:image/${p.endsWith('.jpg')?'jpeg':'png'};base64,${fs.readFileSync(p).toString('base64')}`;
const image=(p,x,y,w,h,stretch=false)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" ${stretch?'preserveAspectRatio="none"':''} href="${data(p)}"/>`;
const layer=(id,b,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${id}" ${extra}>${b}</g>`;
const label=(x,y,t,size=13,color='#e9e4d3')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}">${t}</text>`;
const locked='sodipodi:insensitive="true"';
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${b}</svg>`;
const reference='F:/工作项目/率土之滨/rag/assets/d8/d8a56f2b7e2504fa7319cfa8f022d5cb0ca0168e3a7ac013f6883da1f848a350.jpg';
const support='F:/工作项目/率土之滨/rag/assets/4f/4ff3395ce69c1849a0b0561c1c3b2cc0f875fb1fdf2db9b9e5439c449cbd8376.jpg';
const bdir='F:/EasternDynastyBuild/Releases/s1-20261028-193752561/Project/Validation/Visual/';
// Publication can recapture a candidate's PNGs. Freeze this round's research
// inputs locally so rerunning the design cannot silently change its baseline.
function referenceCopy(name,input){const p=`${dir}/baseline/${name}`;fs.mkdirSync(path.dirname(p),{recursive:true});if(!fs.existsSync(p))fs.copyFileSync(input,p);return p;}
const baselines=[{path:bdir+'s1-20261028-193752561-youzhou-1440x900/smoke-city-overview.png',size:[1440,900],scale:1,x:0,y:0,logicalWidth:1440},
 {path:bdir+'s1-20261028-193752561-mobile-1600x720/smoke-city-overview.png',size:[1600,720],scale:684/820,x:56,y:12,logicalWidth:1504/(684/820)}].map((b,i)=>({...b,sourcePath:b.path,path:referenceCopy(i===0?'desktop-193752561.png':'mobile-193752561.png',b.path)})).map(b=>({...b,sha256:sha(b.path)}));
const ui='Assets/EasternDynasty/Resources/YouzhouUI/',townHall='Validation/CityArt/TownHall.png';
const resource='Assets/EasternDynasty/Resources/YouzhouReferences/resource_hud.png';
const cleanCurrency=referenceCopy('currency-map-193752561.png',bdir+'s1-20261028-193752561-youzhou-1440x900/smoke-hud-design-header.png');
const backdrop=w=>`<rect width="${w}" height="70" fill="#17201e"/>`;
function crop(p,x,y,w,h,r,iw,ih,id){
 const [sx,sy,sw,sh]=r,s=Math.min(w/sw,h/sh),dx=x+(w-sw*s)/2,dy=y+(h-sh*s)/2;
 return `<defs><clipPath id="${id}"><rect x="${dx}" y="${dy}" width="${sw*s}" height="${sh*s}"/></clipPath></defs><g clip-path="url(#${id})">${image(p,dx-sx*s,dy-sy*s,iw*s,ih*s)}</g>`;
}
function header(prefix,w,stress=false,hover=false,disabled=false){
 const pitch=Math.min(168,(w-604)/4);
 const stocks=stress?['999,999','100万','1亿','10000万','12.35亿','12,345']:['3,024','4,036','3,098','916','1,692','0'];
 const details=['木材 +240/时','粮食 +360/时','石料 +540/时','铁矿 +90/时','金币 +120/时','试玩灵玉'];
 let surfaces=image(ui+'header_identity.png',16,4,246,62,true)+image(ui+(hover?'city_marker_selected':'city_marker_crest')+'.png',16,4,62,62)+image(ui+'header_ledger.png',280,5,pitch*4,58,true);
 let pictures=image(townHall,22,13,50,44),texts=stress?label(84,21,'幽州城内全景界面验收用',14)+label(84,40,'长名称主城',14):label(84,31,'新城',20);
 texts+=label(84,60,'幽州 · 府邸 1 级',13,'#c8c7b7');
 let hits=`<rect x="16" y="4" width="246" height="62"/><rect x="0" y="0" width="${w}" height="70"/>`;
 const crops=[[202,9,38,30],[722,9,41,31],[548,9,41,31],[374,9,38,31]];
 for(let i=0;i<6;i++){
  const x=i<4?280+i*pitch:280+pitch*4+16+(i-4)*150,cellW=i<4?pitch-6:144;
  if(i>=4)surfaces+=image(ui+'header_currency.png',x,5,144,58,true);
  if(i<4)pictures+=crop(resource,x+6,19,32,26,crops[i],894,74,prefix+'resource-'+i);
  else pictures+=crop(cleanCurrency,x+7,19,26,28,i===4?[975,19,26,28]:[1125,19,26,28],1440,900,prefix+'currency-'+i);
  const tx=x+(i<4?44:40);
  texts+=label(tx,24,details[i],13,'#d4c797')+label(tx,53,stocks[i],21);
  hits+=`<rect x="${x}" y="5" width="${cellW}" height="58"/>`;
 }
 // The clear/backdrop remains opaque even while modal widgets are disabled.
 return layer(prefix+'header-backdrop',backdrop(w))+'<g'+(disabled?' opacity=".5"':'')+'>'+layer(prefix+'header-surfaces',surfaces)+layer(prefix+'header-images',pictures)+layer(prefix+'header-live-labels',texts)+layer(prefix+'header-state','',hover?'data-hover="true"':'')+layer(prefix+'header-hit-regions',hits,'style="display:none" fill="none" stroke="#ff44bb"')+'</g>';
}
for(const [i,target] of ['desktop','mobile'].entries()){
 const b=baselines[i],[w,h]=b.size;
 const body=layer('board-background',`<rect width="${w}" height="${h+64}" fill="#15212a"/>`)+layer('reference-original',image(reference,0,0,500,280)+image(support,0,280,720,405),'style="display:none" '+locked)+layer('annotation',label(20,25,'主城顶部设计：完整不透明背景、独立城徽横签、连续六资源账栏',16)+label(20,49,'仅顶部为本轮设计；其余为193版旧运行图。手机独立安全区；长名和数值为设计压力示例。',13))+
 `<g transform="translate(0 64)">${layer('baseline-context',image(b.path,0,0,w,h),locked)}<g transform="translate(${b.x} ${b.y}) scale(${b.scale})">${header('',b.logicalWidth,i===1)}</g></g>`;
 save(target+'/city-header.svg',svg(w,h+64,body));
}
let states=layer('previous-page',`<rect width="1440" height="390" fill="#111e29"/>${label(1062,78,'旧页金币 1,600',23)}${label(1254,78,'灵玉 300',23)}`);
for(const [i,n] of ['normal','hover','modal'].entries())states+=label(16,24+i*130,['正常 / 不透明底面覆盖上一页数字','悬停 / 长城名与大数值','模态 / 底面仍保持不透明，控件禁用'][i],15)+`<g transform="translate(0 ${34+i*130})">${header(n+'-',1440,i===1,i===1,i===2)}</g>`;
save('states/city-header.svg',svg(1440,390,states));
save('components/city-header.svg',svg(1440,70,layer('city-header-backdrop',backdrop(1440))));
save('design-spec.json',JSON.stringify({version,status:'editable-awaiting-export',style:'inkscape-city-ledger-v2',reference:{id:'STZB-0428',path:reference,sha256:sha(reference),size:[500,280],date:'2015-09-10',scope:'Historical city page; circular identity and compact two-row resources only; current S1 unknown'},support:{id:'STZB-0527',path:support,sha256:sha(support),size:[720,405],date:'2023-02-01',scope:'Later map header only; no claim of current city appearance'},baselineVersion:'s1-20261028-193752561',baselines,layout:{backdrop:[0,0,'width',70],identity:[16,4,246,62],crest:[16,4,62,62],art:[22,13,50,44],name:[84,7,174,30],wrappedName:[84,5,174,38],detail:[84,44,174,20],production:[280,5,'min(168,(width-604)/4)*4',58],currencyX:'280+pitch*4+16+index*150',stockY:30,detailY:6},fonts:{name:20,wrappedName:14,stock:21,detail:13},components:['ui.youzhou.city_header_backdrop'],reusedAssets:['header_identity','header_ledger','header_currency','city_marker_crest','city_marker_selected'].map(p=>({path:ui+p+'.png',sha256:sha(ui+p+'.png')})),previewAssets:[townHall,resource].map(p=>({path:p,sha256:sha(p)})),notes:['Opaque city-only backdrop is required because the map camera starts below this band; transparent overlays alone retain previous-page pixels.','Map remains transparent over actual terrain. No city camera, facility art generation, rules, pool or save changes.','The town hall preview reuses existing project output; all historical image crops remain preview/reference material.','Dynamic values and Chinese text remain Runtime controls; component contains only the full opaque backing.','City identity uses the existing shared crest/identity/ledger rather than adding unrelated decoration.']},null,2));
console.log('City ledger drafts ready; actual export and inspection required before Runtime.');
