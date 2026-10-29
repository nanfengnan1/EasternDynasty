import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {tickPublication,sourceFingerprint,verifyAndroidReceipt} from '../Automation/publisher.mjs';

const directory=path.dirname(fileURLToPath(import.meta.url));
const fixtures=await fs.mkdtemp(path.join(directory,'.publisher-fixtures-'));
const hash=value=>createHash('sha256').update(value).digest('hex');
const json=async(file,value)=>{await fs.mkdir(path.dirname(file),{recursive:true});await fs.writeFile(file,JSON.stringify(value,null,2));};
const read=async file=>JSON.parse(await fs.readFile(file,'utf8'));
let passed=0,serial=0;
const checks=['core','assets','unity-repository','routes','youzhou-1440','youzhou-1280','mobile-1600','legacy','mouse','mouse-s1','experience'];

async function fixture(options={}){
  const base=path.join(fixtures,String(++serial)),sourceRoot=path.join(base,'SourceProject'),version='s1-fixture-'+serial;
  const snapshotRoot=path.join(base,'Snapshots',version),projectRoot=path.join(snapshotRoot,'Project'),inputRoot=path.join(snapshotRoot,'Source');
  const job={version,sourceRoot,snapshotRoot,projectRoot,inputRoot,metadata:{ragSnapshot:'frozen-reference.json'}};
  const receiptDir=path.join(projectRoot,'Validation','Releases',version);
  for(const project of [sourceRoot,projectRoot,inputRoot])for(const folder of ['Assets','Packages','ProjectSettings']){
    await fs.mkdir(path.join(project,folder),{recursive:true});await fs.writeFile(path.join(project,folder,'fixture.txt'),'frozen '+folder);
  }
  await json(path.join(sourceRoot,'Automation','config.json'),{enabled:true,publicationEnabled:true,publishAndroid:options.android!==false});
  await json(path.join(sourceRoot,'.local','continuous-development','paths.json'),{node:'fixture-node',powershell:'fixture-powershell',codexEntry:'fixture-codex'});
  const developmentFile=path.join(sourceRoot,'.local','continuous-development','state.json');
  await json(developmentFile,{nextRunUtc:'2099-01-01T00:00:00Z',pendingPublish:'not-owned-by-publisher',sentinel:'do-not-write'});
  const developmentBytes=await fs.readFile(developmentFile,'utf8');
  const legacyReleaseFile=path.join(sourceRoot,'Validation','Releases',version,'release.json');
  await json(legacyReleaseFile,{version,status:'waiting_for_desktop',sentinel:'original-legacy-evidence'});
  const legacyReleaseBytes=await fs.readFile(legacyReleaseFile,'utf8');
  const expectedInput=await sourceFingerprint(inputRoot);
  let state={version,status:'queued'},active=true,time=Date.now(),leaseCount=0,releases=0,validationWaits=options.validationWaits||0,installWaits=options.installWaits||0,androidTries=0;
  const calls=[];
  const queue={
    readPublicationState:async()=>({active:active?{version}:null,jobs:[{job,state}]}),
    claimNextPublication:async()=>active?{job,state}:null,
    updatePublicationJob:async({version:requested,status,patch})=>{assert.equal(requested,version);state={...state,...patch,status};if(['published','failed','superseded'].includes(status))active=false;return state;},
    verifyPublicationSnapshot:async candidate=>{assert.equal(candidate,job);assert.equal(await sourceFingerprint(inputRoot),expectedInput);}
  };
  const release=async status=>json(path.join(receiptDir,'release.json'),{version,status,executable:path.join(base,'games','EasternDynasty.exe')});
  const sourceFiles=async()=>{const result=[];for(const folder of ['Assets','Packages','ProjectSettings'])result.push({path:folder+'\\fixture.txt',sha256:hash(await fs.readFile(path.join(projectRoot,folder,'fixture.txt')))});return result;};
  const screenshotPaths=[];
  for(const suffix of ['youzhou-1440x900','youzhou-1280x720','mobile-1600x720']){
    const dir=path.join(projectRoot,'Validation','Visual',version+'-'+suffix);await fs.mkdir(dir,{recursive:true});
    for(const name of suffix==='youzhou-1440x900'?['overview','card','city']:['task']){const file=path.join(dir,name+'.png');await fs.writeFile(file,Buffer.from('89504e470d0a1a0a'+Buffer.from(suffix+name).toString('hex'),'hex'));screenshotPaths.push(file);}
  }
  const dependencies={
    queue,now:()=>time,
    acquireLease:async()=>{leaseCount++;return {release:async()=>{releases++;}};},
    runProcess:async(command,args,context)=>{
      assert.equal(context.cwd,projectRoot);
      assert.ok(context.runDir.startsWith(snapshotRoot+path.sep));
      assert.ok(!args.includes(path.join(sourceRoot,'Tools','Publish-Iteration.ps1')));
      calls.push({command,args,context});
      if(args.includes('-ValidateOnly')){
        assert.equal(args[args.indexOf('-DestinationPath')+1],path.join(base,'games'));
        if(validationWaits-->0){await release('waiting_for_desktop');return;}
        await json(path.join(receiptDir,'validation.json'),{version,sourceSha256:'a'.repeat(64),sourceFiles:await sourceFiles(),savesUnchanged:true,checks:options.missingCheck?checks.slice(1):checks});
        await release(options.wrongVersion?'not-a-state':'validated');
        if(options.wrongVersion)await json(path.join(receiptDir,'release.json'),{version:'another-version',status:'validated'});
      }else if(args.includes('--output-last-message')){
        assert.equal(args[args.indexOf('--sandbox')+1],'read-only');
        assert.ok(context.prompt.includes(version));
        let evidence=screenshotPaths;
        if(options.noMobile)evidence=screenshotPaths.filter(file=>!file.includes('-mobile-'));
        await json(args[args.indexOf('--output-last-message')+1],{status:options.reviewBlocked?'blocked':'no_change',summary:'Fixture review only; no actual visual review is claimed.',changes:[],evidence,remaining:[]});
        if(options.mutateDuringReview)await fs.appendFile(path.join(projectRoot,'Assets','fixture.txt'),' invalid new code');
      }else if(args.includes('-InstallVerified')){
        assert.equal(args[args.indexOf('-DestinationPath')+1],path.join(base,'games'));
        await release(installWaits-->0?'waiting_for_player':'published');
      }else if(args.some(arg=>arg.endsWith('Build-Android.ps1'))){
        const destination=args[args.indexOf('-DestinationPath')+1];assert.equal(destination,path.join(base,'apk'));androidTries++;
        const apk=path.join(destination,'EasternDynasty-S1-2026102601.apk');await fs.mkdir(destination,{recursive:true});await fs.writeFile(apk,'signed fixture bytes');
        await json(path.join(destination,'build-info.json'),{status:'built-and-package-verified',applicationId:'com.easterndynasty.s1',apk,sha256:hash(await fs.readFile(apk)),signatureVerified:true,packageVerified:true,sourceProjectSettingsUnchanged:true,sourceSnapshotVerified:true,builtUtc:new Date(options.staleAndroid==='always'||(options.staleAndroid&&androidTries===1)?time-60000:time).toISOString()});
      }else throw new Error('Unexpected fixture subprocess');
    }
  };
  return {base,job,sourceRoot,projectRoot,receiptDir,calls,dependencies,run:()=>tickPublication({sourceRoot},dependencies),dry:()=>tickPublication({sourceRoot,dryRun:true},dependencies),state:()=>state,advance:ms=>{time+=ms;},leases:()=>({leaseCount,releases}),assertDevelopmentUntouched:async()=>{assert.equal(await fs.readFile(developmentFile,'utf8'),developmentBytes);assert.equal(await fs.readFile(legacyReleaseFile,'utf8'),legacyReleaseBytes);}};
}
const count=(f,arg)=>f.calls.filter(call=>call.args.includes(arg)).length;
async function test(name,action){await action();passed++;console.log('PASS '+name);}

