#!/usr/bin/env python3
"""Source-backed W17 regression checks. No Unity or PhysX is invoked."""
import unittest
from pathlib import Path
from geometry_qa import (
    DEMO,CONTROLLER,MOTOR,PROFILES,YAW_SAMPLES,
    analyze,parse_source,swept_halfwidth,first_yaw_for_margin,
)

class W17ActualCHLGeometryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.demo=DEMO.read_text(encoding="utf-8")
        cls.controller=CONTROLLER.read_text(encoding="utf-8")
        cls.motor=MOTOR.read_text(encoding="utf-8")
        cls.report=analyze(cls.demo,cls.controller,cls.motor)

    def row(self,rig,yaw,zone="NARROW_SOLID_BRIDGE"):
        return next(x for x in self.report["yawSampleRows"]
                    if x["rig"]==rig and x["yawDeg"]==yaw and x["zone"]==zone)

    def test_01_actual_producer_geometry_fifty_scenarios(self):
        self.assertEqual(len(self.report["yawSampleRows"]),2*len(YAW_SAMPLES)*3)
        self.assertEqual(self.report["producerExactSourceHead"],
            "9a947108928d4c2cdfdfbb25cbeb30464439da37")
        self.assertEqual(len(YAW_SAMPLES),9)

    def test_02_measure_real_heavy_wheel_union_not_chassis_only(self):
        p=PROFILES["Heavy"]
        self.assertAlmostEqual(2*swept_halfwidth(p,0),2.82,places=6)
        self.assertGreater(2*swept_halfwidth(p,0),p["hullWidthM"])
        self.assertAlmostEqual(self.row("Heavy",0)["sideMarginIfCenteredM"],.44,places=6)

    def test_03_light_real_wheel_union(self):
        p=PROFILES["Light"]
        self.assertAlmostEqual(2*swept_halfwidth(p,0),2.0,places=6)
        self.assertAlmostEqual(self.row("Light",0)["sideMarginIfCenteredM"],.85,places=6)

    def test_04_heavy_yaw10_is_within_review_margin(self):
        self.assertTrue(self.row("Heavy",10)["meetsReviewMargin025EachSide"])
        self.assertGreater(self.row("Heavy",10)["sideMarginIfCenteredM"],.25)

    def test_05_heavy_yaw12_fails_review_margin_even_if_still_fits(self):
        r=self.row("Heavy",12)
        self.assertTrue(r["positiveWidthFit"])
        self.assertFalse(r["meetsReviewMargin025EachSide"])
        self.assertAlmostEqual(r["sideMarginIfCenteredM"],.222754,places=5)

    def test_06_heavy_yaw30_exceeds_bridge_actual_static_envelope(self):
        r=self.row("Heavy",30)
        self.assertFalse(r["positiveWidthFit"])
        self.assertLess(r["sideMarginIfCenteredM"],-.05)

    def test_07_light_yaw30_still_fits(self):
        r=self.row("Light",30)
        self.assertTrue(r["positiveWidthFit"])
        self.assertTrue(r["meetsReviewMargin025EachSide"])

    def test_08_pinned_ramp_bridge_both_width3point7(self):
        g=self.report["geometry"]
        self.assertEqual(g["SOLID_RAMP"]["size"][0],3.7)
        self.assertEqual(g["NARROW_SOLID_BRIDGE"]["size"][0],3.7)
        self.assertEqual(g["FINAL_PLATFORM"]["size"][0],4.2)

    def test_09_actual_ramp_to_bridge_horizontal_edge_gap(self):
        d=self.report["rampExitToBridgeStartHorizontalM"]
        self.assertGreater(d,.12)
        self.assertLess(d,.15)

    def test_10_actual_ramp_to_bridge_top_step(self):
        d=self.report["rampExitToBridgeTopVerticalM"]
        self.assertGreater(d,.14)
        self.assertLess(d,.15)

    def test_11_actual_bridge_final_overlap_no_gap(self):
        self.assertAlmostEqual(self.report["bridgeFinalPlatformOverlapZ_M"],.5)

    def test_12_checkpoint_and_finish_separated_in_real_CHL_not_MAPGEN(self):
        self.assertEqual(self.report["geometry"]["checkpoints"],[5,11.5,21,29])
        self.assertEqual(self.report["geometry"]["finishZ"],38)
        self.assertAlmostEqual(self.report["lastCheckpointFinishTriggerGapM"],8.84)
        self.assertTrue(self.report["notEquivalentToMAPGEN279"])

    def test_13_latest_motor_and_finish_runtime_source_present(self):
        self.assertIn("body.AddForce(",self.motor)
        self.assertIn("finishArbiter.TryCommit(",self.controller)

    def test_14_all_physical_acceptance_not_run(self):
        self.assertEqual(self.report["unityPhysX"],"NOT_RUN")
        self.assertEqual(self.report["actualRigidBodyTraversal"],"NOT_RUN")
        self.assertEqual(self.report["independentUnityG1"],"NOT_RUN")

    def test_15_no_false_direct_gameplay_completion(self):
        self.assertNotIn("PASS",self.report["sourceOnlyVerdict"])
        self.assertNotIn("CODEX_READY_PASS",self.report["sourceOnlyVerdict"])

    def test_16_real_heavy_margin_break_angle_within_10_to_12deg(self):
        threshold=self.report["bridgeHeavyMaxYawFor025SideM"]
        self.assertGreater(threshold,10)
        self.assertLess(threshold,12)

    def test_17_negative_old_3m_bridge_source_rejected(self):
        old=self.demo.replace("new Vector3(3.70f, .60f, 10f)",
                              "new Vector3(3.00f, .60f, 10f)",1)
        self.assertNotEqual(old,self.demo)
        with self.assertRaisesRegex(ValueError,"W17_BRIDGE_GEOMETRY_CHANGED"):
            analyze(old,self.controller,self.motor)

    def test_18_negative_colocated_checkpoint_finished_source_rejected(self):
        old=self.demo.replace('Gate("CP3", 29f,','Gate("CP3", 38f,',1)
        self.assertNotEqual(old,self.demo)
        with self.assertRaisesRegex(ValueError,"CHANGED_RUNTIME_EXPRESSION"):
            analyze(old,self.controller,self.motor)

    def test_19_negative_changed_ramp_angle_rejected(self):
        old=self.demo.replace("new Vector3(3.7f, .55f, 7f), -12f",
                              "new Vector3(3.7f, .55f, 7f), -2f",1)
        self.assertNotEqual(old,self.demo)
        with self.assertRaisesRegex(ValueError,"W17_RAMP_GEOMETRY_CHANGED"):
            analyze(old,self.controller,self.motor)

    def test_20_negative_missing_collider_wheel_guard(self):
        old=self.demo.replace("new Vector3(.82f, .82f, .82f)",
                              "new Vector3(.40f, .40f, .40f)",1)
        self.assertNotEqual(old,self.demo)
        with self.assertRaisesRegex(ValueError,"CHANGED_RUNTIME_EXPRESSION"):
            analyze(old,self.controller,self.motor)

    def test_21_negative_missing_real_finish_arbiter(self):
        old=self.controller.replace("finishArbiter.TryCommit(","removedArbiter(",1)
        self.assertNotEqual(old,self.controller)
        with self.assertRaisesRegex(ValueError,"FINISH_ARBITER_SOURCE_MISMATCH"):
            analyze(self.demo,old,self.motor)

    def test_22_negative_missing_actual_motor(self):
        old=self.motor.replace("body.AddTorque(","fakeSteer(",1)
        self.assertNotEqual(old,self.motor)
        with self.assertRaisesRegex(ValueError,"FORCE_MOTOR_SOURCE_MISMATCH"):
            analyze(self.demo,self.controller,old)

    def test_23_heavy_yaw8_bridge_passes_but_checkpoint_gate_fails(self):
        bridge=self.row("Heavy",8)
        gate=self.row("Heavy",8,"TRIGGER_APERTURE")
        self.assertTrue(bridge["meetsReviewMargin025EachSide"])
        self.assertFalse(gate["meetsReviewMargin025EachSide"])
        self.assertTrue(gate["positiveWidthFit"])
        self.assertGreater(gate["sideMarginIfCenteredM"],.15)
        self.assertLess(gate["sideMarginIfCenteredM"],.25)

    def test_24_gate_aperture_requires_025_margin_from_real_source(self):
        self.assertAlmostEqual(self.row("Heavy",0,"TRIGGER_APERTURE")["sideMarginIfCenteredM"],.34)
        self.assertTrue(self.row("Heavy",0,"TRIGGER_APERTURE")["meetsReviewMargin025EachSide"])
        self.assertEqual(self.report["geometry"]["TRIGGER_APERTURE"]["size"],[3.5,4.5,.16])

if __name__=="__main__":
    unittest.main()
