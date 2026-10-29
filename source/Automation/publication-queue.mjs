/** Frozen inputs and a separate Unity working copy for each publication.
 * Queue records are append-only. Only the publisher changes active/job-status.
 * A named Windows mutex serializes short queue transactions across Node hosts.
 * Failed/obsolete snapshots are retained deliberately; never delete an active
 * release or a user's files in an unattended cleanup pass.
 */
import fs from 'node:fs/promises';
import {createReadStream, constants} from 'node:fs';
import path from 'node:path';
import {createHash, randomUUID} from 'node:crypto';
import {spawn} from 'node:child_process';

const requiredTrees = ['Assets', 'Packages', 'ProjectSettings'];
const optionalTrees = ['Tools', 'Automation', 'Docs'];
const rootFiles = ['AGENTS.md', 'agent.md', 'README.md'];
const explicitFixtures = ['Validation/CardDisplayChecks/ExportCardDisplay.cs'];
const terminal = new Set(['published', 'failed', 'superseded']);
const fold = s => process.platform === 'win32' ? s.toLowerCase() : s;
const sha = b => createHash('sha256').update(b).digest('hex');
const json = value => JSON.stringify(value, null, 2) + '\n';
const iso = () => new Date().toISOString();
const fail = message => { throw new Error('PUBLICATION_QUEUE: ' + message); };
const exists = async file => fs.lstat(file).then(() => true, e => { if (e.code === 'ENOENT') return false; throw e; });
const inside = (root, target) => fold(target).startsWith(fold(root + path.sep));
const sameStat = (a,b) => a.size === b.size && a.mtimeMs === b.mtimeMs && a.ctimeMs === b.ctimeMs && a.ino === b.ino && a.dev === b.dev;

function versionName(version) {
  if (typeof version !== 'string' || !/^[A-Za-z0-9][A-Za-z0-9._-]{0,100}$/.test(version) || /[.]$/.test(version) || /^(con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\.|$)/i.test(version)) fail('Invalid version.');
  return version;
}
function relativeName(value) {
  if (typeof value !== 'string' || /[\x00-\x1f<>:"|?*\\]/.test(value)) fail('Invalid relative path.');
  const parts = value.split('/');
  if (parts.some(p => !p || p === '.' || p === '..' || /[. ]$/.test(p))) fail('Unsafe relative path: ' + value);
  return value;
}
async function checkedDirectory(value, create = false) {
  if (typeof value !== 'string' || !path.isAbsolute(value) || /^\\\\/.test(value)) fail('An absolute local directory is required.');
  const full = path.resolve(value);
  // Check existing ancestors before mkdir so even a nonexistent child cannot
  // route a write through a directory junction.
  let probe = full;
  while (!(await exists(probe))) { const next = path.dirname(probe); if (next === probe) fail('Directory has no existing ancestor.'); probe = next; }
  if (fold(await fs.realpath(probe)) !== fold(probe) || (await fs.lstat(probe)).isSymbolicLink()) fail('Reparse-point directory: ' + probe);
  if (create) await fs.mkdir(full, {recursive:true});
  const stat = await fs.lstat(full);
  if (!stat.isDirectory() || stat.isSymbolicLink() || fold(await fs.realpath(full)) !== fold(full)) fail('Unsafe directory: ' + full);
  return full;
}
async function checkedFile(root, relative) {
  relativeName(relative);
  const full = path.join(root, ...relative.split('/'));
  if (!inside(root, full)) fail('Path escapes root.');
  const stat = await fs.lstat(full);
  if (!stat.isFile() || stat.isSymbolicLink() || fold(await fs.realpath(full)) !== fold(full)) fail('Nonregular/reparse input: ' + relative);
  return {full, stat};
}
async function hashFile(file) {
  const hash = createHash('sha256');
  for await (const bytes of createReadStream(file)) hash.update(bytes);
  return hash.digest('hex');
}
async function readJson(file, missing = null) {
  try { return JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, '')); }
  catch (error) { if (error.code === 'ENOENT') return missing; throw error; }
}
async function atomicJson(file, value) {
  const temp = file + '.' + randomUUID() + '.tmp';
  await fs.writeFile(temp, json(value), {flag:'wx'});
  await fs.rename(temp, file);
}
function excluded(relative) {
  return relative.split('/').some(name => name.startsWith('.') || /^(Library|Temp|Obj|Builds?|Logs?|Screenshots?|node_modules|UserSettings|\.git)$/i.test(name) ||
    /\.(log|tmp|bak|pem|pfx|p12|key|keystore|jks)$/i.test(name) || /^(auth|credentials?|secrets?|tokens?|service-auth)(?:[.-]|$)/i.test(name));
}
function privateMaterial(relative) {
  return relative.split('/').some(name => /^\.env(?:\.|$)/i.test(name) || /\.(pem|pfx|p12|key|keystore|jks)$/i.test(name) || /^(auth|credentials?|secrets?|tokens?|service-auth)\.(json|ya?ml|txt)$/i.test(name));
}
async function unityConfiguration(root) {
  const name = '.local/unity.json';
  if (!(await exists(path.join(root, name)))) return null;
  const {full} = await checkedFile(root, name);
  const value = await readJson(full);
  if (!value || typeof value.unityEditor !== 'string' || !path.isAbsolute(value.unityEditor) || path.basename(value.unityEditor).toLowerCase() !== 'unity.exe') fail('Invalid local Unity editor configuration.');
  const editor = await fs.lstat(value.unityEditor);
  if (!editor.isFile()) fail('Configured Unity editor does not exist.');
  const safe = {unityEditor:value.unityEditor};
  if (typeof value.unityVersion === 'string' && /^[0-9A-Za-z.]+$/.test(value.unityVersion)) safe.unityVersion = value.unityVersion;
  return Buffer.from(json(safe));
}

