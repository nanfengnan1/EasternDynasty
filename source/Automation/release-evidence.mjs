import fs from 'node:fs/promises';
import path from 'node:path';
import {createHash} from 'node:crypto';

const sourceFolders=['Assets','Packages','ProjectSettings'];
const fold=value=>process.platform==='win32'?value.toLowerCase():value;
const inside=(root,file)=>fold(file).startsWith(fold(root+path.sep));
function fail(message){throw new Error('RELEASE_EVIDENCE: '+message);}

function receiptPath(value) {
  if(typeof value!=='string'||!value||/[\x00-\x1f<>:"|?*]/.test(value))fail('Invalid source file path.');
  // PowerShell receipts use backslashes. Accept either separator, but never
  // normalize away a dot/empty segment or a rooted path before validating it.
  const normalized=value.replaceAll('\\','/');
  const parts=normalized.split('/');
  if(parts.length<2||!sourceFolders.includes(parts[0])||parts.some(p=>!p||p==='.'||p==='..'||/[. ]$/.test(p)))
    fail('Source file path is outside the three canonical source folders: '+value);
  return normalized;
}

async function listSourceFiles(projectRoot) {
  const files=new Map();
  async function walk(folder) {
    const stat=await fs.lstat(folder);
    if(stat.isSymbolicLink()||!stat.isDirectory())fail('Source directory is not a regular directory: '+folder);
    const actual=await fs.realpath(folder);
    if(!inside(projectRoot,actual)||fold(actual)!==fold(folder))fail('Source directory escapes through a reparse point: '+folder);
    for(const entry of await fs.readdir(folder,{withFileTypes:true})) {
      const absolute=path.join(folder,entry.name),item=await fs.lstat(absolute);
      if(item.isSymbolicLink())fail('Source reparse points are not allowed: '+absolute);
      if(item.isDirectory()){await walk(absolute);continue;}
      if(!item.isFile())fail('Unsupported source file type: '+absolute);
      const real=await fs.realpath(absolute);
      if(!inside(projectRoot,real)||fold(real)!==fold(absolute))fail('Source file escapes the project: '+absolute);
      const relative=receiptPath(path.relative(projectRoot,absolute));
      const key=fold(relative);
      if(files.has(key))fail('Duplicate source path: '+relative);
      files.set(key,{absolute,relative,stat:item});
    }
  }
  for(const folder of sourceFolders)await walk(path.join(projectRoot,folder));
  return files;
}

function sameFile(left,right) {
  return left.size===right.size&&left.mtimeMs===right.mtimeMs&&left.ctimeMs===right.ctimeMs&&left.dev===right.dev&&left.ino===right.ino;
}

/** Verify the exact playable source set recorded by a completed validation.
 * The caller separately binds the receipt version to its pending release and
 * verifies the visual-review receipt; existence of this file is not approval.
 */
export async function verifyValidatedSources(projectRoot,validationReceipt) {
  if(!validationReceipt||typeof validationReceipt.version!=='string'||
    !/^[A-Za-z0-9][A-Za-z0-9._-]*$/.test(validationReceipt.version))fail('Missing or invalid validation version.');
  if(!Array.isArray(validationReceipt.sourceFiles)||!validationReceipt.sourceFiles.length)fail('Validation sourceFiles must be nonempty.');
  if(typeof projectRoot!=='string'||!path.isAbsolute(projectRoot))fail('An absolute project root is required.');
  const root=path.resolve(projectRoot),rootStat=await fs.lstat(root);
  if(rootStat.isSymbolicLink()||!rootStat.isDirectory())fail('Project root is not a regular directory.');
  if(fold(await fs.realpath(root))!==fold(root))fail('Project root has a reparse-point ancestor.');

  const expected=new Map();
  for(const entry of validationReceipt.sourceFiles) {
    if(!entry||typeof entry!=='object')fail('Invalid sourceFiles entry.');
    const relative=receiptPath(entry.path),key=fold(relative);
    if(expected.has(key))fail('Duplicate path in validation receipt: '+relative);
    if(typeof entry.sha256!=='string'||!/^[a-fA-F0-9]{64}$/.test(entry.sha256))fail('Invalid SHA-256 for '+relative);
    expected.set(key,{relative,sha256:entry.sha256.toLowerCase()});
  }
  const actual=await listSourceFiles(root);
  if(actual.size!==expected.size)fail('Source file count differs from validation receipt.');
  for(const [key,item] of actual) {
    const record=expected.get(key);
    if(!record)fail('Unvalidated source file: '+item.relative);
    const hash=createHash('sha256').update(await fs.readFile(item.absolute)).digest('hex');
    if(hash!==record.sha256)fail('Source hash changed: '+item.relative);
    const after=await fs.lstat(item.absolute);
    if(!after.isFile()||after.isSymbolicLink()||!sameFile(item.stat,after))fail('Source file changed during verification: '+item.relative);
  }
  // Detect additions/removals or a reparse-point replacement during hashing.
  const final=await listSourceFiles(root);
  if(final.size!==actual.size)fail('Source file set changed during verification.');
  for(const [key,item] of final)if(!actual.has(key)||!sameFile(actual.get(key).stat,item.stat))fail('Source file changed during verification: '+item.relative);
  return {version:validationReceipt.version,sourceFilesVerified:actual.size,status:'source-files-verified'};
}
