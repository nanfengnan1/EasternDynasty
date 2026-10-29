import fs from 'node:fs';
import path from 'node:path';
const version='s1-20261028-002751316',root=path.resolve('Docs/DesignUI/'+version);
const save=(p,s)=>{const f=path.join(root,p);fs.mkdirSync(path.dirname(f),{recursive:true});fs.writeFileSync(f,s);};
const data=p=>'data:image/png;base64,'+fs.readFileSync(p).toString('base64');
const ref='F:/工作项目/率土之滨/rag/assets/08/08627a10e55883e80f67fb768ac95327aae33b4610dbdb45858ec548b930b5c9.png';
const layer=(id,label,s,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${label}" ${extra}>${s}</g>`;
const svg=(w,h,s)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${s}</svg>`;
const text=(x,y,s,size=18,color='#e6e0ce',extra='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}" ${extra}>${s}</text>`;
const defs='<defs><linearGradient id="cost-gradient" x2="0" y2="1"><stop stop-color="#383c3c" stop-opacity=".85"/><stop offset="1" stop-color="#242929" stop-opacity=".8"/></linearGradient><linearGradient id="command-gradient" x2="0" y2="1"><stop stop-color="#eceded"/><stop offset="1" stop-color="#a9afb2"/></linearGradient></defs>';
const effect=(w,h)=>`<rect width="${w}" height="${h}" fill="#252b2b" fill-opacity=".75"/><path d="M0 .5H${w}M0 ${h-.5}H${w}" fill="none" stroke="#858575" stroke-opacity=".5"/>`;
const cost=(w,h)=>`<rect width="${w}" height="${h}" fill="url(#cost-gradient)"/><path d="M0 .5H${w}" stroke="#888c87" stroke-opacity=".5"/>`;
const arrow='<rect width="36" height="24" fill="#000" fill-opacity="0"/><path d="M2 3L11 12L2 21H9L18 12L9 3ZM18 3L27 12L18 21H25L34 12L25 3Z" fill="#d4c98d"/>';
const command=(hover=false)=>`<rect x=".5" y=".5" width="359" height="43" fill="url(#command-gradient)" stroke="${hover?'#fff7cf':'#e0e3e4'}"/><path d="M2 2H358" stroke="#fff" stroke-opacity=".7"/>${hover?'<rect x="1" y="1" width="358" height="42" fill="#fff7cf" fill-opacity=".16"/>':''}`;
save('components/facility.svg',svg(1960,570,defs+layer('facility-components','2024设施页：横向效果 / 共享费用 / 独立浅色命令',`<g id="facility-effect" transform="scale(2)">${effect(960,76)}</g><g id="facility-cost" transform="translate(0 170) scale(2)">${cost(960,88)}</g><g id="facility-arrow" transform="translate(0 380) scale(2)">${arrow}</g><g id="facility-command" transform="translate(110 380) scale(2)">${command()}</g><g id="facility-command-hover" transform="translate(900 380) scale(2)">${command(true)}</g>`)));
const manifest=JSON.parse(fs.readFileSync('Assets/EasternDynasty/Resources/asset-manifest.json','utf8'));
const atlas='Assets/EasternDynasty/Resources/'+manifest.entries.find(x=>x.id==='reference.youzhou.resource_hud').resourcePath+'.png';
const icons=[[722,9,41,31],[202,9,38,30],[548,9,41,31],[374,9,38,31]];
let iconNumber=0;
const icon=(i,x,y)=>{
 if(i===4)return `<circle cx="${x+12}" cy="${y+12}" r="10" fill="#efbd69"/><rect x="${x+8}" y="${y+8}" width="8" height="8" fill="#3c362b"/>`;
 const [rx,ry,rw,rh]=icons[i],id='resource-clip-'+(++iconNumber);
 return `<defs><clipPath id="${id}"><rect width="${rw}" height="${rh}"/></clipPath></defs><g transform="translate(${x} ${y}) scale(${28/rw} ${24/rh})"><g clip-path="url(#${id})"><image x="${-rx}" y="${-ry}" width="894" height="74" href="${data(atlas)}"/></g></g>`;
};
function sheet(x,y,w,h,empty=true,state='ready',suffix=''){
 const initial=empty||state==='working';
 const sx=x+20,sw=w-40,vy=y+92+(empty?108:0),iw=sw-24,ax=sx+(sw-360)/2,ay=y+h-62;
 let surfaces='',images='',labels='',hits='';
 labels+=text(sx,y+39,'磨坊',28,'#dac181')+text(sx+sw,y+34,'城区 1 / Lv. '+(initial?'0':'1')+' · 10',16,'#b9c0c4','text-anchor="end"');
 labels+=text(sx,y+62,state==='working'?'施工中 → Lv. 1':state==='maximum'?'已达最高等级':empty?'选择要在此处兴建的设施':'设施升级 · 完工后生效',16,'#b9c0c4');
 // The existing eight selectors are retained in the baseline; their page semantics do not change.

 labels+=text(sx+8,vy+24,'每级增加 160 粮食/小时。',18);
 surfaces+=`<g transform="translate(${sx} ${vy+44})">${effect(iw,76)}</g>`;
 labels+=text(sx+16,vy+88,'粮食 / 小时',18,'#d8c68f');
 const current=sx+iw*.38, next=sx+iw*.76;
 labels+=text(current,vy+67,'当前 Lv. '+(initial?'0':'1'),14,'#b9c0c4')+text(current,vy+103,initial?'0':'160',28);
 labels+=text(next,vy+67,state==='maximum'?'最高等级':'完工 Lv. '+(initial?'1':'2'),14,'#b9c0c4')+text(next,vy+103,state==='maximum'?'—':initial?'160':'320',28,'#94c1a2');
 images+=`<g transform="translate(${sx+iw*.64} ${vy+73})">${arrow}</g>`;
 labels+=text(sx+4,vy+145,state==='working'?'已投入资源 · 中断后全额返还':'建设费用 / 当前库存',16,'#dac181');
 surfaces+=`<g transform="translate(${sx} ${vy+154})">${cost(iw,88)}</g>`;
 for(let i=0;i<5;i++){
  const cx=sx+i*iw/5,n=['粮食','木材','石料','铁矿','金币'][i],c=Math.ceil([100,90,150,35,40][i]*(initial?1:1.65)),stock=[4000,3000,1800,900,1600][i];
  images+=icon(i,cx+10,vy+159);
  labels+=text(cx+44,vy+177,n,16,'#dac181');
  labels+=text(cx+10,vy+204,state==='working'?String(c):`${c} / ${state==='shortage'?0:stock}`,17);
  labels+=text(cx+10,vy+229,state==='working'?'已付 · 返 '+c:state==='shortage'?'缺 '+c:'充足',14,state==='shortage'?'#dec88a':'#b9c0c4');
 }
 labels+=text(sx+4,vy+282,'前置：城主府 1 级 / 当前 1 级',16,'#b9c0c4');
 labels+=text(sx+4,vy+335,state==='working'?'剩余工期 20秒':'建设用时 20秒',18);
 labels+=text(sx+4,vy+367,'项目设施配置 · 收益于完工后生效',13,'#aeb5b5');
 const caption=state==='working'?'中断施工 · 查看返还明细':state==='maximum'?'已达最高等级':empty?'20秒 / 开始建设':'40秒 / 升级设施';
 surfaces+=`<g transform="translate(${ax} ${ay})" opacity="${state==='shortage'?.4:1}">${command()}</g>`;
 labels+=text(ax+180,ay+29,caption,18,state==='shortage'?'#d1d4d4':'#202526','text-anchor="middle"');
 labels+=text(sx+sw/2,ay-20,state==='shortage'?'资源不足，补充后可开工':state==='working'?'施工完成后自动生效；离开页面仍继续建设。':'工匠空闲 · 资源充足 · 可立即开工',16,'#9bc0b2','text-anchor="middle"');
 hits+=`<rect x="${ax}" y="${ay}" width="360" height="44"/>`;
 // A clip matches the actual scroll viewport; content below remains scrollable, not a claim of being visible.
 const clipId='ledger-clip'+suffix;
 const bodySurfaces=surfaces.slice(0,surfaces.lastIndexOf('<g transform="translate('+ax));
 const actionSurface=surfaces.slice(bodySurfaces.length);
 // Keep all live text editable. Clip only content rows to the scrolling window.
 const topLabels=labels.slice(0,labels.indexOf(`<text x="${sx+8}"`));
 const rest=labels.slice(topLabels.length),cut=rest.indexOf(`<text x="${ax+180}"`);
 return `<defs><clipPath id="${clipId}"><rect x="${sx}" y="${vy}" width="${sw}" height="${h-112-(vy-y)}"/></clipPath></defs>`+layer('facility-surfaces'+suffix,'共享效果与费用底 / 固定命令',`<g clip-path="url(#${clipId})">${bodySurfaces}</g>`+actionSurface)+layer('facility-images'+suffix,'既有资源图标 / 原图双箭头',`<g clip-path="url(#${clipId})">${images}</g>`)+layer('facility-live-labels'+suffix,'当前游戏真实字段的设计样例',topLabels+`<g clip-path="url(#${clipId})">${rest.slice(0,cut)}</g>`+rest.slice(cut))+layer('facility-state'+suffix,'空地 / 升级 / 缺资源 / 施工中',`<desc>${state}; sample data from project rules, not official resource values</desc>`)+layer('facility-hit-regions'+suffix,'固定44高行动；滚动区及九格保持',hits,'style="display:none" fill="none" stroke="#ff00ff"');
}
for(const [target,pw,ph,s,ox,oy,lw,lh,folder] of [['desktop',1440,900,1,0,0,1440,900,'youzhou-1440x900'],['mobile',1600,720,684/820,56,12,1504/(684/820),820,'mobile-1600x720']]){
 const baseline=`F:/EasternDynastyBuild/Releases/s1-20261027-104752269/Project/Validation/Visual/s1-20261027-104752269-${folder}/smoke-facility-dossier.png`;
 fs.mkdirSync(path.join(root,'baseline'),{recursive:true});fs.copyFileSync(baseline,path.join(root,'baseline',target+'.png'));
 const w=(lw-48)*.66,x=lw-24-w,y=142,h=lh-240;
 // Preserve the left-hand current building/locator and the existing eight image selectors.
 const wipe=`<rect x="${x+1}" y="${y+1}" width="${w-2}" height="90" fill="#101416"/><rect x="${x+1}" y="${y+198}" width="${w-2}" height="${h-199}" fill="#101416"/>`;
 save(target+'/facility.svg',svg(pw,ph,defs+layer('reference-original','2024官方设施同页；历史布局研究',`<image width="800" height="450" href="${data(ref)}"/>`,'style="display:none" sodipodi:insensitive="true"')+layer('baseline-context','104752269旧实图；非本轮运行结果',`<image width="${pw}" height="${ph}" href="${data(baseline)}"/>`)+`<g transform="translate(${ox} ${oy}) scale(${s})">${layer('sheet-replacement','替换完整右侧说明/收益/费用/命令',wipe)}${sheet(x,y,w,h)}</g>`));
}
save('states/facility.svg',svg(1700,740,defs+'<rect width="1700" height="740" fill="#101416"/>'+sheet(12,12,820,700,false,'working','-working')+sheet(860,12,820,700,false,'shortage','-shortage')));
save('design-spec.json',JSON.stringify({version,status:'draft-awaiting-export',reference:{id:'Q09-CU-A001',path:ref,resolution:[800,450],publishedAt:'2024-05-15',currentS1:'unknown',excludedFromS1:true,use:'Layout only; no screenshot pixels, official costs or continuous-upgrade rules imported'},baselineVersion:'s1-20261027-104752269',layout:{contentHeight:374,description:[8,0,'innerWidth-16',34],effect:[0,44,'innerWidth',76],effectFractions:{unit:[.02,.32],current:[.38,.18],arrow:[.64,36],next:[.76,.22]},costHeadingY:126,costs:[0,154,'innerWidth',88],requirement:[4,258,'innerWidth-8',44],duration:[4,312,'innerWidth-8',28],note:[4,350,'innerWidth-8',24],action:{width:360,height:44,bottom:18},optionHeight:96,optionPitch:108},components:['facility_effect','facility_cost','facility_arrow','facility_command','facility_command_hover'],notes:['Five project costs are retained, even though the 2024 reference shows three resources.','Existing building raster and eight selectors retained; no new program art.','Description precedes the compact horizontal effect row.','First-screen costs remain complete; requirements/notes scroll at the shortest viewport.','The design preserves the isolated fixed command and all original world rules.']},null,2));
console.log('Editable facility sheet sources written; export and inspect before Runtime changes.');
