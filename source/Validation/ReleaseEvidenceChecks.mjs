import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {verifyValidatedSources} from '../Automation/release-evidence.mjs';

const validationRoot=path.dirname(fileURLToPath(import.meta.url));
const sandbox=await fs.mkdtemp(path.join(validationRoot,'release-evidence-fixture-'));
const project=path.join(sandbox,'Project'),outside=path.join(sandbox,'Outside');
const hash=value=>createHash('sha256').update(value).digest('hex');
const baseline={version:'s1-evidence-test',sourceFiles:[
  {path:'Assets\\Game.cs',sha256:hash('class Game {}')},
  {path:'Packages/manifest.json',sha256:hash('{}')},
  {path:'ProjectSettings/ProjectVersion.txt',sha256:hash('6000.3.27f1')}
]};
let checks=0;
async function rejected(receipt=baseline) {
  await assert.rejects(()=>verifyValidatedSources(project,receipt));checks++;
}
try {
  for(const folder of ['Assets','Packages','ProjectSettings'])await fs.mkdir(path.join(project,folder),{recursive:true});
  await fs.mkdir(outside);await fs.writeFile(path.join(outside,'Secret.cs'),'outside');
  await fs.writeFile(path.join(project,'Assets','Game.cs'),'class Game {}');
  await fs.writeFile(path.join(project,'Packages','manifest.json'),'{}');
  await fs.writeFile(path.join(project,'ProjectSettings','ProjectVersion.txt'),'6000.3.27f1');
  const result=await verifyValidatedSources(project,baseline);
  assert.equal(result.sourceFilesVerified,3);assert.equal(result.version,baseline.version);checks++;
  const upper=structuredClone(baseline);upper.sourceFiles[0].sha256=upper.sourceFiles[0].sha256.toUpperCase();
  assert.equal((await verifyValidatedSources(project,upper)).status,'source-files-verified');checks++;
  await rejected(null);await rejected({...baseline,version:''});await rejected({...baseline,version:'../other'});
  await rejected({...baseline,sourceFiles:[]});await rejected({...baseline,sourceFiles:null});
  await rejected({...baseline,sourceFiles:[...baseline.sourceFiles,baseline.sourceFiles[0]]});
  for(const bad of ['Assets/../Outside/Secret.cs','Assets/./Game.cs','Assets//Game.cs','/Assets/Game.cs','C:\\Assets\\Game.cs','Assets\\..\\Outside\\Secret.cs','Docs/Game.cs','Assets/Game.cs:stream','Assets/Game.cs ','Assets/']) {
    const receipt=structuredClone(baseline);receipt.sourceFiles[0].path=bad;await rejected(receipt);
  }
  const wrongHash=structuredClone(baseline);wrongHash.sourceFiles[0].sha256='not-a-sha';await rejected(wrongHash);
  await fs.writeFile(path.join(project,'Assets','Game.cs'),'class Game { int changed; }');await rejected();
  await fs.writeFile(path.join(project,'Assets','Game.cs'),'class Game {}');
  await fs.writeFile(path.join(project,'Assets','New.cs'),'new');await rejected();
  await fs.unlink(path.join(project,'Assets','New.cs'));
  await fs.rename(path.join(project,'Assets','Game.cs'),path.join(project,'Assets','Renamed.cs'));await rejected();
  await fs.rename(path.join(project,'Assets','Renamed.cs'),path.join(project,'Assets','Game.cs'));
  await fs.unlink(path.join(project,'Packages','manifest.json'));await rejected();
  await fs.writeFile(path.join(project,'Packages','manifest.json'),'{}');
  // Windows junctions require no symlink privilege and must not grant a path
  // escape even when a malicious receipt hashes the outside file correctly.
  const link=path.join(project,'Assets','Linked');
  await fs.symlink(outside,link,process.platform==='win32'?'junction':'dir');
  await rejected({...baseline,sourceFiles:[...baseline.sourceFiles,{path:'Assets/Linked/Secret.cs',sha256:hash('outside')}]});
  await fs.unlink(link);
  const rootLink=path.join(sandbox,'LinkedProject');
  await fs.symlink(project,rootLink,process.platform==='win32'?'junction':'dir');
  await assert.rejects(()=>verifyValidatedSources(rootLink,baseline));checks++;
  await fs.unlink(rootLink);
  const differentVersion={...baseline,version:'s1-other-valid-version'};
  assert.equal((await verifyValidatedSources(project,differentVersion)).version,differentVersion.version);checks++;
  // A valid other version is reported, not compared to an implicit global
  // pending version. The caller must explicitly bind those two version IDs.
  assert.equal((await verifyValidatedSources(project,baseline)).sourceFilesVerified,3);checks++;
  console.log('PASS: '+checks+' release source-evidence checks (exact set, hashes, path validation, reparse escapes). No game/build/task launched.');
} finally {
  const absolute=path.resolve(sandbox),expectedPrefix=path.resolve(validationRoot)+path.sep+'release-evidence-fixture-';
  if(!absolute.startsWith(expectedPrefix)||absolute===path.resolve(validationRoot))throw new Error('Unsafe fixture cleanup path.');
  const info=await fs.lstat(absolute);
  if(info.isSymbolicLink()||!info.isDirectory())throw new Error('Fixture cleanup root changed.');
  await fs.rm(absolute,{recursive:true,force:true});
}
