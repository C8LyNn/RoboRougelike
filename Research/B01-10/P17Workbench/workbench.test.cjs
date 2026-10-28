#!/usr/bin/env node
"use strict";
// BAL17-R5WB001 real zero-dependency Node-ready source test suite.
// node Research/B01-10/P17Workbench/workbench.test.cjs
const W=require("./R5WorkbenchCore.js");
const same=(a,b)=>JSON.stringify(a)===JSON.stringify(b);
const valid=x=>W.inspect(x).valid;
const blank=()=>W.fresh();
const reject=(fn,re)=>{let er=null;try{fn();}catch(e){er=e;}return !!er&&(!re||re.test(er.message));};
const severe={HealthMultiplier:3,DamageMultiplier:3,FireIntervalMultiplier:.3,MoveSpeed:6};
const cases=[
["fresh six stages",()=>blank().Stages.length===6],
["fresh six valid offers",()=>W.inspect(blank()).rows.length===6],
["R5 exact JSON roundtrip",()=>same(W.parse(W.exportJSON(blank())),blank())],
["ordinary scout and escort spawn",()=>W.spawn(blank().Stages[0].Options[0]).length===2],
["original arenas 0-1-2",()=>same(blank().Stages.map(x=>x.Options[0].Arena),[0,1,2,0,1,2])],
["source-model threats finite",()=>W.inspect(blank()).rows.every(x=>x.threat>0&&x.threat<x.budget)],
["add second real R5 route",()=>W.addChoice(blank(),2).Stages[2].Options.length===2],
["add no mutation of source",()=>{let a=blank();W.addChoice(a,2);return a.Stages[2].Options.length===1;}],
["alternate reward is visibly higher",()=>{const [a,b]=W.addChoice(blank(),2).Stages[2].Options;return a.ScrapReward+a.RewardJitter<b.ScrapReward-b.RewardJitter;}],
["cannot add 3rd route",()=>reject(()=>W.addChoice(W.addChoice(blank(),2),2),/maximum/)],
["remove alternative valid",()=>valid(W.removeChoice(W.addChoice(blank(),2),2,1))],
["cannot remove last",()=>reject(()=>W.removeChoice(blank(),0,0),/retain/)],
["title edit immutable",()=>{const x=blank(),y=W.put(x,1,0,{Title:"Inne starcie"});return x.Stages[1].Options[0].Title!==y.Stages[1].Options[0].Title;}],
["severe difficulty auto cap",()=>{const x=W.put(blank(),2,0,severe,true);return valid(x)&&x.Stages[2].Options[0].ThreatBudget>12;}],
["identical severe difficulty with no auto cap rejects",()=>reject(()=>W.put(blank(),2,0,severe,false),/threat/)],
["too low explicit cap rejects",()=>reject(()=>W.put(blank(),1,0,{ThreatBudget:1}),/threat/)],
["negative invalid speed and NaN rejects",()=>reject(()=>W.put(blank(),1,0,{MoveSpeed:7}),/MoveSpeed/)&&reject(()=>W.put(blank(),1,0,{DamageMultiplier:NaN}),/DamageMultiplier/)],
["malformed input rejects",()=>reject(()=>W.parse('{"Stages":'),/invalid JSON/)],
["unexpected Unity field rejected",()=>{let x=blank();x.Stages[0].Options[0].FakeWeapon="x";return reject(()=>W.parse(JSON.stringify(x)),/unknown/);}],
["duplicate enemy encounter ID rejected",()=>{let x=blank();x.Stages[1].Options[0].Id=x.Stages[0].Options[0].Id;return reject(()=>W.parse(JSON.stringify(x)),/duplicate/);}],
["R5 requires six stages",()=>{let x=blank();x.Stages.pop();return reject(()=>W.parse(JSON.stringify(x)),/six stages/);}],
["elite gunner cannot use scout",()=>reject(()=>W.put(blank(),3,0,{Variants:[2]}),/EliteGunner/)],
["elite bruiser cannot use scout",()=>reject(()=>W.put(blank(),3,0,{Variants:[1]}),/Bruiser/)],
["boss cannot use scout",()=>reject(()=>W.put(blank(),5,0,{Variants:[3]}),/Bruiser/)],
["original gunner elite supported",()=>valid(W.put(blank(),3,0,{Roster:[2],Variants:[2]}))],
["original bruiser boss has no escort",()=>{let x=W.put(blank(),5,0,{Roster:[1],Variants:[3]});return W.spawn(x.Stages[5].Options[0]).length===1&&x.Stages[5].Options[0].Risk==="Boss";}],
["three enemies plus escort equals four",()=>{let x=W.put(blank(),4,0,{Roster:[0,2,3],Variants:[0,0,0]});return W.spawn(x.Stages[4].Options[0]).length===4;}],
["valid alt roster is supported",()=>valid(W.put(blank(),4,0,{Roster:[0,2],Variants:[0,0],AlternativeRoster:[3,0]}))],
["invalid alt roster length rejected",()=>reject(()=>W.put(blank(),1,0,{AlternativeRoster:[0,2]}),/roster/)],
["invalid archetype 99 rejected",()=>reject(()=>W.put(blank(),1,0,{Roster:[99]}),/enum/)],
["unknown field rejected",()=>reject(()=>W.put(blank(),0,0,{FakeEconomicRule:10}),/unsupported/)],
["RiskName spoof rejected",()=>reject(()=>W.put(blank(),0,0,{Risk:"Boss"}),/unsupported/)],
["stale risk warns but import is valid",()=>{let x=blank();x.Stages[0].Options[0].Risk="Boss";let r=W.inspect(x);return r.valid&&r.warnings.length===1;}],
["overlapping reward ranges warn",()=>{let x=W.addChoice(blank(),1);x.Stages[1].Options[1].ScrapReward=x.Stages[1].Options[0].ScrapReward;let r=W.inspect(x);return r.valid&&r.warnings.some(s=>s.includes("overlap"));}],
["jitter above reward rejected",()=>reject(()=>W.put(blank(),1,0,{RewardJitter:1000}),/RewardJitter/)],
["negative Złom rejected",()=>reject(()=>W.put(blank(),1,0,{ScrapReward:-2}),/ScrapReward/)],
["out of range arena rejected",()=>reject(()=>W.put(blank(),1,0,{Arena:4}),/Arena/)],
["bad stage rejected",()=>reject(()=>W.put(blank(),-1,0,{Title:"bad"}),/unknown/)],
["failed edit has zero mutation",()=>{let x=blank(),b=W.exportJSON(x);reject(()=>W.put(x,1,0,{MoveSpeed:999}));return W.exportJSON(x)===b;}],
["edited deck export/import stable",()=>{let x=W.put(W.addChoice(blank(),2),2,1,{Title:"Route B",ScrapReward:180});return same(W.parse(W.exportJSON(x)),x);}],
["1MB limit fail closed",()=>reject(()=>W.parse("X".repeat(1000001)),/under 1MB/)]
];
let p=0,f=0;
for(let i=0;i<cases.length;i++){const [name,run]=cases[i];try{if(!run())throw Error("assertion false");p++;process.stdout.write("PASS "+String(i+1).padStart(2,"0")+" "+name+"\n");}catch(e){f++;process.stderr.write("FAIL "+String(i+1).padStart(2,"0")+" "+name+": "+e.message+"\n");}}
process.stdout.write("BAL17-R5WB001 SOURCE_ONLY "+p+" PASS / "+f+" FAIL | NATIVE_UNITY_NOT_RUN\n");
if(f)process.exitCode=1;
