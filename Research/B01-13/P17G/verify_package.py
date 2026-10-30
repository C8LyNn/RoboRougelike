#!/usr/bin/env python3
"""Original exact-byte Unity prefab/API integrity gate; not native Unity verification."""
import hashlib
import json
from pathlib import Path
import re
import sys

BASE = Path(__file__).resolve().parent
PACKAGE = BASE / "UnityPackage"

def check():
    manifest = json.loads((BASE / "manifest.json").read_text(encoding="utf-8"))
    assert manifest["package_id"] == "INPUT-DISCOVERY-001"
    assert len(manifest["files"]) == 12, "EXPECTED_12_FILE_PINS"
    for rel, specs in sorted(manifest["files"].items()):
        content = (BASE / rel).read_bytes()
        assert hashlib.sha256(content).hexdigest() == specs["sha256"], "SHA256_MISMATCH: " + rel
        assert len(content) == specs["bytes"], "SIZE_MISMATCH: " + rel
        print("PIN_PASS", rel, len(content), specs["sha256"])
    meta_files = list(PACKAGE.rglob("*.meta"))
    assert len(meta_files) == 3, "EXPECTED_THREE_UNITY_META"
    guids = {}
    for file in meta_files:
        match = re.search(r"(?m)^guid: ([0-9a-f]{32})$", file.read_text(encoding="utf-8"))
        assert match, "MISSING_GUID " + file.name
        assert match.group(1) not in guids, "DUPLICATE_GUID"
        guids[match.group(1)] = file.name
    prefab = (PACKAGE / "Prefabs/B01_13_InputDiscovery_Optional.prefab").read_text(encoding="utf-8")
    adapter_meta = (PACKAGE / "Runtime/InputDiscoveryOverlay.cs.meta").read_text(encoding="utf-8")
    guid = re.search(r"(?m)^guid: ([0-9a-f]{32})$", adapter_meta).group(1)
    assert prefab.count("m_Script: {fileID: 11500000, guid: " + guid + ", type: 3}") == 1
    assert prefab.count("--- !u!1 &") == 1
    assert prefab.count("--- !u!114 &") == 1
    assert prefab.count("m_GameObject: {fileID: 1100000}") == 2
    ledger = (PACKAGE / "Runtime/InputDiscoveryLedger.cs").read_text(encoding="utf-8")
    adapter = (PACKAGE / "Runtime/InputDiscoveryOverlay.cs").read_text(encoding="utf-8")
    assert "public sealed class InputDiscoveryOverlay : MonoBehaviour" in adapter
    assert "Input.GetMouseButtonDown(0)" in adapter and "Input.GetMouseButtonDown(1)" in adapter
    assert "Time.realtimeSinceStartupAsDouble" in adapter
    assert "private readonly InputDiscoveryLedger ledger" in adapter
    for source in (ledger, adapter):
        for banned in ("File.Write", "FileStream", "PlayerPrefs", "UnityWebRequest", "Application.persistentDataPath"):
            assert banned not in source, "UNEXPECTED_IO: " + banned
    print("PREFAB_SOURCE_PASS: three unique GUIDs, one script mapping, no persistent I/O")
    print("VERIFY_TOTAL=12 PIN_PASS")

if __name__ == "__main__":
    try:
        check()
    except (ValueError, OSError, AssertionError, KeyError, AttributeError) as error:
        print("FAIL_CLOSED: " + str(error), file=sys.stderr)
        sys.exit(2)
