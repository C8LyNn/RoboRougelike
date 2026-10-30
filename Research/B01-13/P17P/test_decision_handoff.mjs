import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import crypto from "node:crypto";
import vm from "node:vm";
import {spawnSync} from "node:child_process";
import {pathToFileURL,fileURLToPath} from "node:url";
import {PREDECESSOR_SHA,PREDECESSOR_BLOB,ONLY_GAME,ONLY_SAVE,ONLY_HOST,CHOICES,verifyPredecessor,renderDecisionExtension,buildHtml,cli} from "./decision_handoff.mjs";

function gitBlob(bytes){return crypto.createHash("sha1").update("blob "+bytes.length+"\0").update(bytes).digest("hex")}
const sources=[["B01_W15_ORIGINAL_SOURCE","6ec383f0b3e341d58f9b5e971fc435a74d78a583"],["B01_W16_ORIGINAL_SOURCE","56d158bb6c107a04fc42e7ee78d6a07317997729"],["B01_W17_ORIGINAL_SOURCE",PREDECESSOR_BLOB]];
for(const [name,digest] of sources){const f=process.env[name];assert.ok(f&&fs.existsSync(f),"MISSING_ORIGINAL:"+name);assert.equal(gitBlob(fs.readFileSync(f)),digest,"SOURCE_DRIFT:"+name)}
const load=async n=>import(pathToFileURL(fs.realpathSync(process.env[n])).href);
const {analyze}=await load("B01_W16_ORIGINAL_SOURCE");
const {validate:validateW17,render:renderW17}=await load("B01_W17_ORIGINAL_SOURCE");
const h="a".repeat(64);
const event=(at_ms,event,value="NONE")=>({at_ms,event,value});
function trial(id="T0001",completion="RECOVERED"){
 return {schema:"B01-13-RECOVERY-TRIAL/R1",game_sha:ONLY_GAME,save_sha:ONLY_SAVE,player_sha256:h,source_to_player:"UNVERIFIED",unity_g1:"NOT_RUN",human_verified:0,trial_id:id,scenario:"DETACHED_AT_SAVE",filled:true,events:completion==="RECOVERED"?[event(0,"CUE"),event(350,"NOTICE"),event(1100,"ACTION"),event(1400,"FEEDBACK","CLEAR"),event(3200,"END","RECOVERED")]:[event(0,"CUE"),event(400,"NOTICE"),event(3600,"END",completion)]};
}
const args=(pairs)=>({schema:"B01-13-RATER-CONSENSUS/R1",truth_gate:"UNATTESTED_TECHNICAL_TRANSCRIPTION",pairs:pairs.map(([a,b])=>({a,b}))});
async function report(){return analyze(args([[trial(),trial("T0001","ABANDONED")],[trial("T0002"),trial("T0002")]]),process.env.B01_W15_ORIGINAL_SOURCE)}
const clone=x=>structuredClone(x);
async function temp(f){const d=fs.mkdtempSync(path.join(os.tmpdir(),"b01-w18-decision-"));try{return await f(d)}finally{fs.rmSync(d,{recursive:true,force:true})}}
function scripts(html){return [...html.matchAll(/<script>([\s\S]*?)<\/script>/g)].map(x=>x[1])}
function cspHashes(html){return [...html.matchAll(/'sha256-([a-zA-Z0-9+/=]+)'/g)].map(x=>x[1])}

