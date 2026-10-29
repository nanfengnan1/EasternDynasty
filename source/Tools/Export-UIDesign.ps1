param(
    [Parameter(Mandatory=$true)][string]$SourcePath,
    [string]$InkscapePath,
    [ValidateRange(320,7680)][int]$Width=1920,
    [string[]]$ObjectIds=@()
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$edRoot=Get-EdProjectRoot
$edDesignRoot=[IO.Path]::GetFullPath((Join-Path $edRoot 'Docs\DesignUI')).TrimEnd('\')
if(-not [IO.Path]::IsPathRooted($SourcePath)){$SourcePath=Join-Path $edRoot $SourcePath}
$edSource=(Resolve-Path -LiteralPath $SourcePath).Path
if(-not $edSource.StartsWith($edDesignRoot+'\',[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetExtension($edSource) -ne '.svg'){throw 'Design sources must be SVG files under Docs/DesignUI.'}
$edXml=[Xml.XmlDocument]::new();$edXml.XmlResolver=$null;$edXml.Load($edSource)
if($edXml.SelectNodes("//*[local-name()='script' or local-name()='foreignObject']").Count -gt 0){throw 'Executable content is not allowed in design sources.'}
foreach($edAttribute in $edXml.SelectNodes('//@*')){if($edAttribute.LocalName -eq 'href' -and $edAttribute.Value -notmatch '^(data:image/|#)'){throw 'Embed reference images; external links must not be fetched during export.'}}
if(-not $InkscapePath){
    $edConfigFile=Join-Path $edRoot '.local\design-tools.json'
    if(Test-Path -LiteralPath $edConfigFile){$InkscapePath=(Get-Content -LiteralPath $edConfigFile -Raw -Encoding UTF8|ConvertFrom-Json).inkscape}
}
if(-not $InkscapePath){$InkscapePath='F:\EasternDynastyBuild\DesignTools\Inkscape-1.4.4\PFiles64\Inkscape\bin\inkscape.com'}
if(-not(Test-Path -LiteralPath $InkscapePath -PathType Leaf)){throw 'Inkscape is unavailable. Read Docs/Professional-UI-Workflow.md; do not claim a tool export.'}
$edOutput=Join-Path (Split-Path -Parent $edSource) 'exports'
New-Item -ItemType Directory -Path $edOutput -Force|Out-Null
function Invoke-EdDesignTool([string[]]$Arguments,[string]$Tag){
    $stdout=Join-Path $edOutput ($Tag+'.stdout.log');$stderr=Join-Path $edOutput ($Tag+'.stderr.log')
    # Wait only for the CLI process, not the inherited Windows job containing
    # other workers. Normalize case-duplicate environment names from the host.
    # This changes only this PowerShell process, never user/machine settings.
    $edEnvironment=[Environment]::GetEnvironmentVariables()
    if($edEnvironment.Contains('Path') -and $edEnvironment.Contains('PATH')){
        $edPathValue=[string]$edEnvironment['Path']
        [Environment]::SetEnvironmentVariable('PATH',$null,'Process')
        [Environment]::SetEnvironmentVariable('Path',$edPathValue,'Process')
    }
    $start=[Diagnostics.ProcessStartInfo]::new()
    $start.FileName=$InkscapePath;$start.Arguments=ConvertTo-EdProcessArguments $Arguments
    $start.WorkingDirectory=Split-Path -Parent $edSource
    $start.UseShellExecute=$false;$start.CreateNoWindow=$true
    $start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
    $process=[Diagnostics.Process]::new();$process.StartInfo=$start
    try {
        if(-not $process.Start()){throw 'Inkscape process did not start.'}
        $outTask=$process.StandardOutput.ReadToEndAsync();$errTask=$process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        [IO.File]::WriteAllText($stdout,$outTask.Result,[Text.UTF8Encoding]::new($false))
        [IO.File]::WriteAllText($stderr,$errTask.Result,[Text.UTF8Encoding]::new($false))
        if($process.ExitCode -ne 0){throw ('Inkscape failed ('+$process.ExitCode+'): '+$stderr)}
    } finally {$process.Dispose()}
    return $stdout
}
$edVersionLog=Invoke-EdDesignTool @('--version') 'version'
$edVersion=(Get-Content -LiteralPath $edVersionLog -Raw -Encoding UTF8).Trim()
$edBase=[IO.Path]::GetFileNameWithoutExtension($edSource)
$edPreview=Join-Path $edOutput ($edBase+'-preview.png')
Invoke-EdDesignTool @($edSource,'--export-type=png','--export-area-page',('--export-width='+$Width),('--export-filename='+$edPreview)) 'preview'|Out-Null
$edGeometryLog=Invoke-EdDesignTool @($edSource,'--query-all') 'geometry'
$edGeometry=Join-Path $edOutput 'geometry.csv'
Copy-Item -LiteralPath $edGeometryLog -Destination $edGeometry -Force
$edArtifacts=@($edPreview)
foreach($edId in $ObjectIds){
    if($edId -notmatch '^[A-Za-z_][A-Za-z0-9_.-]*$' -or -not $edXml.SelectSingleNode("//*[@id='$edId']")){throw ('Unknown or invalid design object: '+$edId)}
    $edSlice=Join-Path $edOutput ($edBase+'-'+$edId+'.png')
    Invoke-EdDesignTool @($edSource,'--export-type=png',('--export-id='+$edId),'--export-id-only',('--export-filename='+$edSlice)) ('object-'+$edId)|Out-Null
    $edArtifacts+=$edSlice
}
Add-Type -AssemblyName System.Drawing
$edImages=@(foreach($file in $edArtifacts){
    $bitmap=[Drawing.Image]::FromFile($file)
    try{@{path=$file;relativePath=$file.Substring($edRoot.Length+1);width=$bitmap.Width;height=$bitmap.Height;sha256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash}}finally{$bitmap.Dispose()}
})
$edReceipt=[ordered]@{status='exported-not-runtime-verified';atUtc=[DateTime]::UtcNow.ToString('o');tool=$InkscapePath;toolVersion=$edVersion;source=$edSource;relativeSource=$edSource.Substring($edRoot.Length+1);sourceSha256=(Get-FileHash -LiteralPath $edSource -Algorithm SHA256).Hash;geometry=$edGeometry;images=$edImages;note='Design export only. Reference measurement sheets and design previews are not playable UI or production artwork.'}
[IO.File]::WriteAllText((Join-Path $edOutput 'export-receipt.json'),($edReceipt|ConvertTo-Json -Depth 7),[Text.UTF8Encoding]::new($false))
$edReceipt|ConvertTo-Json -Depth 7
