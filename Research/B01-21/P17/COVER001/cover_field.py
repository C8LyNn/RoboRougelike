#!/usr/bin/env python3
"""B01-21 MAP-COVER-001: source-accurate 1.25 m horizontal LOS + reachable cover anchors.

Consumes immutable original MAPGEN001 CombatArena geometry through B01-21's
existing P17 map_routes source. Not a Unity NavMesh/Physics, enemy AI or 3D
occlusion implementation. Standard-library only; deterministic output.
"""
from __future__ import annotations
import argparse
import base64
import hashlib
import json
import math
from pathlib import Path
import sys
from xml.sax.saxutils import escape

P17 = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(P17))
import map_routes as routes

OUT = Path(__file__).resolve().parent / "generated"
PACKAGE = "B01-21-MAP-COVER-001"
EYE_Y = 1.25
GRID_STEP = 4
HEAVY = routes.PROFILES["Heavy"]
INPUT_IDS = tuple(routes.SOURCE_BLOBS)
BASE_SHA = "1971c819a99947553d7f28a104aa9601374b3b69"


def stable_json(obj: object) -> str:
    return json.dumps(obj, indent=2, sort_keys=True, ensure_ascii=False) + "\n"


def covers_in(plan: dict) -> list[dict]:
    allowed = {"Floor", "Boundary", "Cover"}
    found = []
    for element in plan["elements"]:
        kind = element.get("kind")
        if kind not in allowed:
            raise ValueError("UNSUPPORTED_MAP_SOLID:" + str(kind))
        if element.get("pitchDegrees") != 0:
            raise ValueError("UNSUPPORTED_MAP_ROTATION")
        if kind == "Cover":
            found.append(element)
    return found


def clip_original_aabb(start: tuple[float, float], end: tuple[float, float],
                       element: dict, eye_y: float = EYE_Y) -> bool:
    """Finite segment vs horizontal rectangle, only if 3D box covers test eye height.

    This is a direct static Box AABB sightline proxy, NOT actual raycast,
    PhysX visibility, muzzle/mesh/camera occlusion or shadow tracing.
    """
    size = element["size"]
    centre = element["position"]
    if not (centre["y"] - size["y"] / 2 <= eye_y <= centre["y"] + size["y"] / 2):
        return False
    near, far = 0.0, 1.0
    for j, axis in enumerate(("x", "z")):
        low = centre[axis] - size[axis] / 2
        high = centre[axis] + size[axis] / 2
        travel = end[j] - start[j]
        if abs(travel) < 1e-12:
            if start[j] < low or start[j] > high:
                return False
            continue
        a = (low - start[j]) / travel
        b = (high - start[j]) / travel
        if a > b:
            a, b = b, a
        near = max(near, a)
        far = min(far, b)
        if far < near:
            return False
    # Do not count a cover at the observer or at exact endpoint as blocking.
    return near < 1 and far > 0


def sight(plan: dict, position: tuple[int, int],
          original_covers: list[dict] | None = None) -> dict:
    items = original_covers if original_covers is not None else covers_in(plan)
    blocked = []
    for index, enemy in enumerate(plan["enemySpawnDescriptors"]):
        point = (enemy["x"], enemy["z"])
        hits = [j for j, cover in enumerate(items)
                if clip_original_aabb(point, position, cover)]
        blocked.append(hits)
    return {
        "visibleEnemyDescriptors": [i for i, hits in enumerate(blocked) if not hits],
        "occludedEnemyDescriptors": [i for i, hits in enumerate(blocked) if hits],
        "blockingCoverIndicesByEnemy": blocked,
    }


def heavy_free(plan: dict, pos: tuple[int, int]) -> bool:
    return routes.free_on_original_geometry(plan, pos, HEAVY)


