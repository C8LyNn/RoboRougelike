#!/usr/bin/env python3
"""Seven static *source-defined payload* AABBs, never native PhysX measurements.

Input: exact source Git blobs from Codex PR #302 and PARTSIZE PR #286.
AABB assumptions: unit Unity primitives (Cylinder diameter=1,height=2,
Sphere diameter=1), counterfactual unit-scale parent and separately actual
R3VehicleAssembly 0.52-scale host root, yaw0; child X90/Z90.
Collider.enabled, whole-part volume, swept clearance & typed mounts UNKNOWN.
"""
import argparse
import json
import math
from pathlib import Path
import re
import subprocess
import sys

CODEX_SHA="d44fa09c227dffca1c1f8832383bbd80430bba90"
PART_SHA="be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
GEOM_PATH="Assets/R4_1/R41PartGeometry.cs"
GEOM_BLOB="76a14de4c347dd3e91b0a37487d804023eab5da1"
HOST_PATH="Assets/R3/R3VehicleAssembly.cs"
HOST_BLOB="1b49c7365388694ebffacc5a4f20b3572150577a"
CAT_PATH="Assets/PARTSIZE001/Runtime/PartSizeBaselineCatalog.cs"
CAT_BLOB="6153d1758e800ec5c4210323610b061c06e8ea85"
KINDS=("Booster","JumpDrive","Blink","Gyroscope","OmniDrive","MagneticGrapple","HoverEngine")
GRID=.55

def git_bytes(repo,sha,path,blob):
    obj=subprocess.check_output(["git","-C",str(repo),"rev-parse",sha+":"+path],text=True).strip()
    if obj!=blob: raise ValueError("ORIGINAL_SOURCE_GIT_BLOB_DRIFT:"+path)
    # Git blob bytes are UTF-8; Windows locale may otherwise decode Polish names as cp1252.
    return subprocess.check_output(["git","-C",str(repo),"show",sha+":"+path]).decode("utf-8")

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

def host_scale_from_original_assembly(source):
    # Source evidence from real Codex #302 (and #300), not a guessed prefab.
    required=("var root=new GameObject(\"Pojazd \" + model.Design.Name); root.SetActive(false);",
      "root.transform.SetPositionAndRotation(position,rotation);",
      "item.transform.SetParent(root.transform,false);",
      "item.transform.localPosition=(Vector3)state.Cell*0.55f;",
      "item.transform.localRotation=R3MountingGrid.Rotation(state.RotationQuarterTurns);",
      "if(!wide)payload=R41PartGeometry.Payload(item.transform,state.Kind,state.Weapon,",
      "true,out partMuzzle);")
    for x in required:
        if x not in source: raise ValueError("HOST_SOURCE_CONTRACT_CHANGED:"+x)
    actual=float(find_one(source,r"item\.transform\.localScale=Vector3\.one\*([\d.]+)f;"))
    if actual<=0 or actual>=1: raise ValueError("HOST_SCALE_UNSUPPORTED")
    if actual!=.52: raise ValueError("HOST_SCALE_SOURCE_CHANGED_FROM_052")
    return actual

def fitted_host_row(counterfactual,scale):
    # Original R41 child local transform is nested under the actual .52 host
    # part root, itself an existing physical unit-cube scaled .52. Yaw=0 only.
    lo=[round(x*scale,7) for x in counterfactual["payload_aabb_min_m"]]
    hi=[round(x*scale,7) for x in counterfactual["payload_aabb_max_m"]]
    root_extent=scale*.5
    union_lo=[round(min(x,-root_extent),7) for x in lo]
    union_hi=[round(max(x,+root_extent),7) for x in hi]
    cells=[list(range(math.floor((a+GRID/2)/GRID),
         math.floor((b+GRID/2)/GRID)+1)) for a,b in zip(union_lo,union_hi)]
    overshoot=[round(max(0,-GRID/2-a,b-GRID/2),6) for a,b in zip(union_lo,union_hi)]
    return {"assembled_source_scale":scale,"assembled_yaw_deg":0,
       "assembled_payload_aabb_min_m":lo,
       "assembled_payload_aabb_max_m":hi,
       "assembled_root_plus_payload_aabb_min_m":union_lo,
       "assembled_root_plus_payload_aabb_max_m":union_hi,
       "assembled_signed_cells_xyz":cells,
       "assembled_overshoot_vs_one_cell_xyz_m":overshoot,
       "assembled_overhang_any":any(x>0 for x in overshoot),
       "assembled_host_requests_solid_payload":True,
       "assembled_native_collider_verified":False}

def analyze(geom,catalog,host_source):
    params=source_parameters(geom)
    names=re.findall(r'"([^"]+)"',find_one(catalog,r"CanonicalNames = \{([\s\S]*?)\};"))
    if len(names)!=19 or not set(KINDS).issubset(names):
        raise ValueError("PARTSIZE_CANONICAL_CATALOG_MISMATCH")
    if "return PartSizeReferenceFixtures.Unmeasured(canonicalKind," not in catalog:
        raise ValueError("PRODUCER_NO_LONGER_FAILS_CLOSED")
    scale=host_scale_from_original_assembly(host_source)
    rows=[classify(params,k) for k in KINDS]
    for row in rows:
        row["unit_parent_counterfactual_only"]=True
        row.update(fitted_host_row(row,scale))
    return {"producer_sha":PART_SHA,"codex_sha":CODEX_SHA,
        "geometry_blob":GEOM_BLOB,"catalog_blob":CAT_BLOB,
        "host_assembly_blob":HOST_BLOB,"host_assembly_scale":scale,
        "source_only":True,"native":"NOT_RUN","G2":"HOLD",
        "assumption":"original CODEX R3VehicleAssembly .52 scaled source parent, static yaw0 part anchor, nominal primitive bounds; no native Unity/PhysX or dynamic pose verification",
        "rows":rows}

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--repo",default=".")
    parser.add_argument("--out")
    args=parser.parse_args()
    report=analyze(git_bytes(args.repo,CODEX_SHA,GEOM_PATH,GEOM_BLOB),
                   git_bytes(args.repo,PART_SHA,CAT_PATH,CAT_BLOB),
                   git_bytes(args.repo,CODEX_SHA,HOST_PATH,HOST_BLOB))
    body=json.dumps(report,sort_keys=True,indent=2)+"\n"
    if args.out:Path(args.out).write_text(body,encoding="utf8")
    else:print(body)
    print("7 SOURCE_STATIC_AABB RECORDS FROM ACTUAL HOST SCALE; NATIVE_UNITY_NOT_RUN",file=sys.stderr)

if __name__=="__main__":
    main()
