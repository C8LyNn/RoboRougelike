import io
import json
from pathlib import Path
import tempfile
import unittest
import warnings
import zipfile
import package as p

def mutate(raw,changes=None,extra=None):
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        files={n:z.read(n) for n in z.namelist()}
    files.update(changes or {})
    files.update(extra or {})
    b=io.BytesIO()
    with zipfile.ZipFile(b,"w") as z:
        for k in sorted(files):z.writestr(p.zinfo(k),files[k])
    return b.getvalue()

class SecureV2PhysicalSourceTests(unittest.TestCase):
    def test_scope_original_source(self):
        self.assertEqual(len(p.scope()),27)
    def test_real_guard_contract(self):
        p.guard_contract()
    def test_deterministic_original_zip(self):
        self.assertEqual(p.build(),p.build())
    def test_original_manifest_sha_bytes(self):
        raw=p.build()
        self.assertEqual(len(p.verify(raw)),len(p.scope())+1)
    def test_original_legacy_named_public_api_mutation_rejected(self):
        path=p.ASSET+"/Runtime/SecureDebrisPlaygroundV2.cs"
        original=p.blob(path).decode()
        bad=original.replace("public bool Reattach(int index) => DockGate().Reattach(index);",
                             "public bool Reattach(int index) => CommitDock(index);")
        self.assertNotEqual(original,bad)
        # Source contract must reject changed actual Git independent of archived manifest.
        # This is an intentional mutation-negative, not a fabricated Unity native test.
        self.assertNotIn("public bool Reattach(int index) => DockGate().Reattach(index);",bad)
        self.assertIn("public bool Reattach(int index) => DockGate().Reattach(index);",original)
        self.assertRaises(ValueError,p.verify,mutate(p.build(),{path:bad.encode()}))
    def test_original_colliders_all_layers(self):
        content=p.blob(p.ASSET+"/Runtime/SecureDebrisPlaygroundV2.cs").decode()
        self.assertIn("SecureDockPolicy.AllLayers,QueryTriggerInteraction.Ignore",content)
        self.assertNotIn("collisionLayers",content)
    def test_frozen_w06_sha_pinned(self):
        self.assertEqual(p.PARENT,"cb2b3a76e84266e2730fffc4ab048c1039f0652f")
        self.assertEqual(len(p.PINS),3)
    def test_corrupt_original_source_rejected(self):
        raw=p.build()
        path=p.scope()[0]
        self.assertRaises(ValueError,p.verify,mutate(raw,{path:b"evil"}))
    def test_foreign_codex_entry_rejected(self):
        raw=p.build()
        self.assertRaises(ValueError,p.verify,mutate(raw,extra={"Assets/R3/Unowned.cs":b"unsafe"}))
    def test_traversal_rejected(self):
        self.assertRaises(ValueError,p.verify,mutate(p.build(),extra={"../unsafe.txt":b"bad"}))
    def test_forged_native_unity_rejected(self):
        raw=p.build()
        with zipfile.ZipFile(io.BytesIO(raw)) as z:m=json.loads(z.read(p.MANIFEST))
        m["native_unity"]="PASS"
        self.assertRaises(ValueError,p.verify,mutate(raw,{p.MANIFEST:json.dumps(m).encode()}))
    def test_forged_codex_ack_rejected(self):
        raw=p.build()
        with zipfile.ZipFile(io.BytesIO(raw)) as z:m=json.loads(z.read(p.MANIFEST))
        m["codex_ack"]=True
        self.assertRaises(ValueError,p.verify,mutate(raw,{p.MANIFEST:json.dumps(m).encode()}))
    def test_forged_parent_rejected(self):
        raw=p.build()
        with zipfile.ZipFile(io.BytesIO(raw)) as z:m=json.loads(z.read(p.MANIFEST))
        m["parent_w06_sha"]="0"*40
        self.assertRaises(ValueError,p.verify,mutate(raw,{p.MANIFEST:json.dumps(m).encode()}))
    def test_safe_staging_and_rollback(self):
        raw=p.build()
        with tempfile.TemporaryDirectory() as scratch:
            target=Path(scratch)/"new"
            p.stage(raw,target)
            self.assertTrue(target.is_dir())
            p.rollback(raw,target)
            self.assertFalse(target.exists())
    def test_no_overwrite(self):
        with tempfile.TemporaryDirectory() as scratch:
            self.assertRaises(ValueError,p.stage,p.build(),Path(scratch))
    def test_no_stage_in_repo(self):
        self.assertRaises(ValueError,p.stage,p.build(),p.ROOT/"dist"/"not-created")
    def test_dirty_file_rollback_refused(self):
        raw=p.build()
        with tempfile.TemporaryDirectory() as scratch:
            target=Path(scratch)/"new"
            p.stage(raw,target)
            (target/p.scope()[0]).write_bytes(b"mutated")
            self.assertRaises(ValueError,p.rollback,raw,target)
            self.assertTrue(target.exists())
    def test_foreign_file_rollback_refused(self):
        raw=p.build()
        with tempfile.TemporaryDirectory() as scratch:
            target=Path(scratch)/"new"
            p.stage(raw,target)
            (target/"important.txt").write_text("keep")
            self.assertRaises(ValueError,p.rollback,raw,target)
            self.assertTrue((target/"important.txt").exists())
    def test_missing_file_rollback_refused(self):
        raw=p.build()
        with tempfile.TemporaryDirectory() as scratch:
            target=Path(scratch)/"new"
            p.stage(raw,target)
            (target/p.scope()[0]).unlink()
            self.assertRaises(ValueError,p.rollback,raw,target)
            self.assertTrue(target.exists())
    def test_duplicate_member_rejected(self):
        raw=p.build()
        with zipfile.ZipFile(io.BytesIO(raw)) as z:
            items={name:z.read(name) for name in z.namelist()}
        buff=io.BytesIO()
        with zipfile.ZipFile(buff,"w") as z:
            for name in sorted(items):z.writestr(p.zinfo(name),items[name])
            with warnings.catch_warnings():
                warnings.simplefilter("ignore",UserWarning)
                z.writestr(p.zinfo(p.MANIFEST),items[p.MANIFEST])
        self.assertRaises(ValueError,p.verify,buff.getvalue())

if __name__=="__main__":unittest.main(verbosity=2)
