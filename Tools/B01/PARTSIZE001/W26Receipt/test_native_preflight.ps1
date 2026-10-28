# Executes REAL PowerShell/Git operations on Windows and Linux, but NOT Unity.
# Uses linked disposable Git worktrees at the exact frozen PARTSIZE source SHA.
# All negative cases must reject BEFORE editor invocation or writing evidence.
[CmdletBinding()]
param()
$ErrorActionPreference="Stop"
$SOURCE="be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
$operator=Join-Path $PSScriptRoot "Run-UnityNative.ps1"
$repo=(& git -C $PSScriptRoot rev-parse --show-toplevel | Out-String).Trim()
if($LASTEXITCODE -ne 0){throw "NO_REPO"}
$editor=[Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
if(!(Test-Path -LiteralPath $editor -PathType Leaf)){throw "PWHS_BINARY_NOT_FOUND"}
$base=Join-Path ([IO.Path]::GetTempPath()) ("partsize_w38_"+[Guid]::NewGuid().ToString("N"))
$wt=Join-Path $base "linked-disposable"
$sibling=Join-Path $base "sibling-producer-worktree"
$attached=Join-Path $base "attached-branch"
$ordinary=Join-Path $base "ordinary-clone"
$out=Join-Path $base "outside-evidence"
$branchName="b01-23-w38-fixture-"+[Guid]::NewGuid().ToString("N")
$passed=0
function ExpectPass([string]$label,[string]$project,[string]$ev) {
    $result=(& $operator -UnityEditor $editor -ProjectPath $project -EvidenceDir $ev -PreflightOnly *>&1 | Out-String)
    if($result -notmatch "PARTSIZE_DISPOSABLE_PREFLIGHT_PASS"){throw "EXPECTED_PREFLIGHT_PASS:$label $result"}
    $script:passed++
    Write-Host "PREFLIGHT_POSITIVE_PASS:$label"
}
function ExpectReject([string]$label,[string]$project,[string]$ev,[string]$expected) {
    $caught=""
    try {
        & $operator -UnityEditor $editor -ProjectPath $project -EvidenceDir $ev -PreflightOnly *>$null
    } catch { $caught=$_.Exception.Message }
    if(!$caught.Contains($expected)){
        throw "EXPECTED_PREFLIGHT_REJECTION:$label expected=$expected actual=$caught"
    }
    $script:passed++
    Write-Host "PREFLIGHT_NEGATIVE_PASS:$label cause=$expected"
}
New-Item -ItemType Directory -Path $base -Force | Out-Null
try {
    & git -C $repo worktree add --detach $wt $SOURCE | Out-Null
    if($LASTEXITCODE -ne 0){throw "CREATE_DISPOSABLE_FAILED"}
    $marker=Join-Path $wt ".b01-60-disposable-qa"
    Set-Content -LiteralPath $marker -Value "explicitly disposable W38 CI fixture" -Encoding utf8
    # Cache is permitted; Unity output can populate Library/ and Temp/.
    New-Item -ItemType Directory -Path (Join-Path $wt "Library") -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $wt "Library/cache.txt") -Value "Unity cache"
    New-Item -ItemType Directory -Path (Join-Path $wt "Temp") -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $wt "Temp/cache.txt") -Value "Unity cache"
    ExpectPass "real_detached_git_worktree_with_cache" $wt $out
    # PASS14: B01-60 actually reproduced a P0 sibling-worktree receipt
    # guard bypass with the ORIGINAL operator on Windows and Ubuntu.
    # Two genuine source-pinned registered detached worktrees are mandatory.
    & git -C $repo worktree add --quiet --detach $sibling $SOURCE
    if($LASTEXITCODE -ne 0){throw "CREATE_SIBLING_PRODUCER_NEGATIVE_FAILED"}
    ExpectReject "evidence_at_sibling_root" $wt $sibling "RECEIPT_OVERLAPS_REGISTERED_WORKTREE"
    ExpectReject "evidence_inside_sibling_assets" $wt (Join-Path $sibling "Assets/W14-Evidence") "RECEIPT_OVERLAPS_REGISTERED_WORKTREE"
    ExpectReject "evidence_parent_enclosing_sibling" $wt $base "RECEIPT_OVERLAPS_REGISTERED_WORKTREE"
    ExpectReject "evidence_inside_registered_primary" $wt (Join-Path $repo "B01-W14-Evidence") "RECEIPT_OVERLAPS_REGISTERED_WORKTREE"
    ExpectReject "evidence_equal_registered_primary" $wt $repo "RECEIPT_OVERLAPS_REGISTERED_WORKTREE"
    # False-positive guard: a sibling-prefix name is NOT a descendant.
    ExpectPass "similar_prefix_outside_all_registered_worktrees" $wt ($sibling+"-evidence")

    # Bypass #1: marker and matching HEAD are insufficient without linked
    # worktree provenance (ordinary clone and attached worktree must fail).
    & git clone --quiet --no-hardlinks --local $repo $ordinary
    if($LASTEXITCODE -ne 0){throw "CLONE_FOR_NEGATIVE_FAILED"}
    # actions/checkout creates a shallow current-branch clone; the historical
    # producer SHA must be fetched explicitly into the ordinary-clone negative.
    & git -C $ordinary fetch --quiet --no-tags $repo $SOURCE
    if($LASTEXITCODE -ne 0){throw "CLONE_FETCH_PIN_FOR_NEGATIVE_FAILED"}
    & git -C $ordinary checkout --quiet --detach $SOURCE
    if($LASTEXITCODE -ne 0){throw "CLONE_PIN_FOR_NEGATIVE_FAILED"}
    Set-Content -LiteralPath (Join-Path $ordinary ".b01-60-disposable-qa") -Value "forged"
    ExpectReject "ordinary_detached_clone_with_marker" $ordinary $out "REFUSING_NON_LINKED_GIT_WORKTREE"
    & git -C $repo worktree add --quiet -b $branchName $attached $SOURCE
    if($LASTEXITCODE -ne 0){throw "CREATE_ATTACHED_NEGATIVE_FAILED"}
    Set-Content -LiteralPath (Join-Path $attached ".b01-60-disposable-qa") -Value "forged"
    ExpectReject "attached_git_worktree_with_marker" $attached $out "REFUSING_ATTACHED_GIT_BRANCH"

    # Bypass #2: equality / descendant / canonical .. aliases are rejected.
    ExpectReject "evidence_equal_project" $wt $wt "RECEIPT_MUST_BE_OUTSIDE_PROJECT"
    ExpectReject "evidence_child_of_project" $wt (Join-Path $wt "Artifacts/Evidence") "RECEIPT_MUST_BE_OUTSIDE_PROJECT"
    ExpectReject "evidence_dotdot_alias" $wt (Join-Path (Join-Path $wt "Library") "../Artifacts") "RECEIPT_MUST_BE_OUTSIDE_PROJECT"

    # Bypass #3: actual untracked Unity compiler inputs must refuse even
    # when tracked repo is clean, then return to a demonstrably valid state.
    foreach($ext in @("cs","asmdef","meta")) {
        $file=Join-Path $wt ("Assets/W38_Untracked_Injector."+$ext)
        Set-Content -LiteralPath $file -Value "uncommitted Unity source"
        ExpectReject ("untracked_Assets_"+$ext) $wt $out "REFUSING_UNTRACKED_PROJECT_FILE"
        Remove-Item -LiteralPath $file -Force
    }
    foreach($path in @("Packages/W38-inject.json","ProjectSettings/W38-Injection.asset")) {
        $file=Join-Path $wt $path
        Set-Content -LiteralPath $file -Value "uncommitted host setting"
        ExpectReject ("untracked_"+$path) $wt $out "REFUSING_UNTRACKED_PROJECT_FILE"
        Remove-Item -LiteralPath $file -Force
    }
    # Ignored hidden source should still fail; no global/user .gitignore edit.
    $exclude=(& git -C $repo rev-parse --path-format=absolute --git-path info/exclude | Out-String).Trim()
    $oldExclude=[IO.File]::ReadAllBytes($exclude)
    try {
        [IO.File]::AppendAllText($exclude,[Environment]::NewLine+"Assets/W38_Ignored.cs"+[Environment]::NewLine)
        $ignored=Join-Path $wt "Assets/W38_Ignored.cs"
        Set-Content -LiteralPath $ignored -Value "hidden by info/exclude"
        ExpectReject "ignored_untracked_Assets_cs" $wt $out "REFUSING_IGNORED_UNITY_SOURCE"
        Remove-Item -LiteralPath $ignored -Force
    } finally {
        [IO.File]::WriteAllBytes($exclude,$oldExclude)
    }
    # A one-time consumed marker prevents a second native session. CI only
    # simulates the consumed state, never calls a real Unity executable.
    $used=Join-Path $wt ".b01-60-disposable-qa.used"
    Set-Content -LiteralPath $used -Value "consumed"
    ExpectReject "already_used_worktree" $wt $out "DISPOSABLE_WORKTREE_ALREADY_USED"
    Remove-Item -LiteralPath $used -Force
    ExpectPass "clean_again_after_every_negative" $wt $out
    if(Test-Path -LiteralPath $out){throw "PREFLIGHT_TEST_CREATED_EVIDENCE"}
    Write-Host "PARTSIZE_W38_REAL_GIT_POWERSHELL_PREFLIGHT_PASS total=$passed native_Unity=NOT_RUN"
} finally {
    & git -C $repo worktree remove --force $wt 2>$null | Out-Null
    & git -C $repo worktree remove --force $sibling 2>$null | Out-Null
    & git -C $repo worktree remove --force $attached 2>$null | Out-Null
    & git -C $repo branch -D $branchName 2>$null | Out-Null
    if(Test-Path -LiteralPath $base){Remove-Item -LiteralPath $base -Recurse -Force}
}
