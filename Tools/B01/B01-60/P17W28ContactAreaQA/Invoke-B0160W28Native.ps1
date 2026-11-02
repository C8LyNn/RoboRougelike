#requires -Version 5.1
<# B01-60 W28: manually run TWO new real Codex gameplay contact/area Unity EditMode tests
   from a user-created DISPOSABLE worktree, NEVER from Codex's active game tree.
   G0 approval and explicit operator consent required before starting Unity.
#>
[CmdletBinding()]
param(
 [string]$RepoPath='.',
 [string]$UnityPath='',
 [string]$ScratchParent='',
 [switch]$Run,
 [switch]$SourceG0Approved,
 [switch]$DisposableCheckoutAcknowledged
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$exitCode=2
function Require([bool]$ok,[string]$reason) {if(-not $ok){throw "B0160_W28_HOLD: $reason"}}
function LocalGit([string[]]$gitArgs) {
 $lines=@(& git @gitArgs 2>&1)
 Require ($LASTEXITCODE -eq 0) 'Git source verification failed, no auto-fetch'
 return ($lines -join [Environment]::NewLine).Trim()
}
function Under([string]$child,[string]$parent) {
 $c=[IO.Path]::GetFullPath($child).TrimEnd('\','/')+'\'
 $p=[IO.Path]::GetFullPath($parent).TrimEnd('\','/')+'\'
 return $c.StartsWith($p,[StringComparison]::OrdinalIgnoreCase)
}
try {
 Require ($env:OS -eq 'Windows_NT') 'manual native Unity operator only on Windows'
 $null=Get-Command git -ErrorAction Stop
 $null=Get-Command python -ErrorAction Stop
 $repo=(Resolve-Path -LiteralPath $RepoPath -ErrorAction Stop).Path
 Require (([IO.Path]::GetFullPath((LocalGit @('-C',$repo,'rev-parse','--show-toplevel')))).TrimEnd('\','/') -ieq
  ([IO.Path]::GetFullPath($repo)).TrimEnd('\','/')) 'require exact Git checkout root'
 $head=LocalGit @('-C',$repo,'rev-parse','HEAD')
 $base='e03d36f2b86f8c73a067e574b67237a8921f622a'
 Require ((LocalGit @('-C',$repo,'merge-base',$base,$head)) -eq $base) 'wrong Codex W27 parent'
 $dirty=LocalGit @('-C',$repo,'status','--porcelain','--untracked-files=normal')
 Require ([string]::IsNullOrWhiteSpace($dirty)) 'checkout not clean; native would modify dirty Unity project'
 $mine='Tools/B01/B01-60/P17W28ContactAreaQA/Invoke-B0160W28Native.ps1'
 Require ([IO.Path]::GetFullPath((Join-Path $repo $mine)) -ieq [IO.Path]::GetFullPath($PSCommandPath)) 'operator file not in target checkout'
 $check='Tools/B01/B01-60/P17W28ContactAreaQA/check.py'
 foreach($p in @($mine,$check,'Assets/Tests/Editor/W28B0160ContactAreaClearanceTests.cs')){
  Require ((LocalGit @('-C',$repo,'hash-object','--',$p)) -eq
   (LocalGit @('-C',$repo,'rev-parse',"HEAD:$p"))) "dirty native runner or test $p"
 }
 Write-Host "B0160_W28_ORIGINAL_W27=$base CURRENT_SOURCE_HEAD=$head"
 & python (Join-Path $repo $check)
 Require ($LASTEXITCODE -eq 0) 'independent actual source/owned-only manifest gate failed'
 if(-not $Run) {
  $exitCode=0
  Write-Host 'B0160_W28_SOURCE_CHECK_ONLY_NATIVE_UNITY_NOT_RUN'
 } else {
  Require $SourceG0Approved.IsPresent 'independent exact Codex #824 + own W28 source G0 absent'
  Require $DisposableCheckoutAcknowledged.IsPresent 'owner must explicitly acknowledge independent disposable clone'
  Require ($repo -match 'B0160-W28-Disposable') 'must be a separately named B0160-W28-Disposable worktree, not Codex active project'
  if([string]::IsNullOrWhiteSpace($ScratchParent)){$ScratchParent=$env:TEMP}
  $scratch=(Resolve-Path -LiteralPath $ScratchParent -ErrorAction Stop).Path
  Require (-not $scratch.StartsWith('\\')) 'network scratch refused'
  Require (-not(Under $scratch $repo) -and -not(Under $repo $scratch)) 'scratch intersects project tree'
  $d=Get-Item -LiteralPath $scratch -ErrorAction Stop
  while($null -ne $d) {
   Require (-not [bool]($d.Attributes -band [IO.FileAttributes]::ReparsePoint)) 'linked scratch parent refused'
   $d=$d.Parent
  }
  if([string]::IsNullOrWhiteSpace($UnityPath)){
   $UnityPath=Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.3.14f1/Editor/Unity.exe'
  }
  Require (Test-Path -LiteralPath $UnityPath -PathType Leaf) 'Unity Editor missing'
  $editor=(Resolve-Path -LiteralPath $UnityPath).Path
  Require ($editor -match '[\\/]6000\.3\.14f1[\\/]Editor[\\/]Unity\.exe$') 'incorrect Unity version/path'
  Require (-not [bool]((Get-Item -LiteralPath $editor).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'linked Editor denied'
  Write-Host "B0160_W28_EDITOR_SHA256=$((Get-FileHash -LiteralPath $editor -Algorithm SHA256).Hash.ToLowerInvariant())"
  Write-Host 'This test touches ONLY the disposable Unity clone. This will run 2 W28 contact/area Unity tests, NOT a Windows Player build.'
  $consent=Read-Host 'Type exact B0160 W28 CONTACT AREA NATIVE to launch activated Unity'
  Require ($consent -ceq 'B0160 W28 CONTACT AREA NATIVE') 'owner explicitly denied native test'
  $folder=Join-Path $scratch ('B0160-W28-EVIDENCE-'+[Guid]::NewGuid().ToString('N'))
  $null=New-Item -Path $folder -ItemType Directory -ErrorAction Stop
  $testXml=Join-Path $folder 'W28-contact-area-results.xml'
  $editorLog=Join-Path $folder 'W28-Editor.log'
  $processLog=Join-Path $folder 'W28-process.log'
  $filter='W28B0160ContactAreaClearanceTests'
  $argv=@('-batchmode','-nographics','-quit','-projectPath',$repo,
   '-runTests','-testPlatform','EditMode','-testFilter',$filter,
   '-testResults',$testXml,'-logFile',$editorLog)
  & $editor @argv *>&1 | Out-File -LiteralPath $processLog -Encoding utf8
  $nativeExit=$LASTEXITCODE
  $oldLog=Get-Content -LiteralPath $processLog -Raw
  Set-Content -LiteralPath $processLog -Value ("exit_code=$nativeExit"+[Environment]::NewLine+$oldLog) -Encoding utf8
  $hashes=@{}
  foreach($file in @($testXml,$editorLog,$processLog)) {
   if(Test-Path -LiteralPath $file -PathType Leaf) {
    $hashes[[IO.Path]::GetFileName($file)]=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
   }
  }
  $receipt=[ordered]@{
   package_id='B01-60-W28-CONTACT-AREA-NATIVE-001'
   head_sha=$head
   original_codex_w27_sha=$base
   unity_version='6000.3.14f1'
   native_invoked=$true
   exit_code=$nativeExit
   test_filter=$filter
   sha256=$hashes
   unsigned_evidence=$true
   independent_qa_accepted=$false
   player_build='NOT_RUN'
  }
  $receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $folder 'W28-run.json') -Encoding utf8
  Write-Host "B0160_W28_PRIVATE_EVIDENCE_NO_UPLOAD=$folder"
  Require ($nativeExit -eq 0) "Unity returned native exit $nativeExit"
  & python (Join-Path $repo $check) --audit-native $folder --expected-head $head
  Require ($LASTEXITCODE -eq 0) 'NUnit3 two actual cases and Unity Editor evidence failed'
  Write-Host 'B0160_W28_REAL_UNITY_TWO_CASES_STRUCTURALLY_PASS_INDEPENDENT_QA_PENDING'
  $exitCode=0
 }
}
catch {
 $exitCode=2;if($Run){$exitCode=3}
 Write-Warning ('B0160_W28_FAIL_CLOSED: '+($_.Exception.Message -replace '[\r\n]+',' '))
}
finally { Write-Host "B0160_W28_EXIT=$exitCode NO_MERGE_NO_AUTO_UPLOAD_NO_BACKGROUND_PC_ACCESS" }
exit $exitCode
