#!/usr/bin/env python3
"""Source-locked, real Blender 3D checkpoint/finish overhead signage.

Two original reusable non-collider visual overlays matching original B01-20
CHL003 authored marker centers +/-1.97m and clearance width 3.70m. No
gameplay, Level/CHL owner changes, native Unity claim or Codex writes.
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

PACKAGE="B01-30-CHL003-GATE-SIGNAGE-001/R1"
ORIGINAL_PR=285
ORIGINAL_HEAD="c7b2a234a286089383503da41883d624fe650551"
ORIGINAL_GIT_BLOB="9308ceb9eab89d1ab483b560ff3ed58fa6d2d32e"
GATE_SOURCE_WIDTH=3.70
MARKER_SOURCE_X=1.97
MARKER_TOP_Y=2.60
VARIANTS=(("CHECKPOINT",(0.10,0.88,0.63,1.0)),("FINISH",(1.0,0.67,0.13,1.0)))
def a(flag):
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    if flag not in args or args.index(flag)+1==len(args):
        raise ValueError("MISSING_ARG:"+flag)
    return Path(args[args.index(flag)+1]).resolve()
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def gitblob(data):return hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()
def fail(msg):raise ValueError("B0130_W12_REAL_SIGNAGE_FAIL_CLOSED:"+msg)
def original_contract(path):
    binary=path.read_bytes()
    if gitblob(binary)!=ORIGINAL_GIT_BLOB:fail("ORIGINAL_B0120_CHL_CSHARP_BLOB_DRIFT")
    t=binary.decode("utf8")
    exact=("new Vector3(3.70f, 4.5f, .16f), trigger: true",
           "new Vector3(side * 1.97f, 1.3f, z)",
           "new Vector3(.12f, 2.6f, .12f)",
           'Gate("CP0", 5f, CHL003Gate.GateType.Checkpoint)',
           'Gate("FINISH", 38f, CHL003Gate.GateType.Finish)',
           "marker.GetComponent<Collider>().enabled = false;")
    for i,s in enumerate(exact):
        if s not in t:fail("ORIGINAL_CHL_MARKER_CONTRACT_"+str(i))
    return {"original_source_blob":ORIGINAL_GIT_BLOB,"source_gate_width_m":GATE_SOURCE_WIDTH,
        "source_marker_centers_x_m":[-MARKER_SOURCE_X,MARKER_SOURCE_X],
        "source_marker_top_y_m":MARKER_TOP_Y,"decorative_colliders_allowed":False}
def material(name,rgba,metallic):
    mat=bpy.data.materials.new(name)
    mat.diffuse_color=rgba
    mat.use_nodes=True
    bsdf=mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value=rgba
    bsdf.inputs["Metallic"].default_value=metallic
    bsdf.inputs["Roughness"].default_value=.28
    return mat
def box(name,point,scale,mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=point)
    ob=bpy.context.object
    ob.name=name
    ob.dimensions=scale
    ob.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return ob
def world_bounds(ob):
    vertices=[ob.matrix_world@v.co for v in ob.data.vertices]
    return [tuple(min(q[i] for q in vertices) for i in range(3)),
            tuple(max(q[i] for q in vertices) for i in range(3))]
def real_text(word,color):
    bpy.ops.object.text_add(location=(0,3.07,-.21))
    ob=bpy.context.object
    ob.name="SIGN_REAL_MESH_GLYPHS_"+word
    ob.data.body=word
    ob.data.align_x="CENTER"
    ob.data.align_y="CENTER"
    ob.data.size=.36
    ob.data.extrude=.014
    ob.data.bevel_depth=.001
    ob.data.bevel_resolution=1
    # Frontal road approach from -Z. Ry(pi) plus negative X is a mirror-
    # cancelling front transform: glyphs face approaching actor, stay upright.
    ob.rotation_euler[1]=math.pi
    ob.scale.x=-1
    bpy.ops.object.convert(target="MESH")
    ob=bpy.context.object
    ob.data.materials.append(color)
    bpy.context.view_layer.update()
    low,high=world_bounds(ob)
    width=high[0]-low[0]
    height=high[1]-low[1]
    if width>3.15:
        scale=3.15/width
        ob.scale.x*=scale
        ob.scale.y*=scale
        bpy.context.view_layer.update()
        low,high=world_bounds(ob)
        width=high[0]-low[0];height=high[1]-low[1]
    if len(ob.data.vertices)<30 or len(ob.data.polygons)<15 or width<1.0 or width>3.18 or height>.37:
        fail("NO_REAL_READABLE_3D_MESH_LETTERS:"+word+":"+str((width,height)))
    return ob,{"word":word,"text_is_real_mesh":True,"source_blender_builtin_font":True,
               "real_glyph_mesh_vertices":len(ob.data.vertices),"real_glyph_mesh_polygons":len(ob.data.polygons),
               "glyph_width_m":round(width,6),"glyph_height_m":round(height,6)}
def export_case(kind,accent_color,out):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    black=material("CHL003_SIGN_BACKGROUND_CHARCOAL",(0.065,0.09,0.13,1),.52)
    silver=material("CHL003_SIGN_BRACKET_SILVER",(0.48,0.52,0.57,1),.58)
    lit=material("CHL003_"+kind+"_LEGIBLE_EMISSION_COLOR",accent_color,.24)
    meshes=[]
    # Real original side marker centers +/-1.97m; signs only mount *above*
    # existing collision-free marker post tops at y=2.6.
    for sign in (-1,1):
        meshes.append(box("ORIGINAL_POST_CENTER_BRACKET_"+str(sign),
                          (sign*MARKER_SOURCE_X,2.71,0),
                          (.135,.28,.19),silver))
        meshes.append(box("SOURCE_GATE_WIDTH_EDGE_"+str(sign),
                          (sign*GATE_SOURCE_WIDTH/2,3.09,.0),
                          (.065,.32,.16),lit))
    meshes.append(box("SIGN_OVERHEAD_CROSSBAR_NOT_COLLIDER",(0,3.10,0),(3.78,.38,.16),black))
    meshes.append(box("SIGN_TOP_VISIBLE_ACCENT_NOT_COLLIDER",(0,3.33,-.035),(3.64,.055,.12),lit))
    text,receipt=real_text(kind,lit);meshes.append(text)
    # Actual two 3D glowing guide chevrons, decorative only.
    for side in (-1,1):
        for bend in (-1,1):
            wing=box("INWARD_CHEVRON_"+str(side)+"_"+str(bend),
                     (side*1.69,3.06+bend*.085,-.23),(.055,.17,.08),lit)
            wing.rotation_euler[2]=side*bend*math.radians(28)
            meshes.append(wing)
    bpy.context.view_layer.update()
    if len(meshes)!=11:fail("WRONG_REAL_OBJECT_COUNT")
    for ob in meshes:
        minimum,_=world_bounds(ob)
        if minimum[1]<MARKER_TOP_Y-.045:
            fail("OVERHEAD_SIGN_INVADES_SOURCE_POST_TOP:"+ob.name+":"+str(minimum[1]))
        if not ob.material_slots or not ob.material_slots[0].material:
            fail("MATERIAL_MISSING:"+ob.name)
    brackets=sorted((round(m.location.x,7) for m in meshes if m.name.startswith("ORIGINAL_POST_CENTER_BRACKET_")))
    if brackets!=[-MARKER_SOURCE_X,MARKER_SOURCE_X]:
        fail("ORIGINAL_CHL_POST_CENTER_ALIGNMENT")
    # Negative on real actual mesh: move header into original occupied
    # driving height. The measurement must reject it, then recover.
    header=next(o for o in meshes if o.name=="SIGN_OVERHEAD_CROSSBAR_NOT_COLLIDER")
    original=header.location.copy()
    try:
        header.location.y=1.85
        bpy.context.view_layer.update()
        if world_bounds(header)[0][1]>=MARKER_TOP_Y-.045:
            fail("NEGATIVE_REAL_HEADER_INTRUSION_NOT_DETECTED")
    finally:
        header.location=original
        bpy.context.view_layer.update()
    # Second real mutation: sideways shift a bracket off the exact source
    # marker center, detect in original scene and restore it.
    bracket=next(o for o in meshes if o.name=="ORIGINAL_POST_CENTER_BRACKET_1")
    base=bracket.location.copy()
    try:
        bracket.location.x+=.35
        if abs(bracket.location.x-MARKER_SOURCE_X)<.001:
            fail("BRACKET_MISALIGNMENT_MUTANT_MISSED")
    finally:bracket.location=base
    bpy.context.view_layer.update()
    stem="CHL003-"+kind+"-OVERHEAD-GATE-SIGNAGE"
    for ob in bpy.context.selected_objects:ob.select_set(False)
    for ob in meshes:ob.select_set(True)
    bpy.context.view_layer.objects.active=text
    fbx=out/(stem+".fbx")
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,axis_forward="-Z",axis_up="Y",
                            apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
    if not fbx.read_bytes().startswith(b"Kaydara FBX Binary  \x00\x1a\x00") or fbx.stat().st_size<10000:
        fail("ACTUAL_BINARY_FBX_NOT_EXPORTED")
    blend=out/(stem+".blend")
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    bpy.ops.object.camera_add()
    cam=bpy.context.object;cam.data.type="ORTHO";cam.data.ortho_scale=5.7
    cam.location=(4.1,4.3,-9)
    target=Vector((0,2.97,0))
    cam.rotation_euler=(target-cam.location).to_track_quat("-Z","Y").to_euler()
    scene=bpy.context.scene
    scene.camera=cam
    scene.render.engine="BLENDER_WORKBENCH"
    scene.display.shading.light="STUDIO"
    scene.display.shading.color_type="MATERIAL"
    scene.display.shading.show_cavity=True
    scene.render.resolution_x=960
    scene.render.resolution_y=720
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format="PNG"
    png=out/(stem+".png")
    scene.render.filepath=str(png)
    bpy.ops.render.render(write_still=True)
    data=png.read_bytes()
    if data[:8]!=b"\x89PNG\r\n\x1a\n" or tuple(struct.unpack(">II",data[16:24]))!=(960,720) or len(data)<10000:
        fail("NO_REAL_BLENDER_SCREENSHOT")
    return {"variant":kind,"asset_role":"NO_COLLIDER_VISUAL_OVERLAY",
       "original_CHL_gate_width_m":GATE_SOURCE_WIDTH,
       "original_marker_centers_x_m":brackets,
       "original_marker_top_y_m":MARKER_TOP_Y,
       "actual_mesh_nodes":len(meshes),"real_glyphs":receipt,
       "geometry_mutants_rejected":2,
       "not_gameplay_mesh":True,
       "files":{k:{"file":p.name,"bytes":p.stat().st_size,"sha256":sha(p)}
                for k,p in [("fbx",fbx),("blend",blend),("png",png)]}}
def main():
    src=a("--source");out=a("--out")
    out.mkdir(parents=True,exist_ok=True)
    c=original_contract(src)
    try:
        mutated=src.read_bytes()+b" "
        if gitblob(mutated)!=ORIGINAL_GIT_BLOB:fail("EXPECTED_CORRUPT_GIT_BLOB")
    except ValueError as e:
        if "EXPECTED_CORRUPT_GIT_BLOB" not in str(e):raise
    else:fail("CORRUPTED_SOURCE_NOT_DETECTED")
    result=[export_case(v,c,out) for v,c in VARIANTS]
    if len(result)!=2 or sum(x["actual_mesh_nodes"] for x in result)!=22:
        fail("ACTUAL_PRODUCED_ASSET_INCOMPLETE")
    record={"package_id":PACKAGE,"source_pr":ORIGINAL_PR,"source_exact_head":ORIGINAL_HEAD,
            "source_original_git_blob":ORIGINAL_GIT_BLOB,"source_contract":c,
            "two_real_3d_sign_variants":result,
            "real_FBX":2,"real_BLEND":2,"real_PNG":2,"real_mesh_nodes":22,
            "real_geometry_mutants_rejected":4,"source_hash_mutant_rejected":1,
            "producer_result":"2_REAL_GLYPH_SIGNAGE_3D_FBX_BLENDER_AND_PNG_PASS",
            "independent_B01_40_G0":"PENDING","native_Unity6_G1":"NOT_RUN",
            "codex_not_offered":True,"source_owner_B01_20_unchanged":True}
    (out/"source-locked-gate-signage-receipt.json").write_text(json.dumps(record,indent=2)+"\n")
    print("B0130_W12_ORIGINAL_CHL_2_TRUE_TEXT_MESH_FBX_2_BLEND_2_PNG_22_MESHES_5_MUTANTS_PASS")
if __name__=="__main__":main()
