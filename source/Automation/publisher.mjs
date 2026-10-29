import fs from 'node:fs/promises';
import {createWriteStream} from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawn} from 'node:child_process';
import {createHash} from 'node:crypto';
import {verifyValidatedSources} from './release-evidence.mjs';

const installedRoot=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const fold=value=>process.platform==='win32'?value.toLowerCase():value;
const inside=(root,file)=>fold(file).startsWith(fold(root+path.sep));
const sha=bytes=>createHash('sha256').update(bytes).digest('hex');
const read=async(file,fallback=null)=>{try{return JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));}catch(e){if(e.code==='ENOENT')return fallback;throw e;}};
async function write(file,value){await fs.mkdir(path.dirname(file),{recursive:true});const temporary=file+'.'+process.pid+'.tmp';await fs.writeFile(temporary,JSON.stringify(value,null,2),'utf8');await fs.rename(temporary,file);}
const fileSha=async file=>sha(await fs.readFile(file));
const alive=pid=>{try{process.kill(pid,0);return true;}catch{return false;}};
const requiredChecks=['core','assets','unity-repository','routes','youzhou-1440','youzhou-1280','mobile-1600','legacy','mouse','mouse-s1','experience'];
const deferred=new Set(['waiting_for_player','waiting_for_desktop']);

export async function sourceFingerprint(projectRoot){
  const hash=createHash('sha256');
  async function walk(dir){for(const item of (await fs.readdir(dir,{withFileTypes:true})).sort((a,b)=>a.name.localeCompare(b.name))){const file=path.join(dir,item.name);if(item.isDirectory())await walk(file);else if(item.isFile()){hash.update(path.relative(projectRoot,file));hash.update(await fs.readFile(file));}else throw new Error('Unsupported snapshot source entry: '+file);}}
  for(const folder of ['Assets','Packages','ProjectSettings'])await walk(path.join(projectRoot,folder));
  return hash.digest('hex');
}

async function stopOwnTree(pid){if(!pid)return;await new Promise(resolve=>{const child=spawn('taskkill.exe',['/PID',String(pid),'/T','/F'],{windowsHide:true,stdio:'ignore'});child.once('error',resolve);child.once('close',resolve);});}
async function runProcess(command,args,{cwd,runDir,prompt='',timeoutMinutes=10,lock,lockFile}){
  await fs.mkdir(runDir,{recursive:true});
  const out=createWriteStream(path.join(runDir,'stdout.jsonl')),err=createWriteStream(path.join(runDir,'stderr.log'));
  const child=spawn(command,args,{cwd,windowsHide:true,stdio:['pipe','pipe','pipe']});
  const completion=new Promise(resolve=>{child.once('error',error=>resolve({error}));child.once('close',code=>resolve({code}));});
  child.stdout.pipe(out);child.stderr.pipe(err);child.stdin.on('error',()=>{});child.stdin.end(prompt,'utf8');
  let timer,outcome;
  try{
    if(lock&&child.pid){lock.child=child.pid;await write(lockFile,lock);}
    outcome=await Promise.race([completion,new Promise(resolve=>{timer=setTimeout(()=>resolve({timedOut:true}),timeoutMinutes*60000);})]);
    if(outcome.timedOut)await stopOwnTree(child.pid);
  }finally{
    clearTimeout(timer);await completion;out.end();err.end();
    if(lock){delete lock.child;await write(lockFile,lock);}
  }
  if(outcome.error)throw outcome.error;
  if(outcome.timedOut)throw new Error('Publication subprocess exceeded its time limit: '+runDir);
  if(outcome.code!==0)throw new Error('Publication subprocess failed ('+outcome.code+'): '+runDir);
}

