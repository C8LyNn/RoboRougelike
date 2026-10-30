/* B01-12-LOCALIZE-001 R1: browser/Node portable, no game writes. */
(function(root,factory){ 'use strict'; const lib=factory(); if(typeof module==='object'&&module.exports)module.exports=lib;else root.B01Localize=lib; })(typeof globalThis!=='undefined'?globalThis:this,function(){
'use strict';
const LOCALES=Object.freeze(['pl','en','pseudo']);
const KEY_RE=/^[a-z][a-z0-9]*(?:\.[a-z][a-z0-9]*)+$/;
const SLOT_RE=/\{([a-z][a-z0-9]*)\}/g;
const ALLOWED_SLOTS=new Set(['count','weapon','phase','scrap','key','module']);
const MAX_ENTRY=400;
function fail(msg){throw new Error('LOCALIZE_INVALID: '+msg)}
function object(x){return x!==null && typeof x==='object' && !Array.isArray(x) && Object.getPrototypeOf(x)===Object.prototype}
function tokens(s){return [...s.matchAll(SLOT_RE)].map(m=>m[1]).sort().join('|')}
function shape(entry){return typeof entry==='string'?'text':object(entry)&&entry.$plural==='count'?'plural':'invalid'}
function checkText(text,key){if(typeof text!=='string'||!text||text.length>MAX_ENTRY||/[<>\u0000-\u001f]/.test(text))fail(key+' unsafe/nontext');
 const replaced=text.replace(SLOT_RE,'');if(/[{}]/.test(replaced))fail(key+' malformed slot'); for(const t of text.matchAll(SLOT_RE))if(!ALLOWED_SLOTS.has(t[1]))fail(key+' slot '+t[1]); return tokens(text)}
function validate(catalog){if(!object(catalog)||Object.keys(catalog).sort().join(',')!=='en,pl,version'||catalog.version!==1||!object(catalog.pl)||!object(catalog.en))fail('catalog structure');
 const keys=Object.keys(catalog.en).sort();if(keys.length<8||keys.length>96||keys.some(k=>!KEY_RE.test(k))||JSON.stringify(keys)!==JSON.stringify(Object.keys(catalog.pl).sort()))fail('key parity');
 for(const key of keys){const en=catalog.en[key],pl=catalog.pl[key],type=shape(en);if(type==='invalid'||type!==shape(pl))fail(key+' kind mismatch');
  if(type==='text'){if(checkText(en,key)!==checkText(pl,key))fail(key+' slot parity')}
  else {const forms=['one','few','many','other'];for(const [locale,e] of [['en',en],['pl',pl]]){
   if(Object.keys(e).sort().join(',')!=='$plural,few,many,one,other')fail(key+' '+locale+' plural shape');
   const signatures=forms.map(f=>checkText(e[f],key+' '+f));if(new Set(signatures).size!==1)fail(key+' '+locale+' plural slot mismatch');
   if(!signatures[0].split('|').includes('count'))fail(key+' plural missing count');
  } if(tokens(en.other)!==tokens(pl.other))fail(key+' plural locale slot parity');
  }
 }
 return Object.freeze({version:1,keys:Object.freeze(keys.slice()),count:keys.length});}
function plural(locale,n){if(!Number.isSafeInteger(n)||n<0||n>1000000)fail('invalid count');if(locale==='en')return n===1?'one':'other';
 if(n===1)return 'one';const mod10=n%10,mod100=n%100; if(mod10>=2&&mod10<=4 && !(mod100>=12&&mod100<=14))return 'few';return 'many';}
const ACCENT=Object.freeze({a:'à',b:'ƀ',c:'ç',d:'ď',e:'ë',f:'ƒ',g:'ğ',h:'ĥ',i:'ï',j:'ĵ',k:'ķ',l:'ľ',m:'ɱ',n:'ñ',o:'õ',p:'ƥ',q:'ʠ',r:'ř',s:'š',t:'ŧ',u:'ü',v:'ṽ',w:'ŵ',x:'ẋ',y:'ÿ',z:'ž'});
function pseudo(s){let out='';for(const char of s){const c=ACCENT[char.toLowerCase()];out+=c?(char===char.toUpperCase()?c.toUpperCase():c):char;}return '⟦'+out+' ~~⟧'}
function make(catalog){const info=validate(catalog);
 function translate(locale,key,params={}){if(!LOCALES.includes(locale))fail('unknown locale');if(!info.keys.includes(key))fail('unknown key '+key);if(!object(params))fail('params');
  for(const [k,v] of Object.entries(params)){if(!ALLOWED_SLOTS.has(k)|| !(typeof v==='string'||(typeof v==='number'&&Number.isFinite(v)))||String(v).length>80 || /[<>\u0000-\u001f]/.test(String(v)))fail('unsafe param '+k)}
  const chosen=locale==='pseudo'?'en':locale;const entry=catalog[chosen][key];let src=typeof entry==='string'?entry:entry[plural(chosen,params.count)];let required=[...src.matchAll(SLOT_RE)].map(m=>m[1]);for(const k of required)if(!Object.hasOwn(params,k))fail('missing param '+k);
  const rendered=src.replace(SLOT_RE,(_,k)=>String(params[k]));return locale==='pseudo'?pseudo(rendered):rendered;
 }
 return Object.freeze({translate,keys:info.keys,plural});
}
return Object.freeze({make,validate,plural,pseudo,LOCALES});
});
