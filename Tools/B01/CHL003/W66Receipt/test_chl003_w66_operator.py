#!/usr/bin/env python3
"""Source-only real Git worktree + synthetic XML/CSV receiver negatives.
ALL expected 20E/15P cases come from original producer #285 C# source.
Do not label the synthetic XML/CSV as an actual Unity result.
"""
from pathlib import Path
import json
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET
import chl003_w66_operator as op


def run(*args):
    p=subprocess.run(list(args),stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
    if p.returncode:raise AssertionError(p.stderr)
    return p.stdout.strip()


def nunit(stage):
    root=ET.Element('test-run', result='Passed')
    assembly=ET.SubElement(root,'test-suite',type='Assembly',name=op.ASSEMBLIES[stage]+'.dll',result='Passed')
    for cls, names in op.CASE_CLASSES[stage].items():
        for name in names:
            fullname=f'B01.CHL003.Tests.{cls}.{name}'
            ET.SubElement(assembly,'test-case',name=name,classname=f'B01.CHL003.Tests.{cls}',
                          fullname=fullname,result='Passed')
    return ET.tostring(root,encoding='unicode')


def trial_log():
    header='profile,attempt,massKg,widthM,lengthM,elapsedSec,state,physicsSamples,collisionEntries,contactPoints,barrierPoints,rampPoints,bridgePoints,groundPoints,falls,checkpoints,completions,peakSpeedMps,travelMeters,minHeightM,maxHeightM'
    lines=[]
    for profile in ('LIGHT','HEAVY'):
        for attempt in (1,2,3):
            mass,width=(270,2.0) if profile=='LIGHT' else (1150,2.82)
            vals=[profile,attempt,mass,width,2.3,7.2,'Completed',400,7,23,0,3,5,15,0,4,1,8.2,42.7,0,2.3]
            lines.extend(['CHL003_NATIVE_REAL_TRIAL '+header,','.join(map(str,vals))])
    return '\n'.join(lines)+'\n'


class OperatorSourceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp=tempfile.TemporaryDirectory(prefix='chl003_w66_git_')
        cls.root=Path(cls.temp.name)
        cls.repo=cls.root/'repo'
        cls.worktree=cls.root/'pristine-source'
        cls.repo.mkdir()
        run('git','init',str(cls.repo))
        run('git','-C',str(cls.repo),'config','user.email','qa@example.invalid')
        run('git','-C',str(cls.repo),'config','user.name','QA Test')
        (cls.repo/'README.txt').write_text('fixture\n',encoding='utf-8')
        run('git','-C',str(cls.repo),'add','README.txt')
        run('git','-C',str(cls.repo),'commit','-m','QA-only test fixture')
        cls.commit=run('git','-C',str(cls.repo),'rev-parse','HEAD')
        run('git','-C',str(cls.repo),'worktree','add','--detach',str(cls.worktree),cls.commit)

    @classmethod
    def tearDownClass(cls):
        cls.temp.cleanup()

    def test_01_real_registered_detached_worktree_accepted(self):
        self.assertEqual(op.registered_detached(self.worktree,self.commit),self.worktree.resolve())

    def test_02_wrong_exact_sha_fails_closed(self):
        with self.assertRaisesRegex(op.ReceiptError,'WRONG_SOURCE_SHA'):
            op.registered_detached(self.worktree,'0'*40)

    def test_03_nonlinked_regular_checkout_fails_closed(self):
        with self.assertRaisesRegex(op.ReceiptError,'NOT_REGISTERED_LINKED'):
            op.registered_detached(self.repo,self.commit)

    def test_04_evidence_root_inside_project_rejected(self):
        with self.assertRaisesRegex(op.ReceiptError,'EVIDENCE_IN_PROJECT'):
            op.safe_output(self.worktree,self.worktree/'evidence')

    def test_05_evidence_equal_project_rejected(self):
        with self.assertRaisesRegex(op.ReceiptError,'EVIDENCE_IN_PROJECT'):
            op.safe_output(self.worktree,self.worktree)

    def test_06_evidence_parent_of_project_rejected(self):
        with self.assertRaisesRegex(op.ReceiptError,'EVIDENCE_ANCESTOR_OF_PROJECT'):
            op.safe_output(self.worktree,self.root)

    def test_07_dirty_tracked_file_rejected(self):
        p=self.worktree/'README.txt'
        p.write_text('unsafe\n',encoding='utf-8')
        try:
            with self.assertRaisesRegex(op.ReceiptError,'DIRTY_CHECKOUT'):
                op.registered_detached(self.worktree,self.commit)
        finally:p.write_text('fixture\n',encoding='utf-8')

    def test_08_untracked_source_rejected(self):
        (self.worktree/'Assets').mkdir(exist_ok=True)
        p=self.worktree/'Assets'/'Injected.cs'
        p.write_text('evil',encoding='utf-8')
        try:
            with self.assertRaisesRegex(op.ReceiptError,'DIRTY_CHECKOUT'):
                op.registered_detached(self.worktree,self.commit)
        finally:p.unlink();(self.worktree/'Assets').rmdir()

    def test_09_good_editmode_20_cases_synthetic_pass(self):
        self.assertEqual(op.validate_xml('EditMode',nunit('EditMode')),20)

    def test_10_good_playmode_15_cases_synthetic_pass(self):
        self.assertEqual(op.validate_xml('PlayMode',nunit('PlayMode')),15)

    def test_11_missing_E20_rejected(self):
        root=ET.fromstring(nunit('EditMode'))
        assembly=root.find('.//test-suite')
        target=next(c for c in assembly if 'E20_' in c.get('name'))
        assembly.remove(target)
        with self.assertRaisesRegex(op.ReceiptError,'NUNIT_CASE_COUNT'):
            op.validate_xml('EditMode',ET.tostring(root,encoding='unicode'))

    def test_12_missing_P15_rejected(self):
        root=ET.fromstring(nunit('PlayMode'))
        assembly=root.find('.//test-suite')
        target=next(c for c in assembly if 'P15_' in c.get('name'))
        assembly.remove(target)
        with self.assertRaisesRegex(op.ReceiptError,'NUNIT_CASE_COUNT'):
            op.validate_xml('PlayMode',ET.tostring(root,encoding='unicode'))

    def test_13_wrong_fullname_rejected(self):
        root=ET.fromstring(nunit('EditMode'))
        root.find('.//test-case').set('fullname','FAKE.E01_OrderedGatesAndCompletionOnce')
        with self.assertRaisesRegex(op.ReceiptError,'NUNIT_CLASSNAME_FULLNAME'):
            op.validate_xml('EditMode',ET.tostring(root,encoding='unicode'))

    def test_14_wrong_assembly_rejected(self):
        root=ET.fromstring(nunit('EditMode'))
        root.find('.//test-suite').set('name','B01.CHL003.OtherTests.dll')
        with self.assertRaisesRegex(op.ReceiptError,'WRONG_NUNIT_ASSEMBLY'):
            op.validate_xml('EditMode',ET.tostring(root,encoding='unicode'))

    def test_15_skipped_case_rejected(self):
        root=ET.fromstring(nunit('PlayMode'))
        root.find('.//test-case').set('result','Skipped')
        with self.assertRaisesRegex(op.ReceiptError,'NUNIT_CASE_UNEXPECTED_OR_NOT_PASSED'):
            op.validate_xml('PlayMode',ET.tostring(root,encoding='unicode'))

    def test_16_six_synthetic_trials_positive(self):
        self.assertEqual(op.validate_trials(trial_log()),6)

    def test_17_duplicate_trial_rejected(self):
        t=trial_log().replace('HEAVY,3,1150','HEAVY,2,1150')
        with self.assertRaisesRegex(op.ReceiptError,'TRIAL_PROFILE_ATTEMPT_MISMATCH'):
            op.validate_trials(t)

    def test_18_missing_bridge_physics_rejected(self):
        t=trial_log().replace('400,7,23,0,3,5,15,0,4,1','400,7,23,0,3,0,15,0,4,1')
        with self.assertRaisesRegex(op.ReceiptError,'TRIAL_INSUFFICIENT_bridgePoints'):
            op.validate_trials(t)

    def test_19_boolean_false_exit_code_fails_closed(self):
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)
            meta={'source_sha':op.EXPECTED_SHA,'unity_version':op.UNITY_VERSION,
                  'stages':{'EditMode':{'exit_code':False}},'hashes':{}}
            (p/'execution.json').write_text(json.dumps(meta),encoding='utf-8')
            with self.assertRaisesRegex(op.ReceiptError,'NATIVE_EXIT_CODE_NOT_INT_ZERO'):
                op.validate_receipt(p)

    def test_23_cases_under_foreign_assembly_rejected(self):
        root=ET.fromstring(nunit('EditMode'))
        expected=root.find('.//test-suite')
        foreign=ET.SubElement(root,'test-suite',type='Assembly',name='B01.UNRELATED.EditModeTests.dll',result='Passed')
        for case in list(expected):
            expected.remove(case)
            foreign.append(case)
        with self.assertRaisesRegex(op.ReceiptError,'NUNIT_CASE_OUTSIDE_EXPECTED_ASSEMBLY'):
            op.validate_xml('EditMode',ET.tostring(root,encoding='unicode'))

    def test_24_any_heavy_narrower_than_light_rejected(self):
        bad=trial_log().replace('HEAVY,1,1150,2.82,','HEAVY,1,1150,0.01,')
        with self.assertRaisesRegex(op.ReceiptError,'RIG_FOOTPRINT_IDENTICAL_OR_INVERTED'):
            op.validate_trials(bad)

    def test_20_missing_native_evidence_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)
            meta={'source_sha':op.EXPECTED_SHA,'unity_version':op.UNITY_VERSION,
                  'stages':{'EditMode':{'exit_code':0}},'hashes':{}}
            (p/'execution.json').write_text(json.dumps(meta),encoding='utf-8')
            with self.assertRaisesRegex(op.ReceiptError,'RECEIPT_FILES_MISSING'):
                op.validate_receipt(p)

    def test_21_wrong_receipt_SHA_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)
            (p/'execution.json').write_text(json.dumps({'source_sha':'0'*40,'unity_version':op.UNITY_VERSION}),encoding='utf-8')
            with self.assertRaisesRegex(op.ReceiptError,'WRONG_RECEIPT_SOURCE_OR_UNITY'):
                op.validate_receipt(p)

    def test_22_empty_log_never_native_pass(self):
        with self.assertRaisesRegex(op.ReceiptError,'SIX_PHYSICAL_TRIALS_REQUIRED'):
            op.validate_trials('')


def run_tests():
    suite=unittest.defaultTestLoader.loadTestsFromTestCase(OperatorSourceTests)
    result=unittest.TextTestRunner(verbosity=2).run(suite)
    if not result.wasSuccessful():
        raise op.ReceiptError('SELFTEST_FAILURE')
    print(f'CHL_W66_QA_OPERATOR_SOURCE_SELFTEST_PASS={result.testsRun} NATIVE_UNITY=NOT_RUN')


if __name__ == '__main__':run_tests()