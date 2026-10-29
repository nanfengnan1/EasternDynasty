import fs from 'node:fs';
import path from 'node:path';
const root=import.meta.dirname;
const save=(p,s)=>{fs.mkdirSync(path.join(root,path.dirname(p)),{recursive:true});fs.writeFileSync(path.join(root,p),s);};
const img=p=>'data:image/png;base64,'+fs.readFileSync(p).toString('base64');
const original='F:/工作项目/率土之滨/rag/assets/ea/ead909e3cd3ff7f4a6b2a1c98bcada429ae47bb1a77d5801c202c2c26b10dd6a.jpg';
const base='F:/EasternDynastyBuild/Releases/s1-20261028-022752190/Project/Validation/Visual/s1-20261028-022752190-youzhou-1440x900/smoke-land-grain.png';
const svg=(w,h,s)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${s}</svg>`;
const layer=(id,s,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${id}" ${extra}>${s}</g>`;
const text=(x,y,t,size=15)=>`<text x="${x}" y="${y}" font-family="Noto Sans CJK SC,Microsoft YaHei" font-size="${size}" fill="#eee7d4">${t}</text>`;
const defs='<defs><linearGradient id="land-fade"><stop stop-color="#171c1c" stop-opacity=".96"/><stop offset="1" stop-color="#171c1c" stop-opacity=".90"/></linearGradient></defs>';
const strip=(x,y,w,h)=>`<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="url(#land-fade)"/>`;
save('components/land.svg',svg(720,180,defs+layer('land-components','<g transform="scale(2)"><rect id="land-strip" width="360" height="90" fill="url(#land-fade)"/></g>')));
for(const [target,w,h] of [['desktop',1440,900],['mobile',1800,820]]){
 const x=w-380,y=100;
 const background=`<image width="${w}" height="${h}" href="${img(base)}"/>`;
 const patch=`<defs><clipPath id="clear-old-panel"><rect x="${x}" y="${y}" width="360" height="506"/></clipPath></defs><g clip-path="url(#clear-old-panel)"><image x="${x-550}" y="${y-80}" width="1440" height="900" href="${img(base)}"/></g>`;
 const skins=strip(x,y,360,70)+strip(x+256,y+81,88,160)+strip(x,y+260,360,86)+strip(x,y+358,360,148);
 const art=`<image x="${x+16}" y="${y+77}" width="232" height="168" href="${img('Assets/EasternDynasty/Resources/Youzhou/grain.png')}"/>`;
 const labels=text(x+18,y+36,'粮田',28)+text(x+18,y+62,'涿郡平原 / 0, 0')+text(x+290,y+126,'2',35)+text(x+270,y+185,'土地等级')+text(x+270,y+221,'中立土地')+text(x+38,y+290,'守军兵力')+text(x+38,y+324,'300',27)+text(x+191,y+290,'基础产量 / 时')+text(x+211,y+324,'+360',27)+text(x+20,y+382,'出征消耗   30 粮食   20 木材')+text(x+20,y+416,'需要先占领与目标相邻的土地。')+text(x+112,y+477,'派兵占领',18)+text(x+319,y+42,'×',22);
 const ux=Math.max(620,(w-270)/2),uy=h-61;
 const footer=strip(ux,uy,82,49)+strip(ux+90,uy,180,49)+text(ux+20,uy+31,'保存',16)+text(ux+109,uy+31,'时间暂停 · 继续',16);
 save(target+'/land.svg',svg(w,h,defs+layer('reference-original',`<image width="554" height="236" href="${img(original)}"/>`,'style="display:none" sodipodi:insensitive="true"')+layer('baseline-context',background+patch,'sodipodi:insensitive="true"')+layer('land-surfaces',skins)+layer('land-images',art)+layer('land-live-labels',labels)+layer('land-state','<desc>Neutral land example. Commands remain original; other terrain uses registered assets. Background is baseline composition context, not a new runtime image.</desc>')+layer('footer-spacing',footer)+layer('land-hit-regions',`<rect x="${x}" y="${y}" width="360" height="506" fill="none" stroke="magenta"/>`,'style="display:none"')));
}
save('design-spec.json',JSON.stringify({version:'s1-20261028-064751954',reference:original,baseline:base,referenceScope:'Historical S1 tutorial; current applicability unknown; measurement only',status:'source-awaiting-export',layout:{panel:[360,506],title:[0,0,360,70],image:[16,77,232,168],identity:[256,81,88,160],facts:[0,260,360,86],actions:[0,358,360,148],utilityGap:8},components:['ui.youzhou.land_strip'],notes:['Separate floating terrain image, no invented circle/diamond ornament.','Existing full terrain image remains uncropped; existing action geometry and rules retained.','Mobile canvas is a logical safe-area design, not an Android runtime capture.']},null,2));
