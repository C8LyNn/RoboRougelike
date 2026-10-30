#!/usr/bin/env python3
"""B01-23 W23: exact original selected R5 native fixture generation correction.
Checks only owned source, and intentionally DOES NOT impersonate native NUnit."""
import subprocess, pathlib, re, unittest, sys
ROOT=pathlib.Path(__file__).resolve().parents[4]
HOST="Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"
R5="cc7f73541517a18f4479bb155534bd4832d9985e"
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT).decode().strip()
class ColdRestartFixture(unittest.TestCase):
    def test_selected_R5_is_immutable_parent(self):
        subprocess.run(["git","merge-base","--is-ancestor",R5,"HEAD"],check=True,cwd=ROOT)
        self.assertEqual(len(R5),40)
    def test_original_exact_fixture_is_sole_cold_restart(self):
        code=subprocess.check_output(["git","show","HEAD:"+HOST],cwd=ROOT).decode()
        body=code.split("public IEnumerator ColdRestart_RestoresRealRigidbodyAndConsumedIds()",1)[1].split("public IEnumerator WrongGeneration_",1)[0]
        self.assertIn("MakeFixture(41,4,true,false)",body)
        self.assertIn("SnapshotCodec.TryDecode(saved,out RunSnapshot savedSnapshot)",body)
        self.assertIn("Assert.AreEqual(5,savedSnapshot.Generation)",body)
        self.assertIn("restored.TryRestoreBytes(saved,41,5)",body)
        self.assertIn("Assert.AreEqual(5,restored.Generation)",body)
        self.assertNotIn("restored.TryRestoreBytes(saved,41,4)",body)
        self.assertNotIn("Assert.AreEqual(4,restored.Generation)",body)
    def test_no_non_fixture_original_R5_source_changed(self):
        items=git("diff","--name-only",R5,"HEAD").splitlines()
        self.assertEqual(sorted(items),sorted([HOST,
            "Tools/B01/B01-23/P17W23R5ColdRestart/README.md",
            "Tools/B01/B01-23/P17W23R5ColdRestart/test_fixture.py",
            ".github/workflows/b01-23-p17w23-r5-coldrestart-fixture.yml"]))
    def test_real_Git_bytes_and_G0_can_not_be_inherited(self):
        before=git("rev-parse",R5+":"+HOST)
        after=git("rev-parse","HEAD:"+HOST)
        self.assertNotEqual(before,after)
        self.assertIn("NATIVE_G1_NOT_RUN",git("show","HEAD:Tools/B01/B01-23/P17W23R5ColdRestart/README.md"))
if __name__=="__main__":unittest.main(verbosity=2)
