#!/usr/bin/env python3
"""Original-source intake guard. Never promotes SOURCE PASS to native Unity G1."""
import json
import re
import subprocess
from pathlib import Path

ROOT=Path("Tools/B01/B01-50/W27DebrisPhysicsLab001")
MANIFEST=ROOT/"MANIFEST-GIT-BLOBS.tsv"
DEPSHA="b38216d94fb0f87af0c1cbc47090c7cb4bf7177e"
files=[]
for raw in MANIFEST.read_text().splitlines():
    sha,path=raw.split("\t",1)
    p=Path(path)
    assert p.is_file() and p!=MANIFEST and path not in files, path
    actual=subprocess.check_output(["git","hash-object","--no-filters",str(p)],text=True).strip()
    assert actual==sha,(path,sha,actual)
    files.append(path)
assert all(p.startswith(ROOT.as_posix()+"/") or p==".github/workflows/b01-50-w27-debris-physics-lab.yml" for p in files)
tracked=[p for p in ROOT.rglob("*") if p.is_file() and p!=MANIFEST]
assert len(files)==len(tracked)+1,("manifest closure",len(files),len(tracked))
metas=list(ROOT.rglob("*.meta"))
assert len(metas)==8,len(metas)
ids=[]
for p in metas:
    text=p.read_text()
    m=re.search(r"^guid: ([0-9a-f]{32})$",text,re.M)
    assert m,p
    assert Path(str(p)[:-5]).is_file(),p
    ids.append(m.group(1))
assert len(set(ids))==len(ids)
previous=set()
for p in Path("Assets").rglob("*.meta"):
    previous.update(re.findall(r"^guid: ([0-9a-f]{32})$",p.read_text(errors="replace"),re.M))
assert not (set(ids)&previous)
scene=(ROOT/"Native/Scenes/B0150W27DebrisPhysicsLab.unity").read_text()
main=(ROOT/"Native/Runtime/B0150W27DebrisPhysicsLab.cs").read_text()
guid=re.search(r"^guid: ([0-9a-f]{32})$", (ROOT/"Native/Runtime/B0150W27DebrisPhysicsLab.cs.meta").read_text(),re.M).group(1)
assert f"m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}" in scene
assert "autoRunOnPlay: 0" in scene
assert "warmupFixedSteps: 120" in scene and "measuredFrames: 180" in scene
assert scene.startswith("%YAML 1.1") and "--- !u!1660057539" in scene
assert "RunBenchmark()" in main and "Application.temporaryCachePath" in main
assert "10, 100, 500" in main and "DYNAMIC_CONTACT" in main and "STATIC_REST" in main
assert "ProfilerRecorder.StartNew" in main and "yield return new WaitForEndOfFrame()" in main
assert "SOURCE_CAPTURE_ONLY_AWAITING_G1_REVIEW" in main and "INCONCLUSIVE_CONTACTS" in main
assert DEPSHA in main and DEPSHA in (ROOT/"Tools/analyze_native_csv.py").read_text()
for c in ("B0150W27DebrisPhysicsLab","B0150W27ContactWitness","B0150W27Sweeper","B0150W27ActiveControl"):
    assert (ROOT/f"Native/Runtime/{c}.cs").is_file(),c
runtime=json.loads((ROOT/"Native/Runtime/B0150.W27.DebrisPhysicsLab.asmdef").read_text())
native=json.loads((ROOT/"Native/Tests/PlayMode/B0150.W27.DebrisPhysicsLab.NativeTests.asmdef").read_text())
assert runtime["includePlatforms"]==["Editor"] and runtime["references"]==["B0150.DebrisSleep.R3.Runtime"]
assert native["includePlatforms"]==["Editor"] and set(native["references"])=={"B0150.DebrisSleep.R3.Runtime","B0150.W27.DebrisPhysicsLab"}
assert "UNITY_INCLUDE_TESTS" in native["defineConstraints"] and "TestAssemblies" in native["optionalUnityReferences"]
assert (ROOT/"Native/Tests/PlayMode/B0150W27NativeBenchmarkTests.cs").read_text().count("[UnityTest]")==2
print("B0150_W27_ORIGINAL_GIT_BLOBS_PASS",len(files),"file(s), 8 unique importer GUIDs")
print("B0150_W27_ACTUAL_UNITY_SCENE_SOURCE_AND_TWO_NATIVE_TESTS_PRESENT; UNITY_G1_NOT_RUN")
