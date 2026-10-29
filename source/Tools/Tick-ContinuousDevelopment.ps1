$ErrorActionPreference='Stop'
$edRoot=Split-Path -Parent $PSScriptRoot
$edPaths=Get-Content -LiteralPath (Join-Path $edRoot '.local\continuous-development\paths.json') -Raw -Encoding UTF8 | ConvertFrom-Json
& $edPaths.node (Join-Path $edRoot 'Automation\runner.mjs')
exit $LASTEXITCODE
