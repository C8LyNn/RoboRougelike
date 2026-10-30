#!/usr/bin/env python3
"""Actual deterministic Unity native FBX package of original CHL003 signage.

Two source-produced 3D FBX and local receiver instructions. Explicitly only
a visual overlay, no Collider/GameObject layer, source-owner runtime touched.
"""
from __future__ import annotations
import argparse
import copy
import gzip
import hashlib
import io
import json
import tarfile
from pathlib import Path

PACKAGE="B01-30-CHL003-GATE-SIGNAGE-001/R1"
ROOT="Assets/B0130CHL003GateSigns001"
def sha(b):return hashlib.sha256(b).hexdigest()
def fail(msg):raise ValueError("B0130_W12_REAL_UNITYPACKAGE_REJECT:"+msg)
def guid(path):return sha((PACKAGE+"|"+path).encode())[:32]
def load(source,received):
    input=json.loads((source/"source-locked-gate-signage-receipt.json").read_text())
    qa=json.loads(received.read_text())
    if input.get("package_id")!=PACKAGE or qa.get("package_id")!=PACKAGE or qa.get("actual_reimport_3d_meshes")!=22:
        fail("ORIGINAL_SOURCE_OR_REAL_BLENDER_ROUNDTRIP_ABSENT")
    if input.get("source_exact_head")!="c7b2a234a286089383503da41883d624fe650551":
        fail("ORIGINAL_CHL_HEAD_CHANGED")
    if len(input["two_real_3d_sign_variants"])!=2 or len(qa["signs"])!=2:
        fail("MISSING_ORIGINAL_SIGNS")
    assets=[]
    for variant in ("CHECKPOINT","FINISH"):
        item=next((x for x in input["two_real_3d_sign_variants"] if x["variant"]==variant),None)
        match=next((x for x in qa["signs"] if x["variant"]==variant),None)
        if item is None or match is None:fail("REAL_SIGNAGE_VARIANTS_INCOMPLETE")
        original=item["files"]["fbx"];raw=(source/original["file"]).read_bytes()
        if not raw.startswith(b"Kaydara FBX Binary  \x00\x1a\x00") or sha(raw)!=original["sha256"] or len(raw)!=original["bytes"] or match["real_binary_fbx_sha256"]!=original["sha256"]:
            fail("ORIGINAL_TRUE_FBX_BINARY_DRIFT:"+variant)
        path=ROOT+"/Models/CHL003-"+variant+"-OVERHEAD-NOCOLLIDER.fbx"
        meta=("fileFormatVersion: 2\nguid: "+guid(path)+"\n").encode()
        assets.append({"path":path,"guid":guid(path),"asset":raw,"meta":meta})
    note=(
      "CHL003 - SOURCE-LOCKED ACTUAL 3D CHECKPOINT AND FINISH SIGNAGE\n"
      "B01-20 owner CHL003 original PR #285 exact head c7b2a234a286089383503da41883d624fe650551\n"
      "Visual 3D ONLY: do NOT add colliders, triggers, rigidbody or Layer8.\n"
      "Both models align to existing CHL checkpoint gate source markers at x=+-1.97m,\n"
      "original opening 3.70m and existing post top y=2.6m.\n"
      "Manual optional placement: at origin (0, 0, gate world z) in an isolated\n"
      "Unity6 URP scene. If parenting to gate whose world Y=2.25m, correct\n"
      "local position to compensate: position Y=-2.25m, not 0.\n"
      "Two signs are alternatives, not duplicated on the same gate: use\n"
      "CHECKPOINT at CP0..CP3 and FINISH at finish if B01-20 approves.\n"
      "Models were generated and fresh reimported with real Blender,\n"
      "native Unity ModelImporter/URP/text legibility/clearance NOT_RUN.\n"
      "Do not modify original B01-20 course/demo while receiving G0.\n"
      "B01-40 independent source G0 and B01-60 Unity native G1 PENDING.\n"
      "Rollback: remove ONLY Assets/B0130CHL003GateSigns001 from\n"
      "DISPOSABLE test project after verifying no external references.\n"
    ).encode()
    path=ROOT+"/RECEIVE_README.txt"
    assets.append({"path":path,"guid":guid(path),"asset":note,
                   "meta":("fileFormatVersion: 2\nguid: "+guid(path)+"\n").encode()})
    if len(assets)!=3 or len({x["guid"] for x in assets})!=3:fail("DUPLICATE_UNITY_META_GUID")
    return assets
def pack(entries):
    output=io.BytesIO()
    with gzip.GzipFile(fileobj=output,mode="wb",mtime=0,filename="",compresslevel=9) as gz:
        with tarfile.open(fileobj=gz,mode="w",format=tarfile.USTAR_FORMAT) as tar:
            for entry in sorted(entries,key=lambda q:q["path"]):
                for suffix,blob in (("asset",entry["asset"]),("asset.meta",entry["meta"]),
                                    ("pathname",(entry["path"]+"\n").encode())):
                    member=tarfile.TarInfo(entry["guid"]+"/"+suffix)
                    member.size=len(blob)
                    member.mode=0o644;member.mtime=0;member.uid=member.gid=0
                    member.uname=member.gname=""
                    tar.addfile(member,io.BytesIO(blob))
    return output.getvalue()
