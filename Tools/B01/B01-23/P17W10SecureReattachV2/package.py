#!/usr/bin/env python3
"""Deterministic Git-source physical ZIP with independent source-call-path guards."""
import argparse
import hashlib
import io
import json
from pathlib import Path
import shutil
import subprocess
import sys
import zipfile

ROOT=Path(__file__).resolve().parents[4]
ASSET="Assets/B0123SecureReattachV2"
OWN="Tools/B01/B01-23/P17W10SecureReattachV2/"
CI=".github/workflows/b01-23-p17w10-secure-reattach-v2.yml"
PARENT="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
PINS={
    "Assets/B0123DebrisRepulse001/Runtime/DebrisRepulsionEmitter.cs":"2b3a8465c550bd49a5c98be5a8064a4ce0947f4b",
    "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
    "Assets/B0123DebrisRepulse001/Runtime/B0123.DebrisRepulse.Runtime.asmdef":"25a49dc836b62d791c0bf40c214c1c6cf09a9fea",
}
ID="B01-23-SECURE-REATTACH-V2-R1-1"
MANIFEST="PACKAGE_MANIFEST.json"

def g(*args):
    return subprocess.check_output(["git",*args],cwd=ROOT).decode().strip()
def blob(path):
    return subprocess.check_output(["git","show","HEAD:"+path],cwd=ROOT)
def sha(data):
    return hashlib.sha256(data).hexdigest()
def source_sha():
    return g("rev-parse","HEAD")

def scope():
    subprocess.run(["git","merge-base","--is-ancestor",PARENT,"HEAD"],cwd=ROOT,check=True)
    for path,h in PINS.items():
        if g("rev-parse","HEAD:"+path)!=h:
            raise ValueError("Frozen W06 producer Git blob drift: "+path)
    files=sorted(g("ls-tree","-r","--name-only","HEAD",ASSET+"/").splitlines()+
        g("ls-tree","-r","--name-only","HEAD",OWN).splitlines()+[ASSET+".meta",CI])
    if len(files)!=len(set(files)) or any(not(p.startswith(ASSET+"/") or
        p.startswith(OWN) or p in (ASSET+".meta",CI)) for p in files):
        raise ValueError("owner scope or duplicate")
    metas={}
    asm={}
    for p in files:
        data=blob(p)
        if p.endswith(".meta"):
            ids=[line.partition(":")[2].strip() for line in data.decode().splitlines()
                 if line.startswith("guid:")]
            if len(ids)!=1 or len(ids[0])!=32 or any(c not in "0123456789abcdef" for c in ids[0]):
                raise ValueError("invalid Unity GUID "+p)
            if ids[0] in metas:raise ValueError("duplicate Unity GUID "+p)
            metas[ids[0]]=p
        if p.endswith(".asmdef"):
            j=json.loads(data)
            asm[j["name"]]=j
    if len(metas)!=13:raise ValueError("expected 13 stable unique Unity meta GUIDs, got "+str(len(metas)))
    expected={
       "B0123.SecureReattachV2.Runtime":["B0123.DebrisRepulse.Runtime"],
       "B0123.SecureReattachV2.Editor":["B0123.SecureReattachV2.Runtime"],
       "B0123.SecureReattachV2.PlayMode":["B0123.SecureReattachV2.Runtime"],
    }
    if {k:sorted(v["references"]) for k,v in asm.items()} != expected:
        raise ValueError("Unity asmdef dependency graph changed")
    if asm["B0123.SecureReattachV2.Editor"]["includePlatforms"]!=["Editor"] or \
       "UNITY_INCLUDE_TESTS" not in asm["B0123.SecureReattachV2.PlayMode"]["defineConstraints"]:
        raise ValueError("Unity test/editor assembly boundaries missing")
    required={
        ASSET+"/Runtime/SecureDockPolicy.cs",ASSET+"/Runtime/SecureRigSpec.cs",
        ASSET+"/Runtime/SecureDebrisPlaygroundV2.cs",
        ASSET+"/Editor/SecureReattachSceneBuilder.cs",
        ASSET+"/Tests/PlayMode/SecureReattachV2PlayModeTests.cs",
        OWN+"Program.cs",OWN+"DockV2SourceTests.csproj",OWN+"package.py",
        OWN+"test_package.py",OWN+"README.md",CI
    }
    if not required.issubset(files):raise ValueError("missing W10 real deliverables")
    return files

