param([ValidateSet('Menu','Garage','Combat','Transition','Return','RangeLong','Manual')][string]$Mode='Menu',[int]$Width=1280,[int]$Height=720,[string]$Executable='',[string]$Label='')
$ErrorActionPreference='Stop'
$project=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if(!$Executable){$Executable=Join-Path $project 'Builds/FULL-MERGE270/RoboRougelike.exe'}
$buildReceipt=Get-ChildItem (Join-Path $project 'Artifacts/Local/FM270-*/Build-process.json') | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if(!$buildReceipt){throw 'Missing native build receipt'}
$source=(Get-Content -Raw $buildReceipt.FullName | ConvertFrom-Json).source
if(@(git -C $project diff --name-only $source -- Assets Packages ProjectSettings).Count){throw 'Source differs from built version'}
$stamp=[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$run=Join-Path $project ('Artifacts/Local/EXIT273-Windows-'+$Mode+'-'+$Width+'-'+$Label+'-'+$stamp)
New-Item -ItemType Directory -Path $run | Out-Null
$arguments=@('-screen-fullscreen','0','-screen-width',$Width,'-screen-height',$Height,'-force-d3d11','-logFile',('"'+(Join-Path $run 'Player.log')+'"'))
if($Mode -ne 'Manual'){$arguments+=@('-exit273-output',('"'+$run+'"'),'-exit273-mode',$Mode)}
$start=[DateTime]::UtcNow
$p=Start-Process $Executable -ArgumentList $arguments -WorkingDirectory (Split-Path $Executable) -WindowStyle Normal -PassThru
$handle=$p.Handle
@{source=$source;pid=$p.Id;mode=$Mode;label=$Label;executable=$Executable;width=$Width;height=$Height;startedUtc=$start.ToString('o');arguments=$arguments;visible=$true;uiAutomationAttached=$false} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $run 'launch.json')
Write-Output "PLAYER $Mode pid=$($p.Id) evidence=$run"
# A timeout retains the owned Player for diagnosis; it never converts a kill into a normal exit.
$timeout=if($Mode -eq 'Manual'){1200000}else{300000}
if(!$p.WaitForExit($timeout)){@{status='NOT_COMPLETE';source=$source;pid=$p.Id;reason='Process did not exit within observation interval; retained'} | ConvertTo-Json | Set-Content (Join-Path $run 'timeout.json');throw 'Player did not exit; retained'}
$p.Refresh();$code=[int]$p.ExitCode
@{source=$source;mode=$Mode;label=$Label;pid=$p.Id;exitCode=$code;exitUnsigned=([long]$code -band 0xffffffffL);exitHex=('0x{0:X8}' -f ([long]$code -band 0xffffffffL));startedUtc=$start.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content (Join-Path $run 'process.json')
Write-Output "PLAYER EXIT $code evidence=$run"
if($code -ne 0){exit 1}
if($Mode -ne 'Manual'){
 $result=Get-Content -Raw (Join-Path $run 'exit-result.json') | ConvertFrom-Json
 if($result.status -ne 'Passed' -or !$result.applicationQuitReached){throw 'Player acceptance incomplete or failed'}
}
