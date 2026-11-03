#!/usr/bin/env python3
"""B01-40 P17 W33: original Git integrator selection G0, no mutation of owners.
PASS means exact-source transport and independently detected PRODUCT RED,
NOT full-game Save/ECON green, native Unity or a Windows Player.
"""
import hashlib,io,json,re,subprocess,tempfile,zipfile,sys
from pathlib import Path
R=Path(__file__).resolve().parents[4]
REF={
"game877":"3bd9b159997924dcecb4e1cd1d41137896e5199e",
"fullgame873":"d537b7310979b91df27a08b14d0f1c74d0889089",
"numeric915":"95060c1d254624355737fd2c48dd87cf79099ebe",
"numeric910":"03d34006a4755b7f1b6d48498acf20fef01925cb",
"writer896":"a0282f2de0250d2a8fe421bbe182b4cb6fab2a11",
"writer885":"78c1d54d24a76c931ea80965a50eca03fae4156a",
"schema907":"64d35ab765064760f1a75035a65b9554ab280d6c",
"rig792":"16cb2a7b5158605b15191b6074f9e392e0d1ebd3",
"economy780":"b4df10a7ad7e6c758f8331013f6b216b3a3689a3",
"admission855":"c9c19665a4f9385e6479f7151097deb797026ade",
"saw861":"d9c5b81ed1f5aeae8f55f0a44ea6e4c455eacb86"
}
MODEL="Assets/R3/R3VehicleModel.cs"
TEST="Assets/Tests/Editor/GarageEditFreedomTests.cs"
WORLDS="Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"
CATALOG="Assets/R3/R3VehicleCatalog.cs"
SLOT="Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs"
SESSION="Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/CanonicalWholeGameSessionV3.cs"
INTEGRATOR="Tools/B01/B01-40/P17W33IntegratorG0/"
WF=".github/workflows/b01-40-w33-exact-source-econ-save-schema-g0.yml"
OWN={INTEGRATOR+"gate.py",INTEGRATOR+"README.md",WF}
def chk(p,code):
 if not p: raise AssertionError("B0140_W33_HOLD_"+code)
