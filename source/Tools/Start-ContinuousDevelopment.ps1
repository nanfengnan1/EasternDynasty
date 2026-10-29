param([switch]$HoldForCurrentDevelopment)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$edRoot=Get-EdProjectRoot
$edConfig=Get-Content -LiteralPath (Join-Path $edRoot 'Automation\config.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$edLocal=Join-Path $edRoot '.local\continuous-development'
New-Item -ItemType Directory -Path $edLocal -Force | Out-Null
$edNode=(Get-Command node.exe -ErrorAction Stop).Source
$edCodex=(Get-Command codex.cmd -ErrorAction Stop).Source
$edEntry=Join-Path (Split-Path -Parent $edCodex) 'node_modules\@openai\codex\bin\codex.js'
if(-not (Test-Path -LiteralPath $edEntry)){throw 'Installed Codex CLI entry not found.'}
$edPowerShell=Join-Path $PSHOME 'powershell.exe'
[IO.File]::WriteAllText((Join-Path $edLocal 'paths.json'),(@{node=$edNode;codexEntry=$edEntry;powershell=$edPowerShell}|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
if($HoldForCurrentDevelopment){[IO.File]::WriteAllText((Join-Path $edLocal 'manual-hold.json'),(@{reason='Initial parallel development and review is active; root will remove this hold after handoff.';atUtc=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json),[Text.UTF8Encoding]::new($false))}
# Five-minute ticks inspect independent state; only the development state controls hourly AI work.
$edArgs=ConvertTo-EdProcessArguments @('-NoProfile','-ExecutionPolicy','Bypass','-WindowStyle','Hidden','-File',(Join-Path $edRoot 'Tools\Tick-ContinuousDevelopment.ps1'))
$edAction=New-ScheduledTaskAction -Execute $edPowerShell -Argument $edArgs -WorkingDirectory $edRoot
$edTrigger=New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(5) -RepetitionInterval (New-TimeSpan -Minutes 5)
$edSettings=New-ScheduledTaskSettingsSet -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Minutes 60) -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
$edPrincipal=New-ScheduledTaskPrincipal -UserId ([Security.Principal.WindowsIdentity]::GetCurrent().Name) -LogonType Interactive -RunLevel Limited
Register-ScheduledTask -TaskName $edConfig.taskName -Action $edAction -Trigger $edTrigger -Settings $edSettings -Principal $edPrincipal -Description ('S1 Youzhou: improve main source every '+$edConfig.intervalHours+' hour(s), then freeze and enqueue a release; publication runs independently.') -Force | Select-Object TaskName,State
$edPublishArgs=ConvertTo-EdProcessArguments @('-NoProfile','-ExecutionPolicy','Bypass','-WindowStyle','Hidden','-File',(Join-Path $edRoot 'Tools\Tick-Publication.ps1'))
$edPublishAction=New-ScheduledTaskAction -Execute $edPowerShell -Argument $edPublishArgs -WorkingDirectory $edRoot
$edPublishSettings=New-ScheduledTaskSettingsSet -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Minutes 165) -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
Register-ScheduledTask -TaskName $edConfig.publicationTaskName -Action $edPublishAction -Trigger $edTrigger -Settings $edPublishSettings -Principal $edPrincipal -Description 'S1 Youzhou: verify isolated fixed-version snapshots and deliver Windows/APK; wait for desktop or player without blocking development.' -Force | Select-Object TaskName,State
Write-Output ('STATUS: '+(Join-Path $edLocal 'status.json'))
Write-Output ('PUBLICATION: '+(Join-Path $edRoot '.local\publication\status.json'))
