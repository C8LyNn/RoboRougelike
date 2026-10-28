#!/usr/bin/env node
/* B01-10 PASS15 standalone original-data validator (RESEARCH ONLY).
 * Run from an authorized checkout, after obtaining B01-25's immutable repaired
 * original JSON bytes:
 * node Docs/B01Design/PASS15/validate_ld02_encounter.mjs --map /path/to/LD02_ARENA.json
 *
 * Checks exact original Git blob SHA1, original MapDocument v2 schema, sealed
 * perimeter and route cells, exact prototype JS/HTML parity, 2 positive and
 * 2 negative transitions. Never compiles/launches Unity or awards Scrap.
 */
import { readFileSync } from "node:fs";
import { join, dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { createHash } from "node:crypto";
import { runInNewContext } from "node:vm";

const root = dirname(fileURLToPath(import.meta.url));
const config = JSON.parse(readFileSync(join(root,"LD02_EncounterRuleset_R1.json"),"utf8"));
const engine = readFileSync(join(root,"LD02_EncounterEngine.js"),"utf8");
const html = readFileSync(join(root,"LD02_EncounterPrototype.html"),"utf8");
const at = process.argv.indexOf("--map");
if(at < 0 || !process.argv[at+1]) {
  console.error("REQUIRED: --map /path/to/original B01-25 repaired LD02_ARENA.json; no fabricated fixture pass");
  process.exit(2);
}
const mapBytes = readFileSync(resolve(process.argv[at+1]));
const map = JSON.parse(mapBytes.toString("utf8"));
const blobSha = createHash("sha1").update(
  Buffer.concat([Buffer.from("blob " + mapBytes.length + "\0","utf8"),mapBytes])
).digest("hex");
let passed = 0, total = 0;
function test(name,condition) {
  total++;
  if(!condition) { console.error("FAIL "+name); process.exitCode=1; }
  else { passed++; console.log("PASS "+name); }
}
test("original git blob matches repaired B01-25",blobSha===config.sourceMap.gitBlobSha1);
test("original MapDocument v2 exact 168 local items",map.version===2&&map.globalFloor===false&&map.items.length===168);
test("original 4 corner Platforms",config.sourceMap.expectedCorners.every(c=>
  map.items.some(it=>it.kind===c.kind&&it.x===c.x&&it.z===c.z&&it.scaleY===4)));
const key=p=>p[0]+","+p[1];
const ground = new Set(map.items.filter(p=>p.kind===0).map(p=>p.x+","+p.z));
const objects = new Set(map.items.filter(p=>[1,6,7,8].includes(p.kind)).map(p=>p.x+","+p.z));
for(const route of config.scenario.routes) {
  const cells=route.waypoints;
  test("route "+route.id+" is contiguous, on Ground, not overlapping cover/walls",
    cells[0][0]===2&&cells[0][1]===5&&cells.at(-1)[0]===9&&cells.at(-1)[1]===5&&
    cells.every(p=>ground.has(key(p))&&!objects.has(key(p)))&&
    cells.slice(1).every((p,i)=>Math.abs(p[0]-cells[i][0])+Math.abs(p[1]-cells[i][1])===1)&&
    route.nominalMeters===4*(cells.length-1));
}
test("HTML embeds EXACT committed JS engine",html.includes(engine));
test("HTML embeds EXACT committed candidate rules",html.includes("const rules = "+JSON.stringify(config)+";"));
test("HTML map drawn from SAME original repaired object cells",
  html.includes("const mapObjects = "+JSON.stringify(map.items.filter(p=>p.kind!==0).map(p=>[p.kind,p.x,p.z]))+";"));
const vm = { };
runInNewContext(engine,vm,{timeout:1500});
const E = vm.LD02Encounter;
function scenario(loadout,actions) {
  let state=E.initial(loadout);
  for(const action of actions) state=E.advance(state,action,config);
  return state;
}
const S={type:"start"}, F={type:"finish"};
const O={type:"route",id:"OPEN"}, C={type:"route",id:"COVER"};
const A={type:"fire",group:"A"}, B={type:"fire",group:"B"};
const p1=scenario({types:["Basic"],barrels:2,functionalDrive:true},[S,O,A,A,F]);
test("POS01 short lane 2 kills then finish; no Scrap",p1.phase==="won"&&p1.kills===2&&p1.hp===82&&p1.scrap===0);
const p2=scenario({types:["Basic","Automatic"],barrels:12,functionalDrive:true},[S,C,B,A,F]);
test("POS02 flank lane 12 barrels both groups; no Scrap",p2.phase==="won"&&p2.kills===2&&p2.hp===93&&p2.scrap===0);
const n1=scenario(null,[S,O,F]);
test("NEG01 early finish cannot grant win",n1.phase==="combat"&&n1.kills===0&&n1.scrap===0);
const n2=scenario(null,[S,C,{type:"frame",damage:999,defeated:2},F]);
test("NEG02 simultaneous defeat cancels kills and rewards",n2.phase==="lost"&&n2.kills===0&&n2.scrap===0);
const n3=scenario({types:["Basic","Automatic","Heavy"],barrels:12,functionalDrive:true},[S]);
test("NEG03 third weapon family rejects launch",n3.phase==="setup"&&n3.lastError==="max-2-families");
const rec=scenario(null,[S,O,A,{type:"abort"},{type:"retry"}]);
test("abort and retry clears run state",rec.phase==="setup"&&rec.hp===100&&rec.kills===0);
console.log("TOTAL "+passed+"/"+total+"; INPUT_MAP_GIT_BLOB "+blobSha+
  "; JS_REDUCER_ONLY; UNITY_NOT_RUN; PHYSX_NOT_RUN; PLAYER_NOT_RUN");
if(passed!==total)process.exitCode=1;
