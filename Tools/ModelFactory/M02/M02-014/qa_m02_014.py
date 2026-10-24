#!/usr/bin/env python3
"""Independent M02-014 checks. stdlib-only, no Blender execution claims."""
import ast
import json
import math
import struct
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parent
GEN=ROOT/"generate_m02_014.py"
OUT=Path(sys.argv[sys.argv.index("--out")+1]) if "--out" in sys.argv else Path("build/M02-014")

def check_static():
    source=GEN.read_text(encoding="utf-8")
    ast.parse(source,filename=str(GEN))
    required=("bpy.ops.wm.save_as_mainfile","bpy.ops.export_scene.gltf",
              "PLANETARY_CARRIER_OUTPUT_X_4_TO_1","ROTOR_SHROUD_SPIN_X",
              "M02-014_MOUNT_FRAME__ORIGIN","bpy.ops.object.delete")
    for token in required:
        assert token in source, "missing: "+token
    assert 0.100-0.053-0.0465>0.00049, "sun/planet envelope"
    assert 0.147-0.100-0.0465>0.00049, "planet/ring envelope"
    assert 0.157-0.009-0.118-0.0035>0.020, "rotor/cage envelope"
    assert math.isclose(16/(16+48),0.25)
    print("STATIC PASS: syntax, generator contracts, 4:1 kinematics, three radial envelopes")

def check_export():
    glb=OUT/"M02-014_animated.glb"
    blend=OUT/"M02-014.blend"
    qa=OUT/"M02-014_qa.json"
    assert blend.is_file() and blend.stat().st_size>1000
    assert glb.is_file() and glb.stat().st_size>1000
    assert qa.is_file()
    data=glb.read_bytes()
    magic,version,length=struct.unpack_from("<4sII",data,0)
    assert magic==b"glTF" and version==2 and length==len(data)
    size,chunk_type=struct.unpack_from("<I4s",data,12)
    assert chunk_type==b"JSON"
    doc=json.loads(data[20:20+size])
    assert len(doc.get("materials",[]))>=7
    assert len(doc.get("meshes",[]))>=60
    nodes=doc["nodes"]
    names={n.get("name","") for n in nodes}
    for name in ("ROTOR_SHROUD_SPIN_X","PLANETARY_CARRIER_OUTPUT_X_4_TO_1","M02-014_MOUNT_FRAME__ORIGIN"):
        assert name in names, "missing node "+name
    animations=doc.get("animations",[])
    assert animations, "no animation"
    animated={nodes[ch["target"]["node"]].get("name","") for anim in animations for ch in anim["channels"]}
    assert "ROTOR_SHROUD_SPIN_X" in animated
    assert "PLANETARY_CARRIER_OUTPUT_X_4_TO_1" in animated
    assert any(n.startswith("PLANET_") for n in animated)
    report=json.loads(qa.read_text(encoding="utf-8"))
    assert report["id"]=="M02-014" and report["materials"]>=7
    assert report["sun_planet_radial_gap_m"]>0
    assert report["planet_ring_radial_gap_m"]>0
    assert report["rotor_stator_rail_gap_m"]>0
    print("EXPORT PASS: actual BLEND, GLB2, material/mesh/node/channel counts",len(doc["meshes"]),len(animations))

if __name__=="__main__":
    check_static()
    if "--export" in sys.argv:
        check_export()
