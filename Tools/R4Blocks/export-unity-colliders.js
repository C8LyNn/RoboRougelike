'use strict';
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const crypto=require('node:crypto');
const {parseGlb,readAccessor,verifySource}=require('./verify-collider-geometry.js');
const sha=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const REVISION='03af5207143da76faeeb8004543a51f26c7f9c0d';
const CATALOG_SHA='3b4208e917d7314673f77d16934bb1bf4894d5b7b0839889c5d9a7fbb49adc74';
const MANIFEST_SHA='cb8b7d258457aac0fe7063b2b4bfa4d1e9f746f97d88d4a932721f4b786d6a2b';

function buildBundle(sourceRoot) {
  const checks=verifySource(sourceRoot);
  if(checks.length!==36 || checks.some(r=>!r.ok)) throw new Error('Source geometry preflight did not pass for all 36 pieces');
  const root=path.resolve(sourceRoot);
  const catalogBytes=fs.readFileSync(path.join(root,'integration','R4_BLOCKS_UNITY.json'));
  const manifestBytes=fs.readFileSync(path.join(root,'SHA256SUMS.txt'));
  if(sha(catalogBytes)!==CATALOG_SHA || sha(manifestBytes)!==MANIFEST_SHA) throw new Error('Source revision hashes differ');
  const catalog=JSON.parse(catalogBytes.toString('utf8'));
  const hashes=new Map();
  for(const line of manifestBytes.toString('utf8').split(/\r?\n/).filter(Boolean)) {
    const match=/^([a-f0-9]{64})\s+(.+)$/.exec(line);
    if(!match || hashes.has(match[2])) throw new Error('Invalid source manifest');
    hashes.set(match[2],match[1]);
  }
  const shapes=[];
  for(const block of catalog.blocks) {
    const relative=block.colliders, file=path.resolve(root,relative), under=path.relative(root,file);
    if(typeof relative!=='string' || !under || under.startsWith('..') || path.isAbsolute(under)) throw new Error('Invalid source collider path');
    const bytes=fs.readFileSync(file), sourceSha256=sha(bytes);
    if(sourceSha256!==hashes.get(relative)) throw new Error(`Collider changed during export: ${block.id}`);
    const glb=parseGlb(bytes), nodes=(glb.json.nodes || []).filter(n=>Number.isInteger(n.mesh)), pieces=[];
    for(const node of nodes) {
      const primitives=glb.json.meshes?.[node.mesh]?.primitives;
      if(!primitives || primitives.length!==1) throw new Error(`Unsupported primitive layout: ${node.name}`);
      const primitive=primitives[0];
      // JSON canonicalizes -0 to 0; normalize explicitly before round-trip checks.
      const positions=readAccessor(glb,primitive.attributes.POSITION,'position').flat().map(n=>n===0?0:n);
      const indices=readAccessor(glb,primitive.indices,'index');
      pieces.push({name:node.name,positions,indices});
    }
    shapes.push({id:block.id,sourceSha256,pieces});
  }
  if(shapes.length!==30 || shapes.reduce((sum,s)=>sum+s.pieces.length,0)!==36) throw new Error('Unexpected shape or piece count');
  return {schemaVersion:1,sourceRevision:REVISION,sourceCatalogSha256:CATALOG_SHA,
    coordinates:'source_glb_xyz_unmodified',gridMeters:.55,shapes};
}

function verifyBundleAgainstSource(bundle,sourceRoot) {
  try { assert.deepStrictEqual(bundle,buildBundle(sourceRoot)); }
  catch(error) { throw new Error(`Bundle does not exactly match verified source: ${error.name}`); }
  return true;
}

if(require.main===module) {
  try {
    if(process.argv.length!==4) throw new Error('Usage: node export-unity-colliders.js PATH_TO_R4Blocks OUTPUT_JSON');
    const bundle=buildBundle(process.argv[2]);
    fs.writeFileSync(process.argv[3],JSON.stringify(bundle)+'\n',{encoding:'utf8',flag:'wx'});
    console.log(`Exported ${bundle.shapes.length} shapes / ${bundle.shapes.reduce((n,s)=>n+s.pieces.length,0)} pieces; existing output was not overwritten.`);
  } catch(error) { console.error(error.message); process.exitCode=1; }
}
module.exports={buildBundle,verifyBundleAgainstSource};
