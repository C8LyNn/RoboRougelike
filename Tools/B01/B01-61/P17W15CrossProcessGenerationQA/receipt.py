#!/usr/bin/env python3
"""Fail-closed manifest for real cross-process C# source QA (no Unity claim)."""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
BASE="33094f154c7b91119b36d58aab285e6e83c5de57"
OWN="Tools/B01/B01-61/P17W15CrossProcessGenerationQA/"
WF=".github/workflows/b01-61-p17w15-crossprocess-generation-qa.yml"
SOURCE={
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
}
def git(*args):
    return subprocess.check_output(["git", *args],cwd=ROOT,text=True).strip()
def receipt(log_path):
    subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
    for path,sha in SOURCE.items():
        if git("rev-parse","HEAD:"+path)!=sha:
            raise ValueError("frozen original source drift: "+path)
    paths=git("diff","--name-only",BASE,"HEAD").splitlines()
    if len(paths)!=5 or len(set(paths))!=5 or any(not(p.startswith(OWN) or p==WF) for p in paths):
        raise ValueError("unexpected owned path scope: "+repr(paths))
    log=log_path.read_text(encoding="utf-8")
    for required in (
        "TOTAL 10 PASS 10 FAIL 0",
        "CROSS_PROCESS_REPLAY_WITHOUT_DURABLE_WATERMARK=REPRODUCED",
        "SAME_GENERATION_FORK_WITHOUT_HOST_COMMIT_ID=REPRODUCED",
        "CONTRACT_556_SOURCE_ONLY=TRUE LIVE_HOST_ADAPTER=NOT_TESTED",
        "NATIVE_UNITY=NOT_RUN SAVELOAD_ACCEPTED=HOLD G2=HOLD",
        "PASS REAL_PROCESS_C_stale_8_REPLAY_ACCEPTED_without_external_watermark",
        "PASS REAL_PROCESS_D_stale_8_REJECTED_with_host_supplied_min_9",
        "PASS REAL_PROCESS_H_same_generation_fork_requires_host_transaction_policy",
    ):
        if required not in log: raise ValueError("missing actual original-source evidence: "+required)
    return {
      "qa_head_sha":git("rev-parse","HEAD"),
      "only_selected_save_commit":BASE,
      "only_selected_gameplay_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
      "original_source_blobs":SOURCE,
      "new_qa_files_sha256":{p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in paths},
      "csharp_source_real_process_assertions":{"pass":10,"fail":0},
      "durable_rollback_anchor":"NOT_IMPLEMENTED_IN_SOURCE_ONLY_556",
      "same_generation_commit_identity":"HOST_MUST_ENFORCE",
      "new_unity_host_adapter_exact_sha":None,
      "host_bridge_real_qa":"NOT_RUN_NO_CANDIDATE",
      "native_unity_g1":"NOT_RUN",
      "g2":"HOLD",
      "codex_ack":False,
      "merge":False,
    }
def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--log",required=True,type=Path)
    ap.add_argument("--out",required=True,type=Path)
    a=ap.parse_args()
    r=receipt(a.log)
    a.out.parent.mkdir(parents=True,exist_ok=True)
    a.out.write_text(json.dumps(r,indent=2,sort_keys=True)+"\n",encoding="utf-8")
    print(json.dumps(r,sort_keys=True))
if __name__=="__main__":main()
