param([string]$Stage='Targeted',[string]$Filter='',[ValidateSet('None','Both','EditMode','PlayMode')][string]$Modes='Both',[switch]$Compile,[switch]$Build)
$ErrorActionPreference='Stop'
$project=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$unity='C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe'
$source=(git -C $project rev-parse HEAD).Trim()
$run=Join-Path $project ('Artifacts/Local/FM270-'+$Stage+'-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
New-Item -ItemType Directory -Path $run | Out-Null
git -C $project diff --quiet HEAD -- Assets Packages ProjectSettings
if($LASTEXITCODE -ne 0 -or (git -C $project ls-files --others --exclude-standard Assets)){throw 'Freeze all Unity source before native execution'}
if((Get-Content -Raw (Join-Path $project 'ProjectSettings/ProjectVersion.txt')) -notmatch 'm_EditorVersion: 6000\.3\.14f1\r?\n'){throw 'Unexpected Unity version'}
$script:failed=$false
function Invoke-Phase([string]$phase,[string[]]$extra){
 $start=[DateTime]::UtcNow
 $args=@('-batchmode','-nographics','-projectPath',('"'+$project+'"'),'-logFile',('"'+(Join-Path $run ($phase+'.log'))+'"'))+$extra
 Write-Output "START $Stage/$phase source=$source evidence=$run"
 $p=Start-Process $unity -ArgumentList $args -WindowStyle Hidden -PassThru;$handle=$p.Handle
 if(!$p.WaitForExit(5400000)){Stop-Process -Id $p.Id;throw 'Owned Unity exceeded repository90min process watchdog'}
 $p.Refresh();$code=$p.ExitCode
 @{source=$source;phase=$phase;stage=$Stage;exitCode=$code;startedUtc=$start.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o');arguments=$extra} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $run ($phase+'-process.json'))
 Write-Output "END $Stage/$phase native=$code"
 if($phase -eq 'Compile' -and $code -ne 0){throw 'Compilation failed: later phases stopped'}
 if($code -ne 0){$script:failed=$true}
}
if($Compile){Invoke-Phase 'Compile' @('-quit')}
if($Build){Invoke-Phase 'Build' @('-quit','-executeMethod','B01RC2Build.BuildRelease')}
$testModes=if($Modes -eq 'None'){@()}elseif($Modes -eq 'Both'){@('EditMode','PlayMode')}else{@($Modes)}
foreach($mode in $testModes){
 $xml=Join-Path $run ($mode+'.xml');$extra=@('-runTests','-testPlatform',$mode,'-testResults',('"'+$xml+'"'))
 if($Filter){$extra+=@('-testFilter',('"'+$Filter+'"'))}
 Invoke-Phase $mode $extra
 if(!(Test-Path $xml)){$script:failed=$true;Write-Output 'Missing actual NUnit XML';continue}
 [xml]$result=Get-Content $xml;$n=$result.'test-run';Write-Output "$mode total=$($n.total) passed=$($n.passed) failed=$($n.failed) skipped=$($n.skipped)"
 if([int]$n.total -eq 0 -or [int]$n.failed -ne 0 -or [int]$n.skipped -ne 0){$script:failed=$true}
}
Write-Output "COMPLETE $run"
if($script:failed){exit 2}
exit 0
