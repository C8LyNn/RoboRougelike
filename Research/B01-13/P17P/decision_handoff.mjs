#!/usr/bin/env node
// B01-13 W18 independent extension to immutable W17 offline review board.
// No imported Unity/Save source; all observer evidence remains unverified.
import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import {pathToFileURL} from "node:url";

export const PREDECESSOR_SHA="f05790e274f31ac39c6ae4b2e873db896a3b36d8";
export const PREDECESSOR_BLOB="d9f4267ad2f827a84e08f63e4c5880af1b8c739e";
export const ONLY_GAME="d94400d95d13db7f27e1533df05c9292d5380431";
export const ONLY_SAVE="33094f154c7b91119b36d58aab285e6e83c5de57";
export const ONLY_HOST="efb2b06eafedab5469b57f163152d96aca3c5915";
export const CHOICES=Object.freeze(["PENDING","CHECK_TRANSCRIPT","REQUEST_NATIVE_REPRO","DOCUMENT_VARIANCE"]);
const requireOk=(condition,reason)=>{if(!condition)throw Error(reason)};
const blob=(bytes)=>crypto.createHash("sha1").update("blob "+bytes.length+"\0").update(bytes).digest("hex");
const sha256=(bytes)=>crypto.createHash("sha256").update(bytes).digest("hex");

