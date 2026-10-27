#!/usr/bin/env python3
"""B01-21 W17. Original CHL-003 source geometry QA, NOT Unity/PhysX.
No changes to CHL producer files; fail-closed original-source parsing.
"""
from __future__ import annotations
import argparse
import json
import math
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
DEMO = ROOT / "Assets/RoboRougelikeChallenges/CHL003/Demo/CHL003DemoBootstrap.cs"
CONTROLLER = ROOT / "Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003CourseController.cs"
MOTOR = ROOT / "Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003VehicleMotor.cs"
CROSSING = ROOT / "Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003GateCrossing.cs"
PRODUCER_HEAD = "9a947108928d4c2cdfdfbb25cbeb30464439da37"
DEMO_BLOB = "c74455dda6aa6c5210ebcce73330cf296b3597bd"
TARGET_MARGIN = 0.25
YAW_SAMPLES = (0, 5, 8, 10, 12, 15, 20, 25, 30)

def ensure(ok, problem):
    if not ok:
        raise ValueError(problem)

def num(s):
    return float(s.replace("f", ""))

def parse_box(source: str, key: str, tilted=False):
    pat = (r'Box\("' + re.escape(key) + r'",\s*new Vector3\('
        + r'([^)]*)\),\s*new Vector3\(([^)]*)\)'
        + (r',\s*([-\d.]+f)\);' if tilted else r'\);'))
    match = re.search(pat, source)
    ensure(match is not None, "MISSING_OR_CHANGED_PRIMITIVE_" + key)
    pos = [num(s.strip()) for s in match.group(1).split(",")]
    size = [num(s.strip()) for s in match.group(2).split(",")]
    ensure(len(pos) == len(size) == 3 and all(
        math.isfinite(t) and t>0 for t in size), "INVALID_PRIMITIVE_"+key)
    return {"center":pos,"size":size,
            "pitchDegrees":num(match.group(3)) if tilted else 0.0}

def parse_source(source: str, controller: str, motor: str):
    # Pin the ORIGINAL Unity authoring expressions, no standalone competing geometry.
    for snippet in (
        'new Vector3(2.5f, .65f, 3.4f)',
        'new Vector3(1.45f, .52f, 2.3f)',
        'float width = heavy ? 1.0f : .59f;',
        'new Vector3(.82f, .82f, .82f)',
        'end * (heavy ? 1.15f : .75f)',
        'RigidbodyConstraints.FreezeRotationX',
        'RigidbodyConstraints.FreezeRotationZ',
        'Box("FINAL_PLATFORM"',
        'Gate("FINISH", 38f, CHL003Gate.GateType.Finish)',
        'Gate("CP3", 29f, CHL003Gate.GateType.Checkpoint)',
    ):
        ensure(snippet in source,"CHANGED_RUNTIME_EXPRESSION: "+snippet)
    ensure("private bool PhysicallyOverlapsFinish()" in controller and
           "finishArbiter.TryCommit(" in controller,
           "FINISH_ARBITER_SOURCE_MISMATCH")
    ensure("body.AddForce(" in motor and "body.AddTorque(" in motor,
           "FORCE_MOTOR_SOURCE_MISMATCH")
    ensure("body.AddForce(" in motor and
           "course.IsPaused || body.isKinematic) return;" in motor,
           "MOVEMENT_SOURCE_MISMATCH")
    out={k:parse_box(source,k,k=="SOLID_RAMP")
         for k in ("APPROACH_GROUND","REAL_LOW_BARRIER","SOLID_RAMP",
                   "NARROW_SOLID_BRIDGE","FINAL_PLATFORM")}
    cplist=re.findall(r'Gate\("CP(\d+)",\s*([\d.]+)f,',source)
    ensure(cplist==[("0","5"),("1","11.5"),("2","21"),("3","29")],
           "CP_ORDER_CHANGED")
    out["checkpoints"]=[float(z) for _,z in cplist]
    out["finishZ"]=38.0
    out["triggerSize"]=[3.5,4.5,.16]
    ensure("new Vector3(3.5f, 4.5f, .16f)" in source,"TRIGGER_VOLUME_DRIFT")
    out["TRIGGER_APERTURE"]={"center":[0,2.25,0],"size":out["triggerSize"],"pitchDegrees":0.0}
    ensure(out["NARROW_SOLID_BRIDGE"]["size"]==[3.70,.60,10.],
           "W17_BRIDGE_GEOMETRY_CHANGED")
    ensure(out["SOLID_RAMP"]["size"]==[3.7,.55,7.] and
           out["SOLID_RAMP"]["pitchDegrees"]==-12.0,
           "W17_RAMP_GEOMETRY_CHANGED")
    return out

