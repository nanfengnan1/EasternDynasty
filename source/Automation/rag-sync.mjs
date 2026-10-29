import fs from 'node:fs/promises';
import path from 'node:path';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

const moduleRoot=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const themes=['主城 城内设施 UI','任务 新手 引导','练兵 扫荡','战斗 战报 回放'];
const hash=value=>createHash('sha256').update(value).digest('hex');
const text=(value,limit=1800)=>typeof value==='string'?value.slice(0,limit):'';
const array=value=>Array.isArray(value)?value:[];
const safeId=value=>typeof value==='string'&&/^[A-Za-z0-9_.:-]{1,180}$/.test(value)?value:null;
const stable=value=>JSON.stringify(value,(_,v)=>v&&typeof v==='object'&&!Array.isArray(v)?Object.fromEntries(Object.keys(v).sort().map(k=>[k,v[k]])):v);
const errorCode=error=>String(error?.code||error?.name||'READ_FAILED').replace(/[^A-Za-z0-9_-]/g,'').slice(0,80);
async function readJson(file,maxBytes=8*1024*1024){const info=await fs.stat(file);if(info.size>maxBytes)throw Object.assign(new Error('JSON limit exceeded'),{code:'JSON_TOO_LARGE'});return JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));}
async function optionalJson(file){try{return await readJson(file);}catch(error){if(error.code==='ENOENT')return null;throw error;}}
async function writeJson(file,value){await fs.mkdir(path.dirname(file),{recursive:true});const temporary=file+'.'+process.pid+'.tmp';await fs.writeFile(temporary,JSON.stringify(value,null,2),'utf8');await fs.rename(temporary,file);}

