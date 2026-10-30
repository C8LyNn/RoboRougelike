import test from "node:test";
import assert from "node:assert/strict";
import crypto from "node:crypto";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import vm from "node:vm";
import {spawnSync} from "node:child_process";
import {fileURLToPath} from "node:url";
import {ID,FROZEN,SIGNAL,ROUNDS,validate,at,choose,render,generate} from "./damage_cues.mjs";
const tmp=fn=>{const d=fs.mkdtempSync(path.join(os.tmpdir(),"b01w19-cues-"));try{return fn(d)}finally{fs.rmSync(d,{recursive:true,force:true})}};
function clone(x){return structuredClone(x)}
function dom(){
 class El{
  constructor(tag="div"){this.tagName=tag.toUpperCase();this.children=[];this.dataset={};this.listeners={};this.attributes={};this.disabled=false;this.textContent="";this.value=0}
  append(...children){this.children.push(...children)}
  appendChild(child){this.children.push(child)}
  replaceChildren(){this.children=[]}
  addEventListener(k,fn){this.listeners[k]=fn}
  setAttribute(k,v){this.attributes[k]=v}
  querySelectorAll(sel){assert.equal(sel,"button");return this.children.filter(x=>x.tagName==="BUTTON")}
  click(){this.listeners.click?.()}
  focus(){this.didFocus=true}
 }
 const els=Object.fromEntries(["parts","prompt","progress","step","feedback","next","again","mode","summary"].map(id=>[id,new El(id==="next"||id==="again"||id==="mode"?"button":"div")]));
 const doc={listeners:{},activeElement:null,getElementById(id){return els[id]},createElement(tag){return new El(tag)},addEventListener(k,fn){this.listeners[k]=fn}};
 const src=render().match(/<script>([\s\S]*?)<\/script>/)[1];
 vm.runInNewContext(src,{document:doc});
 return {doc,els,keys:(key,options={})=>doc.listeners.keydown({key,altKey:false,ctrlKey:false,metaKey:false,...options})};
}
test("unique package ID and only frozen reattach/Save/host",()=>{assert.equal(ID,"B01-13-DAMAGE-CUE-TUTORIAL-001/R1");assert.equal(FROZEN.game,"d94400d95d13db7f27e1533df05c9292d5380431");assert.equal(FROZEN.save,"33094f154c7b91119b36d58aab285e6e83c5de57");assert.equal(FROZEN.host,"efb2b06eafedab5469b57f163152d96aca3c5915");assert.equal(FROZEN.alt,"#604 ALT_HOLD")});
test("three explicit redundant glyph/pattern states",()=>{assert.deepEqual(Object.keys(SIGNAL),["OK","DAMAGED","DETACHED"]);assert.equal(new Set(Object.values(SIGNAL).map(x=>x.glyph)).size,3);assert.equal(new Set(Object.values(SIGNAL).map(x=>x.border)).size,3)});
test("all six original scenario objects validate",()=>{assert.equal(ROUNDS.length,6);for(const r of ROUNDS)assert.equal(validate(r),r)});
test("equal LIGHT and HEAVY coverage",()=>{assert.equal(ROUNDS.filter(r=>r.vehicle==="LIGHT").length,3);assert.equal(ROUNDS.filter(r=>r.vehicle==="HEAVY").length,3)});
test("every state is targeted twice",()=>{for(const s of Object.keys(SIGNAL))assert.equal(ROUNDS.filter(r=>r.target===s).length,2)});
test("every round contains one and only one correct part",()=>{for(const r of ROUNDS)assert.equal(r.parts.filter(p=>p.state===r.target).length,1)});
test("all round part ID namespaces are unambiguous",()=>assert.equal(new Set(ROUNDS.flatMap(r=>r.parts.map(p=>p.id))).size,24));
test("round index first and last",()=>{assert.equal(at(0).id,"L01");assert.equal(at(5).id,"H03")});
test("invalid negative index fails closed",()=>assert.throws(()=>at(-1),/INDEX/));
test("invalid float index fails closed",()=>assert.throws(()=>at(0.5),/INDEX/));
test("index past last fails closed",()=>assert.throws(()=>at(6),/INDEX/));
test("correct choice yields named part",()=>{for(let i=0;i<6;i++){const r=at(i),p=r.parts.find(p=>p.state===r.target),x=choose(i,p.id);assert.equal(x.correct,true);assert.equal(x.correct_part,p.id);assert.equal(x.target_state,r.target)}});
test("wrong choice never counts as correct",()=>{for(let i=0;i<6;i++){const r=at(i),p=r.parts.find(p=>p.state!==r.target);assert.equal(choose(i,p.id).correct,false)}});
test("foreign and arbitrary part IDs fail closed",()=>assert.throws(()=>choose(1,"L01-P3"),/SELECT/));
test("false part ID and numeric ID fail closed",()=>{assert.throws(()=>choose(0,"__proto__"),/SELECT/);assert.throws(()=>choose(0,1),/SELECT/)});
test("round without one target fails validation",()=>{const r=clone(at(0));r.parts[2].state="OK";assert.throws(()=>validate(r),/UNIQUE_TARGET/)});
test("duplicate part name rejected",()=>{const r=clone(at(0));r.parts[0].name=r.parts[1].name;assert.throws(()=>validate(r),/DUPLICATE_NAME/)});
test("duplicate or forged part identity rejected",()=>{const r=clone(at(0));r.parts[1].id=r.parts[0].id;assert.throws(()=>validate(r),/PART/)});
test("unknown part state rejected",()=>{const r=clone(at(1));r.parts[1].state="NATIVE_PASS";assert.throws(()=>validate(r),/PART/)});
test("unknown top level field rejected",()=>{const r=clone(at(1));r.player_email="PRIVATE";assert.throws(()=>validate(r),/SCHEMA/)});
test("wrong vehicle dimension rejected",()=>{const r=clone(at(0));r.vehicle="HEAVY";assert.throws(()=>validate(r),/VEHICLE/)});
test("HTML is one fully offline self-contained document",()=>{const html=render();assert.match(html,/<!doctype html>/);assert.match(html,/<style>[\s\S]*<\/style>/);assert.equal((html.match(/<script>/g)||[]).length,1);assert.ok(!html.includes("<script src="));assert.ok(!html.includes("<link rel="));assert.ok(!html.includes("https://"))});
test("HTML includes explicit no-human, Unity and Save caveats",()=>{const html=render();for(const x of ["zero rzeczywistych uczestników","NOT_RUN","NOT_VERIFIED","#604 ALT_HOLD",FROZEN.host])assert.ok(html.includes(x),x)});
test("HTML contains focus, accessibility and noncolor signals",()=>{const h=render();for(const x of ['lang="pl"','aria-live="polite"','aria-label="Legenda stanów"','role="group"','prefers-reduced-motion','focus-visible','border-style:dashed','border-style:dotted','<progress'])assert.ok(h.includes(x),x)});
test("HTML inline executable script matches exact CSP SHA256",()=>{const h=render(),script=h.match(/<script>([\s\S]*?)<\/script>/)[1];const hash=crypto.createHash("sha256").update(script).digest("base64");assert.ok(h.includes("script-src &apos;sha256-"+hash+"&apos;"));assert.ok(h.includes("connect-src &apos;none&apos;"))});
test("render is deterministic without network or random seeds",()=>assert.equal(render(),render()));
test("private output creates genuine HTML with SHA256",()=>tmp(d=>{const f=path.join(d,"tutorial.html");const result=generate(f),b=fs.readFileSync(f);assert.equal(result.rounds,6);assert.equal(result.real_participants,0);assert.equal(result.native_unity,"NOT_RUN");assert.equal(result.sha256,crypto.createHash("sha256").update(b).digest("hex"));assert.ok(b.toString().includes("id=\"parts\""))}));
test("output cannot overwrite existing file",()=>tmp(d=>{const file=path.join(d,"tutorial.html");generate(file);assert.throws(()=>generate(file),/EEXIST/)}));
test("output cannot reside in Git working tree",()=>tmp(d=>{fs.mkdirSync(path.join(d,".git"));assert.throws(()=>generate(path.join(d,"tutorial.html")),/GIT_OUTPUT/)}));
test("relative path rejected",()=>assert.throws(()=>generate("file.html"),/ABSOLUTE_OUTPUT/));
test("actual Node CLI subprocess creates playable HTML",()=>tmp(d=>{const file=path.join(d,"t.html"),processResult=spawnSync(process.execPath,[fileURLToPath(new URL("./damage_cues.mjs",import.meta.url)),"--output",file],{encoding:"utf8"});assert.equal(processResult.status,0,processResult.stderr);assert.equal(JSON.parse(processResult.stdout).rounds,6);assert.ok(fs.readFileSync(file,"utf8").includes("<script>"))}));
test("actual CLI rejects malformed args with exit 2",()=>{const run=spawnSync(process.execPath,[fileURLToPath(new URL("./damage_cues.mjs",import.meta.url)),"--unity"],{encoding:"utf8"});assert.equal(run.status,2);assert.match(run.stderr,/ARGS/)});
test("actual client JS executes in simulated DOM and renders four buttons",()=>{const {els}=dom();assert.equal(els.parts.children.length,4);assert.equal(els.next.disabled,true);assert.equal(els.step.textContent,"Próba 1 / 6 · LIGHT")});
test("actual UI first selection gives feedback, blocks duplicate scoring",()=>{const {els}=dom();els.parts.children[2].click();assert.equal(els.next.disabled,false);assert.match(els.feedback.textContent,/Poprawnie/);assert.equal(els.parts.children.filter(b=>b.dataset.result==="right").length,1);els.parts.children[2].click();els.next.click();assert.equal(els.step.textContent,"Próba 2 / 6 · HEAVY")});
test("actual UI wrongly selected part gives exact correction",()=>{const {els}=dom();els.parts.children[0].click();assert.match(els.feedback.textContent,/Prawidłowo:/);assert.equal(els.parts.children[0].dataset.result,"wrong")});
test("actual UI supports mode toggle with retained accessible state",()=>{const {els}=dom();els.mode.click();assert.equal(els.parts.children[0].dataset.mode,"symbol");assert.match(els.parts.children[0].attributes["aria-label"],/Sprawna/);els.mode.click();assert.equal(els.parts.children[0].dataset.mode,"full")});
test("actual client keyboard 1-4 selection and N advances",()=>{const {els,keys}=dom();keys("3");assert.equal(els.next.disabled,false);keys("n");assert.equal(els.step.textContent,"Próba 2 / 6 · HEAVY")});
test("actual client keyboard M and R toggle/reset",()=>{const {els,keys}=dom();keys("m");assert.equal(els.parts.children[0].dataset.mode,"symbol");keys("4");keys("n");assert.equal(els.step.textContent,"Próba 2 / 6 · HEAVY");keys("r");assert.equal(els.step.textContent,"Próba 1 / 6 · LIGHT")});
test("actual client keyboard respects modifier keys",()=>{const {els,keys}=dom();keys("3",{ctrlKey:true});assert.equal(els.next.disabled,true)});
test("actual client completes six real simulated interactions without telemetry",()=>{const {els}=dom();for(let i=0;i<6;i++){const correct=at(i).parts.findIndex(p=>p.state===at(i).target);els.parts.children[correct].click();els.next.click()}assert.equal(els.progress.value,6);assert.match(els.summary.textContent,/6 \/ 6/);assert.equal(els.parts.children.length,0)});
test("actual client restart after finish re-enables playable beginning",()=>{const {els}=dom();for(let i=0;i<6;i++){els.parts.children[0].click();els.next.click()}els.again.click();assert.equal(els.progress.value,0);assert.equal(els.parts.children.length,4);assert.equal(els.next.disabled,true)});
