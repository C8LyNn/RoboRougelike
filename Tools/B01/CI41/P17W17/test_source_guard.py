#!/usr/bin/env python3
"""12 genuine source-control cases for the original final #596 native fixture isolation."""
import pathlib
import sys
import unittest

sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
import source_guard as g
REPO=pathlib.Path(__file__).resolve().parents[4]
HOST=(REPO/g.HOST).read_text(encoding="utf-8")
NATIVE=(REPO/g.PLAY).read_text(encoding="utf-8")

class RealOriginalSelected596FixtureQA(unittest.TestCase):
    def expect_host(self,source,why):
        with self.assertRaisesRegex(g.Refuse,why):
            g.validate_host_source(source)
    def expect_native(self,source,why):
        with self.assertRaisesRegex(g.Refuse,why):
            g.validate_original_native_source(source)

    def test_01_actual_original_596_host_has_shared_global_file(self):
        g.validate_host_source(HOST)
        self.assertIn("b0123_one_save_host_v2.checkpoint",HOST)
        print("ORIGINAL_596_UNITY_PLAYMODE_SHARED_GLOBAL_FILE_SOURCE_RISK_REPRODUCED")

    def test_02_actual_four_unity_tests_have_three_separate_runs(self):
        self.assertEqual(g.validate_original_native_source(NATIVE),{9,31,41})
        print("ORIGINAL_596_FOUR_NATIVE_CASES_USE_31_41_9_SHARED_SLOT_NO_LIFECYCLE")

    def test_03_no_unsafe_main_branch_mutation_needed(self):
        self.assertEqual(len(g.OWNED),6)
        self.assertNotIn(g.HOST,g.OWNED)
        self.assertNotIn(g.PLAY,g.OWNED)
        self.assertNotIn(g.CORE,g.OWNED)

    def test_04_reject_replaced_original_slot_name(self):
        self.expect_host(HOST.replace("b0123_one_save_host_v2.checkpoint",
                    "new-slot-per-case.checkpoint"),"SHARED_FIXED_NATIVE_SAVE_SLOT")

    def test_05_reject_removed_global_persistent_path(self):
        self.expect_host(HOST.replace("Application.persistentDataPath",
                    "System.IO.Path.GetTempPath()"),"SHARED_FIXED_NATIVE_SAVE_SLOT")

    def test_06_reject_absent_original_FileCheckpointStore(self):
        self.expect_host(HOST.replace("new FileCheckpointStore(sidecar)",
                    "new MemoryCheckpointStore()"),"NATIVE_HOST_DOES_NOT_USE_REAL")

    def test_07_reject_new_slot_interface_without_independent_reaudit(self):
        self.expect_host(HOST+" \n // UsePersistentCheckpointSlot\n",
                         "NEW_NATIVE_SLOT_SELECTION_API")

    def test_08_reject_absent_Awake(self):
        self.expect_host(HOST.replace("private void Awake()","private void AwakeOriginal()"),
                         "SHARED_FILE_CREATED_OUTSIDE_ORIGINAL_AWAKE")

    def test_09_reject_one_original_native_case_removed(self):
        self.expect_native(NATIVE.replace("public IEnumerator ColdRestart_",
                        "public System.Collections.IEnumerator ColdRestart_",1),
                        "SELECTED_596_ORIGINAL_NATIVE_CASE_INVENTORY")

    def test_10_reject_native_run_id_drift(self):
        self.expect_native(NATIVE.replace("MakeFixture(41,4",
                        "MakeFixture(66,4"),"ORIGINAL_NATIVE_TEST_RUN_ID_FIXTURES_MOVED")

    def test_11_reject_native_already_cleaning_shared_source_file(self):
        self.expect_native(NATIVE+"\n // File.Delete(sharedSidecar)\n",
                         "ORIGINAL_NATIVE_TEST_HAS_NEW_FILE_LIFECYCLE_REVIEW")

    def test_12_verify_our_real_original_CSharp_run_is_disposable_only(self):
        s=(REPO/"Tools/B01/CI41/P17W17/Program.cs").read_text()
        self.assertIn("new FileCheckpointStore(path)",s)
        self.assertIn("Guid.NewGuid().ToString(\"N\")",s)
        self.assertIn("Directory.Delete(root,true)",s)
        self.assertNotIn("Application.persistentDataPath",s.replace(
            "// Only writes disposable own GUID-named OS directory; NEVER real Unity persistentDataPath.",""))
        self.assertIn("NATIVE_UNITY_G1_NOT_RUN_SOURCE_PROCESS_ONLY",s)

if __name__=="__main__":unittest.main(verbosity=2)
