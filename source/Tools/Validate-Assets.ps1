param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [switch]$LibraryOnly
)
$ErrorActionPreference = 'Stop'

# Shared implementation for the CLI importer and its isolated tests. No Unity process.
function Initialize-EdPngInspector {
    if ('EdAssetPngInspector' -as [type]) { return }
    Add-Type -AssemblyName System.Drawing
    Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
public sealed class EdAssetPngInfo { public int Width,Height; public bool HasAlpha,HasTransparency; public string Sha256; }
public static class EdAssetPngInspector {
 static uint U32(byte[] b,int p){return ((uint)b[p]<<24)|((uint)b[p+1]<<16)|((uint)b[p+2]<<8)|b[p+3];}
 static readonly uint[] Table=MakeTable();
 static uint[] MakeTable(){var t=new uint[256];for(uint n=0;n<256;n++){uint c=n;for(int k=0;k<8;k++)c=(c&1)!=0?0xedb88320U^(c>>1):c>>1;t[n]=c;}return t;}
 static uint Crc(byte[] b,int p,int n){uint c=0xffffffff;for(int i=p;i<p+n;i++)c=Table[(c^b[i])&255]^(c>>8);return c^0xffffffff;}
 public static EdAssetPngInfo Read(string path){
  var file=new FileInfo(path); if(file.Length<45||file.Length>67108864)throw new InvalidDataException("PNG size outside 45 bytes..64 MiB.");
  byte[] b=File.ReadAllBytes(path),signature={137,80,78,71,13,10,26,10};
  for(int i=0;i<8;i++)if(b[i]!=signature[i])throw new InvalidDataException("Invalid PNG signature.");
  var result=new EdAssetPngInfo();bool header=false,data=false,end=false;int p=8;
  while(p<b.Length){
   if(b.Length-p<12)throw new InvalidDataException("Truncated PNG chunk.");uint length=U32(b,p);
   if(length>67108864||(long)p+length+12>b.Length)throw new InvalidDataException("Invalid PNG chunk length.");
   int len=(int)length;string type=System.Text.Encoding.ASCII.GetString(b,p+4,4);
   if(Crc(b,p+4,len+4)!=U32(b,p+8+len))throw new InvalidDataException("PNG CRC mismatch: "+type);
   if(!header&&type!="IHDR")throw new InvalidDataException("PNG must start with IHDR.");
   if(type=="IHDR"){
    if(header||len!=13)throw new InvalidDataException("Invalid PNG IHDR.");header=true;
    uint w=U32(b,p+8),h=U32(b,p+12);if(w<1||h<1||w>8192||h>8192||(long)w*h>16777216)throw new InvalidDataException("PNG dimensions exceed limits.");
    result.Width=(int)w;result.Height=(int)h;int color=b[p+17];result.HasAlpha=color==4||color==6;
   }
   if(type=="tRNS")result.HasAlpha=true;
   if(type=="IDAT")data=true;
   p+=len+12;
   if(type=="IEND"){if(len!=0||!data||p!=b.Length)throw new InvalidDataException("Invalid PNG IEND/trailing bytes.");end=true;break;}
  }
  if(!end)throw new InvalidDataException("PNG IEND missing.");
  using(var memory=new MemoryStream(b))using(var image=Image.FromStream(memory,false,true))using(var bitmap=new Bitmap(image)){
   if(bitmap.Width!=result.Width||bitmap.Height!=result.Height)throw new InvalidDataException("Decoded PNG dimensions mismatch.");
   var bits=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   try{var row=new byte[bitmap.Width*4];for(int y=0;y<bitmap.Height&&!result.HasTransparency;y++){
    Marshal.Copy(IntPtr.Add(bits.Scan0,y*bits.Stride),row,0,row.Length);for(int x=3;x<row.Length;x+=4)if(row[x]<255){result.HasTransparency=true;break;}
   }}finally{bitmap.UnlockBits(bits);}
  }
  using(var sha=SHA256.Create())result.Sha256=BitConverter.ToString(sha.ComputeHash(b)).Replace("-","").ToLowerInvariant();
  return result;
 }
}
'@
}

