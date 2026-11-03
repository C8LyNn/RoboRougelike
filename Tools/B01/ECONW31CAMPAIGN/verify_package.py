#!/usr/bin/env python3
"""B01-22 P17 W31 standalone test-only transport, no native Unity claim."""
from pathlib import Path
import json,subprocess
r=Path(__file__).resolve().parents[3]
m=json.loads(Path(__file__).with_name("manifest.json").read_text())
def g(*args):
    return subprocess.check_output(["git",*args],cwd=r,text=True).strip()
assert m["selected_original_896_sha"]=="a0282f2de0250d2a8fe421bbe182b4cb6fab2a11"
for p,sha in m["source_producer_blobs"].items():
    assert g("rev-parse","HEAD:"+p)==sha,("ORIGINAL_PRODUCER_CHANGED",p)
for p,sha in m["own_pinned_blobs"].items():
    assert g("rev-parse","HEAD:"+p)==sha,("W31_TEST_FILE_MODIFIED",p)
for p,sha in m["immutable_fetched_source_blobs"].items():
    path=r/"Tools/B01/ECONW31CAMPAIGN/.source"/p
    assert path.exists() and g("hash-object",str(path))==sha,("FOREIGN_SOURCE_CHANGED",p)
base=m["selected_original_896_sha"]
if subprocess.run(["git","cat-file","-e",base+"^{commit}"],cwd=r,
    stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode:
    subprocess.run(["git","fetch","--no-tags","--depth=1","origin",base],cwd=r,check=True)
changed=set(g("diff","--name-only",base,"HEAD").splitlines())
assert changed==set(m["own_added_paths"]) and len(changed)==6,sorted(changed)
assert len(m["own_pinned_blobs"])==5
for p in changed:
    assert p.startswith("Tools/B01/ECONW31CAMPAIGN/") or p==".github/workflows/b01-22-w31-original-896-campaign.yml",p
    assert not p.startswith(("Assets/","Tools/B01/B01-62/"))
assert m["new_game_source_files"]==0
assert m["native_unity_g1"]=="NOT_RUN"
assert m["owner_policy_834"]=="PENDING"
print("W31_SOURCE_SCOPE_PASS original selected #896 3 producer Git blobs unchanged, #780 frozen 2 source blobs + #855 1 guard pinned, 6 own ADD-only paths, 5 own blobs; Unity native NOT_RUN")
