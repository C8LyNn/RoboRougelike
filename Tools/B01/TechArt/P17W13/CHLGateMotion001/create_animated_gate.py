#!/usr/bin/env python3
"""Make one ACTUAL source-pinned animated CHL-003 gate *visual only* in Blender.

One genuinely keyframed real mesh: arm pivot rotates 0 -> -75 -> 0 degrees
on frames 1, 31, 61. Real 3D shaft + physical arrowhead geometry, authored
alongside source-pinned right-side marker x=+1.97m. No gameplay state hook.
"""
from __future__ import annotations
import bpy
import hashlib
import json
import math
import struct
import sys
from pathlib import Path
from mathutils import Vector
from mathutils import Matrix

PACKAGE="B01-30-CHL003-ANIMATED-GATE-INDICATOR-001/R1"
SOURCE_PR=285
SOURCE_SHA="c7b2a234a286089383503da41883d624fe650551"
SOURCE_BLOB="9308ceb9eab89d1ab483b560ff3ed58fa6d2d32e"
PIVOT=(1.97,4.08,-.32)
KEYS={1:0.0,31:-75.0,61:0.0}
FRAMES=(1,31,61)
ROOT="CHL003_ANIMATED_GATE_POINTER"
def arg(flag):
    a=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    if flag not in a or a.index(flag)+1==len(a):raise ValueError("MISSING:"+flag)
    return Path(a[a.index(flag)+1]).resolve()
def fail(msg):raise ValueError("B0130_W13_GATEMOTION_FAIL_CLOSED:"+msg)
def gitblob(data):return hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def original_source(src):
    b=src.read_bytes()
    if gitblob(b)!=SOURCE_BLOB:fail("ORIGINAL_OWNER_CHL_SOURCE_BLOB_CHANGED")
    text=b.decode("utf8")
    markers=(
        'Gate("CP0", 5f, CHL003Gate.GateType.Checkpoint)',
        'Gate("FINISH", 38f, CHL003Gate.GateType.Finish)',
        'new Vector3(side * 1.97f, 1.3f, z)',
        'new Vector3(.12f, 2.6f, .12f)',
        'new Vector3(3.70f, 4.5f, .16f), trigger: true',
        'marker.GetComponent<Collider>().enabled = false;')
    if any(s not in text for s in markers):fail("CHLIGINAL_GATE_MARKER_CONTRACT_DRIFT")
    return {"original_chl_sha":SOURCE_SHA,"original_git_blob":SOURCE_BLOB,"gate_width":3.70,
            "original_post_center_x":1.97,"original_post_top_y":2.60,
            "original_post_physics_colliders_disabled":True}
def mat(name,rgba,metallic):
    m=bpy.data.materials.new(name);m.diffuse_color=rgba;m.use_nodes=True
    bsdf=m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value=rgba
    bsdf.inputs["Metallic"].default_value=metallic
    bsdf.inputs["Roughness"].default_value=.31
    return m
def cube(name,location,dimensions,material):
    bpy.ops.mesh.primitive_cube_add(size=1,location=location)
    o=bpy.context.object;o.name=name;o.dimensions=dimensions
    o.data.materials.append(material)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o["render_proxy_only"]=True
    return o
