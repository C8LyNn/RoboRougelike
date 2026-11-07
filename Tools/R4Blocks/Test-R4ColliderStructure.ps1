param([Parameter(Mandatory=$true)][string]$SourceRoot)
$ErrorActionPreference = 'Stop'
$inspector = Join-Path $PSScriptRoot 'Inspect-R4ColliderStructure.ps1'
if (-not (Test-Path -LiteralPath $inspector)) { throw 'Collider structure inspector is missing.' }
& $inspector -SourceRoot $SourceRoot -SelfTest
if (-not $?) { throw 'Collider structure inspector self-test failed.' }
