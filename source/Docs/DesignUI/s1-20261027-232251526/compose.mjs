// Editable, reference-led vector composition. Inkscape produces all final PNGs.
import fs from 'node:fs';
import path from 'node:path';
const version='s1-20261027-232251526',root=path.resolve('Docs/DesignUI/'+version);
const save=(p,s)=>{const f=path.join(root,p);fs.mkdirSync(path.dirname(f),{recursive:true});fs.writeFileSync(f,s);};
const data=p=>'data:image/png;base64,'+fs.readFileSync(p).toString('base64');
const ref='F:/工作项目/率土之滨/rag/assets/e5/e5abedde334d978717e308c869709386316d462218423043087044a4c13134db.png';
const layer=(id,label,b,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${label}" ${extra}>${b}</g>`;
const text=(x,y,t,size=18,color='#e6e0ce',extra='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}" ${extra}>${t}</text>`;
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${b}</svg>`;
const diamond='<path d="M16 0L32 16L16 32L0 16Z" fill="#0d1010"/>';
// The five silhouettes follow the symbols visible at the 2022 reference's
// right-hand stat rows. No historical bitmap pixels or live text are exported.
const symbols=[
 '<path d="M6 6L10 7L23 20L20 23L7 10Z M17 23L23 17L25 19L23 21L27 25L25 27L21 23L19 25Z"/>',
 '<path fill-rule="evenodd" d="M6 8H10V12H14V8H18V12H22V8H26V17H23V26H9V17H6ZM14 20V26H18V20Z"/>',
 '<path d="M16 6L24 10V17Q23 22 16 27Q9 22 8 17V10Z"/>',
 '<path d="M18 5L24 9L21 14L20 19L25 23L23 27H10L7 24L13 22L16 17L14 11Z"/>',
 '<path d="M16 4Q14 10 10 12L8 9Q4 17 10 22L14 23V28H18V23L22 22Q28 17 23 11L21 15Q20 8 16 4Z"/>'
];
const names=['attack','siege','defense','speed','strategy'];
const icon=i=>diamond+`<g fill="#e7d9a2">${symbols[i]}</g>`;
const ring='<rect width="72" height="72" fill="#000" fill-opacity="0"/><circle cx="36" cy="36" r="34.5" fill="#222b29" stroke="#99977d"/><circle cx="36" cy="36" r="30.5" fill="#151d1c" stroke="#5e665b" stroke-width=".6"/>';
save('components/dossier.svg',svg(560,160,layer('dossier-components','原图五属性符号与战法圆形容器 / 2倍矢量',names.map((n,i)=>`<g id="dossier-${n}" transform="translate(${i*76} 0) scale(2)">${icon(i)}</g>`).join('')+`<g id="dossier-skill-ring" transform="translate(410 0) scale(2)">${ring}</g>`)));
const png=(name,x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" preserveAspectRatio="none" href="${data('Assets/EasternDynasty/Resources/YouzhouUI/'+name+'.png')}"/>`;
function content(x,y,w,{owned=false,archive=false,legacy=false,suffix=''}={}){
 const column=(w-28)/2, slot=(w-28)/2;
 let surfaces=png('portrait_base',x+8,y+62,w-16,38);
 let labels=text(x+8,y+24,'基础属性',24)+text(x+8,y+49,legacy?'旧版原型 · 未收录项保留空缺':'资料基础值  /  绿色数字为每级成长',16,'#b9b3a5');
 labels+=text(x+18,y+87,owned?'Lv.20 / 50':'图鉴预览',18)+text(x+w-18,y+87,owned?'经验 640 / 1600':'未关联持有卡等级',16,'#b9b3a5','text-anchor="end"');
 if(owned)surfaces+=`<rect x="${x+8}" y="${y+99}" width="${w-16}" height="3" fill="#41443c"/><rect x="${x+8}" y="${y+99}" width="${(w-16)*.4}" height="3" fill="#c6b782"/>`;
 const vals=['84','15','83','56','62'],growth=['1.72','1.38','1.83','1.18','0.72'],titles=['攻击','攻城','防御','速度','谋略'];
 let images='';
 for(let i=0;i<5;i++){
  const sx=x+8+(i%2)*(column+12),sy=y+116+Math.floor(i/2)*54;
  images+=`<g transform="translate(${sx} ${sy+5})">${icon(i)}</g>`;
  labels+=text(sx+42,sy+29,titles[i],18)+text(sx+90,sy+30,legacy&&i>0?'—':vals[i],23)+(!legacy?text(sx+176,sy+29,'(+'+growth[i]+')',17,'#99b59b'):'');
 }
 labels+=text(x+8,y+300,'可转兵种',16,'#b9b3a5')+text(x+100,y+300,legacy?'资料未收录':'弩兵、死士',18);
 let hits='';
 for(let i=0;i<2;i++){
  const sx=x+8+i*(slot+12),sy=y+332;
  surfaces+=png('portrait_base',sx,sy,slot,100);
  images+=`<g transform="translate(${sx+8} ${sy+14})">${ring}</g>`+png('nav_book',sx+20,sy+37,48,26);
  labels+=text(sx+92,sy+29,i?'学习战法':'自带战法',16,'#b9b3a5');
  labels+=text(sx+92,sy+66,i?(owned?'尚未学习':'持有卡可查看'):(archive?'长名称按真实字体换行':'勇挚刚毅'),19);
  hits+=`<rect x="${sx}" y="${sy}" width="${slot}" height="100"/>`;
 }
 return layer('dossier-surfaces'+suffix,'独立状态条与横向双战法底座',surfaces)+layer('dossier-images'+suffix,'五属性符号 / 已有书册 / 战法容器',images)+layer('dossier-live-labels'+suffix,'真实字段示例；不补造兵力体力或战法等级',labels)+layer('dossier-state'+suffix,'图鉴/持有/旧版状态',`<desc>${owned?'持有状态为设计样例，非玩家存档':legacy?'旧版保留空缺，不填造官方属性':'基础值来自当前目录；未伪装等级20实战属性'}</desc>`)+layer('dossier-hit-regions'+suffix,'两块完整战法区域，打开同一只读战法页',hits,'style="display:none" fill="none" stroke="#ff00ff"');
}
for(const [target,w,h,s,ox,oy,lw,lh] of [['desktop',1440,900,1,0,0,1440,900],['mobile',1600,720,684/820,56,12,1504/(684/820),820]]){
 const previous=`Docs/DesignUI/s1-20261027-222251138/${target}/portrait.svg`;
 const oldBaseline=`Docs/DesignUI/s1-20261027-222251138/baseline/${target}.png`;
 fs.mkdirSync(path.join(root,'baseline'),{recursive:true});fs.copyFileSync(oldBaseline,path.join(root,'baseline',target+'.png'));
 const mw=Math.min(1280,lw-64),mh=Math.min(820,lh-28),px=(lw-mw)/2,py=(lh-mh)/2;
 const x=px+20+mw*.43+28,y=py+198,cw=px+mw-x-24-22;
 // Reuse the preceding editable portrait composition, rather than flattening it.
 let prior=fs.readFileSync(previous,'utf8').replace(/<svg\b[^>]*>/,'').replace(/<\/svg>\s*$/,'');
 // Keep the original hidden reference and baseline layers, with the newly
 // editable portrait layers. Paint only the right attribute viewport.
 const wipe=`<rect x="${ox+x*s}" y="${oy+y*s}" width="${(cw+22)*s}" height="${(mh-282)*s}" fill="#202323"/>`;
 save(`${target}/dossier.svg`,svg(w,h,prior+layer('dossier-baseline-replacement','同页旧右栏被设计区域覆盖；这是设计合成',wipe)+`<g transform="translate(${ox} ${oy}) scale(${s})">${content(x,y,cw)}</g>`));
}
save('states/dossier.svg',svg(1320,510,'<rect width="1320" height="510" fill="#202323"/>'+content(12,12,636,{owned:true})+content(672,12,636,{legacy:true,suffix:'-legacy'})));
save('design-spec.json',JSON.stringify({version,status:'draft-awaiting-export',reference:{id:'STZB-0467',path:ref,size:[1793,1008],year:2022,scope:'Historical official card detail; current S1 styling unknown'},baselineVersion:'s1-20261027-104752269',layout:{contentHeight:440,attributes:[8,0,'width-16',312],status:[8,62,'width-16',38],rowStart:116,rowPitch:54,iconSize:32,columnGap:12,statOffsets:{label:42,value:90,growth:176},troops:[8,278,'width-16',34],skillY:332,skillHeight:100,skillGap:12,skillRing:[8,14,72,72],skillBook:[20,37,48,26],skillTitle:[92,8,'width-100',30],skillName:[92,38,'width-100',54]},components:[...names.map(x=>'dossier_'+x),'dossier_skill_ring'],reusedComponents:['portrait_base','nav_book'],notes:['Attributes span the full information width; two skills move from the vertical rail to one lower row.','Five symbols are editable silhouettes based on official reference; no screenshot pixels exported.','Existing neutral book denotes a skill record, not official native-skill artwork or grade.','No invented locked slots, troop/stamina totals or level-adjusted attributes.','The preceding editable portrait composition is retained. Previews are composites, not new runtime captures.']},null,2));
console.log('Dossier layered design sources written.');
