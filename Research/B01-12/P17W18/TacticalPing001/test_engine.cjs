'use strict';
const fs=require('node:fs'),crypto=require('node:crypto'),path=require('node:path'),assert=require('node:assert/strict');
const here=__dirname;
function gate(){const man=JSON.parse(fs.readFileSync(path.join(here,'manifest.json'),'utf8'));
  if(man.package!=='B01-12-TACTICAL-PING-001/R1'||!man.source||Object.keys(man.source).length!==5)throw Error('FAIL_CLOSED manifest');
  for(const [name,exp] of Object.entries(man.source)){
    if(!['demo.html','ping_engine.js','ui.js','test_engine.cjs','test_browser.py'].includes(name))throw Error('FAIL_CLOSED unknown file');
    const buf=fs.readFileSync(path.join(here,name));const hash=crypto.createHash('sha256').update(buf).digest('hex');
    if(buf.length!==exp.bytes||hash!==exp.sha256)throw Error('FAIL_CLOSED source mismatch: '+name);
  }
}
try{gate();}catch(e){console.error(e.message);process.exit(2);}
const P=require('./ping_engine.js');const cases=[];function check(label,fn){try{fn();cases.push({name:label,result:'PASS'});}catch(e){cases.push({name:label,result:'FAIL',error:e.message});}}
function add(s,id='P1',type='attack',x=500,y=500,priority=2){return P.step(s,{op:'add',id,type,point:{x,y},priority});}
function ok(x){assert.equal(x.ok,true);return x.state;}
function no(x,reason){assert.equal(x.ok,false);if(reason)assert.equal(x.reason,reason);}
check('initial valid and empty',()=>{const s=P.initial();assert(P.valid(s));assert.equal(s.markers.length,0);});
check('invalid extra state key',()=>assert(!P.valid({...P.initial(),hack:1})));
check('empty accepted tick',()=>assert.equal(ok(P.step(P.initial(),{op:'tick',time:0})).clock,0));
check('deterministic export',()=>assert.equal(P.exportState(P.initial()),P.exportState(P.initial())));
check('unknown op reject',()=>no(P.step(P.initial(),{op:'hack'}),'UNKNOWN_OP'));
check('event null reject',()=>no(P.step(P.initial(),null),'INVALID_EVENT'));
check('reset exact schema',()=>assert.deepEqual(ok(P.step(P.initial(),{op:'reset'})),P.initial()));
check('extra reset key reject',()=>no(P.step(P.initial(),{op:'reset',bad:1}),'INVALID_EVENT'));
check('valid rank empty',()=>assert.deepEqual(P.rank(P.initial()),[]));
check('invalid rank empty',()=>assert.deepEqual(P.rank({}),[]));
for(const type of P.TYPES){
  check('add '+type,()=>{const s=ok(add(P.initial(),'P1',type));assert.equal(s.markers[0].type,type);assert.equal(s.markers[0].expires,P.TTL[type]);});
  check('ttl expiry '+type,()=>{const s=ok(add(P.initial(),'P1',type));let a=ok(P.step(s,{op:'tick',time:P.TTL[type]-1}));assert.equal(a.markers.length,1);a=ok(P.step(a,{op:'tick',time:P.TTL[type]}));assert.equal(a.markers.length,0);});
  check('priority preserved '+type,()=>assert.equal(ok(add(P.initial(),'P1',type,17,99,3)).markers[0].priority,3));
}
for(const id of ['', 'lower','P-1','a','<script>','1234567890123456789',123,null,'P.3']){
  check('reject invalid ID '+String(id),()=>no(add(P.initial(),id),'INVALID_MARKER'));
}
for(const n of [-1,1000,1.2,NaN,Infinity,null]){
  check('reject x '+String(n),()=>no(add(P.initial(),'P1','move',n,400),'INVALID_MARKER'));
  check('reject y '+String(n),()=>no(add(P.initial(),'P1','move',400,n),'INVALID_MARKER'));
}
for(const p of [0,4,-1,1.1,'2',null])check('reject priority '+String(p),()=>no(add(P.initial(),'P1','attack',500,500,p),'INVALID_MARKER'));
for(const t of [-1,1.2,NaN,Infinity,1000000001,'3'])check('reject time '+String(t),()=>no(P.step(P.initial(),{op:'tick',time:t}),'CLOCK_REWIND'));
check('clock rewind',()=>{const s=ok(P.step(P.initial(),{op:'tick',time:100}));no(P.step(s,{op:'tick',time:99}),'CLOCK_REWIND');});
check('duplicate ID reject',()=>{const s=ok(add(P.initial()));no(add(s),'DUPLICATE_ID');});
check('reject no select ID',()=>no(P.step(P.initial(),{op:'select',id:'P1'}),'UNKNOWN_ID'));
check('select null',()=>{const s=ok(add(P.initial()));assert.equal(ok(P.step(s,{op:'select',id:null})).selected,null);});
check('remove selected clears',()=>{const s=ok(add(P.initial()));const t=ok(P.step(s,{op:'remove',id:'P1'}));assert.equal(t.selected,null);assert.equal(t.markers.length,0);});
check('unknown remove reject',()=>no(P.step(P.initial(),{op:'remove',id:'P1'}),'UNKNOWN_ID'));
check('strict remove keys',()=>no(P.step(P.initial(),{op:'remove',id:'P1',x:1}),'INVALID_ID'));
check('strict select keys',()=>no(P.step(P.initial(),{op:'select',id:null,x:1}),'INVALID_SELECTION'));
check('strict tick keys',()=>no(P.step(P.initial(),{op:'tick',time:4,x:1}),'CLOCK_REWIND'));
check('rank priority',()=>{let s=P.initial();s=ok(add(s,'P1','move',1,1,1));s=ok(add(s,'P2','defend',1,1,3));s=ok(add(s,'P3','repair',1,1,2));assert.deepEqual(P.rank(s).map(m=>m.id),['P2','P3','P1']);});
check('cap exactly six',()=>{let s=P.initial();for(let i=1;i<=6;i++)s=ok(add(s,'P'+i));assert.equal(s.markers.length,6);no(add(s,'P7'),'CAPACITY');});
check('no partial mutation rejected cap',()=>{let s=P.initial();for(let i=1;i<=6;i++)s=ok(add(s,'P'+i));const before=JSON.stringify(s);no(add(s,'P7'));assert.equal(JSON.stringify(s),before);});
check('rev serial six',()=>{let s=P.initial();for(let i=1;i<=6;i++)s=ok(add(s,'P'+i));assert.equal(s.serial,6);});
check('expired selected resets',()=>{const s=ok(add(P.initial()));const t=ok(P.step(s,{op:'tick',time:6000}));assert.equal(t.selected,null);});
check('restart reset',()=>{const s=ok(add(P.initial()));assert.deepEqual(ok(P.step(s,{op:'reset'})),P.initial());});
check('round trip import/export',()=>{const s=ok(add(P.initial()));const r=P.importState(P.exportState(s));assert.equal(r.ok,true);assert.deepEqual(r.state,s);});
check('import invalid JSON',()=>no(P.importState('{'),'INVALID_JSON'));
check('import oversized',()=>no(P.importState('x'.repeat(5001)),'INVALID_JSON'));
check('import unknown key',()=>no(P.importState(JSON.stringify({...P.initial(),bad:true})),'INVALID_SNAPSHOT'));
check('import duplicate marker ID',()=>{const s=ok(add(P.initial()));const t={...s,markers:[s.markers[0],s.markers[0]]};no(P.importState(JSON.stringify(t)),'INVALID_SNAPSHOT');});
check('import stale expired marker',()=>{const s=ok(add(P.initial()));const t={...s,clock:6000};no(P.importState(JSON.stringify(t)),'INVALID_SNAPSHOT');});
check('import nonexistent selection',()=>{const s=ok(add(P.initial()));no(P.importState(JSON.stringify({...s,selected:'NO'})),'INVALID_SNAPSHOT');});
check('import malicious extra proto key',()=>no(P.importState('{"version":1,"clock":0,"serial":0,"selected":null,"markers":[],"__proto__":{"polluted":true}}'),'INVALID_SNAPSHOT'));
check('state cannot be array',()=>no(P.step([], {op:'reset'}),'INVALID_STATE'));
check('validate no side effects',()=>{const s=P.initial(),before=JSON.stringify(s);no(add(s,'evil'));assert.equal(JSON.stringify(s),before);});
check('input coords cloned',()=>{const original={x:500,y:500};const s=ok(P.step(P.initial(),{op:'add',id:'P1',type:'move',point:original,priority:1}));original.x=3;assert.equal(s.markers[0].point.x,500);});
check('strict event object keys',()=>no(P.step(P.initial(),{op:'add',id:'P1',type:'move',point:{x:1,y:1},priority:1,nasty:1}),'INVALID_MARKER'));
check('future serial cap',()=>{const s={...P.initial(),serial:1000000};no(add(s,'P1'),'OVERFLOW');});
check('clock at max expires overflow',()=>{const s={...P.initial(),clock:1000000000};no(add(s,'P1'),'OVERFLOW');});
const result={package:'B01-12-TACTICAL-PING-001/R1',runtime:process.version,cases,total:cases.length,pass:cases.filter(c=>c.result==='PASS').length,fail:cases.filter(c=>c.result==='FAIL').length};
console.log(`NODE ${result.pass}/${result.total} ${result.fail?'FAIL':'PASS'}`);for(const c of cases.filter(c=>c.result==='FAIL'))console.error(c.name,c.error);
if(process.env.WRITE_RECEIPT==='1')fs.writeFileSync(path.join(here,'node-results.json'),JSON.stringify(result,null,2)+'\n');
if(result.fail)process.exit(1);
