$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$edRoot = Get-EdProjectRoot

function Read-EdStatusJson([string]$Path) {
    if (Test-Path -LiteralPath $Path) {
        return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    return $null
}

# Read-only: do not initialize tasks, acquire a publication job, or launch agents.
$edConfig = Read-EdStatusJson (Join-Path $edRoot 'Automation\config.json')
$edLanes = @(
    @{ Name = 'development'; Directory = 'continuous-development'; Task = $edConfig.taskName; Enabled = $edConfig.enabled },
    @{ Name = 'publication'; Directory = 'publication'; Task = $edConfig.publicationTaskName; Enabled = $edConfig.publicationEnabled }
)
$edResult = [ordered]@{ observedUtc = [DateTime]::UtcNow.ToString('o'); projectRoot = $edRoot }
foreach ($edLane in $edLanes) {
    $edLocal = Join-Path $edRoot ('.local\' + $edLane.Directory)
    $edTask = $null
    $edTaskError = $null
    try { $edTask = Get-ScheduledTask -TaskName $edLane.Task -ErrorAction Stop }
    catch { $edTaskError = $_.Exception.Message }
    $edResult[$edLane.Name] = [ordered]@{
        configuredEnabled = $edLane.Enabled
        taskName = $edLane.Task
        scheduledTaskEnabled = $(if ($edTask) { [bool]$edTask.Settings.Enabled } else { $null })
        scheduledTaskState = $(if ($edTask) { [string]$edTask.State } else { 'unknown' })
        taskReadError = $edTaskError
        hold = Read-EdStatusJson (Join-Path $edLocal 'manual-hold.json')
        lastStatus = Read-EdStatusJson (Join-Path $edLocal 'status.json')
    }
}
$edParent = Split-Path -Parent $edRoot
$edResult['delivery'] = [ordered]@{
    windowsExecutableExists = Test-Path -LiteralPath (Join-Path $edParent 'games\EasternDynasty.exe')
    windowsVersion = Read-EdStatusJson (Join-Path $edParent 'games\version.json')
    android = Read-EdStatusJson (Join-Path $edParent 'apk\build-info.json')
    note = 'A built executable or package receipt does not prove current source/UI or real-device acceptance.'
}
$edResult | ConvertTo-Json -Depth 12
