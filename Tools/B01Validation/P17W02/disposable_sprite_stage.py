#!/usr/bin/env python3
"""B01-40 P17/W02: real reversible staging of pinned original B01-32 Sprite PNGs.

No Unity is launched. Never modifies current Git checkout, actual Codex
Assets, live saves or any project without an explicit disposable marker.
The receiver accepts ONLY the exact original CI-built 30-member package.
"""
import argparse
import hashlib
import io
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import zipfile

PACKAGE="B01-40-P17W02-SPRITE-STAGE-001"
INPUT_PACKAGE="B01-40-P17-DECAL-SPRITE-BRIDGE-001"
PRODUCER="829ac8da9db83a49865d24fa05a5143f59d4b83f"
CODEX="be2c4b6ac63e04eff5b046e50641ec8d1da4837f"
ZIP_SHA="1662c46ba75736949c7fb1a5ae20dd057bfd69819d4cb74aee2d53c2b10f987c"
PREFIX="Assets/B01ImportStaging"
ROOT="Assets/B01ImportStaging/B0140DamageDecals"
FRAMES=ROOT+"/Frames"
MARKER=".b0140-disposable-only"
MARKER_BYTES=b"B0140-P17W02-DISPOSABLE-UNITY-STAGING v1\n"
RECEIPT=".b0140-p17w02-install-receipt.json"
STATES=("clean","abraded","fractured","critical")
GUID=re.compile(r"(?m)^guid:[ \t]*([0-9a-f]{32})[ \t]*\r?$")

class Reject(ValueError):pass
def need(b,msg):
    if not b:raise Reject(msg)
def digest(b):return hashlib.sha256(b).hexdigest()
def canonical(o):return (json.dumps(o,sort_keys=True,ensure_ascii=False,separators=(",",":"))+"\n").encode()
def guids(path):return digest((INPUT_PACKAGE+":v1:"+path).encode())[:32]
def expected():
    folders=[PREFIX+".meta",ROOT+".meta",FRAMES+".meta"]
    frames=[FRAMES+"/B0140_"+state+"-"+str(i)+".png" for state in STATES for i in range(3)]
    return folders+frames+[n+".meta" for n in frames]
EXPECTED=expected()

def check_file_archive(archive):
    original=Path(archive).read_bytes()
    need(digest(original)==ZIP_SHA,"ORIGINAL_30_MEMBER_ZIP_SHA_MISMATCH")
    with zipfile.ZipFile(io.BytesIO(original)) as z:
        entries=z.infolist()
        paths=[e.filename for e in entries]
        need(len(entries)==30 and paths==sorted(paths) and len(paths)==len(set(paths)),
             "ORIGINAL_ZIP_MEMBER_INVENTORY_INVALID")
        need(all(not name.startswith("/") and "\\" not in name and ".." not in Path(name).parts
                 and len(name)<210 for name in paths),"ORIGINAL_ZIP_UNSAFE_NAME")
        need(all(e.create_system==3 and e.date_time==(1980,1,1,0,0,0) and
                 (e.external_attr >> 16)==0o100644 and e.file_size<2_000_000
                 for e in entries),"ORIGINAL_ZIP_METADATA_DRIFT")
        need(set(paths)==set(EXPECTED+[
             "Reference/codex-sprite-binding.json",
             "Reference/producer-receipt.json",
             "Reference/part-damage-atlas.png"]),"ORIGINAL_ZIP_FILE_NAMES_UNEXPECTED")
        blobs={name:z.read(name) for name in paths}
    binding=json.loads(blobs["Reference/codex-sprite-binding.json"])
    need(binding.get("package_id")==INPUT_PACKAGE and
         binding.get("owner_original_sha")==PRODUCER and
         binding.get("native_unity")=="NOT_RUN" and
         binding.get("codex_integrated") is False and
         len(binding.get("sprites",[]))==12,"ORIGINAL_IMPORTER_PROVENANCE_INVALID")
    by_path={x["sprite_asset_path"]:x for x in binding["sprites"]}
    need(len(by_path)==12,"ORIGINAL_SPRITE_ID_DUPLICATE")
    for order,state in enumerate(STATES):
        for i in range(3):
            name=FRAMES+"/B0140_"+state+"-"+str(i)+".png"
            meta=name+".meta"
            row=by_path.get(name)
            need(row is not None and row["state"]==state and row["frame"]==i and
                 row["duration_ms"]==160 and row["png_sha256"]==digest(blobs[name]) and
                 row["sprite_meta_guid"]==guids(name),"ORIGINAL_FRAME_BINDING_DRIFT:"+name)
            png=blobs[name]
            need(png[:8]==b"\x89PNG\r\n\x1a\n" and png[12:16]==b"IHDR" and
                 int.from_bytes(png[16:20],"big")==256 and
                 int.from_bytes(png[20:24],"big")==256 and
                 png[25] in (6,),"ORIGINAL_RGBA_PNG_INVALID:"+name)
            text=blobs[meta].decode("utf-8")
            need("spriteMode: 1" in text and "textureType: 8" in text and
                 "alphaIsTransparency: 1" in text and
                 "enableMipMap: 0" in text and "spriteGenerateFallbackPhysicsShape: 0" in text,
                 "UNITY_SPRITE_SINGLE_META_CONTRACT_CHANGED:"+meta)
    all_guids=[]
    for name in EXPECTED:
        if not name.endswith(".meta"):continue
        m=GUID.search(blobs[name].decode("utf-8"))
        need(m is not None and m.group(1)==guids(name[:-5]),"STABLE_META_GUID_INVALID:"+name)
        all_guids.append(m.group(1))
    need(len(all_guids)==15 and len(set(all_guids))==15,"DUPLICATE_SOURCE_GUID")
    return {name:blobs[name] for name in EXPECTED},set(all_guids)

