#!/usr/bin/env python3
"""B01-60 independent, fail-closed verification of ORIGINAL native evidence."""
import argparse, hashlib, json, struct, zlib, xml.etree.ElementTree as ET
from pathlib import Path
STAGES=("Compile","EditMode","PlayMode","Player720","Player1080")
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def js(p): return json.loads(Path(p).read_text(encoding="utf-8-sig"))
def nunit(path,expected):
 try:
  r=ET.parse(path).getroot()
  cases=list(r.iter("test-case"))
  observed={"total":len(cases),"passed":sum(x.get("result")=="Passed" for x in cases),
   "failed":sum(x.get("result")=="Failed" for x in cases),"skipped":sum(x.get("result")=="Skipped" for x in cases)}
  return (r.tag=="test-run" and all(int(r.get(k,"-1"))==v for k,v in observed.items())
   and len(cases)>0 and observed["passed"]==observed["total"] and r.get("result")=="Passed"
   and all(sum(c.get("name")==name for c in cases)==1 for name in expected))
 except (ET.ParseError,ValueError,OSError): return False
def png(p,w,h):
 try:
  b=Path(p).read_bytes()
  if b[:8]!=b"\x89PNG\r\n\x1a\n": return False
  offset=8; data=[]; dims=None; end=False
  while offset+12<=len(b):
   n=struct.unpack_from(">I",b,offset)[0]
   if n>100000000 or offset+12+n>len(b): return False
   t=b[offset+4:offset+8]; d=b[offset+8:offset+8+n]
   if zlib.crc32(t+d)&0xffffffff !=struct.unpack_from(">I",b,offset+8+n)[0]: return False
   if t==b"IHDR":
    if n!=13: return False
    x,y,bits,color,c,filt,interlace=struct.unpack(">IIBBBBB",d)
    if (x,y)!=(w,h) or bits!=8 or color not in (2,6) or c or filt or interlace:return False
    dims=(x,y,3 if color==2 else 4)
   if t==b"IDAT":data.append(d)
   if t==b"IEND":end=n==0 and offset+12==len(b);break
   offset+=12+n
  if not dims or not data or not end:return False
  raw=zlib.decompress(b"".join(data))
  return len(raw)==(dims[0]*dims[2]+1)*dims[1]
 except (OSError,ValueError,zlib.error,struct.error):return False
def check(plan,root):
 out={"schema":"b01-60-qa-verdict-v1","source_sha":plan["source_sha"],"target_sha":plan["target_sha"],
 "attribution":"B01-60_TOOL_CHECK_NOT_NATIVE_EXECUTION","g2":"HOLD","manual":"NOT_RUN","stages":[]}
 for name in STAGES:
  stage=Path(root)/name;errors=[];result={"stage":name,"status":"NOT_RUN","problems":[]}
  proc=stage/"process.json"
  if proc.exists():
   try:
    p=js(proc);code=p.get("exit_code")
    if p.get("source_sha")!=plan["source_sha"] or p.get("target_sha")!=plan["target_sha"]:errors.append("STALE_SHA")
    if p.get("unity_version")!="6000.3.14f1":errors.append("WRONG_UNITY")
    if type(code)!=int:errors.append("NO_NATIVE_EXIT")
    log=stage/("Player.log" if name.startswith("Player") else "Editor.log")
    if not log.is_file() or not log.stat().st_size:errors.append("NO_LOG")
    if name in ("EditMode","PlayMode") and not nunit(stage/"results.xml",plan.get("required_nunit",{}).get(name,[])):errors.append("INVALID_OR_INCOMPLETE_NUNIT_XML")
    if name.startswith("Player"):
     w,h=(1280,720) if name=="Player720" else (1920,1080)
     names=plan.get("screenshots",{}).get(name,[])
     if not names or any("/" in s or "\\" in s or ".." in s or not png(stage/s,w,h) for s in names):errors.append("INVALID_OR_MISSING_SCREENSHOTS")
     tele=stage/"telemetry.json"
     if not tele.exists():errors.append("MISSING_TELEMETRY")
     else:
      t=js(tele);rows=t.get("cases",[]);ids=[r.get("id") for r in rows]
      if t.get("source_sha")!=plan["source_sha"] or len(ids)!=len(set(ids)):errors.append("STALE_OR_DUPLICATE_CASES")
      for case in plan.get("required_functional",{}).get(name,[]):
       rr=next((r for r in rows if r.get("id")==case),None)
       if not rr or rr.get("status")!="PASS":errors.append("MISSING_OR_FAILED_CASE:"+case)
       elif case.startswith("CAM-") and (rr.get("actualOwnRayHits")!=0 or rr.get("worldPenetrationCount")!=0 or not isinstance(rr.get("maxContinuousSpeedMps"),(int,float)) or rr["maxContinuousSpeedMps"]>15):errors.append("CAMERA_CONTRACT_FAIL:"+case)
       elif case.startswith("R6-") and (rr.get("finishReached") is not True or not isinstance(rr.get("wheelContactSamples"),int) or rr["wheelContactSamples"]<=0):errors.append("R6_CONTACT_OR_FINISH_FAIL:"+case)
    result.update(status="BLOCKED" if type(code)!=int else ("FAIL" if code!=0 or errors else "PASS"),exit_code=code)
   except (ValueError,TypeError,KeyError,ET.ParseError,OSError) as e:errors.append("INVALID_EVIDENCE:"+type(e).__name__);result["status"]="FAIL"
  result["problems"]=errors
  out["stages"].append(result)
 required=[r for r in out["stages"] if r["stage"] in plan.get("required_stages",STAGES)]
 out["status"]="FAIL" if any(r["status"]=="FAIL" for r in required) else ("BLOCKED" if any(r["status"]=="BLOCKED" for r in required) else ("NOT_RUN" if any(r["status"]=="NOT_RUN" for r in required) else "PASS"))
 return out
def main():
 p=argparse.ArgumentParser();p.add_argument("--plan",required=True);p.add_argument("--evidence",required=True);p.add_argument("--out");a=p.parse_args()
 plan=js(a.plan)
 if plan.get("schema")!="b01-60-native-plan-v1" or not plan.get("source_files"):raise SystemExit("BLOCKED: source file pin manifest required")
 if len(plan.get("source_sha",""))!=40 or len(plan.get("target_sha",""))!=40:raise SystemExit("BLOCKED: SHA pin malformed")
 result=check(plan,a.evidence)
 if a.out:Path(a.out).write_text(json.dumps(result,sort_keys=True,indent=2)+"\n")
 print(json.dumps(result,sort_keys=True))
 return 0 if result["status"]=="PASS" else 2 if result["status"]=="FAIL" else 3
if __name__=="__main__":raise SystemExit(main())
