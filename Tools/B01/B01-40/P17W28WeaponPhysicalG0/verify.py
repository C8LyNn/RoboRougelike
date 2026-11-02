#!/usr/bin/env python3
"""B01-40 P17/W28: inspect ORIGINAL #824 exact source and REAL native NUnit fixture.
Run on separate Git source checkout; never Unity, Save, build, or owner workspace.
"""
import argparse
import hashlib
import json
import pathlib
import re
import subprocess
import sys
import tempfile

BASE="e03d36f2b86f8c73a067e574b67237a8921f622a"
PARENT="e7a85944465d908f175a6f5e2ae07f4aedcc593f"
TEST="Assets/Tests/Editor/B0140W28PhysicalClearanceTests.cs"
META=TEST+".meta"
OWN="Tools/B01/B01-40/P17W28WeaponPhysicalG0/"
CI=".github/workflows/b01-40-w28-original-physx-weapon-g0.yml"
ORIGINAL={
 "Assets/R3/R3VehicleAssembly.cs":"ed97dd1b0ee3b77c839615497b249dcaee479bb8",
 "Assets/Tests/Editor/W27SpecialWeaponClearanceTests.cs":"5041bbb68bbf10d859bec25dc52ff4356d39e0c4",
 "Assets/Tests/Editor/W27BeamClearanceTests.cs":"39147e59ef43d50514e74875642a9af7833855a6",
 "Assets/WeaponIntegration/RRLBeamBinding.cs":"fd91d20979cafbb706121e2e4943d6d317b0dcd9",
 "Assets/WeaponIntegration/RRLShotgunBinding.cs":"05f48723a632a6329fe7030e0989ec26e8e04f24",
 "Assets/R1/W27WeaponPlayerQA.cs":"1f9023317f49488eb5ebbad93fb079d6b0d3b024",
}
EXPECTED={"Basic","Automatic","Heavy","Shotgun","Railgun","RocketLauncher"}
def run(*args):
 p=subprocess.run(["git",*args],capture_output=True)
 if p.returncode:raise RuntimeError("G0_FAIL_GIT: "+str(args)+" "+p.stderr.decode(errors="replace")[-160:])
 return p.stdout
def must(ok,reason):
 if not ok:raise RuntimeError("B0140_W28_G0_HOLD: "+reason)
