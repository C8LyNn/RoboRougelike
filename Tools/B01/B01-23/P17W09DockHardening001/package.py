#!/usr/bin/env python3
"""Git-original, deterministic W08 physical source ZIP, fail-closed install and rollback."""
import argparse
import hashlib
import io
import json
from pathlib import Path
import shutil
import subprocess
import sys
import zipfile

ROOT = Path(__file__).resolve().parents[4]
ASSET = "Assets/B0123DockHardening001"
OWN = "Tools/B01/B01-23/P17W09DockHardening001/"
CI = ".github/workflows/b01-23-p17w09-safedock-hardening.yml"
PARENT = "99dfbd25d471f9983f9472b12d3725f4c002f7e9"
REQUIRES_W06 = "cb2b3a76e84266e2730fffc4ab048c1039f0652f"
FROZEN_BLOBS = {
    "Assets/B0123ReattachClearance001/Runtime/DebrisSafeReattachGate.cs": "7c7e96a10d69149119309863a0c2a9bdab829072",
    "Assets/B0123ReattachClearance001/Runtime/DockClearancePolicy.cs": "de0a847dbecacb92ad63a09ca12f5c9c372d597a",
    "Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs": "e4e31c32cb5abda2fef987efa4b45fd61e828312",
}
ID = "B01-23-SAFEDOCK-HARDENING-001"
MANIFEST = "PACKAGE_MANIFEST.json"

def g(*argv):
    return subprocess.check_output(["git", *argv], cwd=ROOT).decode().strip()

def git_blob(path):
    return subprocess.check_output(["git", "show", "HEAD:" + path], cwd=ROOT)

def digest(payload):
    return hashlib.sha256(payload).hexdigest()

def source_sha():
    return g("rev-parse", "HEAD")

def original_scope():
    subprocess.run(["git", "merge-base", "--is-ancestor", PARENT, "HEAD"],
                   cwd=ROOT, stdout=subprocess.DEVNULL, check=True)
    subprocess.run(["git", "merge-base", "--is-ancestor", REQUIRES_W06, "HEAD"],
                   cwd=ROOT, stdout=subprocess.DEVNULL, check=True)
    for path, sha in FROZEN_BLOBS.items():
        if g("rev-parse", "HEAD:" + path) != sha:
            raise ValueError("Original W06/W07 source changed: " + path)
    assets = g("ls-tree", "-r", "--name-only", "HEAD", ASSET + "/").splitlines()
    own = g("ls-tree", "-r", "--name-only", "HEAD", OWN).splitlines()
    if not assets or not own:
        raise ValueError("W08 game source or tools missing")
    files = sorted(assets + own + [ASSET + ".meta", CI])
    if len(files) != len(set(files)) or any(
        not (p.startswith(ASSET + "/") or p.startswith(OWN) or p in (ASSET + ".meta", CI))
        for p in files):
        raise ValueError("duplicate or cross-owner source")
    meta_ids = set()
    count = 0
    for path in files:
        data = git_blob(path)
        if path.endswith(".meta"):
            count += 1
            lines = [line.split(":", 1)[1].strip() for line in data.decode().splitlines()
                     if line.startswith("guid:")]
            if len(lines) != 1 or len(lines[0]) != 32 or any(c not in "0123456789abcdef" for c in lines[0]):
                raise ValueError("invalid Unity GUID record " + path)
            if lines[0] in meta_ids:
                raise ValueError("duplicate Unity GUID " + path)
            meta_ids.add(lines[0])
    if count != 12:
        raise ValueError("expected 12 original Unity .meta GUID records; got " + str(count))
    asm = {}
    for path in files:
        if path.endswith(".asmdef"):
            info = json.loads(git_blob(path))
            asm[info["name"]] = info
    expected = {
        "B0123.DockHardening.Runtime": ["B0123.DebrisPlayground.Runtime", "B0123.ReattachClearance.Runtime"],
        "B0123.DockHardening.Editor": ["B0123.DockHardening.Runtime", "B0123.DebrisPlayground.Runtime", "B0123.ReattachClearance.Runtime"],
        "B0123.DockHardening.PlayMode": ["B0123.DockHardening.Runtime", "B0123.DebrisPlayground.Runtime", "B0123.ReattachClearance.Runtime"],
    }
    if {k: sorted(v["references"]) for k,v in asm.items()} != {k: sorted(v) for k,v in expected.items()}:
        raise ValueError("wrong or missing Unity assembly references")
    if asm["B0123.DockHardening.Editor"]["includePlatforms"] != ["Editor"]:
        raise ValueError("Editor assembly not isolated")
    if "UNITY_INCLUDE_TESTS" not in asm["B0123.DockHardening.PlayMode"]["defineConstraints"]:
        raise ValueError("PlayMode test compilation constraints missing")
    for required in (
        ASSET + "/Runtime/HardenedDockPolicy.cs",
        ASSET + "/Runtime/HardenedDockDemo.cs",
        ASSET + "/Editor/HardenedDockInstaller.cs",
        ASSET + "/Tests/PlayMode/W09ProtectedDockPlayModeTests.cs",
        OWN + "Program.cs", OWN + "DockSourceTests.csproj",
        OWN + "package.py", OWN + "test_package.py", CI,
    ):
        if required not in files:
            raise ValueError("missing owned deliverable: " + required)
    return files, count

def info(path):
    z=zipfile.ZipInfo(path,date_time=(1980,1,1,0,0,0))
    z.compress_type=zipfile.ZIP_STORED
    z.create_system=3
    z.external_attr=0o100644<<16
    return z

