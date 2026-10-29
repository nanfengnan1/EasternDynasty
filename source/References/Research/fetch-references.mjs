import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.dirname(fileURLToPath(import.meta.url));
const requests = [
  ['map-mountains-a.png', 'https://pic.gamedreamer.com/66a1a96b48425930cb3dac9529a3b9bc/picture/e0b3799ff1681d08fbdad88980e209e8.png'],
  ['map-mountains-b.png', 'https://pic.gamedreamer.com/66a1a96b48425930cb3dac9529a3b9bc/picture/4b78f81f6cb8a3c4b7daec8dfeb4a61e.png'],
  ['map-river.png', 'https://pic.gamedreamer.com/66a1a96b48425930cb3dac9529a3b9bc/picture/e590b00363df0f080d966da4fd47a680.png'],
  ['map-strategic.png', 'https://pic.gamedreamer.com/66a1a96b48425930cb3dac9529a3b9bc/picture/245e399576a0385fedcc10229a3cafcb.png'],
  ['atlas-card-100006.jpg', 'https://images.gamedreamer.com/stzb/20160415/images/data/small/card_100006.jpg']
];
const manifest = await Promise.all(requests.map(async ([name, url]) => {
  try {
    const response = await fetch(url, { signal: AbortSignal.timeout(20000), headers: { 'User-Agent': 'Mozilla/5.0', Referer: 'https://stzb.gamedreamer.com.tw/' } });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    if (!response.headers.get('content-type')?.startsWith('image/')) throw new Error('Not an image');
    const data = new Uint8Array(await response.arrayBuffer());
    await fs.writeFile(path.join(root, name), data);
    return { name, url, bytes: data.length, retrieved: new Date().toISOString(), use: 'Visual analysis reference only; never ship as game artwork.' };
  } catch (error) { return { name, url, error: error.message }; }
}));
const manifestPath = path.join(root, 'image-sources.json');
let previous = [];
try { previous = JSON.parse(await fs.readFile(manifestPath, 'utf8')); }
catch (error) { if (error.code !== 'ENOENT') throw error; }
const names = new Set(manifest.map(entry=>entry.name));
await fs.writeFile(manifestPath, JSON.stringify([...previous.filter(entry=>!names.has(entry.name)), ...manifest], null, 2));
for (const entry of manifest) console.log(JSON.stringify(entry));
