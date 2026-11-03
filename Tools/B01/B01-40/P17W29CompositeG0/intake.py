#!/usr/bin/env python3
"""B01-40 W29. W27 original code upgraded for NEW #826 Unity-compatible V3 composite G0 and safe source handoff.
Strictly source/transport QA: no Save writer, Unity Editor, or Codex worktree.
"""
from __future__ import annotations
import argparse
import hashlib
import io
import zipfile
import json
import re
import subprocess
import tempfile
from pathlib import Path

OWN="Tools/B01/B01-40/P17W29CompositeG0/"
WORKFLOW=".github/workflows/b01-40-p17w29-826-exclusive-composite-g0.yml"
PINS={
 "r3":"f5db40bf293fe9582ec7c6fcb3515aeff6fcf937",
 "full":"99fb08619c78b3834bb4c0450499babf991f218a",
 "rig":"16cb2a7b5158605b15191b6074f9e392e0d1ebd3",
 "econ":"b4df10a7ad7e6c758f8331013f6b216b3a3689a3",
 "r5":"60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb",
 "api":"b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
 "game":"d94400d95d13db7f27e1533df05c9292d5380431",
 "save":"33094f154c7b91119b36d58aab285e6e83c5de57",
 "target":"ed8cf4bcc131688b9a93968a89683d2496586683",
}
PARENTS={
 "r3":("3aaaec4027ee246fbfb6f209179f0e9507363473",8,0),
 "full":("2b7983f0f9e09dd1d56906b379a3f7af7b02f21a",9,0),
 "rig":("ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1",8,2),
 "econ":("cc5d65f66ad7a764af6515c566e489b9fc3be82e",16,0),
 "r5":("cc7f73541517a18f4479bb155534bd4832d9985e",3,1),
}
DIRECT={
 "r3":("Tools/B01/B01-62/P17W25StrictCanonicalPathR3/CanonicalSingleAuthorityStoreV3R3.cs",
       "Tools/B01/B01-62/P17W25StrictCanonicalPathR3/CanonicalSlotSessionV3R3.cs"),
 "full":("Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs",
         "Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/CanonicalWholeGameSessionV3.cs"),
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
SHARED_ECON_FOLDER="Assets/B01Packages.meta"
POLICY="Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs"
SECTION="Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs"
NATIVE="Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"
EXPECTED_BLOBS={
 ("econ",SHARED_ECON_FOLDER):"38bd6da78864e817f6c8905aa0805a83528d2a25",
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
            elif name=="full":ok=path.startswith("Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/") or path==".github/workflows/b01-62-w28-unity-compatible-whole-game-v3.yml"
            elif name=="rig":ok=path.startswith("Tools/B01/B01-23/P17W25PhysicalRollbackR2/") or path==".github/workflows/b01-23-p17w25-physical-rollback-r2.yml" or (st=="M" and path in (
                 "Assets/B0123CanonicalRigPortW24/Runtime/CanonicalPhysicalRigPortW24.cs",
                 "Assets/B0123CanonicalRigPortW24/Tests/PlayMode/CanonicalPhysicalRigPortW24NativeTests.cs"))
            elif name=="econ":ok=path.startswith("Tools/B01/ECONAPPROVED007/") or path.startswith("Assets/B01Packages/ECONAPPROVED007") or path==".github/workflows/b01-22-p17w25-econ-section-v7.yml"
            else:ok=path.startswith("Tools/B01/B01-23/P17W23R5ColdRestart/") or path==".github/workflows/b01-23-p17w23-r5-coldrestart-fixture.yml" or (st=="M" and path==NATIVE)
            must(ok,"FOREIGN_PRODUCER_CHANGE:"+name+":"+path)
        print("B0140_W29_ORIGINAL_PRODUCER_SCOPE_PASS "+name+" "+str(len(rows)))
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
    print("B0140_W29_EXACT_PINNED_SOURCE_BLOBS_PASS")
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
    # Critical shared folder meta omitted by earlier W27 intake. It must be
    # counted once, pinned as an original source, and compared to published R3.
    # Existing game copy must never be overwritten merely by package install.
    shared=read(PINS["econ"],SHARED_ECON_FOLDER)
    must(SHARED_ECON_FOLDER not in selected,"SHARED_META_ALREADY_OWNER")
    selected[SHARED_ECON_FOLDER]=shared
    ownership[SHARED_ECON_FOLDER]="econ"
    must(read(PINS["target"],SHARED_ECON_FOLDER)==shared,
         "CODEX_GAME_SHARED_PACKAGES_FOLDER_META_CONFLICT")
    print("B0140_W29_SHARED_B01PACKAGES_META_BYTE_EXACT_WITH_GAME_PASS")
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
    must(by_owner.get("rig")==9 and by_owner.get("econ")==6,
         "NEW_9_RIG_6_ECON_INCLUDING_ROOT_GUID_COUNT:"+str(by_owner))
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
    # #826 replaces old #797 V2. Verify V3 original source, not producer test copies.
    complete=b"\n".join(selected[p] for p in DIRECT["full"])
    must(b"SHA256.Create()" in complete and b"ComputeHash" in complete
         and b"File.Replace(staging,authoritative,null)" in complete
         and b"File.Move(staging,authoritative)" in complete
         and b"StringComparison.Ordinal" in complete,
         "V3_UNITY_COMPAT_AND_ATOMIC_PUBLISH_SIGNATURES_MISSING")
    for obsolete in (b"SHA256.HashData",b"Convert.ToHexString",
                     b"File.Move(staging,authoritative,true)"):
        must(obsolete not in complete,"OBSOLETE_797_UNITY_API_REAPPEARED:"+obsolete.decode())
    must(b"CanonicalWorldSlotLease" in complete and b"Flush(true)" in complete
         and b"slot.canonical-game-v2" in complete
         and b"B0162G02" in complete,
         "V3_ONE_OWNER_OR_V2_BINARY_FORMAT_MISSING")
    print("B0140_W29_ORIGINAL_826_V3_UNITY_ABI_AND_ATOMIC_PUBLISH_SOURCE_SIGNATURES_PASS")
    manifest={
      "schema":"B01-COMPOSITE-SOURCE-G0-V3",
      "identity":"B01-40-P17-W29-826-SELECTED-COMPOSITE-SOURCE-001",
      "source_heads":{k:PINS[k] for k in ("r3","full","rig","econ","r5")},
      "frozen_dependencies":{k:PINS[k] for k in ("api","game","save")},
      "codex_target_base":PINS["target"],
      "adoption_mode":"OWNER_SCOPED_SOURCE_CLOSURE_NO_FLAT_BRANCH_OVERLAY",
      "one_writer":"826_V3_WHOLE_GAME_ONLY_787_AS_AUTHORITY_REFERENCE_NOT_RAW_WRITER",
      "source_files":{p:{"owner":ownership[p],"bytes":len(b),"sha256":sha(b)}
                     for p,b in sorted(selected.items())},
      "source_file_count":len(selected),
      "all_unity_guids":all_guids,
      "unity_guid_count":len(all_guids),
      "new_rig_guids":9,
      "new_economy_v7_guids":5,
      "shared_economy_parent_meta_guid":all_guids.get("b01a0001000000000000000000000001"),
      "target_shared_folder_meta_blob":"38bd6da78864e817f6c8905aa0805a83528d2a25",
      "asmdef_names":asmdefs,
      "producer_exact_original_git_blobs":{
         name+":"+path:expected for (name,path),expected in EXPECTED_BLOBS.items()
      },
      "api_compatibility":"V3_CSHARP9_NETSTANDARD21_SOURCE_GREEN_CROSSCHECK_NEEDED",
      "unity_abi_independent_receipt":"https://github.com/C8LyNn/RoboRougelike/pull/847",
      "source_transport_g0":"SOURCE_PASS_IF_THIS_CI_SUCCESS",
      "full_unity_product_g0":"HOLD_ENGINE_BINDER_AND_NATIVE_G1",
      "native_g1":"NOT_RUN_PACKAGE",
      "playable_game":"NOT_INTEGRATED",
      "g2":"HOLD",
      "codex_ack":False,
      "merge":False,
      "migration":"BACKUP_FIRST_EXPLICIT_OWNER_APPROVAL_ONLY",
      "alternatives":"797_OLD_WRITER_787_RAW_STORE_749_773_753_741_793_V6_764_V6_772_OLD_RIG_HOLD",
    }
    print("B0140_W29_ASMDEF_SCRIPT_GUID_CLOSURE_PASS GUIDS="+str(len(all_guids))+
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
    payload["B01-40/W29/PACKAGE_MANIFEST.json"]=(json.dumps(manifest,sort_keys=True,indent=2)+"\n").encode("utf-8")
    with tempfile.TemporaryDirectory(prefix="b01-w29-private-source-") as d:
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
    print("B0140_W29_OWNED_DISPOSABLE_SOURCE_STAGE_ROLLBACK_NEGATIVES_PASS")
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
    manifest_bytes=(json.dumps(m,sort_keys=True,indent=2)+"\n").encode("utf-8")
    output.write_bytes(manifest_bytes)
    # SOURCE-only, NOT installable directly into Unity: omit audit-only old R3 raw
    # Store to make it physically impossible for the receiver ZIP to overlay
    # a second writer by accident. The exact raw R3 bytes remain proven by
    # the manifest and immutable B01-40 G0 script.
    importable={k:v for k,v in data.items() if k not in DIRECT["r3"]}
    entries={"PACKAGE_MANIFEST.json":manifest_bytes,
       "REVIEW_ONLY_README.txt":(
         "G0 SOURCE ONLY. Do not install #826 until game binder/native G1. "
         "No raw #787 Store; only one writer. Exact selected SHA in MANIFEST.\n"
       ).encode("utf-8")}
    for k,v in importable.items():
        group="REVIEW_ONLY/" if k in DIRECT["full"] else "SOURCE/"
        entries[group+k]=v
    with io.BytesIO() as buffer:
        with zipfile.ZipFile(buffer,"w",compression=zipfile.ZIP_DEFLATED,
                             compresslevel=9) as archive:
            for name,b in sorted(entries.items()):
                info=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
                info.create_system=3; info.external_attr=(0o100644<<16)
                info.compress_type=zipfile.ZIP_DEFLATED
                archive.writestr(info,b,compress_type=zipfile.ZIP_DEFLATED,
                                 compresslevel=9)
        package=buffer.getvalue()
    zipfile_path=output.parent/"B0140_W29_826_EXCLUSIVE_COMPOSITE_SOURCE_ONLY.zip"
    must(not zipfile_path.exists(),"SOURCE_ZIP_RECEIPT_ALREADY_EXISTS")
    zipfile_path.write_bytes(package)
    print("B0140_W29_SOURCE_ZIP_RAW_R3_EXCLUDED_PASS members="+str(len(entries))+
          " sha256="+sha(package))
    print("B0140_W29_826_SOURCE_TRANSPORT_G0_PASS_PRODUCT_G0_HOLD_BINDER")
    print("B0140_W29_G1_NOT_RUN_NO_CODEX_INSTALL_NO_MERGE")
    print("SOURCE_RECEIPT_SHA256="+sha(output.read_bytes()))
if __name__=="__main__":
    try:main()
    except (GateHold,subprocess.CalledProcessError) as e:
        print("B0140_W29_SOURCE_G0_FAIL:"+str(e),flush=True)
        raise
