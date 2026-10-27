#!/usr/bin/env python3
"""Download ORIGINAL Codex R3 + R4 C#; verify individual Git blob SHA-1 identity."""
import hashlib
import json
from pathlib import Path
from urllib.request import urlopen
PIN="d44fa09c227dffca1c1f8832383bbd80430bba90"
SOURCES={
 "Assets/R3/R3VehicleModel.cs":"6d8b1c01449f531970f2e284154ddfce911151a8",
 "Assets/R4/R4WeaponDefinition.cs":"f4044c053f5aebb055b8ba0df0026ea156246e44",
}
ROOT=Path(__file__).resolve().parent
def git_blob(raw):
 return hashlib.sha1(b"blob "+str(len(raw)).encode("ascii")+b"\0"+raw).hexdigest()
def main():
 receipt=[]
 for name,expected in SOURCES.items():
  with urlopen("https://raw.githubusercontent.com/C8LyNn/RoboRougelike/"+PIN+"/"+name,timeout=30) as response: raw=response.read()
  got=git_blob(raw)
  if got!=expected: raise ValueError("PINNED_ORIGINAL_GIT_BLOB_MISMATCH: "+name+" got "+got)
  path=ROOT/"_pinned"/name;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(raw)
  receipt.append({"path":name,"git_blob":got,"sha256":hashlib.sha256(raw).hexdigest(),"size":len(raw)})
 print("PINNED_REAL_CODEX_302_R3_R4_BLOBS_PASS",len(receipt),PIN)
 out=ROOT/"_pinned"/"real-source-receipt.json"
 out.write_text(json.dumps({"pin":PIN,"files":receipt,"unity_native":"NOT_RUN","codex_worktree_modified":False},indent=2)+"\n")
if __name__=="__main__":main()
