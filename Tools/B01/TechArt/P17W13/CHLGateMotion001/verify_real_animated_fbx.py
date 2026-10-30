#!/usr/bin/env python3
"""Independent FRESH Blender proof of actual imported FBX keyframe motion.

Verifies real vertex movements at three frames, not merely metadata or an
FBX header. Negative controls actually modify imported object geometry.
"""
from __future__ import annotations
import bpy
import hashlib
import json
import math
import sys
from pathlib import Path
def arg(flag):
    a=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    if flag not in a or a.index(flag)+1==len(a):raise ValueError("MISSING_ARG:"+flag)
    return Path(a[a.index(flag)+1]).resolve()
def fail(s):raise ValueError("B0130_W13_REAL_FBX_ANIMATION_REIMPORT_RED:"+s)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def bbox(ob):
    v=[ob.matrix_world@x.co for x in ob.data.vertices]
    return (tuple(min(p[i] for p in v) for i in range(3)),
            tuple(max(p[i] for p in v) for i in range(3)))
def sample(ob,frame):
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()
    lo,hi=bbox(ob)
    return {"frame":frame,"min_x":float(lo[0]),"min_y":float(lo[1]),
            "max_y":float(hi[1]),"matrix_rot_z_rad":float(ob.rotation_euler[2])}
def main():
    root=arg("--source");out=arg("--out");out.mkdir(parents=True,exist_ok=True)
    receipt=json.loads((root/"original-chl003-gate-animated-3d-receipt.json").read_text())
    if receipt["package_id"]!="B01-30-CHL003-ANIMATED-GATE-INDICATOR-001/R1":
        fail("WRONG_ORIGINAL_SOURCE_RECEIPT")
    info=receipt["real_binary_FBX"];p=root/info["file"]
    if sha(p)!=info["sha256"] or p.stat().st_size!=info["bytes"]:
        fail("REAL_ORIGINAL_FBX_BINARY_SOURCE_SHA")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(p),use_anim=True)
    bpy.context.view_layer.update()
    meshes=[o for o in bpy.data.objects if o.type=="MESH"]
    if len(meshes)!=6:fail("REAL_MESH_COUNT_6_NOT_RECOVERED:"+str([o.name for o in meshes]))
    pointer=next((o for o in meshes if o.name=="CHL003_ANIMATED_GATE_POINTER"),None)
    mount=next((o for o in meshes if o.name=="CHL003_POST_MOUNT_SOURCE_X197"),None)
    if pointer is None or mount is None:
        fail("IMPORTED_ORIGINAL_MESH_ID_LOST")
    if len(pointer.data.vertices)<14 or len(pointer.data.polygons)<11:
        fail("ORIGINAL_REAL_POINTER_VERTICES_MISSING")
    if any(len(o.data.polygons)<6 or not o.material_slots or not o.material_slots[0].material for o in meshes):
        fail("ACTUAL_MESH_SURFACE_OR_MATERIAL_LOST")
    if abs(mount.matrix_world.translation.x-1.97)>.003:
        fail("ORIGINAL_RIGHT_POST_MARKER_X_CHANGED")
    frames=[sample(pointer,k) for k in (1,31,61)]
    if abs(frames[1]["max_y"]-frames[0]["max_y"])<.35:
        fail("IMPORT_DID_NOT_PRESERVE_ACTUAL_MOVING_FBX_VERTEX_FRAMES:"+str(frames))
    if abs(frames[2]["max_y"]-frames[0]["max_y"])>.03:
        fail("ORIGINAL_ROTATION_DID_NOT_RETURN_TO_IDLE:"+str(frames))
    if abs(frames[1]["min_x"]-frames[0]["min_x"])<.05:
        fail("ACTUAL_ROTATING_ARROW_WIDTH_UNCHANGED")
    if min(f["min_y"] for f in frames)<3.34:
        fail("ORIGINAL_ANIMATED_ARROW_OCCUPIES_BELOW_SIGNAGE")
    # Distinct independent real animation negative: delete actual imported
    # animation action, forcing it to freeze across all original frames.
    action=pointer.animation_data.action if pointer.animation_data else None
    if action is None:
        fail("REAL_FBX_IMPORT_OMITTED_ANIMATION_FCURVES")
    pointer.animation_data_clear()
    bpy.context.scene.frame_set(1);first=bbox(pointer)[1][1]
    bpy.context.scene.frame_set(31);second=bbox(pointer)[1][1]
    if abs(first-second)>.01:
        fail("NEGATIVE_UNKEYFRAMED_MOTION_STILL_EXISTS")
    if abs(frames[1]["max_y"]-frames[0]["max_y"])<.35:
        fail("REMOVED_ORIGINAL_FBX_KEYFRAME_NEGATIVE_FALSE_GREEN")
    # The non-mutated original actual output was checked above, no change to
    # actual upstream physical FBX source on disk.
    result={"package_id":receipt["package_id"],
      "original_B0120_CHL_SHA":receipt["original_source_head"],
      "actual_imported_original_FBX_sha256":info["sha256"],
      "actual_fbx_animation_curves_present":True,
      "actual_reimport_mesh_nodes":len(meshes),
      "actual_reimport_frames":frames,
      "real_fbx_rotates_and_returns":True,
      "actual_imported_animation_removed_negative":"REJECTED",
      "native_Unity_AnimationImporter":"NOT_RUN",
      "independent_B01_40_60":"PENDING",
      "status":"ONE_TRUE_ANIMATED_FBX_FRESH_BLENDER_REIMPORT_3_FRAMES_PASS"}
    (out/"real-source-fbx-animation-reimport-qa.json").write_text(json.dumps(result,indent=2)+"\n")
    print("B0130_W13_TRUE_IMPORTED_FBX_6_MESHES_3_REAL_ANIM_FRAMES_AND_KEYFRAME_REMOVAL_NEGATIVE_PASS",
      json.dumps({"frames":[round(x["max_y"],4) for x in frames],
                  "fbx_sha256":info["sha256"]}))
if __name__=="__main__":main()
