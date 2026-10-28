#!/usr/bin/env python3
"""Original Unity source integrity gate. No Unity compilation or GPU claim."""
import json
import re
from pathlib import Path
import yaml

ROOT=Path(__file__).resolve().parents[3]
P=ROOT/"Assets/B0132P17WheelWake"
manifest=json.loads((P/"PACKAGE_MANIFEST.json").read_text(encoding="utf8"))
assert manifest["package_id"]=="B01-32-WHEELWAKE-001"
assert manifest["integrated"] is False and manifest["native_unity"]=="NOT_RUN"
print("MANIFEST_PASS B01-32-WHEELWAKE-001 / not integrated")

metas=[P.with_suffix(".meta")]+list(P.rglob("*.meta"))
assert len(metas)==16, f"expected exact 16 own package .meta, got {len(metas)}"
guids={}
for path in metas:
    src=path.read_text(encoding="utf8")
    m=re.search(r"(?m)^guid: ([a-f0-9]{32})$",src)
    assert m, f"missing stable 32-char GUID {path}"
    g=m.group(1)
    assert g not in guids, f"duplicate Unity GUID {path} and {guids[g]}"
    guids[g]=path
    if path.name.endswith(".cs.meta"): assert "MonoImporter:" in src
    if path.name.endswith(".prefab.meta"): assert "PrefabImporter:" in src
    if path.name.endswith(".asmdef.meta"): assert "AssemblyDefinitionImporter:" in src
    if path.name.endswith(".unity.meta"): assert "DefaultImporter:" in src
    if path.name.endswith("WheelWake.meta") or path.name in ("Runtime.meta","Prefabs.meta","Tests.meta","PlayMode.meta","Scenes.meta"):
        assert "folderAsset: yes" in src
print(f"META_GUID_PASS {len(guids)}/16 distinct stable Unity import references")

class Loader(yaml.SafeLoader): pass
Loader.add_multi_constructor("tag:unity3d.com,2011:",
    lambda loader,suffix,node: loader.construct_mapping(node,deep=True))
def read_unity(path):
    raw=path.read_text(encoding="utf8")
    raw=raw.replace("%TAG !u! tag:unity3d.com,2011:\\n","")
    raw=re.sub(r"(?m)^--- !u!","%TAG !u! tag:unity3d.com,2011:\\n--- !u!",raw)
    return list(yaml.load_all(raw,Loader=Loader))
def script_guid(name):
    s=(P/"Runtime"/(name+".cs.meta")).read_text(encoding="utf8")
    return re.search(r"(?m)^guid: ([a-f0-9]{32})$",s).group(1)

prefab=read_unity(P/"Prefabs/B0132_DualWheelWake.prefab")
assert len(prefab)==3, "original Unity prefab must have actual GO/Transform/MonoBehaviour"
assert prefab[0]["GameObject"]["m_Name"]=="B0132_DualWheelWake"
assert prefab[2]["MonoBehaviour"]["m_Script"]["guid"]==script_guid("B0132WheelWakeEmitter")
assert prefab[2]["MonoBehaviour"]["m_GameObject"]["fileID"]==prefab[1]["Transform"]["m_GameObject"]["fileID"]
assert prefab[2]["MonoBehaviour"]["leftWheelOffset"]["x"]<0
assert prefab[2]["MonoBehaviour"]["rightWheelOffset"]["x"]>0
print("PREFAB_BINDING_PASS actual source dual wheel offsets and emitter GUID")

scene=read_unity(P/"Scenes/B0132_WheelWakeDemo.unity")
assert len(scene)==4
assert scene[2]["MonoBehaviour"]["m_Script"]["guid"]==script_guid("B0132WheelWakeDemo")
assert scene[2]["MonoBehaviour"]["mode"]==1
assert scene[2]["MonoBehaviour"]["grounded"]==1
assert scene[3]["SceneRoots"]["m_Roots"]
print("SCENE_BINDING_PASS stand-alone source scene host and root")

runtime=json.loads((P/"Runtime/B0132.P17WheelWake.Runtime.asmdef").read_text())
tests=json.loads((P/"Tests/PlayMode/B0132.P17WheelWake.PlayModeTests.asmdef").read_text())
assert runtime["references"]==[]
assert tests["references"]==[runtime["name"]]
assert tests["optionalUnityReferences"]==["TestAssemblies"]
code=(P/"Tests/PlayMode/B0132WheelWakePlayModeTests.cs").read_text(encoding="utf8")
assert code.count("[UnityTest]")==3
for label in ["ActiveCruise_CreatesTwoCosmeticEmitters_WithoutPhysics",
              "ContactLost_ImmediatelyClearsBothParticleSystems",
              "OffAndDisable_DoNotResumeStaleEmission"]:
    assert label in code
for file in ["WheelWakeProfile.cs","B0132WheelWakeEmitter.cs","B0132WheelWakeDemo.cs"]:
    c=(P/"Runtime"/file).read_text(encoding="utf8")
    for forbidden in ["R1RunController","R6CombatFeedback","VfxPoolManager","ApplyDamage","AddForce(","AddTorque("]:
        assert forbidden not in c, f"canonical/physics mutation {forbidden} in {file}"
assert "ParticleSystemStopBehavior.StopEmittingAndClear" in (P/"Runtime/B0132WheelWakeEmitter.cs").read_text()
print("OWNERSHIP_AND_ASMDEF_PASS isolated runtime, 3 authored PlayMode tests, no game mutations")
print("ORIGINAL_UNITY_YAML_G0_PASS 16 GUIDs, 1 prefab, 1 scene, 2 asmdefs, 3 authored tests")
print("NATIVE_UNITY_NOT_RUN and independent B01-40/B01-60 QA not yet executed")
