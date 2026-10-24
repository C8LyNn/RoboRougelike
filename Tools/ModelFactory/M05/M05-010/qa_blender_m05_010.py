#!/usr/bin/env python3
"""Executable independent bpy + exported glTF verification for M05-010."""
import importlib.util
import json
import math
from pathlib import Path
import struct
import sys
import tempfile

import bpy

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("m05_010_generator",HERE/"generate_m05_010.py")
generator=importlib.util.module_from_spec(spec)
spec.loader.exec_module(generator)


def glb_document(path):
    data=path.read_bytes()
    magic,version,length=struct.unpack_from("<4sII",data,0)
    assert (magic,version,length)==(b"glTF",2,len(data))
    chunk_length,chunk_type=struct.unpack_from("<I4s",data,12)
    assert chunk_type==b"JSON"
    return json.loads(data[20:20+chunk_length].decode("utf-8").rstrip(" \x00"))


def main():
    # Explicitly verify that the generator does not erase foreign scene data.
    bpy.ops.mesh.primitive_cube_add(size=.05,location=(4.,4.,4.))
    foreign=bpy.context.object
    foreign.name="FOREIGN_SENTINEL_DO_NOT_TOUCH"
    original_mesh=foreign.data
    stats=[]
    for i in range(2):
        generator.build()
        result=generator.audit()
        assert bpy.data.objects.get(foreign.name) is foreign
        assert foreign.data is original_mesh
        assert len(generator.COL.objects)==result["mesh_objects"]+5
        assert result["lamellae"]==30 and result["fan_blades"]==7
        assert result["animated_meshes"]==12
        assert result["materials"]==8
        assert result["fan_radial_clearance_m"]>=.009
        assert result["vertices"]>2500 and result["triangles"]>4500
        rotor=bpy.data.objects[generator.PREFIX+"ROTOR_PIVOT"]
        blade=bpy.data.objects[generator.PREFIX+"Rotor_Blade_00"]
        bpy.context.scene.frame_set(1)
        v1=blade.matrix_world.translation.copy()
        bpy.context.scene.frame_set(31)
        v31=blade.matrix_world.translation.copy()
        assert abs((v1-v31).length-.218)<.001,(v1,v31)
        bpy.context.scene.frame_set(61)
        v61=blade.matrix_world.translation.copy()
        assert (v1-v61).length<1e-5,(v1,v61)
        assert abs(rotor.rotation_euler[2]-2*math.pi)<1e-5
        stats.append((result["mesh_objects"],result["vertices"],result["triangles"]))
    assert stats[0]==stats[1],("non-deterministic rebuild",stats)
    # Ensure there is no duplicate collection, orphaned material or mesh
    # growth when generating twice in the same live Blender process.
    assert len([c for c in bpy.data.collections if c.name==generator.COLLECTION])==1
    assert len([m for m in bpy.data.materials if m.name.startswith(generator.PREFIX)])==8
    with tempfile.TemporaryDirectory(prefix="m05-010-bpy-qa-") as tmp:
        result=generator.export(Path(tmp))
        output=Path(tmp)
        glb=output/(generator.ID+"_vertical_radiator.glb")
        fbx=output/(generator.ID+"_vertical_radiator.fbx")
        blend=output/(generator.ID+"_vertical_radiator.blend")
        assert all(p.exists() and p.stat().st_size>1024 for p in (glb,fbx,blend))
        doc=glb_document(glb)
        assert len(doc.get("materials",[]))>=8
        assert len(doc.get("animations",[]))>=1
        assert len(doc.get("meshes",[]))>=30
        nodes=doc.get("nodes",[])
        names=[n.get("name","") for n in nodes]
        assert not any("FOREIGN_SENTINEL" in n for n in names)
        assert any("ROTOR_PIVOT" in n for n in names)
        assert any("MOUNT_TOP_SOCKET" in n for n in names)
        assert any("COOLANT_IN" in n for n in names)
        assert any("COOLANT_OUT" in n for n in names)
        result.update({"rebuild_same_scene":"PASS","foreign_objects_preserved":"PASS",
                       "animated_glb_parsed":"PASS","glb_animations":len(doc["animations"]),
                       "glb_materials":len(doc["materials"]),
                       "blender":"PASS (real bpy two builds, export and glTF parser)"})
        (output/(generator.ID+"_qa.json")).write_text(
            json.dumps(result,indent=2)+"\n",encoding="utf-8")
        print("M05-010 QA PASS",json.dumps(result,sort_keys=True))
    # CI production export runs generator again into build/M05-010.


if __name__=="__main__":
    main()
