#!/usr/bin/env python3
"""CI41-W06 known-RED baseline regression: never mask source graph failures."""
import copy
from pathlib import Path
import sys
import unittest

HERE = Path(__file__).resolve().parent
sys.path.insert(0,str(HERE))
import verify_known_baseline as v

A="Assets/RoboRougelikeMechanics/MOV005OmniDrive/Tests/EditMode/RoboRougelike.MOV005OmniDrive.EditModeTests.asmdef"
B="Assets/RoboRougelikeMechanics/MOV006MagneticGrapple/Tests/EditMode/RoboRougelike.MOV006MagneticGrapple.EditModeTests.asmdef"
SHA="a"*40
BLOB_A="b"*40
BLOB_B="c"*40
ERRS=[
 {"code":"EDITMODE_ASSEMBLY_MUST_INCLUDE_EDITOR_ONLY","path":A,"detail":"[]"},
 {"code":"EDITMODE_ASSEMBLY_MUST_INCLUDE_EDITOR_ONLY","path":B,"detail":"[]"},
]
BASE={"schema":v.SCHEMA,"expected_asmdef_count":27,"known_errors":ERRS,
      "known_source_blob_pins":[{"path":A,"blob":BLOB_A},{"path":B,"blob":BLOB_B}]}
REPORT={"source_sha":SHA,"result":"FAIL_SOURCE_GRAPH","errors":ERRS,
        "tracked_asmdef_count":27,"native_unity":"NOT_RUN"}
ENTRIES={A:BLOB_A,B:BLOB_B}

class BaselineTest(unittest.TestCase):
    def compare(self,change=None,base=None,entries=None,source=SHA):
        return v.compare(change or copy.deepcopy(REPORT),
                         base or copy.deepcopy(BASE),
                         entries or dict(ENTRIES), source)

    def test_01_exact_known_2_red_is_open_never_source_pass(self):
        result=self.compare()
        self.assertEqual(result["status"],"KNOWN_BASELINE_DEFECTS_OPEN")
        self.assertEqual(result["strict_source_graph_result"],"FAIL_SOURCE_GRAPH")
        self.assertEqual(result["game_source_g0"],"HOLD_KNOWN_DEFECTS")
        self.assertEqual(result["known_defect_count"],2)
        print("TWO_REAL_UNFIXED_ASMDEF_CONTRACT_DEFECTS_STAY_EXPLICIT_RED")

    def test_02_new_unexpected_original_error_blocks_baseline(self):
        modified=copy.deepcopy(REPORT)
        modified["errors"].append({"code":"ASSEMBLY_REFERENCE_CYCLE",
                                   "path":A,"detail":"new"})
        self.assertEqual(self.compare(change=modified)["status"],"BASELINE_REVIEW_REQUIRED")

    def test_03_source_defect_blob_changed_rejects(self):
        modified=dict(ENTRIES)
        modified[A]="f"*40
        self.assertEqual(self.compare(entries=modified)["status"],"BASELINE_REVIEW_REQUIRED")

    def test_04_full_fix_requires_new_formal_baseline_review(self):
        modified=copy.deepcopy(REPORT)
        modified["errors"]=[]
        modified["result"]="PASS_SOURCE_GRAPH"
        self.assertEqual(self.compare(change=modified)["status"],"BASELINE_REVIEW_REQUIRED")
        print("FIXED_SOURCE_CANNOT_SILENTLY_REUSE_OPEN_DEFECT_BASELINE")

    def test_05_test_count_and_result_sha_drift_rejected(self):
        wrong=copy.deepcopy(REPORT)
        wrong["tracked_asmdef_count"]=28
        self.assertIn("ASMDEF_COUNT_DRIFT",self.compare(change=wrong)["baseline_issues"])
        wrong=copy.deepcopy(REPORT)
        wrong["source_sha"]="f"*40
        self.assertIn("SOURCE_SHA_REPORT_MISMATCH",self.compare(change=wrong)["baseline_issues"])

    def test_06_changed_or_unlisted_known_defects_rejected(self):
        wrong=copy.deepcopy(REPORT)
        wrong["errors"]=wrong["errors"][:1]
        self.assertIn("KNOWN_RED_ERROR_SET_DRIFT",self.compare(change=wrong)["baseline_issues"])
        changed=copy.deepcopy(BASE)
        changed["schema"]="v2"
        self.assertIn("INVALID_BASELINE_SCHEMA",self.compare(base=changed)["baseline_issues"])

    def test_07_missing_source_blob_blocks(self):
        wrong={A:BLOB_A}
        self.assertIn("MISSING_BASELINE_SOURCE_BLOB",
                      self.compare(entries=wrong)["baseline_issues"])

if __name__=="__main__":
    unittest.main(verbosity=2)
