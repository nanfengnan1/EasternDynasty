// Assemble editable existing components; only Inkscape exports previews.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261027-211752367',root=path.resolve('Docs/DesignUI',version);
const save=(p,s)=>{fs.mkdirSync(path.dirname(path.join(root,p)),{recursive:true});fs.writeFileSync(path.join(root,p),s);};
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const data=p=>'data:image/'+(p.endsWith('.jpg')?'jpeg':'png')+';base64,'+fs.readFileSync(p).toString('base64');
const layer=(id,title,body,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${title}" ${extra}>${body}</g>`;
const image=(p,x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" preserveAspectRatio="xMidYMid meet" href="${data(p)}"/>`;
const text=(x,y,t,size=16,color='#e0d2aa',extra='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}" text-anchor="middle" ${extra}>${t}</text>`;
const original='F:/工作项目/率土之滨/rag/assets/d8/d8a56f2b7e2504fa7319cfa8f022d5cb0ca0168e3a7ac013f6883da1f848a350.jpg';
const secondary='F:/工作项目/率土之滨/rag/assets/20/20c7ebba88a57755f11b7b69c6f93e6544ef25ad04dcb34e0f1380cfe153433f.jpg';
const newer='F:/工作项目/率土之滨/rag/assets/4f/4ff3395ce69c1849a0b0561c1c3b2cc0f875fb1fdf2db9b9e5439c449cbd8376.jpg';
const componentSource='Docs/DesignUI/s1-20261027-114202570/components/navigation.svg';
const components=fs.readFileSync(componentSource,'utf8');
const shapes=['normal','active','recruit','letter','book'].map(id=>components.match(new RegExp(`<g id="${id}">[\\s\\S]*?</g>`))[0]).join('');
const gradients=components.match(/<defs>([\s\S]*?)<\/defs>/)[1];
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${gradients}${shapes}</defs>${b}</svg>`;
const referenceLayer=()=>layer('reference-original','锁定参考：2015同页；2021共享菜单；2023较新地图仅对比',image(original,0,0,500,280)+image(secondary,0,300,1170,540)+image(newer,1180,300,720,405),'style="display:none" sodipodi:insensitive="true"');
const asset=n=>'Assets/EasternDynasty/Resources/'+n+'.png';
// SVG viewBoxes follow actual exported object bounds, keeping icon proportions
// identical to ScaleToFit and recruitment's 71-to-70 production skin mapping.
const symbol=(id,x,y,w,h,viewBox,preserve='xMidYMid meet')=>`<svg x="${x}" y="${y}" width="${w}" height="${h}" viewBox="${viewBox}" preserveAspectRatio="${preserve}"><use href="#${id}"/></svg>`;
function footer(w,h,{empty=false,hover=-1,active=true,speed='时间暂停 · 继续',suffix=''}={}){
 const y=h-74,labels=['主城','领地','武将','战报','帮助'];
 let surfaces=`<rect x="0" y="${h-78}" width="${w}" height="78" fill="url(#shelf)"/>`,images='',titles='',hits='';
 for(let i=0;i<5;i++){
  const x=20+116*i;
  surfaces+=symbol((active&&i===0)||i===hover?'active':'normal',x,y,108,70,'0 0 108 70');
  if(i<2)images+=image(asset('Youzhou/'+(i?'mountain':'city')),x+34,y+8,70,56);
  else if(i===2){
   if(empty)titles+=text(x+69,y+40,'未编队',16,'#46413d');
   else for(let slot=0;slot<3;slot++)images+=image(asset('Portraits/s1_'+['100744','100039','100746'][slot]),x+35+slot*21,y+8,25,56);
  }else images+=symbol(i===3?'letter':'book',x+34,y+8,70,56,i===3?'5 3 74 40':'9 4.5 66 36');
  titles+=text(x+17.5,y+23,labels[i][0],18)+text(x+17.5,y+48,labels[i][1],18);
  hits+=`<rect id="hit-${i}${suffix}" x="${x}" y="${y}" width="108" height="70"/>`;
 }
 const rx=w-152,ux=Math.max(620,(w-262)/2);
 surfaces+=symbol('recruit',rx+12.5,y,107,70,'12.5 -0.5 107 71','none');
 images+=image(asset('Portraits/s1_100039'),rx+46,y+7,40,38);
 titles+=text(rx+66,y+63,'招募');
 for(const [x,ww,label] of [[ux,82,'保存'],[ux+82,180,speed]]){
  surfaces+=`<rect x="${x}" y="${h-61}" width="${ww}" height="49" fill="#16212a" fill-opacity=".85"/><rect x="${x+.5}" y="${h-60.5}" width="${ww-1}" height="48" fill="none" stroke="#797765"/>`;
  titles+=text(x+ww/2,h-30,label);
  hits+=`<rect x="${x}" y="${h-61}" width="${ww}" height="49"/>`;
 }
 hits+=`<rect x="${rx}" y="${y}" width="132" height="70"/>`;
 return layer('footer-surfaces'+suffix,'既有矢量图签/招募/渐隐托底',surfaces)
  +layer('footer-images'+suffix,'真实既有画像/地形/建筑；文书可编辑',images)
  +layer('footer-live-labels'+suffix,'竖标题和真实保存/时间文字',titles)
  +layer('footer-state'+suffix,'主城选中；悬停共用既有状态；不烘焙动态数据','')
  +layer('footer-hit-regions'+suffix,'八个操作父范围-隐藏',hits,'style="display:none" fill="none" stroke="#ff00ff"');
}
for(const target of ['desktop','mobile']){
 const mobile=target==='mobile',w=mobile?1600:1440,h=mobile?720:900;
 const x=mobile?56:0,y=mobile?12:0,sw=mobile?1504:w,sh=mobile?684:h;
 const scale=Math.min(sw/1440,sh/(mobile?820:900)),lw=sw/scale,lh=sh/scale;
 const context=`Docs/DesignUI/s1-20261027-201251989/${target}/exports/city-header-preview.png`;
 const bg=layer('baseline-context','104752269运行基线+已导出主城设计；非本轮运行图',image(context,0,0,w,h)+`<rect x="${x}" y="${y+(lh-78)*scale}" width="${sw}" height="${78*scale}" fill="#111e29"/>`,'sodipodi:insensitive="true"');
 save(`${target}/city-footer.svg`,svg(w,h,referenceLayer()+bg+`<g transform="translate(${x} ${y}) scale(${scale})">${footer(lw,lh)}</g>`));
}
let stateBody=referenceLayer()+layer('baseline-context','状态说明底；不是游戏截图','<rect width="1440" height="570" fill="#111e29"/>','sodipodi:insensitive="true"');
stateBody+=text(720,35,'城内全景 / 主城选中 / 当前真实编队')+footer(1440,140);
stateBody+=text(720,210,'设施或任务子页 / 仍保留主城归属 / 空编队 / 武将悬停')+footer(1440,315,{empty:true,hover:2,suffix:'-empty'});
stateBody+=text(720,385,'模态背景 / 图文保留 / GUI 禁用且不登记背景命中')+`<g opacity=".5">${footer(1440,490,{suffix:'-modal'})}</g>`;
stateBody+=text(720,545,'布局稿只展示导航状态；真实点击、字体、遮罩隔离由发布者验收。',16);
save('states/city-footer.svg',svg(1440,570,stateBody));
const spec={version,status:'editable-awaiting-export',references:[
 {id:'STZB-0428',path:original,sha256:sha(original),size:[500,280],year:2015,scope:'City same-page placement context only; low-resolution historical image, not production pixels.'},
 {id:'STZB-0499',path:secondary,sha256:sha(secondary),size:[2340,1080],year:2021,scope:'Shared navigation visual language from map; adapted to project city shell, NOT claimed official city layout.'},
 {id:'STZB-0527',path:newer,sha256:sha(newer),size:[720,405],year:2023,scope:'Newer map variant reviewed; not corresponding city page. Does not establish current S1 city design.'}],
 baselineVersion:'s1-20261027-104752269',baselines:['desktop','mobile'].map(t=>({path:`Docs/DesignUI/s1-20261027-190751837/baseline/${t}-104752269.png`,sha256:sha(`Docs/DesignUI/s1-20261027-190751837/baseline/${t}-104752269.png`)})),
 reusedEditableComponents:{path:componentSource,sha256:sha(componentSource),receipt:'Docs/DesignUI/s1-20261027-114202570/components/exports/export-receipt.json'},
 components:['nav_ticket','nav_ticket_active','nav_recruit','nav_letter','nav_book','nav_shelf'].map(x=>'ui.youzhou.'+x),
 productionIds:['nav_ticket','nav_ticket_active','nav_recruit','nav_letter','nav_book','nav_shelf'].map(x=>'ui.youzhou.'+x),
 layout:{footerHeight:78,entry:[20,-74,108,70],button:[108,70],left:20,pitch:116,yFromBottom:74,art:[34,8,70,56],caption:[5,4,25,50],recruit:[-152,-74,132,70],recruitRight:20,recruitArt:[46,7,40,38],recruitCaption:[8,47,116,22],utilityX:'max(620,(width-262)/2)',save:[82,49],clock:[180,49],utilityBottom:12},
 fonts:{title:18,titleMinimum:14,caption:16,captionMinimum:12,family:'Microsoft YaHei / Noto Sans SC, measured at runtime'},
 runtime:'MapHudPresentation.DrawYouzhouMapFooter shared by city and map; MapHudNavigationLayout unchanged',
 notes:['No new production pixels or imported assets; reuse six provenance-checked stable UI IDs and existing portraits.','No new runtime illustration, building art or screenshot upscaling.','Main-city art and letter/book keep native proportions; image slot is taller (56 vs 43), actual artwork sizes differ by aspect ratio.','78-unit footprint and all commands preserved. Six entries grouped left/right; utilities centered.','Context is old runtime plus preceding inspected designs, NOT current runtime proof.','2015 same-page image is insufficient for current city art; existing S1UX-04 and S1DEV-06 gaps remain.']};
save('design-spec.json',JSON.stringify(spec,null,2));
console.log('City footer editable draft composed from existing provenance-checked vectors.');
