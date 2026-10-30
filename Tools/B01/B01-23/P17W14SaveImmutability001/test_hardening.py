import importlib.util
import io
import json
import zipfile
from pathlib import Path
import unittest
OLD=Path(__file__).resolve().parents[1]/"P17W13DetachedSaveContract001"/"package.py"
spec=importlib.util.spec_from_file_location("b01_w14_package", OLD)
p=importlib.util.module_from_spec(spec)
spec.loader.exec_module(p)
class SelectedV2ImmutabilityPackageTests(unittest.TestCase):
    def test_only_frozen_gameplay_parent(self):
        self.assertEqual(p.PIN,"d94400d95d13db7f27e1533df05c9292d5380431")
    def test_original_w13_source_preserved(self):
        self.assertEqual(p.PARENT_W13,"6da2c523e03766cd5a6deaed0cb3cccf8765354b")
    def test_exact_scope(self):
        self.assertEqual(len(p.scope()),15)
    def test_b01w14_source_not_replacement_gameplay(self):
        self.assertTrue(all(x.startswith("Assets/B0123DetachedSaveContract001") or
            x.startswith("Tools/B01/B01-23/P17W13DetachedSaveContract001") or
            x.startswith("Tools/B01/B01-23/P17W14SaveImmutability001") or
            x.startswith(".github/workflows/b01-23-p17w13-") or
            x.startswith(".github/workflows/b01-23-p17w14-")
            for x in p.scope()))
    def test_new_original_binary_schema2(self):
        s=p.source(p.ASSET+"/Runtime/DetachedSnapshotContract.cs").decode()
        self.assertIn('private const int Version=2;',s)
        self.assertIn('B01SV14!',s)
    def test_original_live_w06_ledger_not_returned(self):
        s=p.source(p.ASSET+"/Runtime/DetachedSnapshotContract.cs").decode()
        self.assertNotIn("public DebrisImpulseLedger GetLedger(",s)
        self.assertNotIn("return rebuilt[slot];",s)
        self.assertIn("public bool IsConsumed(int slot,long id)",s)
        self.assertIn("public int ConsumedCount(int slot)",s)
    def test_original_published_snapshot_copy(self):
        s=p.source(p.ASSET+"/Runtime/DetachedSnapshotContract.cs").decode()
        self.assertIn("private readonly PersistedPart[] parts;",s)
        self.assertIn("public PersistedPart[] Parts => (PersistedPart[])parts.Clone();",s)
        self.assertIn("this.parts[i]=parts[i];",s)
    def test_red_witness_testcases_present(self):
        s=p.source(p.TOOLS+"Program.cs").decode()
        self.assertIn("W14 #550 original input array mutation",s)
        self.assertIn("W14 #550 returned Parts array mutation",s)
        self.assertIn("W14 #550 original mutable DebrisImpulseLedger",s)
    def test_git_archive_determinism(self):
        self.assertEqual(p.build(),p.build())
    def test_selected_manifest_v2(self):
        data=p.verify(p.build())
        m=json.loads(data[p.MANIFEST])
        self.assertEqual(m["version"],"2.0.0")
        self.assertEqual(m["supersedes_w13_sha"],p.PARENT_W13)
        self.assertEqual(m["selected_secure_v2_sha"],p.PIN)
        self.assertEqual(m["native_unity"],"NOT_RUN")
        self.assertEqual(m["savegame_host"],"NOT_INTEGRATED")
    def test_real_exact_original_git_blobs(self):
        self.assertEqual(len(p.PINS),4)
        p.scope()
    def test_unique_original_4_unity_guid_and_asmdef(self):
        self.assertEqual(len([x for x in p.scope() if x.endswith(".meta")]),4)
        self.assertEqual(len([x for x in p.scope() if x.endswith(".asmdef")]),1)
    def test_original_zip_v2_contains_actual_host_source(self):
        source=p.source(p.ASSET+"/Runtime/DetachedSnapshotContract.cs")
        self.assertEqual(p.verify(p.build())[p.ASSET+"/Runtime/DetachedSnapshotContract.cs"],source)
    def test_physical_archive_manifest_tamper_denied(self):
        raw=p.build()
        with zipfile.ZipFile(io.BytesIO(raw)) as z:
            blob={name:z.read(name) for name in z.namelist()}
        m=json.loads(blob[p.MANIFEST]);m["version"]="1.0.0"
        blob[p.MANIFEST]=json.dumps(m).encode()
        buff=io.BytesIO()
        with zipfile.ZipFile(buff,"w") as z:
            for name in sorted(blob):z.writestr(p.zinfo(name),blob[name])
        with self.assertRaises(ValueError):p.verify(buff.getvalue())
if __name__=="__main__":
    unittest.main(verbosity=2)
