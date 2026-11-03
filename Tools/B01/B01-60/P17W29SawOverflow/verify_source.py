#!/usr/bin/env python3
"""B01-60 W29 exact original #828 owner-safety broadphase diagnosis guard.

This is ONLY a source authenticity and regression source validator.
It does not execute Unity, its real PhysX, any C# gameplay, or Windows Player.
"""
import argparse
from pathlib import Path
import re
import subprocess
import sys

ROOT=Path(__file__).resolve().parents[4]
BASE="887fd18bf0a518ee24776c660f5efa6e3a0491d8"
GAME="Assets/RoboRougelikeMechanics/WPN005RotarySaw/Runtime/RotarySawModule.cs"
TEST="Assets/RoboRougelikeMechanics/WPN005RotarySaw/Tests/PlayMode/B0160W29SawOwnerQueryOverflowTests.cs"
META=TEST+".meta"
OWN="Tools/B01/B01-60/P17W29SawOverflow/"
GUID="9ef607249c374b9c91d30f443a3d9b10"
BASE_RUNTIME_BLOB="bb58273eaa756e1df5d98ab64111527c87576656"
AFTER_RUNTIME_BLOB="06715ac2dc18f8a8c926895d40d0a54d7c3c91ca"
BASE_IMMUTABLE={
 "Assets/RoboRougelikeMechanics/WPN005RotarySaw/Tests/PlayMode/RotarySawPhysicsTests.cs",
 "Assets/WeaponIntegration/RRLSawBinding.cs",
 "Assets/R4_1/R41PartGeometry.cs",
 "Assets/R3/R3VehicleAssembly.cs",
}
BEFORE="if (count >= overlapBuffer.Length) return true;"
AFTER="""if (count >= overlapBuffer.Length)
            {
                // Owner-clearance overflow is a real broadphase overflow too.
                // Stop before energy/heat/damage AND retain the public diagnostic.
                OverflowedLastTick = true;
                return true;
            }"""
METHOD="SaturatedOwnerSafetyBroadphase_SetsOverflowAndStopsResources_ThenClearedMotorCuts"
EXPECTED={
 GAME:"M",TEST:"A",META:"A",
 OWN+"verify_source.py":"A",
 OWN+"README.md":"A",
 OWN+"manifest.json":"A",
 ".github/workflows/b01-60-w29-saw828-overflow-source.yml":"A",
}

def g(*args):
 p=subprocess.run(["git","-C",str(ROOT),*args],stdout=subprocess.PIPE,
                  stderr=subprocess.PIPE,text=True)
 if p.returncode:raise AssertionError("git "+str(args)+": "+p.stderr[:300])
 return p.stdout.strip()
def need(ok,why):
 if not ok:raise AssertionError(why)
def exact_runtime(old,now):
 need(old.count(BEFORE)==1,"Expected #828 original source owner-overlap early guard exactly once")
 need(old.replace(BEFORE,AFTER)==now,
      "Gameplay producer source delta must be EXACTLY the one original 828 overflow-flag fix")
 # Both branches protect complete saturation, before heat/energy/damage, no
 # global policy change, do not remove original lower downstream check.
 need(now.count("OverflowedLastTick = true;")==2,
      "Missing owner broadphase or enemy-contact broadphase overflow receipts")
 start=now.index("private bool IntersectsOtherOwnedPart()")
 end=now.index("private bool automaticSimulation",start)
 scope=now[start:end]
 need("if (count >= overlapBuffer.Length)" in scope and
      scope.index("OverflowedLastTick = true;")<scope.index("return true;",scope.index("if (count >= overlapBuffer.Length)")),
      "Diagnostic MUST be stamped BEFORE rejecting the unknown owner contact query")
 return True

