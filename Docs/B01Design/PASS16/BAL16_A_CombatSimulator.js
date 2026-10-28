/* B01-10 BAL16-A v1: deterministic research combat/equipment simulator.
   No canonical game implementation; data values come from BAL16_A_SourceProfiles.json.
   Shots and HP outcomes are SYNTHETIC. No collision, aim, armor, ammo or PhysX.
   Use browser global BAL16A, or module.exports when run through Node CommonJS. */
(function (root) {
"use strict";
const own = Object.prototype.hasOwnProperty;
function ensure(condition, message) { if(!condition) throw new Error(message); }
const get=(data,id)=>{const x=data.weaponProfiles.find(p=>p.id===id);ensure(x,"unknown weapon family "+id);return x;};
function quote(data, p, health) {
  ensure(Number.isInteger(p)&&p>0&&Number.isFinite(health)&&health>=0&&health<=1,"bad quote");
  if(health===1)return 0;
  if(health===0&&p<=1)return 0;
  const q=Math.max(1,Math.ceil((1-health)*p*data.economy.sourceRate35Pct-1e-12));
  return health===0?Math.min(p-1,q):q;
}
function verifyLoadout(data, build) {
  ensure(data&&Array.isArray(data.weaponProfiles),"missing source profile");
  ensure(build&&Array.isArray(build.weapons),"missing gun list");
  ensure(build.weapons.length>=1&&build.weapons.length<=data.limits.mountedWeaponFamilies,"two mounted weapon families maximum");
  ensure(new Set(build.weapons.map(x=>x.id)).size===build.weapons.length,"duplicated family");
  const chassis=data.vehicleDesigns.find(v=>v.id===build.chassis);
  ensure(chassis,"unknown chassis");
  let total=0;
  for(const item of build.weapons) {
    const w=get(data,item.id);
    ensure(w.analysisMode==="SIMPLE_INTERVAL_PROJECTILE","native special gun behavior not modeled");
    ensure(Number.isInteger(item.count)&&item.count>=1,"invalid gun count");
    total+=item.count;
  }
  ensure(total<=24,"simulator guard: max 24 virtual guns, NOT a game cap");
  return {chassis,gunCount:total};
}
function modelBuild(data, build) {
 const {chassis,gunCount}=verifyLoadout(data,build);
 const guns=build.weapons.map(x=>({profile:get(data,x.id),count:x.count}));
 // Explicit modeling assumption: base design has one Basic Gun, which is
 // replaced by the first selected gun, and all extra guns are added without
 // altering armor/wheel positions or verifying mounting fit.
 const mass=chassis.baselineMassKg-get(data,"Basic").mountedMassKg+
   guns.reduce((sum,x)=>sum+x.profile.mountedMassKg*x.count,0);
 const weight=Math.max(data.mobility.weightFactorClamp[0],
                 Math.min(data.mobility.weightFactorClamp[1],
                 Math.sqrt(chassis.baselineMassKg/Math.max(1,mass))));
 const wheelBonus=1; // same stock wheel count as base design
 const buy=guns.reduce((sum,x)=>sum+x.count*x.profile.buyScrap,0);
 const zeroRepair=guns.reduce((sum,x)=>sum+x.count*quote(data,x.profile.buyScrap,0),0);
 const halfRepair=guns.reduce((sum,x)=>sum+x.count*quote(data,x.profile.buyScrap,.5),0);
 const intactDismantle=guns.reduce((sum,x)=>sum+x.count*Math.floor(x.profile.buyScrap*.25),0);
 const nominalDps=guns.reduce((sum,x)=>sum+x.count*x.profile.damage/x.profile.intervalSeconds,0);
 const installedGunHp=guns.reduce((sum,x)=>sum+x.count*x.profile.partHp,0);
 return {id:build.weapons.map(x=>x.id+"x"+x.count).join("+"),
  chassis:chassis.id,gunCount,families:build.weapons.length,coreHp:chassis.coreHp,
  gunHpSumNotSharedPool:installedGunHp,gunMassKg:mass-chassis.baselineMassKg+get(data,"Basic").mountedMassKg,
  modeledTotalMassKg:mass,baselineMassKg:chassis.baselineMassKg,
  modeledWeightFactor:weight,modeledSpeedLimitMps:chassis.designSpeed*weight*wheelBonus,
  buyAllGunsScrap:buy,repairAllGuns0HpScrap:zeroRepair,repairAllGuns50PctHpScrap:halfRepair,
  dismantleIntactAllGunsScrap:intactDismantle,nominalIdealDps:nominalDps,
  scope:"ORIGINAL_PROFILES_WITH_SYNTHETIC_HITS_AND_FULLY_OPERATIONAL_MASS"};
}
function rng(seed) {
 let value=(Number(seed)>>>0)||0x9e3779b9;
 return ()=>{value^=value<<13;value^=value>>>17;value^=value<<5;return (value>>>0)/4294967296;};
}
function simulateFight(data,build,options={}) {
 verifyLoadout(data,build);
 const chance=options.accuracy===undefined?data.simulation.defaultAccuracy:options.accuracy;
 const target=options.targetHp===undefined?180:options.targetHp;
 const maxTime=options.maxSeconds===undefined?data.simulation.maxSeconds:options.maxSeconds;
 ensure(Number.isFinite(chance)&&chance>=0&&chance<=1,"invalid accuracy");
 ensure(Number.isFinite(target)&&target>0,"invalid target HP");
 ensure(Number.isFinite(maxTime)&&maxTime>=0&&maxTime<=120,"invalid duration");
 const guns=[];
 for(const family of build.weapons)for(let i=0;i<family.count;i++)
   guns.push({kind:family.id,profile:get(data,family.id),next:0,shots:0});
 const roll=rng(options.seed??1);
 let hp=target,shots=0,hits=0,damage=0,last=0;
 for(let iter=0;iter<150000;iter++){
   let next=Infinity;
   for(const gun of guns)if(gun.next<next)next=gun.next;
   if(next>maxTime+1e-8||!Number.isFinite(next))break;
   for(const gun of guns){
     if(gun.next>next+1e-8)continue;
     shots++;gun.shots++;
     if(roll()<chance){
       hits++;const dealt=Math.min(hp,gun.profile.damage);
       hp-=dealt;damage+=dealt;
     }
     gun.next=+(gun.next+gun.profile.intervalSeconds).toFixed(8);
     if(hp<=0){last=next;return {killed:true,ttkSeconds:+last.toFixed(4),
       hpRemaining:0,shots,hits,damageDealt:damage,seed:options.seed??1};}
   }
 }
 return {killed:false,ttkSeconds:null,hpRemaining:hp,shots,hits,damageDealt:damage,seed:options.seed??1};
}
function quantile(xs,q) {
 if(xs.length===0)return null;
 const sorted=xs.slice().sort((a,b)=>a-b),at=(sorted.length-1)*q;
 const lo=Math.floor(at),hi=Math.ceil(at);
 return +(sorted[lo]+(sorted[hi]-sorted[lo])*(at-lo)).toFixed(4);
}
function runTrials(data,build,options={}) {
 let count=options.trials??data.simulation.trialSeeds;
 ensure(Number.isInteger(count)&&count>=1&&count<=128,"trials out of bound");
 const starts=options.firstSeed??1;ensure(Number.isInteger(starts),"seed must be integer");
 const results=Array.from({length:count},(_,i)=>simulateFight(data,build,{...options,seed:starts+i}));
 const killed=results.filter(x=>x.killed);
 const ttk=killed.map(x=>x.ttkSeconds);
 return {...modelBuild(data,build),targetHp:options.targetHp??180,
   assumedAccuracy:options.accuracy??data.simulation.defaultAccuracy,
   trials:count,killRate:killed.length/count,
   ttkMedianSeconds:quantile(ttk,.5),ttkP10Seconds:quantile(ttk,.1),
   ttkP90Seconds:quantile(ttk,.9),timeoutTrials:count-killed.length,
   exampleSeed1:results[0],units:"HP, kg, m/s, Scrap, seconds; no native shooting"};
}
function candidateBuilds(chassis="Standard") {
 const choices=[["Basic","Automatic"],["Basic","Heavy"],["Automatic","Heavy"]];
 const counts=[2,6,12];
 return choices.flatMap(pair=>counts.map(n=>({
   chassis,weapons:[{id:pair[0],count:n/2},{id:pair[1],count:n/2}]
 })));
}
const api=Object.freeze({get,quote,verifyLoadout,modelBuild,simulateFight,runTrials,candidateBuilds});
root.BAL16A=api;
if(typeof module!=="undefined"&&module.exports)module.exports=api;
})(typeof globalThis==="object"?globalThis:this);
