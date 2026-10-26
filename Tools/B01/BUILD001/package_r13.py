#!/usr/bin/env python3
"""Deterministic full BUILD001 R1.3 package. Standard library, no engine or network."""
import argparse
import hashlib
import io
import json
from pathlib import Path, PurePosixPath
import re
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[3]
PREFIXES = (
    "Assets/B01Construction.meta",
    "Assets/B01Construction/BUILD001.meta",
    "Assets/B01Construction/BUILD001/",
    "Docs/B01/BUILD001/",
    "Tools/B01/BUILD001/",
)
BASE_SHA = "489676414a3eb45fe70124098e3778a81d0aaff2"


def source_paths():
    names = subprocess.check_output(["git", "ls-files", "-z", "--",
        "Assets/B01Construction.meta", "Assets/B01Construction/BUILD001.meta",
        "Assets/B01Construction/BUILD001",
        "Docs/B01/BUILD001", "Tools/B01/BUILD001"],
        cwd=ROOT).decode().split("\x00")
    return sorted(n for n in names if n and not n.endswith(".pyc"))


def safe_path(path):
    posix = PurePosixPath(path)
    return (path == posix.as_posix() and not posix.is_absolute()
        and ".." not in posix.parts and "\\" not in path
        and any(path == p or path.startswith(p) for p in PREFIXES))


def validate_source(files):
    if len(files) != len(set(files)): raise ValueError("duplicate files")
    if any(not safe_path(p) for p in files): raise ValueError("path escape")
    if not files or not all((ROOT / p).is_file() for p in files):
        raise ValueError("missing source")
    assets = [p for p in files if p.startswith("Assets/")]
    for asset in assets:
        if asset.endswith(".meta"): continue
        if asset + ".meta" not in files: raise ValueError("missing meta " + asset)
    guid_to_path = {}
    for meta in (p for p in assets if p.endswith(".meta")):
        content = (ROOT / meta).read_text(encoding="utf8")
        match = re.search(r"(?m)^guid: ([0-9a-f]{32})$", content)
        if not match: raise ValueError("bad GUID " + meta)
        guid = match.group(1)
        if guid in guid_to_path:
            raise ValueError("duplicate GUID: " + meta + " vs " + guid_to_path[guid])
        guid_to_path[guid] = meta
    for suffix in ("EditMode", "PlayMode"):
        asmdef = ROOT / ("Assets/B01Construction/BUILD001/Tests/" + suffix +
            "/B01.BUILD001." + suffix + "Tests.asmdef")
        data = json.loads(asmdef.read_text(encoding="utf8"))
        if data.get("optionalUnityReferences") != ["TestAssemblies"]:
            raise ValueError("broken TestAssemblies " + suffix)
        if any("TestRunner" in reference for reference in data["references"]):
            raise ValueError("duplicate TestRunner " + suffix)
    shader = ROOT / "Assets/B01Construction/BUILD001/Resources/BUILD001_PreviewURP.shader"
    if not shader.is_file() or 'RenderPipeline"="UniversalPipeline' not in shader.read_text():
        raise ValueError("missing explicit URP shader")
    return len(guid_to_path)


def manifest_for(files, head_sha):
    return {
        "schema": "B01-PACKAGE-FULL-SHA256-R13",
        "package_id": "B01-BUILD001-R1.3",
        "base_source_sha": BASE_SHA,
        "source_head_sha": head_sha,
        "G2": "HOLD",
        "native_unity": "NOT_RUN_BY_PACKAGER",
        "files": [
            {"path": p, "bytes": len((ROOT / p).read_bytes()),
             "sha256": hashlib.sha256((ROOT / p).read_bytes()).hexdigest()}
            for p in files
        ]
    }


def build_zip(files, manifest):
    destination = io.BytesIO()
    payloads = {p: (ROOT / p).read_bytes() for p in files}
    payloads["PACKAGE_MANIFEST_R13.json"] = (json.dumps(manifest,
        sort_keys=True, indent=2) + "\n").encode("utf8")
    with zipfile.ZipFile(destination, "w") as archive:
        for name in sorted(payloads):
            info = zipfile.ZipInfo(name, (1980, 1, 1, 0, 0, 0))
            info.create_system = 3
            info.compress_type = zipfile.ZIP_STORED
            info.external_attr = (0o100644 << 16)
            archive.writestr(info, payloads[name])
    return destination.getvalue()


def validate_zip(data, manifest):
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        archive.testzip()
        expected = {p["path"]: p for p in manifest["files"]}
        if sorted(archive.namelist()) != sorted([*expected,"PACKAGE_MANIFEST_R13.json"]):
            raise ValueError("ZIP file list mismatch")
        for path, row in expected.items():
            payload = archive.read(path)
            if len(payload) != row["bytes"] or hashlib.sha256(payload).hexdigest() != row["sha256"]:
                raise ValueError("ZIP mismatch: " + path)
        if json.loads(archive.read("PACKAGE_MANIFEST_R13.json")) != manifest:
            raise ValueError("manifest mismatch")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default="dist/B01-BUILD001-R13.zip")
    args = parser.parse_args()
    files = source_paths()
    guid_count = validate_source(files)
    head = subprocess.check_output(["git", "rev-parse", "HEAD"],cwd=ROOT).decode().strip()
    manifest = manifest_for(files, head)
    first = build_zip(files, manifest)
    second = build_zip(files, manifest)
    if first != second: raise ValueError("nondeterministic package")
    validate_zip(first, manifest)
    out = ROOT / args.out
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_bytes(first)
    report = {"status": "PASS_STATIC_AND_BYTES", "source_sha": head,
        "source_files": len(files), "meta_guids": guid_count,
        "zip_entries": len(files)+1, "zip_bytes": len(first),
        "zip_sha256": hashlib.sha256(first).hexdigest(),
        "native_unity": "NOT_RUN"}
    print(json.dumps(report, indent=2, sort_keys=True))


if __name__ == "__main__": main()
