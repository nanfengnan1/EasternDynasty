param(
    [string]$UnityEditor,
    [string]$DestinationPath,
    [ValidatePattern('^[0-9A-Za-z][0-9A-Za-z._-]{0,63}$')][string]$VersionName='0.2.0',
    [ValidateRange(0,2147483647)][int]$VersionCode=0,
    [switch]$CheckEnvironmentOnly,
    [switch]$PrepareWorkspaceOnly,
    [switch]$ReplacePrevious,
    [switch]$CleanWorkspaceOnSuccess
)
$ErrorActionPreference='Stop'
function Assert-AndroidSourceSnapshot([string]$Root,[object[]]$Snapshot) {
    $edExpected=@{}
    foreach($edEntry in $Snapshot){
        if($edExpected.ContainsKey($edEntry.path)){throw ('Duplicate snapshot path: '+$edEntry.path)}
        $edExpected[$edEntry.path]=$edEntry.sha256
    }
    $edSeen=0
    foreach($edFolder in @('Assets','Packages','ProjectSettings')) {
        $edFolderPath=Join-Path $Root $edFolder
        if((Get-Item -LiteralPath $edFolderPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Snapshot folder is a reparse point: '+$edFolder)}
        foreach($edItem in Get-ChildItem -LiteralPath $edFolderPath -Recurse -Force){
            if($edItem.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Snapshot contains a reparse point: '+$edItem.FullName)}
            if($edItem.PSIsContainer){continue}
            $edRelative=$edItem.FullName.Substring($Root.TrimEnd('\').Length+1)
            if(-not $edExpected.ContainsKey($edRelative)){throw ('Source file was added after snapshot: '+$edRelative)}
            if((Get-FileHash -LiteralPath $edItem.FullName -Algorithm SHA256).Hash -ne $edExpected[$edRelative]){throw ('Source file changed after snapshot: '+$edRelative)}
            $edSeen++
        }
    }
    if($edSeen -ne $edExpected.Count){throw ('Source file set changed after snapshot: expected '+$edExpected.Count+', found '+$edSeen)}
}
function Remove-AndroidCurrentWorkspace([string]$Workspace,[string]$WorkspaceBase,[string]$RunId,[string]$SourceRoot,[string]$Evidence,[string]$ReceiptPath) {
    $edState=[ordered]@{status='retained';workspace=$Workspace;reason='validation-not-complete';evidence=$Evidence}
    $edRemovalStarted=$false
    try {
        if(-not [IO.Path]::IsPathRooted($Workspace) -or -not [IO.Path]::IsPathRooted($WorkspaceBase) -or $RunId -notmatch '^[0-9]{8}-[0-9]{6}-[0-9a-f]{8}$'){throw 'Workspace path/run is invalid.'}
        $edExact=[IO.Path]::GetFullPath($Workspace).TrimEnd('\')
        $edBase=[IO.Path]::GetFullPath($WorkspaceBase).TrimEnd('\')
        $edSource=[IO.Path]::GetFullPath($SourceRoot).TrimEnd('\')
        $edArchive=[IO.Path]::GetFullPath($Evidence).TrimEnd('\')
        if($edExact -ne [IO.Path]::GetFullPath((Join-Path $edBase $RunId)) -or [IO.Path]::GetDirectoryName($edExact) -ne $edBase -or $edExact -eq $edSource -or $edSource.StartsWith($edExact+'\',[StringComparison]::OrdinalIgnoreCase) -or $edArchive.StartsWith($edExact+'\',[StringComparison]::OrdinalIgnoreCase) -or $edArchive -eq $edExact){throw 'Workspace is not the exact isolated current-run directory.'}
        $edAncestor=$edExact
        while($edAncestor){
            if((Get-Item -LiteralPath $edAncestor -Force -ErrorAction Stop).Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Workspace ancestry contains a reparse point: '+$edAncestor)}
            $edAncestor=Split-Path -Parent $edAncestor
        }
        $edMarkerPath=Join-Path $edExact '.eastern-dynasty-android-workspace.json'
        $edMarker=Get-Content -LiteralPath $edMarkerPath -Raw -Encoding UTF8|ConvertFrom-Json
        if($edMarker.run -ne $RunId -or $edMarker.purpose -ne 'isolated-ascii-android-build' -or -not [IO.Path]::IsPathRooted([string]$edMarker.source) -or [IO.Path]::GetFullPath([string]$edMarker.source).TrimEnd('\') -ne $edSource){throw 'Workspace ownership marker does not match this source and run.'}
        $edDelivery=Get-Content -LiteralPath $ReceiptPath -Raw -Encoding UTF8|ConvertFrom-Json
        if($edDelivery.status -ne 'built-and-package-verified' -or $edDelivery.androidWorkspace -ne (Join-Path $edExact 'Project') -or $edDelivery.evidence -ne $Evidence -or $edDelivery.sha256 -notmatch '^[a-fA-F0-9]{64}$' -or [IO.Path]::GetFullPath([string]$edDelivery.apk).StartsWith($edExact+'\',[StringComparison]::OrdinalIgnoreCase) -or (Get-FileHash -LiteralPath $edDelivery.apk -Algorithm SHA256).Hash -ne $edDelivery.sha256){throw 'Verified delivery receipt/APK is absent or inconsistent.'}
        $edProject=Join-Path $edExact 'Project'
        if(@(Get-CimInstance Win32_Process -ErrorAction Stop|Where-Object {$_.Name -eq 'Unity.exe' -and $_.CommandLine -and (Test-EdProjectCommandLine $_.CommandLine $edProject)}).Count){throw 'The workspace is still open in Unity.'}
        # Traverse one directory level at a time; reject links before following
        # them. Never enumerate, delete or infer ownership of sibling runs.
        $edPending=[Collections.Generic.Queue[string]]::new()
        $edPending.Enqueue($edExact)
        while($edPending.Count){
            foreach($edItem in Get-ChildItem -LiteralPath $edPending.Dequeue() -Force -ErrorAction Stop){
                if($edItem.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Workspace contains a reparse point: '+$edItem.FullName)}
                if($edItem.PSIsContainer){$edPending.Enqueue($edItem.FullName)}else{
                    $edProbe=[IO.File]::Open($edItem.FullName,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::None)
                    $edProbe.Dispose()
                }
            }
        }
        # Persist original build evidence outside the workspace before removal.
        foreach($edPair in @(
            @('Builds\EasternDynasty-S1.apk.build.json','android-build.json'),
            @('Logs\unity-android.log','unity-android.log'),
            @('source-snapshot.json','source-snapshot.json'),
            @('.eastern-dynasty-android-workspace.json','workspace-marker.json')
        )){
            $edFrom=Join-Path $edExact $edPair[0]
            $edTo=Join-Path $edArchive $edPair[1]
            Copy-Item -LiteralPath $edFrom -Destination $edTo -Force -ErrorAction Stop
            if((Get-FileHash -LiteralPath $edFrom -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $edTo -Algorithm SHA256).Hash){throw 'Archived workspace evidence hash differs.'}
        }
        if(@(Get-CimInstance Win32_Process -ErrorAction Stop|Where-Object {$_.Name -eq 'Unity.exe' -and $_.CommandLine -and (Test-EdProjectCommandLine $_.CommandLine $edProject)}).Count){throw 'Unity reopened the workspace; cleanup skipped.'}
        $edRemovalStarted=$true
        Remove-Item -LiteralPath $edExact -Recurse -Force -ErrorAction Stop
        if(Test-Path -LiteralPath $edExact){throw 'Workspace directory remains after cleanup.'}
        $edState.status='removed'
        $edState.reason='successful-current-run-workspace-removed'
    } catch {
        $edState.status=if($edRemovalStarted){'cleanup-incomplete'}else{'retained'}
        $edState.reason=$_.Exception.Message
    }
    return $edState
}
function Remove-AndroidPreviousArtifact([object]$Previous,[string]$Destination,[string]$NewApk) {
    $edCleanup=[ordered]@{status='kept';apk=$null;reason='no-valid-previous-receipt';sidecar='not-attempted'}
    try {
        if(-not $Previous -or $Previous.status -ne 'built-and-package-verified' -or $Previous.applicationId -ne 'com.easterndynasty.s1' -or $Previous.sha256 -notmatch '^[0-9a-fA-F]{64}$' -or -not [IO.Path]::IsPathRooted([string]$Previous.apk)){return $edCleanup}
        $edOld=[IO.Path]::GetFullPath([string]$Previous.apk)
        $edAllowed=[IO.Path]::GetFullPath($Destination).TrimEnd('\')
        $edName=[IO.Path]::GetFileName($edOld)
        $edCleanup.apk=$edOld
        if([IO.Path]::GetDirectoryName($edOld).TrimEnd('\') -ne $edAllowed -or $edName -notmatch '^EasternDynasty-S1-[0-9]+\.apk$' -or $edName -ne ('EasternDynasty-S1-'+$Previous.versionCode+'.apk') -or $edOld -eq [IO.Path]::GetFullPath($NewApk)){$edCleanup.reason='outside-allowed-previous-artifact';return $edCleanup}
        # Resolve ancestry before any deletion. No directory enumeration or
        # recursive cleanup: only the one exact receipt path is eligible.
        $edParent=$edAllowed
        while($edParent){
            if((Get-Item -LiteralPath $edParent -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){$edCleanup.reason='reparse-point-ancestor';return $edCleanup}
            $edParent=Split-Path -Parent $edParent
        }
        if(-not ('EasternDynasty.AndroidArtifactCleanup' -as [type])) {
            Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;
namespace EasternDynasty {
    public static class AndroidArtifactCleanup {
        [StructLayout(LayoutKind.Sequential)] struct FileAttributeTag { public uint Attributes; public uint ReparseTag; }
        [StructLayout(LayoutKind.Sequential)] struct FileDisposition { public byte DeleteFile; }
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
        static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError=true)]
        static extern bool GetFileInformationByHandleEx(SafeFileHandle handle, int kind, out FileAttributeTag info, uint size);
        [DllImport("kernel32.dll", SetLastError=true)]
        static extern bool SetFileInformationByHandle(SafeFileHandle handle, int kind, ref FileDisposition info, uint size);
        public static string DeleteMatching(string path, string hash, string exactText) {
            // Open existing with READ | DELETE and FileShare.None. Validate and
            // mark deletion on that same locked handle, avoiding a check/delete
            // race and preserving files currently opened by another process.
            using(var handle=CreateFile(path,0x80010000,0,IntPtr.Zero,3,0x08200000,IntPtr.Zero)) {
                if(handle.IsInvalid) return "kept-unavailable-win32-"+Marshal.GetLastWin32Error();
                FileAttributeTag attributes;
                if(!GetFileInformationByHandleEx(handle,9,out attributes,8) || (attributes.Attributes & 0x410)!=0) return "kept-nonregular-file";
                using(var stream=new FileStream(handle,FileAccess.Read)) {
                    if(!String.IsNullOrEmpty(exactText)) {
                        if(stream.Length>4096) return "kept-sidecar-mismatch";
                        using(var reader=new StreamReader(stream,new UTF8Encoding(false,true),true,4096,true)) {
                            if(!String.Equals(reader.ReadToEnd().TrimEnd('\r','\n'),exactText,StringComparison.OrdinalIgnoreCase)) return "kept-sidecar-mismatch";
                        }
                    } else using(var sha=SHA256.Create()) {
                        if(!String.Equals(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-",""),hash,StringComparison.OrdinalIgnoreCase)) return "kept-hash-mismatch";
                    }
                    var disposition=new FileDisposition { DeleteFile=1 };
                    if(!SetFileInformationByHandle(handle,4,ref disposition,1)) return "kept-delete-failed-win32-"+Marshal.GetLastWin32Error();
                }
            }
            return "deleted";
        }
    }
}
'@
        }
        $edCleanup.reason=[EasternDynasty.AndroidArtifactCleanup]::DeleteMatching($edOld,[string]$Previous.sha256,$null)
        if($edCleanup.reason -eq 'deleted') {
            $edCleanup.status='removed-previous'
            $edCleanup.sidecar=[EasternDynasty.AndroidArtifactCleanup]::DeleteMatching(($edOld+'.sha256'),$null,([string]$Previous.sha256+'  '+$edName))
        }
    } catch {$edCleanup.reason='kept-cleanup-error: '+$_.Exception.Message}
    return $edCleanup
}
. (Join-Path $PSScriptRoot 'Common.ps1')
$edRoot=Get-EdProjectRoot
$edEditor=Resolve-EdUnityEditor $UnityEditor
$edAndroid=Join-Path (Split-Path -Parent $edEditor) 'Data\PlaybackEngines\AndroidPlayer'
$edMissing=[Collections.Generic.List[string]]::new()
foreach($edDependency in @('OpenJDK\bin\java.exe','OpenJDK\release','NDK\source.properties','SDK\platform-tools\adb.exe')) {
    if(-not (Test-Path -LiteralPath (Join-Path $edAndroid $edDependency) -PathType Leaf)){$edMissing.Add($edDependency)}
}
$edSdkBuildTools=@(Get-ChildItem -LiteralPath (Join-Path $edAndroid 'SDK\build-tools') -Directory -ErrorAction SilentlyContinue | Where-Object {
    (Test-Path -LiteralPath (Join-Path $_.FullName 'aapt.exe')) -and (Test-Path -LiteralPath (Join-Path $_.FullName 'zipalign.exe')) -and (Test-Path -LiteralPath (Join-Path $_.FullName 'lib\apksigner.jar'))
} | Sort-Object {[version]($_.Name -replace '-.*$','')} -Descending | Select-Object -First 1)
if($edSdkBuildTools.Count -eq 0){$edMissing.Add('SDK build-tools (aapt.exe, zipalign.exe and lib/apksigner.jar)')}
if(-not @(Get-ChildItem -LiteralPath (Join-Path $edAndroid 'SDK\platforms') -Filter 'android-*' -Directory -ErrorAction SilentlyContinue | Where-Object {Test-Path -LiteralPath (Join-Path $_.FullName 'android.jar')}).Count){$edMissing.Add('SDK platforms/android-*/android.jar')}
if($edMissing.Count){throw ('Android modules are incomplete for '+(Get-EdUnityVersion)+': '+($edMissing -join ', ')+'. In Unity Hub add Android Build Support with SDK/NDK tools and OpenJDK for this Editor.')}
$edNdk=Get-Content -LiteralPath (Join-Path $edAndroid 'NDK\source.properties') -Raw -Encoding UTF8
$edJavaRelease=Get-Content -LiteralPath (Join-Path $edAndroid 'OpenJDK\release') -Raw -Encoding UTF8
if($edNdk -notmatch 'Pkg\.Revision\s*=\s*27\.2\.12479018') {throw 'Unity6000.3 requires the supported NDK r27c (27.2.12479018). Repair its Hub NDK module.'}
if($edJavaRelease -notmatch 'JAVA_VERSION="17[.\"]'){throw 'Unity6000.3 requires OpenJDK17. Repair its Hub OpenJDK module.'}
$edSettingsFile=Join-Path $edRoot 'ProjectSettings\ProjectSettings.asset'
if((Get-Content -LiteralPath $edSettingsFile -Raw -Encoding UTF8) -notmatch '(?m)^\s*activeInputHandler:\s*0\s*$'){throw 'The mobile input implementation requires Input Manager (Old), activeInputHandler=0.'}
if($CheckEnvironmentOnly){[pscustomobject]@{unity=$edEditor;android=$edAndroid;buildTools=$edSdkBuildTools[0].FullName;ndk='27.2.12479018';jdk=17;minimumAndroid='7.1 / API25';status='ready'};return}
Assert-EdEditorAvailable $edEditor
if(-not $DestinationPath){$DestinationPath=Join-Path (Split-Path -Parent $edRoot) 'apk'}
$edDestination=[IO.Path]::GetFullPath($DestinationPath)
if($edDestination -eq [IO.Path]::GetPathRoot($edDestination) -or $edDestination.TrimEnd('\') -eq $edRoot.TrimEnd('\') -or $edDestination.StartsWith((Join-Path $edRoot 'Assets')+'\',[StringComparison]::OrdinalIgnoreCase)) {throw 'Choose a dedicated APK output directory outside Assets and the project root.'}
$edLatest=Join-Path $edDestination 'build-info.json'
$edPrevious=if(Test-Path -LiteralPath $edLatest){Get-Content -LiteralPath $edLatest -Raw -Encoding UTF8|ConvertFrom-Json}else{$null}
if($VersionCode -eq 0){
    $VersionCode=[int]([DateTime]::UtcNow.ToString('yyyyMMddHH'))
    if($edPrevious -and $edPrevious.applicationId -eq 'com.easterndynasty.s1' -and [long]$edPrevious.versionCode -ge $VersionCode){$VersionCode=[int]([long]$edPrevious.versionCode+1)}
}
$edRunId=[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8)
$edEvidence=Join-Path $edRoot ('Validation\AndroidBuild\'+$edRunId)
$edWorkspaceBase=[IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetPathRoot($edRoot)) 'EasternDynastyBuild\Android'))
if($edWorkspaceBase -match '[^\x00-\x7F]' -or $edWorkspaceBase.StartsWith($edRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Android workspace must use an independent ASCII path.'}
# Every attempt has a fresh owned directory. Never mirror/delete a previous
# workspace or copy the original Library; all .meta files are preserved.
$edWorkspace=[IO.Path]::GetFullPath((Join-Path $edWorkspaceBase $edRunId))
if(-not $edWorkspace.StartsWith($edWorkspaceBase+'\',[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $edWorkspace)){throw 'Android workspace is not a new bounded directory.'}
$edAncestor=$edWorkspaceBase
while($edAncestor){
    if(Test-Path -LiteralPath $edAncestor){if((Get-Item -LiteralPath $edAncestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Workspace ancestor is a reparse point: '+$edAncestor)}}
    $edAncestor=Split-Path -Parent $edAncestor
}
New-Item -ItemType Directory -Path $edWorkspace|Out-Null
$edUnityProject=Join-Path $edWorkspace 'Project'
$edBuildDirectory=Join-Path $edWorkspace 'Builds'
$edUnityLogs=Join-Path $edWorkspace 'Logs'
foreach($edDirectory in @($edEvidence,$edUnityProject,$edBuildDirectory,$edUnityLogs)){New-Item -ItemType Directory -Path $edDirectory -Force|Out-Null}
[IO.File]::WriteAllText((Join-Path $edWorkspace '.eastern-dynasty-android-workspace.json'),(@{source=$edRoot;run=$edRunId;createdUtc=[DateTime]::UtcNow.ToString('o');purpose='isolated-ascii-android-build'}|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
$edSnapshot=@()
foreach($edFolder in @('Assets','Packages','ProjectSettings')) {
    $edSourceFolder=Join-Path $edRoot $edFolder
    $edSourceItems=@(Get-ChildItem -LiteralPath $edSourceFolder -Recurse -Force)
    if((Get-Item -LiteralPath $edSourceFolder -Force).Attributes -band [IO.FileAttributes]::ReparsePoint -or @($edSourceItems|Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count){throw ('Source snapshot cannot contain reparse points: '+$edFolder)}
    Copy-Item -LiteralPath $edSourceFolder -Destination $edUnityProject -Recurse
    foreach($edFile in $edSourceItems|Where-Object {-not $_.PSIsContainer}) {
        $edRelative=$edFile.FullName.Substring($edRoot.Length+1)
        $edCopy=Join-Path $edUnityProject $edRelative
        $edFileHash=(Get-FileHash -LiteralPath $edFile.FullName -Algorithm SHA256).Hash
        if((Get-FileHash -LiteralPath $edCopy -Algorithm SHA256).Hash -ne $edFileHash){throw ('Snapshot differs from source: '+$edRelative)}
        $edSnapshot+=@{path=$edRelative;sha256=$edFileHash;bytes=$edFile.Length}
    }
}
$edSnapshotText=@{source=$edRoot;workspace=$edUnityProject;files=$edSnapshot;createdUtc=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $edEvidence 'source-snapshot.json'),$edSnapshotText,[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $edWorkspace 'source-snapshot.json'),$edSnapshotText,[Text.UTF8Encoding]::new($false))
Assert-AndroidSourceSnapshot $edRoot $edSnapshot
Assert-AndroidSourceSnapshot $edUnityProject $edSnapshot
if($PrepareWorkspaceOnly){[pscustomobject]@{status='snapshot-verified';project=$edUnityProject;workspace=$edWorkspace;files=$edSnapshot.Count;evidence=$edEvidence};return}
$edCandidate=Join-Path $edBuildDirectory 'EasternDynasty-S1.apk'
$edOutputName='EasternDynasty-S1-'+$VersionCode+'.apk'
$edOutput=Join-Path $edDestination $edOutputName
if(Test-Path -LiteralPath $edOutput){throw ('This version already exists: '+$edOutput+'. Choose a higher version code; an old APK is never reused as a new result.')}
$edBeforeHash=(Get-FileHash -LiteralPath $edSettingsFile -Algorithm SHA256).Hash
$edBuildLog=Join-Path $edUnityLogs 'unity-android.log'
$edFailure=$null
function Invoke-AndroidEditor([string[]]$Arguments,[string]$LogPath,[string]$Marker) {
    Assert-EdEditorAvailable $edEditor
    $edWorkspaceEditors=@(Get-CimInstance Win32_Process -ErrorAction Stop | Where-Object {$_.Name -eq 'Unity.exe' -and $_.CommandLine -and (Test-EdProjectCommandLine $_.CommandLine $edUnityProject)})
    if($edWorkspaceEditors.Count){throw 'Android snapshot is already open in Unity.'}
    $edInvocation=ConvertTo-EdProcessArguments (@('-batchmode','-nographics','-quit','-projectPath',$edUnityProject,'-logFile',$LogPath)+$Arguments)
    $edProcess=Start-Process -FilePath $edEditor -ArgumentList $edInvocation -WindowStyle Hidden -PassThru -Wait
    if($edProcess.ExitCode -ne 0){throw ('Unity exited '+$edProcess.ExitCode+'. See '+$LogPath)}
    $edText=Get-Content -LiteralPath $LogPath -Raw -Encoding UTF8
    if(-not $edText.Contains($Marker) -or $edText -match '(?im)^Shader error|^.*error CS[0-9]+:|^Exception:'){throw ('Unity did not complete '+$Marker+'. See '+$LogPath)}
}
function Invoke-AndroidValidationTool([string]$Executable,[string[]]$Arguments,[string]$Name) {
    $edOut=Join-Path $edEvidence ($Name+'.log');$edError=Join-Path $edEvidence ($Name+'-stderr.log')
    $edProcess=Start-Process -FilePath $Executable -ArgumentList (ConvertTo-EdProcessArguments $Arguments) -WindowStyle Hidden -RedirectStandardOutput $edOut -RedirectStandardError $edError -Wait -PassThru
    if($edProcess.ExitCode -ne 0){throw ($Name+' exited '+$edProcess.ExitCode+'. See '+$edOut+' and '+$edError)}
    return (Get-Content -LiteralPath $edOut -Raw -Encoding UTF8)
}
try {
    Invoke-AndroidEditor @('-buildTarget','Android','-executeMethod','EasternDynasty.Editor.AndroidBuildTools.BuildApk','-edAndroidOutput',$edCandidate,'-edAndroidVersionCode',[string]$VersionCode,'-edAndroidVersionName',$VersionName) $edBuildLog 'EASTERN_DYNASTY_ANDROID_BUILD_OK'
    if(-not (Test-Path -LiteralPath $edCandidate -PathType Leaf) -or (Get-Item -LiteralPath $edCandidate).Length -lt 1MB){throw 'New Android APK is absent or implausibly small.'}
    $edBuildReceipt=Get-Content -LiteralPath ($edCandidate+'.build.json') -Raw -Encoding UTF8|ConvertFrom-Json
    if($edBuildReceipt.applicationId -ne 'com.easterndynasty.s1' -or $edBuildReceipt.versionCode -ne $VersionCode -or -not $edBuildReceipt.settingsRestored){throw 'Android build receipt is inconsistent.'}
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $edZip=[IO.Compression.ZipFile]::OpenRead($edCandidate)
    try {
        foreach($edRequired in @('AndroidManifest.xml','classes.dex','lib/arm64-v8a/libunity.so','lib/arm64-v8a/libil2cpp.so','lib/armeabi-v7a/libunity.so','lib/armeabi-v7a/libil2cpp.so')){
            $edEntry=$edZip.GetEntry($edRequired);if(-not $edEntry -or $edEntry.Length -eq 0){throw ('Missing APK component: '+$edRequired)}
        }
        if(-not @($edZip.Entries|Where-Object {$_.FullName -like 'assets/bin/Data/*' -and $_.Length -gt 0}).Count){throw 'Unity player data missing from APK.'}
    } finally {$edZip.Dispose()}
    $edJava=Join-Path $edAndroid 'OpenJDK\bin\java.exe'
    $edSigner=Join-Path $edSdkBuildTools[0].FullName 'lib\apksigner.jar'
    $edSignature=Invoke-AndroidValidationTool $edJava @('-jar',$edSigner,'verify','--verbose','--print-certs',$edCandidate) 'signature'
    if($edSignature -notmatch 'Signer #1 certificate SHA-256 digest:'){throw 'APK verifier did not report a signing certificate.'}
    Invoke-AndroidValidationTool (Join-Path $edSdkBuildTools[0].FullName 'zipalign.exe') @('-c','-P','16','-v','4',$edCandidate) 'zipalign' | Out-Null
    $edBadging=Invoke-AndroidValidationTool (Join-Path $edSdkBuildTools[0].FullName 'aapt.exe') @('dump','badging',$edCandidate) 'manifest'
    if($edBadging -notmatch "package: name='com.easterndynasty.s1'" -or $edBadging -notmatch ("versionCode='"+$VersionCode+"'") -or $edBadging -notmatch "sdkVersion:'25'" -or $edBadging -notmatch "launchable-activity:"){throw 'APK package identity, version, minimum Android API or launcher is incorrect.'}
} catch {$edFailure=$_}
finally {
    # Only the isolated snapshot was opened or switched to Android. Never write
    # the source ProjectSettings/Library or force its platform during cleanup.
    if(Test-Path -LiteralPath $edBuildLog){Copy-Item -LiteralPath $edBuildLog -Destination (Join-Path $edEvidence 'unity-android.log') -Force}
    try {Assert-AndroidSourceSnapshot $edRoot $edSnapshot} catch {if(-not $edFailure){$edFailure=$_}}
}
if($edFailure){
    [IO.File]::WriteAllText((Join-Path $edEvidence 'result.json'),(@{status='failed';error=[string]$edFailure;atUtc=[DateTime]::UtcNow.ToString('o');delivered=$false;workspaceCleanup=@{status='retained';workspace=$edWorkspace;reason='build-or-validation-failed'}}|ConvertTo-Json -Depth 4),[Text.UTF8Encoding]::new($false))
    throw $edFailure
}
New-Item -ItemType Directory -Path $edDestination -Force|Out-Null
$edHash=(Get-FileHash -LiteralPath $edCandidate -Algorithm SHA256).Hash
$edStaged=Join-Path $edDestination ('.'+$edOutputName+'.'+$edRunId+'.tmp')
Copy-Item -LiteralPath $edCandidate -Destination $edStaged
if((Get-FileHash -LiteralPath $edStaged -Algorithm SHA256).Hash -ne $edHash){throw 'Copied APK hash differs; old delivery remains intact.'}
# Verify the full original file set and bytes again immediately before delivery.
# The imported Android workspace itself may contain normal Unity-generated edits.
try {Assert-AndroidSourceSnapshot $edRoot $edSnapshot} catch {
    [IO.File]::WriteAllText((Join-Path $edEvidence 'result.json'),(@{status='failed';error=[string]$_;atUtc=[DateTime]::UtcNow.ToString('o');delivered=$false;workspaceCleanup=@{status='retained';workspace=$edWorkspace;reason='source-snapshot-changed'}}|ConvertTo-Json -Depth 4),[Text.UTF8Encoding]::new($false))
    throw
}
[IO.File]::Move($edStaged,$edOutput)
$edReceipt=[ordered]@{
    status='built-and-package-verified';applicationId='com.easterndynasty.s1';versionName=$VersionName;versionCode=$VersionCode
    apk=$edOutput;sha256=$edHash;bytes=(Get-Item -LiteralPath $edOutput).Length;unityVersion=(Get-EdUnityVersion)
    minimumAndroid='7.1';minimumApi=25;architectures=@('arm64-v8a','armeabi-v7a');scriptingBackend='IL2CPP';graphicsApi='OpenGLES3'
    signing='Unity local debug key';signatureVerified=$true;packageVerified=$true;zipAlignmentVerified=$true;deviceTested=$false
    originalProjectOpened=$false;sourceProjectSettingsUnchanged=((Get-FileHash -LiteralPath $edSettingsFile -Algorithm SHA256).Hash -eq $edBeforeHash);androidWorkspace=$edUnityProject
    sourceSnapshotVerified=$true;sourceSnapshotFiles=$edSnapshot.Count
    workspaceCleanup=@{status=$(if($CleanWorkspaceOnSuccess){'pending'}else{'disabled'});workspace=$edWorkspace;reason=$(if($CleanWorkspaceOnSuccess){'awaiting-successful-delivery'}else{'CleanWorkspaceOnSuccess-not-requested'})}
    builtUtc=[DateTime]::UtcNow.ToString('o');evidence=$edEvidence
}
$edReceiptText=$edReceipt|ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $edEvidence 'result.json'),$edReceiptText,[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText($edLatest,$edReceiptText,[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText(($edOutput+'.sha256'),($edHash+'  '+$edOutputName+[Environment]::NewLine),[Text.UTF8Encoding]::new($false))
$edCleanup=if($ReplacePrevious){Remove-AndroidPreviousArtifact $edPrevious $edDestination $edOutput}else{@{status='disabled';reason='ReplacePrevious-not-requested'}}
# Cleanup is optional and occurs only after the new artifact and receipt exist.
# Locked, altered, unregistered or out-of-directory artifacts are retained.
[IO.File]::WriteAllText((Join-Path $edEvidence 'previous-artifact-cleanup.json'),($edCleanup|ConvertTo-Json -Depth 4),[Text.UTF8Encoding]::new($false))
if($CleanWorkspaceOnSuccess){
    $edReceipt.workspaceCleanup=Remove-AndroidCurrentWorkspace $edWorkspace $edWorkspaceBase $edRunId $edRoot $edEvidence $edLatest
    # The APK is already delivered. Cleanup or cleanup-reporting failures must
    # not turn a successful package delivery into a failed build.
    try {
        [IO.File]::WriteAllText((Join-Path $edEvidence 'workspace-cleanup.json'),($edReceipt.workspaceCleanup|ConvertTo-Json -Depth 4),[Text.UTF8Encoding]::new($false))
        $edReceiptText=$edReceipt|ConvertTo-Json -Depth 5
        [IO.File]::WriteAllText((Join-Path $edEvidence 'result.json'),$edReceiptText,[Text.UTF8Encoding]::new($false))
        [IO.File]::WriteAllText($edLatest,$edReceiptText,[Text.UTF8Encoding]::new($false))
    } catch {Write-Warning ('APK delivery succeeded; cleanup status could not be fully saved: '+$_.Exception.Message)}
}
Write-Output ('ANDROID_APK_READY: '+$edOutput)
Write-Output ('SHA256: '+$edHash)
Write-Output ('PACKAGE_VALIDATION: '+(Join-Path $edEvidence 'result.json'))
Write-Output 'Device installation/play testing is separate; this result verifies compilation, package contents, signature and manifest.'
