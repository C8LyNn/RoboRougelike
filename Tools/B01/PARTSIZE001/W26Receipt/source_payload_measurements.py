#!/usr/bin/env python3
"""Seven static *source-defined payload* AABBs, never native PhysX measurements.

Input: exact source Git blobs from Codex PR #300 and PARTSIZE PR #286.
AABB assumptions: unit Unity primitives (Cylinder diameter=1,height=2,
Sphere diameter=1), unrotated unit-scale parent; rotating child only X90/Z90.
Collider.enabled, whole-part volume, swept clearance & typed mounts UNKNOWN.
"""
import argparse
import json
import math
from pathlib import Path
import re
import subprocess
import sys

CODEX_SHA="7cfd1276d4708395144668249223d78fb83c3b19"
PART_SHA="be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
GEOM_PATH="Assets/R4_1/R41PartGeometry.cs"
GEOM_BLOB="76a14de4c347dd3e91b0a37487d804023eab5da1"
CAT_PATH="Assets/PARTSIZE001/Runtime/PartSizeBaselineCatalog.cs"
CAT_BLOB="6153d1758e800ec5c4210323610b061c06e8ea85"
KINDS=("Booster","JumpDrive","Blink","Gyroscope","OmniDrive","MagneticGrapple","HoverEngine")
GRID=.55

def git_bytes(repo,sha,path,blob):
    obj=subprocess.check_output(["git","-C",str(repo),"rev-parse",sha+":"+path],text=True).strip()
    if obj!=blob: raise ValueError("ORIGINAL_SOURCE_GIT_BLOB_DRIFT:"+path)
    return subprocess.check_output(["git","-C",str(repo),"show",sha+":"+path],text=True)

def find_one(text,pattern):
    matches=re.findall(pattern,text)
    if len(matches)!=1: raise ValueError("SOURCE_TRANSFORM_NOT_UNIQUE:"+pattern)
    return matches[0]

def anchors(s):
    return (
        "GameObject.CreatePrimitive(type)",
        "if(!solid){var collider=item.GetComponent<Collider>();collider.enabled=false;Object.Destroy(collider);}",
        "kind==R3PartKind.Blink||kind==R3PartKind.Gyroscope?PrimitiveType.Sphere",
        "kind==R3PartKind.CrawlerTrack?PrimitiveType.Cube:PrimitiveType.Cylinder",
        "device.localPosition=Vector3.zero;device.localScale=Vector3.one*.85f;",
        "device.localRotation=Quaternion.Euler(90,0,0)",
        "device.localRotation=Quaternion.Euler(0,0,90)",
        'Primitive(PrimitiveType.Cylinder,block,material,false,"Pierścień żyroskopu")',
    )

def source_parameters(geom):
    for token in anchors(geom):
        if token not in geom: raise ValueError("SOURCE_ANCHOR_MISSING:"+token)
    default=float(find_one(geom,r"device\.localScale=Vector3\.one\*([\d.]+)f;"))
    rear,front=map(float,find_one(geom,r"Vector3\.forward\*\(kind==R3PartKind\.Booster\?([-\d.]+)f:([-\d.]+)f\)"))
    sx,sy,sz=map(float,find_one(geom,r"device\.localRotation=Quaternion\.Euler\(90,0,0\);device\.localPosition=[^\n]*\n\s*device\.localScale=new Vector3\(([\d.]+)f,([\d.]+)f,([\d.]+)f\);"))
    lowy,low=map(float,find_one(geom,r"device\.localPosition=Vector3\.down\*([\d.]+)f;device\.localScale=new Vector3\(1,([\d.]+)f,1\);"))
    omy,ox,oy,oz=map(float,find_one(geom,r"device\.localPosition=Vector3\.down\*([\d.]+)f;device\.localRotation=Quaternion\.Euler\(0,0,90\);device\.localScale=new Vector3\(([\d.]+)f,([\d.]+)f,([\d.]+)f\);"))
    grid=float(find_one(geom,r"public const float Grid=([\d.]+)f,"))
    if grid!=GRID:raise ValueError("SOURCE_GRID_CHANGED")
    return dict(default=default,rear=rear,front=front,fore=(sx,sy,sz),lowy=lowy,low=low,omy=omy,omni=(ox,oy,oz))

