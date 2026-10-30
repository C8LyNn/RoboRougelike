#!/usr/bin/env python3
"""Independent Blender GLB clean re-import acceptance + ACTUAL 3D renders.
Checks every original B01-23 part's WORLD coordinate, ID, kind and brace flag.
This is SOURCE/BLENDER QA, emphatically NOT Unity Collider/Socket/PhysX G1.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import math
import struct
import sys
from pathlib import Path
import bpy
from mathutils import Vector
from source_contract import GRID_M, RIG_FILES, load_pinned

def digest(p:Path):return hashlib.sha256(p.read_bytes()).hexdigest()
def loc(part):return (part["cell"]["x"]*GRID_M,
                      part["cell"]["z"]*GRID_M,
                      part["cell"]["y"]*GRID_M)
def frame_and_render(name,objects,out):
    mins=[min(float(o.matrix_world.translation[i]) for o in objects) for i in range(3)]
    maxs=[max(float(o.matrix_world.translation[i]) for o in objects) for i in range(3)]
    target=Vector([(mins[i]+maxs[i])/2 for i in range(3)])
    span=max(maxs[i]-mins[i] for i in range(3))+1.25
    bpy.ops.object.camera_add()
    camera=bpy.context.object
    camera.name="QA_PREVIEW_CAMERA_NOT_EXPORTED"
    camera.data.type="ORTHO"
    camera.data.ortho_scale=span*1.65
    scene=bpy.context.scene
    scene.camera=camera
    scene.render.engine="BLENDER_WORKBENCH"
    scene.display.shading.light="STUDIO"
    scene.display.shading.color_type="MATERIAL"
    scene.display.shading.show_cavity=True
    scene.render.resolution_x=960
    scene.render.resolution_y=720
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format="PNG"
    scene.render.film_transparent=False
    results=[]
    for tag,direction in (
        ("ISOMETRIC",(1.5,-2.0,1.35)),
        ("TOP",(0.08,-0.06,3.0))
    ):
        distance=span*2.5
        camera.location=target+Vector(direction).normalized()*distance
        camera.rotation_euler=(target-camera.location).to_track_quat("-Z","Y").to_euler()
        path=out/(name+"-GLB-CLEAN-REIMPORT-"+tag+".png")
        scene.render.filepath=str(path)
        bpy.ops.render.render(write_still=True)
        png=path.read_bytes()
        if not png.startswith(b"\x89PNG\r\n\x1a\n"):raise RuntimeError("NO_REAL_PNG:"+str(path))
        w,h=struct.unpack(">II",png[16:24])
        if (w,h)!=(960,720) or len(png)<16000:
            raise RuntimeError("PNG_BAD_FRAME_OR_NO_VISUAL:"+str(path)+":"+str(len(png)))
        results.append({"file":path.name,"bytes":len(png),"sha256":digest(path),
                        "width":w,"height":h,"render":"ACTUAL_BLENDER_WORKBENCH_CLEAN_GLB_REIMPORT"})
    return results

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    a=argparse.ArgumentParser()
    a.add_argument("--source",type=Path,required=True)
    a.add_argument("--export",type=Path,required=True)
    a.add_argument("--out",type=Path,required=True)
    args=a.parse_args(argv)
    args.out.mkdir(parents=True,exist_ok=True)
    rigs,kits,blobs,validation=load_pinned(args.source)
    producer=json.loads((args.export/"source-export-receipt.json").read_text())
    assert producer["original_source_git_blobs"]==blobs
    assert producer["source_validation"]==validation
    reports=[]
    for name,(filename,blob,expected,expected_braces) in RIG_FILES.items():
        entry=next(x for x in producer["exports"] if x["rig"]==name)
        glb=args.export/entry["glb"]
        raw=glb.read_bytes()
        assert raw[:4]==b"glTF" and struct.unpack("<I",raw[4:8])[0]==2
        assert len(raw)==struct.unpack("<I",raw[8:12])[0]
        assert len(raw)==entry["glb_bytes"] and digest(glb)==entry["glb_sha256"]
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.gltf(filepath=str(glb))
        bpy.context.view_layer.update()
        partobjs=[obj for obj in bpy.data.objects if obj.type=="MESH" and obj.name.startswith("P17PART__")]
        byid={o.name.removeprefix("P17PART__"):o for o in partobjs}
        assert len(partobjs)==expected and len(byid)==expected,("MISSING_OR_REPEAT_PART",name,len(partobjs),expected)
        brace_ids={p["localId"] for k in kits["kits"] if k["variantId"]==rigs[name]["blueprintId"]
                   for p in k["mandatoryParts"]}
        assert len(brace_ids)==expected_braces
        actual_braces=0
        diffs=[]
        for part in rigs[name]["parts"]:
            oid=part["localId"]
            assert oid in byid,("MISSING_ORIGINAL_PART",oid)
            o=byid[oid]
            actual=tuple(o.matrix_world.translation)
            want=loc(part)
            delta=max(abs(actual[i]-want[i]) for i in range(3))
            assert delta<0.00015,("WRONG_REAL_3D_POSITION",name,oid,actual,want,delta)
            assert o.get("p17_source_localId")==oid,("MISSING_GLB_EXTRAS_LOCAL_ID",oid)
            assert o.get("p17_source_kind")==part["kind"],("MISSING_GLB_EXTRAS_KIND",oid)
            assert bool(o.get("p17_source_brace"))==(oid in brace_ids),("BRACE_MARKER_WRONG",oid)
            assert o.get("p17_visual_only_no_collider") in (True,1),("FALSE_COLLIDER_CLAIM",oid)
            assert len(o.data.vertices)>=8 and len(o.data.polygons)>=6,("EMPTY_MESH",oid)
            assert len(o.data.materials)>=1,("MISSING_VISUAL_MATERIAL",oid)
            if oid in brace_ids:actual_braces+=1
            diffs.append(round(delta,8))
        assert actual_braces==expected_braces
        rendered=frame_and_render(name,partobjs,args.out)
        reports.append({"rig":name,"actual_glb_bytes":len(raw),"glb_sha256":digest(glb),
          "actual_imported_meshes":len(partobjs),"original_brace_highlight_nodes":actual_braces,
          "maximum_world_position_error_m":max(diffs),"rendered_png":rendered})
    result={
        "package_id":"B01-30-P17-BRACE-VIS-001/R1",
        "source_producer_pr":375,
        "source_producer_sha":"5666a39eabd4cea91b2b605a9eb99ea2f1e226e8",
        "result":"ACTUAL_GLBS_REIMPORTED_AND_6_PNG_RENDERED_PASS",
        "source_blobs":blobs,
        "imported_parts_total":sum(q["actual_imported_meshes"] for q in reports),
        "source_braces_highlighted":sum(q["original_brace_highlight_nodes"] for q in reports),
        "actual_blender_3d_proofs":reports,
        "known_limits":["only logical grid visual proxies","NOT native Unity or PhysX",
          "NOT accurate physical mesh/collider envelopes","NOT proof of source weakpoint mitigation",
          "original owner B01-23 source and Codex Assets unchanged"]
    }
    assert result["imported_parts_total"]==43 and result["source_braces_highlighted"]==5
    (args.out/"actual-glb-receiver-qa.json").write_text(json.dumps(result,indent=2)+"\n")
    print("B01_30_P17_ACTUAL_3_GLB_43_MESH_5_BRACE_REIMPORT_6_PNG_PASS",
          json.dumps({"meshes":43,"braces":5,"png":sum(len(q["rendered_png"]) for q in reports)}))
if __name__=="__main__":main()
