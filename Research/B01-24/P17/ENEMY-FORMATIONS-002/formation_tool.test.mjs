import test from "node:test";
import assert from "node:assert/strict";
import {readContent,checkPins,validate,resolveFormation,asUnityVectors,previewSvg} from "./formation_tool.mjs";

const fresh=()=>readContent();
const broken=change=>{const c=fresh();change(c);assert.throws(()=>validate(c));};
test("canonical original R5Arena + R1EncounterManager + R1Definition + R5Plan Git blobs",()=>assert.equal(checkPins(),4));
test("original game geometry accepts three arena variants, 12 layouts and 30 positions",()=>{
 assert.deepEqual(validate(fresh()),{
  formations:3,layouts:12,validated_positions:30,source:"original-R5-matching-static-XZ-geometry-only"
 });
});
for(const f of fresh().formations){
 for(let count=1;count<=4;count++)test(f.id+" spawn "+count,()=>{
  const result=resolveFormation(fresh(),f.id,count);
  assert.equal(result.length,count);
  assert.ok(result.every(p=>p.y===0&&Number.isFinite(p.x)&&Number.isFinite(p.z)));
  assert.equal(new Set(result.map(p=>p.x+","+p.z)).size,count);
 });
}
test("original Unity Vector3 constructor output matches selected actual content",()=>{
 const source=asUnityVectors(resolveFormation(fresh(),"B0124-SCRAP-PINCER",3));
 assert.equal((source.match(/new Vector3\(/g)||[]).length,3);
 assert.ok(source.startsWith("new Vector3[] { "));
 assert.ok(source.includes("new Vector3(-14.0f, 0.0f, 17.0f)"));
});
test("SVG scene preview is original geometry and unit visual, not stock art",()=>{
 const s=previewSvg(fresh(),"B0124-INDUSTRIAL-CROSSFIRE",4);
 assert.ok(s.startsWith("<svg xmlns="));
 assert.equal((s.match(/<rect /g)||[]).length,1+4+6);
 assert.equal((s.match(/<circle /g)||[]).length,1+4);
});
test("deterministic emitter produces identical position and SVG bytes",()=>{
 const c=fresh(),id="B0124-WASTE-CHASE";
 assert.equal(previewSvg(c,id,4),previewSvg(c,id,4));
 assert.equal(asUnityVectors(resolveFormation(c,id,4)),asUnityVectors(resolveFormation(c,id,4)));
});
test("source content is never mutated by validate/resolve/preview",()=>{
 const c=fresh(),initial=JSON.stringify(c);validate(c);
 resolveFormation(c,c.formations[0].id,4);previewSvg(c,c.formations[2].id,2);
 assert.equal(JSON.stringify(c),initial);
});
test("reject overlapping two enemies",()=>broken(c=>{c.formations[0].options["4"][1]=[c.formations[0].options["4"][0][0]+1,17];}));
test("reject source geometry: industrial central fixed solid wall",()=>broken(c=>{c.formations[2].options["1"][0]=[17,7];}));
test("reject source geometry: salvage yard press",()=>broken(c=>{c.formations[0].options["1"][0]=[-10,0];}));
test("reject source geometry: wasteland rock",()=>broken(c=>{c.formations[1].options["1"][0]=[26,8];}));
test("reject source geometry: arena boundary",()=>broken(c=>{c.formations[1].options["1"][0]=[41,0];}));
test("reject unit too close to existing player spawn",()=>broken(c=>{c.formations[2].options["1"][0]=[0,-12];}));
test("reject truncated player/enemy envelope",()=>broken(c=>{c.conservative_vehicle_radius_m=0.1;}));
test("reject missing spawn count 3",()=>broken(c=>{delete c.formations[0].options["3"];}));
test("reject incorrect formation length",()=>broken(c=>{c.formations[1].options["2"].pop();}));
test("reject duplicate formation IDs",()=>broken(c=>{c.formations[1].id=c.formations[0].id;}));
test("reject duplicate arena assignment",()=>broken(c=>{c.formations[2].arena=0;}));
test("reject invalid arena enum",()=>broken(c=>{c.formations[2].arena=99;}));
test("reject NaN spawn",()=>broken(c=>{c.formations[0].options["1"][0][0]=Number.NaN;}));
test("reject missing package version",()=>broken(c=>{c.version="2.0.0";}));
test("reject unsupported SpawnCount, no silent clip",()=>assert.throws(()=>resolveFormation(fresh(),"B0124-SCRAP-PINCER",5)));
test("reject unknown formation ID",()=>assert.throws(()=>resolveFormation(fresh(),"does-not-exist",2)));
test("reject additional unapproved formation entry",()=>broken(c=>{c.formations.push({...c.formations[0],id:"extra",arena:3});}));
