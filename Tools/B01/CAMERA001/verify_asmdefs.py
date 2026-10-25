#!/usr/bin/env python3
"""CAMERA001 configuration regression; JSON/static only, NEVER Unity compilation."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
TARGET = ROOT / "Assets/B01Packages/CAMERA001/Tests"
for mode in ("EditMode", "PlayMode"):
    name = f"B01.CAMERA001.{mode}Tests.asmdef"
    path = TARGET / mode / name
    data = json.loads(path.read_text(encoding="utf-8"))
    assert data["references"] == ["B01.CAMERA001.Runtime"], (path, data["references"])
    assert data["optionalUnityReferences"] == ["TestAssemblies"], path
    assert data["includePlatforms"] == (["Editor"] if mode == "EditMode" else []), path
    assert data["autoReferenced"] is False, path
    for blocked in ("UnityEngine.TestRunner", "UnityEditor.TestRunner"):
        assert blocked not in data["references"], (path, blocked)
    print(f"STATIC_PASS: {path.relative_to(ROOT)}")
print("STATIC_PASS: 2/2 asmdefs; UNITY_COMPILE=NOT_RUN")
