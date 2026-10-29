#!/usr/bin/env python3
"""Independent REAL Blender re-import of emitted FBX (not trust exporter log).
Four original gimbal nodes must ALL move inside ONE re-imported animation take.
Visual image output renders actual re-imported geometry, never source BLEND.
"""
import bpy
import hashlib
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

PIVOTS=("N01_GIMBAL_PIVOT","N02_GIMBAL_PIVOT","N03_GIMBAL_PIVOT","N04_GIMBAL_PIVOT")

def arg(name):
    a=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    assert name in a
    return Path(a[a.index(name)+1]).resolve()
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def centre(o):return o.matrix_world.translation.copy()
def get_pose(pivots,frame):
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()
    return {p.name:{"quaternion":p.matrix_world.to_quaternion().copy(),
                    "location":centre(p)} for p in pivots}
def cam(name,loc,target):
    scene=bpy.context.scene
    obj=bpy.data.objects.get(name)
    if obj is None:
        datablock=bpy.data.cameras.new(name)
        obj=bpy.data.objects.new(name,datablock)
        scene.collection.objects.link(obj)
    obj.location=loc
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat("-Z","Y").to_euler()
    obj.data.type="ORTHO"
    obj.data.ortho_scale=2.8
    scene.camera=obj

def main():
    target=arg("--fbx")
    out=arg("--out")
    assert target.is_file() and target.stat().st_size>200_000
    source=json.loads(arg("--source-receipt").read_text())
    assert sha(target)==source["fbx_sha256"]
    out.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(target), use_anim=True)
    scene=bpy.context.scene
    scene.render.fps=30
    scene.frame_start=1
    scene.frame_end=121
    meshes=sorted([o for o in bpy.data.objects if o.type=="MESH"],key=lambda q:q.name)
    pivots=[]
    for name in PIVOTS:
        p=bpy.data.objects.get(name)
        assert p is not None,(name,"IMPORTED_PIVOT_MISSING")
        assert p.animation_data and p.animation_data.action,(name,"ANIMATION_NOT_IMPORTABLE")
        pivots.append(p)
    assert len(meshes)==source["original_mesh_count"],(
        "FBX_MESH_NODE_LOSS",len(meshes),source["original_mesh_count"])
    rest=get_pose(pivots,1)
    tilted=get_pose(pivots,31)
    reverse=get_pose(pivots,91)
    deltas={}
    for p in pivots:
        name=p.name
        forward=math.degrees(rest[name]["quaternion"].rotation_difference(tilted[name]["quaternion"]).angle)
        backward=math.degrees(rest[name]["quaternion"].rotation_difference(reverse[name]["quaternion"]).angle)
        assert 7.0<forward<15.0,(name,"ORIGINAL_TILT_MISSING_IN_REAL_FBX",forward)
        assert 7.0<backward<15.0,(name,"REVERSE_TILT_MISSING_IN_REAL_FBX",backward)
        travel=(rest[name]["location"]-tilted[name]["location"]).length
        assert travel<.001,(name,"PIVOT_UNEXPECTED_TRANSLATION",travel)
        deltas[name]={"frame1_to31_world_quaternion_angle_deg":round(forward,5),
                      "frame1_to91_world_quaternion_angle_deg":round(backward,5),
                      "pivot_translation_delta_m":round(travel,7),
                      "imported_active_action":p.animation_data.action.name}
    assert len(deltas)==4
    # Actual FBX mesh/render evidence at meaningful moving frames.
    scene.render.engine="BLENDER_WORKBENCH"
    scene.render.resolution_x=540
    scene.render.resolution_y=540
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format="PNG"
    scene.display.shading.color_type="MATERIAL"
    scene.display.shading.light="STUDIO"
    scene.display.shading.show_cavity=True
    scene.world.color=(.07,.09,.13)
    views={"underside":((0,-.1,-4.),(0,0,-.4)),
           "angled":((3.0,-3.3,-2.7),(0,0,-.4))}
    renders=[]
    for frame in (1,31):
        scene.frame_set(frame)
        for view,(location,look) in views.items():
            cam("B01_30_M04_OWN_RENDER_CAMERA",location,look)
            dest=out/("M04-FBX-"+view+"-frame"+str(frame)+".png")
            scene.render.filepath=str(dest)
            bpy.ops.render.render(write_still=True)
            assert dest.is_file() and dest.stat().st_size>9000,(str(dest),"EMPTY_RENDER")
            renders.append({"file":dest.name,"sha256":sha(dest),"bytes":dest.stat().st_size})
    report={"package_id":"B01-30-M04-FBX-RECEIVER-001","version":"R1",
            "result":"REAL_BLENDER_FBX_REIMPORT_AND_FOUR_PIVOT_MOTION_PASS",
            "source_fbx_sha256":sha(target),
            "actual_imported_mesh_count":len(meshes),
            "original_blend_mesh_count":source["original_mesh_count"],
            "four_real_imported_animated_pivots":len(deltas),
            "real_imported_animations":deltas,
            "visual_proof_png":renders,
            "preview_scope":"Blender FBX import and Workbench render, NOT Unity G1",
            "not_run":["Unity native FBX import","Unity Animator 4 gimbal simultaneous",
                       "actual URP shader/material look","PhysX","Codex integration"]}
    (out/"m04-real-fbx-reimport.json").write_text(json.dumps(report,indent=2)+"\n")
    print("B01_30_M04_ACTUAL_FBX_REIMPORT_4GIMBALS_PASS",
          json.dumps({"pivots":len(deltas),"meshes":len(meshes),
                      "real_FBX_rendered_PNG":len(renders)},sort_keys=True))
if __name__=="__main__":main()
