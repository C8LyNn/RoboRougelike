'use strict';
const assert=require('node:assert/strict');
const {buildBundle}=require('./export-unity-colliders.js');
const {verifySockets}=require('./verify-socket-surface.js');
const root=process.argv[2];assert.ok(root,'Provide the exact #963 R4Blocks source directory.');
const result=verifySockets(root,buildBundle(root));
assert.equal(result.count,190);
assert.deepEqual(result.failures.map(f=>[f.id,f.socket,f.kind,f.uncoveredSamples]),[
  ['R4B_HexColumn',0,'partial',4],['R4B_HexColumn',1,'partial',4],
  ['R4B_OctColumn',0,'full',4],['R4B_OctColumn',1,'full',4]
]);
for(const f of result.failures) {
  const gap=f.examples[0].nearestTriangleEdgeMeters;
  assert.ok(f.id==='R4B_HexColumn' ? gap>.00020&&gap<.00026 : gap>.040&&gap<.041,`${f.id} gap changed`);
}
console.log('Source baseline reproduced: 190 sampled patches, four known mismatches with expected gap ranges.');
