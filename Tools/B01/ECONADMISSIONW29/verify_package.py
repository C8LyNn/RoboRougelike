#!/usr/bin/env python3
"""B01-22 W29 immutable V7 admission and strict only-owned ADD package identity."""
import json,re,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[3]
m=json.loads(Path(__file__).with_name("manifest.json").read_text())
def git(*args):
    return subprocess.check_output(["git",*args],cwd=root,text=True).strip()
assert m["base_sha"]=="b4df10a7ad7e6c758f8331013f6b216b3a3689a3"
for path,sha in m["frozen_original_v7_blobs"].items():
    assert git("rev-parse","HEAD:"+path)==sha,("FROZEN_V7_MODIFIED",path)
for path,sha in m["pinned_package_git_blobs"].items():
    assert git("rev-parse","HEAD:"+path)==sha,("OWN_BLOB_CHANGED",path)
base=m["base_sha"]
if subprocess.run(["git","cat-file","-e",base+"^{commit}"],cwd=root,
                  stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode:
    subprocess.run(["git","fetch","--no-tags","--depth=1","origin",base],
                   cwd=root,check=True,stdout=subprocess.DEVNULL)
changed=set(git("diff","--name-only",base,"HEAD").splitlines())
assert changed==set(m["only_added_paths"]) and len(changed)==13,changed
assert len(m["pinned_package_git_blobs"])==12
for path in changed:
    assert path.startswith("Assets/B01Packages/ECONADMISSIONW29") or path.startswith(
        "Tools/B01/ECONADMISSIONW29/") or path==".github/workflows/b01-22-w29-v7-admission.yml",path
    assert not path.startswith(("Assets/R1/","Assets/R3/","Assets/WeaponIntegration/"))
newmeta=[root/"Assets/B01Packages/ECONADMISSIONW29.meta"]+list(
    (root/"Assets/B01Packages/ECONADMISSIONW29").rglob("*.meta"))
assert len(newmeta)==4
ids=[]
for p in newmeta:
    found=re.findall(r"^guid:\s*([0-9a-f]{32})$",p.read_text(),re.M)
    assert len(found)==1,(str(p),found)
    ids.extend(found)
assert len(set(ids))==4
older=(root/"Assets/B01Packages").rglob("*.meta")
old_ids=set()
for p in older:
    if p not in newmeta:
        old_ids.update(re.findall(r"^guid:\s*([0-9a-f]{32})$",p.read_text(),re.M))
assert not(set(ids)&old_ids),set(ids)&old_ids
assembly=json.loads((root/"Assets/B01Packages/ECONADMISSIONW29/Runtime/B01.EconAdmission.W29.asmdef").read_text())
assert assembly["references"]==["B01.EconApprovedV7"]
assert assembly["autoReferenced"] is False and assembly["noEngineReferences"] is True
production=(root/"Assets/B01Packages/ECONADMISSIONW29/Runtime/EconomyV7AdmissionW29.cs").read_text()
assert "snapshot.Wins==0&&snapshot.Pending.Any(t=>t.Free)" in production
assert "TryDecodeForCanonicalOwner" in production and "TryEncodeForCanonicalOwner" in production
for dangerous in ["FileStream","File.Write","File.Move","File.Replace","UnityEngine","SHA256.Create"]:
    assert dangerous not in production,dangerous
assert m["unity_native"]=="NOT_RUN"
assert m["full_game_writer"]=="NOT_CHANGED"
print("W29_MANIFEST_PASS 13 add-only owned paths 12 exact Git blobs 2 unchanged original V7 CSharp runtime blobs 4 globally distinct Unity GUIDs SOURCE_G0_PENDING NATIVE_G1_NOT_RUN")
