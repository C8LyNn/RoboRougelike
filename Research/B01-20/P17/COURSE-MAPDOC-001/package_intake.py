#!/usr/bin/env python3
"""COURSE-MAPDOC-001/R2: deterministic, FAIL-CLOSED Codex import-only MAPDOC archive.

Original source generator, map converter, MapDraft and saved-game files are NEVER changed.
The archive deliberately contains ONLY 18 native MapDocument v2 JSON files. The 18
non-native checkpoint sidecars stay outside the archive to prevent editor mis-import.
Run only AFTER the original Codex MapDraft C# receiver has accepted those files.
"""
import hashlib
import json
import os
from pathlib import Path
import re
import stat
import sys
import zipfile

PKG="COURSE-MAPDOC-001"
ORIGINAL_MAPGEN="5be31039411f89f17654b1c332f8692883fa03cc"
ORIGINAL_MAPDRAFT_BLOB="0331a54a1545ecd1ea18b616ef13fed113474b8c"
VARIANTS=("A-SLALOM","B-CHICANE","C-ENDURANCE")
SEEDS=(7,31,83,109,202,407)
ALLOW={f"P17-COURSE-{v}-seed-{n}-mapdocument-v2.json"
       for v in VARIANTS for n in SEEDS}
ZIP_NAME="codex-import-only-18-mapdocs.zip"
RECEIPT_NAME="codex-import-only-receipt.json"

class IntakeRejected(ValueError):
    pass
def require(value,why):
    if not value:raise IntakeRejected("COURSE_MAPDOC_R2_FAIL_CLOSED:"+why)
def digest(b):return hashlib.sha256(b).hexdigest()
def readbytes(base,name):
    require(name==Path(name).name and not name.startswith(".") and
            "/" not in name and "\\" not in name,"UNSAFE_FILENAME")
    f=base/name
    require(f.is_file() and not f.is_symlink(),"MISSING_OR_SYMLINK:"+name)
    return f.read_bytes()
def readjson(base,name):
    try:return json.loads(readbytes(base,name))
    except (ValueError,UnicodeError) as e:raise IntakeRejected("INVALID_JSON:"+name) from e
def validate_doc(m):
    require(type(m) is dict and m.get("version")==2 and m.get("globalFloor") is False,
            "NOT_NATIVE_MAPDOCUMENT_V2")
    items=m.get("items")
    require(type(items) is list and 4<=len(items)<=4002,"MAP_ITEM_COUNT")
    ground=solid=spawn=finish=0
    seen=set()
    for it in items:
        require(type(it) is dict,"INVALID_ITEM_OBJECT")
        kind=it.get("kind")
        require(type(kind) is int and kind in (0,1,2,3,4,5,8),"UNSUPPORTED_KIND")
        x,z=it.get("x"),it.get("z")
        require(type(x) is int and type(z) is int and abs(x)<=50 and abs(z)<=50,
                "INVALID_POSITION")
        require((it.get("rotation"),it.get("scaleX"),it.get("scaleY"),it.get("scaleZ"))==
                (0,1,1,1),"UNSUPPORTED_ITEM_TRANSFORM")
        require((kind,x,z) not in seen,"DUPLICATE_ITEM")
        seen.add((kind,x,z))
        if kind==0:ground+=1
        else:solid+=1
        if kind==4:spawn+=1
        if kind==5:finish+=1
    require(ground>0 and ground<=2000 and solid<=2000,"NATIVE_ITEM_BUDGET")
    require(spawn==1 and finish==1,"MISSING_SPECIAL_ITEMS")
    return {"ground":ground,"solidIncludingSpawnFinish":solid,
            "spawn":spawn,"finish":finish}
