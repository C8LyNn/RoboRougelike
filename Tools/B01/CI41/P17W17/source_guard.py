#!/usr/bin/env python3
"""Original #596 conditional native PlayMode per-test filesystem isolation witness.

This is a direct original production C# FileCheckpointStore test, not Unity G1.
"""
import argparse
import json
from pathlib import Path
import re
import subprocess
import sys

FINAL="efb2b06eafedab5469b57f163152d96aca3c5915"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
HOST="Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs"
PLAY="Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"
CORE="Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs"
BLOBS={
 HOST:"4bac6f327c9b93ae98dd2061c0ca8ea2100aab42",
 PLAY:"1ad6239b8e4e932dc1b87bb355d225867b2f4a4b",
 CORE:"1f9e883c38101f214e4335867faa731f7ae67689",
}
OWNED={
"Tools/B01/CI41/P17W17/Original596NativeIsolationQA.csproj",
"Tools/B01/CI41/P17W17/Program.cs",
"Tools/B01/CI41/P17W17/source_guard.py",
"Tools/B01/CI41/P17W17/test_source_guard.py",
"Tools/B01/CI41/P17W17/README.md",
".github/workflows/b01-41-p17w17-596-native-test-isolation.yml",
}
ORIGINAL_NATIVE_CASES={
"ColdLoad_RealRigidbody_RoundTripLightHeavy",
"CorruptLoad_LeavesPreviouslyActiveRigidbodyAndIdsUntouched",
"ColdRestart_RestoresRealRigidbodyAndConsumedIds",
"WrongGeneration_CannotPartiallyPublishRealPhysicalWorld",
}
class Refuse(Exception):pass

def require(ok,why):
    if not ok:raise Refuse(why)

def git(root,*args):
    result=subprocess.run(["git","-C",str(root),*args],capture_output=True)
    require(result.returncode==0,"GIT_OBJECT_OR_ANCESTRY_MISSING:"+args[0])
    return result.stdout.decode("utf-8","replace").strip()

def validate_host_source(source):
    require(re.search(r'Application\.persistentDataPath\s*,\s*"b0123_one_save_host_v2\.checkpoint"',source)
            is not None,"SHARED_FIXED_NATIVE_SAVE_SLOT_NO_LONGER_PRESENT_REVIEW")
    require("new FileCheckpointStore(sidecar)" in source,
            "NATIVE_HOST_DOES_NOT_USE_REAL_ORIGINAL_FILE_STORE")
    require("private void Awake()" in source,
            "SHARED_FILE_CREATED_OUTSIDE_ORIGINAL_AWAKE")
    require("UsePersistentCheckpointSlot" not in source,
            "NEW_NATIVE_SLOT_SELECTION_API_REQUIRES_NEW_RECEIVER_REVIEW")

def validate_original_native_source(source):
    pairs=re.findall(r"\[UnityTest\]\s*public IEnumerator\s+(\w+)\s*\(",source)
    require(len(pairs)==4 and set(pairs)==ORIGINAL_NATIVE_CASES,
            "SELECTED_596_ORIGINAL_NATIVE_CASE_INVENTORY_MOVED")
    ids={int(x) for x in re.findall(r"MakeFixture\(\s*(\d+)\s*,",source)}
    require(ids=={9,31,41},"ORIGINAL_NATIVE_TEST_RUN_ID_FIXTURES_MOVED")
    require("File.Delete(" not in source and "File.Move(" not in source,
            "ORIGINAL_NATIVE_TEST_HAS_NEW_FILE_LIFECYCLE_REVIEW")
    return ids

def audit(root):
    for sha in (FINAL,GAME,SAVE):
        require(git(root,"rev-parse","--verify",sha+"^{commit}")==sha,
                "ORIGINAL_IMMUTABLE_COMMIT_REQUIRED")
    git(root,"merge-base","--is-ancestor",GAME,FINAL)
    git(root,"merge-base","--is-ancestor",SAVE,FINAL)
    git(root,"merge-base","--is-ancestor",FINAL,"HEAD")
    actual=set(git(root,"diff","--name-only",FINAL,"HEAD").splitlines())
    require(actual==OWNED,"CI41_W17_OWNER_SCOPE_VIOLATION")
    for path,blob in BLOBS.items():
        require(git(root,"rev-parse",FINAL+":"+path)==blob
                and git(root,"rev-parse","HEAD:"+path)==blob,
                "ORIGINAL_SELECTED_596_PRODUCER_BLOB_DRIFT:"+path)
    host=(root/HOST).read_text(encoding="utf-8")
    tests=(root/PLAY).read_text(encoding="utf-8")
    validate_host_source(host)
    runs=validate_original_native_source(tests)
    # Core source is directly compiled/executed by our .NET8 project,
    # not copied; original bytes checked against Git object above.
    require("public sealed class FileCheckpointStore" in
            (root/CORE).read_text(encoding="utf-8"),
            "MISSING_ACTUAL_ORIGINAL_FILE_STORE")
    return {
     "package":"CI41-SELECTED-596-NATIVE-ISOLATION-001","version":"R1",
     "selected_authoritative_adapter":"#596","selected_adapter_sha":FINAL,
     "alternative_604":"ALT_HOLD_NOT_INSTALLED",
     "frozen_game_510":GAME,"frozen_save_556":SAVE,
     "actual_original_producer_blobs":BLOBS,
     "original_unity_native_playmode_case_names":sorted(ORIGINAL_NATIVE_CASES),
     "native_test_run_ids":sorted(runs),
     "shared_fixed_file":"b0123_one_save_host_v2.checkpoint",
     "native_test_isolation":"SHARED_GLOBAL_FIXED_SLOT_POTENTIAL_TEST_CONFLICT",
     "original_native_cases_executed":"NOT_RUN",
     "direct_original_source_CSharp_test":"EXECUTE_SEPARATELY_IN_WORKFLOW",
     "source_test_result":"ACTUAL_HISTORICAL_STATE_ISOLATION_RISK_DETECTED",
     "native_unity_g1":"NOT_RUN","source_bridge_g0":"B01_40_RECEIVER_ONLY",
     "live_canonical_510_save":"HOLD","codex_ack":False,
     "g2":"HOLD","release_gate":"HOLD","owned_paths":sorted(OWNED)
    }

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",default=".")
    p.add_argument("--output",required=True)
    args=p.parse_args()
    try:
        report=audit(Path(args.repo))
        code=0
    except (Refuse,OSError,ValueError,UnicodeError) as e:
        report={"package":"CI41-SELECTED-596-NATIVE-ISOLATION-001",
                "source_test_result":"FAIL_CLOSED",
                "reason":str(e),"native_unity_g1":"NOT_RUN",
                "release_gate":"HOLD"}
        code=2
    result=json.dumps(report,indent=2,sort_keys=True)+"\n"
    Path(args.output).write_text(result,encoding="utf-8")
    print(result,end="")
    return code
if __name__=="__main__":
    sys.exit(main())
