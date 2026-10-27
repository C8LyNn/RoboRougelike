# B01-23 W26 native operator, ONLY disposable pinned Unity6000.3.14f1 worktree.
# Run only after licensed Unity is installed. Never edits or stages Codex source.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$UnityEditor,
    [Parameter(Mandatory=$true)][string]$ProjectPath,
    [Parameter(Mandatory=$true)][string]$EvidenceDir
)
$ErrorActionPreference="Stop"
$ExpectedSHA="be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
$ExpectedUnity="6000.3.14f1"
$project=[IO.Path]::GetFullPath($ProjectPath)
$evidence=[IO.Path]::GetFullPath($EvidenceDir)
if(!(Test-Path -LiteralPath $UnityEditor -PathType Leaf)){throw "UNITY_EDITOR_NOT_FOUND"}
if(!(Test-Path -LiteralPath (Join-Path $project ".b01-60-disposable-qa") -PathType Leaf)){throw "REFUSING_NON_DISPOSABLE_PROJECT"}
if(!(Test-Path -LiteralPath (Join-Path $project "Assets/PARTSIZE001/Tests/PlayMode/PartSize001PlayModeTests.cs"))){throw "PINNED_PARTSIZE_TEST_ASSET_MISSING"}
if($evidence.StartsWith($project+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw "RECEIPT_MUST_BE_OUTSIDE_PROJECT"}
$sha=(& git -C $project rev-parse HEAD 2>$null | Out-String).Trim()
if($LASTEXITCODE -ne 0 -or $sha -ne $ExpectedSHA){throw "PROJECT_SOURCE_SHA_NOT_PINNED:$sha"}
$dirty=(& git -C $project status --porcelain --untracked-files=no | Out-String).Trim()
if($LASTEXITCODE -ne 0 -or $dirty){throw "REFUSING_TRACKED_DIRTY_PROJECT"}
$projectVersion=Get-Content -LiteralPath (Join-Path $project "ProjectSettings/ProjectVersion.txt") -Raw
if($projectVersion -notmatch 'm_EditorVersion:\s*6000\.3\.14f1'){throw "PROJECT_UNITY_VERSION_MISMATCH"}
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
