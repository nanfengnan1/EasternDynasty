param([string]$OfficialFields='C:\Users\fenglei\Downloads\率土之滨_素材集_2026-10-25\_采集记录\cn_hero_data.json')
$ErrorActionPreference='Stop'
$edProject=Split-Path -Parent $PSScriptRoot
. (Join-Path $edProject 'Tools\Common.ps1')
$edData=Join-Path (Split-Path -Parent (Resolve-EdUnityEditor)) 'Data'
$edMono=Join-Path $edData 'MonoBleedingEdge\bin\mono.exe'
$edCompiler=Join-Path $edData 'MonoBleedingEdge\lib\mono\4.5\csc.exe'
$edCheckDir=Join-Path $PSScriptRoot 'CardDisplayChecks'
[IO.Directory]::CreateDirectory($edCheckDir)|Out-Null
$edCheckSource=Join-Path $edCheckDir 'ExportCardDisplay.cs'
$edCheckExe=Join-Path $edCheckDir 'ExportCardDisplay.exe'
$edSource=@'
using System; using System.Linq; using System.Globalization; using System.Text; using EasternDynasty;
static class ExportCardDisplay {
 static string N(double n) { return n.ToString("R",CultureInfo.InvariantCulture); }
 static string S(string s) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(s??"")); }
 static void Main() { foreach(var g in Catalog.Generals.Where(g=>g.s1))
  Console.WriteLine(string.Join("\t",new[]{g.id,N(g.defenseGrowth),N(g.strategyGrowth),N(g.speedGrowth),N(g.siege),N(g.siegeGrowth),N(g.attackDistance),S(g.biography),S(g.advancedTroops),S(g.combination),S(g.policyName),S(g.policyText)}));
 }
}
'@
[IO.File]::WriteAllText($edCheckSource,$edSource,[Text.UTF8Encoding]::new($false))
$edModel=@(Get-ChildItem -LiteralPath (Join-Path $edProject 'Assets\EasternDynasty\Scripts\Core') -Filter '*.cs'|ForEach-Object {$_.FullName})
& $edMono $edCompiler /nologo /target:exe /out:$edCheckExe $edModel $edCheckSource
if($LASTEXITCODE -ne 0){throw 'Card display export compile failed.'}
$edRows=@(& $edMono $edCheckExe)
if($LASTEXITCODE -ne 0){throw 'Card display export failed.'}
$edOfficial=@{}
foreach($edItem in (Get-Content -LiteralPath $OfficialFields -Raw -Encoding UTF8|ConvertFrom-Json)){$edOfficial[[string]$edItem.id]=$edItem}
$edNumeric=@('defGrow','ruseGrow','speedGrow','siege','siegeGrow','distance')
$edText=@('desc','type_availible','group','policyName','policyDesc')
$edSeen=@{}
foreach($edRow in $edRows){
 $edCells=$edRow.Split("`t")
 if($edCells.Count -ne 12){throw 'Unexpected card display export shape.'}
 $edId=$edCells[0].Substring(3)
 if($edSeen.ContainsKey($edId)){throw ('Duplicate card identity: '+$edId)}
 $edSeen[$edId]=$true
 $edOriginal=$edOfficial[$edId]
 if(-not $edOriginal){throw ('Missing official record: '+$edId)}
 for($edIndex=0;$edIndex -lt $edNumeric.Count;$edIndex++){
  $edExpected=[double]::Parse([string]$edOriginal.($edNumeric[$edIndex]),[Globalization.CultureInfo]::InvariantCulture)
  $edActual=[double]::Parse($edCells[$edIndex+1],[Globalization.CultureInfo]::InvariantCulture)
  if([Math]::Abs($edExpected-$edActual) -gt 0.0000001){throw ('Numeric mismatch: '+$edId+' '+$edNumeric[$edIndex])}
 }
 for($edIndex=0;$edIndex -lt $edText.Count;$edIndex++){
  $edExpected=[string]$edOriginal.($edText[$edIndex])
  $edActual=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($edCells[$edIndex+7]))
  if($edExpected -cne $edActual){throw ('Text mismatch: '+$edId+' '+$edText[$edIndex])}
 }
}
if($edRows.Count -ne 245){throw ('Unexpected S1 display-card count: '+$edRows.Count)}
$edResult='PASS: 245 card identities; all 11 added display fields match the pinned official source exactly (numeric tolerance 1e-7).'
[IO.File]::WriteAllText((Join-Path $edCheckDir 'result.txt'),$edResult,[Text.UTF8Encoding]::new($false))
Write-Output $edResult
