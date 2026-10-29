// Editable UI composition; PNGs are produced only by the real Inkscape export tool.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261028-141251981', root=path.join(process.cwd(),'Docs/DesignUI',version);
const ref='F:/工作项目/率土之滨/rag/assets/20/20c7ebba88a57755f11b7b69c6f93e6544ef25ad04dcb34e0f1380cfe153433f.jpg';
const newer='F:/工作项目/率土之滨/rag/assets/4f/4ff3395ce69c1849a0b0561c1c3b2cc0f875fb1fdf2db9b9e5439c449cbd8376.jpg';
const baseline=s=>`F:/EasternDynastyBuild/Releases/s1-20261028-110251322/Project/Validation/Visual/s1-20261028-110251322-${s}/smoke-hud-design-quest.png`;
const manifest=JSON.parse(fs.readFileSync('Assets/EasternDynasty/Resources/asset-manifest.json','utf8').replace(/^\uFEFF/,''));
const asset=id=>{const e=manifest.entries.find(e=>e.id===id);return `Assets/EasternDynasty/Resources/${e.resourcesPath??e.resourcePath}.png`;};
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const uri=p=>`data:image/${p.endsWith('.jpg')?'jpeg':'png'};base64,${fs.readFileSync(p).toString('base64')}`;
const save=(p,v)=>{const f=path.join(root,p);fs.mkdirSync(path.dirname(f),{recursive:true});fs.writeFileSync(f,v);};
const layer=(id,label,body,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${label}" ${extra}>${body}</g>`;
const text=(x,y,value,size=16,color='#eee7d4',anchor='start')=>`<text x="${x}" y="${y}" font-size="${size}" text-anchor="${anchor}" font-family="Microsoft YaHei,Noto Sans SC" fill="${color}">${value}</text>`;
const defs='<linearGradient id="roster-tone"><stop stop-color="#1a201c" stop-opacity=".97"/><stop offset=".65" stop-color="#30362b" stop-opacity=".93"/><stop offset="1" stop-color="#30362b" stop-opacity=".58"/></linearGradient>';
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}</defs>${b}</svg>`;
const row=(hover=false)=>`<rect width="354" height="44" fill="none"/><path d="M0 1H354V43H0Z" fill="url(#roster-tone)"/><path d="M0 1H40V43H0Z" fill="#30362b" stroke="${hover?'#e0d3a4':'#8b886d'}"/><path d="M44 1H354M44 43H354" stroke="${hover?'#c4b886':'#777d64'}" stroke-opacity="${hover?'.9':'.45'}"/><path d="M224 9V35" stroke="#949278" stroke-opacity=".38"/>`;
const img=(p,x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" preserveAspectRatio="xMidYMid meet" href="${uri(p)}"/>`;
const ids=['portrait.s1_100630','portrait.s1_100744','portrait.s1_100746'];
const arts=ids.map(asset), strip=asset('ui.youzhou.army_strip'),command=asset('ui.youzhou.army_command');
function army(prefix,x,y,state='idle'){
 const tr=s=>`<g transform="translate(${x} ${y})">${s}</g>`;
 let surfaces=img(strip,0,0,354,28)+img(strip,0,176,354,20),images='',labels=text(8,20,'第一部队')+text(346,21,state==='empty'?'0 / 0 兵':'450 / 610 兵',20,'#eee7d4','end'),hits='';
 for(let i=0;i<3;i++){
  const cy=32+i*48;
  surfaces+=`<g transform="translate(0 ${cy})">${row(state==='hover'&&i===0)}</g>`;
  if(state!=='empty')images+=img(arts[i],6,cy+1,30,42);
  labels+=text(48,cy+28,state==='empty'?'待任命':state==='long'&&i===0?'兀突骨·群·步':['皇甫嵩','王子服','郑泰'][i],18);
  labels+=text(344,cy+27,state==='empty'?'空位':['前锋','中军','大营'][i]+' Lv.'+(state==='long'?'50':'1'),13,'#d3cbb2','end');
  hits+=`<rect x="0" y="${cy}" width="354" height="44"/><rect x="6" y="${cy+1}" width="30" height="42"/><rect x="48" y="${cy+2}" width="176" height="40"/><rect x="232" y="${cy+2}" width="112" height="40"/>`;
 }
 surfaces+=img(command,0,198,171,44)+img(command,183,198,171,44);
 labels+=text(8,191,state==='training'?'练兵作业 · 48 秒':state==='returning'?'部队返程 · 27 秒':'驻城待命 · 政令 20/20',13);
 labels+=text(85.5,226,'进入征兵',16,'#eee7d4','middle')+text(268.5,226,state==='returning'?'撤回部队':'调整编队',16,state==='returning'?'#8e8c7f':'#eee7d4','middle');
 return layer(prefix+'army-surfaces','横向名签 / 独立图像底座',tr(surfaces))+layer(prefix+'army-images','已有完整肖像（不裁切）',tr(images))+layer(prefix+'army-live-labels','横排姓名 / 席位等级 / 真实数据',tr(labels))+layer(prefix+'army-state','兵力与命令状态',tr('<rect x="8" y="27" width="338" height="3" fill="#595c50"/><rect x="8" y="27" width="249" height="3" fill="#9ca97d"/>'))+layer(prefix+'army-hit-regions','整行触控及图文诊断（隐藏）',tr(hits),'style="display:none" fill="none" stroke="#ff00ff"');
}
save('components/army-rows.svg',svg(730,200,layer('army-components','2倍正式组件',`<g transform="scale(2)"><g id="army-row">${row()}</g><g id="army-row-hover" transform="translate(0 52)">${row(true)}</g></g>`)));
for(const [target,w,h,key] of [['desktop',1440,900,'youzhou-1440x900'],['mobile',1600,720,'mobile-1600x720']]){
 const b=baseline(key),scale=target==='mobile'?.833333:1;
 const body=layer('baseline-context','110轮未修改运行基线（锁定）',img(b,0,0,w,h),'sodipodi:insensitive="true"')
 +layer('comparison-board','下方为设计组件；不是新游戏截图',`<rect y="${h}" width="${w}" height="340" fill="#46523f"/>${text(20,h+28,'上方：旧运行基线；下方：拟接入的横向队员名签 / 非新运行图',18)}`)
 +layer('reference-original','2021同页原图（锁定；仅布局参考）',img(ref,w-650,h+44,620,286),'sodipodi:insensitive="true"')
 +`<g transform="translate(20 ${h+66}) scale(${scale})">${army('',0,0)}</g>`;
 save(`${target}/army-rows.svg`,svg(w,h+340,body));
}
save('states/army-rows.svg',svg(1180,610,layer('state-board','姓名/空位/忙碌与禁用状态',`<rect width="1180" height="610" fill="#46523f"/>`)+['idle','long','empty','training','returning','hover'].map((s,i)=>text(20+(i%3)*390,24+Math.floor(i/3)*300,s)+army(s+'-',20+(i%3)*390,42+Math.floor(i/3)*300,s)).join('')));
save('design-spec.json',JSON.stringify({version,status:'source-awaiting-export',reference:{id:'STZB-0499',path:ref,sha256:sha(ref),width:2340,height:1080,year:2021,scope:'Historical exploration/promotion map, current S1 unknown. Horizontal unit-name strips only; three member rows are a project adaptation, not an official army list.'},newerReference:{id:'STZB-0527',path:newer,sha256:sha(newer),year:2023,width:720,height:405,scope:'Later same map page; inspected, too low-resolution for production artwork; no clearer left army structure.'},baselines:['youzhou-1440x900','mobile-1600x720'].map(k=>({path:baseline(k),sha256:sha(baseline(k))})),layout:{area:[20,'height-336',354,242],header:[0,0,354,28],card:[0,32,354,44],pitch:48,art:[6,1,30,42],name:[48,2,176,40],identity:[232,2,112,40],status:[0,176,354,20],actions:[[0,198,171,44],[183,198,171,44]]},fonts:{name:18,minimumName:12,identity:13},components:['ui.youzhou.army_row','ui.youzhou.army_row_hover'],reusedAssets:[...ids,'ui.youzhou.army_strip','ui.youzhou.army_command','ui.youzhou.army_command_hover'].map(id=>({id,path:asset(id),sha256:sha(asset(id))})),notes:['Preserve true formation order, all member details and existing commands; rows represent members of one project army, not multiple armies.','Full portrait uses ScaleToFit; portrait and text are separate live clickable regions in a 354x44 row.','No screenshot pixels or dynamic text in formal row skins. No new illustration algorithm.','Desktop/mobile use independent real baselines; design boards are not new runtime screenshots.']},null,2));
console.log('Editable army row draft prepared for actual Inkscape export.');
