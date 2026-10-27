// B01-25 PASS11: research-only acceptance fixtures. No Unity/game source modifications.
// Run: node acceptance-suite.mjs
const KIND={"Ground":0,"Wall":1,"Ramp":2,"Obstacle":3,"Spawn":4,"Finish":5,"Platform":6,"Pillar":7,"LowBarrier":8,"Steps":9};
const SOURCE={
  "pr": 310,
  "commit": "231005c6d0351945a8ee6684f3856af118f0e94f",
  "blobs": {
    "Assets/MapEditor/MapDraft.cs": "b5f8f395f93972c0ede000701dbbada8fcfcdfdb",
    "Assets/MapEditor/MapWorld.cs": "8928908db123b35dcab4b324fe9d3a7a8fceee4e",
    "Assets/MapEditor/R1MapEditor.cs": "c92e120d9bc3ce26b3eac68cafdfeea4bacefad7",
    "Assets/R3/R3VehicleModel.cs": "9f1bf190c7535182c6ce375cbfe67f5b2862e811"
  },
  "note": "Independent Node/V8 model of original C# MapDraft load/footprint/TestError; NOT original C# execution or Unity/PhysX proof."
};
function mk(kind,x,z,rotation=0,scaleX=1,scaleY=1,scaleZ=1){return {kind:KIND[kind],x,z,rotation,scaleX,scaleY,scaleZ};}

function key(x,z){return `${x},${z}`;}

function makeFixtures(){
  const doc=(globalFloor,ground,objects)=>{
    const cells=[...ground].map(v=>v.split(",").map(Number)).sort((a,b)=>a[0]-b[0]||a[1]-b[1]);
    return {version:2,globalFloor,items:[...cells.map(([x,z])=>mk("Ground",x,z)),...objects]};
  };
  const g1=new Set();
  for(let x=0;x<=12;x++)for(let z=0;z<=3;z++)g1.add(key(x,z));
  const ld01=doc(false,g1,[mk("Spawn",0,1),mk("Finish",12,1),mk("Pillar",3,1),mk("Pillar",6,2),mk("LowBarrier",8,1),mk("Ramp",9,2),mk("Steps",10,1)]);
  const ld02=doc(true,new Set(),[mk("Spawn",0,5),mk("Finish",11,5),mk("Pillar",3,2),mk("Pillar",3,7),mk("Pillar",8,2),mk("Pillar",8,7),mk("LowBarrier",5,4),mk("LowBarrier",5,6),mk("Wall",5,0),mk("Wall",5,9)]);
  const g3=new Set();
  for(let x=0;x<=8;x++)for(const z of [1,2,4,5])g3.add(key(x,z));
  for(const x of [0,1,7,8])g3.add(key(x,3));
  const ld03=doc(false,g3,[mk("Spawn",0,2),mk("Finish",8,2),mk("Wall",4,1),mk("LowBarrier",4,2),mk("Ramp",5,2)]);
  return {LD01:ld01,LD02:ld02,LD03:ld03};
}

function cells(p){
  const out=[];
  for(let x=0;x<p.scaleX;x++)for(let z=0;z<p.scaleZ;z++){
    let rx=x,rz=z;
    switch(p.rotation){case 1:rx=z;rz=-x;break;case 2:rx=-x;rz=-z;break;case 3:rx=-z;rz=x;break;}
    out.push(key(p.x+rx,p.z+rz));
  }
  return out;
}

function loadCheck(doc){
  if(!doc||![1,2].includes(doc.version)||!Array.isArray(doc.items)||doc.items.length>2000)return "Niepoprawny format lub wersja mapy.";
  let spawns=0,finishes=0;
  const floor=new Set(),objects=new Set();
  for(const raw of doc.items){
    if(!raw)return "Niepoprawny element.";
    const p={...raw};
    if(doc.version===1)p.scaleX=p.scaleY=p.scaleZ=1;
    if(!Number.isInteger(p.kind)||p.kind<0||p.kind>9||
      !Number.isInteger(p.rotation)||p.rotation<0||p.rotation>3||
      ![p.scaleX,p.scaleY,p.scaleZ].every(v=>Number.isInteger(v)&&v>=1&&v<=4)||
      ![p.x,p.z].every(v=>Number.isInteger(v)&&Math.abs(v)<=50))return "Niepoprawny element.";
    const footprint=cells(p);
    if(footprint.some(k=>k.split(",").some(v=>Math.abs(Number(v))>50)))return "Niepoprawny element.";
    const dest=p.kind===KIND.Ground?floor:objects;
    for(const k of footprint){if(dest.has(k))return "Nakładające się elementy.";dest.add(k);}
    if(p.kind===KIND.Spawn)spawns++;
    if(p.kind===KIND.Finish)finishes++;
  }
  if(spawns>1||finishes>1)return "Mapa może mieć tylko jeden start i jedną metę.";
  return null;
}

