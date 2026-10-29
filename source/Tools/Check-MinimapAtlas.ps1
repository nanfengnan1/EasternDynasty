param([string]$UnityData,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$atlasRoot=Split-Path -Parent $PSScriptRoot
if(-not $UnityData){$UnityData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'}
if(-not $OutputDirectory){$OutputDirectory=Join-Path $atlasRoot 'Validation/MinimapAtlas'}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$atlasMono=Join-Path $UnityData 'MonoBleedingEdge/bin/mono.exe'
$atlasCompiler=Join-Path $UnityData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$atlasExe=Join-Path $OutputDirectory 'MinimapAtlasChecks.exe'
$atlasSource=Join-Path $atlasRoot 'Assets/EasternDynasty/Scripts/Runtime/MinimapAtlasRaster.cs'
$atlasViewportSource=Join-Path $atlasRoot 'Assets/EasternDynasty/Scripts/Runtime/MinimapViewportGeometry.cs'
$atlasCore=@(Get-ChildItem -LiteralPath (Join-Path $atlasRoot 'Assets/EasternDynasty/Scripts/Core') -Filter '*.cs' | ForEach-Object {$_.FullName})
& $atlasMono $atlasCompiler /nologo /target:exe /r:System.Drawing.dll /r:System.Runtime.Serialization.dll /out:$atlasExe $atlasCore (Join-Path $atlasRoot 'Validation/MinimapAtlasChecks.cs') $atlasSource $atlasViewportSource
if($LASTEXITCODE -ne 0){throw 'Minimap atlas checks failed to compile.'}
& $atlasMono $atlasExe $OutputDirectory $atlasSource $atlasViewportSource 2>&1 | Tee-Object -FilePath (Join-Path $OutputDirectory 'checks.log')
if($LASTEXITCODE -ne 0){throw 'Minimap atlas checks failed: inspect true land coordinates, bridge direction and immutable rendering.'}
& (Join-Path $atlasRoot 'Validation/MinimapNavigationChecks.ps1') -OutputDirectory $OutputDirectory
Write-Output 'MINIMAP_ATLAS_VALIDATION_OK: pure C# only; generated previews are not Unity or phone screenshots.'
