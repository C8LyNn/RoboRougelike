#!/usr/bin/env python3
"""B01-22 W28 exact original Codex live-game source provenance ONLY, never Unity PASS."""
from pathlib import Path
import json,re,subprocess
root=Path(__file__).resolve().parents[3]
m=json.loads(Path(__file__).with_name("manifest.json").read_text())
def git(*args):
    return subprocess.check_output(["git",*args],cwd=root,text=True).strip()
assert m["codex_base_sha"]=="e03d36f2b86f8c73a067e574b67237a8921f622a"
for p,sha in m["original_game_blobs"].items():
    assert git("rev-parse","HEAD:"+p)==sha,("GAME_SOURCE_MISMATCH",p)
for p,sha in m["package_blobs"].items():
    assert git("rev-parse","HEAD:"+p)==sha,("PACKAGE_BLOB_MISMATCH",p)
base=m["codex_base_sha"]
if subprocess.run(["git","cat-file","-e",base+"^{commit}"],cwd=root,
                  stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode:
    subprocess.run(["git","fetch","--no-tags","--depth=1","origin",base],
                   cwd=root,check=True,stdout=subprocess.DEVNULL)
changed=set(git("diff","--name-only",base,"HEAD").splitlines())
assert changed==set(m["package_paths"]) and len(changed)==6,sorted(changed)
assert not any(x.startswith(("Assets/R1/","Assets/R3/","Assets/WeaponIntegration/")) for x in changed)
test=(root/"Assets/Tests/Editor/B0122W28LiveGarageRecoveryRegression.cs").read_text()
assert test.count("[Test]")==4
for term in ["R3VehicleCatalog.Create","R3VehicleModel","EmergencyRepair()",
             "RepairCost(","CanDeploy","Core","Wheel","Armor"]:
    assert term in test,term
meta=(root/"Assets/Tests/Editor/B0122W28LiveGarageRecoveryRegression.cs.meta").read_text()
guids=re.findall(r"^guid: ([0-9a-f]{32})$",meta,re.M)
assert guids==["52f950262b3243dc81d76b24ba042df3"]
old=[p for p in (root/"Assets").rglob("*.meta") if p.name!="B0122W28LiveGarageRecoveryRegression.cs.meta"]
assert not any(guids[0] in p.read_text(errors="replace") for p in old)
assert m["unity_native"]=="NOT_RUN" and m["source_g0"]=="INDEPENDENT_PENDING"
print("W28_SOURCE_PROVENANCE_PASS 6 add-only files original Codex R1/R3 unchanged 1 unique GUID 4 authored REAL R3 Unity NUnit tests; UNITY_EDITMODE_NOT_RUN; NO_GAMEPLAY_INTEGRATION")
