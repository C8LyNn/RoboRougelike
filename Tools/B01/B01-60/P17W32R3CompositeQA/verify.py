#!/usr/bin/env python3
"""Source/Git byte acceptance of ONE real #877 game + genuine QA UnityTests.

NEVER calls Unity, PhysX, Windows Player or simulated gameplay.
The product defect is EXPECTED_RED; GitHub success means source witness only.
"""
import hashlib
import json
import pathlib
import re
import subprocess
import sys

ROOT=pathlib.Path(__file__).resolve().parents[4]
MANIFEST=json.loads((pathlib.Path(__file__).parent/"manifest.json").read_text(encoding="utf-8"))
BASE=MANIFEST["game_parent_pr877"]
TEST_PATH="Assets/Tests/Editor/B0160W32R3V7PaidZeroBoundaryTests.cs"

def git(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE).decode().strip()
def get(path):
 return subprocess.check_output(["git","-C",str(ROOT),"show","HEAD:"+path]).decode()
def assert_rule(condition,message):
 if not condition:raise ValueError("B0160_W32_SOURCE_RED: "+message)
def body(source,signature):
 i=source.find(signature)
 assert_rule(i>=0,"method missing: "+signature)
 p=source.find("{",i)
 assert_rule(p>=0,"method block missing "+signature)
 depth=0
 for j in range(p,len(source)):
  if source[j]=="{":depth+=1
  if source[j]=="}":
   depth-=1
   if depth==0:return source[p:j+1]
 raise ValueError("unclosed method: "+signature)
def defect_witness(source):
 quote=body(source,"public int RepairCost(R3PartState part)")
 mounted=body(source,"public bool Repair(Vector3Int cell)")
 inv=body(source,"public bool RepairInventory(string id)")
 bulk=body(source,"public int RepairAffordable()")
 emerg=body(source,"public bool EmergencyRepair()")
 original_wrong_quote=("purchase*0.35f" in quote and
    "Mathf.CeilToInt" in quote and "part.Health<=0" in quote)
 zero_denied=("price <= 0" in mounted and "cost<=0" in inv and
               "RepairCost(p)<=0" in emerg)
 bulk_inherits=("Repair(part.Cell)" in bulk)
 return {"destroyed_quote_old_35ceil_RED":original_wrong_quote,
         "mounted_inventory_emergency_paid0_denied_RED":zero_denied,
         "bulk_inherits_paid0_denial_RED":bulk_inherits}
