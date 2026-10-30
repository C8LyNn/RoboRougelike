#!/usr/bin/env python3
import importlib.util
from pathlib import Path
import sys
import tempfile
import unittest
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/"Tools/B01/B01-23/P17W10SecureReattachV2"))
import package
import receiver_preflight as gate

class ReceiverMigrationGate(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(prefix="b0123-w11-receiver-")
        self.root=Path(self.tmp.name)/"scratch"
        package.stage(package.build(),self.root)
    def tearDown(self):
        self.tmp.cleanup()
    def extra(self,rel,text):
        p=self.root/rel
        p.parent.mkdir(parents=True,exist_ok=True)
        p.write_text(text,encoding="utf-8")
        return p
    def test_actual_original_git_sources_staged_and_scanned(self):
        self.assertGreater(gate.validate(self.root),15)
    def test_legacy_w07_source_path_rejected(self):
        self.extra("Assets/B0123DebrisPlayground001/Runtime/Unused.cs","x")
        with self.assertRaisesRegex(ValueError,"LEGACY_COMPONENT_OR_SOURCE_PATH"):
            gate.validate(self.root)
    def test_legacy_w08_path_rejected(self):
        self.extra("Assets/B0123ReattachClearance001/Runtime/DebrisSafeReattachGate.cs","x")
        with self.assertRaisesRegex(ValueError,"LEGACY_COMPONENT_OR_SOURCE_PATH"):
            gate.validate(self.root)
    def test_legacy_w09_path_rejected(self):
        self.extra("Assets/B0123DockHardening001/Runtime/HardenedDockDemo.cs","x")
        with self.assertRaisesRegex(ValueError,"LEGACY_COMPONENT_OR_SOURCE_PATH"):
            gate.validate(self.root)
    def test_scene_w07_guid_rejected(self):
        self.extra("Assets/Host/Scene.unity","m_Script: {fileID: 11500000, guid: b017d070000000000000000000000008, type: 3}")
        with self.assertRaisesRegex(ValueError,"LEGACY_SCENE_PREFAB_SCRIPT_GUID"):
            gate.validate(self.root)
    def test_prefab_w08_guid_rejected(self):
        self.extra("Assets/Host/Robot.prefab","m_Script: {guid: b018d080000000000000000000000008}")
        with self.assertRaisesRegex(ValueError,"LEGACY_SCENE_PREFAB_SCRIPT_GUID"):
            gate.validate(self.root)
    def test_asset_w09_guid_rejected(self):
        self.extra("Assets/Host/Enemy.asset","guid: b019d090000000000000000000000008")
        with self.assertRaisesRegex(ValueError,"LEGACY_SCENE_PREFAB_SCRIPT_GUID"):
            gate.validate(self.root)
    def test_direct_old_game_class_reference_rejected(self):
        self.extra("Assets/Host/Gameplay.cs","public class Host { DebrisRigPlayground oldDemo; }")
        with self.assertRaisesRegex(ValueError,"LEGACY_HOST_CODE_REFERENCE"):
            gate.validate(self.root)
    def test_safe_v2_host_reference_allowed(self):
        self.extra("Assets/Host/Gameplay.cs","public class Host { B01.SecureReattachV2.SecureDebrisPlaygroundV2 v2; }")
        self.assertGreater(gate.validate(self.root),15)
    def test_missing_v2_controller_rejected(self):
        (self.root/gate.V2_SOURCE).unlink()
        with self.assertRaisesRegex(ValueError,"RECEIVER_MISSING_EXCLUSIVE_V2_CONTROLLER"):
            gate.validate(self.root)
    def test_wrong_transaction_version_rejected(self):
        p=self.root/gate.V2_SOURCE
        p.write_text(p.read_text().replace("if(!rig.Emitter.RearmAfterReattach(rig.Spec.PartId))","if(true)"))
        with self.assertRaisesRegex(ValueError,"RECEIVER_TRANSACTION_VERSION_NOT_V1_1"):
            gate.validate(self.root)
    def test_read_only_gate_never_cleans_unsafe_paths(self):
        p=self.extra("Assets/Host/Gameplay.cs","DebrisRigPlayground")
        with self.assertRaises(ValueError):gate.validate(self.root)
        self.assertTrue(p.exists())
    def test_original_rollback_exact_bytes(self):
        package.rollback(package.build(),self.root)
        self.assertFalse(self.root.exists())
    def test_rollback_refuses_unsafe_file(self):
        p=self.extra("Assets/Host/Gameplay.cs","DebrisRigPlayground")
        with self.assertRaises(ValueError):package.rollback(package.build(),self.root)
        self.assertTrue(p.exists())
    def test_rollback_refuses_foreign_empty_dir(self):
        p=self.root/"Assets/ForeignEmpty"
        p.mkdir()
        with self.assertRaises(ValueError):package.rollback(package.build(),self.root)
        self.assertTrue(p.exists())
if __name__=="__main__":unittest.main()
