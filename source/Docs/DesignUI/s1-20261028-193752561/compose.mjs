// Editable SVG composition; production PNG pixels are exported only by Inkscape.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261028-193752561',dir=`Docs/DesignUI/${version}`;
const save=(p,s)=>{fs.mkdirSync(path.dirname(`${dir}/${p}`),{recursive:true});fs.writeFileSync(`${dir}/${p}`,s);};
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const data=p=>`data:image/${p.endsWith('.jpg')?'jpeg':'png'};base64,${fs.readFileSync(p).toString('base64')}`;
const image=(p,x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" href="${data(p)}"/>`;
const layer=(id,b,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${id}" ${extra}>${b}</g>`;
const label=(x,y,t,size=13,color='#e9e4d3')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}">${t}</text>`;
const locked='sodipodi:insensitive="true"';
const defs=`<linearGradient id="identity-smoke"><stop stop-color="#17201e" stop-opacity=".94"/><stop offset=".85" stop-color="#17201e" stop-opacity=".8"/><stop offset="1" stop-color="#17201e" stop-opacity="0"/></linearGradient>`;
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}</defs>${b}</svg>`;
const reference='F:/工作项目/率土之滨/rag/assets/4f/4ff3395ce69c1849a0b0561c1c3b2cc0f875fb1fdf2db9b9e5439c449cbd8376.jpg';
const support='F:/工作项目/率土之滨/rag/assets/20/20c7ebba88a57755f11b7b69c6f93e6544ef25ad04dcb34e0f1380cfe153433f.jpg';
const bdir='F:/EasternDynastyBuild/Releases/s1-20261028-183252245/Project/Validation/Visual/';
const baselines=[{path:bdir+'s1-20261028-183252245-youzhou-1440x900/smoke-hud-design-header.png',size:[1440,900],scale:1,x:0,y:0,logicalWidth:1440},
 {path:bdir+'s1-20261028-183252245-mobile-1600x720/smoke-hud-long-values.png',size:[1600,720],scale:5/6,x:56,y:12,logicalWidth:1804.8}].map(b=>({...b,sha256:sha(b.path)}));
const ui='Assets/EasternDynasty/Resources/YouzhouUI/',city='Assets/EasternDynasty/Resources/Youzhou/city.png';
const resource='Assets/EasternDynasty/Resources/YouzhouReferences/resource_hud.png';
const identity=(hover=false)=>`<rect width="246" height="62" fill="#17201e" fill-opacity="0"/><rect x="32" y="7" width="214" height="48" fill="url(#identity-smoke)"/><path d="M62 54H230" stroke="#8f8b6f" stroke-opacity=".6"/>`;
const ledger=()=>'<rect width="672" height="58" fill="#17201e" fill-opacity=".45"/><path d="M0 57.5H672 M168 9V49 M336 9V49 M504 9V49" stroke="#aaa68a" stroke-opacity=".28"/>';
function header(prefix,w,stress=false,hover=false,disabled=false){
 const stocks=stress?['999,999','100万','1亿','10000万','12.35亿','12,345']:['3,000','4,000','1,800','900','1,600','300'];
 const details=['木材 +240/时','粮食 +360/时','石料 +180/时','铁矿 +90/时','金币 +120/时','试玩灵玉'];
 let surfaces=image(ui+'header_veil.png',0,0,w,70)+`<g transform="translate(16 4)">${identity(hover)}</g>`+image(ui+(hover?'city_marker_selected':'city_marker_crest')+'.png',16,4,62,62)+`<g transform="translate(280 5)">${ledger()}</g>`;
 let pictures=image(city,22,13,50,44),texts=stress?label(84,29,'界面压力验收用长名称主城',14):label(84,31,'幽州主城',20);
 texts+=label(84,60,'幽州 · 领地 0',13,'#c8c7b7');
 let hits='<rect x="16" y="4" width="246" height="62"/>';
 const crops=[[202,9,38,30],[722,9,41,31],[548,9,41,31],[374,9,38,31]];
 for(let i=0;i<6;i++){
  const x=i<4?280+i*168:968+(i-4)*150,cellW=i<4?162:144;
  if(i>=4)surfaces+=image(ui+'header_currency.png',x,5,144,58);
  if(i<4){const [cx,cy,cw,ch]=crops[i],k=Math.min(32/cw,26/ch),ix=x+6+(32-cw*k)/2,iy=19+(26-ch*k)/2;
   pictures+=`<defs><clipPath id="${prefix}resource-${i}"><rect x="${ix}" y="${iy}" width="${cw*k}" height="${ch*k}"/></clipPath></defs><g clip-path="url(#${prefix}resource-${i})">${image(resource,ix-cx*k,iy-cy*k,894*k,74*k)}</g>`;
  }else{
   // Existing runtime symbols are represented by a native-size baseline sample, not new production art.
   const b=baselines[0],bx=i===4?1131:1281;
   pictures+=`<defs><clipPath id="${prefix}currency-${i}"><rect x="${x+7}" y="19" width="26" height="28"/></clipPath></defs><g clip-path="url(#${prefix}currency-${i})">${image(b.path,x+7-bx,19-17,1440,900)}</g>`;
  }
  const tx=x+(i<4?44:40);
  texts+=label(tx,24,details[i],13,'#d4c797')+label(tx,53,stocks[i],21);
  hits+=`<rect x="${x}" y="5" width="${cellW}" height="58"/>`;
 }
 return `<g ${disabled?'opacity=".5"':''}>`+layer(prefix+'header-surfaces',surfaces)+layer(prefix+'header-images',pictures)+layer(prefix+'header-live-labels',texts)+layer(prefix+'header-state','',hover?'data-hover="true"':'')+layer(prefix+'header-hit-regions',hits,'style="display:none" fill="none" stroke="#ff44bb"')+'</g>';
}
for(const [i,target] of ['desktop','mobile'].entries()){
 const b=baselines[i],[w,h]=b.size;
 const context=`<rect x="${b.x}" y="${b.y}" width="${b.logicalWidth*b.scale}" height="${70*b.scale}" fill="#536047"/>`;
 const body=layer('board-background',`<rect width="${w}" height="${h+64}" fill="#15212a"/>`)+layer('reference-original',image(reference,0,0,720,405)+image(support,0,0,2340,1080),'style="display:none" '+locked)+layer('annotation',label(20,25,'设计预览：身份图章＋连续资源账栏；仅顶部为本轮设计，背景为183轮旧运行图',16)+label(20,49,'上产量／下库存，货币同轴；顶部中性背景仅验证透明层，手机采用独立基线与安全区',13))+
 `<g transform="translate(0 64)">${layer('baseline-context',image(b.path,0,0,w,h),locked)}${layer('terrain-context-sample',context,locked)}<g transform="translate(${b.x} ${b.y}) scale(${b.scale})">${header('',b.logicalWidth,i===1)}</g></g>`;
 save(target+'/header.svg',svg(w,h+64,body));
}
let states=layer('background','<rect width="1440" height="370" fill="#566148"/>');
for(const [i,n] of ['normal','hover','modal'].entries())states+=label(16,24+i*120,['正常 / 资源真实数值示例','悬停 / 长名与大数值示例','模态 / 全部背景禁用'][i],15)+`<g transform="translate(0 ${34+i*120})">${header(n+'-',1440,i===1,i===1,i===2)}</g>`;
save('states/header.svg',svg(1440,370,states));
save('components/header.svg',svg(1400,300,`<g id="header-identity" transform="scale(2)">${identity()}</g><g id="header-ledger" transform="translate(0 160) scale(2)">${ledger()}</g>`));
save('design-spec.json',JSON.stringify({version,status:'editable-awaiting-export',reference:{id:'STZB-0527',path:reference,sha256:sha(reference),size:[720,405],date:'2023-02-01',scope:'Historical map HUD; current S1 appearance unknown; layout reference only'},support:{id:'STZB-0499',path:support,sha256:sha(support),size:[2340,1080],date:'2021-12-22',scope:'Sixth-anniversary map reference, not current S1'},baselines,layout:{identity:[16,4,246,62],crest:[16,4,62,62],art:[22,13,50,44],name:[84,7,174,30],wrappedName:[84,5,174,38],identityDetail:[84,44,174,20],productionStart:280,productionPitch:'min(168,(width-604)/4)',productionCell:['pitch-6',58],currencyStart:'280+pitch*4+16',currencyPitch:150,currencyWidth:144,stock:[44,30,'cell-48',26],detail:[44,6,'cell-48',23],icon:[6,19,32,26],currencyStock:[40,30,100,26],currencyDetail:[40,6,100,23],headerHeight:70},fonts:{ruler:20,stock:21,rate:13,secondary:13},components:['ui.youzhou.header_identity','ui.youzhou.header_ledger'],reusedAssets:[ui+'header_veil.png',ui+'header_currency.png',ui+'city_marker_crest.png',ui+'city_marker_selected.png',city,resource].map(p=>({path:p,sha256:sha(p)})),notes:['Project adaptation: compact contiguous six-resource ledger, larger complete city image with existing map crest; no invented official capacity or currency controls.','The source retains native-size historical resource samples below their source dimensions; no upscale or new standalone resource icon claimed.','All live text remains separate; design background is an explicitly labelled collage, not a new runtime screenshot.','Top header input and camera boundaries are unchanged; blank upper-right band does not become a new map interaction region.']},null,2));
console.log('Header editable drafts created. Export and inspect before Runtime integration.');
