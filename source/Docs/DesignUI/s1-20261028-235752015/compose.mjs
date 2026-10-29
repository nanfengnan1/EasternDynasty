// Editable SVG composition. Raster output is produced only by Export-UIDesign/Inkscape.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261028-235752015',root=path.join(process.cwd(),'Docs/DesignUI',version);
const reference='F:/工作项目/率土之滨/rag/assets/4f/4ff3395ce69c1849a0b0561c1c3b2cc0f875fb1fdf2db9b9e5439c449cbd8376.jpg';
const baseline=s=>`F:/EasternDynastyBuild/Releases/s1-20261028-225251901/Project/Validation/Visual/s1-20261028-225251901-${s}/smoke-hud-recruit-seal.png`;
const manifest=JSON.parse(fs.readFileSync('Assets/EasternDynasty/Resources/asset-manifest.json','utf8').replace(/^\uFEFF/,''));
const asset=id=>'Assets/EasternDynasty/Resources/'+manifest.entries.find(e=>e.id===id).resourcePath+'.png';
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const uri=p=>`data:image/${p.endsWith('.jpg')?'jpeg':'png'};base64,${fs.readFileSync(p).toString('base64')}`;
const save=(p,s)=>{const f=path.join(root,p);fs.mkdirSync(path.dirname(f),{recursive:true});fs.writeFileSync(f,s);};
const layer=(id,label,body,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${label}" ${extra}>${body}</g>`;
const text=(x,y,t,size=16,color='#e8e2cd',anchor='start')=>`<text x="${x}" y="${y}" font-size="${size}" fill="${color}" text-anchor="${anchor}" font-family="Microsoft YaHei,Noto Sans SC">${t}</text>`;
const img=(p,x,y,w,h,aspect='xMidYMid meet')=>`<image x="${x}" y="${y}" width="${w}" height="${h}" preserveAspectRatio="${aspect}" href="${uri(p)}"/>`;
const defs='<linearGradient id="sleeve-shade"><stop stop-color="#222a23" stop-opacity=".96"/><stop offset=".8" stop-color="#222a23" stop-opacity=".9"/><stop offset="1" stop-color="#222a23" stop-opacity=".52"/></linearGradient>';
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}</defs>${b}</svg>`;
// Reuses the established flat translucent ribbon and divider language; no invented icon art.
const sleeve=(hover=false)=>'<rect width="264" height="68" fill="url(#sleeve-shade)"/>'+(hover?'<rect x="1" y="1" width="262" height="66" fill="none" stroke="#d1bd80" stroke-width="2"/>':'')+'<path d="M10 43H254" stroke="#a5a18a" stroke-opacity=".38"/>';
const reuse=['ui.youzhou.nav_book','ui.youzhou.quest_strip','ui.youzhou.quest_action','ui.youzhou.quest_action_ready'];
function quest(prefix,x,y,state='active'){
 const tr=b=>`<g transform="translate(${x} ${y})">${b}</g>`;
 const done=state==='complete',ready=state==='ready',long=state==='long',hover=state==='hover';
 const surfaces=`<g transform="translate(90 0)">${sleeve(hover)}</g>`+img(asset(reuse[1]),0,74,212,44,'none')+img(asset(ready?reuse[3]:reuse[2]),222,74,132,44,'none');
 const labels=text(100,15,done||long?'第 3 章 · 安定郡县':'第 1 章 · 立足幽州',13,'#c1bda9')+text(100,38,done?'幽州初定':long?'以战养战':'立足幽州',19)
  +text(100,61,done?'全部事宜已完成，查看章节记录。':long?'在己方资源地完成一次屯田':'查看主城与九格城区',14)
  +text(10,101,done?'已领取 10 / 10':ready?'目标达成 1 / 1':'目标进度 0 / 1',13)
  +text(288,102,done?'章节记录 ›':ready?'领取奖励':'前往 ›',16,'#eee7d4','middle');
 return layer(prefix+'quest-surfaces','分离的任务信息签与行动栏',tr(surfaces))+layer(prefix+'quest-images','独立完整书册；不铺整块底面',tr(img(asset(reuse[0]),0,0,80,68)))
  +layer(prefix+'quest-live-labels','章节 / 标题 / 目标 / 动态进度',tr(labels))
  +layer(prefix+'quest-state','实际目标进度',tr(`<rect x="10" y="111" width="192" height="3" fill="#595c50"/><rect x="10" y="111" width="${done||ready?192:0}" height="3" fill="#d1bd80"/>`))
  +layer(prefix+'quest-hit-regions','统一章节手势与44高行动（隐藏）',tr('<rect width="354" height="68"/><rect x="222" y="74" width="132" height="44"/>'),'style="display:none" fill="none" stroke="#ff00ff"');
}
save('components/quest-sleeve.svg',svg(1080,136,layer('quest-components','2倍无字底面；普通与悬停',`<g id="quest-sleeve" transform="scale(2)">${sleeve()}</g><g id="quest-sleeve-hover" transform="translate(552 0) scale(2)">${sleeve(true)}</g>`)));
for(const [target,w,h,key] of [['desktop',1440,900,'youzhou-1440x900'],['mobile',1600,720,'mobile-1600x720']]){
 const scale=target==='mobile'?684/820:1;
 const b=layer('baseline-context','225版真实运行基线（锁定）',img(baseline(key),0,0,w,h),'sodipodi:insensitive="true"')
  +layer('design-board','下方为设计对照，不是修改后游戏截图',`<rect y="${h}" width="${w}" height="300" fill="#46523f"/>${text(20,h+28,'上：225版运行基线；下：独立书册与任务信息签设计 / 2023原图。非新运行截图。',18)}`)
  +`<g transform="translate(20 ${h+65}) scale(${scale})">${quest('',0,0)}</g>`
  +layer('reference-original','STZB-0527原图（锁定；仅布局研究）',img(reference,w-550,h+44,500,250),'sodipodi:insensitive="true"');
 save(`${target}/quest-sleeve.svg`,svg(w,h+300,b));
}
save('states/quest-sleeve.svg',svg(800,560,layer('state-board','当前 / 可领取 / 完成 / 长目标 / 悬停 / 禁用示意','<rect width="800" height="560" fill="#46523f"/>')+['active','ready','complete','long','hover','disabled'].map((s,i)=>text(20+i%2*400,24+Math.floor(i/2)*185,s)+`<g ${s==='disabled'?'opacity=".55"':''}>${quest(s+'-',20+i%2*400,40+Math.floor(i/2)*185,s)}</g>`).join('')));
save('design-spec.json',JSON.stringify({version,status:'source-awaiting-export',reference:{id:'STZB-0527',path:reference,sha256:sha(reference),width:720,height:405,date:'2023-02-01',scope:'Historical map with detached book task entrance. Current S1 unknown; project expanded summary retains existing commands, not a claim of original reconstruction.'},baselines:['youzhou-1440x900','mobile-1600x720'].map(k=>({path:baseline(k),sha256:sha(baseline(k))})),layout:{panel:[20,90,354,118],open:[20,90,354,68],book:[20,90,80,68],body:[110,90,264,68],chapter:[120,90,244,18],title:[120,108,244,24],objective:[120,132,244,26],progress:[20,164,212,44],action:[242,164,132,44]},fonts:{chapter:13,title:19,objective:14,progress:13,action:16},components:['ui.youzhou.quest_sleeve','ui.youzhou.quest_sleeve_hover'],reusedAssets:reuse.map(id=>({id,path:asset(id),sha256:sha(asset(id))})),notes:['The detached book uses the original aspect ratio; no historical screenshot pixels are used in production.','Task information sits beside rather than underneath the book; 10-unit image-to-panel opening.','Panel height 132 to 118; chapter background area 29028 to 17952 square units; title and objective remain live text.','One parent gesture owns book/title/objective and transparent opening; the opening blocks terrain without an invisible extra command.','No rules, rewards, save fields or roster changes.'],mobile:{safe:[56,12,1504,684],scale:684/820}},null,2));
console.log('Prepared editable quest sleeve sources; export and inspect before Runtime changes.');
