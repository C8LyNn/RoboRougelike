import json
from pathlib import Path
import stat
import sys
import tempfile
import unittest
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import zipreadbench as m

SOURCE = Path(__file__).resolve().parents[1] / 'original_playerfootprint.py'


class Tests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.base = Path(self.tmp.name)
        self.zip = self.base / 'bundle.zip'
        self.out = self.base / 'receipt.json'
        self.write()

    def write(self, extras=None):
        files = {'RoboRougelike.exe': b'MZtest', 'UnityPlayer.dll': b'MZtest',
                 'RoboRougelike_Data/globalgamemanagers': b'live-not-executed',
                 'RoboRougelike_Data/resources.assets': b'Z'*1024}
        files.update(extras or {})
        with zipfile.ZipFile(self.zip, 'w', compression=zipfile.ZIP_DEFLATED) as z:
            for k, v in files.items(): z.writestr(k, v)

    def test_real_frozen_blob(self):
        self.assertEqual(m.file_hash(SOURCE, git=True), m.SOURCE_BLOB)

    def test_rounds_and_quantiles(self):
        x = m.bench(self.zip, SOURCE, m.file_hash(self.zip), 3)
        self.assertEqual(x['files'], 4)
        self.assertEqual(len(x['rounds']), 3)
        self.assertEqual(len(x['per_file']), 4)
        self.assertEqual(x['uncompressed_bytes'], sum(e['bytes'] for e in x['per_file']))
        self.assertTrue(all(e['median_read_ns'] >= 0 for e in x['per_file']))
        self.assertIn('NOT_UNITY_RUNTIME', x['provenance'])

    def test_refuse_wrong_sha_no_receipt(self):
        self.assertEqual(m.main(['--archive', str(self.zip), '--frozen-source', str(SOURCE),
                                 '--expected-sha256', '0'*64, '--out', str(self.out)]), 1)
        self.assertFalse(self.out.exists())

    def test_refuse_tampered_frozen_validator(self):
        p = self.base/'fake.py'
        p.write_bytes(SOURCE.read_bytes()+b'\n# tampered')
        with self.assertRaisesRegex(ValueError, 'source blob mismatch'):
            m.bench(self.zip, p, m.file_hash(self.zip), 2)

    def test_refuse_bad_repeat_count(self):
        for value in (0, 1, 9, 100):
            with self.assertRaisesRegex(ValueError, 'repeats'):
                m.bench(self.zip, SOURCE, m.file_hash(self.zip), value)

    def test_refuse_malicious_zip_slip(self):
        self.write({'../evil': b'bad'})
        with self.assertRaises(ValueError): m.bench(self.zip, SOURCE, m.file_hash(self.zip), 2)

    def test_refuse_same_case_collision(self):
        self.write({'unityplayer.DLL': b'MZother'})
        with self.assertRaises(ValueError): m.bench(self.zip, SOURCE, m.file_hash(self.zip), 2)

    def test_refuse_symlink_archive(self):
        p = self.base/'linked.zip'
        p.symlink_to(self.zip)
        with self.assertRaisesRegex(ValueError, 'non-symlink'):
            m.bench(p, SOURCE, m.file_hash(self.zip), 2)

    def test_output_exclusive(self):
        args = ['--archive', str(self.zip), '--frozen-source', str(SOURCE),
                '--expected-sha256', m.file_hash(self.zip), '--out', str(self.out), '--repeats', '2']
        self.assertEqual(m.main(args), 0)
        before = self.out.read_bytes()
        self.assertEqual(m.main(args), 1)
        self.assertEqual(self.out.read_bytes(), before)

    def test_refuse_corrupt_bundle(self):
        self.zip.write_bytes(b'not a ZIP')
        self.assertEqual(m.main(['--archive', str(self.zip), '--frozen-source', str(SOURCE),
                                 '--expected-sha256', m.file_hash(self.zip), '--out', str(self.out)]), 1)
        self.assertFalse(self.out.exists())

    def test_percentile_definition(self):
        self.assertEqual(m.percentile([10, 20, 30, 40, 50], .95), 50)
        self.assertEqual(m.median([2, 4, 6, 8]), 5)


if __name__ == '__main__': unittest.main()
