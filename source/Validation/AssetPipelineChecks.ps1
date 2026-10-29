param([string]$ProjectRoot=(Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
$ProjectRoot=[IO.Path]::GetFullPath($ProjectRoot)
$tools=Join-Path $ProjectRoot 'Tools'
. (Join-Path $tools 'Validate-Assets.ps1') -ProjectRoot $ProjectRoot -LibraryOnly
$sandboxParent=Join-Path $ProjectRoot 'Validation/AssetPipelineSandbox'
$sandbox=Join-Path $sandboxParent ([Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory((Join-Path $sandbox 'Assets/EasternDynasty')) | Out-Null
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'Assets/EasternDynasty/Resources') -Destination (Join-Path $sandbox 'Assets/EasternDynasty/Resources') -Recurse
[IO.Directory]::CreateDirectory((Join-Path $sandbox 'Inputs')) | Out-Null
$resourceRoot=Join-Path $sandbox 'Assets/EasternDynasty/Resources'
$manifestFile=Join-Path $resourceRoot 'asset-manifest.json'
$portraitA=Join-Path $resourceRoot 'Portraits/huangfu_song.png'
$portraitB=Join-Path $resourceRoot 'Portraits/han_officer.png'
$checks=0

function Assert-AssetCheck([bool]$Condition,[string]$Name) {
    if (!$Condition) { throw "FAIL: $Name" }; $script:checks++; Write-Output "PASS $Name"
}
function Asset-State {
    return (@(Get-ChildItem -LiteralPath (Join-Path $sandbox 'Assets') -File -Recurse | Sort-Object FullName | ForEach-Object { $_.FullName.Substring($sandbox.Length)+' '+(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }) -join "`n")
}
function Invoke-AssetImport([hashtable]$Extra=@{}) {
    $parameters=@{ ProjectRoot=$sandbox;Id='portrait.zhu_jun';Kind='portrait';SourcePath=$portraitA;Purpose='Pipeline test fixture only';SourceKind='user-provided';SourceReference='isolated test fixture';Rights='Test data, not a historical portrait claim';CreatedUtc='2026-10-25' }
    foreach($key in $Extra.Keys){$parameters[$key]=$Extra[$key]}
    & (Join-Path $tools 'Import-Asset.ps1') @parameters
}
function Expect-AssetFailure([string]$Name,[scriptblock]$Action,[string[]]$ErrorIncludes=@()) {
    $before=Asset-State;$failed=$false;$failureMessage=''
    try { & $Action | Out-Null } catch { $failed=$true;$failureMessage=$_.Exception.Message; Write-Output ("EXPECTED $Name : "+$failureMessage) }
    Assert-AssetCheck $failed "$Name rejects"
    foreach($fragment in $ErrorIncludes){Assert-AssetCheck ($failureMessage.Contains($fragment)) "$Name identifies $fragment"}
    Assert-AssetCheck ((Asset-State) -ceq $before) "$Name leaves all Assets bytes unchanged"
    Assert-AssetCheck (!(Test-Path -LiteralPath (Join-Path $sandbox 'AssetInbox/.asset-transaction'))) "$Name leaves no pending transaction"
}
function Write-TestManifest($Value) { [IO.File]::WriteAllText($manifestFile,($Value|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false)) }

try {
    $baselineManifest=Read-EdAssetManifest $sandbox
    $baselineReady=@($baselineManifest.entries|Where-Object {$_.status -ceq 'ready'}).Count
    $baselineMissing=@($baselineManifest.entries|Where-Object {$_.status -ceq 'missing'}).Count
    $initial=Test-EdAssetManifest $baselineManifest $sandbox -CheckFiles
    Assert-AssetCheck ($initial.Ready -eq $baselineReady -and $initial.Missing -eq $baselineMissing -and $initial.Entries['portrait.zhu_jun'].status -ceq 'missing') 'copied baseline matches actual ready/missing assets and retains the explicit missing portrait fixture'
    Invoke-AssetImport | Out-Null
    $target=Join-Path $resourceRoot 'Portraits/zhu_jun.png'
    Assert-AssetCheck ((Get-FileHash -LiteralPath $target).Hash -eq (Get-FileHash -LiteralPath $portraitA).Hash) 'import preserves PNG bytes'
    $after=Test-EdAssetManifest (Read-EdAssetManifest $sandbox) $sandbox -CheckFiles
    Assert-AssetCheck ($after.Ready -eq $initial.Ready+1 -and $after.Missing -eq $initial.Missing-1 -and $after.PendingMeta -eq $initial.PendingMeta+1) 'import changes missing status without pretending Unity already imported it'
    # Simulate the meta Unity creates, then verify whole-file preservation, not just GUID.
    Copy-Item -LiteralPath ($portraitA+'.meta') -Destination ($target+'.meta')
    $metaHash=(Get-FileHash -LiteralPath ($target+'.meta')).Hash
    $before=Asset-State;Invoke-AssetImport | Out-Null
    Assert-AssetCheck ((Asset-State) -ceq $before) 'repeat import is byte-for-byte idempotent'
    Expect-AssetFailure 'replacement without explicit switch' { Invoke-AssetImport @{SourcePath=$portraitB} }
    Invoke-AssetImport @{SourcePath=$portraitB;Replace=$true} | Out-Null
    Assert-AssetCheck ((Get-FileHash -LiteralPath $target).Hash -eq (Get-FileHash -LiteralPath $portraitB).Hash) 'explicit Replace updates PNG'
    Assert-AssetCheck ((Get-FileHash -LiteralPath ($target+'.meta')).Hash -eq $metaHash) 'Replace preserves exact meta bytes and GUID'
    $before=Asset-State;Invoke-AssetImport @{SourcePath=$portraitB;Replace=$true} | Out-Null
    Assert-AssetCheck ((Asset-State) -ceq $before) 'repeat Replace is idempotent'
    Expect-AssetFailure 'ID traversal' { Invoke-AssetImport @{Id='portrait.../escape';Replace=$true} }
    Expect-AssetFailure 'unknown kind' { Invoke-AssetImport @{Kind='executable';Replace=$true} }
    Expect-AssetFailure 'unknown source kind' { Invoke-AssetImport @{SourceKind='unverified-download';Replace=$true} }
    Expect-AssetFailure 'source URL' { Invoke-AssetImport @{SourcePath='https://example.invalid/image.png';Replace=$true} }
    Expect-AssetFailure 'source traversal' { Invoke-AssetImport @{SourcePath='../outside.png';Replace=$true} }
    Expect-AssetFailure 'wrong portrait aspect' { Invoke-AssetImport @{SourcePath=(Join-Path $resourceRoot 'Youzhou/grain.png');Replace=$true} }
    Expect-AssetFailure 'opaque portrait used as map' { Invoke-AssetImport @{Id='map.youzhou.grain';Kind='map-tile';Replace=$true} }
    $bad=Join-Path $sandbox 'Inputs/bad.png';[IO.File]::WriteAllText($bad,'not a PNG')
    Expect-AssetFailure 'invalid PNG' { Invoke-AssetImport @{SourcePath=$bad;Replace=$true} }
    $bytes=[IO.File]::ReadAllBytes($portraitA);$bytes[100]=$bytes[100] -bxor 1;[IO.File]::WriteAllBytes($bad,$bytes)
    Expect-AssetFailure 'PNG CRC corruption' { Invoke-AssetImport @{SourcePath=$bad;Replace=$true} }
    $manifestBytes=[IO.File]::ReadAllBytes($manifestFile)
    $value=Read-EdAssetManifest $sandbox;$value.entries+=@($value.entries[0]);Write-TestManifest $value
    Expect-AssetFailure 'duplicate manifest ID' { Invoke-AssetImport @{Replace=$true} }
    [IO.File]::WriteAllBytes($manifestFile,$manifestBytes)
    $value=Read-EdAssetManifest $sandbox;$value.entries[0].resourcePath='../escape';Write-TestManifest $value
    Expect-AssetFailure 'Resources path traversal' { Invoke-AssetImport @{Replace=$true} }
    [IO.File]::WriteAllBytes($manifestFile,$manifestBytes)
    $untracked=Join-Path $resourceRoot 'Portraits/untracked.png';Copy-Item -LiteralPath $portraitA -Destination $untracked
    Expect-AssetFailure 'unregistered user PNG even with Replace' { Invoke-AssetImport @{Id='portrait.untracked';Replace=$true} }
    Remove-Item -LiteralPath $untracked
    $orphan=Join-Path $resourceRoot 'Portraits/orphan.png.meta';Copy-Item -LiteralPath ($portraitA+'.meta') -Destination $orphan
    Expect-AssetFailure 'orphan meta ownership' { Invoke-AssetImport @{Id='portrait.orphan'} }
    Remove-Item -LiteralPath $orphan
    $collision=Join-Path $sandbox 'Assets/Other/Resources/Portraits';[IO.Directory]::CreateDirectory($collision)|Out-Null
    $collisionFile=Join-Path $collision 'han_officer.png';Copy-Item -LiteralPath $portraitB -Destination $collisionFile
    Expect-AssetFailure 'duplicate global Resources path' { Invoke-AssetImport @{Replace=$true} }
    Remove-Item -LiteralPath $collisionFile
    # Real OS lock forces failure after the PNG commit but before the manifest commit.
    $manifestLock=[IO.File]::Open($manifestFile,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
    try { Expect-AssetFailure 'manifest commit lock and rollback' { Invoke-AssetImport @{Replace=$true} } } finally { $manifestLock.Dispose() }
    $mutex=[IO.File]::Open((Join-Path $sandbox 'AssetInbox/.asset-import.lock'),[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    try { Expect-AssetFailure 'concurrent importer lock' { Invoke-AssetImport @{Replace=$true} } } finally { $mutex.Dispose() }
    [IO.Directory]::CreateDirectory((Join-Path $sandbox 'Temp'))|Out-Null
    $editorLock=[IO.File]::Open((Join-Path $sandbox 'Temp/UnityLockfile'),[IO.FileMode]::Create,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    try { Expect-AssetFailure 'live Unity lock' { Invoke-AssetImport @{Replace=$true} } } finally { $editorLock.Dispose() }
    # Model abrupt termination between two commits and exercise explicit recovery.
    $before=Asset-State;$txn=Join-Path $sandbox 'AssetInbox/.asset-transaction';[IO.Directory]::CreateDirectory($txn)|Out-Null
    [IO.File]::Copy($manifestFile,(Join-Path $txn 'manifest.before.json'));[IO.File]::Copy($target,(Join-Path $txn 'asset.before.png'))
    $journal=@{id='portrait.zhu_jun';kind='portrait';existed=$true;committed=$false;assetAfterSha256=(Get-FileHash -LiteralPath $portraitA).Hash.ToLowerInvariant();manifestAfterSha256='0'*64}
    [IO.File]::WriteAllText((Join-Path $txn 'journal.json'),($journal|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
    [IO.File]::Copy($portraitA,$target,$true)
    $blocked=$false;try{Test-EdAssetManifest (Read-EdAssetManifest $sandbox) $sandbox -CheckFiles|Out-Null}catch{$blocked=$true}
    Assert-AssetCheck $blocked 'unfinished transaction blocks validation'
    & (Join-Path $tools 'Import-Asset.ps1') -ProjectRoot $sandbox -Recover | Out-Null
    Assert-AssetCheck ((Asset-State) -ceq $before) 'crash recovery restores complete prior Assets state'
    $referenceInput=Join-Path $resourceRoot 'YouzhouReferences/resources.png'
    if(-not(Test-Path -LiteralPath $referenceInput)){$referenceInput=$portraitA}
    $beforeReference=Test-EdAssetManifest (Read-EdAssetManifest $sandbox) $sandbox -CheckFiles
    Invoke-AssetImport @{Id='reference.youzhou.pipeline_fixture';Kind='map-reference';SourcePath=$referenceInput} | Out-Null
    $referenceTarget=Join-Path $resourceRoot 'YouzhouReferences/pipeline_fixture.png'
    Assert-AssetCheck ((Get-FileHash -LiteralPath $referenceTarget -Algorithm SHA256).Hash -ceq (Get-FileHash -LiteralPath $referenceInput -Algorithm SHA256).Hash) 'map-reference import preserves exact source PNG bytes'
    $afterReference=Test-EdAssetManifest (Read-EdAssetManifest $sandbox) $sandbox -CheckFiles
    Assert-AssetCheck ($afterReference.Ready -eq $beforeReference.Ready+1 -and $afterReference.Missing -eq $beforeReference.Missing -and $afterReference.Entries['reference.youzhou.pipeline_fixture'].resourcePath -ceq 'YouzhouReferences/pipeline_fixture') 'map-reference uses the canonical managed directory without changing portrait coverage'
    Expect-AssetFailure 'map-reference with wrong ID namespace' { Invoke-AssetImport @{Id='map.youzhou.invalid_reference';Kind='map-reference';SourcePath=$referenceInput} }
    $referenceUntracked=Join-Path $resourceRoot 'YouzhouReferences/untracked.png'
    Copy-Item -LiteralPath $referenceInput -Destination $referenceUntracked
    try { Expect-AssetFailure 'unregistered map-reference PNG even with Replace' { Invoke-AssetImport @{Id='reference.youzhou.untracked';Kind='map-reference';SourcePath=$referenceInput;Replace=$true} } }
    finally {Remove-Item -LiteralPath $referenceUntracked}
    # A small, deliberately wide transparent fixture exercises UI import without
    # borrowing a map-reference namespace or treating test pixels as game artwork.
    $uiInput=Join-Path $sandbox 'Inputs/ui_wide_fixture.png'
    $uiBitmap=[Drawing.Bitmap]::new(191,29,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $uiBitmap.SetPixel(50,10,[Drawing.Color]::FromArgb(170,70,80,90))
        $uiBitmap.Save($uiInput,[Drawing.Imaging.ImageFormat]::Png)
    } finally {$uiBitmap.Dispose()}
    $beforeUi=Test-EdAssetManifest (Read-EdAssetManifest $sandbox) $sandbox -CheckFiles
    $uiArguments=@{Id='ui.youzhou.pipeline_fixture';Kind='ui-sprite';SourcePath=$uiInput;SourceKind='licensed-local';SourceReference='isolated transparent UI pipeline fixture, not production art';Rights='Project test fixture only'}
    Invoke-AssetImport $uiArguments | Out-Null
    $uiTarget=Join-Path $resourceRoot 'YouzhouUI/pipeline_fixture.png'
    $afterUi=Test-EdAssetManifest (Read-EdAssetManifest $sandbox) $sandbox -CheckFiles
    $uiEntry=$afterUi.Entries['ui.youzhou.pipeline_fixture']
    Assert-AssetCheck ($afterUi.Ready -eq $beforeUi.Ready+1 -and $afterUi.Missing -eq $beforeUi.Missing -and $uiEntry.resourcePath -ceq 'YouzhouUI/pipeline_fixture') 'UI skin has its own canonical namespace and directory'
    Assert-AssetCheck ($uiEntry.width -eq 191 -and $uiEntry.height -eq 29 -and $uiEntry.hasAlpha -and $uiEntry.hasTransparency -and $uiEntry.sha256 -ceq (Get-FileHash -LiteralPath $uiInput -Algorithm SHA256).Hash.ToLowerInvariant()) 'UI skin accepts wide transparent canvas and preserves exact source bytes'
    $before=Asset-State;Invoke-AssetImport $uiArguments | Out-Null
    Assert-AssetCheck ((Asset-State) -ceq $before) 'UI import is byte-for-byte idempotent'
    Expect-AssetFailure 'UI sprite with map-reference ID namespace' { Invoke-AssetImport @{Id='reference.youzhou.bad_ui';Kind='ui-sprite';SourcePath=$uiInput} }
    Expect-AssetFailure 'UI ID cannot masquerade as a map-reference' { Invoke-AssetImport @{Id='ui.youzhou.bad_reference';Kind='map-reference';SourcePath=$uiInput} }
    $uiOpaqueInput=Join-Path $sandbox 'Inputs/ui_opaque_fixture.png'
    $uiOpaqueBitmap=[Drawing.Bitmap]::new(49,17,[Drawing.Imaging.PixelFormat]::Format24bppRgb)
    try {
        $uiGraphics=[Drawing.Graphics]::FromImage($uiOpaqueBitmap)
        try {$uiGraphics.Clear([Drawing.Color]::White)} finally {$uiGraphics.Dispose()}
        $uiOpaqueBitmap.Save($uiOpaqueInput,[Drawing.Imaging.ImageFormat]::Png)
    } finally {$uiOpaqueBitmap.Dispose()}
    Invoke-AssetImport @{Id='ui.youzhou.opaque';Kind='ui-sprite';SourcePath=$uiOpaqueInput;SourceKind='licensed-local';SourceReference='isolated opaque UI fixture, not production art';Rights='Project test fixture only'} | Out-Null
    $opaqueUi=(Test-EdAssetManifest (Read-EdAssetManifest $sandbox) $sandbox -CheckFiles).Entries['ui.youzhou.opaque']
    Assert-AssetCheck (!$opaqueUi.hasAlpha -and !$opaqueUi.hasTransparency -and $opaqueUi.width -eq 49 -and $opaqueUi.height -eq 17 -and $opaqueUi.sha256 -ceq (Get-FileHash -LiteralPath $uiOpaqueInput -Algorithm SHA256).Hash.ToLowerInvariant()) 'UI accepts opaque rectangular panels without altering dimensions or source bytes'
    $uiUntracked=Join-Path $resourceRoot 'YouzhouUI/untracked.png'
    Copy-Item -LiteralPath $uiInput -Destination $uiUntracked
    try { Expect-AssetFailure 'unregistered UI PNG even with Replace' { Invoke-AssetImport @{Id='ui.youzhou.untracked';Kind='ui-sprite';SourcePath=$uiInput;Replace=$true} } }
    finally {Remove-Item -LiteralPath $uiUntracked}
    $uiCollisionDir=Join-Path $sandbox 'Assets/Other/Resources/YouzhouUI'
    [IO.Directory]::CreateDirectory($uiCollisionDir)|Out-Null
    $uiCollision=Join-Path $uiCollisionDir 'pipeline_fixture.png'
    Copy-Item -LiteralPath $uiInput -Destination $uiCollision
    try { Expect-AssetFailure 'duplicate UI global Resources key' { Test-EdAssetManifest (Read-EdAssetManifest $sandbox) $sandbox -CheckFiles | Out-Null } }
    finally {Remove-Item -LiteralPath $uiCollision}
    if (!('EasternDynasty.AssetCatalogRules' -as [type])) {Add-Type -Path (Join-Path $ProjectRoot 'Assets/EasternDynasty/Scripts/Infrastructure/Assets/AssetCatalog.cs')}
    Assert-AssetCheck ([EasternDynasty.AssetCatalogRules]::ExpectedResourcePath('ui.youzhou.pipeline_fixture','ui-sprite') -ceq 'YouzhouUI/pipeline_fixture') 'C# catalog matches PowerShell UI namespace'
    Assert-AssetCheck ($null -eq [EasternDynasty.AssetCatalogRules]::ExpectedResourcePath('reference.youzhou.pipeline_fixture','ui-sprite') -and $null -eq [EasternDynasty.AssetCatalogRules]::ExpectedResourcePath('ui.youzhou.pipeline_fixture','map-reference')) 'C# catalog rejects cross-kind UI namespaces'
    $csharpUi=[EasternDynasty.AssetRecord]::new()
    foreach($property in $uiEntry.PSObject.Properties){if($property.Name -ne 'source'){$csharpUi.($property.Name)=$property.Value}}
    $csharpUi.source=[EasternDynasty.AssetSource]::new()
    foreach($property in $uiEntry.source.PSObject.Properties){$csharpUi.source.($property.Name)=$property.Value}
    $csharpManifest=[EasternDynasty.AssetManifest]::new();$csharpManifest.schemaVersion=1;$csharpManifest.entries=@($csharpUi)
    Assert-AssetCheck ([EasternDynasty.AssetCatalogRules]::Validate($csharpManifest).Count -eq 1) 'C# catalog accepts the same transparent wide UI manifest'
    $csharpUi.hasTransparency=$false;$csharpUi.hasAlpha=$false
    Assert-AssetCheck ([EasternDynasty.AssetCatalogRules]::Validate($csharpManifest).Count -eq 1) 'C# catalog accepts opaque UI panels without a transparency requirement'
    $nativePortrait=$baselineManifest.entries|Where-Object {$_.kind -ceq 'portrait' -and $_.status -ceq 'ready' -and $_.width -eq 240 -and $_.height -eq 340}|Select-Object -First 1
    Assert-AssetCheck ($null -ne $nativePortrait) 'native 240x340 RAG portrait is present for aspect-preservation regression'
    $nativeInput=Get-EdAssetPath $sandbox $nativePortrait.resourcePath
    Invoke-AssetImport @{Id='portrait.pipeline_native_dimensions';SourcePath=$nativeInput} | Out-Null
    $nativeTarget=Join-Path $resourceRoot 'Portraits/pipeline_native_dimensions.png'
    $nativeInfo=Read-EdValidatedPng $nativeTarget
    Assert-AssetCheck ($nativeInfo.Width -eq 240 -and $nativeInfo.Height -eq 340) 'native 240x340 portrait is accepted without resizing or cropping'
    Assert-AssetCheck ($nativeInfo.Sha256 -ceq (Get-FileHash -LiteralPath $nativeInput -Algorithm SHA256).Hash.ToLowerInvariant()) 'native-aspect portrait preserves all original PNG bytes'
    # Warm a real path, mutate its bytes without changing size/mtime, then demand revalidation.
    Read-EdValidatedPng $referenceTarget | Out-Null
    $cachedBytes=[IO.File]::ReadAllBytes($referenceTarget)
    $cachedTime=[IO.File]::GetLastWriteTimeUtc($referenceTarget)
    try {
        $corruptBytes=[byte[]]$cachedBytes.Clone();$corruptBytes[100]=$corruptBytes[100] -bxor 1
        [IO.File]::WriteAllBytes($referenceTarget,$corruptBytes);[IO.File]::SetLastWriteTimeUtc($referenceTarget,$cachedTime)
        Assert-AssetCheck ([IO.File]::GetLastWriteTimeUtc($referenceTarget) -eq $cachedTime -and (Get-Item -LiteralPath $referenceTarget).Length -eq $cachedBytes.Length) 'cache corruption fixture preserves both timestamp and file length'
        $corruptSha=(Get-FileHash -LiteralPath $referenceTarget -Algorithm SHA256).Hash.ToLowerInvariant()
        $expectedSha=$afterReference.Entries['reference.youzhou.pipeline_fixture'].sha256
        Expect-AssetFailure 'cached PNG content changes despite identical timestamp and length' { Test-EdAssetManifest (Read-EdAssetManifest $sandbox) $sandbox -CheckFiles | Out-Null } @('reference.youzhou.pipeline_fixture',$referenceTarget,"expected SHA256=$expectedSha","actual SHA256=$corruptSha",'PNG CRC mismatch:')
    } finally {[IO.File]::WriteAllBytes($referenceTarget,$cachedBytes);[IO.File]::SetLastWriteTimeUtc($referenceTarget,$cachedTime)}
    Test-EdAssetManifest (Read-EdAssetManifest $sandbox) $sandbox -CheckFiles | Out-Null
    Write-Output "ASSET_PIPELINE_CHECKS_OK: $checks assertions; production resource PNGs/metas untouched."
} finally {
    # Only remove this test's resolved GUID directory below the named sandbox parent.
    $full=[IO.Path]::GetFullPath($sandbox);$parent=[IO.Path]::GetFullPath($sandboxParent).TrimEnd('\')+'\'
    if (!$full.StartsWith($parent,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -notmatch '^[a-f0-9]{32}$') { throw 'Unsafe sandbox cleanup path.' }
    Assert-EdAssetNoReparse $full
    if (Test-Path -LiteralPath $full) { Remove-Item -LiteralPath $full -Recurse -Force }
}
