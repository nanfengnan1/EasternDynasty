import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const root = path.dirname(fileURLToPath(import.meta.url));
const requests = [
  ['portrait-xunyu.jpg', 'https://images.gamedreamer.com/stzb/20220721/images/jzzl/card_100024.jpg'],
  ['portrait-huangfusong.jpg', 'https://images.gamedreamer.com/stzb/20220721/images/jzzl/card_100039.jpg'],
  ['portrait-kebineng.jpg', 'https://images.gamedreamer.com/stzb/20220721/images/jzzl/card_100550.jpg'],
  ['official-feature-0.jpg','https://www.daisangoku.com/pc/gw/20210507115529/img/0_d6ccf2e.jpg'],
  ['official-feature-1.jpg','https://www.daisangoku.com/pc/gw/20210507115529/img/1_c3c2f69.jpg'],
  ['official-feature-2.jpg','https://www.daisangoku.com/pc/gw/20210507115529/img/2_6a2f5bb.jpg'],
  ['recruitment-pool-2022.png','https://r.res.easebar.com/pic/20221024/315e97f4-9dcc-4499-8dee-c24066c2f703.png'],
  ['recruitment-cards-2022.png','https://r.res.easebar.com/pic/20221024/91af497f-8503-4cba-a996-9656b78da31d.png']
];
const manifestPath = path.join(root,'image-sources.json');
let manifest = JSON.parse(await fs.readFile(manifestPath,'utf8'));
for (const [name,url] of requests) {
  const response = await fetch(url,{signal:AbortSignal.timeout(20000)});
  if (!response.ok || !response.headers.get('content-type')?.startsWith('image/')) throw new Error('Invalid image response: '+url);
  const data = new Uint8Array(await response.arrayBuffer());
  await fs.writeFile(path.join(root,name),data);
  manifest = manifest.filter(item=>item.name!==name);
  manifest.push({name,url,bytes:data.length,retrieved:new Date().toISOString(),use:'Visual analysis reference only; never ship as game artwork.'});
  console.log(name+' '+data.length+' bytes');
}
await fs.writeFile(manifestPath,JSON.stringify(manifest,null,2));
