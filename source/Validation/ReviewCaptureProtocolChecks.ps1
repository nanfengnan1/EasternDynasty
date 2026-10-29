param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$edCaptureRoot=Split-Path -Parent $PSScriptRoot
$edTokens=$null;$edErrors=$null
$edAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $edCaptureRoot 'Tools/Check-UiClicks.ps1'),[ref]$edTokens,[ref]$edErrors)
if($edErrors.Count){throw ($edErrors|Out-String)}
# Import only the pure protocol functions, never the tool's Win32 or launch body.
foreach($name in @('Get-ReviewCaptureObservation','Test-ReviewCaptureFresh','Test-ReviewReadyLog','Wait-ReviewReady')) {
    $fn=@($edAst.FindAll({param($n)$n -is [Management.Automation.Language.FunctionDefinitionAst]},$true)|Where-Object Name -eq $name)
    if($fn.Count -ne 1){throw ('Missing capture protocol function: '+$name)}
    . ([scriptblock]::Create($fn[0].Extent.Text))
}
$edCaptureCases=[Collections.Generic.List[object]]::new()
function Assert-CaptureProtocol([string]$Name,[bool]$Actual,[bool]$Expected) {
    if($Actual -ne $Expected){throw ('REVIEW_CAPTURE_PROTOCOL_FAILED: '+$Name+' expected='+$Expected+' actual='+$Actual)}
    $edCaptureCases.Add([ordered]@{name=$Name;expected=$Expected;passed=$true})
}
foreach($case in @(
    @('current process rendered and window ready','ED_REVIEW_READY pid=412 frame=23 targets=18 size=1440x900',$true),
    @('marker within normal player log',"startup`r`nED_REVIEW_READY pid=412 frame=23 targets=18 size=1440x900`r`nother log",$true),
    @('previous process marker','ED_REVIEW_READY pid=411 frame=23 targets=18 size=1440x900',$false),
    @('process id prefix is not identity','ED_REVIEW_READY pid=4120 frame=23 targets=18 size=1440x900',$false),
    @('startup window exists but no controls','VALIDATION_WINDOW: 1440x900 requested=1440x900',$false),
    @('no rendered frame','ED_REVIEW_READY pid=412 frame=0 targets=18 size=1440x900',$false),
    @('empty target list','ED_REVIEW_READY pid=412 frame=23 targets=0 size=1440x900',$false),
    @('window resizing not finished','ED_REVIEW_READY pid=412 frame=23 targets=18 size=1280x720',$false),
    @('empty startup log','',$false)
)) {Assert-CaptureProtocol $case[0] (Test-ReviewReadyLog $case[1] 412) $case[2]}
$before=[pscustomobject]@{imageTicks=100L;imageBytes=64;stateTicks=100L;stateBytes=128;processId=412;sequence=4;frame=10;parseError=$null}
$baseline=[pscustomobject]@{imageTicks=101L;imageBytes=64;stateTicks=101L;stateBytes=128;processId=412;sequence=5;frame=11;parseError=$null}
foreach($mutation in @('none','unchanged-image','unchanged-state','empty-image','empty-state','malformed-json','wrong-process','unchanged-sequence','older-sequence','zero-frame','legacy-state')) {
    $after=$baseline|ConvertTo-Json|ConvertFrom-Json
    switch($mutation) {
        'unchanged-image' {$after.imageTicks=100L}
        'unchanged-state' {$after.stateTicks=100L}
        'empty-image' {$after.imageBytes=0}
        'empty-state' {$after.stateBytes=0}
        'malformed-json' {$after.parseError='synthetic partial JSON'}
        'wrong-process' {$after.processId=411}
        'unchanged-sequence' {$after.sequence=4}
        'older-sequence' {$after.sequence=3}
        'zero-frame' {$after.frame=0}
        'legacy-state' {$after.processId=$null;$after.sequence=$null;$after.frame=$null}
    }
    Assert-CaptureProtocol ('capture pair '+$mutation) (Test-ReviewCaptureFresh $before $after 412) ($mutation -eq 'none')
}
$priorProcess=$before|ConvertTo-Json|ConvertFrom-Json
$priorProcess.processId=411;$priorProcess.sequence=999
$firstCapture=$baseline|ConvertTo-Json|ConvertFrom-Json
$firstCapture.sequence=1
Assert-CaptureProtocol 'new process first sequence accepted only with both newer files' (Test-ReviewCaptureFresh $priorProcess $firstCapture 412) $true
$firstCapture.stateTicks=100L
Assert-CaptureProtocol 'new process still rejects stale file' (Test-ReviewCaptureFresh $priorProcess $firstCapture 412) $false

