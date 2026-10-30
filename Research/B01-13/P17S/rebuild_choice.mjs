#!/usr/bin/env node
// B01-13 PASS17 W21: standalone interactive garage rebuild planning UX rehearsal.
// Explicitly not Unity, native ECON security, an authenticated win, or a human study.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {pathToFileURL} from 'node:url';
export const PACKAGE='B01-13-REBUILD-CHOICE-REHEARSAL-001/R1';
export const PINS=Object.freeze({
 game:'d94400d95d13db7f27e1533df05c9292d5380431',
 save:'33094f154c7b91119b36d58aab285e6e83c5de57',
 host:'efb2b06eafedab5469b57f163152d96aca3c5915',
 econ:'12572117a3dce369257feabc57021cde58023369',
 selectedR4:'09c1809b73324a6d31c13e5d0143de62ac1e3ca6'
});
export const CADENCES=Object.freeze(['PER_WIN','PER_RUN']);
export const VEHICLES=Object.freeze({
 LIGHT:Object.freeze([
  {id:'L-C',label:'Rdzeń',hp:100,essential:true},
  {id:'L-W',label:'Koło',hp:60,essential:true},
  {id:'L-G',label:'Broń',hp:0,essential:false},
  {id:'L-A',label:'Pancerz',hp:0,essential:false},
  {id:'L-B',label:'Akumulator',hp:0,essential:false}
 ]),
 HEAVY:Object.freeze([
  {id:'H-C',label:'Rdzeń',hp:100,essential:true},
  {id:'H-D',label:'Napęd',hp:0,essential:true},
  {id:'H-G',label:'Działo',hp:0,essential:false},
  {id:'H-A',label:'Pancerz',hp:0,essential:false},
  {id:'H-S',label:'Wspornik',hp:80,essential:false}
 ])
});
const requireValid=(yes,why)=>{if(!yes)throw Error(why)};
function copy(v){return JSON.parse(JSON.stringify(v))}
export function initial(vehicle='LIGHT',cadence='PER_WIN'){
 requireValid(Object.hasOwn(VEHICLES,vehicle),'VEHICLE');
 requireValid(CADENCES.includes(cadence),'CADENCE');
 return {schema:PACKAGE,vehicle,cadence,price:70,cash:100,freeUsed:false,wins:0,seq:0,seen:[],tickets:[],loanerAvailable:true,parts:VEHICLES[vehicle].map(p=>({...p,status:p.hp===0?'DESTROYED':'READY'})),last:'Przygotuj kolejkę odbudowy w garażu. To symulacja, nie stan gry.'};
}
export function canDeploy(s){return s.parts.every(p=>!p.essential||p.status==='READY')}
export function checkState(s){
 requireValid(s&&s.schema===PACKAGE&&Object.hasOwn(VEHICLES,s.vehicle)&&CADENCES.includes(s.cadence),'STATE_ID');
 requireValid(Number.isInteger(s.cash)&&s.cash>=0&&s.cash<=100,'CURRENCY');
 requireValid(Number.isInteger(s.price)&&s.price>=1&&Number.isInteger(s.wins)&&s.wins>=0&&Number.isInteger(s.seq)&&s.seq>=0,'COUNTERS');
 requireValid(typeof s.freeUsed==='boolean'&&typeof s.loanerAvailable==='boolean','FLAGS');
 requireValid(Array.isArray(s.parts)&&s.parts.length===VEHICLES[s.vehicle].length&&Array.isArray(s.tickets)&&Array.isArray(s.seen),'COLLECTIONS');
 requireValid(new Set(s.seen).size===s.seen.length&&s.seen.every(id=>/^DEMO-[0-9]{3}$/.test(id)),'LEDGER_ID');
 requireValid(new Set(s.tickets.map(t=>t.partId)).size===s.tickets.length,'TICKET_DUP');
 for(let i=0;i<s.parts.length;i++){
  const p=s.parts[i],base=VEHICLES[s.vehicle][i];
  requireValid(p.id===base.id&&p.label===base.label&&p.essential===base.essential,'PART_ID');
  requireValid(['READY','DESTROYED','REBUILDING'].includes(p.status)&&Number.isInteger(p.hp)&&p.hp>=0&&p.hp<=100,'PART_STATUS');
  requireValid((p.status==='READY')===(p.hp>0),'PART_HP');
 }
 for(const t of s.tickets){
  requireValid(s.parts.some(p=>p.id===t.partId&&p.status==='REBUILDING'),'TICKET_OWNER');
  requireValid(Number.isInteger(t.queuedAtWin)&&t.queuedAtWin>=0&&t.queuedAtWin<=s.wins&&[0,s.price].includes(t.cost),'TICKET_COST');
 }
 return s;
}
export function act(input,action){
 checkState(input);
 requireValid(action&&typeof action==='object'&&['QUEUE','WIN','LOSS','LOANER_WIN','LOANER_LOSS','REPLAY_LAST'].includes(action.type),'ACTION');
 const s=copy(input),part=s.parts.find(x=>x.id===action.partId);
 if(action.type==='QUEUE'){
  requireValid(typeof action.partId==='string'&&part,'PART_NOT_FOUND');
  if(part.status!=='DESTROYED'){s.last='Odmowa: ta część nie jest zniszczona (0 HP) albo już oczekuje.';return s}
  const free=!s.freeUsed;
  if(!free&&s.cash<s.price){s.last='Brak środków: odbudowa kosztuje '+s.price+' złomu; pozostało '+s.cash+'.';return s}
  if(free)s.freeUsed=true;else s.cash-=s.price;
  part.status='REBUILDING';s.tickets.push({partId:part.id,cost:free?0:s.price,queuedAtWin:s.wins});
  s.last=part.label+': REBUILDING / NIEDOSTĘPNA. '+(free?'Zużyto jeden darmowy przydział.':'Zapłacono '+s.price+' złomu.')+' Wymagane NASTĘPNE rzeczywiste zwycięstwo.';
  return checkState(s);
 }
 if(action.type==='REPLAY_LAST'){
  if(!s.seen.length){s.last='Brak lokalnego zdarzenia do powtórzenia.';return s}
  return act(s,{type:'WIN',eventId:s.seen[s.seen.length-1]});
 }
 requireValid(typeof action.eventId==='string'&&/^DEMO-[0-9]{3}$/.test(action.eventId),'EVENT_ID');
 if(s.seen.includes(action.eventId)){s.last='REPLAY_REJECTED: zdarzenie '+action.eventId+' już przetworzono. Brak ponownej nagrody/odbudowy.';return checkState(s)}
 const hypothetical=action.type.startsWith('LOANER_');
 if(hypothetical){
  if(canDeploy(s)||!s.loanerAvailable){s.last='LOANER_DENIED: alternatywne wyzwanie dostępne tylko raz przy zablokowanym starcie.';return s}
 }else if(!canDeploy(s)){s.last='SOFTLOCK_RISK: moduł krytyczny 0 HP lub REBUILDING. Normalny start niedostępny. Pojazd zastępczy NIEZATWIERDZONY.';return s}
 s.seen.push(action.eventId);s.seq++;
 if(hypothetical)s.loanerAvailable=false;
 if(action.type==='LOSS'||action.type==='LOANER_LOSS'){
  s.last='PORAŻKA: nie ukończono żadnej kolejki, darmowy przydział nie odnawia się.'+(hypothetical?' Hipotetyczny pojazd zastępczy zużyty.':'');
  return checkState(s);
 }
 s.wins++;
 let completed=0;
 for(const ticket of s.tickets){
  if(ticket.queuedAtWin<s.wins){const p=s.parts.find(x=>x.id===ticket.partId);p.hp=100;p.status='READY';completed++}
 }
 s.tickets=s.tickets.filter(t=>t.queuedAtWin>=s.wins);
 if(s.cadence==='PER_WIN')s.freeUsed=false;
 s.last=(hypothetical?'NIEZATWIERDZONY LOANER: ':'')+'Wygrano zdarzenie '+action.eventId+', ukończono '+completed+' wcześniejszych odbudów. '+(s.cadence==='PER_WIN'?'Kolejny darmowy przydział dostępny.':'Darmowy przydział na run nie odnawia się.');
 return checkState(s);
}
const style=[
":root{font-family:system-ui,Segoe UI,sans-serif;color-scheme:dark;background:#101927;color:#f2f6fd}",
"*{box-sizing:border-box}body{margin:0;background:linear-gradient(145deg,#193b49,#101927 65%);min-height:100vh;line-height:1.5}main{max-width:1060px;margin:auto;padding:clamp(15px,3vw,32px)}",
".eyebrow{font-size:.78rem;font-weight:800;letter-spacing:.12em;color:#99e9e4}h1{font-size:clamp(1.5rem,4vw,2.6rem);line-height:1.1}.muted{color:#c6d5e0}.warning{background:#372e2a;border-left:5px solid #ffd38a;padding:13px 17px;margin:16px 0}",
".panel{background:#1a3042;border:1px solid #6b8fa8;border-radius:14px;margin:14px 0;padding:clamp(14px,3vw,22px)}.row{display:flex;flex-wrap:wrap;align-items:center;gap:10px}.row>span{flex:1}",
".toolbar{display:flex;gap:12px;flex-wrap:wrap}label{display:grid;gap:6px}select{background:#264759;color:white;border:1px solid #aed3e1;padding:10px;min-height:44px;border-radius:8px;font:inherit}select:focus-visible,button:focus-visible{outline:4px solid #ffe69a;outline-offset:3px}",
".stats{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:9px}.stat{background:#264457;border:1px solid #63869b;border-radius:10px;padding:12px}.stat strong{display:block;font-size:1.45rem}",
".tiles{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:12px}.tile{border:2px solid #7494a8;border-radius:12px;background:#243d50;padding:13px}.tile[data-status=DESTROYED]{border-style:dashed;border-color:#ffbb92}.tile[data-status=REBUILDING]{border-style:dotted;border-color:#ebc0ff}",
".status{font-size:.9rem}.button{font:inherit;font-weight:700;cursor:pointer;border:1px solid #a0d4e7;background:#29566a;color:white;min-height:44px;padding:9px 13px;border-radius:8px}.button:disabled{opacity:.48;cursor:not-allowed}",
".button.danger{background:#663d39;border-color:#ffbda4}.button.hypothesis{background:#58426c;border-color:#d9baff}.button.light{background:#294d45;border-color:#a7f3d1}.tile button{width:100%;margin-top:7px}",
"#feedback{font-weight:750;min-height:64px;border-left:4px solid #8cdaca;padding:8px 12px;background:#263c4f}#risk{font-weight:700}",
".note{font-size:.88rem}code{overflow-wrap:anywhere}@media(prefers-reduced-motion:reduce){*{transition:none!important;animation:none!important}}@media print{body{background:white;color:black}.panel,.tile,.stat,.warning{background:white;color:black;box-shadow:none}.button{display:none}}"
].join("\n");
const js=[
"(()=>{'use strict';",
"const DATA=__FIXTURES__;",
...['copy','initial','canDeploy','checkState','act'].map(x=>'__FN_'+x+'__'),
"const by=id=>document.getElementById(id),tiles=by('tiles'),feedback=by('feedback'),risk=by('risk'),wallet=by('cash'),free=by('free'),won=by('wins'),waiting=by('waiting'),seq=by('seq'),vehicle=by('vehicle'),cadence=by('cadence'),battle=by('battle'),lose=by('lose'),loanerWin=by('loaner-win'),loanerLoss=by('loaner-loss'),replay=by('replay'),reset=by('reset');",
"let state=initial('LIGHT','PER_WIN');",
"function paint(){tiles.replaceChildren();wallet.textContent=state.cash+' złomu';free.textContent=state.freeUsed?'ZUŻYTY':'DOSTĘPNY';won.textContent=String(state.wins);waiting.textContent=String(state.tickets.length);seq.textContent=String(state.seen.length);feedback.textContent=state.last;",
"const blocked=!canDeploy(state);risk.textContent=blocked?'SOFTLOCK_RISK: nie można rozpocząć normalnego wyzwania przy niesprawnej części krytycznej. Naprawa w kolejce czeka na zwycięstwo. Brak zatwierdzonej polityki awaryjnej.':'Pojazd spełnia uproszczone założenia gotowości (model — nie Unity CanDeploy).';",
"battle.disabled=blocked;lose.disabled=blocked;loanerWin.disabled=!blocked||!state.loanerAvailable;loanerLoss.disabled=!blocked||!state.loanerAvailable;",
"for(const p of state.parts){const tile=document.createElement('section');tile.className='tile';tile.dataset.status=p.status;const h=document.createElement('h3');h.textContent=p.label+(p.essential?' · KRYTYCZNA':'');const meta=document.createElement('p');meta.className='status';meta.textContent='HP '+p.hp+' / '+p.status;",
"const button=document.createElement('button');button.type='button';button.className='button';button.dataset.partId=p.id;button.textContent=p.status==='DESTROYED'?'Zleć odbudowę 0 HP':p.status==='REBUILDING'?'Oczekuje na zwycięstwo':'Sprawna — brak odbudowy';button.disabled=p.status!=='DESTROYED';button.setAttribute('aria-label','Zlecenie '+p.label+', '+p.status);button.addEventListener('click',()=>{state=act(state,{type:'QUEUE',partId:p.id});paint()});tile.append(h,meta,button);tiles.appendChild(tile)}}",
"function outcome(type){const id='DEMO-'+String(state.seq+1).padStart(3,'0');state=act(state,{type,eventId:id});paint()}",
"battle.addEventListener('click',()=>outcome('WIN'));lose.addEventListener('click',()=>outcome('LOSS'));loanerWin.addEventListener('click',()=>outcome('LOANER_WIN'));loanerLoss.addEventListener('click',()=>outcome('LOANER_LOSS'));",
"replay.addEventListener('click',()=>{state=act(state,{type:'REPLAY_LAST'});paint()});reset.addEventListener('click',()=>{state=initial(vehicle.value,cadence.value);paint()});",
"vehicle.addEventListener('change',()=>{state=initial(vehicle.value,cadence.value);paint()});cadence.addEventListener('change',()=>{state=initial(vehicle.value,cadence.value);paint()});",
"paint();})();"
].join("\n").replace("__FIXTURES__",JSON.stringify({PACKAGE,CADENCES,VEHICLES}).replace(/</g,"\\u003c"))
.replace("__FN_copy__",copy.toString())
.replace("__FN_initial__",initial.toString())
.replace("__FN_canDeploy__",canDeploy.toString())
.replace("__FN_checkState__",checkState.toString())
.replace("__FN_act__",act.toString())
.replace("const DATA=", "const DATA="); // original functions rely on PACKAGE,CADENCES,VEHICLES,requireValid; declared below
const embedded=js.replace("const DATA="+JSON.stringify({PACKAGE,CADENCES,VEHICLES}).replace(/</g,"\\u003c")+";","const DATA="+JSON.stringify({PACKAGE,CADENCES,VEHICLES}).replace(/</g,"\\u003c")+";const PACKAGE=DATA.PACKAGE,CADENCES=DATA.CADENCES,VEHICLES=DATA.VEHICLES;"const requireValid="+requireValid.toString()+";");
const escapeHtml=x=>String(x).replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;");
export function render(){
 const hash=crypto.createHash('sha256').update(embedded).digest('base64');
 return ['<!doctype html><html lang="pl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">',
 '<meta http-equiv="Content-Security-Policy" content="default-src &apos;none&apos;; script-src &apos;sha256-'+hash+'&apos;; style-src &apos;unsafe-inline&apos;; connect-src &apos;none&apos;; base-uri &apos;none&apos;; img-src &apos;none&apos;; form-action &apos;none&apos;">',
 '<title>B01-13 W21 — decyzje o odbudowie robota</title><style>'+style+'</style></head><body><main>',
 '<p class="eyebrow">B01-13 · GRYWALNY PROTOTYP UX · W21</p><h1>Warsztat odbudowy — wybierz, zapłać i przetrwaj</h1>',
 '<p class="muted">Przećwicz kolejkę odbudowy 0 HP, wykorzystanie darmowego przydziału, skutki porażki, kolejne zwycięstwo i ryzyko utraty mobilności. Wyniki to wyłącznie lokalna symulacja.</p>',
 '<aside class="warning"><strong>Granice dowodowe:</strong> 0 rzeczywistych uczestników. Brak Unity/PhysX, live ECON, trwałego zapisu, prawdziwych uwierzytelnionych zwycięstw i ochrony przed fizycznym rollbackiem. Dodatkowy pojazd zastępczy jest HIPOTEZĄ NIEZATWIERDZONĄ, a nie funkcją gry.</aside>',
 '<section class="panel" aria-label="Konfiguracja wersji modelu"><div class="toolbar"><label>Pojazd<select id="vehicle"><option value="LIGHT">LIGHT · części opcjonalne zniszczone</option><option value="HEAVY">HEAVY · zniszczony napęd krytyczny</option></select></label>',
 '<label>Hipoteza darmowej odbudowy<select id="cadence"><option value="PER_WIN">Jedna na zwycięstwo</option><option value="PER_RUN">Jedna na run</option></select></label><button class="button light" id="reset" type="button">Reset lokalnego ćwiczenia</button></div></section>',
 '<section class="panel" aria-label="Zasoby i kolejka"><div class="stats"><div class="stat">Portfel<strong id="cash"></strong></div><div class="stat">Darmowy przydział<strong id="free"></strong></div><div class="stat">Wygrane<strong id="wins"></strong></div><div class="stat">W kolejce<strong id="waiting"></strong></div><div class="stat">Zdarzenia lokalne<strong id="seq"></strong></div></div>',
 '<p id="risk" role="status" aria-live="polite"></p><p id="feedback" role="status" aria-live="polite"></p></section>',
 '<section class="panel" aria-labelledby="parts-title"><h2 id="parts-title">Moduły robota · 0 HP ≠ dostępne</h2><div id="tiles" class="tiles"></div></section>',
 '<section class="panel" aria-labelledby="fight-title"><h2 id="fight-title">Symuluj następne wyzwanie</h2><p class="muted">Kolejka kończy się dopiero po NASTĘPNYM zwycięstwie; porażka nie przyspiesza napraw. DEMO-xxx to lokalne identyfikatory, nie zweryfikowane ID gry.</p>',
 '<div class="row"><button class="button" id="battle" type="button">Symuluj zwycięstwo</button><button class="button danger" id="lose" type="button">Symuluj porażkę</button><button class="button" id="replay" type="button">Powtórz ostatnie ID (test odmowy)</button></div>',
 '<p class="muted">Eksperyment awaryjny (NIEZATWIERDZONY; tylko dla zablokowanego startu, jednorazowy):</p><div class="row"><button class="button hypothesis" id="loaner-win" type="button">Hipotetyczny pojazd zastępczy · zwycięstwo</button><button class="button hypothesis" id="loaner-loss" type="button">Hipotetyczny pojazd zastępczy · porażka</button></div></section>',
 '<footer class="note muted">Zabezpieczenie ID w tym ćwiczeniu jest tylko efemeryczne. Źródło ECON #252 '+PINS.econ+' · host R2 #596 '+PINS.host+' · selected R4 #647 '+PINS.selectedR4+' · #652/#644/#604 ALT_HOLD. Brak Codex ACK, brak G1 i G2.</footer>',
 '</main><script>'+embedded+'</script></body></html>'].join("\n");
}
export function emit(file){
 requireValid(path.isAbsolute(file),'OUTPUT_ABSOLUTE');
 for(let d=fs.realpathSync(path.dirname(file));;d=path.dirname(d)){requireValid(!fs.existsSync(path.join(d,'.git')),'GIT_OUTPUT_DENIED');if(path.dirname(d)===d)break}
 const fd=fs.openSync(file,fs.constants.O_WRONLY|fs.constants.O_CREAT|fs.constants.O_EXCL|(fs.constants.O_NOFOLLOW||0),0o600);
 try{const h=render();fs.writeFileSync(fd,h,'utf8');fs.fsyncSync(fd);return{output:file,sha256:crypto.createHash('sha256').update(h).digest('hex'),bytes:Buffer.byteLength(h),native:'NOT_RUN',human_sessions:0,real_econ:'NOT_VERIFIED'}}finally{fs.closeSync(fd)}
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
 try{requireValid(process.argv.length===4&&process.argv[2]==='--output','ARGS');console.log(JSON.stringify(emit(process.argv[3])))}catch(err){console.error('FAIL_CLOSED',String(err.message||err));process.exitCode=2}
}
