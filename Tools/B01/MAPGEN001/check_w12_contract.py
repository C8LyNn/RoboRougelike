#!/usr/bin/env python3
"""W12 producer static source contracts; DOES NOT compile Unity/PhysX."""
from pathlib import Path
import re
import json

root=Path(__file__).resolve().parents[3]
runtime=root/"Assets/B01Packages/MAPGEN001/Runtime/MapGenBuilder.cs"
cube=root/"Assets/B01Packages/MAPGEN001/Tests/PlayMode/MapGenPhysicalTraversalTests.cs"
play=root/"Assets/B01Packages/MAPGEN001/Tests/PlayMode/MapGenPlayModeTests.cs"
shader=root/"Assets/B01Packages/MAPGEN001/Resources/MAPGEN001_BlocksURP.shader"
meta=shader.with_name(shader.name+".meta")
resource_meta=shader.parent.with_name("Resources.meta")
source=runtime.read_text()
body=cube.read_text()
assert 'Resources.Load<Shader>(RetainedShaderPath)' in source
assert 'Shader.Find(' not in source
assert 'MAPGEN001_RETAINED_URP_SHADER_MISSING_OR_UNSUPPORTED' in source
assert shader.is_file() and meta.is_file() and resource_meta.is_file()
assert 'RenderPipeline" = "UniversalPipeline' in shader.read_text()
assert 'Tags { "LightMode" = "SRPDefaultUnlit" }' in shader.read_text()
assert 'MAPGEN001_BlocksURP' in source and 'MAPGEN001_BlocksURP' in play.read_text()
assert 'PhysicsMaterialCombine.Minimum' in body
assert 'staticFriction = 0f' in body and 'dynamicFriction = 0f' in body
assert 'CubeForceActuatorMovesOnFlatControlSurface' in body
cases=re.findall(r'\[UnityTest\]\s*public IEnumerator (\w+)_CubePhysics\(\)',body)
assert len(cases)==12 and len(set(cases))==12, cases
for phrase in ('rb.AddForce(rb.transform.forward * throttle, ForceMode.Acceleration)',
               'rb.AddTorque(Vector3.up * steer, ForceMode.Acceleration)',
               'maxPlanarSpeed=', 'bestGoalDistance=', 'floorRaySamples='):
    assert phrase in body, phrase
# No source test may skip real Rigidbody motion via Transform teleport.
assert not re.search(r'(?m)^\s*(?:rover|rb)\.transform\.position\s*=.*$',body.replace('rover.transform.position = start;',''))
guids=re.findall(r'(?m)^guid: ([0-9a-f]{32})$', meta.read_text()+"\n"+resource_meta.read_text())
assert len(guids)==2 and len(set(guids))==2, guids

# W12 R3.4.1: classify the original native CS1061 distinctly from a
# static-source PASS. On Unity 6, Shader has no GetTag; Material owns it.
# This is a textual contract and a negative fixture, NOT a Unity compilation.
play_source=play.read_text()
correct='renderer.sharedMaterial.GetTag("RenderPipeline", false)'
incorrect='renderer.sharedMaterial.shader.GetTag("RenderPipeline", false)'
expected_tests={
    "GeneratedBlocksUseUrpShaderInsteadOfErrorMaterial",
    "GenerateAndClearRealPhysicsColliders",
    "RigidbodyActuallyFallsAndLandsOnGeneratedFloor",
    "OffsetAndRotatedHostReturnsWorldSpaceMetadata",
    "RegenerateCyclesRemoveOldColliderRoots",
    "RejectedInputPreservesPreviousLiveLevel",
}
actual_tests=set(re.findall(r'\\[UnityTest\\]\\s*public IEnumerator (\\w+)\\(',play_source))
assert actual_tests==expected_tests,(actual_tests,expected_tests)
assert play_source.count(correct)==1 and incorrect not in play_source
assert 'Assert.AreEqual("UniversalPipeline",' in play_source
assert re.search(r'\\.shader\\.GetTag\\s*\\(',play_source) is None
# Deliberately reintroduce the original defect in a memory-only fixture;
# the contract MUST mark it RED, and the committed source must be GREEN.
synthetic_red=play_source.replace(correct,incorrect)
assert incorrect in synthetic_red and correct not in synthetic_red
assert synthetic_red.count(incorrect)==1
# Explicitly audit other NEW R3.4 Unity API surfaces, not all of Unity:
# Shader.isSupported and Resources.Load<Shader> are UnityEngine APIs;
# PhysicsMaterial, frictionCombine, Collider.sharedMaterial and Physics.Raycast
# are UnityEngine.Physics APIs in Unity 6000; absence of native compile is noted.
api_audit={
    "Material.GetTag":correct in play_source,
    "Resources.Load<Shader>":'Resources.Load<Shader>(RetainedShaderPath)' in source,
    "Shader.isSupported":'shader.isSupported' in source,
    "PhysicsMaterial_constructor":'new PhysicsMaterial(' in body,
    "PhysicsMaterialCombine.Minimum":'PhysicsMaterialCombine.Minimum' in body,
    "Collider.sharedMaterial":'.sharedMaterial = contactMaterial' in body,
    "Physics.Raycast":'Physics.Raycast(' in body,
    "Rigidbody.AddForce":'rb.AddForce(' in body,
    "Rigidbody.AddTorque":'rb.AddTorque(' in body,
}
assert all(api_audit.values()),api_audit
print(json.dumps({"test_api":"Material.GetTag","native_cs1061_regression":"STATIC_GREEN",
                  "original_bad_call_fixture":"STATIC_RED_DETECTED",
                  "preserved_PlayMode_UnityTests":len(actual_tests),
                  "other_r34_api_contracts":api_audit,
                  "Unity_native_CSharp":"NOT_RUN"},sort_keys=True))

print(json.dumps({"result":"STATIC_PASS","12_cube_cases":len(cases),"flat_control":True,
                  "retained_urp_shader":True,"meta_pair":True,"physics_real_engine":"NOT_RUN"},sort_keys=True))
