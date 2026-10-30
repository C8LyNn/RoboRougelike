#!/usr/bin/env python3
"""Original immutable GitHub API captures + hostile version-evidence mutations."""
import copy
import json
import os
from pathlib import Path
import sys
import unittest
from unittest import mock

sys.path.insert(0,str(Path(__file__).resolve().parent))
import bridge_qa_freshness as q

ROOT=Path(__file__).resolve().parent
FIXTURE=ROOT/"original-pull-qa-capture.json"

class FreshnessAgainstOriginalGithub(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.actual=json.loads(FIXTURE.read_text(encoding="utf-8"))
        cls.truth=q.inspect(cls.actual)
        assert cls.truth["release_gate"]=="HOLD"

    def mutation(self,edit,reason):
        mutated=copy.deepcopy(self.actual)
        edit(mutated)
        with self.assertRaisesRegex(q.FenceReject,reason):
            q.inspect(mutated)

    def test_01_actual_current_587_core_is_different(self):
        x=self.truth["candidates"]["587"]
        self.assertEqual(x["source_test_freshness"],
                         "QA_ORIGINAL_CORE_BLOB_STALE_RETEST_NEW_SHA")
        self.assertNotEqual(x["actual_current_core_git_blob"],
                            x["actual_qa_tested_core_git_blob"])
        self.assertEqual(x["source_admission"],"HOLD")
        print("REAL_CURRENT_587_CORE_SHA_DIFFERS_FROM_588_QA_SOURCE_HOLD")

    def test_02_actual_585_qa_exact_original_but_red(self):
        x=self.truth["candidates"]["585"]
        self.assertEqual(x["source_test_freshness"],
                         "EXACT_PRODUCER_SHA_TESTED")
        self.assertEqual(x["qa_run_executed"],"PASS_WITH_EXPECTED_RELEASE_RED")
        self.assertIn("POST_ACTIVATION",x["independent_release_blocker"])
        self.assertEqual(x["source_admission"],"HOLD")
        print("REAL_585_EXACT_QA_BUT_ACTIVATION_SECURITY_RED")

    def test_03_both_candidates_refused_and_no_claims(self):
        r=self.truth
        self.assertEqual(r["candidate_selection"],"NOT_AUTHORIZED_BY_B01_41")
        self.assertEqual(r["host_save_load"],"HOLD")
        self.assertEqual(r["source_bridge_g0"],"PENDING_OWNER_AND_INDEPENDENT_QA")
        self.assertEqual(r["unity_native_g1"],"NOT_RUN")
        self.assertEqual(r["g2"],"HOLD")
        self.assertIs(r["codex_ack"],False)
        self.assertIs(r["integrated"],False)
        self.assertEqual(r["release_gate"],"HOLD")

    def test_04_original_qa_run_shas_are_the_qa_pr_heads(self):
        for pr in ("585","587"):
            self.assertEqual(self.actual["qa_runs"][pr]["head_sha"],
                             q.PINS[q.PROPOSALS[pr]["qa"]])

    def test_05_original_source_blob_identities_are_concrete(self):
        self.assertEqual(self.actual["sources"]["585:current"]["blob"],
                         "e9e9dc6d437e96dad6b08c1639e8bd94d62c4f4b")
        self.assertEqual(self.actual["sources"]["587:tested"]["blob"],
                         "db69bae2bf4cf106da68f13e0b067499f0da02f3")
        self.assertEqual(self.actual["sources"]["587:current"]["blob"],
                         "16a7d7043a2ecb59a41bbfb063170235872fd7fc")

    def test_06_stable_original_evidence_digest(self):
        self.assertEqual(len(self.truth["evidence_sha256"]),64)
        self.assertEqual(self.truth,q.inspect(copy.deepcopy(self.actual)))

def mutate(path,value):
    def edit(data):
        target=data
        for x in path[:-1]:target=target[x]
        target[path[-1]]=value
    return edit

HOST="587"
SRC="587:current"
OLD="587:tested"
MUTATIONS=[
 ("game_sha",("pulls","510","head_sha"),"0"*40,"ORIGINAL_PR_IDENTITY"),
 ("save_sha",("pulls","556","head_sha"),"f"*40,"ORIGINAL_PR_IDENTITY"),
 ("587_pr",("pulls","587","number"),585,"ORIGINAL_PR_IDENTITY"),
 ("585_pr",("pulls","585","number"),587,"ORIGINAL_PR_IDENTITY"),
 ("qa_588_sha",("pulls","588","head_sha"),"e"*40,"ORIGINAL_PR_IDENTITY"),
 ("qa_572_sha",("pulls","572","head_sha"),"e"*40,"ORIGINAL_PR_IDENTITY"),
 ("585_repo",("pulls","585","repository"),"attacker/repo","ORIGINAL_PR_IDENTITY"),
 ("587_repo",("pulls","587","repository"),"attacker/repo","ORIGINAL_PR_IDENTITY"),
 ("585_closed",("pulls","585","state"),"closed","ORIGINAL_PR_STATE"),
 ("587_not_draft",("pulls","587","draft"),False,"ORIGINAL_PR_STATE"),
 ("game_closed",("pulls","510","state"),"closed","ORIGINAL_PR_STATE"),
 ("save_not_draft",("pulls","556","draft"),False,"ORIGINAL_PR_STATE"),
 ("587_wrong_parent",("pulls","587","base_sha"),"a"*40,"HOST_BRIDGE_NOT_BASED"),
 ("585_wrong_parent",("pulls","585","base_sha"),"b"*40,"HOST_BRIDGE_NOT_BASED"),
 ("587_wrong_path",("sources",SRC,"path"),"Assets/attacker.cs","MISSING_CURRENT_HOST"),
 ("587_wrong_kind",("sources",SRC,"type"),"dir","MISSING_CURRENT_HOST"),
 ("585_current_no_blob",("sources","585:current","blob"),None,"MISSING_CURRENT_HOST"),
 ("587_current_short_blob",("sources",SRC,"blob"),"no","MISSING_CURRENT_HOST"),
 ("qa_587_original_blob",("sources",OLD,"blob"),"0"*40,"QA_TESTED_SOURCE_BLOB_MISMATCH"),
 ("qa_585_original_blob",("sources","585:tested","blob"),"f"*40,"QA_TESTED_SOURCE_BLOB_MISMATCH"),
 ("qa_587_wrong_source",("sources",OLD,"path"),"Assets/other.cs","QA_TESTED_SOURCE_BLOB_MISMATCH"),
 ("current_587_missing_size",("sources",SRC,"size"),None,"INVALID_SOURCE_BYTES"),
 ("current_585_tiny",("sources","585:current","size"),1,"INVALID_SOURCE_BYTES"),
 ("qa_588_wrong_run",("qa_runs","587","id"),99,"ORIGINAL_QA_RUN_PROVENANCE"),
 ("qa_572_wrong_sha",("qa_runs","585","head_sha"),"0"*40,"ORIGINAL_QA_RUN_PROVENANCE"),
 ("qa_588_failed",("qa_runs","587","conclusion"),"failure","ORIGINAL_QA_RUN_PROVENANCE"),
 ("qa_572_incomplete",("qa_runs","585","status"),"in_progress","ORIGINAL_QA_RUN_PROVENANCE"),
 ("qa_588_foreign_repo",("qa_runs","587","repository"),"foreign/repo","ORIGINAL_QA_RUN_PROVENANCE"),
 ("587_forged_same_core",("sources",SRC,"blob"),q.PROPOSALS["587"]["qa_original_target_blob"],
  "HISTORICAL_BASELINE_CHANGED"),
 ("585_forged_drift",("sources","585:current","blob"),"f"*40,
  "HISTORICAL_BASELINE_CHANGED"),
]
for idx,(label,path,value,reason) in enumerate(MUTATIONS,7):
    def run(self,path=path,value=value,reason=reason):
        self.mutation(mutate(path,value),reason)
    setattr(FreshnessAgainstOriginalGithub,f"test_{idx:02d}_{label}",run)

class CaptureStructureTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.actual=json.loads(FIXTURE.read_text())

    def test_37_missing_pr_ref_rejected(self):
        x=copy.deepcopy(self.actual)
        del x["pulls"]["587"]
        with self.assertRaisesRegex(q.FenceReject,"MISSING_OR_FOREIGN_PULLS"):
            q.inspect(x)

    def test_38_unexpected_extra_candidate_rejected(self):
        x=copy.deepcopy(self.actual)
        x["pulls"]["999"]=x["pulls"]["587"]
        with self.assertRaisesRegex(q.FenceReject,"MISSING_OR_FOREIGN_PULLS"):
            q.inspect(x)

    def test_39_missing_tested_source_rejected(self):
        x=copy.deepcopy(self.actual)
        del x["sources"]["587:tested"]
        with self.assertRaisesRegex(q.FenceReject,"MISSING_OR_FOREIGN_SOURCES"):
            q.inspect(x)

    def test_40_missing_independent_qa_run_rejected(self):
        x=copy.deepcopy(self.actual)
        del x["qa_runs"]["587"]
        with self.assertRaisesRegex(q.FenceReject,"MISSING_OR_FOREIGN_QA_RUNS"):
            q.inspect(x)

    def test_41_wrong_capture_shape_rejected(self):
        with self.assertRaisesRegex(q.FenceReject,"INVALID_CAPTURE_SHAPE"):
            q.inspect({"pulls":self.actual["pulls"]})

if __name__=="__main__":
    unittest.main(verbosity=2)
