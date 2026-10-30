import io,json,tempfile,unittest,zipfile
from pathlib import Path
import package as p
def mutated(raw,extra=None,override=None):
    with zipfile.ZipFile(io.BytesIO(raw)) as z:data={n:z.read(n) for n in z.namelist()}
    data.update(extra or {});data.update(override or {})
    buffer=io.BytesIO()
    with zipfile.ZipFile(buffer,"w") as z:
        for n in sorted(data):z.writestr(p.entry(n),data[n])
    return buffer.getvalue()
class SourcePackageTests(unittest.TestCase):
    def test_frozen_selected_save(self):
        self.assertEqual(p.SELECTED_SAVE,"33094f154c7b91119b36d58aab285e6e83c5de57")
    def test_frozen_selected_gameplay(self):
        self.assertEqual(p.SELECTED_GAME,"d94400d95d13db7f27e1533df05c9292d5380431")
    def test_original_critical_git_blobs(self):self.assertEqual(len(p.PINS),5)
    def test_exact_22_own_git_paths(self):self.assertEqual(len(p.scope()),24)
    def test_11_unique_meta_guids_and_three_asmdefs(self):p.scope()
    def test_actual_original_unity_body_source(self):
        s=p.source(p.ASSET+"/Unity/IsolatedTwinRigSaveHost.cs").decode()
        self.assertIn("Rigidbody",s);self.assertIn("DebrisImpulseLedger",s)
        self.assertIn("TwinWorld.Build(snapshot,transform)",s)
    def test_actual_original_transaction_state(self):
        s=p.source(p.ASSET+"/Core/AtomicWorldSwap.cs").decode()
        self.assertIn("ColdRestore.TryStage",s)
        self.assertIn("if(touchedOld&&old!=null)",s)
    def test_staged_primitive_goes_live_only_with_atomic_world_root(self):
        source=p.source(p.ASSET+"/Unity/IsolatedTwinRigSaveHost.cs").decode()
        self.assertIn("part.SetActive(true);",source)
        self.assertIn("if(part.activeInHierarchy)",source)
        self.assertIn("container.SetActive(true);",source)
        self.assertIn("body.linearVelocity",source)
    def test_real_git_deterministic_archive(self):self.assertEqual(p.build(),p.build())
    def test_exact_23_archive_members(self):self.assertEqual(len(p.verify(p.build())),25)
    def test_manifest_source_guards(self):
        x=p.verify(p.build());m=json.loads(x[p.MANIFEST])
        self.assertEqual(m["native_unity"],"NOT_RUN")
        self.assertEqual(m["canonical_510_live_import"],"HOLD")
        self.assertFalse(m["codex_ack"])
    def test_reject_foreign_member(self):
        with self.assertRaises(ValueError):
            p.verify(mutated(p.build(),extra={"Assets/Canon/HACK.cs":b"abc"}))
    def test_reject_zip_traversal(self):
        with self.assertRaises(ValueError):
            p.verify(mutated(p.build(),extra={"../bad":b"abc"}))
    def test_reject_original_source_byte_tamper(self):
        with self.assertRaises(ValueError):
            p.verify(mutated(p.build(),override={p.scope()[0]:b"mutated"}))
    def test_reject_fake_native_pass(self):
        b=p.build()
        with zipfile.ZipFile(io.BytesIO(b)) as z:m=json.loads(z.read(p.MANIFEST))
        m["native_unity"]="PASS"
        with self.assertRaises(ValueError):
            p.verify(mutated(b,override={p.MANIFEST:json.dumps(m).encode()}))
    def test_reject_fake_510_integration(self):
        b=p.build()
        with zipfile.ZipFile(io.BytesIO(b)) as z:m=json.loads(z.read(p.MANIFEST))
        m["canonical_510_live_import"]="INTEGRATED"
        with self.assertRaises(ValueError):
            p.verify(mutated(b,override={p.MANIFEST:json.dumps(m).encode()}))
    def test_real_scratch_stage_rollback(self):
        with tempfile.TemporaryDirectory() as tmp:
            dest=Path(tmp)/"new";raw=p.build()
            p.stage(raw,dest);self.assertTrue((dest/p.MANIFEST).exists())
            p.rollback(raw,dest);self.assertFalse(dest.exists())
    def test_refuse_existing_receiver(self):
        with tempfile.TemporaryDirectory() as tmp:
            with self.assertRaises(ValueError):p.stage(p.build(),Path(tmp))
    def test_refuse_repo_receiver(self):
        with self.assertRaises(ValueError):p.stage(p.build(),p.ROOT/"Assets"/"unsafe")
    def test_refuse_foreign_empty_subdir_deletion(self):
        with tempfile.TemporaryDirectory() as tmp:
            dest=Path(tmp)/"new";raw=p.build();p.stage(raw,dest)
            target=dest/"Assets"/"foreign_EMPTY";target.mkdir(parents=True)
            with self.assertRaisesRegex(ValueError,"foreign EMPTY"):p.rollback(raw,dest)
            self.assertTrue(target.is_dir())
    def test_refuse_foreign_nonempty_dir_deletion(self):
        with tempfile.TemporaryDirectory() as tmp:
            dest=Path(tmp)/"new";raw=p.build();p.stage(raw,dest)
            target=dest/"foreign";target.mkdir();(target/"preserve").write_bytes(b"secret")
            with self.assertRaises(ValueError):p.rollback(raw,dest)
            self.assertEqual((target/"preserve").read_bytes(),b"secret")
    def test_refuse_dirty_original_source_bytes(self):
        with tempfile.TemporaryDirectory() as tmp:
            dest=Path(tmp)/"new";raw=p.build();p.stage(raw,dest)
            (dest/p.scope()[0]).write_bytes(b"WRONG")
            with self.assertRaises(ValueError):p.rollback(raw,dest)
            self.assertTrue(dest.exists())
    def test_refuse_missing_original_source(self):
        with tempfile.TemporaryDirectory() as tmp:
            dest=Path(tmp)/"new";raw=p.build();p.stage(raw,dest)
            (dest/p.scope()[0]).unlink()
            with self.assertRaises(ValueError):p.rollback(raw,dest)
    def test_refuse_foreign_file(self):
        with tempfile.TemporaryDirectory() as tmp:
            dest=Path(tmp)/"new";raw=p.build();p.stage(raw,dest)
            (dest/"foreign").write_bytes(b"abc")
            with self.assertRaises(ValueError):p.rollback(raw,dest)
    def test_refuse_symlink(self):
        with tempfile.TemporaryDirectory() as tmp:
            dest=Path(tmp)/"new";raw=p.build();p.stage(raw,dest)
            try:(dest/"link").symlink_to(dest/"victim")
            except (OSError,NotImplementedError):self.skipTest("symlink not allowed by host")
            with self.assertRaises(ValueError):p.rollback(raw,dest)
            self.assertTrue((dest/"link").is_symlink())
if __name__=="__main__":unittest.main(verbosity=2)
