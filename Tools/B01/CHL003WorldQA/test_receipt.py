#!/usr/bin/env python3
import copy
import json
import os
import unittest
from pathlib import Path
from verify_receipt import verify, ReceiptInvalid

class CHL003WorldReceiptTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        f=Path(os.environ["CHL003_RECEIPT"])
        cls.original=json.loads(f.read_text(encoding="utf-8"))
        cls.ok=verify(cls.original)
    def setUp(self):
        self.data=copy.deepcopy(self.original)
    def bad(self):
        with self.assertRaises(ReceiptInvalid):verify(self.data)
    def test_01_real_dotnet_receipt_has_all_cases(self):
        self.assertEqual(self.ok["positive"],18)
        self.assertEqual(self.ok["narrowNegative"],10)
        self.assertEqual(self.ok["nativeUnity"],"NOT_RUN")
    def test_02_fake_engine_green_rejected(self):
        self.data["nativeUnity"]="PASS";self.bad()
    def test_03_fake_rigidbody_green_rejected(self):
        self.data["realVehicleDrive"]="PASS";self.bad()
    def test_04_wrong_producer_pin_rejected(self):
        self.data["producerPinnedSha"]="0"*40;self.bad()
    def test_05_misrepresented_case_count_rejected(self):
        self.data["passed"]=31;self.bad()
    def test_06_missing_full_positive_route_rejected(self):
        self.data["sourceCases"].pop(0);self.bad()
    def test_07_duplicate_seed_variant_rejected(self):
        self.data["sourceCases"][1]=copy.deepcopy(self.data["sourceCases"][0]);self.bad()
    def test_08_nominal_light_width_rejected(self):
        r=next(x for x in self.data["sourceCases"] if x["kind"]=="COURSE_BASELINE" and x["rig"]=="LIGHT_MEASURED")
        r["widthMeters"]=2.1;self.bad()
    def test_09_missing_side_clearance_rejected(self):
        r=self.data["sourceCases"][0];r["minSideMarginMeters"]=0;self.bad()
    def test_10_reordered_checkpoint_rejected(self):
        r=next(x for x in self.data["sourceCases"] if x["kind"]=="COURSE_BASELINE" and x["segments"]==4)
        r["checkpoints"][0],r["checkpoints"][1]=r["checkpoints"][1],r["checkpoints"][0]
        self.bad()
    def test_11_forged_accepted_narrow_course_rejected(self):
        r=next(x for x in self.data["sourceCases"] if x["kind"]=="COURSE_NARROW_REJECT")
        r["rejectedReason"]="PASS";self.bad()
    def test_12_missing_narrow_negative_rejected(self):
        self.data["sourceCases"]=[v for v in self.data["sourceCases"]
                                  if not(v["kind"]=="COURSE_NARROW_REJECT" and v["seed"]==2 and v["rig"]=="LIGHT_MEASURED")]
        self.bad()
    def test_13_obstacle_collision_false_green_rejected(self):
        r=next(x for x in self.data["sourceCases"] if x["kind"]=="INJECTED_ROUTE_BLOCK")
        r["rejectedReason"]="BFS_SPINE_CLEARANCE_OK";self.bad()
    def test_14_stale_hash_unchanged_rejected(self):
        r=next(x for x in self.data["sourceCases"] if x["kind"]=="INJECTED_ROUTE_BLOCK")
        r["newGeometryHash"]=r["oldGeometryHash"];self.bad()
    def test_15_fake_CHL_controller_green_rejected(self):
        r=next(x for x in self.data["sourceCases"] if x["kind"]=="CHECKPOINT_ORDER_NEGATIVE")
        r["actualCHLController"]="PASS";self.bad()
    def test_16_nonfinite_point_rejected(self):
        r=next(x for x in self.data["sourceCases"] if x["kind"]=="COURSE_BASELINE")
        r["start"][0]=float("nan");self.bad()
    def test_17_bad_hash_rejected(self):
        self.data["sourceCases"][0]["geometryHash"]="G"*64;self.bad()
    def test_18_route_validator_bypassed_rejected(self):
        self.data["sourceCases"][0]["validatorExploredCells"]=0;self.bad()
    def test_19_changed_finish_coordinate_rejected(self):
        self.data["sourceCases"][0]["finish"][2]+=5;self.bad()

if __name__=="__main__":unittest.main()
