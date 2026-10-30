#!/usr/bin/env node
// B01-13 W22: bounded recovery-route planner for human playtest preparations.
// Deliberately synthetic: no canonical Save, Unity, actual participants or authority.
import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import {pathToFileURL} from "node:url";
export const PACKAGE="B01-13-SOFTLOCK-RECOVERY-ROUTES-001/R1";
export const PINS=Object.freeze({game:"d94400d95d13db7f27e1533df05c9292d5380431",save:"33094f154c7b91119b36d58aab285e6e83c5de57",host:"efb2b06eafedab5469b57f163152d96aca3c5915",r4:"09c1809b73324a6d31c13e5d0143de62ac1e3ca6",econ:"12572117a3dce369257feabc57021cde58023369"});
export const PRESETS=Object.freeze({
 LIGHT:Object.freeze([{id:"L-CORE",essential:true,status:"READY"},{id:"L-DRIVE",essential:true,status:"READY"},{id:"L-GUN",essential:false,status:"DESTROYED"},{id:"L-ARMOR",essential:false,status:"DESTROYED"},{id:"L-BATTERY",essential:false,status:"DESTROYED"}]),
 HEAVY:Object.freeze([{id:"H-CORE",essential:true,status:"READY"},{id:"H-DRIVE",essential:true,status:"DESTROYED"},{id:"H-GUN",essential:false,status:"DESTROYED"},{id:"H-ARMOR",essential:false,status:"DESTROYED"},{id:"H-SUPPORT",essential:false,status:"READY"}])
});
export const CADENCES=Object.freeze(["PER_VICTORY","PER_RUN"]);
export const POLICIES=Object.freeze(["STRICT","ONE_UNAPPROVED_LOANER"]);
export const GOALS=Object.freeze(["FULL_RESTORE","CAN_DEPLOY"]);
const assert=(ok,why)=>{if(!ok)throw Error(why)};
const copy=x=>JSON.parse(JSON.stringify(x));
export function make(vehicle="HEAVY",cadence="PER_VICTORY",policy="STRICT",scrap=100,quote=70){
 assert(Object.hasOwn(PRESETS,vehicle),"INVALID_VEHICLE");
 assert(CADENCES.includes(cadence)&&POLICIES.includes(policy),"INVALID_POLICY");
 assert(Number.isInteger(scrap)&&scrap>=0&&scrap<=500,"INVALID_SCRAP");
 assert(Number.isInteger(quote)&&quote>=1&&quote<=150,"INVALID_QUOTE");
 return{vehicle,cadence,policy,scrap,quote,freeUsed:false,loanerAvailable:true,wins:0,seen:[],parts:PRESETS[vehicle].map(p=>({...p}))};
}
export function validate(s){
 assert(s&&Object.hasOwn(PRESETS,s.vehicle)&&CADENCES.includes(s.cadence)&&POLICIES.includes(s.policy),"STATE_POLICY");
 assert(Number.isInteger(s.scrap)&&s.scrap>=0&&s.scrap<=500&&Number.isInteger(s.quote)&&s.quote>=1&&s.quote<=150,"STATE_MONEY");
 assert(Number.isInteger(s.wins)&&s.wins>=0&&s.wins<=4&&typeof s.freeUsed==="boolean"&&typeof s.loanerAvailable==="boolean","STATE_FLAGS");
 assert(Array.isArray(s.seen)&&new Set(s.seen).size===s.seen.length&&s.seen.every(id=>/^DEMO-[0-9]{3}$/.test(id))&&s.seen.length<=8,"STATE_IDS");
 assert(s.seen.length>=s.wins,"MISSING_WIN_ID");
 const base=PRESETS[s.vehicle];
 assert(Array.isArray(s.parts)&&s.parts.length===base.length,"STATE_PARTS");
 for(let i=0;i<base.length;i++)assert(s.parts[i].id===base[i].id&&s.parts[i].essential===base[i].essential&&["READY","DESTROYED","PENDING"].includes(s.parts[i].status),"STATE_OWNERSHIP");
 return s;
}
export function canDeploy(s){return s.parts.every(p=>!p.essential||p.status==="READY")}
export function achieved(s,goal="FULL_RESTORE"){
 assert(GOALS.includes(goal),"INVALID_GOAL");
 return goal==="CAN_DEPLOY"?canDeploy(s):s.parts.every(p=>p.status==="READY");
}
export function apply(s,action){
 validate(s);
 assert(action&&["QUEUE","WIN","LOSS","LOANER_WIN","LOANER_LOSS"].includes(action.type),"INVALID_ACTION");
 const next=copy(s),fail=reason=>({ok:false,reason,state:s});
 if(action.type==="QUEUE"){
  const p=next.parts.find(x=>x.id===action.part);
  if(!p)return fail("UNKNOWN_PART");
  if(p.status!=="DESTROYED")return fail("ALREADY_QUEUED_OR_READY");
  if(next.freeUsed&&next.scrap<next.quote)return fail("INSUFFICIENT_SCRAP");
  const cost=next.freeUsed?next.quote:0;
  if(!next.freeUsed)next.freeUsed=true;else next.scrap-=next.quote;
  p.status="PENDING";
  return{ok:true,state:validate(next),reason:cost?"PAID":"FREE",cost};
 }
 const id=Object.hasOwn(action,"eventId")?action.eventId:("DEMO-"+String(next.seen.length+1).padStart(3,"0"));
 if(typeof id!=="string"||!/^DEMO-[0-9]{3}$/.test(id))return fail("INVALID_DEMO_ID");
 if(next.seen.includes(id))return fail("LOCAL_REPLAY_DENIED");
 const loaner=action.type.startsWith("LOANER");
 if(loaner){
  if(next.policy!=="ONE_UNAPPROVED_LOANER")return fail("UNAPPROVED_OPTION_NOT_SELECTED");
  if(canDeploy(next))return fail("LOANER_NOT_NEEDED");
  if(!next.loanerAvailable)return fail("LOANER_USED");
  if(!next.parts.some(p=>p.essential&&p.status==="PENDING"))return fail("ESSENTIAL_REBUILD_NOT_QUEUED");
  next.loanerAvailable=false;
 }else if(!canDeploy(next))return fail("ESSENTIAL_0HP_BLOCKS_START");
 next.seen.push(id);
 if(action.type==="LOSS"||action.type==="LOANER_LOSS")return{ok:true,state:validate(next),reason:"LOSS_NO_COMPLETION",cost:0};
 next.wins++;
 for(const p of next.parts)if(p.status==="PENDING")p.status="READY";
 if(next.cadence==="PER_VICTORY")next.freeUsed=false;
 return{ok:true,state:validate(next),reason:loaner?"UNAPPROVED_HYPOTHETICAL_WIN":"SIMULATED_WIN",cost:0};
}
function key(s){return[s.scrap,s.freeUsed,s.loanerAvailable,s.wins,s.parts.map(p=>p.status[0]).join("")].join("|")}
export function route(start,goal="FULL_RESTORE",maxActions=9,maxVictories=3){
 validate(start);assert(GOALS.includes(goal),"INVALID_GOAL");
 assert(Number.isInteger(maxActions)&&maxActions>=0&&maxActions<=12&&Number.isInteger(maxVictories)&&maxVictories>=0&&maxVictories<=4,"LIMIT");
 const queue=[{s:copy(start),steps:[]}],visited=new Set([key(start)]),denied=new Set();
 for(let n=0;n<queue.length;n++){
  const item=queue[n];
  if(achieved(item.s,goal))return{status:"ROUTE_FOUND",goal,steps:item.steps,expanded:n+1,examined:visited.size,warning:"MODEL_ONLY_NOT_NATIVE"};
  if(item.steps.length>=maxActions)continue;
  const acts=item.s.parts.filter(p=>p.status==="DESTROYED").map(p=>({type:"QUEUE",part:p.id}));
  if(item.s.parts.some(p=>p.status==="PENDING")&&item.s.wins<maxVictories){
   acts.push({type:"WIN"});
   if(!canDeploy(item.s)&&item.s.policy==="ONE_UNAPPROVED_LOANER")acts.push({type:"LOANER_WIN"});
  }
  for(const a of acts){
   const out=apply(item.s,a);
   if(!out.ok){denied.add(out.reason);continue}
   const k=key(out.state);
   if(visited.has(k))continue;
   visited.add(k);
   queue.push({s:out.state,steps:[...item.steps,{...a,reason:out.reason,cost:out.cost}]});
  }
 }
 const reason=!canDeploy(start)&&start.policy==="STRICT"?"ESSENTIAL_0HP_BLOCKS_START":denied.has("INSUFFICIENT_SCRAP")?"INSUFFICIENT_SCRAP":denied.has("LOANER_USED")?"LOANER_EXHAUSTED":"NO_ROUTE_WITHIN_MODEL_LIMIT";
 return{status:"HOLD_WITHIN_MODEL",goal,reason,steps:[],expanded:queue.length,examined:visited.size,warning:"NOT_A_PROVEN_LIVE_GAME_SOFTLOCK"};
}
export function matrix(scrap=100,quote=70){
 const rows=[];
 for(const vehicle of ["LIGHT","HEAVY"])for(const cadence of CADENCES)for(const policy of POLICIES){
  const s=make(vehicle,cadence,policy,scrap,quote);
  rows.push({vehicle,cadence,policy,full:route(s,"FULL_RESTORE"),deploy:route(s,"CAN_DEPLOY")});
 }
 return rows;
}
function textStep(a,parts){
 if(a.type==="QUEUE")return "Zleć odbudowę "+a.part+" ("+(a.cost?String(a.cost)+" złomu":"darmowy przydział")+"); 0 HP pozostaje niedostępne do następnego zwycięstwa.";
 if(a.type==="WIN")return "Symulowane zwycięstwo tylko przy gotowości pojazdu; ukończenie wcześniejszych zleceń.";
 return "HIPOTEZA NIEZATWIERDZONA: jednorazowy pojazd zastępczy wygrywa; ukończenie wcześniejszych zleceń.";
}
export function markdown(rows){
 assert(Array.isArray(rows)&&rows.length===8,"MATRIX");
 const h=["# B01-13 — W22: ścieżki odzyskania pojazdu (hipotezy)","","**Nie są to wyniki gry:** 0 graczy; Unity/PhysX NOT_RUN; Save/Load NOT_VERIFIED; nie ma zweryfikowanych zwycięstw ani trwałej ochrony ID. Jednorazowy pojazd zastępczy NIEZATWIERDZONY. Zdarzenia DEMO-xxx są wyłącznie lokalnymi znacznikami; nie są uwierzytelnionymi ID zwycięstwa. Brak ścieżki oznacza HOLD w skończonym modelu, nie dowód rzeczywistego softlocku.","","| Robot | Przydział | Ratunek | Pełna odbudowa | Gotowość do walki |","|---|---|---|---|---|"];
 for(const r of rows)h.push("| "+[r.vehicle,r.cadence,r.policy,r.full.status==="ROUTE_FOUND"?r.full.steps.length+" kroków":"HOLD",r.deploy.status==="ROUTE_FOUND"?r.deploy.steps.length+" kroków":"HOLD"].join(" | ")+" |");
 for(const r of rows){
  h.push("","## "+r.vehicle+" / "+r.cadence+" / "+r.policy,"","**Pełna odbudowa:** "+(r.full.status==="ROUTE_FOUND"?"ścieżka w modelu":"HOLD — "+r.full.reason));
  if(r.full.steps.length)for(let i=0;i<r.full.steps.length;i++)h.push(String(i+1)+". "+textStep(r.full.steps[i]));
  else h.push("W tym wariancie brak wymaganych działań albo trasy; sprawdź cel i blokadę.");
  h.push("","**Gotowość:** "+r.deploy.status+(r.deploy.reason?" — "+r.deploy.reason:""));
 }
 h.push("","## Bramka badań","0 prawdziwych sesji. Potrzebne: za zgodą gracza realny build, faktyczne HP/CanDeploy, czas zrozumienia zlecenia, pomyłki w rozróżnianiu darmowej/płatnej odbudowy, poczucie softlocku; porównać warianty bez zgadywania polityki awaryjnej.","","**Status:** IMPLEMENTED=SOURCE_ONLY; G0=PENDING; G1=NOT_RUN; G2=HOLD; CODEX_ACK=NONE.");
 return h.join("\n")+"\n";
}
export function write(file,format="md",scrap=100,quote=70){
 assert(path.isAbsolute(file),"OUTPUT_ABSOLUTE");assert(["md","json"].includes(format),"FORMAT");
 for(let d=fs.realpathSync(path.dirname(file));;d=path.dirname(d)){assert(!fs.existsSync(path.join(d,".git")),"GIT_OUTPUT_DENIED");if(path.dirname(d)===d)break}
 const rows=matrix(scrap,quote);
 const content=format==="md"?markdown(rows):JSON.stringify({package:PACKAGE,assumptions:{scrap,quote,real_human_sessions:0,native_unity:"NOT_RUN",live_econ:"NOT_VERIFIED",loaner:"UNAPPROVED",confidence:"BOUNDED_HYPOTHESIS"},rows},null,2)+"\n";
 const fd=fs.openSync(file,fs.constants.O_CREAT|fs.constants.O_EXCL|fs.constants.O_WRONLY|(fs.constants.O_NOFOLLOW||0),0o600);
 try{fs.writeFileSync(fd,content,"utf8");fs.fsyncSync(fd);return{file,format,bytes:Buffer.byteLength(content),sha256:crypto.createHash("sha256").update(content).digest("hex"),rows:rows.length,real_participants:0,native:"NOT_RUN"}}finally{fs.closeSync(fd)}
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
 try{
  assert(process.argv.length===5&&process.argv[2]==="--format"&&["md","json"].includes(process.argv[3]),"ARGS");
  console.log(JSON.stringify(write(process.argv[4],process.argv[3])));
 }catch(e){console.error("FAIL_CLOSED",String(e.message||e));process.exitCode=2}
}