test("selected frozen #510/#556/#596 and W17 exact commit pinned",()=>{assert.equal(ONLY_GAME,"d94400d95d13db7f27e1533df05c9292d5380431");assert.equal(ONLY_SAVE,"33094f154c7b91119b36d58aab285e6e83c5de57");assert.equal(ONLY_HOST,"efb2b06eafedab5469b57f163152d96aca3c5915");assert.equal(PREDECESSOR_SHA,"f05790e274f31ac39c6ae4b2e873db896a3b36d8")});
test("exact W17 source file physical Git blob verified",()=>assert.match(verifyPredecessor(process.env.B01_W17_ORIGINAL_SOURCE),/^file:/));
test("W17 prior RED correctly reproduced: original kappa is 0.5",async()=>{const x=await analyze(args([[trial(),trial("T0001","ABANDONED")],[trial("T0002"),trial("T0002")],[trial("T0003","STOPPED"),trial("T0003","STOPPED")]]),process.env.B01_W15_ORIGINAL_SOURCE);assert.equal(x.summary.completion_agreement.cohen_kappa,0.5);validateW17(x)});
test("W17 corrected negative oracle: genuinely tampered kappa is rejected",async()=>{const x=await report();x.summary.completion_agreement.cohen_kappa=0.1234;assert.throws(()=>validateW17(x),/KAPPA_DRIFT/)});
test("W17 original immutable export still renders genuine original observations",async()=>{const x=await report();const html=renderW17(x,"c".repeat(64));assert.match(html,/id="trials"/);assert.match(html,/data-trial="T0001"/)});
test("one selected example emits complete decision board",async()=>{const x=await report();const html=await buildHtml(x,process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64));assert.match(html,/decision-handoff/);assert.match(html,/download-decisions/);assert.match(html,/id="decision-T0001"/);assert.ok(!html.includes('id="decision-T0002"'))});
test("new UI includes four permitted decisions, no participant details",async()=>{const html=await buildHtml(await report(),process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64));for(const d of CHOICES)assert.ok(html.includes('value="'+d+'"'));assert.ok(!html.includes("participant_name"))});
test("existing filtering and new decision scripts preserved",async()=>{const html=await buildHtml(await report(),process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64));assert.equal(scripts(html).length,2);for(const id of ["search","reset","print","download-decisions"])assert.ok(html.includes('id="'+id+'"'))});
test("inline scripts match CSP approved exact SHA256 hashes",async()=>{const html=await buildHtml(await report(),process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64));const sc=scripts(html),hashes=cspHashes(html);assert.equal(sc.length,2);assert.equal(hashes.length,2);for(let i=0;i<2;i++)assert.ok(hashes.includes(crypto.createHash("sha256").update(sc[i]).digest("base64")))});
test("new HTML retains offline CSP restrictions",async()=>{const html=await buildHtml(await report(),process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64));for(const x of ["default-src &apos;none&apos;","connect-src &apos;none&apos;","base-uri &apos;none&apos;"])assert.ok(html.includes(x));assert.ok(!html.includes("<script src="));assert.ok(!html.includes("fetch("))});
test("new HTML uses native table label and visible non-attestation warning",async()=>{const html=await buildHtml(await report(),process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64));for(const item of ['id="decision-table"','for="decision-T0001"','id="decision-count"','role="status"',"nie odbiór funkcji gry","NOT_RUN"])assert.ok(html.includes(item))});
test("deterministic repeated generated artifact given one report",async()=>{const x=await report(),h1=await buildHtml(x,process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64)),h2=await buildHtml(x,process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64));assert.equal(h1,h2)});
test("no disagreement refuses fake resolved-state claim",async()=>{const x=await analyze(args([[trial(),trial()]]),process.env.B01_W15_ORIGINAL_SOURCE);await assert.rejects(()=>buildHtml(x,process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64)),/NO_UNRESOLVED_DISAGREEMENT/)});
test("invalid original report digest refused",async()=>{await assert.rejects(()=>buildHtml(await report(),process.env.B01_W17_ORIGINAL_SOURCE,"notahash"),/REPORT_DIGEST/)});
test("source SHA mutation not tolerated",async()=>{await temp(async d=>{const f=path.join(d,"wrong.mjs");fs.writeFileSync(f,"export function render(){}");await assert.rejects(()=>buildHtml(await report(),f,"c".repeat(64)),/UPSTREAM_BLOB_DRIFT/)})});
test("relative source refused",()=>assert.throws(()=>verifyPredecessor("relative.mjs"),/UPSTREAM_PATH/));
test("symlink to source refused",()=>temp(d=>{const s=path.join(d,"src.mjs"),link=path.join(d,"link.mjs");fs.copyFileSync(process.env.B01_W17_ORIGINAL_SOURCE,s);try{fs.symlinkSync(s,link)}catch{return}assert.throws(()=>verifyPredecessor(link),/UPSTREAM_TYPE/)}));
test("cannot forge human_verified=1",async()=>{const x=clone(await report());x.human_verified=1;await assert.rejects(()=>buildHtml(x,process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64)),/FALSE_ATTESTATION/)});
test("cannot forge Unity G1",async()=>{const x=clone(await report());x.native_unity="PASS";await assert.rejects(()=>buildHtml(x,process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64)),/FALSE_ATTESTATION/)});
test("cannot forge live Save/Load",async()=>{const x=clone(await report());x.live_save_load_verified=true;await assert.rejects(()=>buildHtml(x,process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64)),/FALSE_ATTESTATION/)});
test("cannot forge source-to-player attestation",async()=>{const x=clone(await report());x.source_to_player="VERIFIED";await assert.rejects(()=>buildHtml(x,process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64)),/FALSE_ATTESTATION/)});
test("cannot forge observed review queue",async()=>{const x=clone(await report());x.review_queue=[];await assert.rejects(()=>buildHtml(x,process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64)),/QUEUE_MISMATCH/)});
test("cannot forge original selected game",async()=>{const x=clone(await report());x.game_sha="0".repeat(40);await assert.rejects(()=>buildHtml(x,process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64)),/FROZEN_CONTRACT/)});
test("cannot forge original selected Save schema",async()=>{const x=clone(await report());x.save_sha="0".repeat(40);await assert.rejects(()=>buildHtml(x,process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64)),/FROZEN_CONTRACT/)});
test("cannot invent participant identity field",async()=>{const x=clone(await report());x.participant_name="person";await assert.rejects(()=>buildHtml(x,process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64)),/SCHEMA_KEYS/)});
test("existing HTML topology tamper refused",()=>assert.throws(()=>renderDecisionExtension("<html>wrong</html>","c".repeat(64),["T0001"]),/HTML_SOURCE/));
test("new extension cannot inject arbitrary strings into IDs",()=>assert.throws(()=>renderDecisionExtension("<!doctype html>","c".repeat(64),['"><img onerror=alert(1)>']),/TRIAL_ID/));
test("new extension refuses duplicate trial ID choices",()=>assert.throws(()=>renderDecisionExtension("<!doctype html>","c".repeat(64),["T0001","T0001"]),/TRIAL_ID/));
test("new extension rejects HTML without expected CSP",()=>assert.throws(()=>renderDecisionExtension("<!doctype html><html></html>","c".repeat(64),["T0001"]),/W17_HTML_TOPOLOGY/));
test("physical subprocess exports isolated HTML with original SHA256 evidence",async()=>temp(async d=>{const x=await report(),raw=Buffer.from(JSON.stringify(x)),f=path.join(d,"observations.json"),out=path.join(d,"decisions.html");fs.writeFileSync(f,raw);const run=spawnSync(process.execPath,[fileURLToPath(new URL("./decision_handoff.mjs",import.meta.url)),"--input",f,"--upstream",process.env.B01_W17_ORIGINAL_SOURCE,"--output",out],{encoding:"utf8"});assert.equal(run.status,0,run.stderr);const stats=JSON.parse(run.stdout),html=fs.readFileSync(out,"utf8");assert.equal(stats.source_report_sha256,crypto.createHash("sha256").update(raw).digest("hex"));assert.ok(html.includes(stats.source_report_sha256));assert.equal(stats.human_verified,0);assert.equal(stats.native_unity,"NOT_RUN");assert.equal(stats.items,1)}));
test("exclusive file creation blocks replay/overwrite",async()=>temp(async d=>{const f=path.join(d,"x.json"),out=path.join(d,"out.html");fs.writeFileSync(f,JSON.stringify(await report()));const opts=["--input",f,"--upstream",process.env.B01_W17_ORIGINAL_SOURCE,"--output",out];await cli(opts);await assert.rejects(()=>cli(opts),/EEXIST/)}));
test("output into Git working tree rejected",async()=>temp(async d=>{const f=path.join(d,"x.json"),out=path.join(d,"out.html");fs.writeFileSync(f,JSON.stringify(await report()));fs.mkdirSync(path.join(d,".git"));await assert.rejects(()=>cli(["--input",f,"--upstream",process.env.B01_W17_ORIGINAL_SOURCE,"--output",out]),/GIT_OUTPUT/);assert.equal(fs.existsSync(out),false)}));
test("relative input, output or malformed CLI fail closed",async()=>{await assert.rejects(()=>cli(["--input","x","--upstream",process.env.B01_W17_ORIGINAL_SOURCE,"--output","y"]),/INPUT_ABSOLUTE/);await assert.rejects(()=>cli(["--human"]),/ARGS/)});
test("invalid JSON cannot create output",async()=>temp(async d=>{const f=path.join(d,"x.json"),out=path.join(d,"out.html");fs.writeFileSync(f,"not json");await assert.rejects(()=>cli(["--input",f,"--upstream",process.env.B01_W17_ORIGINAL_SOURCE,"--output",out]));assert.equal(fs.existsSync(out),false)}));
test("new UI decision controls work in mock DOM (not a real browser test)",async()=>{
 const html=await buildHtml(await report(),process.env.B01_W17_ORIGINAL_SOURCE,"c".repeat(64));
 const source=scripts(html)[1];
 let exported="",download="",revoke="",clicked=false;
 const select={dataset:{reviewDecision:"T0001"},value:"PENDING",events:{},addEventListener(n,f){this.events[n]=f}};
 const summary={textContent:""};
 const button={events:{},addEventListener(n,f){this.events[n]=f}};
 const nodes={"decision-count":summary,"download-decisions":button};
 const fakeDocument={getElementById(id){return nodes[id]},querySelectorAll(selector){assert.equal(selector,"[data-review-decision]");return [select]},body:{appendChild(a){this.last=a}},createElement(type){assert.equal(type,"a");return{href:"",download:"",click(){clicked=true;download=this.download},remove(){}}}};
 const URL={createObjectURL(b){exported=b.parts.join("");return "blob:local"},revokeObjectURL(uri){revoke=uri}};
 class Blob{constructor(parts,opts){this.parts=parts;this.options=opts}}
 const tasks=[];vm.runInNewContext(source,{document:fakeDocument,URL,Blob,setTimeout(fn){tasks.push(fn)}});
 assert.equal(summary.textContent,"0 / 1 decyzji określonych");
 select.value="REQUEST_NATIVE_REPRO";select.events.change();assert.equal(summary.textContent,"1 / 1 decyzji określonych");
 button.events.click();assert.equal(clicked,true);assert.equal(download,"b01-13-review-handoff-"+"c".repeat(12)+".json");
 const doc=JSON.parse(exported);assert.equal(doc.schema,"B01-13-REVIEW-HANDOFF/R1");assert.equal(doc.source_report_sha256,"c".repeat(64));assert.equal(doc.decisions.length,1);assert.equal(doc.decisions[0].disposition,"REQUEST_NATIVE_REPRO");assert.equal(doc.human_verified,0);assert.equal(doc.native_unity,"NOT_RUN");assert.equal(doc.live_game_save_verified,false);assert.equal(doc.codex_accepted,false);
 tasks[0]();assert.equal(revoke,"blob:local");
});
test("invalid client-side dropdown value fails closed to PENDING",async()=>{
 const html=await buildHtml(await report(),process.env.B01_W17_ORIGINAL_SOURCE,"d".repeat(64)),source=scripts(html)[1];
 let exported="";
 const select={dataset:{reviewDecision:"T0001"},value:"G2_APPROVED",addEventListener(){}};
 const button={addEventListener(n,fn){this.fn=fn}};
 const doc={getElementById(id){return id==="decision-count"?{textContent:""}:button},querySelectorAll(){return [select]},createElement(){return{click(){},remove(){}}},body:{appendChild(){}}};
 class Blob{constructor(parts){this.parts=parts}}
 vm.runInNewContext(source,{document:doc,Blob,URL:{createObjectURL(b){exported=b.parts.join("");return "blob:local"},revokeObjectURL(){}},setTimeout(){}});
 button.fn();const result=JSON.parse(exported);assert.equal(result.decisions[0].disposition,"PENDING");assert.equal(result.codex_accepted,false);
});