def sample_exposure(plan: dict, original_covers: list[dict]) -> list[dict]:
    out = []
    for x in range(-24, 25, GRID_STEP):
        for z in range(-24, 25, GRID_STEP):
            pos = (x, z)
            if not heavy_free(plan, pos):
                continue
            perception = sight(plan, pos, original_covers)
            out.append({
                "x": x, "z": z,
                "visibleEnemyCount": len(perception["visibleEnemyDescriptors"]),
                "visibleEnemyDescriptors": perception["visibleEnemyDescriptors"],
                "occludedEnemyDescriptors": perception["occludedEnemyDescriptors"],
            })
    if not out:
        raise ValueError("NO_SOURCE_CLEAR_EXPOSURE_GRID")
    return out


def probe_for_cover(index: int, element: dict) -> list[tuple[int, int]]:
    x, z, size = element["position"]["x"], element["position"]["z"], element["size"]
    # Offset deliberately exceeds historical Heavy half envelope + 0.25m.
    # Candidate positions are only suggestions until original floor/AABB reachability.
    xstep = size["x"]/2 + HEAVY["width"]/2 + routes.MARGIN + 0.65
    zstep = size["z"]/2 + HEAVY["length"]/2 + routes.MARGIN + 0.65
    return [
        (round(x), math.floor(z-zstep)),
        (round(x), math.ceil(z+zstep)),
        (math.floor(x-xstep), round(z)),
        (math.ceil(x+xstep), round(z)),
    ]


def find_shelter_anchors(plan: dict, original_covers: list[dict]) -> list[dict]:
    occupied: set[tuple[int, int]] = set()
    result = []
    for cover_id, cover in enumerate(original_covers):
        options = []
        for pos in probe_for_cover(cover_id, cover):
            if pos in occupied or not heavy_free(plan, pos):
                continue
            vis = sight(plan, pos, original_covers)
            self_behind = [
                i for i, indices in enumerate(vis["blockingCoverIndicesByEnemy"])
                if cover_id in indices
            ]
            if not self_behind:
                continue
            try:
                path = routes.shortest_to(plan, routes.START, pos, HEAVY)
            except ValueError:
                # Fail closed on source reachability; never emit an unreachable cover.
                continue
            if len(path) < 2 or path[-1] != pos:
                raise ValueError("INVALID_ANCHOR_SOURCE_PATH")
            options.append((
                -len(self_behind),
                -len(vis["occludedEnemyDescriptors"]),
                len(path),
                pos[0], pos[1],
                {
                    "anchorId": "COVER_%02d" % (cover_id+1),
                    "coverSourceElementOrdinal": cover_id,
                    "sourceCoverPosition": cover["position"],
                    "sourceCoverSize": cover["size"],
                    "anchorLocalXZ": {"x":pos[0], "z":pos[1]},
                    "originalEnemyDescriptors": len(plan["enemySpawnDescriptors"]),
                    "visibleEnemyDescriptors": vis["visibleEnemyDescriptors"],
                    "occludedEnemyDescriptors": vis["occludedEnemyDescriptors"],
                    "directOcclusionByThisCover": self_behind,
                    "historicalHeavyReachabilityGridStepsFromStart": len(path)-1,
                    "historicalHeavyAccessTurnHints": [
                        {"x":p[0], "z":p[1]} for p in routes.simplified(path)
                    ],
                    "sourceAabbAndFloorClear": True,
                    "nativePhysxAnd3dCombatLineOfSight": "NOT_RUN",
                },
            ))
        if options:
            options.sort(key=lambda a: a[:5])
            best = options[0][-1]
            point = best["anchorLocalXZ"]
            occupied.add((point["x"], point["z"]))
            result.append(best)
    if len(result) < 2:
        raise ValueError("TOO_FEW_ACTUAL_SELF_OCCLUDING_REACHABLE_COVER_ANCHORS")
    return result


