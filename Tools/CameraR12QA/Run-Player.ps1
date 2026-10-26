$ErrorActionPreference='Stop'
$project=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$source=(git -C $project rev-parse HEAD).Trim()
$run=Join-Path $project ('Artifacts/Local/CameraR12-Player-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
New-Item -ItemType Directory -Path $run | Out-Null
$exe=Join-Path $project 'Builds/CAMERA-R12-QA/CameraR12QA.exe'
$start=[DateTime]::UtcNow
$args=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile',('"'+(Join-Path $run 'Player.log')+'"'),'-camera-qa-output',('"'+$run+'"'))
# Native render evidence requires a visible window; no UIA/accessibility client attaches.
$p=Start-Process -FilePath $exe -ArgumentList $args -WorkingDirectory (Split-Path $exe) -WindowStyle Normal -PassThru
$handle=$p.Handle
@{source=$source;producer='48581e49a878eca376674551017532bf17fd35b2';startedUtc=$start.ToString('o');pid=$p.Id;executable=$exe;exeSHA256=(Get-FileHash $exe).Hash;assemblySHA256=(Get-FileHash (Join-Path (Split-Path $exe) 'CameraR12QA_Data/Managed/Assembly-CSharp.dll')).Hash;args=$args;visible=$true;accessibilityClientAttached=$false} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $run 'launch.json')
Write-Output "PLAYER pid=$($p.Id) source=$source evidence=$run"
if(!$p.WaitForExit(600000)){throw 'QA exceeded 10 minute harness watchdog; process left for inspection, not killed'}
$p.Refresh();$code=$p.ExitCode
@{source=$source;producer='48581e49a878eca376674551017532bf17fd35b2';exitCode=$code;startedUtc=$start.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content (Join-Path $run 'process.json')
Write-Output "PLAYER COMPLETE nativeExit=$code evidence=$run"
exit 0
