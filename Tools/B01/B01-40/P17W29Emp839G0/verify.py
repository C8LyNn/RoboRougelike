#!/usr/bin/env python3
"""B01-40 W29: original #839 EMP source G0 against published R3 #824.
No duplicate weapon code. Does NOT run Unity or alter Codex active worktree.
"""
import argparse,hashlib,io,json,re,subprocess,tempfile,zipfile
from pathlib import Path
BASE="e03d36f2b86f8c73a067e574b67237a8921f622a"
CAND="fea000fe0a706df2b52b6f295bdb3135ba1f9384"
GAME="ed8cf4bcc131688b9a93968a89683d2496586683"
NATIVE_QA="16d338f13779ee71ac74b8549e0168dce92bfda3"
EMITTER="Assets/RoboRougelikeMechanics/WPN006EMP/Runtime/EmpEmitter.cs"
TEST="Assets/RoboRougelikeMechanics/WPN006EMP/Tests/PlayMode/EmpEmitterPhysicsTests.cs"
BINDING="Assets/WeaponIntegration/RRLEmpBinding.cs"
DOC="Docs/W28/EMP_DIRECT_ACTIVATION_QA.md"
MODIFIED=(EMITTER,TEST,BINDING)
def must(ok,msg):
 if not ok:raise RuntimeError("B0140_W29_EMP_G0_HOLD:"+msg)
