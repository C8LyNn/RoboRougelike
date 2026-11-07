param(
    [Parameter(Mandatory=$true)][string]$Source,
    [Parameter(Mandatory=$true)][string]$Catalog,
    [string]$ExpectedSourceSha256 = '3b4208e917d7314673f77d16934bb1bf4894d5b7b0839889c5d9a7fbb49adc74',
    [switch]$SelfTest
)
$ErrorActionPreference = 'Stop'
$culture = [System.Globalization.CultureInfo]::InvariantCulture

function Normalize-Ints($values) {
    return (($values | ForEach-Object { [string][int]$_ }) -join ',')
}
function Check-Floats([string]$actual, $expected, [string]$label) {
    $numbers = @($actual.Split(','))
    if ($numbers.Count -ne $expected.Count) { throw "${label}: wrong component count" }
    for ($i=0; $i -lt $numbers.Count; $i++) {
        $parsed = [double]::Parse($numbers[$i].Trim().TrimEnd('f'), $culture)
        if ([Math]::Abs($parsed - [double]$expected[$i]) -gt .000051) { throw "${label}: component $i differs" }
    }
}
function Verify-Catalog([string]$text, $sourceData) {
    $entryPattern = '(?s)new Entry\{Id="(?<id>[^"]+)",MountModules=(?<mount>true|false),Cells=new\[\]\{(?<cells>.*?)\},ContactMaskAnchor=(?<mask>\d+),\s*Sockets=new\[\]\{(?<sockets>.*?)\},\s*PartialSocket=new\[\]\{(?<partial>.*?)\},\s*Bonds=new \(Vector3Int,Vector3Int\)\[\]\{(?<bonds>.*?)\}\},'
    $matches = [regex]::Matches($text, $entryPattern)
    $allEntries = [regex]::Matches($text, 'new Entry\{').Count
    if ($matches.Count -ne 30 -or $allEntries -ne 30 -or $sourceData.blocks.Count -ne 30) { throw 'Expected exactly 30 parseable catalog and source entries.' }
    $sourceById = @{}
    foreach ($block in $sourceData.blocks) {
        if ($sourceById.ContainsKey($block.id)) { throw "Duplicate source ID $($block.id)" }
        $sourceById[$block.id] = $block
    }
    $seen = @{}
    foreach ($entry in $matches) {
        $id = $entry.Groups['id'].Value
        if ($seen.ContainsKey($id) -or -not $sourceById.ContainsKey($id)) { throw "Unexpected or duplicate catalog ID $id" }
        $seen[$id] = $true; $block = $sourceById[$id]
        if ([bool]::Parse($entry.Groups['mount'].Value) -ne [bool]$block.mount_modules) { throw "$id module policy differs" }
        if ([int]$entry.Groups['mask'].Value -ne [int]$block.contact_mask_anchor) { throw "$id contact mask differs" }
        $cellMatches = [regex]::Matches($entry.Groups['cells'].Value, 'new Vector3Int\(([^)]*)\)')
        if ($cellMatches.Count -ne $block.cells.Count) { throw "$id cell count differs" }
        for ($i=0; $i -lt $cellMatches.Count; $i++) {
            if ((Normalize-Ints ($cellMatches[$i].Groups[1].Value.Split(','))) -ne (Normalize-Ints $block.cells[$i])) { throw "$id cell $i differs" }
        }
        $socketPattern = 'new BuildSocket\(new Vector3Int\((?<cell>[^)]*)\),new Vector3Int\((?<face>[^)]*)\),BuildSocketKind\.(?<kind>\w+),new Vector2\((?<half>[^)]*)\),new Vector2\((?<offset>[^)]*)\)\)'
        $socketMatches = [regex]::Matches($entry.Groups['sockets'].Value, $socketPattern)
        $partial = @([regex]::Matches($entry.Groups['partial'].Value, '\b(true|false)\b') | ForEach-Object { [bool]::Parse($_.Value) })
        if ($socketMatches.Count -ne $block.sockets.Count -or $partial.Count -ne $block.sockets.Count) { throw "$id socket count differs" }
        for ($i=0; $i -lt $socketMatches.Count; $i++) {
            $socket=$socketMatches[$i]; $expected=$block.sockets[$i]
            if ($expected.kind -notin @('full','partial')) { throw "$id socket $i has unknown source kind" }
            if ($socket.Groups['kind'].Value -ne 'Structural') { throw "$id socket $i has unexpected kind" }
            if ((Normalize-Ints ($socket.Groups['cell'].Value.Split(','))) -ne (Normalize-Ints $expected.cell)) { throw "$id socket $i cell differs" }
            if ((Normalize-Ints ($socket.Groups['face'].Value.Split(','))) -ne (Normalize-Ints $expected.face)) { throw "$id socket $i face differs" }
            if ($partial[$i] -ne ($expected.kind -eq 'partial')) { throw "$id socket $i partial flag differs" }
            Check-Floats $socket.Groups['half'].Value $expected.half "$id socket $i half"
            Check-Floats $socket.Groups['offset'].Value $expected.offset "$id socket $i offset"
        }
        $bondPattern = '\(new Vector3Int\((?<a>[^)]*)\),new Vector3Int\((?<b>[^)]*)\)\)'
        $bondMatches = [regex]::Matches($entry.Groups['bonds'].Value, $bondPattern)
        if ($bondMatches.Count -ne $block.bonds.Count) { throw "$id bond count differs" }
        for ($i=0; $i -lt $bondMatches.Count; $i++) {
            if ((Normalize-Ints ($bondMatches[$i].Groups['a'].Value.Split(','))) -ne (Normalize-Ints $block.bonds[$i][0]) -or
                (Normalize-Ints ($bondMatches[$i].Groups['b'].Value.Split(','))) -ne (Normalize-Ints $block.bonds[$i][1])) { throw "$id bond $i differs" }
        }
    }
    return $seen.Count
}

$actualHash=(Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash
if ($actualHash -ne $ExpectedSourceSha256) { throw "Source SHA-256 differs: $actualHash" }
$sourceData = Get-Content -LiteralPath $Source -Raw | ConvertFrom-Json
$catalogText = [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Catalog))
$count = Verify-Catalog $catalogText $sourceData
Write-Output "Source parity PASS: $count block definitions."
if ($SelfTest) {
    $mutations = @(
        @('Id="R4B_Cube",MountModules=true','Id="R4B_Cube",MountModules=false'),
        @('ContactMaskAnchor=63','ContactMaskAnchor=62'),
        @('Cells=new[]{new Vector3Int(0,0,0)}','Cells=new[]{new Vector3Int(0,0,1)}'),
        @('new Vector2(0.4700f,0.4700f)','new Vector2(0.4600f,0.4700f)')
    )
    foreach ($mutation in $mutations) {
        $index=$catalogText.IndexOf($mutation[0], [StringComparison]::Ordinal)
        if ($index -lt 0) { throw 'Self-test mutation target not found.' }
        $tampered=$catalogText.Remove($index,$mutation[0].Length).Insert($index,$mutation[1])
        $rejected=$false
        try { [void](Verify-Catalog $tampered $sourceData) } catch { $rejected=$true }
        if (-not $rejected) { throw "Self-test failed to reject: $($mutation[0])" }
    }
    Write-Output 'Self-test PASS: changed policy, contact mask, cell and socket dimensions are rejected.'
}
