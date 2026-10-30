import pathlib,re,unittest,subprocess
ROOT=pathlib.Path(__file__).resolve().parents[4]
F='Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs'
R5='cc7f73541517a18f4479bb155534bd4832d9985e'
class ColdRestartFixtureTests(unittest.TestCase):
    def test_exact_frozen_r5_parent_and_fixture_only_change(self):
        subprocess.run(['git','merge-base','--is-ancestor',R5,'HEAD'],cwd=ROOT,check=True)
        files=subprocess.check_output(['git','diff','--name-only',R5,'HEAD'],cwd=ROOT,text=True).splitlines()
        self.assertEqual(set(files),{F,'.github/workflows/b01-23-p17w23-r5-coldrestart-fixture.yml',
            'Tools/B01/B01-23/P17W23R5ColdRestartFixture001/README.md',
            'Tools/B01/B01-23/P17W23R5ColdRestartFixture001/test_fixture.py'})
    def test_cold_restart_uses_g5_after_g4_save(self):
        text=(ROOT/F).read_text()
        match=re.search(r'public IEnumerator ColdRestart_RestoresRealRigidbodyAndConsumedIds\(\)(.*?)public IEnumerator WrongGeneration_',text,re.S)
        self.assertIsNotNone(match)
        body=match.group(1)
        self.assertIn('MakeFixture(41,4,true,false)',body)
        self.assertIn('Assert.AreEqual(5,decoded.Generation)',body)
        self.assertIn('TryRestoreBytes(saved,41,5)',body)
        self.assertIn('Assert.AreEqual(5,restored.Generation)',body)
        self.assertIn('2310001',body)
        self.assertIn('2310002',body)
        self.assertNotIn('TryRestoreBytes(saved,41,4)',body)
    def test_unmodified_real_atomic_world_swap_and_host(self):
        for f in ('Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs',
                  'Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs',
                  'Assets/B0123UnitySaveBridge001/Core/OwnerWorldLeaseRetirement.cs'):
            before=subprocess.check_output(['git','show',R5+':'+f],cwd=ROOT)
            self.assertEqual(before,(ROOT/f).read_bytes())
if __name__=='__main__':unittest.main()
