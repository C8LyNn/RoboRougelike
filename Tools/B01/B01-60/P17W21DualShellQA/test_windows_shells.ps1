#requires -Version 5.1
# Real Windows PowerShell 5.1 and PowerShell7 cross-shell SOURCE QA.
# Calls the actual frozen W20 operator, never licensed Unity or owner PC.
[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][ValidateSet("WindowsPowerShell51","PowerShell7")][string]$Shell,
 [Parameter(Mandatory=$true)][string]$Repo,
 [Parameter(Mandatory=$true)][string]$TempParent,
 [Parameter(Mandatory=$true)][string]$ProofParent
)
Set-StrictMode -Version Latest
$ErrorActionPreference="Stop"
$Base="c75cb6e52a0de11f27b570c34e08d32082af421f"
$path="Tools/B01/B01-60/P17W20WindowsUnityOperator/Invoke-B0160WindowsUnity.ps1"
function Need([bool]$ok,[string]$msg){if(-not $ok){throw "B01_60_W21_SOURCE_QA_DENIED: $msg"}}
function GitRead([string[]]$arg) {
 $out=@(& git @arg 2>&1)
 Need ($LASTEXITCODE -eq 0) ("Original git source unavailable, rc="+$LASTEXITCODE+", argv="+($arg -join ",")+", output="+($out -join ";"))
 return ($out -join [Environment]::NewLine).Trim()
}
function VerifyEvidence([string]$scratch,[string]$expectedStatus,[string]$head) {
 $dir=Join-Path $scratch "evidence"
 $p=Join-Path $dir "operator-report.json"
 $m=Join-Path $dir "sha256-manifest.json"
 $h=Join-Path $dir "sha256-manifest.json.sha256"
 Need ((Test-Path -LiteralPath $p -PathType Leaf) -and (Test-Path -LiteralPath $m -PathType Leaf) -and
       (Test-Path -LiteralPath $h -PathType Leaf)) "actual original Windows evidence missing"
 $r=Get-Content -LiteralPath $p -Raw | ConvertFrom-Json
 Need ($r.source_sha -ceq $head -and $r.native_g1 -ceq "NOT_RUN" -and
       $r.selected_R4_sha -ceq "09c1809b73324a6d31c13e5d0143de62ac1e3ca6" -and
       $r.result -ceq $expectedStatus -and -not $r.merged -and -not $r.uploaded) "fake G1 or altered status"
 Need ((Get-FileHash -LiteralPath $m -Algorithm SHA256).Hash.ToLowerInvariant() -ceq
        (Get-Content -LiteralPath $h -Raw).Trim().ToLowerInvariant()) "original SHA256 manifest altered"
 $list=Get-Content -LiteralPath $m -Raw | ConvertFrom-Json
 Need ($list.unsigned -and $list.source_sha -ceq $head) "fake signed evidence"
 foreach($f in $list.files) {
  Need ($f.path -match '^[A-Za-z0-9_./-]+$' -and $f.path -notmatch '(^|/)[.][.](/|$)' -and
        -not $f.path.StartsWith("/")) "unsafe evidence path"
  $file=Join-Path $dir ($f.path.Replace("/",[IO.Path]::DirectorySeparatorChar))
  Need ((Test-Path -LiteralPath $file -PathType Leaf) -and
       -not [bool]((Get-Item -LiteralPath $file).Attributes -band [IO.FileAttributes]::ReparsePoint)) "foreign evidence symlink/file"
  Need ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant() -ceq $f.sha256) "original source-evidence file hash mismatch"
 }
 Need (-not(Test-Path -LiteralPath (Join-Path $dir "original-V2-native-only/native-verdict.json"))) "fake real Unity V2 verdict"
}
Need ($env:OS -ceq "Windows_NT") "REAL Windows shell only"
$root=(Resolve-Path -LiteralPath $Repo).Path
$temp=(Resolve-Path -LiteralPath $TempParent).Path
$proof=(Resolve-Path -LiteralPath $ProofParent).Path
Need ($root -ine $temp -and $root -ine $proof) "QA scratch overlapping source root"
$head=GitRead @("-C",$root,"rev-parse","HEAD")
Need ($head -match '^[0-9a-f]{40}$' -and
      (GitRead @("-C",$root,"merge-base",$Base,$head)) -ceq $Base) "not on W20 descendant"
Need ((GitRead @("-C",$root,"rev-parse",$Base+":"+$path)) -ceq
       "6e9b549ae36e9fbf6ba9260aeb1b4739d997fed0") "original W20 operator Git blob changed"
Need ((GitRead @("-C",$root,"rev-parse","HEAD:" + $path)) -ceq
       "6e9b549ae36e9fbf6ba9260aeb1b4739d997fed0") "old operator modified by W21"
