import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const root=import.meta.dirname;
const save=(p,s)=>{fs.mkdirSync(path.join(root,path.dirname(p)),{recursive:true});fs.writeFileSync(path.join(root,p),s);};
const img=p=>'data:image/'+(p.endsWith('.jpg')?'jpeg':'png')+';base64,'+fs.readFileSync(p).toString('base64');
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const reference='F:/工作项目/率土之滨/rag/assets/ea/ead909e3cd3ff7f4a6b2a1c98bcada429ae47bb1a77d5801c202c2c26b10dd6a.jpg';
const release='F:/EasternDynastyBuild/Releases/s1-20261028-095751175/Project/Validation/Visual/';
const desktop=release+'s1-20261028-095751175-youzhou-1440x900/smoke-land-design.png';
const mobile='F:/EasternDynastyBuild/Releases/s1-20261027-104752269/Project/Validation/Visual/s1-20261027-104752269-mobile-1600x720/smoke-land-owned.png';
const svg=(w,h,s)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${s}</svg>`;
const layer=(id,s,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${id}" ${extra}>${s}</g>`;
const t=(x,y,s,n=15,color='#e8e2cd')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans CJK SC" font-size="${n}" fill="${color}">${s}</text>`;
const image=(p,x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" preserveAspectRatio="xMidYMid meet" href="${img(p)}"/>`;
const ui=n=>'Assets/EasternDynasty/Resources/YouzhouUI/'+n+'.png';
const tile=n=>'Assets/EasternDynasty/Resources/Youzhou/'+n+'.png';
const skin=(x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" preserveAspectRatio="none" href="${img(ui('land_surface'))}"/>`;
// Editable circles/strips follow S1-RES-009's action silhouette; no screenshot pixels.
function orderSkin(active=false){return `<rect width="208" height="44" fill="#000" fill-opacity="0"/><path d="M22 5 H192 L207 22 L192 39 H22 Z" fill="${active?'#d8d8cc':'#222c29'}" fill-opacity=".94"/><path d="M44 5 H192 L207 22 L192 39 H44" fill="none" stroke="${active?'#eee8d4':'#8f927e'}" stroke-width=".7"/><circle cx="22" cy="22" r="21" fill="#202824" fill-opacity=".98" stroke="#b7a272" stroke-width="1"/>`;}
function icon(symbol,x,y){
 if(symbol==='farm'||symbol==='city')return image(tile(symbol==='farm'?'grain':'city'),x,y,34,34);
 const lines=symbol==='guard'?[[.16,.2,.5,.08],[.5,.08,.84,.2],[.16,.2,.22,.65],[.84,.2,.78,.65],[.22,.65,.5,.92],[.78,.65,.5,.92],[.5,.2,.5,.76]]:symbol==='train'?[[.21,.12,.21,.92],[.21,.12,.84,.2],[.84,.2,.7,.53],[.7,.53,.21,.47],[.38,.75,.87,.75],[.48,.9,.78,.9]]:symbol==='recall'?[[.82,.23,.28,.23],[.28,.23,.08,.45],[.08,.45,.28,.66],[.28,.45,.8,.45],[.8,.45,.8,.9]]:[[.18,.12,.82,.88],[.82,.12,.18,.88],[.12,.68,.4,.9],[.6,.9,.88,.68]];
 return `<g transform="translate(${x} ${y})" stroke="#d8bc78" stroke-width="2" fill="none">${lines.map(l=>`<path d="M${l[0]*34},${l[1]*34} L${l[2]*34},${l[3]*34}"/>`).join('')}</g>`;
}
const contracts={owned:{height:292,info:116,ys:[128,180,232]},busy:{height:216,info:96,ys:[108,160]},city:{height:144,info:76,ys:[88]},neutral:{height:268,info:200,ys:[212]},blocked:{height:268,info:200,ys:[212]}};
function action(y,label,symbol,active=false,enabled=true){return `<g transform="translate(16 ${y})" opacity="${enabled?1:.65}">${orderSkin(active&&enabled)}${icon(symbol,5,5)}${t(48,29,label,18,active&&enabled?'#252d29':'#e8e2cd')}</g>`;}
function commands(state){
 let labels='',actions='',icons='';const c=contracts[state];
 if(state==='owned'){
  labels=t(20,34,'屯田预计收益 · 政令 20/20',14)+t(45,63,'0')+t(149,63,'0')+t(45,87,'1,200')+t(149,87,'0')+t(20,111,'作业 90 秒 · 完成后返城',13);
  icons=['forest','grain','stone','iron'].map((v,i)=>image(tile(v),18+(i%2)*104,46+Math.floor(i/2)*24,22,22)).join('');
  actions=action(c.ys[0],'驻守','guard')+action(c.ys[1],'屯田·2令','farm',true)+action(c.ys[2],'部队练兵','train');
 }else if(state==='busy'){
  labels=t(20,36,'行军中',18)+t(20,65,'本阶段剩余 16 秒');actions=action(c.ys[0],'撤回部队','recall')+action(c.ys[1],'部队练兵','train');
 }else if(state==='city'){
  labels=t(20,33,'主城全景与设施建设')+t(20,57,'部队、练兵和城建任务');actions=action(c.ys[0],'进入主城','city',true);
 }else{
  labels=t(20,35,'出征消耗')+t(52,65,'30 粮食')+t(52,95,'20 木材')+t(20,135,state==='blocked'?'这块地形不可通行。':'需要先占领与目标')+(state==='blocked'?'':t(20,160,'相邻的土地。'));
  icons=image(tile('grain'),20,46,22,22)+image(tile('forest'),20,76,22,22);actions=action(c.ys[0],state==='blocked'?'不可通行':'派兵占领','attack',false,false);
 }
 return layer('command-information-surface',skin(0,0,240,c.info))+layer('command-state-text',labels)+layer('command-resource-icons',icons)+layer('command-controls',actions)+layer('command-hit-regions',`<rect width="240" height="${c.height}" fill="none" stroke="magenta"/>`,'style="display:none"');
}
// The most recent game baseline remains unaltered outside this one command region.
// Adjacent-map montage clears the old region; this is a labelled design, not a game capture.
for(const [name,w,h,base,s,ox,oy,cx,cy,state] of [
 ['desktop',1440,900,desktop,1,0,0,792,394,'owned'],
 ['mobile',1600,720,mobile,684/820,56,12,974,370,'owned'],
 ['busy',1440,900,desktop,1,0,0,792,432,'busy'],
 ['city',1440,900,desktop,1,0,0,792,468,'city'],
 ['neutral',1440,900,desktop,1,0,0,792,406,'neutral'],
 ['blocked',1440,900,desktop,1,0,0,792,406,'blocked']]){
 const px=ox+cx*s,py=oy+(name==='mobile'?370:394)*s;
 const clear=`<defs><clipPath id="old-command-clear"><rect x="${px}" y="${py}" width="${240*s}" height="${310*s}"/></clipPath></defs><g clip-path="url(#old-command-clear)"><image x="${-240*s}" width="${w}" height="${h}" href="${img(base)}"/></g>`;
 const body=layer('reference-original',image(reference,0,0,554,236),'style="display:none" sodipodi:insensitive="true"')+layer('baseline-context',image(base,0,0,w,h)+clear,'sodipodi:insensitive="true"')+`<g transform="translate(${ox+cx*s} ${oy+cy*s}) scale(${s})">${commands(state)}</g>`;
 if(name==='mobile'){
  const previous=fs.readFileSync(path.join(root,'../s1-20261028-095751175/mobile/land.svg'),'utf8');
  const marker='<g transform="translate(974 370)">';
  if(!previous.includes(marker))throw Error('Previous inspected mobile layout changed');
  save(name+'/land-orders.svg',previous.slice(0,previous.indexOf(marker))+marker+commands(state)+'</g></g></svg>');
 }else save(name+'/land-orders.svg',svg(w,h,body));
}
save('components/land-orders.svg',svg(480,260,layer('land-order-components',`<g id="land-order" transform="translate(16 16) scale(2)">${orderSkin()}</g><g id="land-order-active" transform="translate(16 140) scale(2)">${orderSkin(true)}</g>`)));
save('design-spec.json',JSON.stringify({version:'s1-20261028-110251322',status:'draft-awaiting-export-review',reference:{id:'S1-RES-009',path:reference,sha256:sha(reference),size:[554,236],scope:'2021 historical S1 tutorial, silhouette/layout only; not 2026 rules or HD art'},baselines:[desktop,mobile].map(p=>({path:p,sha256:sha(p)})),differences:['Continuous dark action rectangle becomes a short information surface and separate circular icon/tapered label strips.','Busy/city/single-command states shrink to their actual content; no 220-unit fixed button offset.','Icon at the leading circular end and title at x48 remain independent; selected command uses the light strip seen in the reference.'],contracts,command:{width:208,height:44,icon:[5,5,34,34],label:[48,4,156,36]},components:['land_order','land_order_active'],context:'Same-resolution actual 095 baseline; only right action region is cleared by a disclosed neighboring-map montage. Other panels remain that baseline. State text is editable project sample data, not official rules. Preview is not a runtime screenshot.'},null,2));
