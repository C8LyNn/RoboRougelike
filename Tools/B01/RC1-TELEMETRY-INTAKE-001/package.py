#!/usr/bin/env python3
"""Deterministic, exact-scope SOURCE/RECEIPT archive; never a Unity build."""
from __future__ import annotations
import hashlib
import json
import pathlib
import stat
import sys
import zipfile

ROOT = pathlib.Path(__file__).resolve().parent
FILES = (
    "EVIDENCE.json", "INSTALL_FOR_CODEX.md", "README.md", "ingest.mjs",
    "manifest.json", "package.py", "samples/drive.csv",
    "samples/rc1-native-result.json", "test.mjs",
)
DATE = (1980, 1, 1, 0, 0, 0)

def write_bytes(zf, name: str, raw: bytes) -> None:
    info = zipfile.ZipInfo(name, DATE)
    info.create_system = 3
    info.external_attr = (stat.S_IFREG | 0o644) << 16
    info.compress_type = zipfile.ZIP_STORED
    zf.writestr(info, raw)

def build(out: pathlib.Path) -> dict:
    manifest = {}
    out.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(out, "w", allowZip64=False) as archive:
        for name in FILES:
            path = ROOT / name
            if path.is_symlink() or not path.is_file():
                raise RuntimeError("SOURCE_MISSING_OR_SYMLINK:" + name)
            raw = path.read_bytes()
            write_bytes(archive, name, raw)
            manifest[name] = {"bytes": len(raw), "sha256": hashlib.sha256(raw).hexdigest()}
        write_bytes(archive, "FILE_SHA256_MANIFEST.json",
                    (json.dumps(manifest, sort_keys=True, indent=2) + "\n").encode())
    # Independently verify what was written, not the in-memory input.
    with zipfile.ZipFile(out) as archive:
        if archive.namelist() != [*FILES, "FILE_SHA256_MANIFEST.json"]:
            raise RuntimeError("PACKAGE_CONTENT_MISMATCH")
        for name in FILES:
            got = archive.read(name)
            if len(got) != manifest[name]["bytes"] or hashlib.sha256(got).hexdigest() != manifest[name]["sha256"]:
                raise RuntimeError("PACKAGE_HASH_FAIL:" + name)
    return {"name": out.name, "bytes": out.stat().st_size, "sha256": hashlib.sha256(out.read_bytes()).hexdigest(), "entries": len(FILES)+1, "sourceFiles": len(FILES), "verifiedPerFileSha256": len(FILES)}

def main():
    dest = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "build"
    first = dest / "B01_RC1_TELEMETRY_INTAKE_R1.zip"
    temp = dest / ".repeat-compare.zip"
    a, b = build(first), build(temp)
    if a["sha256"] != b["sha256"] or a["bytes"] != b["bytes"]:
        raise RuntimeError("ZIP_NOT_DETERMINISTIC")
    temp.unlink()
    (dest / "PACKAGE_SHA256_REPORT.json").write_text(json.dumps({**a, "repeatByteIdentical": True, "Unity": "NOT_RUN", "HumanPlaytest": "NOT_RUN", "G2": "HOLD"}, indent=2) + "\n")
    print(json.dumps({**a, "repeatByteIdentical": True}, indent=2))

if __name__ == "__main__":
    main()
