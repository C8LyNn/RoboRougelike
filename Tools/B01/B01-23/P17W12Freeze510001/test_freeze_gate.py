import io,sys,tempfile,unittest,zipfile
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import freeze_gate as f
class Frozen510(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.raw=f.reproduce();cls.paths=f.files()
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory(prefix="w12-freeze-510-")
  self.root=Path(self.tmp.name)/"staged";self.root.mkdir()
  with zipfile.ZipFile(io.BytesIO(self.raw)) as z:
   for p in self.paths:
    target=self.root/p;target.parent.mkdir(parents=True,exist_ok=True)
    target.write_bytes(z.read(p))
 def tearDown(self):self.tmp.cleanup()
 def extra(self,p,value):
  target=self.root/p;target.parent.mkdir(parents=True,exist_ok=True)
  target.write_text(value);return target
 def failscan(self,reason):
  with self.assertRaisesRegex(ValueError,reason):f.receiver_scan(self.root)
 def test_01_original_zip_exact_bytes(self):self.assertEqual(len(self.raw),99666)
 def test_02_original_zip_full_manifest(self):self.assertTrue(f.verify_archive(self.raw))
 def test_03_frozen_original_34_git_sources(self):self.assertEqual(len(self.paths),34)
 def test_04_unique_unity_guids_and_asmdef(self):self.assertEqual(len(f.source_check()),34)
 def test_05_selected_receiver_positive(self):self.assertGreaterEqual(f.receiver_scan(self.root),34)
 def test_06_v11_source_code_rejected(self):
  p=self.root/f.ASSET/"Runtime/SecureDebrisPlaygroundV2.cs"
  p.write_bytes(p.read_bytes()+b"// alternate")
  self.failscan("ALTERNATIVE_OR_MUTATED_SOURCE")
 def test_07_old_v10_source_rejected(self):
  p=self.root/f.ASSET/"Runtime/SecureDockPolicy.cs"
  p.write_bytes(f.original(f.ASSET+"/Runtime/SecureDockPolicy.cs",f.OLD))
  self.failscan("ALTERNATIVE_OR_MUTATED_SOURCE")
 def test_08_511_extra_tool_rejected(self):
  self.extra("Tools/B01/B01-23/P17W11SecureV2RearmAtomicity/Program.cs","test")
  self.failscan("LEGACY_OR_ALTERNATIVE_INSTALLED")
 def test_09_494_legacy_source_rejected(self):
  self.extra("Assets/B0123DockHardening001/Runtime/HardenedDockDemo.cs","test")
  self.failscan("LEGACY_OR_ALTERNATIVE_INSTALLED")
 def test_10_w07_source_rejected(self):
  self.extra("Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs","test")
  self.failscan("LEGACY_OR_ALTERNATIVE_INSTALLED")
 def test_11_w08_source_rejected(self):
  self.extra("Assets/B0123ReattachClearance001/Runtime/DebrisSafeReattachGate.cs","test")
  self.failscan("LEGACY_OR_ALTERNATIVE_INSTALLED")
 def test_12_w07_scene_guid_rejected(self):
  self.extra("Assets/World/Arena.unity","m_Script: {guid: "+f.LEGACY_GUIDS[0]+"}")
  self.failscan("LEGACY_SCRIPT_GUID")
 def test_13_w08_prefab_guid_rejected(self):
  self.extra("Assets/Robots/Player.prefab","m_Script: {guid: "+f.LEGACY_GUIDS[1]+"}")
  self.failscan("LEGACY_SCRIPT_GUID")
 def test_14_w09_asset_guid_rejected(self):
  self.extra("Assets/Robots/Setup.asset","m_Script: {guid: "+f.LEGACY_GUIDS[2]+"}")
  self.failscan("LEGACY_SCRIPT_GUID")
 def test_15_old_external_public_type_rejected(self):
  self.extra("Assets/Host/OldController.cs","public class Old { DebrisRigPlayground p; }")
  self.failscan("LEGACY_HOST_CSHARP_ROUTE")
 def test_16_valid_new_v2_host_ref_allowed(self):
  self.extra("Assets/Host/NewController.cs","public class New { B01.SecureReattachV2.SecureDebrisPlaygroundV2 p; }")
  self.assertGreater(f.receiver_scan(self.root),34)
 def test_17_unowned_extra_inside_package_rejected(self):
  self.extra(f.ASSET+"/Runtime/AltSource.cs","test")
  self.failscan("UNOWNED_FILE_INSIDE_SELECTED_PACKAGE")
 def test_18_missing_selected_file_rejected(self):
  (self.root/self.paths[0]).unlink();self.failscan("SELECTED_SOURCE_MISSING")
 def test_19_corrupt_physical_zip_rejected(self):
  x=bytearray(self.raw);x[30]^=1
  with self.assertRaisesRegex(ValueError,"NOT_FROZEN_510_SOURCE_INNER_ZIP"):f.verify_archive(bytes(x))
 def test_20_rejected_foreign_file_is_not_deleted(self):
  p=self.extra("Assets/Host/Unsafe.cs","HardenedDockDemo")
  self.failscan("LEGACY_HOST_CSHARP_ROUTE")
  self.assertEqual(p.read_text(),"HardenedDockDemo")
if __name__=="__main__":unittest.main(verbosity=2)
