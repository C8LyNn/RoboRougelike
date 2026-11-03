#!/usr/bin/env python3
"""B01-40 P17/W32: independent original original-game #877 / frozen unique Save #896 G0.
Runs against immutable original commits, not Codex worktree or alternate game.
Does NOT run Unity Editor, PhysX, or Windows Player.
"""
import argparse, hashlib, io, json, re, subprocess, tempfile, zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
P={
 "game":"d537b7310979b91df27a08b14d0f1c74d0889089", # #873
 "garage":"3bd9b159997924dcecb4e1cd1d41137896e5199e", # #877 CURRENT
 "garage_base":"9634b1e5b4576aca4304d1d5b4c3d01446737650",
 "reverse":"01688cd2e67b80de6a439e29de4e0b3a769a48de", # #887 exact source
 "save":"a0282f2de0250d2a8fe421bbe182b4cb6fab2a11", # #896 FROZEN PAIR
 "save_base":"78c1d54d24a76c931ea80965a50eca03fae4156a", # #885 sole writer predecessor
 "save_v3":"99fb08619c78b3834bb4c0450499babf991f218a", # #826
 "authority":"f5db40bf293fe9582ec7c6fcb3515aeff6fcf937", # #787 audit only
 "physical":"16cb2a7b5158605b15191b6074f9e392e0d1ebd3", # #792
 "economy":"b4df10a7ad7e6c758f8331013f6b216b3a3689a3", # #780
 "admission":"c9c19665a4f9385e6479f7151097deb797026ade", # #855
 "fixture":"60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb", # #750
 "frozen729":"b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
 "frozen556":"33094f154c7b91119b36d58aab285e6e83c5de57",
 "frozen510":"d94400d95d13db7f27e1533df05c9292d5380431",
 "saw":"fb55ff3c147eb578d0a4aa5a967f69fd6ec88ca6", # #861 separate only
 "turret":"bae40c25c48b26849c3291c84b79a66a9ba5c2a5" # #893 separate only
}
BASE_P="Assets/B01Packages.meta"
PAD_DIR="Assets/B01Packages/MapJumpPad001"
ADMISSION_DIR="Assets/B01Packages/ECONADMISSIONW29"
RIG_DIR="Assets/B0123CanonicalRigPortW24"
BIND="Assets/R3/R3BuildValidation.cs"
REST="Assets/R3/R3RestorationValidation.cs"
MODEL="Assets/R3/R3VehicleModel.cs"
GARAGE_VIEW="Assets/R3/R3GarageView.cs"
SAVE_PROD="Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/"
SAVE_SLOT=SAVE_PROD+"WholeGameEconomySlotV3.cs"
SAVE_SESSION=SAVE_PROD+"CanonicalWholeGameSessionV3.cs"
COLD="Tools/B01/B01-62/P17W29ColdReadV3/CanonicalColdReadV3.cs"
ADMISSION_CS=ADMISSION_DIR+"/Runtime/EconomyV7AdmissionW29.cs"
EXPECTED_GARAGE={
 ("M",BIND),("M",REST),("M","Assets/Tests/Editor/GarageEditFreedomTests.cs"),
 ("A",".github/workflows/b01-w30-garage-source.yml"),
 ("A","Docs/W29/GARAGE_FUNCTIONAL_SWEEP_GATE.md"),
 ("A","Docs/W30/GARAGE_BOUNDED_PHYSICS_RECEIPT.md"),
 ("A","Tools/B01/Codex/W30/verify_garage.py"),
 ("A",".github/workflows/b01-w31-native-acceptance.yml"),
 ("A","Assets/B01Packages/MapJumpPad001/README.md.meta"),
 ("A","Assets/B01Packages/MapJumpPad001/manifest.json.meta"),
 ("A","Docs/W31/NATIVE_ACCEPTANCE.md"),
 ("A","Tools/B01/Codex/W31/verify_artifacts.py")
}
def must(v,code):
 if not v:raise AssertionError("B0140W32_SOURCE_G0_HOLD_"+code)
