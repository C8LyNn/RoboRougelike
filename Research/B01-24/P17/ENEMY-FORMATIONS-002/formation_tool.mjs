// B01-24 P17 ENEMY-FORMATIONS-002. Real standalone content validator/exporter.
// Uses canonical geometry snapshots pinned to actual Git source blobs, NOT Unity PhysX.
import {readFileSync} from "node:fs";
import {createHash} from "node:crypto";
import {dirname,resolve} from "node:path";
import {fileURLToPath} from "node:url";

const dir=dirname(fileURLToPath(import.meta.url)),repo=resolve(dir,"../../../..");
const original={
 "Assets/R5/R5Arena.cs":"fbe1537036910e0dcc2b2c7e502e66848fcd14a5",
 "Assets/R1/R1EncounterManager.cs":"0a1c36b246c059a8bd9ec93a9d531cbe916b64bf",
 "Assets/R1/R1RunDefinition.cs":"5665e64d3d665c67d352eb5a9809a66fc5136698",
 "Assets/R5/R5RunPlan.cs":"6fde58f1fc621cde6488d9ee81c3510aa729839a"
};
const blobSha=bytes=>createHash("sha1").update(Buffer.from("blob "+bytes.length+"\0")).update(bytes).digest("hex");
// Exact 2D footprint snapshots of original R5Arena.Build Box definitions.
// Aabb is checked in X/Z at ground level: treats even low cover as solid.
const world=[
 [{x:0,z:42,w:86,h:2},{x:0,z:-42,w:86,h:2},{x:-42,z:0,w:2,h:82},{x:42,z:0,w:2,h:82}],
 Array.from({length:2},(_,i)=>i===0?-1:1).flatMap(side=>[
  {x:side*10,z:0,w:3,h:5},{x:side*21,z:13,w:5,h:2},{x:side*20,z:-13,w:4,h:3}]),
 [{x:-26,z:-5,w:4,h:6},{x:26,z:8,w:6,h:3}],
 [-1,1].flatMap(side=>[-1,1].map(row=>({x:side*17,z:row*7,w:10,h:9})))
   .concat([{x:-31,z:0,w:3,h:14},{x:31,z:0,w:3,h:14}])
];
const blocksFor=arena=>world[0].concat(world[arena+1]);
const distance=(a,b)=>Math.hypot(a[0]-b[0],a[1]-b[1]);
const circleBox=(p,box,r)=>Math.hypot(Math.max(Math.abs(p[0]-box.x)-box.w/2,0),
 Math.max(Math.abs(p[1]-box.z)-box.h/2,0))<=r;
