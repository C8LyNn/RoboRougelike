#!/usr/bin/env python3
"""B01-40 independent PASS14 receiver. Executes EXACT B01-23 owner Python source.
Source/parser receipt QA only: synthetic XML/logs NEVER constitute native Unity G1.
"""
import hashlib
import importlib.util
import json
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

SOURCE_SHA = "f3a40810a178c11696360c32921872bfacecf21b"
PATH = "Tools/B01/PARTSIZE001/W26Receipt/check_receipt.py"
BLOB = "b4ca19bd1b9142a364dbed2fb6bb2dd30f2b53ac"
OPERATOR_BLOB = "da575df50d51d5f76776d96e3b5251103b85d923"
OPERATOR_PATH = "Tools/B01/PARTSIZE001/W26Receipt/Run-UnityNative.ps1"

def git(*args):
    return subprocess.check_output(["git", *args], stderr=subprocess.PIPE)

def original_module():
    for path, expected in ((PATH,BLOB),(OPERATOR_PATH,OPERATOR_BLOB)):
        sha=git("rev-parse",SOURCE_SHA+":"+path).decode().strip()
        assert sha==expected, "PINNED_ORIGINAL_GIT_BLOB_CHANGED "+path+" "+sha
    data=git("show",SOURCE_SHA+":"+PATH)
    sha=hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()
    assert sha==BLOB, "ORIGINAL_PARSER_BYTES_NOT_GIT_OBJECT"
    with tempfile.TemporaryDirectory(prefix="b01_40_w66_actual_original_") as tmp:
        f=Path(tmp)/"check_receipt.py"
        f.write_bytes(data)
        spec=importlib.util.spec_from_file_location("b01_23_owner_w66_receipt",f)
        module=importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
    return module

ORIGINAL=original_module()

def xml(stage):
    cls=ORIGINAL.CLASSES[stage]
    root=ET.Element("test-run",result="Passed",
                    total=str(ORIGINAL.EXPECTED_COUNTS[stage]),
                    passed=str(ORIGINAL.EXPECTED_COUNTS[stage]))
    for method,count in ORIGINAL.EXPECTED[stage].items():
        for i in range(count):
            name=method+(f"({i})" if count>1 else "")
            ET.SubElement(root,"test-case",classname=cls,name=name,
                          fullname=cls+"."+name,result="Passed")
    return root

def raw(root):
    return ET.tostring(root,encoding="utf-8")

def receipt(root):
    meta={"source_sha":ORIGINAL.SOURCE_SHA,"unity_version":ORIGINAL.UNITY,
          "status":"MOCK_NOT_UNITY"}
    for stage in ("EditMode","PlayMode"):
        data=raw(xml(stage))
        log=("Initialize engine version: "+ORIGINAL.UNITY+
             "\nSYNTHETIC TEST ONLY: NOT ACTUAL UNITY\n"*3).encode()
        (root/(stage+".xml")).write_bytes(data)
        (root/(stage+".log")).write_bytes(log)
        meta[stage]={"assembly":ORIGINAL.ASSEMBLIES[stage],"exit_code":0,
                     "xml_sha256":hashlib.sha256(data).hexdigest(),
                     "log_sha256":hashlib.sha256(log).hexdigest()}
    (root/"receipt.json").write_text(json.dumps(meta),encoding="utf-8")
    return meta

def save(root,meta):
    (root/"receipt.json").write_text(json.dumps(meta),encoding="utf-8")