/** Deliberate allowlist: no previous build/evidence directories or auth files. */
async function collect(root, {all = false} = {}) {
  const output = new Map();
  async function add(relative) {
    const item = await checkedFile(root, relative);
    const key = fold(relative);
    if (output.has(key)) fail('Case-colliding source paths: ' + relative);
    output.set(key, {relative, ...item});
  }
  async function walk(relative, filter = () => true) {
    const folder = await checkedDirectory(path.join(root, ...relative.split('/')));
    for (const entry of await fs.readdir(folder, {withFileTypes:true})) {
      const rel = relative ? relative + '/' + entry.name : entry.name;
      relativeName(rel);
      const playable = requiredTrees.includes(rel.split('/')[0]);
      // Every playable descendant is part of the proof, including an Assets
      // folder named Logs/Build. Filtering those could hide executable code.
      if (!all && playable && privateMaterial(rel)) fail('Private configuration inside playable inputs: ' + rel);
      if (!all && !playable && excluded(rel)) continue;
      const absolute = path.join(root, ...rel.split('/')), stat = await fs.lstat(absolute);
      if (stat.isSymbolicLink()) fail('Reparse points are forbidden: ' + rel);
      if (stat.isDirectory()) await walk(rel, filter);
      else if (filter(rel)) await add(rel);
    }
  }
  if (all) {
    for (const entry of await fs.readdir(root, {withFileTypes:true})) {
      if (entry.isDirectory()) await walk(entry.name);
      else await add(entry.name);
    }
  } else {
    for (const tree of requiredTrees) await walk(tree);
    for (const tree of optionalTrees) if (await exists(path.join(root, tree))) await walk(tree);
    for (const name of rootFiles) if (await exists(path.join(root, name))) await add(name);
    if (await exists(path.join(root, 'AssetInbox/.asset-transaction'))) fail('Unfinished asset import transaction.');
    if (await exists(path.join(root, 'AssetInbox'))) await walk('AssetInbox', rel => /\.(json|md)$/i.test(rel));
    if (await exists(path.join(root, 'Validation'))) {
      await checkedDirectory(path.join(root, 'Validation'));
      for (const entry of await fs.readdir(path.join(root, 'Validation'), {withFileTypes:true}))
        if (!entry.isDirectory() && /\.(cs|mjs|ps1)$/i.test(entry.name) && !excluded(entry.name)) await add('Validation/' + entry.name);
      for (const fixture of explicitFixtures) if (await exists(path.join(root, fixture))) await add(fixture);
    }
    if (await exists(path.join(root, '.local/unity.json'))) await add('.local/unity.json');
  }
  return new Map([...output].sort(([a],[b]) => a.localeCompare(b)));
}
async function inventory(root, options) {
  const files = await collect(root, options);
  for (const item of files.values()) {
    item.sha256 = await hashFile(item.full);
    const after = await fs.lstat(item.full);
    if (!sameStat(item.stat, after)) fail('Input changed while hashing: ' + item.relative);
  }
  const final = await collect(root, options);
  compareInventories(files, final, false);
  return files;
}
function compareInventories(before, after, hashes = true) {
  if (before.size !== after.size) fail('Source file set changed during snapshot.');
  for (const [key,item] of before) {
    const other = after.get(key);
    if (!other || !sameStat(item.stat, other.stat) || (hashes && item.sha256 !== other.sha256)) fail('Source changed during snapshot: ' + item.relative);
  }
}
async function copyFiles(files, target, unityBytes = null) {
  for (const item of files.values()) {
    const destination = path.join(target, ...item.relative.split('/'));
    await checkedDirectory(path.dirname(destination), true);
    if (item.relative === '.local/unity.json' && unityBytes) await fs.writeFile(destination, unityBytes, {flag:'wx'});
    else await fs.copyFile(item.full, destination, constants.COPYFILE_EXCL);
  }
}
function publicFiles(files) { return [...files.values()].map(item => ({path:item.relative, size:item.stat.size, sha256:item.sha256})); }
function normalizedInputs(files, unityBytes) {
  return publicFiles(files).map(item => item.path === '.local/unity.json' ? {...item,size:unityBytes.length,sha256:sha(unityBytes)} : item);
}
function canonical(value) {
  if (Array.isArray(value)) return value.map(canonical);
  if (value && typeof value === 'object') return Object.fromEntries(Object.keys(value).sort().map(key => [key,canonical(value[key])]));
  return value;
}
async function queuePaths(sourceRoot, create = true) {
  const root = await checkedDirectory(sourceRoot);
  const base = path.join(root, '.local/publication'), queue = path.join(base,'queue'), states = path.join(base,'job-status');
  for (const directory of [base,queue,states]) if (create || await exists(directory)) await checkedDirectory(directory,create);
  return {root,base,queue,states,active:path.join(base,'active.json')};
}

