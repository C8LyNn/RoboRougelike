"""W17 true PR272/PR286 source evidence regressions, never synthetic Unity PASS."""
import importlib.util
import os
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("w17",ROOT/"w17_signed_placement_conflicts.py")
g=importlib.util.module_from_spec(spec);spec.loader.exec_module(g)
P=Path(os.environ.get("B01_PARTSIZE_PR286_SOURCE",str(ROOT/"NO_PR286")))
C=Path(os.environ.get("B01_PARTSIZE_PINNED_SOURCE",str(ROOT/"NO_R41")))

class W17RealGeometry(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  if not (P/"PartSizeContract.cs").is_file() or not (C/"R41PartGeometry.cs").is_file():
   raise unittest.SkipTest("real PR286 / PR272 source bytes absent")
  cls.report=g.build(P,C)
  cls.groups={p["part"]:p for p in cls.report["parts"]}

 def rec(self,name,yaw=0):
  return self.groups[name]["fourYawCases"][yaw]
 def test_01_actual_pin_and_versions(self):
  self.assertEqual(self.report["inputPartsizePR286Head"],
   os.environ.get("B01_PARTSIZE_PR286_HEAD","UNSET"))
  self.assertEqual(self.report["canonicalR41PinnedHead"],
   "9cd5a5272a9ebda130a97a5f1f73ce3eaf24b976")
 def test_02_sixteen_source_physical_vs_render_rows(self):
  self.assertEqual(sum(len(x["fourYawCases"]) for x in self.report["parts"]),16)
 def test_03_one_cell_wheel_signed_pivot(self):
  r=self.rec("Wheel")
  self.assertEqual(r["signedPhysicalCellIndexRangesXYZ"],[[0],[-1,0],[-1,0,1]])
  self.assertEqual(r["conservativeSignedCellCount"],6)
  self.assertFalse(r["positiveOnlyAnchoredExisting1CellFit"])
 def test_04_track_signed_local(self):
  r=self.rec("CrawlerTrack")
  self.assertEqual(r["signedPhysicalCellIndexRangesXYZ"],[[0],[-1,0],[-1,0,1]])
 def test_05_gun_signed_local(self):
  r=self.rec("GunHeavy")
  self.assertEqual(r["signedPhysicalCellIndexRangesXYZ"],[[0],[0,1],[0,1,2]])
 def test_06_wheel_yaw90_moves_extra_z_to_x(self):
  r=self.rec("Wheel",1)
  self.assertEqual(r["signedPhysicalCellIndexRangesXYZ"],[[-1,0,1],[-1,0],[0]])
 def test_07_heavy_gun_yaw90_forward_positive_x(self):
  r=self.rec("GunHeavy",1)
  self.assertEqual(r["signedPhysicalCellIndexRangesXYZ"],[[0,1,2],[0,1],[0]])
 def test_08_heavy_gun_yaw180_forward_negative_z(self):
  r=self.rec("GunHeavy",2)
  self.assertEqual(r["signedPhysicalCellIndexRangesXYZ"],[[0],[0,1],[-2,-1,0]])
 def test_09_four_yaw_all_three_part_conflicts(self):
  for name in ("Wheel","CrawlerTrack","GunHeavy"):
   self.assertTrue(all(not v["positiveOnlyAnchoredExisting1CellFit"]
                       for v in self.groups[name]["fourYawCases"]))
 def test_10_track_boxes_intersect_neighbor_in_four_yaws(self):
  r=[v for v in self.report["boxNeighborProbes"] if v["part"]=="CrawlerTrack"]
  self.assertEqual(len(r),4)
  self.assertTrue(all(v["positiveBoxBoxIntersection"] for v in r))
  self.assertTrue(all(abs(v["staticBoxBoxIntersectionVolumeM3"]-.008182304)<1e-10 for v in r))
 def test_11_gun_boxes_intersect_neighbor_in_four_yaws(self):
  r=[v for v in self.report["boxNeighborProbes"] if v["part"]=="GunHeavy"]
  self.assertEqual(len(r),4)
  self.assertTrue(all(v["positiveBoxBoxIntersection"] for v in r))
  self.assertTrue(all(abs(v["staticBoxBoxIntersectionVolumeM3"]-.006749184)<1e-10 for v in r))
 def test_12_neighbor_rotates_correctly(self):
  self.assertEqual([x["neighborLogicalOffsetXYZ"]
     for x in self.report["boxNeighborProbes"][:4]],
     [[0,0,1],[1,0,0],[0,0,-1],[-1,0,0]])
 def test_13_wheel_mesh_is_not_source_boxproof(self):
  with self.assertRaisesRegex(ValueError,"BoxCollider"):
   g.box_neighbor_conflict("Wheel",0)
 def test_14_suspension_physically_inside_but_visual_below(self):
  for r in self.groups["Suspension"]["fourYawCases"]:
   self.assertTrue(r["positiveOnlyAnchoredExisting1CellFit"])
   self.assertEqual(r["signedPhysicalCellIndexRangesXYZ"],[[0],[0],[0]])
   self.assertEqual(r["signedRenderCellIndexRangesXYZ"],[[0],[-1,0],[0]])
 def test_15_reject_bogus_bounds(self):
  for lo,hi in ((1,0),(-float("inf"),1),(0,float("nan"))):
   with self.assertRaises(ValueError):
    g.signed_indices(lo,hi)
 def test_16_positive_root_one_cell_not_approved(self):
  self.assertTrue(all(not v["proposedGridOccupancyApproved"]
    for p in self.report["parts"] for v in p["fourYawCases"]))
  self.assertTrue(self.report["thisReportApprovesNoMounts"])
 def test_17_native_not_run(self):
  self.assertEqual(self.report["unityEditor"],"NOT_RUN")
  self.assertEqual(self.report["unityPhysx"],"NOT_RUN")
  self.assertTrue(all(v["nativeColliderBounds"]=="NOT_RUN"
    for p in self.report["parts"] for v in p["fourYawCases"]))
 def test_18_no_contact_claim_for_wheel(self):
  self.assertEqual(len(self.report["boxNeighborProbes"]),8)
  self.assertFalse(any(x["part"]=="Wheel" for x in self.report["boxNeighborProbes"]))
 def test_19_source_contract_negatives_present(self):
  self.assertEqual(len(self.report["negativeCases"]),5)
  self.assertIn("W17-BROADPHASE-OMITTED",{x["id"] for x in self.report["negativeCases"]})
 def test_20_signed_interval_grid_edges(self):
  self.assertEqual(g.signed_indices(-.275,.275),[0])
  self.assertEqual(g.signed_indices(-.276,.276),[-1,0,1])

if __name__=="__main__":
 unittest.main(verbosity=2)
