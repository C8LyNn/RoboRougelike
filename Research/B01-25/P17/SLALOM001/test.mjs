// B01-25-SLALOM-001/R1 — standalone Node 18+ replay of actual original assets.
import assert from "node:assert/strict";
import {createHash} from "node:crypto";
import {readFileSync} from "node:fs";
import {dirname,join} from "node:path";
import {fileURLToPath} from "node:url";
import vm from "node:vm";
import {validateSlalom} from "./validate-core.mjs";

const dir=dirname(fileURLToPath(import.meta.url));
const asset=n=>readFileSync(join(dir,n),"utf8");
const txt=asset("arena.json"),doc=JSON.parse(txt),spec=JSON.parse(asset("manifest.json"));
const blob=createHash("sha1").update("blob "+Buffer.byteLength(txt)+"\0").update(txt).digest("hex");
assert.equal(blob,spec.mapBlob,"Manifest must pin EXACT Git file bytes");
const base=validateSlalom(doc,spec);
assert.ok(base.ok,base.failures.join(", "));
assert.equal(base.checks.length,42);
console.log("PASS / original MapDocument core:",base.checks.length,"checks",base.metrics);
const copy=v=>JSON.parse(JSON.stringify(v));
const tests=[
 ["corner box to thin wall",(d)=>{const x=d.items.find(i=>i.kind===6&&i.x===0&&i.z===0);Object.assign(x,{kind:1,rotation:1,scaleY:1});}],
 ["gap filled by wall",d=>d.items.push({kind:1,x:11,z:4,rotation:0,scaleX:1,scaleY:1,scaleZ:1})],
 ["duplicate spawn",d=>d.items.push({...d.items.find(i=>i.kind===4),x:5,z:2})],
 ["missing floor below spawn",d=>d.items.splice(d.items.findIndex(i=>i.kind===0&&i.x===7&&i.z===2),1)],
 ["deleted physical gate wall",d=>d.items.splice(d.items.findIndex(i=>i.kind===1&&i.x===5&&i.z===7),1)],
 ["globalFloor accidental",d=>{d.globalFloor=true;}],
 ["broken route waypoint",(d,m)=>{m.routes.reference.waypoints[9]=[8,12];}]
];
for(const [label,mutate]of tests){
 const d=copy(doc),m=copy(spec);mutate(d,m);
 const res=validateSlalom(d,m);assert.equal(res.ok,false,"MUTATION ESCAPED: "+label);
 console.log("PASS / negative detected:",label,"->",res.failures[0]);
}
const html=asset("playable-grid-preview.html");
function scriptJson(id) {
 const marker='<script id="'+id+'" type="application/json">';
 const index=html.indexOf(marker);
 assert.ok(index>=0,"Missing embedded original "+id);
 return JSON.parse(html.slice(index+marker.length,html.indexOf("</script>",index)));
}
assert.deepEqual(scriptJson("mapdata"),doc);
assert.deepEqual(scriptJson("manifestdata"),spec);
assert.ok(html.includes('role="img"')&&html.includes('aria-live="polite"'));
const scripts=[...html.matchAll(/<script>([\s\S]*?)<\/script>/g)];
assert.equal(scripts.length,1,"Exactly one executable UI source");
const callbacks={},els={};
function el(id){
 if(els[id])return els[id];
 if(id==="arenaCanvas")return els[id]={width:560,height:620,getContext:()=>ctx};
 return els[id]={textContent:"",innerHTML:"",events:{},setAttribute(){},
  addEventListener(type,fn){this.events[type]=fn;}};
}
const drawMethods=["clearRect","fillRect","beginPath","moveTo","lineTo","save","setLineDash","stroke","restore","arc","fill","fillText"];
const ctx=Object.fromEntries(drawMethods.map(k=>[k,()=>{}]));
const buttons=[["0","1"],["-1","0"],["0","-1"],["1","0"]].map(([dx,dz])=>({
 dataset:{dx,dz},events:{},addEventListener(type,fn){this.events[type]=fn;}
}));
const dom={getElementById(id){
 if(id==="mapdata")return {textContent:JSON.stringify(doc)};
 if(id==="manifestdata")return {textContent:JSON.stringify(spec)};
 return el(id);
},querySelectorAll(sel){assert.equal(sel,"[data-dx]");return buttons;}};
const win={addEventListener(type,fn){callbacks[type]=fn;}};
vm.runInNewContext(scripts[0][1],{document:dom,window:win,console},{timeout:2500});
const ui=win.slalomDebug;
assert.equal(typeof ui?.move,"function");
assert.equal(ui.snapshot().gates,0);
assert.equal(ui.move(0,1),true);
assert.equal(ui.move(0,1),false,"Cannot cross filled wall z4 at x7");
assert.equal(ui.snapshot().blocked,1);
ui.reset();
const waypoints=spec.routes.reference.waypoints;
for(let i=1;i<waypoints.length;i++){
 const [x,z]=waypoints[i],[xx,zz]=waypoints[i-1];
 assert.equal(ui.move(x-xx,z-zz),true,"Move "+i+" rejected");
}
assert.equal(ui.snapshot().finished,true);
assert.equal(ui.snapshot().gates,4);
assert.equal(ui.snapshot().steps,49);
assert.equal(ui.move(1,0),false,"Completed preview must stop movement");
ui.reset();
callbacks.keydown({key:"ArrowRight",preventDefault(){}});
assert.equal(ui.snapshot().position[0],8,"Keyboard input must move actual marker");
els.toggle.events.click.call(els.toggle);
assert.equal(els.toggle.textContent,"Pokaż trasę");
console.log("PASS / original HTML JS executed in isolated DOM+Canvas stubs, 49/49 route moves, four gates, blocked wall, reset, keyboard and toggle.");
console.log("B01-25-SLALOM-001 SOURCE_ONLY PASS (42 positive predicates, 7 negative mutation rejects, 1 complete interactive replay). UNITY/PHYSX/WINDOWS_PLAYER NOT_RUN.");
