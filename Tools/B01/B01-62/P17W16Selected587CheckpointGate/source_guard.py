#!/usr/bin/env python3
"""#587 SINGLE selected Unity producer; exact source provenance. Not an installer."""
import argparse,hashlib,json,subprocess
from pathlib import Path
PRODUCER="ad7904eca571c742257551fa8103f266fcb9b752"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
FILES={
"AtomicWorldSwap.cs":(PRODUCER,"Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs","16a7d7043a2ecb59a41bbfb063170235872fd7fc"),
"DetachedSnapshotContract.cs":(SAVE,"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs","b52e511c370f5fca56c74029050844dc8cbb7f9a"),
"DebrisImpulseCore.cs":(GAME,"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs","fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
"AtomicDockLedger.cs":(GAME,"Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs","b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"),
}
def gitsha(raw):return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()
def execute(repo,dest,receipt):
    d=Path(dest);p=Path(receipt)
    if d.exists() or p.exists() or not d.parent.is_dir() or not p.parent.is_dir():
        raise RuntimeError("REFUSE_PREEXISTING_DESTINATION")
    originals={}
    for filename,(rev,path,sha) in FILES.items():
        ref=f"{rev}:{path}"
        blob=subprocess.check_output(["git","-C",str(repo),"show",ref])
        original_id=subprocess.check_output(["git","-C",str(repo),"rev-parse",ref]).decode().strip()
        if gitsha(blob)!=sha or original_id!=sha:raise RuntimeError("SOURCE_PIN_DRIFT "+filename)
        originals[filename]=blob
    core=originals["AtomicWorldSwap.cs"].decode("utf8")
    for guard in ("if(!Consistent(candidate.Capture(),snapshot))",
                  "candidate.Activate();", "catch{cleanupPending=true;}"):
        if guard not in core:raise RuntimeError("ORIGINAL_SELECTED_587_LAYOUT_CHANGED")
    if core.count("if(!Consistent(candidate.Capture(),snapshot))")<2:
        raise RuntimeError("SELECTED_587_POST_ACTIVATION_GUARD_NOT_FOUND")
    d.mkdir()
    for name,data in originals.items():(d/name).write_bytes(data)
    record={"schema":"B01-62-W16-SINGLE-587-RECEIPT-v1",
        "selected_adapter_pr":587,"selected_adapter_sha":PRODUCER,
        "frozen_save_pr":556,"frozen_save_sha":SAVE,
        "frozen_game_pr":510,"frozen_game_sha":GAME,
        "original_source_pins":{n:{"commit":rev,"path":path,"blob":sha} for n,(rev,path,sha) in sorted(FILES.items())},
        "alternatives_hold":[585,577,584],"corrected_receiver":"B01-62-SELECTED-587-RECEIVER-ONLY",
        "not_real_game_510_save_load":True,"native_unity":"NOT_RUN","code_ack":False,"merged":False}
    raw=(json.dumps(record,sort_keys=True,indent=2)+"\n").encode()
    p.write_bytes(raw)
    print("B0162_W16_SELECTED_587_ORIGINAL_GIT_BLOBS=4/4")
    print("B0162_W16_RECEIPT_SHA256="+hashlib.sha256(raw).hexdigest())
if __name__=="__main__":
    ap=argparse.ArgumentParser();ap.add_argument("--repo",required=True)
    ap.add_argument("--out",required=True);ap.add_argument("--receipt",required=True)
    args=ap.parse_args();execute(args.repo,args.out,args.receipt)
