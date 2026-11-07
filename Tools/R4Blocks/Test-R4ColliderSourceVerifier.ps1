param([Parameter(Mandatory=$true)][string]$SourceRoot)
$ErrorActionPreference = 'Stop'
$verifier = Join-Path $PSScriptRoot 'Verify-R4ColliderSources.ps1'
if (-not (Test-Path -LiteralPath $verifier)) { throw 'Collider source verifier is missing.' }
& $verifier -SourceRoot $SourceRoot -SelfTest
if (-not $?) { throw 'Collider source verifier self-test failed.' }
