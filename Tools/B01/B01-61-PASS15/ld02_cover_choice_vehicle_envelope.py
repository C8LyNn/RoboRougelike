#!/usr/bin/env python3
"""B01-61 P15: source-pin original R3 part-block body bounds vs NEW LD02 routes.
RUNS actual Git JSON/source text and deterministic swept AABB; NOT Unity/PhysX.
A conservative axis-aligned, yaw0 primary part-cube envelope is not proof that
other payloads, suspension, dynamics, or turning clear in real Unity physics.
"""
import json, math, subprocess, sys, argparse
from ld02_perimeter_geometry_check import intersects
CODEX="231005c6d0351945a8ee6684f3856af118f0e94f"
LEVEL_REF="691c8cb6b0be086d455e530f2531de7f3753a0ba"
LEVEL_PATH="Research/B01-25/P15/LD02_COVER_CHOICE.json"
PIN={
 "Assets/MapEditor/MapWorld.cs":"8928908db123b35dcab4b324fe9d3a7a8fceee4e",
 "Assets/R3/R3VehicleAssembly.cs":"49fab51ac993128a9965df38ce32489f493b257a",
 "Assets/R3/R3VehicleCatalog.cs":"f76de5bbdcc1dbdb7ddb9fe9859c36c232fd8584",
 "Assets/R4_1/R41PartGeometry.cs":"76a14de4c347dd3e91b0a37487d804023eab5da1",
 LEVEL_PATH:"8586312b9606d94b088e4e7640c28105e2d6bac1",
}
SHORT=[(2,5),(3,5),(4,5),(5,5),(6,5),(7,5),(8,5),(9,5)]
COVER=[(2,5),(2,4),(2,3),(2,2),(3,2),(4,2),(5,2),(6,2),
       (7,2),(8,2),(9,2),(10,2),(10,3),(10,4),(10,5),(9,5)]
def git(root,*cmd):
    return subprocess.check_output(["git","-C",root,*cmd],text=True).strip()
def read(root,ref,path):
    sha=git(root,"rev-parse",f"{ref}:{path}")
    if sha!=PIN[path]:raise ValueError(f"WRONG_SOURCE_BLOB {path}: {sha} != {PIN[path]}")
    return git(root,"show",f"{ref}:{path}")
def check(s,anchors,name):
    for a in anchors:
        if a not in s:raise ValueError(f"CHANGED_ORIGINAL_CSHARP:{name}: {a}")
def box(item):
    k=item["kind"];x,z=item["x"]*4.,item["z"]*4.
    if k==1:
        if item["rotation"]==0: rx,rz=2.,.25
        elif item["rotation"]==1:rx,rz=.25,2.
        else:raise ValueError("UNSUPPORTED_WALL_ROTATION")
    elif k==6:rx=rz=2.
    elif k==7:rx=rz=.75
    elif k==8:rx,rz=2.,.25
    else:raise ValueError(f"UNSUPPORTED_SOLID_KIND {k}")
    if k==6:
        if (item["rotation"],item["scaleX"],item["scaleY"],item["scaleZ"])!=(0,1,4,1):
            raise ValueError("UNSUPPORTED_REPAIRED_CORNER_PLATFORM")
    elif any(item[p]!=1 for p in ("scaleX","scaleY","scaleZ")):
        raise ValueError("UNSUPPORTED_SOLID_SCALE")
    return (x-rx,x+rx,z-rz,z+rz,k)
def gap(x,z,rx,rz,b):
    dx=max(b[0]-(x+rx),(x-rx)-b[1],0.)
    dz=max(b[2]-(z+rz),(z-rz)-b[3],0.)
    return math.hypot(dx,dz)
def scan(route,solids,rx,rz):
    minimum=1e9;where=None;contacts=0
    for a,c in zip(route,route[1:]):
        dx,dz=c[0]-a[0],c[1]-a[1]
        if abs(dx)+abs(dz)!=1:raise ValueError("NONCONTIGUOUS_ROUTE")
        for t in range(17):  # <=0.25 m center steps, including endpoints
            x=(a[0]+dx*t/16)*4.
            z=(a[1]+dz*t/16)*4.
            for b in solids:
                clearance=gap(x,z,rx,rz,b)
                if clearance<minimum:minimum,where=clearance,(round(x,2),round(z,2),b[4])
                if clearance<=1e-10:contacts+=1
    return minimum,where,contacts
