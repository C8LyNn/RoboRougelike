#!/usr/bin/env python3
"""Independently receive exact original #885 SOLE Store (not install in game).
Run real ORIGINAL W30 C#9/.NET disk QA in an isolated OS temp Git worktree
before invoking this read-only Git source, mutation and log/receipt validator.
"""
import argparse,hashlib,json,pathlib,subprocess,sys
SAVE="78c1d54d24a76c931ea80965a50eca03fae4156a"
PARENT="e41c17d9e79317dbe8d4a16cee54baf301f8ee37"
V7="b4df10a7ad7e6c758f8331013f6b216b3a3689a3"
ADMISSION="c9c19665a4f9385e6479f7151097deb797026ade"
TOP="Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/"
STORE=TOP+"WholeGameEconomySlotV3.cs"
SESSION=TOP+"CanonicalWholeGameSessionV3.cs"
REGRESSION="Tools/B01/B01-62/P17W30AdmissionV3/Program.cs"
SRC={
 "ApprovedEconomyPolicyV7.cs":(V7,"Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs"),
 "CanonicalEconomySectionV7.cs":(V7,"Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs"),
 "EconomyV7AdmissionW29.cs":(ADMISSION,"Assets/B01Packages/ECONADMISSIONW29/Runtime/EconomyV7AdmissionW29.cs")
}
def need(x,msg):
 if not x:raise RuntimeError("B0140_W31_FAIL_CLOSED:"+msg)
def git(root,*a):
 p=subprocess.run(["git",*a],cwd=root,capture_output=True)
 need(p.returncode==0,"GIT:"+repr(a)+":"+p.stderr.decode(errors="replace")[-250:])
 return p.stdout.decode().strip()
def data(root,p):return (root/p).read_bytes()
def oid(b):return hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
def sha(b):return hashlib.sha256(b).hexdigest()
def control(store,session):
 return all(x in store for x in (
    "EconomyV7AdmissionW29.TryAdmit",
    "EconomyV7AdmissionW29.TryEncodeForCanonicalOwner",
    "EconomyV7AdmissionW29.TryDecodeForCanonicalOwner",
    "File.Replace(staging,authoritative,null)",
    "file.Flush(true)")) and "EconomyV7AdmissionW29.TryAdmit" in session and not any(
    x in store+session for x in ("SHA256.HashData(","Convert.ToHexString(",
       "File.Move(staging,authoritative,true)","File.Delete(authoritative)"))
def main():
 p=argparse.ArgumentParser()
 p.add_argument("--save-tree",required=True)
 p.add_argument("--qa-log",required=True)
 p.add_argument("--receipt",required=True)
 a=p.parse_args();root=pathlib.Path(a.save_tree).resolve()
 need(git(root,"rev-parse","HEAD")==SAVE,"NOT_SELECTED_IMMUTABLE_SAVE_HEAD")
 delta=git(root,"diff","--name-only",PARENT,SAVE).splitlines()
 need(len(delta)==13,"ORIGINAL_885_DELTA_CHANGED:"+str(len(delta)))
 need(STORE in delta and SESSION in delta,"ORIGINAL_ONE_WRITER_NOT_MODIFIED")
 for pth in [STORE,SESSION,REGRESSION]:
  need(oid(data(root,pth))==git(root,"rev-parse",SAVE+":"+pth),"ORIGINAL_885_SOURCE_BLOB_MUTATED:"+pth)
 for file,(ref,path) in SRC.items():
  content=data(root,"Tools/B01/B01-62/P17W30AdmissionV3/.source/"+file)
  need(oid(content)==git(root,"rev-parse",ref+":"+path),
       "ORIGINAL_PRODUCER_SOURCE_NOT_EXACT:"+file)
 originalStore=data(root,STORE).decode()
 originalSession=data(root,SESSION).decode()
 need(control(originalStore,originalSession),"SELECTED_SINGLE_OWNER_885_PROTECTION_MISSING")
 for mutated in (
  originalStore.replace("EconomyV7AdmissionW29.TryAdmit","REMOVED_ADMISSION"),
  originalStore.replace("EconomyV7AdmissionW29.TryDecodeForCanonicalOwner","DISABLE_READ_ADMISSION"),
  originalStore.replace("File.Replace(staging,authoritative,null)","File.Delete(authoritative)")):
  need(not control(mutated,originalSession),"UNSAFE_MUTATION_NOT_REJECTED")
 log=pathlib.Path(a.qa_log).read_text()
 total="B0162_W30_ADMISSION_SINGLE_WRITER_REAL_OS_TOTAL 4 PASS 4 FAIL 0"
 need(log.count(total)==1,"REAL_ORIGINAL_DOTNET_W30_4_CASES_MISSING_OR_REPEATED")
 for evidence in ("W30_MALFORMED_FREE_DENIED_VALID_SAVE_CAN_CONTINUE=PASS",
                  "W30_ORIGINAL_780_VALID_DIGEST_INVALID_FREE_COLD_QUARANTINE=PASS",
                  "W30_ZERO_PAID_NEVER_FREE_AND_STABLE_FULL_SAVE=PASS",
                  "W30_WIN1_FREE_WIN2_REBUILD_DISTINCT_RECEIPT_REPLAY_DENIED=PASS"):
  need(evidence in log,"REAL_ORIGINAL_V3_DISK_TEST_MISSING:"+evidence)
 fields={
  "package":"B01-40-W31-ORIGINAL-SAVE885-G0-REAL-OS",
  "selected_save_w30_single_writer_sha":SAVE,
  "selected_parent":PARENT,
  "selected_v7_sha":V7,
  "selected_admission_sha":ADMISSION,
  "original_scope_files":len(delta),
  "selected_original_blobs":{x:{"blob":git(root,"rev-parse",SAVE+":"+x),"sha256":sha(data(root,x))}
                            for x in [STORE,SESSION,REGRESSION]},
  "original_imported_sha":{name:sha(data(root,"Tools/B01/B01-62/P17W30AdmissionV3/.source/"+name)) for name in SRC},
  "real_disk_tests_pass":4,
  "original_admission_write_read_cold_quarantine":"PASS_REAL_SOURCE_OS_ONLY",
  "in_memory_negative_mutations_rejected":3,
  "native_Unity":"NOT_RUN",
  "game_typed_binder":"NOT_IMPLEMENTED",
  "installed_in_current_R3_game":"NO",
  "player_ZIP":"NOT_BUILT",
  "additional_save_writers_installed":0,
  "merge":False,"release":False
 }
 out=pathlib.Path(a.receipt)
 need(not out.exists(),"REFUSE_REPLACE_OLD_EVIDENCE")
 out.parent.mkdir(parents=True,exist_ok=True)
 encoded=(json.dumps(fields,indent=2,sort_keys=True)+"\n").encode()
 out.write_bytes(encoded)
 print("B0140_W31_ORIGINAL_SINGLE_WRITER_885_4_OF_4_REAL_OS_G0_PASS",sha(encoded))
 print("B0140_W31_ORIGINAL_SAVE_READ_WRITE_AND_ATOMIC_MUTATIONS_GUARDED_PASS")
 print("B0140_W31_SAVE885_SOURCE_G0_ONLY_UNITY_G1_NOT_RUN")
if __name__=="__main__":
 try:main()
 except Exception as e:
  print(e,file=sys.stderr)
  sys.exit(2)
