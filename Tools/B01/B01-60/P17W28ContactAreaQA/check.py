#!/usr/bin/env python3
"""B01-60 P17/W28: real Codex W27 source QA and independent Unity receipt gate.

Source tests are source tests. The optional native gate requires authentic local
Unity Editor.log, NUnit3 XML and exact SHA pinned to the *invoked* checkout.
Self-hashed evidence alone is not independently authenticated.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[4]
W27="e03d36f2b86f8c73a067e574b67237a8921f622a"
W27_BODY={
 "Assets/R3/R3VehicleAssembly.cs":"ed97dd1b0ee3b77c839615497b249dcaee479bb8",
 "Assets/WeaponIntegration/RRLSawBinding.cs":"a0bc49151fcecde159685f2a0841447606a9d291",
 "Assets/WeaponIntegration/RRLEmpBinding.cs":"6ff22154024dd04ee72043f9766db1840218df03",
 "Assets/RoboRougelikeMechanics/WPN005RotarySaw/Runtime/RotarySawModule.cs":"a79a81280d2ebd3a6f68c26619f7dda6335a5b5f",
 "Assets/RoboRougelikeMechanics/WPN006EMP/Runtime/EmpEmitter.cs":"f38da021c22de609a196876d508ec9d92fb9ff56",
 "Assets/Tests/Editor/W27BeamClearanceTests.cs":"39147e59ef43d50514e74875642a9af7833855a6",
 "Assets/Tests/Editor/W27SpecialWeaponClearanceTests.cs":"5041bbb68bbf10d859bec25dc52ff4356d39e0c4",
 "Assets/R1/W27WeaponPlayerQA.cs":"1f9023317f49488eb5ebbad93fb079d6b0d3b024",
}
NEW="Assets/Tests/Editor/W28B0160ContactAreaClearanceTests.cs"
META=NEW+".meta"
GUID="77b16028b34f46af9d92f225a51c902e"
SUITE="W28B0160ContactAreaClearanceTests"
CASES=(
 "RotarySaw_DynamicallyAttachedOwnerColliderNeverBecomesContactVictim",
 "Emp_OwnElectronicsNeverDisrupt_EnemyBehindWallDoesNotDisrupt_ButPulseCostsCooldown"
)
OWN=(
 NEW,META,
 "Tools/B01/B01-60/P17W28ContactAreaQA/check.py",
 "Tools/B01/B01-60/P17W28ContactAreaQA/test_check.py",
 "Tools/B01/B01-60/P17W28ContactAreaQA/Invoke-B0160W28Native.ps1",
 "Tools/B01/B01-60/P17W28ContactAreaQA/README.md",
 "Tools/B01/B01-60/P17W28ContactAreaQA/manifest.json",
 ".github/workflows/b01-60-p17w28-contact-area-native.yml",
)
def need(ok,why):
 if not ok:raise ValueError("B0160_W28_HOLD: "+why)

def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE).decode().strip()

def source_check():
 need(git("merge-base",W27,"HEAD")==W27,"must descend exact original Codex #824")
 changes=[s.split("\t") for s in git("diff","--name-status",W27,"HEAD").splitlines()]
 need(set(tuple(x) for x in changes)=={("A",p) for p in OWN},
      "only eight OWN QA files can be added; no Codex gameplay changes")
 for path,sha in W27_BODY.items():
  need(git("rev-parse","HEAD:"+path)==sha,
       "original Codex original source/fixture unexpectedly changed "+path)
 code=(ROOT/NEW).read_text(encoding="utf-8")
 meta=(ROOT/META).read_text(encoding="utf-8")
 need(re.search(r"(?m)^guid: "+GUID+r"$",meta) is not None,"Unity meta GUID mismatch")
 need(code.count("[UnityTest]")==2,"two real authored Editor PlayMode UnityTests required")
 names=re.findall(r"public\s+IEnumerator\s+([A-Za-z0-9_]+)\s*\(",code)
 need(tuple(x for x in names if x in CASES)==CASES,"original W28 authored test identities drifted")
 saw=git("show",W27+":Assets/WeaponIntegration/RRLSawBinding.cs")
 emp=git("show",W27+":Assets/WeaponIntegration/RRLEmpBinding.cs")
 phys=git("show",W27+":Assets/RoboRougelikeMechanics/WPN006EMP/Runtime/EmpEmitter.cs")
 assembly=git("show",W27+":Assets/R3/R3VehicleAssembly.cs")
 need("TryResolve(Collider collider,out SawTarget target)" in saw
      and "RRLHitRouter.CanHit(collider,Device.Owner)" in saw
      and "Module.Advance(Time.fixedDeltaTime)" in saw,
      "actual contact saw route missing")
 need("public bool Fire()" in emp and "Module.TryPulse(out var report)" in emp
      and "public void FireGroup(bool secondary)" in assembly,
      "actual area or group fire route missing")
 need("Physics.OverlapSphereNonAlloc" in phys
      and "Physics.RaycastNonAlloc" in phys
      and "IsFilteredByOwnership" in phys
      and "cooldownTimer.Commit(cooldown)" in phys,
      "actual EMP physical/ownership/cooldown pipeline missing")
 for token in (
  "MakeGun(R4WeaponKind.RotarySaw", "MakeGun(R4WeaponKind.EmpEmitter",
  "saw.TryResolve(collider, out _)", "saw.SetMotorRequested(true)",
  "Physics.SyncTransforms()", "wall.SetActive(false)",
  "emp.Fire()", "assembly.FireGroup(false)",
  "ownReceiver.IsDisrupted", "enemyReceiver.IsDisrupted",
  "canonical.DeviceReadyAt", "yield return new WaitForFixedUpdate()"
 ):
  need(token in code,"W28 source missing own source-native AC "+token)
 return {"source":"PASS_PROVENANCE_AND_AUTHORED_TESTS_ONLY",
         "head_sha":git("rev-parse","HEAD"),"codex_w27_sha":W27,
         "original_codex_blobs":W27_BODY,"source_files_added":len(OWN),
         "unity_test_methods":list(CASES),
         "unity_editmode":"NOT_RUN","unity_playmode":"NOT_RUN",
         "windows_player":"NOT_RUN","game_integrated":False}

def native_audit(folder,expected_head):
 root=Path(folder).resolve(strict=True)
 need(root.is_dir() and not root.is_symlink(),"evidence not real directory")
 need(re.fullmatch("[0-9a-f]{40}",expected_head) is not None,"invalid source SHA")
 xmlpath=root/"W28-contact-area-results.xml"
 logpath=root/"W28-Editor.log"
 process=root/"W28-process.log"
 meta=root/"W28-run.json"
 for p in (xmlpath,logpath,process,meta):
  need(p.is_file() and not p.is_symlink(),"missing or linked original Unity evidence "+p.name)
 receipt=json.loads(meta.read_text(encoding="utf-8"))
 need(receipt.get("head_sha")==expected_head,"receipt SHA vs invoked source mismatch")
 need(receipt.get("original_codex_w27_sha")==W27,"wrong original W27 lineage")
 need(receipt.get("unity_version")=="6000.3.14f1","wrong verified Unity Editor version")
 need(receipt.get("native_invoked") is True,"native never invoked")
 need(receipt.get("exit_code")==0,"Unity test exit not zero")
 need(receipt.get("test_filter")==SUITE,"wrong original test-filter")
 stdout=process.read_text(errors="replace")
 need(re.search(r"(?m)^exit_code=0$",stdout)!=None,"original Unity process exit missing")
 log=logpath.read_text(errors="replace")
 need("6000.3.14f1" in log,"no original Editor version marker")
 xml=ET.parse(xmlpath).getroot()
 need(xml.tag=="test-run" and xml.get("result")=="Passed","NUnit3 root not passed")
 need(int(xml.get("failed","-1"))==0 and int(xml.get("passed","-1"))==2,
      "exact two real authored Unity cases must PASS")
 tests=list(xml.iter("test-case"))
 need(len(tests)==2,"missing, extra or duplicate NUnit test-case")
 names={t.get("fullname","") for t in tests if t.get("result")=="Passed"}
 need(names=={SUITE+"."+n for n in CASES},
      "NUnit3 wrong actual two tests/identities")
 for p in (xmlpath,logpath,process):
  h=hashlib.sha256(p.read_bytes()).hexdigest()
  need(receipt.get("sha256",{}).get(p.name)==h,
       "original byte-integrity mismatch for "+p.name)
 return {"native_evidence":"STRUCTURALLY_VALID_REQUIRES_INDEPENDENT_HUMAN_CONFIRMATION",
         "source_sha":expected_head,"actual_original_nunit_cases":len(tests),
         "unity_version":"6000.3.14f1","unsigned_receipt":True}

def main():
 p=argparse.ArgumentParser()
 p.add_argument("--audit-native",default="")
 p.add_argument("--expected-head",default="")
 a=p.parse_args()
 try:
  if a.audit_native:
   print(json.dumps(native_audit(a.audit_native,a.expected_head),indent=2))
  else:print(json.dumps(source_check(),indent=2))
 except Exception as e:
  print("B0160_W28_GATE_RED="+str(e),file=sys.stderr);return 3
 return 0

if __name__=="__main__":sys.exit(main())
