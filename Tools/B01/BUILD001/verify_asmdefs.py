#!/usr/bin/env python3
"""BUILD001 asmdef contract check: no Unity editor/compiler execution."""
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[3]
for mode in ("EditMode", "PlayMode"):
    path = ROOT / f"Assets/B01Construction/BUILD001/Tests/{mode}/B01.BUILD001.{mode}Tests.asmdef"
    data = json.loads(path.read_text(encoding="utf-8"))
    assert data["references"] == ["B01.BUILD001.Runtime"], (path, data["references"])
    assert data["optionalUnityReferences"] == ["TestAssemblies"], path
    assert data["includePlatforms"] == (["Editor"] if mode == "EditMode" else []), path
    assert data["autoReferenced"] is False, path
    assert not any(x in data["references"] for x in ("UnityEngine.TestRunner","UnityEditor.TestRunner")), path
    print(f"STATIC_PASS: {path.relative_to(ROOT)}")
print("STATIC_PASS: 2/2 BUILD001 asmdefs; Unity compile/PlayMode NOT_RUN")
