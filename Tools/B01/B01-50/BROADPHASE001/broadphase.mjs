/**
 * B01-50 BROADPHASE001: pure, deterministic 2D XZ disk-vs-swept-disk reference.
 * This is NOT Unity Physics, R4 collision, armor damage, or a native benchmark.
 * No external packages or network calls.
 */
const MAX_CELLS = 20000;
const finite = (x) => typeof x === 'number' && Number.isFinite(x);
const requireValue = (ok, message) => { if (!ok) throw new RangeError(message); };
function checkTarget(t) {
  requireValue(t && Number.isSafeInteger(t.id) && t.id >= 0 && finite(t.x) && finite(t.z) && finite(t.radius) && t.radius >= 0, 'invalid target');
}
function checkShot(p) {
  requireValue(p && finite(p.x0) && finite(p.z0) && finite(p.x1) && finite(p.z1) && finite(p.radius) && p.radius >= 0, 'invalid shot');
}
function cellRange(minX,maxX,minZ,maxZ,cell) {
  const a=Math.floor(minX/cell), b=Math.floor(maxX/cell), c=Math.floor(minZ/cell), d=Math.floor(maxZ/cell);
  const count=(b-a+1)*(d-c+1);
  requireValue(Number.isSafeInteger(count) && count <= MAX_CELLS && count > 0,'oversized spatial query/target');
  return {a,b,c,d};
}
/** Exact first normalized time t of continuous swept disk/target disk collision. */
export function hitTime(shot,target) {
  const dx=shot.x1-shot.x0, dz=shot.z1-shot.z0;
  const rx=shot.x0-target.x, rz=shot.z0-target.z;
  const rr=shot.radius+target.radius;
  const cc=rx*rx+rz*rz-rr*rr;
  if(cc<=0) return 0;
  const aa=dx*dx+dz*dz;
  if(aa===0) return null;
  const bb=2*(rx*dx+rz*dz);
  const disc=bb*bb-4*aa*cc;
  if(disc<0) return null;
  const t=(-bb-Math.sqrt(disc))/(2*aa);
  return t>=0 && t<=1 ? t : null;
}
function better(hit,best) {return hit && (!best || hit.time<best.time || (hit.time===best.time && hit.id<best.id));}
function bruteInternal(shot,targets,counter,prevalidated) {
  checkShot(shot);
  let best=null;
  for(const target of targets) {
    if (!prevalidated) checkTarget(target);
    counter.tested++;
    const t=hitTime(shot,target);
    if(t!==null) {const hit={id:target.id,time:t};if(better(hit,best))best=hit;}
  }
  return best;
}
export function bruteFirstHit(shot,targets,counter={tested:0}) {
  return bruteInternal(shot,targets,counter,false);
}
// Benchmark-only: all targets MUST be validated by a SpatialGrid constructor first.
export function bruteFirstHitPrevalidated(shot,targets,counter={tested:0}) {
  return bruteInternal(shot,targets,counter,true);
}
export class SpatialGrid {
  constructor(targets,cellSize=4) {
    requireValue(finite(cellSize) && cellSize>0,'invalid cell size');
    this.cellSize=cellSize;this.cells=new Map();this.targets=targets;
    const seen=new Set();this.inserts=0;
    for(let i=0;i<targets.length;i++) {
      const t=targets[i];checkTarget(t);
      requireValue(!seen.has(t.id),'duplicate target id');seen.add(t.id);
      const q=cellRange(t.x-t.radius,t.x+t.radius,t.z-t.radius,t.z+t.radius,cellSize);
      for(let x=q.a;x<=q.b;x++) for(let z=q.c;z<=q.d;z++) {
        const key=`${x}:${z}`;let b=this.cells.get(key);
        if(!b){b=[];this.cells.set(key,b);}b.push(i);this.inserts++;
      }
    }
  }
  firstHit(shot,counter={tested:0,visitedCells:0}) {
    checkShot(shot);
    const r=shot.radius;
    const q=cellRange(Math.min(shot.x0,shot.x1)-r,Math.max(shot.x0,shot.x1)+r,Math.min(shot.z0,shot.z1)-r,Math.max(shot.z0,shot.z1)+r,this.cellSize);
    const found=new Set();let best=null;
    for(let x=q.a;x<=q.b;x++) for(let z=q.c;z<=q.d;z++) {
      counter.visitedCells=(counter.visitedCells||0)+1;
      const bucket=this.cells.get(`${x}:${z}`);if(!bucket)continue;
      for(const i of bucket) {
        if(found.has(i))continue;found.add(i);counter.tested++;
        const t=hitTime(shot,this.targets[i]);
        if(t!==null){const hit={id:this.targets[i].id,time:t};if(better(hit,best))best=hit;}
      }
    }
    return best;
  }
}
export function makeScenario({seed=7,robotParts=24,enemies=8,projectiles=64,worldExtent=120}={}) {
  requireValue(Number.isSafeInteger(seed) && Number.isInteger(robotParts) && robotParts>=0 && Number.isInteger(enemies) && enemies>=0 && Number.isInteger(projectiles) && projectiles>=0 && finite(worldExtent) && worldExtent>1,'invalid scenario');
  let s=seed>>>0;
  function random(){s^=(s<<13);s^=s>>>17;s^=(s<<5);return (s>>>0)/4294967296;}
  const targets=[];
  for(let e=0;e<enemies;e++) {
    const anchorX=(random()*2-1)*worldExtent*.8;
    const anchorZ=(random()*2-1)*worldExtent*.8;
    for(let k=0;k<robotParts;k++) {
      const ring=Math.floor(k/8)+1;
      const angle=(k%8)*Math.PI/4;
      targets.push({id:targets.length,x:anchorX+ring*.57*Math.cos(angle),z:anchorZ+ring*.57*Math.sin(angle),radius:.32});
    }
  }
  const shots=[];
  for(let k=0;k<projectiles;k++) {
    const x=(random()*2-1)*worldExtent,z=(random()*2-1)*worldExtent;
    const angle=random()*Math.PI*2,len=2+random()*18;
    shots.push({x0:x,z0:z,x1:x+len*Math.cos(angle),z1:z+len*Math.sin(angle),radius:.11});
  }
  return {seed,robotParts,enemies,projectiles,targets,shots};
}
export function validateSameHits(a,b) {
  if(a===null || b===null) return a===b;
  return a.id===b.id && a.time===b.time;
}
