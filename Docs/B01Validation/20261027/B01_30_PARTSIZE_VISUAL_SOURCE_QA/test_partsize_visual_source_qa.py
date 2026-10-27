"""W16 source-grounded geometric acceptance; NO Unity engine execution."""
import importlib.util
import json
import os
import tempfile
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("b01_partsize_visual",ROOT/"partsize_visual_source_qa.py")
geo=importlib.util.module_from_spec(spec);spec.loader.exec_module(geo)
SNAP=Path(os.environ.get("B01_PARTSIZE_PINNED_SOURCE",""))

class SourceGeometryTests(unittest.TestCase):
 def test_00_actual_pinned_source_literals(self):
  if not (SNAP/"R41PartGeometry.cs").exists():
   self.skipTest("exact SHA pinned R3/R41 source not staged by GH Actions")
  verified=geo.verify_sources([SNAP/"R41PartGeometry.cs",SNAP/"R3VehicleAssembly.cs",
                               SNAP/"R3BonusModuleGeometry.cs",SNAP/"R3VehicleModel.cs"])
  self.assertEqual(verified,{"R41PartGeometry.cs":13,"R3VehicleAssembly.cs":5,
    "R3BonusModuleGeometry.cs":6,"R3VehicleModel.cs":4})

 def test_01_reject_actual_source_drift(self):
  text="public const float Grid=0.60f, BlockSize=0.52f;"
  with self.assertRaisesRegex(ValueError,"SOURCE_DRIFT"):
   geo.verify_text(text,geo.GEOMETRY_ANCHORS,"R41PartGeometry.cs")
 def test_02_wheel_real_collider_baseline(self):
  r=geo.measure("Wheel",0)
  self.assertEqual(r["physicalColliderSizeXYZ"],[.52,.91,.78])
  self.assertEqual(r["physicalOverrunByAxisXYZ"],[0,.375,.115])
  self.assertFalse(r["physicalFitsLogicalAABB"])
 def test_03_wheel_90_degree_swaps_x_z_not_height(self):
  r=geo.measure("Wheel",1)
  self.assertEqual(r["physicalColliderSizeXYZ"],[.78,.91,.52])
  self.assertEqual(r["physicalOverrunByAxisXYZ"],[.115,.375,0])
 def test_04_wheel_opposite_yaw_equal_extents(self):
  for yaw in [0,1]:
   a=geo.measure("Wheel",yaw)["physicalColliderSizeXYZ"]
   b=geo.measure("Wheel",yaw+2)["physicalColliderSizeXYZ"]
   self.assertEqual(a,b)
 def test_05_crawler_track_out_of_1cell(self):
  r=geo.measure("CrawlerTrack",0)
  self.assertEqual(r["physicalColliderSizeXYZ"],[.52,.676,.936])
  self.assertEqual(r["physicalOverrunByAxisXYZ"],[0,.141,.193])
 def test_06_crawler_track_all_yaws_overrun(self):
  self.assertTrue(all(not geo.measure("CrawlerTrack",a)["physicalFitsLogicalAABB"] for a in range(4)))
 def test_07_suspension_visual_only_external(self):
  r=geo.measure("Suspension",0)
  self.assertEqual(r["physicalColliderSizeXYZ"],[.52,.52,.52])
  self.assertEqual(r["physicalOverrunByAxisXYZ"],[0,0,0])
  self.assertEqual(r["renderOverrunByAxisXYZ"],[0,.245,0])
 def test_08_suspension_zero_collision_all_yaws(self):
  self.assertTrue(all(geo.measure("Suspension",a)["physicalFitsLogicalAABB"] for a in range(4)))
 def test_09_heavy_gun_barrel_outside(self):
  r=geo.measure("GunHeavy",0)
  self.assertEqual(r["physicalColliderSizeXYZ"],[.52,.676,1.352])
  self.assertEqual(r["physicalOverrunByAxisXYZ"],[0,.141,.817])
 def test_10_heavy_gun_90deg_footprint(self):
  r=geo.measure("GunHeavy",1)
  self.assertEqual(r["physicalColliderSizeXYZ"],[1.352,.676,.52])
  self.assertEqual(r["physicalOverrunByAxisXYZ"],[.817,.141,0])
 def test_11_hover_engine_real_collider(self):
  r=geo.measure("HoverEngine",0)
  self.assertEqual(r["physicalColliderSizeXYZ"],[.52,.5772,.52])
  self.assertEqual(r["physicalOverrunByAxisXYZ"],[0,.0422,0])
 def test_12_booster_exhaust_physical(self):
  r=geo.measure("Booster",0)
  self.assertEqual(r["physicalColliderSizeXYZ"],[.52,.52,.78])
  self.assertEqual(r["physicalOverrunByAxisXYZ"],[0,0,.245])
 def test_13_bonus_two_by_two_is_real_one_owner(self):
  r=geo.measure("BonusModule",0)
  self.assertEqual(r["logicalCellsWHD"],[2,1,2])
  self.assertEqual(r["physicalColliderSizeXYZ"],[1.07,.52,1.07])
  self.assertTrue(r["physicalFitsLogicalAABB"])
 def test_14_bonus_all_four_quadrants(self):
  for q in range(4):
   self.assertTrue(geo.measure("BonusModule",q)["physicalFitsLogicalAABB"])
 def test_15_armor_source_size(self):
  for q in range(4):
   self.assertEqual(geo.measure("Armor",q)["physicalColliderSizeXYZ"],[.52,.52,.52])
 def test_16_real_v_visual_flags(self):
  self.assertFalse(geo.measure("Wheel",0)["visualFitsLogicalAABB"])
  self.assertTrue(geo.measure("Armor",0)["visualFitsLogicalAABB"])
 def test_17_reject_fake_part(self):
  with self.assertRaises(ValueError):geo.measure("FAKE",0)
 def test_18_negative_grid_claim(self):
  self.assertEqual(geo.GRID,.55)
  self.assertEqual(geo.BLOCK,.52)
  self.assertNotEqual(geo.GRID,geo.BLOCK)
 def test_19_32_real_analytical_orientation_rows(self):
  rows=[geo.measure(k,q) for k in geo.PARTS for q in range(4)]
  self.assertEqual(len(rows),32)
  self.assertTrue(all(r["unityNative"]=="NOT_RUN" for r in rows))
  self.assertTrue(all(r["physxComputePenetration"]=="NOT_RUN" for r in rows))
 def test_20_no_phantom_mount_auth(self):
  r=geo.measure("Wheel",0)
  self.assertNotIn("approvedToMount",r)
  self.assertNotIn("sourceUnityPass",r)
 def test_21_logical_rotation_matches_root(self):
  _,hi=geo.physical_logical_grid_bounds((2,1,2),0)
  self.assertEqual(hi,[.825,.275,.825])
  lo,hi=geo.physical_logical_grid_bounds((2,1,2),1)
  self.assertEqual((lo,hi),([-.275,-.275,-.825],[.825,.275,.275]))
 def test_22_report_written_if_real_sources(self):
  if not (SNAP/"R41PartGeometry.cs").exists():
   self.skipTest("pinned R41 file missing: source gate cannot run locally")
  with tempfile.TemporaryDirectory() as d:
   p=Path(d)/"audit.json"
   res=geo.run([SNAP/"R41PartGeometry.cs",SNAP/"R3VehicleAssembly.cs",
                SNAP/"R3BonusModuleGeometry.cs",SNAP/"R3VehicleModel.cs"],p)
   self.assertEqual(len(res["records"]),32)
   self.assertEqual(json.loads(p.read_text())["G2"],"HOLD")

 def test_23_track_neighbor_real_box_box_intersection(self):
  r=geo.axis_aligned_child_box_neighbor_overlap("CrawlerTrack")
  self.assertEqual(r["overlapDepthsXYZ"],[.442,.104,.178])
  self.assertAlmostEqual(r["overlapVolumeM3"],.008182304,places=9)
  self.assertTrue(r["positiveSourceGeometryIntersection"])
  self.assertEqual(r["actualUnityCollision"],"NOT_RUN")
 def test_24_heavy_weapon_neighbor_real_box_box_intersection(self):
  r=geo.axis_aligned_child_box_neighbor_overlap("GunHeavy")
  self.assertEqual(r["overlapDepthsXYZ"],[.2496,.052,.52])
  self.assertAlmostEqual(r["overlapVolumeM3"],.006749184,places=9)
  self.assertTrue(r["positiveSourceGeometryIntersection"])
 def test_25_convex_wheel_cannot_claim_box_box_intersection(self):
  with self.assertRaises(ValueError):
   geo.axis_aligned_child_box_neighbor_overlap("Wheel")
 def test_26_no_unverified_other_neighbor_direction(self):
  with self.assertRaises(ValueError):
   geo.axis_aligned_child_box_neighbor_overlap("CrawlerTrack",(1,0,0))
if __name__=="__main__":unittest.main(verbosity=2)
