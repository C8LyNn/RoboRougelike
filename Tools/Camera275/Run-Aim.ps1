param([Parameter(Mandatory=$true)][string]$Receipt,[string]$Stage='Candidate')
$ErrorActionPreference='Stop'
$project=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$build=Get-Content -Raw $Receipt | ConvertFrom-Json
$exe=$build.exe
if((Get-FileHash (Join-Path (Split-Path $exe) 'RoboRougelike_Data/Managed/Assembly-CSharp.dll')).Hash.ToLower() -ne $build.assemblySHA256){throw 'Pinned Player assembly changed'}
$run=Join-Path $project ('Artifacts/Local/CAM275-Aim-'+$Stage+'-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
New-Item -ItemType Directory -Path $run | Out-Null
$arguments=@('-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-force-d3d11','-logFile',('"'+(Join-Path $run 'Player.log')+'"'),'-camera275-aim-output',('"'+$run+'"'))
$start=[DateTime]::UtcNow
$p=Start-Process $exe -ArgumentList $arguments -WorkingDirectory (Split-Path $exe) -WindowStyle Normal -PassThru
$handle=$p.Handle
@{source=$build.source;pid=$p.Id;exe=$exe;assemblySHA256=$build.assemblySHA256;arguments=$arguments;startedUtc=$start.ToString('o');method='Native mouse/LMB on isolated stationary canonical vehicle; actual projectile HP damage. Not human combat.'} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $run 'launch.json')
Write-Output "CAM275 AIM pid=$($p.Id) evidence=$run"
if(!$p.WaitForExit(360000)){throw 'Owned aim fixture exceeded bounded observation; retained for diagnosis'}
$p.Refresh();if($null -eq $p.ExitCode){throw 'Native exit code unavailable'};$code=[int]$p.ExitCode
@{source=$build.source;pid=$p.Id;exitCode=$code;exitHex=('0x{0:X8}' -f ([long]$code -band 0xffffffffL));startedUtc=$start.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content (Join-Path $run 'process.json')
Write-Output "CAM275 AIM EXIT $code evidence=$run"
if($code -ne 0){exit 1}
$result=Get-Content -Raw (Join-Path $run 'aim-result.json') | ConvertFrom-Json
if($result.status -ne 'Passed'){throw 'Native aim fixture failed'}
exit 0
