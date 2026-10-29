// Editable vector composition. Raster output is produced only by Export-UIDesign.ps1 / Inkscape.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261028-130751956',root=path.join(process.cwd(),'Docs/DesignUI',version);
const ref='F:/工作项目/率土之滨/rag/assets/20/20c7ebba88a57755f11b7b69c6f93e6544ef25ad04dcb34e0f1380cfe153433f.jpg';
const base=s=>`F:/EasternDynastyBuild/Releases/s1-20261028-110251322/Project/Validation/Visual/s1-20261028-110251322-${s}/smoke-hud-design-quest.png`;
const art='Assets/EasternDynasty/Resources/Youzhou/city.png';
const uri=p=>'data:image/'+(p.endsWith('.jpg')?'jpeg':'png')+';base64,'+fs.readFileSync(p).toString('base64');
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const save=(p,v)=>{const dest=path.join(root,p);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.writeFileSync(dest,v);};
const layer=(id,label,v,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${label}" ${extra}>${v}</g>`;
const defs='<linearGradient id="ribbon-tone"><stop stop-color="#20221c" stop-opacity=".97"/><stop offset=".72" stop-color="#452a24" stop-opacity=".94"/><stop offset="1" stop-color="#452a24" stop-opacity=".2"/></linearGradient><linearGradient id="crest-tone" x2="0" y2="1"><stop stop-color="#29291f"/><stop offset="1" stop-color="#55432e"/></linearGradient>';
const crest=(selected=false)=>`<rect width="52" height="52" fill="none"/><circle cx="26" cy="26" r="25" fill="url(#crest-tone)" stroke="${selected?'#eed9a0':'#a99a72'}" stroke-width="${selected?'2':'1.5'}"/><circle cx="26" cy="26" r="21" fill="none" stroke="#d1bd8a" stroke-opacity=".25"/>`;
const ribbon=(w=140,h=52)=>`<rect width="${w}" height="${h}" fill="none"/><path d="M0 2H${w}V${h-2}H0Z" fill="url(#ribbon-tone)"/><path d="M8 2H${w-4}" stroke="#b6a77d" stroke-opacity=".35"/><path d="M8 ${h-2}H${w-4}" stroke="#b6a77d" stroke-opacity=".25"/>`;
const text=(x,y,s,size,color='#eee4c9')=>`<text x="${x}" y="${y}" font-size="${size}" font-family="Microsoft YaHei,Noto Sans SC" fill="${color}">${s}</text>`;
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}</defs>${b}</svg>`;
function marker(id,x,y,name='新城',detail='主城 · 幽州',wide=false,selected=false){
 const w=wide?304:184,h=wide?72:52,cy=(h-52)/2,wrap=s=>`<g transform="translate(${x} ${y})">${s}</g>`;
 const nameText=wide?text(60,21,'界面压力验收用长名称',16)+text(60,43,'主城完整显示',16):text(60,21,name,16);
 return layer(id+'-surfaces','名签/图章底面',wrap(`<g transform="translate(44)">${ribbon(w-44,h)}</g><g transform="translate(0 ${cy})">${crest(selected)}</g>`))
 +layer(id+'-city-image','已有完整城市派生图',wrap(`<image x="6" y="${cy+12}" width="40" height="28" preserveAspectRatio="xMidYMid meet" href="${uri(art)}"/>`))
 +layer(id+'-live-labels','名称与真实归属分层',wrap(nameText+text(60,h-8,detail,12,'#cbbb94')))
 +layer(id+'-hit-regions','真实点击范围（隐藏）',wrap(`<rect width="${w}" height="${h}"/><rect x="6" y="${cy+12}" width="40" height="28"/><rect x="60" y="2" width="${w-70}" height="${wide?44:24}"/><rect x="60" y="${h-24}" width="${w-70}" height="22"/>`),'style="display:none" fill="none" stroke="#ff00ff"');
}
save('components/city-label.svg',svg(760,144,layer('city-label-components','双倍正式矢量组件',`<g transform="scale(2)"><g id="city-marker-crest">${crest()}</g><g id="city-marker-selected" transform="translate(62)">${crest(true)}</g><g id="city-marker-ribbon" transform="translate(124)">${ribbon()}</g></g>`)));
for(const [target,size,b] of [['desktop',[1440,900],base('youzhou-1440x900')],['mobile',[1600,720],base('mobile-1600x720')]]){
 const [w,h]=size;
 // The full baseline is locked and labelled; the proposed component is on an independent comparison board.
 // No old screenshot pixels are passed off as new runtime output.
 const comparison=layer('baseline-context','110轮原始运行基线（未修改）',`<image width="${w}" height="${h}" href="${uri(b)}"/>`,'sodipodi:insensitive="true"')
 +layer('reference-original','2021原作地图同页（锁定）',`<image x="${w-540}" y="${h+34}" width="520" height="240" href="${uri(ref)}"/>`,'sodipodi:insensitive="true"')
 +layer('design-board','设计组件对照板（非游戏截图）',`<rect y="${h}" width="${w}" height="300" fill="#3d4937"/>${text(20,h+26,'上方为基线叠加设计，非新运行图 / 原图见右侧',16)}`);
 // Reference placed above the board in painter order.
 const reference=layer('reference-preview','原作参考显示',`<image x="${w-540}" y="${h+34}" width="520" height="240" href="${uri(ref)}"/>`,'sodipodi:insensitive="true"');
 const scale=target==='mobile'?.833333:1;
 const proposal=target==='desktop'?marker('context-home',628,306)+marker('context-neutral',628,168,'蓟城','中立城池 · Lv.5'):'<g transform="translate(731.33 235) scale(.833333)">'+marker('context-home',0,0)+'</g><g transform="translate(731.33 133) scale(.833333)">'+marker('context-neutral',0,0,'蓟城','中立城池 · Lv.5')+'</g>';
 save(`${target}/city-label.svg`,svg(w,h+300,comparison+proposal+reference+`<g transform="translate(0 ${h+30}) scale(${scale})">${marker('home',20,20)}${marker('neutral',232,20,'蓟城','中立城池 · Lv.5')}${marker('long',20,100,'','主城 · 幽州',true,true)}</g>`));
}
save('states/city-label.svg',svg(680,340,layer('state-board','中立/己方/选中/长名',`<rect width="680" height="340" fill="#3d4937"/>`)+marker('neutral',20,24,'蓟城','中立城池 · Lv.5')+marker('owned',240,24,'蓟城','己方城池 · Lv.5')+marker('home',20,112)+marker('selected',240,112,'蓟城','中立城池 · Lv.5',false,true)+marker('long',20,208,'','主城 · 幽州',true,true)));
save('design-spec.json',JSON.stringify({version,status:'source-awaiting-export',reference:{id:'STZB-0499',path:ref,sha256:sha(ref),width:2340,height:1080,year:2021,scope:'historical unknown S1; city crest and horizontal name hierarchy only'},baselines:['youzhou-1440x900','mobile-1600x720'].map(s=>({path:base(s),sha256:sha(base(s))})),layout:{minimumWidth:184,maximumWidth:304,height:52,wrappedHeight:72,crest:[0,'(height-52)/2',52,52],image:[6,'(height-52)/2+12',40,28],name:[60,2,'width-70','24 or 44'],detail:[60,'height-24','width-70',22],ribbon:[44,0,'width-44','height'],anchor:'centered over projected city, bottom 12 above anchor'},fonts:{name:16,minimumName:14,detail:12},components:['city_marker_crest','city_marker_selected','city_marker_ribbon'],reusedArt:{id:'map.youzhou.city',path:art,sha256:sha(art),scope:'existing derived image, not an official city icon'},notes:['No current-S1 rule or skin claim. No screenshot is a production asset.','Round city crest and narrow dark horizontal name strip follow visible 2021 reference; 2023 auxiliary screenshot is too small for icon artwork.','Separate status row and responsive width are project readability/touch adaptations.','Desktop/mobile boards use different real baselines and mobile component scale; boards are not new game screenshots.']},null,2));
console.log('Editable city marker design prepared for Inkscape.');
