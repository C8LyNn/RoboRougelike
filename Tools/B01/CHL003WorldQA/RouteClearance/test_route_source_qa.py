#!/usr/bin/env python3
"""Independent static original-source corridor assertions and tamper regressions."""
import copy
import unittest
from route_source_qa import DEMO,GATE,analyze,half_length,geometry,top_ramp_endpoints

class ActualN27CorridorQA(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.demo=DEMO.read_text(encoding="utf-8")
        cls.gate=GATE.read_text(encoding="utf-8")
        cls.r=analyze(cls.demo,cls.gate)
    def row(self,rig,g,yaw=0):
        return next(x for x in self.r["gateProfiles"] if
                    x["rig"]==rig and x["gate"]==g and x["yawDeg"]==yaw)
    def test_01_pinned_producer_SHA(self):
        self.assertEqual(self.r["producerInputSHA"],
          "077e692785aff2990a2d72a80fd1db62ac57b0e8")
    def test_02_20_repro_source_rows(self):
        self.assertEqual(len(self.r["gateProfiles"]),20)
        self.assertEqual({x["gate"] for x in self.r["gateProfiles"]},
                         {"CP0","CP1","CP2","CP3","FINISH"})
    def test_03_source_CP_zero_and_one_road(self):
        self.assertEqual(self.row("Light","CP0")["surface"],"GROUND")
        self.assertEqual(self.row("Heavy","CP1")["surface"],"GROUND")
        self.assertEqual(self.r["groundTopSpan"]["topY_M"],0)
    def test_04_CP_two_real_bridge(self):
        self.assertEqual(self.row("Heavy","CP2")["surface"],"BRIDGE")
        self.assertEqual(self.row("Heavy","CP2")["surfaceZ_M"],[19.5,29.5])
    def test_05_CP_three_real_overlap(self):
        self.assertEqual(self.row("Heavy","CP3")["surface"],"BRIDGE_TO_FINAL")
        self.assertEqual(self.r["roadJointsM"]["bridgeToFinalPlanarOverlap"],.5)
        self.assertGreater(self.r["finalTopSpan"]["zEndM"],38)
    def test_06_ramp_entry_actual_y_and_ground_overlap(self):
        z=self.r["roadJointsM"]
        self.assertGreater(z["groundToRampPlanarOverlap"],1.9)
        self.assertLess(z["groundToRampPlanarOverlap"],2.1)
        self.assertGreater(z["groundToRampEntranceLip"],.08)
        self.assertLess(z["groundToRampEntranceLip"],.09)
    def test_07_ramp_exit_lip_both_axes(self):
        z=self.r["roadJointsM"]
        self.assertAlmostEqual(z["rampExitToBridgeStartGap"],.133659,places=6)
        self.assertAlmostEqual(z["rampExitToBridgeTopLip"],.143318,places=6)
    def test_08_five_gates_spaced_in_original_order(self):
        self.assertEqual([x["centerSpacingM"] for x in self.r["checkpointSpacingM"]],
                         [6.5,9.5,8,9])
    def test_09_finish_straight_heavy_hull_near_edge(self):
        end=self.row("Heavy","FINISH")
        self.assertAlmostEqual(end["wholeBodyLongitudinalHalfExtentM"],1.7)
        self.assertAlmostEqual(end["platformEndMarginFromFrontHullAtGateCenterM"],.3)
    def test_10_finish_yaw_eight_reduces_front_margin(self):
        straight=self.row("Heavy","FINISH",0)
        yaw=self.row("Heavy","FINISH",8)
        self.assertLess(yaw["platformEndMarginFromFrontHullAtGateCenterM"],.15)
        self.assertGreater(yaw["platformEndMarginFromFrontHullAtGateCenterM"],.13)
        self.assertGreater(straight["platformEndMarginFromFrontHullAtGateCenterM"],
                           yaw["platformEndMarginFromFrontHullAtGateCenterM"])
    def test_11_finish_light_more_longitudinal_margin(self):
        self.assertGreater(self.row("Light","FINISH")["platformEndMarginFromFrontHullAtGateCenterM"],.8)
    def test_12_minimum_nonphysical_trigger_contact_window(self):
        # Conditional: same original hull/wheel collider breadth, max source
        # speed12m/s and *illustrative* fixed50Hz; not actual Unity callback proof.
        for g in ("CP0","CP1","CP2","CP3","FINISH"):
            self.assertGreater(self.row("Light",g)["idealTickWindowsAt12mps50Hz_NOT_NATIVE"],10)
            self.assertGreater(self.row("Heavy",g)["idealTickWindowsAt12mps50Hz_NOT_NATIVE"],14)
    def test_13_ideal_root_vertically_within_physical_gate(self):
        for x in self.r["gateProfiles"]:
            self.assertTrue(x["fitsGateVerticalInIdealFlatPose"])
        self.assertAlmostEqual(self.row("Heavy","CP2")["idealWheelSupportedRootY_M"],2.61)
    def test_14_both_rigs_yaw8_width_guard_on_corrected_3_7(self):
        for x in self.r["gateProfiles"]:
            self.assertTrue(x["meetsSourceSideGuard025Centered"])
    def test_15_no_claim_of_native_or_rig_contact(self):
        self.assertEqual(self.r["nativeUnity"],"NOT_RUN")
        self.assertEqual(self.r["realWheelContact"],"NOT_RUN")
        self.assertEqual(self.r["actualMotorDrivenLight3Heavy3"],"NOT_RUN")
    def test_16_mutant_finish_moved_outside_platform_rejected(self):
        old=self.demo.replace('Gate("FINISH", 38f','Gate("FINISH", 41f',1)
        self.assertNotEqual(old,self.demo)
        with self.assertRaisesRegex(ValueError,"GATE_LOCATION_DRIFT"):
            analyze(old,self.gate)
    def test_17_mutant_CP_order_mismatch_rejected(self):
        old=self.demo.replace('Gate("CP2", 21f','Gate("CP2", 29f',1)
        self.assertNotEqual(old,self.demo)
        with self.assertRaisesRegex(ValueError,"GATE_LOCATION_DRIFT"):
            analyze(old,self.gate)
    def test_18_mutant_ramp_lip_increase_rejected(self):
        old=self.demo.replace(
            'Box("SOLID_RAMP", new Vector3(0f, .54f, 16f)',
            'Box("SOLID_RAMP", new Vector3(0f, .24f, 16f)',1)
        self.assertNotEqual(old,self.demo)
        with self.assertRaisesRegex(ValueError,"RAMP_BRIDGE_LIP_CHANGED"):
            analyze(old,self.gate)
    def test_19_mutant_bridge_z_further_rejected(self):
        old=self.demo.replace(
            'Box("NARROW_SOLID_BRIDGE", new Vector3(0f, 1.38f, 24.5f)',
            'Box("NARROW_SOLID_BRIDGE", new Vector3(0f, 1.38f, 25.5f)',1)
        self.assertNotEqual(old,self.demo)
        with self.assertRaises(ValueError):
            analyze(old,self.gate)
    def test_20_mutant_bridge_vertical_zero_delta_rejected(self):
        old=self.demo.replace(
            'Box("NARROW_SOLID_BRIDGE", new Vector3(0f, 1.38f, 24.5f)',
            'Box("NARROW_SOLID_BRIDGE", new Vector3(0f, 1.23f, 24.5f)',1)
        self.assertNotEqual(old,self.demo)
        with self.assertRaisesRegex(ValueError,"RAMP_BRIDGE_LIP_CHANGED"):
            analyze(old,self.gate)
    def test_21_mutant_old_gate_3_5_not_current_source(self):
        old=self.demo.replace('new Vector3(3.70f, 4.5f, .16f)',
                              'new Vector3(3.5f, 4.5f, .16f)',1)
        self.assertNotEqual(old,self.demo)
        with self.assertRaisesRegex(ValueError,"PRODUCER_GEOMETRY_DRIFT"):
            analyze(old,self.gate)
    def test_22_mutant_missing_strict_side_guard_rejected(self):
        old=self.gate.replace('minimumSideClearance = .25f',
                              'minimumSideClearance = 0f',1)
        self.assertNotEqual(old,self.gate)
        with self.assertRaisesRegex(ValueError,"PRODUCER_GATE_POLICY_DRIFT"):
            analyze(self.demo,old)
    def test_23_mutant_finish_final_platform_shortened_rejected(self):
        # Preserve bridge->final overlap while shortening ONLY the exit.
        # Otherwise the earlier seam check masks the intended end-of-track guard.
        old=self.demo.replace(
            'Box("FINAL_PLATFORM", new Vector3(0f, 1.38f, 34.5f)',
            'Box("FINAL_PLATFORM", new Vector3(0f, 1.38f, 31.5f)',1)
        old=old.replace(
            'new Vector3(4.2f, .60f, 11f)',
            'new Vector3(4.2f, .60f, 5f)',1)
        self.assertNotEqual(old,self.demo)
        with self.assertRaisesRegex(ValueError,"FINISH_FRONT_COLLIDER_EXCEEDS_PLATFORM"):
            analyze(old,self.gate)

if __name__=="__main__":unittest.main()
