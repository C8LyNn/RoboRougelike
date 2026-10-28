"""Original-tool CLI integration tests. All generated PresentMon rows are synthetic QA controls.

No Player, Unity or genuine profiling is executed here. Uses subprocess and temp
files to exercise the *actual published command-line entrypoints* on this OS.
Portable across Windows and Linux (no shell, shell-specific paths or dependencies).
"""
import csv
import io
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

from test_receiver import DUMMY_META, create_raw_ms

ROOT = Path(__file__).resolve().parent


def invoke(program, *args):
    return subprocess.run(
        [sys.executable, str(ROOT / program), *map(str, args)],
        capture_output=True, text=True, check=False, timeout=30,
        cwd=str(ROOT),
    )


class RawToFinalJsonCLITests(unittest.TestCase):
    def execute(self, durations, mutate=None):
        with tempfile.TemporaryDirectory(prefix='b0150-pass15-') as directory:
            root = Path(directory)
            raw = create_raw_ms(durations)
            if mutate:
                raw = mutate(raw)
            raw_file = root / 'original_raw.csv'
            raw_file.write_bytes(raw)
            norm = root / 'normalized.csv'
            parsed = root / 'inspection.json'
            meta_file = root / 'source_meta.json'
            meta_file.write_text(json.dumps(DUMMY_META), encoding='utf8')
            bridge = invoke('presentmon_bridge.py', '--raw', raw_file, '--scenario', 'G02', '--run', 'R01', '--out', norm)
            if bridge.returncode:
                return {'bridge': bridge, 'normalized_exists': norm.exists(), 'raw': raw}
            analyzer = invoke('analyze_frametimes.py', '--csv', norm, '--metadata', meta_file, '--output', parsed)
            return {
                'bridge': bridge, 'analyzer': analyzer,
                'output': json.loads(parsed.read_text()) if parsed.exists() else None,
                'receipt': json.loads(norm.with_suffix('.receipt.json').read_text()),
                'normalized_header': list(csv.reader(io.StringIO(norm.read_text())))[0],
                'raw': raw,
            }

    def test_119_s_is_rejected_no_normalized_artifact(self):
        p = self.execute([1000] * 119)
        self.assertNotEqual(p['bridge'].returncode, 0)
        self.assertIn('INSUFFICIENT_DISPLAY_DURATION', p['bridge'].stderr)
        self.assertFalse(p['normalized_exists'])

    def test_120_s_complete_raw_to_analyzer(self):
        p = self.execute([1000] * 120)
        self.assertEqual(p['bridge'].returncode, 0, p['bridge'].stderr)
        self.assertEqual(p['analyzer'].returncode, 0, p['analyzer'].stderr)
        self.assertEqual(p['receipt']['displayed_coverage_ms'], 120000)
        self.assertEqual(p['output']['observed_duration_s'], 120)
        self.assertEqual(p['output']['sample_count'], 120)
        self.assertEqual(p['output']['duration_basis'], 'PRESENTMON_CUMULATIVE_DISPLAY_INTERVALS')
        self.assertEqual(p['output']['classification'], 'WINDOWS_CAPTURE_REVIEW_REQUIRED')
        self.assertIs(p['output']['runtime_pass'], False)
        self.assertIsNone(p['output']['mean_gpu_ms'])
        self.assertIsNone(p['output']['total_observed_gc_alloc_bytes'])

    def test_121_s_complete_raw_to_analyzer(self):
        p = self.execute([1000] * 121)
        self.assertEqual(p['bridge'].returncode, 0, p['bridge'].stderr)
        self.assertEqual(p['analyzer'].returncode, 0, p['analyzer'].stderr)
        self.assertEqual(p['output']['observed_duration_s'], 121)
        self.assertIs(p['output']['runtime_pass'], False)

    def test_malformed_extra_undeclared_cell_must_be_rejected(self):
        def inject(raw):
            lines = raw.decode('utf8').splitlines()
            lines[1] += ',UNDECLARED_GHOST_FIELD'
            return ('\n'.join(lines) + '\n').encode('utf8')
        p = self.execute([1000] * 120, inject)
        self.assertNotEqual(p['bridge'].returncode, 0, 'MALFORMED_RAW_ROW must fail closed')
        self.assertIn('MALFORMED_RAW_ROW', p['bridge'].stderr)
        self.assertFalse(p['normalized_exists'])

    def test_duplicate_required_header_must_be_rejected(self):
        def inject(raw):
            lines = raw.decode('utf8').splitlines()
            lines[0] = lines[0].replace('ProcessID,', 'ProcessID,ProcessID,', 1)
            # Duplicate column with a value, not just a shifted malformed row.
            lines[1:] = [line.replace('RoboRougelike.exe,42,', 'RoboRougelike.exe,42,42,', 1) for line in lines[1:]]
            return ('\n'.join(lines) + '\n').encode('utf8')
        p = self.execute([1000] * 120, inject)
        self.assertNotEqual(p['bridge'].returncode, 0)
        self.assertIn('DUPLICATE_RAW_COLUMNS', p['bridge'].stderr)
        self.assertFalse(p['normalized_exists'])

    def test_legitimate_extra_presentmon_header_is_allowed(self):
        def inject(raw):
            lines = raw.decode('utf8').splitlines()
            lines[0] += ',AdditionalOptionalPresentMonColumn'
            lines[1:] = [line + ',any' for line in lines[1:]]
            return ('\n'.join(lines) + '\n').encode('utf8')
        p = self.execute([1000] * 120, inject)
        self.assertEqual(p['bridge'].returncode, 0, p['bridge'].stderr)
        self.assertEqual(p['analyzer'].returncode, 0, p['analyzer'].stderr)
        self.assertEqual(p['output']['observed_duration_s'], 120)


if __name__ == '__main__':
    unittest.main(verbosity=2)
