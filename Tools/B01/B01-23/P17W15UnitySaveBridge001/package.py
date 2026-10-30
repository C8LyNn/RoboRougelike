#!/usr/bin/env python3
"""Original Git source package for isolated W15 host; strict external scratch only."""
import argparse,hashlib,io,json,os,shutil,subprocess,sys,zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
ASSET="Assets/B0123UnitySaveBridge001"
TOOLS="Tools/B01/B01-23/P17W15UnitySaveBridge001/"
WORKFLOW=".github/workflows/b01-23-p17w15-unity-save-bridge.yml"
SELECTED_SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
SELECTED_GAME="d94400d95d13db7f27e1533df05c9292d5380431"
PINS={
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
 "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43",
 "Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs":"328977b4cdcdcd7c9f128c01f11ff67f8e46086a",
 "Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs":"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
}
MANIFEST="PACKAGE_MANIFEST.json"
def git(*a):return subprocess.check_output(["git",*a],cwd=ROOT).decode().strip()
def source(path):return subprocess.check_output(["git","show","HEAD:"+path],cwd=ROOT)
def sha(b):return hashlib.sha256(b).hexdigest()
def scope():
    subprocess.run(["git","merge-base","--is-ancestor",SELECTED_SAVE,"HEAD"],cwd=ROOT,check=True)
    subprocess.run(["git","merge-base","--is-ancestor",SELECTED_GAME,"HEAD"],cwd=ROOT,check=True)
    for p,h in PINS.items():
        if git("rev-parse","HEAD:"+p)!=h:raise ValueError("frozen SOURCE blob changed "+p)
    files=sorted([ASSET+".meta",WORKFLOW]+
        git("ls-tree","-r","--name-only","HEAD",ASSET+"/").splitlines()+
        git("ls-tree","-r","--name-only","HEAD",TOOLS).splitlines())
    if len(files)!=22 or len(files)!=len(set(files)):raise ValueError("W15 exactly 22 owner Git files required")
    if any(not (p==ASSET+".meta" or p==WORKFLOW or p.startswith(ASSET+"/") or p.startswith(TOOLS))
           for p in files):raise ValueError("foreign file")
    metas=[];defs=[]
    for p in files:
        if p.endswith(".meta"):
            tags=[line.split(":",1)[1].strip() for line in source(p).decode().splitlines() if line.startswith("guid:")]
            if len(tags)!=1 or len(tags[0])!=32:raise ValueError("GUID bad")
            metas.extend(tags)
        if p.endswith(".asmdef"):defs.append(json.loads(source(p)))
    if len(metas)!=10 or len(set(metas))!=10:raise ValueError("bad GUID count or duplicate")
    if sorted(x["name"] for x in defs)!=[
        "B0123.UnitySaveBridge.Core","B0123.UnitySaveBridge.PlayModeTests",
        "B0123.UnitySaveBridge.Unity"]:raise ValueError("not three new isolated asmdefs")
    core=source(ASSET+"/Core/AtomicWorldSwap.cs").decode()
    unity=source(ASSET+"/Unity/IsolatedTwinRigSaveHost.cs").decode()
    if ("ColdRestore.TryStage(bytes" not in core or
        "currentCold=ready;" not in core or
        "checkpoint.Commit(" not in core or
        "post-activation physics/ID mismatch" not in core or
        "verdict=WorldSwapVerdict.RollbackQuarantine" not in core or
        "old.Deactivate()" not in core or
        "durable!=publishedGeneration" not in core or
        "diskHash!=publishedHash" not in core or
        "publishedHash=fingerprint;" not in core or
        "TryUseIsolatedNativeTestSlot" not in unity or
        "TrySelectPersistentRunSlot" not in unity or
        "CheckpointSlotPath.ForRun" not in unity or
        "class CheckpointSlotPath" not in core or
        "b0123_isolated_save_slots" not in core or
        "slotOwnerLease" not in unity or
        "FileShare.None" not in unity or
        "quarantined=true;" not in core or
        "SameIdentityAndLedger" not in core or
        "TwinWorld.Build(snapshot,transform)" not in unity or
        "GameObject.CreatePrimitive" not in unity or
        "new DebrisImpulseLedger()" not in unity or
        "body.linearVelocity" not in unity or
        "PhysicalIdle()" not in unity):
        raise ValueError("missing actual transaction/Unity import safeguards")
    if any(x in core for x in ["GetLedger(", "Reflection", "System.Reflection"]):
        raise ValueError("forbidden public ledger/reflection")
    return files

def entry(name):
    z=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0));z.compress_type=zipfile.ZIP_STORED
    z.create_system=3;z.external_attr=0o100644<<16;return z
