#!/usr/bin/env python3
"""B01-40 exact-original B01-20 W66 CHL operator isolation acceptance.
Read-only execution of owner's real Python safe_output and registered_detached.
No Unity launch, no CHL producer mutation, no G1/G2.
"""
import hashlib
import importlib.util
from pathlib import Path
import subprocess
import tempfile
import unittest

OWNER_COMMIT="b9f0458db297fb81e76add00855609b32eb0ef4b"
OWNER_PATH="Tools/B01/CHL003/W66Receipt/chl003_w66_operator.py"
OWNER_GIT_BLOB="42ab146450ea3284d97a9a2bd5434a49fe195ab7"

def git(*args):
    return subprocess.check_output(["git",*args],stderr=subprocess.PIPE).decode().strip()

def load_owner():
    assert git("rev-parse",OWNER_COMMIT+":"+OWNER_PATH)==OWNER_GIT_BLOB
    src=subprocess.check_output(["git","show",OWNER_COMMIT+":"+OWNER_PATH])
    digest=hashlib.sha1(b"blob "+str(len(src)).encode()+b"\0"+src).hexdigest()
    assert digest==OWNER_GIT_BLOB
    with tempfile.TemporaryDirectory(prefix="b01_40_chl_actual_") as root:
        path=Path(root)/"chl003_w66_operator.py"; path.write_bytes(src)
        spec=importlib.util.spec_from_file_location("b01_20_actual_chl_operator",path)
        mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)
    return mod

OWNER=load_owner()

class ActualCHLOperatorIsolation(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.tmp=tempfile.TemporaryDirectory(prefix="b01_40_chl_siblings_")
        cls.root=Path(cls.tmp.name)
        cls.repo=cls.root/"primary"
        cls.repo.mkdir()
        git("init",str(cls.repo))
        git("-C",str(cls.repo),"config","user.name","B01 QA")
        git("-C",str(cls.repo),"config","user.email","qa@example.invalid")
        (cls.repo/"README").write_text("physical Git fixture; not game\n")
        git("-C",str(cls.repo),"add","README")
        git("-C",str(cls.repo),"commit","-m","isolated Git worktree fixture")
        cls.sha=git("-C",str(cls.repo),"rev-parse","HEAD")
        cls.one=cls.root/"producer-A"
        cls.two=cls.root/"producer-B"
        git("-C",str(cls.repo),"worktree","add","--detach",str(cls.one),cls.sha)
        git("-C",str(cls.repo),"worktree","add","--detach",str(cls.two),cls.sha)

    @classmethod
    def tearDownClass(cls): cls.tmp.cleanup()

    def test_01_original_sha_worktree_and_detached_registry_are_real(self):
        self.assertEqual(OWNER.registered_detached(self.one,self.sha),self.one.resolve())
        self.assertEqual(OWNER.registered_detached(self.two,self.sha),self.two.resolve())

    def test_02_unrelated_external_evidence_dir_is_allowed(self):
        target=self.root/"outside-evidence"
        a,b=OWNER.safe_output(self.one,target)
        self.assertEqual(a,self.one.resolve())
        self.assertEqual(b,target.resolve())

    def test_03_current_worktree_receipt_is_denied(self):
        with self.assertRaisesRegex(OWNER.ReceiptError,"EVIDENCE_IN_PROJECT"):
            OWNER.safe_output(self.one,self.one/"evidence")

    def test_04_sibling_source_worktree_root_currently_not_rejected(self):
        # This is a genuine current-owner-script RED witness, not an expected
        # security PASS: preflight permits evidence to be written into another
        # registered source worktree, a gap pending B01-20 remediation.
        _,target=OWNER.safe_output(self.one,self.two)
        self.assertEqual(target,self.two.resolve())
        print("SOURCE_DEFECT_REPRODUCED=CHL_EVIDENCE_SIBLING_WORKTREE_ROOT_ACCEPTED")

    def test_05_sibling_source_unity_assets_subfolder_currently_not_rejected(self):
        target=self.two/"Assets"/"evidence"
        _,actual=OWNER.safe_output(self.one,target)
        self.assertEqual(actual,target.resolve())
        print("SOURCE_DEFECT_REPRODUCED=CHL_EVIDENCE_SIBLING_ASSETS_ACCEPTED")

    def test_06_parent_of_current_project_denied(self):
        with self.assertRaisesRegex(OWNER.ReceiptError,"EVIDENCE_ANCESTOR_OF_PROJECT"):
            OWNER.safe_output(self.one,self.root)

    def test_07_wrong_source_sha_refused(self):
        with self.assertRaisesRegex(OWNER.ReceiptError,"WRONG_SOURCE_SHA"):
            OWNER.registered_detached(self.one,"0"*40)

if __name__=="__main__":
    print("CHL_ORIGINAL_OPERATOR_GIT_BLOB="+OWNER_GIT_BLOB+
          " NATIVE_UNITY=NOT_RUN SIBLING_TEST=KNOWN_RED_WITNESS",flush=True)
    unittest.main(verbosity=2)
