#!/usr/bin/env python3
"""16 source-level negative controls on exact two separate original R3 candidates."""
from pathlib import Path
import sys
import unittest
sys.path.insert(0,str(Path(__file__).resolve().parent))
import stage_original as g

class OriginalDualR3SourceControls(unittest.TestCase):
    def reject(self,candidate,core,host,why):
        with self.assertRaisesRegex(g.Reject,why):
            g.inspect_source(candidate,core,host)
    @classmethod
    def setUpClass(cls):
        core=g.git("show",g.R3["A"]["sha"]+":Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",binary=True).decode()
        host=g.git("show",g.R3["A"]["sha"]+":"+g.HOST,binary=True).decode()
        bcore=g.git("show",g.R3["B"]["sha"]+":Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",binary=True).decode()
        bhost=g.git("show",g.R3["B"]["sha"]+":"+g.HOST,binary=True).decode()
        ccore=g.git("show",g.R3["C"]["sha"]+":Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",binary=True).decode()
        chost=g.git("show",g.R3["C"]["sha"]+":"+g.HOST,binary=True).decode()
        cls.a=(core,host);cls.b=(bcore,bhost);cls.c=(ccore,chost)
    def test_01_original_r3a_exact_source_fingerprint(self):
        g.inspect_source("A",*self.a)
        self.assertEqual(g.R3["A"]["sha"],"22cec9888047994ae523cbe7bf1b0a483d64cec4")
    def test_02_original_r3b_exact_source_retained_lease(self):
        g.inspect_source("B",*self.b)
        self.assertEqual(g.R3["B"]["sha"],"d9b95146e46a5d81d5a291d7887e456164f4d0c2")
    def test_03_different_transaction_code_still_two_versions(self):
        self.assertNotEqual(self.a[0],self.b[0])
        self.assertNotEqual(g.R3["A"]["core_blob"],g.R3["B"]["core_blob"])
    def test_04_original_r3b_does_not_have_r3a_epoch_guard(self):
        self.assertNotIn("publishedGeneration",self.b[0])
        self.assertIn("publishedGeneration",self.a[0])
    def test_05_no_source_selection_without_owner(self):
        self.assertEqual(len(g.R3),3)
        self.assertEqual(len(g.OWNED),7)
    def test_06_reject_mismatched_r3a_core(self):
        self.reject("A",self.b[0],self.a[1],"MISSING_R3A_PUBLISHED")
    def test_07_reject_mismatched_r3b_core(self):
        self.reject("B",self.a[0],self.b[1],"R3B_CORE_CHANGED")
    def test_08_reject_competing_host_for_a(self):
        self.reject("A",self.a[0],self.b[1],"R3A_ON_DISABLE")
    def test_09_reject_competing_host_for_b(self):
        self.reject("B",self.b[0],self.a[1],"R3B_RETAINED")
    def test_10_reject_original_r3a_guard_removed(self):
        self.reject("A",self.a[0].replace("durable!=publishedGeneration","false"),self.a[1],
                    "MISSING_R3A_PUBLISHED")
    def test_11_reject_original_r3a_hash_guard_removed(self):
        self.reject("A",self.a[0].replace("diskHash!=publishedHash","false"),self.a[1],
                    "MISSING_R3A_PUBLISHED")
    def test_12_reject_original_r3a_released_lease_wiring(self):
        self.reject("A",self.a[0],self.a[1].replace(
                    "OnDisable(){ReleaseSlot();}","OnDisable(){}"),
                    "R3A_ON_DISABLE")
    def test_13_reject_original_r3b_retained_owner_wiring(self):
        self.reject("B",self.b[0],self.b[1].replace(
                    "HostSlotLease.TryAcquire","HostSlotLease.FakeAcquire"),
                    "R3B_RETAINED")
    def test_14_reject_alt_604_checkpoint_fence_import(self):
        self.reject("B",self.b[0]+"\n// CheckpointFence",self.b[1],
                    "ALT_604_FORBIDDEN")
    def test_15_reject_missing_public_file_store(self):
        self.reject("A",self.a[0].replace("public sealed class FileCheckpointStore",
                                         "public sealed class OtherCheckpointStore"),
                    self.a[1],"NOT_ORIGINAL_FROZEN_SOURCE_CORE")
    def test_16_reject_unknown_third_source_variant(self):
        self.reject("X",*self.a,"CANDIDATE_UNKNOWN")
    def test_17_real_owner_r4_quarantine_source_is_new_version_not_alt(self):
        g.inspect_source("C",*self.c)
        self.assertEqual(g.R3["C"]["sha"],"ae5dbfa5e9c4689d2bf80fcabf8438e9c673c419")
        self.assertEqual(self.c[1],self.a[1])
        self.assertIn("commitMayHaveChangedAuthority",self.c[0])
    def test_18_reject_r4_without_same_call_commit_ambiguity_quarantine(self):
        self.reject("C",self.c[0].replace("commitMayHaveChangedAuthority","lostCommitFlag"),
                    self.c[1],"R4_PENDING_COMMIT_STICKY")

if __name__=="__main__":unittest.main(verbosity=2)
