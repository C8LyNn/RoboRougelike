#!/usr/bin/env python3
"""Immutable source and prefab GUID gate for B01-13 W10, not native Unity QA."""
import hashlib
import json
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parent
PACKAGE = ROOT / "UnityPackage"

def main():
    m = json.loads((ROOT / "manifest.json").read_text(encoding="utf-8"))
    assert m["package_id"] == "MOTION-STALL-001"
    assert len(m["files"]) == 12, "EXPECTED_12_IMMUTABLE_SOURCE_FILES"
    for relative, pin in sorted(m["files"].items()):
        raw = (ROOT / relative).read_bytes()
        assert hashlib.sha256(raw).hexdigest() == pin["sha256"], "HASH_MISMATCH " + relative
        assert len(raw) == pin["bytes"], "SIZE_MISMATCH " + relative
        print("ORIGINAL_SHA256_PASS", relative, len(raw), pin["sha256"])

    guids = set()
    metas = list(PACKAGE.rglob("*.meta"))
    assert len(metas) == 3, "EXPECTED_THREE_UNITY_METAS"
    for meta in metas:
        match = re.search(r"(?m)^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8"))
        assert match, "INVALID_UNITY_META_GUID"
        assert match.group(1) not in guids, "DUPLICATE_UNITY_META_GUID"
        guids.add(match.group(1))

    original_meta = (PACKAGE / "Runtime/MotionStallOverlay.cs.meta").read_text(encoding="utf-8")
    source_guid = re.search(r"(?m)^guid: ([0-9a-f]{32})$", original_meta).group(1)
    prefab = (PACKAGE / "Prefabs/B01_13_MotionStall_Optional.prefab").read_text(encoding="utf-8")
    assert prefab.count("m_Script: {fileID: 11500000, guid: " + source_guid + ", type: 3}") == 1
    assert prefab.count("--- !u!1 &") == 1
    assert prefab.count("--- !u!114 &") == 1
    assert prefab.count("m_GameObject: {fileID: 1200000}") == 2
    assert "observedVehicle: {fileID: 0}" in prefab, "EXPECTED_UNASSIGNED_TARGET_FAIL_SAFE"

    ledger = (PACKAGE / "Runtime/MotionStallLedger.cs").read_text(encoding="utf-8")
    adapter = (PACKAGE / "Runtime/MotionStallOverlay.cs").read_text(encoding="utf-8")
    assert "class MotionStallOverlay : MonoBehaviour" in adapter
    assert "Input.GetKey(KeyCode.W)" in adapter and "Input.GetKey(KeyCode.S)" in adapter
    assert "observedVehicle.position" in adapter
    assert "MaximumSampleGapSeconds" in ledger and "TeleportDistanceMeters" in ledger
    for src in (ledger, adapter):
        for banned in ("Rigidbody.AddForce", "Rigidbody.velocity", "transform.position =",
                       "File.Write", "FileStream", "PlayerPrefs", "UnityWebRequest"):
            assert banned not in src, "UNEXPECTED_SIDE_EFFECT " + banned
    print("STATIC_PREFAB_GUID_PASS 3/3 UNIQUE, SCRIPT_REFERENCE_PASS, MISSING_TARGET_SAFE")
    print("VERIFIED_IMMUTABLE_FILE_COUNT=12")

if __name__ == "__main__":
    try:
        main()
    except (AssertionError, ValueError, OSError, KeyError, AttributeError) as exc:
        print("FAIL_CLOSED: " + str(exc), file=sys.stderr)
        sys.exit(2)
