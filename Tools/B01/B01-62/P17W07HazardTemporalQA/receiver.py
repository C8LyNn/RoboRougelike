#!/usr/bin/env python3
"""Read-only ORIGINAL owner map temporal-route receiver. Pure Python 3.12 stdlib."""
import argparse
import hashlib
import json
import math
from collections import deque
from pathlib import Path
import subprocess

PACKAGE_SHA = "6095577ece27320b1a861a738cc23ba42d6793a6"
OWNER_PATH = "Research/B01-25/P17/HAZARDPULSE001/"
OWNER_PROFILE_BLOB = "c0b03adee398ed12fa535c85ca2506d090d40cca"
OWNER_CORE_BLOB = "a7a2c69c80d43bf027f2b00896df72f892fa1a17"
MAPS = {
    "CLEARANCE_PULSE_CROSSING": (
        "ad52a604ffa4f143d98e607049d6f79e0c7e580f",
        "Research/B01-25/P17/CLEARANCELADDER001/arena.json",
        "53eb696950121465befea2855eda4d7268750016", 415
    ),
    "SLALOM_PULSE_CROSSING": (
        "f565e250659692541d672cdab108c4cfb8cfdd31",
        "Research/B01-25/P17/SLALOM001/arena.json",
        "013f5fe2058e76233f580a439e8402c0fe09f124", 360
    )
}
BLOCKER_KINDS = frozenset((1, 6, 7, 8))  # conservative source-grid obstacles
PERIOD_TICKS = 24                         # 6 seconds, four samples/second
EDGE_TICKS = 4                            # assumption: one 4m cell per second
DIRECTIONS = ((0, 1), (0, -1), (1, 0), (-1, 0), (0, 0))

class Reject(ValueError):
    pass

def git_blob(raw):
    return hashlib.sha1(b"blob " + str(len(raw)).encode() + b"\0" + raw).hexdigest()

def read_git(repo, commit, path, expected):
    def run(*arguments):
        return subprocess.run(["git", "-C", str(repo), *arguments],
                              check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE).stdout
    actual = run("rev-parse", f"{commit}:{path}").decode().strip()
    if actual != expected:
        raise Reject(f"PINNED_GIT_SOURCE_DRIFT {path} {actual} != {expected}")
    raw = run("show", f"{commit}:{path}")
    if git_blob(raw) != expected:
        raise Reject(f"PHYSICAL_GIT_BLOB_MISMATCH {path}")
    return raw

def authored_inputs(repo):
    prof = read_git(repo, PACKAGE_SHA, OWNER_PATH+"profiles.json", OWNER_PROFILE_BLOB)
    read_git(repo, PACKAGE_SHA, OWNER_PATH+"PulseCore.cs", OWNER_CORE_BLOB)
    maps = {key:read_git(repo, spec[0], spec[1], spec[2])
            for key,spec in MAPS.items()}
    return json.loads(prof), maps

def integer_pair(value, label):
    if not isinstance(value, list) or len(value)!=2 or any(type(x) is not int for x in value):
        raise Reject("BAD_PAIR "+label)
    return tuple(value)

