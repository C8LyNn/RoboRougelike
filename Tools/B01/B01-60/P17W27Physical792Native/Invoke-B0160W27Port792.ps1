#requires -Version 5.1
<#
B01-60 W27 P0 physical native FOUR original #792 PlayMode cases.
Safe default: actual Git/source preflight and four ephemeral private Unity
project stage/rollback. -Run additionally requires external G0 approval AND
typed exact interactive consent; NO SSH/admin/auto fetch/merge/upload/delete.
#>
[CmdletBinding()]
param(
 [string]$RepoPath='.',
 [string]$ScratchParent='',
 [string]$UnityPath='',
 [switch]$Run,
 [switch]$G0Reviewed
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$exitCode=2
function Need([bool]$ok,[string]$msg) {
 if(-not $ok){throw 'B0160_W27_PORT792_HOLD: '+$msg}
}
function LocalGit([string[]]$a) {
 $v=@(& git @a 2>&1)
 Need ($LASTEXITCODE -eq 0) 'Git original local command failed (no fetch)'
 return ($v -join [Environment]::NewLine).Trim()
}
function Inside([string]$a,[string]$b){
 $a=[IO.Path]::GetFullPath($a).TrimEnd('\','/')+'\'
 $b=[IO.Path]::GetFullPath($b).TrimEnd('\','/')+'\'
 return $a.StartsWith($b,[StringComparison]::OrdinalIgnoreCase)
}
try {
 Need ($env:OS -eq 'Windows_NT') 'Windows only'
 $null=Get-Command git -ErrorAction Stop
 $null=Get-Command python -ErrorAction Stop
 $version=@(& python --version 2>&1)
 Need ($LASTEXITCODE -eq 0 -and ($version -join ' ') -match '^Python 3\.') 'Python3 missing'
 $repo=(Resolve-Path -LiteralPath $RepoPath -ErrorAction Stop).Path
 $root=LocalGit @('-C',$repo,'rev-parse','--show-toplevel')
 Need ([IO.Path]::GetFullPath($root).TrimEnd('\','/') -ieq
       [IO.Path]::GetFullPath($repo).TrimEnd('\','/')) 'Must use repo top-level'
 $head=LocalGit @('-C',$repo,'rev-parse','HEAD')
 $parent='8057c4da7edc49f67ec2de505c4d56a57eba4483'
 Need ((LocalGit @('-C',$repo,'merge-base',$parent,$head)) -eq $parent) 'Not selected W24 QA descendant'
 $owner='16cb2a7b5158605b15191b6074f9e392e0d1ebd3'
 Need ((LocalGit @('-C',$repo,'cat-file','-t',$owner)) -eq 'commit') 'Owner #772 must exist in local Git history; NO automatic fetch'
 $paths=@(
  'Tools/B01/B01-60/P17W27Physical792Native/port792_native.py',
  'Tools/B01/B01-60/P17W27Physical792Native/test_port792_native.py',
  'Tools/B01/B01-60/P17W27Physical792Native/Invoke-B0160W27Port792.ps1'
 )
 foreach($p in $paths) {
  Need ((LocalGit @('-C',$repo,'hash-object','--',$p)) -eq
       (LocalGit @('-C',$repo,'rev-parse',"HEAD:$p"))) "Modified W27 runner file: $p"
 }
 Need ([IO.Path]::GetFullPath((Join-Path $repo $paths[2])) -ieq
       [IO.Path]::GetFullPath($PSCommandPath)) 'Must launch tracked original W27 script'
 if([string]::IsNullOrWhiteSpace($ScratchParent)){$ScratchParent=$env:TEMP}
 $scratch=(Resolve-Path -LiteralPath $ScratchParent -ErrorAction Stop).Path
 Need ($scratch -notmatch '^\\') 'UNC scratch not allowed'
 Need ($scratch -ine $repo -and -not(Inside $scratch $repo) -and
      -not(Inside $repo $scratch)) 'Scratch inside or parent of game checkout denied'
 $item=Get-Item -LiteralPath $scratch -ErrorAction Stop
 while($null -ne $item){
  Need (-not [bool]($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Linked/symlink scratch ancestor refused'
  $item=$item.Parent
 }
 $i=0
 if(-not [string]::IsNullOrEmpty($env:GIT_CONFIG_COUNT)){$i=[int]$env:GIT_CONFIG_COUNT}
 $env:GIT_CONFIG_COUNT=[string]($i+1)
 [Environment]::SetEnvironmentVariable(('GIT_CONFIG_KEY_'+$i),'core.longpaths','Process')
 [Environment]::SetEnvironmentVariable(('GIT_CONFIG_VALUE_'+$i),'true','Process')
 $py=Join-Path $repo $paths[0]
 & python $py --mode preflight --scratch-parent $scratch *>&1
 Need ($LASTEXITCODE -eq 0) 'Original #792 real source staging preflight failed'
 if(-not $Run) {
  Write-Host 'B0160_W27_PORT792_SOURCE_ONLY_PASS G1_NOT_RUN'
  $exitCode=0
 } else {
  Need ($G0Reviewed.IsPresent) 'Must review independent original source G0 #792 and selected #787/#797/#780 separately before native'
  if([string]::IsNullOrEmpty($UnityPath)){$UnityPath=Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.3.14f1/Editor/Unity.exe'}
  Need (Test-Path -LiteralPath $UnityPath -PathType Leaf) 'No genuine locally licensed Unity Hub 6000.3.14f1 executable'
  $editor=(Resolve-Path -LiteralPath $UnityPath).Path
  Need ($editor -match '[\\/]6000\.3\.14f1[\\/]Editor[\\/]Unity\.exe$') 'Wrong Unity Editor version or path'
  Need (-not [bool]((Get-Item -LiteralPath $editor).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Unity Editor executable symlink denied'
  Write-Host ('B0160_W27_REAL_UNITY_EXE_SHA256='+((Get-FileHash -LiteralPath $editor -Algorithm SHA256).Hash.ToLowerInvariant()))
  Write-Host '4 original #792 physical Rigidbody/AtomicDockLedger Unity Test Framework PlayMode methods in 4 private projects.'
  Write-Host 'NO canonical #787/#797/#780 Save implementation installed: real game Save/Load G1 remains NOT_RUN.'
  Write-Host 'Original SOURCE_G0 must be reviewed independently. Consent does NOT make it accepted.'
  Write-Host 'This may use Unity license seats. Results only saved to new private scratch. No upload, merge or cleanup.'
  $token=Read-Host 'Type exactly B0160 W27 PORT792 FOUR NATIVE PHYSX G0 REVIEWED'
  Need ($token -ceq 'B0160 W27 PORT792 FOUR NATIVE PHYSX G0 REVIEWED') 'Native physical test consent absent'
  & python $py --mode native --unity $editor --scratch-parent $scratch --operator-consent 'B0160 W27 PORT792 FOUR NATIVE PHYSX G0 REVIEWED' *>&1
  Need ($LASTEXITCODE -eq 0) 'Real #772 Unity tests failed/partial; G1 HOLD, examine local raw logs'
  Write-Host 'B0160_W27_PORT792_REAL_UNITY_RESULTS_PENDING_INDEPENDENT_XML_EDITOR_LOG_REVIEW'
  $exitCode=0
 }
}
catch {
 $exitCode=2
 if($Run){$exitCode=3}
 Write-Warning ($_.Exception.Message -replace '[\r\n]+',' ')
}
finally {
 Write-Host "B0160_W27_PORT792_EXIT=$exitCode FULL_CANONICAL_SAVE_G1_NOT_RUN"
}
exit $exitCode
