// Editable design documents. Raster output is produced only by Export-UIDesign / Inkscape.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const root=process.cwd(), dir=path.join(root,'Docs/DesignUI/s1-20261029-010252300');
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const original='F:/工作项目/率土之滨/rag/assets/cd/cda6606c2a9244c8751d1a0bc91c6c6c33086502298efc45e857eb54b41e4385.png';
const book='Assets/EasternDynasty/Resources/YouzhouUI/nav_book.png',ring='Assets/EasternDynasty/Resources/YouzhouUI/dossier_skill_ring.png';
const baselines=['youzhou-1440x900','mobile-1600x720'].map(s=>'F:/EasternDynastyBuild/Releases/s1-20261028-225251901/Project/Validation/Visual/s1-20261028-225251901-'+s+'/smoke-card-skill.png');
const im=(p,x,y,w,h)=>`<image x="${x}" y="${y}" width="${w}" height="${h}" preserveAspectRatio="xMidYMid meet" xlink:href="data:image/png;base64,${fs.readFileSync(p).toString('base64')}"/>`;
const layer=(id,content,extra='')=>`<g inkscape:groupmode="layer" inkscape:label="${id}" id="${id}" ${extra}>${content}</g>`;
const rect=(x,y,w,h,fill)=>`<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="${fill}"/>`;
const text=(x,y,s,size=18,c='#eee9dc',anchor='start')=>`<text x="${x}" y="${y}" fill="${c}" font-family="Microsoft YaHei" font-size="${size}" text-anchor="${anchor}">${s}</text>`;
const svg=(w,h,s)=>`<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${s}</svg>`;
// Border vocabulary follows the inspected historical skill sheet: rectangular
// dark field, thin inner rule, warm title band. No historical image pixels.
const surface=(w,h)=>rect(0,0,w,h,'#191d20')+`<path d="M1 1 H${w-1} V${h-1} H1 Z" fill="none" stroke="#817766" stroke-width="2"/>`+rect(5,5,w-10,34,'#292a2b')+`<path d="M5 42 H${w-5}" stroke="#817766" fill="none"/>`;
const learnedSurface=(w,h)=>rect(0,0,w,h,'#191d20')+`<path d="M0 1 H${w} M0 ${h-1} H${w}" stroke="#635e52" fill="none"/>`;
const paragraph='自身受到攻击伤害后，有60.0%几率对敌军单体发动一次攻击（伤害率100.0%），自身受到策略攻击伤害后，有40.0%几率对敌军群体发动一次攻击（伤害率80.0%）；当自身兵力首次低于初始兵力的90%、80%、70%和60%时，造成的攻击伤害提高5.0%（受攻击属性影响），受到恢复效果提高5.0%（受防御属性影响），此效果最多叠加4次；';
const lines=[];let line='',units=0;
for(const c of paragraph){const n=c.charCodeAt(0)>255?1:.56;if(units+n>23.5){lines.push(line);line='';units=0;}line+=c;units+=n;}if(line)lines.push(line);
// Runtime uses exact font measurement, these editable lines are a design example.
function sheet(prefix='',nativeLines=lines,learned='请从已拥有卡查看'){
 const h=Math.max(240,104+nativeLines.length*24),ly=h+18,lh=84;
 const surfaces=`<g transform="translate(8 4)">${surface(620,h)}</g><g transform="translate(8 ${ly})">${learnedSurface(620,lh)}</g>`;
 const images=im(ring,26,78,96,96)+im(book,41,108,66,36);
 const labels=text(318,32,'勇挚刚毅',24,'#d3bc83','middle')+text(74,202,'自带战法',16,'#c2b597','middle')+text(160,76,'战法效果',16,'#d3bc83')+nativeLines.map((s,i)=>text(160,108+i*24,s,18)).join('')+text(26,ly+36,'学习战法',17,'#c2b597')+text(160,ly+36,learned,18)+text(12,ly+lh+36,'当前展示资料效果；逐条战法结算与拆解尚未开放。',16,'#b9b3a5');
 return layer(prefix+'surfaces',surfaces)+layer(prefix+'images',images)+layer(prefix+'live-text',labels)+layer(prefix+'state','')+layer(prefix+'hit-regions',`<rect x="0" y="0" width="636" height="500" fill="none" stroke="#00ffff"/>`,'style="display:none"');
}
for(const [i,name] of ['desktop','mobile'].entries()){
 fs.mkdirSync(path.join(dir,name),{recursive:true});
 const [w,h,x,y,s]=i===0?[1440,900,678,182,1]:[1600,720,772.8293,142.4488,684/820];
 const body=layer('reference-original',im(original,0,0,572,321),'style="display:none" sodipodi:insensitive="true"')+layer('baseline-context',im(baselines[i],0,0,w,h),'sodipodi:insensitive="true"')+`<g transform="translate(${x} ${y}) scale(${s})">${rect(0,0,658,i===0?594:566,'#1f2423')}${sheet()}</g>`;
 fs.writeFileSync(path.join(dir,name,'skill-sheet.svg'),svg(w,h,body));
}
fs.mkdirSync(path.join(dir,'components'),{recursive:true});
fs.writeFileSync(path.join(dir,'components/skill-sheet.svg'),svg(1240,700,`<g id="skill-sheet" transform="scale(2)">${surface(620,240)}</g><g id="skill-learning" transform="translate(0 500) scale(2)">${learnedSurface(620,84)}</g>`));
fs.mkdirSync(path.join(dir,'states'),{recursive:true});
fs.writeFileSync(path.join(dir,'states/skill-sheet.svg'),svg(1320,1100,rect(0,0,1320,1100,'#1f2423')+`<g transform="translate(10 10)">${sheet('preview-')}</g><g transform="translate(670 10)">${sheet('owned-',lines,'尚未学习战法')}</g><g transform="translate(10 550)">${sheet('missing-',['战法说明尚未收录。'],'请从已拥有卡查看')}</g>`));
fs.writeFileSync(path.join(dir,'design-spec.json'),JSON.stringify({version:'s1-20261029-010252300',status:'design-awaiting-export',reference:{id:'Q07-JA-A003',path:original,sha256:sha(original),size:[572,321],scope:'Historical skill popup, screenshot/client/season unknown; use composition only. No official skill parameters applied.'},baselines:baselines.map(p=>({path:p,sha256:sha(p),version:'s1-20261028-225251901'})),layout:{width:636,nativeY:88,nativeX:160,nativeWidth:'width-184',sheetHeight:'max(240,nativeTextHeight+104)',icon:[26,78,96,96],book:[41,108,66,36],learningY:'sheetHeight+18',learningHeight:'max(84,learnedTextHeight+44)'},components:['ui.youzhou.skill_sheet','ui.youzhou.skill_learning'],reusedAssets:[book,ring].map(p=>({path:p,sha256:sha(p)})),notes:['Left icon and right effect text share one sheet; same column continues in the learning row.','Book is existing project illustration, not the official skill art. No invented types, trigger rates or upgrades.','Full description remains live wrapped text and retains measured scroll bounds.','Skill title/description are a design example; actual runtime values remain unchanged.']},null,2)+'\n');
