import hashlib, importlib.util, io, json, os, shutil, subprocess, sys, tempfile, unittest, zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT));sys.path.insert(0,str(ROOT.parent/"PACKAGE-AUTO-001" if (ROOT.parent/"PACKAGE-AUTO-001").is_dir() else ROOT.parent))
import receiver_w14 as v
import world_upgrade as w
import package_auto as a
ART=Path(os.environ.get('B01_W14_ARTIFACTS','/mnt/data'))
OLD=ART/'MAPGENWORLD_R2_PRODUCER_ARTIFACT.zip'

def blob_to_zip(outer_path,inner_path):
    with zipfile.ZipFile(outer_path) as z:return z.read(inner_path)
def old_zip():return blob_to_zip(OLD,'mapgenworld-evidence/package/MAPGENWORLD001_SOURCE_FULL.zip')
def world_new_zip():return blob_to_zip(ART/v.PINS['WORLD_R11']['outer'],v.PINS['WORLD_R11']['inner'])
def assets_from_zip(data):
    with zipfile.ZipFile(io.BytesIO(data)) as z:return {p:z.read(p) for p in z.namelist() if p.startswith('Assets/')}
def mkzip(entries):
    out=io.BytesIO()
    with zipfile.ZipFile(out,'w',zipfile.ZIP_STORED) as z:
        for p,d in sorted(entries.items()):z.writestr(p,d)
    return out.getvalue()
def git(*args,cwd):
    p=subprocess.run(['git',*args],cwd=cwd,capture_output=True,text=True,check=True)
    return p.stdout.strip()

class Receipts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.original={k:(ART/p['outer']).read_bytes() for k,p in v.PINS.items()}
    def test_01_four_actual_complete_artifacts(self):
        for kind,dat in self.original.items():
            with self.subTest(kind=kind):
                info,_,_=v.verify(kind,dat)
                self.assertEqual(info['file_sha256_pass'],v.PINS[kind]['records'])
    def test_02_actual_cross_package_guid_and_scope(self):
        result={n:v.verify(n,d) for n,d in self.original.items()}
        cross=v.cross_check(result)
        self.assertEqual(cross['source_collisions'],0)
        self.assertTrue(any(x['path']=='Assets/B01Packages.meta' for x in cross['preserve_only_parent_meta']))
    def test_03_wrong_original_zip_sha_rejected(self):
        z=v.read_zip(self.original['BUILD_R14']);name=v.PINS['BUILD_R14']['inner'];z[name]=z[name][:-3]+b'BAD'
        with self.assertRaisesRegex(v.GateError,'INNER_ZIP_SHA'):v.verify('BUILD_R14',mkzip(z))
    def test_04_missing_unity_meta_even_forged_manifest(self):
        kind='WORLD_R11';z=v.read_zip(self.original[kind]);p=v.PINS[kind]['inner'];files=v.read_zip(z[p]);removed=next(x for x in files if x.endswith('.cs.meta'));del files[removed]
        m=json.loads(files[v.PINS[kind]['manifest']]);m['files']=[r for r in m['files'] if r['path']!=removed];files[v.PINS[kind]['manifest']]=json.dumps(m).encode();z[p]=mkzip(files)
        with self.assertRaises(v.GateError):v.verify(kind,mkzip(z))
    def test_05_casefold_conflicting_member_rejected(self):
        with self.assertRaisesRegex(v.GateError,'DUPLICATE_PATH'):v.read_zip(mkzip({'A/Foo.cs':b'x','A/foo.cs':b'y'}))
    def test_06_path_traversal_rejected(self):
        with self.assertRaisesRegex(v.GateError,'UNSAFE_PATH'):v.read_zip(mkzip({'Assets/../bad.cs':b'bad'}))
    def test_07_windows_device_name_rejected(self):
        with self.assertRaisesRegex(v.GateError,'WINDOWS_NAME'):v.read_zip(mkzip({'Assets/CON.cs':b'x'}))
    def test_08_raw_data_hash_mismatch_under_manifest(self):
        kind='CAMERA_R122';z=v.read_zip(self.original[kind]);p=v.PINS[kind]['inner'];files=v.read_zip(z[p]);path=next(x for x in files if x.endswith('.cs'));files[path]+=b'\n// mutated';z[p]=mkzip(files)
        with self.assertRaises(v.GateError):v.verify(kind,mkzip(z))
    def test_09_missing_external_manifest_rejected(self):
        kind='ECONLEG_GUARD';z=v.read_zip(self.original[kind]);del z['source-manifest.json']
        with self.assertRaisesRegex(v.GateError,'EXTERNAL_MANIFEST_REQUIRED'):v.verify(kind,mkzip(z))
    def test_10_foreign_asmdef_ref_remains_unresolved(self):
        result={n:v.verify(n,d) for n,d in self.original.items()}
        missing=v.cross_check(result)['external_assembly_refs'];self.assertTrue(any(x['external']=='B01.Legendary.Unity' for x in missing))
        self.assertTrue(any(x['external']=='B01.MAPGEN001.Runtime' for x in missing))
    def test_11_fake_external_manifest_sha_rejected(self):
        kind='ECONLEG_GUARD';z=v.read_zip(self.original[kind]);m=json.loads(z['source-manifest.json']);m['producer_head']='0'*40;z['source-manifest.json']=json.dumps(m).encode()
        with self.assertRaisesRegex(v.GateError,'SOURCE_SHA_MISMATCH'):v.verify(kind,mkzip(z))

