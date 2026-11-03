#!/usr/bin/env python3
"""B01-60 W31 positive paid/emergency real-game Unity fixture SOURCE GATE.

Expected green on CI means original live code and four genuine Unity NUnit
sources are present and unchanged. NO Unity Editor, C# compilation, Player
or real physics is run by this Python source verifier.
"""
from pathlib import Path
import re
import subprocess
import sys

ROOT=Path(__file__).resolve().parents[4]
BASE="3bd9b159997924dcecb4e1cd1d41137896e5199e"
GAME="d537b7310979b91df27a08b14d0f1c74d0889089"
ROOT_FIXTURE="Assets/Tests/Editor/B0160W31SafeRestorationGameAcceptanceTests.cs"
META=ROOT_FIXTURE+".meta"
OWN="Tools/B01/B01-60/P17W31SafeRestoration/"
GUID="5ab2cd05bff24318ade73a61d4e64e39"
EXACT_GAME={
 "Assets/R3/R3BuildValidation.cs":"996a2de80039103538700020cca38bc0ede6076a",
 "Assets/R3/R3RestorationValidation.cs":"0d31d7201957d36b637a9be58574ca6c4c5d1dbf",
 "Assets/R3/R3VehicleModel.cs":"1d0f378dc598e08b3d7c16975f419d84231511bb",
 "Assets/R1/R1RunController.cs":"09a6776db67662e8960d1f446ba5c5168cd3e4b2",
 "Assets/Tests/Editor/GarageEditFreedomTests.cs":"6c59d1ab5f58225e44a97b3a7b434a228159649a",
}
EXACT_ADDS={
 ROOT_FIXTURE,META,OWN+"verify_source.py",OWN+"README.md",
 OWN+"manifest.json",".github/workflows/b01-60-p17w31-safe-restoration-source.yml"
}
TESTS=(
 "LiveExteriorRotarySaw_PartialDamagePaidRepairPreservesOperationalGunAndId",
 "LiveDefaultBasicGun_DestroyedEmergencyOrderMustWaitForRealNextVictory",
 "PhysicallyBlockedLegacySaw_BulkRepairEstimateNeverPromisesImpossibleSpend",
 "LegalDefaultBasic_PartialDamageBulkQuoteMatchesActualImmediateRepair"
)
REQUIRED={
 TESTS[0]:(
  'new EnterPlayMode()','SelectVehicle(R3VehicleKind.Standard)',
  'OpenSandboxGarage()','R4WeaponKind.RotarySaw',
  'run.GarageMount(gunId, exterior, default)',
  'destroyed.Health = 0.5f','former.Operational, Is.True',
  'model.RepairCost(destroyed)','model.CanDeploy, Is.True',
  'run.GarageRepair(exterior), Is.True',
  'model.Scrap, Is.EqualTo(wallet - price)',
  'model.Parts.Count, Is.EqualTo(count)',
  'model.Inventory.Count, Is.EqualTo(inventory)',
  'Is.EqualTo(ids)', 'rebuiltGun.Operational, Is.True',
  'model.CanDeploy, Is.True',
  'run.GarageRepair(exterior), Is.False',
 ),
 TESTS[1]:(
  'new EnterPlayMode()','SelectVehicle(R3VehicleKind.Standard)',
  'OpenSandboxGarage()','R4WeaponKind.Basic',
  'gun.Health = 0f','oldMounted.gameObject.SetActive(false)',
  'model.EmergencyRepairCost',
  'run.GarageEmergencyRepair(), Is.True',
  'model.Scrap, Is.EqualTo(before - quote)',
  'Is.EqualTo(beforeIds)',
  'model.CanDeploy, Is.False',
  'run.Player, Is.Not.Null',
  'run.GarageEmergencyRepair(), Is.False',
 ),
 TESTS[2]:(
  'new EnterPlayMode()', 'OpenSandboxGarage()',
  'gun.Weapon = R4WeaponKind.RotarySaw',
  'gun.Health = 0f',
  'oldMounted.gameObject.SetActive(false)',
  'model.RepairCost(gun)',
  'run.GarageRepair(gun.Cell), Is.False',
  'run.GarageRepairAffordable(), Is.False',
  'model.Scrap, Is.EqualTo(before)',
  'gun.Health, Is.Zero',
  'model.Parts.Count, Is.EqualTo(count)',
  'model.Inventory.Count, Is.EqualTo(owned)',
  'Is.EqualTo(ids)',
  'int advertised = model.AffordableRepairCost',
  'Assert.That(advertised, Is.Zero'
 ),
 TESTS[3]:(
  'new EnterPlayMode()', 'OpenSandboxGarage()',
  'gun.Weapon, Is.EqualTo(R4WeaponKind.Basic)',
  'gun.Health = 0.5f',
  'oldMounted.Operational, Is.True',
  'int quote = model.AffordableRepairCost',
  'run.GarageRepairAffordable(), Is.True',
  'before - model.Scrap, Is.EqualTo(quote)',
  'model.Parts.Count, Is.EqualTo(count)',
  'model.Inventory.Count, Is.EqualTo(owned)',
  'Is.EqualTo(ids)',
  'model.CanDeploy, Is.True',
  'rebuilt.Operational, Is.True',
  'run.GarageRepairAffordable(), Is.False',
  'model.AffordableRepairCost, Is.Zero'
 )
}

