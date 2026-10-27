#!/usr/bin/env python3
"""Full deterministic isolated CHL-003 ZIP, exact per-file SHA256/bytes.
Manifest self-file excluded from hash map; physically included in ZIP.
"""
import argparse,hashlib,json,subprocess,zipfile
from pathlib import Path
ROOTS=["Assets/RoboRougelikeChallenges/","Docs/B01/CHL003/","Tools/B01/CHL003/"]
SINGLE=["Assets/RoboRougelikeChallenges.meta",".github/workflows/b01-chl003-r1-source.yml"]
MANIFEST="Docs/B01/CHL003/manifest.json"
OUT=Path("chl003-r1-evidence")
OUT_ZIP=OUT/"CHL003_R1_FULL_SOURCE_AND_QA.zip"
def sha(b):return hashlib.sha256(b).hexdigest()
def tracked():
    raw=subprocess.check_output(["git","ls-files","-z","--",*SINGLE,*[x.rstrip("/") for x in ROOTS]])
    return sorted(x.decode() for x in raw.split(b"\0") if x and x.decode()!=MANIFEST)
def receipt():
    data={}
    for p in tracked():
        f=Path(p)
        if not f.is_file() or f.is_symlink():raise RuntimeError("missing/symlink: "+p)
        b=f.read_bytes()
        data[p]={"sha256":sha(b),"bytes":len(b)}
    return data
def write_zip(v):
    OUT.mkdir(exist_ok=True)
    with zipfile.ZipFile(OUT_ZIP,"w") as z:
        for p in sorted([*v,MANIFEST]):
            obj=zipfile.ZipInfo(p,(1980,1,1,0,0,0))
            obj.compress_type=zipfile.ZIP_DEFLATED
            obj.external_attr=(0o644<<16)
            z.writestr(obj,Path(p).read_bytes(),compresslevel=9)
    with zipfile.ZipFile(OUT_ZIP) as z:
        if len(z.namelist())!=len(v)+1 or z.testzip() is not None:
            raise RuntimeError("bad ZIP physical archive")
        for p,info in v.items():
            if sha(z.read(p))!=info["sha256"]:
                raise RuntimeError("ZIP content mismatch: "+p)
    out={"entries":len(v)+1,"sources":len(v),
        "bytes":OUT_ZIP.stat().st_size,"sha256":sha(OUT_ZIP.read_bytes()),
        "zip_file":str(OUT_ZIP)}
    (OUT/"SHA256_REPORT.json").write_text(json.dumps(out,sort_keys=True,indent=2)+"\n")
    return out
def main():
    a=argparse.ArgumentParser()
    a.add_argument("--verify-zip",action="store_true")
    args=a.parse_args()
    v=receipt()
    print("CHL003_SHA256_RECEIPT_JSON="+json.dumps(v,sort_keys=True,separators=(",",":")))
    if not args.verify_zip:
        print("CHL003_SOURCE_RECEIPT_ONLY_PENDING_MANIFEST")
        return
    m=json.loads(Path(MANIFEST).read_text())
    if m.get("package_id")!="CHL-003" or m.get("revision")!="R1":
        raise RuntimeError("wrong package revision")
    if m.get("source_sha256_by_path")!={p:a["sha256"] for p,a in v.items()}:
        raise RuntimeError("SHA256 manifest stale")
    if m.get("source_bytes_by_path")!={p:a["bytes"] for p,a in v.items()}:
        raise RuntimeError("source bytes stale")
    for p in v:
        if p.startswith("Assets/RoboRougelikeChallenges/") and p.endswith((".cs",".asmdef",".unity")):
            if p+".meta" not in v:raise RuntimeError("missing importer meta "+p)
    print("CHL003_FULL_ZIP_JSON="+json.dumps(write_zip(v),sort_keys=True))
if __name__=="__main__":main()
