import fs from 'node:fs/promises';
import {createWriteStream} from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawn} from 'node:child_process';
import {createHash} from 'node:crypto';
import {due,nextAction,validConfig} from './policy.mjs';
import {syncRag} from './rag-sync.mjs';
import {enqueueSnapshot} from './publication-queue.mjs';
export {due,nextAction} from './policy.mjs';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const local=path.join(root,'.local','continuous-development');
const iso=()=>new Date().toISOString();
const read=async(file,fallback=null)=>{try{return JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));}catch(e){if(e.code==='ENOENT')return fallback;throw e;}};
async function write(file,value){await fs.mkdir(path.dirname(file),{recursive:true});const temp=file+'.'+process.pid+'.tmp';await fs.writeFile(temp,JSON.stringify(value,null,2),'utf8');await fs.rename(temp,file);}
const alive=pid=>{try{process.kill(pid,0);return true;}catch{return false;}};
let leaseProcess;
async function acquireLease(){
  const powershell=path.join(process.env.SystemRoot||'C:\\Windows','System32','WindowsPowerShell','v1.0','powershell.exe');
  const name='Local\\EasternDynasty-'+createHash('sha256').update(root.toLowerCase()).digest('hex').slice(0,16);
  const child=spawn(powershell,['-NoProfile','-ExecutionPolicy','Bypass','-File',path.join(root,'Automation','lease.ps1'),'-Name',name],{windowsHide:true,stdio:['pipe','pipe','pipe']});
  child.stdin.on('error',()=>{});
  const locked=await new Promise(resolve=>{let text='';child.stdout.on('data',data=>{text+=data.toString();if(text.includes('ACQUIRED'))resolve(true);});child.once('error',()=>resolve(false));child.once('close',()=>resolve(false));});
  if(locked)leaseProcess=child;
  return locked;
}
async function releaseLease(){if(!leaseProcess)return;const child=leaseProcess;leaseProcess=null;const closed=new Promise(resolve=>child.once('close',resolve));child.stdin.end('\n');await closed;}
export function validResult(result){return result&&['changed','no_change','blocked'].includes(result.status)&&typeof result.summary==='string'&&['changes','evidence','remaining'].every(k=>Array.isArray(result[k])&&result[k].every(v=>typeof v==='string'));}
async function sourceFingerprint(){
  const hash=createHash('sha256');
  async function walk(dir){for(const item of (await fs.readdir(dir,{withFileTypes:true})).sort((a,b)=>a.name.localeCompare(b.name))){const file=path.join(dir,item.name);if(item.isDirectory())await walk(file);else if(item.isFile()){hash.update(path.relative(root,file));hash.update(await fs.readFile(file));}}}
  for(const folder of ['Assets','Packages','ProjectSettings'])await walk(path.join(root,folder));
  return hash.digest('hex');
}
async function event(status,extra={}){const value={status,atUtc:iso(),...extra};await write(path.join(local,'status.json'),value);await fs.appendFile(path.join(local,'events.jsonl'),JSON.stringify(value)+'\n');return value;}
async function stopOwnTree(pid){if(!pid)return;await new Promise(resolve=>{const child=spawn('taskkill.exe',['/PID',String(pid),'/T','/F'],{windowsHide:true,stdio:'ignore'});child.once('error',resolve);child.once('close',resolve);});}
async function runProcess(command,args,{cwd=root,prompt='',runDir,timeoutMinutes=10,lockRecord}){
  const out=createWriteStream(path.join(runDir,'stdout.jsonl')),err=createWriteStream(path.join(runDir,'stderr.log'));
  const child=spawn(command,args,{cwd,windowsHide:true,stdio:['pipe','pipe','pipe']});
  const completion=new Promise(resolve=>{child.once('error',error=>resolve({error}));child.once('close',code=>resolve({code}));});
  child.stdout.pipe(out);child.stderr.pipe(err);child.stdin.on('error',()=>{});child.stdin.end(prompt,'utf8');
  if(lockRecord&&child.pid){lockRecord.child=child.pid;await write(path.join(local,'lock.json'),lockRecord);}
  let timer;const timeout=new Promise(resolve=>{timer=setTimeout(()=>resolve({timedOut:true}),timeoutMinutes*60000);});
  let outcome;
  try{outcome=await Promise.race([completion,timeout]);if(outcome.timedOut)await stopOwnTree(child.pid);}
  finally{clearTimeout(timer);await completion;out.end();err.end();if(lockRecord){delete lockRecord.child;await write(path.join(local,'lock.json'),lockRecord);}}
  if(outcome.error)throw outcome.error;
  if(outcome.timedOut)throw new Error('Owned worker reached its bounded time limit; no release was published.');
  if(outcome.code!==0)throw new Error('Child process failed ('+outcome.code+'); see '+runDir);
}
async function acquire(){
  if(!await acquireLease())return null;
  try{
    await fs.mkdir(local,{recursive:true});
    const lockFile=path.join(local,'lock.json');
    const prior=await read(lockFile);
    if(prior&&(alive(prior.pid)||(prior.child&&alive(prior.child)))){await releaseLease();return null;}
    if(prior)await fs.unlink(lockFile);
    const record={pid:process.pid,startedUtc:iso()};
    await fs.writeFile(lockFile,JSON.stringify(record),{flag:'wx'});return record;
  }catch(e){await releaseLease();if(e.code==='EEXIST')return null;throw e;}
}

