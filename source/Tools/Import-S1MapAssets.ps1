[CmdletBinding()]
param(
    [string]$ProjectRoot=(Split-Path -Parent $PSScriptRoot),
    [string]$RagRoot='',
    [string]$CitySource='',
    [switch]$PrepareOnly
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Validate-Assets.ps1') -ProjectRoot $ProjectRoot -LibraryOnly
$project=Get-EdAssetRoot $ProjectRoot
if(-not $RagRoot){$RagRoot=Join-Path (Split-Path -Parent $project) 'rag'}
$rag=[IO.Path]::GetFullPath($RagRoot)
Assert-EdAssetNoReparse $rag
$mapInbox=Join-Path $project 'AssetInbox/S1Map'
Assert-EdAssetNoReparse $mapInbox
[IO.Directory]::CreateDirectory($mapInbox)|Out-Null
$resourceHash='b81b85839fc9949d282d2fa62312c98ff1f789a9f9d1d7d54f94a6643a4614ec'
$referenceSource=Join-Path $rag ('assets/b8/'+$resourceHash+'.jpg')
Assert-EdAssetNoReparse $referenceSource
if(-not (Test-Path -LiteralPath $referenceSource -PathType Leaf) -or (Get-FileHash -LiteralPath $referenceSource -Algorithm SHA256).Hash.ToLowerInvariant() -cne $resourceHash){throw 'Pinned RAG resource image is missing or its source hash changed.'}
$resourcePng=Join-Path $mapInbox 'resources-original-pixels.png'
Assert-EdAssetNoReparse $resourcePng
Add-Type -AssemblyName System.Drawing
$bitmap=[Drawing.Bitmap]::new($referenceSource)
try {
    if($bitmap.Width-ne320-or$bitmap.Height-ne320){throw 'Reference texture UVs require the pinned 320x320 image.'}
    # Format conversion only; the same source pixels are sampled by the game's mesh UVs.
    $bitmap.Save($resourcePng,[Drawing.Imaging.ImageFormat]::Png)
    $roundtrip=[Drawing.Bitmap]::new($resourcePng)
    try {
        for($y=0;$y-lt320;$y++){for($x=0;$x-lt320;$x++){
            if($bitmap.GetPixel($x,$y).ToArgb()-ne$roundtrip.GetPixel($x,$y).ToArgb()){throw "Lossless conversion changed pixel $x,$y."}
        }}
    } finally {$roundtrip.Dispose()}
} finally {$bitmap.Dispose()}
$cityPng=Join-Path $mapInbox 'city-reference-derivative.png'
Assert-EdAssetNoReparse $cityPng
if($CitySource){
    if($CitySource -match '^\\\\|^[a-z]+://|(^|[\\/])\.\.([\\/]|$)'){throw 'CitySource must be a local path without traversal or a network URL.'}
    $cityInput=[IO.Path]::GetFullPath($CitySource);Assert-EdAssetNoReparse $cityInput
    if(-not (Test-Path -LiteralPath $cityInput -PathType Leaf)){throw 'CitySource must be the existing generated transparent PNG.'}
    if($cityInput-ne[IO.Path]::GetFullPath($cityPng)){[IO.File]::Copy($cityInput,$cityPng,$true)}
}
if(-not (Test-Path -LiteralPath $cityPng -PathType Leaf)){throw 'Supply -CitySource with the generated city PNG; this command does not generate images.'}
Initialize-EdPngInspector
$cityInfo=[EdAssetPngInspector]::Read($cityPng)
if($cityInfo.Width-ne1536-or$cityInfo.Height-ne1024-or-not$cityInfo.HasTransparency){throw 'City derivative dimensions/alpha differ from the calibrated 1536x1024 transparent sprite.'}
if($cityInfo.Sha256-cne'178d9ac249e7178b9669edad891a1a40ec534e156a910e66d898f4155fba6b41'){throw 'City derivative hash differs from the recorded generation; review provenance and calibration before replacing it.'}
$meta=Join-Path $project 'Assets/EasternDynasty/Resources/Youzhou/city.png.meta'
$oldCity=Join-Path $project 'Assets/EasternDynasty/Resources/Youzhou/city.png'
$oldHash=(Get-FileHash -LiteralPath $oldCity -Algorithm SHA256).Hash.ToLowerInvariant()
$metaHash=(Get-FileHash -LiteralPath $meta -Algorithm SHA256).Hash.ToLowerInvariant()
$backup=Join-Path $mapInbox ('city-before-'+$oldHash+'.png')
Assert-EdAssetNoReparse $backup
if(-not(Test-Path -LiteralPath $backup)){[IO.File]::Copy($oldCity,$backup,$false)}
if($PrepareOnly){Write-Output 'S1_MAP_PREPARED: source pixels verified; transparent city staged; no production assets changed.';return}
$reference='https://nie.res.netease.com/r/pic/20151103/08151107-3425-4db6-9609-5592dfb2c8e4 | RAG S1-RES-004 SHA256='+$resourceHash+' | decoded source pixels losslessly stored as PNG; runtime mesh UV sampling | Docs/S1-Map-Art.md'
& (Join-Path $PSScriptRoot 'Import-Asset.ps1') -ProjectRoot $project -Id reference.youzhou.resources -Kind map-reference -SourcePath $resourcePng -Purpose 'S1四资源地块历史官网组合图；运行时UV采样' -SourceKind user-provided -SourceReference $reference -Rights '用户指定本地RAG；网易原始美术，保留来源；本记录不授予商业再分发许可。' -CreatedUtc '2026-10-25' -Replace
$cityReference='Docs/S1-Map-Art.md | image_gen edit 01a1717d-cb88-7812-af47-761d170077c2/exec-a7209b0d-72ac-47d3-8264-a31fca48c62a.png | source RAG S1-RES-010 SHA256=7213b8e71ac5f0414809de7858af8529184db33b0a0af3f9e6618da2b1c024cb | https://nie.res.netease.com/r/pic/20151103/c2c56d34-350f-410b-a751-f40777ad2135'
& (Join-Path $PSScriptRoot 'Import-Asset.ps1') -ProjectRoot $project -Id map.youzhou.city -Kind map-tile -SourcePath $cityPng -Purpose '依据历史官网主城截图去文字/背景生成的城池派生图；并非官网原始sprite' -SourceKind imagegen -SourceReference $cityReference -Rights '本地参考图经image_gen加工；来源原始美术归原权利人，本记录不授予商业再分发许可。' -CreatedUtc '2026-10-25' -Replace
if((Get-FileHash -LiteralPath $meta -Algorithm SHA256).Hash.ToLowerInvariant()-cne$metaHash){throw 'City .meta changed unexpectedly during image replacement.'}
Write-Output ('S1_MAP_IMPORT_OK: original resource pixels verified; city derivative='+$cityInfo.Sha256+'; city .meta unchanged.')
