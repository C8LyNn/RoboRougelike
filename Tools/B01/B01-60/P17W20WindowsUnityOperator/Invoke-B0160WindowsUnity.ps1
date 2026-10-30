#requires -Version 5.1
<#
User-operated B01-60 #667. Run with pwsh -NoProfile -File THIS_SCRIPT -RepoPath .
Add -Run and type B0160 RUN NATIVE to start original V2 native tests.
No SSH, network fetch, privileged service, shared project mutation, deletion, or upload.
#>
[CmdletBinding()]
param([string]$RepoPath=".",[string]$ScratchParent="",[string]$UnityPath="",[switch]$Run)
Set-StrictMode -Version Latest
$ErrorActionPreference="Stop"
$SourceSha="";$scratch="";$exitCode=2
$receipt=@{package="B01-60-P17W20-WINDOWS-LOCAL-UNITY";mode="PREFLIGHT";
  source_sha="";real_unity="NOT_RUN";native_g1="NOT_RUN";selected_R4="SELECTED_647_SECURITY_P0_G0_HOLD";
  selected_R4_sha="09c1809b73324a6d31c13e5d0143de62ac1e3ca6";
  ALT_HOLD_R4_652="ae5dbfa5e9c4689d2bf80fcabf8438e9c673c419";
  canonical_SaveLoad="HOLD";codex_ack="NONE";merged=$false;uploaded=$false;
  editor_sha256="";operator_consent=$false;result="NOT_RUN";
  start_utc=(Get-Date).ToUniversalTime().ToString("o")}
