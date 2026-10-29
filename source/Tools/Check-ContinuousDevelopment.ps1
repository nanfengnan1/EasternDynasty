$ErrorActionPreference='Stop'
$edRoot=Split-Path -Parent $PSScriptRoot
$edNode=(Get-Command node.exe -ErrorAction Stop).Source
Push-Location -LiteralPath $edRoot
try {
    foreach($edScript in @('Automation\runner.mjs','Automation\publisher.mjs','Automation\publication-queue.mjs','Automation\policy.mjs','Automation\rag-sync.mjs','Automation\release-evidence.mjs')) {
        & $edNode --check $edScript
        if($LASTEXITCODE -ne 0){throw ('Node syntax failed: '+$edScript)}
    }
    foreach($edScript in @('Tools\Start-ContinuousDevelopment.ps1','Tools\Tick-ContinuousDevelopment.ps1','Tools\Tick-Publication.ps1','Tools\Publish-Iteration.ps1','Tools\Build-Android.ps1')) {
        $edTokens=$null;$edErrors=$null
        [Management.Automation.Language.Parser]::ParseFile((Join-Path $edRoot $edScript),[ref]$edTokens,[ref]$edErrors)|Out-Null
        if($edErrors){throw ('PowerShell syntax failed: '+$edScript+' '+($edErrors.Message -join '; '))}
    }
    foreach($edTest in @('Validation\HourlyAutomationChecks.mjs','Validation\AutomationChecks.mjs','Validation\RagSyncChecks.mjs','Validation\ReleaseEvidenceChecks.mjs','Validation\PublicationQueueChecks.mjs','Validation\PublisherChecks.mjs','Validation\DevelopmentIsolationChecks.mjs','Validation\PublicationPreparationChecks.mjs')) {
        & $edNode $edTest
        if($LASTEXITCODE -ne 0){throw ('Automation check failed: '+$edTest)}
    }
    & $edNode 'Automation\runner.mjs' --dry-run
    if($LASTEXITCODE -ne 0){throw 'Automation configuration check failed.'}
    & $edNode 'Automation\publisher.mjs' --dry-run
    if($LASTEXITCODE -ne 0){throw 'Publication configuration check failed.'}
    'CONTINUOUS_DEVELOPMENT_CHECKS_OK'
} finally {Pop-Location}
