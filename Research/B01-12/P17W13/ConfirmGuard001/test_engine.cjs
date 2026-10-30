'use strict';
const fs=require('fs'),path=require('path'),crypto=require('crypto'),assert=require('assert/strict');
const base=__dirname, manifest=JSON.parse(fs.readFileSync(path.join(base,'manifest.json'),'utf8'));
function pin(){for(const [name,expected] of Object.entries(manifest.pins)){const raw=fs.readFileSync(path.join(base,name));if(raw.length!==expected.bytes||crypto.createHash('sha256').update(raw).digest('hex')!==expected.sha256){console.error('FAIL_CLOSED '+name);process.exit(2);}}}
pin();const E=require('./confirm_engine');let results=[];
function check(name,fn){try{fn();results.push({name,pass:true})}catch(e){results.push({name,pass:false,error:e.message});}}
const ev=(type,at,value='')=>({type,at,value});
function go(s,type,at,value=''){const r=E.transition(s,ev(type,at,value));assert.equal(r.ok,true,r.reason);return r.state;}
check('initial valid',()=>assert(E.validate(E.initial())));
check('no initial receipt',()=>assert.equal(E.initial().receipt,null));
check('three actions',()=>assert.equal(Object.keys(E.ACTIONS).length,3));
check('two modes',()=>assert.equal(E.MODES.length,2));
check('hold threshold 1200',()=>assert.equal(E.HOLD_MS,1200));
check('TTL 6000',()=>assert.equal(E.TTL_MS,6000));
for(const action of Object.keys(E.ACTIONS)){
  check('switch action '+action,()=>{let s=go(E.initial(),'SET_ACTION',10,action);assert.equal(s.action,action);});
  check('hold commits '+action,()=>{let s=go(E.initial(),'SET_ACTION',10,action);s=go(s,'ARM',20);s=go(s,'HOLD_START',40,'mouse');s=go(s,'HOLD_END',1240,'mouse');assert.equal(s.phase,'committed');assert.equal(s.receipt.action,action);});
  check('two-step commits '+action,()=>{let s=go(E.initial(),'SET_ACTION',10,action);s=go(s,'SET_MODE',11,'two-step');s=go(s,'ARM',20);s=go(s,'CONFIRM',220);assert.equal(s.phase,'committed');assert.equal(s.receipt.mode,'two-step');});
}
for(const early of [0,1,500,1199])check('too short '+early,()=>{let s=go(E.initial(),'ARM',1);s=go(s,'HOLD_START',2,'x');s=go(s,'HOLD_END',2+early,'x');assert.equal(s.committed,0);assert.equal(s.phase,'armed');});
for(const ok of [1200,1201,2000,5000])check('hold succeeds '+ok,()=>{let s=go(E.initial(),'ARM',1);s=go(s,'HOLD_START',2,'x');s=go(s,'HOLD_END',2+ok,'x');assert.equal(s.committed,1);});
for(const expiry of [6000,6001,25000])check('expires '+expiry,()=>{let s=go(E.initial(),'ARM',0);s=go(s,'TICK',expiry);assert.equal(s.phase,'expired');assert.equal(s.receipt,null);});
for(const expiry of [6000,6001])check('hold expiry '+expiry,()=>{let s=go(E.initial(),'ARM',0);s=go(s,'HOLD_START',1,'x');s=go(s,'HOLD_END',expiry,'x');assert.equal(s.phase,'expired');});
for(const k of ['wrong','keyboard','P_2'])check('pointer '+k,()=>{let s=go(E.initial(),'ARM',0);s=go(s,'HOLD_START',1,k);assert.equal(E.transition(s,ev('HOLD_END',1201,'incorrect')).reason,'POINTER_MISMATCH');});
for(const delta of [0,1,199])check('two step too early '+delta,()=>{let s=go(E.initial(),'SET_MODE',0,'two-step');s=go(s,'ARM',1);assert.equal(E.transition(s,ev('CONFIRM',1+delta)).reason,'TOO_EARLY');});
for(const delta of [200,201,5999])check('two step accepted '+delta,()=>{let s=go(E.initial(),'SET_MODE',0,'two-step');s=go(s,'ARM',1);s=go(s,'CONFIRM',1+delta);assert.equal(s.committed,1);});
for(const evt of ['HOLD_START','HOLD_END','CONFIRM','TICK'])check('duplicate fail safety '+evt,()=>{let s=go(E.initial(),'ARM',1);s=go(s,'HOLD_START',2,'x');s=go(s,'HOLD_END',1202,'x');let r=E.transition(s,ev(evt,1203,evt==='HOLD_START'||evt==='HOLD_END'?'x':''));assert.equal(r.state.committed,1);assert.equal(r.state.receipt.serial,1);});
check('cancel clears',()=>{let s=go(E.initial(),'ARM',0);s=go(s,'HOLD_START',1,'x');s=go(s,'CANCEL',100);assert.equal(s.phase,'idle');assert.equal(s.receipt,null);});
check('reset commits counter',()=>{let s=go(E.initial(),'ARM',0);s=go(s,'HOLD_START',1,'x');s=go(s,'HOLD_END',1201,'x');s=go(s,'RESET',1300);assert.equal(s.committed,0);assert.equal(s.phase,'idle');});
check('rearm new receipt serial',()=>{let s=go(E.initial(),'ARM',0);s=go(s,'HOLD_START',1,'x');s=go(s,'HOLD_END',1201,'x');s=go(s,'ARM',1300);assert.equal(s.serial,2);assert.equal(s.receipt,null);});
check('invalid input object',()=>{assert.equal(E.transition(E.initial(),null).ok,false);});
check('invalid state rejected',()=>{assert.equal(E.transition({...E.initial(),committed:2},ev('ARM',1)).reason,'INVALID_STATE');});
check('input immutability',()=>{let s=E.initial();const b=JSON.stringify(s);E.transition(s,ev('ARM',10));assert.equal(JSON.stringify(s),b);});
check('nonmonotonic rejects',()=>{let s=go(E.initial(),'ARM',50);assert.equal(E.transition(s,ev('TICK',49)).reason,'NON_MONOTONIC_CLOCK');});
check('jump rejects',()=>{assert.equal(E.transition(E.initial(),ev('ARM',86400001)).reason,'CLOCK_JUMP');});
check('cancel recovers after giant clock jump',()=>{let s=go(E.initial(),'ARM',1);s=go(s,'CANCEL',86400003);assert.equal(s.phase,'idle');});
check('reset recovers after giant clock jump',()=>{let s=go(E.initial(),'ARM',1);s=go(s,'RESET',86400003);assert.equal(s.phase,'idle');});
check('unknown action',()=>{assert.equal(E.transition(E.initial(),ev('SET_ACTION',5,'danger')).reason,'UNKNOWN_ACTION');});
check('unknown mode',()=>{assert.equal(E.transition(E.initial(),ev('SET_MODE',5,'instant')).reason,'UNKNOWN_MODE');});
check('invalid pointer',()=>{let s=go(E.initial(),'ARM',0);assert.equal(E.transition(s,ev('HOLD_START',10,'<invalid>')).reason,'INVALID_POINTER');});
check('unknown event',()=>{assert.equal(E.transition(E.initial(),ev('INJECT',1)).reason,'UNKNOWN_EVENT');});
check('too many event fields',()=>{assert.equal(E.transition(E.initial(),{...ev('ARM',1),force:true}).reason,'INVALID_EVENT');});
check('big value rejected',()=>{assert.equal(E.transition(E.initial(),ev('ARM',1,'x'.repeat(100))).reason,'INVALID_VALUE');});
check('switch blocked while armed',()=>{let s=go(E.initial(),'ARM',0);assert.equal(E.transition(s,ev('SET_ACTION',10,'abandon')).reason,'BUSY');});
check('no commit on tick',()=>{let s=go(E.initial(),'ARM',0);s=go(s,'HOLD_START',1,'x');s=go(s,'TICK',2000);assert.equal(s.phase,'holding');assert.equal(s.committed,0);});
check('progress <=100',()=>{let s=go(E.initial(),'ARM',0);s=go(s,'HOLD_START',1,'x');s=go(s,'TICK',2000);assert.equal(E.progress(s),100);});
check('unknown event preserves state',()=>{let s=go(E.initial(),'ARM',1);const b=JSON.stringify(s);E.transition(s,ev('BAD',5));assert.equal(JSON.stringify(s),b);});
check('expired cannot confirm',()=>{let s=go(E.initial(),'SET_MODE',0,'two-step');s=go(s,'ARM',1);s=go(s,'TICK',6001);assert.equal(E.transition(s,ev('CONFIRM',6002)).ok,false);});
const pass=results.filter(x=>x.pass).length;fs.writeFileSync(path.join(base,'node-results.json'),JSON.stringify({total:results.length,passed:pass,failed:results.length-pass,cases:results},null,2)+'\n');console.log(`NODE ${pass}/${results.length} ${pass===results.length?'PASS':'FAIL'}`);if(pass!==results.length){console.error(results.filter(x=>!x.pass));process.exit(1)}
