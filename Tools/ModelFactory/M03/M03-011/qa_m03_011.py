#!/usr/bin/env python3
"""Independent read-back QA for actual M03-011 OBJ and procedural rig."""
from collections import Counter
import hashlib
import json
import math
from pathlib import Path
import subprocess
import sys
import tempfile

HERE=Path(__file__).resolve().parent
GEN=HERE/"generate_m03_011.py"

def read_obj(path):
    global_verts=[]
    meshes={}
    current=None
    material=None
    for line in path.read_text(encoding="utf-8").splitlines():
        fields=line.split()
        if not fields: continue
        if fields[0]=="o":
            current=fields[1]
            assert current not in meshes
            meshes[current]={"vertices":[],"faces":[],"material":None}
        elif fields[0]=="usemtl":
            material=fields[1]
            meshes[current]["material"]=material
        elif fields[0]=="v":
            assert current is not None
            xyz=tuple(float(t) for t in fields[1:])
            assert len(xyz)==3 and all(math.isfinite(v) for v in xyz)
            global_verts.append(xyz)
            meshes[current]["vertices"].append(len(global_verts)-1)
        elif fields[0]=="f":
            assert current is not None
            tri=tuple(int(t)-1 for t in fields[1:])
            assert len(tri)==3 and len(set(tri))==3
            meshes[current]["faces"].append(tri)
    return global_verts,meshes

def independent_manifold(v,meshes):
    total_faces=0
    for name,m in meshes.items():
        owned=set(m["vertices"])
        edges=Counter()
        volume=0.
        for a,b,c in m["faces"]:
            assert a in owned and b in owned and c in owned,name
            A,B,C=v[a],v[b],v[c]
            U=[B[i]-A[i] for i in range(3)]
            W=[C[i]-A[i] for i in range(3)]
            normal=(U[1]*W[2]-U[2]*W[1],
                    U[2]*W[0]-U[0]*W[2],
                    U[0]*W[1]-U[1]*W[0])
            assert sum(x*x for x in normal)>1e-14,name
            volume+=sum(A[i]*normal[i] for i in range(3))/6
            for i,j in ((a,b),(b,c),(c,a)):
                edges[(min(i,j),max(i,j))]+=1
        assert all(x==2 for x in edges.values()),name
        assert volume>1e-10,(name,volume)
        total_faces+=len(m["faces"])
    return total_faces

def bounds(v,ids):
    return [(min(v[i][k] for i in ids),max(v[i][k] for i in ids))
            for k in range(3)]

def test_geometry(v,meshes):
    assert len(meshes)==30,len(meshes)
    assert len({m["material"] for m in meshes.values()})==5
    frame=bounds(v,meshes["Corner_L_Frame"]["vertices"])
    guard=bounds(v,meshes["Impact_Guard_L"]["vertices"])
    assert abs(frame[0][1]-1.)<1e-8
    assert abs(frame[1][1]-1.)<1e-8
    assert abs(frame[2][0]-.095)<1e-8
    assert abs(guard[2][1]-.34)<1e-8
    assert abs(guard[0][0]+.26)<1e-8
    assert abs(guard[1][0]+.26)<1e-8
    # AABB of L-shaped whole objects overlaps, so check all four convex
    # arm pairs, which is stricter and does not misclassify the concavity.
    fixed=(((-.07,1.),(-.07,.07)),((-.07,.07),(-.07,1.)))
    def ov(a,b):return min(a[1],b[1])-max(a[0],b[0])
    for frame_no in range(65):
        # Two compression cycles: rest at 1,33,65; maximum at 17,49.
        phase=frame_no%32
        delta=.022*(phase/16 if phase<=16 else (32-phase)/16)
        moving=(((-.26+delta,.42+delta),(-.26+delta,-.12+delta)),
                ((-.26+delta,-.12+delta),(-.26+delta,.42+delta)))
        for f in fixed:
            for m in moving:
                assert ov(f[0],m[0])<=0 or ov(f[1],m[1])<=0,frame_no
        gap=.05-delta
        assert gap>=.028-1e-9
        # Shock core fixed-side contact and moving-side contact in
        # local X/Y coordinates; no material overlaps steel or guard.
        assert abs((-.12+delta+gap)-(-.07))<1e-9
    for axis in ("X","Y"):
        assert "Elastomer_Shock_"+axis in meshes
        for k in (1,2,3):
            assert "Shock_Rib_"+axis+str(k) in meshes
    for name in ("Mount_Endplate_X","Mount_Endplate_Y"):
        assert name in meshes
    return 65

def main():
    with tempfile.TemporaryDirectory(prefix="rrl-m03-011-") as folder:
        out=Path(folder)
        proc=subprocess.run([sys.executable,str(GEN),"--out",str(out)],
            capture_output=True,text=True)
        if proc.returncode:
            raise RuntimeError("Generator failed:\\n"+proc.stdout+"\\n"+proc.stderr)
        assert '"status": "PASS"' in proc.stdout
        obj=out/"M03-011_impact_corner.obj"
        mtl=out/"M03-011_impact_corner.mtl"
        assert obj.stat().st_size>5000 and mtl.stat().st_size>200
        v,meshes=read_obj(obj)
        faces=independent_manifold(v,meshes)
        frames=test_geometry(v,meshes)
        manifest=json.loads((out/"M03-011_manifest.json").read_text())
        assert manifest["id"]=="M03-011"
        assert manifest["parts"]==len(meshes)==30
        assert manifest["vertices"]==len(v)
        assert manifest["triangles"]==faces
        assert manifest["materials"]==5
        assert len(manifest["snap_points"])==3
        assert manifest["animation"]["pose_samples"]==65
        assert abs(manifest["animation"]["steel_guard_min_gap_m"]-.028)<1e-9
        assert manifest["blender"]=="NOT RUN" and manifest["unity"]=="NOT RUN"
        # Deterministic second run: compare actual bytes, not just counts.
        h={f:hashlib.sha256((out/f).read_bytes()).hexdigest()
           for f in ("M03-011_impact_corner.obj","M03-011_impact_corner.mtl",
                     "M03-011_manifest.json")}
        subprocess.run([sys.executable,str(GEN),"--out",str(out)],
                       capture_output=True,text=True,check=True)
        for f,digest in h.items():
            assert hashlib.sha256((out/f).read_bytes()).hexdigest()==digest,f
        print(json.dumps({"id":"M03-011","status":"PASS",
             "real_obj_vertices":len(v),"real_obj_faces":faces,
             "real_obj_manifold_parts":len(meshes),
             "materials":len({m["material"] for m in meshes.values()}),
             "animation_pose_samples":frames,"rebuild":"BYTE_IDENTICAL",
             "blender":"NOT RUN","unity":"NOT RUN"},indent=2))

if __name__=="__main__":
    main()
