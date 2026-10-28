#!/usr/bin/env node
/**
 * B01-11 PASS15 phase B, original B01-12 garage-source executable receiver.
 * SOURCE_JS_V8 + emulated DOM only; NOT a true browser, Unity, PhysX or human test.
 * node Research/B01-11/PASS15/garage_functional_source_receiver.js <repo>
 * Inputs are exact Git objects, no changes to other agents' files.
 */
"use strict";
const OWNER_SHA="ff0a7366bd12222cf0a547c82b7715177fd13aa4";
const PATH="Research/B01-12/W74/garage-readiness.html";
const BLOB="564354278cce2de03cf20a9787e1da88c3bfef05";

function verifyAndRun(html){
 const inline=html.match(/<script>([\s\S]*?)<\/script>/);
 if(!inline)throw Error("NO_ORIGINAL_INLINE_SCRIPT");
 const ids=[...html.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
 const pieceIds=[...html.matchAll(/data-piece="([^"]+)"/g)].map(x=>x[1]);
 const elements=new Map();
 const make=(tag="div")=>({tagName:tag,dataset:{},children:[],handlers:{},textContent:"",className:"",value:"",checked:false,hidden:false,disabled:false,attrs:{},
   append(...v){this.children.push(...v)},replaceChildren(...v){this.children=[...v]},
   setAttribute(k,v){this.attrs[k]=v},addEventListener(k,fn){this.handlers[k]=fn}});
 ids.forEach(id=>elements.set(id,make()));elements.get("drive").value="wheels";
 const schematic=pieceIds.map(k=>{const n=make();n.dataset.piece=k;return n;});
 const document={getElementById:id=>{if(!elements.has(id))throw Error("UNKNOWN_DOM_ID "+id);return elements.get(id)},
  createElement:make,createTextNode:s=>({textContent:s}),
  querySelectorAll:q=>q==="[data-piece]"?schematic:[]};
 // THIS executes the untouched producer's original JavaScript, no reinterpretation.
 new Function("document",inline[1])(document);
 const el=k=>elements.get(k);
 const codes=()=>el("warnings").children.filter(c=>c.dataset.issue).map(c=>c.children[0]?.textContent);
 const checks=[];
 function check(id,green){checks.push({id,pass:!!green})}
 function action(slot,verb){
  const card=el("partControls").children.find(x=>x.dataset.slot===slot);
  const button=card.children[1].children.find(x=>x.dataset.action===verb);
  if(!button||button.disabled)throw Error("DISABLED_ACTION "+slot+"/"+verb);
  el("partControls").handlers.click({target:{closest:()=>button}});
 }
 check("initial_both",codes().join("|")==="CRITICAL_WEAPON|CRITICAL_MOBILITY");
 check("tips_off_initial",!el("tips").checked&&el("optional").hidden);
 el("tips").checked=true;el("tips").handlers.change();check("tips_on_still_both",codes().length===2);
 el("tips").checked=false;el("tips").handlers.change();check("tips_off_still_both",codes().length===2);
 action("gun","toggle");check("mount_gun_only_mobility",codes().join("|")==="CRITICAL_MOBILITY");
 action("left","toggle");check("one_wheel_not_functional",codes().join("|")==="CRITICAL_MOBILITY");
 action("right","toggle");check("wheels_both_functional_ready",codes().length===0);
 action("gun","damage");check("mounted_zero_hp_gun_not_functional",codes().join("|")==="CRITICAL_WEAPON");
 action("gun","repair");check("gun_repair_restores_ready",codes().length===0);
 el("drive").value="tracks";el("drive").handlers.change();check("selected_tracks_missing_critical",codes().join("|")==="CRITICAL_MOBILITY");
 action("trackL","toggle");action("trackR","toggle");check("both_tracks_functional_ready",codes().length===0);
 // The only NEW visual-source risk. The HUD schematic itself has no tracks.
 const trackPieces=schematic.filter(p=>p.dataset.piece==="trackL"||p.dataset.piece==="trackR");
 check("selected_tracks_have_both_onscreen_schematic_pieces",trackPieces.length===2);
 action("trackR","damage");check("broken_track_right_critical",codes().join("|")==="CRITICAL_MOBILITY");
 el("drive").value="hover";el("drive").handlers.change();check("missing_hover_critical",codes().join("|")==="CRITICAL_MOBILITY");
 action("hover","toggle");check("functional_hover_ready",codes().length===0);
 el("connection").checked=true;el("connection").handlers.change({target:{checked:true}});check("independent_structure_warning",codes().join("|")==="CRITICAL_STRUCTURE");
 el("checkStart").handlers.click();check("start_mock_blocked_structure",el("outcome").textContent.includes("CRITICAL_STRUCTURE"));
 el("backEditor").handlers.click();check("editor_return_when_blocked",el("outcome").textContent.includes("Powrót do edytora: dostępny"));
 el("preset").handlers.change({target:{value:"zero"}});check("preset_zero_has_both_reasons",codes().join("|")==="CRITICAL_WEAPON|CRITICAL_MOBILITY");
 el("preset").handlers.change({target:{value:"ready"}});check("preset_ready",codes().length===0);
 return {method:"exact original producer inline JS executed in V8 with emulated DOM, NOT original-browser/Unity/human",
   expected_original_git_blob:BLOB,tests:checks,total:checks.length,
   positive_green:checks.filter(t=>t.id!=="selected_tracks_have_both_onscreen_schematic_pieces"&&t.pass).length,
   expected_positive:checks.length-1,
   track_schematic_present:trackPieces.length===2,
   distinct_repro:"Selected tracks are operational but trackL/trackR have no schematic slots",
   source_piece_ids:pieceIds};
}
if(typeof module!=="undefined")module.exports={verifyAndRun,OWNER_SHA,PATH,BLOB};
if(typeof require!=="undefined"&&typeof module!=="undefined"&&require.main===module){
 const cp=require("child_process"),path=require("path"),crypto=require("crypto");
 const root=path.resolve(process.argv[2]||".");
 try{
  const sha=cp.execFileSync("git",["-C",root,"rev-parse",OWNER_SHA+":"+PATH],{encoding:"utf8"}).trim();
  if(sha!==BLOB)throw Error("ORIGINAL_BLOB_MISMATCH: "+sha);
  const source=cp.execFileSync("git",["-C",root,"show",OWNER_SHA+":"+PATH],{encoding:"utf8"});
  const hash=crypto.createHash("sha1").update("blob "+Buffer.byteLength(source)+"\0").update(source).digest("hex");
  if(hash!==BLOB)throw Error("ORIGINAL_BYTES_MISMATCH "+hash);
  const r=verifyAndRun(source);r.actual_source_git_blob=hash;
  process.stdout.write(JSON.stringify(r,null,2)+"\n");
  // Current exact-original criterion: 19 positive GREEN and one original visual EXPECTED_RED.
  process.exitCode=r.positive_green===r.expected_positive&&!r.track_schematic_present?0:1;
 }catch(e){process.stderr.write("FAIL_CLOSED: "+e.message+"\n");process.exitCode=2;}
}
