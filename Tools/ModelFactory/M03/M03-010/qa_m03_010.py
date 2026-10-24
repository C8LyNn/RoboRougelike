#!/usr/bin/env python3
"""Independent, read-only M03-010 QA on actual exported OBJ/MTL bytes.

Regenerates twice, verifies each closed outward-wound mesh, no positive
volumetric AABB overlap, real stepped depth, ceramic/lip flush contact,
four physically open bores and four load paths into the backplate.
No bpy, Unity or third-party dependency.
"""
from __future__ import annotations
from collections import Counter
import hashlib
import json
import math
from pathlib import Path
import subprocess
import sys
import tempfile

HERE=Path(__file__).resolve().parent
GEN=HERE/"generate_m03_010.py"
STEM="M03-010_stepped_segment_armor"
DEPTHS=(.076,.088,.100,.112,.124)


def sub(a,b):
    return tuple(x-y for x,y in zip(a,b))


def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])


def dot(a,b):
    return sum(x*y for x,y in zip(a,b))


def read_obj(path):
    all_vertices=[]
    parts={}
    name=None
    mtllib=None
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("mtllib "):
            mtllib=line[7:]
        elif line.startswith("o "):
            name=line[2:]
            assert name not in parts
            parts[name]={"vertices":[],"faces":[],"material":None}
        elif line.startswith("usemtl "):
            assert name is not None
            parts[name]["material"]=line[7:]
        elif line.startswith("v "):
            assert name is not None
            coords=tuple(map(float,line.split()[1:]))
            assert len(coords)==3 and all(math.isfinite(v) for v in coords)
            all_vertices.append(coords)
            parts[name]["vertices"].append(len(all_vertices)-1)
        elif line.startswith("f "):
            assert name is not None
            tri=tuple(int(x)-1 for x in line.split()[1:])
            assert len(tri)==3 and len(set(tri))==3
            assert all(i in parts[name]["vertices"] for i in tri)
            parts[name]["faces"].append(tri)
    return mtllib,all_vertices,parts


def bounds(vertices,indices):
    return [(min(vertices[i][k] for i in indices),
             max(vertices[i][k] for i in indices)) for k in range(3)]