def run_git(repo,*args):
    p=subprocess.run(["git","-C",str(repo),*args],
                     stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    need(p.returncode==0,"CURRENT_CODEX_GIT_UNAVAILABLE:"+p.stderr.decode(errors="replace")[:130])
    return p.stdout

def original_codex_source_guard(repo,new_guids):
    repo=Path(repo).resolve(strict=True)
    need(Path(run_git(repo,"rev-parse","--show-toplevel").decode().strip()).resolve()==repo,
         "REPO_NOT_GIT_ROOT")
    blobpaths=[p.decode() for p in run_git(repo,"ls-tree","-r","--name-only","-z",CODEX).split(b"\x00") if p]
    source={x.casefold() for x in blobpaths}
    need(all(n.casefold() not in source for n in EXPECTED),"CODEX_SPRITE_DESTINATION_ALREADY_EXISTS")
    need(all(not n.casefold().startswith(PREFIX.casefold()+"/") and
                 n.casefold()!=PREFIX.casefold()+".meta" for n in blobpaths),
         "CODEX_CONTAINS_STAGING_PARENT")
    p=subprocess.run(["git","-C",str(repo),"grep","-h","-I","-E",
                      "^guid: [0-9a-f]{32}",CODEX,"--","Assets"],
                     stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    need(p.returncode in (0,1),"CURRENT_CODEX_GUID_GREP_FAILED")
    old=set(GUID.findall(p.stdout.decode(errors="replace")))
    need(not (old & new_guids),"CODEX_SPRITE_GUID_COLLISION")
    return {"pinned_codex_sha":CODEX,"codex_asset_paths":len(blobpaths),
            "codex_guid_values":len(old),"new_guid_collisions":0}

def guard_target(where):
    raw=Path(where).absolute()
    # Do not resolve symlinks *before* checking each user-supplied path.
    for p in [raw,*raw.parents]:
        need(not p.is_symlink(),"TARGET_SYMLINK_COMPONENT")
        need(not (p/".git").exists(),"TARGET_INSIDE_GIT_CHECKOUT")
    root=raw.resolve(strict=True)
    need(root.is_dir() and (root/MARKER).is_file() and
         (root/MARKER).read_bytes()==MARKER_BYTES and
         (root/"Assets").is_dir() and not (root/"Assets").is_symlink(),
         "NOT_EXPLICIT_DISPOSABLE_TEST_PROJECT")
    need(not (root/"Assets"/"B01ImportStaging").is_symlink() and
         not (root/RECEIPT).is_symlink(),"STAGING_SYMLINK_REJECTED")
    return root

def inventory(root):
    return {p.relative_to(root).as_posix() for p in root.rglob("*") if p.is_file()}

def preflight(root,files):
    need(not (root/PREFIX).exists() and not (root/(PREFIX+".meta")).exists() and
         not (root/RECEIPT).exists(),"TARGET_STAGING_ALREADY_PRESENT")
    # Reject Windows' case-insensitive aliases even if this runner is Linux.
    old=inventory(root)
    oldfold={p.casefold() for p in old}
    need(not oldfold.intersection(n.casefold() for n in EXPECTED+[RECEIPT]),
         "TARGET_CASE_INSENSITIVE_PATH_COLLISION")
    need(not any(p.casefold().startswith(PREFIX.casefold()+"/") for p in old),
         "TARGET_PREFIX_ALIAS_PRESENT")
    return {"mode":"SOURCE_PREFLIGHT_PASS","files_to_install":len(files),"existing_files_preserved":len(old)}

def receipt_for(files,source):
    return {"package_id":PACKAGE,"source_package_id":INPUT_PACKAGE,"source_original_zip_sha256":ZIP_SHA,
            "original_art_sha":PRODUCER,"codex_pinned_sha":CODEX,
            "file_sha256":{name:digest(raw) for name,raw in sorted(files.items())},
            "original_codex_source_check":source,"native_unity":"NOT_RUN",
            "codex_ack":"PENDING","G1":"NOT_RUN","G2":"HOLD"}

def install(root,files,source):
    preflight(root,files)
    created=[]
    directories=[]
    try:
        # Exclusive directory and file creation: never overwrite any existing
        # project source. Rollback only paths actually created in this call.
        (root/PREFIX).mkdir()
        directories.append(root/PREFIX)
        for d in (ROOT,FRAMES):
            (root/d).mkdir()
            directories.append(root/d)
        for name,raw in sorted(files.items()):
            dest=root/name
            with dest.open("xb") as out:out.write(raw)
            created.append((dest,digest(raw)))
        payload=canonical(receipt_for(files,source))
        with (root/RECEIPT).open("xb") as out:out.write(payload)
        return {"mode":"DISPOSABLE_STAGE_CREATED","file_count":len(files),
                "receipt_sha256":digest(payload),"native_unity":"NOT_RUN"}
    except Exception:
        # If a concurrent writer changed an owned file, preserve it and report.
        unsafe=[]
        for dest,h in reversed(created):
            if dest.exists() and dest.is_file() and digest(dest.read_bytes())==h:
                dest.unlink()
            else:
                unsafe.append(str(dest))
        for d in reversed(directories):
            if d.is_dir():
                try:d.rmdir()
                except OSError:unsafe.append(str(d))
        if unsafe:raise Reject("PARTIAL_STAGING_UNSAFE_MANUAL_REVIEW:"+repr(unsafe))
        raise

def read_receipt(root,files):
    path=root/RECEIPT
    need(path.is_file() and not path.is_symlink(),"STAGE_RECEIPT_MISSING")
    obj=json.loads(path.read_bytes())
    require=receipt_for(files,obj.get("original_codex_source_check"))
    need(obj==require,"STAGE_RECEIPT_CHANGED")
    return obj

def verify(root,files):
    read_receipt(root,files)
    need(all((root/name).is_file() and not (root/name).is_symlink() and
             digest((root/name).read_bytes())==digest(raw)
             for name,raw in files.items()),"STAGED_FILE_CHANGED_OR_MISSING")
    current={p.relative_to(root).as_posix() for p in (root/PREFIX).rglob("*") if p.is_file()}
    need(current=={n for n in files if n.startswith(PREFIX+"/")},
         "UNTRACKED_FOREIGN_FILES_IN_STAGED_FOLDER")
    need(all((root/d).is_dir() and not (root/d).is_symlink()
             for d in (PREFIX,ROOT,FRAMES)),"STAGING_DIRECTORY_DRIFT")
    return {"mode":"DISPOSABLE_STAGE_BYTES_VERIFIED","file_count":len(files),
            "native_unity":"NOT_RUN"}

def rollback(root,files):
    verify(root,files)
    # No modified/foreign files can be silently deleted. Verify first.
    for name in sorted(files,reverse=True):
        (root/name).unlink()
    for d in (FRAMES,ROOT,PREFIX):
        (root/d).rmdir()
    (root/RECEIPT).unlink()
    return {"mode":"SAFE_ROLLBACK_COMPLETE","removed_own_files":len(files),
            "native_unity":"NOT_RUN"}

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--archive",required=True)
    parser.add_argument("--project",required=True,
                        help="External pre-marked disposable project root with Assets directory")
    parser.add_argument("--repo",required=True,help="Git root used for pinned Codex source check")
    parser.add_argument("--mode",choices=("preflight","install","verify","rollback"),required=True)
    args=parser.parse_args()
    try:
        files,ids=check_file_archive(args.archive)
        src=original_codex_source_guard(args.repo,ids)
        root=guard_target(args.project)
        if args.mode=="preflight":result=preflight(root,files)
        elif args.mode=="install":result=install(root,files,src)
        elif args.mode=="verify":result=verify(root,files)
        else:result=rollback(root,files)
        print(json.dumps({**result,"source":src,"project_marker":MARKER,"package_id":PACKAGE},
                         sort_keys=True))
        return 0
    except (Reject,OSError,ValueError,KeyError,zipfile.BadZipFile,UnicodeError) as exc:
        print("B0140_STAGE_REJECT "+str(exc)+" UNITY_NATIVE_NOT_RUN",file=sys.stderr)
        return 2
if __name__=="__main__":sys.exit(main())
