#!/usr/bin/env python3
"""W10 v2 original 3-file Unity source delta pack; frozen source-parent SHA never overwritten."""
import hashlib,io,json,sys,subprocess,zipfile
from pathlib import Path
FILES=["Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs","Assets/B0123DockHardening001/Tests/PlayMode/W10PublicDockGuardPlayModeTests.cs","Assets/B0123DockHardening001/Tests/PlayMode/W10PublicDockGuardPlayModeTests.cs.meta"]
FROZEN_OLD="cdcaea750fda1a9d7db6018e34970f4b9f206427"
FROZEN_W08="99dfbd25d471f9983f9472b12d3725f4c002f7e9"
FROZEN_W09="763748f1be0c61356fb4462144640bb359364226"
FROZEN_OLD_BLOB="e4e31c32cb5abda2fef987efa4b45fd61e828312"
def git(*a):return subprocess.check_output(["git",*a])
def digest(x):return hashlib.sha256(x).hexdigest()
def blob(x):return hashlib.sha1(b"blob "+str(len(x)).encode()+b"\0"+x).hexdigest()
def zi(path):
    z=zipfile.ZipInfo(path,(1980,1,1,0,0,0));z.create_system=3
    z.external_attr=0o100644<<16;z.compress_type=zipfile.ZIP_STORED
    return z
def receipt(files,sha):
    return {"package_id":"B01-23-P17-REATTACH-PUBLIC-API-GUARD","version":"R2-W10",
    "producer_source_sha":sha,"original_legacy_sha":FROZEN_OLD,
    "original_legacy_blob":FROZEN_OLD_BLOB,"unchanged_w08_sha":FROZEN_W08,
    "unchanged_w09_sha":FROZEN_W09,"native_unity":"NOT_RUN","codex_ack":False,
    "files":[{"path":p,"bytes":len(files[p]),"sha256":digest(files[p]),
              "git_blob":blob(files[p])} for p in sorted(files)]}
def build():
    sha=git("rev-parse","HEAD").decode().strip()
    files={p:git("show",sha+":"+p) for p in FILES}
    record=receipt(files,sha)
    buf=io.BytesIO()
    with zipfile.ZipFile(buf,"w") as z:
        for p in sorted(files):z.writestr(zi(p),files[p])
        z.writestr(zi("PACKAGE_MANIFEST.json"),(json.dumps(record,indent=2,sort_keys=True)+"\n").encode())
    return buf.getvalue(),record
def verify(data):
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        names=z.namelist()
        if set(names)!=set(FILES)|{"PACKAGE_MANIFEST.json"} or len(names)!=4 or len(set(names))!=4:raise ValueError("MEMBER_SET")
        if any(p.startswith("/") or ".." in p.split("/") or "\\" in p for p in names):raise ValueError("TRAVERSAL")
        if any(info.file_size>200000 for info in z.infolist()):raise ValueError("MEMBER_TOO_LARGE")
        files={p:z.read(p) for p in FILES}
        manifest=json.loads(z.read("PACKAGE_MANIFEST.json"))
        if manifest!=receipt(files,manifest["producer_source_sha"]):raise ValueError("MANIFEST_TAMPER")
        for row in manifest["files"]:
            if git("rev-parse",manifest["producer_source_sha"]+":"+row["path"]).decode().strip()!=row["git_blob"]:
                raise ValueError("GIT_SOURCE_MISMATCH")
        if manifest["original_legacy_blob"]!=FROZEN_OLD_BLOB:raise ValueError("LEGACY_PIN_MISMATCH")
        # Every published v2 MUST actually differ from unsafe original.
        if manifest["files"][0]["path"]=="Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs":
            if manifest["files"][0]["git_blob"]==FROZEN_OLD_BLOB:raise ValueError("STILL_OLD_UNGUARDED")
        return manifest
def stage(raw,dest):
    verify(raw)
    if dest.exists() or dest.is_symlink():raise ValueError("STAGE_EXISTS")
    if subprocess.run(["git","-C",str(dest.parent.resolve()),"rev-parse","--show-toplevel"],
        stdout=subprocess.PIPE,stderr=subprocess.DEVNULL).returncode==0:
        raise ValueError("STAGE_INSIDE_REPO")
    dest.mkdir()
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        for p in FILES:
            out=dest/p;out.parent.mkdir(parents=True,exist_ok=True);out.write_bytes(z.read(p))
    (dest/".owned-w10-dock").write_text(digest(raw))
def rollback(raw,dest):
    verify(raw)
    if dest.is_symlink() or not dest.is_dir():raise ValueError("NOT_OWNED_STAGE")
    marker=dest/".owned-w10-dock"
    if not marker.is_file() or marker.is_symlink() or marker.read_text()!=digest(raw):
        raise ValueError("MARKER_MISMATCH")
    with zipfile.ZipFile(io.BytesIO(raw)) as z:expected={p:z.read(p) for p in FILES}
    seen=set();approved=set()
    for p in FILES:
        xs=Path(p).parts
        for i in range(1,len(xs)):approved.add(Path(*xs[:i]).as_posix())
    for path in dest.rglob("*"):
        if path.is_symlink():raise ValueError("FOREIGN_SYMLINK")
        rel=path.relative_to(dest).as_posix()
        if path.is_dir() and rel not in approved:raise ValueError("FOREIGN_DIRECTORY")
        if path.is_file():
            seen.add(rel)
            if rel not in set(FILES)|{".owned-w10-dock"}:raise ValueError("FOREIGN_FILE")
            if rel in expected and path.read_bytes()!=expected[rel]:raise ValueError("MODIFIED_FILE")
    if seen!=set(FILES)|{".owned-w10-dock"}:raise ValueError("MISSING_FILE")
    import shutil
    shutil.rmtree(dest)
if __name__=="__main__":
    raw,manifest=build();p=Path(sys.argv[1]);q=Path(sys.argv[2])
    if p.exists() or q.exists():raise ValueError("NO_OVERWRITE")
    p.write_bytes(raw);verify(raw)
    q.write_text(json.dumps(manifest,indent=2,sort_keys=True)+"\n")
    print("ORIGINAL_W10_REAL_DELTA_ZIP_BYTES="+str(len(raw))+" SHA256="+digest(raw)+
          " PRODUCER_SHA="+manifest["producer_source_sha"])
