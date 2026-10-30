#!/usr/bin/env node
import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import {pathToFileURL} from "node:url";
export const SHA="d94400d95d13db7f27e1533df05c9292d5380431";
export const cases=[
 ["LIGHT_CLEAR","LIGHT","CLEAR"],["LIGHT_OVERLAP","LIGHT","OVERLAP"],
 ["HEAVY_CLEAR","HEAVY","CLEAR"],["HEAVY_OVERLAP","HEAVY","OVERLAP"]
];
const square=[[0,1,3,2],[1,2,0,3],[2,3,1,0],[3,0,2,1]];
const outcomes=["OBSERVED","PRODUCT_BLOCKED","SETUP_BLOCKED","NOT_RUN"];
const fail=(ok,msg)=>{if(!ok)throw Error(msg)};
const shape=(o,k)=>fail(o&&typeof o==="object"&&!Array.isArray(o)&&Object.keys(o).sort().join("|")===k.slice().sort().join("|"),"SCHEMA_KEYS");
export function playerHash(file){
 const st=fs.statSync(file);fail(st.isFile()&&st.size>0&&st.size<=8*1024*1024*1024,"PLAYER_SIZE");
 const fd=fs.openSync(file,"r"),h=crypto.createHash("sha256"),b=Buffer.alloc(65536);
 try{let n;while((n=fs.readSync(fd,b,0,b.length,null))>0)h.update(b.subarray(0,n));}
 finally{fs.closeSync(fd)}return h.digest("hex");
}
export function plan({sha=SHA,hash,sessions,seed=0}){
 fail(sha===SHA,"UNSELECTED_SHA");fail(typeof hash==="string"&&/^[0-9a-f]{64}$/.test(hash),"HASH");
 fail(Number.isInteger(sessions)&&sessions>=4&&sessions<=64&&sessions%4===0,"SESSIONS");
 fail(Number.isInteger(seed)&&seed>=0&&seed<1000000,"SEED");
 const cards=[];
 for(let s=0;s<sessions;s++)for(let n=0;n<4;n++){
  const c=cases[square[(s+seed)%4][n]];
  cards.push({id:"S"+String(s+1).padStart(2,"0")+"-T"+(n+1),condition:c[0],rig:c[1],geometry:c[2],position:n+1});
 }
 return {schema:"B01-13-REATTACH-R1",candidate_sha:sha,player_exe_sha256:hash,player_to_source:"UNVERIFIED",unity_g1:"NOT_RUN",human_trials:"NOT_RUN",mode:"TECHNICAL_ONLY",sessions,seed,cards};
}
export function score(p,obs){
 shape(p,["schema","candidate_sha","player_exe_sha256","player_to_source","unity_g1","human_trials","mode","sessions","seed","cards"]);
 fail(p.schema==="B01-13-REATTACH-R1"&&p.candidate_sha===SHA&&p.player_to_source==="UNVERIFIED"&&p.unity_g1==="NOT_RUN"&&p.human_trials==="NOT_RUN"&&p.mode==="TECHNICAL_ONLY","FORGED_GATE");
 fail(JSON.stringify(p)===JSON.stringify(plan({sha:p.candidate_sha,hash:p.player_exe_sha256,sessions:p.sessions,seed:p.seed})),"TAMPER");
 fail(Array.isArray(obs)&&obs.length===p.cards.length,"INCOMPLETE");
 const byId=new Map(p.cards.map(c=>[c.id,c])),seen=new Set();
 const counts=Object.fromEntries(cases.map(c=>[c[0],{observed:0,product_blocked:0,setup_blocked:0,not_run:0,safety_red:0,feedback_red:0}]));
 for(const row of obs){
  shape(row,["id","outcome","feedback_visible","state_changed"]);
  fail(byId.has(row.id)&&!seen.has(row.id),"DUPLICATE_OR_UNKNOWN");seen.add(row.id);
  fail(outcomes.includes(row.outcome),"OUTCOME");
  const c=byId.get(row.id),s=counts[c.condition],attempted=row.outcome==="OBSERVED"||row.outcome==="PRODUCT_BLOCKED";
  if(attempted){
   fail(typeof row.feedback_visible==="boolean"&&typeof row.state_changed==="boolean","FLAGS");
   if(c.geometry==="OVERLAP"&&row.state_changed)s.safety_red++;
   if(row.outcome==="OBSERVED"&&(!row.feedback_visible||row.state_changed!==(c.geometry==="CLEAR")))s.feedback_red++;
  }else fail(row.feedback_visible===null&&row.state_changed===null,"UNRUN_FLAGS");
  const field={OBSERVED:"observed",PRODUCT_BLOCKED:"product_blocked",SETUP_BLOCKED:"setup_blocked",NOT_RUN:"not_run"}[row.outcome];s[field]++;
 }
 const total=k=>Object.values(counts).reduce((n,x)=>n+x[k],0);
 return {schema:"B01-13-REATTACH-SCORE-R1",candidate_sha:SHA,player_exe_sha256:p.player_exe_sha256,evidence:"OPERATOR_REPORTED",human_verified:0,unity_g1:"NOT_RUN",integrated:false,status:total("safety_red")?"PHYSICAL_SAFETY_RED":total("feedback_red")?"FEEDBACK_RED":"TECHNICAL_ONLY",counts};
}
export function writePrivate(file,obj){
 fail(path.isAbsolute(file),"ABSOLUTE_OUTPUT");
 let cur=fs.realpathSync(path.dirname(file));
 for(;;){fail(!fs.existsSync(path.join(cur,".git")),"IN_GIT");const up=path.dirname(cur);if(up===cur)break;cur=up;}
 const fd=fs.openSync(file,fs.constants.O_CREAT|fs.constants.O_WRONLY|fs.constants.O_EXCL|(fs.constants.O_NOFOLLOW||0),0o600);
 try{fs.writeFileSync(fd,JSON.stringify(obj,null,2)+"\n");fs.fsyncSync(fd);}finally{fs.closeSync(fd);}
}
export function cli(argv){
 const [mode,...rest]=argv,a={};fail(rest.length%2===0,"ARGUMENTS");
 for(let i=0;i<rest.length;i+=2){fail(rest[i].startsWith("--")&&!Object.hasOwn(a,rest[i].slice(2)),"ARGUMENTS");a[rest[i].slice(2)]=rest[i+1];}
 if(mode==="prepare"){shape(a,["player","output","sessions","seed"]);const v=plan({hash:playerHash(a.player),sessions:Number(a.sessions),seed:Number(a.seed)});writePrivate(a.output,v);return {cards:v.cards.length};}
 if(mode==="score"){shape(a,["plan","observations","output"]);const v=score(JSON.parse(fs.readFileSync(a.plan)),JSON.parse(fs.readFileSync(a.observations)));writePrivate(a.output,v);return {status:v.status};}
 throw Error("UNKNOWN_COMMAND");
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
 try{console.log(JSON.stringify(cli(process.argv.slice(2))))}catch(e){console.error("FAIL_CLOSED",e.message);process.exitCode=2;}
}
