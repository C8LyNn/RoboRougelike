#!/usr/bin/env python3
"""B01-60 W30: exact-source reverse-garage swept-weapon asymmetry witness.
Only original git byte identity, structural vulnerability and native fixture
registration are tested here: NO Unity engine, PhysX, PlayMode or Player PASS.
"""
from pathlib import Path
import re
import subprocess
import sys

ROOT=Path(__file__).resolve().parents[4]
BASE="9634b1e5b4576aca4304d1d5b4c3d01446737650"
COD="d537b7310979b91df27a08b14d0f1c74d0889089"
FIXTURE="Assets/Tests/Editor/B0160W30ReverseWeaponMountSafetyTests.cs"
META=FIXTURE+".meta"
GUID="86ab4aa55c024b1298b0a750c5145eb3"
PREFIX="Tools/B01/B01-60/P17W30ReverseMount/"
EXPECTED={
 FIXTURE,META,PREFIX+"verify_source.py",PREFIX+"README.md",
 PREFIX+"manifest.json",".github/workflows/b01-60-w30-reverse-weapon-mount-source.yml"
}
ORIGINAL={
 "Assets/R3/R3BuildValidation.cs":"ba247dd539e58d0cf004b8bb7be270f6e9d5c437",
 "Assets/Tests/Editor/GarageEditFreedomTests.cs":"efba13d3be821e75e2c4e1798b87955be2ccd3a3",
 "Assets/R5/R5Arena.cs":"3ee9d05ffdccbd3dea991f80223afeacd91e3b95",
}
IDENTITY="RealGarage_DoesNotAllowNewArmorToEnterAlreadyMountedSawDisc"
REQUIRED=(
 'EditorSceneManager.OpenScene("Assets/scene 1.unity"',
 'new EnterPlayMode()',
 'run.SelectVehicle(R3VehicleKind.Standard)',
 'run.OpenSandboxGarage()',
 'run.GarageRemove(gun.Cell)',
 'owned.Weapon=R4WeaponKind.RotarySaw',
 'run.GarageMount(gunId,safeExteriorSaw,default)',
 'new Vector3Int(1,1,4)',
 'new Vector3Int(1,1,5)',
 'new Vector3Int(0,1,5)',
 'run.GarageBuy(cell,R3PartKind.Armor)',
 'GameObject.CreatePrimitive(PrimitiveType.Cube)',
 'physicalWitness.transform.SetParent(assembly.transform,false)',
 'box.ClosestPoint(discCenter)',
 'RotarySawContactMath.IsOnCuttingDisc(',
 'Assert.That(cutsOwnedArmor,Is.True',
 'physicalWitness.SetActive(false)',
 'Object.Destroy(physicalWitness)',
 'run.VehicleModel.PlacementError(dangerousArmor,R3PartKind.Armor)',
 'issue,Is.Not.Null.And.Contain("Fioletowa strefa")',
 'run.GarageBuy(dangerousArmor,R3PartKind.Armor),Is.False',
 'run.VehicleModel.Scrap,Is.EqualTo(scrapBefore)',
 'run.VehicleModel.Parts.Count,Is.EqualTo(partsBefore)',
 'run.VehicleModel.Inventory.Count,Is.EqualTo(inventoryBefore)',
 'Select(p=>p.Id).OrderBy(id=>id),Is.EqualTo(idsBefore)',
 'new ExitPlayMode()',
)