def original_sightline_field(plan: dict, layout_id: str) -> dict:
    if len(plan["enemySpawnDescriptors"]) != 3:
        raise ValueError("ENEMY_SPAWN_DESCRIPTOR_CONTRACT_CHANGED")
    covers = covers_in(plan)
    grid = sample_exposure(plan, covers)
    anchors = find_shelter_anchors(plan, covers)
    histogram = {
        str(i): sum(p["visibleEnemyCount"] == i for p in grid) for i in range(4)
    }
    return {
        "schema": "B01-21-P17-MAP-COVER-001-v1",
        "packageId": PACKAGE,
        "sourceLayoutId": layout_id,
        "sourceOriginalGameCommit": routes.SOURCE_GAME,
        "sourceBlueprintGitBlob": routes.SOURCE_BLOBS[layout_id],
        "sourceMapGenGeometryHash": plan["geometryHash"],
        "seed": plan["seed"],
        "coordinateFrame": "MAPGEN001 LOCAL XZ, metres, not Unity transformed world",
        "eyeHeightMeters": EYE_Y,
        "sourceOcclusionType": "axis-aligned 3D Box vs horizontal finite XZ segment",
        "enemySpawnDescriptors": plan["enemySpawnDescriptors"],
        "coverElementCount": len(covers),
        "sampleGridStepMetres": GRID_STEP,
        "historicalHeavyEnvelope": {
            "width": HEAVY["width"],
            "length": HEAVY["length"],
            "clearancePerSide": routes.MARGIN,
            "currentActualRigMeasured": False,
        },
        "visibilityGrid": grid,
        "visibleEnemyCountHistogram": histogram,
        "shelterAnchors": anchors,
        "limitations": {
            "sourceGeometryOnly": True,
            "ignoresCoverDamageAndDynamicObjects": True,
            "nativeUnityRaycast": "NOT_RUN",
            "nativePhysxVehicleAccess": "NOT_RUN",
            "gameplayAiOrCombatApplied": False,
            "integrated": False,
        }
    }


def svg_view(field: dict, plan: dict) -> str:
    px = lambda x: (x + 32)*8
    py = lambda z: (32-z)*8
    lines = ['<?xml version="1.0" encoding="utf-8"?>',
             '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1130 730" role="img">',
             '<rect width="1130" height="730" fill="#141c27"/>',
             '<text x="48" y="62" fill="#f5f7fa" font-family="sans-serif" font-size="25" font-weight="bold">'+escape(field["sourceLayoutId"])+' — source sightline field</text>',
             '<g transform="translate(64 104)">',
             '<rect width="512" height="512" fill="#34454f"/>']
    for p in field["visibilityGrid"]:
        t=p["visibleEnemyCount"]
        # 0 = shielded, 3 = fully exposed; small dots preserve exact original cover.
        shade = ("#44e4a8","#8fcfb2","#f5b76c","#f07a6f")[t]
        lines.append('<rect x="%.1f" y="%.1f" width="12" height="12" rx="3" fill="%s" opacity=".8"/>'%
                     (px(p["x"])-6,py(p["z"])-6,shade))
    for element in plan["elements"]:
        if element["kind"] == "Floor":
            continue
        x,z = element["position"]["x"],element["position"]["z"]
        w,h = element["size"]["x"]*8,element["size"]["z"]*8
        shade="#bd9f83" if element["kind"] == "Boundary" else "#a870bc"
        lines.append('<rect data-source-solid="%s" x="%.3f" y="%.3f" width="%.3f" height="%.3f" fill="%s" stroke="#19232e" stroke-width="1"/>'%
                     (element["kind"],px(x)-w/2,py(z)-h/2,w,h,shade))
    for anchor in field["shelterAnchors"]:
        p=anchor["anchorLocalXZ"]
        lines.append('<circle data-source-cover-anchor="%s" cx="%.1f" cy="%.1f" r="5" fill="#fff4ac" stroke="#111" stroke-width="2"/>'%
                     (anchor["anchorId"],px(p["x"]),py(p["z"])))
    for n,point in enumerate(field["enemySpawnDescriptors"]):
        lines.append('<circle data-enemy-descriptor="%d" cx="%.1f" cy="%.1f" r="8" fill="#f96a82" stroke="white" stroke-width="2"/>'%
                     (n,px(point["x"]),py(point["z"])))
    lines.append('</g>')
    lines.extend([
        '<g fill="#dce5ed" font-family="sans-serif" font-size="17">',
        '<text x="640" y="142">Original 64×64m MAPGEN arena</text>',
        '<text x="640" y="178">Static ray height: 1.25m</text>',
        '<text x="640" y="212">Referential Heavy: 5.180 × 5.8m</text>',
        '<text x="640" y="246">Source-clear 4m sample points: %d</text>' % len(field["visibilityGrid"]),
        '<text x="640" y="280">Self-occluding reachable anchors: %d</text>' % len(field["shelterAnchors"]),
        '<text x="640" y="335">Visible from 0/1/2/3 spawn descriptors:</text>',
        '<text x="640" y="365">0: %s   1: %s   2: %s   3: %s</text>' %
        tuple(field["visibleEnemyCountHistogram"][str(i)] for i in range(4)),
        '<text x="640" y="427" fill="#44e4a8">Green = shielded by source box</text>',
        '<text x="640" y="459" fill="#f07a6f">Red = exposed to 3 source markers</text>',
        '<text x="640" y="491" fill="#fff4ac">Yellow = candidate cover anchor</text>',
        '<text x="640" y="549" font-size="15">NOT actual dynamic LOS / AI / PhysX</text>',
        '<text x="640" y="575" font-size="15">No canonical gameplay integration</text>',
        '</g>',
        '<text x="52" y="682" font-family="sans-serif" font-size="13" fill="#b8c6d0">B01-21 · MAP-COVER-001 · immutable original MAPGEN source geometry · conservative data only</text>',
        '</svg>',
    ])
    return "\n".join(lines)+"\n"


