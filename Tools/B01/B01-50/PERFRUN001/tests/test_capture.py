"""Synthetic process fixtures for original operator logic; never native Unity acceptance."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import zipfile
from unittest import mock

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import capture

VALIDATOR_MOCK = '''#!/usr/bin/env python3
import argparse,json
from pathlib import Path
p=argparse.ArgumentParser()
for name in ('folder','game-sha','build-sha','scenario','out'):p.add_argument('--'+name,required=True)
a=p.parse_args()
f=Path(a.folder)
if not all((f/x).is_file() for x in ('frames.csv','objects.csv','receipt.json')):raise SystemExit(7)
Path(a.out).write_text(json.dumps({'status':'LIVE_RAW_REVIEW_REQUIRED_NOT_PERFORMANCE_ACCEPTANCE'}))
'''

class DummyProcess:
    def __init__(self,argv,*,exit=0,complete=True,create=True):
        self.argv=argv;self.exit=exit
        if create:
            folder=Path(argv[argv.index('-b0150-live-output')+1]);folder.mkdir(parents=True,exist_ok=False)
            def flag(s):return argv[argv.index(s)+1]
            data={'classification':capture.CAPTURE_CLASS,'game_sha':flag('-b0150-live-game-sha'),
                'build_zip_sha256':flag('-b0150-live-build-sha'),'scenario_id':flag('-b0150-live-scenario'),
                'capture_complete':complete}
            (folder/'receipt.json').write_text(json.dumps(data))
            (folder/'frames.csv').write_text('frame_index,elapsed_ms,delta_ms\n0,1,16\n')
            (folder/'objects.csv').write_text('elapsed_ms,scene,active_parts,active_projectiles,active_enemies,rigidbodies,scan_ms\n1,test,0,0,0,0,0\n')
    def wait(self,timeout):return self.exit
    def terminate(self):pass
    def kill(self):pass

class OperatorTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        self.player=self.root/'Player.exe';self.player.write_bytes(b'MOCK PLAYER, NEVER UNITY')
        self.zip=self.root/'build.zip'
        self.dll=self.root/'UnityPlayer.dll';self.dll.write_bytes(b'ORIGINAL UNITY DLL')
        self.data=self.root/'Player_Data';self.data.mkdir()
        (self.data/'boot.config').write_bytes(b'original boot config')
        (self.data/'globalgamemanagers').write_bytes(b'original managers')
        with zipfile.ZipFile(self.zip,'w') as z:
            z.write(self.player,arcname='RoboRougelike/Player.exe')
            z.write(self.dll,arcname='RoboRougelike/UnityPlayer.dll')
            for name in ('boot.config','globalgamemanagers'):
                z.write(self.data/name,arcname='RoboRougelike/Player_Data/'+name)
        self.validator=self.root/'validate_live.py';self.validator.write_text(VALIDATOR_MOCK)
        self.output=self.root/'output';self.output.mkdir()
        self.args=self.options()
        self.patch=mock.patch.object(capture,'RECORDER_VALIDATOR_GIT_BLOB',capture.git_blob_sha(self.validator))
        self.patch.start();self.addCleanup(self.patch.stop)
    def options(self,**changes):
        d=dict(player=str(self.player),build_zip=str(self.zip),validator=str(self.validator),game_sha='a'*40,
               scenario='12gun-map',out_parent=str(self.output),run_seconds=155)
        d.update(changes)
        return type('Options',(),d)()
    def config(self,**updates):return capture.validated_inputs(self.options(**updates))
    def test_01_real_file_hashes_correct(self):
        c=self.config();self.assertEqual(c['build_sha256'],hashlib.sha256(self.zip.read_bytes()).hexdigest())
        self.assertEqual(c['player_sha256'],hashlib.sha256(self.player.read_bytes()).hexdigest())
    def test_02_exact_command_switches(self):
        c=self.config();cmd=capture.player_argv(c)
        self.assertEqual(cmd[0],str(self.player));self.assertIn('-b0150-live-output',cmd)
        self.assertEqual(cmd[cmd.index('-b0150-live-scenario')+1],'12gun-map')
    def test_03_unsafe_scenario_rejects(self):
        for scenario in ('../evil','a b','CON','con','bad.txt','é','a'*49):
            with self.subTest(scenario=scenario),self.assertRaises(capture.IntakeError):self.config(scenario=scenario)
    def test_04_bad_sha_rejects(self):
        for sha in ('a'*39,'A'*40,'z'*40,'0'*41):
            with self.subTest(sha=sha),self.assertRaises(capture.IntakeError):self.config(game_sha=sha)
    def test_05_wrong_validator_git_blob_rejects(self):
        with mock.patch.object(capture,'RECORDER_VALIDATOR_GIT_BLOB','1'*40):
            with self.assertRaisesRegex(capture.IntakeError,'validator differs'):self.config()
    def test_06_runtime_too_short_rejects(self):
        with self.assertRaises(capture.IntakeError):self.config(run_seconds=149)
    def test_07_missing_player_rejects(self):
        with self.assertRaises(capture.IntakeError):self.config(player=str(self.root/'NOTFOUND.exe'))
    def test_08_duplicate_run_folder_rejects(self):
        (self.output/'12gun-map').mkdir()
        with self.assertRaisesRegex(capture.IntakeError,'overwrite'):self.config()
    def test_09_dry_run_creates_no_evidence(self):
        args=['--player',str(self.player),'--build-zip',str(self.zip),'--validator',str(self.validator),
              '--game-sha','a'*40,'--scenario','12gun-map','--out-parent',str(self.output),'--dry-run']
        self.assertEqual(capture.main(args),0)
        self.assertFalse((self.output/'12gun-map').exists())
    def test_10_fake_process_no_receipt_returns_failed(self):
        c=self.config();res=capture.run_and_validate(c,runner=lambda argv,**kwargs:DummyProcess(argv,create=False))
        self.assertEqual(res['classification'],capture.ERROR_CLASS)
        self.assertFalse(res['native_performance_acceptance'])
        self.assertIn('receipt.json',res['error'])
    def test_11_fake_process_nonzero_rejects_even_complete(self):
        c=self.config();res=capture.run_and_validate(c,runner=lambda argv,**kwargs:DummyProcess(argv,exit=123))
        self.assertEqual(res['classification'],capture.ERROR_CLASS)
        self.assertIn('nonzero',res['error'])
    def test_12_fake_incomplete_capture_rejects(self):
        c=self.config();res=capture.run_and_validate(c,runner=lambda argv,**kwargs:DummyProcess(argv,complete=False))
        self.assertEqual(res['classification'],capture.ERROR_CLASS)
        self.assertIn('incomplete',res['error'])
    def test_13_fake_capture_with_mock_validator_still_not_native_pass(self):
        c=self.config();res=capture.run_and_validate(c,runner=lambda argv,**kwargs:DummyProcess(argv))
        self.assertEqual(res['classification'],capture.RESULT_CLASS)
        self.assertFalse(res['native_performance_acceptance'])
        self.assertEqual(res['validator_returncode'],0)
        self.assertEqual(len(res['files_sha256']),3)
        self.assertTrue((c['run_dir']/'operator_receipt.json').is_file())
        self.assertEqual((c['run_dir']/'operator_receipt.json').read_text().count('NOT_MEASURED'),1)
    def test_14_reexecution_refuses_overwrite(self):
        c=self.config();capture.run_and_validate(c,runner=lambda argv,**kwargs:DummyProcess(argv))
        with self.assertRaisesRegex(capture.IntakeError,'overwrite'):self.config()
    def test_15_validator_negative_result_not_promoted(self):
        c=self.config()
        fake_verify=lambda *a,**kw:subprocess.CompletedProcess(a,12)
        res=capture.run_and_validate(c,runner=lambda argv,**kwargs:DummyProcess(argv),verifier=fake_verify)
        self.assertEqual(res['classification'],capture.ERROR_CLASS)
        self.assertEqual(res['validator_returncode'],12)
    def test_16_mismatched_binary_and_build_zip_rejected(self):
        self.player.write_bytes(b'DIFFERENT GAME BINARY')
        with self.assertRaisesRegex(capture.IntakeError,'EXACTLY ONE'):self.config()
    def test_17_unreadable_zip_rejected(self):
        self.zip.write_bytes(b'MALFORMED ZIP')
        with self.assertRaisesRegex(capture.IntakeError,'readable original'):self.config()
    def test_18_original_zip_member_verified(self):
        self.assertEqual(self.config()['player_zip_member'],'RoboRougelike/Player.exe')
    def test_19_duplicate_matching_binary_in_zip_rejected(self):
        with zipfile.ZipFile(self.zip,'a') as z:z.write(self.player,arcname='duplicate/Player.exe')
        with self.assertRaisesRegex(capture.IntakeError,'EXACTLY ONE'):self.config()
    def test_20_no_external_network_dependencies(self):
        src=(Path(__file__).resolve().parents[1]/'capture.py').read_text()
        for banned in ('urllib.request','requests.','http.client','socket.'):
            self.assertNotIn(banned,src)


    def test_21_mixed_build_modified_unityplayer_dll_rejected(self):
        self.dll.write_bytes(b'WRONG VERSION OF UNITYPLAYER DLL')
        with self.assertRaisesRegex(capture.IntakeError,'distribution|mismatch'):
            self.config()
    def test_22_missing_extracted_data_asset_rejected(self):
        (self.data/'globalgamemanagers').unlink()
        with self.assertRaisesRegex(capture.IntakeError,'distribution|missing'):
            self.config()
    def test_23_malicious_zip_member_path_rejected(self):
        with zipfile.ZipFile(self.zip,'a') as z:
            z.writestr('RoboRougelike/../../escape.bin',b'evil')
        with self.assertRaisesRegex(capture.IntakeError,'unsafe|traversal'):
            self.config()
    def test_24_malformed_list_receipt_fails_closed_and_preserves_operator_receipt(self):
        c=self.config()
        def runner(argv,**kwargs):
            proc=DummyProcess(argv)
            folder=Path(argv[argv.index('-b0150-live-output')+1])
            (folder/'receipt.json').write_text('[]')
            return proc
        result=capture.run_and_validate(c,runner=runner)
        self.assertEqual(result['classification'],capture.ERROR_CLASS)
        self.assertFalse(result['native_performance_acceptance'])
        self.assertIn('error',result)
        self.assertTrue((c['run_dir']/'operator_receipt.json').is_file())
    def test_25_rogue_extra_zip_root_rejected(self):
        with zipfile.ZipFile(self.zip,'a') as z:
            z.writestr('WRONG_ROOT/Resources/game.json',b'mixed archive')
        with self.assertRaisesRegex(capture.IntakeError,'root|distribution'):
            self.config()

if __name__=='__main__':unittest.main()
