#!/usr/bin/env python3
"""Independent verification of M01-009 generated static and animated assets.

Uses only the standard library. OBJ geometry is parsed from the exported
file, not from generator structures. GLB validation checks the actual
binary JSON chunk, material count, animated node channels and mesh count.
"""
from collections import Counter
import json
import math
from pathlib import Path
import struct
import sys

ID="M01-009"
STEM="M01-009_tri_segment_rocket_launcher"


def parse_obj(path):
    vertices=[]
    objects={}
    current=None
    material=None
    for line in path.read_text().splitlines():
        if line.startswith("o "):
            current=line[2:]
            assert current not in objects
            objects[current]={"vertices":[],"faces":[],"material":None}
        elif line.startswith("usemtl "):
            material=line[7:]
            objects[current]["material"]=material
        elif line.startswith("v "):
            v=tuple(float(x) for x in line.split()[1:])
            assert len(v)==3 and all(math.isfinite(c) for c in v)
            vertices.append(v)
            objects[current]["vertices"].append(len(vertices)-1)
        elif line.startswith("f "):
            face=tuple(int(x.split("/")[0])-1 for x in line.split()[1:])
            assert len(face)==3 and len(set(face))==3
            assert all(0<=i<len(vertices) for i in face)
            objects[current]["faces"].append(face)
    assert vertices and objects
    return vertices,objects


def bbox(vertices,indices):
    return [(min(vertices[i][a] for i in indices),
             max(vertices[i][a] for i in indices)) for a in range(3)]


def signed_volume(vertices,faces):
    volume=0.
    directed=Counter()
    for i,j,k in faces:
        a,b,c=vertices[i],vertices[j],vertices[k]
        ux,uy,uz=[b[t]-a[t] for t in range(3)]
        vx,vy,vz=[c[t]-a[t] for t in range(3)]
        n=(uy*vz-uz*vy,uz*vx-ux*vz,ux*vy-uy*vx)
        assert sum(x*x for x in n)>1e-16
        volume+=sum(a[t]*n[t] for t in range(3))/6
        for edge in ((i,j),(j,k),(k,i)):
            directed[edge]+=1
    assert volume>1e-9,volume
    assert all(count==1 and directed[(j,i)]==1
               for (i,j),count in directed.items())
    return volume


def inspect_glb(path):
    data=path.read_bytes()
    assert len(data)>1024
    magic,version,length=struct.unpack_from("<4sII",data,0)
    assert magic==b"glTF" and version==2 and length==len(data)
    chunk_size,kind=struct.unpack_from("<I4s",data,12)
    assert kind==b"JSON"
    gltf=json.loads(data[20:20+chunk_size])
    assert len(gltf.get("materials",[]))>=8
    assert len(gltf.get("meshes",[]))>=75
    assert len(gltf.get("animations",[]))>=1
    nodes=gltf["nodes"]
    targets=set()
    channels=0
    for animation in gltf["animations"]:
        for channel in animation["channels"]:
            node=nodes[channel["target"]["node"]]["name"]
            if channel["target"]["path"]=="rotation" and "LID_" in node:
                targets.add(node)
                channels+=1
    assert all(any(group in name for name in targets)
               for group in ("LID_A","LID_B","LID_C")),targets
    return dict(meshes=len(gltf["meshes"]),
                materials=len(gltf["materials"]),
                animations=len(gltf["animations"]),
                lid_rotation_channels=channels,
                animated_nodes=sorted(targets))


def verify(folder):
    vertices,objects=parse_obj(folder/(STEM+".obj"))
    report=json.loads((folder/(ID+"_qa.json")).read_text())
    assert report["status"]=="PASS"
    assert report["mount_pivot_m"]==[0,0,0]
    assert report["muzzle_axis"]=="+Z"
    assert report["moving_hatches"]==3
    assert report["poses_checked"]==129
    assert report["materials"]==8
    assert len(objects)==report["parts"]
    assert len(vertices)==report["vertices"]
    assert sum(len(o["faces"]) for o in objects.values())==report["triangles"]
    assert len({o["material"] for o in objects.values()})==8
    for name,obj in objects.items():
        assert obj["vertices"] and obj["faces"],name
        signed_volume(vertices,obj["faces"])
    for label,x in zip("ABC",(-.44,0.,.44)):
        for prefix in ("Square_Port_","Inner_Ceramic_Lip_",
                       "Opening_Hatch_","Hinge_Pin_","Rocket_Casing_"):
            assert prefix+label in objects
        frame=bbox(vertices,objects["Square_Port_"+label]["vertices"])
        door=bbox(vertices,objects["Opening_Hatch_"+label]["vertices"])
        assert frame[2][1]<door[2][0],(label,"door intersects square port")
        assert abs((door[0][0]+door[0][1])/2-x)<1e-7
    mtl=(folder/(STEM+".mtl")).read_text()
    assert mtl.count("newmtl ")==8
    if report["blender"]=="EXPORTED":
        for ext in (".blend","_animated.glb","_animated.fbx"):
            assert (folder/(STEM+ext)).stat().st_size>1024
        glb=inspect_glb(folder/(STEM+"_animated.glb"))
    else:
        assert report["blender"]=="NOT RUN"
        glb="NOT RUN"
    return {"model":ID,"status":"PASS","objects":len(objects),
            "vertices":len(vertices),"triangles":report["triangles"],
            "materials":8,"manifold":"PASS","actual_obj":"PASS",
            "animation_pose_samples":129,"blender":report["blender"],
            "glb":glb,"unity":"NOT RUN"}


if __name__=="__main__":
    print(json.dumps(verify(Path(sys.argv[1])),indent=2))
