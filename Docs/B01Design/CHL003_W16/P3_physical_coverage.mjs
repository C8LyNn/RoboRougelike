// B01-10 PASS 3: examine ORIGINAL producer PlayMode test coverage, NOT Unity results.
import {readFileSync} from 'node:fs';
import {createHash} from 'node:crypto';
import path from 'node:path';
export const PRODUCER='077e692785aff2990a2d72a80fd1db62ac57b0e8';
export const BLOBS={motor:'2ec5334539cfe3b1108ad2c868e04be7a9b18129',demo:'9308ceb9eab89d1ab483b560ff3ed58fa6d2d32e',play:'64590d1fcb1faf278e120922a6dbfab23d24976d',metrics:'b5050e3921092a2192d5928f86aec6ca0f208ab6'};
export const load=folder=>Object.fromEntries(Object.keys(BLOBS).map(k=>[k,readFileSync(path.join(folder,k+'.cs'),'utf8')]));
const isTrue=(s,v)=>s.includes(v);
export function review(s){
 const errors=[];
 for(const [name,sha] of Object.entries(BLOBS)){
  if(typeof s[name]!=='string'){errors.push('ORIGINAL_MISSING_'+name);continue}
  const b=Buffer.from(s[name]); const hash=createHash('sha1').update('blob '+b.length+'\0').update(b).digest('hex');
  if(hash!==sha)errors.push('BLOB_MISMATCH_'+name+':'+hash);
 }
 if(errors.length)return{status:'SOURCE_FAIL',errors,native:'NOT_RUN'};
 const a=s.play.indexOf('P07_SameNativeCourseLightHeavyThreeRunsEachWithRealContact'),
       b=s.play.indexOf('P08_FinalCheckpointAndFinishCoLocatedNeverCompleteSameFixedStep');
 const test=s.play.slice(a,b), yaw=s.play.slice(s.play.indexOf('P14_HeavyYaw8_'));
 for(const [id,v] of [
 ['P07_FOUND',a>0&&b>a],
 ['P07_RAMP',test.includes('measured.RampContacts')],
 ['P07_BRIDGE',test.includes('measured.BridgeContacts')],
 ['METRICS_BARRIER',s.metrics.includes('public int BarrierContacts')],
 ['MOTOR_GROUND_RAY',s.motor.includes('Physics.RaycastNonAlloc(')],
 ['SOURCE_WIDTH',s.demo.includes('new Vector3(3.70f, .60f, 10f)')],
 ['P14_YAW8',yaw.includes('Quaternion.Euler(0f,8f,0f)')]]) if(!v)errors.push('ORIGINAL_ANCHOR_MISSING_'+id);
 const flags={
  barrierAssert:/Assert\.(Greater|IsTrue)\s*\([^;]*?measured\.BarrierContacts/s.test(test),
  wheelSupportAssert:/Assert\.(Greater|IsTrue)\s*\([^;]*?(GroundedSteps|SupportRayCount|AirborneSteps)/s.test(test),
  dynamicDifferenceAssert:/\b(light|heavy)(Elapsed|PeakSpeed|RampContacts|BridgeContacts)\b/i.test(test),
  yaw8FullFinishAssert:/Assert\.AreEqual\s*\(\s*CHL003CourseCore\.Phase\.Completed/s.test(yaw)
 };
 return{status:errors.length?'SOURCE_FAIL':'PINNED_SOURCE_INSPECTED',inputSha:PRODUCER,verifiedBlobs:4,flags,
 findings:[
 {id:'P3-GROUND',result:flags.wheelSupportAssert?'COVERED_IN_SOURCE_NOT_NATIVE':'WHEEL_CONTACT_AT_SEAM_UNVERIFIED'},
 {id:'P3-RAMP',result:flags.barrierAssert?'BARRIER_ASSERTED_SEAM_STILL_UNVERIFIED':'BARRIER_CONTACT_COLLECTED_BUT_NOT_ASSERTED'},
 {id:'P3-HEAVY',result:flags.yaw8FullFinishAssert?'YAW8_FINISH_ASSERTED_NOT_NATIVE':'YAW8_ONLY_CHECKPOINT_NOT_FULL_FINISH'}
 ],errors,native:'NOT_RUN',G2:'HOLD'};
}
if(process.argv[1]&&process.argv[1].endsWith('P3_physical_coverage.mjs')){
 const r=process.argv[2]?review(load(process.argv[2])):{status:'SOURCE_INPUT_MISSING',native:'NOT_RUN'};
 console.log(JSON.stringify(r,null,2));
 if(r.status!=='PINNED_SOURCE_INSPECTED')process.exitCode=1;
}