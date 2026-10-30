#!/usr/bin/env python3
"""Convert existing #423 REAL GLB visual fixtures to three Unity-importable FBXs.

Blender source-via-GLB export only. No new gameplay geometry or colliders.
The three original #423 GLB binary SHA256 values and four immutable B01-23
original JSON Git blobs are checked before any export.
"""
from __future__ import annotations
import bpy
import hashlib
import json
import sys
from pathlib import Path

RIGS={"Flanker":(15,1),"Bulwark":(16,2),"Skirmisher":(12,2)}
ORIGINAL_P17="5666a39eabd4cea91b2b605a9eb99ea2f1e226e8"
ORIGINAL_GLBS="60a453e4ecd915725bc2fac5433ba7ad43a628cb"
BLOBS={
 "P17_Flanker_Reinforced.json":"7259e3454b2f7dd72ca66de83673c143075a807f",
 "P17_Bulwark_Reinforced.json":"8b00e5da047a22bc23644639e3292c447dd6b335",
 "P17_Skirmisher_Reinforced.json":"edf332a66645c5ea4374b1957d2cc1a7d356e4d2",
 "P17_StructuralBracingKits.json":"9b3d2403e7f6e32ea6e20f9f5bb0a83b859bc6e1"
}
def arg(flag):
    a=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    if flag not in a:raise ValueError("MISSING_ARG:"+flag)
    return Path(a[a.index(flag)+1]).resolve()
def sha(raw):return hashlib.sha256(raw).hexdigest()
def gitblob(raw):return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\x00"+raw).hexdigest()
def die(m):raise ValueError("B0130-P17W09-FAIL_CLOSED:"+m)
def source(root):
    data={}
    for p,expected in BLOBS.items():
        f=root/p
        if gitblob(f.read_bytes())!=expected:die("OWNER_SOURCE_BLOB:"+p)
        data[p]=json.loads(f.read_text("utf8"))
    kits=data["P17_StructuralBracingKits.json"]["kits"]
    out={}
    for name,(count,braces) in RIGS.items():
        rig=data["P17_"+name+"_Reinforced.json"]
        assert len(rig["parts"])==count
        found=[k for k in kits if k["variantId"]==rig["blueprintId"]]
        assert len(found)==1
        braceids={part["localId"] for part in found[0]["mandatoryParts"]}
        assert len(braceids)==braces
        out[name]=(rig,braceids)
    return out

def inspect_meshes(name,objects,rig,braces,phase):
    expected={part["localId"]:part for part in rig["parts"]}
    if len(objects)!=len(expected):die("MESH_COUNT_"+phase+"_"+name+":"+str(len(objects)))
    byid={}
    for obj in objects:
        if not obj.name.startswith("P17PART__"):die("UNKNOWN_MESH_NAME:"+obj.name)
        ident=obj.name.removeprefix("P17PART__")
        if ident not in expected or ident in byid:die("UNKNOWN_OR_DUPLICATE_PART:"+ident)
        byid[ident]=obj
        c=expected[ident]["cell"]
        wanted=(c["x"]*.55,c["z"]*.55,c["y"]*.55)
        measured=tuple(obj.matrix_world.translation)
        if max(abs(a-b) for a,b in zip(measured,wanted))>0.0015:
            die("SOURCE_POSITION_DRIFT:"+phase+":"+ident+":"+str(measured)+":"+str(wanted))
        # Real FBX should retain an original, distinct material for amber braces.
        actual=[slot.material.name.split(".")[0] for slot in obj.material_slots if slot.material]
        desired=("BRACE_SOURCE" if ident in braces else expected[ident]["kind"])+"_VIS_ONLY"
        if desired not in actual:die("VISUAL_MATERIAL_OR_BRACE_LOST:"+phase+":"+ident+":"+str(actual))
        if len(obj.data.vertices)<8 or len(obj.data.polygons)<6:die("NON_REAL_MESH:"+ident)
    return sum(len(obj.data.polygons) for obj in objects)

