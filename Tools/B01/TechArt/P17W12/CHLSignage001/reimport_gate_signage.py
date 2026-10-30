#!/usr/bin/env python3
"""Actual fresh Blender FBX reimport of authored original CHL003 3D signage.

Independently parse real vertices, material classes, source marker anchors,
real mesh typography and above-vehicle-plane clearance. Native Unity NOT_RUN.
"""
from __future__ import annotations
import bpy
import hashlib
import json
import math
import sys
from pathlib import Path
from mathutils import Vector
PACKAGE="B01-30-CHL003-GATE-SIGNAGE-001/R1"
def arg(flag):
    a=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    if flag not in a or a.index(flag)+1==len(a):raise ValueError("MISSING_ARG:"+flag)
    return Path(a[a.index(flag)+1]).resolve()
def fail(message):raise ValueError("B0130_W12_REAL_FBX_RECEIVER_RED:"+message)
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def bounds(ob):
    v=[ob.matrix_world @ p.co for p in ob.data.vertices]
    return (tuple(min(q[i] for q in v) for i in range(3)),
            tuple(max(q[i] for q in v) for i in range(3)))
def verify(root,case):
    word=case["variant"];f=case["files"]["fbx"];real=root/f["file"]
    if sha(real)!=f["sha256"] or real.stat().st_size!=f["bytes"]:fail("IMPORTED_BINARY_SHA_OR_BYTES_DIFFER")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(real),use_anim=False)
    bpy.context.view_layer.update()
    meshes=[ob for ob in bpy.data.objects if ob.type=="MESH"]
    if len(meshes)!=11:fail("REIMPORTED_11_ACTUAL_MESHES_REQUIRED:"+word+":"+str(len(meshes)))
    by={ob.name:ob for ob in meshes}
    wanted=["ORIGINAL_POST_CENTER_BRACKET_-1","ORIGINAL_POST_CENTER_BRACKET_1",
            "SIGN_OVERHEAD_CROSSBAR_NOT_COLLIDER",
            "SIGN_TOP_VISIBLE_ACCENT_NOT_COLLIDER",
            "SIGN_REAL_MESH_GLYPHS_"+word]
    if any(k not in by for k in wanted):fail("REAL_3D_CORE_NAME_LOST:"+str(list(by)))
    if len([ob for ob in meshes if ob.name.startswith("INWARD_CHEVRON_")])!=4:
        fail("FOUR_ACTUAL_CHEVRONS_MISSING")
    if len([ob for ob in meshes if ob.name.startswith("SOURCE_GATE_WIDTH_EDGE_")])!=2:
        fail("SOURCE_WIDTH_EDGE_MARKERS_MISSING")
    brackets=[by["ORIGINAL_POST_CENTER_BRACKET_-1"],
              by["ORIGINAL_POST_CENTER_BRACKET_1"]]
    if abs(brackets[0].matrix_world.translation.x+1.97)>.001 or abs(brackets[1].matrix_world.translation.x-1.97)>.001:
        fail("ORIGINAL_CHL_MARKER_CENTER_WAS_REPOSITIONED")
    for ob in meshes:
        if len(ob.data.polygons)<6 or len(ob.data.vertices)<8:fail("DID_NOT_IMPORT_ACTUAL_TRIANGULAR_MESH:"+ob.name)
        if not ob.material_slots or not ob.material_slots[0].material:fail("SHADING_MISSING:"+ob.name)
        if bounds(ob)[0][1]<2.548:fail("PHYSICAL_VISUAL_INTRUDES_BELOW_CHL_MARKER_TOP:"+ob.name)
    label=by["SIGN_REAL_MESH_GLYPHS_"+word]
    if len(label.data.vertices)<30 or len(label.data.polygons)<15:
        fail("FONT_WAS_NOT_CONVERTED_TO_ORIGINAL_REAL_MESH")
    lb,ub=bounds(label)
    realwidth=ub[0]-lb[0]
    if abs(realwidth-case["real_glyphs"]["glyph_width_m"])>.009:
        fail("READABLE_GLYPH_ORIGINAL_WORLD_WIDTH_DRIFT")
    color=[m.name.split(".")[0] for m in (s.material for s in label.material_slots) if m]
    if "CHL003_"+word+"_LEGIBLE_EMISSION_COLOR" not in color:
        fail("REAL_GLYPH_MATERIAL_VARIANT_LOST:"+str(color))
    # Deliberate negative on genuinely imported 3D geometry, distinct from
    # the producer's own original geometry mutation and exact-source test.
    support=brackets[0];before=support.location.copy()
    try:
        support.location.x=before.x+.38
        bpy.context.view_layer.update()
        if abs(support.matrix_world.translation.x+1.97)<.1:
            fail("MOVED_REAL_IMPORTED_3D_BRACKET_WAS_NOT_DETECTED")
    finally:
        support.location=before
        bpy.context.view_layer.update()
    # A false text mesh should also be caught if actually moved under post.
    fontorig=label.location.copy()
    try:
        label.location.y=0
        bpy.context.view_layer.update()
        if bounds(label)[0][1]>=2.55:fail("LOWERED_REAL_GLYPH_MESH_FALSE_GREEN")
    finally:
        label.location=fontorig
        bpy.context.view_layer.update()
    return {"variant":word,"actual_reimport_mesh_nodes":len(meshes),
      "source_anchor_x_m":[-1.97,1.97],"real_glyph_width_m":round(realwidth,7),
      "original_actual_glyph_polygon_count":len(label.data.polygons),
      "real_imported_mutants_rejected":2,
      "real_binary_fbx_sha256":f["sha256"],"materials_preserved":True}
