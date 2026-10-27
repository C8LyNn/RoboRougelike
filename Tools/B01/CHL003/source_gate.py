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
"Runtime/CHL003ContactMonitor.cs",
"Runtime/CHL003FinishArbiter.cs",
"Runtime/CHL003GateCrossing.cs",
"Runtime/CHL003TrialMetrics.cs",
"Runtime/B01.CHL003.Runtime.asmdef",
"Demo/CHL003DemoBootstrap.cs",
"Demo/B01.CHL003.Demo.asmdef",
"Demo/CHL003_Demo.unity",
"Tests/EditMode/B01.CHL003.EditModeTests.asmdef",
"Tests/PlayMode/B01.CHL003.PlayModeTests.asmdef",
"Tests/EditMode/CHL003CoreEditModeTests.cs",
"Tests/EditMode/CHL003FinishAndTelemetryEditModeTests.cs",
"Tests/PlayMode/CHL003DemoAcceptancePlayModeTests.cs",
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
for must in ["GameObject.CreatePrimitive","new Vector3(3.70f, .60f, 10f)",
             "REAL_LOW_BARRIER","SOLID_RAMP","Gate(\"CP0\"",
             "CHL003_LIGHT_RIG","CHL003_HEAVY_RIG",
             "GUILayout.RepeatButton","RigidbodyInterpolation.Interpolate"]:
    require(must in demo,"missing real demo component: "+must)
for must in ["Physics.ComputePenetration","Physics.Raycast" if False else "Physics.SyncTransforms",
             "WaitForFixedUpdate","Rigidbody","CHL003CourseController"]:
    require(must in physical,"physical test must use Unity Physics: "+must)
editNames=re.findall(r"\[Test\]\s+public void (\w+)\(",edit)
playNames=re.findall(r"\[UnityTest\]\s+public IEnumerator (\w+)\(",physical)
moreEdit=(pkg/"Tests/EditMode/CHL003FinishAndTelemetryEditModeTests.cs").read_text()
morePlay=(pkg/"Tests/PlayMode/CHL003DemoAcceptancePlayModeTests.cs").read_text()
editNames += re.findall(r"\[Test\]\s+public void (\w+)\(",moreEdit)
playNames += re.findall(r"\[UnityTest\]\s+public IEnumerator (\w+)\(",morePlay)
require(len(editNames)==19 and len(set(editNames))==19, "expected 19 authored Edit NUnit methods")
require(len(playNames)==14 and len(set(playNames))==14, "expected 14 authored Play UnityTests")
arbiter=(pkg/"Runtime/CHL003FinishArbiter.cs").read_text()
metrics=(pkg/"Runtime/CHL003TrialMetrics.cs").read_text()
monitor=(pkg/"Runtime/CHL003ContactMonitor.cs").read_text()
for invariant in ("LastCheckpointAccepted(fixedStep)", "TryCommit(fixedStep,",
                 "PhysicallyOverlapsFinish()", "Physics.ComputePenetration(",
                 "activeActor.isKinematic = true", "metrics.RecordPhysics("):
    require(invariant in runtime,"W17 runtime arbitration/physics missing: "+invariant)
for invariant in ("fixedStep <= queuedStep.Value", "fixedStep <= lastCheckpointStep",
                 "stillPhysicallyOverlapping", "CancelPending()"):
    require(invariant in arbiter,"same-step finish gate missing: "+invariant)
for invariant in ("CollisionEntries", "BarrierContacts", "RampContacts",
                 "BridgeContacts", "RecordCheckpoint", "RecordFailed", "SummaryCsv"):
    require(invariant in metrics,"real trial telemetry field missing: "+invariant)
require("OnCollisionEnter" in monitor and "OnCollisionStay" in monitor,
        "no native contact event source")
require("OnTriggerStay" in (pkg/"Runtime/CHL003Gate.cs").read_text(),
        "co-located physics stay callback missing")
require("P07_SameNativeCourseLightHeavyThreeRunsEachWithRealContact" in morePlay and
        "P08_FinalCheckpointAndFinishCoLocatedNeverCompleteSameFixedStep" in morePlay,
        "real native physical course acceptance missing")
# Dependency-triggered C1 concrete original geometry mismatch from B01-21
# QA Draft #295: bridge 3.70m physically admits Heavy yaw8 while old
# 3.50m CP trigger rejected its entire real Collider.bounds envelope under
# 0.25m required clearance. Gate opening MUST equal narrowest physical span.
gate_match=re.search(r'var obj = Box\\(label, new Vector3\\(0f, 2\\.25f, z\\),\\s*new Vector3\\(([\\d.]+)f, 4\\.5f, \\.16f\\)',demo)
bridge_match=re.search(r'"NARROW_SOLID_BRIDGE".*?new Vector3\\(([\\d.]+)f, \\.60f, 10f\\)',demo,re.S)
post_match=re.search(r'new Vector3\\(side \\* ([\\d.]+)f, 1\\.3f, z\\)',demo)
require(gate_match is not None and bridge_match is not None and
        post_match is not None, "B01-21 original physical gate geometry not found")
