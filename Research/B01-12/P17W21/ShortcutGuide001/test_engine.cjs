'use strict';
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const base=__dirname,manifest=JSON.parse(fs.readFileSync(path.join(base,'manifest.json'),'utf8'));
for(const [file,expected] of Object.entries(manifest.sources)){
 const b=fs.readFileSync(path.join(base,file));const hash=crypto.createHash('sha256').update(b).digest('hex');
 if(hash!==expected.sha256||b.length!==expected.bytes){console.error('FAIL_CLOSED source mismatch: '+file);process.exit(2);}
}
const E=require('./guide_engine.js');let done=0;const names=[];
function test(name,fn){try{fn();done++;names.push(name);}catch(e){console.error('RED '+name+' '+e.stack);process.exitCode=1;}}
const eq=(x,y)=>assert.deepEqual(x,y);
test('Schema exact version',()=>assert.equal(E.VERSION,'b01-12.shortcut-guide.v1'));
test('Immutable registry',()=>assert.ok(Object.isFrozen(E.COMMANDS)&&E.COMMANDS.every(Object.isFrozen)));
test('Approved original shortcuts',()=>eq(E.validateCommands(E.COMMANDS),{ok:true,count:20}));
test('Source has three distinct scenes',()=>eq(E.SCOPES,['garage','combat','map']));
test('Registry duplicate IDs rejected',()=>assert.equal(E.validateCommands([...E.COMMANDS,E.COMMANDS[0]]).error,'DUPLICATE_COMMAND_ID'));
test('Registry empty rejected',()=>assert.equal(E.validateCommands([]).error,'INVALID_REGISTRY_SIZE'));
test('Registry too large rejected',()=>assert.equal(E.validateCommands(Array(65).fill(E.COMMANDS[0])).error,'INVALID_REGISTRY_SIZE'));
test('Registry non-array rejected',()=>assert.equal(E.validateCommands(null).error,'INVALID_REGISTRY_SIZE'));
test('Registry extra keys rejected',()=>assert.equal(E.validateCommands([{...E.COMMANDS[0],extra:'x'}]).error,'INVALID_COMMAND_SCHEMA'));
test('Registry malicious key rejected',()=>assert.equal(E.validateCommands([{...E.COMMANDS[0],key:'<img>'}]).error,'INVALID_COMMAND_VALUE'));
test('Registry global conflict rejected',()=>assert.equal(E.validateCommands([...E.COMMANDS,{...E.COMMANDS[0],id:'global-copy',scope:'map'}]).error,'KEY_CONFLICT'));
test('Registry same-scope conflict rejected',()=>assert.equal(E.validateCommands([...E.COMMANDS,{...E.COMMANDS[6],id:'garage-copy'}]).error,'KEY_CONFLICT'));
test('Different scopes may share a key',()=>assert.equal(E.validateCommands([...E.COMMANDS,{...E.COMMANDS[6],id:'combat-copy',scope:'combat'}]).ok,true));
for(const scope of E.SCOPES){
 test('Accessible '+scope,()=>assert.equal(E.getCommands(scope).ok,true));
 test('Global help '+scope,()=>assert.ok(E.getCommands(scope).commands.some(c=>c.id==='help')));
 test('Global pause '+scope,()=>assert.ok(E.getCommands(scope).commands.some(c=>c.id==='pause')));
 test('Global screenshot '+scope,()=>assert.ok(E.getCommands(scope).commands.some(c=>c.id==='screenshot')));
 test('Other-context commands hidden '+scope,()=>assert.equal(E.getCommands(scope).commands.some(c=>c.scope!=='global'&&c.scope!==scope),false));
 test('Stable deterministic search '+scope,()=>eq(E.getCommands(scope,'ctrl'),E.getCommands(scope,'ctrl')));
 test('Empty search '+scope,()=>assert.equal(E.getCommands(scope,'absentyyyy').commands.length,0));
 test('Can serialize restore '+scope,()=>{const s=E.reduce(E.initial(),{type:'SCOPE',value:scope}).state;eq(E.restore(E.serialize(s).json).state,s)});
}
for(const c of E.COMMANDS){
 test('Search by ID title for '+c.id,()=>{const scope=c.scope==='global'?'garage':c.scope;assert.ok(E.getCommands(scope,c.title).commands.some(x=>x.id===c.id))});
 test('Select from relevant scene '+c.id,()=>{const scope=c.scope==='global'?'garage':c.scope;const s=E.reduce(E.initial(),{type:'SCOPE',value:scope}).state;assert.equal(E.reduce(s,{type:'SELECT',value:c.id}).state.selected,c.id)});
}
const fail=(s,e)=>{const before=JSON.stringify(s);const r=E.reduce(s,e);assert.equal(r.ok,false);assert.equal(JSON.stringify(s),before);return r.error;};
let s=E.initial();test('Initial state valid',()=>assert.equal(E.validateState(s),true));
test('Invalid context rejected',()=>assert.equal(fail(s,{type:'SCOPE',value:'outside'}),'INVALID_SCOPE'));
test('Unknown event rejected',()=>assert.equal(fail(s,{type:'EXECUTE',value:'anything'}),'UNKNOWN_EVENT'));
test('Missing value rejected',()=>assert.equal(fail(s,{type:'QUERY'}),'INVALID_EVENT'));
test('Additional event value rejected',()=>assert.equal(fail(s,{type:'QUERY',value:'x',debug:true}),'INVALID_EVENT'));
test('Prototype event rejected',()=>assert.equal(fail(s,new Date()),'INVALID_EVENT'));
test('Object query rejected',()=>assert.equal(fail(s,{type:'QUERY',value:{}}),'INVALID_QUERY'));
test('Query too long rejected',()=>assert.equal(fail(s,{type:'QUERY',value:'a'.repeat(65)}),'INVALID_QUERY'));
test('Control chars rejected',()=>assert.equal(fail(s,{type:'QUERY',value:'a\nb'}),'INVALID_QUERY'));
test('Unrecognized select denied',()=>assert.equal(fail(s,{type:'SELECT',value:'hidden'}),'UNKNOWN_OR_HIDDEN_COMMAND'));
test('Selection wrong context denied',()=>assert.equal(fail(s,{type:'SELECT',value:'combat-boost'}),'UNKNOWN_OR_HIDDEN_COMMAND'));
test('Selection non-string denied',()=>assert.equal(fail(s,{type:'SELECT',value:5}),'INVALID_SELECTION'));
test('Toggle with extra data denied',()=>assert.equal(fail(s,{type:'TOGGLE',value:1}),'INVALID_TOGGLE'));
test('Close with extra data denied',()=>assert.equal(fail(s,{type:'CLOSE',value:'hi'}),'INVALID_CLOSE'));
test('Reset with extra data denied',()=>assert.equal(fail(s,{type:'RESET',value:1}),'INVALID_RESET'));
test('Initial revision',()=>assert.equal(s.revision,0));
test('Selection changes revision',()=>{s=E.reduce(s,{type:'SELECT',value:'garage-move'}).state;assert.equal(s.revision,1)});
test('Select key remembered',()=>assert.equal(s.selected,'garage-move'));
test('Search clears selection',()=>{s=E.reduce(s,{type:'QUERY',value:'ctrl'}).state;assert.equal(s.selected,null)});
test('Filtered selection denied',()=>assert.equal(fail(s,{type:'SELECT',value:'garage-move'}),'UNKNOWN_OR_HIDDEN_COMMAND'));
test('Search allows Ctrl+Z',()=>assert.ok(E.getCommands('garage','CTRL+Z').commands.some(c=>c.id==='garage-undo')));
test('Context switch clears filter',()=>{s=E.reduce(s,{type:'SCOPE',value:'combat'}).state;assert.equal(s.query,'')});
test('Context switch sets visible',()=>assert.equal(s.open,true));
test('Context switch clears selected',()=>assert.equal(s.selected,null));
test('Close clears selected',()=>{s=E.reduce(s,{type:'SELECT',value:'combat-fire1'}).state;s=E.reduce(s,{type:'CLOSE',value:null}).state;assert.equal(s.selected,null)});
test('Close hides',()=>assert.equal(s.open,false));
test('Toggle opens',()=>{s=E.reduce(s,{type:'TOGGLE',value:null}).state;assert.equal(s.open,true)});
test('Restore denies incompatible version',()=>assert.equal(E.restore(JSON.stringify({...s,version:'v0'})).error,'INVALID_STATE'));
test('Restore denies future unknown selection',()=>assert.equal(E.restore(JSON.stringify({...s,selected:'nope'})).error,'STALE_SELECTION'));
test('Restore denies selection on hidden panel',()=>assert.equal(E.restore(JSON.stringify({...s,selected:'combat-fire1',open:false})).error,'HIDDEN_SELECTION'));
test('Restore denies duplicate foreign field',()=>assert.equal(E.restore(JSON.stringify({...s,evil:true})).error,'INVALID_STATE'));
test('Restore denies invalid JSON',()=>assert.equal(E.restore('{').error,'BAD_JSON'));
test('Restore denies oversized',()=>assert.equal(E.restore('a'.repeat(513)).error,'INVALID_JSON_SIZE'));
test('Restore denies string length null',()=>assert.equal(E.restore(null).error,'INVALID_JSON_SIZE'));
test('Restore valid roundtrip',()=>eq(E.restore(E.serialize(s).json).state,s));
test('Reduce invalid state fail closed',()=>assert.equal(E.reduce({...s,revision:-1},{type:'RESET',value:null}).error,'INVALID_STATE'));
test('Max revision rejects before writes',()=>assert.equal(E.reduce({...s,revision:1000000},{type:'TOGGLE',value:null}).error,'REVISION_EXHAUSTED'));
test('Reset produces default plus monotonic revision',()=>{const prior=s.revision;s=E.reduce(s,{type:'RESET',value:null}).state;assert.equal(s.revision,prior+1);assert.equal(s.scope,'garage')});
test('Immutable returned state',()=>assert.equal(Object.isFrozen(s),true));
test('Unicode search accented text',()=>assert.equal(E.getCommands('garage','Shortcut guide').commands.length,1));
test('Query rejects invalid context',()=>assert.equal(E.getCommands('other','').error,'INVALID_QUERY'));
test('Query rejects tab',()=>assert.equal(E.getCommands('garage','\t').error,'INVALID_QUERY'));
test('Query does not mutate registry',()=>{const before=JSON.stringify(E.COMMANDS);E.getCommands('garage','zz');assert.equal(JSON.stringify(E.COMMANDS),before)});
test('Sorted results invariant',()=>{const all=E.getCommands('garage').commands;for(let i=1;i<all.length;i++)assert.ok(all[i-1].category.localeCompare(all[i].category)<=0)});
if(process.exitCode){process.exit(1)}
const record={suite:'node',total:done,passed:done,failed:0,names};fs.writeFileSync(path.join(base,'node-results.json'),JSON.stringify(record,null,2)+'\n');console.log('NODE '+done+'/'+done+' PASS');
