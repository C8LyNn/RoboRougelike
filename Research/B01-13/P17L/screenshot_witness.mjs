#!/usr/bin/env node
import fs from "node:fs";
import path from "node:path";
import zlib from "node:zlib";
import crypto from "node:crypto";
import {pathToFileURL} from "node:url";
export const W13_COMMIT="aee4315304ca3e2fe3e155242d7d181260089efc";
export const W13_BLOB="94c621f54fffd7e1325700b88b441652755c0feb";
export const SELECTED="#510@d94400d95d13db7f27e1533df05c9292d5380431";
const demand=(ok,msg)=>{if(!ok)throw Error(msg)};
const exact=(o,keys)=>demand(o&&typeof o==="object"&&!Array.isArray(o)&&Object.keys(o).sort().join("|")===keys.slice().sort().join("|"),"SCHEMA_KEYS");
const hash=x=>crypto.createHash("sha256").update(x).digest("hex");
const table=Array.from({length:256},(_,n)=>{let v=n;for(let i=0;i<8;i++)v=v&1?(v>>>1)^0xedb88320:v>>>1;return v>>>0});
export function crc32(b){let c=0xffffffff;for(const v of b)c=table[(c^v)&255]^(c>>>8);return(c^0xffffffff)>>>0}
function privateBytes(f,limit){
 demand(path.isAbsolute(f),"ABSOLUTE_PATH");const st=fs.lstatSync(f);
 demand(st.isFile()&&!st.isSymbolicLink()&&st.size>0&&st.size<=limit,"FILE_SIZE_TYPE");
 for(let d=fs.realpathSync(path.dirname(f));;d=path.dirname(d)){demand(!fs.existsSync(path.join(d,".git")),"PRIVATE_PATH_IN_GIT");if(path.dirname(d)===d)break}
 return fs.readFileSync(f);
}
export function png(b){
 const signature=Buffer.from([137,80,78,71,13,10,26,10]);
 demand(b.length>=57&&b.subarray(0,8).equals(signature),"PNG_SIGNATURE");
 let at=8,w=0,h=0,bpp=0,hadIdat=false,ended=false,first=true,parts=[];
 while(at<b.length){
  demand(at+12<=b.length,"PNG_TRUNCATED");const len=b.readUInt32BE(at);at+=4;
  demand(len<=24*1024*1024&&at+len+8<=b.length,"PNG_LENGTH");
  const name=b.toString("ascii",at,at+4);at+=4;const data=b.subarray(at,at+len);at+=len;
  const actual=b.readUInt32BE(at);at+=4;
  demand(crc32(b.subarray(at-len-8,at-4))===actual,"PNG_CRC");
  if(first){
   demand(name==="IHDR"&&len===13,"PNG_IHDR");first=false;
   w=data.readUInt32BE(0);h=data.readUInt32BE(4);
   demand(w>0&&h>0&&w<=8192&&h<=8192&&w*h<=16000000,"PNG_SIZE");
   demand(data[8]===8&&(data[9]===2||data[9]===6)&&data[10]===0&&data[11]===0&&data[12]===0,"PNG_FORMAT");
   bpp=data[9]===2?3:4;
  }else if(name==="IDAT"){demand(!ended,"PNG_ORDER");hadIdat=true;parts.push(data)}
  else if(name==="IEND"){demand(len===0&&hadIdat&&at===b.length,"PNG_END");ended=true;break}
  else demand(!hadIdat&&["sRGB","gAMA","pHYs"].includes(name),"PNG_CHUNK_UNSUPPORTED");
 }
 demand(ended,"PNG_NO_END");
 const bytes=h*(1+w*bpp);let raw;
 try{raw=zlib.inflateSync(Buffer.concat(parts),{maxOutputLength:bytes+1})}catch{throw Error("PNG_DEFLATE")}
 demand(raw.length===bytes,"PNG_UNCOMPRESSED_LENGTH");
 for(let y=0;y<h;y++)demand(raw[y*(1+w*bpp)]<=4,"PNG_FILTER");
 return {width:w,height:h,sha256:hash(b)};
}
export function markers(pair,mark,images){
 exact(mark,["schema","observations"]);
 demand(mark.schema==="B01-13-PNG-RELOAD-MARKERS/R1"&&Array.isArray(mark.observations)&&mark.observations.length<=160,"MARK_SCHEMA");
 const expected=new Set(pair.snapshots.flatMap(s=>s.modules.map(m=>s.stage+"/"+m.visual_token))),seen=new Set(),out=[];
 for(const v of mark.observations){
  exact(v,["stage","visual_token","x","y"]);
  const key=v.stage+"/"+v.visual_token;
  demand(expected.has(key)&&!seen.has(key),"MARK_UNKNOWN_OR_DUPLICATE");
  const image=images[v.stage];demand(image&&Number.isInteger(v.x)&&Number.isInteger(v.y)&&v.x>=0&&v.y>=0&&v.x<image.width&&v.y<image.height,"MARK_BOUNDS");
  seen.add(key);out.push(v);
 }
 demand(seen.size===expected.size,"MARK_COVERAGE");
 return out.sort((a,b)=>(a.stage+a.visual_token).localeCompare(b.stage+b.visual_token));
}
export async function receipt({pairFile,marksFile,beforeFile,afterFile,upstreamFile}){
 const source=fs.readFileSync(upstreamFile);
 const blob=crypto.createHash("sha1").update("blob "+source.length+"\0").update(source).digest("hex");
 demand(blob===W13_BLOB,"FROZEN_W13_DRIFT");
 const {evaluate}=await import(pathToFileURL(fs.realpathSync(upstreamFile)).href);
 demand(typeof evaluate==="function","UPSTREAM_MISSING");
 const pair=JSON.parse(privateBytes(pairFile,131072)),check=evaluate(pair);
 demand(check.selected_sha===SELECTED.slice(5),"SELECTED_SHA");
 const im={BEFORE_SAVE:png(privateBytes(beforeFile,24*1024*1024)),AFTER_LOAD:png(privateBytes(afterFile,24*1024*1024))};
 const ms=markers(pair,JSON.parse(privateBytes(marksFile,131072)),im);
 return {schema:"B01-13-PNG-RELOAD-WITNESS/R1",w13_commit:W13_COMMIT,w13_blob:W13_BLOB,selected:SELECTED,player_exe_sha256:pair.player_exe_sha256,session:pair.session,visual_status:check.status,visual_diff:check.diff,images:[{stage:"BEFORE_SAVE",...im.BEFORE_SAVE},{stage:"AFTER_LOAD",...im.AFTER_LOAD}],markers:ms,same_image_sha:im.BEFORE_SAVE.sha256===im.AFTER_LOAD.sha256,evidence:"OPERATOR_ANNOTATED_PNG_BYTES_NOT_AUTHENTICATED",source_to_player:"UNVERIFIED",human_verified:0,native_unity:"NOT_RUN",save_id_verified:false};
}
export function writePrivate(file,data){
 demand(path.isAbsolute(file),"OUTPUT_ABSOLUTE");
 for(let d=fs.realpathSync(path.dirname(file));;d=path.dirname(d)){demand(!fs.existsSync(path.join(d,".git")),"OUTPUT_IN_GIT");if(path.dirname(d)===d)break}
 const fd=fs.openSync(file,fs.constants.O_CREAT|fs.constants.O_EXCL|fs.constants.O_WRONLY|(fs.constants.O_NOFOLLOW||0),0o600);
 try{fs.writeFileSync(fd,JSON.stringify(data,null,2)+"\n");fs.fsyncSync(fd)}finally{fs.closeSync(fd)}
}
export async function cli(args){
 demand(args.length===12,"ARG_COUNT");const a={};
 for(let i=0;i<args.length;i+=2){demand(args[i].startsWith("--")&&!Object.hasOwn(a,args[i].slice(2)),"ARGS");a[args[i].slice(2)]=args[i+1]}
 exact(a,["pair","marks","before","after","upstream","output"]);
 const out=await receipt({pairFile:a.pair,marksFile:a.marks,beforeFile:a.before,afterFile:a.after,upstreamFile:a.upstream});
 writePrivate(a.output,out);return {saved:a.output,status:out.visual_status};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
 try{console.log(JSON.stringify(await cli(process.argv.slice(2))))}catch(e){console.error("FAIL_CLOSED",String(e.message||e));process.exitCode=2}
}
