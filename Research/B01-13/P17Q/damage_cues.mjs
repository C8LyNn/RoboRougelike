#!/usr/bin/env node
// B01-13 W19 — playable offline robot-part damage cue UX microtutorial.
// NOT Unity, actual human study, canonical Save/Load or PhysX.
import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import {pathToFileURL} from "node:url";
export const ID="B01-13-DAMAGE-CUE-TUTORIAL-001/R1";
export const FROZEN=Object.freeze({game:"d94400d95d13db7f27e1533df05c9292d5380431",save:"33094f154c7b91119b36d58aab285e6e83c5de57",host:"efb2b06eafedab5469b57f163152d96aca3c5915",alt:"#604 ALT_HOLD"});
export const SIGNAL=Object.freeze({OK:{text:"Sprawna",glyph:"✓ ◇",border:"solid"},DAMAGED:{text:"Uszkodzona",glyph:"! △",border:"dashed"},DETACHED:{text:"Oderwana",glyph:"× ○",border:"dotted"}});
const names=["Rdzeń","Koło lewe","Koło prawe","Pancerz","Napęd","Łącznik","Akumulator","Wirnik","Blok boczny","Sterowanie","Wspornik","Silnik"];
const specs=[
 ["L01","LIGHT","Wskaż oderwaną część","DETACHED",[0,1,3,4],["OK","DAMAGED","DETACHED","OK"]],
 ["H01","HEAVY","Wskaż część do naprawy","DAMAGED",[0,5,7,8],["OK","DETACHED","OK","DAMAGED"]],
 ["L02","LIGHT","Wskaż sprawną część","OK",[2,3,4,6],["DAMAGED","DETACHED","OK","DAMAGED"]],
 ["H02","HEAVY","Wskaż oderwaną część","DETACHED",[8,9,10,11],["DAMAGED","OK","DETACHED","DAMAGED"]],
 ["L03","LIGHT","Wskaż część do naprawy","DAMAGED",[1,2,6,4],["DETACHED","OK","DAMAGED","OK"]],
 ["H03","HEAVY","Wskaż sprawną część","OK",[5,7,9,11],["DETACHED","DAMAGED","OK","DETACHED"]]
];
const guard=(ok,msg)=>{if(!ok)throw Error(msg)};
export const ROUNDS=Object.freeze(specs.map(([id,vehicle,prompt,target,indices,states])=>Object.freeze({id,vehicle,prompt,target,parts:Object.freeze(indices.map((x,i)=>Object.freeze({id:id+"-P"+(i+1),name:names[x],state:states[i]})))})));
export function validate(r){
 guard(r&&typeof r==="object"&&Object.keys(r).sort().join("|")==="id|parts|prompt|target|vehicle","SCHEMA");
 guard(/^[LH][0-9]{2}$/.test(r.id)&&r.vehicle===(r.id.startsWith("L")?"LIGHT":"HEAVY"),"VEHICLE");
 guard(Object.hasOwn(SIGNAL,r.target)&&typeof r.prompt==="string"&&r.prompt.length>=10,"TARGET");
 guard(Array.isArray(r.parts)&&r.parts.length===4&&r.parts.filter(x=>x.state===r.target).length===1,"UNIQUE_TARGET");
 guard(new Set(r.parts.map(x=>x.name)).size===4,"DUPLICATE_NAME");
 for(let i=0;i<4;i++)guard(r.parts[i].id===r.id+"-P"+(i+1)&&names.includes(r.parts[i].name)&&Object.hasOwn(SIGNAL,r.parts[i].state),"PART");
 return r;
}
ROUNDS.forEach(validate);
export function at(i){guard(Number.isInteger(i)&&i>=0&&i<ROUNDS.length,"INDEX");return ROUNDS[i]}
export function choose(i,partId){const round=at(i);guard(typeof partId==="string"&&round.parts.some(x=>x.id===partId),"SELECT");const chosen=round.parts.find(x=>x.id===partId),target=round.parts.find(x=>x.state===round.target);return Object.freeze({round:round.id,correct:chosen.id===target.id,selected:partId,correct_part:target.id,chosen_state:chosen.state,target_state:round.target})}
const esc=s=>String(s).replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;").replace(/'/g,"&#39;");
const css=[
":root{font-family:system-ui,Segoe UI,sans-serif;color-scheme:dark;background:#0c1823;color:#f3f8ff}*{box-sizing:border-box}",
"body{margin:0;background:linear-gradient(140deg,#204357,#0c1823 60%);min-height:100vh;line-height:1.5}main{max-width:1050px;margin:auto;padding:clamp(16px,4vw,40px)}",
"h1{font-size:clamp(1.8rem,4vw,2.8rem);margin:0}.eyebrow{color:#94f3e7;letter-spacing:.1em;font-size:.8rem;font-weight:800}.muted{color:#c6d7e3}",
".notice{border-left:5px solid #ffd08a;padding:14px;background:#38322d;margin:20px 0}.panel{border:1px solid #7195a9;background:#152a3a;border-radius:17px;padding:clamp(16px,3vw,28px);box-shadow:0 15px 35px #0005}",
".top{display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap}.target{font-size:1.4rem;font-weight:800}progress{width:100%;height:14px;accent-color:#8ef0dd}",
".legend{display:flex;flex-wrap:wrap;gap:7px}.key{border:1px solid #94c4d8;background:#234052;padding:6px 10px;border-radius:8px}",
"#parts{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:13px;margin:20px 0}.part{appearance:none;background:#203b4b;color:#f9fcff;min-height:142px;border:3px solid #9ce4e6;border-radius:13px;cursor:pointer;text-align:left;padding:16px;display:flex;flex-direction:column;gap:9px}",
".part[data-state=DAMAGED]{border-style:dashed;border-color:#ffd08c}.part[data-state=DETACHED]{border-style:dotted;border-color:#f5b9ef}.part:hover{background:#34536a}",
".part:focus-visible,button:focus-visible{outline:4px solid #ffe79a;outline-offset:3px}.part .glyph{font-size:1.8rem;font-weight:800}.part .name{font-weight:800}.part .state{color:#e4f1fa;font-size:.9rem}",
".part[data-mode=symbol] .state{position:absolute;clip-path:inset(50%);height:1px;width:1px;overflow:hidden;white-space:nowrap}",
".part[data-result=right]{outline:4px solid #a9f5c3}.part[data-result=wrong]{outline:4px solid #ff9393}.part:disabled{cursor:default}",
".actions{display:flex;flex-wrap:wrap;gap:9px}.action{font:inherit;font-weight:700;cursor:pointer;background:#285367;border:1px solid #9bdaeb;color:white;border-radius:8px;min-height:44px;padding:9px 15px}.action:disabled{opacity:.5;cursor:not-allowed}",
"#feedback{min-height:54px;font-weight:700}kbd{padding:2px 5px;background:#274253;border:1px solid #829eaf;border-radius:4px}",
"@media(min-width:750px){#parts{grid-template-columns:repeat(4,minmax(0,1fr))}}@media(prefers-reduced-motion:reduce){*{animation:none!important;transition:none!important}}",
"@media print{body{background:white;color:black}.panel,.notice,.part{background:white;color:black;box-shadow:none}.state{position:static!important;clip-path:none!important;height:auto!important;width:auto!important;color:black!important}.actions{display:none}}"
].join("\n");
const payload=JSON.stringify({rounds:ROUNDS,signals:SIGNAL}).replace(/</g,"\\u003c");
const script=[
"(()=>{'use strict';const data=DATA;",
"const el=id=>document.getElementById(id),grid=el('parts'),prompt=el('prompt'),progress=el('progress'),step=el('step'),message=el('feedback'),next=el('next'),again=el('again'),mode=el('mode'),summary=el('summary');",
"let index=0,selected=null,full=true,score=0;",
"function stateLabel(s){return data.signals[s].text}",
"function answer(id){if(selected!==null)return;const r=data.rounds[index],p=r.parts.find(x=>x.id===id);if(!p)return;",
"selected=id;const goal=r.parts.find(x=>x.state===r.target),win=p.id===goal.id;if(win)score++;",
"for(const button of grid.querySelectorAll('button')){button.disabled=true;if(button.dataset.id===id)button.dataset.result=win?'right':'wrong'}",
"message.textContent=win?'Poprawnie: '+p.name+' ('+stateLabel(p.state)+').':'Wybrano '+p.name+' ('+stateLabel(p.state)+'). Prawidłowo: '+goal.name+' ('+stateLabel(goal.state)+').';next.disabled=false;next.focus()}",
"function draw(){const r=data.rounds[index];selected=null;grid.replaceChildren();progress.max=data.rounds.length;progress.value=index;step.textContent='Próba '+(index+1)+' / '+data.rounds.length+' · '+r.vehicle;prompt.textContent=r.prompt;summary.textContent='';message.textContent='Wybierz moduł lub naciśnij klawisz 1–4.';next.textContent=index===data.rounds.length-1?'Podsumowanie':'Dalej';next.disabled=true;",
"r.parts.forEach((p,i)=>{const b=document.createElement('button');b.type='button';b.className='part';b.dataset.id=p.id;b.dataset.state=p.state;b.dataset.mode=full?'full':'symbol';b.setAttribute('aria-label',(i+1)+'. '+p.name+', '+stateLabel(p.state));",
"const g=document.createElement('span');g.className='glyph';g.textContent=data.signals[p.state].glyph;g.setAttribute('aria-hidden','true');const n=document.createElement('span');n.className='name';n.textContent=p.name;const s=document.createElement('span');s.className='state';s.textContent=stateLabel(p.state);b.append(g,n,s);b.addEventListener('click',()=>answer(p.id));grid.appendChild(b)})}",
"function finish(){selected='DONE';grid.replaceChildren();progress.value=data.rounds.length;prompt.textContent='Koniec treningu';step.textContent='Ukończono '+data.rounds.length+' / '+data.rounds.length;summary.textContent='Poprawne wskazania: '+score+' / '+data.rounds.length+'. To wynik lokalnego treningu, nie badania z ludźmi.';message.textContent='Nie zapisujemy ani nie przesyłamy danych.';next.disabled=true}",
"next.addEventListener('click',()=>{if(selected===null||selected==='DONE')return;if(index===data.rounds.length-1)finish();else{index++;draw()}});",
"again.addEventListener('click',()=>{index=0;score=0;draw()});",
"mode.addEventListener('click',()=>{full=!full;mode.textContent=full?'Tryb: opisy + symbole':'Tryb: tylko symbole';for(const b of grid.querySelectorAll('button'))b.dataset.mode=full?'full':'symbol'});",
"document.addEventListener('keydown',e=>{if(e.altKey||e.ctrlKey||e.metaKey||['INPUT','TEXTAREA','SELECT'].includes(document.activeElement?.tagName))return;",
"if(/^[1-4]$/.test(e.key)&&selected===null)answer(data.rounds[index].parts[Number(e.key)-1].id);else if(e.key.toLowerCase()==='n'&&!next.disabled)next.click();else if(e.key.toLowerCase()==='r')again.click();else if(e.key.toLowerCase()==='m')mode.click()});draw();})();"
].join("\n").replace("DATA",payload);
export function render(){
 const hash=crypto.createHash("sha256").update(script).digest("base64");
 return ['<!doctype html><html lang="pl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">',
 '<meta http-equiv="Content-Security-Policy" content="default-src &apos;none&apos;; script-src &apos;sha256-'+hash+'&apos;; style-src &apos;unsafe-inline&apos;; img-src &apos;none&apos;; connect-src &apos;none&apos;; base-uri &apos;none&apos;; form-action &apos;none&apos;">',
 '<title>RoboRougelike — trening rozpoznawania uszkodzeń</title><style>'+css+'</style></head><body><main>',
 '<p class="eyebrow">B01-13 · GRYWALNA MAK IETA UX · OFFLINE</p><h1>Rozpoznaj stan robota</h1>',
 '<p class="muted">Wskaż sprawne, uszkodzone lub oderwane moduły. To symulacja czytelności sygnałów, nie scena gry Unity ani działające Save/Load.</p>',
 '<aside class="notice"><strong>Ograniczenia:</strong> zero rzeczywistych uczestników. Unity/PhysX NOT_RUN. Save/Load NOT_VERIFIED. Brak telemetrii i zapisu danych.</aside>',
 '<section class="panel" aria-labelledby="training"><div class="top"><h2 id="training">Odczyt stanu części</h2><span id="step" class="muted"></span></div><progress id="progress" max="6" value="0" aria-label="Postęp prób"></progress>',
 '<p id="prompt" class="target"></p><div class="legend" aria-label="Legenda stanów">',
 ...Object.entries(SIGNAL).map(([k,v])=>'<span class="key">'+esc(v.glyph)+' · '+esc(v.text)+' · '+esc(v.border)+'</span>'),
 '</div><div id="parts" role="group" aria-label="Wybierz jedną z czterech części"></div><p id="feedback" role="status" aria-live="polite"></p><p id="summary" role="status" aria-live="polite"></p>',
 '<div class="actions"><button class="action" id="next" type="button" disabled>Dalej</button><button class="action" id="mode" type="button">Tryb: opisy + symbole</button><button class="action" id="again" type="button">Od początku</button></div>',
 '<p class="muted">Skróty: <kbd>1–4</kbd> wybór, <kbd>N</kbd> następna próba, <kbd>M</kbd> tryb, <kbd>R</kbd> reset. Klawiatura: Tab i Enter.</p></section>',
 '<footer class="muted">Wynik nie dowodzi dostępności ani rzeczywistej użyteczności. Sygnały: symbol + kształt ramki + opis.<p>Frozen #510 '+FROZEN.game+' / #556 '+FROZEN.save+' / #596 '+FROZEN.host+' / #604 ALT_HOLD</p></footer></main>',
 '<script>'+script+'</script></body></html>'].join("\n");
}
export function generate(file){
 guard(path.isAbsolute(file),"ABSOLUTE_OUTPUT");
 for(let d=fs.realpathSync(path.dirname(file));;d=path.dirname(d)){guard(!fs.existsSync(path.join(d,".git")),"GIT_OUTPUT");if(path.dirname(d)===d)break}
 const fd=fs.openSync(file,fs.constants.O_CREAT|fs.constants.O_EXCL|fs.constants.O_WRONLY|(fs.constants.O_NOFOLLOW||0),0o600);
 try{const content=render();fs.writeFileSync(fd,content,"utf8");fs.fsyncSync(fd);return{file,sha256:crypto.createHash("sha256").update(content).digest("hex"),rounds:ROUNDS.length,real_participants:0,native_unity:"NOT_RUN",live_save:"NOT_VERIFIED"}}finally{fs.closeSync(fd)}
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
 try{guard(process.argv.length===4&&process.argv[2]==="--output","ARGS");console.log(JSON.stringify(generate(process.argv[3])))}catch(e){console.error("FAIL_CLOSED",String(e.message||e));process.exitCode=2}
}
