#!/usr/bin/env python3
"""B01-60 W30 exact current #877 cumulative-game source and real Unity G1 evidence gate.

Source invariants are NOT Unity physics, a native compilation, extracted Windows
Player, or a real player. Native G1 structurally verifies original Editor/NUnit
results only after real operator invocation; independent human QA remains required.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[4]
BASE="a0de194fd382830fab4b7d013d204b0697b25078"
GAME="d537b7310979b91df27a08b14d0f1c74d0889089"
W27="ed8cf4bcc131688b9a93968a89683d2496586683"
OWN_CS="Assets/Tests/Editor/W30B0160GarageDynamicGateTests.cs"
OWN_META=OWN_CS+".meta"
GUID="ab160030a28b4f99bd96e017b4d36250"
OWN={
 OWN_CS,OWN_META,
 "Tools/B01/B01-60/P17W30LiveGarageAcceptance/verify.py",
 "Tools/B01/B01-60/P17W30LiveGarageAcceptance/test_verify.py",
 "Tools/B01/B01-60/P17W30LiveGarageAcceptance/Invoke-B0160W30Native.ps1",
 "Tools/B01/B01-60/P17W30LiveGarageAcceptance/README.md",
 "Tools/B01/B01-60/P17W30LiveGarageAcceptance/manifest.json",
 ".github/workflows/b01-60-p17w30-live-garage-acceptance.yml",
}
PINS={
 "Assets/R3/R3BuildValidation.cs":"996a2de80039103538700020cca38bc0ede6076a",
 "Assets/R3/R3RestorationValidation.cs":"0d31d7201957d36b637a9be58574ca6c4c5d1dbf",
 "Assets/R3/R3GarageView.cs":"1976a19516905be63259109ec1af0a92ef52d6f4",
 "Assets/R3/R3VehicleModel.cs":"1d0f378dc598e08b3d7c16975f419d84231511bb",
 "Assets/Tests/Editor/GarageEditFreedomTests.cs":"6c59d1ab5f58225e44a97b3a7b434a228159649a",
 "Assets/R5/R5Arena.cs":"3ee9d05ffdccbd3dea991f80223afeacd91e3b95",
 "Assets/Tests/Editor/W29LiveArenaJumpPadTests.cs":"a18552c147aaed91ee44823b0667a86addf8be8f",
 "Assets/Tests/Editor/RRLW6EmpTests.cs":"95778639d0024e248d0053e5b38c20135290aa3d",
 "Assets/RoboRougelikeMechanics/WPN006EMP/Tests/PlayMode/EmpEmitterPhysicsTests.cs":"e4c949da2b6063b94dc9c29cf06e9619c911ba90"
}
NATIVE_CASES=(
 "LiveOwnedBlockerMovedIntoSawDisc_RefusesWithoutScrapOrIdLoss_ThenRecovers",
 "RotateRealSawGhost_UsesSameAuthoritativePlacementGateAndDoesNotMutateOnReject",
)
# FULL original 5 targeted suites, including prior native-verified EMP on a
# NEW combined candidate, not only the green original producer source.
SUITES={
 "garage":("EditMode","GarageEditFreedomTests",13),
 "dynamic":("EditMode","W30B0160GarageDynamicGateTests",2),
 "jump":("EditMode","W29LiveArenaJumpPadTests",2),
 "emp-integration":("EditMode","RRLW6EmpPhysicalTests",12),
 "emp-module":("PlayMode","RoboRougelike.Mechanics.EMP.Tests.EmpEmitterPhysicsTests",22)
}
def need(ok,why):
 if not ok:raise ValueError("B0160_W30_HOLD: "+why)
def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE).decode().strip()

def original(path):
 return subprocess.check_output(["git","-C",str(ROOT),"show",BASE+":"+path]).decode()

def check_source():
 head=git("rev-parse","HEAD")
 need(git("merge-base",BASE,head)==BASE,"not actual cumulative #877 descendant")
 need(git("merge-base",GAME,BASE)==GAME,"#877 not descendant of selected actual #873 game")
 need(git("merge-base",W27,GAME)==W27,"#873 not descendant of actual W27 #824")
 changed=[line.split("\t",1) for line in git("diff","--name-status",BASE,"HEAD").splitlines()]
 need({(status,path) for status,path in changed}=={("A",p) for p in OWN},
      "ONLY eight new independent B01-60 QA files may be added, no game/codex edits")
 for path,blob in PINS.items():
  need(git("rev-parse","HEAD:"+path)==blob,
       "current combined game source Git blob unexpected: "+path)
 code=(ROOT/OWN_CS).read_text(encoding="utf-8")
 meta=(ROOT/OWN_META).read_text(encoding="utf-8")
 need(re.search(r"(?m)^guid: "+GUID+r"$",meta)!=None,"Unity GUID changed")
 need(code.count("[UnityTest]")==2,"exact two new real Unity coroutine tests")
 methods=tuple(re.findall(r"public IEnumerator (\w+)\(\)",code))
 need(all(x in methods for x in NATIVE_CASES),"actual native test case IDs missing")
 need(code.count("yield return StartRealGarage();")==2,
      "must execute current full scene for both cases")
 for marker in (
  "Object.FindFirstObjectByType<R3GarageView>",
  "run.OpenSandboxGarage()",
  "view.Preview.GetComponentsInChildren<Transform>",
  "Physics.SyncTransforms()",
  "own.transform.position = center.position;",
  "view.PlacementIssue","view.PreviewValid","view.ClickTarget()",
  "model.OwnedPlacementError","model.Scrap",
  "model.Inventory.Select(p => p.Id)", "model.Parts.Select(p => p.Id)",
  "view.RotateSelected(1, 1)",
  "model.Parts.Single(p => p.Id == id).Cell"
 ):
  need(marker in code,"native real game regression lacks case "+marker)
 binding=original("Assets/R3/R3BuildValidation.cs")
 restoration=original("Assets/R3/R3RestorationValidation.cs")
 view=original("Assets/R3/R3GarageView.cs")
 model=original("Assets/R3/R3VehicleModel.cs")
 gamearena=original("Assets/R5/R5Arena.cs")
 need("canonical.SetInstallationGate(candidate=>gate!=null?gate.Validate(candidate)" in binding,
      "one original installation gate missing")
 need("return FunctionalWeaponPlacementIssue(candidate,probe);" in binding
      and "Physics.OverlapSphereNonAlloc(muzzle.position,extent" in binding
      and "RotarySawContactMath.IsOnCuttingDisc(" in binding
      and "Physics.SphereCastNonAlloc(" in binding
      and "if(count>=functionalOverlap.Length)" in binding
      and "if(nearCount>=functionalOverlap.Length)" in binding
      and "if(castCount>=functionalCast.Length)" in binding,
      "actual physical disc gate not integrated in #877")
 need("var mountedIssue=ExistingMountedWeaponPlacementIssue(probe,candidateColliders);" in binding
      and "var newWeaponIssue=FunctionalWeaponPlacementIssue(prospective[index],probe);" in restoration
      and "var installedWeaponIssue=ExistingMountedWeaponPlacementIssue(probe,colliders,replacedIds);" in restoration,
      "bidirectional canonical mount/repair physical weapon gate missing")
 garage=original("Assets/Tests/Editor/GarageEditFreedomTests.cs")
 need("NewArmorCannotBlockExistingSawAfterGaragePurchase" in garage
      and "ExistingDeadLegacySawCannotRepairOrEmergencyRepair" in garage,
      "both new original producer physical reverse-mount + repair P0 Unity native cases missing")
 need("BuildPlacementRules.CheckForInstallation(" in binding
      and "Physics.OverlapBox(" in binding,
      "original physical BUILD001 installation validation bypassed")
 need("Owned!=null?run.VehicleModel.OwnedPlacementError" in view
      and "string error=PlacementIssue;if(error!=null){Message=error;return false;}" in view,
      "ghost and transaction no longer share canonical rejection")
 need("Scrap-=Price(kind,weapon)" in model
      and "PlacementError(cell,kind,false,weapon,rotation)!=null)return false" in model,
      "canonical Buy must reject BEFORE charging Scrap")
 need("if(kind==R5ArenaKind.SalvageYard)JumpPad(" in gamearena
      and "pad.AddComponent<B01.MapJumpPad001.MapJumpPad>()" in gamearena,
      "real #873 arena JumpPad not present")
 return {
  "status":"PASS_ORIGINAL_EXACT_HEAD_SOURCE_ONLY",
  "head_sha":head,"parent_game_pr877_sha":BASE,"parent_pr873_sha":GAME,
  "added_only_own_files":len(OWN),"pinned_current_game_blobs":len(PINS),
  "new_real_UnityTest_sources":list(NATIVE_CASES),
  "target_native_suites":{k:{"platform":v[0],"filter":v[1],"expected":v[2]}
                          for k,v in SUITES.items()},
  "source_qa":"PASS_ONLY","unity_g1":"NOT_RUN","player_windows_x64":"NOT_RUN",
  "integrated_new_QA_test_sources_only":True,
  "new_gameplay_runtime_changes":False,
  "selected_saw_861":"EXTERNAL_UNINTEGRATED_NO_CONFLICTING_863",
  "game_qag2":"HOLD"
 }

def native_audit(directory,expected_head):
 need(re.fullmatch(r"[0-9a-f]{40}",expected_head or "")!=None,"invalid source SHA")
 folder=Path(directory).resolve(strict=True)
 need(folder.is_dir() and not folder.is_symlink(),"missing native evidence directory")
 metadata=folder/"W30-native-receipt.json"
 need(metadata.is_file() and not metadata.is_symlink(),"no original receipt file")
 receipt=json.loads(metadata.read_text(encoding="utf-8"))
 need(receipt.get("source_head")==expected_head
      and receipt.get("game877_parent")==BASE
      and receipt.get("game873_parent")==GAME,"wrong original integrated game source")
 need(receipt.get("unity_version")=="6000.3.14f1"
      and receipt.get("native_invoked") is True,"no actual Unity invoked")
 checks={}
 for label,(platform,filter_name,expected) in SUITES.items():
  files={ext:folder/("W30-"+label+ext) for ext in (".xml",".Editor.log",".process.log")}
  for ext,p in files.items():
   need(p.is_file() and not p.is_symlink(),"no real native "+label+ext)
   need(receipt.get("sha256",{}).get(p.name)==hashlib.sha256(p.read_bytes()).hexdigest(),
        "original native evidence hash mismatch "+p.name)
  need(receipt.get("exits",{}).get(label)==0,"nonzero Unity Editor exit "+label)
  root=ET.parse(files[".xml"]).getroot()
  need(root.tag=="test-run" and root.get("result")=="Passed"
       and int(root.get("failed","-1"))==0,
       "real NUnit suite not PASS "+label)
  cases=list(root.iter("test-case"))
  need(int(root.get("passed","-1"))==expected and len(cases)==expected
       and all(c.get("result")=="Passed" for c in cases),
       "real native suite wrong exact count/cases "+label)
  if label=="dynamic":
   need(all(any(n in c.get("fullname","") for c in cases) for n in NATIVE_CASES),
        "both original newly authored physical cases must actually pass")
  if label=="garage":
   need(any("RealGarageRejectsOwnedSawDiscPenetrationBeforeInventoryMutation"
                in c.get("fullname","") for c in cases),
        "old #877 canonical garage guard not tested")
  editor=files[".Editor.log"].read_text(encoding="utf-8",errors="replace")
  proc=files[".process.log"].read_text(encoding="utf-8",errors="replace")
  need("6000.3.14f1" in editor and re.search(r"(?m)^exit_code=0",proc),
       "genuine Unity Editor/process receipt missing")
  checks[label]=len(cases)
 return{"native_xml_log_structure":"PASS_UNATTESTED_PENDING_PEER_QA",
        "source_sha":expected_head,"passed":checks,"total":sum(checks.values()),
        "windows_player":"NOT_RUN","human_playtest":"NOT_RUN",
        "native_receipt_unsigned":True}

def main():
 p=argparse.ArgumentParser()
 p.add_argument("--native-evidence",default="")
 p.add_argument("--source-sha",default="")
 args=p.parse_args()
 try:
  result=native_audit(args.native_evidence,args.source_sha) if args.native_evidence else check_source()
  print(json.dumps(result,indent=2,sort_keys=True))
  return 0
 except Exception as e:
  print("B0160_W30_GATE_RED: "+str(e),file=sys.stderr)
  return 3
if __name__=="__main__":sys.exit(main())
