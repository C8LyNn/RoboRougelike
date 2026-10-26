#!/usr/bin/env python3
"""W14 source-level fixture integrity; does NOT execute Unity, PhysX or R6."""
import hashlib
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "Assets/B01Packages/MAPGENWORLD001/Tests/PlayMode/MapWorldW14NativeAcceptanceTests.cs"
META = Path(str(SOURCE)+".meta")
BASE = ROOT / "Assets/B01Packages/MAPGENWORLD001/Tests/PlayMode/MapWorldLifecycleTests.cs"
UPSTREAM = "5be31039411f89f17654b1c332f8692883fa03cc"

def cases():
    actual = SOURCE.read_text(encoding="utf-8")
    matches = re.findall(
        r'\[UnityTest\]\s+public\s+IEnumerator\s+W14_(Arena|Course)(07|31|83)_(Light|Heavy)_RealWidth\(\)\s*=>\s*InspectPhysicalEnvelope\(MapRecipe\.(CombatArena|ObstacleCourse),\s*(7|31|83),\s*(true|false)\);',
        actual
    )
    rows = []
    for recipe, label_seed, rig, target, actual_seed, flag in matches:
        if (recipe == "Arena") != (target == "CombatArena"):
            raise ValueError("RECIPE_ALIAS_MISMATCH")
        if int(label_seed) != int(actual_seed):
            raise ValueError("SEED_ALIAS_MISMATCH")
        if (rig == "Heavy") != (flag == "true"):
            raise ValueError("ENVELOPE_ALIAS_MISMATCH")
        rows.append({"recipe":target, "seed":int(label_seed),
                     "profile":rig, "native":"NOT_RUN",
                     "passCategory":"COLLIDER_CLEARANCE_AND_REPLAY_ONLY",
                     "actualR6Drive":"NOT_RUN"})
    return rows,actual

class W14NativeSourceFixtureTests(unittest.TestCase):
    def test_twelve_unique_native_case_pairs(self):
        rows,_=cases()
        self.assertEqual(len(rows),12)
        expected = {(r,s,p) for r in ("CombatArena","ObstacleCourse")
                    for s in (7,31,83) for p in ("Light","Heavy")}
        self.assertEqual({(v["recipe"],v["seed"],v["profile"]) for v in rows},expected)
    def test_actual_physx_collider_probe_not_transform_teleport_acceptance(self):
        _,text=cases()
        self.assertIn("Physics.ComputePenetration(",text)
        self.assertIn("yield return new WaitForFixedUpdate()",text)
        self.assertIn("PenetrationTolerance = .005f",text)
        self.assertIn("profile.Width + 2f * request.ClearancePerSideMeters",text)
        self.assertIn("actualR6Drive=NOT_RUN",text)
        self.assertNotIn("R6_DRIVE_PASS",text)
    def test_five_impossible_real_light_seeds_have_no_scene_mutation(self):
        _,text=cases()
        self.assertRegex(text,r"new\[\]\s*\{\s*2,\s*12,\s*14,\s*22,\s*28\s*\}")
        self.assertIn("Assert.AreSame(arena, adapter.Current)",text)
        self.assertIn("Assert.AreSame(arena.GeneratedRoot, builder.Current.GeneratedRoot)",text)
        self.assertIn("Assert.AreEqual(0, cleared)",text)
    def test_corrupt_receipt_rejected_before_live_scene_replacement(self):
        _,text=cases()
        self.assertIn("adapter.TryReplay(badReceipt",text)
        self.assertIn("MapWorldFailure.InvalidReplayReceipt",text)
        self.assertIn("Assert.AreSame(initial, adapter.Current)",text)
        self.assertIn("Assert.AreSame(originalRoot, builder.Current.GeneratedRoot)",text)
        self.assertIn("Assert.AreEqual(oldReadyCount, snapshots)",text)
    def test_full_valid_replay_and_destroy_visibility(self):
        _,text=cases()
        self.assertIn("adapter.TryReplay(receipt,",text)
        self.assertIn("Assert.AreEqual(receipt, replay.ReplayReceipt)",text)
        self.assertIn("Assert.IsFalse(originalRoot.activeInHierarchy)",text)
        self.assertIn("adapter.ClearWorld()",text)
    def test_w15_re_signed_seed_drift_keeps_real_world(self):
        # Structural gate ONLY, NOT native Unity execution.
        old=BASE.read_text(encoding="utf-8")
        self.assertIn("W15SignedSeedGeometryContradictionKeepsLiveMap",old)
        self.assertIn('parts[2]="31"',old)
        self.assertIn("sha.ComputeHash(",old)
        self.assertIn("MapWorldFailure.ReplayGeometryMismatch",old)
        self.assertIn("Assert.AreSame(first,adapter.Current)",old)
        self.assertIn("Assert.AreSame(existing,builder.Current)",old)
        self.assertIn("Assert.AreEqual(0,cleared)",old)

    def test_native_meta_and_existing_tests_preserved(self):
        m=META.read_text(encoding="utf-8")
        self.assertIn("guid: b0121001000000000000000000000010",m)
        old=BASE.read_text(encoding="utf-8")
        self.assertIn("ReplayReceiptRegeneratesExactlyOrFailsClosed",old)
        self.assertIn("RotatedAndOffsetHostAcceptsValidatedWorldMetadata",old)

if __name__=="__main__":
    import sys
    if len(sys.argv)==3 and sys.argv[1]=="manifest":
        rows,_=cases()
        sha=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
        result={"suite":"B01-21 W14 native PhysX collider replay fixtures",
                "source_sha256":sha,"producer_pin":UPSTREAM,
                "unity":"NOT_RUN","actualR6Drive":"NOT_RUN",
                "nativeCases":len(rows),"cases":rows,
                "additionalNativeNegative":"W14_RealLight10mImpossibleCourseRejectsWithoutTouchingArena"}
        path=Path(sys.argv[2]);path.parent.mkdir(parents=True,exist_ok=True)
        path.write_text(json.dumps(result,sort_keys=True,indent=2)+"\n",encoding="utf-8")
        print("W14_NATIVE_MATRIX_EMITTED_CASES="+str(len(rows))+" UNITY_NATIVE=NOT_RUN")
    else:unittest.main()
