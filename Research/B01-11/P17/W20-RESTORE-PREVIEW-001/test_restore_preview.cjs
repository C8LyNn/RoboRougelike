'use strict';
const {test} = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const U = require('./restore_preview.js');
const current = () => ({runId:42,slotId:'slot_A',generation:10,sha256:'a'.repeat(64),partCount:12,detachedCount:3,consumedCount:4});
const target = () => ({...current(),generation:11,sha256:'b'.repeat(64),partCount:10,detachedCount:2,consumedCount:5});
const host = () => ({epoch:7,verifiedReadback:true,quiescent:true,singleSlotOwner:true,canonicalReady:true,unresolvedCommit:false});
function expectBlock(reason,a=current(),b=target(),h=host()) {const s=U.session(a,b,h); assert.equal(s.phase,'blocked');assert.equal(s.verdict,reason);assert.equal(U.display(s).level,'danger');assert.equal(U.delta(s),null);}
function armed() {const s=U.session(current(),target(),host());assert.equal(s.phase,'review');const a=U.arm(s);assert.equal(a.accepted,true);return a.state;}
function deny(reason,s=armed(),a=current(),b=target(),h=host()) {const x=U.request(s,a,b,h);assert.equal(x.accepted,false);assert.equal(x.reason,reason);assert.strictEqual(x.state,s);}
test('verified same-run newer checkpoint can be reviewed',()=>assert.equal(U.gate(current(),target(),host()).reason,'READY'));
test('review is not restore',()=>assert.equal(U.session(current(),target(),host()).phase,'review'));
test('review tells player world could change',()=>assert.match(U.display(U.session(current(),target(),host())).message,/zmienić stan robota/));
test('arm is explicit first confirmation',()=>assert.equal(armed().phase,'armed'));
test('request is only proposal not actual restore',()=>{const s=armed();const r=U.request(s,current(),target(),host());assert.equal(r.accepted,true);assert.equal(r.reason,'REQUEST_ONLY');assert.equal(r.state.phase,'requested');assert.match(U.display(r.state).message,/nie jest potwierdzenie wczytania/i);});
test('no repeated proposal on requested phase',()=>{const s=U.request(armed(),current(),target(),host()).state;deny('NOT_ARMED',s);});
test('no arm from blocked state',()=>{const s=U.session(current(),{...target(),runId:99},host());assert.equal(U.arm(s).accepted,false);});
test('no request without arm',()=>deny('NOT_ARMED',U.session(current(),target(),host())));
test('delta is descriptive only',()=>assert.deepEqual(U.delta(U.session(current(),target(),host())),{partChange:-2,detachedChange:-1,consumedChange:1,generationsAhead:1}));
const invalidSnapshots = [
 ['null',null],['empty',{}],['run zero',{...current(),runId:0}],['run noninteger',{...current(),runId:1.1}],
 ['run overflow',{...current(),runId:Number.MAX_SAFE_INTEGER+1}],['slot traversal',{...current(),slotId:'../../foo'}],
 ['slot empty',{...current(),slotId:''}],['slot unicode',{...current(),slotId:'ą'}],['slot too long',{...current(),slotId:'z'.repeat(49)}],
 ['gen zero',{...current(),generation:0}],['gen negative',{...current(),generation:-2}],
 ['gen infinity',{...current(),generation:Infinity}],['gen overflow',{...current(),generation:Number.MAX_SAFE_INTEGER+1}],
 ['digest uppercase',{...current(),sha256:'A'.repeat(64)}],['digest missing',{...current(),sha256:null}],
 ['digest short',{...current(),sha256:'a'.repeat(63)}],['negative parts',{...current(),partCount:-1}],
 ['fraction parts',{...current(),partCount:1.7}],['detached too many',{...current(),detachedCount:13}],
 ['consumed negative',{...current(),consumedCount:-1}],['consumed fraction',{...current(),consumedCount:0.5}]
];
for(const [name,s] of invalidSnapshots) test('invalid metadata '+name,()=>{assert.equal(U.validSnapshot(s),false);expectBlock('INVALID_METADATA',s);});
for(const [name,h] of [
 ['null',null],['empty',{}],['epoch zero',{...host(),epoch:0}],
 ['epoch overflow',{...host(),epoch:2**54}],['readback string',{...host(),verifiedReadback:'true'}],
 ['quiescent undefined',{...host(),quiescent:undefined}],['owner number',{...host(),singleSlotOwner:1}],
 ['canonical absent',{...host(),canonicalReady:null}],['commit absent',{...host(),unresolvedCommit:undefined}]
]) test('invalid host '+name,()=>{assert.equal(U.validHost(h),false);expectBlock('INVALID_METADATA',current(),target(),h);});
for(const [name,c,t,h,reason] of [
 ['foreign run',current(),{...target(),runId:43},host(),'FOREIGN_RUN'],
 ['foreign slot',current(),{...target(),slotId:'slot_B'},host(),'FOREIGN_SLOT'],
 ['unverified target',current(),target(),{...host(),verifiedReadback:false},'UNVERIFIED_TARGET'],
 ['commit uncertainty',current(),target(),{...host(),unresolvedCommit:true},'AMBIGUOUS_COMMIT'],
 ['host race',current(),target(),{...host(),singleSlotOwner:false},'HOST_BUSY'],
 ['world not quiescent',current(),target(),{...host(),quiescent:false},'WORLD_ACTIVE'],
 ['canonical not integrated',current(),target(),{...host(),canonicalReady:false},'CANONICAL_API_PENDING'],
 ['older target',current(),{...target(),generation:9},host(),'OLDER_GENERATION'],
 ['same gen different digest',current(),{...target(),generation:10},host(),'FORKED_GENERATION'],
 ['same gen same digest',current(),{...target(),generation:10,sha256:'a'.repeat(64)},host(),'ALREADY_CURRENT'],
 ['consumed ids decrease',current(),{...target(),consumedCount:3},host(),'POSSIBLE_ID_RESURRECTION'],
]) test('block '+name,()=>expectBlock(reason,c,t,h));
test('block order: foreign identity wins over possible ready',()=>expectBlock('FOREIGN_RUN',current(),{...target(),runId:999}, {...host(),canonicalReady:false}));
test('host epoch changes mid-confirmation cannot be accepted',()=>deny('CHANGED_DURING_CONFIRMATION',armed(),current(),target(),{...host(),epoch:8}));
test('checkpoint digest changes mid-confirmation cannot be accepted',()=>deny('CHANGED_DURING_CONFIRMATION',armed(),current(),{...target(),sha256:'c'.repeat(64)},host()));
test('current physical generation changes mid-confirmation',()=>deny('CHANGED_DURING_CONFIRMATION',armed(),{...current(),generation:9},target(),host()));
test('target part count changes mid-confirmation',()=>deny('CHANGED_DURING_CONFIRMATION',armed(),current(),{...target(),partCount:9},host()));
test('target consumed count changes mid-confirmation',()=>deny('CHANGED_DURING_CONFIRMATION',armed(),current(),{...target(),consumedCount:6},host()));
test('target detached count changes mid-confirmation',()=>deny('CHANGED_DURING_CONFIRMATION',armed(),current(),{...target(),detachedCount:3},host()));
test('current part count changes mid-confirmation',()=>deny('CHANGED_DURING_CONFIRMATION',armed(),{...current(),partCount:13},target(),host()));
test('readback proof is withdrawn mid-confirmation',()=>deny('UNVERIFIED_TARGET',armed(),current(),target(),{...host(),verifiedReadback:false}));
test('commit becomes uncertain mid-confirmation',()=>deny('AMBIGUOUS_COMMIT',armed(),current(),target(),{...host(),unresolvedCommit:true}));
test('slot authority lost mid-confirmation',()=>deny('HOST_BUSY',armed(),current(),target(),{...host(),singleSlotOwner:false}));
test('world resumes mid-confirmation',()=>deny('WORLD_ACTIVE',armed(),current(),target(),{...host(),quiescent:false}));
test('canonical API disabled mid-confirmation',()=>deny('CANONICAL_API_PENDING',armed(),current(),target(),{...host(),canonicalReady:false}));
test('host source invalid mid-confirmation',()=>deny('INVALID_METADATA',armed(),current(),target(),{...host(),epoch:0}));
test('foreign slot mid-confirmation',()=>deny('FOREIGN_SLOT',armed(),current(),{...target(),slotId:'other'},host()));
test('no synthetic claim can mean executed Unity restore',()=>{const r=U.request(armed(),current(),target(),host());assert.equal(r.reason,'REQUEST_ONLY');assert.equal('loaded' in r.state,false);});
test('sessions and nested snapshots are frozen',()=>{const s=U.session(current(),target(),host());assert.ok(Object.isFrozen(s));for(const field of ['current','target','host'])assert.ok(Object.isFrozen(s[field]));});
test('caller changing input after snapshot does not alter UI',()=>{const c=current(),t=target(),h=host();const s=U.session(c,t,h);c.generation=100;t.generation=4;h.quiescent=false;assert.equal(s.phase,'review');assert.equal(s.current.generation,10);assert.equal(s.target.generation,11);});
test('show blocked copy without untrusted checkpoint details',()=>{const s=U.session(current(),{...target(),runId:777},host());assert.equal(U.display(s).title,'Wczytanie zablokowane');assert.equal(U.delta(s),null);});
test('missing state fails closed',()=>assert.equal(U.display(null).level,'danger'));
test('stress: 150 generated source snapshots monotonic and safe',()=>{for(let i=1;i<=150;i++){const c={...current(),generation:i,consumedCount:i};const t={...target(),generation:i+1,consumedCount:i+1};const x=U.request(U.arm(U.session(c,t,host())).state,c,t,host());assert.equal(x.accepted,true);}});
test('stress: 250 invalid save fork digests rejected',()=>{for(let i=0;i<250;i++){const t={...target(),generation:10,sha256:(i%2?'b':'c').repeat(64)};assert.equal(U.session(current(),t,host()).verdict,'FORKED_GENERATION');}});
test('interactive UI provides accessible review + explicit two-step confirmation',()=>{const html=fs.readFileSync(path.join(__dirname,'index.html'),'utf8');assert.match(html,/lang="pl"/);assert.match(html,/aria-live="polite"/);for(const id of ['safe','foreign','stale','uncertain','arm','confirm']) assert.match(html,new RegExp('id="'+id+'"'));assert.match(html,/nie wykonuje rzeczywistego wczytania/i);assert.match(html,/restore_preview\.js/);});
test('workflow runs on 2 OS and invokes original tests',()=>{const wf=fs.readFileSync(path.join(__dirname,'../../../../.github/workflows/b01-11-p17w20-restore-preview-001.yml'),'utf8');assert.match(wf,/ubuntu-24\.04/);assert.match(wf,/windows-2022/);assert.match(wf,/node --test .*test_restore_preview\.cjs/);});
test('manifest pins all five companion files in byte-exact form',()=>{const manifest=JSON.parse(fs.readFileSync(path.join(__dirname,'manifest.json'),'utf8'));assert.equal(manifest.package,'B01-11-RESTORE-PREVIEW-001/R1');assert.equal(manifest.files.length,5);for(const item of manifest.files){assert.ok(!item.path.includes('..'));const full=path.resolve(__dirname,'../../../../',item.path);const data=fs.readFileSync(full);assert.equal(data.length,item.bytes);assert.equal(crypto.createHash('sha256').update(data).digest('hex'),item.sha256);}});
