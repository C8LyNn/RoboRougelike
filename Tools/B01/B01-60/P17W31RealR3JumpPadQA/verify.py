#!/usr/bin/env python3
"""Exact original CURRENT R5+R3 gameplay source and own-only W31 Unity QA coverage.

Source-only checks of production Git identities and genuine Unity test authoring.
Never claim PhysX/NUnit/Windows Player execution from this Python program.
"""
import json
import re
from pathlib import Path
import subprocess
import sys

ROOT=Path(__file__).resolve().parents[4]
BASE="4aab71e35a885f9fc117838a372a7f7e1bb32c65"
GAME="a0de194fd382830fab4b7d013d204b0697b25078"
W29="d537b7310979b91df27a08b14d0f1c74d0889089"
PKG="Tools/B01/B01-60/P17W31RealR3JumpPadQA/"
SOURCE="Assets/Tests/Editor/B0160W31RealR3JumpPadTests.cs"
GUID="8b0160311c21e4389e6538160f2e631e"
TESTS=("SalvageYard_ActualMultiColliderR3VehicleGetsOneRealUpwardImpulse",
"SalvageYard_DisabledRealPadCannotLaunchFullR3Vehicle")
FILES={SOURCE,SOURCE+".meta",PKG+"verify.py",PKG+"test_verify.py",PKG+"manifest.json",PKG+"README.md",".github/workflows/b01-60-p17w31-real-r3-jumppad.yml"}
PINS={"Assets/B01Packages/MapJumpPad001/Runtime/MapJumpPad.cs":"711103bcb682af38b7c0e9bf4c21dea92c8e0547","Assets/R5/R5Arena.cs":"3ee9d05ffdccbd3dea991f80223afeacd91e3b95","Assets/R1/R1RunController.cs":"09a6776db67662e8960d1f446ba5c5168cd3e4b2","Assets/R1/R1VehicleState.cs":"c8042044f25d213e4fcfd7cd89bd0222161b46f1","Assets/R6/R6VehicleDrive.cs":"6e5991cf263ac64b943dc4ac09a546d65ad2fa45","Assets/Pack/P12_PipelineSafeTankController.cs":"8beeedb4db40b7b33c8f685d7b6aab1247a4b2d7","Assets/Tests/Editor/W29LiveArenaJumpPadTests.cs":"a18552c147aaed91ee44823b0667a86addf8be8f","Assets/Tests/Editor/R5ArenaTests.cs":"9a3a4fa3e4f0c21b627b999b593428f1e7fce40f"}
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE).decode().strip()
def need(test,message):
 if not test:raise ValueError("B0160_W31_SOURCE_RED: "+message)
def game(path):return subprocess.check_output(["git","-C",str(ROOT),"show",GAME+":"+path]).decode(errors="replace")
def verify():
 h=git("rev-parse","HEAD")
 need(git("merge-base",BASE,h)==BASE,"not exact descendant of own existing W30 QA")
 need(git("merge-base",GAME,BASE)==GAME,"own W30 parent no longer contains selected real R5 game #877")
 need(git("merge-base",W29,GAME)==W29,"selected cumulative R5 JumpPad game ancestry changed")
 changed={tuple(line.split("\t",1)) for line in git("diff","--name-status",BASE,h).splitlines()}
 need(changed=={("A",p) for p in FILES},"foreign source edits or incomplete W31 seven-own-path package")
 for path,sha in PINS.items():
  need(git("rev-parse","HEAD:"+path)==sha,"original game Git blob has changed: "+path)
 c=(ROOT/SOURCE).read_text(encoding="utf-8")
 meta=(ROOT/(SOURCE+".meta")).read_text(encoding="utf-8")
 need(re.search(r"(?m)^guid: "+GUID+r"$",meta)!=None,"source GUID conflict/ref changed")
 need(c.count("[UnityTest]")==2 and all("IEnumerator "+n+"()" in c for n in TESTS),
      "two new actual Unity test methods absent")
 for token in ("EditorSceneManager.OpenScene(GameplayScene, OpenSceneMode.Single)",
  "yield return new EnterPlayMode()","run.StartRun()","run.ChooseRoute(0)",
  "run.Player.GetComponent<R3VehicleAssembly>()","run.Player.GetComponent<Rigidbody>()",
  "run.Arena.Build(R5ArenaKind.SalvageYard)","run.Player.MoveTo(landing, Quaternion.identity)",
  "p.body.linearVelocity.y","p.body.position.y","p.pad.SetActive(false)",
  "p.pad.CanLaunch(p.body)","p.body.mass","body.isKinematic",
  "GetComponentsInChildren<Collider>","RigidbodyConstraints.FreezePositionY"):
  need(token in c,"actual full vehicle native test not wired: "+token)
 pad=game("Assets/B01Packages/MapJumpPad001/Runtime/MapJumpPad.cs")
 arena=game("Assets/R5/R5Arena.cs")
 ctrl=game("Assets/Pack/P12_PipelineSafeTankController.cs")
 phys=game("Assets/R6/R6VehicleDrive.cs")
 state=game("Assets/R1/R1VehicleState.cs")
 run=game("Assets/R1/R1RunController.cs")
 need("other.attachedRigidbody" in pad and "body.AddForce(velocityChange, ForceMode.VelocityChange)" in pad,
       "real pad must affect attached source Rigidbody via actual velocity change")
 need("if(kind==R5ArenaKind.SalvageYard)JumpPad(" in arena
      and "pad.AddComponent<B01.MapJumpPad001.MapJumpPad>()" in arena,
       "selected REAL R5 arena has no actual physical pad")
 need("rb.constraints =" in ctrl and "RigidbodyConstraints.FreezePositionY" in ctrl,
       "legacy vehicle Y freeze model changed; inspect runtime physically")
 need("body.constraints=RigidbodyConstraints.None" in phys and "body.useGravity=true" in phys,
       "actual R6 physical drive does not establish free vertical gravity")
 need("public void MoveTo(Vector3 position, Quaternion rotation)" in state
      and "body.position = position" in state,
       "real R3 vehicle not moved by real scene code")
 need("run.Arena.Build" not in c or "R5ArenaKind.SalvageYard" in c,"real arena absent")
 need("Arena.Build(CurrentEncounter.Arena)" in run,
       "original game encounter no longer invokes R5 arena")
 receipt={"status":"SOURCE_G0_GITHUB_EXACT_NATIVE_NOT_RUN",
 "head":h,"base_W30":BASE,"actual_game877":GAME,
 "original_real_game_blob_pins":len(PINS),"exact_new_own_adds":len(FILES),
 "authored_real_game_UnityTests":list(TESTS),"real_Unity_6000_3_14f1":"NOT_RUN",
 "real_PhysX":"NOT_RUN","Windows_Player":"NOT_RUN","qa_accepted":False}
 return receipt
if __name__=="__main__":
 try:print(json.dumps(verify(),indent=2,sort_keys=True))
 except Exception as ex:print(str(ex),file=sys.stderr);sys.exit(3)
