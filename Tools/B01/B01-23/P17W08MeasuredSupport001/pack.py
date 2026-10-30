#!/usr/bin/env python3
"""Deterministic original Git-object Unity-only ZIP, no worktree installations."""
import hashlib
import io
import json
from pathlib import Path
import subprocess
import sys
import zipfile

FILES=[
    "Assets/B0123MeasuredSupport001.meta",
    "Assets/B0123MeasuredSupport001/MeasuredSupportCapture.cs",
    "Assets/B0123MeasuredSupport001/MeasuredSupportCapture.cs.meta"
]
FROZEN_PR_417="f1c82267b1ad3d03d67ce17b478a180a69e7d7d7"
FROZEN_BLOB_417="2b53bf0a9aa7c32aee27fb4772b65edabf7324c6"
def git(*args):
    return subprocess.check_output(["git",*args])
def item(path,data):
    return {"path":path,"bytes":len(data),
            "sha256":hashlib.sha256(data).hexdigest(),
            "git_blob_sha1":hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()}
def record(files,sha):
    return {"package_id":"B01-23-P17-MEASURED-SUPPORT-001","version":"R1",
            "producer_source_sha":sha,"original_immutable_massbalance_sha":FROZEN_PR_417,
            "original_massbalance_git_blob":FROZEN_BLOB_417,
            "native_unity":"NOT_RUN","codex_ack":False,
            "source_only_managed_stub":"YES",
            "files":[item(p,files[p]) for p in sorted(files)]}
def zi(name):
    x=zipfile.ZipInfo(name,(1980,1,1,0,0,0))
    x.create_system=3
    x.external_attr=0o100644<<16
    x.compress_type=zipfile.ZIP_STORED
    return x
def build():
    sha=git("rev-parse","HEAD").decode().strip()
    files={p:git("show",sha+":"+p) for p in FILES}
    receipt=record(files,sha)
    buf=io.BytesIO()
    with zipfile.ZipFile(buf,"w") as z:
        for p in sorted(files):z.writestr(zi(p),files[p])
        z.writestr(zi("PACKAGE_MANIFEST.json"),(json.dumps(receipt,sort_keys=True,indent=2)+"\n").encode())
    return buf.getvalue(),receipt
def verify(raw):
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        names=z.namelist()
        assert len(names)==4 and len(set(names))==4 and set(names)==set(FILES)|{"PACKAGE_MANIFEST.json"}
        assert all(not p.startswith("/") and ".." not in p.split("/") for p in names)
        files={p:z.read(p) for p in FILES}
        manifest=json.loads(z.read("PACKAGE_MANIFEST.json"))
        assert manifest==record(files,manifest["producer_source_sha"])
        for row in manifest["files"]:
            assert git("rev-parse",manifest["producer_source_sha"]+":"+row["path"]).decode().strip()==row["git_blob_sha1"]
        assert manifest["original_immutable_massbalance_sha"]==FROZEN_PR_417
        assert manifest["original_massbalance_git_blob"]==FROZEN_BLOB_417
        return manifest
if __name__=="__main__":
    raw,receipt=build()
    zip_path=Path(sys.argv[1]);rece_path=Path(sys.argv[2])
    if zip_path.exists() or rece_path.exists():raise RuntimeError("REFUSE_OVERWRITE")
    zip_path.write_bytes(raw);verify(raw)
    rece_path.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
    print("ORIGINAL_SOURCE_UNITY_ZIP_BYTES="+str(len(raw))+
          " SHA256="+hashlib.sha256(raw).hexdigest()+
          " SOURCE_SHA="+receipt["producer_source_sha"])
