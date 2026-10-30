#!/usr/bin/env python3
"""ACTUAL Blender 4.x geometry from unmodified B01-23 P17 reinforced rigs.
43 source-pinned logical part proxies across 3 3D GLB + source BLEND files.
The proxy shapes are NOT gameplay meshes, colliders, socket/clearance measurements.
Blender Z-up geometry -> GLB Y-up via standard exporter, source has Y-up.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from source_contract import GRID_M, PROXY_BOX_M, RIG_FILES, load_pinned

MAT={
 "Core":((0.10,0.56,0.88,1),0.22),
 "Armor":((0.46,0.49,0.56,1),0.32),
 "Gun":((0.78,0.24,0.52,1),0.21),
 "Wheel":((0.13,0.15,0.19,1),0.11),
 "CrawlerTrack":((0.26,0.29,0.36,1),0.10),
 "HoverEngine":((0.10,0.67,0.53,1),0.21),
 "BRACE_SOURCE":((0.99,0.71,0.12,1),0.32),
}
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def actual_position(part):
    c=part["cell"]
    return (c["x"]*GRID_M,c["z"]*GRID_M,c["y"]*GRID_M)

def new_material(name,rgba,metallic):
    m=bpy.data.materials.new(name=name)
    m.diffuse_color=rgba
    m.use_nodes=True
    bs=m.node_tree.nodes.get("Principled BSDF")
    bs.inputs["Base Color"].default_value=rgba
    bs.inputs["Metallic"].default_value=metallic
    bs.inputs["Roughness"].default_value=0.52
    return m

def part_mesh(part,highlight,materials):
    kind=part["kind"]
    loc=actual_position(part)
    if kind=="Wheel":
        bpy.ops.mesh.primitive_cylinder_add(vertices=20,radius=0.235,depth=0.275,location=loc)
        shape="wheel-disc"  # X-axis axle, only visual, no actual wheel/physics claim
        bpy.context.object.rotation_euler[1]=math.pi/2
    elif kind=="CrawlerTrack":
        bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
        bpy.context.object.scale=(0.38,0.35,0.46)
        shape="track-placeholder"
    elif kind=="HoverEngine":
        bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=0.227,depth=0.23,location=loc)
        shape="hover-puck"
    elif kind=="Gun":
        bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
        bpy.context.object.scale=(0.28,0.33,0.46)
        shape="gun-placeholder"
    elif kind=="Core":
        bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
        bpy.context.object.scale=(0.49,0.49,0.49)
        shape="core-block"
    else:
        bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
        bpy.context.object.scale=(PROXY_BOX_M,PROXY_BOX_M,PROXY_BOX_M)
        shape="armor-block"
    obj=bpy.context.object
    obj.name="P17PART__"+part["localId"]
    obj.data.name="MESH__"+part["localId"]
    obj.data.materials.append(materials["BRACE_SOURCE" if highlight else kind])
    obj["p17_source_localId"]=part["localId"]
    obj["p17_source_kind"]=kind
    obj["p17_source_cell"]=str([part["cell"][k] for k in ("x","y","z")])
    obj["p17_source_brace"]=bool(highlight)
    obj["p17_visual_shape"]=shape
    obj["p17_visual_only_no_collider"]=True
    # Geometry scale baked into vertices; world transforms retain original cell positions.
    bpy.context.view_layer.objects.active=obj
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    assert tuple(round(x,6) for x in obj.location)==tuple(round(x,6) for x in loc)
    return obj

def build_one(name,rig,brace_ids,out):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    materials={kind:new_material(kind+"_VIS_ONLY",*val) for kind,val in MAT.items()}
    objects=[]
    for part in rig["parts"]:
        objects.append(part_mesh(part,part["localId"] in brace_ids,materials))
    assert len(objects)==RIG_FILES[name][2]
    assert sum(bool(o.get("p17_source_brace")) for o in objects)==RIG_FILES[name][3]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    file=out/(name+"-P17-BRACED-VISUAL-ONLY.glb")
    # Standard GLB2 with Blender Z-up -> Y-up transform; Unity-native NOT_RUN.
    bpy.ops.export_scene.gltf(filepath=str(file),export_format="GLB",
                             use_selection=True,export_extras=True,
                             export_animations=False,export_yup=True)
    assert file.stat().st_size>20000,(file,file.stat().st_size)
    blend=out/(name+"-P17-BRACED-VISUAL-ONLY.blend")
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    assert blend.stat().st_size>100000
    return {"rig":name,"blueprintId":rig["blueprintId"],"part_nodes":len(objects),
            "highlighted_brace_nodes":len(brace_ids),
            "source_part_positions_blender_m":{
                p["localId"]:[round(x,6) for x in actual_position(p)] for p in rig["parts"]
            },"glb":file.name,"glb_bytes":file.stat().st_size,"glb_sha256":sha(file),
            "blend":blend.name,"blend_bytes":blend.stat().st_size,"blend_sha256":sha(blend)}

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    parser=argparse.ArgumentParser()
    parser.add_argument("--source",type=Path,required=True)
    parser.add_argument("--out",type=Path,required=True)
    a=parser.parse_args(argv)
    a.out.mkdir(parents=True,exist_ok=True)
    rigs,kit,blobs,validation=load_pinned(a.source)
    recipes=[]
    for name,(filename,blob,expected,extras) in RIG_FILES.items():
        blueprint=rigs[name]["blueprintId"]
        found=[k for k in kit["kits"] if k["variantId"]==blueprint]
        assert len(found)==1
        recipes.append(build_one(name,rigs[name],
                                 {p["localId"] for p in found[0]["mandatoryParts"]},
                                 a.out))
    receipt={
        "package_id":"B01-30-P17-BRACE-VIS-001/R1",
        "producer":"B01-30 TECH ART",
        "partsize_owner":"B01-23",
        "original_source_pr":375,
        "original_source_sha":"5666a39eabd4cea91b2b605a9eb99ea2f1e226e8",
        "original_source_git_blobs":blobs,
        "source_validation":validation,
        "metres_per_grid_cell_observed_not_canonical":GRID_M,
        "proxy_width_metres_visual_only":PROXY_BOX_M,
        "blender_up":"Z","export_GLTF_up":"Y",
        "collision_geometry":"NOT_IMPLEMENTED",
        "unity_editor":"NOT_RUN","native_physx":"NOT_RUN",
        "status":"ACTUAL_GLBS_AND_BLEND_EXPORTED_SOURCE_G0_PRODUCER",
        "exports":recipes
    }
    (a.out/"source-export-receipt.json").write_text(json.dumps(receipt,indent=2)+"\n")
    print("B01_30_P17_SOURCE_LOCKED_3_REAL_GLB_3_REAL_BLEND_PASS",
          json.dumps({"files":len(recipes)*2,"parts":validation["total_parts"],
          "braces":validation["highlighted_source_braces"],
          "glb_bytes":[x["glb_bytes"] for x in recipes]}))
if __name__=="__main__":main()
