param([Parameter(Mandatory=$true)][string]$DestinationPath,[switch]$Replace,[switch]$DiscardPrevious,[string]$BuildPath,[string]$ExpectedManifestPath)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$edRoot=Get-EdProjectRoot
$edBuild=if($BuildPath){[IO.Path]::GetFullPath($BuildPath).TrimEnd('\')}else{[IO.Path]::GetFullPath((Join-Path $edRoot 'Builds\Windows')).TrimEnd('\')}
$edDestination=[IO.Path]::GetFullPath($DestinationPath).TrimEnd('\')
$edDriveRoot=[IO.Path]::GetPathRoot($edDestination).TrimEnd('\')
if($edDestination -eq $edDriveRoot -or $edDestination -eq $edRoot -or $edDestination -eq $edBuild -or $edBuild.StartsWith($edDestination+'\',[StringComparison]::OrdinalIgnoreCase) -or
   $edDestination.StartsWith($edBuild+'\',[StringComparison]::OrdinalIgnoreCase) -or
   $edRoot.StartsWith($edDestination+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Install destination must be a dedicated folder outside the build tree and must not contain the project.'}
if(-not (Test-Path -LiteralPath (Join-Path $edBuild 'EasternDynasty.exe')) -or -not (Test-Path -LiteralPath (Join-Path $edBuild 'EasternDynasty_Data'))){throw 'Complete Windows build missing. Run Tools/Build-Windows.ps1 first.'}
$edExists=Test-Path -LiteralPath $edDestination
function Assert-EdDestinationIdle {
    $edActive=@(Get-CimInstance Win32_Process -ErrorAction Stop | Where-Object {
        $_.ExecutablePath -and $_.ExecutablePath.StartsWith($edDestination+'\',[StringComparison]::OrdinalIgnoreCase)
    })
    if($edActive.Count -gt 0){throw 'INSTALL_IN_USE: Target client is running; preserve it and publish after exit.'}
    # Fail closed if the game process cannot be inspected under this account.
    if(@(Get-CimInstance Win32_Process -Filter "Name='EasternDynasty.exe'" -ErrorAction Stop | Where-Object {-not $_.ExecutablePath}).Count -gt 0){throw 'INSTALL_IN_USE: Cannot establish the running client location.'}
}
Assert-EdDestinationIdle
if($edExists -and -not (Test-Path -LiteralPath $edDestination -PathType Container)){throw 'Destination is a file.'}
if($edExists -and @(Get-ChildItem -LiteralPath $edDestination -Force).Count -gt 0) {
    if(-not $Replace){throw 'Destination is not empty; use a new folder or -Replace for a previous installation.'}
    $edMarker=Join-Path $edDestination '.eastern-dynasty-install.json'
    if(-not (Test-Path -LiteralPath $edMarker) -or (Get-Content -LiteralPath $edMarker -Raw -Encoding UTF8 | ConvertFrom-Json).product -ne 'EasternDynasty'){throw 'Refusing to replace an unrelated folder without the installation marker.'}
}
foreach($edTree in @($edBuild,$edDestination)) {
    $edCheck=$edTree
    while($edCheck) {
        if(Test-Path -LiteralPath $edCheck){if((Get-Item -LiteralPath $edCheck -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Install paths must not traverse reparse points.'}}
        $edCheck=Split-Path -Parent $edCheck
    }
    if((Test-Path -LiteralPath $edTree) -and @(Get-ChildItem -LiteralPath $edTree -Recurse -Force -Attributes ReparsePoint).Count -gt 0){throw 'Install tree contains reparse points.'}
}
$edParent=Split-Path -Parent $edDestination
$edLeaf=Split-Path -Leaf $edDestination
New-Item -ItemType Directory -Path $edParent -Force | Out-Null
$edStage=Join-Path $edParent ('.'+$edLeaf+'.stage-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $edStage | Out-Null
Get-ChildItem -LiteralPath $edBuild -Force | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination $edStage -Recurse -Force}
$edManifest=@(foreach($edFile in Get-ChildItem -LiteralPath $edBuild -File -Recurse -Force){
    $edRelative=$edFile.FullName.Substring($edBuild.Length+1)
    $edHash=(Get-FileHash -LiteralPath $edFile.FullName).Hash
    if((Get-FileHash -LiteralPath (Join-Path $edStage $edRelative)).Hash -ne $edHash){throw ('Install verification failed: '+$edRelative)}
    [pscustomobject]@{path=$edRelative;sha256=$edHash}
})
$edRecord=@{product='EasternDynasty';installedUtc=[DateTime]::UtcNow.ToString('o');files=$edManifest} | ConvertTo-Json -Depth 4
if($ExpectedManifestPath){
    $edExpected=(Get-Content -LiteralPath $ExpectedManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json).files
    if(-not $edExpected -or @($edExpected).Count -ne $edManifest.Count){throw 'Validated manifest does not match staged file count.'}
    $edActual=@{};foreach($edEntry in $edManifest){$edActual[$edEntry.path]=$edEntry.sha256}
    foreach($edEntry in $edExpected){if(-not $edActual.ContainsKey($edEntry.path) -or $edActual[$edEntry.path] -ne $edEntry.sha256){throw ('Validated manifest does not match staged bytes: '+$edEntry.path)}}
}
[IO.File]::WriteAllText((Join-Path $edStage '.eastern-dynasty-install.json'),$edRecord,[Text.UTF8Encoding]::new($false))
$edBackup=$null
# All recursive moves are confined to the checked absolute destination and its generated sibling.
if([IO.Path]::GetFullPath($edStage) -ne $edStage -or (Split-Path -Parent $edStage) -ne $edParent){throw 'Staging path validation failed.'}
if($edExists) {
    Assert-EdDestinationIdle
    $edBackup=Join-Path $edParent ($edLeaf+'.previous-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,6))
    if((Split-Path -Parent $edBackup) -ne $edParent){throw 'Backup path validation failed.'}
    Move-Item -LiteralPath $edDestination -Destination $edBackup
}
try {Move-Item -LiteralPath $edStage -Destination $edDestination}
catch {if($edBackup -and -not (Test-Path -LiteralPath $edDestination)){Move-Item -LiteralPath $edBackup -Destination $edDestination};throw}
Write-Output ('INSTALLED: '+(Join-Path $edDestination 'EasternDynasty.exe')+'; verified '+$edManifest.Count+' files.')
if($edBackup -and $DiscardPrevious){
    # Only remove this invocation's generated backup after the verified replacement is installed.
    $edResolvedBackup=[IO.Path]::GetFullPath($edBackup)
    if((Split-Path -Parent $edResolvedBackup) -ne $edParent -or -not (Split-Path -Leaf $edResolvedBackup).StartsWith($edLeaf+'.previous-',[StringComparison]::OrdinalIgnoreCase)){throw 'Backup cleanup boundary failed.'}
    $edOldMarker=Join-Path $edResolvedBackup '.eastern-dynasty-install.json'
    if(-not (Test-Path -LiteralPath $edOldMarker) -or (Get-Content -LiteralPath $edOldMarker -Raw -Encoding UTF8 | ConvertFrom-Json).product -ne 'EasternDynasty'){throw 'Backup cleanup requires a known installation marker.'}
    if(@(Get-ChildItem -LiteralPath $edResolvedBackup -Recurse -Force -Attributes ReparsePoint).Count -gt 0){throw 'Backup cleanup refuses reparse points.'}
    Remove-Item -LiteralPath $edResolvedBackup -Recurse -Force
} elseif($edBackup){Write-Output ('Previous installation retained: '+$edBackup)}
Write-Output 'User saves remain in AppData/LocalLow/EasternDynasty/EasternDynasty.'
