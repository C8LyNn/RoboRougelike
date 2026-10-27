"""Executable synthetic-negative fixtures; not Unity native evidence."""
import hashlib
import io
import json
import os
import subprocess
from pathlib import Path
import tempfile
import unittest
import zipfile
import sys
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import gate


def sample(package='PARTSIZE-001'):
    cfg=gate.SPEC[package]
    root=cfg['asset']; docs=cfg['roots'][1]
    payload={
       root+'Runtime/Controller.cs':b'public class DemoController {}\n',
       root+'Tests/EditMode/PositiveTests.cs':b'public class PositiveTests {}\n',
       root+'Tests/PlayMode/NegativePhysicsTests.cs':b'public class NegativePhysicsTests {}\n',
       root+'Demo/Demo.unity':b'%YAML 1.1\n',
       root+'Runtime/Core.asmdef':json.dumps({'name':package+'.Runtime','references':[]}).encode(),
       docs+'README.md':b'Owned demo and APIs\n',
       docs+'INSTALL_FOR_CODEX.md':b'only after G2\n',
       docs+'ROLLBACK.md':b'use PACKAGE-AUTO-001 detached worktree journal\n',
       docs+'TEST_PLAN.md':b'positive/negative native QA needed\n'
    }
    if package=='CHL-003':
        payload[root+'Rigs/Light.prefab']=b'%YAML 1.1 light Rigidbody\n'
        payload[root+'Rigs/Heavy.prefab']=b'%YAML 1.1 heavy Rigidbody\n'
    originals=list(p for p in payload if p.startswith('Assets/'))
    for idx,path in enumerate(originals):
        payload[path+'.meta']=(f'fileFormatVersion: 2\nguid: {idx+1:032x}\n').encode()
    return payload

def build(tmp,payload,package='PARTSIZE-001',state='QA',repack=None):
    archive=tmp/'package.zip'
    with zipfile.ZipFile(archive,'w',compression=zipfile.ZIP_DEFLATED) as z:
        for p,content in payload.items():z.writestr(p,content)
    receipt={
        'package_id':package,'version':'R1','producer':gate.SPEC[package]['producer'],
        'source_sha':'a'*40,'producer_pr':'https://github.com/C8LyNn/RoboRougelike/pull/999',
        'state':state,'zip_sha256':gate.digest(archive.read_bytes()),
        'files':[{'path':p,'sha256':gate.digest(v),'bytes':len(v)} for p,v in payload.items()],
        'external_assemblies':[],
    }
    return archive,receipt

