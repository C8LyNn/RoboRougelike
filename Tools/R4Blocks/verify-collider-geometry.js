'use strict';
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');

const CATALOG_SHA = '3b4208e917d7314673f77d16934bb1bf4894d5b7b0839889c5d9a7fbb49adc74';
const MANIFEST_SHA = 'cb8b7d258457aac0fe7063b2b4bfa4d1e9f746f97d88d4a932721f4b786d6a2b';
const sha = bytes => crypto.createHash('sha256').update(bytes).digest('hex');

function parseGlb(bytes) {
  if (bytes.length < 28 || bytes.readUInt32LE(0) !== 0x46546c67 || bytes.readUInt32LE(4) !== 2 || bytes.readUInt32LE(8) !== bytes.length)
    throw new Error('Invalid GLB 2 header or length');
  const jsonLength = bytes.readUInt32LE(12);
  if (jsonLength % 4 || bytes.readUInt32LE(16) !== 0x4e4f534a || 20 + jsonLength + 8 > bytes.length)
    throw new Error('Invalid GLB JSON chunk');
  const json = JSON.parse(bytes.subarray(20, 20 + jsonLength).toString('utf8').replace(/[\0 ]+$/g, ''));
  const binHeader = 20 + jsonLength, binLength = bytes.readUInt32LE(binHeader);
  if (binLength % 4 || bytes.readUInt32LE(binHeader + 4) !== 0x004e4942 || binHeader + 8 + binLength !== bytes.length)
    throw new Error('Invalid GLB binary chunk');
  if (json.asset?.version !== '2.0' || json.buffers?.length !== 1 || json.buffers[0].uri ||
      json.buffers[0].byteLength > binLength || binLength - json.buffers[0].byteLength > 3)
    throw new Error('Unsupported GLB buffer');
  return { json, bin: bytes.subarray(binHeader + 8) };
}

function readAccessor(glb, index, kind) {
  const a = glb.json.accessors?.[index], v = glb.json.bufferViews?.[a?.bufferView];
  const components = kind === 'position' ? 3 : 1, componentType = kind === 'position' ? 5126 : 5123;
  const elementBytes = kind === 'position' ? 12 : 2;
  if (!a || !v || a.sparse || a.componentType !== componentType || a.type !== (kind === 'position' ? 'VEC3' : 'SCALAR') ||
      v.buffer !== 0 || !Number.isInteger(a.count) || a.count < 1)
    throw new Error(`Unsupported ${kind} accessor`);
  const viewStart = v.byteOffset || 0, viewEnd = viewStart + v.byteLength;
  const start = viewStart + (a.byteOffset || 0), stride = v.byteStride || elementBytes;
  if (viewStart < 0 || start < viewStart || stride < elementBytes || viewEnd > glb.json.buffers[0].byteLength ||
      start + (a.count - 1) * stride + elementBytes > viewEnd)
    throw new Error(`Out-of-bounds ${kind} accessor`);
  const result = [];
  for (let i=0; i<a.count; i++) {
    const off = start + i*stride;
    if (components === 3) result.push([glb.bin.readFloatLE(off), glb.bin.readFloatLE(off+4), glb.bin.readFloatLE(off+8)]);
    else result.push(glb.bin.readUInt16LE(off));
  }
  return result;
}

