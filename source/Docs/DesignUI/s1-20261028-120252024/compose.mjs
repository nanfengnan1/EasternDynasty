import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const root=import.meta.dirname;
const version='s1-20261028-120252024';
const write=(p,v)=>{fs.mkdirSync(path.join(root,path.dirname(p)),{recursive:true});fs.writeFileSync(path.join(root,p),v);};
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const data=p=>'data:image/'+(p.endsWith('.jpg')?'jpeg':'png')+';base64,'+fs.readFileSync(p).toString('base64');
const image=(p,x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" preserveAspectRatio="xMidYMid meet" href="${data(p)}"/>`;
const text=(x,y,v,size=15,color='#e8e2cd',extra='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans CJK SC" font-size="${size}" fill="${color}" ${extra}>${v}</text>`;
const layer=(id,content,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${id}" ${extra}>${content}</g>`;
const svg=(w,h,content)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${content}</svg>`;
const reference='F:/工作项目/率土之滨/rag/assets/ea/ead909e3cd3ff7f4a6b2a1c98bcada429ae47bb1a77d5801c202c2c26b10dd6a.jpg';
const released='F:/EasternDynastyBuild/Releases/s1-20261028-110251322/Project/Validation/Visual/';
const baselines=['youzhou-1440x900','mobile-1600x720'].map(n=>released+'s1-20261028-110251322-'+n+'/smoke-land-orders.png');
const tile=n=>'Assets/EasternDynasty/Resources/Youzhou/'+n+'.png';
// Editable sheet and rules follow the reference's single dark information leaf.
// No map pixels, decoration generator, or live text enters the formal component.
const sheet=()=>'<rect width="280" height="228" fill="#202722" fill-opacity=".94"/><path d="M16 152 H264 M16 186 H264" fill="none" stroke="#a8a58d" stroke-opacity=".24" stroke-width=".7"/><path d="M16 227.5 H264" fill="none" stroke="#b9b29a" stroke-opacity=".55" stroke-width=".7"/>';
function facts(state){
 const city=state==='city',blocked=state==='blocked',owned=state==='owned'||city;
 const special=city||blocked;
 const labels=text(18,37,city?'新城 · 主城':blocked?'山地':'石场',28)+text(18,62,'蓟城与上谷 / 7, 14',14,'#b8c0bd')+
 text(223,96,(city?'1':'2')+'级',24,'#e8e2cd','text-anchor="middle"')+
 text(223,116,city?'主城辖区':blocked?'山河地形':'土地等级',13,'#b8c0bd','text-anchor="middle"')+
 text(223,138,owned?'己方领地':blocked?'不可通行':'中立土地',13,'#b8c0bd','text-anchor="middle"')+
 text(16,176,owned?'控制状态':'守军兵力',15,'#b8c0bd')+text(160,177,owned?'已占领':blocked?'—':'12,600',20)+
 text(16,208,city?'城内设施':blocked?'地形作用':'基础产量 / 时',15,'#b8c0bd')+
 text(special?132:160,209,city?'九格城区':blocked?'不可通行':'+360',special?18:20);
 const shield='<g transform="translate(132 159)" stroke="#78b9a0" fill="none" stroke-width="1.4"><path d="M3 4 L11 1 L19 4 L17 15 L11 21 L5 15 Z M11 4 V18"/></g>';
 return layer('fact-surface',sheet())+layer('terrain-image',image(tile(city?'city':blocked?'mountain':'stone'),12,68,156,78))+
 layer('fact-symbols',shield+(!special?image(tile('stone'),132,191,22,22):''))+
 layer('live-facts',labels)+layer('close-control','<rect x="224" y="12" width="44" height="44" fill="#17201d" fill-opacity=".65"/>'+text(246,42,'×',20,'#e8e2cd','text-anchor="middle"'))+
 layer('hit-regions','<rect width="280" height="228" fill="none" stroke="magenta"/><rect x="224" y="12" width="44" height="44" fill="none" stroke="magenta"/>','style="display:none"');
}
for(const [name,w,h,base,s,x,y,oldx,oldy] of [
 ['desktop',1440,900,baselines[0],1,394,435,394,416],
 ['mobile',1600,720,baselines[1],684/820,56+550*684/820,12+382*684/820,514,315]
]){
 const clear=`<defs><clipPath id="old-facts"><rect x="${oldx}" y="${oldy}" width="${280*s+1}" height="${266*s+1}"/></clipPath></defs><g clip-path="url(#old-facts)"><image x="${oldx-1100}" y="${oldy-180}" width="${w}" height="${h}" href="${data(base)}"/></g>`;
 write(name+'/land-facts.svg',svg(w,h,layer('reference-original',image(reference,0,0,554,236),'style="display:none" sodipodi:insensitive="true"')+
 layer('baseline-context',image(base,0,0,w,h)+clear,'sodipodi:insensitive="true"')+`<g transform="translate(${x} ${y}) scale(${s})">${facts('owned')}</g>`));
}
write('states/land-facts.svg',svg(940,330,layer('reference-original',image(reference,0,0,554,236),'style="display:none" sodipodi:insensitive="true"')+layer('baseline-context','<rect width="940" height="330" fill="#4d5648"/>')+
 ['neutral','city','blocked'].map((s,i)=>`<g transform="translate(${20+i*310} 65)">${text(0,-20,s==='neutral'?'中立土地':s==='city'?'主城辖区':'不可通行',18)}${facts(s).replaceAll('id="','id="'+s+'-')}</g>`).join('')));
write('components/land-facts.svg',svg(600,496,layer('fact-components',`<g id="land-facts" transform="translate(20 20) scale(2)">${sheet()}</g>`)));
write('design-spec.json',JSON.stringify({version,status:'draft-awaiting-export-review',reference:{id:'S1-RES-009',path:reference,sha256:sha(reference),size:[554,236],scope:'2021 historical S1 tutorial; layout only, no official rule or HD material claim'},baselines:baselines.map(p=>({path:p,sha256:sha(p),version:'s1-20261028-110251322'})),differences:['Two separated dark fact blocks become one compact information leaf with two horizontal label/value rows.','Control and yield values use a subordinate 20-22px hierarchy instead of competing 27px columns; level and ownership remain distinct.','Complete tile increases from 140x70 to 156x78 while the total leaf shrinks from 280x266 to 280x228.'],layout:{facts:[280,228],header:[0,0,280,148],art:[12,64,256,84],image:[12,68,156,78],identity:[178,72,90,70],summary:[16,156,248,60],defense:[16,156,248,28],yield:[16,188,248,28],label:[0,0,112,28],icon:[116,3,22,22],value:[144,0,104,28],close:[224,12,44,44]},components:['land_facts'],reusedAssets:['map.youzhou.stone','map.youzhou.city','map.youzhou.mountain'],runtime:['LandSelectionLayout.cs','LandSelectionPresentation.cs'],context:'Desktop and mobile are separate actual 110 baseline screenshots. Only the old fact rectangle is cleared using a disclosed neighboring-map montage; repeated terrain in that region is not a new game render. State sheet contains editable sample game data, not official rules. Existing guard symbol is a layout placeholder, not exported new art.'},null,2));