def git(*args):
 p=subprocess.run(["git","-C",str(ROOT),*args],stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
 if p.returncode:raise AssertionError("git "+str(args)+" failed: "+p.stderr[:350])
 return p.stdout.strip()

def must(ok,message):
 if not ok:raise AssertionError(message)

def gap(source):
 must(source.count("string FunctionalWeaponPlacementIssue(R3PartState candidate,GameObject probe)")==1,
      "canonical weapon sweep authority missing/moved")
 start=source.index("string FunctionalWeaponPlacementIssue(R3PartState candidate,GameObject probe)")
 end=source.index("static string Explain(",start)
 block=source[start:end]
 must("if(candidate.Kind!=R3PartKind.Gun)return null;" in block,
      "Original current-game source no longer has the reverse-install candidate-kind gap")
 must("if(candidate.Weapon==R4WeaponKind.RotarySaw)" in block and
      "RotarySawContactMath.IsOnCuttingDisc" in block,
      "Real original saw rotating disc physical check not found")
 must("if(candidate.Weapon==R4WeaponKind.EmpEmitter)return null;" in block,
      "Area EMP must stay separate from projectile/saw")
 must("return FunctionalWeaponPlacementIssue(candidate,probe);" in source,
      "Current installed R3 gate must call the one canonical authority")
 must("BuildPlacementRules.CheckForInstallation(installed,item" in source,
      "Current BUILD001 collider authority missing")
 must("foreach(var oldPart in assembly.Parts" not in block and
      "foreach(var mounted in assembly.Parts" not in block,
      "This package describes ONLY original W30 #877 gap, not fixed new original")
 return True

def fixture(src):
 meth=re.findall(r"\[UnityTest\]\s+public\s+IEnumerator\s+(\w+)\s*\(",src)
 must(meth==[IDENTITY],"Expected one meaningful genuine Unity PlayMode acceptance scenario: "+repr(meth))
 for fragment in REQUIRED:
  must(fragment in src,"Fixture missing actual original R3/physically grounded reverse-mount assertion: "+fragment)
 must("using FakeUnity" not in src and "class PhysicsShim" not in src,
      "No fake Unity or simulated Physics check allowed")
 return True

def main():
 must(git("merge-base",BASE,"HEAD")==BASE,
      "Independent W30 QA branch must descend from exact actual #877 and no newer guessed branch")
 must(git("merge-base",COD,BASE)==COD,
      "Verified #877 must actually descend from #873 selected cumulative gameplay")
 changed={}
 for line in git("diff","--name-status",BASE,"HEAD").splitlines():
  status,path=line.split("\t",1)
  changed[path]=status
 must(set(changed)==EXPECTED and all(x=="A" for x in changed.values()),
      "B01-60 may add ONLY SIX own QA files, no canonical R3, Codex, Save or producer edits: "+repr(changed))
 for path,blob in ORIGINAL.items():
  must(git("rev-parse",BASE+":"+path)==blob and git("rev-parse","HEAD:"+path)==blob,
       "Exact original Codex file byte drift "+path)
 for path in EXPECTED:
  must(git("hash-object","--",path)==git("rev-parse","HEAD:"+path),
       "Dirty, rewritten or unstaged QA file: "+path)
 src=(ROOT/FIXTURE).read_text(encoding="utf-8")
 fixture(src)
 gap((ROOT/"Assets/R3/R3BuildValidation.cs").read_text(encoding="utf-8"))
 must((ROOT/META).read_text(encoding="utf-8").strip()=="fileFormatVersion: 2\nguid: "+GUID,
      "Invalid native Unity meta")
 matches=git("grep","-l","-i",GUID,"--","*.meta").splitlines()
 must(matches==[META],"Duplicate Unity GUID: "+repr(matches))
 print("B0160_W30_EXACT_877_ORIGINAL_CANONICAL_R3_SOURCE_IMMUTABLE_PASS")
 print("B0160_W30_REVERSE_ARMOR_VS_EXISTING_SAW_BYPASS_PRESENT_EXPECTED_SOURCE_RED")
 print("B0160_W30_ONE_REAL_UNITY_R3_PHYSX_FAILING_FIRST_PLAYMODE_TEST_AUTHORED_NOT_RUN")
 print("B0160_W30_SIX_ADD_ONLY_QA_PATHS_UNIQUE_GUID_PASS")
 print("B0160_W30_SOURCE_ONLY_WITNESS_PASS_UNITY_G1_PLAYER_NOT_RUN_GAME_NOT_ACCEPTED")
 return 0
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as e:
  print("B0160_W30_SOURCE_WITNESS_FAIL_CLOSED="+str(e),file=sys.stderr)
  sys.exit(2)
