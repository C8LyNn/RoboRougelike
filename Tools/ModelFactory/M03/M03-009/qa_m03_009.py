#!/usr/bin/env python3
"""Independent read-only QA of actual M03-009 exported OBJ/MTL.

No bpy, Unity or third-party Python dependency. Regenerates twice and
inspects mesh manifoldness, normals, true holes, clearances and 4 mounts.
"""
from __future__ import annotations
from collections import Counter
import json
import math
from pathlib import Path
import subprocess
import sys
import tempfile

HERE=Path(__file__).resolve().parent
GEN=HERE/"generate_m03_009.py"
STEM="M03-009_perforated_side_armor"


def dot(a,b):
    return sum(x*y for x,y in zip(a,b))


def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])


def sub(a,b):
    return tuple(x-y for x,y in zip(a,b))


def inspect(obj,mtl,manifest):
    verts=[]
    objects={}
    current=None
    materials=set()
    for line in obj.read_text(encoding="utf-8").splitlines():
        if line.startswith("mtllib "):
            assert line[7:]==mtl.name
        elif line.startswith("o "):
            current=line[2:]
            assert current not in objects
            objects[current]=dict(vertices=[],faces=[],material=None)
        elif line.startswith("usemtl "):
            assert current is not None
            objects[current]["material"]=line[7:]
            materials.add(line[7:])
        elif line.startswith("v "):
            assert current is not None
            v=tuple(map(float,line.split()[1:]))
            assert len(v)==3 and all(math.isfinite(x) for x in v)
            verts.append(v)
            objects[current]["vertices"].append(len(verts)-1)
        elif line.startswith("f "):
            assert current is not None
            f=tuple(int(i)-1 for i in line.split()[1:])
            assert len(f)==3 and len(set(f))==3
            assert all(i in objects[current]["vertices"] for i in f)
            objects[current]["faces"].append(f)
    declared={line[7:] for line in mtl.read_text().splitlines()
              if line.startswith("newmtl ")}
    assert materials==declared and len(materials)==5
    assert len(objects)==manifest["parts"]==26
    assert len(verts)==manifest["vertices"]
    assert sum(len(o["faces"]) for o in objects.values())==manifest["triangles"]
    for name,obj in objects.items():
        directed=Counter()
        volume=0.
        for f in obj["faces"]:
            a,b,c=(verts[i] for i in f)
            n=cross(sub(b,a),sub(c,a))
            assert dot(n,n)>1e-17,(name,"degenerate")
            volume+=dot(a,cross(b,c))/6
            for i,j in ((f[0],f[1]),(f[1],f[2]),(f[2],f[0])):
                directed[(i,j)]+=1
        assert all(count==1 and directed.get((j,i))==1
                   for (i,j),count in directed.items()),(name,"non-manifold")
        assert volume>1e-12,(name,"inverted",volume)
    # Independent broad-phase collision check on the exported OBJ,
    # including both perforated sheets, frame, fasteners and lugs.
    bounds={}
    for name,item in objects.items():
        vv=[verts[i] for i in item["vertices"]]
        bounds[name]=[(min(v[k] for v in vv),max(v[k] for v in vv))
                      for k in range(3)]
    names=list(bounds)
    collisions_checked=0
    for index,name in enumerate(names):
        for other in names[index+1:]:
            a,b=bounds[name],bounds[other]
            overlap=[min(a[k][1],b[k][1])-max(a[k][0],b[k][0])
                     for k in range(3)]
            assert min(overlap)<=1e-8,(name,other,"AABB penetration",overlap)
            collisions_checked+=1
    assert collisions_checked==325
    assert manifest["no_volume_overlap_pair_checks"]==325
    assert manifest["outer_real_through_holes"]==24
    assert manifest["inner_real_through_holes"]==96
    assert manifest["through_bored_mounts"]==4
    assert manifest["snap_points"]==4
    assert abs(manifest["sheet_air_gap_m"]-.009)<1e-8
    # Confirm that each perforation is physically a through-hole,
    # not a colored surface or a disconnected decorative plane.
    # Both lattice meshes must have exactly the expected 4 inner-wall
    # quads (8 triangles) per hole and an actual 4-wall perimeter.
    for name,nx,nz,halfx,halfz,openx,openz,ylo,yhi in (
        ("Outer_Steel_24_Through_Holes",6,4,.390,.280,.082,.090,.080,.110),
        ("Inner_Mesh_96_Through_Holes",12,8,.390,.280,.041,.042,.055,.071)):
        p=objects[name]
        assert len(p["faces"])==24*nx*nz+4*(nx+nz)
        expected_y={round(ylo,9),round(yhi,9)}
        assert {round(verts[i][1],9) for i in p["vertices"]}==expected_y
        # No front/back triangles are allowed to cover the center of a
        # nominal opening. The centre is tested in every hole.
        for iz in range(nz):
            for ix in range(nx):
                cx=-halfx+(ix+.5)*2*halfx/nx
                cz=-halfz+(iz+.5)*2*halfz/nz
                for tri in p["faces"]:
                    a,b,c=[verts[i] for i in tri]
                    if not (abs(a[1]-b[1])<1e-8 and
                            abs(b[1]-c[1])<1e-8):
                        continue
                    # Barycentric test in XZ: face cannot fill the hole.
                    area=(b[0]-a[0])*(c[2]-a[2])-(b[2]-a[2])*(c[0]-a[0])
                    if abs(area)<1e-15:
                        continue
                    u=((cx-a[0])*(c[2]-a[2])-(cz-a[2])*(c[0]-a[0]))/area
                    v=((b[0]-a[0])*(cz-a[2])-(b[2]-a[2])*(cx-a[0]))/area
                    assert not (u>=-1e-8 and v>=-1e-8 and u+v<=1+1e-8), (
                        name,ix,iz,"fake filled hole")
    for sx in (-1,1):
        for sz in (-1,1):
            name="Mount_Bore_"+("L" if sx<0 else "R")+(
                "_B" if sz<0 else "_T")
            mount=objects[name]
            radial=[math.hypot(verts[i][0]-sx*.518,
                               verts[i][2]-sz*.315)
                    for i in mount["vertices"]]
            assert abs(min(radial)-.019)<1e-7
            assert abs(max(radial)-.043)<1e-7
            assert {round(verts[i][1],8) for i in mount["vertices"]}=={0.,.035}
            assert len(mount["faces"])==256
            # No other part intrudes into the 19mm through-bore.
            for othername,other in objects.items():
                if othername==name:
                    continue
                # Compare bounding boxes first; all 4 bores are outside
                # frame and lattice, and bridges begin at y=.035.
                vv=[verts[i] for i in other["vertices"]]
                if not (min(v[1] for v in vv)<.035-1e-8 and
                        max(v[1] for v in vv)>0+1e-8):
                    continue
                assert not any(math.hypot(v[0]-sx*.518,v[2]-sz*.315)<.019-1e-8
                               for v in vv),(name,othername,"bore blocked")
    return dict(status="PASS",meshes=len(objects),materials=len(materials),
                vertices=len(verts),
                triangles=sum(len(o["faces"]) for o in objects.values()),
                outer_real_holes=24,backing_real_holes=96,
                mount_through_bores=4,manifold="PASS",
                aabb_nonpenetration_pairs=collisions_checked,
                blender="NOT RUN",unity="NOT RUN")


def main():
    subprocess.run([sys.executable,"-m","py_compile",str(GEN)],check=True)
    test=subprocess.run([sys.executable,str(GEN),"--self-test"],
                        check=True,capture_output=True,text=True)
    assert json.loads(test.stdout)["result"]=="PASS"
    with tempfile.TemporaryDirectory(prefix="m03-009-qa-") as d:
        a,b=Path(d)/"a",Path(d)/"b"
        for out in (a,b):
            subprocess.run([sys.executable,str(GEN),"--static-obj",
                            "--out",str(out)],check=True,
                           capture_output=True,text=True)
        for ext in (".obj",".mtl","_manifest.json"):
            assert (a/(STEM+ext)).read_bytes()==(b/(STEM+ext)).read_bytes()
        report=json.loads((a/(STEM+"_manifest.json")).read_text())
        assert report["backend"]=="python-stdlib"
        result=inspect(a/(STEM+".obj"),a/(STEM+".mtl"),report)
    print(json.dumps(result,indent=2))


if __name__=="__main__":
    main()
