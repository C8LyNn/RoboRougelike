#requires -Version 5.1
<#
B01-60 P17/W23: EXACT owner B01-23 #732 corrected R5 seven ORIGINAL Unity PlayMode cases.
This script DOES NOT validate formal G0 source acceptance. Operator must review it
independently and pass -SourceG0Reviewed to explicitly claim human G0 review.
Default is safe source-only preflight. No Unity/native activity unless -Run and
exact interactive typed consent.
#>
[CmdletBinding()]
param(
 [string]$RepoPath='.',
 [string]$ScratchParent='',
 [string]$UnityPath='',
 [switch]$Run,
 [switch]$SourceG0Reviewed
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$exitCode=2
function Need([bool]$ok,[string]$why) {
 if(-not $ok){throw "B0160_W23_FIXED7_FAIL_CLOSED: $why"}
}
function LocalGit([string[]]$a) {
 $v=@(& git @a 2>&1)
 Need ($LASTEXITCODE -eq 0) 'Local-only git command failed, no automatic fetch'
 return ($v -join [Environment]::NewLine).Trim()
}
function ChildOf([string]$a,[string]$b) {
 $a=[IO.Path]::GetFullPath($a).TrimEnd('\','/')+'\'
 $b=[IO.Path]::GetFullPath($b).TrimEnd('\','/')+'\'
 return $a.StartsWith($b,[StringComparison]::OrdinalIgnoreCase)
}
try {
 Need ($env:OS -eq 'Windows_NT') 'Windows only'
 $null=Get-Command git -ErrorAction Stop
 $null=Get-Command python -ErrorAction Stop
 $python=@(& python --version 2>&1)
 Need ($LASTEXITCODE -eq 0 -and ($python -join ' ') -match '^Python 3\.') 'Python3 required'
 $repo=(Resolve-Path -LiteralPath $RepoPath -ErrorAction Stop).Path
 Need (([IO.Path]::GetFullPath((LocalGit @('-C',$repo,'rev-parse','--show-toplevel')))).TrimEnd('\','/') -ieq
       ([IO.Path]::GetFullPath($repo)).TrimEnd('\','/')) 'Must start from exact Git repository root'
 $head=LocalGit @('-C',$repo,'rev-parse','HEAD')
 Need ($head -match '^[0-9a-f]{40}$') 'Invalid current Git HEAD'
 $w22='481bfb1cf2804d94fb49d7e6a57134e96c535b86'
 Need ((LocalGit @('-C',$repo,'merge-base',$w22,$head)) -eq $w22) 'Not on correct W22-descendant branch'
 $corrected='0bee4ce9928354433ec719c45a8186378361d46e'
 Need ((LocalGit @('-C',$repo,'cat-file','-t',$corrected)) -eq 'commit') 'Original #732 corrected source commit missing locally. No auto-fetch.'
 $own=@(
  'Tools/B01/B01-60/P17W23FixedR5SevenNative/run_fixed7.py',
  'Tools/B01/B01-60/P17W23FixedR5SevenNative/Invoke-B0160W23Fixed7.ps1',
  'Tools/B01/B01-60/P17W23FixedR5SevenNative/test_fixed7.py'
 )
 foreach($p in $own) {
  Need ((LocalGit @('-C',$repo,'hash-object','--',$p)) -eq
        (LocalGit @('-C',$repo,'rev-parse',"HEAD:$p"))) "Modified/untracked W23 native launcher: $p"
 }
 Need ([IO.Path]::GetFullPath((Join-Path $repo $own[1])) -ieq [IO.Path]::GetFullPath($PSCommandPath)) 'Operator must launch checked original script'
 if([string]::IsNullOrWhiteSpace($ScratchParent)){$ScratchParent=$env:TEMP}
 $parent=(Resolve-Path -LiteralPath $ScratchParent -ErrorAction Stop).Path
 Need (-not $parent.StartsWith('\\')) 'UNC scratch refused'
 Need ($parent -ine $repo -and -not(ChildOf $parent $repo) -and -not(ChildOf $repo $parent)) 'Scratch cannot overlap source checkout'
 $q=Get-Item -LiteralPath $parent -ErrorAction Stop
 while($null -ne $q) {
  Need (-not [bool]($q.Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Symlink/junction scratch ancestor denied'
  $q=$q.Parent
 }
 $idx=0
 if(-not [string]::IsNullOrWhiteSpace($env:GIT_CONFIG_COUNT)){$idx=[int]$env:GIT_CONFIG_COUNT}
 $env:GIT_CONFIG_COUNT=[string]($idx+1)
 [Environment]::SetEnvironmentVariable(('GIT_CONFIG_KEY_'+$idx),'core.longpaths','Process')
 [Environment]::SetEnvironmentVariable(('GIT_CONFIG_VALUE_'+$idx),'true','Process')
 $runner=Join-Path $repo $own[0]
 Write-Host "B0160_W23_FIXED7_HEAD=$head CORRECTED_R5=$corrected"
 & python $runner --mode preflight --scratch-parent $parent *>&1
 Need ($LASTEXITCODE -eq 0) 'Original #732 corrected R5 7 private source project preflight failed'
 if(-not $Run) {
  $exitCode=0
  Write-Host 'B0160_W23_FIXED7_SOURCE_PASS_OWNER_PC_NATIVE_G1_NOT_RUN'
 } else {
  Need ($SourceG0Reviewed.IsPresent) 'Native R5 blocked pending operator independent #732 source G0 review. Supply -SourceG0Reviewed only AFTER review.'
  if([string]::IsNullOrWhiteSpace($UnityPath)){
   $UnityPath=Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.3.14f1/Editor/Unity.exe'
  }
  Need (Test-Path -LiteralPath $UnityPath -PathType Leaf) 'Missing legally activated Unity Hub Editor 6000.3.14f1'
  $unity=(Resolve-Path -LiteralPath $UnityPath).Path
  Need ($unity -match '[\\/]6000\.3\.14f1[\\/]Editor[\\/]Unity\.exe$') 'Wrong Unity Editor version or path'
  Need (-not [bool]((Get-Item -LiteralPath $unity).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Linked Unity Editor denied'
  Write-Host "B0160_W23_FIXED7_UNITY_EXE_SHA256=$((Get-FileHash -LiteralPath $unity -Algorithm SHA256).Hash.ToLowerInvariant())"
  Write-Host 'You must have independently reviewed formal B01-40 source G0 for corrected owner #732; this local switch is NOT G0 acceptance.'
  Write-Host 'Runs original exact seven R5 corrected PlayMode/PhysX methods in seven distinct scratch Unity projects, each after private checkpoint probe.'
  Write-Host 'No upload, SSH, admin, deletion, merge or protected game project access. Native outputs remain NOT independently attested.'
  $answer=Read-Host 'Type exactly B0160 R5 FIXED7 G0 REVIEWED NATIVE to authorize this single operator run'
  Need ($answer -ceq 'B0160 R5 FIXED7 G0 REVIEWED NATIVE') 'Native explicit operator consent declined'
  & python $runner --mode native --unity $unity --scratch-parent $parent --operator-consent 'B0160 R5 FIXED7 G0 REVIEWED NATIVE' *>&1
  Need ($LASTEXITCODE -eq 0) 'Native corrected R5 seven-case runner failed; full G1 HOLD'
  $exitCode=0
  Write-Host 'B0160_W23_FIXED7_REAL_UNITY_TESTS_FINISHED_REQUIRES_INDEPENDENT_RECEIPT_REVIEW'
 }
}
catch {
 $exitCode=2
 if($Run){$exitCode=3}
 Write-Warning ('B0160_W23_FIXED7_HOLD: '+($_.Exception.Message -replace '[\r\n]+',' '))
}
finally {
 Write-Host "B0160_W23_FIXED7_EXIT=$exitCode G1_ONLY_WITH_REAL_EDITOR_ORIGINAL_LOG_XML_AND_SOURCE_G0"
}
exit $exitCode
