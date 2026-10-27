"""W30 actual Codex R3 source+real B01-23 unit-parent-proxy divergence tests."""
import importlib.util,os,unittest
from pathlib import Path
R=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("w30",R/"w30_parent_scale_reconcile.py")
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
CANON=Path(os.environ.get("B01_PARTSIZE_PINNED_SOURCE",R/"NO_SOURCE"))
PROD=Path(os.environ.get("B01_PARTSIZE_PR286_SOURCE",R/"NO_PROD"))
SRC=Path(os.environ.get("B01_PARTSIZE_OWNER_7_PROXY",R/"NO_OWNER")).expanduser()
class RealR3HierarchyMismatchQA(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  if not all(x.exists() for x in [CANON/"R41PartGeometry.cs",
     CANON/"R3VehicleAssembly.cs",PROD/"PartSizeBaselineCatalog.cs",SRC]):
   raise unittest.SkipTest("Real pinned R3/PartSize/B01-23 seven proxy source unavailable")
  cls.asm=(CANON/"R3VehicleAssembly.cs").read_text()
  cls.geom=(CANON/"R41PartGeometry.cs").read_text()
  cls.cat=(PROD/"PartSizeBaselineCatalog.cs").read_text()
  cls.report=m.reconcile(cls.asm,cls.geom,cls.cat,SRC)
  cls.rows={r['kind']:r for r in cls.report['rows']}
 def test_01_real_original_hierarchy_scale(self):
  self.assertEqual(m.root_scale(self.asm),.52)
 def test_02_actual_original_git_heads(self):
  self.assertEqual(self.report["inputCodexSourceHead"],"7cfd1276d4708395144668249223d78fb83c3b19")
  self.assertEqual(self.report["inputPartsizeHead"],"be2be0e4802bc3f450179901f9ae91c6d84bcb5b")
  self.assertEqual(self.report["inputProducerSourceProxyQaHead"],"dc2b3c199e6576f757f5ff6214e41fff444f73a8")
 def test_03_all_real_seven_source_types(self):
  self.assertEqual(tuple(self.rows),m.KINDS)
 def test_04_actual_world_vs_unit_omni_vertical(self):
  a=self.rows["OmniDrive"]
  self.assertEqual(a["b01_23_unitParentSourceProxyMinXYZ"],[-.18,-.85,-.45])
  self.assertEqual(a["b01_23_unitParentSourceCellsXYZ"][1],[-2,-1,0])
  q=a["actualR3ScaledStaticYawCases"][0]
  self.assertEqual(q["payloadSourceProxyWorldMinXYZ"],[-.0936,-.442,-.234])
  self.assertEqual(q["payloadSourceProxyWorldMaxXYZ"],[.0936,.026,.234])
  self.assertEqual(q["payloadSignedCellsXYZ"][1],[-1,0])
 def test_05_omni_union_with_real_root_box(self):
  r=self.rows["OmniDrive"]["actualR3ScaledStaticYawCases"][0]
  self.assertEqual(r["unionWithCoreRootBoxMinXYZ"],[-.26,-.442,-.26])
  self.assertEqual(r["unionWithCoreRootBoxMaxXYZ"],[.26,.26,.26])
  self.assertEqual(r["rootPlusPayloadSignedCellsXYZ"],[[0],[-1,0],[0]])
 def test_06_real_yaw90_rotates_horizontal_axes(self):
  row=self.rows["OmniDrive"]
  q0=row["actualR3ScaledStaticYawCases"][0]
  q1=row["actualR3ScaledStaticYawCases"][1]
  self.assertEqual(q1["payloadSignedCellsXYZ"][1],[-1,0])
  self.assertEqual(q0["payloadSignedCellsXYZ"],q1["payloadSignedCellsXYZ"])
 def test_07_booster_2cell_vs_original_unit_proxy_3cell(self):
  x=self.rows["Booster"]
  self.assertEqual(x["b01_23_unitParentSourceCellsXYZ"][2],[-2,-1,0])
  self.assertEqual(x["actualR3ScaledStaticYawCases"][0]["payloadSignedCellsXYZ"][2],[-1,0])
 def test_08_sphere_proxy_fits_root_cell_after_scaling(self):
  for name in ("Blink","Gyroscope"):
   x=self.rows[name]
   q0=x["actualR3ScaledStaticYawCases"][0]
   self.assertEqual(q0["payloadSignedCellsXYZ"],[[0],[0],[0]])
 def test_09_gyroscope_excludes_extra_nonphysical_ring(self):
  self.assertTrue(self.rows["Gyroscope"]["extraGyroDecorativeRingExcluded"])
  self.assertIn('Primitive(PrimitiveType.Cylinder,block,material,false,"Pierścień żyroskopu")',self.geom)
 def test_10_all_native_remain_unexecuted(self):
  self.assertEqual(self.report["actualUnity"],"NOT_RUN")
  self.assertEqual(self.report["physx"],"NOT_RUN")
  self.assertTrue(all(x["sweptGeometryStatus"]=="NOT_RUN"
   for x in self.rows.values()))
 def test_11_no_model_job_or_hq_art_approval(self):
  self.assertEqual(self.report["HQConceptSelection"],"PENDING")
  self.assertFalse(self.report["changeProducerFiles"])
 def test_12_reject_forged_parent_scale(self):
  wrong=self.asm.replace("item.transform.localScale=Vector3.one*0.52f;",
                         "item.transform.localScale=Vector3.one*1.0f;")
  with self.assertRaisesRegex(ValueError,"PINNED_SOURCE_DRIFT"):
   m.reconcile(wrong,self.geom,self.cat,SRC)
 def test_13_reject_forged_omni_child(self):
  wrong=self.geom.replace('device.localPosition=Vector3.down*.4f;',
                          'device.localPosition=Vector3.down*.6f;')
  with self.assertRaisesRegex(ValueError,"PINNED_SOURCE_DRIFT"):
   m.reconcile(self.asm,wrong,self.cat,SRC)
 def test_14_reject_model_switch(self):
  with self.assertRaisesRegex(ValueError,"invalid AABB arity"):
   m.cell_cover([1,2],[3,4])
 def test_15_root_vs_primitive_scope_explicit(self):
  self.assertTrue(all(len(x["actualR3ScaledStaticYawCases"])==4 for x in self.rows.values()))
  self.assertIn("NOT a real Collider.bounds",self.report["purpose"])
 def test_16_no_wrong_4_ring_equivalence_to_current_omni(self):
  self.assertIn("one cylinder child",self.report["OmniDriveNewSourceFact"])
  self.assertIn("if(kind==R3PartKind.OmniDrive)",self.geom)
  self.assertIn("var device=Primitive(type,block,material,solid",self.geom)
if __name__=="__main__":unittest.main(verbosity=2)