PROFILES = {
 "Light": {"massKg":270.0,"hullWidthM":1.45,"hullLengthM":2.30,
           "wheelXOffsetM":.59,"wheelZOffsetM":.75,"wheelRadiusM":.41},
 "Heavy": {"massKg":1150.0,"hullWidthM":2.50,"hullLengthM":3.40,
           "wheelXOffsetM":1.00,"wheelZOffsetM":1.15,"wheelRadiusM":.41},
}

def swept_halfwidth(profile, yaw_degrees):
    a=math.radians(abs(yaw_degrees))
    # Hull box corners, plus FOUR actual SPHERES. NOT a bounding box
    # over-estimate of the vehicle's complete unrotated footprint.
    box=.5*profile["hullWidthM"]*math.cos(a)+.5*profile["hullLengthM"]*math.sin(a)
    wheels=(profile["wheelXOffsetM"]*math.cos(a)+
            profile["wheelZOffsetM"]*math.sin(a)+profile["wheelRadiusM"])
    return max(box,wheels)

def section_aabb(box):
    x,y,z=box["center"];sx,sy,sz=box["size"]
    return {"x":[x-sx/2,x+sx/2],"z":[z-sz/2,z+sz/2],
            "topY":y+sy/2}

def ramp_upper_edges(box):
    # Unity Transform Quaternion.Euler(pitch,0,0), local top-side vertices.
    cx,cy,cz=box["center"];_,height,length=box["size"]
    a=math.radians(box["pitchDegrees"])
    return [{
        "end":name,"z":cz+math.sin(a)*(height/2)+math.cos(a)*dz,
        "topY":cy+math.cos(a)*(height/2)-math.sin(a)*dz
    } for name,dz in (("entrance",-length/2),("exit",length/2))]

def first_yaw_for_margin(profile, lane, margin):
    ensure((lane-2*swept_halfwidth(profile,0))/2 >= margin,
           "NO_STRAIGHT_BASELINE_CLEARANCE")
    lo,hi=0.0,45.0
    for _ in range(70):
        mid=(lo+hi)/2
        if lane/2-swept_halfwidth(profile,mid) >= margin:lo=mid
        else:hi=mid
    return lo

