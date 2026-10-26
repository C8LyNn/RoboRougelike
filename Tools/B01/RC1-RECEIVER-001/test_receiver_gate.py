import hashlib
import importlib.util
import subprocess
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
import zipfile
import zlib
sys.path.insert(0,str(Path(__file__).resolve().parent))
from receiver_gate import (GateError, camera_inner, audit_rc1, audit_native,
                           verify_release_zip,sha256,git_blob,RC1_GAME,RC1_BASE,
                           MAPGEN,WORLD,CAMERA,BASE_ZIP,PRESERVE)

REAL_ARCHIVE = Path(__file__).with_name('camera-real-artifact.zip')


def wrap(entries):
    f=io.BytesIO()
    with zipfile.ZipFile(f,'w',compression=zipfile.ZIP_STORED) as z:
        for k,v in entries.items():z.writestr(k,v)
    data=f.getvalue();out=io.BytesIO()
    with zipfile.ZipFile(out,'w') as z:
        z.writestr('CAMERA001_R1_2_FULL_SOURCE_QA.zip',data)
        z.writestr('SHA256_REPORT.json',json.dumps({'sha256':sha256(data),'bytes':len(data),'entries':len(entries)}))
    return out.getvalue(),sha256(data),len(data)

def entries():
    with zipfile.ZipFile(REAL_ARCHIVE) as a:
        with zipfile.ZipFile(io.BytesIO(a.read('CAMERA001_R1_2_FULL_SOURCE_QA.zip'))) as z:
            return {n:z.read(n) for n in z.namelist()}

def rehash(entries, filename):
    p='Docs/B01/CAMERA001/manifest.json'
    m=json.loads(entries[p]);data=entries[filename]
    m['r12_source_sha256_by_path'][filename]=sha256(data)
    m['r12_source_bytes_by_path'][filename]=len(data)
    m['files'][filename]['git_blob_sha1']=git_blob(data)
    m['files'][filename]['bytes']=len(data)
    entries[p]=json.dumps(m).encode()


def fixture():
    files={'MAPGEN001':[],'MAPGENWORLD001':[]}
    trees={k:{'tree':[]} for k in ['game','base','mapgen','world']}
    blob='a'*40
    def add(which,path,digest=blob):trees[which]['tree'].append({'type':'blob','path':path,'sha':digest})
    for path in PRESERVE:
        p=path+'/test.txt' if '.' not in path.split('/')[-1] else path
        add('game',p);add('base',p)
    root='Assets/B01Packages.meta'
    for k in ['game','mapgen','world']:add(k,root)
    for tag,which,n in [('MAPGEN001','mapgen',35),('MAPGENWORLD001','world',23)]:
        for i in range(n):
            p=f'Assets/B01Packages/{tag}/test{i}.cs'
            files[tag].append({'path':p,'sha256':'b'*64,'bytes':5})
            add('game',p);add(which,p)
    receipt={'gameSource':RC1_GAME,'base':RC1_BASE,'phy':'9'*40,
        'guidCollisions':0,'protectedPathsUnchanged':PRESERVE,
        'packages':{k:{'source':MAPGEN if k=='MAPGEN001' else WORLD,'byteIdenticalFiles':len(v),'files':v} for k,v in files.items()}}
    return receipt,trees

