// Editable vector layout; all PNG output is produced by the verified Inkscape CLI.
import fs from 'node:fs';
import path from 'node:path';
const version='s1-20261027-134751635', root=path.join(process.cwd(),'Docs/DesignUI',version);
const read=p=>fs.readFileSync(p,'utf8');
const uri=p=>'data:image/png;base64,'+fs.readFileSync(p).toString('base64');
const layer=(id,label,body,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${label}" ${extra}>${body}</g>`;
const save=(p,s)=>{fs.mkdirSync(path.dirname(path.join(root,p)),{recursive:true});fs.writeFileSync(path.join(root,p),s);};
const defs='<linearGradient id="quest-fabric" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#211c1b"/><stop offset=".52" stop-color="#3a2522"/><stop offset="1" stop-color="#72392f"/></linearGradient><linearGradient id="quest-smoke"><stop stop-color="#171c1c" stop-opacity=".92"/><stop offset=".82" stop-color="#171c1c" stop-opacity=".83"/><stop offset="1" stop-color="#171c1c" stop-opacity=".12"/></linearGradient><linearGradient id="quest-ready"><stop stop-color="#663d2f"/><stop offset="1" stop-color="#853e30"/></linearGradient>';
const banner='<g id="quest-banner"><path d="M0 0H56V120L0 110Z" fill="url(#quest-fabric)"/><path d="M3 0V108L53 117V0" fill="none" stroke="#b6a77d" stroke-opacity=".5"/><path d="M4 92H52" stroke="#b6a77d" stroke-opacity=".42"/></g>';
const strip='<rect id="quest-strip" width="290" height="72" fill="url(#quest-smoke)"/>';
const action=ready=>`<g id="quest-action${ready?'-ready':''}"><rect width="132" height="44" fill="${ready?'url(#quest-ready)':'#202321'}" fill-opacity=".94"/><path d="M1 1H131V43H1Z" fill="none" stroke="${ready?'#cbb780':'#8c856c'}" stroke-opacity=".8"/></g>`;
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}</defs>${b}</svg>`;
save('components/quest.svg',svg(540,160,layer('quest-components','01 可编辑竖签/信息底/操作状态',banner+`<g transform="translate(70 0)">${strip}</g><g transform="translate(70 88)">${action(false)}</g><g transform="translate(218 88)">${action(true)}</g>`)));
const type=(x,y,t,size=14,color='#e8e2cd',extra='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}" ${extra}>${t}</text>`;
function quest(state='active',ox=20,oy=90){
 const ready=state==='ready',complete=state==='complete';
 const wrap=b=>`<g transform="translate(${ox} ${oy})">${b}</g>`;
 const skins=banner+`<g transform="translate(64)">${strip}</g><g transform="translate(64 78) scale(.5103448276 .4722222222)">${strip.replace('id="quest-strip"','id="quest-progress-back"')}</g><g transform="translate(222 76)">${action(ready)}</g>`;
 const labels=type(28,42,'任',27,'#ded0a5','text-anchor="middle"')+type(28,78,'务',27,'#ded0a5','text-anchor="middle"')+type(28,106,ready?'可领':complete?'已成':'章节',12,'#e8d2a2','text-anchor="middle"')+type(74,18,complete?'第三章 · 安定郡县':'第 1 章 · 立足幽州',13,'#c1bda9')+type(74,43,complete?'幽州初定':'立足幽州',19)+type(74,67,complete?'全部事宜已完成，查看章节记录。':'查看主城与九格城区',14)+type(74,100,(complete?'已领取':ready?'目标达成':'目标进度')+'  '+(complete?'10 / 10':ready?'1 / 1':'0 / 1'),13)+type(288,104,complete?'章节记录 ›':ready?'领取奖励':'前往 ›',16,'#eee7d4','text-anchor="middle"');
 const status=`<rect x="74" y="108" width="128" height="3" fill="#595c50"/>${ready||complete?'<rect x="74" y="108" width="128" height="3" fill="#d1bd80"/>':''}`;
 const hits='<rect x="0" y="0" width="56" height="120"/><rect x="64" y="0" width="290" height="72"/><rect x="64" y="78" width="148" height="34"/><rect x="222" y="76" width="132" height="44"/>';
 return layer('quest-surfaces','11 竖签与透明信息条',wrap(skins))+layer('quest-live-labels','12 独立动态中文文字',wrap(labels))+layer('quest-state','13 真实进度示例',wrap(status))+layer('quest-hit-regions','14 点击范围-隐藏',wrap(hits),'style="display:none" fill="none" stroke="#ff00ff"');
}
const baseline='F:/EasternDynastyBuild/Releases/s1-20261027-104752269/Project/Validation/Visual/s1-20261027-104752269-youzhou-1440x900/smoke-overview.png';
for(const target of ['desktop','mobile']){
 let source=read(`Docs/DesignUI/s1-20261027-124251578/${target}/map-hud.svg`);
 // Remove the old card from design context using a labelled map-only source area.
 // This is an illustrative compositing layer, never a new runtime screenshot/asset.
 const context=layer('quest-context','10 旧任务卡覆盖-地图片段仅设计上下文',`<defs><clipPath id="quest-context-clip"><rect x="20" y="90" width="354" height="174"/></clipPath></defs><g clip-path="url(#quest-context-clip)"><image x="-380" y="-460" width="1440" height="900" href="${uri(baseline)}"/></g>`,'sodipodi:insensitive="true"');
 source=source.replace('</defs>',defs+'</defs>').replace(/<\/svg>\s*$/,context+quest()+'</svg>');
 save(target+'/map-hud.svg',source);
}
save('states/quest-states.svg',svg(440,500,'<rect width="440" height="500" fill="#59634c"/>'+['active','ready','complete'].map((s,i)=>quest(s,20,20+i*160).replaceAll('id="quest-','id="'+s+'-quest-')).join('')));
save('design-spec.json',JSON.stringify({version,status:'source-awaiting-export-and-inspection',reference:{id:'STZB-0499',file:'F:/工作项目/率土之滨/rag/assets/20/20c7ebba88a57755f11b7b69c6f93e6544ef25ad04dcb34e0f1380cfe153433f.jpg',year:2021,width:2340,height:1080,scope:'Task pennant is clearly visible; current S1 unknown; composition only'},newerReference:{id:'STZB-0527',year:2023,width:720,height:405,role:'Newer same map page; book-like task entrance at lower left; a different historical variant, not a reason to invent detailed artwork'},baselineVersion:'s1-20261027-104752269',area:[20,90,354,120],banner:[20,90,56,120],open:[84,90,290,72],chapter:[94,92,270,20],title:[94,112,270,26],objective:[94,140,270,22],progress:[84,168,148,34],action:[242,166,132,44],fonts:{banner:27,chapter:13,title:19,objective:14,progress:13,action:16},components:['quest_banner','quest_strip','quest_action','quest_action_ready'].map(x=>'ui.youzhou.'+x),states:['active','claimable','all-complete','land-objective-return','modal-suppressed'],runtime:'MapHudQuestPresentation / MapHudQuestLayout',notes:['Vertical black/red pennant follows visible reference, without invented ornaments.','Horizontal transparent strips borrow the readable label hierarchy visible in the map reference; current project objective and progress remain explicit. This is a project adaptation, not official task expansion artwork.','Area height reduces 174 to 120; no bitmap screenshot is a production component.','No new building/portrait illustrations; all dynamic text remains runtime labels.','Desktop and mobile retain their existing independent safe-area context layouts.']},null,2));
console.log('Quest editable sources ready for actual Inkscape export.');
