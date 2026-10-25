param([int]$Limit=99,[string]$Label='MAPGEN-R32-wheels',[switch]$RampProbe,[switch]$MaterialOverride)
$ErrorActionPreference='Stop';$project=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$out=Join-Path $project ('Artifacts/Local/'+$Label+'-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'));New-Item -ItemType Directory -Path $out | Out-Null
$exe=Join-Path $project 'Builds/MAPGEN-R32-QA/MapgenNativeQA.exe'
$started=[DateTime]::UtcNow;$head=(git -C $project rev-parse HEAD).Trim()
$arguments=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-force-d3d11','-mapgen-wheel-output',('"'+$out+'"'),'-mapgen-case-limit',"$Limit",'-logFile',('"'+(Join-Path $out 'Player.log')+'"'))
if($RampProbe){$arguments+='-mapgen-ramp-probe'}
if($MaterialOverride){$arguments+='-mapgen-material-override'}
$p=Start-Process -FilePath $exe -ArgumentList $arguments -PassThru
$owned=$p.Handle
@{pid=$p.Id;exe=$exe;source=$head;producerSource='003bced54480d509ffc0d49d26daf87160a8d609';startedUtc=$started.ToString('o');caseLimit=$Limit;rampProbe=[bool]$RampProbe;materialOverride=[bool]$MaterialOverride;assemblySha256=(Get-FileHash -LiteralPath (Join-Path $project 'Builds/MAPGEN-R32-QA/MapgenNativeQA_Data/Managed/Assembly-CSharp.dll')).Hash} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'launch.json')
Write-Output "Actual wheel QA pid=$($p.Id) evidence=$out"
if(!$p.WaitForExit(1800000)){Stop-Process -Id $p.Id -Force;throw 'Owned wheel QA exceeded30minutes; no PASS'}
$p.Refresh();@{exitCode=$p.ExitCode;finishedUtc=[DateTime]::UtcNow.ToString('o');pid=$p.Id} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'process.json')
Write-Output "Wheel QA actual native exit=$($p.ExitCode) evidence=$out";exit $p.ExitCode
