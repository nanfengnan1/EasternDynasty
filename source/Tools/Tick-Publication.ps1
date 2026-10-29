param([switch]$DryRun)
$ErrorActionPreference='Stop'
$edRoot=Split-Path -Parent $PSScriptRoot
$edPathsFile=Join-Path $edRoot '.local\continuous-development\paths.json'
if(-not (Test-Path -LiteralPath $edPathsFile)){throw 'Initialize the local development tool paths before starting publication.'}
$edPaths=Get-Content -LiteralPath $edPathsFile -Raw -Encoding UTF8 | ConvertFrom-Json
$edArguments=@((Join-Path $edRoot 'Automation\publisher.mjs'),'--source-root',$edRoot)
if($DryRun){$edArguments+='--dry-run'}
& $edPaths.node @edArguments
exit $LASTEXITCODE
