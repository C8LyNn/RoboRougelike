#!/usr/bin/env python3
"""W32 Windows Player ZIP canonicality: nine source-only RED-to-GREEN cases.
No Unity, actual Player or human participants are executed by this test.
"""
import hashlib
import json
import stat
import sys
import tempfile
import unittest
import warnings
import zipfile
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent.parent/'P17W28'))
import playtest_intake as target

GAME_SHA='a'*40
GOOD_EXE=b'FIRST_EXE_GOOD'
def sha(b):return hashlib.sha256(b).hexdigest()

class CanonicalZipTests(unittest.TestCase):
    def setUp(self):
        temp=tempfile.TemporaryDirectory();self.addCleanup(temp.cleanup)
        self.root=Path(temp.name)
        self.archive=self.root/'game.zip'
        self.log=self.root/'Player.log'
        self.log.write_bytes(b'synthetic log only')

    def write_player(self, extra=()):
        with warnings.catch_warnings():
            warnings.simplefilter('ignore',UserWarning)
            with zipfile.ZipFile(self.archive,'w') as z:
                z.writestr('RoboRougelike.exe',GOOD_EXE)
                z.writestr('UnityPlayer.dll',b'dll')
                z.writestr('RoboRougelike_Data/boot.config',b'cfg')
                for name, data in extra:z.writestr(name,data)

    def reject(self, code):
        with self.assertRaisesRegex(target.Refused,code):target.inspect_unity_zip(self.archive)

    def test_exact_duplicate_exe_cannot_crosscheck_first_when_extracted_second(self):
        self.write_player([('RoboRougelike.exe',b'SECOND_EXE_BAD')])
        with zipfile.ZipFile(self.archive) as z:
            self.assertEqual(z.read('RoboRougelike.exe'),b'SECOND_EXE_BAD')
        receipt=self.root/'receipt.json'
        receipt.write_text(json.dumps({
            'schema':'b01-13-w30-windows-player-session-r1',
            'gameCommit':GAME_SHA,'gameCommitProvenance':'OPERATOR_ASSERTED_NOT_EMBEDDED_IN_PLAYER',
            'processExitCode':0,'captureStatus':'PROCESS_EXITED_LOG_PRESENT',
            'exeSha256':sha(GOOD_EXE),'exeBytes':len(GOOD_EXE),
            'playerLog':{'sha256':sha(self.log.read_bytes()),'bytes':self.log.stat().st_size},
            'utcStart':'2026-11-03T12:00:00+00:00','utcEnd':'2026-11-03T12:01:00+00:00',
            'unityNativeG1':'NOT_VERIFIED','actualGameplayPassed':'NOT_VERIFIED',
            'humanSessionsIndependentlyVerified':0,'playerLaunchG1':'NOT_INDEPENDENTLY_VERIFIED'}))
        self.reject('ZIP_DUPLICATE_MEMBER')
        with self.assertRaisesRegex(target.Refused,'ZIP_DUPLICATE_MEMBER'):
            target.verify_w30_session(receipt,self.archive,self.log,GAME_SHA)

    def test_casefold_duplicate_dll(self):
        self.write_player([('unityplayer.dll',b'other')]);self.reject('ZIP_DUPLICATE_MEMBER')

    def test_directory_file_collision(self):
        self.write_player([('Extra/',b''),('extra',b'shadow')]);self.reject('ZIP_DUPLICATE_MEMBER')

    def test_unix_symlink_exe(self):
        x=zipfile.ZipInfo('RoboRougelike.exe');x.create_system=3
        x.external_attr=(stat.S_IFLNK|0o777)<<16
        with zipfile.ZipFile(self.archive,'w') as z:
            z.writestr(x,'other.exe')
            z.writestr('UnityPlayer.dll',b'dll')
            z.writestr('RoboRougelike_Data/boot.config',b'cfg')
        self.reject('ZIP_SYMLINK_MEMBER')

    def test_ntfs_alternate_stream_name(self):
        self.write_player([('data:stream',b'x')]);self.reject('ZIP_MEMBER_PATH_UNSAFE')

    def test_windows_trailing_dot_segment(self):
        self.write_player([('cache./file.tmp',b'x')]);self.reject('ZIP_MEMBER_PATH_UNSAFE')

    def test_windows_reserved_device_name(self):
        self.write_player([('AUX.txt',b'x')]);self.reject('ZIP_MEMBER_PATH_UNSAFE')

    def test_legitimate_unity_crash_handler_exe_allowed(self):
        self.write_player([('UnityCrashHandler64.exe',b'handler')])
        self.assertEqual(target.inspect_unity_zip(self.archive),'ARCHIVE_STRUCTURE_PRESENT_NOT_BOOT_PROOF')

    def test_valid_nested_build_archive_allowed(self):
        with zipfile.ZipFile(self.archive,'w') as z:
            z.writestr('build/RoboRougelike.exe',GOOD_EXE)
            z.writestr('build/UnityPlayer.dll',b'dll')
            z.writestr('build/RoboRougelike_Data/boot.config',b'cfg')
        self.assertEqual(target.inspect_unity_zip(self.archive),'ARCHIVE_STRUCTURE_PRESENT_NOT_BOOT_PROOF')

if __name__=='__main__':unittest.main()
