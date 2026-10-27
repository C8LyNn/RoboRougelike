#!/usr/bin/env python3
"""B01-21 N27-21: source-backed CHL longitudinal route / gate occupancy QA.
Computes strict original authoring geometry only; NO actual Unity, contacts, PhysX.
"""
import argparse
import json
import os
import math
import re
from pathlib import Path

REPO = Path(__file__).resolve().parents[4]
PRODUCER_SHA = "077e692785aff2990a2d72a80fd1db62ac57b0e8"
DEMO_BLOB = "9308ceb9eab89d1ab483b560ff3ed58fa6d2d32e"
RUNTIME_GATE_BLOB = "5c736da620487e48131f6d454bcf231ae368b513"
DEMO = Path(os.environ["B01_21_CURRENT_CHL_DEMO"]) if "B01_21_CURRENT_CHL_DEMO" in os.environ else REPO / "Assets/RoboRougelikeChallenges/CHL003/Demo/CHL003DemoBootstrap.cs"
GATE = Path(os.environ["B01_21_CURRENT_CHL_GATE"]) if "B01_21_CURRENT_CHL_GATE" in os.environ else REPO / "Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003GateCrossing.cs"

def require(value, problem):
    if not value:
        raise ValueError(problem)

def triple(raw):
    values = tuple(float(v.strip().removesuffix("f")) for v in raw.split(","))
    require(len(values) == 3 and all(math.isfinite(x) for x in values), "INVALID_VECTOR3")
    return values

def geometry(source, gate_source):
    for token in (
        'new Vector3(3.70f, 4.5f, .16f)',
        'new Vector3(side * 1.97f, 1.3f, z)',
        'new Vector3(2.5f, .65f, 3.4f)',
        'new Vector3(1.45f, .52f, 2.3f)',
        'float width = heavy ? 1.0f : .59f;',
        'new Vector3(.82f, .82f, .82f)',
        'end * (heavy ? 1.15f : .75f)',
        'RigidbodyConstraints.FreezeRotationX',
        'RigidbodyConstraints.FreezeRotationZ',
    ):
        require(token in source, "PRODUCER_GEOMETRY_DRIFT:" + token)
    for token in ("minimumSideClearance = .25f",
                  "currentDepth - previousDepth >= .005f",
                  "localMinX >= gateMinX + minimumSideClearance"):
        require(token in gate_source, "PRODUCER_GATE_POLICY_DRIFT:" + token)
    names = ("APPROACH_GROUND", "REAL_LOW_BARRIER", "SOLID_RAMP",
             "NARROW_SOLID_BRIDGE", "FINAL_PLATFORM")
    solids = {}
    for name in names:
        pattern = (r'Box\("' + name + r'",\s*new Vector3\(([^)]+)\),\s*'
                   r'new Vector3\(([^)]+)\)(?:,\s*(-?\d+)f)?\);')
        m = re.search(pattern, source)
        require(m is not None, "MISSING_OR_CHANGED_BOX:" + name)
        center, size = triple(m[1]), triple(m[2])
        require(all(x > 0 for x in size), "NON_POSITIVE_SIZE:" + name)
        solids[name] = {
            "centerM": center, "sizeM": size,
            "pitchDeg": float(m[3]) if m[3] else 0.0,
        }
    checkpoints = re.findall(
        r'Gate\("(CP[0-3]|FINISH)",\s*(\d+(?:\.\d+)?)f,\s*'
        r'CHL003Gate\.GateType\.(?:Checkpoint|Finish)\)', source)
    require([x[0] for x in checkpoints] ==
            ["CP0", "CP1", "CP2", "CP3", "FINISH"], "GATE_ORDER_DRIFT")
    require([float(x[1]) for x in checkpoints] == [5, 11.5, 21, 29, 38],
            "GATE_LOCATION_DRIFT")
    require(solids["NARROW_SOLID_BRIDGE"]["sizeM"] == (3.70, .60, 10),
            "BRIDGE_CHANGED")
    require(solids["SOLID_RAMP"]["sizeM"] == (3.7, .55, 7),
            "RAMP_CHANGED")
    require(solids["SOLID_RAMP"]["pitchDeg"] == -12,
            "RAMP_PITCH_CHANGED")
    return solids, checkpoints

