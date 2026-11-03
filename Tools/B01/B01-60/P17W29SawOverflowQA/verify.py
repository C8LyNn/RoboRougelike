#!/usr/bin/env python3
"""B01-60 W29: independent exact-author-SHA candidate patch verification.

SOURCE ONLY: does not execute Unity, World/PhysX/Windows Player or a human playtest.
Source diffs, not C# string markers alone, are verified byte-for-byte against original
#828. Complete native G1 requires original Unity Editor logs, NUnit3 XML and process
exit and must report failure if two known default-mount game cases remain red.
"""
import argparse
import json
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
import hashlib

ROOT=Path(__file__).resolve().parents[4]
BASE="887fd18bf0a518ee24776c660f5efa6e3a0491d8"
W27="e03d36f2b86f8c73a067e574b67237a8921f622a"
GAME="Assets/RoboRougelikeMechanics/WPN005RotarySaw/Runtime/RotarySawModule.cs"
NUNIT="Assets/RoboRougelikeMechanics/WPN005RotarySaw/Tests/PlayMode/RotarySawPhysicsTests.cs"
GAME_BASE_BLOB="bb58273eaa756e1df5d98ab64111527c87576656"
NUNIT_BASE_BLOB="661a60faf2d06956bc980f4213b223a46165d466"
OLD="if (count >= overlapBuffer.Length) return true;"
NEW="""if (count >= overlapBuffer.Length)
            {
                // This first owner-safety broadphase is a real collision-query
                // overflow, not an unblocked disc. Preserve the existing
                // observable diagnostic while failing closed before motor,
                // energy, heat or contact damage can advance.
                OverflowedLastTick = true;
                return true;
            }"""
METHOD="OwnerSafetyBroadphaseOverflowReportsAndResumesAfterClearing"
TEST_ANCHOR="        [UnityTest] public IEnumerator RegisteredExternalOwnerColliderIsIgnored()"
TEST_MARKER="        // W29: the FIRST safety broadphase may saturate before the damage scan."
OWN_PATHS={
GAME,NUNIT,
"Tools/B01/B01-60/P17W29SawOverflowQA/verify.py",
"Tools/B01/B01-60/P17W29SawOverflowQA/test_verify.py",
"Tools/B01/B01-60/P17W29SawOverflowQA/Invoke-B0160W29SawNative.ps1",
"Tools/B01/B01-60/P17W29SawOverflowQA/README.md",
"Tools/B01/B01-60/P17W29SawOverflowQA/manifest.json",
".github/workflows/b01-60-p17w29-saw-overflow.yml",
}
def need(x, msg):
 if not x: raise ValueError("B0160_W29_HOLD: "+msg)
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE).decode().strip()
def original(path):
 return subprocess.check_output(["git","-C",str(ROOT),"show",BASE+":"+path]).decode()
def owner_diff():
 need(git("merge-base",BASE,"HEAD")==BASE,"not a strict child of original #828")
 need(git("merge-base",W27,BASE)==W27,"unexpected #828 original Codex W27 ancestry")
 need(git("rev-parse",BASE+":"+GAME)==GAME_BASE_BLOB,"original author runtime blob drift")
 need(git("rev-parse",BASE+":"+NUNIT)==NUNIT_BASE_BLOB,"original author test blob drift")
 entries=[line.split("\t",1) for line in git("diff","--name-status",BASE,"HEAD").splitlines()]
 need(set(path for _,path in entries)==OWN_PATHS,"changed a non-owned or missing file")
 need(sum(status=="M" for status,_ in entries)==2,"only two existing original module/fixture paths may change")
 need({path for status,path in entries if status=="M"}=={GAME,NUNIT},
      "no original game file other than saw module/fixture may change")
 need(all(status=="A" for status,path in entries if path not in {GAME,NUNIT}),
      "all additional B01-60 paths must be newly added")
 return entries

def patched_module():
 before=original(GAME)
 after=(ROOT/GAME).read_text(encoding="utf-8")
 need(before.count(OLD)==1,"original guard line not uniquely identifiable")
 need(after==before.replace(OLD,NEW,1),
      "runtime change must be EXACT one original owner safety branch; no other semantics")
 need(after.count("OverflowedLastTick = true;")==2,
      "both saturated owner broadphase and target scan must publish overflow")
 prefix=after.split("private bool IntersectsOtherOwnedPart()",1)[1].split("private bool automaticSimulation",1)[0]
 need(prefix.index("OverflowedLastTick = true;")<prefix.index("return true;",prefix.index("if (count >=")),
      "overflow must be recorded before early safety return")
 main=after.split("public void Advance(float dt)",1)[1]
 need("OverflowedLastTick = false;" in main and "OwnGeometryBlockedLastTick = ready && motorRequested && IntersectsOtherOwnedPart()" in main,
      "original per-step diagnostics or owner block contract removed")
 need("bool requestUnblocked = motorRequested && !OwnGeometryBlockedLastTick;" in main,
      "original fail-closed motor rule removed")
 return {"original_blob":GAME_BASE_BLOB, "original_count":before.count(OLD),
         "two_scans_flag_overflow":True,"fail_closed_motor_intact":True}