def validate_level(level, mapdoc, require_pins=True):
    lid=level.get("levelId")
    if lid not in MAPS: raise Reject("UNRECOGNIZED_LEVEL")
    commit,path,blob,total=MAPS[lid]
    map_pr=359 if lid=="CLEARANCE_PULSE_CROSSING" else 346
    if require_pins and (level.get("mapCommit")!=commit or level.get("mapPath")!=path
                         or level.get("mapGitBlob")!=blob or level.get("mapPR")!=map_pr):
        raise Reject("ORIGINAL_MAP_PROVENANCE_DRIFT")
    items=mapdoc.get("items")
    if mapdoc.get("version")!=2 or mapdoc.get("globalFloor") is not False or not isinstance(items,list) or len(items)!=total:
        raise Reject("INVALID_MAP_DOCUMENT")
    for o in items:
        if not isinstance(o,dict) or type(o.get("kind")) is not int or type(o.get("x")) is not int or type(o.get("z")) is not int:
            raise Reject("INVALID_MAP_ITEM")
        if o["kind"] not in (0,1,4,5,6,7,8):raise Reject("UNKNOWN_KIND")
        if o.get("scaleX")!=1 or o.get("scaleZ")!=1:
            raise Reject("UNSUPPORTED_MULTICELL_FOOTPRINT")
    floors={(o["x"],o["z"]) for o in items if o["kind"]==0}
    blockers={(o["x"],o["z"]) for o in items if o["kind"] in BLOCKER_KINDS}
    starts=[(o["x"],o["z"]) for o in items if o["kind"]==4]
    ends=[(o["x"],o["z"]) for o in items if o["kind"]==5]
    if len(starts)!=1 or len(ends)!=1:raise Reject("MISSING_OR_AMBIGUOUS_START_FINISH")
    start,finish=starts[0],ends[0]
    if start not in floors or finish not in floors or start in blockers or finish in blockers:
        raise Reject("START_FINISH_NOT_CLEAR_FLOOR")
    zones=level.get("zones")
    if not isinstance(zones,list) or len(zones)!=2:raise Reject("NEED_EXACTLY_TWO_ZONES")
    seen=set()
    for z in zones:
        if not isinstance(z,dict) or not isinstance(z.get("id"),str) or not z["id"] or z["id"] in seen:
            raise Reject("DUPLICATE_OR_INVALID_ZONE_ID")
        seen.add(z["id"])
        cell=integer_pair(z.get("cell"),"cell")
        world=integer_pair(z.get("world"),"world")
        if world != (cell[0]*4,cell[1]*4):raise Reject("CELL_TO_WORLD_DISAGREEMENT")
        if cell not in floors or cell in blockers or cell in (start,finish):
            raise Reject("ZONE_CENTER_NOT_CLEAR")
        for name,expected in (("radius",1.35),("warning",1),("active",2),("recovery",3)):
            value=z.get(name)
            if type(value) not in (int,float) or not math.isfinite(value) or value != expected:
                raise Reject("INVALID_ZONE_"+name.upper())
        offset=z.get("offset")
        if type(offset) is not int or offset not in (0,3):
            raise Reject("INVALID_STAGGER_OFFSET")
    if {z["offset"] for z in zones}!={0,3}:
        raise Reject("NO_ALTERNATING_SAFE_LANE")
    if start == finish:raise Reject("TRIVIAL_START_FINISH")
    return floors-blockers,start,finish,zones

def active(z, tick):
    t=tick*.25
    phase=(t+z["offset"])%6.0
    return 1<=phase<3

def hazard_hit(z, x, zz, tick):
    dx=x*4-z["world"][0]
    dz=zz*4-z["world"][1]
    return active(z,tick) and dx*dx+dz*dz <= (z["radius"]+1e-9)**2

def edge_safe(a,b,tick,zones):
    # Each transition lasts one second; sample 0.25/0.5/0.75/1s.
    for i in range(1,EDGE_TICKS+1):
        x=a[0]+(b[0]-a[0])*i/EDGE_TICKS
        zz=a[1]+(b[1]-a[1])*i/EDGE_TICKS
        if any(hazard_hit(z,x,zz,tick+i) for z in zones):
            return False
    return True

