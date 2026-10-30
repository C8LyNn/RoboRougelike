#!/usr/bin/env python3
"""Immutable producer source & test-only scope guard, never grants Save/Load acceptance."""
import hashlib
import json
import pathlib
import subprocess
import sys
ROOT=pathlib.Path(__file__).resolve().parents[4]
OWNER="d94400d95d13db7f27e1533df05c9292d5380431"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
OWN="Tools/B01/B01-61/P17W13ColdLoadOriginalLedgerQA/"
CI=".github/workflows/b01-61-p17w13-coldload-ledger.yml"
PINS={
 "Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs":(OWNER,"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"),
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":(W06,"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
 "Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs":(OWNER,"328977b4cdcdcd7c9f128c01f11ff67f8e46086a"),
}
def git(*a):return subprocess.check_output(["git",*a],cwd=ROOT,stderr=subprocess.PIPE)
def verify():
    changed=git("diff","--name-only",OWNER,"HEAD").decode().splitlines()
    if len(changed)!=6 or any(not (p.startswith(OWN) or p==CI) for p in changed):
        raise ValueError("QA_SCOPE_DRIFT "+repr(changed))
    for p,(ref,sha) in PINS.items():
        raw=git("show",ref+":"+p)
        digest=hashlib.sha1(b"blob "+str(len(raw)).encode()+bytes([0])+raw).hexdigest()
        if digest!=sha:raise ValueError("ORIGINAL_PIN_MISMATCH "+p)
        if git("rev-parse","HEAD:"+p).decode().strip()!=sha:
            raise ValueError("FROZEN_PRODUCER_SOURCE_CHANGED "+p)
    source=git("show",OWNER+":Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs").decode()
    for missing in ("LoadSnapshot(", "RestoreSnapshot(", "ExportSnapshot(", "CaptureSnapshot("):
        if missing in source:raise ValueError("NEW_ADAPTER_SOURCE_REQUIRES_RETARGET "+missing)
    if "private readonly DebrisImpulseLedger consumed" not in source:
        raise ValueError("ORIGINAL_LEDGER_CHANGED")
    print("IMMUTABLE_PRODUCER_SHA="+OWNER+" EXACT_GIT_BLOBS=3/3 QA_ONLY_NEW_PATHS=6 PASS")
    print(json.dumps({"source_sha":OWNER,"native_unity":"NOT_RUN","actual_save_adapter":"ABSENT",
       "release":"SAVELOAD_HOLD","qa_sha":git("rev-parse","HEAD").decode().strip()},sort_keys=True))
if __name__=="__main__":
    try:verify()
    except (ValueError,OSError,subprocess.CalledProcessError) as e:
        print("QA_SOURCE_PIN_FAILURE "+str(e),file=sys.stderr);sys.exit(2)