def inspect(binary,entries):
    with tarfile.open(fileobj=io.BytesIO(binary),mode="r:gz") as t:
        members=t.getmembers()
        if len(members)!=9 or any(not m.isfile() or ".." in m.name or m.name.startswith("/") for m in members):
            fail("ACTUAL_UNITY_TAR_STRUCTURE_OR_TRAVERSAL")
        if len({m.name for m in members})!=9:fail("DUPLICATE_TAR_MEMBERS")
        data={m.name:t.extractfile(m).read() for m in members}
    for e in entries:
        if not e["path"].startswith(ROOT+"/") or e["guid"]!=guid(e["path"]):
            fail("UNKNOWN_OUT_OF_SCOPE_UNITY_ASSET")
        for typ,payload in (("asset",e["asset"]),("asset.meta",e["meta"]),
                            ("pathname",(e["path"]+"\n").encode())):
            if data.get(e["guid"]+"/"+typ)!=payload:fail("CORRUPT_ACTUAL_UNITY_MEMBER:"+typ)
        if ("guid: "+e["guid"]+"\n").encode() not in e["meta"]:
            fail("UNITY_META_GUID_EMBED_NOT_MATCH")
    if set(data)!={e["guid"]+"/"+typ for e in entries for typ in ("asset","asset.meta","pathname")}:
        fail("FOREIGN_UNITY_TAR_MEMBER")
    return {"actual_native_unity_assets":3,"real_FBX_binary_assets":2,
            "real_TAR_members":9,"unique_deterministic_META_GUIDs":3}
def mutations(original):
    tests=[]
    for label,action in (
      ("actual_FBX_byte_corruption","fbx"),
      ("forged_embedded_GUID","guid"),
      ("foreign_Codex_game_path","path"),
      ("one_original_FBX_missing","missing"),
      ("original_gzip_binary_corruption","gzip"),
    ):
        entries=copy.deepcopy(original)
        if action=="fbx":
            data=bytearray(entries[0]["asset"]);data[350]^=0x40;entries[0]["asset"]=bytes(data)
        elif action=="guid":
            entries[0]["meta"]=entries[0]["meta"].replace(b"guid: ",b"guid: forged")
        elif action=="path":
            entries[1]["path"]=entries[1]["path"].replace(ROOT,"Assets/CodexOwned")
        elif action=="missing":
            entries=entries[1:]
        binary=pack(entries)
        if action=="gzip":
            binary=bytearray(binary);binary[-8]^=0x50;binary=bytes(binary)
        try:inspect(binary,original)
        except (OSError,EOFError,ValueError,tarfile.TarError,KeyError) as e:
            tests.append({"label":label,"result":"EXPECTED_DENIAL","error":str(e)[:200]})
        else:
            raise AssertionError("FALSE_GREEN_DAMAGED_UNITY_ASSET:"+label)
    return tests
def main():
    p=argparse.ArgumentParser()
    p.add_argument("--source",type=Path,required=True)
    p.add_argument("--receiver",type=Path,required=True)
    p.add_argument("--out",type=Path,required=True)
    a=p.parse_args();a.out.mkdir(parents=True,exist_ok=True)
    entries=load(a.source,a.receiver)
    original=pack(entries)
    good=inspect(original,entries)
    if pack(entries)!=original:fail("ACTUAL_BINARY_NOT_DETERMINISTIC")
    bad=mutations(entries)
    if len(bad)!=5:fail("NEGATIVE_CASES_INCOMPLETE")
    output=a.out/"B01-30-CHL003-REAL-3D-CHECKPOINT-FINISH.unitypackage"
    output.write_bytes(original)
    receipt={"package_id":PACKAGE,"status":"REAL_TWO_SOURCE_LOCKED_FBX_BINARY_IN_NATIVE_FORMAT_UNITYPACKAGE_PASS",
     "original_B0120_CHL_PR":285,"original_source_sha":"c7b2a234a286089383503da41883d624fe650551",
     "unitypackage":output.name,"unitypackage_bytes":len(original),
     "unitypackage_sha256":sha(original),**good,
     "assets":[{"path":x["path"],"guid":x["guid"],"bytes":len(x["asset"]),"sha256":sha(x["asset"])}
                for x in entries],
     "real_mutated_binary_package_negative_controls":bad,
     "deterministic_binary_replay":True,"native_unity_import":"NOT_RUN",
     "independent_B0140_G0":"PENDING","B0160_native_G1":"NOT_RUN","codex_ACK":"NONE"}
    (a.out/"real-source-signage-unitypackage-receipt.json").write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n")
    print("B0130_W12_REAL_2_FBX_3_ASSET_UNITYPACKAGE_9_TAR_3_GUID_5_NEGATIVES_PASS",
          json.dumps({"bytes":len(original),"sha":sha(original)}))
if __name__=="__main__":main()
