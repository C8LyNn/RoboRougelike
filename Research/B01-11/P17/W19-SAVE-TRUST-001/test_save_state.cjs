'use strict';
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const UX = require('./save_state.js');
const digest = 'f'.repeat(64);
function state() { return UX.initialState(9, 'test_slot'); }
function e(kind, seq, extra={}) { return {kind, seq, runId:9, slot:'test_slot', ...extra}; }
function fire(s,ev) { const r=UX.transition(s,ev); assert.equal(r.accepted,true,r.reason); return r.state; }
function begin() {return fire(state(),e('BEGIN',1,{operationId:'op_1'}));}
const proof = generation => ({kind:'durable-readback',generation,digest});
function saved() {return fire(begin(),e('SAVE_VERIFIED',2,{operationId:'op_1',proof:proof(1)}));}
function uncertain() {return fire(begin(),e('SAVE_UNCERTAIN',2,{operationId:'op_1'}));}
function rejected(s,ev,reason) {const r=UX.transition(s,ev);assert.equal(r.accepted,false);assert.equal(r.reason,reason);assert.strictEqual(r.state,s);}

for (const [desc,run,slot] of [
  ['run zero',0,'test_slot'],['run negative',-1,'test_slot'],['fraction run',1.5,'test_slot'],
  ['unsafe run',Number.MAX_SAFE_INTEGER+1,'test_slot'],['empty slot',9,''],
  ['path injection',9,'../../etc'],['unicode',9,'ąąć'],['too long',9,'x'.repeat(49)]
]) test('identity rejection: '+desc,()=>assert.throws(()=>UX.initialState(run,slot),TypeError));
test('valid maximum slot and safe run',()=>assert.equal(UX.initialState(Number.MAX_SAFE_INTEGER,'x'.repeat(48)).phase,'idle'));
test('initial state cannot be mutated',()=>assert.ok(Object.isFrozen(state())));
test('begin changes only pending operation',()=>{const s=begin();assert.equal(s.phase,'saving');assert.equal(s.operationId,'op_1');assert.equal(s.generation,null);});
test('success only on valid readback',()=>{const s=saved();assert.equal(s.phase,'saved');assert.equal(s.generation,1);assert.equal(s.digest,digest);assert.equal(UX.copy(s).title,'Zapis potwierdzony');});
test('save intent is not success',()=>assert.notEqual(UX.copy(begin()).title,'Zapis potwierdzony'));
test('a COMMIT throw is NOT a success',()=>{const s=uncertain();assert.equal(s.phase,'uncertain');assert.equal(UX.copy(s).level,'danger');assert.match(UX.copy(s).title,/Nie można potwierdzić/);});
test('late verified event after uncertain state cannot relabel success',()=>{const s=uncertain();rejected(s,e('SAVE_VERIFIED',3,{operationId:'op_1',proof:proof(1)}),'wrong-operation-or-phase');});
test('cannot start new save while uncertain',()=>rejected(uncertain(),e('BEGIN',3,{operationId:'new'}),'operation-not-allowed'));
test('cannot reconcile without prior uncertainty',()=>rejected(begin(),e('RECONCILE_BEGIN',2,{operationId:'op_1'}),'wrong-operation-or-phase'));
test('reconcile requires matching operation',()=>rejected(uncertain(),e('RECONCILE_BEGIN',3,{operationId:'wrong'}),'wrong-operation-or-phase'));
test('reconcile verified requires readback',()=>{const s=fire(uncertain(),e('RECONCILE_BEGIN',3,{operationId:'op_1'}));rejected(s,e('RECONCILE_VERIFIED',4,{operationId:'op_1'}),'missing-or-nonmonotonic-readback');});
test('positive reconcile resolves uncertain',()=>{const s=fire(uncertain(),e('RECONCILE_BEGIN',3,{operationId:'op_1'}));const n=fire(s,e('RECONCILE_VERIFIED',4,{operationId:'op_1',proof:proof(1)}));assert.equal(n.phase,'saved');});
test('negative reconcile requires host confirmation',()=>{const s=fire(uncertain(),e('RECONCILE_BEGIN',3,{operationId:'op_1'}));rejected(s,e('RECONCILE_NOT_SAVED',4,{operationId:'op_1'}),'unconfirmed-reconciliation');const n=fire(s,e('RECONCILE_NOT_SAVED',4,{operationId:'op_1',confirmed:true}));assert.equal(n.phase,'blocked');assert.equal(n.problem,'NO_DURABLE_COMMIT');});
test('inconclusive reconcile remains uncertain',()=>{const s=fire(uncertain(),e('RECONCILE_BEGIN',3,{operationId:'op_1'}));assert.equal(fire(s,e('RECONCILE_INCONCLUSIVE',4,{operationId:'op_1'})).phase,'uncertain');});
test('replay seq ignored',()=>rejected(begin(),e('SAVE_VERIFIED',1,{operationId:'op_1',proof:proof(1)}),'replayed-or-out-of-order'));
test('foreign run ignored',()=>rejected(begin(),e('SAVE_VERIFIED',99,{runId:10,operationId:'op_1',proof:proof(1)}),'foreign-run-or-slot'));
test('foreign slot ignored',()=>rejected(begin(),e('SAVE_VERIFIED',99,{slot:'foreign',operationId:'op_1',proof:proof(1)}),'foreign-run-or-slot'));
test('foreign op ignored with no sequence consumption',()=>rejected(begin(),e('SAVE_VERIFIED',99,{operationId:'op_2',proof:proof(1)}),'wrong-operation-or-phase'));
test('invalid event blocked',()=>rejected(begin(),e('BOGUS',2,{operationId:'op_1'}),'invalid-event'));
test('missing sequence blocked',()=>rejected(begin(),e('SAVE_VERIFIED',NaN,{operationId:'op_1',proof:proof(1)}),'invalid-event'));
test('invalid proof type blocked',()=>rejected(begin(),e('SAVE_VERIFIED',2,{operationId:'op_1',proof:{kind:'memory-only',generation:1,digest}}),'missing-or-nonmonotonic-readback'));
test('invalid checksum blocked',()=>rejected(begin(),e('SAVE_VERIFIED',2,{operationId:'op_1',proof:{kind:'durable-readback',generation:1,digest:'bad'}}),'missing-or-nonmonotonic-readback'));
test('missing generation blocked',()=>rejected(begin(),e('SAVE_VERIFIED',2,{operationId:'op_1',proof:{kind:'durable-readback',digest}}),'missing-or-nonmonotonic-readback'));
test('nonmonotonic generation blocked',()=>{let s=saved();s=fire(s,e('BEGIN',3,{operationId:'op_2'}));rejected(s,e('SAVE_VERIFIED',4,{operationId:'op_2',proof:proof(1)}),'missing-or-nonmonotonic-readback');});
test('newer generation is accepted',()=>{let s=saved();s=fire(s,e('BEGIN',3,{operationId:'op_2'}));s=fire(s,e('SAVE_VERIFIED',4,{operationId:'op_2',proof:proof(2)}));assert.equal(s.generation,2);});
test('second BEGIN while saving cannot hijack operation',()=>rejected(begin(),e('BEGIN',2,{operationId:'op_2'}),'operation-not-allowed'));
test('unrecognized failure reason cannot enter blocked',()=>rejected(begin(),e('SAVE_FAILED',2,{operationId:'op_1',reason:'SUCCEEDED'}),'invalid-failure-reason'));
for(const reason of ['BUSY','STALE','CONFLICT','ROLLBACK','IO','INVALID_ID','CORRUPT'])
  test('blocked for '+reason,()=>{const s=fire(begin(),e('SAVE_FAILED',2,{operationId:'op_1',reason}));assert.equal(s.phase,'blocked');assert.notEqual(UX.copy(s).level,'success');});
