#!/usr/bin/env python3
"""Mutation tests on downloaded ORIGINAL GitHub run/job metadata, no mock API."""
from __future__ import annotations
import copy
import json
from pathlib import Path
import unittest
import action_evidence as a

ROOT=Path(__file__).resolve().parent
FIXTURES=ROOT/"receipts"
CASES=tuple(a.CASES)

class OriginalRunEvidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.original={}
        for case in CASES:
            path=FIXTURES/("original-"+case+".json")
            cls.original[case]=a.read_capture(path)
            original=a.validate(case,cls.original[case])
            assert original["native_unity_g1"]=="NOT_RUN"
            assert original["release_gate"]=="HOLD"

    def mutated(self,case,change,reason):
        x=copy.deepcopy(self.original[case])
        change(x)
        with self.assertRaisesRegex(a.EvidenceError,reason):
            a.validate(case,x)

    def test_01_original_selected_source_not_native(self):
        r=a.validate("selected-510-source",self.original["selected-510-source"])
        self.assertEqual(r["classification"],"SOURCE_ONLY_EXECUTED")
        self.assertEqual(r["exact_run_head_sha"],a.SELECTED)
        self.assertEqual(r["native_unity_g1"],"NOT_RUN")
        print("ORIGINAL_SELECTED_510_SOURCE_SUCCESS_NATIVE_G1_NOT_RUN")

    def test_02_real_native_readiness_ZIP_not_Unity(self):
        r=a.validate("native-532-readiness",self.original["native-532-readiness"])
        self.assertEqual(r["classification"],"SOURCE_ONLY_EXECUTED")
        self.assertEqual(r["source_job_executed"],"PASS")
        self.assertEqual(r["native_unity_g1"],"NOT_RUN")
        self.assertEqual(r["save_load_persistence"],"HOLD_UNTIL_INDEPENDENT_ADAPTER_ACCEPTANCE")
        print("ORIGINAL_532_ZIP_PASS_NATIVE_NOT_RUN_SAVELOAD_HOLD")

    def test_03_original_ci02_activation_prevents_native(self):
        r=a.validate("blocked-edit-522",self.original["blocked-edit-522"])
        self.assertEqual(r["classification"],"NATIVE_BLOCKED_ACTIVATION")
        self.assertEqual(r["native_unity_g1"],"NOT_RUN")

    def test_04_original_ci03_activation_prevents_native(self):
        r=a.validate("blocked-play-522",self.original["blocked-play-522"])
        self.assertEqual(r["classification"],"NATIVE_BLOCKED_ACTIVATION")
        self.assertEqual(r["native_unity_g1"],"NOT_RUN")

    def test_05_real_mutation_rejects_unknown_case(self):
        with self.assertRaisesRegex(a.EvidenceError,"UNKNOWN_PINNED_RUN"):
            a.validate("fabricated",self.original["native-532-readiness"])

    def test_06_real_capture_retained_exact_source_sha(self):
        doc=self.original["selected-510-source"]
        self.assertEqual(doc["run"]["head_sha"],a.SELECTED)
        self.assertEqual(len(doc["jobs"]["jobs"]),2)
        self.assertTrue(all(x["head_sha"]==a.SELECTED for x in doc["jobs"]["jobs"]))

def tweak(run=None,job=None,step=None):
    """Mutate real fields in ORIGINAL captured GitHub API data."""
    def apply(x):
        if run:
            key,value=run
            x["run"][key]=value
        if job:
            key,value=job
            x["jobs"]["jobs"][0][key]=value
        if step:
            target, key, value=step
            match=[s for s in x["jobs"]["jobs"][0]["steps"] if s.get("name")==target]
            if not match:
                raise RuntimeError("real GitHub step not present: "+target)
            match[0][key]=value
    return apply

SOURCE="native-532-readiness"
BLOCKED="blocked-edit-522"
STEP_SOURCE="Build exact-original portable Unity 6000.3.14f1 project and verify 10 adversarial negatives (NOT native)"
STEP_ACTIVATION="Require valid Unity activation credentials"
STEP_NATIVE="Unity 6000.3.14f1 EditMode"
STEP_CHECK="Require nonempty, fully passing executed test cases"
STEP_LOG="Preserve XML, Unity logs or activation blocker"

