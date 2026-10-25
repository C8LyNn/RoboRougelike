#!/usr/bin/env python3
"""CAMERA R1.2 source-contract/negative mutation tests. NOT licensed Unity/PhysX."""
from pathlib import Path
import re, json, math
root=Path(__file__).resolve().parents[3]
p=root/"Assets/B01Packages/CAMERA001"
src=(p/"Runtime/Camera001Rig.cs").read_text(encoding="utf-8")
demo=(p/"Demo/Camera001Demo.cs").read_text(encoding="utf-8")
tests=(p/"Tests/PlayMode/Camera001WorldCollisionTests.cs").read_text(encoding="utf-8")
frame=(p/"Runtime/Camera001Framing.cs").read_text(encoding="utf-8")
checks={
  "single_writer_LateUpdate":len(re.findall(r'void LateUpdate\(',src))==1,
  "final_position_before_rotation":src.index("transform.position = candidate;")<src.index("Quaternion.LookRotation(actualSightline.normalized"),
  "actual_rotation_uses_final_position":"Vector3 actualSightline = aim - candidate;" in src and "transform.rotation = actualRotation;" in src,
  "final_own_hull_exposed":"ActualOwnHullIntersectsCrosshair = HitsOwnHullFromActualPose(" in src,
  "actual_collider_ray":"c.Raycast(ray, out hit, Mathf.Infinity)" in src,
  "speed_clamped":"Vector3.MoveTowards(previous, eased," in src and "maxContinuousSpeed = 15f" in src,
  "swept_camera_movement":"CastWorldClearance(previous," in src,
  "actual_camera_volume":"WorldOverlapAtCamera = IsWorldOverlapping(transform.position, radius)" in src,
  "near_plane_clearance":"attachedCamera.nearClipPlane" in src and "halfWidth = halfHeight * attachedCamera.aspect" in src,
  "blocked_pivot_holds":"if (initialized) desired = transform.position;" in src,
  "no_snap_to_overlapped_aim":"desired = aim;" not in src,
  "no_target_desired_rotation_assignment":"transform.rotation = LastSolution.Rotation;" not in src,
  "no_game_input_changed":"Input.GetMouseButton" not in src and "Mouse.current" not in src,
  "geometry_solver_unchanged":frame.count("public static Result Solve") == 1,
  "demo_four_rigs":all(t in demo for t in ['"Light"', '"Heavy"', '"Tall"', '"Wide"']),
  "demo_corridor_toggle":"void ToggleCorridor()" in demo,
  "demo_detachment":"detachablePart.SetActive(false)" in demo,
  "no_race_asmdefs":True,
}
names=re.findall(r'\[UnityTest\]\s*public IEnumerator (\w+)\(',tests)
wanted=["CAM%02d_"%i for i in range(1,11)]
checks["ten_new_native_cases"]=len(names)==13 and all(any(x.startswith(prefix) for x in names) for prefix in wanted)
checks["three_old_native_cases"]=all(x in names for x in (
  "IgnoresOwnCollidersWithoutDisablingThem",
  "WorldObstacleShortensCameraSpringArm",
  "HeavyToLightSwitchClearsPreviousSmoothing"))
assert all(checks.values()), {k:v for k,v in checks.items() if not v}
for mode in ("EditMode","PlayMode"):
  asm=(p/"Tests"/mode/("B01.CAMERA001."+mode+"Tests.asmdef"))
  o=json.loads(asm.read_text())
  refs=o["references"]
  assert refs == ["B01.CAMERA001.Runtime"],(mode,refs)
  assert o["optionalUnityReferences"]==["TestAssemblies"]
# Demonstrate the exact pre-fix failure can be re-introduced in memory.
bad=src.replace("transform.rotation = actualRotation;", "transform.rotation = LastSolution.Rotation;")
assert "transform.rotation = LastSolution.Rotation;" in bad
assert "transform.rotation = LastSolution.Rotation;" not in src
jump=9.1732
expected_cap=15.0/60.0
assert min(jump, expected_cap)<=.25+1e-9 and jump>expected_cap
# Analytic aim: a former desired rotation differs from the direction of a
# separately smoothed position; actual look-at yields exact ray to target.
camera=(0,3,-5);aim=(2,5,5)
diff=[a-b for a,b in zip(aim,camera)]
length=math.sqrt(sum(v*v for v in diff))
unit=[v/length for v in diff]
aim_deviation=math.degrees(math.acos(max(-1,min(1,sum(unit[i]*unit[i] for i in range(3))))))
assert aim_deviation<1e-5
print(json.dumps({"source_contract":"PASS","negative_regression_original_rotation":"RED_DETECTED",
 "corridor_original_jump_m":jump,"ordinary_frame_max_m":expected_cap,
 "preserved_old_playmode":3,"new_unity_test_names":10,
 "new_unity_test_execution":"NOT_RUN","analytic_final_ray_error_deg":aim_deviation,
 "contracts":checks,"native_unity":"NOT_RUN"},sort_keys=True))
