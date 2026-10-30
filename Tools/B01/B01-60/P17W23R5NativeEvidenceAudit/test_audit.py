#!/usr/bin/env python3
"""Only synthetic receipts; full original Unity tests must be run by operator."""
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

import audit as a

class R5EvidenceTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(prefix="b0160-w23-synthetic-")
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.original_raw = {p: ("synthetic:" + p).encode() for p in a.ORIGINAL_GIT_BLOBS}
        pins = {p: a.git_blob_sha1(blob) for p, blob in self.original_raw.items()}
        self.patched = patch.dict(a.ORIGINAL_GIT_BLOBS, pins)
        self.patched.start()
        self.addCleanup(self.patched.stop)
        self.receipt = {
            "package": "B01-60-P17W22-R5-NATIVE-PARTIAL",
            "selected_host_R5_SHA": a.R5_SHA,
            "original_510_SHA": a.GAME_SHA, "original_556_SHA": a.SAVE_SHA,
            "R5_FULL_NATIVE_G1": "HOLD_ORIGINAL_COLDRESTART_GEN4_EXPECTS_GEN5",
            "source_methods": [a.PARTIAL[0],a.PARTIAL[1],a.HELD,*a.PARTIAL[2:]],
            "native_partial_scope": list(a.PARTIAL),
            "merged": False,"uploaded":False,"source_G0":"B01-40_699_SOURCE_ONLY",
            "native_G1":"NOT_RUN","source_preflight":"PASS_6_STAGE_ROLLBACK_NO_UNITY"
        }
        self.members = {}
        self.flush()

    def flush(self):
        self.members["P22-receipt.json"] = json.dumps(self.receipt).encode()
        for rel, blob in self.members.items():
            p = self.root / rel
            p.parent.mkdir(exist_ok=True, parents=True)
            p.write_bytes(blob)
        inventory = {rel: a.sha256(blob) for rel, blob in self.members.items()}
        obj = {"unsigned": True,"receipt":self.receipt,"evidence_sha256":inventory}
        (self.root/"P22-sha256-manifest.json").write_text(json.dumps(obj))

    def native(self):
        self.receipt["native_G1"] = "PARTIAL_R5_SIX_OF_SEVEN_ONLY"
        self.receipt.pop("source_preflight")
        for i, method in enumerate(a.PARTIAL):
            prefix=f"P{i}/"
            product=f"B0160W22Case{i}123456abcdef"
            self.members[prefix+a.SETTINGS_PATH] = (
                "companyName: B0160W22123456abcdef\nproductName: "+product+"\n").encode()
            self.members[prefix+a.PROBE_PATH] = (
                "B0160W22ScopeProbe OnlyPrivateSavePath "+product).encode()
            for p, blob in self.original_raw.items():
                self.members[prefix+p] = blob
            for tag, test in ((f"guard{i}",a.PROBE),
                               (f"r5case{i}",a.SUITE+method)):
                x=ET.Element("test-run", {"result":"Passed","total":"1",
                     "passed":"1","failed":"0","skipped":"0","inconclusive":"0"})
                ET.SubElement(x,"test-case",{"fullname":test,"result":"Passed"})
                self.members[tag+".xml"]=ET.tostring(x)
                self.members[tag+"-Editor.log"]=b"Unity Editor 6000.3.14f1\n"
                self.members[tag+"-process.log"]=b"exit_code=0\noriginal stdout\n"
        self.flush()

    def alter(self, name, f):
        self.members[name] = f(self.members[name])
        self.flush()

    def denied(self):
        with self.assertRaises(a.Hold):
            a.audit(self.root)

    def test_01_source_only_valid_not_native(self):
        self.assertEqual(a.audit(self.root),"SOURCE_PREFLIGHT_ONLY_NATIVE_NOT_RUN")
    def test_02_missing_source_hash(self):
        obj=json.loads((self.root/"P22-sha256-manifest.json").read_text())
        obj["evidence_sha256"]={}
        (self.root/"P22-sha256-manifest.json").write_text(json.dumps(obj))
        self.denied()
    def test_03_mutated_receipt_without_rehash(self):
        (self.root/"P22-receipt.json").write_bytes(b"changed")
        self.denied()
    def test_04_false_full_r5_g1_never_allowed(self):
        self.receipt["native_G1"]="PASS_ALL_7"
        self.flush();self.denied()
    def test_05_preflight_with_native_logs_rejected(self):
        self.members["guard0.xml"]=b"forged"
        self.flush();self.denied()
    def test_06_wrong_original_r5_sha(self):
        self.receipt["selected_host_R5_SHA"]="0"*40
        self.flush();self.denied()
    def test_07_cold_restart_hold_removed(self):
        self.receipt["R5_FULL_NATIVE_G1"]="PASS"
        self.flush();self.denied()
    def test_08_selected_test_method_substituted(self):
        self.receipt["native_partial_scope"][1]="ForeignTest"
        self.flush();self.denied()
    def test_09_synthetic_native_only_structural(self):
        self.native()
        self.assertEqual(a.audit(self.root),
            "STRUCTURALLY_CONSISTENT_6_R5_CASES_UNATTESTED_FULL_G1_HOLD")
    def test_10_native_missing_exact_nunit(self):
        self.native()
        (self.root/"r5case4.xml").unlink()
        self.denied()
    def test_11_native_wrong_test_name_with_consistent_sha(self):
        self.native()
        self.alter("r5case2.xml", lambda b:b.replace(
            a.PARTIAL[2].encode(), b"UnexpectedOtherTest"))
        self.denied()
    def test_12_native_skipped_case_denied(self):
        self.native()
        self.alter("guard0.xml", lambda b:b.replace(b'Passed',b'Skipped'))
        self.denied()
    def test_13_native_bad_nunit_totals(self):
        self.native()
        self.alter("r5case5.xml",lambda b:b.replace(b'total="1"',b'total="2"'))
        self.denied()
    def test_14_native_process_nonzero_exit(self):
        self.native()
        self.alter("r5case4-process.log",
                   lambda b:b.replace(b"exit_code=0",b"exit_code=1"))
        self.denied()
    def test_15_native_editor_wrong_version(self):
        self.native()
        self.alter("guard2-Editor.log",
                   lambda b:b.replace(b"6000.3.14f1",b"6000.4.0f1"))
        self.denied()
    def test_16_native_wrong_private_player_settings(self):
        self.native()
        self.alter("P1/"+a.SETTINGS_PATH,lambda b:b.replace(
            b"B0160W22Case1", b"RoboRougelike1"))
        self.denied()
    def test_17_native_selected_source_drift_even_if_rehashed(self):
        self.native()
        self.alter("P4/"+a.FIXTURE_PATH,lambda b:b+b"\nunsafe edit")
        self.denied()
    def test_18_native_probe_missing_expected_product(self):
        self.native()
        self.alter("P0/"+a.PROBE_PATH,lambda b:b.replace(
            b"B0160W22Case0",b"B0160W22Case9"))
        self.denied()
    def test_19_native_unexpected_extra_test_log(self):
        self.native()
        self.members["r5case6.xml"]=self.members["r5case0.xml"]
        self.flush()
        self.denied()
    def test_20_native_one_required_log_missing_manifest_entry(self):
        self.native()
        manifest=json.loads((self.root/"P22-sha256-manifest.json").read_text())
        manifest["evidence_sha256"].pop("r5case0-Editor.log")
        (self.root/"P22-sha256-manifest.json").write_text(json.dumps(manifest))
        self.denied()

if __name__=="__main__":
    unittest.main(verbosity=2)
