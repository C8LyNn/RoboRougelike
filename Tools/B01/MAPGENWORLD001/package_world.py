#!/usr/bin/env python3
"""Deterministic, scoped MAPGENWORLD001 full import package + hash/GUID verifier."""
import hashlib
import io
import json
from pathlib import Path
import re
import sys
import zipfile

ROOT = Path(__file__).resolve().parents[3]
ASSET = "Assets/B01Packages/MAPGENWORLD001"
META = ASSET + ".meta"
DOCS = "Docs/B01/MAPGENWORLD001"
TOOLS = "Tools/B01/MAPGENWORLD001"
UPSTREAM = "5be31039411f89f17654b1c332f8692883fa03cc"
MANIFEST = "META-INF/MAPGENWORLD001_MANIFEST.json"
EPOCH = (1980,1,1,0,0,0)
# Deterministic source ZIP excludes all .NET/Unity caches and compiler outputs.
GENERATED_COMPONENTS = frozenset({
    "obj", "bin", "__pycache__", ".pytest_cache", ".mypy_cache",
    ".vs", ".idea", ".vscode", "Library", "Temp", "Artifacts"
})
GENERATED_EXTENSIONS = (".pyc", ".pyo", ".pdb", ".dll", ".zip", ".tmp", ".bak")

def is_generated(path):
    return any(part in GENERATED_COMPONENTS for part in Path(path).parts) or path.endswith(GENERATED_EXTENSIONS)

def sha(data):
    return hashlib.sha256(data).hexdigest()

def owned(path):
    return path == META or path.startswith(ASSET + "/") or path.startswith(DOCS + "/") or path.startswith(TOOLS + "/")

def gather(root=ROOT):
    found = {META: (root / META).read_bytes()}
    for prefix in [ASSET, DOCS, TOOLS]:
        folder = root / prefix
        if not folder.is_dir(): raise ValueError("MISSING_FOLDER " + prefix)
        for f in folder.rglob("*"):
            if f.is_symlink(): raise ValueError("SYMLINK " + str(f))
            if not f.is_file(): continue
            rel = f.relative_to(root).as_posix()
            if is_generated(rel):
                continue
            if not owned(rel): raise ValueError("SCOPE " + rel)
            found[rel] = f.read_bytes()
    validate_assets(found)
    return dict(sorted(found.items()))

def validate_assets(files):
    guids = {}
    for p in files:
        if not owned(p) or p.startswith("/") or ".." in Path(p).parts or "\\" in p:
            raise ValueError("PATH_NOT_OWNED " + p)
        if is_generated(p): raise ValueError("GENERATED_FILE_NOT_ALLOWED " + p)
        if p.startswith(ASSET + "/") and not p.endswith(".meta"):
            if p + ".meta" not in files: raise ValueError("MISSING_META " + p)
        if p == META or p.startswith(ASSET + "/") and p.endswith(".meta"):
            text = files[p].decode("utf-8")
            m = re.search(r"(?m)^guid: ([a-f0-9]{32})$",text)
            if not m: raise ValueError("INVALID_GUID " + p)
            if m.group(1) in guids: raise ValueError("DUPLICATE_GUID "+p+" "+guids[m.group(1)])
            guids[m.group(1)] = p
    for sub in ["Runtime","Tests","Tests/EditMode","Tests/PlayMode"]:
        if f"{ASSET}/{sub}.meta" not in files: raise ValueError("MISSING_FOLDER_META "+sub)
    return guids

def create_zip(files):
    guids = validate_assets(files)
    info = {"schemaVersion":1,"package":"MAPGEN-WORLD-ADAPTER-001","version":"1.0.1",
            "upstreamMAPGEN001":UPSTREAM,"unityNative":"NOT_RUN","g2":"HOLD",
            "preserveTargetParentMeta":"Assets/B01Packages.meta",
            "guidCount":len(guids),
            "files":[{"path":p,"size":len(b),"sha256":sha(b)} for p,b in sorted(files.items())]}
    payload = json.dumps(info,sort_keys=True,indent=2).encode("utf-8")+b"\n"
    entries = dict(files)
    entries[MANIFEST] = payload
    buff=io.BytesIO()
    with zipfile.ZipFile(buff,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
        for path, data in sorted(entries.items()):
            meta=zipfile.ZipInfo(path,EPOCH)
            meta.compress_type=zipfile.ZIP_DEFLATED
            meta.external_attr=0o100644 << 16
            z.writestr(meta,data,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
    return buff.getvalue(),info

def verify_zip(data):
    with zipfile.ZipFile(io.BytesIO(data),"r") as z:
        members=z.infolist()
        names=[m.filename for m in members]
        if len(names)!=len(set(names)): raise ValueError("DUPLICATE_ZIP_ENTRY")
        if MANIFEST not in names: raise ValueError("MANIFEST_MISSING")
        for name in names:
            if name!=MANIFEST and not owned(name) or name.startswith("/") or ".." in Path(name).parts or "\\" in name:
                raise ValueError("ZIP_TRAVERSAL "+name)
            if name!=MANIFEST and is_generated(name):
                raise ValueError("GENERATED_FILE_NOT_ALLOWED "+name)
        manifest=json.loads(z.read(MANIFEST))
        if manifest.get("upstreamMAPGEN001")!=UPSTREAM: raise ValueError("UPSTREAM_PIN_MISMATCH")
        expected={f["path"]:f for f in manifest["files"]}
        if set(names)-{MANIFEST} != set(expected): raise ValueError("ZIP_MANIFEST_FILESET_MISMATCH")
        files={}
        for name,item in expected.items():
            actual=z.read(name)
            if sha(actual)!=item["sha256"] or len(actual)!=item["size"]:
                raise ValueError("HASH_OR_SIZE_MISMATCH "+name)
            files[name]=actual
        guids=validate_assets(files)
        if len(guids)!=manifest["guidCount"]: raise ValueError("GUID_COUNT")
        return manifest

def main():
    if len(sys.argv)<3 or sys.argv[1] not in ("build","verify"):
        raise SystemExit("usage: package_world.py build <output-dir> | verify <package.zip>")
    if sys.argv[1]=="build":
        out=Path(sys.argv[2]);out.mkdir(parents=True,exist_ok=True)
        data,manifest=create_zip(gather())
        filename=out/"MAPGENWORLD001_SOURCE_FULL.zip"
        filename.write_bytes(data)
        (out/"MANIFEST.json").write_text(json.dumps(manifest,sort_keys=True,indent=2)+"\n")
        (out/"SHA256SUMS.txt").write_text(sha(data)+"  "+filename.name+"\n")
        assert len(verify_zip(data)["files"])==len(manifest["files"])
        print("PACKAGE_WORLD_BUILD_PASS files="+str(len(manifest["files"]))+
              " guid="+str(manifest["guidCount"])+" bytes="+str(len(data))+
              " sha256="+sha(data))
    else:
        archive=Path(sys.argv[2]);man=verify_zip(archive.read_bytes())
        print("PACKAGE_WORLD_VERIFY_PASS files="+str(len(man["files"]))+
              " guid="+str(man["guidCount"])+" sha256="+sha(archive.read_bytes()))

if __name__=="__main__":
    main()
