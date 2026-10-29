#!/usr/bin/env python3
"""B01-30 M04-UNITY-INTAKE-001.
Real deterministic Unity custom package from frozen actual M04-024 FBX #386.
NO mesh regeneration, gameplay edits, Unity install, or new MODEL_JOB.
Native Unity imported-state is independently inspectable using provided C#.
"""
from __future__ import annotations
import argparse
import gzip
import hashlib
import io
import json
import struct
import tarfile
from pathlib import Path

PACKAGE_ID="B01-30-M04-UNITY-INTAKE-001"
SOURCE_PR=386
SOURCE_COMMIT="82ea17a6f964aa0cf08890c827b038ad9a0fc57f"
SOURCE_ACTION_RUN=40900769565
SOURCE_FBX_SHA256="a017534047b9db6a1001b32b186809b796d4e6562f1c9ea560042de97cf58cd3"
SOURCE_FBX_BYTES=2109580
ROOT="Assets/B0130M04Intake001"
FBX_NAME="M04-024-4GIMBALS-ORIGINAL.fbx"
HERE=Path(__file__).resolve().parent

def digest(data:bytes)->str:
    return hashlib.sha256(data).hexdigest()
def guid(path:str)->str:
    return digest((PACKAGE_ID+"|"+path).encode("utf8"))[:32]

