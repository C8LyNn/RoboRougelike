#requires -Version 5.1
<#
B01-60 P17/W22 #713: manually opted-in R5 PARTIAL native receiver.
Distinct from the frozen/working W21 #711 V2 operator.
G1 FULL R5 remains HOLD: original R5 ColdRestart expects gen4 after Save emits gen5.
#>
[CmdletBinding()]
param([string]$RepoPath='.',[string]$ScratchParent='',
      [string]$UnityPath='',[switch]$Run)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$exitCode=2
function Need([bool]$ok,[string]$why) {
 if(-not $ok){throw "B0160_W22_FAIL_CLOSED: $why"}
}
function Invoke-LocalGit([string[]]$args2) {
 $v=@(& git @args2 2>&1)
 Need ($LASTEXITCODE -eq 0) 'Local-only Git source identity command failed'
 return ($v -join [Environment]::NewLine).Trim()
}
function IsChild([string]$a,[string]$b) {
 $a=[IO.Path]::GetFullPath($a).TrimEnd('\','/')+'\'
 $b=[IO.Path]::GetFullPath($b).TrimEnd('\','/')+'\'
 return $a.StartsWith($b,[StringComparison]::OrdinalIgnoreCase)
}
try {
 Need ($env:OS -eq 'Windows_NT') 'Windows only'
 $null=Get-Command python -ErrorAction Stop
 $null=Get-Command git -ErrorAction Stop
 $python=@(& python --version 2>&1)
 Need ($LASTEXITCODE -eq 0 -and ($python -join ' ') -match '^Python 3\.') 'Python 3 required'
 $repo=(Resolve-Path -LiteralPath $RepoPath -ErrorAction Stop).Path
 Need (([IO.Path]::GetFullPath((Invoke-LocalGit @('-C',$repo,'rev-parse','--show-toplevel')))).TrimEnd('\','/') -ieq
       ([IO.Path]::GetFullPath($repo)).TrimEnd('\','/')) 'Run from exact Git root'
 $head=Invoke-LocalGit @('-C',$repo,'rev-parse','HEAD')
 Need ($head -match '^[0-9a-f]{40}$') 'No exact source Git HEAD'
 $w21='02272e8b0facc00d3943672770251b1d01182b85'
 Need ((Invoke-LocalGit @('-C',$repo,'merge-base',$w21,$head)) -eq $w21) 'W22 must descend frozen W21'
 $file='Tools/B01/B01-60/P17W22R5PartialNative/Invoke-B0160W22R5.ps1'
 $runner='Tools/B01/B01-60/P17W22R5PartialNative/r5_native.py'
 $tests='Tools/B01/B01-60/P17W22R5PartialNative/test_r5_native.py'
 Need ([IO.Path]::GetFullPath((Join-Path $repo $file)) -ieq [IO.Path]::GetFullPath($PSCommandPath)) 'Launcher must be Git tracked'
 foreach($path in @($file,$runner,$tests)) {
   Need ((Invoke-LocalGit @('-C',$repo,'hash-object','--',$path)) -eq
         (Invoke-LocalGit @('-C',$repo,'rev-parse',"HEAD:$path"))) "Changed/untracked W22 source: $path"
 }
 $r5='cc7f73541517a18f4479bb155534bd4832d9985e'
 Need ((Invoke-LocalGit @('-C',$repo,'cat-file','-t',$r5)) -eq 'commit') 'R5 #690 Git SHA not present locally: no network fetch'
 if([string]::IsNullOrWhiteSpace($ScratchParent)){$ScratchParent=$env:TEMP}
 $parent=(Resolve-Path -LiteralPath $ScratchParent -ErrorAction Stop).Path
 Need (-not $parent.StartsWith('\\')) 'Network scratch refused'
 Need ($parent -ine $repo -and -not(IsChild $parent $repo) -and -not(IsChild $repo $parent)) 'Scratch must be disjoint from repository'
 $p=Get-Item -LiteralPath $parent -ErrorAction Stop
 while($null -ne $p) {
   Need (-not [bool]($p.Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Junction/symlink ancestor refused'
   $p=$p.Parent
 }
 # Git-for-Windows long paths are configured for this process only. No global/user config writes.
 $idx=0
 if(-not [string]::IsNullOrWhiteSpace($env:GIT_CONFIG_COUNT)){$idx=[int]$env:GIT_CONFIG_COUNT}
 $env:GIT_CONFIG_COUNT=[string]($idx+1)
 [Environment]::SetEnvironmentVariable(('GIT_CONFIG_KEY_'+$idx),'core.longpaths','Process')
 [Environment]::SetEnvironmentVariable(('GIT_CONFIG_VALUE_'+$idx),'true','Process')
 $py=Join-Path $repo $runner
 Write-Host "B0160_W22_EXACT_HEAD=$head ORIGINAL_R5=$r5"
 Write-Host 'B0160_W22_SOURCE_ONLY_PREFLIGHT_R5_SIX_PRIVATE_SCRATCH_PROJECTS'
 & python $py --mode preflight --scratch-parent $parent *>&1
 Need ($LASTEXITCODE -eq 0) 'R5 original Git ZIP / private preflight not accepted'
 if(-not $Run) {
   $exitCode=0
   Write-Host 'B0160_W22_SOURCE_PASS_NATIVE_G1_NOT_RUN_FULL_R5_HOLD'
 } else {
   if([string]::IsNullOrWhiteSpace($UnityPath)) {
     $UnityPath=Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.3.14f1/Editor/Unity.exe'
   }
   Need (Test-Path -LiteralPath $UnityPath -PathType Leaf) 'Unity Hub 6000.3.14f1 missing; no fake native G1'
   $unity=(Resolve-Path -LiteralPath $UnityPath).Path
   Need ($unity -match '[\\/]6000\.3\.14f1[\\/]Editor[\\/]Unity\.exe$') 'Exact Unity Hub Editor path required'
   Need (-not [bool]((Get-Item -LiteralPath $unity).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Linked Editor rejected'
   Write-Host "B0160_W22_UNITY_EDITOR_SHA256=$((Get-FileHash -LiteralPath $unity -Algorithm SHA256).Hash.ToLowerInvariant())"
   Write-Host 'Confirm Unity 6000.3.14f1 licensed/activated in your own Unity Hub.'
   Write-Host 'Runs ONLY six R5 original PlayMode/PhysX tests, each in its own private Unity project, AFTER private save-path guard.'
   Write-Host 'Original R5 ColdRestart test is SOURCE_RED (gen4 vs gen5) and NOT run; FULL R5 G1 remains HOLD.'
   $answer=Read-Host 'To authorize this SINGLE R5 PARTIAL native run type exactly B0160 R5 NATIVE PARTIAL'
   Need ($answer -ceq 'B0160 R5 NATIVE PARTIAL') 'Operator declined exact consent'
   & python $py --mode native --unity $unity --scratch-parent $parent --operator-consent 'B0160 R5 NATIVE PARTIAL' *>&1
   Need ($LASTEXITCODE -eq 0) 'Native R5 partial failed (do not claim G1)'
   $exitCode=0
   Write-Host 'B0160_W22_NATIVE_R5_PARTIAL_ONLY_FULL_G1_HOLD'
 }
}
catch {
 $exitCode=2
 if($Run){$exitCode=3}
 Write-Warning ('B0160_W22_HOLD: '+($_.Exception.Message -replace '[\r\n]+',' '))
}
finally {
 Write-Host "B0160_W22_EXIT=$exitCode REAL_G1_ONLY_IF_REAL_EDITOR_AND_XML VERIFIED_NO_UPLOAD_NO_CLEANUP_NO_MERGE"
}
exit $exitCode
