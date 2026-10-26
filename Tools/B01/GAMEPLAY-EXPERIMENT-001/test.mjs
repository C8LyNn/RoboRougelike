import {readFileSync} from 'node:fs';import {analyze} from './analyze.mjs';
const base=JSON.parse(readFileSync(new URL('./SYNTHETIC_FIXTURES.json',import.meta.url),'utf8'));
const results=[];function check(label,cond){results.push({label,pass:!!cond});}
const ordinary=analyze(base);check('900 synthetic events schema passes',ordinary.acceptedEvents===900&&ordinary.rejectedEvents===0);
check('100 seeds every build activity',ordinary.combos.length===9&&ordinary.combos.every(c=>c.n===100));
check('synthetic evidence never masquerades as native',ordinary.qualityGaps.includes('SYNTHETIC_ONLY_NO_PLAYTEST'));
check('synthetic evidence never implies native G1',!ordinary.physicalGameplayCertified&&ordinary.evidence.nativePlayerSessions===0);
check('diverse builds have no artificial universal winner',ordinary.dominance.length===0);
const double=[...base,base[0]];check('double event rejected',analyze(double).errors.some(x=>x.reason==='DUPLICATE_EVENT_ID'));
const badLoot=base.map((r,i)=>i===0?{...r,lootedNonArmor:4}:r);check('fourth non-armor loot rejected',analyze(badLoot).errors.some(x=>x.reason==='INVALID_EVENT_SCHEMA_OR_SALVAGE'));
const armorLoot=base.map((r,i)=>i===0?{...r,lootedArmor:1}:r);check('armor loot excluded',analyze(armorLoot).errors.some(x=>x.reason==='INVALID_EVENT_SCHEMA_OR_SALVAGE'));
const timeTravel=base.map((r,i)=>i===0?{...r,firstMeaningfulActionSeconds:r.seconds+1}:r);check('invalid time-to-first-fun rejected',analyze(timeTravel).rejectedEvents===1);
const duplicateSample=base.map((r,i)=>i===1?{...r,seed:base[0].seed,buildId:base[0].buildId,challenge:base[0].challenge}:r);check('same build+activity+seed rejected',analyze(duplicateSample).errors.some(x=>x.reason==='DUPLICATE_BUILD_CHALLENGE_SEED'));
// Make first synthetic build strictly dominate all counterparts without claiming real outcome.
const mono=base.map(r=>r.buildId==='Allrounder'?{...r,finished:true,repairScrap:0}:r.buildId==='HeavyGunner'?{...r,finished:false,repairScrap:100}: {...r,finished:false,repairScrap:100});
check('source checks flag artificial dominance',analyze(mono).dominance.some(d=>d.better==='Allrounder'));
const sourceProbe=analyze(base,{requireReal:true});check('requireReal rejects synthetic-only data',sourceProbe.errors.some(e=>e.reason==='NO_NATIVE_SOURCE_EVIDENCE'));
check('empty input fails closed',analyze([]).errors.some(e=>e.reason==='EMPTY_DATA'));
check('single mode not evidence completeness',analyze(base.filter(r=>r.challenge==='race')).qualityGaps.includes('MISSING_BUILD_OR_CHALLENGE_MATRIX'));
check('synthetic mixed with native refused under real gate',analyze(base.map((r,i)=>i===0?{...r,origin:'native_api'}:r),{requireReal:true}).errors.some(e=>e.reason==='SYNTHETIC_DATA_NOT_ALLOWED_IN_REAL_EVIDENCE'));
const summary={cases:results.length,passed:results.filter(x=>x.pass).length,failed:results.filter(x=>!x.pass).length,details:results,syntheticEvents:base.length,
 naturalDominance:ordinary.dominance.length,syntheticOnly:true,realPlaytest:'NOT_RUN',CIContractOnly:true};
process.stdout.write(JSON.stringify(summary,null,2)+'\n');if(summary.failed)process.exitCode=1;
