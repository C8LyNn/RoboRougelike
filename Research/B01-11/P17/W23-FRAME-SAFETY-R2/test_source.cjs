'use strict';
const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const crypto=require('node:crypto');
const cp=require('node:child_process');
const ROOT=path.resolve(__dirname,'../../../../');
const R1='c27fd4ecef6df52b0b8086cc2234904918fa1c4b';
const HTML='Research/B01-11/P17/W22-FRAME-SAFETY-001/index.html';
const html=fs.readFileSync(path.join(ROOT,HTML),'utf8');
const old=cp.execFileSync('git',['show',R1+':'+HTML],{cwd:ROOT,encoding:'utf8'});
function gitBlob(text){const buf=Buffer.from(text);return crypto.createHash('sha1').update('blob '+buf.length+'\0').update(buf).digest('hex');}
function verifyManifest(relative,expected){
 const m=JSON.parse(fs.readFileSync(path.join(ROOT,relative),'utf8'));
 assert.equal(m.package,expected);
 const seen=new Set();
 for(const entry of m.files){
   assert.equal(typeof entry.path,'string');
   assert.ok(!entry.path.includes('..')&&!path.isAbsolute(entry.path));
   assert.ok(!seen.has(entry.path));seen.add(entry.path);
   const bytes=fs.readFileSync(path.join(ROOT,entry.path));
   assert.equal(bytes.length,entry.bytes,entry.path);
   assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),entry.sha256,entry.path);
 }
 return m;
}
test('historical W22 R1 source blob is exact and immutable',()=>assert.equal(gitBlob(old),'b0b71ef7299590b1bea4227165df2eeffc54c87a'));
test('historical R1 reset actually altered loadout and FOV',()=>{
 assert.match(old,/scene=fresh\(\);scene\.aspect=keepAspect/);
 assert.match(old,/for\(const id of \['weapon-left','weapon-right'\]\)\$\(id\)\.setAttribute\('aria-pressed','true'\)/);
});
test('R2 has two separate reset buttons with distinct intent',()=>{
 assert.match(html,/id="reset" type="button">Przywróć kamerę/);
 assert.match(html,/id="reset-all" type="button">Zresetuj całe doświadczenie/);
});
test('R2 camera-only reset only sets camera and not fresh loadout',()=>{
 const start=html.indexOf("$('reset').addEventListener('click'");
 const end=html.indexOf("$('reset-all').addEventListener('click'");
 assert.ok(start>0&&end>start);
 const handler=html.slice(start,end);
 assert.match(handler,/scene\.camera=\{\.\.\.fresh\(\)\.camera\}/);
 assert.doesNotMatch(handler,/scene=fresh\(\)/);
 assert.doesNotMatch(handler,/\.active=/);
 assert.doesNotMatch(handler,/setViewport\(/);
 assert.doesNotMatch(handler,/verticalFov=/);
 assert.doesNotMatch(handler,/setAttribute\('aria-pressed'/);
});
test('R2 full reset explicitly replaces scene + viewport',()=>{
 const start=html.indexOf("$('reset-all').addEventListener('click'");
 const end=html.indexOf("\n  draw();\n})();",start);
 assert.ok(start>0&&end>start);
 const handler=html.slice(start,end);
 assert.match(handler,/scene=fresh\(\)/);
 assert.match(handler,/setViewport\(false\)/);
 assert.match(handler,/aria-pressed','true'/);
});
test('W22 original source tests and core remain unchanged',()=>{
 const original=cp.execFileSync('git',['show',R1+':Research/B01-11/P17/W22-FRAME-SAFETY-001/frame.js'],{cwd:ROOT,encoding:'utf8'});
 const current=fs.readFileSync(path.join(ROOT,'Research/B01-11/P17/W22-FRAME-SAFETY-001/frame.js'),'utf8');
 assert.equal(current,original);
});
test('W22 original manifest updated, fully valid and self-excluding',()=>{
 const m=verifyManifest('Research/B01-11/P17/W22-FRAME-SAFETY-001/manifest.json','B01-11-FRAME-SAFETY-001/R1');
 assert.equal(m.files.length,5);
 const f=m.files.find(x=>x.path===HTML);
 assert.ok(f&&f.sha256!==crypto.createHash('sha256').update(old).digest('hex'));
});
test('W23 R2 manifest pins all changed artifacts and exact bytes',()=>{
 const m=verifyManifest('Research/B01-11/P17/W23-FRAME-SAFETY-R2/manifest.json','B01-11-FRAME-SAFETY-001/R2');
 assert.equal(m.files.length,6);
 assert.ok(m.files.some(f=>f.path===HTML));
 assert.ok(m.files.some(f=>f.path.endsWith('/test_browser.cjs')));
});
test('W23 dedicated workflow retains two OS source QA + native browser QA',()=>{
 const y=fs.readFileSync(path.join(ROOT,'.github/workflows/b01-11-p17w23-frame-safety-r2.yml'),'utf8');
 for(const token of ['ubuntu-24.04','windows-2022','test_source.cjs','test_browser.cjs','playwright','node --test','fetch-depth: 16'])assert.ok(y.includes(token),token);
});
test('no Unity or actual player evidence claimed in W23 README',()=>{
 const md=fs.readFileSync(path.join(__dirname,'README.md'),'utf8');
 assert.match(md,/G1_NOT_RUN/);
 assert.match(md,/headless Chromium/);
 assert.match(md,/not Unity/);
});
