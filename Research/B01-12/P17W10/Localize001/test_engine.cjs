'use strict';
const fs=require('fs'),path=require('path'),crypto=require('crypto');const assert=require('assert/strict');
const ROOT=__dirname, manifest=JSON.parse(fs.readFileSync(path.join(ROOT,'manifest.json'),'utf8'));
for(const [name,info] of Object.entries(manifest.files)){
 const data=fs.readFileSync(path.join(ROOT,name));const hash=crypto.createHash('sha256').update(data).digest('hex');
 if(hash!==info.sha256||data.length!==info.bytes){console.error('FAIL_CLOSED source mismatch',name);process.exit(2)}
}
const mod=require('./localize_engine.js'), catalog=require('./catalog.js');let checks=[];
function test(label,fn){try{fn();checks.push({label,pass:true})}catch(e){checks.push({label,pass:false,error:String(e)});console.error('FAIL',label,e.message)}}
function clone(){return JSON.parse(JSON.stringify(catalog))}
const l=mod.make(catalog);
test('catalog-key-parity-30',()=>assert.equal(l.keys.length,30));
test('available-locales',()=>assert.deepEqual(mod.LOCALES,['pl','en','pseudo']));
for(const [locale,expected] of [['pl','Garaż'],['en','Garage'],['pseudo',null]]){
 test('translate-nav-'+locale,()=>{let t=l.translate(locale,'nav.garage');if(expected)assert.equal(t,expected);else assert.ok(t.startsWith('⟦')&&t.endsWith('⟧'))});
}
const cases=[[0,'części','parts'],[1,'część','part'],[2,'części','parts'],[3,'części','parts'],[4,'części','parts'],[5,'części','parts'],[11,'części','parts'],[12,'części','parts'],[14,'części','parts'],[21,'części','parts'],[22,'części','parts'],[24,'części','parts'],[25,'części','parts'],[101,'części','parts'],[102,'części','parts'],[112,'części','parts']];
for(const [count,pl,en] of cases){
 test('plural-parts-pl-'+count,()=>assert.equal(l.translate('pl','garage.parts',{count}),`${count} ${pl}`));
 test('plural-parts-en-'+count,()=>assert.equal(l.translate('en','garage.parts',{count}),`${count} ${en}`));
}
const ammo=[[1,'pocisk'],[2,'pociski'],[4,'pociski'],[5,'pocisków'],[12,'pocisków'],[22,'pociski'],[111,'pocisków']];
for(const [count,expected] of ammo)test('plural-ammo-'+count,()=>assert.equal(l.translate('pl','combat.ammo',{count}),`${count} ${expected}`));
for(const [k,v] of [['garage.scrap',40],['combat.weapon','Railgun'],['combat.reload','R'],['garage.mount','Wheel'],['audit.format',28]]){
 const p=k==='garage.scrap'?{scrap:v}:k==='combat.weapon'?{weapon:v}:k==='combat.reload'?{key:v}:k==='garage.mount'?{module:v}:{count:v};
 test('interpolate-'+k,()=>assert.ok(l.translate('en',k,p).includes(String(v))));
}
for(const [locale,key,params] of [['pl','garage.parts',{}],['en','garage.scrap',{}],['pseudo','combat.reload',{}],['pl','nav.current',{}]]){
 test('missing-argument-'+key,()=>assert.throws(()=>l.translate(locale,key,params),/LOCALIZE_INVALID/));
}
for(const count of [-2,1.5,NaN,Infinity,1000001,Number.MAX_SAFE_INTEGER])test('reject-count-'+count,()=>assert.throws(()=>l.translate('pl','garage.parts',{count}),/LOCALIZE_INVALID/));
for(const locale of ['de','PL','',null])test('reject-locale-'+locale,()=>assert.throws(()=>l.translate(locale,'nav.garage'),/LOCALIZE_INVALID/));
for(const key of ['__proto__','constructor','x','nav.__proto__'])test('reject-key-'+key,()=>assert.throws(()=>l.translate('pl',key),/LOCALIZE_INVALID/));
for(const param of ['<img src=x onerror=alert(1)>','<script>alert(1)</script>','\u0000','\n','X'.repeat(90)])test('reject-unsafe-param-'+JSON.stringify(param).slice(0,15),()=>assert.throws(()=>l.translate('pl','garage.mount',{module:param}),/LOCALIZE_INVALID/));
test('pseudo-preserves-value',()=>assert.ok(l.translate('pseudo','garage.scrap',{scrap:47}).includes('47')));
test('pseudo-wraps-and-expands',()=>{let s=l.translate('pseudo','garage.warning');assert.ok(s.startsWith('⟦')&&s.length>catalog.en['garage.warning'].length)});
test('localized-wheel-pl',()=>assert.equal(l.translate('pl','part.wheel'),'Koło'));
test('localized-weapon-en',()=>assert.equal(l.translate('en','weapon.railgun'),'Railgun'));
test('source-catalog-immutable',()=>{const before=JSON.stringify(catalog);l.translate('pl','garage.parts',{count:23});assert.equal(JSON.stringify(catalog),before)});
for(const [label,modify] of [
 ['missing-pl-key',c=>{delete c.pl['nav.garage']}],
 ['extra-pl-key',c=>{c.pl['random.key']='foo'}],
 ['invalid-version',c=>{c.version=2}],
 ['unknown-root',c=>{c.extra='evil'}],
 ['unsafe-html',c=>{c.en['nav.garage']='<img>'}],
 ['unescaped-braces',c=>{c.en['nav.garage']='bad {broken'}],
 ['unknown-placeholder',c=>{c.en['nav.garage']='bad {password}'}],
 ['mismatched-placeholder',c=>{c.en['garage.scrap']='scrap {weapon}'}],
 ['invalid-plural-entry',c=>{delete c.pl['combat.ammo'].few}],
 ['missing-plural-count',c=>{for(const f of ['one','few','many','other'])c.en['combat.ammo'][f]='NONE'}],
 ['oversized-label',c=>{c.en['app.title']='X'.repeat(405)}],
 ['bad-control-character',c=>{c.en['app.title']='bad\u0000value'}],
 ['different-entry-kind',c=>{c.en['combat.ammo']='one'}],
 ['unexpected-plural-slot',c=>{c.en['combat.ammo'].one='{count} {weapon}'}],
 ['empty-string',c=>{c.pl['nav.combat']=''}]
])test('reject-catalog-'+label,()=>{const c=clone();modify(c);assert.throws(()=>mod.make(c),/LOCALIZE_INVALID/)});
let receipt={package:'B01-12-LOCALIZE-001/R1',total:checks.length,pass:checks.filter(x=>x.pass).length,fail:checks.filter(x=>!x.pass),node:process.version,checks};
fs.writeFileSync(path.join(ROOT,'node-results.json'),JSON.stringify(receipt,null,2)+'\n');console.log(JSON.stringify({total:receipt.total,pass:receipt.pass,fail:receipt.fail.length,node:receipt.node}));process.exit(receipt.fail.length?1:0);
