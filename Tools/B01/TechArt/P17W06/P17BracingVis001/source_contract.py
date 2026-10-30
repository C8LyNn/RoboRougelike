#!/usr/bin/env python3
"""B01-30: read-only B01-23 P17 bracing 3D visual fixture source contract.
PROXIES ONLY: logical 0.55m grid, not Unity colliders/mount sockets or physics.
The original game-candidate JSON (PR #375) remains frozen and unchanged.
"""
from __future__ import annotations
from collections import Counter
from copy import deepcopy
import hashlib
import json
from pathlib import Path

ORIGINAL_PR=375
ORIGINAL_HEAD="5666a39eabd4cea91b2b605a9eb99ea2f1e226e8"
ORIGINAL_PARTSIZE="be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
ORIGINAL_WEAKPOINT="098ccdbe8f129253fa4ada52318436b7cc2b4dc4"
GRID_M=0.55  # #141 observed grid, NOT new canonical dimension or collider approval
PROXY_BOX_M=0.42  # intentionally inset, purely visual
RIG_FILES={
 "Flanker": ("P17_Flanker_Reinforced.json","7259e3454b2f7dd72ca66de83673c143075a807f",15,1),
 "Bulwark": ("P17_Bulwark_Reinforced.json","8b00e5da047a22bc23644639e3292c447dd6b335",16,2),
 "Skirmisher": ("P17_Skirmisher_Reinforced.json","edf332a66645c5ea4374b1957d2cc1a7d356e4d2",12,2),
}
KIT_NAME="P17_StructuralBracingKits.json"
KIT_BLOB="9b3d2403e7f6e32ea6e20f9f5bb0a83b859bc6e1"
KINDS={"Core","Armor","Gun","Wheel","CrawlerTrack","HoverEngine"}

def err(message:str):
    raise ValueError("B01-30-P17-VIS-SOURCE-REJECT:"+message)

def raw_assets(source:Path):
    found={}
    for name,(filename,gitblob,count,extra) in RIG_FILES.items():
        path=source/filename
        if not path.is_file():err("SOURCE_MISSING:"+filename)
        found[name]=json.loads(path.read_text(encoding="utf8"))
    p=source/KIT_NAME
    if not p.is_file():err("KIT_MISSING")
    return found,json.loads(p.read_text(encoding="utf8"))