def meta(path:str,kind:str)->bytes:
    # File-format + canonical GUID are sufficient for Unity to generate
    # importer defaults. Explicit setup/verification happens via editor menu.
    header=f"fileFormatVersion: 2\nguid: {guid(path)}\n"
    if kind=="fbx":
        # Avoid hand-forged fragile Unity-version-specific ModelImporter YAML.
        return header.encode()
    if kind=="cs":
        return (header+"MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n"
                "  defaultReferences: []\n  executionOrder: 0\n"
                "  icon: {instanceID: 0}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()
    return (header+"TextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()

def assets_from_fbx(fbx:Path,source_receipt:Path,reimport_receipt:Path)->list[dict]:
    data=fbx.read_bytes()
    assert len(data)==SOURCE_FBX_BYTES,(len(data),SOURCE_FBX_BYTES)
    assert digest(data)==SOURCE_FBX_SHA256,"WRONG_SOURCE_FBX_BINARY_SHA256"
    assert data.startswith(b"Kaydara FBX Binary  \x00\x1a\x00"),"NOT_REAL_BINARY_FBX"
    # Blender-written FBX version; nonzero and plausibly 7.x.
    assert 7000<=struct.unpack_from("<I",data,23)[0]<8000
    source=json.loads(source_receipt.read_text("utf8"))
    accepted=json.loads(reimport_receipt.read_text("utf8"))
    assert source["id"]=="B01-30-M04-FBX-RECEIVER-001"
    assert source["source_original_owner_commit"]=="eef02864b106546b200011a2f0286fe7d9a83a1f"
    assert source["fbx_sha256"]==SOURCE_FBX_SHA256
    assert source["fbx_bytes"]==SOURCE_FBX_BYTES
    assert source["original_mesh_count"]==105
    assert accepted["result"]=="REAL_BLENDER_FBX_REIMPORT_AND_FOUR_PIVOT_MOTION_PASS"
    assert accepted["source_fbx_sha256"]==SOURCE_FBX_SHA256
    assert accepted["actual_imported_mesh_count"]==105
    assert accepted["four_real_imported_animated_pivots"]==4
    for n in ("N01_GIMBAL_PIVOT","N02_GIMBAL_PIVOT","N03_GIMBAL_PIVOT","N04_GIMBAL_PIVOT"):
        p=accepted["real_imported_animations"][n]
        assert 7<float(p["frame1_to31_world_quaternion_angle_deg"])<15
        assert 7<float(p["frame1_to91_world_quaternion_angle_deg"])<15
    model=ROOT+"/"+FBX_NAME
    installer=ROOT+"/Editor/M04CandidateImportCheck.cs"
    readme=ROOT+"/README.txt"
    readme_data=(
        "B01-30 / M04-UNITY-INTAKE-001 (source pinned #386)\n"
        "Contains ORIGINAL animated M04-024 FBX, no new model or source changes.\n"
        "Unity 6 + URP: Tools > B01 > M04-024 > Prepare and check candidate FBX\n"
        "Editor check: imported 4 named pivots, animation curves in a COMMON clip,\n"
        "105 visual MeshFilter targets, model scale and original source SHA.\n"
        "Material assignment/URP look and native import acceptance NOT_RUN.\n"
        "No gameplay changes; QA gate B01-40 and B01-60 before Codex use.\n"
        "Rollback: delete only Assets/B0130M04Intake001 after restoring references.\n"
    ).encode()
    pairs=[(model,data,"fbx"),(installer,(HERE/"M04CandidateImportCheck.cs").read_bytes(),"cs"),
           (readme,readme_data,"text")]
    return [{"path":path,"asset":data,"meta":meta(path,kind),
             "guid":guid(path),"sha256":digest(data),"kind":kind}
            for path,data,kind in sorted(pairs,key=lambda t:t[0])]

def pack(assets:list[dict])->bytes:
    blob=io.BytesIO()
    with gzip.GzipFile(fileobj=blob,mode="wb",mtime=0,filename="",compresslevel=9) as compressed:
        with tarfile.open(fileobj=compressed,mode="w",format=tarfile.USTAR_FORMAT) as archive:
            for asset in assets:
                g=asset["guid"]
                for kind,content in (("asset",asset["asset"]),
                                      ("asset.meta",asset["meta"]),
                                      ("pathname",(asset["path"]+"\n").encode())):
                    entry=tarfile.TarInfo(g+"/"+kind)
                    entry.size=len(content)
                    entry.uid=entry.gid=entry.mtime=0
                    entry.mode=0o644
                    entry.uname=entry.gname=""
                    archive.addfile(entry,io.BytesIO(content))
    return blob.getvalue()

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--fbx",required=True)
    ap.add_argument("--source-receipt",required=True)
    ap.add_argument("--reimport-receipt",required=True)
    ap.add_argument("--out",required=True)
    args=ap.parse_args()
    out=Path(args.out);out.mkdir(parents=True,exist_ok=True)
    files=assets_from_fbx(Path(args.fbx),Path(args.source_receipt),
                          Path(args.reimport_receipt))
    assert len(files)==3 and len({x["guid"] for x in files})==3
    raw=pack(files)
    target=out/"M04-024-SOURCE-LOCKED-UNITY-INTAKE-001.unitypackage"
    target.write_bytes(raw)
    report={
        "package_id":PACKAGE_ID,"version":"R1",
        "status":"REAL_BINARY_UNITYPACKAGE_BUILT_UNITY_NATIVE_NOT_RUN",
        "producer_pr":SOURCE_PR,"producer_source_sha":SOURCE_COMMIT,
        "producer_actions_run":SOURCE_ACTION_RUN,
        "producer_exact_fbx_sha256":SOURCE_FBX_SHA256,
        "original_fbx_bytes":SOURCE_FBX_BYTES,
        "unitypackage":target.name,"unitypackage_bytes":len(raw),
        "unitypackage_sha256":digest(raw),
        "assets":[{"path":x["path"],"guid":x["guid"],
                   "asset_bytes":len(x["asset"]),"asset_sha256":x["sha256"],
                   "kind":x["kind"]} for x in files],
        "unity_candidate_asset_root":ROOT,
        "not_run":["native Unity 6000.3.14f1 FBX import",
                   "native Unity Editor script compile",
                   "Unity imported animation curves and scale",
                   "URP material parity", "G1 independent B01-60", "Codex integration"]
    }
    (out/"m04-unity-intake-receipt.json").write_text(json.dumps(report,indent=2)+"\n")
    print("M04_UNITY_INTAKE_REAL_SOURCE_LOCKED_UNITYPACKAGE_CREATED",
          json.dumps({"assets":len(files),"archive_bytes":len(raw),
                      "source_fbx_bytes":SOURCE_FBX_BYTES,
                      "archive_sha256":digest(raw)},sort_keys=True))
if __name__=="__main__":
    main()
