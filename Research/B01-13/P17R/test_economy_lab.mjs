import test from "node:test";
import assert from "node:assert/strict";
import crypto from "node:crypto";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import vm from "node:vm";
import {spawnSync} from "node:child_process";
import {fileURLToPath} from "node:url";
import {PACKAGE,GAME,SAVE,R2_HOST,FROZEN_ECON,CADENCES,SCENARIOS,validateScenario,simulate,compare,render,write} from "./economy_lab.mjs";
const tmp=fn=>{const d=fs.mkdtempSync(path.join(os.tmpdir(),"b01-w20-econ-"));try{return fn(d)}finally{fs.rmSync(d,{recursive:true,force:true})}};
const copy=structuredClone;
const light=()=>SCENARIOS[0],heavy=()=>SCENARIOS[1],tworuns=()=>SCENARIOS[2];
function browser(){
 class E{
  constructor(tag="div"){this.tagName=tag.toUpperCase();this.children=[];this.events={};this.style={};this.attrs={};this.value="";this.textContent="";this.className=""}
  addEventListener(n,f){this.events[n]=f}
  append(...xs){this.children.push(...xs)}
  appendChild(x){this.children.push(x)}
  replaceChildren(){this.children=[]}
  setAttribute(k,v){this.attrs[k]=v}
 }
 const els=Object.fromEntries(["scenario","price","budget","price-value","budget-value","summary","events","delta","graph"].map(id=>[id,new E()]));
 els.scenario.value="LIGHT_SHORT";els.price.value="70";els.budget.value="250";
 const doc={getElementById(id){assert.ok(els[id],"unknown id "+id);return els[id]},createElement(tag){return new E(tag)}};
 const code=render().match(/<script>([\s\S]*?)<\/script>/)[1];
 vm.runInNewContext(code,{document:doc});
 return els;
}
test("W20 versioned package and exact frozen owner boundaries",()=>{assert.equal(PACKAGE,"B01-13-REBUILD-ECON-LAB-001/R1");assert.equal(GAME,"d94400d95d13db7f27e1533df05c9292d5380431");assert.equal(SAVE,"33094f154c7b91119b36d58aab285e6e83c5de57");assert.equal(R2_HOST,"efb2b06eafedab5469b57f163152d96aca3c5915");assert.equal(FROZEN_ECON,"12572117a3dce369257feabc57021cde58023369")});
test("two distinct cadence hypotheses, no approved single-choice claim",()=>assert.deepEqual(CADENCES,["PER_VICTORY","PER_RUN"]));
test("all original fixture shapes validate",()=>{assert.equal(SCENARIOS.length,3);SCENARIOS.forEach(x=>assert.equal(validateScenario(x),x))});
test("fixture includes two different light and heavy run styles",()=>{assert.equal(light().events.length,2);assert.equal(heavy().events.length,2);assert.equal(tworuns().events.length,3)});
test("win IDs unique in each fixture",()=>{for(const s of SCENARIOS)assert.equal(new Set(s.events.map(e=>e.victory_id)).size,s.events.length)});
test("light 250/70 per-victory one free each win",()=>{const r=simulate(light(),"PER_VICTORY");assert.equal(r.free,2);assert.equal(r.paid,1);assert.equal(r.blocked,0);assert.equal(r.spent,70);assert.equal(r.remaining,180)});
test("light 250/70 per-run only one free across wins",()=>{const r=simulate(light(),"PER_RUN");assert.equal(r.free,1);assert.equal(r.paid,2);assert.equal(r.blocked,0);assert.equal(r.spent,140);assert.equal(r.remaining,110)});
test("light comparative differential grounded in hypothetical model",()=>{const r=compare(light());assert.deepEqual(r.delta,{free_victory_minus_run:1,spent_run_minus_victory:70,blocked_run_minus_victory:0})});
test("heavy 250/70 per-victory free 2, paid 3, no blocked",()=>{const r=simulate(heavy(),"PER_VICTORY");assert.deepEqual([r.free,r.paid,r.blocked,r.spent,r.remaining],[2,3,0,210,40])});
test("heavy 250/70 per-run free 1, paid 3, blocked 1",()=>{const r=simulate(heavy(),"PER_RUN");assert.deepEqual([r.free,r.paid,r.blocked,r.spent,r.remaining],[1,3,1,210,40])});
test("heavy comparison can change availability without changing spend",()=>{const r=compare(heavy());assert.equal(r.delta.spent_run_minus_victory,0);assert.equal(r.delta.blocked_run_minus_victory,1)});
test("new run correctly replenishes one per-run token",()=>{const r=simulate(tworuns(),"PER_RUN");assert.equal(r.free,2);assert.equal(r.paid,3);assert.equal(r.spent,210)});
test("per-victory is consumed at most once within a victory",()=>{const r=simulate(heavy(),"PER_VICTORY",{budget:0,price:70});assert.deepEqual([r.free,r.paid,r.blocked],[2,0,3])});
test("no budget per-run still retains only one free per run",()=>{const r=simulate(heavy(),"PER_RUN",{budget:0});assert.deepEqual([r.free,r.paid,r.blocked],[1,0,4])});
test("high funds pay all remaining without blocked",()=>{const a=simulate(heavy(),"PER_RUN",{budget:1000,price:50});assert.equal(a.blocked,0);assert.equal(a.paid,4);assert.equal(a.remaining,800)});
test("each event row conserves parts",()=>{for(const s of SCENARIOS)for(const c of CADENCES){const m=simulate(s,c);assert.equal(m.rows.length,s.events.length);for(const row of m.rows)assert.equal(row.free+row.paid+row.blocked,row.parts)}});
test("no stochastic events in repeated runs",()=>{const a=simulate(tworuns(),"PER_VICTORY",{price:100,budget:300}),b=simulate(tworuns(),"PER_VICTORY",{price:100,budget:300});assert.deepEqual(a,b)});
test("zero-price rejected (economy abuse guard)",()=>assert.throws(()=>simulate(light(),"PER_RUN",{price:0}),/PRICE_RANGE/));
test("negative and fractional prices rejected",()=>{for(const price of [-1,1.5,NaN,1001])assert.throws(()=>simulate(light(),"PER_RUN",{price}),/PRICE_RANGE/)});
test("negative and fractional budget rejected",()=>{for(const budget of [-1,1.1,Infinity,100001])assert.throws(()=>simulate(light(),"PER_RUN",{budget}),/BUDGET_RANGE/)});
test("unknown cadence explicitly rejected",()=>assert.throws(()=>simulate(light(),"GLOBAL_DEBIT"),/CADENCE/));
test("duplicate victory ID rejected, never grants second token",()=>{const s=copy(light());s.events[1].victory_id=s.events[0].victory_id;assert.throws(()=>simulate(s,"PER_VICTORY"),/VICTORY_REPLAY/)});
test("duplicate part in same run rejected",()=>{const s=copy(light());s.events[1].parts=["L01"];assert.throws(()=>simulate(s,"PER_RUN"),/DUPLICATE_PART/)});
test("noncontiguous run ID sequence rejected",()=>{const s=copy(tworuns());s.events[2].run="RUN03";assert.throws(()=>simulate(s,"PER_RUN"),/NONCONTIGUOUS_RUN/)});
test("forged PII fields refused",()=>{const s=copy(light());s.participant_name="Alice";assert.throws(()=>simulate(s,"PER_RUN"),/SCENARIO_FIELDS/)});
test("forged event fields refused",()=>{const s=copy(light());s.events[0].real_unity_pass=true;assert.throws(()=>simulate(s,"PER_VICTORY"),/EVENT_FIELDS/)});
test("malformed part ID refused",()=>{const s=copy(light());s.events[0].parts[0]="PII-EMAIL";assert.throws(()=>simulate(s,"PER_RUN"),/PART_ID/)});
test("malformed victory/run IDs refused",()=>{const s=copy(light());s.events[0].victory_id="FAKE";assert.throws(()=>simulate(s,"PER_RUN"),/EVENT_IDS/)});
test("zero missing victories refused",()=>{const s=copy(light());s.events=[];assert.throws(()=>simulate(s,"PER_RUN"),/EVENT_COUNT/)});
test("zero parts in event refused",()=>{const s=copy(light());s.events[0].parts=[];assert.throws(()=>simulate(s,"PER_RUN"),/PART_COUNT/)});
test("HTML interactive comparator is self-contained and Polish",()=>{const h=render();assert.match(h,/<!doctype html>/);assert.match(h,/lang="pl"/);assert.ok(h.includes('id="scenario"'));assert.ok(h.includes('id="price"'));assert.ok(h.includes('id="budget"'));assert.ok(!h.includes("https://"));assert.ok(!h.includes("<script src="))});
test("HTML explicitly warns no real players/Unity or authoritative ledger",()=>{const h=render();for(const x of ["0 prawdziwych graczy","0 testów natywnych Unity/PhysX","rollback","#510",FROZEN_ECON,"Codex ACK NONE"])assert.ok(h.includes(x),x)});
test("HTML CSP SHA256 exactly matches embedded JS and blocks network",()=>{const h=render();const js=h.match(/<script>([\s\S]*?)<\/script>/)[1],sha=crypto.createHash("sha256").update(js).digest("base64");assert.ok(h.includes("script-src &apos;sha256-"+sha+"&apos;"));assert.ok(h.includes("connect-src &apos;none&apos;"))});
test("HTML accessible labels, progress and live status",()=>{const h=render();for(const x of ['aria-live="polite"','aria-labelledby="controls-title"','scope="col"','<caption>','prefers-reduced-motion','focus-visible'])assert.ok(h.includes(x),x)});
test("HTML render deterministic",()=>assert.equal(render(),render()));
test("original HTML engine created on physical private disk",()=>tmp(d=>{const f=path.join(d,"economy.html"),x=write(f),buf=fs.readFileSync(f);assert.equal(x.sha256,crypto.createHash("sha256").update(buf).digest("hex"));assert.equal(x.native_unity,"NOT_RUN");assert.equal(x.real_participants,0)}));
test("physical HTML file cannot be overwritten",()=>tmp(d=>{const f=path.join(d,"economy.html");write(f);assert.throws(()=>write(f),/EEXIST/)}));
test("physical output refuses Git directories",()=>tmp(d=>{fs.mkdirSync(path.join(d,".git"));assert.throws(()=>write(path.join(d,"x.html")),/GIT_OUTPUT/)}));
test("relative output rejected",()=>assert.throws(()=>write("x.html"),/ABSOLUTE_OUTPUT/));
test("actual Node child process generates offline HTML",()=>tmp(d=>{const f=path.join(d,"econ.html"),run=spawnSync(process.execPath,[fileURLToPath(new URL("./economy_lab.mjs",import.meta.url)),"--output",f],{encoding:"utf8"});assert.equal(run.status,0,run.stderr);assert.equal(JSON.parse(run.stdout).native_unity,"NOT_RUN");assert.ok(fs.readFileSync(f,"utf8").includes('id="summary"'))}));
test("actual Node child process rejects malformed args",()=>{const run=spawnSync(process.execPath,[fileURLToPath(new URL("./economy_lab.mjs",import.meta.url)),"--save"],{encoding:"utf8"});assert.equal(run.status,2);assert.match(run.stderr,/ARGS/)});
test("actual original client inline script generates 2 score cards",()=>{const els=browser();assert.equal(els.summary.children.length,2);assert.equal(els.events.children.length,2);assert.match(els.delta.textContent,/70 złomu/)});
test("actual client variant prices update after slider change",()=>{const els=browser();els.price.value="100";els.price.events.input();assert.match(els["price-value"].textContent,/100 złomu/);assert.match(els.delta.textContent,/100 złomu/)});
test("actual client changes budget to 0 and reports blocked repairs",()=>{const els=browser();els.budget.value="0";els.budget.events.input();assert.match(els["budget-value"].textContent,/0 złomu/);assert.match(els.summary.children[0].children[2].textContent,/bez środków 1/);assert.match(els.summary.children[1].children[2].textContent,/bez środków 2/)});
test("actual client switching heavy scenario renders all events",()=>{const els=browser();els.scenario.value="HEAVY_STREAK";els.scenario.events.input();assert.equal(els.events.children.length,2);assert.match(els.delta.textContent,/Różnica kosztu/)});
test("actual client switching to two-run scenario updates row count",()=>{const els=browser();els.scenario.value="TWO_RUNS";els.scenario.events.input();assert.equal(els.events.children.length,3)});
test("original client model parity with source math for every UI fixture",()=>{const els=browser();for(const s of SCENARIOS){els.scenario.value=s.id;els.scenario.events.input();const compared=compare(s);assert.ok(els.delta.textContent.includes(String(compared.delta.spent_run_minus_victory)+" złomu"));assert.ok(els.summary.children[0].children[1].textContent.includes(String(compared.per_victory.spent)));assert.ok(els.summary.children[1].children[1].textContent.includes(String(compared.per_run.spent))) }});
test("actual client graph fraction cannot exceed 100%",()=>{const els=browser();for(const s of SCENARIOS){els.scenario.value=s.id;els.scenario.events.input();assert.ok(parseFloat(els.graph.style.width)>=0&&parseFloat(els.graph.style.width)<=100)}});
