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
FIX_REF="f6d6886f45b1766f9b7a9c2975e1bd1deb3fdb84"
FIX_BLOB="e39e2f174984832bd97311c050e3c22fd6fb1175"
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
    p.add_argument("--owner-fix",action="store_true",help="Use exact immutable B01-25 new repaired map SHA")
    opt=p.parse_args()
    codex=original(opt.repo,CODEX,"Assets/MapEditor/MapWorld.cs",BLOBS["Assets/MapEditor/MapWorld.cs"])
    draft=original(opt.repo,CODEX,"Assets/MapEditor/MapDraft.cs",BLOBS["Assets/MapEditor/MapDraft.cs"])
    data_ref,data_blob=(FIX_REF,FIX_BLOB) if opt.owner_fix else (LD02,BLOBS[PATH])
    data=json.loads(original(opt.repo,data_ref,PATH,data_blob))
    # Source-anchored: reject unknown changes instead of silently assuming collider dimensions.
    require=[
      ("MapWorld.Grid=4",r"\bconst float Grid=4f;"),
      ("original Wall cube",r"case MapItemKind\.Wall:position=new Vector3\(0,2,0\);scale=new Vector3\(Grid,4,\.5f\);"),
      ("original Platform cube",r"case MapItemKind\.Platform:position=new Vector3\(0,\.5f,0\);scale=new Vector3\(4,1,4\);"),
      ("original parent yaw",r"o\.transform\.rotation=Quaternion\.Euler\(0,item\.rotation\*90,0\);"),
      ("original parent item scale",r"o\.transform\.localScale=new Vector3\(item\.scaleX,item\.scaleY,item\.scaleZ\);"),
      ("original centered grid base",r"new Vector3\(item\.x\*Grid,0,item\.z\*Grid\)"),
      ("original physical BoxCollider",r"Primitive\(o,item\.kind==MapItemKind\.Pillar\?PrimitiveType\.Cylinder:PrimitiveType\.Cube"),
    ]
    for name,pattern in require:
        if not re.search(pattern,codex):
            raise ValueError(f"UNKNOWN_ORIGINAL_WORLD_SOURCE: {name}")
    if not re.search(r"enum MapItemKind\s*\{\s*Ground,\s*Wall,\s*Ramp,\s*Obstacle,\s*Spawn,\s*Finish,\s*Platform",draft):
        raise ValueError("UNKNOWN_ORIGINAL_MAP_KIND_IDS")
    if (data["version"]!=2 or data["globalFloor"] is not False
        or len(data["items"])!=168):
        raise ValueError("UNKNOWN_ORIGINAL_LD02_DOCUMENT")
    walls=[i for i in data["items"] if i["kind"]==1]
    platforms=[i for i in data["items"] if i["kind"]==6]
    if len(walls)!=(36 if opt.owner_fix else 40):
        raise ValueError("UNEXPECTED_WALL_COUNT_FOR_PINNED_DATA")
    if len(platforms)!=(4 if opt.owner_fix else 0):
        raise ValueError("UNEXPECTED_PLATFORM_COUNT_FOR_PINNED_DATA")
    if opt.owner_fix:
        expected_corners={(0,0),(11,0),(0,9),(11,9)}
        if {(p["x"],p["z"]) for p in platforms}!=expected_corners:
            raise ValueError("WRONG_ORIGINAL_OWNER_REPAIRED_CORNER_POSITIONS")
    bounds=[]
    for w in walls+platforms:
        if w["kind"]==1:
            if (w["rotation"] not in (0,1) or
                any(w[k]!=1 for k in ("scaleX","scaleY","scaleZ"))):
                raise ValueError("UNSUPPORTED_WALL_TRANSFORM: do not approximate")
            half_x,half_z=(2.,.25) if w["rotation"]==0 else (.25,2.)
        else:
            if (w["rotation"]!=0 or (w["scaleX"],w["scaleY"],w["scaleZ"])!=(1,4,1)):
                raise ValueError("UNKNOWN_CORNER_PLATFORM_GEOMETRY")
            # Original Platform child Cube 4 x 1 x 4, parent scale=(1,4,1):
            # actual y range [0,4] contains our 1.5m horizontal ray height.
            half_x,half_z=2.,2.
        x,z=w["x"]*4.,w["z"]*4.
        bounds.append((w["kind"],(x-half_x,x+half_x,z-half_z,z+half_z)))
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
        wallhits=sum(intersects(ray,b) for kind,b in bounds if kind==1)
        platformhits=sum(intersects(ray,b) for kind,b in bounds if kind==6)
        hits=wallhits+platformhits
        if label.endswith("MIDDLE"):
            correct=wallhits>=1
            count_hit+=int(correct)
            print(f'{"PASS" if correct else "FAIL"} | ORIGINAL_WALL_SIDE_CENTER={label} | wall_AABB_hits={wallhits} | platform_AABB_hits={platformhits}')
        else:
            gap=hits==0
            count_hole+=int(gap)
            correct=(platformhits>=1 and wallhits==0) if opt.owner_fix else gap
            print(f'{"PASS_CORNER_PLATFORM" if correct and opt.owner_fix else "EXPECTED_RED_GAP" if gap else "FAIL_CORNER_KIND"} | ORIGINAL_WALL_CORNER={label} | wall_AABB_hits={wallhits} | platform_AABB_hits={platformhits} | centerline_2m_ray={ray}')
            if not correct:
                raise ValueError(f"CORNER_EXPECTED_MISSING_OR_WRONG_COLLIDER_KIND:{label}")
    if count_hit!=4:raise ValueError("INCONSISTENT_ORIGINAL_WORLD_CALC_SIDE_POSITIVE")
    if count_hole!=(0 if opt.owner_fix else 4):
        raise ValueError("ORIGINAL_PINNED_PERIMETER_STATE_CHANGED")
    print(f"ORIGINAL_GIT_SOURCE_AND_DATA=VERIFIED | CODEX={CODEX} | LD02={data_ref} | JSON_BLOB={data_blob}")
    if opt.owner_fix:
        print("SIDE_MIDDLE_WALL_HITS=4/4 | REPAIRED_CORNER_PLATFORM_HITS=4/4 | CORNER_CENTERLINE_GAPS=0/4")
        print("CLOSED_PERIMETER=STATIC_GREEN | UNITY_PHYSX=NOT_RUN | NATIVE_ESCAPE=UNMEASURED")
        return 0
    print("SIDE_MIDDLE_HITS=4/4 | CORNER_CENTERLINE_GAPS=4/4 | MEASURED_CORNER_OPENING=1.75m")
    print("CLOSED_PERIMETER=RED_EXPECTED | UNITY_PHYSX=NOT_RUN | NATIVE_ESCAPE=UNMEASURED")
    if opt.require_closed:return 1
    return 0

if __name__=="__main__":
    try:sys.exit(main())
    except Exception as e:
        print(f"QA_HARNESS_FAIL={type(e).__name__}: {e}",file=sys.stderr)
        sys.exit(2)