def git(*args):
 p=subprocess.run(["git","-C",str(ROOT),*args],stdout=subprocess.PIPE,
                 stderr=subprocess.PIPE,text=True)
 if p.returncode:raise AssertionError("git "+str(args)+" "+p.stderr[:300])
 return p.stdout.strip()
def require(ok,reason):
 if not ok:raise AssertionError(reason)
def source_gate(install,restore,model,run):
 require('canonical.SetRestorationGate(parts=>gate!=null&&gate.ValidateRestoration(parts));' in install,
         "Original R3 must bind single canonical restoration gate")
 require('var mountedIssue=ExistingMountedWeaponPlacementIssue(probe,candidateColliders);' in install,
         "Original latest #877 must enforce reverse mounting")
 require('var newWeaponIssue=FunctionalWeaponPlacementIssue(prospective[index],probe);' in restore,
         "Original restoration batch must apply same weapon sweep")
 require('var installedWeaponIssue=ExistingMountedWeaponPlacementIssue(probe,colliders,replacedIds);' in restore,
         "Original restoration must check existing mounted weapons with replaced IDs")
 require('if(volume!=BuildPlacementResult.Valid)' in restore and 'return false;' in restore,
         "Original BUILD001 physical volume fail closed must remain")
 require('if(part.Health<=0&&restorationGate!=null&&!restorationGate(new[]{part}))return false;' in model,
         "Actual paid Repair must reject BEFORE Scrap/HP mutation")
 require('if(restorationGate!=null&&!restorationGate(plan))return false;' in model,
         "Actual EmergencyRepair must reject BEFORE Scrap/HP mutation")
 require('public bool GarageRepair(Vector3Int cell) => EditGarage(()=>VehicleModel.Repair(cell));' in run,
         "Test must call real live R1 game paid repair")
 require('public bool GarageEmergencyRepair() => EditGarage(()=>VehicleModel.EmergencyRepair());' in run,
         "Test must call real live R1 game emergency repair")
 require('RebuildGarageVehicle();return true;' in run,
         "Successful game garage restoration must rebuild true physical vehicle")

def quote_authority_gap_present(model):
 # This is a deliberately expected PRODUCT SOURCE RED witness, not green gameplay.
 # DeepCopy allocates a NEW model and copies data but NOT the callable
 # restorationGate bound by the one original live R3BuildValidation.Bind.
 block=model[model.index("public R3VehicleModel DeepCopy()"):]
 block=block[:block.index("public int Price(")] if "public int Price(" in block else block[:block.index("public bool Repair(")]
 require("var copy=new R3VehicleModel(" in block,
         "Underlying R3 DeepCopy is no longer the original mutable detached model")
 require("copy.restorationGate" not in block and "copy.SetRestorationGate" not in block,
         "Expected original W32 SOURCE RED unsafe detached restoration quote has changed: rerun native verification on fixed version")
 require("get {var preview=DeepCopy();preview.RepairAffordable();return Scrap-preview.Scrap;}" in model,
         "W32 bulk repair quote no longer uses the detached DeepCopy: re-evaluate old W32 source witness")
 require("public bool GarageRepairAffordable()" not in model,
         "Game owner action is in original R1RunController, not second model")

