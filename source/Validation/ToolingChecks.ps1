$ErrorActionPreference='Stop'
$edRoot=Split-Path -Parent $PSScriptRoot
. (Join-Path $edRoot 'Tools\Common.ps1')
$script:edChecks=0
function Check-EdTool([bool]$Value,[string]$Message){if(-not $Value){throw $Message};$script:edChecks++}
function Expect-EdToolFailure([scriptblock]$Action,[string]$Message){$failed=$false;try{& $Action | Out-Null}catch{$failed=$true};Check-EdTool $failed $Message}
$edRun=Join-Path $PSScriptRoot ('ToolingChecks\'+[Guid]::NewGuid().ToString('N'))
$edDestination=Join-Path $edRun 'Client With Spaces'
$edInstaller=Join-Path $edRoot 'Tools\Install-Build.ps1'
Check-EdTool ((Get-EdProjectRoot) -eq $edRoot) 'Project root depends on the current shell directory.'
Check-EdTool ((Get-EdUnityVersion) -eq '6000.3.27f1') 'Wrong Unity version.'
Check-EdTool (Test-Path -LiteralPath (Resolve-EdUnityEditor)) 'Unity discovery failed.'
Check-EdTool ((ConvertTo-EdProcessArguments @('-logFile','C:\A B\log.txt')) -eq '"-logFile" "C:\A B\log.txt"') 'Process argument quoting failed.'
Expect-EdToolFailure {ConvertTo-EdProcessArguments @('bad"argument')} 'Unsafe process argument accepted.'
Check-EdTool (Test-EdProjectCommandLine ('Unity.exe -projectPath "'+$edRoot+'" -batchmode') $edRoot) 'Exact project path was not detected.'
Check-EdTool (-not (Test-EdProjectCommandLine ('Unity.exe -projectPath "'+$edRoot+'-copy"') $edRoot)) 'Sibling project was treated as the active project.'
Check-EdTool (Test-EdProjectCommandLine 'Unity.exe -projectPath="C:\A B\Project"' 'C:\A B\Project') 'Quoted project path with spaces failed.'
Check-EdTool (-not (Test-EdProjectCommandLine 'Unity.exe serve' $edRoot)) 'Hub process was treated as an open Editor.'
Expect-EdToolFailure {Confirm-EdUnityEditor (Resolve-EdUnityEditor) '0.0.0'} 'Wrong Unity version was accepted.'
Expect-EdToolFailure {& $edInstaller -DestinationPath $edRoot -Replace} 'Installer accepted the project root.'
Expect-EdToolFailure {& $edInstaller -DestinationPath (Join-Path $edRoot 'Builds\Windows\Nested')} 'Installer accepted its build source.'
& $edInstaller -DestinationPath $edDestination
$edMarkerPath=Join-Path $edDestination '.eastern-dynasty-install.json'
$edMarker=Get-Content -LiteralPath $edMarkerPath -Raw -Encoding UTF8 | ConvertFrom-Json
Check-EdTool ($edMarker.product -eq 'EasternDynasty' -and $edMarker.files.Count -gt 100) 'Installation package incomplete.'
Check-EdTool (Test-Path -LiteralPath (Join-Path $edDestination 'EasternDynasty_Data')) 'Player data directory missing.'
Check-EdTool (Test-Path -LiteralPath (Join-Path $edDestination 'UnityPlayer.dll')) 'UnityPlayer.dll missing.'
$edBefore=(Get-FileHash -LiteralPath $edMarkerPath).Hash
Expect-EdToolFailure {& $edInstaller -DestinationPath $edDestination} 'Installer silently replaced a nonempty directory.'
Check-EdTool ((Get-FileHash -LiteralPath $edMarkerPath).Hash -eq $edBefore) 'Rejected installation changed the destination.'
$edUnrelated=Join-Path $edRun 'Unrelated Folder'
New-Item -ItemType Directory -Path $edUnrelated -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $edUnrelated 'keep.txt'),'do not change')
Expect-EdToolFailure {& $edInstaller -DestinationPath $edUnrelated -Replace} 'Installer accepted an unrelated nonempty folder.'
Check-EdTool ([IO.File]::ReadAllText((Join-Path $edUnrelated 'keep.txt')) -eq 'do not change') 'Unrelated file changed.'
& $edInstaller -DestinationPath $edDestination -Replace
$edBackups=@(Get-ChildItem -LiteralPath $edRun -Directory | Where-Object {$_.Name -like 'Client With Spaces.previous-*'})
Check-EdTool ($edBackups.Count -eq 1) 'Previous installation was not retained.'
Check-EdTool ((Get-FileHash -LiteralPath (Join-Path $edBackups[0].FullName '.eastern-dynasty-install.json')).Hash -eq $edBefore) 'Backup differs from previous installation.'
Check-EdTool (@(Get-ChildItem -LiteralPath $edRun -Directory -Force | Where-Object {$_.Name -like '*.stage-*'}).Count -eq 0) 'Completed installation left staging directory.'
foreach($edFile in $edMarker.files){Check-EdTool ((Get-FileHash -LiteralPath (Join-Path $edDestination $edFile.path)).Hash -eq $edFile.sha256) ('Installed file changed: '+$edFile.path)}
$edResult='PASS: '+$script:edChecks+' tooling checks; process arguments, root discovery, verified install, overwrite guards and retained backup.'
[IO.File]::WriteAllText((Join-Path $edRun 'result.txt'),$edResult)
Write-Output $edResult
Write-Output ('Evidence: '+$edRun)
