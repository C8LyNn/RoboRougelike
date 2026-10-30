#!/usr/bin/env python3
"""Real Blender CHL003 bridge clearance diagrams from immutable B01-20 source.

Visualization of two real CHL demo vehicle geometries at 4 yaw poses. The
mesh is physical 3D Blender art, not a Unity Physics/PlayMode/Collider test.
Source is exact B01-20 original CHL003DemoBootstrap.cs Git blob.
"""
from __future__ import annotations
import bpy
import hashlib
import json
import math
import re
import struct
import sys
from pathlib import Path
from mathutils import Vector

SOURCE_SHA="c7b2a234a286089383503da41883d624fe650551"
SOURCE_GIT_BLOB="9308ceb9eab89d1ab483b560ff3ed58fa6d2d32e"
PACKAGE="B01-30-CHL003-BRIDGE-YAW-3D-001/R1"
CASES=(("HEAVY",0.0,True),("HEAVY",8.0,True),("HEAVY",12.0,False),("LIGHT",30.0,True))
def arg(s):
    a=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    if s not in a:raise ValueError("MISSING_ARG "+s)
    return Path(a[a.index(s)+1]).resolve()
def fail(s):raise ValueError("B0130_W11_BRIDGE_SOURCE_REJECT:"+s)
def gitblob(b):return hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def contract(p):
    raw=p.read_bytes()
    if gitblob(raw)!=SOURCE_GIT_BLOB:fail("B01_20_ORIGINAL_GIT_BLOB_CHANGED")
    t=raw.decode()
    clauses=[
      'Box("NARROW_SOLID_BRIDGE", new Vector3(0f, 1.38f, 24.5f),',
      'new Vector3(3.70f, .60f, 10f)',
      'new Vector3(2.5f, .65f, 3.4f) :',
      'new Vector3(1.45f, .52f, 2.3f)',
      'float width = heavy ? 1.0f : .59f;',
      'new Vector3(.82f, .82f, .82f)',
      'end * (heavy ? 1.15f : .75f)',
      'new Vector3(3.70f, 4.5f, .16f), trigger: true',
      'side * 1.97f',
    ]
    for i,c in enumerate(clauses):
        if c not in t:fail("CHL_SOURCE_CONTRACT_MARKER_"+str(i))
    return {
      "bridge_width_m":3.7,"bridge_length_m":10.0,"bridge_height_m":.60,
      "minimum_side_clearance_m":.25,
      "wheel_radius_m":.41,
      "HEAVY":{"chassis_x":2.5,"chassis_z":3.4,"chassis_height":.65,"wheel_x":1.0,"wheel_z":1.15},
      "LIGHT":{"chassis_x":1.45,"chassis_z":2.3,"chassis_height":.52,"wheel_x":.59,"wheel_z":.75}
    }

def material(name,color):
    mat=bpy.data.materials.new(name)
    mat.diffuse_color=tuple(color)
    mat.use_nodes=True
    mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value=tuple(color)
    mat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value=.5
    return mat

def cube(name,xyz,size,mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=xyz)
    obj=bpy.context.object
    obj.name=name
    obj.dimensions=size
    obj.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return obj

def actual_width(objs):
    points=[ob.matrix_world@v.co for ob in objs for v in ob.data.vertices]
    return max(p.x for p in points)-min(p.x for p in points)

def analytic_width(spec,angle,radius):
    a=math.radians(angle)
    chassis=abs(spec["chassis_x"]*math.cos(a))+abs(spec["chassis_z"]*math.sin(a))
    wheel=2*(abs(spec["wheel_x"]*math.cos(a))+abs(spec["wheel_z"]*math.sin(a))+radius)
    return max(chassis,wheel)

