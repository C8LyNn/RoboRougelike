#!/usr/bin/env python3
"""Validate original C# gate receipt; independent negative/mutation checks.
Does NOT run Unity or fabricate physics contact. """
import copy
import json
import math
import os
import unittest
from pathlib import Path

HEAD="f4ca318577edbdf4d003ab3e104ac7ec71d83cfe"
EXPECTED={
 "HEAVY_CENTERED_STRAIGHT_ACCEPT","HEAVY_OFFSET_0_08_ACCEPT",
 "HEAVY_OFFSET_0_10_REJECT","HEAVY_OFFSET_NEG_0_10_REJECT",
 "HEAVY_CENTERED_YAW_3_ACCEPT","HEAVY_CENTERED_YAW_4_ACCEPT",
 "HEAVY_CENTERED_YAW_5_REJECT","HEAVY_CENTERED_YAW_8_REJECT",
 "HEAVY_CENTERED_YAW_10_REJECT","HEAVY_YAW_8_ON_BRIDGE_POLICY_PASS",
 "HEAVY_YAW_12_ON_BRIDGE_POLICY_REJECT","LIGHT_CENTERED_STRAIGHT_ACCEPT",
 "LIGHT_OFFSET_0_40_ACCEPT","LIGHT_OFFSET_0_55_REJECT",
 "LIGHT_YAW_8_ACCEPT","LIGHT_YAW_30_ACCEPT",
 "HEAVY_NEGATIVE_SIGN_DEPTH_REJECT","HEAVY_GUARD_YAW_LIMITS",
}
def verify(d):
    def require(b,key):
        if not b: raise ValueError(key)
    require(d.get("sourceExactCommit")==HEAD,"PRODUCER_SHA")
    require(d.get("actualProductionClass")=="B01.CHL003.CHL003GateCrossing","ORIGINAL_CSHARP_CLASS")
    require(d.get("originalPolicyOnly") is True,"SOURCE_AUTHORITY")
    require(d.get("actualUnity")=="NOT_RUN" and d.get("physicalDrive")=="NOT_RUN",
            "FAKE_NATIVE_PASS")
    require(d.get("sourceTestPassed")==18 and d.get("sourceTestFailed")==0 and
            d.get("total")==18,"SOURCE_TEST_TOTAL")
    require(d.get("realCHLWidthMeters")==3.5 and d.get("realBridgeWidthMeters")==3.7
            and d.get("guardEachSideMeters")==.25,"APERTURE_WIDTH_OR_GUARD")
    rows=d.get("results")
    require(type(rows)==list and len(rows)==18,"ROWS_COUNT")
    names=[v.get("test") for v in rows if type(v)==dict]
    require(len(names)==18 and set(names)==EXPECTED and len(set(names))==18,"MISSING_OR_DUPLICATE_CASES")
    for row in rows:
        require(row.get("passed") is True and row.get("nativeUnity")=="NOT_RUN",
                "FORGED_ROW_VERDICT")
        require(row.get("actualGateLimitDeg") is None or (4.60<row["actualGateLimitDeg"]<4.70),
                "GATE_LIMIT")
        require(row.get("actualBridgeLimitDeg") is None or (10.30<row["actualBridgeLimitDeg"]<10.40),
                "BRIDGE_LIMIT")
        if "sourceResult" in row:
            require(row["sourceResult"] is row["expected"],"INCORRECT_POLICY_VERDICT")
            if "gateMarginMeters" in row:
                for key in ("gateMarginMeters","bridgeMarginMeters","projectedWidthMeters","yawDeg","centerOffsetMeters"):
                    require(type(row.get(key)) in (float,int) and math.isfinite(row[key]),"BAD_FINITE")
    by={x["test"]:x for x in rows}
    straight=by["HEAVY_OFFSET_0_10_REJECT"]
    require(straight["sourceResult"] is False and straight["bridgeMarginMeters"]>.25
            and straight["gateMarginMeters"]<.25,"NEW_UNSAFE_OFFSET_NOT_ESTABLISHED")
    rot=by["HEAVY_CENTERED_YAW_5_REJECT"]
    require(rot["sourceResult"] is False and rot["bridgeMarginMeters"]>.25,
            "NEW_YAW_ONLY_CP_REJECTION_NOT_ESTABLISHED")
    guard=by["HEAVY_GUARD_YAW_LIMITS"]
    require(abs(guard["straightGateCenterOffsetLimitMeters"]-.09)<1e-5,"X_OFFSET_LIMIT")
    return True

class ReceiptMutationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.original=json.loads(Path(os.environ["B01_21_CHL_DQ_RECEIPT"]).read_text())
    def setUp(self):self.d=copy.deepcopy(self.original)
    def reject(self):
        with self.assertRaises(ValueError):verify(self.d)
    def test_01_genuine_compiled_source_receipt_valid(self):self.assertTrue(verify(self.d))
    def test_02_fake_native_success_is_rejected(self):
        self.d["actualUnity"]="PASS";self.reject()
    def test_03_source_sha_changed_is_rejected(self):
        self.d["sourceExactCommit"]="0"*40;self.reject()
    def test_04_source_case_removed_is_rejected(self):
        self.d["results"].pop();self.reject()
    def test_05_same_case_duplicated_is_rejected(self):
        self.d["results"][1]=self.d["results"][0];self.reject()
    def test_06_heavy_10cm_forged_success_is_rejected(self):
        next(x for x in self.d["results"] if x["test"]=="HEAVY_OFFSET_0_10_REJECT")["sourceResult"]=True
        self.reject()
    def test_07_heavy_5deg_forged_success_is_rejected(self):
        next(x for x in self.d["results"] if x["test"]=="HEAVY_CENTERED_YAW_5_REJECT")["sourceResult"]=True
        self.reject()
    def test_08_guard_forged_as_zero_is_rejected(self):
        self.d["guardEachSideMeters"]=0;self.reject()
    def test_09_gate_width_forged_as_bridge_is_rejected(self):
        self.d["realCHLWidthMeters"]=3.7;self.reject()
    def test_10_source_count_false_positive_rejected(self):
        self.d["sourceTestPassed"]=19;self.reject()
    def test_11_garbage_nonfinite_margin_rejected(self):
        self.d["results"][0]["gateMarginMeters"]=float("nan");self.reject()
    def test_12_fake_computed_0_09_limit_rejected(self):
        next(x for x in self.d["results"] if x["test"]=="HEAVY_GUARD_YAW_LIMITS")["straightGateCenterOffsetLimitMeters"]=.25
        self.reject()

if __name__=="__main__":unittest.main()
