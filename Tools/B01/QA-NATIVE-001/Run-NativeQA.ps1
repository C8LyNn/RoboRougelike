# B01-60: independent Windows runner; only an opted-in disposable worktree.
[CmdletBinding()]param(
 [Parameter(Mandatory=$true)][string]$ProjectPath,
 [Parameter(Mandatory=$true)][string]$UnityExe,
 [Parameter(Mandatory=$true)][string]$PlanPath,
 [Parameter(Mandatory=$true)][string]$EvidencePath,
 [string]$PlayerExe='',
 [int]$TimeoutMinutes=20,
 [string]$Python='python'
)
$ErrorActionPreference='Stop'
$project=(Resolve-Path -LiteralPath $ProjectPath).Path
$editor=(Resolve-Path -LiteralPath $UnityExe).Path
$plan=Get-Content -LiteralPath $PlanPath -Raw | ConvertFrom-Json
if($plan.schema -ne 'b01-60-native-plan-v1'){throw 'INVALID_QA_PLAN'}
if($editor -notmatch '6000[.]3[.]14f1'){throw 'WRONG_UNITY_EDITOR'}
if(!(Test-Path (Join-Path $project '.b01-60-disposable-qa'))){throw 'ACTIVE_WORKTREE_PROTECTION: marker missing'}
if(Test-Path (Join-Path $project 'Temp/UnityLockfile')){throw 'ACTIVE_UNITY_EDITOR'}
if(!(Select-String -LiteralPath (Join-Path $project 'ProjectSettings/ProjectVersion.txt') -Pattern 'm_EditorVersion: 6000.3.14f1' -Quiet)){throw 'WRONG_PROJECT_UNITY_VERSION'}
# Validate the plan before starting Unity.
& $Python (Join-Path $PSScriptRoot 'qa_gate_v2.py') --plan $PlanPath --evidence $EvidencePath --validate-only
if($LASTEXITCODE -ne 0){throw 'INVALID_OR_INCOMPLETE_QA_PLAN_V2'}
$head=(& git -C $project rev-parse HEAD).Trim()
if($head -ne $plan.target_sha){throw 'STALE_TARGET_GIT_HEAD'}
$sourceFiles=@($plan.source_files.PSObject.Properties)
if($sourceFiles.Count -eq 0){throw 'SOURCE_SHA256_MANIFEST_REQUIRED'}
foreach($p in $sourceFiles){
 $name=$p.Name
 if([IO.Path]::IsPathRooted($name) -or $name -match '(^|[/\\])[.][.]([/\\]|$)'){throw 'SOURCE_PATH_ESCAPE'}
 $file=Join-Path $project $name
 if(!(Test-Path -LiteralPath $file -PathType Leaf)){throw "MISSING_PINNED_SOURCE: $name"}
 $actual=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
 if($actual -ne $p.Value){throw "SOURCE_HASH_MISMATCH: $name"}
}
$evidence=[IO.Path]::GetFullPath($EvidencePath)
if($evidence.StartsWith($project + [IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'EVIDENCE_MUST_BE_OUTSIDE_CHECKOUT'}
if(Test-Path -LiteralPath $evidence){throw 'REFUSE_OLD_EVIDENCE_OVERWRITE'}
New-Item -ItemType Directory -Path $evidence | Out-Null
Copy-Item -LiteralPath $PlanPath -Destination (Join-Path $evidence 'plan.json')
function Quote([string]$value){return '"' + $value.Replace('"','\"') + '"'}
function Run([string]$label,[string]$executable,[string[]]$argv){
 $dir=Join-Path $evidence $label
 New-Item -ItemType Directory -Path $dir | Out-Null
 $started=(Get-Date).ToUniversalTime().ToString('o')
 $argsText=($argv|ForEach-Object{Quote $_}) -join ' '
 $process=Start-Process -FilePath $executable -ArgumentList $argsText -PassThru -WindowStyle Hidden
 $timedOut=$false
 try{
  if(!$process.WaitForExit($TimeoutMinutes*60000)){Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue;$timedOut=$true}
  $process.Refresh()
  $code=if($timedOut){-124}else{[int]$process.ExitCode}
 }catch{$code=-125}
 @{stage=$label;exit_code=$code;source_sha=$plan.source_sha;target_sha=$plan.target_sha;unity_version='6000.3.14f1';started_utc=$started;ended_utc=(Get-Date).ToUniversalTime().ToString('o');timeout=$timedOut;binary=$executable;argv=$argv;attribution='B01-60_CONTROLLED_RUNNER'} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $dir 'process.json') -Encoding utf8
 Write-Host "B01-60 $label native exit $code"
 return $code
}
$required=@($plan.required_stages)
$code=Run 'Compile' $editor @('-batchmode','-nographics','-quit','-projectPath',$project,'-logFile',(Join-Path $evidence 'Compile/Editor.log'))
if($code -eq 0){
 foreach($mode in @('EditMode','PlayMode')){
  if($required -notcontains $mode){continue}
  $code=Run $mode $editor @('-batchmode','-nographics','-quit','-projectPath',$project,'-runTests','-testPlatform',$mode,'-testFilter',$plan.test_filter,'-testResults',(Join-Path $evidence "$mode/results.xml"),'-logFile',(Join-Path $evidence "$mode/Editor.log"))
  if($code -ne 0){break}
 }
}
if($code -eq 0 -and $PlayerExe -ne ''){
 $player=(Resolve-Path -LiteralPath $PlayerExe).Path
 if(!$player.StartsWith($project+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'PLAYER_NOT_FROM_DISPOSABLE_CHECKOUT'}
 foreach($row in @(@('Player720','1280','720'),@('Player1080','1920','1080'))){
  if($required -notcontains $row[0]){continue}
  $code=Run $row[0] $player @('-screen-fullscreen','0','-screen-width',$row[1],'-screen-height',$row[2],'-force-d3d11','-qa-output',(Join-Path $evidence $row[0]),'-logFile',(Join-Path $evidence ($row[0]+'/Player.log')))
  if($code -ne 0){break}
 }
}
& $Python (Join-Path $PSScriptRoot 'qa_gate_v2.py') --plan (Join-Path $evidence 'plan.json') --evidence $evidence --out (Join-Path $evidence 'verdict.json')
exit $LASTEXITCODE