def make(rig,yaw,should_pass,model,out):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    deck=material("BRIDGE_DECK_VISUAL_ONLY",(.48,.52,.58,1))
    metal=material("HULL_VISUAL_ONLY",(.12,.36,.67,1))
    tire=material("FOUR_SPHERE_WHEELS_VISUAL_ONLY",(.10,.11,.14,1))
    amber=material("CLEARANCE_EDGE_AMBER_NOT_COLLIDER",(.98,.72,.11,1))
    status=material("QA_PASS_SIDE_CLEARANCE" if should_pass else "QA_FAIL_SIDE_CLEARANCE",
                    (.15,.84,.48,1) if should_pass else (.96,.12,.15,1))
    bridge_width=model["bridge_width_m"]
    cube("CHL003_SOURCE_BRIDGE_DECK",(0,0,0),(.0+bridge_width,model["bridge_length_m"],model["bridge_height_m"]),deck)
    # Actual bridge top at +0.30 m. Root local height 1.23 m ensures
    # wheel bottom = 1.23-.52-.41=.30 m: touching visually, not PhysX proof.
    rootz=.30+.52+model["wheel_radius_m"]
    vehicle=model[rig]
    yaw_r=math.radians(yaw)
    carrier=bpy.data.objects.new("RIG_PIVOT_YAW_VISUAL_ONLY",None)
    bpy.context.collection.objects.link(carrier)
    carrier.location=(0,0,rootz)
    carrier.rotation_euler[2]=yaw_r
    body=cube("CHL003_"+rig+"_ACTUAL_SOURCE_CHASSIS",(0,0,rootz-.12),
              (vehicle["chassis_x"],vehicle["chassis_z"],vehicle["chassis_height"]),metal)
    body.parent=carrier
    body.matrix_parent_inverse=carrier.matrix_world.inverted()
    wheels=[]
    for sx in (-1,1):
        for sy in (-1,1):
            name="CHL003_"+rig+"_WHEEL_"+str(sx)+"_"+str(sy)
            # Make a true sphere of source diameter .82m, 32×16 face segments.
            bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=16,radius=model["wheel_radius_m"],
                 location=(sx*vehicle["wheel_x"],sy*vehicle["wheel_z"],rootz-.52))
            ob=bpy.context.object
            ob.name=name
            ob.data.materials.append(tire)
            ob.parent=carrier
            ob.matrix_parent_inverse=carrier.matrix_world.inverted()
            wheels.append(ob)
    bpy.context.view_layer.update()
    mesh_vehicle=[body]+wheels
    measured=actual_width(mesh_vehicle)
    analytic=analytic_width(vehicle,yaw,model["wheel_radius_m"])
    if abs(measured-analytic)>.015:fail("ACTUAL_MESH_ENVELOPE_DEVIATES_FROM_SOURCE_GEOMETRY:"+str(measured)+"/"+str(analytic))
    clearance=(bridge_width-analytic)/2
    verdict=clearance+.00000001>=model["minimum_side_clearance_m"]
    if verdict!=should_pass:fail("CLEARANCE_REGRESSION:"+rig+"/"+str(yaw)+"/"+str(clearance))
    for sign in (-1,1):
        cube("QA_BRIDGE_VISIBLE_EDGE_"+str(sign),
          (sign*bridge_width/2,0,.35),(.055,model["bridge_length_m"],.08),amber)
        cube("QA_CLEARANCE_RULE_MARKER_"+str(sign),
          (sign*(bridge_width/2-model["minimum_side_clearance_m"]),0,.365),
          (.035,model["bridge_length_m"],.06),status)
    # Mutate actual visual geometry by +20° yaw, test computed mesh extents change.
    original=carrier.rotation_euler[2]
    try:
        carrier.rotation_euler[2]=original+math.radians(20)
        bpy.context.view_layer.update()
        if abs(actual_width(mesh_vehicle)-measured)<.04:fail("ACTUAL_MESH_YAW_MUTANT_NOT_DETECTED")
    finally:
        carrier.rotation_euler[2]=original
        bpy.context.view_layer.update()
    basename="CHL003-"+rig+"-YAW"+str(int(yaw))+"-"+("PASS" if verdict else "FAIL")
    fbx=out/(basename+".fbx")
    bpy.ops.object.select_all(action="DESELECT")
    for ob in bpy.data.objects:
        if ob.type=="MESH":ob.select_set(True)
    bpy.context.view_layer.objects.active=body
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,axis_forward="-Z",axis_up="Y",bake_anim=False)
    if not fbx.read_bytes().startswith(b"Kaydara FBX Binary  \x00\x1a\x00") or fbx.stat().st_size<15000:
        fail("ACTUAL_FBX_NOT_BINARY")
    # Actually render original Blender scene, no stock images.
    bpy.ops.object.camera_add()
    camera=bpy.context.object
    camera.data.type="ORTHO";camera.data.ortho_scale=10.
    camera.location=(7,-12,9)
    camera.rotation_euler=(Vector((0,0,.6))-camera.location).to_track_quat("-Z","Y").to_euler()
    scene=bpy.context.scene;scene.camera=camera
    scene.render.engine="BLENDER_WORKBENCH"
    scene.display.shading.light="STUDIO"
    scene.display.shading.color_type="MATERIAL"
    scene.display.shading.show_cavity=True
    scene.render.resolution_x=960;scene.render.resolution_y=720
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format="PNG"
    png=out/(basename+".png");scene.render.filepath=str(png)
    bpy.ops.render.render(write_still=True)
    raw=png.read_bytes()
    if raw[:8]!=b"\x89PNG\r\n\x1a\n" or tuple(struct.unpack(">II",raw[16:24]))!=(960,720) or len(raw)<12000:
        fail("ACTUAL_ORIGINAL_BLENDER_RENDER_EMPTY")
    blend=out/(basename+".blend")
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    # All 4 spheres and body must be original mesh geometry; bridge/markers visual.
    return {
      "rig":rig,"yaw_degrees":yaw,"source_bridge_width_m":bridge_width,
      "source_minimum_side_clearance_m":model["minimum_side_clearance_m"],
      "source_vehicle_chassis_width_m":vehicle["chassis_x"],"source_vehicle_chassis_length_m":vehicle["chassis_z"],
      "actual_source_wheel_diameter_m":2*model["wheel_radius_m"],
      "analytic_wheel_inclusive_body_width_m":analytic,
      "actual_blender_mesh_bounding_width_m":measured,
      "conservative_each_side_clearance_m":clearance,
      "source_policy_pass":verdict,"vehicle_meshes":len(mesh_vehicle),
      "negative_actual_yaw_mesh_mutation_rejected":True,
      "files":{k:{"name":p.name,"bytes":p.stat().st_size,"sha256":sha(p)}
               for k,p in (("fbx",fbx),("blend",blend),("png",png))}
    }

