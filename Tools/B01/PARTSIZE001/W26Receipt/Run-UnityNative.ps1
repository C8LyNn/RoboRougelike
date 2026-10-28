# B01-23 W26 native operator, ONLY disposable pinned Unity6000.3.14f1 worktree.
# Run only after licensed Unity is installed. Never edits or stages Codex source.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$UnityEditor,
    [Parameter(Mandatory=$true)][string]$ProjectPath,
    [Parameter(Mandatory=$true)][string]$EvidenceDir,
    [switch]$PreflightOnly
)
$ErrorActionPreference="Stop"
$ExpectedSHA="be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
$ExpectedUnity="6000.3.14f1"
# Canonicalize BEFORE checking evidence isolation; aliases using .. cannot
# bypass equality/descendant. Reparse points in ancestors fail closed.
$project=[IO.Path]::GetFullPath($ProjectPath).TrimEnd([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar)
$evidence=[IO.Path]::GetFullPath($EvidenceDir).TrimEnd([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar)
if([string]::IsNullOrWhiteSpace($project) -or [string]::IsNullOrWhiteSpace($evidence)){throw "INVALID_PROJECT_OR_EVIDENCE_PATH"}
function Assert-NoLinkedPathComponents([string]$candidate) {
    $cursor=$candidate
    while(![string]::IsNullOrEmpty($cursor)) {
        if(Test-Path -LiteralPath $cursor) {
            $info=Get-Item -LiteralPath $cursor -Force
            if(($info.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "UNTRUSTED_LINKED_PATH:$cursor"
            }
        }
        $parent=[IO.Path]::GetDirectoryName($cursor)
        if([string]::IsNullOrEmpty($parent) -or $parent -eq $cursor){break}
        $cursor=$parent
    }
}
Assert-NoLinkedPathComponents $project
Assert-NoLinkedPathComponents $evidence
if($evidence.Equals($project,[StringComparison]::OrdinalIgnoreCase) -or
   $evidence.StartsWith($project+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or
   $evidence.StartsWith($project+[IO.Path]::AltDirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){
    throw "RECEIPT_MUST_BE_OUTSIDE_PROJECT"
}
if(!(Test-Path -LiteralPath $UnityEditor -PathType Leaf)){throw "UNITY_EDITOR_NOT_FOUND"}
if(!(Test-Path -LiteralPath $project -PathType Container)){throw "PROJECT_DIR_NOT_FOUND"}
$marker=Join-Path $project ".b01-60-disposable-qa"
$usedMarker=Join-Path $project ".b01-60-disposable-qa.used"
if(!(Test-Path -LiteralPath $marker -PathType Leaf)){throw "REFUSING_NON_DISPOSABLE_PROJECT"}
if(Test-Path -LiteralPath $usedMarker){throw "DISPOSABLE_WORKTREE_ALREADY_USED"}
if(!(Test-Path -LiteralPath (Join-Path $project "Assets/PARTSIZE001/Tests/PlayMode/PartSize001PlayModeTests.cs"))){throw "PINNED_PARTSIZE_TEST_ASSET_MISSING"}

# Ordinary clones and attached branches cannot masquerade as disposable.
# Require a genuine registered LINKED Git worktree (a .git FILE) at a
# detached HEAD, with a separate primary worktree and exact source SHA.
if(!(Test-Path -LiteralPath (Join-Path $project ".git") -PathType Leaf)){
    throw "REFUSING_NON_LINKED_GIT_WORKTREE"
}
$gitRoot=(& git -C $project rev-parse --show-toplevel 2>$null | Out-String).Trim()
if($LASTEXITCODE -ne 0 -or
   !([IO.Path]::GetFullPath($gitRoot).TrimEnd([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar)).Equals($project,[StringComparison]::OrdinalIgnoreCase)){
    throw "PROJECT_NOT_GIT_WORKTREE_ROOT"
}
$gitDir=(& git -C $project rev-parse --absolute-git-dir 2>$null | Out-String).Trim()
if($LASTEXITCODE -ne 0 -or !(($gitDir -replace '\\','/') -match '/worktrees/[^/]+$')){
    throw "REFUSING_NON_LINKED_GIT_WORKTREE"
}
$branchName=(& git -C $project symbolic-ref --quiet HEAD 2>$null | Out-String).Trim()
if($LASTEXITCODE -eq 0 -or $branchName){throw "REFUSING_ATTACHED_GIT_BRANCH"}
$sha=(& git -C $project rev-parse HEAD 2>$null | Out-String).Trim()
if($LASTEXITCODE -ne 0 -or $sha -ne $ExpectedSHA){throw "PROJECT_SOURCE_SHA_NOT_PINNED:$sha"}
$worktreeListing=@(& git -C $project worktree list --porcelain 2>$null)
if($LASTEXITCODE -ne 0){throw "GIT_WORKTREE_REGISTRY_UNAVAILABLE"}
$entries=(($worktreeListing -join [Environment]::NewLine) -split '(?:\r?\n){2,}')
if($entries.Count -lt 2){throw "NO_INDEPENDENT_PRIMARY_WORKTREE"}
$confirmed=$false
$registeredRoots=New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
foreach($entry in $entries){
    $lines=$entry -split '\r?\n'
    if($lines.Count -lt 3 -or !$lines[0].StartsWith('worktree ')){continue}
    $entryRoot=[IO.Path]::GetFullPath($lines[0].Substring(9)).TrimEnd([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar)
    # Registered siblings (including primary) are real source checkouts,
    # not safe output directories. Fail closed on any linked path alias.
    Assert-NoLinkedPathComponents $entryRoot
    [void]$registeredRoots.Add($entryRoot)
    if($entryRoot.Equals($project,[StringComparison]::OrdinalIgnoreCase)){
        if(!($lines -contains 'detached') -or !($lines -contains ("HEAD "+$ExpectedSHA))){
            throw "GIT_WORKTREE_REGISTRY_NOT_DETACHED_AT_SOURCE"
        }
        $confirmed=$true
    }
}
if(!$confirmed){throw "PROJECT_NOT_REGISTERED_DISPOSABLE_WORKTREE"}
if($registeredRoots.Count -lt 2){throw "GIT_WORKTREE_REGISTRY_INCOMPLETE"}
# P0: an apparently external EvidenceDir can be INSIDE ANOTHER
# registered worktree, or enclose it. Both must be rejected BEFORE Unity.
# Use the existing canonical/reparse-checked paths and separator boundaries.
foreach($otherRoot in $registeredRoots){
    if($evidence.Equals($otherRoot,[StringComparison]::OrdinalIgnoreCase) -or
       $evidence.StartsWith($otherRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or
       $evidence.StartsWith($otherRoot+[IO.Path]::AltDirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or
       $otherRoot.StartsWith($evidence+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or
       $otherRoot.StartsWith($evidence+[IO.Path]::AltDirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){
        throw "RECEIPT_OVERLAPS_REGISTERED_WORKTREE:$otherRoot"
    }
}
$dirty=(& git -C $project status --porcelain --untracked-files=no | Out-String).Trim()
if($LASTEXITCODE -ne 0 -or $dirty){throw "REFUSING_TRACKED_DIRTY_PROJECT"}
# Reject all noncache untracked project files, including extra Unity source.
# Explicit Unity caches are allowed; source roots are checked for ignored
# files as well, so even a malicious .gitignore cannot hide a .cs/.asmdef.
$untracked=@(& git -C $project -c core.quotePath=false ls-files --others --exclude-standard --full-name)
if($LASTEXITCODE -ne 0){throw "UNTRACKED_AUDIT_UNAVAILABLE"}
foreach($candidate in $untracked){
    $p=($candidate -replace '\\','/')
    if(!$p){continue}
    if($p -eq ".b01-60-disposable-qa"){continue}
    if($p -match '^(Library|Temp|Logs|obj|Obj|\.vs)/'){continue}
    throw "REFUSING_UNTRACKED_PROJECT_FILE:$p"
}
$ignoredSources=@(& git -C $project -c core.quotePath=false ls-files --others --ignored --exclude-standard -- Assets Packages ProjectSettings)
if($LASTEXITCODE -ne 0){throw "IGNORED_SOURCE_AUDIT_UNAVAILABLE"}
if($ignoredSources.Count -gt 0){throw "REFUSING_IGNORED_UNITY_SOURCE:$($ignoredSources[0])"}
$projectVersion=Get-Content -LiteralPath (Join-Path $project "ProjectSettings/ProjectVersion.txt") -Raw
if($projectVersion -notmatch 'm_EditorVersion:\s*6000\.3\.14f1'){throw "PROJECT_UNITY_VERSION_MISMATCH"}
if($PreflightOnly){
    Write-Host "PARTSIZE_DISPOSABLE_PREFLIGHT_PASS source=$sha; no Unity editor executed; G1 NOT_RUN"
    return
}
# Atomic, single-use claim of this disposable worktree before actual
# Unity execution. A second invocation refuses to reuse the same checkout.
$stamp=[IO.File]::Open($usedMarker,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
try {
    $bytes=[Text.Encoding]::UTF8.GetBytes("PARTSIZE_NATIVE_SESSION source=$sha"+[Environment]::NewLine)
    $stamp.Write($bytes,0,$bytes.Length)
} finally { $stamp.Dispose() }
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
$receipt=@{source_sha=$ExpectedSHA; unity_version=$ExpectedUnity; status="RAW_NATIVE_FILES_ONLY_NOT_G1"; EditMode=@{}; PlayMode=@{}}
foreach($stage in @("EditMode","PlayMode")){
    $assembly=if($stage -eq "EditMode"){"B01.PARTSIZE001.EditModeTests"}else{"B01.PARTSIZE001.PlayModeTests"}
    $xml=Join-Path $evidence ($stage+".xml")
    $log=Join-Path $evidence ($stage+".log")
    if(Test-Path -LiteralPath $xml){Remove-Item -LiteralPath $xml -Force}
    if(Test-Path -LiteralPath $log){Remove-Item -LiteralPath $log -Force}
    # Real Unity NUnit execution; no mock output or guess about results.
    & $UnityEditor -batchmode -nographics -projectPath $project -runTests -testPlatform $stage -assemblyNames $assembly -testResults $xml -logFile $log
    $code=$LASTEXITCODE
    if($null -eq $code){$code=900}
    $xmlHash=if(Test-Path -LiteralPath $xml){(Get-FileHash -Algorithm SHA256 -LiteralPath $xml).Hash.ToLowerInvariant()}else{$null}
    $logHash=if(Test-Path -LiteralPath $log){(Get-FileHash -Algorithm SHA256 -LiteralPath $log).Hash.ToLowerInvariant()}else{$null}
    $receipt[$stage]=@{exit_code=[int]$code; xml_sha256=$xmlHash; log_sha256=$logHash; assembly=$assembly}
}
$path=Join-Path $evidence "receipt.json"
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $path -Encoding utf8
Write-Host "PARTSIZE_RAW_NATIVE_EVIDENCE_WRITTEN $path"
Write-Host "Independent B01-60 evidence review required; no auto G1 or G2."
if($receipt.EditMode.exit_code -ne 0 -or $receipt.PlayMode.exit_code -ne 0){exit 2}