def method_body(source,name):
    # Brace-depth extract of real C# method to distinguish nearby code/comments.
    signature="public bool "+name+"(int index)"
    start=source.find(signature)
    if start<0:raise ValueError("public V2 API route missing: "+name)
    end=source.find("\n",start)
    return source[start:end].strip()

def guard_contract():
    unity=blob(ASSET+"/Runtime/SecureDebrisPlaygroundV2.cs").decode()
    policy=blob(ASSET+"/Runtime/SecureDockPolicy.cs").decode()
    for name in ("Reattach","TrySafeReattach","ReattachFromGui"):
        route=method_body(unity,name)
        if "DockGate()."+name+"(index)" not in route:
            raise ValueError("PUBLIC SAFE REATTACH BYPASS: "+name)
    if "private bool CommitDock(int index)" not in unity or \
       "public bool CommitDock" in unity or "private DockResult InspectDock(int index)" not in unity:
        raise ValueError("physical mutator must remain private and inspector private")
    if "if(GUILayout.Button(\"Reattach (GUARDED)\"))ReattachFromGui(i);" not in unity:
        raise ValueError("GUI reattach bypasses single guarded route")
    if "Physics.OverlapBoxNonAlloc(" not in unity or \
       "SecureDockPolicy.AllLayers,QueryTriggerInteraction.Ignore" not in unity or \
       "Physics.ComputePenetration(" not in unity:
        raise ValueError("required real PhysX all-layer gate missing")
    if "new GuardedDockCommand(InspectDock,CommitDock)" not in unity or \
       "public bool Reattach(int index) => Execute(index)" not in policy or \
       "public bool TrySafeReattach(int index) => Execute(index)" not in policy or \
       "public bool ReattachFromGui(int index) => Execute(index)" not in policy:
        raise ValueError("public aliases do not all use GuardedDockCommand.Execute")
    if "return LastResult == DockResult.Allowed && commit(index);" not in policy:
        raise ValueError("dock mutator can execute without inspect approval")
    # Original W06 rearm is an atomic consumed-ID mutation. It MUST occur
    # before the first physical Rigidbody/Transform mutation in the new version.
    actual_commit=unity.split("private bool CommitDock(int index)",1)[1].split("public void ResetForNewRun()",1)[0]
    if actual_commit.count("rig.Emitter.RearmAfterReattach(rig.Spec.PartId)") != 1 or \
       "if(!rig.Emitter.RearmAfterReattach(rig.Spec.PartId))\n                return false;" not in actual_commit or \
       actual_commit.index("rig.Emitter.RearmAfterReattach") > actual_commit.index("rig.Body.linearVelocity"):
        raise ValueError("W11 consumed-ID MUST rearm before all Rigidbody changes")
    editor=blob(ASSET+"/Editor/SecureReattachSceneBuilder.cs").decode()
    if "NewScene(NewSceneSetup.EmptyScene" not in editor or \
       "controller.AddComponent<SecureDebrisPlaygroundV2>()" not in editor or \
       "AddComponent<DebrisRigPlayground>" in editor:
        raise ValueError("replacement scene still contains legacy W07 controller")

def zinfo(p):
    x=zipfile.ZipInfo(p,date_time=(1980,1,1,0,0,0))
    x.compress_type=zipfile.ZIP_STORED
    x.create_system=3
    x.external_attr=0o100644<<16
    return x

def build():
    files=scope()
    guard_contract()
    original={p:blob(p) for p in files}
    manifest={
        "package":ID,"version":"1.1.0","source_sha":source_sha(),
        "parent_w06_sha":PARENT,"frozen_source_blobs":PINS,
        "owned_meta_guids":13,"native_unity":"NOT_RUN","codex_ack":False,
        "files":[{"path":p,"bytes":len(original[p]),"sha256":sha(original[p])} for p in files]
    }
    original[MANIFEST]=(json.dumps(manifest,sort_keys=True,separators=(",",":"))+"\n").encode()
    data=io.BytesIO()
    with zipfile.ZipFile(data,"w") as z:
        for p in sorted(original):
            z.writestr(zinfo(p),original[p])
    return data.getvalue()