function Need([bool]$ok,[string]$reason) {if(-not $ok){throw "B0160_W20_FAIL_CLOSED: $reason"}}
function Invoke-LocalGit([string[]]$a) {
 $v=@(& git @a 2>&1)
 Need ($LASTEXITCODE -eq 0) "Local Git source/integrity operation failed"
 return ($v -join [Environment]::NewLine).Trim()
}
function ChildOf([string]$a,[string]$b) {
 $a=[IO.Path]::GetFullPath($a).TrimEnd('\','/')+'\'
 $b=[IO.Path]::GetFullPath($b).TrimEnd('\','/')+'\'
 return $a.StartsWith($b,[StringComparison]::OrdinalIgnoreCase)
}
function Evidence([string]$dir,[hashtable]$state) {
 $e=Join-Path $dir "evidence"
 if(-not(Test-Path -LiteralPath $e)){New-Item -ItemType Directory -Path $e | Out-Null}
 $state.end_utc=(Get-Date).ToUniversalTime().ToString("o")
 $state | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $e "operator-report.json") -Encoding UTF8
 $files=@()
 foreach($f in (Get-ChildItem -LiteralPath $e -Recurse -File -Force)) {
  if($f.Name -in @("sha256-manifest.json","sha256-manifest.json.sha256")){continue}
  Need (-not [bool]($f.Attributes -band [IO.FileAttributes]::ReparsePoint)) "Symlink in evidence"
  $files+=@{path=$f.FullName.Substring($e.Length).TrimStart('\','/').Replace('\','/');
             bytes=$f.Length;sha256=(Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
 }
 $p=Join-Path $e "sha256-manifest.json"
 @{source_sha=$SourceSha;unsigned=$true;note="SHA256 integrity, NOT a cryptographic signature";
   files=$files} | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath $p -Encoding UTF8
 (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant() |
   Set-Content -LiteralPath (Join-Path $e "sha256-manifest.json.sha256") -Encoding ASCII
}
try {
 Need ($env:OS -eq "Windows_NT") "Windows desktop only"
 $null=Get-Command git -ErrorAction Stop
 $null=Get-Command python -ErrorAction Stop
 $py=@(& python --version 2>&1)
 Need ($LASTEXITCODE -eq 0 -and ($py -join " ") -match '^Python 3\.') "Python 3 required"
 $repo=(Resolve-Path -LiteralPath $RepoPath -ErrorAction Stop).Path
 $top=Invoke-LocalGit @("-C",$repo,"rev-parse","--show-toplevel")
 Need ([IO.Path]::GetFullPath($top).TrimEnd('\') -ieq [IO.Path]::GetFullPath($repo).TrimEnd('\')) "RepoPath must be Git root"
 $SourceSha=Invoke-LocalGit @("-C",$repo,"rev-parse","HEAD")
 $receipt.source_sha=$SourceSha
 Need ($SourceSha -match '^[0-9a-f]{40}$') "Missing exact HEAD SHA"
 $me="Tools/B01/B01-60/P17W20WindowsUnityOperator/Invoke-B0160WindowsUnity.ps1"
 Need ([IO.Path]::GetFullPath((Join-Path $repo $me)) -ieq [IO.Path]::GetFullPath($PSCommandPath)) "Run this Git-tracked W20 script only"
 $localBlob=Invoke-LocalGit @("-C",$repo,"hash-object","--",$me)
 $committedBlob=Invoke-LocalGit @("-C",$repo,"rev-parse","HEAD:$me")
 Need ($localBlob -eq $committedBlob) "Local operator script modified since Git HEAD"
 foreach($f in @("Tools/B01/B01-60/P17W20WindowsUnityOperator/base_native.py",
                 "Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py")) {
  Need ((Git @("-C",$repo,"rev-parse","HEAD:$f")) -match '^[0-9a-f]{40}$') "Required frozen runner missing"
 }
 if([string]::IsNullOrWhiteSpace($ScratchParent)){$ScratchParent=$env:TEMP}
 $parent=(Resolve-Path -LiteralPath $ScratchParent -ErrorAction Stop).Path
 Need (-not ([bool]((Get-Item -LiteralPath $parent).Attributes -band [IO.FileAttributes]::ReparsePoint))) "Scratch parent symlink forbidden"
 Need (-not (ChildOf $parent $repo) -and -not (ChildOf $repo $parent) -and
       $parent -ine $repo) "Scratch parent must be disjoint from original game"
 $scratch=Join-Path $parent ("B0160W20-"+[guid]::NewGuid().ToString("N"))
 Need (-not(Test-Path -LiteralPath $scratch)) "Refuse existing workspace"
 New-Item -Path $scratch -ItemType Directory -ErrorAction Stop | Out-Null
 $co=Join-Path $scratch "git-exact-detached"
 $ev=Join-Path $scratch "evidence"
 New-Item -Path $ev -ItemType Directory -ErrorAction Stop | Out-Null
 Write-Host "B0160_W20_NEW_DISPOSABLE_SCRATCH=$scratch"
 $cl=@(& git -c core.hooksPath=NUL clone --no-hardlinks --no-checkout -- $repo $co 2>&1)
 Need ($LASTEXITCODE -eq 0) "Local-only clone failed; NO NETWORK FETCH attempted"
 $null=Invoke-LocalGit @("-C",$co,"-c","core.hooksPath=NUL","checkout","--detach",$SourceSha)
 Need ((Git @("-C",$co,"rev-parse","HEAD")) -eq $SourceSha) "Detached checkout SHA incorrect"
 $prj=Get-Content -LiteralPath (Join-Path $co "ProjectSettings/ProjectVersion.txt") -Raw
 Need ($prj -match '(?m)^m_EditorVersion: 6000\.3\.14f1\s*$') "Wrong project Unity version"
 $m=Get-Content -LiteralPath (Join-Path $co "Packages/manifest.json") -Raw | ConvertFrom-Json
 Need ($null -ne $m.dependencies.'com.unity.test-framework') "Unity Test Framework not declared"
 Need ($null -ne $m.dependencies.'com.unity.modules.physics') "Physics module not declared"
 Write-Host "B0160_W20_GIT_SHA_TESTFRAMEWORK_PHYSICS_SOURCE_DECLARATIONS_PASS $SourceSha"
 if([string]::IsNullOrWhiteSpace($UnityPath)) {
  $UnityPath=Join-Path $env:ProgramFiles "Unity/Hub/Editor/6000.3.14f1/Editor/Unity.exe"
 }
 $ready=(Test-Path -LiteralPath $UnityPath -PathType Leaf)
 $editor=""
 if($ready) {
  $editor=(Resolve-Path -LiteralPath $UnityPath).Path
  Need ($editor -match '[\\/]6000\.3\.14f1[\\/]Editor[\\/]Unity\.exe$') "Editor must be exact installed Unity Hub 6000.3.14f1"
  Need (-not [bool]((Get-Item -LiteralPath $editor).Attributes -band [IO.FileAttributes]::ReparsePoint)) "Editor symlink denied"
  $receipt.editor_sha256=(Get-FileHash -LiteralPath $editor -Algorithm SHA256).Hash.ToLowerInvariant()
  Write-Host "B0160_W20_EDITOR_EXE_LOCAL_HASHED_VERSION_FROM_PATH_ONLY_LICENSE_NOT_PROVEN"
 } else {Write-Host "B0160_W20_EDITOR_NOT_PRESENT_NATIVE_NOT_RUN"}
 $temp=Join-Path $scratch "python-temp"
 New-Item -Path $temp -ItemType Directory | Out-Null
 $env:TEMP=$temp;$env:TMP=$temp;$env:TMPDIR=$temp
 $driver=Join-Path $co "Tools/B01/B01-60/P17W20WindowsUnityOperator/base_native.py"
 $pre=Join-Path $ev "v2-source-preflight"
 & python $driver --mode preflight --out $pre *>&1 |
   Tee-Object -FilePath (Join-Path $ev "source-preflight-stdout.log")
 Need ($LASTEXITCODE -eq 0 -and (Test-Path -LiteralPath (Join-Path $pre "receipt.json"))) "Original V2 source preflight fail"
 $receipt.source_preflight="PASS_16_AUTHORED_CASES_NOT_EXECUTED"
 if(-not $Run) {
  $receipt.result="PREFLIGHT_PASS_NATIVE_NOT_RUN"
  $exitCode=0
  Write-Host "B0160_W20_PREFLIGHT_PASS_REAL_UNITY_G1_NOT_RUN"
 } else {
  Need $ready "Unity Hub 6000.3.14f1 missing. Native NOT_RUN; no activation bypass"
  Write-Host "Verify legitimate Unity license via your own Unity Hub. No credentials needed."
  Write-Host "This runs ONLY original independent #510 V2: 2 EditMode +14 PlayMode PhysX cases."
  Write-Host "R4 #647 owner-selected, #652 ALT_HOLD. Save/Load native HOLD: #670 orphan-physics host P0, no safe G0/fixture acceptance."
  $answer=Read-Host "To authorize ONE local native run, type exactly B0160 RUN NATIVE"
  Need ($answer -ceq "B0160 RUN NATIVE") "Explicit real operator opt-in denied"
  $receipt.operator_consent=$true
  $out=Join-Path $ev "original-V2-native-only"
  $receipt.mode="OPERATOR_NATIVE_V2_ONLY"
  & python $driver --mode native --unity $editor --out $out *>&1 |
    Tee-Object -FilePath (Join-Path $ev "native-stdout.log")
  $code=$LASTEXITCODE
  $receipt.native_process_exit=$code
  Need ($code -eq 0) "Original Unity/activation/TestRunner returned nonzero"
  foreach($f in @("EditMode.xml","PlayMode.xml","EditMode-Editor.log","PlayMode-Editor.log","native-verdict.json")) {
   Need (Test-Path -LiteralPath (Join-Path $out $f) -PathType Leaf) "Genuine Unity XML/log/identity missing"
  }
  $v=Get-Content -LiteralPath (Join-Path $out "native-verdict.json") -Raw | ConvertFrom-Json
  Need ($v.G1 -eq "PASS" -and $v.owner_sha -eq "d94400d95d13db7f27e1533df05c9292d5380431") "V2 original native verdict/source invalid"
  $receipt.native_g1="PASS_V2_ONLY_R4_STILL_NOT_RUN"
  $receipt.result="REAL_V2_ONLY_PASS_NOT_CANONICAL_SAVE"
  $exitCode=0
  Write-Host "B0160_W20_V2_16_NATIVE_PASS_ONLY_R4_AND_CANONICAL_G1_HOLD"
 }
}
catch {
 $receipt.result="HOLD_FAIL_CLOSED"
 $receipt.error=($_.Exception.Message -replace '[\r\n]+',' ')
 if($Run){$exitCode=3}
 Write-Warning ("B0160_W20_HOLD: "+$receipt.error)
}
finally {
 if(-not [string]::IsNullOrWhiteSpace($scratch) -and (Test-Path -LiteralPath $scratch)) {
  try {
   Evidence $scratch $receipt
   Write-Host "B0160_W20_EVIDENCE_RETAINED_NO_UPLOAD=$scratch\evidence"
   Write-Host "B0160_W20_SHA256_PROVENANCE_UNSIGNED"
  } catch {
   Write-Warning ("B0160_W20_HASH_MANIFEST_FAILED: "+$_.Exception.Message)
   $exitCode=5
  }
 }
 Write-Host "B0160_W20_EXIT=$exitCode NO_CLEANUP_NO_UPLOAD_NO_MERGE"
}
exit $exitCode
