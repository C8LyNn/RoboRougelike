#!/usr/bin/env python3
"""B01-20 COURSE-MAPDOC-001: loss-labeled deterministic MAPGEN → genuine MapDocument v2 export.
No Codex Assets modifications; checkpoint positions are a separate NON-NATIVE sidecar.
"""
from collections import deque
import hashlib
import json
import math
from pathlib import Path
import sys

SOURCE = "5be31039411f89f17654b1c332f8692883fa03cc"
EXPECTED_SEEDS = (7,31,83,109,202,407)
EXPECTED_VARIANTS = ("P17-COURSE-A-SLALOM","P17-COURSE-B-CHICANE","P17-COURSE-C-ENDURANCE")
KINDS={"Boundary":1,"Obstacle":3,"Ramp":2,"Cover":8}
PRIORITY={"Boundary":4,"Obstacle":3,"Ramp":2,"Cover":1}
MAXCOORD=50
def fail(msg):raise ValueError("COURSE_MAPDOC_FAIL_CLOSED:"+msg)
def check(ok,msg):
    if not ok:fail(msg)
def hash_bytes(data):return hashlib.sha256(data).hexdigest()
def item(kind,x,z):
    return dict(kind=kind,x=x,z=z,rotation=0,scaleX=1,scaleY=1,scaleZ=1)
def compact(x):return json.dumps(x,ensure_ascii=False,separators=(",",":"),sort_keys=False)+"\n"
def floor_points(el,shift):
    p,s=el["position"],el["size"]
    # Snap integer cell centres that are INSIDE original floor rectangles.
    xl=math.ceil(p["x"]-s["x"]*.5+1e-8)
    xr=math.floor(p["x"]+s["x"]*.5-1e-8)
    zl=math.ceil(p["z"]-s["z"]*.5+1e-8)
    zr=math.floor(p["z"]+s["z"]*.5-1e-8)
    return {(x,z-shift) for x in range(xl,xr+1) for z in range(zl,zr+1)}
def occupied_points(el,shift):
    p,s=el["position"],el["size"]
    # Conservative top-down AABB raster: a cell overlaps if any part intersects source.
    xl=math.ceil(p["x"]-s["x"]*.5-.49999)
    xr=math.floor(p["x"]+s["x"]*.5+.49999)
    zl=math.ceil(p["z"]-s["z"]*.5-.49999)
    zr=math.floor(p["z"]+s["z"]*.5+.49999)
    return {(x,z-shift) for x in range(xl,xr+1) for z in range(zl,zr+1)}
def point(p,shift):
    return (math.floor(p["x"]+.5),math.floor(p["z"]+.5)-shift)
def connected(floor,obstacles,route):
    first=route[0]
    if first not in floor or first in obstacles:return False
    seen={first};q=deque([first])
    while q:
        x,z=q.popleft()
        for n in ((x+1,z),(x-1,z),(x,z+1),(x,z-1)):
            if n not in seen and n in floor and n not in obstacles:
                seen.add(n);q.append(n)
    return all(p in seen for p in route)
