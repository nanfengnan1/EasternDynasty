param(
    [string]$ExecutablePath,
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$')][string]$Tag='experience-interactive',
    [ValidateRange(1024,3840)][int]$PixelWidth=1440,
    [ValidateRange(720,2160)][int]$PixelHeight=900
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$edExperienceRoot=Get-EdProjectRoot
if(-not $ExecutablePath){$ExecutablePath=Join-Path $edExperienceRoot 'Builds\Windows\EasternDynasty.exe'}
$ExecutablePath=[IO.Path]::GetFullPath($ExecutablePath)
if(-not (Test-Path -LiteralPath $ExecutablePath -PathType Leaf) -or [IO.Path]::GetFileName($ExecutablePath) -ne 'EasternDynasty.exe'){throw 'A built EasternDynasty.exe is required.'}
$edExperienceBase=Join-Path $edExperienceRoot ('Validation\Visual\'+$Tag)
$edExperienceRun=[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8)
$edExperienceOutput=Join-Path $edExperienceBase $edExperienceRun
New-Item -ItemType Directory -Path $edExperienceOutput -Force | Out-Null
$edExperienceSaveDirectory=Join-Path $env:USERPROFILE 'AppData\LocalLow\EasternDynasty\EasternDynasty'
$edExperienceProcess=$null
$edExperienceHadFocus=$false
$edExperienceChecks=[Collections.Generic.List[string]]::new()
$edExperienceCaptures=[Collections.Generic.List[string]]::new()
$edExperienceFailure=$null

if(-not ('EDExperiencePointer' -as [type])) {
Add-Type @'
using System;
using System.IO;
using System.Runtime.InteropServices;
public static class EDExperiencePointer {
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left,Top,Right,Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct LASTINPUTINFO { public uint cbSize,dwTime; }
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,IntPtr pid);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a,uint b,bool attach);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr SetFocus(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int command);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h,ref POINT p);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h,out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags,uint dx,uint dy,uint data,UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
    [DllImport("user32.dll")] private static extern IntPtr GetKeyboardLayout(uint threadId);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern uint MapVirtualKeyEx(uint code,uint type,IntPtr layout);
    public static void KeyEvent(IntPtr window,byte key,uint flags) {
        // Keep the scan code consistent with the review window's keyboard layout.
        // A zero scan code is not a physical F8/digit event for all input paths.
        uint scan=MapVirtualKeyEx(key,4,GetKeyboardLayout(GetWindowThreadProcessId(window,IntPtr.Zero)));
        if(scan==0)throw new InvalidOperationException("No hardware scan code for review key "+key);
        keybd_event(key,(byte)(scan&255),flags|((scan&0xff00)==0xe000?1u:0u),UIntPtr.Zero);
    }
    [DllImport("user32.dll",SetLastError=true)] private static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
    public static uint LastInputTick() {
        var info=new LASTINPUTINFO{cbSize=(uint)Marshal.SizeOf(typeof(LASTINPUTINFO))};
        if(!GetLastInputInfo(ref info))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        return info.dwTime;
    }
    public static bool FocusGame(IntPtr h) {
        if(GetForegroundWindow()==h)return true;
        uint current=GetCurrentThreadId(),foreground=GetWindowThreadProcessId(GetForegroundWindow(),IntPtr.Zero);
        bool attached=current!=foreground&&AttachThreadInput(current,foreground,true);
        try {ShowWindow(h,9);BringWindowToTop(h);SetForegroundWindow(h);SetFocus(h);return GetForegroundWindow()==h;}
        finally {if(attached)AttachThreadInput(current,foreground,false);}
    }
    public static byte[] ReadShared(string path) {
        using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
        using(var memory=new MemoryStream()){stream.CopyTo(memory);return memory.ToArray();}
    }
}
'@
}
[EDExperiencePointer]::SetProcessDPIAware() | Out-Null
$edExperienceCursor=[EDExperiencePointer+POINT]::new()
[EDExperiencePointer]::GetCursorPos([ref]$edExperienceCursor) | Out-Null
$edExperienceForeground=[EDExperiencePointer]::GetForegroundWindow()

function Assert-Experience([bool]$Condition,[string]$Message) {
    if(-not $Condition){throw ('EXPERIENCE_ASSERT: '+$Message)}
    $edExperienceChecks.Add($Message)
}
function Assert-ExperiencePlayerAvailable {
    $others=@(Get-Process -Name EasternDynasty -ErrorAction SilentlyContinue | Where-Object { -not $edExperienceProcess -or $_.Id -ne $edExperienceProcess.Id })
    if($others.Count -gt 0){throw ('PLAYER_IN_USE: another EasternDynasty client is running; no desktop interaction will continue. PIDs: '+(($others | ForEach-Object {$_.Id}) -join ','))}
    if($edExperienceProcess -and $edExperienceProcess.HasExited){throw 'The review client exited before the interaction checks completed.'}
}
function Focus-Experience {
    Assert-ExperiencePlayerAvailable
    $edExperienceProcess.Refresh()
    for($focusAttempt=0;$focusAttempt -lt 3;$focusAttempt++) {
        Assert-ExperiencePlayerAvailable
        if($script:edExperienceHadFocus -and [EDExperiencePointer]::GetForegroundWindow() -ne $edExperienceProcess.MainWindowHandle){throw 'DESKTOP_BUSY: Review player lost focus; defer instead of taking focus back.'}
        if([EDExperiencePointer]::FocusGame($edExperienceProcess.MainWindowHandle)){$script:edExperienceHadFocus=$true;return}
        $activationShell=New-Object -ComObject WScript.Shell
        try {$activationShell.AppActivate($edExperienceProcess.Id) | Out-Null}
        finally {[Runtime.InteropServices.Marshal]::FinalReleaseComObject($activationShell) | Out-Null}
        Start-Sleep -Milliseconds 200
    }
    throw 'DESKTOP_BUSY: Could not focus the review client.'
}
function Move-Experience([double]$X,[double]$Y) {
    $point=[EDExperiencePointer+POINT]::new();$point.X=[int][Math]::Round($X);$point.Y=[int][Math]::Round($Y)
    if(-not [EDExperiencePointer]::ClientToScreen($edExperienceProcess.MainWindowHandle,[ref]$point)){throw 'ClientToScreen failed.'}
    if(-not [EDExperiencePointer]::SetCursorPos($point.X,$point.Y)){throw 'Could not position the pointer.'}
}
function Click-Experience([double]$X,[double]$Y) {
    if(([EDExperiencePointer]::GetAsyncKeyState(1) -band 0x8000) -ne 0 -or ([EDExperiencePointer]::GetAsyncKeyState(2) -band 0x8000) -ne 0){throw 'DESKTOP_BUSY: A mouse button is held; review click deferred.'}
    Focus-Experience;Move-Experience $X $Y;Start-Sleep -Milliseconds 100
    Assert-ExperiencePointer $X $Y
    [EDExperiencePointer]::mouse_event(2,0,0,0,[UIntPtr]::Zero)
    try {Start-Sleep -Milliseconds 85} finally {[EDExperiencePointer]::mouse_event(4,0,0,0,[UIntPtr]::Zero)}
    Assert-ExperiencePointer $X $Y
    Start-Sleep -Milliseconds 300
}
function Assert-ExperiencePointer([double]$X,[double]$Y) {
    $expected=[EDExperiencePointer+POINT]::new();$expected.X=[int][Math]::Round($X);$expected.Y=[int][Math]::Round($Y)
    [EDExperiencePointer]::ClientToScreen($edExperienceProcess.MainWindowHandle,[ref]$expected) | Out-Null
    $actual=[EDExperiencePointer+POINT]::new();[EDExperiencePointer]::GetCursorPos([ref]$actual)|Out-Null
    if([Math]::Abs($expected.X-$actual.X)+[Math]::Abs($expected.Y-$actual.Y) -gt 2 -or [EDExperiencePointer]::GetForegroundWindow() -ne $edExperienceProcess.MainWindowHandle){throw 'DESKTOP_BUSY: Pointer or focus changed during review input.'}
}
function Drag-Experience([double]$X,[double]$Y,[double]$EndX,[double]$EndY) {
    Focus-Experience;Move-Experience $X $Y;Start-Sleep -Milliseconds 100
    [EDExperiencePointer]::mouse_event(2,0,0,0,[UIntPtr]::Zero)
    try {
        for($step=1;$step -le 12;$step++) {
            Assert-ExperiencePlayerAvailable
            Move-Experience ($X+($EndX-$X)*$step/12) ($Y+($EndY-$Y)*$step/12)
            Start-Sleep -Milliseconds 35
        }
    } finally {[EDExperiencePointer]::mouse_event(4,0,0,0,[UIntPtr]::Zero)}
    Start-Sleep -Milliseconds 300
}
function Get-ExperienceTarget($State,[string]$Id) {
    $targetMatches=@($State.targets | Where-Object {$_.id -eq $Id})
    if($targetMatches.Count -ne 1){throw ('Missing/ambiguous current visible target '+$Id+' on '+$State.tab+'. Present: '+(($State.targets | ForEach-Object {$_.id}) -join ', '))}
    return $targetMatches[0]
}
function Click-ExperienceTarget($State,[string]$Id) {
    $target=Get-ExperienceTarget $State $Id
    Click-Experience ([double]$target.x) ([double]$target.y)
}
function Find-ExperienceScrolledTarget($State,[string]$Id,[string]$Name,[double]$X,[double]$Y,[int]$MaxScrolls=7,[int]$WheelNotches=2) {
    for($scrollAttempt=0;$scrollAttempt -le $MaxScrolls;$scrollAttempt++) {
        if(@($State.targets | Where-Object {$_.id -eq $Id}).Count -eq 1){return $State}
        if($scrollAttempt -eq $MaxScrolls){break}
        Focus-Experience;Move-Experience $X $Y;Assert-ExperiencePointer $X $Y
        # Wheel the actual content viewport; only accept a target that becomes visibly rendered.
        [EDExperiencePointer]::mouse_event(0x0800,0,0,[uint32](4294967296-120*$WheelNotches),[UIntPtr]::Zero)
        Start-Sleep -Milliseconds 200;Assert-ExperiencePointer $X $Y
        $State=Capture-Experience ($Name+'-scroll-'+$scrollAttempt)
    }
    throw ('EXPERIENCE_ASSERT: Content scrolling did not reveal '+$Id+'.')
}
function ConvertFrom-ExperienceCapture([byte[]]$Png,[string]$Json) {
    # Pure validation shared with regression checks. Freshness is separately
    # required for BOTH files before this candidate can reach the output folder.
    if($null -eq $Png -or $Png.Length -lt 1000 -or $Png[0] -ne 137 -or [Text.Encoding]::ASCII.GetString($Png,$Png.Length-8,4) -ne 'IEND'){throw 'PNG is incomplete.'}
    $state=$Json | ConvertFrom-Json
    if($null -eq $state -or -not $state.PSObject.Properties['targets'] -or $null -eq $state.targets -or @($state.targets).Count -eq 0){throw 'JSON contains no visible targets.'}
    return $state
}
function Capture-Experience([string]$Name) {
    $capturePath=Join-Path $edExperienceSaveDirectory 'review-capture.png'
    $statePath=Join-Path $edExperienceSaveDirectory 'review-state.json'
    $beforeImage=if(Test-Path -LiteralPath $capturePath){(Get-Item -LiteralPath $capturePath).LastWriteTimeUtc.Ticks}else{0L}
    $beforeState=if(Test-Path -LiteralPath $statePath){(Get-Item -LiteralPath $statePath).LastWriteTimeUtc.Ticks}else{0L}
    $lastCaptureIssue='No capture attempt completed.'
    for($attempt=0;$attempt -lt 3;$attempt++) {
        Focus-Experience
        [EDExperiencePointer]::KeyEvent($edExperienceProcess.MainWindowHandle,0x77,2);Start-Sleep -Milliseconds 180
        [EDExperiencePointer]::KeyEvent($edExperienceProcess.MainWindowHandle,0x77,0)
        try {Start-Sleep -Milliseconds 160} finally {[EDExperiencePointer]::KeyEvent($edExperienceProcess.MainWindowHandle,0x77,2)}
        $deadline=[DateTime]::UtcNow.AddSeconds(4)
        do {
            Start-Sleep -Milliseconds 160;Assert-ExperiencePlayerAvailable
            if(-not (Test-Path -LiteralPath $capturePath) -or -not (Test-Path -LiteralPath $statePath)){$lastCaptureIssue='PNG or state file is missing.';continue}
            $imageTick=(Get-Item -LiteralPath $capturePath).LastWriteTimeUtc.Ticks
            $stateTick=(Get-Item -LiteralPath $statePath).LastWriteTimeUtc.Ticks
            if($imageTick -le $beforeImage -or $stateTick -le $beforeState){$lastCaptureIssue='Files not fresh: PNG ticks '+$beforeImage+' -> '+$imageTick+'; JSON ticks '+$beforeState+' -> '+$stateTick;continue}
            try {
                $png=[EDExperiencePointer]::ReadShared($capturePath)
                $json=[Text.Encoding]::UTF8.GetString([EDExperiencePointer]::ReadShared($statePath))
                $state=ConvertFrom-ExperienceCapture $png $json
                [IO.File]::WriteAllBytes((Join-Path $edExperienceOutput ($Name+'.png')),$png)
                [IO.File]::WriteAllText((Join-Path $edExperienceOutput ($Name+'.json')),$json,[Text.UTF8Encoding]::new($false))
                $edExperienceCaptures.Add($Name)
                return $state
            } catch { $lastCaptureIssue=[string]$_;if([DateTime]::UtcNow -ge $deadline){break} }
        } while([DateTime]::UtcNow -lt $deadline)
    }
    throw ('No complete fresh F8 screenshot and JSON received for '+$Name+'. '+$lastCaptureIssue)
}
function Set-ExperienceSpeed([int]$Desired,[string]$Name) {
    for($attempt=0;$attempt -lt 4;$attempt++) {
        $state=Capture-Experience ($Name+'-'+$attempt)
        if([int]$state.speed -eq $Desired){return $state}
        Click-ExperienceTarget $state 'speed-cycle'
    }
    throw ('Could not select simulation speed '+$Desired+'.')
}
function Assert-ExperienceMapStill($Before,$After,[string]$Name) {
    $delta=[Math]::Abs($After.focusX-$Before.focusX)+[Math]::Abs($After.focusZ-$Before.focusZ)
    Assert-Experience ($delta -lt .025) ($Name+' blocks map motion')
}
function Assert-ExperienceMinimap($State) {
    Assert-Experience ($State.tab -eq '领地' -and $State.mapHudVisible -and $State.minimapStyle -eq 'youzhou-atlas-v1') 'map draws the Youzhou atlas minimap in its actual map HUD'
    Assert-ExperienceTargetInClient $State 'minimap-area'
    Assert-ExperienceTargetInClient $State 'map-home'
    $points=@($State.minimapViewportPoints)
    Assert-Experience ($points.Count -ge 6 -and $points.Count%2 -eq 0) 'minimap exposes at least three clipped camera-ground projection vertices'
    foreach($coordinate in $points) {Assert-Experience (-not [double]::IsNaN([double]$coordinate) -and -not [double]::IsInfinity([double]$coordinate) -and [double]$coordinate -ge 0 -and [double]$coordinate -le 1) 'minimap camera footprint remains within normalized map bounds'}
}
function Assert-ExperienceMinimapDesign($State) {
    Assert-Experience ($State.minimapDesignStyle -eq 'youzhou-atlas-sidebar-v3' -and $State.minimapTextFits) 'minimap exported sidebar design and actual font fit are visible'
    $atlasScale=$State.mapHudTouchMinimum/44.0
    $atlasWidth=$State.mapHudSafeWidth/$atlasScale
    $atlasDesign=@(@('minimap-panel',($atlasWidth-302),90,282,204),@('minimap-title',($atlasWidth-92),90,72,54),@('map-home',($atlasWidth-86),154,60,60),@('map-home-art',($atlasWidth-66),160,20,28),@('map-home-caption',($atlasWidth-86),191,60,20),@('minimap-area',($atlasWidth-302),90,204,204),@('minimap-legend',($atlasWidth-92),224,72,70))
    foreach($design in $atlasDesign) {
        Assert-ExperienceTargetInClient $State $design[0]
        $drawn=Get-ExperienceTarget $State $design[0]
        Assert-Experience ([Math]::Abs($drawn.x-$drawn.w*.5-$State.mapHudSafeX-$design[1]*$atlasScale) -lt .1 -and [Math]::Abs($drawn.y-$drawn.h*.5-$State.mapHudSafeY-$design[2]*$atlasScale) -lt .1 -and [Math]::Abs($drawn.w-$design[3]*$atlasScale) -lt .1 -and [Math]::Abs($drawn.h-$design[4]*$atlasScale) -lt .1) ($design[0]+' matches the inspected sidebar minimap design')
    }
}
function Assert-ExperienceMinimapFocus($Before,$After,[double]$PointerX,[double]$PointerY) {
    $area=Get-ExperienceTarget $Before 'minimap-area'
    $u=($PointerX-($area.x-$area.w*.5))/$area.w
    $v=($PointerY-($area.y-$area.h*.5))/$area.h
    Assert-Experience ($u -gt 0 -and $u -lt 1 -and $v -gt 0 -and $v -lt 1) 'minimap navigation clicks inside the actual visible atlas'
    Assert-Experience ([Math]::Abs($After.focusX-($u*40-.5)) -lt .03 -and [Math]::Abs($After.focusZ-(39.5-$v*40)) -lt .03) 'minimap pointer position resolves to the matching S1 40 by 40 map focus'
}
function Assert-ExperienceMinimapNavigation($Before,$After,[double]$ClickX,[double]$ClickY) {
    Assert-ExperienceMinimap $Before
    Assert-ExperienceMinimap $After
    Assert-Experience ($After.minimapNavigationSequence -eq ($Before.minimapNavigationSequence+1)) 'minimap navigation consumes exactly one new visible pointer event'
    # Win32 client rows are zero based; the Windows Unity input path reports the
    # same pixel with bottom-left Y=height-1-clientY, then IMGUI uses height-Y.
    # The publisher's 1247,217 click therefore arrives at IMGUI pixel 1247,218.
    # Check the independently observed event before verifying its map projection;
    # do not loosen the focus tolerance to hide a one-pixel coordinate mismatch.
    $pointerX=[double]$After.minimapNavigationPointerX
    $pointerY=[double]$After.minimapNavigationPointerY
    Assert-Experience ([Math]::Abs($pointerX-$ClickX) -lt .01 -and [Math]::Abs($pointerY-($ClickY+1)) -lt .01) 'minimap received pointer matches the requested Windows client pixel'
    Assert-Experience ($Before.speed -eq 0 -and $After.speed -eq 0 -and -not [string]::IsNullOrEmpty($Before.battleWorldHash) -and $After.battleWorldHash -eq $Before.battleWorldHash -and $After.selected -eq $Before.selected) 'minimap navigation preserves the paused campaign and selected land'
    Assert-ExperienceMinimapFocus $Before $After $pointerX $pointerY
    Assert-Experience ((($Before.minimapViewportPoints | ConvertTo-Json -Compress) -ne ($After.minimapViewportPoints | ConvertTo-Json -Compress))) 'minimap camera footprint moves with actual map navigation'
}
function Assert-ExperienceCityPage($State,[string]$Page) {
    Assert-Experience ($State.cityOpen -and $State.tab -eq '主城' -and $State.cityPage -eq $Page) ('city opens independent '+$Page+' page')
    foreach($nav in @('city-overview','city-catalog','city-facilities','city-quests','city-training','city-back')) {
        $null=Get-ExperienceTarget $State $nav
    }
    $slots=@($State.targets | Where-Object {$_.id -match '^city-slot-[0-8]$'}).Count
    Assert-Experience ($slots -eq $(if($Page -eq 'facilities'){9}else{0})) ($Page+' exposes district slots only on the facilities page')
    if($Page -ne 'overview') {Assert-Experience ([string]::IsNullOrEmpty($State.cityNavigationStyle) -and @($State.cityNavigationDock).Count -eq 0 -and @($State.cityContentCanvas).Count -eq 0) ($Page+' does not expose stale overview edge-dock geometry')}
}
function Assert-ExperienceCityShell($State) {
    Assert-ExperienceCityHeader $State
    foreach($id in @('hud-save','speed-cycle')) {Assert-ExperienceTargetInClient $State $id}
    Assert-ExperienceCityFooter $State
}
function Assert-ExperienceCityHeader($State) {
    Assert-Experience ($State.cityShellStyle -eq 'city-crest-landscape-navigation-v1' -and -not [string]::IsNullOrEmpty($State.cityHudFullName) -and $State.cityHudLevel -gt 0) 'city header identifies the actual home and town hall level'
    Assert-Experience ($State.tab -eq '主城' -and $State.cityHeaderDesignStyle -eq 'inkscape-city-ledger-v2' -and $State.cityHeaderDesignTextFits -and $State.cityHudShortName -eq $State.cityHudFullName) 'city header follows the inspected ledger design and fits the complete actual home name with measured Chinese fonts'
    Assert-Experience $State.cityHeaderBackdropDrawn 'city header reports its actual opaque backdrop draw in this visible frame'
    foreach($id in @('city-header-backdrop','city-hud-home','city-hud-home-crest','city-hud-home-art','city-hud-home-name','city-hud-home-detail','city-header-production')) {Assert-ExperienceTargetInClient $State $id}
    $headerUnit=$State.mapHudTouchMinimum/44.0
    Assert-Experience ($headerUnit -gt 0) 'city header uses the actual positive safe-area scale'
    $identity=Get-ExperienceTarget $State 'city-hud-home'
    $identityArt=Get-ExperienceTarget $State 'city-hud-home-art'
    $identityName=Get-ExperienceTarget $State 'city-hud-home-name'
    Assert-Experience ((Test-ExperienceLandContains $identity $identityArt) -and (Test-ExperienceLandContains $identity $identityName) -and -not (Test-ExperienceLandOverlap $identityArt $identityName)) 'city architectural seal and complete name occupy separate visible regions of the real home command'
    $nameY=7;$nameHeight=30
    if($State.cityHeaderNameWrapped){$nameY=5;$nameHeight=38}
    $headerWidth=$State.mapHudSafeWidth/$headerUnit
    $resourcePitch=[Math]::Min(168,($headerWidth-604)/4)
    $headerDesign=@(@('city-header-backdrop',0,0,$headerWidth,70),@('city-hud-home',16,4,246,62),@('city-hud-home-crest',16,4,62,62),@('city-hud-home-art',22,13,50,44),@('city-hud-home-name',84,$nameY,174,$nameHeight),@('city-hud-home-detail',84,44,174,20),@('city-header-production',280,5,($resourcePitch*4),58))
    $backdrop=Get-ExperienceTarget $State 'city-header-backdrop'
    $production=Get-ExperienceTarget $State 'city-header-production'
    Assert-Experience ((Test-ExperienceLandContains $backdrop $identity) -and (Test-ExperienceLandContains $backdrop $production) -and -not (Test-ExperienceLandOverlap $identity $production)) 'opaque city header covers the separate identity and continuous production ledger'
    $previous=$identity
    for($resource=0;$resource -lt 6;$resource++) {
        $cell=Get-ExperienceTarget $State ('hud-resource-'+$resource)
        Assert-ExperienceTargetInClient $State ('hud-resource-'+$resource)
        Assert-Experience (Test-ExperienceLandContains $backdrop $cell) 'each visible resource cell has the full opaque header behind it'
        if($resource -lt 4){Assert-Experience (Test-ExperienceLandContains $production $cell) 'all four production columns belong to the same visible ledger'}
        Assert-Experience (-not (Test-ExperienceLandOverlap $previous $cell)) 'city live resource cells remain separate from the complete identity and each preceding cell'
        $previous=$cell
        $parts=@()
        foreach($part in @('icon','stock','detail')) {
            $regionId='hud-resource-'+$part+'-'+$resource
            $region=Get-ExperienceTarget $State $regionId
            Assert-ExperienceTargetInClient $State $regionId
            Assert-Experience (Test-ExperienceLandContains $cell $region) ($regionId+' is actually drawn inside its read-only resource column')
            foreach($other in $parts){Assert-Experience (-not (Test-ExperienceLandOverlap $other $region)) 'resource icon, stock and income occupy separate visible layers'}
            $parts+=,$region
        }
        $cellX=280+$resource*$resourcePitch;$cellWidth=$resourcePitch-6
        $iconOffset=6;$iconWidth=32;$iconHeight=26;$textOffset=44;$textWidth=$cellWidth-48
        if($resource -ge 4){$cellX=280+$resourcePitch*4+16+($resource-4)*150;$cellWidth=144;$iconOffset=7;$iconWidth=26;$iconHeight=28;$textOffset=40;$textWidth=100}
        $headerDesign+=@(@(('hud-resource-'+$resource),$cellX,5,$cellWidth,58),@(('hud-resource-icon-'+$resource),($cellX+$iconOffset),19,$iconWidth,$iconHeight),@(('hud-resource-stock-'+$resource),($cellX+$textOffset),30,$textWidth,26),@(('hud-resource-detail-'+$resource),($cellX+$textOffset),6,$textWidth,23))
    }
    foreach($design in $headerDesign) {
        $drawn=Get-ExperienceTarget $State $design[0]
        Assert-Experience ([Math]::Abs($drawn.x-$drawn.w*.5-$State.mapHudSafeX-$design[1]*$headerUnit) -lt .1 -and [Math]::Abs($drawn.y-$drawn.h*.5-$State.mapHudSafeY-$design[2]*$headerUnit) -lt .1 -and [Math]::Abs($drawn.w-$design[3]*$headerUnit) -lt .1 -and [Math]::Abs($drawn.h-$design[4]*$headerUnit) -lt .1) ($design[0]+' matches the actual exported city header geometry and safe area')
    }
}
function Compare-ExperienceCityHeaderPixels([string]$BeforePath,[string]$AfterPath,$State) {
    # This compares real captured pixels when called by publication. It detects
    # page-history-dependent painting; it cannot certify typography or art quality.
    Add-Type -AssemblyName System.Drawing
    $before=[Drawing.Bitmap]::new($BeforePath)
    $after=$null
    try {
        $after=[Drawing.Bitmap]::new($AfterPath)
        if($before.Width -ne $after.Width -or $before.Height -ne $after.Height){throw 'City header comparison requires equal capture dimensions.'}
        $unit=[double]$State.mapHudTouchMinimum/44.0
        if($unit -le 0 -or [double]$State.mapHudSafeWidth -le 0){throw 'City header comparison requires a positive safe area and scale.'}
        $left=[int][Math]::Ceiling([double]$State.mapHudSafeX)
        $top=[int][Math]::Ceiling([double]$State.mapHudSafeY)
        $right=[int][Math]::Floor([double]$State.mapHudSafeX+[double]$State.mapHudSafeWidth)
        $bottom=[int][Math]::Floor([double]$State.mapHudSafeY+70*$unit)
        if($left -lt 0 -or $top -lt 0 -or $right -gt $before.Width -or $bottom -gt $before.Height -or $right -le $left -or $bottom -le $top){throw 'City header comparison rectangle is outside its actual capture.'}
        $different=0;$first=$null
        for($y=$top;$y -lt $bottom;$y++){for($x=$left;$x -lt $right;$x++) {
            if($before.GetPixel($x,$y).ToArgb() -ne $after.GetPixel($x,$y).ToArgb()) {
                $different++
                if($null -eq $first){$first=@($x,$y)}
            }
        }}
        return [pscustomobject]@{before=$BeforePath;after=$AfterPath;bounds=@($left,$top,($right-$left),($bottom-$top));comparedPixels=($right-$left)*($bottom-$top);differentPixels=$different;firstDifferentPixel=$first;scope='Exact top-bar pixel equality after a paused ordinary-page round trip; independent visual review remains required.'}
    } finally {if($null -ne $after){$after.Dispose()};$before.Dispose()}
}
function Assert-ExperienceCityFooter($State) {
    Assert-Experience ($State.tab -eq '主城' -and $State.cityFooterDesignStyle -eq 'youzhou-reference-image-captions-v2' -and $State.cityFooterDesignTextFits) 'city footer uses the actual shared Inkscape image-above-caption design and fitted horizontal Chinese labels'
    $unit=$State.mapHudTouchMinimum/44.0
    Assert-Experience ($unit -gt 0) 'city footer uses the actual positive safe-area scale'
    $footerWidth=$State.mapHudSafeWidth/$unit;$footerHeight=$State.mapHudSafeHeight/$unit
    $footer=[pscustomobject]@{x=$State.mapHudSafeX+$State.mapHudSafeWidth/2;y=$State.mapHudSafeY+$State.mapHudSafeHeight-39*$unit;w=$State.mapHudSafeWidth;h=78*$unit}
    $ids=@('city-open','map-tab','roster-tab','reports-tab','hud-help','recruit-tab')
    $designs=@();$seen=@()
    for($i=0;$i -lt $ids.Count;$i++) {
        $id=$ids[$i];$x=20+116*$i;$y=$footerHeight-74;$w=108
        $art=@(4,3,100,42);$caption=@(4,46,100,22)
        if($i -eq 5){$x=$footerWidth-152;$w=132;$art=@(46,7,40,38);$caption=@(8,47,116,22)}
        $parent=Get-ExperienceTarget $State $id
        $image=Get-ExperienceTarget $State ($id+'-art');$text=Get-ExperienceTarget $State ($id+'-caption')
        foreach($regionId in @($id,($id+'-art'),($id+'-caption'))){Assert-ExperienceTargetInClient $State $regionId}
        Assert-Experience ((Test-ExperienceLandContains $footer $parent) -and (Test-ExperienceLandContains $parent $image) -and (Test-ExperienceLandContains $parent $text) -and -not (Test-ExperienceLandOverlap $image $text) -and $parent.w -ge ($State.mapHudTouchMinimum - .1) -and $parent.h -ge ($State.mapHudTouchMinimum - .1)) ($id+' has a complete touch target with separate visible image and caption inside the 78-unit footer')
        $placement=($image.y+$image.h/2) -le ($text.y-$text.h/2+.1)
        Assert-Experience $placement ($id+' preserves the designed image-above-horizontal-caption hierarchy')
        $designs+=@(@($id,$x,$y,$w,70),@(($id+'-art'),($x+$art[0]),($y+$art[1]),$art[2],$art[3]),@(($id+'-caption'),($x+$caption[0]),($y+$caption[1]),$caption[2],$caption[3]))
        foreach($other in $seen){Assert-Experience (-not (Test-ExperienceLandOverlap $parent $other)) ($id+' does not overlap any previous city footer action')}
        $seen+=,$parent
    }
    # The shared footer's inspected 064 revision leaves eight units between
    # save and time controls. Keep the exact geometry check at its original tolerance.
    $utilityX=[Math]::Max(620,($footerWidth-270)/2)
    foreach($utility in @(@('hud-save',$utilityX,82),@('speed-cycle',($utilityX+90),180))) {
        $parent=Get-ExperienceTarget $State $utility[0]
        Assert-ExperienceTargetInClient $State $utility[0]
        Assert-Experience ((Test-ExperienceLandContains $footer $parent) -and $parent.w -ge ($State.mapHudTouchMinimum - .1) -and $parent.h -ge ($State.mapHudTouchMinimum - .1)) ($utility[0]+' remains a complete safe-area touch command inside the shelf')
        foreach($other in $seen){Assert-Experience (-not (Test-ExperienceLandOverlap $parent $other)) ($utility[0]+' does not overlap any of the other eight footer controls')}
        $seen+=,$parent
        $designs+=,@($utility[0],$utility[1],($footerHeight-61),$utility[2],49)
    }
    foreach($design in $designs) {
        $actual=Get-ExperienceTarget $State $design[0]
        Assert-Experience ([Math]::Abs($actual.x-$actual.w/2-$State.mapHudSafeX-$design[1]*$unit) -lt .1 -and [Math]::Abs($actual.y-$actual.h/2-$State.mapHudSafeY-$design[2]*$unit) -lt .1 -and [Math]::Abs($actual.w-$design[3]*$unit) -lt .1 -and [Math]::Abs($actual.h-$design[4]*$unit) -lt .1) ($design[0]+' exactly matches the inspected city footer draft after safe-area scaling')
    }
}
function Convert-ExperienceCityBounds($Values) {
    Assert-Experience (@($Values).Count -eq 4) 'city composition exposes the actual drawn rectangle'
    return [pscustomobject]@{x=($Values[0]+$Values[2]*.5);y=($Values[1]+$Values[3]*.5);w=$Values[2];h=$Values[3]}
}
function Assert-ExperienceCityEdgeDock($State) {
    Assert-Experience ($State.tab -eq '主城' -and $State.cityPage -eq 'overview' -and $State.cityNavigationStyle -eq 'city-edge-dock-v1') 'overview exposes the actual left image navigation dock in compact and expanded views'
    Assert-Experience ($State.cityDockDesignStyle -eq 'inkscape-city-dock-v1' -and $State.cityDockDesignTextFits) 'floating dock uses the actual professional design with fitted Chinese navigation and return captions'
    $unit=$State.mapHudTouchMinimum/44.0
    Assert-Experience ($unit -gt 0) 'dock geometry uses the actual positive device scale'
    $dock=Convert-ExperienceCityBounds $State.cityNavigationDock
    $content=Convert-ExperienceCityBounds $State.cityContentCanvas
    $canvas=Convert-ExperienceCityBounds $State.cityPanoramaCanvas
    $title=Convert-ExperienceCityBounds $State.cityPanoramaTitleBounds
    Assert-Experience (-not (Test-ExperienceLandOverlap $dock $content) -and ($dock.x+$dock.w*.5) -lt ($content.x-$content.w*.5) -and [Math]::Abs(($dock.y-$dock.h*.5)-($content.y-$content.h*.5)) -lt .1 -and [Math]::Abs(($dock.y+$dock.h*.5)-($content.y+$content.h*.5)) -lt .1 -and (Test-ExperienceLandContains $content $canvas)) 'edge dock and raised city canvas are separate full-height siblings with the panorama contained in actual content'
    $expectedTitle=$State.cityHudFullName+$(if($State.cityPlanning){"`n建设选址"}else{"`n城内全景"})
    Assert-Experience ($State.cityPanoramaTitleText -eq $expectedTitle -and -not [string]::IsNullOrEmpty($State.cityHudFullName) -and (Test-ExperienceLandContains $canvas $title) -and $State.cityPanoramaTitleMeasuredHeight -gt 0 -and $State.cityPanoramaTitleMeasuredHeight -le 40.1 -and $State.cityPanoramaTitleMeasuredHeight*$State.mapHudTouchMinimum/44 -le ($title.h+.1)) 'panorama retains the full actual city name inside its measured two-line title area'
    $seen=@()
    foreach($pageId in @('overview','catalog','facilities','quests','training')) {
        $button=Get-ExperienceTarget $State ('city-'+$pageId)
        $art=Get-ExperienceTarget $State ('city-nav-'+$pageId+'-art')
        $caption=Get-ExperienceTarget $State ('city-nav-'+$pageId+'-caption')
        foreach($regionId in @(('city-'+$pageId),('city-nav-'+$pageId+'-art'),('city-nav-'+$pageId+'-caption'))) {Assert-ExperienceTargetInClient $State $regionId}
        Assert-Experience ((Test-ExperienceLandContains $dock $button) -and (Test-ExperienceLandContains $button $art) -and (Test-ExperienceLandContains $button $caption) -and -not (Test-ExperienceLandOverlap $art $caption) -and ($art.y+$art.h*.5) -le ($caption.y-$caption.h*.5+.1) -and $button.w -ge ($State.mapHudTouchMinimum-.1) -and $button.h -ge ($State.mapHudTouchMinimum-.1)) ($pageId+' edge navigation shows its actual image above a separate caption inside a complete touch action')
        Assert-Experience ([Math]::Abs($button.w-84*$unit) -lt .1 -and [Math]::Abs($button.h-86*$unit) -lt .1 -and [Math]::Abs($art.w-72*$unit) -lt .1 -and [Math]::Abs($art.h-54*$unit) -lt .1 -and [Math]::Abs($caption.w-80*$unit) -lt .1 -and [Math]::Abs($caption.h-24*$unit) -lt .1 -and [Math]::Abs($art.x-$button.x) -lt .1 -and [Math]::Abs($art.y-($button.y-14*$unit)) -lt .1 -and [Math]::Abs($caption.x-$button.x) -lt .1 -and [Math]::Abs($caption.y-($button.y+29*$unit)) -lt .1) ($pageId+' visible seal art and readable caption match the exported 84 by 86 design exactly')
        if($seen.Count -gt 0){Assert-Experience ([Math]::Abs($button.y-$seen[-1].y-90*$unit) -lt .1) 'floating city seals keep the reviewed 90-unit vertical rhythm'}
        foreach($other in $seen) {Assert-Experience (-not (Test-ExperienceLandOverlap $button $other)) ($pageId+' edge navigation does not overlap another action')}
        $seen+=,$button
    }
    $back=Get-ExperienceTarget $State 'city-back'
    Assert-ExperienceTargetInClient $State 'city-back'
    Assert-Experience ((Test-ExperienceLandContains $dock $back) -and $back.w -ge ($State.mapHudTouchMinimum-.1) -and $back.h -ge ($State.mapHudTouchMinimum-.1) -and ($back.y-$back.h*.5) -gt ($seen[-1].y+$seen[-1].h*.5) -and ($dock.y+$dock.h*.5)-($back.y+$back.h*.5) -le ($State.mapHudTouchMinimum*16/44+.1)) 'city return remains a separate reachable action at the bottom of the dock'
    foreach($other in $seen) {Assert-Experience (-not (Test-ExperienceLandOverlap $back $other)) 'bottom city return does not overlap a page entry'}
    $backArt=Get-ExperienceTarget $State 'city-dock-back-art'
    $backCaption=Get-ExperienceTarget $State 'city-dock-back-caption'
    foreach($backId in @('city-dock-back-art','city-dock-back-caption')){Assert-ExperienceTargetInClient $State $backId}
    Assert-Experience ((Test-ExperienceLandContains $back $backArt) -and (Test-ExperienceLandContains $back $backCaption) -and -not (Test-ExperienceLandOverlap $backArt $backCaption) -and [Math]::Abs($backArt.w-32*$unit) -lt .1 -and [Math]::Abs($backArt.h-32*$unit) -lt .1 -and [Math]::Abs($backArt.x-$back.x) -lt .1 -and [Math]::Abs($backArt.y-($back.y-10*$unit)) -lt .1 -and [Math]::Abs($backCaption.w-($back.w-4*$unit)) -lt .1 -and [Math]::Abs($backCaption.h-22*$unit) -lt .1 -and [Math]::Abs($backCaption.x-$back.x) -lt .1 -and [Math]::Abs($backCaption.y-($back.y+21*$unit)) -lt .1) 'bottom return exposes the real 32-unit artwork and separate readable caption inside its original 64-unit touch action'
}
function Assert-ExperienceCityOverview($State) {
    Assert-ExperienceCityPage $State 'overview'
    Assert-Experience ($State.cityOverviewVisible -and $State.cityOverviewStyle -eq 'panorama-side-army-v1' -and -not $State.cityExpanded) 'city overview draws the full-height panorama and vertical army side rail'
    $scene=$State.cityOverviewScene;$affairs=$State.cityOverviewAffairs;$army=$State.cityOverviewArmy
    Assert-Experience (@($scene).Count -eq 4 -and @($affairs).Count -eq 4 -and @($army).Count -eq 4) 'city review observes the three actual drawn panel bounds'
    $designedArmy=$State.PSObject.Properties.Name -contains 'cityArmyDesignStyle' -and $State.cityArmyDesignStyle -eq 'youzhou-city-army-tickets-v1'
    if($designedArmy) {
        $unit=$State.mapHudTouchMinimum/44.0
        $content=$State.cityContentCanvas
        $available=$scene[1]+$scene[3]-$army[1]
        $designedHeight=[Math]::Min($available-190*$unit,(($army[2]-32*$unit)/3-8*$unit)*1.45+48*$unit)+190*$unit
        Assert-Experience ($unit -gt 0 -and @($content).Count -eq 4 -and [Math]::Abs($scene[1]-$content[1]) -lt .1 -and [Math]::Abs($scene[3]-$content[3]) -lt .1 -and [Math]::Abs($army[3]-$designedHeight) -lt .1 -and ($army[1]+$army[3]) -le ($scene[1]+$scene[3]+.1)) 'city panorama retains the entire content height and the designed army uses its exact compact portrait footprint'
        Assert-Experience (($scene[0]+$scene[2]) -lt $affairs[0] -and ($affairs[1]+$affairs[3]) -lt $army[1]) 'compact army and affairs remain separated beside the full-height city'
    } else {
        # Preserve the recorded pre-design geometry regression without rewriting its fixture.
        Assert-Experience (($scene[0]+$scene[2]) -lt $affairs[0] -and ($affairs[1]+$affairs[3]) -lt $army[1] -and [Math]::Abs(($scene[1]+$scene[3])-($army[1]+$army[3])) -lt .1) 'panorama fills the content height beside separated affairs and army panels'
    }
    $ids=@('city-overview-facilities','city-construction-page','city-overview-quests','city-formation','city-recruit','city-army-training')
    for($index=0;$index -lt @($State.cityOverviewArmyUids).Count;$index++) {$ids+='city-general-'+$index}
    $cards=@($State.targets | Where-Object {$_.id -match '^city-general-[0-2]$'})
    Assert-Experience ($cards.Count -eq @($State.cityOverviewArmyUids).Count) 'city card targets correspond exactly to the currently deployed owned generals'
    $rects=@()
    foreach($id in $ids) {
        Assert-ExperienceTargetInClient $State $id
        $rect=Get-ExperienceTarget $State $id
        $parent=if($id -in @('city-overview-facilities','city-construction-page','city-overview-quests')){$affairs}else{$army}
        Assert-Experience (($rect.x-$rect.w*.5) -ge ($parent[0]-.1) -and ($rect.y-$rect.h*.5) -ge ($parent[1]-.1) -and ($rect.x+$rect.w*.5) -le ($parent[0]+$parent[2]+.1) -and ($rect.y+$rect.h*.5) -le ($parent[1]+$parent[3]+.1)) ($id+' lies inside its actual city side panel')
        if($id -like 'city-general-*'){Assert-Experience ($rect.h -gt $rect.w) ($id+' presents a vertical portrait')}
        foreach($other in $rects) {
            $overlap=[Math]::Abs($rect.x-$other.x) -lt (($rect.w+$other.w)*.5-.1) -and [Math]::Abs($rect.y-$other.y) -lt (($rect.h+$other.h)*.5-.1)
            if($overlap){throw ('EXPERIENCE_ASSERT: City side controls overlap: '+$id+' / '+$other.id)}
        }
        $rects+=,$rect
    }
}
function Assert-ExperienceCityArmyComposition($State) {
    Assert-Experience ($State.cityArmyCompositionStyle -eq 'formation-standards-muster-v2') 'city army draws the formation standards, strength bar, supply row and illustrated command group'
    Assert-Experience ($State.cityArmyDesignStyle -eq 'youzhou-city-army-tickets-v1' -and $State.cityArmyDesignTextFits) 'city army uses the inspected compact Inkscape design and actual-font fitting'
    Assert-Experience ($State.cityArmyBusy -eq $State.orderActive -and $State.cityArmyDisplayedTroops -eq $State.troops -and $State.cityArmyDisplayedCap -eq $State.cityArmyActualCap -and $State.cityArmyDisplayedRecruit -eq $State.cityArmyActualRecruit) 'city army strength, capacity and recruitment quantity come from the actual current campaign'
    Assert-Experience (@($State.cityArmyDisplayedCosts).Count -eq 3 -and @($State.cityArmyActualCosts).Count -eq 3) 'city army exposes all three displayed resource prices and independent Core recruitment quote'
    for($index=0;$index -lt 3;$index++) {
        Assert-Experience ([Math]::Abs($State.cityArmyDisplayedCosts[$index]-$State.cityArmyActualCosts[$index]) -lt .00001) ('city army displayed recruitment cost matches Core resource '+$index)
    }
    $army=Convert-ExperienceCityBounds $State.cityOverviewArmy
    $strength=Convert-ExperienceCityBounds $State.cityArmyStrength
    $supply=Convert-ExperienceCityBounds $State.cityArmySupply
    $unit=$State.mapHudTouchMinimum/44.0
    Assert-Experience ((Test-ExperienceLandContains $army $strength) -and (Test-ExperienceLandContains $army $supply) -and -not (Test-ExperienceLandOverlap $strength $supply) -and $strength.h -ge 28*$unit-.1 -and $supply.h -ge 40*$unit-.1) 'city army keeps separate visible strength and supply rows inside the panel'
    $scene=$State.cityOverviewScene;$affairs=$State.cityOverviewAffairs
    $contentWidth=$affairs[0]+$affairs[2]-$scene[0]
    Assert-Experience ($scene[2] -ge .6*$contentWidth-.1) 'city panorama retains at least sixty percent of the content width beside the responsive army panel'
    Assert-Experience (@($State.cityArmyActionArt).Count -eq 12 -and @($State.cityArmyActionCaptions).Count -eq 12 -and @($State.cityArmySupplyIcons).Count -eq $(if($State.cityArmyBusy){0}else{12})) 'three commands expose actual art and title geometry, with supply icons only during idle recruitment'
    $parents=@('city-formation','city-recruit','city-army-training')
    for($index=0;$index -lt 3;$index++) {
        $offset=$index*4
        $slot=Convert-ExperienceCityBounds (@($State.cityOverviewGeneralSlots)[$offset..($offset+3)])
        Assert-Experience (($slot.y+$slot.h*.5) -le ($strength.y-$strength.h*.5-5.9*$unit)) ('army standard '+$index+' clears the actual strength row')
        $parent=Get-ExperienceTarget $State $parents[$index]
        $art=Convert-ExperienceCityBounds (@($State.cityArmyActionArt)[$offset..($offset+3)])
        $caption=Convert-ExperienceCityBounds (@($State.cityArmyActionCaptions)[$offset..($offset+3)])
        Assert-Experience ((Test-ExperienceLandContains $parent $art) -and (Test-ExperienceLandContains $parent $caption) -and -not (Test-ExperienceLandOverlap $art $caption) -and $art.w -ge 32*$unit-.1 -and $art.h -ge 32*$unit-.1 -and $parent.h -ge $State.mapHudTouchMinimum-.1 -and ($supply.y+$supply.h*.5) -le ($parent.y-$parent.h*.5-7.9*$unit)) ('army action '+$index+' has a visible illustration and separate title inside the actual 44-unit command')
        foreach($part in @('art','caption')) {
            $id='city-army-action-'+$index+'-'+$part
            if($index -eq 0 -and $State.cityArmyBusy) {
                Assert-Experience (@($State.targets | Where-Object {$_.id -eq $id}).Count -eq 0) 'busy army exposes no disabled formation illustration or title target'
            } else {
                $drawn=if($part -eq 'art'){$art}else{$caption}
                $target=Get-ExperienceTarget $State $id
                Assert-ExperienceTargetInClient $State $id
                Assert-Experience ([Math]::Abs($target.x-$drawn.x)+[Math]::Abs($target.y-$drawn.y)+[Math]::Abs($target.w-$drawn.w)+[Math]::Abs($target.h-$drawn.h) -lt .1) ($id+' matches the real drawn child area of the command')
            }
        }
        if(-not $State.cityArmyBusy) {
            $icon=Convert-ExperienceCityBounds (@($State.cityArmySupplyIcons)[$offset..($offset+3)])
            Assert-Experience ((Test-ExperienceLandContains $supply $icon) -and $icon.w -ge 22*$unit-.1 -and $icon.h -ge 20*$unit-.1) ('recruitment resource '+$index+' has a visible supply icon')
        }
    }
}
function Assert-ExperienceCityAffairsComposition($State) {
    Assert-ExperienceCityOverview $State
    Assert-ExperienceCityEdgeDock $State
    Assert-ExperienceCityArmyComposition $State
    foreach($pageId in @('overview','catalog','facilities','quests','training')) {
        $button=Get-ExperienceTarget $State ('city-'+$pageId)
        $art=Get-ExperienceTarget $State ('city-nav-'+$pageId+'-art')
        $caption=Get-ExperienceTarget $State ('city-nav-'+$pageId+'-caption')
        foreach($regionId in @(('city-'+$pageId),('city-nav-'+$pageId+'-art'),('city-nav-'+$pageId+'-caption'))) {Assert-ExperienceTargetInClient $State $regionId}
        Assert-Experience ((Test-ExperienceLandContains $button $art) -and (Test-ExperienceLandContains $button $caption) -and -not (Test-ExperienceLandOverlap $art $caption) -and $button.h -ge ($State.mapHudTouchMinimum-.1)) ($pageId+' navigation keeps a visible image and separate caption in its original 44-unit touch control')
    }
    $designedAffairs=$State.cityAffairsCompositionStyle -eq 'inkscape-city-affairs-v1'
    Assert-Experience (($designedAffairs -or $State.cityAffairsCompositionStyle -eq 'facility-medallion-army-plinth-v1') -and $State.cityAffairsWorking -eq $State.cityBuilding -and $State.cityAffairsDisplayedKind -eq $State.cityAffairsActualKind -and $State.cityAffairsDisplayedLevel -eq $State.cityAffairsActualLevel) 'city affairs shows the actual idle hall or live construction identity and displayed current/target level'
    Assert-Experience ($State.cityAffairsActualPlot -eq $(if($State.cityBuilding){$State.cityConstructionPlot}else{4})) 'city affairs illustration identifies the real hall or active construction plot'
    $affairsPanel=Convert-ExperienceCityBounds $State.cityOverviewAffairs
    $affairsImage=Convert-ExperienceCityBounds $State.cityAffairsArt
    $affairsSummary=Convert-ExperienceCityBounds $State.cityAffairsSummary
    $affairsTarget=Get-ExperienceTarget $State 'city-affairs-art'
    Assert-ExperienceTargetInClient $State 'city-affairs-art'
    Assert-Experience ((Test-ExperienceLandContains $affairsPanel $affairsImage) -and (Test-ExperienceLandContains $affairsPanel $affairsSummary) -and -not (Test-ExperienceLandOverlap $affairsImage $affairsSummary) -and [Math]::Abs($affairsImage.x-$affairsTarget.x)+[Math]::Abs($affairsImage.y-$affairsTarget.y)+[Math]::Abs($affairsImage.w-$affairsTarget.w)+[Math]::Abs($affairsImage.h-$affairsTarget.h) -lt .1 -and $affairsImage.h -ge ($State.mapHudTouchMinimum*2-.1)) 'city facility medallion has a large real touch area separate from its factual summary'
    $questEntry=Get-ExperienceTarget $State 'city-overview-quests'
    $questImage=Get-ExperienceTarget $State 'city-overview-quests-art'
    $questDrawn=Convert-ExperienceCityBounds $State.cityAffairsQuestArt
    Assert-ExperienceTargetInClient $State 'city-overview-quests-art'
    Assert-Experience ((Test-ExperienceLandContains $questEntry $questImage) -and $questEntry.h -ge ($State.mapHudTouchMinimum-.1) -and [Math]::Abs($questImage.x-$questDrawn.x)+[Math]::Abs($questImage.y-$questDrawn.y)+[Math]::Abs($questImage.w-$questDrawn.w)+[Math]::Abs($questImage.h-$questDrawn.h) -lt .1) 'chapter scroll art belongs to the visible 44-unit chapter entry'
    if($designedAffairs) {
        $unit=$State.mapHudTouchMinimum/44.0
        $building=Get-ExperienceTarget $State 'city-affairs-building'
        $caption=Get-ExperienceTarget $State 'city-affairs-caption'
        $questCaption=Get-ExperienceTarget $State 'city-overview-quests-caption'
        foreach($id in @('city-affairs-building','city-affairs-caption','city-overview-quests-caption')) {Assert-ExperienceTargetInClient $State $id}
        Assert-Experience ((Test-ExperienceLandContains $affairsImage $building) -and (Test-ExperienceLandContains $affairsImage $caption) -and -not (Test-ExperienceLandOverlap $building $caption) -and (Test-ExperienceLandContains $questEntry $questCaption) -and -not (Test-ExperienceLandOverlap $questImage $questCaption)) 'exported city affairs keeps building/name and chapter image/title in distinct visible subregions of their single parent commands'
        Assert-Experience ($State.cityAffairsDesignTextFits -and [Math]::Abs($affairsPanel.h-218*$unit) -lt .1 -and [Math]::Abs($affairsImage.w-112*$unit)+[Math]::Abs($affairsImage.h-112*$unit) -lt .1 -and [Math]::Abs($building.w-104*$unit)+[Math]::Abs($building.h-78*$unit) -lt .1 -and [Math]::Abs($caption.w-88*$unit)+[Math]::Abs($caption.h-26*$unit) -lt .1) 'city affairs matches the real exported seal/building/caption geometry and actual-font fit inside the original footprint'
        $commands=@((Get-ExperienceTarget $State 'city-overview-facilities'),(Get-ExperienceTarget $State 'city-construction-page'),$questEntry)
        foreach($command in $commands) {Assert-Experience ((Test-ExperienceLandContains $affairsPanel $command) -and [Math]::Abs($command.h-44*$unit) -lt .1 -and -not (Test-ExperienceLandOverlap $command $affairsImage) -and -not (Test-ExperienceLandOverlap $command $affairsSummary)) 'independent affairs commands clear the seal and factual construction strip with full touch height'}
        for($i=0;$i -lt $commands.Count;$i++) {for($j=$i+1;$j -lt $commands.Count;$j++) {Assert-Experience (-not (Test-ExperienceLandOverlap $commands[$i] $commands[$j])) 'city affairs manage, construction and chapter entries do not overlap'}}
    }
    Assert-Experience (@($State.cityOverviewGeneralSlots).Count -eq 12 -and @($State.cityOverviewGeneralArt).Count -eq 12 -and @($State.cityOverviewGeneralNames).Count -eq 12) 'all three army slots expose their actual painting and separate identity bounds'
    for($index=0;$index -lt 3;$index++) {
        $offset=$index*4
        $slot=Convert-ExperienceCityBounds (@($State.cityOverviewGeneralSlots)[$offset..($offset+3)])
        $portrait=Convert-ExperienceCityBounds (@($State.cityOverviewGeneralArt)[$offset..($offset+3)])
        $identity=Convert-ExperienceCityBounds (@($State.cityOverviewGeneralNames)[$offset..($offset+3)])
        Assert-Experience ((Test-ExperienceLandContains $slot $portrait) -and (Test-ExperienceLandContains $slot $identity) -and -not (Test-ExperienceLandOverlap $portrait $identity) -and $portrait.h -ge $slot.h*.68 -and $identity.h -ge ($State.mapHudTouchMinimum*40/44-.1)) ('army slot '+$index+' keeps the source painting clear of a readable name and level base')
    }
}
function Assert-ExperienceCityLandscape($State) {
    Assert-Experience ($State.cityLandscapeStyle -eq 'courtyard-landscape-v2' -and $State.cityLandscapeTextureWidth -eq 1800 -and $State.cityLandscapeTextureHeight -eq 880) 'city uses the complete production courtyard landscape'
    $landscape=@($State.cityLandscapeBounds);$viewport=@($State.cityLandscapeViewport)
    Assert-Experience ($landscape.Count -eq 4 -and $viewport.Count -eq 4) 'city landscape and actual viewport have visible rectangle evidence'
    Assert-Experience ($landscape[2] -gt 0 -and $landscape[3] -gt 0 -and $viewport[2] -gt 0 -and $viewport[3] -gt 0 -and $landscape[0] -le $viewport[0]+.1 -and $landscape[1] -le $viewport[1]+.1 -and $landscape[0]+$landscape[2] -ge $viewport[0]+$viewport[2]-.1 -and $landscape[1]+$landscape[3] -ge $viewport[1]+$viewport[3]-.1) 'city landscape fills the actual viewport without a blank letterbox after input'
    $townScale=$State.cityPanoramaImagePixelWidth/1000.0
    Assert-Experience ([Math]::Abs($landscape[0]+400*$townScale-$State.cityPanoramaImagePixelX) -lt .1 -and [Math]::Abs($landscape[1]+220*$townScale-$State.cityPanoramaImagePixelY) -lt .1 -and [Math]::Abs($landscape[2]/1800.0-$townScale) -lt .0001) 'city scenery and clickable facilities share the original town coordinates'
}
function Assert-ExperienceCityPanoramaExpanded($Before,$After) {
    Assert-ExperienceCityPage $After 'overview'
    Assert-Experience (-not $Before.cityExpanded -and $Before.cityOverviewVisible -and $After.cityExpanded -and -not $After.cityOverviewVisible) 'expand replaces the compact city side rail with the panorama'
    Assert-Experience ($Before.speed -eq 0 -and $After.speed -eq 0 -and -not [string]::IsNullOrEmpty($Before.battleWorldHash) -and $After.battleWorldHash -eq $Before.battleWorldHash) 'expanding the paused city preserves the complete campaign without charging'
    $beforeViewport=Get-ExperienceTarget $Before 'city-panorama-viewport'
    $afterViewport=Get-ExperienceTarget $After 'city-panorama-viewport'
    # The default panorama already fills the content height. Expansion reclaims
    # the side rail horizontally; compare the actual visible area, not one axis.
    $beforeArea=[double]$Before.cityPanoramaViewportWidth*[double]$Before.cityPanoramaViewportHeight
    $afterArea=[double]$After.cityPanoramaViewportWidth*[double]$After.cityPanoramaViewportHeight
    $beforePixelArea=[double]$beforeViewport.w*[double]$beforeViewport.h
    $afterPixelArea=[double]$afterViewport.w*[double]$afterViewport.h
    Assert-Experience ($beforeArea -gt 0 -and $beforePixelArea -gt 0 -and $afterArea -gt $beforeArea+1 -and $afterPixelArea -gt $beforePixelArea+1) 'expanded city has a larger drawn viewport area in both logical and client coordinates'
    Assert-Experience ([Math]::Abs($afterArea/$beforeArea-$afterPixelArea/$beforePixelArea) -lt .001) 'expanded viewport diagnostics match the actual visible input geometry at the current UI scale'
    Assert-Experience ($afterViewport.x-$afterViewport.w/2 -le $beforeViewport.x-$beforeViewport.w/2+.1 -and $afterViewport.x+$afterViewport.w/2 -ge $beforeViewport.x+$beforeViewport.w/2-.1 -and $afterViewport.y-$afterViewport.h/2 -le $beforeViewport.y-$beforeViewport.h/2+.1 -and $afterViewport.y+$afterViewport.h/2 -ge $beforeViewport.y+$beforeViewport.h/2-.1) 'expanded city contains the previous viewport without trading away visible height or width'
    Assert-Experience (@($After.targets | Where-Object {$_.id -match '^city-general-[0-2]$|^city-army-action-|^city-affairs-' -or $_.id -in @('city-overview-facilities','city-construction-page','city-overview-quests','city-overview-quests-art','city-overview-quests-caption','city-formation','city-recruit','city-army-training')}).Count -eq 0) 'expanded city exposes no controls from the removed affairs or army side rail'
    foreach($id in @('city-panorama-expand','city-panorama-plus','city-panorama-minus','city-panorama-reset','city-panorama-viewport','city-planning','city-back')) {Assert-ExperienceTargetInClient $After $id}
    Assert-ExperienceMapStill $Before $After 'city panorama expansion'
}
function Assert-ExperienceCityPanoramaRestored($Before,$After) {
    Assert-ExperienceCityOverview $After
    Assert-Experience (-not $After.cityPlanning -and $After.battleWorldHash -eq $Before.battleWorldHash) 'collapsing the panorama restores the city side rail without construction or campaign changes'
    $beforeViewport=Get-ExperienceTarget $Before 'city-panorama-viewport'
    $afterViewport=Get-ExperienceTarget $After 'city-panorama-viewport'
    Assert-Experience ([Math]::Abs($beforeViewport.x-$afterViewport.x)+[Math]::Abs($beforeViewport.y-$afterViewport.y)+[Math]::Abs($beforeViewport.w-$afterViewport.w)+[Math]::Abs($beforeViewport.h-$afterViewport.h) -lt .1) 'city back restores the exact original visible panorama bounds'
    Assert-ExperienceMapStill $Before $After 'city panorama collapse'
}
function Assert-ExperienceQuestReceipt($Before,$After,[int]$Id,[int]$Next,[double[]]$Reward,[bool]$ChapterComplete=$false) {
    Assert-Experience ($Before.speed -eq 0 -and $After.speed -eq 0) ('quest '+$Id+' reward is measured with the actual clock paused')
    Assert-Experience ($After.receiptOpen -and $After.receiptId -eq $Id -and $After.receiptNext -eq $Next -and -not [string]::IsNullOrEmpty($After.receiptTitle) -and $After.receiptChapterComplete -eq $ChapterComplete -and -not $After.receiptAllComplete) ('quest '+$Id+' displays its actual receipt identity and next objective')
    Assert-Experience (@($After.receiptCredited).Count -eq 5 -and @($After.receiptBalance).Count -eq 5) ('quest '+$Id+' receipt exposes all five actual resource amounts')
    for($resourceIndex=0;$resourceIndex -lt 5;$resourceIndex++) {
        Assert-Experience ([Math]::Abs($After.resources[$resourceIndex]-$Before.resources[$resourceIndex]-$Reward[$resourceIndex]) -lt .00001 -and [Math]::Abs($After.receiptCredited[$resourceIndex]-$Reward[$resourceIndex]) -lt .00001 -and [Math]::Abs($After.receiptBalance[$resourceIndex]-$After.resources[$resourceIndex]) -lt .00001) ('quest '+$Id+' receipt matches the exact credit and balance for resource '+$resourceIndex)
    }
    Assert-Experience ($After.jade -eq $Before.jade -and $After.claimedQuests -eq ($Before.claimedQuests+1)) ('quest '+$Id+' credits only one normal reward and no jade')
    foreach($targetId in @('quest-reward-modal','quest-reward-close','quest-reward-done','quest-reward-next')) {$null=Get-ExperienceTarget $After $targetId}
    Assert-Experience (@($After.targets).Count -eq 4 -and @($After.visibleQuestIds).Count -eq 0) ('quest '+$Id+' diagnostic targets contain only the four visible receipt controls')
}
function Wait-ExperienceState([string]$Name,[scriptblock]$Condition,[int]$Seconds=20) {
    $end=[DateTime]::UtcNow.AddSeconds($Seconds);$attempt=0
    do {
        $current=Capture-Experience ($Name+'-'+$attempt);$attempt++
        if(& $Condition $current){return $current}
        Start-Sleep -Milliseconds 250;Assert-ExperiencePlayerAvailable
    } while([DateTime]::UtcNow -lt $end)
    throw ('EXPERIENCE_ASSERT: Timed out waiting for '+$Name)
}
function Get-ExperienceSaveHashes {
    $result=[ordered]@{}
    foreach($name in @('campaign.json','campaign.json.bak','youzhou-40-v1.json','youzhou-40-v1.json.bak')) {
        $path=Join-Path $edExperienceSaveDirectory $name
        $result[$name]=if(Test-Path -LiteralPath $path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash}else{$null}
    }
    return $result
}
function Restart-ExperienceReview([string]$LogName,[switch]$ReportArchive,[switch]$OwnedRoster) {
    # Each scenario uses a fresh read-only client and its own campaign. Never run
    # two review clients together or alter a player's existing campaign.
    Focus-Experience
    $restartInputTick=[EDExperiencePointer]::LastInputTick()
    if(-not $edExperienceProcess.CloseMainWindow()){throw 'Could not close the first read-only review client.'}
    if(-not $edExperienceProcess.WaitForExit(5000)){throw 'First review client is still running; refusing to start a second client.'}
    Assert-Experience $edExperienceProcess.HasExited 'first read-only review process exits before the second fresh scenario starts'
    $script:edExperienceHadFocus=$false
    $script:edExperienceProcess=$null
    Assert-ExperiencePlayerAvailable
    $reviewArguments=@('-edReview','-screen-fullscreen','0','-screen-width',[string]$PixelWidth,'-screen-height',[string]$PixelHeight,'-logFile',(Join-Path $edExperienceOutput $LogName))
    if($ReportArchive){$reviewArguments+='-edReviewReportArchive'}
    if($OwnedRoster){$reviewArguments+='-edOwnedRosterReview'}
    $arguments=ConvertTo-EdProcessArguments $reviewArguments
    $script:edExperienceProcess=Start-Process -FilePath $ExecutablePath -ArgumentList $arguments -WindowStyle Normal -PassThru
    $windowDeadline=[DateTime]::UtcNow.AddSeconds(20)
    do {$edExperienceProcess.Refresh();if($edExperienceProcess.MainWindowHandle -ne 0){break};Start-Sleep -Milliseconds 200} while([DateTime]::UtcNow -lt $windowDeadline -and -not $edExperienceProcess.HasExited)
    if($edExperienceProcess.HasExited -or $edExperienceProcess.MainWindowHandle -eq 0){throw 'No second read-only review client window appeared.'}
    Start-Sleep -Seconds 7
    $client=[EDExperiencePointer+RECT]::new()
    if(-not [EDExperiencePointer]::GetClientRect($edExperienceProcess.MainWindowHandle,[ref]$client)){throw 'Could not measure the second review client area.'}
    $script:cw=[double]($client.Right-$client.Left);$script:ch=[double]($client.Bottom-$client.Top)
    if([EDExperiencePointer]::LastInputTick() -ne $restartInputTick){throw 'DESKTOP_BUSY: New system input occurred during review client restart; no pointer movement or focus recovery will continue.'}
    Assert-ExperiencePlayerAvailable
    Focus-Experience
    Move-Experience ($cw*.5) ($ch*.48)
}
function Assert-ExperienceTargetInClient($State,[string]$Id) {
    $target=Get-ExperienceTarget $State $Id
    Assert-Experience ($target.w -gt 0 -and $target.h -gt 0 -and ($target.x-$target.w*.5) -ge 0 -and ($target.y-$target.h*.5) -ge 0 -and ($target.x+$target.w*.5) -le $cw -and ($target.y+$target.h*.5) -le $ch) ($Id+' is fully inside the actual client at '+$PixelWidth+'x'+$PixelHeight)
}
function Assert-ExperienceFacilityAtlas($State) {
    Assert-ExperienceCityPage $State 'catalog'
    Assert-Experience ($State.cityCatalogStyle -eq 'inkscape-facility-atlas-v2' -and @($State.cityCatalogNodes).Count -gt 0) 'facility atlas records actual visible building nodes from the inspected design'
    Assert-Experience ($State.cityCatalogDesignReady -and $State.cityCatalogDesignTextFits) 'facility atlas loads its exported Inkscape components and fits actual Chinese GUI fonts'
    $viewport=Get-ExperienceTarget $State 'city-catalog-scroll';$canvas=@($State.cityCatalogCanvas);$hall=@($State.cityCatalogHallArt)
    $unit=$State.mapHudTouchMinimum/44.0
    Assert-Experience ($canvas.Count -eq 4 -and $hall.Count -eq 4 -and $hall[0]+$hall[2] -lt $canvas[0]) 'city hall artwork and building sheet occupy separate regions'
    Assert-Experience ([Math]::Abs($hall[2]-184*$unit)+[Math]::Abs($hall[3]-138*$unit) -lt .1) 'hall illustration retains the inspected complete rectangular 184 by 138 design'
    Assert-Experience ([Math]::Abs($viewport.x-$viewport.w*.5-$canvas[0])+[Math]::Abs($viewport.y-$viewport.h*.5-$canvas[1])+[Math]::Abs($viewport.w-$canvas[2])+[Math]::Abs($viewport.h-$canvas[3]) -lt .1) 'atlas scrolling uses its actual drawn canvas'
    Assert-Experience ((@($State.cityCatalogNodes | ForEach-Object {$_.id} | Sort-Object) -join ',') -eq ($State.visibleFacilityIds -join ',')) 'visible atlas identities match the fully drawn nodes'
    $columns=if($canvas[2]/$unit -ge 1460){3}else{2}
    $cardWidth=($canvas[2]-24*$unit-16*$unit*($columns-1))/$columns
    foreach($node in @($State.cityCatalogNodes)) {
        $card=Get-ExperienceTarget $State ('city-catalog-card-'+$node.id);$art=@($node.art);$action=@($node.action);$name=@($node.name)
        $cardX=$card.x-$card.w*.5;$cardY=$card.y-$card.h*.5
        Assert-ExperienceTargetInClient $State $card.id
        Assert-Experience ([Math]::Abs($card.w-$cardWidth)+[Math]::Abs($card.h-206*$unit) -lt .1) 'horizontal atlas node matches its inspected responsive column width and 206-high layout'
        Assert-Experience ($art.Count -eq 4 -and $action.Count -eq 4 -and $art[2] -ge ($State.mapHudTouchMinimum*160/44)-.1 -and $art[3] -ge ($State.mapHudTouchMinimum*160/44)-.1) 'facility building image remains substantially larger than an icon'
        Assert-Experience ($cardX -ge $canvas[0]-.1 -and $cardX+$card.w -le $canvas[0]+$canvas[2]+.1 -and $cardY -ge $canvas[1]-.1 -and $cardY+$card.h -le $canvas[1]+$canvas[3]+.1 -and $art[0]+$art[2] -lt $action[0]) 'complete horizontal image and its right-side action remain inside the visible scroll node'
        foreach($part in @(@('art',10,14,216,162),@('name',238,8,($card.w/$unit-250),32),@('state',238,42,($card.w/$unit-250),22),@('effect',238,72,($card.w/$unit-250),24),@('condition',238,102,($card.w/$unit-250),28),@('action',238,150,($card.w/$unit-250),44))) {
            $bounds=@($node.($part[0]))
            Assert-Experience ($bounds.Count -eq 4 -and [Math]::Abs($bounds[0]-$cardX-$part[1]*$unit)+[Math]::Abs($bounds[1]-$cardY-$part[2]*$unit)+[Math]::Abs($bounds[2]-$part[3]*$unit)+[Math]::Abs($bounds[3]-$part[4]*$unit) -lt .2) ('facility '+$part[0]+' matches its inspected Inkscape bounds')
            Assert-Experience ($bounds[0] -ge $cardX-.1 -and $bounds[1] -ge $cardY-.1 -and $bounds[0]+$bounds[2] -le $cardX+$card.w+.1 -and $bounds[1]+$bounds[3] -le $cardY+$card.h+.1) ('facility '+$part[0]+' stays completely inside its visible node')
        }
        $stateRow=@($node.state);$effect=@($node.effect);$condition=@($node.condition)
        Assert-Experience ($art[0]+$art[2] -lt $name[0] -and $name[1]+$name[3] -le $stateRow[1] -and $stateRow[1]+$stateRow[3] -le $effect[1] -and $effect[1]+$effect[3] -le $condition[1] -and $condition[1]+$condition[3] -le $action[1]) 'complete illustration clears the ordered title, level, effect, prerequisite and command rows'
        $imageTarget=@($State.targets | Where-Object {$_.id -eq ('city-catalog-image-'+$node.id)})
        $nameTarget=@($State.targets | Where-Object {$_.id -eq ('city-catalog-name-'+$node.id)})
        $goTarget=@($State.targets | Where-Object {$_.id -eq ('city-catalog-go-'+$node.id)})
        Assert-Experience ($imageTarget.Count -eq $nameTarget.Count -and $imageTarget.Count -eq $goTarget.Count) 'image, name and action share exactly the same real navigation availability'
        if($imageTarget.Count -gt 0) {
            $image=$imageTarget[0];Assert-ExperienceTargetInClient $State $image.id
            Assert-Experience ([Math]::Abs($image.x-$image.w*.5-$art[0])+[Math]::Abs($image.y-$image.h*.5-$art[1])+[Math]::Abs($image.w-$art[2])+[Math]::Abs($image.h-$art[3]) -lt .1) 'clickable building image matches the actually drawn artwork'
            $title=$nameTarget[0];Assert-ExperienceTargetInClient $State $title.id
            Assert-Experience ([Math]::Abs($title.x-$title.w*.5-$name[0])+[Math]::Abs($title.y-$title.h*.5-$name[1])+[Math]::Abs($title.w-$name[2])+[Math]::Abs($title.h-$name[3]) -lt .1) 'clickable building name matches its actually drawn title'
            $go=Get-ExperienceTarget $State ('city-catalog-go-'+$node.id)
            Assert-ExperienceTargetInClient $State $go.id
            Assert-Experience ($go.h -ge $State.mapHudTouchMinimum-.1 -and [Math]::Abs($go.x-$go.w*.5-$action[0])+[Math]::Abs($go.y-$go.h*.5-$action[1])+[Math]::Abs($go.w-$action[2])+[Math]::Abs($go.h-$action[3]) -lt .1) 'facility action retains a separate 44 logical pixel touch target'
        }
    }
    foreach($category in 0..3) {Assert-ExperienceTargetInClient $State ('city-catalog-category-'+$category)}
}
function Assert-ExperienceFacilityDossier($State,[bool]$CostsVisible=$true) {
    Assert-ExperienceCityPage $State 'facilities'
    Assert-Experience ($State.cityFacilityStyle -eq 'inkscape-facility-sheet-v1' -and $State.cityFacilityPortraitId -eq $State.cityFacility) 'facility artwork identifies the actually selected or constructing facility'
    Assert-Experience ($State.cityFacilityDesignAssetsReady -and $State.cityFacilityDesignTextFits) 'facility sheet uses all five exported skins and fits actual Chinese GUI fonts'
    Assert-Experience ($State.cityFacilityGalleryStyle -eq 'inkscape-facility-gallery-v1' -and $State.cityFacilityGalleryReady -and $State.cityFacilityGalleryTextFits) 'facility gallery loads its three real Inkscape skins and fits actual Chinese names and states'
    $portrait=@($State.cityFacilityPortrait);$ledger=@($State.cityFacilityLedger);$action=@($State.cityFacilityAction)
    Assert-Experience ($portrait.Count -eq 4 -and $ledger.Count -eq 4 -and $action.Count -eq 4) 'facility artwork, ledger and fixed action have actual drawing geometry'
    Assert-Experience ($portrait[2] -gt 0 -and $portrait[3] -gt 0 -and $portrait[0]+$portrait[2] -lt $ledger[0] -and $ledger[1]+$ledger[3] -le $action[1]) 'rectangular building stage and ledger/action occupy separate visible regions'
    $unit=$State.mapHudTouchMinimum/44.0
    $art=@($State.cityFacilityGalleryImage);$footer=@($State.cityFacilityGalleryFooter)
    Assert-Experience ($art.Count -eq 4 -and $footer.Count -eq 4) 'facility complete illustration and footer expose actual drawn bounds'
    Assert-Experience ([Math]::Abs($art[0]-$portrait[0]-12*$unit)+[Math]::Abs($art[1]-$portrait[1]-22*$unit)+[Math]::Abs($art[2]-$portrait[2]+24*$unit)+[Math]::Abs($art[3]-$portrait[3]+58*$unit) -lt .2) 'complete facility illustration matches the inspected Inkscape image area'
    Assert-Experience ([Math]::Abs($footer[0]-$portrait[0])+[Math]::Abs($footer[1]-$portrait[1]-$portrait[3]+34*$unit)+[Math]::Abs($footer[2]-$portrait[2])+[Math]::Abs($footer[3]-34*$unit) -lt .2 -and $art[1]+$art[3] -le $footer[1]) 'independent 34-high name/level footer clears the whole illustration'
    $scroll=Get-ExperienceTarget $State 'city-detail-scroll'
    Assert-Experience ([Math]::Abs($scroll.x-$scroll.w*.5-$ledger[0])+[Math]::Abs($scroll.y-$scroll.h*.5-$ledger[1])+[Math]::Abs($scroll.w-$ledger[2])+[Math]::Abs($scroll.h-$ledger[3]) -lt .1) 'scroll input targets the drawn facility ledger'
    if($CostsVisible) {
        $costs=@($State.cityFacilityCosts)
        $description=@($State.cityFacilityDescription);$effect=@($State.cityFacilityEffectRow)
        Assert-Experience ($description.Count -eq 4 -and $effect.Count -eq 4 -and $description[1] -ge $ledger[1]-.1 -and $description[1]+$description[3] -le $effect[1]+.1 -and $effect[1]+$effect[3] -le $costs[1]+.1) 'description, horizontal current/next effect and shared cost strip retain the inspected visual order'
        Assert-Experience ($costs.Count -eq 4 -and $costs[0] -ge $ledger[0]-.1 -and $costs[1] -ge $ledger[1]-.1 -and $costs[0]+$costs[2] -le $ledger[0]+$ledger[2]+.1 -and $costs[1]+$costs[3] -le $ledger[1]+$ledger[3]+.1) 'five complete construction cost columns are visible before scrolling'
    }
    $unit=$State.mapHudTouchMinimum/44.0
    Assert-Experience ([Math]::Abs($action[2]/$unit-360) -lt .1 -and [Math]::Abs($action[3]/$unit-44) -lt .1) 'fixed facility action matches the actual 360 by 44 design instead of a full-width bar'
    $controls=@($State.targets | Where-Object {$_.id -match '^city-(slot|option)-[0-9]+$'})
    Assert-Experience (@($controls | Where-Object {$_.id -like 'city-slot-*'}).Count -eq 9) 'compact district locator retains all nine real plot controls'
    foreach($control in $controls) {
        Assert-ExperienceTargetInClient $State $control.id
        Assert-Experience ($control.w -ge $State.mapHudTouchMinimum-.1 -and $control.h -ge $State.mapHudTouchMinimum-.1) ($control.id+' retains a 44 logical pixel touch area')
        if($control.id -like 'city-option-*') {
            $suffix=$control.id.Substring('city-option-'.Length)
            foreach($part in @(@('city-facility-image-',4,1,($control.w/$unit-8),58),@('city-facility-title-',0,60,($control.w/$unit),20))) {
                $child=Get-ExperienceTarget $State ($part[0]+$suffix)
                Assert-ExperienceTargetInClient $State $child.id
                Assert-Experience ([Math]::Abs($child.x-$child.w*.5-($control.x-$control.w*.5)-$part[1]*$unit)+[Math]::Abs($child.y-$child.h*.5-($control.y-$control.h*.5)-$part[2]*$unit)+[Math]::Abs($child.w-$part[3]*$unit)+[Math]::Abs($child.h-$part[4]*$unit) -lt .2) ($child.id+' matches the actual visible artwork/title inside its existing parent command')
            }
        }
    }
}
function Assert-ExperienceCardDossier($State) {
    Assert-ExperienceCardPortrait $State
    Assert-ExperienceCardDossierDesign $State
    Assert-Experience (-not [string]::IsNullOrEmpty($State.cardDetail) -and $State.cardSection -eq 0 -and @($State.cardDossierIdentity).Count -eq 4 -and @($State.cardDossierAttributes).Count -eq 4) 'card dossier exposes geometry only for its visible attribute page'
    $viewport=Get-ExperienceTarget $State 'card-scroll'
    foreach($entry in @(@('card-native-skill','cardDossierNative'),@('card-learned-skill','cardDossierLearned'))) {
        Assert-ExperienceTargetInClient $State $entry[0]
        $slot=Get-ExperienceTarget $State $entry[0];$bounds=@($State.($entry[1]))
        Assert-Experience ($bounds.Count -eq 4 -and [Math]::Abs(($slot.x-$slot.w*.5)-$bounds[0]) -lt .05 -and [Math]::Abs(($slot.y-$slot.h*.5)-$bounds[1]) -lt .05 -and [Math]::Abs($slot.w-$bounds[2]) -lt .05 -and [Math]::Abs($slot.h-$bounds[3]) -lt .05) ($entry[0]+' click target matches the actually drawn skill slot')
        Assert-Experience (($slot.x-$slot.w*.5) -ge ($viewport.x-$viewport.w*.5-.05) -and ($slot.x+$slot.w*.5) -le ($viewport.x+$viewport.w*.5+.05) -and ($slot.y-$slot.h*.5) -ge ($viewport.y-$viewport.h*.5-.05) -and ($slot.y+$slot.h*.5) -le ($viewport.y+$viewport.h*.5+.05)) ($entry[0]+' is fully visible inside the scroll viewport')
    }
    Assert-Experience (-not [string]::IsNullOrEmpty($State.cardDossierNativeName) -and -not [string]::IsNullOrEmpty($State.cardDossierLearnedName)) 'dossier retains actual native skill and owned-or-preview learning state'
}
function Assert-ExperienceCardDossierDesign($State) {
    Assert-Experience ($State.cardSection -eq 0 -and $State.cardDossierDesignStyle -eq 'inkscape-dossier-inline-v1' -and $State.cardDossierDesignAssetsReady -and $State.cardDossierDesignTextFits) 'attribute page loads all eight controlled Inkscape components and fits its actual Chinese GUI fonts'
    $unit=$State.mapHudTouchMinimum/44.0
    $viewport=Get-ExperienceTarget $State 'card-scroll'
    $width=$State.cardDossierDesignWidth
    $column=($width-28)/2
    $originX=$viewport.x-$viewport.w/2
    $originY=$viewport.y-$viewport.h/2-$State.cardDetailScrollY*$unit
    Assert-Experience ($unit -gt 0 -and [Math]::Abs($viewport.w-($width+22)*$unit) -lt .1 -and $width -gt 0) 'dossier source width and origin use the actual safe-area scroll viewport'
    $designs=@(@('cardDossierAttributes',8,0,($width-16),312),@('cardDossierNative',8,332,$column,100),@('cardDossierLearned',(20+$column),332,$column,100),@('cardDossierNativeImage',16,346,72,72),@('cardDossierLearnedImage',(28+$column),346,72,72),@('cardDossierNativeNameArea',100,370,($column-100),54),@('cardDossierLearnedNameArea',(112+$column),370,($column-100),54))
    foreach($design in $designs) {
        $drawn=@($State.($design[0]))
        Assert-Experience ($drawn.Count -eq 4 -and [Math]::Abs($drawn[0]-$originX-$design[1]*$unit)+[Math]::Abs($drawn[1]-$originY-$design[2]*$unit)+[Math]::Abs($drawn[2]-$design[3]*$unit)+[Math]::Abs($drawn[3]-$design[4]*$unit) -lt .1) ($design[0]+' matches the inspected design and the actual scroll origin')
        Assert-Experience (Test-ExperienceLandContains $viewport (Convert-ExperienceCityBounds $drawn)) ($design[0]+' remains completely visible in the real content viewport')
    }
    Assert-Experience (@($State.cardDossierAttributeIcons).Count -eq 20) 'all five actually drawn imported attribute symbols expose their real geometry'
    for($index=0;$index -lt 5;$index++) {
        $offset=$index*4;$drawn=@($State.cardDossierAttributeIcons)[$offset..($offset+3)]
        $x=8+($index%2)*($column+12);$y=121+[Math]::Floor($index/2)*54
        Assert-Experience ([Math]::Abs($drawn[0]-$originX-$x*$unit)+[Math]::Abs($drawn[1]-$originY-$y*$unit)+[Math]::Abs($drawn[2]-32*$unit)+[Math]::Abs($drawn[3]-32*$unit) -lt .1) ('attribute symbol '+$index+' occupies the designed 32-square inline cell')
    }
    foreach($entry in @(@('cardDossierNative','cardDossierNativeImage','cardDossierNativeNameArea'),@('cardDossierLearned','cardDossierLearnedImage','cardDossierLearnedNameArea'))) {
        $slot=Convert-ExperienceCityBounds $State.($entry[0]);$image=Convert-ExperienceCityBounds $State.($entry[1]);$name=Convert-ExperienceCityBounds $State.($entry[2])
        Assert-Experience ((Test-ExperienceLandContains $slot $image) -and (Test-ExperienceLandContains $slot $name) -and -not (Test-ExperienceLandOverlap $image $name)) ($entry[0]+' keeps actual image and name inside its clickable surface without overlap')
    }
}
function Assert-ExperienceCardChrome($State) {
    Assert-Experience ($State.cardChromeStyle -eq 'inkscape-card-chrome-v1' -and $State.cardChromeAssetsReady -and $State.cardChromeTextFits -and -not [string]::IsNullOrEmpty($State.cardDetail)) 'card chrome uses the inspected Inkscape surfaces and fits actual Chinese fonts'
    $unit=$State.mapHudTouchMinimum/44.0
    $right=@($State.cardChromeRight);$portrait=@($State.cardPortraitFrame)
    Assert-Experience ($unit -gt 0 -and $right.Count -eq 4 -and $portrait.Count -eq 4 -and $right[2] -gt 0 -and $right[3] -gt 0) 'card chrome reports its actual right-column frame and safe-area scale'
    Assert-Experience ([Math]::Abs($right[0]-$portrait[0]-$portrait[2]-28*$unit)+[Math]::Abs($right[1]-$portrait[1]+14*$unit)+[Math]::Abs($right[3]-$portrait[3]-28*$unit)+[Math]::Abs($right[2]-($portrait[2]/.43-$portrait[2]-72*$unit)) -lt .1) 'compact chrome preserves the surrounding modal and complete portrait allocation'
    $width=$right[2]/$unit;$height=$right[3]/$unit;$tabWidth=($width-66)/3
    $designs=@(@('card-attributes',0,24,$tabWidth,54),@('card-skill',$tabWidth,24,$tabWidth,54),@('card-source',(2*$tabWidth),24,$tabWidth,54),@('card-close',($width-54),24,54,54),@('card-scroll',0,142,$width,($height-226)),@('card-return',($width-180),($height-62),180,54))
    if($State.cardDetailOwnedUid -gt 0){$designs+=,@('owned-card-manage',0,($height-62),([Math]::Min(240,$width-196)),54)}
    else{Assert-Experience (@($State.targets | Where-Object id -eq 'owned-card-manage').Count -eq 0) 'preview chrome exposes no stale owned management target'}
    $safe=[pscustomobject]@{x=($State.mapHudSafeX+$State.mapHudSafeWidth/2);y=($State.mapHudSafeY+$State.mapHudSafeHeight/2);w=$State.mapHudSafeWidth;h=$State.mapHudSafeHeight}
    $regions=@()
    foreach($design in $designs) {
        $actual=Get-ExperienceTarget $State $design[0]
        Assert-Experience ([Math]::Abs($actual.x-$actual.w/2-$right[0]-$design[1]*$unit)+[Math]::Abs($actual.y-$actual.h/2-$right[1]-$design[2]*$unit)+[Math]::Abs($actual.w-$design[3]*$unit)+[Math]::Abs($actual.h-$design[4]*$unit) -lt .1) ($design[0]+' matches the inspected compact card design')
        Assert-ExperienceTargetInClient $State $design[0]
        Assert-Experience (Test-ExperienceLandContains $safe $actual) ($design[0]+' remains completely visible inside the mobile or desktop safe area')
        if($design[0] -ne 'card-scroll'){Assert-Experience ($actual.w -ge 44*$unit-.05 -and $actual.h -ge 44*$unit-.05) ($design[0]+' retains an independent full touch action')}
        $regions+=,$actual
    }
    $identity=@($State.cardDossierIdentity)
    Assert-Experience ($identity.Count -eq 4 -and [Math]::Abs($identity[0]-$right[0])+[Math]::Abs($identity[1]-$right[1]-90*$unit)+[Math]::Abs($identity[2]-$right[2])+[Math]::Abs($identity[3]-40*$unit) -lt .1) 'actual identity band matches the inspected full-width 40-high surface'
    $regions+=,(Convert-ExperienceCityBounds $identity)
    for($i=0;$i -lt $regions.Count;$i++) {
        Assert-Experience (-not (Test-ExperienceLandOverlap $regions[$i] (Convert-ExperienceCityBounds $portrait))) 'compact chrome leaves the complete portrait unobscured'
        for($j=0;$j -lt $i;$j++){Assert-Experience (-not (Test-ExperienceLandOverlap $regions[$i] $regions[$j])) 'navigation, identity, scroll content and footer actions never overlap'}
    }
    $viewport=Get-ExperienceTarget $State 'card-scroll'
    Assert-Experience ($viewport.h/$unit -ge 432-.05 -and [Math]::Abs($viewport.h/$unit-($height-282)-56) -lt .1) 'compact header gives 56 more logical pixels to the fully visible attributes and skill actions'
}
function Assert-ExperienceCardPortrait($State) {
    Assert-ExperienceCardChrome $State
    Assert-Experience ($State.cardPortraitStyle -eq 'inkscape-portrait-column-v2' -and -not [string]::IsNullOrEmpty($State.cardDetail)) 'opened card renders the inspected Inkscape portrait column and separate base'
    Assert-Experience $State.cardPortraitDesignAssetsReady 'all three controlled Inkscape surfaces load at their exported dimensions'
    $frame=@($State.cardPortraitFrame)
    Assert-Experience ($frame.Count -eq 4 -and $frame[2] -gt 0 -and $frame[3] -gt 0) 'card frame reports its actual drawn bounds'
    $regions=@('cardPortraitImage','cardPortraitBanner','cardPortraitCrown','cardPortraitBase')
    for($i=0;$i -lt $regions.Count;$i++) {
        $r=@($State.($regions[$i]))
        Assert-Experience ($r.Count -eq 4 -and $r[2] -gt 0 -and $r[3] -gt 0 -and $r[0] -ge $frame[0]-.05 -and $r[1] -ge $frame[1]-.05 -and ($r[0]+$r[2]) -le ($frame[0]+$frame[2]+.05) -and ($r[1]+$r[3]) -le ($frame[1]+$frame[3]+.05)) ($regions[$i]+' stays in the actual card frame')
        for($j=0;$j -lt $i;$j++) {
            $other=@($State.($regions[$j]))
            $overlap=$r[0] -lt ($other[0]+$other[2]) -and ($r[0]+$r[2]) -gt $other[0] -and $r[1] -lt ($other[1]+$other[3]) -and ($r[1]+$r[3]) -gt $other[1]
            Assert-Experience (-not $overlap) ($regions[$i]+' does not cover '+$regions[$j])
        }
    }
    $image=@($State.cardPortraitImage)
    Assert-Experience ($State.cardPortraitSourceAspect -gt 0 -and [Math]::Abs($image[2]/$image[3]-$State.cardPortraitSourceAspect) -lt .001) 'opened stable-ID portrait retains its complete source aspect'
    Assert-Experience ($State.cardPortraitStars -gt 0 -and $State.cardPortraitRank -le $State.cardPortraitStars) 'rarity and owned rank use the actual card definition'
    $unit=$State.mapHudTouchMinimum/44.0
    Assert-Experience ($unit -gt 0) 'portrait diagnostics use the actual safe-area scale'
    $banner=@($State.cardPortraitBanner);$stars=@($State.cardPortraitCrown);$base=@($State.cardPortraitBase)
    $nameStep=[Math]::Min(38,($frame[3]/$unit-200)/[Math]::Max(1,$State.cardDetailName.Length))
    Assert-Experience ([Math]::Abs($State.cardPortraitNameStep-$nameStep) -lt .01 -and [Math]::Abs($State.cardPortraitNameCellHeight-$nameStep) -lt .01 -and $State.cardPortraitNameFontSize -gt 0 -and $State.cardPortraitNameGlyphWidth -gt 0 -and $State.cardPortraitNameGlyphWidth -le 44 -and $State.cardPortraitNameGlyphHeight -gt 0 -and $State.cardPortraitNameGlyphHeight -le $nameStep -and $State.cardPortraitNameLineHeight -le $nameStep) 'every actual name glyph and font line fits the designed 44-wide cells'
    Assert-Experience ($State.cardPortraitRankFontSize -gt 0 -and $State.cardPortraitRankGlyphWidth -gt 0 -and $State.cardPortraitRankGlyphWidth -le 48 -and $State.cardPortraitRankGlyphHeight -gt 0 -and $State.cardPortraitRankGlyphHeight -le 30 -and $State.cardPortraitRankLineHeight -le 30) 'actual rarity font fits each independent 48-by-30 vertical star cell'
    Assert-Experience ([Math]::Abs($banner[0]-$frame[0]-10*$unit)+[Math]::Abs($banner[1]-$frame[1]-12*$unit)+[Math]::Abs($banner[2]-48*$unit)+[Math]::Abs($banner[3]-(68+$State.cardDetailName.Length*$nameStep)*$unit) -lt .1) 'name pennant matches the inspected source geometry'
    Assert-Experience ([Math]::Abs($stars[0]-$banner[0])+[Math]::Abs($stars[1]-$banner[1]-$banner[3]-8*$unit)+[Math]::Abs($stars[2]-48*$unit)+[Math]::Abs($stars[3]-$State.cardPortraitStars*30*$unit) -lt .1) 'rarity sits directly below the pennant in one complete vertical column'
    Assert-Experience ([Math]::Abs($base[0]-$frame[0]-12*$unit)+[Math]::Abs($base[1]-$frame[1]-$frame[3]+80*$unit)+[Math]::Abs($base[2]-$frame[2]+24*$unit)+[Math]::Abs($base[3]-68*$unit) -lt .1) 'portrait factual base retains its designed lower anchor'
    $areaW=$frame[2]-82*$unit;$areaH=$frame[3]-104*$unit
    $fittedW=[Math]::Min($areaW,$areaH*$State.cardPortraitSourceAspect);$fittedH=$fittedW/$State.cardPortraitSourceAspect
    Assert-Experience ([Math]::Abs($image[2]-$fittedW)+[Math]::Abs($image[3]-$fittedH)+[Math]::Abs($image[0]-$frame[0]-68*$unit-($areaW-$fittedW)/2)+[Math]::Abs($image[1]-$frame[1]-12*$unit) -lt .1) 'complete source fits the enlarged top-aligned design image window'
    $oldW=[Math]::Min($areaW,($frame[3]-137*$unit)*$State.cardPortraitSourceAspect)
    Assert-Experience ($image[2] -ge $oldW-.05) 'complete artwork is at least as large as before the design change'
}
function Assert-ExperienceOwnedRoster($State) {
    Assert-Experience ($State.ownedRosterOpen -and $State.tab -eq '武将' -and -not $State.cardDetail -and $State.ownedRosterCount -eq $State.cardCount) 'owned roster shows its real owned-instance collection without a modal'
    foreach($ownedId in @('owned-roster-scroll','owned-progress-scroll','owned-general-detail','owned-formation-0','owned-formation-1','owned-formation-2','owned-slot-0','owned-slot-1','owned-slot-2','owned-assign','general-training','owned-card-training')) {Assert-ExperienceTargetInClient $State $ownedId}
    $ownedViewport=Get-ExperienceTarget $State 'owned-roster-scroll'
    $ownedCards=@($State.targets | Where-Object {$_.id -match '^owned-card-[0-9]+$'})
    Assert-Experience ($ownedCards.Count -eq @($State.ownedRosterVisibleUids).Count) 'owned roster observes only its actually drawn fully visible instance cards'
    $ownedSeen=@()
    foreach($ownedCard in $ownedCards) {
        $ownedUid=[int]$ownedCard.id.Substring(11)
        Assert-Experience ($ownedUid -in @($State.ownedRosterVisibleUids) -and $ownedCard.w -gt 0 -and $ownedCard.h -gt 0 -and $ownedCard.x-$ownedCard.w*.5 -ge $ownedViewport.x-$ownedViewport.w*.5-.1 -and $ownedCard.y-$ownedCard.h*.5 -ge $ownedViewport.y-$ownedViewport.h*.5-.1 -and $ownedCard.x+$ownedCard.w*.5 -le $ownedViewport.x+$ownedViewport.w*.5+.1 -and $ownedCard.y+$ownedCard.h*.5 -le $ownedViewport.y+$ownedViewport.h*.5+.1) ('owned UID '+$ownedUid+' is a complete visible card inside its true scroll viewport')
        foreach($seen in $ownedSeen) {
            if([Math]::Abs($ownedCard.x-$seen.x) -lt (($ownedCard.w+$seen.w)*.5-.1) -and [Math]::Abs($ownedCard.y-$seen.y) -lt (($ownedCard.h+$seen.h)*.5-.1)) {throw 'EXPERIENCE_ASSERT: Owned roster card targets overlap.'}
        }
        $ownedSeen+=,$ownedCard
    }
    $fixedTraining=Get-ExperienceTarget $State 'general-training';$ownedTraining=Get-ExperienceTarget $State 'owned-card-training'
    Assert-Experience ($fixedTraining.x -eq $ownedTraining.x -and $fixedTraining.y -eq $ownedTraining.y -and $fixedTraining.w -eq $ownedTraining.w -and $fixedTraining.h -eq $ownedTraining.h) 'training aliases identify the same fixed visible action'
}
function Assert-ExperienceOwnedRosterModal($State) {
    $hidden=@($State.targets | Where-Object {$_.id -match '^owned-card-[0-9]+$|^owned-(formation|slot)-[0-2]$|^owned-(card-first|roster-scroll|general-detail|progress-scroll|assign|card-training)$|^general-training$'})
    Assert-Experience (-not $State.ownedRosterOpen -and @($State.ownedRosterVisibleUids).Count -eq 0 -and $hidden.Count -eq 0) 'owned detail modal exposes no background roster card, formation, progression or training targets'
}
function Assert-ExperienceMapHud($State) {
    Assert-Experience ($State.terrainStyle -eq 'youzhou-terrain-patches-v1') 'map shows the resource-patch terrain renderer used for this candidate'
    Assert-Experience ($State.riverStyle -eq 'youzhou-river-banks-v1') 'map uses the actual bank-distance river and raised crossing renderer'
    Assert-Experience ($State.mapHudVisible -and $State.mapHudStyle -eq 'youzhou-reference-image-captions-v2' -and $State.tab -eq '领地' -and -not $State.cardDetail) 'map shows the reference-led image-above-horizontal-caption navigation, full portrait cards and live S1 header'
    Assert-ExperienceMapHeader $State
    Assert-ExperienceMapRecruit $State
    Assert-Experience ($State.mapHudArmyStyle -eq 'youzhou-army-rows-v3' -and $State.mapHudArmyTextFits) 'map army uses the inspected horizontal-row design and fits actual Chinese font metrics'
    $hudIds=@('hud-ruler-city','hud-save','city-open','map-tab','roster-tab','recruit-tab','reports-tab','hud-help','speed-cycle','army-reinforcement','hud-army-header','hud-army-status')
    $hudIds+=@(0..5 | ForEach-Object {'hud-resource-'+$_})
    for($cardIndex=0;$cardIndex -lt @($State.mapHudArmyUids).Count;$cardIndex++) {$hudIds+='hud-army-card-'+$cardIndex}
    $hudIds+=$(if($State.orderActive){'hud-army-recall'}else{'hud-army-formation'})
    if($State.trainingActive){$hudIds+='army-training-progress'}
    if(@($State.targets | Where-Object {$_.id -eq 'army-battle-watch'}).Count -gt 0){$hudIds+='army-battle-watch'}
    $drawnCards=@($State.targets | Where-Object {$_.id -match '^hud-army-card-[0-2]$'})
    Assert-Experience ($drawnCards.Count -eq @($State.mapHudArmyUids).Count) 'HUD draws one card target per actual deployed general and no target for empty slots'
    $hudRects=@()
    foreach($hudId in $hudIds) {
        $rect=Get-ExperienceTarget $State $hudId
        Assert-Experience ($rect.w -gt 0 -and $rect.h -gt 0 -and ($rect.x-$rect.w*.5) -ge ($State.mapHudSafeX-.1) -and ($rect.y-$rect.h*.5) -ge ($State.mapHudSafeY-.1) -and ($rect.x+$rect.w*.5) -le ($State.mapHudSafeX+$State.mapHudSafeWidth+.1) -and ($rect.y+$rect.h*.5) -le ($State.mapHudSafeY+$State.mapHudSafeHeight+.1)) ($hudId+' fits the current rendered safe area')
        foreach($other in $hudRects) {
            $overlap=[Math]::Abs($rect.x-$other.x) -lt (($rect.w+$other.w)*.5-.1) -and [Math]::Abs($rect.y-$other.y) -lt (($rect.h+$other.h)*.5-.1)
            if($overlap){throw ('EXPERIENCE_ASSERT: Map HUD controls overlap: '+$hudId+' / '+$other.id)}
        }
        $hudRects+=,$rect
    }
    Assert-Experience ($hudRects.Count -eq $hudIds.Count) 'all visible HUD resource cells, card targets and actions are mutually separated'
    Assert-ExperienceMapCommand $State
    Assert-ExperienceMapQuest $State
    Assert-ExperienceMapCityLabels $State
}
function Assert-ExperienceMapRecruit($State) {
    Assert-Experience ($State.mapHudVisible -and $State.tab -eq '领地') 'recruitment inspection is on the actual visible map'
    $unit=$State.mapHudTouchMinimum/44.0
    Assert-Experience ($unit -gt 0) 'map recruitment uses a positive actual safe-area scale'
    $width=$State.mapHudSafeWidth/$unit;$height=$State.mapHudSafeHeight/$unit
    $safe=[pscustomobject]@{x=$State.mapHudSafeX+$State.mapHudSafeWidth*.5;y=$State.mapHudSafeY+$State.mapHudSafeHeight*.5;w=$State.mapHudSafeWidth;h=$State.mapHudSafeHeight}
    if($State.selected -lt 0) {
        Assert-Experience ($State.mapRecruitStyle -eq 'youzhou-recruit-pedestal-v3' -and $State.mapRecruitTextFits) 'map-only recruitment uses the inspected raised seal and actual fitted Chinese title'
        $designs=@(@('recruit-tab',($width-180),($height-144),160,140),@('recruit-tab-art',($width-130),($height-134),60,88),@('recruit-tab-caption',($width-170),($height-30),140,24))
    } else {
        Assert-Experience ([string]::IsNullOrEmpty($State.mapRecruitStyle) -and -not $State.mapRecruitTextFits) 'selected-land view suppresses the raised recruitment seal and its diagnostic'
        $designs=@(@('recruit-tab',($width-152),($height-74),132,70),@('recruit-tab-art',($width-106),($height-67),40,38),@('recruit-tab-caption',($width-144),($height-27),116,22))
    }
    foreach($design in $designs) {
        $drawn=Get-ExperienceTarget $State $design[0]
        Assert-Experience ((Test-ExperienceLandContains $safe $drawn) -and [Math]::Abs($drawn.x-$drawn.w*.5-$State.mapHudSafeX-$design[1]*$unit) -lt .1 -and [Math]::Abs($drawn.y-$drawn.h*.5-$State.mapHudSafeY-$design[2]*$unit) -lt .1 -and [Math]::Abs($drawn.w-$design[3]*$unit) -lt .1 -and [Math]::Abs($drawn.h-$design[4]*$unit) -lt .1) ($design[0]+' exactly follows the inspected map recruitment state after safe-area scaling')
    }
    $button=Get-ExperienceTarget $State 'recruit-tab';$art=Get-ExperienceTarget $State 'recruit-tab-art';$caption=Get-ExperienceTarget $State 'recruit-tab-caption'
    Assert-Experience ((Test-ExperienceLandContains $button $art) -and (Test-ExperienceLandContains $button $caption) -and -not (Test-ExperienceLandOverlap $art $caption) -and ($art.y+$art.h*.5) -lt ($caption.y-$caption.h*.5)) 'complete recruitment portrait remains separate from its readable horizontal title'
    foreach($id in @('city-open','map-tab','roster-tab','reports-tab','hud-help','hud-save','speed-cycle')) {
        Assert-Experience (-not (Test-ExperienceLandOverlap $button (Get-ExperienceTarget $State $id))) ('recruitment leaves the existing '+$id+' action unobstructed')
    }
}
function Get-ExperienceMapRecruitRaisedPoint($State) {
    Assert-Experience ($State.selected -lt 0 -and $State.mapRecruitStyle -eq 'youzhou-recruit-pedestal-v3') 'raised recruitment click requires the actually visible seal'
    $art=Get-ExperienceTarget $State 'recruit-tab-art';$unit=$State.mapHudTouchMinimum/44.0
    $point=[pscustomobject]@{x=$art.x;y=$art.y-18*$unit}
    $footerTop=$State.mapHudSafeY+$State.mapHudSafeHeight-78*$unit
    Assert-Experience ($point.y -gt ($art.y-$art.h*.5) -and $point.y -lt $footerTop -and $point.y -lt ($art.y+$art.h*.5)) 'upper portrait click is inside real drawn artwork above the old footer, without an invisible target'
    return $point
}
function Assert-ExperienceMapRecruitDrag($Before,$After) {
    Assert-Experience ($After.mapHudVisible -and $After.tab -eq '领地' -and $After.mouseBlocked -and [string]::IsNullOrEmpty($After.cardDetail) -and $After.selected -eq $Before.selected -and $After.battleWorldHash -eq $Before.battleWorldHash) 'drag ending inside raised recruitment cancels navigation and terrain selection without campaign effects'
    Assert-ExperienceMapStill $Before $After 'raised recruitment drag'
    Assert-Experience ([Math]::Abs($After.zoom-$Before.zoom) -lt .001) 'raised recruitment drag preserves camera zoom'
}
function Get-ExperienceHeaderCrestPoint($State) {
    $crest=Get-ExperienceTarget $State 'hud-ruler-crest'
    # Click the visible upper rim, separately from the complete city's center.
    [pscustomobject]@{x=$crest.x;y=($crest.y-$crest.h*.42)}
}
function Assert-ExperienceMapHeader($State) {
    Assert-Experience ($State.mapHudHeaderStyle -eq 'youzhou-reference-ledger-v2' -and $State.mapHudHeaderTextFits) 'map header uses the inspected contiguous ledger and fits actual Chinese font metrics'
    $unit=$State.mapHudTouchMinimum/44
    Assert-Experience ($unit -gt 0) 'map header has a valid safe-area scale'
    $logicalWidth=$State.mapHudSafeWidth/$unit
    $pitch=[Math]::Min(168,($logicalWidth-604)/4)
    $name=Get-ExperienceTarget $State 'hud-ruler-name';$wrapped=$name.h/$unit -gt 31
    $designs=@(@('hud-ruler-city',16,4,246,62),@('hud-ruler-crest',16,4,62,62),@('hud-ruler-art',22,13,50,44),@('hud-ruler-detail',84,44,174,20),@('hud-ruler-name',84,$(if($wrapped){5}else{7}),174,$(if($wrapped){38}else{30})))
    foreach($i in 0..5) {
        $x=if($i -lt 4){280+$i*$pitch}else{280+4*$pitch+16+($i-4)*150}
        $cw=if($i -lt 4){$pitch-6}else{144};$textX=$x+$(if($i -lt 4){44}else{40});$textW=if($i -lt 4){$cw-48}else{100}
        $designs+=@(@(('hud-resource-'+$i),$x,5,$cw,58),@(('hud-resource-icon-'+$i),($x+$(if($i -lt 4){6}else{7})),19,$(if($i -lt 4){32}else{26}),$(if($i -lt 4){26}else{28})),@(('hud-resource-stock-'+$i),$textX,30,$textW,26),@(('hud-resource-detail-'+$i),$textX,6,$textW,23))
    }
    foreach($d in $designs) {
        $r=Get-ExperienceTarget $State $d[0]
        Assert-Experience ([Math]::Abs($r.x-$r.w*.5-$State.mapHudSafeX-$d[1]*$unit) -lt .1 -and [Math]::Abs($r.y-$r.h*.5-$State.mapHudSafeY-$d[2]*$unit) -lt .1 -and [Math]::Abs($r.w-$d[3]*$unit) -lt .1 -and [Math]::Abs($r.h-$d[4]*$unit) -lt .1) ($d[0]+' follows the inspected identity and aligned inventory design')
    }
}
function Assert-ExperienceMapQuest($State) {
    Assert-Experience ($State.mapQuestStyle -eq 'youzhou-quest-sleeve-v4' -and $State.mapQuestArtKind -eq 'task-book' -and $State.mapQuestTextFits -and $State.mapQuestId -eq $State.questCurrentId) 'book task design shows the actual current task and fits actual Chinese font metrics'
    $panel=Get-ExperienceTarget $State 'hud-quest-panel';$open=Get-ExperienceTarget $State 'quest-open'
    $art=Get-ExperienceTarget $State 'hud-quest-art';$title=Get-ExperienceTarget $State 'hud-quest-title'
    $objective=Get-ExperienceTarget $State 'hud-quest-objective';$progress=Get-ExperienceTarget $State 'hud-quest-progress'
    $sleeve=Get-ExperienceTarget $State 'hud-quest-body';$chapter=Get-ExperienceTarget $State 'hud-quest-chapter'
    Assert-Experience ((Test-ExperienceLandContains $open $sleeve) -and -not (Test-ExperienceLandOverlap $art $sleeve) -and (Test-ExperienceLandContains $sleeve $chapter) -and (Test-ExperienceLandContains $sleeve $title) -and (Test-ExperienceLandContains $sleeve $objective) -and -not (Test-ExperienceLandOverlap $chapter $title)) 'detached book leaves the task information sleeve and three live text rows unobscured'
    $actionId=$(if($State.mapQuestId -eq 0){'hud-quest-history'}elseif($State.mapQuestReady){'quest-claim'}else{'quest-go'})
    $action=Get-ExperienceTarget $State $actionId
    $safe=[pscustomobject]@{x=($State.mapHudSafeX+$State.mapHudSafeWidth*.5);y=($State.mapHudSafeY+$State.mapHudSafeHeight*.5);w=$State.mapHudSafeWidth;h=$State.mapHudSafeHeight}
    Assert-Experience ((Test-ExperienceLandContains $safe $panel) -and (Test-ExperienceLandContains $panel $art) -and (Test-ExperienceLandContains $panel $open) -and (Test-ExperienceLandContains $panel $progress) -and (Test-ExperienceLandContains $panel $action)) 'compact task panel fits safe area with independent banner, chapter entry, progress and action'
    foreach($region in @($title,$objective)) {Assert-Experience (Test-ExperienceLandContains $open $region) 'drawn task title and objective stay inside their real chapter entry'}
    Assert-Experience ((Test-ExperienceLandContains $open $art) -and -not (Test-ExperienceLandOverlap $art $progress) -and -not (Test-ExperienceLandOverlap $art $action) -and -not (Test-ExperienceLandOverlap $art $title) -and -not (Test-ExperienceLandOverlap $art $objective) -and -not (Test-ExperienceLandOverlap $title $objective) -and -not (Test-ExperienceLandOverlap $open $action) -and -not (Test-ExperienceLandOverlap $open $progress) -and -not (Test-ExperienceLandOverlap $progress $action)) 'book belongs to its chapter control; book, objective, progress and action do not obscure each other'
    foreach($touch in @($art,$open,$action)) {
        Assert-Experience ($State.mapHudTouchMinimum -gt 0 -and $touch.w -ge $State.mapHudTouchMinimum-.1 -and $touch.h -ge $State.mapHudTouchMinimum-.1) ($touch.id+' keeps a separate touch area of at least 44 logical pixels in both dimensions')
    }
    # Check the inspected SVG dimensions independently of the runtime layout helper.
    $questScale=$State.mapHudTouchMinimum/44
    $questDesign=@(@('hud-quest-panel',20,90,354,118),@('hud-quest-art',20,90,80,68),@('quest-open',20,90,354,68),@('hud-quest-title',120,108,244,24),@('hud-quest-objective',120,132,244,26),@('hud-quest-progress',20,164,212,44),@($actionId,242,164,132,44))
    $questDesign+=@(@('hud-quest-body',110,90,264,68),@('hud-quest-chapter',120,90,244,18))
    foreach($design in $questDesign) {
        $drawn=Get-ExperienceTarget $State $design[0]
        Assert-Experience ([Math]::Abs($drawn.x-$drawn.w*.5-$State.mapHudSafeX-$design[1]*$questScale) -lt .1 -and [Math]::Abs($drawn.y-$drawn.h*.5-$State.mapHudSafeY-$design[2]*$questScale) -lt .1 -and [Math]::Abs($drawn.w-$design[3]*$questScale) -lt .1 -and [Math]::Abs($drawn.h-$design[4]*$questScale) -lt .1) ($design[0]+' matches the compact exported design rather than the old 174-unit charter')
    }
    Assert-Experience (-not (Test-ExperienceLandOverlap $panel (Get-ExperienceTarget $State 'hud-army-panel'))) 'compact task panel remains separate from the map army'
    foreach($landReturn in @($State.targets | Where-Object {$_.id -eq 'quest-land-return'})) {
        Assert-Experience (-not (Test-ExperienceLandOverlap $panel $landReturn)) 'compact task panel leaves the real land-objective return unobscured'
    }
    Assert-Experience ($State.mapQuestTarget -gt 0 -and $State.mapQuestProgress -ge 0 -and $State.mapQuestProgress -le $State.mapQuestTarget) 'task progress stays within the actual objective'
    $actions=@($State.targets | Where-Object {$_.id -in @('quest-go','quest-claim','hud-quest-history')})
    Assert-Experience ($actions.Count -eq 1 -and $actions[0].id -eq $actionId) 'only the current task action is visible'
}
function Assert-ExperienceMapCommand($State) {
    foreach($entry in @('city-open','map-tab','roster-tab','recruit-tab','reports-tab','hud-help')) {
        $button=Get-ExperienceTarget $State $entry
        $art=Get-ExperienceTarget $State ($entry+'-art');$caption=Get-ExperienceTarget $State ($entry+'-caption')
        $placement=($art.y+$art.h*.5) -le ($caption.y-$caption.h*.5+.1)
        Assert-Experience ((Test-ExperienceLandContains $button $art) -and (Test-ExperienceLandContains $button $caption) -and -not (Test-ExperienceLandOverlap $art $caption) -and $placement) ($entry+' follows the exported image above its horizontal caption')
        Assert-Experience ($State.mapHudTouchMinimum -gt 0 -and $button.h -ge ($State.mapHudTouchMinimum-.1)) ($entry+' keeps its full touch target')
    }
    $panel=Get-ExperienceTarget $State 'hud-army-panel'
    $header=Get-ExperienceTarget $State 'hud-army-header';$status=Get-ExperienceTarget $State 'hud-army-status'
    $reinforce=Get-ExperienceTarget $State 'army-reinforcement'
    $actionId=$(if($State.orderActive){'hud-army-recall'}else{'hud-army-formation'})
    $action=Get-ExperienceTarget $State $actionId
    foreach($readOnly in @($header,$status)) {
        Assert-Experience (Test-ExperienceLandContains $panel $readOnly) 'army header and status are drawn inside the actual blocked region'
    }
    foreach($foot in @($reinforce,$action)) {
        Assert-Experience ((Test-ExperienceLandContains $panel $foot) -and $foot.w -ge ($State.mapHudTouchMinimum-.1) -and $foot.h -ge ($State.mapHudTouchMinimum-.1)) 'army foot command is visible inside its panel with 44-unit touch width and height'
    }
    # Independently check the inspected SVG contract, including its open gaps.
    $armyScale=$State.mapHudTouchMinimum/44
    Assert-Experience ($armyScale -gt 0) 'army inspection has a valid safe-area scale'
    $armyY=$State.mapHudSafeHeight/$armyScale-396
    $armyDesign=@(@('hud-army-panel',20,$armyY,354,302),@('hud-army-header',20,$armyY,354,28),@('hud-army-status',20,($armyY+236),354,20),@('army-reinforcement',20,($armyY+258),171,44),@($actionId,203,($armyY+258),171,44))
    for($index=0;$index -lt @($State.mapHudArmyUids).Count;$index++) {
        $card=Get-ExperienceTarget $State ('hud-army-card-'+$index)
        $art=Get-ExperienceTarget $State ('hud-army-art-'+$index)
        $name=Get-ExperienceTarget $State ('hud-army-name-'+$index)
        $identity=Get-ExperienceTarget $State ('hud-army-identity-'+$index)
        Assert-Experience ((Test-ExperienceLandContains $panel $card) -and (Test-ExperienceLandContains $card $art) -and (Test-ExperienceLandContains $card $name) -and (Test-ExperienceLandContains $card $identity)) 'full portrait, name strip and slot/level base remain inside the actual general card'
        Assert-Experience (-not (Test-ExperienceLandOverlap $art $name) -and -not (Test-ExperienceLandOverlap $art $identity) -and -not (Test-ExperienceLandOverlap $name $identity) -and ($art.x+$art.w*.5) -le ($name.x-$name.w*.5+.1) -and ($name.x+$name.w*.5) -le ($identity.x-$identity.w*.5+.1)) 'complete army portrait, horizontal name and identity follow separate left-to-right columns'
        Assert-Experience ($card.w -ge ($State.mapHudTouchMinimum-.1) -and $card.h/$armyScale -ge 63.99) 'complete horizontal army row retains its 64-unit touch height'
        Assert-Experience (($reinforce.y-$reinforce.h*.5) -ge ($card.y+$card.h*.5-.1) -and ($action.y-$action.h*.5) -ge ($card.y+$card.h*.5-.1)) 'both army commands stay below the portrait row'
        $rowY=$armyY+32+$index*68
        $armyDesign+=@(@(('hud-army-card-'+$index),20,$rowY,354,64),@(('hud-army-art-'+$index),24,($rowY+1),44,62),@(('hud-army-name-'+$index),82,($rowY+12),164,40),@(('hud-army-identity-'+$index),262,($rowY+12),102,40))
        if($index -gt 0) {
            $previous=Get-ExperienceTarget $State ('hud-army-card-'+($index-1))
            Assert-Experience ([Math]::Abs((($card.y-$card.h*.5)-($previous.y+$previous.h*.5))/$armyScale-4) -lt .01) 'army rows preserve the actual four-unit gap'
        }
    }
    foreach($dynamicId in @('army-training-progress','army-battle-watch')) {
        $dynamic=@($State.targets | Where-Object {$_.id -eq $dynamicId})
        if($dynamic.Count -eq 0){continue}
        Assert-Experience ($dynamic.Count -eq 1) 'dynamic army entry is observed once'
        $entry=$dynamic[0];$quest=Get-ExperienceTarget $State 'hud-quest-panel'
        $safe=[pscustomobject]@{x=($State.mapHudSafeX+$State.mapHudSafeWidth*.5);y=($State.mapHudSafeY+$State.mapHudSafeHeight*.5);w=$State.mapHudSafeWidth;h=$State.mapHudSafeHeight}
        Assert-Experience (Test-ExperienceLandContains $safe $entry) 'dynamic army entry remains fully inside the current safe area'
        $offset=if($dynamicId -eq 'army-training-progress' -or -not $State.trainingActive){51}else{102}
        $armyDesign+=,@($dynamicId,20,($armyY-$offset),354,44)
        Assert-Experience (-not (Test-ExperienceLandOverlap $entry $quest) -and -not (Test-ExperienceLandOverlap $entry $panel) -and $entry.w -ge ($State.mapHudTouchMinimum-.1) -and $entry.h -ge ($State.mapHudTouchMinimum-.1)) 'training and battle watch keep full touch size outside task and army regions'
        foreach($return in @($State.targets | Where-Object {$_.id -eq 'quest-land-return'})) {
            Assert-Experience (-not (Test-ExperienceLandOverlap $entry $return)) 'dynamic army entry keeps the actual task objective return accessible'
        }
    }
    foreach($design in $armyDesign) {
        $drawn=Get-ExperienceTarget $State $design[0]
        Assert-Experience ([Math]::Abs($drawn.x-$drawn.w*.5-$State.mapHudSafeX-$design[1]*$armyScale) -lt .1 -and [Math]::Abs($drawn.y-$drawn.h*.5-$State.mapHudSafeY-$design[2]*$armyScale) -lt .1 -and [Math]::Abs($drawn.w-$design[3]*$armyScale) -lt .1 -and [Math]::Abs($drawn.h-$design[4]*$armyScale) -lt .1) ($design[0]+' matches the inspected horizontal army row design')
    }
    $regions=@($State.targets | Where-Object {$_.id -match '^hud-army-(art|name|identity)-[0-2]$'})
    Assert-Experience ($regions.Count -eq 3*@($State.mapHudArmyUids).Count) 'empty army slots expose no fabricated portrait or name region'
}
function Get-ExperienceMapArmyGap($State) {
    $first=Get-ExperienceTarget $State 'hud-army-card-0';$second=Get-ExperienceTarget $State 'hud-army-card-1'
    $unit=$State.mapHudTouchMinimum/44
    $top=$first.y+$first.h*.5;$bottom=$second.y-$second.h*.5
    Assert-Experience ($unit -gt 0 -and [Math]::Abs(($bottom-$top)/$unit-4) -lt .01 -and [Math]::Abs($first.x-$second.x) -lt .1) 'gap click uses the actual four-unit opening between aligned visible rows'
    return [pscustomobject]@{x=($first.x+$second.x)*.5;y=($top+$bottom)*.5}
}
function Assert-ExperienceMapArmyGap($Before,$After) {
    Assert-Experience ($After.mapHudVisible -and $After.mouseBlocked -and [string]::IsNullOrEmpty($After.cardDetail) -and $After.selected -eq $Before.selected -and $After.battleWorldHash -eq $Before.battleWorldHash) 'visible row gap blocks terrain without opening a general, changing selection or spending'
    Assert-ExperienceMapStill $Before $After 'army row gap click'
    Assert-Experience ([Math]::Abs($After.zoom-$Before.zoom) -lt .001) 'army row gap preserves camera zoom'
}
function Assert-ExperienceMapCommandModal($State) {
    $regions=@($State.targets | Where-Object {$_.id -match '^hud-army-(card|art|name|identity)-[0-2]$|^hud-army-(panel|header|status)$|^hud-quest-|^quest-(open|go|claim)$|^(city-open|map-tab|roster-tab|recruit-tab|reports-tab|hud-help)-(art|caption)$'})
    Assert-Experience (-not $State.mapHudVisible -and $regions.Count -eq 0) 'card modal exposes no covered map portrait or illustrated navigation regions'
    Assert-Experience ([string]::IsNullOrEmpty($State.mapRecruitStyle) -and -not $State.mapRecruitTextFits) 'covered map exposes no stale raised recruitment style or font diagnostic'
    Assert-Experience ([string]::IsNullOrEmpty($State.minimapDesignStyle) -and @($State.targets | Where-Object {$_.id -match '^minimap-|^map-home($|-)' }).Count -eq 0) 'card modal exposes no covered atlas, location image or caption'
    Assert-Experience (@($State.targets | Where-Object {$_.id -match '^(city-label-(art-|name-|detail-)?|capital-label-)[0-9]+$'}).Count -eq 0) 'card modal exposes no covered city label, city illustration, complete name or ownership line'
}
function Assert-ExperienceMapCityLabels($State) {
    Assert-Experience ($State.tab -eq '领地' -and -not $State.modal -and -not $State.confirmationOpen -and [string]::IsNullOrEmpty($State.cardDetail)) 'city label inspection uses the actual unobstructed map'
    Assert-Experience ($State.mapCityLabelStyle -eq 'youzhou-city-marker-v1' -and $State.mapCityLabelDesignReady -and $State.mapCityLabelTextFits) 'city labels use the actual exported design textures and fit complete city names and ownership with measured Chinese fonts'
    $unit=$State.mapHudTouchMinimum/44.0
    Assert-Experience ($unit -gt 0) 'city labels use the actual positive safe-area scale'
    $safe=[pscustomobject]@{x=($State.mapHudSafeX+$State.mapHudSafeWidth*.5);y=($State.mapHudSafeY+$State.mapHudSafeHeight*.5);w=$State.mapHudSafeWidth;h=$State.mapHudSafeHeight}
    $labels=@($State.targets | Where-Object {$_.id -match '^city-label-[0-9]+$'})
    $parts=@($State.targets | Where-Object {$_.id -match '^city-label-(art|name|detail)-[0-9]+$'})
    Assert-Experience ($parts.Count -eq 3*$labels.Count) 'each visible city label owns exactly one actual image, complete name and ownership region; culled labels expose no orphan parts'
    $seen=@()
    foreach($label in $labels) {
        $id=$label.id -replace '^city-label-',''
        Assert-ExperienceTargetInClient $State $label.id
        Assert-Experience ((Test-ExperienceLandContains $safe $label) -and $label.w -ge (184*$unit-.1) -and $label.w -le (304*$unit+.1) -and ($label.h -ge ($State.mapHudTouchMinimum-.1)) -and ([Math]::Abs($label.h-52*$unit) -lt .1 -or [Math]::Abs($label.h-72*$unit) -lt .1)) 'actual city label retains the inspected adaptive width and a complete touch target with short or wrapped name height'
        $art=Get-ExperienceTarget $State ('city-label-art-'+$id);$name=Get-ExperienceTarget $State ('city-label-name-'+$id);$detail=Get-ExperienceTarget $State ('city-label-detail-'+$id)
        foreach($part in @($art,$name,$detail)) {
            Assert-ExperienceTargetInClient $State $part.id
            Assert-Experience (Test-ExperienceLandContains $label $part) ($part.id+' is actually drawn inside the original city command')
        }
        Assert-Experience (-not (Test-ExperienceLandOverlap $art $name) -and -not (Test-ExperienceLandOverlap $art $detail) -and -not (Test-ExperienceLandOverlap $name $detail) -and ($art.x+$art.w*.5) -lt ($name.x-$name.w*.5)) 'city illustration, complete name and ownership occupy separate visible layers'
        $left=$label.x-$label.w*.5;$top=$label.y-$label.h*.5
        $nameHeight=if([Math]::Abs($label.h-72*$unit) -lt .1){44}else{24}
        $expected=@(@($name,60,2,($label.w/$unit-70),$nameHeight),@($detail,60,($label.h/$unit-24),($label.w/$unit-70),22))
        foreach($design in $expected) {
            $actual=$design[0]
            Assert-Experience ([Math]::Abs($actual.x-$actual.w*.5-$left-$design[1]*$unit) -lt .1 -and [Math]::Abs($actual.y-$actual.h*.5-$top-$design[2]*$unit) -lt .1 -and [Math]::Abs($actual.w-$design[3]*$unit) -lt .1 -and [Math]::Abs($actual.h-$design[4]*$unit) -lt .1) ($actual.id+' matches the inspected name and ownership layout after safe-area scaling')
        }
        Assert-Experience ($art.w -gt 0 -and $art.h -gt 0 -and $art.w -le (40*$unit+.1) -and $art.h -le (28*$unit+.1) -and ([Math]::Abs($art.w-40*$unit) -lt .1 -or [Math]::Abs($art.h-28*$unit) -lt .1) -and [Math]::Abs($art.x-$left-26*$unit) -lt .1 -and [Math]::Abs($art.y-$label.y) -lt .1) 'city label records the actual complete ScaleToFit image centered inside its inspected 40 by 28 container'
        foreach($other in $seen){Assert-Experience (-not (Test-ExperienceLandOverlap $label $other)) 'actually visible city labels never overlap each other'}
        $seen+=,$label
    }
    foreach($capital in @($State.targets | Where-Object {$_.id -match '^capital-label-[0-9]+$'})) {
        $owner=Get-ExperienceTarget $State ($capital.id -replace '^capital-label-','city-label-')
        Assert-Experience ($capital.x -eq $owner.x -and $capital.y -eq $owner.y -and $capital.w -eq $owner.w -and $capital.h -eq $owner.h) 'capital alias identifies the same actual visible city command'
    }
}
function Check-ExperienceMapCityLabelInputs($State) {
    Assert-Experience ($State.speed -eq 0) 'city label navigation starts paused before checking complete campaign state'
    if($State.selected -ge 0){Click-ExperienceTarget $State 'land-close';$State=Capture-Experience '02n-city-label-start-clear'}
    Click-ExperienceTarget $State 'map-home';$State=Capture-Experience '02n-city-label-home-view'
    Assert-ExperienceMapCityLabels $State
    $origin=$State
    $capitals=@($State.targets | Where-Object {$_.id -match '^capital-label-[0-9]+$'} | ForEach-Object {$_.id -replace '^capital-label-',''})
    $homes=@($State.targets | Where-Object {$_.id -match '^city-label-[0-9]+$' -and ($_.id -replace '^city-label-','') -notin $capitals})
    Assert-Experience ($homes.Count -eq 1) 'centered home exposes its one actually visible city command without assuming a fixed land ID'
    $homeId=$homes[0].id -replace '^city-label-',''
    foreach($part in @('art','name','detail')) {
        Assert-ExperienceMapCityLabels $State
        Click-ExperienceTarget $State ('city-label-'+$part+'-'+$homeId)
        $opened=Capture-Experience ('02n-home-label-'+$part)
        Assert-Experience ($opened.cityOpen -and $opened.cityPage -eq 'overview' -and $opened.cityPlot -eq 4 -and $opened.selected -eq [int]$homeId -and $opened.battleWorldHash -eq $origin.battleWorldHash) 'drawn home illustration, complete name and ownership enter the actual central-city panorama without campaign effects'
        Click-ExperienceTarget $opened 'city-back';$State=Capture-Experience ('02n-home-label-'+$part+'-return')
        Assert-Experience ($State.tab -eq '领地' -and -not $State.cityOpen -and $State.selected -eq [int]$homeId -and $State.battleWorldHash -eq $origin.battleWorldHash) 'home label return retains the actual home selection and complete campaign'
        Assert-ExperienceMapStill $origin $State 'home label city return'
        Click-ExperienceTarget $State 'land-close';$State=Capture-Experience ('02n-home-label-'+$part+'-clear')
    }
    # Pick from actual visible capitals. If this view culls all of them, navigate
    # through the visible atlas and inspect again; never assume city 731 is drawn.
    $candidates=@($State.targets | Where-Object {$_.id -match '^capital-label-[0-9]+$'})
    foreach($point in @(@(.35,.40),@(.55,.50),@(.72,.62))) {
        if($candidates.Count -gt 0){break}
        $atlas=Get-ExperienceTarget $State 'minimap-area'
        Click-Experience ($atlas.x+($point[0]-.5)*$atlas.w) ($atlas.y+($point[1]-.5)*$atlas.h)
        $State=Capture-Experience ('02n-city-label-search-'+[int]($point[0]*100))
        Assert-ExperienceMapCityLabels $State
        Assert-Experience ($State.battleWorldHash -eq $origin.battleWorldHash -and $State.selected -lt 0) 'atlas search for a visible city preserves the complete campaign and clear selection'
        $candidates=@($State.targets | Where-Object {$_.id -match '^capital-label-[0-9]+$'})
    }
    Assert-Experience ($candidates.Count -gt 0) 'map navigation discovers an actually visible capital label for image and complete-name interaction'
    $capitalId=$candidates[0].id -replace '^capital-label-','';$capitalOrigin=$State
    foreach($part in @('art','name')) {
        Assert-ExperienceMapCityLabels $State
        Click-ExperienceTarget $State ('city-label-'+$part+'-'+$capitalId)
        $selectedCity=Capture-Experience ('02n-capital-label-'+$part)
        Assert-Experience ($selectedCity.tab -eq '领地' -and -not $selectedCity.cityOpen -and -not $selectedCity.modal -and -not $selectedCity.confirmationOpen -and $selectedCity.selected -eq [int]$capitalId -and $selectedCity.landPanelVisible -and $selectedCity.landPanelLandId -eq [int]$capitalId -and -not $selectedCity.selectedOwned -and $selectedCity.battleWorldHash -eq $origin.battleWorldHash) 'drawn neutral capital image and name select that exact city without opening home, issuing orders or altering the campaign'
        Assert-ExperienceMapStill $capitalOrigin $selectedCity 'capital label selection'
        Click-ExperienceTarget $selectedCity 'land-close';$State=Capture-Experience ('02n-capital-label-'+$part+'-clear')
        Assert-Experience ($State.selected -lt 0 -and $State.battleWorldHash -eq $origin.battleWorldHash) 'capital label close restores the map without campaign effects'
    }
    Click-ExperienceTarget $State 'map-home';$State=Capture-Experience '02n-city-label-home-restored'
    Assert-ExperienceMapStill $origin $State 'city label inspection home return'
    Assert-Experience ($State.selected -eq $origin.selected -and $State.battleWorldHash -eq $origin.battleWorldHash) 'city label inspection restores the initial selection and complete paused campaign'
    return $State
}
function Test-ExperienceLandContains($Outer,$Inner) {
    return ($Inner.w -gt 0 -and $Inner.h -gt 0 -and $Inner.x-$Inner.w*.5 -ge $Outer.x-$Outer.w*.5-.1 -and $Inner.y-$Inner.h*.5 -ge $Outer.y-$Outer.h*.5-.1 -and $Inner.x+$Inner.w*.5 -le $Outer.x+$Outer.w*.5+.1 -and $Inner.y+$Inner.h*.5 -le $Outer.y+$Outer.h*.5+.1)
}
function Test-ExperienceLandOverlap($First,$Second) {
    return ([Math]::Abs($First.x-$Second.x) -lt (($First.w+$Second.w)*.5-.1) -and [Math]::Abs($First.y-$Second.y) -lt (($First.h+$Second.h)*.5-.1))
}
function Assert-ExperienceLand($State) {
    Assert-Experience ($State.landPanelVisible -and $State.landPanelStyle -eq 'land-compact-facts-v5' -and $State.landPanelLandId -eq $State.selected -and -not [string]::IsNullOrEmpty($State.landPanelKind) -and $State.tab -eq '领地' -and -not $State.confirmationOpen -and [string]::IsNullOrEmpty($State.cardDetail)) 'selected S1 land shows its visible compact facts and separate command panel'
    Assert-Experience $State.landCommandTextFits 'every land fact label, value and command fits its actual GUI font rectangle at a readable minimum size'
    $landActions=@('land-close')
    switch($State.landPanelState) {
        'city' {$landActions+='land-enter-city'}
        'owned' {$landActions+=@('army-garrison','army-farm','land-training')}
        'busy' {$landActions+=@('army-recall','land-training')}
        'neutral' {$landActions+='army-attack'}
        'blocked' {$landActions+='army-attack'}
        default {throw ('EXPERIENCE_ASSERT: Unknown visible land panel state '+$State.landPanelState)}
    }
    $safe=[pscustomobject]@{x=($State.mapHudSafeX+$State.mapHudSafeWidth*.5);y=($State.mapHudSafeY+$State.mapHudSafeHeight*.5);w=$State.mapHudSafeWidth;h=$State.mapHudSafeHeight}
    $landDetails=@('land-art-image','land-art-identity','land-fact-defense','land-fact-yield')
    $landIcons=@($landActions | Where-Object {$_ -ne 'land-close'} | ForEach-Object {$_+'-icon'})
    $landLabels=@($landActions | Where-Object {$_ -ne 'land-close'} | ForEach-Object {$_+'-label'})
    foreach($landId in (@('land-panel','land-command-surface','land-order-information','land-art','land-summary','land-actions','map-close')+$landDetails+$landActions+$landIcons+$landLabels)) {
        $landRect=Get-ExperienceTarget $State $landId
        Assert-Experience (Test-ExperienceLandContains $safe $landRect) ($landId+' fits the actual rendered safe area')
    }
    $panel=Get-ExperienceTarget $State 'land-panel';$art=Get-ExperienceTarget $State 'land-art';$summary=Get-ExperienceTarget $State 'land-summary';$commands=Get-ExperienceTarget $State 'land-actions'
    $commandSurface=Get-ExperienceTarget $State 'land-command-surface'
    Assert-Experience ((Test-ExperienceLandContains $panel $art) -and (Test-ExperienceLandContains $panel $summary) -and (Test-ExperienceLandContains $commandSurface $commands) -and -not (Test-ExperienceLandOverlap $panel $commandSurface) -and -not (Test-ExperienceLandOverlap $art $summary) -and -not (Test-ExperienceLandOverlap $art $commands) -and -not (Test-ExperienceLandOverlap $summary $commands)) 'land facts and commands occupy separate visible surfaces'
    $landScale=$State.landTouchMinimum/44
    $menuHeight=switch($State.landPanelState){'owned'{292};'busy'{216};'city'{144};default{268}}
    $infoHeight=switch($State.landPanelState){'owned'{116};'busy'{96};'city'{76};default{200}}
    $information=Get-ExperienceTarget $State 'land-order-information'
    Assert-Experience ((Test-ExperienceLandContains $commandSurface $information) -and [Math]::Abs($information.h-$infoHeight*$landScale) -lt .1) 'the actual information background follows its state-specific content'
    Assert-Experience ([Math]::Abs($panel.w-280*$landScale) -lt .1 -and [Math]::Abs($panel.h-228*$landScale) -lt .1 -and [Math]::Abs($commandSurface.w-240*$landScale) -lt .1 -and [Math]::Abs($commandSurface.h-$menuHeight*$landScale) -lt .1 -and [Math]::Abs($art.w-256*$landScale) -lt .1 -and [Math]::Abs($art.h-84*$landScale) -lt .1 -and [Math]::Abs($summary.w-248*$landScale) -lt .1 -and [Math]::Abs($summary.h-60*$landScale) -lt .1 -and [Math]::Abs($commands.w-208*$landScale) -lt .1 -and [Math]::Abs($commands.h-($menuHeight-24)*$landScale) -lt .1) 'land selection retains inspected compact facts and state-specific command composition'
    foreach($factDesign in @(@('land-art',12,64,256,84),@('land-art-image',12,68,156,78),@('land-art-identity',178,72,90,70),@('land-summary',16,156,248,60),@('land-fact-defense',16,156,248,28),@('land-fact-yield',16,188,248,28),@('land-close',224,12,44,44))) {
        $factDrawn=Get-ExperienceTarget $State $factDesign[0]
        Assert-Experience ([Math]::Abs(($factDrawn.x-$factDrawn.w*.5)-($panel.x-$panel.w*.5)-$factDesign[1]*$landScale) -lt .1 -and [Math]::Abs(($factDrawn.y-$factDrawn.h*.5)-($panel.y-$panel.h*.5)-$factDesign[2]*$landScale) -lt .1 -and [Math]::Abs($factDrawn.w-$factDesign[3]*$landScale) -lt .1 -and [Math]::Abs($factDrawn.h-$factDesign[4]*$landScale) -lt .1) ($factDesign[0]+' follows the inspected compact facts design')
    }
    $image=Get-ExperienceTarget $State 'land-art-image';$identity=Get-ExperienceTarget $State 'land-art-identity'
    Assert-Experience ((Test-ExperienceLandContains $art $image) -and (Test-ExperienceLandContains $art $identity) -and -not (Test-ExperienceLandOverlap $image $identity) -and ($image.x+$image.w*.5) -le ($identity.x-$identity.w*.5) -and $image.w -ge (156*$landScale-.1) -and $image.h -ge (78*$landScale-.1)) 'actual drawn terrain image is complete and separated from the vertical identity block'
    $defense=Get-ExperienceTarget $State 'land-fact-defense';$yieldFact=Get-ExperienceTarget $State 'land-fact-yield'
    Assert-Experience ((Test-ExperienceLandContains $summary $defense) -and (Test-ExperienceLandContains $summary $yieldFact) -and -not (Test-ExperienceLandOverlap $defense $yieldFact) -and ($defense.y+$defense.h*.5) -le ($yieldFact.y-$yieldFact.h*.5) -and [Math]::Abs($defense.x-$yieldFact.x) -lt .1 -and [Math]::Abs($defense.w-248*$landScale) -lt .1 -and [Math]::Abs($yieldFact.w-248*$landScale) -lt .1 -and [Math]::Abs($defense.h-28*$landScale) -lt .1 -and [Math]::Abs($yieldFact.h-28*$landScale) -lt .1) 'defense and yield remain complete aligned factual rows without overlap'
    $landRects=@()
    foreach($landId in $landActions) {
        $landRect=Get-ExperienceTarget $State $landId
        $owner=if($landId -eq 'land-close'){$panel}else{$commandSurface}
        Assert-Experience ($State.landTouchMinimum -gt 0 -and $landRect.w -ge ($State.landTouchMinimum-.1) -and $landRect.h -ge ($State.landTouchMinimum-.1) -and (Test-ExperienceLandContains $owner $landRect) -and -not (Test-ExperienceLandOverlap $landRect $art) -and -not (Test-ExperienceLandOverlap $landRect $summary)) ($landId+' retains a 44-unit touch target and does not cover illustration or summary')
        if($landId -ne 'land-close') {
            Assert-Experience (Test-ExperienceLandContains $commands $landRect) ($landId+' stays inside the visible command area')
            $icon=Get-ExperienceTarget $State ($landId+'-icon')
            Assert-Experience ((Test-ExperienceLandContains $landRect $icon) -and $icon.w -ge (34*$landScale-.1) -and $icon.h -ge (34*$landScale-.1)) ($landId+' draws its symbol inside the actual owning button')
            $label=Get-ExperienceTarget $State ($landId+'-label')
            $commandWidth=208
            $commandTop=if($State.landPanelState -eq 'owned'){if($landId -eq 'army-garrison'){116}elseif($landId -eq 'army-farm'){168}else{220}}elseif($State.landPanelState -eq 'busy'){if($landId -eq 'army-recall'){96}else{148}}elseif($State.landPanelState -eq 'city'){76}else{200}
            Assert-Experience (($landRect.y-$landRect.h*.5) -ge ($information.y+$information.h*.5+12*$landScale-.1)) 'separate command does not obscure its factual note'
            $inset=5
            Assert-Experience ([Math]::Abs($landRect.w-$commandWidth*$landScale) -lt .1 -and [Math]::Abs($landRect.h-44*$landScale) -lt .1 -and [Math]::Abs(($landRect.y-$landRect.h*.5)-($commands.y-$commands.h*.5)-$commandTop*$landScale) -lt .1 -and [Math]::Abs(($icon.x-$icon.w*.5)-($landRect.x-$landRect.w*.5)-$inset*$landScale) -lt .1 -and [Math]::Abs($icon.y-$landRect.y) -lt .1 -and [Math]::Abs($icon.w-34*$landScale) -lt .1 -and [Math]::Abs($icon.h-34*$landScale) -lt .1) ($landId+' uses the inspected compact 44-unit row with its icon centered vertically')
            Assert-Experience ((Test-ExperienceLandContains $landRect $label) -and -not (Test-ExperienceLandOverlap $icon $label) -and ($icon.x+$icon.w*.5) -le ($label.x-$label.w*.5) -and [Math]::Abs(($label.x-$label.w*.5)-($landRect.x-$landRect.w*.5)-48*$landScale) -lt .1 -and [Math]::Abs(($label.y-$label.h*.5)-($landRect.y-$landRect.h*.5)-4*$landScale) -lt .1 -and [Math]::Abs($label.w-156*$landScale) -lt .1 -and [Math]::Abs($label.h-36*$landScale) -lt .1) ($landId+' actually draws its readable label beside its icon inside the same clickable button')
        }
        foreach($other in $landRects) {Assert-Experience (-not (Test-ExperienceLandOverlap $landRect $other)) ($landId+' is separated from '+$other.id)}
        $landRects+=,$landRect
    }
    $close=Get-ExperienceTarget $State 'land-close';$oldClose=Get-ExperienceTarget $State 'map-close'
    Assert-Experience ($close.x -eq $oldClose.x -and $close.y -eq $oldClose.y -and $close.w -eq $oldClose.w -and $close.h -eq $oldClose.h) 'both land close aliases identify the same actual visible control'
}
function Assert-ExperienceLandModal($State) {
    $background=@($State.targets | Where-Object {$_.id -match '^land-(panel|command-surface|order-information|art(-image|-identity)?|fact-defense|fact-yield|summary|actions|close|training(-(icon|label))?|enter-city(-(icon|label))?)$|^map-close$|^army-(attack|garrison|farm)(-(icon|label))?$|^army-recall-(icon|label)$'})
    $recall=@($State.targets | Where-Object {$_.id -eq 'army-recall'});$hudRecall=@($State.targets | Where-Object {$_.id -eq 'hud-army-recall'})
    $panelRecall=$recall.Count -gt 0 -and ($hudRecall.Count -ne 1 -or $recall[0].x -ne $hudRecall[0].x -or $recall[0].y -ne $hudRecall[0].y -or $recall[0].w -ne $hudRecall[0].w -or $recall[0].h -ne $hudRecall[0].h)
    Assert-Experience (-not $State.landPanelVisible -and -not $State.landCommandTextFits -and $background.Count -eq 0 -and -not $panelRecall) 'land modal exposes no hidden illustration, summary or background command image/label targets; independent army-rail recall is identified by its own rectangle'
}
function Click-ExperienceLandCaption($State,[string]$Id) {
    $button=Get-ExperienceTarget $State $Id;$icon=Get-ExperienceTarget $State ($Id+'-icon')
    $caption=Get-ExperienceTarget $State ($Id+'-label')
    Assert-Experience ($State.landCommandTextFits -and (Test-ExperienceLandContains $button $caption) -and -not (Test-ExperienceLandOverlap $icon $caption)) ($Id+' caption click uses the actual drawn readable label inside its original button and outside the icon')
    Click-ExperienceTarget $State ($Id+'-label')
}
function Check-ExperienceLandReturn($State,[string]$Name) {
    Assert-Experience ($State.speed -eq 0) 'land inspection interaction is paused before comparing the complete campaign'
    Assert-ExperienceLand $State
    Click-ExperienceTarget $State 'land-art-image';$inspected=Capture-Experience ($Name+'-art')
    Assert-Experience ($inspected.landPanelVisible -and $inspected.mouseBlocked -and $inspected.selected -eq $State.selected -and $inspected.battleWorldHash -eq $State.battleWorldHash -and -not $inspected.confirmationOpen) 'clicking the visible terrain illustration neither selects through the panel nor spends resources'
    Assert-ExperienceMapStill $State $inspected 'land illustration click'
    Click-ExperienceTarget $inspected 'land-art-identity';$identityInspected=Capture-Experience ($Name+'-identity')
    Assert-Experience ($identityInspected.landPanelVisible -and $identityInspected.mouseBlocked -and $identityInspected.selected -eq $State.selected -and $identityInspected.battleWorldHash -eq $State.battleWorldHash -and -not $identityInspected.confirmationOpen) 'clicking the separate visible identity neither falls through nor starts an action'
    Assert-ExperienceMapStill $State $identityInspected 'land identity click'
    $inspected=$identityInspected
    Click-ExperienceTarget $inspected 'land-close';$closed=Capture-Experience ($Name+'-closed')
    Assert-Experience ($closed.tab -eq '领地' -and $closed.selected -eq -1 -and -not $closed.landPanelVisible -and -not $closed.landCommandTextFits -and $closed.mapHudVisible -and $closed.battleWorldHash -eq $State.battleWorldHash -and @($closed.targets | Where-Object {$_.id -match '^land-(panel|command-surface|order-information|art(-image|-identity)?|fact-defense|fact-yield|summary|actions|close|training(-(icon|label))?|enter-city(-(icon|label))?)$|^map-close$|^army-(attack|garrison|farm)-(icon|label)$|^army-recall-(icon|label)$'}).Count -eq 0) 'closing land removes its visible targets and restores the unchanged map'
    # Re-pick the exact original tile through its observed world projection. Do not
    # continue with a stale target or silently switch to the next attackable field.
    Assert-Experience ($State.landTileX -gt 0 -and $State.landTileY -gt 0 -and $State.landTileX -lt $cw -and $State.landTileY -lt $ch) 'original land projection is inside the current client for actual re-selection'
    Click-Experience $State.landTileX $State.landTileY;$restored=Capture-Experience ($Name+'-reselected')
    Assert-Experience ($restored.selected -eq $State.selected -and $restored.landPanelLandId -eq $State.landPanelLandId -and $restored.battleWorldHash -eq $State.battleWorldHash) 'actual terrain click reopens the exact original land without changing the campaign'
    Assert-ExperienceLand $restored
    if($restored.landHasTerrainPatch) {
        Click-ExperienceTarget $restored 'land-close';$meadowClosed=Capture-Experience ($Name+'-meadow-closed')
        Assert-Experience ($meadowClosed.selected -eq -1 -and $meadowClosed.battleWorldHash -eq $restored.battleWorldHash) 'close before picking the exposed meadow preserves the campaign'
        Assert-Experience ($restored.landMeadowX -gt 0 -and $restored.landMeadowY -gt 0 -and $restored.landMeadowX -lt $cw -and $restored.landMeadowY -lt $ch) 'observed meadow point lies inside the client'
        Click-Experience $restored.landMeadowX $restored.landMeadowY;$meadowPicked=Capture-Experience ($Name+'-meadow-picked')
        Assert-Experience ($meadowPicked.selected -eq $restored.selected -and $meadowPicked.landPanelLandId -eq $restored.landPanelLandId -and $meadowPicked.battleWorldHash -eq $restored.battleWorldHash) 'exposed meadow outside the smaller artwork still selects the same full logical land cell without an action or reward'
        Assert-ExperienceLand $meadowPicked
        $restored=$meadowPicked
    }
    return $restored
}

$edExperienceBefore=Get-ExperienceSaveHashes
[IO.File]::WriteAllText((Join-Path $edExperienceOutput 'saves-before.json'),($edExperienceBefore | ConvertTo-Json),[Text.UTF8Encoding]::new($false))
try {
    Assert-ExperiencePlayerAvailable
    $edExperienceArgs=ConvertTo-EdProcessArguments @('-edReview','-screen-fullscreen','0','-screen-width',[string]$PixelWidth,'-screen-height',[string]$PixelHeight,'-logFile',(Join-Path $edExperienceOutput 'player.log'))
    $edExperienceProcess=Start-Process -FilePath $ExecutablePath -ArgumentList $edExperienceArgs -WindowStyle Normal -PassThru
    $edExperienceDeadline=[DateTime]::UtcNow.AddSeconds(20)
    do {$edExperienceProcess.Refresh();if($edExperienceProcess.MainWindowHandle -ne 0){break};Start-Sleep -Milliseconds 200} while([DateTime]::UtcNow -lt $edExperienceDeadline -and -not $edExperienceProcess.HasExited)
    if($edExperienceProcess.HasExited -or $edExperienceProcess.MainWindowHandle -eq 0){throw 'No review client window appeared.'}
    Start-Sleep -Seconds 7
    $edExperienceClient=[EDExperiencePointer+RECT]::new()
    if(-not [EDExperiencePointer]::GetClientRect($edExperienceProcess.MainWindowHandle,[ref]$edExperienceClient)){throw 'Could not measure client area.'}
    $cw=[double]($edExperienceClient.Right-$edExperienceClient.Left);$ch=[double]($edExperienceClient.Bottom-$edExperienceClient.Top)
    Move-Experience ($cw*.5) ($ch*.48)
    $state=Capture-Experience '00-ready'
    Assert-Experience ($state.tab -eq '领地' -and -not $state.cityOpen -and -not $state.cardDetail) 'review starts on fresh map'

    # Find two genuinely unblocked points. Their positions derive from the current client, not an old UI layout.
    $dragStart=$null;$dragEnd=$null;$probe=0
    foreach($candidate in @(@(.50,.48),@(.55,.58),@(.46,.60),@(.63,.43),@(.40,.42))) {
        $probe++;$sx=$cw*$candidate[0];$sy=$ch*$candidate[1]
        Move-Experience $sx $sy;$a=Capture-Experience ('probe-'+$probe+'-start')
        if($a.mouseBlocked){continue}
        $ex=$sx+$cw*.09;$ey=$sy+$ch*.055
        Move-Experience $ex $ey;$b=Capture-Experience ('probe-'+$probe+'-end')
        if(-not $b.mouseBlocked){$dragStart=@($sx,$sy);$dragEnd=@($ex,$ey);$state=$b;break}
    }
    Assert-Experience ($null -ne $dragStart) 'unblocked map drag points found'
    Drag-Experience $dragStart[0] $dragStart[1] $dragEnd[0] $dragEnd[1]
    $dragged=Capture-Experience '01-left-drag'
    Assert-Experience (([Math]::Abs($dragged.focusX-$state.focusX)+[Math]::Abs($dragged.focusZ-$state.focusZ)) -gt .25) 'left drag pans map'
    Assert-Experience ($dragged.selected -eq $state.selected -and $dragged.tab -eq '领地' -and -not $dragged.cityOpen -and -not $dragged.cardDetail) 'left drag does not select terrain or open modal'

    # Hover at the live map viewport edge, then prove HUD hover stops camera movement.
    Focus-Experience;Move-Experience ($cw-8) ($ch*.5)
    $edgeHover=Capture-Experience '01a-edge-hover'
    Assert-Experience (-not $edgeHover.mouseBlocked) 'right map edge is not obstructed by UI'
    Start-Sleep -Milliseconds 800;Assert-ExperiencePlayerAvailable
    Move-Experience ($cw*.5) ($ch*.48)
    $edgePanned=Capture-Experience '01b-edge-panned'
    Assert-Experience (([Math]::Abs($edgePanned.focusX-$edgeHover.focusX)+[Math]::Abs($edgePanned.focusZ-$edgeHover.focusZ)) -gt .05) 'mouse edge hover pans map without a held button'
    Assert-Experience ($edgePanned.selected -eq $dragged.selected -and -not $edgePanned.cityOpen -and -not $edgePanned.cardDetail) 'edge hover does not select terrain or open a panel'
    Focus-Experience;Move-Experience ($cw*.5) ($ch*.035)
    Start-Sleep -Milliseconds 600
    $toolbarHover=Capture-Experience '01c-toolbar-hover'
    Assert-Experience $toolbarHover.mouseBlocked 'top toolbar blocks map pointer input'
    Assert-ExperienceMapStill $edgePanned $toolbarHover 'top toolbar hover'

    Click-ExperienceTarget $toolbarHover 'map-home';$homeState=Capture-Experience '02-map-home'
    $homeState=Set-ExperienceSpeed 0 '02r-map-reinforcement-origin'
    Assert-ExperienceMapHud $homeState
    $topSurface=Get-ExperienceTarget $homeState 'map-surface'
    Focus-Experience;Move-Experience $topSurface.x ($topSurface.y-$topSurface.h*.5+4)
    $topEdge=Capture-Experience '02-header-top-edge'
    Assert-Experience (-not $topEdge.mouseBlocked) 'map top edge below the transparent header remains reachable'
    Start-Sleep -Milliseconds 800;Assert-ExperiencePlayerAvailable
    Move-Experience $topSurface.x $topSurface.y
    $topPanned=Capture-Experience '02-header-top-panned'
    Assert-Experience (([Math]::Abs($topPanned.focusX-$topEdge.focusX)+[Math]::Abs($topPanned.focusZ-$topEdge.focusZ)) -gt .05) 'top edge hover still pans after the camera extends behind the HUD'
    Assert-Experience ($topPanned.selected -eq $homeState.selected -and $topPanned.battleWorldHash -eq $homeState.battleWorldHash) 'top edge hover changes only the camera'
    Click-ExperienceTarget $topPanned 'map-home';$homeState=Capture-Experience '02-header-top-return'
    Assert-ExperienceMapHud $homeState
    $atlasOrigin=$homeState
    Assert-ExperienceMinimapDesign $homeState
    foreach($atlasReadOnly in @('minimap-title','minimap-legend')) {
        $atlasBefore=$homeState
        Click-ExperienceTarget $atlasBefore $atlasReadOnly
        $homeState=Capture-Experience ('02m-readonly-'+$atlasReadOnly)
        Assert-ExperienceMapStill $atlasBefore $homeState $atlasReadOnly
        Assert-Experience ($homeState.selected -eq $atlasBefore.selected -and $homeState.battleWorldHash -eq $atlasBefore.battleWorldHash -and $homeState.minimapNavigationSequence -eq $atlasBefore.minimapNavigationSequence) 'visible atlas text blocks terrain selection and never issues navigation or campaign effects'
    }
    foreach($atlasPoint in @(@(.22,.32),@(.78,.68))) {
        $atlasArea=Get-ExperienceTarget $homeState 'minimap-area'
        # Match the integer client coordinates actually delivered by the pointer.
        $atlasX=[Math]::Round($atlasArea.x+($atlasPoint[0]-.5)*$atlasArea.w)
        $atlasY=[Math]::Round($atlasArea.y+($atlasPoint[1]-.5)*$atlasArea.h)
        Click-Experience $atlasX $atlasY
        $atlasMoved=Capture-Experience ('02m-atlas-'+[int]($atlasPoint[0]*100))
        Assert-ExperienceMinimapNavigation $homeState $atlasMoved $atlasX $atlasY
        Assert-ExperienceMinimapDesign $atlasMoved
        $homeState=$atlasMoved
    }
    Click-ExperienceTarget $homeState 'map-home';$homeState=Capture-Experience '02m-atlas-home-restored'
    Assert-ExperienceMinimap $homeState
    Assert-ExperienceMapStill $atlasOrigin $homeState 'atlas home return'
    Assert-Experience ($homeState.speed -eq 0 -and $homeState.selected -eq $atlasOrigin.selected -and $homeState.battleWorldHash -eq $atlasOrigin.battleWorldHash) 'atlas home return preserves the selection and complete campaign'
    foreach($homePart in @('map-home-art','map-home-caption')) {
        $atlasArea=Get-ExperienceTarget $homeState 'minimap-area'
        $atlasX=[Math]::Round($atlasArea.x+$atlasArea.w*.28);$atlasY=[Math]::Round($atlasArea.y+$atlasArea.h*.18)
        Click-Experience $atlasX $atlasY
        $atlasAway=Capture-Experience ('02m-away-'+$homePart)
        Assert-ExperienceMinimapNavigation $homeState $atlasAway $atlasX $atlasY
        if($homePart -eq 'map-home-art') {
            $roundHome=Get-ExperienceTarget $atlasAway 'map-home'
            # Both endpoints lie inside the visible circle. The diagonal exceeds
            # the physical drag-cancel threshold even in the smaller preview.
            Drag-Experience ($roundHome.x-$roundHome.w*.34) ($roundHome.y-$roundHome.h*.34) ($roundHome.x+$roundHome.w*.34) ($roundHome.y+$roundHome.h*.34)
            $atlasDrag=Capture-Experience '02m-home-drag-cancelled'
            Assert-ExperienceMapStill $atlasAway $atlasDrag 'round home drag'
            Assert-Experience ($atlasDrag.selected -eq $atlasAway.selected -and $atlasDrag.battleWorldHash -eq $atlasAway.battleWorldHash -and $atlasDrag.minimapNavigationSequence -eq $atlasAway.minimapNavigationSequence) 'dragging the visible round locator cancels home and blocks terrain input'
            $atlasAway=$atlasDrag
        }
        Click-ExperienceTarget $atlasAway $homePart
        $homeState=Capture-Experience ('02m-home-'+$homePart)
        Assert-ExperienceMinimapDesign $homeState
        Assert-ExperienceMapStill $atlasOrigin $homeState $homePart
        Assert-Experience ($homeState.selected -eq $atlasOrigin.selected -and $homeState.battleWorldHash -eq $atlasOrigin.battleWorldHash -and $homeState.minimapNavigationSequence -eq $atlasAway.minimapNavigationSequence) 'location image and caption invoke the same home camera action without terrain events or campaign effects'
    }
    # Navigate with the actual atlas, then click visible bank and bridge geometry.
    # Observed projections are not registered invisible controls; every click must
    # go through the normal map ray and preserve the complete paused campaign.
    $riverOrigin=$homeState
    $riverAtlas=Get-ExperienceTarget $homeState 'minimap-area'
    $riverAtlasX=[Math]::Round($riverAtlas.x+((27.5/40)-.5)*$riverAtlas.w)
    $riverAtlasY=[Math]::Round($riverAtlas.y+((30.5/40)-.5)*$riverAtlas.h)
    Click-Experience $riverAtlasX $riverAtlasY
    $riverView=Capture-Experience '02m-river-navigation'
    Assert-ExperienceMinimapNavigation $homeState $riverView $riverAtlasX $riverAtlasY
    Assert-Experience ($riverView.riverBridgeId -ge 0 -and $riverView.riverBankId -ge 0) 'river navigation exposes an unobstructed real bridge and shallow bank in the viewport'
    Click-Experience $riverView.riverBankX $riverView.riverBankY
    $bankSelected=Capture-Experience '02m-river-bank-selected'
    Assert-ExperienceLand $bankSelected
    Assert-Experience ($bankSelected.selected -eq $riverView.riverBankId -and $bankSelected.landPanelState -eq 'blocked' -and $bankSelected.battleWorldHash -eq $riverOrigin.battleWorldHash) 'decorative shallow bank still selects its exact impassable water cell without changing the campaign'
    Click-ExperienceTarget $bankSelected 'army-attack'
    $bankRejected=Capture-Experience '02m-river-bank-rejected'
    Assert-Experience (-not $bankRejected.confirmationOpen -and -not $bankRejected.orderActive -and $bankRejected.battleWorldHash -eq $riverOrigin.battleWorldHash) 'the visible disabled water attack action cannot issue an order or spend resources'
    Click-ExperienceTarget $bankRejected 'land-close'
    $bridgeView=Capture-Experience '02m-river-bank-closed'
    Assert-Experience ($bridgeView.riverBridgeId -ge 0) 'closing the water panel restores an unobstructed real bridge projection'
    Click-Experience $bridgeView.riverBridgeX $bridgeView.riverBridgeY
    $bridgeSelected=Capture-Experience '02m-river-bridge-selected'
    Assert-ExperienceLand $bridgeSelected
    Assert-Experience ($bridgeSelected.selected -eq $bridgeView.riverBridgeId -and $bridgeSelected.landPanelState -eq 'neutral' -and $bridgeSelected.battleWorldHash -eq $riverOrigin.battleWorldHash) 'raised visible bridge selects the original traversable crossing cell without issuing an order'
    Assert-ExperienceMapRecruit $bridgeSelected
    Click-ExperienceTarget $bridgeSelected 'land-close'
    $riverClosed=Capture-Experience '02m-river-bridge-closed'
    Assert-ExperienceMapRecruit $riverClosed
    Assert-Experience ($riverClosed.riverBridgeId -ge 0) 'bridge railing observation is on an unobstructed real crossing'
    Click-Experience $riverClosed.riverRailX $riverClosed.riverRailY
    $railSelected=Capture-Experience '02m-river-railing-selected'
    Assert-Experience ($railSelected.selected -eq $riverClosed.riverBridgeId -and $railSelected.landPanelState -eq 'neutral' -and $railSelected.battleWorldHash -eq $riverOrigin.battleWorldHash) 'clicking the visible outer bridge post still selects the traversable bridge instead of the neighbouring water'
    Click-ExperienceTarget $railSelected 'land-close'
    $riverClosed=Capture-Experience '02m-river-railing-closed'
    Click-ExperienceTarget $riverClosed 'map-home'
    $homeState=Capture-Experience '02m-river-home-restored'
    Assert-ExperienceMapStill $riverOrigin $homeState 'river inspection home return'
    Assert-Experience ($homeState.selected -eq $riverOrigin.selected -and $homeState.battleWorldHash -eq $riverOrigin.battleWorldHash) 'river inspection returns home with the original selection and complete campaign'
    $hudOrigin=$homeState
    if(@($homeState.mapHudArmyUids).Count -gt 1) {
        # Derive the visible gap from the two actual rows; it is deliberately
        # not a registered review target or an extra button.
        $gap=Get-ExperienceMapArmyGap $homeState
        Click-Experience $gap.x $gap.y
        $homeState=Capture-Experience '02h-army-row-gap'
        Assert-ExperienceMapArmyGap $hudOrigin $homeState
        Assert-ExperienceMapHud $homeState
    }
    if(@($homeState.mapHudArmyUids).Count -gt 0) {
        $hudDragCard=Get-ExperienceTarget $homeState 'hud-army-card-0'
        Drag-Experience ($hudDragCard.x-$hudDragCard.w*.28) $hudDragCard.y ($hudDragCard.x+$hudDragCard.w*.28) $hudDragCard.y
        $homeState=Capture-Experience '02h-card-drag'
        Assert-Experience ([string]::IsNullOrEmpty($homeState.cardDetail) -and $homeState.selected -eq $hudOrigin.selected -and $homeState.battleWorldHash -eq $hudOrigin.battleWorldHash) 'drag ending inside a HUD portrait does not open the card or mutate the campaign'
        Assert-ExperienceMapStill $hudOrigin $homeState 'HUD portrait drag'
    }
    # Inspect each actually deployed general through the new portrait rail. The
    # session stays paused, so compare the entire campaign rather than only troops.
    for($hudCard=0;$hudCard -lt @($hudOrigin.mapHudArmyUids).Count;$hudCard++) {
        Click-ExperienceTarget $homeState ('hud-army-card-'+$hudCard);$hudDetail=Capture-Experience ('02h-card-'+$hudCard)
        Assert-Experience ($hudDetail.tab -eq '领地' -and $hudDetail.cardDetailOwnedUid -eq $hudOrigin.mapHudArmyUids[$hudCard] -and $hudDetail.cardDetailOwnedUid -eq $hudDetail.selectedCardUid -and -not [string]::IsNullOrEmpty($hudDetail.cardDetail) -and $hudDetail.battleWorldHash -eq $hudOrigin.battleWorldHash) ('HUD portrait '+$hudCard+' opens its actual owned general without changing the campaign')
        Assert-ExperienceMapCommandModal $hudDetail
        Click-ExperienceTarget $hudDetail 'card-return';$homeState=Capture-Experience ('02h-card-'+$hudCard+'-return')
        Assert-Experience ($homeState.mapHudVisible -and [string]::IsNullOrEmpty($homeState.cardDetail) -and $homeState.selected -eq $hudOrigin.selected -and $homeState.battleWorldHash -eq $hudOrigin.battleWorldHash) ('HUD card '+$hudCard+' return restores map selection and complete campaign state')
        Assert-ExperienceMapStill $hudOrigin $homeState 'HUD card return'
        Assert-ExperienceMapHud $homeState
        foreach($armyPart in @('art','name','identity')) {
            Click-ExperienceTarget $homeState ('hud-army-'+$armyPart+'-'+$hudCard)
            $armyPartDetail=Capture-Experience ('02h-card-'+$hudCard+'-'+$armyPart)
            Assert-Experience ($armyPartDetail.tab -eq '领地' -and $armyPartDetail.cardDetailOwnedUid -eq $hudOrigin.mapHudArmyUids[$hudCard] -and $armyPartDetail.cardDetailOwnedUid -eq $armyPartDetail.selectedCardUid -and -not [string]::IsNullOrEmpty($armyPartDetail.cardDetail) -and $armyPartDetail.battleWorldHash -eq $hudOrigin.battleWorldHash) ('drawn army '+$armyPart+' opens the same actual owned general without spending or assignment')
            Assert-ExperienceMapCommandModal $armyPartDetail
            Click-ExperienceTarget $armyPartDetail 'card-return'
            $homeState=Capture-Experience ('02h-card-'+$hudCard+'-'+$armyPart+'-return')
            Assert-Experience ($homeState.mapHudVisible -and [string]::IsNullOrEmpty($homeState.cardDetail) -and $homeState.selected -eq $hudOrigin.selected -and $homeState.battleWorldHash -eq $hudOrigin.battleWorldHash) 'drawn army part return restores the same map and complete campaign'
            Assert-ExperienceMapStill $hudOrigin $homeState 'army drawing part return'
            Assert-ExperienceMapHud $homeState
        }
    }
    foreach($armyReadOnly in @('hud-army-header','hud-army-status')) {
        Click-ExperienceTarget $homeState $armyReadOnly
        $armyReadOnlyState=Capture-Experience ('02h-'+$armyReadOnly+'-readonly')
        Assert-Experience ($armyReadOnlyState.mapHudVisible -and $armyReadOnlyState.mouseBlocked -and [string]::IsNullOrEmpty($armyReadOnlyState.cardDetail) -and $armyReadOnlyState.selected -eq $homeState.selected -and $armyReadOnlyState.battleWorldHash -eq $homeState.battleWorldHash) 'drawn army header and status block terrain without opening a card, assigning or spending'
        Assert-ExperienceMapStill $homeState $armyReadOnlyState 'army read-only display'
        Assert-ExperienceMapHud $armyReadOnlyState
        $homeState=$armyReadOnlyState
    }
    # The new art and the caption are separate visible parts of one control.
    # Click both, return via the real map tab, and retain the entire paused World.
    foreach($entry in @(@('roster-tab','武将'),@('recruit-tab','招募'),@('reports-tab','战报'),@('hud-help','规则'))) {
        foreach($part in @('art','caption')) {
            Click-ExperienceTarget $homeState ($entry[0]+'-'+$part)
            $entryState=Capture-Experience ('02h-nav-'+$entry[0]+'-'+$part)
            Assert-Experience ($entryState.tab -eq $entry[1] -and $entryState.battleWorldHash -eq $hudOrigin.battleWorldHash -and -not $entryState.mapHudVisible) 'illustration and caption reach the same real page without spending or assignment'
            Click-ExperienceTarget $entryState 'map-tab'
            $homeState=Capture-Experience ('02h-nav-'+$entry[0]+'-'+$part+'-return')
            Assert-ExperienceMapHud $homeState
            Assert-Experience ($homeState.selected -eq $hudOrigin.selected -and $homeState.battleWorldHash -eq $hudOrigin.battleWorldHash) 'illustrated navigation return preserves selection and the complete campaign'
            Assert-ExperienceMapStill $hudOrigin $homeState 'illustrated navigation return'
        }
    }
    # Exercise the added visible artwork above the old footer, independently of
    # the existing image-center/title round trips. Never create a hidden target.
    $recruitRaised=Get-ExperienceMapRecruitRaisedPoint $homeState
    $recruitUnit=$homeState.mapHudTouchMinimum/44.0
    Drag-Experience ($recruitRaised.x-16*$recruitUnit) $recruitRaised.y ($recruitRaised.x+16*$recruitUnit) $recruitRaised.y
    $recruitDragged=Capture-Experience '02h-recruit-raised-drag'
    Assert-ExperienceMapRecruitDrag $homeState $recruitDragged
    Assert-ExperienceMapHud $recruitDragged
    $recruitRaised=Get-ExperienceMapRecruitRaisedPoint $recruitDragged
    Click-Experience $recruitRaised.x $recruitRaised.y
    $recruitOpened=Capture-Experience '02h-recruit-raised-click'
    Assert-Experience ($recruitOpened.tab -eq '招募' -and -not $recruitOpened.mapHudVisible -and $recruitOpened.selected -eq $hudOrigin.selected -and $recruitOpened.battleWorldHash -eq $hudOrigin.battleWorldHash) 'visible upper recruitment portrait opens the real recruitment page without selecting terrain or drawing a card'
    Click-ExperienceTarget $recruitOpened 'map-tab';$homeState=Capture-Experience '02h-recruit-raised-return'
    Assert-ExperienceMapHud $homeState
    Assert-Experience ($homeState.selected -eq $hudOrigin.selected -and $homeState.battleWorldHash -eq $hudOrigin.battleWorldHash) 'return restores the raised recruitment seal and complete paused campaign'
    Assert-ExperienceMapStill $hudOrigin $homeState 'raised recruitment return'
    foreach($part in @('art','caption')) {
        Click-ExperienceTarget $homeState ('map-tab-'+$part)
        $homeState=Capture-Experience ('02h-nav-map-'+$part)
        Assert-ExperienceMapHud $homeState
        Assert-Experience ($homeState.selected -eq $hudOrigin.selected -and $homeState.battleWorldHash -eq $hudOrigin.battleWorldHash) 'selected map illustration and caption preserve the campaign'
        Click-ExperienceTarget $homeState ('city-open-'+$part)
        $entryCity=Capture-Experience ('02h-nav-city-'+$part)
        Assert-Experience ($entryCity.cityOpen -and $entryCity.cityPage -eq 'overview' -and $entryCity.battleWorldHash -eq $hudOrigin.battleWorldHash) 'city illustration and caption enter the real panorama without construction or assignment'
        Click-ExperienceTarget $entryCity 'city-back'
        $homeState=Capture-Experience ('02h-nav-city-'+$part+'-return')
        Assert-ExperienceMapHud $homeState
        Assert-Experience ($homeState.selected -eq $hudOrigin.selected -and $homeState.battleWorldHash -eq $hudOrigin.battleWorldHash) 'city illustration return preserves map selection and complete campaign'
        Assert-ExperienceMapStill $hudOrigin $homeState 'city illustrated navigation return'
    }
    Click-ExperienceTarget $homeState 'hud-army-formation';$hudFormation=Capture-Experience '02h-formation'
    Assert-Experience ($hudFormation.tab -eq '武将' -and -not $hudFormation.mapHudVisible -and $hudFormation.battleWorldHash -eq $hudOrigin.battleWorldHash) 'HUD formation action reaches the actual owned roster without assigning or spending'
    Click-ExperienceTarget $hudFormation 'map-tab';$homeState=Capture-Experience '02h-formation-return'
    Assert-Experience ($homeState.mapHudVisible -and $homeState.selected -eq $hudOrigin.selected -and $homeState.battleWorldHash -eq $hudOrigin.battleWorldHash) 'formation navigation returns to the same map and unchanged campaign'
    Click-ExperienceTarget $homeState 'hud-ruler-city';$hudCity=Capture-Experience '02h-ruler-city'
    Assert-Experience ($hudCity.cityOpen -and $hudCity.cityPage -eq 'overview' -and -not $hudCity.mapHudVisible -and $hudCity.battleWorldHash -eq $hudOrigin.battleWorldHash) 'header city image opens the real city overview without completing a facility visit'
    Click-ExperienceTarget $hudCity 'city-back';$homeState=Capture-Experience '02h-ruler-city-return'
    Assert-Experience ($homeState.mapHudVisible -and $homeState.selected -eq $hudOrigin.selected -and $homeState.battleWorldHash -eq $hudOrigin.battleWorldHash) 'header city return preserves the same map selection and campaign'
    foreach($headerPart in @('hud-ruler-crest','hud-ruler-art','hud-ruler-name')) {
        if($headerPart -eq 'hud-ruler-crest') {
            $crestPoint=Get-ExperienceHeaderCrestPoint $homeState
            Click-Experience $crestPoint.x $crestPoint.y
        } else {Click-ExperienceTarget $homeState $headerPart}
        $headerCity=Capture-Experience ('02h-'+$headerPart)
        Assert-Experience ($headerCity.cityOpen -and $headerCity.cityPage -eq 'overview' -and $headerCity.battleWorldHash -eq $hudOrigin.battleWorldHash) 'drawn header crest, complete city image and name open the actual overview without side effects'
        Click-ExperienceTarget $headerCity 'city-back';$homeState=Capture-Experience ('02h-'+$headerPart+'-return')
        Assert-ExperienceMapStill $hudOrigin $homeState 'header image/name return'
        Assert-ExperienceMapHeader $homeState
    }
    # A resource cell is a visible read-only display, not an action. Clicking its
    # actual displayed rectangle must neither navigate nor select terrain below it.
    Click-ExperienceTarget $homeState 'hud-resource-0';$hudResource=Capture-Experience '02h-resource-display'
    Assert-Experience ($hudResource.mapHudVisible -and $hudResource.mouseBlocked -and $hudResource.selected -eq $homeState.selected -and $hudResource.battleWorldHash -eq $homeState.battleWorldHash) 'resource display blocks terrain input without navigation, spending or a fake resource action'
    Assert-ExperienceMapStill $hudOrigin $hudResource 'HUD navigation return'
    Assert-ExperienceMapHud $hudResource
    $homeState=$hudResource
    # Exercise the actually drawn stock text for every commodity and currency.
    foreach($resourceIndex in 0..5) {
        Click-ExperienceTarget $homeState ('hud-resource-stock-'+$resourceIndex)
        $stockClick=Capture-Experience ('02h-stock-'+$resourceIndex)
        Assert-Experience ($stockClick.mapHudVisible -and $stockClick.mouseBlocked -and $stockClick.selected -eq $homeState.selected -and $stockClick.battleWorldHash -eq $homeState.battleWorldHash) 'visible stock text is read-only and blocks the terrain beneath the transparent header'
        Assert-ExperienceMapStill $homeState $stockClick 'header stock input'
        Assert-ExperienceMapHud $stockClick
        $homeState=$stockClick
    }
    $charterOrigin=$homeState;$charterArt=Get-ExperienceTarget $homeState 'hud-quest-art'
    Move-Experience $charterArt.x $charterArt.y
    $homeState=Capture-Experience '02q-quest-sleeve-hover'
    Assert-Experience ($homeState.mapQuestHovered -and $homeState.mouseBlocked -and $homeState.battleWorldHash -eq $charterOrigin.battleWorldHash) 'visible detached task book highlights its information sleeve without changing the campaign'
    Assert-ExperienceMapStill $charterOrigin $homeState 'task book hover'
    # The book control needs a vertical in-bounds drag to exceed the real
    # physical drag threshold even at the smallest supported viewport scale.
    Drag-Experience $charterArt.x ($charterArt.y-$charterArt.h*.40) $charterArt.x ($charterArt.y+$charterArt.h*.40)
    $homeState=Capture-Experience '02q-charter-drag'
    Assert-Experience ($homeState.tab -eq '领地' -and $homeState.mapQuestId -eq $charterOrigin.mapQuestId -and $homeState.battleWorldHash -eq $charterOrigin.battleWorldHash) 'drag inside the task illustration does not open a chapter or grant progress'
    Assert-ExperienceMapStill $charterOrigin $homeState 'task charter drag'
    $taskTitle=Get-ExperienceTarget $homeState 'hud-quest-title'
    Drag-Experience $charterArt.x $charterArt.y $taskTitle.x $taskTitle.y
    $homeState=Capture-Experience '02q-quest-image-to-title-drag'
    Assert-Experience ($homeState.mapHudVisible -and $homeState.mapQuestId -eq $charterOrigin.mapQuestId -and $homeState.selected -eq $charterOrigin.selected -and $homeState.battleWorldHash -eq $charterOrigin.battleWorldHash) 'drag across the visible book and text cancels the shared chapter command without selecting land'
    Assert-ExperienceMapStill $charterOrigin $homeState 'task image-to-title drag'
    foreach($part in @('hud-quest-art','hud-quest-chapter','hud-quest-title','hud-quest-objective')) {
        $questOrigin=$homeState
        Click-ExperienceTarget $homeState $part;$charterPage=Capture-Experience ('02q-charter-'+$part)
        Assert-Experience ($charterPage.cityOpen -and $charterPage.cityPage -eq 'quests' -and $charterPage.questCurrentId -eq $questOrigin.mapQuestId -and $charterPage.battleWorldHash -eq $questOrigin.battleWorldHash) 'visible task banner, title and objective open the actual current chapter without progress or reward'
        Click-ExperienceTarget $charterPage 'map-tab';$homeState=Capture-Experience ('02q-charter-'+$part+'-return')
        Assert-ExperienceMapHud $homeState
        Assert-ExperienceMapStill $questOrigin $homeState 'task charter chapter return'
        Assert-Experience ($homeState.battleWorldHash -eq $questOrigin.battleWorldHash -and $homeState.mapQuestId -eq $questOrigin.mapQuestId) 'task charter return preserves the complete campaign and current objective'
    }
    $questProgressOrigin=$homeState
    Click-ExperienceTarget $homeState 'hud-quest-progress';$homeState=Capture-Experience '02q-quest-progress-readonly'
    Assert-Experience ($homeState.mapHudVisible -and $homeState.mouseBlocked -and $homeState.selected -eq $questProgressOrigin.selected -and $homeState.mapQuestId -eq $questProgressOrigin.mapQuestId -and $homeState.mapQuestProgress -eq $questProgressOrigin.mapQuestProgress -and $homeState.battleWorldHash -eq $questProgressOrigin.battleWorldHash) 'visible task progress remains read-only and blocks terrain without opening a chapter, advancing a task or granting a reward'
    Assert-ExperienceMapStill $questProgressOrigin $homeState 'compact task progress input'
    Assert-ExperienceMapHud $homeState
    Click-ExperienceTarget $homeState 'army-reinforcement';$mapReinforcement=Capture-Experience '02r-map-reinforcement'
    Assert-Experience ($mapReinforcement.tab -eq '征兵' -and $mapReinforcement.reinforcementReturnTab -eq '领地' -and $mapReinforcement.battleWorldHash -eq $homeState.battleWorldHash) 'map army opens the same uncharged reinforcement page with its original map context'
    Click-ExperienceTarget $mapReinforcement 'reinforcement-back';$mapReinforcementReturn=Capture-Experience '02r-map-reinforcement-return'
    Assert-Experience ($mapReinforcementReturn.tab -eq '领地' -and $mapReinforcementReturn.selected -eq $homeState.selected -and $mapReinforcementReturn.battleWorldHash -eq $homeState.battleWorldHash) 'reinforcement return preserves map selection and complete campaign state'
    Assert-ExperienceMapStill $homeState $mapReinforcementReturn 'reinforcement map origin return'
    $homeState=$mapReinforcementReturn
    $homeState=Check-ExperienceMapCityLabelInputs $homeState
    Move-Experience $homeState.homeX $homeState.homeY;$homeState=Capture-Experience '03-home-pointer'
    Assert-Experience (-not $homeState.mouseBlocked) 'actual home tile is reachable without HUD obstruction'
    Click-Experience $homeState.homeX $homeState.homeY;$city=Capture-Experience '04-home-click'
    Assert-ExperienceCityPage $city 'overview'
    Assert-Experience ($city.cityPlot -eq 4) 'actual map home click retains the central district selection'
    Assert-Experience (-not $city.questReady -and $city.claimedQuests -eq 0) 'city overview does not complete the district visit before opening facilities'
    $city=Set-ExperienceSpeed 0 '04-city-portrait-origin'
    Assert-ExperienceCityOverview $city
    Assert-ExperienceCityAffairsComposition $city
    # The new city shell uses the same real navigation commands. Exercise both
    # artwork and caption; neither path may change a paused campaign or map focus.
    Assert-ExperienceCityShell $city
    # The old ordinary-page header must be cleared when entering the city
    # directly. Do not route through the map: its camera would repaint it first.
    Move-Experience ($cw*.5) ($ch*.48)
    $city=Capture-Experience '04-city-header-clean'
    Assert-ExperienceCityHeader $city
    $headerClean=$city
    Click-ExperienceTarget $city 'roster-tab-art'
    $headerOrdinary=Capture-Experience '04-city-header-ordinary'
    Assert-Experience ($headerOrdinary.tab -eq '武将' -and $headerOrdinary.battleWorldHash -eq $headerClean.battleWorldHash -and -not $headerOrdinary.cityHeaderBackdropDrawn -and [string]::IsNullOrEmpty($headerOrdinary.cityHeaderDesignStyle)) 'ordinary roster page exposes no stale city header draw diagnostics and preserves the paused campaign'
    Click-ExperienceTarget $headerOrdinary 'city-open'
    Move-Experience ($cw*.5) ($ch*.48)
    $city=Capture-Experience '04-city-header-ordinary-return'
    Assert-ExperienceCityOverview $city
    Assert-ExperienceCityShell $city
    Assert-Experience ($city.speed -eq 0 -and $city.battleWorldHash -eq $headerClean.battleWorldHash -and $city.selected -eq $headerClean.selected -and $city.cityHudFullName -eq $headerClean.cityHudFullName) 'direct ordinary-page return preserves the exact paused city header content and campaign'
    Assert-ExperienceMapStill $headerClean $city 'direct ordinary-page city return'
    $headerPixels=Compare-ExperienceCityHeaderPixels (Join-Path $edExperienceOutput '04-city-header-clean.png') (Join-Path $edExperienceOutput '04-city-header-ordinary-return.png') $city
    [IO.File]::WriteAllText((Join-Path $edExperienceOutput '04-city-header-pixel-comparison.json'),($headerPixels | ConvertTo-Json -Depth 4),[Text.UTF8Encoding]::new($false))
    Assert-Experience ($headerPixels.differentPixels -eq 0) 'city header pixels are unchanged after directly returning from an ordinary page with the same campaign and pointer state'
    foreach($shellPart in @('art','caption')) {
        $shellOrigin=$city
        Click-ExperienceTarget $city ('map-tab-'+$shellPart);$shellMap=Capture-Experience ('04-city-shell-map-'+$shellPart)
        Assert-Experience ($shellMap.tab -eq '领地' -and $shellMap.battleWorldHash -eq $shellOrigin.battleWorldHash) 'city footer navigates to the map without changing the campaign'
        Click-ExperienceTarget $shellMap ('city-open-'+$shellPart);$city=Capture-Experience ('04-city-shell-return-'+$shellPart)
        Assert-ExperienceCityOverview $city
        Assert-ExperienceCityShell $city
        Assert-ExperienceMapStill $shellOrigin $city 'city illustrated footer round trip'
        Assert-Experience ($city.battleWorldHash -eq $shellOrigin.battleWorldHash -and $city.selected -eq $shellOrigin.selected) 'city shell round trip preserves complete campaign and map selection'
    }
    foreach($entry in @(@('city-open','主城'),@('roster-tab','武将'),@('reports-tab','战报'),@('hud-help','规则'),@('recruit-tab','招募'))) {
        foreach($shellPart in @('art','caption')) {
            $footerOrigin=$city
            Click-ExperienceTarget $city ($entry[0]+'-'+$shellPart)
            $footerPage=Capture-Experience ('04-city-footer-'+$entry[0]+'-'+$shellPart)
            Assert-Experience ($footerPage.tab -eq $entry[1] -and $footerPage.speed -eq 0 -and $footerPage.battleWorldHash -eq $footerOrigin.battleWorldHash -and $footerPage.selected -eq $footerOrigin.selected -and -not $footerPage.confirmationOpen -and [string]::IsNullOrEmpty($footerPage.cardDetail)) 'city footer image and title open the actual destination without recruitment, spending, order, selection or modal side effects'
            if($entry[0] -eq 'city-open') {
                $city=$footerPage
                Assert-Experience ($city.cityPage -eq 'overview' -and -not $city.cityPlanning -and -not $city.cityExpanded) 'selected city footer image and title retain the real compact panorama'
            } else {
                Assert-Experience ([string]::IsNullOrEmpty($footerPage.cityFooterDesignStyle) -and -not $footerPage.cityFooterDesignTextFits) 'non-city destination does not report a stale designed city footer'
                # Generic pages return through their real map command; use the
                # drawn map image/title to enter the city again, never a hidden target.
                Click-ExperienceTarget $footerPage 'map-tab'
                $footerMap=Capture-Experience ('04-city-footer-'+$entry[0]+'-'+$shellPart+'-map')
                Assert-ExperienceMapHud $footerMap
                Assert-Experience ($footerMap.battleWorldHash -eq $footerOrigin.battleWorldHash -and $footerMap.selected -eq $footerOrigin.selected) 'footer destination returns to the unchanged map without settling or drawing recruitment'
                Click-ExperienceTarget $footerMap ('city-open-'+$shellPart)
                $city=Capture-Experience ('04-city-footer-'+$entry[0]+'-'+$shellPart+'-return')
            }
            Assert-ExperienceCityOverview $city
            Assert-ExperienceCityShell $city
            Assert-ExperienceMapStill $footerOrigin $city 'city designed footer image/title round trip'
            Assert-Experience ($city.battleWorldHash -eq $footerOrigin.battleWorldHash -and $city.selected -eq $footerOrigin.selected) 'all six city footer image/title routes preserve the complete paused campaign and selected terrain'
        }
    }
    foreach($shellPart in @('art','name')) {
        $shellOrigin=$city
        Click-ExperienceTarget $city ('city-hud-home-'+$shellPart);$city=Capture-Experience ('04-city-shell-crest-'+$shellPart)
        Assert-ExperienceCityOverview $city
        Assert-ExperienceCityShell $city
        Assert-Experience ($city.battleWorldHash -eq $shellOrigin.battleWorldHash -and $city.selected -eq $shellOrigin.selected) 'home crest and name return to actual overview without spending or visiting a facility'
    }
    # Click the text actually drawn in every resource column. These remain
    # read-only displays; neither the map nor city panorama may move underneath.
    foreach($resourceIndex in 0..5) {foreach($resourcePart in @('stock','detail')) {
        $headerOrigin=$city
        Click-ExperienceTarget $city ('hud-resource-'+$resourcePart+'-'+$resourceIndex)
        $city=Capture-Experience ('04-city-header-'+$resourcePart+'-'+$resourceIndex)
        Assert-ExperienceCityOverview $city
        Assert-ExperienceCityShell $city
        Assert-Experience ($city.mouseBlocked -and $city.speed -eq 0 -and $headerOrigin.speed -eq 0 -and -not [string]::IsNullOrEmpty($headerOrigin.battleWorldHash) -and $city.battleWorldHash -eq $headerOrigin.battleWorldHash -and $city.selected -eq $headerOrigin.selected -and $city.cityHudFullName -eq $headerOrigin.cityHudFullName -and [string]::IsNullOrEmpty($city.cardDetail) -and -not $city.confirmationOpen) 'city stock and income text blocks terrain without navigation, spending, selection or a fake resource action'
        Assert-ExperienceMapStill $headerOrigin $city 'city header stock/income click'
        Assert-Experience ([Math]::Abs($city.zoom-$headerOrigin.zoom) -lt .001 -and $city.cityExpanded -eq $headerOrigin.cityExpanded -and $city.cityPlanning -eq $headerOrigin.cityPlanning -and [Math]::Abs($city.cityPanoramaZoom-$headerOrigin.cityPanoramaZoom) -lt .001 -and [Math]::Abs($city.cityPanoramaPanX-$headerOrigin.cityPanoramaPanX)+[Math]::Abs($city.cityPanoramaPanY-$headerOrigin.cityPanoramaPanY) -lt .1) 'read-only city header preserves both map zoom and the complete city panorama view'
    }}
    $cityPortraitOrigin=$city
    $cityPortrait=Get-ExperienceTarget $city 'city-general-0'
    Drag-Experience $cityPortrait.x $cityPortrait.y ($cityPortrait.x+24) ($cityPortrait.y+16)
    $city=Capture-Experience '04-city-portrait-drag'
    Assert-Experience ([string]::IsNullOrEmpty($city.cardDetail) -and $city.battleWorldHash -eq $cityPortraitOrigin.battleWorldHash -and $city.selected -eq $cityPortraitOrigin.selected) 'dragging a vertical city portrait neither opens details nor selects underlying terrain'
    Assert-ExperienceMapStill $cityPortraitOrigin $city 'city portrait drag'
    for($cityCard=0;$cityCard -lt @($cityPortraitOrigin.cityOverviewArmyUids).Count;$cityCard++) {
        Click-ExperienceTarget $city ('city-general-'+$cityCard);$cityDetail=Capture-Experience ('04-city-portrait-'+$cityCard)
        Assert-Experience ($cityDetail.tab -eq '主城' -and $cityDetail.cardDetailOwnedUid -eq $cityPortraitOrigin.cityOverviewArmyUids[$cityCard] -and -not [string]::IsNullOrEmpty($cityDetail.cardDetail) -and -not $cityDetail.cityOverviewVisible -and $cityDetail.battleWorldHash -eq $cityPortraitOrigin.battleWorldHash) ('city portrait '+$cityCard+' opens the actual owned card without changing the campaign')
        Assert-Experience (@($cityDetail.targets | Where-Object {$_.id -match '^city-general-[0-2]$|^city-nav-|^city-army-action-|^city-affairs-' -or $_.id -in @('city-overview-quests-art','city-overview-quests-caption','city-overview-facilities','city-construction-page','city-overview-quests','city-formation','city-recruit','city-army-training')}).Count -eq 0) ('city portrait '+$cityCard+' detail suppresses the obscured city side-rail targets')
        Assert-Experience ([string]::IsNullOrEmpty($cityDetail.cityShellStyle) -and @($cityDetail.targets | Where-Object {$_.id -match '^city-hud-|^hud-resource-' -or $_.id -in @('city-open-art','city-open-caption','map-tab-art','map-tab-caption')}).Count -eq 0) 'city portrait modal exposes no obscured crest, resource or illustrated navigation targets'
        Assert-Experience ([string]::IsNullOrEmpty($cityDetail.cityHeaderDesignStyle) -and -not $cityDetail.cityHeaderDesignTextFits -and -not $cityDetail.cityHeaderNameWrapped -and -not $cityDetail.cityHeaderBackdropDrawn -and @($cityDetail.targets | Where-Object {$_.id -in @('city-header-backdrop','city-header-production')}).Count -eq 0) 'city portrait modal suppresses stale header design, backdrop and full-name fit diagnostics together with its obscured targets'
        Assert-Experience ([string]::IsNullOrEmpty($cityDetail.cityFooterDesignStyle) -and -not $cityDetail.cityFooterDesignTextFits -and @($cityDetail.targets | Where-Object {$_.id -match '^(city-open|map-tab|roster-tab|reports-tab|hud-help|recruit-tab)-(art|caption)$'}).Count -eq 0) 'city portrait modal suppresses every obscured footer image/title target and its font-fit diagnostic'
        Click-ExperienceTarget $cityDetail 'card-return';$city=Capture-Experience ('04-city-portrait-'+$cityCard+'-return')
        Assert-ExperienceCityOverview $city
        Assert-Experience ([string]::IsNullOrEmpty($city.cardDetail) -and $city.selected -eq $cityPortraitOrigin.selected -and $city.battleWorldHash -eq $cityPortraitOrigin.battleWorldHash) ('city portrait '+$cityCard+' returns to the same overview and unchanged campaign')
        $identityOffset=$cityCard*4
        $identityBounds=Convert-ExperienceCityBounds (@($city.cityOverviewGeneralNames)[$identityOffset..($identityOffset+3)])
        Click-Experience $identityBounds.x $identityBounds.y;$cityNameDetail=Capture-Experience ('04-city-identity-'+$cityCard)
        Assert-Experience ($cityNameDetail.cardDetailOwnedUid -eq $cityPortraitOrigin.cityOverviewArmyUids[$cityCard] -and -not [string]::IsNullOrEmpty($cityNameDetail.cardDetail) -and $cityNameDetail.battleWorldHash -eq $cityPortraitOrigin.battleWorldHash) ('separate city name base '+$cityCard+' opens the same actual owned general as its portrait')
        Click-ExperienceTarget $cityNameDetail 'card-return';$city=Capture-Experience ('04-city-identity-'+$cityCard+'-return')
        Assert-ExperienceCityAffairsComposition $city
        foreach($cityImagePart in @('art','name')) {
            Click-ExperienceTarget $city ('city-general-'+$cityImagePart+'-'+$cityCard)
            $cityDesignDetail=Capture-Experience ('04-city-designed-'+$cityImagePart+'-'+$cityCard)
            Assert-Experience ($cityDesignDetail.cardDetailOwnedUid -eq $cityPortraitOrigin.cityOverviewArmyUids[$cityCard] -and $cityDesignDetail.battleWorldHash -eq $cityPortraitOrigin.battleWorldHash) 'designed portrait image and curved name base open the same owned general without mutation'
            Assert-Experience (@($cityDesignDetail.targets | Where-Object {$_.id -match '^city-general-(art|name)-'}).Count -eq 0) 'modal card hides all underlying designed portrait regions'
            Click-ExperienceTarget $cityDesignDetail 'card-return';$city=Capture-Experience ('04-city-designed-'+$cityImagePart+'-'+$cityCard+'-return')
            Assert-ExperienceCityOverview $city
            Assert-Experience ($city.battleWorldHash -eq $cityPortraitOrigin.battleWorldHash -and $city.selected -eq $cityPortraitOrigin.selected) 'designed portrait round trip preserves the campaign and selected terrain'
        }
    }
    $cityArmyActionOrigin=$city
    for($armyAction=0;$armyAction -lt 3;$armyAction++) {
        foreach($armyPart in @('art','caption')) {
            Click-ExperienceTarget $city ('city-army-action-'+$armyAction+'-'+$armyPart)
            $cityActionPage=Capture-Experience ('04-city-army-'+$armyAction+'-'+$armyPart)
            $expectedTab=@('武将','征兵','练兵')[$armyAction]
            $returnTarget=@('city-formation-back','reinforcement-back','training-back')[$armyAction]
            Assert-Experience ($cityActionPage.tab -eq $expectedTab -and -not $cityActionPage.cityOverviewVisible -and -not $cityActionPage.orderActive -and $cityActionPage.battleWorldHash -eq $cityArmyActionOrigin.battleWorldHash) ('city army '+$armyPart+' '+$armyAction+' opens the actual command page without paying, recruiting or ordering')
            Assert-Experience (@($cityActionPage.targets | Where-Object {$_.id -match '^city-army-action-'}).Count -eq 0) 'command destination exposes no removed city army illustration targets'
            Click-ExperienceTarget $cityActionPage $returnTarget
            $city=Capture-Experience ('04-city-army-'+$armyAction+'-'+$armyPart+'-return')
            Assert-ExperienceCityAffairsComposition $city
            Assert-Experience ($city.selected -eq $cityArmyActionOrigin.selected -and $city.battleWorldHash -eq $cityArmyActionOrigin.battleWorldHash) ('city army '+$armyPart+' '+$armyAction+' returns to the same city and unchanged paused campaign')
        }
    }
    Click-ExperienceTarget $city 'city-overview-quests-art';$cityScrollPage=Capture-Experience '04-city-task-scroll'
    Assert-Experience ($cityScrollPage.cityPage -eq 'quests' -and $cityScrollPage.battleWorldHash -eq $cityPortraitOrigin.battleWorldHash -and -not $cityScrollPage.questReady) 'visible chapter scroll opens actual tasks without completing the district visit or claiming rewards'
    Click-ExperienceTarget $cityScrollPage 'city-back';$city=Capture-Experience '04-city-task-scroll-return'
    Assert-ExperienceCityAffairsComposition $city
    Click-ExperienceTarget $city 'city-overview-quests-caption';$cityTaskCaption=Capture-Experience '04-city-task-caption'
    Assert-Experience ($cityTaskCaption.cityPage -eq 'quests' -and $cityTaskCaption.battleWorldHash -eq $cityPortraitOrigin.battleWorldHash -and -not $cityTaskCaption.questReady) 'separate chapter title opens the same actual task page without completing a visit or paying rewards'
    Click-ExperienceTarget $cityTaskCaption 'city-back';$city=Capture-Experience '04-city-task-caption-return'
    Assert-ExperienceCityAffairsComposition $city
    Click-ExperienceTarget $city 'city-overview-quests';$cityAffairs=Capture-Experience '04-city-side-affairs'
    Assert-Experience ($cityAffairs.cityPage -eq 'quests' -and $cityAffairs.battleWorldHash -eq $cityPortraitOrigin.battleWorldHash -and -not $cityAffairs.questReady) 'side affairs opens actual chapter tasks without claiming rewards or completing a facility visit'
    Click-ExperienceTarget $cityAffairs 'city-back';$city=Capture-Experience '04-city-side-affairs-return'
    Assert-ExperienceCityOverview $city
    Click-ExperienceTarget $city 'city-army-training';$citySideTraining=Capture-Experience '04-city-side-training'
    Assert-Experience ($citySideTraining.tab -eq '练兵' -and $citySideTraining.trainingReturnTab -eq '主城' -and $citySideTraining.battleWorldHash -eq $cityPortraitOrigin.battleWorldHash -and -not $citySideTraining.orderActive) 'side army training opens the independent real plan without starting an order or spending'
    Click-ExperienceTarget $citySideTraining 'training-back';$city=Capture-Experience '04-city-side-training-return'
    Assert-ExperienceCityOverview $city
    Assert-Experience ($city.battleWorldHash -eq $cityPortraitOrigin.battleWorldHash -and $city.selected -eq $cityPortraitOrigin.selected) 'side training return preserves the complete campaign and original selected land'
    Assert-ExperienceTargetInClient $city 'city-construction-page'
    Click-ExperienceTarget $city 'city-construction-page';$emptyConstruction=Capture-Experience '04-construction-empty'
    foreach($buttonId in @('city-construction-plan','city-construction-catalog')) {Assert-ExperienceTargetInClient $emptyConstruction $buttonId}
    Assert-Experience ($emptyConstruction.cityPage -eq 'construction' -and -not $emptyConstruction.cityCompletionAvailable -and -not $emptyConstruction.questReady -and $emptyConstruction.battleWorldHash -eq $city.battleWorldHash) 'construction page opens without inventing a completion, visiting facilities or changing the campaign'
    Click-ExperienceTarget $emptyConstruction 'city-construction-plan';$emptyPlan=Capture-Experience '04-construction-plan'
    Assert-Experience ($emptyPlan.cityPage -eq 'overview' -and $emptyPlan.cityPlanning -and $emptyPlan.battleWorldHash -eq $city.battleWorldHash) 'idle construction leads to actual panorama selection without starting a job'
    Click-ExperienceTarget $emptyPlan 'city-back';$city=Capture-Experience '04-construction-plan-back'
    Assert-Experience (-not $city.cityPlanning -and -not $city.cityBuilding -and -not $city.questReady) 'return closes construction site selection before leaving the city'
    $cityHeadingY=(Get-ExperienceTarget $city 'city-overview').y
    Drag-Experience ($cw*.17) $cityHeadingY ($cw*.19) ($cityHeadingY+12)
    $cityOverviewBlocked=Capture-Experience '04a-city-overview-pointer-block';Assert-ExperienceMapStill $city $cityOverviewBlocked 'city overview'
    Assert-Experience ($cityOverviewBlocked.selected -eq $city.selected) 'city overview drag cannot select underlying land'
    Click-ExperienceTarget $cityOverviewBlocked 'city-formation';$cityFormation=Capture-Experience '04b-city-formation'
    Assert-Experience ($cityFormation.tab -eq '武将' -and $cityFormation.selected -eq $city.selected) 'city army entry opens the actual formation roster and preserves map selection'
    Click-ExperienceTarget $cityFormation 'city-formation-back';$cityFormationReturn=Capture-Experience '04c-city-formation-return'
    Assert-ExperienceCityPage $cityFormationReturn 'overview'
    $reinforcementOrigin=Set-ExperienceSpeed 0 '04r-origin-paused'
    Click-ExperienceTarget $reinforcementOrigin 'city-recruit';$reinforcement=Capture-Experience '04r-ready'
    Assert-Experience ($reinforcement.tab -eq '征兵' -and $reinforcement.reinforcementReturnTab -eq '主城' -and $reinforcement.reinforcementReturnPage -eq 'overview' -and $reinforcement.battleWorldHash -eq $reinforcementOrigin.battleWorldHash) 'city recruitment opens an independent cost plan without changing campaign state'
    Click-ExperienceTarget $reinforcement 'reinforcement-amount-0';$reinforcementLess=Capture-Experience '04r-less'
    Assert-Experience ($reinforcementLess.reinforcementAmount -eq [Math]::Max(0,$reinforcement.reinforcementAmount-50) -and $reinforcementLess.battleWorldHash -eq $reinforcement.battleWorldHash) 'visible minus button changes the preview quantity without payment'
    Click-ExperienceTarget $reinforcementLess 'reinforcement-formation';$reinforcementFormation=Capture-Experience '04r-formation'
    Click-ExperienceTarget $reinforcementFormation 'reinforcement-formation-back';$reinforcement=Capture-Experience '04r-formation-return'
    Assert-Experience ($reinforcement.tab -eq '征兵' -and $reinforcement.reinforcementAmount -eq $reinforcementLess.reinforcementAmount -and $reinforcement.battleWorldHash -eq $reinforcementLess.battleWorldHash) 'formation return preserves the same reinforcement amount and uncharged world'
    $reinforcementCard=Get-ExperienceTarget $reinforcement 'reinforcement-general-2'
    Drag-Experience $reinforcementCard.x $reinforcementCard.y ($reinforcementCard.x+35) ($reinforcementCard.y+16)
    $reinforcementDrag=Capture-Experience '04r-card-drag';Assert-ExperienceMapStill $reinforcement $reinforcementDrag 'independent reinforcement page'
    Assert-Experience ([string]::IsNullOrEmpty($reinforcementDrag.cardDetail) -and $reinforcementDrag.selected -eq $reinforcement.selected -and $reinforcementDrag.battleWorldHash -eq $reinforcement.battleWorldHash) 'dragging a visible reinforcement card neither opens it nor selects the map'
    $reinforcementSlider=Get-ExperienceTarget $reinforcementDrag 'reinforcement-slider'
    Drag-Experience ($reinforcementSlider.x-$reinforcementSlider.w*.30) $reinforcementSlider.y ($reinforcementSlider.x+$reinforcementSlider.w*.10) $reinforcementSlider.y
    $reinforcementSlide=Capture-Experience '04r-slider'
    Assert-Experience ($reinforcementSlide.reinforcementAmount -gt 0 -and $reinforcementSlide.reinforcementAmount -ne $reinforcementDrag.reinforcementAmount -and $reinforcementSlide.battleWorldHash -eq $reinforcementDrag.battleWorldHash) 'actual slider drag changes quantity while preserving campaign state'
    Click-ExperienceTarget $reinforcementSlide 'reinforcement-amount-3';$reinforcementMaximum=Capture-Experience '04r-maximum-preview'
    Assert-Experience ($reinforcementMaximum.reinforcementAmount -eq ($reinforcementMaximum.reinforcementCap-$reinforcementMaximum.troops) -and $reinforcementMaximum.battleWorldHash -eq $reinforcementSlide.battleWorldHash) 'fill-to-cap previews the exact remaining troop capacity without recruiting'
    Click-ExperienceTarget $reinforcementMaximum 'reinforcement-back';$reinforcementCancelled=Capture-Experience '04r-plan-back'
    Assert-Experience ($reinforcementCancelled.tab -eq '主城' -and $reinforcementCancelled.cityPage -eq 'overview' -and $reinforcementCancelled.battleWorldHash -eq $reinforcementOrigin.battleWorldHash) 'back abandons the preview without changing troops or resources'
    Click-ExperienceTarget $reinforcementCancelled 'city-recruit';$reinforcement=Capture-Experience '04r-fresh-plan'
    Click-ExperienceTarget $reinforcement 'reinforcement-amount-0';$reinforcement=Capture-Experience '04r-selected-amount'
    Click-ExperienceTarget $reinforcement 'reinforcement-confirm';$reinforcementConfirm=Capture-Experience '04r-confirm'
    Assert-Experience ($reinforcementConfirm.confirmationOpen -and $reinforcementConfirm.battleWorldHash -eq $reinforcement.battleWorldHash) 'opening actual reinforcement approval does not charge'
    Click-ExperienceTarget $reinforcementConfirm 'confirm-no';$reinforcementNo=Capture-Experience '04r-confirm-cancel'
    Assert-Experience (-not $reinforcementNo.confirmationOpen -and $reinforcementNo.tab -eq '征兵' -and $reinforcementNo.battleWorldHash -eq $reinforcement.battleWorldHash) 'cancelling approval leaves the selected plan and campaign unchanged'
    Click-ExperienceTarget $reinforcementNo 'reinforcement-confirm';$reinforcementConfirm=Capture-Experience '04r-confirm-again'
    $reinforcementApprove=Get-ExperienceTarget $reinforcementConfirm 'confirm-yes'
    Click-Experience $reinforcementApprove.x $reinforcementApprove.y;$reinforcementDone=Capture-Experience '04r-completed'
    Assert-Experience ($reinforcementDone.reinforcementCompleted -and $reinforcementDone.reinforcementAdded -eq $reinforcement.reinforcementAmount -and $reinforcementDone.troops -eq ($reinforcement.troops+$reinforcement.reinforcementAmount)) 'approved amount is added once and shown in a completion receipt'
    for($resourceIndex=0;$resourceIndex -lt 3;$resourceIndex++) {
        Assert-Experience ([Math]::Abs(($reinforcement.reinforcementBalance[$resourceIndex]-$reinforcementDone.reinforcementBalance[$resourceIndex])-$reinforcement.reinforcementCost[$resourceIndex]) -lt .00001 -and [Math]::Abs($reinforcementDone.reinforcementPaid[$resourceIndex]-$reinforcement.reinforcementCost[$resourceIndex]) -lt .00001) ('receipt cost matches actual resource debit '+$resourceIndex)
    }
    Click-Experience $reinforcementApprove.x $reinforcementApprove.y;$reinforcementRepeated=Capture-Experience '04r-approval-repeat'
    Assert-Experience ($reinforcementRepeated.reinforcementCompleted -and $reinforcementRepeated.battleWorldHash -eq $reinforcementDone.battleWorldHash) 'repeated approval coordinate does not recruit or pay a second time'
    Click-ExperienceTarget $reinforcementRepeated 'reinforcement-done';$reinforcementReturned=Capture-Experience '04r-done-return'
    Click-ExperienceTarget $reinforcementReturned 'city-recruit';$reinforcementReopened=Capture-Experience '04r-reopened'
    Assert-Experience (-not $reinforcementReopened.reinforcementCompleted -and $reinforcementReopened.battleWorldHash -eq $reinforcementDone.battleWorldHash) 'reopening shows a new preview without replaying the completed purchase'
    Click-ExperienceTarget $reinforcementReopened 'reinforcement-back';$cityFormationReturn=Capture-Experience '04r-final-return'
    Assert-ExperienceCityPage $cityFormationReturn 'overview'
    Click-ExperienceTarget $cityFormationReturn 'city-building-4';$panoramaBuilding=Capture-Experience '04d-panorama-town-hall'
    Assert-Experience ($panoramaBuilding.cityPage -eq 'facilities' -and $panoramaBuilding.cityPlot -eq 4 -and -not $panoramaBuilding.cityBuilding) 'panorama town hall hotspot opens its actual facility without starting an upgrade'
    Click-ExperienceTarget $panoramaBuilding 'city-back';$panoramaReturn=Capture-Experience '04e-panorama-building-return'
    Assert-ExperienceCityPage $panoramaReturn 'overview'
    # The existing hall hotspot above performs the first legitimate facility visit.
    # Compare the complete World only after that action, so art navigation neither
    # hides a new visit nor falsely rejects the original quest's real progression.
    $cityArtOrigin=$panoramaReturn
    Assert-ExperienceCityAffairsComposition $cityArtOrigin
    Click-ExperienceTarget $panoramaReturn 'city-affairs-art';$cityHallArt=Capture-Experience '04e-affairs-hall-art'
    Assert-Experience ($cityHallArt.cityPage -eq 'facilities' -and $cityHallArt.cityPlot -eq $cityArtOrigin.cityAffairsActualPlot -and $cityHallArt.cityFacility -eq $cityArtOrigin.cityAffairsDisplayedKind -and -not $cityHallArt.cityBuilding -and $cityHallArt.battleWorldHash -eq $cityArtOrigin.battleWorldHash) 'idle affairs building art reaches the real town hall without upgrading or repeating quest progression'
    Click-ExperienceTarget $cityHallArt 'city-back';$panoramaReturn=Capture-Experience '04e-affairs-hall-return'
    Assert-ExperienceCityAffairsComposition $panoramaReturn
    foreach($affairsPart in @('building','caption')) {
        $affairsOrigin=$panoramaReturn
        Click-ExperienceTarget $panoramaReturn ('city-affairs-'+$affairsPart);$affairsDestination=Capture-Experience ('04e-affairs-hall-'+$affairsPart)
        Assert-Experience ($affairsDestination.cityPage -eq 'facilities' -and $affairsDestination.cityPlot -eq $affairsOrigin.cityAffairsActualPlot -and $affairsDestination.cityFacility -eq $affairsOrigin.cityAffairsDisplayedKind -and -not $affairsDestination.cityBuilding -and $affairsDestination.battleWorldHash -eq $affairsOrigin.battleWorldHash) ('idle affairs '+$affairsPart+' opens the same actual hall without construction or repeated quest effects')
        Click-ExperienceTarget $affairsDestination 'city-back';$panoramaReturn=Capture-Experience ('04e-affairs-hall-'+$affairsPart+'-return')
        Assert-ExperienceCityAffairsComposition $panoramaReturn
        Assert-Experience ($panoramaReturn.battleWorldHash -eq $affairsOrigin.battleWorldHash) ('idle affairs '+$affairsPart+' return preserves complete campaign')
        Assert-ExperienceMapStill $affairsOrigin $panoramaReturn ('city affairs '+$affairsPart+' navigation')
    }
    foreach($cityNavPage in @('overview','catalog','facilities','quests','training')) {
        $cityNavOrigin=$panoramaReturn
        Click-ExperienceTarget $panoramaReturn ('city-nav-'+$cityNavPage+'-art');$cityNavDestination=Capture-Experience ('04e-nav-art-'+$cityNavPage)
        if($cityNavPage -eq 'training') {
            Assert-Experience ($cityNavDestination.tab -eq '练兵' -and $cityNavDestination.trainingReturnTab -eq '主城' -and -not $cityNavDestination.orderActive) 'training navigation image opens the independent plan with the actual city return source'
            Click-ExperienceTarget $cityNavDestination 'training-back';$panoramaReturn=Capture-Experience ('04e-nav-art-'+$cityNavPage+'-return')
        } else {
            Assert-ExperienceCityPage $cityNavDestination $cityNavPage
            if($cityNavPage -eq 'overview') {$panoramaReturn=$cityNavDestination}
            else {Click-ExperienceTarget $cityNavDestination 'city-back';$panoramaReturn=Capture-Experience ('04e-nav-art-'+$cityNavPage+'-return')}
        }
        Assert-Experience ($cityNavDestination.battleWorldHash -eq $cityNavOrigin.battleWorldHash -and $panoramaReturn.battleWorldHash -eq $cityNavOrigin.battleWorldHash -and $panoramaReturn.selected -eq $cityNavOrigin.selected) ($cityNavPage+' illustration navigates and returns without spending, starting orders or changing the campaign')
        Assert-ExperienceCityAffairsComposition $panoramaReturn
        Assert-ExperienceMapStill $cityNavOrigin $panoramaReturn ('city '+$cityNavPage+' image navigation')
    }
    foreach($cityNavPage in @('overview','catalog','facilities','quests','training')) {
        $cityNavOrigin=$panoramaReturn
        Assert-ExperienceCityEdgeDock $cityNavOrigin
        Click-ExperienceTarget $panoramaReturn ('city-nav-'+$cityNavPage+'-caption');$cityNavDestination=Capture-Experience ('04e-nav-caption-'+$cityNavPage)
        if($cityNavPage -eq 'training') {
            Assert-Experience ($cityNavDestination.tab -eq '练兵' -and $cityNavDestination.trainingReturnTab -eq '主城' -and -not $cityNavDestination.orderActive) 'training navigation caption opens the independent plan with the actual city return source'
            Click-ExperienceTarget $cityNavDestination 'training-back';$panoramaReturn=Capture-Experience ('04e-nav-caption-'+$cityNavPage+'-return')
        } else {
            Assert-ExperienceCityPage $cityNavDestination $cityNavPage
            if($cityNavPage -eq 'overview') {$panoramaReturn=$cityNavDestination}
            else {Click-ExperienceTarget $cityNavDestination 'city-back';$panoramaReturn=Capture-Experience ('04e-nav-caption-'+$cityNavPage+'-return')}
        }
        Assert-Experience ($cityNavDestination.battleWorldHash -eq $cityNavOrigin.battleWorldHash -and $panoramaReturn.battleWorldHash -eq $cityNavOrigin.battleWorldHash -and $panoramaReturn.selected -eq $cityNavOrigin.selected) ($cityNavPage+' caption navigates and returns without spending, starting orders or changing the campaign')
        Assert-ExperienceCityEdgeDock $panoramaReturn
        Assert-ExperienceCityAffairsComposition $panoramaReturn
        Assert-ExperienceMapStill $cityNavOrigin $panoramaReturn ('city '+$cityNavPage+' caption navigation')
    }
    foreach($dockBackPart in @('art','caption')) {
        $dockBackOrigin=$panoramaReturn
        Assert-ExperienceCityEdgeDock $dockBackOrigin
        Click-ExperienceTarget $panoramaReturn ('city-dock-back-'+$dockBackPart);$dockBackMap=Capture-Experience ('04e-dock-back-'+$dockBackPart)
        Assert-Experience ($dockBackMap.tab -eq '领地' -and -not $dockBackMap.cityOpen -and $dockBackMap.battleWorldHash -eq $dockBackOrigin.battleWorldHash -and $dockBackMap.selected -eq $dockBackOrigin.selected) ('visible bottom return '+$dockBackPart+' reaches the map without spending or selecting terrain')
        Assert-ExperienceMapStill $dockBackOrigin $dockBackMap ('city dock '+$dockBackPart+' return')
        Click-ExperienceTarget $dockBackMap 'city-open';$panoramaReturn=Capture-Experience ('04e-dock-back-'+$dockBackPart+'-reopen')
        Assert-ExperienceCityAffairsComposition $panoramaReturn
        Assert-Experience ($panoramaReturn.battleWorldHash -eq $dockBackOrigin.battleWorldHash -and $panoramaReturn.selected -eq $dockBackOrigin.selected) ('return '+$dockBackPart+' and actual city reentry preserve the complete campaign and selection')
        Assert-ExperienceMapStill $dockBackOrigin $panoramaReturn ('city dock '+$dockBackPart+' round trip')
    }
    Click-ExperienceTarget $panoramaReturn 'city-planning';$sitePlan=Capture-Experience '04e-site-plan'
    Assert-Experience ($sitePlan.cityPlanning -and $sitePlan.cityPage -eq 'overview' -and @($sitePlan.citySiteIds).Count -eq $sitePlan.cityAvailableSites -and $sitePlan.cityAvailableSites -gt 0 -and $sitePlan.battleWorldHash -eq $panoramaReturn.battleWorldHash) 'panorama shows every real available site without paying or constructing'
    $siteId=[int]$sitePlan.citySiteIds[0];$siteTarget=Get-ExperienceTarget $sitePlan ('city-site-'+$siteId)
    Assert-ExperienceTargetInClient $sitePlan ('city-site-'+$siteId)
    Drag-Experience ($siteTarget.x-$siteTarget.w*.30) $siteTarget.y ($siteTarget.x+$siteTarget.w*.30) $siteTarget.y
    $siteDrag=Capture-Experience '04e-site-drag'
    Assert-Experience ($siteDrag.cityPage -eq 'overview' -and $siteDrag.cityPlanning -and $siteDrag.battleWorldHash -eq $sitePlan.battleWorldHash) 'dragging within a visible site does not enter construction or move the map'
    Assert-ExperienceMapStill $sitePlan $siteDrag 'panorama site drag'
    Click-ExperienceTarget $siteDrag ('city-site-'+$siteId);$siteDetail=Capture-Experience '04e-site-detail'
    Assert-Experience ($siteDetail.cityPage -eq 'facilities' -and $siteDetail.cityPlot -eq $siteId -and -not $siteDetail.cityBuilding -and $siteDetail.battleWorldHash -eq $sitePlan.battleWorldHash) 'real empty-site click reaches the same plot without starting construction'
    Click-ExperienceTarget $siteDetail 'city-back';$siteReturn=Capture-Experience '04e-site-return'
    Assert-Experience ($siteReturn.cityPage -eq 'overview' -and $siteReturn.cityPlanning -and $siteReturn.battleWorldHash -eq $sitePlan.battleWorldHash) 'facility back preserves the panorama planning layer without spending'
    Click-ExperienceTarget $siteReturn 'city-back';$panoramaReturn=Capture-Experience '04e-site-close'
    Assert-Experience ($panoramaReturn.tab -eq '主城' -and $panoramaReturn.cityPage -eq 'overview' -and -not $panoramaReturn.cityPlanning -and @($panoramaReturn.citySiteIds).Count -eq 0 -and $panoramaReturn.battleWorldHash -eq $sitePlan.battleWorldHash) 'back closes site selection before leaving the city and removes its interaction targets'
    $cameraOrigin=$panoramaReturn
    foreach($cameraButton in @('city-panorama-expand','city-panorama-plus','city-panorama-minus','city-panorama-reset','city-panorama-viewport')) {Assert-ExperienceTargetInClient $cameraOrigin $cameraButton}
    Assert-Experience ([Math]::Abs($cameraOrigin.cityPanoramaImageWidth/$cameraOrigin.cityPanoramaImageHeight-1000.0/440.0) -lt .0001) 'compact panorama preserves the original artwork aspect ratio'
    Assert-ExperienceCityLandscape $cameraOrigin
    Click-ExperienceTarget $cameraOrigin 'city-panorama-expand';$cityExpanded=Capture-Experience '04p-city-expanded'
    Assert-ExperienceCityPanoramaExpanded $cameraOrigin $cityExpanded
    Assert-ExperienceCityEdgeDock $cityExpanded
    Assert-Experience (@($cityExpanded.targets | Where-Object {$_.id -match '^city-affairs-' -or $_.id -in @('city-overview-quests-art','city-overview-quests-caption')}).Count -eq 0) 'expanded panorama exposes none of the removed affairs illustration targets'
    Assert-ExperienceCityLandscape $cityExpanded
    Assert-Experience ([Math]::Abs($cityExpanded.cityPanoramaImageWidth/$cityExpanded.cityPanoramaImageHeight-1000.0/440.0) -lt .0001) 'expanded panorama remains proportional'
    # Click the rear roof of the scenic court drawn at source (161,168), using the town transform.
    # This is scenery, so do not manufacture an invisible review target for it.
    $streetX=$cityExpanded.cityPanoramaImagePixelX+$cityExpanded.cityPanoramaImagePixelWidth*161/1000
    $streetY=$cityExpanded.cityPanoramaImagePixelY+$cityExpanded.cityPanoramaImagePixelHeight*168/440
    $streetViewport=Get-ExperienceTarget $cityExpanded 'city-panorama-viewport'
    Assert-Experience ($streetX -gt $streetViewport.x-$streetViewport.w/2 -and $streetX -lt $streetViewport.x+$streetViewport.w/2 -and $streetY -gt $streetViewport.y-$streetViewport.h/2 -and $streetY -lt $streetViewport.y+$streetViewport.h/2) 'the decorative street roof is visible inside the real expanded viewport'
    Click-Experience $streetX $streetY;$streetClick=Capture-Experience '04p-city-street-scenery'
    Assert-Experience ($streetClick.tab -eq '主城' -and $streetClick.cityPage -eq 'overview' -and $streetClick.cityExpanded -and -not $streetClick.cityBuilding -and $streetClick.battleWorldHash -eq $cityExpanded.battleWorldHash) 'clicking a visible scenic house neither invents a facility nor changes the campaign'
    Assert-Experience (@($streetClick.targets | Where-Object {$_.id -match '^city-building-[0-8]$'}).Count -eq 1 -and @($streetClick.targets | Where-Object {$_.id -match '^city-site-[0-8]$'}).Count -eq 0) 'new city offers only its real named hall and no scenic house or hidden site targets'
    Assert-ExperienceMapStill $cityExpanded $streetClick 'city street scenery click'
    $cityExpanded=$streetClick
    # Source-art points on an empty district's tree canopy, river and actual gate/wall masonry.
    # Use the actual town transform and require visibility; no invisible review targets.
    foreach($sceneryPoint in @(@{name='garden-tree';x=455;y=90},@{name='river-water';x=190;y=381},@{name='gate-arch';x=520;y=405},@{name='wall-stones';x=260;y=291},@{name='paved-avenue';x=500;y=315})) {
        $sceneryX=$cityExpanded.cityPanoramaImagePixelX+$cityExpanded.cityPanoramaImagePixelWidth*$sceneryPoint.x/1000
        $sceneryY=$cityExpanded.cityPanoramaImagePixelY+$cityExpanded.cityPanoramaImagePixelHeight*$sceneryPoint.y/440
        $sceneryViewport=Get-ExperienceTarget $cityExpanded 'city-panorama-viewport'
        Assert-Experience ($sceneryX -gt $sceneryViewport.x-$sceneryViewport.w/2 -and $sceneryX -lt $sceneryViewport.x+$sceneryViewport.w/2 -and $sceneryY -gt $sceneryViewport.y-$sceneryViewport.h/2 -and $sceneryY -lt $sceneryViewport.y+$sceneryViewport.h/2) ('real '+$sceneryPoint.name+' artwork is visible before clicking')
        Click-Experience $sceneryX $sceneryY;$sceneryClick=Capture-Experience ('04p-city-'+$sceneryPoint.name)
        Assert-Experience ($sceneryClick.tab -eq '主城' -and $sceneryClick.cityPage -eq 'overview' -and $sceneryClick.cityExpanded -and -not $sceneryClick.cityBuilding -and $sceneryClick.battleWorldHash -eq $cityExpanded.battleWorldHash) ('visible '+$sceneryPoint.name+' click does not open a fictitious facility or change World')
        Assert-Experience (@($sceneryClick.targets | Where-Object {$_.id -match '^city-building-[0-8]$'}).Count -eq 1 -and @($sceneryClick.targets | Where-Object {$_.id -match '^city-site-[0-8]$'}).Count -eq 0) 'decorative landscape, fortifications and paving add no facility or planning targets'
        Assert-ExperienceMapStill $cityExpanded $sceneryClick ('city '+$sceneryPoint.name+' click')
        $cityExpanded=$sceneryClick
    }
    $cityZoomed=$cityExpanded
    for($cameraStep=0;$cameraStep -lt 4;$cameraStep++) {Click-ExperienceTarget $cityZoomed 'city-panorama-plus';$cityZoomed=Capture-Experience ('04p-city-zoom-'+$cameraStep)}
    Assert-Experience ($cityZoomed.cityPanoramaZoom -eq 2 -and $cityZoomed.cityPanoramaImageHeight -gt $cityExpanded.cityPanoramaImageHeight -and $cityZoomed.battleWorldHash -eq $cameraOrigin.battleWorldHash) 'four real zoom clicks magnify the same city without changing campaign state'
    Assert-ExperienceCityLandscape $cityZoomed
    $cameraHall=Get-ExperienceTarget $cityZoomed 'city-building-4'
    Drag-Experience $cameraHall.x $cameraHall.y ($cameraHall.x+64) ($cameraHall.y+30)
    $cityPanned=Capture-Experience '04p-city-dragged'
    Assert-Experience ($cityPanned.cityPage -eq 'overview' -and $cityPanned.cityExpanded -and ([Math]::Abs($cityPanned.cityPanoramaPanX-$cityZoomed.cityPanoramaPanX) -gt 10 -or [Math]::Abs($cityPanned.cityPanoramaPanY-$cityZoomed.cityPanoramaPanY) -gt 10) -and $cityPanned.battleWorldHash -eq $cameraOrigin.battleWorldHash) 'drag starting on the actual hall pans the city without opening facilities or paying'
    Assert-ExperienceCityLandscape $cityPanned
    Assert-ExperienceMapStill $cityZoomed $cityPanned 'city camera drag'
    Click-ExperienceTarget $cityPanned 'city-building-4';$cameraFacility=Capture-Experience '04p-city-hall-detail'
    Assert-Experience ($cameraFacility.cityPage -eq 'facilities' -and $cameraFacility.cityPlot -eq 4 -and $cameraFacility.battleWorldHash -eq $cameraOrigin.battleWorldHash) 'the transformed hall hotspot opens the real facility without upgrading'
    Click-ExperienceTarget $cameraFacility 'city-back';$cameraFacilityBack=Capture-Experience '04p-city-hall-return'
    Assert-Experience ($cameraFacilityBack.cityExpanded -and $cameraFacilityBack.cityPanoramaZoom -eq $cityPanned.cityPanoramaZoom -and [Math]::Abs($cameraFacilityBack.cityPanoramaPanX-$cityPanned.cityPanoramaPanX) -lt .1 -and [Math]::Abs($cameraFacilityBack.cityPanoramaPanY-$cityPanned.cityPanoramaPanY) -lt .1 -and $cameraFacilityBack.battleWorldHash -eq $cameraOrigin.battleWorldHash) 'facility back preserves the expanded camera and campaign'
    Click-ExperienceTarget $cameraFacilityBack 'city-planning';$cameraSites=Capture-Experience '04p-city-zoomed-sites'
    $cameraViewport=Get-ExperienceTarget $cameraSites 'city-panorama-viewport'
    foreach($cameraTarget in @($cameraSites.targets | Where-Object {$_.id -match '^city-(site|building)-[0-8]$'})) {
        Assert-Experience ($cameraTarget.x-$cameraTarget.w/2 -ge $cameraViewport.x-$cameraViewport.w/2-1 -and $cameraTarget.x+$cameraTarget.w/2 -le $cameraViewport.x+$cameraViewport.w/2+1 -and $cameraTarget.y-$cameraTarget.h/2 -ge $cameraViewport.y-$cameraViewport.h/2-1 -and $cameraTarget.y+$cameraTarget.h/2 -le $cameraViewport.y+$cameraViewport.h/2+1) ('zoomed city target is completely visible: '+$cameraTarget.id)
    }
    Click-ExperienceTarget $cameraSites 'city-panorama-reset';$cameraReset=Capture-Experience '04p-city-reset'
    Assert-Experience ($cameraReset.cityExpanded -and $cameraReset.cityPlanning -and $cameraReset.cityPanoramaZoom -eq 1 -and $cameraReset.cityPanoramaPanX -eq 0 -and $cameraReset.cityPanoramaPanY -eq 0 -and @($cameraReset.citySiteIds).Count -eq $cameraReset.cityAvailableSites -and $cameraReset.battleWorldHash -eq $cameraOrigin.battleWorldHash) 'reset reveals every actual construction site and retains planning without charges'
    $cameraSite=[int]$cameraReset.citySiteIds[0]
    Click-ExperienceTarget $cameraReset ('city-site-'+$cameraSite);$cameraSiteDetail=Capture-Experience '04p-expanded-site-detail'
    Assert-Experience ($cameraSiteDetail.cityPage -eq 'facilities' -and $cameraSiteDetail.cityPlot -eq $cameraSite -and -not $cameraSiteDetail.cityBuilding -and $cameraSiteDetail.battleWorldHash -eq $cameraOrigin.battleWorldHash) 'expanded planning selects the actual visible district without starting construction'
    Click-ExperienceTarget $cameraSiteDetail 'city-back';$cameraSiteBack=Capture-Experience '04p-expanded-site-return'
    Assert-Experience ($cameraSiteBack.cityExpanded -and $cameraSiteBack.cityPlanning) 'site detail returns to expanded planning'
    Click-ExperienceTarget $cameraSiteBack 'city-back';$cameraPlanningBack=Capture-Experience '04p-planning-close'
    Assert-Experience ($cameraPlanningBack.cityExpanded -and -not $cameraPlanningBack.cityPlanning -and $cameraPlanningBack.tab -eq '主城') 'back closes planning before expanded city'
    Click-ExperienceTarget $cameraPlanningBack 'city-back';$panoramaReturn=Capture-Experience '04p-expanded-close'
    Assert-Experience (-not $panoramaReturn.cityExpanded -and $panoramaReturn.cityPage -eq 'overview' -and $panoramaReturn.tab -eq '主城' -and $panoramaReturn.battleWorldHash -eq $cameraOrigin.battleWorldHash) 'back restores compact city before leaving for the world map'
    Assert-ExperienceCityPanoramaRestored $cameraOrigin $panoramaReturn
    Click-ExperienceTarget $panoramaReturn 'city-facilities';$facilities=Capture-Experience '04f-city-facilities'
    Assert-ExperienceCityPage $facilities 'facilities'
    Assert-Experience (@($facilities.targets | Where-Object {$_.id -match '^city-slot-[0-8]$'}).Count -eq 9) 'all nine city plots are visible and selectable'
    $cityPoint=Get-ExperienceTarget $facilities 'city-slot-8'
    Drag-Experience $cityPoint.x $cityPoint.y ($cityPoint.x+35) ($cityPoint.y+25)
    $cityAfterDrag=Capture-Experience '05-city-pointer-block';Assert-ExperienceMapStill $facilities $cityAfterDrag 'city facilities'
    Click-ExperienceTarget $cityAfterDrag 'city-back';$cityReturned=Capture-Experience '05a-facilities-return'
    Assert-ExperienceCityPage $cityReturned 'overview'

    $cityReturned=Set-ExperienceSpeed 0 '05b-catalog-paused'
    Click-ExperienceTarget $cityReturned 'city-overview-facilities';$facilityCatalog=Capture-Experience '05c-facility-catalog'
    Assert-ExperienceCityPage $facilityCatalog 'catalog'
    Assert-Experience ($facilityCatalog.cityCatalogCategory -eq 0 -and $facilityCatalog.battleWorldHash -eq $cityReturned.battleWorldHash) 'facility catalog opens all real facilities without spending or advancing a task'
    Assert-ExperienceFacilityAtlas $facilityCatalog
    Click-ExperienceTarget $facilityCatalog 'city-catalog-image-2';$atlasImageLocation=Capture-Experience '05c-atlas-image-mill'
    Assert-Experience ($atlasImageLocation.cityPage -eq 'facilities' -and $atlasImageLocation.cityPlot -eq 0 -and $atlasImageLocation.cityFacility -eq 2 -and $atlasImageLocation.cityFacilitiesOrigin -eq 'catalog' -and $atlasImageLocation.battleWorldHash -eq $facilityCatalog.battleWorldHash) 'actual large mill image reaches its real empty plot without construction or spending'
    Click-ExperienceTarget $atlasImageLocation 'city-back';$atlasImageReturn=Capture-Experience '05c-atlas-image-return'
    Assert-Experience ($atlasImageReturn.cityPage -eq 'catalog' -and $atlasImageReturn.cityCatalogCategory -eq 0 -and $atlasImageReturn.cityCatalogScrollY -eq $facilityCatalog.cityCatalogScrollY -and $atlasImageReturn.battleWorldHash -eq $facilityCatalog.battleWorldHash) 'large image navigation returns to the same atlas category and position'
    Assert-ExperienceFacilityAtlas $atlasImageReturn
    Click-ExperienceTarget $atlasImageReturn 'city-catalog-name-2';$atlasNameLocation=Capture-Experience '05c-atlas-name-mill'
    Assert-Experience ($atlasNameLocation.cityPage -eq 'facilities' -and $atlasNameLocation.cityPlot -eq 0 -and $atlasNameLocation.cityFacility -eq 2 -and $atlasNameLocation.cityFacilitiesOrigin -eq 'catalog' -and $atlasNameLocation.battleWorldHash -eq $facilityCatalog.battleWorldHash) 'visible facility name reaches the same real mill location without construction or spending'
    Click-ExperienceTarget $atlasNameLocation 'city-back';$atlasNameReturn=Capture-Experience '05c-atlas-name-return'
    Assert-Experience ($atlasNameReturn.cityPage -eq 'catalog' -and $atlasNameReturn.cityCatalogCategory -eq 0 -and $atlasNameReturn.cityCatalogScrollY -eq $facilityCatalog.cityCatalogScrollY -and $atlasNameReturn.battleWorldHash -eq $facilityCatalog.battleWorldHash) 'name navigation returns to the exact atlas category and scroll position'
    Assert-ExperienceFacilityAtlas $atlasNameReturn
    $facilityCatalog=$atlasNameReturn
    $catalogViewport=Get-ExperienceTarget $facilityCatalog 'city-catalog-scroll'
    $catalogScrolled=Find-ExperienceScrolledTarget $facilityCatalog 'city-catalog-go-9' '05d-catalog-scroll-workshop' $catalogViewport.x $catalogViewport.y 7 2
    Assert-ExperienceFacilityAtlas $catalogScrolled
    Assert-Experience ($catalogScrolled.cityCatalogScrollY -gt $facilityCatalog.cityCatalogScrollY -and $catalogScrolled.battleWorldHash -eq $facilityCatalog.battleWorldHash) 'real catalog scrolling reveals the complete lower workshop card without changing the campaign'
    Assert-ExperienceTargetInClient $catalogScrolled 'city-catalog-go-9'
    $catalogCard=Get-ExperienceTarget $catalogScrolled 'city-catalog-card-9'
    Assert-Experience (($catalogCard.y-$catalogCard.h*.5) -ge ($catalogViewport.y-$catalogViewport.h*.5-.01) -and ($catalogCard.y+$catalogCard.h*.5) -le ($catalogViewport.y+$catalogViewport.h*.5+.01)) 'catalog target is recorded only when its complete card is visible in the real scroll viewport'
    Click-ExperienceTarget $catalogScrolled 'city-catalog-go-9';$catalogPrerequisite=Capture-Experience '05e-catalog-workshop-prerequisite'
    Assert-Experience ($catalogPrerequisite.cityPage -eq 'facilities' -and $catalogPrerequisite.cityPlot -eq 4 -and $catalogPrerequisite.cityFacility -eq 1 -and $catalogPrerequisite.cityFacilitiesOrigin -eq 'catalog' -and $catalogPrerequisite.battleWorldHash -eq $facilityCatalog.battleWorldHash) 'locked workshop reaches the real town hall without purchasing it'
    Click-ExperienceTarget $catalogPrerequisite 'city-back';$catalogRestored=Capture-Experience '05f-catalog-return-position'
    Assert-Experience ($catalogRestored.cityPage -eq 'catalog' -and $catalogRestored.cityCatalogCategory -eq 0 -and [Math]::Abs($catalogRestored.cityCatalogScrollY-$catalogScrolled.cityCatalogScrollY) -lt .01 -and $catalogRestored.battleWorldHash -eq $facilityCatalog.battleWorldHash) 'facility back restores the actual catalog category and scroll position'
    Click-ExperienceTarget $catalogRestored 'city-catalog-category-3';$catalogMilitary=Capture-Experience '05g-catalog-military'
    Assert-Experience ($catalogMilitary.cityCatalogCategory -eq 3 -and $catalogMilitary.cityCatalogScrollY -eq 0 -and ($catalogMilitary.visibleFacilityIds -join ',') -eq '7,8') 'military filter displays the real barracks and drill ground cards'
    Assert-ExperienceFacilityAtlas $catalogMilitary
    Click-ExperienceTarget $catalogMilitary 'city-catalog-go-7';$catalogBarracks=Capture-Experience '05h-catalog-barracks-prerequisite'
    Assert-Experience ($catalogBarracks.cityPlot -eq 4 -and $catalogBarracks.cityFacility -eq 1 -and $catalogBarracks.battleWorldHash -eq $facilityCatalog.battleWorldHash) 'locked barracks reaches its prerequisite without charging'
    Click-ExperienceTarget $catalogBarracks 'city-back';$catalogMilitary=Capture-Experience '05i-catalog-military-return'
    Assert-Experience ($catalogMilitary.cityCatalogCategory -eq 3 -and $catalogMilitary.cityPage -eq 'catalog') 'barracks return preserves the military category'
    Click-ExperienceTarget $catalogMilitary 'city-catalog-category-2';$catalogEconomy=Capture-Experience '05j-catalog-economy'
    Assert-Experience ($catalogEconomy.cityCatalogCategory -eq 2 -and $catalogEconomy.visibleFacilityIds -contains 2 -and $catalogEconomy.visibleFacilityIds -contains 4 -and @($catalogEconomy.visibleFacilityIds | Where-Object {$_ -lt 2 -or $_ -gt 6}).Count -eq 0) 'resource filter shows the mill and quarry with only actual resource facilities'
    Assert-ExperienceFacilityAtlas $catalogEconomy
    Click-ExperienceTarget $catalogEconomy 'city-catalog-go-2';$catalogMill=Capture-Experience '05k-catalog-new-mill'
    Assert-Experience ($catalogMill.cityPage -eq 'facilities' -and $catalogMill.cityPlot -eq 0 -and $catalogMill.cityFacility -eq 2 -and -not $catalogMill.cityBuilding -and $catalogMill.battleWorldHash -eq $facilityCatalog.battleWorldHash) 'unbuilt mill reaches a real empty plot and waits for an explicit build action'
    Click-ExperienceTarget $catalogMill 'city-back';$catalogEconomyReturn=Capture-Experience '05l-catalog-economy-return'
    Click-ExperienceTarget $catalogEconomyReturn 'city-back';$cityReturned=Capture-Experience '05m-catalog-overview-return'
    Assert-ExperienceCityPage $cityReturned 'overview'
    Assert-Experience ($cityReturned.battleWorldHash -eq $facilityCatalog.battleWorldHash) 'complete catalog navigation leaves all campaign state unchanged'

    Click-ExperienceTarget $cityReturned 'city-quests';$quest=Capture-Experience '06-first-quest'
    Assert-ExperienceCityPage $quest 'quests'
    Assert-Experience ($quest.questReady -and $quest.claimedQuests -eq 0) 'visiting the actual nine-plot facilities completes first guide objective'
    Drag-Experience ($cw*.76) ($ch*.36) ($cw*.78) ($ch*.42)
    $questBlocked=Capture-Experience '06a-quests-pointer-block';Assert-ExperienceMapStill $quest $questBlocked 'independent chapter quests'
    Assert-Experience ($questBlocked.selected -eq $quest.selected) 'quest page drag cannot select underlying land'
    $questBlocked=Set-ExperienceSpeed 0 '06b-first-reward-paused'
    $quest=Find-ExperienceScrolledTarget $questBlocked 'quest-claim' 'first-quest-claim' ($cw*.80) ($ch*.57)
    Click-ExperienceTarget $quest 'quest-detail-1';$objectiveReady=Capture-Experience '06c-objective-ready'
    Assert-Experience ($objectiveReady.cityPage -eq 'quest-detail' -and $objectiveReady.questDetailId -eq 1 -and $objectiveReady.questDetailReady -and $objectiveReady.battleWorldHash -eq $quest.battleWorldHash) 'ready task opens its real objective without automatically claiming'
    $objectiveContent=Get-ExperienceTarget $objectiveReady 'quest-detail-content'
    Drag-Experience $objectiveContent.x $objectiveContent.y ($objectiveContent.x+20) ($objectiveContent.y-45)
    $objectiveBlocked=Capture-Experience '06d-objective-input-block'
    Assert-ExperienceMapStill $objectiveReady $objectiveBlocked 'independent objective detail'
    Assert-Experience ($objectiveBlocked.selected -eq $objectiveReady.selected -and $objectiveBlocked.battleWorldHash -eq $objectiveReady.battleWorldHash) 'objective drag neither selects map land nor grants a reward'
    Click-ExperienceTarget $objectiveBlocked 'city-back';$objectiveList=Capture-Experience '06e-objective-list-return'
    Assert-Experience ($objectiveList.cityPage -eq 'quests' -and $objectiveList.questChapter -eq $quest.questChapter -and [Math]::Abs($objectiveList.questScrollY-$quest.questScrollY) -lt .001 -and $objectiveList.battleWorldHash -eq $quest.battleWorldHash) 'objective return restores the original chapter and visible scroll position'
    $quest=$objectiveList
    $receiptUnderlyingFacilities=Get-ExperienceTarget $quest 'city-facilities'
    $claim=Get-ExperienceTarget $quest 'quest-claim';Click-Experience $claim.x $claim.y
    $claimed=Capture-Experience '07-first-reward';Assert-Experience ($claimed.claimedQuests -eq 1) 'first guide reward claimed once'
    Assert-ExperienceQuestReceipt $quest $claimed 1 2 @(300,250,250,100,100)
    Click-Experience $claim.x $claim.y;$repeat=Capture-Experience '08-first-reward-repeat'
    Assert-Experience ($repeat.claimedQuests -eq 1) 'repeated reward location does not claim unfinished next task'
    Assert-Experience ($repeat.receiptOpen -and $repeat.receiptId -eq 1 -and $repeat.battleWorldHash -eq $claimed.battleWorldHash) 'repeated original claim coordinate preserves the open receipt and complete campaign JSON'
    Click-Experience $receiptUnderlyingFacilities.x $receiptUnderlyingFacilities.y;$receiptUnderlying=Capture-Experience '08-reward-underlying-navigation'
    Assert-Experience ($receiptUnderlying.receiptOpen -and $receiptUnderlying.cityPage -eq 'quests' -and $receiptUnderlying.battleWorldHash -eq $claimed.battleWorldHash) 'receipt blocks the captured real underlying facility navigation coordinate'
    $receiptPanel=Get-ExperienceTarget $receiptUnderlying 'quest-reward-modal'
    Drag-Experience ($receiptPanel.x-$receiptPanel.w*.20) $receiptPanel.y ($receiptPanel.x-$receiptPanel.w*.15) ($receiptPanel.y+12)
    $receiptDrag=Capture-Experience '08-reward-map-block';Assert-ExperienceMapStill $claimed $receiptDrag 'quest reward receipt'
    Assert-Experience ($receiptDrag.receiptOpen -and $receiptDrag.selected -eq $claimed.selected -and $receiptDrag.battleWorldHash -eq $claimed.battleWorldHash) 'receipt drag changes neither map selection nor claimed campaign state'
    Focus-Experience;Move-Experience $receiptPanel.x $receiptPanel.y;Assert-ExperiencePointer $receiptPanel.x $receiptPanel.y
    [EDExperiencePointer]::mouse_event(0x0800,0,0,[uint32](4294967296-480),[UIntPtr]::Zero)
    Start-Sleep -Milliseconds 200;Assert-ExperiencePointer $receiptPanel.x $receiptPanel.y
    $receiptWheel=Capture-Experience '08-reward-underlying-scroll'
    Assert-Experience ($receiptWheel.receiptOpen -and [Math]::Abs($receiptWheel.questScrollY-$receiptDrag.questScrollY) -lt .001 -and $receiptWheel.battleWorldHash -eq $claimed.battleWorldHash) 'receipt blocks actual mouse wheel scrolling of the underlying quest list and preserves campaign JSON'
    Click-ExperienceTarget $receiptWheel 'quest-reward-next';$receiptNext=Capture-Experience '08-reward-next-mill'
    Assert-ExperienceCityPage $receiptNext 'facilities'
    Assert-Experience (-not $receiptNext.receiptOpen -and $receiptNext.cityPlot -eq 0 -and $receiptNext.cityFacility -eq 2 -and -not $receiptNext.cityBuilding -and $receiptNext.battleWorldHash -eq $claimed.battleWorldHash) 'receipt next button reaches the actual buildable mill plot without a second reward or construction charge'
    Click-ExperienceTarget $receiptNext 'city-quests';$receiptQuestReturn=Capture-Experience '08-reward-return-to-tasks'
    Assert-Experience ($receiptQuestReturn.questCurrentId -eq 2 -and -not $receiptQuestReturn.questCompleted -and $receiptQuestReturn.battleWorldHash -eq $claimed.battleWorldHash) 'return from receipt target retains the actual next task and credited campaign'
    Click-ExperienceTarget $receiptQuestReturn 'quest-completed';$completed=Capture-Experience '08a-completed-quests'
    Assert-Experience ($completed.questCompleted -and @($completed.visibleQuestIds).Count -eq 1 -and $completed.visibleQuestIds[0] -eq 1 -and $completed.claimedQuests -eq 1) 'completed filter shows only the actually claimed first quest'
    Assert-Experience (@($completed.targets | Where-Object {$_.id -in @('quest-go','quest-claim')}).Count -eq 0) 'completed tasks expose no claim or progression action'
    Click-ExperienceTarget $completed 'quest-detail-1';$objectiveClaimed=Capture-Experience '08a-objective-claimed'
    Assert-Experience ($objectiveClaimed.questDetailClaimed -and -not $objectiveClaimed.questDetailCurrent -and $objectiveClaimed.battleWorldHash -eq $completed.battleWorldHash) 'claimed task detail preserves credited resources and has no progression command'
    Click-ExperienceTarget $objectiveClaimed 'quest-detail-action';$objectiveCompletedReturn=Capture-Experience '08a-objective-claimed-return'
    Assert-Experience ($objectiveCompletedReturn.cityPage -eq 'quests' -and $objectiveCompletedReturn.questCompleted -and $objectiveCompletedReturn.battleWorldHash -eq $completed.battleWorldHash) 'claimed detail action returns to its completed filter without another reward'
    $completed=$objectiveCompletedReturn
    Click-ExperienceTarget $completed 'quest-chapter-1';$chapterTwo=Capture-Experience '08b-second-chapter'
    Assert-Experience ($chapterTwo.questChapter -eq 1 -and -not $chapterTwo.questCompleted -and @($chapterTwo.visibleQuestIds).Count -gt 0 -and @($chapterTwo.visibleQuestIds | Where-Object {$_ -lt 4 -or $_ -gt 7}).Count -eq 0) 'second chapter displays its actual stable quest IDs and leaves completed filter'
    Assert-Experience (@($chapterTwo.targets | Where-Object {$_.id -in @('quest-go','quest-claim')}).Count -eq 0 -and $chapterTwo.claimedQuests -eq 1) 'locked future chapter cannot skip quest order or claim rewards'
    Click-ExperienceTarget $chapterTwo 'quest-detail-4';$objectiveLocked=Capture-Experience '08b-objective-locked'
    Assert-Experience ($objectiveLocked.questDetailId -eq 4 -and -not $objectiveLocked.questDetailCurrent -and -not $objectiveLocked.questDetailReady -and $objectiveLocked.battleWorldHash -eq $chapterTwo.battleWorldHash) 'future objective remains locked even when formation is already sufficient'
    Click-ExperienceTarget $objectiveLocked 'quest-detail-action';$objectiveCurrent=Capture-Experience '08b-objective-locate-current'
    Assert-Experience ($objectiveCurrent.questDetailId -eq 2 -and $objectiveCurrent.questDetailCurrent -and @($objectiveCurrent.questDetailCost).Count -eq 5 -and $objectiveCurrent.battleWorldHash -eq $chapterTwo.battleWorldHash) 'future detail locates the actual current mill objective and previews its resource cost'
    Click-ExperienceTarget $objectiveCurrent 'quest-detail-action';$objectiveMill=Capture-Experience '08b-objective-mill-location'
    Assert-Experience ($objectiveMill.cityPage -eq 'facilities' -and $objectiveMill.cityFacilitiesOrigin -eq 'quest-detail' -and $objectiveMill.cityPlot -eq $objectiveCurrent.questDetailRoutePlot -and $objectiveMill.cityFacility -eq 2 -and -not $objectiveMill.cityBuilding -and $objectiveMill.battleWorldHash -eq $chapterTwo.battleWorldHash) 'objective action reaches the quoted real mill plot and requires separate construction approval'
    Click-ExperienceTarget $objectiveMill 'city-back';$objectiveMillBack=Capture-Experience '08b-objective-mill-return'
    Assert-Experience ($objectiveMillBack.cityPage -eq 'quest-detail' -and $objectiveMillBack.questDetailId -eq 2) 'facility back restores the exact objective detail'
    Click-ExperienceTarget $objectiveMillBack 'city-back';$objectiveChapterBack=Capture-Experience '08b-objective-chapter-return'
    Assert-Experience ($objectiveChapterBack.cityPage -eq 'quests' -and $objectiveChapterBack.questChapter -eq 1 -and [Math]::Abs($objectiveChapterBack.questScrollY-$chapterTwo.questScrollY) -lt .001 -and $objectiveChapterBack.battleWorldHash -eq $chapterTwo.battleWorldHash) 'two-level objective return preserves the original future chapter without spending'
    $chapterTwo=$objectiveChapterBack
    Click-ExperienceTarget $chapterTwo 'quest-chapter-2';$chapterThree=Capture-Experience '08c-third-chapter'
    Assert-Experience ($chapterThree.questChapter -eq 2 -and @($chapterThree.visibleQuestIds).Count -gt 0 -and @($chapterThree.visibleQuestIds | Where-Object {$_ -lt 8 -or $_ -gt 10}).Count -eq 0 -and $chapterThree.claimedQuests -eq 1) 'third chapter displays remaining stable IDs without granting rewards'
    Click-ExperienceTarget $chapterThree 'quest-focus-current';$focusedQuest=Capture-Experience '08c-focus-current'
    Assert-Experience ($focusedQuest.questChapter -eq 0 -and -not $focusedQuest.questCompleted -and $focusedQuest.questCurrentId -eq 2 -and 2 -in $focusedQuest.visibleQuestIds -and $focusedQuest.claimedQuests -eq 1) 'visible focus button returns from future chapter to the fully visible current task without granting rewards'
    Click-ExperienceTarget $focusedQuest 'quest-completed';$focusCompleted=Capture-Experience '08c-focus-completed'
    Click-ExperienceTarget $focusCompleted 'quest-focus-current';$focusedQuest=Capture-Experience '08c-focus-from-completed'
    Assert-Experience ($focusedQuest.questChapter -eq 0 -and -not $focusedQuest.questCompleted -and 2 -in $focusedQuest.visibleQuestIds -and $focusedQuest.claimedQuests -eq 1) 'current task remains reachable from completed reward history'
    Click-ExperienceTarget $focusedQuest 'quest-active';$activeQuests=Capture-Experience '08d-active-quests'
    Assert-Experience (-not $activeQuests.questCompleted) 'main affairs filter returns to active chapter navigation'
    Click-ExperienceTarget $activeQuests 'quest-chapter-0';$firstChapter=Capture-Experience '08e-first-chapter'
    Assert-Experience ($firstChapter.questChapter -eq 0 -and @($firstChapter.visibleQuestIds).Count -gt 0 -and @($firstChapter.visibleQuestIds | Where-Object {$_ -lt 1 -or $_ -gt 3}).Count -eq 0) 'first chapter preserves its original three quest IDs'
    $firstChapter=Find-ExperienceScrolledTarget $firstChapter 'quest-go' 'mill-quest-go' ($cw*.80) ($ch*.57)
    Click-ExperienceTarget $firstChapter 'quest-go';$guidedPlot=Capture-Experience '08f-quest-to-facilities'
    Assert-ExperienceCityPage $guidedPlot 'facilities'
    Assert-Experience ($guidedPlot.cityPlot -eq 0 -and -not $guidedPlot.cityBuilding) 'mill objective navigates to an actual empty construction plot'
    Click-ExperienceTarget $guidedPlot 'city-catalog';$catalogMapOrigin=Capture-Experience '08g-catalog-map-origin'
    Click-ExperienceTarget $catalogMapOrigin 'city-catalog-category-2';$catalogMapOrigin=Capture-Experience '08h-catalog-map-resource'
    Click-ExperienceTarget $catalogMapOrigin 'city-catalog-go-2';$catalogMapDetail=Capture-Experience '08i-catalog-map-detail'
    Click-ExperienceTarget $catalogMapDetail 'map-tab';$catalogMap=Capture-Experience '08j-catalog-to-map'
    Assert-Experience ($catalogMap.tab -eq '领地' -and $catalogMap.battleWorldHash -eq $guidedPlot.battleWorldHash) 'leaving catalog details through the bottom map tab preserves the campaign'
    Assert-ExperienceMapQuest $catalogMap
    Click-ExperienceTarget $catalogMap 'quest-go';$catalogMapQuest=Capture-Experience '08k-map-quest-facility'
    Assert-Experience ($catalogMapQuest.cityPage -eq 'facilities' -and $catalogMapQuest.cityFacilitiesOrigin -eq 'overview' -and $catalogMapQuest.cityPlot -eq 0 -and $catalogMapQuest.cityFacility -eq 2 -and $catalogMapQuest.battleWorldHash -eq $guidedPlot.battleWorldHash) 'map task navigation clears the stale catalog origin and reaches its actual mill plot'
    Click-ExperienceTarget $catalogMapQuest 'city-back';$catalogMapBack=Capture-Experience '08l-map-quest-return'
    Assert-ExperienceCityPage $catalogMapBack 'overview'
    Click-ExperienceTarget $catalogMapBack 'city-facilities';$guidedPlot=Capture-Experience '08m-resume-district'
    Click-ExperienceTarget $guidedPlot 'city-slot-0';$plot=Capture-Experience '09-empty-plot'
    Assert-Experience ($plot.cityPlot -eq 0 -and -not $plot.cityBuilding) 'empty district slot selected'
    $plot=Set-ExperienceSpeed 0 '09a-facility-paused'
    Assert-ExperienceFacilityDossier $plot
    Click-ExperienceTarget $plot 'city-facility-image-4';$quarryImage=Capture-Experience '09a-facility-quarry-image'
    Assert-ExperienceFacilityDossier $quarryImage
    Assert-Experience ($quarryImage.cityFacility -eq 4 -and $quarryImage.battleWorldHash -eq $plot.battleWorldHash) 'clicking the quarry image changes the actual preview without purchasing'
    Click-ExperienceTarget $quarryImage 'city-facility-title-2';$millName=Capture-Experience '09a-facility-mill-name'
    Assert-ExperienceFacilityDossier $millName
    Assert-Experience ($millName.cityFacility -eq 2 -and $millName.battleWorldHash -eq $plot.battleWorldHash) 'clicking the mill name selects the same image-and-ledger preview without purchasing'
    $plot=$millName
    foreach($selection in @(@('city-facility-image-3',3),@('city-facility-title-9',9),@('city-facility-image-9',9),@('city-facility-title-2',2))) {
        Click-ExperienceTarget $plot $selection[0];$selectedGallery=Capture-Experience ('09a-gallery-'+$selection[0])
        Assert-ExperienceFacilityDossier $selectedGallery
        Assert-Experience ($selectedGallery.cityFacility -eq $selection[1] -and $selectedGallery.battleWorldHash -eq $millName.battleWorldHash -and -not $selectedGallery.cityBuilding) 'facility image/title selection updates the actual preview without starting construction or changing World'
        $plot=$selectedGallery
    }
    Click-ExperienceTarget $plot 'city-option-7';$lockedFacility=Capture-Experience '09b-locked-barracks'
    Assert-ExperienceFacilityDossier $lockedFacility
    Assert-Experience ($lockedFacility.cityFacility -eq 7 -and $lockedFacility.cityRequiredHall -eq 2 -and $lockedFacility.cityCurrentEffect -eq 0 -and $lockedFacility.cityNextEffect -eq 250) 'actual barracks selection shows its locked prerequisite and real troop-cap preview'
    Assert-Experience ($lockedFacility.battleWorldHash -eq $plot.battleWorldHash -and -not $lockedFacility.cityBuilding) 'facility selection does not spend resources or create a building'
    Assert-Experience (@($lockedFacility.targets | Where-Object {$_.id -eq 'city-build'}).Count -eq 0) 'locked facility exposes no enabled construction target'
    Click-ExperienceTarget $lockedFacility 'city-prerequisite';$facilityHall=Capture-Experience '09c-facility-prerequisite'
    Assert-Experience ($facilityHall.cityPlot -eq 4 -and $facilityHall.cityFacility -eq 1 -and $facilityHall.battleWorldHash -eq $plot.battleWorldHash) 'prerequisite button reaches the actual town hall without constructing or charging'
    Click-ExperienceTarget $facilityHall 'city-slot-0';$plot=Capture-Experience '09d-facility-mill'
    Click-ExperienceTarget $plot 'city-option-2';$plot=Capture-Experience '09e-mill-preview'
    Assert-Experience ($plot.cityCurrentEffect -eq 0 -and $plot.cityNextEffect -eq 160 -and $plot.cityRequiredHall -eq 1) 'mill preview shows the exact production gained on completion'
    $facilityScroll=Get-ExperienceTarget $plot 'city-detail-scroll'
    Focus-Experience;Move-Experience $facilityScroll.x $facilityScroll.y;Assert-ExperiencePointer $facilityScroll.x $facilityScroll.y
    [EDExperiencePointer]::mouse_event(0x0800,0,0,[uint32](4294967296-480),[UIntPtr]::Zero)
    Start-Sleep -Milliseconds 200;Assert-ExperiencePointer $facilityScroll.x $facilityScroll.y
    $scrolledFacility=Capture-Experience '09f-facility-cost-scroll'
    Assert-Experience ($scrolledFacility.cityDetailScrollY -gt $plot.cityDetailScrollY -and $scrolledFacility.cityFacility -eq 2 -and $scrolledFacility.battleWorldHash -eq $plot.battleWorldHash) 'real detail scrolling reveals lower cost and time rows without selecting or purchasing a facility'
    $plot=$scrolledFacility
    Click-ExperienceTarget $plot 'city-catalog';$millCatalog=Capture-Experience '09g-mill-catalog-route'
    Click-ExperienceTarget $millCatalog 'city-catalog-category-2';$millCatalog=Capture-Experience '09h-mill-catalog-selected'
    Click-ExperienceTarget $millCatalog 'city-catalog-go-2';$plot=Capture-Experience '09i-mill-catalog-build-preview'
    Assert-Experience ($plot.cityPage -eq 'facilities' -and $plot.cityPlot -eq 0 -and $plot.cityFacility -eq 2 -and $plot.battleWorldHash -eq $scrolledFacility.battleWorldHash) 'catalog build path reaches the actual mill preview before explicit payment'
    $plot=Find-ExperienceScrolledTarget $plot 'city-build' 'mill-build-button' ($cw*.82) ($ch*.60)
    $buildCommand=Get-ExperienceTarget $plot 'city-build'
    Click-Experience ($buildCommand.x-$buildCommand.w*.30) $buildCommand.y
    $queued=Capture-Experience '10-mill-building'
    Assert-ExperienceFacilityDossier $queued
    Assert-Experience $queued.cityBuilding 'actual build button starts construction queue'
    Assert-Experience ($queued.cityFacility -eq 2 -and $queued.cityConstructionPlot -eq 0) 'new construction detail shows the actual mill identity'
    Click-ExperienceTarget $queued 'city-back';$catalogWorking=Capture-Experience '10-catalog-working'
    Assert-Experience ($catalogWorking.cityPage -eq 'catalog' -and $catalogWorking.cityCatalogCategory -eq 2 -and $catalogWorking.cityBuilding -and $catalogWorking.battleWorldHash -eq $queued.battleWorldHash) 'starting construction through the catalog returns to the same live category'
    Click-ExperienceTarget $catalogWorking 'city-catalog-go-4';$catalogQueue=Capture-Experience '10-catalog-busy-route'
    Assert-Experience ($catalogQueue.cityPage -eq 'facilities' -and $catalogQueue.cityPlot -eq 0 -and $catalogQueue.cityFacility -eq 2 -and $catalogQueue.battleWorldHash -eq $queued.battleWorldHash) 'another requested facility routes to the real busy construction without a second charge'
    $queued=$catalogQueue
    Click-ExperienceTarget $queued 'city-quests';$queuedQuest=Capture-Experience '10-queued-task'
    $queuedQuest=Find-ExperienceScrolledTarget $queuedQuest 'quest-go' 'queued-quest-go' ($cw*.80) ($ch*.57)
    Click-ExperienceTarget $queuedQuest 'quest-detail-2';$objectiveWorking=Capture-Experience '10-objective-working'
    Assert-Experience ($objectiveWorking.questDetailCurrent -and -not $objectiveWorking.questDetailReady -and $objectiveWorking.questDetailRoutePlot -eq 0 -and $objectiveWorking.battleWorldHash -eq $queuedQuest.battleWorldHash) 'working objective identifies the actual current construction without new charges'
    Click-ExperienceTarget $objectiveWorking 'quest-detail-action';$objectiveWorkingPlot=Capture-Experience '10-objective-working-plot'
    Assert-Experience ($objectiveWorkingPlot.cityPage -eq 'facilities' -and $objectiveWorkingPlot.cityPlot -eq 0 -and $objectiveWorkingPlot.cityBuilding -and $objectiveWorkingPlot.battleWorldHash -eq $queuedQuest.battleWorldHash) 'working objective action opens its real construction controls'
    Click-ExperienceTarget $objectiveWorkingPlot 'city-back';$objectiveWorkingBack=Capture-Experience '10-objective-working-back'
    Click-ExperienceTarget $objectiveWorkingBack 'city-back';$queuedQuest=Capture-Experience '10-objective-working-list'
    Click-ExperienceTarget $queuedQuest 'quest-go';$guidedQueue=Capture-Experience '10-guided-queue'
    Assert-Experience ($guidedQueue.cityPage -eq 'facilities' -and $guidedQueue.cityPlot -eq 0 -and $guidedQueue.cityFacility -eq 2 -and $guidedQueue.cityBuilding -and $guidedQueue.battleWorldHash -eq $queued.battleWorldHash) 'task go reaches actual ongoing construction without duplicate charge or a new empty plot'
    $queued=$guidedQueue
    Click-ExperienceTarget $queued 'city-slot-1';$otherPlot=Capture-Experience '10a-other-plot'
    Click-ExperienceTarget $otherPlot 'city-queue-locate';$queued=Capture-Experience '10b-queue-location'
    Assert-Experience ($queued.cityPlot -eq 0 -and $queued.cityFacility -eq 2 -and $queued.cityConstructionPlot -eq 0 -and $queued.battleWorldHash -eq $otherPlot.battleWorldHash) 'queue entry returns to the actual construction without spending again'
    $cancelReady=Find-ExperienceScrolledTarget $queued 'city-cancel' 'mill-cancel-button' ($cw*.82) ($ch*.60) 2 8
    $cancelCommand=Get-ExperienceTarget $cancelReady 'city-cancel'
    Click-Experience ($cancelCommand.x+$cancelCommand.w*.30) $cancelCommand.y
    $cancelConfirm=Capture-Experience '10a-mill-cancel-confirm'
    Assert-Experience ([string]::IsNullOrEmpty($cancelConfirm.cityFacilityStyle) -and @($cancelConfirm.targets | Where-Object {$_.id -match '^city-(slot|option)-' -or $_.id -in @('city-detail-scroll','city-build','city-cancel')}).Count -eq 0) 'refund confirmation exposes no covered facility controls or false visible sheet'
    Assert-Experience ($cancelConfirm.confirmationOpen -and $cancelConfirm.cityRefundConfirmation -and $cancelConfirm.cityBuilding -and $cancelConfirm.battleWorldHash -eq $cancelReady.battleWorldHash) 'visible cancellation previews the five actual paid resources without changing the job'
    Click-ExperienceTarget $cancelConfirm 'confirm-no';$continueBuild=Capture-Experience '10a-mill-continue'
    Assert-Experience (-not $continueBuild.confirmationOpen -and $continueBuild.cityBuilding -and $continueBuild.battleWorldHash -eq $cancelReady.battleWorldHash) 'continue construction dismisses refund preview without refund or cancellation'
    Click-ExperienceTarget $continueBuild 'city-cancel';$cancelConfirm=Capture-Experience '10a-mill-refund-reopen'
    Click-ExperienceTarget $cancelConfirm 'confirm-yes';$cancelledBuild=Capture-Experience '10b-mill-cancelled'
    Assert-Experience (-not $cancelledBuild.confirmationOpen -and -not $cancelledBuild.cityBuilding -and $cancelledBuild.cityMillLevel -eq 0 -and -not $cancelledBuild.questReady -and $cancelledBuild.claimedQuests -eq 1) 'confirmed cancellation removes the queue without building the mill or advancing its quest'
    for($resourceIndex=0;$resourceIndex -lt 5;$resourceIndex++) {
        Assert-Experience ([Math]::Abs($cancelledBuild.resources[$resourceIndex]-$cancelConfirm.resources[$resourceIndex]-$cancelConfirm.cityRefund[$resourceIndex]) -lt .00001) ('confirmed refund restores the displayed paid resource '+$resourceIndex+' exactly once')
    }
    Assert-Experience ($cancelledBuild.jade -eq $cancelConfirm.jade) 'construction refund does not create jade'
    $cancelledBuild=Find-ExperienceScrolledTarget $cancelledBuild 'city-build' 'mill-rebuild-button' ($cw*.82) ($ch*.60)
    Click-ExperienceTarget $cancelledBuild 'city-build';$queuedAgain=Capture-Experience '10c-mill-restarted'
    Assert-Experience ($queuedAgain.cityBuilding -and -not $queuedAgain.questReady -and $queuedAgain.claimedQuests -eq 1) 'cancelled empty plot can start a fresh real construction queue'
    Click-ExperienceTarget $queuedAgain 'city-back';$buildingOverview=Capture-Experience '10d-building-overview'
    Assert-ExperienceCityPage $buildingOverview 'overview'
    Assert-Experience $buildingOverview.cityBuilding 'returning to city overview preserves active construction'
    Assert-ExperienceCityOverview $buildingOverview
    Assert-ExperienceCityAffairsComposition $buildingOverview
    Click-ExperienceTarget $buildingOverview 'city-affairs-art';$cityWorkArt=Capture-Experience '10d-affairs-working-art'
    Assert-Experience ($cityWorkArt.cityPage -eq 'facilities' -and $cityWorkArt.cityPlot -eq $buildingOverview.cityAffairsActualPlot -and $cityWorkArt.cityFacility -eq $buildingOverview.cityAffairsDisplayedKind -and $cityWorkArt.cityBuilding -and $cityWorkArt.battleWorldHash -eq $buildingOverview.battleWorldHash) 'working affairs building art reaches the exact live construction without spending or starting another job'
    Click-ExperienceTarget $cityWorkArt 'city-back';$buildingOverview=Capture-Experience '10d-affairs-working-return'
    Assert-ExperienceCityAffairsComposition $buildingOverview
    Assert-Experience ($buildingOverview.battleWorldHash -eq $cityWorkArt.battleWorldHash) 'working affairs image returns to the unchanged live city overview'
    foreach($affairsPart in @('building','caption')) {
        $affairsOrigin=$buildingOverview
        Click-ExperienceTarget $buildingOverview ('city-affairs-'+$affairsPart);$affairsDestination=Capture-Experience ('10d-affairs-working-'+$affairsPart)
        Assert-Experience ($affairsDestination.cityPage -eq 'facilities' -and $affairsDestination.cityPlot -eq $affairsOrigin.cityAffairsActualPlot -and $affairsDestination.cityFacility -eq $affairsOrigin.cityAffairsDisplayedKind -and $affairsDestination.cityBuilding -and $affairsDestination.battleWorldHash -eq $affairsOrigin.battleWorldHash) ('working affairs '+$affairsPart+' opens the exact live queue without a second charge or completion')
        Click-ExperienceTarget $affairsDestination 'city-back';$buildingOverview=Capture-Experience ('10d-affairs-working-'+$affairsPart+'-return')
        Assert-ExperienceCityAffairsComposition $buildingOverview
        Assert-Experience ($buildingOverview.battleWorldHash -eq $affairsOrigin.battleWorldHash) ('working affairs '+$affairsPart+' return preserves the same queue and complete campaign')
        Assert-ExperienceMapStill $affairsOrigin $buildingOverview ('working city affairs '+$affairsPart+' navigation')
    }
    Click-ExperienceTarget $buildingOverview 'city-overview-facilities';$sideCurrentWork=Capture-Experience '10d-side-current-work'
    Assert-Experience ($sideCurrentWork.cityPage -eq 'facilities' -and $sideCurrentWork.cityPlot -eq $buildingOverview.cityConstructionPlot -and $sideCurrentWork.cityFacility -eq 2 -and $sideCurrentWork.cityBuilding -and $sideCurrentWork.battleWorldHash -eq $buildingOverview.battleWorldHash) 'city side manage action reaches the actual current construction instead of the idle facility catalog without a second charge'
    Click-ExperienceTarget $sideCurrentWork 'city-back';$buildingOverview=Capture-Experience '10d-side-current-work-return'
    Assert-ExperienceCityOverview $buildingOverview
    Assert-Experience ($buildingOverview.battleWorldHash -eq $sideCurrentWork.battleWorldHash) 'current construction side action returns to the unchanged city overview'
    Click-ExperienceTarget $buildingOverview 'city-construction-page';$workingProgress=Capture-Experience '10d-construction-progress'
    foreach($buttonId in @('city-construction-manage','city-construction-cancel')) {Assert-ExperienceTargetInClient $workingProgress $buttonId}
    Assert-Experience ($workingProgress.cityPage -eq 'construction' -and $workingProgress.cityBuilding -and -not $workingProgress.cityCompletionAvailable -and $workingProgress.battleWorldHash -eq $buildingOverview.battleWorldHash) 'progress page shows the actual restarted queue and cancellation has no completion receipt'
    Click-ExperienceTarget $workingProgress 'city-construction-cancel';$progressRefund=Capture-Experience '10d-construction-refund'
    Assert-Experience ($progressRefund.confirmationOpen -and $progressRefund.cityRefundConfirmation -and $progressRefund.battleWorldHash -eq $workingProgress.battleWorldHash) 'progress page cancellation opens the actual paid resource refund confirmation'
    Click-ExperienceTarget $progressRefund 'confirm-no';$workingProgress=Capture-Experience '10d-construction-continue'
    Click-ExperienceTarget $workingProgress 'city-construction-manage';$progressFacility=Capture-Experience '10d-construction-facility'
    Assert-Experience ($progressFacility.cityPage -eq 'facilities' -and $progressFacility.cityPlot -eq 0 -and $progressFacility.battleWorldHash -eq $workingProgress.battleWorldHash) 'progress manage enters the real active facility without a second charge'
    Click-ExperienceTarget $progressFacility 'city-back';$workingProgress=Capture-Experience '10d-construction-facility-back'
    Assert-Experience ($workingProgress.cityPage -eq 'construction' -and $workingProgress.battleWorldHash -eq $progressFacility.battleWorldHash) 'facility back retains construction progress parent'
    Click-ExperienceTarget $workingProgress 'city-back';$buildingOverview=Capture-Experience '10d-construction-city-back'
    Assert-ExperienceCityPage $buildingOverview 'overview'
    Click-ExperienceTarget $buildingOverview 'city-planning';$siteWorking=Capture-Experience '10d-site-working'
    Assert-Experience ($siteWorking.cityPlanning -and 0 -notin $siteWorking.citySiteIds -and $siteWorking.battleWorldHash -eq $buildingOverview.battleWorldHash) 'active construction is absent from available panorama sites'
    Click-ExperienceTarget $siteWorking 'city-building-0';$siteQueue=Capture-Experience '10d-site-queue'
    Assert-Experience ($siteQueue.cityPage -eq 'facilities' -and $siteQueue.cityPlot -eq 0 -and $siteQueue.cityFacility -eq 2 -and $siteQueue.cityBuilding -and $siteQueue.battleWorldHash -eq $siteWorking.battleWorldHash) 'visible construction hotspot reaches the same pending mill without another charge'
    Click-ExperienceTarget $siteQueue 'city-back';$siteWorking=Capture-Experience '10d-site-queue-back'
    Click-ExperienceTarget $siteWorking 'city-planning';$buildingOverview=Capture-Experience '10d-site-working-close'
    Click-ExperienceTarget $buildingOverview 'city-facilities';$buildingReturned=Capture-Experience '10e-building-return'
    Assert-Experience ($buildingReturned.cityPage -eq 'facilities' -and $buildingReturned.cityBuilding -and $buildingReturned.cityPlot -eq 0) 'reentering facilities keeps the actual queue and selected plot'
    $state=Set-ExperienceSpeed 60 'build-speed'
    $edExperienceDeadline=[DateTime]::UtcNow.AddSeconds(15);$iteration=0
    do {Start-Sleep -Milliseconds 350;$state=Capture-Experience ('build-wait-'+$iteration);$iteration++} while($state.cityBuilding -and [DateTime]::UtcNow -lt $edExperienceDeadline)
    Assert-Experience (-not $state.cityBuilding -and $state.cityMillLevel -eq 1) 'construction queue completes and builds the actual mill'
    $state=Set-ExperienceSpeed 0 '10g-completed-preview-paused'
    Assert-Experience ($state.cityCurrentEffect -eq 160 -and $state.cityNextEffect -eq 320) 'completed facility immediately displays its new income and next upgrade'
    Click-ExperienceTarget $state 'city-slot-1';$existingChoice=Capture-Experience '10h-existing-choice'
    Click-ExperienceTarget $existingChoice 'city-option-2';$existingPreview=Capture-Experience '10i-existing-mill'
    Assert-Experience ($existingPreview.cityPlot -eq 1 -and $existingPreview.cityFacility -eq 2 -and $existingPreview.cityCurrentEffect -eq 160 -and -not $existingPreview.cityBuilding) 'existing facility selection shows actual level rather than offering a duplicate'
    Click-ExperienceTarget $existingPreview 'city-existing';$state=Capture-Experience '10j-existing-location'
    Assert-Experience ($state.cityPlot -eq 0 -and $state.cityFacility -eq 2 -and $state.battleWorldHash -eq $existingPreview.battleWorldHash) 'existing facility action locates the built mill without charging or duplicating it'
    Click-ExperienceTarget $state 'city-back';$builtPanorama=Capture-Experience '10j-built-panorama'
    Click-ExperienceTarget $builtPanorama 'city-planning';$builtSites=Capture-Experience '10j-built-sites'
    Assert-Experience (0 -notin $builtSites.citySiteIds -and $builtSites.cityMillLevel -eq 1 -and $builtSites.battleWorldHash -eq $state.battleWorldHash) 'completed mill replaces the reserved site and remains unavailable for duplicate placement'
    Click-ExperienceTarget $builtSites 'city-building-0';$builtDetail=Capture-Experience '10j-built-detail'
    Assert-Experience ($builtDetail.cityPlot -eq 0 -and $builtDetail.cityCurrentEffect -eq 160 -and $builtDetail.battleWorldHash -eq $state.battleWorldHash) 'completed panorama facility opens its real income and upgrade details'
    Click-ExperienceTarget $builtDetail 'city-back';$builtSites=Capture-Experience '10j-built-return'
    Click-ExperienceTarget $builtSites 'city-planning';$builtPanorama=Capture-Experience '10j-built-plan-close'
    Click-ExperienceTarget $builtPanorama 'city-construction-page';$completeProgress=Capture-Experience '10j-completion-progress'
    foreach($buttonId in @('city-completion-view','city-completion-dismiss')) {Assert-ExperienceTargetInClient $completeProgress $buttonId}
    Assert-Experience ($completeProgress.cityPage -eq 'construction' -and $completeProgress.cityCompletionAvailable -and $completeProgress.cityCompletionPlot -eq 0 -and $completeProgress.cityCompletionBuilding -eq 2 -and $completeProgress.cityCompletionLevel -eq 1 -and $completeProgress.cityCompletionBefore -eq 0 -and $completeProgress.cityCompletionAfter -eq 160 -and $completeProgress.battleWorldHash -eq $builtPanorama.battleWorldHash) 'actual completed mill produces a frozen zero-to-160 income receipt without another reward'
    Click-ExperienceTarget $completeProgress 'city-completion-view';$completeFacility=Capture-Experience '10j-completion-facility'
    Assert-Experience ($completeFacility.cityPage -eq 'facilities' -and $completeFacility.cityPlot -eq 0 -and $completeFacility.cityCurrentEffect -eq 160 -and $completeFacility.battleWorldHash -eq $completeProgress.battleWorldHash) 'completion action locates the built facility and its actual effect'
    Click-ExperienceTarget $completeFacility 'city-back';$completeProgress=Capture-Experience '10j-completion-back'
    Assert-Experience ($completeProgress.cityPage -eq 'construction' -and $completeProgress.cityCompletionAvailable -and $completeProgress.battleWorldHash -eq $completeFacility.battleWorldHash) 'returning to completion leaves the receipt and campaign unchanged'
    Click-ExperienceTarget $completeProgress 'city-completion-dismiss';$dismissedProgress=Capture-Experience '10j-completion-dismiss'
    Assert-Experience (-not $dismissedProgress.cityCompletionAvailable -and $dismissedProgress.cityMillLevel -eq 1 -and $dismissedProgress.battleWorldHash -eq $completeProgress.battleWorldHash) 'dismissing the session receipt preserves facilities, income, rewards and all campaign data'
    Click-ExperienceTarget $dismissedProgress 'city-back';$builtPanorama=Capture-Experience '10j-completion-dismiss-back'
    Click-ExperienceTarget $builtPanorama 'city-construction-page';$dismissedAgain=Capture-Experience '10j-completion-reopen'
    Assert-Experience (-not $dismissedAgain.cityCompletionAvailable -and $dismissedAgain.battleWorldHash -eq $completeProgress.battleWorldHash) 'reopening dismissed completion does not reconstruct history or settle again'
    Click-ExperienceTarget $dismissedAgain 'city-back';$builtPanorama=Capture-Experience '10j-completion-final-back'
    Click-ExperienceTarget $builtPanorama 'city-building-0';$state=Capture-Experience '10j-built-final-detail'
    $state=Set-ExperienceSpeed 1 'normal-speed'
    Click-ExperienceTarget $state 'city-back';$completedOverview=Capture-Experience '10f-construction-return'
    Assert-ExperienceCityPage $completedOverview 'overview'
    Click-ExperienceTarget $completedOverview 'city-quests';$millQuest=Capture-Experience '11-mill-completed'
    Assert-Experience ($millQuest.questReady -and $millQuest.claimedQuests -eq 1) 'completed mill advances production objective'
    $millQuest=Set-ExperienceSpeed 0 '11a-mill-reward-paused'
    $millQuest=Find-ExperienceScrolledTarget $millQuest 'quest-claim' 'mill-quest-claim' ($cw*.80) ($ch*.57)
    Click-ExperienceTarget $millQuest 'quest-detail-2';$millObjectiveReady=Capture-Experience '11b-objective-completed-mill'
    Assert-Experience ($millObjectiveReady.questDetailReady -and $millObjectiveReady.questDetailId -eq 2 -and @($millObjectiveReady.questDetailCost).Count -eq 0 -and $millObjectiveReady.battleWorldHash -eq $millQuest.battleWorldHash) 'completed construction updates the objective to its actual claim action without retaining a construction quote'
    $claim=Get-ExperienceTarget $millObjectiveReady 'quest-detail-action';Click-Experience $claim.x $claim.y
    $millReward=Capture-Experience '12-mill-reward';Assert-Experience ($millReward.claimedQuests -eq 2) 'mill task reward claimed'
    Assert-ExperienceQuestReceipt $millQuest $millReward 2 3 @(500,400,400,150,100)
    Click-Experience $claim.x $claim.y;$millRepeat=Capture-Experience '13-mill-reward-repeat'
    Assert-Experience ($millRepeat.claimedQuests -eq 2 -and -not $millRepeat.cityBuilding) 'repeat claim neither duplicates reward nor starts an upgrade'
    Assert-Experience ($millRepeat.receiptOpen -and $millRepeat.receiptId -eq 2 -and $millRepeat.battleWorldHash -eq $millReward.battleWorldHash) 'second repeated claim coordinate cannot change receipt or credited resources'
    Click-ExperienceTarget $millRepeat 'quest-reward-done';$millContinued=Capture-Experience '13-reward-continue'
    Assert-ExperienceCityPage $millContinued 'quests'
    Assert-Experience (-not $millContinued.receiptOpen -and $millContinued.questCurrentId -eq 3 -and $millContinued.questChapter -eq 0 -and -not $millContinued.questCompleted -and 3 -in $millContinued.visibleQuestIds -and $millContinued.battleWorldHash -eq $millReward.battleWorldHash) 'receipt continue locates the fully visible next task without any additional reward'
    Click-ExperienceTarget $millContinued 'quest-completed';$completedTwo=Capture-Experience '13a-completed-two'
    Assert-Experience ($completedTwo.questCompleted -and @($completedTwo.visibleQuestIds).Count -eq 2 -and ($completedTwo.visibleQuestIds -join ',') -eq '1,2' -and $completedTwo.claimedQuests -eq 2) 'completed page shows both real reward records after construction'
    Click-ExperienceTarget $completedTwo 'quest-active';$upgradeQuest=Capture-Experience '13b-upgrade-quest'
    $upgradeQuest=Find-ExperienceScrolledTarget $upgradeQuest 'quest-go' 'upgrade-quest-go' ($cw*.80) ($ch*.57)
    Click-ExperienceTarget $upgradeQuest 'quest-go';$upgradePlot=Capture-Experience '13c-upgrade-location'
    Assert-Experience ($upgradePlot.cityPage -eq 'facilities' -and $upgradePlot.cityPlot -eq 4 -and -not $upgradePlot.cityBuilding -and $upgradePlot.claimedQuests -eq 2) 'upgrade quest opens the actual central town hall without starting or claiming anything'
    Click-ExperienceTarget $upgradePlot 'city-back';$millRepeat=Capture-Experience '13d-upgrade-return'
    Assert-ExperienceCityPage $millRepeat 'overview'
    $millRepeat=Set-ExperienceSpeed 1 '13e-post-reward-normal-speed'

    Click-ExperienceTarget $millRepeat 'recruit-tab';$recruit=Capture-Experience '14-recruitment'
    Assert-Experience ($recruit.tab -eq '招募') 'recruitment navigation works'
    $recruit=Set-ExperienceSpeed 0 '14a-card-browsing-paused'
    Click-ExperienceTarget $recruit 'card-library';$library=Capture-Experience '15-card-library'
    Assert-Experience ($library.tab -eq '图鉴' -and $library.battleWorldHash -eq $recruit.battleWorldHash) 'recruitment opens browsable card library without changing the paused campaign'
    Click-ExperienceTarget $library 'faction-魏';$filtered=Capture-Experience '16-wei-filter'
    Assert-Experience ($filtered.catalogFaction -eq '魏') 'faction filter selects Wei cards'
    Click-ExperienceTarget $filtered 'card-first';$detail=Capture-Experience '17-card-detail'
    Assert-Experience (-not [string]::IsNullOrEmpty($detail.cardDetail) -and $detail.cardDetailOwnedUid -eq 0 -and $detail.cardDetailScrollY -eq 0 -and $detail.battleWorldHash -eq $recruit.battleWorldHash) 'library card click opens definition-only detail without creating an owned card'
    Click-ExperienceTarget $detail 'card-attributes';$attributes=Capture-Experience '18-card-attributes'
    Assert-Experience ($attributes.cardSection -eq 0 -and $attributes.cardDetail -eq $detail.cardDetail) 'card attributes section opens'
    Assert-ExperienceCardDossier $attributes
    # These are measured drawn bounds, not invisible review targets. Decorative
    # portrait, pennant and stars must keep the modal and the paused World intact.
    foreach($portraitRegion in @('cardPortraitImage','cardPortraitBanner','cardPortraitCrown')) {
        $portraitBefore=$attributes;$portraitBounds=@($portraitBefore.$portraitRegion)
        Click-Experience ($portraitBounds[0]+$portraitBounds[2]*.5) ($portraitBounds[1]+$portraitBounds[3]*.5)
        $attributes=Capture-Experience ('18-portrait-readonly-'+$portraitRegion)
        Assert-ExperienceCardDossier $attributes
        Assert-Experience ($attributes.speed -eq 0 -and $attributes.mouseBlocked -and $attributes.cardDetail -eq $portraitBefore.cardDetail -and $attributes.cardDetailOwnedUid -eq $portraitBefore.cardDetailOwnedUid -and $attributes.cardSection -eq $portraitBefore.cardSection -and $attributes.cardDetailScrollY -eq $portraitBefore.cardDetailScrollY -and $attributes.selected -eq $portraitBefore.selected -and $attributes.selectedCardUid -eq $portraitBefore.selectedCardUid -and $attributes.battleWorldHash -eq $portraitBefore.battleWorldHash) ($portraitRegion+' actual click preserves the open card, selected land and complete paused campaign')
        Assert-ExperienceMapStill $portraitBefore $attributes ($portraitRegion+' actual click')
    }
    foreach($cardControl in @('card-native-skill','card-scroll','card-return','card-close')) {Assert-ExperienceTargetInClient $attributes $cardControl}
    $nativeSkillTarget=Get-ExperienceTarget $attributes 'card-native-skill';$cardViewport=Get-ExperienceTarget $attributes 'card-scroll'
    Assert-Experience (($nativeSkillTarget.x-$nativeSkillTarget.w*.5) -ge ($cardViewport.x-$cardViewport.w*.5-.01) -and ($nativeSkillTarget.x+$nativeSkillTarget.w*.5) -le ($cardViewport.x+$cardViewport.w*.5+.01) -and ($nativeSkillTarget.y-$nativeSkillTarget.h*.5) -ge ($cardViewport.y-$cardViewport.h*.5-.01) -and ($nativeSkillTarget.y+$nativeSkillTarget.h*.5) -le ($cardViewport.y+$cardViewport.h*.5+.01)) 'homepage native skill button is completely visible in the actual content viewport'
    Click-ExperienceTarget $attributes 'card-native-skill';$nativeSkill=Capture-Experience '18a-card-native-skill'
    Assert-Experience ($nativeSkill.cardSection -eq 1 -and $nativeSkill.cardDetail -eq $detail.cardDetail -and $nativeSkill.cardDetailScrollY -eq 0 -and $nativeSkill.battleWorldHash -eq $recruit.battleWorldHash) 'clicking the visible homepage native skill opens its actual skill section without changing the campaign'
    Assert-Experience ($nativeSkill.cardSkillStyle -eq 'inkscape-skill-sheet-v1' -and $nativeSkill.cardSkillAssetsReady -and $nativeSkill.cardSkillTextFits) 'skill page uses the inspected Inkscape sheet and measured real font'
    foreach($skillRegion in @('cardSkillImage','cardSkillTitle')) {
        $skillBefore=$nativeSkill;$skillBounds=@($skillBefore.$skillRegion);$skillView=@($skillBefore.cardSkillViewport)
        Assert-Experience ($skillBounds.Count -eq 4 -and $skillBounds[2] -gt 0 -and $skillBounds[3] -gt 0 -and $skillBounds[0] -ge $skillView[0] -and $skillBounds[1] -ge $skillView[1] -and ($skillBounds[0]+$skillBounds[2]) -le ($skillView[0]+$skillView[2]) -and ($skillBounds[1]+$skillBounds[3]) -le ($skillView[1]+$skillView[3])) ($skillRegion+' is an actual visible drawing')
        Click-Experience ($skillBounds[0]+$skillBounds[2]*.5) ($skillBounds[1]+$skillBounds[3]*.5)
        $nativeSkill=Capture-Experience ('18a-skill-sheet-readonly-'+$skillRegion)
        Assert-Experience ($nativeSkill.cardSection -eq 1 -and $nativeSkill.cardDetail -eq $skillBefore.cardDetail -and $nativeSkill.cardDetailScrollY -eq 0 -and $nativeSkill.battleWorldHash -eq $skillBefore.battleWorldHash -and $nativeSkill.mouseBlocked) ($skillRegion+' actual click keeps the complete campaign and skill sheet read-only')
        Assert-ExperienceMapStill $skillBefore $nativeSkill ($skillRegion+' click')
    }
    $skillViewport=Get-ExperienceTarget $nativeSkill 'card-scroll'
    Assert-ExperienceTargetInClient $nativeSkill 'card-scroll'
    Focus-Experience;Move-Experience $skillViewport.x $skillViewport.y;Assert-ExperiencePointer $skillViewport.x $skillViewport.y
    [EDExperiencePointer]::mouse_event(0x0800,0,0,[uint32](4294967296-360),[UIntPtr]::Zero)
    Start-Sleep -Milliseconds 200;Assert-ExperiencePointer $skillViewport.x $skillViewport.y
    $skillWheeled=Capture-Experience '18a-card-native-skill-wheel'
    Assert-Experience ($skillWheeled.cardSection -eq 1 -and $skillWheeled.cardDetail -eq $nativeSkill.cardDetail -and $skillWheeled.battleWorldHash -eq $nativeSkill.battleWorldHash -and $skillWheeled.cardSkillContentHeight -eq $nativeSkill.cardSkillContentHeight) 'actual wheel over skill text preserves the definition, measured content and complete campaign'
    Assert-Experience ($skillWheeled.cardDetailScrollY -ge 0 -and $skillWheeled.cardDetailScrollY -le $skillWheeled.cardSkillScrollLimit+.05) 'actual skill wheel stays inside the measured content bounds'
    if($skillWheeled.cardSkillScrollLimit -gt .05) {
        Assert-Experience ($skillWheeled.cardDetailScrollY -gt 0) 'genuinely overflowing skill text responds to actual wheel input'
    } else {
        Assert-Experience ([Math]::Abs($skillWheeled.cardDetailScrollY) -lt .05) 'fully fitting skill text remains visible without a spurious scroll offset'
    }
    Assert-ExperienceMapStill $nativeSkill $skillWheeled 'skill text actual wheel'
    $nativeSkill=$skillWheeled
    Click-ExperienceTarget $nativeSkill 'card-attributes';$attributes=Capture-Experience '18b-card-attributes-return'
    Assert-ExperienceCardDossier $attributes
    Click-ExperienceTarget $attributes 'card-learned-skill';$learnedSkill=Capture-Experience '18c-card-learned-skill'
    Assert-Experience ($learnedSkill.cardSection -eq 1 -and $learnedSkill.cardDetail -eq $attributes.cardDetail -and $learnedSkill.cardDossierLearnedName -eq $attributes.cardDossierLearnedName -and $learnedSkill.cardDetailScrollY -eq 0 -and $learnedSkill.battleWorldHash -eq $attributes.battleWorldHash) 'visible learned skill slot opens the same read-only dossier without inventing ownership or spending'
    Click-ExperienceTarget $learnedSkill 'card-attributes';$attributes=Capture-Experience '18d-card-learned-return'
    # Click actual drawing bounds inside the existing skill surface. No extra
    # synthetic target is registered for either the imported image or its text.
    foreach($dossierRegion in @('cardDossierNativeImage','cardDossierNativeNameArea','cardDossierLearnedImage','cardDossierLearnedNameArea')) {
        Assert-ExperienceCardDossier $attributes
        $dossierBefore=$attributes;$drawn=@($dossierBefore.$dossierRegion)
        Click-Experience ($drawn[0]+$drawn[2]*.5) ($drawn[1]+$drawn[3]*.5)
        $dossierSkill=Capture-Experience ('18e-dossier-region-'+$dossierRegion)
        Assert-Experience ($dossierSkill.speed -eq 0 -and $dossierSkill.cardSection -eq 1 -and $dossierSkill.cardDetail -eq $dossierBefore.cardDetail -and $dossierSkill.cardDetailOwnedUid -eq $dossierBefore.cardDetailOwnedUid -and $dossierSkill.cardDetailScrollY -eq 0 -and $dossierSkill.cardDossierNativeName -eq $dossierBefore.cardDossierNativeName -and $dossierSkill.cardDossierLearnedName -eq $dossierBefore.cardDossierLearnedName -and $dossierSkill.battleWorldHash -eq $dossierBefore.battleWorldHash) ($dossierRegion+' actual image or name click opens the same frozen card skill page without changing ownership or campaign')
        Assert-Experience ([string]::IsNullOrEmpty($dossierSkill.cardDossierDesignStyle) -and @($dossierSkill.cardDossierNativeImage).Count -eq 0 -and @($dossierSkill.cardDossierNativeNameArea).Count -eq 0 -and @($dossierSkill.cardDossierLearnedImage).Count -eq 0 -and @($dossierSkill.cardDossierLearnedNameArea).Count -eq 0) 'hidden attribute-page image/name regions are not exposed as visible input geometry'
        Assert-ExperienceMapStill $dossierBefore $dossierSkill ($dossierRegion+' actual click')
        Click-ExperienceTarget $dossierSkill 'card-attributes';$attributes=Capture-Experience ('18e-dossier-region-return-'+$dossierRegion)
        Assert-ExperienceCardDossier $attributes
        Assert-Experience ($attributes.cardDetail -eq $dossierBefore.cardDetail -and $attributes.cardDetailOwnedUid -eq $dossierBefore.cardDetailOwnedUid -and $attributes.cardDetailScrollY -eq 0 -and $attributes.selectedCardUid -eq $dossierBefore.selectedCardUid -and $attributes.battleWorldHash -eq $dossierBefore.battleWorldHash) ($dossierRegion+' return restores actual attribute artwork, card identity and complete campaign')
        Assert-ExperienceMapStill $dossierBefore $attributes ($dossierRegion+' attribute return')
    }
    Click-ExperienceTarget $attributes 'card-skill';$skill=Capture-Experience '19-card-skill'
    Assert-ExperienceCardPortrait $skill
    Assert-Experience ($skill.cardSection -eq 1 -and $skill.cardDetail -eq $detail.cardDetail -and $skill.battleWorldHash -eq $recruit.battleWorldHash) 'card skill tab opens the same read-only definition'
    Drag-Experience ($cw*.30) ($ch*.46) ($cw*.35) ($ch*.52)
    $blocked=Capture-Experience '20-detail-pointer-block';Assert-ExperienceMapStill $skill $blocked 'card detail modal'
    Assert-Experience ($blocked.cardDetail -eq $detail.cardDetail -and $blocked.battleWorldHash -eq $recruit.battleWorldHash) 'modal drag preserves inspected card and the entire campaign'
    Click-ExperienceTarget $blocked 'card-source';$cardSource=Capture-Experience '20a-card-source'
    Assert-Experience ($cardSource.cardSection -eq 2 -and $cardSource.cardDetail -eq $detail.cardDetail -and $cardSource.cardDetailScrollY -eq 0) 'source tab begins at the actual biography and acquisition information'
    $sourceViewport=Get-ExperienceTarget $cardSource 'card-scroll'
    Focus-Experience;Move-Experience $sourceViewport.x $sourceViewport.y;Assert-ExperiencePointer $sourceViewport.x $sourceViewport.y
    [EDExperiencePointer]::mouse_event(0x0800,0,0,[uint32](4294967296-360),[UIntPtr]::Zero)
    Start-Sleep -Milliseconds 200;Assert-ExperiencePointer $sourceViewport.x $sourceViewport.y
    $cardSourceScrolled=Capture-Experience '20b-card-source-scrolled'
    Assert-ExperienceCardPortrait $cardSourceScrolled
    Assert-Experience ($cardSourceScrolled.cardSection -eq 2 -and $cardSourceScrolled.cardDetail -eq $detail.cardDetail -and $cardSourceScrolled.cardDetailOwnedUid -eq 0 -and $cardSourceScrolled.cardDetailScrollY -gt $cardSource.cardDetailScrollY -and $cardSourceScrolled.battleWorldHash -eq $recruit.battleWorldHash) 'actual source viewport scrolling reveals more recorded text without recruiting, spending or changing any campaign state'
    Assert-Experience (@($cardSourceScrolled.targets | Where-Object {$_.id -eq 'card-native-skill'}).Count -eq 0) 'source page exposes no hidden homepage skill target'
    Click-ExperienceTarget $cardSourceScrolled 'card-return';$closed=Capture-Experience '21-detail-close'
    Assert-Experience ([string]::IsNullOrEmpty($closed.cardDetail) -and $closed.cardDetailOwnedUid -eq 0 -and $closed.cardDetailScrollY -eq 0 -and $closed.tab -eq '图鉴' -and $closed.catalogFaction -eq '魏' -and $closed.battleWorldHash -eq $recruit.battleWorldHash) 'visible bottom return restores the filtered library and clears the actual detail scroll without payment'
    # Exercise the archive's own displayed controls and real content viewport.
    # A partially clipped row is deliberately allowed to have no click target.
    $assertArchive={param($archiveState)
        Assert-Experience ($archiveState.archiveOpen -and $archiveState.tab -eq '图鉴' -and $archiveState.battleWorldHash -eq $recruit.battleWorldHash) 'archive inspection preserves the entire paused campaign'
        foreach($archiveControl in @('archive-scroll','archive-search','archive-search-clear','archive-scope','library-back','faction-全部','faction-魏')) {Assert-ExperienceTargetInClient $archiveState $archiveControl}
        $archiveViewport=Get-ExperienceTarget $archiveState 'archive-scroll'
        $archiveCards=@($archiveState.targets | Where-Object {$_.id -like 'archive-card-*'})
        Assert-Experience ($archiveCards.Count -eq @($archiveState.archiveVisibleIds).Count) 'archive diagnostics observe exactly the currently visible card targets'
        Assert-Experience ($archiveState.archiveDesignStyle -eq 'inkscape-archive-compact-v1' -and $archiveState.archiveDesignAssetsReady -and $archiveState.archiveDesignTextFits -and $archiveState.archiveDesignColumns -ge 6) 'archive loads inspected Inkscape components and fits the actual Chinese GUI fonts'
        $archiveNodes=@($archiveState.archiveDesignNodes)
        Assert-Experience ($archiveNodes.Count -eq $archiveCards.Count) 'archive image/name/footer geometry exists only for complete visible cards'
        if($archiveState.archiveScrollY -eq 0) {Assert-Experience ($archiveNodes.Count -ge [Math]::Min($archiveState.archiveCount,2*$archiveState.archiveDesignColumns)) 'archive initial viewport exposes two complete rows from its actual filtered definitions'}
        foreach($archiveNode in $archiveNodes) {
            Assert-Experience ($archiveNode.id -in @($archiveState.archiveVisibleIds) -and -not [string]::IsNullOrEmpty($archiveNode.name)) 'archive geometry retains the actual visible definition identity'
            $archiveFrame=@($archiveNode.card)
            foreach($archiveRegion in @('art','nameArea','footer','troop','cost','stars','skill','scope')) {
                $archiveBounds=@($archiveNode.$archiveRegion)
                Assert-Experience ($archiveBounds.Count -eq 4 -and $archiveBounds[2] -gt 0 -and $archiveBounds[3] -gt 0 -and $archiveBounds[0] -ge $archiveFrame[0]-.1 -and $archiveBounds[1] -ge $archiveFrame[1]-.1 -and $archiveBounds[0]+$archiveBounds[2] -le $archiveFrame[0]+$archiveFrame[2]+.1 -and $archiveBounds[1]+$archiveBounds[3] -le $archiveFrame[1]+$archiveFrame[3]+.1) ('actual archive '+$archiveRegion+' lies fully inside its clickable card')
            }
            foreach($archivePair in @(@('art','nameArea'),@('art','footer'),@('nameArea','footer'),@('nameArea','troop'),@('art','troop'),@('troop','footer'),@('cost','stars'),@('art','cost'),@('art','stars'),@('skill','scope'))) {
                $archiveA=@($archiveNode.($archivePair[0]));$archiveB=@($archiveNode.($archivePair[1]))
                Assert-Experience ($archiveA[0]+$archiveA[2] -le $archiveB[0]+.1 -or $archiveB[0]+$archiveB[2] -le $archiveA[0]+.1 -or $archiveA[1]+$archiveA[3] -le $archiveB[1]+.1 -or $archiveB[1]+$archiveB[3] -le $archiveA[1]+.1) ('actual archive '+$archivePair[0]+'/'+$archivePair[1]+' regions do not overlap')
            }
        }
        foreach($archiveCard in $archiveCards) {
            Assert-Experience (($archiveCard.x-$archiveCard.w*.5) -ge ($archiveViewport.x-$archiveViewport.w*.5-.1) -and ($archiveCard.x+$archiveCard.w*.5) -le ($archiveViewport.x+$archiveViewport.w*.5+.1) -and ($archiveCard.y-$archiveCard.h*.5) -ge ($archiveViewport.y-$archiveViewport.h*.5-.1) -and ($archiveCard.y+$archiveCard.h*.5) -le ($archiveViewport.y+$archiveViewport.h*.5+.1)) ('archive target is a complete card inside the real viewport: '+$archiveCard.id)
        }
        if($archiveState.archiveCount -eq 0) {Assert-Experience ($archiveCards.Count -eq 0 -and @($archiveState.targets | Where-Object {$_.id -eq 'card-first'}).Count -eq 0) 'empty archive results expose no hidden or stale card target'}
    }
    & $assertArchive $closed
    Assert-Experience (-not $closed.archiveAllSources -and $closed.archiveCount -gt 0 -and [string]::IsNullOrEmpty($closed.archiveSearch)) 'recruitment library starts with its actual recruitable Wei scope'
    $archiveRecruitableCount=$closed.archiveCount
    Click-ExperienceTarget $closed 'archive-scope';$archiveAll=Capture-Experience '21a-archive-all-sources'
    & $assertArchive $archiveAll
    Assert-Experience ($archiveAll.archiveAllSources -and $archiveAll.archiveCount -gt $archiveRecruitableCount -and $archiveAll.catalogFaction -eq '魏' -and $archiveAll.archiveScrollY -eq 0 -and [string]::IsNullOrEmpty($archiveAll.archiveSelected)) 'scope click reveals actual historical Wei definitions and resets browsing selection without recruiting'
    foreach($archiveRegion in @('art','nameArea','footer')) {
        $archiveBefore=$archiveAll
        # Click the actual drawn region inside the card's existing parent action.
        # No invisible child target is registered just to satisfy this driver.
        $archiveNode=@($archiveBefore.archiveDesignNodes)[0]
        $archiveBounds=@($archiveNode.$archiveRegion)
        Click-Experience ($archiveBounds[0]+$archiveBounds[2]*.5) ($archiveBounds[1]+$archiveBounds[3]*.5)
        $archiveRegionDetail=Capture-Experience ('21a-archive-region-'+$archiveRegion)
        Assert-Experience ($archiveRegionDetail.cardDetail -eq $archiveNode.id -and $archiveRegionDetail.cardDetailOwnedUid -eq 0 -and $archiveRegionDetail.archiveSelected -eq $archiveNode.id -and -not $archiveRegionDetail.archiveOpen -and $archiveRegionDetail.battleWorldHash -eq $archiveBefore.battleWorldHash) ('actual archive '+$archiveRegion+' click opens its own read-only definition')
        Assert-Experience ([string]::IsNullOrEmpty($archiveRegionDetail.archiveDesignStyle) -and @($archiveRegionDetail.archiveDesignNodes).Count -eq 0) 'covered archive exposes no stale design image/name/footer geometry'
        Assert-ExperienceMapStill $archiveBefore $archiveRegionDetail ('archive '+$archiveRegion+' click')
        Click-ExperienceTarget $archiveRegionDetail 'card-return'
        $archiveAll=Capture-Experience ('21a-archive-region-return-'+$archiveRegion)
        & $assertArchive $archiveAll
        Assert-Experience ([string]::IsNullOrEmpty($archiveAll.cardDetail) -and $archiveAll.archiveSelected -eq $archiveNode.id -and $archiveAll.archiveAllSources -eq $archiveBefore.archiveAllSources -and $archiveAll.catalogFaction -eq $archiveBefore.catalogFaction -and $archiveAll.archiveSearch -eq $archiveBefore.archiveSearch -and [Math]::Abs($archiveAll.archiveScrollY-$archiveBefore.archiveScrollY) -lt .01 -and $archiveAll.battleWorldHash -eq $archiveBefore.battleWorldHash) ('archive '+$archiveRegion+' return preserves filters, selection, scroll and campaign')
        Assert-ExperienceMapStill $archiveBefore $archiveAll ('archive '+$archiveRegion+' return')
    }
    Click-ExperienceTarget $archiveAll 'archive-search'
    # Digits avoid any keyboard-layout-dependent letter mapping and produce an
    # unmatched query through actual focused keyboard input, without a debug setter.
    for($archiveDigit=0;$archiveDigit -lt 8;$archiveDigit++) {
        Focus-Experience
        [EDExperiencePointer]::KeyEvent($edExperienceProcess.MainWindowHandle,0x39,0)
        try {Start-Sleep -Milliseconds 45} finally {[EDExperiencePointer]::KeyEvent($edExperienceProcess.MainWindowHandle,0x39,2)}
        Start-Sleep -Milliseconds 45
    }
    # Finish text editing through a visible, already-selected faction control.
    # This leaves the query/scope unchanged; the exact typed query, empty result
    # and entire paused campaign are still asserted after a genuinely fresh F8.
    Click-ExperienceTarget $archiveAll 'faction-魏'
    $archiveEmpty=Capture-Experience '21b-archive-empty-search'
    & $assertArchive $archiveEmpty
    Assert-Experience ($archiveEmpty.archiveSearch -eq '99999999' -and $archiveEmpty.archiveCount -eq 0 -and $archiveEmpty.archiveAllSources -and $archiveEmpty.catalogFaction -eq '魏' -and $archiveEmpty.archiveScrollY -eq 0) 'typing in the visible search field shows a true empty result while retaining faction and scope'
    Click-ExperienceTarget $archiveEmpty 'archive-search-clear';$archiveCleared=Capture-Experience '21c-archive-cleared-search'
    & $assertArchive $archiveCleared
    Assert-Experience ([string]::IsNullOrEmpty($archiveCleared.archiveSearch) -and $archiveCleared.archiveCount -eq $archiveAll.archiveCount -and $archiveCleared.archiveAllSources -and $archiveCleared.catalogFaction -eq '魏' -and @($archiveCleared.archiveVisibleIds).Count -gt 0) 'visible clear control restores the exact filtered archive after an empty search'
    Click-ExperienceTarget $archiveCleared 'archive-scope';$archiveRecruitable=Capture-Experience '21d-archive-recruitable'
    & $assertArchive $archiveRecruitable
    Assert-Experience (-not $archiveRecruitable.archiveAllSources -and $archiveRecruitable.archiveCount -eq $archiveRecruitableCount -and $archiveRecruitable.catalogFaction -eq '魏') 'scope toggles back to exactly the original recruitable definitions'
    Click-ExperienceTarget $archiveRecruitable 'archive-scope';$archiveScrolled=Capture-Experience '21e-archive-before-scroll'
    $archiveViewport=Get-ExperienceTarget $archiveScrolled 'archive-scroll'
    for($archiveAttempt=0;$archiveAttempt -lt 13;$archiveAttempt++) {
        Focus-Experience;Move-Experience $archiveViewport.x $archiveViewport.y;Assert-ExperiencePointer $archiveViewport.x $archiveViewport.y
        $archiveWheel=if($archiveAttempt -eq 0){1200}else{240}
        [EDExperiencePointer]::mouse_event(0x0800,0,0,[uint32](4294967296-$archiveWheel),[UIntPtr]::Zero)
        Start-Sleep -Milliseconds 200;Assert-ExperiencePointer $archiveViewport.x $archiveViewport.y
        $archiveScrolled=Capture-Experience ('21f-archive-scroll-'+$archiveAttempt)
        & $assertArchive $archiveScrolled
        $archiveCompleteCards=@($archiveScrolled.targets | Where-Object {$_.id -like 'archive-card-*'})
        if($archiveScrolled.archiveScrollY -gt 0 -and $archiveCompleteCards.Count -gt 0 -and @($archiveScrolled.targets | Where-Object {$_.id -eq 'card-first'}).Count -eq 0){break}
    }
    Assert-Experience ($archiveScrolled.archiveScrollY -gt 0 -and $archiveCompleteCards.Count -gt 0 -and @($archiveScrolled.targets | Where-Object {$_.id -eq 'card-first'}).Count -eq 0) 'real archive wheel scrolling reaches fully visible later cards and removes the clipped first-card target'
    $archiveChosen=$archiveCompleteCards[$archiveCompleteCards.Count-1]
    Drag-Experience ($archiveChosen.x-$archiveChosen.w*.2) ($archiveChosen.y-$archiveChosen.h*.1) ($archiveChosen.x+$archiveChosen.w*.2) ($archiveChosen.y+$archiveChosen.h*.1)
    $archiveDragged=Capture-Experience '21g-archive-card-drag'
    & $assertArchive $archiveDragged
    Assert-Experience ([string]::IsNullOrEmpty($archiveDragged.cardDetail) -and $archiveDragged.archiveSelected -eq $archiveScrolled.archiveSelected -and [Math]::Abs($archiveDragged.archiveScrollY-$archiveScrolled.archiveScrollY) -lt .01) 'drag ending inside a visible archive card does not open or select it'
    Assert-ExperienceMapStill $archiveScrolled $archiveDragged 'archive card drag'
    Click-ExperienceTarget $archiveDragged $archiveChosen.id;$archiveLaterDetail=Capture-Experience '21h-archive-later-detail'
    $archiveExpectedId=$archiveChosen.id.Substring(13)
    Assert-Experience ($archiveLaterDetail.cardDetail -eq $archiveExpectedId -and $archiveLaterDetail.cardDetailOwnedUid -eq 0 -and $archiveLaterDetail.archiveSelected -eq $archiveExpectedId -and -not $archiveLaterDetail.archiveOpen -and $archiveLaterDetail.battleWorldHash -eq $recruit.battleWorldHash) 'actual later-card click opens exactly that definition without creating ownership or changing campaign state'
    Click-ExperienceTarget $archiveLaterDetail 'card-return';$closed=Capture-Experience '21i-archive-later-return'
    & $assertArchive $closed
    Assert-Experience ([string]::IsNullOrEmpty($closed.cardDetail) -and $closed.archiveSelected -eq $archiveExpectedId -and $closed.archiveAllSources -and $closed.catalogFaction -eq '魏' -and [string]::IsNullOrEmpty($closed.archiveSearch) -and $closed.archiveScrollY -gt 0 -and [Math]::Abs($closed.archiveScrollY-$archiveScrolled.archiveScrollY) -lt .01 -and $archiveExpectedId -in @($closed.archiveVisibleIds)) 'later-card return restores the actual nonzero scroll, selected definition, faction and scope'
    Click-ExperienceTarget $closed 'library-back';$back=Capture-Experience '22-library-return'
    Assert-Experience ($back.tab -eq '招募' -and $back.catalogFaction -eq '魏') 'return to recruitment retains faction'
    Click-ExperienceTarget $back 'card-library';$reopened=Capture-Experience '23-library-reopen'
    Assert-Experience ($reopened.tab -eq '图鉴' -and $reopened.catalogFaction -eq '魏') 'reopening library retains faction'
    Click-ExperienceTarget $reopened 'roster-tab';$roster=Capture-Experience '24-owned-roster'
    Assert-Experience ($roster.tab -eq '武将') 'owned roster navigation works'
    Assert-ExperienceOwnedRoster $roster
    Click-ExperienceTarget $roster 'owned-card-first';$owned=Capture-Experience '25-owned-card-detail'
    Assert-Experience (-not [string]::IsNullOrEmpty($owned.cardDetail) -and $owned.cardDetailOwnedUid -gt 0 -and $owned.cardDetailOwnedUid -eq $owned.selectedCardUid -and $owned.cardDetailOwnedLevel -eq $owned.selectedCardLevel -and $owned.cardDetailOwnedExperience -eq $owned.selectedCardExperience -and $owned.cardDetailOwnedLevelCap -ge $owned.cardDetailOwnedLevel -and $owned.battleWorldHash -eq $recruit.battleWorldHash) 'owned card opens its actual instance with matching level and experience without campaign changes'
    Assert-ExperienceOwnedRosterModal $owned
    Assert-ExperienceCardDossier $owned
    foreach($cardControl in @('owned-card-manage','card-return','card-native-skill')) {Assert-ExperienceTargetInClient $owned $cardControl}
    Click-ExperienceTarget $owned 'card-return';$ownedReturned=Capture-Experience '25a-owned-bottom-return'
    Assert-Experience ([string]::IsNullOrEmpty($ownedReturned.cardDetail) -and $ownedReturned.tab -eq '武将' -and $ownedReturned.battleWorldHash -eq $owned.battleWorldHash) 'owned bottom return restores the actual roster without charging or changing formation'
    Click-ExperienceTarget $ownedReturned 'owned-card-first';$ownedManage=Capture-Experience '25b-owned-manage-preview'
    Click-ExperienceTarget $ownedManage 'owned-card-manage';$managed=Capture-Experience '25c-owned-manage-roster'
    Assert-Experience ([string]::IsNullOrEmpty($managed.cardDetail) -and $managed.tab -eq '武将' -and $managed.selectedCardUid -eq $ownedManage.cardDetailOwnedUid -and $managed.battleWorldHash -eq $ownedManage.battleWorldHash) 'owned management entry selects the real roster instance without assigning a formation or spending resources'
    Click-ExperienceTarget $managed 'owned-card-first';$ownedClosePreview=Capture-Experience '25d-owned-close-preview'
    Click-ExperienceTarget $ownedClosePreview 'card-close';$ownedClosed=Capture-Experience '26-owned-return'
    Assert-Experience ([string]::IsNullOrEmpty($ownedClosed.cardDetail) -and $ownedClosed.cardDetailOwnedUid -eq 0 -and $ownedClosed.tab -eq '武将' -and $ownedClosed.battleWorldHash -eq $recruit.battleWorldHash) 'owned card close restores roster and leaves the entire paused campaign unchanged'
    $ownedClosed=Set-ExperienceSpeed 1 '26a-card-browsing-resume'

    # Training must be reached by visible controls and execute a real captured-land order.
    Click-ExperienceTarget $ownedClosed 'city-open';$trainingCity=Capture-Experience '27-training-city'
    Click-ExperienceTarget $trainingCity 'city-training';$emptyTraining=Capture-Experience '28-training-no-land'
    $emptyTraining=Set-ExperienceSpeed 0 '28a-training-history-pause'
    Click-ExperienceTarget $emptyTraining 'training-history';$emptyHistory=Capture-Experience '28b-training-history-empty'
    Assert-Experience ($emptyHistory.trainingHistoryOpen -and $emptyHistory.trainingHistorySelected -eq -1 -and $emptyHistory.trainingHistoryStatus -eq 'none' -and $emptyHistory.trainingHistoryParticipants.Count -eq 0 -and $emptyHistory.battleWorldHash -eq $emptyTraining.battleWorldHash) 'empty history has no invented participant or reward and leaves the campaign unchanged'
    Click-ExperienceTarget $emptyHistory 'training-history-prepare';$emptyHistoryBack=Capture-Experience '28c-training-history-return'
    Assert-Experience (-not $emptyHistoryBack.trainingHistoryOpen -and $emptyHistoryBack.tab -eq '练兵' -and $emptyHistoryBack.trainingTarget -eq $emptyTraining.trainingTarget -and $emptyHistoryBack.trainingReturnTab -eq $emptyTraining.trainingReturnTab -and $emptyHistoryBack.battleWorldHash -eq $emptyTraining.battleWorldHash) 'empty history returns to the exact original plan without charge'
    $emptyTraining=Set-ExperienceSpeed 1 '28d-training-history-resume'
    Assert-Experience ($emptyTraining.tab -eq '练兵' -and $emptyTraining.trainingReturnTab -eq '主城' -and $emptyTraining.trainingTarget -eq -1 -and -not [string]::IsNullOrEmpty($emptyTraining.trainingProblem)) 'city entry opens independent training with no-land reason'
    Assert-Experience (-not $emptyTraining.orderActive -and $emptyTraining.commands -eq $trainingCity.commands -and $emptyTraining.generalExperienceTotal -eq $trainingCity.generalExperienceTotal) 'opening no-land training does not charge or grant experience'
    $trainingGeneral=Get-ExperienceTarget $emptyTraining 'training-general-2'
    # Both endpoints deliberately stay inside the actual visible card: releasing
    # an IMGUI button after a drag must not be mistaken for an inspection click.
    Drag-Experience ($trainingGeneral.x-$trainingGeneral.w*.18) ($trainingGeneral.y-$trainingGeneral.h*.12) ($trainingGeneral.x+$trainingGeneral.w*.18) ($trainingGeneral.y+$trainingGeneral.h*.12)
    $trainingBlocked=Capture-Experience '29-training-map-block';Assert-ExperienceMapStill $emptyTraining $trainingBlocked 'independent training page'
    Assert-Experience ([string]::IsNullOrEmpty($trainingBlocked.cardDetail) -and $trainingBlocked.tab -eq '练兵' -and $trainingBlocked.selected -eq $emptyTraining.selected) 'dragging a training general card does not inspect it or change the underlying selection'
    Click-ExperienceTarget $trainingBlocked 'training-general-2';$trainingCard=Capture-Experience '29a-training-general-detail'
    Assert-Experience (-not [string]::IsNullOrEmpty($trainingCard.cardDetail) -and $trainingCard.tab -eq '练兵' -and $trainingCard.trainingReturnTab -eq '主城') 'deliberate training general click opens its inspectable card while keeping the training origin'
    Click-ExperienceTarget $trainingCard 'card-close';$trainingBlocked=Capture-Experience '29b-training-general-return'
    Assert-Experience ([string]::IsNullOrEmpty($trainingBlocked.cardDetail) -and $trainingBlocked.tab -eq '练兵' -and $trainingBlocked.trainingTarget -eq $emptyTraining.trainingTarget) 'closing the training card restores the unchanged training plan'
    Click-ExperienceTarget $trainingBlocked 'training-back';$trainingCityReturn=Capture-Experience '30-training-city-return'
    Assert-Experience ($trainingCityReturn.tab -eq '主城' -and $trainingCityReturn.cityPage -eq $trainingCity.cityPage -and $trainingCityReturn.cityPlot -eq $trainingCity.cityPlot -and $trainingCityReturn.selected -eq $trainingCity.selected) 'training returns to preserved city page and map selection'
    Click-ExperienceTarget $trainingCityReturn 'city-training';$trainingNoLand=Capture-Experience '31-training-no-land-reopen'
    Click-ExperienceTarget $trainingNoLand 'training-map';$trainingLand=Capture-Experience '32-training-map-target'
    Assert-Experience ($trainingLand.tab -eq '领地' -and $trainingLand.selected -ge 0 -and -not $trainingLand.selectedOwned) 'training no-land action selects an actual occupiable field'
    # Pause through the visible time control so waiting/return/reentry are measured
    # before arrival, regardless of PNG write speed on the host desktop.
    $trainingLand=Set-ExperienceSpeed 0 'battle-departure-pause'
    Assert-Experience ($trainingLand.landHasTerrainPatch) 'actual resource field must exercise meadow picking; missing terrain diagnostics may not skip the interaction'
    $trainingLand=Check-ExperienceLandReturn $trainingLand '32a-land-panel'
    Click-ExperienceTarget $trainingLand 'army-attack-icon';$attackConfirm=Capture-Experience '33a-attack-confirmation'
    Assert-ExperienceLandModal $attackConfirm
    Assert-Experience ($attackConfirm.confirmationOpen -and $attackConfirm.watchAttackOnArrival -and -not $attackConfirm.orderActive -and $attackConfirm.reportCount -eq $trainingLand.reportCount -and $attackConfirm.battleWorldHash -eq $trainingLand.battleWorldHash) 'attack confirmation defaults to arrival watching without an early charge order or result'
    Click-ExperienceTarget $attackConfirm 'attack-watch-toggle';$attackOptOut=Capture-Experience '33b-attack-watch-off'
    Assert-Experience (-not $attackOptOut.watchAttackOnArrival -and -not $attackOptOut.orderActive -and $attackOptOut.battleWorldHash -eq $trainingLand.battleWorldHash) 'visible arrival-watch option can be disabled without changing the campaign'
    Click-ExperienceTarget $attackOptOut 'attack-watch-toggle';$attackOptIn=Capture-Experience '33c-attack-watch-on'
    Click-ExperienceTarget $attackOptIn 'confirm-no';$attackDeclined=Capture-Experience '33d-attack-declined'
    Assert-Experience (-not $attackDeclined.confirmationOpen -and -not $attackDeclined.orderActive -and $attackDeclined.battleWorldHash -eq $trainingLand.battleWorldHash) 'declining departure leaves the complete campaign unchanged'
    Assert-ExperienceLand $attackDeclined
    Click-ExperienceLandCaption $attackDeclined 'army-attack';$attackConfirm=Capture-Experience '33e-attack-confirm-again'
    Assert-ExperienceLandModal $attackConfirm
    Assert-Experience ($attackConfirm.confirmationOpen -and -not $attackConfirm.orderActive -and $attackConfirm.battleWorldHash -eq $trainingLand.battleWorldHash) 'the actual attack label and its visible icon both reach the same confirmation without charging'
    Click-ExperienceTarget $attackConfirm 'confirm-yes';$trainingAttack=Capture-Experience '33-training-capture-issued'
    Assert-Experience ($trainingAttack.orderActive -and $trainingAttack.orderAction -eq 0 -and $trainingAttack.battleWaiting -and $trainingAttack.orderWatchOnArrival -and $trainingAttack.currentBattlePending -and -not [string]::IsNullOrEmpty($trainingAttack.orderBattleId) -and $trainingAttack.currentBattleId -eq $trainingAttack.orderBattleId) 'confirmed attack opens waiting for the actual identified army order'
    $watchId=$trainingAttack.orderBattleId
    Assert-Experience ($trainingAttack.reportCount -eq $trainingLand.reportCount -and -not $trainingAttack.selectedOwned -and -not $trainingAttack.replayOpen) 'unarrived waiting exposes neither new battle report nor captured land nor replay'
    Drag-Experience ($cw*.42) ($ch*.37) ($cw*.48) ($ch*.43);$waitingDrag=Capture-Experience '33f-battle-waiting-map-block'
    Assert-ExperienceMapStill $trainingAttack $waitingDrag 'battle waiting page'
    Assert-Experience ($waitingDrag.battleWaiting -and $waitingDrag.selected -eq $trainingAttack.selected -and $waitingDrag.battleWorldHash -eq $trainingAttack.battleWorldHash) 'waiting drag blocks underlying selection and leaves the pending battle unchanged'
    Click-ExperienceTarget $waitingDrag 'waiting-recall';$recallConfirm=Capture-Experience '33g-battle-recall-confirm'
    Assert-Experience ($recallConfirm.confirmationOpen -and $recallConfirm.orderActive -and -not $recallConfirm.orderReturning -and $recallConfirm.reportCount -eq $trainingLand.reportCount) 'waiting recall requires confirmation before changing the marching army'
    Click-ExperienceTarget $recallConfirm 'confirm-no';$waitingAgain=Capture-Experience '33h-battle-recall-declined'
    Assert-Experience ($waitingAgain.battleWaiting -and -not $waitingAgain.orderReturning -and $waitingAgain.battleWorldHash -eq $trainingAttack.battleWorldHash) 'declining recall preserves the same unarrived battle order'
    Click-ExperienceTarget $waitingAgain 'waiting-back';$waitingMap=Capture-Experience '33i-battle-waiting-return'
    Assert-Experience ($waitingMap.tab -eq '领地' -and $waitingMap.orderActive -and -not $waitingMap.currentBattlePending -and $waitingMap.battleWorldHash -eq $trainingAttack.battleWorldHash) 'waiting return preserves the army order and prevents an unsolicited arrival page'
    Click-ExperienceTarget $waitingMap 'army-battle-watch';$waitingReopen=Capture-Experience '33j-battle-waiting-reopen'
    Assert-Experience ($waitingReopen.battleWaiting -and $waitingReopen.currentBattlePending -and $waitingReopen.currentBattleId -eq $watchId -and $waitingReopen.reportCount -eq $trainingLand.reportCount -and $waitingReopen.battleWorldHash -eq $trainingAttack.battleWorldHash) 'map battle entry reopens the same pending battle without creating results'
    $state=Set-ExperienceSpeed 1 'battle-arrival-speed'
    $currentBattle=Wait-ExperienceState '33k-battle-arrived' {param($s) $s.replayOpen -and $s.watchingCurrentBattle} 45
    Assert-Experience ($currentBattle.replayBattleId -eq $watchId -and $currentBattle.lastReportBattleId -eq $watchId -and $currentBattle.reportCount -eq ($trainingLand.reportCount+1) -and $currentBattle.selectedOwned -and -not $currentBattle.currentBattlePending) 'actual arrival automatically opens the recorded battle with its departure identity and settles once'
    $currentBattleAdvanced=Wait-ExperienceState '33l-battle-current-playing' {param($s) $s.replayPosition -gt $currentBattle.replayPosition -or $s.replayPosition -eq ($s.replayCount-1)} 8
    Assert-Experience ($currentBattleAdvanced.replayPosition -gt $currentBattle.replayPosition -or $currentBattleAdvanced.replayPosition -eq ($currentBattleAdvanced.replayCount-1)) 'current battle automatically advances its recorded events after arrival'
    Click-ExperienceTarget $currentBattleAdvanced 'replay-end';$currentBattleEnd=Capture-Experience '33m-battle-current-result'
    Assert-Experience ($currentBattleEnd.replayPosition -eq ($currentBattleEnd.replayCount-1) -and $currentBattleEnd.replayWorldHash -eq $currentBattle.replayWorldHash) 'current battle result seek preserves the complete settled campaign'
    Click-ExperienceTarget $currentBattleEnd 'replay-outcome-detail';$currentOutcome=Capture-Experience '33m-current-outcome'
    Assert-Experience ($currentOutcome.outcomeOpen -and $currentOutcome.watchingCurrentBattle -and $currentOutcome.outcomeBattleId -eq $watchId -and -not $currentOutcome.replayPlaying -and $currentOutcome.replayWorldHash -eq $currentBattle.replayWorldHash) 'current battle outcome opens the same settled identity without repeating settlement'
    Click-ExperienceTarget $currentOutcome 'outcome-participant-1';$currentFormation=Capture-Experience '33m-current-formation'
    Assert-Experience ($currentFormation.battleFormationOpen -and -not $currentFormation.outcomeOpen -and $currentFormation.battleFormationSelected -eq 1 -and $currentFormation.battleFormationId -eq $watchId -and $currentFormation.watchingCurrentBattle -and $currentFormation.replayWorldHash -eq $currentOutcome.replayWorldHash) 'actual outcome participant opens that frozen card and retains current battle origin'
    Click-ExperienceTarget $currentFormation 'formation-back';$currentOutcome=Capture-Experience '33m-current-formation-return'
    Assert-Experience ($currentOutcome.outcomeOpen -and -not $currentOutcome.battleFormationOpen -and $currentOutcome.watchingCurrentBattle -and $currentOutcome.replayPosition -eq $currentBattleEnd.replayPosition -and $currentOutcome.replayWorldHash -eq $currentBattle.replayWorldHash) 'formation returns to the current outcome before the existing process and map returns'
    Click-ExperienceTarget $currentOutcome 'replay-back';$currentBattleEnd=Capture-Experience '33m-current-outcome-return'
    Assert-Experience (-not $currentBattleEnd.outcomeOpen -and $currentBattleEnd.replayOpen -and $currentBattleEnd.watchingCurrentBattle -and $currentBattleEnd.replayPosition -eq ($currentBattleEnd.replayCount-1) -and $currentBattleEnd.replayWorldHash -eq $currentBattle.replayWorldHash) 'outcome return restores the current battle result before returning to the map'
    Click-ExperienceTarget $currentBattleEnd 'replay-back';$currentBattleMap=Capture-Experience '33n-battle-current-map'
    Assert-Experience ($currentBattleMap.tab -eq '领地' -and -not $currentBattleMap.watchingCurrentBattle -and $currentBattleMap.reportCount -eq ($trainingLand.reportCount+1)) 'current battle exits to the map without another report'
    $currentBattleMap=Set-ExperienceSpeed 0 'battle-repeat-pause'
    $watchHash=$currentBattleMap.battleWorldHash
    for($watchRepeat=0;$watchRepeat -lt 10;$watchRepeat++) {
        Click-ExperienceTarget $currentBattleMap 'army-battle-watch';$currentBattleAgain=Capture-Experience ('33o-current-reopen-'+$watchRepeat)
        Assert-Experience ($currentBattleAgain.replayOpen -and $currentBattleAgain.watchingCurrentBattle -and $currentBattleAgain.replayBattleId -eq $watchId -and $currentBattleAgain.replayPosition -eq ($currentBattleAgain.replayCount-1) -and $currentBattleAgain.battleWorldHash -eq $watchHash) ('current battle reopen '+$watchRepeat+' retains the same result cursor and complete campaign hash')
        Click-ExperienceTarget $currentBattleAgain 'replay-start';$currentBattleReset=Capture-Experience ('33p-current-reset-'+$watchRepeat)
        Assert-Experience ($currentBattleReset.replayPosition -eq 0 -and $currentBattleReset.battleWorldHash -eq $watchHash) ('current battle restart '+$watchRepeat+' only seeks recorded events')
        Click-ExperienceTarget $currentBattleReset 'replay-end';$currentBattleResult=Capture-Experience ('33q-current-result-'+$watchRepeat)
        Click-ExperienceTarget $currentBattleResult 'replay-back';$currentBattleMap=Capture-Experience ('33r-current-exit-'+$watchRepeat)
        Assert-Experience ($currentBattleMap.tab -eq '领地' -and $currentBattleMap.battleWorldHash -eq $watchHash -and $currentBattleMap.reportCount -eq ($trainingLand.reportCount+1)) ('current battle cycle '+$watchRepeat+' does not repeat troop loss capture experience or rewards')
    }
    $state=Set-ExperienceSpeed 60 'training-capture-speed'
    $trainingCaptured=Wait-ExperienceState 'training-land-captured' {param($s) -not $s.orderActive -and $s.tab -eq '领地'}
    Assert-Experience $trainingCaptured.selectedOwned 'training target was occupied by the real battle'
    Assert-ExperienceLand $trainingCaptured
    $trainingCaptured=Set-ExperienceSpeed 1 'training-normal-speed'
    Click-ExperienceTarget $trainingCaptured 'land-training-icon';$trainingReady=Capture-Experience '34-training-land-entry'
    Assert-Experience ($trainingReady.tab -eq '练兵' -and $trainingReady.trainingReturnTab -eq '领地' -and $trainingReady.trainingTarget -eq $trainingCaptured.selected -and [string]::IsNullOrEmpty($trainingReady.trainingProblem)) 'owned land opens ready training for the exact selected tile'
    $trainingComparisonPlan=Set-ExperienceSpeed 0 '34c-training-compare-pause'
    Click-ExperienceTarget $trainingComparisonPlan 'training-compare';$trainingComparison=Capture-Experience '34d-training-compare-open'
    Assert-Experience ($trainingComparison.trainingPickerOpen -and $trainingComparison.trainingCandidate -eq $trainingComparisonPlan.trainingTarget -and $trainingComparison.trainingPreviewTotal -gt 0 -and $trainingComparison.trainingPreviewSeconds -gt (2*$trainingComparison.trainingPreviewMarch) -and $trainingComparison.battleWorldHash -eq $trainingComparisonPlan.battleWorldHash) 'comparison opens with actual capped gain and round-trip time without charging'
    Drag-Experience ($cw*.75) ($ch*.36) ($cw*.80) ($ch*.42);$trainingComparisonDrag=Capture-Experience '34e-training-compare-map-blocked'
    Assert-ExperienceMapStill $trainingComparison $trainingComparisonDrag 'training target comparison'
    Click-ExperienceTarget $trainingComparisonDrag 'training-sort-gain';$trainingComparisonGain=Capture-Experience '34f-training-compare-gain'
    Assert-Experience ($trainingComparisonGain.trainingTargetSort -eq 1 -and $trainingComparisonGain.trainingCandidate -eq $trainingComparison.trainingCandidate -and $trainingComparisonGain.battleWorldHash -eq $trainingComparisonPlan.battleWorldHash) 'gain sort preserves the draft target and campaign'
    Click-ExperienceTarget $trainingComparisonGain 'training-sort-time';$trainingComparisonTime=Capture-Experience '34g-training-compare-time'
    Assert-Experience ($trainingComparisonTime.trainingTargetSort -eq 0 -and $trainingComparisonTime.battleWorldHash -eq $trainingComparisonPlan.battleWorldHash) 'time sort only changes target presentation'
    $pickerList=Get-ExperienceTarget $trainingComparisonTime 'training-picker-list'
    $trainingComparisonVisible=Find-ExperienceScrolledTarget $trainingComparisonTime ('training-picker-target-'+$trainingCaptured.selected) '34h-training-compare-visible' $pickerList.x $pickerList.y
    Click-ExperienceTarget $trainingComparisonVisible ('training-picker-target-'+$trainingCaptured.selected);$trainingComparisonChosen=Capture-Experience '34i-training-compare-chosen'
    Assert-Experience ($trainingComparisonChosen.trainingCandidate -eq $trainingCaptured.selected -and ($trainingComparisonChosen.trainingPreviewAwards | Measure-Object -Sum).Sum -eq $trainingComparisonChosen.trainingPreviewTotal) 'visible land click displays the sum of actual per-general forecasts'
    Click-ExperienceTarget $trainingComparisonChosen 'training-picker-back';$trainingComparisonCancelled=Capture-Experience '34j-training-compare-cancel'
    Assert-Experience (-not $trainingComparisonCancelled.trainingPickerOpen -and $trainingComparisonCancelled.tab -eq '练兵' -and $trainingComparisonCancelled.trainingTarget -eq $trainingComparisonPlan.trainingTarget -and $trainingComparisonCancelled.trainingReturnTab -eq '领地' -and $trainingComparisonCancelled.battleWorldHash -eq $trainingComparisonPlan.battleWorldHash) 'comparison back preserves original plan and map origin without charges'
    Click-ExperienceTarget $trainingComparisonCancelled 'training-compare';$trainingComparisonAgain=Capture-Experience '34k-training-compare-again'
    Click-ExperienceTarget $trainingComparisonAgain 'training-picker-apply';$trainingComparisonApplied=Capture-Experience '34l-training-compare-apply'
    Assert-Experience (-not $trainingComparisonApplied.trainingPickerOpen -and -not $trainingComparisonApplied.confirmationOpen -and -not $trainingComparisonApplied.orderActive -and $trainingComparisonApplied.trainingTarget -eq $trainingCaptured.selected -and $trainingComparisonApplied.battleWorldHash -eq $trainingComparisonPlan.battleWorldHash) 'choosing a land returns to preparation and still requires explicit departure confirmation'
    $trainingReady=Set-ExperienceSpeed 1 '34m-training-compare-resume'
    Click-ExperienceTarget $trainingReady 'training-formation';$trainingPlanFormation=Capture-Experience '34a-training-plan-formation'
    Assert-Experience ($trainingPlanFormation.tab -eq '武将' -and $trainingPlanFormation.commands -eq $trainingReady.commands -and $trainingPlanFormation.generalExperienceTotal -eq $trainingReady.generalExperienceTotal) 'training preparation opens real formation editing without early charge'
    Click-ExperienceTarget $trainingPlanFormation 'training-formation-back';$trainingPlanReturn=Capture-Experience '34b-training-plan-return'
    Assert-Experience ($trainingPlanReturn.tab -eq '练兵' -and $trainingPlanReturn.trainingTarget -eq $trainingReady.trainingTarget -and $trainingPlanReturn.trainingReturnTab -eq '领地' -and $trainingPlanReturn.selected -eq $trainingCaptured.selected) 'formation returns to the same training plan and original land entry'
    $trainingReady=$trainingPlanReturn
    Click-ExperienceTarget $trainingReady ('training-target-'+$trainingCaptured.selected);$trainingChosen=Capture-Experience '35-training-target-selected'
    Assert-Experience ($trainingChosen.trainingTarget -eq $trainingCaptured.selected -and $trainingChosen.trainingReward -gt 0) 'visible target selection exposes positive configured reward'
    Click-ExperienceTarget $trainingChosen 'training-confirm';$trainingConfirm=Capture-Experience '36-training-confirmation'
    Assert-Experience ($trainingConfirm.confirmationOpen -and -not $trainingConfirm.orderActive -and $trainingConfirm.commands -eq $trainingChosen.commands) 'confirmation shows before any order or charge'
    Click-ExperienceTarget $trainingConfirm 'confirm-no';$trainingDeclined=Capture-Experience '37-training-declined'
    Assert-Experience (-not $trainingDeclined.confirmationOpen -and -not $trainingDeclined.orderActive -and $trainingDeclined.commands -eq $trainingChosen.commands) 'declining training keeps commands and idle army'
    # Pause before approval so screenshot latency cannot finish this cancellation fixture.
    $trainingDeclined=Set-ExperienceSpeed 0 '37a-training-before-start-pause'
    Click-ExperienceTarget $trainingDeclined 'training-confirm';$trainingConfirm=Capture-Experience '38-training-confirm-again'
    $yes=Get-ExperienceTarget $trainingConfirm 'confirm-yes';Click-Experience $yes.x $yes.y
    $trainingStarted=Capture-Experience '39-training-started'
    Assert-Experience ($trainingStarted.trainingActive -and $trainingStarted.orderAction -eq 3 -and $trainingStarted.commands -eq ($trainingChosen.commands-2) -and $trainingStarted.generalExperienceTotal -eq $trainingChosen.generalExperienceTotal) 'confirm once creates training order and debits two commands without early experience'
    Click-Experience $yes.x $yes.y;$trainingRepeated=Capture-Experience '40-training-repeat-click'
    Assert-Experience ($trainingRepeated.trainingActive -and $trainingRepeated.commands -eq $trainingStarted.commands -and $trainingRepeated.trainingHistoryCount -eq $trainingStarted.trainingHistoryCount -and $trainingRepeated.generalExperienceTotal -eq $trainingStarted.generalExperienceTotal) 'repeated confirmation position cannot create another charge or reward'
    Click-ExperienceTarget $trainingRepeated 'training-back';$trainingLandReturn=Capture-Experience '41-training-land-return'
    Assert-Experience ($trainingLandReturn.tab -eq '领地' -and $trainingLandReturn.selected -eq $trainingCaptured.selected -and $trainingLandReturn.trainingActive) 'leaving training preserves active order and exact selected land'
    Assert-Experience ($trainingLandReturn.landHasTerrainPatch) 'captured training field must retain a resource patch and exercise meadow picking while busy'
    $trainingLandReturn=Check-ExperienceLandReturn $trainingLandReturn '41a-land-busy-panel'
    Click-ExperienceTarget $trainingLandReturn 'land-training-icon';$trainingBusy=Capture-Experience '42-training-busy-reentry'
    Assert-Experience ($trainingBusy.tab -eq '练兵' -and $trainingBusy.trainingActive -and $trainingBusy.trainingTarget -eq $trainingCaptured.selected -and $trainingBusy.commands -eq $trainingStarted.commands) 'busy reentry displays the same active training order'
    $trainingBusy=Set-ExperienceSpeed 0 '42a-training-execution-pause'
    Assert-Experience ($trainingBusy.trainingExecutionVisible -and $trainingBusy.trainingUntilHome -gt $trainingBusy.trainingUntilAward -and $trainingBusy.trainingUntilAward -gt 0 -and $trainingBusy.trainingExecutionAward -eq 0) 'active execution displays separate future credit and return clocks without early reward'
    $executionScroll=Get-ExperienceTarget $trainingBusy 'training-execution-scroll'
    Drag-Experience $executionScroll.x ($executionScroll.y+35) $executionScroll.x ($executionScroll.y-35)
    $executionDragged=Capture-Experience '42b-training-execution-scroll'
    Assert-Experience ($executionDragged.trainingExecutionVisible -and $executionDragged.battleWorldHash -eq $trainingBusy.battleWorldHash -and $executionDragged.focusX -eq $trainingBusy.focusX -and $executionDragged.focusZ -eq $trainingBusy.focusZ -and -not $executionDragged.confirmationOpen) 'execution content drag preserves frozen campaign and blocks the map and cancel button'
    $trainingBusy=$executionDragged
    Click-ExperienceTarget $trainingBusy 'training-cancel';$trainingCancelConfirm=Capture-Experience '43-training-cancel-confirm'
    Assert-Experience $trainingCancelConfirm.confirmationOpen 'cancelling training requires the visible confirmation'
    Click-ExperienceTarget $trainingCancelConfirm 'confirm-yes';$trainingCancelled=Capture-Experience '44-training-cancelled'
    Assert-Experience ($trainingCancelled.trainingReceiptStatus -eq 'cancelled' -and $trainingCancelled.trainingReceiptAward -eq 0 -and $trainingCancelled.trainingCompletionCount -eq 0 -and $trainingCancelled.commands -eq $trainingStarted.commands -and $trainingCancelled.generalExperienceTotal -eq $trainingChosen.generalExperienceTotal) 'cancel issues no experience and does not refund the spent commands'
    Assert-Experience ($trainingCancelled.trainingExecutionVisible -and $trainingCancelled.trainingExecutionStage -eq 2 -and $trainingCancelled.trainingUntilAward -eq 0 -and $trainingCancelled.trainingUntilHome -gt 0 -and $trainingCancelled.trainingExecutionAward -eq 0) 'cancelled execution shows only its real return countdown and zero reward'
    $state=Set-ExperienceSpeed 60 'training-cancel-return-speed'
    $state=Wait-ExperienceState 'training-cancel-returned' {param($s) -not $s.orderActive}
    $state=Set-ExperienceSpeed 1 'training-cancel-normal-speed'
    Click-ExperienceTarget $state 'training-back';$state=Capture-Experience '45-training-cancel-back'
    Click-ExperienceTarget $state 'city-open';$state=Capture-Experience '46-training-city-again'
    Click-ExperienceTarget $state 'city-training';$trainingSecond=Capture-Experience '47-training-second-ready'
    Assert-Experience ($trainingSecond.trainingReturnTab -eq '主城' -and $trainingSecond.trainingTarget -eq $trainingCaptured.selected -and [string]::IsNullOrEmpty($trainingSecond.trainingProblem)) 'city entry selects the usable captured training land'
    Click-ExperienceTarget $trainingSecond 'training-confirm';$state=Capture-Experience '48-training-complete-confirm'
    Click-ExperienceTarget $state 'confirm-yes';$trainingCompleteStart=Capture-Experience '49-training-complete-start'
    Assert-Experience ($trainingCompleteStart.trainingActive -and $trainingCompleteStart.commands -eq ($trainingSecond.commands-2)) 'second explicitly confirmed training debits once'
    # Observe the 60-second work phase at normal speed so a slow screenshot write
    # cannot skip the entire phase between captures. Speed up only after it is proven.
    $working=Wait-ExperienceState 'training-working' {param($s) $s.trainingActive -and $s.orderPhase -eq 2 -and -not $s.orderReturning} 45
    Assert-Experience ($working.generalExperienceTotal -eq $trainingSecond.generalExperienceTotal -and $working.trainingCompletionCount -eq 0) 'working phase grants no experience before completion'
    Assert-Experience ($working.trainingExecutionVisible -and $working.trainingExecutionStage -eq 1 -and $working.trainingUntilAward -eq $working.trainingPhaseRemaining -and $working.trainingUntilHome -gt $working.trainingUntilAward) 'working view credits at the actual work timer and keeps return time separate'
    $state=Set-ExperienceSpeed 60 'training-finish-speed'
    $trainingFinished=Wait-ExperienceState 'training-finished' {param($s) -not $s.orderActive -and $s.trainingCompletionCount -eq 1}
    $trainingFinished=Set-ExperienceSpeed 1 'training-finished-normal-speed'
    Assert-Experience ($trainingFinished.trainingReceiptStatus -eq 'completed' -and $trainingFinished.trainingReceiptTarget -eq $trainingCaptured.selected -and $trainingFinished.trainingReceiptAward -gt 0 -and $trainingFinished.trainingHistoryCount -eq 2 -and ($trainingFinished.generalExperienceTotal-$trainingSecond.generalExperienceTotal) -eq $trainingFinished.trainingReceiptAward) 'completion and return issue one matching real experience receipt'
    Assert-Experience ($trainingFinished.trainingReceiptAward -eq $trainingSecond.trainingPreviewTotal) 'actual completed training matches the displayed capped preparation forecast'
    Click-ExperienceTarget $trainingFinished 'training-back';$state=Capture-Experience '50-training-finished-city'
    Click-ExperienceTarget $state 'city-training';$trainingReceiptReopen=Capture-Experience '51-training-receipt-reopen'
    Assert-Experience ($trainingReceiptReopen.trainingCompletionCount -eq 1 -and $trainingReceiptReopen.trainingHistoryCount -eq 2 -and $trainingReceiptReopen.generalExperienceTotal -eq $trainingFinished.generalExperienceTotal -and $trainingReceiptReopen.commands -eq $trainingFinished.commands) 'reopening completed training neither repeats receipt nor grants another reward'
    $historyPlan=Set-ExperienceSpeed 0 '51a-training-history-pause'
    Click-ExperienceTarget $historyPlan 'training-history';$historyCompleted=Capture-Experience '51b-training-history-completed'
    Assert-Experience ($historyCompleted.trainingHistoryOpen -and $historyCompleted.trainingHistorySelected -eq 1 -and $historyCompleted.trainingHistoryStatus -eq 'completed' -and $historyCompleted.trainingHistoryTarget -eq $trainingCaptured.selected -and ($historyCompleted.trainingHistoryAwards | Measure-Object -Sum).Sum -eq $trainingFinished.trainingReceiptAward -and $historyCompleted.battleWorldHash -eq $historyPlan.battleWorldHash) 'actual latest receipt shows its recorded per-participant gains without awarding again'
    $recordedParticipants=$historyCompleted.trainingHistoryParticipants -join ','
    $historyPortrait=Get-ExperienceTarget $historyCompleted 'training-history-general-0'
    Drag-Experience ($historyPortrait.x-20) $historyPortrait.y ($historyPortrait.x+30) ($historyPortrait.y+40)
    $historyDrag=Capture-Experience '51c-training-history-drag'
    Assert-Experience ([string]::IsNullOrEmpty($historyDrag.cardDetail) -and $historyDrag.trainingHistorySelected -eq 1 -and $historyDrag.battleWorldHash -eq $historyPlan.battleWorldHash) 'dragging a recorded participant does not open a card or change rewards'
    Click-ExperienceTarget $historyDrag 'training-history-general-0';$historyCard=Capture-Experience '51d-training-history-card'
    Assert-Experience (-not [string]::IsNullOrEmpty($historyCard.cardDetail) -and $historyCard.trainingHistoryOpen -and $historyCard.trainingHistorySelected -eq 1 -and $historyCard.battleWorldHash -eq $historyPlan.battleWorldHash) 'recorded participant opens a read-only card reference'
    Click-ExperienceTarget $historyCard 'card-close';$historyCardBack=Capture-Experience '51e-training-history-card-back'
    Assert-Experience ($historyCardBack.trainingHistorySelected -eq 1 -and ($historyCardBack.trainingHistoryParticipants -join ',') -eq $recordedParticipants) 'card close preserves the selected recorded participants'
    Click-ExperienceTarget $historyCardBack 'training-history-row-0';$historyCancelled=Capture-Experience '51f-training-history-cancelled'
    Assert-Experience ($historyCancelled.trainingHistoryStatus -eq 'cancelled' -and ($historyCancelled.trainingHistoryAwards | Measure-Object -Sum).Sum -eq 0 -and $historyCancelled.battleWorldHash -eq $historyPlan.battleWorldHash) 'older cancelled receipt retains zero gain and cannot repeat its order'
    Click-ExperienceTarget $historyCancelled 'training-history-back';$historyPlanBack=Capture-Experience '51g-training-history-plan'
    Assert-Experience (-not $historyPlanBack.trainingHistoryOpen -and $historyPlanBack.trainingTarget -eq $historyPlan.trainingTarget -and $historyPlanBack.trainingReturnTab -eq $historyPlan.trainingReturnTab -and $historyPlanBack.battleWorldHash -eq $historyPlan.battleWorldHash) 'history returns to the same plan and entry point without changing the world'
    # Generate further receipts using visible confirmations and real cancellations.
    # This makes the history list genuinely scrollable at both Windows resolutions.
    for($historyOrder=0;$historyOrder -lt 5;$historyOrder++) {
        Click-ExperienceTarget $historyPlanBack 'training-confirm';$historyOrderConfirm=Capture-Experience ('51h-history-start-confirm-'+$historyOrder)
        Click-ExperienceTarget $historyOrderConfirm 'confirm-yes';$historyOrderStarted=Capture-Experience ('51i-history-started-'+$historyOrder)
        Assert-Experience ($historyOrderStarted.trainingActive -and $historyOrderStarted.commands -eq ($historyPlanBack.commands-2)) 'history fixture is a separately approved real order'
        Click-ExperienceTarget $historyOrderStarted 'training-cancel';$historyRecall=Capture-Experience ('51j-history-recall-confirm-'+$historyOrder)
        Click-ExperienceTarget $historyRecall 'confirm-yes';$historyRecallDone=Capture-Experience ('51k-history-recalled-'+$historyOrder)
        Assert-Experience ($historyRecallDone.trainingReceiptStatus -eq 'cancelled' -and $historyRecallDone.trainingReceiptAward -eq 0 -and $historyRecallDone.generalExperienceTotal -eq $trainingFinished.generalExperienceTotal) 'explicit cancelled order creates only a zero-gain receipt'
        Click-ExperienceTarget $historyRecallDone 'training-result';$historyImmediate=Capture-Experience ('51l-history-returning-result-'+$historyOrder)
        Assert-Experience ($historyImmediate.trainingHistoryOpen -and $historyImmediate.trainingHistoryStatus -eq 'cancelled' -and $historyImmediate.battleWorldHash -eq $historyRecallDone.battleWorldHash) 'returning army result button opens its settled receipt without recalling again'
        Click-ExperienceTarget $historyImmediate 'training-history-back';$null=Capture-Experience ('51m-history-result-back-'+$historyOrder)
        $null=Set-ExperienceSpeed 60 ('51n-history-return-speed-'+$historyOrder)
        $null=Wait-ExperienceState ('51o-history-returned-'+$historyOrder) {param($s) -not $s.orderActive}
        $historyPlanBack=Set-ExperienceSpeed 0 ('51p-history-paused-'+$historyOrder)
    }
    Click-ExperienceTarget $historyPlanBack 'training-history';$historyArchive=Capture-Experience '51q-training-history-archive'
    Assert-Experience ($historyArchive.trainingHistoryCount -eq 7 -and $historyArchive.trainingHistorySelected -eq 6) 'history archive includes all seven actual training orders newest first'
    $historyList=Get-ExperienceTarget $historyArchive 'training-history-list'
    $historyOldestVisible=Find-ExperienceScrolledTarget $historyArchive 'training-history-row-0' '51r-history-oldest-scroll' $historyList.x $historyList.y
    Assert-Experience ($historyOldestVisible.trainingHistoryScrollY -gt $historyArchive.trainingHistoryScrollY -and $historyOldestVisible.battleWorldHash -eq $historyPlanBack.battleWorldHash) 'real mouse wheel reveals the oldest receipt without changing the campaign'
    Click-ExperienceTarget $historyOldestVisible 'training-history-row-0';$historyOldest=Capture-Experience '51s-training-history-oldest'
    Assert-Experience ($historyOldest.trainingHistorySelected -eq 0 -and $historyOldest.trainingHistoryStatus -eq 'cancelled' -and $historyOldest.trainingHistoryAwards.Count -eq 3 -and ($historyOldest.trainingHistoryParticipants -join ',') -eq $recordedParticipants -and $historyOldest.battleWorldHash -eq $historyPlanBack.battleWorldHash) 'scrolling and choosing the oldest real receipt preserves its original identity and zero reward'
    Click-ExperienceTarget $historyOldest 'training-history-back';$historyArchiveBack=Capture-Experience '51t-training-history-archive-back'
    Assert-Experience (-not $historyArchiveBack.trainingHistoryOpen -and $historyArchiveBack.trainingTarget -eq $historyPlan.trainingTarget -and $historyArchiveBack.battleWorldHash -eq $historyPlanBack.battleWorldHash) 'archive back leaves the original training plan actionable and unchanged'
    $trainingReceiptReopen=Set-ExperienceSpeed 1 '51u-training-history-resume'
    Click-ExperienceTarget $trainingReceiptReopen 'training-back';$state=Capture-Experience '52-training-quest-city'
    Click-ExperienceTarget $state 'city-quests';$trainingQuest=Capture-Experience '53-training-quest-entry'
    Click-ExperienceTarget $trainingQuest 'quest-training-go';$trainingQuestPage=Capture-Experience '54-training-quest-go'
    Assert-Experience ($trainingQuestPage.tab -eq '练兵' -and $trainingQuestPage.trainingReturnTab -eq '主城' -and $trainingQuestPage.trainingTarget -eq $trainingCaptured.selected -and [string]::IsNullOrEmpty($trainingQuestPage.trainingProblem) -and $trainingQuestPage.claimedQuests -eq $trainingQuest.claimedQuests) 'quest growth shortcut reaches actionable training without changing quest claims'
    Click-ExperienceTarget $trainingQuestPage 'training-formation';$trainingFormation=Capture-Experience '55-training-formation'
    Assert-Experience ($trainingFormation.tab -eq '武将' -and -not $trainingFormation.orderActive -and $trainingFormation.commands -eq $trainingReceiptReopen.commands -and $trainingFormation.generalExperienceTotal -eq $trainingFinished.generalExperienceTotal) 'training formation control opens the actual roster without charge'
    Click-ExperienceTarget $trainingFormation 'training-formation-back';$trainingFormationReturn=Capture-Experience '56-training-formation-return'
    Assert-Experience ($trainingFormationReturn.tab -eq '练兵' -and $trainingFormationReturn.trainingTarget -eq $trainingCaptured.selected -and $trainingFormationReturn.trainingReturnTab -eq '主城') 'formation return preserves quest training origin and selected target'
    Click-ExperienceTarget $trainingFormationReturn 'training-back';$questTrainingReturn=Capture-Experience '57-training-quest-return'
    Assert-ExperienceCityPage $questTrainingReturn 'quests'
    Assert-Experience ($questTrainingReturn.questChapter -eq $trainingQuest.questChapter -and $questTrainingReturn.questCompleted -eq $trainingQuest.questCompleted -and $questTrainingReturn.claimedQuests -eq $trainingQuest.claimedQuests -and $questTrainingReturn.selected -eq $trainingQuest.selected) 'quest training returns to the original chapter and map selection without claims'
    Click-ExperienceTarget $questTrainingReturn 'city-back';$finalCity=Capture-Experience '58-quest-city-return'
    Assert-ExperienceCityPage $finalCity 'overview'
    Click-ExperienceTarget $finalCity 'city-back';$finalMap=Capture-Experience '59-city-map-return'
    Assert-Experience ($finalMap.tab -eq '领地' -and -not $finalMap.cityOpen -and $finalMap.selected -eq $finalCity.selected) 'city back navigation closes the overview to the original map selection'
    Click-ExperienceTarget $finalMap 'quest-open';$trackerQuest=Capture-Experience '60-map-quest-entry'
    Assert-ExperienceCityPage $trackerQuest 'quests'
    Assert-Experience ($trackerQuest.claimedQuests -eq $finalMap.claimedQuests -and $trackerQuest.selected -eq $finalMap.selected) 'map task title opens the independent chapter page without claiming or selecting another tile'
    Click-ExperienceTarget $trackerQuest 'reports-tab';$battleReport=Capture-Experience '61-battle-report'
    $battleReport=Set-ExperienceSpeed 0 '61-battle-report-paused'
    Assert-Experience ($battleReport.tab -eq '战报' -and $battleReport.reportCount -gt 0) 'real completed capture is available in battle reports'
    $archiveHash=$battleReport.battleWorldHash
    foreach($archiveId in @('report-filter-0','report-filter-1','report-filter-2','report-filter-3','report-list','report-text','report-replay','report-outcome','report-formation','report-location')) {
        $archiveControl=Get-ExperienceTarget $battleReport $archiveId
        Assert-Experience ($archiveControl.x-$archiveControl.w*.5 -ge 0 -and $archiveControl.y-$archiveControl.h*.5 -ge 0 -and $archiveControl.x+$archiveControl.w*.5 -le $cw -and $archiveControl.y+$archiveControl.h*.5 -le $ch) ('archive action is fully inside the actual client: '+$archiveId)
    }
    Click-ExperienceTarget $battleReport 'report-filter-1';$archiveCaptured=Capture-Experience '61-archive-captured'
    Assert-Experience ($archiveCaptured.reportFilter -eq 1 -and $archiveCaptured.reportFilteredCount -eq $archiveCaptured.reportCapturedCount -and $archiveCaptured.reportFilteredCount -gt 0 -and $archiveCaptured.reportSelectedCaptured -and $archiveCaptured.battleWorldHash -eq $archiveHash) 'captured filter selects a real occupied-land result without changing campaign state'
    $archiveRows=@($archiveCaptured.targets | Where-Object {$_.id -like 'report-row-*'})
    Assert-Experience ($archiveRows.Count -gt 0) 'filtered list exposes an actually drawn record row'
    $archiveRow=$archiveRows[$archiveRows.Count-1]
    Click-ExperienceTarget $archiveCaptured $archiveRow.id;$archiveChosen=Capture-Experience '61-archive-record'
    Assert-Experience ($archiveChosen.reportSelectedIndex -eq [int]$archiveRow.id.Substring(11) -and $archiveChosen.reportSelectedReplay -and $archiveChosen.reportSelectedRounds -gt 0 -and $archiveChosen.reportSelectedInitial -gt 0 -and $archiveChosen.battleWorldHash -eq $archiveHash) 'clicking a visible summary selects its original source record with actual frozen troops and rounds'
    $archiveList=Get-ExperienceTarget $archiveChosen 'report-list'
    Focus-Experience;Move-Experience $archiveList.x $archiveList.y;Assert-ExperiencePointer $archiveList.x $archiveList.y
    [EDExperiencePointer]::mouse_event(0x0800,0,0,[uint32](4294967296-240),[UIntPtr]::Zero)
    Start-Sleep -Milliseconds 200;Assert-ExperiencePointer $archiveList.x $archiveList.y
    $archiveScrolled=Capture-Experience '61-archive-scroll'
    $archiveScrollMoved=if($archiveChosen.reportListScrollMaximum -gt 0){$archiveScrolled.reportListScrollY -gt $archiveChosen.reportListScrollY}else{[Math]::Abs($archiveScrolled.reportListScrollY) -lt .001}
    Assert-Experience ($archiveScrollMoved -and $archiveScrolled.reportSelectedBattleId -eq $archiveChosen.reportSelectedBattleId -and $archiveScrolled.battleWorldHash -eq $archiveHash) 'actual report-list wheel respects scroll bounds and does not select another battle'
    $archiveDrawnRows=@($archiveScrolled.targets | Where-Object {$_.id -like 'report-row-*'})
    Assert-Experience ($archiveDrawnRows.Count -gt 0) 'scroll leaves actual report rows available'
    $archiveDragRow=$archiveDrawnRows[0]
    Drag-Experience ($archiveDragRow.x-40) $archiveDragRow.y ($archiveDragRow.x+40) ($archiveDragRow.y-12);$archiveDragged=Capture-Experience '61-archive-drag'
    Assert-Experience ($archiveDragged.reportSelectedBattleId -eq $archiveChosen.reportSelectedBattleId -and $archiveDragged.selected -eq $archiveChosen.selected -and [Math]::Abs($archiveDragged.focusX-$archiveChosen.focusX) -lt .01 -and [Math]::Abs($archiveDragged.focusZ-$archiveChosen.focusZ) -lt .01 -and $archiveDragged.battleWorldHash -eq $archiveHash) 'dragging a visible report row neither selects another battle nor moves the map'
    Click-ExperienceTarget $archiveDragged 'report-replay';$archiveReplay=Capture-Experience '61-archive-replay'
    Assert-Experience ($archiveReplay.replayOpen -and $archiveReplay.replayBattleId -eq $archiveChosen.reportSelectedBattleId -and $archiveReplay.replayWorldHash -eq $archiveHash) 'filtered archive opens the exact selected frozen battle'
    Click-ExperienceTarget $archiveReplay 'replay-back';$archiveReturned=Capture-Experience '61-archive-return'
    Assert-Experience ($archiveReturned.tab -eq '战报' -and $archiveReturned.reportFilter -eq 1 -and $archiveReturned.reportSelectedBattleId -eq $archiveChosen.reportSelectedBattleId -and [Math]::Abs($archiveReturned.reportListScrollY-$archiveDragged.reportListScrollY) -lt .01 -and $archiveReturned.battleWorldHash -eq $archiveHash) 'replay return preserves the archive filter, exact selection and scroll without new effects'
    Click-ExperienceTarget $archiveReturned 'report-filter-2';$archiveNotCaptured=Capture-Experience '61-archive-not-captured'
    Assert-Experience ($archiveNotCaptured.reportFilter -eq 2 -and $archiveNotCaptured.reportFilteredCount -eq $archiveNotCaptured.reportNotCapturedCount -and -not $archiveNotCaptured.reportSelectedCaptured -and $archiveNotCaptured.battleWorldHash -eq $archiveHash) 'not-captured filter never presents an occupied result'
    Click-ExperienceTarget $archiveNotCaptured 'report-filter-3';$archiveLegacy=Capture-Experience '61-archive-legacy'
    Assert-Experience ($archiveLegacy.reportFilter -eq 3 -and $archiveLegacy.reportFilteredCount -eq $archiveLegacy.reportLegacyCount -and -not $archiveLegacy.reportSelectedReplay -and @($archiveLegacy.targets | Where-Object {$_.id -in @('report-replay','report-outcome','report-formation','report-location')}).Count -eq 0 -and $archiveLegacy.battleWorldHash -eq $archiveHash) 'legacy or empty filter exposes no fabricated replay, reward, formation or location action'
    if($archiveLegacy.reportFilteredCount -eq 0) {
        Click-ExperienceTarget $archiveLegacy 'report-show-all'
    } else {Click-ExperienceTarget $archiveLegacy 'report-filter-0'}
    $battleReport=Capture-Experience '61-archive-show-all'
    Assert-Experience ($battleReport.reportFilter -eq 0 -and $battleReport.reportFilteredCount -eq $battleReport.reportCount -and $battleReport.battleWorldHash -eq $archiveHash) 'visible show-all action restores the complete archive without settlement'
    Click-ExperienceTarget $battleReport 'report-row-0';$battleReport=Capture-Experience '61-archive-newest'
    Click-ExperienceTarget $battleReport 'report-formation';$reportFormation=Capture-Experience '61-formation-report'
    Assert-Experience ($reportFormation.battleFormationOpen -and $reportFormation.battleFormationFromReport -and $reportFormation.battleFormationSelected -eq 0 -and $reportFormation.battleFormationParticipants.Count -eq 3 -and -not $reportFormation.replayPlaying -and $reportFormation.replayWorldHash -eq $battleReport.battleWorldHash) 'report opens a frozen formation directly without changing the campaign'
    foreach($formationTargetId in @('formation-back','formation-participant-0','formation-participant-1','formation-participant-2','formation-next','formation-previous','formation-content')) {
        $formationTarget=Get-ExperienceTarget $reportFormation $formationTargetId
        Assert-Experience ($formationTarget.x-$formationTarget.w*.5 -ge 0 -and $formationTarget.y-$formationTarget.h*.5 -ge 0 -and $formationTarget.x+$formationTarget.w*.5 -le $cw -and $formationTarget.y+$formationTarget.h*.5 -le $ch) ('formation control is fully inside the client: '+$formationTargetId)
    }
    $formationOther=Get-ExperienceTarget $reportFormation 'formation-participant-1'
    Drag-Experience ($formationOther.x-55) $formationOther.y ($formationOther.x+55) ($formationOther.y+8);$formationDrag=Capture-Experience '61-formation-card-drag'
    Assert-Experience ($formationDrag.battleFormationSelected -eq 0 -and $formationDrag.replayWorldHash -eq $reportFormation.replayWorldHash) 'dragging within another participant does not select that card'
    Click-ExperienceTarget $formationDrag 'formation-participant-2';$formationThird=Capture-Experience '61-formation-third'
    Assert-Experience ($formationThird.battleFormationSelected -eq 2 -and $formationThird.battleFormationUid -eq $formationThird.battleFormationParticipants[2] -and $formationThird.battleFormationUid -ne $reportFormation.battleFormationUid -and $formationThird.battleFormationLevel -gt 0 -and $formationThird.battleFormationAttack -ge 0 -and $formationThird.replayWorldHash -eq $reportFormation.replayWorldHash) 'visible third card selects its frozen identity, level and base attack'
    Click-ExperienceTarget $formationThird 'formation-next';$formationAtEnd=Capture-Experience '61-formation-end'
    Assert-Experience ($formationAtEnd.battleFormationSelected -eq 2) 'next participant is bounded at the final recorded card'
    Click-ExperienceTarget $formationAtEnd 'formation-previous';$formationPrevious=Capture-Experience '61-formation-previous'
    Assert-Experience ($formationPrevious.battleFormationSelected -eq 1 -and $formationPrevious.battleFormationUid -eq $formationPrevious.battleFormationParticipants[1]) 'previous selects the real middle participant'
    Click-ExperienceTarget $formationPrevious 'formation-next';$formationNext=Capture-Experience '61-formation-next'
    Assert-Experience ($formationNext.battleFormationSelected -eq 2) 'next returns to the last recorded participant'
    $formationContent=Get-ExperienceTarget $formationNext 'formation-content'
    Drag-Experience ($formationContent.x-25) ($formationContent.y+25) ($formationContent.x+25) ($formationContent.y-25);$formationContentDrag=Capture-Experience '61-formation-map-block'
    Assert-Experience ($formationContentDrag.battleFormationOpen -and $formationContentDrag.battleFormationSelected -eq 2 -and $formationContentDrag.selected -eq $reportFormation.selected -and [Math]::Abs($formationContentDrag.focusX-$reportFormation.focusX) -lt .01 -and [Math]::Abs($formationContentDrag.focusZ-$reportFormation.focusZ) -lt .01 -and $formationContentDrag.replayWorldHash -eq $reportFormation.replayWorldHash) 'formation content drag cannot move the map or change settled rewards'
    if($formationContentDrag.battleFormationScrollMaximum -gt 0) {
        Focus-Experience;Move-Experience $formationContent.x $formationContent.y;Assert-ExperiencePointer $formationContent.x $formationContent.y
        [EDExperiencePointer]::mouse_event(0x0800,0,0,[uint32](4294967296-480),[UIntPtr]::Zero)
        Start-Sleep -Milliseconds 200;Assert-ExperiencePointer $formationContent.x $formationContent.y
        $formationScrolled=Capture-Experience '61-formation-scrolled'
        Assert-Experience ($formationScrolled.battleFormationScrollY -gt 0 -and $formationScrolled.battleFormationSelected -eq 2 -and $formationScrolled.replayWorldHash -eq $reportFormation.replayWorldHash) 'real wheel reveals clipped formation details without changing the frozen card or campaign'
    } else {$formationScrolled=$formationContentDrag}
    Click-ExperienceTarget $formationScrolled 'formation-back';$battleReport=Capture-Experience '61-formation-report-return'
    Assert-Experience ($battleReport.tab -eq '战报' -and -not $battleReport.battleFormationOpen -and $battleReport.battleWorldHash -eq $reportFormation.replayWorldHash) 'direct formation returns directly to the same report'
    Click-ExperienceTarget $battleReport 'report-outcome';$reportOutcome=Capture-Experience '61a-report-outcome'
    $outcomeAwards=($reportOutcome.outcomeParticipantExperience | Measure-Object -Sum).Sum
    Assert-Experience ($reportOutcome.outcomeOpen -and $reportOutcome.replayPosition -eq 0 -and $reportOutcome.outcomeBattleId -eq $reportOutcome.replayBattleId -and $reportOutcome.replayWorldHash -eq $battleReport.battleWorldHash -and $reportOutcome.outcomeExperience -eq $outcomeAwards -and $reportOutcome.outcomeParticipantIds.Count -gt 0) 'report outcome displays actual per-participant awards from the recorded battle without advancing its playback cursor'
    Assert-Experience (($reportOutcome.outcomeInitial-$reportOutcome.outcomeLoss+$reportOutcome.outcomeRecovery) -eq $reportOutcome.outcomeFinal -and $reportOutcome.outcomeCaptured -and $reportOutcome.outcomeDefenderFinal -eq 0) 'outcome troop ledger balances and reflects the actual occupied target'
    foreach($outcomeTargetId in @('outcome-return','outcome-play','outcome-location')) {
        $outcomeTarget=Get-ExperienceTarget $reportOutcome $outcomeTargetId
        Assert-Experience ($outcomeTarget.x-$outcomeTarget.w*.5 -ge 0 -and $outcomeTarget.y-$outcomeTarget.h*.5 -ge 0 -and $outcomeTarget.x+$outcomeTarget.w*.5 -le $cw -and $outcomeTarget.y+$outcomeTarget.h*.5 -le $ch) ('outcome action is fully inside the client: '+$outcomeTargetId)
    }
    $outcomeContent=Get-ExperienceTarget $reportOutcome 'outcome-content'
    Drag-Experience ($outcomeContent.x-40) ($outcomeContent.y+20) ($outcomeContent.x+20) ($outcomeContent.y-35);$outcomeDrag=Capture-Experience '61b-outcome-map-block'
    Assert-Experience ($outcomeDrag.outcomeOpen -and $outcomeDrag.selected -eq $reportOutcome.selected -and [Math]::Abs($outcomeDrag.focusX-$reportOutcome.focusX) -lt .01 -and [Math]::Abs($outcomeDrag.focusZ-$reportOutcome.focusZ) -lt .01 -and $outcomeDrag.replayWorldHash -eq $reportOutcome.replayWorldHash) 'dragging outcome content cannot move the map or repeat battle effects'
    Click-ExperienceTarget $outcomeDrag 'outcome-return';$outcomeEvent=Capture-Experience '61c-outcome-event-return'
    Assert-Experience ($outcomeEvent.replayOpen -and -not $outcomeEvent.outcomeOpen -and $outcomeEvent.replayPosition -eq 0 -and $outcomeEvent.replayWorldHash -eq $reportOutcome.replayWorldHash) 'outcome return keeps the same preparation event'
    Click-ExperienceTarget $outcomeEvent 'replay-back';$battleReport=Capture-Experience '61d-outcome-report-return'
    Assert-Experience ($battleReport.tab -eq '战报' -and $battleReport.battleWorldHash -eq $reportOutcome.replayWorldHash) 'second return restores the report without granting rewards'
    Click-ExperienceTarget $battleReport 'report-replay';$replayStart=Capture-Experience '62-replay-start'
    Assert-Experience ($replayStart.replayOpen -and $replayStart.replayPosition -eq 0 -and $replayStart.replayCount -gt 2 -and -not $replayStart.replayPlaying -and -not [string]::IsNullOrEmpty($replayStart.replayBattleId)) 'report action opens the recorded battle at its frozen initial state'
    $replayHash=$replayStart.replayWorldHash
    Click-ExperienceTarget $replayStart 'replay-step';$replayStep=Capture-Experience '63-replay-event'
    Assert-Experience ($replayStep.replayPosition -eq 1 -and $replayStep.replayWorldHash -eq $replayHash) 'next event changes playback cursor without changing campaign JSON hash'
    Click-ExperienceTarget $replayStep 'replay-outcome';$midOutcome=Capture-Experience '63-outcome-mid-event'
    Assert-Experience ($midOutcome.outcomeOpen -and $midOutcome.replayPosition -eq 1 -and $midOutcome.replayWorldHash -eq $replayHash) 'outcome can be consulted from a recorded intermediate event without seeking to the result'
    Click-ExperienceTarget $midOutcome 'outcome-return';$replayStep=Capture-Experience '63-outcome-mid-return'
    Assert-Experience (-not $replayStep.outcomeOpen -and $replayStep.replayPosition -eq 1 -and $replayStep.replayWorldHash -eq $replayHash) 'return from totals restores the exact intermediate event'
    Click-ExperienceTarget $replayStep 'replay-participant-2';$fieldFormation=Capture-Experience '63-formation-field'
    Assert-Experience ($fieldFormation.battleFormationOpen -and $fieldFormation.battleFormationSelected -eq 2 -and -not $fieldFormation.battleFormationFromReport -and $fieldFormation.replayPosition -eq 1 -and $fieldFormation.replayWorldHash -eq $replayHash) 'battlefield portrait opens the corresponding frozen participant at the current event'
    Click-ExperienceTarget $fieldFormation 'formation-back';$replayStep=Capture-Experience '63-formation-field-return'
    Assert-Experience (-not $replayStep.battleFormationOpen -and -not $replayStep.outcomeOpen -and -not $replayStep.replayEventsVisible -and $replayStep.replayPosition -eq 1 -and $replayStep.replayWorldHash -eq $replayHash) 'formation returns to the original field event without resuming automatically'
    Assert-Experience (-not $replayStep.replayEventsVisible -and $replayStep.replayKind -eq 1 -and $replayStep.replayDamageTotal -eq ($replayStep.replayDefenderBefore-$replayStep.replayDefenderAfter) -and $replayStep.replayDamageTotal -gt 0 -and $replayStep.replayLossTotal -eq 0 -and -not $replayStep.replayResultVisible -and $replayStep.replayGold -eq 0) 'battlefield shows the current recorded army damage without future cumulative losses or result rewards'
    Click-ExperienceTarget $replayStep 'replay-events';$eventDetails=Capture-Experience '63a-replay-event-details'
    Assert-Experience ($eventDetails.replayEventsVisible -and $eventDetails.replayPosition -eq 1 -and $eventDetails.replayWorldHash -eq $replayHash) 'visible event detail tab preserves the current recorded event and complete world state'
    Click-ExperienceTarget $eventDetails 'replay-formation';$eventFormation=Capture-Experience '63a-formation-events'
    Assert-Experience ($eventFormation.battleFormationOpen -and -not $eventFormation.replayEventsVisible -and $eventFormation.replayPosition -eq 1 -and $eventFormation.replayWorldHash -eq $replayHash) 'event details provide a visible frozen formation entry'
    Click-ExperienceTarget $eventFormation 'formation-back';$eventDetailsReturned=Capture-Experience '63a-formation-events-return'
    Assert-Experience ($eventDetailsReturned.replayEventsVisible -and $eventDetailsReturned.replayEventScrollY -eq $eventDetails.replayEventScrollY -and $eventDetailsReturned.replayPosition -eq 1 -and $eventDetailsReturned.replayWorldHash -eq $replayHash) 'formation return preserves the event list and its scroll position'
    $eventDetails=$eventDetailsReturned
    Click-ExperienceTarget $eventDetails 'replay-event-2';$eventLoss=Capture-Experience '63b-replay-select-loss'
    Assert-Experience ($eventLoss.replayPosition -eq 2 -and $eventLoss.replayKind -eq 2 -and -not $eventLoss.replayPlaying -and $eventLoss.replayLossTotal -eq ($eventLoss.replayAttackerBefore-$eventLoss.replayAttackerAfter) -and $eventLoss.replayLossTotal -gt 0 -and $eventLoss.replayWorldHash -eq $replayHash) 'clicking the visible loss event seeks its recorded before and after troop counts without settlement'
    Click-ExperienceTarget $eventLoss 'replay-round-0';$eventStart=Capture-Experience '63c-replay-round-preparation'
    Assert-Experience ($eventStart.replayPosition -eq 0 -and $eventStart.replayDamageTotal -eq 0 -and $eventStart.replayLossTotal -eq 0 -and $eventStart.replayRecoveryTotal -eq 0 -and $eventStart.replayWorldHash -eq $replayHash) 'preparation round chip resets the cumulative display without modifying the record or campaign'
    $eventList=Get-ExperienceTarget $eventStart 'replay-event-list'
    Focus-Experience;Move-Experience $eventList.x $eventList.y;Assert-ExperiencePointer $eventList.x $eventList.y
    [EDExperiencePointer]::mouse_event(0x0800,0,0,[uint32](4294967296-480),[UIntPtr]::Zero)
    Start-Sleep -Milliseconds 200;Assert-ExperiencePointer $eventList.x $eventList.y
    $eventScrolled=Capture-Experience '63d-replay-event-scroll'
    $eventScrollMoved=if($eventStart.replayEventScrollMaximum -gt 0){$eventScrolled.replayEventScrollY -gt $eventStart.replayEventScrollY}else{[Math]::Abs($eventScrolled.replayEventScrollY) -lt .001}
    Assert-Experience ($eventScrollMoved -and $eventScrolled.replayPosition -eq 0 -and $eventScrolled.replayWorldHash -eq $replayHash) 'actual event-list wheel scroll respects content bounds without moving the playback cursor or map'
    $visibleEvent=@($eventScrolled.targets | Where-Object {$_.id -match '^replay-event-[0-9]+$'} | Sort-Object { [int]($_.id -replace '^replay-event-','') })[-1]
    if(-not $visibleEvent){throw 'Scrolled battle event list has no fully visible actionable event.'}
    $visibleEventIndex=[int]($visibleEvent.id -replace '^replay-event-','')
    Click-ExperienceTarget $eventScrolled $visibleEvent.id;$eventSelected=Capture-Experience '63d-replay-scrolled-selection'
    Assert-Experience ($eventSelected.replayPosition -eq $visibleEventIndex -and $eventSelected.replayWorldHash -eq $replayHash -and -not $eventSelected.replayPlaying) 'a fully visible event revealed by real scrolling can be selected without replaying any campaign effects'
    Click-ExperienceTarget $eventSelected 'replay-round-1';$eventRound=Capture-Experience '63e-replay-round-one'
    Assert-Experience ($eventRound.replayRound -eq 1 -and $eventRound.replayPosition -eq 1 -and $eventRound.replayDamageTotal -eq $replayStep.replayDamageTotal -and $eventRound.replayLossTotal -eq 0 -and $eventRound.replayWorldHash -eq $replayHash) 'round one chip returns to its actual first event and recomputes cumulative values without double counting'
    Click-ExperienceTarget $eventRound 'replay-field';$replayStep=Capture-Experience '63f-replay-field-return'
    Assert-Experience (-not $replayStep.replayEventsVisible -and $replayStep.replayPosition -eq 1 -and $replayStep.replayWorldHash -eq $replayHash) 'battlefield tab restores the same army event with no repeated battle effects'
    Click-ExperienceTarget $replayStep 'replay-speed';$replayFast=Capture-Experience '64-replay-speed'
    Assert-Experience ($replayFast.replaySpeed -eq 2 -and $replayFast.replayWorldHash -eq $replayHash) 'replay speed changes only presentation speed'
    # The play/pause control stays in the same visible rectangle. Pause before a
    # screenshot write can consume the whole short battle at 2x speed.
    Click-ExperienceTarget $replayFast 'replay-play'
    Click-ExperienceTarget $replayFast 'replay-play';$pauseAction=Capture-Experience '65-replay-pause-action'
    Assert-Experience (-not $pauseAction.replayPlaying -and $pauseAction.replayPosition -gt $replayStep.replayPosition -and $pauseAction.replayPosition -lt ($pauseAction.replayCount-1) -and $pauseAction.replayWorldHash -eq $replayHash) 'actual play then pause stops before the result and preserves campaign JSON'
    Click-ExperienceTarget $pauseAction 'replay-play';$replayPlay=Capture-Experience '65a-replay-playing'
    Assert-Experience ($replayPlay.replayPosition -gt $pauseAction.replayPosition -and $replayPlay.replayWorldHash -eq $replayHash) 'resumed play advances real recorded events without running the world clock'
    if($replayPlay.replayPlaying){Click-ExperienceTarget $replayPlay 'replay-play'}
    $replayPaused=Capture-Experience '66-replay-paused'
    Assert-Experience (-not $replayPaused.replayPlaying -and $replayPaused.replayWorldHash -eq $replayHash) 'pause or completed playback remains read only'
    Click-ExperienceTarget $replayPaused 'replay-start';$replayReset=Capture-Experience '67-replay-reset'
    $scrub=Get-ExperienceTarget $replayReset 'replay-progress'
    Drag-Experience ($scrub.x-$scrub.w*.5+8) $scrub.y ($scrub.x+$scrub.w*.25) $scrub.y
    $replayScrub=Capture-Experience '67a-replay-scrub'
    Assert-Experience ($replayScrub.replayPosition -gt 0 -and -not $replayScrub.replayPlaying -and $replayScrub.replayWorldHash -eq $replayHash) 'dragging the visible progress control seeks without settlement'
    Click-ExperienceTarget $replayScrub 'replay-start';$replayReset=Capture-Experience '67b-replay-restart'
    Click-ExperienceTarget $replayReset 'replay-next';$replayRound=Capture-Experience '68-replay-round'
    Assert-Experience ($replayRound.replayPosition -gt 0 -and $replayRound.replayWorldHash -eq $replayHash) 'round navigation reads frozen events'
    Click-ExperienceTarget $replayRound 'replay-prev';$replayPrevious=Capture-Experience '69-replay-previous'
    Assert-Experience ($replayPrevious.replayPosition -eq 0 -and $replayPrevious.replayWorldHash -eq $replayHash) 'previous round returns to recorded preparation'
    Click-ExperienceTarget $replayPrevious 'replay-end';$replayEnd=Capture-Experience '70-replay-result'
    Assert-Experience ($replayEnd.replayPosition -eq ($replayEnd.replayCount-1) -and $replayEnd.replayWorldHash -eq $replayHash -and $replayEnd.troops -eq $replayStart.troops -and $replayEnd.generalExperienceTotal -eq $replayStart.generalExperienceTotal) 'jump to settled result does not repeat troop loss experience capture or reward'
    Click-ExperienceTarget $replayEnd 'replay-outcome-detail';$endOutcome=Capture-Experience '70a-outcome-detail'
    Assert-Experience ($endOutcome.outcomeOpen -and $endOutcome.outcomeGold -eq $replayEnd.replayGold -and $endOutcome.outcomeExperience -eq $replayEnd.replayExperience -and $endOutcome.outcomeLoss -eq $replayEnd.replayLossTotal -and $endOutcome.outcomeRecovery -eq $replayEnd.replayRecoveryTotal -and $endOutcome.replayWorldHash -eq $replayHash) 'full outcome agrees with the final recorded event totals'
    Click-ExperienceTarget $endOutcome 'outcome-play';$outcomePlaying=Capture-Experience '70b-outcome-play'
    Assert-Experience (-not $outcomePlaying.outcomeOpen -and $outcomePlaying.replayOpen -and $outcomePlaying.replayWorldHash -eq $replayHash) 'outcome play reopens the same frozen process without modifying the campaign'
    $outcomeAdvanced=Wait-ExperienceState '70c-outcome-playing' {param($s) $s.replayPosition -gt 0} 8
    Assert-Experience ($outcomeAdvanced.replayPosition -gt 0 -and $outcomeAdvanced.replayWorldHash -eq $replayHash) 'play from outcome actually advances recorded events'
    Click-ExperienceTarget $outcomeAdvanced 'replay-end';$replayEnd=Capture-Experience '70d-outcome-play-ended'
    Drag-Experience ($cw*.40) ($ch*.38) ($cw*.46) ($ch*.43);$replayDrag=Capture-Experience '71-replay-map-block'
    Assert-Experience ($replayDrag.replayOpen -and $replayDrag.selected -eq $replayEnd.selected -and [Math]::Abs($replayDrag.focusX-$replayEnd.focusX) -lt .01 -and [Math]::Abs($replayDrag.focusZ-$replayEnd.focusZ) -lt .01 -and $replayDrag.replayWorldHash -eq $replayHash) 'battle playback blocks map dragging and selection'
    Click-ExperienceTarget $replayDrag 'replay-back';$replayClosed=Capture-Experience '72-replay-return'
    Assert-Experience ($replayClosed.tab -eq '战报' -and -not $replayClosed.replayOpen -and $replayClosed.troops -eq $replayStart.troops -and $replayClosed.generalExperienceTotal -eq $replayStart.generalExperienceTotal) 'replay exit returns to reports without another settlement'
    Click-ExperienceTarget $replayClosed 'report-replay';$replayReopened=Capture-Experience '73-replay-reopen'
    Assert-Experience ($replayReopened.replayBattleId -eq $replayStart.replayBattleId -and $replayReopened.replayPosition -eq 0 -and $replayReopened.troops -eq $replayStart.troops -and $replayReopened.generalExperienceTotal -eq $replayStart.generalExperienceTotal) 'reopening uses the same saved battle identity and snapshot'
    Click-ExperienceTarget $replayReopened 'replay-back';$replayReturned=Capture-Experience '74-replay-location-ready'
    Click-ExperienceTarget $replayReturned 'report-location';$battleLocation=Capture-Experience '75-battle-location'
    Assert-Experience ($battleLocation.tab -eq '领地' -and $battleLocation.selected -eq $trainingCaptured.selected -and $battleLocation.selectedOwned) 'battle location opens the actual captured tile'
    Click-ExperienceTarget $battleLocation 'reports-tab';$locationReport=Capture-Experience '75a-outcome-location-report'
    Click-ExperienceTarget $locationReport 'report-outcome';$locationOutcome=Capture-Experience '75b-outcome-location-ready'
    Click-ExperienceTarget $locationOutcome 'outcome-location';$outcomeLocation=Capture-Experience '75c-outcome-location'
    Assert-Experience ($outcomeLocation.tab -eq '领地' -and $outcomeLocation.selected -eq $battleLocation.selected -and $outcomeLocation.battleWorldHash -eq $replayHash) 'outcome location opens the actual target without another capture or award'
    # Obtain a second land with real map clicks after all historical battle
    # assertions, so comparison tests cannot replace their report under test.
    $compareMap=Set-ExperienceSpeed 0 '75d-training-second-land-pause'
    Assert-Experience ($compareMap.nextTarget -ne $trainingCaptured.selected -and $compareMap.targetX -gt 0 -and $compareMap.targetX -lt $cw -and $compareMap.targetY -gt 0 -and $compareMap.targetY -lt $ch) 'second comparison land is a different on-screen march target'
    Move-Experience $compareMap.targetX $compareMap.targetY;$compareProbe=Capture-Experience '75e-training-second-land-visible'
    Assert-Experience (-not $compareProbe.mouseBlocked) 'actual second-land terrain coordinate is unobstructed before clicking'
    Click-Experience $compareProbe.targetX $compareProbe.targetY;$compareLand=Capture-Experience '75f-training-second-land-selected'
    Assert-Experience ($compareLand.selected -eq $compareProbe.nextTarget -and -not $compareLand.selectedOwned -and $compareLand.tab -eq '领地') 'real terrain click selects the different unowned comparison target'
    Click-ExperienceTarget $compareLand 'army-attack';$compareAttack=Capture-Experience '75g-training-second-land-confirm'
    if($compareAttack.watchAttackOnArrival){Click-ExperienceTarget $compareAttack 'attack-watch-toggle';$compareAttack=Capture-Experience '75h-training-second-land-no-watch'}
    Click-ExperienceTarget $compareAttack 'confirm-yes';$compareMarch=Capture-Experience '75i-training-second-land-march'
    Assert-Experience ($compareMarch.orderActive -and $compareMarch.orderAction -eq 0 -and $compareMarch.reportCount -eq $compareLand.reportCount) 'second comparison land requires a real unarrived attack order'
    $compareMarch=Set-ExperienceSpeed 60 '75j-training-second-land-speed'
    $compareOccupied=Wait-ExperienceState '75k-training-second-land-occupied' {param($s) -not $s.orderActive -and $s.selectedOwned}
    $compareOccupied=Set-ExperienceSpeed 0 '75l-training-second-land-paused'
    Assert-Experience ($compareOccupied.selected -eq $compareLand.selected -and $compareOccupied.ownedLandCount -eq ($compareLand.ownedLandCount+1)) 'completed actual battle adds the second comparison land'
    Click-ExperienceLandCaption $compareOccupied 'land-training';$compareTwoPlan=Capture-Experience '75m-training-two-land-plan'
    Assert-Experience ($compareTwoPlan.tab -eq '练兵' -and $compareTwoPlan.trainingTarget -eq $compareOccupied.selected -and $compareTwoPlan.battleWorldHash -eq $compareOccupied.battleWorldHash -and -not $compareTwoPlan.orderActive) 'the original training button caption opens the same independent plan as its icon without an order or charge'
    Click-ExperienceTarget $compareTwoPlan 'training-compare';$compareTwo=Capture-Experience '75n-training-two-land-picker'
    Assert-Experience ($compareTwo.trainingCandidateOrder.Count -ge 2 -and $compareTwo.trainingCandidate -eq $compareOccupied.selected) 'comparison exposes both actually occupied targets and starts from its land entry'
    $compareViewport=Get-ExperienceTarget $compareTwo 'training-picker-list'
    $compareOldVisible=Find-ExperienceScrolledTarget $compareTwo ('training-picker-target-'+$trainingCaptured.selected) '75o-training-older-target-visible' $compareViewport.x $compareViewport.y
    Click-ExperienceTarget $compareOldVisible ('training-picker-target-'+$trainingCaptured.selected);$compareDifferent=Capture-Experience '75p-training-different-candidate'
    Assert-Experience ($compareDifferent.trainingCandidate -eq $trainingCaptured.selected -and $compareDifferent.trainingTarget -eq $compareOccupied.selected -and $compareDifferent.trainingPreviewTotal -gt 0 -and $compareDifferent.battleWorldHash -eq $compareTwoPlan.battleWorldHash) 'clicking the other visible land changes only the draft candidate and its forecast'
    Click-ExperienceTarget $compareDifferent 'training-picker-back';$compareDiscarded=Capture-Experience '75q-training-different-discarded'
    Assert-Experience (-not $compareDiscarded.trainingPickerOpen -and $compareDiscarded.trainingTarget -eq $compareOccupied.selected -and $compareDiscarded.battleWorldHash -eq $compareTwoPlan.battleWorldHash) 'back discards a genuinely different draft land without charging'
    Click-ExperienceTarget $compareDiscarded 'training-compare';$compareTwo=Capture-Experience '75r-training-two-land-reopen'
    $compareViewport=Get-ExperienceTarget $compareTwo 'training-picker-list'
    $compareOldVisible=Find-ExperienceScrolledTarget $compareTwo ('training-picker-target-'+$trainingCaptured.selected) '75s-training-older-target-visible' $compareViewport.x $compareViewport.y
    Click-ExperienceTarget $compareOldVisible ('training-picker-target-'+$trainingCaptured.selected);$compareDifferent=Capture-Experience '75t-training-different-reselected'
    Click-ExperienceTarget $compareDifferent 'training-picker-apply';$compareApplied=Capture-Experience '75u-training-different-applied'
    Assert-Experience (-not $compareApplied.trainingPickerOpen -and $compareApplied.trainingTarget -eq $trainingCaptured.selected -and $compareApplied.selected -eq $compareOccupied.selected -and $compareApplied.trainingReturnTab -eq '领地' -and $compareApplied.trainingPreviewTotal -eq $compareDifferent.trainingPreviewTotal -and $compareApplied.battleWorldHash -eq $compareTwoPlan.battleWorldHash) 'applying the different land preserves original map selection and transfers its exact forecast without an order'
    Click-ExperienceTarget $compareApplied 'training-confirm';$compareApproval=Capture-Experience '75v-training-different-confirmation'
    Assert-Experience ($compareApproval.confirmationOpen -and -not $compareApproval.orderActive -and $compareApproval.battleWorldHash -eq $compareTwoPlan.battleWorldHash) 'changed target still requires a separate approval with no early charge'
    Click-ExperienceTarget $compareApproval 'confirm-no';$compareUncharged=Capture-Experience '75w-training-different-declined'
    Click-ExperienceTarget $compareUncharged 'training-back';$compareReturned=Capture-Experience '75x-training-different-origin'
    Assert-Experience ($compareReturned.tab -eq '领地' -and $compareReturned.selected -eq $compareOccupied.selected -and $compareReturned.battleWorldHash -eq $compareTwoPlan.battleWorldHash) 'comparison and declined departure return to the original land entry without repeating battle or training effects'
    $logPath=Join-Path $edExperienceOutput 'player.log'
    $log=[Text.Encoding]::UTF8.GetString([EDExperiencePointer]::ReadShared($logPath))
    Assert-Experience (-not ($log -match 'NullReferenceException|MissingReferenceException|IndexOutOfRangeException|ArgumentException:|Shader error|EXPERIENCE_ASSERT')) 'player log has no runtime exceptions'

    Restart-ExperienceReview 'player-quest5.log'
    $freshQuest=Capture-Experience '76-quest5-fresh-map'
    Assert-Experience ($freshQuest.tab -eq '领地' -and $freshQuest.claimedQuests -eq 0 -and -not $freshQuest.orderActive -and -not $freshQuest.cityBuilding) 'second read-only scenario starts with actual unclaimed city objectives and no queued work'
    $freshQuest=Set-ExperienceSpeed 0 '76-quest5-paused'
    Click-ExperienceTarget $freshQuest 'city-open';$freshCity=Capture-Experience '76-quest5-city'
    Click-ExperienceTarget $freshCity 'city-facilities';$freshFacilities=Capture-Experience '76-quest5-district-visit'
    Assert-ExperienceCityPage $freshFacilities 'facilities'
    Click-ExperienceTarget $freshFacilities 'city-quests';$firstGuide=Capture-Experience '76-quest5-first-task'
    $firstGuide=Find-ExperienceScrolledTarget $firstGuide 'quest-claim' '76-quest5-first-claim' ($cw*.80) ($ch*.57)
    Assert-Experience ($firstGuide.questCurrentId -eq 1 -and $firstGuide.questReady -and $firstGuide.claimedQuests -eq 0) 'fresh district visit genuinely completes the first prerequisite before its reward'
    Click-ExperienceTarget $firstGuide 'quest-claim';$firstGuideReward=Capture-Experience '77-quest5-first-reward'
    Assert-ExperienceQuestReceipt $firstGuide $firstGuideReward 1 2 @(300,250,250,100,100)
    Click-ExperienceTarget $firstGuideReward 'quest-reward-next';$freshMill=Capture-Experience '77-quest5-mill-target'
    Assert-Experience ($freshMill.cityPage -eq 'facilities' -and $freshMill.cityPlot -eq 0 -and $freshMill.cityFacility -eq 2 -and -not $freshMill.cityBuilding) 'fresh reward navigation reaches the actual empty mill plot'
    $freshMill=Find-ExperienceScrolledTarget $freshMill 'city-build' '77-quest5-mill-build' ($cw*.82) ($ch*.60)
    Click-ExperienceTarget $freshMill 'city-build';$freshMillQueue=Capture-Experience '77-quest5-mill-queue'
    Assert-Experience ($freshMillQueue.cityBuilding -and $freshMillQueue.cityConstructionPlot -eq 0 -and $freshMillQueue.cityMillLevel -eq 0) 'fresh mill uses the real pending construction queue'
    $freshMillQueue=Set-ExperienceSpeed 60 '77-quest5-mill-speed'
    $freshMillBuilt=Wait-ExperienceState '77-quest5-mill-complete' {param($s) -not $s.cityBuilding -and $s.cityMillLevel -eq 1}
    $freshMillBuilt=Set-ExperienceSpeed 0 '77-quest5-mill-paused'
    Click-ExperienceTarget $freshMillBuilt 'city-quests';$secondGuide=Capture-Experience '78-quest5-second-task'
    $secondGuide=Find-ExperienceScrolledTarget $secondGuide 'quest-claim' '78-quest5-second-claim' ($cw*.80) ($ch*.57)
    Assert-Experience ($secondGuide.questCurrentId -eq 2 -and $secondGuide.questReady -and $secondGuide.claimedQuests -eq 1) 'fresh completed mill genuinely unlocks the second reward'
    Click-ExperienceTarget $secondGuide 'quest-claim';$secondGuideReward=Capture-Experience '78-quest5-second-reward'
    Assert-ExperienceQuestReceipt $secondGuide $secondGuideReward 2 3 @(500,400,400,150,100)
    Click-ExperienceTarget $secondGuideReward 'quest-reward-next';$freshHall=Capture-Experience '78-quest5-hall-target'
    Assert-Experience ($freshHall.cityPage -eq 'facilities' -and $freshHall.cityPlot -eq 4 -and $freshHall.cityFacility -eq 1 -and $freshHall.cityLevel -eq 1) 'third objective reaches the real level-one central town hall'
    $freshHall=Find-ExperienceScrolledTarget $freshHall 'city-build' '78-quest5-hall-build' ($cw*.82) ($ch*.60)
    Click-ExperienceTarget $freshHall 'city-build';$freshHallQueue=Capture-Experience '78-quest5-hall-queue'
    Assert-Experience ($freshHallQueue.cityBuilding -and $freshHallQueue.cityConstructionPlot -eq 4 -and $freshHallQueue.cityLevel -eq 1) 'town hall upgrade queues without prematurely completing its objective'
    $freshHallQueue=Set-ExperienceSpeed 60 '78-quest5-hall-speed'
    $freshHallBuilt=Wait-ExperienceState '78-quest5-hall-complete' {param($s) -not $s.cityBuilding -and $s.cityLevel -eq 2}
    $freshHallBuilt=Set-ExperienceSpeed 0 '78-quest5-hall-paused'
    Click-ExperienceTarget $freshHallBuilt 'city-quests';$thirdGuide=Capture-Experience '79-quest5-third-task'
    $thirdGuide=Find-ExperienceScrolledTarget $thirdGuide 'quest-claim' '79-quest5-third-claim' ($cw*.80) ($ch*.57)
    Assert-Experience ($thirdGuide.questCurrentId -eq 3 -and $thirdGuide.questReady -and $thirdGuide.claimedQuests -eq 2) 'completed real town hall upgrade unlocks the third reward'
    Click-ExperienceTarget $thirdGuide 'quest-claim';$thirdGuideReward=Capture-Experience '79-quest5-third-reward'
    Assert-ExperienceQuestReceipt $thirdGuide $thirdGuideReward 3 4 @(650,500,550,220,150) $true
    Click-ExperienceTarget $thirdGuideReward 'quest-reward-done';$fourthGuide=Capture-Experience '79-quest5-fourth-task'
    $fourthGuide=Find-ExperienceScrolledTarget $fourthGuide 'quest-claim' '79-quest5-fourth-claim' ($cw*.80) ($ch*.57)
    Assert-Experience ($fourthGuide.questCurrentId -eq 4 -and $fourthGuide.questReady -and $fourthGuide.questChapter -eq 1 -and $fourthGuide.claimedQuests -eq 3) 'the actual starting three-general formation meets the fourth objective after chapter one'
    Click-ExperienceTarget $fourthGuide 'quest-claim';$fourthGuideReward=Capture-Experience '79-quest5-fourth-reward'
    Assert-ExperienceQuestReceipt $fourthGuide $fourthGuideReward 4 5 @(350,250,150,100,150)
    Click-ExperienceTarget $fourthGuideReward 'quest-reward-done';$fifthGuide=Capture-Experience '80-quest5-unfinished'
    $fifthGuide=Find-ExperienceScrolledTarget $fifthGuide 'quest-go' '80-quest5-go' ($cw*.80) ($ch*.57)
    Assert-Experience ($fifthGuide.questCurrentId -eq 5 -and -not $fifthGuide.questReady -and $fifthGuide.claimedQuests -eq 4 -and 5 -in $fifthGuide.visibleQuestIds) 'fifth objective is genuinely unfinished and visibly actionable before any reinforcement'
    Click-ExperienceTarget $fifthGuide 'quest-detail-5';$objectiveRecruit=Capture-Experience '80-objective-reinforcement'
    Assert-Experience ($objectiveRecruit.questDetailCurrent -and @($objectiveRecruit.questDetailCost).Count -eq 5 -and $objectiveRecruit.battleWorldHash -eq $fifthGuide.battleWorldHash) 'recruitment objective displays an uncharged default quote'
    Click-ExperienceTarget $objectiveRecruit 'quest-detail-action';$objectiveRecruitPlan=Capture-Experience '80-objective-reinforcement-plan'
    Assert-Experience ($objectiveRecruitPlan.tab -eq '征兵' -and $objectiveRecruitPlan.reinforcementReturnPage -eq 'quest-detail' -and $objectiveRecruitPlan.battleWorldHash -eq $fifthGuide.battleWorldHash) 'objective reaches adjustable reinforcement with its own return origin'
    Click-ExperienceTarget $objectiveRecruitPlan 'reinforcement-back';$objectiveRecruitBack=Capture-Experience '80-objective-reinforcement-back'
    Assert-Experience ($objectiveRecruitBack.cityPage -eq 'quest-detail' -and $objectiveRecruitBack.questDetailId -eq 5 -and $objectiveRecruitBack.battleWorldHash -eq $fifthGuide.battleWorldHash) 'reinforcement returns to the original task detail without paying'
    Click-ExperienceTarget $objectiveRecruitBack 'city-back';$fifthGuide=Capture-Experience '80-objective-reinforcement-list'
    Click-ExperienceTarget $fifthGuide 'quest-go';$questReinforcement=Capture-Experience '80-quest5-reinforcement-entry'
    Assert-Experience ($questReinforcement.tab -eq '征兵' -and $questReinforcement.reinforcementReturnTab -eq '主城' -and $questReinforcement.reinforcementReturnPage -eq 'quests' -and $questReinforcement.reinforcementReadyQuestId -eq 0 -and $questReinforcement.battleWorldHash -eq $fifthGuide.battleWorldHash) 'visible fifth-task go opens the actual uncharged reinforcement plan and preserves its quest origin'
    $costViewport=Get-ExperienceTarget $questReinforcement 'reinforcement-cost-scroll'
    Assert-ExperienceTargetInClient $questReinforcement 'reinforcement-cost-scroll'
    Focus-Experience;Move-Experience $costViewport.x $costViewport.y;Assert-ExperiencePointer $costViewport.x $costViewport.y
    [EDExperiencePointer]::mouse_event(0x0800,0,0,[uint32](4294967296-480),[UIntPtr]::Zero)
    Start-Sleep -Milliseconds 200;Assert-ExperiencePointer $costViewport.x $costViewport.y
    $questCostScrolled=Capture-Experience '80-quest5-cost-scroll'
    Assert-Experience ($questCostScrolled.tab -eq '征兵' -and $questCostScrolled.reinforcementAmount -eq $questReinforcement.reinforcementAmount -and $questCostScrolled.battleWorldHash -eq $fifthGuide.battleWorldHash) 'real wheel input over reinforcement costs preserves quantity and all campaign state whether or not scrolling is needed'
    Assert-ExperienceMapStill $questReinforcement $questCostScrolled 'quest reinforcement cost viewport'
    Click-ExperienceTarget $questCostScrolled 'reinforcement-back';$questReinforcementBack=Capture-Experience '80-quest5-uncharged-return'
    Assert-ExperienceCityPage $questReinforcementBack 'quests'
    Assert-Experience ($questReinforcementBack.questChapter -eq $fifthGuide.questChapter -and $questReinforcementBack.questCompleted -eq $fifthGuide.questCompleted -and $questReinforcementBack.questCurrentId -eq 5 -and -not $questReinforcementBack.questReady -and $questReinforcementBack.selected -eq $fifthGuide.selected -and $questReinforcementBack.battleWorldHash -eq $fifthGuide.battleWorldHash) 'reinforcement back restores the original unfinished chapter and map selection without any charge or reward'
    $questReinforcementBack=Find-ExperienceScrolledTarget $questReinforcementBack 'quest-go' '81-quest5-reenter' ($cw*.80) ($ch*.57)
    Click-ExperienceTarget $questReinforcementBack 'quest-go';$questReinforcementPlan=Capture-Experience '81-quest5-plan'
    Click-ExperienceTarget $questReinforcementPlan 'reinforcement-amount-0';$questReinforcementSelected=Capture-Experience '81-quest5-selected-amount'
    Assert-Experience ($questReinforcementSelected.reinforcementAmount -eq [Math]::Max(0,$questReinforcementPlan.reinforcementAmount-50) -and $questReinforcementSelected.reinforcementAmount -gt 0 -and ($questReinforcementSelected.reinforcementAmount % 100) -ne 0 -and $questReinforcementSelected.battleWorldHash -eq $fifthGuide.battleWorldHash) 'actual minus control chooses a non-hundred reinforcement amount without changing the campaign'
    Click-ExperienceTarget $questReinforcementSelected 'reinforcement-confirm';$questReinforcementConfirm=Capture-Experience '81-quest5-confirmation'
    Assert-Experience ($questReinforcementConfirm.confirmationOpen -and $questReinforcementConfirm.battleWorldHash -eq $fifthGuide.battleWorldHash) 'fifth-task reinforcement confirmation previews exact costs without spending'
    Click-ExperienceTarget $questReinforcementConfirm 'confirm-yes';$questReinforcementDone=Capture-Experience '82-quest5-completed-receipt'
    Assert-Experience ($questReinforcementDone.reinforcementCompleted -and $questReinforcementDone.reinforcementAdded -eq $questReinforcementSelected.reinforcementAmount -and $questReinforcementDone.troops -eq ($questReinforcementSelected.troops+$questReinforcementSelected.reinforcementAmount) -and $questReinforcementDone.reinforcementReadyQuestId -eq 5 -and $questReinforcementDone.claimedQuests -eq 4 -and -not $questReinforcementDone.receiptOpen) 'actual fifth-task reinforcement completes once and offers its still-unclaimed quest reward'
    for($resourceIndex=0;$resourceIndex -lt 3;$resourceIndex++) {
        Assert-Experience ([Math]::Abs($questReinforcementSelected.reinforcementBalance[$resourceIndex]-$questReinforcementDone.reinforcementBalance[$resourceIndex]-$questReinforcementSelected.reinforcementCost[$resourceIndex]) -lt .00001 -and [Math]::Abs($questReinforcementDone.reinforcementPaid[$resourceIndex]-$questReinforcementSelected.reinforcementCost[$resourceIndex]) -lt .00001) ('quest reinforcement receipt matches exact actual payment for resource '+$resourceIndex)
    }
    foreach($buttonId in @('reinforcement-back','reinforcement-done','reinforcement-quest')) {Assert-ExperienceTargetInClient $questReinforcementDone $buttonId}
    $reinforcementDoneButton=Get-ExperienceTarget $questReinforcementDone 'reinforcement-done'
    $reinforcementQuestButton=Get-ExperienceTarget $questReinforcementDone 'reinforcement-quest'
    Assert-Experience (($reinforcementDoneButton.x+$reinforcementDoneButton.w*.5) -le ($reinforcementQuestButton.x-$reinforcementQuestButton.w*.5)) 'completion return and quest handoff remain separate actual click targets at the requested resolution'
    Click-ExperienceTarget $questReinforcementDone 'reinforcement-quest';$reinforcementQuestHandoff=Capture-Experience '83-quest5-reward-handoff'
    Assert-ExperienceCityPage $reinforcementQuestHandoff 'quests'
    Assert-Experience (-not $reinforcementQuestHandoff.reinforcementCompleted -and -not $reinforcementQuestHandoff.receiptOpen -and $reinforcementQuestHandoff.questCurrentId -eq 5 -and $reinforcementQuestHandoff.questReady -and 5 -in $reinforcementQuestHandoff.visibleQuestIds -and $reinforcementQuestHandoff.claimedQuests -eq 4 -and $reinforcementQuestHandoff.battleWorldHash -eq $questReinforcementDone.battleWorldHash) 'completion handoff visibly focuses the real fifth task without recruiting again or claiming its reward'
    $fifthClaim=Get-ExperienceTarget $reinforcementQuestHandoff 'quest-claim'
    Click-Experience $fifthClaim.x $fifthClaim.y;$fifthReward=Capture-Experience '84-quest5-reward-claimed'
    Assert-ExperienceQuestReceipt $reinforcementQuestHandoff $fifthReward 5 6 @(450,350,150,120,100)
    Click-Experience $fifthClaim.x $fifthClaim.y;$fifthRewardRepeat=Capture-Experience '84-quest5-reward-repeat'
    Assert-Experience ($fifthRewardRepeat.receiptOpen -and $fifthRewardRepeat.receiptId -eq 5 -and $fifthRewardRepeat.claimedQuests -eq 5 -and $fifthRewardRepeat.battleWorldHash -eq $fifthReward.battleWorldHash) 'repeated original fifth-task claim coordinate cannot duplicate recruitment or reward'
    Click-ExperienceTarget $fifthRewardRepeat 'quest-reward-done';$fifthRewardReturn=Capture-Experience '85-quest5-reward-return'
    Assert-ExperienceCityPage $fifthRewardReturn 'quests'
    Assert-Experience (-not $fifthRewardReturn.receiptOpen -and $fifthRewardReturn.questCurrentId -eq 6 -and $fifthRewardReturn.claimedQuests -eq 5 -and $fifthRewardReturn.battleWorldHash -eq $fifthReward.battleWorldHash) 'closing fifth-task reward advances to the real sixth objective without another payment or credit'
    $sixthDetail=Find-ExperienceScrolledTarget $fifthRewardReturn 'quest-detail-6' '85-sixth-objective' ($cw*.80) ($ch*.57)
    Click-ExperienceTarget $sixthDetail 'quest-detail-6';$objectiveTerritory=Capture-Experience '85-objective-territory'
    Click-ExperienceTarget $objectiveTerritory 'quest-detail-action';$objectiveLand=Capture-Experience '85-objective-land'
    Assert-Experience ($objectiveLand.tab -eq '领地' -and $objectiveLand.questLandReturn -and $objectiveLand.selected -ge 0 -and -not $objectiveLand.selectedOwned -and -not $objectiveLand.orderActive -and $objectiveLand.battleWorldHash -eq $fifthRewardReturn.battleWorldHash) 'territory objective selects actionable land without dispatch and exposes a task return'
    Assert-ExperienceTargetInClient $objectiveLand 'quest-land-return'
    Click-ExperienceTarget $objectiveLand 'quest-land-return';$objectiveLandBack=Capture-Experience '85-objective-land-back'
    Assert-Experience ($objectiveLandBack.cityPage -eq 'quest-detail' -and $objectiveLandBack.questDetailId -eq 6 -and -not $objectiveLandBack.questLandReturn -and $objectiveLandBack.battleWorldHash -eq $fifthRewardReturn.battleWorldHash) 'actual map return restores the same task detail without marching or charging'
    Click-ExperienceTarget $objectiveLandBack 'city-back';$fifthRewardReturn=Capture-Experience '85-objective-land-list'
    Click-ExperienceTarget $fifthRewardReturn 'city-back';$quest5Overview=Capture-Experience '85-quest5-city-return'
    Assert-ExperienceCityPage $quest5Overview 'overview'
    Click-ExperienceTarget $quest5Overview 'city-back';$quest5Map=Capture-Experience '85-quest5-map-return'
    Assert-Experience ($quest5Map.tab -eq '领地' -and -not $quest5Map.cityOpen -and $quest5Map.battleWorldHash -eq $fifthReward.battleWorldHash) 'returning through city to map preserves the completed recruitment and single quest-five credit'
    $quest5Log=[Text.Encoding]::UTF8.GetString([EDExperiencePointer]::ReadShared((Join-Path $edExperienceOutput 'player-quest5.log')))
    Assert-Experience (-not ($quest5Log -match 'NullReferenceException|MissingReferenceException|IndexOutOfRangeException|ArgumentException:|Shader error|EXPERIENCE_ASSERT')) 'fresh fifth-task review log has no runtime exceptions'

    # Long-list input uses a separate read-only scenario. Setup executes seven real
    # core battles; assertions below still click only rendered controls and rows.
    Restart-ExperienceReview 'player-report-archive.log' -ReportArchive
    $archiveMany=Capture-Experience '86-archive-many-all'
    Assert-Experience ($archiveMany.tab -eq '战报' -and $archiveMany.speed -eq 0 -and $archiveMany.reportCount -eq 8 -and $archiveMany.reportCapturedCount -eq 6 -and $archiveMany.reportNotCapturedCount -eq 1 -and $archiveMany.reportLegacyCount -eq 1) 'isolated archive review contains seven actual settlements and one legacy text record'
    $manyHash=$archiveMany.battleWorldHash
    Click-ExperienceTarget $archiveMany 'report-filter-1';$archiveMany=Capture-Experience '86-archive-many-captured'
    Assert-Experience ($archiveMany.reportFilteredCount -eq 6 -and $archiveMany.reportListScrollMaximum -gt 0) 'captured archive is genuinely longer than the visible viewport'
    $manyRows=@($archiveMany.targets | Where-Object {$_.id -like 'report-row-*'})
    Assert-Experience ($manyRows.Count -ge 2) 'long archive exposes at least two actual rows for different-row drag validation'
    $otherRow=$manyRows | Where-Object {[int]$_.id.Substring(11) -ne $archiveMany.reportSelectedIndex} | Select-Object -First 1
    Assert-Experience ($null -ne $otherRow) 'drag target is an actually visible unselected report'
    Drag-Experience ($otherRow.x-40) $otherRow.y ($otherRow.x+40) ($otherRow.y+8);$manyDragged=Capture-Experience '86-archive-many-other-row-drag'
    Assert-Experience ($manyDragged.reportSelectedIndex -eq $archiveMany.reportSelectedIndex -and $manyDragged.reportSelectedBattleId -eq $archiveMany.reportSelectedBattleId -and $manyDragged.battleWorldHash -eq $manyHash) 'dragging within a different visible report cannot select it'
    Click-ExperienceTarget $manyDragged $otherRow.id;$manySelected=Capture-Experience '86-archive-many-other-row-click'
    Assert-Experience ($manySelected.reportSelectedIndex -eq [int]$otherRow.id.Substring(11) -and $manySelected.reportSelectedBattleId -ne $archiveMany.reportSelectedBattleId -and $manySelected.battleWorldHash -eq $manyHash) 'actual click on the same report selects its distinct frozen battle identity'
    $manyViewport=Get-ExperienceTarget $manySelected 'report-list'
    Focus-Experience;Move-Experience $manyViewport.x $manyViewport.y;Assert-ExperiencePointer $manyViewport.x $manyViewport.y
    [EDExperiencePointer]::mouse_event(0x0800,0,0,[uint32](4294967296-480),[UIntPtr]::Zero)
    Start-Sleep -Milliseconds 200;Assert-ExperiencePointer $manyViewport.x $manyViewport.y
    $manyScrolled=Capture-Experience '86-archive-many-scrolled'
    Assert-Experience ($manyScrolled.reportListScrollY -gt $manySelected.reportListScrollY -and $manyScrolled.reportListScrollY -le $manyScrolled.reportListScrollMaximum+.01 -and $manyScrolled.reportSelectedBattleId -eq $manySelected.reportSelectedBattleId -and $manyScrolled.battleWorldHash -eq $manyHash) 'real wheel scroll moves a nonempty report range while preserving the selected battle'
    $manyVisible=@($manyScrolled.targets | Where-Object {$_.id -like 'report-row-*'})
    Assert-Experience ($manyVisible.Count -gt 0) 'scrolled archive exposes actual older rows'
    $olderRow=$manyVisible[$manyVisible.Count-1]
    Click-ExperienceTarget $manyScrolled $olderRow.id;$olderSelected=Capture-Experience '86-archive-older-selected'
    Assert-Experience ($olderSelected.reportSelectedIndex -eq [int]$olderRow.id.Substring(11) -and $olderSelected.battleWorldHash -eq $manyHash) 'clicking the scrolled older row selects its correct source index'
    Click-ExperienceTarget $olderSelected 'report-replay';$olderReplay=Capture-Experience '86-archive-older-replay'
    Assert-Experience ($olderReplay.replayOpen -and $olderReplay.replayBattleId -eq $olderSelected.reportSelectedBattleId -and $olderReplay.replayWorldHash -eq $manyHash) 'older-row replay uses the exact frozen battle selected after scrolling'
    Click-ExperienceTarget $olderReplay 'replay-step';$olderEvent=Capture-Experience '86-archive-older-event'
    Assert-Experience ($olderEvent.replayPosition -eq ($olderReplay.replayPosition+1) -and $olderEvent.replayBattleId -eq $olderReplay.replayBattleId -and $olderEvent.replayWorldHash -eq $manyHash) 'actual next-event click advances the older recorded battle without changing identity or campaign state'
    Click-ExperienceTarget $olderEvent 'replay-back';$olderReturn=Capture-Experience '86-archive-older-return'
    Assert-Experience ($olderReturn.reportFilter -eq 1 -and $olderReturn.reportSelectedBattleId -eq $olderSelected.reportSelectedBattleId -and [Math]::Abs($olderReturn.reportListScrollY-$olderSelected.reportListScrollY) -lt .01 -and $olderReturn.battleWorldHash -eq $manyHash) 'return from an older event keeps the actual nonzero list offset and selection'
    Click-ExperienceTarget $olderReturn 'report-filter-2';$manyDefeat=Capture-Experience '86-archive-defeat'
    Assert-Experience ($manyDefeat.reportFilteredCount -eq 1 -and $manyDefeat.reportSelectedReplay -and -not $manyDefeat.reportSelectedCaptured -and $manyDefeat.reportSelectedFinal -eq 0 -and $manyDefeat.battleWorldHash -eq $manyHash) 'not-captured tab selects the actual defeated army without changing its final troops'
    Click-ExperienceTarget $manyDefeat 'report-filter-3';$manyLegacy=Capture-Experience '86-archive-existing-legacy'
    Assert-Experience ($manyLegacy.reportFilteredCount -eq 1 -and -not $manyLegacy.reportSelectedReplay -and @($manyLegacy.targets | Where-Object {$_.id -in @('report-replay','report-outcome','report-formation','report-location')}).Count -eq 0 -and $manyLegacy.battleWorldHash -eq $manyHash) 'actual legacy row exposes only its original text without invented battle actions'
    $archiveReviewLog=[Text.Encoding]::UTF8.GetString([EDExperiencePointer]::ReadShared((Join-Path $edExperienceOutput 'player-report-archive.log')))
    Assert-Experience ($archiveReviewLog.Contains('BATTLE_REPORT_ARCHIVE_REVIEW_READY') -and -not ($archiveReviewLog -match 'NullReferenceException|MissingReferenceException|IndexOutOfRangeException|ArgumentException:|EXPERIENCE_ASSERT')) 'isolated archive input scenario has its setup marker and no runtime exceptions'

    # A new read-only session supplies a genuinely long owned-card list. The
    # fixture never creates hidden click targets or writes any player save.
    Restart-ExperienceReview 'player-owned-roster.log' -OwnedRoster
    $ownedMany=Capture-Experience '87-owned-roster-many'
    Assert-ExperienceOwnedRoster $ownedMany
    Assert-Experience ($ownedMany.speed -eq 0 -and $ownedMany.ownedRosterCount -eq 36 -and @($ownedMany.ownedFormationUids).Count -eq 3) 'isolated owned roster starts paused with 36 diagnostic instances and the actual three formation slots'
    $ownedHash=$ownedMany.battleWorldHash
    $ownedViewport=Get-ExperienceTarget $ownedMany 'owned-roster-scroll'
    for($ownedAttempt=0;$ownedAttempt -lt 13;$ownedAttempt++) {
        Focus-Experience;Move-Experience $ownedViewport.x $ownedViewport.y;Assert-ExperiencePointer $ownedViewport.x $ownedViewport.y
        $ownedWheel=if($ownedAttempt -eq 0){1200}else{240}
        [EDExperiencePointer]::mouse_event(0x0800,0,0,[uint32](4294967296-$ownedWheel),[UIntPtr]::Zero)
        Start-Sleep -Milliseconds 200;Assert-ExperiencePointer $ownedViewport.x $ownedViewport.y
        $ownedScrolled=Capture-Experience ('87-owned-roster-scroll-'+$ownedAttempt)
        Assert-ExperienceOwnedRoster $ownedScrolled
        $ownedVisible=@($ownedScrolled.targets | Where-Object {$_.id -match '^owned-card-[0-9]+$'})
        if($ownedScrolled.ownedRosterScrollY -gt 0 -and $ownedVisible.Count -gt 0 -and @($ownedScrolled.targets | Where-Object {$_.id -eq 'owned-card-first'}).Count -eq 0){break}
    }
    Assert-Experience ($ownedScrolled.ownedRosterScrollY -gt 0 -and $ownedVisible.Count -gt 0 -and @($ownedScrolled.targets | Where-Object {$_.id -eq 'owned-card-first'}).Count -eq 0 -and $ownedScrolled.battleWorldHash -eq $ownedHash) 'real owned-roster wheel reveals complete later instances while removing the clipped first-card alias without campaign effects'
    $ownedChosen=$ownedVisible[$ownedVisible.Count-1];$ownedExpectedUid=[int]$ownedChosen.id.Substring(11)
    Drag-Experience ($ownedChosen.x-$ownedChosen.w*.2) ($ownedChosen.y-$ownedChosen.h*.1) ($ownedChosen.x+$ownedChosen.w*.2) ($ownedChosen.y+$ownedChosen.h*.1)
    $ownedDragged=Capture-Experience '87-owned-roster-card-drag'
    Assert-ExperienceOwnedRoster $ownedDragged
    Assert-Experience ([string]::IsNullOrEmpty($ownedDragged.cardDetail) -and $ownedDragged.ownedRosterSelectedUid -eq $ownedScrolled.ownedRosterSelectedUid -and [Math]::Abs($ownedDragged.ownedRosterScrollY-$ownedScrolled.ownedRosterScrollY) -lt .01 -and $ownedDragged.battleWorldHash -eq $ownedHash) 'drag ending inside a visible owned card does not inspect or select that instance'
    Assert-ExperienceMapStill $ownedScrolled $ownedDragged 'owned roster card drag'
    Click-ExperienceTarget $ownedDragged $ownedChosen.id;$ownedLaterDetail=Capture-Experience '87-owned-roster-later-detail'
    Assert-ExperienceOwnedRosterModal $ownedLaterDetail
    Assert-Experience ($ownedLaterDetail.cardDetailOwnedUid -eq $ownedExpectedUid -and $ownedLaterDetail.selectedCardUid -eq $ownedExpectedUid -and $ownedLaterDetail.cardDetailOwnedLevel -gt 0 -and $ownedLaterDetail.cardDetailOwnedLevel -eq $ownedLaterDetail.selectedCardLevel -and $ownedLaterDetail.cardDetailOwnedExperience -eq $ownedLaterDetail.selectedCardExperience -and $ownedLaterDetail.battleWorldHash -eq $ownedHash) 'visible later-card click opens the exact owned UID with its real level and experience'
    Click-ExperienceTarget $ownedLaterDetail 'card-return';$ownedBack=Capture-Experience '87-owned-roster-later-return'
    Assert-ExperienceOwnedRoster $ownedBack
    Assert-Experience ($ownedBack.ownedRosterSelectedUid -eq $ownedExpectedUid -and $ownedBack.ownedRosterScrollY -gt 0 -and [Math]::Abs($ownedBack.ownedRosterScrollY-$ownedScrolled.ownedRosterScrollY) -lt .01 -and $ownedExpectedUid -in @($ownedBack.ownedRosterVisibleUids) -and $ownedBack.battleWorldHash -eq $ownedHash) 'owned detail return retains the exact later instance and its nonzero list position'
    Click-ExperienceTarget $ownedBack 'owned-slot-2';$ownedSlot=Capture-Experience '87-owned-roster-slot'
    Assert-Experience ($ownedSlot.ownedRosterSelectedSlot -eq 2 -and $ownedSlot.ownedRosterSelectedUid -eq $ownedExpectedUid -and ($ownedSlot.ownedFormationUids -join ',') -eq ($ownedMany.ownedFormationUids -join ',') -and $ownedSlot.battleWorldHash -eq $ownedHash) 'choosing a formation destination changes UI selection without assigning, charging or changing the campaign'
    Click-ExperienceTarget $ownedSlot 'owned-formation-1';$ownedFormation=Capture-Experience '87-owned-roster-current-formation'
    Assert-Experience ($ownedFormation.ownedRosterSelectedSlot -eq 1 -and $ownedFormation.ownedRosterSelectedUid -eq $ownedMany.ownedFormationUids[1] -and $ownedFormation.battleWorldHash -eq $ownedHash -and [Math]::Abs($ownedFormation.ownedRosterScrollY-$ownedBack.ownedRosterScrollY) -lt .01) 'current formation strip selects the actual deployed UID without assigning or resetting the collection scroll'
    Click-ExperienceTarget $ownedFormation 'owned-general-detail';$ownedSelectedDetail=Capture-Experience '87-owned-roster-selected-detail'
    Assert-ExperienceOwnedRosterModal $ownedSelectedDetail
    Assert-Experience ($ownedSelectedDetail.cardDetailOwnedUid -eq $ownedMany.ownedFormationUids[1] -and $ownedSelectedDetail.battleWorldHash -eq $ownedHash) 'selected-general detail opens the deployed instance selected by the formation strip'
    Click-ExperienceTarget $ownedSelectedDetail 'card-return';$ownedBeforeProgress=Capture-Experience '87-owned-roster-progress-top'
    $ownedProgress=Get-ExperienceTarget $ownedBeforeProgress 'owned-progress-scroll';$ownedTrainingTop=Get-ExperienceTarget $ownedBeforeProgress 'general-training'
    Focus-Experience;Move-Experience $ownedProgress.x $ownedProgress.y;Assert-ExperiencePointer $ownedProgress.x $ownedProgress.y
    [EDExperiencePointer]::mouse_event(0x0800,0,0,[uint32](4294967296-1200),[UIntPtr]::Zero)
    Start-Sleep -Milliseconds 200;Assert-ExperiencePointer $ownedProgress.x $ownedProgress.y
    $ownedProgressBottom=Capture-Experience '87-owned-roster-progress-bottom'
    Assert-ExperienceOwnedRoster $ownedProgressBottom
    $ownedTrainingBottom=Get-ExperienceTarget $ownedProgressBottom 'general-training'
    Assert-Experience ($ownedProgressBottom.ownedProgressScrollY -gt 0 -and $ownedTrainingTop.x -eq $ownedTrainingBottom.x -and $ownedTrainingTop.y -eq $ownedTrainingBottom.y -and $ownedTrainingTop.w -eq $ownedTrainingBottom.w -and $ownedTrainingTop.h -eq $ownedTrainingBottom.h -and $ownedProgressBottom.battleWorldHash -eq $ownedHash) 'progression content really scrolls while its training action remains fixed and visible'
    Click-ExperienceTarget $ownedProgressBottom 'general-training';$ownedTraining=Capture-Experience '87-owned-roster-training'
    Assert-Experience ($ownedTraining.tab -eq '练兵' -and $ownedTraining.trainingReturnTab -eq '武将' -and -not $ownedTraining.orderActive -and $ownedTraining.battleWorldHash -eq $ownedHash) 'fixed roster training control opens the independent plan without a charge or an order'
    Click-ExperienceTarget $ownedTraining 'training-back';$ownedTrainingBack=Capture-Experience '87-owned-roster-training-return'
    Assert-ExperienceOwnedRoster $ownedTrainingBack
    Assert-Experience ($ownedTrainingBack.ownedRosterSelectedSlot -eq 1 -and $ownedTrainingBack.ownedRosterSelectedUid -eq $ownedMany.ownedFormationUids[1] -and [Math]::Abs($ownedTrainingBack.ownedRosterScrollY-$ownedBack.ownedRosterScrollY) -lt .01 -and [Math]::Abs($ownedTrainingBack.ownedProgressScrollY-$ownedProgressBottom.ownedProgressScrollY) -lt .01 -and $ownedTrainingBack.battleWorldHash -eq $ownedHash) 'training return restores the roster instance, slot and both real scroll offsets without settlement'
    $ownedReviewLog=[Text.Encoding]::UTF8.GetString([EDExperiencePointer]::ReadShared((Join-Path $edExperienceOutput 'player-owned-roster.log')))
    Assert-Experience ($ownedReviewLog.Contains('OWNED_ROSTER_REVIEW_READY') -and -not ($ownedReviewLog -match 'NullReferenceException|MissingReferenceException|IndexOutOfRangeException|ArgumentException:|EXPERIENCE_ASSERT')) 'isolated owned roster scenario has its setup marker and no runtime exceptions'
} catch {$edExperienceFailure=$_}
finally {
    [EDExperiencePointer]::mouse_event(4,0,0,0,[UIntPtr]::Zero)
    if($edExperienceProcess -and -not $edExperienceProcess.HasExited){[EDExperiencePointer]::KeyEvent($edExperienceProcess.MainWindowHandle,0x77,2)}
    if($edExperienceProcess -and -not $edExperienceProcess.HasExited) {
        try {$edExperienceProcess.CloseMainWindow() | Out-Null;if(-not $edExperienceProcess.WaitForExit(5000)){$edExperienceProcess.Kill();$edExperienceProcess.WaitForExit(5000) | Out-Null}}
        catch {if(-not $edExperienceFailure){$edExperienceFailure=$_}}
    }
    $edRestoreDesktop=-not ([string]$edExperienceFailure -match 'PLAYER_IN_USE|DESKTOP_BUSY') -and -not @(Get-Process -Name EasternDynasty -ErrorAction SilentlyContinue).Count
    if($edRestoreDesktop){[EDExperiencePointer]::SetCursorPos($edExperienceCursor.X,$edExperienceCursor.Y) | Out-Null}
    # Leave a player's newly opened client alone; restore the old foreground only when no player took over.
    if($edRestoreDesktop -and $edExperienceForeground -ne [IntPtr]::Zero){[EDExperiencePointer]::SetForegroundWindow($edExperienceForeground) | Out-Null}
    $edExperienceAfter=Get-ExperienceSaveHashes
    foreach($saveName in $edExperienceBefore.Keys) {
        if($edExperienceBefore[$saveName] -ne $edExperienceAfter[$saveName] -and -not $edExperienceFailure){$edExperienceFailure='Player save changed during review check: '+$saveName}
    }
    $edExperienceResult=[ordered]@{status=if($edExperienceFailure){if([string]$edExperienceFailure -match 'PLAYER_IN_USE'){'PLAYER_IN_USE'}else{'FAILED'}}else{'PASSED'};checkedUtc=[DateTime]::UtcNow.ToString('o');executable=$ExecutablePath;run=$edExperienceRun;directory=$edExperienceOutput;checks=@($edExperienceChecks.ToArray());captures=@($edExperienceCaptures.ToArray());playerSavesUnchanged=(($edExperienceBefore | ConvertTo-Json -Compress) -eq ($edExperienceAfter | ConvertTo-Json -Compress));failure=if($edExperienceFailure){[string]$edExperienceFailure}else{$null}}
    $edExperienceJson=$edExperienceResult | ConvertTo-Json -Depth 5
    [IO.File]::WriteAllText((Join-Path $edExperienceOutput 'result.json'),$edExperienceJson,[Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $edExperienceBase 'latest.json'),$edExperienceJson,[Text.UTF8Encoding]::new($false))
}
if($edExperienceFailure){throw $edExperienceFailure}
Write-Output ('EXPERIENCE_UI_CLICKS_OK: '+$edExperienceChecks.Count+' assertions; '+$edExperienceCaptures.Count+' fresh paired captures; '+$edExperienceOutput)
