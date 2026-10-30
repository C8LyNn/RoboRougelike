#!/usr/bin/env python3
"""Exact selected-source and QA release-HOLD manifest, never claim false G1."""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
BASE="ad7904eca571c742257551fa8103f266fcb9b752"
SOURCES={
 "Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":"16a7d7043a2ecb59a41bbfb063170235872fd7fc",
 "Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs":"ff5da20e3c98ab3b540f37fb5652666c47228ed3",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
}
QA="Tools/B01/B01-61/P17W16Selected587AtomicAcceptance/"
WORKFLOW=".github/workflows/b01-61-p17w16-selected587-atomic-qa.yml"

def git(*args):
    return subprocess.check_output(["git",*args],cwd=ROOT,text=True).strip()

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--log",required=True,type=Path)
    p.add_argument("--out",required=True,type=Path)
    args=p.parse_args()
    subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],check=True,cwd=ROOT)
    for path,expected in SOURCES.items():
        actual=git("rev-parse","HEAD:"+path)
        if actual!=expected:
            raise ValueError("SOURCE DRIFT/INCOMPATIBLE BRIDGE "+path+" "+actual)
    paths=git("diff","--name-only",BASE,"HEAD").splitlines()
    if len(paths)!=5 or len(set(paths))!=5 or any(
            not (x.startswith(QA) or x==WORKFLOW) for x in paths):
        raise ValueError("QA OWNERSHIP SCOPE FAIL "+repr(paths))
    log=args.log.read_text(encoding="utf8")
    evidence=(
       "TOTAL 15 PASS 15 FAIL 0",
       "PASS POST_ACTIVATION_POSE_mutation_REJECTS_and_restores_old",
       "PASS POST_ACTIVATION_STABLE_ID_mutation_REJECTS_and_restores_old",
       "PASS ROLLBACK_CLEANUP_THROW_sticky_quarantine_and_future_denial",
       "PASS RED_SAME_GENERATION_DIFFERENT_CONSUMED_WORLD_ACCEPTED",
       "PASS RED_ROLLBACK_EXCEPTION_RETURNS_RECOVERABLE_CODE_WHILE_QUARANTINED",
       "EQUAL_GENERATION_DIFFERENT_WORLD_RED=REPRODUCED",
       "ROLLBACK_QUARANTINE_VERDICT_RED=REPRODUCED",
       "RELEASE_VERDICT=HOLD_P0_RED",
       "LIVE_510_PRIVATE_LEDGER=NOT_INTEGRATED UNITY_G1=NOT_RUN CODEX_ACK=NONE G2=HOLD",
    )
    for expected in evidence:
        if expected not in log:
            raise ValueError("MISSING ACTUAL TEST/GATE EVIDENCE "+expected)
    result={
      "qa_commit_sha":git("rev-parse","HEAD"),
      "exact_original_bridge_candidate_sha":BASE,
      "only_frozen_save_556_sha":"33094f154c7b91119b36d58aab285e6e83c5de57",
      "only_frozen_gameplay_510_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
      "exact_original_source_git_blobs":SOURCES,
      "owned_qa_source_sha256":{x:hashlib.sha256((ROOT/x).read_bytes()).hexdigest() for x in paths},
      "original_csharp_source_qa_pass":15,
      "original_csharp_source_qa_fail":0,
      "post_activation_identity_pose_rollback":"SOURCE_PASS",
      "equal_generation_divergent_id_ledger":"SOURCE_RED",
      "failed_rollback_quarantine_result_code":"SOURCE_RED",
      "release_gate":"HOLD_P0_RED",
      "same_generation_restart_commit_watermark":"MISSING_HOST_DURABLE_AUTHORITY",
      "native_unity_physx_g1":"NOT_RUN",
      "live_original_510_private_ledger_save":"NOT_INTEGRATED",
      "g2":"HOLD","codex_ack":False,"merged":False
    }
    args.out.parent.mkdir(parents=True,exist_ok=True)
    args.out.write_text(json.dumps(result,sort_keys=True,indent=2)+"\n",encoding="utf8")
    print(json.dumps(result,sort_keys=True))

if __name__=="__main__":
    main()
