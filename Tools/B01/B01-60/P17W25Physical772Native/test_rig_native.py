#!/usr/bin/env python3
"""Actual Git #772 source and real scratch stage/rollback tests; NOT Unity."""
import json
from pathlib import Path
import re
import tempfile
import unittest
import rig_native as r

class Original772SourceChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.n=r.original_module()
        cls.source=r.selected_source(cls.n)
        cls.helper=cls.n.runner()
    def test_01_exact_rig_original_sha_and_parent_v3(self):
        self.assertEqual(r.git("merge-base",r.API,r.RIG),r.API)
        self.assertEqual(len(r.git("diff","--name-only",r.API,r.RIG).splitlines()),21)
    def test_02_exact_owned_physical_and_ledger_git_blobs(self):
        for path,sha in r.PINS.items():
            self.assertEqual(r.git("rev-parse",r.RIG+":"+path),sha)
    def test_03_original_frozen_510_556_and_selected_api_729_byte_unchanged(self):
        for path,ref in (
          ("Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs",r.GAME),
          ("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",r.SAVE),
          ("Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs",r.API)):
            self.assertEqual(r.git("rev-parse",ref+":"+path),
                             r.git("rev-parse",r.RIG+":"+path))
    def test_04_three_original_authored_native_unity_test_names(self):
        txt=self.source[r.NATIVE_FIXTURE]
        names=tuple(s.decode() for s in re.findall(
            rb"\[UnityTest\]\s*public\s+IEnumerator\s+(\w+)\s*\(",txt))
        self.assertEqual(names,r.METHODS)
    def test_05_real_nine_owner_unity_meta_and_two_asmdefs(self):
        own=[p for p in self.source if p.startswith("Assets/B0123CanonicalRigPortW24")]
        self.assertEqual(len([p for p in own if p.endswith(".meta")]),9)
        self.assertEqual(len([p for p in own if p.endswith(".asmdef")]),2)
        self.assertGreaterEqual(self.helper.guid_check(self.source),30)
    def test_06_unique_private_player_names_and_scope_guards(self):
        s0,p0=r.private_case(self.source,0,"123456abcdef",self.helper)
        s1,p1=r.private_case(self.source,1,"123456abcdef",self.helper)
        self.assertNotEqual(p0,p1)
        self.assertIn(p0.encode(),s0[r.PROBE])
        self.assertIn(p1.encode(),s1[r.PROBE])
        self.assertNotEqual(s0[r.PROJECT],s1[r.PROJECT])
        self.assertEqual(self.helper.guid_check(s0),
                         self.helper.guid_check(self.source)+1)
    def test_07_three_private_physical_original_projects_stage_rollback(self):
        with tempfile.TemporaryDirectory(prefix="b0160-w25-rig-source-") as d:
            self.assertEqual(r.main(["--mode","preflight","--scratch-parent",d]),0)
            roots=list(Path(d).glob("B22-*"))
            self.assertEqual(len(roots),1)
            report=json.loads((roots[0]/"P25-receipt.json").read_text())
            self.assertEqual(report["native_G1"],"NOT_RUN")
            self.assertEqual(report["source_preflight"],
                             "PASS_3_GIT_EXACT_PRIVATE_PROJECT_STAGES_ROLLED_BACK")
            for i in range(3):
                self.assertFalse((roots[0]/("P"+str(i))).exists())
    def test_08_native_blocked_without_independent_g0_or_owner_consent(self):
        with tempfile.TemporaryDirectory(prefix="b0160-w25-rig-deny-") as d:
            with self.assertRaises(AssertionError):
                r.main(["--mode","native","--scratch-parent",d,
                        "--operator-consent","B0160 W25 RIG NATIVE"])
            with self.assertRaises(AssertionError):
                r.main(["--mode","native","--scratch-parent",d,
                        "--source-g0-approved"])
    def test_09_original_frozen_native_runner_byte_pinned(self):
        self.assertEqual(
           r.git("rev-parse",r.W24+":"+r.BASE.relative_to(r.ROOT).as_posix()),
           "40ca11d886fc567a5a79f6abfeabdf2b37f4c36e")
    def test_10_never_selects_773_or_749_into_rig_772_native_scene(self):
        for x in self.source:
            self.assertNotIn("P17W24-749",x)
            self.assertNotIn("P17W24OneAuthority",x)
        self.assertEqual(r.STORE_CANDIDATE,
                         "3aaaec4027ee246fbfb6f209179f0e9507363473")

if __name__=="__main__":
    unittest.main(verbosity=2)
