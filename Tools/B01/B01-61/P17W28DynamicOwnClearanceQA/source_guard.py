#!/usr/bin/env python3
"""Exact-source independent Codex #824 dynamic-weapon Unity QA preflight. Not a Unity runner."""
import hashlib,json,pathlib,re,subprocess,sys
ROOT=pathlib.Path(__file__).resolve().parents[5]
BASE="e03d36f2b86f8c73a067e574b67237a8921f622a"
PREFIX="Tools/B01/B01-61/P17W28DynamicOwnClearanceQA/"
ADDED="Assets/Tests/Editor/B0161W28DynamicOwnClearanceRegressionTests.cs"
META=ADDED+".meta"
WORKFLOW=".github/workflows/b01-61-w28-dynamic-own-clearance-824.yml"
EXPECTED={ADDED,META,PREFIX+"README.md",PREFIX+"source_guard.py",WORKFLOW}
PINS={
"Assets/R3/R3VehicleAssembly.cs":"ed97dd1b0ee3b77c839615497b249dcaee479bb8",
"Assets/WeaponIntegration/RRLShotgunBinding.cs":"05f48723a632a6329fe7030e0989ec26e8e04f24",
"Assets/WeaponIntegration/RRLRailgunBinding.cs":"14fc1447e93ac2cd0983f24232099cdb02dcec75",
"Assets/WeaponIntegration/RRLRocketBinding.cs":"1778f19e5b35ddb7f39b39c52a142b385da063d9",
"Assets/WeaponIntegration/RRLBeamBinding.cs":"fd91d20979cafbb706121e2e4943d6d317b0dcd9",
"Assets/Tests/Editor/W27SpecialWeaponClearanceTests.cs":"5041bbb68bbf10d859bec25dc52ff4356d39e0c4",
"Assets/Tests/Editor/W27BeamClearanceTests.cs":"39147e59ef43d50514e74875642a9af7833855a6"
}
def git(*args):
 return subprocess.check_output(["git",*args],cwd=ROOT,stderr=subprocess.DEVNULL,text=True).strip()
def run():
 assert subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT).returncode==0
 head=git("rev-parse","HEAD")
 for path,sha in PINS.items():
  assert git("rev-parse",BASE+":"+path)==sha,(path,"base original changed")
  assert git("rev-parse","HEAD:"+path)==sha,(path,"proposed game code modified")
  assert git("hash-object",path)==sha,(path,"disk drift")
 changes=set(git("diff","--name-only",BASE,"HEAD").splitlines())
 assert changes==EXPECTED,("QA_ONLY_5_ADD_PATHS_EXPECTED",sorted(changes))
 for path in EXPECTED:
  assert subprocess.run(["git","cat-file","-e",BASE+":"+path],cwd=ROOT,
     stderr=subprocess.DEVNULL).returncode!=0,("NOT_ADD_ONLY",path)
 test=(ROOT/ADDED).read_text(encoding="utf8")
 methods=re.findall(r"\[UnityTest\]\s+public IEnumerator (\w+)\(",test)
 required={
 "MovingOwnedArmorBlocksBothDirectAndGroupSpecialGunsWithoutAmmoOrCooldown",
 "RotatedGunMustTreatDifferentGunColliderAsOwnedButExternalCoverAsFirstHit",
 "ContinuousBeamCoolsOrStaysStableWhileMovingOwnedArmorEntersLane"}
 assert set(methods)==required,("UNITY_CASES_MISSING",methods)
 for token in ["R3VehicleAssembly.Build(","R3VehicleCatalog.Create(","Physics.SyncTransforms()",
               "RRLShotgunBinding","RRLRailgunBinding","RRLRocketBinding",
               "RRLBeamBinding","DirectFire(obj.gun)","obj.assembly.FireGroup(false)",
               "DeviceReadyAt","NextFireTime","Is.EqualTo(ammo)","rb.position=",
               "gun.GunPivot","ClearProjectilePath(","beam.Module.Heat"]:
  assert token in test,("NATIVE_REAL_SOURCE_TARGET_MISSING",token)
 match=re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$",(ROOT/META).read_text())
 assert match, "MISSING_UNITY_META_GUID"
 guid=match.group(1)
 unique={}
 for raw in git("ls-files","*.meta").splitlines():
  txt=(ROOT/raw).read_text(encoding="utf8",errors="replace")
  x=re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$",txt)
  if x:
   old=unique.setdefault(x.group(1),raw)
   assert old==raw,("DUPLICATE_PROJECT_GUID",x.group(1),raw,old)
 assert unique.get(guid)==META
 manifest={"package":"B01-61-W28-CODEX824-DYNAMIC-PHYSX-REGRESSION-001",
   "codex_original_gameplay_source_pr824":BASE,"qa_head":head,
   "original_production_source_blobs":PINS,
   "new_qa_only_source_sha256":{p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in sorted(changes)},
   "new_unique_unity_meta_guid":guid,
   "native_unity_cases_authored":len(methods),"native_unity_execution":"NOT_RUN",
   "native_tests":methods,
   "tested":"GIT_SOURCE_INTEGRITY_AND_FIXTURE_PRECONDITIONS_ONLY",
   "windows_player":"NOT_RUN_ON_THIS_QA_HEAD",
   "worktree_or_game_source_changed":False,
   "merged":False,"g1":"NOT_RUN","g2":"HOLD"}
 out=pathlib.Path(sys.argv[1]);out.parent.mkdir(parents=True,exist_ok=True)
 out.write_text(json.dumps(manifest,indent=2,sort_keys=True)+"\n")
 print("B0161_W28_EXACT_824_7_PRODUCTION_BLOBS_UNTOUCHED=PASS")
 print("B0161_W28_FIVE_NEW_QA_ONLY_FILES_ONE_UNIQUE_GUID=PASS")
 print("B0161_W28_THREE_NATIVE_REAL_UNITY_EDITMODE_CASES=AUTHORED_NOT_RUN")
 print("B0161_W28_GAMEPLAY_G1=NOT_RUN_PLAYER=NOT_RUN")
 print("SOURCE_QA_MANIFEST="+json.dumps({"qa_sha":head,"cases":len(methods),"unique_guids":len(unique),"producer_sha":BASE}))
if __name__=="__main__":run()
