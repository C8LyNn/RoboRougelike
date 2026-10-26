#!/usr/bin/env python3
"""CAMERA R1.2.1 read-only source guard: differential-only, preserves camera
behaviour and all R1.2 tests; independent native B01-60 QA still needed."""
from pathlib import Path
import re,json
root=Path(__file__).resolve().parents[3]
p=root/"Assets/B01Packages/CAMERA001"
src=(p/"Runtime/Camera001AimTransitionDiagnostics.cs").read_text()
cam=(p/"Runtime/Camera001Rig.cs").read_text()
tests=(p/"Tests/EditMode/Camera001AimTransitionTests.cs").read_text()
assert "public static Observation Measure(Sample sample)" in src
assert "PreviousTargetWorld" in src and "CurrentTargetWorld" in src
assert "CurrentMuzzleWorld" in src and "PreviousMuzzleForwardWorld" in src
assert "CurrentMuzzleForwardWorld" in src and "DeltaTimeSeconds" in src
assert 'TARGET_STEP_EXCEEDS_TURRET_BUDGET_PLAUSIBLE' in src
assert 'UNRESOLVED_TRACKING_GAP_REQUIRES_PAIRED_NATIVE_QA' in src
assert 'CAMERA_FINAL_POSE_UNSAFE' in src
assert 'MuzzleWorldStepExceedsTurretBudget' in src
assert not re.search(r'\b(?:transform|camera|weapon|controller)\s*\.\s*(?:position|rotation)\s*=',src)
assert 'Input.Get' not in src and 'Raycast' not in src
assert '170f' in src and '.05f' in src
assert 'transform.rotation = actualRotation;' in cam
assert 'Vector3.MoveTowards(previous, eased,' in cam
cases=re.findall(r'\[Test\]\s+public void (\w+)\(',tests)
assert len(cases)==6 and len(set(cases))==6, cases
for suffix in ['Camera001AimTransitionDiagnostics.cs','Camera001AimTransitionTests.cs']:
    folder="Runtime" if suffix.startswith('Camera001AimTransitionDiagnostics') else "Tests/EditMode"
    f=p/folder/suffix
    assert f.is_file() and f.with_name(f.name+".meta").is_file()
old=(p/"Tests/PlayMode/Camera001WorldCollisionTests.cs").read_text()
oldNames=re.findall(r'\[UnityTest\]\s+public IEnumerator (\w+)\(',old)
assert len(oldNames)==13
# Actual source requirement does not relax strict current camera/native probes.
assert 'CAM09_ContinuousCorridor_NoPivotSnapOrWallPenetration' in oldNames
print(json.dumps({"source_static":"PASS","new_editmode_cases":len(cases),
 "preserved_playmode":len(oldNames),"exact_CSharp_headless":"separate CI",
 "native_unity":"NOT_RUN"},sort_keys=True))
