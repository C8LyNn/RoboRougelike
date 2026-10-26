#!/usr/bin/env python3
"""BUILD-001 R1.4 complete deterministic source ZIP (not a PR-only delta)."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import package_r13 as shared

ROOT = Path(__file__).resolve().parents[3]
PARENT = "be54b4a33360cbd1923ae5dacbc92dff8eaf586b"


def build():
    files = shared.source_paths()
    guid_count = shared.validate_source(files)
    head = subprocess.check_output(["git","rev-parse","HEAD"],cwd=ROOT).decode().strip()
    manifest = shared.manifest_for(files, head)
    manifest.update({
        "schema": "B01-PACKAGE-FULL-SHA256-R14",
        "package_id": "B01-BUILD001-R1.4",
        "base_source_sha": PARENT,
        "supersedes_zip": "B01-BUILD001-R1.3 source archive, not native certified",
        "receiver_native_red": "PR258 nested unequal contact actual area",
        "native_unity": "NOT_RUN_BY_PACKAGER",
        "G2": "HOLD"
    })
    first=shared.build_zip(files,manifest)
    second=shared.build_zip(files,manifest)
    if first != second: raise ValueError("non-repeatable R1.4 ZIP")
    shared.validate_zip(first,manifest)
    report={
        "package_id": "B01-BUILD001-R1.4",
        "source_base": PARENT,
        "source_sha": head,
        "source_files": len(files),
        "zip_entries": len(files)+1,
        "unique_unity_guids": guid_count,
        "zip_bytes": len(first),
        "zip_sha256": hashlib.sha256(first).hexdigest(),
        "status": "PACKAGE_STATIC_PASS_NATIVE_NOT_RUN",
        "G2": "HOLD"
    }
    return first,report


def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--out",default="dist/B01-BUILD001-R14.zip")
    args=ap.parse_args()
    data,receipt=build()
    path=ROOT/args.out
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_bytes(data)
    print(json.dumps(receipt,indent=2,sort_keys=True))


if __name__=="__main__": main()