# A dedicated synthetic directory verifies actual timestamp/JSON observation. It
# cannot touch the player's persistent-data directory and launches no process.
$edFixtureDirectory=Join-Path $edCaptureRoot ('Validation/ReviewCaptureProtocol/fixtures/'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $edFixtureDirectory -Force|Out-Null
$edImagePath=Join-Path $edFixtureDirectory 'synthetic-capture.bin'
$edStatePath=Join-Path $edFixtureDirectory 'synthetic-state.json'
$missing=Get-ReviewCaptureObservation $edImagePath $edStatePath
Assert-CaptureProtocol 'missing pair rejected' (Test-ReviewCaptureFresh $missing $missing 412) $false
[IO.File]::WriteAllBytes($edImagePath,[byte[]]@(1,2,3,4))
[IO.File]::WriteAllText($edStatePath,'{"reviewProcessId":412,"reviewCaptureSequence":1,"reviewCaptureFrame":11}',[Text.UTF8Encoding]::new($false))
$observed=Get-ReviewCaptureObservation $edImagePath $edStatePath
Assert-CaptureProtocol 'actual synthetic file observation' (Test-ReviewCaptureFresh $missing $observed 412) $true
[IO.File]::WriteAllText($edStatePath,'{"reviewProcessId":',[Text.UTF8Encoding]::new($false))
$malformed=Get-ReviewCaptureObservation $edImagePath $edStatePath
Assert-CaptureProtocol 'partial actual JSON is observed without crashing polling' (-not [string]::IsNullOrEmpty($malformed.parseError)) $true
Assert-CaptureProtocol 'partial actual JSON never passes' (Test-ReviewCaptureFresh $missing $malformed 412) $false

# Execute the real readiness loop against a fake clock and log reader. No sleep,
# OS focus query, native interop, player launch or input injection is executed.
[IO.File]::WriteAllText((Join-Path $edFixtureDirectory 'player.log'),'synthetic readiness log',[Text.UTF8Encoding]::new($false))
foreach($readinessScenario in @('ready-after-two-polls','timeout','process-exited')) {
    & {
        param($Scenario)
        $edOutput=$edFixtureDirectory
        $edProcess=[pscustomobject]@{Id=412;MainWindowHandle=[IntPtr]::Zero}
        $script:edReviewHadFocus=$false
        $script:edSyntheticClock=[DateTime]::Parse('2026-01-01T00:00:00Z').ToUniversalTime()
        $script:edSyntheticAvailabilityCalls=0
        $script:edSyntheticSleepCalls=0
        function Get-Date {$script:edSyntheticClock=$script:edSyntheticClock.AddSeconds(15);return $script:edSyntheticClock}
        function Start-Sleep {param($Milliseconds) $script:edSyntheticSleepCalls++}
        function Assert-PlayerAvailable {
            $script:edSyntheticAvailabilityCalls++
            if($Scenario -eq 'process-exited'){throw 'Review player exited unexpectedly.'}
        }
        function Get-Content {
            param($LiteralPath,[switch]$Raw,$Encoding)
            if($Scenario -eq 'ready-after-two-polls' -and $script:edSyntheticAvailabilityCalls -ge 2){return 'ED_REVIEW_READY pid=412 frame=23 targets=18 size=1440x900'}
            return 'Synthetic startup has no rendered-control readiness yet.'
        }
        $failure=$null
        try {Wait-ReviewReady}catch{$failure=$_.Exception.Message}
        switch($Scenario) {
            'ready-after-two-polls' {
                Assert-CaptureProtocol 'real wait accepts delayed rendered readiness' ($null -eq $failure -and $script:edSyntheticAvailabilityCalls -eq 2 -and $script:edSyntheticSleepCalls -eq 1) $true
            }
            'timeout' {
                Assert-CaptureProtocol 'real wait stops at 45-second bound' ($failure -like 'REVIEW_NOT_READY:*' -and $script:edSyntheticAvailabilityCalls -eq 3 -and $script:edSyntheticSleepCalls -eq 3) $true
                Assert-CaptureProtocol 'readiness timeout does not invent desktop-busy cause' ($failure -notmatch 'DESKTOP_BUSY') $true
            }
            'process-exited' {
                Assert-CaptureProtocol 'real wait immediately preserves process-exit failure' ($failure -eq 'Review player exited unexpectedly.' -and $script:edSyntheticSleepCalls -eq 0) $true
            }
        }
    } $readinessScenario
}

$edRuntime=Get-Content -LiteralPath (Join-Path $edCaptureRoot 'Assets/EasternDynasty/Scripts/Runtime/Diagnostics/EasternDynastyDiagnostics.cs') -Raw -Encoding UTF8
$edTool=Get-Content -LiteralPath (Join-Path $edCaptureRoot 'Tools/Check-UiClicks.ps1') -Raw -Encoding UTF8
Assert-CaptureProtocol 'ready follows repaint and visible real navigation target' ($edRuntime -match 'reviewRepaintFrame >= 0 && Time.frameCount > reviewRepaintFrame && reviewTargets.ContainsKey\("roster-tab"\)') $true
Assert-CaptureProtocol 'ready waits until actual requested dimensions match' ($edRuntime -match 'Screen.width == reviewWindowWidth && Screen.height == reviewWindowHeight') $true
Assert-CaptureProtocol 'normal gameplay returns before capture diagnostics' ($edRuntime -match 'private void UpdateReviewCapture\(\)\s*\{\s*if \(!reviewMode\) return;') $true
Assert-CaptureProtocol 'input begins after process-specific readiness' ($edTool -match 'Focus-Game\s+Wait-ReviewReady\s+Focus-Game\s+\$initial=Capture-Game') $true
Assert-CaptureProtocol 'readiness wait is bounded' ($edTool -match '\$readyDeadline=\(Get-Date\).AddSeconds\(45\)' -and $edTool -match 'while\(\(Get-Date\) -lt \$readyDeadline\)') $true
Assert-CaptureProtocol 'three capture retries and strict failure remain' ($edTool -match '\$attempt -lt 3 -and -not \$fresh' -and $edTool -match 'if\(-not \$fresh\)\{throw') $true
Assert-CaptureProtocol 'trace distinguishes screenshot request from state write' ($edRuntime -match 'ED_REVIEW_CAPTURE_REQUEST' -and $edRuntime -match 'ED_REVIEW_CAPTURE_STATE_WRITTEN' -and $edRuntime -match 'screenshot=requested' -and $edTool -match 'capture-trace.jsonl') $true
Assert-CaptureProtocol 'player readiness log is cleared before this review launch' ($edTool -match "Assert-PlayerAvailable\s+\[IO.File\]::WriteAllText\(\(Join-Path \`$edOutput 'player.log'\),'',\[Text.UTF8Encoding\]::new\(\`$false\)\)\s+\`$edArgs=ConvertTo-EdProcessArguments") $true
$result=[ordered]@{status='PASSED';checkedUtc=[DateTime]::UtcNow.ToString('o');caseCount=$edCaptureCases.Count;scope='Pure readiness and capture protocol checks with synthetic log/state/files. No Unity, client, desktop input, screenshot or runtime-failure reproduction.';fixtureDirectory=$edFixtureDirectory;cases=@($edCaptureCases.ToArray())}
if($OutputDirectory){New-Item -ItemType Directory -Path $OutputDirectory -Force|Out-Null;[IO.File]::WriteAllText((Join-Path $OutputDirectory 'review-capture-protocol.json'),($result|ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))}
Write-Output ('PASS: '+$edCaptureCases.Count+' review readiness, process identity, paired freshness and diagnostic checks; runtime reproduction remains publisher work.')
