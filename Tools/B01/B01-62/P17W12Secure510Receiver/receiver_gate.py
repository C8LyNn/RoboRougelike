#!/usr/bin/env python3
"""Independent B01-62 #510 exact-source ZIP and receiver preflight.

Read-only for real receivers. Stage/rollback is allowed solely on a new
caller-owned disposable scratch directory, never an existing game project.
No claims of real Unity/PhysX execution or save schema migration.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import zipfile

SOURCE_SHA = "d94400d95d13db7f27e1533df05c9292d5380431"
ZIP_SHA = "d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387"
OLD_W07_GUID = "b017d070000000000000000000000008"
V2_GUID = "b01a0a10000000000000000000000009"
MANIFEST = "PACKAGE_MANIFEST.json"
ASSET = "Assets/B0123SecureReattachV2"
OWNED = ("Assets/B0123SecureReattachV2/", "Assets/B0123SecureReattachV2.meta",
         "Tools/B01/B01-23/P17W10SecureReattachV2/",
         "Tools/B01/B01-23/P17W11DockAtomicR2/",
         ".github/workflows/b01-23-p17w10-secure-reattach-v2.yml",
         ".github/workflows/b01-23-p17w11-atomic-dock-r2.yml")
FORBIDDEN_PATH = ("B0123DebrisPlayground001", "P17W10PublicDockGuardR2",
                  "P17W11SecureV2RearmAtomicity")
LEGACY_TEXT = ("DebrisRigPlayground", "B0123DebrisPlayground001",
               "P17W10PublicDockGuardR2", "P17W11SecureV2RearmAtomicity")

class Hold(ValueError):
    pass

def sha(b):
    return hashlib.sha256(b).hexdigest()

def git_source(repo, path):
    return subprocess.check_output(["git", "-C", str(repo), "show", SOURCE_SHA+":"+path])

def safe_name(n):
    return bool(n) and not n.startswith("/") and "\\" not in n and all(
        part not in ("", ".", "..") for part in n.split("/")) and "\x00" not in n

def load_bundle(archive, source_repo):
    raw=Path(archive).read_bytes()
    if sha(raw)!=ZIP_SHA or len(raw)!=99666:
        raise Hold("WRONG_IMMUTABLE_ZIP_SHA_OR_SIZE")
    with zipfile.ZipFile(archive) as z:
        infos=z.infolist()
        names=[x.filename for x in infos]
        if len(names)!=35 or len(set(n.casefold() for n in names))!=35 or not all(map(safe_name,names)):
            raise Hold("DUPLICATE_OR_UNSAFE_SOURCE_ARCHIVE")
        if any(x.is_dir() or x.flag_bits & 1 or (x.external_attr>>16 & 0o170000) not in (0,0o100000) for x in infos):
            raise Hold("UNEXPECTED_ARCHIVE_ENTRY_TYPE")
        data={name:z.read(name) for name in names}
    if MANIFEST not in data: raise Hold("MISSING_MANIFEST")
    m=json.loads(data[MANIFEST])
    if not(m.get("source_sha")==SOURCE_SHA and m.get("version")=="2.0.0"
        and m.get("owned_meta_guids")==14 and m.get("codex_ack") is False
        and m.get("native_unity")=="NOT_RUN"
        and m.get("parent_w06_sha")=="cb2b3a76e84266e2730fffc4ab048c1039f0652f"):
        raise Hold("WRONG_PROVENANCE_OR_UNJUSTIFIED_GATES")
    records=m.get("files",[])
    if len(records)!=34 or set(x["path"] for x in records)!=set(data)-{MANIFEST}:
        raise Hold("INVALID_SOURCE_MEMBER_SET")
    guids=[]
    asm={}
    for record in records:
        path=record["path"]
        if not any(path==p or path.startswith(p) for p in OWNED):
            raise Hold("NON_SELECTED_OWNER_PATH:"+path)
        if any(t in path for t in FORBIDDEN_PATH):
            raise Hold("LEGACY_OR_ALTERNATIVE_PATH:"+path)
        b=data[path]
        if record.get("bytes")!=len(b) or record.get("sha256")!=sha(b):
            raise Hold("BAD_PER_FILE_HASH:"+path)
        if git_source(source_repo,path)!=b:
            raise Hold("NOT_SELECTED_GIT_BYTES:"+path)
        if path.endswith(".meta"):
            found=re.findall(rb"(?m)^guid: ([0-9a-f]{32})$",b)
            if len(found)!=1:raise Hold("INVALID_UNITY_META:"+path)
            guids.extend(x.decode() for x in found)
        if path.endswith(".asmdef"):
            info=json.loads(b)
            asm[info["name"]]=info
    if len(guids)!=14 or len(set(guids))!=14 or V2_GUID not in guids or OLD_W07_GUID in guids:
        raise Hold("GUID_COLLISION_OR_LEGACY_GUID")
    expect={
        "B0123.SecureReattachV2.Runtime": ["B0123.DebrisRepulse.Runtime"],
        "B0123.SecureReattachV2.Editor": ["B0123.SecureReattachV2.Runtime"],
        "B0123.SecureReattachV2.PlayMode": ["B0123.SecureReattachV2.Runtime"]
    }
    if {k:v.get("references") for k,v in asm.items()}!=expect:
        raise Hold("BAD_ASMDEF_DEPENDENCY_GRAPH")
    if b"controller.AddComponent<SecureDebrisPlaygroundV2>()" not in data[
        ASSET+"/Editor/SecureReattachSceneBuilder.cs"]:
        raise Hold("NON_V2_SCENE_BUILDER")
    if b"AddComponent<DebrisRigPlayground>" in data[
        ASSET+"/Editor/SecureReattachSceneBuilder.cs"]:
        raise Hold("LEGACY_SCENE_BUILDER")
    return data

def receiver_preflight(receiver, package):
    """Read only. No assumptions about actual Codex receiver: caller supplies path."""
    root=Path(receiver)
    if not root.is_dir() or root.is_symlink(): return ["RECEIVER_UNVERIFIED"]
    conflicts=[]
    owned={p.casefold() for p in package if p!=MANIFEST}
    for file in root.rglob("*"):
        if file.is_symlink():
            conflicts.append("SYMLINK:"+file.relative_to(root).as_posix());continue
        if not file.is_file():continue
        rel=file.relative_to(root).as_posix()
        if rel.casefold() in owned:
            conflicts.append("WOULD_OVERWRITE_PACKAGE_PATH:"+rel)
        if file.suffix.lower() in (".unity",".prefab",".asset",".cs",".asmdef",".meta"):
            # Treat unreadable/oversized resource as unknown; never allow migration.
            if file.stat().st_size>8_000_000:
                conflicts.append("UNINSPECTED_RESOURCE:"+rel);continue
            text=file.read_text(encoding="utf-8",errors="replace")
            if OLD_W07_GUID in text or any(term in text for term in LEGACY_TEXT):
                conflicts.append("LEGACY_OR_ALTERNATIVE_REFERENCE:"+rel)
    return sorted(set(conflicts))

def stage_scratch(package, path):
    """Stage ONLY to a brand new caller-owned disposable location, never a receiver."""
    root=Path(path)
    if root.exists() or root.is_symlink() or not root.parent.is_dir():
        raise Hold("SCRATCH_STAGE_MUST_BE_NEW")
    root.mkdir()
    try:
        for name,b in package.items():
            if not safe_name(name):raise Hold("INVALID_STAGED_NAME")
            dest=root/name
            dest.parent.mkdir(parents=True,exist_ok=True)
            dest.write_bytes(b)
    except Exception:
        shutil.rmtree(root)
        raise

def rollback_scratch(package,path):
    root=Path(path)
    if not root.is_dir() or root.is_symlink(): raise Hold("SCRATCH_NOT_SAFE")
    observed={}
    for child in root.rglob("*"):
        if child.is_symlink():raise Hold("FOREIGN_SYMLINK")
        if child.is_file():
            observed[child.relative_to(root).as_posix()]=child.read_bytes()
        elif not child.is_dir():
            raise Hold("FOREIGN_ENTRY")
    if set(observed)!=set(package):raise Hold("FOREIGN_OR_MISSING_FILES")
    if any(observed[k]!=v for k,v in package.items()):
        raise Hold("DIRTY_SCRATCH_NO_ROLLBACK")
    shutil.rmtree(root)

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--archive",required=True)
    p.add_argument("--source-repo",required=True)
    p.add_argument("--receiver",required=True)
    p.add_argument("--receipt",required=True)
    a=p.parse_args()
    target=Path(a.receipt)
    if target.exists() or not target.parent.is_dir():raise Hold("RECEIPT_WRITE_ONCE")
    package=load_bundle(a.archive,a.source_repo)
    conflicts=receiver_preflight(a.receiver,package)
    result={
        "schema":"B01-62-P17W12-RECEIVER-PREFLIGHT-v1",
        "package_source_sha":SOURCE_SHA, "package_zip_sha256":ZIP_SHA,
        "members":len(package),"guid_count":14,"asmdefs":3,
        "selected":"#510","alternatives":["#511 HOLD","#494 HOLD"],
        "receiver":"EXPLICIT_DISPOSABLE_FIXTURE_NOT_CANONICAL",
        "preflight":"HOLD" if conflicts else "SOURCE_PACKAGE_RECEIVER_PATHS_CLEAR_ONLY",
        "conflicts":conflicts,"native_unity":"NOT_RUN",
        "live_save_load_schema":"NOT_VERIFIED",
        "codex_ack":"NONE","integrated":False,
        "formal_g0":"B01_40_REQUIRED"
    }
    data=(json.dumps(result,indent=2,sort_keys=True)+"\n").encode()
    target.write_bytes(data)
    print("B01_62_W12_PREFLIGHT="+result["preflight"])
    print("B01_62_W12_RECEIPT_SHA256="+sha(data))
    return 3 if conflicts else 0
if __name__=="__main__":
    raise SystemExit(main())
