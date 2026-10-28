#!/usr/bin/env python3
"""Exact new Unity TextAsset and weak-point source witness; no Editor/G1."""
import json
from pathlib import Path
import re
import sys

ROOT=Path(".")
OWN={
"Assets/PARTSIZE001/Weakpoints.meta":"89cce9bdc1af425abc5f7e008af11401",
"Assets/PARTSIZE001/Weakpoints/P17.meta":"89cce9bdc1af425abc5f7e008af11402",
"Assets/PARTSIZE001/Weakpoints/P17/P17_SingleBreakOutcomes.json.meta":"89cce9bdc1af425abc5f7e008af11403",
}
RX=re.compile(r"^guid: ([0-9a-f]{32})$",re.M)
def one(s):
    a=RX.findall(s)
    if len(a)!=1:raise ValueError("NEW_META_GUID_INVALID")
    return a[0]
def audit():
    for path,sha in OWN.items():
        p=ROOT/path
        if one(p.read_text(encoding="utf8"))!=sha:
            raise ValueError("P17_NEW_META_GUID_DRIFT:"+path)
        if path.endswith(".json.meta") and (
            "TextScriptImporter:" not in p.read_text(encoding="utf8") or
            not Path(str(p)[:-5]).is_file()):
            raise ValueError("WEAKPOINT_TEXTASSET_MISSING")
        if not path.endswith(".json.meta") and (
            "folderAsset: yes" not in p.read_text(encoding="utf8") or
            not Path(str(p)[:-5]).is_dir()):
            raise ValueError("WEAKPOINT_FOLDER_META_MISMATCH")
    collisions=[]
    found=set()
    for p in (ROOT/"Assets").rglob("*.meta"):
        for g in RX.findall(p.read_text(encoding="utf8")):
            if g in OWN.values():
                found.add(g)
                rel=p.as_posix()
                if OWN.get(rel)!=g:
                    collisions.append(rel)
    if collisions or found!=set(OWN.values()):
        raise ValueError("WEAKPOINT_META_COLLISION:"+",".join(collisions))
    data=json.loads((ROOT/"Assets/PARTSIZE001/Weakpoints/P17/P17_SingleBreakOutcomes.json").read_text())
    if (data["schema"]!="B01-P17-WEAKPOINTS-001-v1" or
        data["originBlueprintSha"]!="0978497561c8b6400490235d3e5a401ac32bea2d" or
        len(data["rigs"])!=3 or
        sum(len(r["singleBreakCases"]) for r in data["rigs"])!=38 or
        any(x["nativePhysicalStatus"]!="NOT_RUN" for rig in data["rigs"] for x in rig["singleBreakCases"])):
        raise ValueError("FAKE_PHYSICS_OR_DATA_CASE_COUNT")
    return 3,38

def tests():
    for bad in ["guid: BAD","guid: "+"a"*31,"guid: "+"a"*32+"\nguid: "+"b"*32,"not a meta"]:
        try:one(bad)
        except ValueError:pass
        else:raise AssertionError("MUTATED_GUID_ACCEPTED")
    return 4

if __name__=="__main__":
    try:
        a,b=audit();n=tests()
        print(f"B01_23_P17_WEAKPOINT_UNITY_META_PASS {a}/3 GUID, {b}/38 source cases, {n}/4 negatives; UNITY_NOT_RUN")
    except (AssertionError,ValueError,KeyError,OSError) as e:
        print("B01_23_P17_WEAKPOINT_UNITY_META_FAIL:"+str(e),file=sys.stderr)
        raise SystemExit(1)