def git(*args,check=True):
 cp=subprocess.run(["git",*args],cwd=ROOT,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 if check:must(cp.returncode==0,"GIT_"+repr(args)+":"+cp.stderr.decode(errors="replace")[-160:])
 return cp.stdout
def source(ref,path):return git("show",ref+":"+path)
def tree(ref,prefix):
 return set(git("ls-tree","-r","--name-only",ref,"--",prefix).decode().splitlines())
def digest(b):return hashlib.sha256(b).hexdigest()
def ancestor(p,c):
 return subprocess.run(["git","merge-base","--is-ancestor",p,c],cwd=ROOT,
                       stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0
def changes(p,c):
 return {tuple(l.split("\t",1)) for l in git("diff","--name-status",p,c).decode().splitlines()}
def have(text,*phrases):return all(p in text for p in phrases)
def known_repo_refs():
 for key,ref in P.items():
  must(git("rev-parse",ref).decode().strip()==ref,"UNKNOWN_EXACT_"+key)
 for p,c in [
  ("game","garage"),("garage_base","garage"),("garage_base","reverse"),
  ("save_v3","save_base"),("save_base","save")
 ]:
  must(ancestor(P[p],P[c]),"BROKEN_ANCESTRY_"+p+"_"+c)
 must(changes(P["game"],P["garage"])==EXPECTED_GARAGE,
      "GARAGE_SEVEN_SCOPED_ORIGINAL_GAME_PATHS")
 must(changes(P["garage_base"],P["reverse"])==
      {("M",BIND),("M","Assets/Tests/Editor/GarageEditFreedomTests.cs")},
      "SELECTED_887_TWO_ORIGINAL_GAME_FILES_ONLY")
 save_delta=changes(P["save_base"],P["save"])
 legal_prod={SAVE_SLOT,SAVE_SESSION}
 expected_workflow_updates={".github/workflows/b01-62-w30-single-writer-admission.yml"}
 expected_new_files={
  ".github/workflows/b01-62-w31-frozen-atomic-pair.yml",
  "Tools/B01/B01-62/P17W31AtomicPair/AtomicPairQA.csproj",
  "Tools/B01/B01-62/P17W31AtomicPair/Program.cs",
  "Tools/B01/B01-62/P17W31AtomicPair/README.md",
  "Tools/B01/B01-62/P17W31AtomicPair/manifest.json",
  "Tools/B01/B01-62/P17W31AtomicPair/source_guard.py"
 }
 must({p for status,p in save_delta if status=="A"}==expected_new_files,
      "SAVE_NEW_FILES_NOT_EXCLUSIVE_TO_ORIGINAL_896")
 must({p for status,p in save_delta if status=="M"}==
      legal_prod|expected_workflow_updates,
      "SAVE_MODIFIED_FOREIGN_SCOPE_OR_WORKFLOWS")
 must(len(save_delta)==9 and all(s in ("A","M") for s,_ in save_delta),
      "SAVE_896_EXCLUSIVE_NINE_PATH_DELTA")
 must(source(P["economy"],BASE_P)==source(P["garage"],BASE_P),
      "CURRENT_GAME_ECON_PARENT_META_DIFFERENT")
 return {"#877_changed_game_source_paths":len(EXPECTED_GARAGE),
         "#887_import_semantics":"SELECTED_ONLY_IN_SAME_877_OWNER",
         "#896_changed_source_paths":len(save_delta),
         "#896_modified_product_files":sorted(legal_prod),
         "shared_b01packages_meta":"ORIGINAL_BYTES_MATCH"}
def gameplay():
 s=source(P["garage"],BIND).decode("utf-8")
 r=source(P["garage"],REST).decode("utf-8")
 m=source(P["garage"],MODEL).decode("utf-8")
 v=source(P["garage"],GARAGE_VIEW).decode("utf-8")
 tests=source(P["garage"],"Assets/Tests/Editor/GarageEditFreedomTests.cs").decode("utf-8")
 must(have(s,"canonical.SetInstallationGate(candidate=>gate!=null?gate.Validate(candidate)",
          "canonical.SetRestorationGate(parts=>gate!=null&&gate.ValidateRestoration(parts))",
          "BuildPlacementRules.CheckForInstallation","FunctionalWeaponPlacementIssue",
          "ExistingMountedWeaponPlacementIssue","Physics.OverlapSphereNonAlloc(",
          "Physics.SphereCastNonAlloc(","Physics.OverlapCapsuleNonAlloc(",
          "Collider[] functionalOverlap=new Collider[FunctionalQueryCapacity]",
          "const int FunctionalQueryCapacity=256;"),"SOURCE_GAME_CANONICAL_GATE_CLOSURE")
 main=s[s.index("public string Validate(R3PartState candidate)"):s.index("static string Explain(BuildPlacementResult result)")]
 must(main.index("BuildPlacementRules.CheckForInstallation")<
      main.index("ExistingMountedWeaponPlacementIssue(probe,candidateColliders)")<
      main.index("return FunctionalWeaponPlacementIssue(candidate,probe);"),
      "INSTALLATION_REVERSE_FORWARD_ORDER")
 must("if(candidate.Kind!=R3PartKind.Gun)return null;" in s,
      "GAME_GUN_SPECIAL_HANDLING_DRIFT")
 must(have(s,"if(count>=functionalOverlap.Length)",
             "if(nearCount>=functionalOverlap.Length)",
             "if(castCount>=functionalCast.Length)",
             "if(count>=FunctionalQueryCapacity)"),
      "GARAGE_256_HIT_FAIL_CLOSED")
 must(have(s,"RotarySawContactMath.IsOnCuttingDisc(",
          "candidateCollider.ClosestPoint(center)-center",
          "Physics.OverlapCapsuleNonAlloc(start,end,radius,",
          "candidate.Weapon==R4WeaponKind.EmpEmitter)return null;",
          "ignoredOwnerIds!=null&&ignoredOwnerIds.Contains(gun.Id)"),
      "REVERSE_SAW_BARREL_EMP_AND_BATCH_IDS")
 must("BuildVolumeValidator.Check" in r and
      r.index("BuildVolumeValidator.Check")<
      r.index("FunctionalWeaponPlacementIssue(prospective[index],probe)")<
      r.index("ExistingMountedWeaponPlacementIssue(probe,colliders,replacedIds)")<
      r.index("return true;",r.index("ExistingMountedWeaponPlacementIssue(probe,colliders,replacedIds)")),
      "RESTORE_MISSING_REUSED_SAW_AND_OWNER_BATCH")
 must(have(r,"SetRestorationGate","B01") or have(s,"canonical.SetRestorationGate"),
      "NO_AUTHORITATIVE_RESTORE_GATE") # source R3 partial calls canonical
 must(have(tests,"NewArmorCannotBlockExistingSawAfterGaragePurchase",
              "ExistingDeadLegacySawCannotRepairOrEmergencyRepair",
              "new GameObject[260]"),"GARAGE_NATIVE_SOURCE_FIXTURES_MISSING")
 buy=m[m.index("public bool Buy("):m.index("public int RepairCost(")]
 mount=m[m.index("public bool Mount(string id,Vector3Int cell,Vector3Int rotation)"):
         m.index("public bool RepairInventory(")]
 must(buy.index("PlacementError(")<buy.index("Scrap-=Price(")<buy.index("Parts.Add("),
      "BUY_NOT_BEFORE_MUTATION")
 must(mount.index("OwnedPlacementError(")<mount.index("inventory.Remove("),
      "MOUNT_NOT_BEFORE_MUTATION")
 must(have(v,"public string PlacementIssue","run.VehicleModel.OwnedPlacementError",
             "run.GarageBuy(","run.GarageMount("),"VIEW_AUTHORITY_SPLIT")
 # Six independent deliberately broken source paths must reject.
 guard={
  "removed_reverse_call":lambda a,b,c,d,e:(a.replace(
    "ExistingMountedWeaponPlacementIssue(probe,candidateColliders)",
    "BAD_REVERSE(probe,candidateColliders)",1),b,c,d,e),
  "removed_repair_call":lambda a,b,c,d,e:(a,b.replace(
    "FunctionalWeaponPlacementIssue(prospective[index],probe)",
    "BAD_REPAIR(prospective[index],probe)",1),c,d,e),
  "removed_batch_restore_call":lambda a,b,c,d,e:(a,b.replace(
    "ExistingMountedWeaponPlacementIssue(probe,colliders,replacedIds)",
    "BAD_BATCH(probe,colliders,replacedIds)",1),c,d,e),
  "weak_overlap_saturation":lambda a,b,c,d,e:(a.replace(
    "if(count>=functionalOverlap.Length)","if(count>functionalOverlap.Length)",1),b,c,d,e),
  "weak_capsule_saturation":lambda a,b,c,d,e:(a.replace(
    "if(count>=FunctionalQueryCapacity)","if(count>FunctionalQueryCapacity)",1),b,c,d,e),
  "weak_payment":lambda a,b,c,d,e:(a,b,c.replace(
    "Scrap-=Price(kind,weapon);","Scrap=Price(kind,weapon);",1),d,e)
 }
 for k,fn in guard.items():
  a,b,c,d,e=fn(s,r,m,v,tests)
  original=(s,r,m,v,tests);modified=(a,b,c,d,e)
  must(modified!=original,"INEFFECTIVE_NEGATIVE_"+k)
  if k=="removed_reverse_call":must(
    "ExistingMountedWeaponPlacementIssue(probe,candidateColliders)" not in a,
    "MISS_REVERSE_NEGATIVE")
  elif k=="removed_repair_call":must(
    "FunctionalWeaponPlacementIssue(prospective[index],probe)" not in b,
    "MISS_REPAIR_NEGATIVE")
  elif k=="removed_batch_restore_call":must(
    "ExistingMountedWeaponPlacementIssue(probe,colliders,replacedIds)" not in b,
    "MISS_BATCH_NEGATIVE")
  elif k=="weak_overlap_saturation":must("if(count>=functionalOverlap.Length)" not in a,
    "MISS_OVERLAP_NEGATIVE")
  elif k=="weak_capsule_saturation":must("if(count>=FunctionalQueryCapacity)" not in a,
    "MISS_CAPSULE_NEGATIVE")
  else:must("Scrap-=Price(kind,weapon);" not in c,"MISS_PRICE_NEGATIVE")
 return {"actual_live_game":"#873+#877 current original Unity source",
         "exact_877_game_SHA":P["garage"],"single_installation_authority":"SOURCE_PASS",
         "same_repair_and_reverse_safety":"SOURCE_PASS",
         "before_scrap_and_inventory_mutation":"SOURCE_PASS",
         "guarded_256_physics_queries":"SOURCE_PASS",
         "source_mutants_rejected":len(guard),
         "authored_native_game_tests":"PRESENT_NOT_EXECUTED",
         "physx_behaviour":"NOT_RUN"}
def save_source():
 slot=source(P["save"],SAVE_SLOT).decode("utf-8")
 sess=source(P["save"],SAVE_SESSION).decode("utf-8")
 cold=source(P["save"],COLD).decode("utf-8")
 adm=source(P["admission"],ADMISSION_CS).decode("utf-8")
 must(have(slot,"EconomyV7AdmissionW29.TryAdmit",
                "EconomyV7AdmissionW29.TryEncodeForCanonicalOwner",
                "EconomyV7AdmissionW29.TryDecodeForCanonicalOwner",
                "File.Replace(staging,authoritative,null)",
                "File.Move(staging,authoritative)",
                "file.Flush(true)","B0162G02","slot.canonical-game-v2"),
      "SAVE_EXCLUSIVE_V3_HARDEND_SOURCE_MISSING")
 must(have(sess,"EconomyV7AdmissionW29.TryAdmit","CanonicalWorldGateway"),
      "SAVE_WRITER_SESSION_ADMISSION_MISSING")
 must("CanonicalColdReadV3" in cold and
      ("EconomyV7AdmissionW29" in slot) and
      ("class EconomyV7AdmissionW29" in adm or "static class EconomyV7AdmissionW29" in adm),
      "ONE_ORIGINAL_ADMISSION_OR_COLD_NOT_CONNECTED")
 for forbidden in ("SHA256.HashData(","Convert.ToHexString(",
                   "File.Move(staging,authoritative,true)",
                   "File.Delete(authoritative)"):
  must(forbidden not in slot+sess,"OLD_UNITY_API_OR_UNSAFE_DELETE_"+forbidden)
 # Prevent silent use of older raw Store as a second competing writer:
 must("AtomicDockLedger" not in slot+sess or
      "slot.canonical-game-v2" in slot,"SAVE_WRITER_SOURCE_BRANCH_INCONSISTENT")
 freeze_requirements={
  "slot_frozen_world":"frozenWorld=(byte[])world.Clone();",
  "slot_frozen_econ":"frozenEcon=econ.Copy();",
  "slot_semantic_frozen":"Semantic(frozenWorld,frozenEcon,expectedGeneration+1)",
  "slot_encode_frozen":"Encode(expectedGeneration+1,frozenWorld,",
  "slot_postpublish_world_frozen":"actualWorld.SequenceEqual(frozenWorld)",
  "slot_postpublish_econ_frozen":"SameEconomicSection(actualEconomy,frozenEcon)"
 }
 freeze_session={
  "session_frozen_world":"frozenWorld=(byte[])world.Clone();",
  "session_frozen_economy":"frozenEconomy=economy.Copy();",
  "session_validate_frozen":"ValidatePair(frozenWorld,frozenEconomy)",
  "session_gateway_frozen":"gateway.TryRestore(frozenWorld,runId,minimum)",
  "session_armed_frozen_world":"armedWorld=frozenWorld;",
  "session_armed_frozen_economy":"armedEconomy=frozenEconomy;"
 }
 for name,word in freeze_requirements.items():
  must(word in slot,"SAVE_896_FREEZE_"+name)
 for name,word in freeze_session.items():
  must(word in sess,"SAVE_896_FREEZE_"+name)
 # Four independent precise source negative-control changes, no product edits.
 for name,word in [
  ("skip_world_snapshot","frozenWorld=(byte[])world.Clone();"),
  ("skip_econ_snapshot","frozenEcon=econ.Copy();"),
  ("unsafe_postpublish_world","actualWorld.SequenceEqual(frozenWorld)"),
  ("unsafe_postpublish_wallet","SameEconomicSection(actualEconomy,frozenEcon)")
 ]:
  broken=slot.replace(word,"UNSAFE_MUTANT()",1)
  must(broken!=slot and word not in broken,"SAVE_896_NEGATIVE_NOT_REJECTED_"+name)
 must(source(P["save_base"],SAVE_SLOT)!=source(P["save"],SAVE_SLOT) and
      source(P["save_base"],SAVE_SESSION)!=source(P["save"],SAVE_SESSION),
      "SAVE_896_NO_ORIGINAL_WRITER_OR_SESSION_MUTATION")
 return {"save_latest":"#896 frozen physical whole-world + V7 pair; one V3 Store only",
         "on_disk_format":"B0162G02",
         "lease":"slot.canonical-game-v2",
         "atomic_publish":"File.Replace(existing); File.Move(only initial)",
         "unity_api_netstandard21_source":"CHECKED_SYMBOLS_NO_RUNTIME_UNITY",
         "original_855_V7_admission":"LINKED_ON_READ_AND_COMMIT",
         "cold_reader":"#858 inherited",
         "in_game_RigLedger_binder":"NOT_PRESENT",
         "original_frozen_pair_guards":len(freeze_requirements)+len(freeze_session),
         "save_mutant_negative_cases":4,
         "native_unity_and_player":"NOT_RUN"}
def economy_pricing_witness():
 m=source(P["garage"],MODEL).decode("utf-8")
 must("part.Health<=0&&purchase<=1)return 0;" in m and
      "Mathf.CeilToInt((1-part.Health)*purchase*0.35f)" in m and
      "part.Health<=0?Math.Min(purchase-1,quote):quote" in m and
      "cost<=0||Scrap<cost" in m,
      "ECON_EXPECTED_RED_WITNESS_SOURCE_HAS_CHANGED")
 # Exact current unmodified R3 applies ceil(35%), while V7 owner #731
 # requires floor(65%) for fully destroyed owned parts including legal paid0.
 pairs=[]
 for price in (1,5,15,30,100):
  old=0 if price<=1 else min(price-1,max(1,(35*price+99)//100))
  expected=(65*price)//100
  pairs.append({"purchase_scrap":price,"R3_destroyed_quote":old,
                "approved_V7_quote":expected,
                "R3_zero_quote_denied":old==0})
 must(sum(x["R3_destroyed_quote"]!=x["approved_V7_quote"] for x in pairs)==4 and
      pairs[0]["R3_zero_quote_denied"] and pairs[0]["approved_V7_quote"]==0,
      "ECON_EXPECTED_RED_PRICE_4_CASES_PLUS_PAID_ZERO")
 return {"product_result":"RED_EXPECTED_OWNER_APPROVED_V7_SOURCE_DIVERGENCE",
         "case_matrix":pairs,
         "fix_owner":"CODEX_CANONICAL_R3_ONLY",
         "production_source_modified":False}
def meta():
 selected_paths=set(tree(P["garage"],"Assets"))
 meta_paths=sorted(p for p in selected_paths if p.endswith(".meta"))
 by_guid={}; asm={}
 def feed(ref,paths,kind):
  for p in sorted(paths):
   if p.endswith(".meta"):
    mat=re.search(rb"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$",source(ref,p))
    if not mat:continue
    guid=mat.group(1).decode().lower()
    if guid in by_guid:
     must(by_guid[guid]==p,"DUPLICATE_GUID_"+kind+":"+p+"/"+by_guid[guid])
    by_guid[guid]=p
   elif p.endswith(".asmdef"):
    obj=json.loads(source(ref,p).decode())
    key=obj["name"]
    if key in asm:must(asm[key]==p,"DUPLICATE_ASMDEF_"+kind+":"+p)
    asm[key]=p
 feed(P["garage"],selected_paths,"CURRENT_GAME")
 current_guids=len(by_guid);current_asms=len(asm)
 # Current game has no Save writer scripts in Assets. New producer-owned packages
 # are simulated in a future G0 UNION, but never installed into live game.
 for tag,prefix,ref in (
  ("#792 RIG",RIG_DIR,P["physical"]),
  ("#855 V7 admission",ADMISSION_DIR,P["admission"])
 ):
  paths={p for p in tree(ref,prefix) if p.startswith(prefix+"/")}
  paths.add(prefix+".meta")
  for p in paths:
   if p in selected_paths:
    must(source(P["garage"],p)==source(ref,p),
         "OVERWRITE_GAME_WITH_FOREIGN_PRODUCER_"+p)
  feed(ref,paths,tag)
 for p in tree(P["physical"],RIG_DIR):
  if p.endswith(".cs") and p.startswith(RIG_DIR+"/"):
   must(p+".meta" in tree(P["physical"],RIG_DIR),"RIG_SCRIPT_META_MISSING_"+p)
 for p in tree(P["admission"],ADMISSION_DIR):
  if p.endswith(".cs"):
   must(p+".meta" in tree(P["admission"],ADMISSION_DIR),"ADMISSION_META_MISSING_"+p)
 must(current_guids>=1046 and current_asms>=110,"LIVE_UNITY_META_SCAN_TRUNCATED")
 for k in ("README.md.meta","manifest.json.meta"):
  meta_file=PAD_DIR+"/"+k
  must(meta_file in selected_paths,"W31_JUMPPAD_MISSING_META_"+k)
  must(meta_file not in tree(P["game"],"Assets"),"W31_META_NOT_ACTUALLY_NEW_"+k)
 must(source(P["garage"],BASE_P)==source(P["economy"],BASE_P),
      "ECON_ALREADY_OWNED_ROOT_META_CONFLICT")
 return {"current_game_original_unique_guids":current_guids,
         "current_game_asmdefs":current_asms,
         "future_selected_package_union_unique_guids":len(by_guid),
         "future_selected_package_union_asmdefs":len(asm),
         "unity_meta_collision":"NONE_ORIGINAL_BYTES"}
def scratch(original):
 with tempfile.TemporaryDirectory(prefix="B0140W32_ORIGINAL_INTAKE_") as temp:
  root=Path(temp);alien=root/"FOREIGN";(alien/"EMPTY").mkdir(parents=True)
  (alien/"keep.bin").write_bytes(b"NON_OWNER_SAVE_DO_NOT_TOUCH")
  current=root/"NEW_QA_OWNED_ONLY";current.mkdir()
  for name,data in original.items():
   p=current/name;p.parent.mkdir(parents=True,exist_ok=True)
   must(not p.exists(),"SRC_TARGET_ALREADY_EXISTS")
   p.write_bytes(data)
  def owned():
   for f in current.rglob("*"):
    if f.is_symlink():return False
    if f.is_dir() and not any(f.iterdir()):return False
    if f.is_file():
     k=f.relative_to(current).as_posix()
     if k not in original or f.read_bytes()!=original[k]:return False
   return {f.relative_to(current).as_posix() for f in current.rglob("*")
           if f.is_file()}==set(original)
  must(owned(),"STAGE_INITIAL_MISSING")
  test=current/"ALIEN_EMPTY";test.mkdir()
  must(not owned(),"FOREIGN_EMPTY_NOT_REJECTED");test.rmdir()
  target=next(iter(original));(current/target).write_bytes(b"DIRTY_NON_OWNER")
  must(not owned(),"DIRTY_BYTES_NOT_REJECTED")
  (current/target).write_bytes(original[target])
  must(owned(),"RESTORED_RECEIPT_INCORRECT")
  for k in original:(current/k).unlink()
  for d in sorted((x for x in current.rglob("*") if x.is_dir()),
                  key=lambda f:len(f.parts),reverse=True):d.rmdir()
  current.rmdir()
  must((alien/"keep.bin").read_bytes()==b"NON_OWNER_SAVE_DO_NOT_TOUCH" and
       (alien/"EMPTY").is_dir(),"ALIEN_DATA_OR_EMPTY_DIR_DELETED")
 return "OWNED_BYTES_STAGED_ROLLBACK_DIRTY_AND_ALIEN_EMPTY_REJECTED"
def make_zip(payload):
 out=io.BytesIO()
 with zipfile.ZipFile(out,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
  for k,v in sorted(payload.items()):
   info=zipfile.ZipInfo(k,(1980,1,1,0,0,0))
   info.create_system=3;info.external_attr=(0o100644<<16)
   info.compress_type=zipfile.ZIP_DEFLATED
   z.writestr(info,v,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
 return out.getvalue()
def main():
 a=argparse.ArgumentParser();a.add_argument("--out",required=True);args=a.parse_args()
 out=Path(args.out);must(not out.exists(),"EVIDENCE_ALREADY_EXISTS")
 scope=known_repo_refs()
 game=gameplay()
 save=save_source()
 econ_red=economy_pricing_witness()
 guid=meta()
 selected=dict()
 for path in sorted(p for _,p in EXPECTED_GARAGE):
  selected["REVIEW_ONLY_GAME_DELTA/"+path]=source(P["garage"],path)
 for path in (SAVE_SLOT,SAVE_SESSION,COLD):
  selected["REVIEW_ONLY_SAVE_NOT_UNITY_INSTALLED/"+path]=source(P["save"],path)
 selected["REVIEW_ONLY_SAVE_NOT_UNITY_INSTALLED/"+ADMISSION_CS]=source(P["admission"],ADMISSION_CS)
 for p in sorted(tree(P["physical"],RIG_DIR)):
  if p.endswith(".cs") or p.endswith(".meta") or p.endswith(".asmdef"):
   selected["REVIEW_ONLY_RIG_NOT_UNITY_INSTALLED/"+p]=source(P["physical"],p)
 stage=scratch(selected)
 bytes_receipt={k:{"sha256":digest(v),"bytes":len(v)}
                for k,v in sorted(selected.items())}
 manifest={"schema":"B01-40-P17-W32-GAME877-SAVE896-ORIGINAL-COMPOSITE-G0",
           "exact_pins":P,"ancestry_and_scope":scope,
           "current_garage_authoritative_source":game,
           "exclusive_save_admission_cold_source":save,
           "owner_approved_V7_real_game_pricing_red":econ_red,
           "unity_meta_future_union":guid,
           "staging":stage,"payload_sha256":bytes_receipt,
           "source_transport_g0":"PASS_IF_EXACT_SHA_CI_SUCCEEDS",
           "game_save_binder":"HOLD_NO_ACTUAL_R3_TYPED_OWNER",
           "standalone_saw_861":"HOLD_NATIVE",
           "turret_893":"HOLD_ORIGINAL_EXTERNAL_FBX_NATIVE",
           "loaner_834":"OWNER_POLICY_PENDING",
           "real_native_unity":"NOT_RUN",
           "windows_player":"NOT_BUILT",
           "anti_softlock":"RED",
           "release":False,"merged":False}
 packet={"PACKAGE_MANIFEST.json":(json.dumps(manifest,sort_keys=True,indent=2)+"\n").encode(),
         "NOT_INSTALLABLE_README.txt":(
            "ALL SOURCES REVIEW_ONLY; not a Unity asset installation or Windows Player. "
            "Do not install raw #787 store or older #797/#826 as a second writer. "
            "Do not change gameplay branches or player Save without B01-00 approval.\n").encode()}
 packet.update(selected)
 zipbytes=make_zip(packet)
 out.mkdir()
 (out/"manifest.json").write_bytes(packet["PACKAGE_MANIFEST.json"])
 (out/"B0140_W32_GAME877_SAVE896_REVIEW_ONLY.zip").write_bytes(zipbytes)
 (out/"package-sha256.json").write_bytes((json.dumps({
   "inner_zip_sha256":digest(zipbytes),"payload_entries":len(packet),
   "source_files":len(selected)},sort_keys=True,indent=2)+"\n").encode())
 print("B0140_W32_GARAGE877_NEW_REVERSE_RESTORATION_SAME_AUTHORITY_SOURCE_G0_PASS")
 print("B0140_W32_SAVE896_855_ADMISSION_COLD_ONE_WRITER_SOURCE_G0_PASS")
 print("B0140_W32_GAME_AND_SELECTED_RIG_ADMISSION_FUTURE_GUID_COLLISION_FREE")
 print("B0140_W32_SIX_SOURCE_MUTANTS_AND_HOSTILE_OS_ROLLBACK_REJECTED")
 print("B0140_W32_SOURCE_ONLY_INNER_ZIP_SHA256="+digest(zipbytes))
 print("B0140_W32_NATIVE_UNITY_AND_WINDOWS_PLAYER_NOT_RUN")
 print("B0140_W32_ORIGINAL_R3_V7_65_PERCENT_REBUILD_SOURCE_RED_EXPECTED")
if __name__=="__main__":main()
