#!/usr/bin/env python3
"""B01-40 W27. Exact-Git composite source G0 and safe source handoff.
Strictly source/transport QA: no Save writer, Unity Editor, or Codex worktree.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import re
import subprocess
import tempfile
from pathlib import Path

OWN="Tools/B01/B01-40/P17W27CompositeG0/"
WORKFLOW=".github/workflows/b01-40-p17w27-selected-composite-g0.yml"
PINS={
 "r3":"f5db40bf293fe9582ec7c6fcb3515aeff6fcf937",
 "full":"2b7983f0f9e09dd1d56906b379a3f7af7b02f21a",
 "rig":"16cb2a7b5158605b15191b6074f9e392e0d1ebd3",
 "econ":"b4df10a7ad7e6c758f8331013f6b216b3a3689a3",
 "r5":"60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb",
 "api":"b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
 "game":"d94400d95d13db7f27e1533df05c9292d5380431",
 "save":"33094f154c7b91119b36d58aab285e6e83c5de57",
 "target":"f92c873bb1e397a598b133e107ae704e15f86768",
}
PARENTS={
 "r3":("3aaaec4027ee246fbfb6f209179f0e9507363473",8,0),
 "full":("ba3201644b252241429a6f493238c49d822adfae",8,0),
 "rig":("ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1",8,2),
 "econ":("cc5d65f66ad7a764af6515c566e489b9fc3be82e",16,0),
 "r5":("cc7f73541517a18f4479bb155534bd4832d9985e",3,1),
}
DIRECT={
 "r3":("Tools/B01/B01-62/P17W25StrictCanonicalPathR3/CanonicalSingleAuthorityStoreV3R3.cs",
       "Tools/B01/B01-62/P17W25StrictCanonicalPathR3/CanonicalSlotSessionV3R3.cs"),
 "full":("Tools/B01/B01-62/P17W25WholeGameEconomyV7/WholeGameEconomySlotV2.cs",
         "Tools/B01/B01-62/P17W25WholeGameEconomyV7/CanonicalWholeGameSessionV2.cs"),
}
PARTS={
 "game":("Assets/B0123SecureReattachV2","Assets/B0123DebrisRepulse001"),
 "save":("Assets/B0123DetachedSaveContract001",),
 "api":("Assets/B0123CanonicalSaveApi001",),
 "r5":("Assets/B0123UnitySaveBridge001",),
 "rig":("Assets/B0123CanonicalRigPortW24",),
 "econ":("Assets/B01Packages/ECONAPPROVED007",),
}
CODEC="Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"
POLICY="Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs"
SECTION="Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs"
NATIVE="Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"
EXPECTED_BLOBS={
 ("save",CODEC):"b52e511c370f5fca56c74029050844dc8cbb7f9a",
 ("econ",POLICY):"53ebce6c07388dc9c8a8ed79b6de4d3ccbee4773",
 ("econ",SECTION):"d8b71de91db36295b6220e9c4baad71eb54455f6",
 ("r5",NATIVE):"2508a04f57b04447112d2777ce4e3cd7478ed40e",
 ("api","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs"):
   "21733278b6ab0669fccb6d085e0b6cf242792de2",
 ("api","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs"):
   "a9331dd620336d8c42165d134dc8a5e05ed51873",
}
class GateHold(Exception):pass
def must(condition,msg):
    if not condition:raise GateHold(msg)
def git(*args):
    p=subprocess.run(["git",*args],capture_output=True)
    must(p.returncode==0,"GIT:"+str(args)+":"+p.stderr.decode(errors="replace")[-220:])
    return p.stdout
def sha(data):return hashlib.sha256(data).hexdigest()
def read(ref,path):return git("show",ref+":"+path)
def index(ref,prefix):
    return [p for p in git("ls-tree","-r","--name-only",ref,"--",prefix,prefix+".meta").decode().splitlines()
            if p==prefix+".meta" or p.startswith(prefix+"/")]
def blobs_and_ancestry():
    for name,ref in PINS.items():
        must(git("rev-parse",ref).decode().strip()==ref,"MISSING_PR_OR_TARGET_SHA:"+name)
    for name,(base,added,modified) in PARENTS.items():
        subprocess.run(["git","merge-base","--is-ancestor",base,PINS[name]],check=True)
        rows=git("diff","--name-status",base,PINS[name]).decode().splitlines()
        status=[s.split("\t",1)[0] for s in rows]
        must(len(rows)==added+modified and status.count("A")==added
             and status.count("M")==modified and all(s in ("A","M") for s in status),
             "PRODUCER_SCOPE_DRIFT:"+name+":"+str(rows))
        for row in rows:
            st,path=row.split("\t",1)
            if name=="r3":ok=path.startswith("Tools/B01/B01-62/P17W25StrictCanonicalPathR3/") or path==".github/workflows/b01-62-w25-773-strict-case-path.yml"
            elif name=="full":ok=path.startswith("Tools/B01/B01-62/P17W25WholeGameEconomyV7/") or path==".github/workflows/b01-62-w25-whole-game-econ-v7.yml"
            elif name=="rig":ok=path.startswith("Tools/B01/B01-23/P17W25PhysicalRollbackR2/") or path==".github/workflows/b01-23-p17w25-physical-rollback-r2.yml" or (st=="M" and path in (
                 "Assets/B0123CanonicalRigPortW24/Runtime/CanonicalPhysicalRigPortW24.cs",
                 "Assets/B0123CanonicalRigPortW24/Tests/PlayMode/CanonicalPhysicalRigPortW24NativeTests.cs"))
            elif name=="econ":ok=path.startswith("Tools/B01/ECONAPPROVED007/") or path.startswith("Assets/B01Packages/ECONAPPROVED007") or path==".github/workflows/b01-22-p17w25-econ-section-v7.yml"
            else:ok=path.startswith("Tools/B01/B01-23/P17W23R5ColdRestart/") or path==".github/workflows/b01-23-p17w23-r5-coldrestart-fixture.yml" or (st=="M" and path==NATIVE)
            must(ok,"FOREIGN_PRODUCER_CHANGE:"+name+":"+path)
        print("B0140_W27_ORIGINAL_PRODUCER_SCOPE_PASS "+name+" "+str(len(rows)))
    must(subprocess.run(["git","merge-base","--is-ancestor",PINS["r3"],PINS["full"]],
               capture_output=True).returncode==0,
         "WHOLEGAME_NOT_DESCENDANT_SELECTED_STRICT_R3")
    for (name,path),expected in EXPECTED_BLOBS.items():
        got=git("rev-parse",PINS[name]+":"+path).decode().strip()
        must(got==expected,"ORIGINAL_BLOB_DRIFT:"+name+":"+path)
    for old in ("743e36478ae8cf99e8429af1a8209da9d7ca64de",
                "75f1631f9a36ce51b0f8edc0c7857e924cdf78ec"):
        # Alternative branches cannot be imported as an ancestor of selected owners.
        must(subprocess.run(["git","merge-base","--is-ancestor",old,PINS["full"]],
               capture_output=True).returncode!=0,"ALT_753_OR_741_IMPORTED")
    print("B0140_W27_EXACT_PINNED_SOURCE_BLOBS_PASS")
def selected_closure():
    selected={}
    ownership={}
    for owner,prefixes in PARTS.items():
        for prefix in prefixes:
            found=index(PINS[owner],prefix)
            must(bool(found),"SOURCE_PREFIX_MISSING:"+owner+":"+prefix)
            for path in found:
                b=read(PINS[owner],path)
                if path in selected:
                    must(b==selected[path],"SAME_UNITY_PATH_TWO_OWNERS_DIFFER:"+path)
                selected[path]=b
                ownership[path]=owner
    for owner,paths in DIRECT.items():
        for path in paths:
            must(path not in selected,"DIRECT_SOURCE_PATH_COLLISION:"+path)
            selected[path]=read(PINS[owner],path)
            ownership[path]=owner
    must(len(DIRECT["full"])==2 and
         all(path.startswith("Tools/") for path in DIRECT["full"]),
         "FULL_GAME_PRODUCTION_HAS_NO_UNITY_BINDER")
    all_guids={}; by_owner={}
    for path,b in selected.items():
        if not path.endswith(".meta"):continue
        hits=re.findall(rb"(?m)^guid:\s*([0-9a-f]{32})\s*$",b)
        must(len(hits)==1,"INVALID_UNITY_META:"+path)
        g=hits[0].decode()
        must(g not in all_guids,"DUPLICATE_UNITY_GUID:"+g+":"+path+":"+all_guids.get(g,""))
        all_guids[g]=path
        owner=ownership[path]
        by_owner[owner]=by_owner.get(owner,0)+1
    must(by_owner.get("rig")==9 and by_owner.get("econ")==5,
         "NEW_9_RIG_5_ECON_GUID_COUNT:"+str(by_owner))
    asmdefs={}
    for path,b in selected.items():
        if path.endswith(".asmdef"):
            config=json.loads(b)
            name=config.get("name")
            must(isinstance(name,str) and name and name not in asmdefs,
                 "ASSEMBLY_NAME_COLLISION:"+str(name))
            asmdefs[name]=path
    missing_asm={}
    for path,b in selected.items():
        if not path.endswith(".asmdef"):continue
        config=json.loads(b)
        missing=[n for n in config.get("references",[])
                 if isinstance(n,str) and (n.startswith("B0123.") or n.startswith("B01."))
                 and n not in asmdefs]
        if missing:missing_asm[path]=missing
    must(not missing_asm,"ASSEMBLY_INTERNAL_REFERENCE_MISSING:"+str(missing_asm))
    missing_script={}
    for path,b in selected.items():
        if not path.endswith((".prefab",".unity",".asset")):continue
        found=re.findall(rb"m_Script:\s*\{[^\n\r]*guid:\s*([0-9a-f]{32})",b)
        missing=[g.decode() for g in found if g.decode() not in all_guids
                 and g!=b"0000000000000000e000000000000000"]
        if missing:missing_script[path]=missing
    must(not missing_script,"SERIALIZED_SCRIPT_GUID_CLOSURE:"+str(missing_script))
    r5=selected[NATIVE]
    must(b"Assert.AreEqual(5,savedSnapshot.Generation)" in r5 and
         b"TryRestoreBytes(saved,41,5)" in r5,
         "COLDRESTART_G4_G5_FIXTURE_LOST")
    code=selected[DIRECT["r3"][0]]
    must(b"StringComparison.Ordinal" in code and
         b"CanonicalSingleAuthorityStoreV3R3" in code,
         "SELECTED_R3_POSIX_CASE_FIX_MISSING")
    v7=selected[POLICY]
    must(b"public static bool TryPaidQuote" in v7 and
         b"purchase*65L" in v7 and b"/100L" in v7,
         "V7_65_PERCENT_FLOOR_NOT_PRESENT")
    full=selected[DIRECT["full"][0]]
    must(b"B0162G02" in full and b"slot.canonical-game-v2" in full,
         "V7_ONE_WRITER_MAGIC_MISSING")
    must(b"CanonicalEconomySectionV7" in full,
         "V7_BINARY_OWNER_SECTION_MISSING")
    # Do not confuse source G0 transport PASS with Unity compatible code.
    api_rejections={
      "Convert.ToHexString":False,
      "SHA256.HashData":False,
      "File.Move(staging,authoritative,true)":False,
    }
    complete=b"\n".join(selected[p] for p in DIRECT["full"])
    for symbol in api_rejections:
        api_rejections[symbol]=symbol.encode() in complete
    must(all(api_rejections.values()),
         "UNITY_NETSTANDARD_REPRODUCER_CHANGED_RECHECK_REQUIRED:"+str(api_rejections))
    print("B0140_W27_UNITY_797_TARGET_API_REPRODUCER_P0_RED_CONFIRMED")
    manifest={
      "schema":"B01-COMPOSITE-SOURCE-G0-V1",
      "identity":"B01-40-P17-W27-SELECTED-COMPOSITE-SOURCE-001",
      "source_heads":{k:PINS[k] for k in ("r3","full","rig","econ","r5")},
      "frozen_dependencies":{k:PINS[k] for k in ("api","game","save")},
      "codex_target_base":PINS["target"],
      "adoption_mode":"OWNER_SCOPED_SOURCE_CLOSURE_NO_FLAT_BRANCH_OVERLAY",
      "one_writer":"797_V7_WHOLE_GAME_ONLY_787_AS_AUTHORITY_REFERENCE_NOT_RAW_WRITER",
      "source_files":{p:{"owner":ownership[p],"bytes":len(b),"sha256":sha(b)}
                     for p,b in sorted(selected.items())},
      "source_file_count":len(selected),
      "all_unity_guids":all_guids,
      "unity_guid_count":len(all_guids),
      "new_rig_guids":9,
      "new_economy_v7_guids":5,
      "asmdef_names":asmdefs,
      "producer_exact_original_git_blobs":{
         name+":"+path:expected for (name,path),expected in EXPECTED_BLOBS.items()
      },
      "api_compatibility":"P0_FAIL_PROVEN_NATIVE_UNITY_MINIMAL_PROBE_797",
      "unity_api_failure_evidence":"https://github.com/C8LyNn/RoboRougelike/pull/797#issuecomment-6588699104",
      "source_transport_g0":"SOURCE_PASS_IF_THIS_CI_SUCCESS",
      "full_unity_product_g0":"HOLD_API_AND_BINDER",
      "native_g1":"NOT_RUN_PACKAGE",
      "playable_game":"NOT_INTEGRATED",
      "g2":"HOLD",
      "codex_ack":False,
      "merge":False,
      "migration":"BACKUP_FIRST_EXPLICIT_OWNER_APPROVAL_ONLY",
      "alternatives":"749_773_RAW_STORE_793_V6_764_V6_753_741_HOLD",
    }
    print("B0140_W27_ASMDEF_SCRIPT_GUID_CLOSURE_PASS GUIDS="+str(len(all_guids))+
          " ASMS="+str(len(asmdefs))+" SOURCES="+str(len(selected)))
    return selected,manifest
def deny_unsafe_rollback(stage,expected):
    must(stage.is_dir() and not stage.is_symlink(),"ROLLBACK_DIR_NOT_OWNED")
    existing=set()
    for x in stage.rglob("*"):
        must(not x.is_symlink(),"ROLLBACK_SYMLINK_DENIED")
        path=x.relative_to(stage).as_posix()
        if x.is_file():
            existing.add(path)
            must(path in expected and x.read_bytes()==expected[path],
                 "ROLLBACK_UNOWNED_DIRTY_FILE_DENIED:"+path)
        elif x.is_dir() and not any(x.iterdir()):
            must(any(y.startswith(path+"/") for y in expected),
                 "ROLLBACK_FOREIGN_EMPTY_DIR_DENIED:"+path)
    must(existing==set(expected),"ROLLBACK_MISSING_OR_FOREIGN_FILE")
    for k in sorted(expected):(stage/k).unlink()
    for d in sorted((p for p in stage.rglob("*") if p.is_dir()),
                    key=lambda z:len(z.parts),reverse=True):d.rmdir()
    stage.rmdir()
def stage_and_rollback(data,manifest):
    payload=dict(data)
    payload["B01-40/W27/PACKAGE_MANIFEST.json"]=(json.dumps(manifest,sort_keys=True,indent=2)+"\n").encode("utf-8")
    with tempfile.TemporaryDirectory(prefix="b01-w27-private-source-") as d:
        root=Path(d)
        foreign=root/"USER_UNOWNED"
        (foreign/"EMPTY").mkdir(parents=True)
        (foreign/"save.bin").write_bytes(b"FOREIGN_SAVE_KEEP")
        stage=root/"BRAND_NEW_RECEIVER_ONLY"
        must(not stage.exists(),"OCCUPIED_STAGE_DENIED")
        stage.mkdir()
        for k,b in payload.items():
            p=stage/k;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b)
        # Dirty input cannot be deleted, even if user overwrote a test file.
        changed=next(k for k in payload if k.endswith(".cs"))
        path=stage/changed
        path.write_bytes(b"MODIFIED_UNOWNED_BYTES")
        try:deny_unsafe_rollback(stage,payload)
        except GateHold as e:must("ROLLBACK_UNOWNED_DIRTY_FILE" in str(e),"DIRTY_DENIAL_REASON_WRONG")
        else:raise GateHold("DIRTY_ROLLBACK_DELETED_FOREIGN_BYTES")
        path.write_bytes(payload[changed])
        extra=stage/"FOREIGN_EMPTY";extra.mkdir()
        try:deny_unsafe_rollback(stage,payload)
        except GateHold as e:must("ROLLBACK_FOREIGN_EMPTY_DIR" in str(e),"EMPTY_DENIAL_REASON_WRONG")
        else:raise GateHold("FOREIGN_EMPTY_DELETED")
        extra.rmdir()
        deny_unsafe_rollback(stage,payload)
        must(not stage.exists() and (foreign/"save.bin").read_bytes()==b"FOREIGN_SAVE_KEEP"
            and (foreign/"EMPTY").is_dir(),
            "FOREIGN_USER_DATA_CHANGED")
    print("B0140_W27_OWNED_DISPOSABLE_SOURCE_STAGE_ROLLBACK_NEGATIVES_PASS")
def main():
    a=argparse.ArgumentParser()
    a.add_argument("--out",required=True)
    args=a.parse_args()
    blobs_and_ancestry()
    data,m=selected_closure()
    stage_and_rollback(data,m)
    output=Path(args.out)
    must(not output.exists(),"RECEIPT_PATH_ALREADY_USED")
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_bytes((json.dumps(m,sort_keys=True,indent=2)+"\n").encode("utf-8"))
    print("B0140_W27_EXACT_SOURCE_TRANSPORT_G0_PASS_PRODUCT_G0_HOLD")
    print("B0140_W27_G1_NOT_RUN_NO_CODEX_INSTALL_NO_MERGE")
    print("SOURCE_RECEIPT_SHA256="+sha(output.read_bytes()))
if __name__=="__main__":
    try:main()
    except (GateHold,subprocess.CalledProcessError) as e:
        print("B0140_W27_SOURCE_G0_FAIL:"+str(e),flush=True)
        raise
