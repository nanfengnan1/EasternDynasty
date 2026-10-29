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
const defs='<defs><linearGradient id="surface"><stop stop-color="#171c1c" stop-opacity=".97"/><stop offset="1" stop-color="#202522" stop-opacity=".96"/></linearGradient><linearGradient id="command"><stop stop-color="#35423d"/><stop offset="1" stop-color="#243032"/></linearGradient><linearGradient id="active"><stop stop-color="#79513d"/><stop offset="1" stop-color="#513c31"/></linearGradient></defs>';
const skin=(x,y,w,h)=>`<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="url(#surface)"/>`;
save('components/land.svg',svg(720,540,defs+layer('land-components','<rect id="land-surface" width="720" height="336" fill="url(#surface)"/><rect id="land-command" y="348" width="656" height="88" fill="url(#command)"/><rect id="land-command-active" y="448" width="656" height="88" fill="url(#active)"/>')));
// Traces of the existing Runtime symbols for layout review only. These are not
// new formal icon artwork and are not exported as production components.
function icon(symbol,x,y){
 const lines=symbol==='guard'?[[.16,.2,.5,.08],[.5,.08,.84,.2],[.16,.2,.22,.65],[.84,.2,.78,.65],[.22,.65,.5,.92],[.78,.65,.5,.92],[.5,.2,.5,.76]]:symbol==='train'?[[.21,.12,.21,.92],[.21,.12,.84,.2],[.84,.2,.7,.53],[.7,.53,.21,.47],[.38,.75,.87,.75],[.48,.9,.78,.9]]:symbol==='recall'?[[.82,.23,.28,.23],[.28,.23,.08,.45],[.08,.45,.28,.66],[.28,.45,.8,.45],[.8,.45,.8,.9]]:[[.18,.12,.82,.88],[.82,.12,.18,.88],[.12,.68,.4,.9],[.6,.9,.88,.68],[.18,.12,.18,.32],[.82,.12,.82,.32]];
 if(symbol==='farm'||symbol==='city')return `<image x="${x}" y="${y}" width="34" height="34" href="${img('Assets/EasternDynasty/Resources/Youzhou/'+(symbol==='farm'?'grain':'city')+'.png')}"/>`;
 return `<g transform="translate(${x} ${y})" stroke="#d8bc78" stroke-width="2" fill="none">${lines.map(l=>`<path d="M${l[0]*34},${l[1]*34} L${l[2]*34},${l[3]*34}"/>`).join('')}</g>`;
}
function action(x,y,w,label,symbol,active=false,enabled=true){
 const inset=w<=104?4:w<=160?12:24, tx=inset+40;
 return `<g opacity="${enabled?1:.65}"><rect x="${x}" y="${y}" width="${w}" height="44" fill="url(#${active?'active':'command'})"/>`+icon(symbol,x+inset,y+5)+`<text x="${x+tx+(w-tx-4)/2}" y="${y+28}" text-anchor="middle" font-family="Microsoft YaHei,Noto Sans CJK SC" font-size="14" fill="#e8e2cd">${label}</text></g>`;
}
function content(state){
 const owned=state==='owned'||state==='busy',city=state==='city',blocked=state==='blocked';
 const surfaces=skin(0,0,360,168)+skin(0,180,360,86)+skin(0,278,360,140);
 const art=`<image x="16" y="70" width="192" height="96" href="${img('Assets/EasternDynasty/Resources/Youzhou/'+(city?'city':blocked?'mountain':'grain')+'.png')}"/>`;
 const text=t(18,36,city?'主城':blocked?'山地':'粮田',28)+t(18,61,'涿郡平原 / 0, 0',15,'#b8c0bd')+t(260,102,city?'1':'2',32)+t(225,127,city?'主城辖区':blocked?'山河地形':'土地等级',15,'#b8c0bd')+t(225,153,owned||city?'己方领地':blocked?'不可通行':'中立土地')+t(55,207,owned||city?'控制状态':'守军兵力',15,'#b8c0bd')+t(55,242,owned||city?'已占领':blocked?'—':'300',27)+t(191,207,city?'城内设施':blocked?'地形作用':'基础产量 / 时',15,'#b8c0bd')+t(225,242,city?'九格城区':blocked?'不可通行':'+360',city||blocked?18:27)+t(318,40,'×',22);
 let commands='';
 if(state==='owned')commands=t(20,300,'屯田预计收益 · 政令 20/20')+t(20,321,'木 180    粮 600    石 180    铁 180',14)+t(20,336,'作业 60 秒 · 完成后返城',12)+action(16,354,104,'驻守','guard')+action(128,354,104,'屯田·3令','farm',true)+action(240,354,104,'部队练兵','train');
 else if(state==='busy')commands=t(20,301,'行军中',18)+t(20,329,'本阶段剩余 16 秒')+action(16,354,160,'撤回部队','recall')+action(184,354,160,'部队练兵','train');
 else if(state==='city')commands=t(20,304,'主城全景与设施建设')+t(20,330,'部队、练兵和城建任务')+action(16,354,328,'进入主城','city',true);
 else commands=t(20,301,'出征消耗   30 粮食   20 木材')+t(20,334,state==='blocked'?'这块地形不可通行。':'需要先占领与目标相邻的土地。')+action(16,362,328,state==='blocked'?'不可通行':'派兵占领','attack',false,false);
 return layer('land-surfaces',surfaces)+layer('land-images',art)+layer('land-live-labels',text)+layer('land-state',commands)+layer('land-hit-regions','<rect width="360" height="418" fill="none" stroke="magenta"/>','style="display:none"');
}
for(const [target,w,h,base,scale,ox,oy,state] of [['desktop',1440,900,desktop,1,0,0,'owned'],['mobile',1600,720,mobile,684/820,56,12,'owned'],['states',1440,900,desktop,1,0,0,'neutral'],['busy',1440,900,desktop,1,0,0,'busy'],['city',1440,900,desktop,1,0,0,'city'],['blocked',1440,900,desktop,1,0,0,'blocked']]){
 const logicalWidth=target==='mobile'?1504/scale:w,px=ox+(logicalWidth-380)*scale,py=oy+100*scale;
 // Locked montage context: only the old selected-land rectangle is replaced by
 // an adjacent unobstructed map crop from THIS resolution's own baseline.
 const patch=`<defs><clipPath id="context-clear"><rect x="${px}" y="${py}" width="${360*scale}" height="${506*scale}"/></clipPath></defs><g clip-path="url(#context-clear)"><image x="${400*scale}" width="${w}" height="${h}" href="${img(base)}"/></g>`;
 const background=`<image width="${w}" height="${h}" href="${img(base)}"/>`+patch;
 save(target+'/land.svg',svg(w,h,defs+layer('reference-original',`<image width="554" height="236" href="${img(reference)}"/>`,'style="display:none" sodipodi:insensitive="true"')+layer('baseline-context',background,'sodipodi:insensitive="true"')+`<g transform="translate(${px} ${py}) scale(${scale})">${content(state)}</g>`));
}
save('design-spec.json',JSON.stringify({version:'s1-20261028-085252514',status:'source-awaiting-export',reference:{id:'S1-RES-009',path:reference,scope:'2021–2022 historical S1 tutorial; current S1 unverified; composition only'},baselines:{desktop,mobile},context:'Editable montage using each resolution own baseline; patch only clears old panel. Not a new runtime screenshot.',layout:{panel:[360,418],header:[0,0,360,168],art:[12,68,336,100],image:[16,70,192,96],identity:[220,70,124,96],summary:[16,180,328,86],actions:[16,278,328,128],surfaceActions:[0,278,360,140],surfaceGap:12},components:['land_surface','land_command','land_command_active'],runtime:'LandSelectionPresentation.cs / LandSelectionLayout.cs',remaining:['Official current S1 land selection art unavailable; existing project tiles reused without enlarging historical screenshots.','Existing command icons retained; inline commands now 44 high with 34 icons and separate 36-high captions; geometry diagram fallback for water/bridge retained.'],states:['neutral','owned','busy','city','blocked'],mobile:{width:1600,height:720,safeArea:[56,12,1504,684],scale:684/820}},null,2));
