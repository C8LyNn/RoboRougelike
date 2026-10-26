import {readFileSync} from 'node:fs';
import {proposedGate,legacyGate,ContractLedger,REASONS} from './policy.mjs';
const tests=JSON.parse(readFileSync(new URL('./fixtures.json',import.meta.url),'utf8'));
const cfg=JSON.parse(readFileSync(new URL('./contract.json',import.meta.url),'utf8'));
const failures=[];const byDomain={};let passed=0;
function same(a,b){return JSON.stringify(a)===JSON.stringify(b)}
function runFixture(t){
 if(t.domain==='ELIGIBILITY')return proposedGate(t.snapshot);
 if(t.domain==='PRODUCER_GAP')return legacyGate(t.snapshot);
 if(t.domain==='ECON_LEDGER'){
   const m=new ContractLedger(t.parts,t.scrap);const got=[];
   for(const action of t.actions){let x;
     if(action.op==='order')x=m.order(action.id);
     else if(action.op==='start')x=m.start(action.id,!!action.loaner);
     else if(action.op==='outcome')x=m.outcome(action.id,action.kind,action.trusted??true);
     else if(action.op==='reward')x=m.grantReward(action.id,action.amount,action.canonical);
     else if(action.op==='mount')x=m.mount(action.id);
     else throw Error('UNKNOWN_ACTION');
     got.push(x);
   }
   const state={results:got,winSerial:m.winSerial,scrap:m.scrap,freeUsed:m.freeUsed,
     loanerInventoryCount:m.loanerInventoryCount,parts:[...m.parts.values()].map(p=>({id:p.id,hp:p.hp,pending:p.queuedAt!==null,mounted:p.mounted}))};
   return state;
 }
 if(t.domain==='ALTERNATIVE_POLICY')return cfg.options.find(x=>x.id===t.policy)?.[t.probe];
 throw Error('UNKNOWN_DOMAIN');
}
for(const t of tests.cases){try{const actual=runFixture(t);byDomain[t.domain]=(byDomain[t.domain]||0)+1;
 if(same(actual,t.expected))passed++;else failures.push({id:t.id,expected:t.expected,actual});}
 catch(error){failures.push({id:t.id,error:String(error)})}}
// Property/stress testing: deterministic values and idempotence across 2,000 seeds.
function rng(seed){let state=(seed|0)>>>0;return()=>{state=(Math.imul(state,1664525)+1013904223)>>>0;return state/4294967296;}}
const stressErrors=[];let stressCases=0;
for(let seed=0;seed<2000;seed++){
 const random=rng(seed+155001);const partCount=1+Math.floor(random()*9);
 const parts=Array.from({length:partCount},(_,i)=>({id:`P${seed}-${i}`,hp:0,buy:1+Math.floor(random()*400),quote:0,mounted:false}));
 for(const p of parts)p.quote=Math.floor(random()*p.buy); // quote<buy, valid price=1→quote=0
 const initialScrap=Math.floor(random()*250);
 const m=new ContractLedger(parts,initialScrap);let moneySpent=0,orderCount=0;const queuedIds=new Set();
 for(const p of parts){const before=m.scrap;const result=m.order(p.id);moneySpent+=before-m.scrap;
   if(result==='QUEUED_FREE'||result==='QUEUED_PAID'){orderCount++;queuedIds.add(p.id);if(m.mount(p.id)!=='CANNOT_MOUNT')stressErrors.push(`pending mounted seed${seed}`);}}
 if(m.scrap!==initialScrap-moneySpent||m.scrap<0)stressErrors.push(`scrap accounting seed${seed}`);
 if(m.start(`battle-${seed}`,true)!=='STARTED')stressErrors.push(`start seed${seed}`);
 const before=m.snapshot();if(m.outcome('counterfeit','WIN',false)!=='STALE_OR_UNTRUSTED_OUTCOME'||m.snapshot()!==before)stressErrors.push(`fake win seed${seed}`);
 const win=random()<0.57;
 const status=m.outcome(`battle-${seed}`,win?'WIN':'LOSS',true);
 if(status!==(win?'VICTORY_RECORDED':'DEFEAT_RECORDED'))stressErrors.push(`result seed${seed}`);
 const snap=m.snapshot();if(m.outcome(`battle-${seed}`,'WIN',true)!=='STALE_OR_UNTRUSTED_OUTCOME'||m.snapshot()!==snap)stressErrors.push(`duplicate win seed${seed}`);
 if(m.loanerInventoryCount!==0||m.parts.size!==partCount||m.winSerial!==(win?1:0)||m.freeUsed!==(!win&&orderCount>0))stressErrors.push(`invariant seed${seed}`);
 if(win&&[...m.parts.values()].some(p=>(queuedIds.has(p.id)?(p.queuedAt!==null||p.hp!==100):p.hp!==0)||p.mounted))stressErrors.push(`queued/unrequested rebuild seed${seed}`);
 if(!win&&[...m.parts.values()].some(p=>p.hp!==0))stressErrors.push(`defeat healed seed${seed}`);
 stressCases++;
}
// Three negative mutation probes, because self-consistent green tests without red oracles are weak.
const controls=[];
const sample=tests.cases.find(t=>t.id==='EL-01');controls.push({id:'MUT-01',passed:proposedGate({...sample.snapshot,ownedParts:[]})!==sample.expected});
controls.push({id:'MUT-02',passed:proposedGate({...sample.snapshot,canDeploy:true})!==sample.expected});
const a=new ContractLedger([{id:'part',hp:0,buy:1,quote:0,mounted:false}],0);a.order('part');a.start('a',true);a.outcome('a','LOSS');controls.push({id:'MUT-03',passed:a.winSerial===0&&a.parts.get('part').hp===0});
if(controls.some(x=>!x.passed))failures.push({id:'MUTATION_CONTROL',failed:controls});
if(stressErrors.length)failures.push({id:'STRESS',count:stressErrors.length,samples:stressErrors.slice(0,8)});
const summary={task:'RS-S04-DESIGN-001',source_SHA:cfg.sourceSHA,source_code_test_execution:'NOT_RUN',
 port_description:'Portable independent JS model; source-equivalent producer gate for counterexamples only',
 scenarios:tests.cases.length,passed,failed:tests.cases.length-passed,byDomain,
 mutationControls:{total:controls.length,passed:controls.filter(x=>x.passed).length},
 deterministicStress:{seeds:stressCases,failed:stressErrors.length},failures,
 verdict:failures.length?'FAIL':'PASS',UnityNative:'NOT_RUN',HQPolicyApproved:false,G2:'HOLD'};
process.stdout.write(JSON.stringify(summary,null,2)+'\n');if(failures.length)process.exitCode=1;
