import test from 'node:test';
import assert from 'node:assert/strict';
import {COLUMNS,review,SOURCE_PIN} from './W17_trial_scorecard.mjs';
const header=COLUMNS.join(',')+'\n';
const make=(profile,attempt,seconds,contacts=10)=>({
 profile,attempt,massKg:profile==='LIGHT'?270:1150,widthM:profile==='LIGHT'?2:2.82,
 lengthM:profile==='LIGHT'?2.3:3.4,elapsedSec:seconds,state:'Completed',
 physicsSamples:300,collisionEntries:10,contactPoints:contacts,barrierPoints:2,
 rampPoints:10,bridgePoints:15,groundPoints:55,falls:0,checkpoints:4,
 completions:1,peakSpeedMps:profile==='LIGHT'?8:5,
 travelMeters:40,minHeightM:0,maxHeightM:2
});
const sourceFixtures=()=>[make('LIGHT',1,8),make('LIGHT',2,8.1),make('LIGHT',3,8.2),
 make('HEAVY',1,14),make('HEAVY',2,14.2),make('HEAVY',3,14.4)];
const csv=rows=>header+rows.map(r=>COLUMNS.map(k=>r[k]).join(',')).join('\n')+'\n';
test('real CHL source SHA pin is deliberate',()=>assert.equal(SOURCE_PIN.length,40));
test('empty genuine native input -> NOT_RUN',()=>assert.equal(review(header).status,'NOT_RUN'));
test('only positive synthetic exercise is never native PASS',()=>{
 const r=review(csv(sourceFixtures()));
 assert.equal(r.status,'DATA_READY_NATIVE_EVIDENCE_PENDING');
 assert.equal(r.nativeUnity,'NOT_VERIFIED_BY_CSV');
 assert.deepEqual(r.scorecard.discriminatingMetrics.includes('elapsedSec'),true);
});
test('insufficient 5 native summaries do not become PASS',()=>{
 assert.match(review(csv(sourceFixtures().slice(0,5))).reason,/REQUIRE_EXACT_3_EACH/);
});
test('four Light and two Heavy are invalid',()=>{
 const r=sourceFixtures();r[5].profile='LIGHT';r[5].attempt=4;
 assert.equal(review(csv(r)).status,'INVALID_INPUT');
});
test('duplicate attempt is not six trials',()=>{
 const r=sourceFixtures();r[1].attempt=1;
 assert.match(review(csv(r)).reason,/DUPLICATE_ATTEMPT/);
});
test('a rig cannot secretly change mass between rounds',()=>{
 const r=sourceFixtures();r[1].massKg=300;
 assert.match(review(csv(r)).reason,/MASS_CHANGED_BETWEEN_TRIALS/);
});
test('NaN, Infinity, empty and wrong header fail closed',()=>{
 for(const replacement of ['NaN','Infinity','']) {
  const s=csv(sourceFixtures()).replace(',8,',','+replacement+',');
  assert.equal(review(s).status,'INVALID_INPUT');
 }
 assert.equal(review('badheader\n').status,'INVALID_INPUT');
});
test('negative contact count fails closed',()=>{
 const r=sourceFixtures();r[2].rampPoints=-1;
 assert.equal(review(csv(r)).status,'INVALID_INPUT');
});
for(const [label,change,expected] of [
 ['checkpoint missing',r=>r[0].checkpoints=3,'CHECKPOINT_COUNT'],
 ['no physical ramp',r=>r[0].rampPoints=0,'NO_RAMP_CONTACT'],
 ['no physical bridge',r=>r[0].bridgePoints=0,'NO_BRIDGE_CONTACT'],
 ['fall occurred',r=>r[0].falls=1,':FALL'],
 ['one did not finish',r=>r[0].state='Running','NOT_COMPLETED'],
 ['reported double completion',r=>r[0].completions=2,'COMPLETIONS'],
 ['not enough motion',r=>r[0].travelMeters=20,'INSUFFICIENT_DISTANCE']
])test(label+' fails design acceptance',()=>{
 const r=sourceFixtures();change(r);
 const receipt=review(csv(r));
 assert.equal(receipt.status,'DESIGN_ACCEPTANCE_FAIL');
 assert.ok(receipt.acceptanceErrors.some(x=>x.includes(expected)));
});
test('all pass but indistinguishable observed dynamics -> inconclusive, not fake balance PASS',()=>{
 const r=sourceFixtures();
 for(let i=0;i<3;i++){
  r[i+3].elapsedSec=r[i].elapsedSec;
  r[i+3].peakSpeedMps=r[i].peakSpeedMps;
 }
 const q=review(csv(r));
 assert.equal(q.status,'DATA_READY_NATIVE_EVIDENCE_PENDING');
 assert.equal(q.physicalDiscrimination,'INCONCLUSIVE_NO_STABLE_DIFFERENCE');
});
test('no raw CSV can authorize G2',()=>{
 const r=review(csv(sourceFixtures()));
 assert.equal(r.g2,'HOLD');
 assert.ok(r.evidenceRequired.some(x=>x.includes('B01-60')));
});
