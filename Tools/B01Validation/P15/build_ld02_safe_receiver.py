#!/usr/bin/env python3
"""B01-40 PASS15: source-pinned, fail-closed MapDocument v2 staging, NOT Unity import.

No source edits. The only optional install destination is an explicitly marked,
separate disposable test root, never Application.persistentDataPath/Maps.
No --force and no overwrite. --cleanup-from deletes ONLY our exact installed bytes.
"""
import argparse
from collections import Counter
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import sys
import zipfile

OWNER_SHA = "f6d6886f45b1766f9b7a9c2975e1bd1deb3fdb84"
MAP_PATH = "Research/B01-25/P14/LD02_ARENA.json"
MAP_BLOB = "e39e2f174984832bd97311c050e3c22fd6fb1175"
CODEX_SHA = "231005c6d0351945a8ee6684f3856af118f0e94f"
CODEX_SOURCE = {
    "Assets/MapEditor/MapDraft.cs": "b5f8f395f93972c0ede000701dbbada8fcfcdfdb",
    "Assets/MapEditor/MapStorage.cs": "f31a94c16e8cb244e66e39b0ac344b085095b630",
}
MAP_NAME = "B01_25_LD02_ARENA_P14"
MAP_FILE = MAP_NAME + ".json"
MAP_MEMBER = "Maps/" + MAP_FILE
ARCHIVE = "LD02_ARENA_SOURCE_STAGING.zip"
MANIFEST_FILE = "LD02_ARENA_SOURCE_MANIFEST.json"
RECEIPT = ".b01-40-ld02-install-receipt.json"
MARKER = ".b01-40-disposable-test-maps"
MARKER_TEXT = "B01-40-ISOLATED-TEST-MAPS-V1\n"
KINDS = {0:"Ground",1:"Wall",2:"Ramp",3:"Obstacle",4:"Spawn",5:"Finish",
         6:"Platform",7:"Pillar",8:"LowBarrier",9:"Steps"}
EXPECTED = {0:120,1:36,4:1,5:1,6:4,7:4,8:2}

class GuardError(ValueError): pass

def ensure(ok, why):
    if not ok:
        raise GuardError(why)

