import fs from 'node:fs';
import path from 'node:path';
const root=import.meta.dirname;
const save=(p,s)=>{fs.mkdirSync(path.join(root,path.dirname(p)),{recursive:true});fs.writeFileSync(path.join(root,p),s);};
const img=p=>'data:image/'+(p.endsWith('.jpg')?'jpeg':'png')+';base64,'+fs.readFileSync(p).toString('base64');
const reference='F:/工作项目/率土之滨/rag/assets/ea/ead909e3cd3ff7f4a6b2a1c98bcada429ae47bb1a77d5801c202c2c26b10dd6a.jpg';
const desktop='F:/EasternDynastyBuild/Releases/s1-20261028-022752190/Project/Validation/Visual/s1-20261028-022752190-youzhou-1440x900/smoke-land-grain.png';
const mobile='F:/EasternDynastyBuild/Releases/s1-20261027-104752269/Project/Validation/Visual/s1-20261027-104752269-mobile-1600x720/smoke-land-grain.png';
const svg=(w,h,s)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${s}</svg>`;
const layer=(id,s,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${id}" ${extra}>${s}</g>`;
const t=(x,y,s,n=15,color='#e8e2cd')=>`<text x="${x}" y="${y}" font-family="Noto Sans CJK SC,Microsoft YaHei" font-size="${n}" fill="${color}">${s}</text>`;
const defs='<defs><linearGradient id="surface"><stop stop-color="#171c1c" stop-opacity=".97"/><stop offset="1" stop-color="#202522" stop-opacity=".96"/></linearGradient></defs>';
const skin=(x,y,w,h)=>`<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="url(#surface)"/>`;
save('components/land.svg',svg(720,336,defs+layer('land-components','<rect id="land-surface" width="720" height="336" fill="url(#surface)"/>')));
function content(state){
 const owned=state==='owned';
 const surfaces=skin(0,0,360,168)+skin(0,180,360,86)+skin(0,278,360,148);
 const art=`<image x="16" y="70" width="192" height="96" href="${img('Assets/EasternDynasty/Resources/Youzhou/grain.png')}"/>`;
 const text=t(18,36,'粮田',28)+t(18,61,'涿郡平原 / 0, 0',15,'#b8c0bd')+t(260,102,'2',32)+t(225,127,'土地等级',15,'#b8c0bd')+t(225,153,owned?'己方领地':'中立土地')+t(55,207,owned?'控制状态':'守军兵力',15,'#b8c0bd')+t(55,242,owned?'已占领':'300',27)+t(191,207,'基础产量 / 时',15,'#b8c0bd')+t(225,242,'+360',27)+t(318,40,'×',22);
 let commands='';
 if(owned){commands=t(20,300,'屯田预计收益 · 政令 20/20')+t(20,324,'木 180    粮 600    石 180    铁 180',14)+t(20,336,'作业 60 秒 · 完成后返城',12);for(const [i,s] of ['驻守','屯田·3令','部队练兵'].entries())commands+=`<rect x="${16+i*112}" y="338" width="104" height="72" fill="#243032"/>`+t(36+i*112,389,s,14);}
 else commands=t(20,301,'出征消耗   30 粮食   20 木材')+t(20,334,'需要先占领与目标相邻的土地。')+`<rect x="16" y="362" width="328" height="52" fill="#243032"/>`+t(118,396,'派兵占领',18,'#939e99');
 return layer('land-surfaces',surfaces)+layer('land-images',art)+layer('land-live-labels',text)+layer('land-state',commands)+layer('land-hit-regions','<rect width="360" height="426" fill="none" stroke="magenta"/>','style="display:none"');
}
for(const [target,w,h,base,scale,ox,oy,state] of [['desktop',1440,900,desktop,1,0,0,'neutral'],['mobile',1600,720,mobile,684/820,56,12,'neutral'],['states',1440,900,desktop,1,0,0,'owned']]){
 const logicalWidth=target==='mobile'?1504/scale:w,px=ox+(logicalWidth-380)*scale,py=oy+100*scale;
 // Locked montage context: only the old selected-land rectangle is replaced by
 // an adjacent unobstructed map crop from THIS resolution's own baseline.
 const patch=`<defs><clipPath id="context-clear"><rect x="${px}" y="${py}" width="${360*scale}" height="${506*scale}"/></clipPath></defs><g clip-path="url(#context-clear)"><image x="${400*scale}" width="${w}" height="${h}" href="${img(base)}"/></g>`;
 const background=`<image width="${w}" height="${h}" href="${img(base)}"/>`+patch;
 save(target+'/land.svg',svg(w,h,defs+layer('reference-original',`<image width="554" height="236" href="${img(reference)}"/>`,'style="display:none" sodipodi:insensitive="true"')+layer('baseline-context',background,'sodipodi:insensitive="true"')+`<g transform="translate(${px} ${py}) scale(${scale})">${content(state)}</g>`));
}
save('design-spec.json',JSON.stringify({version:'s1-20261028-074752500',status:'source-awaiting-export',reference:{id:'S1-RES-009',path:reference,scope:'2021–2022 historical S1 tutorial; current S1 unverified; composition only'},baselines:{desktop,mobile},context:'Editable montage using each resolution own baseline; patch only clears old panel. Not a new runtime screenshot.',layout:{panel:[360,426],header:[0,0,360,168],art:[12,68,336,100],image:[16,70,192,96],identity:[220,70,124,96],summary:[16,180,328,86],actions:[16,278,328,136],surfaceActions:[0,278,360,148],surfaceGap:12},components:['land_surface'],runtime:'LandSelectionPresentation.cs / LandSelectionLayout.cs',remaining:['Official current S1 land selection art unavailable; existing project tiles reused without enlarging historical screenshots.','Existing command icons and touch sizes retained; geometry diagram fallback for water/bridge retained.'],states:['neutral','owned','busy','city','blocked'],mobile:{width:1600,height:720,safeArea:[56,12,1504,684],scale:684/820}},null,2));