async function withQueueLock(sourceRoot, action, {create = true} = {}) {
  const paths = await queuePaths(sourceRoot,create);
  if (process.platform !== 'win32') fail('This local publication queue requires Windows named mutexes.');
  const name = 'Local\\EasternDynasty-PublicationQueue-' + sha(fold(paths.root)).slice(0,24);
  const script = `$ErrorActionPreference='Stop';$m=[Threading.Mutex]::new($false,'${name}');$taken=$false;try{try{$taken=$m.WaitOne(30000)}catch [Threading.AbandonedMutexException]{$taken=$true};if(-not $taken){exit 3};[Console]::WriteLine('ACQUIRED');[Console]::Out.Flush();[void][Console]::ReadLine()}finally{if($taken){$m.ReleaseMutex()};$m.Dispose()}`;
  const child = spawn(path.join(process.env.SystemRoot || 'C:\\Windows', 'System32/WindowsPowerShell/v1.0/powershell.exe'), ['-NoProfile','-NonInteractive','-EncodedCommand',Buffer.from(script,'utf16le').toString('base64')], {windowsHide:true, stdio:['pipe','pipe','pipe']});
  let errors = '';
  child.stderr.on('data', data => { errors += data.toString(); });
  child.stdin.on('error', () => {});
  const closed = new Promise(resolve => child.once('close', resolve));
  await new Promise((resolve,reject) => {
    let stdout = '';
    child.stdout.on('data', data => { stdout += data.toString(); if (stdout.includes('ACQUIRED')) resolve(); });
    child.once('error', reject);
    child.once('close', code => reject(new Error('PUBLICATION_QUEUE: Queue lease failed (' + code + '): ' + errors)));
  });
  try { return await action(paths); }
  finally { child.stdin.end('\n'); await closed; }
}

