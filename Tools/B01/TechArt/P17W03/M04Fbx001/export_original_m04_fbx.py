#!/usr/bin/env python3
"""B01-30 M04-FBX-RECEIVER-001: real FBX export from ORIGINAL M04-024 blend.

No new mesh, deformation, action keyframes or MODEL_JOB. Evaluates existing
four independent original pivot actions at the SAME time into one FBX take.
Blender real import QA is performed separately in receive_fbx.py.
"""
import bpy
import hashlib
import json
import math
import sys
from pathlib import Path

MODEL_SOURCE_SHA="eef02864b106546b200011a2f0286fe7d9a83a1f"
MODEL_GENERATOR_GIT_BLOB="102b2d430260ae4da15fc7dee51f9bd86ccd822c"
PIVOT_NAMES=("N01_GIMBAL_PIVOT","N02_GIMBAL_PIVOT","N03_GIMBAL_PIVOT","N04_GIMBAL_PIVOT")

def arg(name):
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    assert name in argv,("MISSING_ARG",name)
    return Path(argv[argv.index(name)+1]).resolve()

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def snapshot(pivots,frame):
    scene=bpy.context.scene
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    return {o.name:([round(math.degrees(v),5) for v in o.rotation_euler],
                    [round(float(v),6) for v in o.matrix_world.translation]) for o in pivots}

def main():
    output=arg("--out")
    input_glb=arg("--original-glb")
    source_blend=Path(bpy.data.filepath)
    assert source_blend.is_file() and source_blend.name=="M04-024.blend",source_blend
    assert input_glb.is_file() and input_glb.stat().st_size>100_000
    assert bpy.context.scene.render.fps==30
    meshes=[o for o in bpy.data.objects if o.type=="MESH"]
    materials={m.name for o in meshes for m in o.data.materials if m}
    assert len(meshes)>=75 and len(materials)==8,(len(meshes),len(materials))
    roots=[o for o in bpy.data.objects if o.name=="ROOT_UNDERSIDE_MOUNT_Z0"]
    assert len(roots)==1
    assert tuple(round(float(x),5) for x in roots[0].location)==(0.,0.,0.)
    pivots=[]
    for name in PIVOT_NAMES:
        p=bpy.data.objects.get(name)
        assert p and p.type=="EMPTY",(name,"PIVOT_NOT_FOUND")
        assert p.animation_data and p.animation_data.action,(name,"NO_ORIGINAL_ACTIVE_ACTION")
        pivots.append(p)
    before={p.name:(p.animation_data.action.name,len(p.animation_data.action.fcurves)) for p in pivots}
    assert all(x[1]>=1 for x in before.values()),before
    rest=snapshot(pivots,1)
    tilted=snapshot(pivots,31)
    for p in pivots:
        a=rest[p.name][0]
        c=tilted[p.name][0]
        assert 10.<max(abs(x-y) for x,y in zip(a,c))<13.0,(p.name,a,c)
    original_mesh_names=sorted(o.name for o in meshes)
    original_mesh_verts=sum(len(o.data.vertices) for o in meshes)
    original_mesh_polys=sum(len(o.data.polygons) for o in meshes)
    output.mkdir(parents=True,exist_ok=True)
    target=output/"M04-024-4GIMBALS-ORIGINAL.fbx"
    bpy.context.scene.frame_set(1)
    # All four ORIGINAL active actions are evaluated in the same FBX take,
    # with original objects and meshes unchanged. Do NOT generate a fifth
    # action or bake/convert geometry into a new model version.
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(target),
        use_selection=False,
        object_types={"EMPTY","MESH"},
        use_mesh_modifiers=False,
        mesh_smooth_type="FACE",
        use_space_transform=True,
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        bake_anim=True,
        bake_anim_use_all_bones=False,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        path_mode="AUTO",
        add_leaf_bones=False)
    assert target.is_file() and target.stat().st_size>200_000
    raw=target.read_bytes()
    assert raw.startswith(b"Kaydara FBX Binary  \x00\x1a\x00"),"FBX_NOT_BINARY"
    # Verify original scene still intact after FBX export.
    assert sorted(o.name for o in bpy.data.objects if o.type=="MESH")==original_mesh_names
    assert all(p.animation_data.action.name==before[p.name][0] for p in pivots)
    report={
        "id":"B01-30-M04-FBX-RECEIVER-001","version":"R1",
        "status":"REAL_ORIGINAL_BLENDER_FBX_EXPORTED_NO_MODEL_MODIFICATION",
        "model":"M04-024",
        "source_original_owner_commit":MODEL_SOURCE_SHA,
        "source_original_generator_git_blob":MODEL_GENERATOR_GIT_BLOB,
        "source_original_blend_sha256":sha(source_blend),
        "source_original_glb_sha256":sha(input_glb),
        "fbx_name":target.name,
        "fbx_bytes":target.stat().st_size,
        "fbx_sha256":sha(target),
        "blender_version":bpy.app.version_string,
        "export_axis_forward":"-Z",
        "export_axis_up":"Y",
        "export_scale":"1 blender unit = 1 metre (candidate Unity import)",
        "original_mesh_count":len(meshes),
        "original_vertex_count":original_mesh_verts,
        "original_polygon_count":original_mesh_polys,
        "original_material_count":len(materials),
        "original_gimbal_actions":before,
        "original_frame1_local_euler_and_world_location":rest,
        "original_frame31_local_euler_and_world_location":tilted,
        "source_blend_and_glb_unchanged":True,
        "not_run":["Unity FBX importer","Unity URP material parity",
                   "Unity gimbal Animator controller","Unity transform axis/scale G1",
                   "PhysX","Codex integration"]
    }
    (output/"m04-fbx-source-receipt.json").write_text(json.dumps(report,indent=2)+"\n")
    print("B01_30_M04_4GIMBAL_REAL_FBX_EXPORT_PASS",
          json.dumps({"FBX_bytes":report["fbx_bytes"],"FBX_sha256":report["fbx_sha256"],
                      "source_meshes":len(meshes),"original_animated_pivots":len(pivots)},sort_keys=True))

if __name__=="__main__":main()