function Assert-EdAssetNoReparse([string]$Path) {
    $probe = [IO.Path]::GetFullPath($Path)
    while ($probe) {
        if (Test-Path -LiteralPath $probe) {
            $item = Get-Item -LiteralPath $probe -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Reparse/symlink paths are not allowed: $probe" }
        }
        $next = Split-Path -Parent $probe
        if ($next -eq $probe) { break }; $probe = $next
    }
}

function Get-EdAssetRoot([string]$Root) {
    if ($Root -match '^\\\\|^[a-z]+://') { throw 'ProjectRoot must be a local filesystem directory.' }
    if (![IO.Path]::IsPathRooted($Root)) { $Root=Join-Path (Get-Location).Path $Root }
    $full = [IO.Path]::GetFullPath($Root).TrimEnd('\','/')
    Assert-EdAssetNoReparse $full
    if (!(Test-Path -LiteralPath (Join-Path $full 'Assets/EasternDynasty/Resources') -PathType Container)) { throw "Resources directory missing under ProjectRoot: $full" }
    return $full
}

function Get-EdAssetResourcePath([string]$Id,[string]$Kind) {
    if ($Kind -ceq 'ui-sprite' -and $Id -cmatch '^ui\.youzhou\.[a-z][a-z0-9_]{0,63}$') { return 'YouzhouUI/'+$Id.Substring(11) }
    if ($Kind -ceq 'map-reference' -and $Id -cmatch '^reference\.youzhou\.[a-z][a-z0-9_]{0,63}$') { return 'YouzhouReferences/'+$Id.Substring(18) }
    if ($Kind -ceq 'map-tile' -and $Id -cmatch '^map\.youzhou\.[a-z][a-z0-9_]{0,63}$') { return 'Youzhou/'+$Id.Substring(12) }
    if ($Kind -ceq 'portrait' -and $Id -cmatch '^portrait\.[a-z][a-z0-9_]{0,63}$') { return 'Portraits/'+$Id.Substring(9) }
    throw "Unknown kind or invalid stable ID: '$Kind' / '$Id'."
}

function Get-EdAssetPath([string]$Root,[string]$ResourcePath) {
    if ($ResourcePath -cnotmatch '^(Youzhou|YouzhouReferences|YouzhouUI|Portraits)/[a-z][a-z0-9_]{0,63}$') { throw "Invalid Resources path: $ResourcePath" }
    $assetsRoot = [IO.Path]::GetFullPath((Join-Path $Root 'Assets/EasternDynasty/Resources'))
    $full = [IO.Path]::GetFullPath((Join-Path $assetsRoot ($ResourcePath+'.png')))
    if (!$full.StartsWith($assetsRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Asset path escapes Resources.' }
    Assert-EdAssetNoReparse $full
    return $full
}

function Assert-EdAssetFields($Value,[string[]]$Names,[string]$Context) {
    if ($null -eq $Value) { throw "$Context must be an object." }
    foreach ($property in $Value.PSObject.Properties.Name) { if ($Names -cnotcontains $property) { throw "Unknown field '$property' in $Context." } }
    foreach ($name in $Names) { if ($Value.PSObject.Properties.Name -cnotcontains $name) { throw "Missing field '$name' in $Context." } }
}

function Read-EdAssetManifest([string]$Root) {
    $path = Join-Path $Root 'Assets/EasternDynasty/Resources/asset-manifest.json'
    Assert-EdAssetNoReparse $path
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) { throw "Asset manifest missing: $path" }
    return (Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json)
}

function Read-EdValidatedPng([string]$Path) {
    # Re-read actual bytes and verify SHA-256 on every validation; reuse only
    # the decode/CRC result for identical content, never just file timestamps.
    $cache=[AppDomain]::CurrentDomain.GetData('EasternDynasty.ValidatedPngByContent.v1')
    if($null -eq $cache){$cache=[Collections.Generic.Dictionary[string,object]]::new();[AppDomain]::CurrentDomain.SetData('EasternDynasty.ValidatedPngByContent.v1',$cache)}
    $hash=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if($cache.ContainsKey($hash)){return $cache[$hash]}
    Initialize-EdPngInspector
    $info=[EdAssetPngInspector]::Read($Path)
    if($cache.Count -ge 1024){$cache.Clear()}
    $cache[$info.Sha256]=$info
    return $info
}

function Test-EdAssetManifest($Manifest,[string]$Root,[switch]$CheckFiles,[switch]$AllowTransaction) {
    Assert-EdAssetFields $Manifest @('schemaVersion','entries') 'manifest'
    if ($Manifest.schemaVersion -ne 1 -or $Manifest.entries -isnot [Array]) { throw 'Expected schemaVersion=1 and entries array.' }
    if (!$AllowTransaction -and (Test-Path -LiteralPath (Join-Path $Root 'AssetInbox/.asset-transaction'))) { throw 'Unfinished asset transaction; run Import-Asset -Recover before validation/build.' }
    $ids=@{}; $paths=@{}; $ready=0; $missing=0; $pendingMeta=0
    foreach ($entry in $Manifest.entries) {
        Assert-EdAssetFields $entry @('id','kind','status','required','resourcePath','purpose','source','width','height','hasAlpha','hasTransparency','sha256','importedUtc','placeholderPolicy') 'asset entry'
        $expected=Get-EdAssetResourcePath $entry.id $entry.kind
        if ($entry.resourcePath -cne $expected) { throw "Noncanonical Resources path for $($entry.id): $($entry.resourcePath)" }
        if ($ids.ContainsKey($entry.id) -or $paths.ContainsKey($entry.resourcePath)) { throw "Duplicate ID or Resources path: $($entry.id)" }
        $ids[$entry.id]=$entry; $paths[$entry.resourcePath]=$entry
        foreach ($flag in @('required','hasAlpha','hasTransparency')) { if ($entry.$flag -isnot [bool]) { throw "$($entry.id): $flag must be boolean." } }
        if ([string]::IsNullOrWhiteSpace($entry.purpose)) { throw "$($entry.id): purpose is required." }
        $assetPath=Get-EdAssetPath $Root $entry.resourcePath
        if ($entry.status -ceq 'ready') {
            $ready++
            if ($entry.width -isnot [int] -or $entry.height -isnot [int] -or $entry.width -lt 1 -or $entry.height -lt 1 -or $entry.width -gt 8192 -or $entry.height -gt 8192 -or [long]$entry.width*$entry.height -gt 16777216) { throw "$($entry.id): invalid dimensions." }
            if ($entry.sha256 -cnotmatch '^[a-f0-9]{64}$') { throw "$($entry.id): invalid SHA256." }
            Assert-EdAssetFields $entry.source @('kind','reference','rights','createdUtc') "$($entry.id).source"
            if (@('imagegen','user-provided','licensed-local') -cnotcontains $entry.source.kind -or [string]::IsNullOrWhiteSpace($entry.source.reference) -or [string]::IsNullOrWhiteSpace($entry.source.rights)) { throw "$($entry.id): source/rights metadata is required." }
            $parsedDate=[datetime]::MinValue
            if (![datetime]::TryParse($entry.importedUtc,[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::RoundtripKind,[ref]$parsedDate)) { throw "$($entry.id): invalid importedUtc." }
            if ($entry.source.createdUtc -and ![datetime]::TryParse($entry.source.createdUtc,[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::RoundtripKind,[ref]$parsedDate)) { throw "$($entry.id): invalid source.createdUtc." }
            if ($entry.placeholderPolicy -cne 'none' -or ($entry.hasTransparency -and !$entry.hasAlpha)) { throw "$($entry.id): invalid ready/alpha policy." }
            $aspect=$entry.width/[double]$entry.height
            if ($entry.kind -eq 'map-tile' -and (!$entry.hasAlpha -or !$entry.hasTransparency -or [Math]::Abs($aspect-1.5) -gt .08)) { throw "$($entry.id): map tiles require real alpha/transparency and a 3:2 canvas." }
            if ($entry.kind -eq 'portrait' -and [Math]::Abs($aspect-(2.0/3)) -gt .05) { throw "$($entry.id): portraits require a canvas near 2:3 (including native 240x340 references)." }
            if ($CheckFiles) {
                if (!(Test-Path -LiteralPath $assetPath -PathType Leaf)) { throw "$($entry.id): ready PNG missing." }
                Initialize-EdPngInspector
                try { $info=Read-EdValidatedPng $assetPath }
                catch {
                    $pngFailure=$_.Exception.GetBaseException()
                    $actualSha=(Get-FileHash -LiteralPath $assetPath -Algorithm SHA256).Hash.ToLowerInvariant()
                    throw [IO.InvalidDataException]::new("$($entry.id): invalid PNG at '$assetPath'; expected SHA256=$($entry.sha256); actual SHA256=$actualSha; $($pngFailure.Message)",$pngFailure)
                }
                if ($info.Sha256 -cne $entry.sha256 -or $info.Width -ne $entry.width -or $info.Height -ne $entry.height -or $info.HasAlpha -ne $entry.hasAlpha -or $info.HasTransparency -ne $entry.hasTransparency) { throw "$($entry.id): source PNG does not match manifest hash/dimensions/alpha at '$assetPath'; expected SHA256=$($entry.sha256); actual SHA256=$($info.Sha256)." }
                if (Test-Path -LiteralPath ($assetPath+'.meta')) {
                    Assert-EdAssetNoReparse ($assetPath+'.meta')
                    if ((Get-Content -LiteralPath ($assetPath+'.meta') -Raw) -notmatch '(?m)^guid: [a-f0-9]{32}\r?$') { throw "$($entry.id): invalid Unity meta GUID." }
                } else { $pendingMeta++ }
            }
        } elseif ($entry.status -ceq 'missing') {
            $missing++
            if ($entry.kind -cne 'portrait' -or $entry.required -or $entry.placeholderPolicy -cne 'ui-placeholder' -or $entry.width -ne 0 -or $entry.height -ne 0 -or $entry.sha256 -or $entry.hasAlpha -or $entry.hasTransparency) { throw "$($entry.id): missing portrait must be optional, with no invented image metadata." }
            if ($CheckFiles -and (Test-Path -LiteralPath $assetPath)) { throw "$($entry.id): unregistered file exists for a missing record; investigate rather than overwrite." }
        } else { throw "$($entry.id): unknown status '$($entry.status)'." }
    }
    if ($CheckFiles) {
        # Unity Resources paths are global across all Resources directories and extensions.
        $found=@{}
        Get-ChildItem -LiteralPath (Join-Path $Root 'Assets') -Recurse -File | Where-Object { $_.Extension -ne '.meta' } | ForEach-Object {
            $relative=$_.FullName.Substring($Root.Length+1).Replace('\','/')
            if ($relative -match '(?:^|/)Resources/(.+)$') {
                $key=$Matches[1]; $key=$key.Substring(0,$key.Length-[IO.Path]::GetExtension($key).Length)
                if ($paths.ContainsKey($key)) { if ($found.ContainsKey($key)) { throw "Duplicate global Resources key: $key" }; $found[$key]=$_.FullName }
            }
            if ($relative -match '^Assets/EasternDynasty/Resources/(Youzhou|YouzhouReferences|YouzhouUI|Portraits)/.+\.png$') {
                $key=$relative.Substring('Assets/EasternDynasty/Resources/'.Length);$key=$key.Substring(0,$key.Length-4)
                if (!$paths.ContainsKey($key)) { throw "Unregistered managed PNG: $relative" }
            }
        }
    }
    return [pscustomobject]@{ Ready=$ready; Missing=$missing; PendingMeta=$pendingMeta; Entries=$ids }
}

if (!$LibraryOnly) {
    $assetRoot=Get-EdAssetRoot $ProjectRoot
    $result=Test-EdAssetManifest (Read-EdAssetManifest $assetRoot) $assetRoot -CheckFiles
    Write-Output ("PASS asset files: ready={0}; missing={1}; pending Unity meta={2}. Missing portraits remain placeholders; Unity import settings are checked by AssetCatalogTools.ValidateForBuild()." -f $result.Ready,$result.Missing,$result.PendingMeta)
}
