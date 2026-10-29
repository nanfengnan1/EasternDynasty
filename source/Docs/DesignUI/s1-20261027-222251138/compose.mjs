// Editable vector composition; production PNGs are exported only by Inkscape.
import fs from 'node:fs';
import path from 'node:path';
const root=path.resolve('Docs/DesignUI/s1-20261027-222251138');
const save=(p,s)=>{fs.mkdirSync(path.dirname(path.join(root,p)),{recursive:true});fs.writeFileSync(path.join(root,p),s);};
const data=p=>'data:image/'+(p.endsWith('.jpg')?'jpeg':'png')+';base64,'+fs.readFileSync(p).toString('base64');
const ref='F:/工作项目/率土之滨/rag/assets/e5/e5abedde334d978717e308c869709386316d462218423043087044a4c13134db.png';
const layer=(id,title,b,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${title}" ${extra}>${b}</g>`;
const txt=(x,y,t,size=16,color='#e6e0ce',extra='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}" ${extra}>${t}</text>`;
const defs='<linearGradient id="ribbon" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#1c2727"/><stop offset="1" stop-color="#354039"/></linearGradient><linearGradient id="base" x1="0" y1="0" x2="1" y2="0"><stop stop-color="#161e1c" stop-opacity=".96"/><stop offset="1" stop-color="#2c342f" stop-opacity=".62"/></linearGradient>';
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}</defs>${b}</svg>`;
const ribbon='<rect width="48" height="240" fill="#000" fill-opacity="0"/><path d="M.5 .5H47.5V239L.5 221Z" fill="url(#ribbon)" stroke="#77745b" stroke-width="1"/><path d="M5 1V218" stroke="#9b9975" stroke-opacity=".25"/>';
const seal='<rect width="48" height="48" fill="#000" fill-opacity="0"/><path d="M24 1L47 24L24 47L1 24Z" fill="#24362a" stroke="#a7a679"/><path d="M24 5L43 24L24 43L5 24Z" fill="none" stroke="#687b50"/>';
const base='<path d="M0 0H520V68H0Z" fill="url(#base)"/><path d="M0 .5H520" stroke="#77745b" stroke-opacity=".7"/>';
save('components/portrait.svg',svg(1240,500,layer('portrait-components','姓名旗 / 阵营菱章 / 身份底座 — 2倍矢量',`<g id="portrait-ribbon" transform="scale(2)">${ribbon}</g><g id="portrait-seal" transform="translate(120 0) scale(2)">${seal}</g><g id="portrait-base" transform="translate(200 220) scale(2)">${base}</g>`)));
function stage(x,y,w,h,{id='100805',name='孙坚',stars=5,faction='汉',owned=false,cost='3.0',troop='弓',archive=false,layerSuffix=''}={}){
 const step=Math.min(38,(h-104-96)/name.length),bh=68+name.length*step;
 const iw=w-82,ih=h-104,fit=Math.min(iw/240,ih/348),pw=240*fit,ph=348*fit;
 const px=x+68+(iw-pw)/2,py=y+12,bx=x+12,by=y+h-80,bw=w-24;
 let surfaces=`<g transform="translate(${x+10} ${y+12}) scale(1 ${bh/240})">${ribbon}</g><g transform="translate(${x+10} ${y+12})">${seal}</g><g transform="translate(${bx} ${by}) scale(${bw/520} 1)">${base}</g><rect x="${bx+8}" y="${by+8}" width="64" height="52" fill="#151c1b"/>`;
 let labels=txt(x+34,y+44,faction,22,'#ddd7b8','text-anchor="middle"');
 [...name].forEach((g,i)=>labels+=txt(x+34,y+12+55+i*step+28,g,26,'#e6e0ce','text-anchor="middle"'));
 for(let i=0;i<stars;i++)labels+=txt(x+34,y+12+bh+8+i*30+24,'★',24,'#e5bd68','text-anchor="middle"');
 labels+=txt(bx+40,by+22,owned?'等 级':'图 鉴',14,'#b9b3a5','text-anchor="middle"')+txt(bx+40,by+50,owned?'20':faction,28,'#e6e0ce','text-anchor="middle"');
 labels+=txt(bx+86,by+30,owned?'已上阵 · 进阶 2':archive?'历史资料 · 收藏':'可从试玩招募取得',18)+txt(bx+86,by+53,owned?'经验 640 / 1600':`COST ${cost}    /    ${troop}兵`,16,'#b9b3a5');
 if(owned){surfaces+=`<rect x="${bx+86}" y="${by+59}" width="${bw-98}" height="3" fill="#434b43"/><rect x="${bx+86}" y="${by+59}" width="${(bw-98)*.4}" height="3" fill="#e5bd68"/>`;for(let i=0;i<stars;i++)surfaces+=`<rect x="${bx+bw-12-stars*18+i*18}" y="${by+15}" width="14" height="8" fill="${i<2?'#e5bd68':'#434b43'}"/>`;}
 const image=`<image x="${px}" y="${py}" width="${pw}" height="${ph}" href="${data('Assets/EasternDynasty/Resources/Portraits/s1_'+id+'.png')}"/>`;
 return layer('portrait-surfaces'+layerSuffix,'独立旗面、阵营章、底座；不加外框',surfaces)+layer('portrait-images'+layerSuffix,'稳定ID完整画像-不裁切不增加分辨率',image)+layer('portrait-live-labels'+layerSuffix,'运行真实姓名/星级/等级/经验示例',labels)+layer('portrait-hit-regions'+layerSuffix,'画像和身份为只读；模态仍阻挡地图',`<rect x="${x}" y="${y}" width="${w}" height="${h}"/>`,'style="display:none" fill="none" stroke="#ff00ff"');
}
for(const [target,w,h,s,ox,oy,lw,lh] of [['desktop',1440,900,1,0,0,1440,900],['mobile',1600,720,684/820,56,12,1504/(684/820),820]]){
 const file=`F:/EasternDynastyBuild/Releases/s1-20261027-104752269/Project/Validation/Visual/s1-20261027-104752269-${target==='desktop'?'youzhou-1440x900':'mobile-1600x720'}/smoke-card-full.png`;
 const out=path.join(root,'baseline',target+'.png');fs.mkdirSync(path.dirname(out),{recursive:true});fs.copyFileSync(file,out);
 const mw=Math.min(1280,lw-64),mh=Math.min(820,lh-28),ax=(lw-mw)/2+20,ay=(lh-mh)/2+14,aw=mw*.43,ah=mh-28;
 const hidden=layer('reference-original','2022官网周仓详情，仅构图依据；当前S1未知',`<image width="1793" height="1008" href="${data(ref)}"/>`,'style="display:none" sodipodi:insensitive="true"');
 const bg=layer('baseline-context','104752269旧运行图；本图是设计合成非新游戏截图',`<image width="${w}" height="${h}" href="${data(file)}"/><rect x="${ox+ax*s-2}" y="${oy+ay*s-2}" width="${(aw+4)*s}" height="${(ah+4)*s}" fill="#202323"/>`,'sodipodi:insensitive="true"');
 save(`${target}/portrait.svg`,svg(w,h,hidden+bg+`<g transform="translate(${ox} ${oy}) scale(${s})">${stage(ax,ay,aw,ah)}</g>`));
}
save('states/portrait.svg',svg(1800,820,'<rect width="1800" height="820" fill="#202323"/>'+stage(10,12,550,764,{id:'100398',name:'关兴＆张苞',stars:4,faction:'蜀',cost:'4.0',troop:'步',archive:true})+`<g transform="translate(600 0)">${stage(10,12,550,764,{id:'100721',name:'成济',stars:3,faction:'晋',cost:'2.5',troop:'步',archive:true,layerSuffix:'-low'})}</g><g transform="translate(1200 0)">${stage(10,12,550,764,{owned:true,layerSuffix:'-owned'})}</g>`+layer('portrait-state','长名/低星为实际目录；孙坚持有状态为设计样例',txt(20,810,'长名资料卡；不进入试玩招募',16)+txt(620,810,'低星资料卡；不进入试玩招募',16)+txt(1220,810,'孙坚持有状态示例；非玩家存档',16))));
save('design-spec.json',JSON.stringify({version:'s1-20261027-222251138',status:'draft-awaiting-export',reference:{id:'STZB-0467',path:ref,year:2022,size:[1793,1008],scope:'Historical official details layout only; current S1 appearance unknown'},baselineVersion:'s1-20261027-104752269',layout:{image:[68,12,'width-82','height-104'],banner:[10,12,48,'68+nameLength*nameStep'],nameStep:'min(38,(height-200)/nameLength)',stars:['banner.x','banner.yMax+8',48,'stars*30'],base:[12,'height-80','width-24',68]},components:['portrait_ribbon','portrait_seal','portrait_base'],notes:['Full portrait source preserved; no historical pixels in exported components.','Vertical identity column references STZB-0467. Bottom owned/scope strip is project adaptation retaining real fields.','No Figma; Inkscape exports must be inspected before Runtime changes.','Previews composite an old runtime screenshot; not runtime acceptance.']},null,2));
console.log('Portrait layered SVG drafts composed.');
