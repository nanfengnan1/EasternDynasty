// Editable composition only; Inkscape performs every raster export.
import fs from 'node:fs';
import path from 'node:path';
const version='s1-20261027-170251762',root=path.resolve('Docs/DesignUI',version);
const manifest=JSON.parse(fs.readFileSync('Assets/EasternDynasty/Resources/asset-manifest.json','utf8').replace(/^\uFEFF/,''));
const data=p=>'data:image/'+(p.endsWith('.jpg')?'jpeg':'png')+';base64,'+fs.readFileSync(p).toString('base64');
const asset=id=>data('Assets/EasternDynasty/Resources/'+manifest.entries.find(x=>x.id===id).resourcePath+'.png');
const save=(p,s)=>{fs.mkdirSync(path.dirname(path.join(root,p)),{recursive:true});fs.writeFileSync(path.join(root,p),s);};
const layer=(id,title,body,extra='')=>`<g id="${id}" inkscape:groupmode="layer" inkscape:label="${title}" ${extra}>${body}</g>`;
const type=(x,y,t,size=14,color='#eee6cb',extra='')=>`<text x="${x}" y="${y}" font-family="Microsoft YaHei,Noto Sans SC" font-size="${size}" fill="${color}" ${extra}>${t}</text>`;
const defs='<linearGradient id="bronze" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#d0b573"/><stop offset=".5" stop-color="#766444"/><stop offset="1" stop-color="#b8a16c"/></linearGradient><linearGradient id="ink" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#303b32" stop-opacity=".94"/><stop offset="1" stop-color="#121d1b" stop-opacity=".98"/></linearGradient><linearGradient id="fade"><stop stop-color="#19231f" stop-opacity=".96"/><stop offset=".85" stop-color="#19231f" stop-opacity=".9"/><stop offset="1" stop-color="#19231f" stop-opacity=".25"/></linearGradient>';
// 0428/0431: dark card, bronze edging and a curved troop plinth. Full vertical paintings are a project adaptation.
const card='<g id="city-card"><path d="M8 0H120L128 8V207Q64 231 0 207V8Z" fill="url(#bronze)"/><path d="M8 2H120L126 8V205Q64 227 2 205V8Z" fill="url(#ink)"/><path d="M4 176H124V203Q64 222 4 203Z" fill="#18241f"/><path d="M8 176H120M8 2H120" fill="none" stroke="#c8af70"/></g>';
const hover=card.replace('city-card','city-card-hover').replace('fill="url(#bronze)"','fill="#eedba6"').replace('#c8af70','#eedba6');
const strip='<g id="city-strip"><path d="M0 0H380L378 44H0Z" fill="url(#fade)"/><path d="M0 43.5H368" stroke="#9b8859" stroke-opacity=".7"/></g>';
const command='<g id="city-command"><path d="M0 0H120L126 6V44H6L0 38Z" fill="url(#bronze)"/><path d="M1 1H119L125 7V43H7L1 37Z" fill="#24312a"/></g>';
const commandHover=command.replace('city-command','city-command-hover').replace('#24312a','#465041');
let sharedArtwork='';
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}${sharedArtwork}</defs>${b}</svg>`;
save('components/city-army.svg',svg(700,250,layer('components','独立卡面 / 悬停 / 信息底托 / 命令正常与悬停',card+`<g transform="translate(144)">${hover}</g><g transform="translate(300)">${strip}</g><g transform="translate(300 66)">${command}</g><g transform="translate(446 66)">${commandHover}</g>`)));
const images=['s1_100039','s1_100744','s1_100746'],names=['皇甫嵩','王子服','郑泰'];
const existingArmyBaseline=data('F:/EasternDynastyBuild/Releases/s1-20261027-104752269/Project/Validation/Visual/s1-20261027-104752269-youzhou-1440x900/smoke-city-developed.png');
sharedArtwork=`<image id="existing-command-art" width="1440" height="900" href="${existingArmyBaseline}"/>`;
function army(w,available,state='idle'){
 const cw=(w-32)/3,ch=Math.min(available-190,(cw-8)*1.45+48),bodyH=ch+190;
 let surfaces=`<g transform="translate(8 0) scale(${(w-16)/380} .73)">${strip}</g>`,pictures='',labels=type(16,22,'壹  第一部队',16)+type(w-14,22,state==='away'?'部队行军中':'驻城待命',13,'#a9c4a2','text-anchor="end"'),hits='';
 for(let i=0;i<3;i++){
  const x=8+i*(cw+8),y=52,empty=state==='empty';
  surfaces+=`<g transform="translate(${x} ${y}) scale(${cw/128} ${ch/219})">${(state==='hover'&&i===0?hover:card).replaceAll('id="city-card','id="slot-'+i+'-city-card')}</g>`;
  if(!empty)pictures+=`<image x="${x+4}" y="${y+4}" width="${cw-8}" height="${ch-48}" preserveAspectRatio="xMidYMid meet" href="${asset('portrait.'+images[i])}"/>`;
  else labels+=type(x+cw/2,y+ch*.44,'＋',30,'#a79970','text-anchor="middle"');
  labels+=type(x+cw/2,47,['前锋','中军','大营'][i],13,'#cbb47c','text-anchor="middle"')+type(x+cw/2,y+ch-24,empty?'待任命':names[i],16,'#eee6cb','text-anchor="middle"')+(!empty?type(x+cw/2,y+ch-6,'Lv.1',13,'#cbb47c','text-anchor="middle"'):'');
  hits+=`<rect x="${x}" y="${y}" width="${cw}" height="${ch}"/>`;
 }
 const sy=ch+58,py=ch+92,ay=ch+140,aw=(w-28)/3;
 surfaces+=`<g transform="translate(8 ${sy}) scale(${(w-16)/380} ${74/44})">${strip.replace('city-strip','supply-strip')}</g>`;
 labels+=type(16,sy+20,'兵力',13)+type(60,sy+21,state==='empty'?'0 / 0':'450 / 960',21)+type(16,py+15,state==='away'?'执行军令 · 返城后可整编':state==='empty'?'未编队 · 先任命武将':'补兵 +200 · 项目即时征兵',13);
 if(state==='away')labels+=type(47,py+35,'当前阶段 00:25',13);
 else for(const [i,crop,value] of [[0,[722,9,41,31],200],[1,[202,9,38,30],80],[2,[374,9,38,31],20]]){
  const ix=8+i*(w-16)/3;
  const rs=Math.min(22/crop[2],20/crop[3]),rx=ix+(22-crop[2]*rs)/2-crop[0]*rs,ry=py+20+(20-crop[3]*rs)/2-crop[1]*rs;
  pictures+=`<defs><clipPath id="resource-crop-${i}" clipPathUnits="userSpaceOnUse"><rect x="${ix}" y="${py+20}" width="22" height="20"/></clipPath></defs><g clip-path="url(#resource-crop-${i})"><image x="${rx}" y="${ry}" width="${894*rs}" height="${74*rs}" href="${asset('reference.youzhou.resource_hud')}"/></g>`;
  labels+=type(ix+27,py+35,state==='empty'?'0':String(value),13);
 }
 let progress=`<rect x="8" y="${sy+25}" width="${w-16}" height="3" fill="#4c5548"/>`+(state==='empty'?'':`<rect x="8" y="${sy+25}" width="${(w-16)*450/960}" height="3" fill="#93aa7a"/>`);
 for(let i=0;i<3;i++){
  const x=8+i*(aw+6);
  surfaces+=`<g transform="translate(${x} ${ay}) scale(${aw/126} 1)">${command.replace('city-command','action-'+i)}</g>`;
  // Locked baseline crops document the unchanged runtime icons. These are not production slices.
  pictures+=`<defs><clipPath id="command-crop-${i}" clipPathUnits="userSpaceOnUse"><rect x="${x+5}" y="${ay+6}" width="32" height="32"/></clipPath></defs><g clip-path="url(#command-crop-${i})"><use href="#existing-command-art" transform="translate(${x+5-1029-i*128} ${ay+6-758})"/></g>`;
  labels+=type(x+42+(aw-45)/2,ay+28,['编队','征兵','练兵'][i],16,state==='away'&&i===0?'#8b9187':'#eee6cb','text-anchor="middle"');
  hits+=`<rect x="${x}" y="${ay}" width="${aw}" height="44"/>`;
 }
 return layer('army-surfaces','卡面、独立状态底托与命令',surfaces)+layer('army-images','完整正式武将图片 / 操作图像占位',pictures)+layer('army-live-labels','动态姓名、席位、等级与真实费用示例',labels)+layer('army-state','兵力与禁用状态示例',progress)+layer('army-hit-regions','实际命中范围-隐藏',hits,'style="display:none" fill="none" stroke="#ff00ff"');
}
const reference='F:/工作项目/率土之滨/rag/assets/d8/d8a56f2b7e2504fa7319cfa8f022d5cb0ca0168e3a7ac013f6883da1f848a350.jpg';
for(const [target,w,h,rail,available,x,y,s,folder] of [['desktop',1440,900,406,490,1010,312,1,'youzhou-1440x900'],['mobile',1600,720,342,410,1255.16,272.52,.835,'mobile-1600x720']]){
 const baseline=`F:/EasternDynastyBuild/Releases/s1-20261027-104752269/Project/Validation/Visual/s1-20261027-104752269-${folder}/smoke-city-developed.png`;
 const background=layer('reference-original','2015原作城内-锁定隐藏',`<image width="500" height="280" href="${data(reference)}"/>`,'style="display:none" sodipodi:insensitive="true"')+layer('baseline-context','104752269运行基线，仅作为本轮设计上下文',`<image width="${w}" height="${h}" href="${data(baseline)}"/><rect x="${x}" y="${y}" width="${rail*s}" height="${available*s}" fill="#111e29"/>`,'sodipodi:insensitive="true"');
 save(`${target}/city-army.svg`,svg(w,h,background+`<g transform="translate(${x} ${y}) scale(${s})">${army(rail,available)}</g>`));
}
save('states/city-army.svg',svg(1420,530,'<rect width="1420" height="530" fill="#111e29"/>'+['idle','empty','away','hover'].map((s,i)=>`<g transform="translate(${i*352+6} 30)">${type(8,-8,['驻城','空编队','行军-编队禁用','画像悬停'][i],16)}${army(342,410,s).replaceAll('id="','id="'+s+'-').replaceAll('url(#resource-crop-','url(#'+s+'-resource-crop-').replaceAll('url(#command-crop-','url(#'+s+'-command-crop-')}</g>`).join('')));
save('design-spec.json',JSON.stringify({version,status:'editable-source-awaiting-export-review',reference:{id:'STZB-0428',support:'STZB-0431',year:2015,scope:'Historical city page and army-card crop; current S1 appearance unknown. Dark independent cards, bronze edges, curved plinth. Three full vertical paintings are an explicit project adaptation.'},baselineVersion:'s1-20261027-104752269',layout:{inset:8,gap:8,cardTop:52,cardWidth:'(width-32)/3',cardHeight:'min(availableHeight-190,(cardWidth-8)*1.45+48)',portraitInset:[4,4,8,48],identityHeight:42,strengthAfterCard:6,supplyGap:6,commandGap:8,touchHeight:44},components:['city_army_card','city_army_card_hover','city_army_strip','city_army_command','city_army_command_hover'],fonts:{name:16,level:13,strength:21,state:13},runtime:'CityOverviewPresentation.DrawCityOverviewArmy / CityArmyLayout',notes:['No full-rail black panel; compact art proportions remove empty mats.','Panorama, affairs and all gameplay remain as before. No historical portrait crop imported.','Numbers are runtime text; existing facility/formation icons retained. Preview city icons are explicitly composition placeholders.','Desktop/mobile previews use old screenshots for surrounding context, not new game output.']},null,2));
console.log('Layered city army design sources ready.');
