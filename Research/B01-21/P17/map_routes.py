#!/usr/bin/env python3
"""B01-21 MAP-ROUTES-001 v1: real deterministic 3-lane navigation overlay
from ORIGINAL MAPGEN001-generated geometry, not a new map generator.

stdlib only. Produces source-space player/AI waypoint *suggestions*, NOT
actual Unity steering, NPC behaviour, PhysX swept collision or loot logic.
"""
from __future__ import annotations
import argparse, base64, hashlib, heapq, json, math
from pathlib import Path
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "Research/B01-21/P16/generated"
OUTPUT = Path(__file__).resolve().parent / "generated"
SOURCE_BLOBS = {
    "AR16_01_OPEN_FLANK": "fce7cdce22fa9d9f512c8cd1d112cf1d83ad205e",
    "AR16_02_SPLIT_DECISION": "022b5c0cd1e13ca64460a3d6a0ab0122c4c2c790",
    "AR16_03_DENSE_PRESSURE": "1fe7bf8b124d999de75c0fd3cacc85ee62319429",
}
SOURCE_GAME = "589b60a912096d1e9917cb0bbac73a53e7543b4f"
# Historical references from MAPGENWORLD W12 (not a new rig measurement).
PROFILES = {
    "Light": {"width": 2.980, "length": 3.2, "minTurnRadius": 3.0},
    "Heavy": {"width": 5.180, "length": 5.8, "minTurnRadius": 6.0},
}
MARGIN = 0.25
START, FINISH = (0, -25), (0, 25)
# Waypoints express useful tactical lanes rather than random seed variations.
LANES = {
    "MAIN": ((0, -12), (0, 0), (0, 12)),
    "WEST_FLANK": ((-3, -12), (-3, 0), (-3, 12)),
    "EAST_FLANK": ((3, -12), (3, 0), (3, 12)),
}
CARDINAL = ((0, 1), (-1, 0), (1, 0), (0, -1))
EXPECTED_COVER_COUNTS = (6, 11, 16)


def git_blob(data: bytes) -> str:
    return hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()


def stable_json(data) -> str:
    return json.dumps(data, ensure_ascii=False, indent=2, sort_keys=True) + "\n"


def load_original(name: str, path: Path | None = None) -> dict:
    path = path or SOURCE / (name + ".blueprint.json")
    raw = path.read_bytes()
    if git_blob(raw) != SOURCE_BLOBS[name]:
        raise ValueError("SOURCE_GIT_BLOB_CHANGED:" + name)
    d = json.loads(raw)
    if (d.get("schema") != "B01-21-P16-ACTUAL-MAPGEN001-BLUEPRINT-v1"
        or d.get("id") != name or d.get("recipe") != "CombatArena"
        or d.get("originalGameSha") != SOURCE_GAME
        or not isinstance(d.get("elements"), list)):
        raise ValueError("UNKNOWN_MAPGEN_BLUEPRINT_CONTRACT:" + name)
    if d.get("start", {}).get("x") != START[0] or d["start"].get("z") != START[1]:
        raise ValueError("SOURCE_SPAWN_MOVED")
    if d.get("finish", {}).get("x") != FINISH[0] or d["finish"].get("z") != FINISH[1]:
        raise ValueError("SOURCE_FINISH_MOVED")
    if not isinstance(d.get("geometryHash"), str) or len(d["geometryHash"]) != 64:
        raise ValueError("MISSING_GEOMETRY_HASH")
    return d


