#requires -Version 5.1
<# B01-60 PASS17/W25. USER-OPT-IN original #772 PhysX test-only. #773 NOT installed. #>
[CmdletBinding()]
param(
 [string]$RepoPath='.',[string]$ScratchParent='',
 [string]$UnityPath='',[switch]$Run,[switch]$SourceG0Approved
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$exitCode=2
function Need([bool]$ok,[string]$message){
 if(-not $ok){throw "B0160_W25_HOLD: $message"}
}
function LocalGit([string[]]$arguments){
 $result=@(& git @arguments 2>&1)
 Need ($LASTEXITCODE -eq 0) 'Local-only Git source check failed'
 return ($result -join [Environment]::NewLine).Trim()
}
function Child([string]$p,[string]$root){
 $p=[IO.Path]::GetFullPath($p).TrimEnd('\','/')+'\'
 $root=[IO.Path]::GetFullPath($root).TrimEnd('\','/')+'\'
 return $p.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)
}
try {
 Need ($env:OS -eq 'Windows_NT') 'Manual Windows operator only'
 $null=Get-Command git -ErrorAction Stop
 $null=Get-Command python -ErrorAction Stop
 $v=@(& python --version 2>&1)
 Need ($LASTEXITCODE -eq 0 -and ($v -join ' ') -match '^Python 3\.') 'Python3 required'
 $repo=(Resolve-Path -LiteralPath $RepoPath -ErrorAction Stop).Path
 Need (([IO.Path]::GetFullPath((LocalGit @('-C',$repo,'rev-parse','--show-toplevel')))).TrimEnd('\','/') -ieq
       ([IO.Path]::GetFullPath($repo)).TrimEnd('\','/')) 'Require exact Git checkout root'
 $sha=LocalGit @('-C',$repo,'rev-parse','HEAD')
 $parent='6a3589ff23bd811fb456d375e14b45c22b93337d'
 Need ((LocalGit @('-C',$repo,'merge-base',$parent,$sha)) -eq $parent) 'Not descendant of exact W24 B01-60'
 $author='ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1'
 Need ((LocalGit @('-C',$repo,'cat-file','-t',$author)) -eq 'commit') 'Original #772 object absent locally; no auto-fetch'
 $mine='Tools/B01/B01-60/P17W25Physical772Native/Invoke-B0160W25Rig.ps1'
 $code='Tools/B01/B01-60/P17W25Physical772Native/rig_native.py'
 Need ([IO.Path]::GetFullPath((Join-Path $repo $mine)) -ieq [IO.Path]::GetFullPath($PSCommandPath)) 'Only Git tracked operator permitted'
 foreach($f in @($mine,$code)){
   Need ((LocalGit @('-C',$repo,'hash-object','--',$f)) -eq
         (LocalGit @('-C',$repo,'rev-parse',"HEAD:$f"))) "Local native source dirty: $f"
 }
 if([string]::IsNullOrWhiteSpace($ScratchParent)){$ScratchParent=$env:TEMP}
 $parentDir=(Resolve-Path -LiteralPath $ScratchParent -ErrorAction Stop).Path
 Need (-not $parentDir.StartsWith('\\')) 'No network scratch'
 Need ($repo -ine $parentDir -and -not(Child $parentDir $repo) -and -not(Child $repo $parentDir)) 'Scratch overlaps source tree'
 $d=Get-Item -LiteralPath $parentDir -ErrorAction Stop
 while($null -ne $d){
   Need (-not [bool]($d.Attributes -band [IO.FileAttributes]::ReparsePoint)) 'No symlink/junction scratch parents'
   $d=$d.Parent
 }
 $old=0;if(-not [string]::IsNullOrWhiteSpace($env:GIT_CONFIG_COUNT)){$old=[int]$env:GIT_CONFIG_COUNT}
 $env:GIT_CONFIG_COUNT=[string]($old+1)
 [Environment]::SetEnvironmentVariable(('GIT_CONFIG_KEY_'+$old),'core.longpaths','Process')
 [Environment]::SetEnvironmentVariable(('GIT_CONFIG_VALUE_'+$old),'true','Process')
 $script=Join-Path $repo $code
 Write-Host "B0160_W25_SOURCE_HEAD=$sha ORIGINAL_RIG_772=$author"
 & python $script --mode preflight --scratch-parent $parentDir *>&1
 Need ($LASTEXITCODE -eq 0) 'Physical rig private G1 source preflight failed'
 if(-not $Run){
   $exitCode=0
   Write-Host 'B0160_W25_RIG_PREFLIGHT_PASS_REAL_UNITY_G1_NOT_RUN'
 } else {
   Need $SourceG0Approved.IsPresent 'B01-40/41 independent #772 source G0 not approved'
   if([string]::IsNullOrWhiteSpace($UnityPath)){
     $UnityPath=Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.3.14f1/Editor/Unity.exe'
   }
   Need (Test-Path -LiteralPath $UnityPath -PathType Leaf) 'Unity Editor not installed'
   $editor=(Resolve-Path -LiteralPath $UnityPath).Path
   Need ($editor -match '[\\/]6000\.3\.14f1[\\/]Editor[\\/]Unity\.exe$') 'Wrong Unity Editor version'
   Need (-not [bool]((Get-Item -LiteralPath $editor).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Symlink Editor denied'
   Write-Host "B0160_W25_EDITOR_SHA256=$((Get-FileHash -LiteralPath $editor -Algorithm SHA256).Hash.ToLowerInvariant())"
   Write-Host 'Manual licensed Unity activation required. Runs ONLY THREE #772 PhysX PlayMode tests + private scope checks.'
   Write-Host 'Does NOT run canonical #773 Save, production #510 private Rig[], economy/loaner, or V6 #750.'
   $answer=Read-Host 'To authorize exactly one local Unity run type B0160 W25 RIG NATIVE'
   Need ($answer -ceq 'B0160 W25 RIG NATIVE') 'Owner native authorization denied'
   & python $script --mode native --unity $editor --scratch-parent $parentDir --source-g0-approved --operator-consent 'B0160 W25 RIG NATIVE' *>&1
   Need ($LASTEXITCODE -eq 0) 'Real Unity #772 physical rig source G1 evidence rejected'
   $exitCode=0
   Write-Host 'B0160_W25_RIG_NATIVE_LOGS_CREATED_INDEPENDENT_REVIEW_PENDING'
 }
}
catch {
 $exitCode=2;if($Run){$exitCode=3}
 Write-Warning ('B0160_W25_FAIL_CLOSED: '+($_.Exception.Message -replace '[\r\n]+',' '))
}
finally {
 Write-Host "B0160_W25_EXIT=$exitCode NO_UPLOAD_NO_AUTO_FETCH_NO_FOREIGN_CLEANUP_NO_MERGE"
}
exit $exitCode
