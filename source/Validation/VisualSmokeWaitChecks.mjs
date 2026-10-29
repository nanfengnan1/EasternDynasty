import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const sourcePath=path.join(root,'Tools','Validate-Visual.ps1');
const source=fs.readFileSync(sourcePath,'utf8');
// Read only the pure wait function through PowerShell's parser. The publication
// script is never invoked: no Unity, game process, screenshot or desktop API.
const harness=String.raw`
$ErrorActionPreference='Stop'
$tokens=$null;$errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile($env:ED_SMOKE_WAIT_SOURCE,[ref]$tokens,[ref]$errors)
if($errors.Count){throw ($errors | Out-String)}
$functions=@($ast.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Wait-EdVisualSmokeExit'},$true))
if($functions.Count -ne 1){throw 'Expected one isolated wait function.'}
. ([scriptblock]::Create($functions[0].Extent.Text))
function New-ProcessFixture([bool]$WaitResult,[bool]$ExitOnRefresh,[bool]$WaitThrows=$false) {
    $fixture=[pscustomobject]@{waitResult=$WaitResult;exitOnRefresh=$ExitOnRefresh;waitThrows=$WaitThrows;HasExited=$false;waits=0;refreshes=0;receivedTimeout=0}
    $fixture | Add-Member ScriptMethod WaitForExit {param($milliseconds) $this.waits++;$this.receivedTimeout=$milliseconds;if($this.waitThrows){throw 'original process failure'};return $this.waitResult}
    $fixture | Add-Member ScriptMethod Refresh {$this.refreshes++;$this.HasExited=$this.exitOnRefresh}
    return $fixture
}
$cases=[Collections.Generic.List[object]]::new()
function Check-WaitCase([string]$Name,[bool]$WaitResult,[bool]$ExitOnRefresh,[bool]$Expected,[int]$ExpectedRefreshes) {
    $fixture=New-ProcessFixture $WaitResult $ExitOnRefresh
    $actual=Wait-EdVisualSmokeExit $fixture 120000
    if($actual -ne $Expected -or $fixture.waits -ne 1 -or $fixture.refreshes -ne $ExpectedRefreshes -or $fixture.receivedTimeout -ne 120000){throw ('Wait mismatch: '+$Name)}
    $cases.Add([ordered]@{name=$Name;passed=$true;actual=$actual;waits=$fixture.waits;refreshes=$fixture.refreshes})
}
Check-WaitCase 'normal completed process' $true $false $true 0
Check-WaitCase 'exit at timeout boundary accepted after refresh' $false $true $true 1
Check-WaitCase 'still running process rejected' $false $false $false 1
foreach($invalid in @(0,-1)) {
    $fixture=New-ProcessFixture $true $true
    $failure=$null;try {$null=Wait-EdVisualSmokeExit $fixture $invalid}catch{$failure=[string]$_}
    if(-not $failure -or $fixture.waits -ne 0){throw 'Invalid wait budget reached process.'}
    $cases.Add([ordered]@{name=('reject nonpositive budget '+$invalid);passed=$true})
}
$fixture=New-ProcessFixture $false $true $true
$failure=$null;try {$null=Wait-EdVisualSmokeExit $fixture 120000}catch{$failure=[string]$_}
if($failure -notlike '*original process failure*' -or $fixture.refreshes -ne 0){throw 'Process wait error was swallowed.'}
$cases.Add([ordered]@{name='preserve original process error';passed=$true})
$cases.ToArray() | ConvertTo-Json -Depth 5 -Compress
`;
const run=spawnSync('powershell.exe',['-NoProfile','-NonInteractive','-EncodedCommand',Buffer.from(harness,'utf16le').toString('base64')],{
  env:{...process.env,ED_SMOKE_WAIT_SOURCE:sourcePath},encoding:'utf8',timeout:15000,windowsHide:true
});
assert.equal(run.status,0,run.stderr || run.error?.message);
const cases=JSON.parse(run.stdout.replace(/^\uFEFF/,''));
assert.equal(cases.length,6);
assert.ok(cases.every(item=>item.passed));
assert.match(source,/\[ValidateRange\(45,180\)\]\[int\]\$TimeoutSeconds=120/);
assert.match(source,/Wait-EdVisualSmokeExit \$edPlayer \(\$TimeoutSeconds\*1000\)/);
assert.match(source,/if\(\$edPlayer\.ExitCode -ne 0\)\{throw/);
assert.match(source,/if\(\$edCaptures\.Count -ne \$edExpected\.Count\)\{throw/);
assert.match(source,/if\(\$edName -notin \$edCaptures\.Name\)\{throw/);
assert.match(source,/if\(\$edBitmap\.Width -ne \$PixelWidth -or \$edBitmap\.Height -ne \$PixelHeight\)\{throw/);
assert.match(source,/if\(-not \$edLogText\.Contains\('EASTERN_DYNASTY_RUNTIME_SMOKE_OK'\)\)\{throw/);
assert.match(source,/if\(\$edLogText -match .*\)\{throw 'Player log contains a runtime or shader error\.'/);
assert.ok(source.includes("'smoke-hud-header-ledger.png'"));
assert.match(source,/if\(-not \$Legacy -and -not \$edLogText\.Contains\('MAP_HEADER_LEDGER_RUNTIME_OK'\)\)\{throw/);
const captures=[...new Set(source.match(/(?<=')smoke(?:-[a-z0-9-]+)?\.png(?=')/g))];
const markers=[...new Set([...source.matchAll(/\$edLogText\.Contains\('([A-Z_]+_OK)'\)/g)].map(match=>match[1]))];
// Prevent future publication changes from silently shrinking established scope.
assert.ok(captures.length>=190,`Expected the full capture suite, received ${captures.length}`);
assert.ok(markers.length>=75,`Expected all completion gates, received ${markers.length}`);
const result={status:'PASSED',scope:'Node-driven pure PowerShell function fixtures and source gates; no Unity, client, screenshot or desktop execution',cases,requiredCaptures:captures.length,requiredMarkers:markers.length,timeoutSeconds:120,maximumTimeoutSeconds:180,runtimeValidation:'pending publisher'};
if(process.argv[2]){
  const output=path.resolve(process.argv[2]);fs.mkdirSync(path.dirname(output),{recursive:true});fs.writeFileSync(output,JSON.stringify(result,null,2)+'\n');
}
console.log(`VISUAL_SMOKE_WAIT_CHECKS_OK: ${cases.length} process fixtures; ${captures.length} captures and ${markers.length} runtime gates retained; no desktop execution.`);
