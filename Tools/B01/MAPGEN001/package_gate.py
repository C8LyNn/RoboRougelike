#!/usr/bin/env python3
"""Read-only MAPGEN R3.4.1 SHA256 gate and reproducible full-source package ZIP.
Runs only on a clean source checkout; never imports into Unity or edits gameplay.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import zipfile

MANIFEST="Docs/B01/MAPGEN001/manifest.json"
PARENT_METAS=["Assets/B01Packages.meta","Assets/B01Packages/MAPGEN001.meta"]
ROOTS=["Assets/B01Packages/MAPGEN001/","Docs/B01/MAPGEN001/","Tools/B01/MAPGEN001/"]
OUTPUT="mapgen-r341-evidence"

def sha(data): return hashlib.sha256(data).hexdigest()
def tracked():
    raw=subprocess.check_output(["git","ls-files","-z","--"]+PARENT_METAS+[x.rstrip("/") for x in ROOTS])
    files=[a.decode("utf8") for a in raw.split(b"\0") if a]
    return sorted(p for p in files if p!=MANIFEST
                  and (p in PARENT_METAS or any(p.startswith(root) for root in ROOTS))
                  and "/bin/" not in p and "/obj/" not in p)

def snap():
    result={}
    for path in tracked():
        source=Path(path)
        if not source.is_file() or source.is_symlink():raise RuntimeError("missing or symlink: "+path)
        data=source.read_bytes()
        result[path]={"sha256":sha(data),"bytes":len(data)}
    return result

def expected_lists(full):
    source={p:v["sha256"] for p,v in full.items()
            if p in PARENT_METAS or p.startswith(ROOTS[0]) or p.startswith(ROOTS[1])}
    qa={p:v["sha256"] for p,v in full.items() if p.startswith(ROOTS[2])}
    return source,qa

def verify(manifest,full):
    source,qa=expected_lists(full)
    if manifest.get("revision")!="R3.4.1":raise RuntimeError("wrong revision")
    if manifest.get("source_file_sha256_by_path")!=source:raise RuntimeError("source sha256 mismatch / missing")
    if manifest.get("qa_tool_sha256_by_path")!=qa:raise RuntimeError("qa tool sha256 mismatch / missing")
    bysize=manifest.get("source_file_size_by_path")
    if bysize!={p:v["bytes"] for p,v in full.items()}:raise RuntimeError("file byte-length mismatch")
    for path in full:
        assert len(full[path]["sha256"])==64
    return len(source),len(qa)

def zip_package(full,output):
    output.mkdir(parents=True,exist_ok=True)
    file_path=output/"MAPGEN001_R3_4_1_FULL_SOURCE_AND_QA.zip"
    files=sorted(list(full)+[MANIFEST])
    with zipfile.ZipFile(file_path,"w") as z:
        for path in files:
            data=Path(path).read_bytes()
            info=zipfile.ZipInfo(path,(1980,1,1,0,0,0))
            info.compress_type=zipfile.ZIP_DEFLATED
            info.external_attr=0o644<<16
            z.writestr(info,data,compresslevel=9)
    digest=sha(file_path.read_bytes())
    return {"path":str(file_path),"sha256":digest,
            "bytes":file_path.stat().st_size,"entries":len(files)}

def main():
    args=argparse.ArgumentParser()
    args.add_argument("--emit",action="store_true")
    args.add_argument("--verify-zip",action="store_true")
    parsed=args.parse_args()
    full=snap()
    print("MAPGEN_SHA256_RECEIPT_JSON="+json.dumps(full,sort_keys=True,separators=(",",":")))
    if parsed.verify_zip:
        man=json.loads(Path(MANIFEST).read_text(encoding="utf8"))
        src,qa=verify(man,full)
        outcome=zip_package(full,Path(OUTPUT))
        outcome.update({"verified_source_files":src,"verified_qa_files":qa})
        print("MAPGEN_ZIP_RESULT_JSON="+json.dumps(outcome,sort_keys=True,separators=(",",":")))
        (Path(OUTPUT)/"SHA256_REPORT.json").write_text(json.dumps(outcome,indent=2)+"\n",encoding="utf8")
    else:
        print("MAPGEN_ZIP=NOT_BUILT_MANIFEST_VERIFICATION_PENDING")
if __name__=="__main__":main()
