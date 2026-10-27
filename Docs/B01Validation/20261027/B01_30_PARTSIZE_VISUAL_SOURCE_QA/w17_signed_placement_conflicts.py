#!/usr/bin/env python3
"""W17 B01-30: source-grounded signed occupancy and actual BoxCollider conflicts.

Consumes EXACT PR272 R41 and PR286 sources. This is source geometry evidence:
NO actual Unity Collider.bounds/mesh PhysX/narrowphase/rendering, NO mount approval.
"""
import argparse, json, math
from pathlib import Path
import importlib.util

HERE=Path(__file__).resolve().parent
modspec=importlib.util.spec_from_file_location("source_aabb",HERE/"partsize_visual_source_qa.py")
geometry=importlib.util.module_from_spec(modspec);modspec.loader.exec_module(geometry)
audit_spec=importlib.util.spec_from_file_location("pr286_evidence",HERE/"partsize_pr286_delta.py")
pr286=importlib.util.module_from_spec(audit_spec);audit_spec.loader.exec_module(pr286)
TESTED=("Wheel","CrawlerTrack","GunHeavy","Suspension")

def signed_indices(lo,hi,grid=geometry.GRID):
    if not all(math.isfinite(v) for v in (lo,hi,grid)) or lo>=hi or grid<=0:
        raise ValueError("invalid signed occupancy interval")
    a=math.floor(lo/grid+0.5)
    b=math.ceil(hi/grid+0.5)-1
    return list(range(a,b+1))

def signed_cells(r):
    xyz=[signed_indices(r["physicalColliderMinXYZ"][i],
                        r["physicalColliderMaxXYZ"][i]) for i in range(3)]
    return [list(v) for v in xyz],len(xyz[0])*len(xyz[1])*len(xyz[2])

def positive_root_only_fails(r):
    indices,_=signed_cells(r)
    dims=r["logicalCellsWHD"]
    return any(min(k)<0 or max(k)>=dims[i] for i,k in enumerate(indices))

def box_neighbor_conflict(kind,yaw):
    if kind not in ("CrawlerTrack","GunHeavy"):
        raise ValueError("only confirmed BoxCollider child, not convex wheel MeshCollider")
    payload=geometry.PARTS[kind]["shapes"][1]
    lo,hi=geometry.union_bounds([payload],yaw,True)
    direction=tuple(int(x) for x in geometry.rotate_y((0,0,1),yaw))
    nc=[x*geometry.GRID for x in direction]
    near=[v-geometry.BLOCK/2 for v in nc]
    far=[v+geometry.BLOCK/2 for v in nc]
    depths=[round(max(0.0,min(hi[i],far[i])-max(lo[i],near[i])),6)
            for i in range(3)]
    return {"part":kind,"yawQuarterTurns":yaw,
      "neighborLogicalOffsetXYZ":list(direction),
      "neighborCenterXYZ":[round(x,6) for x in nc],
      "payloadBoxMinXYZ":lo,"payloadBoxMaxXYZ":hi,
      "neighborBoxMinXYZ":[round(x,6) for x in near],
      "neighborBoxMaxXYZ":[round(x,6) for x in far],
      "positiveBoxBoxIntersection":all(x>0 for x in depths),
      "staticBoxBoxIntersectionDepthsXYZ":depths,
      "staticBoxBoxIntersectionVolumeM3":round(math.prod(depths),9),
      "conclusion":"SOURCE_REST_BOX_BOX_GEOMETRY_NOT_UNITY_PHYSX"}

def measure_record(kind,yaw):
    r=geometry.measure(kind,yaw)
    physical,num=signed_cells(r)
    vr=[[x for x in signed_indices(r["renderMinXYZ"][i],r["renderMaxXYZ"][i])]
        for i in range(3)]
    root=positive_root_only_fails(r)
    # Old R3 measured wheel/track/heavy have a 1-cell logical owner.
    assert r["logicalCellsWHD"]==[1,1,1]
    return {"part":kind,"yawQuarterTurns":yaw,
      "sourceDerivedPhysicalBoundsMinXYZ":r["physicalColliderMinXYZ"],
      "sourceDerivedPhysicalBoundsMaxXYZ":r["physicalColliderMaxXYZ"],
      "sourceDerivedRenderBoundsMinXYZ":r["renderMinXYZ"],
      "sourceDerivedRenderBoundsMaxXYZ":r["renderMaxXYZ"],
      "originalLogicalOwnerCells":[1,1,1],
      "signedPhysicalCellIndexRangesXYZ":physical,
      "signedRenderCellIndexRangesXYZ":vr,
      "conservativeSignedCellCount":num,
      "positiveOnlyAnchoredExisting1CellFit":not root,
      "proposedGridOccupancyApproved":False,
      "nativeColliderBounds":"NOT_RUN",
      "nativeMountAndPhysicalContact":"NOT_RUN"}