def produce():
    files, metas = original_scope()
    content={path:git_blob(path) for path in files}
    receipt={
        "schema":"B01-ORIGINAL-SOURCE-ZIP-1",
        "package_id":ID, "version":"1.0.0",
        "source_sha":source_sha(), "owner":"B01-23",
        "requires_w08_sha":PARENT,
        "requires_w06_sha":REQUIRES_W06,
        "requires_original_git_blobs":FROZEN_BLOBS,
        "unity_engine_native":"NOT_RUN",
        "independent_qa":"PENDING",
        "codex_ack":False,
        "meta_guid_count":metas,
        "files":[{"path":p,"bytes":len(content[p]),"sha256":digest(content[p])} for p in files],
    }
    content[MANIFEST]=(json.dumps(receipt,sort_keys=True,separators=(",",":"))+"\n").encode("utf-8")
    buff=io.BytesIO()
    with zipfile.ZipFile(buff,"w",compression=zipfile.ZIP_STORED) as archive:
        for path in sorted(content):
            archive.writestr(info(path),content[path])
    return buff.getvalue(), receipt

def verify(payload):
    with zipfile.ZipFile(io.BytesIO(payload),"r") as archive:
        names=[entry.filename for entry in archive.infolist()]
        expected=set(original_scope()[0])|{MANIFEST}
        if len(names)!=len(set(names)) or set(names)!=expected or any(
            p.startswith("/") or "\\" in p or ".." in p.split("/") for p in names):
            raise ValueError("unexpected, missing, duplicate, traversal ZIP member")
        content={name:archive.read(name) for name in names}
    receipt=json.loads(content[MANIFEST].decode("utf-8"))
    if receipt.get("package_id")!=ID or receipt.get("source_sha")!=source_sha() or \
       receipt.get("requires_w08_sha")!=PARENT or receipt.get("requires_w06_sha")!=REQUIRES_W06 or \
       receipt.get("codex_ack") is not False or receipt.get("unity_engine_native")!="NOT_RUN":
        raise ValueError("forged source / Unity / Codex provenance")
    if receipt.get("requires_original_git_blobs")!=FROZEN_BLOBS:
        raise ValueError("frozen original W06/W07 Git blob pins mismatch")
    files=original_scope()[0]
    records=receipt.get("files")
    if not isinstance(records,list) or [r.get("path") for r in records]!=files:
        raise ValueError("manifest file list mismatch")
    for row in records:
        path=row["path"]
        raw=content[path]
        if row.get("bytes")!=len(raw) or row.get("sha256")!=digest(raw) or raw!=git_blob(path):
            raise ValueError("Git source mismatch " + path)
    return content

def safe_destination(path):
    real=path.resolve()
    if real==ROOT.resolve() or ROOT.resolve() in real.parents:
        raise ValueError("cannot stage or rollback in source repository")

def stage(payload,destination):
    safe_destination(destination)
    if destination.exists(): raise ValueError("destination exists; would overwrite")
    content=verify(payload)
    destination.mkdir(parents=False)
    try:
        for path,data in content.items():
            target=destination/path
            target.parent.mkdir(parents=True,exist_ok=True)
            target.write_bytes(data)
    except Exception:
        shutil.rmtree(destination)
        raise

def rollback(payload,destination):
    safe_destination(destination)
    content=verify(payload)
    if not destination.is_dir() or destination.is_symlink():
        raise ValueError("not an isolated normal directory")
    found=set()
    for path in destination.rglob("*"):
        if path.is_symlink(): raise ValueError("symlink in staged directory")
        if path.is_file():
            rel=path.relative_to(destination).as_posix()
            if rel not in content or path.read_bytes()!=content[rel]:
                raise ValueError("foreign/modified file; rollback refused")
            found.add(rel)
    if found!=set(content):
        raise ValueError("missing file; rollback refused")
    shutil.rmtree(destination)

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("command",choices=["source-gate","build","verify","stage","rollback"])
    parser.add_argument("--archive")
    parser.add_argument("--destination")
    args=parser.parse_args()
    if args.command=="source-gate":
        files,meta=original_scope()
        print("PASS original source G0",len(files),"tracked W08 entries",meta,"owned Unity GUIDs")
        return
    if not args.archive: raise ValueError("--archive required")
    archive=Path(args.archive)
    if args.command=="build":
        if archive.exists(): raise ValueError("archive already exists")
        payload,_=produce()
        archive.parent.mkdir(parents=True,exist_ok=True)
        archive.write_bytes(payload)
        verify(payload)
        print("PASS INNER ZIP bytes",len(payload),"SHA256",digest(payload),"members",len(original_scope()[0])+1,"source",source_sha())
    else:
        payload=archive.read_bytes()
        if args.command=="verify":
            verify(payload)
            print("PASS original ZIP integrity SHA256",digest(payload))
        elif args.command=="stage":
            if not args.destination: raise ValueError("--destination required")
            stage(payload,Path(args.destination))
            print("PASS disposable stage")
        elif args.command=="rollback":
            if not args.destination: raise ValueError("--destination required")
            rollback(payload,Path(args.destination))
            print("PASS verified non-destructive rollback")

if __name__=="__main__":
    try:main()
    except (OSError,KeyError,ValueError,TypeError,subprocess.CalledProcessError,zipfile.BadZipFile) as error:
        print("FAIL "+str(error),file=sys.stderr)
        sys.exit(1)
