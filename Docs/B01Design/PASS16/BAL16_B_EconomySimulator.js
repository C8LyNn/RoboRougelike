/* B01-10 BAL16-B: independent budget/repair sensitivity simulator.
 * Research-only wallet model. This is NOT R3VehicleModel and does not call
 * R1RunController, R1 economy, or any Unity code. No rules changed.
 * "all pieces repaired atomically" is a planning what-if, NOT the original
 * R3 RepairAffordable behavior (it may repair only affordable parts).
 */
(function(root) {
 "use strict";
 function check(p,msg){if(!p)throw new Error(msg);}
 function price(data,id){const w=data.weaponProfiles.find(x=>x.id===id);check(w,"unknown gun");return w.buyScrap;}
 function quote(data,id,hpFraction) {
   const p=price(data,id);
   check(Number.isFinite(hpFraction)&&hpFraction>=0&&hpFraction<=1,"health out of [0,1]");
   if(hpFraction===1)return 0;
   if(hpFraction===0&&p<=1)return 0;
   const q=Math.max(1,Math.ceil((1-hpFraction)*p*.35-1e-12));
   return hpFraction===0?Math.min(p-1,q):q;
 }
 function budget(data,build) {
   check(build&&Array.isArray(build.weapons),"missing build");
   check(build.weapons.length>=1&&build.weapons.length<=2,"max 2 families");
   const kinds=new Set();
   let buy=0,rehab0=0,repairHalf=0,salvageFull=0,gunHp=0,guns=0;
   for(const part of build.weapons){
     check(!kinds.has(part.id),"duplicate family");kinds.add(part.id);
     check(Number.isInteger(part.count)&&part.count>0,"invalid count");
     const p=price(data,part.id),w=data.weaponProfiles.find(x=>x.id===part.id);
     check(w.analysisMode==="SIMPLE_INTERVAL_PROJECTILE","special behavior not modeled for this bundle");
     guns+=part.count;buy+=part.count*p;rehab0+=part.count*quote(data,part.id,0);
     repairHalf+=part.count*quote(data,part.id,.5);
     salvageFull+=part.count*Math.floor(p*.25);gunHp+=part.count*w.partHp;
   }
   check(guns<=24,"simulator safety max 24, NOT an in-game cap");
   return {buy,rehab0,repairHalf,salvageFull,gunHp,guns,kindCount:kinds.size,
     note:"Quotes are source-derived; player ownership, adjacency and operation are NOT simulated"};
 }
 function createLedger(data,build,startingScrap=0) {
   check(Number.isInteger(startingScrap)&&startingScrap>=0&&startingScrap<=data.economy.sourceMaxWallet,"wallet invalid");
   const b=budget(data,build);
   const state={scrap:startingScrap,owned:false,health:1,repaired:0,
     receivedRewards:[],events:[],failed:0};
   function tx(kind,amount,accepted,reason){
     const record={kind,amount:accepted?amount:0,accepted,reason,balance:state.scrap};
     state.events.push(record);if(!accepted)state.failed++;
     return record;
   }
   function buy(){
     if(state.owned)return tx("buy",0,false,"already-owned");
     if(state.scrap<b.buy)return tx("buy",0,false,"insufficient-scrap");
     state.scrap-=b.buy;state.owned=true;return tx("buy",-b.buy,true,"new-items-planning");
   }
   function award(eventId,amount){
     check(typeof eventId==="string"&&eventId.length>0&&eventId.length<=64,"bad eventId");
     check(Number.isInteger(amount)&&amount>=0,"negative/fractional reward");
     if(state.receivedRewards.includes(eventId))return tx("reward",0,false,"duplicate-event");
     state.receivedRewards.push(eventId);
     const previous=state.scrap;
     state.scrap=Math.min(data.economy.sourceMaxWallet,state.scrap+amount);
     return tx("reward",state.scrap-previous,true,"synthetic-income-not-R1");
   }
   function setCondition(health) {
     check(Number.isFinite(health)&&health>=0&&health<=1,"health invalid");
     if(!state.owned)return tx("damage",0,false,"not-owned");
     state.health=health;return tx("damage",0,true,"abstract-health-assumption");
   }
   function repair(){
     if(!state.owned)return tx("repair",0,false,"not-owned");
     if(state.health===1)return tx("repair",0,false,"already-intact");
     const cost=state.health<=0?b.rehab0:state.health===.5?b.repairHalf:
       build.weapons.reduce((sum,x)=>sum+x.count*quote(data,x.id,state.health),0);
     if(cost<=0)return tx("repair",0,false,"invalid-price");
     if(state.scrap<cost)return tx("repair",0,false,"insufficient-scrap");
     state.scrap-=cost;state.health=1;state.repaired++;
     return tx("repair",-cost,true,"all-guns-what-if-bundle-only");
   }
   function snapshot(){return {
     scrap:state.scrap,owned:state.owned,health:state.health,
     repairs:state.repaired,failed:state.failed,
     rewards:state.receivedRewards.slice(),events:state.events.map(x=>({...x})),
     initialScrap:startingScrap,quote:b
   };}
   return Object.freeze({buy,award,setCondition,repair,snapshot});
 }
 function runCycles(data,build,{startingScrap=300,perWinScrap=0,cycles=3,healthAfterWin=0}={}) {
   check(Number.isInteger(cycles)&&cycles>=1&&cycles<=25,"cycles invalid");
   check(Number.isInteger(perWinScrap)&&perWinScrap>=0,"income invalid");
   const x=createLedger(data,build,startingScrap);
   const purchase=x.buy();
   let completed=0,repairFailures=0;
   if(purchase.accepted)for(let i=0;i<cycles;i++){
     x.award("synthetic-win-"+i,perWinScrap);
     x.setCondition(healthAfterWin);
     const r=x.repair();
     if(!r.accepted){repairFailures++;break;}
     completed++;
   }
   return {build:build.weapons.map(w=>w.id+"x"+w.count).join("+"),startingScrap,
     perWinScrap,cyclesRequested:cycles,cyclesWithPaidRepairs:completed,repairFailures,
     purchaseAccepted:purchase.accepted,...x.snapshot(),
     engine:"research-only abstract wallet; NOT source R1 rewards / garage ownership / atomicity"};
 }
 root.BAL16B=Object.freeze({quote,budget,createLedger,runCycles});
 if(typeof module!=="undefined"&&module.exports)module.exports=root.BAL16B;
})(typeof globalThis==="object"?globalThis:this);
