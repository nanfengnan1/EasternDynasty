/** Explicit-URL, finite research collector. Node >= 20, built-ins only. */
import fs from 'node:fs/promises';
import {unlinkSync} from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';

const root = path.dirname(fileURLToPath(import.meta.url));
const lockPath = path.join(root, '.collector.lock');
const lock = await fs.open(lockPath, 'wx').catch(e => { throw new Error('Collector lock exists. Run only one collector at a time; remove the lock only after verifying no collector is running. ' + e.message); });
await lock.writeFile(String(process.pid));
await lock.close();
process.on('exit', () => { try { unlinkSync(lockPath); } catch {} });
const project = path.resolve(root, '../..');
const imageRoot = path.join(project, 'References/UI-Modeling');
const args = process.argv.slice(2);
const batch = args.find(a => a.startsWith('--batch='))?.split('=')[1];
const inspect = args.includes('--inspect');
const manifest = JSON.parse(await fs.readFile(path.join(root, 'sources.json'), 'utf8'));
let reviews = {};
try { reviews = JSON.parse(await fs.readFile(path.join(root, 'review-records.json'), 'utf8')); } catch (e) { if (e.code !== 'ENOENT') throw e; }
const indexPath = path.join(root, 'source-index.json');
let index = [];
try { index = JSON.parse(await fs.readFile(indexPath, 'utf8')); } catch (e) { if (e.code !== 'ENOENT') throw e; }
await fs.mkdir(path.join(root, 'metadata'), {recursive: true});
await fs.mkdir(path.join(root, 'runs'), {recursive: true});
await fs.mkdir(imageRoot, {recursive: true});
const startedAt = new Date().toISOString();
const results = [];
const decode = s => s.replace(/&amp;/g, '&').replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&nbsp;/g, ' ').replace(/&quot;/g, '"').replace(/&#39;/g, "'");
const plain = s => decode(s.replace(/<script\b[^>]*>[\s\S]*?<\/script>/gi, '').replace(/<style\b[^>]*>[\s\S]*?<\/style>/gi, '').replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ')).trim();
const relative = p => path.relative(project, p).replaceAll('\\', '/');
const writeJson = (p, v) => fs.writeFile(p, JSON.stringify(v, null, 2) + '\n', 'utf8');

async function retrieve(url) {
  let current = url;
  for (let n = 0; n < 5; n++) {
    const u = new URL(current);
    if (!['https:', 'http:'].includes(u.protocol)) throw new Error('Unsupported URL protocol');
    if (!manifest.allowed_hosts.includes(u.hostname)) throw new Error('Host is outside explicit allowlist: ' + u.hostname);
    const response = await fetch(current, {redirect: 'manual', signal: AbortSignal.timeout(20000), headers: {'User-Agent': 'EasternDynasty-Research/1.0 (limited explicit URL collector)', Accept: '*/*'}});
    if ([301, 302, 303, 307, 308].includes(response.status)) {
      current = new URL(response.headers.get('location'), current).href;
      await response.body?.cancel();
      continue;
    }
    if (!response.ok) { await response.body?.cancel(); throw new Error('HTTP ' + response.status + ' (no retry or access bypass)'); }
    const limit = 8 * 1024 * 1024;
    if (Number(response.headers.get('content-length')) > limit) { await response.body?.cancel(); throw new Error('Response exceeds 8 MiB limit'); }
    const chunks = [];
    let size = 0;
    for await (const chunk of response.body) {
      size += chunk.length;
      if (size > limit) throw new Error('Response exceeds 8 MiB limit');
      chunks.push(chunk);
    }
    return {bytes: Buffer.concat(chunks), final_url: current, content_type: response.headers.get('content-type') || '', http_status: response.status};
  }
  throw new Error('Redirect limit reached');
}

for (const source of manifest.sources.filter(s => !batch || String(s.batch) === batch)) {
  if (!/^[a-z0-9][a-z0-9-]*$/.test(source.id)) throw new Error('Invalid source id');
  const accessedAt = new Date().toISOString();
  try {
    const response = await retrieve(source.url);
    const hash = createHash('sha256').update(response.bytes).digest('hex');
    const old = index.find(s => s.id === source.id);
    let destination;
    let extractedTitle = source.title;
    if (source.type === 'image') {
      if (!response.content_type.startsWith('image/')) throw new Error('Expected image, got ' + response.content_type);
      const ext = response.content_type.includes('png') ? '.png' : response.content_type.includes('webp') ? '.webp' : response.content_type.includes('jpeg') ? '.jpg' : '.bin';
      const existing = index.find(s => s.sha256 === hash && s.type === 'image' && s.local_path);
      destination = existing ? path.resolve(project, existing.local_path) : path.join(imageRoot, `${source.id}-${hash.slice(0, 12)}${ext}`);
      if (!existing) await fs.writeFile(destination, response.bytes);
    } else {
      const html = response.bytes.toString('utf8');
      extractedTitle = plain(html.match(/<title[^>]*>([\s\S]*?)<\/title>/i)?.[1] || source.title);
      const headings = [...html.matchAll(/<h[1-3]\b[^>]*>([\s\S]*?)<\/h[1-3]>/gi)].slice(0, 24).map(m => plain(m[1]));
      const imageLinks = [...html.matchAll(/<img\b[^>]*src=["']([^"']+)["'][^>]*>/gi)].map(m => new URL(decode(m[1]), response.final_url).href).filter(u => !/logo|favicon|social|icon|badge/i.test(u)).slice(0, 15);
      destination = path.join(root, 'metadata', source.id + '.json');
      await writeJson(destination, {title: extractedTitle, source_url: source.url, final_url: response.final_url, accessed_at: accessedAt, sha256: hash, byte_length: response.bytes.length, headings, image_links: imageLinks, storage_note: 'Stores bibliographic metadata and image URLs only; full article body is not saved.'});
      if (inspect) {
        const main = html.match(/<article\b[^>]*>([\s\S]*?)<\/article>/i)?.[1] || html.split(/<h1\b[^>]*>/i).slice(1).join(' ') || html;
        console.log('\nREVIEW ' + source.id + '\n' + plain(main).slice(0, 11500) + '\nIMAGES ' + JSON.stringify(imageLinks));
      }
    }
    const item = {...old, id: source.id, title: source.title, extracted_title: extractedTitle, source_url: source.url, final_url: response.final_url, accessed_at: accessedAt, type: source.type, topic: source.topic, batch: source.batch, target_version: source.target_version, actual_review_status: old?.actual_review_status || 'downloaded_pending_review', local_path: relative(destination), usage_scope: source.usage_scope || 'Research metadata and own analysis only; source copyright remains with its owner. Not a distributable game asset.', sha256: hash, byte_length: response.bytes.length, fetch_status: 'success', change_status: old?.sha256 === hash ? 'unchanged' : old ? 'changed' : 'new'};
    if (reviews[source.id]?.reviewed_sha256 === hash) Object.assign(item, reviews[source.id]);
    else if (old?.sha256 && old.sha256 !== hash) item.actual_review_status = 'changed_pending_review';
    index = [...index.filter(s => s.id !== source.id), item];
    results.push({id: source.id, status: 'success', change: item.change_status, bytes: response.bytes.length, path: item.local_path});
    console.log(source.id + ': ' + item.change_status + ' (' + response.bytes.length + ' bytes)');
    await writeJson(indexPath, index);
  } catch (error) {
    const failure = {id: source.id, source_url: source.url, accessed_at: accessedAt, error: error.message};
    await fs.appendFile(path.join(root, 'failures.jsonl'), JSON.stringify(failure) + '\n');
    const old = index.find(s => s.id === source.id);
    const failedItem = {...old, id: source.id, title: source.title, source_url: source.url, accessed_at: accessedAt, type: source.type, topic: source.topic, batch: source.batch, target_version: source.target_version, actual_review_status: old?.actual_review_status || 'not_reviewed_fetch_failed', local_path: old?.local_path || null, usage_scope: source.usage_scope || 'Research metadata and own analysis only; source copyright remains with its owner.', fetch_status: 'failed', last_error: error.message};
    index = [...index.filter(s => s.id !== source.id), failedItem];
    await writeJson(indexPath, index);
    results.push({...failure, status: 'failed'});
    console.error(source.id + ': ' + error.message);
  }
  await new Promise(resolve => setTimeout(resolve, 1200));
}
const report = {started_at: startedAt, finished_at: new Date().toISOString(), batch: batch || 'all', finite_run: true, recurring_schedule_created: false, results};
const reportPath = path.join(root, 'runs', startedAt.replaceAll(':', '-') + '.json');
await writeJson(reportPath, report);
console.log('Report: ' + relative(reportPath));
if (results.some(r => r.status === 'failed')) process.exitCode = 1;
