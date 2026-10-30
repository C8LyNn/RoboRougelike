#!/usr/bin/env node
import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import {pathToFileURL} from "node:url";
export const SOURCE_SHA="d94400d95d13db7f27e1533df05c9292d5380431";
const scenarios=["ATTACHED_ONLY","DETACHED_HELD","REATTACHED_BEFORE_SAVE"];
function check(ok,why){if(!ok)throw Error(why)}
function schema(o,keys){check(o&&typeof o==="object"&&!Array.isArray(o)&&Object.keys(o).sort().join(",")===keys.slice().sort().join(","),"SCHEMA")}
export function hashBinary(file){
 const stat=fs.lstatSync(file);check(stat.isFile()&&!stat.isSymbolicLink()&&stat.size>0&&stat.size<=8*1024**3,"BINARY_FILE");
 const fd=fs.openSync(file,"r"),hash=crypto.createHash("sha256"),buf=Buffer.alloc(65536);
 try{let n;while((n=fs.readSync(fd,buf,0,buf.length,null))>0)hash.update(buf.subarray(0,n));}finally{fs.closeSync(fd)}
 return hash.digest("hex");
}
export function blank({scenario,session,exe_sha}){
 check(scenarios.includes(scenario),"SCENARIO");check(/^T\d{4}$/.test(session),"SESSION");
 check(/^[a-f0-9]{64}$/.test(exe_sha),"PLAYER_SHA");
 return {schema:"B01-13-VISUAL-SAVE-R1",selected_sha:SOURCE_SHA,player_exe_sha256:exe_sha,source_to_player:"UNVERIFIED",native_unity:"NOT_RUN",human_verified:0,mode:"TECHNICAL_ONLY",scenario,session,unfilled:true,snapshots:[{stage:"BEFORE_SAVE",modules:[]},{stage:"AFTER_LOAD",modules:[]}]};
}
function parseModules(s){
 schema(s,["stage","modules"]);check(Array.isArray(s.modules)&&s.modules.length>0&&s.modules.length<=80,"MODULE_COUNT");
 const index=new Map(),sockets=new Set();
 for(const m of s.modules){
  schema(m,["visual_token","state","socket"]);check(/^V\d{2,4}$/.test(m.visual_token),"VISUAL_TOKEN");
  check(m.state==="ATTACHED"||m.state==="DETACHED","STATE");check(!index.has(m.visual_token),"DUPLICATE_TOKEN");
  if(m.state==="ATTACHED"){check(typeof m.socket==="string"&&/^S\d{2,4}$/.test(m.socket),"SOCKET");check(!sockets.has(m.socket),"DUPLICATE_SOCKET");sockets.add(m.socket);}
  else check(m.socket===null,"DETACHED_SOCKET");
  index.set(m.visual_token,m);
 }
 return index;
}
export function evaluate(d){
 schema(d,["schema","selected_sha","player_exe_sha256","source_to_player","native_unity","human_verified","mode","scenario","session","unfilled","snapshots"]);
 check(d.schema==="B01-13-VISUAL-SAVE-R1"&&d.selected_sha===SOURCE_SHA,"SELECTED_SHA");
 check(typeof d.player_exe_sha256==="string"&&/^[a-f0-9]{64}$/.test(d.player_exe_sha256),"PLAYER_SHA");
 check(d.source_to_player==="UNVERIFIED"&&d.native_unity==="NOT_RUN"&&d.human_verified===0&&d.mode==="TECHNICAL_ONLY","FORGED_GATE");
 check(scenarios.includes(d.scenario)&&/^T\d{4}$/.test(d.session),"SCENARIO_SESSION");
 check(d.unfilled===false,"UNFILLED_TEMPLATE");check(Array.isArray(d.snapshots)&&d.snapshots.length===2,"SNAPSHOT_PAIR");
 check(d.snapshots[0].stage==="BEFORE_SAVE"&&d.snapshots[1].stage==="AFTER_LOAD","STAGE_ORDER");
 const before=parseModules(d.snapshots[0]),after=parseModules(d.snapshots[1]);
 if(d.scenario==="DETACHED_HELD")check([...before.values()].some(x=>x.state==="DETACHED"),"NO_DETACHED_BEFORE");
 else check([...before.values()].every(x=>x.state==="ATTACHED"),"UNEXPECTED_DETACHED_BEFORE");
 const missing=[],unexpected=[],state_change=[],socket_change=[];
 for(const [tag,pre] of before){
  const post=after.get(tag);if(!post){missing.push(tag);continue}
  if(pre.state!==post.state)state_change.push(tag);
  if(pre.state==="ATTACHED"&&post.state==="ATTACHED"&&pre.socket!==post.socket)socket_change.push(tag);
 }
 for(const tag of after.keys())if(!before.has(tag))unexpected.push(tag);
 const count=missing.length+unexpected.length+state_change.length+socket_change.length;
 return {schema:"B01-13-VISUAL-SAVE-RESULT-R1",selected_sha:SOURCE_SHA,player_exe_sha256:d.player_exe_sha256,scenario:d.scenario,session:d.session,evidence:"OPERATOR_TRANSCRIBED_ONLY",human_verified:0,native_unity:"NOT_RUN",save_id_contract_verified:false,status:count?"OBSERVED_VISUAL_DRIFT":"NO_OBSERVED_VISUAL_DRIFT",diff:{missing,unexpected,state_change,socket_change}};
}
function read(file){const st=fs.lstatSync(file);check(st.isFile()&&!st.isSymbolicLink()&&st.size>0&&st.size<=131072,"INPUT_FILE");return JSON.parse(fs.readFileSync(file,"utf8"))}
function writePrivate(file,data){
 check(path.isAbsolute(file),"ABSOLUTE_PATH");
 for(let p=fs.realpathSync(path.dirname(file));;p=path.dirname(p)){check(!fs.existsSync(path.join(p,".git")),"REPOSITORY_OUTPUT");if(path.dirname(p)===p)break}
 const fd=fs.openSync(file,fs.constants.O_WRONLY|fs.constants.O_CREAT|fs.constants.O_EXCL|(fs.constants.O_NOFOLLOW||0),0o600);
 try{fs.writeFileSync(fd,JSON.stringify(data,null,2)+"\n");fs.fsyncSync(fd)}finally{fs.closeSync(fd)}
}
export function cli(args){
 const [command,...rest]=args;check(rest.length%2===0,"ARGS");const opts={};
 for(let i=0;i<rest.length;i+=2){check(rest[i].startsWith("--")&&!Object.hasOwn(opts,rest[i].slice(2)),"ARGS");opts[rest[i].slice(2)]=rest[i+1]}
 if(command==="template"){
  schema(opts,["player","scenario","session","output"]);const data=blank({scenario:opts.scenario,session:opts.session,exe_sha:hashBinary(opts.player)});
  writePrivate(opts.output,data);return {status:"TEMPLATE_ONLY",output:opts.output};
 }
 if(command==="evaluate"){
  schema(opts,["input","output"]);const report=evaluate(read(opts.input));writePrivate(opts.output,report);return {status:report.status,output:opts.output};
 }
 throw Error("UNKNOWN_COMMAND");
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
 try{console.log(JSON.stringify(cli(process.argv.slice(2))))}catch(e){console.error("FAIL_CLOSED",String(e.message||e));process.exitCode=2}
}