function testError(doc){
  const spawn=doc.items.find(p=>p.kind===KIND.Spawn);
  const finish=doc.items.find(p=>p.kind===KIND.Finish);
  if(!spawn)return "Dodaj punkt startowy.";
  if(!finish)return "Dodaj metę.";
  if(!doc.globalFloor){
    const floor=new Set(doc.items.filter(p=>p.kind===KIND.Ground).flatMap(cells));
    if(cells(spawn).some(k=>!floor.has(k)))return "Dodaj podłoże pod punktem startowym.";
  }
  return null;
}

function safeCells(doc,allowRisk=false){
  const ground=new Set(doc.items.filter(p=>p.kind===KIND.Ground).flatMap(cells));
  const forbidden=new Set(doc.items.filter(p=>[KIND.Wall,KIND.Pillar,KIND.Obstacle,...(allowRisk?[]:[KIND.LowBarrier])].includes(p.kind)).flatMap(cells));
  const inside=(x,z)=>Math.abs(x)<=50&&Math.abs(z)<=50&&(doc.globalFloor||ground.has(key(x,z)))&&!forbidden.has(key(x,z));
  return {ground,forbidden,inside};
}

function hasGridRoute(doc,allowRisk=false,deleted=[]){
  const start=doc.items.find(p=>p.kind===KIND.Spawn),finish=doc.items.find(p=>p.kind===KIND.Finish);
  if(!start||!finish)return false;
  const {inside}=safeCells(doc,allowRisk),removed=new Set(deleted);
  const q=[[start.x,start.z]],seen=new Set([key(start.x,start.z)]);
  for(let i=0;i<q.length;i++){
    const [x,z]=q[i];if(x===finish.x&&z===finish.z)return true;
    for(const [dx,dz] of [[1,0],[-1,0],[0,1],[0,-1]]){
      const xx=x+dx,zz=z+dz,k=key(xx,zz);
      if(!seen.has(k)&&!removed.has(k)&&inside(xx,zz)){seen.add(k);q.push([xx,zz]);}
    }
  }
  return false;
}

function routeWitness(doc,path,allowRisk=false){
  const {inside}=safeCells(doc,allowRisk);
  const a=doc.items.find(p=>p.kind===KIND.Spawn),b=doc.items.find(p=>p.kind===KIND.Finish);
  if(!a||!b||path.length<2)return false;
  if(key(...path[0])!==key(a.x,a.z)||key(...path[path.length-1])!==key(b.x,b.z))return false;
  return path.every(([x,z],i)=>inside(x,z)&&(i===0||Math.abs(x-path[i-1][0])+Math.abs(z-path[i-1][1])===1));
}

function routesLD03(){
  const short=Array.from({length:9},(_,x)=>[x,2]);
  const detour=[[0,2],[0,3],[0,4],[0,5],...[1,2,3,4,5,6,7,8].map(x=>[x,5]),[8,4],[8,3],[8,2]];
  return {short,detour};
}

function proposedOutcome(s){
  if(!s.hasStart||!s.hasFinish||!s.spawnHasFloor)return "DRAFT_NOT_TESTABLE";
  if(!s.weaponFunctional||!s.mobilityFunctional)return "DEPLOY_BLOCKED_CURRENT_CODE";
  if(s.outcome==="TIMEOUT")return "TIMEOUT_DESIGN_ONLY_NOT_IN_EDITOR";
  if(s.outcome==="STUCK")return "STUCK_DESIGN_ONLY_NOT_IN_EDITOR";
  if(s.outcome==="TOUCH"&&!s.provenance)return "SANDBOX_TOUCH_NOT_ROUTE_PROOF";
  if(s.combat&&s.outcome==="TOUCH")return "COMBAT_OBJECTIVE_UNSUPPORTED";
  return "UNVERIFIED";
}

