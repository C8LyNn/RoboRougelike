// W12 B01-10 standalone Node 22+ contract/fixtures validator
import {readFileSync} from 'node:fs';
function assertContract(c){
 const errs=[];
 if(c.id!=="W12-B01-10-CONTRACT-RELEASE-R1"||c.version!=="1.0.1")errs.push("BAD_SCHEMA_VERSION");
 if(c.nativeUnity!=="NOT_RUN"||c.g2!=="HOLD")errs.push("FALSE_NATIVE_G2");
 if(c.build.root.maxTrustedCoreCount!==1||c.build.hit.decorationCanAuthorize||c.build.volume.maxNumericalPenetrationM>0.005)errs.push("BAD_BUILD_CONTRACT");
 if(c.legendary.policy.oneOfEachDefinitionMountedPerRobot!==true||!c.legendary.policy.noGlobalDifferentDefinitionCap||!c.legendary.policy.zeroHpMountedCounts||c.legendary.policy.weaponFamiliesPerRobot!==2)errs.push("BAD_LEGEND_POLICY");
 const ids=new Set();for(const d of c.legendary.definitions){if(ids.has(d.Id))errs.push("DUPLICATE_LEGEND_ID");ids.add(d.Id);if(!["Gun","HoverEngine"].includes(d.BasePartKind)||Object.values(d.Footprint).some(v=>!Number.isInteger(v)||v<=0))errs.push("BAD_LEGEND_SHAPE");const m=d.MassCalibration;if(m.finalMassKg!==null||m.status!=="UNRESOLVED_NATIVE_TEST"||!m.testGridKg.includes(m.r0Kg)||!m.testGridKg.includes(m.r1Kg)||m.testGridKg.some(v=>v<m.testRangeKg[0]||v>m.testRangeKg[1]))errs.push("MASS_NOT_GROUNDED");if(d.Ability.implemented)errs.push("FALSE_ABILITY_IMPLEMENTED");}
 if(c.mapgen.rigWidthsM.Light!==2.98||c.mapgen.rigWidthsM.Heavy!==5.18)errs.push("INCORRECT_REAL_HULL");
 if(c.camera.allowedOwnRayIntersections!==0||c.camera.allowedWorldPenetrations!==0||c.camera.rmb!=="familyB")errs.push("BAD_CAMERA");
 if(c.rac.timeOwner!=="FixedUpdate"||c.rac.fixedHz!==50||!c.rac.renderFPS.includes(144))errs.push("BAD_RAC");
 return errs;
}

