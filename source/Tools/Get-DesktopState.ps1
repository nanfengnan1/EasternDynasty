param([ValidateRange(0,3600)][int]$MinimumIdleSeconds=30)
$ErrorActionPreference='Stop'
if(-not ('EDDesktopState' -as [type])) {
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class EDDesktopState {
    [StructLayout(LayoutKind.Sequential)] private struct LASTINPUTINFO {public uint size,time;}
    [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LASTINPUTINFO value);
    [DllImport("kernel32.dll")] private static extern uint GetTickCount();
    public static double IdleSeconds() {
        var value=new LASTINPUTINFO {size=(uint)Marshal.SizeOf(typeof(LASTINPUTINFO))};
        if(!GetLastInputInfo(ref value))throw new InvalidOperationException("Cannot inspect desktop input state.");
        return unchecked(GetTickCount()-value.time)/1000.0;
    }
}
'@
}
$edIdle=[EDDesktopState]::IdleSeconds()
[pscustomobject]@{ready=($edIdle -ge $MinimumIdleSeconds);idleSeconds=$edIdle;minimumIdleSeconds=$MinimumIdleSeconds}