def analyze(demo,controller,motor):
    g=parse_source(demo,controller,motor)
    cross=CROSSING.read_text(encoding="utf-8")
    ensure("minimumSideClearance = .25f" in cross and
           "localMinX >= gateMinX + minimumSideClearance" in cross and
           "localMaxX <= gateMaxX - minimumSideClearance" in cross,
           "LATEST_PHYSICAL_CHECKPOINT_CLEARANCE_CONTRACT_DRIFT")
    laneNames=("SOLID_RAMP","NARROW_SOLID_BRIDGE","FINAL_PLATFORM","TRIGGER_APERTURE")
    rows=[]
    for rig,p in PROFILES.items():
        for angle in YAW_SAMPLES:
            width=2*swept_halfwidth(p,angle)
            for zone in laneNames:
                lane=g[zone]["size"][0]
                margin=(lane-width)/2
                rows.append({"rig":rig,"yawDeg":angle,"zone":zone,"projectedHullAndFourWheelsWidthM":round(width,6),
                    "laneWidthM":lane,"sideMarginIfCenteredM":round(margin,6),
                    "positiveWidthFit":margin>=0,
                    "meetsReviewMargin025EachSide":margin>=TARGET_MARGIN})
    ramp=ramp_upper_edges(g["SOLID_RAMP"])
    bridge=section_aabb(g["NARROW_SOLID_BRIDGE"])
    final=section_aabb(g["FINAL_PLATFORM"])
    cp=g["checkpoints"];finish=g["finishZ"];depth=g["triggerSize"][-1]
    result={
        "kind":"B01-21 W17 original CHL-003 SOURCE_ONLY geometry source fixture",
        "producerExactSourceHead":PRODUCER_HEAD,"producerDemoGitBlob":DEMO_BLOB,
        "unityPhysX":"NOT_RUN","actualRigidBodyTraversal":"NOT_RUN",
        "independentUnityG1":"NOT_RUN","notEquivalentToMAPGEN279":True,
        "profiles":PROFILES,"geometry":g,"rampUpperEdges":ramp,
        "bridgeAABB":bridge,"finalAABB":final,
        "rampExitToBridgeStartHorizontalM":round(bridge["z"][0]-ramp[1]["z"],6),
        "rampExitToBridgeTopVerticalM":round(bridge["topY"]-ramp[1]["topY"],6),
        "bridgeFinalPlatformOverlapZ_M":round(bridge["z"][1]-final["z"][0],6),
        "lastCheckpointFinishCentersDeltaM":finish-cp[-1],
        "lastCheckpointFinishTriggerGapM":round(finish-cp[-1]-depth,6),
        "yawSampleRows":rows,
        "reviewMarginEachSideM":TARGET_MARGIN,
        "bridgeHeavyMaxYawFor025SideM":round(first_yaw_for_margin(
            PROFILES["Heavy"],g["NARROW_SOLID_BRIDGE"]["size"][0],TARGET_MARGIN),6),
        "candidateNegative":"HEAVY yaw8° on 3.70m bridge has >=.25m each-side physical reserve yet may fail strict 3.50m Gate aperture .25m margin. Unity CP native NOT_RUN",
        "sourceOnlyVerdict":"GO_FOR_DISPOSABLE_NATIVE_FALSIFICATION_NOT_CODEX_READY"
    }
    ensure(result["lastCheckpointFinishCentersDeltaM"]==9,
           "ACTUAL_CHL_CP_FINISH_OVERLAP")
    ensure(0<result["rampExitToBridgeStartHorizontalM"]<.2,
           "RAMP_BRIDGE_GAP_UNKNOWN")
    ensure(.1<result["rampExitToBridgeTopVerticalM"]<.2,
           "RAMP_BRIDGE_VERTICAL_LIP_UNKNOWN")
    heavy0=next(x for x in rows if x["rig"]=="Heavy" and x["yawDeg"]==0 and x["zone"]=="NARROW_SOLID_BRIDGE")
    heavy12=next(x for x in rows if x["rig"]=="Heavy" and x["yawDeg"]==12 and x["zone"]=="NARROW_SOLID_BRIDGE")
    gate8=next(x for x in rows if x["rig"]=="Heavy" and x["yawDeg"]==8 and x["zone"]=="TRIGGER_APERTURE")
    bridge8=next(x for x in rows if x["rig"]=="Heavy" and x["yawDeg"]==8 and x["zone"]=="NARROW_SOLID_BRIDGE")
    ensure(bridge8["meetsReviewMargin025EachSide"] and
           gate8["positiveWidthFit"] and not gate8["meetsReviewMargin025EachSide"],
           "MISSING_ACTUAL_GATE_VS_BRIDGE_HEAVY_YAW_NEGATIVE")
    ensure(heavy0["meetsReviewMargin025EachSide"] and
           heavy12["positiveWidthFit"] and
           not heavy12["meetsReviewMargin025EachSide"],
           "MISSING_ACTUAL_HEAVY_YAW_NEGATIVE")
    return result

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--out",required=True)
    args=p.parse_args()
    report=analyze(DEMO.read_text(encoding="utf-8"),
                   CONTROLLER.read_text(encoding="utf-8"),
                   MOTOR.read_text(encoding="utf-8"))
    target=Path(args.out);target.parent.mkdir(parents=True,exist_ok=True)
    target.write_text(json.dumps(report,indent=2,sort_keys=True)+"\n",encoding="utf-8")
    print("CHL003_WORLD_W17_SOURCE_REPORT_PASS "
          "rows="+str(len(report["yawSampleRows"]))+
          " heavyYawLimit="+str(report["bridgeHeavyMaxYawFor025SideM"])+
          " nativeUnity=NOT_RUN")

if __name__=="__main__":main()
