import pathlib,subprocess,unittest
ROOT=pathlib.Path(__file__).resolve().parents[4]
BASE="f5db40bf293fe9582ec7c6fcb3515aeff6fcf937"
R3="Tools/B01/B01-62/P17W25StrictCanonicalPathR3/"
QA="Tools/B01/B01-23/P17W25Independent787StrictPathQA001/"
WORK=".github/workflows/b01-23-p17w25-independent-787-strict-path.yml"
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT,text=True).strip()
class ExactOriginal(unittest.TestCase):
 def test_exact_787_ancestor(self):subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 def test_787_original_store_and_session_unmodified(self):
  for f in ("CanonicalSingleAuthorityStoreV3R3.cs","CanonicalSlotSessionV3R3.cs"):
   p=R3+f;self.assertEqual(git("rev-parse","HEAD:"+p),git("rev-parse",BASE+":"+p))
 def test_frozen_510_556_729_unchanged(self):
  for p in ("Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs","Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs"):
   self.assertEqual(git("rev-parse","HEAD:"+p),git("rev-parse",BASE+":"+p))
 def test_case_sensitive_product_compares_exact_full_paths(self):
  s=(ROOT/(R3+"CanonicalSingleAuthorityStoreV3R3.cs")).read_text()
  self.assertIn("Path.GetFullPath(directory),StringComparison.Ordinal)",s)
  self.assertNotIn("Path.GetFullPath(directory),StringComparison.OrdinalIgnoreCase)",s)
 def test_qa_additions_only(self):
  files=git("diff","--name-only",BASE,"HEAD").splitlines()
  self.assertTrue(files)
  self.assertTrue(all(p.startswith(QA) or p==WORK for p in files),files)
if __name__=="__main__":unittest.main(verbosity=2)
