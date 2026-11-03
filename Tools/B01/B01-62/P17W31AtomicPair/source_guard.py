#!/usr/bin/env python3
"""W31 B01-62 sole writer source-gate. No foreign game/owner/frozen edits."""
import hashlib,json,subprocess,sys
from pathlib import Path
BASE="78c1d54d24a76c931ea80965a50eca03fae4156a"
SLOT="Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs"
SESSION="Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/CanonicalWholeGameSessionV3.cs"
OLDWF=".github/workflows/b01-62-w30-single-writer-admission.yml"
ROOT="Tools/B01/B01-62/P17W31AtomicPair/"
OWN={"Program.cs","AtomicPairQA.csproj","README.md","source_guard.py","manifest.json"}
WORKFLOW=".github/workflows/b01-62-w31-frozen-atomic-pair.yml"
def git(*x):return subprocess.check_output(["git",*x],stderr=subprocess.PIPE).decode().strip()
def main(destination):
 dest=Path(destination)
 if dest.exists():raise AssertionError("REFUSE_EXISTING_RECEIPT")
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],check=True)
 paths={SLOT,SESSION,OLDWF,WORKFLOW}|{ROOT+x for x in OWN}
 changed=set(git("diff","--name-only",BASE,"HEAD").splitlines())
 if changed!=paths:raise AssertionError("NOT_ONLY_9_OWNER_PATHS:"+str(changed^paths))
 slot=Path(SLOT).read_text();session=Path(SESSION).read_text()
 for word in ("frozenWorld=(byte[])world.Clone();",
              "frozenEcon=econ.Copy();",
              "Semantic(frozenWorld,frozenEcon,expectedGeneration+1)",
              "Encode(expectedGeneration+1,frozenWorld,",
              "actualWorld.SequenceEqual(frozenWorld)",
              "SameEconomicSection(actualEconomy,frozenEcon)",
              "File.Replace(staging,authoritative,null)",
              "EconomyV7AdmissionW29.TryDecodeForCanonicalOwner"):
  if word not in slot:raise AssertionError("SINGLE_WRITER_FREEZE_MISSING:"+word)
 for word in ("frozenWorld=(byte[])world.Clone();","frozenEconomy=economy.Copy();",
              "ValidatePair(frozenWorld,frozenEconomy)",
              "gateway.TryRestore(frozenWorld,runId,minimum)",
              "armedWorld=frozenWorld;","armedEconomy=frozenEconomy;"):
  if word not in session:raise AssertionError("GATEWAY_FREEZE_MISSING:"+word)
 for old in ("SHA256.HashData(","Convert.ToHexString(","File.Move(staging,authoritative,true)"):
  if old in slot+session:raise AssertionError("OLD_UNITY_ABI:"+old)
 if "  pull_request:" in Path(OLDWF).read_text():
  raise AssertionError("INHERITED_CI_FIXED_SCOPE")
 for p in ("Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs",
           "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs",
           "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"):
  if git("rev-parse",BASE+":"+p)!=git("rev-parse","HEAD:"+p):
   raise AssertionError("ORIGINAL_FROZEN_MODIFIED")
 obj={"base_885":BASE,"changed_only":sorted(paths),
      "old_source_only_tests":"W28 9 + W29 6 + W30 4",
      "new_source_only_tests":"W31 2",
      "disk_format":"B0162G02","writer_count":1,
      "unity_native_g1":"NOT_RUN","independent_g0":"PENDING_NEW_W31_SHA",
      "owner_policy_834":"PENDING"}
 raw=(json.dumps(obj,sort_keys=True,indent=2)+"\n").encode()
 dest.write_bytes(raw)
 print("B0162_W31_EXCLUSIVE_OWNER_DIFF_9_PATHS=PASS")
 print("B0162_W31_INPUT_SNAPSHOT_CONSISTENCY_STATIC_GUARD=PASS")
 print("B0162_W31_SOURCE_RECEIPT_SHA256="+hashlib.sha256(raw).hexdigest())
if __name__=="__main__":main(sys.argv[1])
