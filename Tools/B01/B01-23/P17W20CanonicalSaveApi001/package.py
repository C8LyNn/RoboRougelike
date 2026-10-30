#!/usr/bin/env python3
"""B01-23 W20: source-only original Git provenance + disposable safe stage."""
import argparse, hashlib, io, json, os, subprocess, sys, zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
ASSET="Assets/B0123CanonicalSaveApi001"
TOOLS="Tools/B01/B01-23/P17W20CanonicalSaveApi001/"
WORKFLOW=".github/workflows/b01-23-p17w20-canonical-api-source.yml"
V2_WORKFLOW=".github/workflows/b01-23-p17w21-canonical-owned-stage-v2.yml"
V2_README="Tools/B01/B01-23/P17W21CanonicalOwnedStageV2/README.md"
V3_WORKFLOW=".github/workflows/b01-23-p17w22-canonical-authority-v3.yml"
V3_README="Tools/B01/B01-23/P17W22CanonicalAuthorityV3/README.md"
FROZEN_API_V2="809f82575c638e1b4d2806b5f417bddab6c0871a"
FROZEN_API_V1="eed2e39636edfd8b8db754c426db452164cbf9d7"
PARENT_647="09c1809b73324a6d31c13e5d0143de62ac1e3ca6"
PINS={
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
}
MANIFEST="PACKAGE_MANIFEST.json"
def git(*x):return subprocess.check_output(["git",*x],cwd=ROOT).decode().strip()
def src(path):return subprocess.check_output(["git","show","HEAD:"+path],cwd=ROOT)
def sha(data):return hashlib.sha256(data).hexdigest()
def paths():
    subprocess.run(["git","merge-base","--is-ancestor",PARENT_647,"HEAD"],cwd=ROOT,check=True)
    subprocess.run(["git","merge-base","--is-ancestor",FROZEN_API_V1,"HEAD"],cwd=ROOT,check=True)
    subprocess.run(["git","merge-base","--is-ancestor",FROZEN_API_V2,"HEAD"],cwd=ROOT,check=True)
    for path,blob in PINS.items():
        if git("rev-parse","HEAD:"+path)!=blob:raise ValueError("frozen source changed "+path)
    names=sorted([ASSET+".meta",WORKFLOW,V2_WORKFLOW,V2_README,V3_WORKFLOW,V3_README]+
        git("ls-tree","-r","--name-only","HEAD",ASSET+"/").splitlines()+
        git("ls-tree","-r","--name-only","HEAD",TOOLS).splitlines())
    if len(names)!=18 or len(names)!=len(set(names)):raise ValueError("expected 18 exact selected lineage original owner files")
    if any(not(n==ASSET+".meta" or n in (WORKFLOW,V2_WORKFLOW,V2_README,V3_WORKFLOW,V3_README) or n.startswith(ASSET+"/") or n.startswith(TOOLS)) for n in names):
        raise ValueError("foreign source")
    guids=[]
    for n in names:
        if n.endswith(".meta"):
            tags=[t.partition(":")[2].strip() for t in src(n).decode().splitlines() if t.startswith("guid:")]
            if len(tags)!=1 or len(tags[0])!=32:raise ValueError("bad Unity guid")
            guids+=tags
    if len(guids)!=5 or len(set(guids))!=5:raise ValueError("not five unique GUIDs")
    if json.loads(src(ASSET+"/Core/B0123.CanonicalSaveApi.Core.asmdef"))["name"]!="B0123.CanonicalSaveApi.Core":
        raise ValueError("missing own assembly")
    body=src(ASSET+"/Core/CanonicalWorldGateway.cs").decode()
    for token in ["Thread.CurrentThread.ManagedThreadId","ColdRestore.TryStage",
                  "ICanonicalStageOwnership","BeginOwnedStage",
                  "CanonicalWorldSlotLease ownerLease","CheckPublishedCheckpoint",
                  "TryCloseAfterPhysicalRetirement",
                  "CleanupUnpublishedAndVerifyDestroyed",
                  "uncertainDurableCommit=true","RollbackQuarantine","staged.Activate()",
                  "staged.Capture()","SnapshotCodec.Encode"]:
        if token not in body:raise ValueError("missing canonical source gate: "+token)
    lease=src(ASSET+"/Core/CanonicalWorldSlotLease.cs").decode()
    for token in ["FileShare.None","TryAcquire(","BindOnce()",
                  "Registry.Add(key,owner)","ReleaseVerified()"]:
        if token not in lease:raise ValueError("missing original OS lease authority "+token)
    for token in ["System.Reflection","GetField(","#604","HostSlotLease.cs"]:
        if token in body:raise ValueError("forbidden private/alternate import")
    return names
def entry(name):
    info=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
    info.compress_type=zipfile.ZIP_STORED;info.create_system=3;info.external_attr=0o100644<<16
    return info