function safeMetadata(value) {
  const text = JSON.stringify(value ?? {});
  if (text.length > 262144) fail('Publication metadata is too large.');
  const clean = JSON.parse(text);
  function check(v) {
    if (!v || typeof v !== 'object') return;
    for (const [key,item] of Object.entries(v)) {
      if (/^(password|secret|token|accessToken|apiKey|authorization|cookie|credentials)$/i.test(key)) fail('Authentication material is not publication metadata.');
      check(item);
    }
  }
  check(clean); return clean;
}

// Recovery is deliberately strict: a version means the same developer inputs
// and metadata. A crash after moving the snapshot but before queue visibility,
// or after queue visibility but before developer state persistence, can retry
// safely without replacing an active publisher's independent status.
async function recoverSnapshot(paths, destination, version, metadata) {
  if (!(await exists(destination))) fail('Queue version exists without its frozen snapshot.');
  await checkedDirectory(destination);
  const job = await readJson((await checkedFile(destination,'job.json')).full);
  validateJobPaths(job);
  if (job.version !== version || fold(job.sourceRoot) !== fold(paths.root) || fold(job.snapshotRoot) !== fold(destination) || JSON.stringify(canonical(job.metadata)) !== JSON.stringify(canonical(metadata))) fail('Existing version has different source, destination, or metadata.');
  const manifest = await verifyPublicationSnapshot(job);
  const before = await inventory(paths.root), config = await unityConfiguration(paths.root);
  if (JSON.stringify(normalizedInputs(before,config)) !== JSON.stringify(manifest.files)) fail('Existing version does not match current development inputs.');
  compareInventories(before,await inventory(paths.root));
  const queueFile = path.join(paths.queue,version+'.json');
  if (await exists(queueFile)) {
    const recorded = await readJson((await checkedFile(paths.queue,version+'.json')).full);
    if (JSON.stringify(recorded) !== JSON.stringify(job)) fail('Existing queue record differs from snapshot job.');
  } else await atomicJson(queueFile,job);
  return job;
}

/** Called only after the development worker has exited, under its own lease.
 * No directory is removed on failure: incomplete .preparing-* directories
 * carry a marker and are never claimable. Retention is intentionally manual.
 */
export async function enqueueSnapshot({sourceRoot, snapshotBase = 'F:\\EasternDynastyBuild\\Releases', version, metadata = {}}) {
  versionName(version);
  const root = await checkedDirectory(sourceRoot), base = await checkedDirectory(snapshotBase, true);
  if (fold(root) === fold(base) || inside(root, base) || inside(base, root)) fail('Snapshot base and development source must be disjoint.');
  const safe = safeMetadata(metadata), destination = path.join(base, version);
  if (await exists(destination) || await exists(path.join(root,'.local/publication/queue',version+'.json')))
    return withQueueLock(root, paths => recoverSnapshot(paths,destination,version,safe));
  const id = randomUUID(), staging = path.join(base, '.preparing-' + version + '-' + id);
  await checkedDirectory(staging, true);
  await fs.writeFile(path.join(staging, '.preparing.json'), json({version, id, sourceRoot:root, createdUtc:iso()}), {flag:'wx'});
  const inputRoot = await checkedDirectory(path.join(staging, 'Source'), true);
  const projectRoot = await checkedDirectory(path.join(staging, 'Project'), true);
  const before = await inventory(root);
  const unityBytes = await unityConfiguration(root);
  await copyFiles(before, inputRoot, unityBytes);
  const frozen = await inventory(inputRoot, {all:true});
  const expected = normalizedInputs(before,unityBytes);
  if (JSON.stringify(publicFiles(frozen)) !== JSON.stringify(expected)) fail('Frozen input copy differs from development inputs.');
  await copyFiles(frozen, projectRoot);
  const work = await inventory(projectRoot, {all:true});
  if (JSON.stringify(publicFiles(work)) !== JSON.stringify(expected)) fail('Unity working copy differs from frozen inputs.');
  const after = await inventory(root);
  compareInventories(before, after);
  const manifest = {schemaVersion:1, version, snapshotId:id, sourceRoot:root, createdUtc:iso(), files:expected};
  const manifestBytes = json(manifest);
  const job = {schemaVersion:1, version, snapshotId:id, createdUtc:manifest.createdUtc, sourceRoot:root, snapshotRoot:destination, inputRoot:path.join(destination,'Source'), projectRoot:path.join(destination,'Project'), manifestPath:path.join(destination,'manifest.json'), manifestSha256:sha(manifestBytes), metadata:safe};
  await fs.writeFile(path.join(staging, 'manifest.json'), manifestBytes, {flag:'wx'});
  await fs.writeFile(path.join(staging, 'job.json'), json(job), {flag:'wx'});
  await fs.writeFile(path.join(staging, '.eastern-dynasty-release.json'), json({schemaVersion:1, snapshotId:id, version, sourceRoot:root, manifestSha256:job.manifestSha256}), {flag:'wx'});
  return withQueueLock(root, async paths => {
    const queueFile = path.join(paths.queue, version + '.json');
    if (await exists(queueFile) || await exists(destination)) return recoverSnapshot(paths,destination,version,safe);
    // Last check before visibility also catches edits made while awaiting a
    // competing publisher's queue lease. The developer lease is the authority
    // for keeping writes stopped during this transaction.
    compareInventories(before, await inventory(root));
    await fs.rename(staging, destination);
    // No producer writes job-status or active, including for a newer version.
    await atomicJson(queueFile, job);
    return job;
  });
}

