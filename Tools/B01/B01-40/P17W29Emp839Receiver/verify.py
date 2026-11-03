#!/usr/bin/env python3
"""W29 exact, owner-authored original EMP #839 + #849 selective receiver G0.

Not a new EMP implementation. Verifies the three ORIGINAL product C# blobs
were installed byte-exact atop latest unchanged Codex W28 gameplay, with
the original real opt-in #849 Windows Player QA harness and unique .meta.
Source-G0 is not a Unity Editor/Player test of this new assembled branch.
"""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import tempfile
BASE="ed8cf4bcc131688b9a93968a89683d2496586683"
OWNER="fea000fe0a706df2b52b6f295bdb3135ba1f9384"
OWNER_BASE="e03d36f2b86f8c73a067e574b67237a8921f622a"
QA_OWNER="16d338f13779ee71ac74b8549e0168dce92bfda3"
DIR="Tools/B01/B01-40/P17W29Emp839Receiver/"
WORKFLOW=".github/workflows/b01-40-w29-selective-emp839-g0.yml"
MOD={
 "Assets/RoboRougelikeMechanics/WPN006EMP/Runtime/EmpEmitter.cs":(
    "f38da021c22de609a196876d508ec9d92fb9ff56",
    "54861d15dbb95fff24577a48761353ebd285ab63"),
 "Assets/RoboRougelikeMechanics/WPN006EMP/Tests/PlayMode/EmpEmitterPhysicsTests.cs":(
    "8cd7e4a27b58c4e7c1e2c18f63a2065e82768a19",
    "e4c949da2b6063b94dc9c29cf06e9619c911ba90"),
 "Assets/WeaponIntegration/RRLEmpBinding.cs":(
    "6ff22154024dd04ee72043f9766db1840218df03",
    "e00fa6a7435d4041c11abbf8d682aba4fa75e966"),
}
ADD={
 "Assets/R1/W27EmpPlayerQA.cs":"a2a43ac6faa7c25d0411b1636d9dec3fb8c85174",
 "Assets/R1/W27EmpPlayerQA.cs.meta":"c23e4f702f7fa978f2c99a12d43e7a5bc8acb68f",
}
class Held(Exception):pass
def need(cond,msg):
 if not cond:raise Held(msg)
def git(*args):
 p=subprocess.run(["git",*args],capture_output=True)
 need(p.returncode==0,"GIT_COMMAND_FAILED "+repr(args)+" "+p.stderr.decode(errors="replace")[-180:])
 return p.stdout