def verify():
 head=git("rev-parse","HEAD")
 assert_rule(git("rev-parse","HEAD^")==BASE,
     "W32 QA must be ONE direct child of current canonical #877 exact SHA")
 assert_rule(git("merge-base",MANIFEST["real_game_parent_pr873"],BASE)==MANIFEST["real_game_parent_pr873"],
     "selected #873 cumulative game ancestry unavailable")
 diff=[tuple(s.split("\t",1)) for s in git("diff","--name-status",BASE,"HEAD").splitlines()]
 assert_rule(set(diff)=={("A",p) for p in MANIFEST["new_paths_exactly"]} and
   len(diff)==len(MANIFEST["new_paths_exactly"]),"QA-only 13 ADDED paths scope violated")
 for path,expected in MANIFEST["product_pins"].items():
  assert_rule(git("rev-parse","HEAD:"+path)==expected,"original GAME source advanced "+path)
 for source in MANIFEST["legacy_qa_sources"]:
  assert_rule(git("rev-parse","HEAD:"+source["path"])==source["source_git_blob"],
              "imported own/producer original NUnit byte mismatch "+source["path"])
  assert_rule(git("rev-parse","HEAD:"+source["path"]+".meta")==source["meta_git_blob"],
              "original companion .meta changed "+source["path"])
 guids=[re.search(r"(?m)^guid:\s*([a-f0-9]{32})\s*$",get(p))
       for p in MANIFEST["new_paths_exactly"] if p.endswith(".cs.meta")]
 assert_rule(all(x is not None for x in guids),"missing real Unity GUID")
 vals=[x.group(1) for x in guids]
 assert_rule(len(set(vals))==4,"test GUID duplicate in selected new fixture set")
 assert_rule(MANIFEST["own_new_unity_guid"] in vals,"W32 Unity GUID mismatched")
 old_guids=subprocess.check_output(["git","-C",str(ROOT),"grep","-h","-E","^guid:","HEAD^","--","Assets/"],stderr=subprocess.PIPE).decode(errors="replace")
 assert_rule(all(("guid: "+g) not in old_guids for g in vals),
       "collision against actual ORIGINAL game asset GUID")
 new=get(TEST_PATH)
 assert_rule(new.count("[Test]")==5,"exactly 5 authored new direct production R3 NUnit cases")
 for m in (
  "ZeroQuotedMountedWheelRepairsAtNoScrapWithoutReplacingOwnedId",
  "ZeroQuotedBulkRepairActuallyRestoresOneWithoutSpendingScrap",
  "ZeroQuotedEmergencyRepairRecoversRequiredRightWheelAndPreservesIds",
  "ExistingNonzeroPartialHpRepairRateAndOneDebitRemainUnchanged",
  "NonzeroDestroyedRebuildStillRequiresFundsNoScrapOrIdMutationOnDenial"
 ):
  assert_rule("void "+m+"()" in new,"missing actual native case "+m)
 for marker in ("model.Repair(Vector3Int.right)","model.RepairAffordable()",
                "model.EmergencyRepair()","model.EmergencyRepairCost",
                "model.CanDeploy","model.Scrap","model.At(Vector3Int.right).Id",
                "model.RepairCost(part)","R3PartKind.Core","R3PartKind.Wheel"):
  assert_rule(marker in new,"real R3 model not invoked: "+marker)
 imported=[get(x["path"]) for x in MANIFEST["legacy_qa_sources"]]
 assert_rule("yield return new EnterPlayMode()" in imported[0] and
     "run.GarageRepair(" in imported[0] and "run.GarageEmergencyRepair()" in imported[0],
     "genuine original #899 mounted physical repair/game cases not staged")
 assert_rule("run.StartRun()" in imported[1] and
     "R5ArenaKind.SalvageYard" in imported[1] and
     "actualPadContactObserved" in imported[1],
     "genuine original #901 real live physical full R3 pad cases not staged")
 assert_rule("RepairInventory(owned.Id)" in imported[2] and
     "OwnedDestroyedArmorRebuildQuoteMatchesApprovedV7Floor65" in imported[2],
     "original B01-10 #897 V7 producer cases not staged byte-identical")
 witness=defect_witness(get("Assets/R3/R3VehicleModel.cs"))
 assert_rule(all(witness.values()),"expected original real R3 P0 defect no longer matches; reevaluate after owner patch")
 return {"status":"SOURCE_ONLY_EXPECTED_ORIGINAL_R3_PRODUCT_RED",
         "head":head,"canonical_game877_exact":BASE,
         "original_game_blobs_pinned":len(MANIFEST["product_pins"]),
         "imported_original_Unity_test_blob_pairs":len(MANIFEST["legacy_qa_sources"]),
         "new_QA_only_paths":len(diff),
         "authored_genuine_original_Unity_NUnit_cases":14,
         "product_defects":witness,
         "independent_source_code_CI":"PASS_ONLY_IF_ACTUALLY_RUN",
         "real_Unity_Editor_G1":"NOT_RUN","real_PhysX":"NOT_RUN",
         "real_Windows_x64_Player":"NOT_RUN","game_fully_accepted":False}
if __name__=="__main__":
 try:print(json.dumps(verify(),sort_keys=True,indent=2))
 except Exception as e:print(str(e),file=sys.stderr);sys.exit(3)
