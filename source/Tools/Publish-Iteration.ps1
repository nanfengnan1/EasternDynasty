param(
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]*$')][string]$Version = ('s1-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')),
    [switch]$ValidateOnly,
    [switch]$InstallVerified,
    [switch]$PrepareOnly,
    [string]$DestinationPath
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$edRoot=Get-EdProjectRoot
$edDestination=if($DestinationPath){[IO.Path]::GetFullPath($DestinationPath).TrimEnd('\')}else{Join-Path (Split-Path -Parent $edRoot) 'games'}
$edRun=Join-Path $edRoot ('Validation\Releases\'+$Version)
New-Item -ItemType Directory -Path $edRun -Force | Out-Null
$edResultFile=Join-Path $edRun 'release.json'
function Write-ReleaseResult([string]$State,[string]$Message) {
    $result=[ordered]@{version=$Version;status=$State;message=$Message;atUtc=[DateTime]::UtcNow.ToString('o');executable=(Join-Path $edDestination 'EasternDynasty.exe');evidence=$edRun}
    [IO.File]::WriteAllText($edResultFile,($result|ConvertTo-Json -Depth 4),[Text.UTF8Encoding]::new($false))
}
function Test-PlayerRunning { return @(Get-CimInstance Win32_Process -Filter "Name='EasternDynasty.exe'" -ErrorAction Stop).Count -gt 0 }
function Test-DesktopAvailable { return (& (Join-Path $PSScriptRoot 'Get-DesktopState.ps1')).ready }
function Wait-DesktopSettled {
    # Opening/resizing our visual-check windows can itself update Windows' input
    # timestamp. Allow those events to age out before classifying the desktop busy.
    $deadline=[DateTime]::UtcNow.AddSeconds(35)
    do {
        if(Test-DesktopAvailable){return $true}
        if(Test-PlayerRunning){return $false}
        Start-Sleep -Milliseconds 500
    } while([DateTime]::UtcNow -lt $deadline)
    return $false
}
function Assert-ForegroundAvailable([switch]$Settle) {
    if(Test-PlayerRunning){throw 'PLAYER_IN_USE: Build retained; an existing player is in use. Foreground testing and installation deferred.'}
    $ready=if($Settle){Wait-DesktopSettled}else{Test-DesktopAvailable}
    if(-not $ready){
        if(Test-PlayerRunning){throw 'PLAYER_IN_USE: A player opened while waiting for foreground verification.'}
        throw 'DESKTOP_BUSY: Build retained; foreground verification waits for 30 idle seconds.'
    }
}
function Get-SourceFingerprint {
    $records=@(foreach($folder in @('Assets','Packages','ProjectSettings')) {
        Get-ChildItem -LiteralPath (Join-Path $edRoot $folder) -Recurse -File -Force | Sort-Object FullName | ForEach-Object {
            $_.FullName.Substring($edRoot.Length+1)+':'+(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    })
    $sha=[Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($records -join "`n")))).Replace('-','') } finally {$sha.Dispose()}
}
function Get-SourceFiles {
    return @(foreach($folder in @('Assets','Packages','ProjectSettings')) {
        Get-ChildItem -LiteralPath (Join-Path $edRoot $folder) -Recurse -File -Force | Sort-Object FullName | ForEach-Object {
            @{path=$_.FullName.Substring($edRoot.Length+1);sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
        }
    })
}
function Get-PreparationToolsFingerprint {
    # A changed validator/build helper must invalidate an earlier preparation.
    # Match publication-queue's Validation input allowlist. Generated receipts,
    # screenshots and compiled test outputs must not invalidate their own build.
    $inputs=@(Get-ChildItem -LiteralPath $PSScriptRoot -Recurse -File -Force)
    $validationRoot=Join-Path $edRoot 'Validation'
    $inputs+=@(Get-ChildItem -LiteralPath $validationRoot -File -Force | Where-Object {$_.Extension -in @('.cs','.mjs','.ps1')})
    $fixture=Join-Path $validationRoot 'CardDisplayChecks\ExportCardDisplay.cs'
    if(Test-Path -LiteralPath $fixture -PathType Leaf){$inputs+=Get-Item -LiteralPath $fixture -Force}
    $unityConfig=Join-Path $edRoot '.local\unity.json'
    if(Test-Path -LiteralPath $unityConfig -PathType Leaf){$inputs+=Get-Item -LiteralPath $unityConfig -Force}
    $records=@($inputs | Sort-Object FullName | ForEach-Object {
        $_.FullName.Substring($edRoot.Length+1)+':'+(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    })
    $sha=[Security.Cryptography.SHA256]::Create()
    try{return [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($records -join "`n")))).Replace('-','')}finally{$sha.Dispose()}
}
function Get-BuildFiles {
    $buildRoot=Join-Path $edRoot 'Builds\Windows'
    if(-not (Test-Path -LiteralPath (Join-Path $buildRoot 'EasternDynasty.exe') -PathType Leaf)){throw 'Prepared Windows executable is missing.'}
    if((Get-Item -LiteralPath $buildRoot -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Build directory cannot be a reparse point.'}
    if(@(Get-ChildItem -LiteralPath $buildRoot -Recurse -Force -Attributes ReparsePoint).Count -gt 0){throw 'Build contains reparse points.'}
    return @(Get-ChildItem -LiteralPath $buildRoot -File -Recurse -Force | Sort-Object FullName | ForEach-Object {
        @{path=$_.FullName.Substring($buildRoot.Length+1);sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
    })
}
function Assert-BuildManifest([object]$Files) {
    $actual=@(Get-BuildFiles)
    if(@($Files).Count -eq 0 -or $actual.Count -ne @($Files).Count){throw 'Validated build file count changed.'}
    $actualByPath=@{};foreach($entry in $actual){$actualByPath[$entry.path]=$entry.sha256}
    $seen=@{}
    foreach($entry in $Files){
        $relative=[string]$entry.path
        if(-not $relative -or [IO.Path]::IsPathRooted($relative) -or $relative.Contains(':') -or
            @($relative -split '[\\/]' | Where-Object {$_ -eq '' -or $_ -eq '.' -or $_ -eq '..' -or $_ -match '[ .]$'}).Count -gt 0){throw 'Invalid build receipt path.'}
        $relative=$relative.Replace('/','\')
        if($seen.ContainsKey($relative)){throw 'Duplicate build receipt path.'}
        $seen[$relative]=$true
        if(-not $actualByPath.ContainsKey($relative) -or $entry.sha256 -notmatch '^[A-Fa-f0-9]{64}$' -or $actualByPath[$relative] -ne $entry.sha256){throw ('Validated build changed: '+$relative)}
    }
}
function Write-ReleaseJson([string]$File,[object]$Value) {
    $temporary=$File+'.'+[Guid]::NewGuid().ToString('N')+'.tmp'
    [IO.File]::WriteAllText($temporary,($Value|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
    Move-Item -LiteralPath $temporary -Destination $File -Force
}
function Get-PreparedReceipt {
    $file=Join-Path $edRun 'preparation.json'
    if(-not (Test-Path -LiteralPath $file -PathType Leaf)){return $null}
    try {
        $receipt=Get-Content -LiteralPath $file -Raw -Encoding UTF8 | ConvertFrom-Json
        if($receipt.schema -ne 1 -or $receipt.version -ne $Version -or $receipt.projectRoot -ne $edRoot -or
            $receipt.sourceSha256 -ne (Get-SourceFingerprint) -or $receipt.toolsSha256 -ne (Get-PreparationToolsFingerprint)){throw 'Prepared inputs changed.'}
        $requiredChecks=@('prepare','serialization','core','assets','build','unity-repository')
        if(@($receipt.checks).Count -ne $requiredChecks.Count -or @($requiredChecks | Where-Object {$receipt.checks -cnotcontains $_}).Count -ne 0){throw 'Preparation receipt does not contain all required checks.'}
        Assert-BuildManifest $receipt.files
        return $receipt
    } catch {
        Write-Host ('Preparation checkpoint cannot be reused: '+$_.Exception.Message)
        return $null
    }
}
function Get-SaveHashes {
    $saveRoot=Join-Path $env:USERPROFILE 'AppData\LocalLow\EasternDynasty\EasternDynasty'
    $hashes=@{}
    foreach($name in @('campaign.json','campaign.json.bak','youzhou-40-v1.json','youzhou-40-v1.json.bak')) {
        $file=Join-Path $saveRoot $name
        $hashes[$name]=if(Test-Path -LiteralPath $file){(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash}else{$null}
    }
    return $hashes
}
function Invoke-ReleaseCheck([string]$Name,[scriptblock]$Action) {
    $global:LASTEXITCODE=0
    & $Action *>&1 | Tee-Object -FilePath (Join-Path $edRun ($Name+'.log')) | Out-Host
    if($LASTEXITCODE -ne 0){throw ('Check '+$Name+' exited with code '+$LASTEXITCODE)}
}
if(([int]$ValidateOnly.IsPresent+[int]$InstallVerified.IsPresent+[int]$PrepareOnly.IsPresent) -gt 1){throw 'Choose only one of ValidateOnly, InstallVerified or PrepareOnly.'}
try {
    if($InstallVerified){
        if(Test-PlayerRunning){Write-ReleaseResult 'waiting_for_player' 'Player is running. Validated installation retained for a later retry.'; exit 0}
        $validationFile=Join-Path $edRun 'validation.json'
        if(-not (Test-Path -LiteralPath $validationFile)){throw 'No completed validation receipt for this version.'}
        $receipt=Get-Content -LiteralPath $validationFile -Raw -Encoding UTF8 | ConvertFrom-Json
        if($receipt.version -ne $Version -or $receipt.sourceSha256 -ne (Get-SourceFingerprint)){throw 'Validated source no longer matches.'}
        if($receipt.toolsSha256 -ne (Get-PreparationToolsFingerprint)){throw 'Validated preparation tools no longer match.'}
        Assert-BuildManifest $receipt.files
        if(Test-PlayerRunning){Write-ReleaseResult 'waiting_for_player' 'Player opened before installation. Validated build retained.'; exit 0}
        Invoke-ReleaseCheck 'install' { & (Join-Path $PSScriptRoot 'Install-Build.ps1') -DestinationPath $edDestination -Replace -DiscardPrevious -ExpectedManifestPath $validationFile }
        Write-ReleaseResult 'published' 'Validated complete client installed; previous inactive installation replaced.'
        exit 0
    }
    $edPrepared=Get-PreparedReceipt
    if(-not $edPrepared){
        $edToolsFingerprint=Get-PreparationToolsFingerprint
        Invoke-ReleaseCheck 'prepare' { Invoke-EdUnity '' 'EasternDynasty.Editor.ProjectTools.Prepare' (Join-Path $edRun 'unity-prepare.log') }
        # A clean snapshot has no previous generated serialization fixtures.
        # Generate all eight from this snapshot before the CLR compatibility tests.
        Invoke-ReleaseCheck 'serialization' { Invoke-EdUnity '' 'EasternDynasty.Editor.ProjectTools.VerifySerialization' (Join-Path $edRun 'unity-serialization.log') }
        $edFingerprint=Get-SourceFingerprint
        Invoke-ReleaseCheck 'core' { & (Join-Path $PSScriptRoot 'Validate.ps1') }
        Invoke-ReleaseCheck 'assets' { & (Join-Path $PSScriptRoot 'Validate-Assets.ps1') }
        Invoke-ReleaseCheck 'build' { & (Join-Path $PSScriptRoot 'Build-Windows.ps1') }
        Invoke-ReleaseCheck 'repository' { Invoke-EdUnity '' 'EasternDynasty.Editor.ProjectTools.VerifyArchitecture' (Join-Path $edRun 'unity-repository.log') }
        if((Get-SourceFingerprint) -ne $edFingerprint -or (Get-PreparationToolsFingerprint) -ne $edToolsFingerprint){throw 'Source or preparation tools changed while preparing the build; refusing a mixed release.'}
        $edPrepared=[ordered]@{schema=1;version=$Version;projectRoot=$edRoot;sourceSha256=$edFingerprint;toolsSha256=$edToolsFingerprint;preparedUtc=[DateTime]::UtcNow.ToString('o');checks=@('prepare','serialization','core','assets','build','unity-repository');sourceFiles=@(Get-SourceFiles);files=@(Get-BuildFiles)}
        Write-ReleaseJson (Join-Path $edRun 'preparation.json') $edPrepared
    } else {
        $edFingerprint=$edPrepared.sourceSha256
        Write-Host ('Reusing verified headless preparation for '+$Version)
    }
    if($PrepareOnly){Write-ReleaseResult 'prepared' 'Headless checks and Windows build completed. Foreground validation and installation remain pending.'; exit 0}
    Assert-ForegroundAvailable
    # A user may play while headless checks run. Protect their saves over the
    # foreground verification interval, after establishing that player is idle.
    $edSaves=Get-SaveHashes
    $edStart=Get-Date
    if(-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'Check-Routes.ps1'))){throw 'Required rendered route regression script is missing.'}
    Invoke-ReleaseCheck 'routes' { & (Join-Path $PSScriptRoot 'Check-Routes.ps1') -Tag $Version }
    Assert-ForegroundAvailable -Settle
    Invoke-ReleaseCheck 'youzhou-1440' { & (Join-Path $PSScriptRoot 'Validate-Visual.ps1') -Tag ($Version+'-youzhou') }
    Assert-ForegroundAvailable -Settle
    Invoke-ReleaseCheck 'youzhou-1280' { & (Join-Path $PSScriptRoot 'Validate-Visual.ps1') -Tag ($Version+'-youzhou') -PixelWidth 1280 -PixelHeight 720 }
    Assert-ForegroundAvailable -Settle
    Invoke-ReleaseCheck 'mobile-1600' { & (Join-Path $PSScriptRoot 'Validate-Visual.ps1') -Tag ($Version+'-mobile') -PixelWidth 1600 -PixelHeight 720 -MobileReview }
    Assert-ForegroundAvailable -Settle
    Invoke-ReleaseCheck 'legacy' { & (Join-Path $PSScriptRoot 'Validate-Visual.ps1') -Tag ($Version+'-legacy') -Legacy }
    Assert-ForegroundAvailable -Settle
    Invoke-ReleaseCheck 'mouse' { & (Join-Path $PSScriptRoot 'Check-UiClicks.ps1') }
    $mouseArchive=Join-Path $edRun 'mouse-captures';New-Item -ItemType Directory -Path $mouseArchive -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $edRoot 'Validation\Visual\youzhou-interactive') -File | Where-Object {$_.LastWriteTime -ge $edStart} | Copy-Item -Destination $mouseArchive -Force
    Assert-ForegroundAvailable -Settle
    Invoke-ReleaseCheck 'mouse-s1' { & (Join-Path $PSScriptRoot 'Check-UiClicks.ps1') -S1 }
    $mouseS1Archive=Join-Path $edRun 'mouse-s1-captures';New-Item -ItemType Directory -Path $mouseS1Archive -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $edRoot 'Validation\Visual\s1-interactive') -File | Where-Object {$_.LastWriteTime -ge $edStart} | Copy-Item -Destination $mouseS1Archive -Force
    Assert-ForegroundAvailable -Settle
    if(-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'Check-Experience.ps1'))){throw 'Required experience regression script is missing.'}
    Invoke-ReleaseCheck 'experience' { & (Join-Path $PSScriptRoot 'Check-Experience.ps1') }
    $edAfter=Get-SaveHashes
    foreach($name in $edSaves.Keys){if($edSaves[$name] -ne $edAfter[$name]){throw ('Player save changed during read-only verification: '+$name)}}
    if((Get-SourceFingerprint) -ne $edFingerprint){throw 'Source changed while verifying the build; refusing a mixed release.'}
    if((Get-PreparationToolsFingerprint) -ne $edPrepared.toolsSha256){throw 'Preparation tools changed during foreground verification.'}
    Assert-BuildManifest $edPrepared.files
    if(Test-PlayerRunning){Write-ReleaseResult 'waiting_for_player' 'Player is in use; verified build was not installed.'; exit 0}
    $receipt=[ordered]@{version=$Version;scope='S1 Youzhou 40x40';sourceSha256=$edFingerprint;toolsSha256=$edPrepared.toolsSha256;verifiedUtc=[DateTime]::UtcNow.ToString('o');checks=@('core','assets','unity-repository','routes','youzhou-1440','youzhou-1280','mobile-1600','legacy','mouse','mouse-s1','experience');savesUnchanged=$true}
    $receipt.sourceFiles=@(Get-SourceFiles)
    [IO.File]::WriteAllText((Join-Path $edRoot 'Builds\Windows\version.json'),($receipt|ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
    $receipt.files=@(Get-BuildFiles)
    [IO.File]::WriteAllText((Join-Path $edRun 'validation.json'),($receipt|ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
    if($ValidateOnly){Write-ReleaseResult 'validated' 'Candidate passed all checks and awaits screenshot review before installation.'; exit 0}
    if(Test-PlayerRunning){Write-ReleaseResult 'waiting_for_player' 'Player opened before installation. Validated build retained.'; exit 0}
    Invoke-ReleaseCheck 'install' { & (Join-Path $PSScriptRoot 'Install-Build.ps1') -DestinationPath $edDestination -Replace -DiscardPrevious -ExpectedManifestPath (Join-Path $edRun 'validation.json') }
    Write-ReleaseResult 'published' 'All required checks passed. Verified complete client installed; previous inactive installation replaced.'
} catch {
    if($_.Exception.Message -match 'DESKTOP_BUSY') {Write-ReleaseResult 'waiting_for_desktop' $_.Exception.Message; exit 0}
    if($_.Exception.Message -match 'PLAYER_IN_USE|INSTALL_IN_USE') {Write-ReleaseResult 'waiting_for_player' $_.Exception.Message; exit 0}
    Write-ReleaseResult 'failed' $_.Exception.Message
    throw
}
