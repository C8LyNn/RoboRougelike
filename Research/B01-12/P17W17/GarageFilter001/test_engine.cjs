'use strict';
const fs=require('fs'),path=require('path'),crypto=require('crypto'),assert=require('node:assert/strict');
const root=__dirname;
function integrity(){const m=JSON.parse(fs.readFileSync(path.join(root,'manifest.json'),'utf8'));for(const [name,expect] of Object.entries(m.sources)){const b=fs.readFileSync(path.join(root,name));if(b.length!==expect.bytes||crypto.createHash('sha256').update(b).digest('hex')!==expect.sha256)throw Error('FAIL_CLOSED '+name);}return m;}
try{integrity();}catch(e){console.error(e.message);process.exit(2);}
const F=require('./filter_engine.js'),CAT=require('./catalog.js');
const checks=[];function test(name,run){try{run();checks.push({name,pass:true});}catch(e){checks.push({name,pass:false,error:e.message});}}
const q=s=>F.search(CAT,s);const ids=r=>r.items.map(x=>x.id);
test('all 12 demonstrator records',()=>assert.equal(q('').count,12));
test('catalog immutability source',()=>{assert.ok(Object.isFrozen(CAT));assert.ok(Object.isFrozen(CAT[0]));});
test('query immutable catalog',()=>{const b=JSON.stringify(CAT);q('type:weapon');assert.equal(JSON.stringify(CAT),b);});
test('pl diacritic folding',()=>assert.deepEqual(ids(q('gasienica')),['wheel-03']));
test('quoted phrase filter',()=>assert.deepEqual(ids(q('"lekki karabin"')),['gun-02']));
test('word AND matching',()=>assert.deepEqual(ids(q('pancerz stalowy')),['armor-01']));
test('type weapon',()=>assert.equal(q('type:weapon').count,3));
test('type drive',()=>assert.equal(q('type:drive').count,4));
test('type utility',()=>assert.deepEqual(ids(q('type:utility')),['util-01']));
test('tag armor',()=>assert.equal(q('tag:armor').count,3));
test('tag repair',()=>assert.deepEqual(ids(q('tag:repair')),['util-01']));
test('owned yes',()=>assert.equal(q('owned:yes').count,6));
test('owned no',()=>assert.equal(q('owned:no').count,6));
test('fit light includes both',()=>assert.equal(q('fit:light').count,9));
test('fit heavy includes both',()=>assert.equal(q('fit:heavy').count,9));
test('fit both exactly',()=>assert.equal(q('fit:both').count,6));
test('mass <=20',()=>assert.equal(q('mass<=20').count,9));
test('cost >=200',()=>assert.equal(q('cost>=200').count,2));
test('cost =35',()=>assert.deepEqual(ids(q('cost=35')),['wheel-02']));
test('energy>=60',()=>assert.deepEqual(ids(q('energy>=60')),['power-01']));
test('slots=1',()=>assert.equal(q('slots=1').count,4));
test('mixed filter',()=>assert.deepEqual(ids(q('type:weapon mass<=20 owned:yes')),['gun-02']));
test('sort by cost',()=>assert.deepEqual(ids(q('type:weapon sort:cost')),['gun-02','gun-01','gun-03']));
test('sort by descending cost',()=>assert.deepEqual(ids(q('type:weapon sort:-cost')),['gun-03','gun-01','gun-02']));
test('sort by mass',()=>assert.deepEqual(ids(q('type:weapon sort:mass')),['gun-02','gun-03','gun-01']));
test('sort by descending energy',()=>assert.deepEqual(ids(q('type:weapon sort:-energy')),['gun-03','gun-01','gun-02']));
test('facets sum to count',()=>{const a=q('owned:yes');assert.equal(Object.values(a.facets).reduce((v,w)=>v+w,0),a.count);});
test('export id contract',()=>{const d=F.exportSelection(q('type:weapon'));assert.equal(d.ok,true);assert.deepEqual(JSON.parse(d.json),{schema:1,kind:'b01-12-filter-selection',ids:ids(q('type:weapon'))});});
test('export invalid result refuses',()=>assert.equal(F.exportSelection(q('tag:missing')).ok,false));
test('catalog duplicate IDs fail',()=>assert.equal(F.search([...CAT,CAT[0]],'').code,F.ERR.CATALOG));
test('catalog foreign type fail',()=>assert.equal(F.search([{...CAT[0],type:'__proto__'}],'').ok,false));
test('catalog malicious name fail',()=>assert.equal(F.search([{...CAT[0],name:'<script>'}],'').ok,false));
test('catalog negative cost fail',()=>assert.equal(F.search([{...CAT[0],cost:-1}],'').ok,false));
test('catalog invalid tags fail',()=>assert.equal(F.search([{...CAT[0],tags:['bad']}],'').ok,false));
test('catalog duplicate tag fail',()=>assert.equal(F.search([{...CAT[0],tags:['speed','speed']}],'').ok,false));
test('catalog zero records fail',()=>assert.equal(F.search([],'').ok,false));
for(const [label,query,code] of [
 ['unknown field','model:wheel',F.ERR.TOKEN],['invalid type','type:bogus',F.ERR.VALUE],['invalid tag','tag:bogus',F.ERR.VALUE],['invalid fit','fit:bogus',F.ERR.VALUE],['invalid owner','owned:maybe',F.ERR.VALUE],['invalid sort','sort:score',F.ERR.VALUE],['duplicate type','type:drive type:weapon',F.ERR.DUPLICATE],['duplicate sorting','sort:mass sort:cost',F.ERR.DUPLICATE],['duplicate numeric bound','mass<=20 mass<=10',F.ERR.DUPLICATE],['negative mass','mass>=-1',F.ERR.TOKEN],['decimal mass','mass<=2.5',F.ERR.TOKEN],['overflow mass','mass<=999999',F.ERR.VALUE],['unclosed quote','"unfinished',F.ERR.QUERY],['script markup','<img>',F.ERR.QUERY],['tab control','hi\tthere',F.ERR.QUERY],['slash','abc\\def',F.ERR.QUERY],['empty assignment','tag:',F.ERR.TOKEN],['illegal comparison','mass<3',F.ERR.TOKEN],['SQL-ish','hello;world',F.ERR.TOKEN],['non-string',null,F.ERR.QUERY],['many chars','a'.repeat(161),F.ERR.QUERY],['too many tokens','one two three four five six seven eight nine ten eleven twelve thirteen',F.ERR.QUERY]]){
 test(label,()=>{let r=q(query);assert.equal(r.ok,false);assert.equal(r.code,code);assert.deepEqual(r.items,[]);});
}
test('deterministic result replay',()=>assert.deepEqual(q('type:drive sort:-mass'),q('type:drive sort:-mass')));
test('no matches valid',()=>{const r=q('mass>=99999');assert.equal(r.ok,true);assert.equal(r.count,0);});
test('source not changed by result mutation',()=>{const r=q('type:weapon');r.items[0].tags.push('fake');assert.equal(CAT.find(x=>x.id===r.items[0].id).tags.includes('fake'),false);});
test('sort stable for equal metric',()=>{const a=q('sort:energy').items.filter(x=>x.energy===0).map(x=>x.id);assert.deepEqual(a,[...a].sort());});
test('bounded token count',()=>assert.equal(q('a b c d e f g h i j k l m').ok,false));
const pass=checks.filter(x=>x.pass).length,receipt={package:'B01-12-GARAGE-FILTER-001/R1',runner:'node',pass,total:checks.length,exit:pass===checks.length?0:1,checks};
fs.writeFileSync(path.join(root,'node-results.json'),JSON.stringify(receipt,null,2)+'\n');console.log('NODE '+pass+'/'+checks.length+(pass===checks.length?' PASS':' FAIL'));for(const c of checks.filter(c=>!c.pass))console.error(c.name+': '+c.error);process.exit(pass===checks.length?0:1);