def verify(raw):
    files=scope()
    guard_contract()
    with zipfile.ZipFile(io.BytesIO(raw),"r") as z:
        names=[x.filename for x in z.infolist()]
        if len(names)!=len(set(names)) or set(names)!=set(files)|{MANIFEST} or any(
            n.startswith("/") or "\\" in n or ".." in n.split("/") for n in names):
            raise ValueError("ZIP altered, duplicate or traversal")
        data={p:z.read(p) for p in names}
    manifest=json.loads(data[MANIFEST])
    if manifest.get("package")!=ID or manifest.get("version")!="1.1.0" or \
       manifest.get("source_sha")!=source_sha() or \
       manifest.get("parent_w06_sha")!=PARENT or \
       manifest.get("frozen_source_blobs")!=PINS or \
       manifest.get("owned_meta_guids")!=13 or \
       manifest.get("native_unity")!="NOT_RUN" or manifest.get("codex_ack") is not False:
        raise ValueError("manifest provenance, bogus native or ACK")
    records=manifest.get("files",[])
    if [x.get("path") for x in records]!=files:
        raise ValueError("manifest path mismatch")
    for r in records:
        p=r["path"]
        if r.get("sha256")!=sha(data[p]) or r.get("bytes")!=len(data[p]) or blob(p)!=data[p]:
            raise ValueError("bytes not original Git: "+p)
    return data

def external(path):
    full=path.resolve()
    if full==ROOT.resolve() or ROOT.resolve() in full.parents:
        raise ValueError("stage/rollback cannot touch repository")

def stage(raw,target):
    external(target)
    if target.exists():raise ValueError("would overwrite existing destination")
    files=verify(raw)
    target.mkdir()
    try:
        for p,data in files.items():
            f=target/p
            f.parent.mkdir(parents=True,exist_ok=True)
            f.write_bytes(data)
    except Exception:
        shutil.rmtree(target)
        raise

def rollback(raw,target):
    external(target)
    original=verify(raw)
    if not target.is_dir() or target.is_symlink():
        raise ValueError("target missing or symlink")
    found=set()
    # R1.1 rollback must NOT silently delete foreign EMPTY directories.
    # A folder is owned only if it is the exact parent of an original member.
    allowed_dirs=set()
    for member in original:
        parts=Path(member).parts
        for level in range(1,len(parts)):
            allowed_dirs.add(Path(*parts[:level]).as_posix())
    for p in target.rglob("*"):
        if p.is_symlink():raise ValueError("foreign symlink")
        if p.is_dir() and p.relative_to(target).as_posix() not in allowed_dirs:
            raise ValueError("foreign empty directory")
        if p.is_file():
            name=p.relative_to(target).as_posix()
            if name not in original or p.read_bytes()!=original[name]:
                raise ValueError("changed/foreign rollback contents")
            found.add(name)
    if found!=set(original):
        raise ValueError("missing staged members")
    shutil.rmtree(target)

def main():
    a=argparse.ArgumentParser()
    a.add_argument("command",choices=["source-gate","build","verify","stage","rollback"])
    a.add_argument("--archive")
    a.add_argument("--destination")
    v=a.parse_args()
    if v.command=="source-gate":
        print("PASS",len(scope()),"original owned W10 files; public routes guarded")
        guard_contract()
        return
    if not v.archive:raise ValueError("--archive needed")
    p=Path(v.archive)
    if v.command=="build":
        if p.exists():raise ValueError("archive exists")
        raw=build()
        p.parent.mkdir(parents=True,exist_ok=True)
        p.write_bytes(raw)
        verify(raw)
        print("PASS INNER ZIP bytes",len(raw),"SHA256",sha(raw),"members",len(scope())+1,"source",source_sha())
        return
    raw=p.read_bytes()
    if v.command=="verify":
        verify(raw)
        print("PASS verified physical W10 ZIP",sha(raw))
    elif v.command=="stage":
        if not v.destination:raise ValueError("--destination needed")
        stage(raw,Path(v.destination));print("PASS stage scratch")
    elif v.command=="rollback":
        if not v.destination:raise ValueError("--destination needed")
        rollback(raw,Path(v.destination));print("PASS clean rollback")

if __name__=="__main__":
    try: main()
    except (ValueError,KeyError,OSError,subprocess.CalledProcessError,zipfile.BadZipFile) as exc:
        print("FAIL "+str(exc),file=sys.stderr);sys.exit(1)