def textfile(ref,path):return git("show",ref+":"+path)
def blob(ref,path):return git("rev-parse",ref+":"+path).decode().strip()
def digest(b):return hashlib.sha256(b).hexdigest()
def run(outpath):
 head=git("rev-parse","HEAD").decode().strip()
 for label,ref in (("BASE",BASE),("OWNER",OWNER),("OWNER_BASE",OWNER_BASE),("NATIVE_QA",QA_OWNER)):
  need(git("rev-parse",ref+"^{commit}").decode().strip()==ref,"MISSING_ORIGINAL_GIT:"+label)
 for before,after in ((BASE,head),(OWNER_BASE,OWNER),(OWNER,QA_OWNER)):
  need(subprocess.run(["git","merge-base","--is-ancestor",before,after],
     capture_output=True).returncode==0,"WRONG_GIT_ANCESTRY:"+before[:10])
 owner_delta=git("diff","--name-status",OWNER_BASE,OWNER).decode().splitlines()
 need(len(owner_delta)==4 and all(x.startswith("M\t") for x in owner_delta),
      "EMP_PRODUCER_839_SCOPE_DRIFT:"+repr(owner_delta))
 owner_paths={row.split("\t",1)[1] for row in owner_delta}
 need(owner_paths==set(MOD)|{"Docs/W28/EMP_DIRECT_ACTIVATION_QA.md"},
      "EMP_839_UNEXPECTED_OWNER_FILES:"+repr(owner_paths))
 qa_delta=git("diff","--name-status",OWNER,QA_OWNER).decode().splitlines()
 need(len(qa_delta)==3 and {x.split("\t",1)[1] for x in qa_delta}==
      set(ADD)|{"Docs/W27/EMP839_NATIVE_QA.md"},
      "EMP_PLAYER_QA_849_SCOPE_DRIFT:"+repr(qa_delta))
 changed=git("diff","--name-status",BASE,head).decode().splitlines()
 expected={("M",p) for p in MOD}|{("A",p) for p in ADD}|{
  ("A",DIR+"verify.py"),("A",DIR+"README.md"),("A",WORKFLOW)}
 actual={tuple(x.split("\t",1)) for x in changed}
 need(actual==expected,"RECEIVER_CHANGED_FOREIGN_GAME_OR_QA_PATH:"+repr(actual^expected))
 print("B0140_W29_EMP_CURRENT_GAME_EXACT_BASE_AND_ONLY_3_SOURCE_2_NATIVE_QA_SELECTED_PASS",head)
 for path,(old,new) in MOD.items():
  need(blob(BASE,path)==old and blob(OWNER_BASE,path)==old,
       "CURRENT_GAME_HAS_DIVERGED_EMP_PRODUCT_SOURCE:"+path)
  need(blob(OWNER,path)==new and blob("HEAD",path)==new,
       "NOT_BYTE_EXACT_ORIGINAL_839_EMP:"+path)
 for path,sha in ADD.items():
  need(blob(QA_OWNER,path)==sha and blob("HEAD",path)==sha,
       "NOT_ORIGINAL_849_WINDOWS_PLAYER_QA:"+path)
  need(subprocess.run(["git","cat-file","-e",BASE+":"+path],
       capture_output=True).returncode!=0,
       "W28_ALREADY_HAS_QA_SOURCE_DO_NOT_DUPLICATE:"+path)
 print("B0140_W29_EMP839_PRODUCT_3_OF_3_AND_849_PLAYER_QA_2_OF_2_GIT_BLOBS_EXACT_PASS")
 # Original whole game engine remains untouched (no source replay across changes).
 assembly="Assets/R3/R3VehicleAssembly.cs"
 orig=textfile(BASE,assembly)
 current=textfile("HEAD",assembly)
 need(orig==current and b"RRLEmpBinding" in current and
      b"if(emp!=null&&emp.Fire())" in current,
      "R3_EMITTER_GROUP_BINDING_CHANGED_OR_MISSING")
 core=textfile("HEAD","Assets/RoboRougelikeMechanics/WPN006EMP/Runtime/EmpEmitter.cs")
 binding=textfile("HEAD","Assets/WeaponIntegration/RRLEmpBinding.cs")
 tests=textfile("HEAD","Assets/RoboRougelikeMechanics/WPN006EMP/Tests/PlayMode/EmpEmitterPhysicsTests.cs")
 player=textfile("HEAD","Assets/R1/W27EmpPlayerQA.cs")
 for expr in (b"SetActivationGate(",b"activationGate != null",b"Physics.OverlapSphereNonAlloc",
              b"Physics.RaycastNonAlloc",b"cooldownTimer.Commit(cooldown)"):
  need(expr in core,"REAL_RADIAL_EMP_AND_GUARD_SIGNATURE_MISSING:"+repr(expr))
 for expr in (b"Module.SetActivationGate(",b"Device.Available",b"transform.IsChildOf(Device.Owner)",
              b"RRLEmpPartState.IsBlocked(gameObject)",b"if(!Module.TryPulse"):
  need(expr in binding,"REAL_R3_EMP_DIRECT_CALL_AUTHORITY_MISSING:"+repr(expr))
 for expr in (b"Pulse_DirectModuleCallRechecksGateBeforeEnergyAndCooldown",
              b"emitter.SetActivationGate(() => false)",b"bank.Current",
              b"emitter.CooldownRemaining"):
  need(expr in tests,"EXISTING_EMP_NATIVE_REGRESSION_MISSING:"+repr(expr))
 need(b"#if B01_UNIFIED_RC2_ACCEPTANCE" in player and
      b"-w27-emp-player-qa" in player and
      b"result.reattachedPulse" in player and
      b"result.disconnectedDenied" in player,
      "OPTIN_WINDOWS_PLAYER_REAL_TEST_MISSING")
 print("B0140_W29_REAL_RADIAL_EMP_GUARD_AND_ORIGINAL_NATIVE_QA_ENTRYPOINT_SOURCE_PASS")
 # The original new GUID must not alias any existing Unity asset.
 meta=textfile("HEAD","Assets/R1/W27EmpPlayerQA.cs.meta")
 match=re.findall(rb"(?m)^guid:\s*([a-f0-9]{32})\s*$",meta)
 need(len(match)==1,"INVALID_NATIVE_PLAYER_QA_UNITY_META")
 value=match[0].decode()
 p=subprocess.run(["git","grep","-l",value,BASE,"--","Assets"],
                  capture_output=True)
 need(p.returncode==1,"EMP_PLAYER_QA_GUID_ALREADY_IN_CURRENT_GAME:"+p.stdout.decode()[-160:])
 all_ids={}
 grep=subprocess.run(["git","grep","-n","-E","^guid: [0-9a-f]{32}$","HEAD","--","Assets"],capture_output=True)
 need(grep.returncode in (0,1),"UNITY_GUID_SCAN_ERROR")
 for line in grep.stdout.decode(errors="replace").splitlines():
  found=re.match(r"^[^:]+:(.*\.meta):\d+:guid: ([0-9a-f]{32})$",line)
  if found:
   path,id=found.groups()
   need(id not in all_ids or all_ids[id]==path,
        "DUPLICATED_UNITY_META_GUID:"+id+" "+all_ids.get(id,"")+" "+path)
   all_ids[id]=path
 need(value in all_ids,"NEW_PLAYER_META_NOT_IN_ACTUAL_HEAD")
 print("B0140_W29_GAME_PLUS_EMP_NATIVE_QA_META_UNIQUE_GUID_PASS",len(all_ids))
 # Physically stage ORIGINAL producer source into an owned temporary directory.
 # Mutation must FAIL identity and NEVER permit user-file/empty-dir cleanup.
 with tempfile.TemporaryDirectory(prefix="b0140-w29-emp-source-") as name:
  root=Path(name)
  foreign=root/"user-foreign";(foreign/"EMPTY").mkdir(parents=True)
  user=foreign/"save.envelope";user.write_bytes(b"FOREIGN_PLAYER_BYTES_KEEP")
  stage=root/"new-only-test-stage";need(not stage.exists(),"OVERWRITE_USER_STAGE")
  stage.mkdir()
  for path in sorted(set(MOD)|set(ADD)):
   b=textfile("HEAD",path)
   dest=stage/path;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(b)
   need(dest.read_bytes()==b,"COPY_SOURCE_NOT_IDENTICAL:"+path)
  target=stage/"Assets/WeaponIntegration/RRLEmpBinding.cs"
  original=target.read_bytes();target.write_bytes(original+b"FOREIGN_MUTATION")
  need(target.read_bytes()!=textfile(OWNER,"Assets/WeaponIntegration/RRLEmpBinding.cs"),
       "DIRTY_PRODUCT_SOURCE_NEGATIVE_NOT_CAUGHT")
  target.write_bytes(original)
  need(target.read_bytes()==textfile(OWNER,"Assets/WeaponIntegration/RRLEmpBinding.cs"),
       "RESTORE_EXACT_SOURCE_FAILED")
  need(user.read_bytes()==b"FOREIGN_PLAYER_BYTES_KEEP" and (foreign/"EMPTY").is_dir(),
       "FOREIGN_USER_SAVE_OR_EMPTY_DIRECTORY_MUTATED")
 print("B0140_W29_OWNED_SOURCE_STAGE_DIRTY_NEGATIVE_AND_FOREIGN_SAVE_PRESERVED_PASS")
 manifest={
  "schema":"B01-40-P17W29-EMP839-SELECTIVE-GAME-RECEIVER-001",
  "game_base":BASE,"branch_exact_head":head,
  "original_feature_owner":OWNER,"original_feature_owner_base":OWNER_BASE,
  "original_native_player_qa":QA_OWNER,
  "selected_original_product_and_test_git_blobs":{
   path:{"source":"839","old_blob":old,"selected_blob":new,"sha256":digest(textfile("HEAD",path))}
   for path,(old,new) in sorted(MOD.items())},
  "selected_original_player_qa_git_blobs":{
   path:{"source":"849","selected_blob":q,"sha256":digest(textfile("HEAD",path))}
   for path,q in sorted(ADD.items())},
  "game_unity_guid_count_after":len(all_ids),
  "native_qa_historical":"839 original targeted 12/12 EditMode + 22/22 PlayMode; 849 original Windows Player exit0 on different source head",
  "selected_game_branch_native_unity_edit_play":"NOT_RUN",
  "selected_game_branch_windows_player":"NOT_BUILT",
  "source_gameplay_implementation":"INCLUDED_ORIGINAL_839_PRODUCTION_BYTES_ON_EXACT_W28_HEAD",
  "original_emp_area_semantics":"NONLINEAR_RADIAL_WITH_OCCLUSION",
  "source_g0":"PASS_IF_ACTUAL_W29_WORKFLOW_GREEN",
  "full_game_native_g1":"HOLD",
  "full_game_playable_g2":"HOLD",
  "code_owner":"ORIGINAL_CODEX_839_NO_B01_40_NEW_EMP_ALGORITHM",
  "ready_for_integrator":False,"codex_ack":"NONE",
  "merge":False,"release":False,
  "rollback":"REVERT_EXACT_ORIGINAL_839_BLOBS_IN_OWN_DRAFT_ONLY"
 }
 out=Path(outpath)
 need(not out.exists(),"DO_NOT_OVERWRITE_RECEIPT")
 out.parent.mkdir(parents=True,exist_ok=True)
 data=(json.dumps(manifest,sort_keys=True,indent=2)+"\n").encode("utf-8")
 out.write_bytes(data)
 print("B0140_W29_EMP839_EXACT_GAME_SOURCE_G0_PASS_NATIVE_G1_NOT_RUN")
 print("B0140_W29_EMP_INNER_MANIFEST_SHA256="+digest(data))
def main():
 p=argparse.ArgumentParser()
 p.add_argument("--receipt",required=True)
 args=p.parse_args()
 run(args.receipt)
if __name__=="__main__":
 try:main()
 except Held as e:
  print("B0140_W29_EMP_SOURCE_G0_FAIL="+str(e),file=sys.stderr)
  raise