export function validateBaseUrl(value){
  const url=new URL(value);
  if(url.protocol!=='http:'||!['127.0.0.1','localhost','[::1]'].includes(url.hostname)||url.username||url.password||url.search||url.hash||url.pathname!=='/')throw new Error('RAG endpoint must be a plain HTTP loopback origin.');
  return url.origin;
}
export function referencedPath(root,relative){
  if(typeof relative!=='string'||!relative||path.isAbsolute(relative)||relative.includes('\0'))return null;
  const target=path.resolve(root,relative);
  return target.startsWith(path.resolve(root)+path.sep)?target:null;
}
export async function fetchJson(endpoint,{baseUrl,token,fetchImpl=fetch,timeoutMs=8000,maxBytes=8*1024*1024}){
  const origin=validateBaseUrl(baseUrl),url=new URL(endpoint,origin);
  if(url.origin!==origin||!endpoint.startsWith('/')||endpoint.startsWith('//'))throw new Error('RAG query must stay on its loopback origin.');
  const controller=new AbortController(),timer=setTimeout(()=>controller.abort(),timeoutMs);
  try{
    const response=await fetchImpl(url,{method:'GET',redirect:'error',headers:{Authorization:'Bearer '+token},signal:controller.signal});
    if(!response.ok)throw Object.assign(new Error('RAG HTTP error'),{code:'HTTP_'+response.status});
    if(Number(response.headers.get('content-length')||0)>maxBytes)throw Object.assign(new Error('RAG response limit exceeded'),{code:'RESPONSE_TOO_LARGE'});
    let bytes=0;const chunks=[];
    for await(const chunk of response.body){bytes+=chunk.length;if(bytes>maxBytes){controller.abort();throw Object.assign(new Error('RAG response limit exceeded'),{code:'RESPONSE_TOO_LARGE'});}chunks.push(chunk);}
    return JSON.parse(Buffer.concat(chunks).toString('utf8'));
  }finally{clearTimeout(timer);}
}
function briefObject(value,max=2400){if(value==null)return null;const encoded=stable(value);return encoded.length<=max?value:{excerpt:encoded.slice(0,max),truncated:true};}
function versionRecord(record){
  const metadata=record.metadata||{};
  return {id:text(record.id,180),dataset:text(record.dataset,180),sourceHash:text(record.source_hash,64),active:record.active===true,createdAt:text(record.created_at,80),sourceStatus:text(record.source_status,80),counts:briefObject(record.counts,1000),scope:briefObject(metadata.scope),summary:text(metadata.summary,1400),batchId:text(metadata.batch_id,180),questionIds:array(metadata.question_ids).slice(0,30),facts:array(metadata.research_facts).slice(0,8).map(f=>({recordId:text(f.record_id,180),entityId:text(f.entity_id,180),entityType:text(f.entity_type,60),predicate:text(f.predicate,180),object:briefObject(f.object),scope:briefObject(f.scope),evidenceStatus:text(f.evidence_status,80),evidence:array(f.evidence).slice(0,4).map(e=>({sourceId:text(e.source_id,180),locator:text(e.locator||e.location||e.description,1000)}))})),factCount:array(metadata.research_facts).length};
}
function assetRecord(record,ragRoot){
  const metadata=record.metadata||{};
  return {id:text(record.id,180),versionId:text(record.version_id,180),title:text(record.title,500),sha256:text(record.sha256,64),category:text(record.category,100),subcategory:text(record.subcategory,100),width:record.width,height:record.height,bytes:Number(record.bytes)||null,s1Status:text(record.s1_status,80),verificationStatus:text(record.verification_status,80),sourceIds:array(record.source_ids).slice(0,20),relativePath:text(record.relative_path,600),localPath:referencedPath(ragRoot,record.relative_path),detailEndpoint:text(record.detail_url,900),fileEndpoint:text(record.file_url,900),scope:briefObject(metadata.scope),role:text(metadata.asset_role,100),excludedFromS1:metadata.exclude_from_current_s1_asset_set===true,note:text(metadata.note,800)};
}
function searchRecord(record,ragRoot){return {id:text(record.id,180),documentId:text(record.document_id,180),versionId:text(record.version_id,180),title:text(record.title,500),content:text(record.content,4000),s1Status:text(record.s1_status,80),verificationStatus:text(record.verification_status,80),sourceIds:array(record.source_ids).slice(0,20),scope:briefObject(record.document_metadata?.scope),documentEndpoint:text(record.document_url,900),localPath:referencedPath(ragRoot,record.relative_path),sources:array(record.sources).slice(0,5).map(s=>({id:text(s.id,180),title:text(s.title,400),url:text(s.url,1200),publishedAt:text(s.published_at,80)}))};}
export function diffRecords(previous,current,key){
  const old=new Map(array(previous).map(record=>[key(record),record]));const present=new Set();
  const added=[],changed=[];
  for(const record of current){const id=key(record);if(present.has(id))continue;present.add(id);if(!old.has(id))added.push(id);else if(stable(old.get(id))!==stable(record))changed.push(id);}
  return {added,changed,removed:[...old.keys()].filter(id=>!present.has(id)).sort()};
}
export function imageGroups(assets){
  const groups=new Map();
  for(const asset of assets){if(!/^[a-f0-9]{64}$/i.test(asset.sha256||''))continue;const sha=asset.sha256.toLowerCase();if(!groups.has(sha))groups.set(sha,[]);groups.get(sha).push({id:asset.id,versionId:asset.versionId});}
  return [...groups].sort(([a],[b])=>a.localeCompare(b)).map(([sha256,identities])=>({sha256,identities}));
}
async function localInventory(ragRoot,{maxFiles=768,maxTotalBytes=32*1024*1024}={}){
  const files=[],errors=[];let totalBytes=0,truncated=false;
  async function walk(directory,depth=0){
    if(depth>6){truncated=true;return;}
    let entries;try{entries=await fs.readdir(directory,{withFileTypes:true});}catch(error){errors.push({path:path.relative(ragRoot,directory),code:errorCode(error)});return;}
    for(const entry of entries.sort((a,b)=>a.name.localeCompare(b.name))){
      if(files.length>=maxFiles){truncated=true;return;}
      const absolute=path.join(directory,entry.name);
      if(entry.isSymbolicLink()){errors.push({path:path.relative(ragRoot,absolute),code:'SYMLINK_SKIPPED'});continue;}
      if(entry.isDirectory()){await walk(absolute,depth+1);continue;}
      if(!entry.isFile()||!(/\.(json|md|txt|csv)$/i.test(entry.name)))continue;
      try{
        const info=await fs.stat(absolute);if(info.size>8*1024*1024||totalBytes+info.size>maxTotalBytes){truncated=true;continue;}
        const bytes=await fs.readFile(absolute);totalBytes+=bytes.length;
        files.push({path:path.relative(ragRoot,absolute).split(path.sep).join('/'),sha256:hash(bytes),bytes:bytes.length});
      }catch(error){errors.push({path:path.relative(ragRoot,absolute),code:errorCode(error)});}
    }
  }
  await walk(path.join(ragRoot,'imports'));
  // Deliberately exclude service configuration/authentication and volatile logs.
  for(const relative of ['question.md','docs/持续学习批次与更新接口.txt','docs/其他AI如何访问.txt']){
    try{const bytes=await fs.readFile(path.join(ragRoot,relative));if(bytes.length>1024*1024){truncated=true;continue;}files.push({path:relative,sha256:hash(bytes),bytes:bytes.length});}catch(error){errors.push({path:relative,code:errorCode(error)});}
  }
  return {files:files.sort((a,b)=>a.path.localeCompare(b.path)),errors,truncated,totalBytes};
}
function fallbackVersions(update){
  return [...array(update?.legacy?.results),...array(update?.batches)].filter(v=>safeId(v.version)&&safeId(v.dataset)).map(v=>({id:v.version,dataset:v.dataset,active:true,source_status:'local-update-record',metadata:{batch_id:v.batch_id||null}}));
}

