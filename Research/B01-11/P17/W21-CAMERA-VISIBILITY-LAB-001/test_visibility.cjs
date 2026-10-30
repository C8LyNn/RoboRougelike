"use strict";
const {test}=require("node:test");
const assert=require("node:assert/strict");
const fs=require("node:fs");
const path=require("node:path");
const crypto=require("node:crypto");
const V=require("./visibility.js");
const camera=()=>({x:0,z:-6,y:2});
const target=()=>({x:0,z:0,y:1});
const wall=()=>({id:"wall",minX:-1,maxX:1,minZ:-3.5,maxZ:-2.5,height:3});
const scene=()=>({camera:camera(),target:target(),radius:0.35,obstacles:[wall()]});
const clear=()=>({...scene(),obstacles:[]});
const no=(reason,s)=>{const r=V.assess(s);assert.equal(r.ok,false);assert.equal(r.reason,reason);assert.equal(r.total,0);};
const visible=(s,count)=>{const r=V.assess(s);assert.equal(r.ok,true);assert.equal(r.visible,count);assert.equal(r.rays.length,5);return r;};
test("core valid baseline",()=>assert.deepEqual(V.validate(scene()),{ok:true,reason:"OK"}));
test("wall blocks all five sample rays",()=>{const r=visible(scene(),0);assert.equal(r.reason,"FULL_OCCLUSION");assert.deepEqual(r.blockers,["wall"]);});
test("clear scene has all rays visible",()=>{const r=visible(clear(),5);assert.equal(r.reason,"CLEAR");assert.equal(r.fraction,1);});
test("fully blocked fraction exactly zero",()=>assert.equal(V.assess(scene()).fraction,0));
test("five target points are distinct",()=>{const p=V.samplePoints(target(),0.35);assert.equal(p.length,5);assert.equal(new Set(p.map(v=>v.x+"/"+v.z)).size,5);});
test("center is first and stable",()=>assert.equal(V.samplePoints(target(),0.35)[0].label,"center"));
test("no global/source side effects on input",()=>{const s=scene(),before=JSON.stringify(s);V.assess(s);V.suggest(s);assert.equal(JSON.stringify(s),before);});
test("obstacle away from the line does not obstruct",()=>visible({...scene(),obstacles:[{...wall(),minX:5,maxX:6}]},5));
test("low obstacle is safely below line",()=>visible({...scene(),obstacles:[{...wall(),height:0.5}]},5));
test("equal-height obstacle blocks on touch",()=>{const a={...wall(),height:1.5};assert.notEqual(V.hitRay(camera(),target(),a),null);});
test("camera altitude above wall clears it",()=>{const s=scene();s.camera.y=9;visible(s,5);});
test("narrow wall occludes fewer than 5 rays",()=>{const s=scene();s.obstacles=[{...wall(),minX:-0.01,maxX:0.01}];const r=V.assess(s);assert.ok(r.visible>0&&r.visible<5);assert.equal(r.reason,"PARTIAL_OCCLUSION");});
test("simple ray crossing returns finite fraction",()=>{const t=V.hitRay(camera(),target(),wall());assert.ok(t>=0&&t<=1);});
test("parallel outside slab returns null",()=>assert.equal(V.hitRay({x:4,z:-6,y:2},{x:4,z:0,y:1},wall()),null));
test("a side-only ray misses the box",()=>assert.equal(V.hitRay({x:-4,z:-6,y:2},{x:-4,z:0,y:1},wall()),null));
test("no 2D projection intersection when obstacle behind camera",()=>assert.equal(V.hitRay(camera(),target(),{...wall(),minZ:-10,maxZ:-9}),null));
test("no intersection when obstacle behind target",()=>assert.equal(V.hitRay(camera(),target(),{...wall(),minZ:1,maxZ:2}),null));
test("camera inside rectangle counts collision",()=>assert.equal(V.hitRay(camera(),target(),{...wall(),minZ:-7,maxZ:-5}),0));
test("target inside rectangle is blocked",()=>assert.notEqual(V.hitRay(camera(),target(),{...wall(),minZ:-0.1,maxZ:0.1}),null));
test("same-depth offset obstacle does not collide",()=>assert.equal(V.hitRay({x:0,z:-6,y:2},{x:0,z:0,y:1},{...wall(),minX:2,maxX:3}),null));
test("overlapping obstacles prefer closer hit",()=>{const s=scene();s.obstacles=[{...wall(),id:"far",minZ:-3,maxZ:-2},{...wall(),id:"near",minZ:-5,maxZ:-4}];assert.equal(V.assess(s).rays[0].by,"near");});
test("overlapping identical obstacles deterministic id tie break",()=>{const s=scene();s.obstacles=[{...wall(),id:"zz"},{...wall(),id:"aa"}];assert.equal(V.assess(s).rays[0].by,"aa");assert.deepEqual(V.assess(s).blockers,["aa"]);});
test("blocked labels are exactly one per ray",()=>assert.deepEqual(V.assess(scene()).rays.map(x=>x.label),["center","right","left","front","back"]));
test("each visible ray has null blocker",()=>assert.ok(V.assess(clear()).rays.every(x=>x.by===null&&!x.blocked)));
test("candidate scorer finds clearer viewpoint for wall",()=>{const s=V.suggest(scene());assert.equal(s.ok,true);assert.equal(s.reason,"IMPROVED");assert.ok(s.candidate.result.visible>s.baseline.visible);});
test("recommended candidate is reproducible",()=>assert.deepEqual(V.suggest(scene()),V.suggest(scene())));
test("clear scene keeps original zero-distance candidate",()=>{const s=V.suggest(clear());assert.equal(s.reason,"ALREADY_CLEAR");assert.equal(s.candidate.label,"original");assert.equal(s.candidate.distance,0);});
test("recommended positions are valid bounded geometry",()=>{const v=V.suggest(scene());assert.equal(V.validate({...scene(),camera:v.candidate.camera}).ok,true);});
test("candidate never worsens baseline visibility",()=>{const b=scene(),r=V.suggest(b);assert.ok(r.candidate.result.visible>=r.baseline.visible);});
test("there is no claim of native Unity in clear copy",()=>assert.match(V.uiCopy(V.assess(clear())).detail,/nie jest pomiar Unity\/PhysX/));
test("blocked copy is critical",()=>assert.equal(V.uiCopy(V.assess(scene())).level,"danger"));
test("partial copy has numeric ratio",()=>{const s=scene();s.obstacles=[{...wall(),minX:-0.01,maxX:0.01}];assert.match(V.uiCopy(V.assess(s)).detail,/[1-4]\/5/);});
test("invalid copy fails closed",()=>assert.equal(V.uiCopy({ok:false}).level,"danger"));
test("null copy fails closed",()=>assert.equal(V.uiCopy(null).level,"danger"));
for(const [name,change] of [
  ["scene null",null],["camera null",{camera:null}],["camera NaN",{camera:{...camera(),x:NaN}}],
  ["camera Infinity",{camera:{...camera(),z:Infinity}}],["camera negative y",{camera:{...camera(),y:-1}}],
  ["camera over height",{camera:{...camera(),y:21}}],["target bad",{target:{...target(),x:101}}],
  ["radius zero",{radius:0}],["radius negative",{radius:-1}],["radius above two",{radius:3}],
  ["radius infinity",{radius:Infinity}],["obstacles not array",{obstacles:{}}],
  ["too many boxes",{obstacles:Array.from({length:33},(_,i)=>({...wall(),id:"box_"+i}))}],
  ["camera equals target",{camera:{x:0,z:0,y:2}}]
])test("scene rejection: "+name,()=>no(name==="camera equals target"?"CAMERA_AT_TARGET":"INVALID_SCENE",change===null?null:{...scene(),...change}));
for(const [name,obstacle] of [
  ["missing ID",{...wall(),id:""}],["path-like ID",{...wall(),id:"../wall"}],
  ["too long ID",{...wall(),id:"w".repeat(41)}],["bad minX",{...wall(),minX:NaN}],
  ["inverted X",{...wall(),minX:2,maxX:1}],["zero X span",{...wall(),maxX:-1}],
  ["inverted Z",{...wall(),minZ:1,maxZ:-3}],["zero Z span",{...wall(),maxZ:-3.5}],
  ["height zero",{...wall(),height:0}],["height negative",{...wall(),height:-1}],
  ["height too high",{...wall(),height:25}],["bad minZ",{...wall(),minZ:Infinity}]
])test("box rejection: "+name,()=>no("INVALID_OBSTACLE",{...scene(),obstacles:[obstacle]}));
test("duplicate obstacle IDs rejected",()=>no("INVALID_OBSTACLE",{...scene(),obstacles:[wall(),{...wall()}]}));
test("32 unique boxes allowed",()=>{const s=scene();s.obstacles=Array.from({length:32},(_,i)=>({...wall(),id:"box_"+i}));assert.equal(V.validate(s).ok,true);});
test("invalid scene has no suggested camera",()=>{const r=V.suggest({...scene(),radius:0});assert.equal(r.ok,false);assert.equal(r.candidate,null);});
test("height raise beyond supported 20 is discarded in scoring",()=>{const s=scene();s.camera.y=19;const r=V.suggest(s);assert.equal(r.ok,true);assert.ok(r.candidate.camera.y<=20);});
test("stress 120 deterministic shifted worlds",()=>{for(let i=0;i<120;i++){const s=scene(),dx=(i%15)-7;s.camera.x=dx;s.camera.z=-6-i%4;s.obstacles[0].minX=-0.5;s.obstacles[0].maxX=0.5;const r=V.assess(s);assert.equal(r.ok,true);assert.equal(r.total,5);assert.equal(r.visible+r.rays.filter(x=>x.blocked).length,5);const a=V.suggest(s);assert.equal(a.ok,true);assert.ok(a.candidate.result.visible>=r.visible);}});
test("stress 100 lowered walls never block any rays",()=>{for(let i=0;i<100;i++){const s=scene();s.obstacles[0].height=0.01+i*0.003;assert.equal(V.assess(s).visible,5);}});
test("HTML has authentic active camera controls, accessible status, no Unity claims",()=>{const html=fs.readFileSync(path.join(__dirname,"index.html"),"utf8");for(const word of ['lang="pl"','aria-live="polite"','id="diagram"','id="clear"','id="wall"','id="corridor"','id="elevated"','id="suggest"','visibility.js'])assert.ok(html.includes(word),word);assert.match(html,/nie uruchamia Unity/i);});
test("workflow source test has Windows and Ubuntu",()=>{const yaml=fs.readFileSync(path.join(__dirname,"../../../../.github/workflows/b01-11-p17w21-camera-visibility-lab-001.yml"),"utf8");assert.match(yaml,/ubuntu-24\.04/);assert.match(yaml,/windows-2022/);assert.match(yaml,/node --test .*test_visibility\.cjs/);});
test("manifest verifies original source and CI bytes",()=>{const manifest=JSON.parse(fs.readFileSync(path.join(__dirname,"manifest.json"),"utf8"));assert.equal(manifest.package,"B01-11-CAMERA-VISIBILITY-LAB-001/R1");assert.equal(manifest.files.length,5);for(const f of manifest.files){assert.ok(!f.path.includes(".."));const bytes=fs.readFileSync(path.resolve(__dirname,"../../../../",f.path));assert.equal(f.bytes,bytes.length,f.path);assert.equal(f.sha256,crypto.createHash("sha256").update(bytes).digest("hex"),f.path);}});
