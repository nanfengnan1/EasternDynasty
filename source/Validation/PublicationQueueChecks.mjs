import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawn} from 'node:child_process';
import {enqueueSnapshot, claimNextPublication, updatePublicationJob, readPublicationState, verifyPublicationSnapshot, retryFailedPublication} from '../Automation/publication-queue.mjs';

const ownFile = fileURLToPath(import.meta.url);
// Independent Node processes exercise the named mutex, not merely promises in
// one isolate. Child mode uses only its isolated fixture paths.
if (process.argv[2] === '--child') {
  const [operation,root,base,version] = process.argv.slice(3);
  const result = operation === 'enqueue' ? await enqueueSnapshot({sourceRoot:root,snapshotBase:base,version}) : await claimNextPublication({sourceRoot:root});
  process.stdout.write(JSON.stringify(result));
} else {
  const parent = path.dirname(ownFile);
  const sandbox = await fs.mkdtemp(path.join(parent,'publication-queue-fixture-'));
  let checks = 0;
  const check = (value, label) => { assert.ok(value,label); checks++; };
  const absent = file => fs.access(file).then(() => false, e => { if (e.code === 'ENOENT') return true; throw e; });
  const write = async (root,name,data) => { const file = path.join(root,name); await fs.mkdir(path.dirname(file),{recursive:true}); await fs.writeFile(file,data); };
  const read = file => fs.readFile(file,'utf8');
  async function fixture(name) {
    const root = path.join(sandbox,name,'source'), base = path.join(sandbox,name,'releases');
    await fs.mkdir(root,{recursive:true}); await fs.mkdir(base,{recursive:true});
    await write(root,'Assets/Game.cs','class Game { const int Version = 1; }');
    await write(root,'Assets/Game.cs.meta','guid: 0123456789abcdef0123456789abcdef');
    await write(root,'Packages/manifest.json','{"dependencies":{}}');
    await write(root,'ProjectSettings/ProjectVersion.txt','m_EditorVersion: fixture');
    await write(root,'Tools/Validate.ps1','Write-Output fixture');
    await write(root,'Automation/policy.mjs','export const fixture = true;');
    await write(root,'Docs/Rules.md','Fixture rules');
    await write(root,'AGENTS.md','Fixture instructions');
    await write(root,'agent.md','Fixture backlog');
    await write(root,'README.md','Fixture readme');
    await write(root,'AssetInbox/asset-manifest.schema.json','{}');
    await write(root,'AssetInbox/research/provenance.json','{"source":"fixture"}');
    await write(root,'AssetInbox/raw.png','do not copy unused source image');
    await write(root,'Validation/CoreChecks.cs','class Checks {}');
    await write(root,'Validation/CardDisplayChecks/ExportCardDisplay.cs','class Export {}');
    await write(root,'Validation/Visual/old/screenshot.png','private old screenshot');
    await write(root,'Validation/Training/old/check.ps1','historical helper');
    await write(root,'Library/cache','previous cache');
    await write(root,'Builds/old.exe','old build');
    await write(root,'.local/auth.json','{"token":"private"}');
    await write(root,'Automation/service-auth.json','{"token":"private"}');
    await write(root,'Docs/old.log','old log');
    const editor = path.join(sandbox,'fake-editor','Unity.exe');
    await write(path.dirname(editor),path.basename(editor),'fixture binary');
    await write(root,'.local/unity.json',JSON.stringify({unityEditor:editor,unityVersion:'6000.3.27f1',token:'must not propagate',extra:'drop'}));
    return {root,base};
  }
  function child(operation,root,base,version = '') {
    return new Promise((resolve,reject) => {
      const process = spawn(globalThis.process.execPath,[ownFile,'--child',operation,root,base,version],{windowsHide:true,stdio:['ignore','pipe','pipe']});
      let out = '', err = '';
      process.stdout.on('data',b => out += b.toString()); process.stderr.on('data',b => err += b.toString());
      process.once('error',reject);
      process.once('close',code => code === 0 ? resolve(JSON.parse(out)) : reject(new Error(err || String(code))));
    });
  }
  try {
    const {root,base} = await fixture('isolation');
    const empty = await readPublicationState({sourceRoot:root});
    check(empty.active === null && empty.jobs.length === 0 && await absent(path.join(root,'.local/publication')),'Read/dry-run does not create queue or publisher state');
    const metadata = {workerResult:{status:'changed',changes:['fixture']},ragReceipt:'immutable-run/brief.json'};
    const job = await enqueueSnapshot({sourceRoot:root,snapshotBase:base,version:'s1-v1',metadata});
    const manifest = await verifyPublicationSnapshot(job);
    check(manifest.files.length >= 15,'Required source/test/metadata inputs included');
    const relative = new Set(manifest.files.map(f => f.path));
    check(relative.has('Assets/Game.cs.meta') && relative.has('Validation/CardDisplayChecks/ExportCardDisplay.cs'),'Unity meta and explicit nested fixture preserved');
    for (const name of ['Library/cache','Builds/old.exe','.local/auth.json','Automation/service-auth.json','Docs/old.log','AssetInbox/raw.png','Validation/Visual/old/screenshot.png','Validation/Training/old/check.ps1']) check(!relative.has(name), 'Excluded ' + name);
    const config = JSON.parse(await read(path.join(job.projectRoot,'.local/unity.json')));
    check(Object.keys(config).sort().join(',') === 'unityEditor,unityVersion','Only local Unity path/version are copied');
    const sourceStat = await fs.stat(path.join(root,'Assets/Game.cs'));
    const inputStat = await fs.stat(path.join(job.inputRoot,'Assets/Game.cs'));
    const projectStat = await fs.stat(path.join(job.projectRoot,'Assets/Game.cs'));
    check(sourceStat.ino !== inputStat.ino && inputStat.ino !== projectStat.ino,'No shared inode/hard link');
    metadata.workerResult.changes.push('after enqueue');
    await write(root,'Assets/Game.cs','class Game { const int Version = 2; }');
    await verifyPublicationSnapshot(job);
    check((await read(path.join(job.projectRoot,'Assets/Game.cs'))).includes('Version = 1'),'Developer edits cannot change frozen release');
    check(job.metadata.workerResult.changes.length === 1,'Publication metadata is detached from mutable caller');
    check(await absent(path.join(root,'.local/publication/active.json')) && await absent(path.join(root,'.local/publication/job-status/s1-v1.json')),'Producer never writes active or publisher status');
    await write(job.projectRoot,'Assets/Game.cs','corrupted working code');
    await assert.rejects(() => verifyPublicationSnapshot(job),/Build working input changed/); checks++;
    await fs.copyFile(path.join(job.inputRoot,'Assets/Game.cs'),path.join(job.projectRoot,'Assets/Game.cs'));
    await write(job.projectRoot,'Assets/Logs/Injected.cs','class Injected {}');
    await assert.rejects(() => verifyPublicationSnapshot(job),/Build working input changed/); checks++;
    await fs.unlink(path.join(job.projectRoot,'Assets/Logs/Injected.cs'));
    await write(job.projectRoot,'ProjectSettings/generated.asset','Unity import side effect');
    await write(job.projectRoot,'Assets/Game.cs.meta','Unity normal importer change');
    await write(job.projectRoot,'Assets/Scenes/EasternDynasty.unity','generated bootstrap scene');
    await verifyPublicationSnapshot(job); checks++;
    await write(job.inputRoot,'Assets/Game.cs','tampered frozen input');
    await assert.rejects(() => verifyPublicationSnapshot(job),/Frozen input changed/); checks++;
    await write(job.inputRoot,'Assets/Game.cs','class Game { const int Version = 1; }');
    await assert.rejects(() => enqueueSnapshot({sourceRoot:root,snapshotBase:base,version:'../escape'}),/Invalid version/); checks++;
    await assert.rejects(() => enqueueSnapshot({sourceRoot:root,snapshotBase:path.join(root,'inside'),version:'escape'}),/disjoint/); checks++;
    await assert.rejects(() => verifyPublicationSnapshot({...job,projectRoot:root}),/escapes/); checks++;
    await assert.rejects(() => enqueueSnapshot({sourceRoot:root,snapshotBase:base,version:'auth',metadata:{token:'private'}}),/Authentication/); checks++;

    const first = await claimNextPublication({sourceRoot:root});
    check(first.job.version === 's1-v1','First candidate claimed');
    await updatePublicationJob({sourceRoot:root,version:'s1-v1',status:'waiting_for_desktop',patch:{checkpoint:{sourceSha256:'fixture'}}});
    const queueBytes = await read(path.join(root,'.local/publication/queue/s1-v1.json'));
    await enqueueSnapshot({sourceRoot:root,snapshotBase:base,version:'s1-v2'});
    await enqueueSnapshot({sourceRoot:root,snapshotBase:base,version:'s1-v3'});
    const pinned = await claimNextPublication({sourceRoot:root});
    check(pinned.job.version === 's1-v1' && pinned.state.status === 'waiting_for_desktop','New candidates cannot replace active desktop wait');
    await updatePublicationJob({sourceRoot:root,version:'s1-v1',status:'preparing',patch:{androidAttempts:1}});
    const progress = (await readPublicationState({sourceRoot:root})).jobs.find(e => e.job.version === 's1-v1').state;
    check(progress.checkpoint.sourceSha256 === 'fixture' && progress.androidAttempts === 1,'State patch is shallow merged');
    await assert.rejects(() => updatePublicationJob({sourceRoot:root,version:'s1-v2',status:'preparing'}),/active/); checks++;
    await updatePublicationJob({sourceRoot:root,version:'s1-v1',status:'published'});
    const latest = await claimNextPublication({sourceRoot:root});
    check(latest.job.version === 's1-v3','After completion newest queued candidate wins');
    const state = await readPublicationState({sourceRoot:root});
    check(state.jobs.find(e => e.job.version === 's1-v2').state.status === 'superseded','Never-started stale candidate is superseded');
    check(queueBytes === await read(path.join(root,'.local/publication/queue/s1-v1.json')),'Immutable queue job never rewritten by publisher');
    await updatePublicationJob({sourceRoot:root,version:'s1-v3',status:'failed',patch:{lastFailure:'fixture'}});
    check(await claimNextPublication({sourceRoot:root}) === null,'Failed completion releases active and drained queue returns null');
    await assert.rejects(() => retryFailedPublication({sourceRoot:root,version:'s1-v3',reason:' '}),/explicit retry reason/); checks++;
    await assert.rejects(() => retryFailedPublication({sourceRoot:root,version:'s1-v1',reason:'test published guard'}),/Only a failed/); checks++;
    await assert.rejects(() => retryFailedPublication({sourceRoot:root,version:'s1-v2',reason:'test superseded guard'}),/Only a failed/); checks++;

    const retry = await fixture('explicit-retry');
    const failedJob = await enqueueSnapshot({sourceRoot:retry.root,snapshotBase:retry.base,version:'s1-failed'});
    const immutableBeforeRetry = await read(path.join(retry.root,'.local/publication/queue/s1-failed.json'));
    await claimNextPublication({sourceRoot:retry.root});
    await updatePublicationJob({sourceRoot:retry.root,version:'s1-failed',status:'failed',patch:{checkpoint:{visualReviewSha256:'old-approval'},windows:{status:'old-result'},android:{status:'old-result'},androidAttempts:3,retryAfterUtc:'2999-01-01T00:00:00Z',lastFailure:'Missing generated serialization fixture',futureProgress:{must:'reset'}}});
    await enqueueSnapshot({sourceRoot:retry.root,snapshotBase:retry.base,version:'s1-other'});
    await claimNextPublication({sourceRoot:retry.root});
    await assert.rejects(() => retryFailedPublication({sourceRoot:retry.root,version:'s1-failed',reason:'test active guard'}),/another publication is active/); checks++;
    await assert.rejects(() => retryFailedPublication({sourceRoot:retry.root,version:'s1-other',reason:'test running guard'}),/Only a failed/); checks++;
    await updatePublicationJob({sourceRoot:retry.root,version:'s1-other',status:'published'});
    await write(failedJob.projectRoot,'Assets/Game.cs','modified working input');
    await assert.rejects(() => retryFailedPublication({sourceRoot:retry.root,version:'s1-failed',reason:'must reject modified working code'}),/Build working input changed/); checks++;
    await fs.copyFile(path.join(failedJob.inputRoot,'Assets/Game.cs'),path.join(failedJob.projectRoot,'Assets/Game.cs'));
    await write(failedJob.inputRoot,'Assets/Game.cs','modified frozen input');
    await assert.rejects(() => retryFailedPublication({sourceRoot:retry.root,version:'s1-failed',reason:'must reject modified frozen code'}),/Frozen input changed/); checks++;
    await fs.copyFile(path.join(failedJob.projectRoot,'Assets/Game.cs'),path.join(failedJob.inputRoot,'Assets/Game.cs'));
    const failedAfterRejections = (await readPublicationState({sourceRoot:retry.root})).jobs.find(e => e.job.version === 's1-failed').state;
    check(failedAfterRejections.status === 'failed' && failedAfterRejections.checkpoint.visualReviewSha256 === 'old-approval','Rejected retry preserves failed state without partial reset');
    await write(failedJob.projectRoot,'Validation/serialization-idle.json','{"generated":"fixture"}');
    const requested = await retryFailedPublication({sourceRoot:retry.root,version:'s1-failed',reason:'Generated missing fixtures using unchanged snapshot editor command'});
    check(requested.status === 'queued' && requested.previousFailure.lastFailure === 'Missing generated serialization fixture' && requested.attemptHistory.length === 1,'Explicit retry queues a complete new attempt with failure history');
    for (const field of ['checkpoint','windows','android','androidAttempts','retryAfterUtc','lastFailure','startedUtc','futureProgress']) check(!Object.hasOwn(requested,field),'Explicit retry clears prior progress: '+field);
    check(immutableBeforeRetry === await read(path.join(retry.root,'.local/publication/queue/s1-failed.json')),'Retry never mutates immutable queue input');
    await assert.rejects(() => retryFailedPublication({sourceRoot:retry.root,version:'s1-failed',reason:'duplicate retry'}),/Only a failed/); checks++;
    const freshAttempt = await claimNextPublication({sourceRoot:retry.root});
    check(freshAttempt.job.version === 's1-failed' && freshAttempt.state.status === 'claimed' && !freshAttempt.state.checkpoint,'Retried job claims from the beginning without old approval');
    await updatePublicationJob({sourceRoot:retry.root,version:'s1-failed',status:'failed',patch:{lastFailure:'Second distinct failure'}});
    // Simulate a crash between writing failed state and clearing active.json.
    await write(retry.root,'.local/publication/active.json',JSON.stringify({version:'s1-failed',claimedUtc:new Date().toISOString()}));
    const secondRetry = await retryFailedPublication({sourceRoot:retry.root,version:'s1-failed',reason:'Explicit second recovery'});
    check(secondRetry.attemptHistory.length === 2 && secondRetry.previousFailure.lastFailure === 'Second distinct failure' && secondRetry.attemptHistory[0].lastFailure === 'Missing generated serialization fixture','Repeated explicit attempts retain ordered failure history');
    check((await readPublicationState({sourceRoot:retry.root})).active === null,'Retry clears only a stale active pointer for the same failed version');

    const concurrent = await fixture('concurrent');
    const produced = await Promise.all([child('enqueue',concurrent.root,concurrent.base,'s1-a'),child('enqueue',concurrent.root,concurrent.base,'s1-b')]);
    check(new Set(produced.map(j => j.version)).size === 2,'Two processes can enqueue independent snapshots');
    const claims = await Promise.all([child('claim',concurrent.root,concurrent.base),child('claim',concurrent.root,concurrent.base)]);
    check(claims[0].job.version === claims[1].job.version,'Concurrent claims see the same pinned active publication');
    const concurrentState = await readPublicationState({sourceRoot:concurrent.root});
    check(concurrentState.jobs.filter(e => e.state.status === 'claimed').length === 1 && concurrentState.jobs.filter(e => e.state.status === 'superseded').length === 1,'One active candidate under cross-process contention');

    const recovery = await fixture('crash-recovery');
    const recoveryMetadata = {workerResult:{status:'changed',changes:['same-version']}};
    const saved = await enqueueSnapshot({sourceRoot:recovery.root,snapshotBase:recovery.base,version:'s1-recover',metadata:recoveryMetadata});
    const savedQueueFile = path.join(recovery.root,'.local/publication/queue/s1-recover.json');
    const savedBytes = await read(savedQueueFile);
    // Simulate snapshot rename committed, queue record did not reach disk.
    await fs.unlink(savedQueueFile);
    const resumed = await enqueueSnapshot({sourceRoot:recovery.root,snapshotBase:recovery.base,version:'s1-recover',metadata:recoveryMetadata});
    check(resumed.snapshotId === saved.snapshotId && await read(savedQueueFile) === savedBytes,'Interrupted snapshot commit restores the identical immutable queue job');
    await claimNextPublication({sourceRoot:recovery.root});
    await updatePublicationJob({sourceRoot:recovery.root,version:'s1-recover',status:'waiting_for_desktop'});
    // Simulate queue committed, developer state persistence was interrupted.
    const retried = await enqueueSnapshot({sourceRoot:recovery.root,snapshotBase:recovery.base,version:'s1-recover',metadata:recoveryMetadata});
    check(retried.snapshotId === saved.snapshotId && (await claimNextPublication({sourceRoot:recovery.root})).state.status === 'waiting_for_desktop','Idempotent enqueue does not reset active publisher state');
    await assert.rejects(() => enqueueSnapshot({sourceRoot:recovery.root,snapshotBase:recovery.base,version:'s1-recover',metadata:{workerResult:{status:'changed',changes:['different']}}}),/different.*metadata/); checks++;
    await write(recovery.root,'Assets/Game.cs','changed after committed release');
    await assert.rejects(() => enqueueSnapshot({sourceRoot:recovery.root,snapshotBase:recovery.base,version:'s1-recover',metadata:recoveryMetadata}),/current development inputs/); checks++;

    const race = await fixture('copy-race');
    await write(race.root,'Assets/Large.bin',Buffer.alloc(16*1024*1024,37));
    let mutating = false, writes = 0, pending = Promise.resolve();
    const timer = setInterval(() => {
      if (!mutating) return;
      pending = pending.then(async () => { await write(race.root,'Assets/Game.cs','copy race ' + (++writes)); });
    },2);
    const watching = setInterval(async () => {
      const entries = await fs.readdir(race.base);
      for (const name of entries) if (name.startsWith('.preparing-') && !(await absent(path.join(race.base,name,'Source/Assets/Game.cs')))) mutating = true;
    },1);
    try { await assert.rejects(() => enqueueSnapshot({sourceRoot:race.root,snapshotBase:race.base,version:'s1-race'}),/changed|differs/); checks++; }
    finally { clearInterval(timer); clearInterval(watching); await pending; }
    check(writes > 0 && await absent(path.join(race.root,'.local/publication/queue/s1-race.json')),'Mid-copy developer writes are rejected before queue visibility');

    const linked = await fixture('junction');
    const outside = path.join(sandbox,'external'); await fs.mkdir(outside);
    await write(outside,'escape.cs','outside data');
    await fs.symlink(outside,path.join(linked.root,'Assets/linked'),'junction');
    await assert.rejects(() => enqueueSnapshot({sourceRoot:linked.root,snapshotBase:linked.base,version:'s1-junction'}),/Reparse|reparse/); checks++;
    console.log('PASS: ' + checks + ' publication snapshot and queue checks.');
  } finally {
    // The target was created by this process, is an immediate child of this
    // test's Validation directory, and must resolve there before recursion.
    const actual = await fs.realpath(sandbox), expectedParent = await fs.realpath(parent);
    assert.equal(path.dirname(actual).toLowerCase(),expectedParent.toLowerCase());
    assert.ok(path.basename(actual).startsWith('publication-queue-fixture-'));
    await fs.rm(actual,{recursive:true,force:true});
  }
}