function validateJobPaths(job) {
  versionName(job?.version);
  for (const key of ['sourceRoot','snapshotRoot','inputRoot','projectRoot','manifestPath']) if (typeof job[key] !== 'string' || !path.isAbsolute(job[key])) fail('Invalid job path: ' + key);
  const snapshot = path.resolve(job.snapshotRoot), source = path.resolve(job.sourceRoot);
  if (path.basename(snapshot) !== job.version || inside(source,snapshot) || inside(snapshot,source) || fold(snapshot) === fold(source)) fail('Snapshot overlaps development source.');
  if (fold(path.resolve(job.inputRoot)) !== fold(path.join(snapshot,'Source')) || fold(path.resolve(job.projectRoot)) !== fold(path.join(snapshot,'Project')) || fold(path.resolve(job.manifestPath)) !== fold(path.join(snapshot,'manifest.json'))) fail('Job path escapes its snapshot.');
  if (!/^[a-f0-9]{64}$/.test(job.manifestSha256 || '')) fail('Invalid manifest hash.');
}
function mutableProjectFile(relative, expected) {
  return relative.endsWith('.meta') || relative.startsWith('ProjectSettings/') || (relative === 'Assets/Scenes/EasternDynasty.unity' && !expected.has(fold(relative)));
}

/** Verify both immutable inputs and build-working-copy code/assets/tools.
 * Unity may generate meta/settings and its known bootstrap scene in Project;
 * exact post-import playable hashes are bound separately by release evidence.
 */
