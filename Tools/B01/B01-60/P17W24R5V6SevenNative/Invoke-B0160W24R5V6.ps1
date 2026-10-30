#requires -Version 5.1
<# Owner-manual R5 V6 corrected seven-case native receiver. G1 NEVER assumed. #>
[CmdletBinding()]
param(
  [string]$RepoPath='.',
  [string]$ScratchParent='',
  [string]$UnityPath='',
  [switch]$Run,
  [switch]$SourceG0Approved
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$exitCode=2
function Need([bool]$ok,[string]$why){if(-not $ok){throw "B0160_W24_HOLD: $why"}}
function LocalGit([string[]]$a){
  $v=@(& git @a 2>&1)
  Need ($LASTEXITCODE -eq 0) 'Local Git verification failed; no automatic fetch'
  return ($v -join [Environment]::NewLine).Trim()
}
function Inside([string]$a,[string]$b){
 $a=[IO.Path]::GetFullPath($a).TrimEnd('\','/')+'\'
 $b=[IO.Path]::GetFullPath($b).TrimEnd('\','/')+'\'
 return $a.StartsWith($b,[StringComparison]::OrdinalIgnoreCase)
}
try {
 Need ($env:OS -eq 'Windows_NT') 'Windows operator only'
 $null=Get-Command git -ErrorAction Stop
 $null=Get-Command python -ErrorAction Stop
 $pv=@(& python --version 2>&1)
 Need ($LASTEXITCODE -eq 0 -and ($pv -join ' ') -match '^Python 3\.') 'Python 3 required'
 $repo=(Resolve-Path -LiteralPath $RepoPath -ErrorAction Stop).Path
 Need (([IO.Path]::GetFullPath((LocalGit @('-C',$repo,'rev-parse','--show-toplevel')))).TrimEnd('\','/') -ieq
       ([IO.Path]::GetFullPath($repo)).TrimEnd('\','/')) 'RepoPath not exact repository root'
 $head=LocalGit @('-C',$repo,'rev-parse','HEAD')
 $base='c782eeec12b46e4a142af79d6e641ab4c9e2e004'
 Need ((LocalGit @('-C',$repo,'merge-base',$base,$head)) -eq $base) 'Missing immutable W23 QA ancestry'
 $v6='60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb'
 Need ((LocalGit @('-C',$repo,'cat-file','-t',$v6)) -eq 'commit') 'No local selected exact #750 commit; no auto-fetch'
 $mine='Tools/B01/B01-60/P17W24R5V6SevenNative/Invoke-B0160W24R5V6.ps1'
 $pyfile='Tools/B01/B01-60/P17W24R5V6SevenNative/r5v6_native.py'
 Need ([IO.Path]::GetFullPath((Join-Path $repo $mine)) -ieq [IO.Path]::GetFullPath($PSCommandPath)) 'Script not from tracked checkout'
 foreach($item in @($mine,$pyfile)) {
   Need ((LocalGit @('-C',$repo,'hash-object','--',$item)) -eq
         (LocalGit @('-C',$repo,'rev-parse',"HEAD:$item"))) 'Changed/untracked local operator code'
 }
 if([string]::IsNullOrWhiteSpace($ScratchParent)){$ScratchParent=$env:TEMP}
 $parent=(Resolve-Path -LiteralPath $ScratchParent -ErrorAction Stop).Path
 Need (-not $parent.StartsWith('\\')) 'UNC/network scratch forbidden'
 Need ($parent -ine $repo -and -not(Inside $parent $repo) -and -not(Inside $repo $parent)) 'Scratch overlaps original repository'
 $cur=Get-Item -LiteralPath $parent -ErrorAction Stop
 while($null -ne $cur) {
   Need (-not [bool]($cur.Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Linked/junction scratch parent denied'
   $cur=$cur.Parent
 }
 $idx=0
 if(-not [string]::IsNullOrWhiteSpace($env:GIT_CONFIG_COUNT)){$idx=[int]$env:GIT_CONFIG_COUNT}
 $env:GIT_CONFIG_COUNT=[string]($idx+1)
 [Environment]::SetEnvironmentVariable(('GIT_CONFIG_KEY_'+$idx),'core.longpaths','Process')
 [Environment]::SetEnvironmentVariable(('GIT_CONFIG_VALUE_'+$idx),'true','Process')
 $p=Join-Path $repo $pyfile
 Write-Host "B0160_W24_EXACT_HEAD=$head AUTHOR_R5V6=$v6"
 & python $p --mode preflight --scratch-parent $parent *>&1
 Need ($LASTEXITCODE -eq 0) 'Source preflight or private 7-project staging refused'
 if(-not $Run) {
   $exitCode=0
   Write-Host 'B0160_W24_SOURCE_PREFLIGHT_7_CASES_PASS_NATIVE_G1_NOT_RUN'
 } else {
   Need $SourceG0Approved.IsPresent 'Independent B01-40/41 original V6 source G0 approval required'
   if([string]::IsNullOrWhiteSpace($UnityPath)) {
      $UnityPath=Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.3.14f1/Editor/Unity.exe'
   }
   Need (Test-Path -LiteralPath $UnityPath -PathType Leaf) 'Unity Hub Editor missing'
   $editor=(Resolve-Path -LiteralPath $UnityPath).Path
   Need ($editor -match '[\\/]6000\.3\.14f1[\\/]Editor[\\/]Unity\.exe$') 'Not exact activated Unity Hub Editor'
   Need (-not [bool]((Get-Item -LiteralPath $editor).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Linked Editor forbidden'
   Write-Host "B0160_W24_UNITY_EXE_SHA256=$((Get-FileHash -LiteralPath $editor -Algorithm SHA256).Hash.ToLowerInvariant())"
   Write-Host 'Verify legitimate Unity Hub activation and independent corrected #750 source G0 before proceeding.'
   Write-Host 'This runs ONLY the seven original R5 V6 authored PlayMode tests in seven isolated disposable private projects; NOT canonical live Save/Load.'
   $answer=Read-Host 'To authorize ONE operator run type B0160 R5V6 SEVEN NATIVE'
   Need ($answer -ceq 'B0160 R5V6 SEVEN NATIVE') 'Operator declined explicit consent'
   & python $p --mode native --unity $editor --scratch-parent $parent --approved-source-g0 --operator-consent 'B0160 R5V6 SEVEN NATIVE' *>&1
   Need ($LASTEXITCODE -eq 0) 'Native Unity seven-test R5 V6 receipt rejected'
   $exitCode=0
   Write-Host 'B0160_W24_SEVEN_R5V6_NATIVE_TESTS_RECORDED_INDEPENDENT_EVIDENCE_REVIEW_REQUIRED'
 }
}
catch {
 $exitCode=2
 if($Run){$exitCode=3}
 Write-Warning ('B0160_W24_FAIL_CLOSED: '+($_.Exception.Message -replace '[\r\n]+',' '))
}
finally {
 Write-Host "B0160_W24_EXIT=$exitCode NO_AUTO_UPLOAD_NO_FOREIGN_CLEANUP_NO_MERGE"
}
exit $exitCode
