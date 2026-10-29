#!/usr/bin/env python3
"""B01-60 W05: INDEPENDENT exact Git-object data & physical stage acceptance of B01-22 v1.0.3.
No changes to game's Assets, any producer file, or Codex worktree. NOT native Unity.
"""
from __future__ import annotations
import hashlib
import importlib.util
import json
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
SRC=Path("Research/B01-22/P17/EQUIPMENT-KITS-001")
OLD="0d212a706c1e633b773d2e03b7b40c28bb63521b"
NEW="a3dd45a4c1f222ec5cc387302eafe9104f91ab52"
CODEX="203a0805251ed28293888e43f2d49f0df63767c6"
OLD_R3_SOURCE="aa7a917f177a9b1f07203868fb7d1d871eb3ee46"
OLD_R3_BLOB="8cc68faf89793c3a803e5807594c9faf0df10091"
NEW_R3_BLOB="1d0f378dc598e08b3d7c16975f419d84231511bb"
R4_BLOB="502f6d7f70d4e8078260de632ab181f244a4d730"
EXPECTED_PRICES={"EK01":110,"EK02":205,"EK03":146,"EK04":175,"EK05":155,
                 "EK06":160,"EK07":195,"EK08":235,"EK09":165,"EK10":176}
ALLOWED_CHANGES={
 ".github/workflows/b01-22-p17-equipment-kits.yml",
 *((SRC/p).as_posix() for p in
   ("CHANGELOG.md","INSTALL_FOR_CODEX.md","README.md","catalog.json",
    "manifest.json","stage_kit_payload.py")),
}
def require(ok,name):
 if not ok:raise AssertionError(name)
def git(*args)->bytes:
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.STDOUT)
def object_at(ref,path)->bytes:
 return git("show",ref+":"+Path(path).as_posix())
def blob_at(ref,path)->str:
 return git("rev-parse",ref+":"+Path(path).as_posix()).decode().strip()
def sha(raw):return hashlib.sha256(raw).hexdigest()
def load(ref,path):return json.loads(object_at(ref,path))
def import_owner_stage():
 path=ROOT/SRC/"stage_kit_payload.py"
 spec=importlib.util.spec_from_file_location("b0122_original_stager",path)
 mod=importlib.util.module_from_spec(spec)
 spec.loader.exec_module(mod)
 return mod
