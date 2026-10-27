#!/usr/bin/env python3
"""B01-21 read-only CHL003 course-compatibility receipt verifier.
This NEVER asserts Unity engine or CHL activity/controller acceptance.
Usage: python3 verify_receipt.py <actual-dotnet-generated-JSON>
"""
import json
import math
import re
import sys
from pathlib import Path

EXPECTED_SHA="5be31039411f89f17654b1c332f8692883fa03cc"
EXPECTED={(s,n,p) for s in (7,31,83) for n in (3,4,5) for p in
          ("LIGHT_MEASURED","HEAVY_MEASURED")}
NARROW={(s,p) for s in (2,12,14,22,28) for p in
        ("LIGHT_MEASURED","HEAVY_MEASURED")}

class ReceiptInvalid(ValueError):
    pass

def demand(ok,msg):
    if not ok: raise ReceiptInvalid(msg)

def n3(v,name):
    demand(type(v)==list and len(v)==3 and
           all(type(x) in (int,float) and math.isfinite(x) for x in v),
           "BAD_POINT_"+name)

def verify(x):
    demand(type(x)==dict,"ROOT_NOT_OBJECT")
    demand(x.get("producerPinnedSha")==EXPECTED_SHA,"PRODUCER_SHA_MISMATCH")
    demand(x.get("authority")=="ACTUAL_FROZEN_MAPGEN_PLANNER_VALIDATOR_WITH_UNITY_MATH_SURROGATE",
           "FALSE_RUNTIME_AUTHORITY")
    demand(x.get("nativeUnity")=="NOT_RUN" and x.get("realVehicleDrive")=="NOT_RUN",
           "UNSUPPORTED_NATIVE_PASS")
    demand(x.get("passed")==30 and x.get("failed")==0 and x.get("total")==30,
           "SOURCE_CASE_COUNT")
    rows=x.get("sourceCases")
    demand(type(rows)==list and len(rows)==30,"CASE_LIST_COUNT")
    positives=set(); negatives=set(); blocked=0; order=0
    for row in rows:
        demand(type(row)==dict and row.get("kind") in
               ("COURSE_BASELINE","COURSE_NARROW_REJECT","INJECTED_ROUTE_BLOCK",
                "CHECKPOINT_ORDER_NEGATIVE"),"UNKNOWN_CASE_KIND")
        kind=row["kind"]
        if kind=="COURSE_BASELINE":
            key=(row.get("seed"),row.get("segments"),row.get("rig"))
            demand(key in EXPECTED and key not in positives,"DUPLICATE_OR_BAD_POSITIVE")
            positives.add(key)
            heavy=key[2]=="HEAVY_MEASURED"
            demand(row.get("widthMeters") == (5.18 if heavy else 2.98),
                   "NOMINAL_OR_FORGED_WIDTH")
            demand(row.get("lengthMeters")==(5.8 if heavy else 3.2),
                   "RIG_LENGTH_MISMATCH")
            demand(row.get("turnRadiusMeters")==(6 if heavy else 3),
                   "RIG_TURN_MISMATCH")
            demand(row.get("minSideMarginMeters")==.25 and row.get("laneMeters")==16,
                   "MARGIN_OR_LANE_MISMATCH")
            demand(row.get("routeCode")=="BFS_SPINE_CLEARANCE_OK" and
                   row.get("validatorExploredCells",0)>0,"ROUTE_NOT_VALIDATED")
            demand(row.get("recipe")=="ObstacleCourse" and
                   row.get("generationVersion")==1,"WRONG_RECIPE_OR_VERSION")
            h=row.get("geometryHash")
            demand(type(h)==str and re.fullmatch("[0-9a-f]{64}",h) is not None,
                   "HASH_INVALID")
            n3(row.get("start"),"START");n3(row.get("finish"),"FINISH")
            cp=row.get("checkpoints")
            demand(type(cp)==list and len(cp)==key[1],"CHECKPOINT_COUNT")
            for i,point in enumerate(cp):
                n3(point,"CHECKPOINT")
                demand(abs(point[2]-(13+16*i))<.001,"CHECKPOINT_ORDER_OR_POSITION")
            demand(abs(row["start"][2]-5)<.001 and
                   abs(row["finish"][2]-(16*key[1]-3))<.001,
                   "START_FINISH_Z_MISMATCH")
            demand(row.get("actualUnityPhysX")=="NOT_RUN" and
                   row.get("actualRigidbodyTraversal")=="NOT_RUN",
                   "FALSE_NATIVE_PHYSX")
        elif kind=="COURSE_NARROW_REJECT":
            key=(row.get("seed"),row.get("rig"))
            demand(key in NARROW and key not in negatives,"BAD_NEGATIVE")
            negatives.add(key)
            demand(row.get("segments")==3 and row.get("laneMeters")==10 and
                   row.get("widthMeters")==(5.18 if key[1]=="HEAVY_MEASURED" else 2.98),
                   "NARROW_PROFILE_MISMATCH")
            demand(row.get("rejectedReason")=="IMPOSSIBLE_COURSE_PROFILE" and
                   row.get("actualUnityPhysX")=="NOT_RUN",
                   "NARROW_NOT_REJECTED")
        elif kind=="INJECTED_ROUTE_BLOCK":
            blocked+=1
            demand(row.get("seed")==7 and
                   row.get("rejectedReason")=="SPINE_FOOTPRINT_BLOCKED",
                   "OBSTACLE_NOT_REJECTED")
            for k in ("oldGeometryHash","newGeometryHash"):
                demand(type(row.get(k))==str and re.fullmatch("[0-9a-f]{64}",row[k]),
                       "OBSTACLE_HASH")
            demand(row["newGeometryHash"]!=row["oldGeometryHash"],
                   "OBSTACLE_GEOMETRY_NOT_CHANGED")
            demand(row.get("actualUnityPhysX")=="NOT_RUN","FALSE_NATIVE_OBSTACLE")
        elif kind=="CHECKPOINT_ORDER_NEGATIVE":
            order+=1
            demand(row.get("seed")==31 and row.get("attemptedIndexOrder")==[0,2,1,3]
                   and row.get("expectedAcceptedPrefix")==1 and
                   row.get("actualCHLController")=="NOT_RUN",
                   "CHECKPOINT_PRECONDITION_INVALID")
    demand(positives==EXPECTED and negatives==NARROW and blocked==1 and order==1,
           "RECEIPT_INCOMPLETE")
    return {"verified":True,"positive":len(positives),"narrowNegative":len(negatives),
            "injectedBlock":blocked,"outOfOrderFixture":order,
            "nativeUnity":"NOT_RUN","chlController":"NOT_RUN"}

def main():
    if len(sys.argv)!=2:
        print("usage: verify_receipt.py <json>",file=sys.stderr);return 3
    try:
        data=json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
        receipt=verify(data)
    except (OSError,ValueError,TypeError) as e:
        print("CHL003_WORLD_RECEIPT_REJECT "+str(e),file=sys.stderr)
        return 2
    print("CHL003_WORLD_RECEIPT_VERIFY_PASS "+json.dumps(receipt,sort_keys=True))
    return 0

if __name__=="__main__":sys.exit(main())
