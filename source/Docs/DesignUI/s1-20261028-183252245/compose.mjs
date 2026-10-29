// Editable vector composition. Only Inkscape exports production image pixels.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const version='s1-20261028-183252245',root=path.resolve('Docs/DesignUI',version);
const save=(p,s)=>{fs.mkdirSync(path.dirname(path.join(root,p)),{recursive:true});fs.writeFileSync(path.join(root,p),s);};
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const data=p=>`data:image/${p.endsWith('.jpg')?'jpeg':'png'};base64,${fs.readFileSync(p).toString('base64')}`;
const image=(p,x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" href="${data(p)}"/>`;
const layer=(id,b,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${id}" ${extra}>${b}</g>`;
const text=(x,y,t,size=13,extra='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="#eee7d4" ${extra}>${t}</text>`;
const defs='<linearGradient id="side-smoke"><stop stop-color="#171c1c" stop-opacity=".88"/><stop offset=".7" stop-color="#171c1c" stop-opacity=".78"/><stop offset="1" stop-color="#171c1c" stop-opacity="0"/></linearGradient>';
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}</defs>${b}</svg>`;
const locked='sodipodi:insensitive="true"';
const reference='F:/工作项目/率土之滨/rag/assets/4f/4ff3395ce69c1849a0b0561c1c3b2cc0f875fb1fdf2db9b9e5439c449cbd8376.jpg';
const support='F:/工作项目/率土之滨/rag/assets/56/569e93a9a0edd8355952f123f61a895f02a717c01049f2d349c3ec3c2fcd87c0.jpg';
const baseDir='F:/EasternDynastyBuild/Releases/s1-20261028-162252252/Project/Validation/Visual/';
const baselines=[
 {path:baseDir+'s1-20261028-162252252-youzhou-1440x900/smoke-hud-design-minimap.png',size:[1440,900],map:[1202,144,204,204]},
 {path:baseDir+'s1-20261028-162252252-mobile-1600x720/smoke-hud-design-minimap.png',size:[1600,720],map:[1361,132,170,170]}
].map(b=>({...b,sha256:sha(b.path)}));
const circle=(hover=false)=>`<circle cx="30" cy="30" r="29.5" fill="${hover?'#3e4639':'#202d29'}" fill-opacity=".94" stroke="${hover?'#dac99a':'#9c9d82'}"/>`;
const pin='<path d="M10 0C4.5 0 0 4.5 0 10C0 17 10 28 10 28S20 17 20 10C20 4.5 15.5 0 10 0ZM10 5A5 5 0 1 1 10 15A5 5 0 1 1 10 5Z" fill="#eee7d4" fill-rule="evenodd"/>';
function atlas(prefix,baseline,hover=false,edge=false,disabled=false){
 const [ox,oy,ow]=baseline.map,k=204/ow;
 const terrain=`<defs><clipPath id="${prefix}terrain-clip"><rect width="204" height="204"/></clipPath></defs><g clip-path="url(#${prefix}terrain-clip)">${image(baseline.path,-ox*k,-oy*k,baseline.size[0]*k,baseline.size[1]*k)}</g>`;
 const labels=text(246,22,'幽州舆图',15,'text-anchor="middle"')+text(246,44,edge?'(0, 39)':'(23, 20)',13,'text-anchor="middle"')+text(246,116,'归城',13,'text-anchor="middle"')+text(230,150,'领地',12)+text(230,174,'视野',12)+text(246,198,'S1 40×40',11,'text-anchor="middle"');
 const hits='<rect width="282" height="204"/><rect width="204" height="204"/><rect x="210" width="72" height="54"/><rect x="216" y="64" width="60" height="60"/><rect x="236" y="70" width="20" height="28"/><rect x="216" y="101" width="60" height="20"/><rect x="210" y="134" width="72" height="70"/>';
 return `<g ${disabled?'opacity=".5"':''}>`+
 layer(prefix+'atlas-surfaces','<rect x="210" width="72" height="54" fill="url(#side-smoke)"/><rect x="210" y="134" width="72" height="70" fill="url(#side-smoke)"/>')+
 layer(prefix+'atlas-terrain-context',terrain,locked)+
 layer(prefix+'atlas-state',`<g transform="translate(216,64)">${circle(hover)}</g><rect x="217" y="143" width="5" height="5" fill="#8dd6b0"/><rect x="215" y="166" width="9" height="7" fill="none" stroke="#eee7d4"/>`)+
 layer(prefix+'atlas-images',`<g transform="translate(236,70)">${pin}</g>`)+
 layer(prefix+'atlas-live-labels',labels)+
 layer(prefix+'atlas-hit-regions',hits,'style="display:none" fill="none" stroke="#ff44bb"')+'</g>';
}
for(const [index,target] of ['desktop','mobile'].entries()){
 const b=baselines[index],[w,h]=b.size,scale=index?5/6:1,sx=index?56:0,sy=index?12:0,lw=index?1804.8:w;
 const x=sx+(lw-302)*scale,y=sy+90*scale;
 // Clean terrain sample covers only the old HUD footprint on this DESIGN board.
 // It is labelled context, never imported and never claimed as a new screenshot.
 const context=`<defs><clipPath id="context-clear"><rect x="${x}" y="${y}" width="${282*scale}" height="${286*scale}"/></clipPath></defs><g clip-path="url(#context-clear)">${image(b.path,x-700*scale,y-390*scale,w,h)}</g>`;
 const body=layer('board-background',`<rect width="${w}" height="${h+70}" fill="#15212a"/>`)+
 layer('reference-original',image(reference,0,0,720,405)+image(support,720,0,500,280),'style="display:none" '+locked)+
 layer('annotation',text(w/2,25,'设计预览：完整204方图＋侧栏；地形来自162轮截图，不是本轮运行结果',16,'text-anchor="middle"')+text(w/2,50,'底部导航仍为旧运行背景；仅右上舆图是本轮设计，释放下方82逻辑单位',13,'text-anchor="middle"'))+
 `<g transform="translate(0,70)">${layer('baseline-context',image(b.path,0,0,w,h),locked)}${layer('map-context-sample',context,locked)}<g transform="translate(${x},${y}) scale(${scale})">${atlas('',b)}</g></g>`;
 save(target+'/atlas.svg',svg(w,h+70,body));
}
let states=layer('background','<rect width="960" height="310" fill="#54614b"/>');
for(const [i,name] of ['normal','hover','disabled'].entries())states+=`<g transform="translate(${20+i*316},60)">${atlas(name+'-',baselines[0],i===1,i===1,i===2)}</g>`+text(20+i*316,32,['正常 / 完整40×40','悬停 / 边界坐标示例','模态 / 背景禁用'][i],16);
states+=text(20,291,'地形为旧基线占位；边界态坐标仅演示排版，真实视野随相机生成。',13);
save('states/atlas.svg',svg(960,310,states));
save('components/atlas.svg',svg(340,130,`<g id="atlas-locate-dark" transform="scale(2)">${circle()}</g><g id="atlas-locate-dark-hover" transform="translate(140,0) scale(2)">${circle(true)}</g><g id="atlas-pin-light" transform="translate(290,0) scale(2)">${pin}</g>`));
save('design-spec.json',JSON.stringify({version,status:'editable-awaiting-export',reference:{id:'STZB-0527',path:reference,sha256:sha(reference),size:[720,405],year:2023,scope:'Historical map HUD: floating atlas and dark round locator. Current S1 appearance unknown; measurement only.'},support:{id:'STZB-0395',path:support,sha256:sha(support),size:[500,280],year:2015,scope:'Historical same-page vertical map controls; supplementary composition only.'},baselines,layout:{panel:['width-302',90,282,204],terrain:[0,0,204,204],title:[210,0,72,54],locate:[216,64,60,60],pin:[236,70,20,28],caption:[216,101,60,20],legend:[210,134,72,70],terrainOpacity:.88},fonts:{title:15,coordinate:13,home:13,legend:12,scope:11},components:['ui.youzhou.atlas_locate_dark','ui.youzhou.atlas_locate_dark_hover','ui.youzhou.atlas_pin_light'],reusedAssets:['Assets/EasternDynasty/Resources/YouzhouUI/atlas_strip.png'].map(p=>({path:p,sha256:sha(p)})),notes:['Project adaptation of two dated same-page references, not a claimed current official S1 reconstruction.','Preserves full square 204x204 live terrain and all 1600 lands; no crop, stretch or hidden map state.','Bounding area 232x286 becomes 282x204: wider by 50, shorter by 82; not a claim of narrower UI.','No raster art algorithm added; no change to minimap pixels/viewport computation/navigation semantics.','Desktop and mobile have separate actual baseline sources; montage labels are explicit.']},null,2));
console.log('Atlas side-rail editable drafts created; export and inspect before Runtime changes.');