async function enqueueCompleted(state,stateFile) {
  const pending=state.pendingSnapshot;
  if(!pending)return null;
  if(await sourceFingerprint()!==pending.sourceSha256)throw new Error('Completed candidate changed before snapshot; retain evidence and repair before another development round.');
  const config=await read(path.join(root,'Automation','config.json'));
  await event('snapshotting',{version:pending.version,nextRunUtc:state.nextRunUtc});
  const job=await enqueueSnapshot({sourceRoot:root,snapshotBase:config.snapshotBase||path.join(path.parse(root).root,'EasternDynastyBuild','Releases'),version:pending.version,metadata:pending.metadata});
  state.lastQueued={version:job.version,projectRoot:job.projectRoot,queuedUtc:iso()};
  delete state.pendingSnapshot;delete state.lastError;
  await write(stateFile,state);
  return job;
}
export async function tick({probe=false,dryRun=false}={}) {
  const config=await read(path.join(root,'Automation','config.json'));
  if(!validConfig(config))throw new Error('Invalid continuous-development config.');
  const stateFile=path.join(local,'state.json');
  let state=await read(stateFile,{}),hold=await read(path.join(local,'manual-hold.json'));
  let action=nextAction(config,state,{held:!!hold,probe});
  if(dryRun)return {enabled:config.enabled,intervalHours:config.intervalHours,held:!!hold,action,due:due(state),nextRunUtc:state.nextRunUtc||null,lastQueued:state.lastQueued||null,pendingSnapshot:state.pendingSnapshot?.version||null,publicationStatus:await read(path.join(root,'.local','publication','status.json'))};
  if(action==='disabled')return event('disabled');
  if(action==='held')return event('manual_development',{reason:hold.reason});
  if(action==='wait')return event('waiting',{nextRunUtc:state.nextRunUtc,lastQueued:state.lastQueued||null});
  const lock=await acquire();if(!lock)return {status:'busy'};
  const version='s1-'+iso().replace(/[-:.]/g,'').replace('T','-').replace('Z','');
  const runDir=path.join(local,'runs',version);await fs.mkdir(runDir,{recursive:true});
  let stage='starting';
  try {
    state=await read(stateFile,{});hold=await read(path.join(local,'manual-hold.json'));
    action=nextAction(config,state,{held:!!hold,probe});
    if(action==='disabled')return event('disabled');
    if(action==='held')return event('manual_development',{reason:hold.reason});
    if(action==='wait')return event('waiting',{nextRunUtc:state.nextRunUtc});
    if(action==='enqueue') {
      stage='snapshotting';const job=await enqueueCompleted(state,stateFile);
      return event('queued_for_publication',{version:job.version,projectRoot:job.projectRoot,nextRunUtc:state.nextRunUtc});
    }
    const paths=await read(path.join(local,'paths.json'));if(!paths?.codexEntry)throw new Error('Run Tools/Start-ContinuousDevelopment.ps1 first.');
    const started=Date.now(),previousRun=state.lastRun||null;
    stage='optimizing';const beforeSource=probe?null:await sourceFingerprint();
    const resultFile=path.join(runDir,'worker-result.json');
    let rag=null;
    if(!probe) {
      state.nextRunUtc=new Date(started+config.intervalHours*3600000).toISOString();state.lastRun=runDir;
      await write(stateFile,state);
      await event('reading_rag',{version,runDirectory:runDir,nextRunUtc:state.nextRunUtc});
      try {rag=await syncRag({projectRoot:root,ragRoot:path.join(path.dirname(root),'rag'),runDir,stateDir:path.join(local,'rag')});}
      catch(e){rag={status:'error',message:e.message};await write(path.join(runDir,'rag-error.json'),rag);}
      state.lastRagRead={atUtc:iso(),...rag};await write(stateFile,state);
    }
    const publisher={status:await read(path.join(root,'.local','publication','status.json')),lastFailure:await read(path.join(root,'.local','publication','last-failure.json'))};
    const context={version,projectRoot:root,ragRoot:path.join(path.dirname(root),'rag'),previousRun,lastError:state.lastError||null,publisher,rag,backlog:path.join(root,'Docs','Hourly-UI-Gameplay-Backlog.md'),deadlineUtc:new Date(started+Math.min(35,config.workerTimeoutMinutes-5)*60000).toISOString()};
    const template=probe?'仅读取 ProjectSettings/ProjectVersion.txt，返回 status=no_change，summary 包含实际 Unity 版本和调度可用，changes/remaining 为空数组，evidence 为文件路径。禁止修改文件、启动构建或子代理。':await fs.readFile(path.join(root,'Automation','iteration-prompt.txt'),'utf8');
    const prompt=template+'\n\n'+JSON.stringify(context,null,2);await fs.writeFile(path.join(runDir,'prompt.txt'),prompt);
    const args=[paths.codexEntry,'--no-daemon','-a','never','--search','exec','--skip-git-repo-check','--sandbox',probe?'read-only':'workspace-write','-c','sandbox_workspace_write.network_access=true','--json','--color','never','-C',root,'--add-dir',local,'--output-schema',path.join(root,'Automation','worker-result.schema.json'),'--output-last-message',resultFile];
    if(!probe)args.push('--add-dir',path.join(path.dirname(root),'rag'));
    args.push('-');
    await event(probe?'probing':'optimizing',{version,runDirectory:runDir,rag,nextRunUtc:state.nextRunUtc});
    await runProcess(paths.node,args,{prompt,runDir,timeoutMinutes:probe?3:config.workerTimeoutMinutes,lockRecord:lock});
    const result=await read(resultFile);if(!validResult(result))throw new Error('Invalid structured worker result.');
    if(probe)return event('probe_passed',{result,runDirectory:runDir});
    const changed=await sourceFingerprint()!==beforeSource;
    if(changed&&result.status!=='changed')throw new Error('Worker changed playable source but did not report completed work; repair before publishing.');
    state.lastResult={version,resultFile,status:result.status,actualSourceChanged:changed,atUtc:iso()};
    if(!changed) {
      await write(stateFile,state);
      return event(result.status==='blocked'?'learning_blocked':'no_change',{result,rag,nextRunUtc:state.nextRunUtc});
    }
    state.pendingSnapshot={version,sourceSha256:await sourceFingerprint(),metadata:{workerResult:resultFile,summary:result.summary,ragSnapshot:rag?.snapshotPath||null,ragDelta:rag?.deltaPath||null}};
    await write(stateFile,state);
    stage='snapshotting';const job=await enqueueCompleted(state,stateFile);
    return event('queued_for_publication',{version:job.version,projectRoot:job.projectRoot,result,nextRunUtc:state.nextRunUtc});
  } catch(e) {
    state.lastError={message:e.message,stage,runDirectory:runDir,atUtc:iso()};
    await write(stateFile,state);
    await event('failed',{...state.lastError,nextRunUtc:state.nextRunUtc||null});throw e;
  } finally {
    try{const latest=await read(path.join(local,'lock.json'));if(latest?.pid===process.pid)await fs.unlink(path.join(local,'lock.json'));}finally{await releaseLease();}
  }
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)) {
  tick({probe:process.argv.includes('--probe'),dryRun:process.argv.includes('--dry-run')}).then(x=>console.log(JSON.stringify(x))).catch(e=>{console.error(e.message);process.exitCode=1;});
}
