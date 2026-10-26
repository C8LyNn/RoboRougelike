param([ValidateSet('Tests','Build')][string]$Mode='Tests')
$ErrorActionPreference='Stop'
$project=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$unity='C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe'
$source=(git -C $project rev-parse HEAD).Trim()
$run=Join-Path $project ('Artifacts/Local/CameraR12-'+$Mode+'-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
New-Item -ItemType Directory -Path $run | Out-Null
git -C $project diff --quiet HEAD -- Assets Packages ProjectSettings
if($LASTEXITCODE -ne 0){throw 'Freeze all Unity sources first'}
if(git -C $project ls-files --others --exclude-standard Assets){throw 'Untracked Unity assets'}
function Invoke-Phase([string]$phase,[string[]]$extra){
 $start=[DateTime]::UtcNow
 $args=@('-batchmode','-nographics','-projectPath',('"'+$project+'"'),'-logFile',('"'+(Join-Path $run ($phase+'.log'))+'"'))+$extra
 Write-Output "START $phase source=$source evidence=$run"
 $p=Start-Process -FilePath $unity -ArgumentList $args -WindowStyle Hidden -PassThru
 $handle=$p.Handle
 if(!$p.WaitForExit(5400000)){Stop-Process -Id $p.Id;throw 'Owned Unity exceeded repository CI watchdog of 90 min'}
 $p.Refresh();$code=$p.ExitCode
 @{source=$source;producer='48581e49a878eca376674551017532bf17fd35b2';phase=$phase;exitCode=$code;startedUtc=$start.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o');arguments=$extra} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $run ($phase+'-process.json'))
 Write-Output "END $phase nativeExit=$code"
 if($phase -eq 'Compile' -and $code -ne 0){throw 'Compilation failed: stop later phases'}
}
if($Mode -eq 'Tests'){
 Invoke-Phase 'Compile' @('-quit')
 foreach($phase in @('EditMode','PlayMode')){
  $xml=Join-Path $run ($phase+'.xml')
  Invoke-Phase $phase @('-runTests','-testPlatform',$phase,'-testFilter','B01.CAMERA001.Tests','-testResults',('"'+$xml+'"'))
  if(Test-Path $xml){[xml]$result=Get-Content $xml;$n=$result.'test-run';Write-Output "$phase total=$($n.total) passed=$($n.passed) failed=$($n.failed) skipped=$($n.skipped)"}else{Write-Output "$phase XML missing"}
 }
}else{Invoke-Phase 'Build' @('-quit','-executeMethod','CameraR12QABuild.Build')}
Write-Output "COMPLETE $run"
exit 0
