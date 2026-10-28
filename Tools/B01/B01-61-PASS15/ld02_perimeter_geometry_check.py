#!/usr/bin/env python3
"""B01-61 PASS15: ORIGINAL Codex MapWorld wall geometry × ORIGINAL B01-25 LD02 JSON.
This is real source/data execution in Python, NOT execution of Unity/PhysX/C#.
Reproduces the exact axis-aligned generated BoxCollider bounds for unscaled yaw0/yaw90
MapItemKind.Wall as expressed by the pinned original MapWorld.CreateItem/ApplyTransform.
"""
import argparse
import json
import pathlib
import re
import subprocess
import sys

CODEX="231005c6d0351945a8ee6684f3856af118f0e94f"
LD02="eecdc5ab9f20cad2f47dd7a139c9cb96981d0c3e"
PATH="Research/B01-25/P14/LD02_ARENA.json"
BLOBS={
"Assets/MapEditor/MapWorld.cs":"8928908db123b35dcab4b324fe9d3a7a8fceee4e",
"Assets/MapEditor/MapDraft.cs":"b5f8f395f93972c0ede000701dbbada8fcfcdfdb",
PATH:"6de0d04900a69606ba873f7c29cef75ed6f182cd",
}

def git(repo,*args):
    return subprocess.check_output(["git","-C",repo,*args],text=True).strip()
def original(repo,ref,path,blob):
    obj=git(repo,"rev-parse",f"{ref}:{path}")
    if obj!=blob:
        raise ValueError(f"ORIGINAL_BLOB_MISMATCH {path}: expected {blob}, actual {obj}")
    return git(repo,"show",f"{ref}:{path}")

