#!/usr/bin/env python3
"""Executable positive/negative source tests of AIM-VAULT-001 with actual parent receiver."""
from __future__ import annotations
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from PIL import Image, ImageDraw
import aim_evidence_vault as v

PIN='8e6cd46bc5f1229b54e565a1b74ea9387d27073d463f61268809f212a9e2ddf4'
GAME1='a'*40; GAME2='b'*40; ZIP1='c'*64; ZIP2='d'*64
CASES=('CLOSE_STATIC','FAR_STATIC','OCCLUDED','ZOOM_MOVING')

class TestVault(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.base=Path(self.tmp.name)
        self.root=self.base/'private'
        self.receiver=Path(__file__).resolve().parent.parent/'P17'/'verify_aim_receipt.py'
        self.assertEqual(hashlib.sha256(self.receiver.read_bytes()).hexdigest(),PIN)
        v.initialize(self.root)
    def make(self,sid='S01',game=GAME1,zipsha=ZIP1,mode='SYNTHETIC'):
        p=self.root/'sessions'/sid;p.mkdir(mode=0o700)
        frames=[]
        for i,c in enumerate(CASES):
            file=c+'.png';img=Image.new('RGB',(80,50),(10+i,30,40))
            ImageDraw.Draw(img).rectangle((4,4,62,38),outline='white');img.save(p/file)
            b=(p/file).read_bytes()
            group='A' if i%2==0 else 'B';ret={'x':10,'y':10};target={'x':20,'y':10}
            impact={'x':22,'y':10} if i==0 else None
            frames.append({'case':c,'group':group,'input':'LMB' if group=='A' else 'RMB','outcome':'UNVERIFIED',
                'image':{'filename':file,'sha256':hashlib.sha256(b).hexdigest(),'width':80,'height':50},
                'points':{'reticle':ret,'target':target,'impact':impact},
                'metrics':{'reticle_target_px':10.0,'impact_target_px':2.0 if impact else None},'note':'SYNTHETIC'})
        witness=None if mode!='HUMAN_APPROVED' else {'unverified':True,'hq':'dummyHQ000','consent':'dummyCons000','host':'dummyHost0','facilitator':'dummyFacil0'}
        data={'schema':'B01-13/P17/AIM-OBS001/v1','mode':mode,'sid':sid,'game_sha':game,
              'player_zip_sha256':zipsha,'consent_witness':witness,'frames':frames}
        (p/'receipt.json').write_text(json.dumps(data),encoding='utf-8')
        return p
    def seal(self,sid='S01',game=GAME1,zipsha=ZIP1,human=False):
        return v.seal(self.root,sid,self.receiver,game,zipsha,human)
    def both(self):
        self.make('S01');self.seal('S01')
        self.make('S02',GAME2,ZIP2);self.seal('S02',GAME2,ZIP2)
    def report(self,content=b'<html>ORIGINAL PRIVATE IMAGE BYTES</html>'):
        f=self.root/'derived'/'REPORT01.html';f.write_bytes(content)
        return f
    def error(self,fn,reason=None):
        with self.assertRaises(v.VaultError) as ctx: fn()
        if reason:self.assertIn(reason,str(ctx.exception))
    def test_empty_private_vault_verified(self):
        self.assertEqual(v.verify(self.root,self.receiver)['sessions'],0)
    def test_seal_original_receiver_and_four_images(self):
        self.make();self.assertEqual(self.seal()['file_count'],5)
        self.assertEqual(v.verify(self.root,self.receiver)['files_checked'],5)
    def test_two_distinct_build_sessions_accepted(self):
        self.both();self.assertEqual(v.verify(self.root,self.receiver)['sessions'],2)
    def test_duplicate_session_rejected(self):
        self.make();self.seal();self.error(lambda:self.seal(),'SESSION_EXISTS')
    def test_original_receiver_rejects_wrong_zip(self):
        self.make();self.error(lambda:self.seal(zipsha=ZIP2),'ZIP')
    def test_original_receiver_rejects_wrong_group(self):
        p=self.make();f=p/'receipt.json';j=json.loads(f.read_text());j['frames'][0]['input']='RMB';f.write_text(json.dumps(j))
        self.error(lambda:self.seal(),'wrong firing input')
    def test_missing_image_rejected(self):
        p=self.make();(p/'OCCLUDED.png').unlink();self.error(lambda:self.seal(),'EXACT_FOUR')
    def test_untracked_image_rejected(self):
        p=self.make();(p/'extra.png').write_bytes(b'junk');self.error(lambda:self.seal(),'EXACT_FOUR')
    def test_human_requires_explicit_flag(self):
        self.make(mode='HUMAN_APPROVED');self.error(lambda:self.seal(),'HUMAN_EVIDENCE')
    def test_human_attestation_structural_only(self):
        self.make(mode='HUMAN_APPROVED');z=self.seal(human=True)
        self.assertFalse(z['consent_authenticated']);self.assertEqual(v.verify(self.root,self.receiver)['sessions'],1)
    def test_receiver_sha_tamper_refused(self):
        self.make();r=self.base/'fake.py';r.write_bytes(self.receiver.read_bytes()+b'\n# injected')
        self.error(lambda:v.seal(self.root,'S01',r,GAME1,ZIP1,False),'RECEIVER_SHA')
    def test_sealed_image_mutation_detected(self):
        p=self.make();self.seal();(p/'CLOSE_STATIC.png').write_bytes(b'MODIFIED')
        self.error(lambda:v.verify(self.root,self.receiver),'SEALED_BYTES_CHANGED')
    def test_sealed_receipt_mutation_detected(self):
        p=self.make();self.seal();(p/'receipt.json').write_text('{}')
        self.error(lambda:v.verify(self.root,self.receiver),'SEALED_BYTES_CHANGED')
    def test_derived_report_attached_to_two_sessions(self):
        self.both();self.report();v.attach_derived(self.root,'REPORT01.html',['S01','S02'])
        z=v.verify(self.root,self.receiver);self.assertEqual((z['reports'],z['files_checked']),(1,11))
    def test_report_tamper_detected(self):
        self.both();f=self.report();v.attach_derived(self.root,'REPORT01.html',['S01','S02'])
        f.write_bytes(b'MODIFIED');self.error(lambda:v.verify(self.root,self.receiver),'DERIVED_REPORT_CHANGED')
    def test_unregistered_report_fails_verify(self):
        self.report();self.error(lambda:v.verify(self.root,self.receiver),'ORPHAN_OR_MISSING_REPORT')
    def test_reject_report_single_session(self):
        self.make();self.seal();self.report();self.error(lambda:v.attach_derived(self.root,'REPORT01.html',['S01']),'TWO_ACTIVE')
    def test_reject_report_reusing_session(self):
        self.make();self.seal();self.report();self.error(lambda:v.attach_derived(self.root,'REPORT01.html',['S01','S01']),'TWO_ACTIVE')
    def test_reject_unsafe_report_name(self):
        self.both();self.error(lambda:v.attach_derived(self.root,'../../hack.html',['S01','S02']),'INVALID_REPORT_NAME')
    def test_reject_report_symlink(self):
        self.both();f=self.root/'derived'/'REPORT01.html';f.symlink_to(self.receiver)
        self.error(lambda:v.attach_derived(self.root,'REPORT01.html',['S01','S02']),'UNSAFE_OR_MISSING')
    def test_withdraw_explicit_confirmation(self):
        self.make();self.seal();self.error(lambda:v.withdraw(self.root,'S01',False),'REQUIRES_CONFIRM')
    def test_withdraw_removes_linked_report_but_preserves_other_session(self):
        self.both();self.report();v.attach_derived(self.root,'REPORT01.html',['S01','S02'])
        x=v.withdraw(self.root,'S01',True)
        self.assertEqual(x['linked_reports_removed'],1)
        self.assertFalse((self.root/'sessions'/'S01').exists())
        self.assertFalse((self.root/'derived'/'REPORT01.html').exists())
        self.assertTrue((self.root/'sessions'/'S02').is_dir())
        self.assertEqual((v.verify(self.root,self.receiver)['sessions'],v.verify(self.root,self.receiver)['withdrawn']),(1,1))
    def test_withdraw_idempotent(self):
        self.make();self.seal();v.withdraw(self.root,'S01',True)
        x=v.withdraw(self.root,'S01',True)
        self.assertEqual(x['local_session_files_removed'],0)
    def test_withdraw_blocks_reuse(self):
        self.make();self.seal();v.withdraw(self.root,'S01',True)
        self.make();self.error(lambda:self.seal(),'SESSION_EXISTS_OR_WITHDRAWN')
    def test_unknown_withdraw_refused(self):
        self.error(lambda:v.withdraw(self.root,'S07',True),'UNKNOWN_SESSION')
    def test_untracked_file_blocks_withdraw_without_deletion(self):
        p=self.make();self.seal();(p/'FOREIGN.txt').write_text('keep')
        self.error(lambda:v.withdraw(self.root,'S01',True),'FOREIGN_SESSION_FILES')
        self.assertTrue((p/'FOREIGN.txt').exists());self.assertTrue((p/'receipt.json').exists())
        self.error(lambda:v.verify(self.root,self.receiver),'TOMBSTONED_SESSION_STILL_PRESENT')
    def test_vault_not_inside_git_tree(self):
        (self.base/'.git').mkdir();self.error(lambda:v.verify(self.root,self.receiver),'REFUSE_GIT_WORKTREE')
    def test_private_permissions(self):
        if os.name != 'nt':
            os.chmod(self.root,0o755);self.error(lambda:v.verify(self.root,self.receiver),'MODE_0700')
    def test_nonempty_init_refused(self):
        self.error(lambda:v.initialize(self.root),'ROOT_MUST_BE_NEW_OR_EMPTY')
    def test_actual_unchanged_aim_delta_report_lifecycle(self):
        self.both()
        original_delta=Path(__file__).resolve().parent.parent/'P17B'/'aim_delta.py'
        self.assertTrue(original_delta.is_file())
        dest=self.root/'derived'/'REPORT01.html'
        args=[sys.executable,str(original_delta),'--receiver',str(self.receiver),
              '--before',str(self.root/'sessions'/'S01'/'receipt.json'),
              '--before-images',str(self.root/'sessions'/'S01'),
              '--before-game-sha',GAME1,'--before-zip-sha256',ZIP1,
              '--after',str(self.root/'sessions'/'S02'/'receipt.json'),
              '--after-images',str(self.root/'sessions'/'S02'),
              '--after-game-sha',GAME2,'--after-zip-sha256',ZIP2,
              '--output',str(dest),'--confirm-private-embedded-images']
        run=subprocess.run(args,capture_output=True,text=True)
        self.assertEqual(run.returncode,0,run.stderr)
        self.assertIn('data:image/png;base64,',dest.read_text(encoding='utf-8'))
        v.attach_derived(self.root,'REPORT01.html',['S01','S02'])
        self.assertEqual(v.verify(self.root,self.receiver)['files_checked'],11)
        v.withdraw(self.root,'S02',True)
        self.assertFalse(dest.exists());self.assertFalse((self.root/'sessions'/'S02').exists())
        self.assertEqual(v.verify(self.root,self.receiver)['sessions'],1)
    def test_forged_index_traversal_fails_closed(self):
        self.make();self.seal()
        f=self.root/'.aim-vault.json';j=json.loads(f.read_text())
        digest=j['sessions']['S01']['files'].pop('CLOSE_STATIC.png')
        j['sessions']['S01']['files']['../../OUTSIDE.png']=digest
        f.write_text(json.dumps(j))
        self.error(lambda:v.withdraw(self.root,'S01',True),'INVALID_INDEX_FILE_NAME')
        self.assertTrue((self.root/'sessions'/'S01'/'receipt.json').exists())
        self.assertTrue((self.root/'sessions'/'S01'/'CLOSE_STATIC.png').exists())
    def test_interrupted_tombstone_fails_closed_and_can_resume(self):
        self.make();self.seal()
        f=self.root/'.aim-vault.json';j=json.loads(f.read_text());j['withdrawn'].append('S01')
        f.write_text(json.dumps(j))
        self.error(lambda:v.verify(self.root,self.receiver),'TOMBSTONED_SESSION_STILL_PRESENT')
        v.withdraw(self.root,'S01',True)
        self.assertEqual(v.verify(self.root,self.receiver)['withdrawn'],1)
    def test_cli_real_process_succeeds_and_fails_closed(self):
        f=Path(v.__file__).resolve()
        okay=subprocess.run([sys.executable,str(f),'verify','--root',str(self.root),'--receiver',str(self.receiver)],capture_output=True,text=True)
        self.assertEqual(okay.returncode,0,okay.stderr)
        bad=subprocess.run([sys.executable,str(f),'withdraw','--root',str(self.root),'--sid','S01'],capture_output=True,text=True)
        self.assertEqual(bad.returncode,2)
        self.assertIn('REQUIRES_CONFIRM',bad.stderr)

if __name__=='__main__':
    unittest.main(verbosity=2)
