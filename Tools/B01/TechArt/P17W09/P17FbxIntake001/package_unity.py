#!/usr/bin/env python3
"""Package exactly THREE real original-model-source-derived FBX into Unitypackage.

Git contains no new Unity Assets and doesn't change game/owner source. This
archives FBX data (proven Blender reimport) and source readme for native G1 QA.
Deterministic gzip/tar, stable GUID, exact member validation & mutations.
"""
from __future__ import annotations
import argparse
import copy
import gzip
import hashlib
import io
import json
import struct
import tarfile
from pathlib import Path

PACKAGE="B01-30-P17-ORIGINAL-GLB-UNITY-FBX-001/R1"
ROOT="Assets/B0130P17VisualBridge001"
RIGS=("Flanker","Bulwark","Skirmisher")
def sha(blob):return hashlib.sha256(blob).hexdigest()
def guid(path):return sha((PACKAGE+"|"+path).encode())[:32]
def reject(reason):raise ValueError("B0130_W09_UNITYPACKAGE_REJECT:"+reason)
def meta(path,fbx):
    s="fileFormatVersion: 2\nguid: "+guid(path)+"\n"
    if not fbx:s+="TextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n"
    return s.encode()
def get_inputs(fbxdir:Path,verified:Path):
    report=json.loads((fbxdir/"fbx-source-locked-export.json").read_text())
    good=json.loads(verified.read_text())
    if report.get("package")!=PACKAGE or good.get("package_id")!=PACKAGE or good.get("real_meshes")!=43 or good.get("original_braces")!=5 or good.get("real_pngs")!=3:
        reject("SOURCE_AND_ROUNDTRIP_RECEIPTS")
    if report.get("input_head")!="60a453e4ecd915725bc2fac5433ba7ad43a628cb" or report.get("owner_head")!="5666a39eabd4cea91b2b605a9eb99ea2f1e226e8":
        reject("SOURCE_HEADS_CHANGED")
    if {e["rig"] for e in report["files"]}!=set(RIGS):reject("THREE_REAL_FBX_REQUIRED")
    entries=[]
    for rig in RIGS:
        item=next(e for e in report["files"] if e["rig"]==rig)
        actual=next(e for e in good["real_binary_fbx"] if e["rig"]==rig)
        if item["fbx_sha256"]!=actual["fbx_sha256"] or item["source_glb_sha256"]!=actual["original_glb_sha256"]:reject("REAL_IMPORT_MISMATCH")
        path=fbxdir/item["fbx"]
        data=path.read_bytes()
        if not data.startswith(b"Kaydara FBX Binary  \x00\x1a\x00") or len(data)!=item["fbx_bytes"] or sha(data)!=item["fbx_sha256"]:reject("FBX_SOURCE_BINARY_DRIFT")
        if not 7000<=struct.unpack_from("<I",data,23)[0]<8000:reject("FBX_REAL_FORMAT_VERSION")
        dest=ROOT+"/Models/"+rig+"-ORIGINAL-P17-VISUAL-PROXY.fbx"
        entries.append({"path":dest,"asset":data,"meta":meta(dest,True),"guid":guid(dest),"kind":"fbx"})
    instructions=(
      "B01-30 P17 W09 — VISUAL PROXY UNITY INTAKE ONLY\n"
      "Source: B01-23 original PR #375 at 5666a39eabd4cea91b2b605a9eb99ea2f1e226e8\n"
      "Visual GLB: B01-30 original PR #423 at 60a453e4ecd915725bc2fac5433ba7ad43a628cb\n"
      "Converted as real FBX only; one original authored part = one mesh.\n"
      "Flanker 15 / Bulwark 16 / Skirmisher 12 = 43 source-pinned meshes.\n"
      "Amber material = one of five SOURCE bracing armor placements.\n"
      "Import this unitypackage in a disposable Unity6 URP project FIRST.\n"
      "Drag each model into empty test scene. Verify 43/43 named meshes, material\n"
      "contrast, axes and 0.55m logical source-grid positions; do not add physics.\n"
      "No C# gameplay, no Colliders, no PhysX/socket/weakpoint validation here.\n"
      "Source Blender reimport != native Unity ModelImporter acceptance.\n"
      "No integration approval or Codex ACK. Receiver B01-40/B01-60 decides G0/G1.\n"
      "ROLLBACK: remove ONLY Assets/B0130P17VisualBridge001, after confirming\n"
      "no external references in the isolated test project.\n"
    ).encode()
    dest=ROOT+"/RECEIVE_README.txt"
    entries.append({"path":dest,"asset":instructions,"meta":meta(dest,False),"guid":guid(dest),"kind":"txt"})
    assert len(entries)==4 and len({e["guid"] for e in entries})==4
    return entries

def pack(assets):
    buf=io.BytesIO()
    with gzip.GzipFile(fileobj=buf,mode="wb",mtime=0,filename="",compresslevel=9) as gz:
        with tarfile.open(fileobj=gz,mode="w",format=tarfile.USTAR_FORMAT) as tar:
            for item in sorted(assets,key=lambda x:x["path"]):
                g=item["guid"]
                for suffix,content in (("asset",item["asset"]),("asset.meta",item["meta"]),
                                       ("pathname",(item["path"]+"\n").encode())):
                    info=tarfile.TarInfo(g+"/"+suffix)
                    info.size=len(content);info.mtime=info.uid=info.gid=0
                    info.mode=0o644;info.uname=info.gname=""
                    tar.addfile(info,io.BytesIO(content))
    return buf.getvalue()

