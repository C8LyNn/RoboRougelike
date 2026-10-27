"""Synthetic regression tests + pinned real public GitHub Actions step snapshot.

No Unity process is launched. Positive receipt is an intentionally synthetic fixture.
"""
import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET
import ci41_evidence as e

SOURCE_SHA = "d95d3660c6e575349d4d825b3522a05e8a650ac3"
OBSERVED = Path(__file__).with_name("observed_ci_20261027.json")


def write_files(base, payload=None):
    root = Path(base)
    jobs = {}
    for stage, workflow in e.STAGE.items():
        document = ET.Element("test-run", {"result":"Passed", "total":"2", "passed":"2",
                                             "failed":"0", "skipped":"0", "inconclusive":"0"})
        for i in range(2):
            ET.SubElement(document, "test-case", {"fullname":f"Synthetic.{stage}.Test{i}", "result":"Passed"})
        xml = ET.tostring(document)
        log = ("Initialize engine version: 6000.3.14f1\n" + "synthetic marker only\n" * 3).encode()
        (root / f"{stage}.xml").write_bytes(xml)
        (root / f"{stage}.log").write_bytes(log)
        jobs[stage] = {"workflow": workflow, "head_sha": SOURCE_SHA, "run_id": 100 if stage=="EditMode" else 101,
                       "activation": "ENGINE_STEP_SUCCEEDED", "conclusion": "success", "native_exit_code": 0,
                       "xml_sha256":hashlib.sha256(xml).hexdigest(), "log_sha256":hashlib.sha256(log).hexdigest()}
    data = {"source_sha":SOURCE_SHA,"unity_version":e.UNITY_VERSION,"jobs":jobs,"fixture":"SYNTHETIC_NOT_UNITY"}
    if payload:
        payload(data,root)
    (root / "receipt.json").write_text(json.dumps(data))
    return data


class ActivationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.snapshots = json.loads(OBSERVED.read_text())

    def test_real_ci02_logs_do_not_claim_license(self):
        self.assertEqual(e.classify_activation(self.snapshots["EditMode"])["code"], "NO_LICENSE_INPUT")
    def test_real_ci03_logs_do_not_claim_license(self):
        self.assertEqual(e.classify_activation(self.snapshots["PlayMode"])["native"], "NOT_RUN")
    def test_nonlicense_failure_not_misdiagnosed(self):
        j=copy.deepcopy(self.snapshots["EditMode"]);j["public_log_excerpt"]="Unknown error"
        self.assertEqual(e.classify_activation(j)["code"],"ACTIVATION_PREFLIGHT_FAILED_UNKNOWN")
    def test_positive_preflight_is_not_valid_license(self):
        j=copy.deepcopy(self.snapshots["EditMode"]);j["steps"][1]["conclusion"]="success";j["steps"][2]["conclusion"]="success"
        self.assertEqual(e.classify_activation(j)["native"],"UNVERIFIED")
    def test_successful_preflight_then_engine_failure_is_not_xml_pass(self):
        j=copy.deepcopy(self.snapshots["EditMode"]);j["steps"][1]["conclusion"]="success";j["steps"][2]["conclusion"]="failure"
        self.assertEqual(e.classify_activation(j)["code"],"ENGINE_STEP_FAILED")
    def test_unrelated_runner_skip_is_not_license_failure(self):
        j=copy.deepcopy(self.snapshots["EditMode"]);j["steps"][1]["conclusion"]="skipped";j["public_log_excerpt"]=""
        self.assertEqual(e.classify_activation(j)["code"],"ENGINE_SKIPPED_OTHER_CAUSE")
    def test_duplicate_engine_steps_fail_closed(self):
        j=copy.deepcopy(self.snapshots["EditMode"]);j["steps"].append(j["steps"][2])
        with self.assertRaisesRegex(e.EvidenceError,"DUPLICATED_STEP"):e.classify_activation(j)
    def test_missing_preflight_fail_closed(self):
        j=copy.deepcopy(self.snapshots["EditMode"]);j["steps"].pop(1)
        with self.assertRaisesRegex(e.EvidenceError,"MISSING_OR_DUPLICATED_STEP"):e.classify_activation(j)
    def test_inconsistent_activation_and_engine_execution_denied(self):
        j=copy.deepcopy(self.snapshots["EditMode"]); j["steps"][2]["conclusion"]="success"
        with self.assertRaisesRegex(e.EvidenceError,"INCONSISTENT_ACTIVATION"):e.classify_activation(j)
    def test_foreign_workflow_fail_closed(self):
        j=copy.deepcopy(self.snapshots["EditMode"]);j["workflow"]="CI-01 Repository Validation"
        with self.assertRaisesRegex(e.EvidenceError,"WRONG_WORKFLOW"):e.classify_activation(j)


