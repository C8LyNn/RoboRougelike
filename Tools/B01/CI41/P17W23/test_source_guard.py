#!/usr/bin/env python3
"""18 independent hostile checks on CI41 original source integration-witness controls."""
import sys,unittest
from pathlib import Path
sys.path.insert(0,str(Path(__file__).parent))
import source_guard as g
root=Path(__file__).resolve().parents[4]
program=(root/(g.BASE+"Program.cs")).read_text()
proj=(root/(g.BASE+"OriginalV3StorageAuthorityQA.csproj")).read_text()
class GuardTests(unittest.TestCase):
 def deny(self,old,new):
    self.assertIn(old,program)
    with self.assertRaises(AssertionError):g.check_program(program.replace(old,new))
 def test_01_valid_source_guard(self):
    self.assertGreaterEqual(g.check_program(program),16)
 def test_02_valid_original_csproj(self):g.check_project(proj)
 def test_03_exact_one_v3(self):
    self.assertEqual(g.OWNER,"b8ca29c7707cd6b4338d0b9772d74dee676d49d2")
 def test_04_exact_one_store(self):
    self.assertEqual(g.SAVE,"d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121")
 def test_05_exact_only_seven_paths(self):self.assertEqual(len(g.OWN),7)
 def test_06_deny_fake_original_gateway(self):
    self.deny("new CanonicalWorldGateway(","new FakeGateway(")
 def test_07_deny_reimplementation(self):
    with self.assertRaises(AssertionError):
      g.check_program(program+"\nclass SingleFileSaveSlot {}")
 def test_08_deny_no_actual_storage(self):
    self.deny("SingleFileSaveSlot.TryCreateNew","FakeStore.TryCreateNew")
 def test_09_deny_no_actual_v3_owner(self):
    self.deny("CanonicalWorldSlotLease.TryAcquire","FakeLease.TryAcquire")
 def test_10_deny_no_store_dispose_bypass(self):
    self.deny("store.Dispose();","/* missing independent storage release */")
 def test_11_deny_no_live_v3_world_assertion(self):
    self.deny("gateway.AuthorityActive&&factory.Last!=null&&factory.Last.Active","true")
 def test_12_deny_no_two_process_barrier(self):
    self.deny('File.WriteAllText(start,"GO")','Thread.Sleep(1)')
 def test_13_deny_no_two_real_external_clr(self):
    self.deny("Process.Start(psi)","new Thread(()=>{})")
 def test_14_deny_no_both_busy_expected(self):
    self.deny('ra=="BUSY"&&rb=="BUSY"','ra=="BUSY"||rb=="BUSY"')
 def test_15_deny_no_original_heavy_ids(self):
    self.deny("CheckIds(actual,1,false,true)","CheckIds(actual,1,true,false)")
 def test_16_deny_no_original_light_ids(self):
    self.deny("CheckIds(oldSnapshot,10,true,false)","CheckIds(oldSnapshot,10,false,true)")
 def test_17_deny_user_private_save_access(self):
    with self.assertRaises(AssertionError):
      g.check_program(program+"\nApplication.persistentDataPath")
 def test_18_deny_compiler_warning_relax(self):
    with self.assertRaises(AssertionError):
      g.check_project(proj.replace("<TreatWarningsAsErrors>true","<TreatWarningsAsErrors>false"))
if __name__=="__main__":unittest.main(verbosity=2)
