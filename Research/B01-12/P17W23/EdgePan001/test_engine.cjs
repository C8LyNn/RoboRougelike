'use strict';const fs=require('fs'),path=require('path'),crypto=require('crypto'),assert=require('assert/strict');const here=__dirname;
function verify(){let m=JSON.parse(fs.readFileSync(path.join(here,'manifest.json'),'utf8'));for(const [file,pin]of Object.entries(m.files)){let b=fs.readFileSync(path.join(here,file));if(b.length!==pin.bytes||crypto.createHash('sha256').update(b).digest('hex')!==pin.sha256){console.error('FAIL_CLOSED source mismatch: '+file);process.exit(2);}}}
verify();const ep=require('./pan_engine.js');let tests=[];const test=(n,fn)=>{try{fn();tests.push({case:n,status:'PASS'});}catch(e){tests.push({case:n,status:'FAIL',message:e.stack});}};
function run(...events){let s=ep.initial();for(const e of events){let r=ep.apply(s,e);if(!r.ok)throw Error(r.reason);s=r.state;}return s;}
const start=(p=1,x=1,y=1)=>({type:'START',pointerId:p,x,y}),tick=(dt=25)=>({type:'TICK',dt}),move=(p=1,x=2,y=2)=>({type:'MOVE',pointerId:p,x,y});
test('initial valid',()=>assert(ep.valid(ep.initial())));test('initial bounds reject',()=>{for(const [w,h] of [[0,400],[150,400],[160,0],[1441,440],[800,941],[NaN,400]])assert.throws(()=>ep.initial(w,h));});
test('no pan without drag',()=>{let s=run(tick());assert.deepEqual(s.scroll,{x:0,y:0});});
// Illegal scroll during drag must reject without mutating.
test('during drag scroll denied',()=>{let s=run(start());let original=JSON.stringify(s),r=ep.apply(s,{type:'SCROLL',x:100,y:100});assert.equal(r.ok,false);assert.equal(JSON.stringify(s),original);});
test('pan right from middle',()=>{let s=run({type:'SCROLL',x:200,y:100},start(1,719,230),tick(50));assert(s.scroll.x>200);assert.equal(s.scroll.y,100);});
test('pan left from middle',()=>{let s=run({type:'SCROLL',x:200,y:100},start(1,1,230),tick(50));assert(s.scroll.x<200);});
test('pan up',()=>{let s=run({type:'SCROLL',x:200,y:150},start(1,360,1),tick(50));assert(s.scroll.y<150);});
test('pan down',()=>{let s=run({type:'SCROLL',x:200,y:150},start(1,360,459),tick(50));assert(s.scroll.y>150);});
test('center deadzone',()=>{let s=run({type:'SCROLL',x:100,y:100},start(1,360,230),tick(50));assert.deepEqual(s.scroll,{x:100,y:100});});
test('corner diagonal',()=>{let s=run({type:'SCROLL',x:200,y:150},start(1,719,459),tick(50));assert(s.scroll.x>200&&s.scroll.y>150);});
test('clamp left top',()=>{let s=run(start(1,0,0),tick(50));assert.deepEqual(s.scroll,{x:0,y:0});});
test('clamp right bottom',()=>{let s=run({type:'SCROLL',x:720,y:480},start(1,720,460),tick(50));assert.deepEqual(s.scroll,{x:720,y:480});});
test('wrong pointer',()=>{let s=run(start(3));let old=JSON.stringify(s);for(const event of [move(4),{type:'END',pointerId:4}]){let r=ep.apply(s,event);assert.equal(r.ok,false);assert.equal(JSON.stringify(s),old);}});
test('end prevents motion',()=>{let s=run(start(),{type:'END',pointerId:1},tick(50));assert.equal(s.drag.active,false);assert.deepEqual(s.scroll,{x:0,y:0});});
test('cancel safely',()=>{let s=run(start(),{type:'CANCEL'});assert.equal(s.drag.active,false);});
test('disable cancels',()=>{let s=run(start(),{type:'ENABLE',value:false});assert.equal(s.drag.active,false);assert.equal(s.drag.enabled,false);});
test('disabled pan no motion',()=>{let s=run({type:'ENABLE',value:false},start(),tick(50));assert.deepEqual(s.scroll,{x:0,y:0});});
test('enable restores',()=>{let s=run({type:'ENABLE',value:false},{type:'ENABLE',value:true},start());assert.equal(s.drag.active,true);});
test('view cannot change mid-gesture',()=>{let s=run(start());assert.equal(ep.apply(s,{type:'VIEW',width:500,height:400}).ok,false);});
test('view clamps previous scroll',()=>{let s=run({type:'SCROLL',x:720,y:480},{type:'VIEW',width:1000,height:800});assert.equal(s.scroll.x,440);assert.equal(s.scroll.y,140);});
test('RESET state with enabled retained',()=>{let s=run({type:'ENABLE',value:false},{type:'RESET'});assert.equal(s.drag.enabled,false);assert.deepEqual(s.scroll,{x:0,y:0});});
test('monotonic revisions',()=>{let s=run(start(1,10,10),move(1,20,20),tick(0),{type:'END',pointerId:1});assert.equal(s.revision,4);});
test('bounded revision cap',()=>{let s=ep.initial();s.revision=1000000;assert.equal(ep.apply(s,tick(0)).ok,false);});
test('no mutation on rejected event',()=>{let s=run(start());const raw=JSON.stringify(s);let r=ep.apply(s,{type:'TICK',dt:-1});assert.equal(r.ok,false);assert.equal(JSON.stringify(s),raw);});
test('deterministic replay',()=>{let inputs=[{type:'SCROLL',x:200,y:100},start(1,710,400),tick(25),tick(50),{type:'END',pointerId:1}];assert.deepEqual(run(...inputs),run(...inputs));});
for(const [name,event] of [['negative dt',{type:'TICK',dt:-1}],['too large dt',{type:'TICK',dt:51}],['NaN dt',{type:'TICK',dt:NaN}],['string dt',{type:'TICK',dt:'30'}],['missing dt',{type:'TICK'}],['extra field',{type:'TICK',dt:20,evil:1}],['unknown',{type:'POWER'}],['non-object',null],['array',[]],['invalid start',{type:'START',pointerId:1,x:-2,y:2}],['string pointer',{type:'START',pointerId:'1',x:2,y:2}],['extra START',{type:'START',pointerId:1,x:2,y:2,foo:1}],['wrong enabled',{type:'ENABLE',value:0}],['bad view',{type:'VIEW',width:0,height:0}],['invalid scroll',{type:'SCROLL',x:9999,y:0}]])test('reject '+name,()=>assert.equal(ep.apply(ep.initial(),event).ok,false));
for(let i=0;i<22;i++)test('edge profile '+i,()=>{let p=Math.round(i*720/21);let v=ep.edgeVelocity(p,720);assert(Number.isFinite(v));assert(Math.abs(v)<=760);if(p>=72&&p<=648)assert.equal(v,0);});
for(const [name,mutate] of [['missing version',s=>delete s.version],['unknown state key',s=>s.hacked=1],['negative scroll',s=>s.scroll.x=-1],['oversize scroll',s=>s.scroll.y=9999],['bad rev',s=>s.revision=-1],['bad pointer',s=>s.drag.pointerId=-3],['array drag',s=>s.drag=[]],['nonfinite canvas',s=>s.canvas.width=Infinity],['oversize view',s=>s.view.width=2000]])test('reject state '+name,()=>{let s=ep.initial();mutate(s);assert.equal(ep.apply(s,tick()).ok,false);});
const failures=tests.filter(t=>t.status!=='PASS');fs.writeFileSync(path.join(here,'node-results.json'),JSON.stringify({tests,pass:tests.length-failures.length,fail:failures.length,version:process.version},null,2)+'\n');console.log('NODE '+(tests.length-failures.length)+'/'+tests.length+(failures.length?' FAIL':' PASS'));if(failures.length){for(const f of failures)console.error(f.case,f.message);process.exit(1);}
