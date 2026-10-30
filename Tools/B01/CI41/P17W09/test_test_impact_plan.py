#!/usr/bin/env python3
"""Physical original Git-diff Unity test-impact planner regressions.

These 22 tests use real Git init/commit/diff/rename, real test assemblies,
and standalone CLI processes; NOT an actual Unity Editor runner.
"""
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parent))
import test_impact_plan as gate

A="MOV001Booster"
B="MOV002JumpDrive"


def git(repo, *args):
    p=subprocess.run(["git","-C",str(repo),*args],text=True,capture_output=True)
    if p.returncode:
        raise AssertionError(p.stderr)
    return p.stdout.strip()


class PhysicalGitImpactTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(prefix="ci41-impact-")
        self.repo=Path(self.tmp.name)
        git(self.repo,"init","-q")
        git(self.repo,"config","user.name","CI41 Test")
        git(self.repo,"config","user.email","ci41@example.invalid")
        self.seed={}
        for mod in (A,B):
            prefix="Assets/RoboRougelikeMechanics/"+mod
            self.seed[prefix+"/Runtime/RoboRougelike."+mod+".asmdef"]=json.dumps({"name":"RoboRougelike."+mod})
            self.seed[prefix+"/Runtime/Engine.cs"]="class OriginalEngine {}"
            for mode in ("EditMode","PlayMode"):
                self.seed[prefix+"/Tests/"+mode+"/RoboRougelike."+mod+"."+mode+"Tests.asmdef"]=json.dumps(
                    {"name":"RoboRougelike."+mod+"."+mode+"Tests"})
                self.seed[prefix+"/Tests/"+mode+"/OriginalTests.cs"]="class OriginalTests {}"
        self.seed["Assets/Tests/Editor/SharedIntegrationTests.cs"]="class SharedIntegrationTests {}"
        self.seed["Docs/README.md"]="original"
        self.write_all(self.seed)
        self.base=self.commit("base exact original test source")

    def tearDown(self):
        self.tmp.cleanup()

    def write(self,path,text):
        p=self.repo/path
        p.parent.mkdir(parents=True,exist_ok=True)
        p.write_text(text,encoding="utf-8")

    def write_all(self,items):
        for path,body in items.items():
            self.write(path,body)

    def commit(self,msg):
        git(self.repo,"add","-A")
        git(self.repo,"commit","-qm",msg)
        return git(self.repo,"rev-parse","HEAD")

    def report(self,change=None,deleted=None,renamed=None,base=None):
        for path,body in (change or {}).items():
            self.write(path,body)
        for path in (deleted or []):
            (self.repo/path).unlink()
        for old,new in (renamed or []):
            (self.repo/new).parent.mkdir(parents=True,exist_ok=True)
            (self.repo/old).rename(self.repo/new)
        head=self.commit("physical original git change")
        inv=gate.inventory(self.repo,head)
        events=gate.changed_paths(self.repo,base or self.base,head)
        return gate.decide(inv,events,base or self.base,head),inv,head

    def assert_full(self,kwargs,reason):
        report,_,_=self.report(**kwargs)
        self.assertEqual(report["mode"],"FULL_UNITY_SUITE_REQUIRED",report)
        self.assertTrue(report["full_suite_required_now"])
        self.assertTrue(any(reason in s for s in report["reasons"]))
        self.assertEqual(report["native_unity"],"NOT_RUN")

    def test_01_original_physical_two_module_inventory(self):
        inv=gate.inventory(self.repo,self.base)
        self.assertEqual((inv["module_count"],inv["module_test_file_count"],
                          inv["central_editor_test_file_count"]),(2,4,1))
        self.assertEqual(set(inv["modules"]),{A,B})
        self.assertTrue(inv["modules"][A]["has_runtime"])
        print("ACTUAL_PHYSICAL_GIT_TWO_MODULE_EDITMODE_PLAYMODE_INVENTORY_PASS")

    def test_02_single_module_source_change_routes_2_existing_test_assemblies(self):
        path="Assets/RoboRougelikeMechanics/"+A+"/Runtime/Engine.cs"
        report,_,_=self.report(change={path:"class EngineChanged {}"})
        self.assertEqual(report["mode"],"TARGETED_PRECHECK_PLUS_FULL_G1_LATER",report)
        self.assertEqual({s["mode"] for s in report["selected_precheck_assemblies"]},
                         {"EditMode","PlayMode"})
        self.assertEqual({s["module"] for s in report["selected_precheck_assemblies"]},{A})
        self.assertEqual(report["game_g1"],"NOT_ASSESSED")
        print("ACTUAL_PHYSICAL_GIT_ONE_MODULE_SELECTS_TWO_TEST_ASSEMBLIES_NOT_G1")

    def test_03_cross_module_changes_require_full(self):
        x="Assets/RoboRougelikeMechanics/"+A+"/Runtime/Engine.cs"
        y="Assets/RoboRougelikeMechanics/"+B+"/Runtime/Engine.cs"
        self.assert_full({"change":{x:"A",y:"B"}},"CROSS_MODULE_CHANGES")

    def test_04_global_assets_change_requires_full(self):
        self.assert_full({"change":{"Assets/Scenes/Arena.unity":"some scene"}},"UNSCOPED_GAME_ASSET")

    def test_05_central_integration_test_change_requires_full(self):
        self.assert_full({"change":{"Assets/Tests/Editor/SharedIntegrationTests.cs":"changed"}},"GLOBAL_OR_CENTRAL_TEST")

    def test_06_packages_manifest_change_requires_full(self):
        self.assert_full({"change":{"Packages/manifest.json":"{}"}},"GLOBAL_OR_CENTRAL_TEST")

    def test_07_projectsettings_change_requires_full(self):
        self.assert_full({"change":{"ProjectSettings/ProjectVersion.txt":"m_EditorVersion: 6000.3.14f1"}},"GLOBAL_OR_CENTRAL_TEST")

    def test_08_assembly_definition_change_requires_full(self):
        p="Assets/RoboRougelikeMechanics/"+A+"/Runtime/RoboRougelike."+A+".asmdef"
        self.assert_full({"change":{p:json.dumps({"name":"RoboRougelike."+A,"references":["Foo"]})}},"TEST_ASSEMBLY_OR_DELETION")

    def test_09_assembly_meta_change_requires_full(self):
        p="Assets/RoboRougelikeMechanics/"+A+"/Runtime/RoboRougelike."+A+".asmdef.meta"
        self.assert_full({"change":{p:"guid: "+"a"*32}},"TEST_ASSEMBLY_OR_DELETION")

    def test_10_delete_test_source_fails_over_to_full(self):
        p="Assets/RoboRougelikeMechanics/"+A+"/Tests/EditMode/OriginalTests.cs"
        self.assert_full({"deleted":[p]},"TEST_ASSEMBLY_OR_DELETION")

    def test_11_rename_moving_source_between_modules_requires_full(self):
        a="Assets/RoboRougelikeMechanics/"+A+"/Runtime/Engine.cs"
        b="Assets/RoboRougelikeMechanics/"+B+"/Runtime/RenamedEngine.cs"
        self.assert_full({"renamed":[(a,b)]},"CROSS_MODULE_CHANGES")

    def test_12_deleted_playmode_assembly_requires_full(self):
        p="Assets/RoboRougelikeMechanics/"+A+"/Tests/PlayMode/RoboRougelike."+A+".PlayModeTests.asmdef"
        self.assert_full({"deleted":[p]},"TEST_ASSEMBLY_OR_DELETION")

    def test_13_missing_existing_playmode_test_source_requires_full(self):
        p="Assets/RoboRougelikeMechanics/"+A+"/Tests/PlayMode/OriginalTests.cs"
        self.assert_full({"deleted":[p]},"MISSING_MODULE_TEST_COVERAGE")

    def test_14_unknown_new_mov_module_has_no_tests_fail_closed(self):
        p="Assets/RoboRougelikeMechanics/MOV999NewEngine/Runtime/Engine.cs"
        self.assert_full({"change":{p:"class NewOne {}"}},"MISSING_TARGET_MODULE_TEST_INVENTORY")

    def test_15_changed_unknown_game_asset_name_requires_full(self):
        self.assert_full({"change":{"Assets/CustomGameplay/NewScript.cs":"class X {}"}},"UNSCOPED_GAME_ASSET")

    def test_16_docs_only_never_claims_game_G1(self):
        report,_,_=self.report(change={"Docs/README.md":"new documentation"})
        self.assertEqual(report["mode"],"SOURCE_ONLY_NO_GAME_ASSETS_CHANGED")
        self.assertEqual(report["game_g1"],"NOT_ASSESSED")
        self.assertFalse(report["selected_precheck_assemblies"])

    def test_17_tools_only_requires_own_ci_but_not_fake_native_execution(self):
        report,_,_=self.report(change={"Tools/B01/Other/Tests/run.py":"print('x')"})
        self.assertEqual(report["mode"],"SOURCE_ONLY_NO_GAME_ASSETS_CHANGED")
        self.assertIn("CHANGES_OUTSIDE_TRACKED_GAME_ASSETS_REQUIRE_OWN_SOURCE_CI",report["reasons"])
        self.assertEqual(report["native_unity"],"NOT_RUN")

    def test_18_unknown_project_root_change_requires_full(self):
        self.assert_full({"change":{"CustomBuildConfig.json":"{}"}},"UNCLASSIFIED_PROJECT_CHANGE")

    def test_19_original_commit_ancestor_is_hard_guard(self):
        self.write("Docs/README.md","changed")
        head=self.commit("new descendant")
        with self.assertRaisesRegex(gate.PlanError,"GIT_COMMAND_FAILED"):
            gate.changed_paths(self.repo,head,self.base)
        self.assertEqual(len(gate.changed_paths(self.repo,self.base,head)),1)

    def test_20_wrong_commit_sha_format_rejected(self):
        with self.assertRaisesRegex(gate.PlanError,"EXACT_40_HEX"):
            gate.inventory(self.repo,"main")
        with self.assertRaises(gate.PlanError):
            gate.inventory(self.repo,"f"*40)

    def test_21_impact_full_suite_is_a_plan_not_a_native_run(self):
        report,_,_=self.report(change={"Assets/Scenes/Test.unity":"scene"})
        self.assertEqual(report["release_gate"],
                         "INDEPENDENT_FULL_NATIVE_SUITE_ALWAYS_REQUIRED_FOR_G1")
        self.assertEqual(report["status"],"SOURCE_PLAN_GENERATED")
        self.assertEqual(report["native_unity"],"NOT_RUN")

    def test_22_actual_cli_inventory_from_original_real_git_objects(self):
        out=self.repo/"output"/"inventory.json"
        args=[sys.executable,str(Path(gate.__file__)),"--repo",str(self.repo),
              "--head-sha",self.base,"--inventory-only",
              "--expect-module-count","2","--out",str(out)]
        p=subprocess.run(args,capture_output=True,text=True)
        self.assertEqual(p.returncode,0,p.stdout+p.stderr)
        x=json.loads(out.read_text())
        self.assertEqual(x["mode"],"REAL_GAME_TEST_INVENTORY")
        self.assertEqual(x["module_test_file_count"],4)
        print("ACTUAL_CLI_PHYSICAL_GIT_TEST_INVENTORY_PASS_NOT_UNITY")

    def test_23_actual_cli_change_plan_and_no_G1(self):
        path="Assets/RoboRougelikeMechanics/"+A+"/Runtime/Engine.cs"
        self.write(path,"class NewChanged {}")
        head=self.commit("actual diff for CLI")
        out=self.repo/"output"/"plan.json"
        p=subprocess.run([sys.executable,str(Path(gate.__file__)),
                          "--repo",str(self.repo),"--base-sha",self.base,
                          "--head-sha",head,"--out",str(out)],capture_output=True,text=True)
        self.assertEqual(p.returncode,0,p.stdout+p.stderr)
        x=json.loads(out.read_text())
        self.assertEqual(x["mode"],"TARGETED_PRECHECK_PLUS_FULL_G1_LATER")
        self.assertEqual(len(x["selected_precheck_assemblies"]),2)
        self.assertEqual(x["native_unity"],"NOT_RUN")
        print("ACTUAL_CLI_PHYSICAL_GIT_DIFF_TARGETED_TWO_ASSEMBLIES_PASS")

    def test_24_actual_cli_count_drift_is_nonzero(self):
        p=subprocess.run([sys.executable,str(Path(gate.__file__)),
                          "--repo",str(self.repo),"--head-sha",self.base,
                          "--inventory-only","--expect-module-count","9"],
                         capture_output=True,text=True)
        self.assertEqual(p.returncode,2,p.stdout+p.stderr)
        self.assertEqual(json.loads(p.stdout)["mode"],"FAIL_CLOSED")
        print("ACTUAL_CLI_INVENTORY_DRIFT_EXIT2")



    def test_25_native_repository_ci01_change_forces_full(self):
        self.assert_full({"change":{".github/workflows/ci-01-repository.yml":"name: changed"}}, "CORE_UNITY_CI_GATE_CHANGED")

    def test_26_native_editmode_ci02_change_forces_full(self):
        self.assert_full({"change":{".github/workflows/ci-02-editmode.yml":"name: disabled"}}, "CORE_UNITY_CI_GATE_CHANGED")

    def test_27_native_playmode_ci03_change_forces_full(self):
        self.assert_full({"change":{".github/workflows/ci-03-playmode.yml":"name: disabled"}}, "CORE_UNITY_CI_GATE_CHANGED")

    def test_28_native_player_build_ci04_change_forces_full(self):
        self.assert_full({"change":{".github/workflows/ci-04-build.yml":"name: disabled"}}, "CORE_UNITY_CI_GATE_CHANGED")

    def test_29_original_mov_script_meta_guid_change_forces_full(self):
        p="Assets/RoboRougelikeMechanics/"+A+"/Runtime/Engine.cs.meta"
        self.assert_full({"change":{p:"fileFormatVersion: 2\nguid: "+"f"*32}}, "TEST_ASSEMBLY_OR_DELETION_CHANGE")

    def test_30_original_mov_shader_asset_change_forces_full(self):
        p="Assets/RoboRougelikeMechanics/"+A+"/Runtime/Vfx.shader"
        self.assert_full({"change":{p:'Shader "NewShader" {}'}}, "MOV_NON_CSHARP_ASSET_CHANGE")

    def test_31_original_mov_asmref_change_forces_full(self):
        p="Assets/RoboRougelikeMechanics/"+A+"/Runtime/External.asmref"
        self.assert_full({"change":{p:'{"reference":"GUID:abcd"}'}}, "TEST_ASSEMBLY_OR_DELETION_CHANGE")

    def test_32_original_same_module_runtime_rename_forces_full(self):
        p="Assets/RoboRougelikeMechanics/"+A+"/Runtime/Engine.cs"
        to="Assets/RoboRougelikeMechanics/"+A+"/Runtime/EngineRenamed.cs"
        self.assert_full({"renamed":[(p,to)]}, "MOV_FILE_REMOVAL_OR_RENAME")

    def test_33_original_same_module_runtime_deletion_forces_full(self):
        p="Assets/RoboRougelikeMechanics/"+A+"/Runtime/Engine.cs"
        self.assert_full({"deleted":[p]}, "MOV_FILE_REMOVAL_OR_RENAME")

    def test_34_unrelated_dedicated_workflow_remains_source_only(self):
        report,_,_=self.report(change={".github/workflows/b01-41-noncanonical.yml":"name: test"})
        self.assertEqual(report["mode"],"SOURCE_ONLY_NO_GAME_ASSETS_CHANGED")
        self.assertEqual(report["game_g1"],"NOT_ASSESSED")

if __name__=="__main__":
    unittest.main(verbosity=2)
