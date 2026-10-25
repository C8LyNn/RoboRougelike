# Minimal Windows/PowerShell wrapper: prompts are intentionally NOT automated.
param(
  [Parameter(Mandatory=$true)][ValidateSet('verify','sandbox','preflight','stage','rollback')][string]$Command,
  [string]$Config,
  [string]$Zip,
  [string]$Repo,
  [string]$Target,
  [string]$SourceRef,
  [string]$ExpectedHead,
  [string]$Journal,
  [string]$Report
)
$ErrorActionPreference='Stop'
$Script=Join-Path $PSScriptRoot 'package_auto.py'
$argsList=@($Script,$Command)
switch ($Command) {
  'sandbox' { $argsList += @('--repo',$Repo,'--target',$Target,'--source-ref',$SourceRef) }
  'rollback' { $argsList += @('--target',$Target,'--journal',$Journal) }
  default {
    $argsList += @('--config',$Config,'--zip',$Zip)
    if ($Target) { $argsList += @('--target',$Target) }
    if ($ExpectedHead) { $argsList += @('--expected-head',$ExpectedHead) }
    if ($Report) { $argsList += @('--report',$Report) }
  }
}
python @argsList
exit $LASTEXITCODE
