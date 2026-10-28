#!/usr/bin/env python3
"""Verify REAL exported original-C# MapBlueprint data, not an authored route mock."""
import json
from pathlib import Path
import sys

def verify(folder):
    folder=Path(folder)
    meta=json.loads((folder/"results.json").read_text(encoding="utf-8"))
    assert meta["packageId"]=="MAPGEN-COURSE-P17"
    assert meta["status"]=="ORIGINAL_SOURCE_CSHARP_VALIDATED"
    assert (meta["variants"],meta["blueprints"],meta["uniqueHashes"])==(3,18,18)
    assert meta["originalCsharpRouteCases"]==36
    assert meta["originalPlannerInvalidRejections"]==9
    assert meta["realUnity"]=="NOT_RUN" and meta["physxDrives"]==0
    assert len(meta["generated"])==18
    seen=set()
    counts={}
    for x in meta["generated"]:
        assert Path(x["file"]).name==x["file"]
        f=json.loads((folder/x["file"]).read_text(encoding="utf-8"))
        assert f["packageId"]=="MAPGEN-COURSE-P17"
        assert f["recipe"]=="ObstacleCourse" and f["seed"]==x["seed"]
        assert f["variantId"]==x["variant"] and f["geometryHash"]==x["geometryHash"]
        assert f["sourceSha"]==meta["sourceSha"]
        assert f["nativeUnity"]=="NOT_RUN" and f["physxDrives"]==0
        seg=f["config"]["Segments"]
        assert seg in (3,4,5)
        assert len(f["checkpoints"])==seg
        assert f["counts"]["floors"]==seg
        assert f["counts"]["boundaries"]==2*seg
        assert f["counts"]["ramps"]>=1
        assert f["guaranteedClearanceWidthM"]>=5.0
        assert f["start"]["z"] < f["finish"]["z"]
        assert {v["profile"] for v in f["profiles"]}=={"Light","Heavy"}
        assert all(v["passed"] and v["code"]=="BFS_SPINE_CLEARANCE_OK" for v in f["profiles"])
        assert {e["kind"] for e in f["elements"]}>={"Floor","Boundary","Ramp"}
        assert all(all(e["size"][k]>0 for k in ("x","y","z")) for e in f["elements"])
        counts.setdefault(f["variantId"],[]).append(f)
        assert f["geometryHash"] not in seen
        seen.add(f["geometryHash"])
    assert len(seen)==18
    assert [len(counts[k]) for k in sorted(counts)]==[6,6,6]
    assert len(list(folder.glob("P17-COURSE-*-seed-*.json")))==18
    print("P17_ORIGINAL_CSHARP_READBACK_PASS=18 CHECKPOINTS=3_4_5 PROFILES=LIGHT_HEAVY UNITY=NOT_RUN")

if __name__=="__main__":
    if len(sys.argv)!=2:raise SystemExit("Usage: verify_courses.py output_directory")
    verify(sys.argv[1])
