#!/usr/bin/env python3
"""Actual Codex original uploaded NUnit + process evidence; negative mutations test-only."""
import copy
from pathlib import Path
import tempfile
import json
import unittest
import xml.etree.ElementTree as ET
import verify_codex_native_evidence as m

REPO=Path(__file__).resolve().parents[3]

class OriginalCodexNativeReceiptIntake(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.data=m.original(REPO)
        cls.result=m.evaluate(cls.data)

    def changed(self):
        return dict(self.data)

    def test_01_actual_pinned_evidence_preserves_full_fail_and_green_playmode(self):
        r=self.result
        self.assertEqual(r["editmode_original"],{"total":1303,"passed":1288,"failed":15,"skipped":0})
        self.assertEqual(r["playmode_original"],{"total":474,"passed":474,"failed":0,"skipped":0})
        self.assertEqual(r["targeted_editor_retest"],{"total":15,"passed":15,"failed":0,"skipped":0})
        self.assertIn("HOLD",r["release_gate"])
        self.assertEqual(r["independent_unity_execution"],"NOT_RUN")

    def test_02_original_compile_build_and_play_exits_but_edit_original_exit_2(self):
        self.assertEqual({x:y["exit"] for x,y in self.result["processes"].items()},
                         {"Compile":0,"Build":0,"EditMode":2,"PlayMode":0})

    def test_03_cannot_forge_full_editor_green_by_changing_json_process_exit(self):
        z=self.changed()
        obj=json.loads(z["EditMode-process.json"]);obj["exitCode"]=0
        z["EditMode-process.json"]=json.dumps(obj).encode()
        with self.assertRaisesRegex(m.EvidenceReject,"PROCESS_EXIT_DOES_NOT_MATCH_NUNIT"):
            m.evaluate(z)

    def test_04_wrong_unity_source_ref_is_denied(self):
        z=self.changed()
        obj=json.loads(z["PlayMode-process.json"]);obj["source"]="0"*40
        z["PlayMode-process.json"]=json.dumps(obj).encode()
        with self.assertRaisesRegex(m.EvidenceReject,"PROCESS_SOURCE_OR_PHASE_MISMATCH"):
            m.evaluate(z)

    def test_05_fake_pass_header_with_actual_failed_tests_cannot_pass(self):
        z=self.changed()
        x=ET.fromstring(z["EditMode.xml"])
        x.set("result","Passed");x.set("failed","0");x.set("passed","1303")
        z["EditMode.xml"]=ET.tostring(x)
        with self.assertRaisesRegex(m.EvidenceReject,"NUNIT_ACTUAL_CASES_DIFFER_FROM_HEADER"):
            m.evaluate(z)

    def test_06_full_suite_failure_count_is_not_erased_by_targeted_green(self):
        z=self.changed()
        x=ET.fromstring(z["EditMode.xml"])
        x.set("failed","0")
        z["EditMode.xml"]=ET.tostring(x)
        with self.assertRaises(m.EvidenceReject):
            m.evaluate(z)

    def test_07_retest_must_have_fifteen_actual_pass_cases(self):
        z=self.changed()
        x=ET.fromstring(z["retest.xml"])
        first=next(x.iter("test-case"));first.set("result","Failed")
        z["retest.xml"]=ET.tostring(x)
        with self.assertRaisesRegex(m.EvidenceReject,"NUNIT_ACTUAL_CASES_DIFFER_FROM_HEADER"):
            m.evaluate(z)

    def test_08_duplicate_result_test_case_is_not_counted_twice(self):
        z=self.changed()
        x=ET.fromstring(z["retest.xml"])
        cases=list(x.iter("test-case"))
        cases[1].set("fullname",cases[0].get("fullname"))
        z["retest.xml"]=ET.tostring(x)
        with self.assertRaisesRegex(m.EvidenceReject,"DUPLICATE_OR_MISSING_NUNIT_CASE"):
            m.evaluate(z)

    def test_09_missing_original_blob_is_rejected_by_source_pin(self):
        with self.assertRaisesRegex(m.EvidenceReject,"GIT_PINNED_EVIDENCE_MISSING"):
            m.git(REPO,"show","0"*40+":Docs/CXFinal/Evidence/PlayMode.xml")

    def test_10_source_only_manifest_can_be_materialized_without_overwriting(self):
        with tempfile.TemporaryDirectory(prefix="b01_40_native_receipt_only_") as tmp:
            path=Path(tmp)/"original_codex_evidence.json"
            with path.open("xb") as fh:
                fh.write((json.dumps(self.result,sort_keys=True,indent=2)+"\n").encode())
            self.assertEqual(json.loads(path.read_bytes())["original_game_sha"],m.ACTUAL_GAME_SHA)
            with self.assertRaises(FileExistsError):
                with path.open("xb") as fh:fh.write(b"unsafe")

if __name__=="__main__":
    print("P16_ORIGINAL_NUNIT_SOURCE_ONLY=TRUE INDEPENDENT_UNITY=NOT_RUN",flush=True)
    unittest.main(verbosity=2)
