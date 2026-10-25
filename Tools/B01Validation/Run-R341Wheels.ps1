param([switch]$RemainingOnly)
$ErrorActionPreference='Stop'
$project=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$limit=if($RemainingOnly){8}else{4};$label=if($RemainingOnly){'remaining8'}else{'first4'}
$out=Join-Path $project ('Artifacts/Local/MAPGEN-R341-wheels-'+$label+'-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'));New-Item -ItemType Directory -Path $out | Out-Null
$folder=Join-Path $project 'Builds/MAPGEN-R341-Wheels';$exe=Join-Path $folder 'MapgenQA.exe'
$arguments=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-force-d3d11','-mapgen-wheel-output',('"'+$out+'"'),'-mapgen-case-limit',"$limit",'-logFile',('"'+(Join-Path $out 'Player.log')+'"'))
if($RemainingOnly){$arguments+='-mapgen-remaining-only'}
$started=[DateTime]::UtcNow;$p=Start-Process -FilePath $exe -ArgumentList $arguments -PassThru;$owned=$p.Handle
@{source=(git -C $project rev-parse HEAD).Trim();producer='5be31039411f89f17654b1c332f8692883fa03cc';caseLimit=$limit;remainingOnly=[bool]$RemainingOnly;assemblySha256=(Get-FileHash -LiteralPath (Join-Path $folder 'MapgenQA_Data/Managed/Assembly-CSharp.dll')).Hash;runtimeAssemblySha256=(Get-FileHash -LiteralPath (Join-Path $folder 'MapgenQA_Data/Managed/B01.MAPGEN001.Runtime.dll')).Hash;startedUtc=$started.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'launch.json')
Write-Output "R341 real wheels $label pid=$($p.Id) evidence=$out"
if(!$p.WaitForExit(1200000)){Stop-Process -Id $p.Id -Force;throw 'Owned real wheel QA timeout; no PASS'}
$p.Refresh();@{exitCode=$p.ExitCode;finishedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'process.json')
$data=Get-Content -Raw -LiteralPath (Join-Path $out 'wheel-trials.json') | ConvertFrom-Json
Write-Output "Actual native exit=$($p.ExitCode); rows=$($data.trials.Count); result=$($data.status)"
if($p.ExitCode -ne 0){exit $p.ExitCode}
if($data.trials.Count -ne $limit -or $data.status -ne 'PASS'){exit 2}
exit 0
