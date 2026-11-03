#!/usr/bin/env python3
"""B01-40 W30: independent original game #873 + garage #877 source G0.
Exact original Git bytes, actual OS scratch staging and hostile negatives.
This is intentionally NOT Unity EditMode, PhysX, or a Windows Player build.
"""
import argparse, hashlib, io, json, re, subprocess, tempfile, zipfile
from pathlib import Path

BASE="ed8cf4bcc131688b9a93968a89683d2496586683" # published #824
LIVE="d537b7310979b91df27a08b14d0f1c74d0889089" # PR #873
GARAGE="20273c3580079dfb299c30c85a67e76f8d893e7f" # child #877
PAD="05db0ddf0a48da592d924e68b7658eea6bba764e" # producer #810
EMP="fea000fe0a706df2b52b6f295bdb3135ba1f9384" # producer #839
OVERLAY="3b3f402ca2adcc9525d96388cca17f0b5e1ae06d" # selected garage #866
SAW_BASE="887fd18bf0a518ee24776c660f5efa6e3a0491d8" # #828
SAW_FIX="8321131aac3e175255a357cf5a30b7716ef6e8fd" # #861
PAD_DIR="Assets/B01Packages/MapJumpPad001"
EMP_PATHS=(
 "Assets/RoboRougelikeMechanics/WPN006EMP/Runtime/EmpEmitter.cs",
 "Assets/RoboRougelikeMechanics/WPN006EMP/Tests/PlayMode/EmpEmitterPhysicsTests.cs",
 "Assets/WeaponIntegration/RRLEmpBinding.cs")
GARAGE_OVERLAY=(
 "Assets/B01UserExperience.meta",
 "Assets/B01UserExperience/B0112GarageSpatialW28.meta",
 "Assets/B01UserExperience/B0112GarageSpatialW28/B0112GarageSpatialOverlay.cs",
 "Assets/B01UserExperience/B0112GarageSpatialW28/B0112GarageSpatialOverlay.cs.meta",
 "Assets/Tests/Editor/B0112GarageSpatialW28Tests.cs",
 "Assets/Tests/Editor/B0112GarageSpatialW28Tests.cs.meta")
CANONICAL="Assets/R3/R3BuildValidation.cs"
GARAGE_TEST="Assets/Tests/Editor/GarageEditFreedomTests.cs"
GARAGE_DOC="Docs/W29/GARAGE_FUNCTIONAL_SWEEP_GATE.md"
SAW_PATHS=(
 "Assets/Pack/P12_PipelineSafeEnemyTurret.cs",
 "Assets/RoboRougelikeMechanics/WPN005RotarySaw/Runtime/RotarySawModule.cs",
 "Assets/Tests/Editor/RRLW6SawTests.cs",
 "Assets/WeaponIntegration/RRLSawBinding.cs",
 "Docs/W29/B01_24_SAW_RECEIVER_R1.md")
def deny(ok,msg):
 if not ok:raise AssertionError("B0140_W30_HOLD:"+msg)
