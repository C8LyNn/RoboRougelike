#!/usr/bin/env python3
"""17 independently authored hostile controls for selected ORIGINAL V2 gateway QA."""
import pathlib
import sys
import unittest
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
import source_guard as g
root=pathlib.Path(__file__).resolve().parents[4]
program=(root/(g.BASE+"Program.cs")).read_text()
project=(root/(g.BASE+"Original691RealSlotQA.csproj")).read_text()

class GenuineOriginalGateTests(unittest.TestCase):
    def fails(self,src,which):
        with self.assertRaisesRegex(g.Denied,which):
            g.check_program(src)
    def test_01_positive_original_real_gateway_source(self):
        self.assertEqual(g.check_program(program),22)
        print("W21_EXACT_ORIGINAL_691_GATEWAY_AND_647_FILESTORE_QA_SOURCE_VERIFIED")
    def test_02_compiler_origins_are_all_direct_original(self):
        g.check_project(project)
    def test_03_exact_owner_only_api_selection(self):
        self.assertEqual(g.ROOTS["api_v1_681"],"eed2e39636edfd8b8db754c426db452164cbf9d7")
        self.assertEqual(g.ROOTS["api_v2_691"],"809f82575c638e1b4d2806b5f417bddab6c0871a")
        self.assertEqual(g.ROOTS["host_r4_647"],"09c1809b73324a6d31c13e5d0143de62ac1e3ca6")
    def test_04_own_7_file_change_scope_only(self):
        self.assertEqual(len(g.FILES),7)
        self.assertTrue(all(p.startswith(g.BASE) or
          p==".github/workflows/b01-41-p17w21-original691-real-slot-qa.yml"
          for p in g.FILES))
    def test_05_deny_copied_fake_gateway_implementation(self):
        self.fails(program+"\nclass CanonicalWorldGateway {}", "FORBIDDEN_LOCAL")
    def test_06_deny_copied_fake_checkpoint_implementation(self):
        self.fails(program+"\nclass FileCheckpointStore {}", "FORBIDDEN_LOCAL")
    def test_07_deny_fake_or_removed_original_file_commit(self):
        self.fails(program.replace("new FileCheckpointStore(file)","new FakeMemoryStore(file)"),
                   "MISSING_REAL_ORIGINAL")
    def test_08_deny_deleted_partial_prepare_fault(self):
        self.fails(program.replace("PrepareThrowsAfterAlloc","NeverThrowOnPrepare"),
                   "MISSING_REAL_ORIGINAL")
    def test_09_deny_deleted_false_cleanup_quarantine(self):
        self.fails(program.replace("CleanupReturnsFalse","AlwaysTrueCleanup"),
                   "MISSING_REAL_ORIGINAL")
    def test_10_deny_deleted_throwing_cleanup_case(self):
        self.fails(program.replace("CleanupThrows","AlwaysSuccessCleanup"),
                   "MISSING_REAL_ORIGINAL")
    def test_11_deny_deleted_lost_durable_ack_fault(self):
        self.fails(program.replace("ThrowAfterDurable","ConfirmWriteAlways"),
                   "MISSING_REAL_ORIGINAL")
    def test_12_deny_fake_in_process_threads_instead_of_os_processes(self):
        self.fails(program.replace("Process.Start(info)","Thread.Start(info)"),
                   "MISSING_REAL_ORIGINAL")
    def test_13_deny_sequential_wait_instead_of_actual_race(self):
        self.fails(program.replace("a.WaitForExit(25000)&&b.WaitForExit(25000)",
                         "a.WaitForExit(25000)"),"MISSING_REAL_ORIGINAL")
    def test_14_deny_dual_winners_or_no_one_winner_rule(self):
        self.fails(program.replace('(ra=="Accepted")!=(rb=="Accepted")',
                                   '(ra=="Accepted")||(rb=="Accepted")'),
                   "MISSING_REAL_ORIGINAL")
    def test_15_deny_reduced_real_process_stress(self):
        self.fails(program.replace("i<8","i<1"),"MISSING_REAL_ORIGINAL")
    def test_16_deny_touching_real_unity_player_checkpoint(self):
        self.fails(program.replace("Path.GetTempPath()","Application.persistentDataPath"),
                   "UNSAFE_PRIVATE")
    def test_17_deny_non_original_CSharp_compiler_closure(self):
        with self.assertRaisesRegex(g.Denied,"MISSING_EXACT_ORIGINAL"):
            g.check_project(project.replace(
              "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs",
              "Assets/B0123CanonicalSaveApi001/Core/MockGateway.cs"))
    def test_18_deny_non_strict_compiler(self):
        with self.assertRaisesRegex(g.Denied,"MUST_COMPILE_REAL"):
            g.check_project(project.replace("<TreatWarningsAsErrors>true",
                                            "<TreatWarningsAsErrors>false"))

if __name__=="__main__":unittest.main(verbosity=2)
