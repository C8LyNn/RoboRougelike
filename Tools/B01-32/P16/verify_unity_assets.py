#!/usr/bin/env python3
"""Actual ORIGINAL repo Unity YAML/metadata/manifest source checks. NOT native Unity."""
import json
import re
from pathlib import Path
import yaml

root = Path(__file__).resolve().parents[3]
pack = root / "Assets/B0132P16VfxDemo"
manifest = json.loads((pack/"PACKAGE_MANIFEST.json").read_text(encoding="utf8"))
assert manifest["PACKAGE_ID"] == "B0132-VFX-P16-R1"
assert manifest["integrated"] is False
assert manifest["native_unity_status"] == "NOT_RUN"
assert not manifest["gameplay_mutations"]
print("MANIFEST_PASS package id, isolation and no native claims")

all_meta = [pack.with_suffix(".meta")] + list(pack.rglob("*.meta"))
assert len(all_meta) == 17, f"Expected 17 package .meta including folders; got {len(all_meta)}"
guid_map={}
for item in all_meta:
    source=item.read_text(encoding="utf8")
    match=re.search(r"(?m)^guid: ([0-9a-f]{32})$",source)
    assert match, f"Missing GUID in {item}"
    guid=match.group(1)
    assert guid not in guid_map, f"Duplicate GUID {guid}: {item} and {guid_map.get(guid)}"
    guid_map[guid]=item
    if item.name.endswith(".prefab.meta"):
        assert "PrefabImporter:" in source, f"Prefab importer invalid: {item}"
    if item.name.endswith(".cs.meta"):
        assert "MonoImporter:" in source, f"Script importer invalid: {item}"
    if item.name.endswith(".asmdef.meta"):
        assert "DefaultImporter:" in source
print(f"GUID_IMPORTER_PASS {len(all_meta)} original .meta, no collision and prefab importers")

class UnityLoader(yaml.SafeLoader):
    pass
def unity_node(loader, suffix, node):
    return loader.construct_mapping(node, deep=True)
UnityLoader.add_multi_constructor("tag:unity3d.com,2011:",unity_node)

actor_guid=re.search(r"(?m)^guid: ([0-9a-f]{32})$", (pack/"Runtime/B0132Effect.cs.meta").read_text()).group(1)
show_guid=re.search(r"(?m)^guid: ([0-9a-f]{32})$", (pack/"Runtime/B0132Showcase.cs.meta").read_text()).group(1)
objects=[]
prefab_names=["ProjectileHit","PartDetached","CriticalWarning"]
for kind, name in enumerate(prefab_names):
    path=pack/f"Prefabs/B0132_{name}.prefab"
    docs=list(yaml.load_all(path.read_text(encoding="utf8"), Loader=UnityLoader))
    assert len(docs)==3, f"{name}: expected real GameObject, Transform and MonoBehaviour"
    game,transform,behavior=docs
    assert "GameObject" in game and "Transform" in transform and "MonoBehaviour" in behavior
    g=game["GameObject"]; t=transform["Transform"]; v=behavior["MonoBehaviour"]
    assert g["m_Name"]==f"B0132_{name}" and g["m_IsActive"]==1
    assert len(g["m_Component"])==2
    assert v["m_Script"]["guid"] == actor_guid, f"Prefab {name} cannot resolve source script"
    assert v["effectKind"]==kind and v["autoPlay"]==1 and v["scale"]==1
    assert v["m_GameObject"]["fileID"] == t["m_GameObject"]["fileID"]
    objects.append(name)
    print(f"PREFAB_YAML_PASS {name} real source mode={kind}")
scene=pack/"Scenes/B0132_P16_VfxDemo.unity"
docs=list(yaml.load_all(scene.read_text(encoding="utf8"), Loader=UnityLoader))
assert len(docs)==4, f"Scene objects expected GameObject, Transform, Host and SceneRoots: {len(docs)}"
assert docs[0]["GameObject"]["m_Name"].startswith("B01-32 PASS16")
assert docs[2]["MonoBehaviour"]["m_Script"]["guid"]==show_guid
assert docs[2]["MonoBehaviour"]["autoCycle"]==1
assert docs[3]["SceneRoots"]["m_Roots"],"Scene root missing"
assert docs[2]["MonoBehaviour"]["m_GameObject"]["fileID"]==docs[1]["Transform"]["m_GameObject"]["fileID"]
print("SCENE_YAML_PASS actual original scene host GUID and linked Transform")

runtime=json.loads((pack/"Runtime/B0132.P16VfxDemo.Runtime.asmdef").read_text())
testing=json.loads((pack/"Tests/PlayMode/B0132.P16VfxDemo.PlayModeTests.asmdef").read_text())
assert runtime["name"]=="B0132.P16VfxDemo.Runtime"
assert runtime["references"]==[]
assert testing["name"]=="B0132.P16VfxDemo.PlayModeTests"
assert runtime["name"] in testing["references"]
assert testing.get("optionalUnityReferences")==["TestAssemblies"]
native=(pack/"Tests/PlayMode/B0132EffectPlayModeTests.cs").read_text()
assert native.count("[UnityTest]")==3
assert all(name in native for name in ["ThreeModes_RenderWithoutPhysicalCollisionAndClear","Disable_ClearsActiveWarningImmediately","Hit_ExpiresAutonomously"])
gameplay=(pack/"Runtime/B0132Effect.cs").read_text()
showcase=(pack/"Runtime/B0132Showcase.cs").read_text()
assert "AddComponent<Rigidbody>" not in gameplay+showcase
assert all(k not in gameplay+showcase for k in ["R1RunController","R4SalvageLedger","Vfx001R6LiveBridge","ApplyDamageResolved("])
assert all(k in gameplay for k in ["CreateHit()","CreateDetached()","CreateWarning()","private void OnDisable() => StopVisuals()"])
print("ASSEMBLIES_AND_BOUNDARY_PASS 2 asmdefs, 3 authored UnityTests, visual-only C# constraints")
print("SOURCE_G0_PASS 17/17 original metas, 3/3 original prefab YAML, 1/1 real scene YAML, 2 asmdefs, 3 authored UnityTests")
print("UNITY_NATIVE_NOT_RUN C# APIs / actual scene import / rendered particles / PlayMode not executed")