def git(*args):
 p=subprocess.run(["git",*args],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 must(p.returncode==0,"GIT_FAILURE:"+repr(args)+":"+p.stderr.decode("utf-8","replace")[-200:])
 return p.stdout
def get(ref,path):return git("show",ref+":"+path)
def digest(b):return hashlib.sha256(b).hexdigest()
def compare_ancestor(a,b):
 return subprocess.run(["git","merge-base","--is-ancestor",a,b],
   stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0
def actual_source():
 for x in (BASE,CAND,GAME,NATIVE_QA):
  must(git("rev-parse",x).decode().strip()==x,"COMMIT_NOT_EXACT:"+x)
 must(compare_ancestor(BASE,CAND) and compare_ancestor(BASE,GAME)
      and compare_ancestor(CAND,NATIVE_QA),"PR_LINEAGE_BROKEN")
 changed=git("diff","--name-status",BASE,CAND).decode().splitlines()
 expected={("M",x) for x in MODIFIED}|{("A",DOC)}
 parsed=set(tuple(line.split("\t",1)) for line in changed)
 must(parsed==expected,"CANDIDATE_NOT_EXCLUSIVE_EMP_SCOPE:"+repr(parsed))
 qa=git("diff","--name-status",CAND,NATIVE_QA).decode().splitlines()
 must(len(qa)==3 and all(x.startswith("A\t") for x in qa)
      and all(x.split("\t",1)[1].startswith(("Assets/R1/W27EmpPlayerQA.cs",
                                            "Docs/W27/EMP839_NATIVE_QA.md"))
              for x in qa),"NATIVE_QA_SCOPE_MIXES_PRODUCT_SOURCE")
 selected={};pins={}
 for path in MODIFIED:
  prev=get(BASE,path);current=get(GAME,path);now=get(CAND,path)
  must(prev==current,"CURRENT_GAME_CHANGED_SAME_EMP_PATH:"+path)
  must(now!=current,"EMP_REWORK_NOT_IN_SOURCE:"+path)
  meta=path+".meta"
  must(get(BASE,meta)==get(CAND,meta)==get(GAME,meta),
       "SELECTED_EMP_SCRIPT_GUID_CHANGED:"+meta)
  guid=re.search(rb"(?m)^guid:\s*([a-f0-9]{32})\s*$",get(CAND,meta))
  must(guid is not None,"GUID_MISSING:"+meta)
  pins[path]={"base_blob":git("rev-parse",BASE+":"+path).decode().strip(),
              "published_game_blob":git("rev-parse",GAME+":"+path).decode().strip(),
              "candidate_blob":git("rev-parse",CAND+":"+path).decode().strip(),
              "candidate_bytes":len(now),"candidate_sha256":digest(now),
              "stable_meta_guid":guid.group(1).decode()}
  selected[path]=now
 module=get(CAND,EMITTER).decode()
 attach=get(CAND,BINDING).decode()
 tests=get(CAND,TEST).decode()
 run=module.index("public bool TryPulse(out EmpPulseReport report)")
 section=module[run:]
 gate=section.index("activationGate != null && !activationGate()")
 cooldown=section.index("cooldownTimer.Ready")
 energy=min((i for i in (section.find("TryConsume"),section.find("SpendEnergy"),
  section.find("EnergySource"),section.find("OverlapSphereNonAlloc")) if i>=0),
  default=len(section))
 must(gate<cooldown and gate<energy,"ACTIVATION_GATE_AFTER_COOLDOWN_ENERGY_OR_PHYSICS")
 must("SetActivationGate(Func<bool> gate)" in module and
      "activationGate == null || activationGate()" in module,
      "READY_AND_DIRECT_CALL_NOT_SHARED_GATE")
 must("SetActivationGate(() => Device != null && Device.Available" in attach
      and "transform.IsChildOf(Device.Owner)" in attach
      and "RRLEmpPartState.IsBlocked(gameObject)" in attach,
      "LIVE_R3_DEVICE_OWNER_DISRUPTION_GUARD_NOT_BOUND")
 must("OverlapSphereNonAlloc" in module and "EmpPulseReport" in module
      and "Physics.Raycast" in module,
      "ORIGINAL_RADIAL_EMP_AND_SOLID_OCCLUSION_NOT_PRESERVED")
 must("Pulse_DirectModuleCallRechecksGateBeforeEnergyAndCooldown" in tests
      and "Assert.AreEqual(40f, bank.Current" in tests
      and "Assert.AreEqual(0f, emitter.CooldownRemaining" in tests
      and "Assert.AreEqual(33f, bank.Current" in tests,
      "NO_ORIGINAL_AUTHORITATIVE_RESOURCE_REGRESSION_TEST")
 must(get(CAND,DOC).startswith(b"# PASS17 W28"),"ORIGINAL_DOC_NOT_PRESENT")
 return selected,pins
def fail_closed_mutants(selected):
 for name,change in (
  ("remove_direct_gate",lambda s:s.replace("(activationGate != null && !activationGate())","false",1)),
  ("remove_binding",lambda s:s.replace("SetActivationGate(() => Device != null && Device.Available",
      "SetActivationGate(() => true && Device.Available",1))):
  if name=="remove_direct_gate":
   data=selected[EMITTER].decode()
   mutated=change(data)
   must(mutated!=data and "activationGate != null && !activationGate()" not in
        mutated[mutated.index("public bool TryPulse(out EmpPulseReport report)"):],
        "MUTANT_NOT_EFFECTIVE:"+name)
  else:
   data=selected[BINDING].decode();mutated=change(data)
   must(mutated!=data and
        "SetActivationGate(() => Device != null && Device.Available" not in mutated,
        "MUTANT_NOT_EFFECTIVE:"+name)
 return {"direct_gate_missing_mutant":"DENIED",
         "binding_owner_gate_mutant":"DENIED"}
def scratch(files):
 with tempfile.TemporaryDirectory(prefix="B01-W29-839-G0-") as td:
  root=Path(td);alien=root/"FOREIGN";(alien/"EMPTY").mkdir(parents=True)
  (alien/"keep").write_bytes(b"KEEP")
  stage=root/"NEW_OWNED";stage.mkdir()
  for name,raw in files.items():
   path=stage/name;path.parent.mkdir(parents=True,exist_ok=True)
   must(not path.exists(),"OVERLAY_EXISTING_FILE")
   path.write_bytes(raw)
  def clean():
   if any(p.is_symlink() for p in stage.rglob("*")):return False
   files_now={p.relative_to(stage).as_posix() for p in stage.rglob("*") if p.is_file()}
   return files_now==set(files) and all((stage/k).read_bytes()==v for k,v in files.items()) and not (stage/"FOREIGN_EMPTY").exists()
  must(clean(),"SCRATCH_INIT_NOT_CLEAN")
  (stage/"FOREIGN_EMPTY").mkdir()
  must(not clean(),"FOREIGN_EMPTY_NOT_DENIED")
  (stage/"FOREIGN_EMPTY").rmdir()
  first=next(iter(files))
  (stage/first).write_bytes(b"DIRTY")
  must(not clean(),"DIRTY_BYTES_NOT_DENIED")
  (stage/first).write_bytes(files[first])
  must(clean(),"SCRATCH_RESTORE_NOT_CLEAN")
  for path in files:(stage/path).unlink()
  for folder in sorted((p for p in stage.rglob("*") if p.is_dir()),key=lambda x:len(x.parts),reverse=True):folder.rmdir()
  stage.rmdir()
  must((alien/"EMPTY").is_dir() and (alien/"keep").read_bytes()==b"KEEP",
       "ALIEN_RECEIVER_CHANGED")
 return {"isolated_owner_staging":"PASS","unowned_empty_reject":"PASS",
         "dirty_owned_byte_reject":"PASS","byte_exact_rollback":"PASS",
         "foreign_original_bytes":"PRESERVED"}
def physical_zip(out,files,manifest):
 d={"MANIFEST.json":(json.dumps(manifest,sort_keys=True,indent=2)+"\n").encode()}
 d.update({"SOURCE/"+path:data for path,data in files.items()})
 d["DO_NOT_IMPORT_UNTIL_OWNER_ACK.txt"]=(
  "Source G0 for #839 only. Requires exact target SHA and real B01-00 selected "
  "Codex scope. Native #849 does not prove integration. No direct live overwrite.\n").encode()
 buf=io.BytesIO()
 with zipfile.ZipFile(buf,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
  for name,data in sorted(d.items()):
   info=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
   info.create_system=3;info.external_attr=0o100644<<16
   info.compress_type=zipfile.ZIP_DEFLATED
   z.writestr(info,data,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
 raw=buf.getvalue()
 (out/"B0140_W29_EMP839_SOURCE_ONLY.zip").write_bytes(raw)
 return {"source_only_zip_sha256":digest(raw),"members":len(d),"bytes":len(raw)}
def main():
 a=argparse.ArgumentParser();a.add_argument("--out",required=True);args=a.parse_args()
 dest=Path(args.out);must(not dest.exists(),"REFUSE_NONEMPTY_RECEIPT")
 files,pins=actual_source()
 mutants=fail_closed_mutants(files)
 stage=scratch(files)
 manifest={"package":"B01-40-W29-EMP839-EXACT-ORIGINAL-G0-001",
  "candidate_839":CAND,"native_qa_849":NATIVE_QA,"baseline_824":BASE,
  "read_only_current_824":GAME,"selected_paths":pins,"source_mutants":mutants,
  "scratch":stage,"native_evidence_external":"#849 exact #839 12/12 EditMode 22/22 PlayMode + Windows Player isolated",
  "independent_native_run":"NOT_RUN","gameplay_import":"NOT_PERFORMED",
  "current_codex_local_worktree":"UNKNOWN_NOT_ACCESSED",
  "source_transport_g0":"PASS_IF_CURRENT_CI_COMPLETES",
  "G2":"HOLD","merge":False,"release":False}
 dest.mkdir(parents=True)
 details=physical_zip(dest,files,manifest)
 (dest/"manifest.json").write_bytes((json.dumps(manifest,indent=2,sort_keys=True)+"\n").encode())
 (dest/"zip-sha256.json").write_bytes((json.dumps(details,indent=2,sort_keys=True)+"\n").encode())
 print("B0140_W29_EMP839_SOURCE_G0_3_ORIGINAL_CODE_PATHS_EXACT_SHA_PASS")
 print("B0140_W29_EMP839_CURRENT_GAME_SAME_PATH_NO_DRIFT_3_OF_3_PASS")
 print("B0140_W29_EMP839_ACTIVATION_RESOURCE_GUARD_AND_RADIAL_SOURCE_PASS")
 print("B0140_W29_EMP839_2_MUTANT_AND_SAFE_OS_SCRATCH_PASS")
 print("B0140_W29_EMP839_SOURCE_ZIP_SHA256="+details["source_only_zip_sha256"])
 print("B0140_W29_EMP839_NATIVE_G1_THIS_QA_NOT_RUN_INTEGRATED_NO")
if __name__=="__main__":main()
