import unittest, pathlib, subprocess
ROOT=pathlib.Path(__file__).resolve().parents[4]
P="Tools/B01/B01-62/P17W20SingleFileSaveSlot/SingleFileSaveSlot.cs"
class Scope(unittest.TestCase):
    def test_frozen_original_669_blob(self):
        self.assertEqual(subprocess.check_output(["git","rev-parse","HEAD:"+P],cwd=ROOT,text=True).strip(),"db7745635164d9eb6b10f3246538700158130b7b")
    def test_selected_original_729_parent(self):
        subprocess.run(["git","merge-base","--is-ancestor","b8ca29c7707cd6b4338d0b9772d74dee676d49d2","HEAD"],cwd=ROOT,check=True)
    def test_unity_assembly_independent_of_tools_669(self):
        s=(ROOT/"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs").read_text()
        self.assertIn("TryAdopt669",s)
        self.assertIn("ICanonicalExternalLease",s)
        self.assertNotIn("using B01.SingleFileSaveSlot001",s)
    def test_no_two_os_streams_in_bridge(self):
        s=(ROOT/"Tools/B01/B01-23/P17W23SingleAuthorityBridge001/CanonicalSingleAuthoritySession.cs").read_text()
        self.assertIn("SingleFileSaveSlot.TryCreateNew(",s)
        self.assertIn("TryAdopt669(",s)
        self.assertNotIn("new FileStream(",s)
if __name__=="__main__":unittest.main()