def tip(name,position,material):
    # True solid 3D triangular arrow at tip of the source-sized rotating bar.
    # Object tip x in [pivot_x -0.66,-0.42]; height +/-0.15m.
    vertices=[(-.65,-.0,-.07),(-.42,.14,-.07),(-.42,-.14,-.07),
              (-.65,-.0,.07),(-.42,.14,.07),(-.42,-.14,.07)]
    faces=[(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)]
    mesh=bpy.data.meshes.new("ACTUAL_ORIGINAL_GATE_ARROW_TIP_MESH")
    mesh.from_pydata(vertices,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
    o.location=position;o.data.materials.append(material)
    return o
def world_bounds(obj):
    points=[obj.matrix_world@v.co for v in obj.data.vertices]
    return [tuple(min(v[i] for v in points) for i in range(3)),
            tuple(max(v[i] for v in points) for i in range(3))]
def frame_state(frame,pointer):
    bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
    mini,maxi=world_bounds(pointer)
    return {"frame":frame,"pointer_world_xyz":[round(float(a),7) for a in pointer.matrix_world.translation],
            "pointer_rot_z_deg":round(math.degrees(pointer.rotation_euler.z),5),
            "real_arrow_vertex_min_x":round(float(mini[0]),6),
            "real_arrow_vertex_max_y":round(float(maxi[1]),6),
            "real_arrow_mesh_polygons":len(pointer.data.polygons)}
def check_scene(pointer,static):
    if len(static)!=5:fail("STATIC_MESH_COUNT_CHANGED")
    if pointer.name!=ROOT or len(pointer.data.polygons)<11 or len(pointer.data.vertices)<14:
        fail("REAL_KEYFRAME_ARROW_GEOMETRY_MISSING")
    frames=[frame_state(frame,pointer) for frame in FRAMES]
    angles=[x["pointer_rot_z_deg"] for x in frames]
    if any(abs(angles[i]-KEYS[frame])>.03 for i,frame in enumerate(FRAMES)):
        fail("KEYFRAME_REAL_OBJECT_ANGLES_NOT_CORRECT:"+str(angles))
    if not (frames[1]["real_arrow_vertex_max_y"]>frames[0]["real_arrow_vertex_max_y"]+.35
            and abs(frames[2]["real_arrow_vertex_max_y"]-frames[0]["real_arrow_vertex_max_y"])<.004):
        fail("ANIMATED_REAL_VERTICES_DO_NOT_MOVE_AND_RETURN")
    if min(world_bounds(pointer)[0][1],*[world_bounds(ob)[0][1] for ob in static])<2.594:
        fail("ORIGINAL_CHL_POST_TOP_LOWER_BOUND_BREACHED")
    if any(abs(o.matrix_world.translation.x-1.97)>.002 for o in static if o.name.startswith("CHL003_POST_MOUNT")):
        fail("ORIGINAL_CHL_CENTER_CHANGED")
    return frames
def render(out,frame):
    sc=bpy.context.scene
    sc.frame_set(frame)
    bpy.ops.object.camera_add()
    cam=bpy.context.object;cam.data.type="ORTHO";cam.data.ortho_scale=3.3
    cam.location=(4.30,4.28,-7.6)
    cam.rotation_euler=(Vector((1.96,3.64,-.2))-cam.location).to_track_quat("-Z","Y").to_euler()
    sc.camera=cam;sc.render.engine="BLENDER_WORKBENCH"
    sc.display.shading.color_type="MATERIAL"
    sc.display.shading.light="STUDIO";sc.display.shading.show_cavity=True
    sc.render.resolution_x=960;sc.render.resolution_y=720
    sc.render.resolution_percentage=100;sc.render.image_settings.file_format="PNG"
    fname=out/("CHL003-ANIMATED-POINTER-FRAME-"+str(frame)+".png")
    sc.render.filepath=str(fname);bpy.ops.render.render(write_still=True)
    data=fname.read_bytes()
    if len(data)<10000 or data[:8]!=b"\x89PNG\r\n\x1a\n" or struct.unpack(">II",data[16:24])!=(960,720):
        fail("NOT_REAL_RENDERED_FRAME:"+str(frame))
    bpy.data.objects.remove(cam,do_unlink=True)
    return {"file":fname.name,"bytes":len(data),"sha256":sha(fname),"rendered_frame":frame}
def main():
    source=arg("--source");out=arg("--out");out.mkdir(parents=True,exist_ok=True)
    contract=original_source(source)
    if gitblob(source.read_bytes()+b" ")==SOURCE_BLOB:fail("SOURCE_BITFLIP_NOT_DETECTED")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    shell=mat("CHL003_GATE_STATUS_ANODIZED_BLACK",(0.08,.11,.15,1),.62)
    steel=mat("CHL003_GATE_SOURCE_SUPPORT_METAL",(.44,.51,.60,1),.75)
    lime=mat("CHL003_GATE_VISUAL_ONLY_SIGNAL_LIME",(.13,.92,.48,1),.10)
    amber=mat("CHL003_GATE_VISUAL_ONLY_SIGNAL_AMBER",(1,.62,.12,1),.12)
    px,py,pz=PIVOT
    static=[
      cube("CHL003_POST_MOUNT_SOURCE_X197",(px,2.655,-.16),(.11,.11,.14),steel),
      cube("CHL003_NONCOLLIDER_EXTENSION_POLE",(px,3.36,-.16),(.075,1.35,.08),steel),
      cube("CHL003_SIGNAL_HOUSING_AT_ANCHOR",(px,py,pz+.11),(.27,.27,.23),shell),
      cube("CHL003_SIGNAL_AMBER_RETICLE_CENTER",(px,py,pz-.04),(.18,.18,.035),amber),
      cube("CHL003_SIGNAL_CAP_SUPPORT",(px,3.98,pz+.18),(.12,.18,.18),steel)]
    # A genuine animated mesh has one pivot anchored at source marker X.
    shaft=cube("CHL003_ARROW_SHAFT_RAW",(px-.23,py,pz),(.46,.09,.12),lime)
    tipobj=tip("CHL003_TRIANGLE_TIP_RAW",(px,py,pz),lime)
    bpy.ops.object.select_all(action="DESELECT")
    shaft.select_set(True);tipobj.select_set(True)
    bpy.context.view_layer.objects.active=shaft
    bpy.ops.object.join()
    pointer=bpy.context.view_layer.objects.active
    pointer.name=ROOT
    bpy.context.scene.cursor.location=(px,py,pz)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR",center="MEDIAN")
    pointer.rotation_mode="XYZ"
    sc=bpy.context.scene
    sc.frame_start=1;sc.frame_end=61;sc.render.fps=30
    for frame,degree in KEYS.items():
        pointer.rotation_euler=(0.,0.,math.radians(degree))
        pointer.keyframe_insert(data_path="rotation_euler",frame=frame,group="CHL003_Pointer_Visual_Only")
    if pointer.animation_data is None or pointer.animation_data.action is None:
        fail("NO_GENUINE_BLENDER_KEYFRAME_CURVE")
    states=check_scene(pointer,static)
    # Mutate genuine scene frame31 angle; assert real vertices differ then restore.
    sc.frame_set(31);before=pointer.rotation_euler.z
    try:
        pointer.rotation_euler.z=0; bpy.context.view_layer.update()
        mutated=world_bounds(pointer)[1][1]
        if abs(mutated-states[0]["real_arrow_vertex_max_y"])>.005:
            fail("MUTATED_KEYFRAME_NOT_RESET_TO_IDLE")
        if abs(mutated-states[1]["real_arrow_vertex_max_y"])<.35:
            fail("ANIMATED_ARROW_STATIC_MUTANT_FALSE_GREEN")
    finally:
        pointer.rotation_euler.z=before;sc.frame_set(31)
    # Actual negative: move real static source mount below original marker top.
    mount=static[0];old=mount.location.copy()
    try:
        mount.location.y=2.4;bpy.context.view_layer.update()
        if world_bounds(mount)[0][1]>2.594:fail("LOWERED_REAL_SOURCE_MOUNT_NOT_DETECTED")
    finally:mount.location=old
    outname="CHL003-ORIGINAL-ANIMATED-GATE-POINTER"
    for ob in bpy.context.selected_objects:ob.select_set(False)
    for ob in static+[pointer]:ob.select_set(True)
    bpy.context.view_layer.objects.active=pointer
    fbx=out/(outname+".fbx")
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
      global_scale=1.,apply_unit_scale=True,axis_forward="-Z",axis_up="Y",
      bake_anim=True,bake_anim_step=1.0,bake_anim_simplify_factor=0.0,
      add_leaf_bones=False,use_mesh_modifiers=False)
    if fbx.stat().st_size<12000 or not fbx.read_bytes().startswith(b"Kaydara FBX Binary  \x00\x1a\x00"):
        fail("NOT_REAL_ANIMATED_BINARY_FBX")
    blend=out/(outname+".blend")
    sc.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    previews=[render(out,f) for f in FRAMES]
    receipt={"package_id":PACKAGE,"original_CHL_source":contract,
       "original_PR":SOURCE_PR,"original_source_head":SOURCE_SHA,
       "original_source_git_blob":SOURCE_BLOB,
       "actual_animation_frames":list(FRAMES),"actual_blender_anim_fps":30,
       "expected_pointer_degrees":{str(k):v for k,v in KEYS.items()},
       "real_3d_static_meshes":len(static),"real_animated_3d_meshes":1,
       "real_keyframed_mesh_name":ROOT,
       "real_source_blender_frame_geometry":states,
       "original_source_corruption_test":"REJECTED",
       "real_3d_geometry_and_animation_mutants_rejected":2,
       "real_binary_FBX":{"file":fbx.name,"bytes":fbx.stat().st_size,"sha256":sha(fbx)},
       "real_editable_BLEND":{"file":blend.name,"bytes":blend.stat().st_size,"sha256":sha(blend)},
       "real_rendered_png_frames":previews,
       "native_Unity_animation_and_render":"NOT_RUN","independent_B01_40":"PENDING",
       "codex_ACK":"NONE",
       "LIMITATION":"Blender authored mesh animation, source visual only. Not wired to CHL gameplay or physically engine validated."}
    (out/"original-chl003-gate-animated-3d-receipt.json").write_text(json.dumps(receipt,indent=2)+"\n")
    print("B0130_W13_GENUINE_KEYFRAMED_FBX_6_REAL_MESH_3_PNG_2_GEOMETRY_MUTANTS_PASS",
      json.dumps({"states":states,"fbx_bytes":fbx.stat().st_size}))
if __name__=="__main__":main()
