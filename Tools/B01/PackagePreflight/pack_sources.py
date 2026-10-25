#!/usr/bin/env python3
"""W9 full package export; actual Git blobs, deterministic ZIP, read-only."""
import hashlib, io, json, zipfile
from pathlib import Path
from w9_preflight import CASES, audit, blob
out=Path("w9-preflight-evidence")
out.mkdir(exist_ok=True)
summary=[]
for case in CASES:
    name,_,fixed,_,manifest_path,key,_=case
    result=audit(case,fixed)
    if result["findings"] or result["integrity"]:
        raise SystemExit("FAIL_CANNOT_PACKAGE_"+name+": "+str(result))
    manifest=json.loads(blob(fixed,manifest_path))
    paths=sorted(set(manifest[key])|{manifest_path})
    files={p:blob(fixed,p) for p in paths}
    metas={}
    for p,data in files.items():
        if not p.endswith(".meta"):continue
        import re
        m=re.search(rb"(?m)^guid:\s*([0-9a-f]{32})\s*$",data)
        if not m:raise RuntimeError(p+" missing GUID")
        guid=m.group(1).decode()
        if guid in metas:raise RuntimeError("Duplicate GUID: "+p+" and "+metas[guid])
        metas[guid]=p
    def archive():
        target=io.BytesIO()
        with zipfile.ZipFile(target,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
            for p,data in files.items():
                info=zipfile.ZipInfo(p,(1980,1,1,0,0,0))
                info.compress_type=zipfile.ZIP_DEFLATED
                info.external_attr=0o644<<16
                z.writestr(info,data,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
        return target.getvalue()
    payload=archive()
    if payload!=archive():raise RuntimeError("NONDETERMINISTIC_ZIP_"+name)
    zpath=out/(name+"_W9_FIXED_FULL_SOURCE.zip")
    zpath.write_bytes(payload)
    receipt={"package":name,"commit":fixed,"original_compilation":"UNITY_NATIVE_FAIL_EXIT1_CODEX_PR212",
             "fixed_static":"PASS_ACTUAL_SOURCE","fixed_native_compile":"NOT_RUN",
             "fixed_editmode":"NOT_RUN","fixed_playmode":"NOT_RUN","B01_60":"PENDING","G2":"HOLD",
             "inner_zip_sha256":hashlib.sha256(payload).hexdigest(),"inner_zip_bytes":len(payload),
             "entries":len(files),"unique_meta_guid_count":len(metas),
             "actual_files":{p:{"sha256":hashlib.sha256(data).hexdigest(),"bytes":len(data)} for p,data in files.items()}}
    (out/(name+"_SHA256_RECEIPT.json")).write_text(json.dumps(receipt,indent=2)+"\n")
    print(f"W9_PACKAGE_{name}_PASS SHA256={receipt['inner_zip_sha256']} BYTES={len(payload)} ENTRIES={len(files)} GUIDS={len(metas)}")
    summary.append({k:v for k,v in receipt.items() if k!="actual_files"})
(out/"W9_THREE_PACKAGES.json").write_text(json.dumps(summary,indent=2)+"\n")
print("W9_THREE_ARCHIVES=CREATED_NATIVE_UNITY_NOT_RUN_G2_HOLD")