class ReceiverTests(unittest.TestCase):
    def test_01_real_producer_zip_positive(self):
        x=camera_inner(REAL_ARCHIVE)
        self.assertEqual((x['files_sha256'],x['unity_guids']), (40,16))
    def test_02_outer_modified_sha_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'mutated.zip';s=bytearray(REAL_ARCHIVE.read_bytes());s[100]^=1;p.write_bytes(s)
            with self.assertRaises((GateError,zipfile.BadZipFile,zlib.error)):camera_inner(p)
    def test_03_manifest_missing_meta_rejected(self):
        e=entries();del e['Assets/B01Packages/CAMERA001/Runtime/Camera001Rig.cs.meta']
        outer,h,n=wrap(e)
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'tamper.zip';p.write_bytes(outer)
            with self.assertRaisesRegex(GateError,'entry count|manifest'):camera_inner(p,h,n)
    def test_04_duplicate_guid_with_rehashed_manifest_rejected(self):
        e=entries()
        ref=e['Assets/B01Packages/CAMERA001/Runtime/Camera001Framing.cs.meta']
        victim='Assets/B01Packages/CAMERA001/Runtime/Camera001Rig.cs.meta'
        e[victim]=ref;rehash(e,victim);out,h,n=wrap(e)
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'tamper.zip';p.write_bytes(out)
            with self.assertRaisesRegex(GateError,'duplicate GUID'):camera_inner(p,h,n)
    def test_05_path_traversal_rejected(self):
        e=entries();e['../hijack.cs']=b'exit'
        out,h,n=wrap(e)
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'tamper.zip';p.write_bytes(out)
            with self.assertRaisesRegex(GateError,'unsafe path'):camera_inner(p,h,n)
    def test_06_valid_rc1_git_tree_provenance(self):
        receipt,trees=fixture()
        x=audit_rc1(receipt,trees)
        self.assertEqual(x['tracked_files'],58)
        self.assertEqual(x['protected_areas'],6)
    def test_07_mutated_git_blob_rejected(self):
        receipt,trees=fixture()
        trees['game']['tree'][-1]['sha']='c'*40
        with self.assertRaisesRegex(GateError,'actual Git blob mismatch'):audit_rc1(receipt,trees)
    def test_08_protected_path_mismatch_rejected(self):
        receipt,trees=fixture();trees['base']['tree'][0]['sha']='c'*40
        with self.assertRaisesRegex(GateError,'protected game path'):audit_rc1(receipt,trees)
    def test_09_source_pin_rejected(self):
        receipt,trees=fixture();receipt['packages']['MAPGEN001']['source']='d'*40
        with self.assertRaisesRegex(GateError,'producer source SHA'):audit_rc1(receipt,trees)
    def test_10_binary_absence_pending(self):
        self.assertEqual(verify_release_zip(None,BASE_ZIP)['status'],'PENDING_BINARY_ACCESS')
    def test_11_binary_false_digest_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'fake.zip';p.write_bytes(b'tampered')
            with self.assertRaisesRegex(GateError,'Windows ZIP SHA'):verify_release_zip(p,BASE_ZIP)
    def test_12_windows_directory_entries_allowed_and_crc_checked(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'test.zip';f=io.BytesIO()
            with zipfile.ZipFile(f,'w') as z:
                z.writestr('RoboRougelike_Data/','')
                z.writestr('RoboRougelike_Data/globalgamemanagers',b'actual bytes')
            data=f.getvalue();p.write_bytes(data)
            x=verify_release_zip(p,(sha256(data),len(data)))
            self.assertEqual(x['status'],'PASS_ACTUAL_ZIP_SHA_CRC')
    def test_13_native_external_pass_does_not_override_known_failure(self):
        native={'gameSource':RC1_GAME,'baseSource':RC1_BASE,'G2':'HOLD','independentG1':'PENDING','windows':{k:{'status':'Passed','nativeExit':0,'failedChecks':[]} for k in ['FinalMapgen720','FinalMapgen1080','FinalReactor']}}
        native['windows']['MapgenHidden720']={'nativeExit':-1073741819}
        cam={'producer':CAMERA,'G2':'HOLD','compile':{'exitCode':0},'native':{mode:{'counts':{'total':n,'passed':n,'failed':0,'skipped':0},'process':{'exitCode':0}} for mode,n in [('EditMode',5),('PlayMode',13)]},'windows':{'WindowsFinal':{'process':{'exitCode':2}}}}
        x=audit_native(native,cam)
        self.assertEqual(x['camera_functional'],'FAIL_OBSERVED_2_OF_8_LIGHT_AIM_TRACKING')
        cam['windows']['WindowsFinal']['process']['exitCode']=0
        with self.assertRaisesRegex(GateError,'incorrectly claimed success'):audit_native(native,cam)
        cam['windows']['WindowsFinal']['process']['exitCode']=2
        native['windows']['MapgenHidden720']['nativeExit']=0
        with self.assertRaisesRegex(GateError,'known shutdown failure'):audit_native(native,cam)
    def test_14_out_of_scope_receipt_rejected(self):
        receipt,trees=fixture();receipt['packages']['MAPGEN001']['files'][0]['path']='Assets/R3/Bad.cs'
        with self.assertRaisesRegex(GateError,'out-of-scope import'):audit_rc1(receipt,trees)

    def test_15_real_package_auto_disposable_stage_rollback(self):
        # Reuse original B01-40 PACKAGE-AUTO-001 code, never reimplement writes.
        original = Path(__file__).resolve().parent.parent / 'PACKAGE-AUTO-001' / 'package_auto.py'
        if not original.exists():
            original = Path(__file__).resolve().parents[3] / '_existing_b01_238_package_auto.py'
        self.assertTrue(original.is_file(), 'pin original #238 package_auto.py')
        spec = importlib.util.spec_from_file_location('b01_package_auto',original)
        pkg = importlib.util.module_from_spec(spec);spec.loader.exec_module(pkg)
        cfg = json.loads(Path(__file__).with_name('CAMERA001_R12_PIN.json').read_text())
        with zipfile.ZipFile(REAL_ARCHIVE) as z:
            inner = z.read('CAMERA001_R1_2_FULL_SOURCE_QA.zip')
        with tempfile.TemporaryDirectory() as d:
            root=Path(d)/'repo';root.mkdir()
            (root/'Assets').mkdir()
            (root/'Assets/B01Packages.meta').write_text(
                'fileFormatVersion: 2\nguid: b01a0001000000000000000000000001\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n')
            (root/'ProjectSettings').mkdir()
            (root/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.3.14f1\n')
            (root/'Packages').mkdir()
            (root/'Packages/manifest.json').write_text('{"dependencies":{"com.unity.render-pipelines.universal":"17.3.0"}}')
            def g(*args):
                return subprocess.check_output(['git','-C',str(root),*args],stderr=subprocess.PIPE,text=True).strip()
            g('init','-q');g('config','user.name','B01-40 test');g('config','user.email','qa@example.invalid')
            g('add','.');g('commit','-qm','synthetic disposable receiver baseline')
            head=g('rev-parse','HEAD');target=Path(d)/'detached'
            pkg.create_sandbox(root,target,head)
            zip_file=Path(d)/'camera-inner-source.zip';zip_file.write_bytes(inner)
            plan,_=pkg.plan_import(cfg,zip_file,target,head)
            self.assertTrue(plan['dry_run_ok']);self.assertEqual(len(plan['actions']),24)
            self.assertEqual(len(plan['preserved_parent']),1)
            self.assertEqual(plan['preserved_parent'][0]['target_guid'],'b01a0001000000000000000000000001')
            receipt=pkg.stage(cfg,zip_file,target,head)
            self.assertEqual(receipt['installed'],24)
            self.assertEqual(pkg.restore(target,Path(receipt['journal']))['restored'],24)
            self.assertFalse((target/'Assets/B01Packages/CAMERA001.meta').exists())
            self.assertIn('b01a0001',(target/'Assets/B01Packages.meta').read_text())
            with self.assertRaises(pkg.GateError): pkg.stage(cfg,zip_file,target,head,fault_after=3)
            self.assertFalse((target/'Assets/B01Packages/CAMERA001.meta').exists())
            self.assertFalse((target/'Assets/B01Packages/CAMERA001/Runtime/Camera001Rig.cs').exists())
            invalid=dict(cfg,zip_sha256='f'*64)
            with self.assertRaises(pkg.GateError):pkg.plan_import(invalid,zip_file,target,head)

if __name__=='__main__':unittest.main(verbosity=2)