function resultBuild(x,c){
 const OK={status:"PASS",code:"OK"};
 if(x.case==="hit"){
  if(x.role!=="structural")return{status:"REJECT",code:"DECORATIVE_COLLIDER"};
  if(!x.registered||x.hitToken!==x.authToken)return{status:"REJECT",code:"UNREGISTERED_COLLIDER"};
  if(x.hitOwner!==x.socketOwner)return{status:"REJECT",code:"SOCKET_OWNER_MISMATCH"};
  if(x.socketId!==x.requestedSocket)return{status:"REJECT",code:"SOCKET_MISMATCH"};
  if(!x.inPatch||x.gapM>c.build.hit.maxGapM)return{status:"REJECT",code:"OUTSIDE_SOCKET_PATCH"};
  if(x.angleDeg>c.build.hit.maxFaceDeviationDeg)return{status:"REJECT",code:"OBLIQUE_HIT"};return OK;
 }
 if(x.case==="root"){
  if(x.roots.length!==1)return{status:"REJECT",code:"MULTIPLE_CORE_ROOTS"};
  if(x.roots[0]!==x.trustedId)return{status:"REJECT",code:"UNTRUSTED_CORE"};
  if(!x.connected||x.supportRoot!==x.trustedId)return{status:"REJECT",code:"CORE_DISCONNECTED"};return OK;
 }
 if(x.case==="footprint"){
  const coords=x.cells,adj=(a,b)=>coords[a]&&coords[b]&&coords[a].reduce((n,v,k)=>n+Math.abs(v-coords[b][k]),0)===1;
  if(coords.length===0||new Set(coords.map(p=>p.join(","))).size!==coords.length)return{status:"REJECT",code:"DISCONNECTED_FOOTPRINT"};
  const visited=new Set([0]),q=[0];
  while(q.length){const i=q.shift();for(const [a,b] of x.edges){if((a===i||b===i)&&adj(a,b)){const k=a===i?b:a;if(!visited.has(k)){visited.add(k);q.push(k);}}}}
  return visited.size===coords.length?OK:{status:"REJECT",code:"DISCONNECTED_FOOTPRINT"};
 }
 if(x.case==="volume"){
  if(!x.physicsChecked||!Number.isFinite(x.depthM))return{status:"REJECT",code:"PHYSICS_UNVERIFIED"};
  return x.depthM<=c.build.volume.maxNumericalPenetrationM?OK:{status:"REJECT",code:"COLLIDER_PENETRATION"};
 }
 if(x.case==="demo"){
  if(!c.build.demo.allowedShaders.includes(x.shader))return{status:"REJECT",code:"URP_SHADER_UNAVAILABLE"};
  if(x.errorMagentaFraction>c.build.demo.maxErrorMagentaFraction)return{status:"REJECT",code:"URP_ERROR_MAGENTA"};
  if(!x.visibleGreenRed)return{status:"REJECT",code:"PREVIEW_NOT_DISTINCT"};return OK;
 }
 return{status:"REJECT",code:"UNKNOWN_BUILD_CASE"};
}

function resultLegend(x,c){
 const OK={status:"PASS",code:"OK"},p=x.newPart;
 if(x.owned.some(z=>z.id===p.id))return{status:"REJECT",code:"DUPLICATE_INSTANCE"};
 if(x.event==="acquire")return OK;
 if(p.hp<=0)return{status:"REJECT",code:"DESTROYED_ITEM"};
 if(p.definition&&x.owned.some(z=>z.robot===p.robot&&z.definition===p.definition))return{status:"REJECT",code:"LEGEND_ALREADY_EQUIPPED"};
 if(p.kind==="Gun"){const fam=new Set(x.owned.filter(z=>z.robot===p.robot&&z.kind==="Gun").map(z=>z.family));fam.add(p.family);if(fam.size>c.legendary.policy.weaponFamiliesPerRobot)return{status:"REJECT",code:"WEAPON_FAMILY_3"};}
 return OK;
}

function resultMap(x,c){
 if(!x.connected)return{status:"REJECT",code:"MAIN_ROUTE_DISCONNECTED"};
 if(x.kind==="ramp"&&!x.advertised)return{status:"PASS",code:"OPTIONAL_BYPASS_PERMITTED"};
 if(x.clearanceM<x.widthM+2*c.mapgen.clearancePerSideM)return{status:"REJECT",code:x.kind==="ramp"?"OPTIONAL_RAMP_MISADVERTISED":"MAIN_ROUTE_TOO_NARROW"};
 return{status:"PASS",code:"GEOMETRY_PRECHECK_ONLY"};
}

function resultCamera(x,c){
 if(x.ownRayHits>c.camera.allowedOwnRayIntersections)return{status:"REJECT",code:"OWN_RAY_COLLISION"};
 if(x.worldPenetrations>c.camera.allowedWorldPenetrations)return{status:"REJECT",code:"WORLD_CLIPPING"};
 if(x.movementM/x.dt>c.camera.proposedMaxContinuousCameraSpeedMps)return{status:"REJECT",code:"CAMERA_DISCONTINUITY"};
 if(x.writers!==1)return{status:"REJECT",code:"MULTIPLE_CAMERA_WRITERS"};
 if(x.aimDeltaDeg>c.camera.maxAimDeltaDeg)return{status:"REJECT",code:"ZOOM_CHANGED_AIM"};
 if(x.lmb!==c.camera.lmb||x.rmb!==c.camera.rmb)return{status:"REJECT",code:"WEAPON_INPUT_CHANGED"};
 return{status:"PASS",code:"OK"};
}

