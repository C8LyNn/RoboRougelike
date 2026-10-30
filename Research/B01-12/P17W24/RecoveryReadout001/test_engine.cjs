'use strict';
const fs=require('fs'),crypto=require('crypto'),path=require('path'),assert=require('assert/strict');
const root=__dirname,m=JSON.parse(fs.readFileSync(path.join(root,'manifest.json'),'utf8'));
for(const [fn,v] of Object.entries(m.files)){const b=fs.readFileSync(path.join(root,fn));if(b.length!==v.bytes||crypto.createHash('sha256').update(b).digest('hex')!==v.sha256){console.error('FAIL_CLOSED source mismatch: '+fn);process.exit(2)}}
const {project}=require('./readout_engine.js'),{list}=require('./fixtures.js');const results=[];
function check(label,fn){try{fn();results.push({case:label,status:'PASS'});}catch(e){results.push({case:label,status:'FAIL',error:String(e)});console.error('FAIL',label,e.message)}}
const cp=x=>JSON.parse(JSON.stringify(x)), examples=list().map(({label,...x})=>x),ok=x=>assert.equal(project(x).ok,true),bad=(x,code)=>{const a=project(x);assert.equal(a.ok,false);if(code)assert.equal(a.code,code)};
check('five scenarios',()=>assert.equal(examples.length,5));
const routes=['NORMAL','LOANER_AVAILABLE','CORE_DESTROYED','LOANER_EXHAUSTED','NO_CHALLENGE'];
examples.forEach((e,i)=>{check('scenario '+i+' accepted',()=>ok(e));check('scenario '+i+' route',()=>assert.equal(project(e).route,routes[i]));check('scenario '+i+' stable',()=>assert.deepEqual(project(cp(e)),project(e)));check('scenario '+i+' no source mutation',()=>{const before=JSON.stringify(e);project(e);assert.equal(JSON.stringify(e),before)});check('scenario '+i+' frozen result',()=>assert.ok(Object.isFrozen(project(e))));});
for(const field of ['run','generation','coreAlive','canDeploy','hasNextChallenge','loanerUsed','loanerEligible','victoryId','freeAvailable','parts']){
 check('missing '+field,()=>{const x=cp(examples[1]);delete x[field];bad(x)});
 check('extra '+field,()=>{const x=cp(examples[1]);x.invalid='extra';bad(x)});
}
for(const [name,mutate,code] of [
 ['foreign run',x=>x.run='../bad','INVALID_SNAPSHOT'],
 ['invalid generation',x=>x.generation=0,'INVALID_SNAPSHOT'],
 ['float gen',x=>x.generation=1.2,'INVALID_SNAPSHOT'],
 ['invalid flags',x=>x.coreAlive='true','INVALID_FLAGS'],
 ['invalid win',x=>x.victoryId='win space','INVALID_VICTORY'],
 ['dead core with loaner',x=>{x.coreAlive=false;x.loanerEligible=true},'CORE_CONTRADICTION'],
 ['dead core deploy',x=>{x.coreAlive=false;x.canDeploy=true},'CORE_CONTRADICTION'],
 ['loaner while normal',x=>{x.canDeploy=true;x.loanerEligible=true},'LOANER_CONTRADICTION'],
 ['loaner already used',x=>{x.loanerUsed=true;x.loanerEligible=true},'LOANER_CONTRADICTION'],
 ['loaner without challenge',x=>{x.hasNextChallenge=false;x.loanerEligible=true},'LOANER_CONTRADICTION'],
 ['bad list',x=>x.parts={},'INVALID_PARTS'],
 ['duplicate id',x=>x.parts.push(cp(x.parts[0])),'INVALID_PART'],
 ['bad part id',x=>x.parts[0].id='../escape','INVALID_PART'],
 ['wrong status',x=>x.parts[0].status='hacked','INVALID_PART'],
 ['negative quote',x=>x.parts[1].rebuildQuote=-1,'INVALID_PART'],
 ['quote not cheaper',x=>x.parts[1].rebuildQuote=100,'QUOTE_CONTRADICTION'],
 ['same win repair impossible',x=>x.parts[1].readyAfter=x.parts[1].destroyedAt,'CHRONOLOGY_CONTRADICTION'],
 ['extra part field',x=>x.parts[1].script='<script>','INVALID_PART'],
 ['invalid part name',x=>x.parts[1].name='<'.repeat(80),'INVALID_PART'],
 ['too many parts',x=>x.parts=Array.from({length:33},(_,i)=>({...cp(x.parts[0]),id:'m'+i})),'INVALID_PARTS']]){
 check(name,()=>{const x=cp(examples[1]);mutate(x);bad(x,code)});
}
check('failed readout does not modify object',()=>{const x=cp(examples[1]);x.parts[1].rebuildQuote=999;const before=JSON.stringify(x);project(x);assert.equal(JSON.stringify(x),before)});
check('id not duplicated in projection',()=>assert.deepEqual(project(examples[1]).parts.map(x=>x.id),['core','drive','radar']));
check('projection no ECON write operations',()=>assert.deepEqual(Object.keys(project(examples[1])).sort(),['explanation','freeLabel','generation','ok','parts','route','run','victoryLabel'].sort()));
check('free label is read only',()=>assert.match(project(examples[1]).freeLabel,/dostępna/));
check('policy price is not computed by UI',()=>{const x=cp(examples[1]);x.parts[1].rebuildQuote=1;assert.equal(project(x).parts[1].quote,1)});
check('different fixture instances',()=>{const a=list(),b=list();a[0].parts[0].name='Zmieniona';assert.notEqual(a[0].parts[0].name,b[0].parts[0].name)});
check('0 price quote can be zero',()=>{const x=cp(examples[1]);x.parts[1].purchasePrice=1;x.parts[1].rebuildQuote=0;ok(x)});
check('pending completion cannot be marked same event',()=>{const x=cp(examples[1]);x.parts[1].readyAfter=x.parts[1].destroyedAt;bad(x,'CHRONOLOGY_CONTRADICTION')});
check('empty parts valid readout',()=>{const x=cp(examples[0]);x.parts=[];assert.equal(project(x).parts.length,0)});
check('monotonic projection',()=>{const x=cp(examples[1]);x.generation++;assert.equal(project(x).generation,10)});
check('malicious markup remains text in projection',()=>{const x=cp(examples[1]);x.parts[0].name='<img onerror=alert(1)>';assert.equal(project(x).parts[0].name,x.parts[0].name)});
const pass=results.filter(x=>x.status==='PASS').length;fs.writeFileSync(path.join(root,'node-results.json'),JSON.stringify({pass,fail:results.length-pass,tests:results},null,2)+'\n');console.log('NODE',pass+'/'+results.length,pass===results.length?'PASS':'FAIL');if(pass!==results.length)process.exit(1);
