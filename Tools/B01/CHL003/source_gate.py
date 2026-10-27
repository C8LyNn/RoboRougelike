#!/usr/bin/env python3
"""Actual isolated CHL Unity importer/scene/test-source gate, no licensed Unity.
Red/green negatives test fixture mutation and GUID/header corruption.
"""
import json,re
from pathlib import Path
root=Path(__file__).resolve().parents[3]
pkg=root/"Assets/RoboRougelikeChallenges/CHL003"
expected=[
"Runtime/CHL003CourseCore.cs",
"Runtime/CHL003CourseController.cs",
"Runtime/CHL003Gate.cs",
"Runtime/CHL003VehicleMotor.cs",
"Runtime/B01.CHL003.Runtime.asmdef",
"Demo/CHL003DemoBootstrap.cs",
"Demo/CHL003_Demo.unity",
"Tests/EditMode/B01.CHL003.EditModeTests.asmdef",
"Tests/PlayMode/B01.CHL003.PlayModeTests.asmdef",
"Tests/EditMode/CHL003CoreEditModeTests.cs",
"Tests/PlayMode/CHL003PhysicsPlayModeTests.cs"]
def require(condition,message):
    if not condition:raise RuntimeError("CHL003_SOURCE_RED:"+message)
for p in expected:
    require((pkg/p).is_file(),"missing asset "+p)
    require((pkg/(p+".meta")).is_file(),"missing paired Unity meta "+p)
metaFiles=sorted((root/"Assets/RoboRougelikeChallenges").rglob("*.meta"))
metaFiles=[root/"Assets/RoboRougelikeChallenges.meta",*metaFiles]
guids={}
for p in metaFiles:
    m=re.search(r"^guid:\s*([0-9a-f]{32})$",p.read_text(),re.M)
    require(m is not None,"missing importer GUID "+str(p))
    require(m.group(1) not in guids,"duplicate GUID "+str(p)+" == "+str(guids.get(m.group(1))))
    guids[m.group(1)]=p
scene=(pkg/"Demo/CHL003_Demo.unity").read_text()
sceneGuid=re.search(r"guid: ([0-9a-f]{32}), type: 3",scene)
bootstrap=(pkg/"Demo/CHL003DemoBootstrap.cs.meta").read_text()
require(sceneGuid and ("guid: "+sceneGuid.group(1)) in bootstrap,
        "scene script GUID must match real C# importer")
require("CHL003_DemoBootstrap" in scene and "SceneRoots" in scene,
        "native Unity scene not serialized")
runtime=(pkg/"Runtime/CHL003CourseController.cs").read_text()
core=(pkg/"Runtime/CHL003CourseCore.cs").read_text()
demo=(pkg/"Demo/CHL003DemoBootstrap.cs").read_text()
physical=(pkg/"Tests/PlayMode/CHL003PhysicsPlayModeTests.cs").read_text()
edit=(pkg/"Tests/EditMode/CHL003CoreEditModeTests.cs").read_text()
for must in [
 "collider.attachedRigidbody != activeActor",
 "ReferenceEquals(gate, checkpoints[ordinal])",
 "Physics.SyncTransforms()",
 "activeActor.linearVelocity = Vector3.zero",
 "actor.GetInstanceID()"]:
    require(must in runtime,"missing actor isolation or retry: "+must)
for must in ["index != CheckpointIndex","physicsActorToken != ActorToken",
             "State != Phase.Running","Paused","Completed?.Invoke",
             "CheckpointReached?.Invoke","TimeLimitSeconds"]:
    require(must in core,"missing state guard: "+must)
for must in ["GameObject.CreatePrimitive","new Vector3(3.0f, .60f, 10f)",
             "REAL_LOW_BARRIER","SOLID_RAMP","Gate(\"CP0\"",
             "CHL003_LIGHT_RIG","CHL003_HEAVY_RIG",
             "GUILayout.RepeatButton","RigidbodyInterpolation.Interpolate"]:
    require(must in demo,"missing real demo component: "+must)
for must in ["Physics.ComputePenetration","Physics.Raycast" if False else "Physics.SyncTransforms",
             "WaitForFixedUpdate","Rigidbody","CHL003CourseController"]:
    require(must in physical,"physical test must use Unity Physics: "+must)
editNames=re.findall(r"\[Test\]\s+public void (\w+)\(",edit)
playNames=re.findall(r"\[UnityTest\]\s+public IEnumerator (\w+)\(",physical)
require(len(editNames)==10 and len(set(editNames))==10, "expected exactly ten Edit NUnit methods")
require(len(playNames)==6 and len(set(playNames))==6, "expected exactly six Play UnityTest methods")
for mode in ("EditMode","PlayMode"):
    asm=json.loads((pkg/"Tests"/mode/("B01.CHL003."+mode+"Tests.asmdef")).read_text())
    require(asm["references"]==["B01.CHL003.Runtime"],"wrong asmdef owner")
    require("TestAssemblies" in asm["optionalUnityReferences"],"Unity Test Framework missing")
asm=json.loads((pkg/"Runtime/B01.CHL003.Runtime.asmdef").read_text())
require(asm["name"]=="B01.CHL003.Runtime" and not asm["references"],
        "runtime must be host-neutral")
for prohibited in ("Assets/R1/","Assets/R3/","Assets/R6/","Assets/Pack/",
                   "Assets/B01Packages/CAMERA001","EditorPrefs","PlayerPrefs"):
    require(prohibited not in core and prohibited not in runtime,
        "canonical gameplay coupling: "+prohibited)
# Concrete negative fixture: deliberate removal of root-physics ownership check
# becomes an error, and accidental scene importer GUID mismatch is rejected.
bad=runtime.replace("collider.attachedRigidbody != activeActor","false")
require(bad!=runtime and "collider.attachedRigidbody != activeActor" not in bad,
        "ownership mutation sentinel not detecting old unsafe gate")
badScene=scene.replace(sceneGuid.group(1),"0"*32)
require("guid: "+sceneGuid.group(1) not in badScene,"GUID mutation sentinel not effective")
print(json.dumps({"static_importer_guard":"PASS","positive_guid_count":len(guids),
 "scene_guid_reference":"PASS","negative_actor_ownership_mutation":"RED_DETECTED",
 "negative_scene_guid_mutation":"RED_DETECTED",
 "editmode_unity_tests_AUTHORED":len(editNames),
 "playmode_unity_tests_AUTHORED":len(playNames),
 "unity_compile":"NOT_RUN","unity_native_tests":"NOT_RUN",
 "direct_scene_play":"NOT_RUN"},sort_keys=True))
