(function(root,factory){const api=factory();if(typeof module==='object'&&module.exports)module.exports=api;root.MissionJournal=api;})(typeof globalThis!=='undefined'?globalThis:this,function(){
'use strict';
const VERSION=1;
const TASKS=Object.freeze([
  {id:'reach_gate',label:'Dotrzyj do bramy',group:'eksploracja',deps:[]},
  {id:'disable_turrets',label:'Wyłącz dwie wieżyczki',group:'walka',deps:['reach_gate']},
  {id:'defeat_guard',label:'Pokonaj strażnika',group:'walka',deps:['disable_turrets']},
  {id:'collect_reward',label:'Odbierz nagrodę',group:'nagrody',deps:['defeat_guard']},
  {id:'exit_arena',label:'Opuść arenę',group:'eksploracja',deps:['collect_reward']}
]);
const IDS=TASKS.map(t=>t.id);
const task=id=>TASKS.find(t=>t.id===id);
const own=(o,k)=>Object.prototype.hasOwnProperty.call(o,k);
const exact=(o,keys)=>o!==null&&typeof o==='object'&&!Array.isArray(o)&&Object.keys(o).length===keys.length&&keys.every(k=>own(o,k));
const receiptGood=r=>typeof r==='string'&&/^[a-zA-Z0-9_-]{1,32}$/.test(r);
function initial(){return{version:VERSION,done:[],receipts:[],pin:null,revision:0};}
function valid(s){
 if(!exact(s,['version','done','receipts','pin','revision'])||s.version!==VERSION||!Array.isArray(s.done)||!Array.isArray(s.receipts)||s.done.length!==s.receipts.length||s.done.length>IDS.length||!Number.isSafeInteger(s.revision)||s.revision<0||s.revision>1000000)return false;
 if(s.pin!==null&&!IDS.includes(s.pin))return false;
 const seen=new Set(),rc=new Set();
 for(let i=0;i<s.done.length;i++){
  let id=s.done[i],r=s.receipts[i];if(!IDS.includes(id)||seen.has(id)||!receiptGood(r)||rc.has(r))return false;
  if(task(id).deps.some(dep=>!seen.has(dep)))return false;
  seen.add(id);rc.add(r);
 }
 return s.revision>=s.done.length;
}
const available=(s,id)=>valid(s)&&IDS.includes(id)&&!s.done.includes(id)&&task(id).deps.every(dep=>s.done.includes(dep));
function transition(s,e){
 if(!valid(s))return{ok:false,reason:'INVALID_STATE',state:s};
 if(!exact(e,['type','id','receipt'])||typeof e.type!=='string')return{ok:false,reason:'INVALID_EVENT',state:s};
 let n={version:VERSION,done:[...s.done],receipts:[...s.receipts],pin:s.pin,revision:s.revision};
 if(e.type==='RESET'){
  if(e.id!==null||e.receipt!==null)return{ok:false,reason:'INVALID_RESET',state:s};
  return{ok:true,reason:'RESET',state:initial()};
 }
 if(e.type==='PIN'){
  if(!IDS.includes(e.id)||e.receipt!==null)return{ok:false,reason:'INVALID_PIN',state:s};
  n.pin=n.pin===e.id?null:e.id;n.revision++;
  return{ok:true,reason:'PIN_UPDATED',state:n};
 }
 if(e.type==='COMPLETE'){
  if(!IDS.includes(e.id)||!receiptGood(e.receipt))return{ok:false,reason:'INVALID_COMPLETE',state:s};
  if(n.receipts.includes(e.receipt))return{ok:false,reason:'DUPLICATE_RECEIPT',state:s};
  if(n.done.includes(e.id))return{ok:false,reason:'ALREADY_DONE',state:s};
  if(!available(s,e.id))return{ok:false,reason:'LOCKED',state:s};
  n.done.push(e.id);n.receipts.push(e.receipt);n.revision++;
  return{ok:true,reason:'COMPLETED',state:n};
 }
 return{ok:false,reason:'UNKNOWN_EVENT',state:s};
}
function importState(raw){
 if(typeof raw!=='string'||new TextEncoder().encode(raw).length>4096)return{ok:false,reason:'OVERSIZE_OR_TYPE'};
 try{const candidate=JSON.parse(raw);if(!valid(candidate))return{ok:false,reason:'INVALID_SCHEMA'};return{ok:true,state:structuredClone(candidate)};}catch(_){return{ok:false,reason:'INVALID_JSON'};}
}
function exportState(s){if(!valid(s))throw Error('INVALID_STATE');return JSON.stringify(s,null,2);}
function view(s){if(!valid(s))throw Error('INVALID_STATE');return TASKS.map(t=>({id:t.id,label:t.label,group:t.group,deps:[...t.deps],status:s.done.includes(t.id)?'done':available(s,t.id)?'available':'locked',pinned:s.pin===t.id}));}
return Object.freeze({VERSION,TASKS,IDS,initial,valid,available,transition,importState,exportState,view});
});