def main():
    src=arg("--original");out=arg("--out")
    out.mkdir(parents=True,exist_ok=True)
    model=contract(src)
    damaged=src.read_bytes()+b" "
    try:contract_bytes_check(damaged)
    except ValueError:pass
    else:fail("NEGATIVE_IMMUTABLE_SOURCE_MUTANT_NOT_REJECTED")
    rows=[make(rig,yaw,passing,model,out) for rig,yaw,passing in CASES]
    if len(rows)!=4 or sum(r["vehicle_meshes"] for r in rows)!=20 or sum(r["source_policy_pass"] for r in rows)!=3:fail("RECEIPT_CASES")
    receipt={"package_id":PACKAGE,"source_owner":"B01-20 CHL003","original_pr":285,
       "original_head":SOURCE_SHA,"original_unmodified_git_blob":SOURCE_GIT_BLOB,
       "real_FBX":4,"real_BLEND":4,"real_PNG":4,
       "source_mutation_rejected":True,"actual_mesh_yaw_mutations_rejected":4,
       "source_contract_read_only":True,"pose_results":rows,
       "status":"REAL_4_CHL003_BRIDGE_YAW_3D_VISUALS_PRODUCER_PASS",
       "native_Unity_PhysX":"NOT_RUN","independent_B01_40_60":"PENDING","codex_ack":"NONE",
       "limitations":["All asset meshes are Blender visuals; not collision hulls",
         "Yaw tests measure static 3D geometry widths, not actual motor traversals",
         "Original CHL author #285/owner B01-20 remains sole source owner"]}
    (out/"original-chl003-bridge-yaw-3d-receipt.json").write_text(json.dumps(receipt,indent=2)+"\n")
    print("B0130_W11_ORIGINAL_CHL_4_REAL_FBX_4_BLEND_4_PNG_3_PASS_1_FAIL_4_YAW_MUTANTS_PASS",
          json.dumps({"poses":len(rows),"full_vehicle_meshes":20,"source_sha":SOURCE_SHA}))
def contract_bytes_check(data):
    if gitblob(data)!=SOURCE_GIT_BLOB:fail("IMMUTABLE_SOURCE_GIT_BLOB_CHANGED")
if __name__=="__main__":main()
