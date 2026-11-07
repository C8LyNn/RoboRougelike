'use strict';
const assert = require('node:assert/strict');
const { analyzeMesh, parseGlb, withinFootprintEnvelope } = require('./verify-collider-geometry.js');

const cube = [
  [-1,-1,-1], [1,-1,-1], [1,1,-1], [-1,1,-1],
  [-1,-1,1], [1,-1,1], [1,1,1], [-1,1,1]
];
const faces = [
  0,2,1, 0,3,2, 4,5,6, 4,6,7, 0,1,5, 0,5,4,
  3,7,6, 3,6,2, 0,4,7, 0,7,3, 1,2,6, 1,6,5
];
assert.equal(analyzeMesh(cube, faces).ok, true, 'a closed convex cube must pass');
assert.equal(analyzeMesh(cube, faces.slice(0,-3)).ok, false, 'a missing face must fail');
assert.equal(analyzeMesh(cube, faces.concat([0,0,1])).ok, false, 'a degenerate triangle must fail');
const reversed=faces.slice(); [reversed[0],reversed[1]]=[reversed[1],reversed[0]];
assert.equal(analyzeMesh(cube, reversed).ok, false, 'inconsistent winding must fail');
const invalid=faces.slice(); invalid[0]=99;
assert.equal(analyzeMesh(cube, invalid).ok, false, 'an out-of-range triangle index must fail');
const dented = cube.map(point => point.slice()); dented[6]=[0,0,0];
assert.equal(analyzeMesh(dented, faces).ok, false, 'a dented mesh must not pass the convexity check');
assert.throws(() => parseGlb(Buffer.from('not a GLB')), /GLB/i);
assert.equal(withinFootprintEnvelope([[-.26,0,0],[.26,0,0]],[[0,0,0]]),true,'a single-cell envelope must include its faces');
assert.equal(withinFootprintEnvelope([[.28,0,0]],[[0,0,0]]),false,'a vertex outside the cell envelope must fail');
assert.equal(withinFootprintEnvelope([[.275,0,0]],[[0,0,0],[1,0,0]]),true,'a two-cell rigid part may bridge the grid gap');
assert.equal(withinFootprintEnvelope([[.55,.55,0]],[[0,0,0],[1,0,0],[0,1,0]]),true,'the coarse envelope intentionally does not detect an empty L-shape corner');
console.log('Geometry self-test PASS: convex cube and valid footprint envelopes accepted; malformed topology, GLB and outside-envelope cases rejected.');
