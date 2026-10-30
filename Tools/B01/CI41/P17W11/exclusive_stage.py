#!/usr/bin/env python3
"""CI41 EXCLUSIVE-STAGE-001: source-only exact-Git mutual-exclusion installer.

Creates an isolated, verifiable Unity Assets staging directory, NOT a game install.
No claim of safe PhysX, atomic dock, G0, G1 or Codex ACK.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import tempfile

SOURCE_PINS = {
    "secure-v2": "33a32b8bd9ab40467ab1a17b5684926a37dbd4d3",  # #488
    "public-guard": "6efe45961ed0ffdf183e8016e350978b9f3dc51c",  # #494
}
PREFIXES = {
    "secure-v2": ("Assets/B0123SecureReattachV2",),
    "public-guard": ("Assets/B0123DebrisPlayground001",
                     "Assets/B0123DockHardening001",
                     "Assets/B0123ReattachClearance001"),
}
REQUIRED = {
    "secure-v2": (
        "Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs",
        "Assets/B0123SecureReattachV2/Runtime/SecureDockPolicy.cs",
        "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",
        "Assets/B0123SecureReattachV2/Editor/SecureReattachSceneBuilder.cs",
        "Assets/B0123SecureReattachV2/Tests/PlayMode/SecureReattachV2PlayModeTests.cs",
    ),
    "public-guard": (
        "Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs",
        "Assets/B0123DockHardening001/Runtime/HardenedDockDemo.cs",
        "Assets/B0123ReattachClearance001/Runtime/DebrisSafeReattachGate.cs",
        "Assets/B0123DockHardening001/Tests/PlayMode/W10PublicDockGuardPlayModeTests.cs",
    ),
}
SHA = re.compile(r"[0-9a-f]{40}\Z")
GUID = re.compile(r"(?m)^guid: ([0-9a-f]{32})\r?$")
MAX_STAGE_FILES = 300
MAX_BLOB = 2_000_000

class GateError(Exception):
    pass

def git(repo, *args, ok=(0,)):
    try:
        p = subprocess.run(["git", "-C", str(repo), *args], capture_output=True)
    except OSError as exc:
        raise GateError("GIT_NOT_AVAILABLE") from exc
    if p.returncode not in ok:
        raise GateError("GIT_COMMAND_REJECTED:"+str(args[:2]))
    return p.stdout

def commit(repo, sha):
    if not SHA.fullmatch(sha):
        raise GateError("EXACT_40_HEX_COMMIT_REQUIRED")
    if git(repo, "rev-parse", "--verify", sha+"^{commit}").decode().strip() != sha:
        raise GateError("SOURCE_COMMIT_MISMATCH")

def tree(repo, sha):
    raw = git(repo,"ls-tree","-r","-z",sha,"--","Assets")
    found = {}
    for record in raw.split(b"\0"):
        if not record:
            continue
        try:
            header, name = record.split(b"\t", 1)
            mode, typ, blob = header.decode("ascii").split()
            path = name.decode("utf-8")
        except (ValueError, UnicodeError) as exc:
            raise GateError("INVALID_GIT_TREE_RECORD") from exc
        if typ != "blob" or mode != "100644":
            raise GateError("UNSAFE_TREE_OBJECT_TYPE_OR_MODE:"+path)
        if (not path.startswith("Assets/") or
                any(x in (".","..","") for x in path.split("/")) or
                "\n" in path or "\r" in path or "\\" in path):
            raise GateError("UNSAFE_GIT_PATH:"+path)
        found[path] = blob
    return found

def scoped(path, roots):
    return any(path == root+".meta" or path.startswith(root+"/") for root in roots)

def read_blob(repo, blob):
    data=git(repo,"cat-file","blob",blob)
    if len(data)>MAX_BLOB:
        raise GateError("OVERSIZED_GIT_ASSET")
    return data

def guid_of(data, path):
    try:
        value=data.decode("utf-8")
    except UnicodeDecodeError as exc:
        raise GateError("INVALID_META_ENCODING:"+path) from exc
    matches=GUID.findall(value)
    if len(matches)!=1:
        raise GateError("INVALID_OR_MISSING_META_GUID:"+path)
    return matches[0]

def plan(repo, target_sha, producer_sha, variant, pinned_sha=None):
    if variant not in PREFIXES:
        raise GateError("ONE_EXPLICIT_VARIANT_REQUIRED")
    commit(repo,target_sha)
    commit(repo,producer_sha)
    pin = SOURCE_PINS[variant] if pinned_sha is None else pinned_sha
    if producer_sha != pin:
        raise GateError("UNAPPROVED_OR_DRIFTED_PRODUCER_SHA")
    git(repo,"merge-base","--is-ancestor",target_sha,producer_sha)
    target, source = tree(repo,target_sha),tree(repo,producer_sha)
    chosen = PREFIXES[variant]
    other = PREFIXES["public-guard" if variant=="secure-v2" else "secure-v2"]
    if any(scoped(p,other) for p in source):
        raise GateError("CANDIDATE_CONTAINS_COMPETING_DOCK_STACK")
    if any(scoped(p,chosen+other) for p in target):
        raise GateError("TARGET_ALREADY_CONTAINS_DOCK_OR_LEGACY")
    selected={p:b for p,b in source.items() if scoped(p,chosen)}
    if not selected or len(selected)>MAX_STAGE_FILES:
        raise GateError("EMPTY_OR_OVERSIZED_DOCK_SOURCE")
    for path in REQUIRED[variant]:
        if path not in selected or path+".meta" not in selected:
            raise GateError("MISSING_REQUIRED_SOURCE_OR_META:"+path)
    for path in selected:
        if path.endswith(".meta"):
            continue
        if path+".meta" not in selected:
            raise GateError("MISSING_UNITY_ASSET_META:"+path)
        parent=Path(path).parent
        while parent.as_posix()!="Assets":
            if parent.as_posix()+".meta" not in selected:
                raise GateError("MISSING_UNITY_FOLDER_META:"+parent.as_posix())
            parent=parent.parent
    source_guids={}
    for path,blob in sorted(selected.items()):
        if not path.endswith(".meta"):
            continue
        key=guid_of(read_blob(repo,blob),path)
        if key in source_guids:
            raise GateError("DUPLICATE_SELECTED_GUID:"+key)
        source_guids[key]=path
    # Inspect original target GUIDs in one Git process, then reject cross-path conflicts.
    raw=git(repo,"grep","-n","-I","-e","^guid: ",target_sha,"--","Assets",ok=(0,1))
    target_guids={}
    for line in raw.decode("utf-8").splitlines():
        m=re.match(r"^[0-9a-f]{40}:(Assets/[^:\n]+\.meta):\d+:guid: ([0-9a-f]{32})\r?$",line)
        if m:
            target_guids.setdefault(m.group(2),set()).add(m.group(1))
    conflicted=sorted(set(source_guids).intersection(target_guids))
    if conflicted:
        raise GateError("TARGET_GUID_COLLISION:"+conflicted[0])
    files=[{"path":path,"git_blob":blob,
            "sha256":hashlib.sha256(read_blob(repo,blob)).hexdigest()}
           for path,blob in sorted(selected.items())]
    return {"package":"CI41-EXCLUSIVE-STAGE-001","version":"1.0.0",
            "selected_variant":variant,"source_sha":producer_sha,"target_sha":target_sha,
            "source_file_count":len(files),"source_meta_guid_count":len(source_guids),
            "files":files,"exclusive_stage":"PASS","security_transaction":"UNVERIFIED_HOLD",
            "native_unity":"NOT_RUN","independent_qa":"PENDING",
            "codex_ack":"NO","integration":"NO","release_gate":"HOLD"}

def stage(repo, result, dest):
    repo=Path(repo).resolve()
    out=Path(dest).resolve()
    if out.exists() or out == repo or repo in out.parents:
        raise GateError("STAGING_DESTINATION_EXISTS_OR_INSIDE_SOURCE_REPO")
    if not out.parent.is_dir():
        raise GateError("STAGING_PARENT_MUST_EXIST")
    with tempfile.TemporaryDirectory(prefix="ci41-exclusive-",dir=out.parent) as temp:
        root=Path(temp)
        for file in result["files"]:
            path=root/file["path"]
            path.parent.mkdir(parents=True,exist_ok=True)
            data=read_blob(repo,file["git_blob"])
            if hashlib.sha256(data).hexdigest()!=file["sha256"]:
                raise GateError("SOURCE_BLOB_DRIFT")
            path.write_bytes(data)
        (root/"STAGE_RECEIPT.json").write_text(json.dumps(result,sort_keys=True,indent=2)+"\n",encoding="utf-8")
        root.rename(out)
    return out

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",default=".")
    p.add_argument("--target-sha",required=True)
    p.add_argument("--producer-sha",required=True)
    p.add_argument("--variant",required=True,choices=sorted(PREFIXES))
    p.add_argument("--stage-dir")
    p.add_argument("--out")
    a=p.parse_args()
    try:
        result=plan(Path(a.repo).resolve(),a.target_sha,a.producer_sha,a.variant)
        if a.stage_dir:
            result["staged_at"]=str(stage(Path(a.repo),result,a.stage_dir))
    except (GateError,OSError,UnicodeDecodeError) as exc:
        result={"package":"CI41-EXCLUSIVE-STAGE-001","exclusive_stage":"FAIL_CLOSED",
                "reason":str(exc),"security_transaction":"UNVERIFIED_HOLD",
                "native_unity":"NOT_RUN","release_gate":"HOLD"}
    output=json.dumps(result,sort_keys=True,indent=2)+"\n"
    if a.out:
        Path(a.out).write_text(output,encoding="utf-8")
    print(output,end="")
    return 2 if result["exclusive_stage"]=="FAIL_CLOSED" else 0

if __name__=="__main__":
    sys.exit(main())
