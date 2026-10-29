import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const root=import.meta.dirname;
const save=(p,s)=>{fs.mkdirSync(path.join(root,path.dirname(p)),{recursive:true});fs.writeFileSync(path.join(root,p),s);};
const img=p=>'data:image/'+(p.endsWith('.jpg')?'jpeg':'png')+';base64,'+fs.readFileSync(p).toString('base64');
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const reference='F:/工作项目/率土之滨/rag/assets/ea/ead909e3cd3ff7f4a6b2a1c98bcada429ae47bb1a77d5801c202c2c26b10dd6a.jpg';
const desktop='F:/EasternDynastyBuild/Releases/s1-20261028-085252514/Project/Validation/Visual/s1-20261028-085252514-youzhou-1440x900/smoke-land-design.png';
const mobile='F:/EasternDynastyBuild/Releases/s1-20261027-104752269/Project/Validation/Visual/s1-20261027-104752269-mobile-1600x720/smoke-land-owned.png';
const svg=(w,h,s)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${s}</svg>`;
const layer=(id,s,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${id}" ${extra}>${s}</g>`;
const t=(x,y,s,n=15,color='#e8e2cd')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans CJK SC" font-size="${n}" fill="${color}">${s}</text>`;
const image=(p,x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" preserveAspectRatio="xMidYMid meet" href="${img(p)}"/>`;
const ui=n=>'Assets/EasternDynasty/Resources/YouzhouUI/'+n+'.png';
const tile=n=>'Assets/EasternDynasty/Resources/Youzhou/'+n+'.png';
const skin=(x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" preserveAspectRatio="none" href="${img(ui('land_surface'))}"/>`;
// Existing symbols are measured layout placeholders, not new exported artwork.
function icon(symbol,x,y){
 const lines=symbol==='guard'?[[.16,.2,.5,.08],[.5,.08,.84,.2],[.16,.2,.22,.65],[.84,.2,.78,.65],[.22,.65,.5,.92],[.78,.65,.5,.92],[.5,.2,.5,.76]]:symbol==='train'?[[.21,.12,.21,.92],[.21,.12,.84,.2],[.84,.2,.7,.53],[.7,.53,.21,.47],[.38,.75,.87,.75],[.48,.9,.78,.9]]:symbol==='recall'?[[.82,.23,.28,.23],[.28,.23,.08,.45],[.08,.45,.28,.66],[.28,.45,.8,.45],[.8,.45,.8,.9]]:[[.18,.12,.82,.88],[.82,.12,.18,.88],[.12,.68,.4,.9],[.6,.9,.88,.68]];
 if(symbol==='farm'||symbol==='city')return image(tile(symbol==='farm'?'grain':'city'),x,y,34,34);
 return `<g transform="translate(${x} ${y})" stroke="#d8bc78" stroke-width="2" fill="none">${lines.map(l=>`<path d="M${l[0]*34},${l[1]*34} L${l[2]*34},${l[3]*34}"/>`).join('')}</g>`;
}
function action(y,label,symbol,active=false,enabled=true){
 return `<g opacity="${enabled?1:.65}"><image x="16" y="${y}" width="208" height="44" preserveAspectRatio="none" href="${img(ui(active?'land_command_active':'land_command'))}"/>${icon(symbol,40,y+5)}${t(82,y+29,label,18)}</g>`;
}
function facts(state){
 const owned=state==='owned'||state==='busy',city=state==='city',blocked=state==='blocked';
 const skins=skin(0,0,280,152)+skin(0,164,280,102);
 const labels=t(18,37,city?'主城':blocked?'山地':'石场',28)+t(18,62,'蓟城与上谷 / 7, 14',14,'#b8c0bd')+t(241,41,'×',22)+t(205,105,city?'1':'2',30)+t(170,126,city?'主城辖区':blocked?'山河地形':'土地等级',14,'#b8c0bd')+t(170,148,owned||city?'己方领地':blocked?'不可通行':'中立土地',14)+t(16,192,owned||city?'控制状态':'守军兵力',14,'#b8c0bd')+t(50,232,owned||city?'已占领':blocked?'—':'300',22)+t(146,192,city?'城内设施':blocked?'地形作用':'基础产量 / 时',14,'#b8c0bd')+t(180,232,city?'九格城区':blocked?'不可通行':'+360',city||blocked?16:22);
 return layer('fact-surfaces',skins)+layer('terrain-image',image(tile(city?'city':blocked?'mountain':'stone'),16,76,140,70))+layer('fact-symbols',icon('guard',16,205)+(!city&&!blocked?image(tile('stone'),148,209,26,26):''))+layer('dynamic-facts',labels)+layer('fact-hit-regions','<rect width="280" height="266" fill="none" stroke="magenta"/><rect x="224" y="12" width="44" height="44" fill="none" stroke="magenta"/>','style="display:none"');
}
function commands(state){
 let labels='',actions='';
 if(state==='owned'){
  labels=t(20,34,'屯田预计收益 · 政令 20/20',14)+t(45,63,'0',15)+t(149,63,'0',15)+t(45,87,'1,200',15)+t(149,87,'0',15)+t(20,111,'作业 90 秒 · 完成后返城',13);
  actions=action(128,'驻守','guard')+action(180,'屯田·2令','farm',true)+action(232,'部队练兵','train');
 }else if(state==='busy'){
  labels=t(20,36,'行军中',18)+t(20,65,'本阶段剩余 16 秒',15);actions=action(180,'撤回部队','recall')+action(232,'部队练兵','train');
 }else if(state==='city'){
  labels=t(20,39,'主城全景与设施建设')+t(20,65,'部队、练兵和城建任务');actions=action(232,'进入主城','city',true);
 }else{
  labels=t(20,35,'出征消耗')+t(52,65,'30 粮食')+t(52,95,'20 木材')+t(20,135,state==='blocked'?'这块地形不可通行。':'需要先占领与目标')+(state==='blocked'?'':t(20,160,'相邻的土地。'));
  actions=action(232,state==='blocked'?'不可通行':'派兵占领','attack',false,false);
 }
 const icons=state==='owned'?['forest','grain','stone','iron'].map((v,i)=>image(tile(v),18+(i%2)*104,46+Math.floor(i/2)*24,22,22)).join(''):state==='neutral'||state==='blocked'?image(tile('grain'),20,46,22,22)+image(tile('forest'),20,76,22,22):'';
 return layer('command-surface',skin(0,0,240,292))+layer('command-state-text',labels)+layer('command-resource-icons',icons)+layer('command-controls',actions)+layer('command-hit-regions','<rect x="16" y="12" width="208" height="264" fill="none" stroke="magenta"/>','style="display:none"');
}
function place(w,h,tx,ty,valid=true){
 if(valid&&tx>=718&&tx<=w-332&&ty>=144&&ty<=h-124){const y=Math.max(100,Math.min(h-392,ty-146));return {fx:Math.max(394,tx-352),fy:y+13,cx:tx+72,cy:y,mode:'flanking'};}
 const y=valid&&ty<h/2?h-392:100;return{fx:w-560,fy:y+13,cx:w-260,cy:y,mode:'edge-dock'};
}
const drafts=[['desktop',1440,900,desktop,1,0,0,720,540,'owned'],['mobile',1600,720,mobile,684/820,56,12,902,516,'owned'],['neutral',1440,900,desktop,1,0,0,720,540,'neutral'],['busy',1440,900,desktop,1,0,0,720,540,'busy'],['city',1440,900,desktop,1,0,0,720,540,'city'],['blocked',1440,900,desktop,1,0,0,720,540,'blocked'],['edge',1440,900,desktop,1,0,0,1350,200,'neutral']];
const records=[];
for(const [name,w,h,base,s,ox,oy,tx,ty,state]of drafts){
 const lw=name==='mobile'?1504/s:w,lh=name==='mobile'?820:h,p=place(lw,lh,tx,ty);
 const px=ox+(lw-380)*s,py=oy+100*s;
 const clear=`<defs><clipPath id="clear-old-panel"><rect x="${px}" y="${py}" width="${360*s}" height="${506*s}"/></clipPath></defs><g clip-path="url(#clear-old-panel)"><image x="${400*s}" width="${w}" height="${h}" href="${img(base)}"/></g>`;
 const body=layer('reference-original',image(reference,0,0,554,236),'style="display:none" sodipodi:insensitive="true"')+layer('baseline-context',image(base,0,0,w,h)+clear,'sodipodi:insensitive="true"')+`<g transform="translate(${ox} ${oy}) scale(${s})"><g transform="translate(${p.fx} ${p.fy})">${facts(state)}</g><g transform="translate(${p.cx} ${p.cy})">${commands(state)}</g></g>`;
 save(name+'/land.svg',svg(w,h,body));records.push({name,state,canvas:[w,h],scale:s,target:[tx,ty],placement:p});
}
save('design-spec.json',JSON.stringify({version:'s1-20261028-095751175',status:'draft-awaiting-export-review',reference:{id:'S1-RES-009',path:reference,sha256:sha(reference),size:[554,236],scope:'2021 historical S1 land selection; layout only; not current S1 art or rules'},baselines:[desktop,mobile].map(p=>({path:p,sha256:sha(p)})),differences:['Fixed remote right rail becomes two target-relative cards with an open center.','Compact 140x70 complete terrain image reduces duplicated landscape dominance.','Three narrow side-by-side commands become vertical 208x44 horizontal picture/text strips, 140-wide captions.'],layout:{facts:[280,266],commandSurface:[240,292],art:[12,68,256,84],image:[16,76,140,70],identity:[166,74,102,78],summary:[16,164,248,102],actions:[16,12,208,264],commandYs:[116,168,220],minimumFontSize:14,preferredFontSize:18},drafts:records,reusedAssets:['ui.youzhou.land_surface','ui.youzhou.land_command','ui.youzhou.land_command_active','map.youzhou.stone','map.youzhou.city','map.youzhou.mountain'],newAssets:[],context:'Locked same-resolution older runtime baseline with adjacent-map clearing montage; previews are not runtime screenshots. Resource values are project sample data. Reference and baseline layers embedded. Existing icon vectors are layout traces only.'},null,2));
