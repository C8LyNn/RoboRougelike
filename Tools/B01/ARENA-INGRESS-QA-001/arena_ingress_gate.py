#!/usr/bin/env python3
"""B01-60 / ARENA-INGRESS-QA-001 — source-pinned, independent GAME-DATA intake.

Tests the three ORIGINAL B01-21 generated CombatArena blueprints against a
separate conservative 2D axis-aligned vehicle-footprint reachability oracle.
It does not replay the owner's C# planner or call Unity/PhysX. The original
full-size heavy/light reference footprints were HISTORICALLY measured.
No source blueprint, generator, or Codex gameplay is modified.
"""
from __future__ import annotations
import argparse
import copy
import hashlib
import json
import math
import subprocess
from collections import Counter, deque
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
PRODUCER="a71eda99a260f807ae8cfea71348e75ff4f6c65a"
GAME="589b60a912096d1e9917cb0bbac73a53e7543b4f"
PREFIX="Research/B01-21/P16/generated/"
MANIFEST_BLOB="aa3eb18599452f08a08eb5d3ada6c9388167e116"
PLANNER="Assets/B01Packages/MAPGEN001/Runtime/MapGenPlanner.cs"
PLANNER_BLOB="89ff64e12f278f2c9393ad542161a9387e1f4117"
INPUTS={
    "AR16_01_OPEN_FLANK":("fce7cdce22fa9d9f512c8cd1d112cf1d83ad205e",160101,6),
    "AR16_02_SPLIT_DECISION":("022b5c0cd1e13ca64460a3d6a0ab0122c4c2c790",160202,11),
    "AR16_03_DENSE_PRESSURE":("1fe7bf8b124d999de75c0fd3cacc85ee62319429",160303,16),
}
# Conservative circle enclosing the entire original historical oriented rectangle;
# unlike the producer's route validator, this independent grid is AABB+inflated BFS.
REFERENCE_PROFILES={
    "Light":(2.98,3.20,0.25),
    "Heavy":(5.18,5.80,0.25),
}
VALID_KINDS={"Floor","Boundary","Cover"}
class Reject(ValueError):pass

def need(ok,why):
    if not ok:raise Reject(why)

def git(*args,stdin=None):
    r=subprocess.run(["git","-C",str(ROOT),*args],
                     input=stdin,capture_output=True,check=True)
    return r.stdout

def read_original(commit,path,blob):
    raw=git("show",f"{commit}:{path}")
    actual=git("hash-object","--stdin",stdin=raw).decode().strip()
    need(actual==blob,f"PIN_MISMATCH {path}: {actual}")
    return raw

def finite(n,tag):
    need(type(n) in (float,int) and math.isfinite(n),f"NONFINITE_{tag}")
    return float(n)

def point(data,label):
    need(isinstance(data,dict) and set(data)=={"x","y","z"},f"BAD_POINT_{label}")
    return tuple(finite(data[a],label+"_"+a) for a in ("x","y","z"))

def rectangles(arena):
    result=[]
    for e in arena["elements"]:
        if e["kind"] in ("Boundary","Cover"):
            x,y,z=point(e["position"],"element_pos")
            w,h,d=point(e["size"],"element_size")
            result.append((e["kind"],x-w/2,x+w/2,z-d/2,z+d/2))
    return result

def is_free(x,z,rects,radius):
    if not (-32+radius < x < 32-radius and -32+radius < z < 32-radius):
        return False
    for kind,lo,hi,bottom,top in rects:
        if lo-radius<=x<=hi+radius and bottom-radius<=z<=top+radius:
            return False
    return True

