#!/usr/bin/env python3
"""Real Git commit fixtures and negative CI wiring mutation tests; Unity NOT_RUN."""
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parent))
import native_ci_contract as gate

HERE=Path(__file__).resolve().parents[4]

def git(repo,*args,expect=0):
    p=subprocess.run(["git","-C",str(repo),*args],capture_output=True,text=True)
    if p.returncode!=expect:
        raise AssertionError("GIT "+str(args)+" "+p.stdout+" "+p.stderr)
    return p.stdout.strip()

class GitNativeCIContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.actual_source_sha=git(HERE,"rev-parse","HEAD")
        cls.original={}
        for kind,path in gate.FILES.items():
            cls.original[kind]=git(HERE,"show",cls.actual_source_sha+":"+path)+"\n"
            # git subprocess .strip() removes end-of-line; all authored source ends in LF
            gate.validate(kind,cls.original[kind])

    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(prefix="ci41-native-contract-")
        self.repo=Path(self.tmp.name)
        git(self.repo,"init","-q")
        git(self.repo,"config","user.email","ci41@example.invalid")
        git(self.repo,"config","user.name","Native Contract Fixture")
        for kind,path in gate.FILES.items():
            file=self.repo/path
            file.parent.mkdir(parents=True,exist_ok=True)
            file.write_text(self.original[kind],encoding="utf-8")
        self.base=self.commit("real original three canonical Unity workflows")

    def tearDown(self):
        self.tmp.cleanup()

    def commit(self,message):
        git(self.repo,"add","-A")
        git(self.repo,"commit","-qm",message)
        return git(self.repo,"rev-parse","HEAD")

    def changed(self,kind,find,replacement):
        path=self.repo/gate.FILES[kind]
        original=path.read_text(encoding="utf-8")
        self.assertIn(find,original)
        path.write_text(original.replace(find,replacement,1),encoding="utf-8")
        return self.commit("negative real Git object alteration: "+kind)

    def reject(self,kind,find,replacement,reason):
        altered=self.changed(kind,find,replacement)
        with self.assertRaisesRegex(gate.UnsafeContract,reason):
            gate.run(self.repo,altered)
        self.assertEqual(gate.run(self.repo,self.base)["result"],"SOURCE_CONTRACT_PASS")

    def test_01_canonical_real_git_commit_gates_all_three_native_jobs(self):
        r=gate.run(self.repo,self.base)
        self.assertEqual(r["result"],"SOURCE_CONTRACT_PASS")
        self.assertEqual(len(r["workflows"]),3)
        self.assertEqual([x["step_count"] for x in r["workflows"]],[6,6,6])
        self.assertEqual(r["native_unity"],"NOT_RUN")
        print("REAL_ORIGINAL_THREE_NATIVE_CI_GIT_OBJECTS_SOURCE_CONTRACT_PASS_NOT_UNITY")

    def test_02_missing_real_editmode_runner(self):
        self.reject("editmode","game-ci/unity-test-runner@","example/not-a-test-runner@","MISSING_PINNED_UNITY_TEST_RUNNER")

    def test_03_changed_real_playmode_mode(self):
        self.reject("playmode","testMode: playmode","testMode: editmode","NATIVE_TEST_MODE_DRIFT")

    def test_04_removed_editmode_nonempty_results_gate(self):
        self.reject("editmode","python3 Tools/CI/check_results.py","echo green && python3 Tools/CI/check_results.py","NATIVE_TEST_RESULT_CHECK_REMOVED")

    def test_05_activation_failure_changed_to_success(self):
        self.reject("editmode","            exit 1","            exit 0","ACTIVATION_NOT_FAIL_CLOSED")

    def test_06_credential_absent_from_activation(self):
        self.reject("playmode","UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}","UNITY_PASSWORD: ''","ACTIVATION_SECRET_BINDING_MISSING")

    def test_07_credential_absent_from_native_runner(self):
        a=self.original["playmode"]
        needle="UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}"
        self.assertEqual(a.count(needle),2)
        self.reject("playmode",a,
                    a.replace(needle,"UNITY_PASSWORD: disabled",2),"NATIVE_RUNNER_SECRET_BINDING_MISSING")

    def test_08_evidence_no_longer_always_uploads(self):
        self.reject("editmode","if: always()","if: success()","LOSS_OF_FAILURE_EVIDENCE")

    def test_09_build_target_unsupported(self):
        self.reject("windows-build","targetPlatform: StandaloneWindows64","targetPlatform: Linux64","WRONG_NATIVE_BUILD_TARGET")

    def test_10_windows_executable_check_gone(self):
        self.reject("windows-build","test -s build/StandaloneWindows64/RoboRougelike.exe","echo okay","NATIVE_PLAYER_OUTPUT_CHECK_REMOVED")

    def test_11_push_trigger_removed(self):
        self.reject("playmode","  push:\n","  disabled_push:\n","MISSING_PUSH_TRIGGER")

    def test_12_pull_request_trigger_removed(self):
        self.reject("editmode","  pull_request:\n","  disabled_pull_request:\n","MISSING_PR_TRIGGER")

    def test_13_checkout_revision_unpinned(self):
        self.reject("editmode",gate.CHECKOUT,"actions/checkout@v4","UNPINNED_CHECKOUT")

    def test_14_extra_unexpected_step_does_not_bypass(self):
        self.reject("editmode","      - name: Unity 6000.3.14f1 EditMode",
                    "      - name: Mystery bypass\n        run: echo skip\n      - name: Unity 6000.3.14f1 EditMode",
                    "UNEXPECTED_NATIVE_STEP_COUNT")

    def test_15_skipping_native_runner_using_if_false(self):
        self.reject("playmode","      - name: Unity 6000.3.14f1 PlayMode",
                    "      - name: Unity 6000.3.14f1 PlayMode\n        if: false",
                    "CONDITIONAL_NATIVE_STEP_BYPASS")

    def test_16_change_activation_secrets_leaked(self):
        self.reject("windows-build","            exit 1",
                    '            echo "$UNITY_LICENSE"\n            exit 1',
                    "ACTIVATION_SECRET_ECHO")

    def test_17_wrong_unity_version(self):
        self.reject("playmode","unityVersion: 6000.3.14f1","unityVersion: 2022.3.1f1",
                    "UNITY_VERSION_DRIFT")

    def test_18_invalid_sha_rejected_prior_to_reading(self):
        with self.assertRaisesRegex(gate.UnsafeContract,"EXACT_COMMIT_SHA_REQUIRED"):
            gate.run(self.repo,"HEAD")
        with self.assertRaises(gate.UnsafeContract):
            gate.run(self.repo,"f"*40)

    def test_19_real_cli_green_and_fail_closed_nonzero(self):
        script=Path(gate.__file__)
        good=subprocess.run([sys.executable,str(script),"--repo",str(self.repo),
                             "--head-sha",self.base],capture_output=True,text=True)
        self.assertEqual(good.returncode,0,good.stdout+good.stderr)
        self.assertEqual(json.loads(good.stdout)["result"],"SOURCE_CONTRACT_PASS")
        bad=self.changed("editmode","testMode: editmode","testMode: skipped")
        failed=subprocess.run([sys.executable,str(script),"--repo",str(self.repo),
                               "--head-sha",bad],capture_output=True,text=True)
        self.assertEqual(failed.returncode,2,failed.stdout+failed.stderr)
        self.assertEqual(json.loads(failed.stdout)["result"],"FAIL_CLOSED")
        self.assertEqual(json.loads(failed.stdout)["native_unity"],"NOT_RUN")
        print("REAL_ORIGINAL_CLI_GREEN_AND_INVALID_COMMITTED_WORKFLOW_EXIT2_PASS")

    def test_20_unmodified_documentation_does_not_change_contract(self):
        file=self.repo/"Docs/NonGameReadme.md"
        file.parent.mkdir(parents=True,exist_ok=True)
        file.write_text("Safe docs only",encoding="utf-8")
        c=self.commit("unrelated docs edit")
        self.assertEqual(gate.run(self.repo,c)["result"],"SOURCE_CONTRACT_PASS")

if __name__=="__main__":
    unittest.main(verbosity=2)
