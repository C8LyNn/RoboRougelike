#!/usr/bin/env python3
"""B01-30 P15: genuinely IMPORT existing M04 producer's generated animated GLB.
Three orthographic projections at rest and frame 31; not Unity, not a model fix.
"""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

def args():
    a=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    return Path(a[a.index("--glb")+1]).resolve(),Path(a[a.index("--out")+1]).resolve()

def coords(obj):
    return [obj.matrix_world @ Vector(x) for x in obj.bound_box]

def envelope(objects):
    points=[p for obj in objects for p in coords(obj)]
    return [[round(min(p[i] for p in points),6) for i in range(3)],
            [round(max(p[i] for p in points),6) for i in range(3)]]

def camera_setup(location,target):
    scene=bpy.context.scene
    camera=bpy.data.objects.get("B01_30_ORTHO_CAMERA")
    if camera is None:
        data=bpy.data.cameras.new("B01_30_ORTHO_CAMERA")
        camera=bpy.data.objects.new("B01_30_ORTHO_CAMERA",data)
        scene.collection.objects.link(camera)
    camera.location=location
    direction=Vector(target)-camera.location
    assert direction.length>.1
    camera.rotation_euler=direction.to_track_quat("-Z","Y").to_euler()
    camera.data.type="ORTHO"
    camera.data.ortho_scale=2.7
    scene.camera=camera

