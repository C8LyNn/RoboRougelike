#!/usr/bin/env node
// Original B01-13 offline playtest-timeline metrics, no Unity or human attestation.
import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import {pathToFileURL} from "node:url";
export const GAME="d94400d95d13db7f27e1533df05c9292d5380431";
export const SAVE="33094f154c7b91119b36d58aab285e6e83c5de57";
const TYPES=["DETACHED_AT_SAVE","REATTACHED_AT_SAVE","DOCK_DENIED"];
const EVENTS=["CUE","NOTICE","ACTION","FEEDBACK","HELP","END"];
function check(x,e){if(!x)throw Error(e)}
function exact(o,keys){check(o&&typeof o==="object"&&!Array.isArray(o)&&Object.keys(o).sort().join("|")===keys.slice().sort().join("|"),"SCHEMA")}
function sha256(bytes){return crypto.createHash("sha256").update(bytes).digest("hex")}
export function hashPlayer(file){
 const st=fs.lstatSync(file);
 check(st.isFile()&&!st.isSymbolicLink()&&st.size>0&&st.size<=8*1024**3,"PLAYER_FILE");
 const fd=fs.openSync(file,"r"),h=crypto.createHash("sha256"),buf=Buffer.alloc(65536);
 try{let n;while((n=fs.readSync(fd,buf,0,buf.length,null))>0)h.update(buf.subarray(0,n));}
 finally{fs.closeSync(fd)}return h.digest("hex");
}
export function blank(trial_id,scenario,player_sha256){
 check(/^T[0-9]{4}$/.test(trial_id),"TRIAL_ID");
 check(TYPES.includes(scenario),"SCENARIO");
 check(typeof player_sha256==="string"&&/^[a-f0-9]{64}$/.test(player_sha256),"PLAYER_SHA");
 return {schema:"B01-13-RECOVERY-TRIAL/R1",game_sha:GAME,save_sha:SAVE,player_sha256,source_to_player:"UNVERIFIED",unity_g1:"NOT_RUN",human_verified:0,trial_id,scenario,filled:false,events:[]};
}
export function score(t,target_ms=30000){
 exact(t,["schema","game_sha","save_sha","player_sha256","source_to_player","unity_g1","human_verified","trial_id","scenario","filled","events"]);
 check(t.schema==="B01-13-RECOVERY-TRIAL/R1"&&t.game_sha===GAME&&t.save_sha===SAVE,"SOURCE_CONTRACT");
 check(typeof t.player_sha256==="string"&&/^[a-f0-9]{64}$/.test(t.player_sha256),"PLAYER_SHA");
 check(t.source_to_player==="UNVERIFIED"&&t.unity_g1==="NOT_RUN"&&t.human_verified===0,"FORGED_GATE");
 check(/^T[0-9]{4}$/.test(t.trial_id)&&TYPES.includes(t.scenario),"TRIAL_ID_SCENARIO");
 check(t.filled===true,"NOT_OBSERVED");check(Number.isInteger(target_ms)&&target_ms>=1000&&target_ms<=300000,"TARGET");
 check(Array.isArray(t.events)&&t.events.length>=2&&t.events.length<=128,"EVENT_COUNT");
 let firstNotice=null,firstAction=null,actions=0,help=0,clear=0,ambiguous=0,absent=0,end=null,prev=-1,cue=0;
 for(let i=0;i<t.events.length;i++){
  const e=t.events[i];exact(e,["at_ms","event","value"]);
  check(Number.isInteger(e.at_ms)&&e.at_ms>=0&&e.at_ms<=600000&&e.at_ms>=prev,"EVENT_TIME");prev=e.at_ms;
  check(EVENTS.includes(e.event),"EVENT_TYPE");
  if(e.event==="CUE"){check(i===0&&e.at_ms===0&&e.value==="NONE","CUE_FIRST");cue++;}
  else if(e.event==="NOTICE"){check(firstNotice===null&&e.value==="NONE","NOTICE_ONCE");firstNotice=e.at_ms;}
  else if(e.event==="ACTION"){check(e.value==="NONE","ACTION_VALUE");actions++;if(firstAction===null)firstAction=e.at_ms;}
  else if(e.event==="FEEDBACK"){
   check(actions>0&&["CLEAR","AMBIGUOUS","ABSENT"].includes(e.value),"FEEDBACK_AFTER_ACTION");
   if(e.value==="CLEAR")clear++;if(e.value==="AMBIGUOUS")ambiguous++;if(e.value==="ABSENT")absent++;
  }else if(e.event==="HELP"){check(e.value==="NONE","HELP_VALUE");help++;}
  else if(e.event==="END"){
   check(i===t.events.length-1&&["RECOVERED","ABANDONED","STOPPED"].includes(e.value)&&e.at_ms>0,"END_LAST");
   end={status:e.value,at_ms:e.at_ms};
  }
  if(i>0)check(e.event!=="CUE","DUPLICATE_CUE");
 }
 check(cue===1&&end!==null,"COMPLETE_TIMELINE");
 if(end.status==="RECOVERED")check(actions>0&&clear+ambiguous+absent>0,"RECOVERY_NEEDS_ATTEMPT_FEEDBACK");
 const independent=end.status==="RECOVERED"&&help===0&&end.at_ms<=target_ms;
 return {schema:"B01-13-RECOVERY-RESULT/R1",game_sha:GAME,save_sha:SAVE,trial_id:t.trial_id,scenario:t.scenario,player_sha256:t.player_sha256,evidence:"OPERATOR_TRANSCRIBED_NOT_ATTESTED",source_to_player:"UNVERIFIED",unity_g1:"NOT_RUN",human_verified:0,save_contract_verified:false,completion:end.status,completion_ms:end.at_ms,notice_ms:firstNotice,first_action_ms:firstAction,attempts:actions,help_events:help,feedback:{clear,ambiguous,absent},independent_within_target:independent};
}
function median(a){
 if(a.length===0)return null;
 const s=[...a].sort((x,y)=>x-y),m=Math.floor(s.length/2);
 return s.length%2?s[m]:(s[m-1]+s[m])/2;
}
export function batch(document,target_ms=30000){
 exact(document,["schema","trials"]);
 check(document.schema==="B01-13-RECOVERY-BATCH/R1"&&Array.isArray(document.trials)&&document.trials.length>=1&&document.trials.length<=128,"BATCH_SCHEMA");
 const results=document.trials.map(x=>score(x,target_ms)),ids=new Set(),hashes=new Set();
 for(const x of results){check(!ids.has(x.trial_id),"DUPLICATE_TRIAL");ids.add(x.trial_id);hashes.add(x.player_sha256);}
 check(hashes.size===1,"MIXED_PLAYER_BINARIES");
 const summarize=a=>({count:a.length,recovered:a.filter(x=>x.completion==="RECOVERED").length,abandoned:a.filter(x=>x.completion==="ABANDONED").length,stopped:a.filter(x=>x.completion==="STOPPED").length,independent_within_target:a.filter(x=>x.independent_within_target).length,median_completion_ms:median(a.filter(x=>x.completion==="RECOVERED").map(x=>x.completion_ms)),median_notice_ms:median(a.filter(x=>x.notice_ms!==null).map(x=>x.notice_ms)),total_attempts:a.reduce((n,x)=>n+x.attempts,0),total_help_events:a.reduce((n,x)=>n+x.help_events,0),ambiguous_feedback_events:a.reduce((n,x)=>n+x.feedback.ambiguous,0),absent_feedback_events:a.reduce((n,x)=>n+x.feedback.absent,0)});
 return {schema:"B01-13-RECOVERY-BATCH-RESULT/R1",game_sha:GAME,save_sha:SAVE,source_to_player:"UNVERIFIED",unity_g1:"NOT_RUN",human_verified:0,save_contract_verified:false,player_sha256:results[0].player_sha256,target_ms,aggregate:summarize(results),scenarios:Object.fromEntries(TYPES.map(k=>[k,summarize(results.filter(x=>x.scenario===k))])),trials:results};
}
function safeRead(file){
 check(path.isAbsolute(file),"INPUT_ABSOLUTE");
 const st=fs.lstatSync(file);check(st.isFile()&&!st.isSymbolicLink()&&st.size>0&&st.size<=512*1024,"INPUT_FILE");
 return JSON.parse(fs.readFileSync(file,"utf8"));
}
function safeWrite(file,obj){
 check(path.isAbsolute(file),"OUTPUT_ABSOLUTE");
 const parent=fs.realpathSync(path.dirname(file));
 for(let p=parent;;p=path.dirname(p)){check(!fs.existsSync(path.join(p,".git")),"GIT_OUTPUT");if(path.dirname(p)===p)break;}
 const fd=fs.openSync(file,fs.constants.O_WRONLY|fs.constants.O_CREAT|fs.constants.O_EXCL|(fs.constants.O_NOFOLLOW||0),0o600);
 try{fs.writeFileSync(fd,JSON.stringify(obj,null,2)+"\n","utf8");fs.fsyncSync(fd);}finally{fs.closeSync(fd);}
}
export function cli(argv){
 const [command,...params]=argv;check(params.length%2===0,"ARGS");const x={};
 for(let i=0;i<params.length;i+=2){check(params[i].startsWith("--")&&!Object.hasOwn(x,params[i].slice(2)),"ARGS");x[params[i].slice(2)]=params[i+1]}
 if(command==="template"){
  exact(x,["player","trial","scenario","output"]);
  const obj=blank(x.trial,x.scenario,hashPlayer(x.player));safeWrite(x.output,obj);return {status:"BLANK_ONLY",output:x.output};
 }
 if(command==="score"){
  exact(x,["input","output"]);const out=score(safeRead(x.input));safeWrite(x.output,out);return {status:"TECHNICAL_ONLY",output:x.output,completion:out.completion};
 }
 if(command==="batch"){
  exact(x,["input","output"]);const out=batch(safeRead(x.input));safeWrite(x.output,out);return {status:"TECHNICAL_ONLY",output:x.output,trials:out.aggregate.count};
 }
 throw Error("UNKNOWN_COMMAND");
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
 try{console.log(JSON.stringify(cli(process.argv.slice(2))))}
 catch(e){console.error("FAIL_CLOSED",String(e.message||e));process.exitCode=2}
}
