#!/usr/bin/env python3
"""B01-60 PASS16/CX03 independent provenance of Codex native D01 EditMode XML.

Consumes ORIGINAL raw NUnit XML and exit objects, refuses malformed XML, test
duplicates, suite-summary inconsistency and 3 historical RED -> GREEN drift.
It is *receipt verification*, not an independent Unity engine execution.
"""
import copy
import json
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
SHA="589b60a912096d1e9917cb0bbac73a53e7543b4f"
PREFIX="Docs/CX03/Evidence/"
BLOBS={
"red.xml":"f73bc2441dcbe50d4c76fe802de2832e5c6a49b1",
"red-exit.txt":"78c6baefdd2d13b3080b53d794e90471df3c4bf2",
"regression.xml":"aba05e2056cc917c13f69992b730b121451781f5",
"regression-exit.txt":"18748286e5b8b4de5db905f87cdfed1a7d48fe60",
}
CORE={
"InvalidWeaponTransactionTests.HealthyValidTransactionsPreserveGuidAndChargeOnce",
"InvalidWeaponTransactionTests.LedgerRejectsMixedInvalidEquipmentAtomically",
"InvalidWeaponTransactionTests.RejectsMixedInvalidSalvageWithoutGrantingOwnership",
"InvalidWeaponTransactionTests.RejectsRepairAndDismantleOfCorruptedOwnedGunWithoutMutation",
}
OLD_FAIL={
"InvalidWeaponTransactionTests.LedgerRejectsMixedInvalidEquipmentAtomically",
"InvalidWeaponTransactionTests.RejectsMixedInvalidSalvageWithoutGrantingOwnership",
"InvalidWeaponTransactionTests.RejectsRepairAndDismantleOfCorruptedOwnedGunWithoutMutation",
}
class Invalid(Exception):pass
def require(condition,why):
    if not condition:raise Invalid(why)
def git(*args):
    return subprocess.run(["git","-C",str(ROOT),*args],check=True,
                          stdout=subprocess.PIPE,stderr=subprocess.PIPE).stdout
def originals():
    git("fetch","--no-tags","origin",SHA)
    res={}
    for name,expected in BLOBS.items():
        raw=git("show",f"{SHA}:{PREFIX}{name}")
        h=subprocess.run(["git","-C",str(ROOT),"hash-object","--stdin"],input=raw,
                         check=True,stdout=subprocess.PIPE).stdout.decode().strip()
        require(h==expected,"ORIGINAL_BLOB_CHANGED:"+name)
        res[name]=raw
    return res

def validate_nunit(raw,expected_total,expected_failed,root_result):
    root=ET.fromstring(raw)
    require(root.tag=="test-run","ROOT_IS_NOT_NUNIT")
    require(root.get("result")==root_result,"WRONG_ROOT_RESULT")
    nodes=list(root.findall(".//test-case"))
    require(len(nodes)==expected_total,"CASE_COUNT_MISMATCH")
    summary={k:int(root.get(k,"-1")) for k in ("total","passed","failed","skipped","inconclusive")}
    require(summary=={"total":expected_total,"passed":expected_total-expected_failed,
                      "failed":expected_failed,"skipped":0,"inconclusive":0},
            "ROOT_SUMMARY_MISMATCH")
    results={}
    for c in nodes:
        name=c.get("name","")
        cls=c.get("classname","")
        full=c.get("fullname","")
        require(name and cls and full==cls+"."+name,"CASE_FULLNAME_MISMATCH")
        require(full not in results,"DUPLICATE_NUNIT_CASE")
        require(c.get("result") in ("Passed","Failed"),"CASE_NOT_EXECUTED")
        require(c.get("runstate")!="Ignored","IGNORED_CASE")
        results[full]=c.get("result")
    require(sum(v=="Failed" for v in results.values())==expected_failed,
            "CASE_FAILURE_SUMMARY_MISMATCH")
    require(CORE.issubset(results),"CORE_CASE_MISSING")
    return results

def change_case_to_skipped(raw):
    root=ET.fromstring(raw)
    first=root.find('.//test-case')
    first.set('result','Skipped')
    return ET.tostring(root,encoding='utf-8')

def main():
    inputs=originals()
    require(inputs["red-exit.txt"].strip()==b"2","BEFORE_NOT_RED_EXIT")
    require(inputs["regression-exit.txt"].strip()==b"0","AFTER_NOT_GREEN_EXIT")
    before=validate_nunit(inputs["red.xml"],4,3,"Failed(Child)")
    after=validate_nunit(inputs["regression.xml"],42,0,"Passed")
    require(set(before)==CORE,"UNEXPECTED_RED_CASES")
    require({k for k,v in before.items() if v=="Failed"}==OLD_FAIL,
            "NOT_THE_ORIGINAL_THREE_RED")
    require(all(after[k]=="Passed" for k in CORE),"CORE_STILL_RED")
    require(len(set(after)-CORE)==38,"MISSING_POSITIVE_REGRESSIONS")
    negatives=[]
    baseline=inputs["regression.xml"]
    trials=[
        ("truncate",lambda raw:raw[:100]),
        ("wrong_root_green",lambda raw:raw.replace(b'result="Passed"',b'result="Failed"',1)),
        ("skipped_case",change_case_to_skipped),
        ("missing_case",lambda raw:raw.replace(b'total="42"',b'total="41"',1)),
        ("duplicate_fullname",lambda raw:raw.replace(
            b'fullname="InvalidWeaponTransactionTests.LedgerRejectsMixedInvalidEquipmentAtomically"',
            b'fullname="InvalidWeaponTransactionTests.HealthyValidTransactionsPreserveGuidAndChargeOnce"',1)),
    ]
    for name,mutate in trials:
        try:validate_nunit(mutate(baseline),42,0,"Passed")
        except (Invalid,ET.ParseError):negatives.append(name)
        else:raise Invalid("MUTATION_ACCEPTED:"+name)
    print("B0160_CX03_JSON="+json.dumps({
        "role":"B01-60","codex_original_evidence_sha":SHA,
        "input_blob_shas":BLOBS,"real_original_red":{"cases":4,"failed":3,"exit":2},
        "real_original_native_green":{"cases":42,"passed":42,"exit":0},
        "original_three_red_now_green":sorted(OLD_FAIL),
        "synthetic_receipt_mutations_rejected":negatives,
        "evidence_kind":"ORIGINAL_RECORDED_UNITY_NUNIT_NOT_RERUN",
        "independent_native_g1":"NOT_RUN","source_integrated":"CODEX_DRAFT_ONLY"
    },sort_keys=True))

if __name__=="__main__":main()
