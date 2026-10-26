param([ValidateSet('Race','TimeAttack','Escape','Survival','Boss','Loop','Camera','Mapgen','Reactor','Input','Retail')][string]$Mode='Race',[string]$Vehicle='Light',[int]$Width=1280,[int]$Height=720,[string]$Executable='')
$ErrorActionPreference='Stop'
$project=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$build=Join-Path $project 'Builds/FULL-MERGE270'
$receipt=Get-ChildItem (Join-Path $project 'Artifacts/Local/FM270-*/Build-process.json') | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if(!$receipt){throw 'No actual build receipt'}
$source=(Get-Content -Raw $receipt.FullName | ConvertFrom-Json).source
if(@(git -C $project diff --name-only $source -- Assets Packages ProjectSettings).Count){throw 'Unity source differs from built source'}
if(!$Executable){$Executable=Join-Path $build 'RoboRougelike.exe'}
$data=Join-Path (Split-Path $Executable) 'RoboRougelike_Data/Managed/Assembly-CSharp.dll'
if((Get-FileHash $data).Hash -ne (Get-FileHash (Join-Path $build 'RoboRougelike_Data/Managed/Assembly-CSharp.dll')).Hash){throw 'Extracted assembly differs'}
$run=Join-Path $project ('Artifacts/Local/FM270-Windows-'+$Mode+'-'+$Vehicle+'-'+$Width+'-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
New-Item -ItemType Directory -Path $run | Out-Null
$args=@('-screen-fullscreen','0','-screen-width',$Width,'-screen-height',$Height,'-force-d3d11','-logFile',('"'+(Join-Path $run 'Player.log')+'"'))
$resultFile='result.json'
if($Mode -eq 'Mapgen'){$args+=@('-rc2-native-output',('"'+$run+'"'));$resultFile='rc2-native-result.json'}
elseif($Mode -eq 'Reactor'){$args+=@('-rc2-reactor-output',('"'+$run+'"'));$resultFile='rc2-reactor-result.json'}
elseif($Mode -eq 'Input'){$args+=@('-rc2-input-output',('"'+$run+'"'));$resultFile='rc2-input-result.json'}
elseif($Mode -ne 'Retail'){$args+=@('-fm270-output',('"'+$run+'"'),'-fm270-mode',$Mode,'-fm270-vehicle',$Vehicle)}
$start=[DateTime]::UtcNow
$p=Start-Process -FilePath $Executable -ArgumentList $args -WorkingDirectory (Split-Path $Executable) -WindowStyle Normal -PassThru
$handle=$p.Handle
@{source=$source;pid=$p.Id;mode=$Mode;vehicle=$Vehicle;executable=$Executable;exeSHA256=(Get-FileHash $Executable).Hash;assemblySHA256=(Get-FileHash $data).Hash;width=$Width;height=$Height;startedUtc=$start.ToString('o');args=$args;visible=$true;accessibilityClientAttached=$false} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $run 'launch.json')
Write-Output "PLAYER $Mode/$Vehicle pid=$($p.Id) evidence=$run"
if($Mode -eq 'Retail'){Write-Output 'Interactive retail left open';exit 0}
if(!$p.WaitForExit(660000)){throw 'Finite acceptance exceeded watchdog; owned process retained for inspection'}
$p.Refresh();$code=$p.ExitCode
@{source=$source;mode=$Mode;vehicle=$Vehicle;exitCode=$code;startedUtc=$start.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content (Join-Path $run 'process.json')
Write-Output "PLAYER COMPLETE native=$code evidence=$run"
if($code -ne 0){exit $code}
$result=Get-Content -Raw (Join-Path $run $resultFile) | ConvertFrom-Json
if($result.status -ne 'Passed' -or $result.errors.Count -ne 0){throw 'Actual acceptance failed/incomplete'}
exit 0
