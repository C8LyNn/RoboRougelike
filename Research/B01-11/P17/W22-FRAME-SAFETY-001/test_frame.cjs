'use strict';
const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const crypto=require('node:crypto');
const V=require('./frame.js');
const box=(id,kind,x,y,z,dx=1,dy=1,dz=1,active=true)=>({id,kind,center:{x,y,z},size:{x:dx,y:dy,z:dz},active});
const base=()=>({camera:{x:0,y:3,z:-10},aim:{x:0,y:1,z:0},verticalFov:55,aspect:16/9,
  safe:{left:.12,right:.82,top:.12,bottom:.84},parts:[
    box('chassis','core',0,1,0,1.7,1.5,2),
    box('weaponL','left-weapon',-2.4,1.4,0,.9,.7,1.3),
    box('weaponR','right-weapon',2.4,1.4,0,.9,.7,1.3),
    box('tail','module',0,1,3,1.2,1.2,1.2)]});
const invalid=(reason,s)=>{const r=V.project(s);assert.equal(r.ok,false);assert.equal(r.reason,reason);assert.equal(r.pieces.length,0);};
const badPart=(change)=>({...base(),parts:[{...base().parts[0],...change}]});
const safe=(s)=>{const r=V.project(s);assert.equal(r.ok,true);assert.equal(r.allFramed,true);return r;};
test('valid canonical camera sample',()=>assert.deepEqual(V.validate(base()),{ok:true,reason:'OK'}));
test('four active module samples',()=>{const r=V.project(base());assert.equal(r.total,4);assert.deepEqual(r.pieces.map(p=>p.id),['chassis','weaponL','weaponR','tail']);});
test('detached weapon hidden from frame result',()=>{const s=base();s.parts[2].active=false;assert.equal(V.project(s).total,3);});
test('all weapon-disabled variant still shows chassis and tail',()=>{const s=base();s.parts[1].active=false;s.parts[2].active=false;assert.equal(V.project(s).total,2);});
test('module bounding box yields eight corners',()=>assert.equal(V.corners(base().parts[0]).length,8));
test('all corner coordinates are finite',()=>assert.ok(V.corners(base().parts[0]).every(p=>[p.x,p.y,p.z].every(Number.isFinite))));
test('camera and scene cannot be mutated during projection',()=>{const s=base(),copy=JSON.stringify(s);V.project(s);V.suggest(s);assert.equal(JSON.stringify(s),copy);});
test('centered core fits without weapons',()=>{const s=base();s.parts=s.parts.slice(0,1);safe(s);});
test('wide weapon sticks outside right safe zone',()=>{const s=base();s.parts[2].center.x=7;const r=V.project(s);assert.equal(r.allFramed,false);assert.equal(r.pieces[2].fit,false);});
test('dolly suggestion improves far right part',()=>{const s=base();s.parts[2].center.x=7;const r=V.suggest(s);assert.equal(r.ok,true);assert.equal(r.reason,'SUGGESTED_DOLLY');assert.ok(r.candidate.scale>1);assert.equal(r.candidate.result.allFramed,true);});
test('dolly keeps the world-space aim fixed',()=>{const s=base();const r=V.dolly(s,2);assert.deepEqual(r.aim,s.aim);assert.deepEqual(r.camera,{x:0,y:5,z:-20});});
test('dolly factor 1 is geometrically unchanged',()=>assert.deepEqual(V.dolly(base(),1),base()));
test('invalid negative dolly refused',()=>assert.equal(V.dolly(base(),-1),null));
test('invalid infinite dolly refused',()=>assert.equal(V.dolly(base(),Infinity),null));
test('dolly above bounded factor refused',()=>assert.equal(V.dolly(base(),7),null));
test('projection result has clamped finite u/v for fitted core',()=>{const s=base();s.parts=s.parts.slice(0,1);const r=safe(s).pieces[0].rect;assert.ok(r.minU>=0&&r.maxU<=1&&r.minV>=0&&r.maxV<=1);});
test('near-plane clipping is fail-closed',()=>{const s=base();s.camera={x:0,y:1,z:0.1};s.aim={x:0,y:1,z:1};const r=V.project(s);assert.equal(r.ok,true);assert.ok(r.clipped>0);assert.equal(r.allFramed,false);});
test('focal vertical perspective accepts overhead camera',()=>{const s=base();s.camera={x:0,y:10,z:0};s.aim={x:0,y:1,z:0};const r=V.project(s);assert.equal(r.ok,true);assert.equal(r.total,4);});
test('core missing not accepted',()=>invalid('CORE_MISSING_OR_DUPLICATE',{...base(),parts:base().parts.slice(1)}));
test('core inactive not accepted',()=>{const s=base();s.parts[0].active=false;invalid('CORE_MISSING_OR_DUPLICATE',s);});
test('duplicate live cores not accepted',()=>{const s=base();s.parts.push(box('core2','core',1,1,1));invalid('CORE_MISSING_OR_DUPLICATE',s);});
test('static axis-aligned part boxes still test all 8 corners',()=>{const v=V.corners(box('a','module',0,0,0,2,2,2));assert.deepEqual(new Set(v.map(p=>p.x)).size,2);assert.deepEqual(new Set(v.map(p=>p.y)).size,2);assert.deepEqual(new Set(v.map(p=>p.z)).size,2);});
test('safe rect contains critical core on default scene',()=>assert.equal(V.project(base()).pieces[0].fit,true));
test('framing counts stable after two calls',()=>assert.deepEqual(V.project(base()),V.project(base())));
test('dolly search deterministic',()=>assert.deepEqual(V.suggest(base()),V.suggest(base())));
test('all fit baseline will stay at factor 1',()=>{const s=base();s.parts=s.parts.slice(0,1);const r=V.suggest(s);assert.equal(r.reason,'ALREADY_FIT');assert.equal(r.candidate.scale,1);});
test('off-center safe frame might have no valid dolly',()=>{const s=base();s.safe={left:.6,right:.95,top:.12,bottom:.84};const r=V.suggest(s);assert.equal(r.ok,false);assert.equal(r.reason,'NO_SAFE_DOLLY');});
test('advice never claims Unity verification',()=>{const s=base();s.parts=s.parts.slice(0,1);assert.match(V.message(V.project(s)).detail,/nie jest to test Unity/);});
test('invalid projection leads to warning',()=>assert.equal(V.message(null).level,'danger'));
test('status message distinguishes outside-safe',()=>{const s=base();s.parts[2].center.x=7;assert.equal(V.message(V.project(s)).level,'warning');});
test('status message distinguishes near clipping',()=>{const s=base();s.camera={x:0,y:1,z:0.1};s.aim={x:0,y:1,z:1};assert.equal(V.message(V.project(s)).level,'danger');});
for(const [name,change,reason] of [
  ['empty',null,'INVALID_SCENE'],['missing camera',{camera:null},'INVALID_SCENE'],
  ['camera infinite',{camera:{x:Infinity,y:1,z:1}},'INVALID_SCENE'],
  ['camera NaN',{camera:{x:NaN,y:1,z:1}},'INVALID_SCENE'],
  ['missing aim',{aim:null},'INVALID_SCENE'],['negative aspect',{aspect:-1},'INVALID_SCENE'],
  ['high aspect',{aspect:4},'INVALID_SCENE'],['narrow FOV',{verticalFov:20},'INVALID_SCENE'],
  ['wide FOV',{verticalFov:120},'INVALID_SCENE'],['NaN FOV',{verticalFov:NaN},'INVALID_SCENE'],
  ['missing safe',{safe:null},'INVALID_SCENE'],['reversed horizontal safe',{safe:{left:.9,right:.2,top:.1,bottom:.9}},'INVALID_SCENE'],
  ['reversed vertical safe',{safe:{left:.1,right:.9,top:.9,bottom:.1}},'INVALID_SCENE'],
  ['negative safe',{safe:{left:-.1,right:.9,top:.1,bottom:.9}},'INVALID_SCENE'],
  ['safe above 1',{safe:{left:.1,right:1.1,top:.1,bottom:.9}},'INVALID_SCENE'],
  ['missing parts',{parts:null},'INVALID_SCENE'],['no parts',{parts:[]},'INVALID_SCENE'],
  ['too many parts',{parts:Array.from({length:31},(_,i)=>box('part'+i,i===0?'core':'module',0,0,0))},'INVALID_SCENE'],
  ['camera at aim',{camera:{x:0,y:1,z:0},aim:{x:0,y:1,z:0}},'CAMERA_AT_AIM']
])test('scene rejects '+name,()=>invalid(reason,change===null?null:{...base(),...change}));
for(const [name,change] of [
  ['invalid id',{id:'../../bad'}],['id too long',{id:'i'.repeat(41)}],
  ['id empty',{id:''}],['bad kind',{kind:'enemy'}],['missing active',{active:undefined}],
  ['active string',{active:'true'}],['center null',{center:null}],
  ['center infinity',{center:{x:Infinity,y:0,z:0}}],
  ['size negative',{size:{x:-1,y:1,z:1}}],['size zero',{size:{x:0,y:1,z:1}}],
  ['size too big',{size:{x:9,y:1,z:1}}],['size NaN',{size:{x:NaN,y:1,z:1}}]
])test('part rejects '+name,()=>invalid('INVALID_PART',badPart(change)));
test('duplicate part ids fail',()=>{const s=base();s.parts[1].id='chassis';invalid('INVALID_PART',s);});
test('all 30 valid parts are permitted',()=>{const s=base();s.parts=[box('core','core',0,1,0),...Array.from({length:29},(_,i)=>box('m'+i,'module',i/10,1,0))];assert.equal(V.validate(s).ok,true);});
test('invalid cannot auto frame',()=>{const s=base();s.aspect=100;const r=V.suggest(s);assert.equal(r.ok,false);assert.equal(r.candidate,null);});
test('stress 200 camera distances without accidental mutation',()=>{for(let i=0;i<200;i++){const s=base();s.camera.z=-4-i/20;const before=JSON.stringify(s);const r=V.project(s);assert.equal(r.ok,true);assert.equal(r.total,4);assert.equal(JSON.stringify(s),before);assert.equal(r.pieces.length,4);}});
test('stress 200 detached-part combinations retain one core',()=>{for(let i=0;i<200;i++){const s=base();s.parts[1].active=!!(i&1);s.parts[2].active=!!(i&2);s.parts[3].active=!!(i&4);const r=V.project(s);assert.equal(r.ok,true);assert.equal(r.total,1+Number(!!(i&1))+Number(!!(i&2))+Number(!!(i&4)));}});
test('original browser UI has explicit synthetic label, controls and source',()=>{
 const html=fs.readFileSync(path.join(__dirname,'index.html'),'utf8');
 for(const v of ['lang="pl"','aria-live="polite"','id="frame"','id="wide"','id="portrait"','id="weapon-left"','id="weapon-right"','id="auto"','frame.js'])assert.ok(html.includes(v),v);
 assert.match(html,/nie (uruchamia|wykonuje) Unity/i);
});
test('workflow checks source on Windows and Ubuntu',()=>{
 const y=fs.readFileSync(path.join(__dirname,'../../../../.github/workflows/b01-11-p17w22-frame-safety-001.yml'),'utf8');
 assert.match(y,/ubuntu-24\.04/);assert.match(y,/windows-2022/);assert.match(y,/node --test .*test_frame\.cjs/);
});
test('manifest verifies five actual text sources exactly',()=>{
 const m=JSON.parse(fs.readFileSync(path.join(__dirname,'manifest.json'),'utf8'));
 assert.equal(m.package,'B01-11-FRAME-SAFETY-001/R1');assert.equal(m.files.length,5);
 for(const f of m.files){assert.ok(!f.path.includes('..'));const bytes=fs.readFileSync(path.resolve(__dirname,'../../../../',f.path));assert.equal(bytes.length,f.bytes,f.path);assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),f.sha256,f.path);}
});
