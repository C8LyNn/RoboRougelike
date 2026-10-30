#!/usr/bin/env python3
"""Original Git object Unity folder ZIP + embedded immutable manifest, never host installation."""
import hashlib
import io
import json
from pathlib import Path
import subprocess
import sys
import zipfile

FILES=["Assets/B0123WheelSupportArena001.meta","Assets/B0123WheelSupportArena001/WheelRigRecipe.cs","Assets/B0123WheelSupportArena001/WheelRigRecipe.cs.meta","Assets/B0123WheelSupportArena001/P17WheelSupportArena.cs","Assets/B0123WheelSupportArena001/P17WheelSupportArena.cs.meta","Assets/B0123WheelSupportArena001/Editor.meta","Assets/B0123WheelSupportArena001/Editor/P17WheelSupportSceneFactory.cs","Assets/B0123WheelSupportArena001/Editor/P17WheelSupportSceneFactory.cs.meta"]
PARENT="783d9ddc0b0c37e284c46c2d42a57fa8f67a2021"
SOLVER_PARENT="f1c82267b1ad3d03d67ce17b478a180a69e7d7d7"
def git(*args):return subprocess.check_output(["git",*args])
def digest(data):return hashlib.sha256(data).hexdigest()
def blob(data):return hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()
def receipt(files,sha):
    return {"package_id":"B01-23-P17-W09-WHEEL-SUPPORT-PHYSICS-ARENA-001",
            "version":"R1","original_source_sha":sha,
            "parent_measured_sampler_sha":PARENT,"parent_ideal_solver_sha":SOLVER_PARENT,
            "native_unity":"NOT_RUN","unity_editor":"NOT_RUN",
            "codex_ack":False,"integrated":False,
            "files":[{"path":p,"bytes":len(files[p]),"sha256":digest(files[p]),
                      "git_blob_sha1":blob(files[p])} for p in sorted(files)]}
def zi(path):
    info=zipfile.ZipInfo(path,(1980,1,1,0,0,0))
    info.create_system=3;info.compress_type=zipfile.ZIP_STORED
    info.external_attr=(0o100644<<16)
    return info
def build():
    sha=git("rev-parse","HEAD").decode().strip()
    files={p:git("show",sha+":"+p) for p in FILES}
    manifest=receipt(files,sha)
    out=io.BytesIO()
    with zipfile.ZipFile(out,"w") as z:
        for p in sorted(files):z.writestr(zi(p),files[p])
        z.writestr(zi("PACKAGE_MANIFEST.json"),(json.dumps(manifest,sort_keys=True,indent=2)+"\n").encode())
    return out.getvalue(),manifest
def verify(raw):
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        names=z.namelist()
        if len(names)!=len(FILES)+1 or len(set(names))!=len(names) or set(names)!=set(FILES)|{"PACKAGE_MANIFEST.json"}:
            raise ValueError("MEMBER_SET_OR_DUPLICATE")
        if any(p.startswith("/") or any(x in ("",".","..") for x in p.split("/")) or "\\" in p for p in names):
            raise ValueError("UNSAFE_PATH")
        if any(i.file_size>200000 for i in z.infolist()):raise ValueError("OVERSIZE_ENTRY")
        files={p:z.read(p) for p in FILES}
        manifest=json.loads(z.read("PACKAGE_MANIFEST.json"))
        if manifest!=receipt(files,manifest["original_source_sha"]):raise ValueError("EMBEDDED_RECEIPT_MISMATCH")
        for item in manifest["files"]:
            if git("rev-parse",manifest["original_source_sha"]+":"+item["path"]).decode().strip()!=item["git_blob_sha1"]:
                raise ValueError("GIT_SOURCE_BLOB_MISMATCH")
        return manifest
def stage(raw,dest):
    verify(raw)
    if dest.exists() or dest.is_symlink():raise ValueError("REFUSE_OVERWRITE_DEST")
    if not dest.resolve().parent.is_dir():raise ValueError("BAD_DEST_PARENT")
    inside=subprocess.run(["git","-C",str(dest.resolve().parent),"rev-parse","--show-toplevel"],
                          stdout=subprocess.PIPE,stderr=subprocess.DEVNULL)
    if inside.returncode==0:raise ValueError("DEST_INSIDE_GIT_WORKTREE")
    dest.mkdir()
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        for p in FILES:
            item=dest/p;item.parent.mkdir(parents=True,exist_ok=True);item.write_bytes(z.read(p))
    (dest/".owned-b01-p17w09").write_text(digest(raw)+"\n")
def rollback(raw,dest):
    verify(raw)
    if dest.is_symlink() or not dest.is_dir():raise ValueError("STAGE_MISSING")
    m=dest/".owned-b01-p17w09"
    if not m.is_file() or m.is_symlink() or m.read_text()!=digest(raw)+"\n":
        raise ValueError("OWNERSHIP_MARKER_MISMATCH")
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        expected={p:z.read(p) for p in FILES}
    actual=set()
    for item in dest.rglob("*"):
        if item.is_symlink():raise ValueError("FOREIGN_SYMLINK")
        if item.is_file():
            rel=item.relative_to(dest).as_posix()
            actual.add(rel)
            if rel not in set(expected)|{".owned-b01-p17w09"}:
                raise ValueError("FOREIGN_FILE_REFUSE_DELETE")
            if rel in expected and item.read_bytes()!=expected[rel]:
                raise ValueError("CHANGED_FILE_REFUSE_DELETE")
    if actual!=set(expected)|{".owned-b01-p17w09"}:raise ValueError("MISSING_PACKAGE_FILE")
    # Fail closed for foreign even EMPTY subdirectories: rmtree may not delete unowned dirs.
    owned_dirs=set()
    for p in FILES:
        parts=Path(p).parts
        for i in range(1,len(parts)):
            owned_dirs.add(Path(*parts[:i]).as_posix())
    for item in dest.rglob("*"):
        if item.is_dir() and item.relative_to(dest).as_posix() not in owned_dirs:
            raise ValueError("FOREIGN_DIRECTORY_REFUSE_DELETE")
    import shutil
    shutil.rmtree(dest)
if __name__=="__main__":
    original,manifest=build()
    target=Path(sys.argv[1]);receipt_path=Path(sys.argv[2])
    if target.exists() or receipt_path.exists():raise ValueError("NO_OVERWRITE")
    target.write_bytes(original);receipt_path.write_text(json.dumps(manifest,sort_keys=True,indent=2)+"\n")
    verify(original)
    print("ORIGINAL_UNITY_W09_ZIP_BYTES="+str(len(original))+
          " SHA256="+digest(original)+" HEAD="+manifest["original_source_sha"])