def inspect(data,expected):
    with tarfile.open(fileobj=io.BytesIO(data),mode="r:gz") as tar:
        members=tar.getmembers()
        if len(members)!=12 or len({m.name for m in members})!=12:reject("MISSING_OR_DUPLICATED_PACKAGE_MEMBER")
        if any(not m.isfile() or ".." in m.name.split("/") or m.name.startswith("/") for m in members):reject("TAR_PATH_TRAVERSAL")
        files={m.name:tar.extractfile(m).read() for m in members}
    for item in expected:
        p=item["path"];g=item["guid"]
        if not p.startswith(ROOT+"/") or g!=guid(p):reject("FOREIGN_PATH_OR_GUID")
        for suffix,blob in (("asset",item["asset"]),("asset.meta",item["meta"]),
                            ("pathname",(p+"\n").encode())):
            if files.get(g+"/"+suffix)!=blob:reject("ORIGINAL_ASSET_META_OR_PATH_CORRUPTION:"+p+":"+suffix)
        if ("guid: "+g+"\n").encode() not in item["meta"]:reject("GUID_EMBED_MISMATCH")
    if set(files)!={e["guid"]+"/"+k for e in expected for k in ("asset","asset.meta","pathname")}:
        reject("UNEXPECTED_PACKAGE_FILES")
    return {"actual_original_asset_count":len(expected),"original_binary_fbx_count":3,
      "actual_tar_member_count":len(files),"unique_stable_meta_guids":len(set(e["guid"] for e in expected))}

def mutators(original):
    variants=[]
    for label,index,field in [
        ("actual_original_FBX_corruption",0,"asset"),
        ("wrong_stable_Unity_GUID",0,"meta"),
        ("changed_original_import_path",1,"path"),
    ]:
        test=copy.deepcopy(original)
        if field=="asset":
            data=bytearray(test[index]["asset"]);data[350]^=0x40;test[index]["asset"]=bytes(data)
        elif field=="meta":
            test[index]["meta"]=test[index]["meta"].replace(b"guid: ",b"guid: forged")
        elif field=="path":
            test[index]["path"]=test[index]["path"].replace(ROOT,"Assets/CodexForeign")
        variants.append((label,test))
    variants.append(("missing_original_3rd_FBX",copy.deepcopy(original[:-2]+original[-1:])))
    result=[]
    for label,mutant in variants:
        try:inspect(pack(mutant),original)
        except (ValueError,KeyError,AssertionError,tarfile.TarError) as e:
            result.append({"name":label,"result":"EXPECTED_REJECTION","detail":str(e)[:170]})
        else:raise AssertionError("FALSE_GREEN_MUTATED_UNITYPACKAGE:"+label)
    bad=bytearray(pack(original));bad[31]^=0x61
    try:inspect(bytes(bad),original)
    except (ValueError,KeyError,AssertionError,tarfile.TarError,EOFError,OSError) as e:
        result.append({"name":"real_gzip_archive_byte_corruption","result":"EXPECTED_REJECTION","detail":str(e)[:170]})
    else:raise AssertionError("FALSE_GREEN_CORRUPTED_GZIP_BINARY")
    return result

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--fbxdir",type=Path,required=True)
    parser.add_argument("--roundtrip",type=Path,required=True)
    parser.add_argument("--out",type=Path,required=True)
    a=parser.parse_args()
    a.out.mkdir(parents=True,exist_ok=True)
    expected=get_inputs(a.fbxdir,a.roundtrip)
    binary=pack(expected)
    checks=inspect(binary,expected)
    name="B01-30-P17-ORIGINAL-VISUAL-FBX-UNITY-INTAKE.unitypackage"
    output=a.out/name
    output.write_bytes(binary)
    negatives=mutators(expected)
    if pack(expected)!=binary:reject("NONDETERMINISTIC_PACKAGE_BINARY_REPLAY")
    report={"package_id":PACKAGE,"status":"REAL_ORIGINAL_3D_FBX_UNITYPACKAGE_SOURCE_G0_PRODUCER_PASS",
        "input_glb_pr":423,"input_glb_sha":"60a453e4ecd915725bc2fac5433ba7ad43a628cb",
        "source_B0123_pr":375,"source_B0123_sha":"5666a39eabd4cea91b2b605a9eb99ea2f1e226e8",
        **checks,"unitypackage":name,"unitypackage_bytes":len(binary),"unitypackage_sha256":sha(binary),
        "assets":[{"path":x["path"],"kind":x["kind"],"bytes":len(x["asset"]),
                   "asset_sha256":sha(x["asset"]),"guid":x["guid"]} for x in expected],
        "real_corrupt_source_and_tar_negative_controls":negatives,"byte_identical_replay":True,
        "unity_native":"NOT_RUN","independent_B01_40_60_QA":"PENDING","codex_ack":"NOT_SENT"}
    (a.out/"unity-source-intake-receipt.json").write_text(json.dumps(report,indent=2,sort_keys=True)+"\n")
    print("B0130_P17_W09_REAL_3_FBX_UNITYPACKAGE_4_ASSET_12_TAR_5_NEGATIVES_PASS",
          json.dumps({"package_bytes":len(binary),"unity_assets":4,
                      "tar_members":12,"negative":len(negatives),"sha":sha(binary)}))
if __name__=="__main__":main()
