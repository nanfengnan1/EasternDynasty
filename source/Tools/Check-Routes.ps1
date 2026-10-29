param([ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]*$')][string]$Tag='routes')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$edRoot=Get-EdProjectRoot
$edPlayer=$null
function Assert-RoutePlayerAvailable([int]$AllowedId=0) {
    $others=@(Get-CimInstance Win32_Process -Filter "Name='EasternDynasty.exe'" -ErrorAction Stop | Where-Object {$_.ProcessId -ne $AllowedId})
    if($others.Count -gt 0){throw 'PLAYER_IN_USE: Route verification deferred; an existing player is in use.'}
}
function Get-RouteSaveHashes {
    $saveRoot=Join-Path $env:USERPROFILE 'AppData\LocalLow\EasternDynasty\EasternDynasty'
    $hashes=@{}
    foreach($name in @('campaign.json','campaign.json.bak','youzhou-40-v1.json','youzhou-40-v1.json.bak')) {
        $file=Join-Path $saveRoot $name
        $hashes[$name]=if(Test-Path -LiteralPath $file){(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash}else{$null}
    }
    return $hashes
}
function Get-RouteImageContent([Drawing.Bitmap]$Bitmap) {
    $samples=0;$nonBlack=0;$sum=0.0;$sumSquares=0.0;$minimum=255.0;$maximum=0.0
    $colors=[Collections.Generic.HashSet[int]]::new()
    for($y=8;$y -lt $Bitmap.Height;$y+=16){
        for($x=8;$x -lt $Bitmap.Width;$x+=16){
            $pixel=$Bitmap.GetPixel($x,$y)
            $luma=($pixel.R+$pixel.G+$pixel.B)/3.0
            $samples++;$sum+=$luma;$sumSquares+=$luma*$luma
            if($pixel.R -gt 12 -or $pixel.G -gt 12 -or $pixel.B -gt 12){$nonBlack++}
            $minimum=[Math]::Min($minimum,$luma);$maximum=[Math]::Max($maximum,$luma)
            [void]$colors.Add((($pixel.R -shr 4) -shl 8) -bor (($pixel.G -shr 4) -shl 4) -bor ($pixel.B -shr 4))
        }
    }
    if($samples -eq 0){throw 'Route capture has no measurable image samples.'}
    $deviation=[Math]::Sqrt([Math]::Max(0,($sumSquares/$samples)-[Math]::Pow($sum/$samples,2)))
    $fraction=$nonBlack/[double]$samples
    [pscustomobject]@{samples=$samples;nonBlackFraction=$fraction;lumaRange=($maximum-$minimum);lumaDeviation=$deviation;quantizedColors=$colors.Count;hasContent=($fraction -ge .5 -and ($maximum-$minimum) -ge 48 -and $deviation -ge 8 -and $colors.Count -ge 32)}
}
Assert-RoutePlayerAvailable
$edBefore=Get-RouteSaveHashes
$edRunName=[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8)
$edArtifacts=Join-Path $edRoot ('Validation\Visual\'+$Tag+'-routes-1440x900\'+$edRunName)
New-Item -ItemType Directory -Path $edArtifacts | Out-Null
$edLog=Join-Path $edArtifacts 'player.log'
$edStarted=[DateTime]::UtcNow
try {
    Assert-RoutePlayerAvailable
    $edArgs=@('-edSmoke','-edRoutes','-edRouteOutput',$edArtifacts,'-screen-fullscreen','0','-screen-width','1440','-screen-height','900','-logFile',$edLog)
    $edPlayer=Start-Process -FilePath (Join-Path $edRoot 'Builds\Windows\EasternDynasty.exe') -ArgumentList (ConvertTo-EdProcessArguments $edArgs) -WindowStyle Hidden -PassThru
    $edDeadline=[DateTime]::UtcNow.AddSeconds(90)
    while(-not $edPlayer.WaitForExit(300)) {
        Assert-RoutePlayerAvailable $edPlayer.Id
        if([DateTime]::UtcNow -ge $edDeadline){throw 'Route runtime check timed out. See player.log.'}
    }
    $edPlayer.Refresh()
    Assert-RoutePlayerAvailable $edPlayer.Id
    if($edPlayer.ExitCode -ne 0){throw ('Route runtime Player exited with code '+$edPlayer.ExitCode+'. See '+$edLog)}
    $edReceiptPath=Join-Path $edArtifacts 'result.json'
    if(-not (Test-Path -LiteralPath $edReceiptPath -PathType Leaf)){throw 'Route runtime receipt is missing.'}
    $edReceipt=Get-Content -LiteralPath $edReceiptPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if($edReceipt.status -ne 'passed' -or $edReceipt.width -ne 1440 -or $edReceipt.height -ne 900){throw 'Route runtime receipt failed or has incorrect screen dimensions.'}
    if($edReceipt.captureMethod -ne 'map-camera-rendertexture'){throw 'Route capture did not use the isolated real map camera renderer.'}
    if([DateTimeOffset]::Parse($edReceipt.verifiedUtc).UtcDateTime -lt $edStarted){throw 'Route receipt predates this run.'}
    foreach($edFlag in @('isolatedSave','restored','continuedAfterLoad','unreachableRejected')) {
        if($edReceipt.$edFlag -ne $true){throw ('Missing route runtime evidence: '+$edFlag)}
    }
    if($edReceipt.checks -lt 20 -or @($edReceipt.path).Count -lt 5 -or $edReceipt.turn -lt 1 -or $edReceipt.turn -ge $edReceipt.bridge -or $edReceipt.bridge -ge (@($edReceipt.path).Count-2)){throw 'Route receipt does not contain the expected turning bridge path.'}
    $edSaved=[IO.Path]::GetFullPath($edReceipt.savedFile)
    $edSavePrefix=(Join-Path $edArtifacts 'isolated-save')+'\'
    if(-not $edSaved.StartsWith($edSavePrefix,[StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath $edSaved -PathType Leaf)){throw 'Route restore did not use its isolated save directory.'}
    $edExpected=@('turn','bridge','after-load','arrived')
    if(@($edReceipt.samples).Count -ne $edExpected.Count){throw 'Expected four rendered route samples.'}
    Add-Type -AssemblyName System.Drawing
    $edImageMetrics=@()
    foreach($edStage in $edExpected) {
        $edSamples=@($edReceipt.samples | Where-Object {$_.stage -eq $edStage})
        if($edSamples.Count -ne 1){throw ('Missing or duplicated route sample: '+$edStage)}
        $edSample=$edSamples[0]
        if([Math]::Abs($edSample.expectedX-$edSample.renderedX) -ge .002 -or [Math]::Abs($edSample.expectedZ-$edSample.renderedZ) -ge .002){throw ('Rendered army differs from route sample: '+$edStage)}
        $edVertices=if($edStage -eq 'arrived'){0}else{@($edReceipt.path).Count}
        if($edSample.renderedRouteVertices -ne $edVertices){throw ('Incorrect rendered route vertex count: '+$edStage)}
        $edCapture=Join-Path $edArtifacts ('route-'+$edStage+'.png')
        if([IO.Path]::GetFullPath($edSample.screenshot) -ne $edCapture -or -not (Test-Path -LiteralPath $edCapture -PathType Leaf)){throw ('Route capture missing or outside this run: '+$edStage)}
        $edBitmap=[Drawing.Image]::FromFile($edCapture)
        try {
            if($edBitmap.Width -ne 1440 -or $edBitmap.Height -ne 900){throw ('Incorrect route capture dimensions: '+$edStage)}
            $edContent=Get-RouteImageContent $edBitmap
            $edImageMetrics+=[pscustomobject]@{stage=$edStage;image=$edCapture;content=$edContent}
            [IO.File]::WriteAllText((Join-Path $edArtifacts 'image-content.json'),($edImageMetrics | ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
            if(-not $edContent.hasContent){throw ('Route capture is black, uniform or lacks map detail: '+$edStage+'; see image-content.json')}
        } finally {$edBitmap.Dispose()}
    }
    $edText=Get-Content -LiteralPath $edLog -Raw -Encoding UTF8
    if(-not $edText.Contains('ROUTE_RUNTIME_OK:') -or -not $edText.Contains('"stage":"restored-bridge"')){throw 'Route runtime completion or restored rendered position marker is missing.'}
    if($edText -match '(?im)^ERROR:|^Shader error|Exception:|^Assertion failed'){throw 'Route Player log contains a runtime or shader error.'}
    $edAfter=Get-RouteSaveHashes
    foreach($edName in $edBefore.Keys){if($edBefore[$edName] -ne $edAfter[$edName]){throw ('Player save changed during isolated route verification: '+$edName)}}
    $edResult='PASS: '+$edReceipt.checks+' rendered route checks; four captures, turn, bridge, isolated Unity JSON restore, continuation, arrival and unreachable rejection; '+$edArtifacts
    [IO.File]::WriteAllText((Join-Path $edArtifacts 'result.txt'),$edResult,[Text.UTF8Encoding]::new($false))
    Write-Output $edResult
} finally {
    if($null -ne $edPlayer){
        $edPlayer.Refresh()
        if(-not $edPlayer.HasExited){Stop-Process -Id $edPlayer.Id -Force -ErrorAction SilentlyContinue}
        $edPlayer.Dispose()
    }
}
