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
print(json.dumps({"result":"STATIC_PASS","12_cube_cases":len(cases),"flat_control":True,
                  "retained_urp_shader":True,"meta_pair":True,"physics_real_engine":"NOT_RUN"},sort_keys=True))