def load_authoritative_maps(folder):
    require(folder.is_dir() and not folder.is_symlink(),"INVALID_INPUT_DIRECTORY")
    receipt=readjson(folder,"receipt.json")
    original=readjson(folder,"original-codex-load-receipt.json")
    require(receipt.get("packageId")==PKG and receipt.get("sourceMapgen")==ORIGINAL_MAPGEN
            and receipt.get("maps")==18,"SOURCE_RECEIPT_MISMATCH")
    require(original.get("packageId")==PKG and
            original.get("originalMapDraftGitBlob")==ORIGINAL_MAPDRAFT_BLOB and
            original.get("mapsAcceptedByActualOriginalCodexMapDraft")==18 and
            original.get("originalCsharpRejectAndTestErrorCases")==6 and
            original.get("unityEditMode")=="NOT_RUN","ORIGINAL_CODEX_CSHARP_NOT_VERIFIED")
    entries=receipt.get("converted")
    require(type(entries) is list and len(entries)==18,"RECEIPT_ENTRY_COUNT")
    maps={}
    sidecar_pin={}
    for e in entries:
        require(type(e) is dict and type(e.get("map")) is str,"BAD_RECEIPT_ENTRY")
        name=e["map"]
        require(name in ALLOW and name not in maps,"UNKNOWN_OR_DUPLICATE_DOCUMENT:"+name)
        require(type(e.get("sidecar")) is str and
                e["sidecar"]==name.replace("-mapdocument-v2.json","-checkpoint-sidecar.json"),
                "NON_NATIVE_SIDECAR_NAME_MISMATCH")
        b=readbytes(folder,name)
        require(digest(b)==e.get("mapSha256"),"SOURCE_MAP_BYTES_MISMATCH:"+name)
        doc=readjson(folder,name)
        counts=validate_doc(doc)
        source_counts=e.get("counts") or {}
        require(counts["ground"]==source_counts.get("ground") and
                counts["solidIncludingSpawnFinish"]==source_counts.get("solid",0)+2,
                "SOURCE_COUNTS_CHANGED:"+name)
        side=readbytes(folder,e["sidecar"])
        require(digest(side)==e.get("sidecarSha256"),"NON_NATIVE_SIDECAR_HASH_CHANGED")
        sidej=readjson(folder,e["sidecar"])
        require(sidej.get("sourceMapgenSha")==ORIGINAL_MAPGEN and
                sidej.get("sourceGeometryHash")==e.get("sourceGeometryHash") and
                sidej.get("nativeUnity")=="NOT_RUN" and
                sidej.get("variantId")==e.get("variant") and
                sidej.get("seed")==e.get("seed"),"SIDECAR_PROVENANCE_MISMATCH")
        require(len(sidej.get("checkpointMarkersNonNative",[])) in (3,4,5),
                "SIDECAR_CHECKPOINT_COUNT")
        maps[name]=b
        sidecar_pin[name]=e.get("sidecarSha256")
    require(set(maps)==ALLOW,"MISSING_EXPECTED_MAP_SEED_OR_VARIANT")
    extra=[x.name for x in folder.glob("*-mapdocument-v2.json") if x.name not in ALLOW]
    require(not extra,"UNRECEIPTED_SOURCE_MAP_FILES")
    return maps,sidecar_pin
def build(folder):
    maps,sidecars=load_authoritative_maps(folder)
    path=folder/ZIP_NAME
    require(not path.exists() and not path.is_symlink(),"OUTPUT_ZIP_ALREADY_EXISTS")
    for name in sorted(maps):
        require(not ("checkpoint" in name or "/" in name or "\\" in name),"UNSAFE_ZIP_ENTRY")
    try:
        with zipfile.ZipFile(path,"w",compression=zipfile.ZIP_STORED,allowZip64=False) as archive:
            for name in sorted(maps):
                info=zipfile.ZipInfo(name,(1980,1,1,0,0,0))
                info.create_system=3
                info.external_attr=(stat.S_IFREG|0o644)<<16
                info.compress_type=zipfile.ZIP_STORED
                archive.writestr(info,maps[name])
        with zipfile.ZipFile(path,"r") as actual:
            require(actual.testzip() is None,"CORRUPT_ZIP_CRC")
            actual_names=actual.namelist()
            require(actual_names==sorted(maps) and len(actual_names)==18,
                    "UNEXPECTED_ZIP_ENTRIES")
            for name in actual_names:
                require(actual.getinfo(name).date_time==(1980,1,1,0,0,0),
                        "NONDETERMINISTIC_ZIP_TIMESTAMP")
                require(actual.read(name)==maps[name],"ZIP_PAYLOAD_BYTES_DIFFER")
        receipt={"packageId":PKG,"version":"R2_IMPORT_INTAKE_ONLY",
          "originalInputProducerPR":331,
          "originalMapgenSha":ORIGINAL_MAPGEN,
          "originalCodexMapDraftGitBlob":ORIGINAL_MAPDRAFT_BLOB,
          "originalCodexCsharpMapLoads":"18_PASS",
          "archive":ZIP_NAME,"zipSha256":digest(path.read_bytes()),
          "zipMembers":18,
          "zipContainsOnlyNativeMapDocuments":True,
          "zipContainsNonNativeCheckpointSidecars":False,
          "mapDocuments":[{"path":name,"sha256":digest(maps[name]),
                           "nonNativeSidecarOutsideZipSha256":sidecars[name]}
                          for name in sorted(maps)],
          "realUnityEditMode":"NOT_RUN","realUnityPlayMode":"NOT_RUN",
          "realPhysxDrive":"NOT_RUN","codexACK":"PENDING",
          "rollback":"Discard import ZIP without touching game files; any eventual game import requires Codex-owned undo"}
        (folder/RECEIPT_NAME).write_bytes((json.dumps(receipt,sort_keys=True,
                 separators=(",",":"),ensure_ascii=False)+"\n").encode("utf-8"))
    except BaseException:
        path.unlink(missing_ok=True)
        raise
    print(f"COURSE_MAPDOC_R2_IMPORT_ONLY_ZIP_PASS=18 SHA256={receipt['zipSha256']} UNITY=NOT_RUN")
    return receipt
if __name__=="__main__":
    if len(sys.argv)!=2:raise SystemExit("Usage: package_intake.py original-MapDraft-CSharp-accepted-directory")
    try:build(Path(sys.argv[1]))
    except (IntakeRejected,OSError,KeyError,TypeError,zipfile.BadZipFile) as e:
        print("COURSE_MAPDOC_R2_FAIL_CLOSED "+str(e),file=sys.stderr)
        raise SystemExit(2)