def inspect(obj,mtl,manifest):
    mtllib,vertices,parts=read_obj(obj)
    assert mtllib==mtl.name
    mat_names={line[7:] for line in mtl.read_text().splitlines()
               if line.startswith("newmtl ")}
    assert len(mat_names)==6
    assert {p["material"] for p in parts.values()}==mat_names
    assert len(parts)==manifest["parts"]==32
    assert len(vertices)==manifest["vertices"]
    assert sum(len(p["faces"]) for p in parts.values())==manifest["triangles"]
    bb={}
    for name,part in parts.items():
        directed=Counter()
        volume=0.
        for a,b,c in part["faces"]:
            A,B,C=vertices[a],vertices[b],vertices[c]
            normal=cross(sub(B,A),sub(C,A))
            assert dot(normal,normal)>1e-17,(name,"degenerate")
            volume+=dot(A,cross(B,C))/6
            for i,j in ((a,b),(b,c),(c,a)):
                directed[(i,j)]+=1
        assert all(v==1 and directed.get((j,i))==1
                   for (i,j),v in directed.items()),(name,"nonmanifold")
        assert volume>1e-12,(name,"inverted")
        bb[name]=bounds(vertices,part["vertices"])
    pairs=0
    names=list(parts)
    for idx,name in enumerate(names):
        for other in names[idx+1:]:
            a,b=bb[name],bb[other]
            overlap=[min(a[k][1],b[k][1])-max(a[k][0],b[k][0])
                     for k in range(3)]
            assert min(overlap)<=1e-9,(name,other,"penetration",overlap)
            pairs+=1
    assert pairs==496
    backer=bb["Graphite_Backplate"]
    assert abs(backer[1][0]-.030)<1e-8
    assert abs(backer[1][1]-.050)<1e-8
    for idx,depth in enumerate(DEPTHS):
        tag="%02d"%(idx+1)
        step=bb["Step_Riser_"+tag]
        tile=bb["Ceramic_Overlay_"+tag]
        lip=bb["Copper_Step_Lip_"+tag]
        assert abs(step[1][0]-.050)<1e-8
        assert abs(step[1][1]-depth)<1e-8
        assert abs(tile[1][0]-depth)<1e-8
        assert abs(tile[1][1]-depth-.013)<1e-8
        assert abs(lip[1][0]-depth)<1e-8
        assert abs(lip[1][1]-depth-.007)<1e-8
        assert tile[2][0]-lip[2][1]>=.0109
        assert len(parts["Step_Riser_"+tag]["vertices"])==16
        assert len(parts["Step_Riser_"+tag]["faces"])==28
    for side,sx in (("L",-1),("R",1)):
        rail=bb["Side_Rail_"+side]
        assert abs(rail[1][0]-.050)<1e-8
        for idx in range(3):
            warning=bb["Amber_Rail_Marker_"+side+"_"+str(idx)]
            assert abs(warning[1][0]-rail[1][1])<1e-8
        for sz,vertical in ((-1,"B"),(1,"T")):
            tag=side+"_"+vertical
            ring=parts["Mount_Bore_"+tag]
            ringbox=bb["Mount_Bore_"+tag]
            bridge=bb["Mount_Bridge_"+tag]
            assert abs(ringbox[1][0])<1e-8
            assert abs(ringbox[1][1]-.030)<1e-8
            assert abs(bridge[1][0]-.030)<1e-8
            assert abs(bridge[1][1]-.050)<1e-8
            assert abs(bridge[0][0 if sx==1 else 1]-
                       backer[0][1 if sx==1 else 0])<1e-8
            assert (min(ringbox[0][1],bridge[0][1])-
                    max(ringbox[0][0],bridge[0][0]))>.010
            radial=[math.hypot(vertices[i][0]-sx*.530,
                               vertices[i][2]-sz*.280)
                    for i in ring["vertices"]]
            assert abs(min(radial)-.020)<1e-8
            assert abs(max(radial)-.038)<1e-8
            # The bridge never extends into the bore's 20mm radius.
            assert min(abs(bridge[0][0]-sx*.530),
                       abs(bridge[0][1]-sx*.530))>=.025-1e-8
    assert manifest["segments"]==5
    assert manifest["ceramic_overlays"]==5
    assert manifest["copper_lips"]==5
    assert manifest["bored_mounts"]==4
    assert manifest["snap_points"]==4
    assert manifest["no_volume_overlap_pair_checks"]==496
    assert manifest["animation"]=="NOT REQUIRED"
    return {"status":"PASS","id":"M03-010","meshes":len(parts),
            "vertices":len(vertices),"triangles":sum(len(p["faces"])
            for p in parts.values()),"materials":len(mat_names),
            "true_stepped_tiers":5,"open_mount_bores":4,
            "aabb_nonpenetration_pairs":pairs,"manifold":"PASS",
            "blender":"NOT RUN","unity":"NOT RUN"}


def main():
    subprocess.run([sys.executable,"-m","py_compile",str(GEN)],check=True)
    result=subprocess.run([sys.executable,str(GEN),"--self-test"],
                          check=True,capture_output=True,text=True)
    assert json.loads(result.stdout)["result"]=="PASS"
    with tempfile.TemporaryDirectory(prefix="m03-010-qa-") as tmp:
        dirs=(Path(tmp)/"one",Path(tmp)/"two")
        for out in dirs:
            subprocess.run([sys.executable,str(GEN),"--static-obj",
                            "--out",str(out)],check=True,
                           capture_output=True,text=True)
        for ext in (".obj",".mtl","_manifest.json"):
            a=(dirs[0]/(STEM+ext)).read_bytes()
            b=(dirs[1]/(STEM+ext)).read_bytes()
            assert hashlib.sha256(a).digest()==hashlib.sha256(b).digest()
        manifest=json.loads((dirs[0]/(STEM+"_manifest.json")).read_text())
        assert manifest["backend"]=="python-stdlib"
        assert manifest["blender"]=="NOT RUN" and manifest["unity"]=="NOT RUN"
        qa=inspect(dirs[0]/(STEM+".obj"),dirs[0]/(STEM+".mtl"),manifest)
    qa["regeneration"]="BYTE_IDENTICAL"
    print(json.dumps(qa,indent=2))


if __name__=="__main__":
    main()