def intersects(segment,bounds):
    # Robust 2D slab test for a point-center segment against the original wall AABB.
    (x0,z0),(x1,z1)=segment
    dx,dz=x1-x0,z1-z0
    tmin,tmax=0.,1.
    for origin,direction,lo,hi in ((x0,dx,bounds[0],bounds[1]),
                                   (z0,dz,bounds[2],bounds[3])):
        if abs(direction)<1e-10:
            if origin<lo or origin>hi:return False
        else:
            a,b=(lo-origin)/direction,(hi-origin)/direction
            if a>b:a,b=b,a
            tmin,tmax=max(tmin,a),min(tmax,b)
            if tmin>tmax:return False
    return True

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",default=".")
    p.add_argument("--require-closed",action="store_true")
    opt=p.parse_args()
    codex=original(opt.repo,CODEX,"Assets/MapEditor/MapWorld.cs",BLOBS["Assets/MapEditor/MapWorld.cs"])
    draft=original(opt.repo,CODEX,"Assets/MapEditor/MapDraft.cs",BLOBS["Assets/MapEditor/MapDraft.cs"])
    data=json.loads(original(opt.repo,LD02,PATH,BLOBS[PATH]))
    # Source-anchored: reject unknown changes instead of silently assuming collider dimensions.
    require=[
      ("MapWorld.Grid=4",r"\bconst float Grid=4f;"),
      ("original Wall cube",r"case MapItemKind\.Wall:position=new Vector3\(0,2,0\);scale=new Vector3\(Grid,4,\.5f\);"),
      ("original parent yaw",r"o\.transform\.rotation=Quaternion\.Euler\(0,item\.rotation\*90,0\);"),
      ("original parent item scale",r"o\.transform\.localScale=new Vector3\(item\.scaleX,item\.scaleY,item\.scaleZ\);"),
      ("original centered grid base",r"new Vector3\(item\.x\*Grid,0,item\.z\*Grid\)"),
      ("original physical BoxCollider",r"Primitive\(o,item\.kind==MapItemKind\.Pillar\?PrimitiveType\.Cylinder:PrimitiveType\.Cube"),
    ]
    for name,pattern in require:
        if not re.search(pattern,codex):
            raise ValueError(f"UNKNOWN_ORIGINAL_WORLD_SOURCE: {name}")
    if not re.search(r"enum MapItemKind\s*\{\s*Ground,\s*Wall,\s*Ramp,\s*Obstacle,\s*Spawn,\s*Finish",draft):
        raise ValueError("UNKNOWN_ORIGINAL_MAP_KIND_IDS")
    if (data["version"]!=2 or data["globalFloor"] is not False
        or len(data["items"])!=168):
        raise ValueError("UNKNOWN_ORIGINAL_LD02_DOCUMENT")
    walls=[i for i in data["items"] if i["kind"]==1]
    if len(walls)!=40:raise ValueError("UNKNOWN_ORIGINAL_WALL_COUNT")
    bounds=[]
    for w in walls:
        if (w["rotation"] not in (0,1) or
            any(w[k]!=1 for k in ("scaleX","scaleY","scaleZ"))):
            raise ValueError("UNSUPPORTED_WALL_TRANSFORM: do not approximate")
        x,z=w["x"]*4.,w["z"]*4.
        half_x,half_z=(2.,.25) if w["rotation"]==0 else (.25,2.)
        bounds.append((x-half_x,x+half_x,z-half_z,z+half_z))
    # Original author-side interior is x:0..44m, z:0..36m.
    # Axial 2m rays at corners are deliberately INSIDE the wall outline,
    # but go through the 1.75m gap between 0.25m side-wall extent
    # and the first 4m north/south wall with near edge at x=2m.
    checks={
      "WEST_MIDDLE":((1.,20.),(-1.,20.)),
      "EAST_MIDDLE":((43.,20.),(45.,20.)),
      "SOUTH_MIDDLE":((24.,1.),(24.,-1.)),
      "NORTH_MIDDLE":((24.,35.),(24.,37.)),
      "SOUTH_WEST_CORNER":((1.,1.),(1.,-1.)),
      "SOUTH_EAST_CORNER":((43.,1.),(43.,-1.)),
      "NORTH_WEST_CORNER":((1.,35.),(1.,37.)),
      "NORTH_EAST_CORNER":((43.,35.),(43.,37.)),
    }
    count_hit,count_hole=0,0
    for label,ray in checks.items():
        hits=sum(intersects(ray,b) for b in bounds)
        if label.endswith("MIDDLE"):
            correct=hits>=1
            count_hit+=int(correct)
            print(f'{"PASS" if correct else "FAIL"} | ORIGINAL_WALL_SIDE_CENTER={label} | wall_AABB_hits={hits}')
        else:
            gap=hits==0
            count_hole+=int(gap)
            print(f'{"EXPECTED_RED_GAP" if gap else "CLOSED"} | ORIGINAL_WALL_CORNER={label} | wall_AABB_hits={hits} | centerline_2m_ray={ray}')
    if count_hit!=4:raise ValueError("INCONSISTENT_ORIGINAL_WORLD_CALC_SIDE_POSITIVE")
    if count_hole!=4:raise ValueError("ORIGINAL_PERIMETER_DEFECT_NO_LONGER_REPRODUCES: REVIEW_CHANGED_DATA")
    print(f"ORIGINAL_GIT_SOURCE_AND_DATA=VERIFIED | CODEX={CODEX} | LD02={LD02}")
    print("SIDE_MIDDLE_HITS=4/4 | CORNER_CENTERLINE_GAPS=4/4 | MEASURED_CORNER_OPENING=1.75m")
    print("CLOSED_PERIMETER=RED_EXPECTED | UNITY_PHYSX=NOT_RUN | NATIVE_ESCAPE=UNMEASURED")
    if opt.require_closed:return 1
    return 0

if __name__=="__main__":
    try:sys.exit(main())
    except Exception as e:
        print(f"QA_HARNESS_FAIL={type(e).__name__}: {e}",file=sys.stderr)
        sys.exit(2)
