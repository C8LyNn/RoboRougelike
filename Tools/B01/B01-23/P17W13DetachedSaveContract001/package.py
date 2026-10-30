#!/usr/bin/env python3
"""Original HEAD Git-bytes owner ZIP with safe, non-destructive external scratch staging."""
import argparse,hashlib,io,json,os,shutil,subprocess,sys,zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
ASSET="Assets/B0123DetachedSaveContract001"
TOOLS="Tools/B01/B01-23/P17W13DetachedSaveContract001/"
WORKFLOW=".github/workflows/b01-23-p17w13-detached-save-contract.yml"
W14="Tools/B01/B01-23/P17W14SaveImmutability001/"
W14_WORKFLOW=".github/workflows/b01-23-p17w14-detached-save-v2-immutable.yml"
PARENT_W13="6da2c523e03766cd5a6deaed0cb3cccf8765354b"
PIN="d94400d95d13db7f27e1533df05c9292d5380431"
PINS={
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
 "Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs":"328977b4cdcdcd7c9f128c01f11ff67f8e46086a",
 "Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs":"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
 "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43",
}
MANIFEST="PACKAGE_MANIFEST.json"
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT).decode().strip()
def source(path):return subprocess.check_output(["git","show","HEAD:"+path],cwd=ROOT)
def sha(data):return hashlib.sha256(data).hexdigest()
def scope():
    subprocess.run(["git","merge-base","--is-ancestor",PIN,"HEAD"],cwd=ROOT,check=True)
    subprocess.run(["git","merge-base","--is-ancestor",PARENT_W13,"HEAD"],cwd=ROOT,check=True)
    for p,h in PINS.items():
        if git("rev-parse","HEAD:"+p)!=h:raise ValueError("frozen #510 source drift: "+p)
    paths=sorted([ASSET+".meta",WORKFLOW,W14_WORKFLOW]+
        git("ls-tree","-r","--name-only","HEAD",ASSET+"/").splitlines()+
        git("ls-tree","-r","--name-only","HEAD",TOOLS).splitlines()+
        git("ls-tree","-r","--name-only","HEAD",W14).splitlines())
    if len(paths)!=15 or len(paths)!=len(set(paths)):raise ValueError("expected twelve unique W13 owner paths")
    if any(not(x==ASSET+".meta" or x in (WORKFLOW,W14_WORKFLOW) or x.startswith(ASSET+"/") or x.startswith(TOOLS) or x.startswith(W14)) for x in paths):
        raise ValueError("owner scope violation")
    metas=[];defs=[]
    for p in paths:
        if p.endswith(".meta"):
            lines=[s.split(":",1)[1].strip() for s in source(p).decode().splitlines() if s.startswith("guid:")]
            if len(lines)!=1 or len(lines[0])!=32:raise ValueError("missing/invalid GUID")
            metas.extend(lines)
        if p.endswith(".asmdef"):defs.append(json.loads(source(p)))
    if len(metas)!=4 or len(set(metas))!=4:raise ValueError("Unity GUID duplicate")
    if len(defs)!=1 or defs[0]["name"]!="B0123.DetachedSaveContract.Runtime" or \
       defs[0]["references"]!=["B0123.DebrisRepulse.Runtime"] or not defs[0]["noEngineReferences"]:
        raise ValueError("invalid isolated asmdef")
    runtime=source(ASSET+"/Runtime/DetachedSnapshotContract.cs").decode()
    if "SelectedSourceSha=\""+PIN+"\"" not in runtime or \
       "DebrisImpulseLedger" not in runtime or \
       "TryDetach(p.Id,Zero,Zero,Zero" not in runtime or \
       "Current=ready;" not in runtime or \
       "p.Consumed!=detached" not in runtime or
       "public PersistedPart[] Parts => (PersistedPart[])parts.Clone();" not in runtime or
       "public DebrisImpulseLedger GetLedger(" in runtime or
       "return rebuilt[slot];" in runtime or
       "public bool IsConsumed(int slot,long id)" not in runtime:
        raise ValueError("immutable snapshot and opaque original W06 ledger required")
    if "private const int Version=2;" not in runtime or "B01SV14!" not in runtime:
        raise ValueError("version 2 schema migration required")
    return paths

def zinfo(name):
    z=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
    z.compress_type=zipfile.ZIP_STORED;z.create_system=3;z.external_attr=0o100644<<16
    return z
def build():
    files=scope();blobs={p:source(p) for p in files}
    manifest={"package":"B01-23-DETACHED-SAVE-CONTRACT-001","version":"2.0.0",
        "selected_secure_v2_sha":PIN,"supersedes_w13_sha":PARENT_W13,"own_head_sha":git("rev-parse","HEAD"),
        "frozen_blobs":PINS,"meta_guids":4,"native_unity":"NOT_RUN","savegame_host":"NOT_INTEGRATED",
        "codex_ack":False,
        "files":[{"path":p,"size":len(blobs[p]),"sha256":sha(blobs[p])} for p in files]}
    blobs[MANIFEST]=(json.dumps(manifest,sort_keys=True,separators=(",",":"))+"\n").encode()
    buff=io.BytesIO()
    with zipfile.ZipFile(buff,"w") as z:
        for p in sorted(blobs):z.writestr(zinfo(p),blobs[p])
    return buff.getvalue()

