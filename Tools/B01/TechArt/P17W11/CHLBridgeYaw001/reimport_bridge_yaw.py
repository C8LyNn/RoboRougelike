#!/usr/bin/env python3
"""Independent fresh Blender process: ACTUALLY reimport four FBX 3D mesh assets.

Recompute x extents from original actual vertices, four wheels and chassis;
never rely only on metadata width claims. Visual-only, native Unity NOT_RUN.
"""
from __future__ import annotations
import bpy
import hashlib
import json
import math
import sys
from pathlib import Path
from mathutils import Vector
def arg(s):
    a=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    if s not in a:raise ValueError("MISSING:"+s)
    return Path(a[a.index(s)+1]).resolve()
def fail(s):raise ValueError("B0130_W11_FRESH_FBX_REIMPORT_REJECT:"+s)
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def xspan(meshes):
    positions=[(o.matrix_world@v.co).x for o in meshes for v in o.data.vertices]
    return max(positions)-min(positions)
def verify(origin,item):
    rig=item["rig"];yaw=item["yaw_degrees"]
    info=item["files"]["fbx"]
    path=origin/info["name"]
    if path.stat().st_size!=info["bytes"] or sha(path)!=info["sha256"]:
        fail("ORIGINAL_BINARY_FBX_SHA_DRIFT")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path),use_anim=False)
    bpy.context.view_layer.update()
    meshes=[o for o in bpy.data.objects if o.type=="MESH"]
    vehicles=[o for o in meshes if o.name.startswith("CHL003_"+rig+"_")]
    if len(vehicles)!=5 or len(meshes)!=10:
        fail("ACTUAL_REIMPORT_MESH_PARTS:"+rig+":"+str(len(meshes))+"/"+str(len(vehicles)))
    if len([o for o in vehicles if "WHEEL" in o.name])!=4:
        fail("ACTUAL_SPHERE_WHEEL_COUNT")
    if len([o for o in vehicles if "CHASSIS" in o.name])!=1:
        fail("ORIGINAL_CHASSIS_MISSING")
    if len([o for o in meshes if o.name=="CHL003_SOURCE_BRIDGE_DECK"])!=1:
        fail("REAL_BRIDGE_DECK_MISSING")
    for o in meshes:
        if len(o.data.vertices)<8 or len(o.data.polygons)<6:
            fail("NOT_REAL_3D_TRIANGLES:"+o.name)
        if not o.material_slots or not o.material_slots[0].material:
            fail("VISUAL_MATERIAL_MISSING:"+o.name)
    width=xspan(vehicles)
    if abs(width-item["actual_blender_mesh_bounding_width_m"])>.01:
        fail("ACTUAL_REIMPORTED_GEOMETRY_DISAGREES:"+str(width)+"/"+str(item["actual_blender_mesh_bounding_width_m"]))
    if abs(width-item["analytic_wheel_inclusive_body_width_m"])>.02:
        fail("ACTUAL_REAL_MESH_DIFFERS_FROM_SOURCE_BOUNDS")
    clearance=(item["source_bridge_width_m"]-width)/2
    # Exact *physical mesh* width differs from conservative sphere envelope by
    # discrete UV tessellation. The original source analytic policy must hold.
    if item["source_policy_pass"] is not (item["conservative_each_side_clearance_m"]>=item["source_minimum_side_clearance_m"]-1e-8):
        fail("ORIGINAL_POLICY_LABEL_FORGED")
    if (clearance>=item["source_minimum_side_clearance_m"]-.01)!=item["source_policy_pass"]:
        fail("REIMPORTED_POSE_CLASSIFICATION_CHANGED")
    # Actual 3D negative: move one original wheel x+5 and assert xspan grows.
    wheel=next(o for o in vehicles if "WHEEL" in o.name)
    before=wheel.location.copy()
    wheel.location=before+Vector((5.,0,0))
    bpy.context.view_layer.update()
    if xspan(vehicles)<=width+1:
        fail("MOVED_REAL_VERTEX_NEGATIVE_FALSE_GREEN")
    return {"rig":rig,"yaw_degrees":yaw,"reimported_real_mesh_count":len(meshes),
      "reimported_original_vehicle_meshes":len(vehicles),
      "actual_real_reimported_mesh_width_m":round(width,6),
      "real_mesh_side_clearance_m":round(clearance,6),
      "original_source_policy_pass":item["source_policy_pass"],
      "original_FBX_sha256":info["sha256"],"actual_vertex_mutant_detected":True}
def main():
    source=arg("--source");out=arg("--out")
    out.mkdir(parents=True,exist_ok=True)
    receipt=json.loads((source/"original-chl003-bridge-yaw-3d-receipt.json").read_text())
    if receipt.get("package_id")!="B01-30-CHL003-BRIDGE-YAW-3D-001/R1" or receipt.get("real_FBX")!=4:
        fail("WRONG_3D_PRODUCER_OR_MISSING_FILES")
    rows=[verify(source,item) for item in receipt["pose_results"]]
    if len(rows)!=4 or sum(row["reimported_real_mesh_count"] for row in rows)!=40 or sum(row["original_source_policy_pass"] for row in rows)!=3:
        fail("INCOMPLETE_CASE_MATRIX")
    for case in receipt["pose_results"]:
        for k in ("fbx","blend","png"):
            f=source/case["files"][k]["name"]
            if f.stat().st_size!=case["files"][k]["bytes"] or sha(f)!=case["files"][k]["sha256"]:
                fail("ORIGINAL_THREE_ARTIFACT_SOURCE_CHECK:"+k)
    result={"package_id":receipt["package_id"],"status":"4_ACTUAL_FBX_REAL_GEOMETRY_ROUNDTRIP_PASS",
       "input_original_pr":285,"input_original_head":receipt["original_head"],
       "physical_scene_mesh_total":40,"original_vehicle_part_mesh_total":20,
       "real_reimport_width_verifications":4,"real_mutated_wheel_position_cases":4,
       "source_policy_passes":3,"source_policy_expected_negative":1,"per_pose":rows,
       "native_Unity_PhysX":"NOT_RUN","B01_40_60_independent":"PENDING",
       "not_a_gameplay_collider_test":True}
    (out/"original-chl003-fbx-fresh-reimport-receipt.json").write_text(json.dumps(result,indent=2)+"\n")
    print("B0130_W11_4_ACTUAL_FBX_FRESH_BLENDER_IMPORT_40_MESH_4_VERTEX_MUTANTS_3_PASS_1_FAIL_OK")
if __name__=="__main__":main()
