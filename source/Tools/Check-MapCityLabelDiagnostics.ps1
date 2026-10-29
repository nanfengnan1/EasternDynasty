param([string]$UnityData,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
$edRoot=Get-EdProjectRoot
$edMono=Join-Path $UnityData 'MonoBleedingEdge/bin/mono.exe'
$edCompiler=Join-Path $UnityData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$edBin=Join-Path $edRoot 'Validation/bin'
New-Item -ItemType Directory -Path $edBin -Force | Out-Null
# Compile the actual capture boundary, so removing/restoring its target cleanup
# cannot be hidden by a copied mock. All rendering adapters remain synthetic.
$edCaptureSource=Get-Content -Encoding UTF8 -Raw (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/Diagnostics/MapHudVisualDiagnostics.cs')
$edCaptureStart=$edCaptureSource.IndexOf('        private IEnumerator CaptureMapHudState(string filename)')
if($edCaptureStart -lt 0){throw 'Production capture method is missing; update the pure harness.'}
$edCaptureEnd=$edCaptureSource.IndexOf('        private void AssertMapHudTargets()', $edCaptureStart)
if($edCaptureStart -lt 0 -or $edCaptureEnd -le $edCaptureStart){throw 'Production capture method boundaries changed; update the pure harness.'}
$edExtracted=Join-Path $edBin 'MapCityLabelCapture.production.cs'
$edCaptureMethod=$edCaptureSource.Substring($edCaptureStart,$edCaptureEnd-$edCaptureStart)
[IO.File]::WriteAllText($edExtracted,('using System; using System.Collections; using UnityEngine; namespace EasternDynasty { public sealed partial class EasternDynastyGame {'+[Environment]::NewLine+$edCaptureMethod+'} }'),[Text.UTF8Encoding]::new($false))
$edExe=Join-Path $edBin 'MapCityLabelDiagnosticsChecks.exe'
& $edMono $edCompiler /nologo /target:exe /out:$edExe (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/MapHudNavigationLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/MapCityLabelLayout.cs') (Join-Path $edRoot 'Assets/EasternDynasty/Scripts/Runtime/Diagnostics/MapCityLabelDiagnostics.cs') $edExtracted (Join-Path $edRoot 'Validation/MapCityLabelDiagnosticsChecks.cs')
if($LASTEXITCODE -ne 0){throw 'City marker diagnostic lifecycle harness failed to compile.'}
$edOutput=@(& $edMono $edExe 2>&1)
$edExit=$LASTEXITCODE
$edOutput | Write-Output
if($OutputDirectory){New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null;[IO.File]::WriteAllLines((Join-Path $OutputDirectory 'map-city-label-diagnostics-checks.log'),[string[]]$edOutput,[Text.UTF8Encoding]::new($false))}
if($edExit -ne 0){throw 'City marker diagnostic lifecycle regression failed.'}