/** Observe fresh local RAG knowledge. This never imports assets or marks facts applied. */
export async function syncRag({projectRoot=moduleRoot,ragRoot=path.join(path.dirname(projectRoot),'rag'),runDir,stateDir=path.join(projectRoot,'.local','continuous-development','rag-sync'),baseUrl='http://127.0.0.1:8098',fetchImpl=fetch,timeoutMs=8000,maxAssets=2048,offline=false}={}){
  if(!Number.isInteger(maxAssets)||maxAssets<1||maxAssets>2048)throw new Error('Asset observation limit must be 1..2048.');
  const startedAt=new Date().toISOString();baseUrl=validateBaseUrl(baseUrl);
  const snapshotId='rag-'+startedAt.replace(/[-:.]/g,'').replace('T','-').replace('Z','')+'-'+process.pid;
  const outputDir=runDir?path.join(runDir,'rag'):path.join(stateDir,'runs',snapshotId);
  const snapshotPath=path.join(outputDir,'snapshot.json'),deltaPath=path.join(outputDir,'delta.json'),briefPath=path.join(outputDir,'brief.json'),seenPath=path.join(stateDir,'seen.json');
  const warnings=[],errors=[];
  let prior=null,updateLocal=null;
  try{prior=await optionalJson(seenPath);}catch(error){errors.push({part:'previous-seen',code:errorCode(error)});}
  try{updateLocal=await optionalJson(path.join(ragRoot,'status','knowledge-update.json'));}catch(error){errors.push({part:'local-update-status',code:errorCode(error)});}
  const inventory=await localInventory(ragRoot);errors.push(...inventory.errors.map(e=>({...e,part:'local-inventory'})));
  let token='',health=null,updates=null,versions=null,assets=[],assetsComplete=false,references=[];
  const queryEvidence=[];
  const query=async(endpoint)=>{
    try{const value=await fetchJson(endpoint,{baseUrl,token,fetchImpl,timeoutMs});queryEvidence.push({endpoint,readAtUtc:new Date().toISOString(),ok:true});return value;}
    catch(error){const failure={endpoint,code:errorCode(error)};errors.push(failure);queryEvidence.push({...failure,readAtUtc:new Date().toISOString(),ok:false});return null;}
  };
  if(!offline){
    try{const auth=await readJson(path.join(ragRoot,'config','service-auth.json'),65536);if(typeof auth.token!=='string'||!auth.token)throw Object.assign(new Error('Missing local auth'),{code:'AUTH_UNAVAILABLE'});token=auth.token;}catch(error){errors.push({part:'local-auth',code:errorCode(error)});}
    if(token){
      const response=await Promise.all([query('/health'),query('/versions'),query('/updates')]);health=response[0];versions=response[1];updates=response[2];
      if(health?.status==='ok'&&Array.isArray(versions?.versions)){
        for(let offset=0;offset<maxAssets;offset+=50){
          const page=await query('/assets?limit=50&offset='+offset);
          if(!Array.isArray(page?.assets)){if(page)errors.push({part:'assets',code:'INVALID_RESPONSE'});break;}
          assets.push(...page.assets.slice(0,Math.min(50,maxAssets-assets.length)).map(a=>assetRecord(a,ragRoot)));
          if(page.assets.length<50){assetsComplete=true;break;}
        }
        if(!assetsComplete)warnings.push('Asset scan is incomplete; do not claim the complete collection was refreshed.');
        references=await Promise.all(themes.map(async theme=>{
          const endpoint='/search?q='+encodeURIComponent(theme)+'&scope=s1_preferred&mode=text&limit=5';
          const found=await query(endpoint);
          return {theme,endpoint,scope:'s1_preferred (historical/unknown references may appear)',results:array(found?.results).slice(0,5).map(item=>searchRecord(item,ragRoot))};
        }));
        const after=await query('/versions');
        const activeStamp=value=>stable(array(value?.versions).filter(v=>v.active===true).map(v=>[v.dataset,v.id,v.source_hash]).sort((a,b)=>stable(a).localeCompare(stable(b))));
        if(!Array.isArray(after?.versions)||activeStamp(after)!==activeStamp(versions))errors.push({part:'consistent-version-read',code:'VERSION_CHANGED_DURING_SCAN'});
        if(assetsComplete&&Number.isFinite(health.assets)&&assets.length!==health.assets)errors.push({part:'asset-count',code:'COUNT_CHANGED_DURING_SCAN'});
      }
    }
  }else warnings.push('HTTP queries explicitly disabled; this is a local fallback observation.');
  token='';
  const freshApi=Array.isArray(versions?.versions),rawVersions=freshApi?versions.versions:fallbackVersions(updateLocal);
  const activeVersions=rawVersions.filter(v=>v.active===true).slice(0,256).map(versionRecord).sort((a,b)=>a.id.localeCompare(b.id));
  if(rawVersions.filter(v=>v.active===true).length>256)warnings.push('Active version list exceeds 256; remaining versions require follow-up.');
  const phase=text(updates?.knowledge_update?.phase||updateLocal?.phase,50)||'unknown';
  const phaseFrom=updates?.knowledge_update?'live-api':'local-status-file';
  if(phase!=='ready')warnings.push('Knowledge update phase is '+phase+'; this does not establish a fully published RAG update.');
  if(!freshApi)warnings.push('Live API unavailable. Local imports are reference candidates; old seen data is not presented as a new successful query.');
  const complete=freshApi&&health?.status==='ok'&&updates?.knowledge_update?.phase==='ready'&&assetsComplete&&!inventory.truncated&&errors.length===0&&rawVersions.filter(v=>v.active===true).length<=256;
  const status=complete?'ready':inventory.files.length||activeVersions.length?'degraded':'unavailable';
  const observed={versions:activeVersions.map(v=>({id:v.id,dataset:v.dataset,sourceHash:v.sourceHash})),files:inventory.files,assets:assets.map(a=>({id:a.id,versionId:a.versionId,sha256:a.sha256,s1Status:a.s1Status,verificationStatus:a.verificationStatus,excludedFromS1:a.excludedFromS1}))};
  const uniqueImages=imageGroups(assets),oldHashes=new Set(array(prior?.observed?.assets).map(a=>a.sha256?.toLowerCase()));
  const delta={schemaVersion:1,snapshotId,comparedTo:prior?.snapshotId||null,firstObservation:!prior,status,baselineAdvanced:complete,versions:diffRecords(prior?.observed?.versions,observed.versions,v=>v.dataset+':'+v.id),configurationFiles:diffRecords(prior?.observed?.files,observed.files,f=>f.path),assetIdentities:diffRecords(prior?.observed?.assets,observed.assets,a=>a.versionId+':'+a.id),newImageHashes:uniqueImages.filter(g=>!oldHashes.has(g.sha256)).map(g=>g.sha256),removedRecordsConclusive:complete,applicationStatus:'not-applied-by-sync'};
  delta.changed=[delta.versions,delta.configurationFiles,delta.assetIdentities].some(d=>d.added.length||d.changed.length||d.removed.length);
  const localFiles=inventory.files.map(file=>({...file,absolutePath:path.join(ragRoot,file.path)}));
  const snapshot={schemaVersion:1,snapshotId,startedAtUtc:startedAt,completedAtUtc:new Date().toISOString(),status,ragRoot,baseUrl,knowledgeUpdate:{phase,phaseFrom,updatedAt:text(updates?.knowledge_update?.updated_at||updateLocal?.updated_at,80)},health:health?{status:health.status,assets:health.assets,chunks:health.chunks,embeddedChunks:health.embedded_chunks,semantic:health.semantic}:null,versionsSource:freshApi?'live-api':'local-update-status',activeVersions,assets,uniqueImages,localFiles,references,queryEvidence,limits:{maxAssets,maxVersions:256,maxLocalFiles:768,maxLocalBytes:32*1024*1024,assetsComplete,inventoryTruncated:inventory.truncated},application:{status:'not-applied-by-sync',seenMeaning:'Observed source versions only; no claim that game rules/assets have been applied.',reviewRequired:'Read scope, source IDs and original image before controlled import. Preserve unapplied work even when the next snapshot is unchanged.',fixedImporterWarning:'Tools/Import-S1Rag.ps1 pins an older snapshot; do not run it and label the latest RAG applied without adapting and verifying the import.'},warnings,errors};
  snapshot.application.assetHashMeaning='Image SHA values come from RAG metadata; this observer does not download or rehash image bytes. Import must verify the actual image.';
  const brief={schemaVersion:1,snapshotId,status,phase,phaseFrom,snapshotPath,deltaPath,changed:delta.changed,baselineAdvanced:complete,counts:{activeVersions:activeVersions.length,assetIdentities:assets.length,uniqueImages:uniqueImages.length,configurationFiles:localFiles.length,newImages:delta.newImageHashes.length},activeVersions:activeVersions.map(v=>({id:v.id,dataset:v.dataset,summary:text(v.summary,450),scope:briefObject(v.scope,700),localConfigurationFiles:localFiles.filter(f=>f.path.includes(v.id)).slice(0,6).map(f=>f.absolutePath)})),changedConfigurationFiles:[...delta.configurationFiles.added,...delta.configurationFiles.changed].slice(0,60),references:references.map(r=>({...r,results:r.results.map(d=>({...d,content:text(d.content,550),sources:d.sources.slice(0,2)}))})),priorityAssets:assets.filter(a=>/主城|城内|设施|界面|练兵|回放|战报|任务|按钮|资源栏/.test(a.title+' '+a.category+' '+a.subcategory)).slice(0,20).map(a=>({id:a.id,versionId:a.versionId,title:a.title,s1Status:a.s1Status,excludedFromS1:a.excludedFromS1,localPath:a.localPath,detailEndpoint:a.detailEndpoint,sourceIds:a.sourceIds})),application:snapshot.application,warnings,errors};
  await writeJson(snapshotPath,snapshot);await writeJson(deltaPath,delta);await writeJson(briefPath,brief);
  if(complete)await writeJson(seenPath,{schemaVersion:1,snapshotId,observedAtUtc:snapshot.completedAtUtc,observed});
  return {status,snapshotId,briefPath,snapshotPath,deltaPath,changed:delta.changed,baselineAdvanced:complete,activeVersionCount:activeVersions.length,assetCount:assets.length,uniqueImageCount:uniqueImages.length,localFileCount:localFiles.length,phase,warnings,errors,applicationStatus:'not-applied-by-sync'};
}

if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
  syncRag({offline:process.argv.includes('--offline')}).then(result=>{console.log(JSON.stringify(result));if(result.status==='unavailable')process.exitCode=1;}).catch(error=>{console.error('RAG_SYNC_FAILED: '+errorCode(error));process.exitCode=1;});
}