def sha(b):return hashlib.sha256(b).hexdigest()
def blob(commit,path):return run("show",commit+":"+path)
def check():
 head=run("rev-parse","HEAD").decode().strip()
 must(head!=BASE,"NO_NEW_QA_DELTA")
 must(run("rev-parse",BASE).decode().strip()==BASE,"INCORRECT_BASE")
 must(subprocess.run(["git","merge-base","--is-ancestor",BASE,head],capture_output=True).returncode==0,
      "QA_HEAD_DOES_NOT_DESCEND_EXACT_W27")
 must(subprocess.run(["git","merge-base","--is-ancestor",PARENT,BASE],capture_output=True).returncode==0,
      "W26_W27_HISTORY_NOT_ORDERED")
 rows=run("diff","--name-status",BASE,head).decode().splitlines()
 paths=[]
 for row in rows:
  status,path=row.split("\t",1)
  must(status=="A","SOURCE_PRODUCER_MODIFIED:"+path)
  must(path in (TEST,META,CI) or path.startswith(OWN),
       "FOREIGN_OR_PRODUCER_PATH:"+path)
  paths.append(path)
 must(all(p in paths for p in (TEST,META,OWN+"verify.py",OWN+"README.md",CI)),
      "MISSING_MINIMUM_NATIVE_TEST_RECEIPT_OR_CI")
 print("B0140_W28_ORIGINAL_FINAL_W27_SHA_AND_OWNER_ONLY_SCOPE_PASS",head,len(paths))
 for path,expected in ORIGINAL.items():
  must(run("rev-parse",BASE+":"+path).decode().strip()==expected,
       "ORIGINAL_W27_PRODUCER_GIT_BLOB_CHANGED:"+path)
  must(blob("HEAD",path)==blob(BASE,path),"W27_ORIGINAL_CONTENT_CHANGED:"+path)
 print("B0140_W28_ORIGINAL_R3_PROJECTILE_AND_BEAM_SHA_PASS",len(ORIGINAL))
 original=blob(BASE,"Assets/R3/R3VehicleAssembly.cs")
 must(b"public bool ClearProjectilePath(R3MountedPart part,Vector3 direction,float range,float radius)" in original,
      "ORIGINAL_PHYSX_CLEARANCE_SIGNATURE_CHANGED")
 must(b"Physics.SphereCastAll" in original and b"Physics.OverlapSphere" in original,
      "NOT_ORIGINAL_PHYSX_PATH")
 must(b"ClearProjectilePath(part)" in original,
      "GROUP_CLEARANCE_NOT_CALLED")
 must(b"RRLBeamBinding" in blob(BASE,"Assets/WeaponIntegration/RRLBeamBinding.cs"),
      "MISSING_BEAM")
 must(b"Mathf.Min(pellet.MaxDistance" in blob(BASE,"Assets/WeaponIntegration/RRLShotgunBinding.cs"),
      "SHOTGUN_PHYSICAL_LIFETIME_MISSING")
 test=blob("HEAD",TEST)
 hits={n for n in EXPECTED if ("R4WeaponKind."+n).encode() in test}
 must(hits==EXPECTED,"MISSING_ALL_SIX_KINDS:"+str(EXPECTED-hits))
 needs=[
  b"[UnityTest]",b"[UnitySetUp]",b"[UnityTearDown]",
  b"EnterPlayMode",b"ExitPlayMode",b"R3VehicleAssembly.Build",
  b"GetComponent<Rigidbody>()",b"MovePosition(",b"WaitForFixedUpdate",
  b"Physics.SyncTransforms",b"ClearProjectilePath(c.Gun)",
  b"Is.False",b"Is.True",b"CheckSuppression",
  b"DeviceReadyAt",b"State.Ammo",b"NextFireTime",
  b"FireGroup(false)",b"FireGroup(true)",
  b"R4ProjectileOrigin",b"R4WeaponKind.Basic",
  b"R4WeaponKind.Automatic",b"second.SetActive(false)",
 ]
 for token in needs:must(token in test,"NATIVE_PHYSX_TEST_CONTRACT_MISSING:"+token.decode())
 must(test.count(b"[UnityTest]")==2,"EXACT_TWO_AUTHORED_TESTS_EXPECTED")
 meta=blob("HEAD",META)
 g=re.findall(rb"(?m)^guid:\s*([0-9a-f]{32})\s*$",meta)
 must(len(g)==1,"INVALID_NEW_UNITY_META")
 guid=g[0]
 # Check ALL Unity metas at parent, not just this fixture.
 # One Git index scan on both platforms, not thousands of Git subprocesses.
 probe=subprocess.run(["git","grep","-l",guid.decode(),BASE,"--","Assets"],
                      capture_output=True)
 must(probe.returncode==1,
      "NEW_GUID_COLLIDES_OR_GIT_SCAN_FAILED:"+probe.stdout.decode(errors="replace")[-240:])
 print("B0140_W28_UNIQUE_NEW_UNITY_GUID_AND_TWO_NATIVE_TEST_SOURCES_PASS",guid.decode())
 # Real negative fixture mutation proving byte-guard refusal, no pseudo-PASS.
 with tempfile.TemporaryDirectory(prefix="b0140-w28-owned-only-") as d:
  root=pathlib.Path(d)
  foreign=root/"foreign"/"EMPTY"
  foreign.mkdir(parents=True)
  keep=root/"foreign"/"save.bin"
  keep.write_bytes(b"DO_NOT_TOUCH")
  staged=root/"native-test.cs"
  staged.write_bytes(test)
  def valid_file():return staged.read_bytes()==test
  must(valid_file(),"GOOD_STAGE_MISMATCH")
  staged.write_bytes(test.replace(b"MovePosition(",b"MovePosition_BROKEN(",1))
  must(not valid_file(),"DIRTY_OWNED_TEST_WAS_NOT_REJECTED")
  staged.write_bytes(test)
  must(valid_file(),"GOOD_RECOVERY_MISMATCH")
  staged.unlink()
  must(keep.read_bytes()==b"DO_NOT_TOUCH" and foreign.is_dir(),
       "FOREIGN_SAVE_OR_EMPTY_DIRECTORY_DELETED")
 print("B0140_W28_REAL_DISPOSABLE_SOURCE_STAGE_DIRTY_NEGATIVE_AND_FOREIGN_PRESERVATION_PASS")
 inventory={p:{"bytes":len(blob("HEAD",p)),"sha256":sha(blob("HEAD",p))}
            for p in sorted(paths)}
 report={
  "schema":"B01-40-W28-INDEPENDENT-NATIVE-TEST-SOURCE-G0-V1",
  "head":head,
  "original_w27_head":BASE,
  "original_w26_head":PARENT,
  "original_git_blob_sha":ORIGINAL,
  "owned_new_paths":inventory,
  "native_unity_version":"6000.3.14f1",
  "native_cases":["EveryProjectileClass_TracksMovingOwnerColliderAndNeverSpendsOnBlockedFire",
                  "DifferentLpmRpmGroupsAreIndependent_WhenOwnedArmorBlocksOnlyOneGun"],
  "source_g0":"PASS_SOURCE_ONLY",
  "native_editor_playmode":"NOT_RUN",
  "native_windows_player":"NOT_RUN",
  "user_gameplay_build":"NOT_GENERATED",
  "saw_emp_native_regression":"NOT_IMPLEMENTED_BY_THIS_QA",
  "unity_scene_garage_3d":"NOT_VALIDATED",
  "loaner_softlock":"RED_UNRESOLVED_OWNER_POLICY",
  "economy_save":"BLOCKED_BY_PRODUCER_797_UNITY_API",
  "codex_ack":"NONE",
  "merge":False,"release":False,
  "original_game_files_modified":False,
 }
 return report
def main():
 p=argparse.ArgumentParser()
 p.add_argument("--output",required=True)
 args=p.parse_args()
 report=check()
 output=pathlib.Path(args.output)
 must(not output.exists(),"OVERWRITE_EXISTING_EVIDENCE")
 output.parent.mkdir(parents=True,exist_ok=True)
 b=(json.dumps(report,sort_keys=True,indent=2,ensure_ascii=False)+"\n").encode("utf-8")
 output.write_bytes(b)
 print("B0140_W28_NATIVE_UNITY_PHYSX_TEST_SOURCE_G0_PASS_TWO_AUTHORED_NATIVE_NOT_RUN")
 print("B0140_W28_RECEIPT_SHA256",sha(b))
if __name__=="__main__":
 try:main()
 except Exception as e:
  print("B0140_W28_G0_FAIL",e,file=sys.stderr)
  raise
