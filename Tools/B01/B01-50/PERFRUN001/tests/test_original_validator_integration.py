"""Interop with the exact, REAL PERFSCALING-001 validate_live.py source.

No actual Unity Player is invoked. The synthetic process creates format-faithful
120-second sample files; the REAL original parser is executed as a Python process.
Set B0150_ORIGINAL_VALIDATOR to an actual file with the frozen original Git blob.
Absent validator yields explicit SKIP, not a falsely claimed PASS.
"""
from __future__ import annotations

import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import capture

VALIDATOR_ENV = 'B0150_ORIGINAL_VALIDATOR'
VALIDATOR_PATH = os.environ.get(VALIDATOR_ENV)


def fake_complete_120s(c, *, patch=None):
    """Synthetic, never to be confused with a Player/PhysX performance result."""
    out = c['run_dir'] / 'capture'
    out.mkdir(parents=False, exist_ok=False)
    meta = {
        'classification': capture.CAPTURE_CLASS,
        'game_sha': c['game_sha'],
        'build_zip_sha256': c['build_sha256'],
        'scenario_id': c['scenario'],
        'unity_version': '6000.3.14f1',
        'capture_complete': True,
        'observed_capture_seconds': 120.04,
        'profiler_main_render_gpu_gc': 'NOT_MEASURED',
        'frame_rows': 7200,
        'object_scans': 120,
    }
    if patch:
        meta.update(patch)
    (out / 'receipt.json').write_text(json.dumps(meta) + '\n', encoding='utf-8')
    with (out / 'frames.csv').open('w', encoding='utf-8', newline='') as f:
        f.write('frame_index,elapsed_ms,delta_ms\n')
        for i in range(7200):
            f.write(f'{i},{i*16},{16}\n')
    with (out / 'objects.csv').open('w', encoding='utf-8', newline='') as f:
        f.write('elapsed_ms,scene,active_parts,active_projectiles,active_enemies,rigidbodies,scan_ms\n')
        for i in range(120):
            f.write(f'{i*1000},Arena_A,24,2,8,31,0.1\n')
    return out


class PseudoProcess:
    """Unit-test fake: no Player process is ever created."""
    def wait(self, timeout):
        return 0
    def terminate(self):
        raise AssertionError('fake process must not be terminated')
    def kill(self):
        raise AssertionError('fake process must not be killed')


@unittest.skipUnless(VALIDATOR_PATH, f'REAL pinned validator path required via {VALIDATOR_ENV}')
class OriginalValidatorInteroperability(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.validator = Path(VALIDATOR_PATH).resolve()
        if not cls.validator.is_file():
            raise RuntimeError('original pinned validator missing')
        actual = capture.git_blob_sha(cls.validator)
        if actual != capture.RECORDER_VALIDATOR_GIT_BLOB:
            raise RuntimeError(f'original validator Git blob mismatch, got {actual}')

    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        root = Path(self.tmp.name)
        self.c = {
            'player': root / 'NOT_EXECUTED_Player.exe',
            'run_dir': root / 'synthetic-run',
            'validator': self.validator,
            'game_sha': 'a' * 40,
            'build_sha256': 'b' * 64,
            'scenario': 'synthetic-test',
            'player_sha256': 'c' * 64,
            'player_zip_member': 'synthetic/NOT_EXECUTED_Player.exe',
            'validator_git_blob': capture.RECORDER_VALIDATOR_GIT_BLOB,
            'run_seconds': 165,
            'distribution_files_verified': 4,
            'distribution_sha256': 'd' * 64,
        }

    def _run(self, *, patch=None, mutate=None):
        def runner(argv, **kwargs):
            folder = fake_complete_120s(self.c, patch=patch)
            if mutate:
                mutate(folder)
            return PseudoProcess()
        return capture.run_and_validate(self.c, runner=runner)

    def test_real_original_receiver_accepts_complete_synthetic_raw_trace_not_native(self):
        result = self._run()
        self.assertEqual(result['classification'], capture.RESULT_CLASS)
        self.assertEqual(result['validator_returncode'], 0)
        self.assertFalse(result['native_performance_acceptance'])
        self.assertEqual(result['render_gpu_gc_fps'], 'NOT_MEASURED')
        validation = json.loads((self.c['run_dir'] / 'validation.json').read_text())
        self.assertEqual(validation['status'], 'LIVE_RAW_REVIEW_REQUIRED_NOT_PERFORMANCE_ACCEPTANCE')
        self.assertEqual(validation['frame_rows'], 7200)
        self.assertEqual(validation['object_scans'], 120)
        for filename in ('receipt.json', 'frames.csv', 'objects.csv'):
            original = self.c['run_dir'] / 'capture' / filename
            digest = hashlib.sha256(original.read_bytes()).hexdigest()
            self.assertEqual(result['files_sha256'][filename], digest)
            self.assertEqual(validation['raw_sha256'][filename], digest)

    def test_real_original_receiver_rejects_invalid_unity_version(self):
        result = self._run(patch={'unity_version': '0.0.0-test'})
        self.assertEqual(result['classification'], capture.ERROR_CLASS)
        self.assertNotEqual(result['validator_returncode'], 0)
        self.assertIn('validator', result['error'])

    def test_real_original_receiver_rejects_sparse_object_scans(self):
        def truncate(folder):
            p = folder / 'objects.csv'
            lines = p.read_text().splitlines()
            p.write_text('\n'.join(lines[:21]) + '\n')
        result = self._run(mutate=truncate)
        self.assertEqual(result['classification'], capture.ERROR_CLASS)
        self.assertNotEqual(result['validator_returncode'], 0)

    def test_real_original_receiver_rejects_bad_csv_header(self):
        def corrupt(folder):
            p = folder / 'frames.csv'
            content = p.read_text()
            p.write_text(content.replace('frame_index,elapsed_ms,delta_ms', 'wrong,elapsed_ms,delta_ms', 1))
        result = self._run(mutate=corrupt)
        self.assertEqual(result['classification'], capture.ERROR_CLASS)
        self.assertNotEqual(result['validator_returncode'], 0)


if __name__ == '__main__':
    unittest.main()
