#!/usr/bin/env python3
"""22 source-only tests of a REAL Unity PlayMode negative-case suite. G1 NOT_RUN."""
import copy
import json
from pathlib import Path
import sys
import unittest

sys.path.insert(0,str(Path(__file__).resolve().parent))
import native_suite_guard as g

REPO=Path(__file__).resolve().parents[4]
SOURCE=(REPO/g.NATIVE).read_text(encoding="utf-8")
ASM=json.loads((REPO/g.ASM).read_text(encoding="utf-8"))
FAKE_METAS={p:"guid: b0141"+str(i+1).zfill(28)+"\n" for i,p in
    enumerate(sorted([g.ROOT+".meta",g.ROOT+"/Tests.meta",g.ASM+".meta",g.NATIVE+".meta"]))}

class NativeFailureInventory(unittest.TestCase):
    def bad_source(self,text,reason):
        with self.assertRaisesRegex(g.Refuse,reason):
            g.source_methods(text)

    def test_01_four_new_native_playmode_tests_parsed(self):
        x=g.source_methods(SOURCE)
        self.assertEqual(set(x),set(g.EXPECTED))
        self.assertEqual(len(x),4)
        print("FOUR_REAL_UNITY_PLAYMODE_NEGATIVE_SOURCE_CASES_AUTHORED_G1_NOT_RUN")

    def test_02_true_original_test_asmdef_importable(self):
        g.assembly_ok(ASM)

    def test_03_four_unique_new_unity_guids(self):
        self.assertEqual(g.validate_guid_entries(FAKE_METAS),4)

    def test_04_reject_missing_one_native_case(self):
        self.bad_source(SOURCE.replace(
            "public IEnumerator ForeignRunSameFence_",
            "public IEnumerable ForeignRunSameFence_",1),"NATIVE_CASE_INVENTORY")

    def test_05_reject_added_dummy_native_test(self):
        x=SOURCE.replace("    public sealed class SaveBridgeFailureNativeTests",
                         "    public sealed class SaveBridgeFailureNativeTests",1)
        x=x.replace("        private static string Slot()", 
             "        [UnityTest] public IEnumerator FakePass(){ yield return null; }\n"
             "        private static string Slot()",1)
        self.bad_source(x,"NATIVE_CASE_INVENTORY")

    def test_06_reject_renamed_required_case(self):
        self.bad_source(SOURCE.replace(
            "CorruptPersistentFence_QuarantinesFreshHostBeforePhysicalPublication",
            "CorruptPersistentFence_NoActualTest",1),"NATIVE_CASES_MISMATCH")

    def test_07_reject_ignore_native_test(self):
        self.bad_source(SOURCE.replace("[UnityTest]","[Ignore]\n        [UnityTest]",1),
                        "DISABLED_NATIVE_TEST")

    def test_08_reject_explicit_native_test(self):
        self.bad_source(SOURCE.replace("[UnityTest]","[Explicit]\n        [UnityTest]",1),
                        "DISABLED_NATIVE_TEST")

    def test_09_reject_compiler_exclusion(self):
        self.bad_source("#if UNITY_EDITOR\n"+SOURCE,"DISABLED_NATIVE_TEST")

    def test_10_reject_fake_nunit_without_original_import(self):
        self.bad_source(SOURCE.replace("using NUnit.Framework;",""),
                        "MISSING_REAL_UNITY_TEST_IMPORT")

    def test_11_reject_incorrect_namespace(self):
        self.bad_source(SOURCE.replace("namespace B01.CI41.NativeSaveQA",
                         "namespace B01.Attacker"),"WRONG_NATIVE_NAMESPACE")

    def test_12_reject_missing_corrupted_fence_disk_mutation(self):
        self.bad_source(SOURCE.replace("File.WriteAllBytes(fence,physical)",
                          "File.ReadAllBytes(fence)",1),"MISSING_NEGATIVE_ASSERTION")

    def test_13_reject_missing_post_corrupt_quarantine_assertion(self):
        self.bad_source(SOURCE.replace("Assert.IsTrue(fresh.Quarantined)",
                                      "Assert.IsFalse(fresh.Quarantined)",1),
                        "MISSING_NEGATIVE_ASSERTION")

    def test_14_reject_no_native_rigidbody_absence_assertion(self):
        self.bad_source(SOURCE.replace(
            "Assert.AreEqual(0,freshGo.GetComponentsInChildren<Rigidbody>(true).Length)",
            "Assert.AreEqual(0,0)",1),"MISSING_NEGATIVE_ASSERTION")

    def test_15_reject_missing_cleanup(self):
        self.bad_source(SOURCE.replace("if(File.Exists(fence))File.Delete(fence);",
                          "if(File.Exists(fence))File.ReadAllBytes(fence);",1),
                        "MISSING_NATIVE_LIFECYCLE_OR_OWNED_CLEANUP")

    def test_16_reject_test_without_genuine_unity_host(self):
        self.bad_source(SOURCE.replace(
            "AddComponent<IsolatedTwinRigSaveHost>",
            "AddComponent<OtherHost>"),"NO_REAL_UNITY_HOST_IN_NATIVE_TEST")

    def test_17_reject_private_reflection(self):
        self.bad_source(SOURCE.replace(
            "private static string Slot()", 
            "private static string Fake(){ return typeof(object).GetField(\"x\").ToString(); }\n"
            "        private static string Slot()",1),
            "UNAUTHORIZED_PRIVATE_GAME_HOOK")

    def test_18_reject_asmdef_runtime_wrong_reference(self):
        bad=copy.deepcopy(ASM)
        bad["references"][1]="B0123.Unrelated.Unity"
        with self.assertRaisesRegex(g.Refuse,"WRONG_NATIVE_TEST_ASSEMBLY_DEPENDENCIES"):
            g.assembly_ok(bad)

    def test_19_reject_asmdef_without_editor_test_runner(self):
        bad=copy.deepcopy(ASM)
        bad["includePlatforms"]=[]
        with self.assertRaisesRegex(g.Refuse,"NATIVE_ASMDEF_NOT_TEST_RUNNER_IMPORTABLE"):
            g.assembly_ok(bad)

    def test_20_reject_asmdef_without_nunit(self):
        bad=copy.deepcopy(ASM)
        bad["precompiledReferences"]=[]
        with self.assertRaisesRegex(g.Refuse,"NATIVE_ASMDEF_NOT_TEST_RUNNER_IMPORTABLE"):
            g.assembly_ok(bad)

    def test_21_reject_duplicate_unity_asset_guid(self):
        bad=copy.deepcopy(FAKE_METAS)
        names=list(bad)
        bad[names[1]]=bad[names[0]]
        with self.assertRaisesRegex(g.Refuse,"COLLIDING_UNITY_GUID"):
            g.validate_guid_entries(bad)

    def test_22_reject_missing_meta_guid(self):
        bad=copy.deepcopy(FAKE_METAS)
        bad[next(iter(bad))]="fileFormatVersion: 2\n"
        with self.assertRaisesRegex(g.Refuse,"MISSING_DUPLICATE_META_GUID"):
            g.validate_guid_entries(bad)

if __name__=="__main__":
    unittest.main(verbosity=2)
