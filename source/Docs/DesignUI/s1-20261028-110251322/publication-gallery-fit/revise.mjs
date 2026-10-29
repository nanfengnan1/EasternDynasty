import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const root=path.resolve('Docs/DesignUI/s1-20261028-110251322/publication-gallery-fit');
const inherited=path.resolve('Docs/DesignUI/s1-20261028-012751756');
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const reference='F:/工作项目/率土之滨/rag/assets/08/08627a10e55883e80f67fb768ac95327aae33b4610dbdb45858ec548b930b5c9.png';
const baseline='F:/EasternDynastyBuild/Releases/s1-20261028-095751175/Project/Validation/Visual/s1-20261028-095751175-youzhou-1440x900/smoke-facility-locked.png';
for(const target of ['desktop','mobile']) {
  const source=path.join(inherited,target,'gallery.svg');
  let svg=fs.readFileSync(source,'utf8');
  const start=svg.lastIndexOf('<g id="gallery-surfaces"');
  const imageStart=svg.indexOf('<g id="gallery-images"',start);
  if(start<0||imageStart<0)throw new Error('Missing editable gallery layers');
  const surfaces=svg.slice(start,imageStart)
    .replaceAll('height="96"','height="100"')
    .replaceAll('height="94"','height="98"')
    .replaceAll('height="36"','height="40"');
  svg=svg.slice(0,start)+surfaces+svg.slice(imageStart);
  const labels=svg.lastIndexOf('<g id="gallery-live-labels"');
  svg=svg.slice(0,labels)+svg.slice(labels).replaceAll('y="176"','y="178"').replaceAll('height="96"','height="100"');
  // All inherited illustration assets, textures, name cells and 108-unit row spacing remain unchanged.
  fs.mkdirSync(path.join(root,target),{recursive:true});
  fs.writeFileSync(path.join(root,target,'gallery.svg'),svg);
}
const font='Assets/EasternDynasty/Resources/Fonts/CJK.otf',b=fs.readFileSync(font),tables={};
for(let i=0;i<b.readUInt16BE(4);i++){const offset=12+i*16;tables[b.toString('ascii',offset,offset+4)]=b.readUInt32BE(offset+8);}
const unitsPerEm=b.readUInt16BE(tables.head+18),ascender=b.readInt16BE(tables.hhea+4),descender=b.readInt16BE(tables.hhea+6),lineGap=b.readInt16BE(tables.hhea+8);
const line12=(ascender-descender+lineGap)*12/unitsPerEm;
const spec={status:'design-amendment-awaiting-export',purpose:'Publication mobile font-fit correction; not a new visual-redesign claim',reference:{id:'Q09-CU-A001',path:reference,sha256:hash(reference),resolution:[800,450],publishedAt:'2024-05-15',s1Identity:'unknown; layout research only'},baseline:{path:baseline,sha256:hash(baseline),version:'s1-20261028-095751175',scope:'Actual desktop runtime; mobile run aborted before screenshot export'},inheritedDesign:inherited,scope:'Only eight facility selection tiles; no new art or rules. Existing 58-high complete thumbnails and 20-high facility names remain unchanged.',before:{tileHeight:96,footerHeight:36,statusY:80,statusHeight:16},after:{tileHeight:100,footerHeight:40,statusY:80,statusHeight:20},unchanged:{optionImage:[4,1,'width-8',58],name:[0,60,'width',20],rowSpacing:108,minimumStatusFont:12},font:{path:font,sha256:hash(font),unitsPerEm,ascender,descender,lineGap,lineHeightAt12:line12,oldStatusFits:line12<=16,newStatusFits:line12<=20,scope:'OpenType line metrics; actual Unity GUIStyle remains a publication gate'},notes:['Mobile preview inherits the existing editable 012 design and historical embedded backdrop; it is not a new runtime capture.','The 4-unit extension uses existing row spacing and preserves the first-screen ledger.','Current runtime uses existing selected texture scaled to 100; no resource import required.']};
fs.writeFileSync(path.join(root,'design-spec.json'),JSON.stringify(spec,null,2));
console.log(JSON.stringify({sources:['desktop/gallery.svg','mobile/gallery.svg'],font:spec.font}));
