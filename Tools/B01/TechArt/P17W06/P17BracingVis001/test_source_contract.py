#!/usr/bin/env python3
"""Executed negative tests against unchanged original source-locked 43-cell rig data.
Errors must be detected before generating any proxy mesh or game-facing archive.
"""
from __future__ import annotations
from copy import deepcopy
import argparse
import json
from pathlib import Path
from source_contract import load_pinned,validate,ensure_git_blobs

def main():
    p=argparse.ArgumentParser();p.add_argument("--source",required=True,type=Path)
    a=p.parse_args()
    rigs,kit,blobs,report=load_pinned(a.source)
    assert report["total_parts"]==43
    assert report["highlighted_source_braces"]==5
    assert report["source_predicted_mitigation_pairs"]==6
    tests=[
      ("duplicate-owner-ID",lambda r,k:r["Flanker"]["parts"][1].update(localId=r["Flanker"]["parts"][0]["localId"])),
      ("duplicate-logical-cell",lambda r,k:r["Bulwark"]["parts"][1].update(cell=deepcopy(r["Bulwark"]["parts"][0]["cell"]))),
      ("noninteger-grid-position",lambda r,k:r["Skirmisher"]["parts"][0]["cell"].update(x=0.25)),
      ("missing-source-brace",lambda r,k:r["Bulwark"]["parts"][-1].update(localId="P17-FORGED-NOT-BRACE")),
      ("tampered-brace-location",lambda r,k:k["kits"][0]["mandatoryParts"][0]["cell"].update(z=3)),
      ("unapproved-rotation",lambda r,k:r["Flanker"]["parts"][0]["rotationQuarterTurns"].update(y=1)),
      ("collider-status-falsely-G1",lambda r,k:r["Bulwark"].update(nativePhysicalStatus="PASS")),
      ("source-commit-drift",lambda r,k:k.update(originalPartSizeCommit="bad")),
      ("source-orphan-claim-drift",lambda r,k:k["kits"][2]["expectedMitigations"][0].update(previouslyOrphanedPartId="FORGED-PART")),
      ("contact-mask-drift",lambda r,k:r["Skirmisher"]["parts"][0].update(contactMask=0)),
      ("unsupported-part-kind",lambda r,k:r["Flanker"]["parts"][0].update(kind="NotAuthorized")),
      ("duplicate-variant",lambda r,k:k["kits"][1].update(variantId=k["kits"][0]["variantId"])),
    ]
    receipts=[]
    for name,mutation in tests:
        trial_rigs,trial_kit=deepcopy(rigs),deepcopy(kit)
        mutation(trial_rigs,trial_kit)
        try:validate(trial_rigs,trial_kit)
        except ValueError as exc:
            receipts.append({"case":name,"outcome":"EXPECTED_REJECTION","error":str(exc)})
        else:raise AssertionError("FALSE_GREEN_NEGATIVE:"+name)
    print("B01_30_P17_SOURCE_LOCKED_43_PARTS_5_BRACES_12_NEGATIVE_TESTS_PASS",
          json.dumps({"rigs":3,"parts":43,"braces":5,"negatives":len(receipts)},sort_keys=True))
    return receipts

if __name__=="__main__":main()
