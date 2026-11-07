'use strict';
const assert = require('node:assert/strict');
const { analyzeMesh, parseGlb } = require('./verify-collider-geometry.js');

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
console.log('Geometry self-test PASS: convex cube accepted; open, degenerate, reversed, invalid-index, dented and malformed cases rejected.');
