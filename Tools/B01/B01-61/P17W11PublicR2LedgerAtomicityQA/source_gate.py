#!/usr/bin/env python3
"""Fail-closed exact original Git blob pin + scope-only validation. Use from repository root."""
import hashlib
import json
import subprocess
import sys

BASE="6efe45961ed0ffdf183e8016e350978b9f3dc51c"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
PINS={
 "Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs":(BASE,"60fc22efec0e49d7688adde1fcb776093f2196af"),
 "Assets/B0123DebrisPlayground001/Runtime/RigDemoDefinition.cs":(BASE,"b7d1a503c89537316fe48dbb54f5997bb21d14ce"),
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":(W06,"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
 "Assets/B0123DebrisRepulse001/Runtime/DebrisRepulsionEmitter.cs":(W06,"2b3a8465c550bd49a5c98be5a8064a4ce0947f4b"),
}
OWN_PREFIX=("Tools/B01/B01-61/P17W11PublicR2LedgerAtomicityQA/","Assets/B0161W11DockAtomicityQA/")
OWN_ROOT_META="Assets/B0161W11DockAtomicityQA.meta"
OWN_WORKFLOW=".github/workflows/b01-61-p17w11-r2-ledger-atomicity.yml"
def git(*args):
    return subprocess.check_output(["git",*args],stderr=subprocess.PIPE)
def run():
    for path,(sha,pin) in PINS.items():
        actual=git("rev-parse",sha+":"+path).decode().strip()
        if actual!=pin:
            raise ValueError("PIN_DRIFT "+path+" "+actual)
        raw=git("show",sha+":"+path)
        calculated=hashlib.sha1(b"blob "+str(len(raw)).encode()+bytes([0])+raw).hexdigest()
        if calculated!=pin: raise ValueError("PIN_BYTE_DRIFT "+path)
        now=git("rev-parse","HEAD:"+path).decode().strip()
        if now!=pin:raise ValueError("SOURCE_EDIT_PROHIBITED "+path+" "+now)
    changes=git("diff","--name-only",BASE,"HEAD").decode().splitlines()
    if not changes:raise ValueError("NO_QA_DELTA")
    if any(not (f.startswith(OWN_PREFIX) or f==OWN_ROOT_META or f==OWN_WORKFLOW) for f in changes):
        raise ValueError("OUTSIDE_QA_SCOPE "+str(changes))
    print("SOURCE_GIT_BLOBS=4/4 PASS EXACT_W10_SHA="+BASE)
    print("QA_ONLY_FILES="+str(len(changes))+" PASS")
    print(json.dumps({"target_original":BASE,"w06_source":W06,"own_paths":changes,
         "native_unity":"NOT_RUN","source_qa":"LEDGER_TRANSACTION_HOLD",
         "codex_ack":False},sort_keys=True))
if __name__=="__main__":
    try:run()
    except (OSError,ValueError,subprocess.CalledProcessError) as e:
        print("QA_PROVENANCE_HOLD "+str(e),file=sys.stderr)
        sys.exit(2)