class PhysicalSourceGate(unittest.TestCase):
    def setUp(self):
        self.d=tempfile.TemporaryDirectory();self.tmp=Path(self.d.name)
        self.data=sample();self.zip,self.receipt=build(self.tmp,self.data)
    def tearDown(self):self.d.cleanup()
    def check(self):return gate.verify(self.zip,self.receipt)
    def rebuild(self):self.zip,self.receipt=build(self.tmp,self.data)
    def fails(self,code,fn=None):
        if fn:fn()
        with self.assertRaises(gate.GateError) as ex:self.check()
        self.assertIn(code,str(ex.exception))
    def test_01_full_partsize_has_g0_but_no_native_g2(self):
        r=self.check();self.assertEqual(r['g0_source'],'PASS_SOURCE_BYTES');self.assertFalse(r['codex_ready']);self.assertEqual(r['native_unity'],'NOT_RUN_BY_RECEIVER')
    def test_02_full_chl_has_two_rigs_not_physical_proof(self):
        a,b=build(self.tmp,sample('CHL-003'),'CHL-003');r=gate.verify(a,b);self.assertEqual(r['package_id'],'CHL-003');self.assertEqual(r['g2'],'HOLD')
    def test_03_wrong_outer_sha(self):self.fails('archive_sha256_mismatch',lambda:self.receipt.update(zip_sha256='0'*64))
    def test_04_missing_zip_entry(self):
        self.data.pop(next(p for p in self.data if p.endswith('README.md')));self.rebuild();self.fails('missing_handoff_document')
    def test_05_file_tamper_only_zip(self):
        p=next(p for p in self.data if p.endswith('Controller.cs'));self.data[p]=b'mutated';a,b=build(self.tmp,self.data);self.zip=a;self.receipt['zip_sha256']=b['zip_sha256'];self.fails('per_file_sha256_or_size_mismatch')
    def test_06_zip_traversal(self):
        self.data['../evil.cs']=b'bad';self.rebuild();self.fails('unsafe_path')
    def test_07_casefold_collision(self):
        p=next(p for p in self.data if p.endswith('Controller.cs'));self.data[p.lower()]=self.data[p];self.rebuild();self.fails('manifest_duplicate_casefold_path')
    def test_08_reserved_windows_con(self):
        self.data['Assets/B01Packages/PARTSIZE001/CON.cs']=b'bad';self.rebuild();self.fails('unsafe_path')
    def test_09_duplicate_guid(self):
        paths=sorted(p for p in self.data if p.endswith('.meta'));self.data[paths[1]]=self.data[paths[0]];self.rebuild();self.fails('guid_collision')
    def test_10_missing_meta(self):
        self.data.pop(next(p for p in self.data if p.endswith('Controller.cs.meta')));self.rebuild();self.fails('missing_meta')
    def test_11_orphan_meta(self):
        self.data['Assets/B01Packages/PARTSIZE001/Ghost.cs.meta']=b'fileFormatVersion: 2\nguid: ffffffffffffffffffffffffffffffff\n';self.rebuild();self.fails('orphan_meta')
    def test_12_asmdef_missing_upstream(self):
        p=next(p for p in self.data if p.endswith('.asmdef'));self.data[p]=json.dumps({'name':'Partsize.Runtime','references':['B01.Unavailable']}).encode();self.rebuild();self.fails('unresolved_asmdef_reference')
    def test_13_unauthorized_gamefile(self):
        self.data['Assets/R3/BadPatch.cs']=b'hack';self.rebuild();self.fails('out_of_owner_scope')
    def test_14_missing_demo(self):
        for p in list(self.data):
            if p.endswith('Demo.unity') or p.endswith('Demo.unity.meta'):del self.data[p]
        self.rebuild();self.fails('missing_real_demo_scene')
    def test_15_missing_negative_test(self):
        for p in list(self.data):
            if 'NegativePhysicsTests.cs' in p:del self.data[p]
        self.rebuild();self.fails('missing_playmode_test_source')
    def test_16_chl_one_rig_rejected(self):
        data=sample('CHL-003')
        for p in list(data):
            if p.endswith('Heavy.prefab') or p.endswith('Heavy.prefab.meta'):del data[p]
        a,b=build(self.tmp,data,'CHL-003')
        with self.assertRaisesRegex(gate.GateError,'two_rigidbody'):gate.verify(a,b)
    def test_17_wrong_owner(self):self.fails('wrong_version_or_producer',lambda:self.receipt.update(producer='B01-20'))
    def test_18_no_implicit_codex_claim(self):
        self.receipt['state']='CODEX_CLAIMED';self.fails('codex_claim_requires_direct_github_ack_reference')
    def test_19_offered_frozen_requires_pr(self):
        self.receipt['state']='OFFERED_FROZEN';self.receipt['producer_pr']='';self.fails('frozen_offer_missing_pr')
    def test_20_target_occupied_path_mismatch(self):
        p=next(p for p in self.data if p.endswith('Controller.cs'))
        with self.assertRaisesRegex(gate.GateError,'target_path_or_guid_conflicts'):gate.verify(self.zip,self.receipt,{p:{'sha256':'0'*64}})
    def test_21_target_existing_same_bytes_allowed(self):
        p=next(p for p in self.data if p.endswith('Controller.cs'))
        r=gate.verify(self.zip,self.receipt,{p:{'sha256':gate.digest(self.data[p])}});self.assertEqual(r['target_identical_files'],1)
    def test_22_target_different_asset_same_guid_blocked(self):
        meta=next(p for p in self.data if p.endswith('Controller.cs.meta'))
        guid=gate.GUID.findall(self.data[meta].decode())[0]
        with self.assertRaisesRegex(gate.GateError,'target_path_or_guid_conflicts'):gate.verify(self.zip,self.receipt,{'Assets/Other.meta':{'sha256':'0'*64,'guid':guid}})
    def test_23_github_blob_provenance_good(self):
        tree={p:gate.blob_sha1(v) for p,v in self.data.items()};self.assertEqual(gate.verify(self.zip,self.receipt,source_tree=tree)['g0_source'],'PASS_SOURCE_BYTES')
    def test_24_github_blob_provenance_wrong(self):
        tree={p:gate.blob_sha1(v) for p,v in self.data.items()};p=next(iter(tree));tree[p]='0'*40
        with self.assertRaisesRegex(gate.GateError,'source_git_blob_mismatch'):gate.verify(self.zip,self.receipt,source_tree=tree)
    def test_25_fake_ready_state_not_recognized(self):
        with self.assertRaisesRegex(gate.GateError,'state_requires_human_handoff_disposition'):self.check_with_state('CODEX_READY')
    def check_with_state(self,state):self.receipt['state']=state;return self.check()
    def test_26_deny_returned_without_new_version(self):
        self.receipt['state']='RETURNED_FOR_REWORK';self.fails('state_requires_human_handoff_disposition')
    def test_27_zip_symlink_block(self):
        p=next(p for p in self.data if p.endswith('Controller.cs'))
        zi=zipfile.ZipInfo(p);zi.create_system=3;zi.external_attr=(0o120777<<16)
        with zipfile.ZipFile(self.zip,'w') as z:
            for path,value in self.data.items():z.writestr(zi if path==p else path,value)
        self.receipt['zip_sha256']=gate.digest(self.zip.read_bytes());self.fails('zip_directory_or_symlink')
    def test_28_no_automatic_g2_even_with_ack_url(self):
        self.receipt.update(state='CODEX_CLAIMED',codex_ack_link='https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-123')
        tree={p:gate.blob_sha1(v) for p,v in self.data.items()}
        r=gate.verify(self.zip,self.receipt,source_tree=tree);self.assertEqual(r['codex_ack'],'NOT_VERIFIED');self.assertFalse(r['codex_ready']);self.assertEqual(r['g2'],'HOLD')
    def test_29_frozen_without_git_proof_denied(self):
        self.receipt['state']='OFFERED_FROZEN';self.fails('frozen_source_requires_local_git_commit_proof')
    def test_30_real_pinned_git_commit_source_proof(self):
        repo=self.tmp/'repo';repo.mkdir()
        subprocess.run(['git','-C',str(repo),'init','-q'],check=True)
        subprocess.run(['git','-C',str(repo),'config','core.autocrlf','false'],check=True)
        subprocess.run(['git','-C',str(repo),'config','user.email','qa@example.invalid'],check=True)
        subprocess.run(['git','-C',str(repo),'config','user.name','B01 test'],check=True)
        for name,data in self.data.items():
            file=repo/name;file.parent.mkdir(parents=True,exist_ok=True);file.write_bytes(data)
        subprocess.run(['git','-C',str(repo),'add','.'],check=True)
        subprocess.run(['git','-C',str(repo),'commit','-qm','frozen fixture'],check=True)
        sha=subprocess.run(['git','-C',str(repo),'rev-parse','HEAD'],check=True,capture_output=True,text=True).stdout.strip()
        self.receipt['source_sha']=sha;self.receipt['state']='OFFERED_FROZEN'
        tree=gate.git_tree_at_commit(repo,sha)
        self.assertEqual(gate.verify(self.zip,self.receipt,source_tree=tree)['g0_source'],'PASS_SOURCE_BYTES')
        tree[next(iter(tree))]='0'*40
        with self.assertRaisesRegex(gate.GateError,'source_git_blob_mismatch'):
            gate.verify(self.zip,self.receipt,source_tree=tree)
    def test_31_invalid_git_commit_denied(self):
        repo=self.tmp/'repo';repo.mkdir()
        subprocess.run(['git','-C',str(repo),'init','-q'],check=True)
        with self.assertRaisesRegex(gate.GateError,'git_source_unavailable'):
            gate.git_tree_at_commit(repo,'a'*40)
    def test_32_incomplete_zip_with_original_manifest_denied(self):
        with zipfile.ZipFile(self.zip,'w') as z:
            for p,data in self.data.items():
                if not p.endswith('README.md'):z.writestr(p,data)
        self.receipt['zip_sha256']=gate.digest(self.zip.read_bytes())
        self.fails('zip_count_mismatch')
    def test_33_casefold_target_conflict_preflight(self):
        p=next(p for p in self.data if p.endswith('Controller.cs'))
        with self.assertRaisesRegex(gate.GateError,'target_path_or_guid_conflicts'):
            gate.verify(self.zip,self.receipt,{p:{'sha256':'0'*64}})
    def test_34_unknown_asmdef_guid_reference_rejected(self):
        p=next(p for p in self.data if p.endswith('.asmdef'))
        self.data[p]=json.dumps({'name':'Demo.Runtime','references':['GUID:0123456789abcdef0123456789abcdef']}).encode();self.rebuild();self.fails('unresolved_asmdef_reference')
    def test_35_non_zip_false_positive_rejected(self):
        self.zip.write_bytes(b'not a zip')
        self.receipt['zip_sha256']=gate.digest(self.zip.read_bytes())
        self.fails('corrupt_zip')
    def test_37_existing_target_case_differs_rejected(self):
        p=next(p for p in self.data if p.endswith('Controller.cs'))
        with self.assertRaisesRegex(gate.GateError,'target_path_or_guid_conflicts'):
            gate.verify(self.zip,self.receipt,{p.lower():{'sha256':gate.digest(self.data[p])}})
    def test_38_target_index_duplicate_casefold_rejected(self):
        p=next(p for p in self.data if p.endswith('Controller.cs'))
        with self.assertRaisesRegex(gate.GateError,'target_casefold_duplicate'):
            gate.verify(self.zip,self.receipt,{p:{'sha256':gate.digest(self.data[p])},p.lower():{'sha256':'0'*64}})
    def test_39_source_git_evidence_marked_not_run_without_repo(self):
        self.assertEqual(self.check()['git_source_crosscheck'],'NOT_RUN')
    def test_40_asmdef_local_guid_reference_resolves(self):
        root='Assets/B01Packages/PARTSIZE001/'
        dep=root+'Runtime/Dep.asmdef'
        dep_guid='f'*32
        self.data[dep]=json.dumps({'name':'Dependency.Runtime','references':[]}).encode()
        self.data[dep+'.meta']=('fileFormatVersion: 2\nguid: '+dep_guid+'\n').encode()
        primary=next(p for p in self.data if p.endswith('/Core.asmdef'))
        self.data[primary]=json.dumps({'name':'PARTSIZE-001.Runtime','references':['GUID:'+dep_guid]}).encode()
        self.rebuild();self.assertEqual(self.check()['g0_source'],'PASS_SOURCE_BYTES')
    def test_41_external_guid_must_be_explicit(self):
        p=next(p for p in self.data if p.endswith('.asmdef'))
        external='f'*32
        self.data[p]=json.dumps({'name':'Partsize.Runtime','references':['GUID:'+external]}).encode()
        self.rebuild();self.fails('unresolved_asmdef_reference')
        self.receipt['external_assembly_guids']=[external]
        self.assertEqual(self.check()['declared_external_assembly_guids'],[external])
    def test_36_invalid_target_index_blocked(self):
        p=next(p for p in self.data if p.endswith('Controller.cs'))
        with self.assertRaisesRegex(gate.GateError,'invalid_target_index_entry'):gate.verify(self.zip,self.receipt,{p:{'sha256':'hello'}})

if __name__=='__main__':unittest.main(verbosity=2)
