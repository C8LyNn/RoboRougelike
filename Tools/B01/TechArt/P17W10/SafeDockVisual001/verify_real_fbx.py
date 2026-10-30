#!/usr/bin/env python3
"""Second Blender process on original REAL exported SAFE/BLOCKED FBX files.

Checks mesh axes/positions/materials/real positive AABB overlap in blocked
fixture and no blocker in safe. Actual Blender geometry; NOT Unity PhysX G1.
"""
from __future__ import annotations
import bpy
import hashlib
import json
import sys
from pathlib import Path
from mathutils import Vector

def arg(flag):
    a=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    if flag not in a:raise ValueError("MISSING_ARG:"+flag)
    return Path(a[a.index(flag)+1]).resolve()
def die(reason):raise ValueError("B0130_W10_REAL_FBX_RECEIVER_RED:"+reason)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def bounds(obj):
    verts=[obj.matrix_world@v.co for v in obj.data.vertices]
    return ([min(float(v[i]) for v in verts) for i in range(3)],
            [max(float(v[i]) for v in verts) for i in range(3)])
def overlapping(a,b):
    low,hi=bounds(a); otherlow,otherhi=bounds(b)
    return min(max(0.,min(hi[i],otherhi[i])-max(low[i],otherlow[i])) for i in range(3))
def matches(a,b,tol=.002):
    return len(a)==len(b) and max(abs(x-y) for x,y in zip(a,b))<tol
def do_case(artifact,case):
    file=artifact/case["fbx"]["file"]
    if file.stat().st_size!=case["fbx"]["bytes"] or sha(file)!=case["fbx"]["sha256"]:
        die("ACTUAL_BINARY_FBX_SOURCE_HASH:"+file.name)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(file),use_anim=False)
    bpy.context.view_layer.update()
    objects=[x for x in bpy.data.objects if x.type=="MESH"]
    expected_names={"ORIGINAL_DIMENSION_CHASSIS","ORIGINAL_DIMENSION_DOCK_TARGET","DOCK_DIRECTION_AMBER_GUIDE"}
    if case["variant"]=="BLOCKED_LAYER8":
        expected_names.add("LAYER8_FOREIGN_BLOCKER_CONTACT_PROBE")
    if len(objects)!=len(expected_names) or {x.name for x in objects}!=expected_names:
        die("ACTUAL_MESH_NAME_LOSS:"+case["rig"]+":"+str([o.name for o in objects]))
    byname={o.name:o for o in objects}
    ch=byname["ORIGINAL_DIMENSION_CHASSIS"]
    part=byname["ORIGINAL_DIMENSION_DOCK_TARGET"]
    if not matches(ch.matrix_world.translation,case["chassis_center_xyz"]):
        die("CHASSIS_WORLD_XYZ_WRONG")
    if not matches(part.matrix_world.translation,case["target_center_xyz"]):
        die("PART_TARGET_WORLD_XYZ_WRONG")
    if not matches([hi-lo for lo,hi in zip(*bounds(ch))],case["chassis_dimensions_xyz"]):
        die("CHASSIS_PHYSICAL_DIMENSIONS_CHANGED")
    if not matches([hi-lo for lo,hi in zip(*bounds(part))],case["part_dimensions_xyz"]):
        die("PART_PHYSICAL_DIMENSIONS_CHANGED")
    mats={
        "ORIGINAL_DIMENSION_CHASSIS":"PROXY_CHASSIS_VIS_ONLY",
        "ORIGINAL_DIMENSION_DOCK_TARGET":"PROXY_DETACHED_PART_TARGET_VIS_ONLY",
        "DOCK_DIRECTION_AMBER_GUIDE":"DOCK_DIRECTION_GUIDE_VIS_ONLY",
        "LAYER8_FOREIGN_BLOCKER_CONTACT_PROBE":"BLOCKER_LAYER8_VISIBLE_IN_QA_NOT_UNITY_LAYER"}
    for ob in objects:
        if len(ob.data.polygons)<6 or len(ob.data.vertices)<8:
            die("REAL_MESH_LOST_TRIANGLES:"+ob.name)
        names=[s.material.name.split(".")[0] for s in ob.material_slots if s.material]
        if mats[ob.name] not in names:
            die("ORIGINAL_MATERIAL_CLASS_LOST:"+ob.name+":"+str(names))
    if overlapping(ch,part)>.0001:die("REIMPORTED_ORIGINAL_CHASSIS_COLLIDES_WITH_TARGET")
    block=byname.get("LAYER8_FOREIGN_BLOCKER_CONTACT_PROBE")
    if case["variant"]=="SAFE":
        if block is not None:die("SAFE_HAS_RED_BLOCKER")
        if case["blocked_real_part_and_layer8_blocker_AABB_contact_m"]!=0:die("SOURCE_BLOCKER_LIE")
    else:
        if block is None:die("BLOCKED_MISSING_LAYER8_VISUAL")
        real=overlapping(part,block)
        if real<=.005 or abs(real-case["blocked_real_part_and_layer8_blocker_AABB_contact_m"])>.002:
            die("BLOCKED_VISUAL_NO_REAL_INTERSECTING_GEOMETRY:"+str(real))
        # Actual 3D negative mutant: move the real imported blocker outside
        # the target volume; overlap detector must flip from contact to clear.
        before=block.location.copy()
        try:
            block.location=before+Vector((5.,0.,0.))
            bpy.context.view_layer.update()
            if overlapping(part,block)>.005:die("LAYER8_BLOCKER_REMOVAL_NEGATIVE_FALSE_GREEN")
        finally:
            block.location=before
            bpy.context.view_layer.update()
    return {"rig":case["rig"],"variant":case["variant"],
      "real_imported_mesh_count":len(objects),"original_part_id":case["part_id"],
      "real_aabb_overlap_m":overlapping(part,block) if block else 0,
      "source_chassis_part_clearance_m":case["source_chassis_part_clearance_m"],
      "all_original_visual_material_names_preserved":True,
      "fbx_sha256":case["fbx"]["sha256"],
      "result":"FRESH_BLENDER_IMPORT_ACTUAL_ORIGINAL_SOURCE_DIMENSIONS_PASS"}

