#!/usr/bin/env python3
"""Fail-closed physical W11 manifest + frozen W10 Git source pin verification."""
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parent
FROZEN_SHA256 = "c391dfebd24621da9a2c23bbc722973b81258da2e490cc314cb4ebaccc5fce99"
PARENT_SHA = "e32d3199fa1fe6e9120df61a61ec02031441a2d3"

def verify():
    manifest = json.loads((ROOT / "manifest.json").read_text(encoding="utf-8"))
    assert manifest["package_id"] == "MOTION-TRACE-REPLAY-001"
    assert manifest["upstream_exact_commit"] == PARENT_SHA
    assert len(manifest["files"]) == 7, "EXPECTED_7_PINNED_W11_FILES"
    for rel, expected in sorted(manifest["files"].items()):
        path = ROOT / rel
        data = path.read_bytes()
        actual = hashlib.sha256(data).hexdigest()
        assert len(data) == expected["bytes"], "SIZE_MISMATCH: " + rel
        assert actual == expected["sha256"], "SHA256_MISMATCH: " + rel
        print("W11_SHA256_PASS", rel, len(data), actual)
    upstream = ROOT / "Tests/PinnedOriginal/MotionStallLedger.cs"
    actual_upstream = hashlib.sha256(upstream.read_bytes()).hexdigest()
    assert actual_upstream == FROZEN_SHA256, "W10_IMMUTABLE_SOURCE_MISMATCH"
    print("UPSTREAM_FROZEN_SHA256_PASS", actual_upstream)
    header = "trace,t_ms,x_mm,z_mm,held,state,event,stalls,recoveries,discontinuities"
    lines = (ROOT / "Fixtures/original_motion_trace_contract.csv").read_text(encoding="utf-8").splitlines()
    assert lines[0] == header, "TRACE_HEADER"
    cases = {line.split(",")[0] for line in lines[1:]}
    assert cases == {"blocked100", "blocked250", "gap_teleport", "release_creep", "no_attempt", "two_stalls"}, "TRACE_CASES"
    assert len(lines) == 102, "EXPECTED_101_PHYSICAL_SAMPLES"
    print("W11_SOURCE_GATE_PASS fixtures=6 samples=101 filepins=7 original_pin=1")

if __name__ == "__main__":
    try:
        verify()
    except (AssertionError, OSError, KeyError, ValueError, IndexError) as e:
        print("W11_VERIFY_FAIL", str(e), file=sys.stderr)
        sys.exit(2)
