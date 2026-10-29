import fs from 'node:fs';
import path from 'node:path';
const version='s1-20261028-012751756',root=path.resolve('Docs/DesignUI/'+version);
const previous=path.resolve('Docs/DesignUI/s1-20261028-002751316');
const save=(p,s)=>{const f=path.join(root,p);fs.mkdirSync(path.dirname(f),{recursive:true});fs.writeFileSync(f,s);};
const data=p=>'data:image/png;base64,'+fs.readFileSync(p).toString('base64');
const layer=(id,title,s,attrs='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${title}" ${attrs}>${s}</g>`;
const svg=(w,h,s)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${s}</svg>`;
const text=(x,y,s,size=18,color='#e6e0ce',attrs='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}" ${attrs}>${s}</text>`;
const defs='<defs><linearGradient id="gallery-ground" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#292c2c"/><stop offset="1" stop-color="#353939"/></linearGradient></defs>';
const stage=(w,h)=>`<rect width="${w}" height="${h}" fill="url(#gallery-ground)"/>`;
const footer=(w,h)=>`<rect width="${w}" height="${h}" fill="#1c2020"/><path d="M0 .5H${w}" stroke="#757973" stroke-opacity=".6"/>`;
const selected=(w,h)=>`<rect x="1" y="1" width="${w-2}" height="${h-2}" fill="#e4ddbd" fill-opacity=".06" stroke="#d8c68f" stroke-width="2"/><path d="M2 3H${w-2}" stroke="#e6dba9" stroke-width="3"/>`;
save('components/gallery.svg',svg(1020,960,defs+layer('gallery-surfaces','矩形展台 / 独立底栏 / 选择态',`<g id="facility-stage" transform="scale(2)">${stage(480,320)}</g><g id="facility-stage-footer" transform="translate(0 650) scale(2)">${footer(480,34)}</g><g id="facility-stage-selected" transform="translate(0 740) scale(2)">${selected(96,96)}</g>`)));
const kinds=['Mill','Lumberyard','Quarry','Ironworks','Residence','Barracks','DrillGround','Workshop'];
const names=['磨坊','伐木场','采石场','炼铁场','民居','兵营','校场','工坊'];
function gallery(lw,lh,choice=0,state='empty',suffix=''){
 const dw=(lw-48)*.34-18,dh=lh-240,x=36,y=150,w=dw-24,h=dh-314;
 const px=lw-24-(lw-48)*.66+20,py=234,pw=(lw-48)*.66-40,cell=pw/8;
 let surfaces=stage(w,h),images=`<image x="12" y="22" width="${w-24}" height="${h-58}" preserveAspectRatio="xMidYMid meet" href="${data('Validation/CityArt/'+kinds[choice]+'.png')}"/>`;
 surfaces+=`<g transform="translate(0 ${h-34})">${footer(w,34)}</g>`;
 let labels=text(10,18,'建筑示意',12,'#b4bbba')+text(14,h-11,names[choice]+(state==='working'?' · 施工中':state==='empty'?' · 未建':''),18)+text(w-14,h-11,state==='working'?'0 → 1 / 10':state==='maximum'?'10 / 10':'0 / 10',16,'#d8c68f','text-anchor="end"');
 let hits='';
 const wipe=`<rect x="25" y="143" width="${dw-2}" height="${dh-2}" fill="#101416"/>`;
 let grid='',gridText='';
 const gx=40,gy=142+dh-268,gw=dw-32,ch=48,cw=(gw-12)/3;
 gridText+=text(42,142+dh-280,'城 区 定 位  ·  九格占地',14,'#b9c0c4');
 for(let i=0;i<9;i++){
  const bx=gx+i%3*(cw+6),by=gy+Math.floor(i/3)*54;
  grid+=`<rect x="${bx}" y="${by}" width="${cw}" height="${ch}" fill="${i===0?'#3c4137':'#222a29'}" ${i===0?'stroke="#dac181" stroke-width="2"':''}/>`;
  gridText+=text(bx+8,by+19,String(i+1),14,'#b9c0c4')+text(bx+29,by+19,i===0&&state==='working'?'磨坊':i===4?'城主府':'待建设',14,'#b9c0c4')+text(bx+cw/2,by+42,i===0&&state==='working'?'施工中 20秒':i===4?'Lv. 1':'选择设施',13,'#b9c0c4','text-anchor="middle"');
  hits+=`<rect x="${bx}" y="${by}" width="${cw}" height="${ch}"/>`;
 }
 const qy=142+dh-100;
 grid+=`<rect x="40" y="${qy}" width="${gw}" height="44" fill="#25374a" stroke="#44555e"/>`;
 gridText+=text(54,qy+29,state==='working'?'磨坊 → Lv. 1     20秒':'工匠待命 · 选择城区开始建设',16,'#b9c0c4')+text(52,qy+76,state==='working'?'查看施工 · 磨坊':'项目配置 · 单一施工队列',13,'#b9c0c4');
 if(state==='empty')for(let i=0;i<8;i++){
  const xx=px+i*cell,ww=cell-6;
  surfaces+=`<g transform="translate(${xx-x} ${py-y})">${stage(ww,96)}<g transform="translate(0 60)">${footer(ww,36)}</g>${i===choice?selected(ww,96):''}</g>`;
  images+=`<image x="${xx-x+4}" y="${py-y+1}" width="${ww-8}" height="58" preserveAspectRatio="xMidYMid meet" href="${data('Validation/CityArt/'+kinds[i]+'.png')}"/>`;
  labels+=text(xx-x+ww/2,py-y+77,names[i],15,'#e6e0ce','text-anchor="middle"')+text(xx-x+ww/2,py-y+92,i<5?'可建':i<7?'府 2 解锁':'府 3 解锁',12,i<5?'#b9c0c4':'#d8c68f','text-anchor="middle"');
  hits+=`<rect x="${xx}" y="${py}" width="${ww}" height="96"/>`;
 }
 return layer('gallery-clear'+suffix,'替换大圆章；缩小定位行高仍≥44',wipe+grid)+layer('gallery-surfaces'+suffix,'2024同页矩形展台及独立底栏',`<g transform="translate(${x} ${y})">${surfaces}</g>`)+layer('gallery-images'+suffix,'复用现有项目建筑示意；非新原作资产',`<g transform="translate(${x} ${y})">${images}</g>`)+layer('gallery-live-labels'+suffix,'名称 / 等级 / 解锁条件独立可编辑',`<g transform="translate(${x} ${y})">${labels}</g>`+gridText)+layer('gallery-state'+suffix,'选择态 / 在建 / 最高级',`<desc>${state}; only project values</desc>`)+layer('gallery-hit-regions'+suffix,'八设施与九城区父控件',hits,'style="display:none" fill="none" stroke="#ff00ff"');
}
for(const [target,pw,ph,s,ox,oy,lw,lh] of [['desktop',1440,900,1,0,0,1440,900],['mobile',1600,720,684/820,56,12,1504/(684/820),820]]){
 fs.mkdirSync(path.join(root,'baseline'),{recursive:true});fs.copyFileSync(path.join(previous,'baseline',target+'.png'),path.join(root,'baseline',target+'.png'));
 // The inherited right sheet is the previous reviewed design, not a new runtime screenshot.
 let base=fs.readFileSync(path.join(previous,target,'facility.svg'),'utf8');
 base=base.replace('</svg>',defs+`<g transform="translate(${ox} ${oy}) scale(${s})">${gallery(lw,lh)}</g></svg>`);
 save(target+'/gallery.svg',base);
}
save('states/gallery.svg',svg(1440,900,defs+'<rect width="1440" height="900" fill="#101416"/>'+gallery(1440,900,0,'working','-working')));
save('design-spec.json',JSON.stringify({version,status:'draft-awaiting-export',reference:{id:'Q09-CU-A001',path:'F:/工作项目/率土之滨/rag/assets/08/08627a10e55883e80f67fb768ac95327aae33b4610dbdb45858ec548b930b5c9.png',resolution:[800,450],publishedAt:'2024-05-15',s1Status:'unknown',excludedFromS1:true,use:'Only rectangular facility artwork stage/footer hierarchy; existing eight-option selector is a project adaptation.'},baselineVersion:'s1-20261027-104752269',context:'Right ledger is inherited previous Inkscape design, not a runtime result; surrounding baseline is historical runtime screenshot.',layout:{stage:[12,8,'districtWidth-24','districtHeight-314'],image:[12,22,'stageWidth-24','stageHeight-58'],footerHeight:34,locatorHeadingBottom:298,gridBottom:268,gridHeight:156,queueBottom:100,optionHeight:96,optionImage:[4,1,'optionWidth-8',58],optionFooterY:60,optionNameY:60,optionNameHeight:20,optionStateY:80,optionStateHeight:16},components:['facility_stage','facility_stage_footer','facility_stage_selected'],notes:['No new building art or program raster algorithm. Existing 240x180 project silhouettes remain labeled illustrations.','Grid is district positioning, not a panoramic city interior.','Source values, eight commands, cost panel and construction behavior are unchanged.']},null,2));
console.log('Gallery layered sources written; export and inspect before Runtime implementation.');