try{
  await test('dry-run reads queue and destinations without claiming, leasing or running processes',async()=>{
    const f=await fixture();const result=await f.dry();assert.equal(result.active.version,f.job.version);assert.equal(f.calls.length,0);assert.equal(f.leases().leaseCount,0);assert.equal(f.state().status,'queued');await assert.rejects(fs.stat(path.join(f.sourceRoot,'.local','publication')),{code:'ENOENT'});await f.assertDevelopmentUntouched();
  });
  await test('actual queue dry-run leaves a missing publication directory absent',async()=>{
    const f=await fixture();const result=await tickPublication({sourceRoot:f.sourceRoot,dryRun:true});assert.equal(result.active,null);assert.deepEqual(result.queue,[]);await assert.rejects(fs.stat(path.join(f.sourceRoot,'.local','publication')),{code:'ENOENT'});await f.assertDevelopmentUntouched();
  });
  await test('publication hold is independent from development hold',async()=>{
    const f=await fixture({android:false});await json(path.join(f.sourceRoot,'.local','continuous-development','manual-hold.json'),{reason:'developer busy'});assert.equal((await f.run()).status,'published');
    const g=await fixture();await json(path.join(g.sourceRoot,'.local','publication','manual-hold.json'),{reason:'publication paused'});assert.equal((await g.run()).status,'held');assert.equal(g.calls.length,0);
  });
  await test('desktop wait pins candidate while original source changes; retry publishes the frozen version',async()=>{
    const f=await fixture({validationWaits:1});assert.equal((await f.run()).status,'waiting_for_desktop');assert.equal(f.state().status,'waiting_for_desktop');assert.equal(count(f,'--output-last-message'),0);
    await fs.appendFile(path.join(f.sourceRoot,'Assets','fixture.txt'),' next development round');
    const result=await f.run();assert.equal(result.status,'published');assert.equal(result.version,f.job.version);assert.equal(count(f,'-ValidateOnly'),2);assert.equal(count(f,'-InstallVerified'),1);assert.equal(f.leases().leaseCount,f.leases().releases);await f.assertDevelopmentUntouched();
  });
  await test('player wait reuses visual approval and completed APK without recompiling or reviewing',async()=>{
    const f=await fixture({installWaits:1});assert.equal((await f.run()).status,'waiting_for_player');assert.ok(f.state().checkpoint);assert.ok(f.state().android);
    await fs.appendFile(path.join(f.sourceRoot,'Assets','fixture.txt'),' further development');
    assert.equal((await f.run()).status,'published');assert.equal(count(f,'-ValidateOnly'),1);assert.equal(count(f,'--output-last-message'),1);assert.equal(count(f,'-InstallVerified'),2);assert.equal(count(f,'-ReplacePrevious'),1);await f.assertDevelopmentUntouched();
  });
  await test('snapshot changes invalidate saved visual approval before installation',async()=>{
    const f=await fixture({android:false,installWaits:1});await f.run();await fs.appendFile(path.join(f.projectRoot,'Assets','fixture.txt'),' tampered');await assert.rejects(f.run(),/Source hash changed/);assert.equal(count(f,'-InstallVerified'),1);assert.equal(f.state().status,'failed');assert.equal((await read(path.join(f.sourceRoot,'.local','publication','last-failure.json'))).stage,'approval_verification');await f.assertDevelopmentUntouched();
  });
  await test('modified review receipt fails resumed installation',async()=>{
    const f=await fixture({android:false,installWaits:1});await f.run();await fs.appendFile(path.join(f.receiptDir,'visual-review.json'),' ');await assert.rejects(f.run(),/Visual approval receipt changed/);assert.equal(count(f,'-InstallVerified'),1);
  });
  await test('modified inspected screenshot invalidates approved checkpoint',async()=>{
    const f=await fixture({android:false,installWaits:1});await f.run();await fs.appendFile(f.state().checkpoint.visualEvidence[0].path,' changed');await assert.rejects(f.run(),/Reviewed screenshots changed/);assert.equal(count(f,'-InstallVerified'),1);
  });
  await test('four desktop screenshots without mobile cannot pass review',async()=>{
    const f=await fixture({noMobile:true});await assert.rejects(f.run(),/four distinct current screenshots including a mobile/);assert.equal(count(f,'-InstallVerified'),0);assert.equal(count(f,'-ReplacePrevious'),0);
  });
  await test('reviewer rejection never reaches either deliverable',async()=>{
    const f=await fixture({reviewBlocked:true});await assert.rejects(f.run(),/Visual reviewer rejected/);assert.equal(count(f,'-InstallVerified'),0);assert.equal(count(f,'-ReplacePrevious'),0);
  });
  await test('source modifications made during review reject that approval',async()=>{
    const f=await fixture({mutateDuringReview:true});await assert.rejects(f.run(),/Source hash changed/);assert.equal(count(f,'-InstallVerified'),0);
  });
  await test('partial validation or another version is rejected before review',async()=>{
    const f=await fixture({missingCheck:true});await assert.rejects(f.run(),/validation receipt is missing or incomplete/);assert.equal(count(f,'--output-last-message'),0);
    const g=await fixture({wrongVersion:true});await assert.rejects(g.run(),/another version/);assert.equal(count(g,'--output-last-message'),0);
  });
  await test('stale APK receipt schedules bounded retry and preserves completed Windows install',async()=>{
    const f=await fixture({staleAndroid:true});await assert.rejects(f.run(),/Fresh signed Android package receipt/);assert.equal(f.state().status,'android_retry');assert.equal(f.state().androidAttempts,1);assert.equal((await f.run()).status,'waiting_retry');f.advance(16*60000);assert.equal((await f.run()).status,'published');assert.equal(count(f,'-ValidateOnly'),1);assert.equal(count(f,'--output-last-message'),1);assert.equal(count(f,'-InstallVerified'),1);assert.equal(count(f,'-ReplacePrevious'),2);await assert.rejects(fs.stat(path.join(f.sourceRoot,'.local','publication','last-failure.json')),{code:'ENOENT'});await f.assertDevelopmentUntouched();
  });
  await test('Android artifact hash, signature flags and destination are verified',async()=>{
    const f=await fixture();await f.run();const receipt=f.state().android,destination=path.join(f.base,'apk');await assert.rejects(verifyAndroidReceipt({...receipt,signatureVerified:false},{destination}),/Fresh signed/);await assert.rejects(verifyAndroidReceipt({...receipt,sha256:'0'.repeat(64)},{destination}),/artifact hash/);await assert.rejects(verifyAndroidReceipt(receipt,{destination:path.join(f.base,'elsewhere')}),/outside the configured destination/);
  });
  await test('second failed Android attempt releases active candidate and records summary without changing legacy evidence',async()=>{
    const f=await fixture({staleAndroid:'always'});await assert.rejects(f.run(),/Fresh signed/);f.advance(16*60000);await assert.rejects(f.run(),/Fresh signed/);assert.equal(f.state().status,'failed');assert.equal(f.state().androidAttempts,2);const summary=await read(path.join(f.sourceRoot,'Validation','Releases',f.job.version,'publication.json'));assert.equal(summary.status,'failed');assert.equal(summary.evidence,f.receiptDir);assert.equal(summary.lastFailure.stage,'android');await f.assertDevelopmentUntouched();
  });
  await test('independent lease exclusion prevents queue and process mutation',async()=>{
    const f=await fixture();f.dependencies.acquireLease=async()=>null;assert.equal((await f.run()).status,'busy');assert.equal(f.state().status,'queued');assert.equal(f.calls.length,0);await f.assertDevelopmentUntouched();
  });
  console.log(JSON.stringify({status:'passed',checks:passed,scope:'isolated filesystem fixtures; no Unity, desktop interaction or actual publishing'}));
}finally{
  const checked=path.resolve(fixtures);
  if(path.dirname(checked)!==path.resolve(directory)||!path.basename(checked).startsWith('.publisher-fixtures-'))throw new Error('Unsafe fixture cleanup path.');
  await fs.rm(checked,{recursive:true,force:true});
}