def main():
    root=arg("--source");out=arg("--out");out.mkdir(parents=True,exist_ok=True)
    receipt=json.loads((root/"source-locked-gate-signage-receipt.json").read_text())
    if receipt.get("package_id")!=PACKAGE or receipt.get("real_FBX")!=2 or receipt.get("real_mesh_nodes")!=22:
        fail("INCOMPLETE_SOURCE_PHYSICAL_OUTPUT")
    if {x["variant"] for x in receipt["two_real_3d_sign_variants"]}!={"CHECKPOINT","FINISH"}:
        fail("WRONG_OR_DUPLICATED_SIGN_TYPES")
    cases=[verify(root,x) for x in receipt["two_real_3d_sign_variants"]]
    if sum(x["actual_reimport_mesh_nodes"] for x in cases)!=22:fail("ORIGINAL_MESH_LOSS")
    for case in receipt["two_real_3d_sign_variants"]:
        for kind in ("fbx","blend","png"):
            data=case["files"][kind];p=root/data["file"]
            if sha(p)!=data["sha256"] or p.stat().st_size!=data["bytes"]:
                fail("ORIGINAL_PHYSICAL_ART_BINARY_CHANGED:"+kind)
    result={"package_id":PACKAGE,"source_owner_pr":285,"original_head":receipt["source_exact_head"],
      "source_git_blob":receipt["source_original_git_blob"],
      "actual_reimport_3d_meshes":22,"actual_converted_true_word_meshes":2,
      "independent_process_negative_mutations":4,"signs":cases,
      "status":"FRESH_BLENDER_ACTUAL_CHECKPOINT_FINISH_FBX_VISUAL_G0_PRODUCER_PASS",
      "native_Unity_G1":"NOT_RUN","independent_B01_40_60":"PENDING"}
    (out/"fresh-real-fbx-signage-qa.json").write_text(json.dumps(result,indent=2)+"\n")
    print("B0130_W12_REAL_2_FBX_FRESH_BLENDER_22_MESH_2_GLYPH_4_NEW_NEGATIVES_PASS")
if __name__=="__main__":main()
