#!/usr/bin/env python3
"""18 deliberate source-harness tampering/anti-mock cases, not native Unity."""
import unittest,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import source_guard as g
root=Path(__file__).resolve().parents[4]
src=(root/(g.P+"Program.cs")).read_text()
csproj=(root/(g.P+"Original773CasefoldOSQA.csproj")).read_text()
class SourceIntegrity(unittest.TestCase):
 def denied(self,key,replacement):
  self.assertIn(key,src)
  with self.assertRaises(AssertionError):g.validate_program(src.replace(key,replacement))
 def test_01_original_source_positive(self):self.assertGreaterEqual(g.validate_program(src),20)
 def test_02_original_compiler_positive(self):g.project(csproj)
 def test_03_one_selected_749_exact(self):self.assertEqual(g.ORIG,"3aaaec4027ee246fbfb6f209179f0e9507363473")
 def test_04_orig_669_blob(self):self.assertEqual(g.STOR_BLOB,"db7745635164d9eb6b10f3246538700158130b7b")
 def test_05_own_only_seven(self):self.assertEqual(len(g.OWN),7)
 def test_06_deny_fake_session(self):
  with self.assertRaises(AssertionError):g.validate_program(src+"\nclass CanonicalSlotSessionV3 {}")
 def test_07_deny_test_without_original_session(self):self.denied("CanonicalSlotSessionV3R2.TryOpen","MockSession.TryOpen")
 def test_08_deny_missing_old_669(self):self.denied("Legacy.TryOpenExisting","FakeSlot.TryOpenExisting")
 def test_09_deny_replaced_real_CLR(self):self.denied("Process.Start(psi)","new Thread(()=>{})")
 def test_10_deny_missing_race_barrier(self):self.denied('File.WriteAllText(start,"GO")',"Thread.Sleep(1)")
 def test_11_deny_missing_dual_process_exit(self):self.denied("a.WaitForExit(25000)&&b.WaitForExit(25000)","a.WaitForExit(25000)")
 def test_12_deny_raw_dispose_bypass(self):self.denied("session.Dispose();session.Dispose();","session.Dispose();")
 def test_13_deny_false_physical_proof(self):self.denied("session.TryCloseAfterPhysicalRetirement(()=>false)","session.TryCloseAfterPhysicalRetirement(()=>true)")
 def test_14_deny_unchecked_one_owner(self):self.denied('Directory.GetFiles(authorityDir,"*.owner").Length==1',"true")
 def test_15_deny_legacy_double_lock(self):self.denied('Directory.GetFiles(root,"*.lease",SearchOption.AllDirectories).Length==0',"true")
 def test_16_deny_real_crossprocess_stress(self):self.denied("pairs==13&&workers==26","pairs>0")
 def test_17_deny_actual_private_player_save(self):
  with self.assertRaises(AssertionError):g.validate_program(src+"\nApplication.persistentDataPath")
 def test_18_deny_missing_original_csharp_compilation(self):
  with self.assertRaises(AssertionError):g.project(csproj.replace("<TreatWarningsAsErrors>true","<TreatWarningsAsErrors>false"))
if __name__=="__main__":unittest.main(verbosity=2)
