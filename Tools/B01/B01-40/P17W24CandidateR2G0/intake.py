#!/usr/bin/env python3
"""B01-40 W24 independent source transport / GUID / scratch rollback gate.
This is NOT a Unity compilation, live integration, or independent semantic QA
acceptance of the #773 public Store fix. Candidate is NOT owner-selected.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import re
import shutil
import subprocess
import tempfile
from pathlib import Path

PINS = {
    "candidate": "3aaaec4027ee246fbfb6f209179f0e9507363473",
    "selected": "8634e71dfcf633323073fe584a6c0884f1ca5d23",
    "api": "b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
    "rig": "ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1",
    "econ": "cc5d65f66ad7a764af6515c566e489b9fc3be82e",
    "econbase": "1b964643dc191e5daa47b03c6e6fb295c0427493",
    "r5": "60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb",
    "r5base": "cc7f73541517a18f4479bb155534bd4832d9985e",
    "game": "d94400d95d13db7f27e1533df05c9292d5380431",
    "save": "33094f154c7b91119b36d58aab285e6e83c5de57",
}
OWN="Tools/B01/B01-40/P17W24CandidateR2G0/"
WORKFLOW=".github/workflows/b01-40-p17w24-candidate-r2-g0.yml"
R2="Tools/B01/B01-62/P17W24CanonicalPathFixR2/"
RIG="Assets/B0123CanonicalRigPortW24"
ECON="Assets/B01Packages/ECONAPPROVED006"
R5TEST="Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"
BLOBS={
    "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs":"21733278b6ab0669fccb6d085e0b6cf242792de2",
    "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
    "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
    "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
    "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43",
    "Tools/B01/B01-62/P17W23OneAuthorityV3/CanonicalSingleAuthorityStoreV3.cs":"f8fab0b35147a23ae31bcb4a0cf91590d0896d97",
    "Tools/B01/B01-62/P17W23OneAuthorityV3/CanonicalSlotSessionV3.cs":"9d86581626c8657b6662cb281f7621fa43343fb3",
}
def fail(msg):
    raise AssertionError(msg)
def check(x,msg):
    if not x:fail(msg)
def cmd(root,*args):
    p=subprocess.run(["git","-C",str(root),*args],capture_output=True)
    check(p.returncode==0,"GIT_FAIL:"+repr(args)+":"+p.stderr.decode(errors="replace")[-350:])
    return p.stdout
def sha(b):return hashlib.sha256(b).hexdigest()
def source(root,path):
    return cmd(root,"show","HEAD:"+path)
def paths(root,prefix):
    names=cmd(root,"ls-tree","-r","--name-only","HEAD","Assets").decode().splitlines()
    return [s for s in names if s==prefix+".meta" or s.startswith(prefix+"/")]
def diff(root,base):
    rows=cmd(root,"diff","--name-status",base,"HEAD").decode().splitlines()
    return {line.split("\t",1)[1]:line.split("\t",1)[0] for line in rows}
def refs_integrity(roots,own):
    for k,ref in PINS.items():
        if k.endswith("base"):continue
        check(cmd(roots[k],"rev-parse","HEAD").decode().strip()==ref,"MOVED_OWNER_SHA:"+k)
    check(cmd(own,"rev-parse","HEAD").decode().strip()!=PINS["candidate"],
          "OWN_QA_SHOULD_NOT_MUTATE_CANDIDATE")
    check(subprocess.run(["git","-C",str(own),"merge-base","--is-ancestor",
         PINS["candidate"],"HEAD"]).returncode==0,"QA_NOT_CANDIDATE_DESCENDANT")
    ownership=set(diff(own,PINS["candidate"]))
    check(ownership=={
        OWN+"intake.py",OWN+"README.md",WORKFLOW
    },"QA_FOREIGN_OWNER_PATHS:"+str(sorted(ownership)))
    for p,stat in diff(own,PINS["candidate"]).items():
        check(stat=="A","QA_MODIFIED_EXISTING_PATH:"+p)
    comparisons=[
        ("candidate","selected",8,R2),
        ("rig","api",21,RIG),
        ("econ","econbase",13,ECON),
        ("r5","r5base",4,"Tools/B01/B01-23/P17W23R5ColdRestart"),
    ]
    for key,parent,count,prefix in comparisons:
        root=roots[key];change=diff(root,PINS[parent])
        check(len(change)==count,"ORIGINAL_PRODUCER_DIFF_COUNT:"+key+":"+str(change))
        for p,stat in change.items():
            if key=="candidate": allowed=p.startswith(prefix) or p==".github/workflows/b01-62-w24-749-canonical-path-r2.yml"
            elif key=="rig": allowed=p.startswith(prefix) or p.startswith("Tools/B01/B01-23/P17W24CanonicalPhysicalRigPort001/") or p==".github/workflows/b01-23-p17w24-physical510-port.yml"
            elif key=="econ": allowed=p.startswith(prefix) or p.startswith("Tools/B01/ECONAPPROVED006/") or p==".github/workflows/b01-22-p17w24-approved-economy.yml"
            else: allowed=p==R5TEST or p.startswith(prefix) or p==".github/workflows/b01-23-p17w23-r5-coldrestart-fixture.yml"
            check(allowed and stat==("M" if key=="r5" and p==R5TEST else "A"),
                  "NON_OWNED_OR_MUTATED_SOURCE:"+key+":"+p+":"+stat)
    for p,s in BLOBS.items():
        for owner in (["candidate"] if p.startswith("Tools/") else ["candidate","rig"] if p.startswith("Assets/B0123CanonicalSaveApi") else ["candidate"]):
            check(cmd(roots[owner],"rev-parse","HEAD:"+p).decode().strip()==s,
                  "IMMUTABLE_GIT_BLOB_DRIFT:"+owner+":"+p)
    for alt in ("743e36478ae8cf99e8429af1a8209da9d7ca64de",
                "75f1631f9a36ce51b0f8edc0c7857e924cdf78ec"):
        p=subprocess.run(["git","-C",str(roots["candidate"]),"merge-base","--is-ancestor",alt,"HEAD"],
              capture_output=True)
        check(p.returncode!=0,"ALTERNATIVE_753_741_ANCESTOR")
    old=source(roots["selected"],"Tools/B01/B01-62/P17W23OneAuthorityV3/CanonicalSingleAuthorityStoreV3.cs")
    new=source(roots["candidate"],R2+"CanonicalSingleAuthorityStoreV3R2.cs")
    check(new!=old and b"SingleStoreGate" in new and b"OwnerMatches" in new
          and b"DuplicateStore" in new,"R2_PRODUCER_GUARD_MISSING")
    check(b"CanonicalSingleAuthorityStoreV3R2" in source(roots["candidate"],R2+"CanonicalSlotSessionV3R2.cs"),
          "R2_SESSION_NOT_BOUND_TO_R2_STORE")
    t=source(roots["r5"],R5TEST)
    previous=cmd(roots["r5"],"show",PINS["r5base"]+":"+R5TEST)
    check(b"Assert.AreEqual(5,savedSnapshot.Generation)" in t
          and b"TryRestoreBytes(saved,41,5)" in t
          and b"Assert.AreEqual(5,restored.Generation)" in t
          and previous!=t,
          "R5_COLD_GEN5_SOURCE_NOT_PRESENT")
    print("B0140_W24_ORIGINAL_EXACT_SHA_SOURCE_LINEAGE_PASS")
def pack_unity(roots):
    selections=[
        ("game","Assets/B0123SecureReattachV2"),
        ("game","Assets/B0123DebrisRepulse001"),
        ("save","Assets/B0123DetachedSaveContract001"),
        ("api","Assets/B0123CanonicalSaveApi001"),
        ("r5","Assets/B0123UnitySaveBridge001"),
        ("rig",RIG),
        ("econ",ECON),
    ]
    data={};owners={}
    for owner,prefix in selections:
        entries=paths(roots[owner],prefix)
        check(bool(entries),"MISSING_OWNER_ASSETS:"+owner+":"+prefix)
        for path in entries:
            b=source(roots[owner],path)
            if path in data:check(data[path]==b,"ASSET_PATH_COLLISION:"+path)
            data[path]=b;owners[path]=owner
    meta=[p for p in data if p.endswith(".meta")]
    gids={}
    for p in meta:
        text=data[p].decode("utf-8-sig")
        hits=re.findall(r"(?m)^guid:\s*([0-9a-f]{32})\s*$",text)
        check(len(hits)==1,"INVALID_META_GUID:"+p)
        check(hits[0] not in gids,"DUPLICATE_META_GUID:"+p+":"+gids.get(hits[0],""))
        gids[hits[0]]=p
    novel=[p for p in meta if owners[p] in ("rig","econ")]
    check(len([p for p in novel if owners[p]=="rig"])==9,"RIG_9_META_COUNT")
    check(len([p for p in novel if owners[p]=="econ"])==4,"ECON_4_META_COUNT")
    # Source closure: all serialized m_Script GUIDs in selected package source must exist.
    unresolved={}
    for path,b in data.items():
        if path.endswith((".unity",".prefab",".asset")):
            found=set(re.findall(rb"m_Script:\s*\{[^\r\n]*guid:\s*([0-9a-f]{32})",b))
            bad=sorted(g.decode() for g in found if g.decode() not in gids
                       and g!=b"0000000000000000e000000000000000")
            if bad:unresolved[path]=bad
    check(not unresolved,"UNITY_SERIALIZED_SCRIPT_GUID_UNRESOLVED:"+repr(unresolved))
    check(len([p for p in data if p.endswith(".asmdef") and owners[p]=="rig"])==2,
          "RIG_TWO_ASMDEF_REQUIRED")
    check(len([p for p in data if p.endswith(".asmdef") and owners[p]=="econ"])==1,
          "ECON_ONE_ASMDEF_REQUIRED")
    rigasm=json.loads(data[RIG+"/Runtime/B0123.CanonicalRigPortW24.Runtime.asmdef"])
    ecoasm=json.loads(data[ECON+"/Runtime/B01.EconApprovedV6.asmdef"])
    check("B0123.CanonicalSaveApi.Core" in rigasm["references"],
          "RIG_API_ASMDEF_MISSING")
    check(ecoasm.get("autoReferenced") is False and ecoasm.get("noEngineReferences") is True,
          "ECON_ASMDEF_MUST_REMAIN_DETACHED")
    manifest={
        "gate":"B01-40-W24-CANDIDATE-R2-TRANSPORT-G0",
        "pins":PINS,"files":{p:sha(b) for p,b in sorted(data.items())},
        "asset_count":len(data),"guid_count":len(gids),
        "rig_new_guids":9,"economy_new_guids":4,
        "r5_fixture":"CORRECTED_GENERATION_5_SOURCE_ONLY",
        "selected_production":"749_UNTIL_EXPLICIT_OWNER_DECISION",
        "candidate":"773_ONLY_NOT_SELECTED",
        "alternatives":["753_ALT_HOLD","741_ALT_HOLD"],
        "coinstall_original_749_store":False,
        "native_unity":"NOT_RUN","game_save_atomic_economy":"HOLD",
        "private_510_legacy_host_import":"HOLD","codex_ack":False,
        "integrated":False,"released":False,
    }
    print("B0140_W24_UNITY_NEW_GUIDS_PASS 9_RIG 4_ECON TOTAL_META="+str(len(gids)))
    return data,manifest
def safe_cleanup(stage,expected):
    check(stage.is_dir() and not stage.is_symlink(),"ROLLBACK_NOT_OWNED_DIR")
    have=set()
    for p in stage.rglob("*"):
        check(not p.is_symlink(),"ROLLBACK_SYMLINK_BLOCKED")
        rel=p.relative_to(stage).as_posix()
        if p.is_file():
            have.add(rel)
            check(rel in expected and p.read_bytes()==expected[rel],
                  "ROLLBACK_FOREIGN_OR_DIRTY_FILE:"+rel)
        elif p.is_dir() and not any(p.iterdir()):
            check(any(k.startswith(rel+"/") for k in expected),
                  "ROLLBACK_FOREIGN_EMPTY_DIR:"+rel)
    check(have==set(expected),"ROLLBACK_MISSING_OWNED_FILES")
    for k in sorted(expected):
        (stage/k).unlink()
    for p in sorted((x for x in stage.rglob("*") if x.is_dir()),
                    key=lambda x:len(x.parts),reverse=True):
        p.rmdir()
    stage.rmdir()
def scratch(data,manifest):
    # Never access or modify live Unity folder, protected branch, or actual Save.
    expected=dict(data)
    expected["B01-40/W24/PACKAGE_MANIFEST.json"]=(json.dumps(manifest,sort_keys=True,indent=2)+"\n").encode()
    with tempfile.TemporaryDirectory(prefix="b0140-w24-private-source-") as t:
        outer=Path(t)
        foreign=outer/"USER_EXISTING"
        foreign.mkdir()
        (foreign/"SAVE_DO_NOT_TOUCH.dat").write_bytes(b"real user sentinel untouched")
        (foreign/"EMPTY").mkdir()
        stage=outer/"NEW_ONLY_GATE_STAGE"
        check(not stage.exists(),"STAGE_ALREADY_EXISTS")
        stage.mkdir()
        for p,b in expected.items():
            f=stage/p;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(b)
        changed=next(k for k in expected if k.endswith(".cs"))
        dirty=stage/changed
        dirty.write_bytes(b"UNOWNED_DIRTY")
        try:safe_cleanup(stage,expected)
        except AssertionError as e:check("ROLLBACK_FOREIGN_OR_DIRTY_FILE" in str(e),"DIRTY_FAIL_WRONG")
        else:fail("DIRTY_ROLLBACK_UNSAFE")
        dirty.write_bytes(expected[changed])
        rogue=stage/"FOREIGN_EMPTY";rogue.mkdir()
        try:safe_cleanup(stage,expected)
        except AssertionError as e:check("ROLLBACK_FOREIGN_EMPTY_DIR" in str(e),"FOREIGN_EMPTY_FAIL_WRONG")
        else:fail("FOREIGN_EMPTY_ROLLBACK_UNSAFE")
        rogue.rmdir()
        safe_cleanup(stage,expected)
        check(not stage.exists() and (foreign/"SAVE_DO_NOT_TOUCH.dat").read_bytes()==b"real user sentinel untouched"
              and (foreign/"EMPTY").is_dir(),"UNOWNED_USER_DATA_MUTATED")
    print("B0140_W24_DISPOSABLE_PHYSICAL_STAGE_AND_SAFE_ROLLBACK_PASS")
def main():
    a=argparse.ArgumentParser()
    for key in ["own",*PINS]:
        if key.endswith("base"):continue
        a.add_argument("--"+key,required=True)
    a.add_argument("--out",required=True)
    args=a.parse_args()
    roots={k:Path(getattr(args,k)).resolve() for k in PINS if not k.endswith("base")}
    own=Path(args.own).resolve()
    refs_integrity(roots,own)
    data,manifest=pack_unity(roots)
    scratch(data,manifest)
    output=Path(args.out)
    check(not output.exists(),"EVIDENCE_OUTPUT_ALREADY_EXISTS")
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(manifest,sort_keys=True,indent=2)+"\n")
    print("B0140_W24_SOURCE_TRANSPORT_G0_PASS_CANDIDATE_NOT_SELECTED")
    print("B0140_W24_NATIVE_UNITY_G1_NOT_RUN_LIVE_ECON_SAVE_HOLD_NO_MERGE")
    print("EVIDENCE_SHA256="+sha(output.read_bytes()))
if __name__=="__main__":
    main()
