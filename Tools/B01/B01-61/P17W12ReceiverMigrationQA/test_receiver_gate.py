#!/usr/bin/env python3
"""Independent W12 source-only real #510 archive migration and rollback negative QA."""
import os
from pathlib import Path
import tempfile
import unittest
from unittest import mock
import receiver_gate as gate

class ActualFrozenProducerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.ctx=gate.frozen_producer()
        cls.p=cls.ctx.__enter__()
        cls.raw,cls.manifest=gate.archive_attestation(cls.p)
    @classmethod
    def tearDownClass(cls):
        cls.ctx.__exit__(None,None,None)
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(prefix="b0161_w12_case_")
        self.addCleanup(self.tmp.cleanup)
        self.parent=Path(self.tmp.name)
    def stage(self):
        target=self.parent/"receiver"
        selected,receipt=gate.stage_and_scan(self.p,self.raw,target)
        self.assertEqual(receipt["violations"],[])
        self.assertEqual(receipt["selected_source_byte_conflicts"],[])
        return selected
    def fixture(self, name,content):
        path=self.parent/"receiver"/"Assets"/name
        path.parent.mkdir(parents=True,exist_ok=True)
        path.write_text(content,encoding="utf-8")
        return path
    def test_01_original_35_member_zip_pinned_source(self):
        self.assertEqual(len(self.manifest["files"]),34)
        self.assertEqual(self.manifest["source_sha"],gate.PRODUCER)
        self.assertEqual(gate.sha(self.raw),gate.EXPECTED_ARCHIVE_SHA256)
    def test_02_selected_standalone_receiver_scans_clean(self):
        target=self.stage()
        result=gate.scan_receiver(target)
        self.assertEqual(result["status"],"PASS_SOURCE_SCAN_ONLY")
        self.assertEqual(gate.verify_staged_selected_bytes(target,self.raw),[])
    def test_03_real_stage_then_rollback_cleans_every_byte(self):
        target=self.stage()
        self.p.rollback(self.raw,target)
        self.assertFalse(target.exists())
    def test_04_inactive_legacy_w07_scene_guid_is_blocked(self):
        self.stage()
        self.fixture("Scenes/Invisible.unity",
          "GameObject:\n  m_IsActive: 0\nMonoBehaviour:\n  m_Enabled: 0\n  m_Script: {fileID: 11500000, guid: b017d070000000000000000000000008, type: 3}\n")
        result=gate.scan_receiver(self.parent/"receiver")
        self.assertTrue(any("LEGACY_GUID_W07" in x for x in result["violations"]))
    def test_05_disabled_w08_prefab_is_blocked(self):
        self.stage()
        self.fixture("Prefab/old.prefab",
          "MonoBehaviour:\n  m_Enabled: 0\n  m_Script: {fileID: 11500000, guid: b018d080000000000000000000000008, type: 3}\n")
        self.assertTrue(any("LEGACY_GUID_W08" in x for x in
            gate.scan_receiver(self.parent/"receiver")["violations"]))
    def test_06_w09_asset_guid_is_blocked(self):
        self.stage()
        self.fixture("State/old.asset",
            "m_Script: {fileID: 11500000, guid: b019d090000000000000000000000008, type: 3}\n")
        self.assertTrue(any("LEGACY_GUID_W09" in x for x in
            gate.scan_receiver(self.parent/"receiver")["violations"]))
    def test_07_external_legacy_public_api_call_is_blocked(self):
        self.stage()
        self.fixture("Scripts/External.cs",
           "using B01.DebrisPlayground001;\nclass X { DebrisRigPlayground old; void Dock(){ old.Reattach(0); }}")
        self.assertTrue(any("LEGACY_SOURCE_TYPE_OR_NAMESPACE" in x for x in
            gate.scan_receiver(self.parent/"receiver")["violations"]))
    def test_08_legacy_name_only_in_comment_is_not_false_positive(self):
        self.stage()
        self.fixture("Scripts/Doc.cs",
          "// DebrisRigPlayground.Reattach is forbidden in this build\n"+
          "/* B01.DebrisPlayground001 is historical */\nclass NewNoLegacy {}")
        self.assertEqual(gate.scan_receiver(self.parent/"receiver")["violations"],[])
    def test_09_utf8_inaccessible_script_fails_closed(self):
        self.stage()
        p=self.parent/"receiver"/"Assets"/"Opaque.cs"
        p.write_bytes(b"\xff\xfe\xfd")
        self.assertTrue(any("NON_UTF8_UNINSPECTED" in x for x in
            gate.scan_receiver(self.parent/"receiver")["violations"]))
    def test_10_duplicate_v2_meta_guid_is_blocked(self):
        self.stage()
        self.fixture("Other/foreign.cs.meta",
                    "fileFormatVersion: 2\nguid: "+gate.SELECTED_GUID+"\n")
        self.assertTrue(any("DUPLICATE_UNITY_GUID" in x for x in
            gate.scan_receiver(self.parent/"receiver")["violations"]))
    def test_11_legacy_source_meta_even_without_scene_is_blocked(self):
        self.stage()
        self.fixture("Other/Old.cs.meta",
            "fileFormatVersion: 2\nguid: b017d070000000000000000000000008\n")
        self.assertTrue(any("LEGACY_COMPONENT_META" in x for x in
            gate.scan_receiver(self.parent/"receiver")["violations"]))
    def test_12_competing_candidate_same_filename_changed_bytes_is_blocked(self):
        target=self.stage()
        path=target/"Assets"/"B0123SecureReattachV2"/"Runtime"/"SecureDebrisPlaygroundV2.cs"
        path.write_bytes(b"// FORGED competing #511 at selected path\n")
        self.assertIn(path.relative_to(target).as_posix(),
                      gate.verify_staged_selected_bytes(target,self.raw))
    def test_13_selected_source_member_missing_is_blocked(self):
        target=self.stage()
        path=target/"Assets"/"B0123SecureReattachV2"/"Runtime"/"AtomicDockLedger.cs"
        path.unlink()
        self.assertIn(path.relative_to(target).as_posix(),
                      gate.verify_staged_selected_bytes(target,self.raw))
    def test_14_original_rollback_refuses_foreign_file_preserves_evidence(self):
        target=self.stage()
        (target/"important.txt").write_text("foreign keep",encoding="utf-8")
        with self.assertRaises(ValueError):self.p.rollback(self.raw,target)
        self.assertTrue((target/"important.txt").exists())
    def test_15_original_rollback_refuses_modified_source_and_keeps_tree(self):
        target=self.stage()
        path=target/"Assets"/"B0123SecureReattachV2"/"Runtime"/"AtomicDockLedger.cs"
        path.write_bytes(b"\x00")
        with self.assertRaises(ValueError):self.p.rollback(self.raw,target)
        self.assertTrue(target.exists())
    def test_16_staging_refuses_preexisting_receiver(self):
        target=self.parent/"receiver"
        target.mkdir()
        with self.assertRaises(ValueError):self.p.stage(self.raw,target)
    def test_17_stage_mid_write_fault_rolls_back_fully(self):
        original=Path.write_bytes
        for fail_at in (1,2,16,35):
            target=self.parent/("fault_"+str(fail_at))
            calls=0
            def injected(path,data):
                nonlocal calls
                calls+=1
                if calls==fail_at:raise OSError("simulated disk write fault "+str(fail_at))
                return original(path,data)
            with mock.patch.object(Path,"write_bytes",injected):
                with self.assertRaises(OSError):self.p.stage(self.raw,target)
            self.assertFalse(target.exists(),"orphan receiver after fault "+str(fail_at))
    def test_18_legacy_in_foreign_test_file_does_not_modify_original_archive(self):
        target=self.stage()
        self.fixture("ThirdParty/Legacy.cs",
            "public class Q { public DebrisSafeReattachGate legacy; }")
        self.assertTrue(gate.scan_receiver(target)["violations"])
        # The original Git archive SHA remains immutable, even if foreign receiver differs.
        self.assertEqual(gate.sha(self.raw),gate.EXPECTED_ARCHIVE_SHA256)
    def test_19_unavailable_receiver_fails_closed(self):
        with self.assertRaises(AssertionError):
            gate.scan_receiver(self.parent/"not_present")
    def test_20_source_only_does_not_claim_ack_or_unity_g1(self):
        # This returns a second source-only attestation; no real Codex receiver is scanned.
        receipt=gate.report()
        self.assertIs(receipt["ACK"],False)
        self.assertEqual(receipt["actual_codex_receiver"],"NOT_SCANNED")
        self.assertEqual(receipt["G0"],"SOURCE_PRECHECK_ONLY_RECEIVER_PENDING")
        self.assertEqual(receipt["G1"],"NOT_RUN")
    @unittest.skipUnless(hasattr(os,"symlink"),"symlink unsupported")
    def test_21_symlink_asset_refuses_scan_and_original_rollback(self):
        target=self.stage()
        source=target/"Assets"/"unsafe.cs"
        dest=self.parent/"secret.txt";dest.write_text("keep")
        try:source.symlink_to(dest)
        except OSError:self.skipTest("OS disallows symlink")
        self.assertTrue(any("SYMLINK_NOT_SCANNABLE" in x for x in
             gate.scan_receiver(target)["violations"]))
        with self.assertRaises(ValueError):self.p.rollback(self.raw,target)
        self.assertTrue(dest.exists())

if __name__=="__main__":unittest.main(verbosity=2)
