#!/usr/bin/env node
/**
 * B01-11 PASS14, runnable original-C# source gate (NOT Unity or human execution).
 * node Research/B01-11/PASS14/reticle_signal_source_gate.js <repo>
 * Requires git object 231005c6... fetched locally.
 */
"use strict";
const INPUT_SHA="231005c6d0351945a8ee6684f3856af118f0e94f";
const PINS={
 feedback:["Assets/WeaponIntegration/RRLAimFeedback.cs","503016450ba51b9112fa47dc27118120b5ba366b"],
 hud:["Assets/R6/R6RangeHUD.cs","adfd3c1a69fc4355e055581a736ce10cccc7ba91"],
 controller:["Assets/Pack/P12_PipelineSafeTankController.cs","ddcb4f480189af77efd3d47dc4630f1b844c6a69"]
};
function analyze(s){
 const checks=[
 ["surface distinct from damageability",s.feedback.includes("HasSurface=target!=null")&&s.feedback.includes("DamageableSurface=target!=null&&")],
 ["fallback has null camera collider",s.controller.includes("LastConsumedAimCollider=null")],
 ["null-target doesn't force early return",s.feedback.includes("if(assembly==null)return result;")&&s.feedback.includes("foreach(var part in assembly.Parts)")],
 ["Clear increments independent of surface damageability",s.feedback.includes("if(!blocked)result.Clear++;")],
 ["no surface has own caption",s.feedback.includes('!HasSurface?"Brak celu')],
 ["nondamageable surface annotated",s.feedback.includes('DamageableSurface?"":" • podłoże / osłona"')],
 ["center cyan keyed ONLY by Clear",s.hud.includes("aimCrossStyle.normal.textColor=feedback.Clear>0?new Color(.2f,.85f,.85f)")],
 ["caption and color displayed together",s.hud.includes("feedback.Caption,aimCaptionStyle")]
 ];
 return {source_sha:INPUT_SHA,scope:"original C# static code-path inspection; NOT C# execution, Unity or human observation",
        passed:checks.filter(x=>x[1]).length,total:checks.length,
        predicates:checks.map(x=>({id:x[0],pass:!!x[1]})),
        source_risk_confirmed:checks.every(x=>x[1]),
        derived_cases:[
         {id:"AIM-U01",surface:"nondamageable terrain",guns:1,inRange:1,aligned:1,clear:1,color:"cyan",caption:"tor: 1/1 • podłoże / osłona",UX:"green target affordance despite nondamageable hit"},
         {id:"AIM-U02",surface:"no camera hit; fallback within weapon range",guns:1,inRange:1,aligned:1,clear:1,color:"cyan",caption:"Brak celu",UX:"green target affordance despite no target"},
         {id:"AIM-U03",surface:"damageable target",guns:1,inRange:1,aligned:1,clear:1,color:"cyan",caption:"tor: 1/1",UX:"geometrically clear but no guaranteed ballistic impact"},
         {id:"AIM-U04",surface:"obstructed muzzle",guns:1,inRange:1,aligned:1,clear:0,color:"amber",caption:"przeszkoda przy lufie",UX:"correct obstruction signal"}
        ]};
}
if(typeof module!=="undefined")module.exports={analyze,PINS,INPUT_SHA};
if(typeof require!=="undefined"&&typeof module!=="undefined"&&require.main===module){
 const cp=require("child_process"),path=require("path"),root=path.resolve(process.argv[2]||".");
 let exact=true;const s={};
 try{
 for(const [k,[p,expected]] of Object.entries(PINS)){
  const blob=cp.execFileSync("git",["-C",root,"rev-parse",INPUT_SHA+":"+p],{encoding:"utf8"}).trim();
  if(blob!==expected)exact=false;
  s[k]=cp.execFileSync("git",["-C",root,"show",INPUT_SHA+":"+p],{encoding:"utf8"});
 }
 const result=analyze(s);result.exact_git_blobs=exact;
 process.stdout.write(JSON.stringify(result,null,2)+"\n");
 process.exitCode=exact&&result.source_risk_confirmed?0:1;
 }catch(e){process.stderr.write("SOURCE_UNAVAILABLE_OR_NOT_PINNED: "+e.message+"\n");process.exitCode=2;}
}
