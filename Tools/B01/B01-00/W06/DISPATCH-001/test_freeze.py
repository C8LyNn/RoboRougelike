"""W07 real regression for freeze/ownership state transitions (no Unity needed)."""
from contextlib import redirect_stdout, redirect_stderr
import hashlib
import io
import json
from pathlib import Path
import tempfile
import threading
import unittest

from dispatch import DispatchError, claim, claims_from_dir, freeze, release, main
from test_dispatch import fixture


class FreezeAcceptanceTests(unittest.TestCase):
    def test_freeze_actual_file_and_new_hash(self):
        with tempfile.TemporaryDirectory() as d:
            a = claim(fixture(), d, 'W06-DISPATCH')
            receipt = freeze(d, 'W06-DISPATCH', a['sha256'])
            actual = Path(a['claim']).read_bytes()
            self.assertEqual(receipt['sha256'], hashlib.sha256(actual).hexdigest())
            self.assertNotEqual(receipt['sha256'], a['sha256'])
            self.assertEqual(receipt['previous_sha256'], a['sha256'])
            self.assertEqual(json.loads(actual)['state'], 'FROZEN')

    def test_release_frozen_rejected_even_correct_current_sha(self):
        with tempfile.TemporaryDirectory() as d:
            a = claim(fixture(), d, 'W06-DISPATCH')
            frozen = freeze(d, 'W06-DISPATCH', a['sha256'])
            with self.assertRaisesRegex(DispatchError, 'release forbidden'):
                release(d, 'W06-DISPATCH', frozen['sha256'])
            self.assertTrue(Path(a['claim']).exists())

    def test_release_codex_claimed_rejected_even_correct_current_sha(self):
        with tempfile.TemporaryDirectory() as d:
            a = claim(fixture(), d, 'W06-DISPATCH')
            f = Path(a['claim'])
            data = json.loads(f.read_bytes())
            data['state'] = 'CODEX_CLAIMED'
            raw = (json.dumps(data, sort_keys=True, separators=(',', ':')) + '\n').encode()
            f.write_bytes(raw)  # Simulates external authenticated owner, not a CLI transition.
            with self.assertRaisesRegex(DispatchError, 'release forbidden'):
                release(d, 'W06-DISPATCH', hashlib.sha256(raw).hexdigest())
            self.assertEqual(json.loads(f.read_bytes())['state'], 'CODEX_CLAIMED')

    def test_frozen_claim_prevents_duplicate_ownership(self):
        with tempfile.TemporaryDirectory() as d:
            a = claim(fixture(), d, 'W06-DISPATCH')
            freeze(d, 'W06-DISPATCH', a['sha256'])
            with self.assertRaises(DispatchError):
                claim(fixture(), d, 'W06-DISPATCH')

    def test_freeze_stale_sha_fails_without_mutation(self):
        with tempfile.TemporaryDirectory() as d:
            a = claim(fixture(), d, 'W06-DISPATCH')
            with self.assertRaises(DispatchError):
                freeze(d, 'W06-DISPATCH', '0' * 64)
            self.assertEqual(json.loads(Path(a['claim']).read_bytes())['state'], 'ACTIVE')

    def test_freeze_twice_fails_even_current_hash(self):
        with tempfile.TemporaryDirectory() as d:
            a = claim(fixture(), d, 'W06-DISPATCH')
            f = freeze(d, 'W06-DISPATCH', a['sha256'])
            with self.assertRaisesRegex(DispatchError, 'only ACTIVE'):
                freeze(d, 'W06-DISPATCH', f['sha256'])
            self.assertEqual(json.loads(Path(a['claim']).read_bytes())['state'], 'FROZEN')

    def test_freeze_claimed_fails(self):
        with tempfile.TemporaryDirectory() as d:
            a = claim(fixture(), d, 'W06-DISPATCH')
            f = Path(a['claim'])
            data = json.loads(f.read_text()); data['state'] = 'CODEX_CLAIMED'
            raw = (json.dumps(data, sort_keys=True, separators=(',', ':')) + '\n').encode()
            f.write_bytes(raw)
            with self.assertRaises(DispatchError):
                freeze(d, 'W06-DISPATCH', hashlib.sha256(raw).hexdigest())

    def test_symlink_claim_refused(self):
        with tempfile.TemporaryDirectory() as d:
            a = claim(fixture(), d, 'W06-DISPATCH')
            f = Path(a['claim'])
            original = f.with_name('owned.backup')
            f.rename(original); f.symlink_to(original)
            with self.assertRaisesRegex(DispatchError, 'symlink'):
                freeze(d, 'W06-DISPATCH', a['sha256'])

    def test_corrupted_claim_rejected_with_matching_corruption_hash(self):
        with tempfile.TemporaryDirectory() as d:
            a = claim(fixture(), d, 'W06-DISPATCH')
            f = Path(a['claim'])
            raw = b'{"id":"W06-DISPATCH","state":"ACTIVE"}\n'
            f.write_bytes(raw)
            with self.assertRaises(DispatchError):
                freeze(d, 'W06-DISPATCH', hashlib.sha256(raw).hexdigest())

    def test_claim_identity_tamper_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            a = claim(fixture(), d, 'W06-DISPATCH')
            f = Path(a['claim'])
            data = json.loads(f.read_bytes()); data['id'] = 'OTHER-TASK'
            raw = (json.dumps(data, sort_keys=True, separators=(',', ':')) + '\n').encode()
            f.write_bytes(raw)
            with self.assertRaisesRegex(DispatchError, 'mismatched'):
                freeze(d, 'W06-DISPATCH', hashlib.sha256(raw).hexdigest())

    def test_concurrent_release_freeze_never_releases_frozen(self):
        with tempfile.TemporaryDirectory() as d:
            a = claim(fixture(), d, 'W06-DISPATCH')
            outcome = []
            def do_freeze():
                try: freeze(d, 'W06-DISPATCH', a['sha256']); outcome.append('freeze')
                except DispatchError: outcome.append('blocked-freeze')
            def do_release():
                try: release(d, 'W06-DISPATCH', a['sha256']); outcome.append('release')
                except DispatchError: outcome.append('blocked-release')
            x = threading.Thread(target=do_freeze); y = threading.Thread(target=do_release)
            x.start(); y.start(); x.join(); y.join()
            self.assertEqual(len(outcome), 2)
            self.assertEqual(len([s for s in outcome if not s.startswith('blocked')]), 1)
            f = Path(a['claim'])
            if f.exists():
                self.assertEqual(json.loads(f.read_bytes())['state'], 'FROZEN')

    def test_frozen_one_scope_does_not_block_independent_scope(self):
        with tempfile.TemporaryDirectory() as d:
            a = claim(fixture(), d, 'W06-DISPATCH')
            freeze(d, 'W06-DISPATCH', a['sha256'])
            b = claim(fixture(), d, 'CHL-WORK')
            self.assertTrue(Path(b['claim']).exists())
            self.assertEqual(len(claims_from_dir(d)), 2)

    def test_freeze_cli_receipt_then_release_cli_blocked(self):
        with tempfile.TemporaryDirectory() as d:
            a = claim(fixture(), d, 'W06-DISPATCH')
            out = io.StringIO()
            with redirect_stdout(out):
                code = main(['--snapshot', 'unused.json', '--claims-dir', d, 'freeze', 'W06-DISPATCH',
                             '--expected-sha', a['sha256']])
            self.assertEqual(code, 0)
            receipt = json.loads(out.getvalue())
            err = io.StringIO()
            with redirect_stderr(err):
                code = main(['--snapshot', 'unused.json', '--claims-dir', d, 'release', 'W06-DISPATCH',
                             '--expected-sha', receipt['sha256']])
            self.assertEqual(code, 2)
            self.assertIn('release forbidden', err.getvalue())
            self.assertTrue(Path(a['claim']).exists())

    def test_freeze_does_not_modify_other_files(self):
        with tempfile.TemporaryDirectory() as d:
            a = claim(fixture(), d, 'W06-DISPATCH')
            marker = Path(d) / 'human_notes.txt'; marker.write_text('preserve', encoding='utf-8')
            freeze(d, 'W06-DISPATCH', a['sha256'])
            self.assertEqual(marker.read_text(encoding='utf-8'), 'preserve')
            self.assertFalse((Path(d) / '.dispatch_guard').exists())
            self.assertFalse((Path(d) / '.dispatch_freeze_W06-DISPATCH.tmp').exists())


if __name__ == '__main__':
    unittest.main()
