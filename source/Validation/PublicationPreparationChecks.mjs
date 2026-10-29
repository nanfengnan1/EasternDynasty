import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawn} from 'node:child_process';

// Execute the production orchestration in a disposable project whose tool
// scripts are fixtures. No Unity, user32, player, real save or installer runs.
const validationRoot=path.dirname(fileURLToPath(import.meta.url));
const sandbox=await fs.mkdtemp(path.join(validationRoot,'publication-preparation-fixture-'));
const project=path.join(sandbox,'Project');
const toolRoot=path.join(project,'Tools');
const version='s1-preparation-test';
const runRoot=path.join(project,'Validation','Releases',version);
const buildRoot=path.join(project,'Builds','Windows');
const destination=path.join(sandbox,'ExplicitGamesDestination');
const powershell=process.env.SystemRoot ? path.join(process.env.SystemRoot,'System32','WindowsPowerShell','v1.0','powershell.exe') : 'powershell.exe';
let checks=0;
const write=async(relative,text)=>{const file=path.join(project,relative);await fs.mkdir(path.dirname(file),{recursive:true});await fs.writeFile(file,text);};
const readJson=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const controls={player:true,ready:false,failCore:false,mutateSource:false,openPlayerAfterRoutes:false,mutateSave:false,mutateBuild:false,skipSerialization:false};
const saveControls=()=>write('control.json',JSON.stringify(controls));
const calls=async()=>{try{return (await fs.readFile(path.join(project,'calls.txt'),'utf8')).trim().split(/\r?\n/).filter(Boolean);}catch(error){if(error.code==='ENOENT')return [];throw error;}};
const count=async name=>(await calls()).filter(line=>line===name).length;
async function invoke(mode,success=true) {
  await saveControls();
  const args=['-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',path.join(toolRoot,'Publish-Iteration.ps1'),'-Version',version,'-DestinationPath',destination,...(Array.isArray(mode)?mode:[mode])];
  const result=await new Promise((resolve,reject)=>{
    const child=spawn(powershell,args,{cwd:project,windowsHide:true,stdio:['ignore','pipe','pipe']});let output='';
    child.stdout.on('data',x=>output+=x);child.stderr.on('data',x=>output+=x);
    child.on('error',reject);child.on('close',code=>resolve({code,output}));
  });
  assert.equal(result.code===0,success,result.output);checks++;
  return result;
}
async function expectState(state){assert.equal((await readJson(path.join(runRoot,'release.json'))).status,state);checks++;}
async function expectPreparationCount(expected){assert.equal(await count('prepare'),expected);assert.equal(await count('serialization'),expected);assert.equal(await count('build'),expected);checks++;}
try {
  await fs.mkdir(toolRoot,{recursive:true});
  await fs.copyFile(path.join(validationRoot,'..','Tools','Publish-Iteration.ps1'),path.join(toolRoot,'Publish-Iteration.ps1'));
  await write('Assets/Game.cs','class Game {}');
  await write('Packages/manifest.json','{}');
  await write('ProjectSettings/ProjectVersion.txt','6000.3.27f1');
  await write('Tools/Common.ps1',String.raw`
function Get-EdProjectRoot {return (Split-Path -Parent $PSScriptRoot)}
$env:USERPROFILE=Join-Path (Get-EdProjectRoot) 'FixtureUser'
function Get-FixtureControl {return (Get-Content -LiteralPath (Join-Path (Get-EdProjectRoot) 'control.json') -Raw -Encoding UTF8 | ConvertFrom-Json)}
function Add-FixtureCall([string]$Name){[IO.File]::AppendAllText((Join-Path (Get-EdProjectRoot) 'calls.txt'),$Name+[Environment]::NewLine)}
function Get-CimInstance {param([string]$ClassName,[string]$Filter,[string]$ErrorAction) if((Get-FixtureControl).player){[pscustomobject]@{ProcessId=99999;Name='EasternDynasty.exe'}}}
function Invoke-EdUnity([string]$Editor,[string]$Method,[string]$Log){
  if($Method -match '\.Prepare$'){Add-FixtureCall 'prepare'}
  elseif($Method -match '\.VerifySerialization$'){
    Add-FixtureCall 'serialization'
    if(-not (Get-FixtureControl).skipSerialization){
      foreach($prefix in @('','youzhou-')){foreach($state in @('idle','outbound','returning','returned')){
        [IO.File]::WriteAllText((Join-Path (Get-EdProjectRoot) ('Validation\serialization-'+$prefix+$state+'.json')),(@{state=($prefix+$state);generatedBy='current-snapshot'}|ConvertTo-Json))
      }}
    }
  }else{Add-FixtureCall 'repository'}
  [IO.File]::WriteAllText($Log,'fixture Unity method '+$Method)
}
`);
  await write('Tools/Get-DesktopState.ps1',String.raw`[pscustomobject]@{ready=(Get-FixtureControl).ready}`);
  await write('Tools/Validate.ps1',String.raw`
Add-FixtureCall 'core'
foreach($prefix in @('','youzhou-')){foreach($state in @('idle','outbound','returning','returned')){
  $fixturePath=Join-Path (Get-EdProjectRoot) ('Validation\serialization-'+$prefix+$state+'.json')
  if(-not (Test-Path -LiteralPath $fixturePath -PathType Leaf)){throw ('Missing generated serialization fixture: '+$fixturePath)}
  if((Get-Content -LiteralPath $fixturePath -Raw -Encoding UTF8 | ConvertFrom-Json).generatedBy -ne 'current-snapshot'){throw 'Wrong serialization fixture generation'}
}}
if((Get-FixtureControl).failCore){throw 'fixture core failure'}
`);
  await write('Tools/Validate-Assets.ps1',String.raw`Add-FixtureCall 'assets'`);
  await write('Tools/Build-Windows.ps1',String.raw`
Add-FixtureCall 'build'
$fixtureBuild=Join-Path (Get-EdProjectRoot) 'Builds\Windows'
New-Item -ItemType Directory -Path (Join-Path $fixtureBuild 'EasternDynasty_Data') -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $fixtureBuild 'EasternDynasty.exe'),'fixture exe; never executable')
[IO.File]::WriteAllText((Join-Path $fixtureBuild 'EasternDynasty_Data\content.dat'),'fixture build content')
if((Get-FixtureControl).mutateSource){[IO.File]::AppendAllText((Join-Path (Get-EdProjectRoot) 'Assets\Game.cs'),' // changed while building')}
`);
  await write('Tools/Check-Routes.ps1',String.raw`
param([string]$Tag)
Add-FixtureCall 'routes'
$control=Get-FixtureControl
if($control.openPlayerAfterRoutes){$control.player=$true;[IO.File]::WriteAllText((Join-Path (Get-EdProjectRoot) 'control.json'),($control|ConvertTo-Json))}
`);
  await write('Tools/Validate-Visual.ps1',String.raw`param([string]$Tag,[int]$PixelWidth,[int]$PixelHeight,[switch]$MobileReview,[switch]$Legacy) Add-FixtureCall 'visual'`);
  await write('Tools/Check-UiClicks.ps1',String.raw`
param([switch]$S1)
Add-FixtureCall $(if($S1){'mouse-s1'}else{'mouse'})
$fixtureVisual=Join-Path (Get-EdProjectRoot) $(if($S1){'Validation\Visual\s1-interactive'}else{'Validation\Visual\youzhou-interactive'})
New-Item -ItemType Directory -Path $fixtureVisual -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $fixtureVisual 'fixture.txt'),'fixture screenshot, no UI used')
`);
  await write('Tools/Check-Experience.ps1',String.raw`
Add-FixtureCall 'experience'
if((Get-FixtureControl).mutateSave){
  $fixtureSave=Join-Path $env:USERPROFILE 'AppData\LocalLow\EasternDynasty\EasternDynasty'
  New-Item -ItemType Directory -Path $fixtureSave -Force | Out-Null
  [IO.File]::WriteAllText((Join-Path $fixtureSave 'campaign.json'),'changed fixture save')
}
if((Get-FixtureControl).mutateBuild){[IO.File]::AppendAllText((Join-Path (Get-EdProjectRoot) 'Builds\Windows\EasternDynasty.exe'),' changed during UI checks')}
`);
  await write('Tools/Install-Build.ps1',String.raw`
param([string]$DestinationPath,[switch]$Replace,[switch]$DiscardPrevious,[string]$ExpectedManifestPath)
Add-FixtureCall 'install'
[IO.File]::WriteAllText((Join-Path (Get-EdProjectRoot) 'fixture-install.json'),(@{destination=$DestinationPath;manifest=$ExpectedManifestPath;replace=[bool]$Replace;discardPrevious=[bool]$DiscardPrevious}|ConvertTo-Json))
`);

  // A busy desktop and an existing player cannot block headless preparation.
  await invoke('-PrepareOnly');await expectState('prepared');await expectPreparationCount(1);
  assert.deepEqual(await calls(),['prepare','serialization','core','assets','build','repository']);checks++;
  const initial=await readJson(path.join(runRoot,'preparation.json'));
  assert.deepEqual(initial.checks,['prepare','serialization','core','assets','build','unity-repository']);checks++;
  assert.equal(initial.sourceFiles.length,3);assert.equal(initial.files.length,2);assert.match(initial.toolsSha256,/^[A-F0-9]{64}$/);checks++;
  assert.equal((await calls()).filter(x=>['routes','visual','mouse','mouse-s1','experience','install'].includes(x)).length,0);checks++;
  await invoke('-ValidateOnly');await expectState('waiting_for_player');await expectPreparationCount(1);
  controls.player=false;
  await invoke('-ValidateOnly');await expectState('waiting_for_desktop');await expectPreparationCount(1);
  assert.equal((await readJson(path.join(runRoot,'release.json'))).executable,path.join(destination,'EasternDynasty.exe'));checks++;

  await write('Assets/Game.cs','class Game { int revision; }');
  await invoke('-PrepareOnly');await expectPreparationCount(2);
  assert.notEqual((await readJson(path.join(runRoot,'preparation.json'))).sourceSha256,initial.sourceSha256);checks++;
  await fs.appendFile(path.join(buildRoot,'EasternDynasty.exe'),' changed');
  await invoke('-PrepareOnly');await expectPreparationCount(3);
  await fs.writeFile(path.join(buildRoot,'unexpected.txt'),'extra file');
  await invoke('-PrepareOnly');await expectPreparationCount(4);
  await fs.unlink(path.join(buildRoot,'EasternDynasty_Data','content.dat'));
  await invoke('-PrepareOnly');await expectPreparationCount(5);
  await fs.writeFile(path.join(runRoot,'preparation.json'),'broken json');
  await invoke('-PrepareOnly');await expectPreparationCount(6);
  await fs.appendFile(path.join(toolRoot,'Validate.ps1'),'\n# changed validator\n');
  await invoke('-PrepareOnly');await expectPreparationCount(7);
  const wrongRoot=await readJson(path.join(runRoot,'preparation.json'));wrongRoot.projectRoot=path.join(sandbox,'OtherProject');
  await fs.writeFile(path.join(runRoot,'preparation.json'),JSON.stringify(wrongRoot));
  await invoke('-PrepareOnly');await expectPreparationCount(8);
  await write('Validation/NewRuleChecks.cs','class NewRuleChecks {}');
  await invoke('-PrepareOnly');await expectPreparationCount(9);
  await write('Validation/NewRuleChecks.cs','class NewRuleChecks { int changed; }');
  await invoke('-PrepareOnly');await expectPreparationCount(10);
  await write('Validation/CardDisplayChecks/ExportCardDisplay.cs','class ExportCardDisplay {}');
  await invoke('-PrepareOnly');await expectPreparationCount(11);
  const truncated=await readJson(path.join(runRoot,'preparation.json'));truncated.checks.pop();
  await fs.writeFile(path.join(runRoot,'preparation.json'),JSON.stringify(truncated));
  await invoke('-PrepareOnly');await expectPreparationCount(12);
  const missingChecks=await readJson(path.join(runRoot,'preparation.json'));delete missingChecks.checks;
  await fs.writeFile(path.join(runRoot,'preparation.json'),JSON.stringify(missingChecks));
  await invoke('-PrepareOnly');await expectPreparationCount(13);
  await invoke(['-PrepareOnly','-ValidateOnly'],false);await expectPreparationCount(13);

  // Failed preparation cannot be reported as prepared, nor launch foreground.
  await write('Assets/Game.cs','class Game { int next; }');controls.failCore=true;
  await invoke('-PrepareOnly',false);await expectState('failed');assert.equal(await count('routes'),0);checks++;
  controls.failCore=false;controls.mutateSource=true;
  await invoke('-PrepareOnly',false);await expectState('failed');assert.equal(await count('routes'),0);checks++;
  controls.mutateSource=false;

  // If a player appears between clients, later render/mouse checks are deferred.
  controls.ready=true;controls.openPlayerAfterRoutes=true;
  await invoke('-ValidateOnly');await expectState('waiting_for_player');
  assert.equal(await count('routes'),1);assert.equal(await count('visual'),0);checks++;
  controls.openPlayerAfterRoutes=false;controls.player=false;
  controls.mutateSave=true;
  await invoke('-ValidateOnly',false);await expectState('failed');
  assert.match((await readJson(path.join(runRoot,'release.json'))).message,/save changed/i);checks++;
  controls.mutateSave=false;controls.mutateBuild=true;
  await invoke('-ValidateOnly',false);await expectState('failed');
  assert.match((await readJson(path.join(runRoot,'release.json'))).message,/build changed/i);checks++;
  controls.mutateBuild=false;
  await invoke('-ValidateOnly');await expectState('validated');
  const validated=await readJson(path.join(runRoot,'validation.json'));
  assert.equal(validated.savesUnchanged,true);assert.equal(validated.sourceFiles.length,3);
  assert.deepEqual(validated.checks,['core','assets','unity-repository','routes','youzhou-1440','youzhou-1280','mobile-1600','legacy','mouse','mouse-s1','experience']);checks++;
  assert.equal(await count('install'),0);checks++;

  controls.player=true;
  await invoke('-InstallVerified');await expectState('waiting_for_player');assert.equal(await count('install'),0);checks++;
  controls.player=false;controls.ready=false;
  await invoke('-InstallVerified');await expectState('published');
  const installation=await readJson(path.join(project,'fixture-install.json'));
  assert.equal(installation.destination,destination);assert.equal(installation.manifest,path.join(runRoot,'validation.json'));
  assert.equal(installation.replace,true);assert.equal(installation.discardPrevious,true);checks++;

  for(const mutate of [
    receipt=>{receipt.files[0].path='../outside.exe';},
    receipt=>{receipt.files[1]=receipt.files[0];},
    receipt=>{receipt.version='s1-other';},
    receipt=>{receipt.toolsSha256='0'.repeat(64);},
    receipt=>{receipt.files[0].sha256='0'.repeat(64);}
  ]){
    const receipt=structuredClone(validated);mutate(receipt);
    await fs.writeFile(path.join(runRoot,'validation.json'),JSON.stringify(receipt));
    await invoke('-InstallVerified',false);await expectState('failed');
  }
  await fs.writeFile(path.join(runRoot,'validation.json'),JSON.stringify(validated));
  await fs.appendFile(path.join(buildRoot,'EasternDynasty.exe'),' changed after validation');
  await invoke('-InstallVerified',false);await expectState('failed');
  await write('Assets/Game.cs','class ChangedAfterValidation {}');
  await invoke('-InstallVerified',false);await expectState('failed');
  assert.equal(await count('install'),1);checks++;
  // Removing generation must fail on a clean project, even if the Unity stub
  // reports success; compatibility tests cannot silently use absent old files.
  for(const prefix of ['','youzhou-'])for(const state of ['idle','outbound','returning','returned']){
    await fs.unlink(path.join(project,'Validation',`serialization-${prefix}${state}.json`));
  }
  controls.skipSerialization=true;
  await invoke('-PrepareOnly',false);await expectState('failed');
  assert.match((await readJson(path.join(runRoot,'release.json'))).message,/Missing generated serialization fixture/);checks++;
  console.log(`PASS: ${checks} publication preparation checks. Headless checkpoint/retries, input and build mutations, foreground/player gates, save protection, installation binding. No Unity, real player, desktop or real save accessed.`);
} finally {
  const absolute=path.resolve(sandbox),prefix=path.resolve(validationRoot)+path.sep+'publication-preparation-fixture-';
  if(!absolute.startsWith(prefix)||absolute===path.resolve(validationRoot))throw new Error('Unsafe fixture cleanup path.');
  const info=await fs.lstat(absolute);if(!info.isDirectory()||info.isSymbolicLink())throw new Error('Fixture cleanup refuses reparse roots.');
  await fs.rm(absolute,{recursive:true,force:true});
}
