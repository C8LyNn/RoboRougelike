#!/usr/bin/env python3
"""B01-40 W30: exact #873→#877 original gameplay + new authored genuine Unity QA.
Only SOURCE G0 (Git object/owner/ABI/negative staging), never native Unity G1.
"""
import argparse, hashlib, json, os, pathlib, re, subprocess, sys, tempfile

BASE="ed8cf4bcc131688b9a93968a89683d2496586683"
GAME="d537b7310979b91df27a08b14d0f1c74d0889089"
GARAGE="20273c3580079dfb299c30c85a67e76f8d893e7f"
OWNER="Assets/R3/R3BuildValidation.cs"
OWNER_TEST="Assets/Tests/Editor/GarageEditFreedomTests.cs"
REPORT="Docs/W29/GARAGE_FUNCTIONAL_SWEEP_GATE.md"
NATIVE="Assets/Tests/Editor/B0140W30DynamicGarageClearanceTests.cs"
META=NATIVE+".meta"
OWN="Tools/B01/B01-40/P17W30MovingGarageG0/"
CI=".github/workflows/b01-40-p17w30-877-moving-garage-g0.yml"
MUST_ADD={NATIVE,META,OWN+"verify.py",OWN+"README.md",CI}
GUID="32e817b5d6b64e689f8a615cca71bd42"

class G0Error(Exception): pass
def need(ok,msg):
 if not ok: raise G0Error(msg)
