import hashlib,io,json,re,subprocess,sys,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
API="b8ca29c7707cd6b4338d0b9772d74dee676d49d2"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
ASSET="Assets/B0123CanonicalRigPortW24"
OWN="Tools/B01/B01-23/P17W24CanonicalPhysicalRigPort001/"
WORKFLOW=".github/workflows/b01-23-p17w24-physical510-port.yml"
MANIFEST="B01-23/W24/PACKAGE_MANIFEST.json"
PINS=[(GAME,"Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs"),
(GAME,"Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs"),
(SAVE,"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"),
(API,"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs"),
(API,"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs")]
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT)
def files():
    return sorted(x for x in git("ls-tree","-r","--name-only","HEAD").decode().splitlines()
        if x==ASSET+".meta" or x.startswith(ASSET+"/") or x.startswith(OWN) or x==WORKFLOW)
def source(x):return git("show","HEAD:"+x)
def guard():
    subprocess.run(["git","merge-base","--is-ancestor",API,"HEAD"],cwd=ROOT,check=True)
    changed=set(git("diff","--name-only",API,"HEAD").decode().splitlines())
    if changed!=set(files()) or len(changed)<21:raise ValueError("Foreign/missing source")
    for ref,path in PINS:
        if git("rev-parse",ref+":"+path)!=git("rev-parse","HEAD:"+path):
            raise ValueError("Original frozen file changed")
    metas=[x for x in files() if x.endswith(".meta")]
    ids=[]
    for x in metas:
        m=re.search(r"(?m)^guid: ([0-9a-f]{32})$",source(x).decode())
        if not m:raise ValueError("Missing Unity GUID")
        ids.append(m.group(1))
    if len(ids)!=9 or len(set(ids))!=9:raise ValueError("Duplicate/missing GUID")
    if len([x for x in files() if x.endswith(".asmdef")])!=2:
        raise ValueError("Two new Unity assemblies required")
    runtime=source(ASSET+"/Runtime/CanonicalPhysicalRigPortW24.cs").decode()
    for name in ("Rigidbody","BoxCollider","transform.SetParent(",
        "CanonicalRigLedgerHydratorW24.TryHydrate","CleanupUnpublishedAndVerifyDestroyed",
        "AllOwnedPhysicalRootsDestroyed","linearVelocity","angularVelocity"):
        if name not in runtime:raise ValueError("Missing physical runtime feature "+name)
    if "System.Reflection" in runtime or "GetField(" in runtime:
        raise ValueError("Forbidden private Rig reflection")
    if source(ASSET+"/Tests/PlayMode/CanonicalPhysicalRigPortW24NativeTests.cs").decode().count("[UnityTest]")!=3:
        raise ValueError("Missing three authored Unity tests")
def entry(name):
    x=zipfile.ZipInfo(name,(1980,1,1,0,0,0))
    x.compress_type=zipfile.ZIP_STORED;x.external_attr=0o100644<<16;x.create_system=3; x.flag_bits=0
    return x
def build():
    guard();data={x:source(x) for x in files()}
    manifest={"package":"B01-23-CANONICAL-510-PHYSICAL-PORT-001","version":"1.0.0",
        "head":git("rev-parse","HEAD").decode().strip(),"api_729":API,
        "frozen_510":GAME,"frozen_556":SAVE,
        "members":{x:hashlib.sha256(b).hexdigest() for x,b in data.items()},
        "unity_guids":9,"asmdefs":2,"native_g1":"NOT_RUN",
        "existing_private_510_rigs":"HOLD","live_save":"HOLD","codex_ack":False}
    stream=io.BytesIO()
    with zipfile.ZipFile(stream,"w") as z:
        for x in sorted(data):z.writestr(entry(x),data[x])
        z.writestr(entry(MANIFEST),json.dumps(manifest,sort_keys=True,separators=(",",":")).encode())
    return stream.getvalue()
def verify(raw):
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        names=z.namelist()
        if z.testzip()!=None or len(names)!=len(set(names)) or set(names)!=set(files())|{MANIFEST}:
            raise ValueError("Foreign/missing member")
        m=json.loads(z.read(MANIFEST))
        if m.get("native_g1")!="NOT_RUN" or m.get("codex_ack") or m.get("api_729")!=API:
            raise ValueError("Fake release")
        for x in files():
            b=z.read(x)
            if b!=source(x) or hashlib.sha256(b).hexdigest()!=m["members"][x]:
                raise ValueError("Modified original Git bytes "+x)
        return m
def stage(raw,dest):
    dest=Path(dest)
    if dest.exists() or dest.is_symlink() or ROOT in dest.resolve().parents:
        raise ValueError("Foreign receiver")
    verify(raw);dest.mkdir()
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        for x in z.namelist():
            p=dest/x;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(z.read(x))
def rollback(raw,dest):
    dest=Path(dest);verify(raw)
    if not dest.is_dir() or dest.is_symlink() or ROOT in dest.resolve().parents:
        raise ValueError("Unsafe receiver")
    with zipfile.ZipFile(io.BytesIO(raw)) as z:expected={x:z.read(x) for x in z.namelist()}
    seen=set()
    for p in dest.rglob("*"):
        if p.is_symlink():raise ValueError("Foreign symlink")
        name=p.relative_to(dest).as_posix()
        if p.is_file():seen.add(name)
        if p.is_dir() and not any(p.iterdir()) and not any(
            x.startswith(name+"/") for x in expected):raise ValueError("Foreign EMPTY dir")
    if seen!=set(expected):raise ValueError("Foreign/missing files")
    for name,content in expected.items():
        if (dest/name).read_bytes()!=content:raise ValueError("Dirty owned bytes")
    for name in expected:(dest/name).unlink()
    for p in sorted((x for x in dest.rglob("*") if x.is_dir()),
        key=lambda x:len(x.parts),reverse=True):p.rmdir()
    dest.rmdir()
if __name__=="__main__":
    raw=build();verify(raw)
    print("PASS ORIGINAL W24 INNER ZIP",len(raw),"bytes",len(files())+1,
        "members SHA256",hashlib.sha256(raw).hexdigest(),"HEAD",
        git("rev-parse","HEAD").decode().strip())
    if len(sys.argv)==3 and sys.argv[1]=="build":
        dest=ROOT/sys.argv[2];dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(raw)
