#!/usr/bin/env node
"use strict";
/* BAL17 R5Catalog validator + threat/choice export.
 * No Unity gameplay source modified. Mirrors source R5RunPlan.Validate and Threat
 * so Node checks are static/model-only; Unity NUnit owner-side acceptance is separate.
 * Run: node Research/B01-10/P17/validate_deck.cjs
 */
const fs=require("node:fs"),path=require("node:path"),crypto=require("node:crypto");
const ROOT=__dirname,DATA="R5_EncounterDeck_BAL17.json";
const SOURCE="Assets/R5/R5RunPlan.cs",ENEMY_SOURCE="Assets/R3/R3EnemyArchetype.cs";
const defFields=new Set(["Id","Title","Arena","Roster","Variants","AlternativeRoster",
"ThreatBudget","Risk","Loot","Rule","RewardJitter","EnemyCount","HealthMultiplier",
"DamageMultiplier","FireIntervalMultiplier","ProjectileSpeedMultiplier","MoveSpeed",
"PreferredDistance","StrafeStrength","ScrapReward"]);
const clone=x=>JSON.parse(JSON.stringify(x));
const gitBlob=b=>crypto.createHash("sha1").update(Buffer.concat(
[Buffer.from("blob "+b.length+"\0"),b])).digest("hex");
function fault(msg){throw Error(msg);}
function ok(test,msg){if(!test)fault(msg);}
function num(v,min,max,key){ok(typeof v==="number"&&Number.isFinite(v)&&v>=min&&v<=max,key+" out of range");}
function variantRoster(option,roster){
 ok(Array.isArray(roster)&&roster.length>=1&&roster.length<=3,"bad roster");
 ok(roster.length===option.EnemyCount&&option.Variants.length===roster.length,"roster/variant/EnemyCount mismatch");
 for(let i=0;i<roster.length;i++){
 const k=roster[i],v=option.Variants[i];
 ok(Number.isInteger(k)&&k>=0&&k<=3,"invalid R3EnemyArchetype");
 ok(Number.isInteger(v)&&v>=0&&v<=3,"invalid R5EnemyVariant");
 ok(v!==2||k===2,"EliteGunner must be Gunner");
 ok((v!==1&&v!==3)||k===1,"EliteBruiser/SiegeBoss must be Bruiser");
 }
}
function spawned(option,roster){
 const isBoss=option.Variants.includes(3),n=isBoss?1:Math.min(option.EnemyCount+1,4),spawn=[];
 for(let i=0;i<n;i++)spawn.push({kind:i<option.EnemyCount?roster[i]:0,
 variant:i<option.EnemyCount?option.Variants[i]:0,escort:i>=option.EnemyCount});
 return spawn;
}
function threat(option,roster=option.Roster){
 const foes=spawned(option,roster),kinds=new Set(),h=option.HealthMultiplier,
 d=option.DamageMultiplier,f=option.FireIntervalMultiplier,m=option.MoveSpeed;
 let total=0;
 for(const {kind:k,variant:v} of foes) {
 kinds.add(k);
 const hull=k===1?1.6:k===0?0.8:1.1,gun=(k===1||k===2)?1.6:1;
 const mobility=k===3?1.4:1,range=k===2?1.3:1,variant=v===3?3.8:v===0?1:1.65;
 total+=(hull*h+gun*d/Math.max(.3,f)+mobility*m*.22+range)*variant;
 }
 return total*(kinds.size>1?1.12:1)*(option.Arena===2?1.15:option.Arena===1?1.05:1);
}
function risk(opt,t=threat(opt)){
 return opt.Variants.includes(3)?"Boss":t<5?"Niskie":t<9?"Umiarkowane":t<12?"Wysokie":"Bardzo wysokie";
}
function validate(deck) {
 ok(deck&&typeof deck==="object"&&!Array.isArray(deck),"catalog object required");
 ok(Object.keys(deck).length===1&&Object.hasOwn(deck,"Stages"),"R5Catalog only accepts Stages");
 ok(Array.isArray(deck.Stages)&&deck.Stages.length===6,"exact six stages required");
 const ids=new Set(),report=[];
 for(let s=0;s<6;s++){
  const options=deck.Stages[s]?.Options;
  ok(Array.isArray(options)&&options.length>=1&&options.length<=2,"stage "+(s+1)+" must have 1-2 options");
  for(let c=0;c<options.length;c++){
   const x=options[c];
   ok(x&&typeof x==="object"&&!Array.isArray(x),"encounter object required");
   ok(Object.keys(x).every(k=>defFields.has(k)),"unknown encounter field (unsupported R1 schema)");
   ok(typeof x.Id==="string"&&x.Id.startsWith("bal17_")&&!ids.has(x.Id),"duplicate/invalid P17 encounter Id");
   ids.add(x.Id);
   ok(typeof x.Title==="string"&&x.Title.trim()!==""&&
     typeof x.Rule==="string"&&x.Rule.trim()!==""&&
     typeof x.Loot==="string"&&x.Loot.trim()!=="","missing title/loot/rule");
   ok(Number.isInteger(x.Arena)&&x.Arena>=0&&x.Arena<=2,"invalid original R5ArenaKind");
   ok(Number.isInteger(x.EnemyCount)&&x.EnemyCount>=1&&x.EnemyCount<=3,"enemy count must be 1-3");
   ok(Array.isArray(x.Variants),"missing variants");
   variantRoster(x,x.Roster);
   if(x.AlternativeRoster!==undefined)variantRoster(x,x.AlternativeRoster);
   num(x.HealthMultiplier,.5,3,"health");num(x.DamageMultiplier,.1,3,"damage");
   num(x.FireIntervalMultiplier,.3,3,"fireInterval");
   num(x.ProjectileSpeedMultiplier,.5,2,"projectile");
   num(x.MoveSpeed,0,6,"move");num(x.PreferredDistance,6,20,"preferred");
   num(x.StrafeStrength,0,1,"strafe");num(x.ThreatBudget,.1,100,"threatBudget");
   ok(Number.isInteger(x.ScrapReward)&&x.ScrapReward>=0&&x.ScrapReward<=100000,"invalid payout");
   ok(Number.isInteger(x.RewardJitter)&&x.RewardJitter>=0&&x.RewardJitter<=1000&&
     x.RewardJitter<=x.ScrapReward,"invalid payout jitter");
   const threats=[threat(x)];
   if(x.AlternativeRoster)threats.push(threat(x,x.AlternativeRoster));
   const worst=Math.max(...threats);
   ok(worst<=x.ThreatBudget+1e-5,"source R5Threat > budget "+x.Id);
   ok(x.ThreatBudget-Math.ceil(worst)>=1&&x.ThreatBudget-Math.ceil(worst)<=3,
       "threat cap not tuned to authored difficulty "+x.Id);
   ok(x.Risk===risk(x),"R5 risk label disagrees with source Threat "+x.Id);
   report.push({stage:s+1,id:x.Id,arena:x.Arena,
      playableOptions:options.length,threat:Number(worst.toFixed(4)),
      limit:x.ThreatBudget,spawnCount:spawned(x,x.Roster).length,
      rewardLow:x.ScrapReward-x.RewardJitter,rewardHigh:x.ScrapReward+x.RewardJitter,
      roster:x.Roster.join("-"),risk:x.Risk});
  }
 }
 ok(report.length===10,"expected ten ready-to-import options");
 ok(deck.Stages[0].Options.length===1&&deck.Stages[5].Options.length===1,
 "start tutorial + unique final boss");
 ok(deck.Stages.slice(1,5).every(s=>s.Options.length===2),
 "four actual risk/reward choices in stages 2..5");
 ok(deck.Stages[5].Options[0].Variants.includes(3),"stage 6 must use original R5 SiegeBoss");
 ok(new Set(deck.Stages.flatMap(x=>x.Options.map(o=>o.Arena))).size===3,
 "all three original R5 arena kinds required");
 ok(deck.Stages.slice(1,5).every(stage=>{
  const [a,b]=stage.Options;
  return a.ScrapReward+a.RewardJitter<b.ScrapReward-b.RewardJitter;
 }),"choice payout ranges must not overlap; player sees meaningful risk/reward difference");
 return report;
}
function runTests(deck){
 let pass=0;
 function check(label,fn){fn();pass++;console.log("PASS "+String(pass).padStart(2,"0")+" "+label);}
 function expectReject(label,mutate){
  check(label,()=>{const m=clone(deck);mutate(m);let rejected=false;
  try{validate(m);}catch(e){rejected=true;}
  ok(rejected,"validator fail-open on "+label);});
 }
 check("native R5Catalog six-stage/ten-option source schema",()=>ok(validate(deck).length===10,"not ten"));
 check("stable deterministic read-only evaluation",()=>ok(JSON.stringify(validate(deck))===JSON.stringify(validate(deck)),"drift"));
 check("required zero gap for boss escort",()=>ok(spawned(deck.Stages[5].Options[0],deck.Stages[5].Options[0].Roster).length===1,"boss escort bug"));
 check("3 or 4 real spawn including escort",()=>ok(spawned(deck.Stages[2].Options[0],deck.Stages[2].Options[0].Roster).length===3&&
 spawned(deck.Stages[4].Options[0],deck.Stages[4].Options[0].Roster).length===4,"escort count"));
 check("differentiated guaranteed stage 2..5 payouts",()=>ok(deck.Stages.slice(1,5).every(s=>
  s.Options[0].ScrapReward+s.Options[0].RewardJitter < s.Options[1].ScrapReward-s.Options[1].RewardJitter),"risk parity"));
 check("all ten dynamic threat scores lower than tuned budgets",()=>ok(validate(deck).every(x=>x.threat<x.limit),"budget"));
 check("all alternate rosters bounded by source budget",()=>ok(deck.Stages.flatMap(s=>s.Options).filter(x=>x.AlternativeRoster).every(x=>
  threat(x,x.AlternativeRoster)<=x.ThreatBudget),"alternate risk"));
 check("finite payouts and source enum values only",()=>ok(validate(deck).every(x=>x.rewardLow>=0&&x.rewardHigh<=100000),"payout"));
 expectReject("NEG 1: duplicate ID",d=>{d.Stages[2].Options[0].Id=d.Stages[1].Options[0].Id});
 expectReject("NEG 2: missing stage",d=>{d.Stages.pop()});
 expectReject("NEG 3: third route in one stage",d=>{d.Stages[1].Options.push(clone(d.Stages[1].Options[0]));});
 expectReject("NEG 4: incorrect variants length",d=>{d.Stages[2].Options[0].Variants.pop()});
 expectReject("NEG 5: boss on Scout instead Bruiser",d=>{d.Stages[5].Options[0].Roster=[0]});
 expectReject("NEG 6: EliteGunner on Bruiser",d=>{d.Stages[3].Options[1].Roster=[1]});
 expectReject("NEG 7: threat cap below actual",d=>{d.Stages[4].Options[1].ThreatBudget=1});
 expectReject("NEG 8: payout jitter greater than reward",d=>{d.Stages[2].Options[0].RewardJitter=1000});
 expectReject("NEG 9: unknown ArenaKind enum",d=>{d.Stages[1].Options[0].Arena=999});
 expectReject("NEG 10: illegal AI move speed",d=>{d.Stages[4].Options[0].MoveSpeed=7});
 expectReject("NEG 11: invalid alternative roster archetype",d=>{d.Stages[2].Options[0].AlternativeRoster=[3,99]});
 expectReject("NEG 12: reward bands overlap",d=>{d.Stages[1].Options[1].ScrapReward=95});
 expectReject("NEG 13: invented R1 serialized field",d=>{d.Stages[0].Options[0].NewEnemyWaveScript="UNIMPLEMENTED"});
 expectReject("NEG 14: damage NaN",d=>{d.Stages[0].Options[0].DamageMultiplier=NaN});
 return pass;
}
function main(){
 const filepath=path.join(ROOT,DATA);
 const deck=JSON.parse(fs.readFileSync(filepath,"utf8"));
 const records=validate(deck),count=runTests(deck);
 const original=fs.readFileSync(filepath);
 const root=path.resolve(ROOT,"../../..");
 // Source pin is enforced when this checkout contains Codex identical original R5 files.
 const sourcePins=[
  [SOURCE,"6fde58f1fc621cde6488d9ee81c3510aa729839a"],
  [ENEMY_SOURCE,"66cd1ee734b129117b3753d1205469a0496cb673"]
 ];
 for(const [rel,pin] of sourcePins){
 const absolute=path.join(root,rel);
 if(fs.existsSync(absolute)){
   const sha=gitBlob(fs.readFileSync(absolute));
   ok(sha===pin,"native source changed, manual BAL17 rebase required: "+rel);
   console.log("PIN_OK "+rel+" "+sha);
 } else console.log("NATIVE_SOURCE_NOT_IN_CHECKOUT "+rel+"; compare pinned Git blob at PR310 before Unity import");
 }
 console.log("DATA_BLOB "+gitBlob(original));
 console.log("TESTS "+count+"/"+count+" SOURCE_JS_PASS | UNITY_NUNIT_NOT_RUN");
 console.log("R5_CHOICES "+JSON.stringify(records));
}
if(require.main===module)try{main();}catch(e){console.error("FAIL "+e.stack);process.exitCode=1;}
module.exports={validate,threat,spawned,risk,runTests,gitBlob};