def main():
    glb,out=args()
    assert glb.is_file() and glb.stat().st_size>100_000
    out.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.gltf(filepath=str(glb))
    meshes=[x for x in bpy.data.objects if x.type=="MESH"]
    pivots=sorted([x for x in bpy.data.objects if "GIMBAL_PIVOT" in x.name],key=lambda x:x.name)
    mats=sorted({m.name for obj in meshes for m in obj.data.materials if m})
    assert len(meshes)>=75,(len(meshes),"GLB import missing meshes")
    assert len(pivots)==4,(len(pivots),"GLB import missing animation pivot nodes")
    assert len(mats)>=8,(len(mats),"GLB lost material slots")
    imported_action_map={}
    for pivot in pivots:
        anim=pivot.animation_data
        imported_action_map[pivot.name]={
            "active_action":anim.action.name if anim and anim.action else None,
            "nla_tracks":[{
                "name":t.name,"mute":t.mute,
                "strips":[{"name":q.name,"action":q.action.name if q.action else None,
                           "mute":q.mute} for q in t.strips]
            } for t in anim.nla_tracks] if anim else []
        }
    combine_four="--combine-clips" in sys.argv
    combined_assigned={}
    if combine_four:
        # glTF exporter put each gimbal in an independent animation action.
        # Blender importer leaves all but the first action inactive. Activate
        # EACH actual original action in a disposable lookdev scene, never the
        # producer .blend and never any Unity/gameplay files.
        for pivot in pivots:
            found=bpy.data.actions.get(f"{pivot.name}Action_{pivot.name}")
            assert found is not None,(pivot.name,"NO_EXPORTED_GIMBAL_ACTION")
            animation=pivot.animation_data_create()
            animation.action=found
            for track in animation.nla_tracks:
                track.mute=True
            combined_assigned[pivot.name]=found.name
        assert len(combined_assigned)==4
    print("B01_30_M04_ACTUAL_IMPORTED_ACTION_INVENTORY",json.dumps({
        "actions":[a.name for a in bpy.data.actions],
        "pivots":imported_action_map},sort_keys=True))

    scene=bpy.context.scene
    scene.render.engine="BLENDER_WORKBENCH"
    scene.render.resolution_x=640
    scene.render.resolution_y=640
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format="PNG"
    scene.display.shading.light="STUDIO"
    scene.display.shading.color_type="MATERIAL"
    scene.display.shading.show_shadows=True
    scene.display.shading.show_cavity=True
    scene.display.shading.cavity_type="BOTH"
    scene.display.shading.background_type="WORLD"
    scene.world.color=(.058,.079,.105)
    scene.render.film_transparent=False
    scene.view_settings.view_transform="Standard"
    try:scene.view_settings.look="Medium High Contrast"
    except Exception:pass
    scene.camera=None
    views={
       "bottom":((0,-.02,-4.4),(0,0,-.42)),
       "side":((4.6,-.3,-.25),(0,0,-.45)),
       "three_quarter":((3.2,-3.8,-2.5),(0,0,-.38)),
    }
    diagnostics={}
    for frame in (1,31):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        pose={}
        for pivot in pivots:
            euler=pivot.matrix_world.to_euler()
            pose[pivot.name]=[round(float(math.degrees(x)),3) for x in euler]
        diagnostics[str(frame)]={"bounds":envelope(meshes),"pivot_world_euler_degrees":pose}
        for label,(loc,targ) in views.items():
            camera_setup(loc,targ)
            path=out/f"M04-{label}-frame{frame:03}.png"
            scene.render.filepath=str(path)
            bpy.ops.render.render(write_still=True)
            assert path.is_file() and path.stat().st_size>9000,(path,"blank render?")
    moving=[pivot.name for pivot in pivots if
        diagnostics["1"]["pivot_world_euler_degrees"][pivot.name]!=
        diagnostics["31"]["pivot_world_euler_degrees"][pivot.name]]
    changed=bool(moving)
    assert changed,("GLB imported clips have no visible pose change - fail closed",diagnostics)
    if combine_four:
        assert len(moving)==4,("FOUR_INDEPENDENT_GIMBALS_NOT_ACTIVATED",moving,diagnostics)
        for pivot in pivots:
            rest=diagnostics["1"]["pivot_world_euler_degrees"][pivot.name]
            active=diagnostics["31"]["pivot_world_euler_degrees"][pivot.name]
            axis=0 if pivot.name in ("N01_GIMBAL_PIVOT","N04_GIMBAL_PIVOT") else 1
            angle=abs(active[axis]-rest[axis])
            assert 2.0<angle<15.0,(pivot.name,"INCORRECT_IMPORTED_GIMBAL_AXIS_OR_STROKE",angle)
    # Four exported glTF animations are independent clips; the default Blender
    # active timeline may select just one. Name the ACTUAL moving pivot rather
    # than imply this preview proves four simultaneous gimbal movements.
    idle=[pivot.name for pivot in pivots if pivot.name not in moving]
    report={
       "source_model":"M04-024",
       "source_head":"eef02864b106546b200011a2f0286fe7d9a83a1f",
       "source_generator_blob":"102b2d430260ae4da15fc7dee51f9bd86ccd822c",
       "original_export_binary_glb_bytes":glb.stat().st_size,
       "actual_blender_version":bpy.app.version_string,
       "actual_imported_meshes":len(meshes),
       "actual_imported_materials":len(mats),
       "actual_imported_pivots":len(pivots),
       "actual_imported_actions":[a.name for a in bpy.data.actions],
       "actual_imported_action_map":imported_action_map,
       "real_pose_change_in_imported_animation":changed,
       "default_import_moving_pivots":moving,
       "default_import_idle_pivots":idle,
       "active_clip_mode":"FOUR_EXPORTED_ACTIONS_EXPLICITLY_APPLIED" if combine_four else "ORIGINAL_DEFAULT_IMPORT",
       "combined_action_assignments":combined_assigned,
       "default_import_preview_scope":"EXPLICIT_4_CLIP_COMBINATION_BLENDER_ONLY" if combine_four else "SAMPLED_ACTIVE_CLIP_ONLY_NOT_FOUR_SIMULTANEOUS_GIMBALS",
       "four_gimbals_simultaneous_in_game":"NOT_VERIFIED",
       "poses":diagnostics,
       "views":list(views),
       "rendered_png_count":6,
       "four_animated_pivots_in_actual_blender_import":bool(combine_four and len(moving)==4),
       "status":"SOURCE_GLB_IMPORT_AND_ORTHO_RENDER_PASS",
       "not_run":["Unity","PhysX","URP","native 3D module integration","collision narrowphase"]
    }
    (out/"M04-original-glb-lookdev.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print("B01_30_M04_ORIGINAL_GLB_IMPORT_RENDER_PASS",json.dumps(report,sort_keys=True))
if __name__=="__main__":main()
