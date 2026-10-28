import assert from 'node:assert/strict';
import test from 'node:test';
import fs from 'node:fs';
import {fileURLToPath} from 'node:url';
import path from 'node:path';
import {verifyPinnedFiles,evaluatePolicyCase,proposedHostSocketPolicy,manifests} from './W26_source_ownership_review.mjs';
const base=path.dirname(fileURLToPath(import.meta.url));
const cases=JSON.parse(fs.readFileSync(path.join(base,'W26_acceptance_fixtures.json'),'utf8'));
test('Manifest locked to exact two source owners',()=>{assert.equal(manifests.source_files.length,5);assert.equal(manifests.g2,'HOLD')});
for(const x of cases.checkpoint)test('CHL '+x.id,()=>assert.equal(evaluatePolicyCase(x.input),x.expected));
for(const x of cases.socket)test('PART '+x.id,()=>{
 assert.equal(evaluatePolicyCase(x.input),x.expectedCurrent);
 assert.equal(proposedHostSocketPolicy(x.input),x.expectedFuture);
});
test('Socket fail-closed even if every hypothetical property is true',()=>assert.notEqual(evaluatePolicyCase(cases.socket.at(-1).input),'MOUNT_APPROVED'));
test('Missing real producer files must explicitly FAIL not fabricate source PASS',()=>{
 const r=verifyPinnedFiles(path.join(base,'definitely-no-sources'));
 assert.equal(r.status,'FAIL');assert.equal(r.blobs.length,0);
});
test('Original pinned source Git blobs and anchors verified when supplied',()=>{
 const input=process.env.W26_ORIGINAL_SOURCE_DIR;
 if(!input){console.log('REAL_GITHUB_BLOB_CHECK=NOT_RUN (no original downloaded source directory)');return}
 const receipt=verifyPinnedFiles(input);
 console.log('REAL_SOURCE_RECEIPT='+JSON.stringify(receipt));
 assert.equal(receipt.status,'PASS',receipt.failures.join(','));
 assert.equal(receipt.blobs.length,5);
});
