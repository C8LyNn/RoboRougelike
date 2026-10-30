import json
import pathlib
import sys
import tempfile
import unittest
from unittest.mock import patch
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parents[1]))
import scenecost

HEADER='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'
def doc(cls,ident,type_name,body=''):
    return f'--- !u!{cls} &{ident}\n{type_name}:\n'+body

def fixture(*,bad_component=False,bad_guid=False,nonconvex=True):
    go='  m_Name: Robot Test\n  m_IsActive: 1\n  m_Component:\n  - component: {fileID: 12}\n  - component: {fileID: 13}\n  - component: {fileID: 14}\n  - component: {fileID: '+('999' if bad_component else '15')+'}\n'
    body=HEADER+doc(1,11,'GameObject',go)
    body+=doc(4,12,'Transform','  m_GameObject: {fileID: 11}\n')
    body+=doc(54,13,'Rigidbody','  m_GameObject: {fileID: 11}\n')
    body+=doc(64,14,'MeshCollider','  m_GameObject: {fileID: 11}\n  m_Convex: '+('0' if nonconvex else '1')+'\n')
    body+=doc(114,15,'MonoBehaviour','  m_GameObject: {fileID: 11}\n'+('  m_Script: {fileID: 11500000, guid: 0123456789abcdef0123456789abcdef, type: 3}\n' if not bad_guid else '  m_Script: {fileID: 0}\n'))
    return body

class SceneCostTests(unittest.TestCase):
    def setUp(self):
        self.t=tempfile.TemporaryDirectory();self.addCleanup(self.t.cleanup)
        self.root=pathlib.Path(self.t.name)
        self.src=self.root/'scene.unity';self.src.write_text(fixture())
    def check(self,**k):
        return scenecost.scan(self.src,**k)
    def test_01_real_serialized_sections(self):
        r=self.check();self.assertEqual(r['document_count'],5);self.assertEqual(r['counts']['MonoBehaviour'],1)
    def test_02_per_object_script_guid_and_name(self):
        r=self.check();self.assertEqual(r['unique_script_guids'],1);self.assertEqual(r['game_object_names'][11],'Robot Test')
    def test_03_nonconvex_meshcollider_rigidbody_same_gameobject(self):
        self.assertIn('nonconvex_meshcollider_with_rigidbody_same_go',[x['type'] for x in self.check()['issues']])
    def test_04_convex_meshcollider_not_flagged(self):
        self.src.write_text(fixture(nonconvex=False));self.assertEqual(self.check()['issue_count'],0)
    def test_05_missing_component_reference_detected(self):
        self.src.write_text(fixture(bad_component=True));self.assertIn('missing_local_component',[i['type'] for i in self.check()['issues']])
    def test_06_missing_script_guid_detected(self):
        self.src.write_text(fixture(bad_guid=True));self.assertIn('missing_script_guid',[i['type'] for i in self.check()['issues']])
    def test_07_advisory_limit_counts(self):
        r=self.check(limits={'MonoBehaviour':0});self.assertIn('advisory_budget_exceeded',[x['type'] for x in r['issues']])
    def test_08_invalid_budgets_fail_closed(self):
        for bad in [{'NonexistentType':1},{'MeshRenderer':-1},{'Light':False},{'GameObject':1000001}]:
            with self.subTest(bad=bad),self.assertRaises(scenecost.AuditError):self.check(limits=bad)
    def test_09_duplicate_yaml_fileids_rejected(self):
        self.src.write_text(fixture()+doc(4,12,'Transform'))
        with self.assertRaisesRegex(scenecost.AuditError,'duplicate'):self.check()
    def test_10_unsupported_yaml_rejected(self):
        self.src.write_text('a: 1\n');
        with self.assertRaisesRegex(scenecost.AuditError,'Unity YAML'):self.check()
    def test_11_symlink_input_rejected(self):
        alias=self.root/'alias.unity';alias.symlink_to(self.src)
        with self.assertRaisesRegex(scenecost.AuditError,'symlink'):scenecost.scan(alias)
    def test_12_no_overwrite_or_source_modification(self):
        out=self.root/'out.json';out.write_text('unchanged');orig=self.src.read_bytes()
        self.assertEqual(scenecost.main([str(self.src),'--out',str(out)]),2)
        self.assertEqual(out.read_text(),'unchanged');self.assertEqual(self.src.read_bytes(),orig)
    def test_13_valid_json_csv_reports_and_provenance(self):
        j,c=self.root/'report.json',self.root/'report.csv'
        self.assertEqual(scenecost.main([str(self.src),'--out',str(j),'--csv',str(c)]),0)
        self.assertEqual(json.loads(j.read_text())['files'][0]['source_sha256'],scenecost.sha256(self.src))
        self.assertIn('MeshCollider',c.read_text());self.assertIn('MonoBehaviour',c.read_text())
    def test_14_strict_budget_has_nonzero_but_preserves_report(self):
        out=self.root/'out.json'
        self.assertEqual(scenecost.main([str(self.src),'--budgets',str(self._limits()),'--strict','--out',str(out)]),4)
        self.assertTrue(out.is_file())
    def _limits(self):
        p=self.root/'limits.json';p.write_text(json.dumps({'MonoBehaviour':0}));return p
    def test_15_byte_provenance_stable(self):
        r1=self.check();r2=self.check();self.assertEqual(r1,r2)
    def test_16_empty_or_invalid_file_bad(self):
        self.src.write_text(HEADER)
        with self.assertRaisesRegex(scenecost.AuditError,'no Unity'):self.check()
    def test_17_bom_supported(self):
        self.src.write_bytes(b'\xef\xbb\xbf'+fixture().encode());self.assertEqual(self.check()['document_count'],5)
    def test_18_no_file_write_on_multi_input_error(self):
        out=self.root/'out.json';self.assertEqual(scenecost.main([str(self.src),str(self.root/'missing.unity'),'--out',str(out)]),2);self.assertFalse(out.exists())
    def test_19_data_not_fps(self):
        self.assertIn('NOT_FPS',self.check()['classification']);self.assertIn('no mesh triangles',self.check()['limitations'])
    def test_20_missing_output_parent_rejected(self):
        self.assertEqual(scenecost.main([str(self.src),'--out',str(self.root/'nothing'/'out.json')]),2)

if __name__=='__main__':unittest.main()
