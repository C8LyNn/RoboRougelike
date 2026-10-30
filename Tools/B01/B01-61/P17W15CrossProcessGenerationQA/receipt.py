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
BRIDGE_COMMIT="cf7f2a155570f99c412585d5f09f8f97f9e83754"
BRIDGE_BLOBS={
 "Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":"74e853601ecc7a5455d9bb466432ce7c027e0695",
 "Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs":"94594b0b83514e9f2e1c5bb21ddef9f093b299fc",
}
def receipt(log_path,bridge_log,bridge_dir):
    subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
    for path,sha in SOURCE.items():
        if git("rev-parse","HEAD:"+path)!=sha:
            raise ValueError("frozen original source drift: "+path)
    paths=git("diff","--name-only",BASE,"HEAD").splitlines()
    if len(paths)!=7 or len(set(paths))!=7 or any(not(p.startswith(OWN) or p==WF) for p in paths):
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
    bridge_text=bridge_log.read_text(encoding="utf-8")
    for required in (
        "BRIDGE TOTAL 13 PASS 13 FAIL 0",
        "PASS BRIDGE_historic_wrong_busy_callback_self_denies",
        "PASS BRIDGE_fixed_external_host_idle_callback_accepts",
        "PASS BRIDGE_activation_failure_reactivates_old_world",
        "PASS BRIDGE_rollback_failure_sets_quarantine_and_deny_future",
        "BRIDGE_CORE_SELECTED_SOURCE_DIRECT_QA=EXECUTED",
        "UNITY_NATIVE_PHYSX=NOT_RUN LIVE_CANONICAL_510_RESTORE=NOT_IMPLEMENTED",
    ):
        if required not in bridge_text:
            raise ValueError("missing real bridge source test evidence: "+required)
    for repo_path, expected_blob in BRIDGE_BLOBS.items():
        candidate=bridge_dir/Path(repo_path).name
        if not candidate.is_file():
            raise ValueError("missing candidate original Git source: "+str(candidate))
        actual_blob=git("hash-object",str(candidate))
        if actual_blob!=expected_blob:
            raise ValueError("changed bridge blob "+str(candidate)+": "+actual_blob)
    unity_host=(bridge_dir/"IsolatedTwinRigSaveHost.cs").read_text(encoding="utf-8")
    if ("coordinator.TryLoad(bytes,expectedRun,minGeneration,PhysicalIdle)" not in unity_host or
        "coordinator.TrySave(runId,PhysicalIdle,out bytes)" not in unity_host or
        "private bool CanOperate() => coordinator!=null&&!coordinator.Busy&&PhysicalIdle();" not in unity_host or
        "TryLoad(bytes,expectedRun,minGeneration,CanOperate)" in unity_host):
        raise ValueError("Unity host callback still self-blocks or producer wiring drift")
    return {
      "qa_head_sha":git("rev-parse","HEAD"),
      "only_selected_save_commit":BASE,
      "only_selected_gameplay_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
      "original_source_blobs":SOURCE,
      "new_qa_files_sha256":{p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in paths},
      "csharp_source_real_process_assertions":{"pass":10,"fail":0},
      "bridge_exact_candidate_commit":BRIDGE_COMMIT,
      "bridge_original_git_blob_pins":BRIDGE_BLOBS,
      "bridge_original_atomic_core_source_assertions":{"pass":13,"fail":0},
      "bridge_unity_host_callback_source_inspection":"FIXED_AT_EXACT_PIN_NOT_NATIVE",
      "durable_rollback_anchor":"NOT_IMPLEMENTED_IN_SOURCE_ONLY_556",
      "same_generation_commit_identity":"HOST_MUST_ENFORCE",
      "new_unity_host_adapter_exact_sha":BRIDGE_COMMIT,
      "host_bridge_real_qa":"SOURCE_CORE_EXECUTED_NATIVE_NOT_RUN",
      "native_unity_g1":"NOT_RUN",
      "g2":"HOLD",
      "codex_ack":False,
      "merge":False,
    }
def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--log",required=True,type=Path)
    ap.add_argument("--out",required=True,type=Path)
    ap.add_argument("--bridge-log",required=True,type=Path)
    ap.add_argument("--bridge-dir",required=True,type=Path)
    a=ap.parse_args()
    r=receipt(a.log,a.bridge_log,a.bridge_dir)
    a.out.parent.mkdir(parents=True,exist_ok=True)
    a.out.write_text(json.dumps(r,indent=2,sort_keys=True)+"\n",encoding="utf-8")
    print(json.dumps(r,sort_keys=True))
if __name__=="__main__":main()
