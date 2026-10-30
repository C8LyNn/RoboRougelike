/* B01-12 / P17W17 GARAGE-FILTER-001, pure deterministic query engine */
(function(root,factory){if(typeof module==='object'&&module.exports){module.exports=factory();}else{root.GarageFilter=factory();}})(typeof globalThis!=='undefined'?globalThis:this,function(){
'use strict';
const ERR=Object.freeze({QUERY:'QUERY_INVALID',TOKEN:'TOKEN_INVALID',DUPLICATE:'DUPLICATE_FILTER',VALUE:'FILTER_VALUE_INVALID',CATALOG:'CATALOG_INVALID'});
const TYPES=new Set(['drive','weapon','armor','power','utility']);
const TAGS=new Set(['terrain','rolling','speed','armor','hover','kinetic','ranged','energy','durable','utility','repair']);
const FIELDS=new Set(['mass','cost','energy','slots']);
const FITS=new Set(['light','heavy','both']);
const SEARCH=/^[\p{L}\p{N} _\-]+$/u;
function fail(code){return {ok:false,code,items:[],count:0,facets:{}};}
function normalized(s){return s.toLocaleLowerCase('pl').replace(/ł/g,'l').normalize('NFD').replace(/[\u0300-\u036f]/g,'');}
function tokenize(query){
 if(typeof query!=='string'||query.length>160||/[\x00-\x1F\x7F\\]/.test(query)||/<[A-Za-z/]/.test(query))return null;
 let out=[],buf='',quoted=false;for(const ch of query.trim()){
   if(ch==='"'){quoted=!quoted;continue;}
   if(!quoted&&/\s/.test(ch)){if(buf){out.push(buf);buf='';}}else{buf+=ch;}
   if(out.length>12||buf.length>80)return null;
 }
 if(quoted)return null;if(buf)out.push(buf);return out.length<=12?out:null;
}
function parse(query){
 const tokens=tokenize(query);if(!tokens)return {ok:false,code:ERR.QUERY};
 const config={text:[],type:null,tag:null,fit:null,owned:null,comparisons:[],sort:'name',descending:false};
 const seen=new Set();
 for(const raw of tokens){
  const t=normalized(raw);let m;
  if((m=/^(type|tag|fit|owned|sort):(.+)$/.exec(t))){
   const k=m[1],v=m[2];if(seen.has(k))return {ok:false,code:ERR.DUPLICATE};seen.add(k);
   if(k==='type'){if(!TYPES.has(v))return {ok:false,code:ERR.VALUE};config.type=v;}
   if(k==='tag'){if(!TAGS.has(v))return {ok:false,code:ERR.VALUE};config.tag=v;}
   if(k==='fit'){if(!FITS.has(v))return {ok:false,code:ERR.VALUE};config.fit=v;}
   if(k==='owned'){if(!['yes','no'].includes(v))return {ok:false,code:ERR.VALUE};config.owned=v==='yes';}
   if(k==='sort'){const neg=v[0]==='-';const field=neg?v.slice(1):v;if(!['name','mass','cost','energy','slots'].includes(field))return {ok:false,code:ERR.VALUE};config.sort=field;config.descending=neg;}
  }else if((m=/^(mass|cost|energy|slots)(<=|>=|=)(\d{1,6})$/.exec(t))){
   const key=m[1]+m[2];if(seen.has(key))return {ok:false,code:ERR.DUPLICATE};seen.add(key);
   const num=Number(m[3]);if(!Number.isSafeInteger(num)||num>100000)return {ok:false,code:ERR.VALUE};config.comparisons.push({field:m[1],op:m[2],num});
  }else if(/[:<>=]/.test(t))return {ok:false,code:ERR.TOKEN};
  else if(t.length>0&&t.length<=80&&SEARCH.test(t)){config.text.push(t);}
  else return {ok:false,code:ERR.TOKEN};
 }
 return {ok:true,config};
}
function validCatalog(records){
 if(!Array.isArray(records)||records.length>100||records.length===0)return false;
 const ids=new Set();for(const x of records){
  if(!x||Object.getPrototypeOf(x)!==Object.prototype||typeof x.id!=='string'||!/^[-a-z0-9]{3,32}$/.test(x.id)||ids.has(x.id))return false;
  ids.add(x.id);
  if(typeof x.name!=='string'||x.name.length<1||x.name.length>80||/[<>\x00-\x1F]/.test(x.name)||!TYPES.has(x.type))return false;
  if(!FITS.has(x.fit)||typeof x.owned!=='boolean'||!['mass','cost','energy','slots'].every(k=>Number.isSafeInteger(x[k])&&x[k]>=0&&x[k]<=100000))return false;
  if(!Array.isArray(x.tags)||x.tags.length>8||new Set(x.tags).size!==x.tags.length||!x.tags.every(t=>TAGS.has(t)))return false;
 }
 return true;
}
function search(records,query){
 if(!validCatalog(records))return fail(ERR.CATALOG);
 const p=parse(query);if(!p.ok)return fail(p.code);
 const c=p.config;
 const results=records.filter(x=>{
  if(c.type&&x.type!==c.type)return false;
  if(c.tag&&!x.tags.includes(c.tag))return false;
  if(c.fit&&!(x.fit===c.fit||x.fit==='both'&&c.fit!=='both'))return false;
  if(c.owned!==null&&x.owned!==c.owned)return false;
  if(!c.text.every(t=>normalized([x.name,x.type,...x.tags].join(' ')).includes(t)))return false;
  return c.comparisons.every(({field,op,num})=>op==='='?x[field]===num:op==='<='?x[field]<=num:x[field]>=num);
 });
 const sorted=[...results].sort((a,b)=>{
   let v=c.sort==='name'?normalized(a.name).localeCompare(normalized(b.name),'pl'):a[c.sort]-b[c.sort];
   if(c.descending)v=-v;return v||a.id.localeCompare(b.id);
 });
 const facets={drive:0,weapon:0,armor:0,power:0,utility:0};for(const x of results)facets[x.type]++;
 return {ok:true,items:sorted.map(x=>({...x,tags:[...x.tags]})),count:sorted.length,facets};
}
function exportSelection(result){
 if(!result||result.ok!==true||!Array.isArray(result.items)||result.items.length>100||!result.items.every(x=>x&&typeof x.id==='string'&&/^[-a-z0-9]{3,32}$/.test(x.id)))return fail(ERR.CATALOG);
 return {ok:true,json:JSON.stringify({schema:1,kind:'b01-12-filter-selection',ids:result.items.map(x=>x.id)})};
}
return Object.freeze({parse,search,exportSelection,ERR});
});
