#!/usr/bin/env python3
"""B01-41 P17W15: exact original #510 -> #549 -> #556 Git-source closure firewall.

This is a read-only CI dependency/asset ownership gate, not game Save/Load G0/G1.
"""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

GAME = "d94400d95d13db7f27e1533df05c9292d5380431"
HISTORICAL = "6da2c523e03766cd5a6deaed0cb3cccf8765354b"
SAVE = "33094f154c7b91119b36d58aab285e6e83c5de57"
GAME_ROOT = "Assets/B0123SecureReattachV2"
SAVE_ROOT = "Assets/B0123DetachedSaveContract001"
SOURCE_RELATIVE = (
    ".github/workflows/b01-23-p17w13-detached-save-contract.yml",
    ".github/workflows/b01-23-p17w14-detached-save-r2.yml",
    SAVE_ROOT+".meta",
    SAVE_ROOT+"/Runtime.meta",
    SAVE_ROOT+"/Runtime/B0123.DetachedSaveContract.Runtime.asmdef",
    SAVE_ROOT+"/Runtime/B0123.DetachedSaveContract.Runtime.asmdef.meta",
    SAVE_ROOT+"/Runtime/DetachedSnapshotContract.cs",
    SAVE_ROOT+"/Runtime/DetachedSnapshotContract.cs.meta",
    "Tools/B01/B01-23/P17W13DetachedSaveContract001/Program.cs",
    "Tools/B01/B01-23/P17W13DetachedSaveContract001/README.md",
    "Tools/B01/B01-23/P17W13DetachedSaveContract001/SnapshotSourceTests.csproj",
    "Tools/B01/B01-23/P17W13DetachedSaveContract001/package.py",
    "Tools/B01/B01-23/P17W13DetachedSaveContract001/test_package.py",
    "Tools/B01/B01-23/P17W14DetachedSaveContractR2/SECURITY.md",
    "Tools/B01/B01-23/P17W14DetachedSaveContractR2/contract_selection.json",
    "Tools/B01/B01-23/P17W14DetachedSaveContractR2/test_security.py",
)
META_PATHS = tuple(p for p in SOURCE_RELATIVE if p.endswith(".meta"))
SHA_PATTERN = re.compile(r"^[0-9a-f]{40}$")
GUID_PATTERN = re.compile(r"(?m)^guid:\s*([0-9a-f]{32})\s*$")
MAX_BYTES = 4_000_000

class Refused(Exception):
    pass

def require(value,reason):
    if not value: raise Refused(reason)

def git(repo:Path,*args:str)->bytes:
    try:
        p=subprocess.run(["git","-C",str(repo),*args],capture_output=True,check=False)
    except OSError as exc:
        raise Refused("GIT_NOT_AVAILABLE") from exc
    require(p.returncode==0,"GIT_COMMAND_FAILED:"+str(args[0]))
    require(len(p.stdout)<=MAX_BYTES,"OVERSIZED_GIT_OUTPUT")
    return p.stdout

def commit(repo:Path,sha:str)->None:
    require(isinstance(sha,str) and SHA_PATTERN.fullmatch(sha),"EXACT_COMMIT_SHA_REQUIRED")
    actual=git(repo,"rev-parse","--verify",sha+"^{commit}").decode().strip()
    require(actual==sha,"GIT_COMMIT_CHANGED")

def ancestor(repo:Path,older:str,newer:str)->None:
    git(repo,"merge-base","--is-ancestor",older,newer)

def entries(repo:Path,sha:str)->dict[str,tuple[str,str]]:
    raw=git(repo,"ls-tree","-r","-z",sha)
    result={}
    for record in raw.split(b"\0"):
        if not record:continue
        try:
            fields,path=record.split(b"\t",1)
            mode,kind,blob=fields.decode("ascii").split()
            p=path.decode("utf-8")
        except (ValueError,UnicodeError) as exc:
            raise Refused("MALFORMED_SOURCE_GIT_TREE") from exc
        require(mode=="100644" and kind=="blob","SYMLINK_EXECUTABLE_OR_SUBMODULE_IN_SOURCE:"+p)
        require(p not in result and p and "\\" not in p and
                all(x not in ("",".","..") for x in p.split("/")),
                "MALFORMED_OR_DUPLICATE_SOURCE_PATH")
        result[p]=(mode,blob)
    return result

def blob(repo:Path,sha:str,path:str)->bytes:
    result=git(repo,"show",sha+":"+path)
    require(len(result)<=MAX_BYTES,"SOURCE_BLOB_OVERSIZED")
    return result