def validate(rigs:dict,kit:dict):
    if kit.get("schema")!="B01-P17-W02-STRUCTURAL-BRACING-KITS-v1" \
       or kit.get("packageId")!="PARTSIZE001-P17-W02-STRUCTURAL-BRACING-R1" \
       or kit.get("originalPartSizeCommit")!=ORIGINAL_PARTSIZE \
       or kit.get("originalWeakpointCommit")!=ORIGINAL_WEAKPOINT \
       or kit.get("nativePhysicalStatus")!="NOT_RUN" \
       or kit.get("gameIntegrationStatus")!="NOT_INTEGRATED":
        err("KIT_ID_OR_NATIVE_STATUS_CHANGED")
    kits=kit.get("kits")
    if not isinstance(kits,list) or len(kits)!=3:err("KIT_COUNT")
    actual={p.get("variantId"):p for p in kits}
    if len(actual)!=3:err("DUPLICATED_VARIANT")
    summary={}
    total=braces=mitigations=0
    for name,(filename,blob,expected,expected_braces) in RIG_FILES.items():
        rig=rigs.get(name)
        if not isinstance(rig,dict):err("NO_RIG:"+name)
        if rig.get("schema")!="B01-PARTSIZE-P17-RIG-BLUEPRINT-v1" \
           or rig.get("sourcePartSizeCommit")!=ORIGINAL_PARTSIZE \
           or rig.get("contentStatus")!="GAME_CONTENT_CANDIDATE_NOT_INTEGRATED" \
           or rig.get("nativePhysicalStatus")!="UNMEASURED_COLLIDER_AND_SOCKET":
            err("RIG_SOURCE_OR_STATUS:"+name)
        parts=rig.get("parts")
        if not isinstance(parts,list) or len(parts)!=expected:err("WRONG_PART_COUNT:"+name)
        linked=actual.get(rig.get("blueprintId"))
        if not linked:err("KIT_NOT_LINKED:"+name)
        mandatory=linked.get("mandatoryParts")
        target=linked.get("expectedMitigations")
        if not isinstance(mandatory,list) or len(mandatory)!=expected_braces:err("BRACE_COUNT:"+name)
        if not isinstance(target,list) or len(target)<1:err("MITIGATION_COUNT:"+name)
        byid={}
        cells=set()
        for part in parts:
            if not isinstance(part,dict):err("NON_OBJECT_PART:"+name)
            ident=part.get("localId")
            if not isinstance(ident,str) or not ident or ident in byid:err("DUPLICATE_LOCAL_ID:"+name)
            if part.get("kind") not in KINDS:err("UNKNOWN_KIND:"+name)
            c=part.get("cell")
            if not isinstance(c,dict) or set(c.keys())!={"x","y","z"} \
               or any(type(c[ax]) is not int or abs(c[ax])>16 for ax in ("x","y","z")):
                err("INVALID_INTEGER_CELL:"+name)
            loc=tuple(c[ax] for ax in ("x","y","z"))
            if loc in cells:err("OVERLAPPING_LOGICAL_CELLS:"+name)
            cells.add(loc)
            quarter=part.get("rotationQuarterTurns")
            if quarter!={"x":0,"y":0,"z":0}:err("UNEXPECTED_ROTATION:"+name)
            if part.get("contactMask")!=63:err("UNEXPECTED_CONTACT_MASK:"+name)
            if part.get("weapon") not in (None,"Basic"):err("UNKNOWN_WEAPON:"+name)
            byid[ident]=part
        braceids=set()
        for part in mandatory:
            ident=part.get("localId")
            if ident not in byid or ident in braceids:err("MISSING_OR_REPEATED_BRACE:"+name)
            if part!=byid[ident] or part.get("kind")!="Armor" or not ident.startswith("P17-BRACE-"):
                err("BRACE_CONTENT_MISMATCH:"+name)
            braceids.add(ident)
        for t in target:
            if t.get("removedPartId") not in byid or t.get("previouslyOrphanedPartId") not in byid:
                err("MITIGATION_SOURCE_ID_MISSING:"+name)
        summary[name]={"blueprintId":rig["blueprintId"],"original_owner_cells":expected,
          "original_brace_ids":sorted(braceids),"braces":len(braceids),
          "mitigation_pairs":len(target),
          "types":dict(sorted(Counter(part["kind"] for part in parts).items())),
          "grid_aabb_cell_bounds":{
            "min":{ax:min(p["cell"][ax] for p in parts) for ax in ("x","y","z")},
            "max":{ax:max(p["cell"][ax] for p in parts) for ax in ("x","y","z")}
          }}
        total+=len(parts);braces+=len(braceids);mitigations+=len(target)
    if (total,braces,mitigations)!=(43,5,6):err("WRONG_GLOBAL_COUNTS")
    return {"total_parts":total,"highlighted_source_braces":braces,
            "source_predicted_mitigation_pairs":mitigations,"rigs":summary}

def ensure_git_blobs(source:Path,strict=True):
    expected={v[0]:v[1] for v in RIG_FILES.values()}
    expected[KIT_NAME]=KIT_BLOB
    for filename,sha in expected.items():
        raw=(source/filename).read_bytes()
        # SHA1 Git blob with exact original source bytes
        actual=hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\x00"+raw).hexdigest()
        if actual!=sha:err("SOURCE_GIT_BLOB_DRIFT:"+filename+":"+actual)
    return dict(sorted(expected.items()))

def load_pinned(source:Path):
    blobs=ensure_git_blobs(source)
    rigs,kit=raw_assets(source)
    return rigs,kit,blobs,validate(rigs,kit)

if __name__=="__main__":
    import argparse
    parser=argparse.ArgumentParser()
    parser.add_argument("--source",type=Path,required=True)
    a=parser.parse_args()
    rig,kit,sha,report=load_pinned(a.source)
    print("B01_30_P17_ORIGINAL_B01_23_SOURCE_4_BLOBS_43_PARTS_5_BRACES_PASS",
          json.dumps(report,sort_keys=True))
