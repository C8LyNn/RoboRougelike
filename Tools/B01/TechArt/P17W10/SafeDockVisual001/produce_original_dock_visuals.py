#!/usr/bin/env python3
"""B01-30 W10: actual Blender 3D safe/blocked visual *diagnostics* only.

Geometry follows literal immutable B01-23 RigDemoDefinition.cs dimensions
and HardenedDockDemo.cs target pose and all-layer query. No claim of Unity
PhysX/Collider correctness, no runtime patch, and does not remediate #472/#476.
"""
from __future__ import annotations
import bpy
import hashlib
import json
import math
import re
import sys
from pathlib import Path
from mathutils import Vector

CATALOG_GIT_BLOB="b7d1a503c89537316fe48dbb54f5997bb21d14ce"
HARDEN_GIT_BLOB="d46c6341db199d010f48de00f98250becd2d7c87"
CATALOG_HEAD="cdcaea750fda1a9d7db6018e34970f4b9f206427"
HARDEN_HEAD="763748f1be0c61356fb4462144640bb359364226"
PACKAGE="B01-30-P17W10-SAFEDOCK-VISUAL-001/R1"
RIGS={"Light":(2307001,1.5),"Heavy":(2307002,12.0)}
TOL=0.005
def params(name):
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    if name not in argv or argv.index(name)+1>=len(argv):raise ValueError("MISSING_ARG:"+name)
    return Path(argv[argv.index(name)+1]).resolve()
def blob(raw):
    return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\x00"+raw).hexdigest()
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def reject(reason):raise ValueError("B01-30-W10-DOCK-SOURCE-REJECT:"+reason)

def parse_exact_catalog(raw):
    if blob(raw)!=CATALOG_GIT_BLOB:reject("IMMUTABLE_CATALOG_GIT_BLOB_SHA1")
    text=raw.decode("utf8")
    rex=r"new RigDemoDefinition\(\s*(\d+),\s*\"([^\"]+)\",\s*([+-]?[0-9.]+),\s*([0-9.]+),\s*([0-9.]+),\s*([0-9.]+),\s*([0-9.]+),\s*([+-]?[0-9.]+)\s*\)"
    found=re.findall(rex,text)
    if len(found)!=2:reject("NOT_TWO_PHYSICAL_ORIGINAL_RIG_DEFINITIONS")
    rigs={}
    for id,name,x,ch,part,clear,mass,speed in found:
        key="Light" if name=="Light rig" else "Heavy" if name=="Heavy rig" else ""
        if not key or key in rigs or int(id)!=RIGS[key][0] or abs(float(mass)-RIGS[key][1])>1e-8:
            reject("RIG_ID_OR_MASS_CHANGED")
        vals={"root_x":float(x),"chassis_width":float(ch),"part_width":float(part),
              "clearance":float(clear),"mass":float(mass),"radial_speed":float(speed)}
        if min(vals["chassis_width"],vals["part_width"],vals["clearance"])<=0:reject("INVALID_SOURCE_DIMENSIONS")
        vals["target_x"]=vals["root_x"]+(vals["chassis_width"]+vals["part_width"])*.5+vals["clearance"]
        vals["part_id"]=int(id)
        rigs[key]=vals
    if set(rigs)!=set(RIGS):reject("MISSING_SOURCE_RIG")
    return rigs

def validate_source(folder):
    catalog=(folder/"RigDemoDefinition.cs").read_bytes()
    gate=(folder/"HardenedDockDemo.cs").read_bytes()
    rigs=parse_exact_catalog(catalog)
    if blob(gate)!=HARDEN_GIT_BLOB:reject("IMMUTABLE_ALL_LAYER_HARDENED_GATE_SOURCE")
    t=gate.decode("utf8")
    must=["private const float MaxDistance = 3f;","private const float MaxPenetration = 0.005f;",
          "HardenedDockPolicy.AllPhysicsLayers","Physics.OverlapBoxNonAlloc",
          "Physics.ComputePenetration","return demo.Reattach(index);",
          "new Vector3((float)(spec.RootX + spec.PartOffsetX), 1.2f, 0f)"]
    if not all(x in t for x in must):reject("HARDENED_ORIGINAL_SOURCE_CONTRACT_DRIFT")
    return rigs

