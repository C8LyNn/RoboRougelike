#!/usr/bin/env python3
"""Independent B01-61 exact-source Unity GUID/path collision preflight (NOT native Unity).
Reads original Codex #383 and original B01-23 #375 Git objects; never edits Assets.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

CODEX="203a0805251ed28293888e43f2d49f0df63767c6"
BRACING="5666a39eabd4cea91b2b605a9eb99ea2f1e226e8"
BRACING_MANIFEST="Docs/B01/PARTSIZE001/P17_W02_BRACING_MANIFEST.json"
BRACING_MANIFEST_GIT_BLOB="02221b524d2c106e90d179f16c09dcfef8815c6d"
OWN_META_COUNT=6
OWN_JSON_COUNT=4
EXACT_CODEX_META_COUNT=1023
META_RE=re.compile(rb"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$")
SHA_RE=re.compile(r"[a-f0-9]{40}\Z")
ROOT=Path(__file__).resolve().parent
PARENTS=(
    "Assets/PARTSIZE001/Runtime/PartSizeContract.cs",
    "Assets/PARTSIZE001/Blueprints/P17/P17_Flanker_Wheels.json",
    "Assets/PARTSIZE001/Blueprints/P17/P17_Bulwark_Tracks.json",
    "Assets/PARTSIZE001/Blueprints/P17/P17_Skirmisher_Hover.json",
)

class PreflightError(AssertionError):
    pass

def require(condition,reason):
    if not condition:raise PreflightError(reason)

def git(repo,*args,input=None):
    p=subprocess.run(["git","-C",str(repo),*args],input=input,capture_output=True,check=False)
    if p.returncode:
        raise PreflightError("GIT_COMMAND_FAILED "+repr(args)+" "+p.stderr.decode(errors="replace")[-400:])
    return p.stdout

def tree(repo,revision):
    require(SHA_RE.fullmatch(revision),"NONIMMUTABLE_TREE_REF")
    got=git(repo,"rev-parse",revision+"^{commit}").decode().strip()
    require(got==revision,"WRONG_GIT_COMMIT")
    raw=git(repo,"ls-tree","-r","-z",revision,"--","Assets")
    mapping={}
    for record in raw.split(b"\0"):
        if not record:continue
        header,rawpath=record.split(b"\t",1)
        mode,kind,blob=header.decode().split(" ")
        path=rawpath.decode("utf-8")
        require(kind=="blob" and SHA_RE.fullmatch(blob),"INVALID_GIT_TREE_OBJECT")
        require(path not in mapping,"REPEATED_GIT_TREE_PATH")
        mapping[path]=blob
    return mapping

def bulk_blobs(repo,shas):
    unique=list(dict.fromkeys(shas))
    if not unique:return {}
    data=git(repo,"cat-file","--batch",input=("\n".join(unique)+"\n").encode())
    out={}
    pos=0
    for expected in unique:
        eol=data.find(b"\n",pos)
        require(eol>pos,"CATFILE_MISSING_HEADER")
        h=data[pos:eol].decode().split(" ")
        require(len(h)==3 and h[0]==expected and h[1]=="blob","CATFILE_UNEXPECTED_BLOB")
        n=int(h[2])
        value=data[eol+1:eol+1+n]
        require(len(value)==n and data[eol+1+n:eol+2+n]==b"\n","CATFILE_BLOB_TRUNCATED")
        out[expected]=value
        pos=eol+2+n
    require(pos==len(data),"CATFILE_TRAILING_BYTES")
    return out

def parse_guid(content,path):
    matches=META_RE.findall(content)
    require(len(matches)==1,"INVALID_OR_MULTIPLE_META_GUID "+path)
    return matches[0].decode("ascii").lower()

def collisions(game_files,own_files):
    # dict[path, original raw bytes] including all actual game .meta and own
    own_metas={p:d for p,d in own_files.items() if p.endswith(".meta")}
    own_json={p:d for p,d in own_files.items() if p.endswith(".json")}
    require(len(own_files)==OWN_META_COUNT+OWN_JSON_COUNT and len(own_metas)==OWN_META_COUNT and len(own_json)==OWN_JSON_COUNT,"WRONG_OWN_CONTENT_COUNTS")
    require(all(p.startswith("Assets/PARTSIZE001/Bracing") for p in own_files),"OUT_OF_SCOPE_CONTENT_PATH")
    require(all(p.startswith("Assets/") for p in game_files),"INVALID_GAME_TREE_SCOPE")
    codex_meta={p:d for p,d in game_files.items() if p.endswith(".meta")}
    actual_path_lc={}
    for p in game_files:
        key=p.casefold()
        require(key not in actual_path_lc,"CASE_INSENSITIVE_DUPLICATE_EXISTING_PATH "+p)
        actual_path_lc[key]=p
    game_guids={}
    for p,d in codex_meta.items():
        key=parse_guid(d,p)
        require(key not in game_guids,"DUPLICATE_EXISTING_GAME_GUID "+p+" "+game_guids.get(key,""))
        game_guids[key]=p
    own_guids={}
    for p,d in own_metas.items():
        key=parse_guid(d,p)
        require(key not in own_guids,"DUPLICATE_BRACING_GUID "+p)
        own_guids[key]=p
    for p,d in own_files.items():
        require(p.casefold() not in actual_path_lc,"EXISTING_CODEX_ASSET_PATH_COLLISION "+p)
    for guid,p in own_guids.items():
        require(guid not in game_guids,"EXISTING_CODEX_UNITY_GUID_COLLISION "+guid+" OWN="+p+" EXISTING="+game_guids.get(guid,""))
    own_lc={}
    for p in own_files:
        require(p.casefold() not in own_lc,"BRACING_CASE_INSENSITIVE_DUPLICATE "+p)
        own_lc[p.casefold()]=p
    # Every original produced .json must have its original paired .meta.
    require(all(p+".meta" in own_metas for p in own_json),"ORPHAN_OR_UNPAIRED_JSON_META")
    # Original folder metas are paired with actual synthetic folder content.
    require("Assets/PARTSIZE001/Bracing.meta" in own_metas and
            "Assets/PARTSIZE001/Bracing/P17.meta" in own_metas,
            "BRACING_FOLDER_META_MISSING")
    return {"codexAssetFiles":len(game_files),"codexMetaFiles":len(codex_meta),
            "codexUniqueGuids":len(game_guids),"newAssetFiles":len(own_files),
            "newMetaFiles":len(own_metas),"newJsonFiles":len(own_json),
            "newUniqueGuids":len(own_guids),"pathCollisions":0,"guidCollisions":0,
            "newGuids":{path:guid for guid,path in sorted(own_guids.items())}}

def verify_original(repo):
    original_tree=tree(repo,BRACING)
    manifest_raw=git(repo,"show",BRACING+":"+BRACING_MANIFEST)
    observed=git(repo,"rev-parse",BRACING+":"+BRACING_MANIFEST).decode().strip()
    require(observed==BRACING_MANIFEST_GIT_BLOB,"BRACING_ORIGINAL_MANIFEST_DRIFT")
    manifest=json.loads(manifest_raw)
    require(manifest["package_id"]=="PARTSIZE001-P17-W02-STRUCTURAL-BRACING-R1","DIFFERENT_PRODUCER")
    entries=[x for x in manifest["original_files"] if x["path"].startswith("Assets/PARTSIZE001/Bracing")]
    require(len(entries)==10 and len(set(x["path"] for x in entries))==10,"ORIGINAL_MANIFEST_ASSET_SET_CHANGED")
    for e in entries:
        require(original_tree.get(e["path"])==e["git_blob_sha1"],"SOURCE_FILE_GIT_BLOB_MISMATCH "+e["path"])
    blobs=bulk_blobs(repo,[e["git_blob_sha1"] for e in entries])
    own={e["path"]:blobs[e["git_blob_sha1"]] for e in entries}
    return own

def run(repo):
    original=verify_original(repo)
    game_tree=tree(repo,CODEX)
    game_meta_paths={p:sha for p,sha in game_tree.items() if p.endswith(".meta")}
    require(len(game_meta_paths)==EXACT_CODEX_META_COUNT,
            "CODEX_GIT_META_COUNT_UNEXPECTED "+str(len(game_meta_paths)))
    blobs=bulk_blobs(repo,list(game_meta_paths.values()))
    # include all game Assets paths for case-fold path conflict, while content
    # of non-meta game assets is irrelevant; do not load GBs of binaries.
    game={p:(blobs[sha] if p.endswith(".meta") else b"") for p,sha in game_tree.items()}
    report=collisions(game,original)
    missing=[p for p in PARENTS if p not in game_tree]
    report.update({"schema":"B01-61-P17-W06-CODEX-BRACING-COLLISION-GATE-v1",
        "codexGameplaySource":CODEX,"bracingSource":BRACING,
        "manifestBlob":BRACING_MANIFEST_GIT_BLOB,
        "sourceG0CollisionStatus":"PASS_NO_CODEX_GUID_OR_PATH_COLLISION",
        "sourceG0Complete":"NO_FORMAL_B01_40_APPROVAL",
        "missingDirectGameplayPrerequisitePaths":missing,
        "prerequisiteIntegration":"HOLD" if missing else "SOURCE_TREE_PRESENT_NOT_NATIVE_VALIDATED",
        "nativeUnity":"NOT_RUN","physx":"NOT_RUN","codexAck":"NOT_OBSERVED",
        "integrated":False,"safeToMergeAutomatically":False})
    return report

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",default=".")
    p.add_argument("--output",default="codex_bracing_collision_receipt.json")
    args=p.parse_args()
    repo=Path(args.repo).resolve()
    report=run(repo)
    out=Path(args.output)
    require(not out.exists(),"REFUSE_OVERWRITE_OLD_QA_RECEIPT")
    raw=(json.dumps(report,indent=2,sort_keys=True)+"\n").encode("utf-8")
    out.write_bytes(raw)
    print("ACTUAL_CODEX_GAME_COMMIT="+CODEX)
    print("ORIGINAL_BRACING_SOURCE_COMMIT="+BRACING)
    print("ACTUAL_CODEX_ASSETS="+str(report["codexAssetFiles"])+" META="+str(report["codexMetaFiles"])+" UNIQUE_GUID="+str(report["codexUniqueGuids"]))
    print("ORIGINAL_BRACING_FILES=10/10 EXACT_GIT_BLOB | JSON=4/4 | META=6/6")
    print("SOURCE_PATH_COLLISIONS=0 | SOURCE_GUID_COLLISIONS=0 | CASEFOLD_PATHS=PASS")
    print("MISSING_CODEX_ORIGINAL_PARTSIZE_PREREQUISITES="+str(len(report["missingDirectGameplayPrerequisitePaths"]))+"/"+str(len(PARENTS)))
    print("ORIGINAL_G0_COLLISION_RECEIPT_SHA256="+hashlib.sha256(raw).hexdigest())
    print("B0161_W06_ORIGINAL_CODEX_GUID_PATH_PREFLIGHT=PASS | INTEGRATION_DEPENDENCIES="+report["prerequisiteIntegration"]+" | UNITY_NATIVE=NOT_RUN | CODEX_ACK=NO | NO_MERGE")

if __name__=="__main__":
    try:main()
    except (PreflightError,ValueError,KeyError,UnicodeError,TypeError,OSError) as exc:
        print("B0161_W06_SOURCE_COLLISION_GATE_RED="+repr(exc),file=sys.stderr)
        sys.exit(2)