def audit(repo:Path,game_sha:str,history_sha:str,save_sha:str,
          manifest_game:str|None=None,manifest_parent:str|None=None)->dict:
    # The CLI pins original production hashes; parameters permit real disposable
    # Git-repository fault-injection tests without impersonating original SHA.
    for value in (game_sha,history_sha,save_sha): commit(repo,value)
    ancestor(repo,game_sha,history_sha)
    ancestor(repo,history_sha,save_sha)
    base=entries(repo,game_sha)
    full=entries(repo,save_sha)
    changed={p for p in set(base)|set(full) if base.get(p)!=full.get(p)}
    expected=set(SOURCE_RELATIVE)
    require(changed==expected,
            "SOURCE_CLOSURE_DRIFT:missing="+",".join(sorted(expected-changed))
            +";extra="+",".join(sorted(changed-expected)))
    require(all(p not in base and p in full for p in expected),
            "SELECTED_LINEAGE_MUST_ONLY_ADD_VERSIONED_FILES")
    require(all(full[p][0]=="100644" for p in expected),
            "UNSAFE_SOURCE_FILE_MODE")
    require(any(x.startswith(GAME_ROOT+"/") for x in base),
            "MISSING_SELECTED_ORIGINAL_GAMEPLAY_ASSETS")
    for p in full:
        require(not p.startswith("Assets/B0123DebrisPlayground001/")
                and not p.startswith("Assets/B0123DockHardening001/")
                and not p.startswith("Assets/B0123ReattachClearance001/"),
                "LEGACY_GAMEPLAY_ALTERNATIVE_IN_SELECTED_LINEAGE")
    seen={}
    for p in full:
        if not p.startswith("Assets/") or not p.endswith(".meta"):continue
        content=blob(repo,save_sha,p).decode("utf-8",errors="replace")
        matches=GUID_PATTERN.findall(content)
        require(len(matches)==1,"MISSING_OR_DUPLICATE_UNITY_META_GUID:"+p)
        guid=matches[0]
        require(guid not in seen,"GUID_COLLISION:"+seen.get(guid,"")+" WITH "+p)
        seen[guid]=p
    require(len(META_PATHS)==4 and all(p in full for p in META_PATHS),
            "MISSING_SELECTED_SAVE_META_PATHS")
    # Source under the newly authored unity Asset root must have file and
    # folder meta ownership. A dot-meta cannot silently alias a folder.
    for p in full:
        if not p.startswith(SAVE_ROOT+"/") or p.endswith(".meta"):continue
        require(p+".meta" in full,"MISSING_ASSET_META_PAIR:"+p)
    for p in (SAVE_ROOT,SAVE_ROOT+"/Runtime"):
        require(p+".meta" in full,"MISSING_OWNED_UNITY_DIRECTORY_META:"+p)
    asm=json.loads(blob(repo,save_sha,SAVE_ROOT+
                        "/Runtime/B0123.DetachedSaveContract.Runtime.asmdef"))
    require(type(asm) is dict and asm.get("name")==
            "B0123.DetachedSaveContract.Runtime" and
            asm.get("references")==["B0123.DebrisRepulse.Runtime"] and
            asm.get("noEngineReferences") is True,
            "UNEXPECTED_SAVE_ASMDEF_DEPENDENCY")
    selection=json.loads(blob(repo,save_sha,
             "Tools/B01/B01-23/P17W14DetachedSaveContractR2/contract_selection.json"))
    require(type(selection) is dict and
            selection.get("package")=="B01-23-DETACHED-SAVE-CONTRACT-001" and
            selection.get("version")=="2.0.0" and
            selection.get("frozen_gameplay_pr_510")==(manifest_game or game_sha) and
            selection.get("supersedes_sha")==(manifest_parent or history_sha) and
            selection.get("selected_save_lineage")=="#549" and
            selection.get("other_save_contract_544")=="ALT_HOLD" and
            selection.get("unity_native")=="NOT_RUN" and
            selection.get("savegame_host")=="NOT_INTEGRATED" and
            selection.get("codex_ack") is False,
            "CONTRACT_SELECTION_IDENTITY_DRIFT_OR_FALSE_ACCEPTANCE")
    pins={p:full[p][1] for p in sorted(expected)}
    return {"package_id":"CI41-SELECTED-SAVE-LINEAGE-FENCE-001",
            "version":"1.0.0","status":"IMMUTABLE_SOURCE_LINEAGE_PASS",
            "selected_game_sha":game_sha,"selected_save_sha":save_sha,
            "historical_parent_sha":history_sha,"added_source_count":len(pins),
            "original_gameplay_bytes_unmodified":True,
            "asset_guid_count":len(seen),"new_save_guid_count":len(META_PATHS),
            "source_git_blob_sha":pins,
            "source_fingerprint_sha256":hashlib.sha256(
                json.dumps(pins,sort_keys=True,separators=(",",":")).encode()
            ).hexdigest(),
            "save_contract_source_g0":"EXTERNAL_B01_40_560_ONLY",
            "unity_host_bridge":"UNVERIFIED_HOLD",
            "native_unity_g1":"NOT_RUN","g2":"HOLD",
            "codex_ack":False,"integrated":False,"release_gate":"HOLD"}

def main()->int:
    parser=argparse.ArgumentParser()
    parser.add_argument("--repo",default=".")
    parser.add_argument("--output",required=True)
    args=parser.parse_args()
    try:
        report=audit(Path(args.repo),GAME,HISTORICAL,SAVE)
        result=0
    except (Refused,OSError,ValueError,UnicodeError) as exc:
        report={"package_id":"CI41-SELECTED-SAVE-LINEAGE-FENCE-001",
                "status":"REJECTED_FAIL_CLOSED","reason":str(exc),
                "unity_host_bridge":"UNVERIFIED_HOLD","native_unity_g1":"NOT_RUN",
                "release_gate":"HOLD"}
        result=2
    data=json.dumps(report,sort_keys=True,indent=2)+"\n"
    Path(args.output).write_text(data,encoding="utf-8")
    print(data,end="")
    return result

if __name__=="__main__":
    sys.exit(main())
