param(
    [Parameter(Mandatory=$true)][string]$SourceRoot,
    [switch]$SelfTest
)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Verify-R4ColliderSources.ps1') -SourceRoot $SourceRoot
$root=[System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $SourceRoot).Path).TrimEnd([char[]]'\/') + [System.IO.Path]::DirectorySeparatorChar
$catalog=Get-Content -LiteralPath (Join-Path $root 'integration/R4_BLOCKS_UNITY.json') -Raw | ConvertFrom-Json

function Read-ColliderGlb([byte[]]$bytes,[string]$id) {
    if ($bytes.Length -lt 20) { throw "$id GLB header is truncated" }
    $stream=[System.IO.MemoryStream]::new($bytes,$false)
    $reader=[System.IO.BinaryReader]::new($stream)
    try {
        if ($reader.ReadUInt32() -ne 0x46546c67) { throw "$id invalid GLB magic" }
        if ($reader.ReadUInt32() -ne 2) { throw "$id unsupported GLB version" }
        if ($reader.ReadUInt32() -ne $bytes.Length) { throw "$id GLB length differs" }
        $json=$null; $binLength=-1; $chunkIndex=0
        while($stream.Position -lt $stream.Length) {
            if ($stream.Length-$stream.Position -lt 8) { throw "$id truncated chunk header" }
            $chunkLength=$reader.ReadUInt32(); $chunkType=$reader.ReadUInt32()
            if ($chunkLength % 4 -ne 0 -or $chunkLength -gt $stream.Length-$stream.Position) { throw "$id invalid chunk length" }
            if ($chunkIndex -eq 0 -and $chunkType -eq 0x4e4f534a) {
                $payload=$reader.ReadBytes([int]$chunkLength)
                $json=[System.Text.Encoding]::UTF8.GetString($payload).Trim([char[]]@([char]0,' ')) | ConvertFrom-Json
            } elseif ($chunkIndex -eq 1 -and $chunkType -eq 0x004e4942) {
                $binLength=[int]$chunkLength; $stream.Position += $chunkLength
            } else { throw "$id unexpected GLB chunk order or type" }
            $chunkIndex++
        }
        if ($chunkIndex -ne 2 -or $null -eq $json -or $json.asset.version -ne '2.0') { throw "$id incomplete glTF 2.0 container" }
        if (@($json.buffers).Count -ne 1 -or -not [string]::IsNullOrEmpty($json.buffers[0].uri)) { throw "$id must use one embedded buffer" }
        $declared=[int]$json.buffers[0].byteLength
        if ($declared -lt 1 -or $declared -gt $binLength -or $binLength-$declared -gt 3) { throw "$id invalid embedded buffer length" }
        foreach($view in $json.bufferViews) {
            $offset=if($null -eq $view.byteOffset){0}else{[int]$view.byteOffset}
            if ([int]$view.buffer -ne 0 -or $offset -lt 0 -or [int]$view.byteLength -lt 0 -or $offset+[int]$view.byteLength -gt $declared) { throw "$id invalid buffer view" }
        }
        $nodes=@($json.nodes | Where-Object { $null -ne $_.mesh })
        if ($nodes.Count -eq 0) { throw "$id has no collider mesh nodes" }
        $triangles=0; $largest=0
        foreach($node in $nodes) {
            if ($node.name -notmatch ('^UCX_'+[regex]::Escape($id)+'(?:_\d+)?$')) { throw "$id unexpected collider node name: $($node.name)" }
            $meshIndex=[int]$node.mesh
            if ($meshIndex -lt 0 -or $meshIndex -ge $json.meshes.Count) { throw "$id invalid mesh index" }
            $mesh=$json.meshes[$meshIndex]; $nodeTriangles=0
            if (@($mesh.primitives).Count -eq 0) { throw "$id mesh has no primitives" }
            foreach($primitive in $mesh.primitives) {
                if ($null -ne $primitive.mode -and [int]$primitive.mode -ne 4) { throw "$id collider is not triangles" }
                if ($null -eq $primitive.attributes.POSITION -or $null -eq $primitive.indices) { throw "$id missing position or index accessor" }
                $pos=[int]$primitive.attributes.POSITION; $indices=[int]$primitive.indices
                if ($pos -lt 0 -or $pos -ge $json.accessors.Count -or $indices -lt 0 -or $indices -ge $json.accessors.Count) { throw "$id invalid accessor index" }
                if ($json.accessors[$pos].type -ne 'VEC3' -or [int]$json.accessors[$pos].count -lt 4 -or $json.accessors[$indices].type -ne 'SCALAR') { throw "$id invalid collider accessors" }
                $indexCount=[int]$json.accessors[$indices].count
                if ($indexCount -lt 3 -or $indexCount % 3 -ne 0) { throw "$id invalid triangle index count" }
                $nodeTriangles += $indexCount/3
            }
            $triangles += $nodeTriangles; $largest=[Math]::Max($largest,$nodeTriangles)
        }
        return [pscustomobject]@{Id=$id; MeshNodes=$nodes.Count; Triangles=$triangles; LargestMeshTriangles=$largest}
    } finally { $reader.Dispose(); $stream.Dispose() }
}

$results=@()
foreach($block in $catalog.blocks) {
    $path=Join-Path $root $block.colliders
    $results += Read-ColliderGlb ([System.IO.File]::ReadAllBytes($path)) $block.id
}
$multi=@($results | Where-Object { $_.MeshNodes -gt 1 })
$max=($results | Measure-Object -Property LargestMeshTriangles -Maximum).Maximum
Write-Output "GLB structure PASS: $($results.Count)/30 files, $(($results | Measure-Object -Property MeshNodes -Sum).Sum) collider mesh nodes; $($multi.Count) multi-mesh blocks; largest mesh $max triangles."
Write-Output ("Multi-mesh blocks: " + (($multi | ForEach-Object { "$($_.Id)=$($_.MeshNodes)" }) -join ', '))
if ($SelfTest) {
    $original=[System.IO.File]::ReadAllBytes((Join-Path $root $catalog.blocks[0].colliders))
    foreach($mutation in @('magic','length','chunk')) {
        $bytes=[byte[]]$original.Clone()
        switch($mutation) {'magic' {$bytes[0]=0}; 'length' {$bytes[8]=$bytes[8] -bxor 1}; 'chunk' {$bytes[16]=0}}
        $rejected=$false
        try { [void](Read-ColliderGlb $bytes $catalog.blocks[0].id) } catch { $rejected=$true }
        if(-not $rejected) { throw "Self-test failed to reject corrupt $mutation" }
    }
    Write-Output 'Self-test PASS: corrupt GLB magic, length and chunk type are rejected.'
}
