param([string]$Project=(Get-Location).Path,[ValidateSet('Base','Experimental','Targeted')][string]$Variant='Base',[string]$Filter='',[ValidateSet('None','Both','EditMode','PlayMode')][string]$Modes='Both',[switch]$SkipBuild,[switch]$FilterEditOnly)
$ErrorActionPreference='Stop'
$unity='C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe'
$run=Join-Path $Project ('Artifacts/Local/RC1-'+$Variant+'-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
New-Item -ItemType Directory -Path $run | Out-Null
$source=(git -C $Project rev-parse HEAD).Trim()
if((Get-Content -Raw -LiteralPath (Join-Path $Project 'ProjectSettings/ProjectVersion.txt')) -notmatch 'm_EditorVersion: 6000\.3\.14f1\r?\n'){throw 'Unexpected Unity version'}
if($Variant -eq 'Base' -and $source -ne '4c9236a4cdc27814ea2e7679872ec4a524150cc8'){throw 'BASE must be built from exact OBJ210 source checkout'}
if($Variant -ne 'Targeted'){
 git -C $Project diff --quiet HEAD -- Assets Packages ProjectSettings
 if($LASTEXITCODE -ne 0){throw 'Dirty game source cannot be labeled with HEAD'}
 if(git -C $Project ls-files --others --exclude-standard Assets){throw 'Untracked Unity assets: freeze source first'}
}
function Invoke-Native([string]$Phase,[string[]]$Extra){
 $started=[DateTime]::UtcNow
 $args=@('-batchmode','-nographics','-projectPath',('"'+$Project+'"'),'-logFile',('"'+(Join-Path $run ($Phase+'.log'))+'"'))+$Extra
 Write-Output "START $Phase source=$source evidence=$run"
 $p=Start-Process -FilePath $unity -ArgumentList $args -WindowStyle Hidden -PassThru
 $owned=$p.Handle
 # Match Tools/CI/Run-Unity.ps1's process watchdog, not a test assertion/time cap.
 if(!$p.WaitForExit(5400000)){Stop-Process -Id $p.Id;throw "Owned Unity exceeded repository CI 90 minutes: $Phase"}
 $p.Refresh();$code=$p.ExitCode
 @{phase=$Phase;source=$source;exitCode=$code;startedUtc=$started.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o');arguments=$Extra} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $run ($Phase+'-process.json'))
 if($code -ne 0){throw "Native $Phase failed exit=$code; see $run"}
 Write-Output "END $Phase native=$code"
}
Invoke-Native 'Compile' @('-quit')
if($Variant -eq 'Experimental' -and !$SkipBuild){
 Invoke-Native 'Build' @('-quit','-executeMethod','B01RC1Build.BuildAll')
 @{source=$source;evidence=$run} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Project 'Builds/RC1-SOURCE.json')
}
if($Variant -eq 'Experimental' -and $SkipBuild){
 $built=Get-Content -Raw -LiteralPath (Join-Path $Project 'Builds/RC1-SOURCE.json') | ConvertFrom-Json
 if($built.source -ne $source){throw 'Build snapshot differs from current source'}
}
$testModes=if($Modes -eq 'None'){@()}elseif($Modes -eq 'Both'){@('EditMode','PlayMode')}else{@($Modes)}
foreach($mode in $testModes){
 $xml=Join-Path $run ($mode+'.xml')
 $extra=@('-runTests','-testPlatform',$mode,'-testResults',('"'+$xml+'"'))
 if($Filter -and ($mode -eq 'EditMode' -or ($Variant -ne 'Base' -and !$FilterEditOnly))){$extra+=@('-testFilter',$Filter)}
 Invoke-Native $mode $extra
 if(!(Test-Path -LiteralPath $xml)){throw 'Missing actual NUnit XML'}
 [xml]$result=Get-Content -LiteralPath $xml
 $node=$result.'test-run'
 Write-Output "$mode actual result=$($node.result) total=$($node.total) passed=$($node.passed) failed=$($node.failed) skipped=$($node.skipped)"
 if([int]$node.failed -ne 0 -or [int]$node.total -eq 0){throw 'Native NUnit failure/empty result'}
}
if($Variant -eq 'Base'){Invoke-Native 'Build' @('-quit','-executeMethod','R6Build.BuildRelease')}
Write-Output "COMPLETE $Variant $run"
