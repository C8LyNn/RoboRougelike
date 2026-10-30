#!/usr/bin/env python3
"""Independent original R5+750 source SHA receipt, executed real C# generation5 cold."""
import argparse,hashlib,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb"
OWN="Tools/B01/B01-61/P17W24R5ColdRestart750QA/"
FLOW=".github/workflows/b01-61-w24-750-coldrestart-actual-source.yml"
PINS={"AtomicWorldSwap.cs":"7da9a9efe489cf8c441d7bb9a47ba3f60ba4c58c",
"DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
"DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"}
FIXTURE="Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"
FIXTURE_SHA="2508a04f57b04447112d2777ce4e3cd7478ed40e"
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT,text=True,stderr=subprocess.DEVNULL).strip()
def main():
 p=argparse.ArgumentParser();p.add_argument("--log",type=Path,required=True);p.add_argument("--out",type=Path,required=True);a=p.parse_args()
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 if git("rev-parse","HEAD:"+FIXTURE)!=FIXTURE_SHA or git("hash-object",FIXTURE)!=FIXTURE_SHA:raise AssertionError("ORIGINAL_750_UNITY_FIXTURE_DRIFT")
 for path,sha in PINS.items():
  if git("hash-object",str(ROOT/OWN/".original"/path))!=sha:raise AssertionError("ORIGINAL_R5_GIT_SOURCE_DRIFT "+path)
 changed=git("diff","--name-only",BASE,"HEAD").splitlines()
 if len(changed)!=5 or any(not(x.startswith(OWN) or x==FLOW) for x in changed):raise AssertionError("NOT_FIVE_NEW_QA_ONLY "+repr(changed))
 for x in changed:
  if subprocess.run(["git","cat-file","-e",BASE+":"+x],cwd=ROOT,stderr=subprocess.DEVNULL).returncode==0:raise AssertionError("OWNER_SOURCE_OVERWRITE "+x)
 before=git("show","cc7f73541517a18f4479bb155534bd4832d9985e:"+FIXTURE)
 after=(ROOT/FIXTURE).read_text()
 start="public IEnumerator ColdRestart_RestoresRealRigidbodyAndConsumedIds()"
 body=after.split(start,1)[1].split("public IEnumerator WrongGeneration_",1)[0]
 old=before.split(start,1)[1].split("public IEnumerator WrongGeneration_",1)[0]
 for fragment in ["MakeFixture(41,4,true,false)","SnapshotCodec.TryDecode(saved,out RunSnapshot savedSnapshot)","Assert.AreEqual(5,savedSnapshot.Generation)","restored.TryRestoreBytes(saved,41,5)","Assert.AreEqual(5,restored.Generation)"]:
  if fragment not in body:raise AssertionError("COLD_FIXTURE_SOURCE_MISSING "+fragment)
 for oldpart in ["restored.TryRestoreBytes(saved,41,4)","Assert.AreEqual(4,restored.Generation)"]:
  if oldpart in body or oldpart not in old:raise AssertionError("BAD_COLD_FIXTURE_SAME_GEN_NOT_CORRECTED")
 expected=("TOTAL 5 PASS 5 FAIL 0","W24_SELECTED_R5_690_ACTUAL_CORE_556_G4_SAVE_BYTES_G5=PASS",
 "W24_SELECTED_R5_690_COLD_RESTART_SAVED_G5_ACCEPTED=PASS","SOURCE_FIXTURE_750=CORRECT_GENERATION_5_ONLY NATIVE_UNITY_G1_NOT_RUN")
 log=a.log.read_text()
 for e in expected:
  if e not in log:raise AssertionError("ACTUAL_R5_SOURCE_EXECUTION_MISSING "+e)
 r={"qa_head":git("rev-parse","HEAD"),"original_fixture_750_sha":BASE,
    "original_fixture_git_blob":FIXTURE_SHA,"original_r5_556_W06_510_git_sha":PINS,
    "qa_five_added_only_sha256":{x:hashlib.sha256((ROOT/x).read_bytes()).hexdigest() for x in changed},
    "actual_original_source_cases_per_os":5,"source_gate":"GREEN_FIXTURE_GENERATION_5_ONLY",
    "native_Unity_G1":"NOT_RUN","R5_product_release":"HOLD","GAME_510_private_rig":"NOT_INTEGRATED",
    "merged":False,"released":False}
 a.out.parent.mkdir(parents=True,exist_ok=True);a.out.write_text(json.dumps(r,indent=2,sort_keys=True)+"\n")
 print(json.dumps(r,sort_keys=True))
if __name__=="__main__":main()