def check_test(source):
 methods=re.findall(r"\[UnityTest\]\s*public\s*IEnumerator\s*(\w+)\s*\(",source)
 need(methods==[METHOD],"Must contain exactly ONE authentic #828 owner overflow native Unity case: "+str(methods))
 for term in (
  "owner.AddComponent<Rigidbody>()",
  "block.AddComponent<BoxCollider>()",
  "var saw=rotor.AddComponent<RotarySawModule>()",
  "saw.SetAutomaticSimulation(false)",
  "saw.SetMotorRequested(true)",
  "Physics.SyncTransforms()",
  "saw.Advance(.02f)",
  "maxOverlapHits=4",
  "i<5",
  "saw.OwnGeometryBlockedLastTick,Is.True",
  "saw.OverflowedLastTick,Is.True",
  "saw.DamageEventsLastTick,Is.Zero",
  "saw.Motor.Rpm,Is.Zero",
  "saw.Motor.Heat,Is.Zero",
  "target.TotalDamage,Is.Zero",
  "targets[i].gameObject.SetActive(false)",
  "saw.OverflowedLastTick,Is.False",
  "saw.Motor.Rpm,Is.GreaterThan(0f)",
  "targets[4].TotalDamage,Is.GreaterThan(0f)",
 ):
  need(term in source,"Missing original REAL Unity physics/energy/heat/damage test assertion: "+term)
 need(source.count("new UnityTest")==0 and
      "FakeUnity" not in source and "UnityEngineShim" not in source,
      "No synthetic Unity runtime proof allowed")
 return True

def run():
 need(g("merge-base",BASE,"HEAD")==BASE,"Exact #828 gameplay candidate must be ancestor")
 changed={}
 for line in g("diff","--name-status",BASE,"HEAD").splitlines():
  status,path=line.split("\t",1)
  changed[path]=status
 need(changed==EXPECTED,"W29 diff must be EXACT one touched original owner safety line plus six own QA additions: "+str(changed))
 need(g("rev-parse",BASE+":"+GAME)==BASE_RUNTIME_BLOB,"Original owner #828 physical source SHA drift")
 need(g("rev-parse","HEAD:"+GAME)==AFTER_RUNTIME_BLOB,"Our new one-line original runtime file SHA drift")
 old=g("show",BASE+":"+GAME); now=(ROOT/GAME).read_text(encoding="utf-8")
 exact_runtime(old,now)
 for path in BASE_IMMUTABLE:
  need(g("rev-parse",BASE+":"+path)==g("rev-parse","HEAD:"+path),
       "Other original #828/Codex gameplay source modified: "+path)
 for path in EXPECTED:
  need(g("hash-object","--",path)==g("rev-parse","HEAD:"+path),
       "Local checked out source differs from exact committed Git file: "+path)
 source=(ROOT/TEST).read_text(encoding="utf-8")
 check_test(source)
 need((ROOT/META).read_text(encoding="utf-8").strip()==
      "fileFormatVersion: 2\nguid: "+GUID,"Invalid unique Unity .meta GUID")
 occur=g("grep","-l","-i",GUID,"--","*.meta").splitlines()
 need(occur==[META],"New genuine Unity GUID collides with existing Unity source: "+str(occur))
 # Prove the source gate is sensitive to the original failing version and
 # to a fake diagnostic flag placed AFTER returning.
 for mutated in (
  old,now.replace("OverflowedLastTick = true;\n                return true;",
                  "return true;\n                OverflowedLastTick = true;",1),
  now.replace("OverflowedLastTick = true;\n                return true;",
                  "return true;",1),
 ):
  try:exact_runtime(old,mutated)
  except AssertionError:continue
  raise AssertionError("Non-regression or unreachable-flag mutant unexpectedly accepted")
 print("B0160_W29_REAL_ORIGINAL_828_OVERFLOW_SOURCE_ONE_FIX_PASS")
 print("B0160_W29_EXISTING_R3_SAW_FIXTURE_AND_6_GIT_PATHS_UNMODIFIED_PASS")
 print("B0160_W29_ACTUAL_UNITY_PHYSX_OVERFLOW_1_NATIVE_TEST_AUTHORED_ONLY_PASS")
 print("B0160_W29_THREE_ACTUAL_FAIL_CLOSED_SOURCE_MUTANTS_REJECTED_PASS")
 print("B0160_W29_SOURCE_ONLY_PASS_UNITY_G1_AND_WINDOWS_PLAYER_NOT_RUN")
 return 0

if __name__=="__main__":
 try:sys.exit(run())
 except Exception as exc:
  print("B0160_W29_SOURCE_FAIL_CLOSED="+str(exc),file=sys.stderr)
  sys.exit(2)
