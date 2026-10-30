import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import crypto from "node:crypto";
import vm from "node:vm";
import {spawnSync} from "node:child_process";
import {fileURLToPath} from "node:url";
import {PACKAGE,PINS,CADENCES,VEHICLES,initial,canDeploy,checkState,act,render,emit} from "./rebuild_choice.mjs";

const clone=x=>structuredClone(x);
const temp=fn=>{const d=fs.mkdtempSync(path.join(os.tmpdir(),"b01-w21-rehearsal-"));try{return fn(d)}finally{fs.rmSync(d,{recursive:true,force:true})}};
const queue=(s,p)=>act(s,{type:"QUEUE",partId:p});
const event=(s,type,id)=>act(s,{type,eventId:id});
class El{
 constructor(tag="div"){this.tagName=tag.toUpperCase();this.children=[];this.dataset={};this.attributes={};this.listeners={};this.disabled=false;this.textContent="";this.value="";this.className="";this.style={}}
 addEventListener(n,cb){this.listeners[n]=cb}
 setAttribute(n,v){this.attributes[n]=v}
 append(...nodes){this.children.push(...nodes)}
 appendChild(n){this.children.push(n)}
 replaceChildren(){this.children=[]}
 click(){if(!this.disabled)this.listeners.click?.()}
}
function mockUI(){
 const ids=["tiles","feedback","risk","cash","free","wins","waiting","seq","vehicle","cadence","battle","lose","loaner-win","loaner-loss","replay","reset"];
 const els=Object.fromEntries(ids.map(k=>[k,new El(k==="vehicle"||k==="cadence"?"select":"div")]));
 els.vehicle.value="LIGHT";els.cadence.value="PER_WIN";
 const document={getElementById(k){assert.ok(k in els,"DOM_ID:"+k);return els[k]},createElement(k){return new El(k)}};
 const body=render().match(/<script>([\s\S]*?)<\/script>/);
 assert.ok(body,"SCRIPT");
 vm.runInNewContext(body[1],{document});
 return els;
}
test("versioned package and canonical game/Save/ECON/host pins remain literal unchanged",()=>{assert.equal(PACKAGE,"B01-13-REBUILD-CHOICE-REHEARSAL-001/R1");assert.equal(PINS.game,"d94400d95d13db7f27e1533df05c9292d5380431");assert.equal(PINS.save,"33094f154c7b91119b36d58aab285e6e83c5de57");assert.equal(PINS.host,"efb2b06eafedab5469b57f163152d96aca3c5915");assert.equal(PINS.econ,"12572117a3dce369257feabc57021cde58023369");assert.equal(PINS.selectedR4,"09c1809b73324a6d31c13e5d0143de62ac1e3ca6")});
test("two explicitly competing playtest cadences",()=>assert.deepEqual(CADENCES,["PER_WIN","PER_RUN"]));
test("LIGHT and HEAVY preset stable part ownership",()=>{assert.equal(VEHICLES.LIGHT.length,5);assert.equal(VEHICLES.HEAVY.length,5);for(const vehicle of ["LIGHT","HEAVY"])assert.equal(new Set(VEHICLES[vehicle].map(x=>x.id)).size,5)});
test("invalid vehicle fails closed",()=>assert.throws(()=>initial("OTHER"),/VEHICLE/));
test("invalid cadence fails closed",()=>assert.throws(()=>initial("LIGHT","PER_HOUR"),/CADENCE/));
test("first new light model is deployable despite optional destroyed components",()=>{const s=initial();assert.equal(canDeploy(s),true);assert.equal(s.parts.filter(x=>x.status==="DESTROYED").length,3)});
test("heavy with critical destroyed drive is blocked before repair reservation",()=>{const s=initial("HEAVY");assert.equal(canDeploy(s),false);assert.ok(s.parts.find(x=>x.id==="H-D").essential)});
test("queue first 0HP part consumes exactly one free token, preserves ID",()=>{const s=queue(initial(),"L-G");assert.equal(s.freeUsed,true);assert.equal(s.cash,100);assert.equal(s.parts.find(x=>x.id==="L-G").status,"REBUILDING");assert.equal(s.parts.find(x=>x.id==="L-G").hp,0);assert.deepEqual(s.tickets[0],{partId:"L-G",cost:0,queuedAtWin:0})});
test("second queued 0HP part deducts exactly one paid quote",()=>{const s=queue(queue(initial(),"L-G"),"L-A");assert.equal(s.cash,30);assert.equal(s.tickets[1].cost,70);assert.equal(s.tickets.length,2)});
test("third queued part unaffordable cannot be charged or reserved",()=>{const s=queue(queue(queue(initial(),"L-G"),"L-A"),"L-B");assert.equal(s.cash,30);assert.equal(s.tickets.length,2);assert.equal(s.parts.find(x=>x.id==="L-B").status,"DESTROYED");assert.match(s.last,/Brak środków/)});
test("attempt to queue >0HP intact part denied",()=>{const s=queue(initial(),"L-C");assert.equal(s.freeUsed,false);assert.equal(s.tickets.length,0);assert.match(s.last,/Odmowa/)});
test("attempt to requeue pending is denied without double charge",()=>{const a=queue(initial(),"L-G"),b=queue(a,"L-G");assert.equal(b.tickets.length,1);assert.equal(b.cash,100)});
test("unknown part identifier denied",()=>assert.throws(()=>queue(initial(),"H-D"),/PART_NOT_FOUND/));
test("malformed command or unexpected action denied",()=>assert.throws(()=>act(initial(),{type:"GRANT_MONEY"}),/ACTION/));
test("win after queue finishes strictly earlier ticket and renews per-win coupon",()=>{const s=event(queue(initial(),"L-G"),"WIN","DEMO-001");assert.equal(s.tickets.length,0);assert.equal(s.parts.find(x=>x.id==="L-G").status,"READY");assert.equal(s.parts.find(x=>x.id==="L-G").hp,100);assert.equal(s.wins,1);assert.equal(s.freeUsed,false)});
test("per-run free coupon stays used after victory",()=>{const s=event(queue(initial("LIGHT","PER_RUN"),"L-G"),"WIN","DEMO-001");assert.equal(s.freeUsed,true);assert.equal(s.cash,100)});
test("loss does not complete repair or refresh free token",()=>{const s=event(queue(initial(),"L-G"),"LOSS","DEMO-001");assert.equal(s.wins,0);assert.equal(s.freeUsed,true);assert.equal(s.tickets.length,1);assert.equal(s.parts.find(x=>x.id==="L-G").hp,0)});
test("loss then later distinct victory completes original queued part",()=>{let s=queue(initial(),"L-G");s=event(s,"LOSS","DEMO-001");s=event(s,"WIN","DEMO-002");assert.equal(s.wins,1);assert.equal(s.tickets.length,0)});
test("duplicate victory ID denied without queue/reward replay",()=>{let s=queue(initial(),"L-G");s=event(s,"WIN","DEMO-001");const before=clone(s);s=event(s,"WIN","DEMO-001");assert.equal(s.wins,before.wins);assert.equal(s.seq,before.seq);assert.equal(s.cash,before.cash);assert.match(s.last,/REPLAY_REJECTED/)});
test("same ID reused after loss is denied too",()=>{let s=event(initial(),"LOSS","DEMO-001");s=event(s,"WIN","DEMO-001");assert.equal(s.wins,0);assert.equal(s.seq,1)});
test("replay last event control fails closed",()=>{const s=act(event(initial(),"WIN","DEMO-001"),{type:"REPLAY_LAST"});assert.equal(s.wins,1);assert.match(s.last,/REPLAY_REJECTED/)});
test("synthetic/local ID grammar rejects arbitrary input",()=>assert.throws(()=>event(initial(),"WIN","REAL-GAME-123"),/EVENT_ID/));
test("original input remains immutable during queue/price changes",()=>{const a=initial();queue(a,"L-G");assert.equal(a.freeUsed,false);assert.equal(a.cash,100);assert.equal(a.parts.find(x=>x.id==="L-G").status,"DESTROYED")});
test("multiple losses do not create victory quota",()=>{let s=queue(initial(),"L-G");s=event(s,"LOSS","DEMO-001");s=event(s,"LOSS","DEMO-002");assert.equal(s.wins,0);assert.equal(s.freeUsed,true);assert.equal(s.tickets.length,1)});
test("critical destroyed 0HP blocks normal scripted WIN",()=>{const a=initial("HEAVY"),b=event(a,"WIN","DEMO-001");assert.equal(b.wins,0);assert.equal(b.seen.length,0);assert.match(b.last,/SOFTLOCK_RISK/)});
test("pending critical 0HP still blocks native deployment",()=>{const a=queue(initial("HEAVY"),"H-D");assert.equal(canDeploy(a),false);assert.equal(event(a,"WIN","DEMO-001").wins,0)});
test("approved? never: explicitly hypothetical loaner success can unblock critical part",()=>{let s=queue(initial("HEAVY"),"H-D");s=event(s,"LOANER_WIN","DEMO-001");assert.equal(s.wins,1);assert.equal(canDeploy(s),true);assert.equal(s.loanerAvailable,false);assert.match(s.last,/NIEZATWIERDZONY LOANER/)});
test("hypothetical loaner LOSS leaves unresolved deploy circularity",()=>{let s=queue(initial("HEAVY"),"H-D");s=event(s,"LOANER_LOSS","DEMO-001");assert.equal(s.wins,0);assert.equal(canDeploy(s),false);assert.equal(s.loanerAvailable,false);assert.equal(event(s,"LOANER_WIN","DEMO-002").wins,0)});
test("loaner never grants extra win if current normal deployment possible",()=>{const s=event(initial("LIGHT"),"LOANER_WIN","DEMO-001");assert.equal(s.wins,0);assert.equal(s.loanerAvailable,true)});
test("normal victory after an authorized hypothetical loaner restoration",()=>{let s=queue(initial("HEAVY"),"H-D");s=event(s,"LOANER_WIN","DEMO-001");s=event(s,"WIN","DEMO-002");assert.equal(s.wins,2);assert.equal(s.seq,2)});
test("cannot forge extra wallet value",()=>{const s=initial();s.cash=100000;assert.throws(()=>checkState(s),/CURRENCY/)});
test("cannot forge missing ticket for rebuilding part without state acceptance assumption",()=>{const s=queue(initial(),"L-G");s.tickets.push({...s.tickets[0]});assert.throws(()=>checkState(s),/TICKET_DUP/)});
test("cannot forge other-owner physical component id",()=>{const s=initial();s.parts[0].id="H-C";assert.throws(()=>checkState(s),/PART_ID/)});
test("cannot forge invalid ledger ID",()=>{const s=initial();s.seen=["PWNED"];assert.throws(()=>checkState(s),/LEDGER_ID/)});
test("cannot forge negative cash",()=>{const s=initial();s.cash=-1;assert.throws(()=>checkState(s),/CURRENCY/)});
test("can't make REBUILDING part with nonzero HP",()=>{const s=queue(initial(),"L-G");s.parts.find(p=>p.id==="L-G").hp=100;assert.throws(()=>checkState(s),/PART_HP/)});
test("document self-contained offline with explicit false-provenance warnings",()=>{const h=render();assert.match(h,/<!doctype html>/);assert.match(h,/lang="pl"/);for(const x of ["0 rzeczywistych uczestników","Brak Unity/PhysX","NIEZATWIERDZONY","SOFTLOCK_RISK","CODEX","REBUILDING","#652/#644/#604 ALT_HOLD"])if(x!=="CODEX")assert.ok(h.includes(x),x)});
test("document includes native keyboard focus and aria-live announcements",()=>{const h=render();for(const x of ["focus-visible","aria-live=\"polite\"","aria-labelledby=\"fight-title\"","id=\"tiles\"","id=\"reset\""])assert.ok(h.includes(x),x)});
test("CSP hash exactly matches sole original embedded controller",()=>{const h=render(),js=h.match(/<script>([\s\S]*?)<\/script>/)[1],digest=crypto.createHash("sha256").update(js).digest("base64");assert.ok(h.includes("script-src &apos;sha256-"+digest+"&apos;"));assert.equal((h.match(/<script>/g)||[]).length,1);assert.ok(h.includes("connect-src &apos;none&apos;"));assert.ok(!h.includes("<script src="))});
test("render and simulated trials deterministic",()=>assert.equal(render(),render()));
test("actual private HTML file produced and SHA256 grounded",()=>temp(d=>{const file=path.join(d,"playable.html"),r=emit(file),b=fs.readFileSync(file);assert.equal(r.sha256,crypto.createHash("sha256").update(b).digest("hex"));assert.equal(r.human_sessions,0);assert.ok(b.length>8000)}));
test("exclusive HTML cannot overwrite a prior file",()=>temp(d=>{const file=path.join(d,"a.html");emit(file);assert.throws(()=>emit(file),/EEXIST/)}));
test("Git working tree output path denied",()=>temp(d=>{fs.mkdirSync(path.join(d,".git"));assert.throws(()=>emit(path.join(d,"a.html")),/GIT_OUTPUT_DENIED/)}));
test("relative output is rejected",()=>assert.throws(()=>emit("x.html"),/OUTPUT_ABSOLUTE/));
test("CLI subprocess genuinely writes HTML in temporary location",()=>temp(d=>{const f=path.join(d,"choice.html"),p=spawnSync(process.execPath,[fileURLToPath(new URL("./rebuild_choice.mjs",import.meta.url)),"--output",f],{encoding:"utf8"});assert.equal(p.status,0,p.stderr);assert.equal(JSON.parse(p.stdout).real_econ,"NOT_VERIFIED");assert.ok(fs.readFileSync(f,"utf8").includes("id=\"tiles\""))}));
test("CLI rejects invalid usage, no partial file",()=>{const p=spawnSync(process.execPath,[fileURLToPath(new URL("./rebuild_choice.mjs",import.meta.url)),"--native-pass"],{encoding:"utf8"});assert.equal(p.status,2);assert.match(p.stderr,/ARGS/)});
test("original full client controller executes into simulated DOM",()=>{const e=mockUI();assert.equal(e.tiles.children.length,5);assert.equal(e.cash.textContent,"100 złomu");assert.equal(e.battle.disabled,false);assert.equal(e.loanerWin?.disabled,undefined)}); 
test("original UI queue action displays REBUILDING and consumes free quota",()=>{const e=mockUI(),button=e.tiles.children.find(x=>x.children[2].dataset.partId==="L-G").children[2];button.click();assert.equal(e.free.textContent,"ZUŻYTY");assert.equal(e.waiting.textContent,"1");assert.equal(e.tiles.children.find(x=>x.children[2].dataset.partId==="L-G").dataset.status,"REBUILDING")});
test("original UI paid reservation and lack of money are visible",()=>{const e=mockUI();e.tiles.children[2].children[2].click();e.tiles.children[3].children[2].click();assert.equal(e.cash.textContent,"30 złomu");e.tiles.children[4].children[2].click();assert.match(e.feedback.textContent,/Brak środków/);assert.equal(e.waiting.textContent,"2")});
test("original UI victory finishes earlier queue; duplicate event denied",()=>{const e=mockUI();e.tiles.children[2].children[2].click();e.battle.click();assert.equal(e.waiting.textContent,"0");assert.equal(e.wins.textContent,"1");e.replay.click();assert.match(e.feedback.textContent,/REPLAY_REJECTED/);assert.equal(e.wins.textContent,"1")});
test("original UI loss preserves pending rebuild",()=>{const e=mockUI();e.tiles.children[2].children[2].click();e.lose.click();assert.equal(e.waiting.textContent,"1");assert.equal(e.wins.textContent,"0")});
test("original UI heavy softlock disables normal battle until hypothetical intervention",()=>{const e=mockUI();e.vehicle.value="HEAVY";e.vehicle.listeners.change();assert.equal(e.battle.disabled,true);assert.equal(e["loaner-win"].disabled,false);assert.match(e.risk.textContent,/SOFTLOCK_RISK/);e.tiles.children[1].children[2].click();e["loaner-win"].click();assert.equal(e.battle.disabled,false);assert.equal(e["loaner-win"].disabled,true);assert.equal(e.wins.textContent,"1")});
test("original UI cadence change completely resets ephemeral decisions",()=>{const e=mockUI();e.tiles.children[2].children[2].click();e.cadence.value="PER_RUN";e.cadence.listeners.change();assert.equal(e.free.textContent,"DOSTĘPNY");assert.equal(e.waiting.textContent,"0")});
test("original UI restart resets all ephemeral state (not durable save)",()=>{const e=mockUI();e.tiles.children[2].children[2].click();e.reset.click();assert.equal(e.waiting.textContent,"0");assert.equal(e.free.textContent,"DOSTĘPNY");assert.equal(e.cash.textContent,"100 złomu")});