def setmat(name,rgba,metallic=.0):
    mat=bpy.data.materials.new(name)
    mat.diffuse_color=rgba
    mat.use_nodes=True
    shader=mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value=rgba
    shader.inputs["Metallic"].default_value=metallic
    shader.inputs["Roughness"].default_value=.44
    return mat

def mesh_cube(name,pos,dimensions,material):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos)
    ob=bpy.context.object
    ob.name=name
    ob.dimensions=dimensions
    ob.data.materials.append(material)
    bpy.context.view_layer.objects.active=ob
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    ob["b01_is_visual_only"]=True
    return ob

def bounds(obj):
    verts=[obj.matrix_world@v.co for v in obj.data.vertices]
    return ([min(v[k] for v in verts) for k in range(3)],
            [max(v[k] for v in verts) for k in range(3)])
def overlap_depth(a,b):
    alo,ahi=bounds(a);blo,bhi=bounds(b)
    return min(max(0.,min(ahi[k],bhi[k])-max(alo[k],blo[k])) for k in range(3))

def render(name,out,center):
    scene=bpy.context.scene
    bpy.ops.object.camera_add()
    camera=bpy.context.object
    camera.name="B01-30-RECEIVER-PREVIEW-NOT_EXPORTED"
    camera.data.type="ORTHO"
    camera.data.ortho_scale=4.6
    scene.camera=camera
    target=Vector(center)
    camera.location=target+Vector((2.7,-3.8,2.4))
    camera.rotation_euler=(target-camera.location).to_track_quat("-Z","Y").to_euler()
    scene.render.engine="BLENDER_WORKBENCH"
    scene.display.shading.color_type="MATERIAL"
    scene.display.shading.light="STUDIO"
    scene.display.shading.show_cavity=True
    scene.render.resolution_x=960
    scene.render.resolution_y=720
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format="PNG"
    file=out/(name+".png")
    scene.render.filepath=str(file)
    bpy.ops.render.render(write_still=True)
    raw=file.read_bytes()
    if len(raw)<9000 or raw[:8]!=b"\x89PNG\r\n\x1a\n":reject("NO_REAL_PNG")
    return {"file":file.name,"bytes":len(raw),"sha256":sha(file)}

