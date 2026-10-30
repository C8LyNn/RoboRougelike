/* Real R1 UX failure reproducer: original source is immutable and hash-pinned. */
const fs=require('fs'),crypto=require('crypto'),assert=require('assert/strict');
const old=__dirname+'/r1-original/';
const pins={"fixtures.js": "bce8c395a06a56af3446c732a4917046a560a385ffb99979a69788b46ed07c3a", "readout_engine.js": "06c00121b607dd3dca78d3d5e189a3f0fbf05cd446c52ced6a326b0570cf2fb2"};
for(const [file,expected] of Object.entries(pins)){
 const b=fs.readFileSync(old+file);assert.equal(crypto.createHash('sha256').update(b).digest('hex'),expected,'FROZEN_R1_SHA_FAIL '+file);
}
const oldEngine=require(old+'readout_engine.js');
const item=require(old+'fixtures.js').list()[1];delete item.label;item.victoryId=null;item.freeAvailable=true;
const view=oldEngine.project(item);
assert.equal(view.ok,true);assert.match(view.freeLabel,/dostępna/);assert.match(view.victoryLabel,/Nie potwierdzono/);
console.log('ORIGINAL_R1_UX_RED_CONFIRMED: no victory ID, yet displayed free rebuild available; exact R1 hashes PASS.');
