#!/usr/bin/env python3
"""B01-62 W10 independent immutable V2 commit/ledger transaction receiver.
Source-level original C# evidence only. Real CLR ledger+router execution is separate.
"""
import argparse
import hashlib
import json
import re
import subprocess
from pathlib import Path
from gate import GateError, blob_sha, masked, method

V2_SHA="33a32b8bd9ab40467ab1a17b5684926a37dbd4d3"
PINS={
 "controller":("Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs","491c89a18827bb027854bba5e6be6630ce4947b6"),
 "policy":("Assets/B0123SecureReattachV2/Runtime/SecureDockPolicy.cs","cc6611343d585fd0f16ff048f8ecdb0e7b1be30c"),
 "ledger":("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs","fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
 "emitter":("Assets/B0123DebrisRepulse001/Runtime/DebrisRepulsionEmitter.cs","2b3a8465c550bd49a5c98be5a8064a4ce0947f4b"),
 "scene":("Assets/B0123SecureReattachV2/Editor/SecureReattachSceneBuilder.cs","07de6c8ce784b714699ce5598564aab2b2f1db29")
}
def source(repo="."):
    orig={}
    for key,(path,pin) in PINS.items():
        data=subprocess.check_output(["git","-C",str(repo),"show",V2_SHA+":"+path])
        if blob_sha(data)!=pin:raise GateError("V2_SOURCE_DRIFT:"+key)
        orig[key]=data.decode("utf-8-sig")
    return orig

def examine(s):
    commit=method(s["controller"],"CommitDock","bool")
    policy=method(s["policy"],"Execute","bool")
    # Model specifically the physical mutations of a detached rig, not a real Rigidbody.
    order=[
        "rig.Body.linearVelocity=Vector3.zero;",
        "rig.Body.angularVelocity=Vector3.zero;",
        "rig.Body.isKinematic=true;",
        "rig.Part.transform.SetParent(rig.Root.transform,false);",
        "rig.Part.transform.localPosition=rig.LocalMount;",
        "rig.Detached=false;",
        "rig.Emitter.RearmAfterReattach(rig.Spec.PartId)"
    ]
    positions=[commit.find(x) for x in order]
    if not all(x>=0 for x in positions) or positions!=sorted(positions):
        raise GateError("V2_COMMIT_ORDER_OR_LEDGER_CALL_DRIFT")
    if "public bool Reattach(int index) => DockGate().Reattach(index);" not in s["controller"]:
        raise GateError("V2_LEGACY_ALIAS_DRIFT")
    for alias in ["TrySafeReattach","ReattachFromGui"]:
        if "public bool "+alias+"(int index) => DockGate()."+alias+"(index);" not in s["controller"]:
            raise GateError("V2_PUBLIC_ALIAS_DRIFT:"+alias)
    if not all("public bool "+alias+"(int index) => Execute(index);" in s["policy"]
               for alias in ["Reattach","TrySafeReattach","ReattachFromGui"]):
        raise GateError("V2_POLICY_ALIAS_NOT_GUARDED")
    if not re.search(r"LastResult\s*=\s*inspect\s*\(\s*index\s*\)\s*;\s*return\s+LastResult\s*==\s*DockResult\.Allowed\s*&&\s*commit\s*\(\s*index\s*\)",policy):
        raise GateError("V2_POLICY_INSPECT_THEN_COMMIT_DRIFT")
    if "public bool RearmAfterReattach(long id) => id > 0 && seen.Remove(id);" not in s["ledger"]:
        raise GateError("W06_ORIGINAL_LEDGER_DRIFT")
    if "public void ClearForNewRun() => ledger.ClearForNewRun();" not in s["emitter"]:
        raise GateError("ORIGINAL_PUBLIC_LEDGER_RESET_DRIFT")
    if "controller.AddComponent<SecureDebrisPlaygroundV2>();" not in s["scene"]:
        raise GateError("V2_SCENE_EXCLUSIVE_CONTROLLER_DRIFT")
    return {
       "source_gate":"V2_ALL_PUBLIC_ALIASES_INSPECT_FIRST",
       "data_integrity_gate":"HOLD_COMMIT_STATE_MUTATED_BEFORE_LEDGER_REARM_PRECONDITION",
       "exact_source_sha":V2_SHA,
       "physical_mutations_before_missing_ledger_detection":6,
       "preflight_contains_consumed_id":False,
       "w06_public_ledger_reset_available":True,
       "native_physx":"NOT_RUN",
       "real_save_reload":"NOT_RUN",
       "originals":{k:{"path":v[0],"git_blob":v[1]} for k,v in sorted(PINS.items())},
       "rollback":"QA_ONLY_REMOVE_OWN_FILES",
       "codex_ack":"NONE",
       "integrated":False,
    }

def write_stage(s,dest):
    if dest.exists():raise GateError("DISPOSABLE_STAGE_ALREADY_EXISTS")
    if not dest.parent.is_dir():raise GateError("MISSING_PARENT")
    dest.mkdir()
    for key,name in (("ledger","DebrisImpulseCore.cs"),("policy","SecureDockPolicy.cs")):
        (dest/name).open("xb").write(s[key].encode("utf8"))

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",default=".")
    p.add_argument("--receipt",required=True)
    p.add_argument("--stage")
    p.add_argument("--release",action="store_true")
    a=p.parse_args()
    s=source(a.repo)
    report=examine(s)
    path=Path(a.receipt)
    if path.exists() or not path.parent.is_dir():raise GateError("RECEIPT_MUST_BE_NEW")
    data=(json.dumps(report,sort_keys=True,indent=2)+"\n").encode()
    path.open("xb").write(data)
    if a.stage:write_stage(s,Path(a.stage))
    print("B01_62_V2_TRANSACTION=HOLD_COMMIT_BEFORE_ID_REARM")
    print("V2_RECEIPT_SHA256="+hashlib.sha256(data).hexdigest())
    return 3 if a.release else 0
if __name__=="__main__":
    raise SystemExit(main())
