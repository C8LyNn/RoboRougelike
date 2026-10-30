#!/usr/bin/env node
'use strict';
const fs=require('fs'),path=require('path'),assert=require('assert/strict'),crypto=require('crypto');
const ROOT=__dirname, m=JSON.parse(fs.readFileSync(path.join(ROOT,'manifest.json'),'utf8'));
function sha(b){return crypto.createHash('sha256').update(b).digest('hex')}
for(const [name,pin] of Object.entries(m.pinned)){
  const b=fs.readFileSync(path.join(ROOT,name));
  if(b.length!==pin.bytes || sha(b)!==pin.sha256){console.error('FAIL_CLOSED source mismatch: '+name);process.exit(2)}
}
const api=require('./a11y_engine.js');const cases=[];
function check(label,fn){try{fn();cases.push({case:label,pass:true})}catch(e){cases.push({case:label,pass:false,error:String(e)});console.error('FAIL',label,e.message)}}
function action(s,e){return api.transition(s,e)}
check('fresh defaults',()=>assert.deepEqual(api.fresh().prefs,api.DEFAULT));
check('fresh distinct object',()=>assert.notStrictEqual(api.fresh().prefs,api.fresh().prefs));
check('schema',()=>assert.equal(api.SCHEMA,'b01-12.a11y-profile'));
check('version',()=>assert.equal(api.VERSION,1));
check('fresh validity',()=>assert.equal(api.validState(api.fresh()),true));
check('default contrast >= 7',()=>assert.ok(api.preview(api.DEFAULT).minimumTextContrast>=7));
check('high contrast >= 7',()=>assert.ok(api.preview({...api.DEFAULT,contrast:'high'}).minimumTextContrast>=7));
check('standard animation',()=>assert.equal(api.preview(api.DEFAULT).animationMs,220));
check('reduced motion zero duration',()=>assert.equal(api.preview({...api.DEFAULT,motion:'reduced'}).animationMs,0));
check('standard captions visible',()=>assert.equal(api.preview(api.DEFAULT).showCaptions,true));
check('captions toggle',()=>assert.equal(api.preview({...api.DEFAULT,captions:false}).showCaptions,false));
check('pattern toggle',()=>assert.equal(api.preview({...api.DEFAULT,cues:'icon-text-pattern'}).showPattern,true));
check('150% font factor',()=>assert.equal(api.preview({...api.DEFAULT,textScale:150}).fontScale,1.5));
check('125% font factor',()=>assert.equal(api.preview({...api.DEFAULT,textScale:125}).fontScale,1.25));
check('contrast symmetry',()=>assert.equal(api.contrast('#ffffff','#000000'),api.contrast('#000000','#ffffff')));
check('black-white contrast 21',()=>assert.ok(Math.abs(api.contrast('#000000','#ffffff')-21)<1e-10));
for(const [key,values] of Object.entries(api.ALLOWED))for(const value of values){
  check('valid preference '+key+'='+String(value),()=>{const result=action(api.fresh(),{type:'SET',key,value});assert.equal(result.ok,true);assert.equal(result.state.prefs[key],value)});
}
let s=api.fresh();s=action(s,{type:'SET',key:'textScale',value:125}).state;
check('mutation revision increments',()=>assert.equal(s.revision,1));
check('mutation has undo',()=>assert.equal(s.past.length,1));
check('no unwanted redo',()=>assert.equal(s.future.length,0));
const unchanged=action(s,{type:'SET',key:'textScale',value:125});
check('no-op object unchanged',()=>assert.strictEqual(unchanged.state,s));
check('no-op no revision',()=>assert.equal(unchanged.state.revision,1));
let u=action(s,{type:'UNDO'});
check('undo restores 100%',()=>assert.equal(u.state.prefs.textScale,100));
check('undo creates redo',()=>assert.equal(u.state.future.length,1));
check('undo changes revision',()=>assert.equal(u.state.revision,2));
let redo=action(u.state,{type:'REDO'});
check('redo restores 125%',()=>assert.equal(redo.state.prefs.textScale,125));
check('redo clears future',()=>assert.equal(redo.state.future.length,0));
check('redo leaves original untouched',()=>assert.equal(s.prefs.textScale,125));
check('undo fail empty preserves state',()=>{let x=api.fresh();assert.strictEqual(action(x,{type:'UNDO'}).state,x)});
check('redo fail empty preserves state',()=>{let x=api.fresh();assert.strictEqual(action(x,{type:'REDO'}).state,x)});
let ex=action(s,{type:'EXPORT'});
check('export returns output',()=>assert.equal(typeof ex.output,'string'));
check('export stable schema',()=>assert.equal(JSON.parse(ex.output).schema,api.SCHEMA));
check('export unchanged revision',()=>assert.equal(ex.state.revision,s.revision));
check('export state pinned',()=>assert.equal(ex.state.exported,ex.output));
let edited=action(ex.state,{type:'SET',key:'motion',value:'reduced'});
check('edit invalidates prior export',()=>assert.equal(edited.state.exported,null));
let incoming=action(api.fresh(),{type:'IMPORT',text:api.profile(edited.state.prefs)});
check('import valid prefs',()=>assert.deepEqual(incoming.state.prefs,edited.state.prefs));
check('import creates 1 undo',()=>assert.equal(incoming.state.past.length,1));
check('import equals deterministic export',()=>assert.equal(api.profile(incoming.state.prefs),api.profile(edited.state.prefs)));
check('malformed-profile rejection',()=>assert.equal(api.parseProfile('{invalid').ok,false));
const badSamples=[
 ['empty',''],['malformed','{'],['array','[]'],['null','null'],['bad-schema',JSON.stringify({schema:'bad',version:1,prefs:api.DEFAULT})],
 ['future-version',JSON.stringify({schema:api.SCHEMA,version:2,prefs:api.DEFAULT})],
 ['missing-version',JSON.stringify({schema:api.SCHEMA,prefs:api.DEFAULT})],
 ['extra-envelope',JSON.stringify({schema:api.SCHEMA,version:1,prefs:api.DEFAULT,hack:true})],
 ['extra-pref',JSON.stringify({schema:api.SCHEMA,version:1,prefs:{...api.DEFAULT,debug:true}})],
 ['missing-pref',JSON.stringify({schema:api.SCHEMA,version:1,prefs:{contrast:'high'}})],
 ['bad-contrast',JSON.stringify({schema:api.SCHEMA,version:1,prefs:{...api.DEFAULT,contrast:'ultra'}})],
 ['bad-motion',JSON.stringify({schema:api.SCHEMA,version:1,prefs:{...api.DEFAULT,motion:'flash'}})],
 ['string-scale',JSON.stringify({schema:api.SCHEMA,version:1,prefs:{...api.DEFAULT,textScale:'125'}})],
 ['fractional-scale',JSON.stringify({schema:api.SCHEMA,version:1,prefs:{...api.DEFAULT,textScale:126}})],
 ['numeric-captions',JSON.stringify({schema:api.SCHEMA,version:1,prefs:{...api.DEFAULT,captions:1}})],
 ['object-cues',JSON.stringify({schema:api.SCHEMA,version:1,prefs:{...api.DEFAULT,cues:{}}})],
 ['proto-injection',JSON.stringify({schema:api.SCHEMA,version:1,prefs:{...api.DEFAULT,__proto__:null}}).replace('"captions":true','"captions":true,"__proto__":{"x":1}')],
 ['oversized',' '.repeat(4097)],['non-string',42]
];
for(const [label,bad] of badSamples){check('atomic reject '+label,()=>{const before=action(api.fresh(),{type:'SET',key:'textScale',value:150}).state;const original=JSON.stringify(before);const res=action(before,{type:'IMPORT',text:bad});assert.equal(res.ok,false);assert.strictEqual(res.state,before);assert.equal(JSON.stringify(before),original)})}
for(const [label,event] of [
 ['unknown',{type:'EVAL'}],['missing-type',{}],['null',null],['set key', {type:'SET',key:'__proto__',value:'high'}],
 ['set value',{type:'SET',key:'contrast',value:'magical'}],['set extra',{type:'SET',key:'contrast',value:'high',extra:1}],
 ['no-event-set',{type:'SET'}],['reset extra',{type:'RESET',source:'bogus'}],['undo extra',{type:'UNDO',source:'bogus'}],
 ['redo extra',{type:'REDO',source:'bogus'}],['export extra',{type:'EXPORT',source:'bogus'}],['import missing',{type:'IMPORT'}]
]){check('reject action '+label,()=>{const before=api.fresh();const r=action(before,event);assert.equal(r.ok,false);assert.strictEqual(r.state,before)})}
check('invalid state fails closed',()=>assert.throws(()=>action({prefs:{}},{type:'RESET'}),TypeError));
check('invalid prototype state fails closed',()=>assert.throws(()=>action(Object.create(null),{type:'RESET'}),TypeError));
check('invalid preview fails closed',()=>assert.throws(()=>api.preview({contrast:'high'}),TypeError));
check('invalid export fails closed',()=>assert.throws(()=>api.profile({contrast:'high'}),Error));
let hist=api.fresh();for(let i=0;i<90;i++)hist=action(hist,{type:'SET',key:'textScale',value:i%2?100:125}).state;
check('bounded history 24',()=>assert.equal(hist.past.length,24));
check('history still valid',()=>assert.equal(api.validState(hist),true));
check('history copies do not alias state',()=>assert.notStrictEqual(hist.prefs,hist.past.at(-1)));
check('revision 90',()=>assert.equal(hist.revision,90));
let reset=action(hist,{type:'RESET'});
check('reset defaults',()=>assert.deepEqual(reset.state.prefs,api.DEFAULT));
check('reset keeps past',()=>assert.equal(reset.state.past.length,24));
let exported=action(api.fresh(),{type:'EXPORT'}).state;
check('invalid import preserves export',()=>assert.equal(action(exported,{type:'IMPORT',text:'invalid'}).state.exported,exported.exported));
check('valid neutral import no mutation',()=>{const z=api.fresh();const r=action(z,{type:'IMPORT',text:api.profile(z.prefs)});assert.strictEqual(r.state,z)});
const out={package:'B01-12-A11Y-PREFS-001/R1',environment:{node:process.version},total:cases.length,pass:cases.filter(c=>c.pass).length,fail:cases.filter(c=>!c.pass).map(c=>c.case),cases};
fs.writeFileSync(path.join(ROOT,'node-results.json'),JSON.stringify(out,null,2)+'\n');console.log(JSON.stringify({total:out.total,pass:out.pass,fail:out.fail},null,2));process.exit(out.fail.length?1:0);