def main():
    original=arg("--original")
    owner=arg("--owner")
    out=arg("--out")
    out.mkdir(parents=True,exist_ok=True)
    files=list(original.rglob("source-export-receipt.json"))
    if len(files)!=1:die("PREVIOUS_RECEIPT_ABSENT_OR_REPEAT")
    receipt=json.loads(files[0].read_text())
    if receipt.get("package_id")!="B01-30-P17-BRACE-VIS-001/R1" or receipt.get("original_source_sha")!=ORIGINAL_P17:die("PREVIOUS_ARTIFACT_PROVENANCE")
    if receipt.get("original_source_git_blobs")!=dict(sorted(BLOBS.items())):die("PREVIOUS_SOURCE_BLOB_LIST")
    originals=source(owner)
    exports=receipt["exports"]
    if len(exports)!=3 or {e["rig"] for e in exports}!=set(RIGS):die("ORIGINAL_EXPORTED_RIG_IDS")
    results=[]
    for name,(parts,braces) in originals.items():
        entry=next(e for e in exports if e["rig"]==name)
        original_file=list(original.rglob(entry["glb"]))
        if len(original_file)!=1:die("REAL_GLTF_ASSET_NOT_FOUND:"+name)
        original_bytes=original_file[0].read_bytes()
        if len(original_bytes)!=entry["glb_bytes"] or sha(original_bytes)!=entry["glb_sha256"] or original_bytes[:4]!=b"glTF":
            die("REAL_GLTF_SOURCE_SHA_MISMATCH:"+name)
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.gltf(filepath=str(original_file[0]))
        bpy.context.view_layer.update()
        obj=[o for o in bpy.data.objects if o.type=="MESH"]
        source_poly=inspect_meshes(name,obj,parts,braces,"GLB_SOURCE")
        for x in bpy.context.selected_objects:x.select_set(False)
        for x in obj:x.select_set(True)
        bpy.context.view_layer.objects.active=obj[0]
        fbx=out/(name+"-P17-ORIGINAL-GLB-VISUAL-UNITY.fbx")
        bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
            global_scale=1.0,apply_unit_scale=True,
            axis_forward="-Z",axis_up="Y",bake_anim=False,
            add_leaf_bones=False,use_mesh_modifiers=False)
        data=fbx.read_bytes()
        if not data.startswith(b"Kaydara FBX Binary  \x00\x1a\x00") or len(data)<12000:die("NOT_REAL_BINARY_FBX")
        results.append({"rig":name,"source_glb_sha256":sha(original_bytes),
          "source_glb_bytes":len(original_bytes),"source_polygon_total":source_poly,
          "fbx":fbx.name,"fbx_bytes":len(data),"fbx_sha256":sha(data),
          "source_part_meshes":len(obj),"source_amber_braces":len(braces)})
    result={"package":"B01-30-P17-ORIGINAL-GLB-UNITY-FBX-001/R1",
        "input_upstream_pr":423,"input_head":ORIGINAL_GLBS,"owner_pr":375,"owner_head":ORIGINAL_P17,
        "owner_four_original_git_blobs":BLOBS,
        "result":"REAL_ORIGINAL_GLB_TO_THREE_BINARY_FBX_EXPORTED",
        "rbc":"Visual diagnostic only, NOT new PARTSIZE collider or new game model",
        "files":results,"unity_editor":"NOT_RUN","independent_QA":"PENDING"}
    (out/"fbx-source-locked-export.json").write_text(json.dumps(result,indent=2,sort_keys=True)+"\n")
    print("B0130_P17W09_ORIGINAL_3_GLB_TO_THREE_REAL_BINARY_FBX_EXPORTED",
        json.dumps({"fbx":len(results),"meshes":sum(x["source_part_meshes"] for x in results),
          "braces":sum(x["source_amber_braces"] for x in results)}))
if __name__=="__main__":main()
