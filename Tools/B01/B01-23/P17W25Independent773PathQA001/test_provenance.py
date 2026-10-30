import pathlib,subprocess,unittest
ROOT=pathlib.Path(__file__).resolve().parents[4]
BASE="3aaaec4027ee246fbfb6f209179f0e9507363473"
PATH="Tools/B01/B01-62/P17W24CanonicalPathFixR2/"
QA="Tools/B01/B01-23/P17W25Independent773PathQA001/"
WORK=".github/workflows/b01-23-p17w25-independent-773-path-qa.yml"
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT,text=True).strip()
class OriginalQA(unittest.TestCase):
 def test_parent_exact_original_773(self):
  subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 def test_original_store_and_session_unmodified(self):
  for p in (PATH+"CanonicalSingleAuthorityStoreV3R2.cs",PATH+"CanonicalSlotSessionV3R2.cs"):
   self.assertEqual(git("rev-parse","HEAD:"+p),git("rev-parse",BASE+":"+p))
 def test_original_729_556_510_still_unmodified(self):
  for p in ("Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs","Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs","Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs"):
   self.assertEqual(git("rev-parse","HEAD:"+p),git("rev-parse",BASE+":"+p))
 def test_qa_only_source_paths(self):
  changed=git("diff","--name-only",BASE,"HEAD").splitlines()
  self.assertTrue(changed)
  self.assertTrue(all(x.startswith(QA) or x==WORK for x in changed),changed)
 def test_original_773_case_insensitive_validation_source(self):
  x=(ROOT/(PATH+"CanonicalSingleAuthorityStoreV3R2.cs")).read_text()
  self.assertIn("Path.GetFullPath(directory),StringComparison.OrdinalIgnoreCase",x)
if __name__=="__main__":unittest.main(verbosity=2)