def independent_intake():
 require(git("rev-parse","HEAD").decode().strip()!=OLD,"OLD_BRANCH_NOT_ACCEPTABLE")
 require(git("merge-base","--is-ancestor",OLD,NEW)==b"","NEW_NOT_DESCENDANT")
 paths=set(x.replace("\\","/") for x in git("diff","--name-only",OLD,NEW).decode().splitlines())
 require(paths==ALLOWED_CHANGES,"UNEXPECTED_OWNER_CHANGE "+repr(sorted(paths^ALLOWED_CHANGES)))
 require(not any(p.startswith("Assets/") for p in paths),"CANONICAL_CHANGE")
 original=load(OLD,SRC/"catalog.json")
 catalog=load(NEW,SRC/"catalog.json")
 require(original["version"]=="1.0.0" and catalog["version"]=="1.0.3","CATALOG_DATA_VERSION")
 require(len(catalog["kits"])==10,"KITS_COUNT")
 for attr in ("kits","part_prices","weapon_prices"):
  require(catalog[attr]==original[attr],"ORIGINAL_BALANCE_CHANGED_"+attr)
 require(len(catalog["part_prices"])==17 and len(catalog["weapon_prices"])==9,"PRICE_BOOK_COUNT")
 require([x["id"] for x in catalog["kits"]]==list(EXPECTED_PRICES),"KIT_ID_ORDER")
 prov=catalog["provenance"]
 require(prov=={"codex_pr":383,"codex_sha":CODEX,"r3_blob":NEW_R3_BLOB,
                "r4_blob":R4_BLOB},"LATEST_CODEX_PROVENANCE")
 require(blob_at(CODEX,"Assets/R3/R3VehicleModel.cs")==NEW_R3_BLOB,"CODEX_R3_BLOB")
 require(blob_at(CODEX,"Assets/R4/R4WeaponDefinition.cs")==R4_BLOB,"CODEX_R4_BLOB")
 require(blob_at(OLD_R3_SOURCE,"Assets/R3/R3VehicleModel.cs")==OLD_R3_BLOB,"OLD_CODEX_PIN")
 old_r3=object_at(OLD_R3_SOURCE,"Assets/R3/R3VehicleModel.cs")
 new_r3=object_at(CODEX,"Assets/R3/R3VehicleModel.cs")
 text_old=b"public sealed class R3VehicleModel"
 text_new=b"public sealed partial class R3VehicleModel"
 require(new_r3.count(text_new)==1,"EXPECTED_PARTIAL_CLASS_MISSING")
 require(new_r3.replace(text_new,text_old,1)==old_r3,"OTHER_R3_SEMANTIC_BYTES_CHANGED")
 require(len(new_r3)-len(old_r3)==8,"NOT_EXACTLY_EIGHT_BYTES")
 manifest=load(NEW,SRC/"manifest.json")
 require(manifest.get("schema")=="b01-package-v1" and
         manifest.get("revision")=="1.0.3" and
         manifest.get("previous_frozen_source_sha")==OLD and
         manifest.get("codex_target_sha")==CODEX,"MANIFEST_ANCHOR")
 rows=manifest["files"]
 require(len(rows)==13 and len(set(row["path"] for row in rows))==13,"THIRTEEN_FILES")
 for row in rows:
  p=row["path"]
  require(p.startswith(SRC.as_posix()+"/"),"FILE_OUT_OF_SCOPE")
  require(".." not in Path(p).parts and "\\" not in p,"UNSAFE_PATH")
  raw=object_at(NEW,p)
  require(sha(raw)==row["sha256"] and len(raw)==row["bytes"],"MANIFEST_HASH_"+p)
  # HEAD inherits unchanged original owner source (no copied/modified producer blob in QA).
  require(object_at("HEAD",p)==raw,"QA_PRODUCER_FILE_MUTATED_"+p)
 return catalog,manifest,{"changed_original_files":len(paths),"source_member_hashes":len(rows),
                          "original_kits":len(catalog["kits"]),"part_price_kinds":17,
                          "weapon_price_kinds":9,
                          "r3_difference":"ONLY_8_BYTE_PARTIAL_KEYWORD",
                          "codex_r3_blob":NEW_R3_BLOB,"codex_r4_blob":R4_BLOB}