def verify(raw):
    paths=scope()
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        names=[i.filename for i in z.infolist()]
        if len(names)!=len(set(names)) or set(names)!=set(paths)|{MANIFEST} or any(
            x.startswith("/") or "\\" in x or ".." in x.split("/") for x in names):
            raise ValueError("invalid/foreign ZIP member")
        data={x:z.read(x) for x in names}
    manifest=json.loads(data[MANIFEST])
    if manifest.get("package")!="B01-23-DETACHED-SAVE-CONTRACT-001" or \
       manifest.get("version")!="2.0.0" or manifest.get("selected_secure_v2_sha")!=PIN or \
       manifest.get("supersedes_w13_sha")!=PARENT_W13 or
       manifest.get("own_head_sha")!=git("rev-parse","HEAD") or \
       manifest.get("frozen_blobs")!=PINS or manifest.get("meta_guids")!=4 or \
       manifest.get("native_unity")!="NOT_RUN" or \
       manifest.get("savegame_host")!="NOT_INTEGRATED" or manifest.get("codex_ack") is not False:
        raise ValueError("forged manifest gate")
    if [x.get("path") for x in manifest.get("files",[])]!=paths:
        raise ValueError("manifest entries changed")
    for record in manifest["files"]:
        p=record["path"];b=data[p]
        if record.get("size")!=len(b) or record.get("sha256")!=sha(b) or b!=source(p):
            raise ValueError("source byte drift: "+p)
    return data

def external(path):
    absolute=path.resolve()
    if absolute==ROOT.resolve() or ROOT.resolve() in absolute.parents:
        raise ValueError("refuse repo-integrated stage/rollback")
    # Never allow an existing symlink at or above the destination.
    cursor=path.absolute()
    while True:
        if cursor.is_symlink():raise ValueError("symlink on scratch path")
        if cursor==cursor.parent:break
        cursor=cursor.parent

def stage(raw,target):
    external(target)
    if os.path.lexists(str(target)):raise ValueError("refuse overwrite")
    items=verify(raw)
    target.mkdir()
    try:
        for p,b in items.items():
            f=target/p;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(b)
    except Exception:
        shutil.rmtree(target);raise

def rollback(raw,target):
    external(target)
    expected=verify(raw)
    if not target.is_dir() or target.is_symlink():raise ValueError("scratch folder not found")
    # Reject foreign *EMPTY DIRECTORY* as well as foreign files, symlinks and dirty bytes.
    allowed_dirs={Path(".")}
    for p in expected:
        parent=Path(p).parent
        while str(parent)!=".":
            allowed_dirs.add(parent);parent=parent.parent
    found=set()
    for p in target.rglob("*"):
        if p.is_symlink():raise ValueError("foreign symlink")
        relative=p.relative_to(target)
        if p.is_dir():
            if relative not in allowed_dirs:raise ValueError("foreign empty/nonempty directory: "+str(relative))
        elif p.is_file():
            name=relative.as_posix()
            if name not in expected or p.read_bytes()!=expected[name]:raise ValueError("foreign/dirty file")
            found.add(name)
        else:raise ValueError("unsupported scratch inode")
    if found!=set(expected):raise ValueError("missing owner files")
    shutil.rmtree(target)

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("cmd",choices=["build","verify","stage","rollback","source-gate"])
    parser.add_argument("--archive");parser.add_argument("--destination")
    a=parser.parse_args()
    if a.cmd=="source-gate":
        print("PASS",len(scope()),"W13 original Git source files pinned #510");return
    if not a.archive:raise ValueError("--archive required")
    path=Path(a.archive)
    if a.cmd=="build":
        if os.path.lexists(str(path)):raise ValueError("refuse overwrite archive")
        raw=build();path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(raw)
        verify(raw)
        print("PASS ORIGINAL INNER ZIP",len(raw),"bytes members",len(scope())+1,
            "SHA256",sha(raw),"HEAD",git("rev-parse","HEAD"))
    elif a.cmd=="verify":verify(path.read_bytes());print("PASS ZIP",sha(path.read_bytes()))
    elif a.cmd=="stage":
        if not a.destination:raise ValueError("destination needed")
        stage(path.read_bytes(),Path(a.destination));print("PASS stage scratch")
    elif a.cmd=="rollback":
        if not a.destination:raise ValueError("destination needed")
        rollback(path.read_bytes(),Path(a.destination));print("PASS rollback scratch")
if __name__=="__main__":
    try:main()
    except (ValueError,IOError,OSError,subprocess.CalledProcessError,zipfile.BadZipFile,KeyError) as exc:
        print("FAIL "+str(exc),file=sys.stderr);sys.exit(1)
