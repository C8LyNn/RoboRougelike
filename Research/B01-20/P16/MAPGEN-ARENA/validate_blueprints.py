#!/usr/bin/env python3
"""Read back actual original-C# generated JSON. This is NOT Unity physics."""
import json
import pathlib
import sys
def main(root):
    p=pathlib.Path(root)
    receipt=json.loads((p/"results.json").read_text(encoding="utf-8"))
    assert receipt["status"]=="SOURCE_ORIGINAL_CSHARP_PASS"
    assert receipt["totalVariants"]==3 and receipt["uniqueGeneratedMaps"]==18
    assert receipt["actualOriginalCsharpPlans"]==36
    assert receipt["originalPlannerNegativeRejections"]==6
    assert receipt["actualUnityNative"]=="NOT_RUN" and receipt["actualPhysxDrives"]==0
    hashes=set()
    for row in receipt["generatedBlueprints"]:
        b=json.loads((p/row["file"]).read_text(encoding="utf-8"))
        assert b["geometryHash"]==row["geometryHash"] and b["seed"]==row["seed"]
        assert b["variantId"]==row["variant"] and b["nativeUnityPhysics"]=="NOT_RUN"
        assert b["sourceRecipe"]=="CombatArena" and b["sourceOwnerSha"]==receipt["sourceSha"]
        assert len(b["enemySpawns"])==3 and len(b["sourceValidation"])==2
        assert {v["profile"] for v in b["sourceValidation"]}=={"Light","Heavy"}
        assert all(v["sourceValidatorPassed"] and v["sourceRouteStatus"]=="BFS_SPINE_CLEARANCE_OK" for v in b["sourceValidation"])
        assert b["guaranteedClearanceWidthM"]==12
        assert b["bounds"]["size"]=={"x":64,"y":8,"z":64}
        assert any(e["kind"]=="Floor" for e in b["elements"])
        assert sum(e["kind"]=="Boundary" for e in b["elements"])==4
        assert all(all(e["size"][axis]>0 for axis in "xyz") for e in b["elements"])
        assert b["geometryHash"] not in hashes
        hashes.add(b["geometryHash"])
    assert len(hashes)==18
    assert len(list(p.glob("P16-ARENA-*-seed-*.json")))==18
    print("P16_REAL_SOURCE_BLUEPRINTS=18 UNIQUE_HASHES=18 CSHARP_ROUTES=36 UNITY_PHYSX=NOT_RUN")
if __name__=="__main__":
    if len(sys.argv)!=2: raise SystemExit("Usage: validate_blueprints.py OUT_DIR")
    main(sys.argv[1])