function resultRac(x,c){
 if(x.timerWriter!==c.rac.timeOwner||x.fixedHz!==c.rac.fixedHz)return{status:"REJECT",code:"TIMER_NOT_FIXED"};
 if(x.timeWriters!==1)return{status:"REJECT",code:"DOUBLE_TIMER"};
 if(x.runner!==x.boundRunner)return{status:"REJECT",code:"WRONG_RUNNER"};
 if(!x.ordered)return{status:"REJECT",code:"WRONG_CHECKPOINT_ORDER"};
 if(!x.heightClear)return{status:"REJECT",code:"CHECKPOINT_HEIGHT_INVALID"};
 if(x.checkpointShape!=="XZ_GATE")return{status:"REJECT",code:"CHECKPOINT_SHAPE_UNFAIR"};
 if(x.arriveSec>=x.deadlineSec)return{status:"REJECT",code:"TIMEOUT"};
 if(!x.resetAtomic)return{status:"REJECT",code:"RESET_INCOMPLETE"};
 return{status:"PASS",code:"OK"};
}

function suite(c,fx){
 const errs=assertContract(c),ids=new Set(),domains={};let pass=0;
 if(fx.contractId!==c.id)errs.push("FIXTURE_CONTRACT_MISMATCH");
 for(const f of fx.fixtures){
  if(ids.has(f.id))errs.push("DUPLICATE_FIXTURE_ID");ids.add(f.id);
  domains[f.domain]=(domains[f.domain]||0)+1;
  const handler={BUILD:resultBuild,LEGENDARY:resultLegend,MAPGEN:resultMap,CAMERA:resultCamera,RAC:resultRac}[f.domain];
  const v=handler?handler(f.input,c):{status:"REJECT",code:"UNKNOWN_DOMAIN"};
  if(v.status===f.expected.status&&v.code===f.expected.code)pass++;else errs.push(f.id+" expected "+JSON.stringify(f.expected)+" found "+JSON.stringify(v));
 }
 const bad=JSON.parse(JSON.stringify(c));bad.legendary.policy.noGlobalDifferentDefinitionCap=false;
 if(!assertContract(bad).includes("BAD_LEGEND_POLICY"))errs.push("NEGATIVE_LEGEND_CONTROL_FAILED");
 const bad2=JSON.parse(JSON.stringify(c));bad2.build.volume.maxNumericalPenetrationM=0.5;
 if(!assertContract(bad2).includes("BAD_BUILD_CONTRACT"))errs.push("NEGATIVE_VOLUME_CONTROL_FAILED");
 const copy=JSON.parse(JSON.stringify(fx));copy.fixtures[0].expected.code="WRONG";
 if(resultBuild(copy.fixtures[0].input,c).code===copy.fixtures[0].expected.code)errs.push("NEGATIVE_EXPECTATION_CONTROL_FAILED");
 return{contractId:c.id,testDateUTC:"2026-10-25",executedIn:"V8 JavaScript isolate (exact functions serialized to Node .mjs)",scenarios:fx.fixtures.length,passed:pass,failed:fx.fixtures.length-pass,negativeMutationControls:3,domains,errors:errs,verdict:errs.length?"FAIL":"PASS",nativeUnity:"NOT_RUN",g2:"HOLD"};
}
const base = new URL('.',import.meta.url);
const contract=JSON.parse(readFileSync(process.argv[2]||new URL('contract.json',base),'utf8'));
const fixtures=JSON.parse(readFileSync(process.argv[3]||new URL('fixtures.json',base),'utf8'));
const result=suite(contract,fixtures);
process.stdout.write(JSON.stringify(result,null,2)+'\n');
if(result.verdict!=='PASS')process.exitCode=1;
