'use strict';
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const dir=__dirname;
function gate(){let m;try{m=JSON.parse(fs.readFileSync(path.join(dir,'manifest.json'),'utf8'));}catch(e){process.stderr.write('FAIL_CLOSED manifest unreadable\n');process.exit(2);}if(m.schema!==1||typeof m.files!=='object'||Object.keys(m.files).length!==6){process.stderr.write('FAIL_CLOSED manifest schema\n');process.exit(2);}for(const [name,expect] of Object.entries(m.files)){const p=path.join(dir,name);if(!['impact_engine.js','fixture.js','ui.js','demo.html','test_engine.cjs','test_browser.py'].includes(name)||typeof expect.sha256!=='string'||!Number.isInteger(expect.bytes)){process.stderr.write('FAIL_CLOSED manifest contents\n');process.exit(2);}let data;try{data=fs.readFileSync(p);}catch(e){process.stderr.write('FAIL_CLOSED missing '+name+'\n');process.exit(2);}if(data.length!==expect.bytes||crypto.createHash('sha256').update(data).digest('hex')!==expect.sha256){process.stderr.write('FAIL_CLOSED source mismatch: '+name+'\n');process.exit(2);}}}
gate();
const api=require('./impact_engine.js'),base=require('./fixture.js');
let passed=0,failed=0;const cases=[];
function check(name,fn){try{fn();passed++;cases.push({case:name,result:'PASS'});}catch(e){failed++;cases.push({case:name,result:'FAIL',message:e.message});console.error('FAIL',name,e.message);}}
const clone=o=>JSON.parse(JSON.stringify(o));
check('baseline graph valid',()=>assert.equal(api.validate(base),null));
check('sample exact node count',()=>assert.equal(base.nodes.length,15));
check('sample edge count',()=>assert.equal(base.edges.length,17));
check('core protected',()=>assert.deepEqual(api.preview(base,'core').code,'CORE_PROTECTED'));
check('core protected no partial',()=>assert.equal(api.preview(base,'core').components.length,0));
check('unknown target rejected',()=>assert.equal(api.preview(base,'other').code,'TARGET_UNKNOWN'));
check('invalid target input rejected',()=>assert.equal(api.preview(base,{}).code,'TARGET_UNKNOWN'));
check('redundant spine L path',()=>assert.equal(api.preview(base,'spine_l').detached.length,1));
check('redundant spine R path',()=>assert.equal(api.preview(base,'spine_r').detached.length,0));
check('gun needs crossbar',()=>assert.deepEqual(api.preview(base,'crossbar').detached,['gun']));
check('core connected baseline',()=>assert.deepEqual(api.preview(base,'wheel_rr').baselineDisconnected,[]));
check('wheel is a leaf',()=>assert.deepEqual(api.preview(base,'wheel_rr').detached,[]));
check('cell is a leaf',()=>assert.deepEqual(api.preview(base,'cell').detached,[]));
check('sensor is a leaf',()=>assert.deepEqual(api.preview(base,'sensor').detached,[]));
check('front L isolates wheel FL',()=>assert.deepEqual(api.preview(base,'front_l').detached,['wheel_fl']));
check('rear L isolates wheel RL',()=>assert.deepEqual(api.preview(base,'rear_l').detached,['wheel_rl']));
check('successful removal counts sum to original minus one',()=>{for(const n of base.nodes){if(n.id==='core')continue;const p=api.preview(base,n.id);assert.equal(p.retained.length+p.detached.length+p.baselineDisconnected.length,base.nodes.length-1);}});
check('no detached node also retained',()=>{for(const n of base.nodes){if(n.id==='core')continue;const p=api.preview(base,n.id);assert.deepEqual(p.detached.filter(x=>p.retained.includes(x)),[]);}});
check('scan rows N minus core',()=>assert.equal(api.scan(base).rows.length,14));
check('scan ranking highest positive',()=>assert.equal(api.scan(base).rows[0].detachedCount,1));
check('scan repeat byte deterministic',()=>assert.equal(JSON.stringify(api.scan(base)),JSON.stringify(api.scan(clone(base)))));
check('preview repeat deterministic',()=>assert.equal(JSON.stringify(api.preview(base,'spine_l')),JSON.stringify(api.preview(clone(base),'spine_l'))));
check('no data mutation',()=>{const before=JSON.stringify(base);api.preview(base,'spine_l');api.scan(base);assert.equal(JSON.stringify(base),before);});
check('inputs frozen do not throw',()=>{const g=clone(base);Object.freeze(g);Object.freeze(g.nodes);Object.freeze(g.edges);assert.equal(api.preview(g,'gun').ok,true);});
const invalids=[
 ['graph null',null,'GRAPH_SCHEMA'],['graph array',[],'GRAPH_SCHEMA'],['graph string','boom','GRAPH_SCHEMA'],
 ['graph wrong version',{...clone(base),version:2},'GRAPH_SCHEMA'],['graph missing nodes',{version:1,coreId:'core',edges:[]},'GRAPH_SCHEMA'],
 ['graph extra field',{...clone(base),inject:'x'},'GRAPH_SCHEMA'],['graph no nodes',{...clone(base),nodes:[]},'GRAPH_SCHEMA'],
 ['graph no core',{...clone(base),coreId:'missing'},'CORE_MISSING'],['graph core invalid',{...clone(base),coreId:'!'},'CORE_ID'],
 ['duplicate id',(()=>{const g=clone(base);g.nodes[1].id='core';return g})(),'NODE_INVALID'],
 ['node extra attr',(()=>{const g=clone(base);g.nodes[1].unknown=1;return g})(),'NODE_INVALID'],
 ['node bad kind',(()=>{const g=clone(base);g.nodes[1].kind='evil';return g})(),'NODE_INVALID'],
 ['node invalid x',(()=>{const g=clone(base);g.nodes[1].x=Infinity;return g})(),'NODE_INVALID'],
 ['node negative x',(()=>{const g=clone(base);g.nodes[1].x=-1;return g})(),'NODE_INVALID'],
 ['node fractional coordinate',(()=>{const g=clone(base);g.nodes[1].y=5.5;return g})(),'NODE_INVALID'],
 ['node label null',(()=>{const g=clone(base);g.nodes[1].label=null;return g})(),'NODE_INVALID'],
 ['node id injection',(()=>{const g=clone(base);g.nodes[1].id='<svg>';return g})(),'NODE_INVALID'],
 ['node id too long',(()=>{const g=clone(base);g.nodes[1].id='a'.repeat(30);return g})(),'NODE_INVALID'],
 ['extra core',(()=>{const g=clone(base);g.nodes[1].kind='core';return g})(),'MULTIPLE_CORES'],
 ['core kind changed',(()=>{const g=clone(base);g.nodes[0].kind='wheel';return g})(),'CORE_MISSING'],
 ['edge missing node',(()=>{const g=clone(base);g.edges[0]=['core','ghost'];return g})(),'EDGE_INVALID'],
 ['edge self-loop',(()=>{const g=clone(base);g.edges[0]=['core','core'];return g})(),'EDGE_INVALID'],
 ['edge three endpoints',(()=>{const g=clone(base);g.edges[0]=['core','spine_l','gun'];return g})(),'EDGE_INVALID'],
 ['edge not array',(()=>{const g=clone(base);g.edges[0]={a:'core',b:'gun'};return g})(),'EDGE_INVALID'],
 ['duplicate edge reversed',(()=>{const g=clone(base);g.edges.push(['spine_l','core']);return g})(),'EDGE_DUPLICATE'],
 ['edge count overflow',(()=>{const g=clone(base);g.edges=Array(71).fill(['core','gun']);return g})(),'GRAPH_SCHEMA'],
 ['nodes count overflow',(()=>{const g=clone(base);g.nodes=Array(33).fill(g.nodes[0]);return g})(),'GRAPH_SCHEMA']
];
for(const [label,obj,reason] of invalids){check('reject '+label,()=>{const p=api.preview(obj,'spine_l');assert.equal(p.ok,false);assert.equal(p.code,reason);assert.equal(p.retained.length,0);assert.equal(p.detached.length,0);});}
const chain={version:1,coreId:'c',nodes:[{id:'c',label:'Core',kind:'core',x:10,y:10},{id:'a',label:'A',kind:'beam',x:30,y:10},{id:'b',label:'B',kind:'armor',x:50,y:10},{id:'d',label:'D',kind:'weapon',x:70,y:10}],edges:[['c','a'],['a','b'],['b','d']]};
check('chain bridge splits 2',()=>assert.deepEqual(api.preview(chain,'a').detached,['b','d']));
check('chain ending armor removes downstream 1',()=>assert.deepEqual(api.preview(chain,'b').detached,['d']));
check('chain last leaf removes no parts',()=>assert.deepEqual(api.preview(chain,'d').detached,[]));
check('chain sorted component B,D',()=>assert.deepEqual(api.preview(chain,'a').components,[['b','d'],['c']]));
const ring=clone(chain);ring.edges.push(['c','d']);
check('ring all routes survive single removal',()=>{for(const id of ['a','b','d'])assert.deepEqual(api.preview(ring,id).detached,[]);});
const isolated=clone(chain);isolated.edges=[['c','a'],['b','d']];
check('baseline disconnected is explicit',()=>assert.deepEqual(api.preview(isolated,'a').baselineDisconnected,['b','d']));
check('baseline disconnected not newly detached',()=>assert.deepEqual(api.preview(isolated,'a').detached,[]));
check('baseline disconnected excluded retained',()=>assert.deepEqual(api.preview(isolated,'a').retained,['c']));
check('removing disconnected node leaves baseline',()=>assert.deepEqual(api.preview(isolated,'b').baselineDisconnected,['b','d']));
check('removing disconnected node reveals correct components',()=>assert.deepEqual(api.preview(isolated,'b').components,[['a','c'],['d']]));
check('full scan compares all targets',()=>{const rows=api.scan(chain).rows;assert.equal(rows[0].id,'a');assert.equal(rows[0].detachedCount,2);});
for(let seed=1;seed<=18;seed++){
 check('deterministic subset fuzz '+seed,()=>{let g=clone(base);g.edges=g.edges.filter((_,i)=>((i*7+seed*11)%19)>seed%7);const a=api.preview(g,'spine_l'),b=api.preview(clone(g),'spine_l');assert.deepEqual(a,b);assert.deepEqual([...a.detached].sort(),a.detached);});
}
const out={suite:'B01-12-STRUCTURE-IMPACT-001',runner:'node',passed,failed,cases};fs.writeFileSync(path.join(dir,'node-results.json'),JSON.stringify(out,null,2)+'\n');console.log('NODE '+passed+'/'+(passed+failed)+' '+(failed?'FAIL':'PASS'));if(failed)process.exit(1);