MUTATIONS=[
 ("run_id",SOURCE,tweak(run=("id",9)),"RUN_ID_MISMATCH"),
 ("head_sha",SOURCE,tweak(run=("head_sha","0"*40)),"RUN_HEAD_SHA_MISMATCH"),
 ("workflow",SOURCE,tweak(run=("name","Unity tests all green")),"WORKFLOW_IDENTITY_MISMATCH"),
 ("repo",SOURCE,lambda d:d["run"]["repository"].update(full_name="attacker/other"),"CROSS_REPOSITORY_RUN"),
 ("run_attempt",SOURCE,tweak(run=("run_attempt",2)),"RETRIED_RUN_NEEDS"),
 ("run_state",SOURCE,tweak(run=("status","in_progress")),"RUN_NOT_COMPLETED"),
 ("event",SOURCE,tweak(run=("event","workflow_dispatch")),"UNEXPECTED_EVENT"),
 ("job_total",SOURCE,lambda d:d["jobs"].update(total_count=1),"MISSING_OR_DUPLICATED_JOBS"),
 ("job_extra",SOURCE,lambda d:d["jobs"]["jobs"].append(copy.deepcopy(d["jobs"]["jobs"][0])),"MISSING_OR_DUPLICATED_JOBS"),
 ("job_sha",SOURCE,tweak(job=("head_sha","a"*40)),"JOB_SHA_MISMATCH"),
 ("job_attempt",SOURCE,tweak(job=("run_attempt",2)),"JOB_ATTEMPT_MISMATCH"),
 ("job_state",SOURCE,tweak(job=("status","in_progress")),"INCOMPLETE_JOB"),
 ("job_conclusion",SOURCE,tweak(job=("conclusion","failure")),"SOURCE_JOB_NOT_SUCCESS"),
 ("source_run_conclusion",SOURCE,tweak(run=("conclusion","failure")),"SOURCE_JOB_NOT_SUCCESS"),
 ("source_step_skipped",SOURCE,tweak(step=(STEP_SOURCE,"conclusion","skipped")),"MISSING_EXECUTED_SOURCE_STEP"),
 ("source_step_not_finished",SOURCE,tweak(step=(STEP_SOURCE,"status","in_progress")),"MISSING_EXECUTED_SOURCE_STEP"),
 ("duplicate_step_names",SOURCE,lambda d:d["jobs"]["jobs"][0]["steps"].append(copy.deepcopy(d["jobs"]["jobs"][0]["steps"][3])),"DUPLICATE_OR_MALFORMED_STEPS"),
 ("source_native_spoof",SOURCE,lambda d:d["jobs"]["jobs"][0]["steps"].append({"name":"Unity 6000.3.14f1 PlayMode","conclusion":"success","status":"completed"}),"SOURCE_RUN_CLAIMS_NATIVE_STEP"),
 ("source_job_without_steps",SOURCE,tweak(job=("steps",[])),"NO_ACTUAL_JOB_STEP_EVIDENCE"),
 ("source_case_tamper",SOURCE,lambda d:d.update(case="selected-510-source"),"EVIDENCE_CASE_MISMATCH"),
 ("activation_forged_pass",BLOCKED,tweak(step=(STEP_ACTIVATION,"conclusion","success")),"ACTIVATION_OR_NATIVE_STEP_EVIDENCE_MISMATCH"),
 ("native_runner_forged_pass",BLOCKED,tweak(step=(STEP_NATIVE,"conclusion","success")),"ACTIVATION_OR_NATIVE_STEP_EVIDENCE_MISMATCH"),
 ("nunit_check_forged_pass",BLOCKED,tweak(step=(STEP_CHECK,"conclusion","success")),"ACTIVATION_OR_NATIVE_STEP_EVIDENCE_MISMATCH"),
 ("artifact_absent",BLOCKED,tweak(step=(STEP_LOG,"conclusion","skipped")),"ACTIVATION_OR_NATIVE_STEP_EVIDENCE_MISMATCH"),
 ("blocked_run_forged_pass",BLOCKED,tweak(run=("conclusion","success")),"BLOCKED_RUN_STATUS_TAMPERED"),
 ("blocked_job_forged_pass",BLOCKED,tweak(job=("conclusion","success")),"BLOCKED_RUN_STATUS_TAMPERED"),
 ("blocked_cross_sha",BLOCKED,tweak(job=("head_sha",a.SELECTED)),"JOB_SHA_MISMATCH"),
 ("blocked_attempt",BLOCKED,tweak(job=("run_attempt",2)),"JOB_ATTEMPT_MISMATCH"),
 ("selected_cross_source", "selected-510-source",tweak(run=("head_sha",a.CASES["native-532-readiness"]["sha"])),"RUN_HEAD_SHA_MISMATCH"),
]

# Stable independent test IDs for every real-source negative mutation.
for index,(label,case,mutation,reason) in enumerate(MUTATIONS,start=7):
    def case_test(self,case=case,mutation=mutation,reason=reason):
        self.mutated(case,mutation,reason)
    setattr(OriginalRunEvidenceTests,f"test_{index:02d}_{label}",case_test)

if __name__=="__main__":
    unittest.main(verbosity=2)
