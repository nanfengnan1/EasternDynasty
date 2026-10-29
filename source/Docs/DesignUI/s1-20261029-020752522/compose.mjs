// Editable SVG layout. Raster exports are produced only by Export-UIDesign.ps1 / Inkscape.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261029-020752522',dir=`Docs/DesignUI/${version}`;
const save=(p,s)=>{fs.mkdirSync(path.dirname(`${dir}/${p}`),{recursive:true});fs.writeFileSync(`${dir}/${p}`,s);};
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const img=(p,x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" href="data:image/png;base64,${fs.readFileSync(p).toString('base64')}"/>`;
const layer=(id,b,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${id}" ${extra}>${b}</g>`;
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${b}</svg>`;
const text=(x,y,t,size=14,color='#eee6cb')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}">${t}</text>`;
const reference='F:/工作项目/率土之滨/rag/assets/8f/8fcba37972ab8cf8c9cbf0a5380be4603753820a759e90d4a518fa0159b7dae2.png';
const art='Validation/CityArt/city-overview-preview.png';
// Use the already shipped town illustration, never a crop of the research screenshot.
const town='Assets/EasternDynasty/Resources/Youzhou/city.png';
const manifest=JSON.parse(fs.readFileSync('Assets/EasternDynasty/Resources/asset-manifest.json','utf8').replace(/^\uFEFF/,''));
const cityEntry=manifest.entries.find(e=>e.id==='map.youzhou.city');
const cityImage='Assets/EasternDynasty/Resources/'+cityEntry.resourcePath+'.png';
const bdir='F:/EasternDynastyBuild/Releases/s1-20261028-225251901/Project/Validation/Visual/';
const baselines=[['desktop','youzhou-1440x900',1440,900,1,0,0,1440,900],['mobile','mobile-1600x720',1600,720,684/820,56,12,1504/(684/820),820]].map(([name,suffix,w,h,scale,x,y,lw,lh])=>{
 const sourcePath=bdir+'s1-20261028-225251901-'+suffix+'/smoke-city-overview.png',p=`${dir}/baseline/${name}.png`;
 fs.mkdirSync(path.dirname(p),{recursive:true});if(!fs.existsSync(p))fs.copyFileSync(sourcePath,p);
 return {name,path:p,sourcePath,sha256:sha(p),w,h,scale,x,y,lw,lh};
});
const circle=(active=false)=>`<circle cx="22" cy="22" r="21" fill="${active?'#5d593e':'#202a27'}" fill-opacity=".94" stroke="#c9b675" stroke-width="1.3"/><circle cx="22" cy="22" r="18.5" fill="none" stroke="#7b795b" stroke-width=".6"/>`;
const command=(active=false)=>`<circle cx="40" cy="18" r="17.5" fill="${active?'#635d3c':'#202a27'}" fill-opacity=".94" stroke="#c9b675" stroke-width="1.1"/><rect x="1" y="36" width="78" height="20" fill="${active?'#635d3c':'#18231f'}" fill-opacity=".88"/><path d="M5 55.5H75" fill="none" stroke="#b5a56e" stroke-width=".7"/>`;
const title=w=>`<rect width="${w}" height="52" fill="#18231f" fill-opacity=".88"/><path d="M1 1V51H${w-1}" fill="none" stroke="#b5a56e" stroke-width="1"/>`;
// Circular return arrow measured from the right-side reset control of RB29-LUMBER-A001.
const reset=`<path d="M25 10 A11 11 0 1 0 27 21" fill="none" stroke="#e0cf91" stroke-width="3"/><path d="M25 3L25 13L15 13Z" fill="#e0cf91"/>`;
// The existing district nine-cell meaning, edited as vector UI rather than new scene artwork.
const grid=Array.from({length:9},(_,i)=>`<rect x="${2+i%3*10}" y="${2+Math.floor(i/3)*10}" width="8" height="8" fill="${i===4?'#e0cf91':'#9eaa8c'}"/>`).join('');
const parts=[['panorama_title',640,104,`<g transform="scale(2)">${title(320)}</g>`],['panorama_command',160,112,`<g transform="scale(2)">${command()}</g>`],['panorama_command_active',160,112,`<g transform="scale(2)">${command(true)}</g>`],['panorama_zoom',88,88,`<g transform="scale(2)">${circle()}</g>`],['panorama_reset',64,64,`<g transform="scale(2)">${reset}</g>`],['panorama_grid',64,64,`<g transform="scale(2)">${grid}</g>`]];
save('components/toolbar.svg',svg(700,620,parts.map(([id,w,h,b],i)=>`<g transform="translate(0 ${i===0?0:112+(i-1)*100})">${layer(id,`<rect width="${w}" height="${h}" fill="#000" fill-opacity="0"/>${b}`)}</g>`).join('')));
function toolbar(w,prefix='',state='normal'){
 const tw=Math.min(320,w-374),xs=[w-358,w-168,w-80],active=state==='planning';
 let surfaces=`<g transform="translate(0 2)">${title(tw)}</g>`,pictures=img(cityImage,6,10,44,36),labels=text(54,23,state==='long'?'幽州城内全景验收用长名称':'新城',16)+text(54,44,active?'建设选址':'城内全景',13,'#d2c79f'),hits=`<rect width="${tw}" height="52" y="2"/>`;
 xs.forEach((x,i)=>{
  surfaces+=`<g transform="translate(${x} 0)">${command(active&&i===2)}</g>`;
  pictures+=i===0?img(cityImage,x+26,4,28,28):`<g transform="translate(${x+26} 4) scale(.875)">${i===1?reset:grid}</g>`;
  labels+=text(x+12,51,[state==='expanded'?'收起城景':'展开城景','总览复位',active?'结束选址':'选址建设'][i],14);
  hits+=`<rect x="${x}" width="80" height="56"/>`;
 });
 [w-270,w-220].forEach((x,i)=>{surfaces+=`<g transform="translate(${x} 6)"${state==='normal'&&i===0?' opacity=".45"':''}>${circle()}</g>`;labels+=text(x+10,36,i?'＋':'－',24,state==='normal'&&i===0?'#899081':'#e0cf91');hits+=`<rect x="${x}" y="6" width="44" height="44"/>`;});
 return layer(prefix+'surfaces',surfaces)+layer(prefix+'images',pictures)+layer(prefix+'live-text',labels)+layer(prefix+'state','',state==='disabled'?'opacity=".45"':'')+layer(prefix+'hit-regions',hits,'style="display:none" fill="none" stroke="#ff44aa"');
}
for(const b of baselines){
 const rail=Math.max(342,Math.min(406,342+(b.lh-180-640)*.8)),scene=b.lw-152-rail-12,w=scene-20;
 const body=layer('reference-original',img(reference,0,0,1767,987),'style="display:none" sodipodi:insensitive="true"')+layer('background',`<rect width="${b.w}" height="${b.h+210}" fill="#192720"/>`)+layer('annotation',text(20,27,'主城城景工具区 · 独立城名图签 / 圆形图文操作 / 分组缩放',18)+text(20,53,'上方为本轮分层设计（平色衬底仅供审稿），下方为225版运行基线；不是改后运行截图。',13))+`<g transform="translate(${b.x+138*b.scale} 78) scale(${b.scale})">${toolbar(w,'',b.name==='mobile'?'long':'normal')}</g>`+layer('baseline-context',img(b.path,0,210,b.w,b.h),'sodipodi:insensitive="true"');
 save(b.name+'/toolbar.svg',svg(b.w,b.h+210,body));
}
save('states/toolbar.svg',svg(1200,490,layer('background','<rect width="1200" height="490" fill="#344339"/>')+['normal','planning','expanded','disabled','long'].map((s,i)=>text(12,20+i*96,s,13)+`<g transform="translate(16 ${28+i*96})"${s==='disabled'?' opacity=".45"':''}>${toolbar(1150,s+'-',s)}</g>`).join('')));
save('design-spec.json',JSON.stringify({version,status:'draft-before-export',reference:{id:'RB29-LUMBER-A001',path:reference,sha256:sha(reference),size:[1767,987],date:'2022-08-11',scope:'Historical official city appearance editor, unknown current S1, excluded; circle/caption separation and reset glyph only. Not a lumber icon or current city art.'},baselines,layout:{height:56,titleMaxWidth:320,titleText:[54,6,'titleWidth-62',40],commandWidth:80,commandArt:[26,4,28,28],commandCaption:[4,36,72,20],expandOffset:358,minusOffset:270,plusOffset:220,resetOffset:168,planningOffset:80,zoomSize:44,viewportUnchanged:true},components:parts.map(([name,w,h])=>({id:'ui.youzhou.'+name,objectId:name,width:w,height:h})),reusedAssets:[{id:cityEntry.id,path:cityImage,sha256:sha(cityImage)}],differences:['Full-width connected dark text strip becomes a capped independent city identity and separated controls.','Camera expand/reset/planning gain separate image and caption, based on the historical circle-caption pattern.','Zoom becomes its own two circular controls; full name keeps two lines and existing viewport/building coordinates remain unchanged.'],notes:['No new city rendering, no screenshot pixels in formal assets, no invented rule parameters.','One standard reset glyph follows visible original; district grid is existing project semantics.','All live names/captions remain Runtime text; after implementation publisher must capture desktop/mobile, long name, expanded/planning and disabled states.']},null,2));
console.log('Editable toolbar drafts ready. Export and inspect before Runtime changes.');
