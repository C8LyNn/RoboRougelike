#!/usr/bin/env python3
"""W14 fail-closed independent exact-source pins, QA-only scope, producer kept immutable."""
import hashlib,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
SOURCE="e77529262b8f03c6e9f0e25b642038c351872044"
SELECTED="d94400d95d13db7f27e1533df05c9292d5380431"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
OWN="Tools/B01/B01-61/P17W14SaveChoice544BoundaryQA/"
WF=".github/workflows/b01-61-p17w14-544-security-boundary.yml"
PINS={
 "Tools/B01/B01-23/P17W13SecureV2SaveContract001/StableIdSnapshotCodec.cs":(SOURCE,"2dd3ee7f465a595ed42c2cbb6637200bb903185f"),
 "Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs":(SELECTED,"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"),
 "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":(SELECTED,"b680ed0ec969ac23203fa55eab7b90f8daa86e43"),
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":(W06,"fa1aa5e531927cbe73cf5fdfe161b4a981316db5")
}
def git(*a):return subprocess.check_output(["git",*a],cwd=ROOT,stderr=subprocess.PIPE)
def blob(data):return hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()
def main():
    changed=git("diff","--name-only",SOURCE,"HEAD").decode().splitlines()
    if len(changed)!=6 or any(not(x.startswith(OWN) or x==WF) for x in changed):
        raise ValueError("QA_ONLY_SCOPE_VIOLATION: "+repr(changed))
    for path,(ref,sha) in PINS.items():
        raw=git("show",ref+":"+path)
        if blob(raw)!=sha:raise ValueError("PIN_"+path)
        if ref==SOURCE and git("rev-parse","HEAD:"+path).decode().strip()!=sha:
            raise ValueError("PRODUCER_544_CODE_CHANGED")
    s=git("show",SOURCE+":Tools/B01/B01-23/P17W13SecureV2SaveContract001/StableIdSnapshotCodec.cs").decode()
    if "public PartRecord[] Parts {get;}" not in s or "public B01.SecureReattachV2.AtomicDockLedger StagedLedger(int index)" not in s:
        raise ValueError("PRODUCER_API_NEW_VERSION_REQUIRES_NEW_EXACT_SHA_QA")
    print("B0161_W14_PROVENANCE PASS original_544="+SOURCE+" selected_510="+SELECTED+" source_blobs=4/4 QA_added=6/6")
    print(json.dumps({"w14_544_source_sha":SOURCE,"selected_510_source_sha":SELECTED,
      "unity_g1":"NOT_RUN","save_contract_544":"HOLD","codex_ack":False,"merged":False},sort_keys=True))
if __name__=="__main__":
    try:main()
    except (ValueError,OSError,subprocess.CalledProcessError) as exc:
        print("PIN_FAIL "+str(exc),file=sys.stderr)
        sys.exit(2)
