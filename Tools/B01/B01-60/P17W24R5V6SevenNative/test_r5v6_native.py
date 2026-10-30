#!/usr/bin/env python3
"""Direct exact-author Git SHA and physical SOURCE preflight tests; NOT Unity."""
import json
from pathlib import Path
import re
import tempfile
import unittest
import r5v6_native as v

class SevenR5V6SourceChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.n=v.original()
        cls.src=v.selected_source(cls.n)
        cls.m=cls.n.runner()

    def test_01_exact_author_v6_sha_and_unique_fixture_blob(self):
        self.assertEqual(v.git("rev-parse",v.V6+":"+self.n.FIXTURE),v.CORRECTED_BLOB)
        self.assertEqual(v.git("merge-base",v.R5,v.V6),v.R5)

    def test_02_sole_four_author_v6_changes(self):
        self.assertEqual(set(v.git("diff","--name-only",v.R5,v.V6).splitlines()),
                         v.ONLY_AUTHOR_CHANGES)

    def test_03_no_changes_to_original_r5_product_unity_source(self):
        old=self.n.selected_source()
        self.assertEqual(set(old),set(self.src))
        for p in old:
            if p!=self.n.FIXTURE:
                self.assertEqual(old[p],self.src[p],p)
        self.assertNotEqual(old[self.n.FIXTURE],self.src[self.n.FIXTURE])

    def test_04_corrected_cold_restart_real_serialized_generation(self):
        text=self.src[self.n.FIXTURE].decode()
        part=text.split("public IEnumerator "+self.n.HOLD_METHOD+"()",1)[1].split(
            "public IEnumerator WrongGeneration_",1)[0]
        for expected in ("SnapshotCodec.TryDecode(saved,out RunSnapshot savedSnapshot)",
                         "Assert.AreEqual(5,savedSnapshot.Generation)",
                         "restored.TryRestoreBytes(saved,41,5)",
                         "Assert.AreEqual(5,restored.Generation)",
                         "IsConsumed(0,2310001)","IsConsumed(1,2310002)"):
            self.assertIn(expected,part)
        self.assertNotIn("Assert.AreEqual(4,restored.Generation)",part)

    def test_05_seven_original_playmode_case_names(self):
        text=self.src[self.n.FIXTURE].decode()
        found=tuple(re.findall(
            r"\[UnityTest\]\s*public\s+IEnumerator\s+(\w+)\s*\(",text))
        self.assertEqual(found,self.n.METHODS)
        self.assertEqual(len(found),7)

    def test_06_original_meta_guid_stable_and_private_extra(self):
        old=self.n.selected_source()
        self.assertEqual(self.m.guid_check(self.src),self.m.guid_check(old))
        p0,_=self.n.private_source(self.src,0,"abcdef012345",self.m)
        p6,_=self.n.private_source(self.src,6,"abcdef012345",self.m)
        self.assertEqual(self.m.guid_check(p0),self.m.guid_check(old)+1)
        self.assertNotEqual(p0[self.n.PS],p6[self.n.PS])

    def test_07_independent_seven_private_physical_source_stage_rollback(self):
        with tempfile.TemporaryDirectory(prefix="b0160-w24-original-") as t:
            self.assertEqual(v.main(["--mode","preflight","--scratch-parent",t]),0)
            roots=list(Path(t).glob("B22-*"))
            self.assertEqual(len(roots),1)
            receipt=json.loads((roots[0]/"P24-receipt.json").read_text())
            self.assertEqual(receipt["corrected_owner_fixture_SHA"],v.V6)
            self.assertEqual(receipt["source_preflight"],
                             "PASS_7_PRIVATE_R5V6_STAGES_ROLLED_BACK_NO_EDITOR")
            self.assertEqual(receipt["native_G1"],"NOT_RUN")
            for i in range(7):
                self.assertFalse((roots[0]/("P"+str(i))).exists())

    def test_08_operator_native_cannot_bypass_consent_or_g0(self):
        with tempfile.TemporaryDirectory(prefix="b0160-w24-negative-") as t:
            with self.assertRaises(AssertionError):
                v.main(["--mode","native","--unity","Z:/fake/6000.3.14f1/Editor/Unity.exe",
                        "--scratch-parent",t,"--approved-source-g0"])
            with self.assertRaises(AssertionError):
                v.main(["--mode","native","--unity","Z:/fake/6000.3.14f1/Editor/Unity.exe",
                        "--scratch-parent",t,
                        "--operator-consent","B0160 R5V6 SEVEN NATIVE"])

    def test_09_original_r5_and_v6_codex_product_sources_not_written(self):
        for path,blob in self.n.BLOBS.items():
            if path==self.n.FIXTURE:continue
            self.assertEqual(v.git("rev-parse",v.V6+":"+path),blob)

    def test_10_old_six_case_harness_frozen_and_author_seven_separate(self):
        self.assertEqual(v.git("rev-parse",v.QA_BASE+":"+
                               str(v.OLDER.relative_to(v.ROOT)).replace("\\","/")),
                         "40ca11d886fc567a5a79f6abfeabdf2b37f4c36e")
        self.assertEqual(len(self.n.PARTIAL),6)
        self.assertEqual(len(self.n.METHODS),7)

if __name__=="__main__":
    unittest.main(verbosity=2)