function analyzeMesh(vertices, indices) {
  const errors = [];
  if (!Array.isArray(vertices) || vertices.length < 4 || !Array.isArray(indices) || indices.length % 3 || indices.length < 12)
    return {ok:false, errors:['invalid vertex or triangle count']};
  if (vertices.some(v => !Array.isArray(v) || v.length !== 3 || v.some(n => !Number.isFinite(n))))
    return {ok:false, errors:['non-finite vertex']};
  const quant = v => v.map(n => Math.round(n / 1e-6)).join(',');
  const unique = new Map(); vertices.forEach(v => unique.set(quant(v), v));
  const edges = new Map(); let degenerate=0, nonConvexFaces=0;
  const edge = (a,b) => {
    const ka=quant(vertices[a]), kb=quant(vertices[b]), key=ka<kb ? `${ka}|${kb}` : `${kb}|${ka}`;
    const e=edges.get(key) || {count:0, orientation:0}; e.count++; e.orientation += ka<kb ? 1 : -1; edges.set(key,e);
  };
  for (let i=0; i<indices.length; i+=3) {
    const [a,b,c]=indices.slice(i,i+3);
    if (![a,b,c].every(n => Number.isInteger(n) && n>=0 && n<vertices.length)) return {ok:false, errors:['invalid triangle index']};
    const p=vertices[a], q=vertices[b], r=vertices[c];
    const u=q.map((n,j)=>n-p[j]), v=r.map((n,j)=>n-p[j]);
    const normal=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]];
    const length=Math.hypot(...normal);
    if (length<1e-12) { degenerate++; continue; }
    edge(a,b);edge(b,c);edge(c,a);
    let positive=false, negative=false;
    for (const point of unique.values()) {
      const distance=normal.reduce((sum,n,j)=>sum+n*(point[j]-p[j]),0)/length;
      if (distance>1e-5) positive=true; else if (distance< -1e-5) negative=true;
      if (positive && negative) { nonConvexFaces++; break; }
    }
  }
  const openOrNonManifold=[...edges.values()].filter(e=>e.count!==2).length;
  const inconsistent=[...edges.values()].filter(e=>e.count===2 && e.orientation!==0).length;
  if (degenerate) errors.push(`${degenerate} degenerate triangles`);
  if (openOrNonManifold) errors.push(`${openOrNonManifold} open/non-manifold edges`);
  if (inconsistent) errors.push(`${inconsistent} inconsistently oriented edges`);
  if (nonConvexFaces) errors.push(`${nonConvexFaces} faces fail the half-space convexity check`);
  return {ok:errors.length===0, errors, vertices:unique.size, triangles:indices.length/3};
}

// Necessary coarse check only: an AABB cannot detect holes in an L-shaped footprint.
function withinFootprintEnvelope(vertices, cells) {
  if (!Array.isArray(vertices) || !vertices.length || !Array.isArray(cells) || !cells.length ||
      cells.some(c=>!Array.isArray(c) || c.length!==3 || c.some(n=>!Number.isInteger(n)))) return false;
  const min=[0,1,2].map(axis=>Math.min(...cells.map(c=>c[axis]))*.55-.26-.001);
  const max=[0,1,2].map(axis=>Math.max(...cells.map(c=>c[axis]))*.55+.26+.001);
  return vertices.every(v=>Array.isArray(v) && v.length===3 && v.every((n,axis)=>Number.isFinite(n) && n>=min[axis] && n<=max[axis]));
}

// Separating-axis test for a convex triangle mesh and an axis-aligned box.
// Call only after the closed/convex numerical checks have passed.
function convexIntersectsBox(vertices, indices, center, half) {
  const dot=(a,b)=>a[0]*b[0]+a[1]*b[1]+a[2]*b[2];
  const sub=(a,b)=>[a[0]-b[0],a[1]-b[1],a[2]-b[2]];
  const cross=(a,b)=>[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]];
  const boxAxes=[[1,0,0],[0,1,0],[0,0,1]], axes=boxAxes.slice();
  for(let i=0;i<indices.length;i+=3) {
    const a=vertices[indices[i]],b=vertices[indices[i+1]],c=vertices[indices[i+2]];
    const edges=[sub(b,a),sub(c,b),sub(a,c)];
    axes.push(cross(edges[0],sub(c,a)));
    for(const edge of edges)for(const boxAxis of boxAxes)axes.push(cross(edge,boxAxis));
  }
  for(const raw of axes) {
    const length=Math.hypot(...raw);if(length<1e-12)continue;
    const axis=raw.map(n=>n/length), projected=vertices.map(v=>dot(v,axis));
    const min=Math.min(...projected),max=Math.max(...projected),middle=dot(center,axis);
    const radius=half.reduce((sum,n,i)=>sum+n*Math.abs(axis[i]),0);
    // Contact at the numerical boundary is not a penetration into the box interior.
    if(max<=middle-radius+1e-7 || middle+radius<=min+1e-7)return false;
  }
  return true;
}

function emptyFootprintCells(cells) {
  const min=[0,1,2].map(axis=>Math.min(...cells.map(c=>c[axis])));
  const max=[0,1,2].map(axis=>Math.max(...cells.map(c=>c[axis])));
  if(max.some((n,i)=>n-min[i]>10))throw new Error('Unreasonably large source footprint');
  const occupied=new Set(cells.map(c=>c.join(','))),empty=[];
  for(let x=min[0];x<=max[0];x++)for(let y=min[1];y<=max[1];y++)for(let z=min[2];z<=max[2];z++)
    if(!occupied.has([x,y,z].join(',')))empty.push([x,y,z]);
  return empty;
}

