#!/usr/bin/env python3
"""READ-ONLY PARTSIZE R1 native evidence intake. Parser test PASS != Unity PASS.
Pinned producer remains unchanged: PR286@be2be0e. No G1/G2 auto approval.
"""
import argparse
from collections import Counter
import csv
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

SOURCE_SHA="be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
UNITY="6000.3.14f1"
SOURCE_PREFIX="Assets/PARTSIZE001/Tests"
SOURCES={
 "EditMode":(f"{SOURCE_PREFIX}/EditMode/PartSize001EditModeTests.cs",
             "7e6261c84ddc98a5a005c16c5bb3a85ec4df853d"),
 "PlayMode":(f"{SOURCE_PREFIX}/PlayMode/PartSize001PlayModeTests.cs",
             "b8add79ea3fe98c55273d8226af0c4dc2f8ce1e9"),
}
CLASSES={"EditMode":"B01.PARTSIZE001.Tests.PartSize001EditModeTests",
         "PlayMode":"B01.PARTSIZE001.Tests.PartSize001PlayModeTests"}
EXPECTED={
 "EditMode":{
  "LegacyCubeSourceDimensionsFitOneGridCell":1,
  "LegacyWideFourCellsHaveOneOwnerAndReal107CmHull":1,
  "AllXYZQuarterTurnFixturesFitTheSamePhysicalEnvelope":7,
  "WideInteriorFacesAreNotRealExternalMountPoints":1,
  "RotatedLogicalCellsActuallyBlockAnExistingOwner":1,
  "PhysicalBoxCannotClaimOneCellFor120CmHull":1,
  "NoRealColliderMeasurementsNeverTurnIntoFakeApproval":1,
  "RejectNaNInfinityIncompleteOrNonpositivePhysicalHull":1,
  "InvalidHugeGridAllocationIsNeverAccepted":1,
  "CanonicalR3NineteenTypesNeverInferWheelOrGunPhysicalExtent":1,
  "RotationPeriod4AndNegativeTurnsRemainEquivalent":1,
 },
 "PlayMode":{
  "AdjacentLegacyCubesDoNotPenetrate":1,
  "RealPhysXRejectsTwoBodiesPenetrating":1,
  "RealColliderBiggerThanDeclaredCellsIsRejected":1,
  "WidePhysicalColliderYaw90FitsRotatedFourCells":1,
  "UnverifiedBroadphaseNeverCertifiesPhysicalContact":1,
  "DecorativeChildCannotBeSilentlyOmittedFromInventory":1,
  "OmittedPhysicalChildCannotBypassAuthoritativeInventory":1,
  "W17_LyingCompleteExternalInventoryCannotHideOverlappingBox":1,
  "W17_IrrelevantExternalListCannotMaskOmittedPenetrator":1,
  "W17_ExposedGridFaceCannotAuthorizePhysicalMountSocket":1,
  "DemoProducesThreePhysicallyCheckedColorStates":1,
 }}
UNMEASURED=("Wheel","Gun","Suspension","Booster","JumpDrive","Blink",
            "Gyroscope","OmniDrive","MagneticGrapple","HoverEngine","CrawlerTrack")
EXPECTED_COUNTS={"EditMode":17,"PlayMode":11}

class EvidenceError(ValueError): pass

def git_output(root, *args):
    return subprocess.check_output(["git","-C",str(root),*args],stderr=subprocess.PIPE)

def assert_producer_source(root):
    """Compare actual immutable Git blobs, not checkout-smudged CRLF copies."""
    for platform,(path,blob) in SOURCES.items():
        actual=git_output(root,"rev-parse",f"{SOURCE_SHA}:{path}").decode().strip()
        if actual!=blob:
            raise EvidenceError("SOURCE_BLOB_DRIFT:"+platform)
        source=git_output(root,"show",f"{SOURCE_SHA}:{path}").decode("utf8")
        method_pattern=r"\[(?:Test|TestCase\([^\]]*\)|UnityTest)\]\s*public\s+(?:void|IEnumerator)\s+(\w+)\s*\("
        # Each TestCase line before a method may be separated by several
        # additional TestCase attributes; extract methods and TestCase count.
        methods=set(re.findall(r"\bpublic\s+(?:void|IEnumerator)\s+(\w+)\s*\(",source))
        methods.discard("Cleanup")
        if methods!=set(EXPECTED[platform]):
            raise EvidenceError("SOURCE_TEST_METHOD_DRIFT:"+platform)
        n=source.count("[Test]")+source.count("[TestCase(")+source.count("[UnityTest]")
        if n!=EXPECTED_COUNTS[platform]:
            raise EvidenceError("SOURCE_TEST_CASE_DRIFT:"+platform)
    catalog_path="Assets/PARTSIZE001/Runtime/PartSizeBaselineCatalog.cs"
    actual=git_output(root,"rev-parse",f"{SOURCE_SHA}:{catalog_path}").decode().strip()
    if actual!="6153d1758e800ec5c4210323610b061c06e8ea85":
        raise EvidenceError("BASELINE_CATALOG_BLOB_DRIFT")
    catalog=git_output(root,"show",f"{SOURCE_SHA}:{catalog_path}").decode()
    kinds=catalog.split("CanonicalNames = {",1)[1].split("};",1)[0]
    names=re.findall(r'"([^"]+)"',kinds)
    if len(names)!=19 or [k for k in names if k not in
        ("Armor","Core","BonusModule","DirectionalShield","PointDefense",
         "StructuralScanner","ScannerPulse","IndependentSteering")]!=list(UNMEASURED):
        raise EvidenceError("UNMEASURED_CATALOG_MISMATCH")
    csv_path="Docs/B01/PARTSIZE001/observed_r3_size_matrix.csv"
    rows=list(csv.DictReader(git_output(root,"show",f"{SOURCE_SHA}:{csv_path}").decode().splitlines()))
    if len(rows)!=19 or [r["part_kind"] for r in rows]!=names:
        raise EvidenceError("SOURCE_CSV_CATALOG_DRIFT")
    for row in rows:
        if row["part_kind"] in UNMEASURED and (
            row["actual_unity_collider_measured"]!="NOT_RUN" or
            row["mesh_aabb_measured_m"] or row["socket_contact_measured"] or
            row["functional_clearance_measured"] or row["source_derived_box_x_m"]):
            raise EvidenceError("FALSE_APPROVAL_OF_UNMEASURED:"+row["part_kind"])
    return {"source_sha":SOURCE_SHA,"verified_native_methods":17+11,
            "physically_unmeasured":list(UNMEASURED),
            "native": "NOT_RUN", "status":"SOURCE_INVENTORY_VERIFIED_ONLY"}