def git(*args):
 p=subprocess.run(["git",*args],cwd=R,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 chk(p.returncode==0,"GIT_"+str(args)+":"+p.stderr.decode(errors="replace")[-200:])
 return p.stdout
def raw(ref,path):return git("show",REF[ref]+":"+path)
def txt(ref,path):return raw(ref,path).decode("utf-8-sig")
def dif(refA,refB):
 return {tuple(z.split("\t",1)) for z in git("diff","--name-status",REF[refA],REF[refB]).decode().splitlines()}
def scope():
 chk(git("rev-parse","HEAD").decode().strip() in [*REF.values()] or
     git("merge-base","--is-ancestor",REF["game877"],"HEAD")==b"",
     "NON_CANONICAL_QA_BRANCH_ANCESTRY")
 for k,v in REF.items():
  chk(git("rev-parse",v).decode().strip()==v,"MISSING_SHA_"+k)
 chk(git("merge-base","--is-ancestor",REF["fullgame873"],REF["game877"])==b"","BAD_GAME_ANCESTRY")
 chk(git("merge-base","--is-ancestor",REF["game877"],REF["numeric915"])==b"","BAD_915_ANCESTRY")
 chk(git("merge-base","--is-ancestor",REF["game877"],REF["numeric910"])==b"","BAD_910_ANCESTRY")
 chk(git("merge-base","--is-ancestor",REF["writer885"],REF["writer896"])==b"","SECOND_WRITER_NOT_SUCCESSOR")
 changed=set(git("diff","--name-only",REF["game877"],"HEAD").decode().splitlines())
 chk(changed==OWN,"B0140_EXCLUSIVE_THREE_ADD_PATHS"+str(sorted(changed^OWN)))
 x=dif("game877","numeric915");y=dif("game877","numeric910")
 chk(len(x)==6 and ("M",MODEL) in x,"915_EXACT_SIX_SCOPE")
 chk(len(y)==15 and ("M",MODEL) in y,"910_EXACT_FIFTEEN_SCOPE")
 chk(git("merge-base","--is-ancestor",REF["numeric915"],REF["numeric910"])==b"",
     "910_NOT_DESCENDANT_OF_CLOSED_915")
 chk(len(dif("numeric915","numeric910"))==9 and all(status=="A" for status,_ in dif("numeric915","numeric910")),
     "910_SUPERSEDED_915_EXTRA_NINE_QA_PATHS_NOT_ADD_ONLY")
 chk(raw("game877",MODEL)!=raw("numeric915",MODEL) and
     raw("numeric915",MODEL)==raw("numeric910",MODEL),
     "NEW_910_DID_NOT_PRESERVE_UNMODIFIED_915_PRODUCT_R3")
 chk({p for status,p in x if status=="M"}=={MODEL,TEST},"915_FOREIGN_MODIFIED")
 chk({p for status,p in y if status=="M"}=={MODEL,TEST},"910_FOREIGN_MODIFIED")
 chk(len(dif("writer885","writer896"))==9 and
     {p for s,p in dif("writer885","writer896") if s=="M"}==
     {SLOT,SESSION,".github/workflows/b01-62-w30-single-writer-admission.yml"},
     "SAVE_896_UNEXPECTED_OWNER_DIFF")
 return {"B0140":sorted(changed),"numeric915":sorted(p for s,p in x),
         "numeric910":sorted(p for s,p in y),"save896_delta":9,
         "mutually_exclusive_modified_game_path":MODEL}
def semantic():
 game=txt("game877",MODEL)
 p=txt("numeric915",MODEL)
 alternate=txt("numeric910",MODEL)
 source=txt("writer896",SLOT)
 session=txt("writer896",SESSION)
 contract=txt("writer896",WORLDS)
 catalog=txt("game877",CATALOG)
 one="if(part.Health<=0f)return (int)(((long)purchase*65L)/100L);"
 other="if(part.Health<=0f)return (int)(((long)purchase*65L)/100L);"
 chk(one in p and other in alternate and
     "Mathf.CeilToInt((1-part.Health)*purchase*0.35f)" in p and
     "Scrap -= price; part.Health = 1; return true;" in p,
     "915_NUMERIC_FIX_OR_INSTANT_0HP_SOURCE_MOVED")
 chk("Scrap -= price; part.Health = 1; return true;" in alternate,
     "910_UNEXPECTED_COMPLETED_TEMPORAL_INTEGRATION")
 chk("Mathf.CeilToInt((1-part.Health)*purchase*0.35f)" in game,
     "OLD_GAME_877_NO_LONGER_EXPECTED_RED")
 chk("FrozenPartCatalog.Slots=2" not in contract and
     "public const int Slots=2;" in contract and
     "s.Parts.Length!=FrozenPartCatalog.Slots" in contract and
     "if(count!=FrozenPartCatalog.Slots)return false;" in contract,
     "FROZEN_WORLD_SCHEMA_DRIFT")
 chk("!ColdRestore.TryStage(world,out ColdRestore? staged)" in source and
     "frozenWorld=(byte[])world.Clone();" in source and
     "frozenEcon=econ.Copy();" in source and
     "actualWorld.SequenceEqual(frozenWorld)" in source and
     "SameEconomicSection(actualEconomy,frozenEcon)" in source and
     "frozenWorld=(byte[])world.Clone();" in session,
     "ONE_WRITER_FREEZE_CAS_STAGE_MOVED")
 chk("R3VehicleCatalog" in catalog and "Parts.Add" in catalog,
     "GAME_DYNAMIC_FACTORY_SOURCE_NOT_RECOGNIZED")
 witness=txt("schema907","Tools/B01/B01-62/P17W32R3WorldCompatibility/Program.cs")
 for line in ("ActualGameCatalog(R3VehicleKind.Light,20)",
              "ActualGameCatalog(R3VehicleKind.Standard,83)",
              "ActualGameCatalog(R3VehicleKind.Heavy,139)",
              "OriginalContractCannotEncodeActualGameCount"):
  chk(line in witness,"ORIGINAL_907_REAL_FACTORY_WITNESS_"+line)
 # Three mutation negatives. Under either simulated unsafe edit, the exact
 # defensible original assertions MUST not hold. No author source edited.
 chk(one not in p.replace(one,"return 0;",1),"MUTANT_PRICE_REJECTED")
 chk("part.Health = 1; return true;" not in p.replace(
     "part.Health = 1; return true;","PENDING_BOOKED_ONLY;",1),
     "MUTANT_TEMPORAL_REJECTED")
 chk("public const int Slots=2;" not in contract.replace(
     "public const int Slots=2;","public const int Slots=139;",1),
     "MUTANT_SCHEMA_CAPACITY_REJECTED")
 prices=[1,5,15,30,100]
 price_cases=[{"purchase":v,"approved_paid_quote":v*65//100,
               "initial_game_quote":0 if v==1 else min(v-1,max(1,(35*v+99)//100))}
             for v in prices]
 chk(price_cases==[{"purchase":1,"approved_paid_quote":0,"initial_game_quote":0},
                  {"purchase":5,"approved_paid_quote":3,"initial_game_quote":2},
                  {"purchase":15,"approved_paid_quote":9,"initial_game_quote":6},
                  {"purchase":30,"approved_paid_quote":19,"initial_game_quote":11},
                  {"purchase":100,"approved_paid_quote":65,"initial_game_quote":35}],
     "PRICE_WITNESS_NOT_APPROVED_V7")
 return {
   "numeric_quote_source_915":"CORRECT_PRICE_P1_PAID_ZERO_SOURCE_ONLY",
   "numeric_quote_source_910":"CORRECT_PRICE_LATEST_SUCCESSOR_OF_CLOSED_915_TEST_ONLY_EXTRA",
   "selected_numeric_pr":"#910_LATEST_SOURCE_03d340_WITH_#915_PRODUCTION_BY_BLOBS_NOT_APPROVED_FOR_GAME",
   "direct_0HP_HP1_after_paid_order":"PRODUCT_TEMPORAL_RED_ON_BOTH_915_AND_910",
   "approved_V7_requires":"0HP_PENDING_UNTIL_NEXT_AUTHENTIC_DISTINCT_WIN_ID",
   "R3_actual_part_counts":[20,83,139],
   "existing_896_world_schema":"TWO_FROZEN_TEST_RIG_PARTS",
   "real_full_game_save_compatibility":"PRODUCT_P0_RED_913",
   "one_writer":"EXACT_896_SOURCE_ONLY_UNWIRED",
   "original_price_witness":price_cases,
   "source_negative_controls":3,
   "native_unity":"NOT_RUN","windows_player":"NOT_BUILT"}
def stage(orig):
 with tempfile.TemporaryDirectory(prefix="B0140_W33_STAGE_") as d:
  root=Path(d)
  own=root/"OWN";own.mkdir()
  alien=root/"FOREIGN";(alien/"EMPTY").mkdir(parents=True)
  (alien/"keep").write_bytes(b"owner-other-agent-untouched")
  for k,v in orig.items():
   p=own/k;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(v)
  def valid():
   disk={x.relative_to(own).as_posix():x.read_bytes()
         for x in own.rglob("*") if x.is_file()}
   return disk==orig and not any(x.is_symlink() for x in own.rglob("*")) and not any(
       x.is_dir() and not any(x.iterdir()) for x in own.rglob("*"))
  chk(valid(),"STAGED_EXACT_ORIGINAL_BYTES")
  (own/"FOREIGN_EMPTY").mkdir()
  chk(not valid(),"EMPTY_FOREIGN_NOT_DENIED")
  (own/"FOREIGN_EMPTY").rmdir()
  k=next(iter(orig))
  (own/k).write_bytes(b"DIRTY_OVERWRITE")
  chk(not valid(),"DIRTY_OWNED_NOT_DENIED")
  (own/k).write_bytes(orig[k])
  chk(valid(),"POSITIVE_AFTER_RESTORE")
  for k in orig:(own/k).unlink()
  for p in sorted((p for p in own.rglob("*") if p.is_dir()),
                  key=lambda z:len(z.parts),reverse=True):p.rmdir()
  own.rmdir()
  chk((alien/"EMPTY").is_dir() and (alien/"keep").read_bytes()==b"owner-other-agent-untouched",
      "FOREIGN_DATA_DELETED")
 return "ORIGINAL_BYTES_STAGED_DIRTY_AND_FOREIGN_EMPTY_REJECTED_ROLLBACK_CLEAN"
def zipped(data):
 f=io.BytesIO()
 with zipfile.ZipFile(f,"w",compression=zipfile.ZIP_DEFLATED) as z:
  for k,v in sorted(data.items()):
   info=zipfile.ZipInfo(k,(1980,1,1,0,0,0));info.create_system=3
   info.external_attr=0o100644<<16
   z.writestr(info,v,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
 return f.getvalue()
def main():
 dest=Path(sys.argv[1]);chk(not dest.exists(),"OUT_ALREADY_EXISTS")
 provenance=scope()
 state=semantic()
 selected={
  "REVIEW_ONLY/game877_model.cs":raw("game877",MODEL),
  "REVIEW_ONLY/numeric915_model.cs":raw("numeric915",MODEL),
  "REVIEW_ONLY/numeric910_model.cs":raw("numeric910",MODEL),
  "REVIEW_ONLY/save896_slot.cs":raw("writer896",SLOT),
  "REVIEW_ONLY/save896_session.cs":raw("writer896",SESSION),
  "REVIEW_ONLY/frozen556_two_slot_contract.cs":raw("writer896",WORLDS),
  "REVIEW_ONLY/actual_R3_catalog.cs":raw("game877",CATALOG),
  "REVIEW_ONLY/original_907_contract_CSharp_tests.cs":
   raw("schema907","Tools/B01/B01-62/P17W32R3WorldCompatibility/Program.cs")
 }
 stage_result=stage(selected)
 sums={k:hashlib.sha256(v).hexdigest() for k,v in sorted(selected.items())}
 receipt={"schema":"B01-40-P17-W33-ORIGINAL_INTEGRATOR_G0",
          "refs":REF,"scope":provenance,"evidence":state,
          "original_git_blobs_sha256":sums,"physical_source_rollback":stage_result,
          "conclusion":"SOURCE_PROVENANCE_PASS; PRODUCT_ECON_TEMPORAL_RED; PRODUCT_SCHEMA_913_RED",
          "one_save_writer_only":True,"safe_to_integrate_full_game":False,
          "native_unity":"NOT_RUN","release":False}
 warning=b"REVIEW ONLY. Do not install into Unity. Preserve one #896 writer; versioned R3 schema requires producer+owner approval; old two-slot files cannot be silently migrated.\n"
 rawjson=(json.dumps(receipt,sort_keys=True,indent=2)+"\n").encode()
 package=zipped({"MANIFEST.json":rawjson,"NOT_INSTALLABLE.txt":warning,**selected})
 dest.mkdir()
 (dest/"manifest.json").write_bytes(rawjson)
 (dest/"W33_SOURCE_REVIEW_NOT_INSTALLABLE.zip").write_bytes(package)
 (dest/"sha256.json").write_text(json.dumps({"sha256":hashlib.sha256(package).hexdigest(),
  "entries":len(selected)+2},sort_keys=True)+"\n",encoding="utf-8")
 print("B0140_W33_ORIGINAL_877_915_910_896_SOURCE_SELECTION_G0_PASS")
 print("B0140_W33_SCHEMA_913_REAL_R3_20_83_139_VS_FROZEN_2_PRODUCT_RED_DETECTED")
 print("B0140_W33_V7_0HP_NEXT_AUTHENTIC_WIN_PRODUCT_TEMPORAL_RED_DETECTED")
 print("B0140_W33_SINGLE_FROZEN_896_WRITER_NO_DOUBLE_STORE_VERIFIED")
 print("B0140_W33_SOURCE_ONLY_ZIP_SHA256="+hashlib.sha256(package).hexdigest())
 print("B0140_W33_NATIVE_UNITY_G1_NOT_RUN_AND_PLAYER_NOT_BUILT")
if __name__=="__main__":main()
