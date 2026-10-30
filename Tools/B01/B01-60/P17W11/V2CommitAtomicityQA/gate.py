#!/usr/bin/env python3
"""Exact original Git source method re-execution with genuine W06 C# ledger, QA-only physics state."""
from __future__ import annotations
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys

ROOT=Path(__file__).resolve().parents[5]
HERE=Path(__file__).resolve().parent
GEN=HERE/"Generated"
OUT=ROOT/"qa-w11-v2-commit-evidence"
OWNER="33a32b8bd9ab40467ab1a17b5684926a37dbd4d3"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
V2="Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs"
POL="Assets/B0123SecureReattachV2/Runtime/SecureDockPolicy.cs"
CORE="Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs"
EMITTER="Assets/B0123DebrisRepulse001/Runtime/DebrisRepulsionEmitter.cs"
PIN={V2:"491c89a18827bb027854bba5e6be6630ce4947b6",
     POL:"cc6611343d585fd0f16ff048f8ecdb0e7b1be30c",
     CORE:"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
     EMITTER:"2b3a8465c550bd49a5c98be5a8064a4ce0947f4b"}
QA="Tools/B01/B01-60/P17W11/V2CommitAtomicityQA/"
OWN={QA+p for p in ["Harness.cs","OriginalCommitQa.csproj","gate.py","README.md","manifest.json"]}|{".github/workflows/b01-60-p17w11-v2-commit-atomicity.yml"}
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def expect(cond,message):
 if not cond:raise AssertionError(message)
def original(path):
 value=git("rev-parse",OWNER+":"+path).decode().strip()
 expect(value==PIN[path],"Frozen original git blob drift for "+path)
 expect(git("rev-parse","HEAD:"+path).decode().strip()==value,
        "QA changed producer source "+path)
 return git("show",OWNER+":"+path)
def extract(method,source):
 needle="private bool "+method+"(int index)"
 pos=source.index(needle)
 start=source.index("{",pos)
 depth=0;end=None
 for i in range(start,len(source)):
  if source[i]=="{":depth+=1
  elif source[i]=="}":
   depth-=1
   if depth==0:end=i+1;break
 expect(end is not None,"source method braces incomplete")
 fragment=source[pos:end]
 expect("rig.Body.linearVelocity=Vector3.zero" in fragment,
        "actual original body not found")
 expect("rig.Detached=false" in fragment,"actual original state mutator not found")
 expect("rig.Emitter.RearmAfterReattach(rig.Spec.PartId)" in fragment,
        "actual original ledger rearm call not found")
 expect(fragment.index("rig.Body.linearVelocity") <
        fragment.index("rig.Emitter.RearmAfterReattach"),"source no longer matches vulnerable order")
 expect(fragment.index("rig.Detached=false") <
        fragment.index("rig.Emitter.RearmAfterReattach"),"source no longer matches vulnerable detached flag")
 return fragment
def main():
 upstream=git("diff","--name-only",W06,OWNER).decode().splitlines()
 expect(len(upstream)==27 and all(x.startswith("Assets/B0123SecureReattachV2/") or
      x==".github/workflows/b01-23-p17w10-secure-reattach-v2.yml" or
      x.startswith("Tools/B01/B01-23/P17W10SecureReattachV2/") or
      x=="Assets/B0123SecureReattachV2.meta"
      for x in upstream),"producer W10 source scope drift")
 changes=git("diff","--name-only",OWNER,"HEAD").decode().splitlines()
 expect(set(changes)==OWN, "QA changes collided with producer "+repr(changes))
 expect(git("merge-base",OWNER,"HEAD").decode().strip()==OWNER,"QA parent not original owner")
 expect(git("rev-parse",W06+":"+CORE).decode().strip()==PIN[CORE],
        "W06 immutable upstream changed")
 raw={x:original(x) for x in PIN}
 source=raw[V2].decode("utf-8")
 fragment=extract("CommitDock",source)
 expect(source.count("public bool Reattach(int index) => DockGate().Reattach(index);")==1,
        "legacy API not routed through original gate")
 expect(source.count("public bool TrySafeReattach(int index) => DockGate().TrySafeReattach(index);")==1,
        "safe public API not routed")
 expect(source.count("public bool ReattachFromGui(int index) => DockGate().ReattachFromGui(index);")==1,
        "GUI API not routed")
 # Deliberate source extraction: compile exact original C# method body, not a paraphrase
 # or a separately modeled method, while fake Rigidbody/Transform only replace Unity APIs.
 GEN.mkdir(exist_ok=True)
 (GEN/"OriginalCommit.g.cs").write_text(
     "using System;\nnamespace B01.SecureReattachV2\n{\n"+
     "    internal sealed partial class TransactionHost\n    {\n"+
     fragment+"\n    }\n}\n",encoding="utf8")
 (GEN/"OriginalDebrisImpulseCore.cs").write_bytes(raw[CORE])
 (GEN/"OriginalSecureDockPolicy.cs").write_bytes(raw[POL])
 OUT.mkdir(exist_ok=True)
 meta={"producer":"B01-23","source_pr":488,"original_sha":OWNER,
       "qa_scope_paths":len(changes),"original_git_objects":PIN,
       "actual_compiled_producer_commit_method_fragment_sha256":hashlib.sha256(fragment.encode()).hexdigest(),
       "method_byte_length":len(fragment.encode()),
       "reference":"actual original W06 C#9 ledger and original V2 C#9 GuardedDockCommand",
       "physics_boundary":"INDEPENDENT_QA_STUB_NOT_NATIVE_UNITY",
       "source_verdict":"EXPECTED_TRANSACTION_RED","native_unity":"NOT_RUN",
       "codex_ack":"PENDING"}
 (OUT/"provenance.json").write_text(json.dumps(meta,indent=2,sort_keys=True)+"\n",encoding="utf8")
 try:
  build=subprocess.run(["dotnet","build",str(HERE/"OriginalCommitQa.csproj"),"-c","Release","--warnaserror"],
        cwd=ROOT,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
  (OUT/"build.log").write_text("EXIT="+str(build.returncode)+"\n"+build.stdout,encoding="utf8")
  expect(build.returncode==0,"actual original C# compilation failed "+build.stdout[-1500:])
  print("B01_60_W11_DIRECT_ORIGINAL_CSHARP9_BUILD=PASS")
  run=subprocess.run(["dotnet","run","--no-build","-c","Release",
          "--project",str(HERE/"OriginalCommitQa.csproj")],
          cwd=ROOT,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
  (OUT/"runtime.log").write_text("EXIT="+str(run.returncode)+"\n"+run.stdout,encoding="utf8")
  for line in run.stdout.splitlines():
   if "B01_60_W11_" in line or line.startswith("PASS") or line.startswith("FAIL"):
    print(line)
  expect(run.returncode==0,"real original C# method expected RED not reproduced")
  expect("B01_60_W11_GATE=EXPECTED_SOURCE_TRANSACTION_RED_REPRODUCED" in run.stdout,
         "missing negative source evidence")
  print("B01_60_W11_EXACT_SHA="+OWNER+" QA_G0_EXPECTED_TRANSACTION_RED_REPRODUCED")
  print("B01_60_W11_NATIVE_UNITY=NOT_RUN G2=HOLD CODEX_ACK=PENDING")
 finally:
  shutil.rmtree(GEN,ignore_errors=True)
 return 0
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as exc:
  print("B01_60_W11_GATE_FAIL",repr(exc),file=sys.stderr)
  sys.exit(2)
