(function(root,factory){if(typeof module==='object'&&module.exports)module.exports=factory();else root.ShortcutGuide=factory();})(typeof globalThis!=='undefined'?globalThis:this,function(){
'use strict';
const VERSION='b01-12.shortcut-guide.v1';
const SCOPES=Object.freeze(['garage','combat','map']);
const CATEGORIES=Object.freeze(['general','navigation','building','weapons','support']);
const MAX_QUERY=64,MAX_REV=1000000,MAX_ACTIONS=64;
const COMMANDS=Object.freeze([
 ['help','global','general','?','Shortcut guide','Open or close the command guide'],
 ['pause','global','general','Escape','Pause / back','Return to the previous UI layer'],
 ['screenshot','global','general','F12','Screenshot','Capture the current screen'],
 ['garage-move','garage','building','G','Move module','Pick up a selected module'],
 ['garage-rotate','garage','building','R','Rotate module','Rotate the selected part by a quarter turn'],
 ['garage-snap','garage','building','Shift+S','Toggle snap','Toggle socket-grid alignment'],
 ['garage-undo','garage','building','Ctrl+Z','Undo','Reverse a garage edit'],
 ['garage-redo','garage','building','Ctrl+Y','Redo','Reapply an undone garage edit'],
 ['garage-save','garage','general','Ctrl+S','Save draft','Save the experimental garage draft'],
 ['garage-find','garage','navigation','Ctrl+F','Find part','Focus the part-search field'],
 ['combat-fire1','combat','weapons','Mouse1','Primary fire','Fire the primary weapon group'],
 ['combat-fire2','combat','weapons','Mouse2','Secondary fire','Fire the secondary group'],
 ['combat-boost','combat','navigation','Shift','Boost','Activate available boost'],
 ['combat-lock','combat','weapons','T','Target focus','Prioritize a visible opponent'],
 ['combat-repair','combat','support','H','Repair','Use an available repair charge'],
 ['combat-swap','combat','weapons','Q','Switch group','Cycle weapon focus'],
 ['map-ping','map','navigation','P','Place ping','Put a tactical marker on the map'],
 ['map-remove','map','navigation','Delete','Remove ping','Remove the selected tactical marker'],
 ['map-cycle','map','navigation','Tab','Next marker','Select the next marker on the map'],
 ['map-zoom','map','navigation','Ctrl+Plus','Zoom in','Increase tactical map zoom']
].map(a=>Object.freeze({id:a[0],scope:a[1],category:a[2],key:a[3],title:a[4],description:a[5]})));
function fail(code){return Object.freeze({ok:false,error:code});}
function plain(x){return x!==null&&typeof x==='object'&&(Object.getPrototypeOf(x)===Object.prototype||Object.getPrototypeOf(x)===null);}
function own(x,key){return Object.prototype.hasOwnProperty.call(x,key);}
function keys(x,expected){return plain(x)&&Object.keys(x).length===expected.length&&expected.every(k=>own(x,k));}
function norm(s){return s.normalize('NFD').replace(/[\u0300-\u036f]/g,'').replace(/ł/g,'l').replace(/Ł/g,'L').toLocaleLowerCase('en').trim();}
function validateCommands(list){
 if(!Array.isArray(list)||list.length>MAX_ACTIONS||list.length===0)return fail('INVALID_REGISTRY_SIZE');
 const ids=new Set(),chords=new Map();
 for(const c of list){
  if(!keys(c,['id','scope','category','key','title','description'])||!Object.values(c).every(v=>typeof v==='string'))return fail('INVALID_COMMAND_SCHEMA');
  if(!/^[a-z][a-z0-9-]{1,40}$/.test(c.id)||!['global',...SCOPES].includes(c.scope)||!CATEGORIES.includes(c.category)||!c.key||c.key.length>24||/[^a-zA-Z0-9+? ]/.test(c.key)||c.title.length<1||c.title.length>56||c.description.length<1||c.description.length>120)return fail('INVALID_COMMAND_VALUE');
  if(ids.has(c.id))return fail('DUPLICATE_COMMAND_ID');ids.add(c.id);
  const key=norm(c.key);if(!chords.has(key))chords.set(key,[]);chords.get(key).push(c);
 }
 const conflicts=[];
 for(const entries of chords.values())for(let i=0;i<entries.length;i++)for(let j=i+1;j<entries.length;j++)if(entries[i].scope==='global'||entries[j].scope==='global'||entries[i].scope===entries[j].scope)conflicts.push(Object.freeze([entries[i].id,entries[j].id]));
 if(conflicts.length)return Object.freeze({ok:false,error:'KEY_CONFLICT',conflicts:Object.freeze(conflicts)});
 return Object.freeze({ok:true,count:list.length});
}
function getCommands(scope,query='',registry=COMMANDS){
 if(!SCOPES.includes(scope)||typeof query!=='string'||query.length>MAX_QUERY||/[\u0000-\u001f\u007f]/.test(query))return fail('INVALID_QUERY');
 const valid=validateCommands(registry);if(!valid.ok)return valid;
 const words=norm(query).split(/\s+/).filter(Boolean);
 const result=registry.filter(c=>(c.scope==='global'||c.scope===scope)&&words.every(w=>norm([c.key,c.title,c.description,c.category].join(' ')).includes(w))).sort((a,b)=>a.category.localeCompare(b.category)||a.title.localeCompare(b.title)||a.id.localeCompare(b.id));
 return Object.freeze({ok:true,commands:Object.freeze(result.map(c=>Object.freeze({...c})))});
}
const FIELDS=['version','scope','query','selected','open','revision'];
function validateState(s){return keys(s,FIELDS)&&s.version===VERSION&&SCOPES.includes(s.scope)&&typeof s.query==='string'&&s.query.length<=MAX_QUERY&&!/[\u0000-\u001f\u007f]/.test(s.query)&&(s.selected===null||typeof s.selected==='string'&&s.selected.length<=42)&&typeof s.open==='boolean'&&Number.isSafeInteger(s.revision)&&s.revision>=0&&s.revision<=MAX_REV;}
function initial(){return Object.freeze({version:VERSION,scope:'garage',query:'',selected:null,open:true,revision:0});}
function reduce(state,event,registry=COMMANDS){
 if(!validateState(state))return fail('INVALID_STATE');const v=validateCommands(registry);if(!v.ok)return v;
 if(!plain(event)||!keys(event,['type','value'])||typeof event.type!=='string')return fail('INVALID_EVENT');
 if(state.revision===MAX_REV)return fail('REVISION_EXHAUSTED');
 let next={...state};
 if(event.type==='SCOPE'){
  if(typeof event.value!=='string'||!SCOPES.includes(event.value))return fail('INVALID_SCOPE');next.scope=event.value;next.selected=null;next.query='';next.open=true;
 }else if(event.type==='QUERY'){
  if(typeof event.value!=='string'||event.value.length>MAX_QUERY||/[\u0000-\u001f\u007f]/.test(event.value))return fail('INVALID_QUERY');next.query=event.value;next.selected=null;next.open=true;
 }else if(event.type==='SELECT'){
  if(typeof event.value!=='string')return fail('INVALID_SELECTION');const c=getCommands(state.scope,state.query,registry);if(!c.ok)return c;if(!c.commands.some(x=>x.id===event.value))return fail('UNKNOWN_OR_HIDDEN_COMMAND');next.selected=event.value;next.open=true;
 }else if(event.type==='TOGGLE'){
  if(event.value!==null)return fail('INVALID_TOGGLE');next.open=!state.open;next.selected=null;
 }else if(event.type==='CLOSE'){
  if(event.value!==null)return fail('INVALID_CLOSE');next.open=false;next.selected=null;
 }else if(event.type==='RESET'){
  if(event.value!==null)return fail('INVALID_RESET');next={...initial()};
 }else return fail('UNKNOWN_EVENT');
 next.revision=state.revision+1;return Object.freeze({ok:true,state:Object.freeze(next)});
}
function serialize(s){if(!validateState(s))return fail('INVALID_STATE');return Object.freeze({ok:true,json:JSON.stringify(s)});}
function restore(json,registry=COMMANDS){
 if(typeof json!=='string'||json.length>512||json.length===0)return fail('INVALID_JSON_SIZE');let s;try{s=JSON.parse(json)}catch{return fail('BAD_JSON')}
 if(!validateState(s))return fail('INVALID_STATE');const v=validateCommands(registry);if(!v.ok)return v;
 if(s.selected!==null&&!getCommands(s.scope,s.query,registry).commands.some(c=>c.id===s.selected))return fail('STALE_SELECTION');
 if(s.selected!==null&&!s.open)return fail('HIDDEN_SELECTION');return Object.freeze({ok:true,state:Object.freeze({...s})});
}
return Object.freeze({VERSION,SCOPES,COMMANDS,initial,validateState,validateCommands,getCommands,reduce,serialize,restore});
});
