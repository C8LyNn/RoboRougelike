import test from "node:test";
import assert from "node:assert/strict";
import {readFileSync, readdirSync} from "node:fs";
import {join} from "node:path";
import {createHash} from "node:crypto";
const dir=process.env.P17_BLUEPRINTS_DIR;
if(!dir)throw Error("P17_BLUEPRINTS_DIR missing");
test("built real-source HTML has 18 playable MAPGEN ObstacleCourse blueprints",()=>{
 const html=readFileSync(join(dir,"COURSE-DRIVE-001.html"),"utf8");
 assert.ok(html.includes("COURSE-DRIVE-001"));
 assert.ok(html.includes('id="profile"')&&html.includes('id="auto"')&&html.includes('id="pause"'));
 assert.ok(!html.includes("__EMBEDDED_"));
 const match=html.match(/<script id="maps-data" type="application\/json">([\s\S]*?)<\/script>/);
 assert.ok(match,"missing embedded real original CSharp data");
 const maps=JSON.parse(match[1]);
 assert.equal(maps.length,18);
 assert.equal(new Set(maps.map(m=>m.geometryHash)).size,18);
 assert.deepEqual([...new Set(maps.map(m=>m.config.segments))].sort(),[3,4,5]);
 for(const m of maps){
  assert.equal(m.sourceSha,"5be31039411f89f17654b1c332f8692883fa03cc");
  assert.equal(m.nativeUnity,"NOT_RUN");
  assert.ok(m.checkpoints.length>=3);
 }
 const code=html.match(/<script type="module">([\s\S]*?)<\/script>/);
 assert.ok(code);
 assert.doesNotThrow(()=>new Function(code[1]),"embedded standalone core+app must parse");
 const receipt=JSON.parse(readFileSync(join(dir,"drive-receipt.json"),"utf8"));
 assert.equal(receipt.originalSourceBlueprints,18);
 assert.equal(receipt.bundleSHA256,createHash("sha256").update(readFileSync(join(dir,"COURSE-DRIVE-001.html"))).digest("hex"));
 assert.equal(receipt.nativePhysX,"NOT_RUN");
});
test("browser bundle is truly single file with no external src or network dependency",()=>{
 const html=readFileSync(join(dir,"COURSE-DRIVE-001.html"),"utf8");
 assert.ok(!/<script[^>]+src=/.test(html));
 assert.ok(!/<link[^>]+href=/.test(html));
 assert.ok(!/<iframe/.test(html));
 assert.ok(!/fetch\s*\(/.test(html));
 assert.ok(!/XMLHttpRequest/.test(html));
});
test("all 18 source blueprint files are archived alongside standalone viewer",()=>{
 const n=readdirSync(dir).filter(x=>/^P17-COURSE-.*-seed-[0-9]+\.json$/.test(x)).length;
 assert.equal(n,18);
});