def test_staging(catalog,manifest):
 mod=import_owner_stage()
 original_root=ROOT/SRC
 real,_=mod.verify_package(original_root)
 require(real==catalog,"ORIGINAL_STAGE_VERIFY")
 negatives=[]
 with tempfile.TemporaryDirectory(prefix="b0160_w05_equipment_") as rawroot:
  t=Path(rawroot)
  dst=t/"real-stage"
  receipt=mod.stage(original_root,dst)
  require(set(x.name for x in dst.iterdir())=={"catalog.json","offers.json","receipt.json"},
          "STAGED_FILE_SET")
  require((dst/"catalog.json").read_bytes()==object_at(NEW,SRC/"catalog.json"),
          "ORIGINAL_CATALOG_STAGE_BYTE_MISMATCH")
  offers=json.loads((dst/"offers.json").read_bytes())
  require(offers["schema"]=="b01-codex-kit-offers-v1"
          and offers["source_codex_sha"]==CODEX
          and offers["data_only"] is True
          and offers["host_transaction_required"] is True
          and offers["has_mount_positions"] is False,"HOST_TRANSACTION_GUARD")
  require(len(offers["offers"])==10,"OUTPUT_OFFERS")
  for i,(kit,offer) in enumerate(zip(catalog["kits"],offers["offers"])):
   require(kit["id"]==offer["id"],"OFFER_ORDER")
   expected_lines=[]
   for p in kit["parts"]:
    kind=p["kind"];weapon=p.get("weapon")
    unit=(catalog["weapon_prices"][weapon] if kind=="Gun" else catalog["part_prices"][kind])
    expected_lines.append({"part_kind":kind,"weapon_kind":weapon,"quantity":p["count"],
                           "reference_unit_scrap":unit,"reference_line_scrap":unit*p["count"]})
   require(offer["parts"]==expected_lines,"PHYSICAL_OFFER_LINES_"+kit["id"])
   total=sum(x["reference_line_scrap"] for x in expected_lines)
   qty=sum(x["quantity"] for x in expected_lines)
   require(total==EXPECTED_PRICES[kit["id"]]==offer["reference_total_scrap"],"OFFER_COST_"+kit["id"])
   require(qty==offer["item_count"] and qty<=12,"OFFER_QUANTITY_"+kit["id"])
  require(receipt.get("modified_host") is False and
          receipt.get("file_sha256")=={
              "catalog.json":sha((dst/"catalog.json").read_bytes()),
              "offers.json":sha((dst/"offers.json").read_bytes())},"HOST_OR_RECEIPT_UNSAFE")
  before=(dst/"offers.json").read_bytes()
  def expect_reject(name,expected,fn):
   try:fn()
   except mod.IntakeError as e:
    require(str(e)==expected,"WRONG_NEGATIVE_REASON_"+name+"="+str(e))
    negatives.append(name)
    return
   raise AssertionError("NEGATIVE_ACCEPTED_"+name)
  expect_reject("existing_stage_no_clobber","DESTINATION_EXISTS",
                lambda:mod.stage(original_root,dst))
  (dst/"offers.json").write_bytes(before+b"corruption")
  expect_reject("tampered_offer_rollback","STAGED_DATA_CHANGED_offers.json",
                lambda:mod.rollback(dst))
  require(dst.is_dir(),"UNSAFE_DESTRUCTIVE_ROLLBACK")
  (dst/"offers.json").write_bytes(before)
  (dst/"foreign.txt").write_text("not owned",encoding="utf-8")
  expect_reject("foreign_file_rollback","STAGE_HAS_UNEXPECTED_FILES",lambda:mod.rollback(dst))
  require((dst/"foreign.txt").exists(),"FOREIGN_FILE_REMOVED")
  (dst/"foreign.txt").unlink()
  receipt_raw=(dst/"receipt.json").read_bytes()
  altered=json.loads(receipt_raw)
  altered["modified_host"]=True
  (dst/"receipt.json").write_text(json.dumps(altered),encoding="utf-8")
  expect_reject("false_modified_host_rollback","RECEIPT_INVALID",lambda:mod.rollback(dst))
  (dst/"receipt.json").write_bytes(receipt_raw)
  require(mod.rollback(dst)=={"status":"ROLLED_BACK","game_modified":False},"ROLLBACK_RETURN")
  require(not dst.exists(),"ROLLBACK_DID_NOT_REMOVE")
  second=t/"second-stage"
  mod.stage(original_root,second)
  require((second/"offers.json").read_bytes()==before,"OFFER_NOT_DETERMINISTIC")
  require(mod.rollback(second)["status"]=="ROLLED_BACK","SECOND_ROLLBACK")
  return {"real_staged_offers":len(offers["offers"]),
          "all_exact_source_prices":list(EXPECTED_PRICES.items()),
          "negative_rejections":negatives,
          "rollback":"COMPLETED_UNMODIFIED_GAME"}

def main():
 if len(sys.argv)!=2:raise RuntimeError("usage: independent_acceptance.py OUTPUT_JSON")
 data,manifest,orig=independent_intake()
 stage=test_staging(data,manifest)
 report={"package":"B01-60-INDEPENDENT-EQUIPMENT-KITS-V103-W05",
         "owner_411_sha":NEW,"parent_333_immutable_sha":OLD,
         "codex_gameplay_sha":CODEX,
         "source_checks":orig,"physical_stage":stage,
         "verdict":"SOURCE_AND_ISOLATED_STAGE_QA_PASS",
         "native_unity":"NOT_RUN","editmode":"NOT_RUN","playmode":"NOT_RUN",
         "codex_ack":"PENDING","hq_offer":"PENDING","integrated":False,"merge":False}
 output=Path(sys.argv[1])
 output.parent.mkdir(parents=True,exist_ok=True)
 output.write_text(json.dumps(report,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
 print(json.dumps(report,ensure_ascii=False))
if __name__=="__main__":
 try:main()
 except Exception as e:
  print("B01-60-W05-INDEPENDENT-QA-FAIL "+repr(e),file=sys.stderr)
  raise