def build(producer,canonical):
    # Fail closed on modified source shape, catalog, owner and 11 unmeasured.
    source=pr286.evaluate(producer,canonical)
    assert source["pr286CatalogKinds"]==19
    assert len(source["physicalUnmeasuredCanonicalKinds"])==11
    rows=[measure_record(p,q) for p in TESTED for q in range(4)]
    overlaps=[box_neighbor_conflict(p,q)
              for p in ("CrawlerTrack","GunHeavy") for q in range(4)]
    conflicts=[x for x in rows if x["part"]!="Suspension"]
    suspended=[x for x in rows if x["part"]=="Suspension"]
    if not all(not x["positiveOnlyAnchoredExisting1CellFit"] for x in conflicts):
        raise ValueError("SOURCE_GEOMETRY_DRIFT: 12 collision payloads formerly outside")
    if not all(x["positiveOnlyAnchoredExisting1CellFit"] for x in suspended):
        raise ValueError("SOURCE_GEOMETRY_DRIFT: Suspension static physical collider formerly FIT")
    if not all(x["signedRenderCellIndexRangesXYZ"]!=x["signedPhysicalCellIndexRangesXYZ"] for x in suspended):
        raise ValueError("SOURCE_GEOMETRY_DRIFT: Suspension source decorative piston should render outside")
    if not all(x["positiveBoxBoxIntersection"] for x in overlaps):
        raise ValueError("SOURCE_GEOMETRY_DRIFT: expected 8 source box intersection probes")
    return {"schema":"B01-30-W17-3-PLACEMENT-CONFLICTS-1",
      "inputPartsizePR286Head":source["producerPr286ExactHead"],
      "canonicalR41PinnedHead":geometry.SOURCE_HEAD,
      "parts":[{"part":p,
        "fourYawCases":[x for x in rows if x["part"]==p],
        "recommendedNextTest":(
          "convex MeshCollider versus actual neighbor, 4 yaw, PhysX ComputePenetration"
          if p=="Wheel" else
          "noncolliding piston: Renderer.bounds versus physical collider inventory, four yaw"
          if p=="Suspension" else
          "actual child BoxCollider versus standard neighbor, 4 yaw, PhysX ComputePenetration"
        )} for p in TESTED],
      "boxNeighborProbes":overlaps,
      "negativeCases":[
        {"id":"W17-WHEEL-OFFSET","scope":"physically enabled Wheel with center root at (0,0,0)",
         "expect":"REJECT fake one-cell physical claim; validate actual convex MeshCollider with real neighbor",
         "note":"probe actual signed XYZ record; do not use this descriptive field for geometry"},
        {"id":"W17-TRACK-PLUS_Z","scope":"Track child BoxCollider + neighbor Armor anchored at (0,0,1)",
         "expect":"detect static overlap source rest; native sensor must measure true contact"},
        {"id":"W17-HEAVY_GUN-PLUS_Z","scope":"GunHeavy child BoxCollider + neighbor Armor anchored at (0,0,1)",
         "expect":"detect static overlap and yaw/pitch sweep before authorizing"},
        {"id":"W17-SUSPENSION-VISUAL_ONLY","scope":"Suspension root Collider plus render-only piston",
         "expect":"show renderer outside cell but preserve collider-only valid mounting; no decorative BuildSocket"},
        {"id":"W17-BROADPHASE-OMITTED","scope":"two independent overlapping BoxColliders; [] plus completeExternalBroadphase=true",
         "expect":"MUST REJECT by authoritative broadphase rather than relying on caller claim"}],
      "sourcePredictedNotUnityMeasurement":True,
      "thisReportApprovesNoMounts":True,
      "unityEditor":"NOT_RUN","unityPhysx":"NOT_RUN","g2":"HOLD"}

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--producer",required=True)
    ap.add_argument("--canonical",required=True)
    ap.add_argument("--output",required=True)
    args=ap.parse_args()
    data=build(args.producer,args.canonical)
    out=Path(args.output);out.parent.mkdir(parents=True,exist_ok=True)
    out.write_text(json.dumps(data,sort_keys=True,indent=2)+"\n","utf8")
    print(json.dumps({"cases":len(data["parts"])*4,
      "boxProbes":len(data["boxNeighborProbes"]),
      "positiveBoxBox":sum(x["positiveBoxBoxIntersection"] for x in data["boxNeighborProbes"]),
      "unity":"NOT_RUN"}))

if __name__=="__main__":main()
