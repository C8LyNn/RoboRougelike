'use strict';
/* B01-12 isolated read-only UI projection. No gameplay decisions or writes. */
(function(root,factory){const api=factory();if(typeof module==='object'&&module.exports)module.exports=api;root.RecoveryReadout=api;})(typeof globalThis!=='undefined'?globalThis:this,function(){
 const allowed=['run','generation','coreAlive','canDeploy','hasNextChallenge','loanerUsed','loanerEligible','victoryId','freeAvailable','parts'];
 const partKeys=['id','name','status','purchasePrice','rebuildQuote','destroyedAt','readyAfter'];
 const statuses=['ok','damaged','destroyed','pending','rebuilt'];
 const plain=o=>o!==null&&typeof o==='object'&&!Array.isArray(o)&&(Object.getPrototypeOf(o)===Object.prototype||Object.getPrototypeOf(o)===null);
 const keys=(o,k)=>plain(o)&&Object.keys(o).length===k.length&&Object.keys(o).every(x=>k.includes(x));
 const id=s=>typeof s==='string'&&/^[A-Za-z0-9][A-Za-z0-9_-]{0,39}$/.test(s);
 const natural=n=>Number.isSafeInteger(n)&&n>=0;
 const failure=(code)=>Object.freeze({ok:false,code});
 function project(src){
  if(!keys(src,allowed)||!id(src.run)||!natural(src.generation)||src.generation===0)return failure('INVALID_SNAPSHOT');
  for(const k of ['coreAlive','canDeploy','hasNextChallenge','loanerUsed','loanerEligible','freeAvailable'])if(typeof src[k]!=='boolean')return failure('INVALID_FLAGS');
  if(src.victoryId!==null&&!id(src.victoryId))return failure('INVALID_VICTORY');
  if(!Array.isArray(src.parts)||src.parts.length>32)return failure('INVALID_PARTS');
  if(!src.coreAlive&&(src.canDeploy||src.loanerEligible))return failure('CORE_CONTRADICTION');
  if(src.loanerEligible&&(src.canDeploy||src.loanerUsed||!src.coreAlive||!src.hasNextChallenge))return failure('LOANER_CONTRADICTION');
  if(src.canDeploy&&src.loanerEligible)return failure('ROUTE_CONTRADICTION');
  const seen=new Set(), projected=[];
  for(const p of src.parts){
   if(!keys(p,partKeys)||!id(p.id)||!p.name||typeof p.name!=='string'||p.name.length>72||!statuses.includes(p.status)||!natural(p.purchasePrice)||p.purchasePrice>10000000||!natural(p.rebuildQuote)||p.rebuildQuote>10000000||!natural(p.destroyedAt)||!natural(p.readyAfter)||seen.has(p.id))return failure('INVALID_PART');
   seen.add(p.id);
   if((p.status==='pending'||p.status==='destroyed')&&(p.purchasePrice>0&&p.rebuildQuote>=p.purchasePrice))return failure('QUOTE_CONTRADICTION');
   if((p.status==='pending'||p.status==='destroyed')&&p.readyAfter<=p.destroyedAt)return failure('CHRONOLOGY_CONTRADICTION');
   if(p.status==='rebuilt'&&p.readyAfter<=p.destroyedAt)return failure('CHRONOLOGY_CONTRADICTION');
   projected.push(Object.freeze({id:p.id,name:p.name,status:p.status,quote:p.rebuildQuote,readyAfter:p.readyAfter,reason:p.status==='pending'?'Oczekuje na następne autentyczne zwycięstwo':p.status==='destroyed'?'Odbudowa nie została zamówiona':p.status==='rebuilt'?'Odbudowana po wcześniejszym zwycięstwie':p.status==='damaged'?'Uszkodzona, ale nie zniszczona':'Sprawna'}));
  }
  const route=src.canDeploy?'NORMAL':src.loanerEligible?'LOANER_AVAILABLE':!src.coreAlive?'CORE_DESTROYED':!src.hasNextChallenge?'NO_CHALLENGE':src.loanerUsed?'LOANER_EXHAUSTED':'NONFUNCTIONAL';
  const explanation={NORMAL:'Konstrukcja jest zdolna do walki. Uszkodzone części opcjonalne nie blokują startu.',LOANER_AVAILABLE:'Konstrukcja nie może walczyć. Dostępny jednorazowy słabszy robot zastępczy.',CORE_DESTROYED:'Rdzeń zniszczony. Robot zastępczy nie jest dostępny.',NO_CHALLENGE:'Brak kolejnego autentycznego wyzwania. Robot zastępczy jest niedostępny.',LOANER_EXHAUSTED:'Możliwość użycia robota zastępczego została wykorzystana.',NONFUNCTIONAL:'Konstrukcja jest niesprawna. Brak potwierdzonej dostępności robota zastępczego.'};
  const result={ok:true,run:src.run,generation:src.generation,route,explanation:explanation[route],victoryLabel:src.victoryId===null?'Nie potwierdzono zwycięstwa':'Potwierdzone zwycięstwo: '+src.victoryId,freeLabel:src.freeAvailable?'Darmowa odbudowa: dostępna dla uprawnionego wcześniejszego zamówienia':'Darmowa odbudowa: niedostępna',parts:Object.freeze(projected)};
  return Object.freeze(result);
 }
 return Object.freeze({project});
});
