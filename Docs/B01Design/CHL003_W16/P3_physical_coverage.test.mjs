import test from 'node:test';import assert from 'node:assert/strict';
import {load,review,PRODUCER} from './P3_physical_coverage.mjs';
const dir=process.env.P3_ORIGINAL_DIR;const original=dir?load(dir):{};
test('original source required: no fake pass on absent git inputs',()=>{
 assert.ok(dir,'P3_ORIGINAL_DIR required');assert.equal(review(original).status,'PINNED_SOURCE_INSPECTED');
});
test('source includes ramp + bridge collision assertions',()=>{
 const x=review(original);assert.equal(x.verifiedBlobs,5);assert.equal(x.inputSha,PRODUCER);
 assert.equal(x.flags.barrierAssert,false);
});
test('W26 false-grounded-gap source fix + P15 authored but never native executed',()=>{
 const x=review(original);assert.equal(x.flags.nearGroundSourceFix,true);
 assert.equal(x.flags.nativeP15Authored,true);
 assert.equal(x.findings[0].result,'SOURCE_FIXED_B0160_NEAR_GAP_NATIVE_NOT_RUN');
 assert.equal(x.flags.wheelSupportAssert,false);
 assert.equal(x.flags.dynamicDifferenceAssert,false);
});
test('P14 yaw8 only checks a checkpoint, not Heavy end-to-end',()=>{
 const x=review(original);assert.equal(x.flags.yaw8FullFinishAssert,false);
 assert.deepEqual(x.findings.map(t=>t.id),['P3-GROUND','P3-RAMP','P3-HEAVY']);
});
test('mutating original producer text is detected by exact blob proof',()=>{
 const bad={...original,play:original.play.replace('measured.BridgeContacts','BADSOURCE.BridgeContacts')};
 assert.equal(review(bad).status,'SOURCE_FAIL');
 assert.ok(review(bad).errors.some(e=>e.startsWith('BLOB_MISMATCH_play')));
});
test('source QA cannot issue native PASS or G2',()=>{
 const x=review(original);assert.equal(x.native,'NOT_RUN');assert.equal(x.G2,'HOLD');
});