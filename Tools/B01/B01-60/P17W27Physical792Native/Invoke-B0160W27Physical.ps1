#requires -Version 5.1
<# B01-60 W27 — ORIGINAL AUTHOR #792 4 PlayMode G1, user-operated only. #>
[CmdletBinding()]
param(
 [string]$RepoPath='.',[string]$ScratchParent='',
 [string]$UnityPath='',[switch]$Run,[switch]$SourceG0Approved
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$rc=2
function Require([bool]$ok,[string]$reason){
 if(-not $ok){throw "B0160_W27_HOLD: $reason"}
}
function Git([string[]]$a){
 $o=@(& git @a 2>&1)
 Require ($LASTEXITCODE -eq 0) 'local Git check failed; no automatic fetch'
 return ($o -join [Environment]::NewLine).Trim()
}
function IsUnder([string]$child,[string]$ancestor){
 $a=[IO.Path]::GetFullPath($ancestor).TrimEnd('\','/')+'\'
 $b=[IO.Path]::GetFullPath($child).TrimEnd('\','/')+'\'
 return $b.StartsWith($a,[StringComparison]::OrdinalIgnoreCase)
}
try {
 Require ($env:OS -eq 'Windows_NT') 'only owner Windows PC may run Unity G1'
 $null=Get-Command git -ErrorAction Stop
 $null=Get-Command python -ErrorAction Stop
 $ver=@(& python --version 2>&1)
 Require ($LASTEXITCODE -eq 0 -and ($ver -join ' ') -match '^Python 3\.') 'Python3 required'
 $root=(Resolve-Path -LiteralPath $RepoPath -ErrorAction Stop).Path
 Require (([IO.Path]::GetFullPath((Git @('-C',$root,'rev-parse','--show-toplevel')))).TrimEnd('\','/') -ieq
    ([IO.Path]::GetFullPath($root)).TrimEnd('\','/')) 'RepoPath must be exact checkout root'
 $base='494e1056366c9004deefd7e4a73ad07b0a62a137'
 $head=Git @('-C',$root,'rev-parse','HEAD')
 Require ((Git @('-C',$root,'merge-base',$base,$head)) -eq $base) 'untrusted #783 ancestry'
 $owner='16cb2a7b5158605b15191b6074f9e392e0d1ebd3'
 Require ((Git @('-C',$root,'cat-file','-t',$owner)) -eq 'commit') 'exact #792 original Git object missing; no auto-fetch'
 $mine='Tools/B01/B01-60/P17W27Physical792Native/Invoke-B0160W27Physical.ps1'
 $pythonFile='Tools/B01/B01-60/P17W27Physical792Native/rig_r2_native.py'
 Require ([IO.Path]::GetFullPath((Join-Path $root $mine)) -ieq [IO.Path]::GetFullPath($PSCommandPath)) 'wrong operator source file'
 foreach($file in @($mine,$pythonFile)){
  Require ((Git @('-C',$root,'hash-object','--',$file)) -eq
           (Git @('-C',$root,'rev-parse',"HEAD:$file"))) "dirty operator code: $file"
 }
 if([string]::IsNullOrWhiteSpace($ScratchParent)){$ScratchParent=$env:TEMP}
 $scratch=(Resolve-Path -LiteralPath $ScratchParent -ErrorAction Stop).Path
 Require (-not $scratch.StartsWith('\\')) 'no network scratch'
 Require (-not (IsUnder $scratch $root) -and -not (IsUnder $root $scratch)) 'scratch overlaps source'
 $p=Get-Item -LiteralPath $scratch -ErrorAction Stop
 while($null -ne $p){
  Require (-not [bool]($p.Attributes -band [IO.FileAttributes]::ReparsePoint)) 'linked scratch parent forbidden'
  $p=$p.Parent
 }
 $old=0;if(-not [string]::IsNullOrWhiteSpace($env:GIT_CONFIG_COUNT)){$old=[int]$env:GIT_CONFIG_COUNT}
 $env:GIT_CONFIG_COUNT=[string]($old+1)
 [Environment]::SetEnvironmentVariable(('GIT_CONFIG_KEY_'+$old),'core.longpaths','Process')
 [Environment]::SetEnvironmentVariable(('GIT_CONFIG_VALUE_'+$old),'true','Process')
 $py=Join-Path $root $pythonFile
 Write-Host "B0160_W27_HEAD=$head EXACT_AUTHOR_792=$owner"
 & python $py --mode preflight --scratch-parent $scratch *>&1
 Require ($LASTEXITCODE -eq 0) 'four original #792 private source projects preflight failed'
 if(-not $Run){
  Write-Host 'B0160_W27_SOURCE_ONLY_FOUR_PHYSICS_PROJECTS_PASS_NATIVE_UNITY_G1_NOT_RUN'
  $rc=0
 } else {
  Require $SourceG0Approved.IsPresent 'B01-40/41 exact #792 independent G0 not approved'
  if([string]::IsNullOrWhiteSpace($UnityPath)){
   $UnityPath=Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.3.14f1/Editor/Unity.exe'
  }
  Require (Test-Path -LiteralPath $UnityPath -PathType Leaf) 'exact Unity Hub Editor absent'
  $editor=(Resolve-Path -LiteralPath $UnityPath).Path
  Require ($editor -match '[\\/]6000\.3\.14f1[\\/]Editor[\\/]Unity\.exe$') 'Unity Editor version mismatch'
  Require (-not [bool]((Get-Item -LiteralPath $editor).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'linked Editor rejected'
  Write-Host "B0160_W27_EDITOR_SHA256=$((Get-FileHash -LiteralPath $editor -Algorithm SHA256).Hash.ToLowerInvariant())"
  Write-Host 'Requires owner activated legitimate Unity. Four original #792 RIG PhysX tests, NOT canonical Save/ECON and NOT #750.'
  $consent=Read-Host 'For one local run type B0160 W27 PHYSICS NATIVE'
  Require ($consent -ceq 'B0160 W27 PHYSICS NATIVE') 'operator declined explicit consent'
  & python $py --mode native --scratch-parent $scratch --unity $editor --source-g0-approved --operator-consent 'B0160 W27 PHYSICS NATIVE' *>&1
  Require ($LASTEXITCODE -eq 0) 'native PhysX NUnit/Editor evidence rejected'
  Write-Host 'B0160_W27_NATIVE_XML_EDITOR_PRODUCED_UNATTESTED_INDEPENDENT_G1_REQUIRED'
  $rc=0
 }
}
catch {
 $rc=2;if($Run){$rc=3}
 Write-Warning ('B0160_W27_FAIL_CLOSED: '+($_.Exception.Message -replace '[\r\n]+',' '))
}
finally {
 Write-Host "B0160_W27_EXIT=$rc NO_AUTO_FETCH_NO_UPLOAD_NO_FOREIGN_DELETE_NO_MERGE"
}
exit $rc
