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

OWNER_COMMIT="9a6acca417524545fbce7ac5962fe0944c5237c6"
OWNER_PATH="Tools/B01/QA-CHL003/W66Receipt/chl003_w66_operator.py"
OWNER_GIT_BLOB="96aec2f870ccc510727048a7208292f781e80c36"

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

    def test_04_sibling_source_worktree_root_is_rejected_after_owner_fix(self):
        with self.assertRaisesRegex(OWNER.ReceiptError,"EVIDENCE_IN_REGISTERED_WORKTREE"):
            OWNER.safe_output(self.one,self.two)

    def test_05_sibling_source_assets_is_rejected_after_owner_fix(self):
        with self.assertRaisesRegex(OWNER.ReceiptError,"EVIDENCE_IN_REGISTERED_WORKTREE"):
            OWNER.safe_output(self.one,self.two/"Assets"/"receipts")

    def test_05b_sibling_source_parent_alias_is_rejected(self):
        with self.assertRaisesRegex(OWNER.ReceiptError,"EVIDENCE_ANCESTOR_OF_(?:PROJECT|REGISTERED_WORKTREE)"):
            OWNER.safe_output(self.one,self.root)

    def test_05c_similarly_named_sibling_prefix_is_allowed(self):
        target=self.root/"producer-B-different"/"evidence"
        _,actual=OWNER.safe_output(self.one,target)
        self.assertEqual(actual,target.resolve())

    def test_06_parent_of_current_project_denied(self):
        with self.assertRaisesRegex(OWNER.ReceiptError,"EVIDENCE_ANCESTOR_OF_PROJECT"):
            OWNER.safe_output(self.one,self.root)

    def test_07_exact_20_15_authored_nunit_ids(self):
        self.assertEqual(len(OWNER.ids("EditMode")),20)
        self.assertEqual(len(OWNER.ids("PlayMode")),15)
        self.assertIn("B01.CHL003.Tests.CHL003FinishAndTelemetryEditModeTests.E20_B0160NearGroundFalseSupportRejectsHoveringWheel",OWNER.ids("EditMode"))
        self.assertIn("B01.CHL003.Tests.CHL003DemoAcceptancePlayModeTests.P15_B0160NearGroundAirGapNeverAppliesThrustOrYaw",OWNER.ids("PlayMode"))

    @staticmethod
    def synthetic_trial_rows(broken=False):
        header="profile,attempt,massKg,widthM,lengthM,elapsedSec,state,physicsSamples,collisionEntries,contactPoints,barrierPoints,rampPoints,bridgePoints,groundPoints,falls,checkpoints,completions,peakSpeedMps,travelMeters,minHeightM,maxHeightM"
        lines=[]
        for profile,mass,width in (("LIGHT",270,2.0),("HEAVY",1150,2.82)):
            for attempt in range(1,4):
                w=0.01 if broken and profile=="HEAVY" and attempt==1 else width
                values=(profile,attempt,mass,w,2.3,7.2,"Completed",400,7,23,0,3,5,15,0,4,1,8.2,42.7,0,2.3)
                lines.extend(("CHL003_NATIVE_REAL_TRIAL "+header,",".join(map(str,values))))
        return "\n".join(lines)+"\n"

    def test_08_synthetic_six_trial_structure_control_only(self):
        self.assertEqual(OWNER.validate_trials(self.synthetic_trial_rows()),6)

    def test_09_heavy_attempt_one_incorrect_hull_is_rejected(self):
        with self.assertRaisesRegex(OWNER.ReceiptError,"RIG_FOOTPRINT_IDENTICAL_OR_INVERTED"):
            OWNER.validate_trials(self.synthetic_trial_rows(broken=True))

    def test_07b_foreign_nunit_assembly_cases_rejected(self):
        import xml.etree.ElementTree as ET
        root=ET.Element("test-run",result="Passed")
        expected=ET.SubElement(root,"test-suite",type="Assembly",name=OWNER.ASSEMBLIES["EditMode"]+".dll")
        foreign=ET.SubElement(root,"test-suite",type="Assembly",name="OTHER.FOREIGN.dll")
        for full in OWNER.ids("EditMode"):
            cls,name=full.rsplit(".",1)
            ET.SubElement(foreign,"test-case",classname=cls,name=name,fullname=full,result="Passed")
        with self.assertRaisesRegex(OWNER.ReceiptError,"NUNIT_CASE_OUTSIDE_EXPECTED_ASSEMBLY"):
            OWNER.validate_xml("EditMode",ET.tostring(root,encoding="unicode"))

    def test_07_wrong_source_sha_refused(self):
        with self.assertRaisesRegex(OWNER.ReceiptError,"WRONG_SOURCE_SHA"):
            OWNER.registered_detached(self.one,"0"*40)

if __name__=="__main__":
    print("CHL_ORIGINAL_OPERATOR_GIT_BLOB="+OWNER_GIT_BLOB+
          " NATIVE_UNITY=NOT_RUN SIBLING_GUARD=EXPECTED_GREEN_NEW_OWNER_SHA",flush=True)
    unittest.main(verbosity=2)