test('BUSY shows competing host text',()=>{const s=fire(begin(),e('SAVE_FAILED',2,{operationId:'op_1',reason:'BUSY'}));assert.match(UX.copy(s).detail,/Inna sesja/);});
test('blocked state requires host readiness attestation',()=>{const s=fire(begin(),e('SAVE_FAILED',2,{operationId:'op_1',reason:'IO'}));rejected(s,e('HOST_READY',3),'host-not-ready');assert.equal(fire(s,e('HOST_READY',3,{confirmed:true})).phase,'idle');});
test('HOST_READY cannot clear uncertainty',()=>rejected(uncertain(),e('HOST_READY',3,{confirmed:true}),'host-not-ready'));
test('DISCONNECT during pending becomes uncertain',()=>{const s=fire(begin(),e('DISCONNECT',2));assert.equal(s.phase,'uncertain');});
test('DISCONNECT during confirmed becomes blocked',()=>{const s=fire(saved(),e('DISCONNECT',3));assert.equal(s.phase,'blocked');assert.equal(s.generation,1);assert.notEqual(UX.copy(s).level,'success');});
test('duplicate disconnect while already uncertain ignored',()=>rejected(uncertain(),e('DISCONNECT',3),'already-uncertain'));
test('fresh run has no prior green state',()=>{const s=saved();const n=UX.initialState(s.runId+1,s.slot);assert.equal(n.generation,null);assert.equal(n.phase,'idle');assert.notEqual(UX.copy(n).level,'success');});
test('unrecognized phase defaults to fail-closed UX',()=>assert.equal(UX.copy({...state(),phase:'mystery'}).level,'danger'));
test('strict readback protects replay across 128 chained operations',()=>{
  let s=state(),seq=0;
  for(let i=1;i<=128;i++){
    const id='op_'+i;s=fire(s,e('BEGIN',++seq,{operationId:id}));
    s=fire(s,e('SAVE_VERIFIED',++seq,{operationId:id,proof:proof(i)}));
    assert.equal(s.generation,i);
  }
  assert.equal(s.phase,'saved');
});
test('malformed events never create false success in 500 attempts',()=>{
  const s=begin();
  for(let i=0;i<500;i++){
    const event=e('SAVE_VERIFIED',i+2,{operationId:i%2?'bad':'op_1',proof:{kind:'memory-only',generation:1,digest}});
    const r=UX.transition(s,event);assert.equal(r.accepted,false);assert.strictEqual(r.state,s);
  }
});
test('real HTML has labeled controls, live region and no simulated save presented as actual',()=>{
  const html=fs.readFileSync(path.join(__dirname,'index.html'),'utf8');
  assert.match(html,/lang="pl"/);assert.match(html,/aria-live="polite"/);
  for(const key of ['confirmed','ambiguous','busy','reconcile','fresh']) assert.match(html,new RegExp('id="'+key+'"'));
  assert.match(html,/nie wykonuje zapisu do gry/i);assert.match(html,/save_state\.js/);
});
test('manifest lists immutable source bytes and workflow',()=>{
  const manifest=JSON.parse(fs.readFileSync(path.join(__dirname,'manifest.json'),'utf8'));
  assert.equal(manifest.package,'B01-11-SAVE-TRUST-001/R1');
  assert.equal(manifest.files.length,5);
  for(const item of manifest.files){
    assert.ok(!item.path.includes('..'));
    const full=path.resolve(__dirname,'../../../../',item.path);
    const bytes=fs.readFileSync(full);
    assert.equal(bytes.length,item.bytes,item.path);
    assert.equal(require('node:crypto').createHash('sha256').update(bytes).digest('hex'),item.sha256,item.path);
  }
});
