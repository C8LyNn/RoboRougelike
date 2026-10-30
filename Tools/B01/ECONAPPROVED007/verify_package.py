#!/usr/bin/env python3
"""W25 original Git manifest and frozen P17 ancestors, no false G0 pass."""
from pathlib import Path
import json, re, subprocess
ROOT=Path(__file__).resolve().parents[3]
m=json.loads(Path(__file__).with_name("manifest.json").read_text())
assert m["parent_sha"]=="cc5d65f66ad7a764af6515c566e489b9fc3be82e"
assert m["native_unity"]=="NOT_RUN" and m["source_g0"]=="INDEPENDENT_PENDING"
def git(*cmd):
    return subprocess.check_output(["git",*cmd],cwd=ROOT,text=True).strip()
for path,sha in m["pinned_blobs"].items():
    assert git("rev-parse","HEAD:"+path)==sha,("BLOB_MISMATCH",path)
for name,p in m["frozen_files"].items():
    assert git("rev-parse","HEAD:"+p)==m["frozen_sha"][name],("FROZEN_CHANGED",name)
base=m["parent_sha"]
if subprocess.run(["git","cat-file","-e",base+"^{commit}"],cwd=ROOT,
   stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode:
    subprocess.run(["git","fetch","--no-tags","--depth=1","origin",base],
                   cwd=ROOT,check=True,stdout=subprocess.DEVNULL)
changed=set(git("diff","--name-only",base,"HEAD").splitlines())
assert len(changed)==16 and changed==set(m["owned_paths"]),("PATHS",sorted(changed))
assert len(m["pinned_blobs"])==15
for path in changed:
    assert path.startswith(("Assets/B01Packages/ECONAPPROVED007",
                            "Tools/B01/ECONAPPROVED007/")) or path==".github/workflows/b01-22-p17w25-econ-section-v7.yml",path
def guids(path):
    return re.findall(r"^guid:\s*([0-9a-f]{32})$",path.read_text(),re.M)
new_meta=list((ROOT/"Assets/B01Packages/ECONAPPROVED007").rglob("*.meta"))
new_meta.append(ROOT/"Assets/B01Packages/ECONAPPROVED007.meta")
assert len(new_meta)==5
new_ids=[v for p in new_meta for v in guids(p)]
assert len(new_ids)==5 and len(set(new_ids))==5
existing=list((ROOT/"Assets/B01Packages").rglob("*.meta"))
legacy={v for p in existing if p not in new_meta for v in guids(p)}
assert not(set(new_ids)&legacy)
asm=json.loads((ROOT/"Assets/B01Packages/ECONAPPROVED007/Runtime/B01.EconApprovedV7.asmdef").read_text())
assert asm["name"]=="B01.EconApprovedV7" and asm["references"]==[]
assert asm["autoReferenced"] is False and asm["noEngineReferences"] is True
policy=(ROOT/"Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs").read_text()
section=(ROOT/"Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs").read_text()
assert "scrap=(int)((long)purchase*65L/100L)" in policy
assert "return scrap>=0&&scrap<purchase;" in policy
assert "FileStream" not in policy+section and "UnityEngine" not in policy+section
assert "SHA256.Create()" in section and "TryDecode" in section
print("W25_MANIFEST_PASS 16 owned ADD-only 15 pinned Git blobs 5 new globally unique Unity GUIDs; V3-V6 frozen; SOURCE_G0_PENDING")
