// B01-10 W16: standalone read-only CHL003 design acceptance oracle.
// Does NOT implement a Unity course or certify physical driving.
import {readFileSync} from 'node:fs';
function course(x,c){
  const needed=x.rigWidthM+2*c.clearanceEachSideM;
  if(!Number.isFinite(x.mainWidthM)||!Number.isFinite(x.rigWidthM)||x.rigWidthM<=0||x.mainWidthM<=0)return{status:"REJECT",code:"BAD_ENVELOPE"};
  if(x.mainWidthM+1e-9<needed)return{status:"REJECT",code:"MANDATORY_ROUTE_TOO_NARROW"};
  if(x.advertisesOptional && (!Number.isFinite(x.optionalWidthM)||x.optionalWidthM+1e-9<needed))
    return{status:"REJECT",code:"OPTIONAL_ROUTE_MISADVERTISED"};
  if(!x.advertisesOptional && x.optionalWidthM!==null && x.optionalWidthM+1e-9<needed)
    return{status:"PASS",code:"MAIN_VALID_OPTIONAL_BYPASS"};
  return{status:"PASS",code:"GEOMETRY_ONLY"};
}

function crossing(x,c){
  if(x.phase!=="Running")return{status:"REJECT",code:"NOT_RUNNING"};
  if(x.observedActorId!==x.authorizedActorId)return{status:"REJECT",code:"FOREIGN_ACTOR"};
  if(x.eventGeneration!==x.activeGeneration)return{status:"REJECT",code:"STALE_GENERATION"};
  if(x.paused)return{status:"REJECT",code:"PAUSED"};
  if(x.checkpointIndex!==x.expectedIndex)return{status:"REJECT",code:"WRONG_CHECKPOINT_ORDER"};
  if(x.alreadyCredited)return{status:"REJECT",code:"DUPLICATE_CONTACT"};
  if(!(x.dt>0&&Math.abs(x.dt-c.timer.fixedDeltaSeconds)<1e-9))return{status:"REJECT",code:"BAD_PHYSICS_TIMESTEP"};
  if(!(x.before.z<x.gate.z&&x.after.z>=x.gate.z))return{status:"REJECT",code:"NO_FORWARD_PLANE_CROSSING"};
  const fraction=(x.gate.z-x.before.z)/(x.after.z-x.before.z);
  const ix=x.before.x+(x.after.x-x.before.x)*fraction;
  const iy=x.before.y+(x.after.y-x.before.y)*fraction;
  const margin=c.clearanceEachSideM;
  if(Math.abs(ix-x.gate.x)+x.rigWidthM/2+margin>x.gate.widthM/2+1e-9)
    return{status:"REJECT",code:"HULL_OUTSIDE_GATE_WIDTH"};
  if(iy-x.rigHeightM/2<x.gate.yBottom-1e-9||iy+x.rigHeightM/2>x.gate.yTop+1e-9)
    return{status:"REJECT",code:"HEIGHT_EXCLUDES_RIG"};
  return{status:"PASS",code:"GEOMETRY_EVENT_ONLY"};
}

function session(x,c){
  let phase="Ready",paused=false,index=0,elapsed=0,gen=0,emissions=0,ignored=0;
  const maxIndex=x.checkpointCount;
  if(!(maxIndex>=c.checkpoints.min&&maxIndex<=c.checkpoints.max))
    return{phase:"Invalid",index,elapsed,gen,emissions,ignored,reason:"BAD_CHECKPOINT_COUNT"};
  for(const ev of x.events){
    switch(ev.op){
      case "begin":if(phase==="Ready"){phase="Running";paused=false}else ignored++;break;
      case "tick":
        if(phase!=="Running"||paused){ignored++;break}
        if(ev.dt!==c.timer.fixedDeltaSeconds){ignored++;break}
        elapsed=Math.round((elapsed+ev.dt)*1000)/1000;
        if(x.limitSeconds!==null&&elapsed>=x.limitSeconds){phase="Failed";paused=false}
        break;
      case "pause":if(phase==="Running"&&!paused)paused=true;else ignored++;break;
      case "resume":if(phase==="Running"&&paused)paused=false;else ignored++;break;
      case "hit":
        if(phase!=="Running"||paused||ev.actor!==x.actorId||ev.generation!==gen||ev.checkpoint!==index){ignored++;break}
        index++;break;
      case "finish":
        if(phase!=="Running"||paused||ev.actor!==x.actorId||ev.generation!==gen||index!==maxIndex){ignored++;break}
        phase="Completed";emissions++;break;
      case "fall":if(phase==="Running"){phase="Failed";paused=false}else ignored++;break;
      case "retry":
        if(phase==="Failed"||phase==="Completed"){phase="Ready";paused=false;index=0;elapsed=0;gen++}else ignored++;
        break;
      default:ignored++;
    }
  }
  return{phase,index,elapsed,gen,emissions,ignored,reason:null};
}

function run(c,fx){
 const errors=[],counts={},ids=new Set();let passed=0;
 if(fx.contractId!==c.id||c.evidence.UnityPlayMode!=="NOT_RUN")errors.push("MISREPRESENTED_SCOPE");
 for(const testcase of fx.scenarios){
  if(ids.has(testcase.id))errors.push("DUPLICATE_CASE_ID "+testcase.id);ids.add(testcase.id);
  counts[testcase.type]=(counts[testcase.type]||0)+1;
  const fn={width:course,crossing,session}[testcase.type];
  if(!fn){errors.push("UNKNOWN_CASE_TYPE");continue;}
  const got=fn(testcase.input,c);
  if(JSON.stringify(got)===JSON.stringify(testcase.expected))passed++;
  else errors.push(`${testcase.id} expected=${JSON.stringify(testcase.expected)} observed=${JSON.stringify(got)}`);
 }
 const mut=JSON.parse(JSON.stringify(fx));
 mut.scenarios[0].expected.code="WRONG_EXPECTATION";
 if(JSON.stringify(course(mut.scenarios[0].input,c))===JSON.stringify(mut.scenarios[0].expected))errors.push("NEGATIVE_EXPECTATION_UNDETECTED");
 const incorrect=JSON.parse(JSON.stringify(c));incorrect.clearanceEachSideM=0;
 if(course(fx.scenarios[2].input,incorrect).status!=="PASS")errors.push("NEGATIVE_MARGIN_MUTATION_UNDETECTED");
 const spoof=JSON.parse(JSON.stringify(fx.scenarios.find(x=>x.id==="GATE-02").input));spoof.observedActorId=spoof.authorizedActorId;
 if(crossing(spoof,c).status!=="PASS")errors.push("NEGATIVE_ACTOR_MUTATION_UNDETECTED");
 return{contractId:c.id,total:fx.scenarios.length,passed,failed:fx.scenarios.length-passed,domainCounts:counts,negativeMutations:3,errors,verdict:errors.length?"FAIL":"PASS",execution:"V8 JS tested same functions serialized to Node ESM; GitHub Node CI required independently",UnityEditMode:"NOT_RUN",UnityPlayMode:"NOT_RUN",physicalDrive:"NOT_RUN",G2:"HOLD"};
}
const base=new URL('.',import.meta.url);
const contract=JSON.parse(readFileSync(new URL('contract.json',base)));
const fixtures=JSON.parse(readFileSync(new URL('fixtures.json',base)));
const receipt=run(contract,fixtures);
console.log(JSON.stringify(receipt,null,2));
if(receipt.verdict!=='PASS')process.exitCode=1;
