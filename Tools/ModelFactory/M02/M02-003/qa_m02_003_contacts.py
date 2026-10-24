#!/usr/bin/env python3
"""Independent geometry and repeatability QA for the M02-003 V2 sand wheel.

No Blender, Unity or third-party modules required. This validates actual
checked-in OBJ vertices/faces and compares them with a newly generated asset.
"""
from collections import Counter
import hashlib
import json
import math
from pathlib import Path
import subprocess
import sys
import tempfile

HERE=Path(__file__).resolve().parent
STEM="M02-003_balloon_sand_wheel"
SOURCE=HERE/"generate_m02_003.py"


def load_obj(path):
    verts=[]
    objects={}
    name=None
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("o "):
            name=line[2:]
            assert name not in objects,name
            objects[name]={"vertices":[],"faces":[],"materials":set()}
        elif line.startswith("v "):
            assert name is not None
            coords=tuple(map(float,line.split()[1:]))
            assert len(coords)==3 and all(math.isfinite(v) for v in coords)
            verts.append(coords)
            objects[name]["vertices"].append(len(verts)-1)
        elif line.startswith("usemtl "):
            objects[name]["materials"].add(line.split()[1])
        elif line.startswith("f "):
            tri=tuple(int(i)-1 for i in line.split()[1:])
            assert len(tri)==3 and len(set(tri))==3
            assert all(0<=i<len(verts) for i in tri)
            objects[name]["faces"].append(tri)
    return verts,objects


def bounds(verts,indices,axis):
    values=[verts[i][axis] for i in indices]
    return min(values),max(values)


def validate_obj(obj_path):
    verts,objects=load_obj(obj_path)
    assert len(objects)==47 and len(verts)==5000
    assert sum(len(p["faces"]) for p in objects.values())==9840
    rim=objects["Alloy_Bead_Rim"]
    rim_bounds=bounds(verts,rim["vertices"],0)
    assert abs(rim_bounds[0]+.301)<1e-8
    assert abs(rim_bounds[1]-.301)<1e-8
    contacts={}
    for side in ("L","R"):
        ring=objects["Bead_Trim_"+side]
        low,high=bounds(verts,ring["vertices"],0)
        seat=min(rim_bounds[1],high)-max(rim_bounds[0],low)
        exposed=(rim_bounds[0]-low) if side=="L" else (high-rim_bounds[1])
        assert abs(seat-.004)<1e-7,(side,"missing rim engagement",seat)
        assert abs(exposed-.002)<1e-7,(side,"hidden trim",exposed)
        radius=[math.hypot(verts[i][1],verts[i][2]) for i in ring["vertices"]]
        assert abs(min(radius)-.231)<1e-7
        assert abs(max(radius)-.242)<1e-7
        assert .245-max(radius)>=.003-1e-7
        contacts[side]={"axial_seat_m":round(seat,6),
                        "visible_outset_m":round(exposed,6)}
    # Per-part closed manifold and correct edge winding, independent from
    # the procedural generator's own self-test.
    for name,obj in objects.items():
        edge_use=Counter()
        signed_volume=0.
        owned=set(obj["vertices"])
        for a,b,c in obj["faces"]:
            assert a in owned and b in owned and c in owned,name
            A,B,C=verts[a],verts[b],verts[c]
            u=[B[k]-A[k] for k in range(3)]
            v=[C[k]-A[k] for k in range(3)]
            n=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],
               u[0]*v[1]-u[1]*v[0])
            assert sum(x*x for x in n)>1e-13,(name,"degenerate")
            signed_volume+=(
                A[0]*(B[1]*C[2]-B[2]*C[1])+
                A[1]*(B[2]*C[0]-B[0]*C[2])+
                A[2]*(B[0]*C[1]-B[1]*C[0]))/6
            for a1,b1 in ((a,b),(b,c),(c,a)):
                edge_use[(min(a1,b1),max(a1,b1))]+=1
        assert all(count==2 for count in edge_use.values()),name
        assert signed_volume>1e-8,(name,"inward winding or zero volume",signed_volume)
    return {"objects":len(objects),"vertices":len(verts),
            "triangles":sum(len(o["faces"]) for o in objects.values()),
            "seated_trims":contacts,"manifold":"PASS"}


def main():
    original=HERE/"generated"
    result=validate_obj(original/(STEM+".obj"))
    manifest=json.loads((original/"M02-003_manifest.json").read_text())
    assert manifest["geometry_revision"]=="M02-003_V2_SEATED_BEAD_TRIMS"
    assert manifest["bead_trim_seated_count"]==2
    assert abs(manifest["bead_trim_axial_seat_m"]-.004)<1e-9
    assert abs(manifest["bead_trim_visible_axial_m"]-.002)<1e-9
    assert manifest["parts"]==47 and manifest["vertices"]==5000
    assert manifest["triangles"]==9840
    assert manifest["blender"].startswith("NOT RUN") and manifest["unity"]=="NOT RUN"
    with tempfile.TemporaryDirectory(prefix="m02-003-fix-") as tmp:
        output=Path(tmp)
        subprocess.run([sys.executable,str(SOURCE),"--out",str(output)],
                       check=True,capture_output=True,text=True)
        for filename in (STEM+".obj",STEM+".mtl"):
            expected=(original/filename).read_bytes()
            actual=(output/filename).read_bytes()
            assert hashlib.sha256(expected).digest()==hashlib.sha256(actual).digest(),filename
        generated=json.loads((output/"M02-003_manifest.json").read_text())
        for key in ("geometry_revision","bead_trim_seated_count",
                    "bead_trim_axial_seat_m","bead_trim_visible_axial_m"):
            assert generated[key]==manifest[key],key
    print(json.dumps({"model":"M02-003","status":"PASS",
                      "geometry_revision":"M02-003_V2_SEATED_BEAD_TRIMS",
                      "obj_qa":result,"rebuild_obj_mtl":"BYTE_IDENTICAL",
                      "blender":"NOT RUN","unity":"NOT RUN"},indent=2))


if __name__=="__main__":
    main()
