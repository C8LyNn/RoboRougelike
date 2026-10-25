param([Parameter(Mandatory)][string]$Exe,[Parameter(Mandatory)][string]$Output,[int]$Width=1280,[int]$Height=720,[Parameter(Mandatory)][string]$Source,[switch]$Interactive,[string[]]$Additional=@())
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $Output){throw 'Never overwrite Player evidence'}
New-Item -ItemType Directory -Path $Output | Out-Null
$Output=(Resolve-Path -LiteralPath $Output).Path
$Exe=(Resolve-Path -LiteralPath $Exe).Path
$log=Join-Path $Output 'Player.log'
$args=@('-screen-fullscreen','0','-screen-width',"$Width",'-screen-height',"$Height",'-force-d3d11','-logFile',('"'+$log+'"'))+$Additional
$started=[DateTime]::UtcNow
@{exe=$Exe;source=$Source;exeSha256=(Get-FileHash -LiteralPath $Exe -Algorithm SHA256).Hash;width=$Width;height=$Height;interactive=$Interactive.IsPresent;additional=$Additional;startedUtc=$started.ToString('o')} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Output 'launch.json')
$style=if($Interactive){'Normal'}else{'Hidden'}
$p=Start-Process -FilePath $Exe -WorkingDirectory (Split-Path -Parent $Exe) -ArgumentList $args -WindowStyle $style -PassThru
$owned=$p.Handle
Write-Output "Player PID=$($p.Id) output=$Output"
$timedOut=$false
if($Interactive){$p.WaitForExit()}else{$timedOut=!$p.WaitForExit(240000)}
if($timedOut){Stop-Process -Id $p.Id;$p.WaitForExit()}
$p.Refresh();$code=$p.ExitCode
@{exitCode=$code;pid=$p.Id;timedOut=$timedOut;startedUtc=$started.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Output 'process.json')
Write-Output "Player actual exit=$code timeout=$timedOut"
if($timedOut -or $code -ne 0){exit 1}
exit 0