def convert(src):
    check(src.get("packageId")=="MAPGEN-COURSE-P17" and src.get("recipe")=="ObstacleCourse","ORIGINAL_PACKAGE")
    check(src.get("sourceSha")==SOURCE and src.get("nativeUnity")=="NOT_RUN","ORIGINAL_SOURCE")
    check(src.get("variantId") in EXPECTED_VARIANTS and src.get("seed") in EXPECTED_SEEDS,"VARIANT_SEED")
    check(isinstance(src.get("geometryHash"),str) and len(src["geometryHash"])==64,"GEOMETRY_HASH")
    elems=src.get("elements")
    check(isinstance(elems,list) and len(elems)>=6,"MISSING_ELEMENTS")
    shift=round(src["bounds"]["center"]["z"])
    floor=set()
    for e in elems:
        if e["kind"]=="Floor":floor |= floor_points(e,shift)
    check(0<len(floor)<=2000,"GROUND_BUDGET_EXCEEDED:"+str(len(floor)))
    check(all(abs(x)<=MAXCOORD and abs(z)<=MAXCOORD for x,z in floor),"FLOOR_OUTSIDE_NATIVE_BOUNDS")
    solid={}
    source_counts={}
    for e in elems:
        k=e["kind"]
        if k=="Floor":continue
        check(k in KINDS,"UNSUPPORTED_ORIGINAL_KIND")
        source_counts[k]=source_counts.get(k,0)+1
        for p in occupied_points(e,shift):
            check(all(abs(v)<=MAXCOORD for v in p),"SOLID_OUTSIDE_NATIVE_BOUNDS")
            if p not in solid or PRIORITY[k]>PRIORITY[solid[p]]:
                solid[p]=k
    check(len(solid)<=1998,"OBJECT_BUDGET_EXCEEDED")
    checkpoints=src["checkpoints"]
    check(len(checkpoints) in (3,4,5) and len(checkpoints)==src["config"]["segments"],"CHECKPOINT_MISMATCH")
    route=[point(src["start"],shift)]+[point(p,shift) for p in checkpoints]+[point(src["finish"],shift)]
    check(len(set(route))>=len(route)-1,"DEGENERATE_ROUTE")
    check(all(p in floor for p in route),"ROUTE_OFF_FLOOR")
    check(all(p not in solid for p in route),"ROUTE_ON_SOLID")
    check(connected(floor,solid,route),"GRID_ROUTE_DISCONNECTED")
    check(route[0]!=route[-1],"SPAWN_FINISH_COLLISION")
    # Source geometry projects onto native 1m integer x/z tiles. No invented native checkpoint kind.
    grounds=[item(0,x,z) for x,z in sorted(floor)]
    solids=[item(KINDS[k],x,z) for (x,z),k in sorted(solid.items())]
    sp=item(4,*route[0]);en=item(5,*route[-1])
    doc={"version":2,"globalFloor":False,"items":[*grounds,*solids,sp,en]}
    check(len(grounds)<=2000 and len(solids)+2<=2000,"MAPDRAFT_ITEM_BUDGET")
    check(len({(a["x"],a["z"]) for a in solids})==len(solids),"DUPLICATE_OBJECT")
    check(route[0] not in solid and route[-1] not in solid,"SPECIAL_COLLISION")
    sidecar={
      "packageId":"COURSE-MAPDOC-001","sourcePackage":"MAPGEN-COURSE-P17",
      "sourceMapgenSha":SOURCE,"sourceGeometryHash":src["geometryHash"],
      "variantId":src["variantId"],"seed":src["seed"],"recipe":"ObstacleCourse",
      "worldToMapShiftZ":shift,"checkpointMarkersNonNative":[{"x":x,"z":z} for x,z in route[1:-1]],
      "sourceCounts":source_counts,
      "nativeItemCounts":{"ground":len(grounds),"solid":len(solids),"spawn":1,"finish":1},
      "conversion":"1m integer cell centre floor sampling; conservative AABB solid raster; z recenter",
      "knownLosses":["No native MapDraft checkpoint item kind (sidecar only)",
                     "Ramp pitch/elevation discarded by 2D MapDraft schema",
                     "Walls/obstacles rasterized to 1m tiles; dimensions rounded",
                     "Original vehicle clearance/PhysX requires native revalidation"],
      "nativeUnity":"NOT_RUN","physx":"NOT_RUN"
    }
    return doc,sidecar
def main(input_folder,output_folder):
    root=Path(input_folder);out=Path(output_folder)
    receipt=json.loads((root/"results.json").read_text(encoding="utf-8"))
    check(receipt["packageId"]=="MAPGEN-COURSE-P17" and receipt["sourceSha"]==SOURCE,"RECEIPT_SOURCE")
    check(receipt["blueprints"]==18 and len(receipt["generated"])==18,"RECEIPT_18")
    out.mkdir(parents=True,exist_ok=True)
    entries=[]
    distinct=set()
    for entry in receipt["generated"]:
        name=entry["file"]
        check(Path(name).name==name and name.endswith(".json"),"UNSAFE_INPUT_NAME")
        src=json.loads((root/name).read_text(encoding="utf-8"))
        check(src["geometryHash"]==entry["geometryHash"],"WRONG_GENERATOR_HASH")
        check(src["geometryHash"] not in distinct,"DUPLICATE_ORIGINAL_GEOMETRY")
        distinct.add(src["geometryHash"])
        doc,sidecar=convert(src)
        base=Path(name).stem
        data=compact(doc).encode("utf-8");support=compact(sidecar).encode("utf-8")
        mapname=base+"-mapdocument-v2.json";side=base+"-checkpoint-sidecar.json"
        (out/mapname).write_bytes(data);(out/side).write_bytes(support)
        entries.append({"variant":src["variantId"],"seed":src["seed"],"map":mapname,"sidecar":side,
                        "sourceGeometryHash":src["geometryHash"],"mapSha256":hash_bytes(data),
                        "sidecarSha256":hash_bytes(support),"counts":sidecar["nativeItemCounts"]})
    check(len(entries)==18 and len(distinct)==18,"FINAL_COUNT")
    summary={
      "packageId":"COURSE-MAPDOC-001","version":"R1","sourceMapgen":SOURCE,
      "sourceInputPr":331,"originalSourceGeneration":"ORIGINAL_MAPGEN_CSHARP",
      "maps":18,"converted":entries,
      "nativeMapDraftCsharp":"REQUIRES_SEPARATE_COMPILED_ACCEPTANCE",
      "nativeUnity":"NOT_RUN","physx":"NOT_RUN","codexACK":"PENDING"
    }
    (out/"receipt.json").write_bytes(compact(summary).encode("utf-8"))
    print("COURSE_MAPDOC_REAL_18_NATIVE_JSON_PRODUCED=18 ORIGINAL_CSHARP_INPUT=PASS UNITY=NOT_RUN")
if __name__=="__main__":
    if len(sys.argv)!=3:raise SystemExit("Usage: convert.py original-csharp-blueprints output")
    main(sys.argv[1],sys.argv[2])
