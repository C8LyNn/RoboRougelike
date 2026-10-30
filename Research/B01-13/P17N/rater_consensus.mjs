#!/usr/bin/env node
// B01-13 PASS17/W16: read-only cross-rater disagreement analysis on original frozen W15 scores.
import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import {pathToFileURL} from "node:url";
export const W15_COMMIT="acc2288f72d47bd3969f840524cf8c5d07c0c6d2";
export const W15_BLOB="6ec383f0b3e341d58f9b5e971fc435a74d78a583";
export const GAME="d94400d95d13db7f27e1533df05c9292d5380431";
export const SAVE="33094f154c7b91119b36d58aab285e6e83c5de57";
export const LIMITS=Object.freeze({notice_ms:1000,completion_ms:2000,attempts:1});
const CATEGORIES=["RECOVERED","ABANDONED","STOPPED"];
function ensure(ok,message){if(!ok)throw Error(message)}
function keys(o,expected){ensure(o&&typeof o==="object"&&!Array.isArray(o)&&Object.keys(o).sort().join("|")===expected.slice().sort().join("|"),"SCHEMA_KEYS")}
function gitBlob(bytes){return crypto.createHash("sha1").update("blob "+bytes.length+"\0").update(bytes).digest("hex")}
export function checkedUpstream(file){
 ensure(path.isAbsolute(file),"UPSTREAM_ABSOLUTE");
 const st=fs.lstatSync(file);ensure(st.isFile()&&!st.isSymbolicLink()&&st.size>0&&st.size<=65536,"UPSTREAM_FILE");
 const bytes=fs.readFileSync(file);ensure(gitBlob(bytes)===W15_BLOB,"UPSTREAM_BLOB_MISMATCH");
 return pathToFileURL(fs.realpathSync(file)).href;
}
function privateInput(file){
 ensure(path.isAbsolute(file),"INPUT_ABSOLUTE");
 const st=fs.lstatSync(file);ensure(st.isFile()&&!st.isSymbolicLink()&&st.size>0&&st.size<=1024*1024,"INPUT_FILE");
 return JSON.parse(fs.readFileSync(file,"utf8"));
}
function privateOutput(file,obj){
 ensure(path.isAbsolute(file),"OUTPUT_ABSOLUTE");
 for(let d=fs.realpathSync(path.dirname(file));;d=path.dirname(d)){ensure(!fs.existsSync(path.join(d,".git")),"OUTPUT_GIT_BLOCK");if(path.dirname(d)===d)break}
 const fd=fs.openSync(file,fs.constants.O_WRONLY|fs.constants.O_CREAT|fs.constants.O_EXCL|(fs.constants.O_NOFOLLOW||0),0o600);
 try{fs.writeFileSync(fd,JSON.stringify(obj,null,2)+"\n");fs.fsyncSync(fd)}finally{fs.closeSync(fd)}
}
function kappa(pairs){
 const n=pairs.length;
 let agree=0;const margA=[0,0,0],margB=[0,0,0];
 for(const x of pairs){if(x.a.completion===x.b.completion)agree++;margA[CATEGORIES.indexOf(x.a.completion)]++;margB[CATEGORIES.indexOf(x.b.completion)]++}
 const observed=agree/n,expected=margA.reduce((s,v,i)=>s+(v/n)*(margB[i]/n),0);
 return {observed_agreement:Number(observed.toFixed(4)),expected_chance_agreement:Number(expected.toFixed(4)),cohen_kappa:Math.abs(1-expected)<1e-12?null:Number(((observed-expected)/(1-expected)).toFixed(4)),kappa_defined:Math.abs(1-expected)>=1e-12};
}
function compare(a,b){
 const flags=[];
 if(a.completion!==b.completion)flags.push("COMPLETION_CONFLICT");
 if((a.help_events>0)!==(b.help_events>0))flags.push("HELP_CONFLICT");
 if(a.notice_ms===null&&b.notice_ms!==null||a.notice_ms!==null&&b.notice_ms===null)flags.push("NOTICE_MISSING_CONFLICT");
 else if(a.notice_ms!==null&&b.notice_ms!==null&&Math.abs(a.notice_ms-b.notice_ms)>LIMITS.notice_ms)flags.push("NOTICE_TIME_GAP");
 if(Math.abs(a.completion_ms-b.completion_ms)>LIMITS.completion_ms)flags.push("COMPLETION_TIME_GAP");
 if(Math.abs(a.attempts-b.attempts)>LIMITS.attempts)flags.push("ACTION_COUNT_GAP");
 const categories=["clear","ambiguous","absent"];
 if(categories.some(key=>(a.feedback[key]>0)!==(b.feedback[key]>0)))flags.push("FEEDBACK_CLASS_CONFLICT");
 return {trial_id:a.trial_id,scenario:a.scenario,completion_a:a.completion,completion_b:b.completion,completion_gap_ms:Math.abs(a.completion_ms-b.completion_ms),notice_a_ms:a.notice_ms,notice_b_ms:b.notice_ms,notice_gap_ms:a.notice_ms===null||b.notice_ms===null?null:Math.abs(a.notice_ms-b.notice_ms),actions_a:a.attempts,actions_b:b.attempts,assistance_a:a.help_events>0,assistance_b:b.help_events>0,feedback_a:a.feedback,feedback_b:b.feedback,flags,status:flags.length?"REVIEW_REQUIRED":"CONSISTENT_TRANSCRIPTION"};
}
export async function analyze(payload,upstreamFile){
 keys(payload,["schema","pairs","truth_gate"]);
 ensure(payload.schema==="B01-13-RATER-CONSENSUS/R1"&&payload.truth_gate==="UNATTESTED_TECHNICAL_TRANSCRIPTION","INPUT_GATE");
 ensure(Array.isArray(payload.pairs)&&payload.pairs.length>=1&&payload.pairs.length<=128,"PAIR_COUNT");
 const upstreamUrl=checkedUpstream(upstreamFile);
 const {score}=await import(upstreamUrl);ensure(typeof score==="function","UPSTREAM_API");
 const rows=[],observed=[],ids=new Set(),hashes=new Set();
 for(const item of payload.pairs){
  keys(item,["a","b"]);
  const a=score(item.a),b=score(item.b);
  ensure(a.game_sha===GAME&&b.game_sha===GAME&&a.save_sha===SAVE&&b.save_sha===SAVE,"FROZEN_SOURCE_PIN");
  ensure(a.trial_id===b.trial_id&&a.scenario===b.scenario&&a.player_sha256===b.player_sha256,"RATER_PAIR_MISMATCH");
  ensure(!ids.has(a.trial_id),"DUPLICATE_TRIAL");ids.add(a.trial_id);hashes.add(a.player_sha256);
  rows.push(compare(a,b));observed.push({a,b});
 }
 ensure(hashes.size===1,"MIXED_EXE_HASH");
 const flagged=rows.filter(x=>x.status==="REVIEW_REQUIRED");
 const counts={};for(const row of flagged)for(const flag of row.flags)counts[flag]=(counts[flag]||0)+1;
 return {schema:"B01-13-RATER-CONSENSUS-RESULT/R1",w15_commit:W15_COMMIT,w15_blob:W15_BLOB,game_sha:GAME,save_sha:SAVE,player_exe_sha256:[...hashes][0],evidence:"TWO_OPERATOR_TRANSCRIPTIONS_NOT_ATTESTED",human_verified:0,native_unity:"NOT_RUN",live_save_load_verified:false,code_ack:false,source_to_player:"UNVERIFIED",gates:{source_only:true,native_g1:"NOT_RUN",study_sessions_verified:0},thresholds:LIMITS,summary:{trial_pairs:rows.length,review_required:flagged.length,consistent:rows.length-flagged.length,completion_agreement:kappa(observed),flag_counts:counts},review_queue:rows.filter(x=>x.flags.length),all_results:rows};
}
export async function cli(args){
 ensure(args.length===6,"ARG_COUNT");const o={};
 for(let i=0;i<6;i+=2){ensure(args[i].startsWith("--")&&!Object.hasOwn(o,args[i].slice(2)),"ARGS");o[args[i].slice(2)]=args[i+1]}
 keys(o,["input","upstream","output"]);
 const report=await analyze(privateInput(o.input),o.upstream);privateOutput(o.output,report);
 return {status:"TECHNICAL_ONLY",trials:report.summary.trial_pairs,review_required:report.summary.review_required,output:o.output};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
 try{console.log(JSON.stringify(await cli(process.argv.slice(2))))}catch(e){console.error("FAIL_CLOSED",String(e.message||e));process.exitCode=2}
}
