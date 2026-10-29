import assert from 'node:assert/strict';
import {due,validResult,tick} from '../Automation/runner.mjs';
import {spawn} from 'node:child_process';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
assert.equal(due({}),true);
assert.equal(due({nextRunUtc:'2026-10-25T06:00:00Z'},Date.parse('2026-10-25T05:59:59Z')),false);
assert.equal(due({nextRunUtc:'2026-10-25T06:00:00Z'},Date.parse('2026-10-25T06:00:00Z')),true);
assert.equal(validResult({status:'changed',summary:'Done',changes:['x'],evidence:['file'],remaining:[]}),true);
assert.equal(validResult({status:'published',summary:'unsupported',changes:[],evidence:[],remaining:[]}),false);
assert.equal(validResult({status:'changed',summary:'Done',changes:[],evidence:[false],remaining:[]}),false);
const status=await tick({dryRun:true});
assert.equal(typeof status.enabled,'boolean');
assert.equal(typeof status.held,'boolean');
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const shell=path.join(process.env.SystemRoot,'System32','WindowsPowerShell','v1.0','powershell.exe');
const name='Local\\EasternDynasty-LeaseTest-'+process.pid;
function lease(){
  const p=spawn(shell,['-NoProfile','-ExecutionPolicy','Bypass','-File',path.join(root,'Automation','lease.ps1'),'-Name',name],{windowsHide:true,stdio:['pipe','pipe','pipe']});
  const closed=new Promise(resolve=>p.once('close',resolve));
  const ready=new Promise((resolve,reject)=>{let value='';p.stdout.on('data',d=>{value+=d;if(value.includes('ACQUIRED'))resolve(true)});p.once('error',reject);p.once('close',()=>resolve(false));});
  return {p,ready,closed};
}
const first=lease();assert.equal(await first.ready,true);
try{const second=lease();assert.equal(await second.ready,false);assert.equal(await second.closed,3);}finally{first.p.stdin.end('\n');await first.closed;}
const third=lease();try{assert.equal(await third.ready,true);}finally{third.p.stdin.end('\n');await third.closed;}
console.log('PASS: 12 automation scheduling/result/dry-run/mutex-exclusion/reacquisition checks. No AI worker or build launched.');
