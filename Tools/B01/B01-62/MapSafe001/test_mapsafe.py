import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import mapsafe as m

def doc(items=None,version=2):
    if items is None: items=[{"kind":0,"x":0,"z":0,"rotation":0}]
    return json.dumps({"version":version,"items":items},separators=(",",":")).encode()

class RealDiskTests(unittest.TestCase):
    def setUp(self):
        t=tempfile.TemporaryDirectory(prefix="mapsafe-real-")
        self.addCleanup(t.cleanup)
        self.root=Path(t.name)
        self.maps,self.vault=self.root/"Maps",self.root/"Vault"
        self.maps.mkdir();self.vault.mkdir()
        self.target=self.maps/"Mapa 1.json"
        self.original=doc()
        self.target.write_bytes(self.original)

    def backup(self): return m.backup(self.maps,self.vault,"Mapa 1")

    def test_original_backup_bytes_and_checksum(self):
        s=self.backup()
        info,data=m.verify(self.vault,s["id"])
        self.assertEqual(self.original,data)
        self.assertEqual(m.digest(data),info["sha256"])
        self.assertEqual("manual",info["kind"])

    def test_missing_file_can_be_restored_without_replace(self):
        s=self.backup();self.target.unlink()
        r=m.restore(self.maps,self.vault,"Mapa 1",s["id"])
        self.assertFalse(r["replaced"])
        self.assertEqual(self.original,self.target.read_bytes())

    def test_existing_file_no_silent_replace(self):
        s=self.backup()
        with self.assertRaisesRegex(m.Refused,"requires"):
            m.restore(self.maps,self.vault,"Mapa 1",s["id"])
        self.assertEqual(self.original,self.target.read_bytes())

    def test_both_explicit_options_required(self):
        s=self.backup()
        for replace,expect in ((True,None),(False,m.digest(self.original))):
            with self.assertRaises(m.Refused):
                m.restore(self.maps,self.vault,"Mapa 1",s["id"],replace,expect)

    def test_explicit_replace_preserves_previous_bytes(self):
        s=self.backup()
        old=doc([{"kind":1,"x":2,"z":1,"rotation":0}])
        self.target.write_bytes(old)
        r=m.restore(self.maps,self.vault,"Mapa 1",s["id"],True,m.digest(old))
        self.assertEqual(self.original,self.target.read_bytes())
        info,payload=m.verify(self.vault,r["undo_snapshot_id"])
        self.assertEqual("pre-restore",info["kind"])
        self.assertEqual(old,payload)

    def test_corrupt_current_map_kept_as_forensic_copy(self):
        s=self.backup()
        self.target.write_bytes(b"bad_json")
        r=m.restore(self.maps,self.vault,"Mapa 1",s["id"],True,m.digest(b"bad_json"))
        info,data=m.verify(self.vault,r["undo_snapshot_id"])
        self.assertFalse(info["restorable"])
        self.assertEqual(b"bad_json",data)
        with self.assertRaises(m.Refused):
            m.restore(self.maps,self.vault,"Mapa 1",info["id"],True,m.digest(self.original))

    def test_wrong_expected_checksum_never_replaces(self):
        s=self.backup()
        new=doc([{"kind":1,"x":1,"z":2,"rotation":0}]);self.target.write_bytes(new)
        with self.assertRaisesRegex(m.Refused,"hash"):
            m.restore(self.maps,self.vault,"Mapa 1",s["id"],True,m.digest(self.original))
        self.assertEqual(new,self.target.read_bytes())

    def test_tampered_snapshot_rejected(self):
        s=self.backup()
        (self.vault/s["id"]/"payload.json").write_bytes(b"corrupt")
        with self.assertRaisesRegex(m.Refused,"hash mismatch"):
            m.verify(self.vault,s["id"])
        self.assertEqual(self.original,self.target.read_bytes())

    def test_alias_and_windows_device_name_rejected(self):
        for name in ("CON","nul","COM1","LPT9","../map"," x","x ","x.json","one/two"):
            with self.subTest(name=name), self.assertRaises(m.Refused):
                m.safe_name(name)
        with self.assertRaisesRegex(m.Refused,"alias"):
            m.target_path(self.maps,"mapa 1")

    def test_v1_scale_migration_rule(self):
        self.target.write_bytes(doc([{"kind":1,"x":0,"z":0,"rotation":0,"scaleX":99}],1))
        self.assertEqual(1,m.check_map(self.target.read_bytes()))
        self.backup()

    def test_v2_invalid_footprint_refused(self):
        self.target.write_bytes(doc([{"kind":1,"x":50,"z":0,"rotation":0,"scaleX":4}]))
        with self.assertRaises(m.Refused): self.backup()
        self.assertFalse(list(self.vault.iterdir()))

    def test_oversize_refused(self):
        self.target.write_bytes(b" "*(m.MAX_BYTES+1))
        with self.assertRaises(m.Refused): self.backup()
        self.assertFalse(list(self.vault.iterdir()))

    def test_failed_atomic_replace_does_not_change_original(self):
        s=self.backup()
        other=doc([{"kind":1,"x":3,"z":0,"rotation":0}]);self.target.write_bytes(other)
        with patch.object(m.os,"replace",side_effect=OSError("injected I/O error")):
            with self.assertRaises(OSError):
                m.restore(self.maps,self.vault,"Mapa 1",s["id"],True,m.digest(other))
        self.assertEqual(other,self.target.read_bytes())
        self.assertFalse(list(self.maps.glob("*.tmp")))

    def test_failed_no_clobber_publish_does_not_create_target(self):
        s=self.backup();self.target.unlink()
        with patch.object(m.os,"link",side_effect=OSError("no links")):
            with self.assertRaises(OSError): m.restore(self.maps,self.vault,"Mapa 1",s["id"])
        self.assertFalse(self.target.exists())

    def test_symlink_source_rejected_if_os_allows(self):
        self.target.unlink()
        real=self.root/"actual.json";real.write_bytes(self.original)
        try: self.target.symlink_to(real)
        except (OSError,NotImplementedError): self.skipTest("OS symlink privilege unavailable")
        with self.assertRaises(m.Refused): self.backup()

    def test_vault_nested_inside_maps_refused(self):
        nested=self.maps/"Vault";nested.mkdir()
        with self.assertRaises(m.Refused): m.vault_dir(nested,self.maps)

    def test_restore_wrong_map_name_refused(self):
        s=self.backup()
        with self.assertRaises(m.Refused):
            m.restore(self.maps,self.vault,"Another",s["id"])

if __name__=="__main__": unittest.main(verbosity=2)