def git(repo,*args):
    cmd=["git","-C",str(repo),*args]
    p=subprocess.run(cmd,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if p.returncode:
        raise GuardError("GIT_ORIGINAL_OBJECT_UNAVAILABLE:"+p.stderr.decode("utf-8",errors="replace")[:110])
    return p.stdout

def sha256(data):
    return hashlib.sha256(data).hexdigest()

def assert_original_blob(repo,commit,path,blob):
    actual=git(repo,"rev-parse",commit+":"+path).decode().strip()
    ensure(actual==blob,"PINNED_GIT_BLOB_DRIFT:"+path)
    raw=git(repo,"show",commit+":"+path)
    ensure(hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()==blob,
           "GIT_OBJECT_BYTES_TAMPER:"+path)
    return raw

def validate_name(name):
    ensure(bool(name) and name==name.strip() and len(name)<=64,
           "UNSAFE_MAP_NAME")
    ensure(re.fullmatch(r"[A-Za-z0-9_ -]+",name) is not None,"UNSAFE_MAP_NAME")
    ensure(name not in (".",".."),"UNSAFE_MAP_NAME")
    ensure(not re.fullmatch(r"(?:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\..*)?",
                           name,re.IGNORECASE),"WINDOWS_RESERVED_MAP_NAME")
    ensure(PurePosixPath("Maps/"+name+".json").parts==("Maps",name+".json"),"UNSAFE_ZIP_MEMBER")
    return name

def footprint(it):
    x,z,rot=it["x"],it["z"],it["rotation"]
    for dx in range(it["scaleX"]):
        for dz in range(it["scaleZ"]):
            if rot==0: rx,rz=dx,dz
            elif rot==1:rx,rz=dz,-dx
            elif rot==2:rx,rz=-dx,-dz
            else:rx,rz=-dz,dx
            yield x+rx,z+rz

def validate_map(raw):
    ensure(len(raw)<1024*1024,"MAP_EXCEEDS_ORIGINAL_STORAGE_LIMIT")
    try: doc=json.loads(raw)
    except (ValueError,UnicodeDecodeError) as e: raise GuardError("INVALID_MAP_JSON") from e
    ensure(isinstance(doc,dict) and set(doc)=={"version","globalFloor","items"},
           "MAP_DOCUMENT_SCHEMA_UNEXPECTED")
    ensure(type(doc["version"]) is int and doc["version"]==2 and doc["globalFloor"] is False,
           "MAP_VERSION_OR_GLOBAL_FLOOR")
    items=doc["items"]
    ensure(isinstance(items,list) and len(items)==168 and len(items)<=2000,"MAP_ITEM_COUNT")
    count=Counter();layers={True:set(),False:set()}
    spawn=None
    for it in items:
        ensure(isinstance(it,dict) and set(it)=={"kind","x","z","rotation","scaleX","scaleY","scaleZ"},
               "MAP_ITEM_SCHEMA_UNEXPECTED")
        ensure(all(type(it[k]) is int for k in it),"MAP_ITEM_TYPE")
        ensure(it["kind"] in KINDS and 0<=it["rotation"]<=3 and
               all(1<=it[k]<=4 for k in ("scaleX","scaleY","scaleZ")) and
               abs(it["x"])<=50 and abs(it["z"])<=50,"MAP_ITEM_OUT_OF_RANGE")
        cells=set(footprint(it))
        ensure(all(abs(x)<=50 and abs(z)<=50 for x,z in cells),"MAP_FOOTPRINT_OUT_OF_RANGE")
        is_ground=(it["kind"]==0)
        ensure(not (cells & layers[is_ground]),"MAP_LAYER_FOOTPRINT_OVERLAP")
        layers[is_ground].update(cells)
        count[it["kind"]]+=1
        if it["kind"]==4: spawn=cells
    ensure(dict(count)==EXPECTED,"MAP_CONTENT_COUNTS_DRIFT")
    ensure(spawn and spawn.issubset(layers[True]),"SPAWN_GROUND_MISSING")
    return {"item_count":len(items),"kinds":{KINDS[k]:v for k,v in sorted(count.items())},
            "spawn_on_ground":True,"globalFloor":False}

def original_inputs(repo):
    for path,blob in CODEX_SOURCE.items():
        raw=assert_original_blob(repo,CODEX_SHA,path,blob).decode("utf-8")
        if path.endswith("MapDraft.cs"):
            ensure("MaxItems=2000" in raw and "MaxScale=4" in raw and
                   "MaxCoordinate=50" in raw and "version=2" in raw,
                   "ORIGINAL_MAPDRAFT_CONTRACT_DRIFT")
        if path.endswith("MapStorage.cs"):
            ensure('"Maps"' in raw and 'name.Trim()+".json"' in raw and
                   "persistentDataPath" in raw,"ORIGINAL_MAPSTORAGE_CONTRACT_DRIFT")
    data=assert_original_blob(repo,OWNER_SHA,MAP_PATH,MAP_BLOB)
    summary=validate_map(data)
    return data,summary

def manifest(raw,summary):
    return {"format":"B01-40-LD02-SOURCE-ONLY-STAGING-v1",
            "owner_commit":OWNER_SHA,"owner_path":MAP_PATH,"owner_git_blob":MAP_BLOB,
            "map_member":MAP_MEMBER,"map_name":MAP_NAME,
            "map_bytes":len(raw),"map_sha256":sha256(raw),
            "codex_ref":CODEX_SHA,"codex_original_blob_by_path":CODEX_SOURCE,
            "map_source_validation":summary,
            "status":"STAGED_SOURCE_ONLY__UNITY_NOT_RUN__NO_CODEX_ACK"}

def serialize(d):
    return (json.dumps(d,sort_keys=True,separators=(",",":"),ensure_ascii=False)+"\n").encode("utf-8")

def worktree_roots(repo):
    data=git(repo,"worktree","list","--porcelain").decode("utf-8",errors="replace")
    return [Path(row[9:]).resolve() for row in data.splitlines() if row.startswith("worktree ")]

def disjoint(a,b):
    return a!=b and a not in b.parents and b not in a.parents

def guard_output_dir(repo,where):
    path=Path(where)
    ensure(path.is_dir() and not path.is_symlink(),"NOT_AN_EXISTING_REAL_DIRECTORY")
    resolved=path.resolve(strict=True)
    for candidate in (resolved,*resolved.parents):
        ensure(not candidate.is_symlink(),"LINKED_OUTPUT_PATH_COMPONENT")
    for root in worktree_roots(repo):
        ensure(disjoint(resolved,root),"OUTPUT_OVERLAPS_GIT_SOURCE_WORKTREE")
    return resolved

def zip_bytes(raw,meta):
    import io
    out=io.BytesIO()
    with zipfile.ZipFile(out,"w",compression=zipfile.ZIP_STORED) as z:
        for name,data in ((MAP_MEMBER,raw),("source-manifest.json",serialize(meta))):
            info=zipfile.ZipInfo(name,(1980,1,1,0,0,0))
            info.compress_type=zipfile.ZIP_STORED
            info.external_attr=(0o100644<<16)
            z.writestr(info,data)
    return out.getvalue()

def exclusive(path,raw):
    with open(path,"xb") as out:
        out.write(raw)

def stage(repo,destination,raw,meta):
    root=guard_output_dir(repo,destination)
    z=zip_bytes(raw,meta)
    a=root/ARCHIVE;b=root/MANIFEST_FILE
    ensure(not a.exists() and not b.exists() and not a.is_symlink() and not b.is_symlink(),
           "STAGING_OUTPUT_ALREADY_EXISTS")
    try:
        exclusive(a,z)
        exclusive(b,serialize(meta))
    except Exception:
        if a.exists():a.unlink()
        if b.exists():b.unlink()
        raise
    with zipfile.ZipFile(a,"r") as check:
        ensure(check.namelist()==[MAP_MEMBER,"source-manifest.json"],"STAGING_MEMBER_MISMATCH")
        ensure(check.read(MAP_MEMBER)==raw,"STAGING_NOT_ORIGINAL_BYTES")
    return {"staging_zip":str(a),"zip_sha256":sha256(z),"manifest":str(b),
            "zip_members":2,"status":"SOURCE_STAGED_NOT_UNITY_IMPORTED"}

def guard_install_root(repo,where):
    root=guard_output_dir(repo,where)
    marker=root/MARKER
    ensure(marker.is_file() and not marker.is_symlink() and
           marker.read_text(encoding="ascii")==MARKER_TEXT,
           "EXPLICIT_DISPOSABLE_MAPS_MARKER_REQUIRED")
    # Windows Python tempfile roots can legitimately be under AppData/Local/Temp.
    # Admit ONLY the actual OS temp subtree for an explicitly marked disposable
    # test root; reject AppData/LocalLow (Unity persistentDataPath), and any
    # non-temp AppData path. No guessing the user's persistentDataPath.
    import tempfile
    parts={part.casefold() for part in root.parts}
    ensure(not parts.intersection({"persistentdatapath","unityproject","projectsettings","assets","locallow"}),
           "POSSIBLE_LIVE_UNITY_PATH_DENIED")
    if "appdata" in parts:
        temp_root=Path(tempfile.gettempdir()).resolve(strict=True)
        ensure(temp_root in root.parents and root!=temp_root,
               "POSSIBLE_LIVE_UNITY_PATH_DENIED")
    return root

def target_paths(root):
    folder=root/"Maps"
    ensure(not folder.is_symlink(),"MAPS_DESTINATION_SYMLINK")
    file=folder/MAP_FILE
    receipt=root/RECEIPT
    return folder,file,receipt

def collision_check(folder):
    if folder.exists():
        ensure(folder.is_dir() and not folder.is_symlink(),"MAPS_DESTINATION_INVALID")
        for entry in folder.iterdir():
            ensure(not entry.is_symlink(),"DESTINATION_CONTAINS_SYMLINK")
            ensure(entry.name.casefold()!=MAP_FILE.casefold() and
                   entry.stem.strip().casefold()!=MAP_NAME.casefold(),
                   "MAP_NAME_OR_CASEFOLD_COLLISION")

def install(repo,where,raw,meta):
    root=guard_install_root(repo,where)
    folder,file,receipt=target_paths(root)
    ensure(not receipt.exists() and not receipt.is_symlink(),"INSTALL_RECEIPT_COLLISION")
    collision_check(folder)
    ensure(not file.exists() and not file.is_symlink(),"REFUSING_MAP_OVERWRITE")
    made_dir=not folder.exists()
    if made_dir:folder.mkdir()
    try:
        exclusive(file,raw)
        exclusive(receipt,serialize({"format":"B01-40-LD02-DISPOSABLE-RECEIPT-v1",
                          "map_member":MAP_MEMBER,"map_sha256":sha256(raw),
                          "source_commit":OWNER_SHA,"source_blob":MAP_BLOB}))
    except Exception:
        if file.exists() and file.read_bytes()==raw:file.unlink()
        if made_dir and not any(folder.iterdir()):folder.rmdir()
        raise
    ensure(file.read_bytes()==raw,"DISPOSABLE_INSTALL_READBACK_FAILED")
    return {"disposable_install":str(file),"receipt":str(receipt),
            "status":"TEST_MAP_COPIED__PLAYER_NOT_RUN__NO_LIVE_UNITY_PATH"}

def cleanup(repo,where,raw):
    root=guard_install_root(repo,where)
    folder,file,receipt=target_paths(root)
    ensure(receipt.is_file() and not receipt.is_symlink() and
           file.is_file() and not file.is_symlink(),"CLEANUP_REQUIRES_OWN_RECEIPT_AND_FILE")
    try: meta=json.loads(receipt.read_text(encoding="utf-8"))
    except ValueError as e:raise GuardError("CLEANUP_RECEIPT_INVALID") from e
    ensure(meta=={"format":"B01-40-LD02-DISPOSABLE-RECEIPT-v1",
                 "map_member":MAP_MEMBER,"map_sha256":sha256(raw),
                 "source_commit":OWNER_SHA,"source_blob":MAP_BLOB},"CLEANUP_RECEIPT_MISMATCH")
    ensure(file.read_bytes()==raw,"CLEANUP_REFUSES_MODIFIED_MAP")
    file.unlink();receipt.unlink()
    if not any(folder.iterdir()):folder.rmdir()
    return {"cleanup":"RESTORED_ORIGINAL_DISPOSABLE_EMPTY_STATE",
            "other_test_maps":"UNMODIFIED","status":"NO_LIVE_UNITY_PATH_TOUCHED"}

def run(argv=None):
    ap=argparse.ArgumentParser(description="Exact B01-25 LD02 MapDocument v2 safe staging; Unity NOT_RUN")
    ap.add_argument("--repo",default=".")
    group=ap.add_mutually_exclusive_group()
    group.add_argument("--stage-to",metavar="EXTERNAL_DIRECTORY")
    group.add_argument("--install-to",metavar="DISPOSABLE_TEST_MAPS_DIR")
    group.add_argument("--cleanup-from",metavar="DISPOSABLE_TEST_MAPS_DIR")
    args=ap.parse_args(argv)
    try:
        repo=Path(args.repo).resolve(strict=True)
        validate_name(MAP_NAME)
        raw,summary=original_inputs(repo)
        meta=manifest(raw,summary)
        print(json.dumps({"step":"MANDATORY_DRY_RUN_PASS","source_sha":OWNER_SHA,
                          "git_blob":MAP_BLOB,"sha256":sha256(raw),"summary":summary,
                          "native_unity":"NOT_RUN"},sort_keys=True),flush=True)
        if args.stage_to: result=stage(repo,args.stage_to,raw,meta)
        elif args.install_to:result=install(repo,args.install_to,raw,meta)
        elif args.cleanup_from:result=cleanup(repo,args.cleanup_from,raw)
        else:result={"status":"DRY_RUN_ONLY__NO_WRITES"}
        print(json.dumps(result,sort_keys=True))
        return 0
    except (GuardError,OSError,ValueError,zipfile.BadZipFile) as exc:
        print("B01_40_LD02_FAIL_CLOSED "+str(exc)+" NATIVE_UNITY=NOT_RUN",file=sys.stderr)
        return 2

if __name__=="__main__":sys.exit(run())
