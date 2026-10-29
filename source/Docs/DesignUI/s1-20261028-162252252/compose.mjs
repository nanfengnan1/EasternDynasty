// Editable composition only; all raster output is exported by Inkscape.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261028-162252252',root=path.join(process.cwd(),'Docs/DesignUI',version);
const reference='F:/工作项目/率土之滨/rag/assets/4f/4ff3395ce69c1849a0b0561c1c3b2cc0f875fb1fdf2db9b9e5439c449cbd8376.jpg';
const baseline=s=>`F:/EasternDynastyBuild/Releases/s1-20261028-141251981/Project/Validation/Visual/s1-20261028-141251981-${s}/smoke-hud-design-quest.png`;
const manifest=JSON.parse(fs.readFileSync('Assets/EasternDynasty/Resources/asset-manifest.json','utf8').replace(/^\uFEFF/,''));
const asset=id=>'Assets/EasternDynasty/Resources/'+manifest.entries.find(e=>e.id===id).resourcePath+'.png';
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const uri=p=>`data:image/${p.endsWith('.jpg')?'jpeg':'png'};base64,${fs.readFileSync(p).toString('base64')}`;
const save=(p,s)=>{const f=path.join(root,p);fs.mkdirSync(path.dirname(f),{recursive:true});fs.writeFileSync(f,s);};
const layer=(id,label,body,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${label}" ${extra}>${body}</g>`;
const text=(x,y,t,size=16,color='#e8e2cd',anchor='start')=>`<text x="${x}" y="${y}" font-size="${size}" fill="${color}" text-anchor="${anchor}" font-family="Microsoft YaHei,Noto Sans SC">${t}</text>`;
const img=(p,x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" preserveAspectRatio="xMidYMid meet" href="${uri(p)}"/>`;
const stretch=(p,x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" preserveAspectRatio="none" href="${uri(p)}"/>`;
const defs='<linearGradient id="paper-shade"><stop stop-color="#222a23" stop-opacity=".96"/><stop offset=".8" stop-color="#222a23" stop-opacity=".9"/><stop offset="1" stop-color="#222a23" stop-opacity=".52"/></linearGradient>';
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}</defs>${b}</svg>`;
const docket=()=>'<rect width="354" height="82" fill="url(#paper-shade)"/><path d="M10 53H344" stroke="#a5a18a" stroke-opacity=".38"/><path d="M62 8V44" stroke="#a5a18a" stroke-opacity=".28"/>';
const reuse=['ui.youzhou.nav_book','ui.youzhou.quest_strip','ui.youzhou.quest_action','ui.youzhou.quest_action_ready'];
function quest(prefix,x,y,state='active'){
 const tr=b=>`<g transform="translate(${x} ${y})">${b}</g>`;
 const done=state==='complete',ready=state==='ready',long=state==='long';
 const surfaces=docket()+stretch(asset(reuse[1]),0,88,212,44)+stretch(asset(ready?reuse[3]:reuse[2]),222,88,132,44);
 const labels=text(74,18,done||long?'第 3 章 · 安定郡县':'第 1 章 · 立足幽州',13,'#c1bda9')+text(74,43,done?'幽州初定':long?'以战养战':'立足幽州',19)
  +text(10,76,done?'全部事宜已完成，查看章节记录。':long?'在己方资源地完成一次屯田':'查看主城与九格城区',14)
  +text(10,115,done?'已领取 10 / 10':ready?'目标达成 1 / 1':'目标进度 0 / 1',13)
  +text(288,116,done?'章节记录 ›':ready?'领取奖励':'前往 ›',16,'#eee7d4','middle');
 return layer(prefix+'quest-surfaces','统一任务页与独立行动',tr(surfaces))+layer(prefix+'quest-images','复用正式书册图像',tr(img(asset(reuse[0]),0,0,56,48)))
  +layer(prefix+'quest-live-labels','章节 / 标题 / 全宽目标 / 动态进度',tr(labels))
  +layer(prefix+'quest-state','实际目标进度',tr(`<rect x="10" y="125" width="192" height="3" fill="#595c50"/><rect x="10" y="125" width="${done||ready?192:0}" height="3" fill="#d1bd80"/>`))
  +layer(prefix+'quest-hit-regions','章节整块操作与独立44高行动（隐藏）',tr('<rect width="354" height="82"/><rect width="56" height="48"/><rect x="222" y="88" width="132" height="44"/>'),'style="display:none" fill="none" stroke="#ff00ff"');
}
save('components/quest-docket.svg',svg(708,164,layer('quest-components','2倍正式无字底面',`<g id="quest-docket" transform="scale(2)">${docket()}</g>`)));
for(const [target,w,h,key] of [['desktop',1440,900,'youzhou-1440x900'],['mobile',1600,720,'mobile-1600x720']]){
 const scale=target==='mobile'?5/6:1;
 const b=layer('baseline-context','141轮真实运行基线（锁定）',img(baseline(key),0,0,w,h),'sodipodi:insensitive="true"')
  +layer('design-board','下方设计对照，非修改后游戏截图',`<rect y="${h}" width="${w}" height="300" fill="#46523f"/>${text(20,h+28,'上方：旧运行基线；下方：任务书册设计及2023历史原图。不是新运行截图。',18)}`)
  +`<g transform="translate(20 ${h+65}) scale(${scale})">${quest('',0,0)}</g>`
  +layer('reference-original','2023原作地图（锁定；仅入口和布局研究）',img(reference,w-550,h+44,500,250),'sodipodi:insensitive="true"');
 save(`${target}/quest-docket.svg`,svg(w,h+300,b));
}
save('states/quest-docket.svg',svg(800,400,layer('state-board','目标 / 可领 / 完成 / 长文', '<rect width="800" height="400" fill="#46523f"/>')+['active','ready','complete','long'].map((s,i)=>text(20+i%2*400,24+Math.floor(i/2)*195,s)+quest(s+'-',20+i%2*400,40+Math.floor(i/2)*195,s)).join('')));
save('design-spec.json',JSON.stringify({version,status:'source-awaiting-export',reference:{id:'STZB-0527',path:reference,sha256:sha(reference),width:720,height:405,year:2023,scope:'Historical map book task entrance; current S1 unknown. Expanded objective layout is project adaptation, not reconstructed original artwork.'},baselines:['youzhou-1440x900','mobile-1600x720'].map(k=>({path:baseline(k),sha256:sha(baseline(k))})),layout:{panel:[20,90,354,132],open:[20,90,354,82],book:[20,90,56,48],chapter:[94,92,270,20],title:[94,112,270,26],objective:[30,146,334,26],progress:[20,178,212,44],action:[242,178,132,44]},fonts:{chapter:13,title:19,objective:14,progress:13,action:16},components:['ui.youzhou.quest_docket'],reusedAssets:reuse.map(id=>({id,path:asset(id),sha256:sha(asset(id))})),notes:['Book, live text, surfaces, progress and click bounds remain separate editable layers.','One 354x82 chapter control contains the book/title/full-width objective; independent 132x44 action below.','Area grows 12 units; no claim of reduced map footprint. Historical image pixels are never production artwork.','No new rules, rewards, save fields, roster or illustrative algorithms.']},null,2));
console.log('Quest docket editable sources prepared for actual Inkscape export.');
