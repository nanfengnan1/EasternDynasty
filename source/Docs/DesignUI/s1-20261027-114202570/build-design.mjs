// Author editable SVG layers. Inkscape, not this script, exports the artwork.
import fs from 'node:fs';
import path from 'node:path';
const root=process.cwd(), dir=path.join(root,'Docs/DesignUI/s1-20261027-114202570');
const uri=p=>'data:image/'+(p.endsWith('.jpg')?'jpeg':'png')+';base64,'+fs.readFileSync(p).toString('base64');
const ref='F:/工作项目/率土之滨/rag/assets/20/20c7ebba88a57755f11b7b69c6f93e6544ef25ad04dcb34e0f1380cfe153433f.jpg';
const baseline='F:/EasternDynastyBuild/Releases/s1-20261027-104752269/Project/Validation/Visual/';
const img=(id,p,x,y,w,h)=>`<image id="${id}" x="${x}" y="${y}" width="${w}" height="${h}" preserveAspectRatio="xMidYMid meet" href="${uri(p)}"/>`;
const asset=n=>path.join(root,'Assets/EasternDynasty/Resources',n+'.png');
const layer=(id,label,body,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${label}" ${extra}>${body}</g>`;
const defs=`<defs><linearGradient id="enamel" x2="0" y2="1"><stop stop-color="#58616a"/><stop offset=".55" stop-color="#303b46"/><stop offset="1" stop-color="#171e28"/></linearGradient><linearGradient id="brass" x2="0" y2="1"><stop stop-color="#d1bf87"/><stop offset=".5" stop-color="#80714d"/><stop offset="1" stop-color="#b8a677"/></linearGradient><linearGradient id="shelf" x2="0" y2="1"><stop stop-color="#131a20" stop-opacity="0"/><stop offset=".5" stop-color="#131a20" stop-opacity=".82"/><stop offset="1" stop-color="#10151b" stop-opacity=".98"/></linearGradient></defs>`;
const frame=(id,active=false)=>`<g id="${id}"><path d="M1 1H107V69H1Z" fill="#e1e1d9" stroke="#8b8d82" stroke-width="2"/><path d="M32 1H107V69H32Z" fill="#c7cdc7" fill-opacity=".5"/><path d="M5 0H30V60L5 55Z" fill="${active?'#773e35':'#302a28'}"/><path d="M5 38V55L30 60V38Z" fill="#773e35" fill-opacity=".6"/>${active?'<path d="M1 68H107" stroke="#c8ad72" stroke-width="3"/>':''}</g>`;
const recruit=`<g id="recruit"><path d="M66 1L110 34L66 69L22 34Z" fill="#d8d8cf" stroke="#373734" stroke-width="2"/><path d="M66 6L103 34L66 63L29 34Z" fill="#46413d"/><path d="M13 48H119V67H13Z" fill="#362a22" fill-opacity=".96" stroke="#a18b5e"/></g>`;
const letter=`<g id="letter"><path d="M6 8L42 4L78 8V37L42 42L6 37Z" fill="#bdb28d" stroke="#5f5741" stroke-width="2"/><path d="M8 9L42 28L76 9M8 36L28 22M76 36L56 22" fill="none" stroke="#6f6447" stroke-width="2"/><rect x="36" y="22" width="12" height="12" fill="#794f43"/><path d="M40 26H44V30H40Z" fill="none" stroke="#c7b891"/></g>`;
const book=`<g id="book"><path d="M10 8Q27 3 42 9Q57 3 74 8V38Q57 33 42 40Q27 33 10 38Z" fill="#bdb28d" stroke="#71664e" stroke-width="2"/><path d="M42 9V40M17 14L35 15M17 21L35 22M17 28L35 29M49 15L67 14M49 22L67 21M49 29L67 28" fill="none" stroke="#756a51" stroke-width="1.5"/></g>`;
const svg=(w,h,body)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs}${body}</svg>`;
// Correct namespace is essential for Inkscape editable layers.
const save=(name,text)=>{const out=path.join(dir,name);fs.mkdirSync(path.dirname(out),{recursive:true});fs.writeFileSync(out,text);};
save('components/navigation.svg',svg(600,160,layer('frames','01 边框与状态',frame('normal')+`<g transform="translate(120)">${frame('active',true)}</g><g transform="translate(240)">${recruit}</g>`)+layer('icons','02 文书与旧书册兼容',`<g transform="translate(380 10)">${letter}</g><g transform="translate(480 10)">${book}</g>`)+layer('shelf-layer','03 导航托底',`<rect id="footer-shelf" y="82" width="600" height="78" fill="url(#shelf)"/>`)));
function navigation(w,h){
 const cy=h-74, start=20, items=[['主城','city',5],['领地','map',0],['武将','roster',1],['战报','reports',3],['帮助','help',4]];
 let frames='',images='',text='',hit='';
 const type=(x,y,t,size=17)=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei, Noto Sans SC" font-size="${size}" fill="#e0d2aa" text-anchor="middle">${t}</text>`;
 for(let i=0;i<items.length;i++) {const [label,id,kind]=items[i],x=start+i*116;
 frames+=`<g transform="translate(${x} ${cy})">${frame('preview-'+id,kind===0)}</g>`;
 if(kind===5)images+=img(id+'-image',asset('Youzhou/city'),x+34,cy+8,70,56);
 if(kind===0)images+=img(id+'-image',asset('Youzhou/mountain'),x+34,cy+8,70,56);
 if(kind===1)for(let j=0;j<3;j++)images+=img(id+'-portrait-'+j,asset('Portraits/s1_'+['100744','100039','100746'][j]),x+35+j*21,cy+8,25,56);
 if(kind===3)images+=`<g transform="translate(${x+34} ${cy+14}) scale(.83 1)">${letter.replace('id="letter"','id="preview-letter"')}</g>`;
 if(kind===4)images+=`<g transform="translate(${x+34} ${cy+14}) scale(.83 1)">${book.replace('id="book"','id="preview-book"')}</g>`;
 text+=type(x+17.5,cy+23,label[0],18)+type(x+17.5,cy+45,label[1],18);hit+=`<rect id="hit-${id}" x="${x}" y="${cy}" width="108" height="70" fill="none" stroke="#00ffff"/>`;
 }
 const rx=w-152;
 frames+=`<g transform="translate(${rx} ${cy})">${recruit.replace('id="recruit"','id="preview-recruit"')}</g>`;
 images+=img('recruit-existing-portrait',asset('Portraits/s1_100039'),rx+46,cy+7,40,38);
 text+=type(rx+66,cy+63,'招募',16);
 const ux=Math.max(620,(w-262)/2);
 frames+=`<path d="M${ux} ${h-61}H${ux+262}V${h-12}H${ux}Z" fill="#16212a" fill-opacity=".85" stroke="#797765"/><path d="M${ux+82} ${h-52}V${h-20}" stroke="#73715f"/>`;
 text+=type(ux+41,h-30,'保存',16)+type(ux+172,h-30,'时间暂停 · 继续',16);
 hit+=`<rect id="hit-recruit" x="${rx}" y="${cy}" width="132" height="70" fill="none" stroke="#00ffff"/>`;
 return layer('footer-background','02 导航托底',`<rect x="0" y="${h-78}" width="${w}" height="78" fill="url(#shelf)"/>`)+layer('button-frames','03 边框与选中状态',frames)+layer('formal-images','04 正式图片与图标',images)+layer('live-labels','05 可编辑文字-运行时独立绘制',text)+layer('hit-regions','06 点击范围-隐藏',hit,'style="display:none"');
}
for(const [name,w,h,bp] of [['desktop',1440,900,'youzhou-1440x900'],['mobile',1800,820,'mobile-1600x720']]){
 const p=baseline+`s1-20261027-104752269-${bp}/smoke-overview.png`;
 // Baseline remains locked as context only; its footer is covered. No screenshot pixels enter production slices.
 let bg=(name==='mobile'?`<svg width="${w}" height="${h}" viewBox="56 12 1504 684">${img('baseline-image',p,0,0,1600,720)}</svg>`:img('baseline-image',p,0,0,w,h))+`<rect x="0" y="${h-78}" width="${w}" height="78" fill="#1b2428"/>`;
 save(name+'/map-hud.svg',svg(w,h,layer('reference-original','00 原作参考-锁定隐藏',img('original-reference',ref,0,0,500,280),'style="display:none" sodipodi:insensitive="true"')+layer('baseline-context','01 旧运行图-仅上下文',bg,'sodipodi:insensitive="true"')+navigation(w,h)));
}
const spec={version:'s1-20261027-114202570',status:'design-source-awaiting-export-inspection',reference:{id:'STZB-0395',file:ref,scope:'2015 historical composition only; not current S1 confirmation'},baselineVersion:'s1-20261027-104752269',footerHeight:78,button:{width:108,height:70,pitch:116,yFromBottom:74,image:[12,7,84,38],caption:[8,47,92,21]},group:{width:580,anchor:'center-bottom',order:['city-open','map-tab','roster-tab','reports-tab','hud-help']},recruit:{anchor:'right-bottom',right:20,width:132,image:[46,7,40,38],caption:[8,47,116,21]},utilities:{save:[20,61,82,49],clock:[102,61,180,49]},fonts:{family:'Microsoft YaHei / Noto Sans SC',caption:17,utility:16},colors:{caption:'#e0d2aa',background:'#1b2428'},states:['normal','selected/hover active frame','modal no targets'],runtime:'MapHudPresentation.DrawYouzhouMapFooter',notes:['Only navigation region is redesigned; baseline outside footer is old screenshot context.','City/mountain and existing portraits are reused at native small display sizes.','Envelope follows visible mail symbol in STZB-0395; book retains legacy help semantics.','Recruit portrait is existing starter/pool identity, not a result preview or new recruitable identity.','No reference screenshot is included in exported production objects.']};
save('design-spec.json',JSON.stringify(spec,null,2));
Object.assign(spec.reference,{id:'STZB-0499',width:2340,height:1080,scope:'2021 anniversary interface, current S1 unknown; composition only',officialVisualRationale:'https://stzb.163.com/news/20210820/20395_967644.html'});
Object.assign(spec.button,{image:[34,8,70,56],caption:[5,4,25,50],captionFlow:'vertical two characters in 25-unit rows'});
Object.assign(spec.recruit,{caption:[8,47,116,22],skinBounds:[12.5,0,107,70]});
Object.assign(spec.fonts,{title:18,caption:16});
Object.assign(spec.group,{anchor:'left-bottom',left:20});
spec.utilities={anchor:'bottom',x:'max(620,(width-262)/2)',save:[0,13,82,49],clock:[82,13,180,49]};
spec.notes[2]='Envelope retains the 2015 documented mail motif; help book retains existing compatibility appearance. Neither is a new official UI asset.';
spec.notes.push('2021 portrait tickets adapted to 70-unit footer controls so map viewport and touch/navigation remain stable; this is not a full reconstruction.');
save('design-spec.json',JSON.stringify(spec,null,2));
