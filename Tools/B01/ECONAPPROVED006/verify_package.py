#!/usr/bin/env python3
"""B01-22 W24 exact original physical Git source, no frozen code edits."""
from pathlib import Path
import subprocess, json, re
ROOT=Path(__file__).resolve().parents[3]
m=json.loads((Path(__file__).with_name("manifest.json")).read_text())
assert m["parent"]=="1b964643dc191e5daa47b03c6e6fb295c0427493"
assert m["native_g1"]=="NOT_RUN" and m["released"] is False
def git(*a):
    return subprocess.check_output(["git",*a],cwd=ROOT,text=True).strip()
for path,sha in m["blobs"].items():
    assert git("rev-parse","HEAD:"+path)==sha,(path,sha)
for tag,p in m["frozen"].items():
    assert git("rev-parse","HEAD:"+p)==m["frozen_sha"][tag],tag
base=m["parent"]
if subprocess.run(["git","cat-file","-e",base+"^{commit}"],cwd=ROOT,
        stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode:
    subprocess.run(["git","fetch","--no-tags","--depth=1","origin",base],
        cwd=ROOT,check=True,stdout=subprocess.DEVNULL)
changed=set(git("diff","--name-only",base,"HEAD").splitlines())
assert changed==set(m["owned_paths"]),(sorted(changed),sorted(m["owned_paths"]))
assert len(changed)==13 and len(m["blobs"])==12
assert all(p.startswith(("Assets/B01Packages/ECONAPPROVED006",
                           "Tools/B01/ECONAPPROVED006/")) or
           p==".github/workflows/b01-22-p17w24-approved-economy.yml"
           for p in changed)
def guid(p):
    return re.findall(r"^guid:\s*([0-9a-f]{32})$",p.read_text(),re.M)
new_meta=list((ROOT/"Assets/B01Packages/ECONAPPROVED006").rglob("*.meta"))
new_meta.append(ROOT/"Assets/B01Packages/ECONAPPROVED006.meta")
new_ids=[g for p in new_meta for g in guid(p)]
assert len(new_meta)==4 and len(new_ids)==4 and len(set(new_ids))==4
all_meta=list((ROOT/"Assets/B01Packages").rglob("*.meta"))
old_ids={g for p in all_meta if p not in new_meta for g in guid(p)}
assert not old_ids.intersection(new_ids)
asm=json.loads((ROOT/"Assets/B01Packages/ECONAPPROVED006/Runtime/B01.EconApprovedV6.asmdef").read_text())
assert asm["name"]=="B01.EconApprovedV6" and asm["noEngineReferences"]
assert asm["autoReferenced"] is False and asm["references"]==[]
source=(ROOT/"Assets/B01Packages/ECONAPPROVED006/Runtime/ApprovedEconomyPolicyV6.cs").read_text()
assert "FileStream" not in source and "UnityEngine" not in source
assert "TryPaidQuote" in source and "TryReserveOneShotLoaner" in source
assert "TryStageTrustedVictory" in source
print("W24_MANIFEST_PASS 13 exclusive additive paths 12 pinned Git blobs 4 globally unique Unity GUIDs; frozen V3/V4/V5 unchanged; SOURCE_G0_INDEPENDENT_PENDING")
