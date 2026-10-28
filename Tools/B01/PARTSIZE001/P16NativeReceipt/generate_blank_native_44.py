#!/usr/bin/env python3
"""B01-23 / PASS16: immutable-source-native 44-pose BLANK intake.
No Unity execution; a generated NOT_RUN row never counts as evidence.
"""
from __future__ import annotations
import argparse
import copy
import json
import pathlib
import re
import subprocess
import sys

PART_SHA="be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
OPERATOR_SHA="26634727ab2323a982bce893421c88c330be46e8"
CATALOG_PATH="Assets/PARTSIZE001/Runtime/PartSizeBaselineCatalog.cs"
CATALOG_BLOB="6153d1758e800ec5c4210323610b061c06e8ea85"
KINDS=("Wheel","Gun","Suspension","Booster","JumpDrive","Blink","Gyroscope",
       "OmniDrive","MagneticGrapple","HoverEngine","CrawlerTrack")
YAWS=(0,90,180,270)

def load_original(repo):
    git=lambda *args: subprocess.check_output(
        ["git","-C",str(repo),*args],text=True,encoding="utf-8").strip()
    blob=git("rev-parse",PART_SHA+":"+CATALOG_PATH)
    if blob!=CATALOG_BLOB:
        raise ValueError("PARTSIZE_CATALOG_GIT_BLOB_MISMATCH:"+blob)
    return git("show",PART_SHA+":"+CATALOG_PATH)

def kinds_from_actual_source(source):
    names=re.findall(r'"([^"]+)"',_one(source,r"CanonicalNames\s*=\s*\{([\s\S]*?)\};"))
    wide=set(re.findall(r'"([^"]+)"',_one(source,r"private static readonly HashSet<string> Wide[\s\S]*?new\[\]\{([\s\S]*?)\}")))
    logical=[x for x in names if x not in wide and x not in ("Armor","Core")]
    if len(names)!=19 or len(set(names))!=19 or len(wide)!=6 or tuple(logical)!=KINDS:
        raise ValueError("CANONICAL_ELEVEN_KIND_SOURCE_DRIFT")
    if "return PartSizeReferenceFixtures.Unmeasured(canonicalKind," not in source or "new Cell3(1,1,1));" not in source:
        raise ValueError("UNMEASURED_ONE_CELL_SOURCE_CONTRACT_DRIFT")
    return logical

def _one(source,regex):
    found=re.findall(regex,source)
    if len(found)!=1:raise ValueError("ORIGINAL_CATALOG_ANCHOR_NOT_UNIQUE")
    return found[0]

def blank_row(kind,yaw):
    return {
      "case_id":f"PART_{kind.upper()}_Y{yaw:03d}",
      "kind":kind,"yaw_deg":yaw,
      "logical_cells":"1x1x1",
      "source_shas":{"partsize_producer":PART_SHA},
      "scene":"Assets/PARTSIZE001/Demo/PARTSIZE001_Demo.unity",
      "native_status":"NOT_RUN",
      # ALL physical numbers and evidence intentionally absent until real Editor.
      "actual_renderer_bounds_min_m":None,"actual_renderer_bounds_max_m":None,
      "actual_collider_bounds_min_m":None,"actual_collider_bounds_max_m":None,
      "actual_collider_types_and_count":None,
      "actual_pivot_world_xyz_m":None,"actual_enabled_trigger_state":None,
      "actual_neighbor_part_id":None,"actual_overlap_depth_m":None,
      "actual_typed_socket_id":None,"actual_typed_socket_owner_id":None,
      "actual_socket_face_normal":None,"actual_socket_contact_verified":None,
      "actual_moving_sweep_result":None,
      "unity_nunit_case_name":None,"original_editor_log_sha256":None,
      "original_nunit_xml_sha256":None,"unity_exit_code":None,
      "screenshot_sha256":None,"physical_failure_reason":None
    }

def build_template(source):
    kinds=kinds_from_actual_source(source)
    return {"schema":"B01-PARTSIZE-NATIVE-44-INTAKE-v1",
     "owner":"B01-23", "intended_native_qa":"B01-60",
     "producer_source_sha":PART_SHA,"qa_operator_source_sha":OPERATOR_SHA,
     "catalog_git_blob":CATALOG_BLOB,"unity_version_required":"6000.3.14f1",
     "evidence_origin":"BLANK_SOURCE_TEMPLATE_NOT_A_UNITY_EXECUTION",
     "gates":{"G0":"SOURCE_ONLY","G1":"NOT_RUN","G2":"HOLD"},
     "native_case_count":44,"measurements":
       [blank_row(kind,yaw) for kind in kinds for yaw in YAWS]}

def validate_blank(data):
    if not isinstance(data,dict) or set(data)!=set(build_template_dummy().keys()):
        raise ValueError("BAD_ROOT_SCHEMA")
    cases=data.get("measurements")
    if not isinstance(cases,list) or len(cases)!=44:
        raise ValueError("NOT_44_NATIVE_POSES")
    ids=set()
    for row in cases:
        if not isinstance(row,dict) or row["case_id"] in ids:
            raise ValueError("DUPLICATE_OR_INVALID_CASE")
        ids.add(row["case_id"])
        if row.get("native_status")!="NOT_RUN":
            raise ValueError("UNAUTHORIZED_FAKE_NATIVE_RESULT")
        if any(v is not None for k,v in row.items() if
                k.startswith("actual_") or k.endswith("_sha256") or
                k in ("unity_nunit_case_name","unity_exit_code","screenshot_sha256","physical_failure_reason")):
            raise ValueError("ORIGINAL_NATIVE_EVIDENCE_WAS_NOT_RUN")
    if data.get("producer_source_sha")!=PART_SHA or data.get("qa_operator_source_sha")!=OPERATOR_SHA:
        raise ValueError("SOURCE_VERSION_NOT_PINNED")
    if data.get("gates")!={"G0":"SOURCE_ONLY","G1":"NOT_RUN","G2":"HOLD"}:
        raise ValueError("MUST_NOT_CLAIM_NATIVE_ACCEPTANCE")
    return True

def build_template_dummy():
    # shape only: keep this check independent of dynamically supplied Git text.
    return {"schema":None,"owner":None,"intended_native_qa":None,
    "producer_source_sha":None,"qa_operator_source_sha":None,
    "catalog_git_blob":None,"unity_version_required":None,
    "evidence_origin":None,"gates":None,"native_case_count":None,
    "measurements":None}

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--repo",default=".")
    ap.add_argument("--out",default="Docs/B01/PARTSIZE001/P16_BLANK_NATIVE_44.json")
    ap.add_argument("--check",action="store_true")
    args=ap.parse_args()
    result=build_template(load_original(args.repo))
    validate_blank(result)
    encoded=json.dumps(result,ensure_ascii=False,indent=2,sort_keys=True)+"\n"
    target=pathlib.Path(args.out)
    if args.check:
        if not target.exists() or target.read_text(encoding="utf8")!=encoded:
            raise ValueError("COMMITTED_44_NATIVE_INTAKE_DRIFT")
    else:
        target.parent.mkdir(parents=True,exist_ok=True)
        target.write_text(encoded,encoding="utf8")
    print("B01_23_P16_BLANK_44_SOURCE_RECEIPT_PASS; 44/44 NATIVE=NOT_RUN; G1=HOLD; G2=HOLD")

if __name__=="__main__":
    try:main()
    except Exception as e:
        print("B01_23_P16_SOURCE_BLANK_RECEIPT_FAIL:"+str(e),file=sys.stderr)
        sys.exit(1)