def run(*args):
 p=subprocess.run(["git",*args],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 deny(p.returncode==0,"ORIGINAL_GIT_UNAVAILABLE:"+repr(args)+":"+p.stderr.decode("utf-8","replace")[-200:])
 return p.stdout
def raw(sha,path):return run("show",sha+":"+path)
def blob(sha,path):return run("rev-parse",sha+":"+path).decode().strip()
def hash256(x):return hashlib.sha256(x).hexdigest()
def ancestor(base,new):
 return subprocess.run(["git","merge-base","--is-ancestor",base,new],
   stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0
def status(base,head):
 return {tuple(v.split("\t",1)) for v in run("diff","--name-status",base,head).decode().splitlines()}
def names(root,ref):
 return set(run("ls-tree","-r","--name-only",ref,"--",root).decode().splitlines())
def original_producer(base,paths,kind,provenance):
 for path in sorted(paths):
  present=raw(GARAGE,path); orig=raw(base,path)
  deny(present==orig,"PRODUCER_SOURCE_CHANGED:"+kind+":"+path)
  provenance[path]={"producer":kind,"original_ref":base,
                    "git_blob":blob(base,path),"sha256":hash256(orig),
                    "bytes":len(orig)}
def symbol_occurs(s,*need):
 return all(n in s for n in need)
def validation_guard():
 g=raw(GARAGE,CANONICAL).decode("utf-8")
 before=raw(LIVE,CANONICAL).decode("utf-8")
 deny(g!=before,"NO_GARAGE_RUNTIME_DELTA")
 main=g[g.index("public string Validate(R3PartState candidate)"):g.index("static string Explain(BuildPlacementResult result)")]
 deny(main.index("BuildPlacementRules.CheckForInstallation") <
      main.index("return FunctionalWeaponPlacementIssue(candidate,probe);") <
      main.index("finally"),"WEAPON_HARDZONE_NOT_ON_SAME_AUTHORITY_AFTER_PHYSICS")
 deny(symbol_occurs(g,"SetInstallationGate(candidate=>gate","SetRestorationGate",
                       "Physics.OverlapBox","FunctionalWeaponPlacementIssue",
                       "Physics.OverlapSphere","Physics.SphereCastAll",
                       "RotarySawContactMath.IsOnCuttingDisc",
                       "other.ClosestPoint(muzzle.position)","!other.transform.IsChildOf(probe.transform)",
                       "other.transform.IsChildOf(transform)",
                       "R4WeaponKind.EmpEmitter"),"CANONICAL_OWNED_PHYSICS_GUARD_MISSING")
 deny(g.index("if(candidate.Weapon==R4WeaponKind.EmpEmitter)return null;") <
      g.index("if(candidate.Weapon==R4WeaponKind.RotarySaw)"),
      "EMP_WRONGLY_CAST_AS_PROJECTILE")
 deny(g.index("Physics.OverlapSphere") < g.index("Physics.SphereCastAll"),
      "DISC_AND_LINEAR_PHYSICS_BRANCH_REFACTORED")
 deny(symbol_occurs(g,"probe.SetActive(false)","DestroyImmediate(probe)"),
      "REJECTED_PROBE_CLEANUP_MISSING")
 model=raw(GARAGE,"Assets/R3/R3VehicleModel.cs").decode("utf-8")
 view=raw(GARAGE,"Assets/R3/R3GarageView.cs").decode("utf-8")
 buy=model[model.index("public bool Buy("):model.index("public int RepairCost(")]
 mount=model[model.index("public bool Mount(string id,Vector3Int cell,Vector3Int rotation)"):
    model.index("public bool RepairInventory(")]
 deny(buy.index("PlacementError(")<buy.index("Scrap-=Price(")<buy.index("Parts.Add("),
      "BUY_CHARGES_BEFORE_SAME_VALIDATOR")
 deny(mount.index("OwnedPlacementError(")<mount.index("inventory.Remove(")<
      mount.index("Parts.Add("),"OWNED_MOUNT_ID_MOVED_BEFORE_VALIDATOR")
 deny(symbol_occurs(view,"public string PlacementIssue",
      "run.VehicleModel.OwnedPlacementError","run.GarageMount(","run.GarageBuy(",
      "run.GarageBuyWeapon("),"GHOST_VS_HOST_GATE_SPLIT")
 test=raw(GARAGE,GARAGE_TEST).decode()
 deny(symbol_occurs(test,"RealGarageRejectsOwnedSawDiscPenetrationBeforeInventoryMutation",
         "new Vector3Int(0,1,4)","GarageMount(id,oldCell,default),Is.False",
         "Scrap,Is.EqualTo(beforeWallet)","Inventory.Single(p=>p.Id==id)"),
      "AUTHORED_REAL_UNITY_OWN_SAW_NEGATIVE_TEST_MISSING")
 return {"same_authoritative_validator":"SOURCE_GUARD_PASS",
         "buy_before_scrap":"SOURCE_GUARD_PASS",
         "mount_before_inventory":"SOURCE_GUARD_PASS",
         "saw_disc_vs_directional_sweep":"SOURCE_GUARD_PASS",
         "genuine_unity_test_authored":"PRESENT_NOT_RUN"}
def dynamics(provenance):
 padpaths={p for p in names(PAD_DIR,PAD) if p.startswith(PAD_DIR+"/")}
 padpaths.add(PAD_DIR+".meta")
 deny(len(padpaths)==19,"ORIGINAL_JUMPPAD_19_PATHS_CHANGED")
 original_producer(PAD,padpaths,"#810",provenance)
 original_producer(EMP,EMP_PATHS,"#839",provenance)
 original_producer(OVERLAY,GARAGE_OVERLAY,"#866",provenance)
 live=raw(GARAGE,"Assets/R5/R5Arena.cs").decode()
 deny(symbol_occurs(live,"JumpPad(new Vector3(7f,0f,-16f))",
      "pad.AddComponent<B01.MapJumpPad001.MapJumpPad>()",
      "area.isTrigger=true","launch.Configure(8.5f,2f,1.25f)",
      "visualCollider.enabled=false"),"R5_JUMPPAD_REAL_ARENA_HOOK_MISSING")
 runner=raw(GARAGE,"Assets/B01Packages/MapJumpPad001/Runtime/MapJumpPad.cs").decode()
 deny(symbol_occurs(runner,"ForceMode.VelocityChange","OnTriggerEnter",
     "Rigidbody"),"ORIGINAL_IMPULSE_RIGIDBODY_MISSING")
 tests=raw(GARAGE,"Assets/Tests/Editor/W29LiveArenaJumpPadTests.cs").decode()
 deny(tests.count("[UnityTest]")==2 and "WaitForFixedUpdate" in tests,
      "REAL_ARENA_NATIVE_FIXTURE_NOT_AUTHORED")
 ui=raw(GARAGE,"Assets/B01UserExperience/B0112GarageSpatialW28/B0112GarageSpatialOverlay.cs").decode()
 deny(symbol_occurs(ui,"OccupiedRed","ProtrusionBlue","WeaponPurple",
                      "MechanismOrange","ValidGreen","source.PlacementIssue"),
      "FIVE_VISIBLE_SOURCE_CLASSES_MISSING")
 return {"original_pad_19_git_paths":"SOURCE_GUARD_PASS",
         "original_emp_3_git_paths":"SOURCE_GUARD_PASS",
         "original_garage_ui_6_git_paths":"SOURCE_GUARD_PASS",
         "actual_r5_jump_trigger":"SOURCE_GUARD_PASS",
         "native_r5_test_cases":"2_AUTHORED_NOT_RUN",
         "five_colors":"VISUAL_SOURCE_ONLY_NO_FULL_MECHANISM_SWEEP"}
def saw_hold():
 diff=status(SAW_BASE,SAW_FIX)
 deny(diff=={("M",p) for p in SAW_PATHS[:-1]}|{("A",SAW_PATHS[-1])},
      "SAW_CANDIDATE_SOURCE_SCOPE_UNEXPECTED")
 code=raw(SAW_FIX,SAW_PATHS[1]).decode()
 binding=raw(SAW_FIX,SAW_PATHS[3]).decode()
 tests=raw(SAW_FIX,SAW_PATHS[2]).decode()
 deny(symbol_occurs(code,"OverflowedLastTick","IntersectsOtherOwnedPart"),
      "SAW_OWNER_OVERFLOW_GUARD_MISSING")
 deny("Enemy_Muzzle" in binding and "Enemy_Yaw" in binding,
      "SAW_ENEMY_SERIALIZED_PARENT_PATH_NOT_MAPPED")
 deny("new Vector3Int(0,1,4)" in tests,"SAW_PLAYER_LEGAL_EXTERNAL_FIXTURE_MISSING")
 return {"saw_candidate_pr":"#861",
         "sha":SAW_FIX,
         "source_review":"CONTRACT_PRESENT",
         "native_unity_g1":"NOT_RUN",
         "adoption":"HOLD",
         "no_branch_overlay":True}
def metas(paths):
 allmeta=names("Assets",GARAGE)
 guids={};asmdefs={}
 for path in sorted(allmeta):
  if path.endswith(".meta"):
   b=raw(GARAGE,path)
   m=re.search(rb"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$",b)
   if not m:continue
   key=m.group(1).decode().lower()
   deny(key not in guids,"DUPLICATE_LIVE_GAME_UNITY_GUID:"+key+":"+path+"/"+guids.get(key,""))
   guids[key]=path
  if path.endswith(".asmdef"):
   a=json.loads(raw(GARAGE,path));name=a["name"]
   deny(name not in asmdefs,"DUPLICATE_LIVE_GAME_ASMDEF:"+name)
   asmdefs[name]=path
 deny(len(guids)>900 and len(asmdefs)>10,"GAME_META_SCAN_TOO_SMALL_OR_LOST")
 for p in paths:
  if p.endswith(".cs") and p.startswith("Assets/B01Packages/"):
   deny(p+".meta" in allmeta,"IMPORTED_PACKAGE_CS_META_MISSING:"+p)
 return {"original_unique_guid_count":len(guids),"original_unique_asmdef_count":len(asmdefs),
         "no_duplicate_game_guid":"PASS","no_duplicate_game_asmdef":"PASS"}
def negative_stage(payload):
 with tempfile.TemporaryDirectory(prefix="B01-40-W30-RECEIVER-") as tmp:
  root=Path(tmp)
  foreign=root/"ALIEN";(foreign/"EMPTY").mkdir(parents=True)
  (foreign/"keep.dat").write_bytes(b"KEEP_UNOWNED_FILE")
  stage=root/"NEW_UNPUBLISHED_CODEX_SCOPE";stage.mkdir()
  for name,b in payload.items():
   p=stage/name;p.parent.mkdir(parents=True,exist_ok=True)
   deny(not p.exists() and not p.is_symlink(),"OCCUPIED_PATH")
   p.write_bytes(b)
  def ownership():
   for x in stage.rglob("*"):
    if x.is_symlink():return False
    rel=x.relative_to(stage).as_posix()
    if x.is_file():
     if rel not in payload or x.read_bytes()!=payload[rel]:return False
    if x.is_dir() and not any(x.iterdir()) and not any(k.startswith(rel+"/") for k in payload):return False
   return {p.relative_to(stage).as_posix() for p in stage.rglob("*") if p.is_file()}==set(payload)
  deny(ownership(),"NEW_RECEIVER_NOT_OWNED")
  extra=stage/"FOREIGN_EMPTY";extra.mkdir()
  deny(not ownership(),"FOREIGN_EMPTY_NOT_DENIED");extra.rmdir()
  victim=next(iter(payload));(stage/victim).write_bytes(b"UNOWNED_MUTATION")
  deny(not ownership(),"MODIFIED_BYTE_NOT_DENIED")
  (stage/victim).write_bytes(payload[victim])
  deny(ownership(),"RESTORED_BYTE_NOT_ACCEPTED")
  for x in sorted(payload):(stage/x).unlink()
  for d in sorted((p for p in stage.rglob("*") if p.is_dir()),key=lambda p:len(p.parts),reverse=True):d.rmdir()
  stage.rmdir()
  deny((foreign/"keep.dat").read_bytes()==b"KEEP_UNOWNED_FILE" and
       (foreign/"EMPTY").is_dir(),"FOREIGN_DATA_REMOVED")
 return {"new_scope_physical_bytes":"PASS","dirty_mutation_refused":"PASS",
         "foreign_empty_refused":"PASS","clean_owned_rollback":"PASS",
         "foreign_files_and_empty_folders_preserved":"PASS"}
def zip_data(items):
 stream=io.BytesIO()
 with zipfile.ZipFile(stream,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
  for name,b in sorted(items.items()):
   info=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
   info.create_system=3;info.external_attr=0o100644<<16;info.compress_type=zipfile.ZIP_DEFLATED
   z.writestr(info,b,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
 return stream.getvalue()
def main():
 a=argparse.ArgumentParser();a.add_argument("--out",required=True);args=a.parse_args()
 out=Path(args.out);deny(not out.exists(),"OUTPUT_ALREADY_OCCUPIED")
 for ref in (BASE,LIVE,GARAGE,PAD,EMP,OVERLAY,SAW_BASE,SAW_FIX):
  deny(run("rev-parse",ref).decode().strip()==ref,"WRONG_ORIGINAL_SHA:"+ref)
 deny(ancestor(BASE,LIVE) and ancestor(LIVE,GARAGE) and ancestor(SAW_BASE,SAW_FIX),
      "SOURCE_ANCESTRY_OR_PR_SCOPE_WRONG")
 live_delta=status(BASE,LIVE)
 deny(len(live_delta)==37 and
      all(x[0] in ("A","M") for x in live_delta),
      "873_LIVE_GAME_CHANGE_COUNT_OR_DELETIONS")
 garage_delta=status(LIVE,GARAGE)
 deny(garage_delta=={("M",CANONICAL),("M",GARAGE_TEST),("A",GARAGE_DOC)},
      "877_3_GAME_PATHS_NOT_EXCLUSIVE")
 combined=set(p for _,p in live_delta|garage_delta)
 deny(len(combined)==40,"CUMULATIVE_40_SOURCE_PATHS_EXPECTED")
 provenance={}
 behavioral=validation_guard()
 dynamics_gates=dynamics(provenance)
 saw=saw_hold()
 unity=metas(combined)
 payload={p:raw(GARAGE,p) for p in combined}
 stage=negative_stage(payload)
 provenance.update({p:{"pr":"#873+#877","git_blob":blob(GARAGE,p),
                       "bytes":len(payload[p]),"sha256":hash256(payload[p])}
                    for p in sorted(payload)})
 manifest={"schema":"B01-40-P17-W30-LIVE873-GARAGE877-ORIGINAL-G0-V1",
  "codex_game_base_824":BASE,"game_source_873":LIVE,"garage_child_877":GARAGE,
  "original_producers":{"jump_pad_810":PAD,"emp_839":EMP,"garage_overlay_866":OVERLAY},
  "source_scope":{"873_adds_modifies":37,"877_extra":3,"combined_paths":40},
  "unchanged_original_producer_blobs":{p:provenance[p] for p in
      sorted(set(provenance)-combined)},
  "changed_original_game_blobs":{p:provenance[p] for p in sorted(combined)},
  "unity":unity,"source_semantic_guards":behavioral,
  "current_gameplay":dynamics_gates,"saw_separate_nonintegrated":saw,
  "scratch":stage,
  "source_g0":"PASS_IF_ALL_EXACT_HEAD_OS_CI_GREEN",
  "native_unity_edit_play":"NOT_RUN","player_windows_zip":"NOT_PRODUCED",
  "physical_full_5_zone_collision":"NOT_YET_COMPLETE",
  "save_826_game_binder":"NOT_INCLUDED","loaner_834":"OWNER_DECISION_PENDING",
  "native_integration":"NO_SOURCE_RECEIVER_ONLY","merge":False,"release":False}
 # Intentionally SOURCE ONLY — do not copy entire producer history, tooling or test source
 # into any active Codex checkout. SOURCE/ is a reviewable current actual delta, not patch.
 packet={"MANIFEST.json":(json.dumps(manifest,sort_keys=True,indent=2)+"\n").encode(),
         "G1_HOLD.txt":b"SOURCE G0 only. No native Unity/Windows Player and no active Codex worktree access. B01-00 approves any intake.\n"}
 packet.update({"READ_ONLY_CURRENT_DIFF/"+p:b for p,b in payload.items()})
 out.mkdir()
 (out/"manifest.json").write_bytes(packet["MANIFEST.json"])
 archive=zip_data(packet)
 (out/"W30_873_877_GAME_SOURCE_ONLY.zip").write_bytes(archive)
 digest=hash256(archive)
 print("B0140_W30_EXACT_LIVE_873_SOURCE_SCOPE_37_PASS")
 print("B0140_W30_EXACT_GARAGE_877_CHILD_ONLY_3_PASS")
 print("B0140_W30_ORIGINAL_810_839_866_BYTES_AND_LIVE_R5_PASS")
 print("B0140_W30_CANONICAL_GARAGE_BUY_MOUNT_PREVIEW_SOURCE_GUARD_PASS")
 print("B0140_W30_CURRENT_GAME_UNIQUE_GUID="+str(unity["original_unique_guid_count"])+
       "_ASMDEF="+str(unity["original_unique_asmdef_count"]))
 print("B0140_W30_SAW_861_REVIEW_ONLY_NATIVE_UNITY_G1_NOT_RUN")
 print("B0140_W30_SAFE_SCRATCH_STAGE_AND_HOSTILE_NEGATIVES_PASS")
 print("B0140_W30_INNER_ZIP_SHA256="+digest)
 print("B0140_W30_SOURCE_ONLY_NATIVE_UNITY_AND_WINDOWS_PLAYER_NOT_RUN")
if __name__=="__main__":main()
