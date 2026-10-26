#!/usr/bin/env python3
"""B01-10 W14 deterministic source-only release package, NEVER Unity native."""
import hashlib
import json
import os
from pathlib import Path
import sys
import zipfile

ROOT = Path(__file__).resolve().parents[3]
PREFIX = "Tools/B01/RC2-GARAGE-WARNINGS-001"
ITEMS = [
    ".github/workflows/b01-w14-rc2-garage-warnings.yml",
    f"{PREFIX}/Runtime/GarageWarningPolicy.cs",
    f"{PREFIX}/Tests/Program.cs",
    f"{PREFIX}/Tests/WarningTests.csproj",
    f"{PREFIX}/fixtures.json",
    f"{PREFIX}/contract.json",
    f"{PREFIX}/README.md",
    f"{PREFIX}/INTEGRATION_FOR_CODEX.md",
    f"{PREFIX}/EVIDENCE.json",
    f"{PREFIX}/package.py",
]
def sha(payload):
    return hashlib.sha256(payload).hexdigest()

def build(path):
    manifest = {"package_id": "B01-W14-RC2-GARAGE-WARNINGS-R1",
                "type": "read_only_csharp_ui_policy_source_not_native_unity",
                "G2": "HOLD", "files": []}
    entries=[]
    for rel in ITEMS:
        p=ROOT / rel
        if not p.is_file():
            raise RuntimeError("MISSING_REQUIRED_SOURCE:" + rel)
        if any(k in rel.split("/") for k in ("obj","bin","__pycache__")):
            raise RuntimeError("BUILD_OUTPUT_FORBIDDEN")
        payload=p.read_bytes()
        entries.append((rel,payload))
        manifest["files"].append({"path":rel,"bytes":len(payload),"sha256":sha(payload)})
    manifest_bytes=(json.dumps(manifest,sort_keys=True,indent=2)+"\n").encode()
    entries.append(("B01-W14-GARAGE-WARNINGS-RECEIPT.json",manifest_bytes))
    entries.sort(key=lambda x:x[0])
    path.parent.mkdir(parents=True,exist_ok=True)
    with zipfile.ZipFile(path,"w",compression=zipfile.ZIP_STORED) as z:
        for name,data in entries:
            zi=zipfile.ZipInfo(name,(1980,1,1,0,0,0))
            zi.compress_type=zipfile.ZIP_STORED
            zi.create_system=3
            zi.external_attr=0o100644 <<16
            z.writestr(zi,data)
    with zipfile.ZipFile(path) as z:
        if z.namelist()!=[n for n,_ in entries]:
            raise RuntimeError("UNSORTED_ARCHIVE")
        actual=json.loads(z.read("B01-W14-GARAGE-WARNINGS-RECEIPT.json"))
        if len(actual["files"])!=len(ITEMS):
            raise RuntimeError("BAD_ITEM_COUNT")
        for row in actual["files"]:
            raw=z.read(row["path"])
            if sha(raw)!=row["sha256"] or len(raw)!=row["bytes"]:
                raise RuntimeError("CORRUPT_FILE:"+row["path"])
    return {"fileCount":len(ITEMS),"archiveEntries":len(entries),
            "zipBytes":path.stat().st_size,"zipSha256":sha(path.read_bytes()),
            "nativeUnity":"NOT_RUN","G2":"HOLD"}

if __name__=="__main__":
    if len(sys.argv)<2:
        raise SystemExit("USAGE: python package.py <output.zip> [receipt.json]")
    output=Path(sys.argv[1]); receipt=build(output)
    if len(sys.argv)>=3:
        p=Path(sys.argv[2]);p.parent.mkdir(parents=True,exist_ok=True)
        p.write_text(json.dumps(receipt,indent=2)+"\n")
    print(json.dumps(receipt,sort_keys=True))
