#!/usr/bin/env python3
"""Create inspectable, byte-verified SOURCE ZIP from actual authored Unity assets.
Produces no player build, Unity test or integration claim.
"""
import hashlib
import json
import subprocess
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
P = ROOT / "Assets/B0132P17WheelWake"
OUTPUT = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else ROOT/"build/B01-32-P17"
OUTPUT.mkdir(parents=True, exist_ok=True)
paths = [P.with_suffix(".meta")] + sorted(p for p in P.rglob("*") if p.is_file())
assert len(paths)==26, f"expected 26 actual package sources including root .meta: {len(paths)}"
relative = [(p.relative_to(ROOT).as_posix(), p.read_bytes()) for p in paths]
assert all(content for _,content in relative)
assert len(set(name.lower() for name,_ in relative))==len(relative)
checksum=lambda data: hashlib.sha256(data).hexdigest()
src_sha=subprocess.check_output(["git","rev-parse","HEAD"],cwd=ROOT,text=True).strip()
receipt={"package_id":"B01-32-WHEELWAKE-001","source_sha":src_sha,
         "package_source":"isolated Unity prefab+scene/4 CSharp/2 asmdefs, no canonical edits",
         "unity_native":"NOT_RUN",
         "file_count":len(relative),
         "total_bytes":sum(len(content) for _,content in relative),
         "files":[{"path":name,"bytes":len(data),"sha256":checksum(data)} for name,data in relative]}
zip_path=OUTPUT/"B01-32-WHEELWAKE-001-R1-original-unity-assets.zip"
with zipfile.ZipFile(zip_path,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as archive:
    for name,data in relative:
        info=zipfile.ZipInfo(name,date_time=(2020,1,1,0,0,0))
        info.compress_type=zipfile.ZIP_DEFLATED
        info.external_attr=0o100644<<16
        archive.writestr(info,data,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
with zipfile.ZipFile(zip_path,"r") as archive:
    assert archive.testzip() is None
    assert sorted(archive.namelist()) == sorted(name for name,_ in relative)
    for name,data in relative:
        assert archive.read(name) == data, "ZIP source byte mismatch "+name
receipt["archive_sha256"]=checksum(zip_path.read_bytes())
receipt["archive_bytes"]=zip_path.stat().st_size
(OUTPUT/"SOURCE_RECEIPT.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf8")
(OUTPUT/"SOURCE_SHA256.txt").write_text("".join(checksum(data)+"  "+name+"\n" for name,data in relative),encoding="utf8")
print("PHYSICAL_ZIP_VERIFY_PASS",len(relative),"/",len(relative),"original Unity asset files")
print("PHYSICAL_ZIP_SHA256",receipt["archive_sha256"],"SOURCE_COMMIT",src_sha)
print("NATIVE_UNITY_NOT_RUN, G2_HOLD and independent receiver still required")
