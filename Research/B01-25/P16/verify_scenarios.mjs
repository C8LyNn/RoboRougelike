// B01-25 PASS16 — independently runnable Node 18+ source/data verifier.
// Only derives MapWorld source-aligned geometry; NEVER asserts Unity/PhysX execution.
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import assert from "node:assert/strict";
const root=dirname(fileURLToPath(import.meta.url));
const manifest=JSON.parse(readFileSync(join(root,"scenarios.manifest.json"),"utf8"));
const key=(x,z)=>x+","+z;
function bbox(i) {
 const x=4*i.x,z=4*i.z;
 if(i.kind===1){const hx=i.rotation%2?.25:2,hz=i.rotation%2?2:.25;return {kind:1,x0:x-hx,x1:x+hx,z0:z-hz,z1:z+hz};}
 if(i.kind===6)return {kind:6,x0:x-2,x1:x+2,z0:z-2,z1:z+2};
 if(i.kind===7)return {kind:7,x0:x-.75,x1:x+.75,z0:z-.75,z1:z+.75};
 if(i.kind===8)return {kind:8,x0:x-2,x1:x+2,z0:z-.25,z1:z+.25};
 return null;
}
function hit(A,B,r,avoidStart=false) {
 let lo=0,hi=1;
 for(const [k,b,c] of [["x","x0","x1"],["z","z0","z1"]]){
 const d=B[k]-A[k];
 if(Math.abs(d)<1e-8){if(A[k]<r[b]||A[k]>r[c])return false;}
 else {const t0=(r[b]-A[k])/d,t1=(r[c]-A[k])/d;lo=Math.max(lo,Math.min(t0,t1));hi=Math.min(hi,Math.max(t0,t1));if(lo>hi)return false;}
 }
 return !avoidStart || (lo>1e-6 && lo<.999999);
}
let checks=0;
for(const s of manifest.scenarios){
 const doc=JSON.parse(readFileSync(join(root,s.file),"utf8"));
 const items=doc.items;
 const assertCase=(ok,message)=>{checks++;assert.ok(ok,s.id+": "+message);};
 const [w,h]=s.footprint.cells;
 assertCase(doc.version===2&&doc.globalFloor===false,"MapDocument v2/local floor");
 assertCase(items.length<=4000&&items.filter(i=>i.kind===0).length<=2000&&items.filter(i=>i.kind!==0).length<=2000,"new MapDraft per-kind caps");
 assertCase(items.every(i=>Number.isInteger(i.kind)&&i.kind>=0&&i.kind<=9&&Number.isInteger(i.rotation)&&i.rotation>=0&&i.rotation<4&&[i.scaleX,i.scaleY,i.scaleZ].every(n=>Number.isInteger(n)&&n>=1&&n<=4)&&Math.abs(i.x)<=50&&Math.abs(i.z)<=50),"MapDraft valid ranges");
 const floors=items.filter(i=>i.kind===0),solid=items.filter(i=>i.kind!==0&&i.kind!==4&&i.kind!==5),objects=items.filter(i=>i.kind!==0);
 const floorCells=new Set(floors.map(i=>key(i.x,i.z))),blocked=new Set(solid.map(i=>key(i.x,i.z)));
 assertCase(floors.length===w*h&&floorCells.size===w*h,"complete local ground");
 assertCase(objects.length===new Set(objects.map(i=>key(i.x,i.z))).size,"non-ground unique footprint");
 assertCase(items.filter(i=>i.kind===4).length===1&&items.filter(i=>i.kind===5).length===1,"unique spawn/finish");
 assertCase(floorCells.has(key(...s.sp))&&floorCells.has(key(...s.finish))&&!blocked.has(key(...s.sp))&&!blocked.has(key(...s.finish)),"spawn/finish ground and clear");
 const corners=[[0,0],[0,h-1],[w-1,0],[w-1,h-1]];
 assertCase(corners.every(([x,z])=>objects.some(i=>i.kind===6&&i.x===x&&i.z===z&&i.scaleY===4)),"sealed source geometry corner platform");
 const sourceBoxes=items.map(bbox).filter(Boolean);
 const xm=(w-1)*4,zm=(h-1)*4;
 const rays=[[1,zm/2,-1,zm/2,1],[xm-1,zm/2,xm+1,zm/2,1],[xm/2,1,xm/2,-1,1],[xm/2,zm-1,xm/2,zm+1,1],[1,1,1,-1,6],[xm-1,1,xm-1,-1,6],[1,zm-1,1,zm+1,6],[xm-1,zm-1,xm-1,zm+1,6]];
 assertCase(rays.every(([x,z,X,Z,kind])=>sourceBoxes.some(b=>b.kind===kind&&hit({x,z},{x:X,z:Z},b))),"4 wall mid + 4 platform corner ray source checks");
 for(const r of s.routeOptions){
 const p=r.waypoints;
 assertCase(p.length===r.expected.steps+1&&key(...p[0])===key(...s.sp)&&key(...p.at(-1))===key(...s.finish),"route endpoints/step count "+r.name);
 assertCase(p.every(([x,z],i)=>floorCells.has(key(x,z))&&!blocked.has(key(x,z))&&(i===0||Math.abs(x-p[i-1][0])+Math.abs(z-p[i-1][1])===1)),"4-neighbor unobstructed route "+r.name);
 assertCase(p.slice(1).every(([x,z],j)=>!sourceBoxes.some(b=>hit({x:p[j][0]*4,z:p[j][1]*4},{x:x*4,z:z*4},b,true))),"source AABB centerline noncollision "+r.name);
 const walls=sourceBoxes.filter(b=>b.kind===1||b.kind===6);
 const shadow=p.slice(1,-1).filter(([x,z])=>walls.some(b=>hit({x:s.hypotheticalEnemyWatchpoint[0]*4,z:s.hypotheticalEnemyWatchpoint[1]*4},{x:x*4,z:z*4},b,true))).length;
 assertCase(shadow===r.expected.shadowPoints,"original Wall/Platform source LOS shadow count "+r.name);
 }
 console.log("PASS",s.id,items.length,"items /",s.routeOptions.map(r=>r.name+":"+r.expected.steps).join(","),"source geometry not PhysX");
}
console.log("DATA_SOURCE_ONLY PASS",checks,"checks; ORIGINAL_CSHARP=NOT_RUN; UNITY/PLAYER=NOT_RUN");
