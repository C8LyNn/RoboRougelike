#!/usr/bin/env python3
"""B01-62 W11: stage exact original #494 W07+W06 C# for independent executed QA.
No changes to Git worktree producer paths or Codex. Unity API simulation != PhysX.
"""
import argparse
import hashlib
import json
import pathlib
import subprocess

TARGET="6efe45961ed0ffdf183e8016e350978b9f3dc51c"
ORIGINALS={
 "DebrisRigPlayground.cs":("Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs","60fc22efec0e49d7688adde1fcb776093f2196af"),
 "RigDemoDefinition.cs":("Assets/B0123DebrisPlayground001/Runtime/RigDemoDefinition.cs","b7d1a503c89537316fe48dbb54f5997bb21d14ce"),
 "DebrisImpulseCore.cs":("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs","fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
 "DebrisRepulsionEmitter.cs":("Assets/B0123DebrisRepulse001/Runtime/DebrisRepulsionEmitter.cs","2b3a8465c550bd49a5c98be5a8064a4ce0947f4b"),
 "ProducerStub.cs":("Tools/B01/B01-23/P17W10PublicDockGuardR2/UnityBoundaryStub.cs","d5fc14c1ad7d4ce730f03aef22a4272cb6dd7e43"),
}
def source_blob(data):
    return hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()
def fetch(repo="."):
    result={}
    for filename,(path,pin) in ORIGINALS.items():
        data=subprocess.check_output(["git","-C",str(repo),"show",TARGET+":"+path])
        if source_blob(data)!=pin:raise ValueError("IMMUTABLE_PRODUCER_DRIFT "+filename)
        result[filename]=data.decode("utf-8-sig")
    return result

def inspect(s):
    demo=s["DebrisRigPlayground.cs"]
    core=s["DebrisImpulseCore.cs"]
    emit=s["DebrisRepulsionEmitter.cs"]
    start=demo.index("public bool Reattach(int index)")
    end=demo.index("private const int DockQueryCapacity",start)
    body=demo[start:end]
    mandatory=[
      "if (!ValidatePhysicalReattach(rig)) return false;",
      "rig.Body.linearVelocity = Vector3.zero;",
      "rig.Body.angularVelocity = Vector3.zero;",
      "rig.Body.isKinematic = true;",
      "rig.Part.transform.SetParent(rig.Root.transform, false);",
      "rig.Part.transform.localPosition = rig.LocalMountPosition;",
      "rig.Detached = false;",
      "rig.Emitter.RearmAfterReattach(rig.Definition.PartId)"
    ]
    positions=[body.find(x) for x in mandatory]
    if any(x<0 for x in positions) or positions!=sorted(positions):
        raise ValueError("ORIGINAL_GUARD_OR_REARM_TRANSACTION_DRIFT")
    if "public void ClearForNewRun() => ledger.ClearForNewRun();" not in emit:
        raise ValueError("W06_REAL_EMITTER_CONTRACT_DRIFT")
    if "public bool RearmAfterReattach(long id) => id > 0 && seen.Remove(id);" not in core:
        raise ValueError("W06_REAL_LEDGER_CONTRACT_DRIFT")
    if "Physics.OverlapBoxNonAlloc(" not in demo or "Physics.ComputePenetration(" not in demo:
        raise ValueError("MISSING_PHYSICS_GUARD")
    return {"selected_producer":TARGET,"original_git_blobs":{k:{"path":v[0],"blob_sha":v[1]} for k,v in sorted(ORIGINALS.items())},
            "public_reach_physics_guard":"SOURCE_OBSERVED",
            "physical_mutations_before_failed_rearm":6,
            "missing_consumed_id_failure_atomicity":"RELEASE_HOLD",
            "tested_engine":"ACTUAL_ORIGINAL_CSHARP_CLASSES_WITH_TEST_ONLY_UNITY_SHIM",
            "native_unity_physx":"NOT_RUN","save_load":"NOT_RUN",
            "codex_ack":"NONE"}

def make_stub(s):
    orig=s["ProducerStub.cs"]
    prefix="namespace B01.DebrisRepulse001"
    if orig.count(prefix)!=1:raise ValueError("PRODUCER_SHIM_SCOPE_DRIFT")
    stub=orig.split(prefix)[0]
    decl="[AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute {}"
    if stub.count(decl)!=1:raise ValueError("PRODUCER_SHIM_ATTRIBUTE_DRIFT")
    stub=stub.replace(decl,decl+"\n    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}\n    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute { public MinAttribute(float n){} }")
    rb="public Vector3 linearVelocity,angularVelocity;"
    if stub.count(rb)!=1:raise ValueError("PRODUCER_SHIM_RB_DRIFT")
    stub=stub.replace(rb,rb+"\n        public Vector3 worldCenterOfMass => transform.position;\n        public void AddForce(Vector3 impulse, ForceMode mode) { linearVelocity = linearVelocity + impulse * (1f/mass); }")
    decl="public enum RigidbodyConstraints{FreezeRotation}"
    if stub.count(decl)!=1:raise ValueError("PRODUCER_SHIM_FORCE_DRIFT")
    stub=stub.replace(decl,decl+"\n    public enum ForceMode { Impulse }")
    return stub

def stage(repo, dest):
    dest=pathlib.Path(dest)
    if dest.exists() or not dest.parent.is_dir():raise ValueError("NEW_DISPOSABLE_STAGE_REQUIRED")
    sources=fetch(repo)
    report=inspect(sources)
    stub=make_stub(sources)
    dest.mkdir()
    for name in ("DebrisRigPlayground.cs","RigDemoDefinition.cs","DebrisImpulseCore.cs","DebrisRepulsionEmitter.cs"):
        (dest/name).write_text(sources[name],encoding="utf-8",newline="\n")
    (dest/"UnityBoundaryShim.cs").write_text(stub,encoding="utf-8",newline="\n")
    receipt=(json.dumps(report,sort_keys=True,indent=2)+"\n").encode()
    (dest/"original-source-receipt.json").write_bytes(receipt)
    print("PINNED_GIT_BLOBS=5/5")
    print("SOURCE_API_GUARDED=YES")
    print("SOURCE_TRANSACTION=HOLD_REARM_AFTER_MUTATION")
    print("SOURCE_RECEIPT_SHA256="+hashlib.sha256(receipt).hexdigest())
    return receipt

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",default=".")
    p.add_argument("--stage",required=True)
    a=p.parse_args()
    stage(a.repo,a.stage)
if __name__=="__main__":main()
