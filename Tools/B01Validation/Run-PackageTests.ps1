param([string]$Filter='B01.CAMERA001.Tests',[ValidateSet('EditMode','PlayMode','Both')][string]$Mode='Both',[string]$Label='CAMERA001-original',[string]$Python='python')
$ErrorActionPreference='Stop'
$project=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if(Test-Path (Join-Path $project 'Temp/UnityLockfile')){throw 'An Editor owns this checkout'}
$out=Join-Path $project ('Artifacts/Local/'+$Label+'-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'));New-Item -ItemType Directory -Path $out | Out-Null
$modes=if($Mode -eq 'Both'){@('EditMode','PlayMode')}else{@($Mode)};$aggregate=0
foreach($item in $modes){
 $xml=Join-Path $out ($item+'.xml');$log=Join-Path $out ($item+'.log');$started=[DateTime]::UtcNow
 $arguments=@('-batchmode','-nographics','-projectPath',('"'+$project+'"'),'-runTests','-testPlatform',$item,'-testFilter',$Filter,'-testResults',('"'+$xml+'"'),'-logFile',('"'+$log+'"'))
 Write-Output "Native $item $Filter evidence=$out"
 $p=Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/6000.3.14f1/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
 $ownedHandle=$p.Handle
 if(!$p.WaitForExit(1200000)){Stop-Process -Id $p.Id -Force;throw 'Owned package test exceeded20 minutes'}
 $p.Refresh();$code=$p.ExitCode
 @{mode=$item;filter=$Filter;source=(git -C $project rev-parse HEAD).Trim();exitCode=$code;startedUtc=$started.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out ($item+'-process.json'))
 if(Test-Path -LiteralPath $xml){
  & $Python (Join-Path $project 'Tools/CI/check_results.py') $xml --exit-code $code
  if($LASTEXITCODE -ne 0){$aggregate=2}
 }else{Write-Output 'No XML: not a passing test run';$aggregate=2}
 Write-Output "Actual $item native exit=$code"
 if($code -ne 0){$aggregate=2}
 if(!(Test-Path -LiteralPath $xml)){break}
}
exit $aggregate