def build():
    files=scope()
    contents={p:source(p) for p in files}
    manifest={
      "package":"B01-23-UNITY-SAVE-HOST-BRIDGE-001","version":"3.0.0",
      "own_head_sha":git("rev-parse","HEAD"),
      "only_save_source_sha":SELECTED_SAVE,"gameplay_510_sha":SELECTED_GAME,
      "frozen_blob_guards":PINS,"unity_guid_count":10,"asmdef_count":3,
      "native_unity":"NOT_RUN","codex_ack":False,"canonical_510_live_import":"HOLD",
      "files":[{"path":p,"bytes":len(contents[p]),"sha256":sha(contents[p])} for p in files],
    }
    contents[MANIFEST]=(json.dumps(manifest,sort_keys=True,separators=(",",":"))+"\n").encode()
    output=io.BytesIO()
    with zipfile.ZipFile(output,"w") as z:
        for p in sorted(contents):z.writestr(entry(p),contents[p])
    return output.getvalue()

def verify(raw):
    files=scope()
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        names=z.namelist()
        if len(names)!=len(set(names)) or set(names)!=set(files)|{MANIFEST} or any(
            p.startswith("/") or "\\" in p or ".." in p.split("/") for p in names):
            raise ValueError("foreign/duplicate/traversal ZIP file")
        contents={p:z.read(p) for p in names}
    manifest=json.loads(contents[MANIFEST])
    if (manifest.get("package")!="B01-23-UNITY-SAVE-HOST-BRIDGE-001" or
        manifest.get("version")!="3.0.0" or
        manifest.get("own_head_sha")!=git("rev-parse","HEAD") or
        manifest.get("only_save_source_sha")!=SELECTED_SAVE or
        manifest.get("gameplay_510_sha")!=SELECTED_GAME or
        manifest.get("frozen_blob_guards")!=PINS or
        manifest.get("unity_guid_count")!=10 or
        manifest.get("asmdef_count")!=3 or
        manifest.get("native_unity")!="NOT_RUN" or
        manifest.get("codex_ack") is not False or
        manifest.get("canonical_510_live_import")!="HOLD" or
        [item.get("path") for item in manifest.get("files",[])]!=files):
        raise ValueError("forged owner/source manifest")
    for r in manifest["files"]:
        p=r["path"];b=contents[p]
        if r.get("bytes")!=len(b) or r.get("sha256")!=sha(b) or b!=source(p):
            raise ValueError("original Git source differs from physical ZIP "+p)
    return contents

def external(path):
    absolute=path.absolute()
    if absolute==ROOT or ROOT in absolute.parents:raise ValueError("repo staging forbidden")
    while True:
        if absolute.is_symlink():raise ValueError("symlink destination")
        if absolute==absolute.parent:break
        absolute=absolute.parent

def stage(raw,target):
    external(target)
    if os.path.lexists(str(target)):raise ValueError("refuse any existing receiver")
    b=verify(raw)
    target.mkdir()
    try:
        for p,content in b.items():
            dest=target/p;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(content)
    except Exception:
        shutil.rmtree(target);raise

def rollback(raw,target):
    external(target)
    b=verify(raw)
    if not target.is_dir() or target.is_symlink():raise ValueError("no clean target")
    dirs={Path(".")}
    for name in b:
        parent=Path(name).parent
        while str(parent)!=".":
            dirs.add(parent);parent=parent.parent
    seen=set()
    for path in target.rglob("*"):
        if path.is_symlink():raise ValueError("unexpected symlink")
        rel=path.relative_to(target)
        if path.is_dir():
            if rel not in dirs:raise ValueError("foreign EMPTY/nonempty directory "+str(rel))
        elif path.is_file():
            key=rel.as_posix()
            if key not in b or path.read_bytes()!=b[key]:raise ValueError("foreign/dirty file")
            seen.add(key)
        else:raise ValueError("foreign inode")
    if seen!=set(b):raise ValueError("missing tracked physical file")
    shutil.rmtree(target)

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("action",choices=["build","verify","stage","rollback","scope"])
    ap.add_argument("--archive");ap.add_argument("--destination")
    a=ap.parse_args()
    if a.action=="scope":print("PASS",len(scope()));return
    if not a.archive:raise ValueError("archive path required")
    path=Path(a.archive)
    if a.action=="build":
        if os.path.lexists(str(path)):raise ValueError("refuse replace ZIP")
        raw=build();path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(raw)
        verify(raw)
        print("PASS ORIGINAL INNER ZIP",len(raw),"bytes members",len(scope())+1,
            "SHA256",sha(raw),"HEAD",git("rev-parse","HEAD"))
    elif a.action=="verify":verify(path.read_bytes());print("PASS VERIFIED",sha(path.read_bytes()))
    elif a.action in ("stage","rollback"):
        if not a.destination:raise ValueError("destination required")
        if a.action=="stage":stage(path.read_bytes(),Path(a.destination))
        else:rollback(path.read_bytes(),Path(a.destination))
        print("PASS "+a.action)
if __name__=="__main__":
    try:main()
    except (ValueError,IOError,OSError,subprocess.CalledProcessError,zipfile.BadZipFile,KeyError) as err:
        print("FAIL "+str(err),file=sys.stderr);sys.exit(1)