def fixture(source):
 methods=tuple(re.findall(r'\[UnityTest\]\s+public\s+IEnumerator\s+(\w+)\s*\(',source))
 require(methods==TESTS,"Expected EXACT four genuine original game Unity PlayMode tests: "+str(methods))
 for name,lines in REQUIRED.items():
  start=source.index("public IEnumerator "+name+"(")
  end=source.find("    [UnityTest]",start+1)
  body=source[start:end if end>=0 else len(source)]
  for term in lines:
   require(term in body,name+" missing real R3/Save authority/wallet/ID check "+term)
 require(source.count('EditorSceneManager.OpenScene("Assets/scene 1.unity"')==4,
         "All four cases must boot actual R3 gameplay scene independently")
 require(source.count('yield return new ExitPlayMode();')==4,
         "Both real gameplay cases must return from Unity play mode")
 require("using UnityEngine;" in source and
         "using UnityEngine.TestTools;" in source and
         "class UnityEngine" not in source,
         "Must be real Unity framework, not synthetic engine shim")

def main():
 require(git("merge-base",BASE,"HEAD")==BASE,"Isolated W31 QA must descend EXACT current real #877")
 require(git("merge-base",GAME,BASE)==GAME,"W31 must be based on actual cumulative #873 live game")
 changed={}
 for line in git("diff","--name-status",BASE,"HEAD").splitlines():
  status,path=line.split("\t",1)
  changed[path]=status
 require(set(changed)==EXACT_ADDS and all(x=="A" for x in changed.values()),
         "ONLY SIX isolated B01-60 ADD paths, no production game or old tests: "+repr(changed))
 for path,blob in EXACT_GAME.items():
  require(git("rev-parse",BASE+":"+path)==blob and
          git("rev-parse","HEAD:"+path)==blob,
          "Protected original source drift: "+path)
 for path in EXACT_ADDS:
  require(git("hash-object","--",path)==git("rev-parse","HEAD:"+path),
          "Checked out own file differs from committed exact Git blob "+path)
 source_gate(*[(ROOT/x).read_text(encoding="utf-8") for x in (
   "Assets/R3/R3BuildValidation.cs","Assets/R3/R3RestorationValidation.cs",
   "Assets/R3/R3VehicleModel.cs","Assets/R1/R1RunController.cs")])
 quote_authority_gap_present((ROOT/"Assets/R3/R3VehicleModel.cs").read_text(encoding="utf-8"))
 fixture((ROOT/ROOT_FIXTURE).read_text(encoding="utf-8"))
 require((ROOT/META).read_text(encoding="utf-8").strip()==
         "fileFormatVersion: 2\nguid: "+GUID,"Invalid Unity .meta")
 require(git("grep","-l","-i",GUID,"--","*.meta").splitlines()==[META],
         "New Unity GUID duplicate")
 # Actual negative controls on the original source contract, not pretend
 # native tests. Each intentionally disabled repair gate MUST be detected.
 inst,rest,model,run=[(ROOT/x).read_text(encoding="utf-8") for x in
     ("Assets/R3/R3BuildValidation.cs","Assets/R3/R3RestorationValidation.cs",
      "Assets/R3/R3VehicleModel.cs","Assets/R1/R1RunController.cs")]
 bad=[
  (inst,rest.replace("var newWeaponIssue=FunctionalWeaponPlacementIssue(prospective[index],probe);",
                     "var newWeaponIssue=(string)null;"),model,run),
  (inst,rest.replace("var installedWeaponIssue=ExistingMountedWeaponPlacementIssue(probe,colliders,replacedIds);",
                     "var installedWeaponIssue=(string)null;"),model,run),
  (inst,rest,model.replace("if(restorationGate!=null&&!restorationGate(plan))return false;",
                           "/*BAD: gate bypass*/"),run),
 ]
 for variant in bad:
  try:source_gate(*variant)
  except AssertionError:continue
  raise AssertionError("Negative disabled safety contract mistakenly passed")
 print("B0160_W32_CURRENT_R3_DEEPCOPY_DROPS_RESTORATION_GATE_EXPECTED_PRODUCT_SOURCE_RED")
 print("B0160_W31_LATEST_ORIGINAL_877_5_GAME_BLOBS_IMMUTABLE_PASS")
 print("B0160_W32_FOUR_REAL_R3_SAFE_REPAIR_AND_BULK_QUOTE_NATIVE_TESTS_AUTHORED_ONLY")
 print("B0160_W31_SIX_QA_ONLY_ADD_PATHS_AND_NEW_UNITY_GUID_UNIQUE_PASS")
 print("B0160_W31_THREE_DELIBERATE_REPAIR_GATE_MUTANTS_REJECTED_PASS")
 print("B0160_W31_SOURCE_ONLY_PASS_NATIVE_UNITY_G1_AND_WINDOWS_PLAYER_NOT_RUN")
 return 0
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as exc:
  print("B0160_W31_SOURCE_G0_FAIL_CLOSED="+str(exc),file=sys.stderr)
  sys.exit(2)