def cmd(*a):
 p=subprocess.run(["git",*a],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 need(p.returncode==0,"GIT_ERROR:"+str(a)+" "+p.stderr.decode(errors="replace")[-300:])
 return p.stdout
def original(ref,path): return cmd("show",ref+":"+path)
def object_sha(ref,path): return cmd("rev-parse",ref+":"+path).decode().strip()
def sha(b): return hashlib.sha256(b).hexdigest()

def source_snapshot():
 for key in (BASE,GAME,GARAGE):
  need(cmd("rev-parse",key+"^{commit}").decode().strip()==key,
       "MISSING_ORIGINAL_SHA:"+key)
 for left,right in ((BASE,GAME),(GAME,GARAGE),(GARAGE,"HEAD")):
  p=subprocess.run(["git","merge-base","--is-ancestor",left,right])
  need(p.returncode==0,"WRONG_GIT_HISTORY:"+left+":"+right)
 current=cmd("rev-parse","HEAD").decode().strip()
 original_873=cmd("diff","--name-status",BASE,GAME).decode().splitlines()
 paths_873={s.split("\t",1)[1] for s in original_873}
 need(len(paths_873)==37,"EXACT_ORIGINAL_873_SCOPE_CHANGED:"+str(len(paths_873)))
 original_877=cmd("diff","--name-status",GAME,GARAGE).decode().splitlines()
 expected_877={"M\t"+OWNER,"M\t"+OWNER_TEST,"A\t"+REPORT}
 need(set(original_877)==expected_877,
      "EXACT_ORIGINAL_877_PRODUCER_SCOPE_CHANGED:"+repr(original_877))
 only_own=cmd("diff","--name-status",GARAGE,"HEAD").decode().splitlines()
 need(set(only_own)=={"A\t"+s for s in MUST_ADD},
      "MODIFIED_Codex_OR_PRODUCER_FILE_OR_MISSING_QA:"+repr(only_own))
 for p in sorted(paths_873):
  if p in (OWNER,OWNER_TEST):continue
  need(object_sha("HEAD",p)==object_sha(GAME,p),
       "ORIGINAL_INTEGRATED_873_SOURCE_CHANGED:"+p)
 for p in (OWNER,OWNER_TEST,REPORT):
  need(original("HEAD",p)==original(GARAGE,p),
       "ORIGINAL_877_PRODUCT_SOURCE_CHANGED:"+p)
 # .meta GUID of the new QA test is unique across the complete actual receiver.
 metas=cmd("ls-tree","-r","-z","HEAD","--","Assets")
 raw=[entry.split(b"\t",1) for entry in metas.split(b"\0") if entry]
 selected=[(path.decode("utf8"),desc.split()[2]) for desc,path in raw
           if path.endswith(b".meta")]
 input_bytes=b"".join(oid+b"\n" for _,oid in selected)
 scan=subprocess.run(["git","cat-file","--batch"],input=input_bytes,
                     stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 need(scan.returncode==0,"BATCH_NATIVE_GIT_META_READ_FAILURE")
 offset=0
 ids={}
 for path,oid in selected:
  end=scan.stdout.find(b"\n",offset)
  need(end>=0,"BATCH_TRUNCATED_HEADER")
  bits=scan.stdout[offset:end].split()
  need(len(bits)==3 and bits[0]==oid and bits[1]==b"blob",
       "META_GIT_OID_MISMATCH:"+path)
  n=int(bits[2])
  content=scan.stdout[end+1:end+1+n]
  need(len(content)==n,"META_TRUNCATED:"+path)
  offset=end+2+n
  m=re.search(rb"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$",content)
  need(m is not None,"META_GUID_MISSING:"+path)
  id=m.group(1).decode().lower()
  need(id not in ids,"DUPLICATE_META_GUID:"+id+":"+path+"/"+ids.get(id,""))
  ids[id]=path
 need(ids.get(GUID)==META,"QA_META_IS_NOT_UNIQUE_AND_RECOGNIZED")
 print("B0140_W30_ORIGINAL_873_37_FILES_AND_877_3_SOURCE_DELTAS_EXACT_UNMODIFIED_PASS",current)
 print("B0140_W30_COMPLETE_RECEIVER_UNITY_GUID_UNIQUENESS_PASS",len(ids))
 return current,paths_873,ids

def source_contract():
 gameplay=original("HEAD",OWNER).decode()
 model=original("HEAD","Assets/R3/R3VehicleModel.cs").decode()
 tests=original("HEAD",OWNER_TEST).decode()
 native=original("HEAD",NATIVE).decode()
 report=original("HEAD",REPORT).decode()
 arena=original("HEAD","Assets/R5/R5Arena.cs").decode()
 overlay=original("HEAD","Assets/B01UserExperience/B0112GarageSpatialW28/B0112GarageSpatialOverlay.cs").decode()
 for needle in ("FunctionalWeaponPlacementIssue(candidate,probe)",
       "Physics.OverlapSphere(muzzle.position","RotarySawContactMath.IsOnCuttingDisc(",
       "Physics.SphereCastAll(pivot.position","QueryTriggerInteraction.Ignore",
       "candidate.Weapon==R4WeaponKind.EmpEmitter","new BuildPhysicsEvidence("):
  need(needle in gameplay,"REAL_877_AUTHORITY_PHYSICAL_CHECK_MISSING:"+needle)
 for needle in ("SetInstallationGate(", "OwnedPlacementError(", "installationGate(candidate)",
       "public bool Buy(", "public bool Mount("):
  need(needle in model,"EXISTING_SINGLE_MODEL_GATE_MISSING:"+needle)
 before=model.index("public bool Buy(")
 buy=model[before:before+900]
 need(buy.index("PlacementError(")<buy.index("Scrap-=Price("),
      "SCRAP_SPENT_BEFORE_ACTUAL_CANONICAL_GATE")
 mount=model[model.index("public bool Mount(string id,Vector3Int cell,Vector3Int rotation)"):
             model.index("public bool RepairInventory(")]
 need(mount.index("OwnedPlacementError(")<mount.index("inventory.Remove(part)"),
      "MOUNT_REMOVES_OWNED_ID_BEFORE_PHYSICAL_GATE")
 for token in ("RealGarageRejectsOwnedSawDiscPenetrationBeforeInventoryMutation",
               "run.GarageMount(id,oldCell,default),Is.False",
               "run.GarageMount(id,exterior,default),Is.True"):
  need(token in tests,"ORIGINAL_877_NATIVE_TEST_SOURCE_MISSING:"+token)
 for token in ("[UnityTest]","new EnterPlayMode()","new ExitPlayMode()",
               "R1RunController.Instance","R3VehicleAssembly",
               "R4WeaponKind.RotarySaw","new Vector3Int(0,1,4)",
               "AddComponent<Rigidbody>()","physical.MovePosition(center)",
               "physical.MovePosition(safelyAway)","new WaitForFixedUpdate()",
               "run.GarageMount(id,exterior,default),Is.False",
               "run.GarageMount(id,exterior,default),Is.True",
               "model.Scrap","model.Inventory.Single(p=>p.Id==id)",
               "model.OwnedPlacementError("):
  need(token in native,"NEW_REAL_UNITY_TEST_ENTRYPOINT_MISSING:"+token)
 need(native.count("[UnityTest]")==1,"NEW_QA_TEST_COUNT_DRIFT")
 for token in ("MapJumpPad(","B01.MapJumpPad001.MapJumpPad",
               "kind==R5ArenaKind.SalvageYard"):
  need(token in arena,"ACTUAL_873_LIVE_ARENA_JUMP_PAD_MISSING:"+token)
 for token in ("OccupiedRed","ProtrusionBlue","WeaponPurple",
               "MechanismOrange","ValidGreen","source.PlacementIssue"):
  need(token in overlay,"ACTUAL_873_FIVE_VISUAL_GARAGE_CLASSES_MISSING:"+token)
 need("RotarySawContactMath" in report,"OWNER_877_REPORT_MISSING")
 print("B0140_W30_877_LIVE_PHYSICAL_WPN005_AND_GARAGE_PREVIEW_BUY_MOUNT_AUTHORITY_PASS")
 print("B0140_W30_873_JUMPPAD_FIVE_COLOR_GARAGE_SOURCE_WIRING_PASS")
 print("B0140_W30_NEW_REAL_UNITY_DYNAMIC_OWNER_RIGIDBODY_TEST_SOURCE_PASS_NOT_EXECUTED")

def negative_source_copy():
 with tempfile.TemporaryDirectory(prefix="B0140W30-G0-original-") as tmp:
  root=pathlib.Path(tmp)
  save=root/"foreign-owner"/"save.envelope"
  save.parent.mkdir()
  save.write_bytes(b"FOREIGN_USER_SAVE_PRESERVE")
  keep=root/"foreign-owner"/"EMPTY"
  keep.mkdir()
  stage=root/"B01-40-only-stage"
  stage.mkdir()
  assert not (stage/OWNER).exists()
  for path in (OWNER,OWNER_TEST,NATIVE,META):
   b=original("HEAD",path)
   target=stage/path
   target.parent.mkdir(parents=True,exist_ok=True)
   target.write_bytes(b)
   need(target.read_bytes()==b,"STAGED_BYTES_DIFFER:"+path)
  bad=stage/OWNER
  good=original(GARAGE,OWNER)
  bad.write_bytes(good+b"UNAUTHORIZED_MODIFICATION")
  need(bad.read_bytes()!=good,"NEGATIVE_DIRTY_877_CANNOT_DETECT")
  bad.write_bytes(good)
  need(bad.read_bytes()==good,"NEGATIVE_ROLLBACK_CANNOT_RESTORE")
  need(save.read_bytes()==b"FOREIGN_USER_SAVE_PRESERVE" and keep.is_dir(),
       "FOREIGN_PLAYER_SAVE_OR_EMPTY_DIRECTORY_MODIFIED")
 print("B0140_W30_REAL_STAGING_DIRTY_PRODUCT_NEGATIVE_AND_FOREIGN_SAVE_PASS")

def main():
 p=argparse.ArgumentParser()
 p.add_argument("--receipt",required=True)
 args=p.parse_args()
 current,paths_873,ids=source_snapshot()
 source_contract()
 negative_source_copy()
 data={
  "schema":"B01-40-P17W30-INDEPENDENT-877-DYNAMIC-PHYSICAL-GARAGE-G0",
  "current_qa_head":current,"original_current_game_873":GAME,
  "original_functional_garage_877":GARAGE,
  "original_game_824":BASE,
  "original_873_paths":len(paths_873),"original_877_modified_paths":3,
  "receiver_unity_meta_count":len(ids),
  "new_test_unity_meta_guid":GUID,
  "original_877_blobs":{p:{"git_blob_sha":object_sha(GARAGE,p),
                              "sha256":sha(original(GARAGE,p))}
                         for p in (OWNER,OWNER_TEST,REPORT)},
  "own_added_files":{p:{"git_blob_sha":object_sha("HEAD",p),
                        "sha256":sha(original("HEAD",p))}
                     for p in sorted(MUST_ADD)},
  "native_unity_6000_3_14f1":"NOT_RUN",
  "windows_player":"NOT_BUILT",
  "native_dynamic_test":"AUTHORED_NOT_EXECUTED",
  "own_source_transport_g0":"PASS_AFTER_REAL_TWO_OS_PARITY",
  "existing_original_873_source":"BYTE_EXACT_NO_DUPLICATES",
  "garage_5_colors":"RENDER_SOURCE_PRESENT_PHYSICAL_MECHANISM_ORANGE_NOT_VALIDATED",
  "saw_861":"NOT_INSTALLED_UNVERIFIED_NATIVE",
  "save_826":"NOT_INSTALLED_IN_GAME_NO_SECOND_WRITER",
  "owner_softlock_834":"PENDING_OWNER_DECISION",
  "codex_ack":"NONE","merge":False,"release":False
 }
 out=pathlib.Path(args.receipt)
 need(not out.exists(),"DO_NOT_OVERWRITE_OLDER_EVIDENCE")
 out.parent.mkdir(parents=True,exist_ok=True)
 encoded=(json.dumps(data,sort_keys=True,indent=2)+"\n").encode()
 out.write_bytes(encoded)
 print("B0140_W30_EXACT_877_DYNAMIC_RECEIVER_SOURCE_G0_PASS")
 print("B0140_W30_INNER_MANIFEST_SHA256",sha(encoded))
if __name__=="__main__":
 try: main()
 except Exception as exc:
  print("B0140_W30_FAIL_CLOSED:"+str(exc),file=sys.stderr)
  sys.exit(2)
