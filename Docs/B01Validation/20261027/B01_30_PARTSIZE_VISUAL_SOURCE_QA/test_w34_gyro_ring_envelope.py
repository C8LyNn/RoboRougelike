"""Original exact Codex Gyroscope visual-ring source-only measurements, not Unity."""
import importlib.util,os,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parent
sp=importlib.util.spec_from_file_location("gyro",ROOT/"w34_gyro_ring_envelope.py")
m=importlib.util.module_from_spec(sp);sp.loader.exec_module(m)
SRC=Path(os.environ.get("B01_PARTSIZE_PINNED_SOURCE",ROOT/"NO_ORIGINAL_SOURCE"))
class OriginalGyroRingVisualEnvelope(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  if not (SRC/"R41PartGeometry.cs").is_file() or not (SRC/"R3VehicleAssembly.cs").is_file():
   raise unittest.SkipTest("original pinned Codex R41 and R3 code unavailable")
  cls.r41=(SRC/"R41PartGeometry.cs").read_text("utf8")
  cls.r3=(SRC/"R3VehicleAssembly.cs").read_text("utf8")
  cls.r=m.audit(cls.r41,cls.r3)
 def test01_actual_gyro_ring_is_nonphysical(self):
  self.assertIn('material,false,"Pierścień żyroskopu"',self.r41)
  self.assertTrue(self.r["nonphysicalRingSource"])
 def test02_source_parent_actual_point52(self):
  self.assertEqual(self.r["rootCubeNominalHalfExtentM"],.26)
  self.assertIn("item.transform.localScale=Vector3.one*0.52f;",self.r3)
 def test03_provenance_current_codex303(self):
  self.assertEqual(self.r["inputCodexPR303Head"],"c89744d5ab43f4a795abe62527d94439be573693")
 def test04_precise_conservative_overhang(self):
  a=self.r["ringYawCases"][0]
  self.assertAlmostEqual(a["decorativeRingConservativeHalfExtentXYZm"][0],.28986062,places=6)
  self.assertAlmostEqual(a["overrunBeyondOneCellMetersXYZ"][0],.01486062,places=6)
  self.assertEqual(a["overrunBeyondOneCellMetersXYZ"][1:],[0,0])
 def test05_yaw_four_variants_and_swap(self):
  c=self.r["ringYawCases"]
  self.assertEqual(len(c),4)
  self.assertEqual(c[0]["decorativeRingConservativeSignedCellsXYZ"],[[-1,0,1],[0],[0]])
  self.assertEqual(c[1]["decorativeRingConservativeSignedCellsXYZ"],[[0],[0],[-1,0,1]])
  self.assertEqual(c[2]["decorativeRingConservativeSignedCellsXYZ"],c[0]["decorativeRingConservativeSignedCellsXYZ"])
  self.assertEqual(c[3]["decorativeRingConservativeSignedCellsXYZ"],c[1]["decorativeRingConservativeSignedCellsXYZ"])
 def test06_no_fake_unity_measurements(self):
  self.assertEqual(self.r["actualVisualMeshMeasurements"],"NOT_RUN")
  self.assertEqual(self.r["actualEngine"],"NOT_RUN")
  self.assertTrue(all(x["actualUnityRendererBounds"]=="NOT_RUN" and x["actualUnityColliderBounds"]=="NOT_RUN" for x in self.r["ringYawCases"]))
 def test07_negative_changed_scale_source_is_rejected(self):
  wrong=self.r41.replace("ring.localScale=new Vector3(1.15f,.05f,1.15f)",
                          "ring.localScale=new Vector3(1.05f,.05f,1.05f)")
  with self.assertRaisesRegex(ValueError,"SOURCE_ANCHOR_DRIFT"):m.audit(wrong,self.r3)
 def test08_negative_changed_euler_source_is_rejected(self):
  wrong=self.r41.replace("Quaternion.Euler(45,0,20)","Quaternion.Euler(0,0,20)")
  with self.assertRaisesRegex(ValueError,"SOURCE_ANCHOR_DRIFT"):m.audit(wrong,self.r3)
 def test09_negative_changed_physics_flag_is_rejected(self):
  wrong=self.r41.replace('material,false,"Pierścień żyroskopu"','material,true,"Pierścień żyroskopu"')
  with self.assertRaisesRegex(ValueError,"SOURCE_ANCHOR_DRIFT"):m.audit(wrong,self.r3)
 def test10_negative_wrong_parent_source_is_rejected(self):
  wrong=self.r3.replace("item.transform.localScale=Vector3.one*0.52f;",
                        "item.transform.localScale=Vector3.one*1.0f;")
  with self.assertRaisesRegex(ValueError,"SOURCE_ANCHOR_DRIFT"):m.audit(self.r41,wrong)
 def test11_negative_unsupported_elliptic_cylinder(self):
  with self.assertRaisesRegex(ValueError,"ELLIPTICAL_CYLINDER_UNSUPPORTED"):
   m.analytical_continuous_cylinder_half_extents((1.15,.05,1.0),(45,0,20),.52)
 def test12_gyro_main_sphere_not_part_of_ring_prediction(self):
  self.assertIn("Gyroscope?PrimitiveType.Sphere",self.r41)
  self.assertLess(.85*.52/2,.26) # root cube exceeds primary gyro sphere
  self.assertFalse(self.r["assetSourceChanged"])
if __name__=="__main__":unittest.main(verbosity=2)
