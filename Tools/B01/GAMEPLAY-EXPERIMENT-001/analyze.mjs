import {readFileSync} from 'node:fs';
const mandatory=['eventId','origin','buildId','challenge','seed','finished','seconds','repairScrap','lootedNonArmor','lootedArmor','wheelDamage','steeringBeforeDegSec','steeringAfterDegSec','partsChangedAfterRun','garageSeconds','firstMeaningfulActionSeconds'];
export const CHALLENGES=['defense','race','boss'];
function isValid(r){
 return r&&mandatory.every(k=>Object.hasOwn(r,k))&&typeof r.eventId==='string'&&r.eventId.length>0&&
  ['native_player','native_api','synthetic_fixture'].includes(r.origin)&&typeof r.buildId==='string'&&
  CHALLENGES.includes(r.challenge)&&Number.isInteger(r.seed)&&r.seed>=0&&typeof r.finished==='boolean'&&
  ['seconds','repairScrap','steeringBeforeDegSec','steeringAfterDegSec','garageSeconds','firstMeaningfulActionSeconds'].every(k=>Number.isFinite(r[k])&&r[k]>=0)&&
  ['lootedNonArmor','lootedArmor','wheelDamage','partsChangedAfterRun'].every(k=>Number.isInteger(r[k])&&r[k]>=0)&&
  r.lootedNonArmor<=3&&r.lootedArmor===0&&r.firstMeaningfulActionSeconds<=r.seconds;
}
const avg=a=>a.length?a.reduce((x,y)=>x+y,0)/a.length:null;
const median=a=>{if(!a.length)return null;const s=[...a].sort((x,y)=>x-y);return(s[Math.floor((s.length-1)/2)]+s[Math.floor(s.length/2)])/2;};
export function analyze(rows,{requireReal=false}={}){
 const errors=[],used=new Set(),seen=new Set(),byBuild=new Map(),byCombo=new Map();
 for(let i=0;i<rows.length;i++){
  const r=rows[i];if(!isValid(r)){errors.push({row:i,reason:'INVALID_EVENT_SCHEMA_OR_SALVAGE'});continue;}
  if(used.has(r.eventId)){errors.push({row:i,reason:'DUPLICATE_EVENT_ID'});continue;}used.add(r.eventId);
  const key=r.buildId+'|'+r.challenge+'|'+r.seed+'|'+r.origin;
  if(seen.has(key)){errors.push({row:i,reason:'DUPLICATE_BUILD_CHALLENGE_SEED'});continue;}seen.add(key);
  if(!byBuild.has(r.buildId))byBuild.set(r.buildId,[]);byBuild.get(r.buildId).push(r);
  const k=r.buildId+'|'+r.challenge;if(!byCombo.has(k))byCombo.set(k,[]);byCombo.get(k).push(r);
 }
 const accepted=[...byBuild.values()].flat();const origins=[...new Set(accepted.map(x=>x.origin))].sort();
 if(!accepted.length)errors.push({reason:'EMPTY_DATA'});
 if(requireReal&&!accepted.some(r=>r.origin==='native_player'||r.origin==='native_api'))errors.push({reason:'NO_NATIVE_SOURCE_EVIDENCE'});
 if(requireReal&&accepted.some(r=>r.origin==='synthetic_fixture'))errors.push({reason:'SYNTHETIC_DATA_NOT_ALLOWED_IN_REAL_EVIDENCE'});
 const combos=[];
 for(const [key,rs] of byCombo){const [build,challenge]=key.split('|');combos.push({build,challenge,n:rs.length,
  finishedRate:rs.filter(x=>x.finished).length/rs.length,
  medianFinishSeconds:median(rs.filter(x=>x.finished).map(x=>x.seconds)),
  medianRepairScrap:median(rs.map(x=>x.repairScrap)),
  medianGarageSeconds:median(rs.map(x=>x.garageSeconds)),
  medianFirstActionSeconds:median(rs.map(x=>x.firstMeaningfulActionSeconds)),
  buildChangeRate:rs.filter(x=>x.partsChangedAfterRun>0).length/rs.length});}
 combos.sort((a,b)=>a.build.localeCompare(b.build)||a.challenge.localeCompare(b.challenge));
 const buildIds=[...byBuild.keys()].sort();const dominance=[];
 // A dominates B only on common seed sets across all challenge types, conservative heuristic.
 for(const a of buildIds)for(const b of buildIds){if(a===b)continue;const rec=[];let all=true,strict=false;
  for(const ch of CHALLENGES){const aa=byCombo.get(a+'|'+ch)||[],bb=byCombo.get(b+'|'+ch)||[];
   const ma=new Map(aa.map(r=>[r.seed,r])),mb=new Map(bb.map(r=>[r.seed,r]));
   const overlap=[...ma.keys()].filter(s=>mb.has(s));
   if(overlap.length<30){all=false;continue;}
   const as=overlap.map(s=>ma.get(s)),bs=overlap.map(s=>mb.get(s));
   const ar=avg(as.map(r=>Number(r.finished))),br=avg(bs.map(r=>Number(r.finished)));
   const ac=avg(as.map(r=>r.repairScrap)),bc=avg(bs.map(r=>r.repairScrap));
   if(ar<br-1e-9||ac>bc+1e-9)all=false;
   if(ar>br+1e-9||ac<bc-1e-9)strict=true;
   rec.push({challenge:ch,paired:overlap.length,winDelta:ar-br,repairDelta:ac-bc});
  }
  if(rec.length===CHALLENGES.length&&all&&strict)dominance.push({better:a,worse:b,evidence:rec});
 }
 // Metric for a single fixed damage event: physics must corroborate source change.
 const damage=[];
 for(const [build,rs] of byBuild){const damaged=rs.filter(r=>r.wheelDamage>0);
  const silent=damaged.filter(r=>r.steeringBeforeDegSec>0 && Math.abs(r.steeringAfterDegSec-r.steeringBeforeDegSec)/r.steeringBeforeDegSec<0.05);
  if(damaged.length)damage.push({build,events:damaged.length,below5pct: silent.length,needsNativeR6Corroboration:true});
 }
 const minCell=Math.min(...combos.map(x=>x.n),Infinity);
 const evidence={originKinds:origins,sourcesIncludeNative:origins.includes('native_api')||origins.includes('native_player'),
  nativePlayerSessions:accepted.filter(r=>r.origin==='native_player').length};
 const gap=[];
 if(minCell<100||!Number.isFinite(minCell))gap.push('LESS_THAN_100_SEEDS_PER_BUILD_ACTIVITY');
 if(buildIds.length<3||CHALLENGES.some(ch=>buildIds.some(build=>!byCombo.has(build+'|'+ch))))gap.push('MISSING_BUILD_OR_CHALLENGE_MATRIX');
 if(!evidence.sourcesIncludeNative)gap.push('SYNTHETIC_ONLY_NO_PLAYTEST');
 if(!origins.includes('native_player'))gap.push('HUMAN_GAMEPLAY_NOT_MEASURED');
 if(damage.some(x=>x.below5pct))gap.push('WHEEL_DAMAGE_WITH_LT5PCT_STEERING_CHANGE_INVESTIGATE');
 if(dominance.length)gap.push('POTENTIAL_DOMINANT_BUILD_INVESTIGATE');
 return {contract:'B01-GAMEPLAY-EXPERIMENT-001-R1',acceptedEvents:accepted.length,rejectedEvents:errors.length,
  errors,combos,dominance,damage,evidence,qualityGaps:gap,
  gate:errors.length?'INVALID_DATA':gap.length?'MORE_G1_PLAYER_EVIDENCE_NEEDED':'DATA_READY_FOR_INDEPENDENT_REVIEW',
  physicalGameplayCertified:false};
}
if(process.argv[1]&&import.meta.url.endsWith(process.argv[1].replaceAll('\\','/'))){
 const path=process.argv[2];if(!path)throw Error('USAGE: node analyze.mjs <events.json> [--require-real]');
 const data=JSON.parse(readFileSync(path,'utf8'));const out=analyze(data,{requireReal:process.argv.includes('--require-real')});
 process.stdout.write(JSON.stringify(out,null,2)+'\n');if(out.errors.length)process.exitCode=1;
}