def create_one(rig,status,cfg,out):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mat_ch=setmat("PROXY_CHASSIS_VIS_ONLY",(0.17,0.3,0.46,1),.28)
    mat_part=setmat("PROXY_DETACHED_PART_TARGET_VIS_ONLY",(0.1,0.76,0.51,1),.25)
    mat_danger=setmat("BLOCKER_LAYER8_VISIBLE_IN_QA_NOT_UNITY_LAYER",(0.98,0.14,0.13,1),.05)
    mat_axis=setmat("DOCK_DIRECTION_GUIDE_VIS_ONLY",(0.98,0.75,0.18,1),.02)
    center=(cfg["target_x"],1.2,0.)
    chassis=mesh_cube("ORIGINAL_DIMENSION_CHASSIS",(cfg["root_x"],1.2,0.),
          (cfg["chassis_width"],.8,1.0),mat_ch)
    part=mesh_cube("ORIGINAL_DIMENSION_DOCK_TARGET",center,
          (cfg["part_width"],.65,.65),mat_part)
    guide=mesh_cube("DOCK_DIRECTION_AMBER_GUIDE",
          ((cfg["root_x"]+cfg["target_x"])*.5,.49,0.),(.12,.05,.07),mat_axis)
    blocker=None
    if status=="BLOCKED_LAYER8":
        blocker=mesh_cube("LAYER8_FOREIGN_BLOCKER_CONTACT_PROBE",center,
                    (.22,.34,.34),mat_danger)
        blocker["b01_source_layer_id_for_QA_only"]=8
        blocker["not_a_Unity_GameObject_layer"]=True
    gap=cfg["clearance"]
    delta=overlap_depth(chassis,part)
    if delta>0:reject("ORIGINAL_SAFE_SOURCE_CHASSIS_AND_PART_OVERLAP")
    if status=="SAFE" and blocker:reject("SAFE_SCENE_UNEXPECTED_BLOCKER")
    if status=="BLOCKED_LAYER8" and (not blocker or overlap_depth(part,blocker)<=TOL):
        reject("BLOCKED_SCENE_NO_REAL_3D_CONTACT")
    # Deliberately mutate each actual in-memory scene, then reject and restore:
    original=part.location.copy()
    try:
        part.location=chassis.location.copy()
        bpy.context.view_layer.update()
        if overlap_depth(part,chassis)<=TOL:reject("REAL_GEOMETRY_MUTATION_FALSE_GREEN")
    finally:
        part.location=original
        bpy.context.view_layer.update()
    scene_meshes=[o for o in bpy.data.objects if o.type=="MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for ob in scene_meshes:ob.select_set(True)
    bpy.context.view_layer.objects.active=scene_meshes[0]
    name=rig+"-"+status+"-ORIGINAL-DOCK-VISUAL"
    fbx=out/(name+".fbx")
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
            axis_forward="-Z",axis_up="Y",global_scale=1.,apply_unit_scale=True,bake_anim=False)
    blend=out/(name+".blend")
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    if not fbx.read_bytes().startswith(b"Kaydara FBX Binary  \x00\x1a\x00") or fbx.stat().st_size<10000 or blend.stat().st_size<100000:reject("INVALID_REAL_3D_OUTPUT")
    png=render(name,out,center)
    return {"rig":rig,"variant":status,"part_id":cfg["part_id"],"part_mass_kg":cfg["mass"],
      "chassis_center_xyz":[cfg["root_x"],1.2,0.],"chassis_dimensions_xyz":[cfg["chassis_width"],.8,1.],
      "target_center_xyz":list(center),"part_dimensions_xyz":[cfg["part_width"],.65,.65],
      "source_chassis_part_clearance_m":gap,"source_3d_chassis_part_AABB_overlap_m":delta,
      "blocked_real_part_and_layer8_blocker_AABB_contact_m":overlap_depth(part,blocker) if blocker else 0,
      "real_mesh_objects":len(scene_meshes),
      "fbx":{"file":fbx.name,"bytes":fbx.stat().st_size,"sha256":sha(fbx)},
      "blend":{"file":blend.name,"bytes":blend.stat().st_size,"sha256":sha(blend)},
      "png":png}

def main():
    src=params("--source");out=params("--out")
    out.mkdir(parents=True,exist_ok=True)
    rigs=validate_source(src)
    negative_source=(src/"RigDemoDefinition.cs").read_bytes()+b" "
    try:parse_exact_catalog(negative_source)
    except ValueError as e:
        if "IMMUTABLE_CATALOG_GIT_BLOB_SHA1" not in str(e):raise
    else:reject("SOURCE_MUTATION_NOT_REJECTED")
    rows=[create_one(rig,state,cfg,out) for rig,cfg in rigs.items() for state in ("SAFE","BLOCKED_LAYER8")]
    if len(rows)!=4 or sum(x["real_mesh_objects"] for x in rows)!=14:reject("WRONG_GENERATED_GEOMETRY_COUNT")
    receipt={"package_id":PACKAGE,"producer":"B01-30 3D/TECHART",
      "source_original_catalog_commit":CATALOG_HEAD,"source_original_catalog_blob":CATALOG_GIT_BLOB,
      "source_hardened_reattach_pr":471,"source_hardened_sha":HARDEN_HEAD,
      "source_hardened_blob":HARDEN_GIT_BLOB,
      "original_source_model_units_m":True,
      "visual_only_NOT_Unity_PhysX":True,"status":"ACTUAL_4_SOURCE_LOCKED_FBX_BLEND_AND_PNG_GENERATED",
      "cases":rows,"negative_source_mutants_rejected":1,"actual_in_memory_overlap_mutants_rejected":4,
      "not_run":["native Unity6 import","PhysX OverlapBoxNonAlloc/ComputePenetration","public API hardening","B01-40/60 independent QA","Codex integration"]}
    (out/"source-locked-safedock-3d-qa.json").write_text(json.dumps(receipt,indent=2)+"\n")
    print("B0130_W10_SOURCE_LOCKED_4_REAL_FBX_4_BLEND_4_PNG_PASS",
        json.dumps({"rigs":2,"safe":2,"blocked":2,"source_mutants":1,
                    "geometry_mutants":4,"models":len(rows)}))
if __name__=="__main__":main()