export function readContent(){
 return JSON.parse(readFileSync(resolve(dir,"formations.json"),"utf8"));
}
export function checkPins(){
 for(const [path,expected] of Object.entries(original)){
  const bytes=readFileSync(resolve(repo,path)),actual=blobSha(bytes);
  if(actual!==expected)throw Error("SOURCE_DRIFT "+path+" expected "+expected+" got "+actual);
 }
 const manager=readFileSync(resolve(repo,"Assets/R1/R1EncounterManager.cs"),"utf8");
 if(!manager.includes("Vector3[] spawnPositions=null")||!manager.includes("spawnPositions[i]"))
  throw Error("SOURCE_CONTRACT_CHANGED optional enemy spawnPositions overload");
 return Object.keys(original).length;
}
export function validate(c){
 if(c.package_id!=="B01-24-ENEMY-FORMATIONS-002"||c.version!=="1.0.0")throw Error("schema version");
 if(c.conservative_vehicle_radius_m<2||c.min_separation_m<6||c.min_player_separation_m<20)
  throw Error("unsafe safety envelope");
 if(!Array.isArray(c.formations)||c.formations.length!==3)throw Error("expected three distinct arena formations");
 const seen=new Set(),arenaSet=new Set();
 for(const formation of c.formations){
  if(seen.has(formation.id)||arenaSet.has(formation.arena))throw Error("duplicated formation or arena");
  seen.add(formation.id);arenaSet.add(formation.arena);
  if(![0,1,2].includes(formation.arena)||Object.keys(formation.options).sort().join()!=="1,2,3,4")
   throw Error("unrecognized R5 arena or incomplete supported spawn counts");
  const blocks=blocksFor(formation.arena);
  for(let count=1;count<=4;count++){
   const positions=formation.options[String(count)];
   if(!Array.isArray(positions)||positions.length!==count)throw Error("incorrect source SpawnCount "+formation.id+"/"+count);
   for(const p of positions){
    if(!Array.isArray(p)||p.length!==2||p.some(n=>!Number.isFinite(n)))throw Error("non-finite 3D spawn");
    if(distance(p,[0,-22])<c.min_player_separation_m)throw Error("player spawn collision "+formation.id+"/"+count);
    if(blocks.some(block=>circleBox(p,block,c.conservative_vehicle_radius_m)))
     throw Error("arena collider overlap "+formation.id+"/"+count+"/"+JSON.stringify(p));
   }
   for(let i=0;i<positions.length;i++)for(let j=i+1;j<positions.length;j++)
    if(distance(positions[i],positions[j])<c.min_separation_m)
     throw Error("inter-enemy spawn overlap "+formation.id+"/"+count);
  }
 }
 return {formations:seen.size,layouts:12,validated_positions:c.formations.reduce((n,f)=>
  n+Object.values(f.options).reduce((sum,pos)=>sum+pos.length,0),0),source:"original-R5-matching-static-XZ-geometry-only"};
}
export function resolveFormation(c,id,count){
 if(!Number.isInteger(count)||count<1||count>4)throw Error("unsupported original SpawnCount");
 const f=c.formations.find(x=>x.id===id);
 if(!f)throw Error("unknown formation");
 return f.options[String(count)].map(p=>Object.freeze({x:p[0],y:0,z:p[1]}));
}
export function asUnityVectors(positions){
 const num=x=>Number.isInteger(x)?x+".0f":x+"f";
 return "new Vector3[] { "+positions.map(p=>
  "new Vector3("+num(p.x)+", "+num(p.y)+", "+num(p.z)+")").join(", ")+" }";
}
export function previewSvg(c,id,count){
 const f=c.formations.find(x=>x.id===id);
 if(!f)throw Error("unknown formation");
 const positions=resolveFormation(c,id,count),boxes=blocksFor(f.arena);
 const xx=x=>((x+44)/88*720).toFixed(2), zz=z=>((44-z)/88*720).toFixed(2);
 const blocks=boxes.map(o=>'<rect x="'+xx(o.x-o.w/2)+'" y="'+zz(o.z+o.h/2)+
  '" width="'+(o.w/88*720).toFixed(2)+'" height="'+(o.h/88*720).toFixed(2)+
  '" fill="#64748b" stroke="#94a3b8" stroke-width="1"/>').join("");
 const units=positions.map((p,i)=>'<circle cx="'+xx(p.x)+'" cy="'+zz(p.z)+
  '" r="'+(c.conservative_vehicle_radius_m/88*720).toFixed(2)+
  '" fill="#fdba74" stroke="#7c2d12" stroke-width="2"/><text x="'+xx(p.x)+
  '" y="'+zz(p.z)+'" font-size="13" text-anchor="middle" fill="#431407" dy="4">'+
  (i+1)+'</text>').join("");
 return '<svg xmlns="http://www.w3.org/2000/svg" width="720" height="720" viewBox="0 0 720 720">'+
  '<rect width="720" height="720" fill="#0f172a"/>'+blocks+
  '<circle cx="'+xx(0)+'" cy="'+zz(-22)+'" r="12" fill="#34d399"/>'+units+
  '<text x="20" y="38" fill="white" font-family="sans-serif" font-size="23">'+id+
  ' · '+count+' enemies</text></svg>';
}
if(process.argv[1]&&resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const c=readContent(),mode=process.argv[2]||"--check";
 checkPins();const receipt=validate(c);
 if(mode==="--check")console.log(JSON.stringify({status:"PASS",...receipt,unity:"NOT_RUN"}));
 else {
  const id=process.argv[3],count=Number(process.argv[4]);
  if(mode==="--emit")console.log(asUnityVectors(resolveFormation(c,id,count)));
  else if(mode==="--svg")console.log(previewSvg(c,id,count));
  else throw Error("usage: node formation_tool.mjs --check | --emit ID COUNT | --svg ID COUNT");
 }
}
