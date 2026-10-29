# Shared project paths and Unity discovery. Dot-source from the public commands.
$script:EdProjectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))

function Get-EdProjectRoot { return $script:EdProjectRoot }
function Get-EdUnityVersion {
    $line = Get-Content -LiteralPath (Join-Path $script:EdProjectRoot 'ProjectSettings\ProjectVersion.txt') | Where-Object { $_ -match '^m_EditorVersion: ' } | Select-Object -First 1
    if (-not $line) { throw 'ProjectSettings/ProjectVersion.txt is missing the Unity version.' }
    return ($line -replace '^m_EditorVersion: ', '').Trim()
}
function Confirm-EdUnityEditor([string]$EditorPath,[string]$Version) {
    if(-not (Test-Path -LiteralPath $EditorPath -PathType Leaf)){throw ('Configured Unity Editor does not exist: '+$EditorPath)}
    $productVersion=(Get-Item -LiteralPath $EditorPath).VersionInfo.ProductVersion
    if(-not $productVersion -or $productVersion -notmatch ('^'+[regex]::Escape($Version)+'(?:[_\s]|$)')){throw ('Editor version differs: expected '+$Version+', found '+$productVersion+'. Choose this project version in Unity Hub.')}
    return (Resolve-Path -LiteralPath $EditorPath).Path
}
function Resolve-EdUnityEditor([string]$UnityEditor) {
    $version = Get-EdUnityVersion
    $candidate = $UnityEditor
    if (-not $candidate) { $candidate = $env:UNITY_EDITOR_PATH }
    $config = Join-Path $script:EdProjectRoot '.local\unity.json'
    if (-not $candidate -and (Test-Path -LiteralPath $config)) {
        $candidate = (Get-Content -LiteralPath $config -Raw -Encoding UTF8 | ConvertFrom-Json).unityEditor
    }
    if ($candidate) {
        return (Confirm-EdUnityEditor $candidate $version)
    }
    $locations = @(
        (Join-Path $env:ProgramFiles ('Unity\Hub\Editor\'+$version+'\Editor\Unity.exe')),
        ('D:\Unity\Hub\Editor\'+$version+'\Editor\Unity.exe')
    )
    foreach ($location in $locations) { if (Test-Path -LiteralPath $location -PathType Leaf) { return (Confirm-EdUnityEditor $location $version) } }
    throw ('Unity '+$version+' not found. Install it with Unity Hub, then run Tools/Setup.ps1 -UnityEditor <full path to Unity.exe>.')
}
function ConvertTo-EdProcessArguments([string[]]$Values) {
    return (($Values | ForEach-Object {
        if ($_ -match '["\r\n]') { throw 'Process arguments must not contain quotes or newlines.' }
        '"' + $_ + '"'
    }) -join ' ')
}
function Test-EdProjectCommandLine([string]$CommandLine,[string]$ProjectRoot) {
    $match=[regex]::Match($CommandLine,'(?:^|\s)-projectPath(?:\s+|=)(?:"([^"]+)"|(\S+))',[Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if(-not $match.Success){return $false}
    $candidate=if($match.Groups[1].Success){$match.Groups[1].Value}else{$match.Groups[2].Value}
    try {return [IO.Path]::GetFullPath($candidate).TrimEnd('\','/').Equals([IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\','/'),[StringComparison]::OrdinalIgnoreCase)} catch {return $false}
}
function Assert-EdEditorAvailable([string]$UnityEditor) {
    $active = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object {
        $_.Name -eq 'Unity.exe' -and $_.CommandLine -and (Test-EdProjectCommandLine $_.CommandLine $script:EdProjectRoot)
    })
    if ($active.Count -gt 0) { throw 'This project is already open in Unity. Use its Eastern Dynasty menu or close that Editor before a batch command.' }
}
function Invoke-EdUnity([string]$UnityEditor,[string]$Method,[string]$LogPath) {
    $editorPath = Resolve-EdUnityEditor $UnityEditor
    Assert-EdEditorAvailable $editorPath
    $arguments = ConvertTo-EdProcessArguments @('-batchmode','-nographics','-quit','-projectPath',$script:EdProjectRoot,'-executeMethod',$Method,'-logFile',$LogPath)
    $process = Start-Process -FilePath $editorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw ('Unity failed with code '+$process.ExitCode+'. See '+$LogPath) }
    $log = Get-Content -LiteralPath $LogPath -Raw -Encoding UTF8
    if ($log -match '(?im)^Shader error|^.*error CS[0-9]+:|^Exception:') { throw ('Unity reported a compilation/runtime error. See '+$LogPath) }
}
