// Editable vector draft. PNGs must come from the verified Inkscape exporter.
import fs from 'node:fs';
import path from 'node:path';
const version='s1-20261027-201251989',root=path.resolve('Docs/DesignUI',version);
const save=(p,s)=>{fs.mkdirSync(path.dirname(path.join(root,p)),{recursive:true});fs.writeFileSync(path.join(root,p),s);};
const data=p=>'data:image/'+(p.endsWith('.jpg')?'jpeg':'png')+';base64,'+fs.readFileSync(p).toString('base64');
const layer=(id,title,body,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${title}" ${extra}>${body}</g>`;
const text=(x,y,t,size=16,color='#eee6cb',extra='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}" ${extra}>${t}</text>`;
const defs='<linearGradient id="ink" x1="0" y1="0" x2="1" y2="0"><stop stop-color="#18251f" stop-opacity=".94"/><stop offset="1" stop-color="#18251f" stop-opacity=".45"/></linearGradient><linearGradient id="bronze" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#d6bf83"/><stop offset=".55" stop-color="#897348"/><stop offset="1" stop-color="#c5af75"/></linearGradient>';
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}</defs>${b}</svg>`;
function identity(hover=false){return `<rect width="252" height="62" fill="#000" fill-opacity="0"/><path d="M32 4H252L246 57H32Z" fill="url(#ink)"/><path d="M55 5H251M55 57H246" stroke="${hover?'#efdfa2':'#a28e5d'}" stroke-opacity=".65"/><circle cx="36" cy="31" r="30.5" fill="url(#bronze)"/><circle cx="36" cy="31" r="28.5" fill="#25372c"/><circle cx="36" cy="31" r="26" fill="none" stroke="${hover?'#efdfa2':'#81754e'}"/>`;}
function strip(){return '<path d="M0 0H184L177 58H0Z" fill="url(#ink)"/><path d="M1 .5H183L181 13" fill="none" stroke="#a28e5d" stroke-opacity=".65"/>';}
save('components/city-header.svg',svg(1440,140,layer('components','独立身份牌/悬停/生产资源条-2倍矢量',`<g id="city-identity" transform="scale(2)">${identity()}</g><g id="city-identity-hover" transform="translate(520 0) scale(2)">${identity(true)}</g><g id="city-resource" transform="translate(1040 0) scale(2)">${strip()}</g>`)));
const original='F:/工作项目/率土之滨/rag/assets/d8/d8a56f2b7e2504fa7319cfa8f022d5cb0ca0168e3a7ac013f6883da1f848a350.jpg';
const resourceRef='Assets/EasternDynasty/Resources/YouzhouReferences/resource_hud.png';
let clipIndex=0;
function crop(p,x,y,w,h,r,iw,ih){const [sx,sy,sw,sh]=r,s=Math.min(w/sw,h/sh),id='clip-'+(++clipIndex),dx=x+(w-sw*s)/2,dy=y+(h-sh*s)/2;return `<defs><clipPath id="${id}"><rect x="${dx}" y="${dy}" width="${sw*s}" height="${sh*s}"/></clipPath></defs><g clip-path="url(#${id})"><image x="${dx-sx*s}" y="${dy-sy*s}" width="${iw*s}" height="${ih*s}" href="${data(p)}"/></g>`;}
// Same crops as the existing runtime icons; only preview, never component pixels.
function icon(i,x,y){
 return crop(resourceRef,x,y,26,24,[[202,9,38,30],[722,9,41,31],[548,9,41,31],[374,9,38,31]][i],894,74);
}
function header(w,long=false){
 let frames=`<g transform="translate(16 4)">${identity()}</g>`,images=`<image x="16" y="5" width="72" height="60" href="${data('Validation/CityArt/TownHall.png')}"/>`,labels='',hits='<rect x="16" y="4" width="252" height="62"/>';
 labels+=long?text(96,22,'幽州城内全景界面验收用',14)+text(96,40,'长名称主城',14):text(96,30,'新城',21);
 labels+=text(97,58,'幽州 · 府邸 3 级',13,'#c8c7b7');
 const names=['木材','粮食','石料','铁矿'],stocks=long?['999,999','9999.9万','9999.99亿','999,999']:['48,563','48,414','47,583','49,440'];
 const pitch=Math.min(760,w-604)/4;
 for(let i=0;i<4;i++){
  const x=280+i*pitch,sw=pitch-6;
  frames+=`<g transform="translate(${x} 5) scale(${sw/184} 1)">${strip()}</g>`;
  images+=icon(i,x+7,10);
  labels+=text(x+40,24,names[i]+' +'+[380,520,300,170][i]+'/时',13,'#d4c797')+text(x+40,51,stocks[i],20);
  hits+=`<rect x="${x}" y="5" width="${sw}" height="58"/>`;
 }
 for(let i=0;i<2;i++){
  const x=w-316+i*150;
  frames+=`<image x="${x}" y="5" width="144" height="58" href="${data('Assets/EasternDynasty/Resources/YouzhouUI/header_currency.png')}"/>`;
  images+=crop('Docs/DesignUI/s1-20261027-190751837/baseline/desktop-104752269.png',x+7,17,26,28,i?[1250,18,34,34]:[1060,20,30,30],1440,900);
  labels+=text(x+40,28,i?'300':'49,362',20)+text(x+40,51,i?'试玩灵玉':'金币 +220/时',13,'#c8c7b7');
  hits+=`<rect x="${x}" y="5" width="144" height="58"/>`;
 }
 return layer('header-surfaces','身份与生产/货币分组; 无贯通黑底',frames)+layer('header-images','既有建筑与图标-非新原画',images)+layer('header-live-labels','库存/时产/完整城名-运行文本示例',labels)+layer('header-state','悬停由独立组件; 府邸等级取真实状态','')+layer('header-hit-regions','父点击和只读库存-隐藏',hits,'style="display:none" fill="none" stroke="#ff00ff"');
}
for(const [target,w,h,x,y,s,lw] of [['desktop',1440,900,0,0,1,1440],['mobile',1600,720,56,12,.835,1800]]){
 const context=`Docs/DesignUI/s1-20261027-190751837/${target}/exports/city-dock-preview.png`;
 const bg=layer('reference-original','2015城内同页-仅测量不导入',`<image width="500" height="280" href="${data(original)}"/>`,'style="display:none" sodipodi:insensitive="true"')+layer('baseline-context','104752269运行基线+前三轮设计; 非新截图',`<image width="${w}" height="${h}" href="${data(context)}"/><rect x="${x}" y="${y}" width="${lw*s}" height="${70*s}" fill="#111e29"/>`,'sodipodi:insensitive="true"');
 save(`${target}/city-header.svg`,svg(w,h,bg+`<g transform="translate(${x} ${y}) scale(${s})">${header(lw)}</g>`));
}
save('stress/city-header.svg',svg(1440,180,'<rect width="1440" height="180" fill="#111e29"/>'+header(1440,true)+text(20,115,'完整长城名两行 / 大库存 / 四类生产资源与两类货币分组',20)+text(20,150,'原图仅支持布局方向；建筑和小图标沿用现有资产，运行字体与数字由发布者验收。',16)));
save('design-spec.json',JSON.stringify({version,status:'editable-awaiting-export',reference:{id:'STZB-0428',path:original,sha256:'d8a56f2b7e2504fa7319cfa8f022d5cb0ca0168e3a7ac013f6883da1f848a350',size:[500,280],year:2015,scope:'Historical city header compact production strip and round identity only; current S1 appearance unconfirmed; no original pixels imported.'},baselineVersion:'s1-20261027-104752269',layout:{identity:[16,4,252,62],art:[16,5,72,60],name:[96,7,166,32],wrappedName:[96,5,166,38],detail:[97,44,165,20],resources:{x:280,y:5,height:58,pitch:'min(760,width-604)/4',gap:6},currency:{x:'width-316+index*150',y:5,width:144,height:58}},components:['city_header_identity','city_header_identity_hover','city_header_resource'],reuse:['ui.youzhou.header_currency'],fonts:{name:21,wrappedName:14,stock:20,rate:13,minimum:12},notes:['No city scene geometry or camera changes; transparency reveals existing page backing, not a newly extended city scene.','Preview icon surrogates use existing formal map tiles; runtime retains the existing small resource and currency icons.','Dynamic text is not baked in components.','Full-screen contexts include previous inspected designs and old runtime screenshots; not new gameplay evidence.']},null,2));
console.log('City header layered SVG draft ready for Inkscape.');
