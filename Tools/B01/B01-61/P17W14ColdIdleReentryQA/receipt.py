#!/usr/bin/env python3
"""Exact-HEAD source-only QA proof with explicit RED producer release gate."""
import argparse
import hashlib
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = "6da2c523e03766cd5a6deaed0cb3cccf8765354b"
PINS = {
    "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":
        "6c036985411164372bf3d21d467780ff3777544e",
    "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":
        "fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
}
OWN_PREFIX = "Tools/B01/B01-61/P17W14ColdIdleReentryQA/"
WORKFLOW = ".github/workflows/b01-61-p17w14-cold-idle-reentry.yml"

def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True).strip()

def make_receipt(test_log):
    subprocess.run(["git", "merge-base", "--is-ancestor", BASE, "HEAD"], cwd=ROOT, check=True)
    for path, expected in PINS.items():
        actual = git("rev-parse", "HEAD:" + path)
        if actual != expected:
            raise ValueError("original source pin changed: " + path)
    paths = git("diff", "--name-only", BASE, "HEAD").splitlines()
    if len(paths) != 5 or len(set(paths)) != 5:
        raise ValueError("expected exactly five unique QA-owned paths: " + repr(paths))
    if any(not (p.startswith(OWN_PREFIX) or p == WORKFLOW) for p in paths):
        raise ValueError("producer/game/foreign path changed: " + repr(paths))
    if not test_log.exists():
        raise ValueError("no actual original CSharp test log")
    log = test_log.read_text(encoding="utf-8")
    if "TOTAL 6 PASS 6 FAIL 0" not in log:
        raise ValueError("real exact-source CSharp witnesses did not pass")
    for name in (
        "RED_busy_outer_denial_still_publishes_reentrant_swap",
        "RED_quarantine_outer_exception_still_publishes_nested_swap",
        "RED_stale_outer_denial_still_publishes_nested_generation",
    ):
        if "PASS " + name not in log:
            raise ValueError("missing real RED witness: " + name)
    return {
        "qa_head_sha": git("rev-parse", "HEAD"),
        "target_producer_pr": 549,
        "target_source_sha": BASE,
        "selected_gameplay_510_sha": "d94400d95d13db7f27e1533df05c9292d5380431",
        "original_git_blob_pins": PINS,
        "changed_paths": paths,
        "changed_sha256": {
            p: hashlib.sha256((ROOT / p).read_bytes()).hexdigest() for p in paths
        },
        "original_csharp_executed": {"assertions": 6, "pass": 6, "fail": 0},
        "original_source_regression_witness": "REPRODUCED_RED",
        "contract_release_gate": "HOLD_REENTRANCY_AND_550",
        "unity_g1": "NOT_RUN",
        "game_save_load_host": "NOT_INTEGRATED",
        "codex_ack": None,
        "merge": False,
    }

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--log", type=Path, required=True)
    ap.add_argument("--out", type=Path, required=True)
    args = ap.parse_args()
    result = make_receipt(args.log)
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(result, sort_keys=True))

if __name__ == "__main__":
    main()