def box_top_and_span(item):
    c, s = item["centerM"], item["sizeM"]
    require(item["pitchDeg"] == 0, "ROTATED_BOX_REQUIRES_RAMP_FORMULA")
    return {"zStartM": c[2]-s[2]/2, "zEndM": c[2]+s[2]/2,
            "topY_M": c[1]+s[1]/2, "widthM": s[0]}

def top_ramp_endpoints(item):
    c, s = item["centerM"], item["sizeM"]
    a = math.radians(item["pitchDeg"])
    return [{
        "zM": c[2] + math.sin(a)*s[1]/2 + math.cos(a)*dz,
        "topY_M": c[1] + math.cos(a)*s[1]/2 - math.sin(a)*dz,
    } for dz in (-s[2]/2, s[2]/2)]

def halfwidth(rig, yaw_degrees):
    a = math.radians(yaw_degrees)
    c, s = math.cos(a), math.sin(a)
    hull_w, hull_l, xwheel, zwheel = (
        (2.5, 3.4, 1., 1.15) if rig == "Heavy" else
        (1.45, 2.3, .59, .75))
    return max(hull_w*.5*c+hull_l*.5*s,
               xwheel*c+zwheel*s+.41)

def half_length(rig, yaw_degrees):
    a = math.radians(yaw_degrees)
    c, s = math.cos(a), math.sin(a)
    hull_w, hull_l, xwheel, zwheel = (
        (2.5, 3.4, 1., 1.15) if rig == "Heavy" else
        (1.45, 2.3, .59, .75))
    return max(hull_l*.5*c+hull_w*.5*s,
               zwheel*c+xwheel*s+.41)