async function acquirePublisherLease({sourceRoot,local,powershell}){
  const name='Local\\EasternDynasty-Publication-'+sha(sourceRoot.toLowerCase()).slice(0,16);
  const child=spawn(powershell,['-NoProfile','-ExecutionPolicy','Bypass','-File',path.join(sourceRoot,'Automation','lease.ps1'),'-Name',name],{windowsHide:true,stdio:['pipe','pipe','pipe']});
  child.stdin.on('error',()=>{});child.stderr.resume();
  let timer;
  const acquired=await new Promise(resolve=>{let output='';timer=setTimeout(()=>resolve(false),10000);child.stdout.on('data',data=>{output+=data.toString();if(output.includes('ACQUIRED'))resolve(true);});child.once('error',()=>resolve(false));child.once('close',()=>resolve(false));});
  clearTimeout(timer);
  if(!acquired){child.stdin.end('\n');if(child.exitCode===null)child.kill();return null;}
  const release=async()=>{if(child.exitCode!==null)return;const closed=new Promise(resolve=>child.once('close',resolve));child.stdin.end('\n');await closed;};
  try{
    await fs.mkdir(local,{recursive:true});
    const lockFile=path.join(local,'lock.json'),prior=await read(lockFile);
    if(prior&&(alive(prior.pid)||(prior.child&&alive(prior.child)))){await release();return null;}
    if(prior)await fs.unlink(lockFile);
    const lock={pid:process.pid,startedUtc:new Date().toISOString()};
    await fs.writeFile(lockFile,JSON.stringify(lock),{flag:'wx'});
    return {lock,lockFile,release:async()=>{try{if((await read(lockFile))?.pid===process.pid)await fs.unlink(lockFile);}finally{await release();}}};
  }catch(error){await release();if(error.code==='EEXIST')return null;throw error;}
}

function validateJobPaths(job,sourceRoot){
  if(!job||!/^[A-Za-z0-9][A-Za-z0-9._-]*$/.test(job.version))throw new Error('Invalid publication version.');
  for(const key of ['sourceRoot','snapshotRoot','projectRoot','inputRoot'])if(typeof job[key]!=='string'||!path.isAbsolute(job[key]))throw new Error('Invalid snapshot path: '+key);
  if(fold(path.resolve(job.sourceRoot))!==fold(sourceRoot))throw new Error('Snapshot belongs to another source project.');
  const snapshot=path.resolve(job.snapshotRoot),project=path.resolve(job.projectRoot),input=path.resolve(job.inputRoot);
  if(!inside(snapshot,project)||!inside(snapshot,input)||project===input||inside(sourceRoot,snapshot)||inside(snapshot,sourceRoot)||fold(snapshot)===fold(sourceRoot))throw new Error('Publication must use an isolated snapshot outside the development project.');
}

async function validationReceipt(job){
  const file=path.join(job.projectRoot,'Validation','Releases',job.version,'validation.json'),value=await read(file);
  if(value?.version!==job.version||!value.savesUnchanged||!/^[a-fA-F0-9]{64}$/.test(value.sourceSha256)||!requiredChecks.every(check=>value.checks?.includes(check)))throw new Error('Full current-version validation receipt is missing or incomplete.');
  await verifyValidatedSources(job.projectRoot,value);
  return {file,value};
}

export async function verifyVisualEvidence(job,review){
  if(!review||review.status!=='no_change'||typeof review.summary!=='string'||!['changes','evidence','remaining'].every(key=>Array.isArray(review[key])&&review[key].every(value=>typeof value==='string'))||review.changes.length)throw new Error('Visual reviewer rejected the candidate or returned an invalid result.');
  const expected=['youzhou-1440x900','youzhou-1280x720','mobile-1600x720'].map(suffix=>path.join(job.projectRoot,'Validation','Visual',job.version+'-'+suffix));
  const allowed=await Promise.all(expected.map(dir=>fs.realpath(dir)));
  if(allowed.some((dir,index)=>fold(dir)!==fold(expected[index])))throw new Error('Screenshot directories cannot escape through reparse points.');
  const inspected=new Map();let mobile=false;
  for(const file of review.evidence.filter(file=>/\.png$/i.test(file))){
    try{
      const actual=await fs.realpath(file);
      if(allowed.some(dir=>inside(dir,actual))&&(await fs.stat(actual)).isFile()){
        inspected.set(fold(actual),{path:actual,sha256:await fileSha(actual)});
        if(inside(allowed[2],actual))mobile=true;
      }
    }catch{}
  }
  if(inspected.size<4||!mobile)throw new Error('Visual review requires four distinct current screenshots including a mobile screenshot.');
  return [...inspected.values()];
}

