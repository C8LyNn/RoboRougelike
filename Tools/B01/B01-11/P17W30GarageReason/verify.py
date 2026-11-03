#!/usr/bin/env python3
"""Exact Git/byte/Unity GUID/scope attestation; no repository writes."""
from pathlib import Path
import hashlib, json, re, subprocess

ROOT=Path(__file__).resolve().parents[4]
D="Tools/B01/B01-11/P17W30GarageReason"
parent="56c874aeac19d7724d7cddeaabc7a6a4f82c08d4"
target="Assets/R3/R3GarageView.cs"
m=json.loads((ROOT/D/"manifest.json").read_text(encoding="utf-8"))
assert m["package"]=="B01-11-W30-GARAGE-REJECTION-001/R1"
assert m["parent_commit"]==parent
expected={
    target,
    "Assets/Tests/Editor/B0111W30GarageRejectionUxTests.cs",
    "Assets/Tests/Editor/B0111W30GarageRejectionUxTests.cs.meta",
    D+"/probe.py",
    D+"/verify.py",
    D+"/README.md",
    ".github/workflows/b01-11-p17w30-garage-reason.yml",
}
recorded={x["path"] for x in m["files"]}
assert len(m["files"])==7 and recorded==expected,(recorded,expected)
for x in m["files"]:
    name=x["path"]
    assert not name.startswith("/") and ".." not in Path(name).parts
    p=ROOT/name
    assert p.is_file() and not p.is_symlink(),name
    data=p.read_bytes()
    assert x["bytes"]==len(data),name+" SIZE"
    assert x["sha256"]==hashlib.sha256(data).hexdigest(),name+" SHA256"
baseline=subprocess.check_output(["git","show",parent+":"+target],cwd=ROOT)
blob=hashlib.sha1(b"blob "+str(len(baseline)).encode()+b"\0"+baseline).hexdigest()
assert blob=="1976a19516905be63259109ec1af0a92ef52d6f4",blob
diff=subprocess.check_output(["git","diff","--name-status",parent,"HEAD"],cwd=ROOT,text=True)
status={}
for ln in diff.splitlines():
    mode,path=ln.split("\t",1)
    status[path]=mode
assert set(status)==expected|{D+"/manifest.json"},status
assert status[target]=="M"
assert all(status[k]=="A" for k in status if k!=target),status
meta_path=ROOT/"Assets/Tests/Editor/B0111W30GarageRejectionUxTests.cs.meta"
match=re.search(r"^guid: ([0-9a-f]{32})$",meta_path.read_text(),re.M)
assert match and match.group(1)=="f29e7bf4a95b46ee82a03297f02e117a"
all_guid=[]
for p in (ROOT/"Assets").rglob("*.meta"):
    for guid in re.findall(rb"(?m)^guid: ([0-9a-f]{32})\s*$",p.read_bytes()):
        all_guid.append(guid.decode())
assert all_guid.count(match.group(1))==1,"new Unity GUID collision"
print("ORIGINAL_GIT_SOURCE_SCOPE_PASS 1 MODIFIED 7 ADD ONLY")
print("SHA256_MANIFEST_PASS 7 EXACT_BYTES and baseline original Git blob")
print("UNITY_META_NEW_GUID_UNIQUE_PASS 1 of "+str(len(all_guid))+" all original assets")
print("NATIVE_UNITY_G1 NOT_RUN (requires editor license + NUnit XML)")
