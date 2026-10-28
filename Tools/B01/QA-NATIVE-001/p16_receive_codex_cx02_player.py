#!/usr/bin/env python3
"""B01-60 PASS16/CX02 independent receipt verifier. READ-ONLY, not Unity.

Fetches exact ORIGINAL Codex Windows Player JSON/process/log Git objects, not
rewritten observations. Checks the 7x4 game-shot matrix, actual numeric damage,
correct group A/LMB vs B/RMB, near/far range negatives, process provenance.
Synthetic mutations of receipt records test the verifier, NOT gameplay.
"""
import copy
import gzip
import json
import math
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
EVIDENCE_SHA = "589b60a912096d1e9917cb0bbac73a53e7543b4f"
PLAYER_SOURCE_SHA = "4aec116282eb583fd852469c2bb7dc7e6d85b9e3"
FOLDER = "Docs/CX02AimFixture/Evidence/"
BLOBS = {
    "corrected-player.json": "2a81d40f7f173fa978d7bd3dee3eb96aff5a3d45",
    "corrected-player-exit.txt": "18748286e5b8b4de5db905f87cdfed1a7d48fe60",
    "corrected-player.log.gz": "e8323aa668de23e4a16679299be445f46b836b10",
    "initial-player.json": "2400a39828b804b57154272b03ebca401f15b767",
    "initial-player-exit.txt": "f33dfa25aa16a97ca941fe1d9cabdc3689179f3f",
    "compile-process.json": "d2e5ca1126a85208c9c38c3e5e0918f8b135f081",
    "build-process.json": "3e4e17c336f5de318c9435af173e7ce8c75e19c8"
}
DISTANCES = (10,30,60,100)
FAMILIES = ("Basic","Automatic","Heavy","Shotgun","Railgun","BeamCutter","RocketLauncher")
GROUPS = {"Basic":"A/LMB","Automatic":"B/RMB","Heavy":"A/LMB",
          "Shotgun":"B/RMB","Railgun":"A/LMB","BeamCutter":"B/RMB",
          "RocketLauncher":"A/LMB"}
RANGE_NEGATIVES = {("Shotgun",60),("Shotgun",100),
                   ("BeamCutter",30),("BeamCutter",60),("BeamCutter",100)}
PRIOR_REPRO_FAILURES = {("Automatic",10),("Automatic",30),
                        ("Automatic",60),("Automatic",100),
                        ("Shotgun",10),("Shotgun",30),("BeamCutter",10)}

class BadReceipt(Exception): pass

def check(condition,why):
    if not condition: raise BadReceipt(why)

def git(*args):
    return subprocess.run(["git","-C",str(ROOT),*args],check=True,
                          stdout=subprocess.PIPE,stderr=subprocess.PIPE).stdout

def originals():
    git("fetch","--no-tags","origin",EVIDENCE_SHA)
    git("fetch","--no-tags","origin",PLAYER_SOURCE_SHA)
    check(subprocess.run(["git","-C",str(ROOT),"merge-base","--is-ancestor",
                          PLAYER_SOURCE_SHA,EVIDENCE_SHA]).returncode==0,
          "PLAYER_SHA_NOT_ANCESTOR_OF_RECEIPT")
    material={}
    for filename,expected in BLOBS.items():
        path=f"{FOLDER}{filename}"
        raw=git("show",f"{EVIDENCE_SHA}:{path}")
        h=subprocess.run(["git","-C",str(ROOT),"hash-object","--stdin"],input=raw,
                         check=True,stdout=subprocess.PIPE).stdout.decode().strip()
        check(h==expected,"ORIGINAL_GIT_BLOB_MISMATCH:"+filename)
        material[filename]=raw
    return material

def finite(value,name):
    check(type(value) in (int,float) and math.isfinite(value), "INVALID_NUMBER:"+name)
    return value

