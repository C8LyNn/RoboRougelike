/* B01-10 PASS17 R5WB001 — browser & Node pure-data encounter authoring engine.
 * REAL R5Catalog Stages.Options schema. No Unity Assets and no write access.
 * Data is authoring output, not a validated native gameplay encounter.
 * Uses current Codex PR310 R5RunPlan.Threat source algorithm, including escort.
 */
(function(root) {
"use strict";
const names={arenas:["Złomowisko","Pustkowia","Przemysł"],enemies:["Scout","Bruiser","Gunner","Flanker"],variants:["Normal","EliteBruiser","EliteGunner","SiegeBoss"]};
const numeric={
  Arena:[0,2,true],EnemyCount:[1,3,true],ScrapReward:[0,100000,true],
  RewardJitter:[0,1000,true],HealthMultiplier:[.5,3,false],
  DamageMultiplier:[.1,3,false],FireIntervalMultiplier:[.3,3,false],
  ProjectileSpeedMultiplier:[.5,2,false],MoveSpeed:[0,6,false],
  PreferredDistance:[6,20,false],StrafeStrength:[0,1,false],
  ThreatBudget:[.1,100,false]
};
const keys=new Set(["Id","Title","Arena","Roster","Variants","AlternativeRoster",
"ThreatBudget","Risk","Loot","Rule","RewardJitter","EnemyCount",
"HealthMultiplier","DamageMultiplier","FireIntervalMultiplier",
"ProjectileSpeedMultiplier","MoveSpeed","PreferredDistance","StrafeStrength","ScrapReward"]);
const copy=o=>JSON.parse(JSON.stringify(o));
const clean=s=>typeof s==="string"&&s.trim().length>0;
function numericValue(v,field){
 const config=numeric[field];if(!config)return false;
 return typeof v==="number"&&Number.isFinite(v)&&v>=config[0]&&v<=config[1]&&(!config[2]||Number.isInteger(v));
}
function error(message){throw new Error(message);}
function spawn(choice,roster=choice.Roster) {
 const boss=choice.Variants.includes(3);
 const count=boss?1:Math.min(choice.EnemyCount+1,4);
 return Array.from({length:count},(_,i)=>({
  archetype:i>=choice.EnemyCount?0:roster[i],
  variant:i>=choice.EnemyCount?0:choice.Variants[i],
  isEscort:i>=choice.EnemyCount
 }));
}
function threat(choice,roster=choice.Roster){
 const foes=spawn(choice,roster);
 let total=0;const archetypes=new Set();
 for(const {archetype:k,variant:v} of foes){
  archetypes.add(k);
  const hull=k===1?1.6:k===0?.8:1.1;
  const weapon=k===1||k===2?1.6:1;
  const mobility=k===3?1.4:1;
  const range=k===2?1.3:1;
  const modifier=v===3?3.8:v===0?1:1.65;
  total+=(hull*choice.HealthMultiplier+weapon*choice.DamageMultiplier/Math.max(.3,choice.FireIntervalMultiplier)+mobility*choice.MoveSpeed*.22+range)*modifier;
 }
 return total*(archetypes.size>1?1.12:1)*(choice.Arena===2?1.15:choice.Arena===1?1.05:1);
}
function risk(c,t=threat(c)){
 return c.Variants.includes(3)?"Boss":t<5?"Niskie":t<9?"Umiarkowane":t<12?"Wysokie":"Bardzo wysokie";
}
function validateRoster(c,roster,path,errors){
 if(!Array.isArray(roster)||roster.length<1||roster.length>3||
    !Array.isArray(c.Variants)||roster.length!==c.Variants.length||
    roster.length!==c.EnemyCount){
  errors.push(path+" roster/count/variants mismatch");return;
 }
 for(let i=0;i<roster.length;i++){
  const k=roster[i],v=c.Variants[i];
  if(!Number.isInteger(k)||k<0||k>3)errors.push(path+" invalid enemy enum "+i);
  if(!Number.isInteger(v)||v<0||v>3)errors.push(path+" invalid variant enum "+i);
  if(v===2&&k!==2)errors.push(path+" EliteGunner needs Gunner");
  if((v===1||v===3)&&k!==1)errors.push(path+" Bruiser/Boss needs Bruiser");
 }
}
function inspect(catalog){
 const errors=[],warnings=[],rows=[],ids=new Set();
 if(!catalog||typeof catalog!=="object"||Array.isArray(catalog)||
    Object.keys(catalog).length!==1||!Object.hasOwn(catalog,"Stages")||
    !Array.isArray(catalog.Stages)||catalog.Stages.length!==6){
   return {valid:false,errors:["R5Catalog must be exactly {Stages:[six stages]}"],warnings,rows};
 }
 catalog.Stages.forEach((stage,s)=>{
  const choices=stage?.Options;
  if(!Array.isArray(choices)||choices.length<1||choices.length>2){
   errors.push("Stage "+(s+1)+" must contain 1–2 options");return;
  }
  choices.forEach((c,i)=>{
   const id="S"+(s+1)+"O"+(i+1);
   if(!c||typeof c!=="object"||Array.isArray(c)){errors.push(id+" invalid object");return;}
   const unknown=Object.keys(c).filter(k=>!keys.has(k));
   if(unknown.length)errors.push(id+" unknown serialized field: "+unknown.join(","));
   if(!clean(c.Id)||ids.has(c.Id))errors.push(id+" missing/duplicate Id");
   else ids.add(c.Id);
   if(!clean(c.Title))errors.push(id+" missing Title");
   if(!clean(c.Loot))errors.push(id+" missing Loot");
   if(!clean(c.Rule))errors.push(id+" missing Rule");
   for(const k of Object.keys(numeric))
     if(!numericValue(c[k],k))errors.push(id+" "+k+" invalid range/value");
   validateRoster(c,c.Roster,id,errors);
   if(c.AlternativeRoster!==undefined&&c.AlternativeRoster!==null&&c.AlternativeRoster.length>0)
     validateRoster(c,c.AlternativeRoster,id+" alt",errors);
   if(Number.isInteger(c.RewardJitter)&&Number.isInteger(c.ScrapReward)&&c.RewardJitter>c.ScrapReward)
     errors.push(id+" RewardJitter exceeds ScrapReward");
   if(errors.some(x=>x.startsWith(id)))return;
   const t=threat(c),alt=Array.isArray(c.AlternativeRoster)&&c.AlternativeRoster.length?threat(c,c.AlternativeRoster):t;
   const largest=Math.max(t,alt);
   if(!Number.isFinite(largest)||largest>c.ThreatBudget+1e-5)
      errors.push(id+" threat "+largest.toFixed(3)+" exceeds budget "+c.ThreatBudget);
   if(c.Risk!==risk(c))warnings.push(id+" Risk label will be recalculated by original R5RunPlan");
   rows.push({stage:s+1,option:i+1,id:c.Id,title:c.Title,arena:names.arenas[c.Arena],
      enemyCount:c.EnemyCount,spawnCount:spawn(c).length,
      threat:Number(t.toFixed(3)),threatAlternate:Number(alt.toFixed(3)),
      budget:c.ThreatBudget,risk:risk(c),
      rewardMin:c.ScrapReward-c.RewardJitter,rewardMax:c.ScrapReward+c.RewardJitter});
  });
  if(choices.length===2&&choices.every(c=>c&&Number.isInteger(c.ScrapReward)&&Number.isInteger(c.RewardJitter))){
   const a=choices[0],b=choices[1];
   if(a.ScrapReward+a.RewardJitter>=b.ScrapReward-b.RewardJitter)
     warnings.push("Stage "+(s+1)+" reward ranges overlap (allowed by R5 but weak player choice)");
  }
 });
 return {valid:errors.length===0,errors,warnings,rows};
}
function assertValid(c){const r=inspect(c);if(!r.valid)error(r.errors[0]);return r;}
function baseChoice(index){
 const id="wb17_stage_"+(index+1)+"_a";
 const c={
 Id:id,Title:"Starcie "+(index+1),Arena:index%3,Roster:[0],Variants:[0],
 EnemyCount:1,ThreatBudget:12,Risk:"Niskie",
 Loot:"Standardowy odzysk R4",Rule:"Zniszcz wszystkie rdzenie",
 RewardJitter:0,HealthMultiplier:1,DamageMultiplier:.4,
 FireIntervalMultiplier:1.6,ProjectileSpeedMultiplier:1,
 MoveSpeed:2,PreferredDistance:12,StrafeStrength:.35,
 ScrapReward:80+index*10};
 c.Risk=risk(c);return c;
}
function fresh(){const d={Stages:Array.from({length:6},(_,i)=>({Options:[baseChoice(i)]}))};assertValid(d);return d;}
function put(catalog,stageIndex,choiceIndex,patch,autoBudget=true){
 assertValid(catalog);
 if(!Number.isInteger(stageIndex)||!Number.isInteger(choiceIndex)||
   !catalog.Stages[stageIndex]?.Options?.[choiceIndex])error("unknown stage/option");
 const clone=copy(catalog),c=clone.Stages[stageIndex].Options[choiceIndex];
 if(!patch||typeof patch!=="object"||Object.keys(patch).length===0)error("empty patch");
 for(const [k,v] of Object.entries(patch)){
  if(!keys.has(k)||k==="Risk")error("unsupported writable field "+k);
  if(k==="Roster"||k==="Variants"||k==="AlternativeRoster"){
   if(!Array.isArray(v))error(k+" must be array of enums");c[k]=v.slice();
  }else c[k]=v;
 }
 if(Object.hasOwn(patch,"Roster")&&!Object.hasOwn(patch,"EnemyCount"))
  c.EnemyCount=c.Roster.length;
 if(autoBudget&&!Object.hasOwn(patch,"ThreatBudget")) {
  const t=threat(c),alt=Array.isArray(c.AlternativeRoster)&&c.AlternativeRoster.length?threat(c,c.AlternativeRoster):t;
  if(Number.isFinite(t)&&Number.isFinite(alt))c.ThreatBudget=Math.min(100,Math.max(c.ThreatBudget,Math.ceil(Math.max(t,alt))+2));
 }
 c.Risk=risk(c);
 assertValid(clone);
 return clone;
}
function addChoice(catalog,stage){
 assertValid(catalog);
 if(!Number.isInteger(stage)||!catalog.Stages[stage])error("unknown stage");
 if(catalog.Stages[stage].Options.length>=2)error("R5 stage maximum is 2 options");
 const draft=copy(catalog);
 const first=draft.Stages[stage].Options[0];
 const second=copy(first);
 second.Id="wb17_stage_"+(stage+1)+"_b";
 if(draft.Stages.some(s=>s.Options.some(o=>o.Id===second.Id)))error("generated ID already exists");
 second.Title=first.Title+" — alternatywa";
 second.Arena=(first.Arena+1)%3;
 second.ScrapReward=Math.min(100000,first.ScrapReward+Math.max(20,first.RewardJitter*2+1));
 second.RewardJitter=first.RewardJitter;
 second.Risk=risk(second);
 second.ThreatBudget=Math.min(100,Math.max(second.ThreatBudget,Math.ceil(threat(second))+2));
 draft.Stages[stage].Options.push(second);
 assertValid(draft);return draft;
}
function removeChoice(catalog,stage,choice){
 assertValid(catalog);
 if(!catalog.Stages[stage]?.Options?.[choice]||catalog.Stages[stage].Options.length<=1)error("must retain one choice per stage");
 const draft=copy(catalog);
 draft.Stages[stage].Options.splice(choice,1);
 assertValid(draft);return draft;
}
function parse(text){
 if(typeof text!=="string"||text.length>1000000)error("catalog must be a JSON text under 1MB");
 let d;try{d=JSON.parse(text);}catch(e){error("invalid JSON: "+e.message);}
 assertValid(d);return copy(d);
}
function exportJSON(d){assertValid(d);return JSON.stringify(d,null,2)+"\n";}
root.B01R5Workbench=Object.freeze({fresh,inspect,assertValid,put,addChoice,removeChoice,parse,exportJSON,threat,spawn,risk,names});
if(typeof module!=="undefined"&&module.exports)module.exports=root.B01R5Workbench;
})(typeof globalThis==="object"?globalThis:this);
