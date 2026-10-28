#!/usr/bin/env python3
"""B01-40 W58 independent witness: original B01-23 parser, new synthetic negatives.
GREEN here confirms the 3 named BYPASSES REPRODUCED in unchanged upstream script.
It does not claim parser security PASS, licensed Unity runs, nor native G1.
"""
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET

ORIGINAL_QA_SHA = "75b9c7902344eab4a012951aa9f578195b6bb943"
ORIGINAL_PARSER_PATH = "Tools/B01/PARTSIZE001/W26Receipt/check_receipt.py"
ORIGINAL_PARSER_BLOB = "29f89a72dc73936a6f18f89554f2a70baccc1cfb"

def git(*args):
    return subprocess.check_output(["git", *args], stderr=subprocess.PIPE)

def load_original():
    actual=git("rev-parse", f"{ORIGINAL_QA_SHA}:{ORIGINAL_PARSER_PATH}").decode().strip()
    if actual != ORIGINAL_PARSER_BLOB:
        raise RuntimeError(f"ORIGINAL_PARSER_BLOB_CHANGED:{actual}")
    data=git("show", f"{ORIGINAL_QA_SHA}:{ORIGINAL_PARSER_PATH}")
    if hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest() != ORIGINAL_PARSER_BLOB:
        raise RuntimeError("ORIGINAL_GIT_BLOB_CONTENT_HASH_FAILED")
    # Deliberately load EXACT raw Git file without modifying or copying its methods.
    with tempfile.TemporaryDirectory(prefix="b01_40_w58_orig_parser_") as tmp:
        file=Path(tmp)/"check_receipt.py"
        file.write_bytes(data)
        spec=importlib.util.spec_from_file_location("b01_23_original_receipt", file)
        module=importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
    return module

ORIG=load_original()

def make_xml(platform):
    cls=ORIG.CLASSES[platform]
    root=ET.Element("test-run", result="Passed",
                    total=str(ORIG.EXPECTED_COUNTS[platform]),
                    passed=str(ORIG.EXPECTED_COUNTS[platform]))
    for method,n in ORIG.EXPECTED[platform].items():
        for i in range(n):
            name = method+(f"({i})" if n>1 else "")
            ET.SubElement(root,"test-case",{"classname":cls,"name":name,
                          "fullname":cls+"."+name,"result":"Passed"})
    return root

def xml_bytes(root):
    return ET.tostring(root,encoding="utf-8",xml_declaration=True)

def make_stage(folder, name, xml=None):
    xml=xml_bytes(make_xml(name)) if xml is None else xml
    log=("Initialize engine version: "+ORIG.UNITY+"\n"+"Synthetic mock log, NOT Unity\n"*4).encode()
    (folder/(name+".xml")).write_bytes(xml)
    (folder/(name+".log")).write_bytes(log)
    return {"assembly":"B01.PARTSIZE001."+name+"Tests",
            "exit_code":0,"xml_sha256":hashlib.sha256(xml).hexdigest(),
            "log_sha256":hashlib.sha256(log).hexdigest()}

def make_receipt(folder):
    o={"source_sha":ORIG.SOURCE_SHA,"unity_version":ORIG.UNITY,
       "status":"RAW_NATIVE_FILES_ONLY_NOT_G1",
       "EditMode":make_stage(folder,"EditMode"),"PlayMode":make_stage(folder,"PlayMode")}
    save(folder,o)
    return o

def save(folder, receipt):
    (folder/"receipt.json").write_text(json.dumps(receipt),encoding="utf-8")

class OriginalParserAdversarialWitness(unittest.TestCase):
    def test_01_unmodified_synthetic_baseline_is_structural_only(self):
        with tempfile.TemporaryDirectory(prefix="b01_40_w58_fixture_") as t:
            f=Path(t);make_receipt(f)
            val=ORIG.inspect_receipt(f)
            self.assertEqual(val["status"],"STRUCTURAL_EVIDENCE_REVIEW_REQUIRED")
            self.assertEqual(val["native_g1"],"PENDING_INDEPENDENT_B01_60")
            print("CONTROL_01_SYNTHETIC_STRUCTURAL_ONLY_PASS",flush=True)

    def test_02_new_case_fullname_does_not_match_name_still_accepted(self):
        root=make_xml("EditMode")
        c=next(root.iter("test-case"))
        original_name=c.get("name")
        c.set("fullname",ORIG.CLASSES["EditMode"]+".FAKE_OTHER_EXECUTED_METHOD")
        self.assertNotEqual(c.get("fullname"), c.get("classname")+"."+original_name)
        accepted=ORIG.check_xml(xml_bytes(root),"EditMode")
        self.assertEqual(accepted["cases"],17)
        print("BYPASS_01_REPRODUCED_original_check_xml_accepts_mismatched_fullname",flush=True)

    def test_03_new_case_receipt_wrong_assembly_still_accepted(self):
        with tempfile.TemporaryDirectory(prefix="b01_40_w58_fixture_") as t:
            f=Path(t);o=make_receipt(f)
            o["EditMode"]["assembly"]="B01.EVIL.FOREIGN.EditModeTests"
            save(f,o)
            val=ORIG.inspect_receipt(f)
            self.assertEqual(val["status"],"STRUCTURAL_EVIDENCE_REVIEW_REQUIRED")
            print("BYPASS_02_REPRODUCED_original_receipt_accepts_foreign_assembly",flush=True)

    def test_04_new_case_boolean_false_exit_code_is_treated_as_zero(self):
        with tempfile.TemporaryDirectory(prefix="b01_40_w58_fixture_") as t:
            f=Path(t);o=make_receipt(f)
            o["PlayMode"]["exit_code"]=False
            save(f,o)
            self.assertIs( json.loads((f/"receipt.json").read_text())["PlayMode"]["exit_code"],False)
            val=ORIG.inspect_receipt(f)
            self.assertEqual(val["status"],"STRUCTURAL_EVIDENCE_REVIEW_REQUIRED")
            print("BYPASS_03_REPRODUCED_original_receipt_accepts_boolean_exit_code",flush=True)

    def test_05_missing_actual_nunit_case_still_denied(self):
        root=make_xml("PlayMode")
        root.remove(next(root.iter("test-case")))
        with self.assertRaisesRegex(ORIG.EvidenceError,"MISSING_OR_EXTRA_NUNIT_CASES"):
            ORIG.check_xml(xml_bytes(root),"PlayMode")
        print("CONTROL_05_MISSING_CASE_DENIED",flush=True)

    def test_06_tampered_bytes_still_denied(self):
        with tempfile.TemporaryDirectory(prefix="b01_40_w58_fixture_") as t:
            f=Path(t);make_receipt(f)
            with (f/"EditMode.log").open("ab") as log:
                log.write(b"NOT_IN_RECEIPT")
            with self.assertRaisesRegex(ORIG.EvidenceError,"NATIVE_RECEIPT_TAMPER"):
                ORIG.inspect_receipt(f)
        print("CONTROL_06_HASH_TAMPER_DENIED",flush=True)

if __name__=="__main__":
    unittest.main(verbosity=2)