class IndependentActualOwnerParser(unittest.TestCase):
    def test_01_clean_synthetic_structure_is_only_independent_review_pending(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);receipt(root)
            result=ORIGINAL.inspect_receipt(root)
            self.assertEqual(result["status"],"STRUCTURAL_EVIDENCE_REVIEW_REQUIRED")
            self.assertEqual(result["native_g1"],"PENDING_INDEPENDENT_B01_60")
            self.assertEqual(result["G2"],"HOLD")
            self.assertEqual(result["stages"]["EditMode"]["cases"],17)
            self.assertEqual(result["stages"]["PlayMode"]["cases"],11)

    def test_02_NEG01_foreign_fullname_refused_both_stages(self):
        for stage in ("EditMode","PlayMode"):
            with self.subTest(stage=stage):
                tree=xml(stage)
                node=next(tree.iter("test-case"))
                node.set("fullname",ORIGINAL.CLASSES[stage]+".OTHER_EXPECTED_NAME")
                with self.assertRaisesRegex(ORIGINAL.EvidenceError,"FOREIGN_OR_MALFORMED_NUNIT_CASE"):
                    ORIGINAL.check_xml(raw(tree),stage)

    def test_03_NEG02_foreign_assembly_refused_both_stages(self):
        for stage in ("EditMode","PlayMode"):
            with self.subTest(stage=stage),tempfile.TemporaryDirectory() as tmp:
                root=Path(tmp);r=receipt(root)
                r[stage]["assembly"]="B01.UNTRUSTED.OtherTests"
                save(root,r)
                with self.assertRaisesRegex(ORIGINAL.EvidenceError,"WRONG_NATIVE_TEST_ASSEMBLY_"+stage):
                    ORIGINAL.inspect_receipt(root)

    def test_04_NEG03_boolean_false_exit_refused_both_stages(self):
        for stage in ("EditMode","PlayMode"):
            with self.subTest(stage=stage),tempfile.TemporaryDirectory() as tmp:
                root=Path(tmp);r=receipt(root)
                r[stage]["exit_code"]=False
                save(root,r)
                self.assertIs(json.loads((root/"receipt.json").read_text())[stage]["exit_code"],False)
                with self.assertRaisesRegex(ORIGINAL.EvidenceError,"INVALID_OR_NONZERO_UNITY_EXIT_"+stage):
                    ORIGINAL.inspect_receipt(root)

    def test_05_required_test_absent_is_rejected(self):
        for stage in ("EditMode","PlayMode"):
            with self.subTest(stage=stage):
                tree=xml(stage);tree.remove(next(tree.iter("test-case")))
                with self.assertRaisesRegex(ORIGINAL.EvidenceError,"MISSING_OR_EXTRA_NUNIT_CASES"):
                    ORIGINAL.check_xml(raw(tree),stage)

    def test_06_skipped_positive_count_cannot_pass(self):
        for stage in ("EditMode","PlayMode"):
            with self.subTest(stage=stage):
                tree=xml(stage)
                next(tree.iter("test-case")).set("result","Skipped")
                with self.assertRaisesRegex(ORIGINAL.EvidenceError,"FAILED_SKIPPED_OR_INCONCLUSIVE"):
                    ORIGINAL.check_xml(raw(tree),stage)

    def test_07_duplicate_fullname_is_denied(self):
        for stage in ("EditMode","PlayMode"):
            with self.subTest(stage=stage):
                tree=xml(stage)
                cases=list(tree.iter("test-case"))
                cases[-1].set("fullname",cases[0].get("fullname"))
                with self.assertRaisesRegex(ORIGINAL.EvidenceError,"DUPLICATE_NUNIT_FULLNAME"):
                    ORIGINAL.check_xml(raw(tree),stage)

    def test_08_modified_log_bytes_cannot_pass(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);receipt(root)
            with (root/"PlayMode.log").open("ab") as f: f.write(b"UNSIGNED")
            with self.assertRaisesRegex(ORIGINAL.EvidenceError,"NATIVE_RECEIPT_TAMPER"):
                ORIGINAL.inspect_receipt(root)

    def test_09_missing_xml_does_not_claim_pass(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);receipt(root)
            (root/"EditMode.xml").unlink()
            self.assertEqual(ORIGINAL.inspect_receipt(root)["status"],"NOT_RUN")

    def test_10_unexpected_integer_exit_denied(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);r=receipt(root)
            r["EditMode"]["exit_code"]=2;save(root,r)
            with self.assertRaisesRegex(ORIGINAL.EvidenceError,"INVALID_OR_NONZERO_UNITY_EXIT"):
                ORIGINAL.inspect_receipt(root)

if __name__=="__main__":
    print("OWNER_QA_SHA="+SOURCE_SHA+" ORIGINAL_PARSER_GIT_BLOB="+BLOB+
          " NATIVE_UNITY=NOT_RUN G1=NOT_RUN G2=HOLD",flush=True)
    unittest.main(verbosity=2)