class ReceiptTests(unittest.TestCase):
    def test_positive_synthetic_is_structural_only(self):
        with tempfile.TemporaryDirectory() as f:
            write_files(f);out=e.check_cross_workflow_receipt(f,SOURCE_SHA)
            self.assertEqual(out["status"],"STRUCTURAL_RECEIPT_ONLY")
            self.assertEqual(out["g1"],"PENDING_INDEPENDENT_NATIVE_REVIEW")
            self.assertEqual(out["stage_cases"],{"EditMode":2,"PlayMode":2})
    def _reject(self,mutate,code):
        with tempfile.TemporaryDirectory() as f:
            write_files(f,mutate)
            with self.assertRaisesRegex(e.EvidenceError,code):e.check_cross_workflow_receipt(f,SOURCE_SHA)
    def test_cross_stage_sha_splice_rejected(self):
        self._reject(lambda d,f:d["jobs"]["PlayMode"].update(head_sha="a"*40),"CROSS_STAGE_SHA_MISMATCH")
    def test_wrong_source_sha_rejected(self):
        self._reject(lambda d,f:d.update(source_sha="b"*40),"UNEXPECTED_SOURCE_OR_EDITOR")
    def test_wrong_version_rejected(self):
        self._reject(lambda d,f:d.update(unity_version="6000.1.1f1"),"UNEXPECTED_SOURCE_OR_EDITOR")
    def test_duplicate_run_id_rejected(self):
        self._reject(lambda d,f:d["jobs"]["PlayMode"].update(run_id=100),"INVALID_OR_REUSED_RUN_ID")
    def test_swapped_mode_name_rejected(self):
        self._reject(lambda d,f:d["jobs"]["PlayMode"].update(workflow=e.STAGE["EditMode"]),"STAGE_WORKFLOW_MISMATCH")
    def test_absent_native_exit_rejected(self):
        self._reject(lambda d,f:d["jobs"]["EditMode"].pop("native_exit_code"),"MISSING_OR_NONZERO_NATIVE_EXIT")
    def test_native_exit_failure_rejected(self):
        self._reject(lambda d,f:d["jobs"]["PlayMode"].update(native_exit_code=3),"MISSING_OR_NONZERO_NATIVE_EXIT")
    def test_preflight_not_engine_execution_rejected(self):
        self._reject(lambda d,f:d["jobs"]["EditMode"].update(activation="NO_LICENSE_INPUT"),"JOB_NOT_EXECUTED_SUCCESSFULLY")
    def test_wrong_xml_hash_rejected(self):
        self._reject(lambda d,f:d["jobs"]["PlayMode"].update(xml_sha256="0"*64),"EVIDENCE_BYTES_TAMPER")
    def test_missing_log_rejected(self):
        self._reject(lambda d,f:(f/"EditMode.log").unlink(),"MISSING_OR_SYMLINKED_EVIDENCE")
    def test_empty_editor_log_rejected(self):
        def change(d,f):
            p=f/"EditMode.log";p.write_text("");d["jobs"]["EditMode"]["log_sha256"]=hashlib.sha256(b"").hexdigest()
        self._reject(change,"LOG_WITHOUT_EDITOR_VERSION")
    def test_missing_nunit_case_rejected(self):
        def change(d,f):
            p=f/"EditMode.xml";root=ET.fromstring(p.read_bytes());root.remove(list(root.iter("test-case"))[-1]);b=ET.tostring(root);p.write_bytes(b);d["jobs"]["EditMode"]["xml_sha256"]=hashlib.sha256(b).hexdigest()
        self._reject(change,"INCONSISTENT_NUNIT_AGGREGATE")
    def test_duplicate_case_rejected(self):
        def change(d,f):
            p=f/"PlayMode.xml";root=ET.fromstring(p.read_bytes());cases=list(root.iter("test-case"));cases[1].set("fullname",cases[0].get("fullname"));b=ET.tostring(root);p.write_bytes(b);d["jobs"]["PlayMode"]["xml_sha256"]=hashlib.sha256(b).hexdigest()
        self._reject(change,"DUPLICATE_OR_MISSING_TEST_ID")
    def test_skipped_case_rejected(self):
        def change(d,f):
            p=f/"PlayMode.xml";root=ET.fromstring(p.read_bytes());list(root.iter("test-case"))[0].set("result","Skipped");b=ET.tostring(root);p.write_bytes(b);d["jobs"]["PlayMode"]["xml_sha256"]=hashlib.sha256(b).hexdigest()
        self._reject(change,"NONPASS_NUNIT_CASE")
    def test_symlink_to_outside_rejected(self):
        with tempfile.TemporaryDirectory() as f, tempfile.TemporaryDirectory() as outside:
            write_files(f);target=Path(outside)/"fake.xml";target.write_bytes((Path(f)/"PlayMode.xml").read_bytes());(Path(f)/"PlayMode.xml").unlink();(Path(f)/"PlayMode.xml").symlink_to(target)
            with self.assertRaisesRegex(e.EvidenceError,"MISSING_OR_SYMLINKED_EVIDENCE"):e.check_cross_workflow_receipt(f,SOURCE_SHA)
    def test_missing_xml_rejected(self):
        self._reject(lambda d,f:(f/"EditMode.xml").unlink(),"MISSING_OR_SYMLINKED_EVIDENCE")
    def test_nunit_missing_aggregate_rejected(self):
        def change(d,f):
            p=f/"PlayMode.xml";root=ET.fromstring(p.read_bytes());del root.attrib["skipped"];b=ET.tostring(root);p.write_bytes(b);d["jobs"]["PlayMode"]["xml_sha256"]=hashlib.sha256(b).hexdigest()
        self._reject(change,"MISSING_NUNIT_AGGREGATE")

if __name__=="__main__":unittest.main(verbosity=2)
