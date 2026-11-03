#!/usr/bin/env python3
"""Independent immutable original #877 SOURCE-RED reachability witness.
This is NOT native Unity/PhysX; compile/PlayMode remain NOT_RUN.
"""
import hashlib,json,pathlib,re,subprocess,sys
ROOT=pathlib.Path(__file__).resolve().parents[4]
BASE="9634b1e5b4576aca4304d1d5b4c3d01446737650"
PRODUCT={
 "Assets/R3/R3BuildValidation.cs":"ba247dd539e58d0cf004b8bb7be270f6e9d5c437",
 "Assets/R3/R3RestorationValidation.cs":"d29a83a0f7b1336b5d844ede3c3aee4d64b06194",
 "Assets/R3/R3VehicleModel.cs":"1d0f378dc598e08b3d7c16975f419d84231511bb",
 "Assets/Tests/Editor/GarageEditFreedomTests.cs":"efba13d3be821e75e2c4e1798b87955be2ccd3a3"
}
SOURCE="Assets/Tests/Editor/B0161W30GarageRestoreHardZoneRegressionTests.cs"
META=SOURCE+".meta"
PREFIX="Tools/B01/B01-61/P17W30GarageRestoreHardZoneQA/"
FLOW=".github/workflows/b01-61-w30-877-restore-hardzone-source-negative.yml"
OWN={SOURCE,META,PREFIX+"source_guard.py",PREFIX+"README.md",FLOW}
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT,text=True).strip()
def digest(raw):return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()
def run(out):
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 changes=set(git("diff","--name-only",BASE,"HEAD").splitlines())
 assert changes==OWN,("only five added QA paths",changes)
 for path in changes:
  assert subprocess.run(["git","cat-file","-e",BASE+":"+path],cwd=ROOT,
    stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode!=0,("existing path altered",path)
 for path,expected in PRODUCT.items():
  assert git("rev-parse",BASE+":"+path)==expected,(path,"base blob changed")
  raw=(ROOT/path).read_bytes()
  assert digest(raw)==expected,(path,"producer altered")
 build=(ROOT/"Assets/R3/R3BuildValidation.cs").read_text(encoding="utf8")
 restore=(ROOT/"Assets/R3/R3RestorationValidation.cs").read_text(encoding="utf8")
 model=(ROOT/"Assets/R3/R3VehicleModel.cs").read_text(encoding="utf8")
 method=build.split("string FunctionalWeaponPlacementIssue",1)[1].split("static string Explain",1)[0]
 rst=restore.split("bool ValidateRestoration(",1)[1].split("[System.Diagnostics.Conditional",1)[0]
 assert "return FunctionalWeaponPlacementIssue(candidate,probe);" in build
 assert "candidate.Weapon==R4WeaponKind.RotarySaw" in method
 assert "RotarySawContactMath.IsOnCuttingDisc" in method
 assert "BuildPlacementRules.CheckForInstallation" in build
 assert "BuildVolumeValidator.Check" in rst
 assert "foreach(var part in prospective)probes.Add(CreateProbe(part));" in rst
 # Source gate is not a runtime fail. This precisely detects why destroyed
 # owner guns can bypass the new functional zone when restored via Repair.
 assert "FunctionalWeaponPlacementIssue" not in rst and "IsOnCuttingDisc" not in rst,\
     "Producer appears to have corrected restoration: replace old negative witness with new native QA!"
 repair=model.split("public bool Repair(Vector3Int cell)",1)[1].split("public int RepairAffordable",1)[0]
 emergency=model.split("public bool EmergencyRepair()",1)[1].split("public int EmergencyRepairCost",1)[0]
 assert "restorationGate(new[]{part})" in repair
 assert repair.index("restorationGate(new[]{part})")<repair.index("Scrap -= price")
 assert "restorationGate(plan)" in emergency
 assert emergency.index("restorationGate(plan)")<emergency.index("foreach(var part in plan)part.Health=1")
 assert "SetRestorationGate" in model
 assert "canonical.SetRestorationGate(parts=>gate!=null&&gate.ValidateRestoration(parts));" in build
 suite=(ROOT/SOURCE).read_text(encoding="utf8")
 assert "[UnityTest]" in suite
 assert "vehicle.Repair(originalCell)" in suite
 assert "R4WeaponKind.RotarySaw" in suite
 assert "R1RunController.Instance" in suite
 assert "Is.EqualTo(scrap)" in suite and "Is.EqualTo(originalId)" in suite
 guid=re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$",(ROOT/META).read_text())
 assert guid
 seen={}
 for file in git("ls-files","*.meta").splitlines():
  m=re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$",(ROOT/file).read_text(encoding="utf8",errors="replace"))
  if m:
   if m.group(1) in seen:raise AssertionError(("GUID_DUPLICATE",file,seen[m.group(1)]))
   seen[m.group(1)]=file
 assert seen[guid.group(1)]==META
 evidence={
  "original_codex_877_head":BASE,
  "qa_head":git("rev-parse","HEAD"),
  "unchanged_original_product_git_blobs":PRODUCT,
  "new_own_only_sha256":{p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in sorted(changes)},
  "actual_original_editor_fixture_authored":True,
  "actual_original_editor_fixture_executed":False,
  "source_gate_result":"RED_RESTORATION_MISSING_FUNCTIONAL_WEAPON_HARDZONE",
  "proven_reachable": ["R3VehicleModel.Repair->restorationGate->ValidateRestoration",
                       "R3VehicleModel.EmergencyRepair->restorationGate->ValidateRestoration"],
  "installation_gate_result":"FunctionalWeaponPlacementIssue present on R3BuildValidation.Validate",
  "native_Unity_6000_3_14f1":"NOT_RUN",
  "Windows_Player":"NOT_RUN",
  "product_fix":"NOT_IMPLEMENTED_BY_B01_61_QA",
  "guid":guid.group(1),"guid_count":len(seen),
  "integrated":False,"released":False
 }
 output=pathlib.Path(out);output.parent.mkdir(parents=True,exist_ok=True)
 output.write_text(json.dumps(evidence,sort_keys=True,indent=2)+"\n")
 print("B0161_W30_877_ORIGINAL_MOUNT_FUNCTIONAL_SAW_GATE_PRESENT=PASS")
 print("B0161_W30_877_RESTORATION_REPAIR_AND_EMERGENCY_REPAIR_BYPASS_SOURCE_RED=CONFIRMED")
 print("B0161_W30_877_FOUR_ORIGINAL_GIT_BLOBS_UNMODIFIED=PASS")
 print("B0161_W30_877_FIVE_ADD_ONLY_QA_PATHS_UNIQUE_UNITY_GUID=PASS")
 print("B0161_W30_877_NATIVE_PHYSX_REPRO=NOT_RUN")
if __name__=="__main__":run(sys.argv[1])
