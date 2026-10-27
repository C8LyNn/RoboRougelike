#!/usr/bin/env python3
"""SYNTHETIC NEGATIVE TESTS OF THE RECEIPT PARSER; NOT Unity executions."""
import hashlib
import json
from pathlib import Path
from tempfile import TemporaryDirectory
import unittest
import xml.etree.ElementTree as ET
import check_receipt as v

def mock_xml(platform):
    total=v.EXPECTED_COUNTS[platform]
    root=ET.Element("test-run",{"result":"Passed","total":str(total),"passed":str(total)})
    cls=v.CLASSES[platform]
    for method,n in v.EXPECTED[platform].items():
        for i in range(n):
            name=method+("("+str(i)+")" if n>1 else "")
            ET.SubElement(root,"test-case",{"classname":cls,"name":name,
                "fullname":cls+"."+name,"result":"Passed"})
    return root

def raw_xml(root):
    return ET.tostring(root,encoding="utf8")

def stage(folder,platform,xml=None,log=None):
    data=raw_xml(mock_xml(platform)) if xml is None else xml
    log=("Initialize engine version: "+v.UNITY+"\n"+("Native runner log segment\n"*3)).encode() if log is None else log
    (folder/(platform+".xml")).write_bytes(data)
    (folder/(platform+".log")).write_bytes(log)
    return {"exit_code":0,
            "xml_sha256":hashlib.sha256(data).hexdigest(),
            "log_sha256":hashlib.sha256(log).hexdigest()}

def receipt(folder):
    obj={"source_sha":v.SOURCE_SHA,"unity_version":v.UNITY,
         "EditMode":stage(folder,"EditMode"),"PlayMode":stage(folder,"PlayMode")}
    (folder/"receipt.json").write_text(json.dumps(obj))
    return obj

class NativeEvidenceParserUnitTests(unittest.TestCase):
    def test_01_missing_receipt_stays_not_run(self):
        with TemporaryDirectory() as temp:
            self.assertEqual(v.inspect_receipt(Path(temp))["status"],"NOT_RUN")

    def test_02_synthetic_complete_structure_requires_independent_review(self):
        with TemporaryDirectory() as temp:
            f=Path(temp); receipt(f)
            verdict=v.inspect_receipt(f)
            self.assertEqual(verdict["status"],"STRUCTURAL_EVIDENCE_REVIEW_REQUIRED")
            self.assertEqual(verdict["stages"]["EditMode"]["cases"],17)
            self.assertEqual(verdict["stages"]["PlayMode"]["cases"],11)
            self.assertEqual(verdict["G2"],"HOLD")
            # This is parser-only synthetic test, NOT genuine G1.

    def test_03_source_sha_mismatch_denied(self):
        with TemporaryDirectory() as temp:
            f=Path(temp); o=receipt(f);o["source_sha"]="0"*40
            (f/"receipt.json").write_text(json.dumps(o))
            with self.assertRaisesRegex(v.EvidenceError,"WRONG_SOURCE_OR_UNITY_VERSION"):
                v.inspect_receipt(f)

    def test_04_fake_green_with_one_missing_real_case_denied(self):
        root=mock_xml("PlayMode")
        root.remove(list(root.iter("test-case"))[-1])
        with self.assertRaisesRegex(v.EvidenceError,"MISSING_OR_EXTRA_NUNIT_CASES"):
            v.check_xml(raw_xml(root),"PlayMode")

    def test_05_duplicate_fullname_even_if_11_green_denied(self):
        root=mock_xml("PlayMode")
        cases=list(root.iter("test-case"))
        cases[-1].set("fullname",cases[0].get("fullname"))
        with self.assertRaisesRegex(v.EvidenceError,"DUPLICATE_NUNIT_FULLNAME"):
            v.check_xml(raw_xml(root),"PlayMode")

    def test_06_foreign_test_replacing_a_green_test_denied(self):
        root=mock_xml("PlayMode")
        cases=list(root.iter("test-case"))
        cases[-1].set("name","WrongTestThatPassed")
        cases[-1].set("fullname",v.CLASSES["PlayMode"]+".WrongTestThatPassed")
        with self.assertRaisesRegex(v.EvidenceError,"FOREIGN_OR_SUBSTITUTED_TEST_METHOD"):
            v.check_xml(raw_xml(root),"PlayMode")

    def test_07_xml_tamper_after_receipt_denied(self):
        with TemporaryDirectory() as temp:
            f=Path(temp);receipt(f)
            with (f/"PlayMode.xml").open("ab") as out:out.write(b"tampered")
            with self.assertRaisesRegex(v.EvidenceError,"NATIVE_RECEIPT_TAMPER"):
                v.inspect_receipt(f)

    def test_08_nonzero_native_exit_is_not_pass(self):
        with TemporaryDirectory() as temp:
            f=Path(temp);o=receipt(f);o["PlayMode"]["exit_code"]=3
            (f/"receipt.json").write_text(json.dumps(o))
            with self.assertRaisesRegex(v.EvidenceError,"NONZERO_UNITY_EXIT"):
                v.inspect_receipt(f)

    def test_09_skipped_or_inconclusive_denied(self):
        root=mock_xml("EditMode")
        list(root.iter("test-case"))[0].set("result","Skipped")
        with self.assertRaisesRegex(v.EvidenceError,"FAILED_SKIPPED_OR_INCONCLUSIVE"):
            v.check_xml(raw_xml(root),"EditMode")

    def test_10_receiver_requires_unmodified_original_fixture_inventory(self):
        self.assertEqual(sum(v.EXPECTED["EditMode"].values()),17)
        self.assertEqual(sum(v.EXPECTED["PlayMode"].values()),11)
        self.assertEqual(len(v.UNMEASURED),11)
        for required in ("W17_LyingCompleteExternalInventoryCannotHideOverlappingBox",
                         "W17_IrrelevantExternalListCannotMaskOmittedPenetrator",
                         "W17_ExposedGridFaceCannotAuthorizePhysicalMountSocket"):
            self.assertIn(required,v.EXPECTED["PlayMode"])

    def test_11_missing_xml_marks_not_run(self):
        with TemporaryDirectory() as temp:
            f=Path(temp);receipt(f);(f/"EditMode.xml").unlink()
            self.assertEqual(v.inspect_receipt(f)["status"],"NOT_RUN")

    def test_12_mismatched_total_green_counts_denied(self):
        root=mock_xml("EditMode")
        root.set("total","18")
        with self.assertRaisesRegex(v.EvidenceError,"NUNIT_TOTAL_MISMATCH"):
            v.check_xml(raw_xml(root),"EditMode")

if __name__=="__main__":unittest.main(verbosity=2)
