// Editable chrome; reference and old runtime pixels remain locked research context.
// PNG components are produced only by the real Inkscape export entry point.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261028-214751426',dir=`Docs/DesignUI/${version}`;
const save=(p,s)=>{fs.mkdirSync(path.dirname(`${dir}/${p}`),{recursive:true});fs.writeFileSync(`${dir}/${p}`,s);};
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const data=p=>'data:image/png;base64,'+fs.readFileSync(p).toString('base64');
const image=(p,x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" href="${data(p)}"/>`;
const layer=(id,b,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${id}" ${extra}>${b}</g>`;
const text=(x,y,t,size=18,color='#e6e0ce',extra='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}" ${extra}>${t}</text>`;
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${b}</svg>`;
const ref='F:/工作项目/率土之滨/rag/assets/e5/e5abedde334d978717e308c869709386316d462218423043087044a4c13134db.png';
const released='F:/EasternDynastyBuild/Releases/s1-20261028-204251299/Project/Validation/Visual/';
function freeze(name,p){const dest=`${dir}/baseline/${name}.png`;fs.mkdirSync(path.dirname(dest),{recursive:true});if(!fs.existsSync(dest))fs.copyFileSync(p,dest);return {path:dest,sourcePath:p,sha256:sha(dest)};}
const baselines=['desktop','mobile'].map((s,i)=>({...freeze(s,released+`s1-20261028-204251299-${i?'mobile-1600x720':'youzhou-1440x900'}/smoke-card-dossier-design.png`),size:i?[1600,720]:[1440,900]}));
const defs='<defs><linearGradient id="selected-glow" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#992b27" stop-opacity="0"/><stop offset="1" stop-color="#992b27" stop-opacity=".66"/></linearGradient></defs>';
const rect=(x,y,w,h,fill,extra='')=>`<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="${fill}" ${extra}/>`;
const skins={
 card_tabs:rect(0,0,600,54,'#191c1c')+'<path d="M200 17V37M400 17V37" stroke="#66635c"/>',
 card_tab_active:rect(0,0,200,54,'url(#selected-glow)')+rect(12,52,176,2,'#b84336'),
 card_identity:rect(0,0,600,40,'#171d1d')+'<path d="M200 9V31M400 9V31" stroke="#625e53"/>',
 card_close:'<rect width="54" height="54" fill="#000" fill-opacity="0"/><path d="M15 13L27 24L39 13L43 17L31 28L43 39L39 43L27 32L15 43L11 39L23 28L11 17Z" fill="#a84139"/>',
 card_action:rect(0,0,240,54,'#262a29')+rect(1,1,238,52,'none','stroke="#aaa391" stroke-width="1.5"')+rect(4,4,232,46,'none','stroke="#444b47"'),
 card_action_hover:rect(0,0,240,54,'#3d4138')+rect(1,1,238,52,'none','stroke="#d3caa6" stroke-width="1.5"')+rect(4,4,232,46,'none','stroke="#706e5a"')
};
const sizes={card_tabs:[600,54],card_tab_active:[200,54],card_identity:[600,40],card_close:[54,54],card_action:[240,54],card_action_hover:[240,54]};
save('components/chrome.svg',svg(1280,440,defs+layer('chrome-components',Object.entries(skins).map(([n,s],i)=>`<g id="${n.replaceAll('_','-')}" transform="translate(0 ${i*72})">${s}</g>`).join(''))));
function skin(n,x,y,w,h){const [sw,sh]=sizes[n];return `<g transform="translate(${x} ${y}) scale(${w/sw} ${h/sh})">${skins[n]}</g>`;}
function chrome(prefix,x,y,w,h,{selected=0,owned=false,hover=false,disabled=false}={}){
 const nw=w-66,tw=nw/3;
 let surfaces=skin('card_tabs',x,y+24,nw,54)+skin('card_tab_active',x+selected*tw,y+24,tw,54)+skin('card_identity',x,y+90,w,40);
 surfaces+=skin(hover?'card_action_hover':'card_action',x+w-180,y+h-62,180,54);
 if(owned)surfaces+=skin('card_action',x,y+h-62,240,54);
 let images=skin('card_close',x+w-54,y+24,54,54);
 images+=image('Assets/EasternDynasty/Resources/YouzhouUI/dossier_attack.png',x+8,y+96,28,28);
 let labels=['属性','战法','资料'].map((t,i)=>text(x+(i+.5)*tw,y+59,t,22,i===selected?'#eee6d7':'#b9b3a5','text-anchor="middle"')).join('');
 labels+=text(x+44,y+117,'汉 · 弓兵',18)+text(x+w/3+12,y+117,'攻击距离  3',18)+text(x+w*2/3+18,y+117,'COST  3.0',18);
 labels+=text(x+w-90,y+h-28,'返回卡牌',18,'#e6e0ce','text-anchor="middle"');
 labels+=owned?text(x+120,y+h-28,'编队与培养',18,'#e6e0ce','text-anchor="middle"'):text(x,y+h-30,'图鉴预览 · 基础属性',16,'#b9b3a5');
 const hits=Array.from({length:3},(_,i)=>rect(x+i*tw,y+24,tw,54,'none')).join('')+rect(x+w-54,y+24,54,54,'none')+rect(x+w-180,y+h-62,180,54,'none')+(owned?rect(x,y+h-62,240,54,'none'):'');
 return `<g ${disabled?'opacity=".55"':''}>`+layer(prefix+'chrome-surfaces',surfaces)+layer(prefix+'chrome-images',images)+layer(prefix+'chrome-live-labels',labels)+layer(prefix+'chrome-state',`<desc>selected=${selected}; owned=${owned}; hover=${hover}; disabled=${disabled}; labels remain runtime text</desc>`)+layer(prefix+'chrome-hit-regions',hits,'style="display:none" stroke="#ff00ff"')+'</g>';
}
for(let i=0;i<2;i++){
 const target=i?'mobile':'desktop',b=baselines[i],[sw,sh]=b.size,s=i?684/820:1,ox=i?56:0,oy=i?12:0,lw=i?1504/s:1440,lh=i?820:900;
 const mw=Math.min(1280,lw-64),mh=Math.min(820,lh-28),px=(lw-mw)/2,py=(lh-mh)/2,x=px+20+mw*.43+28,w=px+mw-x-24;
 const bodyTop=oy+(py+198)*s,bodyH=(mh-282)*s,newTop=oy+(py+142)*s;
 // Body is unchanged old runtime context; only chrome is this editable design.
 const body=`<defs><clipPath id="unchanged-body-clip">${rect(ox+x*s,newTop,w*s,bodyH,'#fff')}</clipPath></defs><g clip-path="url(#unchanged-body-clip)">${image(b.path,0,-56*s,sw,sh)}</g>`;
 const wipe=rect(ox+x*s,oy+(py+1)*s,(w+8)*s,(mh-2)*s,'#202323');
 save(`${target}/chrome.svg`,svg(sw,sh,defs+layer('reference',image(ref,0,0,1793,1008),'style="display:none" sodipodi:insensitive="true"')+layer('baseline',image(b.path,0,0,sw,sh),'sodipodi:insensitive="true"')+layer('chrome-region-clear',wipe)+layer('unchanged-body-context',body,'sodipodi:insensitive="true"')+`<g transform="translate(${ox} ${oy}) scale(${s})">${chrome('',x,py,w,mh)}</g>`));
}
save('states/chrome.svg',svg(1400,440,defs+rect(0,0,1400,440,'#202323')+chrome('preview-',14,0,650,208)+chrome('owned-',724,0,650,208,{owned:true,selected:1,hover:true})+chrome('disabled-',14,224,650,208,{selected:2,disabled:true})+text(724,285,'状态稿仅展示上下固定区；正文另见桌面/手机稿',18)));
save('design-spec.json',JSON.stringify({version,status:'draft-awaiting-export-inspection',reference:{id:'STZB-0467',path:ref,sha256:sha(ref),size:[1793,1008],year:2022,scope:'Historical official detail reference; current S1 styling unknown. Three tabs and two footer actions are project adaptations.'},baselines,layout:{right:'existing info x, panel.y, existing info width, panel.height',navigation:[0,24,'width-66',54],tabs:'three equal adjacent segments',close:['width-54',24,54,54],identity:[0,90,'width',40],viewport:[0,142,'width','height-226'],return:['width-180','height-62',180,54],manage:[0,'height-62','min(240,width-196)',54],footerNote:[0,'height-58','width-196',44],viewportGain:56},components:Object.keys(skins).map(x=>'ui.youzhou.'+x),notes:['Preserve all existing card sections, scrolling, full portrait, owned UID and return commands.','No duplicate name/heading on the right: name remains in the portrait ribbon.','Preview/archive scope moves to the fixed footer; owned scope remains in the existing portrait base.','Historical image pixels are never used in exported production components.','Actual body preview is labeled locked baseline context, shifted up 56 logical pixels; not a runtime screenshot.','No new generated portrait, inferred skill image, stats, locked slots, or S1 identities.']},null,2));
console.log('Editable card chrome design written; actual Inkscape export and inspection still required.');
