// B01-10 W26 READ-ONLY design/QA oracle. Never represents actual Unity gameplay.
// It authorizes no socket and no checkpoint in runtime.
import fs from 'node:fs';
import crypto from 'node:crypto';
import path from 'node:path';
export const manifests={
  "task_id": "N26-10-GAMEPLAY-CP-SOCKET-20261027",
  "producer_shas": {
    "CHL": "077e692785aff2990a2d72a80fd1db62ac57b0e8",
    "PARTSIZE": "be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
  },
  "source_files": [
    {
      "name": "chl_cross",
      "path": "Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003GateCrossing.cs",
      "sha": "5c736da620487e48131f6d454bcf231ae368b513"
    },
    {
      "name": "chl_controller",
      "path": "Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003CourseController.cs",
      "sha": "1e51b02cc833acb9dedbd8b6a0177fd4e42381a0"
    },
    {
      "name": "chl_demo",
      "path": "Assets/RoboRougelikeChallenges/CHL003/Demo/CHL003DemoBootstrap.cs",
      "sha": "9308ceb9eab89d1ab483b560ff3ed58fa6d2d32e"
    },
    {
      "name": "part_native",
      "path": "Assets/PARTSIZE001/Runtime/PartSizeNativePhysics.cs",
      "sha": "44c507eba175029fb2750acbcac44119c1d97001"
    },
    {
      "name": "part_contract",
      "path": "Assets/PARTSIZE001/Runtime/PartSizeContract.cs",
      "sha": "07455919a5544875fee5a78ecbbce36dc6cb49c2"
    }
  ],
  "status": "SOURCE_ONLY_REVIEW_NOT_UNITY",
  "g2": "HOLD"
};
const expectedAnchors={
 chl_cross:[
 'previousDepth <= 0f && currentDepth > 0f',
 'currentDepth - previousDepth >= .005f && facingDot >= .5f',
 'localMinX >= gateMinX + minimumSideClearance',
 'localMaxX <= gateMaxX - minimumSideClearance',
 'minimumSideClearance = .25f'
 ],
 chl_controller:[
 'if (!ForwardHullCrossesGate(gate)) return false;',
 'collider.attachedRigidbody != activeActor',
 'core.AdvanceTime(Time.fixedDeltaTime)',
 'Physics.ComputePenetration(',
 'finishArbiter.TryCommit('
 ],
 chl_demo:[
 'new Vector3(3.70f, .60f, 10f)',
 'new Vector3(3.70f, 4.5f, .16f)',
 'float width = heavy ? 1.0f : .59f'
 ],
 part_native:[
 'Physics.SyncTransforms()',
 'Physics.OverlapBox(',
 'return "INCOMPLETE_EXTERNAL_BROADPHASE";',
 'return "TYPED_SOCKET_UNVERIFIED";',
 'return "NOT_EXPOSED_LOGICAL_FACE";',
 'PartSizeGrid.IsExposedFace('
 ],
 part_contract:[
 'public static bool IsExposedFace(',
 'return "UNMEASURED_PHYSICAL_ENVELOPE";'
 ]
};
export function verifyPinnedFiles(root){
 const out={checks:0,blobs:[],failures:[]};
 for(const file of manifests.source_files){
   const location=path.join(root,file.name+'.cs');
   if(!fs.existsSync(location)){out.failures.push('SOURCE_MISSING:'+file.name);continue}
   const bytes=fs.readFileSync(location);
   const computed=crypto.createHash('sha1').update('blob '+bytes.length+'\0').update(bytes).digest('hex');
   if(computed!==file.sha)out.failures.push('BLOB_DRIFT:'+file.name+':'+computed);
   out.blobs.push({name:file.name,gitBlobSha1:computed});
   for(const anchor of expectedAnchors[file.name]){
     out.checks++;
     if(!bytes.toString('utf8').includes(anchor))out.failures.push('CONTRACT_MISSING:'+file.name+':'+anchor);
   }
 }
 return{...out,status:out.failures.length?'FAIL':'PASS'};
}
export function evaluatePolicyCase(x){
 if(x.lane==='CHL'){
   if(!x.sameActor)return 'WRONG_ACTOR_REJECT';
   if(!x.running||x.paused)return 'NOT_RUNNING_REJECT';
   if(!x.correctOrdinal)return 'WRONG_ORDER_REJECT';
   if(!x.forwardCenterPlane||!x.facingForward)return 'DIRECTION_REJECT';
   if(!x.fullHullWithinAperture)return 'HULL_APERTURE_REJECT';
   if(x.finish && (!x.allRequiredGates||!x.separateFinishFixedStep||!x.overlapsPhysicalFinish))
     return 'FINISH_PENDING_REJECT';
   return 'ELIGIBLE_FOR_NATIVE_PHYSICS_EVIDENCE';
 }
 if(x.lane==='PART'){
   if(!x.exposed)return 'NOT_EXPOSED_LOGICAL_FACE';
   // This is ACTUAL CURRENT SOURCE expected result, irrespective of host claims.
   return 'TYPED_SOCKET_UNVERIFIED';
 }
 throw Error('UNKNOWN_LANE');
}
// The FUTURE binding is only a DESIGN RULE. Neither current PARTSIZE nor Codex
// currently implements it; it must never be interpreted as a verified mount.
export function proposedHostSocketPolicy(x){
 if(!x.exposed)return 'NOT_EXPOSED_LOGICAL_FACE';
 if(!x.authoritativeRegisteredSocket)return 'TYPED_SOCKET_UNVERIFIED';
 if(!x.structuralReceiver||!x.trustedCoreConnected)return 'STRUCTURAL_RECEIVER_INVALID';
 if(!x.typeCompatible||!x.normalAligned)return 'SOCKET_INCOMPATIBLE';
 if(!x.contactAreaVerified)return 'SOCKET_CONTACT_UNVERIFIED';
 if(!x.measuredColliderEnvelope||!x.completeWorldBroadphase)return 'PHYSICS_UNVERIFIED';
 if(x.hasPenetration)return 'PHYSICAL_PENETRATION';
 return 'READY_FOR_AUTHORIZED_HOST_VALIDATION_NOT_MOUNT_PASS';
}
