'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const {buildBundle, verifyBundleAgainstSource} = require('./export-unity-colliders.js');
const root=process.argv[2];
assert.ok(root,'Provide the exact #963 R4Blocks source directory.');
const bundle=buildBundle(root);
assert.equal(bundle.schemaVersion,1);
assert.equal(bundle.shapes.length,30);
assert.equal(bundle.shapes.reduce((n,s)=>n+s.pieces.length,0),36);
assert.equal(new Set(bundle.shapes.map(s=>s.id)).size,30);
assert.deepEqual(bundle.shapes.find(s=>s.id==='R4B_Arch').pieces.length,3);
assert.deepEqual(bundle.shapes.find(s=>s.id==='R4B_Tee4').pieces.length,2);
const roundTrip=JSON.parse(JSON.stringify(bundle));
verifyBundleAgainstSource(roundTrip,root);
if(process.argv[3])verifyBundleAgainstSource(JSON.parse(fs.readFileSync(process.argv[3],'utf8')),root);
assert.equal(JSON.stringify(buildBundle(root)),JSON.stringify(bundle),'export must be deterministic');
for(const mutate of [
  b=>{b.shapes[0].pieces[0].positions[0]+=.01;},
  b=>{b.shapes[0].pieces[0].indices[0]++;},
  b=>{b.shapes[0].pieces.pop();},
  b=>{b.shapes[0].id='wrong';},
  b=>{b.schemaVersion=9;}
]) {
  const changed=structuredClone(bundle);mutate(changed);
  assert.throws(()=>verifyBundleAgainstSource(changed,root),'changed bundle must be rejected');
}
console.log('Collider export self-test PASS: 30 shapes, 36 pieces, source round-trip, deterministic output and five corruptions rejected.');
