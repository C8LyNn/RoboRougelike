#!/usr/bin/env python3
"""Original #826 live one-file source and immutable #855 admission; NO game runtime writes."""
import json,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[3]
m=json.loads(Path(__file__).with_name("manifest.json").read_text())
def git(*a):
    return subprocess.check_output(["git",*a],cwd=root,text=True).strip()
base=m["base_original_save_sha"]
assert base=="99fb08619c78b3834bb4c0450499babf991f218a"
for path,sha in m["pinned_owned_blobs"].items():
    assert git("rev-parse","HEAD:"+path)==sha,("OWN_BLOB_CHANGED",path)
for path in ("Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs",
             "Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/CanonicalWholeGameSessionV3.cs"):
    assert git("rev-parse","HEAD:"+path)==m["original_production_git_blobs"][path],path
for path,sha,target in [
 ("Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs",
  m["original_production_git_blobs"]["Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs"],".source/ApprovedEconomyPolicyV7.cs"),
 ("Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs",
  m["original_production_git_blobs"]["Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs"],".source/CanonicalEconomySectionV7.cs"),
 ("Assets/B01Packages/ECONADMISSIONW29/Runtime/EconomyV7AdmissionW29.cs",
  m["original_production_git_blobs"]["Assets/B01Packages/ECONADMISSIONW29/Runtime/EconomyV7AdmissionW29.cs"],".source/EconomyV7AdmissionW29.cs")]:
    local=root/"Tools/B01/ECONW30RECEIVER"/target
    assert local.exists() and git("hash-object",str(local))==sha,("FOREIGN_SOURCE_MISMATCH",path)
if subprocess.run(["git","cat-file","-e",base+"^{commit}"],cwd=root,
    stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode:
    subprocess.run(["git","fetch","--no-tags","--depth=1","origin",base],cwd=root,check=True)
changed=set(git("diff","--name-only",base,"HEAD").splitlines())
assert changed==set(m["owned_paths"]) and len(changed)==6,sorted(changed)
assert len(m["pinned_owned_blobs"])==5
assert all(p.startswith("Tools/B01/ECONW30RECEIVER/") or p==".github/workflows/b01-22-w30-826-855-cold-receiver.yml" for p in changed)
assert not any(p.startswith(("Assets/","Tools/B01/B01-62/")) for p in changed)
assert m["original_826_writer_modified"] is False
assert m["actual_native_unity"]=="NOT_RUN"
print("W30_ORIGINAL_CONSUMER_SCOPE_PASS original #826 two whole-game CSharp files unchanged, #780 two producer blobs and #855 original guard unchanged; six own ADD-only paths five own SHA1 pins; G1 NOT_RUN")
