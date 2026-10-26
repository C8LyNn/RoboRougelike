#!/usr/bin/env python3
"""Deterministic complete CAMERA R1.2.2 ZIP and exact per-file SHA256/bytes gate.
This does not compile Unity or import assets into the host game.
"""
import hashlib,json,subprocess,zipfile,argparse
from pathlib import Path
MANIFEST="Docs/B01/CAMERA001/manifest.json"
PARENTS=["Assets/B01Packages.meta","Assets/B01Packages/CAMERA001.meta"]
ROOTS=["Assets/B01Packages/CAMERA001/","Docs/B01/CAMERA001/","Tools/B01/CAMERA001/"]
OUT=Path("camera-r122-evidence")
ZIP="CAMERA001_R1_2_2_FULL_SOURCE_QA.zip"
def h(b):return hashlib.sha256(b).hexdigest()
def files():
    data=subprocess.check_output(["git","ls-files","-z","--",*PARENTS,
        *[x.rstrip("/") for x in ROOTS]])
    return sorted(s.decode() for s in data.split(b"\0") if s and
        s.decode()!=MANIFEST and
        (s.decode() in PARENTS or any(s.decode().startswith(x) for x in ROOTS)))
def scan():
    v={}
    for p in files():
        f=Path(p)
        if not f.is_file() or f.is_symlink():raise RuntimeError("missing/symlink "+p)
        b=f.read_bytes()
        v[p]={"sha256":h(b),"bytes":len(b)}
    return v
def zip_bytes(receipt):
    OUT.mkdir(parents=True,exist_ok=True)
    dest=OUT/ZIP
    with zipfile.ZipFile(dest,"w") as z:
        for p in sorted([*receipt,MANIFEST]):
            info=zipfile.ZipInfo(p,(1980,1,1,0,0,0))
            info.compress_type=zipfile.ZIP_DEFLATED
            info.external_attr=0o644<<16
            z.writestr(info,Path(p).read_bytes(),compresslevel=9)
    return {"sha256":h(dest.read_bytes()),"bytes":dest.stat().st_size,
            "entries":len(receipt)+1,"path":str(dest)}
def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--verify-zip",action="store_true")
    parser.add_argument("--emit",action="store_true")
    a=parser.parse_args()
    receipt=scan()
    print("CAMERA_R122_SHA256_RECEIPT_JSON="+json.dumps(receipt,separators=(",",":"),sort_keys=True))
    if a.verify_zip:
        m=json.loads(Path(MANIFEST).read_text())
        if m.get("revision")!="R1.2.2":raise RuntimeError("REVISION_MISMATCH")
        if m.get("r122_source_sha256_by_path")!={p:v["sha256"] for p,v in receipt.items()}:raise RuntimeError("SHA256_MISMATCH")
        if m.get("r122_source_bytes_by_path")!={p:v["bytes"] for p,v in receipt.items()}:raise RuntimeError("BYTES_MISMATCH")
        for path in receipt:
            if path.startswith("Assets/B01Packages/CAMERA001/") and path.endswith((".cs",".asmdef",".unity")):
                if not Path(path+".meta").is_file():raise RuntimeError("MISSING_META "+path)
        result=zip_bytes(receipt)
        result["source_files"]=len(receipt)
        print("CAMERA_R122_ZIP_RESULT_JSON="+json.dumps(result,sort_keys=True))
        (OUT/"SHA256_REPORT.json").write_text(json.dumps(result,indent=2)+"\n")
    else:
        print("CAMERA_R122_ZIP=NOT_BUILT_MANIFEST_PENDING")
if __name__=="__main__":main()
