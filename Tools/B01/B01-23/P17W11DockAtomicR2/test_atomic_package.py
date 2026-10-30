import importlib.util,json
from pathlib import Path
import unittest
old=Path(__file__).resolve().parents[1]/"P17W10SecureReattachV2"/"package.py"
spec=importlib.util.spec_from_file_location("dockr2bundle",old)
p=importlib.util.module_from_spec(spec);spec.loader.exec_module(p)
class OriginalR2AssetTests(unittest.TestCase):
    def test_exact_total_scope(self):self.assertEqual(len(p.scope()),34)
    def test_parent_original_W06_pinned(self):self.assertEqual(p.PARENT,"cb2b3a76e84266e2730fffc4ab048c1039f0652f")
    def test_unchanged_pinned_W06(self):self.assertEqual(len(p.PINS),3)
    def test_real_R2_guard_source_contract(self):p.guard_contract()
    def test_R2_ledger_real_asset(self):self.assertIn(p.ASSET+"/Runtime/AtomicDockLedger.cs",p.scope())
    def test_transaction_order_precedes_mutation(self):
        x=p.blob(p.ASSET+"/Runtime/AtomicDockLedger.cs").decode()
        self.assertLess(x.index("consumed.RearmAfterReattach(id)"),x.index("applyPhysical();"))
    def test_real_physical_exception_compensation(self):
        x=p.blob(p.ASSET+"/Runtime/AtomicDockLedger.cs").decode()
        self.assertIn("rollbackPhysical();",x);self.assertIn("consumed.TryDetach(",x)
    def test_old_direct_emitter_reattach_no_longer_exists(self):
        x=p.blob(p.ASSET+"/Runtime/SecureDebrisPlaygroundV2.cs").decode()
        self.assertNotIn("rig.Emitter.RearmAfterReattach",x)
        self.assertIn("rig.Ledger.TryReattach(rig.Spec.PartId,",x)
    def test_private_commit_only(self):
        x=p.blob(p.ASSET+"/Runtime/SecureDebrisPlaygroundV2.cs").decode()
        self.assertIn("private bool CommitDock(int index)",x)
        self.assertNotIn("public bool CommitDock(",x)
    def test_missing_id_unity_playmode_authored(self):
        x=p.blob(p.ASSET+"/Tests/PlayMode/SecureReattachV2PlayModeTests.cs").decode()
        self.assertIn("LostConsumedIdRejectsEveryPublicAliasWithoutPhysicalMutation",x)
    def test_r2_zip_is_deterministic(self):self.assertEqual(p.build(),p.build())
    def test_manifest_new_version_and_parent(self):
        m=json.loads(p.verify(p.build())[p.MANIFEST])
        self.assertEqual(m["version"],"2.0.0")
        self.assertEqual(m["owned_meta_guids"],14)
        self.assertEqual(m["supersedes_source_sha"],"33a32b8bd9ab40467ab1a17b5684926a37dbd4d3")
if __name__=="__main__":unittest.main(verbosity=2)
