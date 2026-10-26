#!/usr/bin/env python3
"""Structural/self tests with SYNTHETIC data, not Unity native execution."""
import io
import json
from pathlib import Path
import tempfile
import unittest
import zipfile
import xml.etree.ElementTree as ET

import prepare_build_native as p
import verify_build_native as v


def make_cases(edit=True, missing_receiver=False, fail=False):
    root=ET.Element("test-run",{"result":"Passed"})
    counts = ([("B01.BUILD001.Tests.Build001R14ContactAreaTests",14),
               ("BuildRC2SafetyTests",3 if not missing_receiver else 2),
               ("B01.BUILD001.Tests.Build001SocketTests",11),
               ("B01.BUILD001.Tests.Build001R13SafetyTests",7)] if edit else
              [("B01.BUILD001.Tests.Build001PhysicalHitTests",2),
               ("B01.BUILD001.Tests.Build001R13PhysicalTests",4)])
    for cls,num in counts:
        for i in range(num):
            if cls=="BuildRC2SafetyTests":
                name=(("NestedPatchMustSatisfyActualMinimumContactArea(" + str(i) + ")")
                      if i<2 else "AcceptedNestedPatchReportsItsTrueGeometricArea")
            else:
                name=("RC2_NestedAcceptedAreaEqualsActualPhysicalSquareMetres" if
                    cls.endswith("Build001R14ContactAreaTests") and i==0 else
                    f"Test{i}")
            ET.SubElement(root,"test-case",{"classname":cls,"fullname":cls+"."+name,
                "name":name,"result":"Passed"})
    cases=list(root.iter("test-case"))
    root.set("total",str(len(cases)));root.set("passed",str(len(cases)))
    if fail:
        cases[0].set("result","Failed");root.set("result","Failed")
        root.set("passed",str(len(cases)-1))
    return ET.tostring(root,encoding="unicode")


class AcceptanceProfileTests(unittest.TestCase):
    def test_01_original_frozen_pins_are_nonblank(self):
        self.assertEqual(len(p.SOURCE_SHA),40)
        self.assertEqual(len(p.SOURCE_ZIP_SHA256),64)
        self.assertEqual(len(p.RECEIVER_BLOB),40)

    def test_02_path_traversal_and_case(self):
        self.assertTrue(p.path_safe("Assets/B01Construction/BUILD001/Runtime/A.cs"))
        for bad in ("../Secrets","Assets/../Outside",
                    "Assets\\B01Construction\\x","C:/windows/test",
                    "./Assets/B01Construction.meta"):
            self.assertFalse(p.path_safe(bad),bad)

    def test_03_unknown_zip_digest_refused(self):
        with tempfile.TemporaryDirectory() as td:
            z=Path(td)/"fake.zip"
            with zipfile.ZipFile(z,"w") as writer:writer.writestr("unsafe.txt","x")
            with self.assertRaisesRegex(ValueError,"UNEXPECTED_BUILD_R14_ZIP_SHA256"):
                p.verify_archive(z)

    def test_04_no_disposable_marker_blocks(self):
        with tempfile.TemporaryDirectory() as td:
            folder=Path(td)
            (folder/"ProjectSettings").mkdir()
            (folder/"ProjectSettings/ProjectVersion.txt").write_text(
                "m_EditorVersion: 6000.3.14f1")
            with self.assertRaisesRegex(ValueError,"NOT_B01_60_DISPOSABLE_QA"):
                p.assert_disposable(folder,"a"*40)

    def test_05_changed_receiver_fixture_is_rejected(self):
        with tempfile.TemporaryDirectory() as td:
            fixture=Path(td)/"BuildRC2SafetyTests.cs"
            fixture.write_text("// tampered")
            self.assertNotEqual(p.gitblob(fixture.read_bytes()),p.RECEIVER_BLOB)

    def test_06_positive_synthetic_edit_xml_counts(self):
        expected={"B01.BUILD001.Tests.Build001R14ContactAreaTests":14,
                  "BuildRC2SafetyTests":3}
        self.assertEqual(v.parse_nunit(make_cases(),35,expected)["passed"],35)

    def test_07_missing_receiver_case_blocks(self):
        with self.assertRaisesRegex(ValueError,"WRONG_TEST_TOTAL"):
            v.parse_nunit(make_cases(missing_receiver=True),35,
                {"BuildRC2SafetyTests":3})

    def test_08_synthetic_failure_blocks(self):
        with self.assertRaisesRegex(ValueError,"NATIVE_TEST_FAILURE_OR_SKIP"):
            v.parse_nunit(make_cases(fail=True),35,{"BuildRC2SafetyTests":3})

    def test_09_synthetic_play_xml_counts(self):
        expected={"B01.BUILD001.Tests.Build001PhysicalHitTests":2,
                  "B01.BUILD001.Tests.Build001R13PhysicalTests":4}
        self.assertEqual(v.parse_nunit(make_cases(edit=False),6,expected)["passed"],6)

    def test_10_missing_original_evidence_remains_not_run(self):
        with tempfile.TemporaryDirectory() as td:
            r=v.check_evidence({"source_sha":p.SOURCE_SHA,"target_sha":"a"*40},td)
            self.assertEqual(r["status"],"NOT_RUN")
            self.assertEqual(len(r["problems"]),3)
            self.assertEqual(r["g2"],"HOLD")

    def test_11_partial_or_fake_native_receipt_never_passes(self):
        with tempfile.TemporaryDirectory() as td:
            root=Path(td)
            stage=root/"Compile";stage.mkdir()
            (stage/"process.json").write_text(json.dumps(
                {"stage":"Compile","source_sha":p.SOURCE_SHA,"target_sha":"a"*40,
                 "exit_code":0,"unity_version":"6000.3.14f1",
                 "attribution":"FAKE_NOT_B01_60","timeout":False}))
            (stage/"Editor.log").write_text("simulated")
            r=v.check_evidence({"source_sha":p.SOURCE_SHA,"target_sha":"a"*40},root)
            self.assertEqual(r["status"],"FAIL")
            self.assertEqual(r["g2"],"HOLD")

    def test_12_source_hard_requirement(self):
        r=v.check_evidence({"source_sha":"b"*40,"target_sha":"a"*40},"no_such_dir")
        self.assertEqual(r["status"],"NOT_RUN")
        self.assertIn("STALE_SOURCE_OR_TARGET",r["problems"])


if __name__=="__main__":
    unittest.main(verbosity=2)