def free_on_original_geometry(plan: dict, point: tuple[int, int], profile: dict) -> bool:
    """Conservative original-data XZ axis-aligned clearance for 3×3 chassis probes.

    Mirroring the source validator's conservative predicate for our zero-pitch
    CombatArena, not asserting equivalence to Unity collision or R6 swept turns.
    """
    x, z = point
    hw = profile["width"] * 0.5 + MARGIN
    hl = profile["length"] * 0.5 + MARGIN
    floors = [e for e in plan["elements"] if e["kind"] == "Floor"]
    if not floors:
        return False
    for u in (-1, 0, 1):
        for v in (-1, 0, 1):
            px, pz = x + u*hw, z + v*hl
            if not any(abs(e["position"]["x"] - px) <= e["size"]["x"]/2
                       and abs(e["position"]["z"] - pz) <= e["size"]["z"]/2
                       for e in floors):
                return False
    for e in plan["elements"]:
        if e["kind"] == "Floor":
            continue
        if e["kind"] not in ("Cover", "Boundary"):
            raise ValueError("UNSUPPORTED_SOLID_KIND:" + str(e["kind"]))
        if e.get("pitchDegrees", 0) != 0:
            raise ValueError("UNSUPPORTED_SOLID_ROTATION")
        if (abs(e["position"]["x"] - x) < e["size"]["x"]/2 + hw
            and abs(e["position"]["z"] - z) < e["size"]["z"]/2 + hl):
            return False
    return True


def shortest_to(plan: dict, source: tuple[int, int], dest: tuple[int, int],
                profile: dict) -> list[tuple[int, int]]:
    if not free_on_original_geometry(plan, source, profile):
        raise ValueError("ORIGINAL_SOURCE_START_BLOCKED")
    if not free_on_original_geometry(plan, dest, profile):
        raise ValueError("ORIGINAL_SOURCE_WAYPOINT_BLOCKED:" + str(dest))
    q: list[tuple[int, int, int, int, tuple[int, int]]] = []
    serial = 0
    # Integer weighted A*. Tiny lane penalty breaks ties deterministically
    # toward the waypoint, but must never override solid-geometry rejection.
    best = {source: 0}
    prev: dict[tuple[int, int], tuple[int, int]] = {}
    heapq.heappush(q, (1000*(abs(source[0]-dest[0])+abs(source[1]-dest[1])), 0, serial, 0, source))
    visited = 0
    while q:
        _, g, _, _, p = heapq.heappop(q)
        if best.get(p) != g:
            continue
        visited += 1
        if visited > 5000:
            raise ValueError("ROUTE_SEARCH_LIMIT")
        if p == dest:
            out = [p]
            while out[-1] != source:
                out.append(prev[out[-1]])
            out.reverse()
            return out
        for dx, dz in CARDINAL:
            n = (p[0]+dx, p[1]+dz)
            if not (-29 <= n[0] <= 29 and -29 <= n[1] <= 29):
                continue
            if not free_on_original_geometry(plan, n, profile):
                continue
            penalty = abs(n[0]-dest[0])*2
            ng = g + 1000 + penalty
            if ng < best.get(n, 1<<60):
                best[n] = ng
                prev[n] = p
                serial += 1
                dist = abs(n[0]-dest[0])+abs(n[1]-dest[1])
                heapq.heappush(q,(ng+dist*1000,ng,serial,dist,n))
    raise ValueError("NO_ROUTE_TO_WAYPOINT:" + str(dest))


def simplified(poly: list[tuple[int, int]]) -> list[tuple[int, int]]:
    if len(poly) < 3:
        return poly
    out = [poly[0]]
    for i in range(1,len(poly)-1):
        prev = poly[i-1]
        here = poly[i]
        nxt = poly[i+1]
        if (here[0]-prev[0],here[1]-prev[1]) != (nxt[0]-here[0],nxt[1]-here[1]):
            out.append(here)
    out.append(poly[-1])
    return out


def route(plan: dict, lane: str, profile_name: str) -> dict:
    profile = PROFILES[profile_name]
    waypoints = [START, *LANES[lane], FINISH]
    cells = [waypoints[0]]
    for target in waypoints[1:]:
        cells.extend(shortest_to(plan, cells[-1], target, profile)[1:])
    if any(not free_on_original_geometry(plan, cell, profile) for cell in cells):
        raise ValueError("GRID_CLEARANCE_MISMATCH")
    if any(abs(a[0]-b[0])+abs(a[1]-b[1]) != 1 for a,b in zip(cells,cells[1:])):
        raise ValueError("DISCONNECTED_GRID_ROUTE")
    return {
        "routeId": lane,
        "referenceProfile": profile_name,
        "sourceVehicleWidth": profile["width"],
        "sourceVehicleLength": profile["length"],
        "sourceMinTurnRadius": profile["minTurnRadius"],
        "clearancePerSideMeters": MARGIN,
        "start": {"x":START[0],"z":START[1]},
        "finish": {"x":FINISH[0],"z":FINISH[1]},
        "intermediateGoals": [{"x":p[0],"z":p[1]} for p in LANES[lane]],
        "gridPath": [{"x":p[0],"z":p[1]} for p in cells],
        "simplifiedWaypointHints": [{"x":p[0],"z":p[1]} for p in simplified(cells)],
        "gridStepsAt1m": len(cells)-1,
        "sourceFloorAndAabbClear": True,
        "sweptTurnAndPhysxCertificate": False,
    }


