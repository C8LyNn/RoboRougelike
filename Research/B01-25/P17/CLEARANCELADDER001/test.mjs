// B01-25-CLEARANCELADDER-001 original standalone regression, Node 18+.
import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import { join,dirname } from "node:path";
import { fileURLToPath } from "node:url";
import vm from "node:vm";
import { analyzeLadder } from "./clearance-core.mjs";
const base=dirname(fileURLToPath(import.meta.url)),content=n=>readFileSync(join(base,n),"utf8");
const file=content("arena.json"),doc=JSON.parse(file),manifest=JSON.parse(content("manifest.json"));
const sha=createHash("sha1").update("blob "+Buffer.byteLength(file)+"\0").update(file).digest("hex");
assert.equal(sha,manifest.mapGitBlob,"Manifest must pin original exact arena Git bytes");
const original=analyzeLadder(doc,manifest);
assert.ok(original.ok,original.failures.join("|"));
assert.equal(original.checks.length,45);
assert.deepEqual(original.stats.measurements.map(g=>g.sourceOpeningMeters),[28,20,16,12]);
assert.equal(original.stats.minSourceClearanceMeters,6);
console.log("PASS original 45/45 / exact MapDocument v2 + source MapWorld XZ proxy",sha);
const clone=x=>JSON.parse(JSON.stringify(x));
const negative=[
 ["corner gap",d=>{let v=d.items.find(i=>i.kind===6&&i.x===0&&i.z===0);Object.assign(v,{kind:1,rotation:1,scaleY:1});}],
 ["closed gate",d=>d.items.push({kind:1,x:9,z:12,rotation:0,scaleX:1,scaleY:1,scaleZ:1})],
 ["removed gate wall",d=>d.items.splice(d.items.findIndex(i=>i.kind===1&&i.x===7&&i.z===12),1)],
 ["duplicate finish",d=>d.items.push({...d.items.find(i=>i.kind===5),x:8,z:13})],
 ["missing floor below spawn",d=>d.items.splice(d.items.findIndex(i=>i.kind===0&&i.x===9&&i.z===2),1)],
 ["global floor leak",d=>{d.globalFloor=true;}],
 ["obstacle inside lane",d=>d.items.push({kind:7,x:9,z:8,rotation:0,scaleX:1,scaleY:1,scaleZ:1})],
 ["wrong aperture claim",(d,m)=>{m.gates[2].physicalOpeningMeters=20;}],
 ["broken trajectory",(d,m)=>{m.referencePath[4]=[5,4];}]
];
for(const [label,mutate]of negative){const d=clone(doc),m=clone(manifest);mutate(d,m);const result=analyzeLadder(d,m);assert.equal(result.ok,false,"MUTATION ESCAPED: "+label);console.log("PASS negative rejected:",label);}
const html=content("clearance-preview.html");
const embedded=id=>{const start='<script id="'+id+'" type="application/json">',i=html.indexOf(start);assert.ok(i>=0);return JSON.parse(html.slice(i+start.length,html.indexOf("</script>",i)));};
assert.deepEqual(embedded("mapData"),doc);assert.deepEqual(embedded("manifestData"),manifest);
const scripts=[...html.matchAll(/<script>([\s\S]*?)<\/script>/g)];
assert.equal(scripts.length,1,"Single executable original preview script");
const dom={};const ctx=Object.fromEntries(["clearRect","fillRect","beginPath","arc","fill","stroke","moveTo","lineTo","fillText"].map(k=>[k,()=>{}]));
function element(id){
 if(!dom[id])dom[id]=id==="map"?{getContext:()=>ctx,width:580,height:560}:{value:"2.5",innerHTML:"",textContent:"",className:"",handlers:{},addEventListener(k,f){this.handlers[k]=f;}};
 return dom[id];
}
const document={getElementById(id){if(id==="mapData")return{textContent:JSON.stringify(doc)};if(id==="manifestData")return{textContent:JSON.stringify(manifest)};return element(id);}};
const window={};
vm.runInNewContext(scripts[0][1],{document,window,console},{timeout:2500});
const api=window.ladderDebug;
assert.equal(api.getRadius(),2.5);
assert.equal(api.evaluate(2.5).pass,true);
assert.equal(api.setRadius(5.5).pass,true);
const rejected=api.setRadius(6.5);
assert.equal(rejected.pass,false);
assert.deepEqual(Array.from(rejected.gates.filter(g=>!g.pass).map(g=>g.id)),["L3","L4"]);
element("reset").handlers.click();
assert.equal(api.getRadius(),2.5);
element("radius").value="6.5";element("radius").handlers.input();
assert.equal(api.getRadius(),6.5);
assert.match(element("overall").textContent,/BRAK PRZEŚWITU/);
console.log("PASS original HTML inline-JS executed in Node VM (Canvas/DOM stubs): slider, 2.5, 5.5 PASS, 6.5 RED L3/L4, reset; no browser execution");
console.log("B01-25-CLEARANCELADDER-001: 45 positive +9 negative mutation rejects + original HTML source replay. NATIVE_UNITY/PHYSX/PLAYER NOT_RUN");