def find_route(walkable,start,finish,zones):
    initial=(start[0],start[1],0)
    if any(hazard_hit(z,*start,0) for z in zones):raise Reject("SPAWN_ALREADY_ACTIVE")
    queue=deque([initial])
    prev={initial:None}
    goal=None
    while queue:
        x,z,tick=queue.popleft()
        if (x,z)==finish:
            goal=(x,z,tick)
            break
        for dx,dz in DIRECTIONS:
            cell=(x+dx,z+dz)
            if cell not in walkable or not edge_safe((x,z),cell,tick,zones):
                continue
            new=(cell[0],cell[1],(tick+EDGE_TICKS)%PERIOD_TICKS)
            if new not in prev:
                prev[new]=(x,z,tick)
                queue.append(new)
    if goal is None:raise Reject("NO_ZERO_PULSE_TIMED_ROUTE")
    route=[]
    while goal is not None:
        route.append([goal[0],goal[1],goal[2]])
        goal=prev[goal]
    route.reverse()
    # Recheck time and full interpolation on the reconstructed proof.
    for i in range(1,len(route)):
        before,after=route[i-1],route[i]
        if not edge_safe(before,after,(i-1)*EDGE_TICKS,zones):
            raise Reject("ROUTE_REPLAY_DAMAGE")
    return {"time_seconds":len(route)-1, "moves":sum(route[i-1][:2]!=route[i][:2] for i in range(1,len(route))),
            "wait_seconds":sum(route[i-1][:2]==route[i][:2] for i in range(1,len(route))),
            "states_explored":len(prev), "cells_with_phase_tick":route, "sample_step_seconds":.25,
            "one_cell_move_seconds":1, "pulse_hits":0}

def inspect(profiles, raw_maps, require_pins=True):
    if profiles.get("packageId")!="B01-25-HAZARDPULSE-001" or profiles.get("version")!=1 or profiles.get("gridMeters")!=4:
        raise Reject("INVALID_PROFILE_CONTRACT")
    lvls=profiles.get("levels")
    if not isinstance(lvls,list) or {v.get("levelId") for v in lvls if isinstance(v,dict)}!=set(MAPS) or len(lvls)!=2:
        raise Reject("LEVEL_SET_DRIFT")
    results={}
    for lv in sorted(lvls,key=lambda x:x["levelId"]):
        lid=lv["levelId"]
        raw=raw_maps[lid]
        if require_pins and git_blob(raw)!=MAPS[lid][2]:
            raise Reject("ORIGINAL_MAP_BYTES_DRIFT")
        m=json.loads(raw)
        walk,start,finish,zones=validate_level(lv,m,require_pins)
        r=find_route(walk,start,finish,zones)
        r.update({"start":list(start),"finish":list(finish),"walkable_cells":len(walk),
                  "owner_original_map_git_blob":git_blob(raw),
                  "hazard_ids":[z["id"] for z in zones]})
        results[lid]=r
    return {"schema":"b01-62-hazard-temporal-receiver-v1","owner_package":"B01-25-HAZARDPULSE-001",
            "owner_commit":PACKAGE_SHA,"owner_profiles_git_blob":OWNER_PROFILE_BLOB,
            "owner_core_git_blob":OWNER_CORE_BLOB,
            "native_unity":"NOT_RUN","actual_physics":"NOT_RUN",
            "movement_assumption":"source-grid-only, 4m cell per 1sec, time-step 0.25sec, instant direction changes; not PhysX or vehicle motion",
            "maps":results}

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",required=True)
    p.add_argument("--out",required=True)
    v=p.parse_args()
    target=Path(v.out)
    if target.exists():raise Reject("REFUSE_OVERWRITE")
    profiles,maps=authored_inputs(v.repo)
    receipt=inspect(profiles,maps)
    if not target.parent.is_dir():raise Reject("MISSING_OUTPUT_PARENT")
    data=(json.dumps(receipt,sort_keys=True,indent=2,ensure_ascii=False)+"\n").encode("utf-8")
    target.write_bytes(data)
    print("ORIGINAL_HAZARD_TEMPORAL_RECEIVER=PASS "
          + " ".join(f"{k}={z['time_seconds']}sec/{z['pulse_hits']}hits"
                     for k,z in sorted(receipt["maps"].items())))
    print("RECEIPT_SHA256="+hashlib.sha256(data).hexdigest())

if __name__=="__main__":
    main()
