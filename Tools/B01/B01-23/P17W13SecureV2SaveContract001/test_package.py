import io,sys,unittest,zipfile,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import package as pack
class OriginalPackage(unittest.TestCase):
 @classmethod
 def setUpClass(cls):cls.raw=pack.original()
 def mutate(self,rewrite):
  with zipfile.ZipFile(io.BytesIO(self.raw)) as src:
   entries=[(p,src.read(p)) for p in src.namelist()]
  entries=rewrite(entries)
  out=io.BytesIO()
  with zipfile.ZipFile(out,"w") as z:
   for p,b in entries:z.writestr(pack.info(p),b)
  return out.getvalue()
 def reject(self,raw):
  with self.assertRaises((ValueError,KeyError,zipfile.BadZipFile)):pack.verify(raw)
 def test_01_original_bytes(self):self.assertTrue(pack.verify(self.raw))
 def test_02_deterministic(self):self.assertEqual(pack.original(),self.raw)
 def test_03_path_mismatch(self):
  self.reject(self.mutate(lambda x:x+[("Assets/Unsafe.cs",b"bad")]))
 def test_04_missing_member(self):
  self.reject(self.mutate(lambda x:x[:-1]))
 def test_05_duplicate_member(self):
  self.reject(self.mutate(lambda x:x+[x[0]]))
 def test_06_zip_traversal(self):
  self.reject(self.mutate(lambda x:x+[("../entry",b"x")]))
 def test_07_malformed_source(self):
  self.reject(self.mutate(lambda x:[(p,b+b"bad" if p.endswith("StableIdSnapshotCodec.cs") else b) for p,b in x]))
 def test_08_fake_native(self):
  self.reject(self.mutate(lambda x:[(p,b.replace(b'"unity_native":"NOT_RUN"',b'"unity_native":"PASS"') if p=="PACKAGE_MANIFEST.json" else b) for p,b in x]))
 def test_09_fake_codex_ack(self):
  self.reject(self.mutate(lambda x:[(p,b.replace(b'"codex_ack":false',b'"codex_ack":true') if p=="PACKAGE_MANIFEST.json" else b) for p,b in x]))
 def test_10_fake_canonical_rehydration(self):
  self.reject(self.mutate(lambda x:[(p,b.replace(b'"canonical_save_restored":false',b'"canonical_save_restored":true') if p=="PACKAGE_MANIFEST.json" else b) for p,b in x]))
 def test_11_wrong_selected_candidate(self):
  self.reject(self.mutate(lambda x:[(p,b.replace(pack.SOURCE.encode(),b"baf65f2664e38f3b9c38815c3f07285f8107c1b6") if p=="PACKAGE_MANIFEST.json" else b) for p,b in x]))
if __name__=="__main__":unittest.main()