async function verifyApproval(job,checkpoint){
  if(!checkpoint||checkpoint.version!==job.version)throw new Error('Missing publication approval checkpoint.');
  const validation=await validationReceipt(job);
  if(await fileSha(validation.file)!==checkpoint.validationSha256||await sourceFingerprint(job.projectRoot)!==checkpoint.sourceSha256)throw new Error('Validated snapshot or its receipt changed after visual approval.');
  const reviewFile=path.join(job.projectRoot,'Validation','Releases',job.version,'visual-review.json');
  if(await fileSha(reviewFile)!==checkpoint.visualReviewSha256)throw new Error('Visual approval receipt changed.');
  const evidence=await verifyVisualEvidence(job,await read(reviewFile));
  if(JSON.stringify(evidence)!==JSON.stringify(checkpoint.visualEvidence))throw new Error('Reviewed screenshots changed after approval.');
  return validation.value;
}

export async function verifyAndroidReceipt(receipt,{destination,buildStarted=0}){
  if(receipt?.status!=='built-and-package-verified'||receipt.applicationId!=='com.easterndynasty.s1'||!receipt.signatureVerified||!receipt.packageVerified||!receipt.sourceProjectSettingsUnchanged||!receipt.sourceSnapshotVerified||!(Date.parse(receipt.builtUtc)>=buildStarted))throw new Error('Fresh signed Android package receipt was not verified.');
  if(typeof receipt.apk!=='string'||!path.isAbsolute(receipt.apk)||fold(path.dirname(path.resolve(receipt.apk)))!==fold(path.resolve(destination))||!/^[a-fA-F0-9]{64}$/.test(receipt.sha256))throw new Error('Android artifact is outside the configured destination.');
  const stat=await fs.lstat(receipt.apk);
  if(!stat.isFile()||stat.isSymbolicLink()||fold(await fs.realpath(receipt.apk))!==fold(path.resolve(receipt.apk))||await fileSha(receipt.apk)!==receipt.sha256.toLowerCase())throw new Error('Android artifact hash or path does not match its verified receipt.');
  return receipt;
}

function visualPrompt(job){
  return `用户2026-10-27要求专业UI工具及原作参考先行。若快照有Docs/Professional-UI-Workflow.md，必须读取它和本轮UI记录，核对可编辑设计源稿、真实工具导出收据/预览，并实际查看设计预览及对应游戏运行图，对照原作构图。仅代码自绘美术、只有截图嵌入SVG的假组件、只有测量模板/文字说明、未调用工具却声称设计完成，均不满足新UI交付要求，返回blocked说明可执行修正项。不要把工具导出成功当作游戏画面通过。旧快照无该文件时不伪造追溯材料。\n\n用户最新反馈：界面仍太老土，与《率土之滨》差距大。视觉审查必须检查实际构图、图像占比、文字密度、字体层级、材质与风格一致性，不能仅凭功能通过或新按钮判定美术改善。若当前冻结快照包含 Docs/UI-Iteration-Requirements.md，必须读取该文件与 Docs/UIIterations/${job.version}.md，实际查看本轮改动页面的新截图，并与记录中的原作/游戏基线对比；没有实质UI变化、缺少本轮UI记录/目标页面新截图，或仍有明显可读性/操作问题时返回blocked并给出具体修正项。旧候选无此文件时不要伪造追溯记录，但仍检查其实际视觉问题。\n\n你负责 Eastern Dynasty 发布前的只读视觉审查。禁止编辑代码、存档，禁止关闭用户游戏。当前冻结版本：${job.version}。必须实际使用图像查看工具查看至少四张当前 PNG，至少一张手机横屏截图，涵盖 overview、武将卡牌详情、主城、任务；存在练兵和战斗界面时也查看相应证据。\n截图目录：\n${['youzhou-1440x900','youzhou-1280x720','mobile-1600x720'].map(suffix=>path.join(job.projectRoot,'Validation','Visual',job.version+'-'+suffix)).join('\n')}\n只检查当前快照 ${job.projectRoot}，结合 Validation/Releases/${job.version} 的真实点击和回归结果，与冻结输入元数据及本地 ${path.join(path.dirname(job.sourceRoot),'rag')} 的原游戏参考图比较布局、中文裁切、按钮可达性、卡面可读性、主城/练兵/任务导航。不要比较正在开发的主源码。无法实际查看、关键错误或本轮预期改动缺失时 status=blocked；可发布时 status=no_change。changes 必须为空；evidence 必须列实际查看的完整 PNG 路径；remaining 如实列差距，不把回归通过称为全部 UI 还原。summary 使用中文。\n冻结任务元数据：${JSON.stringify(job.metadata||{},null,2)}`;
}

