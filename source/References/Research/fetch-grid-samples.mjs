import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.join(path.dirname(fileURLToPath(import.meta.url)),'Grid-2026-10-25');
await fs.mkdir(root,{recursive:true});
const requests=[
['official-resource-grid-2023.png','https://r.res.easebar.com/pic/20230721/305d6ef6-3bdd-45e5-83c5-8474bb036cea.png','https://www.daisangoku.com/news/update/20230721/31220_1100067.html','2023-07-21'],
['official-land-management-2023.png','https://r.res.easebar.com/pic/20230721/a8d1c67a-aa03-4bf1-891a-2f3d826f4f39.png','https://www.daisangoku.com/news/update/20230721/31220_1100067.html','2023-07-21'],
['official-cultivation-land-2025.png','https://r.res.easebar.com/pic/20250220/c21cfd03-e73e-443a-b384-980c0ed4def2.png','https://www.daisangoku.com/news/update/20250220/31220_1213285.html','2025-02-20'],
['official-selected-land-2025.png','https://r.res.easebar.com/pic/20250220/015ca831-f59c-4514-a999-4bca87c38fb1.png','https://www.daisangoku.com/news/update/20250220/31220_1213285.html','2025-02-20']
];
const rows=await Promise.all(requests.map(async([name,url,sourcePage,published])=>{
 const response=await fetch(url,{signal:AbortSignal.timeout(25000)});
 if(!response.ok||!response.headers.get('content-type')?.startsWith('image/'))throw new Error('Invalid image '+url);
 const bytes=Buffer.from(await response.arrayBuffer());
 await fs.writeFile(path.join(root,name),bytes);
 return{name,url,sourcePage,published,retrieved:new Date().toISOString(),bytes:bytes.length,width:bytes.readUInt32BE(16),height:bytes.readUInt32BE(20),use:'Reference-only image; not a game asset'};
}));
await fs.writeFile(path.join(root,'sources.json'),JSON.stringify(rows,null,2));
for(const row of rows)console.log(JSON.stringify(row));
