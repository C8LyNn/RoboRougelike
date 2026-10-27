#!/usr/bin/env python3
"""Read-only original-source fetch; no host, ECON or CHL source modifications."""
import hashlib
import json
import pathlib
import time
import urllib.request

HERE = pathlib.Path(__file__).resolve().parent
DEST = HERE / "_pinned"
PIN = "88b8eed60345c49481648833e1fde6dc1a400258"
SOURCES = {
    "Assets/R1/R1RunController.cs": "c6d5f3efe4949da68a5c48ec38ef5028af8b8410",
    "Assets/R3/R3VehicleModel.cs": "cdd75fff4923a382b16c430dac1452320eda88f6",
}
def blob_id(raw):
    return hashlib.sha1(b"blob " + str(len(raw)).encode("ascii") + b"\0" + raw).hexdigest()

def main():
    receipts=[]
    for path, expected in SOURCES.items():
        url="https://raw.githubusercontent.com/C8LyNn/RoboRougelike/"+PIN+"/"+path
        err=None
        for trial in range(3):
            try:
                with urllib.request.urlopen(url,timeout=25) as fp:
                    raw=fp.read()
                err=None
                break
            except Exception as ex:
                err=ex
                time.sleep(1)
        if err:
            raise RuntimeError("ACTUAL_FROZEN_HOST_SOURCE_FETCH_FAILED "+path) from err
        actual=blob_id(raw)
        if actual!=expected:
            raise RuntimeError("ACTUAL_HOST_SOURCE_BLOB_MISMATCH "+path+" "+actual)
        file=DEST/path
        file.parent.mkdir(parents=True,exist_ok=True)
        file.write_bytes(raw)
        receipts.append({"path":path,"git_blob":actual,
          "sha256":hashlib.sha256(raw).hexdigest(),"bytes":len(raw)})
    (DEST/"receipt.json").write_text(json.dumps({"commit":PIN,"actual_pinned_host_files":receipts,
       "native_unity":"NOT_RUN"},indent=2)+"\n",encoding="utf-8")
    print("ACTUAL_PINNED_CODEX_CSHARP_VERIFIED",len(receipts),"ORIGINAL_GIT_BLOBS")

if __name__=="__main__":
    main()
