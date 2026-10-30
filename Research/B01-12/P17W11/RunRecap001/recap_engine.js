/* B01-12-RUNRECAP-001/R1: independent, deterministic after-action causality visualizer. */
(function(root,factory){ const api=factory(); if(typeof module==='object'&&module.exports){module.exports=api;} else {root.RunRecap=api;} })(typeof globalThis!=='undefined'?globalThis:this,function(){
'use strict';
const SCHEMA='b01-12.runrecap.v1';
const TYPES=['hit','detach','repair'];
const REASONS=['destroyed','impact','joint_break'];
const SOURCES=['enemy','terrain','self','field','reward'];
const TYPE_LABEL={hit:'Trafienie',detach:'Utrata modułu',repair:'Naprawa'};
const REASON_LABEL={destroyed:'Zniszczenie HP',impact:'Uderzenie',joint_break:'Zerwanie połączenia'};
const SOURCE_LABEL={enemy:'Wróg',terrain:'Teren',self:'Własny pojazd',field:'Naprawa polowa',reward:'Nagroda'};
const own=(o,k)=>Object.prototype.hasOwnProperty.call(o,k);
const isObject=o=>o!==null&&typeof o==='object'&&!Array.isArray(o)&&Object.getPrototypeOf(o)===Object.prototype;
function exact(o,keys){return isObject(o)&&Object.keys(o).length===keys.length&&keys.every(k=>own(o,k));}
const integer=(n,min,max)=>Number.isSafeInteger(n)&&n>=min&&n<=max;
const key=s=>typeof s==='string'&&/^[A-Za-z0-9_-]{1,32}$/.test(s);
const label=s=>typeof s==='string'&&s.trim().length>=1&&s.length<=45&& !/[<>\u0000-\u001f]/.test(s);
const fail=m=>{throw new Error(m);};
function parse(input){
 if(typeof input==='string'){
  if(input.length>100000)fail('Plik przekracza limit 100 KB');
  try{input=JSON.parse(input)}catch(_){fail('Niepoprawny JSON')}
 }
 if(!exact(input,['schema','runId','durationMs','outcome','modules','events']))fail('Nieznany schemat lub dodatkowe pola');
 if(input.schema!==SCHEMA||!key(input.runId)||!integer(input.durationMs,1,3600000)||!['victory','defeat'].includes(input.outcome))fail('Nieprawidłowe metadane');
 if(!Array.isArray(input.modules)||input.modules.length<1||input.modules.length>24)fail('Limit modułów 1–24');
 const known=new Map();
 for(const m of input.modules){if(!exact(m,['id','name','hpStart'])||!key(m.id)||!label(m.name)||!integer(m.hpStart,1,500)||known.has(m.id))fail('Niepoprawny lub zduplikowany moduł'); known.set(m.id,{hp:m.hpStart,max:m.hpStart,detached:false,name:m.name,lastHit:null});}
 if(!Array.isArray(input.events)||input.events.length>300)fail('Limit zdarzeń: 300');
 let lastTime=-1;const eventIds=new Set();
 for(const e of input.events){
  if(!isObject(e)||!TYPES.includes(e.type))fail('Nieznany typ zdarzenia');
  const fields=e.type==='detach'?['id','t','type','moduleId','reason','source']:['id','t','type','moduleId','amount','source'];
  if(!exact(e,fields)||!key(e.id)||eventIds.has(e.id)||!integer(e.t,0,input.durationMs)||e.t<lastTime||!known.has(e.moduleId)||!SOURCES.includes(e.source))fail('Nieprawidłowe zdarzenie, kolejność lub referencja');
  eventIds.add(e.id);lastTime=e.t;
  const state=known.get(e.moduleId);
  if(state.detached)fail('Operacja na odłączonym module');
  if(e.type==='hit') {if(!integer(e.amount,1,500)||!['enemy','terrain','self'].includes(e.source))fail('Niepoprawne obrażenia');state.hp=Math.max(0,state.hp-e.amount);state.lastHit={t:e.t,source:e.source};}
  if(e.type==='repair'){if(!integer(e.amount,1,500)||!['field','reward'].includes(e.source)||state.hp===state.max)fail('Niedozwolona naprawa');state.hp=Math.min(state.max,state.hp+e.amount);}
  if(e.type==='detach'){if(!REASONS.includes(e.reason)||!['enemy','terrain','self'].includes(e.source)||(e.reason==='destroyed'&&state.hp!==0))fail('Niepoprawny demontaż');state.detached=true;}
 }
 return JSON.parse(JSON.stringify(input));
}
function derive(input){
 const run=parse(input);const modules=new Map(run.modules.map(m=>[m.id,{id:m.id,name:m.name,max:m.hpStart,hp:m.hpStart,detached:false,lastHit:null,damage:0,healed:0}]));
 const timeline=[];let damage=0,restored=0,lost=0;
 for(const e of run.events){
  const m=modules.get(e.moduleId);let description='',cause=null;
  if(e.type==='hit'){
   const dealt=Math.min(m.hp,e.amount);m.hp-=dealt;m.damage+=dealt;damage+=dealt;m.lastHit={time:e.t,source:e.source};
   description=`${SOURCE_LABEL[e.source]}: ${m.name} −${dealt} HP${e.amount>dealt?' (nadmiar '+(e.amount-dealt)+')':''}`;
  } else if(e.type==='repair') {
   const gained=Math.min(m.max-m.hp,e.amount);m.hp+=gained;m.healed+=gained;restored+=gained;description=`${SOURCE_LABEL[e.source]}: ${m.name} +${gained} HP`;
  } else {
   m.detached=true;lost++;
   if(e.reason==='destroyed'&&m.lastHit){cause=`Ostatnie trafienie: ${SOURCE_LABEL[m.lastHit.source]}, ${e.t-m.lastHit.time} ms wcześniej`;}
   else cause=`Zdarzenie strukturalne: ${REASON_LABEL[e.reason]}`;
   description=`${m.name} odłączony — ${REASON_LABEL[e.reason]}`;
  }
  timeline.push({id:e.id,t:e.t,type:e.type,moduleId:m.id,moduleName:m.name,source:e.source,description,cause,hpAfter:m.hp,detached:m.detached});
 }
 return {schema:SCHEMA,runId:run.runId,durationMs:run.durationMs,outcome:run.outcome,totalDamage:damage,totalRestored:restored,lostModules:lost,totalModules:run.modules.length,timeline,modules:Array.from(modules.values()).map(m=>({id:m.id,name:m.name,hp:m.hp,max:m.max,detached:m.detached,damage:m.damage,healed:m.healed}))};
}
function filter(report,opts={}){
 if(!isObject(report)||!Array.isArray(report.timeline)||!isObject(opts))fail('Niepoprawny raport lub filtr');
 const type=opts.type===undefined?'all':opts.type;
 const source=opts.source===undefined?'all':opts.source;
 const query=opts.query===undefined?'':opts.query;
 if(!['all',...TYPES].includes(type)||!['all',...SOURCES].includes(source)||typeof query!=='string'||query.length>80)fail('Nieprawidłowy filtr');
 const q=query.toLocaleLowerCase('pl');
 return report.timeline.filter(e=>(type==='all'||e.type===type)&&(source==='all'||e.source===source)&&(!q||[e.moduleName,e.description,e.cause||''].some(s=>s.toLocaleLowerCase('pl').includes(q))));
}
function sample(){return {
 schema:SCHEMA,runId:'W11-ARENA-01',durationMs:60000,outcome:'defeat',modules:[
 {id:'core',name:'Rdzeń',hpStart:150},{id:'wheel',name:'Lewe koło',hpStart:90},{id:'armor',name:'Pancerz boczny',hpStart:100},{id:'gun',name:'Działko',hpStart:70}],events:[
 {id:'e01',t:4200,type:'hit',moduleId:'armor',amount:60,source:'enemy'},
 {id:'e02',t:6500,type:'hit',moduleId:'wheel',amount:25,source:'terrain'},
 {id:'e03',t:7800,type:'hit',moduleId:'armor',amount:45,source:'enemy'},
 {id:'e04',t:7810,type:'detach',moduleId:'armor',reason:'destroyed',source:'enemy'},
 {id:'e05',t:10400,type:'repair',moduleId:'wheel',amount:15,source:'field'},
 {id:'e06',t:18100,type:'hit',moduleId:'gun',amount:20,source:'enemy'},
 {id:'e07',t:21200,type:'detach',moduleId:'wheel',reason:'impact',source:'terrain'},
 {id:'e08',t:40200,type:'hit',moduleId:'core',amount:120,source:'enemy'},
 {id:'e09',t:48200,type:'hit',moduleId:'core',amount:40,source:'enemy'},
 {id:'e10',t:48205,type:'detach',moduleId:'core',reason:'destroyed',source:'enemy'}]};}
return Object.freeze({SCHEMA,TYPES,SOURCES,TYPE_LABEL,SOURCE_LABEL,REASON_LABEL,parse,derive,filter,sample});
});
