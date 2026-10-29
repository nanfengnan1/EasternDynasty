import fs from 'node:fs';
import path from 'node:path';
const version='s1-20261028-043752328',root=path.resolve('Docs/DesignUI/'+version);
const save=(p,s)=>{const f=path.join(root,p);fs.mkdirSync(path.dirname(f),{recursive:true});fs.writeFileSync(f,s);};
const data=p=>'data:image/'+(p.endsWith('.jpg')?'jpeg':'png')+';base64,'+fs.readFileSync(p).toString('base64');
const layer=(id,title,s,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${title}" ${extra}>${s}</g>`;
const svg=(w,h,s)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${s}</svg>`;
const txt=(x,y,s,size=14,fill='#e3e2d9',extra='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${fill}" ${extra}>${s}</text>`;
const defs='<defs><linearGradient id="cardshade" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#25292d"/><stop offset="1" stop-color="#131619"/></linearGradient><linearGradient id="footer" x1="0" y1="0" x2="1" y2="0"><stop stop-color="#303638"/><stop offset="1" stop-color="#1f2428"/></linearGradient></defs>';
const frame=(w,h,active=false)=>`<rect x="1" y="1" width="${w-2}" height="${h-2}" fill="url(#cardshade)" stroke="${active?'#e2c98b':'#918574'}" stroke-width="${active?2:1}"/>`;
const ribbon=(w,h)=>`<path d="M0 0H${w}V${h-8}L${w/2} ${h}L0 ${h-8}Z" fill="#24282c" fill-opacity=".94"/>`;
save('components/archive.svg',svg(800,680,defs+layer('archive-components','卡面边框 / 姓名旗 / 底栏；不含原图或文字',`<g id="archive-frame">${frame(384,504)}</g><g id="archive-selected" transform="translate(400 0)">${frame(384,504,true)}</g><g id="archive-ribbon" transform="translate(0 530)">${ribbon(56,140)}</g><g id="archive-footer" transform="translate(70 530)"><rect width="384" height="96" fill="url(#footer)"/><path d="M0 1H384" stroke="#918574" stroke-width="2"/></g>`)));
const source=fs.readFileSync('Assets/EasternDynasty/Scripts/Core/S1Roster.Generated.cs','utf8');
const records=source.split('\n').filter(x=>x.includes('yield return')).map(line=>{const m=line.match(/GeneralDefinition\("([^"]+)","([^"]+)"[^,]*,Homeland\.\w+,(\d)/);if(!m)throw Error('parse');return {id:m[1],name:m[2],stars:+m[3],faction:line.match(/faction="([^"]+)"/)[1],troop:line.match(/troop="([^"]+)"/)[1],cost:line.match(/cost=([\d.]+)/)[1],skill:line.match(/nativeSkill="([^"]*)"/)[1],recruitable:line.includes('recruitable=true')};});
const names=['严颜','乐进','何太后','公孙瓒','关羽','关银屏','典韦','冯嫽','凌统','刘备','曹操','张飞'];
const featured=names.map(n=>records.find(x=>x.name===n));
const cards=[...featured,...records.filter(c=>!featured.some(f=>f.id===c.id))];
const ref='F:/工作项目/率土之滨/rag/assets/a0/a0fa7fbb94eb4b1692827442ec2a0865e28f6694df2f15a63e7341dfe003cddb.jpg';
function scene(lw,lh,state){
 const vx=156,vy=230,vw=lw-200,vh=lh-360,inner=vw-24,gap=12,ch=Math.min(252,(vh-gap)/2),desired=(ch-52)/1.45+38,cols=Math.max(1,Math.floor((inner+gap)/(desired+gap))),cw=Math.min(desired,(inner-gap*(cols-1))/cols),left=(inner-cols*cw-gap*(cols-1))/2;
 let surfaces=`<rect x="${vx}" y="${vy}" width="${vw}" height="${vh}" fill="#181d1d"/>`,images='',labels='',hits='';
 for(let i=0;i<cols*2;i++){
  const c=state&&i===0?[...records].sort((a,b)=>b.name.length-a.name.length)[0]:state&&i===1?[...records].sort((a,b)=>b.skill.length-a.skill.length)[0]:cards[i],x=vx+left+i%cols*(cw+gap),y=vy+Math.floor(i/cols)*(ch+gap),artH=ch-70,step=Math.min(24,(ch-112)/c.name.length),railH=step*c.name.length+8;
  surfaces+=`<g transform="translate(${x} ${y})">${frame(cw,ch,state&&i===0)}</g><g transform="translate(${x+4} ${y+30})">${ribbon(28,railH)}</g><rect x="${x+4}" y="${y+4}" width="28" height="24" fill="#4e595a"/><rect x="${x+2}" y="${y+ch-44}" width="${cw-4}" height="42" fill="url(#footer)"/><path d="M${x+2} ${y+ch-44}h${cw-4}" stroke="#918574"/>`;
  images+=`<image x="${x+36}" y="${y+26}" width="${cw-40}" height="${artH}" preserveAspectRatio="xMidYMid meet" href="${data('Assets/EasternDynasty/Resources/Portraits/'+c.id+'.png')}"/>`;
  labels+=txt(x+18,y+23,c.faction,18,'#e3e2d9','text-anchor="middle"')+txt(x+39,y+22,'C '+Number(c.cost).toFixed(1),12)+txt(x+cw-8,y+22,'★'.repeat(c.stars),13,'#ddc78b','text-anchor="end"');
  for(let j=0;j<c.name.length;j++)labels+=txt(x+18,y+30+j*step+step*.8,c.name[j],Math.min(20,step-2),'#e3e2d9','text-anchor="middle"');
  labels+=txt(x+18,y+ch-54,c.troop,16,'#d5c491','text-anchor="middle"')+txt(x+10,y+ch-27,c.skill,14)+txt(x+10,y+ch-8,c.recruitable?'试玩可招募':'资料收藏',12,c.recruitable?'#d8c58e':'#b8bfc4');
  hits+=`<rect x="${x}" y="${y}" width="${cw}" height="${ch}"/>`;
 }
 return layer('archive-surfaces','独立薄框与短姓名旗、战法底栏',surfaces)+layer('archive-images','完整正式画像；等比不裁切',images)+layer('archive-live-labels','阵营 / 姓名 / COST / 星级 / 战法 / 范围',labels)+layer('archive-state','选中 / 历史 / 试玩',`<desc>${state?'selected':'all'}; sample cards from real catalog, not official current S1 roster</desc>`)+layer('archive-hit-regions','完整卡片可点；裁切卡不登记',hits,'style="display:none" fill="none" stroke="#f0f"');
}
for(const [target,pw,ph,s,ox,oy,lw,lh] of [['desktop',1440,900,1,0,0,1440,900],['mobile',1600,720,684/820,56,12,1504/(684/820),820],['states',1440,900,1,0,0,1440,900]]){
 const baseline=`F:/EasternDynastyBuild/Releases/s1-20261027-104752269/Project/Validation/Visual/s1-20261027-104752269-${target==='mobile'?'mobile-1600x720':'youzhou-1440x900'}/smoke-s1-archive.png`;
 if(target!=='states'){fs.mkdirSync(path.join(root,'baseline'),{recursive:true});fs.copyFileSync(baseline,path.join(root,'baseline',target+'.png'));}
 save(target+'/archive.svg',svg(pw,ph,defs+layer('reference-original','原作列表：仅研究卡阵与层级',`<image width="1034" height="517" href="${data(ref)}"/>`,'style="display:none" sodipodi:insensitive="true"')+layer('baseline-context','104752269旧运行基线',`<image width="${pw}" height="${ph}" href="${data(baseline)}"/>`,'sodipodi:insensitive="true"')+`<g transform="translate(${ox} ${oy}) scale(${s})">${scene(lw,lh,target==='states')}</g>`));
}
save('design-spec.json',JSON.stringify({version,status:'source-ready-awaiting-tool-export',reference:{id:'STZB-0506',path:ref,resolution:[1034,517],s1Status:'unknown',date:'unknown; historical collection screenshot',use:'Compact image grid, thin frame, vertical identity and footer only'},secondaryReference:'STZB-0509 early historical list 516x317',baselineVersion:'s1-20261027-104752269',layout:{gap:12,maxHeight:252,height:'min(252,(viewportHeight-12)/2)',desiredWidth:'(height-52)/1.45+38',art:[36,26,'width-40','height-70'],ribbon:[4,30,28,'min(24,(height-112)/nameLength)*nameLength+8'],footer:[2,'height-44','width-4',42]},components:['archive_frame','archive_selected','archive_ribbon','archive_footer'],fonts:{name:20,skill:14,scope:12,cost:12,stars:13,minimum:12},notes:['Complete existing portraits; no crops/upscaling into new production art.','Two complete rows adapt to available safe-area height.','No change to roster/search/scope/navigation/rules/save.','Scope remains explicit: 245 archived identities and 41 trial recruitables, not official current S1 pool.','Surrounding baseline chrome remains old runtime; preview is not runtime evidence.']},null,2));
console.log('Editable archive sources composed; export and inspect before runtime.');