def classify(p,kind):
    if kind in ("Blink","Gyroscope"):
        xyz=(0.,0.,0.)
        ext=(p["default"]/2,)*3
        primitive="Sphere"
    elif kind in ("Booster","MagneticGrapple"):
        x,y,z=p["fore"]
        xyz=(0.,0.,p["rear"] if kind=="Booster" else p["front"])
        ext=(x/2,z/2,y)  # rotated X90, cylinder long Y-axis maps to world Z
        primitive="Cylinder"
    elif kind in ("HoverEngine","JumpDrive"):
        xyz=(0.,-p["lowy"],0.)
        ext=(.5,p["low"],.5)
        primitive="Cylinder"
    elif kind=="OmniDrive":
        x,y,z=p["omni"]
        xyz=(0.,-p["omy"],0.)
        ext=(y,x/2,z/2)  # Z90, cylinder long Y-axis maps to world X
        primitive="Cylinder"
    else: raise ValueError("UNRECOGNISED_KIND")
    lo=[round(v-e,7) for v,e in zip(xyz,ext)]
    hi=[round(v+e,7) for v,e in zip(xyz,ext)]
    cells=[list(range(math.floor((a+GRID/2)/GRID),
                      math.floor((b+GRID/2)/GRID)+1)) for a,b in zip(lo,hi)]
    over=[round(max(0.,-GRID/2-a,b-GRID/2),6) for a,b in zip(lo,hi)]
    return {"kind":kind,"primitive":primitive,"payload_aabb_min_m":lo,
            "payload_aabb_max_m":hi,"signed_cells_intersected_xyz":cells,
            "max_overshoot_vs_single_cell_xyz_m":over,
            "source_derived_only":True,"native_physx":"NOT_RUN",
            "payload_collider_enabled_in_host":"UNVERIFIED",
            "dynamic_sweep":"UNMEASURED",
            "typed_socket":"UNVERIFIED",
            "gyro_decorative_ring_nonphysical":kind=="Gyroscope"}

def analyze(geom,catalog):
    params=source_parameters(geom)
    names=re.findall(r'"([^"]+)"',find_one(catalog,r"CanonicalNames = \{([\s\S]*?)\};"))
    if len(names)!=19 or not set(KINDS).issubset(names):
        raise ValueError("PARTSIZE_CANONICAL_CATALOG_MISMATCH")
    if "return PartSizeReferenceFixtures.Unmeasured(canonicalKind," not in catalog:
        raise ValueError("PRODUCER_NO_LONGER_FAILS_CLOSED")
    rows=[classify(params,k) for k in KINDS]
    return {"producer_sha":PART_SHA,"codex_sha":CODEX_SHA,
        "geometry_blob":GEOM_BLOB,"catalog_blob":CAT_BLOB,
        "source_only":True,"native":"NOT_RUN","G2":"HOLD",
        "assumption":"Unity nominal primitive bounds and static local transforms, no PhysX or imported Collider data",
        "rows":rows}

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--repo",default=".")
    parser.add_argument("--out")
    args=parser.parse_args()
    report=analyze(git_bytes(args.repo,CODEX_SHA,GEOM_PATH,GEOM_BLOB),
                   git_bytes(args.repo,PART_SHA,CAT_PATH,CAT_BLOB))
    body=json.dumps(report,sort_keys=True,indent=2)+"\n"
    if args.out:Path(args.out).write_text(body,encoding="utf8")
    else:print(body)
    print("7 SOURCE_STATIC_AABB RECORDS; NATIVE_UNITY_NOT_RUN",file=sys.stderr)

if __name__=="__main__":
    main()
