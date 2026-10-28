# B01-40 PASS14 exact-owner PARTSIZE W66 genuine Git+PowerShell
# sibling-source evidence isolation retest. PreflightOnly NEVER runs Unity.
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$OperatorPath)
$ErrorActionPreference='Stop'
$sourceSha='be2be0e4802bc3f450179901f9ae91c6d84bcb5b'
$repo=(& git rev-parse --show-toplevel | Out-String).Trim()
if($LASTEXITCODE -ne 0){throw 'NOT_GIT_QA_REPOSITORY'}
$operator=[IO.Path]::GetFullPath($OperatorPath)
$fakeUnity=[Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
if(!(Test-Path -LiteralPath $operator -PathType Leaf)){throw 'OWNER_ORIGINAL_SCRIPT_MISSING'}
if(!(Test-Path -LiteralPath $fakeUnity -PathType Leaf)){throw 'TEST_PWSH_BINARY_MISSING'}
$base=Join-Path ([IO.Path]::GetTempPath()) ("b01_40_w66_sibling_"+[Guid]::NewGuid().ToString('N'))
$one=Join-Path $base 'producer-one'
$two=Join-Path $base 'producer-two'
$outside=Join-Path $base 'external-receipts'
$counter=0
function VerifyGit([string]$what){
  if($LASTEXITCODE -ne 0){throw "GIT_FAILED:${what}:$LASTEXITCODE"}
}
function Allow([string]$name,[string]$evidence){
  $out=(& $operator -UnityEditor $fakeUnity -ProjectPath $one -EvidenceDir $evidence -PreflightOnly *>&1 | Out-String)
  if($out -notmatch 'PARTSIZE_DISPOSABLE_PREFLIGHT_PASS' -or $out -notmatch 'G1 NOT_RUN'){
    throw "UNEXPECTED_POSITIVE_REFUSAL:${name}:$out"
  }
  $script:counter++
  Write-Host "INDEPENDENT_W66_ALLOW:$name"
}
function Deny([string]$name,[string]$evidence,[string]$reason){
  $err=''
  try { & $operator -UnityEditor $fakeUnity -ProjectPath $one -EvidenceDir $evidence -PreflightOnly *>$null }
  catch { $err=$_.Exception.Message }
  if(!$err.Contains($reason)){throw "SIBLING_NEGATIVE_FAIL_OPEN:$name need=$reason got=$err"}
  $script:counter++
  Write-Host "INDEPENDENT_W66_DENY:${name}:$reason"
}
New-Item -ItemType Directory -Path $base -Force | Out-Null
try {
  & git -C $repo worktree add --detach $one $sourceSha | Out-Null
  VerifyGit 'linked_one'
  & git -C $repo worktree add --detach $two $sourceSha | Out-Null
  VerifyGit 'linked_two'
  Set-Content -LiteralPath (Join-Path $one '.b01-60-disposable-qa') -Value 'test-only'
  # Genuine two independent registered linked Git producer SHA worktrees.
  $all=@(& git -C $repo worktree list --porcelain | Out-String)
  if($all -notmatch [regex]::Escape($one) -or $all -notmatch [regex]::Escape($two)){
    throw 'WORKTREE_REGISTRY_MISSING_SIBLING'
  }
  Allow 'out_of_all_source_trees' $outside
  Deny 'own_worktree' (Join-Path $one 'evidence') 'RECEIPT_MUST_BE_OUTSIDE_PROJECT'
  Deny 'sibling_root' $two 'RECEIPT_OVERLAPS_REGISTERED_WORKTREE'
  Deny 'sibling_Unity_Assets' (Join-Path $two 'Assets/evidence') 'RECEIPT_OVERLAPS_REGISTERED_WORKTREE'
  Deny 'parent_of_sibling' $base 'RECEIPT_OVERLAPS_REGISTERED_WORKTREE'
  Allow 'fresh_after_denied_attempts' $outside
  if(Test-Path -LiteralPath $outside){throw 'PREFLIGHT_CREATED_EVIDENCE_OUTPUT'}
  Write-Host "B01_40_W66_PART_SIBLING_PRELIGHT_INDEPENDENT_PASS=$counter ORIGINAL_SCRIPT=YES NATIVE_UNITY=NOT_RUN G2=HOLD"
} finally {
  & git -C $repo worktree remove --force $two 2>$null | Out-Null
  & git -C $repo worktree remove --force $one 2>$null | Out-Null
  if(Test-Path -LiteralPath $base){Remove-Item -LiteralPath $base -Recurse -Force}
}
