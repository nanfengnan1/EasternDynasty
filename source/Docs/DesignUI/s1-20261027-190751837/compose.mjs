// Editable SVG components; raster exports are produced by Tools/Export-UIDesign.ps1.
import fs from 'node:fs';
import path from 'node:path';
const version='s1-20261027-190751837',root=path.resolve('Docs/DesignUI',version);
const save=(p,s)=>{fs.mkdirSync(path.dirname(path.join(root,p)),{recursive:true});fs.writeFileSync(path.join(root,p),s);};
const data=p=>'data:image/'+(p.endsWith('.jpg')?'jpeg':'png')+';base64,'+fs.readFileSync(p).toString('base64');
const layer=(id,title,body,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${title}" ${extra}>${body}</g>`;
const text=(x,y,t,size=16,color='#eee6cb',extra='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}" ${extra}>${t}</text>`;
const defs='<linearGradient id="bronze" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#d6bf83"/><stop offset=".55" stop-color="#897348"/><stop offset="1" stop-color="#c5af75"/></linearGradient><linearGradient id="ink" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#364339"/><stop offset="1" stop-color="#18251f"/></linearGradient>';
function seal(state='normal'){
 const active=state==='active',hover=state==='hover';
 return `<rect width="84" height="86" fill="none"/><circle cx="42" cy="34" r="34" fill="${hover?'#efdfa2':'url(#bronze)'}"/><circle cx="42" cy="34" r="${active?31.5:32.5}" fill="url(#ink)"/><circle cx="42" cy="34" r="29.5" fill="none" stroke="${active||hover?'#d9c48a':'#82744e'}" stroke-width=".7"/><path d="M0 59Q42 65 84 59L80 80Q42 92 4 80Z" fill="${active?'#c8b379':'#18251f'}"/><path d="M.5 59.5Q42 65.5 83.5 59.5L79.5 80Q42 91 4.5 80Z" fill="none" stroke="${hover?'#efdfa2':'#aa925f'}" stroke-linejoin="round"/></g>`.replace('</g>','');
}
function back(hover=false){return `<rect width="84" height="64" fill="none"/><circle cx="42" cy="22" r="22" fill="${hover?'#efdfa2':'url(#bronze)'}"/><circle cx="42" cy="22" r="20.5" fill="url(#ink)"/><path d="M0 42H84L80 63H4Z" fill="#18251f"/><path d="M.5 42.5H83.5L79.5 63.5H4.5Z" fill="none" stroke="${hover?'#efdfa2':'#aa925f'}" stroke-linejoin="round"/>`;}
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}</defs>${b}</svg>`;
const componentIds=['dock-seal','dock-seal-active','dock-seal-hover','dock-back','dock-back-hover'];
// Export components at 2x directly from vector paths, never by enlarging a bitmap.
save('components/city-dock.svg',svg(1000,200,layer('components','圆章/选中/悬停与返回组件-2倍矢量导出',componentIds.map((id,i)=>`<g id="${id}" transform="translate(${i*200} 0) scale(2)">${i<3?seal(['normal','active','hover'][i]):back(i===4)}</g>`).join(''))));
const original='F:/工作项目/率土之滨/rag/assets/d8/d8a56f2b7e2504fa7319cfa8f022d5cb0ca0168e3a7ac013f6883da1f848a350.jpg';
const manifest=JSON.parse(fs.readFileSync('Assets/EasternDynasty/Resources/asset-manifest.json','utf8').replace(/^\uFEFF/,''));
const asset=id=>'Assets/EasternDynasty/Resources/'+manifest.entries.find(e=>e.id===id).resourcePath+'.png';
const titles=['城内全景','设施一览','城区管理','主要事宜','部队练兵'];
function pictures(index){
 if(index===0)return `<image x="6" y="2" width="72" height="54" href="${data(asset('map.youzhou.city'))}"/>`;
 if(index===1||index===4)return `<image x="6" y="2" width="72" height="54" href="${data('Validation/CityArt/'+(index===1?'TownHall':'DrillGround')+'.png')}"/>`;
 if(index===2)return Array.from({length:9},(_,n)=>`<rect x="${24+n%3*12}" y="${11+Math.floor(n/3)*12}" width="10" height="10" fill="${n===4?'#d6bc78':'#667463'}"/>`).join('');
 return '<rect x="19.68" y="8.48" width="44.64" height="41.04" fill="#c5bb96"/><path d="M13.2 8.48H70.8M13.2 47.52H70.8" stroke="#9a8050" stroke-width="4"/><path d="M24 16H60M24 21H59M24 26H58" stroke="#464a3c"/><rect x="55.32" y="40.52" width="6" height="6" fill="#87534a"/>';
}
function dock(height,state='normal',backLabel='返回领地'){
 let frames='',images='',labels='',hits='';
 for(let i=0;i<5;i++){
  const y=8+i*90;
  frames+=`<g transform="translate(4 ${y})">${seal(i===0?'active':i===1&&state==='hover'?'hover':'normal')}</g>`;
  images+=`<g transform="translate(4 ${y})">${pictures(i)}</g>`;
  labels+=text(46,y+78,titles[i],16,i===0?'#1c2923':'#eee6cb','text-anchor="middle"');
  hits+=`<rect x="4" y="${y}" width="84" height="86"/>`;
 }
 const by=height-72;
 frames+=`<g transform="translate(4 ${by})">${back(state==='hover')}</g>`;
 labels+=text(46,by+33,'‹',28,'#e1cc8e','text-anchor="middle"')+text(46,by+59,backLabel,16,'#eee6cb','text-anchor="middle"');
 hits+=`<rect x="4" y="${by}" width="84" height="64"/>`;
 return layer('dock-surfaces','独立圆章与下方题签',frames)+layer('dock-images','复用现有城池/建筑图像与城区/文书符号',images)+layer('dock-live-labels','动态中文与返回状态示例-不烘焙',labels)+layer('dock-state','当前页金色题签; 悬停描边','')+layer('dock-hit-regions','五个父目标与独立返回-隐藏',hits,'style="display:none" fill="none" stroke="#ff00ff"');
}
for(const [target,w,h,x,y,s,dh] of [['desktop',1440,900,24,82,1,720],['mobile',1600,720,76.04,80.47,.835,640]]){
 const context=`Docs/DesignUI/s1-20261027-180252216/${target}/exports/city-affairs-preview.png`;
 const bg=layer('reference-original','2015同页圆形设施入口-仅测量隐藏',`<image width="500" height="280" href="${data(original)}"/>`,'style="display:none" sodipodi:insensitive="true"')+layer('baseline-context','104752269运行基线+已接入部队城务设计;非本轮运行截图',`<image width="${w}" height="${h}" href="${data(context)}"/><rect x="${x-1}" y="${y-1}" width="${94*s}" height="${dh*s+2}" fill="#111e29"/>`,'sodipodi:insensitive="true"');
 save(`${target}/city-dock.svg`,svg(w,h,bg+`<g transform="translate(${x} ${y}) scale(${s})">${dock(dh)}</g>`));
}
save('states/city-dock.svg',svg(680,620,'<rect width="680" height="620" fill="#111e29"/>'+['normal','hover','expanded','planning'].map((state,i)=>`<g transform="translate(${i*168+30} 40)">${text(46,-15,['正常','悬停','展开城景','建设选址'][i],14,'#eee6cb','text-anchor="middle"')}${dock(540,state,i===2?'收起城景':i===3?'结束选址':'返回领地').replaceAll('id="','id="'+state+'-')}</g>`).join('')));
save('design-spec.json',JSON.stringify({version,status:'editable-source-awaiting-export-review',reference:{id:'STZB-0428',detail:'STZB-0429',path:original,year:2015,width:500,height:280,sha256:'d8a56f2b7e2504fa7319cfa8f022d5cb0ca0168e3a7ac013f6883da1f848a350',scope:'Historical city page round facility entrance and lower caption; no confirmed newer same-page city panorama. Five-entry left dock is a project adaptation, not an official layout claim. No original pixels imported.'},baselineVersion:'s1-20261027-104752269',layout:{dockWidth:92,navigation:[4,8,84,86],step:90,art:[6,2,72,54],caption:[2,60,80,24],back:[4,'height-72',84,64],backArt:[26,6,32,32],backCaption:[2,42,80,22]},components:['city_dock_seal','city_dock_seal_active','city_dock_seal_hover','city_dock_back','city_dock_back_hover'],fonts:{caption:16,minimum:13,backGlyph:28},colors:{text:'#eee6cb',activeText:'#1c2923',accent:'#e1cc8e'},runtime:'CityDockDesignPresentation / CityDockLayout / CityPresentation',changes:['Remove continuous 92-wide full-height black backing.','Five independent round frames and curved caption plates; active caption carries the selected state.','Art bounds 60x48 to 72x54; full image fitting retained.','Separate circular return above caption, preserves planning/expanded/normal back behavior.'],notes:['Current city canvas and right-side geometry unchanged.','Existing building artwork retained; no additional CitySceneRaster or HudLine algorithms.','Desktop/mobile context includes prior designs; only publisher can supply new game screenshots.']},null,2));
console.log('City dock SVG sources written; awaiting real Inkscape export and inspection.');
