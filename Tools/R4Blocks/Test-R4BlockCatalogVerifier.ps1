param(
    [Parameter(Mandatory=$true)][string]$Source,
    [Parameter(Mandatory=$true)][string]$Catalog
)
$ErrorActionPreference = 'Stop'
$verifier = Join-Path $PSScriptRoot 'Verify-R4BlockCatalog.ps1'
if (-not (Test-Path -LiteralPath $verifier)) { throw 'Catalog verifier is missing.' }
& $verifier -Source $Source -Catalog $Catalog -SelfTest
if (-not $?) { throw 'Catalog verifier self-test failed.' }