export async function verifyPublicationSnapshot(job) {
  validateJobPaths(job);
  await checkedDirectory(job.snapshotRoot);
  await checkedDirectory(job.inputRoot);
  await checkedDirectory(job.projectRoot);
  const localJob = await readJson((await checkedFile(job.snapshotRoot,'job.json')).full);
  if (JSON.stringify(localJob) !== JSON.stringify(job)) fail('Snapshot job differs from immutable queue record.');
  const marker = await readJson((await checkedFile(job.snapshotRoot,'.eastern-dynasty-release.json')).full);
  if (marker?.snapshotId !== job.snapshotId || marker?.version !== job.version || marker?.sourceRoot !== job.sourceRoot || marker?.manifestSha256 !== job.manifestSha256) fail('Snapshot ownership marker mismatch.');
  const manifestBytes = await fs.readFile((await checkedFile(job.snapshotRoot,'manifest.json')).full);
  if (sha(manifestBytes) !== job.manifestSha256) fail('Snapshot manifest changed.');
  const manifest = JSON.parse(manifestBytes.toString('utf8'));
  if (manifest.version !== job.version || manifest.snapshotId !== job.snapshotId || manifest.sourceRoot !== job.sourceRoot || !Array.isArray(manifest.files) || !manifest.files.length) fail('Invalid snapshot manifest.');
  const expected = new Map();
  for (const item of manifest.files) {
    const rel = relativeName(item.path), key = fold(rel);
    if (expected.has(key) || !/^[a-f0-9]{64}$/.test(item.sha256) || !Number.isSafeInteger(item.size) || item.size < 0) fail('Invalid manifest file entry.');
    expected.set(key,item);
  }
  const frozen = await inventory(job.inputRoot, {all:true});
  if (frozen.size !== expected.size) fail('Frozen input file set changed.');
  for (const [key,item] of frozen) if (expected.get(key)?.sha256 !== item.sha256 || expected.get(key)?.size !== item.stat.size) fail('Frozen input changed: ' + item.relative);
  const project = await inventory(job.projectRoot);
  for (const [key,item] of project) {
    if (mutableProjectFile(item.relative,expected)) continue;
    if (expected.get(key)?.sha256 !== item.sha256 || expected.get(key)?.size !== item.stat.size) fail('Build working input changed: ' + item.relative);
  }
  for (const [key,item] of expected) if (!mutableProjectFile(item.path,expected) && !project.has(key)) fail('Build working input missing: ' + item.path);
  return manifest;
}

async function readEntries(paths) {
  const jobs = [];
  if (!(await exists(paths.queue))) return jobs;
  for (const name of await fs.readdir(paths.queue)) {
    if (!name.endsWith('.json')) continue;
    const version = versionName(name.slice(0,-5));
    const job = await readJson((await checkedFile(paths.queue,name)).full);
    validateJobPaths(job);
    if (version !== job.version || fold(job.sourceRoot) !== fold(paths.root)) fail('Queue record identity mismatch.');
    const statePath = path.join(paths.states,name);
    const state = await exists(statePath) ? await readJson((await checkedFile(paths.states,name)).full) : {version, status:'queued', updatedUtc:job.createdUtc};
    if (state.version !== version || typeof state.status !== 'string') fail('Invalid job status.');
    jobs.push({job,state});
  }
  return jobs.sort((a,b) => b.job.createdUtc.localeCompare(a.job.createdUtc) || b.job.version.localeCompare(a.job.version));
}
async function activeRecord(paths) {
  if (!(await exists(paths.active))) return null;
  return readJson((await checkedFile(paths.base,'active.json')).full);
}
export async function readPublicationState({sourceRoot}) {
  return withQueueLock(sourceRoot, async paths => ({active:await activeRecord(paths), jobs:await readEntries(paths)}), {create:false});
}

/** Existing active work always wins, even if awaiting desktop/player. */
export async function claimNextPublication({sourceRoot}) {
  return withQueueLock(sourceRoot, async paths => {
    const entries = await readEntries(paths), active = await activeRecord(paths);
    if (active) {
      const entry = entries.find(e => e.job.version === active.version);
      if (!entry) fail('Active job has no queue record.');
      if (!terminal.has(entry.state.status)) return entry;
      // Recovery after a process stopped between terminal status and clearing
      // active. An active nonterminal record is never overwritten.
      await atomicJson(paths.active, null);
    }
    // Recover a claim whose process stopped before writing active.json.
    const started = entries.filter(e => e.state.status !== 'queued' && !terminal.has(e.state.status));
    if (started.length > 1) fail('Multiple started publications require inspection.');
    const candidate = started[0] || entries.find(e => e.state.status === 'queued');
    if (!candidate) return null;
    const state = candidate.state.status === 'queued' ? {...candidate.state,status:'claimed',startedUtc:iso(),updatedUtc:iso()} : candidate.state;
    await atomicJson(paths.active, {version:candidate.job.version, claimedUtc:state.startedUtc || iso()});
    await atomicJson(path.join(paths.states,candidate.job.version+'.json'),state);
    // Only never-started work is coalesced. Keep immutable jobs/snapshots for
    // audit and manual reclamation; no recursive unattended deletion.
    for (const entry of entries) if (entry !== candidate && entry.state.status === 'queued') await atomicJson(path.join(paths.states,entry.job.version+'.json'), {...entry.state,status:'superseded',supersededBy:candidate.job.version,updatedUtc:iso()});
    return {job:candidate.job,state};
  });
}

