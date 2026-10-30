import json,subprocess,sys
from pathlib import Path
BASE="3aaaec4027ee246fbfb6f209179f0e9507363473"
CRITICAL={
"Tools/B01/B01-62/P17W24CanonicalPathFixR2/CanonicalSingleAuthorityStoreV3R2.cs":"3689238a16d706eda1bf4905cba8029cd2fe77aa",
"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a"}
def git(*xs):return subprocess.check_output(["git",*xs],stderr=subprocess.PIPE).decode().strip()
def main():
 p=Path(__file__).parent;output=Path(sys.argv[1])
 if output.exists():raise AssertionError("RECEIPT_OCCUPIED")
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],check=True)
 for k,v in CRITICAL.items():
  if git("rev-parse",BASE+":"+k)!=v or git("rev-parse","HEAD:"+k)!=v:
   raise AssertionError("FROZEN_OWNER_CHANGED "+k)
 old=subprocess.check_output(["git","show",BASE+":Tools/B01/B01-62/P17W24CanonicalPathFixR2/CanonicalSingleAuthorityStoreV3R2.cs"]).decode()
 new=(p/"CanonicalSingleAuthorityStoreV3R3.cs").read_text()
 body=old.replace("CanonicalSingleAuthorityStore004","CanonicalSingleAuthorityStore005").replace("CanonicalSingleAuthorityStoreV3R2","CanonicalSingleAuthorityStoreV3R3").replace("StringComparison.OrdinalIgnoreCase","StringComparison.Ordinal").replace("StringComparer.OrdinalIgnoreCase","StringComparer.Ordinal")
 # Only the NEW source header is permitted beyond the exact versioned transformations.
 if not new.endswith(body):raise AssertionError("UNAUTHORIZED_R3_DIFFERENCE")
 if new.count("StringComparison.OrdinalIgnoreCase"):raise AssertionError("LINUX_WRONG_PATH_ALIAS_STILL_ACCEPTED")
 required={str(k) for k in ["CanonicalSingleAuthorityStoreV3R3.cs","CanonicalSlotSessionV3R3.cs",
 "Program.cs","Selected773StrictCanonicalPathR3.csproj","source_guard.py","README.md","manifest.json"]}
 paths=set(git("diff","--name-only",BASE,"HEAD").splitlines())
 expected={"Tools/B01/B01-62/P17W25StrictCanonicalPathR3/"+x for x in required}
 expected.add(".github/workflows/b01-62-w25-773-strict-case-path.yml")
 if paths!=expected:raise AssertionError("NOT_EXACT_EIGHT_NEW_ONLY_PATHS "+str(paths^expected))
 receipt={"base":BASE,"pins":CRITICAL,"new_paths":sorted(paths),"source_security":"EXACT_STRICT_PATH","native_unity":"NOT_RUN"}
 output.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
 print("W25_773_R3_IMMUTABLE_ORIGINAL_SOURCE_AND_ONLY_NEW_PATHS=PASS")
if __name__=="__main__":main()
