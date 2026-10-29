// Editable SVG composition; raster output is exclusively produced by Inkscape.
import fs from 'node:fs';
import path from 'node:path';
const version='s1-20261027-155751815', root=path.resolve('Docs/DesignUI',version);
const data=p=>'data:image/png;base64,'+fs.readFileSync(p).toString('base64');
const save=(p,s)=>{fs.mkdirSync(path.dirname(path.join(root,p)),{recursive:true});fs.writeFileSync(path.join(root,p),s);};
const layer=(id,label,body,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${label}" ${extra}>${body}</g>`;
const type=(x,y,t,size=13,extra='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="#eee7d4" ${extra}>${t}</text>`;
const defs='<linearGradient id="atlas-smoke"><stop stop-color="#171c1c" stop-opacity=".88"/><stop offset=".7" stop-color="#171c1c" stop-opacity=".78"/><stop offset="1" stop-color="#171c1c" stop-opacity="0"/></linearGradient><linearGradient id="locator-paper" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#e7e5da"/><stop offset="1" stop-color="#b9baad"/></linearGradient>';
const strip='<rect id="atlas-strip" width="162" height="44" fill="url(#atlas-smoke)"/>';
// Circular location control and teardrop cut-out are visible in 0499 and 0527.
const locator='<g id="atlas-locate"><circle cx="22" cy="22" r="21.5" fill="url(#locator-paper)" stroke="#5a6057"/></g>';
const hover=locator.replace('atlas-locate','atlas-locate-hover').replace('url(#locator-paper)','#f2eddb').replace('#5a6057','#aa9871');
const pin='<path id="atlas-pin" d="M8 0C3.6 0 0 3.6 0 8C0 13.5 8 22 8 22S16 13.5 16 8C16 3.6 12.4 0 8 0ZM8 4A4 4 0 1 1 8 12A4 4 0 1 1 8 4Z" fill="#252a26" fill-rule="evenodd"/>';
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}</defs>${b}</svg>`;
save('components/atlas.svg',svg(340,90,layer('atlas-components','坐标底片 / 定位正常悬停 / 独立图标',strip+`<g transform="translate(180)">${locator}</g><g transform="translate(236)">${hover}</g><g transform="translate(300)">${pin}</g>`)));
const baseline='F:/EasternDynastyBuild/Releases/s1-20261027-104752269/Project/Validation/Visual/s1-20261027-104752269-youzhou-1440x900/smoke-overview.png';
const base=data(baseline);
function atlas(x,y,state='normal'){
 const wrap=b=>`<g transform="translate(${x} ${y})">${b}</g>`;
 const surfaces=strip+`<g transform="translate(178)">${state==='hover'?hover:locator}</g><g transform="translate(14 264) scale(1.2592592593 .5)">${strip.replace('atlas-strip','atlas-legend-strip')}</g>`;
 // Only a clipped baseline sample for composition. Production draws current terrain and events.
 const terrain=`<defs><clipPath id="atlas-sample-clip"><rect x="14" y="54" width="204" height="204"/></clipPath></defs><g clip-path="url(#atlas-sample-clip)" opacity=".88"><image x="-1188" y="-98" width="1440" height="900" href="${base}"/></g>`;
 const images=`<g transform="translate(192 4)">${pin}</g>`;
 const labels=type(8,20,'幽州舆图',17)+type(8,39,state==='edge'?'(0, 39)':'(9, 16)',13)+type(200,38,'归城',11,'text-anchor="middle" style="fill:#252a26"')+type(26,280,'领地',12)+type(82,280,'视野',12)+type(212,280,'S1 · 40×40',12,'text-anchor="end"');
 const legend='<rect x="18" y="272" width="5" height="5" fill="#8dd6b0"/><rect x="69" y="271" width="9" height="7" fill="none" stroke="#eee7d4"/>';
 const hits='<rect x="178" y="0" width="44" height="44"/><rect x="14" y="54" width="204" height="204"/><rect width="162" height="44"/><rect x="14" y="264" width="204" height="22"/>';
 return layer('atlas-surfaces','31 分离坐标条与定位底片',wrap(surfaces))+layer('atlas-terrain-context','32 真实旧地形截图仅作设计占位',wrap(terrain),'sodipodi:insensitive="true"')+layer('atlas-images','33 独立可编辑定位图形',wrap(images))+layer('atlas-live-labels','34 动态坐标与标题示例',wrap(labels))+layer('atlas-state','35 实际领地/视野图例语义',wrap(legend))+layer('atlas-hit-regions','36 点击与阻挡范围-隐藏',wrap(hits),'style="display:none" fill="none" stroke="#ff00ff"');
}
for(const [target,w,h] of [['desktop',1440,900],['mobile',1800,820]]){
 let source=fs.readFileSync(`Docs/DesignUI/s1-20261027-145251750/${target}/map-hud.svg`,'utf8');
 const x=w-252,y=90;
 const context=layer('atlas-context','30 移除旧黑底-地图背景仅为构图占位',`<defs><clipPath id="atlas-context-clip"><rect x="${x}" y="${y}" width="232" height="302"/></clipPath></defs><g clip-path="url(#atlas-context-clip)"><image x="${x-820}" y="${y-200}" width="1440" height="900" href="${base}"/></g>`,'sodipodi:insensitive="true"');
 source=source.replace('</defs>',defs+'</defs>').replace(/<\/svg>\s*$/,context+atlas(x,y)+'</svg>');save(target+'/map-hud.svg',source);
}
save('states/atlas-states.svg',svg(800,340,'<rect width="800" height="340" fill="#697754"/>'+['normal','hover','edge'].map((s,i)=>type(20+i*264,25,['默认 · 动态地图占位','定位悬停','边界坐标示例'][i],15)+atlas(20+i*264,44,s).replaceAll('id="atlas-','id="'+s+'-atlas-').replaceAll('url(#atlas-sample-clip)','url(#'+s+'-atlas-sample-clip)')).join('')));
save('design-spec.json',JSON.stringify({version,status:'source-awaiting-export-inspection',reference:{primary:'STZB-0527',year:2023,dimensions:[720,405],support:'STZB-0499',supportYear:2021,supportDimensions:[2340,1080],scope:'Same map page: floating minimap, round location control and compact coordinate label; current S1 appearance unconfirmed. Historical images are measurement-only.'},baselineVersion:'s1-20261027-104752269',area:{right:20,top:90,width:232,height:286},title:[0,0,162,44],locate:[178,0,44,44],pin:[192,4,16,22],terrain:[14,54,204,204],legend:[14,264,204,22],terrainOpacity:.88,fonts:{title:17,coordinate:13,legend:12,home:11},components:['atlas_strip','atlas_locate','atlas_locate_hover','atlas_pin'].map(x=>'ui.youzhou.'+x),runtime:'MinimapPresentation / MapHudAtlasLayout',notes:['No full backdrop; same 204-square terrain, dynamic ownership and camera polygon.','Baseline atlas image is clipped for design preview only; never imported as an asset.','No new map geography, gameplay, save state or original-game icon raster import.','Inherited header/army/quest/navigation are prior design context, not new runtime screenshots.']},null,2));
console.log('Layered atlas sources ready for Inkscape.');