def main():
    original=arg("--original");out=arg("--out")
    out.mkdir(parents=True,exist_ok=True)
    report=json.loads((original/"source-locked-safedock-3d-qa.json").read_text())
    if report.get("package_id")!="B01-30-P17W10-SAFEDOCK-VISUAL-001/R1":
        die("WRONG_UPSTREAM_ARTIFACT")
    cases=report.get("cases")
    if not isinstance(cases,list) or len(cases)!=4:die("INCOMPLETE_SAFE_AND_BLOCKED_FIXTURE")
    if {(x["rig"],x["variant"]) for x in cases}!={(x,y) for x in ("Light","Heavy") for y in ("SAFE","BLOCKED_LAYER8")}:
        die("MISSING_PAIRED_RIGS")
    output=[do_case(original,case) for case in cases]
    if sum(x["real_imported_mesh_count"] for x in output)!=14:die("WRONG_REAL_TOTAL_MESHES")
    for case in cases:
        for kind in ("png","blend"):
            f=original/case[kind]["file"]
            if f.stat().st_size!=case[kind]["bytes"] or sha(f)!=case[kind]["sha256"]:
                die("PHYSICAL_OUTPUT_DRIFT:"+kind+":"+f.name)
    result={"package_id":report["package_id"],"source_original_catalog_commit":report["source_original_catalog_commit"],
      "source_original_catalog_git_blob":report["source_original_catalog_blob"],
      "source_hardened_sha":report["source_hardened_sha"],"fresh_blender_fbxs":output,
      "real_3d_meshes_total":14,"safe_original_rigs":2,"positive_hidden_layer8_visual_overlaps":2,
      "adversarial_geometry_blocker_removals_rejected":2,
      "source_original_geometry_only":True,
      "result":"4_REAL_FBXS_FRESH_BLENDER_REIMPORT_ORIGINAL_SOURCE_14_MESHES_PASS",
      "native_Unity_6_PhysX":"NOT_RUN","public_API_reattach_safe":"NOT_PROVEN",
      "formal_independent_B01_40_60":"PENDING"}
    (out/"real-fbx-independent-process-qa.json").write_text(json.dumps(result,indent=2)+"\n")
    print("B0130_W10_FRESH_BLENDER_IMPORT_4_ACTUAL_FBX_14_MESHES_2_HIDDEN_LAYER8_BLOCKED_OVERLAPS_PASS",
      json.dumps({"FBX":4,"meshes":14,"positive_contact_cases":2,"negative_removed_contact":2}))
if __name__=="__main__":main()