$cmd=if($Shell -ceq "WindowsPowerShell51"){"powershell.exe"}else{"pwsh.exe"}
$exe=(Get-Command $cmd -ErrorAction Stop).Source
$v=@(& $exe -NoProfile -NonInteractive -Command '$PSVersionTable.PSVersion.Major' 2>&1)
Need ($LASTEXITCODE -eq 0) "Could not start selected real Windows shell"
$major=[int](($v -join [Environment]::NewLine).Trim())
Need (($Shell -ceq "WindowsPowerShell51" -and $major -eq 5) -or
      ($Shell -ceq "PowerShell7" -and $major -ge 7)) "wrong Windows PowerShell engine"
$operator=Join-Path $root $path
function InvokeOriginal([string[]]$extra,[string]$proofLog) {
 $argsList=@("-NoProfile","-NonInteractive","-ExecutionPolicy","Bypass","-File",$operator,
              "-RepoPath",$root,"-ScratchParent",$temp)+$extra
 & $exe @argsList *>&1 | Tee-Object -FilePath $proofLog
 return $LASTEXITCODE
}
$before=@(Get-ChildItem -LiteralPath $temp -Directory -Filter "B0160W20-*" | ForEach-Object {$_.FullName})
$rc=InvokeOriginal @() (Join-Path $proof ("W21-"+$Shell+"-preflight.log"))
Need ($rc -eq 0) "real original Windows source-only preflight failed"
$after=@(Get-ChildItem -LiteralPath $temp -Directory -Filter "B0160W20-*" | Where-Object {$before -notcontains $_.FullName})
Need ($after.Count -eq 1) "exact one newly created GUID scratch expected"
VerifyEvidence $after[0].FullName "PREFLIGHT_PASS_NATIVE_NOT_RUN" $head
Write-Host ("B01_60_W21_"+$Shell+"_ORIGINAL_SOURCE_PREFLIGHT_PASS")
$before=@(Get-ChildItem -LiteralPath $temp -Directory -Filter "B0160W20-*" | ForEach-Object {$_.FullName})
$missing="Z:\W21\missing\6000.3.14f1\Editor\Unity.exe"
$rc=InvokeOriginal @("-Run","-UnityPath",$missing) (Join-Path $proof ("W21-"+$Shell+"-missing-editor.log"))
Need ($rc -eq 3) "missing real Unity must exit3 without asking permission"
$after=@(Get-ChildItem -LiteralPath $temp -Directory -Filter "B0160W20-*" | Where-Object {$before -notcontains $_.FullName})
Need ($after.Count -eq 1) "missing-editor receipt missing its own GUID scratch"
VerifyEvidence $after[0].FullName "HOLD_FAIL_CLOSED" $head
Write-Host ("B01_60_W21_"+$Shell+"_MISSING_UNITY_NOT_RUN_EXIT3_PASS")
$clone=Join-Path $temp ("W21-tampered-"+[guid]::NewGuid().ToString("N"))
$null=@(& git -c core.hooksPath=NUL clone --no-hardlinks --no-checkout -- $root $clone 2>&1)
Need ($LASTEXITCODE -eq 0) "W21 disposable tamper clone failed"
$null=GitRead @("-C",$clone,"-c","core.hooksPath=NUL","checkout","--detach",$head)
$copy=Join-Path $clone $path
Add-Content -LiteralPath $copy -Value "# W21 test-only changed source, should fail exact Git HEAD check"
$out=@(& $exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $copy -RepoPath $clone -ScratchParent $proof 2>&1)
$rc=$LASTEXITCODE
$out | Out-File -LiteralPath (Join-Path $proof ("W21-"+$Shell+"-tamper-denied.log")) -Encoding UTF8
Need ($rc -eq 2 -and ($out -join [Environment]::NewLine) -match "Local operator script modified since Git HEAD") "tampered PS script did not fail closed"
Write-Host ("B01_60_W21_"+$Shell+"_TAMPERED_SCRIPT_DENIED_EXIT2_PASS")
$bad=@(& $exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $operator -RepoPath $root -ScratchParent $root 2>&1)
$rc=$LASTEXITCODE
$bad | Out-File -LiteralPath (Join-Path $proof ("W21-"+$Shell+"-unsafe-scratch.log")) -Encoding UTF8
Need ($rc -eq 2 -and ($bad -join [Environment]::NewLine) -match "Scratch parent must be disjoint") "game working tree allowed as scratch"
Write-Host ("B01_60_W21_"+$Shell+"_GAME_WORKTREE_SCRATCH_DENIED_EXIT2_PASS")
Write-Host ("B01_60_W21_"+$Shell+"_REAL_WINDOWS_FOUR_SOURCE_GATES_PASS_NATIVE_G1_NOT_RUN")
