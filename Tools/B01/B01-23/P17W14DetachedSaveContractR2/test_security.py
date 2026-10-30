import sys,unittest
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/"P17W13DetachedSaveContract001"))
import package as p
class SelectedSaveR2(unittest.TestCase):
 def test_selected_game_sha(self):self.assertEqual(p.PIN,"d94400d95d13db7f27e1533df05c9292d5380431")
 def test_manifest_v2_and_host_holds(self):
  x=p.verify(p.build())["PACKAGE_MANIFEST.json"]
  self.assertIn(b'"version":"2.0.0"',x)
  self.assertIn(b'"savegame_host":"NOT_INTEGRATED"',x)
 def test_unsafe_ledger_getter_absent(self):
  x=p.source(p.ASSET+"/Runtime/DetachedSnapshotContract.cs").decode()
  self.assertNotIn("public DebrisImpulseLedger GetLedger",x)
  self.assertIn("public bool ContainsConsumedId",x)
 def test_defensive_snapshot_backing(self):
  x=p.source(p.ASSET+"/Runtime/DetachedSnapshotContract.cs").decode()
  self.assertIn("public PersistedPart[] Parts => (PersistedPart[])frozenParts.Clone();",x)
  self.assertNotIn("public readonly PersistedPart[] Parts;",x)
 def test_original_pinned_source_provenance(self):self.assertEqual(len(p.PINS),4)
if __name__=="__main__":unittest.main()
