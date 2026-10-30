#!/usr/bin/env python3
"""Strict exact-final-selected-604 SHA, new owned files, original-source stdout."""
import argparse, hashlib, json, subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
BASE="1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a"
OWN="Tools/B01/B01-61/P17W17Final604FenceInterleavingQA/"
WF=".github/workflows/b01-61-p17w17-final604-fence-interleaving.yml"
SOURCES={
 "Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":"399d3cb2074a2659af8f4e941edb28a3d2dd2832",
 "Assets/B0123UnitySaveBridge001/Core/CheckpointFence.cs":"53f907125b09531ebdf5ffdfbb9c5c9b48eca217",
 "Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs":"5012e98bba786c6b9702992c317586a0015678c1",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
}
def git(*args):
    return subprocess.check_output(["git",*args],cwd=ROOT,text=True).strip()
def main():
    p=argparse.ArgumentParser()
    p.add_argument("--log",required=True,type=Path)
    p.add_argument("--out",required=True,type=Path)
    args=p.parse_args()
    subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
    if git("rev-parse","HEAD~5")==BASE:
        pass
    for path,blob in SOURCES.items():
        if git("rev-parse","HEAD:"+path)!=blob:
            raise ValueError("immutable original source SHA drift "+path)
    paths=git("diff","--name-only",BASE,"HEAD").splitlines()
    if len(paths)!=5 or len(set(paths))!=5 or any(
        not (x.startswith(OWN) or x==WF) for x in paths):
        raise ValueError("unexpected source/owner scope "+str(paths))
    host=(ROOT/"Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs").read_text()
    for marker in (
        "UsePersistentCheckpointSlot(string slot)",
        "new FileCheckpointFence(",
        "new AtomicWorldSwap(this,",
        "part.SetActive(true);",
        "SnapshotCodec.Encode(new RunSnapshot(",
        "coordinator.TryLoad(bytes,expectedRun,minGeneration,PhysicalIdle)",
    ):
        if marker not in host:
            raise ValueError("final Unity-facing package SOURCE wiring missing "+marker)
    log=args.log.read_text(encoding="utf8")
    for required in (
        "TOTAL 20 PASS 20 FAIL 0",
        "PASS MID_STAGE_TWO_HOSTS_HIGHER_EPOCH_WINS_STALE_SEAL_QUARANTINES_LOSER",
        "PASS ROLLBACK_ABORT_THROW_IMMEDIATE_QUARANTINE_RETURN",
        "PASS POST_ACTIVATE_STABLE_ID_DRIFT_RESTORES_OLD_AND_LEDGER",
        "PASS FOUR_LIGHT_HEAVY_IDS_00",
        "PASS FOUR_LIGHT_HEAVY_IDS_11",
        "PASS DURABLE_SAVE_REJECTS_CHANGED_POSE_WITH_UNCHANGED_GENERATION",
        "SELECTED_604_EXACT_CORE_FENCE_BYTES_DIRECTLY_EXECUTED=TRUE",
        "INTERLEAVED_TWO_HOST_SEAL_RACE=FAIL_CLOSED_STICKY_QUARANTINE",
        "G0_SOURCE_QA=PASS",
        "UNITY_NATIVE_G1=NOT_RUN LIVE_510_PRIVATE_LEDGER=NOT_INTEGRATED",
        "DURABLE_TWO_FILE_SAVE_ATOMICITY=NOT_IMPLEMENTED",
    ):
        if required not in log:
            raise ValueError("missing actual execution evidence "+required)
    output={
      "qa_head_sha":git("rev-parse","HEAD"),
      "selected_final_candidate_604_exact_commit_sha":BASE,
      "selected_only_556_source_commit":"33094f154c7b91119b36d58aab285e6e83c5de57",
      "immutable_only_510_gameplay_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
      "verified_original_source_git_blobs":SOURCES,
      "owned_qa_files_sha256":{x:hashlib.sha256((ROOT/x).read_bytes()).hexdigest() for x in paths},
      "original_producer_604_csharp_source_case_pass":20,
      "original_producer_604_csharp_source_case_fail":0,
      "deterministic_two_host_interleaving":"SOURCE_FAIL_CLOSED",
      "full_unity_host":"STATIC_ORIGINAL_GIT_PIN_ONLY_NOT_ENGINE_EXECUTED",
      "native_unity_g1":"NOT_RUN",
      "full_atomic_game_save_plus_watermark":"NOT_IMPLEMENTED",
      "canonical_510_private_ledger_hydration":"NOT_AUTHORIZED",
      "formal_physical_g0":"PENDING_B01_40",
      "live_game_saveload":"HOLD","g2":"HOLD","codex_ack":False,
      "merged":False
    }
    args.out.parent.mkdir(parents=True,exist_ok=True)
    args.out.write_text(json.dumps(output,sort_keys=True,indent=2)+"\n")
    print(json.dumps(output,sort_keys=True))
if __name__=="__main__":main()