def patched_tests():
 before=original(NUNIT)
 after=(ROOT/NUNIT).read_text(encoding="utf-8")
 need(after.count(TEST_MARKER)==1,"single added native W29 regression block required")
 need(after.count(TEST_ANCHOR)==1,"original WPN005 fixture anchor drift")
 pos=after.index(TEST_MARKER)
 stop=after.index(TEST_ANCHOR,pos)
 inserted=after[pos:stop]
 need(after[:pos]+after[stop:]==before,
      "original WPN005 PlayMode tests MUST be byte-exact unchanged outside added scenario")
 need(len(re.findall(r"\[UnityTest\]\s*public\s+IEnumerator",after))==
      len(re.findall(r"\[UnityTest\]\s*public\s+IEnumerator",before))+1,
      "one additional genuine Unity PlayMode case required")
 for needle in (
  "public IEnumerator "+METHOD+"()",
  "saw.Configure(config)","config.maxOverlapHits = 4",
  "WaitForFixedUpdate","Physics.SyncTransforms()",
  "Assert.IsTrue(saw.OverflowedLastTick",
  "Assert.IsTrue(saw.OwnGeometryBlockedLastTick",
  "Assert.AreEqual(0f, saw.Motor.Rpm",
  "Assert.AreEqual(0f, saw.Motor.Heat",
  "Assert.AreEqual(0, saw.DamageEventsLastTick",
  "candidate.gameObject.SetActive(false)",
  "Assert.IsFalse(saw.OverflowedLastTick",
  "Assert.Greater(saw.Motor.Rpm, 0f",
  "Assert.Greater(enemy.TotalDamage, 0f"
 ):
  need(needle in inserted,"missing real physics regression input/expectation "+needle)
 return {"baseline_playmode_cases":len(re.findall(r"\[UnityTest\]\s*public\s+IEnumerator",before)),
         "new_playmode_cases":len(re.findall(r"\[UnityTest\]\s*public\s+IEnumerator",after)),
         "added_scenario":METHOD}

def source():
 changes=owner_diff()
 module=patched_module()
 nunit=patched_tests()
 return {"package":"B01-60-P17W29-SAW-OVERFLOW-CORRECTION-001/R1",
         "head_sha":git("rev-parse","HEAD"),"original_codex_828_sha":BASE,
         "exact_own_paths":len(changes),"source_test_status":"PASS_EXACT_DIFF_ONLY",
         "runtime":module,"nunit":nunit,"unity_g1":"NOT_RUN",
         "native_default_player_enemy_geometry":"KNOWN_FAIL_UNRESOLVED",
         "windows_player":"NOT_RUN","game_integrated":False,"g2":"HOLD"}

def native_receipt(folder,head):
 path=Path(folder).resolve(strict=True)
 need(path.is_dir() and not path.is_symlink(),"native evidence missing directory")
 need(re.fullmatch(r"[0-9a-f]{40}",head or "") is not None,"invalid expected exact source HEAD")
 files={name:path/name for name in ("W29-saw-playmode.xml","W29-saw-editmode.xml",
      "W29-saw-playmode-Editor.log","W29-saw-editmode-Editor.log",
      "W29-saw-playmode-process.log","W29-saw-editmode-process.log","W29-saw-run.json")}
 for filename,p in files.items():
  need(p.is_file() and not p.is_symlink(),"missing/symlink real native file "+filename)
 receipt=json.loads(files["W29-saw-run.json"].read_text())
 need(receipt.get("head_sha")==head and receipt.get("original_codex_828_sha")==BASE,
      "native receipt wrong exact candidate lineage")
 need(receipt.get("unity_version")=="6000.3.14f1" and receipt.get("native_executed") is True,
      "Unity 6000.3.14f1 was not run")
 need(receipt.get("playmode_exit")==0 and receipt.get("editmode_exit")==0,
      "Unity process exited nonzero")
 for label in ("playmode","editmode"):
  xml=ET.parse(files["W29-saw-"+label+".xml"]).getroot()
  need(xml.tag=="test-run" and xml.get("result")=="Passed","non-green native "+label)
  need(int(xml.get("failed","-1"))==0,"native "+label+" NUnit failures")
  tests=[node for node in xml.iter("test-case")]
  expected=21 if label=="playmode" else 4
  need(int(xml.get("passed","-1"))>=expected and len(tests)>=expected,
       "native "+label+" did not cover entire original module + 2 default R3 cases")
  if label=="playmode":
   need(any(METHOD in t.get("fullname","") and t.get("result")=="Passed" for t in tests),
        "new physical recovery case not actually passed")
  else:
   for case in ("ActualSawCutsOneLogicalPartJamsOnHardWallAndPreservesHeat",
                "WastelandFourEnemyEscortActuallyUsesMeleeApproachAndSawMotor"):
    need(any(case in t.get("fullname","") and t.get("result")=="Passed" for t in tests),
         "original default player or enemy case remains RED "+case)
  logs=files["W29-saw-"+label+"-Editor.log"].read_text(errors="replace")
  need("6000.3.14f1" in logs,"original Editor log missing Unity version")
  proc=files["W29-saw-"+label+"-process.log"].read_text(errors="replace")
  need("exit_code=0" in proc,"original native process exit receipt absent")
  for n in ("W29-saw-"+label+".xml","W29-saw-"+label+"-Editor.log",
            "W29-saw-"+label+"-process.log"):
   need(receipt.get("sha256",{}).get(n)==hashlib.sha256(files[n].read_bytes()).hexdigest(),
        "native evidence tampered "+n)
 return {"native_evidence":"STRUCTURE_PASS_INDEPENDENT_QA_STILL_REQUIRED","unsigned":True,
         "full_saw_suite":"STILL_REQUIRE_INDEPENDENT_REVIEW","windows_player":"NOT_RUN"}

def main():
 p=argparse.ArgumentParser()
 p.add_argument("--native-receipt",default="")
 p.add_argument("--head",default="")
 a=p.parse_args()
 try:
  result=native_receipt(a.native_receipt,a.head) if a.native_receipt else source()
  print(json.dumps(result,sort_keys=True,indent=2))
  return 0
 except Exception as ex:
  print("B0160_W29_SOURCE_OR_NATIVE_RED="+str(ex),file=sys.stderr)
  return 2
if __name__=="__main__":sys.exit(main())