class WorldUpgrade(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup);self.p=Path(self.tmp.name)
        self.repo=self.p/'repo';self.repo.mkdir();git('init','-q',cwd=self.repo);git('config','user.email','test@example.com',cwd=self.repo);git('config','user.name','G0 test',cwd=self.repo)
        self.o=self.p/'r2.zip';self.o.write_bytes(old_zip());self.n=self.p/'r11.zip';self.n.write_bytes(world_new_zip())
        for name,body in assets_from_zip(old_zip()).items():
            t=self.repo/name;t.parent.mkdir(parents=True,exist_ok=True);t.write_bytes(body)
        root=self.repo/'Assets/B01Packages.meta';root.write_text('fileFormatVersion: 2\nguid: b01a0001000000000000000000000001\n')
        (self.repo/'ProjectSettings').mkdir();(self.repo/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.3.14f1\n')
        (self.repo/'Packages').mkdir();(self.repo/'Packages/manifest.json').write_text('{"dependencies":{"com.unity.render-pipelines.universal":"17.3.0"}}')
        git('add','.',cwd=self.repo);git('commit','-qm','synthetic frozen RC2-like reference, never active game',cwd=self.repo)
        self.head=git('rev-parse','HEAD',cwd=self.repo);self.s=self.p/'sandbox';a.create_sandbox(self.repo,self.s,self.head)
    def test_12_plan_exact_five_and_two(self):
        result=w.plan(self.s,self.o,self.n)[-1];self.assertEqual((result['replacements'],result['additions']),(5,2))
    def test_13_real_stage_and_verified_rollback(self):
        original={p:(self.s/p).read_bytes() for p in assets_from_zip(old_zip())}
        r=w.stage(self.s,self.o,self.n);self.assertEqual(r['upgraded'],7)
        for k,d in assets_from_zip(world_new_zip()).items():self.assertEqual((self.s/k).read_bytes(),d)
        returned=w.rollback(self.s,r['journal']);self.assertEqual(returned['restored'],7)
        for k,d in original.items():self.assertEqual((self.s/k).read_bytes(),d)
        for k in set(assets_from_zip(world_new_zip()))-set(original):self.assertFalse((self.s/k).exists())
    def test_14_interrupted_stage_automatic_rollback(self):
        original={p:(self.s/p).read_bytes() for p in assets_from_zip(old_zip())}
        with self.assertRaisesRegex(w.Blocked,'INJECTED_FAILURE'):w.stage(self.s,self.o,self.n,fault_after=3)
        for p,d in original.items():self.assertEqual((self.s/p).read_bytes(),d)
    def test_15_tampered_existing_target_refused(self):
        target=self.s/'Assets/B01Packages/MAPGENWORLD001/Runtime/MapWorldPolicy.cs';target.write_bytes(b'TAMPER')
        with self.assertRaisesRegex(w.Blocked,'TARGET_BASE_MISMATCH'):w.plan(self.s,self.o,self.n)
    def test_16_wrong_source_zip_refused(self):
        self.n.write_bytes(b'not a real package')
        with self.assertRaisesRegex(w.Blocked,'ORIGINAL_ZIP_SHA_MISMATCH'):w.plan(self.s,self.o,self.n)
    def test_17_non_sandbox_repo_refused(self):
        with self.assertRaisesRegex(w.Blocked,'NOT_DISPOSABLE'):w.plan(self.repo,self.o,self.n)
    def test_18_changed_staged_file_refuses_rollback(self):
        r=w.stage(self.s,self.o,self.n)
        dest=self.s/'Assets/B01Packages/MAPGENWORLD001/Runtime/MapGenWorldAdapter.cs';dest.write_bytes(b'external modification')
        with self.assertRaisesRegex(w.Blocked,'ROLLBACK_MODIFIED'):w.rollback(self.s,r['journal'])
    def test_19_wrong_worktree_head_refused(self):
        git('commit','--allow-empty','-qm','synthetic unrelated change',cwd=self.s)
        with self.assertRaisesRegex(w.Blocked,'WRONG_GIT_HEAD'):w.plan(self.s,self.o,self.n)
    def _normalized_qa_archive(self,kind):
        info,all_files,_=v.verify(kind,(ART/v.PINS[kind]['outer']).read_bytes())
        payload={k:d for k,d in all_files.items() if k.startswith('Assets/')}
        name='B01_W14_QA_ONLY_MANIFEST.json'
        payload[name]=json.dumps({'source_file_sha256_by_path':{p:v.digest(d) for p,d in payload.items()},'source_file_size_by_path':{p:len(d) for p,d in payload.items()}},sort_keys=True).encode()
        data=mkzip(payload)
        path=self.p/(kind+'.qa-source-only.zip');path.write_bytes(data)
        pin=v.PINS[kind]
        # This archive is explicitly synthesized from verified ORIGINAL bytes for disposable QA,
        # not a substitute release artifact and never used as an authorized live installer.
        conf={'id':kind+'-QA-ONLY','revision':'W14','source_sha':pin['source'],
              'zip_sha256':v.digest(data),'zip_bytes':len(data),'zip_entries':len(payload),
              'manifest_path':name,'owned_prefixes':[pin['root']+'/'],
              'owned_root_metas':[pin['meta']],
              'preserve_parent_meta':['Assets/B01Packages.meta','Assets/B01Construction.meta'],
              'unity_editor_version':'6000.3.14f1'}
        return path,conf,len([p for p in payload if p.startswith('Assets/')])
    def test_21_real_build_r14_source_transaction_and_rollback(self):
        p,c,n=self._normalized_qa_archive('BUILD_R14')
        check,assets=a.verify_package(c,p);self.assertEqual(check['status'],'PASS_SOURCE_STATIC_NOT_NATIVE')
        dry,_=a.plan_import(c,p,self.s);self.assertTrue(dry['dry_run_ok'],dry['conflicts'])
        installed=a.stage(c,p,self.s);self.assertEqual(installed['installed'],n)
        rolled=a.restore(self.s,installed['journal']);self.assertEqual(rolled['restored'],n)
    def test_22_real_camera_r122_source_transaction_parent_preserved(self):
        p,c,n=self._normalized_qa_archive('CAMERA_R122');pre=(self.s/'Assets/B01Packages.meta').read_bytes()
        dry,_=a.plan_import(c,p,self.s);self.assertTrue(dry['dry_run_ok'],dry['conflicts'])
        self.assertEqual(len(dry['preserved_parent']),1)
        installed=a.stage(c,p,self.s);self.assertEqual(installed['installed'],n-1)
        self.assertEqual((self.s/'Assets/B01Packages.meta').read_bytes(),pre)
        rolled=a.restore(self.s,installed['journal']);self.assertEqual(rolled['restored'],n-1)
    def test_23_real_guard_source_transaction_and_rollback(self):
        p,c,n=self._normalized_qa_archive('ECONLEG_GUARD')
        dry,_=a.plan_import(c,p,self.s);self.assertTrue(dry['dry_run_ok'],dry['conflicts'])
        installed=a.stage(c,p,self.s);self.assertEqual(installed['installed'],n-1)
        rolled=a.restore(self.s,installed['journal']);self.assertEqual(rolled['restored'],n-1)
    def test_24_injected_nonworld_stage_fails_closed(self):
        p,c,n=self._normalized_qa_archive('BUILD_R14')
        with self.assertRaisesRegex(a.GateError,'After 2 files'):a.stage(c,p,self.s,fault_after=2)
        self.assertFalse((self.s/'Assets/B01Construction/BUILD001.meta').exists())
    def test_20_missing_parent_guid_refused(self):
        (self.s/'Assets/B01Packages.meta').write_bytes(b'invalid')
        with self.assertRaises(w.Blocked):w.plan(self.s,self.o,self.n)

if __name__=='__main__':unittest.main()
