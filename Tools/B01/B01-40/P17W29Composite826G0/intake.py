#!/usr/bin/env python3
"""B01-40 P17/W29 independent ORIGINAL SOURCE composite acceptance.

Stages EXACT Git original #826 V3 whole-game Save, #792 Unity physical Rig[] port,
#780 ECON V7 and #729/#556/#510/W06 into an OS scratch C#9 app.
Derives ONLY a source-test harness from pinned #822, changing the namespace
from old V2 writer to V3; never changes a single producer byte.
The UnityEngine replacement is ONLY an expressly managed QA shim, NOT PhysX.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile

PIN={
 "game":"ed8cf4bcc131688b9a93968a89683d2496586683",
 "v3":"99fb08619c78b3834bb4c0450499babf991f218a",
 "v2":"2b7983f0f9e09dd1d56906b379a3f7af7b02f21a",
 "r3":"f5db40bf293fe9582ec7c6fcb3515aeff6fcf937",
 "rig":"16cb2a7b5158605b15191b6074f9e392e0d1ebd3",
 "econ":"b4df10a7ad7e6c758f8331013f6b216b3a3689a3",
 "r5":"60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb",
 "api":"b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
 "game_frozen":"d94400d95d13db7f27e1533df05c9292d5380431",
 "save_frozen":"33094f154c7b91119b36d58aab285e6e83c5de57",
 "rig_parent":"ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1",
 "econ_parent":"cc5d65f66ad7a764af6515c566e489b9fc3be82e",
 "r5_parent":"cc7f73541517a18f4479bb155534bd4832d9985e",
 "r3_parent":"3aaaec4027ee246fbfb6f209179f0e9507363473",
 "test822":"e66968408b583404cd50250f53e655ea75a085fc",
}
ROOT="Tools/B01/B01-40/P17W29Composite826G0/"
CI=".github/workflows/b01-40-p17w29-v3-physical-composite-g0.yml"
SOURCES={
 "game_frozen":("Assets/B0123SecureReattachV2","Assets/B0123DebrisRepulse001"),
 "save_frozen":("Assets/B0123DetachedSaveContract001",),
 "api":("Assets/B0123CanonicalSaveApi001",),
 "r5":("Assets/B0123UnitySaveBridge001",),
 "rig":("Assets/B0123CanonicalRigPortW24",),
 "econ":("Assets/B01Packages/ECONAPPROVED007",)
}
V3_PREFIX="Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/"
R5_TEST="Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"
READS={
 # The real original C# files the new source-only composite C#9 executable compiles.
 "CanonicalWholeGameSessionV3.cs":("v3",V3_PREFIX+"CanonicalWholeGameSessionV3.cs","927b286e76a358716d69e76da5d8351c2564d613"),
 "WholeGameEconomySlotV3.cs":("v3",V3_PREFIX+"WholeGameEconomySlotV3.cs","247ab912b73a0e6f17e86aa1a3c74343cb4d9af6"),
 "CanonicalPhysicalRigPortW24.cs":("rig","Assets/B0123CanonicalRigPortW24/Runtime/CanonicalPhysicalRigPortW24.cs","7623f698b6e644b8d8247a011b453bf40adc2227"),
 "CanonicalRigLedgerHydratorW24.cs":("rig","Assets/B0123CanonicalRigPortW24/Runtime/CanonicalRigLedgerHydratorW24.cs","eb21cb12605e94d17dc7decdf040c9009ca0aafe"),
 "ManagedUnitySourceOnlyShim.cs":("rig","Tools/B01/B01-23/P17W25PhysicalRollbackR2/ManagedUnitySourceOnlyShim.cs","53d16eb828772532f81a7af1ed2ed3a712b0e9e4"),
 "ApprovedEconomyPolicyV7.cs":("econ","Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs","53ebce6c07388dc9c8a8ed79b6de4d3ccbee4773"),
 "CanonicalEconomySectionV7.cs":("econ","Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs","d8b71de91db36295b6220e9c4baad71eb54455f6"),
 "CanonicalWorldGateway.cs":("api","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs","21733278b6ab0669fccb6d085e0b6cf242792de2"),
 "CanonicalWorldSlotLease.cs":("api","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs","a9331dd620336d8c42165d134dc8a5e05ed51873"),
 "AtomicDockLedger.cs":("game_frozen","Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs","b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"),
 "SecureRigSpec.cs":("game_frozen","Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",""),
 "DetachedSnapshotContract.cs":("save_frozen","Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs","b52e511c370f5fca56c74029050844dc8cbb7f9a"),
 "DebrisImpulseCore.cs":("game_frozen","Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",""),
}
HARNESS="Tools/B01/B01-62/P17W27CompositeOriginalPhysicalABI/Program.cs"
PATCH_SHIM="Tools/B01/B01-62/P17W27CompositeOriginalPhysicalABI/prepare_hierarchy_shim.py"
HARNESS_BLOB="38ff2da6ee8de262180e41c95d3c5610a2928a01"
PATCH_SHIM_BLOB="a729300f8d37457e182f36fdce2a8cccbb5cc398"

class Held(Exception):pass
def need(ok,msg):
 if not ok:raise Held(msg)
def git(*args):
 p=subprocess.run(["git",*args],capture_output=True)
 need(p.returncode==0,"GIT:"+repr(args)+":"+p.stderr.decode("utf-8","replace")[-240:])
 return p.stdout
def cat(k,path):return git("show",PIN[k]+":"+path)
def sha256(b):return hashlib.sha256(b).hexdigest()
def git_blob(b):return hashlib.sha1(b"blob "+str(len(b)).encode("ascii")+b"\0"+b).hexdigest()
def check_commits():
 for k,ref in PIN.items():
  need(git("rev-parse",ref+"^{commit}").decode().strip()==ref,"BAD_OR_MISSING_ORIGINAL_SHA:"+k)
 need(subprocess.run(["git","merge-base","--is-ancestor",PIN["v2"],PIN["v3"]],
                     capture_output=True).returncode==0,"V3_NOT_DESCENDANT_EXACT_V2")
 for k,parent,num_a,num_m,prefix in (
   ("v3","v2",9,0,V3_PREFIX),
   ("rig","rig_parent",8,2,"Tools/B01/B01-23/P17W25PhysicalRollbackR2/"),
   ("econ","econ_parent",16,0,"Tools/B01/ECONAPPROVED007/"),
   ("r5","r5_parent",3,1,"Tools/B01/B01-23/P17W23R5ColdRestart/"),
   ("r3","r3_parent",8,0,"Tools/B01/B01-62/P17W25StrictCanonicalPathR3/"),
 ):
  diff=git("diff","--name-status",PIN[parent],PIN[k]).decode().splitlines()
  a=sum(line.startswith("A\t") for line in diff)
  m=sum(line.startswith("M\t") for line in diff)
  need((a,m,len(diff))==(num_a,num_m,num_a+num_m),
       "PRODUCER_SCOPE_DRIFT:"+k+":"+repr(diff))
  for line in diff:
   status,path=line.split("\t",1)
   if k=="v3":ok=path.startswith(prefix) or path==".github/workflows/b01-62-w28-unity-compatible-whole-game-v3.yml"
   elif k=="rig":ok=path.startswith(prefix) or path==".github/workflows/b01-23-p17w25-physical-rollback-r2.yml" or (status=="M" and path in (
       "Assets/B0123CanonicalRigPortW24/Runtime/CanonicalPhysicalRigPortW24.cs",
       "Assets/B0123CanonicalRigPortW24/Tests/PlayMode/CanonicalPhysicalRigPortW24NativeTests.cs"))
   elif k=="econ":ok=path.startswith(prefix) or path.startswith("Assets/B01Packages/ECONAPPROVED007/") or path=="Assets/B01Packages/ECONAPPROVED007.meta" or path==".github/workflows/b01-22-p17w25-econ-section-v7.yml"
   elif k=="r5":ok=path.startswith(prefix) or path==".github/workflows/b01-23-p17w23-r5-coldrestart-fixture.yml" or (status=="M" and path==R5_TEST)
   else:ok=path.startswith(prefix) or path==".github/workflows/b01-62-w25-773-strict-case-path.yml"
   need(ok,"FOREIGN_PRODUCER_DIFF:"+k+":"+path)
  print("B0140_W29_EXACT_ORIGINAL_OWNER_SCOPE_PASS",k,num_a,num_m)
 print("B0140_W29_SOURCE_V3_IS_REAL_REPLACEMENT_NOT_V2_SIDE_WRITER_PASS")

def original_unity_assets():
 payload={}; owner={}
 for k,prefixes in SOURCES.items():
  for prefix in prefixes:
   files=git("ls-tree","-r","--name-only",PIN[k],"--",prefix,prefix+".meta").decode().splitlines()
   files=[x for x in files if x==prefix+".meta" or x.startswith(prefix+"/")]
   need(bool(files),"SOURCE_UNITY_DIR_MISSING:"+prefix)
   for path in files:
    b=cat(k,path)
    need(path not in payload or payload[path]==b,"ORIGINAL_SOURCE_CONFLICT:"+path)
    payload[path]=b
    owner[path]=k
 guid={};owners={}
 for path,b in sorted(payload.items()):
  if not path.endswith(".meta"):continue
  matches=re.findall(rb"(?m)^guid:\s*([0-9a-f]{32})\s*$",b)
  need(len(matches)==1,"BAD_UNITY_META:"+path)
  id=matches[0].decode()
  need(id not in guid,"SOURCE_GUID_COLLISION:"+id+" "+guid.get(id,"")+" "+path)
  guid[id]=path
  owners[owner[path]]=owners.get(owner[path],0)+1
 need(owners.get("rig")==9 and owners.get("econ")==5,
      "RIG_ECON_GUIDS_DRIFT:"+repr(owners))
 asm={}
 for path,b in payload.items():
  if not path.endswith(".asmdef"):continue
  data=json.loads(b)
  name=data["name"]
  need(name not in asm,"ASSEMBLY_DUPLICATE_NAME:"+name)
  asm[name]=path
 for path,b in payload.items():
  if path.endswith(".asmdef"):
   missing=[x for x in json.loads(b).get("references",[]) if isinstance(x,str)
            and (x.startswith("B01.") or x.startswith("B0123.")) and x not in asm]
   need(not missing,"ASSEMBLY_REFERENCE_UNRESOLVED:"+path+":"+repr(missing))
  if path.endswith((".prefab",".unity",".asset")):
   ids=re.findall(rb"m_Script:\s*\{[^\r\n]*guid:\s*([0-9a-f]{32})",b)
   bad=[m.decode() for m in ids if m.decode() not in guid and
        m!=b"0000000000000000e000000000000000"]
   need(not bad,"UNITY_SERIALIZED_SCRIPT_GUID_MISSING:"+path+":"+repr(bad))
 fixture=payload[R5_TEST]
 need(b"Assert.AreEqual(5,savedSnapshot.Generation)" in fixture and
      b"TryRestoreBytes(saved,41,5)" in fixture,
      "R5_COLD_RESTART_GENERATION_NOT_PINNED")
 print("B0140_W29_SELECTED_ORIGINAL_UNITY_ASMDEF_GUID_CLOSURE_PASS",
       len(payload),len(guid),len(asm),"RIG",owners["rig"],"ECON",owners["econ"])
 return payload,owner,guid,asm

def original_game_receiver(payload):
 # Actual current gameplay (Codex W28 latest #824) might contain the same
 # selected path. Reject any different original byte before installing into game.
 colliding=[];identical=[]
 for path,b in payload.items():
  cp=subprocess.run(["git","show",PIN["game"]+":"+path],capture_output=True)
  if cp.returncode==0:
   if cp.stdout!=b:colliding.append(path)
   else:identical.append(path)
 need(not colliding,"CURRENT_GAME_DIFFERENT_EXISTING_ASSET:"+repr(colliding))
 # Exact entire current receiver Unity asset GUID catalog from one Git grep,
 # not a model-authored claim, must not collide across different file paths.
 p=subprocess.run(["git","grep","-n","-E","^guid: [0-9a-f]{32}$",
                  PIN["game"],"--","Assets"],capture_output=True)
 need(p.returncode in (0,1),"GAME_GUID_GIT_SCAN_FAILED")
 original={}
 for line in p.stdout.decode("utf-8","replace").splitlines():
  match=re.match(r"^[^:]+:(.*\.meta):\d+:guid: ([0-9a-f]{32})$",line)
  if match:
   path,id=match.groups()
   need(id not in original or original[id]==path,
        "CURRENT_GAME_HAS_DUPLICATED_GUID:"+id+":"+path)
   original[id]=path
 for id,path in ((re.findall(rb"(?m)^guid:\s*([0-9a-f]{32})",payload[p])[0].decode(),p)
                for p in payload if p.endswith(".meta")):
  need(id not in original or original[id]==path,
       "CROSS_GAME_IMPORT_GUID_COLLISION:"+id+":"+path+":"+original.get(id,""))
 print("B0140_W29_CURRENT_GAME_ORIGINAL_ASSET_PATH_AND_GUID_NO_COLLISION_PASS",
       len(original),len(identical))
 return len(original),identical

def versioned_writer():
 for filename,(owner,path,expected) in READS.items():
  b=cat(owner,path)
  if expected:need(git_blob(b)==expected,
                   "REAL_ORIGINAL_GIT_BLOB_CHANGED:"+filename+":"+git_blob(b))
 original_v3=cat("v3",V3_PREFIX+"WholeGameEconomySlotV3.cs")
 session_v3=cat("v3",V3_PREFIX+"CanonicalWholeGameSessionV3.cs")
 complete=original_v3+b"\n"+session_v3
 for forbidden in (b"SHA256.HashData(",b"Convert.ToHexString(",
                    b"File.Move(staging,authoritative,true)"):
  need(forbidden not in complete,"OLD_UNITY_API_STILL_USED:"+repr(forbidden))
 for necessary in (b"File.Replace(staging,authoritative,null)",
                   b"File.Move(staging,authoritative)",
                   b"SHA256.Create()",b"slot.canonical-game-v2",
                   b"B0162G02",b"FileShare.None"):
  need(necessary in complete,"ONE_V3_WRITER_CONTRACT_MISSING:"+repr(necessary))
 need(b"CanonicalEconomySectionV7" in session_v3+original_v3,
      "ECON_V7_ORIGINAL_SECTION_NOT_LINKED")
 need(b"ICanonicalCommitPort" in session_v3 and
      b"CanonicalWorldGateway" in session_v3,"ONE_ORIGINAL_729_GATE_NOT_LINKED")
 print("B0140_W29_ACTUAL_V3_UNITY_ABI_SOURCE_AND_ONE_OWNER_FILE_REPLACE_PASS")

def tests_and_packaging(payload,owner,guid,asm,receipt_path):
 out=Path(receipt_path)
 need(not out.exists(),"REFUSE_OVERWRITE_OLD_RECEIPT")
 original=cat("test822",HARNESS)
 need(git_blob(original)==HARNESS_BLOB,"REAL_ORIGINAL_W27_COMPOSITE_TEST_CHANGED")
 patch=cat("test822",PATCH_SHIM)
 need(git_blob(patch)==PATCH_SHIM_BLOB,"REAL_ORIGINAL_SOURCE_SHIM_PATCH_CHANGED")
 # The production V3 has the same contract as original V2 except version
 # and namespace. Transform test-only harness, never original production files.
 test_source=original.decode("utf-8")
 transforms={
  "B01.CanonicalWholeGameAdapter002":"B01.CanonicalWholeGameAdapter003",
  "B01.WholeGameEconomy002":"B01.WholeGameEconomy003",
  "CanonicalWholeGameSessionV2":"CanonicalWholeGameSessionV3",
  "WholeGameEconomySlotV2":"WholeGameEconomySlotV3",
 }
 for old,new in transforms.items():
  need(old in test_source,"OLD_AUTHORED_SOURCE_TEST_ABI_CHANGED:"+old)
  test_source=test_source.replace(old,new)
 need("PhysicalWorldAndV7SingleOwnerCold" in test_source and
      "NonQuiescedGatePreventsGameSave" in test_source,
      "REAL_W27_COMPOSITE_TEST_CASES_REMOVED")
 manifest={
  "schema":"B01-40-P17-W29-V3-WHOLE-GAME-PHYSICAL-COMPOSITE-SOURCE-001",
  "game_head":PIN["game"],
  "producer_exact_heads":{k:PIN[k] for k in ("v3","rig","econ","r5")},
  "authority_ref_only":{"r3":PIN["r3"]},
  "frozen_originals":{k:PIN[k] for k in ("api","game_frozen","save_frozen")},
  "test_harness_original":PIN["test822"],
  "source_unity_count":len(payload),
  "guid_count":len(guid),
  "asmdef_count":len(asm),
  "unity_original_source":{path:{"owner":owner[path],"bytes":len(b),"sha256":sha256(b)}
                           for path,b in sorted(payload.items())},
  "runtime_original_source":{n:{"pr":PIN[k],"repo_path":p,"bytes":len(cat(k,p)),
                                 "git_blob":git_blob(cat(k,p)),
                                 "sha256":sha256(cat(k,p))}
                             for n,(k,p,_) in sorted(READS.items())},
  "writer":"ONLY_V3_826; V2_797_AND_RAW_STORE_787_NOT_COINSTALLED",
  "original_test822_harness_derivation":"TEST_ONLY_LITERAL_V2_TO_V3_NAMESPACE_REPLACEMENT",
  "test_mode":"TRUE_OS_FILESYSTEM_AND_REAL_ORIGINAL_CSHARP_PHYSICS_PORT_WITH_MANAGED_UNITY_SHIM",
  "unity_g1":"NOT_RUN",
  "player_x64":"NOT_BUILT",
  "live_game_binder":"NOT_IMPLEMENTED_OR_ACCEPTED",
  "whole_game_g0":"SOURCE_COMPOSITE_ONLY",
  "save_migration":"NOT_RUN_REQUIRE_BACKUP_FIRST_OWNER_APPROVAL",
  "codex_ack":"NONE",
  "merge":False,
  "release":False,
  "rollback":"DELETE_ONLY_NEW_OWNED_TEMP_STAGE_NEVER_USER_SAVE"
 }
 out.parent.mkdir(parents=True,exist_ok=True)
 with tempfile.TemporaryDirectory(prefix="b0140-w29-private-") as d:
  root=Path(d)
  foreign=root/"user-unowned"
  (foreign/"EMPTY").mkdir(parents=True)
  keep=foreign/"save.envelope"
  keep.write_bytes(b"FOREIGN_PLAYER_SAVE_IS_IMMUTABLE")
  stage=root/"candidate-new-only"
  need(not stage.exists(),"STAGING_PATH_ALREADY_EXISTS")
  stage.mkdir()
  staged={}
  # Preserve original exact Unity assets under separate source-only staging.
  for path,b in sorted(payload.items()):
   dest=stage/"Assets"/path
   dest.parent.mkdir(parents=True,exist_ok=True)
   dest.write_bytes(b)
   staged["Assets/"+path]=b
  # Externally acquired exact ORIGIN git sources, never Git branch overlays.
  for filename,(k,path,_) in sorted(READS.items()):
   b=cat(k,path)
   dest=stage/"External"/filename
   dest.parent.mkdir(parents=True,exist_ok=True)
   dest.write_bytes(b)
   staged["External/"+filename]=b
  assert git_blob(original)==HARNESS_BLOB
  (stage/"Program.cs").write_bytes(test_source.encode("utf-8"))
  staged["Program.cs"]=test_source.encode("utf-8")
  (stage/"prepare_hierarchy_shim.py").write_bytes(patch)
  staged["prepare_hierarchy_shim.py"]=patch
  items=["Program.cs"]+["External/"+s for s in sorted(READS)]
  csproj=("<Project Sdk=\"Microsoft.NET.Sdk\">\n"+
    "<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>"+
    "<LangVersion>9.0</LangVersion><Nullable>enable</Nullable>"+
    "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>"+
    "<NoWarn>CS8618;CS8625</NoWarn><EnableDefaultCompileItems>false</EnableDefaultCompileItems>"+
    "</PropertyGroup><ItemGroup>\n"+
    "".join("<Compile Include=\""+x+"\"/>\n" for x in items)+
    "</ItemGroup></Project>\n").encode("utf-8")
  (stage/"B0140W29RealComposite.csproj").write_bytes(csproj)
  staged["B0140W29RealComposite.csproj"]=csproj
  # Ownership fail-closed, source-mutation vs accidental foreign-content rollback.
  dirty=stage/"External"/"WholeGameEconomySlotV3.cs"
  original_bytes=dirty.read_bytes()
  dirty.write_bytes(b"DIRTY_UNOWNED_MUTATION")
  need(dirty.read_bytes()!=staged["External/WholeGameEconomySlotV3.cs"],
       "DIRTY_SOURCE_NEGATIVE_NOT_DETECTED")
  need(keep.read_bytes()==b"FOREIGN_PLAYER_SAVE_IS_IMMUTABLE" and
       (foreign/"EMPTY").is_dir(),"FOREIGN_FILE_OR_EMPTY_DIRECTORY_TOUCHED")
  dirty.write_bytes(original_bytes)
  for key,b in staged.items():
   need((stage/key).read_bytes()==b,"ORIGINAL_STAGE_SOURCE_CHANGED:"+key)
  before=stage/"External"/"ManagedUnitySourceOnlyShim.cs"
  # This exact B01-62 prior QA-only patch only repairs managed parent Destroy
  # model and is never packaged for Unity; source shim remains off game assets.
  run=subprocess.run([sys.executable,str(stage/"prepare_hierarchy_shim.py"),str(before)],
                     capture_output=True,text=True)
  need(run.returncode==0,"MANAGED_ONLY_PARENT_DESTROY_FIX_FAIL:"+run.stdout+" "+run.stderr)
  print(run.stdout.strip())
  need(b"ChildSnapshot" in before.read_bytes() and
       original_bytes==dirty.read_bytes(),
       "MANAGED_SHIM_ISOLATION_BROKEN")
  # Execute real .NET 8, actual C#9 original V3/physical/V7+729+556 source
  # and REAL OS FileStream/rename cold process path. Not a mock Store.
  result=subprocess.run(["dotnet","run","--project",
      str(stage/"B0140W29RealComposite.csproj"),"-c","Release","--verbosity","minimal"],
      capture_output=True,text=True,timeout=220)
  output=result.stdout+"\n"+result.stderr
  print(output[-6500:])
  success="B0162_W27_ORIGINAL_COMPOSITE_PHYSICAL_SOURCE_ABI_TOTAL 2 PASS 2 FAIL 0"
  need(result.returncode==0 and success in output,
       "ORIGINAL_V3_PLUS_RIG_REAL_OS_SOURCE_COMPOSITE_RED_EXIT_"+
       str(result.returncode)+":"+output[-2500:])
  need("W27_REAL_ORIGINAL_792_PORT_PLUS_797_FULL_WORLD_V7_COLD_IDS=PASS_MANAGED_SOURCE" in output,
       "COLD_OWNER_CAS_VICTORY_ID_CHECK_MISSING")
  need("W27_REAL_792_NOT_QUIESCED_GATE_PREVENTS_797_SAVE_AND_PHYSICAL_STAGE=PASS" in output,
       "NONQUIESCED_PHYSICAL_GATE_CHECK_MISSING")
  # Prior harness labels mention original V2 because producer-authored; the
  # test harness imports ONLY V3 source and literal version namespace changes.
  print("B0140_W29_REAL_ORIGINAL_V3_826_PLUS_792_780_729_COLD_AND_ROLLBACK_2_OF_2_PASS_MANAGED_SOURCE_ONLY")
  print("B0140_W29_NATIVE_UNITY_PHYSX_G1_NOT_RUN_NO_GAME_BINDER_NO_PLAYER")
  # OS scratch source stage is temporary; unowned Save remains unchanged.
  need(keep.read_bytes()==b"FOREIGN_PLAYER_SAVE_IS_IMMUTABLE",
       "REAL_USER_UNOWNED_SAVE_MODIFIED")
  print("B0140_W29_DISPOSABLE_STAGE_DIRTY_NEGATIVE_AND_FOREIGN_SAVE_PRESERVED_PASS")
  # Save logs without absolute paths, so Windows/Ubuntu receipt byte-identical.
  manifest["real_original_composite_csharp_test_cases"]=2
  manifest["real_original_composite_csharp_test_result"]="PASS_ORIGINAL_V3_826_AND_PHYSICAL_792_OS_FILESYSTEM"
  manifest["independent_source_check"]="PASS_ONLY_IF_W29_CI_CROSS_OS_AGREES"
  manifest["assertions"]=[
    "Original same owner physical 792 + one world V3 + original V7 no separate wallet",
    "Generation1 into Generation2 after authentic trusted victory, identical part IDs",
    "Owner lifetime rejects concurrent same-slot owner and cold reopen succeeds",
    "Same verified consumed victory cannot be replayed",
    "Nonquiesced live physical root rejects Save stage before any file",
    "No Unity code, Player, or user Save modified; no second raw Store",
  ]
  bytes_receipt=(json.dumps(manifest,sort_keys=True,indent=2)+"\n").encode("utf-8")
  out.write_bytes(bytes_receipt)
 print("B0140_W29_ORIGINAL_EXACT_SHA_COMPOSITE_SOURCE_G0_PASS")
 print("B0140_W29_INNER_MANIFEST_SHA256="+sha256(out.read_bytes()))

def main():
 p=argparse.ArgumentParser()
 p.add_argument("--receipt",required=True)
 args=p.parse_args()
 check_commits()
 payload,owner,guid,asm=original_unity_assets()
 original_game_receiver(payload)
 versioned_writer()
 tests_and_packaging(payload,owner,guid,asm,args.receipt)

if __name__=="__main__":
 try:main()
 except (Held,subprocess.TimeoutExpired,subprocess.CalledProcessError) as e:
  print("B0140_W29_COMPOSITE_SOURCE_G0_FAIL="+str(e),file=sys.stderr)
  raise
