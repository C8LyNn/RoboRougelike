import test from "node:test";
import assert from "node:assert/strict";
import {readdirSync,readFileSync} from "node:fs";
import {join} from "node:path";
import {validateMap,free,PROFILES,createState,tick,assist,runAssist} from "./core.mjs";
const dir=process.env.P17_BLUEPRINTS_DIR;
if(!dir)throw Error("P17_BLUEPRINTS_DIR must point to REAL original-CSharp output");
const files=readdirSync(dir).filter(f=>/^P17-COURSE-.+-seed-[0-9]+\.json$/.test(f)).sort();
assert.equal(files.length,18,"18 actual original MAPGEN generated courses required");
const maps=files.map(f=>JSON.parse(readFileSync(join(dir,f),"utf8")));
test("source receipt has all 18 original distinct geometry hashes",()=>{
 assert.equal(new Set(maps.map(m=>m.geometryHash)).size,18);
 assert.deepEqual([...new Set(maps.map(m=>m.config.segments))].sort(),[3,4,5]);
});
for(const m of maps)for(const name of ["Light","Heavy"]){
 test(m.variantId+"/seed"+m.seed+"/"+name+": auto checkpoint completion",()=>{
  assert.equal(validateMap(m),m);
  const p=PROFILES[name];
  assert.equal(free(m,p,m.start.x,m.start.z),true);
  assert.equal(free(m,p,1e6,1e6),false);
  const a=runAssist(m,name);
  assert.ok(a.done,"not finished cp "+a.cp+"/"+(m.checkpoints.length+1)+" pos "+a.x+","+a.z+" collisions="+a.collisions);
  assert.equal(a.cp,m.checkpoints.length+1);
  assert.equal(a.collisions,0,"auto spine should have full clearance");
  assert.ok(a.elapsed>0 && a.elapsed<240);
  const b=runAssist(m,name);
  assert.deepEqual(a,b,"deterministic fixed-step replay");
 });
}
test("simulation without controls, pause, and restart",()=>{
 const m=maps[0];
 let s=createState(m,"Light");
 for(let i=0;i<50;i++)s=tick(m,s,{x:0,z:0},.05);
 assert.equal(s.done,false);assert.equal(s.cp,0);
 const pause={...s,paused:true};
 assert.deepEqual(tick(m,pause,{x:1,z:1},.05),pause);
 assert.equal(createState(m,"Light").elapsed,0);
});
test("wrong profile, invalid dt, mismatched source geometry all reject",()=>{
 const m=maps[0];
 assert.throws(()=>createState(m,"Tank"),/UNKNOWN_PROFILE/);
 const s=createState(m,"Light");
 assert.throws(()=>tick(m,s,{},.2),/BAD_FIXED_DT/);
 assert.throws(()=>tick({...m,geometryHash:"a".repeat(64)},s,{},.05),/MAP_STATE_MISMATCH/);
});
test("actual collision with deliberately blocked center prevents movement",()=>{
 const m=maps[0];
 const s=createState(m,"Heavy");
 const block={kind:"Obstacle",position:{x:s.x,y:1,z:s.z+.25},size:{x:6,y:2,z:1},rotationEuler:{x:0,y:0,z:0}};
 const altered={...m,elements:[...m.elements,block]};
 const after=tick(altered,s,{x:0,z:1},.05);
 assert.equal(after.collisions,1);
 assert.equal(after.z,s.z);
 assert.equal(after.done,false);
});
test("tampered source, checkpoint count, invalid obstacle kind all reject",()=>{
 const m=maps[0];
 assert.throws(()=>validateMap({...m,sourceSha:"0".repeat(40)}),/INVALID_MAPGEN_BLUEPRINT/);
 assert.throws(()=>validateMap({...m,checkpoints:[]}),/INVALID_MAPGEN_BLUEPRINT/);
 assert.throws(()=>validateMap({...m,elements:[...m.elements,{kind:"Spike",size:{x:1,y:1,z:1},position:{x:0,y:0,z:0}}]}),/INVALID_MAP_ELEMENT/);
});
test("forward/reverse/lateral inputs compute finite movement",()=>{
 const m=maps[0];
 const s=createState(m,"Light");
 const f=tick(m,s,{x:0,z:1},.05);
 assert.ok(f.z>=s.z && Number.isFinite(f.z));
 const b=tick(m,s,{x:0,z:-1},.05);
 assert.ok(b.z<=s.z && Number.isFinite(b.z));
 assert.ok(Number.isFinite(assist(m,s).z));
});
