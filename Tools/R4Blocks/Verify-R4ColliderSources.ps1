param(
    [Parameter(Mandatory=$true)][string]$SourceRoot,
    [string]$ExpectedCatalogSha256 = '3b4208e917d7314673f77d16934bb1bf4894d5b7b0839889c5d9a7fbb49adc74',
    [string]$ExpectedManifestSha256 = 'cb8b7d258457aac0fe7063b2b4bfa4d1e9f746f97d88d4a932721f4b786d6a2b',
    [switch]$SelfTest
)
$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $SourceRoot).Path).TrimEnd([char[]]'\/') + [System.IO.Path]::DirectorySeparatorChar
$catalog = Join-Path $root 'integration/R4_BLOCKS_UNITY.json'
$manifest = Join-Path $root 'SHA256SUMS.txt'
if ((Get-FileHash -LiteralPath $catalog -Algorithm SHA256).Hash -ne $ExpectedCatalogSha256) { throw 'Unexpected block catalog SHA-256.' }
if ((Get-FileHash -LiteralPath $manifest -Algorithm SHA256).Hash -ne $ExpectedManifestSha256) { throw 'Unexpected source manifest SHA-256.' }
$sourceData = Get-Content -LiteralPath $catalog -Raw | ConvertFrom-Json
if ($sourceData.blocks.Count -ne 30) { throw 'Expected exactly 30 source blocks.' }
$hashes = @{}
foreach ($line in Get-Content -LiteralPath $manifest) {
    $entry = [regex]::Match($line, '^([0-9a-fA-F]{64})\s+(.+)$')
    if (-not $entry.Success) { throw "Malformed manifest line: $line" }
    $relative = $entry.Groups[2].Value.Replace('\','/')
    if ($hashes.ContainsKey($relative)) { throw "Duplicate manifest entry: $relative" }
    $hashes[$relative] = $entry.Groups[1].Value
}
function Verify-ColliderSet($blocks, $expectedHashes) {
    $seen = @{}
    foreach ($block in $blocks) {
        $relative=[string]$block.colliders
        if ([string]::IsNullOrWhiteSpace($relative) -or -not $relative.EndsWith('_colliders.glb',[StringComparison]::OrdinalIgnoreCase)) { throw "Invalid collider path for $($block.id)" }
        $relative=$relative.Replace('\','/')
        if ($seen.ContainsKey($relative) -or -not $expectedHashes.ContainsKey($relative)) { throw "Missing or duplicate collider manifest entry: $relative" }
        $full=[System.IO.Path]::GetFullPath((Join-Path $root $relative))
        if (-not $full.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)) { throw "Collider path escapes source root: $relative" }
        if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { throw "Missing collider: $relative" }
        $actual=(Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash
        if ($actual -ne $expectedHashes[$relative]) { throw "Collider hash mismatch: $relative" }
        $seen[$relative]=$true
    }
    return $seen.Count
}
$count=Verify-ColliderSet $sourceData.blocks $hashes
Write-Output "Source collider SHA-256 PASS: $count/30."
if ($SelfTest) {
    $wrong=@{}; foreach($key in $hashes.Keys){$wrong[$key]=$hashes[$key]}
    $first=[string]$sourceData.blocks[0].colliders; $wrong[$first]='0'*64
    $rejected=$false
    try { [void](Verify-ColliderSet $sourceData.blocks $wrong) } catch { $rejected=$true }
    if (-not $rejected) { throw 'Self-test did not reject an incorrect collider hash.' }
    Write-Output 'Self-test PASS: an incorrect collider hash is rejected.'
}