def analyze(source, gate_source):
    solids, gates = geometry(source, gate_source)
    ground = box_top_and_span(solids["APPROACH_GROUND"])
    bridge = box_top_and_span(solids["NARROW_SOLID_BRIDGE"])
    final = box_top_and_span(solids["FINAL_PLATFORM"])
    ramp_a, ramp_b = top_ramp_endpoints(solids["SOLID_RAMP"])
    zones = {"CP0": ("GROUND", ground), "CP1": ("GROUND", ground),
             "CP2": ("BRIDGE", bridge), "CP3": ("BRIDGE_TO_FINAL", bridge),
             "FINISH": ("FINAL", final)}
    spacing = []
    rows = []
    for rig in ("Light", "Heavy"):
        for yaw in (0, 8):
            hw, hl = halfwidth(rig,yaw), half_length(rig,yaw)
            for name, zraw in gates:
                z = float(zraw)
                zone, support = zones[name]
                surface = support["topY_M"]
                # Ideal flat-surface contact kinematics, not real Unity.
                # Wheel local y=-.52; wheel radius=.41 => rootY=surface+.93.
                ideal_root_y = surface + .93
                hull_top_y = ideal_root_y +(-.12+(.325 if rig == "Heavy" else .26))
                wheel_top_y = ideal_root_y - .52 + .41
                max_y = max(hull_top_y, wheel_top_y)
                center_margin_z = min(z-support["zStartM"],
                                      support["zEndM"]-z)
                # Gate plane 0.16m thick; full longitudinal collider is
                # overlapping for 2*(halfLength + .08) along Z.
                overlap_m = 2*(hl+.08)
                # Source MaxHorizontalSpeed=12m/s and typical default
                # Time.fixedDeltaTime=.02s (not a verified running physics config).
                estimated_overlap_ticks = overlap_m / (12 * .02)
                rows.append({
                    "rig": rig, "yawDeg": yaw, "gate":name,
                    "gateCenterZ_M":z, "surface":zone,
                    "surfaceZ_M":[support["zStartM"],support["zEndM"]],
                    "idealWheelSupportedRootY_M":round(ideal_root_y,6),
                    "idealActorUpperY_M":round(max_y,6),
                    "fitsGateVerticalInIdealFlatPose":max_y<=4.5 and surface>=0,
                    "gateHalfWidthM":1.85,
                    "sideClearanceCenteredM":round(1.85-hw,6),
                    "meetsSourceSideGuard025Centered":1.85-hw>=.25,
                    "wholeBodyLongitudinalHalfExtentM":round(hl,6),
                    "staticLongitudinalGateTriggerContactWindowM":round(overlap_m,6),
                    "idealTickWindowsAt12mps50Hz_NOT_NATIVE":round(estimated_overlap_ticks,6),
                    "platformEndMarginFromFrontHullAtGateCenterM":round(
                        support["zEndM"]-z-hl,6),
                    "sourceStage":"STATIC_SOURCE_ONLY",
                })
    for (name1,z1),(name2,z2) in zip(gates,gates[1:]):
        spacing.append({"from":name1,"to":name2,"centerSpacingM":float(z2)-float(z1)})
    report = {
        "taskId":"N27-B01-21-CHL-SOURCE-ROUTE-20261027",
        "producerInputSHA":PRODUCER_SHA,
        "actualDemoGitBlob":DEMO_BLOB,
        "actualGatePolicyGitBlob":RUNTIME_GATE_BLOB,
        "nativeUnity":"NOT_RUN",
        "realWheelContact":"NOT_RUN",
        "actualMotorDrivenLight3Heavy3":"NOT_RUN",
        "assumption":"Hypothetical flat support, ideal wheel centers, authored 12mps max and 50Hz illustration; no Unity state assertion",
        "sourceGeometry":solids,
        "groundTopSpan":ground,
        "rampTopStart":ramp_a,
        "rampTopExit":ramp_b,
        "bridgeTopSpan":bridge,
        "finalTopSpan":final,
        "roadJointsM":{
          "groundToRampPlanarOverlap":round(ground["zEndM"]-ramp_a["zM"],6),
          "groundToRampEntranceLip":round(ramp_a["topY_M"]-ground["topY_M"],6),
          "rampExitToBridgeStartGap":round(bridge["zStartM"]-ramp_b["zM"],6),
          "rampExitToBridgeTopLip":round(bridge["topY_M"]-ramp_b["topY_M"],6),
          "bridgeToFinalPlanarOverlap":round(bridge["zEndM"]-final["zStartM"],6),
        },
        "checkpointSpacingM":spacing,
        "gateProfiles":rows,
        "sourceOnlyVerdict":"STATIC_CORRIDOR_POSITIONS_CONSISTENT; PHYSICAL_COMPLETION_UNVERIFIED",
    }
    require(len(rows)==20, "ROUTE_GRID_COUNT_DRIFT")
    require(report["roadJointsM"]["rampExitToBridgeStartGap"] > 0,
            "SLOPE_TO_BRIDGE_GAP_SIGN_DRIFT")
    require(.12 < report["roadJointsM"]["rampExitToBridgeStartGap"] < .15,
            "RAMP_BRIDGE_GAP_CHANGED")
    require(.14 < report["roadJointsM"]["rampExitToBridgeTopLip"] < .15,
            "RAMP_BRIDGE_LIP_CHANGED")
    require(report["roadJointsM"]["bridgeToFinalPlanarOverlap"] == .5,
            "BRIDGE_FINAL_TOUCH_CHANGED")
    for row in rows:
        require(row["fitsGateVerticalInIdealFlatPose"],
                "STATIC_GATE_VERTICAL_NOT_CONTAINED")
        # Light+Heavy yaw8 both fit lane when centered (owner's correction).
        require(row["meetsSourceSideGuard025Centered"],
                "SOURCE_GATE_WIDTH_FAILURE: "+row["gate"]+"/"+row["rig"])
    for rig in ("Heavy","Light"):
        f = next(v for v in rows if v["rig"]==rig
                 and v["yawDeg"]==0 and v["gate"]=="FINISH")
        require(f["platformEndMarginFromFrontHullAtGateCenterM"] > 0,
                "FINISH_FRONT_COLLIDER_EXCEEDS_PLATFORM")
    require([x["centerSpacingM"] for x in spacing]==[6.5,9.5,8,9],
            "COURSE_CHECKPOINT_ORDER_CHANGED")
    return report

def main():
    cli=argparse.ArgumentParser()
    cli.add_argument("--output",required=True)
    cli.add_argument("--demo-source",type=Path,default=DEMO)
    cli.add_argument("--gate-source",type=Path,default=GATE)
    args=cli.parse_args()
    report=analyze(args.demo_source.read_text(encoding="utf-8"),
                   args.gate_source.read_text(encoding="utf-8"))
    out=Path(args.output)
    out.parent.mkdir(parents=True,exist_ok=True)
    out.write_text(json.dumps(report,sort_keys=True,indent=2)+"\n",encoding="utf-8")
    print("B01_21_N27_CHL_ACTUAL_SOURCE_ROUTE PASS rows="+str(len(report["gateProfiles"]))
          +" SHA="+report["producerInputSHA"]
          +" native=NOT_RUN")

if __name__=="__main__":
    main()
