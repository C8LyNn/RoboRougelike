#!/usr/bin/env node
// B01-13 W17: offline visual review board for frozen W16 cross-rater results.
// Never attributes the data to real humans or a tested Unity Player.
import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import {pathToFileURL} from "node:url";

export const W16="3f83937874c69cb8744247cb51606dcb2fa0fd91";
export const GAME="d94400d95d13db7f27e1533df05c9292d5380431";
export const SAVE="33094f154c7b91119b36d58aab285e6e83c5de57";
export const FLAGS=Object.freeze(["COMPLETION_CONFLICT","HELP_CONFLICT","NOTICE_MISSING_CONFLICT","NOTICE_TIME_GAP","COMPLETION_TIME_GAP","ACTION_COUNT_GAP","FEEDBACK_CLASS_CONFLICT"]);
export const SCENARIOS=Object.freeze(["DETACHED_AT_SAVE","REATTACHED_AT_SAVE","DOCK_DENIED"]);
const RESULTS=["RECOVERED","ABANDONED","STOPPED"];
const die=(x,msg)=>{if(!x)throw Error(msg)};
const eq=(x,ks)=>die(x&&typeof x==="object"&&!Array.isArray(x)&&Object.keys(x).sort().join("|")===ks.slice().sort().join("|"),"SCHEMA_KEYS");
const uint=(v,max=600000)=>Number.isInteger(v)&&v>=0&&v<=max;
const maybe=v=>v===null||uint(v);
export const digest=b=>crypto.createHash("sha256").update(b).digest("hex");
export function severity(flags){
 if(flags.includes("COMPLETION_CONFLICT")||flags.includes("HELP_CONFLICT"))return "P0";
 if(flags.includes("NOTICE_MISSING_CONFLICT")||flags.includes("FEEDBACK_CLASS_CONFLICT"))return "P1";
 if(flags.length)return "P2";
 return "CONSISTENT";
}
function validateRow(x){
 eq(x,["trial_id","scenario","completion_a","completion_b","completion_gap_ms","notice_a_ms","notice_b_ms","notice_gap_ms","actions_a","actions_b","assistance_a","assistance_b","feedback_a","feedback_b","flags","status"]);
 die(/^T[0-9]{4}$/.test(x.trial_id)&&SCENARIOS.includes(x.scenario),"TRIAL_ID_SCENARIO");
 die(RESULTS.includes(x.completion_a)&&RESULTS.includes(x.completion_b),"COMPLETION_VALUES");
 die(uint(x.completion_gap_ms)&&uint(x.actions_a,128)&&uint(x.actions_b,128),"COUNT_TIME");
 die(maybe(x.notice_a_ms)&&maybe(x.notice_b_ms)&&maybe(x.notice_gap_ms),"NOTICE_TIME");
 die(typeof x.assistance_a==="boolean"&&typeof x.assistance_b==="boolean","ASSISTANCE");
 for(const key of ["feedback_a","feedback_b"]){eq(x[key],["clear","ambiguous","absent"]);for(const val of Object.values(x[key]))die(uint(val,128),"FEEDBACK_COUNT")}
 die(Array.isArray(x.flags)&&x.flags.length<=FLAGS.length&&new Set(x.flags).size===x.flags.length&&x.flags.every(f=>FLAGS.includes(f)),"FLAGS");
 const expected=[];
 if(x.completion_a!==x.completion_b)expected.push("COMPLETION_CONFLICT");
 if(x.assistance_a!==x.assistance_b)expected.push("HELP_CONFLICT");
 if((x.notice_a_ms===null)!==(x.notice_b_ms===null))expected.push("NOTICE_MISSING_CONFLICT");
 else if(x.notice_a_ms!==null&&Math.abs(x.notice_a_ms-x.notice_b_ms)>1000)expected.push("NOTICE_TIME_GAP");
 if(x.completion_gap_ms>2000)expected.push("COMPLETION_TIME_GAP");
 if(Math.abs(x.actions_a-x.actions_b)>1)expected.push("ACTION_COUNT_GAP");
 if(["clear","ambiguous","absent"].some(f=>(x.feedback_a[f]>0)!==(x.feedback_b[f]>0)))expected.push("FEEDBACK_CLASS_CONFLICT");
 die(expected.join("|")===x.flags.join("|"),"TAMPERED_FLAGS");
 die(x.completion_gap_ms<=600000,"TIME_RANGE");
 die(x.notice_gap_ms===(x.notice_a_ms===null||x.notice_b_ms===null?null:Math.abs(x.notice_a_ms-x.notice_b_ms)),"NOTICE_DIFF");
 die(x.status===(x.flags.length?"REVIEW_REQUIRED":"CONSISTENT_TRANSCRIPTION"),"STATUS");
}
export function validate(report){
 eq(report,["schema","w15_commit","w15_blob","game_sha","save_sha","player_exe_sha256","evidence","human_verified","native_unity","live_save_load_verified","code_ack","source_to_player","gates","thresholds","summary","review_queue","all_results"]);
 die(report.schema==="B01-13-RATER-CONSENSUS-RESULT/R1"&&report.w15_commit==="acc2288f72d47bd3969f840524cf8c5d07c0c6d2"&&report.w15_blob==="6ec383f0b3e341d58f9b5e971fc435a74d78a583","W15_FROZEN");
 die(report.game_sha===GAME&&report.save_sha===SAVE,"FROZEN_CONTRACT");
 die(typeof report.player_exe_sha256==="string"&&/^[a-f0-9]{64}$/.test(report.player_exe_sha256),"PLAYER_HASH");
 die(report.evidence==="TWO_OPERATOR_TRANSCRIPTIONS_NOT_ATTESTED"&&report.human_verified===0&&report.native_unity==="NOT_RUN"&&report.live_save_load_verified===false&&report.code_ack===false&&report.source_to_player==="UNVERIFIED","FALSE_ATTESTATION");
 eq(report.gates,["source_only","native_g1","study_sessions_verified"]);
 die(report.gates.source_only===true&&report.gates.native_g1==="NOT_RUN"&&report.gates.study_sessions_verified===0,"FORGED_GATE");
 eq(report.thresholds,["notice_ms","completion_ms","attempts"]);
 die(report.thresholds.notice_ms===1000&&report.thresholds.completion_ms===2000&&report.thresholds.attempts===1,"THRESHOLDS");
 eq(report.summary,["trial_pairs","review_required","consistent","completion_agreement","flag_counts"]);
 die(Array.isArray(report.all_results)&&report.all_results.length>=1&&report.all_results.length<=128&&Array.isArray(report.review_queue),"TRIAL_COUNT");
 const seen=new Set(),flagCounts={},flagged=[];
 for(const row of report.all_results){
  validateRow(row);die(!seen.has(row.trial_id),"DUPLICATE_ID");seen.add(row.trial_id);
  if(row.flags.length){flagged.push(row);for(const f of row.flags)flagCounts[f]=(flagCounts[f]||0)+1}
 }
 die(report.review_queue.length===flagged.length&&report.review_queue.every((row,i)=>JSON.stringify(row)===JSON.stringify(flagged[i])),"QUEUE_MISMATCH");
 die(report.summary.trial_pairs===report.all_results.length&&report.summary.review_required===flagged.length&&report.summary.consistent===report.all_results.length-flagged.length,"SUMMARY_MISMATCH");
 eq(report.summary.flag_counts,Object.keys(flagCounts));
 die(Object.keys(flagCounts).every(key=>report.summary.flag_counts[key]===flagCounts[key]),"FLAG_COUNT_MISMATCH");
 eq(report.summary.completion_agreement,["observed_agreement","expected_chance_agreement","cohen_kappa","kappa_defined"]);
 const c=report.summary.completion_agreement;
 const n=report.all_results.length;
 const observed=report.all_results.filter(x=>x.completion_a===x.completion_b).length/n;
 const a=RESULTS.map(v=>report.all_results.filter(x=>x.completion_a===v).length/n);
 const b=RESULTS.map(v=>report.all_results.filter(x=>x.completion_b===v).length/n);
 const chance=a.reduce((s,v,i)=>s+v*b[i],0);
 const k=1-chance<1e-12?null:Number(((observed-chance)/(1-chance)).toFixed(4));
 die(c.observed_agreement===Number(observed.toFixed(4))&&c.expected_chance_agreement===Number(chance.toFixed(4))&&c.kappa_defined===(k!==null)&&c.cohen_kappa===k,"KAPPA_DRIFT");
 return report;
}
export function escapeHtml(s){
 return String(s).replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;").replace(/'/g,"&#39;");
}
const css=[
":root{font-family:system-ui,-apple-system,Segoe UI,sans-serif;color-scheme:dark;--bg:#101824;--surface:#1a2636;--line:#344457;--text:#f1f5fa;--muted:#aec0d1;--accent:#78dbe2}",
"*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);line-height:1.5}",
".wrap{max-width:1340px;margin:0 auto;padding:2rem 1rem 4rem}.skip{position:absolute;left:-9999px}.skip:focus{left:12px;top:12px;background:white;color:black;padding:1rem;z-index:10}",
"h1{margin:.25rem 0;font-size:clamp(1.5rem,4vw,2.3rem)}h2{margin-top:2.1rem;font-size:1.3rem}p{max-width:85ch}",
".overline{color:var(--accent);font-weight:700;letter-spacing:.08em}.warning{border-left:5px solid #ffbe73;padding:1rem;background:#342a27;margin:1.5rem 0}",
".stats{display:grid;grid-template-columns:repeat(auto-fit,minmax(190px,1fr));gap:.75rem}.stat{background:var(--surface);padding:1rem;border:1px solid var(--line);border-radius:12px}.stat strong{display:block;font-size:1.6rem}.muted{color:var(--muted)}",
".toolbar{display:flex;flex-wrap:wrap;align-items:end;gap:.75rem;margin:1.25rem 0}label{display:flex;flex-direction:column;gap:.35rem;font-weight:600}",
"input,select,button{font:inherit;color:var(--text);background:#192738;border:1px solid #627993;padding:.65rem;border-radius:7px}input:focus-visible,select:focus-visible,button:focus-visible,a:focus-visible{outline:3px solid var(--accent);outline-offset:2px}",
"button{cursor:pointer}button:hover{background:#2d4258}.table-wrap{overflow-x:auto;border:1px solid var(--line);border-radius:12px}",
"table{border-collapse:collapse;width:100%;min-width:900px}caption{text-align:left;padding:.8rem;color:var(--muted)}th,td{border-bottom:1px solid var(--line);padding:.7rem;text-align:left;vertical-align:top}th{background:#253449;white-space:nowrap}tr:hover{background:#26384c}tr[hidden]{display:none}",
".pill{display:inline-block;border:1px solid #677d96;border-radius:999px;padding:.1rem .5rem;white-space:nowrap;font-weight:700}.P0{border-color:#ff8c8c;color:#ffacac}.P1{border-color:#ffce85;color:#ffce85}.P2{border-color:#83d8e9;color:#83d8e9}.CONSISTENT{color:#a7d6b8}",
".flags{max-width:340px}.flags span{display:inline-block;margin:.1rem;padding:.12rem .35rem;border-radius:4px;background:#344255;font-size:.85rem}",
"details{border:1px solid var(--line);border-radius:10px;padding:.8rem;margin:1rem 0}summary{cursor:pointer;font-weight:700}code{overflow-wrap:anywhere}.tiny{font-size:.85rem}",
"@media print{body{background:white;color:black}.toolbar,.skip{display:none}.warning,.stat,.table-wrap,details{border-color:#777;background:white;color:black}.table-wrap{overflow:visible}table{min-width:0;font-size:9pt}tr[hidden]{display:table-row}.pill{color:black}.flags span{background:#eee;color:black}}",
"@media (prefers-reduced-motion:reduce){*{scroll-behavior:auto}}"
].join("\n");
const ui=[
"(()=>{'use strict';const q=id=>document.getElementById(id);",
"const rows=[...document.querySelectorAll('#trials tbody tr')];",
"const scenario=q('scenario'),priority=q('priority'),flag=q('flag'),search=q('search'),counts=q('visible-count');",
"function update(){let visible=0;for(const row of rows){",
"const ok=(scenario.value==='ALL'||row.dataset.scenario===scenario.value)&&(priority.value==='ALL'||row.dataset.priority===priority.value)&&(flag.value==='ALL'||row.dataset.flags.split(' ').includes(flag.value))&&row.dataset.trial.includes(search.value.toUpperCase().trim());",
"row.hidden=!ok;if(ok)visible++;}",
"counts.textContent=visible+' / '+rows.length+' prób widocznych';",
"}",
"for(const control of [scenario,priority,flag,search])control.addEventListener('input',update);",
"q('reset').addEventListener('click',()=>{scenario.value='ALL';priority.value='ALL';flag.value='ALL';search.value='';update();search.focus();});",
"q('print').addEventListener('click',()=>window.print());",
"search.addEventListener('keydown',e=>{if(e.key==='Escape'){search.value='';update();}});",
"update();})();"
].join("\n");
export function render(report,bytes){
 validate(report);
 die(typeof bytes==="string"&&/^[a-f0-9]{64}$/.test(bytes),"EVIDENCE_DIGEST");
 const rows=[...report.all_results].sort((a,b)=>{
 const weight={P0:0,P1:1,P2:2,CONSISTENT:3};
 return weight[severity(a.flags)]-weight[severity(b.flags)]||a.trial_id.localeCompare(b.trial_id);
 });
 const esc=escapeHtml;
 const h=['<!doctype html>','<html lang="pl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">'];
 const csp="'sha256-"+crypto.createHash("sha256").update(ui).digest("base64")+"'";
 h.push('<meta http-equiv="Content-Security-Policy" content="default-src &apos;none&apos;; script-src '+csp+'; style-src &apos;unsafe-inline&apos;; base-uri &apos;none&apos;; form-action &apos;none&apos;; img-src &apos;none&apos;; connect-src &apos;none&apos;">');
 h.push('<title>B01-13 — przegląd rozbieżności obserwacji</title><style>'+css+'</style></head><body>');
 h.push('<a class="skip" href="#reviews">Przejdź do tabeli prób</a><main class="wrap"><p class="overline">B01-13 · RAPORT W17 · OFFLINE</p>');
 h.push('<h1>Rozbieżności obserwacji po Save/Load</h1><p class="muted">Wizualna kolejka przypadków do omówienia na podstawie raportu B01-13 W16. Priorytety dotyczą rozbieżności transkrypcji, nie ważności błędów gry.</p>');
 h.push('<section class="warning" aria-label="Ograniczenia danych"><strong>Niezweryfikowane obserwacje techniczne</strong><p>0 potwierdzonych uczestników, brak testu natywnego Unity/PhysX, brak dowodu trwałości ID lub prawidłowego Save/Load. Zgodność dwóch zapisów nie potwierdza działania gry.</p></section>');
 const sum=report.summary;
 for(const [name,n] of [["Wszystkie próby",sum.trial_pairs],["Do wyjaśnienia",sum.review_required],["Zgodne zapisy",sum.consistent]]){
  if(name==="Wszystkie próby")h.push('<section class="stats" aria-label="Podsumowanie">');
  h.push('<div class="stat"><span class="muted">'+esc(name)+'</span><strong>'+n+'</strong></div>');
 }
 h.push('<div class="stat"><span class="muted">Zgodność wyniku</span><strong>'+Math.round(sum.completion_agreement.observed_agreement*100)+'%</strong></div>');
 h.push('<div class="stat"><span class="muted">Kappa Cohena</span><strong>'+(sum.completion_agreement.cohen_kappa===null?'N/D':esc(sum.completion_agreement.cohen_kappa))+'</strong></div></section>');
 h.push('<h2 id="reviews">Kolejka obserwacji</h2><p class="muted">P0: różny wynik lub pomoc. P1: brak zauważenia / różny feedback. P2: rozbieżności czasu lub prób. Filtry działają lokalnie, również bez internetu.</p>');
 h.push('<div class="toolbar"><label>Scenariusz<select id="scenario"><option value="ALL">Wszystkie</option>');
 for(const s of SCENARIOS)h.push('<option value="'+s+'">'+s+'</option>');
 h.push('</select></label><label>Priorytet<select id="priority"><option value="ALL">Wszystkie</option>');
 for(const s of ["P0","P1","P2","CONSISTENT"])h.push('<option value="'+s+'">'+s+'</option>');
 h.push('</select></label><label>Typ rozbieżności<select id="flag"><option value="ALL">Wszystkie</option>');
 for(const s of FLAGS)h.push('<option value="'+s+'">'+s+'</option>');
 h.push('</select></label><label>ID próby<input id="search" type="search" placeholder="np. T0001" autocomplete="off"></label><button id="reset" type="button">Wyczyść filtry</button><button id="print" type="button">Drukuj raport</button></div>');
 h.push('<p id="visible-count" role="status" aria-live="polite"></p>');
 h.push('<div class="table-wrap"><table id="trials"><caption>Próby uszeregowane według rodzaju rozbieżności; można filtrować.</caption><thead><tr><th scope="col">Próba</th><th scope="col">Priorytet</th><th scope="col">Scenariusz</th><th scope="col">Ocena A / B</th><th scope="col">Różnica NOTICE</th><th scope="col">Różnica END</th><th scope="col">Flagi</th></tr></thead><tbody>');
 for(const row of rows){
  const s=severity(row.flags);
  const f=row.flags.length?row.flags.map(x=>'<span>'+esc(x)+'</span>').join(""):'<span>Brak</span>';
  const notice=row.notice_gap_ms===null?'brak porównania':row.notice_gap_ms+' ms';
  h.push('<tr data-trial="'+row.trial_id+'" data-scenario="'+row.scenario+'" data-priority="'+s+'" data-flags="'+row.flags.join(" ")+'"><th scope="row">'+row.trial_id+'</th><td><span class="pill '+s+'">'+s+'</span></td><td>'+row.scenario+'</td><td>'+row.completion_a+' / '+row.completion_b+'</td><td>'+notice+'</td><td>'+row.completion_gap_ms+' ms</td><td class="flags">'+f+'</td></tr>');
 }
 h.push('</tbody></table></div>');
 h.push('<details><summary>Integralność i znaczenie dowodu</summary><p>Źródłem jest raport W16 po dwóch ręcznych transkrypcjach. Nie służy jako dowód testu u ludzi, gwarancja poprawności Save/Load ani atest binarny Unity.</p>');
 h.push('<p class="tiny">W16 exact commit: <code>'+W16+'</code><br>Wybrany Secure Reattach #510: <code>'+GAME+'</code><br>Wybrany Save #556: <code>'+SAVE+'</code><br>SHA256 oryginalnego JSON: <code>'+bytes+'</code><br>Hash EXE zgłoszony przez W16 (niezweryfikowany): <code>'+report.player_exe_sha256+'</code></p></details>');
 h.push('<p class="muted tiny">RAPORT WYŁĄCZNIE OFFLINE · ZERO POTWIERDZONYCH SESJI Z UDZIAŁEM LUDZI · NATIVE UNITY: NOT_RUN · LIVE SAVE: NOT_VERIFIED</p></main>');
 h.push('<script>'+ui+'</script></body></html>');
 return h.join("\n");
}
export function exportFile(input,output){
 die(path.isAbsolute(input)&&path.isAbsolute(output),"ABSOLUTE_PATHS");
 const st=fs.lstatSync(input);die(st.isFile()&&!st.isSymbolicLink()&&st.size>0&&st.size<=1024*1024,"INPUT_FILE");
 const bytes=fs.readFileSync(input),report=JSON.parse(bytes.toString("utf8"));
 const html=render(report,digest(bytes));
 for(let d=fs.realpathSync(path.dirname(output));;d=path.dirname(d)){die(!fs.existsSync(path.join(d,".git")),"OUTPUT_IN_GIT");if(path.dirname(d)===d)break}
 const fd=fs.openSync(output,fs.constants.O_WRONLY|fs.constants.O_CREAT|fs.constants.O_EXCL|(fs.constants.O_NOFOLLOW||0),0o600);
 try{fs.writeFileSync(fd,html,"utf8");fs.fsyncSync(fd);}finally{fs.closeSync(fd);}
 return {output,review_required:report.summary.review_required,bytes:Buffer.byteLength(html),source_report_sha256:digest(bytes),human_verified:0,native_unity:"NOT_RUN"};
}
export function cli(args){
 die(args.length===4&&args[0]==="--input"&&args[2]==="--output","ARGS");
 return exportFile(args[1],args[3]);
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
 try{console.log(JSON.stringify(cli(process.argv.slice(2))))}
 catch(error){console.error("FAIL_CLOSED",String(error.message||error));process.exitCode=2}
}