/** Explicit dependency injection is for isolated tests. CLI always uses the real
 * queue, Windows lease, subprocesses and all validation/visual/package gates. */
export async function tickPublication({sourceRoot=installedRoot,dryRun=false}={},dependencies={}){
  sourceRoot=path.resolve(sourceRoot);
  const local=path.join(sourceRoot,'.local','publication');
  const config=await read(path.join(sourceRoot,'Automation','config.json'));
  if(!config||typeof config.enabled!=='boolean')throw new Error('Invalid publication config.');
  const queue=dependencies.queue||await import('./publication-queue.mjs');
  const now=dependencies.now||Date.now;
  const iso=()=>new Date(now()).toISOString();
  const enabled=config.publicationEnabled??config.enabled;
  const hold=await read(path.join(local,'manual-hold.json'));
  if(dryRun){const current=await queue.readPublicationState({sourceRoot});return {enabled,held:!!hold,active:current.active,queue:current.jobs.map(({job,state})=>({version:job.version,status:state.status,projectRoot:job.projectRoot})),destinationWindows:path.join(path.dirname(sourceRoot),'games'),destinationAndroid:path.join(path.dirname(sourceRoot),'apk')};}
  async function event(status,extra={}){const value={status,atUtc:iso(),lastFailure:await read(path.join(local,'last-failure.json')),...extra};await write(path.join(local,'status.json'),value);await fs.appendFile(path.join(local,'events.jsonl'),JSON.stringify(value)+'\n');return value;}
  if(!enabled)return event('disabled');
  if(hold)return event('held',{reason:hold.reason});
  const paths=await read(path.join(sourceRoot,'.local','continuous-development','paths.json'));
  if(!paths?.powershell||!paths.node||!paths.codexEntry)throw new Error('Publication needs the existing local node/PowerShell/Codex paths.');
  const lease=await (dependencies.acquireLease||acquirePublisherLease)({sourceRoot,local,powershell:paths.powershell});
  if(!lease)return {status:'busy'};
  let job,state,runDir,jobPathsVerified=false,stage='claiming';
  const processRunner=dependencies.runProcess||runProcess;
  async function summary(status,extra={}){
    if(!jobPathsVerified)return;
    const value={version:job.version,status,atUtc:iso(),projectRoot:job.projectRoot,evidence:path.join(job.projectRoot,'Validation','Releases',job.version),runDirectory:runDir||null,sourceSha256:state?.checkpoint?.sourceSha256||null,windows:state?.windows||null,android:state?.android||null,...extra};
    // Only this version's summary is mirrored. In particular never overwrite
    // release.json belonging to the earlier in-place publication pipeline.
    await write(path.join(sourceRoot,'Validation','Releases',job.version,'publication.json'),value);
    await write(path.join(local,'history',job.version+'.json'),value);
    return value;
  }
  try{
    if(await read(path.join(local,'manual-hold.json')))return event('held');
    const claimed=await queue.claimNextPublication({sourceRoot});
    if(!claimed)return event('idle');
    ({job,state}=claimed);validateJobPaths(job,sourceRoot);jobPathsVerified=true;
    if(state.retryAfterUtc&&Date.parse(state.retryAfterUtc)>now())return event('waiting_retry',{version:job.version,retryAfterUtc:state.retryAfterUtc});
    stage='snapshot_verification';await queue.verifyPublicationSnapshot(job);
    runDir=path.join(job.snapshotRoot,'PublicationRuns',iso().replace(/[:.]/g,'-')+'-'+process.pid);
    await fs.mkdir(runDir,{recursive:true});
    const releaseDir=path.join(job.projectRoot,'Validation','Releases',job.version);
    const destinations={windows:path.join(path.dirname(sourceRoot),'games'),android:path.join(path.dirname(sourceRoot),'apk')};
    async function progress(status,patch={}){state=await queue.updatePublicationJob({sourceRoot,version:job.version,status,patch});await summary(status,{lastFailure:state.lastFailure||null,retryAfterUtc:state.retryAfterUtc||null});await event(status,{version:job.version,projectRoot:job.projectRoot,runDirectory:runDir,...patch});}
    async function run(command,args,phase,timeoutMinutes,prompt=''){await processRunner(command,args,{cwd:job.projectRoot,runDir:path.join(runDir,phase),timeoutMinutes,prompt,lock:lease.lock,lockFile:lease.lockFile});}
    const gateArgs=['-NoProfile','-ExecutionPolicy','Bypass','-File',path.join(job.projectRoot,'Tools','Publish-Iteration.ps1'),'-Version',job.version,'-DestinationPath',destinations.windows];

    if(!state.checkpoint){
      stage='validating';await progress(stage);
      // This gate performs headless checks/build before testing desktop idleness,
      // and reuses its exact source/build preparation receipt on later attempts.
      await run(paths.powershell,[...gateArgs,'-ValidateOnly'],'validation',config.releaseTimeoutMinutes||35);
      const release=await read(path.join(releaseDir,'release.json'));
      if(release?.version!==job.version)throw new Error('Publication gate returned another version.');
      if(deferred.has(release.status)){await progress(release.status,{release});return {status:release.status,version:job.version,release};}
      if(release.status!=='validated')throw new Error('Publication gate did not complete full validation.');
      const validation=await validationReceipt(job),validatedFingerprint=await sourceFingerprint(job.projectRoot);
      stage='visual_review';await progress(stage);
      const reviewDir=path.join(runDir,'visual-review'),reviewFile=path.join(reviewDir,'result.json');
      await fs.mkdir(reviewDir,{recursive:true});
      const prompt=visualPrompt(job);await fs.writeFile(path.join(reviewDir,'prompt.txt'),prompt);
      await run(paths.node,[paths.codexEntry,'--no-daemon','-a','never','exec','--skip-git-repo-check','--sandbox','read-only','--json','--color','never','-C',job.projectRoot,'--output-schema',path.join(job.projectRoot,'Automation','worker-result.schema.json'),'--output-last-message',reviewFile,'-'],'visual-review',15,prompt);
      const review=await read(reviewFile),visualEvidence=await verifyVisualEvidence(job,review);
      await verifyValidatedSources(job.projectRoot,validation.value);
      if(await sourceFingerprint(job.projectRoot)!==validatedFingerprint)throw new Error('Snapshot changed during visual review.');
      const visualReceiptPath=path.join(releaseDir,'visual-review.json');
      await write(visualReceiptPath,{...review,inspected:visualEvidence,atUtc:iso()});
      const checkpoint={version:job.version,sourceSha256:validatedFingerprint,validationSha256:await fileSha(validation.file),visualReviewSha256:await fileSha(visualReceiptPath),visualEvidence,approvedUtc:iso()};
      await write(path.join(releaseDir,'rag-application-review.json'),{...job.metadata,version:job.version,atUtc:iso(),sourceSha256:validatedFingerprint,status:'worker-changes-validated',note:'Links the frozen developer changes to their RAG inputs; does not claim every reference is imported.'});
      await progress('approved',{checkpoint});
    }

    stage='approval_verification';await verifyApproval(job,state.checkpoint);
    if(state.windows?.status!=='published'){
      stage='installing';await progress(stage);
      await run(paths.powershell,[...gateArgs,'-InstallVerified'],'install',10);
      const release=await read(path.join(releaseDir,'release.json'));
      if(release?.version!==job.version||!['published',...deferred].includes(release.status))throw new Error('Windows installation produced neither a successful nor deferred receipt.');
      await progress(release.status==='published'?'windows_published':release.status,{windows:release});
    }

    if(config.publishAndroid&&!state.android){
      stage='approval_verification';await verifyApproval(job,state.checkpoint);
      stage='android';await progress('building_android',{androidAttempts:(state.androidAttempts||0)+1});
      const buildStarted=now();
      await run(paths.powershell,['-NoProfile','-ExecutionPolicy','Bypass','-File',path.join(job.projectRoot,'Tools','Build-Android.ps1'),'-VersionName','0.2.0-s1','-DestinationPath',destinations.android,'-ReplacePrevious','-CleanWorkspaceOnSuccess'],'android',config.androidTimeoutMinutes||30);
      const receipt=await read(path.join(destinations.android,'build-info.json'));
      await verifyAndroidReceipt(receipt,{destination:destinations.android,buildStarted});
      await verifyApproval(job,state.checkpoint);
      const android={...receipt,releaseVersion:job.version,sourceSha256:state.checkpoint.sourceSha256,ragSnapshot:job.metadata?.ragSnapshot||null};
      await write(path.join(releaseDir,'android-release.json'),android);
      await progress('android_published',{android,retryAfterUtc:null,lastFailure:null});
    }else if(state.android){
      stage='android_verification';await verifyAndroidReceipt(state.android,{destination:destinations.android});
      if(state.android.releaseVersion!==job.version||state.android.sourceSha256!==state.checkpoint.sourceSha256)throw new Error('Saved Android receipt belongs to another approved snapshot.');
    }

    const complete=state.windows?.status==='published'&&(!config.publishAndroid||state.android);
    const status=complete?'published':state.windows.status;
    if(complete){await fs.unlink(path.join(local,'last-failure.json')).catch(error=>{if(error.code!=='ENOENT')throw error;});}
    await progress(status,{retryAfterUtc:null,lastFailure:null});
    return event(status,{...await summary(status),lastFailure:complete?null:await read(path.join(local,'last-failure.json'))});
  }catch(error){
    const failure={version:job?.version||null,message:error.message,stage,runDirectory:runDir||null,projectRoot:job?.projectRoot||null,atUtc:iso()};
    await write(path.join(local,'last-failure.json'),failure);
    if(job){
      const retryable=stage==='android'&&(state?.androidAttempts||0)<2;
      const retryAfterUtc=retryable?new Date(now()+15*60000).toISOString():null;
      await queue.updatePublicationJob({sourceRoot,version:job.version,status:retryable?'android_retry':'failed',patch:{lastFailure:failure,retryAfterUtc}});
      await summary(retryable?'android_retry':'failed',{lastFailure:failure,retryAfterUtc});
    }
    await event('failed',{version:job?.version||null,lastFailure:failure});throw error;
  }finally{await lease.release();}
}

if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
  const rootIndex=process.argv.indexOf('--source-root');
  if(rootIndex!==-1&&!process.argv[rootIndex+1]){console.error('--source-root needs an absolute directory.');process.exitCode=1;}
  else tickPublication({sourceRoot:rootIndex===-1?installedRoot:process.argv[rootIndex+1],dryRun:process.argv.includes('--dry-run')}).then(result=>console.log(JSON.stringify(result))).catch(error=>{console.error(error.message);process.exitCode=1;});
}
