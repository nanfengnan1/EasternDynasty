// Editable vector composition only. All raster exports are made by Inkscape.
import fs from 'node:fs';
import path from 'node:path';
const version='s1-20261027-145251750', root=path.join(process.cwd(),'Docs/DesignUI',version);
const read=p=>fs.readFileSync(p,'utf8');
const data=p=>'data:image/png;base64,'+fs.readFileSync(p).toString('base64');
const manifest=JSON.parse(read('Assets/EasternDynasty/Resources/asset-manifest.json').replace(/^\uFEFF/,''));
const asset=id=>data('Assets/EasternDynasty/Resources/'+manifest.entries.find(x=>x.id===id).resourcePath+'.png');
const layer=(id,label,body,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${label}" ${extra}>${body}</g>`;
const save=(p,s)=>{fs.mkdirSync(path.dirname(path.join(root,p)),{recursive:true});fs.writeFileSync(path.join(root,p),s);};
const defs='<linearGradient id="army-paper" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#e4e0d2"/><stop offset="1" stop-color="#b7b8a5"/></linearGradient><linearGradient id="army-name" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#201d1a"/><stop offset=".65" stop-color="#332321"/><stop offset="1" stop-color="#69382f"/></linearGradient><linearGradient id="army-smoke"><stop stop-color="#171c1c" stop-opacity=".94"/><stop offset=".8" stop-color="#171c1c" stop-opacity=".86"/><stop offset="1" stop-color="#171c1c" stop-opacity=".36"/></linearGradient>';
const card='<g id="army-card"><rect width="110" height="142" fill="url(#army-paper)"/><path d="M.5 .5H109.5V141.5H.5Z" fill="none" stroke="#a8a58f"/><path d="M3 4H24V116L3 111Z" fill="url(#army-name)"/><rect x="3" y="122" width="104" height="20" fill="#333831"/></g>';
const strip='<rect id="army-strip" width="354" height="28" fill="url(#army-smoke)"/>';
const command='<g id="army-command"><rect width="171" height="44" fill="#202321" fill-opacity=".94"/><path d="M.5 .5H170.5V43.5H.5Z" fill="none" stroke="#8c856c" stroke-opacity=".8"/></g>';
const commandHover=command.replace('army-command','army-command-hover').replace('#202321','#35382f').replace('#8c856c','#c2b48c');
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}</defs>${b}</svg>`;
save('components/army.svg',svg(520,160,layer('army-components','01 独立卡片/状态条/行动组件',card+`<g transform="translate(130)">${strip}</g><g transform="translate(130 50)">${command}</g><g transform="translate(313 50)">${commandHover}</g>`)));
const type=(x,y,t,size=14,color='#e8e2cd',extra='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}" ${extra}>${t}</text>`;
const portraits=['s1_100039','s1_100744','s1_100746'];
function army(state='idle',ox=20,oy=564){
 const empty=state==='empty',busy=state==='training',names=['皇甫嵩','王子服','郑泰'];
 const wrap=b=>`<g transform="translate(${ox} ${oy})">${b}</g>`;
 let skins=strip,images='',labels=type(8,21,'第一部队',16)+type(346,21,empty?'0 / 0 兵':'450 / 610 兵',20,'#eee7d4','text-anchor="end"'),hits='';
 for(let i=0;i<3;i++){
  const x=i*122,y=32;
  skins+=`<g transform="translate(${x} ${y})">${card.replace('id="army-card"','id="army-card-'+i+'"')}</g>`;
  if(!empty){
   images+=`<image x="${x+26}" y="${y+4}" width="80" height="116" preserveAspectRatio="xMidYMid meet" href="${asset('portrait.'+portraits[i])}"/>`;
   labels+=Array.from(names[i]).map((t,j)=>type(x+13.5,y+24+j*23,t,17,'#eee7d4','text-anchor="middle"')).join('');
  }else labels+=type(x+65,y+69,'待任命',14,'#454d40','text-anchor="middle"');
  labels+=type(x+55,y+137,empty?'空位':['前锋 Lv.1','中军 Lv.1','大营 Lv.1'][i],13,'#e8e2cd','text-anchor="middle"');
  hits+=`<rect x="${x}" y="${y}" width="110" height="142"/>`;
 }
 skins+=`<g transform="translate(0 176) scale(1 .7142857143)">${strip.replace('id="army-strip"','id="army-status"')}</g><g transform="translate(0 198)">${command}</g><g transform="translate(183 198)">${command.replace('id="army-command"','id="army-secondary"')}</g>`;
 labels+=type(8,191,busy?'练兵中 · 目标土地 (10, 14)':'驻城待命 · 政令 20/20',13)+type(85.5,226,'进入征兵',16,'#eee7d4','text-anchor="middle"')+type(268.5,226,busy?'撤回部队':'调整编队',16,'#eee7d4','text-anchor="middle"');
 const progress='<rect x="8" y="27" width="338" height="3" fill="#595c50"/>'+(!empty?'<rect x="8" y="27" width="249.3" height="3" fill="#9ca97d"/>':'');
 hits+='<rect x="0" y="0" width="354" height="28"/><rect x="0" y="176" width="354" height="20"/><rect x="0" y="198" width="171" height="44"/><rect x="183" y="198" width="171" height="44"/>';
 return layer('army-surfaces','21 分离的浅底卡签与深色信息条',wrap(skins))+layer('army-images','22 真实正式武将完整图片',wrap(images))+layer('army-live-labels','23 动态姓名/席位/兵力/状态示例',wrap(labels))+layer('army-state','24 真实兵力比例示例',wrap(progress))+layer('army-hit-regions','25 触控范围-隐藏',wrap(hits),'style="display:none" fill="none" stroke="#ff00ff"');
}
const baseline='F:/EasternDynastyBuild/Releases/s1-20261027-104752269/Project/Validation/Visual/s1-20261027-104752269-youzhou-1440x900/smoke-overview.png';
for(const [target,h] of [['desktop',900],['mobile',820]]){
 let source=read(`Docs/DesignUI/s1-20261027-134751635/${target}/map-hud.svg`);
 const oy=h-336;
 const context=layer('army-context','20 移除旧部队底框-地图片段仅用于设计上下文',`<defs><clipPath id="army-context-clip"><rect x="20" y="${oy}" width="354" height="242"/></clipPath></defs><g clip-path="url(#army-context-clip)"><image x="-980" y="${oy-400}" width="1440" height="900" href="${data(baseline)}"/></g>`,'sodipodi:insensitive="true"');
 source=source.replace('</defs>',defs+'</defs>').replace(/<\/svg>\s*$/,context+army('idle',20,oy)+'</svg>');
 save(target+'/map-hud.svg',source);
}
save('states/army-states.svg',svg(1140,302,'<rect width="1140" height="302" fill="#6a7457"/>'+['idle','training','empty'].map((s,i)=>type(20+i*378,22,['驻城示例','练兵状态示例（非运行）','空编队示例'][i],14)+army(s,20+i*378,40).replaceAll('id="army-','id="'+s+'-army-')).join('')));
save('controls/command-states.svg',svg(590,100,'<rect width="590" height="100" fill="#6a7457"/><defs><filter id="disabled-tint" color-interpolation-filters="sRGB"><feColorMatrix type="matrix" values=".55 0 0 0 0  0 .55 0 0 0  0 0 .55 0 0  0 0 0 1 0"/></filter></defs>'+layer('army-control-states','正常/悬停/返回中禁用',type(10,20,'正常',13)+type(205,20,'悬停',13)+type(400,20,'返回中 · 禁用',13)+`<g transform="translate(10 32)">${command}${type(85.5,28,'调整编队',16,'#eee7d4','text-anchor="middle"')}</g><g transform="translate(205 32)">${commandHover}${type(85.5,28,'调整编队',16,'#eee7d4','text-anchor="middle"')}</g><g transform="translate(400 32)" filter="url(#disabled-tint)">${command.replace('id="army-command"','id="army-command-disabled"')}${type(85.5,28,'撤回部队',16,'#eee7d4','text-anchor="middle"')}</g>`)));
save('design-spec.json',JSON.stringify({version,status:'source-awaiting-export-and-inspection',reference:{id:'STZB-0499',year:2021,file:'F:/工作项目/率土之滨/rag/assets/20/20c7ebba88a57755f11b7b69c6f93e6544ef25ad04dcb34e0f1380cfe153433f.jpg',scope:'Map unit label strips and separated light navigation tickets; project adapts these observed components for the existing three-general roster. Not an official expanded army panel.'},newerReference:{id:'STZB-0527',year:2023,width:720,height:405,scope:'Same map page; later horizontal navigation variant, low-resolution measurement only, current S1 unconfirmed.'},baselineVersion:'s1-20261027-104752269',area:{x:20,bottom:94,width:354,height:242},header:[0,0,354,28],card:[0,32,110,142],cardPitch:122,art:[26,4,80,116],name:[3,4,21,112],identity:[3,122,104,20],statusStrip:[0,176,354,20],actions:[[0,198,171,44],[183,198,171,44]],fonts:{header:16,strength:20,name:17,identity:13,status:13,command:16},components:['army_card','army_strip','army_command'].map(x=>'ui.youzhou.'+x),states:['idle','long-name-max-level','empty','training','marching','returning','modal-suppressed'],runtime:'MapHudArmyLayout / MapHudArmyPresentation',notes:['No full rectangular backdrop: gaps between individual tickets expose the map. Existing overall bounds and blocking remain, preserving touch protection.','Complete portrait region grows 76x99 to 80x116. Image remains ScaleToFit, no crop/upscaled historical screenshot.','Short name pennants and light tickets derive from visible 2021 map references; no invented insignia or unit icon.','All three generals, slot/level, live strength/status, reinforcement and formation/recall remain visible.','Inherited header/quest/navigation designs are context from prior iterations, not new work or current runtime screenshots.']},null,2));
console.log('Army editable sources ready for actual Inkscape export.');
const spec=JSON.parse(read(path.join(root,'design-spec.json')));
spec.components.push('ui.youzhou.army_command_hover');
spec.commandStates={normal:'army_command',hover:'army_command_hover',disabledRgbMultiplier:0.55,disabledAction:'recall while returning',disabledClick:false};
save('design-spec.json',JSON.stringify(spec,null,2));
