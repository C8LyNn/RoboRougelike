#!/usr/bin/env python3
"""Read-only proof of original W07 direct reattach bypass of W08 optional gate."""
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[4]
BASE = "99dfbd25d471f9983f9472b12d3725f4c002f7e9"
FILES = {
 "Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs":"e4e31c32cb5abda2fef987efa4b45fd61e828312",
 "Assets/B0123ReattachClearance001/Runtime/DebrisSafeReattachGate.cs":"7c7e96a10d69149119309863a0c2a9bdab829072",
 "Assets/B0123ReattachClearance001/Runtime/DockClearancePolicy.cs":"de0a847dbecacb92ad63a09ca12f5c9c372d597a",
 "Assets/B0123ReattachClearance001/Editor/SafeReattachDemoInstaller.cs":"5892f7e31d1e9fa07c124d63324c796b5abc282b",
}
D = "Tools/B01/B01-61/P17W09ReattachBypassQA/"
A = "Assets/B0161ReattachBypassQA/"
OWNED = set([
 D+"verify.py",D+"README.md",D+"manifest.json",
 ".github/workflows/b01-61-p17w09-reattach-bypass.yml",
 A.rstrip("/")+".meta",A+"Tests.meta",A+"Tests/PlayMode.meta",
 A+"Tests/PlayMode/B0161.ReattachBypass.PlayMode.asmdef",
 A+"Tests/PlayMode/B0161.ReattachBypass.PlayMode.asmdef.meta",
 A+"Tests/PlayMode/ReattachBypassPlayModeTests.cs",
 A+"Tests/PlayMode/ReattachBypassPlayModeTests.cs.meta"
])
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],text=True).strip()
def check(ok,message):
 if not ok: raise AssertionError(message)
def method(src, signature):
 start=src.find(signature); check(start>=0,"MISSING_METHOD "+signature)
 op=src.find("{",start); check(op>=0,"MISSING_OPEN_BRACE "+signature)
 depth=0
 for i in range(op,len(src)):
  if src[i]=="{": depth+=1
  elif src[i]=="}":
   depth-=1
   if depth==0: return src[op+1:i]
 raise AssertionError("UNCLOSED_METHOD "+signature)
def main():
 check(set(filter(None,git("diff","--name-only",BASE,"HEAD").splitlines()))==OWNED,
       "QA_FILE_SCOPE_CHANGED")
 for path,blob in FILES.items():
  check(git("rev-parse","HEAD:"+path)==blob,"ORIGINAL_SHA_DRIFT "+path)
  check(git("hash-object",path)==blob,"ORIGINAL_SOURCE_MUTATED "+path)
 print("PASS frozen original blobs 4/4; isolated QA paths 11/11")
 old=(ROOT/"Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs").read_text()
 safe=(ROOT/"Assets/B0123ReattachClearance001/Runtime/DebrisSafeReattachGate.cs").read_text()
 installer=(ROOT/"Assets/B0123ReattachClearance001/Editor/SafeReattachDemoInstaller.cs").read_text()
 m=method(old,"public bool Reattach(int index)")
 gui=method(old,"private void OnGUI()")
 s=method(safe,"public bool TrySafeReattach(DebrisRigPlayground demo, int index)")
 install=method(installer,"public static void Install()")
 check(all(x in m for x in ["rig.Body.isKinematic = true","SetParent(","RearmAfterReattach("]),
       "EXPECTED_PHYSICAL_MUTATION_MISSING")
 check("Inspect(demo, index) != DockVerdict.Allowed" in s and "demo.Reattach(index)" in s,
       "EXPECTED_W08_GATE_NOT_FOUND")
 check("Undo.AddComponent<DebrisSafeReattachGate>(controller)" in install,
       "EXPECTED_OPT_IN_INSTALLER_NOT_FOUND")
 unsafe=all(x not in m for x in ("Inspect(","TrySafeReattach(","DockVerdict","Clearance"))
 direct=('GUILayout.Button("Reattach")) Reattach(i)' in gui or
         bool(re.search(r'GUILayout\.Button\("Reattach"\)\)\s*Reattach\(i\)',gui)))
 check(unsafe and direct,"EXPECTED_KNOWN_BYPASS_NOT_FOUND: inspect producer revisions")
 print("SOURCE_BYPASS_REPRODUCED: W07 public Reattach changes physical state without clearance")
 print("SOURCE_BYPASS_REPRODUCED: W07 GUI calls raw Reattach, W08 gate remains opt-in")
 if len(sys.argv)==2 and sys.argv[1]=="--evidence":
  print("QA_EVIDENCE_PASS: actionable original-source blocker reproduced, Unity NOT_RUN")
  return 0
 if len(sys.argv)==2 and sys.argv[1]=="--release":
  print("RELEASE_GATE_RED: unsafe W07 path bypasses W08 clearance")
  return 3
 raise ValueError("Expected --evidence or --release")
if __name__=="__main__":
 try: sys.exit(main())
 except Exception as exc:
  print("QA_VERIFY_ERROR "+str(exc),file=sys.stderr)
  sys.exit(4)
