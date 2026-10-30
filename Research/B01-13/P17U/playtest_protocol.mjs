#!/usr/bin/env node
// B01-13 W23 — real operator-ready playtest protocol built on frozen W22 source.
// Synthetic fixtures ONLY; never an authenticated deploy/ECON authorization.
import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import {pathToFileURL} from "node:url";
import {make,route,apply,achieved,canDeploy,PINS} from "../P17T/recovery_routes.mjs";

export const PACKAGE="B01-13-ECON-SAFETY-PLAYTEST-PROTOCOL-001/R1";
export const PARENT="5eb72088015062962ba8e9ac60ae684c8fba4bdb";
export const SOURCE_BLOB="d40993ae55940477a8086a403dede831d6419d62";
export const CONDITIONS=Object.freeze([
 Object.freeze({id:"CORE_OK_NEXT_YES",coreAlive:true,nextChallengeAvailable:true}),
 Object.freeze({id:"CORE_LOST_NEXT_YES",coreAlive:false,nextChallengeAvailable:true}),
 Object.freeze({id:"CORE_OK_NEXT_NO",coreAlive:true,nextChallengeAvailable:false}),
 Object.freeze({id:"CORE_LOST_NEXT_NO",coreAlive:false,nextChallengeAvailable:false})
]);
export const VEHICLES=Object.freeze(["LIGHT","HEAVY"]);
export const CADENCES=Object.freeze(["PER_VICTORY","PER_RUN"]);
export const POLICIES=Object.freeze(["STRICT","ONE_UNAPPROVED_LOANER"]);
const guard=(x,m)=>{if(!x)throw Error(m)};
const clone=x=>JSON.parse(JSON.stringify(x));
export function validateCase(c){
 guard(c&&typeof c==="object"&&Object.keys(c).sort().join("|")==="cadence|condition|policy|vehicle","CASE_FIELDS");
 guard(VEHICLES.includes(c.vehicle)&&CADENCES.includes(c.cadence)&&POLICIES.includes(c.policy),"CASE_FACTOR");
 guard(CONDITIONS.some(x=>x.id===c.condition),"CASE_CONDITION");
 return c;
}
export function evaluate(c){
 validateCase(c);
 const conditions=CONDITIONS.find(x=>x.id===c.condition);
 const base=make(c.vehicle,c.cadence,c.policy,100,70);
 const normalStart=conditions.coreAlive&&conditions.nextChallengeAvailable&&canDeploy(base);
 const expectedStart=!conditions.coreAlive?"CORE_LOST":!conditions.nextChallengeAvailable?"NO_NEXT_CHALLENGE":canDeploy(base)?"CAN_START_NOW":"ESSENTIAL_0HP";
 let modeled=null,routeStatus,steps=[];
 if(!conditions.coreAlive){routeStatus="HOLD_CORE_LOST"}
 else if(!conditions.nextChallengeAvailable){routeStatus="HOLD_NO_NEXT_CHALLENGE"}
 else {
  const result=route(base,"FULL_RESTORE");
  if(result.status!=="ROUTE_FOUND")routeStatus="HOLD_WITHIN_BOUNDED_MODEL";
  else{
   let current=clone(base);
   for(const step of result.steps){
    const verified=apply(current,step);
    guard(verified.ok===true,"BASE_ROUTE_UNREPLAYABLE");
    current=verified.state;
   }
   guard(achieved(current,"FULL_RESTORE"),"BASE_ROUTE_GOAL_MISMATCH");
   const loaner=result.steps.some(x=>x.type==="LOANER_WIN");
   routeStatus=loaner?"HYPOTHETICAL_ONLY_UNAPPROVED":"ROUTE_IN_MODEL_ONLY";
   steps=result.steps.map(x=>({type:x.type,part:x.part??null,cost:x.cost??0}));
   modeled={stepCount:steps.length,wins:current.wins,cashRemaining:current.scrap,requiresUnapprovedLoaner:loaner};
  }
 }
 const recoveryExpected=routeStatus==="HYPOTHETICAL_ONLY_UNAPPROVED"?"ONLY_UNAPPROVED_HYPOTHESIS":routeStatus==="ROUTE_IN_MODEL_ONLY"?"MODEL_PATH_NOT_GAME_PROOF":"NO_VERIFIED_PATH";
 const id=[c.vehicle==="LIGHT"?"L":"H",c.cadence==="PER_VICTORY"?"V":"R",c.policy==="STRICT"?"S":"U",conditions.id].join("-");
 return{id,case:c,assumptions:{initialScrap:100,paidQuote:70,coreAlive:conditions.coreAlive,nextChallengeAvailable:conditions.nextChallengeAvailable,originalSourceSha:PARENT,sourceGitBlob:SOURCE_BLOB},objective:"FULL_RESTORE",expectedStart,normalStart,routeStatus,recoveryExpected,steps,modeled};
}
export function allCases(){
 const out=[];
 for(const v of VEHICLES)for(const cadence of CADENCES)for(const policy of POLICIES)for(const condition of CONDITIONS)out.push(evaluate({vehicle:v,cadence,policy,condition:condition.id}));
 guard(out.length===32&&new Set(out.map(x=>x.id)).size===32,"CASE_MATRIX");
 return out;
}
export function assignment(cohort){
 guard(Number.isInteger(cohort)&&cohort>=0&&cohort<=3,"COHORT");
 const rows=[],all=allCases();
 let index=0;
 for(const vehicle of VEHICLES)for(const cadence of CADENCES)for(const policy of POLICIES){
  const con=CONDITIONS[(index+cohort)%4].id;
  const item=all.find(x=>x.case.vehicle===vehicle&&x.case.cadence===cadence&&x.case.policy===policy&&x.case.condition===con);
  guard(Boolean(item),"MISSING_CASE");
  rows.push(item);index++;
 }
 // A 4-position fixed rotation and alternating reversal; no randomness or PII.
 const rotate=(cohort*2)%rows.length;
 const ordered=rows.slice(rotate).concat(rows.slice(0,rotate));
 if(cohort%2)ordered.reverse();
 guard(ordered.length===8&&new Set(ordered.map(x=>x.id)).size===8,"COHORT_CASES");
 return ordered;
}
function csvCell(v){const s=String(v);guard(!/[\r\n]/.test(s),"CSV_NEWLINE");return '"'+s.replace(/"/g,'""')+'"'}
export function responseTemplate(cohort){
 const rows=assignment(cohort),cols=["cohort","case_id","normal_start_choice","recovery_choice","confidence_1_to_5","clarity_1_to_5","notes_anonymized"];
 const lines=[cols.map(csvCell).join(",")];
 for(const item of rows)lines.push([String(cohort),item.id,"","","","",""].map(csvCell).join(","));
 return lines.join("\n")+"\n";
}
export function protocol(cohort){
 const rows=assignment(cohort);
 const out=[
 "# B01-13 W23 — scenariusze badań napraw i softlocków",
 "",
 "**DOKUMENT DLA PROWADZĄCEGO.** Nie są to wyniki badania: liczba prawdziwych uczestników 0. Case'y pochodzą z modelu #722 i syntetycznych bramek; Unity, CanDeploy i Save/Load NOT_RUN/NOT_VERIFIED. Nie pokazuj klucza ocen przed zadaniem pytania.",
 "",
 "## Procedura i etyka",
 "",
 "Zanim zaczniesz: uzyskaj dobrowolną, świadomą zgodę, ustal minimalny czas badania i możliwość przerwania. Nie zbieraj imienia, głosu, identyfikatora konta, IP ani danych zdrowotnych. Formularz CSV pozostaje lokalny; nie wysyłaj go automatycznie. Podawaj uczestnikowi tylko fikcyjne opisy przypadków, nie łącz ćwiczenia z rzeczywistymi zapisami gry.",
 "",
 "Pokaż 8 kart w podanej kolejności. Dla każdej zapytaj: (1) czy można rozpocząć NORMALNE następne wyzwanie? (2) czy istnieje bezpieczna droga do pełnej odbudowy w opisanych zasadach, czy to tylko hipoteza? (3) pewność 1–5, czytelność 1–5. Bez podpowiedzi o oczekiwanym kluczu. Porównuj wyniki między warunkami dopiero po zebraniu rzeczywistych zgód i odpowiedzi.",
 "",
 "Założenia: uszkodzenia 0 HP czekają na NASTĘPNE odrębne zwycięstwo; bez zwycięstwa naprawa nie kończy się. Przykładowe 100 złomu i koszt 70 NIE są oficjalnym balansem. Jednorazowy pojazd zastępczy jest propozycją NIEZATWIERDZONĄ, a nie realną funkcją. Brak ścieżki w ograniczonym modelu nie dowodzi prawdziwego softlocku.",
 "",
 "## Karty dla uczestnika (klucz osobno niżej)",
 ""
 ];
 rows.forEach((x,i)=>{
  const core=x.assumptions.coreAlive?"Core zachowany":"Core zniszczony";
  const next=x.assumptions.nextChallengeAvailable?"kolejne wyzwanie dostępne":"brak następnego dostępnego wyzwania";
  out.push("### Zadanie "+(i+1)+" — "+x.id,"","Robot: "+x.case.vehicle+"; darmowa odbudowa: "+(x.case.cadence==="PER_VICTORY"?"jedna na zwycięstwo":"jedna na run")+"; zasada awaryjna: "+(x.case.policy==="STRICT"?"brak pomocy":"hipotetyczny jednorazowy zastępczy (NIEZATWIERDZONY)")+". Stan: "+core+", "+next+".","", "Odpowiedz: normalny start TAK/NIE i dlaczego? Pełna odbudowa: DROGA / TYLKO HIPOTEZA / BRAK POTWIERDZONEJ DROGI? Oceń pewność i czytelność 1–5.","");
 });
 out.push("---","","## KLUCZ OCENY — WYŁĄCZNIE DLA OBSERWATORA","");
 for(const [i,x] of rows.entries()){
  out.push("**"+(i+1)+". "+x.id+"** — NORMAL_START="+x.expectedStart+"; RECOVERY="+x.recoveryExpected+"; ROUTE="+x.routeStatus+(x.steps.length?"; kroki="+x.steps.map(s=>s.type+(s.part?"("+s.part+")":"")).join(" → "):""));
 }
 out.push("","## Granice wnioskowania","","Brak prawdziwych wyników ludzkich, czasu wykonania i problemów UX. Wynik modelu nie autoryzuje zwycięstwa ani naprawy, nie sprawdza polityki właściciela #154. Decyzje o ekonomii, pożyczonym pojeździe, Core i mobilności są do potwierdzenia na prawdziwym buildzie z aktywowanym Unity G1, testami B01-60, a nie w tej paczce.","","SOURCE_G0=PENDING · NATIVE_G1=NOT_RUN · LIVE_ECON=NOT_WIRED · CODEX_ACK=NONE · G2=HOLD.","");
 return out.join("\n");
}
export function machineReceipt(cohort){
 return {package:PACKAGE,parentOriginalCommit:PARENT,parentSourceBlob:SOURCE_BLOB,frozenSourceReferences:PINS,cohort,protocolKind:"SYNTHETIC_STIMULI_AND_BLANK_RESPONSES",actualHumanSessions:0,realNativeUnity:"NOT_RUN",realGameplayECON:"NOT_VERIFIED",loanerApproval:"NOT_APPROVED",cases:assignment(cohort)};
}
export function generate(dir,cohort){
 guard(path.isAbsolute(dir),"DIR_ABSOLUTE");
 guard(Number.isInteger(cohort)&&cohort>=0&&cohort<4,"COHORT");
 const target=fs.realpathSync(dir);
 for(let d=target;;d=path.dirname(d)){guard(!fs.existsSync(path.join(d,".git")),"GIT_OUTPUT_DENIED");if(path.dirname(d)===d)break}
 const spec=[
 ["w23-cohort-"+cohort+"-protocol.md",protocol(cohort)],
 ["w23-cohort-"+cohort+"-responses.csv",responseTemplate(cohort)],
 ["w23-cohort-"+cohort+"-key.json",JSON.stringify(machineReceipt(cohort),null,2)+"\n"]
 ];
 // Fail closed before writing if any intended destination exists.
 for(const [name] of spec)guard(!fs.existsSync(path.join(target,name)),"OUTPUT_EXISTS");
 const output=[];
 try{
  for(const [name,content] of spec){
   const filename=path.join(target,name);
   const fd=fs.openSync(filename,fs.constants.O_CREAT|fs.constants.O_EXCL|fs.constants.O_WRONLY|(fs.constants.O_NOFOLLOW||0),0o600);
   try{fs.writeFileSync(fd,content,"utf8");fs.fsyncSync(fd)}finally{fs.closeSync(fd)}
   output.push({name,bytes:Buffer.byteLength(content),sha256:crypto.createHash("sha256").update(content).digest("hex")});
  }
  return{package:PACKAGE,cohort,originalGitBlob:SOURCE_BLOB,files:output,actualHumanSessions:0,nativeUnity:"NOT_RUN"};
 }catch(err){
  // Never delete unknown/foreign files or data after a partial failure.
  throw err;
 }
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
 try{
  guard(process.argv.length===5&&process.argv[2]==="--cohort"&&/^[0-3]$/.test(process.argv[3]),"ARGS");
  console.log(JSON.stringify(generate(process.argv[4],Number(process.argv[3]))));
 }catch(err){console.error("FAIL_CLOSED",String(err.message||err));process.exitCode=2}
}