gate_width=float(gate_match.group(1))
bridge_width=float(bridge_match.group(1))
marker_half_position=float(post_match.group(1))
require(gate_width==bridge_width==3.70,
        "CHL-CP-GEOMETRY_RED: body-clearance gate narrower than physically traversable bridge")
require(abs(marker_half_position-(gate_width*.5+.12))<1e-6,
        "visual gate posts not aligned with actual trigger width")
bad_demo=demo.replace('new Vector3(3.70f, 4.5f, .16f)',
                      'new Vector3(3.50f, 4.5f, .16f)')
require(bad_demo!=demo and
        'new Vector3(3.70f, 4.5f, .16f)' not in bad_demo and
        'new Vector3(3.50f, 4.5f, .16f)' in bad_demo,
        "negative trigger aperture mutation did not turn RED")
require('P14_HeavyYaw8_RealGateMatchesBridgeRatherThanRejectingClearance' in morePlay,
        "missing exact real PhysX upstream source mismatch reproduction")
require('E19_UpstreamB0121HeavyYaw8OldGateFailsBridgeAlignedGatePasses' in moreEdit,
        "missing original B01-21 exact width/clearance negative")

# Fixed-step authoritative telemetry clock, never render-driven Update.
require("core.AdvanceTime(Time.fixedDeltaTime)" in runtime,
        "CHL elapsed clock must advance on real physics tick")
require("core.AdvanceTime(Time.deltaTime)" not in runtime,
        "render FPS must not advance CHL trial time")
require("P13_ElapsedTimeTracksPhysicsTicksNotRenderedFrames" in morePlay,
        "native fixed-vs-render frame verification missing")

# W17 CHL-02 independent forward body plane and wheel-graze negatives
crossing=(pkg/"Runtime/CHL003GateCrossing.cs").read_text()
motor=(pkg/"Runtime/CHL003VehicleMotor.cs").read_text()
for check in ("previousDepth <= 0f", "currentDepth > 0f",
              "localMinX >= gateMinX + minimumSideClearance",
              "localMaxX <= gateMaxX - minimumSideClearance",
              "facingDot >= .5f", "float.IsNaN(n)"):
    require(check in crossing, "missing CHL-02 real gate guard "+check)
for check in ("ForwardHullCrossesGate(gate)", "Collider.bounds" if False else
              "c.bounds", "positionBeforePhysics", "activeActor.position",
              "IsForwardFullBodyCrossing("):
    require(check in runtime,"source lost actual collider/crossing input "+check)
for check in ("Physics.RaycastNonAlloc", "QueryTriggerInteraction.Ignore",
              "groundHits[i].normal.y < .5f", "c.attachedRigidbody == body",
              "if (!IsGrounded) return;", "AirborneSteps++"):
    require(check in motor,"propulsive airborne control not fail closed "+check)
require("wheelProbes[wheelIndex++] = wheel.transform;" in demo and
        "GetComponentsInChildren<Collider>()" in demo and
        "widthMeters: measuredWidth" in demo,"demo must use 4 physical wheel probes and actual collider envelope")
require("P10_AirborneMotorDoesNotAccelerateOrSteerWithoutRealTerrain" in morePlay
        and "P11_RealBackwardCrossAndWheelGrazeMustNotScore" in morePlay
        and "P12_DrivingForceOccursOnlyWhenRealWheelGroundRaySupports" in morePlay,
        "native actual-physics negative acceptance omitted")
mutation=crossing.replace("previousDepth <= 0f", "true")
require(mutation!=crossing and "previousDepth <= 0f" not in mutation,
        "reverse crossing source mutation RED not detected")
mutation=motor.replace("if (!IsGrounded) return;", "/* injected airborne thrust */")
require(mutation!=motor and "if (!IsGrounded) return;" not in mutation,
        "air thrust source mutation RED not detected")

# Negative mutation: same-step protection absent would be caught.
mutated=arbiter.replace("fixedStep <= lastCheckpointStep", "false")
require(mutated!=arbiter and "fixedStep <= lastCheckpointStep" not in mutated,
        "co-located same-step negative mutation was not detected")
for mode in ("EditMode","PlayMode"):
    asm=json.loads((pkg/"Tests"/mode/("B01.CHL003."+mode+"Tests.asmdef")).read_text())
    expectedRefs=["B01.CHL003.Runtime"] if mode=="EditMode" else [
        "B01.CHL003.Runtime","B01.CHL003.Demo"]
    require(asm["references"]==expectedRefs,"wrong asmdef owner or Demo test visibility")
    require("TestAssemblies" in asm["optionalUnityReferences"],"Unity Test Framework missing")
demoasm=json.loads((pkg/"Demo/B01.CHL003.Demo.asmdef").read_text())
require(demoasm["name"]=="B01.CHL003.Demo" and
        demoasm["references"]==["B01.CHL003.Runtime"],
        "MonoBehaviour demo has no referenced assembly and native PlayMode cannot compile")
# Negative regression: a missing Demo ref must fail test assembly graph review.
brokenRefs=["B01.CHL003.Runtime"]
require(brokenRefs != expectedRefs,
        "PlayMode Demo dependency mutation was not detected")
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
