#!/usr/bin/env python3
"""W23 immutable Git source-only package, exact SHA and safe disposable rollback."""
import io,json,hashlib,zipfile,subprocess,tempfile
from pathlib import Path
from source_prepare import ROOT,OWNER,ORIGINAL,B0162_REF,B0162_PATH,B0162_BLOB,V3_REF,check,git
MANIFEST="B01-23/W23/PACKAGE_MANIFEST.json"  # unique, avoid flat manifest overlay
PFX="Assets/B0123CanonicalSaveApi001"
OWNER_PATH="Tools/B01/B01-23/P17W23UnifiedAuthority001/"
WF=".github/workflows/b01-23-p17w23-unified-authority-v4.yml"
V3_WF=".github/workflows/b01-23-p17w22-canonical-authority-v3.yml"
def src(p):return subprocess.check_output(["git","show","HEAD:"+p],cwd=ROOT)
def sha(b):return hashlib.sha256(b).hexdigest()
def entries():
    check()
    names=git("ls-tree","-r","--name-only","HEAD",OWNER_PATH).splitlines()
    names=[x for x in names if "/.generated/" not in x]
    expect={"CanonicalUnifiedSlotSession.cs","Program.cs","UnifiedOriginalSourceQA.csproj",
        "source_prepare.py","package.py","test_package.py","README.md"}
    if {Path(x).name for x in names}!=expect:raise ValueError("wrong owner scope")
    for p in [PFX+"/Core/CanonicalWorldGateway.cs",PFX+"/Core/CanonicalWorldSlotLease.cs",
        PFX+"/Core/CanonicalWorldSlotLease.cs.meta",WF,V3_WF]:names.append(p)
    if len(names)!=len(set(names)):raise ValueError("duplicate")
    lease=src(PFX+"/Core/CanonicalWorldSlotLease.cs").decode()
    if "public static bool TryAcquire(" in lease or "TryAdoptRealSlot" not in lease or "new FileStream(" in lease:
        raise ValueError("standalone .owner bypass remains")
    program=src(OWNER_PATH+"CanonicalUnifiedSlotSession.cs").decode()
    for x in ["SingleFileSaveSlot.TryOpenExisting","TryAdoptRealSlot",
              "TryCloseAfterPhysicalRetirement","if(gateway==null)","slot.lease"]:
        if x not in program:raise ValueError("missing unified boundary "+x)
    original_meta=src(PFX+"/Core/CanonicalWorldSlotLease.cs.meta").decode()
    if "guid: b0123220000000000000000000000001" not in original_meta:
        raise ValueError("changed canonical Unity GUID")
    return sorted(names)
def newzip():
    names=entries();data={x:src(x) for x in names}
    man={"package":"B01-23-CANONICAL-ONE-AUTHORITY-001","version":"4.0.0",
        "commit":git("rev-parse","HEAD"),"selected_parent_v3":V3_REF,
        "single_physical_lease":"ORIGINAL_B0162_669_SLOT.LEASE_ONLY",
        "dependency_669_commit":B0162_REF,"dependency_669_blob":B0162_BLOB,
        "dependency_669_path":B0162_PATH,
        "original_frozen_blobs":ORIGINAL,"native_G1":"NOT_RUN",
        "full_510_private_unity_adapter":"NOT_IMPLEMENTED","codex_ack":False,
        "source":[{"path":n,"sha256":sha(data[n]),"bytes":len(data[n])} for n in names]}
    data[MANIFEST]=(json.dumps(man,sort_keys=True,separators=(",",":"))+"\n").encode()
    b=io.BytesIO()
    with zipfile.ZipFile(b,"w",compression=zipfile.ZIP_STORED) as z:
        for p in sorted(data):
            inf=zipfile.ZipInfo(p,(1980,1,1,0,0,0));inf.compress_type=zipfile.ZIP_STORED
            inf.create_system=3;inf.external_attr=0o100644<<16
            z.writestr(inf,data[p])
    return b.getvalue()
def verify(payload):
    expected=entries()
    with zipfile.ZipFile(io.BytesIO(payload)) as z:
        files=z.namelist()
        if set(files)!=set(expected)|{MANIFEST} or len(set(files))!=len(files):
            raise ValueError("exact scope mismatch")
        for x in files:
            if Path(x).is_absolute() or ".." in Path(x).parts or "\\" in x:
                raise ValueError("traversal")
        m=json.loads(z.read(MANIFEST))
        if (m["commit"]!=git("rev-parse","HEAD") or
            m["selected_parent_v3"]!=V3_REF or
            m["dependency_669_blob"]!=B0162_BLOB or
            m["single_physical_lease"]!="ORIGINAL_B0162_669_SLOT.LEASE_ONLY" or
            m["native_G1"]!="NOT_RUN" or m["codex_ack"] is not False or
            m["full_510_private_unity_adapter"]!="NOT_IMPLEMENTED"):
            raise ValueError("false provenance/gate")
        if [x["path"] for x in m["source"]]!=expected:raise ValueError("manifest path order")
        for e in m["source"]:
            raw=z.read(e["path"])
            if raw!=src(e["path"]) or sha(raw)!=e["sha256"] or len(raw)!=e["bytes"]:
                raise ValueError("changed original bytes")
        return m
def stage(payload,target):
    verify(payload);dest=Path(target)
    if dest.exists() or dest.is_symlink() or dest.resolve().is_relative_to(ROOT.resolve()):
        raise ValueError("non-exclusive receiver")
    dest.mkdir(parents=True)
    with zipfile.ZipFile(io.BytesIO(payload)) as z:
        for p in z.namelist():
            f=dest/p;f.parent.mkdir(parents=True,exist_ok=True)
            with f.open("xb") as w:w.write(z.read(p))
    return dest
def rollback(payload,target):
    verify(payload);dest=Path(target)
    with zipfile.ZipFile(io.BytesIO(payload)) as z:
        for p in z.namelist():
            f=dest/p
            if f.is_symlink() or not f.is_file() or f.read_bytes()!=z.read(p):
                raise ValueError("foreign/dirty rollback refused")
        for p in z.namelist():(dest/p).unlink()
    for path in sorted({dest/Path(p).parent for p in entries()+[MANIFEST]},
        key=lambda p:len(p.parts),reverse=True):
        if path.is_dir() and not any(path.iterdir()):path.rmdir()
    # Any unrelated EMPTY directories remain intentionally untouched.
if __name__=="__main__":
    b=newzip();verify(b)
    print("PASS ORIGINAL W23 ONE-LEASE INNER ZIP",len(b),"entries",len(zipfile.ZipFile(io.BytesIO(b)).namelist()),"SHA256",sha(b),"HEAD",git("rev-parse","HEAD"),"NATIVE_G1_NOT_RUN")