export async function updatePublicationJob({sourceRoot, version, status, patch = {}}) {
  versionName(version);
  if (typeof status !== 'string' || !/^[a-z][a-z0-9_]{0,63}$/.test(status) || status === 'queued') fail('Invalid publisher status.');
  if (!patch || typeof patch !== 'object' || Array.isArray(patch)) fail('State patch must be an object.');
  for (const reserved of ['version','status','updatedUtc','startedUtc']) if (Object.hasOwn(patch,reserved)) fail('Reserved publisher state field: ' + reserved);
  const clean = safeMetadata(patch);
  return withQueueLock(sourceRoot, async paths => {
    const entries = await readEntries(paths), entry = entries.find(e => e.job.version === version), active = await activeRecord(paths);
    if (!entry) fail('Unknown publication version.');
    if (terminal.has(entry.state.status)) {
      if (entry.state.status === status) return entry.state;
      fail('Terminal publication cannot be resumed or replaced.');
    }
    if (active?.version !== version) fail('Only the active publication may update status.');
    const state = {...entry.state,...clean,status,version,updatedUtc:iso()};
    await atomicJson(path.join(paths.states,version+'.json'),state);
    if (terminal.has(status)) await atomicJson(paths.active,null);
    return state;
  });
}

/** Explicit operator recovery only. No worker or publisher loop calls this.
 * A corrected, generated validation fixture may permit a full retry, but a
 * modified frozen/source-working input requires a new version instead.
 * Preserve failure evidence strictly under history; never revive old visual
 * approvals, build checkpoints, installed-result receipts, or retry timers.
 */
export async function retryFailedPublication({sourceRoot, version, reason}) {
  versionName(version);
  if (typeof reason !== 'string' || !reason.trim() || reason.trim().length > 4000) fail('An explicit retry reason of 1..4000 characters is required.');
  const explanation = reason.trim();
  return withQueueLock(sourceRoot, async paths => {
    const entries = await readEntries(paths), entry = entries.find(e => e.job.version === version), active = await activeRecord(paths);
    if (!entry) fail('Unknown publication version.');
    if (entry.state.status !== 'failed') fail('Only a failed publication can be explicitly retried.');
    if (active && active.version !== version) fail('Cannot retry while another publication is active.');
    if (entries.some(e => e !== entry && e.state.status !== 'queued' && !terminal.has(e.state.status))) fail('Cannot retry while another publication has started.');
    // Verify before any status/active mutation. Normal generated Validation
    // JSON is excluded from the input manifest; code/assets/tools are bound.
    await verifyPublicationSnapshot(entry.job);
    const requestedUtc = iso();
    const {attemptHistory:priorHistory, previousFailure:olderFailure, ...previousFailure} = entry.state;
    if (priorHistory !== undefined && !Array.isArray(priorHistory)) fail('Invalid publication attempt history.');
    const history = priorHistory || [];
    const record = {
      attempt:history.length + 1,
      status:'failed',
      startedUtc:entry.state.startedUtc || null,
      finishedUtc:entry.state.updatedUtc || null,
      lastFailure:entry.state.lastFailure ?? null,
      retryRequestedUtc:requestedUtc,
      reason:explanation
    };
    const state = {
      version,
      status:'queued',
      updatedUtc:requestedUtc,
      retryRequestedUtc:requestedUtc,
      retryReason:explanation,
      manualRetryCount:history.length + 1,
      previousFailure,
      attemptHistory:[...history,record]
    };
    // A stale active pointer to this exact failed version can remain after a
    // crash during terminal-state persistence. Clearing it first is safe: a
    // crash here leaves the job failed, requiring another explicit retry.
    if (active) await atomicJson(paths.active,null);
    await atomicJson(path.join(paths.states,version+'.json'),state);
    return state;
  });
}
