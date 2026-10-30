#!/usr/bin/env python3
"""17 source-only negative controls; never claim this unit test ran Unity or C#."""
from pathlib import Path
import sys
import unittest
sys.path.insert(0,str(Path(__file__).resolve().parent))
import source_guard as g
ROOT=Path(__file__).resolve().parents[4]
PROGRAM=(ROOT/(g.OWN_BASE+"Program.cs")).read_text()
CSPROJ=(ROOT/(g.OWN_BASE+"Original712SessionLeaseQA.csproj")).read_text()
class Safeguards(unittest.TestCase):
    def reject(self,x):
        with self.assertRaises(g.Denied):g.inspect(x,CSPROJ)
    def test_01_original_worker_control_positive(self):
        self.assertGreaterEqual(g.inspect(PROGRAM,CSPROJ),18)
    def test_02_sha_trust_selection(self):
        self.assertEqual(g.COMMITS["R2_712"],"bd8905ad549a7b26854b671be971f7d781308311")
        self.assertEqual(g.COMMITS["API_V2_691"],"809f82575c638e1b4d2806b5f417bddab6c0871a")
        self.assertEqual(g.COMMITS["STORAGE_669"],"d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121")
    def test_03_exact_7_ci41_new_files(self):
        self.assertEqual(len(g.FILES),7)
    def test_04_reject_original_r2_fake_copy(self):
        self.reject(PROGRAM+"\nclass CanonicalSlotSessionR2 {}")
    def test_05_reject_original_store_fake_copy(self):
        self.reject(PROGRAM+"\nclass SingleFileSaveSlot {}")
    def test_06_reject_original_gateway_fake_copy(self):
        self.reject(PROGRAM+"\nclass CanonicalWorldGateway {}")
    def test_07_reject_no_real_child_process(self):
        self.reject(PROGRAM.replace("Process.Start(psi)","FakeSpawn(psi)"))
    def test_08_reject_no_start_barrier(self):
        self.reject(PROGRAM.replace('File.WriteAllText(go,"START")','Thread.Sleep(5)'))
    def test_09_reject_wrong_two_process_wait(self):
        self.reject(PROGRAM.replace("a.WaitForExit(25000)&&b.WaitForExit(25000)",
                                    "a.WaitForExit(25000)"))
    def test_10_reject_no_dual_both_busy_test(self):
        self.reject(PROGRAM.replace('first=="BUSY"&&second=="BUSY"',
                                    'first=="BUSY"||second=="BUSY"'))
    def test_11_reject_no_exact_one_winner_test(self):
        self.reject(PROGRAM.replace('first=="ACCEPTED"&&second=="BUSY"',
                                    'first=="ACCEPTED"||second=="BUSY"'))
    def test_12_reject_no_failed_cleanup_source(self):
        self.reject(PROGRAM.replace("ReturnFalse","AcceptedEvenThoughOrphan"))
    def test_13_reject_no_throwing_cleanup_source(self):
        self.reject(PROGRAM.replace("ThrowCleanup","FakeSuccessCleanup"))
    def test_14_reject_no_repeat_dispose(self):
        self.reject(PROGRAM.replace("owner.Dispose();owner.Dispose();","owner.Dispose();"))
    def test_15_reject_bypassed_retirement_proof(self):
        self.reject(PROGRAM.replace("TryCloseAfterPhysicalRetirement(()=>false)",
                                    "TryCloseAfterPhysicalRetirement(()=>true)"))
    def test_16_reject_user_player_checkpoint_path(self):
        self.reject(PROGRAM.replace("Path.GetTempPath()",
                                    "Application.persistentDataPath"))
    def test_17_reject_relaxed_compiler_or_missing_frozen_source(self):
        with self.assertRaises(g.Denied):
            g.inspect(PROGRAM,CSPROJ.replace("<TreatWarningsAsErrors>true",
                                             "<TreatWarningsAsErrors>false"))
    def test_18_reject_omitted_real_669_storage(self):
        with self.assertRaises(g.Denied):
            g.inspect(PROGRAM,CSPROJ.replace("SingleFileSaveSlot.cs","MockSaveSlot.cs"))
if __name__=="__main__":unittest.main(verbosity=2)
