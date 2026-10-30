import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import zlib from "node:zlib";
import crypto from "node:crypto";
import {spawnSync} from "node:child_process";
import {fileURLToPath} from "node:url";
import {crc32,png,markers,receipt,writePrivate,W13_BLOB,SELECTED} from "./screenshot_witness.mjs";
function chunk(type,data){const name=Buffer.from(type),a=Buffer.alloc(4),b=Buffer.alloc(4);a.writeUInt32BE(data.length);b.writeUInt32BE(crc32(Buffer.concat([name,data])));return Buffer.concat([a,name,data,b])}
function image(color=5){
 const hdr=Buffer.alloc(13);hdr.writeUInt32BE(3);hdr.writeUInt32BE(2,4);hdr[8]=8;hdr[9]=2;
 const row=Buffer.from([0,color,10,15,color,10,15,color,10,15,0,color,10,15,color,10,15,color,10,15]);
 return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk("IHDR",hdr),chunk("IDAT",zlib.deflateSync(row)),chunk("IEND",Buffer.alloc(0))]);
}
const pair=()=>({schema:"B01-13-VISUAL-SAVE-R1",selected_sha:SELECTED.slice(5),player_exe_sha256:"f".repeat(64),source_to_player:"UNVERIFIED",native_unity:"NOT_RUN",human_verified:0,mode:"TECHNICAL_ONLY",scenario:"DETACHED_HELD",session:"T0001",unfilled:false,snapshots:[{stage:"BEFORE_SAVE",modules:[{visual_token:"V01",state:"ATTACHED",socket:"S01"},{visual_token:"V02",state:"DETACHED",socket:null}]},{stage:"AFTER_LOAD",modules:[{visual_token:"V01",state:"ATTACHED",socket:"S01"},{visual_token:"V02",state:"DETACHED",socket:null}]}]});
const marks=()=>({schema:"B01-13-PNG-RELOAD-MARKERS/R1",observations:[{stage:"BEFORE_SAVE",visual_token:"V01",x:0,y:0},{stage:"BEFORE_SAVE",visual_token:"V02",x:2,y:1},{stage:"AFTER_LOAD",visual_token:"V01",x:0,y:1},{stage:"AFTER_LOAD",visual_token:"V02",x:1,y:1}]});
const ims=()=>({BEFORE_SAVE:png(image()),AFTER_LOAD:png(image(9))});
function tmp(f){const d=fs.mkdtempSync(path.join(os.tmpdir(),"b01-w14-"));try{return f(d)}finally{fs.rmSync(d,{recursive:true,force:true})}}
test("real PNG accepted, dimensions, physical bytes hash",()=>{const i=png(image());assert.equal(i.width,3);assert.equal(i.height,2);assert.equal(i.sha256,crypto.createHash("sha256").update(image()).digest("hex"))});
test("PNG different image SHA",()=>assert.notEqual(png(image(4)).sha256,png(image(6)).sha256));
test("PNG signature negative",()=>{const b=image();b[1]^=3;assert.throws(()=>png(b),/PNG_SIGNATURE/)});
test("PNG CRC negative",()=>{const b=image();b[22]^=1;assert.throws(()=>png(b),/PNG_CRC/)});
test("PNG IDAT tamper negative",()=>{const b=image();b[b.length-22]^=1;assert.throws(()=>png(b),/PNG_CRC|PNG_DEFLATE/)});
test("PNG trailing data negative",()=>assert.throws(()=>png(Buffer.concat([image(),Buffer.from("extra")])),/PNG_END/));
test("PNG truncated negative",()=>assert.throws(()=>png(image().subarray(0,30)),/PNG_SIGNATURE|PNG_LENGTH|PNG_TRUNCATED/));
test("PNG huge dimensions fail closed",()=>{const b=image();b.writeUInt32BE(99999,16);const c=Buffer.alloc(4);c.writeUInt32BE(crc32(b.subarray(12,29)));c.copy(b,29);assert.throws(()=>png(b),/PNG_SIZE/)});
test("four valid markers",()=>assert.equal(markers(pair(),marks(),ims()).length,4));
test("missing marker denied",()=>{const m=marks();m.observations.pop();assert.throws(()=>markers(pair(),m,ims()),/MARK_COVERAGE/)});
test("duplicate marker denied",()=>{const m=marks();m.observations.push({...m.observations[0]});assert.throws(()=>markers(pair(),m,ims()),/MARK_UNKNOWN_OR_DUPLICATE/)});
test("unknown token denied",()=>{const m=marks();m.observations[0].visual_token="V99";assert.throws(()=>markers(pair(),m,ims()),/MARK_UNKNOWN_OR_DUPLICATE/)});
test("x outside PNG denied",()=>{const m=marks();m.observations[0].x=3;assert.throws(()=>markers(pair(),m,ims()),/MARK_BOUNDS/)});
test("negative y denied",()=>{const m=marks();m.observations[0].y=-1;assert.throws(()=>markers(pair(),m,ims()),/MARK_BOUNDS/)});
test("fractional x denied",()=>{const m=marks();m.observations[0].x=.5;assert.throws(()=>markers(pair(),m,ims()),/MARK_BOUNDS/)});
test("PII field denied",()=>{const m=marks();m.observations[0].participant="name";assert.throws(()=>markers(pair(),m,ims()),/SCHEMA_KEYS/)});
test("invalid marker schema denied",()=>{const m=marks();m.schema="human";assert.throws(()=>markers(pair(),m,ims()),/MARK_SCHEMA/)});
test("private write no overwrite",()=>tmp(d=>{const f=path.join(d,"a.json");writePrivate(f,{});assert.throws(()=>writePrivate(f,{}),/EEXIST/)}));
test("private write rejects Git ancestor",()=>tmp(d=>{fs.mkdirSync(path.join(d,".git"));assert.throws(()=>writePrivate(path.join(d,"output"),{}),/OUTPUT_IN_GIT/)}));
test("private write requires absolute path",()=>assert.throws(()=>writePrivate("rel",{}),/OUTPUT_ABSOLUTE/));
test("immutable W13 exact original blob pin",()=>assert.equal(W13_BLOB,"94c621f54fffd7e1325700b88b441652755c0feb"));
test("selected #510 exact source SHA pin",()=>assert.equal(SELECTED.slice(5),"d94400d95d13db7f27e1533df05c9292d5380431"));
test("real original frozen W13 source and actual PNG receipt",async()=>{
 const upstream=process.env.B01_W13_ORIGINAL_SOURCE;assert.ok(upstream&&fs.existsSync(upstream),"ORIGINAL_W13_SOURCE_MISSING");
 const d=fs.mkdtempSync(path.join(os.tmpdir(),"b01-w14-int-"));
 try{
 const before=path.join(d,"before.png"),after=path.join(d,"after.png"),pf=path.join(d,"pair.json"),mf=path.join(d,"marks.json");
 fs.writeFileSync(before,image());fs.writeFileSync(after,image(9));fs.writeFileSync(pf,JSON.stringify(pair()));fs.writeFileSync(mf,JSON.stringify(marks()));
 const r=await receipt({pairFile:pf,marksFile:mf,beforeFile:before,afterFile:after,upstreamFile:upstream});
 assert.equal(r.visual_status,"NO_OBSERVED_VISUAL_DRIFT");assert.equal(r.markers.length,4);assert.equal(r.human_verified,0);assert.equal(r.native_unity,"NOT_RUN");assert.equal(r.save_id_verified,false);
 const wrong=path.join(d,"wrong.mjs");fs.writeFileSync(wrong,"export function evaluate(){}");
 await assert.rejects(()=>receipt({pairFile:pf,marksFile:mf,beforeFile:before,afterFile:after,upstreamFile:wrong}),/FROZEN_W13_DRIFT/);
 }finally{fs.rmSync(d,{recursive:true,force:true})}
});
test("real child-process CLI writes private physical result and denies overwrite",()=>{
 const upstream=process.env.B01_W13_ORIGINAL_SOURCE;assert.ok(upstream);
 tmp(d=>{
 const a=path.join(d,"a.png"),b=path.join(d,"b.png"),pf=path.join(d,"pair.json"),mf=path.join(d,"marks.json"),out=path.join(d,"out.json");
 fs.writeFileSync(a,image());fs.writeFileSync(b,image(9));fs.writeFileSync(pf,JSON.stringify(pair()));fs.writeFileSync(mf,JSON.stringify(marks()));
 const run=[fileURLToPath(new URL("./screenshot_witness.mjs",import.meta.url)),"--pair",pf,"--marks",mf,"--before",a,"--after",b,"--upstream",upstream,"--output",out];
 const one=spawnSync(process.execPath,run,{encoding:"utf8"});assert.equal(one.status,0,one.stderr);assert.equal(JSON.parse(fs.readFileSync(out)).images.length,2);
 const two=spawnSync(process.execPath,run,{encoding:"utf8"});assert.equal(two.status,2);assert.match(two.stderr,/EEXIST/);
 });
});
