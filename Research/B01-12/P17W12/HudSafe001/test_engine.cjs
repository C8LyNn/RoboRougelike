'use strict';
const fs=require('node:fs');const path=require('node:path');const crypto=require('node:crypto');const assert=require('node:assert/strict');
const dir=__dirname;let manifest;
function verify(){manifest=JSON.parse(fs.readFileSync(path.join(dir,'manifest.json'),'utf8'));for(const [name,pin] of Object.entries(manifest.pins)){const data=fs.readFileSync(path.join(dir,name));if(data.length!==pin.bytes||crypto.createHash('sha256').update(data).digest('hex')!==pin.sha256){console.error('FAIL_CLOSED: '+name);process.exit(2);}}}
verify();const {solve,overlap,snapshot}=require('./layout_engine.js');let passes=0;let errors=[];
function check(label,fn){try{fn();passes++;}catch(e){errors.push({label,error:e.message});}}
const insets={left:12,right:12,top:12,bottom:12};const mk=(id,w=100,h=48,priority=5,anchor='tl')=>({id,w,h,priority,anchor});
const setup=(w=1280,h=720,widgets=[mk('HP')],exclusions=[])=>({width:w,height:h,insets:{...insets},exclusions,widgets});
for(const w of [320,768,1280,1920]){let cfg=setup(w,720,[mk('A',96,50,9,'tl'),mk('B',96,50,8,'tl'),mk('C',96,50,7,'tr')]);let x=solve(cfg);
check('valid '+w,()=>assert.equal(x.ok,true));check('complete '+w,()=>assert.equal(x.placements.length,3));check('nonoverlap '+w,()=>assert.equal(x.placements.some((p,i)=>x.placements.slice(i+1).some(q=>overlap(p,q))),false));check('inArea '+w,()=>assert(x.placements.every(p=>p.x>=x.safeArea.x&&p.y>=x.safeArea.y&&p.x+p.w<=x.safeArea.x+x.safeArea.w&&p.y+p.h<=x.safeArea.y+x.safeArea.h)));check('stable '+w,()=>assert.equal(snapshot(cfg),snapshot(cfg)));}
for(const [label,field,bad] of [['zero width','width',0],['float width','width',320.5],['huge height','height',5000],['string width','width','320'],['null width','width',null]]){check(label,()=>assert.equal(solve({...setup(),[field]:bad}).ok,false));}
for(const [label,change] of [['negative inset',{left:-1}],['float inset',{right:2.5}],['huge inset',{top:999}],['missing inset',null]]){check(label,()=>{const c=setup();c.insets=change===null?null:{...insets,...change};assert.equal(solve(c).ok,false);});}
for(const [label,w] of [['bad id',mk('<script>')],['duplicate',mk('x')],['bad anchor',mk('x',100,50,9,'BAD')],['negative priority',mk('x',100,50,-1)],['zero dimension',mk('x',0,50)],['too wide',mk('x',501,50)],['fractional',mk('x',100.1,50)]])check(label,()=>{const c=setup();c.widgets=label==='duplicate'?[mk('x'),mk('x')]:[w];assert.equal(solve(c).ok,false);});
for(const [label,ex] of [['negative rect',{x:-1,y:0,w:10,h:10}],['offscreen',{x:1275,y:0,w:20,h:20}],['zero',{x:0,y:0,w:0,h:20}],['fraction',{x:0.5,y:0,w:10,h:10}]])check(label,()=>assert.equal(solve(setup(1280,720,[mk('A')],[ex])).ok,false));
check('blocked all',()=>{const c=setup(320,720,[mk('A',100,40)],[{x:0,y:0,w:320,h:720}]);const x=solve(c);assert.equal(x.ok,false);assert.deepEqual(x.placements,[]);});
check('oversized fail no partial',()=>{const c=setup(320,720,[mk('A'),mk('B',500,300)]);const x=solve(c);assert.equal(x.ok,false);assert.equal(x.placements.length,0);});
check('exclude fit',()=>{const c=setup(320,720,[mk('A',100,48)],[{x:0,y:0,w:150,h:100}]);const x=solve(c);assert.equal(x.ok,true);assert.equal(overlap(x.placements[0],c.exclusions[0]),false);});
check('priority sorted',()=>{const x=solve(setup(1280,720,[mk('LOW',100,40,1),mk('HIGH',100,40,9)]));assert.deepEqual(x.placements.map(p=>p.id),['HIGH','LOW']);});
check('id tie stable',()=>{const x=solve(setup(1280,720,[mk('Z'),mk('A')]));assert.deepEqual(x.placements.map(p=>p.id),['A','Z']);});
check('nonmutating',()=>{const c=setup(1280,720,[mk('A'),mk('B')]);const s=JSON.stringify(c);solve(c);assert.equal(JSON.stringify(c),s);});
check('oversubscription',()=>{const c=setup(1920,1080,Array.from({length:12},(_,i)=>mk('B'+i,500,300,9)));const r=solve(c);assert.equal(r.ok,false);assert.deepEqual(r.placements,[]);});
check('unknown extra props ignored safely',()=>{const c=setup();c.widgets[0].label='<img src=x onerror=alert(1)>';assert.equal(solve(c).ok,true);});
check('empty widgets',()=>assert.equal(solve(setup(1280,720,[])).ok,false));
check('too many widgets',()=>assert.equal(solve(setup(1280,720,Array.from({length:13},(_,i)=>mk('A'+i)))).ok,false));
check('too many exclusion rects',()=>assert.equal(solve(setup(1280,720,[mk('A')],Array.from({length:9},(_,i)=>({x:i*10,y:0,w:10,h:10})))).ok,false));
check('invalid input null',()=>assert.equal(solve(null).ok,false));
check('invalid array',()=>assert.equal(solve([]).ok,false));
check('overlap edge adjacency',()=>assert.equal(overlap({x:0,y:0,w:10,h:10},{x:10,y:0,w:10,h:10}),false));
check('overlap true',()=>assert.equal(overlap({x:0,y:0,w:10,h:10},{x:9,y:9,w:10,h:10}),true));
const result={package:'B01-12-HUDSAFE-001/R1',kind:'node',passed:passes,failed:errors.length,errors,exit:errors.length?1:0};fs.writeFileSync(path.join(dir,'node-results.json'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));if(errors.length)process.exit(1);
