param([ValidateSet('Mapgen','Reactor','Input','Retail')][string]$Mode='Mapgen',[int]$Width=1280,[int]$Height=720,[string]$Executable='')
$ErrorActionPreference='Stop'
$project=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$build=Join-Path $project 'Builds/B01-UNIFIED-RC2'
$source='46b42f62d591ad4aeb9dbfdeda9997044c6b5e1f'
# Two post-build Editor-only fixtures were corrected without changing the Player.
$qaOnly=@('Assets/Tests/Editor/R1PlayIntegrationTests.cs','Assets/Tests/Editor/RRLW34ReviewRegressionTests.cs')
$changed=@(git -C $project diff --name-only $source -- Assets Packages ProjectSettings)
if(@($changed | Where-Object { $_ -notin $qaOnly }).Count){throw 'Production game source differs from release snapshot'}
if(!$Executable){$Executable=Join-Path $build 'RoboRougelike.exe'}
$data=Join-Path (Split-Path $Executable) 'RoboRougelike_Data/Managed/Assembly-CSharp.dll'
$original=Join-Path $build 'RoboRougelike_Data/Managed/Assembly-CSharp.dll'
if((Get-FileHash $data).Hash -ne (Get-FileHash $original).Hash){throw 'Player code differs from release build'}
$run=Join-Path $project ('Artifacts/Local/RC2-Windows-'+$Mode+'-'+$Width+'-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
New-Item -ItemType Directory -Path $run | Out-Null
$args=@('-screen-fullscreen','0','-screen-width',$Width,'-screen-height',$Height,'-force-d3d11','-logFile',('"'+(Join-Path $run 'Player.log')+'"'))
$resultFile=''
if($Mode -eq 'Mapgen'){$args+=@('-rc2-native-output',('"'+$run+'"'));$resultFile='rc2-native-result.json'}
if($Mode -eq 'Reactor'){$args+=@('-rc2-reactor-output',('"'+$run+'"'));$resultFile='rc2-reactor-result.json'}
if($Mode -eq 'Input'){$args+=@('-rc2-input-output',('"'+$run+'"'));$resultFile='rc2-input-result.json'}
$start=[DateTime]::UtcNow
# Real render evidence needs a visible window. No accessibility client attaches.
$p=Start-Process -FilePath $Executable -ArgumentList $args -WorkingDirectory (Split-Path $Executable) -WindowStyle Normal -PassThru
$handle=$p.Handle
@{source=$source;pid=$p.Id;mode=$Mode;executable=$Executable;exeSHA256=(Get-FileHash $Executable).Hash;assemblySHA256=(Get-FileHash $data).Hash;width=$Width;height=$Height;startedUtc=$start.ToString('o');args=$args;visible=$true;accessibilityClientAttached=$false} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $run 'launch.json')
Write-Output "PLAYER $Mode pid=$($p.Id) evidence=$run"
if($Mode -eq 'Retail'){Write-Output 'Interactive retail left open without watchdog';exit 0}
if(!$p.WaitForExit(600000)){throw 'Acceptance exceeded watchdog; process left for inspection'}
$p.Refresh();$code=$p.ExitCode
@{source=$source;mode=$Mode;exitCode=$code;startedUtc=$start.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content (Join-Path $run 'process.json')
Write-Output "PLAYER COMPLETE native=$code evidence=$run"
if($code -ne 0){exit $code}
$result=Get-Content -Raw (Join-Path $run $resultFile) | ConvertFrom-Json
if($result.status -ne 'Passed' -or $result.errors.Count -ne 0){throw 'Actual acceptance result failed or incomplete'}
exit 0
