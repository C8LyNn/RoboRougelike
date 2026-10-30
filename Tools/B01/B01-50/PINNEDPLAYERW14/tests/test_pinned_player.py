"""Real original frozen source tests, never Unity/PhysX simulation."""
import json
from pathlib import Path
import sys
import tempfile
import unittest
import zipfile

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from verify_pinned_player import verify, FROZEN_GIT_BLOB, sha256, git_blob

# GitHub CI fetches the unchanged original source at its exact commit into this parent folder.
ORIGINAL = Path(__file__).resolve().parents[1] / 'original_playerfootprint.py'

class IntakeTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.d = Path(self.tmp.name)
        self.archive=self.d/'player.zip'
        self.out=self.d/'report.json'
        self.build()

    def build(self, extra=None):
        files={'RoboRougelike/RoboRougelike.exe':b'MZ'+b'X'*20,
               'RoboRougelike/UnityPlayer.dll':b'MZ'+b'Y'*10,
               'RoboRougelike/RoboRougelike_Data/globalgamemanagers':b'REAL_FIXTURE_FAKE',
               'RoboRougelike/RoboRougelike_Data/resources.assets':b'SOME_DATA'}
        files.update(extra or {})
        with zipfile.ZipFile(self.archive,'w') as z:
            for name,data in files.items():z.writestr(name,data)

    def test_exact_original_git_blob(self):
        self.assertEqual(git_blob(ORIGINAL),FROZEN_GIT_BLOB)

    def test_valid_fixture_provenance(self):
        r=verify(ORIGINAL,self.archive,sha256(self.archive),self.out)
        self.assertEqual(r['zip_non_directory_entries'],4)
        self.assertFalse(r['native_unity_editor_executed'])
        self.assertFalse(r['runtime_fps_or_memory_measured'])
        self.assertEqual(r['source_git_blob'],FROZEN_GIT_BLOB)

    def test_wrong_expected_sha_refuses_without_output(self):
        with self.assertRaisesRegex(ValueError,'archive SHA mismatch'):
            verify(ORIGINAL,self.archive,'0'*64,self.out)
        self.assertFalse(self.out.exists())

    def test_source_mutation_is_rejected(self):
        wrong=self.d/'tampered.py'
        wrong.write_bytes(ORIGINAL.read_bytes()+b'\n# changed\n')
        with self.assertRaisesRegex(ValueError,'source drift'):
            verify(wrong,self.archive,sha256(self.archive),self.out)
        self.assertFalse(self.out.exists())

    def test_zip_slip_is_rejected(self):
        self.build({'../outside':b'evil'})
        with self.assertRaisesRegex(ValueError,'unsafe ZIP path'):
            verify(ORIGINAL,self.archive,sha256(self.archive),self.out)
        self.assertFalse(self.out.exists())

    def test_case_collision_is_rejected(self):
        self.build({'RoboRougelike/UNITYPLAYER.DLL':b'MZevil'})
        with self.assertRaisesRegex(ValueError,'case-colliding'):
            verify(ORIGINAL,self.archive,sha256(self.archive),self.out)
        self.assertFalse(self.out.exists())

    def test_double_run_cannot_overwrite(self):
        verify(ORIGINAL,self.archive,sha256(self.archive),self.out)
        before=self.out.read_bytes()
        with self.assertRaisesRegex(ValueError,'overwrite'):
            verify(ORIGINAL,self.archive,sha256(self.archive),self.out)
        self.assertEqual(before,self.out.read_bytes())

    def test_symlinked_archive_refused(self):
        l=self.d/'link.zip';l.symlink_to(self.archive)
        with self.assertRaisesRegex(ValueError,'symlinked'):
            verify(ORIGINAL,l,sha256(self.archive),self.out)

    def test_corrupt_archive_rejected_no_output(self):
        self.archive.write_bytes(self.archive.read_bytes()[:-25])
        with self.assertRaises(zipfile.BadZipFile):
            verify(ORIGINAL,self.archive,sha256(self.archive),self.out)
        self.assertFalse(self.out.exists())

    def test_output_is_original_sha_pinned(self):
        r=verify(ORIGINAL,self.archive,sha256(self.archive),self.out)
        self.assertEqual(len(r['original_full_json_sha256']),64)
        self.assertEqual(r['archive_sha256'],sha256(self.archive))

if __name__=='__main__': unittest.main()
