import unittest
from unittest.mock import patch
import audit
class W34SameWriterOriginalDataIntegrity(unittest.TestCase):
 def test_01_all_original_inputs_byte_pinned(self):
  self.assertEqual(len(audit.PINS),6)
  for sha,path,oid in audit.PINS.values():
   self.assertEqual(len(sha),40)
   self.assertEqual(len(oid),40)
   self.assertTrue(path.startswith(("Tools/","Assets/")))
 def test_02_git_real_blob_object(self):
  self.assertEqual(audit.blob(b"hello\n"),"ce013625030ba8dba906f756967f9e9ca394464a")
 def test_03_original_production_semantic_api(self):
  x=("private bool Semantic(byte[] world,XX"
     "CanonicalR3WorldCodecV1.TryDecode;"
     "dynamicWorld.Parts.Count!=econ.Parts.Count;"
     "economicIds.TryAdd(p.Id,p.Hp);"
     "economicIds.TryGetValue(p.Id,out float hp)||hp!=p.Hp;"
     "private static bool SameEconomicSection(")
  self.assertTrue(audit.verify_semantic(x))
 def test_04_kind_category_fix_changes_oracle(self):
  x=("private bool Semantic(byte[] world,XX"
     "CanonicalR3WorldCodecV1.TryDecode;"
     "dynamicWorld.Parts.Count!=econ.Parts.Count;"
     "economicIds.TryAdd(p.Id,p.Hp);"
     "economicIds.TryGetValue(p.Id,out float hp)||hp!=p.Hp;"
     "Category=p.Kind;" "private static bool SameEconomicSection(")
  with self.assertRaises(AssertionError):audit.verify_semantic(x)
 def test_05_no_silent_weakening_id_and_hp(self):
  x=("private bool Semantic(byte[] world,XX"
     "CanonicalR3WorldCodecV1.TryDecode;"
     "dynamicWorld.Parts.Count!=econ.Parts.Count;"
     "economicIds.TryAdd(p.Id,p.Hp);"
     "private static bool SameEconomicSection(")
  with self.assertRaises(AssertionError):audit.verify_semantic(x)
 def test_06_only_one_added_csharp_program_in_disposable_project(self):
  project='<Project><PropertyGroup><OutputType>Exe</OutputType></PropertyGroup><ItemGroup><Compile Include="Program.cs"/></ItemGroup></Project>'
  x=audit.make_project(project)
  self.assertIn("<StartupObject>CI41IndependentQA</StartupObject>",x)
  self.assertEqual(x.count("IndependentQA.cs"),1)
  self.assertEqual(x.count('<Compile Include="Program.cs"/>'),1)
 def test_07_absent_original_startup_does_not_silently_succeed(self):
  with self.assertRaises(AssertionError):audit.make_project('<Project/>')
 def test_08_original_git_bytes_tamper_is_rejected(self):
  with patch("audit.git",return_value=b"not original"):
   with self.assertRaises(AssertionError):audit.read("store")
if __name__=="__main__":unittest.main()
