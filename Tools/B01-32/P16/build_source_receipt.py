#!/usr/bin/env python3
"""Physical original source manifest for independent B01-40/Codex intake.
Never exports native Unity test results or claims G2.
"""
import hashlib
import json
import subprocess
import sys
from pathlib import Path

root=Path(__file__).resolve().parents[3]
pack=root/"Assets/B0132P16VfxDemo"
out=Path(sys.argv[1]) if len(sys.argv)>1 else root/"build/b01-32-p16"
out.mkdir(parents=True,exist_ok=True)
entries=[]
for path in sorted(pack.rglob("*")):
    if not path.is_file():continue
    raw=path.read_bytes()
    entries.append({"relative_path":path.relative_to(root).as_posix(),
                    "bytes":len(raw),"sha256":hashlib.sha256(raw).hexdigest()})
rootmeta=pack.with_suffix(".meta")
assert rootmeta.is_file()
raw=rootmeta.read_bytes()
entries.insert(0,{"relative_path":rootmeta.relative_to(root).as_posix(),
                  "bytes":len(raw),"sha256":hashlib.sha256(raw).hexdigest()})
assert len({x["relative_path"].lower() for x in entries}) == len(entries)
assert all(x["bytes"]>0 for x in entries)
sha=subprocess.check_output(["git","rev-parse","HEAD"],cwd=root,text=True).strip()
record={"package_id":"B0132-VFX-P16-R1","source_commit":sha,"native_unity":"NOT_RUN",
        "files":entries,"file_count":len(entries),"total_bytes":sum(x["bytes"] for x in entries)}
(out/"SOURCE_SHA256.json").write_text(json.dumps(record,indent=2)+"\n",encoding="utf8")
(out/"SOURCE_SHA256.txt").write_text("".join(f'{x["sha256"]}  {x["relative_path"]}\n' for x in entries),encoding="utf8")
print(f"PHYSICAL_SOURCE_HASH_PASS files={len(entries)} totalBytes={record['total_bytes']} inputCommit={sha}")
print("INDEPENDENT_RECEIVER_REQUIRED: all source hashes included, Unity native still NOT_RUN")