def check_xml(raw,platform):
    try: root=ET.fromstring(raw)
    except ET.ParseError as exc: raise EvidenceError("BROKEN_NUNIT_XML") from exc
    if root.tag!="test-run" or root.get("result")!="Passed":
        raise EvidenceError("NUNIT_RUN_NOT_PASSED")
    cases=list(root.iter("test-case"))
    expected=EXPECTED[platform]
    if len(cases)!=EXPECTED_COUNTS[platform]:
        raise EvidenceError("MISSING_OR_EXTRA_NUNIT_CASES")
    if root.get("total") and int(root.get("total"))!=len(cases):
        raise EvidenceError("NUNIT_TOTAL_MISMATCH")
    if root.get("passed") and int(root.get("passed"))!=len(cases):
        raise EvidenceError("NUNIT_PASSED_MISMATCH")
    got=Counter()
    fullnames=set()
    for c in cases:
        if c.get("result")!="Passed":
            raise EvidenceError("FAILED_SKIPPED_OR_INCONCLUSIVE_NUNIT_CASE")
        full=c.get("fullname") or ""
        cls=c.get("classname") or ""
        name=c.get("name") or ""
        if cls!=CLASSES[platform] or not full.startswith(cls+".") or not name:
            raise EvidenceError("FOREIGN_OR_MALFORMED_NUNIT_CASE")
        method=name.split("(",1)[0]
        if method not in expected:
            raise EvidenceError("FOREIGN_OR_SUBSTITUTED_TEST_METHOD")
        if full in fullnames:
            raise EvidenceError("DUPLICATE_NUNIT_FULLNAME")
        fullnames.add(full)
        got[method]+=1
    if got!=Counter(expected):
        raise EvidenceError("MISSING_EXPECTED_TEST_METHOD")
    return {"cases":len(cases),"methods":len(expected),"all_passed":True}

def inspect_receipt(folder):
    f=folder/"receipt.json"
    if not f.is_file():
        return {"status":"NOT_RUN","reason":"NO_NATIVE_RECEIPT"}
    try: receipt=json.loads(f.read_text(encoding="utf8-sig"))
    except (json.JSONDecodeError,UnicodeError) as exc:
        raise EvidenceError("BROKEN_RECEIPT") from exc
    if receipt.get("source_sha")!=SOURCE_SHA or receipt.get("unity_version")!=UNITY:
        raise EvidenceError("WRONG_SOURCE_OR_UNITY_VERSION")
    stages={}
    for stage in ("EditMode","PlayMode"):
        data=receipt.get(stage)
        if not isinstance(data,dict):
            return {"status":"NOT_RUN","reason":"MISSING_STAGE_"+stage}
        if data.get("exit_code")!=0:
            raise EvidenceError("NONZERO_UNITY_EXIT_"+stage)
        xml=folder/(stage+".xml")
        log=folder/(stage+".log")
        if not xml.is_file() or not log.is_file():
            return {"status":"NOT_RUN","reason":"MISSING_NATIVE_FILES_"+stage}
        log_text=log.read_text(encoding="utf8",errors="replace")
        if UNITY not in log_text or len(log_text)<30:
            raise EvidenceError("EDITOR_LOG_WITHOUT_UNITY_VERSION_"+stage)
        if not data.get("xml_sha256") or not data.get("log_sha256"):
            raise EvidenceError("MISSING_NATIVE_BYTES_HASH_"+stage)
        for path,key in ((xml,"xml_sha256"),(log,"log_sha256")):
            if hashlib.sha256(path.read_bytes()).hexdigest()!=data[key]:
                raise EvidenceError("NATIVE_RECEIPT_TAMPER_"+stage)
        stages[stage]=check_xml(xml.read_bytes(),stage)
    return {"status":"STRUCTURAL_EVIDENCE_REVIEW_REQUIRED",
            "native_g1":"PENDING_INDEPENDENT_B01_60",
            "source_sha":SOURCE_SHA,"stages":stages,"G2":"HOLD"}

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("command",choices=("source","receipt"))
    parser.add_argument("--repo",default=".")
    parser.add_argument("--dir",default=".")
    args=parser.parse_args()
    try:
        value=assert_producer_source(Path(args.repo)) if args.command=="source" else inspect_receipt(Path(args.dir))
    except (EvidenceError,subprocess.CalledProcessError,OSError) as exc:
        print(json.dumps({"status":"FAIL_CLOSED","error":str(exc),"G2":"HOLD"}))
        return 2
    print(json.dumps(value,sort_keys=True,indent=2))
    return 0 if value.get("status")!="NOT_RUN" else 3

if __name__=="__main__":
    sys.exit(main())
