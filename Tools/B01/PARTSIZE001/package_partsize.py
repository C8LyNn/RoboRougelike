#!/usr/bin/env python3
"""B01-PARTSIZE-001 R1 immutable, scoped, SHA256-manifested FULL source ZIP.
Runs only on a Git checkout. Does not install into Codex or certify Unity.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path, PurePosixPath
import re
import subprocess
import zipfile

ROOT=Path(__file__).resolve().parents[3]
MAIN_SHA="5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
SCOPES=("Assets/PARTSIZE001/","Assets/PARTSIZE001.meta",
        "Tools/B01/PARTSIZE001/","Docs/B01/PARTSIZE001/")
SIBLING_META="Assets/PARTSIZE001.meta"
MANIFEST="PARTSIZE001_SOURCE_MANIFEST.json"
G2="HOLD"

def sha(data):return hashlib.sha256(data).hexdigest()

def valid_path(path):
    if not isinstance(path,str) or not path or "\\" in path or ":" in path:
        return False
    p=PurePosixPath(path)
    return (not p.is_absolute() and str(p)==path and
            not any(x in (".","..","") for x in path.split("/")) and
            any(path.startswith(x) for x in SCOPES))

def source_paths():
    lines=subprocess.check_output(["git","ls-files","--",
        "Assets/PARTSIZE001","Assets/PARTSIZE001.meta",
        "Tools/B01/PARTSIZE001","Docs/B01/PARTSIZE001"],cwd=ROOT).decode().splitlines()
    return sorted(lines)

def committed_bytes(path):
    """Canonical Git HEAD blob bytes, never checkout-smudged CRLF bytes.

    Source package is immutable per committed SHA on Windows and Linux.
    It does NOT package dirty/uncommitted worktree edits.
    """
    if not valid_path(path):
        raise ValueError("BAD_OR_OUT_OF_SCOPE_BLOB_PATH")
    return subprocess.check_output(["git","show","HEAD:"+path],cwd=ROOT)


def validate(files):
    if not files or len(files)!=len(set(files)) or not all(valid_path(x) for x in files):
        raise ValueError("BAD_OR_OUT_OF_SCOPE_SOURCE_PATH")
    if SIBLING_META not in files:raise ValueError("MISSING_PACKAGE_FOLDER_META")
    meta_guids={}
    for path in files:
        data=committed_bytes(path)
        if not data:raise ValueError("EMPTY_FILE:"+path)
        if path.endswith(".meta"):
            txt=data.decode("utf8")
            mat=re.search(r"(?m)^guid: ([0-9a-f]{32})$",txt)
            if not mat:raise ValueError("NO_STABLE_UNITY_GUID:"+path)
            guid=mat.group(1)
            if guid in meta_guids:raise ValueError("COLLIDING_UNITY_GUID:"+guid)
            meta_guids[guid]=path
        elif path.startswith("Assets/") and path+".meta" not in files:
            raise ValueError("MISSING_ASSET_META:"+path)
    for folder in ["Runtime","Demo","Tests","Tests/EditMode","Tests/PlayMode"]:
        if "Assets/PARTSIZE001/"+folder+".meta" not in files:
            raise ValueError("MISSING_FOLDER_META:"+folder)
    if len(meta_guids)<12:raise ValueError("TOO_FEW_GUIDS")
    if "Assets/B01Packages.meta" in files:raise ValueError("SHARED_ROOT_META_COLLISION")
    return meta_guids

def build(files,source_sha):
    guids=validate(files)
    manifest={
       "schema":"B01_PARTSIZE_SOURCE_MANIFEST_R1",
       "package_id":"B01-PARTSIZE-001","version":"R1",
       "producer":"B01-23","state":"IN_PROGRESS_NOT_OFFERED",
       "source_sha":source_sha,"source_base_sha":MAIN_SHA,
       "canonical_reference_sha":"88b8eed60345c49481648833e1fde6dc1a400258",
       "canonical_source_observations":{
         "R3VehicleModel_cs_git_blob":"cdd75fff4923a382b16c430dac1452320eda88f6",
         "R3BonusModuleGeometry_cs_git_blob":"99f55a2002a80603f1707466d7a0066260d5c3ee",
         "R41PartGeometry_cs_git_blob":"76a14de4c347dd3e91b0a37487d804023eab5da1",
         "grid_m":.55,"block_m":.52,"wide_colliders_m":[1.07,.52,1.07]
       },
       "native_unity":"NOT_RUN","independent_B01_60":"NOT_RUN",
       "codex_target_sha":None,"CODEX_ACK":None,"G2":G2,
       "note":"Real C# geometry portable checks are NOT Unity or actual PhysX. Source-derived reference fixture is not a live measured wheel/gun model.",
       "files":[{"path":path,"bytes":len(committed_bytes(path)),
                 "sha256":sha(committed_bytes(path))} for path in files],
       "guid_count":len(guids),"source_file_count":len(files)
    }
    out=io.BytesIO()
    # ZIP_STORED avoids zlib-version differences; fixed creator system avoids
    # platform-dependent central-directory flags on Windows vs Linux.
    with zipfile.ZipFile(out,"w",compression=zipfile.ZIP_STORED) as archive:
        for path in files:
            zi=zipfile.ZipInfo(path,date_time=(2026,10,27,8,0,0))
            zi.compress_type=zipfile.ZIP_STORED
            zi.create_system=3
            zi.external_attr=0o100644<<16
            archive.writestr(zi,committed_bytes(path),compress_type=zipfile.ZIP_STORED)
        zi=zipfile.ZipInfo(MANIFEST,date_time=(2026,10,27,8,0,0))
        zi.compress_type=zipfile.ZIP_STORED
        zi.create_system=3
        zi.external_attr=0o100644<<16
        archive.writestr(zi,(json.dumps(manifest,indent=2,sort_keys=True)+"\n").encode("utf8"),
                         compress_type=zipfile.ZIP_STORED)
    payload=out.getvalue()
    verify_zip(payload,manifest)
    return payload,manifest

def verify_zip(payload,manifest):
    with zipfile.ZipFile(io.BytesIO(payload)) as z:
        if z.testzip()!=None:raise ValueError("ZIP_CRC_ERROR")
        if z.namelist()!=[r["path"] for r in manifest["files"]]+[MANIFEST]:
            raise ValueError("ZIP_ENTRIES_UNEXPECTED")
        embedded=json.loads(z.read(MANIFEST))
        if embedded!=manifest:raise ValueError("ZIP_EMBEDDED_MANIFEST_MISMATCH")
        for f in embedded["files"]:
            if not valid_path(f["path"]) or len(z.read(f["path"]))!=f["bytes"] or sha(z.read(f["path"]))!=f["sha256"]:
                raise ValueError("ZIP_SOURCE_TAMPERING:"+f["path"])
    return True

def main():
    a=argparse.ArgumentParser()
    a.add_argument("--out",default="dist/B01-PARTSIZE001-R1-FULL-SOURCE.zip")
    args=a.parse_args()
    head=subprocess.check_output(["git","rev-parse","HEAD"],cwd=ROOT).decode().strip()
    files=source_paths();data,manifest=build(files,head)
    duplicate,_=build(files,head)
    if data!=duplicate:raise ValueError("NONDETERMINISTIC_PACKAGE")
    target=ROOT/args.out
    target.parent.mkdir(parents=True,exist_ok=True)
    target.write_bytes(data)
    report={"package_id":"B01-PARTSIZE-001","version":"R1",
            "source_sha":head,"status":"SOURCE_PACKAGE_STATIC_VERIFIED_NATIVE_NOT_RUN",
            "source_files":len(files),"asset_guid_count":manifest["guid_count"],
            "zip_entries":len(files)+1,"zip_bytes":len(data),"zip_sha256":sha(data),
            "G2":"HOLD"}
    print(json.dumps(report,sort_keys=True,indent=2))

if __name__=="__main__":main()
