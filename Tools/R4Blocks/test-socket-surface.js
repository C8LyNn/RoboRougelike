'use strict';
const assert=require('node:assert/strict');
const {sampleSocketSurface}=require('./verify-socket-surface.js');

// One square collider face on x = .26 m, represented by two triangles.
const points=[.26,-.26,-.26, .26,.26,-.26, .26,.26,.26, .26,-.26,.26];
const full={cell:[0,0,0],face:[1,0,0],offset:[0,0],half:[.47,.47]};
const square=[{positions:points,indices:[0,1,2,0,2,3]}];
assert.equal(sampleSocketSurface(full,square).covered,true,'a full square face should support its socket samples');
assert.equal(sampleSocketSurface(full,[{positions:points,indices:[0,1,2]}]).covered,false,'half a face must not satisfy a full socket');
assert.equal(sampleSocketSurface({...full,face:[-1,0,0]},square).covered,false,'the opposite face must not be reported as supported');
assert.equal(sampleSocketSurface({...full,half:[.7,.7]},square).covered,false,'a patch beyond the collider face must fail');
assert.throws(()=>sampleSocketSurface({...full,face:[1,1,0]},square),/face/i);
function polygon(vertices) {
  const positions=vertices.flatMap(([x,z])=>[x,.26,z]),indices=[];
  for(let i=1;i<vertices.length-1;i++)indices.push(0,i,i+1);
  return [{positions,indices}];
}
const hex=Array.from({length:6},(_,i)=>[.26*Math.cos(i*Math.PI/3),.26*Math.sin(i*Math.PI/3)]);
const up={cell:[0,0,0],face:[0,1,0],offset:[0,0],half:[.3,.3]};
const hexResult=sampleSocketSurface(up,polygon(hex));
assert.equal(hexResult.uncoveredSamples,4,'hexagonal end must expose the four oversized patch corners');
assert.ok(hexResult.examples[0].nearestTriangleEdgeMeters>.0001 && hexResult.examples[0].nearestTriangleEdgeMeters<.0005,'hex overshoot is submillimetre');
const oct=polygon([[.26,.20],[.20,.26],[-.20,.26],[-.26,.20],[-.26,-.20],[-.20,-.26],[.20,-.26],[.26,-.20]]);
const octResult=sampleSocketSurface({...up,half:[.47,.47]},oct);
assert.equal(octResult.uncoveredSamples,4,'chamfered end must not satisfy square full-face corners');
assert.ok(octResult.examples[0].nearestTriangleEdgeMeters>.04 && octResult.examples[0].nearestTriangleEdgeMeters<.041,'chamfer overshoot is roughly 40 mm');
console.log('Socket-surface self-test PASS: square coverage, missing/opposite faces, oversized patches, invalid axes, hex and chamfered ends.');
