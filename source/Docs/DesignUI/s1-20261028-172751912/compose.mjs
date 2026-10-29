// Editable navigation layout. Inkscape, not this script, exports all image pixels.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261028-172751912',root=path.resolve('Docs/DesignUI',version);
const save=(p,s)=>{fs.mkdirSync(path.dirname(path.join(root,p)),{recursive:true});fs.writeFileSync(path.join(root,p),s);};
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const asset=n=>'Assets/EasternDynasty/Resources/'+n+'.png';
const reference='F:/工作项目/率土之滨/rag/assets/4f/4ff3395ce69c1849a0b0561c1c3b2cc0f875fb1fdf2db9b9e5439c449cbd8376.jpg';
const image=(p,x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" preserveAspectRatio="xMidYMid meet" href="data:image/${p.endsWith('.jpg')?'jpeg':'png'};base64,${fs.readFileSync(p).toString('base64')}"/>`;
const layer=(id,b,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${id}" ${extra}>${b}</g>`;
const txt=(x,y,t,size=16,color='#e0d2aa')=>`<text x="${x}" y="${y}" text-anchor="middle" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}">${t}</text>`;
const skin=(active=false)=>`<rect x="0" y="0" width="108" height="70" fill="${active?'#444538':'#263036'}" fill-opacity=".94"/><rect x="1" y="1" width="106" height="68" fill="none" stroke="${active?'#d0be88':'#8c9186'}"/><rect x="3" y="46" width="102" height="22" fill="${active?'#5a4f35':'#171e21'}"/><path d="M4 45H104" fill="none" stroke="${active?'#b7a777':'#747b72'}" stroke-width=".6"/>`;
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${b}</svg>`;
function footer(w,h,{prefix='',empty=false,selected=1,hover=-1,disabled=false}={}){
 const y=h-74,labels=['主城','领地','武将','战报','帮助'];
 let surfaces=image(asset('YouzhouUI/nav_shelf'),0,h-78,w,78).replace('xMidYMid meet','none'),images='',titles='',hits='',state='';
 for(let i=0;i<5;i++){
  const x=20+116*i,active=i===selected||i===hover;
  const panel=`<g transform="translate(${x},${y})">${skin(active)}</g>`;
  if(active)state+=panel;else surfaces+=panel;
  if(i<2)images+=image(asset('Youzhou/'+(i?'mountain':'city')),x+4,y+3,100,42);
  else if(i===2){
   if(empty)titles+=txt(x+54,y+30,'未编队',14,'#e0d2aa');
   else for(let j=0;j<3;j++)images+=image(asset('Portraits/s1_'+['100744','100039','100746'][j]),x+9+j*30,y+3,30,42);
  }else images+=image(asset('YouzhouUI/'+(i===3?'nav_letter':'nav_book')),x+4,y+3,100,42);
  titles+=txt(x+54,y+62,labels[i],15);hits+=`<rect x="${x}" y="${y}" width="108" height="70"/>`;
 }
 const rx=w-152,ux=Math.max(620,(w-270)/2);
 surfaces+=image(asset('YouzhouUI/nav_recruit'),rx+12.5,y,107,70);
 images+=image(asset('Portraits/s1_100039'),rx+46,y+7,40,38);titles+=txt(rx+66,y+63,'招募');
 for(const [x,width,text] of [[ux,82,'保存'],[ux+90,180,'时间暂停 · 继续']]){
  surfaces+=`<rect x="${x}" y="${h-61}" width="${width}" height="49" fill="#16212a" fill-opacity=".85" stroke="#797765"/>`;
  titles+=txt(x+width/2,h-31,text);hits+=`<rect x="${x}" y="${h-61}" width="${width}" height="49"/>`;
 }
 hits+=`<rect x="${rx}" y="${y}" width="132" height="70"/>`;
 return `<g ${disabled?'opacity=".5"':''}>${layer(prefix+'navigation-surfaces',surfaces)}${layer(prefix+'navigation-state',state)}${layer(prefix+'navigation-images',images)}${layer(prefix+'navigation-live-labels',titles)}${layer(prefix+'navigation-hit-regions',hits,'style="display:none" fill="none" stroke="#ff55aa"')}</g>`;
}
const baselines=[];
for(const [target,w,h,scale,sx,sy] of [['desktop',1440,900,1,0,0],['mobile',1600,720,720/864,56,12]]){
 const base=`F:/EasternDynastyBuild/Releases/s1-20261028-162252252/Project/Validation/Visual/s1-20261028-162252252-${target==='desktop'?'youzhou-1440x900':'mobile-1600x720'}/smoke-hud-design-quest.png`;
 baselines.push({path:base,sha256:sha(base),size:[w,h]});
 const logicalW=target==='mobile'?1804.8:w,logicalH=target==='mobile'?820.8:h;
 const body=layer('board-background',`<rect width="${w}" height="${h+210}" fill="#15212a"/>`)+layer('reference-original',image(reference,0,0,720,405),'style="display:none" sodipodi:insensitive="true"')+
  layer('baseline-context',image(base,0,44,w,h),'sodipodi:insensitive="true"')+
  layer('annotations',txt(w/2,27,'上：162轮运行基线；下：独立设计预览（不是新运行截图）',18)+txt(w/2,h+83,'0527上图下题结构适配 / 入口与78高底栏不变 / 原图仅作布局参考',16))+
  `<g transform="translate(${sx},${h+110}) scale(${scale})">${footer(logicalW,90)}</g>`;
 save(target+'/navigation.svg',svg(w,h+210,body));
}
let states=layer('background','<rect width="1440" height="470" fill="#15212a"/>');
for(const [i,options] of [{selected:1},{selected:0,empty:true,hover:2},{selected:0,disabled:true}].entries())
 states+=txt(720,i*150+25,['地图 / 领地选中 / 三将完整图','主城共用 / 空编队 / 武将悬停','模态 / 禁用背景 / 不登记可操作目标'][i],16)+footer(1440,i*150+125,{...options,prefix:['map-','city-','modal-'][i]});
save('states/navigation.svg',svg(1440,470,states));
save('components/navigation.svg',svg(500,160,`<g id="navigation-panel" transform="scale(2)">${skin()}</g><g id="navigation-panel-active" transform="translate(260,0) scale(2)">${skin(true)}</g>`));
const reused=['YouzhouUI/nav_shelf','YouzhouUI/nav_letter','YouzhouUI/nav_book','YouzhouUI/nav_recruit','Youzhou/city','Youzhou/mountain','Portraits/s1_100744','Portraits/s1_100039','Portraits/s1_100746'];
save('design-spec.json',JSON.stringify({version,status:'editable-awaiting-export',reference:{id:'STZB-0527',path:reference,sha256:sha(reference),size:[720,405],date:'2023-02-01',scope:'Historical third-party map. Current S1 unknown; layout only, no source pixels used in production.'},baselines,layout:{footerHeight:78,entry:[20,'height-74',108,70],pitch:116,art:[4,3,100,42],caption:[4,46,100,22],portraits:{offset:5,pitch:30,width:30,height:42},recruitArt:[46,7,40,38],recruitCaption:[8,47,116,22],utilities:'unchanged max(620,(width-270)/2), 8-unit gap'},fonts:{title:16,minimum:12,empty:14},components:['ui.youzhou.nav_panel','ui.youzhou.nav_panel_active'],reusedAssets:reused.map(n=>({path:asset(n),sha256:sha(asset(n))})),notes:['Five main entries change from vertical title/pale sideboard to complete centered images above horizontal caption rails.','Existing 78-high footprint, six commands and two utilities retained.','Existing illustrations remain project assets, not newly recovered official art.','Shared city footer is project adaptation; current same-page S1 city reference is still missing.','No Unity/client/desktop execution. Design export and runtime review remain distinct.']},null,2));
const cityReference='F:/工作项目/率土之滨/rag/assets/d8/d8a56f2b7e2504fa7319cfa8f022d5cb0ca0168e3a7ac013f6883da1f848a350.jpg';
const cityBaselines=[];
for(const [target,w,h,sx,scale] of [['desktop',1440,900,0,1],['mobile',1600,720,56,720/864]]){
 const base=`F:/EasternDynastyBuild/Releases/s1-20261028-162252252/Project/Validation/Visual/s1-20261028-162252252-${target==='desktop'?'youzhou-1440x900':'mobile-1600x720'}/smoke-city-footer-design.png`;
 cityBaselines.push({path:base,sha256:sha(base),size:[w,h]});
 let board=layer('board-background',`<rect width="${w}" height="${h+210}" fill="#15212a"/>`)+
  layer('reference-original',image(cityReference,0,0,500,280)+image(reference,500,0,720,405),'style="display:none" sodipodi:insensitive="true"')+
  layer('baseline-context',image(base,0,44,w,h),'sodipodi:insensitive="true"')+
  layer('annotations',txt(w/2,27,'主城共享组件：上为162轮基线，下为设计稿；不是新运行截图',18)+txt(w/2,h+83,'主城沿用地图导航，属于项目适配；2015城内原图仅提供同页上下文',16))+
  `<g transform="translate(${sx},${h+110}) scale(${scale})">${footer(target==='desktop'?1440:1804.8,90,{selected:0})}</g>`;
 save('city-'+target+'/navigation.svg',svg(w,h+210,board));
}
const spec=JSON.parse(fs.readFileSync(path.join(root,'design-spec.json'),'utf8'));
spec.fonts.title=15;spec.cityBaselines=cityBaselines;
spec.cityReference={id:'STZB-0428',path:cityReference,sha256:sha(cityReference),size:[500,280],year:2015,scope:'Historical city page only; no current S1 or shared footer equivalence asserted.'};
save('design-spec.json',JSON.stringify(spec,null,2));
console.log('Editable map/city desktop/mobile, states and components drafts written. Export and inspect before acceptance.');
