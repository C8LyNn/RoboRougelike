#!/usr/bin/env python3
"""Independent adversarial SOURCE-only CI for native-evidence verifier.

All positive fixtures here are *synthetic* and may never be claimed as
executed Unity G1. The verifier marks even valid syntax NOT_CERTIFIED.
"""
import hashlib
import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0,str(Path(__file__).resolve().parent))
import verify_native_evidence as v

class RealEvidenceStructureNegativeControls(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix="ci41-w20-fixture-")
        self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        self.m={
            "schema":"B01-CI41-UNITY-NATIVE-EVIDENCE-001/R1",
            "producer_pr": "647",
            "source_sha":v.R4["647"]["sha"],
            "source_sha_after":v.R4["647"]["sha"],
            "source_r3_parent":v.BASE,
            "frozen_game_sha":v.GAME,
            "frozen_save_sha":v.SAVE,
            "unity_version":"6000.3.14f1",
            "operator_invoked":True,
            "sessions":[]
        }
        for suite,mincount in v.MIN_CASES.items():
            samples=sorted(v.SENTINEL[suite])
            names=samples+[suite+".NativeCase"+str(i) for i in
                           range(mincount-len(samples))]
            xml=('<test-run result="Passed" total="{0}" passed="{0}" failed="0" skipped="0">'.format(
                len(names))+"".join(
                '<test-case name="{0}" fullname="CI41.{1}.{0}" result="Passed" executed="True"/>'.format(
                n,suite) for n in names)+"</test-run>").encode()
            self.m["sessions"].append({
              "suite":suite,"exit_code":0,
              "actual_test_platform":"EditMode" if suite=="EditMode" else "PlayMode",
              "command":["C:/Unity/Editor/Unity.exe","-batchmode","-runTests",
                         "-testResults",suite+".xml","-testPlatform",
                         "EditMode" if suite=="EditMode" else "PlayMode"],
              "started_at":"2026-10-30T16:00:00+01:00",
              "ended_at":"2026-10-30T16:01:00+01:00",
              "editor_log":self.write(suite+"-editor.txt",
                       b"Initialize engine version: 6000.3.14f1\nSIMULATED_FIXTURE"),
              "stdout":self.write(suite+"-stdout.txt",b"SIMULATED_TEST_NOT_ACTUAL_UNITY"),
              "nunit_xml":self.write(suite+".xml",xml)
            })
        self.save()
    def write(self,path,data):
        (self.root/path).write_bytes(data)
        return {"path":path,"sha256":hashlib.sha256(data).hexdigest()}
    def save(self):
        (self.root/"manifest.json").write_text(json.dumps(self.m))
    def fail(self,message):
        self.save()
        with self.assertRaisesRegex(v.Denied,message):
            v.verify(self.root)
    def test_01_structural_positive_is_never_native_attestation(self):
        out=v.verify(self.root)
        self.assertEqual(out["total_nunit_cases"],22)
        self.assertEqual(out["g1"],"NOT_CERTIFIED_REQUIRE_LOCAL_HUMAN_EDITOR_EVIDENCE_REVIEW")
        self.assertEqual(out["result"],"STRUCTURALLY_CONSISTENT_NOT_PROVEN_NATIVE_EXECUTION")
        self.assertEqual(v.OWNER_SIGNED_R4,"647")
        print("W20_SYNTHETIC_ONLY_STRUCTURAL_POSITIVE_NOT_G1_PASS")
    def test_02_wrong_r4_or_mixed_647_652(self):
        self.m["source_sha"]=v.R4["652"]["sha"]
        self.fail("UNTRUSTED_OR_MIXED_R4")
    def test_03_alt_candidate_not_allowed(self):
        self.m["producer_pr"]="604"
        self.fail("UNTRUSTED_OR_MIXED_R4")
    def test_04_mutated_source_on_unity_import(self):
        self.m["source_sha_after"]=v.BASE
        self.fail("SOURCE_MUTATED")
    def test_05_frozen_game_sha_drift(self):
        self.m["frozen_game_sha"]="f"*40
        self.fail("FROZEN_GIT")
    def test_06_frozen_save_sha_drift(self):
        self.m["frozen_save_sha"]="e"*40
        self.fail("FROZEN_GIT")
    def test_07_wrong_unity_version(self):
        self.m["unity_version"]="6000.3.13f1"
        self.fail("WRONG_UNITY")
    def test_08_no_opt_in(self):
        self.m["operator_invoked"]=False
        self.fail("NOT_USER_INITIATED")
    def test_09_missing_physx_suite(self):
        self.m["sessions"].pop(1)
        self.fail("MISSING_OR_ADDITIONAL")
    def test_10_duplicate_suite(self):
        self.m["sessions"][1]["suite"]="EditMode"
        self.fail("DUPLICATE_OR_UNKNOWN")
    def test_11_unity_process_failure(self):
        self.m["sessions"][1]["exit_code"]=1
        self.fail("UNITY_PROCESS_NONZERO")
    def test_12_type_confusion_boolean_return(self):
        self.m["sessions"][0]["exit_code"]=False
        self.fail("UNITY_PROCESS_NONZERO")
    def test_13_missing_run_tests(self):
        self.m["sessions"][0]["command"].remove("-runTests")
        self.fail("NOT_NATIVE_UNITY_TEST_COMMAND")
    def test_14_non_editor_binary(self):
        self.m["sessions"][0]["command"][0]="fake-test-runner.exe"
        self.fail("NO_WINDOWS_UNITY_EDITOR")
    def test_15_invalid_timestamp_duration(self):
        self.m["sessions"][0]["ended_at"]="2026-10-30T15:30:00+01:00"
        self.fail("INVALID_NATIVE_RUN_DURATION")
    def test_16_utc_offset_missing(self):
        self.m["sessions"][0]["started_at"]="2026-10-30T16:00:00"
        self.fail("TIME_NOT_OFFSET_AWARE")
    def test_17_editor_log_hash_tamper(self):
        (self.root/"EditMode-editor.txt").write_bytes(b"REAL_UNITY_IMPERSONATION")
        self.fail("EVIDENCE_SHA256_MISMATCH")
    def test_18_log_activation_failure(self):
        data=b"Initialize engine version: 6000.3.14f1\nLicense activation failed"
        self.m["sessions"][0]["editor_log"]=self.write("EditMode-editor.txt",data)
        self.fail("UNITY_LICENSE_ERROR")
    def test_19_broken_xml(self):
        self.m["sessions"][0]["nunit_xml"]=self.write("EditMode.xml",b"<bad")
        self.fail("NUNIT_XML_MALFORMED")
    def test_20_xml_failed_test(self):
        d=self.root/"EditMode.xml"
        raw=d.read_bytes().replace(b'result="Passed"',b'result="Failed"',1)
        self.m["sessions"][0]["nunit_xml"]=self.write("EditMode.xml",raw)
        self.fail("NUNIT_NOT_PASSED")
    def test_21_xml_duplicate_case(self):
        d=self.root/"EditMode.xml"
        raw=d.read_bytes().replace(b"RealOriginalW06LedgerClearFailsClosedOnMissingIdBeforeCallbacks",
                                   b"ExactOwnerRuntimeAndOriginalW06AreLinkableByUnity")
        self.m["sessions"][0]["nunit_xml"]=self.write("EditMode.xml",raw)
        self.fail("NUNIT_SUMMARY_CASE_COUNT_OR_DUPLICATE")
    def test_22_xml_missing_sentinel(self):
        d=self.root/"EditMode.xml"
        raw=d.read_bytes().replace(b"RealOriginalW06LedgerClearFailsClosedOnMissingIdBeforeCallbacks",
                                   b"AnotherUnrelatedFictionalTest")
        self.m["sessions"][0]["nunit_xml"]=self.write("EditMode.xml",raw)
        self.fail("NUNIT_ORIGINAL_SUITE_SENTINELS_MISSING")
    def test_23_relative_escape(self):
        self.m["sessions"][0]["editor_log"]["path"]="../outside.log"
        self.fail("PATH_ESCAPE_OR_DOT_COMPONENT")
    def test_24_absolute_path_refused(self):
        self.m["sessions"][0]["editor_log"]["path"]="/tmp/private.log"
        self.fail("PATH_ESCAPE_OR_DOT_COMPONENT")
    def test_25_unity_test_platform_wrong(self):
        self.m["sessions"][2]["actual_test_platform"]="EditMode"
        self.fail("WRONG_NATIVE_TEST_PLATFORM")
    def test_26_missing_editor_log(self):
        self.m["sessions"][2]["editor_log"]["path"]="missing-log.txt"
        self.fail("MISSING_OR_EMPTY_EVIDENCE_FILE")
    def test_27_wrong_xml_total(self):
        d=self.root/"PhysX.xml"
        raw=d.read_bytes().replace(b'total="14"',b'total="15"')
        self.m["sessions"][1]["nunit_xml"]=self.write("PhysX.xml",raw)
        self.fail("NUNIT_NOT_ALL_CASES_EXECUTED_AND_PASSED")
    def test_28_xml_document_entity(self):
        d=self.root/"EditMode.xml"
        raw=b'<!DOCTYPE a [<!ENTITY x "z">]>'+d.read_bytes()
        self.m["sessions"][0]["nunit_xml"]=self.write("EditMode.xml",raw)
        self.fail("XML_ENTITY_OR_DOCTYPE")
    def test_29_runner_stdout_activation_failed(self):
        self.m["sessions"][0]["stdout"]=self.write("EditMode-stdout.txt",
                 b"Error: unable to activate")
        self.fail("UNITY_ACTIVATION_FAILURE_STDOUT")
    def test_30_explicit_false_result(self):
        self.m["sessions"][1]["nunit_xml"]=self.write("PhysX.xml",
                 (self.root/"PhysX.xml").read_bytes().replace(
                    b'result="Passed"',b'result="Failed"'))
        self.fail("NUNIT_NOT_PASSED")
    def test_31_release_enforce_always_hold_without_trusted_owner_and_attestation(self):
        self.save()
        saved=sys.argv
        sys.argv=["verify_native_evidence.py","--evidence",str(self.root),
                  "--output",str(self.root/"release.json"),"--enforce"]
        try:
            self.assertEqual(v.main(),3)
        finally:
            sys.argv=saved
        self.assertEqual(json.loads((self.root/"release.json").read_text())["release_gate"],
                         "HOLD_HOST_P0_AND_NATIVE_UNATTESTED")
    def test_32_invalid_receipt_has_nonzero_process_status_not_a_pass(self):
        self.m["producer_pr"]="604"
        self.save()
        saved=sys.argv
        sys.argv=["verify_native_evidence.py","--evidence",str(self.root),
                  "--output",str(self.root/"rejected.json")]
        try:
            self.assertEqual(v.main(),2)
        finally:
            sys.argv=saved
        self.assertEqual(json.loads((self.root/"rejected.json").read_text())["result"],
                         "REJECTED_EVIDENCE")
    def test_33_syntactically_complete_losing_r4_652_still_rejected(self):
        self.m["producer_pr"]="652"
        self.m["source_sha"]=v.R4["652"]["sha"]
        self.m["source_sha_after"]=v.R4["652"]["sha"]
        self.fail("UNTRUSTED_OR_MIXED_R4_SOURCE_SHA")
if __name__=="__main__":unittest.main(verbosity=2)