export function verifyPredecessor(file){
 requireOk(typeof file==="string"&&path.isAbsolute(file),"UPSTREAM_PATH");
 const st=fs.lstatSync(file);
 requireOk(st.isFile()&&!st.isSymbolicLink()&&st.size>0&&st.size<=65536,"UPSTREAM_TYPE");
 const source=fs.readFileSync(file);
 requireOk(blob(source)===PREDECESSOR_BLOB,"UPSTREAM_BLOB_DRIFT");
 return pathToFileURL(fs.realpathSync(file)).href;
}
export function renderDecisionExtension(html,reportDigest,flaggedIds){
 requireOk(typeof html==="string"&&html.startsWith("<!doctype html>"),"HTML_SOURCE");
 requireOk(typeof reportDigest==="string"&&/^[a-f0-9]{64}$/.test(reportDigest),"REPORT_DIGEST");
 requireOk(Array.isArray(flaggedIds)&&flaggedIds.length>0&&flaggedIds.length<=128,"REVIEW_COUNT");
 requireOk(flaggedIds.every(x=>/^T[0-9]{4}$/.test(x))&&new Set(flaggedIds).size===flaggedIds.length,"TRIAL_ID");
 const quote=String.fromCharCode(39);
 const opts=CHOICES.map(x=>'<option value="'+x+'">'+x+'</option>').join("");
 const rows=flaggedIds.map(id=>'<tr><th scope="row">'+id+'</th><td><label for="decision-'+id+'">Decyzja dla '+id+'</label><select id="decision-'+id+'" data-review-decision="'+id+'">'+opts+'</select></td></tr>').join("");
 const section=[
 '<section id="decision-handoff" aria-labelledby="decision-title">',
 '<h2 id="decision-title">Decyzje do przekazania (lokalnie)</h2>',
 '<p>To są decyzje o dalszej analizie rozbieżności, nie odbiór funkcji gry. Wybór nie zmienia źródłowych obserwacji. Brak nazwisk, opisów i połączeń sieciowych.</p>',
 '<div class="table-wrap"><table id="decision-table"><caption>Decyzja techniczna dla każdej rozbieżności</caption><thead><tr><th scope="col">Próba</th><th scope="col">Następny krok</th></tr></thead><tbody>'+rows+'</tbody></table></div>',
 '<p id="decision-count" role="status" aria-live="polite">0 / '+flaggedIds.length+' decyzji określonych</p>',
 '<button type="button" id="download-decisions">Pobierz lokalną listę decyzji (.json)</button>',
 '<p class="muted">PENDING pozostaje otwarte. CHECK_TRANSCRIPT, REQUEST_NATIVE_REPRO i DOCUMENT_VARIANCE oznaczają propozycje działania, nie wykonaną weryfikację, akceptację QA, ani potwierdzony błąd Unity.</p>',
 '</section>'
 ].join("\n");
 const logic=[
 "(()=>{'use strict';",
 "const controls=[...document.querySelectorAll('[data-review-decision]')];",
 "const allowed=new Set(['PENDING','CHECK_TRANSCRIPT','REQUEST_NATIVE_REPRO','DOCUMENT_VARIANCE']);",
 "const count=document.getElementById('decision-count');",
 "const sha='"+reportDigest+"';",
 "function records(){return controls.map(c=>({trial_id:c.dataset.reviewDecision,disposition:allowed.has(c.value)?c.value:'PENDING'}));}",
 "function refresh(){count.textContent=records().filter(r=>r.disposition!=='PENDING').length+' / '+controls.length+' decyzji określonych';}",
 "for(const c of controls)c.addEventListener('change',refresh);",
 "document.getElementById('download-decisions').addEventListener('click',()=>{",
 " const payload={schema:'B01-13-REVIEW-HANDOFF/R1',w17_source:'"+PREDECESSOR_SHA+"',source_report_sha256:sha,verification:'UNATTESTED_OPERATOR_TRIAGE',human_verified:0,native_unity:'NOT_RUN',live_game_save_verified:false,codex_accepted:false,decisions:records()};",
 " const b=new Blob([JSON.stringify(payload,null,2)+'\\n'],{type:'application/json'});",
 " const url=URL.createObjectURL(b),a=document.createElement('a');",
 " a.href=url;a.download='b01-13-review-handoff-'+sha.slice(0,12)+'.json';",
 " document.body.appendChild(a);a.click();a.remove();",
 " setTimeout(()=>URL.revokeObjectURL(url),1000);",
 "});refresh();})();"
 ].join("\n");
 const scriptSha=crypto.createHash("sha256").update(logic).digest("base64");
 const pattern=/script-src ('sha256-[a-zA-Z0-9+/=]+');/;
 requireOk(pattern.test(html)&&html.split("</main>").length===2&&html.split("</body>").length===2,"W17_HTML_TOPOLOGY");
 html=html.replace(pattern,(_m,original)=>"script-src "+original+" "+quote+"sha256-"+scriptSha+quote+";");
 html=html.replace("</main>",section+"</main>");
 html=html.replace("</body>","<script>"+logic+"</script></body>");
 return html;
}
export async function buildHtml(report,originalW17,bytesSha){
 const url=verifyPredecessor(originalW17);
 const {validate,render}=await import(url);
 requireOk(typeof validate==="function"&&typeof render==="function","UPSTREAM_API");
 validate(report);
 requireOk(report.summary.review_required>0,"NO_UNRESOLVED_DISAGREEMENT");
 const ids=report.review_queue.map(x=>x.trial_id);
 const originalHtml=render(report,bytesSha);
 return renderDecisionExtension(originalHtml,bytesSha,ids);
}
function secureRead(file){
 requireOk(path.isAbsolute(file),"INPUT_ABSOLUTE");
 const st=fs.lstatSync(file);
 requireOk(st.isFile()&&!st.isSymbolicLink()&&st.size>0&&st.size<=1024*1024,"INPUT_INVALID");
 return fs.readFileSync(file);
}
function secureWrite(file,html){
 requireOk(path.isAbsolute(file),"OUTPUT_ABSOLUTE");
 for(let parent=fs.realpathSync(path.dirname(file));;parent=path.dirname(parent)){
  requireOk(!fs.existsSync(path.join(parent,".git")),"GIT_OUTPUT");
  if(path.dirname(parent)===parent)break;
 }
 const fd=fs.openSync(file,fs.constants.O_WRONLY|fs.constants.O_CREAT|fs.constants.O_EXCL|(fs.constants.O_NOFOLLOW||0),0o600);
 try{fs.writeFileSync(fd,html,"utf8");fs.fsyncSync(fd)}finally{fs.closeSync(fd)}
}
export async function cli(args){
 requireOk(args.length===6&&args[0]==="--input"&&args[2]==="--upstream"&&args[4]==="--output","ARGS");
 const [input,upstream,output]=[args[1],args[3],args[5]];
 const bytes=secureRead(input),report=JSON.parse(bytes.toString("utf8")),originalSha=sha256(bytes);
 const html=await buildHtml(report,upstream,originalSha);
 secureWrite(output,html);
 return {status:"SOURCE_ONLY",output,source_report_sha256:originalSha,original_w17_blob:PREDECESSOR_BLOB,items:report.summary.review_required,original_html_sha256:sha256(Buffer.from(html)),human_verified:0,native_unity:"NOT_RUN"};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
 try{console.log(JSON.stringify(await cli(process.argv.slice(2))))}catch(e){console.error("FAIL_CLOSED",String(e.message||e));process.exitCode=2}
}