def bfs(arena,profile):
    width,length,clearance=REFERENCE_PROFILES[profile]
    radius=math.hypot(width/2,length/2)+clearance
    obstacles=rectangles(arena)
    start=point(arena["start"],"start")
    finish=point(arena["finish"],"finish")
    sx,sz=round(start[0]),round(start[2])
    fx,fz=round(finish[0]),round(finish[2])
    need(is_free(sx,sz,obstacles,radius),"START_FOOTPRINT_BLOCKED_"+profile)
    need(is_free(fx,fz,obstacles,radius),"FINISH_FOOTPRINT_BLOCKED_"+profile)
    todo=deque([(sx,sz)])
    seen={(sx,sz)}
    parent={}
    while todo:
        x,z=todo.popleft()
        if (x,z)==(fx,fz):
            path=[(x,z)]
            while (x,z) in parent:
                x,z=parent[(x,z)]
                path.append((x,z))
            path.reverse()
            return {"cells":len(seen),"route_length_m":len(path)-1,
                    "min_enclosing_radius_m":round(radius,4),
                    "centerline_possible":all(x==0 for x,z in path),
                    "waypoints_xz":[list(p) for p in path[::max(1,len(path)//8)]]}
        for xx,zz in ((x+1,z),(x-1,z),(x,z+1),(x,z-1)):
            if (xx,zz) not in seen and is_free(xx,zz,obstacles,radius):
                seen.add((xx,zz));parent[(xx,zz)]=(x,z);todo.append((xx,zz))
    raise Reject("NO_CONSERVATIVE_4_NEIGHBOR_ROUTE_"+profile)

def inspect(arena,expected_id,expected_seed,expected_covers,strict_finish=False):
    need(type(arena)==dict,"DATA_ROOT_NOT_OBJECT")
    need(arena.get("schema")=="B01-21-P16-ACTUAL-MAPGEN001-BLUEPRINT-v1","WRONG_SCHEMA")
    need(arena.get("id")==expected_id,"WRONG_LEVEL_ID")
    need(arena.get("recipe")=="CombatArena","NOT_COMBAT_ARENA")
    need(type(arena.get("seed")) is int and arena["seed"]==expected_seed,"SEED_MISMATCH")
    need(arena.get("originalGameSha")==GAME,"ORIGINAL_GAME_SHA_MISMATCH")
    need(type(arena.get("geometryHash")) is str and
         len(arena["geometryHash"])==64 and
         all(c in "0123456789abcdef" for c in arena["geometryHash"]),"HASH_NOT_SHA256")
    need(arena.get("guaranteedClearanceWidthMeters")==12,"CLAIMED_LANE_CHANGED")
    bounds=arena.get("sourceBounds",{})
    need(point(bounds.get("size"),"bounds_size")== (64.0,8.0,64.0),
         "ARENA_BOUNDS_CHANGED")
    start=point(arena.get("start"),"start")
    finish=point(arena.get("finish"),"finish")
    need(start[0]==finish[0]==0 and start[2]==-25 and finish[2]==25,
         "ENDPOINTS_CHANGED")
    enemies=arena.get("enemySpawnDescriptors")
    need(isinstance(enemies,list) and len(enemies)==3,"ENEMY_SPAWN_COUNT")
    coordinates=[point(p,"enemy_spawn") for p in enemies]
    need(len(set(coordinates))==3,"DUPLICATE_ENEMY_SPAWN")
    elements=arena.get("elements")
    need(isinstance(elements,list),"ELEMENTS_NOT_ARRAY")
    counts=Counter()
    for e in elements:
        need(isinstance(e,dict) and e.get("kind") in VALID_KINDS,"UNSUPPORTED_ELEMENT")
        p=point(e.get("position"),"element_pos")
        s=point(e.get("size"),"element_size")
        need(all(v>0 for v in s),"NONPOSITIVE_SIZE")
        need(finite(e.get("pitchDegrees"),"pitch")==0,"TILTED_OR_ROTATED_ELEMENT")
        counts[e["kind"]]+=1
        if e["kind"]=="Cover":
            need(abs(p[0])+s[0]/2 < 32 and abs(p[2])+s[2]/2 < 32,
                 "COVER_OUT_OF_ARENA")
            # Independently enforce original 12m center avenue as real FREE corridor.
            need(abs(p[0])-s[0]/2>=6,"CENTER_AVENUE_COVER_VIOLATION")
    need(counts==Counter({"Floor":1,"Boundary":4,"Cover":expected_covers}),
         "ELEMENT_COUNT_OR_COVER_BUDGET_DRIFT")
    # Exact world floor/outer border: explicit 4 separated 1m boundary AABBs.
    floors=[e for e in elements if e["kind"]=="Floor"]
    need(point(floors[0]["size"],"floor_size")== (64.0,.9,64.0),"NO_64M_FLOOR")
    bounds_list=[(tuple(point(e["position"],"wall_pos")),
                  tuple(point(e["size"],"wall_size")))
                 for e in elements if e["kind"]=="Boundary"]
    needed=[((-32.5,1.8,0.0),(1.0,3.6,66.0)),
            ((32.5,1.8,0.0),(1.0,3.6,66.0)),
            ((0.0,1.8,-32.5),(66.0,3.6,1.0)),
            ((0.0,1.8,32.5),(66.0,3.6,1.0))]
    need(sorted(bounds_list)==sorted(needed),"OPEN_OR_UNEXPECTED_BOUNDARY")
    covers=[e for e in elements if e["kind"]=="Cover"]
    for i,a in enumerate(covers):
        pa=point(a["position"],"a");sa=point(a["size"],"a")
        for b in covers[:i]:
            pb=point(b["position"],"b");sb=point(b["size"],"b")
            collides=abs(pa[0]-pb[0])<(sa[0]+sb[0])/2 and abs(pa[2]-pb[2])<(sa[2]+sb[2])/2
            need(not collides,"INTERSECTING_COVER_FOOTPRINTS")
    for pos in [start,finish,*coordinates]:
        need(is_free(pos[0],pos[2],
                     [x for x in rectangles(arena) if x[0] == "Cover"],1.0),
             "ACTOR_DESCRIPTOR_COVER_INTERSECTION")
    routes={p:bfs(arena,p) for p in REFERENCE_PROFILES}
    overlaps=[{"enemy_index":i,"distance_m":round(math.hypot(x-finish[0],z-finish[2]),3)}
              for i,(x,y,z) in enumerate(coordinates)
              if math.hypot(x-finish[0],z-finish[2])<2.0]
    if strict_finish:
        need(not overlaps,"UNRESOLVED_ENEMY_DESCRIPTOR_AT_FINISH")
    return {"id":expected_id,"seed":expected_seed,"original_geometry_hash":arena["geometryHash"],
            "cover_count":expected_covers,"elements":len(elements),
            "routes":routes,"finish_spawn_overlap":overlaps,
            "policy_status":"HQ_DECISION_REQUIRED_FINISH_SPAWN" if overlaps else "NONE",
            "native_unity":"NOT_RUN","independent_physics":"NOT_RUN"}

def mutate_test(arena,kind,identity):
    """Reject distinct deliberately invalid inputs, including a direct BFS obstruction.

    Match the exact rejection reason: a generic unrelated failure cannot
    accidentally certify a specialised safety check.
    """
    bad=copy.deepcopy(arena)
    expected={
        "blocked_start":"CENTER_AVENUE_COVER_VIOLATION",
        "blocked_finish":"CENTER_AVENUE_COVER_VIOLATION",
        "wrong_sha":"ORIGINAL_GAME_SHA_MISMATCH",
        "duplicate_enemy":"DUPLICATE_ENEMY_SPAWN",
        "broken_boundary":"OPEN_OR_UNEXPECTED_BOUNDARY",
        "nonfinite_size":"NONFINITE_element_size_x",
        "blocked_route_bfs":"NO_CONSERVATIVE_4_NEIGHBOR_ROUTE_Heavy",
    }
    if kind in ("blocked_start","blocked_finish"):
        cover=next(e for e in bad["elements"] if e["kind"]=="Cover")
        cover["position"]={"x":0,"y":2,"z":-25 if kind=="blocked_start" else 25}
        cover["size"]={"x":4,"y":4,"z":4}
    elif kind=="wrong_sha":
        bad["originalGameSha"]="bad-source"
    elif kind=="duplicate_enemy":
        bad["enemySpawnDescriptors"][1]=copy.deepcopy(bad["enemySpawnDescriptors"][0])
    elif kind=="broken_boundary":
        wall=next(e for e in bad["elements"] if
                  e["kind"]=="Boundary" and e["position"]["x"]==-32.5)
        wall["position"]["x"]=-31.5
    elif kind=="nonfinite_size":
        next(e for e in bad["elements"] if e["kind"]=="Cover")["size"]["x"]=float("nan")
    elif kind=="blocked_route_bfs":
        cover=next(e for e in bad["elements"] if e["kind"]=="Cover")
        cover["position"]={"x":0,"y":2,"z":0}
        cover["size"]={"x":64,"y":4,"z":2}
    else: raise RuntimeError(kind)
    try:
        if kind=="blocked_route_bfs":bfs(bad,"Heavy")
        else:inspect(bad,*identity)
    except Reject as exc:
        need(str(exc)==expected[kind],
             "WRONG_MUTATION_FAILURE_"+kind+":"+str(exc))
        return
    raise AssertionError("MUTATION_NOT_REJECTED:"+kind)

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--output",default=None)
    args=p.parse_args()
    git("fetch","--no-tags","origin",PRODUCER)
    git("fetch","--no-tags","origin",GAME)
    read_original(GAME,PLANNER,PLANNER_BLOB)
    manifest=json.loads(read_original(PRODUCER,PREFIX+"generated_manifest.json",MANIFEST_BLOB))
    need(manifest["schema"]=="B01-21-P16-MAPGEN-PACKAGE-v1","OWNER_MANIFEST_SCHEMA")
    need(manifest["inputSha"]==GAME,"OWNER_MANIFEST_GAME_SHA")
    need(len(manifest["arenas"])==len(INPUTS),"OWNER_MANIFEST_COUNT")
    results=[]
    for x in manifest["arenas"]:
        key=x["id"]
        need(key in INPUTS,"UNLISTED_ARENA")
        blob,seed,covers=INPUTS[key]
        arena=json.loads(read_original(PRODUCER,PREFIX+x["blueprintFile"],blob))
        need(x["seed"]==seed and x["covers"]==covers and
             x["geometryHash"]==arena["geometryHash"],"OWNER_MANIFEST_MISMATCH")
        identity=(key,seed,covers)
        record=inspect(arena,*identity)
        rejected=[]
        for kind in ("blocked_start","blocked_finish","wrong_sha",
                     "duplicate_enemy","broken_boundary","nonfinite_size",
                     "blocked_route_bfs"):
            mutate_test(arena,kind,identity);rejected.append(kind)
        try:inspect(arena,*identity,strict_finish=True)
        except Reject as ex:
            need(str(ex)=="UNRESOLVED_ENEMY_DESCRIPTOR_AT_FINISH","UNKNOWN_STRICT_FAILURE")
            strict="POLICY_HOLD_EXPECTED"
        else:strict="STRICT_CLEAR_PASS"
        # Positive in-memory correction proves strict check is NOT permanently red;
        # never export or overwrite this synthetic copy as a production map.
        corrected=copy.deepcopy(arena)
        if record["finish_spawn_overlap"]:
            for hit in record["finish_spawn_overlap"]:
                corrected["enemySpawnDescriptors"][hit["enemy_index"]]["z"]=17.0
        inspect(corrected,*identity,strict_finish=True)
        record["negative_mutations_rejected"]=rejected
        record["objective_strict_gate"]=strict
        record["strict_corrected_IN_MEMORY_positive"]="PASS_NOT_PRODUCTION_DATA"
        results.append(record)
    need({a["id"] for a in results}==set(INPUTS),"MISSING_RESULTS")
    need([a["cover_count"] for a in results]==[6,11,16],"DENSITY_ORDER_NOT_DISTINCT")
    need(len({a["original_geometry_hash"] for a in results})==3,"SAME_LAYOUT_HASH")
    receipt={
      "package":"ARENA-INGRESS-QA-001","role":"B01-60",
      "source_owner":"B01-21","producer_sha":PRODUCER,
      "planner_original_game_sha":GAME,"original_planner_blob":PLANNER_BLOB,
      "original_manifest_blob":MANIFEST_BLOB,
      "input_blueprint_git_blobs":{k:v[0] for k,v in INPUTS.items()},
      "original_arena_count":len(results),"structural_acceptance":"PASS",
      "separate_conservative_bfs_pairs_passed":len(results)*len(REFERENCE_PROFILES),
      "synthetic_negative_mutations_denied":len(results)*7,
      "original_finish_enemy_descriptor_overlaps":sum(len(a["finish_spawn_overlap"]) for a in results),
      "deployment_gate":"HOLD_HQ_POLICY_AND_NATIVE_UNITY",
      "method":"Original Git blueprint bytes + independent AABB and 1m 4-neighbor BFS; NOT Unity/PhysX",
      "results":results}
    out=json.dumps(receipt,indent=2,sort_keys=True,ensure_ascii=False)+"\n"
    if args.output:
        target=Path(args.output);target.parent.mkdir(parents=True,exist_ok=True)
        target.write_text(out,encoding="utf-8")
    print("B0160_P17_ARENA_SUMMARY="+json.dumps({k:v for k,v in receipt.items() if k!="results"},sort_keys=True))
    for record in results:
        print("B0160_P17_ARENA="+json.dumps(record,sort_keys=True))
    print("B0160_P17_ORIGINAL_DATA=PASS;GAMEPLAY_IMPORT=NOT_RUN;FINISH_SPAWN_POLICY=HOLD")

if __name__=="__main__":
    main()
