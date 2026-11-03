#requires -Version 5.1
<# B01-60 P17W29: independently gated FULL existing WPN005 PlayMode
 and default R3 saw EditMode fixture. User manually opts in ONLY inside a
 clean DISPOSABLE B0160-W29-Disposable Unity checkout; not Codex worktree.
 No automatic Unity/background access, network fetch, merge or Player build.
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
function Need([bool]$condition,[string]$why) {
 if(-not $condition){throw "B0160_W29_HOLD: $why"}
}
function GitLocal([string[]]$gitArgs) {
 $result=@(& git @gitArgs 2>&1)
 Need ($LASTEXITCODE -eq 0) 'local Git command failed; no automatic network fetch'
 return ($result -join [Environment]::NewLine).Trim()
}
function Under([string]$child,[string]$parent) {
 $c=[IO.Path]::GetFullPath($child).TrimEnd('\','/')+'\'
 $p=[IO.Path]::GetFullPath($parent).TrimEnd('\','/')+'\'
 return $c.StartsWith($p,[StringComparison]::OrdinalIgnoreCase)
}
try {
 Need ($env:OS -eq 'Windows_NT') 'real native Editor is owner Windows only'
 $null=Get-Command git -ErrorAction Stop
 $null=Get-Command python -ErrorAction Stop
 $root=(Resolve-Path -LiteralPath $RepoPath -ErrorAction Stop).Path
 $actual=GitLocal @('-C',$root,'rev-parse','--show-toplevel')
 Need ([IO.Path]::GetFullPath($actual).TrimEnd('\','/') -ieq
       [IO.Path]::GetFullPath($root).TrimEnd('\','/')) 'not exact repo root'
 $base='887fd18bf0a518ee24776c660f5efa6e3a0491d8'
 $head=GitLocal @('-C',$root,'rev-parse','HEAD')
 Need ((GitLocal @('-C',$root,'merge-base',$base,$head)) -eq $base) 'wrong original #828 ancestry'
 $dirty=GitLocal @('-C',$root,'status','--porcelain','--untracked-files=normal')
 Need ([string]::IsNullOrWhiteSpace($dirty)) 'dirty project: refuse changing operator-owned files'
 $mine='Tools/B01/B01-60/P17W29SawOverflowQA/Invoke-B0160W29SawNative.ps1'
 $verify='Tools/B01/B01-60/P17W29SawOverflowQA/verify.py'
 Need ([IO.Path]::GetFullPath((Join-Path $root $mine)) -ieq
    [IO.Path]::GetFullPath($PSCommandPath)) 'PowerShell invocation not from own Git checkout'
 foreach($p in @($mine,$verify,'Assets/RoboRougelikeMechanics/WPN005RotarySaw/Runtime/RotarySawModule.cs')) {
  Need ((GitLocal @('-C',$root,'hash-object','--',$p)) -eq
       (GitLocal @('-C',$root,'rev-parse',"HEAD:$p"))) "source SHA mismatch $p"
 }
 Write-Host "B0160_W29_CANDIDATE_HEAD=$head ORIGINAL_828_SHA=$base"
 & python (Join-Path $root $verify)
 Need ($LASTEXITCODE -eq 0) 'original runtime/test exact source diff gate RED'
 if(-not $Run) {
  $rc=0
  Write-Host 'B0160_W29_SOURCE_ONLY_GREEN_NO_UNITY_PROCESS'
 } else {
  Need $SourceG0Approved.IsPresent 'independent original #828+W29 G0 not approved'
  Need $DisposableCheckoutAcknowledged.IsPresent 'explicit safe disposable checkout acknowledgment required'
  Need ($root -match 'B0160-W29-Disposable') 'must run in a distinct B0160-W29-Disposable checkout'
  if([string]::IsNullOrWhiteSpace($ScratchParent)){$ScratchParent=$env:TEMP}
  $scratch=(Resolve-Path -LiteralPath $ScratchParent -ErrorAction Stop).Path
  Need (-not $scratch.StartsWith('\\')) 'no network scratch'
  Need (-not (Under $scratch $root) -and -not (Under $root $scratch)) 'scratch overlaps original project'
  $p=Get-Item -LiteralPath $scratch -ErrorAction Stop
  while($null -ne $p) {
   Need (-not [bool]($p.Attributes -band [IO.FileAttributes]::ReparsePoint)) 'linked scratch parent not allowed'
   $p=$p.Parent
  }
  if([string]::IsNullOrWhiteSpace($UnityPath)) {
   $UnityPath=Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.3.14f1/Editor/Unity.exe'
  }
  Need (Test-Path -LiteralPath $UnityPath -PathType Leaf) 'Unity Hub Editor not installed'
  $editor=(Resolve-Path -LiteralPath $UnityPath).Path
  Need ($editor -match '[\\/]6000\.3\.14f1[\\/]Editor[\\/]Unity\.exe$') 'wrong Unity version'
  Need (-not [bool]((Get-Item -LiteralPath $editor).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'linked Editor refused'
  Write-Host "B0160_W29_EDITOR_SHA256=$((Get-FileHash -LiteralPath $editor -Algorithm SHA256).Hash.ToLowerInvariant())"
  Write-Host 'RUNS 21 genuine original + W29 WPN005 PlayMode and 4 known default R3 EditMode; failing defaults MUST block G1. No Windows Player build.'
  $consent=Read-Host 'Type B0160 W29 SAW NATIVE to start activated Unity 6000.3.14f1'
  Need ($consent -ceq 'B0160 W29 SAW NATIVE') 'owner declined native'
  $out=Join-Path $scratch ('B0160-W29-LOCAL-EVIDENCE-'+[Guid]::NewGuid().ToString('N'))
  $null=New-Item -Path $out -ItemType Directory -ErrorAction Stop
  $hashes=@{}
  $exits=@{}
  foreach($case in @(
   @{name='playmode';platform='PlayMode';filter='RoboRougelike.Weapons.RotarySaw.Tests.RotarySawPhysicsTests'},
   @{name='editmode';platform='EditMode';filter='RRLW6SawPhysicalTests'})) {
   $label=$case.name
   $xml=Join-Path $out ('W29-saw-'+$label+'.xml')
   $log=Join-Path $out ('W29-saw-'+$label+'-Editor.log')
   $stdout=Join-Path $out ('W29-saw-'+$label+'-process.log')
   $args=@('-batchmode','-nographics','-quit','-projectPath',$root,
    '-runTests','-testPlatform',$case.platform,'-testFilter',$case.filter,
    '-testResults',$xml,'-logFile',$log)
   & $editor @args *>&1 | Out-File -LiteralPath $stdout -Encoding utf8
   $exit=$LASTEXITCODE
   $old=(Get-Content -LiteralPath $stdout -Raw)
   Set-Content -LiteralPath $stdout -Value ("exit_code=$exit"+[Environment]::NewLine+$old) -Encoding utf8
   $exits[$label]=$exit
   foreach($file in @($xml,$log,$stdout)) {
    if(Test-Path -LiteralPath $file -PathType Leaf) {
     $hashes[[IO.Path]::GetFileName($file)]=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
    }
   }
   Write-Host "B0160_W29_REAL_NATIVE_$($label.ToUpper())_EXIT=$exit"
   if($exit -ne 0){break}
  }
  $receipt=[ordered]@{
   package_id='B01-60-P17W29-SAW-OVERFLOW-CORRECTION-001/R1'
   head_sha=$head
   original_codex_828_sha=$base
   unity_version='6000.3.14f1'
   native_executed=$true
   playmode_exit=if($exits.ContainsKey('playmode')){$exits['playmode']}else{-1}
   editmode_exit=if($exits.ContainsKey('editmode')){$exits['editmode']}else{-1}
   sha256=$hashes
   source_g0_operator_flag=$SourceG0Approved.IsPresent
   independent_qa_accepted=$false
   windows_player='NOT_RUN'
   unsigned_evidence=$true
  }
  $receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'W29-saw-run.json') -Encoding utf8
  Write-Host "B0160_W29_LOCAL_EVIDENCE_NEVER_UPLOADED=$out"
  Need ($receipt.playmode_exit -eq 0 -and $receipt.editmode_exit -eq 0) 'native full saw suite still failed; no G1'
  & python (Join-Path $root $verify) --native-receipt $out --head $head
  Need ($LASTEXITCODE -eq 0) 'incomplete or wrong original native Editor/NUnit SHA evidence'
  Write-Host 'B0160_W29_SAW_NATIVE_STRUCTURAL_PASS_INDEPENDENT_QA_PENDING_PLAYER_NOT_RUN'
  $rc=0
 }
} catch {
 $rc=2
 if($Run){$rc=3}
 Write-Warning ('B0160_W29_FAIL_CLOSED: '+($_.Exception.Message -replace '[\r\n]+',' '))
} finally {
 Write-Host "B0160_W29_EXIT=$rc NO_AUTO_UPLOAD_NO_MERGE_NO_PC_BACKGROUND_ACCESS"
}
exit $rc
