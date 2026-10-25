"""Local integration of real MAPGEN R3.4 producer bytes plus synthetic broken packages."""
import copy
import io
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import unittest
import zipfile

BASE = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(BASE))
import package_auto as a

ZIP = Path(os.environ.get('B01_REAL_MAPGEN_ZIP', str(BASE / 'tests/fixtures/MAPGEN001_R3_4_FULL_SOURCE_AND_QA.zip')))
CONFIG = a.read_json(BASE / 'configs/MAPGEN001.json')


def run(cmd, cwd):
    p = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True)
    if p.returncode: raise RuntimeError(f'{cmd}: {p.stdout} {p.stderr}')
    return p.stdout.strip()


class PackageAutoTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if not ZIP.is_file():
            raise RuntimeError(f'actual producer inner ZIP fixture is required: {ZIP}')
        cls.real_info, cls.real_contents = a.verify_package(CONFIG, ZIP)

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.t = Path(self.temp.name)
        self.repo = self.t / 'repo'
        self.repo.mkdir()
        run(['git','init','-q'],self.repo)
        run(['git','config','user.name','B01 QA'],self.repo)
        run(['git','config','user.email','b01-qa@example.invalid'],self.repo)
        (self.repo/'README.md').write_text('baseline\n')
        (self.repo/'ProjectSettings').mkdir()
        (self.repo/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.3.14f1\n')
        (self.repo/'Packages').mkdir()
        (self.repo/'Packages/manifest.json').write_text(json.dumps({'dependencies':{'com.unity.test-framework':'1.6.0','com.unity.render-pipelines.universal':'17.3.0'}}))
        # Pre-existing shared meta with CAMERA's different GUID, intentionally preserved.
        (self.repo/'Assets').mkdir()
        (self.repo/'Assets/B01Packages.meta').write_text('fileFormatVersion: 2\nguid: b01c0000000000000000000000000001\nfolderAsset: yes\n')
        run(['git','add','.'],self.repo)
        run(['git','commit','-q','-m','baseline'],self.repo)
        self.sha = run(['git','rev-parse','HEAD'],self.repo)
        self.sandbox = self.t/'sandbox'
        a.create_sandbox(self.repo,self.sandbox,self.sha)

    def assert_gate(self, code, func):
        with self.assertRaises(a.GateError) as caught:
            func()
        self.assertEqual(code,caught.exception.code)

    def make_bad_zip(self, mutator, recalc_manifest=False):
        with zipfile.ZipFile(ZIP) as z:
            contents={p:z.read(p) for p in z.namelist()}
        mutator(contents)
        if recalc_manifest:
            m=json.loads(contents[CONFIG['manifest_path']]);
            for k in ('source_file_sha256_by_path','qa_tool_sha256_by_path'):
                m[k]={p:a.sha256(contents[p]) for p in m[k] if p in contents}
            m['source_file_size_by_path']={p:len(contents[p]) for p in m['source_file_size_by_path'] if p in contents}
            m['git_blob_sha1_by_path']={p:__import__('hashlib').sha1(b'blob '+str(len(contents[p])).encode()+b'\x00'+contents[p]).hexdigest() for p in m.get('git_blob_sha1_by_path',{}) if p in contents}
            contents[CONFIG['manifest_path']]=json.dumps(m).encode()
        dest=self.t/'mutation.zip'
        with zipfile.ZipFile(dest,'w',compression=zipfile.ZIP_DEFLATED) as z:
            for p,b in contents.items():z.writestr(p,b)
        c=copy.deepcopy(CONFIG)
        c['zip_sha256']=None;c['zip_bytes']=None;c['zip_entries']=None
        return c,dest

    def test_01_real_package_integrity(self):
        i,_=a.verify_package(CONFIG, ZIP)
        self.assertEqual((i['zip_bytes'],i['zip_entries'],i['sha256_entries']), (83928,62,61))
        self.assertEqual(i['zip_sha256'],'57365e4f4f6f453f0d09366a53c6927ccf87e53df54b4d12bb086b7616f699cb')
        self.assertEqual(i['unity_guid_count'],22)

    def test_02_preserves_parent_meta_and_preflight(self):
        p,_=a.plan_import(CONFIG,ZIP,self.sandbox,self.sha)
        self.assertTrue(p['dry_run_ok']);self.assertEqual(len(p['preserved_parent']),1)
        self.assertEqual(p['preserved_parent'][0]['target_guid'],'b01c0000000000000000000000000001')
        self.assertEqual(p['G2_HANDOFF'],'HOLD')
        self.assertFalse((self.sandbox/'Assets/B01Packages/MAPGEN001.meta').exists())

    def test_03_install_and_rollback_and_original_preserved(self):
        parent=self.sandbox/'Assets/B01Packages.meta';prior=parent.read_bytes()
        result=a.stage(CONFIG,ZIP,self.sandbox,self.sha)
        self.assertGreater(result['installed'],20)
        self.assertEqual(parent.read_bytes(),prior)
        self.assertTrue((self.sandbox/'Assets/B01Packages/MAPGEN001/Resources/MAPGEN001_BlocksURP.shader').is_file())
        restored=a.restore(self.sandbox,result['journal'])
        self.assertEqual(restored['restored'],result['installed'])
        self.assertEqual(parent.read_bytes(),prior)
        self.assertFalse((self.sandbox/'Assets/B01Packages/MAPGEN001.meta').exists())

    def test_04_wrong_complete_zip_sha_is_rejected(self):
        c=copy.deepcopy(CONFIG);c['zip_sha256']='0'*64
        self.assert_gate('ZIP_SHA256_MISMATCH',lambda:a.verify_package(c,ZIP))

    def test_05_tampered_source_file_detected(self):
        c,p=self.make_bad_zip(lambda d:d.__setitem__('Assets/B01Packages/MAPGEN001/Runtime/MapGenBuilder.cs',b'evil'))
        self.assert_gate('FILE_SHA256_MISMATCH',lambda:a.verify_package(c,p))

    def test_06_incomplete_zip_detected(self):
        c,p=self.make_bad_zip(lambda d:d.pop('Assets/B01Packages/MAPGEN001/Resources/MAPGEN001_BlocksURP.shader'))
        self.assert_gate('MANIFEST_SCOPE_MISMATCH',lambda:a.verify_package(c,p))
        # With explicit 62 requirement even before scanning manifest.
        c['zip_entries']=62
        self.assert_gate('ZIP_INCOMPLETE',lambda:a.verify_package(c,p))

    def test_07_missing_meta_rejected_even_if_manifest_forged(self):
        path='Assets/B01Packages/MAPGEN001/Resources/MAPGEN001_BlocksURP.shader.meta'
        c,p=self.make_bad_zip(lambda d:d.pop(path),recalc_manifest=True)
        self.assert_gate('META_PAIR_MISSING',lambda:a.verify_package(c,p))

    def test_08_duplicate_source_guid_even_if_manifest_forged(self):
        first='Assets/B01Packages/MAPGEN001/Resources/MAPGEN001_BlocksURP.shader.meta'
        second='Assets/B01Packages/MAPGEN001/Runtime/MapGenBuilder.cs.meta'
        def mutate(d):
            guid=a.GUID_RE.search(d[second]).group(1)
            d[first]=a.GUID_RE.sub(b'guid: '+guid,d[first])
        c,p=self.make_bad_zip(mutate,recalc_manifest=True)
        self.assert_gate('SOURCE_GUID_COLLISION',lambda:a.verify_package(c,p))

    def test_09_dirty_worktree_rejected(self):
        (self.sandbox/'dirty.txt').write_text('mine')
        self.assert_gate('TARGET_DIRTY',lambda:a.plan_import(CONFIG,ZIP,self.sandbox,self.sha))

    def test_10_live_ordinary_repo_rejected(self):
        self.assert_gate('NOT_ISOLATED_SANDBOX',lambda:a.plan_import(CONFIG,ZIP,self.repo,self.sha))

    def test_11_wrong_target_sha_rejected(self):
        self.assert_gate('TARGET_SHA_MISMATCH',lambda:a.plan_import(CONFIG,ZIP,self.sandbox,'0'*40))

    def test_12_fault_injection_auto_rollback(self):
        self.assert_gate('INJECTED_INSTALL_FAILURE',lambda:a.stage(CONFIG,ZIP,self.sandbox,self.sha,fault_after=4))
        self.assertFalse((self.sandbox/'Assets/B01Packages/MAPGEN001.meta').exists())
        journals=list((self.sandbox/a.JOURNAL_DIR).glob('*.json'))
        self.assertEqual(len(journals),1)
        self.assertEqual(a.read_json(journals[0])['state'],'ROLLED_BACK')
        self.assert_gate('ROLLBACK_FILE_MODIFIED',lambda:self._stage_and_tamper())

    def _stage_and_tamper(self):
        report=a.stage(CONFIG,ZIP,self.sandbox,self.sha)
        changed=self.sandbox/'Assets/B01Packages/MAPGEN001/Resources/MAPGEN001_BlocksURP.shader'
        changed.write_text('user-edited')
        a.restore(self.sandbox,report['journal'])

    def test_13_target_guid_collision_rejected(self):
        # A different target-owned file bearing the incoming shader GUID.
        collision=a.GUID_RE.search(self.real_contents['Assets/B01Packages/MAPGEN001/Resources/MAPGEN001_BlocksURP.shader.meta']).group(1).decode()
        (self.repo/'Assets/Other').mkdir(exist_ok=True)
        (self.repo/'Assets/Other/collider.meta').write_text('fileFormatVersion: 2\nguid: '+collision+'\n')
        run(['git','add','.'],self.repo);run(['git','commit','-q','-m','collision'],self.repo)
        sandbox2=self.t/'sandbox2'
        a.create_sandbox(self.repo,sandbox2,run(['git','rev-parse','HEAD'],self.repo))
        p,_=a.plan_import(CONFIG,ZIP,sandbox2)
        self.assertFalse(p['dry_run_ok'])
        self.assertIn('GUID_COLLISION',[x['type'] for x in p['conflicts']])
        self.assert_gate('TARGET_PREFLIGHT_FAILED',lambda:a.stage(CONFIG,ZIP,sandbox2))

    def test_14_existing_owned_file_blocked(self):
        (self.repo/'Assets/B01Packages/MAPGEN001').mkdir(parents=True)
        (self.repo/'Assets/B01Packages/MAPGEN001.meta').write_text('fileFormatVersion: 2\nguid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\n')
        run(['git','add','.'],self.repo);run(['git','commit','-q','-m','occupied'],self.repo)
        s2=self.t/'sandbox2';a.create_sandbox(self.repo,s2,run(['git','rev-parse','HEAD'],self.repo))
        p,_=a.plan_import(CONFIG,ZIP,s2)
        self.assertIn('TARGET_PATH_EXISTS_NO_OVERWRITE',[x['type'] for x in p['conflicts']])

    def test_15_prototype_cannot_be_staged_as_unity(self):
        c=a.read_json(BASE/'configs/LEGENDARY001.json')
        self.assert_gate('PROTOTYPE_NO_UNITY_INSTALL',lambda:a.plan_import(c,ZIP,self.sandbox))

    def test_16_wrong_owned_prefix_denied(self):
        c=copy.deepcopy(CONFIG);c['owned_prefixes']=['Assets/R3/']
        self.assert_gate('NON_OWNED_PATH',lambda:a.plan_import(c,ZIP,self.sandbox))

    def test_17_unsafe_zip_path_detected(self):
        c,p=self.make_bad_zip(lambda d:d.__setitem__('../outside.txt',b'oops'))
        self.assert_gate('UNSAFE_ZIP_PATH',lambda:a.verify_package(c,p))

    def test_20_w9_three_unmodified_full_package_archives(self):
        root=Path(os.environ.get('B01_W9_PACKAGE_DIR',str(BASE/'tests')))
        for package,name in [('CAMERA001','CAMERA_W9_FIXED_FULL_SOURCE.zip'),('BUILD001','BUILD_W9_FIXED_FULL_SOURCE.zip'),('RAC001','RAC_W9_FIXED_FULL_SOURCE.zip')]:
            config=a.read_json(BASE/'configs'/(package+'.json'))
            result,_=a.verify_package(config,root/name)
            self.assertEqual(result['zip_entries'],37)
            self.assertEqual(result['sha256_entries'],36)

    def test_19_unity_version_and_dependency_mismatch(self):
        (self.repo/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2021.3.0f1\n')
        (self.repo/'Packages/manifest.json').write_text(json.dumps({'dependencies': {'com.unity.test-framework':'1.6.0'}}))
        run(['git','add','.'],self.repo);run(['git','commit','-q','-m','wrong engine'],self.repo)
        s2=self.t/'sandbox2';a.create_sandbox(self.repo,s2,run(['git','rev-parse','HEAD'],self.repo))
        report,_=a.plan_import(CONFIG,ZIP,s2)
        conflicts=[x['type'] for x in report['conflicts']]
        self.assertIn('UNITY_VERSION_MISMATCH',conflicts)
        self.assertIn('UNITY_PACKAGE_MISSING',conflicts)

    def test_18_no_overwrite_after_staged(self):
        a.stage(CONFIG,ZIP,self.sandbox)
        self.assert_gate('TARGET_DIRTY',lambda:a.plan_import(CONFIG,ZIP,self.sandbox))

if __name__=='__main__':unittest.main(verbosity=2)
