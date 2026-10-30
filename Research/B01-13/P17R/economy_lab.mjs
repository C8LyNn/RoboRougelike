#!/usr/bin/env node
// B01-13 PASS17 W20: independent offline balance hypothesis laboratory.
// NOT an authoritative ECON ledger, canonical game Save, actual user test or Unity.
import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import {pathToFileURL} from "node:url";

export const PACKAGE="B01-13-REBUILD-ECON-LAB-001/R1";
export const GAME="d94400d95d13db7f27e1533df05c9292d5380431";
export const SAVE="33094f154c7b91119b36d58aab285e6e83c5de57";
export const R2_HOST="efb2b06eafedab5469b57f163152d96aca3c5915";
export const FROZEN_ECON="12572117a3dce369257feabc57021cde58023369";
export const CADENCES=Object.freeze(["PER_VICTORY","PER_RUN"]);
const ensure=(ok,why)=>{if(!ok)throw Error(why)};
const specs=[
 ["LIGHT_SHORT","Lekki · 2 zwycięstwa",[
  ["RUN01","WIN01",["L01","L02"]],["RUN01","WIN02",["L03"]]]],
 ["HEAVY_STREAK","Ciężki · 2 zwycięstwa",[
  ["RUN02","WIN03",["H01","H02","H03"]],["RUN02","WIN04",["H04","H05"]]]],
 ["TWO_RUNS","Dwa runy · 3 zwycięstwa",[
  ["RUN03","WIN05",["L01","L02"]],["RUN04","WIN06",["H01"]],["RUN04","WIN07",["H02","H03"]]]]
];
export const SCENARIOS=Object.freeze(specs.map(([id,label,events])=>Object.freeze({
 id,label,events:Object.freeze(events.map(([run,victory_id,parts])=>Object.freeze({run,victory_id,parts:Object.freeze(parts)})))
})));
export function validateScenario(s){
 ensure(s&&typeof s==="object"&&Object.keys(s).sort().join("|")==="events|id|label","SCENARIO_FIELDS");
 ensure(/^[A-Z_]{5,24}$/.test(s.id)&&typeof s.label==="string"&&s.label.length>4&&s.label.length<100,"SCENARIO_META");
 ensure(Array.isArray(s.events)&&s.events.length>=1&&s.events.length<=32,"EVENT_COUNT");
 const wins=new Set(),parts=new Set(),runs=new Set();
 let previous=null;
 for(const e of s.events){
  ensure(e&&typeof e==="object"&&Object.keys(e).sort().join("|")==="parts|run|victory_id","EVENT_FIELDS");
  ensure(/^RUN[0-9]{2}$/.test(e.run)&&/^WIN[0-9]{2}$/.test(e.victory_id),"EVENT_IDS");
  ensure(!wins.has(e.victory_id),"VICTORY_REPLAY");wins.add(e.victory_id);
  if(e.run!==previous){ensure(!runs.has(e.run),"NONCONTIGUOUS_RUN");runs.add(e.run);previous=e.run}
  ensure(Array.isArray(e.parts)&&e.parts.length>=1&&e.parts.length<=10,"PART_COUNT");
  for(const p of e.parts){ensure(/^[LH][0-9]{2}$/.test(p),"PART_ID");const key=e.run+":"+p;ensure(!parts.has(key),"DUPLICATE_PART");parts.add(key)}
 }
 return s;
}
SCENARIOS.forEach(validateScenario);
export function simulate(s,cadence,{price=70,budget=250}={}){
 validateScenario(s);ensure(CADENCES.includes(cadence),"CADENCE");
 ensure(Number.isInteger(price)&&price>=1&&price<=1000,"PRICE_RANGE");
 ensure(Number.isInteger(budget)&&budget>=0&&budget<=100000,"BUDGET_RANGE");
 let scrap=budget,free=0,paid=0,blocked=0;const runTokenUsed=new Set();
 const rows=[];
 for(const e of s.events){
  let available=cadence==="PER_VICTORY"?1:(runTokenUsed.has(e.run)?0:1);
  let roundFree=0,roundPaid=0,roundBlocked=0;
  for(const part of e.parts){
   if(available){available=0;roundFree++;free++;if(cadence==="PER_RUN")runTokenUsed.add(e.run)}
   else if(scrap>=price){scrap-=price;roundPaid++;paid++}
   else{roundBlocked++;blocked++}
  }
  rows.push(Object.freeze({run:e.run,victory_id:e.victory_id,parts:e.parts.length,free:roundFree,paid:roundPaid,blocked:roundBlocked,spent:roundPaid*price,scrap_remaining:scrap}));
 }
 const attempts=s.events.reduce((n,e)=>n+e.parts.length,0);
 ensure(free+paid+blocked===attempts&&scrap===budget-paid*price,"MODEL_INVARIANT");
 return Object.freeze({scenario:s.id,cadence,price,budget,attempts,free,paid,blocked,spent:paid*price,remaining:scrap,rows:Object.freeze(rows)});
}
export function compare(s,{price=70,budget=250}={}){
 const victory=simulate(s,"PER_VICTORY",{price,budget});
 const run=simulate(s,"PER_RUN",{price,budget});
 return Object.freeze({scenario:s.id,price,budget,per_victory:victory,per_run:run,delta:Object.freeze({free_victory_minus_run:victory.free-run.free,spent_run_minus_victory:run.spent-victory.spent,blocked_run_minus_victory:run.blocked-victory.blocked})});
}
const esc=x=>String(x).replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;").replace(/'/g,"&#39;");
const css=[
":root{font-family:system-ui,Segoe UI,sans-serif;color-scheme:dark;background:#101826;color:#f2f7ff;--panel:#1a2b3d;--muted:#c6d3e0;--line:#64829c}",
"*{box-sizing:border-box}body{margin:0;background:linear-gradient(130deg,#143343,#101826 66%);min-height:100vh;line-height:1.5}",
"main{max-width:1030px;margin:auto;padding:clamp(16px,3vw,35px)}h1{font-size:clamp(1.7rem,4vw,2.5rem)}h2{font-size:1.35rem}",
".eyebrow{color:#8fe6dc;font-weight:800;letter-spacing:.14em;font-size:.77rem}.muted{color:var(--muted)}.caution{background:#332f2c;border-left:5px solid #ffd18a;padding:12px 16px;margin:20px 0}",
".panel{border:1px solid var(--line);background:var(--panel);border-radius:15px;padding:clamp(14px,3vw,24px);margin:16px 0}",
".filters{display:flex;flex-wrap:wrap;gap:18px;align-items:end}.filters label{display:flex;flex-direction:column;gap:6px;font-weight:650;min-width:190px}",
"input,select{accent-color:#87e1e3;color:inherit;font:inherit;background:#203b51;border:1px solid #8caabd;border-radius:8px;padding:9px;min-height:44px}",
"input:focus-visible,select:focus-visible{outline:4px solid #ffdf88;outline-offset:3px}.cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:14px}",
".card{padding:15px;border:1px solid #789db7;background:#203648;border-radius:12px}.card strong{display:block;font-size:1.6rem}.table-wrap{overflow-x:auto}",
"table{border-collapse:collapse;width:100%;min-width:560px}th,td{text-align:left;border-bottom:1px solid #526d84;padding:10px}th{font-weight:800}caption{text-align:left;color:var(--muted);padding:8px 0}",
".bar-track{height:14px;background:#354b5e;border-radius:999px;overflow:hidden;max-width:450px}.bar-fill{height:100%;background:repeating-linear-gradient(45deg,#9edfe2,#9edfe2 7px,#398791 7px,#398791 14px)}",
".footer{font-size:.86rem;margin-top:24px}code{overflow-wrap:anywhere}@media(prefers-reduced-motion:reduce){*{animation:none!important;transition:none!important}}",
"@media print{body{background:white;color:black}.panel,.card,.caution{background:white;color:black;border-color:#555}.muted{color:#444}select,input{color:black}}"
].join("\n");
const data=JSON.stringify({scenarios:SCENARIOS}).replace(/</g,"\\u003c");
const script=[
"(()=>{'use strict';const data=DATA;",
"const $=id=>document.getElementById(id),pick=$('scenario'),price=$('price'),budget=$('budget'),pLabel=$('price-value'),bLabel=$('budget-value'),summary=$('summary'),tbody=$('events'),delta=$('delta'),graph=$('graph');",
"function score(s,cadence,amount,money){let free=0,paid=0,blocked=0,scrap=money;const used=new Set(),rows=[];for(const event of s.events){let token=cadence==='PER_VICTORY'?1:(used.has(event.run)?0:1),f=0,p=0,b=0;for(const part of event.parts){if(token){token=0;f++;free++;if(cadence==='PER_RUN')used.add(event.run)}else if(scrap>=amount){scrap-=amount;p++;paid++}else{b++;blocked++}}rows.push({id:event.victory_id,free:f,paid:p,blocked:b,spent:p*amount})}return{free,paid,blocked,spent:paid*amount,remaining:scrap,rows}}",
"function cell(tr,value){const c=document.createElement('td');c.textContent=String(value);tr.appendChild(c)}",
"function draw(){const s=data.scenarios.find(x=>x.id===pick.value),p=Number(price.value),b=Number(budget.value);if(!s||!Number.isInteger(p)||!Number.isInteger(b))return;",
"pLabel.textContent=p+' złomu / płatna odbudowa';bLabel.textContent=b+' złomu na start';const win=score(s,'PER_VICTORY',p,b),run=score(s,'PER_RUN',p,b);",
"summary.replaceChildren();for(const [name,obj] of [['Darmowa na zwycięstwo',win],['Darmowa na run',run]]){const div=document.createElement('div');div.className='card';",
"const heading=document.createElement('h3');heading.textContent=name;const stat=document.createElement('strong');stat.textContent='Zapłacono: '+obj.spent;const note=document.createElement('p');note.textContent='Gratis '+obj.free+' · płatne '+obj.paid+' · bez środków '+obj.blocked+' · pozostało '+obj.remaining;div.append(heading,stat,note);summary.appendChild(div)}",
"delta.textContent='Różnica kosztu (run − zwycięstwo): '+(run.spent-win.spent)+' złomu. Różnica zablokowanych prób: '+(run.blocked-win.blocked)+'. To projekcja, nie rzeczywisty wynik balansu.';",
"tbody.replaceChildren();for(let i=0;i<s.events.length;i++){const tr=document.createElement('tr');for(const v of [s.events[i].run,s.events[i].victory_id,s.events[i].parts.length,win.rows[i].free,run.rows[i].free,win.rows[i].blocked,run.rows[i].blocked])cell(tr,v);tbody.appendChild(tr)}",
"const all=s.events.reduce((n,e)=>n+e.parts.length,0);graph.style.width=(all?100*win.free/all:0)+'%';graph.setAttribute('aria-label','Udział darmowych odbudów w wariancie na zwycięstwo: '+win.free+' z '+all)}",
"for(const e of [pick,price,budget])e.addEventListener('input',draw);draw();})();"
].join("\n").replace("DATA",data);
export function render(){
 const pin=crypto.createHash("sha256").update(script).digest("base64");
 return ['<!doctype html><html lang="pl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">',
 '<meta http-equiv="Content-Security-Policy" content="default-src &apos;none&apos;; script-src &apos;sha256-'+pin+'&apos;; style-src &apos;unsafe-inline&apos;; connect-src &apos;none&apos;; img-src &apos;none&apos;; base-uri &apos;none&apos;; form-action &apos;none&apos;">',
 '<title>B01-13 — ekonomia odbudów, porównanie hipotez</title><style>'+css+'</style></head><body><main>',
 '<p class="eyebrow">B01-13 · WAVE20 · OFFLINE GAME BALANCE LAB</p><h1>Ekonomia odbudów — która częstotliwość?</h1>',
 '<p class="muted">Porównanie dwóch niezatwierdzonych hipotez: jeden darmowy przydział na unikalne zwycięstwo albo tylko jeden na run. Koszt płatnej odbudowy jest parametrem, nie oficjalną ceną gry.</p>',
 '<aside class="caution"><strong>Wyniki symulacji, nie badań:</strong> 0 prawdziwych graczy, 0 testów natywnych Unity/PhysX, brak implementacji autorytatywnego ECON lub Save/Load. Model nie dowodzi odporności na rollback, współbieżność ani manipulację zapisem.</aside>',
 '<section class="panel" aria-labelledby="controls-title"><h2 id="controls-title">Parametry scenariusza</h2><div class="filters">',
 '<label>Scenariusz<select id="scenario">',
 ...SCENARIOS.map(s=>'<option value="'+esc(s.id)+'">'+esc(s.label)+'</option>'),
 '</select></label><label>Cena odbudowy (hipoteza)<input id="price" type="range" min="20" max="150" step="10" value="70"><output id="price-value" for="price"></output></label>',
 '<label>Budżet startowy (hipoteza)<input id="budget" type="range" min="0" max="600" step="25" value="250"><output id="budget-value" for="budget"></output></label>',
 '</div></section><section class="panel" aria-labelledby="results-title"><h2 id="results-title">Wyniki modelu</h2>',
 '<div id="summary" class="cards" aria-live="polite"></div><p id="delta" role="status" aria-live="polite"></p>',
 '<p class="muted">Udział darmowych odbudów (na zwycięstwo):</p><div class="bar-track"><div class="bar-fill" id="graph" role="img"></div></div>',
 '<div class="table-wrap"><table><caption>Przebieg zdarzeń modelu; brak zapisu w grze</caption><thead><tr><th scope="col">Run</th><th scope="col">Zwycięstwo</th><th scope="col">Części</th><th scope="col">Gratis / zwycięstwo</th><th scope="col">Gratis / run</th><th scope="col">Brak środków / zwycięstwo</th><th scope="col">Brak środków / run</th></tr></thead><tbody id="events"></tbody></table></div></section>',
 '<footer class="footer muted"><p>Wariant „na zwycięstwo” przydziela jeden darmowy koszt na każdy unikalny zdarzeniowy sukces; „na run” przydziela tylko jeden na cały run. Zawsze obowiązuje lokalny przykładowy budżet, brak rzeczywistego ECON. Oba warianty wymagają później testów graczy i decyzji projektowej.</p>',
 '<p>Frozen game #510 '+GAME+' · Save #556 '+SAVE+' · host #596 '+R2_HOST+' · ECON #252 '+FROZEN_ECON+' · Codex ACK NONE</p></footer>',
 '</main><script>'+script+'</script></body></html>'].join("\n");
}
export function write(file){
 ensure(path.isAbsolute(file),"ABSOLUTE_OUTPUT");
 for(let p=fs.realpathSync(path.dirname(file));;p=path.dirname(p)){ensure(!fs.existsSync(path.join(p,".git")),"GIT_OUTPUT");if(path.dirname(p)===p)break}
 const fd=fs.openSync(file,fs.constants.O_CREAT|fs.constants.O_EXCL|fs.constants.O_WRONLY|(fs.constants.O_NOFOLLOW||0),0o600);
 try{const html=render();fs.writeFileSync(fd,html,"utf8");fs.fsyncSync(fd);return{output:file,bytes:Buffer.byteLength(html),sha256:crypto.createHash("sha256").update(html).digest("hex"),native_unity:"NOT_RUN",real_participants:0,source_contract:"HYPOTHESIS_ONLY"}}finally{fs.closeSync(fd)}
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
 try{ensure(process.argv.length===4&&process.argv[2]==="--output","ARGS");console.log(JSON.stringify(write(process.argv[3])))}catch(e){console.error("FAIL_CLOSED",String(e.message||e));process.exitCode=2}
}
