#!/usr/bin/env python3
"""W15: authenticate saved 121-pose sweep evidence by recomputing REAL GLB vertices.

Checks for forged sweeps even when original GLB SHA256 remains valid. This is
not native Unity, continuous triangle collision, or a BuildSocket approval.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

SOURCE=Path(__file__).resolve().parent
FILES={
 "M01-018":"M01-018_LEG-WPN-001_R1.glb",
 "M04-024":"M04-024_LEG-MOV-001_R1.glb"
}
FIELDS=("source_sha256","meshCount","samplesPerClip","animations")

def load_sampler():
 spec=importlib.util.spec_from_file_location("b01_w15_real_glb_sampler",SOURCE/"swept_geometry_qa.py")
 obj=importlib.util.module_from_spec(spec)
 spec.loader.exec_module(obj)
 return obj

def first_difference(claim,actual,path="$"):
 if type(claim) is not type(actual):return path+":type"
 if isinstance(actual,dict):
  if claim.keys()!=actual.keys():return path+":keys"
  for k in actual:
   bad=first_difference(claim[k],actual[k],path+"."+k)
   if bad:return bad
 elif isinstance(actual,list):
  if len(claim)!=len(actual):return path+":length"
  for i,(a,b) in enumerate(zip(claim,actual)):
   bad=first_difference(a,b,path+"["+str(i)+"]")
   if bad:return bad
 elif claim!=actual:return path+":value"
 return None

def validate_receipt(recomputed,claimed,model):
 if not isinstance(claimed,dict) or not isinstance(recomputed,dict):
  raise ValueError("invalid sweep receipt object")
 for k in FIELDS:
  if k not in claimed or k not in recomputed:
   raise ValueError("missing authenticated sweep field "+k)
  bad=first_difference(claimed[k],recomputed[k],"$."+k)
  if bad:raise ValueError("SWEEP_RECEIPT_FORGED "+model+" "+bad)
 if claimed.get("disclaimer")!=recomputed.get("disclaimer"):
  raise ValueError("mismatched evidence disclaimer")
 return {"modelId":model,"sourceSha256":recomputed["source_sha256"],
   "meshCount":recomputed["meshCount"],"clipsAuthenticated":len(recomputed["animations"]),
   "poseTimesPerClip":recomputed["samplesPerClip"],"status":"SOURCE_RECOMPUTED_EQUAL",
   "unityNative":"NOT_RUN","physxCollision":"NOT_RUN"}

def audit_package(package,samples=121):
 package=Path(package)
 manifest=json.loads((package/"manifest.json").read_text("utf8"))
 if len(manifest.get("binaries",[]))!=len(FILES):
  raise ValueError("unexpected model count in source manifest")
 sampler=load_sampler()
 rows=[]
 for model,name in FILES.items():
  matches=[entry for entry in manifest["binaries"] if entry.get("modelId")==model]
  if len(matches)!=1:raise ValueError("duplicate or absent model "+model)
  f=package/"models"/name
  sha=hashlib.sha256(f.read_bytes()).hexdigest()
  if sha!=matches[0]["reworkedSha256"]:
   raise ValueError("GLB SHA256 mismatch "+model)
  claimed=json.loads((package/"qa"/(model+"_SWEEP.json")).read_text("utf8"))
  if claimed.get("samplesPerClip")!=samples:
   raise ValueError("sample policy mismatch "+model)
  actual=sampler.inspect(f,samples=samples)
  rows.append(validate_receipt(actual,claimed,model))
 return {"schema":"B01-VIS-W15-GLB-SWEEP-PROVENANCE-1",
  "status":"BOTH_SOURCE_REPORTS_AUTHENTICATED","models":rows,
  "mountApproved":False,"nativeUnity":"NOT_RUN","continuousTriangleCollision":"NOT_RUN"}

def main():
 ap=argparse.ArgumentParser()
 ap.add_argument("--package",required=True,type=Path)
 ap.add_argument("--output",required=True,type=Path)
 args=ap.parse_args()
 receipt=audit_package(args.package)
 args.output.parent.mkdir(parents=True,exist_ok=True)
 args.output.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n",encoding="utf8")
 print(json.dumps(receipt,sort_keys=True))

if __name__=="__main__":main()