function verifySource(root) {
  const catalogBytes=fs.readFileSync(path.join(root,'integration','R4_BLOCKS_UNITY.json'));
  const manifestBytes=fs.readFileSync(path.join(root,'SHA256SUMS.txt'));
  if (sha(catalogBytes)!==CATALOG_SHA || sha(manifestBytes)!==MANIFEST_SHA) throw new Error('Source catalog or manifest SHA-256 differs');
  const catalog=JSON.parse(catalogBytes.toString('utf8'));
  if (catalog.blocks?.length!==30) throw new Error('Expected 30 source blocks');
  const hashes=new Map();
  for (const line of manifestBytes.toString('utf8').split(/\r?\n/).filter(Boolean)) {
    const m=/^([a-f0-9]{64})\s+(.+)$/.exec(line); if (!m || hashes.has(m[2])) throw new Error('Invalid/duplicate manifest entry'); hashes.set(m[2],m[1]);
  }
  const sourceRoot=path.resolve(root), seen=new Set(); const results=[];
  for (const block of catalog.blocks) {
    const relative=block.colliders;
    if (typeof relative!=='string' || !relative.endsWith('_colliders.glb') || seen.has(relative) || !hashes.has(relative))
      throw new Error(`Invalid collider source: ${block.id}`);
    seen.add(relative); const file=path.resolve(sourceRoot,relative), rel=path.relative(sourceRoot,file);
    if (!rel || rel.startsWith('..') || path.isAbsolute(rel)) throw new Error(`Collider path outside source: ${relative}`);
    const bytes=fs.readFileSync(file); if (sha(bytes)!==hashes.get(relative)) throw new Error(`Collider SHA-256 mismatch: ${relative}`);
    const glb=parseGlb(bytes);
    if (glb.json.nodes?.some(n=>n.translation || n.rotation || n.scale || n.matrix)) throw new Error(`Unhandled node transform: ${block.id}`);
    const nodes=(glb.json.nodes || []).filter(n=>Number.isInteger(n.mesh));
    if (!nodes.length) throw new Error(`No collider nodes: ${block.id}`);
    for (const node of nodes) {
      if (!new RegExp(`^UCX_${block.id}(?:_\\d+)?$`).test(node.name || '')) throw new Error(`Unexpected collider node: ${node.name}`);
      const mesh=glb.json.meshes?.[node.mesh]; if (!mesh?.primitives?.length) throw new Error(`Missing primitives: ${node.name}`);
      for (const primitive of mesh.primitives) {
        if ((primitive.mode ?? 4)!==4 || primitive.indices==null || primitive.attributes?.POSITION==null) throw new Error(`Unsupported primitive: ${node.name}`);
        const vertices=readAccessor(glb,primitive.attributes.POSITION,'position');
        const indices=readAccessor(glb,primitive.indices,'index');
        const result=analyzeMesh(vertices,indices);
        if (!withinFootprintEnvelope(vertices,block.cells)) { result.ok=false; result.errors.push('vertices exceed the coarse declared footprint envelope'); }
        result.emptyCellChecks=0;
        if (result.ok)for(const cell of emptyFootprintCells(block.cells)) {
          result.emptyCellChecks++;
          if(convexIntersectsBox(vertices,indices,cell.map(n=>n*.55),[.259,.259,.259])) {
            result.ok=false; result.errors.push(`collider penetrates empty footprint cell (${cell.join(',')})`);
          }
        }
        results.push({name:node.name,...result});
      }
    }
  }
  return results;
}

if (require.main===module) {
  try {
    if (process.argv.length!==3) throw new Error('Usage: node verify-collider-geometry.js PATH_TO_R4Blocks');
    const results=verifySource(process.argv[2]), failed=results.filter(r=>!r.ok);
    console.log(`Numerical mesh checks: ${results.length-failed.length}/${results.length} passed; ${failed.length} failed; ${results.reduce((sum,r)=>sum+r.emptyCellChecks,0)} mesh/empty-cell checks.`);
    for (const item of failed) console.log(`${item.name}: ${item.errors.join('; ')}`);
    if (failed.length) process.exitCode=1;
  } catch (error) { console.error(error.message); process.exitCode=1; }
}
module.exports={analyzeMesh,parseGlb,readAccessor,verifySource,withinFootprintEnvelope,convexIntersectsBox};
