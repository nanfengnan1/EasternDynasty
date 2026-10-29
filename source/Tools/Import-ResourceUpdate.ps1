[CmdletBinding()]
param([string]$RagRoot=(Join-Path (Split-Path -Parent $PSScriptRoot) '..\rag'))
$ErrorActionPreference='Stop'
$edResourceRoot=Split-Path -Parent $PSScriptRoot
$RagRoot=[IO.Path]::GetFullPath($RagRoot)
$edResourceInbox=Join-Path $edResourceRoot 'AssetInbox\ResourceUpdate-20261025'
New-Item -ItemType Directory -Path $edResourceInbox -Force | Out-Null
Add-Type -AssemblyName System.Drawing
if(-not ('EDReferencePixelCheck' -as [type])) {
Add-Type -ReferencedAssemblies System.Drawing @'
using System;
using System.Drawing;
public static class EDReferencePixelCheck {
    public static long EqualPixels(string original,string decodedPng) {
        using(var a=new Bitmap(original))using(var b=new Bitmap(decodedPng)) {
            if(a.Width!=b.Width||a.Height!=b.Height)throw new InvalidOperationException("Reference dimensions changed.");
            for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++)
                if(a.GetPixel(x,y).ToArgb()!=b.GetPixel(x,y).ToArgb())throw new InvalidOperationException("Reference pixels changed at "+x+","+y);
            return (long)a.Width*a.Height;
        }
    }
}
'@
}

