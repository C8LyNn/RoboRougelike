param(
    [ValidateSet('EditMode','PlayMode','Build','All')][string]$Task='All',
    [string]$UnityPath='C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe',
    [string]$Python='python'
)
$ErrorActionPreference='Stop'
$project=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if (!(Test-Path -LiteralPath $UnityPath)) { throw 'Install Unity 6000.3.14f1 with Windows Build Support, or pass -UnityPath.' }
if ((Get-Content -Raw (Join-Path $project 'ProjectSettings/ProjectVersion.txt')) -notmatch 'm_EditorVersion: 6000\.3\.14f1\r?\n') { throw 'Unexpected project Unity version.' }
if (Test-Path (Join-Path $project 'Temp/UnityLockfile')) { throw 'Close the Editor for this checkout before running CI.' }
$run=Join-Path $project ('Artifacts/Local/'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')+'-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run | Out-Null
$tasks=if($Task -eq 'All'){@('EditMode','PlayMode','Build')}else{@($Task)}
foreach($item in $tasks){
    $log=Join-Path $run ($item+'.log')
    $arguments=@('-batchmode','-nographics','-projectPath',('"'+$project+'"'),'-logFile',('"'+$log+'"'))
    if($item -eq 'Build'){$arguments+=@('-quit','-executeMethod','MOVSeriesBuild.BuildAll')}
    else{
        $xml=Join-Path $run ($item+'.xml')
        $arguments+=@('-runTests','-testPlatform',$item,'-testResults',('"'+$xml+'"'))
    }
    Write-Output "Starting Unity $item. Evidence: $run"
    $started=[DateTime]::UtcNow
    $process=Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if(!$process.WaitForExit(5400000)){
        Stop-Process -Id $process.Id -Force
        throw "Unity $item exceeded 90 minutes; owned process stopped. See $log"
    }
    $process.Refresh();$code=$process.ExitCode
    $code | Set-Content -LiteralPath (Join-Path $run ($item+'.exitcode'))
    @{task=$item;startedUtc=$started.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o');exitCode=$code} | ConvertTo-Json | Set-Content (Join-Path $run ($item+'-process.json'))
    if($code -ne 0){throw "Unity $item failed: exit $code; see $log"}
    if($item -ne 'Build'){
        & $Python (Join-Path $PSScriptRoot 'check_results.py') $xml --exit-code $code
        if($LASTEXITCODE -ne 0){throw "Invalid or failed $item results: $xml"}
    }else{
        $buildLog=Get-Content -Raw -LiteralPath $log
        foreach($mode in @('False','True')){
            if($buildLog -notmatch "\[MOV SERIES BUILD\] qa=$mode result=Succeeded .*errors=0") {throw "Missing successful build summary qa=$mode"}
        }
        foreach($folder in @('MOV001-009-Windows','MOVSeries-QA-Windows')){
            if(!(Test-Path (Join-Path $project "Builds/$folder/RoboRougelike.exe"))){throw "Missing $folder player"}
        }
    }
    Write-Output "Unity $item PASS; native exit $code"
}
Write-Output "Completed. Evidence: $run"
