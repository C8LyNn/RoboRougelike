import hashlib
import json
import os
from pathlib import Path
import tempfile
import unittest
import sys
sys.path.insert(0,str(Path(__file__).parent))
import native_session as s

COMMIT='a'*40
class Tests(unittest.TestCase):
    def setUp(self):
        tmp=tempfile.TemporaryDirectory();self.addCleanup(tmp.cleanup)
        self.root=Path(tmp.name)
        self.game=self.root/'game';self.game.mkdir()
        self.exe=self.game/'RoboRougelike.exe';self.exe.write_bytes(b'SYNTHETIC_BINARY_NOT_A_UNITY_PLAYER')
        (self.game/'UnityPlayer.dll').write_bytes(b'SYNTHETIC_DLL')
        (self.game/'RoboRougelike_Data').mkdir()
        (self.game/'RoboRougelike_Data'/'boot.config').write_bytes(b'SYNTHETIC_CONFIGURATION')
        self.out=self.root/'isolated';self.out.mkdir()
        self.hash=hashlib.sha256(self.exe.read_bytes()).hexdigest()
    def check(self):return s.preflight(self.exe,self.out,COMMIT,self.hash)
    def refuse(self,fn,code):
        with self.assertRaisesRegex(s.Refused,code):fn()
    def test_structure_accepted_is_not_gameplay(self):self.assertEqual(self.check()['sha256'],self.hash)
    def test_bad_sha_fails(self):self.refuse(lambda:s.preflight(self.exe,self.out,COMMIT,'f'*64),'EXE_HASH_MISMATCH')
    def test_non_sha_format_fails(self):self.refuse(lambda:s.preflight(self.exe,self.out,COMMIT,'zzz'),'EXE_HASH_FORMAT')
    def test_game_commit_format(self):self.refuse(lambda:s.preflight(self.exe,self.out,'broken',self.hash),'GAME_COMMIT_FORMAT')
    def test_missing_real_dll_fails(self):
        (self.game/'UnityPlayer.dll').unlink();self.refuse(self.check,'UNITYPLAYER_DLL')
    def test_missing_player_data_fails(self):
        (self.game/'RoboRougelike_Data'/'boot.config').unlink();self.refuse(self.check,'PLAYER_DATA_FILES')
    def test_symlink_player_rejected(self):
        link=self.game/'other.exe'
        try:link.symlink_to(self.exe)
        except OSError:self.skipTest('No symlink permissions')
        self.refuse(lambda:s.preflight(link,self.out,COMMIT,self.hash),'SYMLINK_PLAYER_EXE')
    def test_output_inside_game_rejected(self):
        p=self.game/'reports';p.mkdir()
        self.refuse(lambda:s.preflight(self.exe,p,COMMIT,self.hash),'OUTPUT_MUST_BE_SEPARATE')
    def test_output_in_git_worktree_rejected(self):
        (self.root/'.git').mkdir();self.refuse(self.check,'OUTPUT_IN_GIT_WORKTREE')
    def test_existing_log_preserved(self):
        (self.out/'W30_Player.log').write_bytes(b'FOREIGN')
        self.refuse(self.check,'REFUSE_EXISTING_OUTPUT')
        self.assertEqual((self.out/'W30_Player.log').read_bytes(),b'FOREIGN')
    def test_without_explicit_two_acks_refused(self):
        self.refuse(lambda:s.capture(self.exe,self.out,COMMIT,self.hash,interactive=True,save_risk=False,platform='nt'),'MANUAL_ACK_BOTH')
    def test_non_windows_run_refused(self):
        self.refuse(lambda:s.capture(self.exe,self.out,COMMIT,self.hash,interactive=True,save_risk=True,platform='posix'),'REAL_WINDOWS_REQUIRED')
    def test_interactive_process_with_log_operator_only_not_unity_pass(self):
        called=[]
        class Result:returncode=0
        def mock(args,**kwargs):
            called.append((args,kwargs));Path(args[2]).write_text('SYNTHETIC LOG, NOT UNITY')
            return Result()
        r=s.capture(self.exe,self.out,COMMIT,self.hash,interactive=True,save_risk=True,platform='nt',runner=mock)
        self.assertEqual(called[0][0][1],'-logFile')
        self.assertEqual(r['captureStatus'],'PROCESS_EXITED_LOG_PRESENT')
        self.assertEqual(r['unityNativeG1'],'NOT_VERIFIED')
        self.assertEqual(r['actualGameplayPassed'],'NOT_VERIFIED')
        self.assertEqual(r['humanSessionsIndependentlyVerified'],0)
        self.assertEqual(json.loads((self.out/'W30_SESSION_RECEIPT.json').read_text())['playerLog']['bytes'],24)
    def test_exit_failure_preserved_not_accepted(self):
        class Result:returncode=11
        def mock(args,**kwargs):
            Path(args[2]).write_text('SYNTHETIC LOG')
            return Result()
        r=s.capture(self.exe,self.out,COMMIT,self.hash,interactive=True,save_risk=True,platform='nt',runner=mock)
        self.assertEqual(r['processExitCode'],11)
        self.assertEqual(r['actualGameplayPassed'],'NOT_VERIFIED')
    def test_missing_log_no_success(self):
        class Result:returncode=0
        r=s.capture(self.exe,self.out,COMMIT,self.hash,interactive=True,save_risk=True,platform='nt',runner=lambda *a,**kw:Result())
        self.assertEqual(r['captureStatus'],'PROCESS_EXITED_LOG_MISSING')
        self.assertIsNone(r['playerLog'])
    def test_relaunch_cannot_overwrite_prior_receipt(self):
        class Result:returncode=0
        s.capture(self.exe,self.out,COMMIT,self.hash,interactive=True,save_risk=True,platform='nt',runner=lambda *a,**kw:Result())
        orig=(self.out/'W30_SESSION_RECEIPT.json').read_bytes()
        self.refuse(self.check,'REFUSE_EXISTING_OUTPUT')
        self.assertEqual(orig,(self.out/'W30_SESSION_RECEIPT.json').read_bytes())
    def test_check_only_does_not_run_or_write(self):
        rc=s.main(['--exe',str(self.exe),'--output-dir',str(self.out),'--game-commit',COMMIT,'--expected-exe-sha256',self.hash,'--check-only'])
        self.assertEqual(rc,0)
        self.assertEqual(list(self.out.iterdir()),[])

if __name__=='__main__':unittest.main(verbosity=2)
