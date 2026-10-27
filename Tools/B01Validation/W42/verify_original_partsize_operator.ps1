# B01-40 W42 independent test of the unmodified original B01-23 W38 PS1.
# Uses REAL pwsh + git; -PreflightOnly prevents ALL Unity invocations.
# No producer Assets or Codex worktree modification. Only temp Git fixtures.
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$OperatorPath)
$ErrorActionPreference = "Stop"
$sha = 'be2be0e4802bc3f450179901f9ae91c6d84bcb5b'
$repo = (& git rev-parse --show-toplevel | Out-String).Trim()
if ($LASTEXITCODE -ne 0) { throw "AUDIT_REPO_MISSING" }
$operator = [IO.Path]::GetFullPath($OperatorPath)
if (!(Test-Path -LiteralPath $operator -PathType Leaf)) { throw "ORIGINAL_OPERATOR_NOT_FETCHED" }
$source = Get-Content -LiteralPath $operator -Raw
if ($source -notmatch [regex]::Escape($sha)) { throw "QA_SCRIPT_EXPECTED_SOURCE_MISSING" }
$runTestsLine = @($source -split "\r?\n" | Where-Object { $_ -match '\& \$UnityEditor .*-runTests' })
if ($runTestsLine.Count -ne 1 -or $runTestsLine[0] -match '(?:^|\s)-quit(?:\s|$)') { throw "UNITY_TESTS_UNSAFE_QUIT" }
$fakeEditor = [Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
if (!(Test-Path -LiteralPath $fakeEditor -PathType Leaf)) { throw "PWSH_PATH_NOT_FOUND" }
$base = Join-Path ([IO.Path]::GetTempPath()) ("b01_40_w42_independent_" + [Guid]::NewGuid().ToString("N"))
$linked = Join-Path $base 'producer-linked'
$attached = Join-Path $base 'producer-attached'
$ordinary = Join-Path $base 'producer-ordinary-clone'
$outside = Join-Path $base 'evidence-outside'
$fixtureBranch = 'b01-40-w42-' + [Guid]::NewGuid().ToString("N")
$counter = 0
function RequireExit([string]$label) {
    if ($LASTEXITCODE -ne 0) { throw "GIT_CMD_FAILED:$label exit=$LASTEXITCODE" }
}
function ShouldAllow([string]$name,[string]$root,[string]$ev) {
    $output = (& $operator -UnityEditor $fakeEditor -ProjectPath $root -EvidenceDir $ev -PreflightOnly *>&1 | Out-String)
    if ($output -notmatch 'PARTSIZE_DISPOSABLE_PREFLIGHT_PASS' -or $output -notmatch 'G1 NOT_RUN') {
        throw "POSITIVE_NOT_ACCEPTED:$name $output"
    }
    $script:counter++
    Write-Host "INDEPENDENT_PRE_OK:$name"
}
function ShouldReject([string]$name,[string]$root,[string]$ev,[string]$reason) {
    $rejected = ''
    try { & $operator -UnityEditor $fakeEditor -ProjectPath $root -EvidenceDir $ev -PreflightOnly *>$null }
    catch { $rejected = $_.Exception.Message }
    if (!$rejected.Contains($reason)) { throw "NEGATIVE_NOT_REJECTED:$name need=$reason actual=$rejected" }
    $script:counter++
    Write-Host "INDEPENDENT_PRE_DENY:$name:$reason"
}
New-Item -ItemType Directory -Force -Path $base | Out-Null
try {
    & git -C $repo worktree add --detach $linked $sha | Out-Null
    RequireExit 'linked_detached_setup'
    Set-Content -LiteralPath (Join-Path $linked '.b01-60-disposable-qa') -Value 'independent test marker'
    New-Item -ItemType Directory -Force -Path (Join-Path $linked 'Library') | Out-Null
    Set-Content -LiteralPath (Join-Path $linked 'Library/cache.tmp') -Value 'legitimate Unity cache'
    ShouldAllow 'detached_linked_clean_and_cache' $linked $outside

    & git clone --local --no-hardlinks --quiet $repo $ordinary
    RequireExit 'ordinary_clone'
    & git -C $ordinary fetch --quiet --no-tags $repo $sha
    RequireExit 'ordinary_fetch'
    & git -C $ordinary checkout --quiet --detach $sha
    RequireExit 'ordinary_detach'
    Set-Content -LiteralPath (Join-Path $ordinary '.b01-60-disposable-qa') -Value 'fake marker'
    ShouldReject 'ordinary_detached_clone' $ordinary $outside 'REFUSING_NON_LINKED_GIT_WORKTREE'

    & git -C $repo worktree add --quiet -b $fixtureBranch $attached $sha
    RequireExit 'attached_checkout'
    Set-Content -LiteralPath (Join-Path $attached '.b01-60-disposable-qa') -Value 'fake marker'
    ShouldReject 'linked_but_attached_branch' $attached $outside 'REFUSING_ATTACHED_GIT_BRANCH'

    ShouldReject 'evidence_equals_root' $linked $linked 'RECEIPT_MUST_BE_OUTSIDE_PROJECT'
    ShouldReject 'evidence_dot_alias_equals_root' $linked (Join-Path $linked '.') 'RECEIPT_MUST_BE_OUTSIDE_PROJECT'
    ShouldReject 'evidence_descendant' $linked (Join-Path $linked 'Library/Evidence') 'RECEIPT_MUST_BE_OUTSIDE_PROJECT'
    ShouldReject 'evidence_dotdot_descendant' $linked (Join-Path $linked 'Library/../Evidence') 'RECEIPT_MUST_BE_OUTSIDE_PROJECT'

    foreach($ext in @('cs','asmdef','meta')) {
        $f=Join-Path $linked ("Assets/independent-w42-injection."+$ext)
        try {
            Set-Content -LiteralPath $f -Value 'untracked Unity compiler input'
            ShouldReject ("unexpected_Assets_."+$ext) $linked $outside 'REFUSING_UNTRACKED_PROJECT_FILE'
        } finally { Remove-Item -LiteralPath $f -Force -ErrorAction SilentlyContinue }
    }
    foreach($item in @('Packages/independent-w42.json','ProjectSettings/independent-w42.asset')) {
        $f=Join-Path $linked $item
        try {
            Set-Content -LiteralPath $f -Value 'untracked Unity project content'
            ShouldReject ("unexpected_"+$item) $linked $outside 'REFUSING_UNTRACKED_PROJECT_FILE'
        } finally { Remove-Item -LiteralPath $f -Force -ErrorAction SilentlyContinue }
    }

    # Different from owner's fixture: tracked Unity project setting modification.
    $versionPath=Join-Path $linked 'ProjectSettings/ProjectVersion.txt'
    $oldVersion=[IO.File]::ReadAllBytes($versionPath)
    try {
        [IO.File]::AppendAllText($versionPath, "# INDEPENDENT_W42_DIRTY")
        ShouldReject 'tracked_changed_ProjectVersion' $linked $outside 'REFUSING_TRACKED_DIRTY_PROJECT'
    } finally { [IO.File]::WriteAllBytes($versionPath,$oldVersion) }

    # Genuine ignored Unity source, added to temp checkout's excludes only.
    $excludes=(& git -C $repo rev-parse --path-format=absolute --git-path info/exclude | Out-String).Trim()
    RequireExit 'git_exclude_path'
    $originalExclude=[IO.File]::ReadAllBytes($excludes)
    $ignoredFile=Join-Path $linked 'Assets/independent-w42-ignored.cs'
    try {
        [IO.File]::AppendAllText($excludes, [Environment]::NewLine + 'Assets/independent-w42-ignored.cs' + [Environment]::NewLine)
        Set-Content -LiteralPath $ignoredFile -Value 'Git-ignored Unity code'
        ShouldReject 'ignored_Assets_compiler_input' $linked $outside 'REFUSING_IGNORED_UNITY_SOURCE'
    } finally {
        Remove-Item -LiteralPath $ignoredFile -Force -ErrorAction SilentlyContinue
        [IO.File]::WriteAllBytes($excludes,$originalExclude)
    }

    # Non-reusable session (CI never calls Unity).
    $used=Join-Path $linked '.b01-60-disposable-qa.used'
    try {
        Set-Content -LiteralPath $used -Value 'claimed'
        ShouldReject 'already_claimed' $linked $outside 'DISPOSABLE_WORKTREE_ALREADY_USED'
    } finally { Remove-Item -LiteralPath $used -Force -ErrorAction SilentlyContinue }
    ShouldAllow 'still_safe_after_all_negative_fixtures' $linked $outside
    if (Test-Path -LiteralPath $outside) { throw 'AUDIT_PREVIEW_CREATED_EVIDENCE' }
    Write-Host "B01_40_W42_INDEPENDENT_PARTSIZE_PREFLIGHT_PASS actual_pwsh_git_cases=$counter licensed_Unity=NOT_RUN"
} finally {
    & git -C $repo worktree remove --force $attached 2>$null | Out-Null
    & git -C $repo worktree remove --force $linked 2>$null | Out-Null
    & git -C $repo branch -D $fixtureBranch 2>$null | Out-Null
    if (Test-Path -LiteralPath $base) { Remove-Item -LiteralPath $base -Recurse -Force }
}
