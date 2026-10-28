import test from 'node:test';
import assert from 'node:assert/strict';
import {hitTime,bruteFirstHit,bruteFirstHitPrevalidated,SpatialGrid,makeScenario,validateSameHits} from '../broadphase.mjs';
function shot(x0,z0,x1,z1,radius=0) {return {x0,z0,x1,z1,radius};}
const targets=[{id:2,x:3,z:0,radius:.5},{id:7,x:5,z:1,radius:1}];
test('exact swept collision time and earliest target',()=>{
 const s=shot(0,0,10,0,0);
 assert.ok(Math.abs(hitTime(s,targets[0])-.25)<1e-12);
 assert.deepEqual(bruteFirstHit(s,targets),new SpatialGrid(targets,1).firstHit(s));
});
test('tangent, initial overlap, zero motion and miss',()=>{
 const t={id:1,x:2,z:1,radius:1};
 for(const s of [shot(0,0,4,0),shot(2,1,2,1),shot(2,3,2,3),shot(0,4,5,4)])
  assert.equal(validateSameHits(bruteFirstHit(s,[t]),new SpatialGrid([t],.5).firstHit(s)),true);
 assert.equal(hitTime(shot(2,1,2,1),t),0);
 assert.equal(hitTime(shot(0,4,5,4),t),null);
});
test('tie-break identical hit time by smaller stable ID',()=>{
 const items=[{id:55,x:5,z:0,radius:1},{id:3,x:5,z:0,radius:1}];
 assert.equal(bruteFirstHit(shot(0,0,10,0),items).id,3);
 assert.equal(new SpatialGrid(items,1).firstHit(shot(0,0,10,0)).id,3);
});
test('negative coordinates and grid boundary do not miss hits',()=>{
 const items=[{id:1,x:-4,z:-4,radius:1},{id:2,x:0,z:0,radius:1}];
 for(const p of [shot(-12,-4,0,-4),shot(-8,-8,8,8),shot(-1,-2,-1,2),shot(-5,-3,-5,-3)])
 assert.equal(validateSameHits(bruteFirstHit(p,items),new SpatialGrid(items,.5).firstHit(p)),true);
});
test('large target spans many cells, no duplicate narrow-phase checks',()=>{
 const items=[{id:1,x:0,z:0,radius:14},{id:2,x:8,z:8,radius:2}];
 const g=new SpatialGrid(items,1);const counts={tested:0,visitedCells:0};
 assert.equal(validateSameHits(bruteFirstHit(shot(-14,-14,14,14),items),g.firstHit(shot(-14,-14,14,14),counts)),true);
 assert.equal(counts.tested,2);
});
test('invalid input, oversized grids, duplicate target IDs rejected',()=>{
 assert.throws(()=>new SpatialGrid([{id:0,x:0,z:0,radius:3}],0),/cell size/);
 assert.throws(()=>new SpatialGrid([{id:0,x:0,z:0,radius:3}],.01),/oversized/);
 assert.throws(()=>new SpatialGrid([{id:0,x:0,z:0,radius:1},{id:0,x:2,z:2,radius:1}]),/duplicate/);
 assert.throws(()=>new SpatialGrid([{id:0,x:NaN,z:0,radius:1}]),/invalid/);
 assert.throws(()=>new SpatialGrid([],1).firstHit(shot(0,0,1,Infinity)),/invalid shot/);
 assert.throws(()=>new SpatialGrid([],1).firstHit(shot(0,0,100000,100000)),/oversized/);
});
test('determinism: same seed identical positions, different seed differs',()=>{
 const a=makeScenario({seed:99}),b=makeScenario({seed:99}),c=makeScenario({seed:100});
 assert.deepEqual(a,b);assert.notDeepEqual(a.targets,c.targets);
});
test('property: 2/6/12 gun proxies × 12 seeds: exact collision parity',()=>{
 for(const part of [24,72,144]) for(let seed=1;seed<=12;seed++){
  const world=makeScenario({seed,robotParts:part,enemies:6,projectiles:120});
  const grid=new SpatialGrid(world.targets,3);
  for(const p of world.shots) assert.equal(validateSameHits(bruteFirstHit(p,world.targets),grid.firstHit(p)),true,`seed=${seed} parts=${part}`);
 }
});
test('dynamic target positions: rebuild index matches scan after translation',()=>{
 const {targets,shots}=makeScenario({seed:77,robotParts:24,enemies:8,projectiles:100});
 for(let frame=0;frame<9;frame++) {
  const moved=targets.map(t=>({...t,x:t.x+frame*.41,z:t.z+Math.sin(frame)*.2}));
  const grid=new SpatialGrid(moved,2);
  for(const p of shots)assert.equal(validateSameHits(bruteFirstHit(p,moved),grid.firstHit(p)),true);
 }
});
test('baseline brute force count matches target × projectile pairs',()=>{
 const w=makeScenario({seed:13,robotParts:24,enemies:8,projectiles:64});const c={tested:0};
 for(const p of w.shots)bruteFirstHit(p,w.targets,c);
 assert.equal(c.tested,24*8*64);
});

test('prevalidated baseline equals checked baseline on validated world',()=>{
 const w=makeScenario({seed:6,robotParts:72,enemies:4,projectiles:90});
 new SpatialGrid(w.targets,4); // proof all targets have been checked
 for(const p of w.shots)assert.equal(validateSameHits(bruteFirstHit(p,w.targets),bruteFirstHitPrevalidated(p,w.targets)),true);
});