def overlay(plan: dict, name: str) -> dict:
    routes = [route(plan,lane,profile)
              for profile in PROFILES for lane in LANES]
    for profile in PROFILES:
        trio=[r for r in routes if r["referenceProfile"]==profile]
        cellsets=[{(p["x"],p["z"]) for p in r["gridPath"]} for r in trio]
        if not(cellsets[1]-cellsets[0] and cellsets[2]-cellsets[0] and cellsets[1]!=cellsets[2]):
            raise ValueError("NO_MEANINGFUL_LEFT_RIGHT_ALTERNATIVE")
        if not (trio[0]["gridStepsAt1m"]<=trio[1]["gridStepsAt1m"] and
                trio[0]["gridStepsAt1m"]<=trio[2]["gridStepsAt1m"]):
            raise ValueError("MAIN_ROUTE_LONGER_THAN_FLANK")
    return {
        "schema":"B01-21-P17-MAP-ROUTES-001-v1",
        "packageId":"B01-21-MAP-ROUTES-001",
        "sourceLayoutId": name,
        "sourceBlueprintGitBlob": SOURCE_BLOBS[name],
        "sourceOriginalGameCommit": SOURCE_GAME,
        "sourceMapGenGeometryHash": plan["geometryHash"],
        "seed":plan["seed"],
        "worldUnits":"metres",
        "coordinateSystem":"MAPGEN001 source local XZ; Y not used for grid steering",
        "routes": routes,
        "executionScope":"source-only original-data XZ grid with historical profiles",
        "nativeUnity":"NOT_RUN",
        "currentAssembledRigMeasured":False,
        "codexIntegration":"PENDING",
        "enemyAiOrRewardsChanged":False,
    }


def svg(plan: dict, data: dict) -> str:
    out=['<?xml version="1.0" encoding="UTF-8"?>',
         '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1060 710" role="img">',
         '<rect width="1060" height="710" fill="#101c28"/>',
         '<g transform="translate(56 96)">',
         '<rect width="512" height="512" fill="#324651"/>']
    # 8 px / metre; source 64m square x=-32..32, z=+32..-32.
    for e in plan["elements"]:
        if e["kind"] == "Floor": continue
        w,h = e["size"]["x"]*8,e["size"]["z"]*8
        x=(e["position"]["x"]+32)*8-w/2
        y=(32-e["position"]["z"])*8-h/2
        color="#c39273" if e["kind"]=="Boundary" else "#af6251"
        out.append(f'<rect data-solid="{e["kind"]}" x="{x:.3f}" y="{y:.3f}" width="{w:.3f}" height="{h:.3f}" fill="{color}"/>')
    palette={"MAIN":"#55f3c9","WEST_FLANK":"#f5da7b","EAST_FLANK":"#f18faa"}
    for lane in LANES:
        # Heavy is conservative reference. Use same file to show three real routes.
        path=next(r for r in data["routes"] if r["referenceProfile"]=="Heavy" and r["routeId"]==lane)
        pts=" ".join(f'{(p["x"]+32)*8:.1f},{(32-p["z"])*8:.1f}' for p in path["gridPath"])
        out.append(f'<polyline data-route="{lane}" points="{pts}" fill="none" stroke="{palette[lane]}" stroke-width="3" stroke-linejoin="round" opacity=".9"/>')
    for kind,p,color in (("start",START,"#4affb2"),("finish",FINISH,"#ffe97e")):
        out.append(f'<circle data-marker="{kind}" cx="{(p[0]+32)*8:.1f}" cy="{(32-p[1])*8:.1f}" r="8" fill="{color}" stroke="white" stroke-width="2"/>')
    out.append('</g>')
    out.append(f'<text x="56" y="56" font-family="sans-serif" font-size="24" font-weight="bold" fill="white">{escape(data["sourceLayoutId"])} — map navigation overlay</text>')
    out.append('<g fill="#dae8f0" font-family="sans-serif" font-size="18">')
    out.append('<text x="605" y="145">Original MAPGEN001 local XZ, 64×64m</text>')
    for i, lane in enumerate(LANES):
        p=next(r for r in data["routes"] if r["referenceProfile"]=="Heavy" and r["routeId"]==lane)
        out.append(f'<text x="605" y="{210+i*46}" fill="{palette[lane]}">{lane}: {p["gridStepsAt1m"]} grid metres</text>')
    out.extend(['<text x="605" y="426" font-size="16">Conservative source AABB / floor checked</text>',
                '<text x="605" y="458" font-size="16">Historical Heavy ref: 5.180×5.8m</text>',
                '<text x="605" y="490" font-size="16">PhysX swept turn radius: NOT_RUN</text>',
                '<text x="605" y="522" font-size="16">Game integration / NPC steering: NO</text>',
                '</g>',
                '<text x="56" y="675" font-family="sans-serif" font-size="13" fill="#aebfcd">B01-21 P17 • exact immutable original MAPGEN001 blueprint • standalone route data only</text>',
                '</svg>'])
    return "\n".join(out)+"\n"