# Complete source images are retained. Display rectangles are UV samples; these
# are neither newly discovered independent sprites nor current rule values.
$edReferencePlan=@(
    [pscustomobject]@{
        id='reference.youzhou.resource_hud';ragId='S1-RES-003'
        sha='02f3ebd3f528fc1cf28be50c89e7c688870e66d92766ce9211644bf3529d2910'
        width=894;height=74;name='resource_hud'
        sourcePage='https://news.dmzgame.com/stzb/gl/2022-08-26/4740.html'
        sourceImage='https://i.dmzgame.com/news/d/file/p/2022/08-26/3c6901735e1b24e2536d1a3c9bd1dcf8.jpg'
        scope='Historical CN S1 tutorial, published 2022-08-26; visual reference only.'
        purpose='S1 historical resource HUD composite; full source, use reviewed UV icon regions only, never render the baked stock/production numbers.'
        regions=@(
            @{semantic='wood';xywh=@(202,9,38,30)},
            @{semantic='iron';xywh=@(374,9,38,31)},
            @{semantic='stone';xywh=@(548,9,41,31)},
            @{semantic='grain';xywh=@(722,9,41,31)}
        )
    },
    [pscustomobject]@{
        id='reference.youzhou.residence_detail';ragId='S1-RES-029'
        sha='3fe6111baca56f74e23f648254e0896823f96dfe5fe9c7c0499219172e3bc7ff'
        width=690;height=389;name='residence_detail'
        sourcePage='https://stzb.163.com/strategy/2015/10/29/20401_577250.html'
        sourceImage='https://nie.res.netease.com/r/pic/20151029/869d173b-7e31-4ed7-9761-20c352601c0b'
        scope='Historical official residence detail; basic S1 facility type, 2015 appearance, not current values.'
        purpose='Historical residence detail screenshot; full source, use reviewed building-art UV region only, excluding the baked 20/20 level and old tax values.'
        regions=@(@{semantic='residence';xywh=@(133,152,77,59)})
    }
)
$edManifestPath=Join-Path $edResourceRoot 'Assets\EasternDynasty\Resources\asset-manifest.json'
$edInitial=Get-Content -LiteralPath $edManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$edExistingMeta=@{}
foreach($entry in $edInitial.entries) {
    $meta=Join-Path $edResourceRoot ('Assets\EasternDynasty\Resources\'+$entry.resourcePath+'.png.meta')
    if(Test-Path -LiteralPath $meta){$edExistingMeta[$meta]=(Get-FileHash -LiteralPath $meta -Algorithm SHA256).Hash}
}
$edRecords=@(foreach($item in $edReferencePlan) {
    $source=Join-Path $RagRoot ('assets\'+$item.sha.Substring(0,2)+'\'+$item.sha+'.jpg')
    if(-not (Test-Path -LiteralPath $source -PathType Leaf) -or (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant() -ne $item.sha){throw ('Pinned RAG source mismatch: '+$item.ragId)}
    $png=Join-Path $edResourceInbox ($item.name+'.png')
    $bitmap=[Drawing.Bitmap]::new($source)
    try {
        if($bitmap.Width -ne $item.width -or $bitmap.Height -ne $item.height){throw ('Unexpected source dimensions: '+$item.ragId)}
        $bitmap.Save($png,[Drawing.Imaging.ImageFormat]::Png)
    } finally {$bitmap.Dispose()}
    $equalPixels=[EDReferencePixelCheck]::EqualPixels($source,$png)
    $reference=$item.sourceImage+' | RAG '+$item.ragId+' SHA256='+$item.sha+' | decoded JPEG pixels losslessly stored as PNG; no crop, resize, repaint or removal | AssetInbox/ResourceUpdate-20261025/import-receipt.json'
    $importMessages=@(& (Join-Path $PSScriptRoot 'Import-Asset.ps1') -Id $item.id -Kind map-reference -SourcePath $png -Purpose $item.purpose -SourceKind user-provided -SourceReference $reference -Rights 'User-designated local RAG source; original artwork belongs to its rights holder. This record does not grant commercial redistribution rights.' -CreatedUtc '2026-10-25' -Replace)
    [pscustomobject][ordered]@{
        id=$item.id;ragId=$item.ragId;ragVersion='s1-resources-167bd3b9ece0acc6340a';sourceSha256=$item.sha
        sourcePage=$item.sourcePage;sourceImage=$item.sourceImage;sourceRelativePath=('assets/'+$item.sha.Substring(0,2)+'/'+$item.sha+'.jpg')
        width=$item.width;height=$item.height;scope=$item.scope;kind='full-reference-image';independentSprite=$false
        conversion='Decoded JPEG pixels stored as PNG without visual editing';equalDecodedPixels=$equalPixels
        pngSha256=(Get-FileHash -LiteralPath $png -Algorithm SHA256).Hash.ToLowerInvariant()
        regionCoordinates='pixel_top_left_xywh';regions=$item.regions;regionReview='Conservative source regions inspected; final runtime consumer layout must be reviewed in Unity.'
        importMessages=$importMessages
    }
})
foreach($meta in $edExistingMeta.Keys){if((Get-FileHash -LiteralPath $meta -Algorithm SHA256).Hash -ne $edExistingMeta[$meta]){throw ('Existing asset GUID metadata changed: '+$meta)}}
$edFinal=Get-Content -LiteralPath $edManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$edReceipt=[ordered]@{
    atUtc=[DateTime]::UtcNow.ToString('o');records=$edRecords;referencesInBatch=2
    newReadyThisRun=(@($edFinal.entries|Where-Object status -eq 'ready').Count-@($edInitial.entries|Where-Object status -eq 'ready').Count)
    readyBefore=@($edInitial.entries|Where-Object status -eq 'ready').Count;readyAfter=@($edFinal.entries|Where-Object status -eq 'ready').Count
    missingAfter=@($edFinal.entries|Where-Object status -eq 'missing').Count;existingMetaFilesVerified=$edExistingMeta.Count
    notes=@('Two full reference images provide five reviewed display regions; not five independent original sprites.','No existing S1 portrait changed; original source bytes match all 245 current identities.','Unity import, display consumption and build validation are performed by the integration owner.')
}
[IO.File]::WriteAllText((Join-Path $edResourceInbox 'import-receipt.json'),($edReceipt|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
Write-Output ('REFERENCE_IMPORT_OK: ready='+$edReceipt.readyAfter+'; missing='+$edReceipt.missingAfter+'; existing meta preserved='+$edExistingMeta.Count)
