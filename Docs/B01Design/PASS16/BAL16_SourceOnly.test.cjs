#!/usr/bin/env node
"use strict";
// B01-10 BAL16 source-only regression, Node: node BAL16_SourceOnly.test.cjs
const fs=require("node:fs"),path=require("node:path"),assert=require("node:assert/strict");
const D=JSON.parse(fs.readFileSync(path.join(__dirname,"BAL16_A_SourceProfiles.json"),"utf8"));
const A=require("./BAL16_A_CombatSimulator.js"),B=require("./BAL16_B_EconomySimulator.js");
let pass=0,fail=0;
function test(name,fn){try{fn();pass++;process.stdout.write("PASS "+name+"\n");}
catch(e){fail++;process.stderr.write("FAIL "+name+" "+e.message+"\n");}}
const build=(n=2,a="Basic",b="Automatic")=>({chassis:"Standard",weapons:[{id:a,count:n/2},{id:b,count:n/2}]});
test("original source versions pinned",()=>assert.equal(D.verifiedCodex.sha,"589b60a912096d1e9917cb0bbac73a53e7543b4f"));
test("9 original R4 weapon profiles",()=>assert.equal(D.weaponProfiles.length,9));
test("3 simple standard ballistic guns",()=>assert.deepEqual(D.weaponProfiles.filter(x=>x.analysisMode==="SIMPLE_INTERVAL_PROJECTILE").map(x=>x.id),["Basic","Automatic","Heavy"]));
test("original Light Standard Heavy masses",()=>assert.deepEqual(D.vehicleDesigns.map(x=>x.baselineMassKg),[192,728,1176]));
test("original core HP",()=>assert.deepEqual(D.vehicleDesigns.map(x=>x.coreHp),[80,100,180]));
test("2 mounted families accepted",()=>assert.equal(A.modelBuild(D,build()).families,2));
test("3 mounted families rejected",()=>assert.throws(()=>A.modelBuild(D,{chassis:"Standard",weapons:[{id:"Basic",count:1},{id:"Automatic",count:1},{id:"Heavy",count:1}]}),/two mounted weapon/));
test("duplicate weapon type rejected",()=>assert.throws(()=>A.modelBuild(D,{chassis:"Standard",weapons:[{id:"Basic",count:1},{id:"Basic",count:1}]}),/duplicated/));
test("special shotgun not modeled as normal bullet",()=>assert.throws(()=>A.modelBuild(D,{chassis:"Standard",weapons:[{id:"Shotgun",count:1}]}),/special/));
test("source stats Basic 40 damage 0.38 second",()=>assert.deepEqual([A.get(D,"Basic").damage,A.get(D,"Basic").intervalSeconds],[40,.38]));
test("0 hit probability cannot kill",()=>assert.equal(A.simulateFight(D,build(),{accuracy:0,targetHp:220,maxSeconds:10}).killed,false));
test("100 percent hit probability kills",()=>assert.equal(A.simulateFight(D,build(),{accuracy:1,targetHp:220,maxSeconds:10}).killed,true));
test("seeded traces deterministic",()=>assert.deepEqual(A.simulateFight(D,build(),{seed:445,accuracy:.55}),A.simulateFight(D,build(),{seed:445,accuracy:.55})));
test("higher chance not slower at same seed",()=>{const lo=A.simulateFight(D,build(),{seed:8,accuracy:.55,targetHp:220}),hi=A.simulateFight(D,build(),{seed:8,accuracy:1,targetHp:220});assert.ok(hi.killed&&(!lo.killed||hi.ttkSeconds<=lo.ttkSeconds));});
test("2/6/12 guns grow mass and price",()=>{const xs=[2,6,12].map(n=>A.modelBuild(D,build(n)));assert.deepEqual(xs.map(x=>x.buyAllGunsScrap),[66,198,396]);assert.ok(xs[0].modeledTotalMassKg<xs[1].modeledTotalMassKg&&xs[1].modeledTotalMassKg<xs[2].modeledTotalMassKg);});
test("modeled speed limit falls with weapon mass",()=>{const x=[2,6,12].map(n=>A.modelBuild(D,build(n)).modeledSpeedLimitMps);assert.ok(x[0]>x[1]&&x[1]>x[2]);});
test("30/36/55 price 0HP 35 percent rebuild",()=>assert.deepEqual([30,36,55].map(x=>A.quote(D,x,0)),[11,13,20]));
test("all source gun 0HP rebuild prices below buy",()=>assert.ok(D.weaponProfiles.every(x=>A.quote(D,x.buyScrap,0)<x.buyScrap)));
test("50 percent part HP proportional quote",()=>assert.deepEqual([30,36,55].map(x=>A.quote(D,x,.5)),[6,7,10]));
test("economy 6 gun cost and rebuilt quote",()=>assert.deepEqual([B.budget(D,build(6)).buy,B.budget(D,build(6)).rehab0],[198,72]));
test("insufficient funds purchase rejects without debit",()=>{const l=B.createLedger(D,build(6),197);assert.equal(l.buy().accepted,false);assert.equal(l.snapshot().scrap,197);});
test("repair with no funds fails unchanged",()=>{const l=B.createLedger(D,build(),66);l.buy();l.setCondition(0);assert.equal(l.repair().accepted,false);assert.equal(l.snapshot().scrap,0);assert.equal(l.snapshot().health,0);});
test("wallet exact 0HP repair charge once",()=>{const l=B.createLedger(D,build(),100);l.buy();l.setCondition(0);assert.equal(l.repair().amount,-24);assert.equal(l.snapshot().scrap,10);});
test("same reward event id cannot pay twice",()=>{const l=B.createLedger(D,build(),100);l.award("W1",20);assert.equal(l.award("W1",20).accepted,false);assert.equal(l.snapshot().scrap,120);});
test("reward cannot be negative or fractional",()=>{const l=B.createLedger(D,build(),100);assert.throws(()=>l.award("W1",-1),/negative/);assert.throws(()=>l.award("W1",1.4),/negative/);});
test("no repair on full-HP cannot debit",()=>{const l=B.createLedger(D,build(),100);l.buy();const b=l.snapshot().scrap;assert.equal(l.repair().accepted,false);assert.equal(l.snapshot().scrap,b);});
test("all 9 combos and 32 seeds deterministic",()=>{for(const sample of A.candidateBuilds()){const x=A.runTrials(D,sample,{accuracy:.75,trials:32,targetHp:180}),y=A.runTrials(D,sample,{accuracy:.75,trials:32,targetHp:180});assert.deepEqual(x,y);}});
test("simulated cycles no free loan/scrap",()=>{const s=B.runCycles(D,build(),{startingScrap:100,perWinScrap:0,cycles:25,healthAfterWin:0});assert.ok(s.scrap>=0&&s.scrap<=100);});
test("invalid health and weapon fail-closed",()=>{assert.throws(()=>B.quote(D,"Basic",NaN),/health/);assert.throws(()=>A.modelBuild(D,{chassis:"Standard",weapons:[{id:"FAKE",count:1}]}),/unknown/);});
test("12 barrel not wrongly 4 capped",()=>assert.equal(A.modelBuild(D,build(12)).gunCount,12));
process.stdout.write("BAL16 SOURCE-ONLY: "+pass+" PASS / "+fail+" FAIL; Unity/PhysX/C# NOT_RUN\n");
if(fail)process.exitCode=1;
