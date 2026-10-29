$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$run=Join-Path $PSScriptRoot ('ReleaseChecks\'+[Guid]::NewGuid().ToString('N'))
$build=Join-Path $run 'Source With Spaces'
$destination=Join-Path $run 'Game With Spaces'
New-Item -ItemType Directory -Path (Join-Path $build 'EasternDynasty_Data') -Force | Out-Null
foreach($name in @('EasternDynasty.exe','UnityPlayer.dll','EasternDynasty_Data\data.bin')){[IO.File]::WriteAllText((Join-Path $build $name),'fixture '+$name)}
$installer=Join-Path $root 'Tools\Install-Build.ps1'
$script:checks=0
function Check([bool]$condition,[string]$name){if(-not $condition){throw $name};$script:checks++}
function Reject([scriptblock]$action,[string]$expected){$message='';try{& $action|Out-Null}catch{$message=$_.Exception.Message};Check ($message -like ('*'+$expected+'*')) ('Expected rejection '+$expected+', received '+$message)}
& $installer -BuildPath $build -DestinationPath $destination | Out-Null
$marker=Join-Path $destination '.eastern-dynasty-install.json'
$before=(Get-FileHash -LiteralPath $marker).Hash
function Get-CimInstance { param($ClassName,$Filter,$ErrorAction) [pscustomobject]@{Name='EasternDynasty.exe';ExecutablePath=(Join-Path $destination 'EasternDynasty.exe')} }
try {Reject {& $installer -BuildPath $build -DestinationPath $destination -Replace -DiscardPrevious} 'INSTALL_IN_USE'}finally{Remove-Item Function:\Get-CimInstance}
Check ((Get-FileHash -LiteralPath $marker).Hash -eq $before) 'In-use installation was changed.'
function Get-CimInstance { param($ClassName,$Filter,$ErrorAction) [pscustomobject]@{Name='EasternDynasty.exe';ExecutablePath=$null} }
try {Reject {& $installer -BuildPath $build -DestinationPath $destination -Replace} 'INSTALL_IN_USE'}finally{Remove-Item Function:\Get-CimInstance}
Check ((Get-FileHash -LiteralPath $marker).Hash -eq $before) 'Uninspectable process changed destination.'
& $installer -BuildPath $build -DestinationPath $destination -Replace | Out-Null
$retained=@(Get-ChildItem -LiteralPath $run -Directory | Where-Object {$_.Name -like 'Game With Spaces.previous-*'})
Check ($retained.Count -eq 1) 'Default backup behavior changed.'
[IO.File]::WriteAllText((Join-Path $build 'EasternDynasty_Data\data.bin'),'new candidate')
& $installer -BuildPath $build -DestinationPath $destination -Replace -DiscardPrevious | Out-Null
Check ([IO.File]::ReadAllText((Join-Path $destination 'EasternDynasty_Data\data.bin')) -eq 'new candidate') 'New complete package was not installed.'
Check (@(Get-ChildItem -LiteralPath $run -Directory | Where-Object {$_.Name -like 'Game With Spaces.previous-*'}).Count -eq 1) 'DiscardPrevious removed unrelated historical backup or retained its own backup.'
Check (Test-Path -LiteralPath $retained[0].FullName) 'Earlier backup was removed.'
Reject {& $installer -BuildPath $build -DestinationPath $run -Replace} 'dedicated folder'
Reject {& $installer -BuildPath $build -DestinationPath (Join-Path $build 'Nested')} 'dedicated folder'
Check (@(Get-ChildItem -LiteralPath $run -Force -Directory | Where-Object {$_.Name -like '*.stage-*'}).Count -eq 0) 'Staging remained after successful replace.'
$expected=Join-Path $run 'expected.json';Copy-Item -LiteralPath $marker -Destination $expected
$beforeRejected=(Get-FileHash -LiteralPath $marker).Hash
[IO.File]::WriteAllText((Join-Path $build 'EasternDynasty_Data\data.bin'),'unverified candidate')
Reject {& $installer -BuildPath $build -DestinationPath $destination -Replace -DiscardPrevious -ExpectedManifestPath $expected} 'Validated manifest'
Check ((Get-FileHash -LiteralPath $marker).Hash -eq $beforeRejected) 'Manifest mismatch replaced the known good installation.'
$result='PASS: '+$script:checks+' release checks; active/uninspectable player, hash preservation, verified replacement, own-backup cleanup, source/destination boundaries.'
[IO.File]::WriteAllText((Join-Path $run 'result.txt'),$result)
Write-Output $result
