import io,json,unittest,tempfile,zipfile
from pathlib import Path
import package as p
class P(unittest.TestCase):
    def test_01_frozen_original_sources(self):self.assertEqual(len(p.PINS),2);p.paths()
    def test_02_exact_owner_scope(self):self.assertEqual(len(p.paths()),16)
    def test_03_meta_guids(self):p.paths()
    def test_04_zip_deterministic(self):self.assertEqual(p.build(),p.build())
    def test_05_zip_members(self):
        with zipfile.ZipFile(io.BytesIO(p.build())) as z:self.assertEqual(len(z.namelist()),17)
    def test_w21_owned_stage_leak_guard_must_be_authorized(self):
        code=p.src(p.ASSET+"/Core/CanonicalWorldGateway.cs").decode()
        self.assertIn("ICanonicalStageOwnership",code)
        self.assertIn("CleanupUnpublishedAndVerifyDestroyed()",code)
        self.assertIn("ownedStage=factory.BeginOwnedStage(snap,cold)",code)
        self.assertIn("if(!safe)",code)
    def test_w21_681_v1_frozen_682_alt_hold(self):
        self.assertEqual(p.FROZEN_API_V1,
            "eed2e39636edfd8b8db754c426db452164cbf9d7")
    def test_w22_selected_original_api_v2_sha_pinned(self):
        self.assertEqual(p.FROZEN_API_V2,
            "809f82575c638e1b4d2806b5f417bddab6c0871a")
    def test_06_verified_manifest(self):self.assertEqual(p.verify(p.build())["version"],"3.0.0")
    def mutate(self,extra=None,alter=None):
        with zipfile.ZipFile(io.BytesIO(p.build())) as z:items={n:z.read(n) for n in z.namelist()}
        items.update(extra or {});items.update(alter or {})
        out=io.BytesIO()
        with zipfile.ZipFile(out,"w") as z:
            for n,content in items.items():z.writestr(p.entry(n),content)
        return out.getvalue()
    def test_07_reject_foreign_zip_member(self):
        with self.assertRaises(ValueError):p.verify(self.mutate(extra={"Assets/Other/Alien.txt":b"hi"}))
    def test_08_reject_zip_traversal(self):
        with self.assertRaises(ValueError):p.verify(self.mutate(extra={"../escape":b"hi"}))
    def test_09_reject_changed_source(self):
        n=p.paths()[0]
        with self.assertRaises(ValueError):p.verify(self.mutate(alter={n:b"malicious"}))
    def test_10_reject_fake_native(self):
        with zipfile.ZipFile(io.BytesIO(p.build())) as z:m=json.loads(z.read(p.MANIFEST))
        m["native"]="PASS"
        with self.assertRaises(ValueError):
            p.verify(self.mutate(alter={p.MANIFEST:json.dumps(m).encode()}))
    def test_11_reject_fake_codex_ack(self):
        with zipfile.ZipFile(io.BytesIO(p.build())) as z:m=json.loads(z.read(p.MANIFEST))
        m["codex_ack"]=True
        with self.assertRaises(ValueError):
            p.verify(self.mutate(alter={p.MANIFEST:json.dumps(m).encode()}))
    def test_12_stage_rollback_leaves_foreign_empty_dir(self):
        with tempfile.TemporaryDirectory() as t:
            target=Path(t)/"new_receiver";raw=p.build();p.stage(raw,target)
            alien=target/"foreign_EMPTY";alien.mkdir()
            p.rollback(raw,target)
            self.assertTrue(alien.is_dir())
    def test_13_stage_refuses_occupied_receiver(self):
        with tempfile.TemporaryDirectory() as t:
            target=Path(t)/"existing";target.mkdir()
            with self.assertRaises(ValueError):p.stage(p.build(),target)
    def test_14_rollback_refuses_changed_bytes(self):
        with tempfile.TemporaryDirectory() as t:
            target=Path(t)/"new";raw=p.build();p.stage(raw,target)
            first=p.paths()[0];(target/first).write_text("altered")
            with self.assertRaises(ValueError):p.rollback(raw,target)
            self.assertEqual((target/first).read_text(),"altered")
    def test_15_rollback_refuses_missing_owned_file(self):
        with tempfile.TemporaryDirectory() as t:
            target=Path(t)/"new";raw=p.build();p.stage(raw,target)
            (target/p.paths()[0]).unlink()
            with self.assertRaises(ValueError):p.rollback(raw,target)
if __name__=="__main__":unittest.main()
