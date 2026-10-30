'use strict';
const fs=require('fs'),path=require('path'),crypto=require('crypto'),assert=require('node:assert/strict');
const dir=__dirname,manifest=JSON.parse(fs.readFileSync(path.join(dir,'manifest.json'),'utf8'));
for(const [file,pin] of Object.entries(manifest.pins)){
 const data=fs.readFileSync(path.join(dir,file));
 if(data.length!==pin.bytes||crypto.createHash('sha256').update(data).digest('hex')!==pin.sha256){console.error('FAIL_CLOSED',file);process.exit(2);}
}
const J=require('./journal_engine.js');const out=[];
function check(name,fn){try{fn();out.push({name,pass:true});}catch(e){out.push({name,pass:false,error:e.stack});}}
const ev=(type,id=null,receipt=null)=>({type,id,receipt});
function go(s,type,id=null,r=null){const result=J.transition(s,ev(type,id,r));assert.equal(result.ok,true,result.reason);return result.state;}
check('initial valid',()=>assert(J.valid(J.initial())));
check('task count',()=>assert.equal(J.TASKS.length,5));
check('initial empty',()=>assert.deepEqual(J.initial().done,[]));
check('strict initial schema',()=>assert(!J.valid({...J.initial(),extra:2})));
check('initial only root available',()=>assert.deepEqual(J.view(J.initial()).filter(t=>t.status==='available').map(t=>t.id),['reach_gate']));
let s=J.initial();const states=[s];
for(let i=0;i<J.TASKS.length;i++){
 const t=J.TASKS[i];check('valid ready '+t.id,()=>assert(J.available(s,t.id)));
 s=go(s,'COMPLETE',t.id,'receipt_'+i);states.push(s);
 check('completed '+t.id,()=>assert.equal(s.done[i],t.id));
 check('receipt '+t.id,()=>assert.equal(s.receipts[i],'receipt_'+i));
 check('replay rejected '+t.id,()=>assert.equal(J.transition(s,ev('COMPLETE',t.id,'receipt_'+i)).reason,'DUPLICATE_RECEIPT'));
 check('roundtrip '+t.id,()=>assert.deepEqual(J.importState(J.exportState(s)).state,s));
}
check('completed all',()=>assert.equal(J.view(s).filter(t=>t.status==='done').length,5));
check('done count',()=>assert.equal(s.done.length,5));
check('full reset',()=>assert.deepEqual(go(s,'RESET'),J.initial()));
for(const t of J.TASKS){
 check('pin '+t.id,()=>{const x=go(J.initial(),'PIN',t.id);assert.equal(x.pin,t.id);});
 check('unpin '+t.id,()=>{let x=go(J.initial(),'PIN',t.id);x=go(x,'PIN',t.id);assert.equal(x.pin,null);});
 check('pin no mutation '+t.id,()=>{const x=J.initial();go(x,'PIN',t.id);assert.equal(x.pin,null);});
}
for(const t of J.TASKS.slice(1))check('dep gate '+t.id,()=>{const x=J.transition(J.initial(),ev('COMPLETE',t.id,'good'));assert.equal(x.reason,'LOCKED');assert.deepEqual(x.state,J.initial());});
for(const bad of [null,[],{},0,'','x'.repeat(33),'<script>','with space','1/a'])check('receipt rejects '+String(bad),()=>{const r=J.transition(J.initial(),ev('COMPLETE','reach_gate',bad));assert.equal(r.ok,false);});
for(const bad of [null,0,'malicious',{},[]])check('invalid objective '+String(bad),()=>{const r=J.transition(J.initial(),ev('COMPLETE',bad,'r'));assert.equal(r.ok,false);});
for(const bad of [null,{},[],17,'garbage','x'.repeat(5000),'{',JSON.stringify({...J.initial(),version:2}),JSON.stringify({...J.initial(),extra:true}),JSON.stringify({...J.initial(),pin:'unknown'}),JSON.stringify({...J.initial(),revision:-1})])check('invalid import '+String(bad).slice(0,24),()=>assert.equal(J.importState(bad).ok,false));
for(const bad of [
 {...J.initial(),done:['defeat_guard'],receipts:['r'],revision:1},
 {...J.initial(),done:['reach_gate','reach_gate'],receipts:['r1','r2'],revision:2},
 {...J.initial(),done:['reach_gate','disable_turrets'],receipts:['r1','r1'],revision:2},
 {...J.initial(),done:['reach_gate'],receipts:[],revision:1},
 {...J.initial(),done:['reach_gate'],receipts:['r'],revision:0},
 {...J.initial(),done:['unknown'],receipts:['r'],revision:1}
])check('bad state rejected '+JSON.stringify(bad),()=>{assert.equal(J.valid(bad),false);assert.equal(J.importState(JSON.stringify(bad)).ok,false);});
check('unknown event rejects',()=>assert.equal(J.transition(J.initial(),ev('DELETE','reach_gate')).reason,'UNKNOWN_EVENT'));
check('extra event field rejects',()=>assert.equal(J.transition(J.initial(),{...ev('RESET'),force:true}).reason,'INVALID_EVENT'));
check('strict reset',()=>assert.equal(J.transition(J.initial(),ev('RESET','reach_gate')).reason,'INVALID_RESET'));
check('strict pin',()=>assert.equal(J.transition(J.initial(),ev('PIN','reach_gate','r')).reason,'INVALID_PIN'));
check('second completion cannot mutate source',()=>{let x=J.initial(),raw=JSON.stringify(x);go(x,'COMPLETE','reach_gate','one');assert.equal(JSON.stringify(x),raw);});
check('snapshot detached',()=>{let x=go(J.initial(),'COMPLETE','reach_gate','one');let y=J.importState(J.exportState(x)).state;y.done.push('evil');assert.equal(x.done.length,1);});
check('html injection rejects bad receipt',()=>assert.equal(J.transition(J.initial(),ev('COMPLETE','reach_gate','<img onerror=alert(1)>')).ok,false));
const pass=out.filter(c=>c.pass).length;fs.writeFileSync(path.join(dir,'node-results.json'),JSON.stringify({total:out.length,passed:pass,failed:out.length-pass,cases:out},null,2)+'\n');
console.log(`NODE ${pass}/${out.length} ${pass===out.length?'PASS':'FAIL'}`);for(const f of out.filter(c=>!c.pass))console.error('FAIL',f.name,f.error);if(pass!==out.length)process.exit(1);
