#!/usr/bin/env python3
"""Independent malicious/mixed receiver fixtures using real original Git bytes.
Fixtures are under TemporaryDirectory; this suite never modifies a real receiver.
"""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
import preflight as p

class ReceiverMigrations(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.paths=p.assert_git_owner()
        cls.guid=p.trusted_guids()
        cls.manifest=p.make_manifest()

    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(prefix="b0161-w12-scratch-")
        self.addCleanup(self.tmp.cleanup)
        self.receiver=Path(self.tmp.name)/"staged-receiver"
        self.receiver.mkdir()
        for path in self.paths:
            file=self.receiver/path
            file.parent.mkdir(parents=True,exist_ok=True)
            file.write_bytes(p.original(p.SELECTED,path))
        (self.receiver/p.MANIFEST).write_text(json.dumps(self.manifest,sort_keys=True)+"\n")
        self.scene=self.receiver/p.SCENE
        self.scene.parent.mkdir(parents=True,exist_ok=True)
        self._scene_with(self.guid["selected"])
    def _scene_with(self,*ids):
        script="".join(
            "--- !u!114 &"+str(100+i)+"\nMonoBehaviour:\n  m_Script: {fileID: 11500000, guid: "+g+", type: 3}\n"
            for i,g in enumerate(ids))
        self.scene.write_text("%YAML 1.1\n"+script)
    def _write(self,path,data):
        f=self.receiver/path
        f.parent.mkdir(parents=True,exist_ok=True)
        f.write_bytes(data if isinstance(data,bytes) else data.encode())
        return f
    def _hold(self,expected):
        with self.assertRaisesRegex(ValueError,expected):
            p.validate_migration(self.receiver)
    def test_genuine_original_source_pin_34(self):
        self.assertEqual(len(self.paths),34)
        self.assertEqual(p.git_blob(p.original(p.SELECTED,p.ASSET+"/Runtime/AtomicDockLedger.cs")),
                         "b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9")
    def test_actual_selected_510_receivable(self):
        r=p.validate_migration(self.receiver)
        self.assertEqual(r["source_sha"],p.SELECTED)
        self.assertEqual(r["files_modified"],0)
        self.assertEqual(r["G1"],"NOT_RUN")
    def test_read_only_even_on_success(self):
        before={f.relative_to(self.receiver).as_posix():hashlib.sha256(f.read_bytes()).hexdigest()
                for f in self.receiver.rglob("*") if f.is_file()}
        p.validate_migration(self.receiver)
        after={f.relative_to(self.receiver).as_posix():hashlib.sha256(f.read_bytes()).hexdigest()
               for f in self.receiver.rglob("*") if f.is_file()}
        self.assertEqual(before,after)
    def test_actual_alternative_511_host_detected_despite_identical_meta_guid(self):
        host=p.ASSET+"/Runtime/SecureDebrisPlaygroundV2.cs"
        alt=p.original(p.ALTERNATIVE,host)
        self.assertNotEqual(alt,p.original(p.SELECTED,host))
        self._write(host,alt)
        self._hold("RECEIVER_SOURCE_DRIFT_OR_511_MIX")
    def test_missing_r2_atomic_ledger(self):
        (self.receiver/p.ASSET/"Runtime/AtomicDockLedger.cs").unlink()
        self._hold("MISSING_OR_SYMLINK_SOURCE")
    def test_wrong_511_manifest_sha(self):
        m=dict(self.manifest)
        m["source_sha"]=p.ALTERNATIVE
        self._write(p.MANIFEST,json.dumps(m))
        self._hold("WRONG_VARIANT_MANIFEST")
    def test_forged_native_or_codex_status(self):
        m=dict(self.manifest);m["native_unity"]="PASS";m["codex_ack"]=True
        self._write(p.MANIFEST,json.dumps(m))
        self._hold("WRONG_VARIANT_MANIFEST")
    def test_historical_w07_script_in_scene(self):
        self._scene_with(self.guid["selected"],self.guid["W07"])
        self._hold("LEGACY_MONOBEHAVIOUR_REACHABLE")
    def test_historical_w08_gate_in_prefab(self):
        self._write("Assets/Gameplay/OldGarage.prefab",
                    "--- !u!114 &2\nMonoBehaviour:\n  m_Script: {fileID: 11500000, guid: "+self.guid["W08"]+", type: 3}")
        self._hold("LEGACY_MONOBEHAVIOUR_REACHABLE")
    def test_historical_w09_in_scriptable_asset(self):
        self._write("Assets/Data/LegacyMount.asset",
                    "m_Script: {fileID: 11500000, guid: "+self.guid["W09"]+", type: 3}")
        self._hold("LEGACY_MONOBEHAVIOUR_REACHABLE")
    def test_old_w07_source_present_even_if_not_instantiated(self):
        legacy=p.KNOWN_META["W07"][1][:-5]
        self._write(legacy,p.original(p.OLDER_W07,legacy))
        self._hold("LEGACY_COMPONENT_SOURCE_PRESENT")
    def test_old_w08_w09_meta_even_without_source(self):
        self._write(p.KNOWN_META["W09"][1],
                    p.original(p.OLDER_W09,p.KNOWN_META["W09"][1]))
        self._hold("LEGACY_COMPONENT_SOURCE_PRESENT")
    def test_duplicate_selected_unity_guid(self):
        self._write("Assets/ThirdParty/ForgedComponent.cs.meta",
                    "fileFormatVersion: 2\nguid: "+self.guid["selected"]+"\n")
        self._hold("DUPLICATE_UNITY_GUID")
    def test_no_target_scene_is_not_migrated(self):
        self.scene.unlink()
        self._hold("MISSING_OR_SYMLINK_SOURCE")
    def test_empty_target_scene_is_not_migrated(self):
        self._scene_with()
        self._hold("SELECTED_TARGET_SCENE_REQUIRES_EXACTLY_ONE")
    def test_duplicate_selected_components(self):
        self._scene_with(self.guid["selected"],self.guid["selected"])
        self._hold("SELECTED_TARGET_SCENE_REQUIRES_EXACTLY_ONE")
    def test_unreadable_binary_scene_is_hard_hold(self):
        self._write(p.SCENE,b"\xff\xfe\x00\x80")
        self._hold("BINARY_TARGET_SCENE")
    def test_dynamic_old_w07_component_factory_denied(self):
        self._write("Assets/Gameplay/MountHook.cs",
                    "class MountHook { void Init(){ gameObject.AddComponent<DebrisRigPlayground>(); } }")
        self._hold("DYNAMIC_LEGACY_COMPONENT_CALLER")
    def test_foreign_v2_runtime_path_denied(self):
        self._write(p.ASSET+"/Runtime/InjectedUnowned.cs","public class Insecure {}")
        self._hold("FOREIGN_V2_ASSET_SOURCE")
    def test_no_false_reject_for_unrelated_prefab(self):
        self._write("Assets/Gameplay/Camera.prefab",
                    "m_Script: {fileID: 11500000, guid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa, type: 3}")
        self.assertEqual(p.validate_migration(self.receiver)["G0"],"RECEIVER_PREFLIGHT_ONLY")
    def test_selected_meta_corruption_is_rejected(self):
        self._write(p.ASSET+"/Runtime/SecureDebrisPlaygroundV2.cs.meta",
                    "fileFormatVersion: 2\nguid: ffffffffffffffffffffffffffffffff\n")
        self._hold("RECEIVER_SOURCE_DRIFT_OR_511_MIX")
    def test_foreign_asset_rollback_never_modified_by_gate(self):
        old=self._write("Assets/Gameplay/GarageNotes.txt","KEEP")
        self._scene_with(self.guid["selected"],self.guid["W07"])
        self._hold("LEGACY_MONOBEHAVIOUR_REACHABLE")
        self.assertEqual(old.read_text(),"KEEP")
    def test_wrong_manifest_file_list_denied(self):
        m=dict(self.manifest)
        m["files"]=m["files"][:-1]
        self._write(p.MANIFEST,json.dumps(m))
        self._hold("NOT_EXACT_SELECTED_34_PATH_MANIFEST")
    def test_symlink_selected_source_denied(self):
        target=self.receiver/p.ASSET/"Runtime/SecureDockPolicy.cs"
        destination=target.with_suffix(".cs.backup")
        target.rename(destination)
        try:
            target.symlink_to(destination)
        except (OSError,NotImplementedError):
            self.skipTest("symlink unavailable in runner")
        self._hold("MISSING_OR_SYMLINK_SOURCE")

if __name__=="__main__":
    unittest.main(verbosity=2)
