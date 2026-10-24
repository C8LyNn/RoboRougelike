#!/usr/bin/env python3
"""Independent M03-012 QA: actual exported OBJ topology, critical clearances and repeatability."""
from collections import Counter
import hashlib
import json
import math
from pathlib import Path
import subprocess
import sys
import tempfile

HERE=Path(__file__).resolve().parent
GEN=HERE/"generate_m03_012.py"
STEM="M03-012_u_spring_bumper"

def read_obj(path):
    verts=[]
    meshes={}
    name=None
    for line in path.read_text(encoding="utf-8").splitlines():
        f=line.split()
        if not f:continue
        if f[0]=="o":
            name=f[1]
            assert name not in meshes
            meshes[name]={"indices":[],"faces":[],"material":None}
        elif f[0]=="usemtl":
            meshes[name]["material"]=f[1]
        elif f[0]=="v":
            assert name is not None
            v=tuple(map(float,f[1:]))
            assert len(v)==3 and all(math.isfinite(t) for t in v)
            verts.append(v)
            meshes[name]["indices"].append(len(verts)-1)
        elif f[0]=="f":
            tri=tuple(int(x)-1 for x in f[1:])
            assert len(tri)==3 and len(set(tri))==3
            meshes[name]["faces"].append(tri)
    return verts,meshes

def check_meshes(verts,meshes):
    total=0
    for name,m in meshes.items():
        own=set(m["indices"])
        assert len(own)>7
        edges=Counter()
        volume=0.
        for a,b,c in m["faces"]:
            assert a in own and b in own and c in own,name
            A,B,C=verts[a],verts[b],verts[c]
            U=[B[i]-A[i] for i in range(3)]
            V=[C[i]-A[i] for i in range(3)]
            n=(U[1]*V[2]-U[2]*V[1],
               U[2]*V[0]-U[0]*V[2],
               U[0]*V[1]-U[1]*V[0])
            assert sum(x*x for x in n)>1e-17,(name,"degenerate")
            volume+=sum(A[i]*n[i] for i in range(3))/6
            for i,j in ((a,b),(b,c),(c,a)):
                edges[(min(i,j),max(i,j))]+=1
        assert all(count==2 for count in edges.values()),(name,"nonmanifold")
        assert volume>1e-10,(name,"wrong normals",volume)
        total+=len(m["faces"])
    return total

def bounds(verts,mesh):
    v=[verts[i] for i in mesh["indices"]]
    return [(min(p[k] for p in v),max(p[k] for p in v)) for k in range(3)]

def test_critical_geometry(v,m):
    assert len(m)==21
    assert len({p["material"] for p in m.values()})==5
    u=bounds(v,m["Moving_U_Bumper"])
    assert abs(u[0][0]+.47)<1e-8 and abs(u[0][1]-.47)<1e-8
    assert abs(u[1][0]+.39)<1e-8 and abs(u[1][1]+.33)<1e-8
    assert abs(u[2][1]-.25)<1e-8
    for label,x in (("L",-.30),("R",.30)):
        guide=bounds(v,m["Fixed_Guide_Tube_"+label])
        rod=bounds(v,m["Moving_Slider_Rod_"+label])
        spring=bounds(v,m["Compression_Spring_"+label])
        assert abs(guide[1][0]+.18)<1e-8 and abs(guide[1][1]-.04)<1e-8
        assert abs(rod[1][0]+.34)<1e-8 and abs(rod[1][1]+.04)<1e-8
        # Radial checks on actual OBJ rings, not only generator constants.
        def radii(name):
            return [math.hypot(v[i][0]-x,v[i][2])
                    for i in m[name]["indices"]]
        guide_r=radii("Fixed_Guide_Tube_"+label)
        rod_r=radii("Moving_Slider_Rod_"+label)
        assert abs(min(guide_r)-.027)<1e-8
        assert abs(max(guide_r)-.045)<1e-8
        assert abs(max(rod_r)-.024)<1e-8
        assert min(guide_r)-max(rod_r)>=.003-1e-8
        assert abs(spring[1][0]+.33)<.006
        assert abs(spring[1][1]+.18)<.006
        # Mount ring bore is truly open and outside the crossmember.
        mount=bounds(v,m["Frame_Mount_Ring_"+label])
        assert abs(mount[1][0])<1e-8 and abs(mount[1][1]-.08)<1e-8
    positions=0
    for frame in range(1,62):
        phase=(frame-1)%30
        d=.06*(phase/15 if phase<=15 else (30-phase)/15)
        assert -.33+d<=-.27+1e-8
        assert -.18-(-.33+d)>=.09-1e-8
        assert .027-.024>=.003-1e-8
        assert (.15-d)/5>2*.0055+.005
        assert -.04+d<=.02+1e-8
        positions+=1
    return positions

def main():
    with tempfile.TemporaryDirectory(prefix="rrl-m03-012-") as temp:
        out=Path(temp)
        p=subprocess.run([sys.executable,str(GEN),"--out",str(out)],
            text=True,capture_output=True)
        if p.returncode:
            raise RuntimeError("Generator failure:\n"+p.stdout+"\n"+p.stderr)
        obj=out/(STEM+".obj")
        mtl=out/(STEM+".mtl")
        assert obj.stat().st_size>10000 and mtl.stat().st_size>200
        v,m=read_obj(obj)
        triangles=check_meshes(v,m)
        poses=test_critical_geometry(v,m)
        manifest=json.loads((out/"M03-012_manifest.json").read_text())
        assert manifest["id"]=="M03-012"
        assert manifest["parts"]==len(m)==21
        assert manifest["vertices"]==len(v)
        assert manifest["triangles"]==triangles
        assert manifest["materials"]==5
        assert len(manifest["snap_points"])==3
        assert manifest["animation"]["pose_samples"]==poses==61
        assert abs(manifest["animation"]["travel_m"]-.06)<1e-9
        assert manifest["blender"]=="NOT RUN" and manifest["unity"]=="NOT RUN"
        filenames=[STEM+".obj",STEM+".mtl","M03-012_manifest.json"]
        sha={n:hashlib.sha256((out/n).read_bytes()).hexdigest() for n in filenames}
        subprocess.run([sys.executable,str(GEN),"--out",str(out)],
                       check=True,text=True,capture_output=True)
        for n in filenames:
            assert hashlib.sha256((out/n).read_bytes()).hexdigest()==sha[n],n
        print(json.dumps({"id":"M03-012","status":"PASS",
             "real_obj_parts":len(m),"real_obj_vertices":len(v),
             "real_obj_triangles":triangles,"materials":5,
             "animated_pose_samples":poses,"manifold":"PASS",
             "regeneration":"BYTE_IDENTICAL","blender":"NOT RUN",
             "unity":"NOT RUN"},indent=2))

if __name__=="__main__":
    main()
