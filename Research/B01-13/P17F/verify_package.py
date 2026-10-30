#!/usr/bin/env python3
"""Check original B01-13 Unity asset bytes, stable GUIDs and prefab wiring before compiling."""
import hashlib
import json
import pathlib
import re
import sys

HERE = pathlib.Path(__file__).resolve().parent
PKG = HERE / "UnityPackage"
MANIFEST = HERE / "manifest.json"

def main():
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    count = 0
    for rel, meta in sorted(manifest["files"].items()):
        name = HERE / rel
        raw = name.read_bytes()
        actual = hashlib.sha256(raw).hexdigest()
        if len(raw) != meta["bytes"] or actual != meta["sha256"]:
            raise AssertionError("SOURCE_OR_ASSET_HASH_MISMATCH " + rel)
        print("SHA256_PASS", rel, meta["bytes"], actual)
        count += 1
    guids = {}
    for path in sorted(PKG.rglob("*.meta")):
        match = re.search(r"(?m)^guid: ([a-f0-9]{32})$", path.read_text(encoding="utf-8"))
        if not match:
            raise AssertionError("META_NO_GUID " + path.name)
        guid = match.group(1)
        if guid in guids:
            raise AssertionError("GUID_COLLISION")
        guids[guid] = path.name
    assert len(guids) == 3, "EXPECTED_THREE_UNIQUE_GUIDS"
    prefab = (PKG / "Prefabs/Ld02TechnicalOverlay.prefab").read_text(encoding="utf-8")
    overlay_meta = (PKG / "Runtime/Ld02TechnicalOverlay.cs.meta").read_text(encoding="utf-8")
    overlay_guid = re.search(r"(?m)^guid: ([a-f0-9]{32})$", overlay_meta).group(1)
    assert prefab.count("m_Script: {fileID: 11500000, guid: " + overlay_guid) == 1, "PREFAB_SCRIPT_GUID_MISMATCH"
    assert prefab.count("--- !u!1 &") == 1, "EXPECTED_ONE_GAME_OBJECT"
    assert prefab.count("--- !u!114 &") == 1, "EXPECTED_ONE_MONOBEHAVIOUR"
    assert prefab.count("m_GameObject: {fileID: 100000}") == 2, "BAD_GAMEOBJECT_REF"
    script = (PKG / "Runtime/Ld02TechnicalOverlay.cs").read_text(encoding="utf-8")
    assert "class Ld02TechnicalOverlay : MonoBehaviour" in script
    assert "KeyCode.F9" in script
    assert "Application.persistentDataPath" not in script
    assert "File.Write" not in script
    assert "UnityWebRequest" not in script
    print("PREFAB_WIRING_STATIC_PASS GUIDS=3 SCRIPT_MATCH=YES IO=NONE")
    print("VERIFY_COMPLETE FILES=" + str(count))

if __name__ == "__main__":
    try:
        main()
    except (AssertionError, OSError, ValueError, KeyError) as e:
        print("FAIL_CLOSED", str(e), file=sys.stderr)
        raise SystemExit(2)
