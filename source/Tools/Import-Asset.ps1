[CmdletBinding(DefaultParameterSetName='Import')]
param(
    [Parameter(Mandatory,ParameterSetName='Import')][string]$Id,
    [Parameter(Mandatory,ParameterSetName='Import')][string]$Kind,
    [Parameter(Mandatory,ParameterSetName='Import')][string]$SourcePath,
    [Parameter(Mandatory,ParameterSetName='Import')][string]$Purpose,
    [Parameter(Mandatory,ParameterSetName='Import')][string]$SourceKind,
    [Parameter(Mandatory,ParameterSetName='Import')][string]$SourceReference,
    [Parameter(Mandatory,ParameterSetName='Import')][string]$Rights,
    [Parameter(ParameterSetName='Import')][string]$CreatedUtc='',
    [Parameter(ParameterSetName='Import')][switch]$Replace,
    [Parameter(Mandatory,ParameterSetName='Recover')][switch]$Recover,
    [string]$ProjectRoot=(Split-Path -Parent $PSScriptRoot)
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Validate-Assets.ps1') -ProjectRoot $ProjectRoot -LibraryOnly
$importRoot=Get-EdAssetRoot $ProjectRoot
$inbox=Join-Path $importRoot 'AssetInbox'
$transaction=Join-Path $inbox '.asset-transaction'
$manifestPath=Join-Path $importRoot 'Assets/EasternDynasty/Resources/asset-manifest.json'

function Remove-EdAssetTransaction {
    $resolved=[IO.Path]::GetFullPath($transaction)
    if ($resolved -cne [IO.Path]::GetFullPath((Join-Path $importRoot 'AssetInbox/.asset-transaction'))) { throw 'Unsafe transaction cleanup path.' }
    Assert-EdAssetNoReparse $resolved
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}

function Restore-EdAssetTransaction {
    Assert-EdAssetNoReparse $transaction
    foreach ($file in @('journal.json','manifest.before.json','asset.before.png')) { Assert-EdAssetNoReparse (Join-Path $transaction $file) }
    $journalPath=Join-Path $transaction 'journal.json'
    if (!(Test-Path -LiteralPath $journalPath)) { throw 'Unfinished staging has no journal. Inspect AssetInbox/.asset-transaction before retrying.' }
    $journal=Get-Content -LiteralPath $journalPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($journal.existed -isnot [bool] -or $journal.committed -isnot [bool]) { throw 'Invalid asset transaction journal flags.' }
    $destination=Get-EdAssetPath $importRoot (Get-EdAssetResourcePath $journal.id $journal.kind)
    if ($journal.committed) { Remove-EdAssetTransaction; return }
    $oldManifest=Join-Path $transaction 'manifest.before.json'
    if (!(Test-Path -LiteralPath $oldManifest)) { throw 'Recovery backup manifest missing; no files changed by recovery.' }
    if ($journal.existed -and !(Test-Path -LiteralPath (Join-Path $transaction 'asset.before.png'))) { throw 'Recovery PNG backup missing; no files changed by recovery.' }
    $before=Get-Content -LiteralPath $oldManifest -Raw -Encoding UTF8 | ConvertFrom-Json
    $beforeValidation=Test-EdAssetManifest $before $importRoot -AllowTransaction
    $beforeEntry=$beforeValidation.Entries[$journal.id]
    if ($journal.existed -and ($null -eq $beforeEntry -or $beforeEntry.status -ne 'ready' -or (Get-FileHash -LiteralPath (Join-Path $transaction 'asset.before.png') -Algorithm SHA256).Hash.ToLowerInvariant() -cne $beforeEntry.sha256)) { throw 'Recovery PNG backup does not match the previous manifest.' }
    if (Test-Path -LiteralPath $destination) {
        $currentHash=(Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($currentHash -cne $journal.assetAfterSha256 -and ($null -eq $beforeEntry -or $currentHash -cne $beforeEntry.sha256)) { throw 'Asset changed outside the interrupted transaction; refusing to overwrite it during recovery.' }
    }
    $manifestHash=(Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($manifestHash -cne (Get-FileHash -LiteralPath $oldManifest -Algorithm SHA256).Hash.ToLowerInvariant() -and $manifestHash -cne $journal.manifestAfterSha256) { throw 'Manifest changed outside the interrupted transaction; recovery requires inspection.' }
    # Never touches .meta. Keep recovery files if an external lock prevents restoration.
    if ($journal.existed) { [IO.File]::Copy((Join-Path $transaction 'asset.before.png'),$destination,$true) }
    elseif (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination -Force }
    # Skip unchanged manifest: it can be read-locked by the failure that triggered rollback.
    if ((Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $oldManifest -Algorithm SHA256).Hash) { [IO.File]::Copy($oldManifest,$manifestPath,$true) }
    Remove-EdAssetTransaction
}

# Refuse to expose a multi-file commit to a live Unity AssetDatabase refresh.
$unityLock=Join-Path $importRoot 'Temp/UnityLockfile'
if (Test-Path -LiteralPath $unityLock) {
    try { $probe=[IO.File]::Open($unityLock,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$probe.Dispose() }
    catch { throw 'Close this project in Unity before importing local assets (the Editor holds UnityLockfile).' }
}
Assert-EdAssetNoReparse $inbox
[IO.Directory]::CreateDirectory($inbox) | Out-Null
$mutex=$null
try {
    Assert-EdAssetNoReparse (Join-Path $inbox '.asset-import.lock')
    $mutex=[IO.File]::Open((Join-Path $inbox '.asset-import.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    if (Test-Path -LiteralPath $transaction) { Restore-EdAssetTransaction }
    if ($Recover) { Write-Output 'PASS: no unfinished asset transaction remains.'; return }
    $resourcePath=Get-EdAssetResourcePath $Id $Kind
    $destination=Get-EdAssetPath $importRoot $resourcePath
    if ($SourcePath -match '^\\\\|^[a-z]+://|(^|[\\/])\.\.([\\/]|$)') { throw 'SourcePath must be a local PNG path without traversal or a network URL.' }
    if (![IO.Path]::IsPathRooted($SourcePath)) { $SourcePath=Join-Path (Get-Location).Path $SourcePath }
    $sourceFull=[IO.Path]::GetFullPath($SourcePath);Assert-EdAssetNoReparse $sourceFull
    if ([IO.Path]::GetExtension($sourceFull) -ine '.png' -or !(Test-Path -LiteralPath $sourceFull -PathType Leaf)) { throw 'SourcePath must name an existing local .png file.' }
    Initialize-EdPngInspector
    $info=[EdAssetPngInspector]::Read($sourceFull)
    $manifest=Read-EdAssetManifest $importRoot
    $validation=Test-EdAssetManifest $manifest $importRoot -CheckFiles
    $old=$validation.Entries[$Id]
    if ((Test-Path -LiteralPath $destination) -and ($null -eq $old -or $old.status -ne 'ready')) { throw 'Destination belongs to an unregistered asset. It will not be overwritten, even with -Replace.' }
    if ((Test-Path -LiteralPath ($destination+'.meta')) -and !(Test-Path -LiteralPath $destination)) { throw 'Orphaned .meta at destination; investigate its ownership before import.' }
    $entry=[pscustomobject][ordered]@{
        id=$Id;kind=$Kind;status='ready';required=($null -ne $old -and $old.required);resourcePath=$resourcePath;purpose=$Purpose
        source=[pscustomobject][ordered]@{kind=$SourceKind;reference=$SourceReference;rights=$Rights;createdUtc=$CreatedUtc}
        width=$info.Width;height=$info.Height;hasAlpha=$info.HasAlpha;hasTransparency=$info.HasTransparency;sha256=$info.Sha256
        importedUtc=[DateTime]::UtcNow.ToString('o');placeholderPolicy='none'
    }
    $same=$null -ne $old -and $old.status -eq 'ready' -and $old.sha256 -ceq $entry.sha256
    $sameMetadata=$same -and $old.purpose -ceq $Purpose -and $old.source.kind -ceq $SourceKind -and $old.source.reference -ceq $SourceReference -and $old.source.rights -ceq $Rights -and $old.source.createdUtc -ceq $CreatedUtc
    if ($sameMetadata) { Write-Output "UNCHANGED $Id (same PNG and metadata; original .meta and manifest bytes preserved).";return }
    if ($null -ne $old -and $old.status -eq 'ready' -and !$Replace) { throw "ID '$Id' already has registered content. Use -Replace to intentionally change it." }
    $newEntries=@($manifest.entries | Where-Object { $_.id -cne $Id })+@($entry)
    $candidate=[pscustomobject][ordered]@{schemaVersion=1;entries=@($newEntries | Sort-Object id)}
    Test-EdAssetManifest $candidate $importRoot | Out-Null
    [IO.Directory]::CreateDirectory($transaction) | Out-Null
    # Prepare every byte before changing Assets. Stage and destination are on the same volume.
    try {
        [IO.File]::Copy($manifestPath,(Join-Path $transaction 'manifest.before.json'))
        [IO.File]::Copy($sourceFull,(Join-Path $transaction 'asset.after.png'))
        if ([EdAssetPngInspector]::Read((Join-Path $transaction 'asset.after.png')).Sha256 -cne $info.Sha256) { throw 'Source changed while it was being staged.' }
        $existed=Test-Path -LiteralPath $destination
        if ($existed) { [IO.File]::Copy($destination,(Join-Path $transaction 'asset.before.png')) }
        [IO.File]::WriteAllText((Join-Path $transaction 'manifest.after.json'),($candidate | ConvertTo-Json -Depth 12)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
        $journal=[pscustomobject]@{id=$Id;kind=$Kind;existed=[bool]$existed;committed=$false;assetAfterSha256=$info.Sha256;manifestAfterSha256=(Get-FileHash -LiteralPath (Join-Path $transaction 'manifest.after.json') -Algorithm SHA256).Hash.ToLowerInvariant()}
        [IO.File]::WriteAllText((Join-Path $transaction 'journal.json'),($journal | ConvertTo-Json),[Text.UTF8Encoding]::new($false))
    } catch { Remove-EdAssetTransaction; throw }
    try {
        [IO.Directory]::CreateDirectory((Split-Path -Parent $destination)) | Out-Null
        if ($existed) { [IO.File]::Replace((Join-Path $transaction 'asset.after.png'),$destination,(Join-Path $transaction 'asset.swap-backup.png')) }
        else { [IO.File]::Move((Join-Path $transaction 'asset.after.png'),$destination) }
        [IO.File]::Replace((Join-Path $transaction 'manifest.after.json'),$manifestPath,(Join-Path $transaction 'manifest.swap-backup.json'))
        Test-EdAssetManifest (Read-EdAssetManifest $importRoot) $importRoot -CheckFiles -AllowTransaction | Out-Null
        $journal.committed=$true
        [IO.File]::WriteAllText((Join-Path $transaction 'journal.json'),($journal | ConvertTo-Json),[Text.UTF8Encoding]::new($false))
    } catch {
        $originalError=$_
        try { Restore-EdAssetTransaction } catch { throw "Import failed: $originalError. Rollback is blocked: $_. Recovery backups remain; close the lock holder and run Import-Asset -Recover. Build validation will reject this project until recovered." }
        throw $originalError
    }
    try { Remove-EdAssetTransaction } catch { Write-Warning "Import committed; transaction cleanup pending. Run Import-Asset -Recover. $_" }
    Write-Output "IMPORTED $Id -> $resourcePath.png; SHA256=$($info.Sha256). Existing .meta untouched. Unity applies the catalog import profile on its next import."
} finally { if ($null -ne $mutex) { $mutex.Dispose() } }
