#!/usr/bin/env python3
"""Fresh-process original FBX reimport, 43 source positions, named material QA.

This is an actual Blender 3D geometry verification, not native Unity G1.
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

RIGS={"Flanker":(15,1),"Bulwark":(16,2),"Skirmisher":(12,2)}
def arg(flag):
    a=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    if flag not in a:raise ValueError("MISSING_ARG:"+flag)
    return Path(a[a.index(flag)+1]).resolve()
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def die(message):raise ValueError("B0130-W09-FBX-REIMPORT-REJECT:"+str(message))
def render(name,objects,out):
    positions=[o.matrix_world.translation for o in objects]
    center=sum(positions,Vector((0,0,0)))/len(positions)
    extent=max((p-center).length for p in positions)+1.0
    bpy.ops.object.camera_add()
    cam=bpy.context.object
    cam.data.type="ORTHO"
    cam.data.ortho_scale=extent*3.2
    scene=bpy.context.scene
    scene.camera=cam
    cam.location=center+Vector((2.5,-3.2,1.8)).normalized()*(extent*4)
    cam.rotation_euler=(center-cam.location).to_track_quat("-Z","Y").to_euler()
    scene.render.engine="BLENDER_WORKBENCH"
    scene.display.shading.light="STUDIO"
    scene.display.shading.show_cavity=True
    scene.display.shading.color_type="MATERIAL"
    scene.render.resolution_x=960
    scene.render.resolution_y=720
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format="PNG"
    f=out/(name+"-SOURCE-GLB-REAL-FBX-ROUNDTRIP.png")
    scene.render.filepath=str(f)
    bpy.ops.render.render(write_still=True)
    d=f.read_bytes()
    if d[:8]!=b"\x89PNG\r\n\x1a\n" or tuple(struct.unpack(">II",d[16:24]))!=(960,720) or len(d)<12000:
        die("REAL_FBX_RENDER_NOT_VALID:"+name)
    return {"file":f.name,"size":len(d),"sha256":sha(f),"width":960,"height":720}

def main():
    fbxdir=arg("--fbxdir");owner=arg("--owner");out=arg("--out")
    out.mkdir(parents=True,exist_ok=True)
    src=json.loads((fbxdir/"fbx-source-locked-export.json").read_text())
    assert src["result"]=="REAL_ORIGINAL_GLB_TO_THREE_BINARY_FBX_EXPORTED"
    kits=json.loads((owner/"P17_StructuralBracingKits.json").read_text())["kits"]
    summaries=[]
    for entry in src["files"]:
        name=entry["rig"]
        if name not in RIGS:die("UNEXPECTED_RIG")
        original=json.loads((owner/("P17_"+name+"_Reinforced.json")).read_text())
        ownerparts={p["localId"]:p for p in original["parts"]}
        brace_sets=[k for k in kits if k["variantId"]==original["blueprintId"]]
        assert len(brace_sets)==1
        braces={p["localId"] for p in brace_sets[0]["mandatoryParts"]}
        file=fbxdir/entry["fbx"]
        if len(file.read_bytes())!=entry["fbx_bytes"] or sha(file)!=entry["fbx_sha256"]:die("ORIGINAL_OUTPUT_FBX_SHA_DRIFT:"+name)
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(file),use_anim=False)
        bpy.context.view_layer.update()
        meshes=[o for o in bpy.data.objects if o.type=="MESH"]
        if len(meshes)!=RIGS[name][0]:die("FBX_REIMPORTED_MESH_COUNT:"+name+":"+str(len(meshes)))
        found=set();sourcetot=0
        maxerr=0.0
        for o in meshes:
            if not o.name.startswith("P17PART__"):die("FBX_IMPORT_CHANGED_MESH_NAME:"+o.name)
            ident=o.name.removeprefix("P17PART__")
            if ident not in ownerparts or ident in found:die("FBX_SOURCE_PART_ID_CORRUPTED:"+ident)
            found.add(ident)
            part=ownerparts[ident];cell=part["cell"]
            expected=(cell["x"]*.55,cell["z"]*.55,cell["y"]*.55)
            actual=tuple(o.matrix_world.translation)
            gap=max(abs(expected[i]-actual[i]) for i in range(3))
            maxerr=max(maxerr,gap)
            if gap>0.0015:die("FBX_PART_WORLD_POSITION:"+name+":"+ident+":"+str(actual)+":"+str(expected))
            expected_material=("BRACE_SOURCE" if ident in braces else part["kind"])+"_VIS_ONLY"
            materials=[slot.material.name.split(".")[0] for slot in o.material_slots if slot.material]
            if expected_material not in materials:die("FBX_BRACE_OR_KIND_MATERIAL_LOST:"+name+":"+ident+":"+str(materials))
            if len(o.data.vertices)<8 or len(o.data.polygons)<6:die("FBX_MESH_TRIANGLE_LOSS:"+ident)
            sourcetot+=len(o.data.polygons)
        if len(found)!=len(ownerparts):die("FBX_ORIGINAL_SOURCE_PARTS_MISSING:"+name)
        if sourcetot!=entry["source_polygon_total"]:die("FBX_GEOMETRY_FACE_COUNT_CHANGED:"+name+":"+str(sourcetot)+":"+str(entry["source_polygon_total"]))
        img=render(name,meshes,out)
        summaries.append({"rig":name,"mesh_count":len(meshes),"original_brace_count":len(braces),
          "face_count":sourcetot,"max_world_delta_m":round(maxerr,8),
          "fbx_sha256":entry["fbx_sha256"],"original_glb_sha256":entry["source_glb_sha256"],"render":img})
    assert len(summaries)==3 and sum(s["mesh_count"] for s in summaries)==43 and sum(s["original_brace_count"] for s in summaries)==5
    result={"package_id":"B01-30-P17-ORIGINAL-GLB-UNITY-FBX-001/R1",
      "input_glb_head":"60a453e4ecd915725bc2fac5433ba7ad43a628cb",
      "owner_head":"5666a39eabd4cea91b2b605a9eb99ea2f1e226e8",
      "status":"ORIGINAL_3_BINARY_FBX_FRESH_BLENDER_REIMPORT_43_SOURCE_MESH_AND_5_BRACE_PASS",
      "source_was_modified":False,"real_binary_fbx":summaries,
      "real_meshes":43,"original_braces":5,"real_pngs":3,
      "not_run":["actual Unity 6000.3.14f1 native ModelImporter","URP materials","Unity physics","independent B01-40 B01-60 QA"]}
    (out/"p17-original-fbx-roundtrip-receipt.json").write_text(json.dumps(result,indent=2,sort_keys=True)+"\n")
    print("B0130_W09_THREE_ORIGINAL_FBX_REAL_BLENDER_ROUNDTRIP_43_MESH_5_BRACE_PASS",
      json.dumps({"fbx":3,"meshes":43,"braces":5,"real_png":3,"maxerr":max(s["max_world_delta_m"] for s in summaries)}))
if __name__=="__main__":main()
