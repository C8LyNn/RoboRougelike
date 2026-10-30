#!/usr/bin/env python3
"""B01-61 independently inspect original #772 physical Unity C# authored source, GUID/asmdef and real direct C#9 stdout."""
import argparse,hashlib,json,re,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1"
OWN="Tools/B01/B01-61/P17W25Physical772IndependentQA/"
FLOW=".github/workflows/b01-61-w25-772-original-rig-ledger.yml"
ASSETS="Assets/B0123CanonicalRigPortW24/"
OWNER={
ASSETS+"Runtime/CanonicalPhysicalRigPortW24.cs":"2a9ac87d3028d5c5a51915a27c8d2f49aa363f85",
ASSETS+"Runtime/CanonicalRigLedgerHydratorW24.cs":"eb21cb12605e94d17dc7decdf040c9009ca0aafe",
ASSETS+"Tests/PlayMode/CanonicalPhysicalRigPortW24NativeTests.cs":"6827351e8f578030de932b2d36a5c0c55337a2f7"}
FROZEN={
"DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"}
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT,text=True,stderr=subprocess.DEVNULL).strip()
def main():
 p=argparse.ArgumentParser();p.add_argument("--log",type=Path,required=True);p.add_argument("--out",type=Path,required=True);a=p.parse_args()
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 for path,sha in OWNER.items():
  if git("rev-parse","HEAD:"+path)!=sha or git("hash-object",path)!=sha:raise AssertionError("ORIGINAL_772_SOURCE_CHANGED "+path)
 for name,sha in FROZEN.items():
  if git("hash-object",str(ROOT/OWN/".original"/name))!=sha:raise AssertionError("ORIGINAL_FROZEN_LEDGER_SOURCE_CHANGED "+name)
 changed=git("diff","--name-only",BASE,"HEAD").splitlines()
 if len(changed)!=5 or any(not (x.startswith(OWN) or x==FLOW) for x in changed):
  raise AssertionError("QA_NOT_FIVE_OWN_ONLY "+repr(changed))
 for x in changed:
  if subprocess.run(["git","cat-file","-e",BASE+":"+x],cwd=ROOT,stderr=subprocess.DEVNULL).returncode==0:raise AssertionError("ORIGINAL_772_MODIFIED "+x)
 meta=git("ls-files","*.meta").splitlines()
 newmeta=[p for p in meta if p.startswith(ASSETS)]
 if len(newmeta)!=9:raise AssertionError("ORIGINAL_772_NEW_GUID_COUNT_NOT_NINE "+repr(newmeta))
 seen={}
 for path in meta:
  content=(ROOT/path).read_text(encoding="utf8",errors="replace")
  match=re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$",content)
  if match:
   guid=match.group(1)
   if guid in seen:raise AssertionError("PROJECT_DUPLICATE_META_GUID "+guid+" "+path+" "+seen[guid])
   seen[guid]=path
  if path in newmeta:
   if not match:raise AssertionError("MISSING_772_META_GUID "+path)
   if not (ROOT/path[:-5]).exists():raise AssertionError("ORPHAN_772_META "+path)
 runtime=json.loads((ROOT/(ASSETS+"Runtime/B0123.CanonicalRigPortW24.Runtime.asmdef")).read_text())
 play=json.loads((ROOT/(ASSETS+"Tests/PlayMode/B0123.CanonicalRigPortW24.PlayMode.asmdef")).read_text())
 if runtime["name"]!="B0123.CanonicalRigPortW24.Runtime" or len(runtime["references"])!=4 or \
    play["references"][0]!=runtime["name"] or "UNITY_INCLUDE_TESTS" not in play["defineConstraints"]:
  raise AssertionError("772_ASMDEF_CLOSURE_OR_NATIVE_TEST_FLAG_MISSING")
 source=(ROOT/(ASSETS+"Runtime/CanonicalPhysicalRigPortW24.cs")).read_text()
 for token in ["private sealed class Rig","private sealed class PhysicalWorld","Rigidbody",
               "AtomicDockLedger","AllOwnedPhysicalRootsDestroyed",
               "SetExternalWritersQuiesced","CanonicalRigLedgerHydratorW24.TryHydrate",
               "linearVelocity","angularVelocity","CleanupUnpublishedAndVerifyDestroyed"]:
  if token not in source:raise AssertionError("772_UNITY_PHYSICS_IMPL_MISSING "+token)
 fixture=(ROOT/(ASSETS+"Tests/PlayMode/CanonicalPhysicalRigPortW24NativeTests.cs")).read_text()
 if fixture.count("[UnityTest]")!=3 or "private sealed class Disk : ICanonicalCommitPort" not in fixture:
  raise AssertionError("772_NATIVE_FIXTURE_NOT_THREE_OR_TESTPORT_MISSING")
 log=a.log.read_text(encoding="utf8")
 for token in ["TOTAL 13 PASS 13 FAIL 0",
               "B0161_W25_772_ORIGINAL_510_ATOMIC_DOCK_LEDGER_AND_556_SOURCE",
               "UNITY_PHYSICS_G1=NOT_RUN ORIGINAL_FROZEN_PRIVATE_RIG_NOT_CONNECTED GAME_SAVELOAD_HOLD"]:
  if token not in log:raise AssertionError("772_ACTUAL_SOURCE_LOG_MISSING "+token)
 result={"qa_head":git("rev-parse","HEAD"),"original_772_sha":BASE,
         "original_blob_shas":{**OWNER,**FROZEN},
         "new_772_unique_meta_guids":len(newmeta),
         "original_project_meta_guids_total_unique":len(seen),
         "original_unity_fixture_authored_cases":3,"native_executed_cases":0,
         "source_original_Csharp9_assertions_per_os":13,
         "qa_5_added_only_sha256":{x:hashlib.sha256((ROOT/x).read_bytes()).hexdigest() for x in changed},
         "original_source_G0":"QA_SOURCE_AND_META_ONLY_NOT_COMBINED","native_Unity_G1":"NOT_RUN",
         "real_510_private_Rig_game_session":"NOT_CONNECTED",
         "G2":"HOLD","merged":False,"released":False}
 a.out.parent.mkdir(parents=True,exist_ok=True);a.out.write_text(json.dumps(result,indent=2,sort_keys=True)+"\n")
 print(json.dumps(result,sort_keys=True))
if __name__=="__main__":main()
