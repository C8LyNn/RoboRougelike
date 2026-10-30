#!/usr/bin/env python3
"""Pin original immutable W06 and W10 V2 Unity host blobs; source-only QA scope."""
import pathlib
import subprocess

ROOT=pathlib.Path(__file__).resolve().parents[4]
PRODUCER="33a32b8bd9ab40467ab1a17b5684926a37dbd4d3"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
SOURCES={
 "Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs":"491c89a18827bb027854bba5e6be6630ce4947b6",
 "Assets/B0123SecureReattachV2/Runtime/SecureDockPolicy.cs":"cc6611343d585fd0f16ff048f8ecdb0e7b1be30c",
 "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"
}
QA="Tools/B01/B01-61/P17W11RealV2LedgerAtomicity/"
EXPECTED={QA+x for x in ["Program.cs","UnityBoundaryAndRealLedgerAdapter.cs",
 "OriginalHostLedgerQA.csproj","verify_source.py","README.md","manifest.json"]}
EXPECTED.add(".github/workflows/b01-61-p17w11-real-v2-ledger-atomicity.yml")
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],text=True).strip()
def guard(ok,message):
 if not ok:raise AssertionError(message)
def method(code,signature):
 start=code.find(signature);guard(start>=0,"method missing "+signature)
 op=code.find("{",start);guard(op>=0,"no brace")
 depth=0
 for i in range(op,len(code)):
  if code[i]=="{":depth+=1
  if code[i]=="}":
   depth-=1
   if depth==0:return code[op+1:i]
 raise AssertionError("no closing brace")
def main():
 files=set(filter(None,git("diff","--name-only",PRODUCER,"HEAD").splitlines()))
 guard(files==EXPECTED,"QA_SCOPE_MISMATCH "+str(sorted(files^EXPECTED)))
 for path,hash in SOURCES.items():
  ref=W06 if "DebrisImpulseCore" in path else PRODUCER
  guard(git("rev-parse",ref+":"+path)==hash,"ORIGINAL_BLOB_PIN "+path)
  guard(git("rev-parse","HEAD:"+path)==hash,"SOURCE_MUTATION_IN_QA "+path)
  guard(git("hash-object",path)==hash,"WORKTREE_SOURCE_CHANGE "+path)
 host=(ROOT/"Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs").read_text()
 ledger=(ROOT/"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs").read_text()
 commit=method(host,"private bool CommitDock(int index)")
 guard("RearmAfterReattach" in commit and
       "rig.Body.isKinematic=true" in commit and
       "rig.Detached=false" in commit,"ORIGINAL_COMMIT_PATTERN_CHANGED")
 marker=commit.index("rig.Emitter.RearmAfterReattach")
 for mutation in ("rig.Body.linearVelocity=Vector3.zero","rig.Body.isKinematic=true",
                  "rig.Part.transform.SetParent(","rig.Detached=false"):
  guard(mutation in commit,"EXPECTED_MUTATION_ABSENT "+mutation)
  guard(commit.index(mutation)<marker,"EXPECTED_FAILING_PRE_REARM_ORDER_CHANGED "+mutation)
 guard("public void ClearForNewRun() => seen.Clear();" in ledger and
       "public bool RearmAfterReattach(long id) => id > 0 && seen.Remove(id);" in ledger,
       "ORIGINAL_W06_LEDGER_RESET_OR_REARM_CHANGED")
 for alias in ("public bool Reattach(int index) => DockGate().Reattach(index);",
               "public bool TrySafeReattach(int index) => DockGate().TrySafeReattach(index);",
               "public bool ReattachFromGui(int index) => DockGate().ReattachFromGui(index);"):
  guard(alias in host,"V2_ROUTE_DRIFT "+alias)
 print("PASS original immutable Git blobs 4/4, original W06 ledger/reset, V2 all aliases, QA-only paths 7/7")
 print("ORIGINAL_SOURCE_RED: real V2 CommitDock changes Rigidbody/parent/Detached BEFORE original W06 ID rearm")
 print("EXACT_SOURCE_QA_PROVENANCE_PASS; NATIVE_UNITY_NOT_RUN")
if __name__=="__main__":main()
