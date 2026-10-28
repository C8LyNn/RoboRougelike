"""Regression tests on unchanged PASS13 PresentMon bridge + amended PASS11 analyzer.

All generated input rows in this script are SYNTHETIC CONTROLS, NOT real gameplay.
"""
import csv
import io
import json
import pathlib
import sys
import tempfile
import unittest
from copy import deepcopy

ROOT=pathlib.Path(__file__).parent
sys.path.insert(0,str(ROOT))
from presentmon_bridge import convert_bytes
from analyze_frametimes import COLS, read_csv, summary, validate_meta

DUMMY_META = {
    'origin':'WINDOWS_PLAYER_CAPTURE',
    'source_sha':'7299dcc5fd63ac042a0ffcd97b69d02fb8744649',
    'player_zip_sha256':'ec3193bb3ac24398c496ea65ad12aa653321db160a4dc74e8f618e3cc7ad6d1d',
    'scenario_id':'G02', 'run_id':'R01',
    'authorization_url':'SYNTHETIC_UNIT_TEST_ONLY_NOT_ACTUAL_APPROVAL',
    'host':'TEST_ONLY', 'cpu':'TEST_ONLY','gpu':'TEST_ONLY','driver':'TEST_ONLY',
    'graphics_api':'D3D12','mode':'RELEASE','width':1280,'height':720,
    'warmup_seconds':30,'capture_seconds':120,'repetition':1, 'tool':'PRESENTMON'
}

def create_raw_ms(durations,extra_na=False):
    f=io.StringIO()
    w=csv.DictWriter(f,fieldnames=['Application','ProcessID','SwapChainAddress','MsBetweenDisplayChange'])
    w.writeheader()
    if extra_na:w.writerow({'Application':'RoboRougelike.exe','ProcessID':'42','SwapChainAddress':'0x1','MsBetweenDisplayChange':'NA'})
    for ms in durations:
        w.writerow({'Application':'RoboRougelike.exe','ProcessID':'42','SwapChainAddress':'0x1','MsBetweenDisplayChange':ms})
    return f.getvalue().encode()

def bridge_to_analyzer(raw):
    rows,receipt=convert_bytes(raw,'G02','R01')
    with tempfile.TemporaryDirectory() as temp:
        p=pathlib.Path(temp)/'normalized.csv'
        with p.open('w',encoding='utf8',newline='') as fd:
            w=csv.writer(fd);w.writerow(COLS);w.writerows(rows)
        parsed=read_csv(p)
    return parsed,receipt

class ExactBoundaryPipelineTests(unittest.TestCase):
    def test_120_seconds_no_lost_first_interval(self):
        rows,rec=bridge_to_analyzer(create_raw_ms([1000]*120))
        self.assertEqual(rec['displayed_coverage_ms'],120000.0)
        s=summary(rows,DUMMY_META)
        self.assertEqual(s['observed_duration_s'],120.0)
        self.assertEqual(s['duration_basis'],'PRESENTMON_CUMULATIVE_DISPLAY_INTERVALS')
        self.assertEqual(s['sample_count'],120)
        self.assertEqual(s['classification'],'WINDOWS_CAPTURE_REVIEW_REQUIRED')
        self.assertFalse(s['runtime_pass'])
    def test_121_seconds_accepted(self):
        rows,receipt=bridge_to_analyzer(create_raw_ms([1000]*121))
        self.assertEqual(receipt['displayed_coverage_ms'],121000.0)
        result=summary(rows,DUMMY_META)
        self.assertEqual(result['observed_duration_s'],121.0)
        self.assertEqual(result['sample_count'],121)
        self.assertFalse(result['runtime_pass'])

    def test_one_present_short_of_120s_rejected(self):
        # Bridge and analyzer must *both* reject this; no false 120s threshold.
        with self.assertRaisesRegex(ValueError,'INSUFFICIENT_DISPLAY_DURATION'):
            bridge_to_analyzer(create_raw_ms([1000]*119))
    def test_boundary_with_missing_interval_before_first_display(self):
        rows,rec=bridge_to_analyzer(create_raw_ms([1000]*120,extra_na=True))
        self.assertEqual(rec['ignored_unavailable_display_interval_rows'],1)
        self.assertEqual(summary(rows,DUMMY_META)['observed_duration_s'],120.0)
    def test_tampered_cumulative_times_rejected(self):
        rows,_=bridge_to_analyzer(create_raw_ms([1000]*120))
        rows[40]['time_ms']+=100
        with self.assertRaisesRegex(ValueError,'cumulative display timestamps mismatch'):
            validate_meta(DUMMY_META,rows)
    def test_disguised_submit_times_rejected(self):
        rows,_=bridge_to_analyzer(create_raw_ms([1000]*120))
        rows[0]['time_ms']=0.0  # old END_MINUS_FIRST accounting, cannot fake new frame.
        with self.assertRaisesRegex(ValueError,'cumulative display timestamps mismatch'):
            validate_meta(DUMMY_META,rows)
    def test_absent_gpu_gc_remains_null(self):
        rows,_=bridge_to_analyzer(create_raw_ms([1000]*120))
        out=summary(rows,DUMMY_META)
        self.assertIsNone(out['mean_gpu_ms'])
        self.assertIsNone(out['total_observed_gc_alloc_bytes'])
    def test_non_presentmon_duration_unchanged(self):
        rows,_=bridge_to_analyzer(create_raw_ms([1000]*120))
        meta=deepcopy(DUMMY_META);meta['tool']='UNITY_PROFILER'
        with self.assertRaisesRegex(ValueError,'captured duration <120 s'):
            validate_meta(meta,rows)
        rows[-1]['time_ms']=121000
        self.assertEqual(validate_meta(meta,rows),'WINDOWS_PLAYER_CAPTURE')
    def test_synthetic_inputs_never_promoted(self):
        rows,_=bridge_to_analyzer(create_raw_ms([1000]*120))
        meta=deepcopy(DUMMY_META);meta['origin']='SYNTHETIC'
        out=summary(rows,meta)
        self.assertEqual(out['classification'],'SYNTHETIC_TEST_ONLY')
        self.assertFalse(out['runtime_pass'])

if __name__=='__main__':unittest.main(verbosity=2)