def validate_shots(receipt, old=False):
    check(isinstance(receipt,dict) and isinstance(receipt.get("shots"),list),
          "NO_SHOT_ARRAY")
    if not old:
        check(receipt.get("status")=="Passed" and receipt.get("errors")==[],
              "ROOT_STATUS_OR_ERRORS")
    shots=receipt["shots"]
    check(len(shots)==28,"NOT_28_SHOTS")
    pairs=set()
    failures=set()
    for s in shots:
        w=s.get("weapon")
        d=s.get("requestedDistance")
        check(w in FAMILIES and type(d) in (float,int) and d in DISTANCES,
              "FOREIGN_WEAPON_OR_DISTANCE")
        pair=(w,int(d))
        check(pair not in pairs,"DUPLICATE_WEAPON_DISTANCE")
        pairs.add(pair)
        if not old:
            check(s.get("fireGroup")==GROUPS[w],"WRONG_ASSIGNED_FIRE_GROUP:"+str(pair))
        damage=finite(s.get("damage"),"damage")
        muzzle=finite(s.get("muzzleDistance"),"muzzleDistance")
        reach=finite(s.get("range"),"range")
        finite(s.get("muzzleError"),"muzzleError")
        check(0 < muzzle and abs(muzzle-d)<3 and reach>0,
              "INVALID_SHOT_GEOMETRY")
        check(type(s.get("expectedHit")) is bool and type(s.get("cameraOnTarget")) is bool,
              "INVALID_HIT_BOOL")
        check(s.get("cameraOnTarget") is True,"CAMERA_OFF_TARGET")
        check(s.get("status") in ("Passed","Failed"),"INVALID_CASE_STATUS")
        if not old:
            should_hit=pair not in RANGE_NEGATIVES
            check(s["status"]=="Passed" and s["expectedHit"]==should_hit,
                  "FALSE_GREEN_OR_WRONG_EXPECTED_RANGE:"+str(pair))
            check((damage>0) if should_hit else (damage==0),
                  "MISMATCH_DAMAGE_AND_EXPECTED_RANGE:"+str(pair))
            if not should_hit:
                check(muzzle>reach and "poza zasięgiem" in s.get("feedback",""),
                      "FALSE_OUT_OF_RANGE")
            else:
                check(damage>0 and "tor:" in s.get("feedback",""),"FALSE_HIT_FEEDBACK")
            direction=s.get("direction",{})
            check(isinstance(direction,dict),"MISSING_DIRECTION")
            xyz=[finite(direction.get(ax),ax) for ax in ("x","y","z")]
            norm=math.sqrt(sum(x*x for x in xyz))
            check(0.99<=norm<=1.01,"BAD_DIRECTION_VECTOR")
        elif s["status"]=="Failed":
            failures.add(pair)
    check(pairs=={(w,d) for w in FAMILIES for d in DISTANCES},"INCOMPLETE_GRID")
    if old:
        check(failures==PRIOR_REPRO_FAILURES,"HISTORICAL_SEVEN_FAIL_SET_DRIFT")
        check(receipt.get("status")=="Failed","HISTORICAL_ROOT_NOT_FAILED")
    return {"cases":28,"positive_hits":sum(p not in RANGE_NEGATIVES for p in pairs),
            "expected_range_misses":len(RANGE_NEGATIVES),
            "historical_failures":len(failures)}

def main():
    raw=originals()
    current=json.loads(raw["corrected-player.json"])
    before=json.loads(raw["initial-player.json"])
    actual=validate_shots(current)
    old=validate_shots(before,old=True)
    check(raw["corrected-player-exit.txt"].strip()==b"0","PLAYER_EXIT_NOT_ZERO")
    check(raw["initial-player-exit.txt"].strip()!=b"0","HISTORICAL_EXIT_NOT_RED")
    for name,phase in (("compile-process.json","Compile"),("build-process.json","Build")):
        p=json.loads(raw[name])
        check(type(p.get("exitCode")) is int and p["exitCode"]==0 and
              p.get("source")==PLAYER_SOURCE_SHA and p.get("phase")==phase,
              "INVALID_NATIVE_PROCESS_RECEIPT:"+name)
        check(p.get("startedUtc") and p.get("finishedUtc"),
              "NO_PROCESS_TIMESTAMPS")
    log=gzip.decompress(raw["corrected-player.log.gz"])
    check(len(log)>100 and (b"CAM275" in log or b"Camera275" in log or b"aim" in log.lower()),
          "MISSING_OR_MISMATCHED_PLAYER_LOG")
    # Explicit mutation sensitivity; each must be rejected by the SAME validator.
    tamper=[]
    for case,change in (
        ("drop_case",lambda x:x["shots"].pop()),
        ("wrong_group",lambda x:x["shots"][4].update(fireGroup="A/LMB")),
        ("zero_damage_passed",lambda x:x["shots"][4].update(damage=0)),
        ("false_range_claim",lambda x:x["shots"][14].update(expectedHit=True)),
        ("bool_damage",lambda x:x["shots"][0].update(damage=True)),
        ("duplicate_case",lambda x:x["shots"][1].update(requestedDistance=10)),
    ):
        edit=copy.deepcopy(current);change(edit)
        try: validate_shots(edit)
        except BadReceipt: tamper.append(case)
        else: raise BadReceipt("MUTATION_NOT_REJECTED:"+case)
    print("B0160_CX02_JSON="+json.dumps({
        "artifact":"Codex original Windows Player evidence",
        "source_sha":PLAYER_SOURCE_SHA,"receipt_sha":EVIDENCE_SHA,
        "source":"exact Git object bytes (not re-executed game)",
        "original_git_blobs":BLOBS,"matrix":actual,
        "historical_run":old,"mutations_rejected":tamper,
        "original_player_log_decompressed_bytes":len(log),
        "native_player_executed_by":"Codex, not B01-60",
        "independent_native_g1":"NOT_RUN",
        "acceptance":"RECEIPT_CONTENT_INDEPENDENT_GREEN_NOT_VISUAL_OR_G1"
    },sort_keys=True))

if __name__=="__main__":
    main()
