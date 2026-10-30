#!/usr/bin/env python3
"""Frozen B01-23 W10 original-source provenance and public alias contract, no code changes."""
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
PRODUCER = "59479716140b0e2682f5a979e210e0f2709d4a31"
DIR = "Tools/B01/B01-61/P17W10V2PublicApiAcceptance/"
SRC = {
  "Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs":
    "491c89a18827bb027854bba5e6be6630ce4947b6",
  "Assets/B0123SecureReattachV2/Runtime/SecureDockPolicy.cs":
    "cc6611343d585fd0f16ff048f8ecdb0e7b1be30c",
  "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":
    "b680ed0ec969ac23203fa55eab7b90f8daa86e43",
}
OWN = {DIR+n for n in ("Program.cs","OriginalSourceQA.csproj","verify_source.py",
                         "README.md","manifest.json")} | {
  ".github/workflows/b01-61-p17w10-v2-public-api.yml"
}
def git(*args):
    return subprocess.check_output(["git","-C",str(ROOT),*args],text=True).strip()
def require(condition,context):
    if not condition: raise AssertionError(context)
def method(src, signature):
    start=src.find(signature)
    require(start>=0,"Missing method "+signature)
    brace=src.find("{",start)
    require(brace>=0,"Missing method opening "+signature)
    depth=0
    for i in range(brace,len(src)):
        if src[i]=="{":depth+=1
        elif src[i]=="}":
            depth-=1
            if depth==0:return src[brace+1:i]
    raise AssertionError("Unterminated "+signature)
def contract(host,policy):
    for old,guarded in [
      ("public bool Reattach(int index) => DockGate().Reattach(index);",True),
      ("public bool TrySafeReattach(int index) => DockGate().TrySafeReattach(index);",True),
      ("public bool ReattachFromGui(int index) => DockGate().ReattachFromGui(index);",True),
    ]:
        if guarded:require(old in host,"PUBLIC_GUARD_BYPASS: "+old)
    require("private bool CommitDock(int index)" in host,"COMMIT_VISIBILITY_REGRESSION")
    require("public bool CommitDock(int index)" not in host,"UNGUARDED_PUBLIC_COMMIT")
    body=method(host,"private bool CommitDock(int index)")
    require("RearmAfterReattach" in body and "rig.Detached=false" in body,
            "PHYSICAL_COMMIT_TARGET_DRIFT")
    gui=method(host,"private void OnGUI()")
    require("ReattachFromGui(i)" in gui and "CommitDock(i)" not in gui,
            "GUI_BYPASS")
    inspect=method(host,"private DockResult InspectDock(int index)")
    require("Physics.OverlapBoxNonAlloc" in inspect and
            "SecureDockPolicy.AllLayers,QueryTriggerInteraction.Ignore" in inspect and
            "Physics.ComputePenetration" in inspect and
            "SecureDockPolicy.Check" in inspect,"PHYSICS_CLEARANCE_BYPASS")
    execute=method(policy,"private bool Execute(int index)")
    require("executing = true;" in execute and
            "LastResult = inspect(index)" in execute and
            "LastResult == DockResult.Allowed && commit(index)" in execute and
            execute.index("LastResult = inspect(index)") < execute.index("commit(index)") and
            "finally { executing = false; }" in execute,
            "GUARD_COMMAND_BYPASS")
    require("private readonly Func<int,bool> commit;" in policy, "COMMIT_DELEGATE_DRIFT")
    require("public const int AllLayers = -1" in policy,"ALL_LAYER_GUARD_REMOVED")
def main():
    assert set(filter(None,git("diff","--name-only",PRODUCER,"HEAD").splitlines()))==OWN,\
        "QA_SCOPE_VIOLATION"
    for path,blob in SRC.items():
        assert git("rev-parse",PRODUCER+":"+path)==blob,"PIN_ORIGINAL_SOURCE"
        assert git("rev-parse","HEAD:"+path)==blob,"ORIGINAL_SOURCE_EDITED"
        assert git("hash-object",path)==blob,"ORIGINAL_WORKTREE_DRIFT"
    host=(ROOT/list(SRC)[0]).read_text(encoding="utf-8")
    policy=(ROOT/list(SRC)[1]).read_text(encoding="utf-8")
    contract(host,policy)
    print("PASS exact original W10 producer Git blobs 3/3; exclusive QA paths 6/6")
    print("PASS all three ORIGINAL V2 public entrypoints route through guarded command; CommitDock private")
    print("PASS original collider audit all layers + actual penetration + source-only internal commit")
    mutations = [
      ("public bool Reattach(int index) => DockGate().Reattach(index);",
       "public bool Reattach(int index) => CommitDock(index);",True),
      ("public bool TrySafeReattach(int index) => DockGate().TrySafeReattach(index);",
       "public bool TrySafeReattach(int index) => CommitDock(index);",True),
      ("public bool ReattachFromGui(int index) => DockGate().ReattachFromGui(index);",
       "public bool ReattachFromGui(int index) => CommitDock(index);",True),
      ("private bool CommitDock(int index)","public bool CommitDock(int index)",True),
      ("if(GUILayout.Button(\"Reattach (GUARDED)\"))ReattachFromGui(i);",
       "if(GUILayout.Button(\"Reattach (GUARDED)\"))CommitDock(i);",True),
      ("SecureDockPolicy.AllLayers,QueryTriggerInteraction.Ignore",
       "0,QueryTriggerInteraction.Ignore",True),
    ]
    for before,after,is_host in mutations:
        source=host if is_host else policy
        assert before in source, "MUTANT_SOURCE_ANCHOR_MISSING "+before
        candidate=source.replace(before,after,1)
        try: contract(candidate if is_host else host,
                      policy if is_host else candidate)
        except AssertionError: continue
        raise AssertionError("MUTANT_SURVIVED: "+before)
    print("PASS 6/6 independent negative source-contract mutations rejected")
    print("QA_SOURCE_CONTRACT_PASS; NATIVE_UNITY_NOT_RUN; OLD_W07_REATTACH_REMAINS_UNSAFE")
if __name__=="__main__":
    main()
