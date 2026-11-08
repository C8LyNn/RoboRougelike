#!/usr/bin/env python3
"""Independent B01-60 W28 source provenance/real NUnit test registration gate.

Runs on exact original Codex W27 #824 source, not a fake Unity engine.
A PASS proves authored test/source closure only, NEVER native G1 / Windows Player.
"""
from pathlib import Path
import re
import subprocess
import sys

ROOT=Path(__file__).resolve().parents[4]
BASE="e03d36f2b86f8c73a067e574b67237a8921f622a"
FIXTURE="Assets/Tests/Editor/B0160W28RealWeaponPhysicsAcceptanceTests.cs"
META=FIXTURE+".meta"
OWN="Tools/B01/B01-60/P17W28WeaponPhysicsAcceptance/"
NEW_GUID="43e6c0476b0c4c29b5ced44f9b47f560"
ORIGINAL={
 "Assets/R3/R3VehicleAssembly.cs":"ed97dd1b0ee3b77c839615497b249dcaee479bb8",
 "Assets/WeaponIntegration/RRLSawBinding.cs":"a0bc49151fcecde159685f2a0841447606a9d291",
 "Assets/WeaponIntegration/RRLEmpBinding.cs":"6ff22154024dd04ee72043f9766db1840218df03",
 "Assets/WeaponIntegration/RRLHitRouter.cs":"3a17ea9ac31685a2677330bcfd95b8fb5c638d5d",
 "Assets/RoboRougelikeMechanics/WPN005RotarySaw/Runtime/RotarySawModule.cs":None,
 "Assets/RoboRougelikeMechanics/WPN006EMP/Runtime/EmpEmitter.cs":None,
 "Assets/Tests/Editor/W27SpecialWeaponClearanceTests.cs":"5041bbb68bbf10d859bec25dc52ff4356d39e0c4",
 "Assets/Tests/Editor/W27BeamClearanceTests.cs":"39147e59ef43d50514e74875642a9af7833855a6",
}
METHODS=(
 "DynamicOwnedRigidbody_EntersAndLeavesShotgunPath_WithoutSpendingBlockedAmmo",
 "RotarySaw_RotatingAgainstOwnArmor_DoesNotDamageItsPhysicalOwner",
 "EmpArea_OwnModulesStaySafe_ExternalWallOccludes_ThenClearPulseRecovers",
 "TwoWeaponGroups_OwnBlockedShotgunDoesNotSuppressUnblockedBasicGroup",
)
REQUIRED={
 METHODS[0]:("movingBody.MovePosition","WaitForFixedUpdate",
     "assembly.ClearProjectilePath(gun),Is.False","state.Ammo,Is.EqualTo(ammo)"),
 METHODS[1]:("saw.SendMessage(\"FixedUpdate\")","saw.Module.Motor.Rpm",
     "saw.Module.DamageEventsLastTick","core.Damageable.CurrentHp"),
 METHODS[2]:("binding.Module.ConfigurePhysics","receiver.IsDisrupted,Is.False",
     "state.DeviceReadyAt,Is.EqualTo(cooldown)","receiver.IsDisrupted,Is.True"),
 METHODS[3]:("model.SyncWeaponGroups","assembly.FireGroup(false)",
     "assembly.FireGroup(true)","basic.NextFireTime,Is.GreaterThan"),
}

def git(*args):
 p=subprocess.run(["git","-C",str(ROOT),*args],text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 if p.returncode!=0:raise AssertionError("Git original source unavailable: "+" ".join(args)+" "+p.stderr[:170])
 return p.stdout.strip()
def need(condition,why):
 if not condition:raise AssertionError(why)
def verify_fixture(source):
 methods=tuple(re.findall(r"\[UnityTest\]\s+public\s+IEnumerator\s+(\w+)\s*\(",source))
 need(methods==METHODS,"Wrong 4 actual real Unity Test Framework PlayMode identities: "+repr(methods))
 for name,fragments in REQUIRED.items():
  start=source.index("public IEnumerator "+name+"(")
  next_name=source.find("    [UnityTest]",start)
  block=source[start:next_name if next_name>=0 else len(source)]
  for fragment in fragments:
   need(fragment in block,name+" is missing actual original-world physics/resource assertion "+fragment)
 need("EditorSceneManager.NewScene" in source and "new EnterPlayMode()" in source,
      "Tests are not actually exercising Unity native PlayMode")
 need("R3VehicleAssembly.Build(" in source and "GetComponent<R3VehicleAssembly>()" in source,
      "Tests do not use the real Codex R3 game vehicle")
 need("GameObject.CreatePrimitive" in source and "Physics.SyncTransforms()" in source,
      "Tests lack real Unity physical obstacles")
 need("Shader.Find(\"Universal Render Pipeline/Lit\")" in source,"Missing real URP fixture")
 need("fake" not in source.lower() and "stub" not in source.lower(),
      "Independent native tests may not introduce fake game/Unity objects")
 return True

def main():
 need(git("rev-parse","HEAD")!="","No Git source")
 need(git("merge-base",BASE,"HEAD")==BASE,"W28 test-only branch MUST descend from exact actual #824 final W27")
 changes=git("diff","--name-status",BASE,"HEAD").splitlines()
 paths={}
 for l in changes:
  status,p=l.split("\t",1)
  need(status=="A","Game/other owner source was modified, removed, or renamed "+l)
  paths[p]=status
 expected={FIXTURE,META,
           OWN+"verify_source.py",OWN+"manifest.json",
           OWN+"README.md",".github/workflows/b01-60-p17w28-actual-weapons-source.yml"}
 need(set(paths)==expected,"W28 must be SIX isolated B01-60 add-only paths, got "+repr(set(paths)^expected))
 for path,want in ORIGINAL.items():
  before=git("rev-parse",BASE+":"+path)
  after=git("rev-parse","HEAD:"+path)
  need(before==after and (want is None or after==want),
       "Original frozen Codex gameplay source differs from exact #824 "+path)
 src=(ROOT/FIXTURE).read_text(encoding="utf-8")
 need(git("hash-object","--",FIXTURE)==git("rev-parse","HEAD:"+FIXTURE),
      "Dirty original authored NUnit fixture")
 verify_fixture(src)
 meta=(ROOT/META).read_text(encoding="utf-8").strip()
 need(meta=="fileFormatVersion: 2\nguid: "+NEW_GUID,"Wrong meta GUID")
 need(git("hash-object","--",META)==git("rev-parse","HEAD:"+META),"Dirty Unity .meta")
 existing=git("grep","-l","-i",NEW_GUID,"--","*.meta").splitlines()
 need(existing==[META],"New Unity GUID collides with another .meta")
 print("B0160_W28_ACTUAL_824_AUTHOR_GIT_CODE_8_PATHS_IMMUTABLE_PASS")
 print("B0160_W28_FOUR_ORIGINAL_REAL_R3_UNITY_PLAYMODE_TEST_IDENTITIES_REGISTERED")
 print("B0160_W28_ONLY_SIX_ADD_ONLY_QA_PATHS_AND_UNIQUE_GUID_PASS")
 print("B0160_W28_SOURCE_ONLY_PASS_UNITY_AND_WINDOWS_PLAYER_NOT_RUN")
 return 0

if __name__=="__main__":
 try:sys.exit(main())
 except Exception as exc:
  print("B0160_W28_FAIL_CLOSED="+str(exc),file=sys.stderr)
  sys.exit(2)
