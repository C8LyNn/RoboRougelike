import test from 'node:test';
import assert from 'node:assert/strict';
import {analyzeCapture,compare,makeSyntheticCapture,parseCsv,quantile,FRAME_FIELDS,OBJECT_FIELDS} from '../core.mjs';
const original=()=>makeSyntheticCapture();
const replaceJson=(data,change)=>{const rec=JSON.parse(data[0]);change(rec);data[0]=JSON.stringify(rec);return data;};
function csvModify(data,index,fn){let lines=data[index].split('\n');fn(lines);data[index]=lines.join('\n');return data;}
const fails=(fn,pattern)=>assert.throws(fn,pattern);
test('original schema: synthetic positive gives only unverified classification',()=>{
 let x=analyzeCapture(...original());assert.equal(x.frames,7200);assert.equal(x.scans,120);assert.equal(x.timeline.length,12);
 assert.equal(x.metric,'UNITY_UNSCALED_DELTA_NOT_DISPLAYED_FRAME_TIME');assert.equal(x.classification,'FILE_STRUCTURE_VALIDATED_ONLY_NATIVE_PROVENANCE_NOT_VERIFIED');
 assert.equal(x.profiler,'CPU_MAIN_RENDER_GPU_GC_NOT_MEASURED');assert(x.p95<20);
});
test('P95/P99 and binning reflect trace, not invented 60 FPS',()=>{
 const a=analyzeCapture(...makeSyntheticCapture({slow:0}));const b=analyzeCapture(...makeSyntheticCapture({slow:2,shots:64}));
 const c=compare(a,b);assert.equal(c.comparable,true);assert.equal(c.p95DeltaMs,2);assert.equal(c.p99DeltaMs,2);assert(c.shotDelta>=64);
 assert(b.updateFpsEstimate<a.updateFpsEstimate);
});
test('missing rows and skipped frame index fail closed',()=>{
 let a=csvModify(original(),1,ls=>ls.splice(500,1));fails(()=>analyzeCapture(...a),/row count mismatch/);
 let b=csvModify(original(),1,ls=>ls[5]=ls[5].replace(/^4,/, '99,'));fails(()=>analyzeCapture(...b),/non-sequential/);
});
test('duplicate header and foreign columns rejected',()=>{
 let a=csvModify(original(),1,ls=>ls[0]+=',delta_ms');fails(()=>analyzeCapture(...a),/missing\/extra|unexpected/);
 let b=csvModify(original(),2,ls=>ls[0]='elapsed_ms,scene,active_parts,active_projectiles,active_enemies,rigidbodies,scan_ms,wrong');fails(()=>analyzeCapture(...b),/missing\/extra|unexpected/);
});
test('malformed CSV quoting fails closed; standard quoted scene accepted',()=>{
 let a=csvModify(original(),2,ls=>ls[7]='6000,"unclosed,23,0,0,0,0.5');fails(()=>analyzeCapture(...a),/unclosed quote/);
 let b=csvModify(original(),2,ls=>ls[1]='0,"Arena, Alpha",24,0,0,32,0.03');const x=analyzeCapture(...b);assert(x.scenes.includes('Arena, Alpha'));
});
test('malicious script-looking scene parsed as text, not executed',()=>{
 let a=csvModify(original(),2,ls=>ls[1]='0,"<img src=x onerror=alert(9)>",24,0,0,32,0.03');
 assert(analyzeCapture(...a).scenes[0].startsWith('<img'));
});
test('negative, NaN, Infinity, zeros and time travel rejected',()=>{
 for(const v of ['-4','NaN','Infinity','0']){let a=csvModify(original(),1,ls=>ls[1]=ls[1].replace(/,[0-9.]+$/,','+v));fails(()=>analyzeCapture(...a),/invalid|nonfinite|negative/);}
 let b=csvModify(original(),1,ls=>ls[2]='1,0,16.4');fails(()=>analyzeCapture(...b),/nonmonotonic/);
});
test('fake unity, inaccurate build SHA or false profiler fields rejected',()=>{
 for(const modify of [r=>r.unity_version='123',r=>r.game_sha='0',r=>r.build_zip_sha256='XX',r=>r.profiler_main_render_gpu_gc='0']){
 let a=replaceJson(original(),modify);fails(()=>analyzeCapture(...a),/unsupported|invalid|false/);
 }
});
test('incomplete 119-second capture refused even when rows exist',()=>{
 let a=replaceJson(original(),r=>r.observed_capture_seconds=119);fails(()=>analyzeCapture(...a),/incomplete/);
 let b=replaceJson(original(),r=>r.capture_complete=false);fails(()=>analyzeCapture(...b),/incomplete/);
});
test('scan row count and invalid counts fail closed',()=>{
 let a=csvModify(original(),2,ls=>ls.splice(100,1));fails(()=>analyzeCapture(...a),/scan count mismatch/);
 let b=csvModify(original(),2,ls=>ls[10]=ls[10].replace(',24,',',-1,'));fails(()=>analyzeCapture(...b),/invalid count/);
});
test('changed host/build/game produces explicit incompatible diagnostic',()=>{
 const a=analyzeCapture(...original());let v=replaceJson(original(),r=>{r.gpu='different GPU';r.build_zip_sha256='c'.repeat(64);});
 const b=analyzeCapture(...v),res=compare(a,b);assert.equal(res.comparable,false);assert.deepEqual(res.mismatches,['buildSha','gpu']);assert.equal(res.status,'CONTEXT_DIFFERS_NO_PERFORMANCE_INFERENCE');
});
test('changed scene produces explicit incompatible diagnostic',()=>{
 const a=analyzeCapture(...original());let v=csvModify(original(),2,ls=>ls[1]=ls[1].replace(',Arena,',',Other,')).slice();
 const b=analyzeCapture(...v);const x=compare(a,b);assert.equal(x.comparable,false);assert(x.mismatches.includes('scenes'));
});
test('quantile and CSV parser strictness, no phantom blanks',()=>{
 assert.equal(quantile([10,30,20,40],.5),25);
 const raw='frame_index,elapsed_ms,delta_ms\n0,1,2\n1,2,3\n';assert.equal(parseCsv(raw,FRAME_FIELDS).length,2);
 fails(()=>parseCsv(raw+'\n',FRAME_FIELDS),/missing\/extra/);
 fails(()=>parseCsv('frame_index,elapsed_ms,delta_ms\n0,1,2\n1,2,3,EXTRA\n',FRAME_FIELDS),/missing\/extra/);
});
test('receipt falsified row count and bogus unmeasured metrics blocked',()=>{
 let a=replaceJson(original(),r=>r.frame_rows=600);fails(()=>analyzeCapture(...a),/row count mismatch/);
 let b=replaceJson(original(),r=>r.classification='NATIVE_ACCEPTED');fails(()=>analyzeCapture(...b),/unsupported capture classification/);
});

test('different scenario IDs invalidate even matching build/hardware',()=>{
 const a=analyzeCapture(...makeSyntheticCapture({scenario:'G02_GUNS2'}));
 const b=analyzeCapture(...makeSyntheticCapture({scenario:'G02_GUNS12'}));
 const result=compare(a,b);
 assert.equal(result.comparable,false);
 assert.deepEqual(result.mismatches,['scenario']);
 assert.equal(result.status,'CONTEXT_DIFFERS_NO_PERFORMANCE_INFERENCE');
});
