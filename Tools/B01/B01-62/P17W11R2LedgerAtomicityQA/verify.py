#!/usr/bin/env python3
"""B01-62 W11: independently compile ORIGINAL #494 W07 class with ORIGINAL W06 ledger,
using ONLY a test-specific Unity API shim. Never creates Unity-native evidence.
"""
import argparse
import hashlib
import json
import re
import subprocess
from pathlib import Path

PIN = {
 "r2": ("6efe45961ed0ffdf183e8016e350978b9f3dc51c",
        "Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs",
        "60fc22efec0e49d7688adde1fcb776093f2196af"),
 "layout": ("6efe45961ed0ffdf183e8016e350978b9f3dc51c",
        "Assets/B0123DebrisPlayground001/Runtime/RigDemoDefinition.cs",
        "b7d1a503c89537316fe48dbb54f5997bb21d14ce"),
 "ledger": ("cb2b3a76e84266e2730fffc4ab048c1039f0652f",
        "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",
        "fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
 "emitter": ("cb2b3a76e84266e2730fffc4ab048c1039f0652f",
        "Assets/B0123DebrisRepulse001/Runtime/DebrisRepulsionEmitter.cs",
        "2b3a8465c550bd49a5c98be5a8064a4ce0947f4b"),
 "shim": ("6efe45961ed0ffdf183e8016e350978b9f3dc51c",
        "Tools/B01/B01-23/P17W10PublicDockGuardR2/UnityBoundaryStub.cs",
        "d5fc14c1ad7d4ce730f03aef22a4272cb6dd7e43"),
 "v2": ("33a32b8bd9ab40467ab1a17b5684926a37dbd4d3",
        "Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs",
        "491c89a18827bb027854bba5e6be6630ce4947b6"),
}

class Drift(RuntimeError): pass

def git_blob(raw):
    return hashlib.sha1(b"blob " + str(len(raw)).encode() + b"\0" + raw).hexdigest()

def pull_sources(repo):
    sources={}
    for name,(commit,path,expected) in PIN.items():
        data=subprocess.check_output(["git","-C",str(repo),"show",commit+":"+path])
        actual=git_blob(data)
        ref=subprocess.check_output(["git","-C",str(repo),"rev-parse",commit+":"+path]).decode().strip()
        if actual!=expected or ref!=expected:
            raise Drift("FAIL_CLOSED_GIT_BLOB "+name+" actual="+actual)
        sources[name]=data.decode("utf-8-sig")
    return sources

def body(s, name):
    m=re.search(r"\b(?:public|private)\s+bool\s+"+re.escape(name)+r"\s*\([^{};]*\)\s*\{",s)
    if not m: raise Drift("MISSING_METHOD "+name)
    depth=0
    for i in range(m.end()-1,len(s)):
        if s[i]=="{": depth+=1
        if s[i]=="}":
            depth-=1
            if depth==0:return s[m.end():i]
    raise Drift("UNBALANCED_METHOD "+name)

def order_guard(source,name):
    fragment=body(source,name)
    actions=[
      "linearVelocity = Vector3.zero",
      "angularVelocity = Vector3.zero",
      "isKinematic = true",
      ".SetParent(",
      "localPosition = ",
      "Detached = false",
      "RearmAfterReattach("
    ]
    # R2 and V2 both must be evaluated without treating their source text as a PASS.
    compact=re.sub(r"\\s+","",fragment)
    locations=[compact.find(re.sub(r"\\s+","",x)) for x in actions]
    if min(locations)<0 or locations != sorted(locations):
        raise Drift("TRANSACTION_SHAPE_DRIFT_"+name)
    return {"mutations_before_ledger":6, "ledger_rearm_after_mutations":True}

def check(src):
    r2=body(src["r2"],"Reattach")
    v2=body(src["v2"],"CommitDock")
    if "if (!ValidatePhysicalReattach(rig)) return false;" not in r2:
        raise Drift("R2_PHYSICS_GATE_MISSING")
    if not re.search(r"\bpublic\s+bool\s+Reattach\(int index\)",src["r2"]):
        raise Drift("R2_PUBLIC_ROUTE_MISSING")
    if not all(t in src["emitter"] for t in ("public void ClearForNewRun()", "ledger.ClearForNewRun()", "public bool RearmAfterReattach")):
        raise Drift("FROZEN_W06_PUBLIC_LEDGER_API_DRIFT")
    if not all(t in src["ledger"] for t in ("seen.Clear()", "seen.Remove(id)", "public bool Contains(long id)")):
        raise Drift("FROZEN_LEDGER_SEMANTICS_DRIFT")
    if not all(t in src["v2"] for t in ("public bool Reattach(int index) => DockGate().Reattach(index)", "private bool CommitDock(int index)")):
        raise Drift("V2_ROUTE_CONTRACT_DRIFT")
    r2shape=order_guard(src["r2"],"Reattach")
    v2shape=order_guard(src["v2"],"CommitDock")
    if "namespace B01.DebrisRepulse001" not in src["shim"]:
        raise Drift("SHIM_EMITTER_SPLIT_MARKER_DRIFT")
    return {"r2_public_physics_gate_present":True,
            "r2_commit_order":r2shape,
            "v2_commit_order":v2shape,
            "w06_ledger_has_external_clear":True,
            "data_integrity_verdict":"BOTH_CANDIDATES_TRANSACTION_HOLD",
            "unity_native":"NOT_RUN"}

def materialize(repo, dest):
    dest=Path(dest)
    if dest.exists():raise Drift("REFUSE_NONEMPTY_RECEIPT_DIR")
    src=pull_sources(repo)
    result=check(src)
    dest.mkdir(parents=True)
    inp=dest/"Input";inp.mkdir()
    (inp/"DebrisRigPlayground.cs").write_text(src["r2"])
    (inp/"RigDemoDefinition.cs").write_text(src["layout"])
    (inp/"DebrisImpulseCore.cs").write_text(src["ledger"])
    # Keep the pinned producer's test-only Unity API shim, strip its fake emitter:
    # the independent adapter uses the REAL original W06 ledger instead.
    (inp/"UnityShim.cs").write_text(src["shim"].split("namespace B01.DebrisRepulse001",1)[0])
    evidence={"schema":"b01-62-w11-original-r2-ledger-atomicity-r1",
              "source_pins":{k:{"commit":v[0],"path":v[1],"git_blob":v[2]} for k,v in sorted(PIN.items())},
              "source_checks":result, "runtime_test_kind":"original R2 C# + original W06 ledger under simulated Unity API",
              "unity_physx_not_executed":True,"codex_ack":False,
              "integration":False, "safe_to_merge":False}
    raw=(json.dumps(evidence,indent=2,sort_keys=True)+"\n").encode()
    (dest/"receipt.json").write_bytes(raw)
    print("ORIGINAL_SOURCE_BLOBS_VERIFIED=6/6")
    print("SOURCE_TRANSACTION_GATE=BOTH_CANDIDATES_TRANSACTION_HOLD")
    print("JSON_SHA256="+hashlib.sha256(raw).hexdigest())
    print("NATIVE_UNITY=NOT_RUN")

if __name__=="__main__":
    ap=argparse.ArgumentParser()
    ap.add_argument("--repo",required=True)
    ap.add_argument("--dest",required=True)
    a=ap.parse_args()
    materialize(a.repo,a.dest)