def build():
    names=paths()
    files={n:src(n) for n in names}
    manifest={"package":"B01-23-CANONICAL-WORLD-API-001","version":"3.0.0","selected_frozen_api_v1":FROZEN_API_V1,
      "selected_frozen_api_v2":FROZEN_API_V2,
      "sha":git("rev-parse","HEAD"),"parent_r4_selected":PARENT_647,
      "frozen_original_blobs":PINS,"native":"NOT_RUN","codex_ack":False,
      "canonical_510_private_adapter":"NOT_IMPLEMENTED","crash_atomic_slot":"B01-62_PENDING",
      "guids":5,"asmdefs":1,
      "owner_lifetime_authority":"REAL_OS_FILESHARE_NONE_MANDATORY",
      "integrated_b0162_712":"NO_SEPARATE_OWNER_ADAPTER_REQUIRED",
      "files":[{"path":n,"size":len(files[n]),"sha256":sha(files[n])} for n in names]}
    files[MANIFEST]=(json.dumps(manifest,sort_keys=True,separators=(",",":"))+"\n").encode()
    out=io.BytesIO()
    with zipfile.ZipFile(out,"w") as z:
        for path in sorted(files):z.writestr(entry(path),files[path])
    return out.getvalue()
def verify(raw):
    expected=paths()
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        names=z.namelist()
        if len(names)!=len(set(names)) or set(names)!=set(expected)|{MANIFEST}:raise ValueError("archive source set")
        for name in names:
            if name.startswith("/") or "\\" in name or ".." in Path(name).parts:raise ValueError("unsafe archive path")
        m=json.loads(z.read(MANIFEST))
        if m["sha"]!=git("rev-parse","HEAD") or m.get("selected_frozen_api_v1")!=FROZEN_API_V1 or
           m.get("selected_frozen_api_v2")!=FROZEN_API_V2 or m["parent_r4_selected"]!=PARENT_647 or \
           m["native"]!="NOT_RUN" or m["codex_ack"] is not False or \
           m["canonical_510_private_adapter"]!="NOT_IMPLEMENTED" or \
           m["crash_atomic_slot"]!="B01-62_PENDING" or m["guids"]!=5 or
           m.get("owner_lifetime_authority")!="REAL_OS_FILESHARE_NONE_MANDATORY" or
           m.get("integrated_b0162_712")!="NO_SEPARATE_OWNER_ADAPTER_REQUIRED":raise ValueError("forged gate")
        if [f["path"] for f in m["files"]]!=expected:raise ValueError("manifest order")
        for x in m["files"]:
            rawfile=z.read(x["path"])
            if rawfile!=src(x["path"]) or len(rawfile)!=x["size"] or sha(rawfile)!=x["sha256"]:
                raise ValueError("original bytes differ")
        return m
def stage(raw,target):
    verify(raw)
    dest=Path(target)
    if dest.exists() or dest.is_symlink() or dest.resolve().is_relative_to(ROOT.resolve()):
        raise ValueError("occupied or repository receiver")
    dest.mkdir(parents=True)
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        for n in z.namelist():
            out=dest/n
            out.parent.mkdir(parents=True,exist_ok=True)
            with out.open("xb") as w:w.write(z.read(n))
    return dest
def rollback(raw,dest):
    # Deliberately refuses ANY changed/foreign-owned file. No recursive cleanup.
    m=verify(raw);dest=Path(dest)
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        for n in z.namelist():
            q=dest/n
            if q.is_symlink() or not q.is_file() or q.read_bytes()!=z.read(n):
                raise ValueError("unsafe changed/missing/symlink target")
        for n in z.namelist():(dest/n).unlink()
    # Remove only empty directories, foreign EMPTY dirs remain untouched.
    for n in sorted([dest/p for p in {str(Path(x["path"]).parent) for x in m["files"]}],
                    key=lambda x:len(x.parts),reverse=True):
        if n.is_dir() and not any(n.iterdir()):n.rmdir()
    # Never remove foreign, including EMPTY, even if safe-looking.
def main():
    cmd=argparse.ArgumentParser();cmd.add_argument("action",choices=["build","verify"])
    cmd.add_argument("--archive",required=True);a=cmd.parse_args()
    if a.action=="build":
        output=Path(a.archive);output.parent.mkdir(parents=True,exist_ok=True)
        output.write_bytes(build())
    else:verify(Path(a.archive).read_bytes())
    raw=Path(a.archive).read_bytes()
    print("PASS ORIGINAL INNER ZIP",len(raw),"bytes members",len(zipfile.ZipFile(io.BytesIO(raw)).namelist()),
          "SHA256",sha(raw),"HEAD",git("rev-parse","HEAD"),
          "UNITY_NATIVE_NOT_RUN")
if __name__=="__main__":main()