def render_all() -> dict[str,str]:
    result={}
    layouts=[]
    for name in SOURCE_BLOBS:
        plan=load_original(name)
        d=overlay(plan,name)
        layouts.append({"id":name,"seed":plan["seed"],
                        "geometryHash":plan["geometryHash"],
                        "routes":len(d["routes"]),
                        "coverCount":sum(e["kind"]=="Cover" for e in plan["elements"])})
        result[name+".routes.json"]=stable_json(d)
        result[name+".routes.svg"]=svg(plan,d)
    if tuple(x["coverCount"] for x in layouts)!=EXPECTED_COVER_COUNTS:
        raise ValueError("SOURCE_COVER_DENSITY_DRIFT")
    result["manifest.json"]=stable_json({
        "schema":"B01-21-P17-MAP-ROUTES-001-MANIFEST-v1",
        "sourceBlueprintGitBlobs":SOURCE_BLOBS,
        "originalMAPGENSourceGameSha":SOURCE_GAME,
        "nativeUnity":"NOT_RUN",
        "physx":"NOT_RUN",
        "numberOfUniqueLayouts":len(layouts),
        "numberOfDistinctProfileLaneRoutes":len(layouts)*len(LANES)*len(PROFILES),
        "layouts":layouts,
    })
    return result


def main() -> int:
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check",action="store_true",help="verify byte-identical generated files")
    parser.add_argument("--emit-base64",action="store_true",help="CI evidence only: print output bytes as base64")
    args=parser.parse_args()
    data=render_all()
    OUTPUT.mkdir(parents=True,exist_ok=True)
    for name,content in sorted(data.items()):
        path=OUTPUT/name
        if args.check:
            if not path.exists() or path.read_bytes()!=content.encode("utf-8"):
                raise ValueError("NON_DETERMINISTIC_OR_MISSING_GENERATED_FILE:"+name)
            print("MAP_ROUTES_P17_BYTE_IDENTICAL",name)
        else:
            path.write_bytes(content.encode("utf-8"))
            print("MAP_ROUTES_P17_GENERATED",name)
        if args.emit_base64:
            print("MAP_ROUTES_P17_FILE_B64 | "+name+" | "+base64.b64encode(content.encode("utf-8")).decode("ascii"))
    print("MAP_ROUTES_P17_PASS 3 blueprints, 2 historical envelopes, 3 meaningful alternative lanes each; 18 total routes; PhysX NOT_RUN")
    return 0


if __name__=="__main__":
    raise SystemExit(main())
