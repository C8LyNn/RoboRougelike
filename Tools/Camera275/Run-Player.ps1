param([Parameter(Mandatory=$true)][string]$Receipt,[string]$Vehicle='Standard',[int]$Fps=60,[int]$Width=1280,[int]$Height=720,[string]$Stage='Baseline')
$ErrorActionPreference='Stop'
$project=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$build=Get-Content -Raw $Receipt | ConvertFrom-Json
$exe=$build.exe
if((Get-FileHash (Join-Path (Split-Path $exe) 'RoboRougelike_Data/Managed/Assembly-CSharp.dll')).Hash.ToLower() -ne $build.assemblySHA256){throw 'Pinned Player assembly changed'}
$run=Join-Path $project ('Artifacts/Local/CAM275-Windows-'+$Stage+'-'+$Vehicle+'-'+$Fps+'-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
New-Item -ItemType Directory -Path $run | Out-Null
$arguments=@('-screen-fullscreen','0','-screen-width',$Width,'-screen-height',$Height,'-force-d3d11','-logFile',('"'+(Join-Path $run 'Player.log')+'"'),'-camera275-output',('"'+$run+'"'),'-camera275-vehicle',$Vehicle,'-camera275-fps',$Fps)
$start=[DateTime]::UtcNow
$p=Start-Process $exe -ArgumentList $arguments -WorkingDirectory (Split-Path $exe) -WindowStyle Normal -PassThru
$handle=$p.Handle
@{source=$build.source;pid=$p.Id;exe=$exe;assemblySHA256=$build.assemblySHA256;vehicle=$Vehicle;fps=$Fps;width=$Width;height=$Height;stage=$Stage;startedUtc=$start.ToString('o');arguments=$arguments;method='Actual Windows Player, native queued input and dynamic R3/R6; not human play'} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $run 'launch.json')
Write-Output "CAM275 PLAYER $Stage/$Vehicle/$Fps pid=$($p.Id) evidence=$run"
if(!$p.WaitForExit(180000)){throw 'Owned diagnostic Player exceeded bounded observation; retained, no normal process kill'}
$p.Refresh();if($null -eq $p.ExitCode){throw 'Native exit code unavailable'};$code=[int]$p.ExitCode
@{source=$build.source;pid=$p.Id;exitCode=$code;exitHex=('0x{0:X8}' -f ([long]$code -band 0xffffffffL));startedUtc=$start.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content (Join-Path $run 'process.json')
Write-Output "CAM275 PLAYER EXIT $code evidence=$run"
if($code -ne 0){exit 1}
$result=Get-Content -Raw (Join-Path $run 'camera-result.json') | ConvertFrom-Json
if($result.status -ne 'Passed'){throw 'Native drive fixture failed'}
