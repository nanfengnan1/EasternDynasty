param([switch]$S1)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$edRoot=Split-Path -Parent $PSScriptRoot
$edOutput=Join-Path $edRoot 'Validation\Visual\youzhou-interactive'
if($S1){$edOutput=Join-Path $edRoot 'Validation\Visual\s1-interactive'}
New-Item -ItemType Directory -Path $edOutput -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $edOutput 'result.txt'),('RUNNING '+[DateTime]::UtcNow.ToString('o')),[Text.UTF8Encoding]::new($false))
if (-not ('EDClick' -as [type])) {
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class EDClick {
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left,Top,Right,Bottom; }
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h,out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,IntPtr pid);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a,uint b,bool attach);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr SetFocus(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int command);
    public static bool FocusGame(IntPtr h) {
        if(GetForegroundWindow()==h)return true;
        uint current=GetCurrentThreadId(), foreground=GetWindowThreadProcessId(GetForegroundWindow(),IntPtr.Zero);
        bool attached=current!=foreground&&AttachThreadInput(current,foreground,true);
        try {ShowWindow(h,9);BringWindowToTop(h);SetForegroundWindow(h);SetFocus(h);return GetForegroundWindow()==h;}
        finally {if(attached)AttachThreadInput(current,foreground,false);}
    }
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h,ref POINT p);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags,uint dx,uint dy,uint data,UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
}
'@
}
[EDClick]::SetProcessDPIAware() | Out-Null
$edCursor=[EDClick+POINT]::new();[EDClick]::GetCursorPos([ref]$edCursor) | Out-Null
$edProcess=$null
$edReviewHadFocus=$false
$edReviewInterrupted=$false
$edFocusShell=New-Object -ComObject WScript.Shell
function Assert-PlayerAvailable {
    $ownPid=if($edProcess){$edProcess.Id}else{-1}
    if(@(Get-CimInstance Win32_Process -Filter "Name='EasternDynasty.exe'" -ErrorAction Stop | Where-Object {$_.ProcessId -ne $ownPid}).Count -gt 0) {
        throw 'PLAYER_IN_USE: Another EasternDynasty client is running; stop this review without taking focus.'
    }
    if($edProcess){$edProcess.Refresh();if($edProcess.HasExited){throw 'Review player exited unexpectedly.'}}
}
function Focus-Game {
    for($edFocusAttempt=0;$edFocusAttempt -lt 5;$edFocusAttempt++) {
        Assert-PlayerAvailable
        $edProcess.Refresh()
        $edWindow=$edProcess.MainWindowHandle
        if($edWindow -eq [IntPtr]::Zero){Start-Sleep -Milliseconds 200;continue}
        if([EDClick]::GetForegroundWindow() -eq $edWindow){$script:edReviewHadFocus=$true;return}
        if($script:edReviewHadFocus){throw 'DESKTOP_BUSY: Review player lost foreground focus; defer instead of taking focus back.'}
        # AppActivate cooperates with the desktop foreground policy; the helper
        # briefly attaches the current foreground thread and always detaches it.
        $edFocusShell.AppActivate($edProcess.Id) | Out-Null
        [EDClick]::FocusGame($edWindow) | Out-Null
        Start-Sleep -Milliseconds 180
        Assert-PlayerAvailable
        if([EDClick]::GetForegroundWindow() -eq $edWindow) {
            Start-Sleep -Milliseconds 90
            Assert-PlayerAvailable
            if([EDClick]::GetForegroundWindow() -eq $edWindow){$script:edReviewHadFocus=$true;return}
        }
        Start-Sleep -Milliseconds 150
    }
    throw ('DESKTOP_BUSY: Could not focus this review player after five bounded attempts. PID='+$edProcess.Id+'; expected HWND='+$edProcess.MainWindowHandle+'; foreground HWND='+[EDClick]::GetForegroundWindow())
}
function Client-Point([double]$x,[double]$y) {
    $clientRect=[EDClick+RECT]::new()
    if(-not [EDClick]::GetClientRect($edProcess.MainWindowHandle,[ref]$clientRect) -or ($clientRect.Right-$clientRect.Left) -ne 1440 -or ($clientRect.Bottom-$clientRect.Top) -ne 900){throw 'DESKTOP_BUSY: Review client dimensions changed from 1440x900; stop instead of clicking stale coordinates.'}
    if($x -lt 0 -or $x -ge 1440 -or $y -lt 0 -or $y -ge 900){throw ('Review target is outside client: '+$x+', '+$y)}
    $point=[EDClick+POINT]::new();$point.X=[int][Math]::Round($x);$point.Y=[int][Math]::Round($y)
    if(-not [EDClick]::ClientToScreen($edProcess.MainWindowHandle,[ref]$point)){throw 'Cannot translate review target.'}
    return $point
}
function Click-Game([double]$x,[double]$y) {
    Focus-Game
    Assert-PlayerAvailable
    if(([EDClick]::GetAsyncKeyState(1) -band 0x8000) -ne 0 -or ([EDClick]::GetAsyncKeyState(2) -band 0x8000) -ne 0){throw 'DESKTOP_BUSY: A mouse button is already held; no review input was sent.'}
    $point=Client-Point $x $y
    [EDClick]::SetCursorPos($point.X,$point.Y) | Out-Null
    Start-Sleep -Milliseconds 100
    $actualCursor=[EDClick+POINT]::new();[EDClick]::GetCursorPos([ref]$actualCursor) | Out-Null
    if([Math]::Abs($actualCursor.X-$point.X)+[Math]::Abs($actualCursor.Y-$point.Y) -gt 2){throw ('DESKTOP_BUSY: Review pointer moved or clipped: expected='+$point.X+','+$point.Y+' actual='+$actualCursor.X+','+$actualCursor.Y+'; no button pressed.')}
    if([EDClick]::GetForegroundWindow() -ne $edProcess.MainWindowHandle){throw 'DESKTOP_BUSY: Review window lost focus before click.'}
    [EDClick]::mouse_event(2,0,0,0,[UIntPtr]::Zero)
    try {Start-Sleep -Milliseconds 80}
    finally {[EDClick]::mouse_event(4,0,0,0,[UIntPtr]::Zero)}
    [EDClick]::GetCursorPos([ref]$actualCursor) | Out-Null
    $trace=[ordered]@{atUtc=[DateTime]::UtcNow.ToString('o');clientX=$x;clientY=$y;screenX=$point.X;screenY=$point.Y;afterX=$actualCursor.X;afterY=$actualCursor.Y}
    [IO.File]::AppendAllText((Join-Path $edOutput 'click-trace.jsonl'),(($trace|ConvertTo-Json -Compress)+[Environment]::NewLine),[Text.UTF8Encoding]::new($false))
    if([Math]::Abs($actualCursor.X-$point.X)+[Math]::Abs($actualCursor.Y-$point.Y) -gt 2 -or [EDClick]::GetForegroundWindow() -ne $edProcess.MainWindowHandle){throw 'DESKTOP_BUSY: Pointer or focus changed during review click; interaction result is not valid.'}
    Start-Sleep -Milliseconds 280
}
function Capture-Game([string]$name) {
    Assert-PlayerAvailable
    $persist=Join-Path $env:USERPROFILE 'AppData\LocalLow\EasternDynasty\EasternDynasty'
    $capturePath=Join-Path $persist 'review-capture.png';$statePath=Join-Path $persist 'review-state.json'
    $before=Get-ReviewCaptureObservation $capturePath $statePath
    $after=$before
    $fresh=$false
    for($attempt=0;$attempt -lt 3 -and -not $fresh;$attempt++) {
        Focus-Game
        [EDClick]::keybd_event(0x77,0,2,[UIntPtr]::Zero);Start-Sleep -Milliseconds 120
        Assert-PlayerAvailable
        [EDClick]::keybd_event(0x77,0,0,[UIntPtr]::Zero)
        try {Start-Sleep -Milliseconds 150}
        finally {[EDClick]::keybd_event(0x77,0,2,[UIntPtr]::Zero)}
        $deadline=(Get-Date).AddSeconds(3)
        do {
            Start-Sleep -Milliseconds 150
            Assert-PlayerAvailable
            $after=Get-ReviewCaptureObservation $capturePath $statePath
            $fresh=Test-ReviewCaptureFresh $before $after $edProcess.Id
        } while(-not $fresh -and (Get-Date) -lt $deadline)
        $trace=[ordered]@{atUtc=[DateTime]::UtcNow.ToString('o');capture=$name;attempt=($attempt+1);processId=$edProcess.Id;window=[string]$edProcess.MainWindowHandle;foreground=[string][EDClick]::GetForegroundWindow();before=$before;after=$after;fresh=$fresh}
        [IO.File]::AppendAllText((Join-Path $edOutput 'capture-trace.jsonl'),(($trace|ConvertTo-Json -Depth 5 -Compress)+[Environment]::NewLine),[Text.UTF8Encoding]::new($false))
    }
    if(-not $fresh){throw ('No fresh screenshot and state for '+$name+' after three focused key attempts. See capture-trace.jsonl and ED_REVIEW_CAPTURE markers in player.log; root cause is not classified as desktop busy.')}
    Start-Sleep -Milliseconds 100
    Copy-Item -LiteralPath $capturePath -Destination (Join-Path $edOutput ($name+'.png'))
    Copy-Item -LiteralPath $statePath -Destination (Join-Path $edOutput ($name+'.json'))
    return (Get-Content -LiteralPath (Join-Path $edOutput ($name+'.json')) -Raw -Encoding UTF8 | ConvertFrom-Json)
}
function Get-ReviewCaptureObservation([string]$ImagePath,[string]$StatePath) {
    $imageInfo=Get-Item -LiteralPath $ImagePath -ErrorAction SilentlyContinue
    $stateInfo=Get-Item -LiteralPath $StatePath -ErrorAction SilentlyContinue
    $state=$null;$parseError=$null
    if($stateInfo){try{$state=Get-Content -LiteralPath $StatePath -Raw -Encoding UTF8|ConvertFrom-Json}catch{$parseError=$_.Exception.Message}}
    return [pscustomobject]@{
        imageTicks=$(if($imageInfo){$imageInfo.LastWriteTimeUtc.Ticks}else{0});imageBytes=$(if($imageInfo){$imageInfo.Length}else{0})
        stateTicks=$(if($stateInfo){$stateInfo.LastWriteTimeUtc.Ticks}else{0});stateBytes=$(if($stateInfo){$stateInfo.Length}else{0})
        processId=$(if($state){$state.reviewProcessId}else{0});sequence=$(if($state){$state.reviewCaptureSequence}else{0});frame=$(if($state){$state.reviewCaptureFrame}else{0});parseError=$parseError
    }
}
function Test-ReviewCaptureFresh($Before,$After,[int]$ExpectedProcessId) {
    $minimumSequence=if($Before.processId -eq $ExpectedProcessId){$Before.sequence}else{0}
    return $After.imageTicks -gt $Before.imageTicks -and $After.stateTicks -gt $Before.stateTicks -and
        $After.imageBytes -gt 0 -and $After.stateBytes -gt 0 -and -not $After.parseError -and
        $After.processId -eq $ExpectedProcessId -and $After.sequence -gt $minimumSequence -and $After.frame -gt 0
}
function Test-ReviewReadyLog([string]$LogText,[int]$ExpectedProcessId) {
    return $LogText -match ('(?m)^ED_REVIEW_READY pid='+[regex]::Escape([string]$ExpectedProcessId)+' frame=[1-9][0-9]* targets=[1-9][0-9]* size=1440x900\r?$')
}
function Wait-ReviewReady {
    $readyDeadline=(Get-Date).AddSeconds(45)
    $playerLog=Join-Path $edOutput 'player.log'
    do {
        Assert-PlayerAvailable
        # Once this review owns focus, losing it remains a deferral, never a retry
        # that steals focus from the user. This loop sends no input.
        if($script:edReviewHadFocus -and [EDClick]::GetForegroundWindow() -ne $edProcess.MainWindowHandle){throw 'DESKTOP_BUSY: Review player lost focus while waiting for readiness.'}
        $logText=if(Test-Path -LiteralPath $playerLog){Get-Content -LiteralPath $playerLog -Raw -Encoding UTF8}else{''}
        if(Test-ReviewReadyLog $logText $edProcess.Id){return}
        Start-Sleep -Milliseconds 150
    } while((Get-Date) -lt $readyDeadline)
    throw ('REVIEW_NOT_READY: No rendered-control readiness marker for PID '+$edProcess.Id+' within 45 seconds. See player.log; no capture key was sent.')
}
function Review-Target($state,[string]$id) {
    $edTargetMatches=@($state.targets | Where-Object {$_.id -eq $id})
    if($edTargetMatches.Count -ne 1){throw ('Expected one visible semantic target '+$id+'; found '+$edTargetMatches.Count+' on '+$state.tab)}
    return $edTargetMatches[0]
}
function Click-Target([string]$id,[string]$capture) {
    $state=Capture-Game ('target-'+$id)
    $target=Review-Target $state $id
    Click-Game $target.x $target.y
    return (Capture-Game $capture)
}
function Set-GameSpeed([int]$desired) {
    for($i=0;$i -lt 4;$i++){
        $state=Capture-Game 'speed-state'
        if($state.speed -eq $desired){return}
        $target=Review-Target $state 'speed-cycle';Click-Game $target.x $target.y
    }
    throw ('Could not set game speed to '+$desired)
}
function Wait-GameState([string]$name,[scriptblock]$condition,[int]$timeout=50) {
    $deadline=(Get-Date).AddSeconds($timeout);$i=0
    do {
        $state=Capture-Game ($name+'-poll-'+$i);$i++
        if(& $condition $state){return (Capture-Game $name)}
        Start-Sleep -Milliseconds 200
    } while((Get-Date) -lt $deadline)
    throw ('Timed out waiting for '+$name+'; last state: '+($state|ConvertTo-Json -Depth 3 -Compress))
}
function Close-LandPanel {
    $state=Capture-Game 'close-land-state'
    if($state.selected -ge 0){
        $target=Review-Target $state 'land-close';Click-Game $target.x $target.y
        $state=Capture-Game 'closed-land'
        if($state.selected -ne -1){throw 'Land close did not clear selected tile.'}
    }
}
function Drag-Game([double]$x,[double]$y,[double]$deltaX,[double]$deltaY) {
    Focus-Game
    $from=Client-Point $x $y;$to=Client-Point ($x+$deltaX) ($y+$deltaY)
    [EDClick]::SetCursorPos($from.X,$from.Y)|Out-Null
    [EDClick]::mouse_event(8,0,0,0,[UIntPtr]::Zero)
    try {
        for($step=1;$step -le 8;$step++){
            Assert-PlayerAvailable
            [EDClick]::SetCursorPos([int]($from.X+($to.X-$from.X)*$step/8),[int]($from.Y+($to.Y-$from.Y)*$step/8))|Out-Null
            Start-Sleep -Milliseconds 45
        }
    }
    finally {[EDClick]::mouse_event(16,0,0,0,[UIntPtr]::Zero)}
    Start-Sleep -Milliseconds 220
}
Assert-PlayerAvailable
[IO.File]::WriteAllText((Join-Path $edOutput 'player.log'),'',[Text.UTF8Encoding]::new($false))
$edArgs=ConvertTo-EdProcessArguments @('-edReview','-screen-fullscreen','0','-screen-width','1440','-screen-height','900','-logFile',(Join-Path $edOutput 'player.log'))
$edProcess=Start-Process -FilePath (Join-Path $edRoot 'Builds\Windows\EasternDynasty.exe') -ArgumentList $edArgs -WindowStyle Normal -PassThru
try {
    $edDeadline=(Get-Date).AddSeconds(15)
    do {$edProcess.Refresh();if($edProcess.MainWindowHandle -ne 0){break};Start-Sleep -Milliseconds 200} while((Get-Date) -lt $edDeadline)
    if($edProcess.MainWindowHandle -eq 0){throw 'No Player window.'}
    Focus-Game
    Wait-ReviewReady
    Focus-Game
    $initial=Capture-Game '00-ready'
    if($initial.tab -ne '领地' -or $initial.modal){throw 'Review did not start on the map.'}
    if($S1){
        Set-GameSpeed 1
        $initial=Capture-Game '00-ready'
        Click-Game $initial.targetX $initial.targetY
        $selected=Capture-Game '01-target'
        if($selected.selected -ne $initial.nextTarget -or $selected.selectedOwned){throw 'S1 target click failed or selected already owned land.'}
        Set-GameSpeed 0
        $attackConfirm=Click-Target 'army-attack' '01-attack-confirm'
        if(-not $attackConfirm.confirmationOpen -or $attackConfirm.orderActive -or -not $attackConfirm.watchAttackOnArrival){throw 'S1 attack did not show the uncharged arrival-watch confirmation.'}
        $attackOptOut=Click-Target 'attack-watch-toggle' '01-attack-watch-off'
        if($attackOptOut.watchAttackOnArrival -or $attackOptOut.orderActive){throw 'S1 watch toggle did not disable automatic viewing before departure.'}
        $attack=Click-Target 'confirm-yes' '01-attack-issued'
        if(-not $attack.orderActive){throw 'S1 attack button did not create an order.'}
        if($attack.tab -ne '领地' -or $attack.orderWatchOnArrival -or $attack.reportCount -ne $selected.reportCount){throw 'S1 opted-out departure opened watching or exposed an early result.'}
        Set-GameSpeed 20
        $owned=Wait-GameState '02-owned' {param($s) -not $s.orderActive}
        Set-GameSpeed 1
        if(-not $owned.selectedOwned -or $owned.orderActive){throw 'S1 actual attack did not capture and return.'}
        $issued=Click-Target 'army-garrison' '03-garrison-issued'
        if(-not $issued.orderActive){throw 'Garrison did not create an order.'}
        Set-GameSpeed 20
        $guard=Wait-GameState '03-garrison' {param($s) $s.orderActive -and $s.orderPhase -eq 1}
        Set-GameSpeed 1
        if(-not $guard.selectedGuarded -or $guard.orderReturning){throw 'S1 garrison coverage missing.'}
        Click-Target 'army-recall' '04-recall-issued' | Out-Null
        Set-GameSpeed 20
        $back=Wait-GameState '04-returned' {param($s) -not $s.orderActive}
        Set-GameSpeed 1
        if($back.orderActive -or $back.selectedGuarded){throw 'S1 recall did not clear order and guard coverage.'}
        $beforeFarm=Capture-Game '05-before-farm'
        $issued=Click-Target 'army-farm' '05-farm-issued'
        if(-not $issued.orderActive -or $issued.commands -ne ($beforeFarm.commands-2)){throw 'S1 farm did not create an order and spend two commands.'}
        $farm=Wait-GameState '05-farming' {param($s) $s.orderActive -and $s.orderPhase -eq 2} 75
        if($farm.orderReturning -or $farm.commands -ne 18){throw 'S1 farm phase or cost incorrect.'}
        Set-GameSpeed 60
        $complete=Wait-GameState '06-harvest-return' {param($s) -not $s.orderActive}
        Set-GameSpeed 1
        if($complete.orderActive -or -not $complete.selectedOwned){throw 'S1 farm did not finish and return to the same owned territory.'}
        $recruitment=Click-Target 'recruit-tab' '07-recruitment'
        if($recruitment.tab -ne '招募'){throw 'Recruitment navigation failed.'}
        $archive=Click-Target 'card-library' '07-archive'
        if($archive.tab -ne '图鉴'){throw 'S1 archive button failed.'}
        $faction=Click-Target 'archive-wei' '08-faction'
        if($faction.catalogFaction -ne '魏'){throw 'S1 faction filter click failed.'}
        $message='S1_UI_CLICKS_OK: semantic-target attack, capture, garrison coverage, recall, farming, return, archive and faction.'
        [IO.File]::WriteAllText((Join-Path $edOutput 'result.txt'),$message,[Text.UTF8Encoding]::new($false))
        Write-Output ($message+' '+$edOutput)
        return
    }
    $roster=Click-Target 'roster-tab' '01-roster-click'
    if($roster.tab -ne '武将'){throw 'Roster navigation failed.'}
    $trained=Click-Target 'general-training' '02-training-click'
    if($trained.tab -ne '练兵' -or $trained.trainingReturnTab -ne '武将'){throw 'S1 general training did not open the independent training page with roster return.'}
    if($null -eq $roster.selectedCardLevel -or $null -eq $trained.selectedCardLevel){throw 'Review state lacks the training level evidence.'}
    if($trained.selectedCardLevel -ne $roster.selectedCardLevel -or $trained.selectedCardExperience -ne $roster.selectedCardExperience -or $trained.commands -ne $roster.commands -or $trained.orderActive){throw 'Opening training charged or granted experience before confirmation.'}
    $trainingReturn=Click-Target 'training-back' '02-training-return'
    if($trainingReturn.tab -ne '武将'){throw 'Independent training did not return to its roster entry.'}
    $card=Click-Target 'owned-card-first' '02-owned-card-detail'
    if([string]::IsNullOrEmpty($card.cardDetail)){throw 'Owned card click did not open a full detail.'}
    $closed=Click-Target 'card-close' '02-owned-card-closed'
    if(-not [string]::IsNullOrEmpty($closed.cardDetail)){throw 'Owned card detail did not close.'}
    $recruitment=Click-Target 'recruit-tab' '03-recruitment-click'
    if($recruitment.tab -ne '招募'){throw 'Recruitment navigation failed.'}
    $draw=Click-Target 'draw-premium' '04-draw-click'
    if(-not $draw.modal -or $draw.jade -ne ($recruitment.jade-100) -or $draw.cardCount -ne ($recruitment.cardCount+1)){throw 'Premium draw did not debit once and add one card.'}
    $blocked=Click-Target 'map-tab' '04-modal-blocked-map'
    if(-not $blocked.modal -or $blocked.tab -ne '招募' -or $blocked.jade -ne $draw.jade -or $blocked.cardCount -ne $draw.cardCount){throw 'Recruitment modal leaked a background map click.'}
    $drawClosed=Click-Target 'draw-close' '04-draw-closed'
    if($drawClosed.modal -or $drawClosed.jade -ne $draw.jade){throw 'Recruitment modal failed to close or charged twice.'}
    Click-Target 'map-tab' '05-map-return' | Out-Null
    Close-LandPanel
    $homeFocus=Click-Target 'map-home' '05-home-focus'
    $capitalTargets=@($homeFocus.targets | Where-Object {$_.id -like 'capital-label-*'})
    if($capitalTargets.Count -eq 0){throw 'No visible capital label to test.'}
    $cityTarget=$capitalTargets | Where-Object {$_.id -eq 'capital-label-731'} | Select-Object -First 1
    if(-not $cityTarget){$cityTarget=$capitalTargets[0]}
    $expectedCity=[int]($cityTarget.id -replace '^capital-label-','')
    Click-Game $cityTarget.x $cityTarget.y
    $city=Capture-Game '05-city-label-click'
    if($city.selected -ne $expectedCity -or $city.modal -or $city.jade -ne $draw.jade){throw ('Capital label did not select exact tile '+$expectedCity)}
    Close-LandPanel
    $beforeMini=Capture-Game '06-before-minimap';$miniTarget=Review-Target $beforeMini 'minimap-area'
    $ratioX=.77;$ratioY=.4
    Click-Game ($miniTarget.x-$miniTarget.w/2+$miniTarget.w*$ratioX) ($miniTarget.y-$miniTarget.h/2+$miniTarget.h*$ratioY)
    $mini=Capture-Game '06-minimap-click'
    if($mini.selected -ne -1 -or [Math]::Abs($mini.focusX-($ratioX*40-.5)) -gt 1 -or [Math]::Abs($mini.focusZ-(39.5-$ratioY*40)) -gt 1){throw 'Minimap did not navigate to the clicked world position.'}
    $surface=Review-Target $mini 'map-surface'
    Click-Game $surface.x $surface.y
    $pick=Capture-Game '07-map-pick'
    if($pick.selected -lt 0 -or $pick.tab -ne '领地'){throw 'Map click failed to select a field cell.'}
    Close-LandPanel
    $beforeZoom=Click-Target 'map-home' '08-before-wheel'
    $surface=Review-Target $beforeZoom 'map-surface'
    Focus-Game;$point=Client-Point $surface.x $surface.y
    [EDClick]::SetCursorPos($point.X,$point.Y)|Out-Null
    Assert-PlayerAvailable
    [EDClick]::mouse_event(0x800,0,0,360,[UIntPtr]::Zero);Start-Sleep -Milliseconds 350
    $zoom=Capture-Game '08-wheel-zoom'
    if($zoom.zoom -ge $beforeZoom.zoom){throw 'Mouse wheel did not zoom.'}
    $surface=Review-Target $zoom 'map-surface'
    Drag-Game $surface.x $surface.y ($surface.w*.11) ($surface.h*.10)
    $drag=Capture-Game '09-drag-map'
    if([Math]::Abs($drag.focusX-$zoom.focusX)+[Math]::Abs($drag.focusZ-$zoom.focusZ) -lt .5){throw 'Right drag did not pan.'}
    if($drag.selected -ne $zoom.selected -or $drag.cityOpen){throw 'Right drag also selected/opened a map location.'}
    $message='UI_CLICKS_OK: semantic-target roster, training, full card details, draw and modal blocking, exact capital selection, minimap, field picking, zoom and right drag.'
    [IO.File]::WriteAllText((Join-Path $edOutput 'result.txt'),$message,[Text.UTF8Encoding]::new($false))
    Write-Output ($message+' '+$edOutput)
}
catch {
    $edReviewInterrupted=$_.Exception.Message -match 'DESKTOP_BUSY|PLAYER_IN_USE'
    [IO.File]::WriteAllText((Join-Path $edOutput 'result.txt'),('FAILED '+[DateTime]::UtcNow.ToString('o')+' '+$_.Exception.Message),[Text.UTF8Encoding]::new($false))
    throw
}
finally {
    if($edProcess){$edProcess.Refresh();if(-not $edProcess.HasExited){Stop-Process -Id $edProcess.Id -Force}}
    # Do not move the user's pointer after a new player has interrupted this review.
    if(-not $edReviewInterrupted -and @(Get-CimInstance Win32_Process -Filter "Name='EasternDynasty.exe'" -ErrorAction SilentlyContinue).Count -eq 0){
        [EDClick]::SetCursorPos($edCursor.X,$edCursor.Y) | Out-Null
    }
}
