// Editable vector composition. PNGs are exported only by the real Inkscape CLI.
import fs from 'node:fs';
import path from 'node:path';
const version='s1-20261027-180252216',root=path.resolve('Docs/DesignUI',version);
const save=(p,s)=>{fs.mkdirSync(path.dirname(path.join(root,p)),{recursive:true});fs.writeFileSync(path.join(root,p),s);};
const data=p=>'data:image/'+(p.endsWith('.jpg')?'jpeg':'png')+';base64,'+fs.readFileSync(p).toString('base64');
const layer=(id,title,b,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${title}" ${extra}>${b}</g>`;
const text=(x,y,t,size=14,color='#eee6cb',extra='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}" ${extra}>${t}</text>`;
const defs='<linearGradient id="bronze"><stop stop-color="#b8a16c"/><stop offset=".5" stop-color="#e1cb8e"/><stop offset="1" stop-color="#897449"/></linearGradient><linearGradient id="ink" x2="0" y2="1"><stop stop-color="#354034"/><stop offset="1" stop-color="#111d1a"/></linearGradient><linearGradient id="fade"><stop stop-color="#1c2923" stop-opacity=".96"/><stop offset=".8" stop-color="#1c2923" stop-opacity=".88"/><stop offset="1" stop-color="#1c2923" stop-opacity=".1"/></linearGradient>';
const seal='<g id="affairs-seal"><circle cx="56" cy="56" r="56" fill="url(#bronze)"/><circle cx="56" cy="56" r="54.5" fill="url(#ink)"/><circle cx="56" cy="56" r="51" fill="none" stroke="#958357" stroke-width=".7"/><path d="M11 78Q56 89 101 78L95 95Q56 119 17 95Z" fill="#18251f"/><path d="M11 78Q56 89 101 78" fill="none" stroke="#b8a16c"/></g>';
const sealHover=seal.replaceAll('affairs-seal','affairs-seal-hover').replace('#958357','#eadca5').replace('url(#bronze)','#efdfa2');
const ribbon='<g id="affairs-ribbon"><path d="M0 0H248L240 108H0Z" fill="url(#fade)"/><path d="M0 0H236M0 108H208" fill="none" stroke="#9b8859" stroke-opacity=".65"/></g>';
const quest='<g id="affairs-quest"><path d="M0 0H378L390 22L378 44H0Z" fill="#24312a"/><path d="M0 .5H378L389 22L378 43.5H0" fill="none" stroke="#9b8859"/></g>';
const questHover=quest.replaceAll('affairs-quest','affairs-quest-hover').replace('#24312a','#424c3b');
const command='<path d="M0 0H120L126 6V44H6L0 38Z" fill="url(#command-bronze)"/><path d="M1 1H119L125 7V43H7L1 37Z" fill="#24312a"/>';
const commandDefs='<linearGradient id="command-bronze" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#d0b573"/><stop offset=".5" stop-color="#766444"/><stop offset="1" stop-color="#b8a16c"/></linearGradient>';
let artwork='';
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}${artwork?commandDefs:''}${artwork}</defs>${b}</svg>`;
save('components/city-affairs.svg',svg(820,250,layer('components','建筑圆章 / 悬停 / 施工信息带 / 任务正常与悬停',seal+`<g transform="translate(130)">${sealHover}</g><g transform="translate(260 1)">${ribbon}</g><g transform="translate(0 154)">${quest}</g><g transform="translate(410 154)">${questHover}</g>`)));
const base='F:/EasternDynastyBuild/Releases/s1-20261027-104752269/Project/Validation/Visual/s1-20261027-104752269-youzhou-1440x900/smoke-city-developed.png';
artwork=`<image id="existing-art" width="1440" height="900" href="${data(base)}"/>`;
function crop(id,x,y,w,h,sx,sy){return `<defs><clipPath id="${id}"><rect x="${x}" y="${y}" width="${w}" height="${h}"/></clipPath></defs><g clip-path="url(#${id})"><use href="#existing-art" transform="translate(${x-sx} ${y-sy})"/></g>`;}
function affairs(w,state='idle'){
 const working=state==='working',ready=state==='reward',hover=state==='hover';
 let surfaces=`<g transform="translate(8)">${hover?sealHover:seal}</g><g transform="translate(132 2) scale(${(w-140)/248} 1)">${ribbon}</g>`;
 const bw=(w-24)/2;
 for(let i=0;i<2;i++)surfaces+=`<g transform="translate(${8+i*(bw+8)} 116) scale(${bw/126} 1)">${command}</g>`;
 surfaces+=`<g transform="translate(8 168) scale(${(w-16)/390} 1)">${hover?questHover:quest}</g>`;
 // Existing game artwork is a locked context crop only, not an imported production image.
 let images=`<image x="12" y="4" width="104" height="78" href="${data(working?'Validation/CityArt/Mill.png':'Validation/CityArt/TownHall.png')}"/>`+crop('document-context',12,172,36,36,1028,252);
 let labels=text(64,101,working?'营建':'府治',16,'#e1cc8e','text-anchor="middle"')+text(142,23,'城 务  /  '+(working?'营 建':'府 治'),13,'#b5bbaa')+text(142,51,working?'磨坊 → 2级':'城主府  Lv.3',18)+text(142,76,working?'剩余 02:38':'工匠待命 · 可兴建升级',13,working?'#e1cc8e':'#b5bbaa')+text(142,103,working?'已建 9  /  空地 0':'已建 9  /  空地 0',13,'#b5bbaa');
 labels+=text(8+bw/2,144,working?'管理施工':'设施一览',16,working?'#e1cc8e':'#eee6cb','text-anchor="middle"')+text(16+bw+bw/2,144,state==='complete'?'查看新完工':'建设进度',16,'#eee6cb','text-anchor="middle"');
 labels+=text(57,187,'立足幽州',14)+text(57,205,ready?'主要事宜 · 可领取奖励':'主要事宜  0 / 1',12,ready?'#e1cc8e':'#b5bbaa')+text(w-22,196,'›',20,'#e1cc8e','text-anchor="middle"');
 let status=`<rect x="142" y="83" width="${w-160}" height="3" fill="#46534a"/>`+(working?`<rect x="142" y="83" width="${(w-160)*.42}" height="3" fill="#d4ba78"/>`:'');
 let hits=`<rect x="8" y="0" width="112" height="112"/><rect x="8" y="116" width="${bw}" height="44"/><rect x="${16+bw}" y="116" width="${bw}" height="44"/><rect x="8" y="168" width="${w-16}" height="44"/>`;
 return layer('affairs-surfaces','独立圆章、信息带与命令组件',surfaces)+layer('affairs-images','现有建筑与文书图像-仅基线上下文',images,'sodipodi:insensitive="true"')+layer('affairs-live-labels','动态文字示例-不烘焙入资源',labels)+layer('affairs-state','真实施工进度示例',status)+layer('affairs-hit-regions','输入范围-隐藏',hits,'style="display:none" fill="none" stroke="#ff00ff"');
}
const original='F:/工作项目/率土之滨/rag/assets/d8/d8a56f2b7e2504fa7319cfa8f022d5cb0ca0168e3a7ac013f6883da1f848a350.jpg';
for(const [target,w,h,rail,x,y,s] of [['desktop',1440,900,406,1010,82,1],['mobile',1600,720,342,1255.16,80.47,.835]]){
 const context=`Docs/DesignUI/s1-20261027-170251762/${target}/exports/city-army-preview.png`;
 const bg=layer('reference-original','2015城内原图-锁定隐藏',`<image width="500" height="280" href="${data(original)}"/>`,'style="display:none" sodipodi:insensitive="true"')+layer('baseline-context','104752269运行基线与上轮已接入部队设计-非新运行截图',`<image width="${w}" height="${h}" href="${data(context)}"/><rect x="${x}" y="${y}" width="${rail*s}" height="${218*s}" fill="#111e29"/>`,'sodipodi:insensitive="true"');
 save(`${target}/city-affairs.svg`,svg(w,h,bg+`<g transform="translate(${x} ${y}) scale(${s})">${affairs(rail)}</g>`));
}
save('states/city-affairs.svg',svg(1420,270,'<rect width="1420" height="270" fill="#111e29"/>'+['idle','working','reward','hover'].map((s,i)=>`<g transform="translate(${i*352+6} 35)">${text(8,-12,['待命','施工中','任务可领','悬停'][i],13)}${affairs(342,s).replaceAll('id="','id="'+s+'-').replaceAll('url(#building-context)','url(#'+s+'-building-context)').replaceAll('url(#document-context)','url(#'+s+'-document-context)')}</g>`).join('')));
save('design-spec.json',JSON.stringify({version,status:'editable-source-awaiting-export-review',reference:{id:'STZB-0428',year:2015,width:500,height:280,sha256:'d8a56f2b7e2504fa7319cfa8f022d5cb0ca0168e3a7ac013f6883da1f848a350',path:original,scope:'Historical city page; current S1 applicability unknown. Round facility seal with lower caption and dark/gold independent controls. No historical pixels enter production assets.'},baselineVersion:'s1-20261027-104752269',layout:{height:218,seal:[8,0,112,112],building:[12,4,104,78],sealCaption:[20,82,88,26],summary:[132,2,'width-140',108],commandsY:116,touchHeight:44,quest:[8,168,'width-16',44],questArt:[12,172,36,36]},components:['city_affairs_seal','city_affairs_seal_hover','city_affairs_ribbon','city_affairs_quest','city_affairs_quest_hover'],reuse:['ui.youzhou.city_army_command','ui.youzhou.city_army_command_hover','existing facility and document artwork'],fonts:{heading:18,label:16,summary:13,quest:14,questState:12},runtime:'CityAffairsDesignPresentation / CityAffairsLayout',notes:['Entire 218-high backing removed; separated round facility seal, tapered live summary and chapter ribbon.','Existing actions and city/army geometry retained. No rules or save fields changed.','Context uses old game pixels and prior army design; this is not a new game screenshot. Existing TownHall/Mill pure-raster artifacts illustrate the unchanged runtime images; source geometry is not modified.']},null,2));
console.log('City affairs editable sources ready for Inkscape.');
