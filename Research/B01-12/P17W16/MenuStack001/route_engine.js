/* B01-12-MENUSTACK-001 R1. Pure deterministic state machine, no game side effects. */
(function(root,factory){const api=factory();if(typeof module==='object'&&module.exports)module.exports=api;else root.MenuStack=api;})(typeof globalThis!=='undefined'?globalThis:this,function(){
'use strict';
const ROUTES=Object.freeze(['garage','parts','inspector','arena','pause','rewards','settings']);
const PARENTS=Object.freeze({garage:null,parts:'garage',inspector:'parts',arena:null,pause:'arena',rewards:'arena',settings:null});
const MAX_HISTORY=16, MAX_REVISION=1000000;
const own=(o,k)=>Object.prototype.hasOwnProperty.call(o,k);
const plain=o=>o!==null&&typeof o==='object'&&!Array.isArray(o)&&Object.getPrototypeOf(o)===Object.prototype;
const keys=(o,allowed)=>plain(o)&&Object.keys(o).length===allowed.length&&allowed.every(k=>own(o,k));
const validRoute=x=>typeof x==='string'&&ROUTES.includes(x);
const isInt=x=>Number.isSafeInteger(x)&&x>=0;
function initial(){return {schema:1,history:['garage'],index:0,dirty:false,pending:null,revision:0};}
function valid(s){if(!keys(s,['schema','history','index','dirty','pending','revision'])||s.schema!==1||!Array.isArray(s.history)||s.history.length<1||s.history.length>MAX_HISTORY||!s.history.every(validRoute)||!isInt(s.index)||s.index>=s.history.length||typeof s.dirty!=='boolean'||!isInt(s.revision)||s.revision>MAX_REVISION)return false;
 if(s.pending!==null){const p=s.pending;if(!keys(p,['type','route','index'])||!['NAVIGATE','BACK','FORWARD'].includes(p.type)||!validRoute(p.route)||!isInt(p.index)||p.index>=MAX_HISTORY)return false;
 if(p.type==='NAVIGATE'&&(p.index!==s.index||p.route===s.history[s.index]))return false;
 if(p.type==='BACK'&&(p.index!==s.index-1||s.history[p.index]!==p.route))return false;
 if(p.type==='FORWARD'&&(p.index!==s.index+1||s.history[p.index]!==p.route))return false;
 if(!s.dirty)return false;}
 return true;}
function view(s){if(!valid(s))return null;const route=s.history[s.index];const ancestors=[];let r=route;while(r!==null){ancestors.unshift(r);r=PARENTS[r];}return {route,breadcrumbs:ancestors,canBack:s.index>0,canForward:s.index<s.history.length-1,history:s.history.slice(),index:s.index,dirty:s.dirty,guard:s.pending?{...s.pending}:null,revision:s.revision};}
function reject(s,reason){return {state:s,status:reason,changed:false};}
function apply(s,request){const next={...s,history:s.history.slice(),pending:null,revision:s.revision+1};if(request.type==='NAVIGATE'){
 const prefix=next.history.slice(0,next.index+1);prefix.push(request.route);if(prefix.length>MAX_HISTORY)prefix.shift();next.history=prefix;next.index=prefix.length-1;
}else next.index=request.index;
 next.dirty=false;return {state:next,status:'NAVIGATED',changed:true};}
function step(s,event){if(!valid(s))return reject(s,'INVALID_STATE');if(!plain(event)||typeof event.type!=='string')return reject(s,'INVALID_EVENT');
 if(s.revision>=MAX_REVISION&&event.type!=='RESET')return reject(s,'REVISION_LIMIT');
 if(event.type==='RESET'){
  if(!keys(event,['type']))return reject(s,'INVALID_EVENT');return {state:initial(),status:'RESET',changed:true};}
 if(s.pending!==null){if(event.type==='CANCEL'&&keys(event,['type'])){return {state:{...s,pending:null,revision:s.revision+1},status:'STAYED',changed:true};}
  if(event.type==='DISCARD'&&keys(event,['type']))return apply(s,s.pending);
  return reject(s,'GUARD_ACTIVE');}
 if(event.type==='SET_DIRTY'){
  if(!keys(event,['type','value'])||typeof event.value!=='boolean')return reject(s,'INVALID_EVENT');if(s.dirty===event.value)return reject(s,'UNCHANGED');return {state:{...s,dirty:event.value,revision:s.revision+1},status:event.value?'DIRTY':'CLEAN',changed:true};}
 let req;
 if(event.type==='NAVIGATE'){
  if(!keys(event,['type','route'])||!validRoute(event.route))return reject(s,'INVALID_ROUTE');if(event.route===s.history[s.index])return reject(s,'SAME_ROUTE');req={type:'NAVIGATE',route:event.route,index:s.index};
 }else if(event.type==='BACK'||event.type==='FORWARD'){
  if(!keys(event,['type']))return reject(s,'INVALID_EVENT');const i=s.index+(event.type==='BACK'?-1:1);if(i<0||i>=s.history.length)return reject(s,'HISTORY_BOUND');req={type:event.type,route:s.history[i],index:i};
 }else return reject(s,'INVALID_EVENT');
 if(s.dirty)return {state:{...s,pending:req,revision:s.revision+1},status:'GUARD_REQUIRED',changed:true};
 return apply(s,req);
}
return Object.freeze({ROUTES,PARENTS,MAX_HISTORY,initial,valid,view,step});
});