def render_all() -> dict[str,str]:
    result={}
    report=[]
    for name in INPUT_IDS:
        plan=routes.load_original(name)
        field=original_sightline_field(plan,name)
        result[name+".cover.json"]=stable_json(field)
        result[name+".cover.svg"]=svg_view(field,plan)
        report.append({
            "sourceLayoutId":name,
            "sourceMapGenGeometryHash":plan["geometryHash"],
            "seed":plan["seed"],
            "sourceCoverElements":field["coverElementCount"],
            "sourceClearSamplePoints":len(field["visibilityGrid"]),
            "sourceSelfOccludingHeavyReachableAnchors":len(field["shelterAnchors"]),
            "visibleEnemyCountHistogram":field["visibleEnemyCountHistogram"],
            "data":name+".cover.json",
            "svg":name+".cover.svg",
        })
    result["manifest.json"]=stable_json({
        "schema":"B01-21-P17-MAP-COVER-001-MANIFEST-v1",
        "packageId": PACKAGE,
        "dependencyRoutePackageHead": BASE_SHA,
        "originalGameSourceSHA": routes.SOURCE_GAME,
        "originalBlueprintGitBlobs":routes.SOURCE_BLOBS,
        "nativeUnity":"NOT_RUN",
        "independentQA":"PENDING",
        "inGame":False,
        "arenas":report,
    })
    return result


def main() -> int:
    cli=argparse.ArgumentParser(description=__doc__)
    cli.add_argument("--check",action="store_true",help="check 7 immutable generated outputs before regeneration")
    cli.add_argument("--emit-base64",action="store_true",help="for first CI materialization of exact generated files")
    args=cli.parse_args()
    data=render_all()
    OUT.mkdir(parents=True,exist_ok=True)
    for name,body in sorted(data.items()):
        path=OUT/name
        raw=body.encode("utf-8")
        if args.check:
            if not path.is_file() or path.read_bytes()!=raw:
                raise ValueError("COMMITTED_COVER_SOURCE_DRIFT:"+name)
            print("MAP_COVER_P17_BYTE_IDENTICAL",name)
        else:
            path.write_bytes(raw)
            print("MAP_COVER_P17_GENERATED",name)
        if args.emit_base64:
            print("MAP_COVER_P17_FILE_B64 | "+name+" | "+base64.b64encode(raw).decode("ascii"))
    print("MAP_COVER_P17_PASS ARENAS=3 OUTPUTS=7 NATIVE_UNITY=NOT_RUN")
    return 0


if __name__=="__main__":
    raise SystemExit(main())
