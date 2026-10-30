#!/usr/bin/env python3
"""Independent #510 exact Git package & disposable Codex receiver migration tests."""
import json
import os
from pathlib import Path
import tempfile
import unittest
from receiver_gate import (Hold, SOURCE_SHA, ZIP_SHA, MANIFEST, OLD_W07_GUID,
    V2_GUID, load_bundle, receiver_preflight, rollback_scratch, stage_scratch, sha)

class ReceiverAcceptance(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source=os.environ["B01_W12_SOURCE"]
        cls.archive=os.environ["B01_W12_ZIP"]
        cls.package=load_bundle(cls.archive,cls.source)

    def fixture(self,content=None,path="Assets/Scenes/Robot.unity"):
        tmp=tempfile.TemporaryDirectory()
        root=Path(tmp.name)
        if content is not None:
            f=root/path;f.parent.mkdir(parents=True,exist_ok=True)
            f.write_text(content,encoding="utf8")
        return tmp,root

    def test_01_one_pinned_producer(self):
        self.assertEqual(SOURCE_SHA,"d94400d95d13db7f27e1533df05c9292d5380431")
        self.assertEqual(sha(Path(self.archive).read_bytes()),ZIP_SHA)
    def test_02_physical_members_and_manifest(self):
        self.assertEqual(len(self.package),35)
        self.assertIn(MANIFEST,self.package)
    def test_03_recorded_git_origin_and_unity_gates(self):
        data=json.loads(self.package[MANIFEST])
        self.assertEqual(data["version"],"2.0.0")
        self.assertEqual(data["source_sha"],SOURCE_SHA)
        self.assertEqual(data["owned_meta_guids"],14)
        self.assertFalse(data["codex_ack"])
        self.assertEqual(data["native_unity"],"NOT_RUN")
    def test_04_actual_v2_controller_exact_git_bytes(self):
        self.assertIn(b"rig.Ledger.TryReattach(rig.Spec.PartId,",
            self.package["Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs"])
    def test_05_missing_legacy_in_selected_package(self):
        self.assertFalse(any("B0123DebrisPlayground001" in x for x in self.package))
        self.assertFalse(any("P17W11SecureV2RearmAtomicity" in x for x in self.package))
    def test_06_clean_disposable_receiver_is_ready_for_staging(self):
        tmp,root=self.fixture()
        with tmp:self.assertEqual(receiver_preflight(root,self.package),[])
    def test_07_unrelated_receiver_file_is_preserved_not_deleted(self):
        tmp,root=self.fixture("owned by Codex","Assets/Other/Unrelated.cs")
        with tmp:
            self.assertEqual(receiver_preflight(root,self.package),[])
            self.assertEqual((root/"Assets/Other/Unrelated.cs").read_text(),"owned by Codex")
    def test_08_real_legacy_W07_guid_in_scene_blocks_migration(self):
        tmp,root=self.fixture("m_Script: {fileID: 11500000, guid: "+OLD_W07_GUID+", type: 3}")
        with tmp:self.assertTrue(any("LEGACY_OR_ALTERNATIVE_REFERENCE" in x for x in receiver_preflight(root,self.package)))
    def test_09_legacy_class_in_prefab_blocks_migration(self):
        tmp,root=self.fixture("component DebrisRigPlayground","Assets/Prefab/Robot.prefab")
        with tmp:self.assertNotEqual(receiver_preflight(root,self.package),[])
    def test_10_alternative_W07_494_reference_blocks(self):
        tmp,root=self.fixture("P17W10PublicDockGuardR2","Assets/Models/Dock.asset")
        with tmp:self.assertNotEqual(receiver_preflight(root,self.package),[])
    def test_11_alternative_511_signature_blocks(self):
        tmp,root=self.fixture("P17W11SecureV2RearmAtomicity","Assets/Models/Alternative.asset")
        with tmp:self.assertNotEqual(receiver_preflight(root,self.package),[])
    def test_12_existing_v2_file_prevents_overwrite(self):
        tmp,root=self.fixture("different version","Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs")
        with tmp:self.assertTrue(any("WOULD_OVERWRITE" in x for x in receiver_preflight(root,self.package)))
    def test_13_casefold_collision_avoids_windows_overwrite(self):
        tmp,root=self.fixture("different version","assets/b0123securereattachv2/runtime/atomicdockledger.cs")
        with tmp:self.assertTrue(any("WOULD_OVERWRITE" in x for x in receiver_preflight(root,self.package)))
    def test_14_symlink_receiver_is_blocked_when_supported(self):
        tmp,root=self.fixture()
        with tmp:
            f=root/"Assets/legacy_link"
            f.parent.mkdir(parents=True)
            try:f.symlink_to(root/"bogus")
            except (OSError,NotImplementedError):
                self.skipTest("Platform cannot create unprivileged symlink")
            self.assertTrue(any("SYMLINK" in x for x in receiver_preflight(root,self.package)))
    def test_15_new_scratch_has_all_original_bytes(self):
        tmp,root=self.fixture()
        with tmp:
            stage=root/"proof"
            stage_scratch(self.package,stage)
            self.assertEqual(len([p for p in stage.rglob("*") if p.is_file()]),35)
            self.assertEqual((stage/MANIFEST).read_bytes(),self.package[MANIFEST])
            rollback_scratch(self.package,stage)
            self.assertFalse(stage.exists())
    def test_16_stage_refuses_existing_directory(self):
        tmp,root=self.fixture()
        with tmp:
            with self.assertRaises(Hold):stage_scratch(self.package,root)
    def test_17_rollback_refuses_dirty_file_and_keeps_evidence(self):
        tmp,root=self.fixture()
        with tmp:
            stage=root/"proof";stage_scratch(self.package,stage)
            (stage/MANIFEST).write_bytes(b"tampered")
            with self.assertRaises(Hold):rollback_scratch(self.package,stage)
            self.assertTrue(stage.exists())
    def test_18_rollback_refuses_foreign_file(self):
        tmp,root=self.fixture()
        with tmp:
            stage=root/"proof";stage_scratch(self.package,stage)
            (stage/"foreign.txt").write_text("not ours")
            with self.assertRaises(Hold):rollback_scratch(self.package,stage)
            self.assertTrue(stage.exists())
    def test_19_rollback_refuses_missing_file(self):
        tmp,root=self.fixture()
        with tmp:
            stage=root/"proof";stage_scratch(self.package,stage)
            (stage/MANIFEST).unlink()
            with self.assertRaises(Hold):rollback_scratch(self.package,stage)
            self.assertTrue(stage.exists())
    def test_20_do_not_infer_real_save_schema_from_source_package(self):
        self.assertNotIn("SaveData.schema.json",self.package)
        self.assertNotIn("Codex_ACK.json",self.package)
    def test_21_v2_guid_is_present_in_real_meta(self):
        meta=self.package["Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs.meta"].decode()
        self.assertIn("guid: "+V2_GUID,meta)
        self.assertNotIn("guid: "+OLD_W07_GUID,meta)
    def test_22_corrupted_zip_is_rejected_before_staging(self):
        tmp,root=self.fixture()
        with tmp:
            p=root/"source.zip";p.write_bytes(Path(self.archive).read_bytes()+b"malicious")
            with self.assertRaisesRegex(Hold,"WRONG_IMMUTABLE"):load_bundle(p,self.source)
    def test_23_original_live_repo_is_never_mutated(self):
        tmp,root=self.fixture()
        with tmp:
            prior=list(root.iterdir())
            self.assertEqual(receiver_preflight(root,self.package),[])
            self.assertEqual(list(root.iterdir()),prior)
if __name__=="__main__":unittest.main(verbosity=2)
