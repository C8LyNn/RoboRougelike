/* B01-12-BOXSELECT-001/R1. Pure, deterministic selection/move engine. No game writes. */
(function(root,factory){const api=factory();if(typeof module==='object'&&module.exports)module.exports=api;else root.BoxSelect=api;})(typeof globalThis!=='undefined'?globalThis:this,function(){
'use strict';
const WIDTH=800,HEIGHT=500,MAX_HISTORY=24;
const BLUEPRINT=Object.freeze([
{id:'core',label:'Core reactor',kind:'core',x:348,y:196,w:104,h:88,locked:true},
{id:'wheel-fl',label:'Front left wheel',kind:'mobility',x:168,y:108,w:88,h:68,locked:false},
{id:'wheel-fr',label:'Front right wheel',kind:'mobility',x:544,y:108,w:88,h:68,locked:false},
{id:'wheel-rl',label:'Rear left wheel',kind:'mobility',x:168,y:332,w:88,h:68,locked:false},
{id:'wheel-rr',label:'Rear right wheel',kind:'mobility',x:544,y:332,w:88,h:68,locked:false},
{id:'gun-a',label:'Railgun A',kind:'weapon',x:312,y:64,w:72,h:56,locked:false},
{id:'gun-b',label:'Railgun B',kind:'weapon',x:416,y:64,w:72,h:56,locked:false},
{id:'armor-l',label:'Left armor',kind:'armor',x:64,y:220,w:104,h:64,locked:false},
{id:'armor-r',label:'Right armor',kind:'armor',x:632,y:220,w:104,h:64,locked:false},
{id:'rad-a',label:'Coolant bank',kind:'utility',x:308,y:380,w:80,h:60,locked:false},
{id:'rad-b',label:'Auxiliary cell',kind:'utility',x:412,y:380,w:80,h:60,locked:false}
]);
const plain=v=>v!==null&&typeof v==='object'&&(Object.getPrototypeOf(v)===Object.prototype||Object.getPrototypeOf(v)===null);
const keys=(o,k)=>plain(o)&&Object.keys(o).sort().join('|')===k.slice().sort().join('|');
const int=(v,lo,hi)=>Number.isSafeInteger(v)&&v>=lo&&v<=hi;
function validParts(parts){if(!Array.isArray(parts)||parts.length!==BLUEPRINT.length)return false;
  const ids=new Set();for(const p of parts){if(!keys(p,['id','label','kind','x','y','w','h','locked'])||typeof p.id!=='string'||ids.has(p.id))return false;ids.add(p.id);
    const b=BLUEPRINT.find(x=>x.id===p.id);if(!b||p.label!==b.label||p.kind!==b.kind||p.locked!==b.locked||p.w!==b.w||p.h!==b.h||!int(p.x,0,WIDTH-p.w)||!int(p.y,0,HEIGHT-p.h))return false;}
  for(let a=0;a<parts.length;a++)for(let b=a+1;b<parts.length;b++)if(intersects(parts[a],parts[b]))return false;
  return true;
}
function validSnapshot(s){return keys(s,['parts','selected'])&&validParts(s.parts)&&Array.isArray(s.selected)&&s.selected.length<=BLUEPRINT.length&&new Set(s.selected).size===s.selected.length&&s.selected.every(id=>typeof id==='string'&&s.parts.some(p=>p.id===id&&!p.locked));}
function validState(s){return keys(s,['version','rev','parts','selected','undo','redo'])&&s.version===1&&int(s.rev,0,1000000)&&validSnapshot({parts:s.parts,selected:s.selected})&&Array.isArray(s.undo)&&s.undo.length<=MAX_HISTORY&&s.undo.every(validSnapshot)&&Array.isArray(s.redo)&&s.redo.length<=MAX_HISTORY&&s.redo.every(validSnapshot);}
function copyParts(parts){return parts.map(p=>({...p}));}
function copySnap(s){return {parts:copyParts(s.parts),selected:[...s.selected]};}
function copyState(s){return {version:1,rev:s.rev,parts:copyParts(s.parts),selected:[...s.selected],undo:s.undo.map(copySnap),redo:s.redo.map(copySnap)};}
function initial(){return {version:1,rev:0,parts:BLUEPRINT.map(p=>({...p})),selected:[],undo:[],redo:[]};}
function intersects(a,b){return a.x<b.x+b.w&&a.x+a.w>b.x&&a.y<b.y+b.h&&a.y+a.h>b.y;}
function contains(box,p){return p.x>=box.x&&p.y>=box.y&&p.x+p.w<=box.x+box.w&&p.y+p.h<=box.y+box.h;}
function fail(s,reason){return {ok:false,reason,state:s};}
function commit(s,next){next.rev=s.rev+1;next.undo=[...s.undo.map(copySnap),copySnap(s)].slice(-MAX_HISTORY);next.redo=[];return {ok:true,reason:'OK',state:next};}
function selection(s,ids,mode){if(!Array.isArray(ids)||ids.length>BLUEPRINT.length||ids.some(id=>typeof id!=='string')||new Set(ids).size!==ids.length)return fail(s,'BAD_SELECTION');
  if(ids.some(id=>!s.parts.some(p=>p.id===id&&!p.locked)))return fail(s,'LOCKED_OR_UNKNOWN');
  const selected=new Set(mode==='replace'?[]:s.selected);for(const id of ids){if(mode==='subtract')selected.delete(id);else if(mode==='toggle'){selected.has(id)?selected.delete(id):selected.add(id);}else selected.add(id);}
  const canonical=s.parts.filter(p=>selected.has(p.id)).map(p=>p.id);if(canonical.join('|')===s.selected.join('|'))return {ok:true,reason:'NO_CHANGE',state:s};
  const n=copyState(s);n.selected=canonical;return commit(s,n);
}
function step(s,e){if(!validState(s))return fail(s,'INVALID_STATE');if(!plain(e)||typeof e.type!=='string')return fail(s,'BAD_EVENT');if(s.rev===1000000)return fail(s,'REV_LIMIT');
  const mode=e.mode;const modes=['replace','add','subtract','toggle'];
  if(e.type==='SELECT'){
    if(!keys(e,['type','ids','mode'])||!modes.includes(mode))return fail(s,'BAD_SELECT_EVENT');return selection(s,e.ids,mode);
  }
  if(e.type==='RECT'){
    if(!keys(e,['type','x','y','w','h','mode','contain'])||!modes.includes(mode)||!['touch','inside'].includes(e.contain)||!int(e.x,0,WIDTH-1)||!int(e.y,0,HEIGHT-1)||!int(e.w,1,WIDTH-e.x)||!int(e.h,1,HEIGHT-e.y))return fail(s,'BAD_RECT');
    const rect=e;const ids=s.parts.filter(p=>!p.locked&&(e.contain==='touch'?intersects(rect,p):contains(rect,p))).map(p=>p.id);
    return selection(s,ids,mode);
  }
  if(e.type==='MOVE'){
    if(!keys(e,['type','dx','dy'])||!int(e.dx,-100,100)||!int(e.dy,-100,100)||(e.dx===0&&e.dy===0))return fail(s,'BAD_MOVE');
    if(s.selected.length===0)return fail(s,'NO_SELECTION');const n=copyState(s);const chosen=new Set(n.selected);
    for(const p of n.parts)if(chosen.has(p.id)){p.x+=e.dx;p.y+=e.dy;if(!int(p.x,0,WIDTH-p.w)||!int(p.y,0,HEIGHT-p.h))return fail(s,'OUT_OF_BOUNDS');}
    if(!validParts(n.parts))return fail(s,'COLLISION');return commit(s,n);
  }
  if(e.type==='UNDO'||e.type==='REDO'){
    if(!keys(e,['type']))return fail(s,'BAD_HISTORY_EVENT');const source=e.type==='UNDO'?'undo':'redo';const dest=source==='undo'?'redo':'undo';if(!s[source].length)return fail(s,'EMPTY_HISTORY');
    const n=copyState(s),target=n[source].pop();n[dest].push(copySnap(s));n.parts=copyParts(target.parts);n.selected=[...target.selected];n.rev=s.rev+1;return {ok:true,reason:'OK',state:n};
  }
  if(e.type==='RESET'){if(!keys(e,['type']))return fail(s,'BAD_RESET');const n=initial();n.rev=s.rev+1;return commit(s,n);}
  return fail(s,'UNKNOWN_EVENT');
}
function snapshot(s){if(!validState(s))throw Error('INVALID_STATE');return JSON.stringify({version:1,revision:s.rev,selected:s.selected,parts:s.parts.map(p=>({id:p.id,x:p.x,y:p.y}))});}
return Object.freeze({WIDTH,HEIGHT,MAX_HISTORY,initial,step,validState,snapshot,intersects});
});
