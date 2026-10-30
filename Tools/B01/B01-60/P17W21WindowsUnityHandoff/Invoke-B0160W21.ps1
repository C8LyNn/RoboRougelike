#requires -Version 5.1
<#
B01-60 #687 Windows ONE-COMMAND local manual Unity V2 operator handoff.
Versioned wrapper of frozen W20 #684: no direct access, no network fetch,
no automatic upload/cleanup. W20 itself demands exact typed native consent.
#>
[CmdletBinding()]
param([string]$RepoPath='.',[string]$ScratchParent='',[string]$UnityPath='',[switch]$Run)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$W20='c75cb6e52a0de11f27b570c34e08d32082af421f'
$dir='';$ret=2;$head=''
function Need([bool]$ok,[string]$reason){if(-not $ok){throw "B0160_W21_HOLD: $reason"}}
function Invoke-LocalGit([string[]]$argv){
  $output=@(& git @argv 2>&1)
  Need ($LASTEXITCODE -eq 0) 'Local Git failed; no remote fetch permitted'
  return ($output -join [Environment]::NewLine).Trim()
}
function Inside([string]$a,[string]$b){
 $a=[IO.Path]::GetFullPath($a).TrimEnd('\','/')+'\'
 $b=[IO.Path]::GetFullPath($b).TrimEnd('\','/')+'\'
 return $a.StartsWith($b,[StringComparison]::OrdinalIgnoreCase)
}
function CheckNoLinks([string]$path){
 $at=(Get-Item -LiteralPath $path -ErrorAction Stop)
 while($null -ne $at){
   Need (-not [bool]($at.Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Linked or junction ancestor rejected'
   $at=$at.Parent
 }
}
try {
 Need ($env:OS -eq 'Windows_NT') 'Local Windows only'
 $null=Get-Command git -ErrorAction Stop
 $null=Get-Command python -ErrorAction Stop
 $py=@(& python --version 2>&1)
 Need ($LASTEXITCODE -eq 0 -and ($py -join ' ') -match '^Python 3\.') 'Python 3 required'
 $repo=(Resolve-Path -LiteralPath $RepoPath -ErrorAction Stop).Path
 Need ([IO.Path]::GetFullPath((Invoke-LocalGit @('-C',$repo,'rev-parse','--show-toplevel'))) -ieq [IO.Path]::GetFullPath($repo)) 'RepoPath must be exact Git root'
 $head=Invoke-LocalGit @('-C',$repo,'rev-parse','HEAD')
 Need ($head -match '^[0-9a-f]{40}$') 'Invalid source SHA'
 $mine='Tools/B01/B01-60/P17W21WindowsUnityHandoff/Invoke-B0160W21.ps1'
 Need ([IO.Path]::GetFullPath((Join-Path $repo $mine)) -ieq [IO.Path]::GetFullPath($PSCommandPath)) 'Run Git-tracked W21 script only'
 foreach($path in @($mine,'Tools/B01/B01-60/P17W21WindowsUnityHandoff/verify_evidence.py')) {
   Need ((Invoke-LocalGit @('-C',$repo,'hash-object','--',$path)) -eq
         (Invoke-LocalGit @('-C',$repo,'rev-parse',"HEAD:$path"))) 'W21 launcher/verifier modified relative to HEAD'
 }
 Need ((Invoke-LocalGit @('-C',$repo,'merge-base',$W20,$head)) -eq $W20) 'W21 is not descendant of frozen W20 #684'
 $prior='Tools/B01/B01-60/P17W20WindowsUnityOperator/Invoke-B0160WindowsUnity.ps1'
 Need ((Invoke-LocalGit @('-C',$repo,'rev-parse',"$($W20):$prior")) -eq
       '6e9b549ae36e9fbf6ba9260aeb1b4739d997fed0') 'Frozen W20 operator blob drift'
 if([string]::IsNullOrWhiteSpace($ScratchParent)){$ScratchParent=$env:TEMP}
 $parent=(Resolve-Path -LiteralPath $ScratchParent -ErrorAction Stop).Path
 Need (-not $parent.StartsWith('\\')) 'Network/UNC scratch forbidden'
 CheckNoLinks $parent
 Need ($parent -ine $repo -and -not(Inside $parent $repo) -and -not(Inside $repo $parent)) 'Scratch must be disjoint from original repository'
 $dir=Join-Path $parent ('B0160W21-'+[guid]::NewGuid().ToString('N'))
 Need (-not(Test-Path -LiteralPath $dir)) 'Refuse existing run directory'
 New-Item -ItemType Directory -Path $dir -ErrorAction Stop | Out-Null
 $source=Join-Path $dir 'frozen-W20-source'
 $results=Join-Path $dir 'W20-private-results'
 New-Item -ItemType Directory -Path $results -ErrorAction Stop | Out-Null
 Write-Host "B0160_W21_NEW_PRIVATE_RUN=$dir"
 $cl=@(& git -c core.hooksPath=NUL clone --no-hardlinks --no-checkout -- $repo $source 2>&1)
 Need ($LASTEXITCODE -eq 0) 'Local detached clone failed'
 $null=Invoke-LocalGit @('-C',$source,'-c','core.hooksPath=NUL','checkout','--detach',$W20)
 Need ((Invoke-LocalGit @('-C',$source,'rev-parse','HEAD')) -eq $W20) 'Frozen W20 checkout failed'
 $old=Join-Path $source $prior
 Need ((Invoke-LocalGit @('-C',$source,'hash-object','--',$prior)) -eq
       '6e9b549ae36e9fbf6ba9260aeb1b4739d997fed0') 'W20 original operator content not exact'
 # The original V2 runner needs historical exact original Git objects. A normal
 # clone copies reachable refs but may omit FETCH_HEAD-only historical PR objects.
 # Rehydrate ONLY the new scratch clone from the local checkout; no network.
 # Branch the pinned objects ONLY IN SCRATCH: git clone follows heads, not unrelated tags.
 $pins=@('d94400d95d13db7f27e1533df05c9292d5380431',
         'cb2b3a76e84266e2730fffc4ab048c1039f0652f',
         '33a32b8bd9ab40467ab1a17b5684926a37dbd4d3',
         '33094f154c7b91119b36d58aab285e6e83c5de57')
 for($i=0;$i -lt $pins.Count;$i++){
   $pin=$pins[$i]
   Need ((Invoke-LocalGit @('-C',$repo,'cat-file','-t',$pin)) -eq 'commit') "Historical Git object missing locally: $pin; no automatic remote fetch"
   $null=Invoke-LocalGit @('-C',$source,'fetch','--no-tags','--',$repo,$pin)
   $null=Invoke-LocalGit @('-C',$source,'branch',('b0160-w21-pin-'+$i),$pin)
   Need ((Invoke-LocalGit @('-C',$source,'rev-parse',('refs/heads/b0160-w21-pin-'+$i))) -eq $pin) 'Local-only pinned source object failed'
 }
 Write-Host 'B0160_W21_OFFLINE_HISTORY_4_OBJECTS_PINNED_TO_SCRATCH_ONLY'
 $shell=(Get-Process -Id $PID).Path
 Need (Test-Path -LiteralPath $shell -PathType Leaf) 'No local PowerShell interpreter'
 $opts=@('-NoProfile','-File',$old,'-RepoPath',$source,'-ScratchParent',$results)
 if(-not [string]::IsNullOrWhiteSpace($UnityPath)){$opts+=@('-UnityPath',$UnityPath)}
 if($Run){$opts+='-Run'}
 & $shell @opts *>&1 | Tee-Object -FilePath (Join-Path $dir 'w20-console.log')
 $w20Exit=$LASTEXITCODE
 $evidenceDirs=@(Get-ChildItem -LiteralPath $results -Directory -Filter 'B0160W20-*')
 Need ($evidenceDirs.Count -eq 1) 'Exactly one frozen W20 evidence directory required'
 $ev=Join-Path $evidenceDirs[0].FullName 'evidence'
 $v=Join-Path $repo 'Tools/B01/B01-60/P17W21WindowsUnityHandoff/verify_evidence.py'
 & python $v --evidence $ev *>&1 | Tee-Object -FilePath (Join-Path $dir 'w21-verify.log')
 $verifyExit=$LASTEXITCODE
 $handoff=@{package='B01-60-P17W21-WINDOWS-HANDOFF';w21_head=$head;
   frozen_w20_source=$W20;w20_exit=$w20Exit;audit_exit=$verifyExit;
   native_test_scope='ORIGINAL_V2_2_EDIT_14_PLAY_ONLY';r5_690_native='NOT_EXECUTED';
   canonical_691_native='NOT_EXECUTED';source_G0='SEPARATE';integrated=$false;
   uploaded=$false;merged=$false;end_utc=(Get-Date).ToUniversalTime().ToString('o')}
 $handoff | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $dir 'w21-handoff.json') -Encoding UTF8
 Need ($w20Exit -eq 0 -and $verifyExit -eq 0) 'W20 or independent evidence check rejected; native HOLD'
 $ret=0
 Write-Host 'B0160_W21_OPERATOR_SOURCE_EVIDENCE_VERIFIED_V2_ONLY_RELEASE_HOLD'
}
catch {
 $ret=2
 if($Run){$ret=3}
 Write-Warning ('B0160_W21_FAIL_CLOSED: '+($_.Exception.Message -replace '[\r\n]+',' '))
}
finally {
 if($dir){Write-Host "B0160_W21_LOCAL_EVIDENCE_NO_UPLOAD_NO_DELETION=$dir"}
 Write-Host "B0160_W21_EXIT=$ret NATIVE_R5_NOT_RUN G2_HOLD NO_MERGE"
}
exit $ret
