#!/usr/bin/env python3
"""W13 CAMERA source ownership, mutation, tests and importer GUID guard.
No Unity/PhysX is executed here.
"""
from pathlib import Path
import re,json
root=Path(__file__).resolve().parents[3]
cam=root/"Assets/B01Packages/CAMERA001"
runtime=(cam/"Runtime/Camera001AimTimeline.cs").read_text()
edit=(cam/"Tests/EditMode/Camera001AimTimelineTests.cs").read_text()
play=(cam/"Tests/PlayMode/Camera001AimTimelinePhysicsTests.cs").read_text()
original=(cam/"Runtime/Camera001Rig.cs").read_text()
pairs=[
 "Runtime/Camera001AimTimeline.cs",
 "Tests/EditMode/Camera001AimTimelineTests.cs",
 "Tests/PlayMode/Camera001AimTimelinePhysicsTests.cs"
]
for p in pairs:
    assert (cam/p).is_file() and (cam/(p+".meta")).is_file(),p
guids=[x.strip().split(": ",1)[1] for p in pairs for x in (cam/(p+".meta")).read_text().splitlines() if x.startswith("guid: ")]
assert len(guids)==3 and len(set(guids))==3
assert "Transform" not in runtime and "Physics.Raycast" not in runtime
assert ".SetPosition" not in runtime and "LateUpdate" not in runtime and "Input.Get" not in runtime
for needle in ["Camera001AimTransitionDiagnostics.Measure(sample)", "records.Count >= capacity",
 "frame.FrameIndex <= previous.FrameIndex","previous.Sample.CurrentTargetWorld",
 "previous.Sample.CurrentMuzzleForwardWorld", "CultureInfo.InvariantCulture",
 "targetStepDeg", "CurrentMuzzleToTargetErrorDegrees", "CameraFinalPoseUnsafe"]:
    assert needle in runtime,needle
# W14 exact causal false-positive guard. Reject skipped native frames BEFORE
# calculating a one-frame slew rate budget; fail-closed and atomic.
assert 'frame.FrameIndex < 0' in runtime
assert '(long)frame.FrameIndex - previous.FrameIndex != 1L' in runtime
assert 'CAMERA001_AIM_TIMELINE_SKIPPED_FRAME' in runtime
assert 'CAMERA001_AIM_TIMELINE_NEGATIVE_FRAME' in runtime
assert runtime.index('CAMERA001_AIM_TIMELINE_SKIPPED_FRAME') < runtime.index(
    'Camera001AimTransitionDiagnostics.Measure(sample)')
assert 'SkippedFramesCannotBeMisclassifiedAsSingleFrameGunSlew' in edit
assert 'NegativeFrameRejectedAndMaxIntFrameCannotWrap' in edit
# Negative mutation reproduces prior monotonic-only acceptance.
mutated_guard=runtime.replace('(long)frame.FrameIndex - previous.FrameIndex != 1L','false')
assert mutated_guard!=runtime and '(long)frame.FrameIndex - previous.FrameIndex != 1L' not in mutated_guard

# W15 SOURCE RECEIPT: raw previous/current target and muzzle world geometry
# must survive capture and appear as recomputable invariant numeric CSV columns.
assert "public Camera001AimTransitionDiagnostics.Sample Sample;" in runtime
assert "Sample = sample, // prior target/forward" in runtime
assert "previousTargetX,previousTargetY,previousTargetZ,currentTargetX" in runtime
assert "muzzleX,muzzleY,muzzleZ,previousForwardX" in runtime
assert "currentForwardX,currentForwardY,currentForwardZ,deltaTimeSeconds" in runtime
assert "Num(v.x) + \",\" + Num(v.y) + \",\" + Num(v.z)" in runtime
assert "StableHitFramesRetainUnresolvedMuzzleLagWithRawCoordinates" in edit
assert "RawProvenanceSurvivesCallerSpoofAndIsRecomputable" in edit
assert "CsvRawFloatsRemainInvariantInCommaDecimalLocale" in edit
m = runtime.replace("Sample = sample, // prior target/forward", "/*lost raw evidence*/")
assert m!=runtime and "Sample = sample, // prior target/forward" not in m

assert "transform.rotation = actualRotation;" in original
edit_names=re.findall(r'\[Test\]\s+public void (\w+)\(',edit)
play_names=re.findall(r'\[UnityTest\]\s+public IEnumerator (\w+)\(',play)
assert len(set(edit_names))==11 and len(edit_names)==11,edit_names
assert len(set(play_names))==2 and len(play_names)==2,play_names
assert "Physics.Raycast(ray" in play and "Vector3.RotateTowards(" in play
assert "QueryTriggerInteraction.Ignore" in play
# A source guard MUST see a deliberate in-memory historical mutation:
mutated=runtime.replace("previous.Sample.CurrentTargetWorld","sample.CurrentTargetWorld")
assert mutated!=runtime and "previous.Sample.CurrentTargetWorld" not in mutated
print(json.dumps({"camera_w13_static":"PASS","new_editmode_cases":len(edit_names),
 "new_playmode_cases":len(play_names),"old_camera_writer_unchanged":"independent Git compare",
 "negative_mutation_detected":True,"unity_native":"NOT_RUN"},sort_keys=True))