def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",default=".")
    root=p.parse_args().repo
    world=read(root,CODEX,"Assets/MapEditor/MapWorld.cs")
    assembly=read(root,CODEX,"Assets/R3/R3VehicleAssembly.cs")
    cat=read(root,CODEX,"Assets/R3/R3VehicleCatalog.cs")
    wheel=read(root,CODEX,"Assets/R4_1/R41PartGeometry.cs")
    check(world,[
      "public const float Grid=4f;",
      "case MapItemKind.Wall:position=new Vector3(0,2,0);scale=new Vector3(Grid,4,.5f);",
      "case MapItemKind.Pillar:position=new Vector3(0,2,0);scale=new Vector3(1.5f,2,1.5f);",
      "case MapItemKind.Platform:position=new Vector3(0,.5f,0);scale=new Vector3(4,1,4);",
      "case MapItemKind.LowBarrier:position=new Vector3(0,.5f,0);scale=new Vector3(4,1,.5f);",
      "o.transform.rotation=Quaternion.Euler(0,item.rotation*90,0);"],"MapWorld")
    check(assembly,[
      "item.transform.localPosition=(Vector3)state.Cell*0.55f;",
      "item.transform.localScale=Vector3.one*0.52f;"],"R3Assembly")
    check(cat,[
      "int width = light ? 1 : heavy ? 3 : 2;",
      "int length = light ? 2 : heavy ? 4 : 3;",
      "int wheels = light ? 2 : 6;",
      "side * (width + 1)"],"R3Catalog")
    check(wheel,["wheel.localScale=new Vector3(1.5f,0.35f,1.5f);",
                 "wheel.localRotation=Quaternion.Euler(0,0,90);"],"R41Wheel")
    doc=json.loads(read(root,LEVEL_REF,LEVEL_PATH))
    if (doc["version"]!=2 or doc["globalFloor"] is not False
            or len(doc["items"])!=173):raise ValueError("UNEXPECTED_P15_MAP_FORMAT")
    counts={k:sum(i["kind"]==k for i in doc["items"]) for k in (0,1,4,5,6,7,8)}
    if counts!={0:120,1:41,4:1,5:1,6:4,7:4,8:2}:
        raise ValueError(f"WRONG_REPAIRED_LEVEL_KINDS {counts}")
    corners={(0,0),(11,0),(0,9),(11,9)}
    fixed={(i["x"],i["z"]) for i in doc["items"] if i["kind"]==6}
    if fixed!=corners:raise ValueError(f"REPAIRED_CORNER_PLATFORM_SET_INVALID:{fixed}")
    solids=[box(i) for i in doc["items"] if i["kind"] in (1,6,7,8)]
    # NEW P15 input — use exactly the existing source-derived slab/ray predicate
    # from the previous P14 receiver, no second approximate wall formula.
    rays={
      "WEST_MIDDLE":((1.,20.),(-1.,20.)),
      "EAST_MIDDLE":((43.,20.),(45.,20.)),
      "SOUTH_MIDDLE":((24.,1.),(24.,-1.)),
      "NORTH_MIDDLE":((24.,35.),(24.,37.)),
      "SOUTH_WEST_CORNER":((1.,1.),(1.,-1.)),
      "SOUTH_EAST_CORNER":((43.,1.),(43.,-1.)),
      "NORTH_WEST_CORNER":((1.,35.),(1.,37.)),
      "NORTH_EAST_CORNER":((43.,35.),(43.,37.)),
    }
    for label,ray in rays.items():
        wallhits=sum(intersects(ray,b[:4]) for b in solids if b[4]==1)
        platformhits=sum(intersects(ray,b[:4]) for b in solids if b[4]==6)
        wanted=(1,0) if label.endswith("MIDDLE") else (0,1)
        if (wallhits,platformhits)!=wanted:
            raise ValueError(f"REPAIRED_P15_COLLIDER_SOURCE_RAY_RED:{label}:{wallhits}/{platformhits}")
        print(f"PASS_REPAIRED_P15_RAY={label} | wall_AABB_hits={wallhits} | platform_AABB_hits={platformhits}")
    # Original source geometry: body chassis [-length..+length] and
    # wheel center x = ±(width+1); all base cubes half-size 0.52/2.
    # Wheel cylinder's source local axle radius/scale is bounded by body/cube envelope.
    scenarios=(("LIGHT",1,2),("HEAVY",3,4))
    for name,width,length in scenarios:
        rx=(width+1)*.55+.26
        rz=length*.55+.26
        for route_name,route in (("EXPOSED",SHORT),("COVER",COVER)):
            dist,pt,hits=scan(route,solids,rx,rz)
            print(f"{'PASS' if not hits else 'PRODUCT_RED'} | {name} {route_name} | samples={17*(len(route)-1)} | centerline_body_AABB_half={rx:.2f}x{rz:.2f}m | min_clearance={dist:.3f}m | closest={pt} | overlap_samples={hits}")
            if hits:raise ValueError(f"ORIGINAL_ROUTE_BODY_ENVELOPE_OVERLAP:{name}:{route_name}")
    # Two deliberate labelled in-memory QA negatives, never modify B01-25 Git bytes.
    for label,route,blocked in (("EXPOSED",SHORT,(6,5)),("COVER",COVER,(6,2))):
        fake={"kind":1,"x":blocked[0],"z":blocked[1],"rotation":0,
              "scaleX":1,"scaleY":1,"scaleZ":1}
        _,_,hits=scan(route,solids+[box(fake)],2.46,2.46)
        if not hits:raise ValueError(f"QA_FAIL_OPEN_INJECTED_WALL:{label}")
        print(f"EXPECTED_REJECTION_SYNTHETIC_WALL | route={label} | cell={blocked} | overlap_samples={hits}")
    print("ORIGINAL_SOURCE_GIT_SHA=VERIFIED | REPAIRED_P15_DATA_GIT_SHA=VERIFIED | NATIVE_RAY_ANALOGUE_8/8=STATIC_PASS | CORNER_PLATFORMS=4/4 | STATIC_ROUTE_ENVELOPES=4/4_PASS | INJECTED_NEGATIVES=2/2_REJECTED | UNITY/PHYSX/REAL_VEHICLE=NOT_RUN")
    return 0
if __name__=="__main__":
    try:sys.exit(main())
    except Exception as e:
        print(f"QA_ERROR={type(e).__name__}: {e}",file=sys.stderr)
        sys.exit(2)
