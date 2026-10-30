#!/usr/bin/env python3
"""Executable negative/positive unit tests. ALL fixture evidence SYNTHETIC."""
import hashlib
import json
from pathlib import Path
import tempfile
import xml.etree.ElementTree as ET
import unittest
import verify_evidence as v

class GateTests(unittest.TestCase):
    def setUp(self):
        self.t=tempfile.TemporaryDirectory(prefix='b0160-w21-synthetic-')
        self.e=Path(self.t.name)/'evidence'
        self.e.mkdir()
        self.content={
            'operator-report.json':json.dumps({'source_sha':v.W20_SHA,'merged':False,
               'uploaded':False,'canonical_SaveLoad':'HOLD',
               'selected_R4':'SELECTED_647_SECURITY_P0_G0_HOLD',
               'source_preflight':'PASS_16_AUTHORED_CASES_NOT_EXECUTED',
               'operator_consent':False,'mode':'PREFLIGHT','result':'PREFLIGHT_PASS_NATIVE_NOT_RUN',
               'native_g1':'NOT_RUN'}).encode(),
            'v2-source-preflight/receipt.json':b'{"test_fixture":true}\n',
        }
        self.write()
    def tearDown(self):self.t.cleanup()
    def write(self):
        for path,data in self.content.items():
            q=self.e/path;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(data)
        manifest={'source_sha':v.W20_SHA,'unsigned':True,
           'files':[{'path':p,'bytes':len(b),'sha256':hashlib.sha256(b).hexdigest()}
             for p,b in sorted(self.content.items())]}
        blob=json.dumps(manifest,indent=2).encode()
        (self.e/'sha256-manifest.json').write_bytes(blob)
        (self.e/'sha256-manifest.json.sha256').write_text(hashlib.sha256(blob).hexdigest()+'\n')
    def mutation(self,path,f):
        self.content[path]=f(self.content[path]);self.write()
        with self.assertRaises(v.Reject):v.audit(self.e)
    def test_01_source_only_synthetic(self):
        self.assertEqual(v.audit(self.e),'SOURCE_ONLY_PASS_NATIVE_NOT_RUN')
    def test_02_wrong_sha(self):
        self.mutation('operator-report.json',lambda x:x.replace(v.W20_SHA.encode(),b'0'*40))
    def test_03_false_native(self):
        self.mutation('operator-report.json',lambda x:x.replace(b'"NOT_RUN"',b'"PASS_V2_ONLY_R4_STILL_NOT_RUN"'))
    def test_04_false_consent(self):
        self.mutation('operator-report.json',lambda x:x.replace(b'false',b'true'))
    def test_05_claim_save_pass(self):
        self.mutation('operator-report.json',lambda x:x.replace(b'"HOLD"',b'"PASS"'))
    def test_06_modification_after_inventory(self):
        (self.e/'operator-report.json').write_bytes(b'different')
        with self.assertRaises(v.Reject):v.audit(self.e)
    def test_07_missing_receipt(self):
        (self.e/'v2-source-preflight/receipt.json').unlink()
        with self.assertRaises(v.Reject):v.audit(self.e)
    def test_08_uninventoried_file(self):
        (self.e/'rogue.txt').write_text('foreign evidence')
        with self.assertRaises(v.Reject):v.audit(self.e)
    def test_09_manifest_hash(self):
        (self.e/'sha256-manifest.json.sha256').write_text('f'*64)
        with self.assertRaises(v.Reject):v.audit(self.e)
    def test_10_path_traversal(self):
        p=self.e/'sha256-manifest.json';d=json.loads(p.read_text());d['files'][0]['path']='../escape'
        x=json.dumps(d).encode();p.write_bytes(x)
        (self.e/'sha256-manifest.json.sha256').write_text(hashlib.sha256(x).hexdigest())
        with self.assertRaises(v.Reject):v.audit(self.e)
    def test_11_directory_symlink(self):
        x=self.e/'rogue';x.symlink_to(self.e, target_is_directory=True)
        with self.assertRaises(v.Reject):v.audit(self.e)
    def test_12_duplicate_entry(self):
        p=self.e/'sha256-manifest.json';d=json.loads(p.read_text());d['files'].append(d['files'][0])
        x=json.dumps(d).encode();p.write_bytes(x)
        (self.e/'sha256-manifest.json.sha256').write_text(hashlib.sha256(x).hexdigest())
        with self.assertRaises(v.Reject):v.audit(self.e)
    @staticmethod
    def xml(names):
        root=ET.Element('test-run',{'result':'Passed','passed':str(len(names)),'failed':'0'})
        for name in sorted(names):ET.SubElement(root,'test-case',{'fullname':name,'result':'Passed'})
        return ET.tostring(root)
    def synthetic_native(self):
        r=json.loads(self.content['operator-report.json']);r.update({
            'native_g1':'PASS_V2_ONLY_R4_STILL_NOT_RUN','mode':'OPERATOR_NATIVE_V2_ONLY',
            'result':'REAL_V2_ONLY_PASS_NOT_CANONICAL_SAVE','operator_consent':True,
            'native_process_exit':0})
        self.content['operator-report.json']=json.dumps(r).encode()
        vpath='original-V2-native-only/'
        for mode,names in [('EditMode',v.EDIT),('PlayMode',v.PLAY)]:
            self.content[vpath+mode+'.xml']=self.xml(names)
            self.content[vpath+mode+'-Editor.log']=b'Unity version 6000.3.14f1\n'
        self.content[vpath+'native-verdict.json']=json.dumps({
            'G1':'PASS','native_unity':'EXECUTED','owner_sha':v.GAME_SHA,
            'results':{'EditMode':{},'PlayMode':{}}}).encode()
        self.write()
    def test_13_synthetic_native_structural_not_attested(self):
        self.synthetic_native()
        self.assertEqual(v.audit(self.e),'STRUCTURALLY_CONSISTENT_V2_ONLY_NOT_NATIVE_ATTESTATION')
    def test_14_missing_case(self):
        self.synthetic_native()
        self.mutation('original-V2-native-only/EditMode.xml',lambda x:x.replace(
          b'ExactOwnerRuntimeAndOriginalW06AreLinkableByUnity',b'UnknownTest'))
    def test_15_double_case(self):
        self.synthetic_native()
        p='original-V2-native-only/PlayMode.xml'
        self.mutation(p,lambda x:x.replace(b'</test-run>',
            b'<test-case fullname="'+sorted(v.PLAY)[0].encode()+b'" result="Passed" /></test-run>'))
    def test_16_wrong_unity(self):
        self.synthetic_native()
        self.mutation('original-V2-native-only/PlayMode-Editor.log',lambda x:x.replace(b'6000.3.14f1',b'6000.2.0f1'))
    def test_17_bad_nunit_result(self):
        self.synthetic_native()
        self.mutation('original-V2-native-only/EditMode.xml',lambda x:x.replace(b'result="Passed"',b'result="Failed"',1))
    def test_18_wrong_original_owner(self):
        self.synthetic_native()
        self.mutation('original-V2-native-only/native-verdict.json',lambda x:x.replace(v.GAME_SHA.encode(),b'0'*40))
    def test_19_no_native_exit(self):
        self.synthetic_native()
        self.mutation('operator-report.json',lambda x:x.replace(b'"native_process_exit": 0',b'"native_process_exit": 3'))
    def test_20_no_native_consent(self):
        self.synthetic_native()
        self.mutation('operator-report.json',lambda x:x.replace(b'"operator_consent": true',b'"operator_consent": false'))

if __name__=='__main__':unittest.main(verbosity=2)
