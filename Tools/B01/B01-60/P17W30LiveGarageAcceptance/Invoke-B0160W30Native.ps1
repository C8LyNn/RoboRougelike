#requires -Version 5.1
<# B01-60 W30: REAL Unity native five ORIGINAL current-game suites.
  Requires exact #877 G0, clean isolated manually created owner disposable
  worktree and explicit operator opt-in. NOT a Windows Player builder.
#>
[CmdletBinding()]
param(
 [string]$RepoPath='.',
 [string]$ScratchParent='',
 [string]$UnityPath='',
 [switch]$Run,
 [switch]$SourceG0Approved,
 [switch]$DisposableCheckoutAcknowledged
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$rc=2
function Need([bool]$ok,[string]$why){
 if(-not $ok){throw "B0160_W30_HOLD: $why"}
}
function LocalGit([string[]]$gitArgs){
 $result=@(& git @gitArgs 2>&1)
 Need ($LASTEXITCODE -eq 0) 'local git failed, no auto-fetch'
 return ($result -join [Environment]::NewLine).Trim()
}
function IsUnder([string]$child,[string]$parent){
 $c=[IO.Path]::GetFullPath($child).TrimEnd('\','/')+'\'
 $p=[IO.Path]::GetFullPath($parent).TrimEnd('\','/')+'\'
 return $c.StartsWith($p,[StringComparison]::OrdinalIgnoreCase)
}
try{
 Need ($env:OS -eq 'Windows_NT') 'native Unity Editor must be operated on owner Windows'
 $null=Get-Command git -ErrorAction Stop
 $null=Get-Command python -ErrorAction Stop
 $repo=(Resolve-Path -LiteralPath $RepoPath -ErrorAction Stop).Path
 $real=LocalGit @('-C',$repo,'rev-parse','--show-toplevel')
 Need ([IO.Path]::GetFullPath($real).TrimEnd('\','/') -ieq
  [IO.Path]::GetFullPath($repo).TrimEnd('\','/')) 'RepoPath not exact checkout root'
 $head=LocalGit @('-C',$repo,'rev-parse','HEAD')
 $base='a0de194fd382830fab4b7d013d204b0697b25078'
 Need ((LocalGit @('-C',$repo,'merge-base',$base,$head)) -eq $base) 'incorrect actual #877 lineage'
 $dirty=LocalGit @('-C',$repo,'status','--porcelain','--untracked-files=normal')
 Need ([string]::IsNullOrWhiteSpace($dirty)) 'dirty source checkout; refuse mutation'
 $mine='Tools/B01/B01-60/P17W30LiveGarageAcceptance/Invoke-B0160W30Native.ps1'
 $verify='Tools/B01/B01-60/P17W30LiveGarageAcceptance/verify.py'
 Need ([IO.Path]::GetFullPath((Join-Path $repo $mine)) -ieq [IO.Path]::GetFullPath($PSCommandPath)) 'wrong operator file'
 foreach($file in @($mine,$verify,'Assets/Tests/Editor/W30B0160GarageDynamicGateTests.cs')){
  Need ((LocalGit @('-C',$repo,'hash-object','--',$file)) -eq
   (LocalGit @('-C',$repo,'rev-parse',"HEAD:$file"))) "dirty original QA source $file"
 }
 Write-Host "B0160_W30_GAME_877=$base CURRENT_QA_HEAD=$head"
 & python (Join-Path $repo $verify)
 Need ($LASTEXITCODE -eq 0) 'exact real game source gate failed'
 if(-not $Run) {
  $rc=0
  Write-Host 'B0160_W30_REAL_GAME_SOURCE_ONLY_NATIVE_UNITY_NOT_RUN'
 } else {
  Need $SourceG0Approved.IsPresent 'independent exact #873/#877 + W30 G0 not approved'
  Need $DisposableCheckoutAcknowledged.IsPresent 'explicit disposable checkout acknowledgment absent'
  Need ($repo -match 'B0160-W30-Disposable') 'refuse running inside Codex/normal checkout'
  if([string]::IsNullOrWhiteSpace($ScratchParent)){$ScratchParent=$env:TEMP}
  $scratch=(Resolve-Path -LiteralPath $ScratchParent -ErrorAction Stop).Path
  Need (-not $scratch.StartsWith('\\')) 'no network share scratch'
  Need (-not (IsUnder $scratch $repo) -and -not (IsUnder $repo $scratch)) 'scratch overlaps repo'
  $walk=Get-Item -LiteralPath $scratch -ErrorAction Stop
  while($null -ne $walk){
   Need (-not [bool]($walk.Attributes -band [IO.FileAttributes]::ReparsePoint)) 'linked scratch parent denied'
   $walk=$walk.Parent
  }
  if([string]::IsNullOrWhiteSpace($UnityPath)){
   $UnityPath=Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.3.14f1/Editor/Unity.exe'
  }
  Need (Test-Path -LiteralPath $UnityPath -PathType Leaf) 'Unity Hub 6000.3.14f1 missing'
  $editor=(Resolve-Path -LiteralPath $UnityPath).Path
  Need ($editor -match '[\\/]6000\.3\.14f1[\\/]Editor[\\/]Unity\.exe$') 'wrong Unity version or path'
  Need (-not [bool]((Get-Item -LiteralPath $editor).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'linked Unity executable denied'
  Write-Host "B0160_W30_EDITOR_SHA256=$((Get-FileHash -LiteralPath $editor -Algorithm SHA256).Hash.ToLowerInvariant())"
  Write-Host 'REAL Unity 6000.3.14f1 five targeted current gameplay suites: garage13, dynamic2, JumpPad2, EMP integration12, emitter22. Not full suite or Player.'
  $approval=Read-Host 'Type B0160 W30 LIVE GARAGE NATIVE'
  Need ($approval -ceq 'B0160 W30 LIVE GARAGE NATIVE') 'operator did not authorize native Unity'
  $out=Join-Path $scratch ('B0160-W30-LOCAL-RECEIPT-'+[Guid]::NewGuid().ToString('N'))
  $null=New-Item -Path $out -ItemType Directory -ErrorAction Stop
  $suites=@(
   @{name='garage';platform='EditMode';filter='GarageEditFreedomTests'},
   @{name='dynamic';platform='EditMode';filter='W30B0160GarageDynamicGateTests'},
   @{name='jump';platform='EditMode';filter='W29LiveArenaJumpPadTests'},
   @{name='emp-integration';platform='EditMode';filter='RRLW6EmpPhysicalTests'},
   @{name='emp-module';platform='PlayMode';filter='RoboRougelike.Mechanics.EMP.Tests.EmpEmitterPhysicsTests'}
  )
  $exitMap=@{}
  $hashes=@{}
  foreach($item in $suites){
   $label=$item.name
   $xml=Join-Path $out ('W30-'+$label+'.xml')
   $log=Join-Path $out ('W30-'+$label+'.Editor.log')
   $process=Join-Path $out ('W30-'+$label+'.process.log')
   $argv=@('-batchmode','-nographics','-quit','-projectPath',$repo,
    '-runTests','-testPlatform',$item.platform,'-testFilter',$item.filter,
    '-testResults',$xml,'-logFile',$log)
   & $editor @argv *>&1 | Out-File -LiteralPath $process -Encoding utf8
   $editorExit=$LASTEXITCODE
   $original=(Get-Content -LiteralPath $process -Raw)
   Set-Content -LiteralPath $process -Value ("exit_code=$editorExit"+[Environment]::NewLine+$original) -Encoding utf8
   $exitMap[$label]=$editorExit
   foreach($file in @($xml,$log,$process)){
    if(Test-Path -LiteralPath $file -PathType Leaf){
     $hashes[[IO.Path]::GetFileName($file)]=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
    }
   }
   Write-Host "B0160_W30_NATIVE_$label EXIT=$editorExit"
   if($editorExit -ne 0){break}
  }
  $receipt=[ordered]@{
   package='B01-60-P17W30-LIVE-GARAGE-G1-001/R1'
   source_head=$head
   game877_parent=$base
   game873_parent='d537b7310979b91df27a08b14d0f1c74d0889089'
   unity_version='6000.3.14f1'
   native_invoked=$true
   exits=$exitMap
   sha256=$hashes
   independent_qa_accepted=$false
   windows_player='NOT_RUN'
   unsigned_self_receipt=$true
  }
  $receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'W30-native-receipt.json') -Encoding utf8
  Write-Host "B0160_W30_EVIDENCE_LOCAL_UNUPLOADED=$out"
  Need ($exitMap.Count -eq 5 -and @($exitMap.Values | Where-Object {$_ -ne 0}).Count -eq 0) 'at least one actual native suite failed or was not run'
  & python (Join-Path $repo $verify) --native-evidence $out --source-sha $head
  Need ($LASTEXITCODE -eq 0) 'original NUnit/Editor/exit/source SHA receipt G1 structural failure'
  Write-Host 'B0160_W30_NATIVE_51_XML_STRUCTURALLY_PASS_INDEPENDENT_PEER_PENDING_PLAYER_NOT_RUN'
  $rc=0
 }
} catch {
 $rc=2
 if($Run){$rc=3}
 Write-Warning ('B0160_W30_FAIL_CLOSED: '+($_.Exception.Message -replace '[\r\n]+',' '))
} finally{
 Write-Host "B0160_W30_EXIT=$rc NO_MERGE_NO_RELEASE_NO_AUTO_UPLOAD_NO_PC_BACKGROUND_ACCESS"
}
exit $rc