function runSuite(){
  const F=makeFixtures(),t=[];
  const test=(name,fn)=>{try{if(fn()!==true)throw Error("assertion returned false");t.push({name,status:"PASS"});}catch(e){t.push({name,status:"FAIL",error:String(e.message||e)});}};
  const clone=d=>JSON.parse(JSON.stringify(d));
  const A=F.LD01,B=F.LD02,C=F.LD03;
  const w=routesLD03();
  // A: unique machine-checkable acceptance per original design.
  test("A01 LD01 exact v2 schema + one Spawn/Finish + grounded start",()=>loadCheck(A)===null&&testError(A)===null&&A.items.filter(p=>p.kind===KIND.Ground).length===52);
  test("A02 LD01 five obstacle instances + conservative 4-neighbour route",()=>[KIND.Pillar,KIND.LowBarrier,KIND.Ramp,KIND.Steps].every(k=>A.items.some(p=>p.kind===k))&&hasGridRoute(A)===true);
  test("A03 LD02 valid arena authoring footprint + four covers + two barriers",()=>loadCheck(B)===null&&testError(B)===null&&B.globalFloor&&B.items.filter(p=>p.kind===KIND.Pillar).length===4&&B.items.filter(p=>p.kind===KIND.LowBarrier).length===2);
  test("A04 LD02 conservative Spawn->Finish grid reachability",()=>hasGridRoute(B)===true);
  test("A05 LD03 valid branching JSON + both witness paths",()=>loadCheck(C)===null&&testError(C)===null&&routeWitness(C,w.short,true)&&routeWitness(C,w.detour,false));
  test("A06 LD03 distinct inner paths and detour longer",()=>{
    const a=new Set(w.short.slice(1,-1).map(p=>key(...p))),b=new Set(w.detour.slice(1,-1).map(p=>key(...p)));
    return [...a].every(k=>!b.has(k))&&w.detour.length>w.short.length&&w.short.length===9&&w.detour.length===15;
  });
  // B: negative coverage explicitly separate from runtime state.
  test("B01 missing Finish rejected by current TestError",()=>{let d=clone(A);d.items=d.items.filter(p=>p.kind!==KIND.Finish);return loadCheck(d)===null&&testError(d)==="Dodaj metę.";});
  test("B02 missing Spawn floor rejected by current TestError",()=>{let d=clone(A);d.items=d.items.filter(p=>!(p.kind===KIND.Ground&&p.x===0&&p.z===1));return loadCheck(d)===null&&testError(d)==="Dodaj podłoże pod punktem startowym.";});
  test("B03 top-layer overlap rejected on JSON import",()=>{let d=clone(A);d.items.push(mk("Pillar",0,1));return loadCheck(d)==="Nakładające się elementy.";});
  test("B04 duplicate Finish rejected on JSON import",()=>{let d=clone(B);d.items.push(mk("Finish",10,8));return loadCheck(d)==="Mapa może mieć tylko jeden start i jedną metę.";});
  test("B05 rotated scaled footprint crossing map bound rejected",()=>{let d=clone(B);d.items.push(mk("Platform",50,50,1,4,1,4));return loadCheck(d)==="Niepoprawny element.";});
  test("B06 scale>4 rejected",()=>{let d=clone(B);d.items.push(mk("Wall",20,20,0,5));return loadCheck(d)==="Niepoprawny element.";});
  test("B07 sever safe detour but keep testError null and risky short available",()=>{
    const d=clone(C);d.items=d.items.filter(p=>!(p.kind===KIND.Ground&&p.x===4&&[4,5].includes(p.z)));
    const cut=[key(4,4),key(4,5)];
    return loadCheck(d)===null&&testError(d)===null&&!routeWitness(d,w.detour,false)&&hasGridRoute(d,true)===true&&!hasGridRoute(d,false,cut);
  });
  test("B08 noncombat map test cannot prove two enemy kills",()=>proposedOutcome({hasStart:true,hasFinish:true,spawnHasFloor:true,weaponFunctional:true,mobilityFunctional:true,combat:true,outcome:"TOUCH",provenance:true})==="COMBAT_OBJECTIVE_UNSUPPORTED");
  test("B09 weaponless robot prevented from starting current editor test",()=>proposedOutcome({hasStart:true,hasFinish:true,spawnHasFloor:true,weaponFunctional:false,mobilityFunctional:true})==="DEPLOY_BLOCKED_CURRENT_CODE");
  test("B10 mobility-less robot prevented from starting current editor test",()=>proposedOutcome({hasStart:true,hasFinish:true,spawnHasFloor:true,weaponFunctional:true,mobilityFunctional:false})==="DEPLOY_BLOCKED_CURRENT_CODE");
  test("B11 simultaneous weaponless and mobility-less also blocked",()=>proposedOutcome({hasStart:true,hasFinish:true,spawnHasFloor:true,weaponFunctional:false,mobilityFunctional:false})==="DEPLOY_BLOCKED_CURRENT_CODE");
  test("B12 timeout is not yet a runtime failure reason",()=>proposedOutcome({hasStart:true,hasFinish:true,spawnHasFloor:true,weaponFunctional:true,mobilityFunctional:true,outcome:"TIMEOUT"})==="TIMEOUT_DESIGN_ONLY_NOT_IN_EDITOR");
  test("B13 stuck robot is not yet a runtime failure reason",()=>proposedOutcome({hasStart:true,hasFinish:true,spawnHasFloor:true,weaponFunctional:true,mobilityFunctional:true,outcome:"STUCK"})==="STUCK_DESIGN_ONLY_NOT_IN_EDITOR");
  test("B14 teleported/unsupported Finish touch is not verified route",()=>proposedOutcome({hasStart:true,hasFinish:true,spawnHasFloor:true,weaponFunctional:true,mobilityFunctional:true,outcome:"TOUCH",provenance:false})==="SANDBOX_TOUCH_NOT_ROUTE_PROOF");
  test("B15 current TestError accepts disconnected/unfloored Finish",()=>{
    const d=clone(A);
    d.items=d.items.filter(p=>!(p.kind===KIND.Ground&&p.x===12&&p.z===1));
    return loadCheck(d)===null&&testError(d)===null;
  });
  // C: ranked and specific owners, never a claim of native completion.
  const matrix=[
    {rank:1,id:"LD01",sourceReady:"JSON_V2_AND_STATIC_ROUTE",oneDependency:"HQ licensed isolated native Light/Heavy Rigidbody traversals (B01-60 with B01-21); route success policy HQ",owner:"B01-21 / B01-60 / HQ"},
    {rank:2,id:"LD03",sourceReady:"TWO_DISTINCT_GRID_PATHS_ONLY",oneDependency:"B01-21 physical 2-rig clearance on both variants; HQ sandbox vs route-verified choice",owner:"B01-21 / HQ"},
    {rank:3,id:"LD02",sourceReady:"EDITOR_GEOMETRY_ONLY",oneDependency:"Codex/HQ-approved enemy encounter/win-condition integration; independent B01-24 LOS QA",owner:"HQ / Codex / B01-24"}
  ];
  test("C01 matrix ranks each unique existing brief exactly once",()=>matrix.length===3&&matrix.map(x=>x.id).join(",")==="LD01,LD03,LD02"&&new Set(matrix.map(x=>x.rank)).size===3);
  test("C02 matrix does not claim verified runtime or production combat",()=>matrix.every(x=>x.oneDependency&&x.owner&&/ONLY|ROUTE/.test(x.sourceReady))&&matrix.find(x=>x.id==="LD02").oneDependency.includes("Codex"));
  const passed=t.filter(x=>x.status==="PASS").length,failed=t.length-passed;
  return {schema:"B01-25-PASS11-RESULT-v1",source:SOURCE,passed,failed,tests:t,matrix,limits:["Portable JavaScript mirrors only documented original MapDraft invariants; C# Unity JsonUtility not executed.","4-neighbour footprint/path is not vehicle physics, collider clearance, slope, turning radius or true PlayMode.","Proposed timeout/stuck/combat outcomes are not implemented in editor.","Actual finish touch is not a verified full route; HQ route policy undecided."]};
}

const summary=runSuite();
console.log(JSON.stringify(summary,null,2));
if(summary.failed>0)process.exitCode=1;
