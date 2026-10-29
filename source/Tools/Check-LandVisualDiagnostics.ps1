param([string]$UnityData,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$edRoot=Get-EdProjectRoot
$edMono=Join-Path $UnityData 'MonoBleedingEdge/bin/mono.exe'
$edCompiler=Join-Path $UnityData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$edBin=Join-Path $edRoot 'Validation/bin'
New-Item -ItemType Directory -Path $edBin -Force | Out-Null
# Execute the production coroutine, using a synthetic Update/OnGUI/EndOfFrame
# scheduler. This check never starts the Editor, a Player or desktop interaction.
$edSource=Get-Content -Encoding UTF8 -Raw (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/Diagnostics/LandVisualDiagnostics.cs')
$edStart=$edSource.IndexOf('        private IEnumerator CaptureLandVisualState(string filename)')
$edEnd=$edSource.IndexOf('        private IEnumerator LandVisualSmoke()', $edStart)
if($edStart -lt 0 -or $edEnd -le $edStart){throw 'Land capture method boundaries changed; update the pure harness.'}
$edMethod=$edSource.Substring($edStart,$edEnd-$edStart)
$edExtracted=Join-Path $edBin 'LandVisualCapture.production.cs'
[IO.File]::WriteAllText($edExtracted,('using System; using System.Collections; using System.IO; using UnityEngine; namespace EasternDynasty { public sealed partial class EasternDynastyGame {'+[Environment]::NewLine+$edMethod+'} }'),[Text.UTF8Encoding]::new($false))
$edExe=Join-Path $edBin 'LandVisualDiagnosticsChecks.exe'
$edLayouts=@('MapHudNavigationLayout.cs','LandCommandMenuLayout.cs','LandSelectionLayout.cs') | ForEach-Object {Join-Path $edRoot ('Assets/EasternDynasty/Scripts/Runtime/'+$_)}
& $edMono $edCompiler /nologo /target:exe /out:$edExe $edLayouts $edExtracted (Join-Path $edRoot 'Validation/LandVisualDiagnosticsChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Land capture frame-order harness failed to compile.'}
$edOutput=@(& $edMono $edExe 2>&1)
$edExit=$LASTEXITCODE
$edOutput | Write-Output
if($edExit -ne 0){throw 'Land capture frame-order regression failed.'}
# A timing mutation must recover the previous defect. It leaves production files
# untouched and proves the scheduler does not silently accept stale observations.
$edBoundary='yield return new WaitForEndOfFrame();'
if(([regex]::Matches($edMethod,[regex]::Escape($edBoundary))).Count -ne 1){throw 'Expected one rendered-frame observation boundary.'}
$edMutated=Join-Path $edBin 'LandVisualCapture.old-schedule.cs'
$edOldMethod=$edMethod.Replace($edBoundary,'yield return null;')
[IO.File]::WriteAllText($edMutated,('using System; using System.Collections; using System.IO; using UnityEngine; namespace EasternDynasty { public sealed partial class EasternDynastyGame {'+[Environment]::NewLine+$edOldMethod+'} }'),[Text.UTF8Encoding]::new($false))
$edMutationExe=Join-Path $edBin 'LandVisualDiagnosticsOldScheduleChecks.exe'
& $edMono $edCompiler /nologo /target:exe /out:$edMutationExe $edLayouts $edMutated (Join-Path $edRoot 'Validation/LandVisualDiagnosticsChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Land capture old-schedule probe failed to compile.'}
$edMutationOutput=@(& $edMono $edMutationExe --require-stale-observation 2>&1)
if($LASTEXITCODE -ne 0){throw 'Land capture old-schedule probe failed to reproduce stale observations.'}
$edMutationOutput | Write-Output
if($OutputDirectory){New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null;[IO.File]::WriteAllLines((Join-Path $OutputDirectory 'land-visual-diagnostics-checks.log'),[string[]]($edOutput+$edMutationOutput),[Text.UTF8Encoding]::new($false))}
