#!/usr/bin/env python3
"""Independent physical Git fixture tests of exact-candidate source plan lock."""
import copy
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

sys.path.insert(0,str(Path(__file__).resolve().parent))
import one_candidate_lock as gate

def git(repo,*args):
    result=subprocess.run(["git","-C",str(repo),*args],capture_output=True,text=True)
    if result.returncode:
        raise AssertionError("git failed "+repr(args)+" "+result.stderr)
    return result.stdout.strip()

class OneCandidateLockTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(prefix="ci41-one-locked-")
        self.repo=Path(self.tmp.name)/"repo"
        self.repo.mkdir()
        git(self.repo,"init","-q")
        git(self.repo,"config","user.name","B01-41 Synthetic Git Fixture")
        git(self.repo,"config","user.email","ci41@invalid.example")
        self.put("Assets/Safe.cs","class Harmless {}\n")
        self.base=self.commit("original clean target")
        self.put("Assets/InheritedW06.cs","class FrozenLedger {}\n")
        self.parent=self.commit("frozen source ancestor")
        for i,path in enumerate(gate.REQUIRED):
            self.put(path,"// Real Git object fixture "+str(i)+"\n")
        self.selected=self.commit("selected downstream source")
        self.plan=self.default_plan()

    def tearDown(self):
        self.tmp.cleanup()

    def put(self,path,content):
        file=self.repo/path
        file.parent.mkdir(parents=True,exist_ok=True)
        file.write_text(content,encoding="utf-8")

    def commit(self,msg):
        git(self.repo,"add","-A")
        git(self.repo,"commit","-qm",msg)
        return git(self.repo,"rev-parse","HEAD")

    def default_plan(self):
        return {"candidate_pr":510,"candidate_sha":self.selected,
                "variant":"secure-v2-r2",
                "imports":[{"pr":510,"sha":self.selected}],
                "intended_operation":"source_only_preflight",
                "independent_g0":"PENDING","native_unity_g1":"NOT_RUN",
                "codex_ack":False}

    def check(self,plan=None,sha=None,base=None,parent=None):
        return gate.verify_plan(self.repo,self.plan if plan is None else plan,
                                selected_sha=self.selected if sha is None else sha,
                                frozen_parent=self.parent if parent is None else parent,
                                base_sha=self.base if base is None else base)

    def rejects(self,reason,mutate):
        d=copy.deepcopy(self.plan)
        mutate(d)
        with self.assertRaisesRegex(gate.Rejected,reason):
            self.check(plan=d)

    def test_01_selected_candidate_original_real_git_source_pass(self):
        r=self.check()
        self.assertEqual(r["lock"],"SELECTED_SOURCE_PREFLIGHT_PASS")
        self.assertEqual(r["allowed_import_count"],1)
        self.assertEqual(r["selected_source_sha"],self.selected)
        self.assertEqual(len(r["verified_original_git_blobs"]),len(gate.REQUIRED))
        self.assertEqual(r["release_gate"],"HOLD")
        self.assertEqual(r["native_unity_g1"],"NOT_RUN")
        print("W12_ONE_CANDIDATE_ORIGINAL_GIT_LOCK_PASS_RELEASE_HOLD")

    def test_02_reject_candidate_511_sha(self):
        self.rejects("CANDIDATE_SHA_MISMATCH",lambda d:d.update(candidate_sha=gate.ALTERNATIVES[511]))

    def test_03_reject_candidate_494_sha(self):
        self.rejects("CANDIDATE_SHA_MISMATCH",lambda d:d.update(candidate_sha=gate.ALTERNATIVES[494]))

    def test_04_reject_import_511_alternative(self):
        self.rejects("MULTIPLE_OR_FOREIGN",lambda d:d.update(imports=[{"pr":511,"sha":gate.ALTERNATIVES[511]}]))

    def test_05_reject_import_494_alternative(self):
        self.rejects("MULTIPLE_OR_FOREIGN",lambda d:d.update(imports=[{"pr":494,"sha":gate.ALTERNATIVES[494]}]))

    def test_06_reject_510_and_511_concurrent(self):
        self.rejects("MULTIPLE_OR_FOREIGN",lambda d:d["imports"].append({"pr":511,"sha":gate.ALTERNATIVES[511]}))

    def test_07_reject_510_and_494_concurrent(self):
        self.rejects("MULTIPLE_OR_FOREIGN",lambda d:d["imports"].append({"pr":494,"sha":gate.ALTERNATIVES[494]}))

    def test_08_reject_wrong_variant_label(self):
        self.rejects("UNSELECTED_VARIANT",lambda d:d.update(variant="secure-v2-r1-1"))

    def test_09_reject_wrong_selected_pr_number(self):
        self.rejects("ONLY_PR_510",lambda d:d.update(candidate_pr=511))

    def test_10_reject_spoofed_independent_g0(self):
        self.rejects("UNPROVEN_GATES",lambda d:d.update(independent_g0="ACCEPTED_SOURCE_ONLY"))

    def test_11_reject_spoofed_unity_native_g1(self):
        self.rejects("UNPROVEN_GATES",lambda d:d.update(native_unity_g1="PASS"))

    def test_12_reject_spoofed_codex_ack(self):
        self.rejects("UNPROVEN_GATES",lambda d:d.update(codex_ack=True))

    def test_13_reject_direct_install_operation(self):
        self.rejects("UNPROVEN_GATES",lambda d:d.update(intended_operation="integrate"))

    def test_14_reject_extra_unvetted_plan_field(self):
        self.rejects("PLAN_SCHEMA",lambda d:d.update(approved=True))

    def test_15_reject_missing_required_original_source(self):
        missing=gate.REQUIRED[1]
        (self.repo/missing).unlink()
        newsha=self.commit("remove actual original AtomicDockLedger")
        d=copy.deepcopy(self.plan)
        d["candidate_sha"]=newsha
        d["imports"]=[{"pr":510,"sha":newsha}]
        with self.assertRaisesRegex(gate.Rejected,"ORIGINAL_SELECTED_SOURCE_REQUIRED"):
            self.check(plan=d,sha=newsha)

    def test_16_reject_legacy_W07_in_selected_candidate(self):
        self.put("Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs","class Legacy {}\n")
        newsha=self.commit("inject real competing legacy source")
        d=copy.deepcopy(self.plan)
        d["candidate_sha"]=newsha
        d["imports"]=[{"pr":510,"sha":newsha}]
        with self.assertRaisesRegex(gate.Rejected,"ALTERNATIVE_OR_LEGACY"):
            self.check(plan=d,sha=newsha)

    def test_17_reject_unfrozen_target_with_selected_path(self):
        # Build a different *real* target -> frozen parent -> candidate commit
        # lineage; the target already contains a conflicting V2-owned asset.
        git(self.repo,"reset","--hard",self.base)
        self.put("Assets/B0123SecureReattachV2/Runtime/Foreign.cs","class Foreign {}\n")
        target=self.commit("unsafe original target already includes selected source")
        self.put("Assets/InheritedW06.cs","class FrozenLedger {}\n")
        parent=self.commit("W06-like immutable ancestor of selected")
        for i,path in enumerate(gate.REQUIRED):
            self.put(path,"// Different full source fixture "+str(i)+"\n")
        candidate=self.commit("actual selected descendant target")
        d=copy.deepcopy(self.plan)
        d["candidate_sha"]=candidate
        d["imports"]=[{"pr":510,"sha":candidate}]
        with self.assertRaisesRegex(gate.Rejected,"TARGET_HAS_LEGACY_OR_SELECTION_COLLISION"):
            self.check(plan=d,sha=candidate,base=target,parent=parent)

    def test_18_reject_source_commit_missing_from_git_repo(self):
        d=copy.deepcopy(self.plan)
        d["candidate_sha"]="a"*40
        d["imports"]=[{"pr":510,"sha":"a"*40}]
        with self.assertRaisesRegex(gate.Rejected,"GIT_CHECK_FAILED"):
            self.check(plan=d,sha="a"*40)

    def test_19_reject_commit_string_ref_instead_of_exact_sha(self):
        d=copy.deepcopy(self.plan)
        d["candidate_sha"]="HEAD"
        d["imports"]=[{"pr":510,"sha":"HEAD"}]
        with self.assertRaisesRegex(gate.Rejected,"EXACT_COMMIT_SHA_REQUIRED"):
            self.check(plan=d,sha="HEAD")

    def test_20_real_cli_rejects_untrusted_fixture_as_if_production(self):
        path=Path(self.tmp.name)/"plan.json"
        path.write_text(json.dumps(self.plan),encoding="utf-8")
        r=subprocess.run([sys.executable,gate.__file__,"--repo",str(self.repo),
                          "--plan",str(path)],capture_output=True,text=True)
        self.assertEqual(r.returncode,2,r.stderr+r.stdout)
        report=json.loads(r.stdout)
        self.assertEqual(report["lock"],"REJECTED_FAIL_CLOSED")
        self.assertEqual(report["release_gate"],"HOLD")
        self.assertEqual(report["native_unity_g1"],"NOT_RUN")

    def test_21_reject_bad_json_plan_as_data_not_commands(self):
        path=Path(self.tmp.name)/"bad.json"
        path.write_text("{not-json",encoding="utf-8")
        with self.assertRaisesRegex(gate.Rejected,"INVALID_PLAN_JSON"):
            gate.read_plan(path)

    def test_22_reject_duplicate_import_even_same_sha(self):
        self.rejects("MULTIPLE_OR_FOREIGN",lambda d:d["imports"].append(d["imports"][0].copy()))

    def test_23_reject_boolean_pr_instead_of_number(self):
        self.rejects("ONLY_PR_510",lambda d:d.update(candidate_pr=True))

    def test_24_read_only_state_does_not_create_untracked_game_files(self):
        before=git(self.repo,"status","--porcelain")
        self.check()
        self.assertEqual(before,git(self.repo,"status","--porcelain"))

if __name__=="__main__":
    unittest.main(verbosity=2)
